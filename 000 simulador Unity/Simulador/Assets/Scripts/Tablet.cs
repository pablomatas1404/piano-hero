// Tablet de cabina: pantalla de inicio con hora e iconos; cada icono abre lo mismo que la pestana correspondiente.
// Va acoplada abajo del panel de la Torre (Torre.cs le reserva el hueco) o suelta: se mueve y se agranda donde quieras.
// Un clic selecciona, doble clic abre, mantener apretado y arrastrar cambia el icono de lugar.
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class Tablet : MonoBehaviour
{
    public static bool Acoplada = true, Pausado;
    public static Rect Hueco;                // lo llena la Torre en cada cuadro: donde entra la tablet cuando esta acoplada
    public const float RESERVA = 470f;       // alto (en unidades de diseno de la Torre) que se le deja a la tablet
    static Rect ultimoRect; static bool visible;
    public static bool Sobre(Vector2 p) { return visible && ultimoRect.Contains(p); }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Crear() { new GameObject("Tablet").AddComponent<Tablet>(); }

    // ---------------------------------------------------------------- aplicaciones (una por pestana)
    public static bool Compacto;   // true mientras se dibuja una app dentro de la tablet: las filas anchas se achican
    static readonly string[] NOM = { "Instrumentos", "Radio COM", "Destinos", "Carrera", "Cercanos", "Shopping", "Maletin", "Luces", "Tren", "Guerra", "Controles", "Teletransporte", "Sonidos", "Sensibilidad", "Modelo", "Aviones" };
    // ancho virtual de cada app: la app se dibuja con ese ancho y se achica (o agranda) para entrar justo en la tablet
    static readonly float[] ANCHO_V = { 440, 440, 560, 560, 490, 520, 520, 520, 480, 520, 600, 460, 460, 460, 460, 620 };
    static readonly Color32[] COL = {
        new Color32(0, 150, 160, 255), new Color32(235, 130, 20, 255), new Color32(215, 50, 60, 255), new Color32(40, 110, 220, 255), new Color32(40, 160, 90, 255),
        new Color32(220, 60, 130, 255), new Color32(130, 90, 60, 255), new Color32(235, 190, 20, 255), new Color32(90, 100, 120, 255), new Color32(180, 40, 40, 255),
        new Color32(110, 80, 200, 255), new Color32(30, 170, 210, 255), new Color32(80, 90, 230, 255),
        new Color32(110, 170, 40, 255), new Color32(190, 110, 220, 255), new Color32(60, 130, 180, 255) };

    int app = -1, sel = -1; List<int> orden = new List<int>();
    Vector2[] scroll = new Vector2[NOM.Length]; float[] zoomAco = new float[NOM.Length], zoomLib = new float[NOM.Length], zoomTmp = new float[NOM.Length];
    float[] zoomApp { get { return Acoplada ? zoomAco : zoomLib; } }   // el zoom clavado de cada app es independiente: uno para acoplada y otro para suelta float homeScroll;
    float homeScroll;
    Texture2D[] ico; Texture2D marco, fondo, selTex, negro; bool cargado;
    GUIStyle sTxt, sBtn, sTit, sEtq, sHora, sBtnMini;

    // posicion y tamano cuando esta suelta (en pixeles de pantalla)
    float wx = -1f, wy = -1f, ww = 460f, wh = 620f;
    bool arrastrandoVentana, redimensionando; Vector2 offVentana, mIni; Rect rIni;
    // arrastre de iconos
    int icoPresionado = -1; Vector2 icoMouse0; bool icoArrastrando; Vector2 icoMouse;

    static string Ruta { get { return System.IO.Path.GetFullPath(System.IO.Path.Combine(Application.dataPath, "..", "tablet_pos.json")); } }
    [System.Serializable] class Pj { public bool acoplada = true; public float x = -1f, y = -1f, w = 460f, h = 620f; public int[] orden; public float[] zooms, zoomsLibre; }

    void Awake()
    {
        try
        {
            if (System.IO.File.Exists(Ruta))
            {
                var j = JsonUtility.FromJson<Pj>(System.IO.File.ReadAllText(Ruta));
                if (j != null) { Acoplada = j.acoplada; wx = j.x; wy = j.y; if (j.w > 200f) ww = j.w; if (j.h > 200f) wh = j.h; if (j.zooms != null) for (int q = 0; q < j.zooms.Length && q < zoomAco.Length; q++) if (j.zooms[q] > 0.3f) zoomAco[q] = j.zooms[q];
                    if (j.zoomsLibre != null) for (int q = 0; q < j.zoomsLibre.Length && q < zoomLib.Length; q++) if (j.zoomsLibre[q] > 0.3f) zoomLib[q] = j.zoomsLibre[q];
                    if (j.zoomsLibre == null) for (int q = 0; q < zoomLib.Length; q++) zoomLib[q] = zoomAco[q];   // primera vez: la suelta arranca con los mismos valores
                    if (j.orden != null) foreach (int o in j.orden) if (o >= 0 && o < NOM.Length && !orden.Contains(o)) orden.Add(o); }
            }
        }
        catch { }
        for (int i = 0; i < NOM.Length; i++) { if (!orden.Contains(i)) orden.Add(i); if (zoomAco[i] < 0.3f) zoomAco[i] = 1f; if (zoomLib[i] < 0.3f) zoomLib[i] = 1f; zoomTmp[i] = zoomApp[i]; }
    }
    void Guardar() { try { System.IO.File.WriteAllText(Ruta, JsonUtility.ToJson(new Pj { acoplada = Acoplada, x = wx, y = wy, w = ww, h = wh, orden = orden.ToArray(), zooms = zoomAco, zoomsLibre = zoomLib })); } catch { } }
    void OnDisable() { if (Pausado) { Pausado = false; Time.timeScale = 1f; } }
    void OnApplicationQuit() { Guardar(); }

    // el arrastre se sostiene mirando el mouse cada cuadro: si se suelta fuera de la ventana igual termina
    void LateUpdate()
    {
        var m = Mouse.current; bool apretado = m != null && m.leftButton.isPressed;
        if (!apretado)
        {
            if (arrastrandoVentana || redimensionando) Guardar();
            arrastrandoVentana = false; redimensionando = false;
            if (icoPresionado >= 0) { if (icoArrastrando) SoltarIcono(); icoPresionado = -1; icoArrastrando = false; }
            return;
        }
        Vector2 p = new Vector2(m.position.ReadValue().x, Screen.height - m.position.ReadValue().y); float W = Screen.width, H = Screen.height;
        if (arrastrandoVentana) { wx = Mathf.Clamp(p.x - offVentana.x, 0f, W - ww); wy = Mathf.Clamp(p.y - offVentana.y, 0f, H - wh); }
        else if (redimensionando) { Vector2 d = p - mIni; ww = Mathf.Clamp(rIni.width + d.x, 300f, W - wx); wh = Mathf.Clamp(rIni.height + d.y, 300f, H - wy); }
        else if (icoPresionado >= 0) { icoMouse = p; if (!icoArrastrando && (p - icoMouse0).magnitude > 8f) icoArrastrando = true; }
    }

    int slotDestino = -1; Rect areaIconos;
    void SoltarIcono()
    {
        if (slotDestino < 0) return;
        int desde = orden.IndexOf(icoPresionado); if (desde < 0) return;
        int a = icoPresionado; orden.RemoveAt(desde); orden.Insert(Mathf.Clamp(slotDestino, 0, orden.Count), a); sel = a; Guardar();
    }

    // ---------------------------------------------------------------- dibujo
    void Preparar()
    {
        if (cargado) return; cargado = true;
        marco = Resources.Load<Texture2D>("tablet/marco_apaisado");
        var f = Font.CreateDynamicFontFromOSFont(new[] { "Segoe UI", "Arial" }, 14);
        sTxt = new GUIStyle(GUI.skin.label) { font = f, fontSize = 13 }; sTxt.normal.textColor = Color.white;
        sTit = new GUIStyle(sTxt) { fontStyle = FontStyle.Bold }; sTit.normal.textColor = new Color(1f, 0.85f, 0.2f);
        sBtn = new GUIStyle(GUI.skin.button) { font = f, fontSize = 12 };
        sBtnMini = new GUIStyle(GUI.skin.button) { font = f, fontSize = 11, fontStyle = FontStyle.Bold };
        sEtq = new GUIStyle(sTxt) { fontSize = 12, fontStyle = FontStyle.Bold, alignment = TextAnchor.UpperCenter, clipping = TextClipping.Clip };
        sHora = new GUIStyle(sTxt) { fontSize = 14, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleLeft };
        negro = new Texture2D(1, 1); negro.SetPixel(0, 0, Color.white); negro.Apply();
        // fondo de pantalla: degradado azul noche
        fondo = new Texture2D(2, 128, TextureFormat.RGBA32, false); fondo.wrapMode = TextureWrapMode.Clamp;
        for (int y = 0; y < 128; y++) { float t = y / 127f; var c = Color.Lerp(new Color(0.02f, 0.05f, 0.14f), new Color(0.08f, 0.28f, 0.42f), t); for (int x = 0; x < 2; x++) fondo.SetPixel(x, 127 - y, c); }
        fondo.Apply();
        selTex = new Texture2D(96, 96, TextureFormat.RGBA32, false); selTex.wrapMode = TextureWrapMode.Clamp;
        for (int y = 0; y < 96; y++) for (int x = 0; x < 96; x++) { float d = Box(x, y, 48, 48, 46, 46, 20); float inner = Box(x, y, 48, 48, 41, 41, 16); selTex.SetPixel(x, 95 - y, new Color(1f, 1f, 1f, Mathf.Clamp01(d - inner))); }
        selTex.Apply();
        ico = new Texture2D[NOM.Length];
        for (int i = 0; i < NOM.Length; i++) ico[i] = CrearIcono(i);
    }

    // ---- iconos dibujados por codigo (anti-aliasing con distancias): cuadrado redondeado con degradado + dibujito blanco
    // triangulito (arriba/abajo) para los botones de plegar paneles
    static Texture2D triArr, triAba;
    public static Texture2D Triangulo(bool arriba)
    {
        if (arriba ? triArr != null : triAba != null) return arriba ? triArr : triAba;
        var t = new Texture2D(32, 32, TextureFormat.RGBA32, false); t.wrapMode = TextureWrapMode.Clamp;
        for (int y = 0; y < 32; y++) for (int x = 0; x < 32; x++)
        {
            float c = arriba ? Tri(x, y, 16, 7, 4, 24, 28, 24) : Tri(x, y, 4, 8, 28, 8, 16, 25);
            t.SetPixel(x, 31 - y, new Color(1f, 1f, 1f, c));
        }
        t.Apply(); if (arriba) triArr = t; else triAba = t; return t;
    }
    static float Cov(float d) { return Mathf.Clamp01(0.5f - d); }
    static float Disc(float x, float y, float cx, float cy, float r) { return Cov(Mathf.Sqrt((x - cx) * (x - cx) + (y - cy) * (y - cy)) - r); }
    static float Ring(float x, float y, float cx, float cy, float r, float w) { return Cov(Mathf.Abs(Mathf.Sqrt((x - cx) * (x - cx) + (y - cy) * (y - cy)) - r) - w * 0.5f); }
    static float Box(float x, float y, float cx, float cy, float hw, float hh, float rad)
    {
        float dx = Mathf.Abs(x - cx) - hw + rad, dy = Mathf.Abs(y - cy) - hh + rad;
        float d = Mathf.Sqrt(Mathf.Max(dx, 0f) * Mathf.Max(dx, 0f) + Mathf.Max(dy, 0f) * Mathf.Max(dy, 0f)) + Mathf.Min(Mathf.Max(dx, dy), 0f) - rad; return Cov(d);
    }
    static float Seg(float x, float y, float ax, float ay, float bx, float by, float w)
    {
        float px = x - ax, py = y - ay, dx = bx - ax, dy = by - ay; float t = Mathf.Clamp01((px * dx + py * dy) / (dx * dx + dy * dy));
        float qx = px - dx * t, qy = py - dy * t; return Cov(Mathf.Sqrt(qx * qx + qy * qy) - w * 0.5f);
    }
    static float Edge(float x, float y, float ax, float ay, float bx, float by, float s) { float dx = bx - ax, dy = by - ay; float l = Mathf.Sqrt(dx * dx + dy * dy); return s * ((dx) * (y - ay) - (dy) * (x - ax)) / l; }
    static float Tri(float x, float y, float ax, float ay, float bx, float by, float cx, float cy)
    {
        float s = Mathf.Sign((bx - ax) * (cy - ay) - (by - ay) * (cx - ax));
        float m = Mathf.Min(Edge(x, y, ax, ay, bx, by, s), Mathf.Min(Edge(x, y, bx, by, cx, cy, s), Edge(x, y, cx, cy, ax, ay, s))); return Mathf.Clamp01(m + 0.5f);
    }
    // dibujito de cada icono: devuelve cuanto cubre en blanco y (cut) cuanto se "recorta" para dejar ver el color de fondo
    static float Glifo(int id, float x, float y, out float cut)
    {
        cut = 0f; float c = 0f;
        switch (id)
        {
            case 0: c = Mathf.Max(Ring(x, y, 48, 52, 28, 6) * (y < 54f ? 1f : 0.35f), Seg(x, y, 48, 52, 66, 34, 6), Disc(x, y, 48, 52, 6)); break;                                                       // instrumentos
            case 1: { float a = 0f; for (int k = 1; k <= 3; k++) a = Mathf.Max(a, Ring(x, y, 30, 66, 14f * k, 5f) * ((x > 30f && y < 66f) ? 1f : 0f)); c = Mathf.Max(a, Disc(x, y, 30, 66, 6)); break; }      // radio
            case 2: c = Mathf.Max(Disc(x, y, 48, 40, 22), Tri(x, y, 28, 50, 68, 50, 48, 80)); cut = Disc(x, y, 48, 40, 9); break;                                                                    // destinos
            case 3: c = Mathf.Max(Seg(x, y, 32, 20, 32, 78, 6), Tri(x, y, 34, 24, 74, 36, 34, 50)); break;                                                                                         // carrera
            case 4: c = Mathf.Max(Ring(x, y, 48, 48, 30, 5), Ring(x, y, 48, 48, 16, 5), Seg(x, y, 48, 48, 72, 26, 5), Disc(x, y, 48, 48, 4)); break;                                                // cercanos
            case 5: c = Mathf.Max(Box(x, y, 48, 58, 26, 20, 6), Ring(x, y, 48, 38, 11, 5) * (y < 42f ? 1f : 0f)); cut = 0f; break;                                                                    // shopping
            case 6: c = Mathf.Max(Box(x, y, 48, 56, 30, 20, 6), Seg(x, y, 38, 34, 58, 34, 5), Seg(x, y, 38, 34, 38, 40, 5), Seg(x, y, 58, 34, 58, 40, 5)); cut = Seg(x, y, 18, 56, 78, 56, 3); break;     // maletin
            case 7: c = Mathf.Max(Disc(x, y, 48, 42, 20), Box(x, y, 48, 70, 10, 6, 2), Seg(x, y, 48, 12, 48, 18, 4), Seg(x, y, 21, 24, 26, 29, 4), Seg(x, y, 75, 24, 70, 29, 4)); break;               // luces
            case 8: c = Mathf.Max(Ring(x, y, 48, 54, 20, 12), Seg(x, y, 48, 14, 48, 30, 6), Disc(x, y, 48, 54, 4)); break;                                                                         // tren
            case 9: c = Mathf.Max(Ring(x, y, 48, 48, 24, 5), Seg(x, y, 48, 16, 48, 36, 4), Seg(x, y, 48, 60, 48, 80, 4), Seg(x, y, 16, 48, 36, 48, 4), Seg(x, y, 60, 48, 80, 48, 4)); break;         // guerra
            case 10: c = Box(x, y, 48, 52, 34, 18, 12); cut = Mathf.Max(Seg(x, y, 30, 52, 44, 52, 5), Seg(x, y, 37, 45, 37, 59, 5), Disc(x, y, 60, 49, 4), Disc(x, y, 68, 56, 4)); break;               // controles
            case 11: c = Mathf.Max(Ring(x, y, 48, 48, 28, 6), Disc(x, y, 48, 48, 11)); cut = Disc(x, y, 48, 48, 5); break;                                                                           // teletransporte
            case 13: c = Mathf.Max(Seg(x, y, 28, 20, 28, 76, 4), Seg(x, y, 48, 20, 48, 76, 4), Seg(x, y, 68, 20, 68, 76, 4), Disc(x, y, 28, 60, 9), Disc(x, y, 48, 36, 9), Disc(x, y, 68, 52, 9)); cut = Mathf.Max(Disc(x, y, 28, 60, 4), Disc(x, y, 48, 36, 4), Disc(x, y, 68, 52, 4)); break;   // sensibilidad
            case 14: c = Mathf.Max(Seg(x, y, 48, 14, 48, 84, 10), Tri(x, y, 48, 38, 8, 66, 88, 66), Tri(x, y, 48, 72, 32, 86, 64, 86)); break;   // modelo (avion)
            case 15: c = Mathf.Max(Tri(x, y, 48, 14, 8, 44, 88, 44), Box(x, y, 48, 64, 30, 18, 3)); cut = Box(x, y, 48, 70, 11, 12, 2); break;   // aviones (hangar)
            default: c = Mathf.Max(Box(x, y, 31, 48, 8, 10, 2), Tri(x, y, 38, 38, 56, 24, 56, 72), Tri(x, y, 38, 38, 56, 72, 38, 58), Ring(x, y, 56, 48, 14, 4) * (x > 58f ? 1f : 0f), Ring(x, y, 56, 48, 26, 4) * (x > 58f ? 1f : 0f)); break;   // sonidos
        }
        return c;
    }
    Texture2D CrearIcono(int id)
    {
        var t = new Texture2D(96, 96, TextureFormat.RGBA32, true); t.wrapMode = TextureWrapMode.Clamp; Color baseC = COL[id];
        for (int y = 0; y < 96; y++)
            for (int x = 0; x < 96; x++)
            {
                float m = Box(x, y, 48, 48, 46, 46, 22); float k = y / 95f; Color b = Color.Lerp(baseC * 1.15f, baseC * 0.62f, k); b.a = 1f;
                float cut; float g = Glifo(id, x, y, out cut); g *= 1f - cut; Color col = Color.Lerp(b, Color.white, g); col.a = m; t.SetPixel(x, 95 - y, col);
            }
        t.Apply(); return t;
    }

    void Nueve(Rect r, float bz)
    {
        if (marco == null) { return; }
        float s = bz / 87f, c = 130f, cd = c * s, W = 1450f, H = 977f;
        float[] xs = { r.x, r.x + cd, r.xMax - cd, r.xMax }, ys = { r.y, r.y + cd, r.yMax - cd, r.yMax };
        float[] us = { 0f, c / W, 1f - c / W, 1f }, vs = { 1f, 1f - c / H, c / H, 0f };    // v hacia arriba: la fila de arriba usa los valores altos
        for (int j = 0; j < 3; j++)
            for (int i = 0; i < 3; i++)
                GUI.DrawTextureWithTexCoords(new Rect(xs[i], ys[j], xs[i + 1] - xs[i], ys[j + 1] - ys[j]), marco, new Rect(us[i], vs[j + 1], us[i + 1] - us[i], vs[j] - vs[j + 1]));
    }

    Rect RectLibre()
    {
        float W = Screen.width, H = Screen.height; ww = Mathf.Clamp(ww, 300f, W); wh = Mathf.Clamp(wh, 300f, H);
        if (wx < 0f) { wx = Mathf.Max(0f, W - ww - 20f); wy = Mathf.Max(0f, 60f); }
        wx = Mathf.Clamp(wx, 0f, W - ww); wy = Mathf.Clamp(wy, 0f, H - wh); return new Rect(wx, wy, ww, wh);
    }

    void OnGUI()
    {
        visible = false;
        if (AvionDeCarton.enAuto) return;
        Rect R;
        if (Acoplada) { if (Hueco.width < 140f || Hueco.height < 140f) return; R = Hueco; } else R = RectLibre();
        Preparar();
        GUI.depth = -60; visible = true; ultimoRect = R;
        var e = Event.current;
        float bz = Mathf.Clamp(Mathf.Min(R.width, R.height) * 0.05f, 10f, 24f);
        var S = new Rect(R.x + bz, R.y + bz, R.width - 2f * bz, R.height - 2f * bz);
        GUI.DrawTexture(new Rect(S.x - 6f, S.y - 6f, S.width + 12f, S.height + 12f), fondo, ScaleMode.StretchToFill);   // se pasa un poco por debajo del marco para que no quede rendija de luz

        // ---- barra de estado: hora a la izquierda, botones a la derecha
        var sb = new Rect(S.x, S.y, S.width, 26f);
        { var c0 = GUI.color; GUI.color = new Color(0f, 0f, 0f, 0.45f); GUI.DrawTexture(sb, negro); GUI.color = c0; }
        int hh = (int)DiaNoche.HoraActual, mm = Mathf.RoundToInt((DiaNoche.HoraActual - hh) * 60f); if (mm == 60) { mm = 0; hh = (hh + 1) % 24; }
        GUI.Label(new Rect(sb.x + 8f, sb.y, 70f, 26f), hh.ToString("00") + ":" + mm.ToString("00"), sHora);
        float bx = sb.xMax - 4f;
        bx -= 88f; if (GUI.Button(new Rect(bx, sb.y + 3f, 86f, 20f), Acoplada ? "SACAR" : "ACOPLAR", sBtnMini)) { Acoplada = !Acoplada; if (Acoplada && Pausado) { Pausado = false; Time.timeScale = 1f; } for (int q = 0; q < zoomTmp.Length; q++) zoomTmp[q] = zoomApp[q]; Guardar(); }
        if (!Acoplada)
        {
            bx -= 124f;
            var bgc = GUI.backgroundColor; GUI.backgroundColor = Pausado ? new Color(1f, 0.45f, 0.3f) : bgc;
            if (GUI.Button(new Rect(bx, sb.y + 3f, 120f, 20f), Pausado ? "VUELO PAUSADO" : "PAUSAR VUELO", sBtnMini)) { Pausado = !Pausado; Time.timeScale = Pausado ? 0f : 1f; }
            GUI.backgroundColor = bgc;
        }
        if (app >= 0) { float tx0 = sb.x + 76f; float tww = bx - tx0 - 4f; if (tww > 20f) GUI.Label(new Rect(tx0, sb.y, tww, 26f), NOM[app].ToUpper(), new GUIStyle(sTit) { clipping = TextClipping.Clip, alignment = TextAnchor.MiddleLeft, fontSize = 14 }); }
        float cy = sb.yMax;
        // ---- barra de la aplicacion abierta: volver, titulo y zoom (A-, A+, CLAVAR)
        if (app >= 0)
        {
            var ab = new Rect(S.x, cy, S.width, 30f);
            { var c0 = GUI.color; GUI.color = new Color(1f, 1f, 1f, 0.08f); GUI.DrawTexture(ab, negro); GUI.color = c0; }
            if (GUI.Button(new Rect(ab.x + 6f, ab.y + 4f, 76f, 22f), "< INICIO", sBtnMini)) app = -1;
            else
            {
                bool clav = Mathf.Abs(zoomTmp[app] - zoomApp[app]) < 0.001f;
                float zx = ab.xMax - 6f;
                zx -= 70f; var bgz = GUI.backgroundColor; GUI.backgroundColor = clav ? new Color(0.5f, 0.5f, 0.5f) : new Color(0.4f, 1f, 0.45f);
                if (GUI.Button(new Rect(zx, ab.y + 4f, 68f, 22f), clav ? "CLAVADO" : "CLAVAR", sBtnMini)) { zoomApp[app] = zoomTmp[app]; Guardar(); }
                GUI.backgroundColor = bgz;
                zx -= 26f; if (GUI.Button(new Rect(zx, ab.y + 4f, 24f, 22f), "A+", sBtnMini)) zoomTmp[app] = Mathf.Min(2.5f, zoomTmp[app] * 1.1f);
                zx -= 42f; GUI.Label(new Rect(zx, ab.y + 4f, 42f, 22f), Mathf.RoundToInt(zoomTmp[app] * 100f) + "%", new GUIStyle(sTxt) { fontSize = 11, alignment = TextAnchor.MiddleCenter });
                zx -= 26f; if (GUI.Button(new Rect(zx, ab.y + 4f, 24f, 22f), "A-", sBtnMini)) zoomTmp[app] = Mathf.Max(0.4f, zoomTmp[app] / 1.1f);
            }
            cy = ab.yMax;
        }
        var C = new Rect(S.x, cy, S.width, S.yMax - cy);
        if (app < 0) Inicio(C, e);
        else
        {
            if (e.type == EventType.ScrollWheel && e.control && C.Contains(e.mousePosition)) { zoomTmp[app] = Mathf.Clamp(zoomTmp[app] * (e.delta.y > 0f ? 0.92f : 1.08f), 0.4f, 2.5f); e.Use(); }   // Ctrl + rueda = zoom; la rueda sola baja
            DibujarApp(C);
        }

        // ---- marco (encima de todo) + agarre para agrandar
        Nueve(R, bz);
        if (!Acoplada)
        {
            var g = new Rect(R.xMax - 26f, R.yMax - 26f, 26f, 26f);
            var c0 = GUI.color; GUI.color = new Color(1f, 1f, 1f, 0.55f);
            for (int k = 0; k < 3; k++) GUI.DrawTexture(new Rect(R.xMax - 7f - k * 5f, R.yMax - 7f - k * 5f, 3f, 3f), negro);
            GUI.color = c0;
            if (e.type == EventType.MouseDown && e.button == 0 && g.Contains(e.mousePosition)) { redimensionando = true; mIni = e.mousePosition; rIni = R; e.Use(); }
            else if (e.type == EventType.MouseDown && e.button == 0 && (sb.Contains(e.mousePosition) || !S.Contains(e.mousePosition) && R.Contains(e.mousePosition))) { arrastrandoVentana = true; offVentana = e.mousePosition - new Vector2(R.x, R.y); e.Use(); }
        }
        // lo que queda sin usar dentro de la tablet no debe llegar a lo que hay detras (mapa, camara, panel)
        if (R.Contains(e.mousePosition) && (e.type == EventType.MouseDown || e.type == EventType.MouseUp || e.type == EventType.ScrollWheel)) e.Use();
    }

    // pantalla de inicio: iconos chicos en 5 columnas (queda lugar para sumar mas); la rueda baja si no entran
    void Inicio(Rect C, Event e)
    {
        float cols = 5f, cw = C.width / cols, isz = Mathf.Clamp(cw * 0.58f, 30f, 62f), ch = isz + 38f;
        int n = orden.Count, filas = Mathf.CeilToInt(n / cols); float alto = 10f + filas * ch + 8f;
        float maxScroll = Mathf.Max(0f, alto - C.height);
        if (e.type == EventType.ScrollWheel && C.Contains(e.mousePosition)) { homeScroll = Mathf.Clamp(homeScroll + e.delta.y * 18f, 0f, maxScroll); e.Use(); }
        homeScroll = Mathf.Clamp(homeScroll, 0f, maxScroll);
        areaIconos = C; slotDestino = -1;
        GUI.BeginGroup(C);   // adentro todo es relativo a la pantalla de la tablet y se recorta al borde
        Vector2 loc = icoMouse - new Vector2(C.x, C.y);
        for (int s = 0; s < n; s++)
        {
            int id = orden[s]; float gx = (s % 5) * cw, gy = 10f + (s / 5) * ch - homeScroll;
            var cell = new Rect(gx, gy, cw, ch); var ir = new Rect(gx + (cw - isz) * 0.5f, gy, isz, isz);
            bool arr = icoArrastrando && icoPresionado == id;
            var c0 = GUI.color; if (arr) GUI.color = new Color(1f, 1f, 1f, 0.35f);
            GUI.DrawTexture(ir, ico[id]);
            if (sel == id && !arr) GUI.DrawTexture(new Rect(ir.x - 3f, ir.y - 3f, ir.width + 6f, ir.height + 6f), selTex);
            var ls = new Rect(gx - 8f, ir.yMax + 3f, cw + 16f, 22f); sEtq.normal.textColor = new Color(0f, 0f, 0f, 0.85f); GUI.Label(new Rect(ls.x + 1f, ls.y + 1f, ls.width, ls.height), NOM[id], sEtq); sEtq.normal.textColor = Color.white; GUI.Label(ls, NOM[id], sEtq);
            GUI.color = c0;
            if (icoArrastrando && loc.x >= cell.x && loc.x < cell.xMax && loc.y >= cell.y && loc.y < cell.yMax) slotDestino = s;
            if (e.type == EventType.MouseDown && e.button == 0 && cell.Contains(e.mousePosition) && icoPresionado < 0)
            {
                sel = id;
                if (e.clickCount >= 2) { app = id; zoomTmp[id] = zoomApp[id]; icoPresionado = -1; }
                else { icoPresionado = id; icoMouse0 = e.mousePosition + new Vector2(C.x, C.y); icoMouse = icoMouse0; icoArrastrando = false; }
                e.Use();
            }
        }
        if (icoArrastrando)
        {
            if (slotDestino < 0) slotDestino = Mathf.Clamp(Mathf.FloorToInt(loc.x / cw) + 5 * Mathf.FloorToInt((loc.y - 10f + homeScroll) / ch), 0, n - 1);
            GUI.DrawTexture(new Rect(loc.x - isz * 0.5f, loc.y - isz * 0.5f, isz, isz), ico[icoPresionado]);
        }
        GUI.EndGroup();
    }

    void DibujarApp(Rect C)
    {
        float a0 = Tienda.PanelLateral.ancho; var r0 = Tienda.PanelLateral.rect;
        float esc = Mathf.Clamp(C.width / ANCHO_V[app] * zoomTmp[app], 0.3f, 2.2f), Wv = C.width / esc, Hv = C.height / esc;   // la app se dibuja a su ancho virtual y se escala para entrar justo
        Tienda.PanelLateral.ancho = Wv - 24f; Tienda.PanelLateral.rect = C;   // asi las pestanas de panel grande se acomodan al ancho de la tablet y el scroll no mueve la camara
        Compacto = true;
        var m0 = GUI.matrix; GUI.matrix = m0 * Matrix4x4.TRS(new Vector3(C.x, C.y, 0f), Quaternion.identity, new Vector3(esc, esc, 1f));
        if (app == 0) { Torre.AppInstrumentos(new Rect(6f, 6f, Wv - 12f, Hv - 12f)); }
        else if (app == 1) { Torre.AppRadio(new Rect(6f, 6f, Wv - 12f, Hv - 12f)); }
        else
        {
            GUILayout.BeginArea(new Rect(6f, 4f, Wv - 12f, Hv - 8f));
            scroll[app] = GUILayout.BeginScrollView(scroll[app], false, false, GUIStyle.none, GUI.skin.verticalScrollbar);
            switch (app)
            {
                case 2: Aeropuertos.PestanaDestinos(sTxt, sBtn, sTit); break;
                case 3: Carrera.PestanaCarrera(sTxt, sBtn, sTit); break;
                case 4: Aeropuertos.PestanaTorre(sTxt, sBtn, sTit); break;
                case 5: Tienda.PestanaShopping(sTxt, sBtn, sTit); break;
                case 6: Tienda.PestanaMaletin(sTxt, sBtn, sTit); break;
                case 7: LucesAvion.PestanaLuces(sTxt, sBtn, sTit); break;
                case 8: TrenAterrizaje.PestanaTren(sTxt, sBtn, sTit); break;
                case 9: Guerra.PestanaGuerra(sTxt, sBtn, sTit); break;
                case 10: Mapeo.PestanaControles(sTxt, sBtn, sTit); break;
                case 11: Teletransporte.Pestana(sTxt, sBtn, sTit); break;
                case 13: AvionDeCarton.AppSensibilidad(); break;
                case 14: AvionDeCarton.AppModelo(); break;
                case 15: Hangar.PestanaAviones(Wv - 30f, Hv - 20f); break;
                default:
                    GUILayout.Label("SONIDOS", sTit);
                    Mezcla.Fila(Mezcla.GENERAL, sTxt); Mezcla.Fila(Mezcla.MOTOR_AVION, sTxt); Mezcla.Fila(Mezcla.MOTOR_AUTO, sTxt); Mezcla.Fila(Mezcla.RADIO, sTxt);
                    Mezcla.Fila(Mezcla.MUSICA, sTxt); Mezcla.Fila(Mezcla.AZAFATA, sTxt); Mezcla.Fila(Mezcla.FX, sTxt); Mezcla.Fila(Mezcla.TORRE, sTxt); break;
            }
            GUILayout.EndScrollView(); GUILayout.EndArea();
        }
        GUI.matrix = m0; Compacto = false;
        Tienda.PanelLateral.ancho = a0; Tienda.PanelLateral.rect = r0;
    }
}
