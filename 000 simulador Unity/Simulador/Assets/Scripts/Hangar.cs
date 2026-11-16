// Pestana "Aviones" (reemplaza al panel central "Elegi tu avion"): lista + foto de costado + giro 3D con el mouse + datos del avion.
// - La foto de costado se saca sola la primera vez que miras un avion y queda guardada en la carpeta "previews" (despues se ve al instante).
// - "Rotar en 3D" carga el modelo en un escenario aparte (lejos del mundo, camara propia, capa 31) y se arrastra con el mouse.
// - Velocidad y demas datos: se leen de ajustes_aviones.json (lo que ajustaste en Pagina 2); si nunca lo ajustaste, el valor de fabrica.
using UnityEngine;
using System.Collections.Generic;

public class Hangar : MonoBehaviour
{
    public static bool abrir;                 // lo lee el panel de abajo para saltar a la pestana Aviones
    const int CAPA = 31;
    static readonly Vector3 CENTRO = new Vector3(0f, -20000f, 0f);   // bajo la tierra: la camara del juego nunca lo ve

    static Hangar inst;
    int sel = -1; Vector2 scroll, scrollInfo;
    Texture2D foto; string fotoDe = "";
    // escenario 3D
    GameObject rig; GLTFast.GltfImport gltfRig; Camera cam; RenderTexture rt;
    int tokenCarga; bool cargandoRig, vivo, giroAuto; float yaw, pitch; string estadoMsg = "";
    // datos del avion elegido (ajustes_aviones.json)
    [System.Serializable] class AjV { public string nombre; public float velMin = 0f, velMax = 417f, vertical = 40f, escala, rx, ry, rz; }
    [System.Serializable] class ListaAjV { public List<AjV> items = new List<AjV>(); }
    AjV datos; bool datosAjustados;

    GUIStyle sTxt, sTit, sBtn, sVerde, sPeq;
    Texture2D fondo;
    bool hayAvion; float proxChequeo;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Crear() { new GameObject("Hangar").AddComponent<Hangar>(); }

    void Awake() { inst = this; }

    static string Carpeta { get { return System.IO.Path.GetFullPath(System.IO.Path.Combine(Application.dataPath, "..", "..")); } }
    static string CarpetaFotos { get { return System.IO.Path.Combine(Carpeta, "previews"); } }
    static string RutaFoto(SelectorAvion.Tipo t)
    {
        string n = t.nombre; foreach (char c in System.IO.Path.GetInvalidFileNameChars()) n = n.Replace(c.ToString(), "");
        return System.IO.Path.Combine(CarpetaFotos, n.Trim() + ".png");
    }
    static string RutaAjustes { get { return System.IO.Path.GetFullPath(System.IO.Path.Combine(Application.dataPath, "..", "ajustes_aviones.json")); } }

    void Estilos()
    {
        if (sTxt != null) return;
        sTxt = new GUIStyle(GUI.skin.label) { fontSize = 13, wordWrap = true, richText = true }; sTxt.normal.textColor = Color.white;
        sTit = new GUIStyle(sTxt) { fontStyle = FontStyle.Bold }; sTit.normal.textColor = new Color(1f, 0.85f, 0.2f);
        sVerde = new GUIStyle(sTxt) { fontStyle = FontStyle.Bold, fontSize = 15 }; sVerde.normal.textColor = new Color(0.4f, 1f, 0.4f);
        sBtn = new GUIStyle(GUI.skin.button) { fontSize = 13, alignment = TextAnchor.MiddleLeft };
        sPeq = new GUIStyle(sTxt) { fontSize = 11 }; sPeq.normal.textColor = new Color(0.75f, 0.75f, 0.75f);
        fondo = new Texture2D(1, 1); fondo.SetPixel(0, 0, new Color(0f, 0f, 0f, 0.82f)); fondo.Apply();
    }

    // ------------------------------------------------------------ seleccion
    void Seleccionar(int i)
    {
        if (i == sel) return;
        sel = i; var t = SelectorAvion.Lista[i];
        DescartarRig(); vivo = false; giroAuto = false; estadoMsg = "";
        CargarFoto(t); CargarDatos(t);
        if (foto == null) { CargarRig(t, true); }   // sin foto todavia: se carga el 3D y se saca sola
    }

    void CargarFoto(SelectorAvion.Tipo t)
    {
        foto = null; fotoDe = t.nombre;
        try
        {
            string p = RutaFoto(t);
            if (System.IO.File.Exists(p)) { var tx = new Texture2D(2, 2); tx.LoadImage(System.IO.File.ReadAllBytes(p)); foto = tx; }
        }
        catch { }
    }

    void CargarDatos(SelectorAvion.Tipo t)
    {
        datos = new AjV(); datosAjustados = false;
        try
        {
            if (!System.IO.File.Exists(RutaAjustes)) return;
            var l = JsonUtility.FromJson<ListaAjV>(System.IO.File.ReadAllText(RutaAjustes));
            string clave = t.recurso ? "Avion" : t.nombre;   // el Emirates guarda su ajuste con el nombre "Avion"
            foreach (var a in l.items) if (a.nombre == clave) { datos = a; datosAjustados = true; break; }
        }
        catch { }
    }

    // ------------------------------------------------------------ escenario 3D
    static void Capa(GameObject g) { foreach (var tr in g.GetComponentsInChildren<Transform>(true)) tr.gameObject.layer = CAPA; }

    void DescartarRig()
    {
        tokenCarga++; cargandoRig = false;
        if (cam != null) cam.enabled = false;
        if (rig != null) Destroy(rig);
        rig = null;
        if (gltfRig != null) { try { gltfRig.Dispose(); } catch { } gltfRig = null; }
    }

    async void CargarRig(SelectorAvion.Tipo t, bool sacarFoto)
    {
        DescartarRig();
        int mi = tokenCarga; cargandoRig = true; estadoMsg = "Cargando " + t.nombre + "...";
        var raiz = new GameObject("HangarRig"); raiz.transform.position = CENTRO;
        var holder = new GameObject("holder"); holder.transform.SetParent(raiz.transform, false);
        GLTFast.GltfImport gl = null;
        try
        {
            if (t.recurso)
            {
                var prefab = Resources.Load<GameObject>("Avion");
                if (prefab != null) Instantiate(prefab, holder.transform);
            }
            else
            {
                gl = new GLTFast.GltfImport();
                bool ok = await gl.Load(new System.Uri(t.archivo).AbsoluteUri);
                if (mi != tokenCarga) { Destroy(raiz); try { gl.Dispose(); } catch { } return; }
                if (ok) await gl.InstantiateMainSceneAsync(holder.transform);
                else { estadoMsg = "No pude cargar " + t.nombre; cargandoRig = false; Destroy(raiz); return; }
            }
        }
        catch (System.Exception e) { estadoMsg = "Error: " + e.Message; cargandoRig = false; Destroy(raiz); return; }
        if (mi != tokenCarga) { Destroy(raiz); if (gl != null) { try { gl.Dispose(); } catch { } } return; }

        rig = raiz; gltfRig = gl; Capa(rig);
        // misma orientacion que en vuelo: la que ajustaste en el panel Modelo (o la de fabrica), asi la nariz (+Z) queda a la derecha
        holder.transform.localRotation = datosAjustados ? Quaternion.Euler(datos.rx, datos.ry, datos.rz) : Quaternion.Euler(0f, t.ry, 0f);
        var rs = rig.GetComponentsInChildren<Renderer>();
        if (rs.Length == 0) { estadoMsg = "El modelo no tiene dibujo"; cargandoRig = false; return; }
        Bounds b = rs[0].bounds; foreach (var r in rs) b.Encapsulate(r.bounds);
        holder.transform.position -= (b.center - CENTRO);   // el centro del avion queda en el eje de giro
        // camara de costado: mira hacia -X, con la nariz (+Z) hacia la derecha de la imagen
        if (rt == null) { rt = new RenderTexture(768, 432, 24); rt.Create(); }
        if (cam == null)
        {
            var go = new GameObject("HangarCam"); go.transform.SetParent(transform, false);
            cam = go.AddComponent<Camera>(); cam.clearFlags = CameraClearFlags.SolidColor; cam.backgroundColor = new Color(0.17f, 0.21f, 0.28f);
            cam.cullingMask = 1 << CAPA; cam.fieldOfView = 30f; cam.targetTexture = rt; cam.depth = -50;
            var al = go.GetComponent<AudioListener>(); if (al != null) Destroy(al);
        }
        Vector3 e2 = b.extents; float tan = Mathf.Tan(15f * Mathf.Deg2Rad);
        float dist = Mathf.Max(e2.z * 1.15f / (tan * 16f / 9f), e2.y * 1.3f / tan) + e2.x;
        cam.transform.position = CENTRO + Vector3.right * dist; cam.transform.LookAt(CENTRO, Vector3.up);
        cam.nearClipPlane = Mathf.Max(0.01f, dist * 0.02f); cam.farClipPlane = dist * 6f;
        yaw = 0f; pitch = 0f; rig.transform.rotation = Quaternion.identity;
        cam.enabled = true; vivo = true; cargandoRig = false; estadoMsg = "";

        if (sacarFoto) { await SacarFoto(t, mi); }
    }

    async Awaitable SacarFoto(SelectorAvion.Tipo t, int mi)
    {
        // se espera a que la camara dibuje con el avion de costado y despues se lee la imagen
        yaw = 0f; pitch = 0f; if (rig != null) rig.transform.rotation = Quaternion.identity;
        await Awaitable.NextFrameAsync(); await Awaitable.NextFrameAsync();
        if (mi != tokenCarga || rt == null) return;
        try
        {
            var prev = RenderTexture.active; RenderTexture.active = rt;
            var tx = new Texture2D(rt.width, rt.height, TextureFormat.RGB24, false);
            tx.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0); tx.Apply();
            RenderTexture.active = prev;
            System.IO.Directory.CreateDirectory(CarpetaFotos);
            System.IO.File.WriteAllBytes(RutaFoto(t), tx.EncodeToPNG());
            foto = tx; fotoDe = t.nombre; estadoMsg = "Foto guardada";
        }
        catch (System.Exception e) { estadoMsg = "No pude sacar la foto: " + e.Message; }
    }

    void Update()
    {
        proxChequeo -= Time.unscaledDeltaTime;
        if (proxChequeo <= 0f) { proxChequeo = 0.5f; hayAvion = FindFirstObjectByType<AvionDeCarton>() != null; }
        if (vivo && rig != null)
        {
            if (giroAuto) yaw += 40f * Time.unscaledDeltaTime;
            rig.transform.rotation = Quaternion.Euler(pitch, yaw, 0f);
        }
    }

    // ------------------------------------------------------------ interfaz
    // sin avion todavia (al arrancar): se dibuja el mismo panel y la flechita de "elegi tu avion"
    void OnGUI()
    {
        if (hayAvion || SelectorAvion.Lista == null || !SelectorAvion.mostrar) return;
        Estilos();
        float W = Screen.width, H = Screen.height; float extra = 190f;
        var b = new GUIStyle(GUI.skin.button) { fontSize = 12 };
        var antes = GUI.backgroundColor; GUI.backgroundColor = Color.green;
        GUI.Button(new Rect(270, H - 194 - extra, 80, 20), "Aviones", b);
        GUI.backgroundColor = antes;
        var rp = new Rect(270, H - 172 - extra, Mathf.Max(300f, W - 440f), 118 + extra);
        GUI.DrawTexture(rp, fondo);
        GUILayout.BeginArea(new Rect(rp.x + 6, rp.y + 4, rp.width - 12, rp.height - 8));
        Panel(rp.width - 12, rp.height - 8);
        GUILayout.EndArea();
        // cartel con flecha hacia el panel
        var c = new GUIStyle(GUI.skin.label) { fontSize = 26, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
        c.normal.textColor = new Color(1f, 0.9f, 0.3f);
        var sombra = new GUIStyle(c); sombra.normal.textColor = Color.black;
        var rc = new Rect(rp.x, rp.y - 74f, rp.width, 60f);
        GUI.Label(new Rect(rc.x + 2, rc.y + 2, rc.width, rc.height), "ELEGI TU AVION\n▼", sombra);
        GUI.Label(rc, "ELEGI TU AVION\n▼", c);
    }

    // contenido de la pestana (dentro de un GUILayout.BeginArea de w x h); lo llama AvionDeCarton (pestana 7) o OnGUI si todavia no hay avion
    public static void PestanaAviones(float w, float h) { if (inst != null) { inst.Estilos(); inst.Panel(w, h); } }

    void Panel(float w, float h)
    {
        var lista = SelectorAvion.Lista;
        if (lista == null) { GUILayout.Label("Cargando lista...", sTxt); return; }
        float anchoLista = 290f, anchoFoto = Mathf.Min(460f, Mathf.Max(260f, w - anchoLista - 300f)), altoFoto = anchoFoto * 9f / 16f;
        var vista = new Rect(0, 0, w, h);
        float contenidoW = Mathf.Max(w, anchoLista + anchoFoto + 280f);
        scrollInfo = GUI.BeginScrollView(vista, scrollInfo, new Rect(0, 0, contenidoW, Mathf.Max(h - 2f, altoFoto + 70f)));

        // ---- lista de aviones
        GUI.Label(new Rect(0, 0, anchoLista, 20), "ELEGI TU AVION (toca uno para verlo)", sTit);
        var vl = new Rect(0, 22, anchoLista, h - 26);
        scroll = GUI.BeginScrollView(vl, scroll, new Rect(0, 0, anchoLista - 20, lista.Length * 28f));
        for (int i = 0; i < lista.Length; i++)
        {
            var t = lista[i]; bool existe = t.recurso || System.IO.File.Exists(t.archivo);
            var antes = GUI.backgroundColor; if (i == sel) GUI.backgroundColor = Color.green;
            GUI.enabled = existe;
            if (GUI.Button(new Rect(0, i * 28f, anchoLista - 22, 26), t.nombre + (existe ? "" : "  [no esta]"), sBtn)) Seleccionar(i);
            GUI.enabled = true; GUI.backgroundColor = antes;
        }
        GUI.EndScrollView();

        // ---- foto / 3D
        float fx = anchoLista + 14f;
        var rf = new Rect(fx, 22, anchoFoto, altoFoto);
        GUI.DrawTexture(rf, fondo);
        if (sel < 0) GUI.Label(rf, "\n   Elegi un avion de la lista para verlo de costado", sTxt);
        else if (vivo && rt != null && cam != null && cam.enabled)
        {
            GUI.DrawTexture(rf, rt, ScaleMode.StretchToFill, false);
            var e = Event.current;   // arrastrar con el mouse para dar vuelta el avion
            if (e.type == EventType.MouseDrag && rf.Contains(e.mousePosition)) { yaw -= e.delta.x * 0.6f; pitch = Mathf.Clamp(pitch + e.delta.y * 0.4f, -80f, 80f); giroAuto = false; e.Use(); }
        }
        else if (foto != null && fotoDe == lista[sel].nombre) GUI.DrawTexture(rf, foto, ScaleMode.ScaleToFit, false);
        else GUI.Label(rf, "\n   " + (estadoMsg != "" ? estadoMsg : "Sin foto todavia"), sTxt);
        if (cargandoRig) GUI.Label(new Rect(rf.x + 8, rf.yMax - 24, rf.width, 22), estadoMsg, sPeq);
        else if (estadoMsg != "") GUI.Label(new Rect(rf.x + 8, rf.yMax - 22, rf.width, 20), estadoMsg, sPeq);

        // ---- botones y datos
        float ix = fx + anchoFoto + 14f, iw = contenidoW - ix - 8f;
        if (sel >= 0)
        {
            var t = lista[sel];
            GUI.Label(new Rect(ix, 22, iw, 22), t.nombre, sVerde);
            float y = 48;
            string tipo = t.helicoptero ? "Helicoptero" : (t.nombre.ToLowerInvariant().Contains("cessna") ? "Avioneta" : "Avion");
            GUI.Label(new Rect(ix, y, iw, 20), "Tipo: " + tipo + "      Archivo: " + (t.recurso ? "incluido" : t.mb.ToString("F1") + " MB" + (t.mb > 20f ? " (pesado)" : "")), sTxt); y += 20;
            GUI.Label(new Rect(ix, y, iw, 20), "Velocidad maxima: <b>" + Mathf.RoundToInt(datos.velMax * 3.6f) + " km/h</b>     minima: " + Mathf.RoundToInt(datos.velMin * 3.6f) + " km/h", sTxt); y += 20;
            GUI.Label(new Rect(ix, y, iw, 20), (t.helicoptero ? "Subida: " : "Cabeceo: ") + (int)datos.vertical + (t.helicoptero ? " m/s" : "°"), sTxt); y += 20;
            GUI.Label(new Rect(ix, y, iw, 32), datosAjustados ? "Datos de lo que ajustaste en Pagina 2 (se actualizan solos)." : "Todavia no lo ajustaste en Pagina 2: valores de fabrica.", sPeq); y += 36;

            if (vivo)
            {
                if (GUI.Button(new Rect(ix, y, 150, 24), giroAuto ? "Parar giro" : "Giro automatico")) giroAuto = !giroAuto;
                if (GUI.Button(new Rect(ix + 156, y, 130, 24), "Foto de costado")) { yaw = 0f; pitch = 0f; giroAuto = false; _ = SacarFoto(t, tokenCarga); }
                y += 28;
                if (foto != null && GUI.Button(new Rect(ix, y, 150, 24), "Ver la foto")) { DescartarRig(); vivo = false; }
                GUI.Label(new Rect(ix + 156, y, iw - 156, 24), "Arrastra con el mouse", sPeq); y += 28;
            }
            else if (!cargandoRig)
            {
                if (GUI.Button(new Rect(ix, y, 150, 24), "Rotar en 3D")) CargarRig(t, foto == null);
                y += 28;
            }
            y += 6;
            GUI.enabled = SelectorAvion.cargando == "" && !cargandoRig;
            var anteFondo = GUI.backgroundColor; GUI.backgroundColor = new Color(0.4f, 1f, 0.4f);
            if (GUI.Button(new Rect(ix, y, 220, 30), "ELEGIR ESTE AVION")) { DescartarRig(); vivo = false; SelectorAvion.Elegir(t); }
            GUI.backgroundColor = anteFondo; GUI.enabled = true;
        }
        GUI.EndScrollView();
    }
}
