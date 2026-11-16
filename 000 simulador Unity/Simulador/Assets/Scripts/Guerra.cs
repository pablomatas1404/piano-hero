// MODO GUERRA (portado de principal.gd, ver RESPUESTA_GUERRA_1..4). Version "verde": todo funciona pero con cubos de colores; se pule despues.
// Lo que trae: boton ON/OFF (pestana Guerra), mira con el stick derecho, disparo, blancos con vida, 5 aeropuertos que contraatacan,
// 20 barcos que avanzan a la costa y desembarcan, invasion terrestre (refuerzos, captura de aeropuertos) y misiones por pasos.
// DISPARO (corregido desde el principio, a pedido de Pablo): la bala SIEMPRE sale de la nariz del avion y va hacia el punto que
// mira la cruz (el rayo de la camara por la cruz fija el destino; el origen es la punta del modelo en su direccion de avance).
// FALTA (anotado): objetivos "Capital Federal" (verdes), modelos 3D de barcos/edificios, colores de vida/semaforo exactos, ver pendientes.
using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;
using CesiumForUnity;
using Unity.Mathematics;

public class Guerra : MonoBehaviour
{
    public static bool modoGuerra, bonusMira;   // bonusMira: la torre prende la mira y el disparo sin modo guerra
    public static bool MiraActiva { get { return modoGuerra || bonusMira; } }
    static Guerra inst;

    // ---- constantes de Godot (principal.gd) ----
    const float CADENCIA_DISPARO = 0.35f;
    const int VIDA_BARCO = 10;            // VIDA_INICIAL_BARCO (tambien aeropuertos)
    const int VIDA_INVASOR = 4;           // VIDA_UNIDAD_INVASION
    const int VIDA_CAPTURA = 40;          // VIDA_CAPTURA_AEROPUERTO
    const int VIDA_INDUSTRIA = 5;         // VIDA_INICIAL_INDUSTRIA
    const int VIDA_JUGADOR = 10;
    const float CARGA_INICIAL = 0.6f, INTERVALO_BALAS = 0.35f, ENFRIAMIENTO = 3.5f; const int RAFAGA = 3;
    const float RADIO_IMPACTO_BALA = 8f, ALCANCE_BALA = 6000f;
    const float OFFSET_MAR = 16f;   // en Cesium el rio esta ~16 m mas arriba que la altura de Aeroparque de los datos (los barcos quedaban hundidos)
    const float ALTURA_BARCO = 50f, VELOCIDAD_BARCOS = 2.5f, LON_COSTA = -58.40f;
    const int MAX_INVASORES = 60; const float ALTURA_FLOTE = 18f, RADIO_CAPTURA = 250f, INTERVALO_REFUERZO = 30f; const int POR_REFUERZO = 5;
    const float LADO_INVASOR = 135f, LADO_AEROPUERTO = 200f, LADO_TOMADO = 220f, LADO_INDUSTRIA = 60f;
    const float RADIO_VOLVER_BASE = 600f;

    static readonly string[] ZONAS_N = { "Quilmes", "Olivos", "cerca de Aeroparque", "cerca de San Fernando" };
    static readonly double[] ZONAS_LAT = { -34.706667, -34.5095, -34.5800, -34.4700 };
    static readonly double[] ZONAS_LON = { -58.244444, -58.4875, -58.4000, -58.5600 };
    static readonly string[] PERMITIDOS = { "Morón", "Ezeiza", "El Palomar" };
    static readonly double[] BARCOS_LAT = { -34.590, -34.540, -34.525, -34.560, -34.545, -34.575, -34.565, -34.590, -34.570, -34.525, -34.585, -34.545, -34.530, -34.565, -34.540, -34.570, -34.560, -34.585, -34.575, -34.565 };
    static readonly double[] BARCOS_LON = { -58.320, -58.304, -58.272, -58.256, -58.224, -58.209, -58.177, -58.161, -58.129, -58.113, -58.082, -58.066, -58.034, -58.018, -57.987, -57.971, -57.939, -57.923, -57.891, -57.859 };
    // para la prueba de blancos con imagen se usan solo 4 de los 10 puntos (norte a sur); para volver a 10: { 0,1,2,3,4,5,6,7,8,9 }
    static readonly int[] VERDE_IDX = { 0, 2, 5, 9 };
    static readonly string[] VERDE_N = { "Nunez", "Belgrano", "Planetario", "Chacarita", "ParqueCentenario", "Congreso", "Caminito", "Flores", "Mataderos", "VillaSoldati" };
    static readonly double[] VERDE_LAT = { -34.545, -34.562, -34.5677, -34.588, -34.6061, -34.6095, -34.6345, -34.6283, -34.6598, -34.6730 };
    static readonly double[] VERDE_LON = { -58.464, -58.456, -58.4136, -58.4555, -58.4342, -58.3927, -58.3631, -58.4650, -58.5094, -58.4650 };
    int verdeDestruidos;
    // Godot: _color_por_vida_restante (negro -> marron -> azul -> blanco; ultimos 2 impactos siempre blanco) y semaforo de invasion
    static Color ColorVida(int resta, int ini)
    {
        if (ini <= 0) ini = 1;
        Color negro = new Color(0.05f, 0.05f, 0.05f), marron = new Color(0.45f, 0.28f, 0.12f), azul = new Color(0.15f, 0.45f, 0.95f), blanco = new Color(1f, 0.95f, 0.55f);
        if (resta <= 2) return blanco;
        float fr = (float)resta / ini;
        if (fr > 0.66f) return Color.Lerp(negro, marron, (1f - fr) / 0.34f);
        if (fr > 0.33f) return Color.Lerp(marron, azul, (0.66f - fr) / 0.33f);
        return Color.Lerp(azul, blanco, (0.33f - fr) / 0.33f);
    }
    static Color Semaforo(int resta) { return resta == 3 ? new Color(0.9f, 0.1f, 0.05f) : resta == 2 ? new Color(1f, 0.85f, 0.1f) : new Color(0.15f, 0.9f, 0.2f); }
    static readonly double[] IND_LAT = { -34.9844, -34.9644, -34.9844, -34.9644, -34.9944, -34.9744, -34.9544 };
    static readonly double[] IND_LON = { -57.9056, -57.8856, -57.8756, -57.9156, -57.8956, -57.8556, -57.9056 };

    class Blanco
    {
        public GameObject go; public CesiumGlobeAnchor anc; public Renderer rend; public string grupo, nombre;
        public int vida, vidaIni; public double lat, lon, h; public float lado; public Color color;
        public bool textura; public bool contraataca, mostrarVida, activadoAtaque, muerto, conModelo; public Renderer[] visuales; public string destino = "";
        public float rafTemp; public int rafBalas; public List<Vector3> origenes = new List<Vector3>();
    }
    class Bala { public GameObject go; public Vector3 dir; public float recorrido; }
    class FxObj { public GameObject go; public float t, dur; public Material mat; public Vector3 fin; public bool parpadeo; }

    readonly List<Blanco> blancos = new List<Blanco>();
    readonly List<Bala> balas = new List<Bala>();
    readonly List<FxObj> efectos = new List<FxObj>();
    readonly HashSet<string> tomados = new HashSet<string>();
    bool iniciado; Transform padre; Material matBase;
    int objDestruidos, barcosDestruidos, industriaDestruidos, invasoresDestruidos, invasoresSembrados;
    int vidaJugador = VIDA_JUGADOR;
    float proxRefuerzo = INTERVALO_REFUERZO, proxSync;
    float velBala = 18f, velInvasion = 4f;

    // ---- mira ----
    public static Vector2 miraPos = new Vector2(0f, -180f); float miraFeedback, cadencia, vidaMostradaHasta; string vidaTexto = "";
    // ---- mision ----
    class Paso { public string nombre, texto; }
    class MisionDef { public string titulo, desc, aviso, final; public Paso[] pasos; public bool disponible = true; public string nota = ""; }
    List<MisionDef> menu = new List<MisionDef>();
    MisionDef misionActual; int paso = -1; int baseBarcos, baseInvasores;
    // ---- audio ----
    AudioSource audioSrc; AudioClip clipDisparo, clipExplosion, clipExplosionBarco;
    AvionDeCarton avion; Camera cam; Vector2 scroll; GUIStyle sTxt, sCartel;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Crear() { if (FindFirstObjectByType<CesiumGeoreference>() == null) return; new GameObject("Guerra").AddComponent<Guerra>(); }

    void Start()
    {
        inst = this;
        var geo = FindFirstObjectByType<CesiumGeoreference>(); padre = geo != null ? geo.transform : null;
        var sh = Shader.Find("Universal Render Pipeline/Unlit"); if (sh == null) sh = Shader.Find("Unlit/Color");
        matBase = new Material(sh);
        audioSrc = gameObject.AddComponent<AudioSource>(); audioSrc.spatialBlend = 0f; audioSrc.volume = 0.8f;
        CargarSonidos();
        CargarModeloBarco();
        menu.Add(new MisionDef { titulo = "Modo Practica", desc = "Capital Federal y despues los aeropuertos.",             pasos = new[] { new Paso { nombre = "Capital Federal", texto = "MODO PRACTICA\nDestrui los objetivos verdes de Capital Federal" }, new Paso { nombre = "Aeropuertos", texto = "Ahora: destrui los objetivos de los aeropuertos" } },
            aviso = "Bienvenido, piloto. Antes de las misiones de verdad, vamos a entrenar la punteria.", final = "MODO PRACTICA COMPLETO!" });
        menu.Add(new MisionDef { titulo = "Nivel 1 - Barcos", desc = "Hundi 3 barcos en el rio y volve a Aeroparque.", aviso = "Base a piloto: detectamos barcos enemigos en el rio. Hundi 3 y volve.",
            pasos = new[] { new Paso { nombre = "Hundir 3 barcos", texto = "NIVEL 1\nDestrui 3 barcos" }, new Paso { nombre = "Volver a base", texto = "Bien! Ahora volve a Aeroparque" } }, final = "NIVEL 1 COMPLETO!" });
        menu.Add(new MisionDef { titulo = "Nivel 2 - Industria", desc = "Destrui la industria enemiga de La Plata (7 edificios).", aviso = "Detectamos actividad industrial enemiga en La Plata.",
            pasos = new[] { new Paso { nombre = "Industria", texto = "NIVEL 2\nDetectamos actividad industrial enemiga en La Plata. Destruila toda." } }, final = "NIVEL 2 COMPLETO!", nota = "en Godot no aparece en el menu; aca si" });
        menu.Add(new MisionDef { titulo = "Nivel 3 - Invasion", desc = "Tropas desembarcando por la costa. Destrui 10.", aviso = "Base a piloto: tropas enemigas desembarcando por la costa. No dejes que avancen.",
            pasos = new[] { new Paso { nombre = "Invasores", texto = "NIVEL 3\nUna oleada de tropas esta desembarcando por la costa. Destrui 10." } }, final = "NIVEL 3 COMPLETO!" });
        menu.Add(new MisionDef { titulo = "Nivel 4 - Defensa Total", desc = "Barcos, aeropuertos, Capital e industria, todo junto.",             pasos = new[] { new Paso { nombre = "Defensa Total", texto = "NIVEL 4 - FINAL\nAtaque combinado: barcos, aeropuertos, Capital e industria. A por todo!" } },
            aviso = "Base a piloto: ataque simultaneo en todos los frentes. Es ahora o nunca.", final = "FELICITACIONES, COMPLETASTE MODO GUERRA!" });
        menu.Add(new MisionDef { titulo = "Aeropuertos (prueba)", desc = "Solo destruir los 5 aeropuertos (paso del Modo Practica).", aviso = "Prueba: destrui los objetivos de los aeropuertos.",
            pasos = new[] { new Paso { nombre = "Aeropuertos", texto = "Destrui los objetivos de los aeropuertos" } }, final = "AEROPUERTOS DESTRUIDOS!", nota = "agregado por Unity para probar" });
    }

    async void CargarSonidos()
    {
        string fx = System.IO.Path.GetFullPath(System.IO.Path.Combine(Application.dataPath, "..", "..", "..", "simulador Claude", "FX"));
        clipDisparo = await Clip(System.IO.Path.Combine(fx, "Disparos", "disparo_canon.mp3"));
        clipExplosion = await Clip(System.IO.Path.Combine(fx, "Explosiones", "explosion_impacto.mp3"));
        clipExplosionBarco = await Clip(System.IO.Path.Combine(fx, "Explosiones", "explosion_barco.mp3"));
    }
    // ------------------------------------------------------------ modelo 3D del barco (el Aquitaine de la carpeta "simulador Claude", el mismo de Godot)
    GameObject plantillaBarco; const float LARGO_BARCO = 351f;   // Godot: 180 m x escala extra 1.95
    static Bounds AabbMundo(Transform raiz)
    {
        bool algo = false; var bb = new Bounds();
        foreach (var r in raiz.GetComponentsInChildren<Renderer>(true))
        {
            Mesh m = null; var mf = r.GetComponent<MeshFilter>(); if (mf != null) m = mf.sharedMesh; var sk = r as SkinnedMeshRenderer; if (sk != null) m = sk.sharedMesh;
            if (m == null) continue;
            Bounds b = m.bounds; Matrix4x4 M = r.transform.localToWorldMatrix;
            for (int i = 0; i < 8; i++)
            {
                Vector3 p = M.MultiplyPoint3x4(b.center + new Vector3((i & 1) == 0 ? -b.extents.x : b.extents.x, (i & 2) == 0 ? -b.extents.y : b.extents.y, (i & 4) == 0 ? -b.extents.z : b.extents.z));
                if (!algo) { bb = new Bounds(p, Vector3.zero); algo = true; } else bb.Encapsulate(p);
            }
        }
        return bb;
    }
    async void CargarModeloBarco()
    {
        try
        {
            string ruta = System.IO.Path.Combine(System.IO.Path.GetFullPath(System.IO.Path.Combine(Application.dataPath, "..", "..", "..", "simulador Claude")), "modelos_objetivos", "barcos", "aquitaine.glb");
            if (!System.IO.File.Exists(ruta)) { Debug.LogWarning("[Guerra] no encuentro el barco " + ruta); return; }
            var gl = new GLTFast.GltfImport();
            if (!await gl.Load(new System.Uri(ruta).AbsoluteUri)) { Debug.LogWarning("[Guerra] no pude cargar el barco"); return; }
            var pivote = new GameObject("BarcoPlantilla"); pivote.SetActive(false);
            var modelo = new GameObject("modelo"); modelo.transform.SetParent(pivote.transform, false);
            await gl.InstantiateMainSceneAsync(modelo.transform);
            // autocalibracion (el archivo viene con el eje largo de lado y ~25 grados de cabeceo propio, ver notas de Godot): eje largo a lo largo de X, y se elige el cabeceo (-25/0/+25) que deja el casco mas bajo
            Bounds b0 = AabbMundo(pivote.transform);
            Quaternion yaw = b0.size.z > b0.size.x ? Quaternion.Euler(0f, 90f, 0f) : Quaternion.identity, mejorQ = yaw; float mejor = float.MaxValue;
            foreach (float p in new[] { 0f })   // el modelo ya viene derecho en Unity (los 25 grados de Godot corrigen su importador)
            {
                var q = Quaternion.Euler(0f, 0f, p) * yaw; pivote.transform.rotation = q;
                float alto = AabbMundo(pivote.transform).size.y; if (alto < mejor - 0.001f) { mejor = alto; mejorQ = q; }
            }
            pivote.transform.rotation = mejorQ;
            float s = LARGO_BARCO / Mathf.Max(0.0001f, AabbMundo(pivote.transform).size.x);
            pivote.transform.localScale = Vector3.one * s;
            modelo.transform.position -= AabbMundo(pivote.transform).center;
            plantillaBarco = pivote;
            Debug.Log("[Guerra] barco listo: escala " + s.ToString("F3") + ", rotacion " + mejorQ.eulerAngles);
        }
        catch (System.Exception e) { Debug.LogWarning("[Guerra] error cargando el barco: " + e.Message); }
    }
    void AdjuntarModeloBarco(Blanco b)
    {
        var v = Instantiate(plantillaBarco, b.go.transform, false); v.name = "visual"; v.transform.localPosition = Vector3.zero; v.SetActive(true);
        b.go.transform.localScale = Vector3.one; b.rend.enabled = false; b.lado = 160f; b.conModelo = true;
        b.visuales = v.GetComponentsInChildren<Renderer>();
    }
    static void TintarBarco(Blanco b, Color c)
    {
        if (b.visuales == null) return;
        var t = Color.Lerp(Color.white, c, 0.7f); var mpb = new MaterialPropertyBlock(); mpb.SetColor("_BaseColor", t); mpb.SetColor("_Color", t); mpb.SetColor("baseColorFactor", t);
        foreach (var r in b.visuales) if (r != null) r.SetPropertyBlock(mpb);
    }

    async Awaitable<AudioClip> Clip(string ruta)
    {
        try
        {
            if (!System.IO.File.Exists(ruta)) return null;
            using (var req = UnityEngine.Networking.UnityWebRequestMultimedia.GetAudioClip(new System.Uri(ruta).AbsoluteUri, AudioType.MPEG))
            {
                var op = req.SendWebRequest(); while (!op.isDone) await Awaitable.NextFrameAsync();
                if (req.result == UnityEngine.Networking.UnityWebRequest.Result.Success) return UnityEngine.Networking.DownloadHandlerAudioClip.GetContent(req);
            }
        }
        catch { }
        return null;
    }
    void Sonar(AudioClip c) { if (c != null && audioSrc != null) audioSrc.PlayOneShot(c); }

    // ------------------------------------------------------------ creacion de blancos
    Blanco Nuevo(string nombre, string grupo, double lat, double lon, double h, float lado, Color color, int vida, Texture2D tex = null)
    {
        var go = GameObject.CreatePrimitive(tex != null ? PrimitiveType.Quad : PrimitiveType.Cube);
        var col = go.GetComponent<Collider>(); if (col != null) Destroy(col);
        go.name = nombre; go.transform.SetParent(padre, false); go.transform.localScale = Vector3.one * lado;
        var anc = go.AddComponent<CesiumGlobeAnchor>(); anc.longitudeLatitudeHeight = new double3(lon, lat, h);
        var mat = new Material(matBase); mat.SetColor("_BaseColor", color); mat.SetColor("_Color", color);
        if (tex != null)   // blanco con imagen (cartel que mira a la camara): recorte por transparencia, de los dos lados
        {
            color = Color.white; mat.SetColor("_BaseColor", color); mat.SetColor("_Color", color);
            mat.SetTexture("_BaseMap", tex); mat.SetTexture("_MainTex", tex);
            mat.SetFloat("_Cull", 0f); mat.SetFloat("_AlphaClip", 1f); mat.SetFloat("_Cutoff", 0.5f); mat.EnableKeyword("_ALPHATEST_ON"); mat.renderQueue = 2450;
        }
        var r = go.GetComponent<Renderer>(); r.sharedMaterial = mat; r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        var b = new Blanco { go = go, anc = anc, rend = r, grupo = grupo, nombre = nombre, vida = vida, vidaIni = vida, lat = lat, lon = lon, h = h, lado = lado, color = color, textura = tex != null };
        go.SetActive(false);
        blancos.Add(b); return b;
    }

    void Iniciar()
    {
        if (iniciado || padre == null) return;
        iniciado = true;
        double hAero; double la, lo;
        Aeropuertos.Buscar("Aeroparque", out la, out lo, out hAero);
        // 5 aeropuertos con blanco fijo a 120 m, contraatacan desde las dos cabeceras
        int nAero = 0;
        foreach (var n in new[] { "Ezeiza", "Morón", "El Palomar", "Campo de Mayo", "San Fernando" })
        {
            double lat, lon, h; if (!Aeropuertos.Buscar(n, out lat, out lon, out h)) { Debug.LogWarning("[Guerra] no encuentro el aeropuerto " + n); continue; }
            var b = Nuevo("Objetivo_" + n, "aeropuertos", lat, lon, h + 120.0, LADO_AEROPUERTO, new Color(1f, 0.12f, 0.1f), VIDA_BARCO, TexBlanco(++nAero));
            b.contraataca = true; Aeropuertos.Cabeceras(n, b.origenes);
            if (b.origenes.Count == 0) b.origenes.Add(Vector3.zero);   // se reemplaza por la posicion del blanco al disparar
        }
        // 10 objetivos verdes de Capital Federal (Godot: 120 m sobre el punto, cuadrado de 200, verde)
        for (int k = 0; k < VERDE_IDX.Length; k++) { int i = VERDE_IDX[k]; Nuevo("Verde_" + VERDE_N[i], "verde", VERDE_LAT[i], VERDE_LON[i], hAero + 120.0, 200f, new Color(0.08f, 1f, 0.2f), VIDA_BARCO, TexBlanco(k)); }
        // 20 barcos en el rio
        for (int i = 0; i < BARCOS_LAT.Length; i++)
        {
            var b = Nuevo("Barco_" + (i + 1), "barcos", BARCOS_LAT[i], BARCOS_LON[i], hAero + OFFSET_MAR + ALTURA_BARCO, 60f, new Color(0.22f, 0.25f, 0.3f), VIDA_BARCO);
            b.contraataca = true; b.go.transform.localScale = new Vector3(70f, 45f, 220f);
        }
        // industria en La Plata (7 edificios, color amarillo pastel)
        double hLP, laLP, loLP; Aeropuertos.Buscar("La Plata", out laLP, out loLP, out hLP);
        for (int i = 0; i < IND_LAT.Length; i++) Nuevo("Industria_" + (i + 1), "industria", IND_LAT[i], IND_LON[i], hLP + LADO_INDUSTRIA * 0.5, LADO_INDUSTRIA, new Color(1f, 0.92f, 0.65f), VIDA_INDUSTRIA);
        // invasion inicial: 4 zonas x 3 destinos x 3 unidades = 36
        for (int z = 0; z < ZONAS_N.Length; z++)
            foreach (var d in PERMITIDOS) for (int j = 0; j < 3; j++) CrearInvasor(ZONAS_LAT[z] + UnityEngine.Random.Range(-0.025f, 0.025f), ZONAS_LON[z] + UnityEngine.Random.Range(-0.025f, 0.025f), d);
    }

    // blancos con imagen: se alternan los tres modelos (colorido, botón rojo, negro) para elegir el que quede mejor
    static readonly string[] TEX_BLANCOS = { "blancos/blanco_colores", "blancos/blanco_rojo", "blancos/blanco_colores", "blancos/blanco_rojo", "blancos/blanco_negro" };   // mayormente colorido y rojo (los del aire); el negro, de vez en cuando
    Texture2D TexBlanco(int i) { var t = Resources.Load<Texture2D>(TEX_BLANCOS[((i % TEX_BLANCOS.Length) + TEX_BLANCOS.Length) % TEX_BLANCOS.Length]); return t; }

    double AlturaSuelo() { double la, lo, h; Aeropuertos.Buscar("Aeroparque", out la, out lo, out h); return h; }

    string MasCercanoLibre(double lat, double lon)
    {
        string mejor = ""; double md = double.MaxValue;
        foreach (var n in PERMITIDOS)
        {
            if (tomados.Contains(n)) continue;
            double la, lo, h; if (!Aeropuertos.Buscar(n, out la, out lo, out h)) continue;
            double dx = (lo - lon) * 111320.0 * Mathf.Cos((float)lat * Mathf.Deg2Rad), dy = (la - lat) * 111320.0, d = dx * dx + dy * dy;
            if (d < md) { md = d; mejor = n; }
        }
        return mejor;
    }

    void CrearInvasor(double lat, double lon, string destino)
    {
        int vivos = 0; foreach (var x in blancos) if (x.grupo == "invasion" && !x.muerto) vivos++;
        if (vivos >= MAX_INVASORES) return;
        if (destino == "") destino = MasCercanoLibre(lat, lon);
        if (destino == "") return;
        var b = Nuevo("Invasion_" + Time.frameCount + "_" + blancos.Count, "invasion", lat, lon, AlturaSuelo() + ALTURA_FLOTE + LADO_INVASOR * 0.5, LADO_INVASOR, new Color(0.9f, 0.1f, 0.05f), VIDA_INVASOR);
        b.destino = destino; invasoresSembrados++;
    }

    // ------------------------------------------------------------ grupos activos segun el paso de mision
    List<string> GruposPaso()
    {
        var l = new List<string>();
        if (misionActual == null || paso < 0 || paso >= misionActual.pasos.Length) return l;
        string n = misionActual.pasos[paso].nombre;
        if (n == "Capital Federal") l.Add("verde");
        else if (n == "Aeropuertos") l.Add("aeropuertos");
        else if (n == "Hundir 3 barcos" || n == "Volver a base") l.Add("barcos");
        else if (n == "Industria") l.Add("industria");
        else if (n == "Invasores") l.Add("invasion");
        else if (n == "Defensa Total") { l.Add("verde"); l.Add("aeropuertos"); l.Add("barcos"); l.Add("industria"); }
        return l;
    }

    void Sincronizar()
    {
        if (plantillaBarco != null) foreach (var bb in blancos) if (bb.grupo == "barcos" && !bb.conModelo && !bb.muerto) AdjuntarModeloBarco(bb);
        var g = modoGuerra ? GruposPaso() : new List<string>();
        foreach (var b in blancos)
        {
            if (b.muerto) continue;
            bool act = modoGuerra && g.Contains(b.grupo);
            if (b.go.activeSelf != act) b.go.SetActive(act);
        }
    }

    // ------------------------------------------------------------ ciclo
    void Update()
    {
        if (avion == null) avion = FindFirstObjectByType<AvionDeCarton>();
        if (cam == null) cam = Camera.main;
        float dt = Time.deltaTime;
        if (iniciado) { ActualizarMundo(dt); }
        if (cam != null) foreach (var bt in blancos) if (bt.textura && !bt.muerto && bt.go.activeSelf) { Vector3 d = bt.go.transform.position - cam.transform.position; if (d.sqrMagnitude > 1f) bt.go.transform.rotation = Quaternion.LookRotation(d, cam.transform.up); }   // el cartel siempre mira a la camara
        proxSync -= dt; if (proxSync <= 0f) { proxSync = 0.5f; Sincronizar(); }
        ActualizarBalas(dt); ActualizarEfectos(dt); ActualizarContraataque(dt);
        miraFeedback = Mathf.Max(0f, miraFeedback - dt);
        if (!MiraActiva || avion == null || cam == null || AvionDeCarton.enAuto) return;

        // mira: se acumula con el stick derecho (180 px/s), queda donde la dejes
        var gp = Gamepad.current;
        if (gp != null && !Controles.escribiendo)
        {
            Vector2 s = gp.rightStick.ReadValue(); if (s.magnitude < 0.2f) s = Vector2.zero;
            miraPos += new Vector2(s.x, -s.y) * 180f * dt;
        }
        float lx = Mathf.Max(0f, Screen.width * 0.5f - 32f - 24f), ly = Mathf.Max(0f, Screen.height * 0.5f - 32f - 24f);
        miraPos.x = Mathf.Clamp(miraPos.x, -lx, lx); miraPos.y = Mathf.Clamp(miraPos.y, -ly, ly);

        cadencia = Mathf.Max(0f, cadencia - dt);
        if (modoGuerra && Controles.Flanco("armamento") && cadencia <= 0f) { cadencia = CADENCIA_DISPARO; Disparar(); }
        if (modoGuerra) ActualizarMision();
    }

    void ActualizarMundo(float dt)
    {
        // refuerzos de invasion cada 30 s
        proxRefuerzo -= dt;
        if (proxRefuerzo <= 0f)
        {
            proxRefuerzo = INTERVALO_REFUERZO;
            var libres = new List<string>(); foreach (var n in PERMITIDOS) if (!tomados.Contains(n)) libres.Add(n);
            if (libres.Count > 0)
            {
                int z = UnityEngine.Random.Range(0, ZONAS_N.Length);
                for (int j = 0; j < POR_REFUERZO; j++) CrearInvasor(ZONAS_LAT[z] + UnityEngine.Random.Range(-0.025f, 0.025f), ZONAS_LON[z] + UnityEngine.Random.Range(-0.025f, 0.025f), libres[UnityEngine.Random.Range(0, libres.Count)]);
            }
        }
        double hSuelo = AlturaSuelo();
        for (int i = blancos.Count - 1; i >= 0; i--)
        {
            var b = blancos[i];
            if (b.muerto) { blancos.RemoveAt(i); continue; }
            if (b.grupo == "barcos")
            {
                double mpg = 111320.0 * Mathf.Cos((float)b.lat * Mathf.Deg2Rad);
                b.lon -= VELOCIDAD_BARCOS / mpg * dt;
                if (b.lon <= LON_COSTA) { Desembarcar(b); blancos.RemoveAt(i); continue; }
                b.anc.longitudeLatitudeHeight = new double3(b.lon, b.lat, b.h);
            }
            else if (b.grupo == "invasion")
            {
                if (b.destino == "" || tomados.Contains(b.destino)) { b.destino = MasCercanoLibre(b.lat, b.lon); if (b.destino == "") continue; }
                double la, lo, h; if (!Aeropuertos.Buscar(b.destino, out la, out lo, out h)) continue;
                double mpgLon = 111320.0 * Mathf.Cos((float)b.lat * Mathf.Deg2Rad);
                double dist = System.Math.Sqrt(System.Math.Pow((lo - b.lon) * mpgLon, 2) + System.Math.Pow((la - b.lat) * 111320.0, 2));
                if (dist < RADIO_CAPTURA) { Capturar(b.destino); Quitar(b); blancos.RemoveAt(i); continue; }
                double f = System.Math.Min(1.0, velInvasion * dt / dist);
                b.lat += (la - b.lat) * f; b.lon += (lo - b.lon) * f;
                b.anc.longitudeLatitudeHeight = new double3(b.lon, b.lat, hSuelo + ALTURA_FLOTE + LADO_INVASOR * 0.5);
            }
        }
    }

    void Quitar(Blanco b) { b.muerto = true; if (b.go != null) Destroy(b.go); }

    void Desembarcar(Blanco barco)
    {
        double lat = barco.lat, lon = barco.lon; Quitar(barco);
        for (int i = 0; i < 5; i++) CrearInvasor(lat, lon, "");
    }

    void Capturar(string nombre)
    {
        if (tomados.Contains(nombre)) return;
        tomados.Add(nombre);
        double la, lo, h; if (!Aeropuertos.Buscar(nombre, out la, out lo, out h)) return;
        var m = Nuevo("AeropuertoTomado_" + nombre, "tomado", la, lo, h + 120.0, LADO_TOMADO, new Color(0.45f, 0.28f, 0.12f), VIDA_CAPTURA);   // queda oculta, como en Godot (no esta en ningun grupo de mision)
        Mapeo.Cartel("Aeropuerto TOMADO por la invasion: " + nombre, 3f);
        for (int j = 0; j < 5; j++) CrearInvasor(la, lo, "");
    }

    // ------------------------------------------------------------ disparo
    // Punta del modelo en la direccion de avance (NO el pivote del avion, que queda atras: ese era el bug de Godot)
    Vector3 Nariz()
    {
        Vector3 fwd = avion.transform.forward;
        var rs = avion.GetComponentsInChildren<Renderer>();
        if (rs.Length == 0) return avion.transform.position + fwd * 8f;
        Bounds bb = rs[0].bounds; foreach (var r in rs) bb.Encapsulate(r.bounds);
        float max = 0f; Vector3 c = bb.center, e = bb.extents;
        for (int i = 0; i < 8; i++)
        {
            Vector3 esq = c + new Vector3((i & 1) == 0 ? -e.x : e.x, (i & 2) == 0 ? -e.y : e.y, (i & 4) == 0 ? -e.z : e.z);
            max = Mathf.Max(max, Vector3.Dot(esq - c, fwd));
        }
        return c + fwd * max;
    }

    void Disparar()
    {
        var pt = new Vector2(Screen.width * 0.5f + miraPos.x, Screen.height - (Screen.height * 0.5f + miraPos.y));
        Ray rayo = cam.ScreenPointToRay(pt);
        Vector3 dir = rayo.direction.normalized, o = rayo.origin;
        // impacto: tolerancia por cercania al centro del blanco, mas generosa mientras mas lejos (40 m cerca -> 100 m lejos; 22 m para invasores)
        Blanco mejor = null; float mejorD = 100f; Vector3 mejorP = o + dir * 5000f;
        foreach (var b in blancos)
        {
            if (b.muerto || !b.go.activeSelf) continue;
            Vector3 c = b.go.transform.position; float avance = Vector3.Dot(c - o, dir); if (avance <= 0f) continue;
            float tol = Mathf.Lerp(40f, 100f, Mathf.Clamp01(avance / 2500f)); if (b.grupo == "invasion") tol = Mathf.Min(tol, 22f);
            tol = Mathf.Max(tol, b.lado * 0.5f);   // un cubo grande se acierta por su tamano
            float d = Vector3.Distance(o + dir * avance, c);
            if (d < tol && d < mejorD + (b.lado * 0.5f)) { if (mejor == null || d < mejorD) { mejorD = d; mejor = b; mejorP = o + dir * avance; } }
        }
        Vector3 origen = Nariz();
        Vector3 destino = mejor != null ? mejorP : o + dir * 3000f;
        Vector3 haciaDestino = destino - origen; if (haciaDestino.sqrMagnitude < 1f) { haciaDestino = avion.transform.forward * 10f; destino = origen + haciaDestino; }
        Traza(origen, destino);
        Sonar(clipDisparo);
        if (mejor == null) return;
        Efecto(mejorP, 5f, 1.2f, new Color(1f, 0.1f, 0.05f), true);
        Impacto(mejor);
    }

    void Impacto(Blanco b)
    {
        b.vida--; miraFeedback = 0.25f; b.mostrarVida = true;
        vidaTexto = Mathf.Max(0, b.vida).ToString(); vidaMostradaHasta = Time.time + 0.25f;
        if (b.contraataca && !b.activadoAtaque) { b.activadoAtaque = true; b.rafTemp = CARGA_INICIAL; b.rafBalas = RAFAGA; }
        if (b.vida > 0)
        {
            var c = b.grupo == "invasion" ? Semaforo(b.vida) : ColorVida(b.vida, b.vidaIni); c.a = 1f;
            if (b.textura) { c = Color.Lerp(new Color(1f, 0.2f, 0.15f), new Color(0.85f, 0.85f, 0.85f), Mathf.Clamp01(b.vida / (float)b.vidaIni)); c.a = 1f; }   // el blanco con imagen se va oscureciendo y tiñendo de rojo
            b.rend.material.SetColor("_BaseColor", c); b.rend.material.SetColor("_Color", c); TintarBarco(b, c);
            return;
        }
        // destruido
        Efecto(b.go.transform.position, b.lado * 0.7f, 0.9f, new Color(1f, 0.85f, 0.1f), false);
        Sonar(b.grupo == "barcos" ? clipExplosionBarco : clipExplosion);
        if (b.grupo == "aeropuertos") objDestruidos++;
        else if (b.grupo == "verde") verdeDestruidos++;
        else if (b.grupo == "barcos") barcosDestruidos++;
        else if (b.grupo == "industria") industriaDestruidos++;
        else if (b.grupo == "invasion") invasoresDestruidos++;
        else if (b.grupo == "tomado") { string n = b.nombre.Replace("AeropuertoTomado_", ""); tomados.Remove(n); }
        Quitar(b);
    }

    void Traza(Vector3 a, Vector3 b)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube); Destroy(go.GetComponent<Collider>());
        go.transform.SetParent(padre, true);
        float d = Mathf.Max(0.05f, Vector3.Distance(a, b));
        go.transform.position = (a + b) * 0.5f; go.transform.rotation = Quaternion.LookRotation((b - a).normalized, Vector3.up);
        go.transform.localScale = new Vector3(0.5f, 0.5f, d);
        var m = new Material(matBase); m.SetColor("_BaseColor", new Color(1f, 0.25f, 0.03f)); m.SetColor("_Color", new Color(1f, 0.25f, 0.03f));
        go.GetComponent<Renderer>().sharedMaterial = m; go.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        Destroy(go, 0.12f);
    }

    void Efecto(Vector3 pos, float radio, float dur, Color col, bool parpadeo)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Sphere); Destroy(go.GetComponent<Collider>());
        go.transform.SetParent(padre, true); go.transform.position = pos; go.transform.localScale = Vector3.one * radio * 2f;
        var m = new Material(matBase); m.SetColor("_BaseColor", col); m.SetColor("_Color", col);
        go.GetComponent<Renderer>().sharedMaterial = m; go.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        efectos.Add(new FxObj { go = go, t = 0f, dur = dur, mat = m, fin = Vector3.one * radio * 3f, parpadeo = parpadeo });
    }

    void ActualizarEfectos(float dt)
    {
        for (int i = efectos.Count - 1; i >= 0; i--)
        {
            var e = efectos[i]; e.t += dt;
            if (e.go == null || e.t >= e.dur) { if (e.go != null) Destroy(e.go); efectos.RemoveAt(i); continue; }
            if (e.parpadeo) e.go.SetActive(((int)(e.t / 0.2f)) % 2 == 0);   // titila 3 veces, como la marca de impacto de Godot
            else e.go.transform.localScale = Vector3.Lerp(e.go.transform.localScale, e.fin, dt * 4f);
        }
    }

    // ------------------------------------------------------------ contraataque del enemigo
    // Pablo (14/11): las bolitas naranjas enemigas no sirven; el sistema se rehace despues. Mientras tanto, el enemigo NO dispara.
    static bool enemigoDispara = false;
    void ActualizarContraataque(float dt)
    {
        if (!enemigoDispara) { for (int i = balas.Count - 1; i >= 0; i--) { if (balas[i].go != null) Destroy(balas[i].go); balas.RemoveAt(i); } return; }
        if (!modoGuerra || avion == null) return;
        foreach (var b in blancos)
        {
            if (b.muerto || !b.activadoAtaque || !b.go.activeSelf) continue;
            b.rafTemp -= dt;
            if (b.rafTemp > 0f) continue;
            if (b.rafBalas > 0) { DispararBarco(b); b.rafBalas--; b.rafTemp = INTERVALO_BALAS; }
            else { b.rafBalas = RAFAGA; b.rafTemp = ENFRIAMIENTO; }
        }
    }

    void DispararBarco(Blanco b)
    {
        var origenes = new List<Vector3>(b.origenes);
        if (b.grupo != "aeropuertos" || origenes.Count == 0) { origenes.Clear(); origenes.Add(b.go.transform.position + b.go.transform.up * 10f); }
        for (int i = 0; i < origenes.Count; i++) if (origenes[i] == Vector3.zero) origenes[i] = b.go.transform.position;
        foreach (var o in origenes)
        {
            Vector3 d = avion.transform.position - o; if (d.magnitude < 0.5f) continue; d.Normalize();
            var go = GameObject.CreatePrimitive(PrimitiveType.Sphere); Destroy(go.GetComponent<Collider>());
            go.transform.SetParent(padre, true); go.transform.position = o; go.transform.localScale = Vector3.one * 3f;
            var m = new Material(matBase); m.SetColor("_BaseColor", new Color(1f, 0.45f, 0.05f)); m.SetColor("_Color", new Color(1f, 0.45f, 0.05f));
            go.GetComponent<Renderer>().sharedMaterial = m; go.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            balas.Add(new Bala { go = go, dir = d });
        }
    }

    void ActualizarBalas(float dt)
    {
        for (int i = balas.Count - 1; i >= 0; i--)
        {
            var b = balas[i];
            if (b.go == null || avion == null) { balas.RemoveAt(i); continue; }
            float avance = velBala * dt; b.go.transform.position += b.dir * avance; b.recorrido += avance;
            if (Vector3.Distance(b.go.transform.position, avion.transform.position) < RADIO_IMPACTO_BALA) { RecibirImpacto(); Destroy(b.go); balas.RemoveAt(i); }
            else if (b.recorrido >= ALCANCE_BALA) { Destroy(b.go); balas.RemoveAt(i); }
        }
    }

    void RecibirImpacto() { if (vidaJugador <= 0) return; vidaJugador--; miraFeedback = 0.25f; }   // al llegar a 0 no pasa nada mas (igual que Godot)

    // ------------------------------------------------------------ misiones
    void Empezar(MisionDef m)
    {
        if (!iniciado) Iniciar();
        misionActual = m; paso = 0; modoGuerra = true;
        baseBarcos = barcosDestruidos; baseInvasores = invasoresDestruidos;
        Mapeo.Cartel(m.aviso + "\n\n" + m.pasos[0].texto, 5f);
        Sincronizar();
    }

    bool PasoCompleto()
    {
        string n = misionActual.pasos[paso].nombre;
        if (n == "Aeropuertos") return objDestruidos >= 5;
        if (n == "Hundir 3 barcos") return barcosDestruidos - baseBarcos >= 3;
        if (n == "Volver a base")
        {
            // aterrizar sobre la pista de Aeroparque (la pista mide ~2,4 km: el centro queda a 1,2 km de cada cabecera, el radio de 600 m del centro no alcanzaba)
            if (avion == null || !avion.enTierra) return false;
            if (Aeropuertos.SobrePista("Aeroparque", avion.transform.position) == 1) return true;
            Vector3 d = avion.transform.position - Aeropuertos.PosMundo("Aeroparque"); d.y = 0f;
            return d.magnitude < 2000f;
        }
        if (n == "Industria") return industriaDestruidos >= IND_LAT.Length;
        if (n == "Invasores") return invasoresDestruidos - baseInvasores >= 10;
        if (n == "Capital Federal") return verdeDestruidos >= VERDE_IDX.Length;
        if (n == "Defensa Total") return verdeDestruidos >= VERDE_IDX.Length && objDestruidos >= 5 && barcosDestruidos >= BARCOS_LAT.Length && industriaDestruidos >= IND_LAT.Length;
        return false;
    }

    void ActualizarMision()
    {
        if (misionActual == null || paso < 0) return;
        if (!PasoCompleto()) return;
        paso++;
        if (paso >= misionActual.pasos.Length) { Mapeo.Cartel(misionActual.final, 4f); Torre.NivelCompleto(); Torre.Hablar("guerra_mision_ok", 2.5f); misionActual = null; paso = -1; Sincronizar(); }
        else { Mapeo.Cartel(misionActual.pasos[paso].texto, 4f); Sincronizar(); }
    }

    // ------------------------------------------------------------ interfaz
    void Estilos()
    {
        if (sTxt != null) return;
        sTxt = new GUIStyle(GUI.skin.label) { fontSize = 13, fontStyle = FontStyle.Bold }; sTxt.normal.textColor = Color.white;
    }

    // mira: aro oscuro con 4 muescas y transparente por dentro (se ve todo el fondo) + cruz roja al centro
    static Texture2D texAro;
    static Texture2D CrearAro()
    {
        const int n = 128; var t = new Texture2D(n, n, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp };
        var px = new Color32[n * n]; float c0 = (n - 1) * 0.5f, rExt = 62f, rInt = 57.5f;
        for (int y = 0; y < n; y++) for (int x = 0; x < n; x++)
        {
            float dx = x - c0, dy = y - c0, d = Mathf.Sqrt(dx * dx + dy * dy); float a = 0f;
            float anillo = Mathf.Clamp01(Mathf.Min(rExt - d, d - rInt) + 0.5f);
            bool muesca = d < rInt + 1f && d > 46f && (Mathf.Abs(dx) < 2.5f || Mathf.Abs(dy) < 2.5f) && (Mathf.Abs(dx) < 2.5f != Mathf.Abs(dy) < 2.5f);
            a = Mathf.Max(anillo, muesca ? 1f : 0f);
            float borde = (a > 0f) ? 0f : Mathf.Clamp01(Mathf.Min(rExt + 2f - d, d - (rInt - 2f)) + 0.5f) * 0.35f;
            if (a > 0f) px[y * n + x] = new Color32(28, 28, 30, (byte)(a * 235f)); else if (borde > 0f && (d > rExt || d < rInt)) px[y * n + x] = new Color32(255, 255, 255, (byte)(borde * 255f)); else px[y * n + x] = new Color32(0, 0, 0, 0);
        }
        t.SetPixels32(px); t.Apply(); return t;
    }
    static void DibujarMira(float cx, float cy, bool golpe)
    {
        if (texAro == null) texAro = CrearAro();
        var c = GUI.color; GUI.color = Color.white; GUI.DrawTexture(new Rect(cx - 36f, cy - 36f, 72f, 72f), texAro);
        GUI.color = golpe ? new Color(1f, 0.55f, 0.05f, 1f) : new Color(0.95f, 0.05f, 0.05f, 1f);
        float a0 = 5f, a1 = 16f, g = 2f; GUI.DrawTexture(new Rect(cx - a1, cy - g * 0.5f, a1 - a0, g), Texture2D.whiteTexture); GUI.DrawTexture(new Rect(cx + a0, cy - g * 0.5f, a1 - a0, g), Texture2D.whiteTexture); GUI.DrawTexture(new Rect(cx - g * 0.5f, cy - a1, g, a1 - a0), Texture2D.whiteTexture); GUI.DrawTexture(new Rect(cx - g * 0.5f, cy + a0, g, a1 - a0), Texture2D.whiteTexture);
        GUI.color = c;
    }

    void OnGUI()
    {
        if (!modoGuerra && bonusMira && avion != null && !AvionDeCarton.enAuto)
        {
            DibujarMira(Screen.width * 0.5f + miraPos.x, Screen.height * 0.5f + miraPos.y, false); return;
        }
        if (!modoGuerra || avion == null) return;
        Estilos();
        float W = Screen.width, H = Screen.height;
        if (!AvionDeCarton.enAuto)
        {
            // cruz: dos barras blancas teñidas de rojo (naranja al pegar)
            float cx = W * 0.5f + miraPos.x, cy = H * 0.5f + miraPos.y;
            DibujarMira(cx, cy, miraFeedback > 0f);
            if (Time.time < vidaMostradaHasta) { var s = new GUIStyle(sTxt) { fontSize = 26 }; GUI.Label(new Rect(cx + 20f, cy - 10f, 80f, 34f), vidaTexto, s); }
        }
        // contadores del paso actual + vida del jugador (arriba, a la derecha de la cajita DINERO)
        var g = GruposPaso(); float x = 472f, y = 28f;
        var box = new Rect(x, y, 230f, 20f + 18f * (g.Count + (g.Contains("invasion") ? 0 : 0)) + 22f);
        GUI.Box(box, "");
        float yy = y + 2f;
        if (g.Contains("verde")) { GUI.Label(new Rect(x + 6, yy, 220, 18), "CAPITAL: " + verdeDestruidos + "/" + VERDE_IDX.Length, sTxt); yy += 18f; }
        if (g.Contains("aeropuertos")) { GUI.Label(new Rect(x + 6, yy, 220, 18), "AEROPUERTOS: " + objDestruidos + "/5", sTxt); yy += 18f; }
        if (g.Contains("barcos")) { GUI.Label(new Rect(x + 6, yy, 220, 18), "BARCOS: " + barcosDestruidos + "/" + BARCOS_LAT.Length, sTxt); yy += 18f; }
        if (g.Contains("industria")) { GUI.Label(new Rect(x + 6, yy, 220, 18), "INDUSTRIA: " + industriaDestruidos + "/" + IND_LAT.Length, sTxt); yy += 18f; }
        if (g.Contains("invasion")) { GUI.Label(new Rect(x + 6, yy, 220, 18), "INVASORES: " + invasoresDestruidos + "/" + invasoresSembrados, sTxt); yy += 18f; }
        if (g.Count == 0) { GUI.Label(new Rect(x + 6, yy, 220, 18), "Modo guerra sin mision", sTxt); yy += 18f; }
        for (int i = 0; i < VIDA_JUGADOR; i++)
        {
            var cc = GUI.color; GUI.color = i < vidaJugador ? new Color(0.2f, 0.9f, 0.3f) : new Color(0.15f, 0.15f, 0.15f);
            GUI.DrawTexture(new Rect(x + 6f + i * 22f, yy + 2f, 18f, 10f), Texture2D.whiteTexture); GUI.color = cc;
        }
        // numerito de vida sobre los blancos ya tocados
        if (cam != null)
            foreach (var b in blancos)
            {
                if (b.muerto || !b.mostrarVida || !b.go.activeSelf) continue;
                Vector3 sp = cam.WorldToScreenPoint(b.go.transform.position); if (sp.z <= 0f) continue;
                GUI.Label(new Rect(sp.x - 20f, H - sp.y - 14f, 60f, 28f), b.vida.ToString(), new GUIStyle(sTxt) { fontSize = 22 });
            }
    }

    // ===== pestana "Guerra" del panel de abajo =====
    public static void PestanaGuerra(GUIStyle txt, GUIStyle btn, GUIStyle titulo) { if (inst != null) inst.DibujarPestana(txt, btn, titulo); }

    void DibujarPestana(GUIStyle txt, GUIStyle btn, GUIStyle titulo)
    {
        GUILayout.Label("MODO GUERRA   (ON = aparecen los blancos de la mision elegida; sin mision no se ve nada)", titulo);
        bool cp = Tablet.Compacto;
        scroll = GUILayout.BeginScrollView(scroll, false, true);
        if (cp) GUILayout.BeginVertical(); else GUILayout.BeginHorizontal();
        var antes = GUI.backgroundColor; if (modoGuerra) GUI.backgroundColor = Color.red;
        if (GUILayout.Button(modoGuerra ? "MODO GUERRA: ON (apagar)" : "MODO GUERRA: OFF (prender)", btn, GUILayout.Width(210), GUILayout.Height(24)))
        {
            modoGuerra = !modoGuerra; if (modoGuerra && !iniciado) Iniciar();
            if (modoGuerra) { Torre.Sonar("guerra_on"); Torre.Hablar("guerra_invasion", 3.5f); } Sincronizar(); Mapeo.Cartel(modoGuerra ? "Modo guerra: ON" : "Modo guerra: OFF", 1.5f);
        }
        GUI.backgroundColor = antes;
        GUILayout.Label(cp ? "Disparo: " + Controles_Texto("armamento") + ". La mira se mueve con el stick derecho." : "  Disparo: " + Controles_Texto("armamento") + "     La mira se mueve con el stick derecho.", txt);
        if (cp) GUILayout.EndVertical(); else GUILayout.EndHorizontal();
        if (misionActual != null)
        {
            if (cp) GUILayout.BeginVertical(); else GUILayout.BeginHorizontal();
            GUILayout.Label("Mision: " + misionActual.titulo + "   paso " + (paso + 1) + "/" + misionActual.pasos.Length + ":  " + misionActual.pasos[paso].texto.Replace("\n", " - "), txt);
            if (GUILayout.Button("Cancelar", btn, GUILayout.Width(80), GUILayout.Height(20))) { misionActual = null; paso = -1; Sincronizar(); }
            if (cp) GUILayout.EndVertical(); else GUILayout.EndHorizontal();
        }
        GUILayout.Label("MISIONES  (elegir una prende el modo guerra solo)", titulo);
        foreach (var m in menu)
        {
            if (cp) GUILayout.BeginVertical(); else GUILayout.BeginHorizontal();
            GUI.enabled = m.disponible;
            if (GUILayout.Button(m.titulo, btn, GUILayout.Width(190), GUILayout.Height(20))) Empezar(m);
            GUI.enabled = true;
            GUILayout.Label(m.desc + (m.nota != "" ? "   (" + m.nota + ")" : ""), txt);
            if (cp) { GUILayout.EndVertical(); GUILayout.Space(4); } else GUILayout.EndHorizontal();
        }
        if (cp) { VelocidadesGuerraTablet(btn, txt); GUILayout.Label("Aeropuertos tomados por la invasion: " + (tomados.Count == 0 ? "ninguno" : string.Join(", ", tomados)), txt); GUILayout.EndScrollView(); return; }
        GUILayout.BeginHorizontal();
        GUILayout.Label("Velocidad de la bala enemiga: " + velBala.ToString("F0") + " m/s", txt, GUILayout.Width(250));
        if (GUILayout.Button("-", btn, GUILayout.Width(24), GUILayout.Height(20))) velBala = Mathf.Max(2f, velBala - 2f);
        if (GUILayout.Button("+", btn, GUILayout.Width(24), GUILayout.Height(20))) velBala += 2f;
        GUILayout.Label("     Velocidad de la invasion: " + velInvasion.ToString("F0") + " m/s", txt, GUILayout.Width(250));
        if (GUILayout.Button("-", btn, GUILayout.Width(24), GUILayout.Height(20))) velInvasion = Mathf.Max(1f, velInvasion - 1f);
        if (GUILayout.Button("+", btn, GUILayout.Width(24), GUILayout.Height(20))) velInvasion = Mathf.Min(20f, velInvasion + 1f);
        if (GUILayout.Button("Curar al piloto", btn, GUILayout.Width(110), GUILayout.Height(20))) vidaJugador = VIDA_JUGADOR;
        GUILayout.EndHorizontal();
        GUILayout.Label("Aeropuertos tomados por la invasion: " + (tomados.Count == 0 ? "ninguno" : string.Join(", ", tomados)), txt);
        GUILayout.EndScrollView();
    }

    void VelocidadesGuerraTablet(GUIStyle btn, GUIStyle txt)
    {
        GUILayout.BeginHorizontal();
        GUILayout.Label("Velocidad de la bala enemiga: " + velBala.ToString("F0") + " m/s", txt);
        if (GUILayout.Button("-", btn, GUILayout.Width(24), GUILayout.Height(20))) velBala = Mathf.Max(2f, velBala - 2f);
        if (GUILayout.Button("+", btn, GUILayout.Width(24), GUILayout.Height(20))) velBala += 2f;
        GUILayout.EndHorizontal();
        GUILayout.BeginHorizontal();
        GUILayout.Label("Velocidad de la invasion: " + velInvasion.ToString("F0") + " m/s", txt);
        if (GUILayout.Button("-", btn, GUILayout.Width(24), GUILayout.Height(20))) velInvasion = Mathf.Max(1f, velInvasion - 1f);
        if (GUILayout.Button("+", btn, GUILayout.Width(24), GUILayout.Height(20))) velInvasion = Mathf.Min(20f, velInvasion + 1f);
        GUILayout.EndHorizontal();
        if (GUILayout.Button("Curar al piloto", btn, GUILayout.Width(140), GUILayout.Height(22))) vidaJugador = VIDA_JUGADOR;
    }

    static string Controles_Texto(string id)
    {
        var a = Controles.ACCIONES.Find(x => x.id == id); if (a == null) return "-";
        return (a.conTecla ? Controles.TextoTecla(a) + " / " : "") + "joystick: " + Controles.TextoJoystick(a);
    }
}
