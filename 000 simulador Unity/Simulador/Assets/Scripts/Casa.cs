// LA CASA DEL JUGADOR (prueba 19/11 en el lote chico de Campo de Mayo que marco Pablo).
// El modelo sale de Blender: 000 simulador Unity/casas/casa_8x10.glb (casa 8 x 10 m sin techo + muebles). Cada mueble es un nodo "MUEBLE_xxx".
// Orientacion: en el glb el frente (puerta) mira a +Z local. El lote chico tiene la calle al OESTE (enfrente esta el terreno grande),
// asi que la casa se gira para que +Z apunte a rumbo 266 (oeste) y se planta 13 m adentro del borde del lote (queda jardin adelante).
// MODO DECORAR (como Los Sims): camara arriba de la casa; clic en un mueble lo agarra, se arrastra por el piso, R o el boton lo gira 90 grados,
// rueda del mouse acerca/aleja. Las posiciones se guardan en casas/casa_muebles.json.
using UnityEngine;
using System.Collections.Generic;
using CesiumForUnity;
using Unity.Mathematics;
using UnityEngine.InputSystem;

public class Casa : MonoBehaviour
{
    static Casa inst;
    // ---- donde va (centro de la casa) ----
    const double LAT = -34.5368104, LON = -58.6742135, ALT = 41.65;   // ALT: altura del piso (las marcas del lote dan 41,7 m)
    const float RUMBO_FRENTE = 266f;                                   // hacia donde mira la puerta (oeste, a la calle)
    static string Carpeta { get { return System.IO.Path.GetFullPath(System.IO.Path.Combine(Application.dataPath, "..", "..", "casas")); } }
    static string RutaGlb { get { return System.IO.Path.Combine(Carpeta, "casa_8x10.glb"); } }
    static string RutaShop { get { return System.IO.Path.Combine(Carpeta, "casa_shopping.glb"); } }   // muebles que se compran en el shopping ("Mi casa")
    static readonly Dictionary<string, string> TIENDA = new Dictionary<string, string> {
        { "MUEBLE_Sofa_01", "Sillon de 3 cuerpos" }, { "MUEBLE_mid_century_lounge_chair", "Sillon de lectura" },
        { "MUEBLE_potted_plant_01", "Planta grande" }, { "MUEBLE_ceiling_fan", "Ventilador de techo" } };
    static string RutaMuebles { get { return System.IO.Path.Combine(Carpeta, "casa_muebles.json"); } }

    // ---- ajuste fino de la casa (editor de la app Mi casa): metros al norte/este, altura y giro extra
    [System.Serializable] class AjusteJ { public float norte, este, alto, giro; }
    AjusteJ aj = new AjusteJ();
    static string RutaAjuste { get { return System.IO.Path.Combine(Carpeta, "casa_ajuste.json"); } }
    void LeerAjuste() { try { if (System.IO.File.Exists(RutaAjuste)) aj = JsonUtility.FromJson<AjusteJ>(System.IO.File.ReadAllText(RutaAjuste)) ?? new AjusteJ(); } catch { } }
    void GuardarAjuste() { try { System.IO.File.WriteAllText(RutaAjuste, JsonUtility.ToJson(aj, true)); } catch { } }
    double LatC { get { return LAT + aj.norte / 6371000.0 * 180.0 / System.Math.PI; } }
    double LonC { get { return LON + aj.este / (6371000.0 * System.Math.Cos(LAT * System.Math.PI / 180.0)) * 180.0 / System.Math.PI; } }
    void AplicarAjuste()
    {
        if (anc == null) return;
        anc.longitudeLatitudeHeight = new double3(LonC, LatC, ALT + aj.alto);
        anc.rotationEastUpNorth = Quaternion.Euler(0f, RUMBO_FRENTE + aj.giro, 0f);
    }
    static bool editarCasa; static float repProx; static string repId = "";
    static bool Rep(string id, bool apretado) { if (!apretado) { if (repId == id) repId = ""; return false; } if (repId != id) { repId = id; repProx = Time.unscaledTime + 0.35f; return true; } if (Time.unscaledTime >= repProx) { repProx = Time.unscaledTime + 0.06f; return true; } return false; }
    void Fila(string titulo, string id, ref float v, float paso, string unidad, GUIStyle txt, GUIStyle btn)
    {
        GUILayout.BeginHorizontal();
        GUILayout.Label(titulo + ": " + v.ToString("0.00") + " " + unidad, txt, GUILayout.Width(190));
        if (Rep(id + "-", GUILayout.RepeatButton("-", btn, GUILayout.Width(44), GUILayout.Height(30)))) { v -= paso; AplicarAjuste(); GuardarAjuste(); }
        if (Rep(id + "+", GUILayout.RepeatButton("+", btn, GUILayout.Width(44), GUILayout.Height(30)))) { v += paso; AplicarAjuste(); GuardarAjuste(); }
        GUILayout.EndHorizontal();
    }

    class Mueble { public string idTienda; public string nombre; public Transform t; public Vector3 pos0; public float rot0; public Bounds local; }
    readonly List<Mueble> muebles = new List<Mueble>();
    GameObject casa; CesiumGlobeAnchor anc; bool cargada, cargando;
    public static bool Cargada { get { return inst != null && inst.cargada; } }

    // ---- modo decorar ----
    public static bool decorando;
    Camera camDec; float alturaCam = 16f; Vector3 panL; Mueble sel; bool arrastrando; Vector3 offArr; string aviso = ""; float avisoHasta;   // panL: corrimiento de la camara en coordenadas de la casa
    GUIStyle sBtn, sTxt, sTit;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Crear() { if (FindFirstObjectByType<CesiumGeoreference>() == null) return; new GameObject("Casa").AddComponent<Casa>(); }

    void Start() { inst = this; Cargar(); }

    // ------------------------------------------------------------ carga del modelo
    async void Cargar()
    {
        if (cargada || cargando) return; cargando = true;
        try
        {
            if (!System.IO.File.Exists(RutaGlb)) { Debug.LogWarning("[Casa] no encuentro " + RutaGlb); cargando = false; return; }
            var geo = FindFirstObjectByType<CesiumGeoreference>();
            casa = new GameObject("CasaJugador"); casa.transform.SetParent(geo.transform, false);
            anc = casa.AddComponent<CesiumGlobeAnchor>();
            LeerAjuste(); AplicarAjuste();
            var gl = new GLTFast.GltfImport();
            if (!await gl.Load(new System.Uri(RutaGlb).AbsoluteUri)) { Debug.LogWarning("[Casa] no pude cargar el glb"); cargando = false; return; }
            var modelo = new GameObject("modelo"); modelo.transform.SetParent(casa.transform, false);
            await gl.InstantiateMainSceneAsync(modelo.transform);
            foreach (var t0 in modelo.GetComponentsInChildren<Transform>(true)) if (t0.name.StartsWith("Vereda")) t0.gameObject.SetActive(false);   // 21/11: sin la vereda/rampa de la puerta: se llega directo a la puerta por el terreno
            foreach (var r in modelo.GetComponentsInChildren<Renderer>(true)) r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
            foreach (var t in modelo.GetComponentsInChildren<Transform>(true))
            {
                if (!t.name.StartsWith("MUEBLE_")) continue;
                var m = new Mueble { nombre = t.name.Substring(7).Replace("_", " "), t = t, pos0 = t.localPosition, rot0 = t.localEulerAngles.y };
                m.local = BoundsLocal(t); muebles.Add(m);
            }
            if (System.IO.File.Exists(RutaShop))
            {
                var gl2 = new GLTFast.GltfImport();
                if (await gl2.Load(new System.Uri(RutaShop).AbsoluteUri))
                {
                    var tienda = new GameObject("tienda"); tienda.transform.SetParent(modelo.transform, false);
                    await gl2.InstantiateMainSceneAsync(tienda.transform);
                    foreach (var t in tienda.GetComponentsInChildren<Transform>(true))
                    {
                        if (!t.name.StartsWith("MUEBLE_")) continue;
                        string nt; TIENDA.TryGetValue(t.name, out nt);
                        var m = new Mueble { idTienda = nt != null ? "Mi casa|" + nt : null, nombre = nt ?? t.name.Substring(7), t = t, pos0 = t.localPosition, rot0 = t.localEulerAngles.y };
                        m.local = BoundsLocal(t); muebles.Add(m);
                    }
                }
            }
            LeerPosiciones(); ActualizarCompras();
            cargada = true;
            Debug.Log("[Casa] casa cargada en Campo de Mayo con " + muebles.Count + " muebles");
        }
        catch (System.Exception e) { Debug.LogWarning("[Casa] error: " + e.Message); }
        cargando = false;
    }

    // caja del mueble en coordenadas del propio mueble (para tocarlo con el mouse sin usar colisionadores: asi no molesta al auto ni al avion)
    static Bounds BoundsLocal(Transform raiz)
    {
        bool algo = false; var bb = new Bounds();
        foreach (var r in raiz.GetComponentsInChildren<Renderer>(true))
        {
            var mf = r.GetComponent<MeshFilter>(); if (mf == null || mf.sharedMesh == null) continue;
            var b = mf.sharedMesh.bounds; var M = raiz.worldToLocalMatrix * r.transform.localToWorldMatrix;
            for (int i = 0; i < 8; i++)
            {
                var p = M.MultiplyPoint3x4(b.center + Vector3.Scale(b.extents, new Vector3((i & 1) == 0 ? -1 : 1, (i & 2) == 0 ? -1 : 1, (i & 4) == 0 ? -1 : 1)));
                if (!algo) { bb = new Bounds(p, Vector3.zero); algo = true; } else bb.Encapsulate(p);
            }
        }
        if (!algo) bb = new Bounds(Vector3.zero, Vector3.one * 0.5f);
        return bb;
    }

    // los muebles del shopping solo se ven si los compraste (se revisa cada segundo)
    float proxCompras;
    void Update() { if (cargada && Time.unscaledTime > proxCompras) { proxCompras = Time.unscaledTime + 1f; ActualizarCompras(); } }
    void ActualizarCompras()
    {
        foreach (var m in muebles)
        {
            if (m.idTienda == null) continue;
            bool tiene = Carrera.Tiene(m.idTienda);
            if (m.t.gameObject.activeSelf != tiene) { m.t.gameObject.SetActive(tiene); if (tiene && cargada) Aviso("Llego a tu casa: " + m.nombre + ". Acomodalo con MODO DECORAR (app Mi casa de la tablet)."); }
        }
    }

    // ------------------------------------------------------------ guardar / leer posiciones
    [System.Serializable] class PosJ { public string n; public float x, z, r; }
    [System.Serializable] class ListaJ { public List<PosJ> items = new List<PosJ>(); }
    void Guardar()
    {
        var l = new ListaJ();
        foreach (var m in muebles) l.items.Add(new PosJ { n = m.t.name, x = m.t.localPosition.x, z = m.t.localPosition.z, r = m.t.localEulerAngles.y });
        try { System.IO.File.WriteAllText(RutaMuebles, JsonUtility.ToJson(l, true)); } catch (System.Exception e) { Debug.LogWarning("[Casa] no pude guardar: " + e.Message); }
    }
    void LeerPosiciones()
    {
        try
        {
            if (!System.IO.File.Exists(RutaMuebles)) return;
            var l = JsonUtility.FromJson<ListaJ>(System.IO.File.ReadAllText(RutaMuebles)); if (l == null) return;
            foreach (var p in l.items) foreach (var m in muebles) if (m.t.name == p.n) { var v = m.t.localPosition; v.x = p.x; v.z = p.z; m.t.localPosition = v; m.t.localRotation = Quaternion.Euler(0f, p.r, 0f); }
        }
        catch { }
    }
    void Restaurar() { foreach (var m in muebles) { var v = m.t.localPosition; v.x = m.pos0.x; v.z = m.pos0.z; m.t.localPosition = v; m.t.localRotation = Quaternion.Euler(0f, m.rot0, 0f); } Guardar(); }

    // ------------------------------------------------------------ modo decorar
    // centro de la puerta de entrada (en el piso) y la direccion "hacia afuera" (el frente de la casa es +Z del modelo)
    public static bool Puerta(out Vector3 pos, out Vector3 haciaAfuera)
    {
        pos = Vector3.zero; haciaAfuera = Vector3.forward;
        if (inst == null || inst.casa == null) return false;
        var mt = inst.casa.transform.Find("modelo"); if (mt == null) return false;
        Transform p = null; foreach (var t in mt.GetComponentsInChildren<Transform>(true)) if (t.name.StartsWith("Puerta entrada")) { p = t; break; }
        if (p == null) return false;
        var r = p.GetComponentInChildren<Renderer>(); pos = r != null ? r.bounds.center : p.position;
        haciaAfuera = mt.forward; haciaAfuera.y = 0f; haciaAfuera.Normalize(); return true;
    }
    public static void SalirDecorar() { if (inst != null && decorando) inst.Salir(); }
    // colisionadores de paredes y piso solo mientras se camina (capa APie.CAPA_PAREDES); la puerta de entrada se atraviesa
    public static void Colisiones(bool si)
    {
        if (inst == null || inst.casa == null) return;
        var mt = inst.casa.transform.Find("modelo"); if (mt == null) return;
        foreach (var mf in mt.GetComponentsInChildren<MeshFilter>(true))
        {
            var t = mf.transform; bool mueble = false; for (var p = t; p != null && p != mt; p = p.parent) if (p.name.StartsWith("MUEBLE_")) { mueble = true; break; }
            if (mueble || t.name.StartsWith("Puerta") || t.name.StartsWith("Vereda")) continue;
            var mc = t.GetComponent<MeshCollider>();
            if (si) { if (mc == null) { mc = t.gameObject.AddComponent<MeshCollider>(); mc.sharedMesh = mf.sharedMesh; } mc.enabled = true; t.gameObject.layer = APie.CAPA_PAREDES; }
            else if (mc != null) mc.enabled = false;
        }
    }
    public static void EntrarDecorar() { if (inst != null) inst.Entrar(); }
    void Entrar()
    {
        if (!cargada) { Aviso("La casa todavia se esta cargando"); return; }
        if (camDec == null)
        {
            var go = new GameObject("CamaraDecorar"); camDec = go.AddComponent<Camera>();
            camDec.depth = 50; camDec.nearClipPlane = 0.3f; camDec.farClipPlane = 20000f; camDec.fieldOfView = 50f;
            var cp = Camera.main; if (cp != null) { camDec.clearFlags = cp.clearFlags; camDec.backgroundColor = cp.backgroundColor; }
        }
        camDec.gameObject.SetActive(true); decorando = true; sel = null; Controles.escribiendo = true;
        Aviso("Toca un mueble y arrastralo. R lo gira. Rueda: acercar. Flechas: mover la camara.");
    }
    void Salir() { decorando = false; sel = null; arrastrando = false; if (camDec != null) camDec.gameObject.SetActive(false); Controles.escribiendo = false; Guardar(); }
    void IrALaCasa()
    {
        var av = FindFirstObjectByType<AvionDeCarton>(); if (av == null) return; var an = av.GetComponent<CesiumGlobeAnchor>(); if (an == null) return;
        if (AvionDeCarton.enAuto) { Aviso("Primero volve al avion"); return; }
        // 32 m hacia la calle (oeste) desde el centro de la casa, mirando a la casa
        double r = RUMBO_FRENTE * System.Math.PI / 180.0, d = 32.0;
        double la = LatC + d * System.Math.Cos(r) / 6371000.0 * 180.0 / System.Math.PI, lo = LonC + d * System.Math.Sin(r) / (6371000.0 * System.Math.Cos(LAT * System.Math.PI / 180.0)) * 180.0 / System.Math.PI;
        an.longitudeLatitudeHeight = new double3(lo, la, ALT + aj.alto + 3.0); an.rotationEastUpNorth = Quaternion.Euler(0f, RUMBO_FRENTE + 180f, 0f);
        av.Reanudar(); Aviso("Estas enfrente de tu casa. Bajate del avion para ir caminando con el autito.");
    }
    void Aviso(string t) { aviso = t; avisoHasta = Time.unscaledTime + 5f; }

    void LateUpdate()
    {
        if (!decorando || camDec == null || casa == null) return;
        var ct = casa.transform;
        // camara arriba de la casa, un poquito inclinada desde el frente (la puerta abajo de la pantalla)
        Vector3 up = ct.up, fr = ct.forward;
        camDec.transform.position = ct.position + ct.TransformDirection(panL) + up * alturaCam + fr * (alturaCam * 0.35f);
        camDec.transform.rotation = Quaternion.LookRotation(-up * 1f - fr * 0.35f, -fr);
        var mouse = Mouse.current; if (mouse == null) return;
        float rueda = EnBoton(mouse.position.ReadValue()) ? 0f : mouse.scroll.ReadValue().y; if (Mathf.Abs(rueda) > 0.01f) alturaCam = Mathf.Clamp(alturaCam - Mathf.Sign(rueda) * 1.2f, 2.5f, 40f);
        var kb = Keyboard.current;
        if (kb != null)
        {
            float vx = (kb.rightArrowKey.isPressed || kb.dKey.isPressed ? 1f : 0f) - (kb.leftArrowKey.isPressed || kb.aKey.isPressed ? 1f : 0f);
            float vy = (kb.upArrowKey.isPressed || kb.wKey.isPressed ? 1f : 0f) - (kb.downArrowKey.isPressed || kb.sKey.isPressed ? 1f : 0f);
            if (vx != 0f || vy != 0f) Correr(vx * 6f * Time.unscaledDeltaTime, vy * 6f * Time.unscaledDeltaTime);
        }
        if (kb != null && sel != null && kb.rKey.wasPressedThisFrame) Girar();
        if (kb != null && kb.escapeKey.wasPressedThisFrame) { Salir(); return; }
        Vector2 mp = mouse.position.ReadValue();
        if (EnBoton(mp)) return;   // los botones de abajo no agarran muebles
        Ray ray = camDec.ScreenPointToRay(mp);
        if (mouse.leftButton.wasPressedThisFrame)
        {
            sel = Tocar(ray); arrastrando = sel != null;
            if (arrastrando) { Vector3 h; if (Piso(ray, out h)) offArr = sel.t.localPosition - h; else offArr = Vector3.zero; }
        }
        if (arrastrando && mouse.leftButton.isPressed && sel != null)
        {
            Vector3 h; if (Piso(ray, out h))
            {
                var p = h + offArr; float y = sel.t.localPosition.y;
                p.x = Mathf.Round(Mathf.Clamp(p.x, -3.8f, 3.8f) * 10f) / 10f; p.z = Mathf.Round(Mathf.Clamp(p.z, -4.8f, 4.8f) * 10f) / 10f;   // de a 10 cm, dentro de la casa
                sel.t.localPosition = new Vector3(p.x, y, p.z);
            }
        }
        if (!mouse.leftButton.isPressed) arrastrando = false;
    }
    // corre la camara en la direccion de la pantalla (derecha / arriba), sin salirse mucho de la casa
    void Correr(float dx, float dy)
    {
        if (camDec == null || casa == null) return; var ct = casa.transform;
        Vector3 r = ct.InverseTransformDirection(camDec.transform.right); r.y = 0f; r.Normalize();
        Vector3 u = ct.InverseTransformDirection(camDec.transform.up); u.y = 0f; u.Normalize();
        panL += r * dx + u * dy; panL.x = Mathf.Clamp(panL.x, -6f, 6f); panL.z = Mathf.Clamp(panL.z, -7f, 7f); panL.y = 0f;
    }
    void Girar() { if (sel == null) return; sel.t.localRotation = Quaternion.Euler(0f, Mathf.Round((sel.t.localEulerAngles.y + 90f) / 90f) * 90f, 0f); }

    // punto del piso (coordenadas locales del modelo) bajo el mouse
    bool Piso(Ray ray, out Vector3 local)
    {
        local = Vector3.zero; var mt = casa.transform.Find("modelo"); if (mt == null) return false;
        var plano = new Plane(mt.up, mt.TransformPoint(new Vector3(0f, 0.1f, 0f)));
        float d; if (!plano.Raycast(ray, out d)) return false;
        local = mt.InverseTransformPoint(ray.GetPoint(d)); return true;
    }
    Mueble Tocar(Ray ray)
    {
        Mueble mejor = null; float md = float.MaxValue;
        foreach (var m in muebles)
        {
            if (!m.t.gameObject.activeInHierarchy) continue;
            var lr = new Ray(m.t.InverseTransformPoint(ray.origin), m.t.InverseTransformDirection(ray.direction));
            float d; var b = m.local; b.Expand(0.1f);
            if (b.IntersectRay(lr, out d) && d < md) { md = d; mejor = m; }
        }
        return mejor;
    }

    // ------------------------------------------------------------ botones del modo decorar
    Rect rBarra { get { return new Rect(Screen.width * 0.5f - 330f, Screen.height - 134f, 660f, 64f); } }   // mas arriba: abajo de todo estan los creditos de Cesium/Google (vinculos que abren un cartel marron si se tocan)
    Rect rFlechas { get { return new Rect(rBarra.x - 150f, Screen.height - 210f, 140f, 140f); } }
    bool EnBoton(Vector2 mpInput) { var p = new Vector2(mpInput.x, Screen.height - mpInput.y); return rBarra.Contains(p) || rFlechas.Contains(p) || Tablet.Sobre(p); }
    void OnGUI()
    {
        if (sBtn == null) { sBtn = new GUIStyle(GUI.skin.button) { fontSize = 18, fontStyle = FontStyle.Bold }; sTxt = new GUIStyle(GUI.skin.label) { fontSize = 20, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter }; sTit = new GUIStyle(sTxt) { fontSize = 16 }; }
        if (Time.unscaledTime < avisoHasta && aviso != "")
        {
            var ra = new Rect(Screen.width * 0.5f - 380f, 90f, 760f, 40f); var c = GUI.color; GUI.color = new Color(0, 0, 0, 0.65f); GUI.DrawTexture(ra, Texture2D.whiteTexture); GUI.color = c;
            sTxt.normal.textColor = Color.white; GUI.Label(ra, aviso, sTxt);
        }
        // con el autito al lado de la casa (la tablet no se ve manejando): boton grande para entrar
        if (!decorando && cargada && AvionDeCarton.enAuto && !APie.activo)
        {
            double lo, la; float al, ru;
            if (MapaAuto.PosActiva(out lo, out la, out al, out ru))
            {
                double dN = (la - LatC) * 111320.0, dE = (lo - LonC) * 111320.0 * System.Math.Cos(LAT * System.Math.PI / 180.0);
                if (dN * dN + dE * dE < 45.0 * 45.0)
                {
                    var bgE = GUI.backgroundColor; GUI.backgroundColor = new Color(0.4f, 1f, 0.5f);
                    if (GUI.Button(new Rect(Screen.width * 0.5f - 170f, Screen.height - 120f, 340f, 50f), "ENTRAR A MI CASA", sBtn)) Entrar();
                    GUI.backgroundColor = bgE;
                }
            }
        }
        if (!decorando) return;
        GUI.depth = -60;
        var r = rBarra; var c0 = GUI.color; GUI.color = new Color(0, 0, 0, 0.7f); GUI.DrawTexture(r, Texture2D.whiteTexture); GUI.color = c0;
        sTit.normal.textColor = new Color(1f, 0.85f, 0.3f);
        GUI.Label(new Rect(r.x, r.y + 2f, r.width, 22f), sel != null ? "Elegiste: " + sel.nombre : "MODO DECORAR - toca un mueble", sTit);
        float bw = 150f, y = r.y + 26f, x = r.x + 10f;
        GUI.enabled = sel != null; if (GUI.Button(new Rect(x, y, bw, 32f), "GIRAR 90", sBtn)) Girar(); GUI.enabled = true; x += bw + 10f;
        if (GUI.Button(new Rect(x, y, bw, 32f), "SOLTAR", sBtn)) sel = null; x += bw + 10f;
        if (GUI.Button(new Rect(x, y, bw, 32f), "COMO ESTABA", sBtn)) { Restaurar(); Aviso("Los muebles volvieron a su lugar de fabrica"); } x += bw + 10f;
        var bg = GUI.backgroundColor; GUI.backgroundColor = new Color(0.4f, 1f, 0.5f);
        if (GUI.Button(new Rect(x, y, bw - 20f, 32f), "LISTO", sBtn)) Salir(); GUI.backgroundColor = bg;
        // flechas para correr la camara (mantener apretado)
        var rf = rFlechas; GUI.color = new Color(0, 0, 0, 0.55f); GUI.DrawTexture(rf, Texture2D.whiteTexture); GUI.color = c0;
        float s3 = rf.width / 3f, pasoF = 6f * Time.unscaledDeltaTime;
        if (GUI.RepeatButton(new Rect(rf.x + s3, rf.y + 2f, s3 - 2f, s3 - 2f), "\u25B2", sBtn)) Correr(0f, pasoF);
        if (GUI.RepeatButton(new Rect(rf.x + s3, rf.y + 2f * s3, s3 - 2f, s3 - 2f), "\u25BC", sBtn)) Correr(0f, -pasoF);
        if (GUI.RepeatButton(new Rect(rf.x + 2f, rf.y + s3, s3 - 2f, s3 - 2f), "\u25C0", sBtn)) Correr(-pasoF, 0f);
        if (GUI.RepeatButton(new Rect(rf.x + 2f * s3, rf.y + s3, s3 - 2f, s3 - 2f), "\u25B6", sBtn)) Correr(pasoF, 0f);
        if (GUI.Button(new Rect(rf.x + s3, rf.y + s3, s3 - 2f, s3 - 2f), "o", sBtn)) panL = Vector3.zero;
        // (sin cartel con el nombre arriba del mueble: quedaba feo; el nombre ya esta en la barra de abajo)
        if (false && sel != null && camDec != null)
        {
            var sp = camDec.WorldToScreenPoint(sel.t.TransformPoint(sel.local.center)); if (sp.z > 0f)
            { var p = new Vector2(sp.x, Screen.height - sp.y); var rr = new Rect(p.x - 70f, p.y - 40f, 140f, 26f); GUI.color = new Color(0, 0, 0, 0.6f); GUI.DrawTexture(rr, Texture2D.whiteTexture); GUI.color = c0; sTit.normal.textColor = new Color(0.5f, 1f, 0.6f); GUI.Label(rr, sel.nombre, sTit); }
        }
    }

    // ------------------------------------------------------------ app de la tablet y punto en el mapa
    public static void Pestana(GUIStyle txt, GUIStyle btn, GUIStyle tit)
    {
        GUILayout.Label("MI CASA - Campo de Mayo (prueba)", tit);
        var tw = new GUIStyle(txt) { wordWrap = true };
        GUILayout.Label("Tu casa de prueba esta en el lote chico de Campo de Mayo, enfrente del terreno grande, con la puerta hacia la calle. Aterriza en Campo de Mayo y anda con el autito; en el mapa es la casita verde.", tw);
        GUILayout.Space(6);
        if (inst == null || !inst.cargada) { GUILayout.Label(inst != null && inst.cargando ? "Cargando la casa..." : "La casa no se pudo cargar (falta casas/casa_8x10.glb).", txt); return; }
        if (GUILayout.Button("IR A MI CASA (te deja en la calle de enfrente)", btn, GUILayout.Height(30))) inst.IrALaCasa();
        if (GUILayout.Button(decorando ? "SALIR DE DECORAR" : "MODO DECORAR (mover muebles)", btn, GUILayout.Height(34))) { if (decorando) inst.Salir(); else inst.Entrar(); }
        GUILayout.Label("En modo decorar: clic en un mueble y arrastralo; R o GIRAR 90 lo gira; rueda del mouse acerca o aleja; LISTO guarda.", tw);
        GUILayout.Space(6);
        if (GUILayout.Button(editarCasa ? "CERRAR AJUSTE DE LA CASA" : "AJUSTAR LA CASA (si quedo hundida, torcida o corrida)", btn, GUILayout.Height(30))) editarCasa = !editarCasa;
        if (editarCasa)
        {
            GUILayout.Label("Mantene apretado + o - para que vaya mas rapido. Se guarda solo.", tw);
            inst.Fila("Altura", "al", ref inst.aj.alto, 0.05f, "m", txt, btn);
            inst.Fila("Giro", "gi", ref inst.aj.giro, 1f, "grados", txt, btn);
            inst.Fila("Correr al norte", "no", ref inst.aj.norte, 0.5f, "m", txt, btn);
            inst.Fila("Correr al este", "es", ref inst.aj.este, 0.5f, "m", txt, btn);
            if (GUILayout.Button("Volver la casa a como estaba", btn, GUILayout.Height(26))) { inst.aj = new AjusteJ(); inst.AplicarAjuste(); inst.GuardarAjuste(); }
        }
        int vis = 0; foreach (var m in inst.muebles) if (m.t.gameObject.activeInHierarchy) vis++; GUILayout.Label("Muebles en la casa: " + vis + "   (compra mas en el Shopping, seccion Mi casa)", txt);
    }
    public static void DibujarEnMapa(Rect r, System.Func<double, double, Vector2> px, Texture2D cir)
    {
        if (inst == null) return; Vector2 p = px(inst.LatC, inst.LonC); if (p.x < -20f || p.y < -20f || p.x > r.width + 20f || p.y > r.height + 20f) return;
        var c0 = GUI.color; GUI.color = Color.black; GUI.DrawTexture(new Rect(p.x - 10f, p.y - 10f, 20f, 20f), cir);
        GUI.color = new Color(0.3f, 0.95f, 0.4f); GUI.DrawTexture(new Rect(p.x - 8f, p.y - 8f, 16f, 16f), cir);
        var s = new GUIStyle(GUI.skin.label) { fontSize = 14, fontStyle = FontStyle.Bold }; s.normal.textColor = Color.black; GUI.Label(new Rect(p.x + 11f, p.y - 9f, 100f, 20f), "MI CASA", s);
        s.normal.textColor = new Color(0.5f, 1f, 0.55f); GUI.Label(new Rect(p.x + 10f, p.y - 10f, 100f, 20f), "MI CASA", s); GUI.color = c0;
    }
}
