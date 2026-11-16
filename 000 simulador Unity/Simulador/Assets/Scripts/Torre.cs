// TORRE (prototipo): cuadro retractil arriba, a la derecha de "Azafata", con un display de letras electronicas.
// Dice destino, altura/velocidad y, con el boton BONUS prendido, canta premios que aparecen a un lado de la ruta ("a las 2, 8 km, lat/lon").
// Los premios se cobran SOLO pasando por el radio exacto (sin disparar). Estrellas en el aire y puntos en el piso. Racha = multiplicador.
// Con BONUS apagado el viaje es normal y tranquilo.
using UnityEngine;
using CesiumForUnity;
using Unity.Mathematics;
using System.Collections.Generic;
using System.Globalization;

public partial class Torre : MonoBehaviour
{
    public static bool lateralAbierto;   // lo avisa AvionDeCarton: si el panel grande de la derecha esta abierto, la torre queda en barrita
    static Torre inst;

    enum Tipo { Celeste, Rojo, Jefe, Combustible }
    class Premio { public bool verde; public Tipo tipo; public double lat, lon, alt; public int valor, vida; public GameObject go; public float nace; public Light luz; public Transform quad, lab, halo; public Material mat, labMat, haloMat, aroMat; public Color haloCol; }
    Premio premio;
    bool abierta = true, bonusOn;
    int racha, total; float proxSpawn = 8f, proxCharla = 50f, proxPueblo = 5f;
    string mensaje = "TORRE LISTA. APRETA BONUS PARA JUGAR", mensajeFijo = ""; float mensajeHasta;
    Material mat, matRayo; Texture2D texAire, texPiso, texJefe, texFlecha;
    GUIStyle sLed, sLedChico, sBtn, sTit, sEtq, sMsg; Font fuente;
    Camera cam; AvionDeCarton avion; Transform padre;
    double pLat, pLon; float pAlt; bool hayPrev; float prevRumbo;
    // para sweep de cobro: posicion anterior
    double aLat, aLon, aAlt; bool hayA;
    readonly string[] CHARLAS = {
        "TORRE: CIELO DESPEJADO, BUEN VUELO", "TORRE: TRAFICO NULO EN TU RUTA", "TORRE: ACORDATE DE MIRAR EL HORIZONTE",
        "TORRE: AEROPARQUE TE SALUDA, PILOTO", "TORRE: DICEN QUE HAY CAFE EN EL DESTINO", "TORRE: VIENTO CALMO, TODO BAJO CONTROL",
        "TORRE: SI VES UNA ESTRELLA, ES TUYA" };

    const float RADIO_PREMIO = 60f, VIDA_PREMIO = 150f;   // celestes: se cobran pasando. Rojos: se cobran a tiros

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Crear() { if (FindFirstObjectByType<CesiumGeoreference>() == null) return; new GameObject("Torre").AddComponent<Torre>(); }

    AudioSource audio; AudioClip clipLaser, clipPing;
    GUIStyle sMsgW;
    void Awake() { inst = this; Carrera.AlLlegar += AlAterrizar; audio = gameObject.AddComponent<AudioSource>(); audio.spatialBlend = 0f; audio.playOnAwake = false; audio.volume = 0.5f; laserSrc = gameObject.AddComponent<AudioSource>(); laserSrc.spatialBlend = 0f; laserSrc.playOnAwake = false; clipLaser = Sonido(1800f, 420f, 0.30f, 14f, true); clipPing = Sonido(900f, 1300f, 0.18f, 18f, false); }
    // sonidito amigable hecho a mano (sin archivos): barrido de tono con un poco de armonico y caida suave, tipo laser de juego
    static AudioClip Sonido(float f0, float f1, float seg, float caida, bool armonico)
    {
        int sr = 44100, n = (int)(sr * seg); var d = new float[n]; double ph = 0;
        for (int i = 0; i < n; i++) { float t = i / (float)n; float f = f0 * Mathf.Pow(f1 / f0, t); ph += 2 * System.Math.PI * f / sr; float env = Mathf.Exp(-caida * t) * Mathf.Min(1f, i / 200f); float v = (float)System.Math.Sin(ph) + (armonico ? 0.25f * (float)System.Math.Sin(ph * 2.0) : 0f); d[i] = v * 0.35f * env; }
        var c = AudioClip.Create("sonidito", n, 1, sr, false); c.SetData(d, 0); return c;
    }

    // ------------------------------------------------------------------ recursos
    void Preparar()
    {
        if (mat != null) return;
        var sh = Shader.Find("Universal Render Pipeline/Unlit"); if (sh == null) sh = Shader.Find("Unlit/Color");
        mat = new Material(sh); matRayo = new Material(sh);
        var c = new Color(0.1f, 1f, 1f); matRayo.SetColor("_BaseColor", c); matRayo.SetColor("_Color", c);
        texAire = Resources.Load<Texture2D>("blancos/blanco_colores"); texPiso = Resources.Load<Texture2D>("blancos/blanco_rojo"); texJefe = Resources.Load<Texture2D>("blancos/blanco_negro");
        var t = new Texture2D(32, 32, TextureFormat.RGBA32, false);
        for (int y = 0; y < 32; y++) for (int x = 0; x < 32; x++) { float lim = (1f - y / 31f) * 16f; t.SetPixel(x, y, Mathf.Abs(x - 15.5f) <= lim ? Color.white : Color.clear); }
        t.Apply(); texFlecha = t;   // apunta hacia arriba
        var padreGeo = FindFirstObjectByType<CesiumGeoreference>(); if (padreGeo != null) padre = padreGeo.transform;
    }

    // ------------------------------------------------------------------ geometria
    static double MetrosLat = 111320.0;
    static double Dist(double la1, double lo1, double la2, double lo2) { double n = (la2 - la1) * MetrosLat, e = (lo2 - lo1) * MetrosLat * System.Math.Cos(la1 * System.Math.PI / 180.0); return System.Math.Sqrt(n * n + e * e); }
    static float Rumbo(double la1, double lo1, double la2, double lo2) { double n = (la2 - la1), e = (lo2 - lo1) * System.Math.Cos(la1 * System.Math.PI / 180.0); float r = (float)(System.Math.Atan2(e, n) * 180.0 / System.Math.PI); return r < 0 ? r + 360f : r; }

    void Aviso(string t, float seg) { mensaje = t; mensajeHasta = Time.unscaledTime + seg; }

    // ------------------------------------------------------------------ premios (varios a la vez, en oleadas)
    // CELESTE: se cobra pasando por el circulo.  ROJO: se cobra disparando.  JEFE (negro): hay que pegarle 3 tiros.
    static float R(float a, float b) { return UnityEngine.Random.Range(a, b); }
    readonly List<Premio> premios = new List<Premio>();
    float proxDisparo, comboHasta, proxDato = 14f, proxResumen = 12f; string ultimoLugar = ""; bool avisoSonico, avisoAltura, avisoCerca;
    // disparo: laser 5 (elegido por Pablo); si no carga, laser small 2; y si tampoco, el sonidito hecho a mano
    AudioClip dispA, dispB; bool dispCargado;
    void SonarDisparo()
    {
        if (!dispCargado) { dispCargado = true; dispA = Resources.Load<AudioClip>("sonidos/disparo_a"); dispB = Resources.Load<AudioClip>("sonidos/disparo_b"); }
        var c = dispA != null && dispA.loadState != AudioDataLoadState.Failed ? dispA : (dispB != null ? dispB : clipLaser);
        laserSrc.clip = c; laserSrc.volume = 0.8f * Mezcla.De(Mezcla.FX); laserSrc.Play();
    }
    AudioSource laserSrc; AudioClip pendClip; float pendHasta, pendVol;
    // el sonido de acierto entra una fracción de segundo despues del disparo y corta el laser, para que no suenen los dos juntos
    void Programar(AudioClip c, float vol, float retraso)
    {
        if (retraso <= 0f) { Reproducir(c, vol); return; }
        pendClip = c; pendVol = vol; pendHasta = Time.unscaledTime + retraso;
    }
    void Reproducir(AudioClip c, float vol)
    {
        if (laserSrc != null) laserSrc.Stop();
        var f = new GameObject("SonidoFX").AddComponent<AudioSource>(); f.spatialBlend = 0f; f.volume = vol * Mezcla.De(Mezcla.FX); f.clip = c; f.Play(); Destroy(f.gameObject, 4f);
    }
    const int MAX_PREMIOS = 24; int maxSim = 1;   // nivel del bonus: arranca en 1 (tranquilo: 2 celestes por tanda) y cada nivel suma mas cosas
    int RojosMax() { return maxSim <= 1 ? 0 : (maxSim + 1) / 2; }   // nivel 1: ninguno; 2: 1; 3: 2; 4: 2; 5: 3...
    AudioClip cobroA, cobroB, acierto;   // sonido unico de monedas (por ahora el de "compra")

    static void Mover(double lat, double lon, float rb, float adelante, float lateral, out double la, out double lo)
    {
        double rad = rb * System.Math.PI / 180.0, radP = (rb + 90f) * System.Math.PI / 180.0;
        double n = System.Math.Cos(rad) * adelante + System.Math.Cos(radP) * lateral, e = System.Math.Sin(rad) * adelante + System.Math.Sin(radP) * lateral;
        la = lat + n / MetrosLat; lo = lon + e / (MetrosLat * System.Math.Cos(lat * System.Math.PI / 180.0));
    }

    // ---- carteles con el precio en el centro (se dibuja a mano sobre una copia de la imagen)
    readonly Dictionary<string, Texture2D> texCache = new Dictionary<string, Texture2D>();
    static readonly string[] FUENTE5X7 = {
        "01110,10001,10011,10101,11001,10001,01110", "00100,01100,00100,00100,00100,00100,01110", "01110,10001,00001,00010,00100,01000,11111",
        "11110,00001,00001,01110,00001,00001,11110", "00010,00110,01010,10010,11111,00010,00010", "11111,10000,11110,00001,00001,10001,01110",
        "00110,01000,10000,11110,10001,10001,01110", "11111,00001,00010,00100,01000,01000,01000", "01110,10001,10001,01110,10001,10001,01110",
        "01110,10001,10001,01111,00001,00010,01100", "00100,01111,10100,01110,00101,11110,00100" };   // 0-9 y $

    static Texture2D Legible(Texture2D src, int tam)
    {
        var rt = RenderTexture.GetTemporary(tam, tam, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
        Graphics.Blit(src, rt); var prev = RenderTexture.active; RenderTexture.active = rt;
        var t = new Texture2D(tam, tam, TextureFormat.RGBA32, true); t.ReadPixels(new Rect(0, 0, tam, tam), 0, 0); t.Apply();
        RenderTexture.active = prev; RenderTexture.ReleaseTemporary(rt); return t;
    }

    static void RectPx(Color32[] px, int tam, int x0, int y0, int x1, int y1, Color32 c)
    {
        for (int y = Mathf.Max(0, y0); y < Mathf.Min(tam, y1); y++) for (int x = Mathf.Max(0, x0); x < Mathf.Min(tam, x1); x++) px[y * tam + x] = c;
    }

    Texture2D TexPremio(Tipo t, int valor, bool verde)
    {
        string clave = t + "_" + valor + "_" + verde; Texture2D r;
        if (texCache.TryGetValue(clave, out r)) return r;
        if (t == Tipo.Combustible) { var bt = TexBidon(); texCache[clave] = bt; return bt; }
        Texture2D baseT = t == Tipo.Celeste ? texAire : (t == Tipo.Jefe ? (texJefe != null ? texJefe : texPiso) : texPiso);
        if (baseT == null) return null;
        const int tam = 512; var copia = Legible(baseT, tam); var px = copia.GetPixels32();
        if (verde) for (int i = 0; i < px.Length; i++) { var c = px[i]; byte tmp = c.r; c.r = (byte)(c.g * 0.4f); c.g = (byte)Mathf.Min(255, (tmp * 0.9f) + 20f); c.b = (byte)(c.b * 0.4f); px[i] = c; }   // rojo -> verde dolar
        string txt = "$" + valor; int k = Mathf.RoundToInt(tam * 0.17f / 7f), ancho = txt.Length * 6 * k - k, x00 = tam / 2 - ancho / 2, y00 = tam / 2 - 7 * k / 2;
        var negro = new Color32(0, 0, 0, 255); var blanco = new Color32(255, 255, 255, 255); int o = Mathf.Max(2, k / 3);
        for (int pasada = 0; pasada < 2; pasada++)
            for (int ci = 0; ci < txt.Length; ci++)
            {
                int gi = txt[ci] == '$' ? 10 : txt[ci] - '0'; var filas = FUENTE5X7[gi].Split(',');
                for (int fy = 0; fy < 7; fy++) for (int fx2 = 0; fx2 < 5; fx2++) if (filas[fy][fx2] == '1')
                {
                    int x = x00 + ci * 6 * k + fx2 * k, y = y00 + (6 - fy) * k;
                    if (pasada == 0) RectPx(px, tam, x - o, y - o, x + k + o, y + k + o, blanco); else RectPx(px, tam, x, y, x + k, y + k, negro);
                }
            }
        copia.SetPixels32(px); copia.Apply(); texCache[clave] = copia; return copia;
    }

    // etiqueta del precio: texto en negrita con borde negro, color segun el valor (blanco 10, amarillo 20, naranja 50, verde 100, magenta chancha, celeste 5)
    readonly Dictionary<string, Texture2D> texEtiq = new Dictionary<string, Texture2D>();
    Texture2D EtiquetaTex(Tipo t, int valor, bool verde)
    {
        string clave = t + "_" + valor; Texture2D r; if (texEtiq.TryGetValue(clave, out r)) return r;
        Color32 col = t == Tipo.Combustible ? new Color32(255, 130, 0, 255) : t == Tipo.Celeste ? new Color32(215, 0, 0, 255) : t == Tipo.Jefe ? new Color32(150, 0, 160, 255) : valor >= 100 ? new Color32(0, 120, 20, 255) : valor >= 50 ? new Color32(215, 85, 0, 255) : valor >= 20 ? new Color32(0, 40, 170, 255) : new Color32(5, 5, 5, 255);
        const int W = 256, H = 96; var tx = new Texture2D(W, H, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp }; var px = new Color32[W * H];
        string txt = t == Tipo.Combustible ? valor.ToString() : "$" + valor; int k = 8, ancho = txt.Length * 6 * k - k, x00 = W / 2 - ancho / 2, y00 = H / 2 - 7 * k / 2, o = 4; var negro = new Color32(255, 255, 255, 255);
        for (int pasada = 0; pasada < 2; pasada++)
            for (int ci = 0; ci < txt.Length; ci++)
            {
                int gi = txt[ci] == '$' ? 10 : txt[ci] - '0'; var filas = FUENTE5X7[gi].Split(',');
                for (int fy = 0; fy < 7; fy++) for (int fx2 = 0; fx2 < 5; fx2++) if (filas[fy][fx2] == '1')
                {
                    int x = x00 + ci * 6 * k + fx2 * k, y = y00 + (6 - fy) * k; int e = pasada == 0 ? o : 0; var c = pasada == 0 ? negro : col;
                    for (int yy = Mathf.Max(0, y - e); yy < Mathf.Min(H, y + k + e); yy++) for (int xx = Mathf.Max(0, x - e); xx < Mathf.Min(W, x + k + e); xx++) px[yy * W + xx] = c;
                }
            }
        tx.SetPixels32(px); tx.Apply(); texEtiq[clave] = tx; return tx;
    }

    // bidon de combustible dibujado a mano: cuerpo naranja, asa, pico y una gotita blanca
    Texture2D texBidon;
    Texture2D TexBidon()
    {
        if (texBidon != null) return texBidon;
        const int n = 128; var px = new Color32[n * n]; var borde = new Color32(60, 25, 0, 255); var cuerpo = new Color32(255, 140, 20, 255); var sombra = new Color32(215, 100, 0, 255); var blanco = new Color32(255, 255, 255, 255);
        RectPx(px, n, 24, 14, 104, 106, borde); RectPx(px, n, 28, 18, 100, 102, cuerpo); RectPx(px, n, 28, 18, 44, 102, sombra);   // cuerpo
        RectPx(px, n, 30, 106, 74, 122, borde); RectPx(px, n, 36, 106, 68, 116, new Color32(0, 0, 0, 0));                           // asa
        RectPx(px, n, 78, 106, 98, 122, borde); RectPx(px, n, 82, 106, 94, 118, sombra);                                            // pico
        RectPx(px, n, 36, 54, 92, 58, borde); RectPx(px, n, 36, 74, 92, 78, borde);                                                  // nervaduras
        for (int y = 0; y < 22; y++) { int w = y < 6 ? 3 + y : Mathf.Max(0, 14 - (y - 6)); RectPx(px, n, 64 - w, 28 + y, 64 + w, 29 + y, blanco); }   // gota
        texBidon = new Texture2D(n, n, TextureFormat.RGBA32, true) { wrapMode = TextureWrapMode.Clamp }; texBidon.SetPixels32(px); texBidon.Apply(); return texBidon;
    }

    // halo suave detras del cartel: asi se distingue el color de noche (celeste / rojo / verde / violeta / naranja)
    Texture2D texHalo;
    Texture2D TexHalo()
    {
        if (texHalo != null) return texHalo;
        const int n = 64; var px = new Color32[n * n];
        for (int y = 0; y < n; y++) for (int x = 0; x < n; x++) { float d = Mathf.Sqrt((x - 31.5f) * (x - 31.5f) + (y - 31.5f) * (y - 31.5f)) / 31.5f; float a = Mathf.Clamp01(1f - d); a = a * a * (3f - 2f * a); px[y * n + x] = new Color32(255, 255, 255, (byte)(a * 255)); }
        texHalo = new Texture2D(n, n, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp }; texHalo.SetPixels32(px); texHalo.Apply(); return texHalo;
    }

    // aros de contorno: simple (pasada) o doble con 4 marcas de mira (disparo)
    readonly Texture2D[] texAro = new Texture2D[2];
    Texture2D TexAro(bool disparo)
    {
        int ix = disparo ? 1 : 0; if (texAro[ix] != null) return texAro[ix];
        const int n = 128; var px = new Color32[n * n]; float c0 = (n - 1) * 0.5f;
        for (int y = 0; y < n; y++) for (int x = 0; x < n; x++)
        {
            float dx = x - c0, dy = y - c0, d = Mathf.Sqrt(dx * dx + dy * dy) / c0; float a = 0f;
            if (!disparo) a = Mathf.Clamp01(1f - Mathf.Abs(d - 0.93f) / 0.04f);
            else
            {
                a = Mathf.Max(Mathf.Clamp01(1f - Mathf.Abs(d - 0.95f) / 0.035f), Mathf.Clamp01(1f - Mathf.Abs(d - 0.78f) / 0.035f));
                bool marca = (Mathf.Abs(dx) < 3.5f || Mathf.Abs(dy) < 3.5f) && d > 0.5f && d < 1f; if (marca) a = 1f;   // cruz de mira
            }
            px[y * n + x] = new Color32(255, 255, 255, (byte)(Mathf.Clamp01(a) * 255));
        }
        var tx = new Texture2D(n, n, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp }; tx.SetPixels32(px); tx.Apply(); texAro[ix] = tx; return tx;
    }

    Premio CrearPremio(Tipo t, int valor, int vida, bool verde, double la, double lo, double alt)
    {
        var p = new Premio { tipo = t, lat = la, lon = lo, alt = alt, nace = Time.unscaledTime, valor = valor, vida = vida, verde = verde };
        var go = GameObject.CreatePrimitive(PrimitiveType.Quad); var col = go.GetComponent<Collider>(); if (col != null) Destroy(col);
        go.name = "PremioTorre"; go.transform.SetParent(padre, false);
        var anc = go.AddComponent<CesiumGlobeAnchor>(); anc.longitudeLatitudeHeight = new double3(lo, la, alt);
        var m = new Material(mat); var tex = TexPremio(t, valor, verde);
        m.SetTexture("_BaseMap", tex); m.SetTexture("_MainTex", tex); m.SetColor("_BaseColor", Color.white); m.SetColor("_Color", Color.white);
        m.SetFloat("_Cull", 0f); m.SetFloat("_AlphaClip", 1f); m.SetFloat("_Cutoff", 0.5f); m.EnableKeyword("_ALPHATEST_ON"); m.renderQueue = 2450;
        var r = go.GetComponent<Renderer>(); r.sharedMaterial = m; r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        p.go = go; p.quad = go.transform; p.mat = m;
        // etiqueta flotante con el precio (transparente, mira a la camara porque cuelga del cartel)
        var lab = GameObject.CreatePrimitive(PrimitiveType.Quad); var cl2 = lab.GetComponent<Collider>(); if (cl2 != null) Destroy(cl2);
        lab.name = "EtiquetaPrecio"; lab.transform.SetParent(go.transform, false);
        var lm = new Material(mat); var lt = EtiquetaTex(t, valor, verde);
        lm.SetTexture("_BaseMap", lt); lm.SetTexture("_MainTex", lt); lm.SetFloat("_Surface", 1f); lm.SetFloat("_Blend", 0f); lm.SetFloat("_SrcBlend", 5f); lm.SetFloat("_DstBlend", 10f); lm.SetFloat("_ZWrite", 0f); lm.SetFloat("_Cull", 0f);
        lm.EnableKeyword("_SURFACE_TYPE_TRANSPARENT"); lm.SetOverrideTag("RenderType", "Transparent"); lm.renderQueue = 3000;
        var lr = lab.GetComponent<Renderer>(); lr.sharedMaterial = lm; lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        p.lab = lab.transform; p.labMat = lm; lab.SetActive(false);
        // halo de color detras del cartel (se ve mucho mas de noche)
        var hl = GameObject.CreatePrimitive(PrimitiveType.Quad); var ch = hl.GetComponent<Collider>(); if (ch != null) Destroy(ch);
        hl.name = "HaloPremio"; hl.transform.SetParent(go.transform, false); hl.transform.localPosition = new Vector3(0f, 0f, 0.05f); hl.transform.localScale = new Vector3(2.1f, 2.1f, 1f);
        p.haloCol = t == Tipo.Celeste ? new Color(0.1f, 0.9f, 1f) : t == Tipo.Combustible ? new Color(1f, 0.6f, 0.1f) : t == Tipo.Jefe ? new Color(0.9f, 0.2f, 1f) : (verde ? new Color(0.2f, 1f, 0.3f) : new Color(1f, 0.1f, 0.05f));
        var hm = new Material(mat); var ht = TexHalo();
        hm.SetTexture("_BaseMap", ht); hm.SetTexture("_MainTex", ht); hm.SetFloat("_Surface", 1f); hm.SetFloat("_Blend", 0f); hm.SetFloat("_SrcBlend", 5f); hm.SetFloat("_DstBlend", 1f); hm.SetFloat("_ZWrite", 0f); hm.SetFloat("_Cull", 0f);   // mezcla ADITIVA: brilla como fluor, sobre todo de noche
        hm.EnableKeyword("_SURFACE_TYPE_TRANSPARENT"); hm.SetOverrideTag("RenderType", "Transparent"); hm.renderQueue = 2999;
        var hr = hl.GetComponent<Renderer>(); hr.sharedMaterial = hm; hr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        p.halo = hl.transform; p.haloMat = hm;
        // contorno: aro simple = se cobra PASANDO (celeste, bidon); aro doble con marcas de mira = se cobra DISPARANDO (rojo, verde, chancha)
        bool disparo = t == Tipo.Rojo || t == Tipo.Jefe;
        var ar = GameObject.CreatePrimitive(PrimitiveType.Quad); var ca2 = ar.GetComponent<Collider>(); if (ca2 != null) Destroy(ca2);
        ar.name = "AroPremio"; ar.transform.SetParent(go.transform, false); ar.transform.localPosition = new Vector3(0f, 0f, 0.04f); ar.transform.localScale = new Vector3(1.7f, 1.7f, 1f);
        var am = new Material(mat); var at = TexAro(disparo);
        am.SetTexture("_BaseMap", at); am.SetTexture("_MainTex", at); am.SetColor("_BaseColor", p.haloCol); am.SetColor("_Color", p.haloCol);
        am.SetFloat("_Surface", 1f); am.SetFloat("_Blend", 0f); am.SetFloat("_SrcBlend", 5f); am.SetFloat("_DstBlend", 1f); am.SetFloat("_ZWrite", 0f); am.SetFloat("_Cull", 0f);
        am.EnableKeyword("_SURFACE_TYPE_TRANSPARENT"); am.SetOverrideTag("RenderType", "Transparent"); am.renderQueue = 2998;
        p.aroMat = am; var ark = ar.GetComponent<Renderer>(); ark.sharedMaterial = am; ark.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        // luz de verdad: ilumina el entorno de noche con el color del premio
        var lgo = new GameObject("LuzPremio"); lgo.transform.SetParent(go.transform, false);
        var lz = lgo.AddComponent<Light>(); lz.type = LightType.Point; lz.color = p.haloCol; lz.range = 140f; lz.intensity = 0f; lz.shadows = LightShadows.None; p.luz = lz;
        premios.Add(p); return p;
    }

    void Quitar(Premio p) { if (p == null) return; if (p.go != null) Destroy(p.go); premios.Remove(p); }
    void QuitarTodos() { foreach (var p in premios) if (p.go != null) Destroy(p.go); premios.Clear(); premio = null; }

    static string Hora(double lat, double lon, float rumbo, Premio p, out double km)
    {
        float rb = Rumbo(lat, lon, p.lat, p.lon); float rel = Mathf.DeltaAngle(rumbo, rb);
        int hora = Mathf.RoundToInt(rel / 30f); hora = ((hora % 12) + 12) % 12; if (hora == 0) hora = 12;
        km = Dist(lat, lon, p.lat, p.lon) / 1000.0; return hora.ToString();
    }

    // una oleada nueva por delante. CELESTES ($5): sobre la ruta, apenas corridos a un lado y al otro (no se agarran con piloto automatico);
    // a veces uno de $50 bien al costado que obliga a desviarse. ROJOS (disparo): varios desparramados, la mayoria de $10; los de $100 son verdes,
    // de 3 tiros y lejos. De vez en cuando una chancha negra ($150, 3 tiros).
    float ladoCeleste = 1f;
    int Cuenta(Tipo tp) { int n = 0; foreach (var p in premios) if (p.tipo == tp) n++; return n; }
    void Oleada(double lat, double lon, float alt, float rumbo, float vel)
    {
        float agl = avion != null ? avion.AlturaSobrePiso : 999f;
        double ground = agl < 900f ? alt - agl : 25.0;
        float rb = Carrera.DestinoRumbo(lat, lon, rumbo);
        float adelante = Mathf.Clamp(vel * 30f, 2500f, 20000f);
        double pisoC = ground + 220.0;
        double altRojo = System.Math.Max(System.Math.Max(1800.0, ground + 700.0), alt + R(-150f, 250f));
        Premio primero = null; double la, lo; int nRojos = RojosMax() == 0 ? 0 : UnityEngine.Random.Range(1, RojosMax() + 1); bool chancha = maxSim >= 4 && UnityEngine.Random.value < 0.12f, grande = maxSim >= 3 && UnityEngine.Random.value < 0.18f;
        // celeste de ruta: solo si no queda ninguno; el segundo queda casi en linea y un poco mas lejos (con una correccion chica se agarra tambien)
        int nCel = Cuenta(Tipo.Celeste), rojosMax = RojosMax();
        if (nCel == 0)
        {
            ladoCeleste = -ladoCeleste; float lat1 = ladoCeleste * R(300f, 800f); Mover(lat, lon, rb, adelante, lat1, out la, out lo);
            primero = CrearPremio(Tipo.Celeste, 5, 1, false, la, lo, System.Math.Max(pisoC, alt + R(-60f, 60f)));
            Mover(lat, lon, rb, adelante + R(2500f, 3500f), lat1 * 0.3f, out la, out lo); CrearPremio(Tipo.Celeste, 5, 1, false, la, lo, System.Math.Max(pisoC, alt + R(-60f, 60f)));
        }
        // bidon de combustible (100 L): a un costado de la ruta, se agarra pasando (si el tanque esta lleno va a la reserva)
        if (Cuenta(Tipo.Combustible) == 0 && UnityEngine.Random.value < 0.40f) { Mover(lat, lon, rb, adelante * R(0.6f, 1.1f), (UnityEngine.Random.value < 0.5f ? -1f : 1f) * R(250f, 900f), out la, out lo); var bid = CrearPremio(Tipo.Combustible, 100, 1, false, la, lo, System.Math.Max(pisoC, alt + R(-60f, 60f))); if (primero == null) primero = bid; }
        if (grande && nCel == 0 && Cuenta(Tipo.Celeste) < 3) { Mover(lat, lon, rb, adelante * R(0.9f, 1.4f), (UnityEngine.Random.value < 0.5f ? -1f : 1f) * R(2800f, 4500f), out la, out lo); CrearPremio(Tipo.Celeste, 50, 1, false, la, lo, System.Math.Max(pisoC, alt + R(-150f, 150f))); }
        // rojos
        int cien = 0;
        for (int i = 0; i < nRojos && Cuenta(Tipo.Rojo) < rojosMax; i++)
        {
            float q = UnityEngine.Random.value; int valor = q < 0.60f ? 10 : (q < 0.80f ? 20 : (q < 0.92f ? 50 : 100)); bool v = valor == 100; if (v) cien++;
            float lado = (UnityEngine.Random.value < 0.5f ? -1f : 1f) * (v ? R(4500f, 8000f) : (valor == 50 ? R(3000f, 6000f) : R(1200f, 5000f)));
            Mover(lat, lon, rb, adelante * (v ? R(1.0f, 1.6f) : R(0.4f, 1.5f)), lado, out la, out lo);
            CrearPremio(Tipo.Rojo, valor, 1, v, la, lo, System.Math.Max(ground + 700.0, altRojo + R(-300f, 300f)));
        }
        if (chancha && Cuenta(Tipo.Jefe) == 0 && premios.Count < maxSim + 3) { Mover(lat, lon, rb, adelante * R(0.8f, 1.5f), (UnityEngine.Random.value < 0.5f ? -1f : 1f) * R(2000f, 5000f), out la, out lo); CrearPremio(Tipo.Jefe, 150, 3, false, la, lo, altRojo); }
        if (primero == null && premios.Count > 0) primero = premios[premios.Count - 1]; if (primero == null) return;
        audio.PlayOneShot(clipPing);
        double km; string h = Hora(lat, lon, rumbo, primero, out km);
        Aviso("CELESTE $5 A LAS " + h + ", " + km.ToString("F1", CultureInfo.InvariantCulture) + " KM. " + nRojos + " ROJOS PARA DISPARAR" + (cien > 0 ? ", UNO VERDE DE $100" : "") + (chancha ? ". CHANCHA NEGRA!" : "") + (grande ? ". CELESTE DE $50 BIEN AL COSTADO" : ""), 8f);
    }

    // cobro de un premio: el precio es fijo (la racha solo cuenta aciertos seguidos, no multiplica la plata)
    void Cobrar(Premio p, string verbo)
    {
        if (p.tipo == Tipo.Combustible)
        {
            bool lleno = Combustible.Tanque > Combustible.CAPACIDAD - 1f; Combustible.Agregar(p.valor); Clips();
            Programar(cobroB != null ? cobroB : clipPing, 0.9f, 0f);
            Aviso(lleno ? "BIDÓN: +" + p.valor + " L A TU RESERVA (TANQUE LLENO). RESERVA " + Mathf.RoundToInt(Combustible.Reserva) + " L" : "BIDÓN: +" + p.valor + " L AL TANQUE", 5f); Quitar(p); return;
        }
        racha = Time.unscaledTime < comboHasta ? Mathf.Min(99, racha + 1) : 1; comboHasta = Time.unscaledTime + 20f;
        int monto = p.valor; total += monto; subtotal += monto;   // se acumula en el cuadro; pasa a la cuenta al aterrizar o con el boton
        Clips();
        AudioClip cc;
        cc = p.tipo == Tipo.Celeste ? cobroB : (acierto != null ? acierto : cobroB);   // celestes: moneda; rojos/verdes/chancha: "combine special"
        Programar(cc != null ? cc : clipPing, 0.9f, p.tipo == Tipo.Celeste ? 0f : 0.12f);
        Aviso(verbo + " +$" + monto + "   ACIERTOS " + racha + "   ACUMULADO $" + subtotal, 4f); Quitar(p);
    }

    void Golpe(Premio p)
    {
        p.vida--;
        if (p.vida > 0) { p.mat.SetColor("_BaseColor", new Color(0.7f, 0.7f, 0.7f)); p.mat.SetColor("_Color", new Color(0.7f, 0.7f, 0.7f)); Programar(clipPing, 0.6f, 0.12f); Aviso("TOCADO. FALTAN " + p.vida + " TIROS", 3f); return; }
        Cobrar(p, "DERRIBADO");
    }

    // distancia minima del segmento A->B al punto P, en metros (plano local)
    static double DistSegmento(double aLa, double aLo, double bLa, double bLo, double pLa, double pLo, double cosLat)
    {
        double ax = 0, ay = 0, bx = (bLo - aLo) * MetrosLat * cosLat, by = (bLa - aLa) * MetrosLat, px = (pLo - aLo) * MetrosLat * cosLat, py = (pLa - aLa) * MetrosLat;
        double dx = bx - ax, dy = by - ay, l2 = dx * dx + dy * dy; double t = l2 < 1e-6 ? 0 : System.Math.Max(0, System.Math.Min(1, (px * dx + py * dy) / l2));
        double qx = ax + t * dx, qy = ay + t * dy; return System.Math.Sqrt((px - qx) * (px - qx) + (py - qy) * (py - qy));
    }

    // datos de torre sobre los lugares por donde vas pasando
    static readonly string[,] HECHOS = {
        { "rosario", "ROSARIO, CUNA DE LA BANDERA, SOBRE EL PARANA" }, { "zarate", "ZARATE: AHI CRUZA EL PUENTE ZARATE-BRAZO LARGO" },
        { "campana", "CAMPANA: GRAN POLO INDUSTRIAL SOBRE EL PARANA" }, { "san nicolas", "SAN NICOLAS: ALLI SE FIRMO EL ACUERDO DE 1852" },
        { "lujan", "LUJAN: LA BASILICA, DESTINO DE PEREGRINOS" }, { "olivos", "OLIVOS: AHI ESTA LA QUINTA PRESIDENCIAL" },
        { "hipodromo de san isidro", "EL HIPODROMO DE SAN ISIDRO, DE LOS MAS LINDOS DEL PAIS" }, { "nordelta", "NORDELTA: CIUDAD DE LAGOS ARTIFICIALES" },
        { "chascomus", "LAGUNA DE CHASCOMUS: PESCA DE PEJERREY Y VELEROS" }, { "cordoba capital", "CORDOBA, LA DOCTA: UNIVERSIDAD DESDE 1613" },
        { "salta capital", "SALTA LA LINDA, CON LOS ANDES DE FONDO" }, { "carlos paz", "VILLA CARLOS PAZ: LAGO SAN ROQUE Y SIERRAS" },
        { "cosquin", "COSQUIN: CAPITAL DEL FOLKLORE" }, { "la falda", "LA FALDA: EN LAS SIERRAS CHICAS" },
        { "iguazu", "IGUAZU: LAS CATARATAS ESTAN CERQUITA" }, { "fagnano", "LAGO FAGNANO, TIERRA DEL FUEGO" },
        { "la plata", "LA PLATA: CIUDAD PLANIFICADA, CON CATEDRAL NEOGOTICA" }, { "tucuman", "TUCUMAN: EL JARDIN DE LA REPUBLICA" },
        { "rio parana", "EL PARANA: UNO DE LOS RIOS MAS LARGOS DE SUDAMERICA" }, { "mar chiquita", "MAR CHIQUITA: ALBUFERA CON AGUA SALADA Y DULCE" },
        { "san clemente", "SAN CLEMENTE DEL TUYU: COSTA ATLANTICA" }, { "tigre", "EL DELTA DEL PARANA, LABERINTO DE ISLAS" },
        { "isla", "ISLAS DEL DELTA: SOLO SE LLEGA EN LANCHA" }, { "laguna", "UNA LAGUNA: LOS ESPEJOS DE LA PAMPA" },
        { "embalse", "UN EMBALSE: AGUA Y ENERGIA PARA TODA LA ZONA" }, { "parque nacional", "UN PARQUE NACIONAL: NATURALEZA PROTEGIDA" },
        { "reserva", "UNA RESERVA NATURAL: MIRA LOS COLORES DESDE ARRIBA" }, { "caba", "LA CIUDAD DE BUENOS AIRES, TODA LA LUZ DEL ESTE" },
        { "mercado central", "EL MERCADO CENTRAL: POR ACA PASA LA COMIDA DE LA CIUDAD" }, { "sapucai", "UNA GRANJA EN EL CAMPO, A MIRAR LOS CULTIVOS" } };

    void DatosDeRuta(double lat, double lon, float alt, float rumbo, float vel, bool dest, double dla, double dlo)
    {
        if (Time.unscaledTime < proxDato || Time.unscaledTime < mensajeHasta) return;
        proxDato = Time.unscaledTime + R(16f, 24f);
        string pn; double pk;
        if (Pueblos.ProximoAdelante(lat, lon, rumbo, out pn, out pk) && pn != ultimoLugar)
        {
            ultimoLugar = pn; string pl = pn.ToLowerInvariant(), hecho = "";
            for (int i = 0; i < HECHOS.GetLength(0); i++) if (pl.Contains(HECHOS[i, 0])) { hecho = HECHOS[i, 1]; break; }
            Aviso("ADELANTE: " + pn.ToUpper() + ", A " + pk.ToString("F0", CultureInfo.InvariantCulture) + " KM." + (hecho != "" ? " " + hecho : ""), 8f); return;
        }
        float kmh = vel * 3.6f;
        if (!avisoSonico && kmh > 1200f) { avisoSonico = true; Aviso("TORRE: PASASTE LOS 1200 KM/H, CASI MACH 1", 6f); return; }
        if (!avisoAltura && alt > 5000f) { avisoAltura = true; Aviso("TORRE: AIRE FINO ARRIBA DE LOS 5000 M", 6f); return; }
        if (dest && vel > 30f)
        {
            double km = Dist(lat, lon, dla, dlo) / 1000.0; int min = Mathf.RoundToInt((float)(km * 1000.0 / vel / 60.0));
            Aviso("FALTAN " + km.ToString("F0", CultureInfo.InvariantCulture) + " KM AL DESTINO. UNOS " + min + " MIN A ESTA VELOCIDAD", 6f); return;
        }
        Aviso(CHARLAS[UnityEngine.Random.Range(0, CHARLAS.Length)], 6f);
    }

    // ------------------------------------------------------------------ ciclo
    void Update()
    {
        Preparar(); VozTick(); if (miEsc && (!abierta || lateralAbierto)) { Controles.escribiendo = false; miEsc = false; }
        audio.volume = 0.5f * Mezcla.De(Mezcla.FX); laserSrc.volume = 0.8f * Mezcla.De(Mezcla.FX);
        if (pendClip != null && Time.unscaledTime >= pendHasta) { var pc = pendClip; pendClip = null; Reproducir(pc, pendVol); }
        if (bancoMonto > 0 && Time.unscaledTime >= bancoFin) { Carrera.Pagar(bancoMonto, false); bancoMonto = 0; Clips(); if (cobroB != null) Reproducir(cobroB, 0.9f); }
        { string dnn; double q1, q2; string nd = Carrera.Destino(out dnn, out q1, out q2) ? dnn : ""; if (nd != ultDest) { if (nd != "" && subtotal > 0) { Carrera.Pagar(subtotal, false); Aviso("ACUMULADO ANTERIOR: $" + subtotal + " PASADO A TU CUENTA", 5f); subtotal = 0; } if (nd != "") { if (DecirRumbo(nd)) Decir("viaje_nuevo", 0.3f); else Decir("viaje_nuevo"); ReiniciarAproximacion(); } ultDest = nd; } }
        if (avion == null) avion = FindFirstObjectByType<AvionDeCarton>(); if (cam == null) cam = Camera.main;
        if (avion == null || AvionDeCarton.enAuto) return;
        double lon, lat; float alt, rumbo;
        if (!MapaAuto.PosActiva(out lon, out lat, out alt, out rumbo)) return;
        float vel = AvionDeCarton.VelMs, ahora = Time.unscaledTime;
        bool volando = !avion.EnTierra && vel > 15f;
        UpdateOrdenes(lat, lon, alt, rumbo, vel, volando);
        TickCombustible(); TickToque();

        // carteles: miran a la camara; de lejos se agrandan para verse, de cerca miden justo
        if (cam != null)
            foreach (var p in premios)
            {
                Vector3 d = p.quad.position - cam.transform.position; float dist = d.magnitude;
                if (dist > 1f) p.quad.rotation = Quaternion.LookRotation(d, cam.transform.up);
                float lado = Mathf.Max(RADIO_PREMIO * 0.75f, dist * 0.0125f) * (p.tipo == Tipo.Jefe ? 1.7f : 1f); p.quad.localScale = new Vector3(lado, lado, 1f);
                if (p.haloMat != null) { if (p.luz != null) p.luz.intensity = 40f * Aeropuertos.factorNoche * Mathf.Clamp01(dist / 400f + 0.3f); float ah = 0.35f + 0.65f * Aeropuertos.factorNoche; float imp = 1f + 14f * Aeropuertos.factorNoche; /* de noche la exposicion del post-proceso baja ~25 veces: se compensa con color HDR */ var hc = new Color(p.haloCol.r * imp, p.haloCol.g * imp, p.haloCol.b * imp, ah); p.haloMat.SetColor("_BaseColor", hc); p.haloMat.SetColor("_Color", hc); if (p.aroMat != null) { var ac = new Color(p.haloCol.r * imp, p.haloCol.g * imp, p.haloCol.b * imp, 1f); p.aroMat.SetColor("_BaseColor", ac); p.aroMat.SetColor("_Color", ac); } }
                if (p.lab != null)   // numerito flotante arriba: se ve de lejos y se apaga suave al acercarse (ahi manda el del centro)
                {
                    float lw = Mathf.Max(lado * 1.2f, dist * 0.05f), lh = lw * 0.375f;
                    p.lab.localScale = new Vector3(lw / lado, lh / lado, 1f); p.lab.localPosition = new Vector3(0f, 0.6f + (lh / lado) * 0.5f, 0f);
                    float al = Mathf.SmoothStep(0f, 0.9f, Mathf.InverseLerp(500f, 1800f, dist)); bool ver = al > 0.02f;
                    if (p.lab.gameObject.activeSelf != ver) p.lab.gameObject.SetActive(ver);
                    if (ver) { var cl = new Color(1f, 1f, 1f, al); p.labMat.SetColor("_BaseColor", cl); p.labMat.SetColor("_Color", cl); }
                }
            }

        Guerra.bonusMira = bonusOn;
        if (!bonusOn) { if (premios.Count > 0) QuitarTodos(); hayA = false; return; }
        if (racha > 0 && ahora > comboHasta) racha = 0;

        // cobro por pasada (celestes): se mira el tramo recorrido desde el cuadro anterior (a 1500 km/h igual no se "salta" el premio)
        if (hayA)
        {
            double cosL = System.Math.Cos(lat * System.Math.PI / 180.0);
            for (int i = premios.Count - 1; i >= 0; i--)
            {
                var p = premios[i]; if (p.tipo != Tipo.Celeste && p.tipo != Tipo.Combustible) continue;
                double dh = DistSegmento(aLat, aLon, lat, lon, p.lat, p.lon, cosL);
                double dv = System.Math.Min(System.Math.Abs(alt - p.alt), System.Math.Abs(aAlt - p.alt));
                if (System.Math.Sqrt(dh * dh + dv * dv) < RADIO_PREMIO || (dh < RADIO_PREMIO && dv < RADIO_PREMIO)) Cobrar(p, "COBRADO");
            }
        }
        aLat = lat; aLon = lon; aAlt = alt; hayA = true;

        // disparo (rojos y jefes): rayo desde la cruz de la mira
        if (cam != null && premios.Count > 0 && Controles.Flanco("armamento") && ahora >= proxDisparo)
        {
            proxDisparo = ahora + 0.25f; SonarDisparo();
            Ray ry = cam.ScreenPointToRay(new Vector2(Screen.width * 0.5f + Guerra.miraPos.x, Screen.height - (Screen.height * 0.5f + Guerra.miraPos.y)));
            Vector3 dir = ry.direction.normalized; Premio mejor = null; float mejorAv = 1e9f;
            foreach (var p in premios)
            {
                if (p.tipo == Tipo.Celeste) continue;
                float av =Vector3.Dot(p.quad.position - ry.origin, dir); if (av <= 0f || av > 9000f) continue;
                float perp = (ry.origin + dir * av - p.quad.position).magnitude, tol = Mathf.Max(45f, av * 0.014f) * (p.tipo == Tipo.Jefe ? 1.8f : 1f);
                if (perp < tol && av < mejorAv) { mejorAv = av; mejor = p; }
            }
            if (mejor != null) Golpe(mejor);
        }

        // limpieza: premios viejos o que ya quedaron lejos atras
        for (int i = premios.Count - 1; i >= 0; i--)
        {
            var p = premios[i]; double dd = Dist(lat, lon, p.lat, p.lon);
            bool atras = Mathf.Abs(Mathf.DeltaAngle(rumbo, Rumbo(lat, lon, p.lat, p.lon))) > 110f;
            if (ahora - p.nace > 240f || (atras && dd > 2500.0)) Quitar(p);
        }

        // oleada nueva: volando, sin llenar de premios, y lejos del destino (si hay)
        string dn; double dla, dlo; bool conDestino = Carrera.Destino(out dn, out dla, out dlo);
        if (volando && ahora >= proxSpawn)
        {
            float adelante = Mathf.Clamp(vel * 30f, 2500f, 20000f);
            bool lejos = !conDestino || Dist(lat, lon, dla, dlo) > adelante + 4000f;
            if ((Cuenta(Tipo.Celeste) == 0 || Cuenta(Tipo.Rojo) < RojosMax()) && premios.Count < maxSim + 4 && lejos && !Aproximando) { Oleada(lat, lon, alt, rumbo, vel); proxSpawn = ahora + R(3f, 6f); }
            else proxSpawn = ahora + 4f;
        }

        // el premio mas cercano (adelante) es el que muestra el tablero
        Premio cer = null; double cerD = 1e18;
        foreach (var p in premios) { double dd = Dist(lat, lon, p.lat, p.lon); if (Mathf.Abs(Mathf.DeltaAngle(rumbo, Rumbo(lat, lon, p.lat, p.lon))) > 100f) dd += 1e6; if (dd < cerD) { cerD = dd; cer = p; } }
        premio = cer;

        // resumen periodico del mas cercano; si no, datos de la ruta
        if (cer != null && ahora >= proxResumen && ahora > mensajeHasta)
        {
            proxResumen = ahora + 9f; double km; string h = Hora(lat, lon, rumbo, cer, out km);
            Aviso((cer.tipo == Tipo.Combustible ? "BIDÓN DE COMBUSTIBLE" : cer.tipo == Tipo.Celeste ? "CELESTE" : (cer.tipo == Tipo.Rojo ? (cer.verde ? "VERDE" : "ROJO") : "CHANCHA NEGRA")) + (cer.tipo == Tipo.Combustible ? " DE " + cer.valor + " L" : " DE $" + cer.valor) + " A LAS " + h + ", " + km.ToString("F1", CultureInfo.InvariantCulture) + " KM" + (cer.tipo != Tipo.Celeste && cer.tipo != Tipo.Combustible ? ". ALTURA " + Mathf.RoundToInt((float)cer.alt) + " M" : ""), 5f);
        }
        if (volando) DatosDeRuta(lat, lon, alt, rumbo, vel, conDestino, dla, dlo);
        if (conDestino && volando && !avisoCerca && Dist(lat, lon, dla, dlo) < 8000.0) { avisoCerca = true; Aviso("A 8 KM DE " + dn.ToUpper() + ". PREPARATE PARA ATERRIZAR", 8f); }
        if (!conDestino) avisoCerca = false;
    }


    // ------------------------------------------------------------------ acumulado del viaje y monedas que vuelan a la cuenta
    int subtotal; class Moneda { public Vector2 a, c, b; public float t0, dur; }
    readonly List<Moneda> monedas = new List<Moneda>(); int bancoMonto; float bancoFin; Rect lastRp = new Rect(300f, 140f, 470f, 244f); float lastEsc = 1f; Texture2D texMoneda; string ultDest = ""; AudioClip clipNivel;
    void Clips() { if (acierto == null) acierto = Resources.Load<AudioClip>("sonidos/acierto_rojo"); if (cobroB == null) { cobroA = Resources.Load<AudioClip>("sonidos/cobro_a"); cobroB = Resources.Load<AudioClip>("sonidos/cobro_b"); } }
    public static void Sonar(string recurso, float vol = 0.9f)   // reproduce un sonido de Resources/sonidos por el canal FX
    {
        if (inst == null) return; var c = Resources.Load<AudioClip>("sonidos/" + recurso); if (c != null) inst.Reproducir(c, vol);
    }
    public static void NivelCompleto(float retraso = 0f)   // sonido de nivel completo: fin de mision de guerra y aterrizaje de viaje/mision
    {
        if (inst == null) return;
        if (retraso > 0f) { inst.StartCoroutine(inst.NivelDespues(retraso)); return; }
        if (inst.clipNivel == null) inst.clipNivel = Resources.Load<AudioClip>("sonidos/nivel_completo");
        if (inst.clipNivel != null) inst.Reproducir(inst.clipNivel, 0.9f);
    }
    // al tocar la pista: primero el frenado; unos segundos despues (mientras carretea) el pago, las monedas y la voz
    void AlAterrizar() { paAlt = paVel = paRum = false; orden = null; consulta = null; ReiniciarAproximacion(); Sonar("frenada", 0.9f); StartCoroutine(PagoDespues(6.5f)); }
    System.Collections.IEnumerator PagoDespues(float seg)
    {
        yield return new WaitForSecondsRealtime(seg);
        Banco(new Vector2(lastRp.center.x, lastRp.center.y)); Decir("viaje_completo"); Decir("cuenta_ok", 0.4f);
    }
    System.Collections.IEnumerator NivelDespues(float seg)
    {
        yield return new WaitForSecondsRealtime(seg);
        if (clipNivel == null) clipNivel = Resources.Load<AudioClip>("sonidos/nivel_completo");
        if (clipNivel != null) Reproducir(clipNivel, 0.9f);
    }
    void OnDestroy() { Carrera.AlLlegar -= AlAterrizar; }
    // pasa todo el acumulado a la cuenta: salen monedas del cuadro hacia la cajita DINERO y al llegar sube el saldo
    void Banco(Vector2 origen)
    {
        if (subtotal <= 0) return;
        Clips(); bancoMonto += subtotal; Aviso("PASASTE $" + subtotal + " A TU CUENTA", 4f); subtotal = 0;
        Vector2 hasta = new Vector2(411f, 53f); float t0 = Time.unscaledTime; int n = 14;
        for (int i = 0; i < n; i++)
        {
            var a = origen + new Vector2(R(-40f, 40f), R(-10f, 10f));
            monedas.Add(new Moneda { a = a, b = hasta, c = (a + hasta) * 0.5f + new Vector2(R(-120f, 120f), R(-170f, -40f)), t0 = t0 + i * 0.06f, dur = 0.8f });
        }
        bancoFin = t0 + n * 0.06f + 0.8f;
        if (cobroB != null) Reproducir(cobroB, 0.9f);
    }
    void DibujarMonedas()
    {
        if (monedas.Count == 0) return;
        if (texMoneda == null)
        {
            const int n = 32; texMoneda = new Texture2D(n, n, TextureFormat.RGBA32, false); var px = new Color32[n * n];
            for (int y = 0; y < n; y++) for (int x = 0; x < n; x++)
            {
                float d = Mathf.Sqrt((x - 15.5f) * (x - 15.5f) + (y - 15.5f) * (y - 15.5f)); float a = Mathf.Clamp01(15.5f - d + 0.5f);
                Color c = d > 12f ? new Color(0.75f, 0.5f, 0.05f) : (d < 5f ? new Color(1f, 0.95f, 0.5f) : new Color(1f, 0.8f, 0.15f));
                px[y * n + x] = new Color32((byte)(c.r * 255), (byte)(c.g * 255), (byte)(c.b * 255), (byte)(a * 255));
            }
            texMoneda.SetPixels32(px); texMoneda.Apply();
        }
        float ahora = Time.unscaledTime; var c0 = GUI.color; GUI.color = Color.white;
        for (int i = monedas.Count - 1; i >= 0; i--)
        {
            var m = monedas[i]; float t = (ahora - m.t0) / m.dur; if (t < 0f) continue; if (t >= 1f) { monedas.RemoveAt(i); continue; }
            float e = t * t * (3f - 2f * t); Vector2 p = (1f - e) * (1f - e) * m.a + 2f * (1f - e) * e * m.c + e * e * m.b;
            float w = 24f * (0.3f + 0.7f * Mathf.Abs(Mathf.Cos(t * 14f)));
            GUI.DrawTexture(new Rect(p.x - w * 0.5f, p.y - 12f, w, 24f), texMoneda);
        }
        GUI.color = c0;
    }

    // ------------------------------------------------------------------ posicion (se arrastra con el mouse y se clava)
    [System.Serializable] class PosJ { public float x = 0.5f, y = 0.4f, w = 470f, h = 600f; public bool fijo; public bool abierta = true; public int v; }
    const float DH = 780f; float fw = 470f, fh = 700f; int resizando; Rect rIni; Vector2 mIni; float fx = 0.5f, fy = 0.4f; bool fijo, arrastrando, posCargada; Vector2 offArr;
    static string RutaPos { get { return System.IO.Path.GetFullPath(System.IO.Path.Combine(Application.dataPath, "..", "torre_pos.json")); } }
    void CargarPos()
    {
        posCargada = true;
        try { if (System.IO.File.Exists(RutaPos)) { var j = JsonUtility.FromJson<PosJ>(System.IO.File.ReadAllText(RutaPos)); if (j != null) { fx = Mathf.Clamp01(j.x); fy = Mathf.Clamp01(j.y); if (j.w > 100f) fw = j.w; if (j.h > 100f && j.v >= 6) fh = j.h; else fh = Mathf.Max(Screen.height - 20f, 700f); fijo = j.fijo; abierta = j.abierta; } } } catch { }
    }
    void GuardarPos() { try { System.IO.File.WriteAllText(RutaPos, JsonUtility.ToJson(new PosJ { x = fx, y = fy, w = fw, h = fh, fijo = fijo, abierta = abierta, v = 6 })); } catch { } }
    Vector2 MouseGui() { var m = UnityEngine.InputSystem.Mouse.current; if (m == null) return Vector2.zero; var p = m.position.ReadValue(); return new Vector2(p.x, Screen.height - p.y); }
    float finDiseno;   // donde termina de verdad el contenido (lo mide OrdenesGUI): el panel se ajusta para que no se corte nada abajo
    float DisenoAlto { get { return Mathf.Max(finDiseno + (Tablet.Acoplada ? Tablet.RESERVA : 0f), Tablet.Acoplada ? 700f : 420f); } }
    float Escala() { return Mathf.Max(0.3f, Mathf.Min(fh / DisenoAlto, fw / 380f)); }
    Rect PanelRect()
    {
        float W = Screen.width, H = Screen.height; fw = Mathf.Clamp(fw, 230f, Mathf.Max(230f, W - 10f)); fh = Mathf.Clamp(fh, 110f, Mathf.Max(110f, H - 10f));
        float w = fw, h = abierta ? fh : 32f * Escala();
        return new Rect(fx * (W - w), fy * (H - h), w, h);
    }
    void LateUpdate()
    {
        // arrastre/redimension: se sostienen mirando el mouse cada cuadro (si se suelta fuera de la ventana, igual termina)
        if (!arrastrando && resizando == 0) return;
        var m = UnityEngine.InputSystem.Mouse.current;
        if (m == null || !m.leftButton.isPressed) { arrastrando = false; resizando = 0; GuardarPos(); return; }
        float W = Screen.width, H = Screen.height;
        if (resizando != 0)
        {
            Vector2 dm = MouseGui() - mIni; float x0 = rIni.x, y0 = rIni.y, x1 = rIni.xMax, y1 = rIni.yMax;
            if ((resizando & 1) != 0) x0 = Mathf.Clamp(rIni.x + dm.x, 0f, x1 - 230f);
            if ((resizando & 2) != 0) x1 = Mathf.Clamp(rIni.xMax + dm.x, x0 + 230f, W);
            if ((resizando & 4) != 0) y0 = Mathf.Clamp(rIni.y + dm.y, 0f, y1 - 110f);
            if ((resizando & 8) != 0) y1 = Mathf.Clamp(rIni.yMax + dm.y, y0 + 110f, H);
            fw = x1 - x0; fh = y1 - y0; fx = Mathf.Clamp01(x0 / Mathf.Max(1f, W - fw)); fy = Mathf.Clamp01(y0 / Mathf.Max(1f, H - fh));
            return;
        }
        var r = PanelRect(); Vector2 p = MouseGui() - offArr;
        fx = Mathf.Clamp01(p.x / Mathf.Max(1f, W - r.width)); fy = Mathf.Clamp01(p.y / Mathf.Max(1f, H - r.height));
    }

    // ------------------------------------------------------------------ numeros de 7 segmentos (estilo tablero de avion)
    static readonly int[] MASK = { 0x3F, 0x06, 0x5B, 0x4F, 0x66, 0x6D, 0x7D, 0x07, 0x7F, 0x6F };   // a=1 b=2 c=4 d=8 e=16 f=32 g=64
    static float AnchoSeg(string t, float h) { float w = h * 0.52f, g = h * 0.2f, tot = 0f; foreach (char c in t) tot += (c == '.' ? h * 0.16f : w) + g; return tot; }
    static void Rc(float x, float y, float w, float h, Color c) { var o = GUI.color; GUI.color = c; GUI.DrawTexture(new Rect(x, y, w, h), Texture2D.whiteTexture); GUI.color = o; }
    static void Digito(float x, float y, float w, float h, int mask, Color on)
    {
        float th = Mathf.Max(2f, h * 0.12f), mid = h * 0.5f; var off = new Color(1f, 1f, 1f, 0.07f); float vh = mid - th * 1.5f;
        Rc(x + th, y, w - 2f * th, th, (mask & 1) != 0 ? on : off);                       // a
        Rc(x + w - th, y + th, th, vh, (mask & 2) != 0 ? on : off);                       // b
        Rc(x + w - th, y + mid + th * 0.5f, th, vh, (mask & 4) != 0 ? on : off);          // c
        Rc(x + th, y + h - th, w - 2f * th, th, (mask & 8) != 0 ? on : off);              // d
        Rc(x, y + mid + th * 0.5f, th, vh, (mask & 16) != 0 ? on : off);                  // e
        Rc(x, y + th, th, vh, (mask & 32) != 0 ? on : off);                               // f
        Rc(x + th, y + mid - th * 0.5f, w - 2f * th, th, (mask & 64) != 0 ? on : off);    // g
    }
    static float Seg7(float x, float y, float h, string t, Color on)
    {
        float w = h * 0.52f, g = h * 0.2f;
        foreach (char c in t)
        {
            if (c == '.') { Rc(x + h * 0.02f, y + h - h * 0.12f, h * 0.12f, h * 0.12f, on); x += h * 0.16f + g; continue; }
            int m = c >= '0' && c <= '9' ? MASK[c - '0'] : (c == '-' ? 64 : 0);
            Digito(x, y, w, h, m, on); x += w + g;
        }
        return x;
    }
    static void Seg7Der(float xDer, float y, float h, string t, Color on) { Seg7(xDer - AnchoSeg(t, h) + h * 0.2f, y, h, t, on); }
    static string Rel(double v, int dec) { return v.ToString("F" + dec, CultureInfo.InvariantCulture); }

    string Recortar(string t, GUIStyle st, float ancho)
    {
        if (st.CalcSize(new GUIContent(t)).x <= ancho) return t;
        while (t.Length > 3 && st.CalcSize(new GUIContent(t + "...")).x > ancho) t = t.Substring(0, t.Length - 1);
        return t + "...";
    }

    void OnGUI()
    {
        Tablet.Hueco = new Rect(0, 0, 0, 0);   // se vuelve a llenar mas abajo si la torre esta a la vista
        if (avion == null || AvionDeCarton.enAuto) return;
        DibujarMonedas();
        if (lateralAbierto) { arrastrando = false; resizando = 0; return; }   // con el panel grande de la derecha abierto (Destinos, etc.) la torre se esconde para no taparlo
        if (!posCargada) CargarPos();
        if (sTit == null)
        {
            fuente = Font.CreateDynamicFontFromOSFont(new[] { "Bahnschrift", "Segoe UI", "Arial" }, 14);
            sBtn = new GUIStyle(GUI.skin.button) { font = fuente, fontSize = 12, fontStyle = FontStyle.Bold };
            sTit = new GUIStyle(GUI.skin.label) { font = fuente, fontSize = 15, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleLeft }; sTit.normal.textColor = Color.white;
            sEtq = new GUIStyle(GUI.skin.label) { font = fuente, fontSize = 10, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleLeft, clipping = TextClipping.Clip }; sEtq.normal.textColor = new Color(0.55f, 0.62f, 0.7f);
            sMsg = new GUIStyle(GUI.skin.label) { font = fuente, fontSize = 14, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleLeft, clipping = TextClipping.Clip }; sMsg.normal.textColor = new Color(0.45f, 1f, 0.7f);
        }
        var rp = PanelRect(); float esc = Escala(); lastRp = rp; lastEsc = esc;
        { float ty = rp.y + (finDiseno + 6f) * esc; Tablet.Hueco = abierta ? new Rect(rp.x + 8f * esc, ty, rp.width - 16f * esc, rp.yMax - ty - 8f * esc) : new Rect(0, 0, 0, 0); }
        Color borde = fijo ? new Color(0.2f, 0.75f, 0.95f, 0.6f) : new Color(1f, 0.8f, 0.1f, 1f);
        Rc(rp.x, rp.y, rp.width, rp.height, new Color(0f, 0f, 0f, 0.93f));
        Rc(rp.x, rp.y, rp.width, 1f, borde); Rc(rp.x, rp.yMax - 1f, rp.width, 1f, borde); Rc(rp.x, rp.y, 1f, rp.height, borde); Rc(rp.xMax - 1f, rp.y, 1f, rp.height, borde);
        // contenido en un espacio virtual escalado: los numeros se adaptan al tamano del cuadro
        var mat0 = GUI.matrix; GUI.matrix = mat0 * Matrix4x4.TRS(new Vector3(rp.x, rp.y, 0f), Quaternion.identity, new Vector3(esc, esc, 1f));
        Contenido(new Rect(0f, 0f, rp.width / esc, rp.height / esc), borde);
        GUI.matrix = mat0;
        // arrastrar desde el cuerpo / redimensionar desde bordes y vertices (solo si no esta clavado)
        var e = Event.current;
        if (!fijo)
        {
            float g = 9f; Vector2 mp = e.mousePosition; int zona = 0;
            if (rp.Contains(mp))
            {
                if (mp.x < rp.x + g) zona |= 1; if (mp.x > rp.xMax - g) zona |= 2;
                if (abierta) { if (mp.y < rp.y + g) zona |= 4; if (mp.y > rp.yMax - g) zona |= 8; }
            }
            if (abierta) { Color vc = new Color(1f, 0.8f, 0.1f, 1f); float k = 12f; Rc(rp.x, rp.y, k, 2f, vc); Rc(rp.x, rp.y, 2f, k, vc); Rc(rp.xMax - k, rp.y, k, 2f, vc); Rc(rp.xMax - 2f, rp.y, 2f, k, vc); Rc(rp.x, rp.yMax - 2f, k, 2f, vc); Rc(rp.x, rp.yMax - k, 2f, k, vc); Rc(rp.xMax - k, rp.yMax - 2f, k, 2f, vc); Rc(rp.xMax - 2f, rp.yMax - k, 2f, k, vc); }
            if (e.type == EventType.MouseDown && e.button == 0 && rp.Contains(mp))
            {
                if (zona != 0) { resizando = zona; rIni = rp; mIni = MouseGui(); e.Use(); }
                else { arrastrando = true; offArr = mp - new Vector2(rp.x, rp.y); e.Use(); }
            }
        }
    }

    // ---- app Instrumentos de la tablet: las mismas filas que antes estaban arriba del panel
    public static void AppInstrumentos(Rect area) { if (inst != null) inst.FilasInstrumentos(area); }
    void FilasInstrumentos(Rect r)
    {
        if (sEtq == null || sTit == null) { GUI.Label(r, "Sin datos todavia"); return; }
        double lon, lat; float alt, rumbo; if (!MapaAuto.PosActiva(out lon, out lat, out alt, out rumbo)) { GUI.Label(r, "Sin posicion"); return; }
        string dn; double dla, dlo; bool dest = Carrera.Destino(out dn, out dla, out dlo);
        Color blanco = new Color(0.95f, 0.97f, 1f), verde = new Color(0.35f, 1f, 0.55f), cian = new Color(0.3f, 0.9f, 1f), ambar = new Color(1f, 0.78f, 0.2f);
        float cw = (r.width - 20f) / 3f, x0 = r.x + 10f, y0 = r.y + 6f;
        // fila 1: altura, velocidad, rumbo
        GUI.Label(new Rect(x0, y0, cw, 14f), "ALTURA  M", sEtq); GUI.Label(new Rect(x0 + cw, y0, cw, 14f), "VELOCIDAD  KM/H", sEtq); GUI.Label(new Rect(x0 + 2f * cw, y0, cw, 14f), "RUMBO  GRADOS", sEtq);
        Seg7(x0, y0 + 16f, 34f, Mathf.RoundToInt(alt).ToString(), blanco);
        FlechasAltura(x0 + cw - 30f, y0 + 17f, 20f, 13f);   // triangulitos: rojo = corregi la altura, verde = bien
        Seg7(x0 + cw, y0 + 16f, 34f, Mathf.RoundToInt(AvionDeCarton.VelMs * 3.6f).ToString(), verde);
        Seg7(x0 + 2f * cw, y0 + 16f, 34f, Mathf.RoundToInt(rumbo).ToString("000"), cian);
        // fila 2: destino y kilometros
        float y1 = y0 + 62f;
        GUI.Label(new Rect(x0, y1, 2f * cw, 14f), "DESTINO", sEtq); GUI.Label(new Rect(x0 + 2f * cw, y1, cw, 14f), "FALTAN  KM", sEtq);
        GUI.Label(new Rect(x0, y1 + 14f, 2f * cw - 8f, 28f), dest ? Recortar(dn.ToUpper(), sTit, 2f * cw - 12f) : "SIN DESTINO", sTit);
        Seg7(x0 + 2f * cw, y1 + 14f, 26f, dest ? Rel(Dist(lat, lon, dla, dlo) / 1000.0, 1) : "--.-", ambar);
    }

    void Contenido(Rect r, Color borde)
    {
        var bg = GUI.backgroundColor;

        // cabecera
        GUI.Label(new Rect(r.x + 10f, r.y + 4f, 90f, 24f), "TORRE", sTit);
        float bx = r.xMax - 8f;
        bx -= 30f; if (GUI.Button(new Rect(bx, r.y + 5f, 30f, 22f), abierta ? "-" : "+", sBtn)) { abierta = !abierta; GuardarPos(); }
        bx -= 88f; GUI.backgroundColor = fijo ? new Color(0.8f, 0.8f, 0.8f) : new Color(1f, 0.85f, 0.2f);
        if (GUI.Button(new Rect(bx, r.y + 5f, 84f, 22f), fijo ? "MOVER" : "CLAVAR", sBtn)) { fijo = !fijo; arrastrando = false; GuardarPos(); }
        bx -= 112f; GUI.backgroundColor = bonusOn ? new Color(0.3f, 1f, 0.45f) : new Color(0.75f, 0.75f, 0.75f);
        if (GUI.Button(new Rect(bx, r.y + 5f, 108f, 22f), bonusOn ? "BONUS: ON" : "BONUS: OFF", sBtn)) { bonusOn = !bonusOn; if (bonusOn) { racha = 0; proxSpawn = Time.unscaledTime + 4f; Aviso("BONUS ACTIVADO. CELESTE: PASA. ROJO: DISPARA", 6f); if (!Carrera.HayViaje) Decir("menu_destinos"); } else Aviso("BONUS APAGADO. VIAJE TRANQUILO", 4f); }
        GUI.backgroundColor = bg;
        bx -= 70f; if (GUI.Button(new Rect(bx, r.y + 7f, 18f, 18f), "-", sBtn)) maxSim = Mathf.Max(1, maxSim - 1);
        GUI.Label(new Rect(bx + 20f, r.y + 5f, 26f, 22f), maxSim.ToString(), sTit); if (GUI.Button(new Rect(bx + 48f, r.y + 7f, 18f, 18f), "+", sBtn)) maxSim = Mathf.Min(10, maxSim + 1);
        if (!fijo && bx - r.x - 100f > 120f) GUI.Label(new Rect(r.x + 74f, r.y + 5f, bx - r.x - 80f, 22f), "ARRASTRALO Y APRETA CLAVAR", sEtq);
        if (!abierta) return;
        Rc(r.x + 8f, r.y + 32f, r.width - 16f, 1f, new Color(0.2f, 0.75f, 0.95f, 0.35f));

        double lon, lat; float alt, rumbo; if (!MapaAuto.PosActiva(out lon, out lat, out alt, out rumbo)) return;
        string dn; double dla, dlo; bool dest = Carrera.Destino(out dn, out dla, out dlo);
        Color blanco = new Color(0.95f, 0.97f, 1f), verde = new Color(0.35f, 1f, 0.55f), cian = new Color(0.3f, 0.9f, 1f), ambar = new Color(1f, 0.78f, 0.2f);
        float cw = (r.width - 20f) / 3f, x0 = r.x + 10f, y0 = r.y + 38f;
        // (las filas de altura/velocidad/rumbo y destino ahora viven en la tablet: AppInstrumentos)
        float y1 = y0 - 46f;
        // fila 3: premio
        float y2 = y1 + 46f;
        GUI.Label(new Rect(x0, y2, cw, 14f), "ACUMULADO  $", sEtq); GUI.Label(new Rect(x0 + cw * 0.9f, y2, cw, 14f), "RELOJ", sEtq); GUI.Label(new Rect(x0 + cw * 1.45f, y2, cw, 14f), "KM", sEtq); GUI.Label(new Rect(x0 + cw * 1.95f, y2, cw, 14f), "EN PESOS $", sEtq);
        if (premio != null)
        {
            float rbtn = Rumbo(lat, lon, premio.lat, premio.lon), rel = Mathf.DeltaAngle(rumbo, rbtn); int hora = Mathf.RoundToInt(rel / 30f); hora = ((hora % 12) + 12) % 12; if (hora == 0) hora = 12;
            Seg7(x0 + cw * 0.9f, y2 + 15f, 24f, hora.ToString("00"), cian);
            Seg7(x0 + cw * 1.45f, y2 + 15f, 24f, Rel(Dist(lat, lon, premio.lat, premio.lon) / 1000.0, 1), blanco);
            // flecha hacia el premio, relativa al rumbo del avion
            var c = new Vector2(r.xMax - 26f, y2 + 26f); var m0 = GUI.matrix; var c0 = GUI.color;
            GUIUtility.RotateAroundPivot(rel, c); GUI.color = verde; GUI.DrawTexture(new Rect(c.x - 15f, c.y - 15f, 30f, 30f), texFlecha); GUI.matrix = m0; GUI.color = c0;
        }
        else { Seg7(x0 + cw * 0.9f, y2 + 15f, 24f, "--", new Color(0.5f, 0.55f, 0.6f)); Seg7(x0 + cw * 1.45f, y2 + 15f, 24f, "--.-", new Color(0.5f, 0.55f, 0.6f)); }
        Seg7(x0 + cw * 1.95f, y2 + 18f, 17f, (subtotal * Carrera.PESOS_POR_DOLAR).ToString(), verde);   // acumulado en pesos
        Seg7(x0, y2 + 15f, 24f, subtotal.ToString(), ambar);   // acumulado del viaje
        // mensaje de la torre + coordenadas del premio
        float y3 = y2 + 46f;
        Rc(x0, y3 - 2f, r.width - 20f, 1f, new Color(0.2f, 0.75f, 0.95f, 0.25f));
        string coords = premio != null ? "LAT " + Rel(premio.lat, 3) + "   LON " + Rel(premio.lon, 3) : "LAT " + Rel(lat, 3) + "   LON " + Rel(lon, 3);
        if (sMsgW == null) { sMsgW = new GUIStyle(sMsg) { fontSize = 13, wordWrap = true, clipping = TextClipping.Clip, alignment = TextAnchor.UpperLeft }; }
        GUI.Label(new Rect(x0, y3, r.width - 20f, 36f), mensaje, sMsgW);
        GUI.backgroundColor = subtotal > 0 ? new Color(1f, 0.85f, 0.2f) : new Color(0.5f, 0.5f, 0.5f);
        var rb = new Rect(x0, y3 + 38f, 200f, 20f);
        if (GUI.Button(rb, "PASAR $" + subtotal + " A MI CUENTA", sBtn) && subtotal > 0) Banco(new Vector2(lastRp.x + (rb.x + rb.width * 0.5f) * lastEsc, lastRp.y + (rb.y + 10f) * lastEsc));
        GUI.backgroundColor = bg;
        GUI.Label(new Rect(x0 + 208f, y3 + 40f, r.width - 228f, 14f), (premio != null ? "PREMIO  " : "TU POSICION  ") + coords, sEtq);
        OrdenesGUI(r, x0, y3 + 66f, ambar, verde, cian, blanco);
    }
}
