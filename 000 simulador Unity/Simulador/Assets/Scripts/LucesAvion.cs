// Luces del avion: roja (izquierda), verde (derecha), estroboscopica (arriba/cola) y luces que iluminan las alas para volar de noche.
// DIFERENCIA CLAVE con Godot: las luces son HIJAS del modelo (modeloT), asi que giran y alabean pegadas al ala (no hace falta Blender).
// Posicion por avion: se ajusta en la pestana "Luces" y "Clavar luces" la guarda en luces_aviones.json (carpeta del proyecto).
// Valores de Godot (RESPUESTA_LUCES_AVION.md): estroboscopica 0,7 ciclos/s con pico agudo (sin doble destello), roja/verde fijas, dos focos de ala de 45 grados.
using UnityEngine;
using System.Collections.Generic;
using UnityEngine.InputSystem;

public class LucesAvion : MonoBehaviour
{
    [System.Serializable] public class Reg { public string nombre; public Vector3 roja, verde, estrobo, alas, alaI, alaD; public float escala; public bool sinBolitaBlanca; }
    [System.Serializable] class Lista { public Reg[] items = new Reg[0]; }

    static LucesAvion inst;
    public static bool Congelado { get { return inst != null && inst.enEdicion; } }
    Transform modelo; string nombreAvion = ""; float esperaConstruir;
    bool enEdicion; float escalaEdicion = 1f; GameObject marcaAlaD, marcaAlaI; int arrastrando = -1; float ultimoDibujo = -10f, tamLuz = 0.4f; Material matAmarilla;
    GameObject rojaGO, verdeGO, estroboGO, alasGO; Light luzRoja, luzVerde, luzEstrobo, luzAlaI, luzAlaD; Renderer esfEstrobo;
    Vector3 pRoja, pVerde, pEstrobo, pAlaI, pAlaD; float largo = 8f, semiancho = 5f;
    bool sinBolitaBlanca; bool lucesOn = true, alasAuto = true, alasForzadas = true; Vector2 scroll; Material matRoja, matVerde, matBlanca; string aviso = "";
    string Ruta { get { return System.IO.Path.GetFullPath(System.IO.Path.Combine(Application.dataPath, "..", "..", "luces_aviones.json")); } }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Crear() { new GameObject("LucesAvion").AddComponent<LucesAvion>(); }

    void Awake() { inst = this; }
    // para la torre
    public static bool Nav { get { return inst != null && inst.lucesOn; } set { if (inst != null) inst.lucesOn = value; } }
    public static bool Aterr { get { return inst != null && !inst.alasForzadas; } set { if (inst != null) inst.alasForzadas = !value; } }

    Material MatEmisivo(Color c)
    {
        var sh = Shader.Find("Universal Render Pipeline/Unlit"); if (sh == null) sh = Shader.Find("Unlit/Color");
        var m = new Material(sh); m.SetColor("_BaseColor", c); m.SetColor("_Color", c); return m;
    }

    void Update()
    {
        var m = AvionDeCarton.ModeloT;
        if (m != modelo) { modelo = m; esperaConstruir = 1f; foreach (var g in new[] { rojaGO, verdeGO, estroboGO, alasGO, marcaAlaD, marcaAlaI }) if (g != null) Destroy(g); rojaGO = verdeGO = estroboGO = alasGO = marcaAlaD = marcaAlaI = null; }
        if (modelo == null) return;
        if (esperaConstruir > 0f) { esperaConstruir -= Time.deltaTime; if (esperaConstruir <= 0f) Reconstruir(); return; }   // espera 1 s a que el avion termine de acomodar escala y orientacion
        ManejarMouse();
        float noche = Aeropuertos.factorNoche;
        // Godot (RESPUESTA_LUCES_AVION): roja y verde FIJAS; estroboscopica = pow(max(0, sin(t*TAU*0.7)), 12): un destello corto cada ~1,43 s, sin doble destello
        bool v = lucesOn;
        float destello = Mathf.Pow(Mathf.Max(0f, Mathf.Sin(Time.time * Mathf.PI * 2f * 1.0f)), 8f);   // 1 destello por segundo, bien marcado
        if (enEdicion) destello = 1f;
        if (esfEstrobo != null) { esfEstrobo.enabled = v && ((!sinBolitaBlanca && destello > 0.12f) || enEdicion); float br = Mathf.Clamp01(destello); matBlanca.SetColor("_BaseColor", new Color(br, br, br)); matBlanca.SetColor("_Color", new Color(br, br, br)); }
        if (luzEstrobo != null) { luzEstrobo.enabled = v; luzEstrobo.intensity = Mathf.Lerp(0f, 6f, destello); }
        bool navVisible = v && (noche > 0.03f || enEdicion);   // las bolitas roja y verde solo se ven de noche (o mientras las editas)
        if (rojaGO != null) rojaGO.SetActive(navVisible); if (verdeGO != null) verdeGO.SetActive(navVisible);
        if (luzRoja != null) luzRoja.intensity = 1.5f; if (luzVerde != null) luzVerde.intensity = 1.5f;
        if (alasGO != null) alasGO.SetActive(!alasForzadas);
    }

    // ---------------- construccion ----------------
    void Reconstruir()
    {
        foreach (var g in new[] { rojaGO, verdeGO, estroboGO, alasGO, marcaAlaD, marcaAlaI }) if (g != null) Destroy(g);
        rojaGO = verdeGO = estroboGO = alasGO = marcaAlaD = marcaAlaI = null; luzRoja = luzVerde = luzEstrobo = null; esfEstrobo = null;
        if (modelo == null) return;
        nombreAvion = AvionDeCarton.nombreAvion ?? "";
        if (matRoja == null) { matRoja = MatEmisivo(new Color(1f, 0.05f, 0.05f)); matVerde = MatEmisivo(new Color(0.05f, 1f, 0.15f)); matBlanca = MatEmisivo(Color.white); }
        // caja del modelo en coordenadas locales del modelo
        var rs = modelo.GetComponentsInChildren<Renderer>();
        Vector3 mn = new Vector3(1e9f, 1e9f, 1e9f), mx = -mn; bool algo = false;
        foreach (var r in rs)
        {
            if (r is ParticleSystemRenderer || r.gameObject.name.StartsWith("Luz")) continue;   // no cuenta las esferas de luz que ya puso este script
            // caja EXACTA desde la malla (Renderer.bounds puede venir atrasado respecto de la escala/posicion que se aplica despues)
            Mesh malla = null; var mf = r.GetComponent<MeshFilter>(); if (mf != null) malla = mf.sharedMesh; var sk = r as SkinnedMeshRenderer; if (sk != null) malla = sk.sharedMesh;
            if (malla == null) continue;
            Bounds b = malla.bounds; algo = true;
            Matrix4x4 aLocal = modelo.worldToLocalMatrix * r.transform.localToWorldMatrix;
            for (int i = 0; i < 8; i++)
            {
                Vector3 p = b.center + new Vector3((i & 1) == 0 ? -b.extents.x : b.extents.x, (i & 2) == 0 ? -b.extents.y : b.extents.y, (i & 4) == 0 ? -b.extents.z : b.extents.z);
                Vector3 l = aLocal.MultiplyPoint3x4(p); mn = Vector3.Min(mn, l); mx = Vector3.Max(mx, l);
            }
        }
        if (!algo) return;
        Vector3 c = (mn + mx) * 0.5f, sz = mx - mn;
        // valores por defecto (en coordenadas locales del modelo: +Z = nariz, +X = derecha, +Y = arriba)
        pRoja = new Vector3(mn.x, c.y, c.z);
        pVerde = new Vector3(mx.x, c.y, c.z);
        pEstrobo = new Vector3(c.x, mx.y, mn.z + sz.z * 0.2f);
        pAlaD = new Vector3(sz.x * 0.2f, c.y + sz.y * 0.15f, c.z); pAlaI = new Vector3(-pAlaD.x, pAlaD.y, pAlaD.z); escalaEdicion = 1f;
        largo = sz.z; semiancho = sz.x * 0.5f;
        Reg g2 = Buscar(nombreAvion);
        if (g2 != null) { pRoja = g2.roja; pVerde = g2.verde; pEstrobo = g2.estrobo; if (g2.alaI != Vector3.zero || g2.alaD != Vector3.zero) { pAlaI = g2.alaI; pAlaD = g2.alaD; } else { pAlaD = g2.alas; pAlaI = new Vector3(-g2.alas.x, g2.alas.y, g2.alas.z); } if (g2.escala > 0f) escalaEdicion = g2.escala; sinBolitaBlanca = g2.sinBolitaBlanca; } else sinBolitaBlanca = false;
        Montar();
    }

    float EscalaLocal(float metros) { float s = Mathf.Max(0.0001f, modelo.lossyScale.x); return metros / s; }

    GameObject Esfera(string nom, Vector3 pos, Material mat, float metros)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Sphere); var col = go.GetComponent<Collider>(); if (col != null) Destroy(col);
        go.name = nom; go.layer = DiaNoche.CAPA_MARCADORES; go.transform.SetParent(modelo, false); go.transform.localPosition = pos;   // capa de marcadores: no la oscurece el modo noche (como en Godot)
        go.transform.localScale = Vector3.one * EscalaLocal(metros);
        var r = go.GetComponent<Renderer>(); r.sharedMaterial = mat; r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; r.receiveShadows = false;
        return go;
    }

    Light LuzPunto(GameObject padre, Color col, float rango, float intensidad)
    {
        var lg = new GameObject("luz"); lg.transform.SetParent(padre.transform, false);
        var l = lg.AddComponent<Light>(); l.type = LightType.Point; l.color = col; l.range = rango; l.intensity = intensidad; l.shadows = LightShadows.None;
        return l;
    }

    void Montar()
    {
        float tam = Mathf.Clamp(largo * 0.035f, 0.25f, 1.2f);   // el punto de luz crece un poco con el avion
        rojaGO = Esfera("LuzRoja", pRoja, matRoja, tam); luzRoja = LuzPunto(rojaGO, new Color(1f, 0.1f, 0.1f), Mathf.Max(6f, largo * 0.8f), 1.5f);
        verdeGO = Esfera("LuzVerde", pVerde, matVerde, tam); luzVerde = LuzPunto(verdeGO, new Color(0.1f, 1f, 0.2f), Mathf.Max(6f, largo * 0.8f), 1.5f);
        estroboGO = Esfera("LuzEstrobo", pEstrobo, matBlanca, tam * 1.3f); esfEstrobo = estroboGO.GetComponent<Renderer>(); luzEstrobo = LuzPunto(estroboGO, Color.white, Mathf.Max(12f, largo * 1.6f), 6f);
        // luces de las alas: una sobre cada semiala, apuntan al conjunto para que el avion no se vea negro de noche
        alasGO = new GameObject("LucesAlas"); alasGO.transform.SetParent(modelo, false);
        var a1 = new GameObject("alaI"); a1.transform.SetParent(alasGO.transform, false); a1.transform.localPosition = pAlaI;
        var a2 = new GameObject("alaD"); a2.transform.SetParent(alasGO.transform, false); a2.transform.localPosition = pAlaD;
        // Godot: dos SpotLight (una por ala), apuntan a cada costado e inclinadas 25 grados hacia abajo; blanco calido, energia 5, cono 45, alcance clamp(tamano*0.55, 1, 14), sin sombra
        float rango = Mathf.Clamp(largo * 0.9f, 3f, 30f);
        luzAlaI = a1.AddComponent<Light>(); luzAlaD = a2.AddComponent<Light>();
        a1.transform.localRotation = Quaternion.Euler(25f, -90f, 0f); a2.transform.localRotation = Quaternion.Euler(25f, 90f, 0f);
        foreach (var l in new[] { luzAlaI, luzAlaD }) { l.type = LightType.Spot; l.color = new Color(1f, 0.95f, 0.85f); l.range = rango; l.spotAngle = 110f; l.innerSpotAngle = 60f; l.intensity = 80f; l.shadows = LightShadows.None; }
        if (matAmarilla == null) matAmarilla = MatEmisivo(new Color(1f, 0.9f, 0.1f));
        tamLuz = tam;
        marcaAlaD = Esfera("LuzMarcaAlaD", pAlaD, matAmarilla, tam * 1.5f); marcaAlaI = Esfera("LuzMarcaAlaI", pAlaI, matAmarilla, tam * 1.5f);
        marcaAlaD.SetActive(false); marcaAlaI.SetActive(false);
    }

    // ---------------- arrastrar con el mouse ----------------
    GameObject Asa(int i) { return i == 0 ? rojaGO : i == 1 ? verdeGO : i == 2 ? estroboGO : i == 3 ? marcaAlaD : marcaAlaI; }

    void ManejarMouse()
    {
        if (rojaGO == null || verdeGO == null || estroboGO == null) return;
        bool tab = Time.unscaledTime - ultimoDibujo < 0.4f; enEdicion = tab;
        // en la pestana Luces las esferas se agrandan (para agarrarlas) y aparecen las marcas amarillas de las luces de las alas
        float k = escalaEdicion;   // el tamano elegido vale igual en edicion y en vuelo
        rojaGO.transform.localScale = Vector3.one * EscalaLocal(tamLuz * k); verdeGO.transform.localScale = Vector3.one * EscalaLocal(tamLuz * k);
        estroboGO.transform.localScale = Vector3.one * EscalaLocal(tamLuz * 1.3f * k);
        // roja y verde se ven del MISMO tamano en pantalla: la mas cercana a la camara se achica hasta igualar a la mas lejana (la perspectiva ya no las diferencia)
        var cm = Camera.main;
        if (cm != null)
        {
            float dr = Vector3.Distance(cm.transform.position, rojaGO.transform.position), dv = Vector3.Distance(cm.transform.position, verdeGO.transform.position), dm = Mathf.Max(dr, dv, 0.01f);
            rojaGO.transform.localScale = Vector3.one * EscalaLocal(tamLuz * k * dr / dm); verdeGO.transform.localScale = Vector3.one * EscalaLocal(tamLuz * k * dv / dm);
        }
        if (marcaAlaD != null) { marcaAlaD.SetActive(tab); marcaAlaI.SetActive(tab); marcaAlaD.transform.localScale = Vector3.one * EscalaLocal(tamLuz * 1.5f * k); marcaAlaI.transform.localScale = marcaAlaD.transform.localScale; }
        if (!tab) { arrastrando = -1; return; }
        var raton = Mouse.current; var cam = Camera.main; if (raton == null || cam == null) return;
        Vector2 mp = raton.position.ReadValue();
        if (arrastrando < 0)
        {
            // agarrar: boton apretado sobre una esfera (no sobre el panel de abajo ni la fila de botones de arriba)
            bool zonaLibre = mp.y > 210f && mp.y < Screen.height - 40f;
            if (raton.leftButton.wasPressedThisFrame && zonaLibre)
            {
                float mejor = 40f;
                for (int i = 0; i < 5; i++)
                {
                    var g = Asa(i); if (g == null || !g.activeInHierarchy) continue;
                    Vector3 sp = cam.WorldToScreenPoint(g.transform.position); if (sp.z <= 0f) continue;
                    float d = Vector2.Distance(new Vector2(sp.x, sp.y), mp); if (d < mejor) { mejor = d; arrastrando = i; }
                }
            }
            return;
        }
        // soltar (chequeo activo del boton, nunca queda trabada)
        if (!raton.leftButton.isPressed) { arrastrando = -1; return; }
        var asa = Asa(arrastrando); if (asa == null) { arrastrando = -1; return; }
        // el punto se mueve en el plano paralelo a la camara que pasa por la esfera
        Ray rayo = cam.ScreenPointToRay(mp);
        var plano = new Plane(-modelo.forward, asa.transform.position);   // plano del PROPIO modelo (no el de la camara): al subir o bajar el avion las luces no se corren de profundidad
        float t; if (!plano.Raycast(rayo, out t)) return;
        Vector3 loc = modelo.InverseTransformPoint(rayo.GetPoint(t));
        if (arrastrando == 0) pRoja = loc; else if (arrastrando == 1) pVerde = loc; else if (arrastrando == 2) pEstrobo = loc;
        else if (arrastrando == 3) pAlaD = loc; else pAlaI = loc;   // cada luz de ala es independiente
        Reaplicar();
    }

    void Reaplicar()
    {
        if (rojaGO == null) return;
        rojaGO.transform.localPosition = pRoja; verdeGO.transform.localPosition = pVerde; estroboGO.transform.localPosition = pEstrobo;
        if (marcaAlaD != null) { marcaAlaD.transform.localPosition = pAlaD; marcaAlaI.transform.localPosition = pAlaI; }
        if (luzAlaI != null) { luzAlaI.transform.localPosition = pAlaI; luzAlaD.transform.localPosition = pAlaD; }
    }

    // ---------------- guardado ----------------
    Reg Buscar(string nombre)
    {
        try
        {
            if (!System.IO.File.Exists(Ruta)) return null;
            var l = JsonUtility.FromJson<Lista>(System.IO.File.ReadAllText(Ruta));
            if (l != null && l.items != null) foreach (var r in l.items) if (r.nombre == nombre) return r;
        }
        catch { }
        return null;
    }

    void Clavar()
    {
        try
        {
            var l = new Lista(); if (System.IO.File.Exists(Ruta)) { var x = JsonUtility.FromJson<Lista>(System.IO.File.ReadAllText(Ruta)); if (x != null && x.items != null) l = x; }
            var lst = new List<Reg>(l.items); lst.RemoveAll(r => r.nombre == nombreAvion);
            lst.Add(new Reg { nombre = nombreAvion, roja = pRoja, verde = pVerde, estrobo = pEstrobo, alaI = pAlaI, alaD = pAlaD, escala = escalaEdicion, sinBolitaBlanca = sinBolitaBlanca });
            l.items = lst.ToArray(); System.IO.File.WriteAllText(Ruta, JsonUtility.ToJson(l, true));
            aviso = "Luces clavadas para " + nombreAvion;
        }
        catch (System.Exception e) { aviso = "No pude guardar: " + e.Message; }
    }

    void Restaurar()
    {
        try
        {
            if (System.IO.File.Exists(Ruta))
            {
                var l = JsonUtility.FromJson<Lista>(System.IO.File.ReadAllText(Ruta));
                if (l != null && l.items != null) { var lst = new List<Reg>(l.items); lst.RemoveAll(r => r.nombre == nombreAvion); l.items = lst.ToArray(); System.IO.File.WriteAllText(Ruta, JsonUtility.ToJson(l, true)); }
            }
        }
        catch { }
        Reconstruir(); aviso = "Luces de fabrica restauradas";
    }

    // ---------------- pestana "Luces" ----------------
    public static void PestanaLuces(GUIStyle txt, GUIStyle btn, GUIStyle titulo) { if (inst != null) inst.Dibujar(txt, btn, titulo); }

    Vector3 Fila(string nombre, Vector3 p, float paso)
    {
        GUILayout.BeginHorizontal();
        GUILayout.Label(nombre, txt0, GUILayout.Width(110));
        p.x = Campo("X", p.x, paso); p.y = Campo("Y", p.y, paso); p.z = Campo("Z", p.z, paso);
        GUILayout.EndHorizontal();
        return p;
    }
    GUIStyle txt0, btn0;
    float Campo(string n, float v, float paso)
    {
        GUILayout.Label(n + " " + v.ToString("F2"), txt0, GUILayout.Width(70));
        if (GUILayout.Button("-", btn0, GUILayout.Width(24), GUILayout.Height(20))) v -= paso;
        if (GUILayout.Button("+", btn0, GUILayout.Width(24), GUILayout.Height(20))) v += paso;
        GUILayout.Space(10);
        return v;
    }

    void Dibujar(GUIStyle txt, GUIStyle btn, GUIStyle titulo)
    {
        txt0 = txt; btn0 = btn; ultimoDibujo = Time.unscaledTime;
        GUILayout.Label("LUCES DEL AVION   Agarra con el mouse (apreta, arrastra y solta) la bolita ROJA, VERDE, BLANCA (estroboscopica) o las AMARILLAS (luces de las alas) y dejala donde quieras. Despues CLAVAR LUCES.", titulo);
        if (modelo == null) { GUILayout.Label("Primero elegi un avion.", txt); return; }
        scroll = GUILayout.BeginScrollView(scroll, false, true);
        GUILayout.BeginHorizontal();
        GUILayout.Label("Avion: " + nombreAvion, txt, GUILayout.Width(300));
        if (GUILayout.Button(lucesOn ? "Luces: ENCENDIDAS" : "Luces: apagadas", btn, GUILayout.Width(150), GUILayout.Height(20))) lucesOn = !lucesOn;
        string ta = alasForzadas ? "Luz de alas: apagada" : "Luz de alas: ENCENDIDA";
        if (GUILayout.Button(ta, btn, GUILayout.Width(190), GUILayout.Height(20))) alasForzadas = !alasForzadas;
        if (GUILayout.Button("Bolitas -", btn, GUILayout.Width(70), GUILayout.Height(20))) escalaEdicion = Mathf.Max(0.25f, escalaEdicion * 0.75f);
        if (GUILayout.Button("Bolitas +", btn, GUILayout.Width(70), GUILayout.Height(20))) escalaEdicion = Mathf.Min(4f, escalaEdicion * 1.33f);
        GUILayout.EndHorizontal();
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Copiar ala IZQ -> DER (espejo)", btn, GUILayout.Width(230), GUILayout.Height(20))) pAlaD = new Vector3(-pAlaI.x, pAlaI.y, pAlaI.z);
        if (GUILayout.Button("Copiar ala DER -> IZQ (espejo)", btn, GUILayout.Width(230), GUILayout.Height(20))) pAlaI = new Vector3(-pAlaD.x, pAlaD.y, pAlaD.z);
        if (GUILayout.Button(sinBolitaBlanca ? "Bolita blanca: OCULTA en vuelo (destello queda)" : "Bolita blanca: visible", btn, GUILayout.Width(330), GUILayout.Height(20))) { sinBolitaBlanca = !sinBolitaBlanca; Clavar(); }   // se guarda al instante
        GUILayout.EndHorizontal();
        float paso = Mathf.Max(0.02f, largo * 0.01f) / Mathf.Max(0.0001f, modelo.lossyScale.x);
        pRoja = Fila("Roja (izq)", pRoja, paso); pVerde = Fila("Verde (der)", pVerde, paso); pEstrobo = Fila("Estroboscopica", pEstrobo, paso); pAlaI = Fila("Ala izquierda", pAlaI, paso); pAlaD = Fila("Ala derecha", pAlaD, paso);
        Reaplicar();
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("CLAVAR LUCES (guardar para este avion)", btn, GUILayout.Width(280), GUILayout.Height(22))) Clavar();
        if (GUILayout.Button("Volver a las de fabrica", btn, GUILayout.Width(170), GUILayout.Height(22))) Restaurar();
        GUILayout.Label(aviso, txt);
        GUILayout.EndHorizontal();
        GUILayout.EndScrollView();
    }
}
