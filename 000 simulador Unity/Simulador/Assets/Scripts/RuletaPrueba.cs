using System.Collections.Generic;
using System.Security.Cryptography;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

// PRUEBA DE FISICA DE LA RULETA (europea, 37 numeros). Boton "RULETA" arriba a la izquierda.
// El resultado sale SOLO de la fisica: lo unico al azar es la fuerza/angulo del lanzamiento y la velocidad de la rueda (generador criptografico).
public class ColLog : MonoBehaviour { public static int n; void OnCollisionEnter(Collision c) { if (n++ < 60) Debug.Log("[TEST] choque con " + c.collider.name + " en " + transform.position.y.ToString("0.00") + " t=" + Time.time.ToString("0.00")); } }

public class RuletaPrueba : MonoBehaviour
{
    bool intro; Texture2D fotoIntro;
    bool entrarAbajo; float entrarT = -1f; AudioSource audEntrar; string verTxt; static Texture2D texBlanca;
    // boton ENTRAR: nitido y sobrio; al tocarlo se hunde, se ilumina y hace clic, y se entra en pantalla completa
    void BotonEntrar()
    {
        var e = Event.current; float H = Screen.height, W = Screen.width;
        float bw = Mathf.Min(W * 0.42f, 380f), bh = Mathf.Max(56f, H * 0.085f); var r = new Rect(W / 2f - bw / 2f, H * 0.84f - bh / 2f, bw, bh);
        bool hundido = entrarAbajo || entrarT >= 0f; var rb = hundido ? new Rect(r.x, r.y + 3f, r.width, r.height) : r;
        if (texBlanca == null) texBlanca = Texture2D.whiteTexture;
        var c0 = GUI.color;
        if (!hundido) { GUI.color = new Color(0f, 0f, 0f, 0.35f); GUI.DrawTexture(new Rect(r.x + 3f, r.y + 5f, r.width, r.height), texBlanca); }
        GUI.color = new Color(0.95f, 0.78f, 0.35f, 1f); GUI.DrawTexture(rb, texBlanca);   // borde dorado
        GUI.color = hundido ? new Color(0.93f, 0.74f, 0.3f, 1f) : new Color(0.1f, 0.06f, 0.03f, 0.88f); GUI.DrawTexture(new Rect(rb.x + 3f, rb.y + 3f, rb.width - 6f, rb.height - 6f), texBlanca);
        GUI.color = c0;
        var st = new GUIStyle(GUI.skin.label) { fontSize = Mathf.RoundToInt(bh * 0.5f), fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter }; st.normal.textColor = hundido ? new Color(0.2f, 0.1f, 0f) : new Color(1f, 0.86f, 0.45f);
        GUI.Label(rb, "ENTRAR", st);
        if (verTxt == null) { var ta = Resources.Load<TextAsset>("version"); verTxt = ta != null ? ta.text.Trim() : ""; }
        if (verTxt != "") { var sv = new GUIStyle(GUI.skin.label) { fontSize = Mathf.Max(12, Mathf.RoundToInt(H * 0.02f)), alignment = TextAnchor.LowerRight }; sv.normal.textColor = new Color(1f, 1f, 1f, 0.75f); GUI.Label(new Rect(W - 310f, H - 34f, 300f, 28f), "versión " + verTxt, sv); }
        if (entrarT >= 0f) { if (e.type == EventType.Repaint && Time.realtimeSinceStartup - entrarT > 0.2f) { entrarT = -1f; entrarAbajo = false; intro = false; Abrir(); } return; }
        if (e.type == EventType.MouseDown && r.Contains(e.mousePosition)) { entrarAbajo = true; e.Use(); }
        else if (e.type == EventType.MouseUp && entrarAbajo)
        {
            entrarAbajo = false; e.Use();
            if (r.Contains(e.mousePosition))
            {
                entrarT = Time.realtimeSinceStartup;
                var clip = Resources.Load<AudioClip>("Ruleta/fichas_sonidos/s1"); if (clip != null && camGO == null) { if (audEntrar == null) audEntrar = gameObject.AddComponent<AudioSource>(); audEntrar.PlayOneShot(clip, 1f); }
            }
        }
    }
#if RULETA_APARTE
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Crear() { new GameObject("RuletaPrueba").AddComponent<RuletaPrueba>(); }
    void Start()
    {
        var fs = Resources.LoadAll<Texture2D>("Ruleta/fotos");
        if (fs.Length > 0) { int i = PlayerPrefs.GetInt("rul_foto", 0); fotoIntro = fs[i % fs.Length]; PlayerPrefs.SetInt("rul_foto", (i + 1) % fs.Length); intro = true; }   // cada vez que se entra, otra foto
        else Abrir();
    }
#endif

    static readonly Vector3 CENTRO = new Vector3(0f, 3000f, 0f);   // lejos del mundo, arriba
    const float RADIO_CUENCO = 4f;          // en unidades del modelo (el cuenco mide 8 de diametro)
    const float G = 98f;                    // gravedad en unidades del modelo (ruleta real = 0,4 m de radio -> x10)

    bool abierta, lanzada, quieta; float tQuieto, tLanzada, tZona;
    GameObject raiz, camGO, paredGO; Camera cam; Light luz;
    Transform baseT, giro, bola; Rigidbody rbGiro, rbBola; float S = 1f;
    float wObj; bool frenarPlato;   // velocidad a la que va el plato (cambia de sentido en cada tirada, como hace el crupier)
    float wRueda;                           // rad/s de la rueda (sentido horario)
    float cans = 0.5f, v0 = 30f, amort = 0.3f, vueltas = 6f, rebote = 0.45f;   // ajustables con los deslizadores
    int resultado = -1; List<int> historial = new List<int>();
    float oldDt; GUIStyle sB, sT;
    PhysicsMaterial matR; bool enganchada; float tEng, tCapt, vEnc, rebZona; MeshCollider capCol; int capN = -1; bool parando; float tCayo; Vector3 posNum;
    // tres fisicas de prueba (una por vista): A costado, B arriba, C cerca
    static readonly float[] FR = { 0.45f, 0.25f, 0.12f }, FF = { 0.06f, 0.12f, 0.25f }, FG = { 1.0f, 1.2f, 1.4f }, FD = { 0.15f, 0.3f, 0.45f }, PS = { 0.75f, 0.6f, 0.4f }, VE0 = { 1.5f, 1.5f, 2f }, VE1 = { 4f, 6.5f, 9f }, FL = { 1.2f, 0.9f, 0.6f };
    int Fis { get { return 2; } }   // misma fisica en todas las vistas (la de CERCA, que fue la que mejor se quedo en su numero)

    static float Azar() { var b = new byte[8]; using (var g = RandomNumberGenerator.Create()) g.GetBytes(b); return (System.BitConverter.ToUInt64(b, 0) >> 11) / (float)(1UL << 53); }

    Quaternion giroRot0 = Quaternion.identity; float wBase = 1.0f; Quaternion ajusteRot = Quaternion.identity; bool prefsCargadas;
    void CargarPrefs() { if (prefsCargadas) return; prefsCargadas = true; v0 = PlayerPrefs.GetFloat("rul2_v0", v0); amort = PlayerPrefs.GetFloat("rul2_amort", amort); wBase = PlayerPrefs.GetFloat("rul3_w", wBase); v0 = Mathf.Clamp(v0, 10f, 40f); vueltas = PlayerPrefs.GetFloat("rul2_vueltas", vueltas); cans = Mathf.Clamp(PlayerPrefs.GetFloat("rul5_cans", cans), 0f, 3f); ajusteRot = new Quaternion(PlayerPrefs.GetFloat("rul2_qx", 0f), PlayerPrefs.GetFloat("rul2_qy", 0f), PlayerPrefs.GetFloat("rul2_qz", 0f), PlayerPrefs.GetFloat("rul2_qw", 1f)); }
    void GuardarPrefs() { PlayerPrefs.SetFloat("rul2_v0", v0); PlayerPrefs.SetFloat("rul2_amort", amort); PlayerPrefs.SetFloat("rul3_w", wBase); PlayerPrefs.SetFloat("rul2_vueltas", vueltas); PlayerPrefs.SetFloat("rul5_cans", cans); PlayerPrefs.SetFloat("rul2_qx", ajusteRot.x); PlayerPrefs.SetFloat("rul2_qy", ajusteRot.y); PlayerPrefs.SetFloat("rul2_qz", ajusteRot.z); PlayerPrefs.SetFloat("rul2_qw", ajusteRot.w); PlayerPrefs.Save(); }
    // caida con suspenso: la bolita sigue girando despacito sobre el borde de los numeros antes de caer
    public static int opcion = -1; bool sus;
    AudioSource audLoop, audCae, audGolpe; AudioClip clipLoop, clipCae, clipGolpe; bool cayo, sonoNumero;
    int pano = 1; Texture2D[] panos; bool config; RuletaMesa mesa;
#if UNITY_WEBGL
    int modelo = 0, vista = 1;
#else
    int modelo = 0, vista = 0;
#endif
    //   // modelo: 0 = vista clara (numeros grandes), 1 = real de casino; vista: 0 = de costado, 1 = desde arriba, 2 = cerca
    static string Limpio(string n) { int i = n.IndexOf('.'); return i < 0 ? n : n.Substring(0, i); }

    // ---- modo "dentro del juego": se abre desde la tablet; el avion queda congelado y la ruleta se ve en pantalla completa
    static bool enJuego; RenderTexture rt; bool congelado;
    Scene escena; PhysicsScene fisica; float acc; List<Renderer> rends = new List<Renderer>(); int ultDibujo; bool suscripta; const float DTF = 1f / 480f;   // dentro de la tablet: fisica propia (escena aparte) y solo la camara de la ruleta ve la ruleta
    struct Cong { public Rigidbody rb; public bool kin; public Vector3 v, w; }
    List<Cong> congeladas = new List<Cong>();
    public static bool Activa { get { var r = Object.FindFirstObjectByType<RuletaPrueba>(); return r != null && r.abierta; } }
    public static void AbrirEnJuego()
    {
        enJuego = true; var r = Object.FindFirstObjectByType<RuletaPrueba>(); if (r == null) r = new GameObject("RuletaPrueba").AddComponent<RuletaPrueba>();
        if (!r.abierta) { r.modelo = 0; r.vista = 1; r.Abrir(); } r.ultDibujo = Time.frameCount;   // dentro de la tablet: ruleta clara, vista desde arriba; el avion sigue volando
    }
    public static void ModoTablet(bool v) { enJuego = v; }
    public static void CerrarEnTablet() { var r = Object.FindFirstObjectByType<RuletaPrueba>(); if (r != null && r.abierta) r.Cerrar(); }
    // botones chicos de la ruleta: la tablet los dibuja en su barra azul, al lado de INICIO (a = lugar disponible, st = estilo de boton chico)
    public static void CabeceraTablet(Rect a, GUIStyle st)
    {
        var r = Object.FindFirstObjectByType<RuletaPrueba>(); if (r == null || !r.abierta || r.mesa == null) return; r.ultDibujo = Time.frameCount;
        var s = new GUIStyle(st) { fontSize = 11, clipping = TextClipping.Clip, alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold };
        float g = 3f, w = (a.width - g * 7f) / 8f; int i = 0; Rect R() { return new Rect(a.x + (w + g) * (i++), a.y, w, a.height); }
        if (GUI.Toggle(R(), r.vista == 1, "ARRIBA", s) && r.vista != 1) { r.vista = 1; r.PonerCamara(); }
        if (GUI.Toggle(R(), r.vista == 2, "CERCA", s) && r.vista != 2) { r.vista = 2; r.PonerCamara(); }
        if (GUI.Toggle(R(), r.config, "AJUSTES", s) != r.config) r.config = !r.config;
        if (GUI.Button(R(), "ORDENAR", s)) r.mesa.PedirOrdenar();
        if (GUI.Button(R(), "ACOMODAR", s)) { r.mesa.editando = true; r.config = false; }
        if (GUI.Toggle(R(), r.soloRuleta, "SOLO RULETA", s) != r.soloRuleta) r.PonerSolo(!r.soloRuleta);
        if (GUI.Button(R(), RuletaMesa.modoPrueba ? "PRUEBA: SI" : "PRUEBA: NO", s)) RuletaMesa.PonerPrueba(!RuletaMesa.modoPrueba);
        if (GUI.Toggle(R(), r.paleta, "COLORES", s) != r.paleta) r.paleta = !r.paleta;
    }
    // la tablet la dibuja dentro de su pantalla: v = lienzo virtual (ya escalado por la tablet), origen = esquina en pantalla, esc = escala
    public static bool DibujarDentro(Rect v, Vector2 origen, float esc)
    {
        var r = Object.FindFirstObjectByType<RuletaPrueba>(); if (r == null || !r.abierta) return false;
        r.ultDibujo = Time.frameCount; RuletaMesa.VW = v.width; RuletaMesa.VH = v.height; RuletaMesa.OrigenV = origen; RuletaMesa.EscV = esc;
        GUI.BeginGroup(v); r.DibujarTodo(); GUI.EndGroup(); return r.abierta;
    }
    void AlCamara(ScriptableRenderContext ctx, Camera c)   // solo la camara de la ruleta ve la ruleta (y su luz): el resto del juego no se entera
    {
        if (!abierta) return; bool mia = c == cam;
        for (int i = 0; i < rends.Count; i++) if (rends[i] != null) rends[i].enabled = mia;
        if (luz != null) luz.enabled = mia;
        // el sol y las demas luces del avion (color anaranjado, etc.) no tocan la ruleta: solo la ve su propia luz
        // (primero se devuelven las mascaras reales y recien despues se arma la lista, si no se guardaba un 0 y el sol quedaba apagado para siempre)
        if (!mia) { if (otrasL != null) for (int i = 0; i < otrasL.Count; i++) if (otrasL[i] != null) otrasL[i].cullingMask = otrasM[i]; if (otrasL == null || Time.frameCount - tOtrasL > 120) { tOtrasL = Time.frameCount; otrasL = new List<Light>(); otrasM = new List<int>(); foreach (var l in Object.FindObjectsByType<Light>(FindObjectsSortMode.None)) if (l != luz) { otrasL.Add(l); otrasM.Add(l.cullingMask); } } }
        else if (otrasL != null) for (int i = 0; i < otrasL.Count; i++) if (otrasL[i] != null) otrasL[i].cullingMask = 0;
    }
    List<Light> otrasL; List<int> otrasM; int tOtrasL;
    bool Rc(Vector3 o, Vector3 d, out RaycastHit h, float dist) { return enJuego && fisica.IsValid() ? fisica.Raycast(o, d, out h, dist) : Physics.Raycast(o, d, out h, dist); }
    void OnDestroy() { if (suscripta) { RenderPipelineManager.beginCameraRendering -= AlCamara; suscripta = false; } }
    void Congelar()
    {
        if (congelado) return; congelado = true; congeladas.Clear();
        foreach (var rb in Object.FindObjectsByType<Rigidbody>(FindObjectsSortMode.None)) { congeladas.Add(new Cong { rb = rb, kin = rb.isKinematic, v = rb.isKinematic ? Vector3.zero : rb.linearVelocity, w = rb.isKinematic ? Vector3.zero : rb.angularVelocity }); rb.isKinematic = true; }
    }
    void Descongelar()
    {
        if (rt != null) { rt.Release(); Destroy(rt); rt = null; }
        if (!congelado) return; congelado = false;
        foreach (var c in congeladas) if (c.rb != null) { c.rb.isKinematic = c.kin; if (!c.kin) { c.rb.linearVelocity = c.v; c.rb.angularVelocity = c.w; } }
        congeladas.Clear();
    }
    const float VOL_GIRO = 0.5f;   // el giro en la pista, suave (el de antes aturdia)
    float fovBase = 34f; bool ambGuardado; UnityEngine.Rendering.AmbientMode ambModo; Color ambLuz; float ambInt, refInt;
    bool soloRuleta, autoSolo; float tSoloFin; int vistaAntes = 1;   // 21/11: boton SOLO RULETA: desaparece todo menos la rueda, en el medio, vista CERCA y 10% mas de zoom
    Rect RRul { get { return soloRuleta ? new Rect(0f, 0f, RuletaMesa.SW, RuletaMesa.SH) : mesa.RectRuleta; } }
    void PonerSolo(bool v) { if (v == soloRuleta) return; soloRuleta = v; autoSolo = false; if (v) { vistaAntes = vista; vista = 2; } else vista = vistaAntes; PonerCamara(); }
    void AsegurarRT()
    {
        if (cam == null) return;
        bool tab = enJuego && RuletaMesa.VW > 0f; int rw = Mathf.Clamp(tab ? Mathf.RoundToInt(RuletaMesa.VW * RuletaMesa.EscV) : Screen.width, 64, 4096), rh = Mathf.Clamp(tab ? Mathf.RoundToInt(RuletaMesa.VH * RuletaMesa.EscV) : Screen.height, 64, 4096);
#if true   // el simulador usa el mismo aspecto que la web
        if (!tab && mesa != null) { var rr0 = RRul; rw = Mathf.Clamp(Mathf.RoundToInt(rr0.width), 64, 4096); rh = Mathf.Clamp(Mathf.RoundToInt(rr0.height), 64, 4096); }
        { float a = rw / (float)rh, aRef = vista == 0 ? 1.35f : 1.05f; cam.fieldOfView = a < aRef ? 2f * Mathf.Atan(Mathf.Tan(fovBase * 0.5f * Mathf.Deg2Rad) * aRef / a) * Mathf.Rad2Deg : fovBase; if (soloRuleta) cam.fieldOfView = 2f * Mathf.Atan(Mathf.Tan(cam.fieldOfView * 0.5f * Mathf.Deg2Rad) / 1.1f) * Mathf.Rad2Deg; }   // ventana angosta: se abre el angulo para que la ruleta entre entera de costado a costado
#endif
        if (rt == null || rt.width != rw || rt.height != rh) { if (rt != null) { rt.Release(); Destroy(rt); } rt = new RenderTexture(rw, rh, 24); }
        cam.targetTexture = rt;
    }

    static int nEscena;   // cada apertura usa una escena con nombre propio: la anterior se descarga de a poco y el mismo nombre daba error (dejaba la ruleta a medio armar, tirada sobre el avion, sin resultados ni pagos)
    bool urlLeida;
    public void Abrir()
    {
        CargarPrefs();
#if UNITY_WEBGL
        if (!urlLeida) { urlLeida = true; string u = Application.absoluteURL.ToLowerInvariant(); if (u.Contains("real")) modelo = 1; if (u.Contains("costado")) vista = 0; else if (u.Contains("cerca")) vista = 2; }   // para probar: ?real, ?costado, ?cerca
#endif
        if (panos == null) panos = new Texture2D[] { null, Resources.Load<Texture2D>("Ruleta/panos/pano1_verde"), Resources.Load<Texture2D>("Ruleta/panos/pano2_azul"), Resources.Load<Texture2D>("Ruleta/panos/pano3_bordo") };
        if (clipLoop == null) { clipLoop = Resources.Load<AudioClip>("Ruleta/sonidos/rueda_casino"); if (clipLoop == null) clipLoop = Resources.Load<AudioClip>("Ruleta/sonidos/rebotes/giro_video_a"); if (clipLoop == null) clipLoop = Resources.Load<AudioClip>("Ruleta/sonidos/bolita_gira_loop"); clipCae = Resources.Load<AudioClip>("Ruleta/sonidos/bolita_cae_rebota"); clipGolpe = Resources.Load<AudioClip>("Ruleta/sonidos/bolita_golpe"); }
        var pref = Resources.Load<GameObject>(modelo == 0 ? "Ruleta/Ruleta" : "Ruleta/RuletaReal");
        if (pref == null) { Debug.LogError("Falta Resources/Ruleta/Ruleta.fbx"); return; }
        raiz = Instantiate(pref); Embellecer();
        if (enJuego) { escena = SceneManager.CreateScene("RuletaFisica" + (++nEscena), new CreateSceneParameters(LocalPhysicsMode.Physics3D)); fisica = escena.GetPhysicsScene(); SceneManager.MoveGameObjectToScene(raiz, escena); }   // la fisica de la ruleta vive aparte: no choca con el avion
        { // enderezar: el eje de la ruleta es el lado mas fino del cuenco y la perilla queda arriba
            Transform cu0 = Buscar(raiz.transform, "cuenco"), pe = Buscar(raiz.transform, "perilla"); var r0 = cu0.GetComponent<Renderer>();
            Vector3 sz = r0.bounds.size; Vector3 ax = Vector3.up; if (sz.z < sz.y && sz.z < sz.x) ax = Vector3.forward; else if (sz.x < sz.y && sz.x < sz.z) ax = Vector3.right;
            if (Vector3.Dot(pe.position - cu0.position, ax) < 0f) ax = -ax;   // la perilla esta por encima del piso del cuenco (el origen del cuenco)
            raiz.transform.rotation = ajusteRot * Quaternion.FromToRotation(ax, Vector3.up) * raiz.transform.rotation;
        }
        { // la parte giratoria tambien llega parada: se endereza con su propio eje (lado mas fino del cono) y la perilla arriba
            Transform g0 = Buscar(raiz.transform, "RULETA_giratoria"), co = Buscar(raiz.transform, "cono_madera"), pe2 = Buscar(raiz.transform, "perilla"); var rc = co.GetComponent<Renderer>();
            Vector3 sz2 = rc.bounds.size; Vector3 ax2 = Vector3.up; if (sz2.z < sz2.y && sz2.z < sz2.x) ax2 = Vector3.forward; else if (sz2.x < sz2.y && sz2.x < sz2.z) ax2 = Vector3.right;
            if (Vector3.Dot(pe2.position - g0.position, ax2) < 0f) ax2 = -ax2;
            g0.rotation = Quaternion.FromToRotation(ax2, Vector3.up) * g0.rotation; giroRot0 = g0.localRotation;
        }
        // normalizar tamano: el cuenco mide RADIO_CUENCO*2
        Transform cu = Buscar(raiz.transform, "cuenco"); var rr = cu.GetComponent<Renderer>();
        float ancho = Mathf.Max(rr.bounds.size.x, rr.bounds.size.z); S = (RADIO_CUENCO * 2f) / ancho; raiz.transform.localScale *= S;
        baseT = Buscar(raiz.transform, "RULETA_base"); giro = Buscar(raiz.transform, "RULETA_giratoria"); bola = Buscar(raiz.transform, "BOLA");
        raiz.transform.position += CENTRO - Buscar(raiz.transform, "RULETA_base").position;
        S = raiz.transform.lossyScale.x * 1f; // unidades modelo -> mundo (el cuenco ya mide 8*S/S ... ver escala)
        S = raiz.transform.lossyScale.x;

        foreach (var t0 in raiz.GetComponentsInChildren<MeshFilter>()) { string n0 = Limpio(t0.name); if (n0 == "cuenco" || n0 == "pista_bola" || n0.StartsWith("aro_")) t0.sharedMesh = DobleCara(t0.sharedMesh); }
        var mat = matR = new PhysicsMaterial("ruleta") { dynamicFriction = 0.06f, staticFriction = 0.08f, bounciness = rebote, frictionCombine = PhysicsMaterialCombine.Average, bounceCombine = PhysicsMaterialCombine.Maximum };
        rbGiro = giro.gameObject.AddComponent<Rigidbody>(); rbGiro.isKinematic = true;
        foreach (var t in raiz.GetComponentsInChildren<Transform>())
        {
            string n = Limpio(t.name); var mf = t.GetComponent<MeshFilter>(); if (mf == null) continue;
            if (n.StartsWith("rombo_") || n.StartsWith("separador_")) { var c = t.gameObject.AddComponent<BoxCollider>(); c.sharedMaterial = mat; if (n.StartsWith("separador_")) AltoSep(c, t); }
            else if (n == "cuenco" || n == "pista_bola") { var c = t.gameObject.AddComponent<MeshCollider>(); c.sharedMaterial = mat; }   // fijos: malla de doble cara
            else if (n == "cono_madera" || n == "cubo_central") { var c = t.gameObject.AddComponent<MeshCollider>(); c.convex = true; c.sharedMaterial = mat; }   // partes que giran: solidos convexos (la malla fina las atraviesa)
            else if (n.StartsWith("casillero_")) { var c = t.gameObject.AddComponent<MeshCollider>(); c.convex = true; c.sharedMesh = Engrosar(mf.sharedMesh, t); c.sharedMaterial = mat; }
        }
        // pared exterior de la pista: el borde del FBX es una rampa inclinada y la bolita se iria volando
        {
            paredGO = new GameObject("pared"); if (enJuego) SceneManager.MoveGameObjectToScene(paredGO, escena); paredGO.transform.position = baseT.position; int seg = 96; var vs = new Vector3[seg * 2]; var tr = new int[seg * 12]; float R = 3.47f * S, y0 = 0.45f * S, y1 = 1.6f * S;
            for (int i = 0; i < seg; i++) { float a = i * Mathf.PI * 2f / seg; vs[i] = new Vector3(Mathf.Cos(a) * R, y0, Mathf.Sin(a) * R); vs[i + seg] = new Vector3(Mathf.Cos(a) * R, y1, Mathf.Sin(a) * R); }
            for (int i = 0; i < seg; i++) { int j = (i + 1) % seg; int k = i * 12; tr[k] = i; tr[k + 1] = i + seg; tr[k + 2] = j; tr[k + 3] = j; tr[k + 4] = i + seg; tr[k + 5] = j + seg; tr[k + 6] = i; tr[k + 7] = j; tr[k + 8] = i + seg; tr[k + 9] = j; tr[k + 10] = j + seg; tr[k + 11] = i + seg; }
            var pm = new Mesh { name = "pared" }; pm.vertices = vs; pm.triangles = tr; var pc = paredGO.AddComponent<MeshCollider>(); pc.sharedMesh = pm; pc.sharedMaterial = mat;
        }
        // la bolita sale del arbol y es dinamica
        bola.SetParent(null, true); bola.localScale *= 1.25f;   // un poquito mas grande
        var sc = bola.gameObject.AddComponent<SphereCollider>(); sc.sharedMaterial = mat;
        rbBola = bola.gameObject.AddComponent<Rigidbody>(); rbBola.useGravity = false; rbBola.mass = 1f;
        rbBola.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic; rbBola.interpolation = enJuego ? RigidbodyInterpolation.None : RigidbodyInterpolation.Interpolate;
        rbBola.solverIterations = 40; rbBola.solverVelocityIterations = 10; rbBola.maxAngularVelocity = 3000f;
        rbBola.isKinematic = true; PonerBolaEnPista(0f);

        camGO = new GameObject("RuletaCam"); cam = camGO.AddComponent<Camera>(); cam.depth = 90; cam.clearFlags = CameraClearFlags.SolidColor; cam.backgroundColor = new Color(0.02f, 0.12f, 0.06f); cam.fieldOfView = 38f; cam.nearClipPlane = 0.2f; cam.farClipPlane = 400f;
        { var ut = System.Type.GetType("UnityEngine.Rendering.Universal.UniversalAdditionalCameraData, Unity.RenderPipelines.Universal.Runtime"); if (ut != null) { var ud = camGO.AddComponent(ut); var pp = ut.GetProperty("renderPostProcessing"); if (pp != null) pp.SetValue(ud, false, null); var vm = ut.GetProperty("volumeLayerMask"); if (vm != null) vm.SetValue(ud, (LayerMask)0, null); var aa = ut.GetProperty("renderShadows"); } }   // sin postproceso del simulador (lavaba los colores)
        var lg = new GameObject("RuletaLuz"); luz = lg.AddComponent<Light>(); luz.type = LightType.Directional; luz.intensity = 1.4f; lg.transform.rotation = Quaternion.Euler(55f, 25f, 0f); luz.cullingMask = ~0; if (enJuego) luz.enabled = false;
        if (Object.FindFirstObjectByType<AudioListener>() == null) camGO.AddComponent<AudioListener>();
        audLoop = camGO.AddComponent<AudioSource>(); audLoop.clip = clipLoop; audLoop.loop = true; audLoop.playOnAwake = false; audLoop.spatialBlend = 0f;
        audCae = camGO.AddComponent<AudioSource>(); audCae.clip = clipCae; audCae.playOnAwake = false; audCae.spatialBlend = 0f; audGolpe = camGO.AddComponent<AudioSource>(); audGolpe.clip = clipGolpe; audGolpe.playOnAwake = false; audGolpe.spatialBlend = 0f;
        PonerCamara(); AsegurarRT();
        oldDt = Time.fixedDeltaTime; if (!enJuego) Time.fixedDeltaTime = 1f / 480f; acc = 0f; ultDibujo = Time.frameCount;
        rends.Clear(); rends.AddRange(raiz.GetComponentsInChildren<Renderer>(true)); foreach (var rb2 in bola.GetComponentsInChildren<Renderer>(true)) if (!rends.Contains(rb2)) rends.Add(rb2);
        if (enJuego && !suscripta) { RenderPipelineManager.beginCameraRendering += AlCamara; suscripta = true; foreach (var r2 in rends) r2.enabled = false; }
        abierta = true; lanzada = false; resultado = -1;
    }

    // para acomodar el plato a mano: gira toda la ruleta 90 grados sobre el eje elegido (cuando la fisica se lanza, el plato tiene que quedar horizontal)
    void Girar(Vector3 eje, float grados = 90f) { ajusteRot = Quaternion.AngleAxis(grados, eje) * ajusteRot; raiz.transform.RotateAround(CENTRO, eje, grados); rbBola.isKinematic = true; PonerBolaEnPista(0f); }

    // ---- aspecto: dorado metalico, maderas pulidas, casilleros mates (sin reflejo sobre los numeros) y un cuarto de estudio para que el metal tenga que reflejar
    static Cubemap cuarto;
    static Cubemap CuartoEstudio()
    {
        if (cuarto != null) return cuarto; const int N = 128; cuarto = new Cubemap(N, TextureFormat.RGBAHalf, true);
        Texture2D env = null; var ta = Resources.Load<TextAsset>("Ruleta/entorno_forest"); if (ta != null) { env = new Texture2D(2, 2, TextureFormat.RGBA32, false); env.LoadImage(ta.bytes); }   // entorno del Blender (HDRI de estudio, CC0)
        for (int f = 0; f < 6; f++)
        {
            var px = new Color[N * N];
            for (int y = 0; y < N; y++) for (int x = 0; x < N; x++)
            {
                float u = (x + 0.5f) / N * 2f - 1f, v = (y + 0.5f) / N * 2f - 1f; Vector3 d;
                switch (f) { case 0: d = new Vector3(1f, -v, -u); break; case 1: d = new Vector3(-1f, -v, u); break; case 2: d = new Vector3(u, 1f, v); break; case 3: d = new Vector3(u, -1f, -v); break; case 4: d = new Vector3(u, -v, 1f); break; default: d = new Vector3(-u, -v, -1f); break; }
                d.Normalize(); float t = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(-0.35f, 0.9f, d.y));
                Color c = Color.Lerp(new Color(0.38f, 0.27f, 0.17f), new Color(0.95f, 0.9f, 0.82f), t);
                if (d.y > 0.5f && Mathf.Abs(d.x) < 0.55f && Mathf.Abs(d.z) < 0.55f) c += new Color(2.6f, 2.4f, 2.1f);                                  // luz grande de techo
                if (Mathf.Abs(d.y - 0.1f) < 0.22f && Mathf.Sin(Mathf.Atan2(d.z, d.x) * 5f) > 0.25f) c += new Color(2.2f, 1.8f, 1.25f);                                // tiras de luz calidas a los lados
                if (d.z > 0.6f && Mathf.Abs(d.y - 0.25f) < 0.18f && Mathf.Abs(d.x) < 0.35f) c += new Color(1.6f, 1.55f, 1.5f);                     // ventana al frente
                if (env != null) { float eu = Mathf.Atan2(d.x, -d.z) / (2f * Mathf.PI) + 0.5f, ev = Mathf.Asin(Mathf.Clamp(d.y, -1f, 1f)) / Mathf.PI + 0.5f; var e = env.GetPixelBilinear(eu, ev); float lr = Mathf.Pow(e.r, 2.2f), lg = Mathf.Pow(e.g, 2.2f), lb = Mathf.Pow(e.b, 2.2f), lum = (lr + lg + lb) / 3f, gan = 1.6f + 6f * lum * lum; c = new Color(lr * gan, lg * gan, lb * gan, 1f); }
                px[y * N + x] = c;
            }
            cuarto.SetPixels(px, (CubemapFace)f);
        }
        cuarto.Apply(true); return cuarto;
    }
    // ---- paleta de colores: cada parte tiene color, brillo y reflejo; se editan en vivo (boton COLORES) y quedan guardados
    class Parte { public string nom; public Color def, col; public float sDef, suav, metal; public bool rDef, refl; public Material mat; }
    Parte[] partes; int parteSel; bool paleta; bool urpOn;
    static Color Col(float r, float g, float b) { var c = new Color(r, g, b, 1f); return QualitySettings.activeColorSpace == ColorSpace.Linear ? c.gamma : c; }   // los valores del Blender son lineales: asi se ve igual
    void AplicarParte(Parte q)
    {
        if (q.mat == null) return; q.mat.SetColor("_BaseColor", q.col); q.mat.SetColor("_Color", q.col); q.mat.SetFloat("_Smoothness", q.suav); q.mat.SetFloat("_Glossiness", q.suav);
        if (!urpOn && q.metal < 0.5f && !q.refl) { float sm = Mathf.Min(q.suav, 0.18f); q.mat.SetFloat("_Smoothness", sm); q.mat.SetFloat("_Glossiness", sm); }
        if (!urpOn && q.metal < 0.5f) { q.mat.SetFloat("_SpecularHighlights", 0f); q.mat.EnableKeyword("_SPECULARHIGHLIGHTS_OFF"); q.mat.SetFloat("_GlossyReflections", q.refl ? 1f : 0f); if (q.refl) q.mat.DisableKeyword("_GLOSSYREFLECTIONS_OFF"); else q.mat.EnableKeyword("_GLOSSYREFLECTIONS_OFF"); }
        if (urpOn && q.metal < 0.5f && q.sDef < 0.2f) { q.mat.SetFloat("_SpecularHighlights", 0f); q.mat.EnableKeyword("_SPECULARHIGHLIGHTS_OFF"); }
        if (urpOn) { if (!q.refl) { q.mat.SetFloat("_EnvironmentReflections", 0f); q.mat.EnableKeyword("_ENVIRONMENTREFLECTIONS_OFF"); } else { q.mat.SetFloat("_EnvironmentReflections", 1f); q.mat.DisableKeyword("_ENVIRONMENTREFLECTIONS_OFF"); } }
    }
    void GuardarParte(int i) { var q = partes[i]; string k = "rulc1_" + i; PlayerPrefs.SetFloat(k + "_r", q.col.r); PlayerPrefs.SetFloat(k + "_g", q.col.g); PlayerPrefs.SetFloat(k + "_b", q.col.b); PlayerPrefs.SetFloat(k + "_s", q.suav); PlayerPrefs.SetInt(k + "_f", q.refl ? 1 : 0); PlayerPrefs.Save(); }
    void ReiniciarParte(int i) { var q = partes[i]; q.col = q.def; q.suav = q.sDef; q.refl = q.rDef; string k = "rulc1_" + i; foreach (var s in new[] { "_r", "_g", "_b", "_s", "_f" }) PlayerPrefs.DeleteKey(k + s); AplicarParte(q); }
    void Embellecer()
    {
        urpOn = UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline != null; var sh = urpOn ? Shader.Find("Universal Render Pipeline/Lit") : Shader.Find("Standard"); if (sh == null) return;
        if (partes == null)
        {
            Parte P(string n, Color c, float s, float me, bool rf) { return new Parte { nom = n, def = c, col = c, sDef = s, suav = s, metal = me, rDef = rf, refl = rf }; }
            partes = new[] { P("PLATO", Col(0.35f, 0.17f, 0.07f), 0.85f, 0f, false), P("BORDE", Col(0.11f, 0.04f, 0.015f), 0.82f, 0f, false), P("CONO", Col(0.3f, 0.12f, 0.04f), 0.85f, 0f, false), P("DORADO", new Color(1f, 0.82f, 0.4f), 0.6f, 1f, true),
                P("ROJO", new Color(0.5f, 0.012f, 0.02f), 0.04f, 0f, false), P("NEGRO", new Color(0.018f, 0.018f, 0.022f), 0.06f, 0f, false), P("VERDE", new Color(0f, 0.34f, 0.1f), 0.04f, 0f, false), P("NUMEROS", new Color(0.98f, 0.96f, 0.9f), 0.08f, 0f, false) };
            if (PlayerPrefs.GetInt("rulc2_m", 0) < 1) { for (int i = 4; i <= 6; i++) foreach (var sf in new[] { "_r", "_g", "_b", "_s", "_f" }) PlayerPrefs.DeleteKey("rulc1_" + i + sf); PlayerPrefs.SetInt("rulc2_m", 1); PlayerPrefs.Save(); }   // casilleros mas intensos (estaban palidos)
            for (int i = 0; i < partes.Length; i++) { string k = "rulc1_" + i; if (PlayerPrefs.HasKey(k + "_r")) { partes[i].col = new Color(PlayerPrefs.GetFloat(k + "_r"), PlayerPrefs.GetFloat(k + "_g"), PlayerPrefs.GetFloat(k + "_b"), 1f); partes[i].suav = PlayerPrefs.GetFloat(k + "_s", partes[i].sDef); partes[i].refl = PlayerPrefs.GetInt(k + "_f", partes[i].rDef ? 1 : 0) == 1; } }
        }
        foreach (var q in partes) { q.mat = new Material(sh); q.mat.SetFloat("_Metallic", q.metal); AplicarParte(q); }
#if true   // el simulador usa el mismo aspecto que la web
        { bool rl = modelo == 1; var su = Shader.Find("Unlit/Color"); if (su != null) { var cc = rl ? new[] { new Color(0.62f, 0.03f, 0.07f), new Color(0.015f, 0.015f, 0.02f), new Color(0f, 0.36f, 0.12f), new Color(1f, 0.86f, 0.45f) } : new[] { new Color(0.8f, 0.06f, 0.09f), new Color(0.045f, 0.045f, 0.05f), new Color(0f, 0.48f, 0.16f), new Color(0.98f, 0.96f, 0.9f) }; for (int i = 4; i <= 7; i++) { partes[i].mat = new Material(su); partes[i].col = cc[i - 4]; partes[i].mat.SetColor("_Color", cc[i - 4]); } }
          if (rl) { partes[0].col = new Color(0.2f, 0.07f, 0.025f); partes[1].col = new Color(0.06f, 0.02f, 0.008f); partes[2].col = new Color(0.16f, 0.06f, 0.025f); } else { for (int i = 0; i < 3; i++) partes[i].col = partes[i].def; } for (int i = 0; i < 3; i++) AplicarParte(partes[i]); }
#endif
        if (clipLoop != null) clipLoop.LoadAudioData();   // que el sonido del giro este cargado antes de tirar
        var perla = new Material(sh); perla.SetColor("_BaseColor", new Color(0.97f, 0.97f, 0.95f)); perla.SetColor("_Color", new Color(0.97f, 0.97f, 0.95f)); perla.SetFloat("_Metallic", 0f); perla.SetFloat("_Smoothness", 0.95f); perla.SetFloat("_Glossiness", 0.95f);
        foreach (var r in raiz.GetComponentsInChildren<Renderer>(true))
        {
            string n = Limpio(r.name);
            if (n == "BOLA" || n == "bola") r.sharedMaterial = perla;
            else if (n == "cuenco") r.sharedMaterial = partes[1].mat; else if (n == "pista_bola") r.sharedMaterial = partes[0].mat; else if (n == "cono_madera") r.sharedMaterial = partes[2].mat;
            else if (n.StartsWith("aro_") || n.StartsWith("rombo_") || n.StartsWith("brazo_") || n.StartsWith("bolita_brazo") || n == "cubo_central" || n == "perilla" || n.StartsWith("separador_")) r.sharedMaterial = partes[3].mat;
            else if (n.StartsWith("casillero_")) { var o = r.sharedMaterial; r.sharedMaterial = o != null && o.name.StartsWith("verde") ? partes[6].mat : (o != null && o.name.StartsWith("rojo") ? partes[4].mat : partes[5].mat); }
            else if (n.StartsWith("num_")) { r.sharedMaterial = partes[7].mat; var c0 = r.bounds.center; r.transform.localScale *= (modelo == 1 ? 1.08f : 1.4f); r.transform.position += c0 - r.bounds.center; }   // numeros mas grandes y mates
        }
        var pg = new GameObject("RuletaReflejo"); pg.transform.SetParent(raiz.transform, false); var pr = pg.AddComponent<ReflectionProbe>(); pr.mode = UnityEngine.Rendering.ReflectionProbeMode.Custom; pr.customBakedTexture = CuartoEstudio(); pr.size = new Vector3(40f, 40f, 40f); pr.importance = 10; pr.intensity = 0.8f; pr.boxProjection = false;
    }

    void PonerCamara()
    {
        Vector3 d = vista == 0 ? new Vector3(0f, 7.6f, -9.2f) : (vista == 1 ? new Vector3(0f, 10.4f, -0.01f) : new Vector3(0f, 7.0f, -4.3f));
        cam.fieldOfView = vista == 1 ? 34f : (vista == 2 ? 40f : 38f); fovBase = cam.fieldOfView;
#if true   // el simulador usa el mismo aspecto que la web
        if (enJuego) { if (!ambGuardado) { ambGuardado = true; ambModo = RenderSettings.ambientMode; ambLuz = RenderSettings.ambientLight; ambInt = RenderSettings.ambientIntensity; refInt = RenderSettings.reflectionIntensity; } RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat; RenderSettings.ambientLight = new Color(0.5f, 0.5f, 0.5f); }   // dentro del juego: la luz ambiente del avion no tine la ruleta
        { bool costado = vista == 0; if (luz != null) luz.intensity = costado ? 1.4f : 0.8f; RenderSettings.ambientIntensity = costado ? 1f : 0.4f; RenderSettings.reflectionIntensity = costado ? 1f : 0.35f; }   // el brillo queda solo en la vista de costado; en las demas, colores mas opacos
#endif
        camGO.transform.position = CENTRO + d * S; camGO.transform.LookAt(CENTRO + Vector3.up * 0.3f * S + (vista == 2 ? Vector3.forward * 0.35f * S : Vector3.zero), vista == 1 ? Vector3.forward : Vector3.up);
    }

    void Cerrar()
    {
        if (ambGuardado) { ambGuardado = false; RenderSettings.ambientMode = ambModo; RenderSettings.ambientLight = ambLuz; RenderSettings.ambientIntensity = ambInt; RenderSettings.reflectionIntensity = refInt; }
        abierta = false; if (!enJuego) Time.fixedDeltaTime = oldDt; if (suscripta) { RenderPipelineManager.beginCameraRendering -= AlCamara; suscripta = false; } if (otrasL != null) { for (int i = 0; i < otrasL.Count; i++) if (otrasL[i] != null) otrasL[i].cullingMask = otrasM[i]; otrasL = null; } rends.Clear(); if (mesa != null) mesa.AlCerrar(); RuletaMesa.VW = 0f; if (rt != null) { rt.Release(); Destroy(rt); rt = null; }
        if (escena.IsValid()) { SceneManager.UnloadSceneAsync(escena); escena = default; }
        if (audLoop) audLoop.Stop(); if (paredGO) Destroy(paredGO); if (raiz) Destroy(raiz); if (bola) Destroy(bola.gameObject); if (camGO) Destroy(camGO); if (luz) Destroy(luz.gameObject);
    }

    // las superficies del FBX son de un solo lado: se duplican con la cara invertida para que se vean y choquen desde ambos lados
    // piso del casillero con espesor (cuerpo convexo) para que la bolita no lo atraviese
    static Mesh Engrosar(Mesh m, Transform t)
    {
        var v = m.vertices; int nv = v.Length; Vector3 dn = t.InverseTransformDirection(Vector3.down).normalized * (0.1f / Mathf.Max(0.0001f, t.lossyScale.x)); Vector3 upL = -dn.normalized;
        for (int i = 0; i < nv; i++) { Vector3 pl = v[i] - upL * Vector3.Dot(v[i], upL); if (pl.magnitude < 1.95f) v[i] -= pl.normalized * 0.35f; else if (pl.magnitude > 2.3f) v[i] += pl.normalized * 0.2f; }   // el borde interno llega por debajo del cono
        var v2 = new Vector3[nv * 2]; for (int i = 0; i < nv; i++) { v2[i] = v[i]; v2[i + nv] = v[i] + dn; }
        var r = new Mesh { name = m.name + "_grueso" }; r.vertices = v2; int nt = (nv * 2) / 3 * 3; var tri = new int[nt]; for (int i = 0; i < nt; i++) tri[i] = i; r.triangles = tri; return r;
    }

    // las paredcitas entre numeros: solo en la fisica se las hace mas altas y mas gruesas para que la bolita no las salte
    void AltoSep(BoxCollider c, Transform t)
    {
        Vector3 rad = t.position - baseT.position; rad.y = 0f; rad = rad.sqrMagnitude < 1e-6f ? Vector3.right : rad.normalized; Vector3 tan = Vector3.Cross(Vector3.up, rad);
        int ia = 0, it = 0; float ba = -1f, bt = -1f;
        for (int i = 0; i < 3; i++) { Vector3 ax = i == 0 ? Vector3.right : (i == 1 ? Vector3.up : Vector3.forward); Vector3 w = t.TransformDirection(ax).normalized; float da = Mathf.Abs(Vector3.Dot(w, Vector3.up)), dt2 = Mathf.Abs(Vector3.Dot(w, tan)); if (da > ba) { ba = da; ia = i; } if (dt2 > bt) { bt = dt2; it = i; } }
        if (ia == it) return;
        Vector3 sz = c.size, ce = c.center; float up = Vector3.Dot(t.TransformDirection(ia == 0 ? Vector3.right : (ia == 1 ? Vector3.up : Vector3.forward)).normalized, Vector3.up) >= 0f ? 1f : -1f;
        float nuevo = sz[ia] * 2.6f; ce[ia] += up * (nuevo - sz[ia]) * 0.5f; sz[ia] = nuevo; sz[it] *= 1.8f; c.size = sz; c.center = ce;
    }

    static Mesh DobleCara(Mesh m)
    {
        var v = m.vertices; var nr = m.normals; var uv = m.uv; var tr = m.triangles; int nv = v.Length;
        var v2 = new Vector3[nv * 2]; var n2 = new Vector3[nv * 2]; var u2 = new Vector2[nv * 2]; var t2 = new int[tr.Length * 2];
        for (int i = 0; i < nv; i++) { v2[i] = v[i]; v2[i + nv] = v[i]; n2[i] = nr.Length > i ? nr[i] : Vector3.up; n2[i + nv] = -n2[i]; if (uv.Length > i) { u2[i] = uv[i]; u2[i + nv] = uv[i]; } }
        for (int i = 0; i < tr.Length; i += 3) { t2[i] = tr[i]; t2[i + 1] = tr[i + 1]; t2[i + 2] = tr[i + 2]; t2[tr.Length + i] = tr[i] + nv; t2[tr.Length + i + 1] = tr[i + 2] + nv; t2[tr.Length + i + 2] = tr[i + 1] + nv; }
        var r = new Mesh { name = m.name + "_2caras" }; r.vertices = v2; r.normals = n2; r.uv = u2; r.subMeshCount = m.subMeshCount;
        if (m.subMeshCount <= 1) r.triangles = t2; else r.triangles = t2;
        return r;
    }

    static Transform Buscar(Transform r, string n) { foreach (var t in r.GetComponentsInChildren<Transform>(true)) if (Limpio(t.name) == n) return t; return n == "RULETA_base" ? r : null; }   // Unity funde el nodo raiz del FBX con el objeto principal

    void PonerBolaEnPista(float ang)
    {
        Vector3 rad = new Vector3(Mathf.Cos(ang), 0f, Mathf.Sin(ang));
        Vector3 pos = baseT.position + (rad * 3.36f + Vector3.up * 0.7f) * S;
        var ip = rbBola.interpolation; rbBola.interpolation = RigidbodyInterpolation.None;
        bola.position = pos; bola.rotation = Quaternion.identity; rbBola.position = pos; rbBola.rotation = Quaternion.identity; Physics.SyncTransforms(); rbBola.interpolation = ip;
        if (!rbBola.isKinematic) { rbBola.linearVelocity = Vector3.zero; rbBola.angularVelocity = Vector3.zero; }
    }

    void RehacerAudio()
    {
        if (camGO == null) return;
        if (audLoop != null) Destroy(audLoop); if (audCae != null) Destroy(audCae); if (audGolpe != null) Destroy(audGolpe);
        AudioListener.pause = false; AudioListener.volume = 1f;
        audLoop = camGO.AddComponent<AudioSource>(); audLoop.clip = clipLoop; audLoop.loop = true; audLoop.playOnAwake = false; audLoop.spatialBlend = 0f;
        audCae = camGO.AddComponent<AudioSource>(); audCae.clip = clipCae; audCae.playOnAwake = false; audCae.spatialBlend = 0f;
        audGolpe = camGO.AddComponent<AudioSource>(); audGolpe.clip = clipGolpe; audGolpe.playOnAwake = false; audGolpe.spatialBlend = 0f;
    }
    public void Lanzar()
    {
        // reinicio de la rueda
        bola.SetParent(null, true); parando = false; frenarPlato = false; tCayo = 0f; sus = false; capCol = null; capN = -1; enganchada = false; tEng = 0f; tCapt = 0f; sonoNumero = false;
        { float q = Azar(); float ps = Mathf.Clamp01(PS[Fis] + (cans - 0.5f) * 0.5f); rebZona = q < ps ? 0.06f : (q < ps + (1f - ps) * 0.6f ? 0.3f : 0.5f); vEnc = (VE0[Fis] + Azar() * (VE1[Fis] - VE0[Fis])) * Mathf.Max(0.4f, 1.3f - 0.6f * cans); }   // la mayoria cae sin rebote; a veces 1 o 2, y a veces viene con fuerza, rebota y sale
        matR.bounciness = FR[Fis]; matR.dynamicFriction = 0.004f; matR.staticFriction = 0.006f;   // en la pista casi sin roce: las vueltas las manda el deslizador
        if (opcion != 0) { float sg = wObj > 0f ? -1f : 1f; wObj = sg * (0.95f + Azar() * 0.35f); }   // el plato sigue girando: no se reinicia, solo cambia de sentido
        else { giro.localRotation = giroRot0; rbGiro.rotation = giro.rotation; wRueda = wBase * (0.85f + Azar() * 0.3f); }
        float ang = Azar() * Mathf.PI * 2f;
        rbBola.isKinematic = true; PonerBolaEnPista(ang); rbBola.isKinematic = false;
        Vector3 rad = new Vector3(Mathf.Cos(ang), 0f, Mathf.Sin(ang));
        Vector3 tang = Vector3.Cross(Vector3.up, rad).normalized;           // la bolita gira en sentido contrario a la rueda
        float v = v0 * (0.92f + Azar() * 0.16f) * S;
        amort = Mathf.Clamp((v0 - 6f) / (Mathf.Max(0.5f, vueltas) * 21.4f), 0.04f, 3f);   // freno que da, aproximadamente, las vueltas pedidas antes de caer
        rbBola.linearVelocity = tang * v; rbBola.angularVelocity = Vector3.zero;
        rbBola.linearDamping = amort; rbBola.angularDamping = 0.03f;
        lanzada = true; quieta = false; tQuieto = 0f; tLanzada = 0f; tZona = 0f; resultado = -1; cayo = false;
        RehacerAudio();   // cada tirada arranca con fuentes de audio nuevas: despues de muchas jugadas el sonido se apagaba y habia que cerrar y abrir
        if (audCae != null) audCae.Stop(); if (audGolpe != null) audGolpe.Stop(); if (audLoop != null && clipLoop != null) { audLoop.volume = VOL_GIRO; audLoop.Play(); }
        if (opcion < 0) opcion = PlayerPrefs.GetInt("rul19_caida", 6); var colB = bola.GetComponent<SphereCollider>(); if (colB != null) colB.enabled = opcion < 1;
        if (opcion >= 1) IniciarSim(ang);
    }

    public int CapN { get { return capN; } } public float TCayoP { get { return tCayo; } } public float Cans { set { cans = value; } } public int Resultado { get { return resultado; } } public bool Lanzada { get { return lanzada; } } public float TLanzada { get { return tLanzada; } } public Vector3 PosBola { get { return rbBola.position; } } public Vector3 CentroBase { get { return baseT.position; } } public float Escala { get { return S; } } public float V0 { set { v0 = value; } } public float Amort { set { amort = value; } }
    public void Diag()
    {
        var cu = Buscar(raiz.transform, "cuenco"); var rd = cu.GetComponent<Renderer>(); var mc = cu.GetComponent<MeshCollider>();
        Debug.Log("[TEST] cuenco bounds=" + rd.bounds.size + " centro=" + rd.bounds.center + " raizRot=" + raiz.transform.eulerAngles + " baseT=" + baseT.position + " collider=" + (mc != null) + " bola=" + bola.position + " kin=" + rbBola.isKinematic);
        Vector3 o = baseT.position + Vector3.up * 3f; for (int k = 0; k < 9; k++) { Vector3 d = new Vector3(0f, 0f, (0.6f + k * 0.3f) * S); if (Rc(baseT.position + d + Vector3.up * 3f * S, Vector3.down, out var h, 10f * S)) Debug.Log("[TEST] rayo r=" + (d.z / S) + " pega en " + h.collider.name + " y=" + (h.point.y - baseT.position.y) / S); else Debug.Log("[TEST] rayo r=" + (d.z / S) + " NO pega"); }
    }
    public void SoltarEn(float r, float w) { wBase = w; wRueda = w; rbBola.isKinematic = true; bola.position = baseT.position + new Vector3(r * S, 0.6f * S, 0f); rbBola.isKinematic = false; rbBola.linearVelocity = Vector3.zero; rbBola.angularVelocity = Vector3.zero; rbBola.linearDamping = 0.1f; lanzada = true; tLanzada = 0f; }
    public void Captura(string ruta, int w, int h) { var r2 = new RenderTexture(w, h, 24); cam.rect = new Rect(0, 0, 1, 1); cam.targetTexture = r2; cam.Render(); RenderTexture.active = r2; var t = new Texture2D(w, h, TextureFormat.RGB24, false); t.ReadPixels(new Rect(0, 0, w, h), 0, 0); t.Apply(); RenderTexture.active = null; cam.targetTexture = null; System.IO.File.WriteAllBytes(ruta, t.EncodeToPNG()); }
    public void Cerrar2() { if (raiz) DestroyImmediate(raiz); if (paredGO) DestroyImmediate(paredGO); if (bola) DestroyImmediate(bola.gameObject); if (camGO) DestroyImmediate(camGO); if (luz) DestroyImmediate(luz.gameObject); }
    public float Vueltas { set { vueltas = value; } }
    public float VelBola { get { return rbBola.linearVelocity.magnitude; } }
    public int Res { get { return resultado; } } public void Latido() { ultDibujo = Time.frameCount; }
    public Camera Cam { get { return cam; } }
    public void Config(int m, int v) { modelo = m; vista = v; }
    public void VerVista(int v) { vista = v; PonerCamara(); }
    public void ActivarLog() { bola.gameObject.AddComponent<ColLog>(); }
    public void Diag2()
    {
        foreach (var c in raiz.GetComponentsInChildren<Collider>()) { string n = Limpio(c.name); if (n == "cono_madera" || n == "cubo_central" || n == "casillero_0" || n == "casillero_32" || n == "aro_bronce" || n == "separador_0") Debug.Log("[TEST] col " + n + " bounds=" + c.bounds.center + " " + c.bounds.size + " tipo=" + c.GetType().Name + (c is MeshCollider mc ? " tris=" + (mc.sharedMesh != null ? mc.sharedMesh.triangles.Length / 3 : -1) : "")); }
        Debug.Log("[TEST] giro pos=" + giro.position + " rb.kin=" + rbGiro.isKinematic + " hijos=" + giro.childCount);
    }
    public void PasoFisico() { Paso(Time.fixedDeltaTime); }
    void FixedUpdate() { if (!enJuego) Paso(Time.fixedDeltaTime); }
    void Update() { if (!enJuego || !abierta) return; if (Time.frameCount - ultDibujo > 5) { Cerrar(); return; } Tick(Time.deltaTime); }   // si la tablet deja de dibujarla, se cierra sola
    public void Tick(float d) { acc += d; int n = 0; while (acc >= DTF && n < 24) { Paso(DTF); if (fisica.IsValid()) fisica.Simulate(DTF); acc -= DTF; n++; } if (acc > DTF) acc = 0f; }   // fisica fina (480 por segundo) solo para la ruleta
    void Paso(float dtp)
    {
        if (!abierta) return;
        float dt = dtp;
        if (rbGiro != null) { rbGiro.MoveRotation(Quaternion.AngleAxis(-wRueda * Mathf.Rad2Deg * dt, Vector3.up) * rbGiro.rotation); if (opcion != 0) { if (wObj == 0f) { wObj = 1.1f; wRueda = wObj; } wRueda = Mathf.MoveTowards(wRueda, (parando || frenarPlato) ? 0f : wObj, 1.2f * dt); wObj *= 1f - 0.004f * dt; } else { wRueda *= 1f - (parando ? 4f : 0.02f) * dt; if (parando && wRueda < 0.01f) wRueda = 0f; } }
        if (lanzada && simOn) { PasoSim(dt); return; }
        if (lanzada && !rbBola.isKinematic)
        {
            rbBola.AddForce(Vector3.down * G * S * rbBola.mass * (cayo ? FG[Fis] * (cans <= 1f ? 1f - 0.6f * cans : 0.4f - 0.1f * (cans - 1f)) : 1f), ForceMode.Force);   // cansada: cae mas despacio
            if (cayo)   // la bolita se va "pegando" a la velocidad de la rueda de a poco: con mucho cansancio lo hace enseguida y baja rodando suave
            {
                Vector3 vw = rbBola.linearVelocity - Vector3.Cross(Vector3.up * -wRueda, rbBola.position - baseT.position); vw.y = 0f;
                rbBola.AddForce(-vw * (0.6f + 5f * cans) * rbBola.mass, ForceMode.Force);
            }
            tLanzada += dt;
            Vector3 loc = (rbBola.position - baseT.position) / S;
            float r = new Vector2(loc.x, loc.z).magnitude;
            if (!cayo && r < 2.9f) { cayo = true; matR.dynamicFriction = FF[Fis]; matR.staticFriction = FF[Fis] * 1.3f; matR.bounciness = rebZona; rbBola.linearDamping = FD[Fis]; if (audLoop != null) audLoop.Stop(); if (audGolpe != null && clipGolpe != null) audGolpe.Play(); }   // sale de la pista: se corta el giro y suenan los rebotes
            if (r > 3.6f || loc.y > 1.6f)    // se escapo del plato: se la devuelve a la zona de numeros
            { Vector3 d = new Vector3(loc.x, 0f, loc.z); d = d.sqrMagnitude < 1e-4f ? Vector3.right : d.normalized; rbBola.position = baseT.position + (d * 2.5f + Vector3.up * 0.35f) * S; rbBola.linearVelocity = Vector3.zero; rbBola.angularVelocity = Vector3.zero; }
            if (cayo) tCayo += dt;
            // piso de un numero: se anota en que hueco esta (si rebota y sale, se anota el siguiente)
            if (!enganchada && cayo && r < 2.75f && Rc(rbBola.position, Vector3.down, out var hc, 0.6f * S) && Limpio(hc.collider.name).StartsWith("casillero_") && hc.distance < 0.3f * S)
            { capCol = hc.collider as MeshCollider; int.TryParse(Limpio(hc.collider.name).Substring(10), out capN); if (!sonoNumero) { sonoNumero = true; if (audCae != null && clipCae != null) audCae.Play(); } }
            if (r < 2.75f && loc.y < 0.45f) tZona += dt;
            if (capCol != null && resultado < 0)
            {
                Vector3 vr = rbBola.linearVelocity - Vector3.Cross(Vector3.up * -wRueda, rbBola.position - baseT.position); vr.y = 0f;
                // se engancha solo cuando ya va cansada (poca velocidad respecto de la rueda); si viene con fuerza rebota, sale y puede caer en otro numero
                tCapt += dt;
                if (!enganchada && ((vr.magnitude < vEnc * S && tCapt > 0.3f + 1.5f * cans) || tZona > 5f)) { enganchada = true; tEng = 0f; }
                if (!enganchada)   // las paredcitas: desde que toca el piso de un numero, un resorte suave lo mantiene en ese hueco
                {
                    Vector3 c0 = capCol.bounds.center; Vector3 d0 = c0 - baseT.position; d0.y = 0f; c0 = baseT.position + d0.normalized * Mathf.Min(d0.magnitude, 2.45f * S) + Vector3.up * c0.y;
                    Vector3 e0 = c0 - rbBola.position; e0.y = 0f;
                    rbBola.AddForce(e0 * 30f * S * rbBola.mass, ForceMode.Force);
                }
                if (enganchada)
                {
                    tEng += dt;
                    Vector3 c = capCol.bounds.center; Vector3 dd = c - baseT.position; dd.y = 0f; c = baseT.position + dd.normalized * Mathf.Min(dd.magnitude, 2.45f * S) + Vector3.up * c.y;
                    Vector3 e = c - rbBola.position; e.y = 0f;
                    rbBola.AddForce((e * 70f - vr * 20f) * S * rbBola.mass, ForceMode.Force);   // se deja caer suave al centro del hueco
                }
            }
            if (((enganchada && tEng > 0.4f) || (cayo && tCayo > 9f) || tLanzada > 40f) && resultado < 0) { Decidir(); Clavar(); }
        }
    }

    // ---------------------------------------------------------------- CAIDA REALISTA (sacada de un video de casino, cuadro por cuadro)
    // 1) la bolita gira pegada a la pista y se va frenando (al principio ~2 vueltas por segundo, al final ~1 vuelta cada 2 s)
    // 2) cuando ya no le alcanza la velocidad, se despega y baja en espiral por el cono (~0,6 s, un cuarto/un tercio de vuelta), acelerando un poco
    //    si en la bajada pega en un rombo, salta y pierde velocidad
    // 3) entra a los numeros: pega contra las paredcitas, salta, a veces rebota para atras, se va muriendo y queda en un hueco
    // 4) queda en el hueco y sigue girando con el plato
    public static bool simLog; public int SimFase { get { return simOn ? simFase : 9; } } int simGolpes, simRomb; float simVueltas;
    void SLog(string m) { if (simLog) Debug.Log("[CAIDA] op" + opcion + " t=" + simT.ToString("0.00") + " " + m); }
    float simS = -1f; public static int simTipo;
    bool simOn; int simFase; float simTh, simW, simR, simVr, simY, simVy, simT, simTFase, simL, simAT, simA2, simMu, simC0, simC1, simWc, simRT, simRP, simRRing, simRRomb, simRombHW, simBr, simPsi, simY0, simRotPrev, simWR, simPrevSurf;
    int simLock = -1; float simTLock; float[] simOff = new float[37]; float simRefOff, simSepOff; Vector3 simRefL; float[] simRombAng = new float[0]; float[] simRombCd = new float[0];
    struct Anillo { public float fr, vC, hop, kc, e, ey, vLock, pRev, eRev, vyMax, vyMin, tMin; }
    Anillo simAn;
    static float Wrap(float a) { while (a > Mathf.PI) a -= 2f * Mathf.PI; while (a < -Mathf.PI) a += 2f * Mathf.PI; return a; }
    float AngW(Vector3 p) { Vector3 d = p - baseT.position; return Mathf.Atan2(d.z, d.x); }
    float RotorAng() { return AngW(giro.TransformPoint(simRefL)); }
    void IniciarSim(float ang)
    {
        // medidas de la ruleta: casilleros, paredcitas y rombos (en angulo respecto del casillero 0, que gira con el plato)
        Vector3 c0 = Vector3.zero; MeshCollider sep0 = null; var cas = new Dictionary<int, Vector3>(); var romb = new List<Vector3>(); float rp = 0f; int nrp = 0; float hw = 0f;
        foreach (var c in raiz.GetComponentsInChildren<Collider>())
        {
            string nm = Limpio(c.name);
            if (nm.StartsWith("casillero_")) { int n; if (int.TryParse(nm.Substring(10), out n) && n >= 0 && n < 37) { cas[n] = c.bounds.center; Vector3 d = c.bounds.center - baseT.position; d.y = 0f; rp += d.magnitude / S; nrp++; } }
            else if (nm == "separador_0" && c is MeshCollider) sep0 = (MeshCollider)c;
        }
        foreach (var rd in raiz.GetComponentsInChildren<Renderer>()) { string nm = Limpio(rd.name); if (nm.StartsWith("rombo_")) { romb.Add(rd.bounds.center); hw = Mathf.Max(hw, Mathf.Max(rd.bounds.extents.x, rd.bounds.extents.z) / S); } }
        if (!cas.ContainsKey(0)) { simOn = false; return; }
        simRefL = giro.InverseTransformPoint(cas[0]); float a0 = AngW(cas[0]);
        for (int n = 0; n < 37; n++) simOff[n] = cas.ContainsKey(n) ? Wrap(AngW(cas[n]) - a0) : 0f;
        simSepOff = sep0 != null ? Wrap(AngW(sep0.bounds.center) - a0) : Mathf.PI / 37f;
        simRP = nrp > 0 ? Mathf.Min(rp / nrp, 2.45f) : 2.45f;
        simRombAng = new float[romb.Count]; simRombCd = new float[romb.Count]; float rr = 0f;
        for (int k = 0; k < romb.Count; k++) { simRombAng[k] = AngW(romb[k]); Vector3 d = romb[k] - baseT.position; d.y = 0f; rr += d.magnitude / S; }
        simRRomb = romb.Count > 0 ? rr / romb.Count : 3.0f; simRombHW = romb.Count > 0 ? Mathf.Clamp(hw * 0.6f, 0.05f, 0.18f) : 0.1f;
        var sc = bola.GetComponent<SphereCollider>(); simBr = sc != null ? sc.radius * bola.lossyScale.x / S : 0.08f;
        // las 5 caidas
        float T; simAT = 33f; simA2 = 35f; simMu = 0.2f;
        var casino = new Anillo { fr = 0.6f, vC = 2.5f, hop = 0.7f, kc = 0.88f, e = 0.35f, ey = 0.35f, vLock = 0.6f, pRev = 0.12f, eRev = 0.5f, vyMax = 7f, vyMin = 2f, tMin = 0.5f };
        int tipo = opcion; if (opcion == 6) { float q = Azar() * 100f; tipo = q < 25f ? 1 : q < 45f ? 2 : q < 65f ? 3 : q < 90f ? 4 : 5; } simTipo = tipo;   // AZAR: casino 25, larga 20, rombos 20, suave 25, loca 10
        switch (tipo)
        {
            case 2: T = 11f + Azar() * 3f; simAn = casino; simC1Larga = true; break;   // LARGA: un par de vueltitas lentas al final                                   // LARGA: como en el casino de verdad, ~20 s en la pista
            case 3: T = 10f + Azar() * 3f; simAn = casino; simA2 = 18f; simMu = 0.15f; simRombHW *= 1.6f; break;   // ROMBOS: baja mas lenta y pega mas en los rombos
            case 4: T = 10f + Azar() * 2f; simAn = new Anillo { fr = 1.5f, vC = 3.5f, hop = 0.55f, kc = 0.78f, e = 0.25f, ey = 0.25f, vLock = 0.8f, pRev = 0.05f, eRev = 0.4f, vyMax = 5f, vyMin = 1.5f, tMin = 0.4f }; break;   // SUAVE: cae y se queda enseguida
            case 5: T = 10f + Azar() * 3f; simAn = new Anillo { fr = 0.4f, vC = 2.0f, hop = 0.8f, kc = 0.9f, e = 0.45f, ey = 0.45f, vLock = 0.5f, pRev = 0.3f, eRev = 0.75f, vyMax = 9f, vyMin = 2.5f, tMin = 0.6f }; break;   // LOCA: rebota, vuelve para atras y cruza varios numeros
            default: T = 10f + Azar() * 3f; simAn = casino; break;                                // CASINO
        }
        simRT = 3.36f; simRRing = 2.78f;
        simWc = Mathf.Sqrt(simAT / simRT);
        float w0 = 2f * Mathf.PI * (1.85f + Azar() * 0.35f);
        simC1 = simC1Larga ? 0.16f : 0.06f; simC1Larga = false; float eT = Mathf.Exp(-simC1 * T); float kk = Mathf.Max(0f, (w0 * eT - simWc) / (1f - eT)); simC0 = kk * simC1;   // freno: roce constante + aire, para llegar a la velocidad justa en T segundos
        simS = wObj > 0f ? -1f : 1f; simTh = ang; simW = simS * w0; simR = simRT; simVr = 0f; simY = 0f; simVy = 0f; simT = 0f; simTFase = 0f; simFase = 0; simLock = -1; simTLock = 0f; simGolpes = 0; simRomb = 0; simVueltas = 0f;
        simRotPrev = RotorAng(); simWR = wRueda; simPsi = Mod(simTh - (simRotPrev + simSepOff), 2f * Mathf.PI / 37f); simPrevSurf = 0f;
        rbBola.isKinematic = true; simOn = true; cayo = false;
        ColocarSim(0f);
    }
    bool simC1Larga;
    static float Mod(float a, float m) { a %= m; return a < 0f ? a + m : a; }
    float simTh0;
    AudioClip[] golpesReal; AudioSource[] golpePool; int golpeIdx;
    void Golpe(float vol)   // golpecitos reales de la bolita, recortados del video del casino
    {
        simGolpes++;
        if (golpesReal == null) { var l = new List<AudioClip>(); for (int i = 1; i <= 12; i++) { var c = Resources.Load<AudioClip>("Ruleta/sonidos/rebotes/golpe_" + i); if (c != null) l.Add(c); } golpesReal = l.ToArray(); }
        if (camGO == null) return;
        if (golpePool == null || golpePool[0] == null) { golpePool = new AudioSource[6]; for (int i = 0; i < 6; i++) { golpePool[i] = camGO.AddComponent<AudioSource>(); golpePool[i].playOnAwake = false; golpePool[i].spatialBlend = 0f; } }
        var src = golpePool[golpeIdx++ % golpePool.Length]; src.Stop();
        if (golpesReal.Length > 0) { src.clip = golpesReal[(int)(Azar() * golpesReal.Length) % golpesReal.Length]; src.pitch = 0.92f + Azar() * 0.16f; src.volume = Mathf.Clamp01(vol); src.Play(); }
        else if (clipGolpe != null) src.PlayOneShot(clipGolpe, Mathf.Clamp01(vol));
    }
    void PasoSim(float dt)
    {
        simT += dt; simTFase += dt;
        float rot = RotorAng(); float dRot = Wrap(rot - simRotPrev); simRotPrev = rot; if (dt > 0f) simWR = Mathf.Lerp(simWR, dRot / dt, 0.2f);
        // salto (la bolita en el aire)
        if (simY > 0f || simVy > 0f) { simVy -= G * dt; simY += simVy * dt; if (simY <= 0f) { simY = 0f; if (Mathf.Abs(simVy) > 3f) { Golpe(Mathf.Abs(simVy) / 12f); simVy = -simVy * (simFase >= 2 ? simAn.ey : 0.3f); } else simVy = 0f; } }
        float wobble = 0f;
        if (simFase == 0)   // 1) en la pista
        {
            float w = Mathf.Abs(simW); w -= (simC0 + simC1 * w) * dt; simW = simS * Mathf.Max(0f, w);
            simTh += simW * dt; simR = simRT;
            if (w < simWc * 1.3f) wobble = 0.025f * (1f - (w - simWc) / (0.3f * simWc)) * Mathf.Sin(simT * 23f) * Mathf.Sin(simT * 7.3f);   // las ultimas vueltas ya "baila" en la pista
            if (audLoop != null) audLoop.pitch = 1f;   // sonido real de ruleta de casino: sin cambiarle el tono
            simVueltas += w * dt / (2f * Mathf.PI);
            if (simR * w * w < simAT) { SLog("se despega de la pista tras " + simVueltas.ToString("0.0") + " vueltas, w=" + w.ToString("0.00")); simFase = 1; simTFase = 0f; simL = simR * simR * w; simVr = 0f; }
        }
        else if (simFase == 1)   // 2) bajando en espiral por el cono
        {
            float a = simAT + simA2 * (simRT - simR); float w = simL / (simR * simR);
            simVr += (simR * w * w - a) * dt; simR += simVr * dt; if (simR > simRT) { simR = simRT; simVr = Mathf.Min(simVr, 0f); }
            simL *= Mathf.Exp(-simMu * dt); w = simL / (simR * simR); simW = simS * w; simTh += simW * dt;
            for (int k = 0; k < simRombAng.Length; k++)   // rombos
            {
                simRombCd[k] -= dt; if (simRombCd[k] > 0f) continue;
                float da = Wrap(simTh - simRombAng[k]) * simR;
                if (Mathf.Abs(da) < simRombHW + simBr && Mathf.Abs(simR - simRRomb) < simRombHW + simBr && simY < 0.05f)
                { simRomb++; SLog("ROMBO " + k); simRombCd[k] = 0.5f; simL *= 0.35f + Azar() * 0.35f; simVr = (Azar() - 0.35f) * 2.5f; simVy = 7f + Azar() * 9f; Golpe(0.8f); }
            }
            if (audLoop != null) audLoop.volume = VOL_GIRO * Mathf.Clamp01(1f - simTFase * 1.5f);
            if (simR <= simRRing) { SLog("entra a los numeros, w=" + simW.ToString("0.00") + " rotor=" + simWR.ToString("0.00")); simTh0 = simTh; simFase = 2; simTFase = 0f; cayo = true; if (audLoop != null) audLoop.Stop(); simVy = Mathf.Max(simVy, 4f + Azar() * 4f); simPsi = Mod(simTh - (rot + simSepOff), 2f * Mathf.PI / 37f); }
        }
        else if (simFase == 2)   // 3) entre los numeros: paredcitas, saltos y rebotes
        {
            float sep = 2f * Mathf.PI / 37f;
            simVr += (-40f * (simR - simRP) - 8f * simVr) * dt; simR += simVr * dt;
            float wr = simW - simWR; if (simY <= 0f) wr *= Mathf.Exp(-simAn.fr * dt);
            simW = wr + simWR; simTh += simW * dt;
            float np = Mod(simTh - (rot + simSepOff), sep);
            bool cruzo = (wr < 0f && np > simPsi + sep / 2f) || (wr > 0f && np < simPsi - sep / 2f);
            if (cruzo && simY < 0.04f)
            {
                float v = Mathf.Abs(wr) * simRP;
                if (v > simAn.vC && Azar() < simAn.pRev) { wr = -wr * simAn.eRev; simVy = Mathf.Min(simAn.vyMax, v * simAn.hop); Golpe(1f); }
                else if (v > simAn.vC) { simVy = Mathf.Min(simAn.vyMax, v * simAn.hop * (0.5f + Azar())); wr *= simAn.kc; Golpe(0.45f + v * 0.08f); }
                else { wr = -wr * simAn.e; simVy = Mathf.Min(simAn.vyMin, v * 0.6f); Golpe(0.25f + v * 0.1f); }
                simW = wr + simWR;
            }
            simPsi = Mod(simTh - (rot + simSepOff), sep);
            if (Mathf.Abs(simW - simWR) * simRP < simAn.vLock && simY <= 0f && simTFase > simAn.tMin || simTFase > 6f)
            {
                int mejor = 0; float md = 9f; for (int n = 0; n < 37; n++) { float d = Mathf.Abs(Wrap(simTh - (rot + simOff[n]))); if (d < md) { md = d; mejor = n; } }
                simLock = mejor; frenarPlato = true; SLog("queda en " + mejor + " tras " + simTFase.ToString("0.00") + "s entre numeros, golpes=" + simGolpes + " recorrio " + ((simTh - simTh0) / (2f * Mathf.PI)).ToString("0.00") + " vueltas (mundo)"); simFase = 3; simTFase = 0f; simTLock = 0f; capN = mejor; sonoNumero = true;
            }
        }
        else   // 4) en el hueco, girando con el plato
        {
            float meta = rot + simOff[simLock]; simTh = Mathf.LerpAngle(simTh * Mathf.Rad2Deg, meta * Mathf.Rad2Deg, 1f - Mathf.Exp(-8f * dt)) * Mathf.Deg2Rad;
            simR = Mathf.Lerp(simR, simRP, 1f - Mathf.Exp(-8f * dt)); simTLock += dt;
            if ((simTLock > 1.0f && Mathf.Abs(wRueda) < 0.02f) || simTLock > 4f) { wRueda = 0f; ColocarSim(0f); Decidir(); Clavar(); return; }   // se canta el numero con el plato ya quieto: la bolita no se mueve mas
        }
        ColocarSim(wobble);
        tLanzada += dt;
    }
    void ColocarSim(float wob)
    {
        float r = simR + wob; Vector3 rad = new Vector3(Mathf.Cos(simTh), 0f, Mathf.Sin(simTh));
        Vector3 xz = baseT.position + rad * r * S; float ySurf = simPrevSurf;
        if (Rc(xz + Vector3.up * 3f * S, Vector3.down, out var h, 6f * S) && !h.collider.name.StartsWith("separador")) ySurf = h.point.y - baseT.position.y;
        if (ySurf < simPrevSurf - 0.02f * S && simPrevSurf != 0f) simY += (simPrevSurf - ySurf) / S;   // si el piso baja de golpe (borde de los numeros), la bolita cae en el aire
        simPrevSurf = ySurf;
        Vector3 p = xz + Vector3.up * (ySurf + (simBr + simY) * S);
        Vector3 vieja = bola.position; bola.position = p; rbBola.position = p;
        Vector3 mov = p - vieja; mov.y = 0f; float dist = mov.magnitude; if (dist > 1e-5f) bola.Rotate(Vector3.Cross(Vector3.up, mov.normalized), dist / (simBr * S) * Mathf.Rad2Deg, Space.World);   // que ruede de verdad
    }

    // la bolita queda clavada en el casillero y acompana a la rueda; el plato frena solo
    void Clavar()
    {
        if (resultado < 0) return;
        Vector3 p = rbBola.position;
        foreach (var c in raiz.GetComponentsInChildren<MeshCollider>())
        {
            if (Limpio(c.name) != "casillero_" + resultado) continue;
            p = c.bounds.center; Vector3 dd = p - baseT.position; dd.y = 0f; if (dd.magnitude > 2.55f * S) p = baseT.position + dd.normalized * 2.45f * S + Vector3.up * p.y;
            { Vector3 hb = rbBola.position - p; hb.y = 0f; if (hb.magnitude < 0.5f * S) { p.x = rbBola.position.x; p.z = rbBola.position.z; } }   // si ya esta en el hueco se queda donde esta, sin saltar al costado
            if (c.Raycast(new Ray(p + Vector3.up * 2f * S, Vector3.down), out var h, 4f * S)) p.y = h.point.y + bola.GetComponent<SphereCollider>().radius * bola.lossyScale.x;
            break;
        }
        rbBola.linearVelocity = Vector3.zero; rbBola.angularVelocity = Vector3.zero; rbBola.isKinematic = true; bola.position = p; bola.SetParent(giro, true);
        parando = true; wRueda = 0f; simOn = false; if (audLoop != null) audLoop.Stop(); if (audCae != null && clipCae != null && !audCae.isPlaying && !sonoNumero) audCae.Play();   // pausa: plato y bolita quedan quietos
    }

    void Decidir()
    {
        if (resultado >= 0) return;
        int n = capN;
        if (n < 0 && Rc(rbBola.position + Vector3.up * 0.2f * S, Vector3.down, out var hit, 2f * S))
        { var nm = Limpio(hit.collider.name); if (nm.StartsWith("casillero_")) int.TryParse(nm.Substring(10), out n); }
        if (n < 0) { float mejor = 1e9f; foreach (var c in raiz.GetComponentsInChildren<MeshCollider>()) { string nm = Limpio(c.name); if (!nm.StartsWith("casillero_")) continue; Vector3 d = c.bounds.center - rbBola.position; d.y = 0f; if (d.sqrMagnitude < mejor) { mejor = d.sqrMagnitude; int.TryParse(nm.Substring(10), out n); } } }
        resultado = n; lanzada = false; historial.Insert(0, n); if (historial.Count > 12) historial.RemoveAt(historial.Count - 1);
    }

    static readonly Color[] MUESTRAS = { new Color(0.62f, 0.4f, 0.22f), new Color(0.45f, 0.27f, 0.14f), new Color(0.36f, 0.2f, 0.1f), new Color(0.72f, 0.5f, 0.3f), new Color(0.55f, 0.22f, 0.12f), new Color(0.25f, 0.13f, 0.08f), new Color(0.82f, 0.62f, 0.38f), new Color(0.12f, 0.07f, 0.05f) };
    void DibujarPaleta()
    {
        if (partes == null) return; var pn = new Rect(8f, 8f, 300f, 392f); GUI.color = new Color(0f, 0f, 0f, 0.82f); GUI.DrawTexture(pn, Texture2D.whiteTexture); GUI.color = Color.white;
        var sc = new GUIStyle(GUI.skin.label) { fontSize = 12, fontStyle = FontStyle.Bold }; sc.normal.textColor = Color.white; var sb = new GUIStyle(GUI.skin.button) { fontSize = 11, fontStyle = FontStyle.Bold };
        GUI.Label(new Rect(pn.x + 8, pn.y + 4, 240, 20), "COLORES  (tocá la parte)", sc); if (GUI.Button(new Rect(pn.xMax - 62, pn.y + 4, 56, 20), "CERRAR", sb)) paleta = false;
        for (int i = 0; i < partes.Length; i++) { var rb = new Rect(pn.x + 8 + (i % 4) * 71, pn.y + 30 + (i / 4) * 26, 68, 23); var gc = GUI.backgroundColor; GUI.backgroundColor = i == parteSel ? new Color(1f, 0.9f, 0.3f) : Color.white; if (GUI.Button(rb, partes[i].nom, sb)) parteSel = i; GUI.backgroundColor = gc; }
        var q = partes[parteSel]; float y = pn.y + 88; GUI.color = q.col; GUI.DrawTexture(new Rect(pn.x + 8, y, pn.width - 16, 26), Texture2D.whiteTexture); GUI.color = Color.white; y += 32;
        bool ch = false; string[] ln = { "Rojo", "Verde", "Azul" }; float[] v = { q.col.r, q.col.g, q.col.b };
        for (int c = 0; c < 3; c++) { GUI.Label(new Rect(pn.x + 8, y, 60, 20), ln[c], sc); float nv = GUI.HorizontalSlider(new Rect(pn.x + 70, y + 5, pn.width - 82, 16), v[c], 0f, 1f); if (nv != v[c]) { v[c] = nv; ch = true; } y += 24; }
        GUI.Label(new Rect(pn.x + 8, y, 60, 20), "Brillo", sc); { float ns = GUI.HorizontalSlider(new Rect(pn.x + 70, y + 5, pn.width - 82, 16), q.suav, 0f, 1f); if (ns != q.suav) { q.suav = ns; ch = true; } } y += 26;
        if (urpOn && GUI.Button(new Rect(pn.x + 8, y, pn.width - 16, 22), q.refl ? "Reflejo del cielo: SI" : "Reflejo del cielo: NO", sb)) { q.refl = !q.refl; ch = true; } y += 28;
        GUI.Label(new Rect(pn.x + 8, y, 200, 20), "Colores de madera listos:", sc); y += 22;
        for (int i = 0; i < MUESTRAS.Length; i++) { var rs = new Rect(pn.x + 8 + (i % 4) * 71, y + (i / 4) * 34, 66, 30); GUI.color = MUESTRAS[i]; GUI.DrawTexture(rs, Texture2D.whiteTexture); GUI.color = Color.white; if (GUI.Button(rs, GUIContent.none, GUIStyle.none)) { v[0] = MUESTRAS[i].r; v[1] = MUESTRAS[i].g; v[2] = MUESTRAS[i].b; ch = true; } }
        y += 72;
        if (ch) { q.col = new Color(v[0], v[1], v[2], 1f); AplicarParte(q); GuardarParte(parteSel); }
        if (GUI.Button(new Rect(pn.x + 8, y, 140, 24), "REINICIAR ESTA", sb)) ReiniciarParte(parteSel);
        if (GUI.Button(new Rect(pn.x + 152, y, 140, 24), "REINICIAR TODO", sb)) for (int i = 0; i < partes.Length; i++) ReiniciarParte(i);
    }
    void OnGUI() { if (enJuego) return; DibujarTodo(); }

    void DibujarTodo()
    {
        if (!enJuego) GUI.depth = -900;
        if (sB == null) { sB = new GUIStyle(GUI.skin.button) { fontSize = 15, fontStyle = FontStyle.Bold }; sT = new GUIStyle(GUI.skin.label) { fontSize = 18, fontStyle = FontStyle.Bold }; sT.normal.textColor = Color.white; }
        if (intro)
        {
            GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), fotoIntro, ScaleMode.ScaleAndCrop);
#if UNITY_WEBGL
            BotonEntrar();
#else
            var st = new GUIStyle(sT) { fontSize = 54, alignment = TextAnchor.MiddleCenter }; st.normal.textColor = new Color(1f, 0.9f, 0.55f);
            GUI.Label(new Rect(0, Screen.height * 0.12f, Screen.width, 80), "RULETA", st);
            if (GUI.Button(new Rect(Screen.width / 2 - 110, Screen.height * 0.8f, 220, 54), "ENTRAR", sB)) { intro = false; Abrir(); }
#endif
            return;
        }
        if (!abierta) {
#if RULETA_APARTE
            if (GUI.Button(new Rect(10, 10, 110, 26), "RULETA", sB)) Abrir();
#endif
            return; }
        if (mesa == null) mesa = new RuletaMesa();
        { AsegurarRT(); if (Event.current.type == EventType.Repaint) { var rr = RRul; float W = RuletaMesa.SW, H = RuletaMesa.SH; GUI.color = Color.black; GUI.DrawTexture(new Rect(0, 0, W, H), Texture2D.whiteTexture); GUI.color = Color.white;
#if UNITY_WEBGL
 cam.rect = new Rect(0f, 0f, 1f, 1f); GUI.DrawTexture(rr, rt, ScaleMode.StretchToFill); } }   // en la web la camara no respeta el recorte: renderiza justo al tamano de la ventana
#else
 cam.rect = new Rect(rr.x / W, 1f - rr.yMax / H, rr.width / W, rr.height / H); GUI.DrawTextureWithTexCoords(rr, rt, new Rect(rr.x / W, 1f - rr.yMax / H, rr.width / W, rr.height / H)); } }
#endif
        var e = Event.current; GUI.enabled = true;
#if UNITY_WEBGL
        const float xMod = 625f, xVis = 370f, vSt = 85f;   // en la web la barra de arriba se acomoda para que ACOMODAR quede a la vista
#else
        const float xMod = 320f, xVis = 510f, vSt = 100f;
#endif
        var matAntes = GUI.matrix; var colBarra = GUI.color; var enBarra = GUI.enabled;
        if (mesa != null && mesa.apostarGrande) { GUI.color = new Color(1f, 1f, 1f, 0f); GUI.enabled = false; }   // en el paño grande la barra de arriba no se ve ni recibe toques (los botones de la mesa quedaban encima y se apretaba REAL / SOLO RULETA sin querer)
#if UNITY_WEBGL
                if (!enJuego && GUI.Button(new Rect(255, 10, 110, 30), "ACOMODAR", sB)) { mesa.editando = true; config = false; }
#endif
        if (!enJuego && GUI.Button(new Rect(10, 10, 110, 30), "CERRAR", sB)) { Cerrar(); Descongelar(); return; }
#if !UNITY_WEBGL
        if (!enJuego && GUI.Toggle(new Rect(815, 10, 170, 30), config, "CONFIGURACION", sB) != config) config = !config;
#endif
        if (false) { if (GUI.Button(new Rect(410, 4, 88, 28), "ORDENAR", sB)) mesa.Restaurar(); if (GUI.Button(new Rect(502, 4, 100, 28), "ACOMODAR", sB)) { mesa.editando = true; config = false; }
            if (GUI.Button(new Rect(606, 4, 130, 28), modelo == 0 ? "RULETA: CLARA" : "RULETA: REAL", sB)) { modelo = 1 - modelo; Cerrar(); Abrir(); return; }   // las dos ruletas: la clara (numeros grandes) y la real de casino
            if (GUI.Button(new Rect(740, 4, 120, 28), RuletaMesa.modoPrueba ? "PRUEBA: SI" : "PRUEBA: NO", sB)) RuletaMesa.PonerPrueba(!RuletaMesa.modoPrueba); }   // siempre a la vista: ordena todo solo / muestra los nodos para acomodar
#if UNITY_WEBGL
        if (!enJuego && GUI.Toggle(new Rect(130, 10, 120, 30), mesa.cajaVis, "BOLETERIA", sB) != mesa.cajaVis) mesa.cajaVis = !mesa.cajaVis;
#else
        if (!enJuego && GUI.Button(new Rect(130, 10, 170, 30), lanzada ? "ruedan..." : "LANZAR BOLITA", sB) && !lanzada) Lanzar();
#endif
        if (!enJuego) { var sSolo = new GUIStyle(sB) { fontSize = 12 }; if (GUI.Toggle(new Rect(xMod, 10, 85, 30), soloRuleta, "SOLO RULETA", sSolo) != soloRuleta) PonerSolo(!soloRuleta); if (GUI.Toggle(new Rect(xMod + 90, 10, 85, 30), modelo == 1, "REAL", sB) != (modelo == 1)) { modelo = 1 - modelo; Cerrar(); Abrir(); return; } }
        if (!enJuego) { if (opcion < 0) opcion = PlayerPrefs.GetInt("rul19_caida", 6); GUI.Label(new Rect(10, 46, 76, 30), "CAIDA:", sB); string[] cn = { "CASINO", "LARGA", "ROMBOS", "SUAVE", "LOCA", "AZAR" }; for (int j = 0; j < 6; j++) { int i = j == 0 ? 6 : j; if (GUI.Toggle(new Rect(90 + j * 96, 44, 92, 30), opcion == i, cn[i - 1], sB) && opcion != i) { opcion = i; PlayerPrefs.SetInt("rul19_caida", i); } } if (opcion == 6 && simTipo > 0) GUI.Label(new Rect(90 + 6 * 96 + 6, 46, 200, 30), "salió: " + cn[simTipo - 1], sT); }
        if (!enJuego) { string[] vn = { "COSTADO", "ARRIBA", "CERCA" }; for (int i = 0; i < 3; i++) if (GUI.Toggle(new Rect(xVis + i * vSt, 10, vSt - 5f, 30), vista == i && !soloRuleta, vn[i], sB) && (vista != i || soloRuleta)) { if (soloRuleta) PonerSolo(false); vista = i; PonerCamara(); } }
#if !UNITY_WEBGL
        if (!lanzada && !enJuego) { if (GUI.Button(new Rect(1010, 10, 110, 30), "GIRAR X", sB)) Girar(Vector3.right); if (GUI.Button(new Rect(1125, 10, 110, 30), "GIRAR Z", sB)) Girar(Vector3.forward); if (GUI.Button(new Rect(1240, 10, 130, 30), "DAR VUELTA", sB)) Girar(Vector3.right, 180f); }
#endif
        GUI.matrix = matAntes; GUI.color = colBarra; GUI.enabled = enBarra;
        if (config) { GUI.Label(new Rect(10, 48, 360, 24), "Velocidad de lanzamiento: " + v0.ToString("0"), sT); v0 = GUI.HorizontalSlider(new Rect(10, 78, 280, 18), v0, 10f, 40f); }
        if (config) { GUI.Label(new Rect(10, 94, 360, 24), "Vueltas de recorrido: " + vueltas.ToString("0.0"), sT); vueltas = GUI.HorizontalSlider(new Rect(10, 124, 280, 18), vueltas, 0.5f, 12f); }
#if !UNITY_WEBGL
        if (!enJuego) GUI.Label(new Rect(10, 140, 300, 30), lanzada ? "..." : (resultado >= 0 ? "listo" : "tocá LANZAR BOLITA"), sT);
#endif
        { string[] fn = { "unica", "unica", "unica" }; if (config) GUI.Label(new Rect(310, 48, 250, 24), "Caída " + fn[Fis], sT); }   // el cartel del resultado ahora lo dibuja la mesa (movible)
        if (config) { GUI.Label(new Rect(10, 204, 420, 24), "Giro del plato: " + wBase.ToString("0.0") + " rad/s", sT); wBase = GUI.HorizontalSlider(new Rect(10, 234, 280, 18), wBase, 0f, 3.5f); }
        if (config) { if (GUI.Button(new Rect(10, 262, 280, 30), "CLAVAR ESTOS AJUSTES", sB)) GuardarPrefs(); }
#if !UNITY_WEBGL
        { string dg = "VERSION 17/11-B"; if (abierta && giro != null && baseT != null) dg += "  giro.up.y=" + giro.up.y.ToString("0.00") + "  base.up.y=" + baseT.up.y.ToString("0.00") + "  bolaY=" + ((bola.position.y - baseT.position.y) / S).ToString("0.00") + "  S=" + S.ToString("0.00") + "  hijos=" + raiz.GetComponentsInChildren<Collider>().Length; GUI.Label(new Rect(10, RuletaMesa.SH - 20, 900, 20), enJuego ? "V17/11-B" : dg, new GUIStyle(sT) { fontSize = enJuego ? 11 : 18 }); }
#endif
        if (config) { GUI.Label(new Rect(10, 338, 460, 24), "Cansancio al caer: " + cans.ToString("0.00") + (cans < 0.25f ? " (con fuerza)" : (cans > 0.75f ? (cans > 1.5f ? " (cansadisima)" : " (muy cansada)") : "")), sT); cans = GUI.HorizontalSlider(new Rect(10, 368, 280, 18), cans, 0f, 3f); }
        if (config) { { string[] pn = { "SIN PAÑO", "PAÑO 1", "PAÑO 2", "PAÑO 3" }; for (int i = 0; i < 4; i++) if (GUI.Toggle(new Rect(10 + i * 92, 302, 88, 28), pano == i, pn[i], sB)) pano = i; } }
        if (config) mesa.DibujarConfig(10, 400, sB, sT);
        if (mesa.editando) config = false;
        if (paleta) DibujarPaleta();
        if (soloRuleta)
        {
            if (e.type == EventType.Layout) { mesa.SoloTick(resultado, lanzada); if (autoSolo && !lanzada && resultado >= 0) { if (tSoloFin <= 0f) tSoloFin = Time.realtimeSinceStartup + 1.5f; else if (Time.realtimeSinceStartup > tSoloFin) { PonerSolo(false); } } }
            if (soloRuleta)
            {
                if (resultado >= 0 && !lanzada) { var sn = new GUIStyle(sT) { fontSize = 26, alignment = TextAnchor.MiddleLeft }; sn.normal.textColor = new Color(1f, 0.9f, 0.5f); GUI.Label(new Rect(10, 52, 260, 40), "Salió el " + resultado, sn); }
                var gb = GUI.backgroundColor; GUI.backgroundColor = new Color(1f, 0.85f, 0.2f);
                if (GUI.Button(new Rect(RuletaMesa.SW - 150f, RuletaMesa.SH - 42f, 142f, 34f), "VOLVER A LA MESA", new GUIStyle(sB) { fontSize = 12, fontStyle = FontStyle.Bold })) PonerSolo(false);
                GUI.backgroundColor = gb;
                if (soloRuleta && e.type == EventType.MouseUp && e.mousePosition.y > 50f && e.mousePosition.y < RuletaMesa.SH - 50f) { if (resultado >= 0 && !lanzada) PonerSolo(false); else if (resultado < 0 && !lanzada) Lanzar(); }
            }
        }
        else mesa.Dibujar(e, config, pano > 0 ? panos[pano] : null, sB, sT, resultado, lanzada);
        if (mesa.tirarPedido) { mesa.tirarPedido = false; if (!lanzada) { Lanzar(); PonerSolo(true); autoSolo = true; tSoloFin = 0f; } }   // al tirar, la ruleta pasa sola a verse de cerca y sin nada que moleste
        GUI.enabled = true;
        if (mesa.cerrarPedido) { mesa.cerrarPedido = false; Cerrar(); Descongelar(); return; }   // RETIRARSE: ya devolvió todo a la cuenta
#if !UNITY_WEBGL
        if (!enJuego) { string h = ""; foreach (var x in historial) h += x + "  "; GUI.Label(new Rect(10, 172, 700, 30), "ultimos: " + h, sT); }
#endif
        if (e.type == EventType.MouseDown || e.type == EventType.MouseUp || e.type == EventType.ScrollWheel) e.Use();
    }
}
