// Mapa grande (calles de OpenStreetMap), minimapa chico (camara cenital) y modo auto ("Bajar del avion").
// Portado de principal.gd / minimapa_camara.gd (ver RESPUESTA_MAPA_Y_MANEJO_AUTO.md y PROMPT_CODIGO_LITERAL_MAPA_Y_AUTO).
// Los autos (.glb) y el sonido se leen DIRECTO de la carpeta del Godot ("simulador Claude"), sin copiarlos.
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Networking;
using UnityEngine.Rendering.Universal;
using CesiumForUnity;

public class MapaAuto : MonoBehaviour
{
    // ---- constantes de Godot ----
    const int ZOOM_MIN = 3, ZOOM_MAX = 19;
    const float MAPA_MIN = 200f, MAPA_MAX = 900f, MINI_MIN = 150f, MINI_MAX = 700f;
    const float V_MAX = 55.56f, ACEL = 8f, FRENO = 12f, V_REVERSA = 4f, GIRO = 40f, ALTURA_A_PIE = 0.75f;
    const float FOV_MINI = 6f, ALTURA_MINI = 5000f;

    class TipoAuto { public string nombre; public string archivo; public float largo; public float rotExtra; }
    static string Godot { get { return System.IO.Path.GetFullPath(System.IO.Path.Combine(Application.dataPath, "..", "..", "..", "simulador Claude")); } }
    static readonly TipoAuto[] AUTOS =
    {
        new TipoAuto { nombre = "Patrullero",     archivo = "police_car.glb",   largo = 5.52f,  rotExtra = 0f },
        new TipoAuto { nombre = "BMW M3 azul",    archivo = "bmw_m3_e46.glb",   largo = 5.388f, rotExtra = 0f },
    };
    static string RutaAuto(TipoAuto t) { return System.IO.Path.Combine(Godot, "modelos_vehiculos", t.archivo); }

    AvionDeCarton avion; CesiumGlobeAnchor anclaAvion;
    GameObject autoGO; CesiumGlobeAnchor anclaAuto; Transform modeloAuto;
    GLTFast.GltfImport[] importados = new GLTFast.GltfImport[AUTOS.Length];
    Vector3 centroNativo; float escalaAuto; bool autoInvertido;
    float velAuto; bool cargandoAuto; string textoAviso = ""; float avisoHasta;
    bool selectorAbierto;
    AudioClip clipAuto; AudioSource fuenteAuto; AudioSource[] capasAuto;
    // selector de aeropuertos en el mapa grande: puntos amarillos; al tocar uno aparecen 3 bolitas (despegar / en el aire / aterrizar)
    List<Aeropuertos.PuntoMapa> puntosAero; float proxPuntos; string aeroSel = ""; Texture2D circulo; GUIStyle sAero, sAeroSel;
    Texture2D Circulo()
    {
        if (circulo != null) return circulo;
        const int n = 64; var px = new Color32[n * n];
        for (int y = 0; y < n; y++) for (int x = 0; x < n; x++) { float d = Mathf.Sqrt((x - 31.5f) * (x - 31.5f) + (y - 31.5f) * (y - 31.5f)) / 31.5f; float a = Mathf.Clamp01((1f - d) * 18f); px[y * n + x] = new Color32(255, 255, 255, (byte)(a * 255)); }
        circulo = new Texture2D(n, n, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp }; circulo.SetPixels32(px); circulo.Apply(); return circulo;
    }
    void RefrescarPuntos() { if (puntosAero == null || Time.unscaledTime > proxPuntos) { var l = Aeropuertos.PuntosMapa(); if (l.Count > 0 || puntosAero == null) puntosAero = l; proxPuntos = Time.unscaledTime + (l.Count > 0 ? 5f : 1f); } }
    // devuelve el aeropuerto bajo el mouse (coordenadas de pantalla) o ""
    string PuntoBajo(Vector2 mouse, Rect r, double izq, double arr)
    {
        if (puntosAero == null) return ""; string mejor = ""; float md = 14f;
        foreach (var p in puntosAero)
        {
            float px = (float)(LonAPx(p.lon, zoomMapa) - izq) + r.x, py = (float)(LatAPx(p.lat, zoomMapa) - arr) + r.y;
            float d = Vector2.Distance(mouse, new Vector2(px, py)); if (d < md) { md = d; mejor = p.nombre; }
        }
        return mejor;
    }
    float zoomCam = 1f, yawCam, pitchCam;
    Camera cam; Vector3 camAdelUlt = Vector3.forward;

    // ---- mapa grande ----
    bool iniciado;
    Rect rectMapa, rectMini;
    int zoomMapa = 12, zoomMini = 12; float zoomManualHasta;
    const bool USAR_MINI_SATELITAL = false;   // camara cenital de Cesium: guardada, apagada (gasta mucha memoria)
    const bool MINI_CALLES = false;   // minimapa chico de la izquierda: sacado (repetido con el mapa grande), codigo guardado
    bool clavado, mapaOculto;
    bool sigueAvion = true; double centroLat, centroLon;
    bool transparente = true;
    enum Arr { Nada, Contenido, MoverMapa, ResizeMapa, MoverMini, ResizeMini }
    Arr arrastrando = Arr.Nada;
    readonly Dictionary<long, Texture2D> baldosas = new Dictionary<long, Texture2D>();
    readonly HashSet<long> pendientes = new HashSet<long>();
    readonly Dictionary<long, float> proximoIntento = new Dictionary<long, float>();
    Texture2D puntoRojo, fondoOscuro, iconoAvion;
    bool ratonSobrePanel;

    // ---- minimapa ----
    RenderTexture rtMini; Camera camMini;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Crear()
    {
        if (FindFirstObjectByType<CesiumGeoreference>() == null) return;
        new GameObject("MapaAuto").AddComponent<MapaAuto>();
    }

    static MapaAuto inst;
    // posicion y rumbo (grados desde el norte) del avion, o del autito si se esta manejando; lo usa el mapeo (marcar lugar, ruta)
    public static bool PosActiva(out double lon, out double lat, out float alt, out float rumbo)
    {
        lon = 0; lat = 0; alt = 0; rumbo = 0;
        if (inst == null) return false;
        var an = inst.AnclaActiva; if (an == null) return false;
        var llh = an.longitudeLatitudeHeight; lon = llh.x; lat = llh.y; alt = (float)llh.z;
        var qe = an.rotationEastUpNorth;
        Vector3 f = new Quaternion(qe.value.x, qe.value.y, qe.value.z, qe.value.w) * Vector3.forward;
        rumbo = Mathf.Atan2(f.x, f.z) * Mathf.Rad2Deg; if (rumbo < 0f) rumbo += 360f;
        return true;
    }

    void Start()
    {
        inst = this;
        autoInvertido = PlayerPrefs.GetInt("auto_invertido", 0) == 1;
        zoomMapa = PlayerPrefs.GetInt("zoom_mapa", 12); zoomMini = PlayerPrefs.GetInt("zoom_mini", 12);
        puntoRojo = new Texture2D(16, 16, TextureFormat.RGBA32, false);
        for (int y = 0; y < 16; y++) for (int x = 0; x < 16; x++)
        {
            float d = Mathf.Sqrt((x - 7.5f) * (x - 7.5f) + (y - 7.5f) * (y - 7.5f));
            Color c = d <= 6f ? new Color(0.9f, 0.15f, 0.15f, 1f) : (d <= 7.6f ? Color.white : new Color(0, 0, 0, 0));
            puntoRojo.SetPixel(x, y, c);
        }
        puntoRojo.Apply();
        // avioncito con la punta hacia arriba (se rota segun el rumbo)
        const int N = 32;
        bool[,] fig = new bool[N, N];
        for (int y = 0; y < N; y++) for (int x = 0; x < N; x++)
        {
            float dx = Mathf.Abs(x - 15.5f);
            bool fus = y >= 2 && y <= 29 && dx <= (y > 24 ? Mathf.Max(0.6f, (29.5f - y) * 0.5f) : 1.8f);
            bool ala = y >= 11 && y <= 19 && dx <= 14f - (y - 11) * 0.9f && dx >= 0f && (y - 11) >= dx * 0.28f - 0.5f && (19 - y) >= -1f;
            bool cola = y >= 2 && y <= 7 && dx <= 6.5f - (y - 2) * 0.7f;
            fig[x, y] = fus || ala || cola;
        }
        iconoAvion = new Texture2D(N, N, TextureFormat.RGBA32, false);
        iconoAvion.filterMode = FilterMode.Bilinear;
        for (int y = 0; y < N; y++) for (int x = 0; x < N; x++)
        {
            Color c = new Color(0, 0, 0, 0);
            if (fig[x, y]) c = new Color(0.9f, 0.1f, 0.1f, 1f);
            else
            {
                bool borde = false;
                for (int oy = -1; oy <= 1 && !borde; oy++) for (int ox = -1; ox <= 1; ox++)
                { int nx = x + ox, ny = y + oy; if (nx >= 0 && ny >= 0 && nx < N && ny < N && fig[nx, ny]) { borde = true; break; } }
                if (borde) c = Color.white;
            }
            iconoAvion.SetPixel(x, y, c);
        }
        iconoAvion.Apply();
        fondoOscuro = new Texture2D(1, 1); fondoOscuro.SetPixel(0, 0, new Color(0.1f, 0.12f, 0.15f, 1f)); fondoOscuro.Apply();
    }

    // ====================== utilidades ======================
    Transform Activo { get { if (AvionDeCarton.enAuto && autoGO != null) return autoGO.transform; return avion != null ? avion.transform : null; } }
    CesiumGlobeAnchor AnclaActiva { get { if (AvionDeCarton.enAuto && anclaAuto != null) return anclaAuto; return anclaAvion; } }

    static double LonAPx(double lon, int z) { return (lon + 180.0) / 360.0 * Mathf.Pow(2f, z) * 256.0; }
    static double LatAPx(double lat, int z)
    {
        double r = lat * System.Math.PI / 180.0;
        return (1.0 - System.Math.Log(System.Math.Tan(r) + 1.0 / System.Math.Cos(r)) / System.Math.PI) / 2.0 * Mathf.Pow(2f, z) * 256.0;
    }
    static double PxALon(double px, int z) { return px / (Mathf.Pow(2f, z) * 256.0) * 360.0 - 180.0; }
    static double PxALat(double py, int z) { return System.Math.Atan(System.Math.Sinh(System.Math.PI * (1.0 - 2.0 * py / (Mathf.Pow(2f, z) * 256.0)))) * 180.0 / System.Math.PI; }
    // zoom segun altura (principal.gd _zoom_mapa_para_altura)
    static int ZoomParaAltura(float h)
    {
        if (h < 1000f) return 17;
        int extra = (int)Mathf.Floor((h - 1000f) / 500f);
        return Mathf.Clamp(17 - 4 - extra, ZOOM_MIN, 17);
    }
    static long Clave(int z, int x, int y) { return ((long)z << 48) | ((long)x << 24) | (long)y; }

    // ====================== Update ======================
    void Update()
    {
        if (avion == null) { avion = FindFirstObjectByType<AvionDeCarton>(); anclaAvion = avion != null ? avion.GetComponent<CesiumGlobeAnchor>() : null; }
        if (AvionDeCarton.enAuto && (avion == null || autoGO == null)) { LimpiarAuto(); AvionDeCarton.enAuto = false; }
        if (USAR_MINI_SATELITAL && rtMini == null && Camera.main != null) CrearMini();
        if (AvionDeCarton.enAuto) ManejarAuto();
        AvionDeCarton.velAutoTxt = Mathf.RoundToInt(velAuto * 3.6f) + " km/h";
    }

    void LateUpdate()
    {
        if (AvionDeCarton.enAuto && autoGO != null) CamaraAuto();
        if (camMini != null)
        {
            var t = Activo;
            if (t != null)
            {
                camMini.transform.position = t.position + Vector3.up * ALTURA_MINI;
                camMini.transform.rotation = Quaternion.LookRotation(Vector3.down, Vector3.forward);   // mira derecho abajo, norte arriba
            }
            camMini.enabled = (Time.frameCount % 3 == 0);   // cada 3 cuadros: cuida la placa integrada
        }
    }

    // ====================== minimapa ======================
    void CrearMini()
    {
        var geo = FindFirstObjectByType<CesiumGeoreference>();
        rtMini = new RenderTexture(512, 512, 16);
        var go = new GameObject("CamaraMinimapa");
        camMini = go.AddComponent<Camera>();
        camMini.targetTexture = rtMini;
        camMini.fieldOfView = FOV_MINI;           // perspectiva angosta, NO ortografica (Cesium necesita perspectiva para el nivel de detalle)
        camMini.nearClipPlane = 10f; camMini.farClipPlane = 60000f;
        camMini.clearFlags = CameraClearFlags.SolidColor; camMini.backgroundColor = Color.black;
        camMini.cullingMask = ~(1 << DiaNoche.CAPA_MARCADORES);
        camMini.depth = -10f;
        var d = camMini.GetUniversalAdditionalCameraData();
        d.renderPostProcessing = true;
        if (geo != null) CesiumCameraManager.GetOrCreate(geo.gameObject).additionalCameras.Add(camMini);
    }

    // ====================== auto ======================
    void AvisarTexto(string s, float seg) { textoAviso = s; avisoHasta = Time.time + seg; }

    async void BajarAuto(int idx)
    {
        if (cargandoAuto || avion == null || AvionDeCarton.enAuto) return;
        var spec = AUTOS[idx];
        string ruta = RutaAuto(spec);
        if (!System.IO.File.Exists(ruta)) { AvisarTexto("No encuentro " + spec.archivo, 3f); return; }
        cargandoAuto = true; AvisarTexto("Cargando " + spec.nombre + "...", 30f);
        try
        {
            if (importados[idx] == null)
            {
                var g = new GLTFast.GltfImport();
                bool ok = await g.Load(new System.Uri(ruta).AbsoluteUri);
                if (!ok) { AvisarTexto("No pude cargar " + spec.archivo, 3f); cargandoAuto = false; return; }
                importados[idx] = g;
            }
            if (avion == null) { cargandoAuto = false; return; }

            // ancho del avion (para dejar el auto AL COSTADO, no encima)
            Transform ta = avion.transform;
            Vector3 derecha = ta.right; derecha.y = 0f; derecha = derecha.sqrMagnitude > 0.001f ? derecha.normalized : Vector3.right;
            Vector3 adelante = ta.forward; adelante.y = 0f; adelante = adelante.sqrMagnitude > 0.001f ? adelante.normalized : Vector3.forward;
            float mitadAncho = 2f;
            var rsAvion = avion.GetComponentsInChildren<Renderer>();
            if (rsAvion.Length > 0)
            {
                mitadAncho = 0f;
                foreach (var r in rsAvion)
                {
                    var b = r.bounds;
                    for (int i = 0; i < 8; i++)
                    {
                        var c = b.center + Vector3.Scale(b.extents, new Vector3((i & 1) == 0 ? -1f : 1f, (i & 2) == 0 ? -1f : 1f, (i & 4) == 0 ? -1f : 1f));
                        mitadAncho = Mathf.Max(mitadAncho, Mathf.Abs(Vector3.Dot(c - ta.position, derecha)));
                    }
                }
            }
            float lateral = mitadAncho + spec.largo * 0.25f + 4f;
            Vector3 pos = ta.position + derecha * lateral;
            float yPiso = ta.position.y - avion.PieM;
            RaycastHit h;
            if (Physics.Raycast(pos + Vector3.up * 2000f, Vector3.down, out h, 4000f, ~0, QueryTriggerInteraction.Ignore)) yPiso = h.point.y;
            pos.y = yPiso + ALTURA_A_PIE;

            var geo = FindFirstObjectByType<CesiumGeoreference>();
            autoGO = new GameObject("Auto");
            autoGO.transform.SetParent(geo.transform, false);
            autoGO.transform.SetPositionAndRotation(pos, Quaternion.identity);
            var modelo = new GameObject("modelo");
            modelo.transform.SetParent(autoGO.transform, false);
            await importados[idx].InstantiateMainSceneAsync(modelo.transform);
            if (autoGO == null) { cargandoAuto = false; return; }

            // auto-escala por la dimension mas larga y centrado (como _crear_modelo_auto)
            var rs = modelo.GetComponentsInChildren<Renderer>();
            centroNativo = Vector3.zero; escalaAuto = 1f;
            if (rs.Length > 0)
            {
                Bounds bb = rs[0].bounds; foreach (var r in rs) bb.Encapsulate(r.bounds);
                float mas = Mathf.Max(bb.size.x, Mathf.Max(bb.size.y, bb.size.z));
                escalaAuto = mas > 0.001f ? spec.largo / mas : 1f;
                centroNativo = autoGO.transform.InverseTransformPoint(bb.center);
            }
            modeloAuto = modelo.transform;
            AplicarOrientacionModelo(spec);
            autoGO.transform.rotation = Quaternion.LookRotation(adelante, Vector3.up);

            anclaAuto = autoGO.AddComponent<CesiumGlobeAnchor>();
            autoGO.AddComponent<CesiumOriginShift>();
            var osAvion = avion.GetComponent<CesiumOriginShift>(); if (osAvion != null) osAvion.enabled = false;

            // sonido del motor del auto
            if (clipAuto == null) clipAuto = await CargarClip(System.IO.Path.Combine(Godot, "FX", "motor_auto_loop.mp3"));
            if (autoGO == null) { cargandoAuto = false; return; }
            fuenteAuto = autoGO.AddComponent<AudioSource>();
            fuenteAuto.clip = clipAuto; fuenteAuto.loop = true; fuenteAuto.spatialBlend = 0f;
            fuenteAuto.volume = Mathf.Pow(10f, Mathf.Lerp(-40f, -6f, 0.5f) / 20f); fuenteAuto.pitch = 0.85f;
            if (clipAuto != null) fuenteAuto.Play();
            // motor BMW por capas (ralenti / medio / alto / aceleracion): si estan los clips, reemplazan al motor viejo
            var nomCapas = new[] { "ralenti", "medio", "alto", "aceleracion" }; capasAuto = new AudioSource[4]; bool hay = true;
            for (int ci = 0; ci < 4; ci++)
            {
                var cl = Resources.Load<AudioClip>("auto/" + nomCapas[ci]); if (cl == null) { hay = false; break; }
                var fa = autoGO.AddComponent<AudioSource>(); fa.clip = cl; fa.loop = true; fa.spatialBlend = 0f; fa.volume = 0f; fa.playOnAwake = false; fa.Play(); capasAuto[ci] = fa;
            }
            if (hay) { fuenteAuto.Stop(); fuenteAuto.volume = 0f; } else { foreach (var fa in capasAuto) if (fa != null) Destroy(fa); capasAuto = null; }

            // el avion queda congelado y oculto donde estaba
            var src = avion.GetComponent<AudioSource>(); if (src != null) src.Pause();
            foreach (var r in rsAvion) r.enabled = false;
            AvionDeCarton.enAuto = true;
            velAuto = 0f; sueloInit = false; yawCam = 0f; pitchCam = 0f; indiceAuto = idx;
            AvisarTexto("Manejando: " + spec.nombre, 2f);
        }
        catch (System.Exception e) { Debug.LogWarning("Error bajando al auto: " + e.Message); LimpiarAuto(); }
        cargandoAuto = false;
    }
    int indiceAuto;
    const float PENDIENTE_MAX = 0.35f;   // subida/bajada maxima del auto (35 %), mas que eso se considera "bulto"
    float sueloEst, tiempoDesfase; bool sueloInit;

    void AplicarOrientacionModelo(TipoAuto spec)
    {
        if (modeloAuto == null) return;
        var rot = Quaternion.Euler(0f, spec.rotExtra + (autoInvertido ? 180f : 0f), 0f);
        modeloAuto.localScale = Vector3.one * escalaAuto;
        modeloAuto.localRotation = rot;
        modeloAuto.localPosition = -(rot * (centroNativo * escalaAuto));
    }

    static async Awaitable<AudioClip> CargarClip(string ruta)
    {
        try
        {
            if (!System.IO.File.Exists(ruta)) return null;
            using (var req = UnityWebRequestMultimedia.GetAudioClip(new System.Uri(ruta).AbsoluteUri, AudioType.MPEG))
            {
                var op = req.SendWebRequest();
                while (!op.isDone) await Awaitable.NextFrameAsync();
                if (req.result != UnityWebRequest.Result.Success) return null;
                return DownloadHandlerAudioClip.GetContent(req);
            }
        }
        catch { return null; }
    }

    void LimpiarAuto()
    {
        if (autoGO != null) Destroy(autoGO);
        autoGO = null; anclaAuto = null; modeloAuto = null; fuenteAuto = null; capasAuto = null;
    }

    void SubirAlAvion()
    {
        if (!AvionDeCarton.enAuto) return;
        LimpiarAuto();
        AvionDeCarton.enAuto = false; velAuto = 0f;
        if (avion != null)
        {
            foreach (var r in avion.GetComponentsInChildren<Renderer>(true)) r.enabled = true;
            var os = avion.GetComponent<CesiumOriginShift>(); if (os != null) os.enabled = true;
            var src = avion.GetComponent<AudioSource>(); if (src != null) src.UnPause();
            avion.Reanudar();   // velocidad 0, en tierra
        }
        AvisarTexto("De vuelta en el avion", 1.5f);
    }

    // _procesar_modo_a_pie
    void ManejarAuto()
    {
        if (autoGO == null) return;
        float dt = Time.deltaTime; if (dt <= 0f) return;
        var gp = Gamepad.current; var kb = Keyboard.current;
        Vector3 arriba = Vector3.up;
        Transform t = autoGO.transform;

        float giro = 0f;
        if (Controles.escribiendo) { gp = null; kb = null; }   // escribiendo un nombre: el autito no reacciona al teclado
        if (gp != null) { float sx = gp.leftStick.x.ReadValue(); if (Mathf.Abs(sx) > 0.2f) giro += sx; }
        if (Controles.Activa("alabeo_derecha")) giro += 1f;
        if (Controles.Activa("alabeo_izquierda")) giro -= 1f;
        giro = Mathf.Clamp(giro, -1f, 1f);
        if (giro != 0f) t.Rotate(arriba, giro * GIRO * dt, Space.World);

        bool acel = Controles.Activa("acelerar");
        bool fren = Controles.Activa("frenar");
        bool reversa = Controles.Activa("reversa_pushback") || (kb != null && (kb.fKey.isPressed || kb.lKey.isPressed)) || (gp != null && gp.rightTrigger.ReadValue() > 0.2f);
        if (acel) velAuto = Mathf.Min(V_MAX, velAuto + ACEL * dt);
        else if (fren) velAuto = Mathf.Max(0f, velAuto - FRENO * dt);

        Vector3 f = t.forward; f -= Vector3.Dot(f, arriba) * arriba;
        if (f.sqrMagnitude > 0.001f) f.Normalize();
        if (reversa) { velAuto = 0f; t.position += -f * V_REVERSA * dt; }
        else if (velAuto > 0.001f) t.position += f * velAuto * dt;

        // pegado al piso: rayo vertical, solo la altura se suaviza (lerp delta*10)
        RaycastHit h;
        if (Physics.Raycast(t.position + arriba * 2000f, -arriba, out h, 4000f, ~0, QueryTriggerInteraction.Ignore))
        {
            // filtro anti-bultos: el piso estimado solo puede subir/bajar segun una pendiente maxima (autos, plantas, puentes de la foto 3D no te levantan)
            if (!sueloInit) { sueloEst = h.point.y; sueloInit = true; }
            float pasoMax = (velAuto + (reversa ? V_REVERSA : 0f)) * dt * PENDIENTE_MAX + 0.02f;
            float dif = h.point.y - sueloEst;
            if (Mathf.Abs(dif) > 8f) { tiempoDesfase += dt; if (tiempoDesfase > 2f) { sueloEst = h.point.y; tiempoDesfase = 0f; } } else tiempoDesfase = 0f;
            sueloEst += Mathf.Clamp(dif, -pasoMax, pasoMax);
            float hAct = t.position.y, hObj = sueloEst + ALTURA_A_PIE;
            float nueva = Mathf.Lerp(hAct, hObj, Mathf.Clamp01(dt * 10f));
            t.position += arriba * (nueva - hAct);
        }
        if (fuenteAuto != null && capasAuto == null) fuenteAuto.volume = Mathf.Pow(10f, Mathf.Lerp(-40f, -6f, 0.5f) / 20f) * Mezcla.De(Mezcla.MOTOR_AUTO);
        if (capasAuto != null)
        {
            float fv = Mathf.Clamp01(velAuto / V_MAX), mz = Mezcla.De(Mezcla.MOTOR_AUTO) * 0.8f; bool ac = Controles.Activa("acelerar") && velAuto < V_MAX * 0.97f;
            float vRal = Mathf.Clamp01(1f - fv / 0.30f), vMed = Mathf.Clamp01(1f - Mathf.Abs(fv - 0.45f) / 0.30f), vAlt = Mathf.Clamp01((fv - 0.45f) / 0.35f), vAce = ac ? Mathf.Clamp01(1f - fv / 0.85f) * 0.85f : 0f;
            float[] vv = { vRal, vMed, vAlt, vAce };
            for (int ci = 0; ci < 4; ci++) if (capasAuto[ci] != null) { float meta = vv[ci] * mz; capasAuto[ci].volume = Mathf.MoveTowards(capasAuto[ci].volume, meta, dt * 3f); capasAuto[ci].pitch = ci == 3 ? 1f : Mathf.Lerp(0.95f, 1.1f, fv); }
        }
        else if (fuenteAuto != null) fuenteAuto.pitch = Mathf.Lerp(0.85f, 1.25f, Mathf.Clamp01(velAuto / V_MAX));
    }

    void CamaraAuto()
    {
        if (cam == null) cam = Camera.main;
        if (cam == null) return;
        Vector3 arriba = Vector3.up;
        Vector3 adel = autoGO.transform.forward; adel -= Vector3.Dot(adel, arriba) * arriba;
        if (adel.sqrMagnitude > 0.0001f) { adel.Normalize(); camAdelUlt = adel; } else adel = camAdelUlt;
        var raton = Mouse.current;
        if (raton != null && !ratonSobrePanel)
        {
            float rueda = raton.scroll.ReadValue().y;
            if (rueda != 0f) zoomCam = Mathf.Clamp(zoomCam * Mathf.Exp(-Mathf.Sign(rueda) * 0.18f), 0.1f, 5f);
        }
        var kb = Keyboard.current;
        if (kb != null && !Controles.escribiendo)
        {
            float z = 0f;
            if (kb.zKey.isPressed || kb.pageUpKey.isPressed) z -= 1f;
            if (kb.xKey.isPressed || kb.pageDownKey.isPressed) z += 1f;
            if (z != 0f) zoomCam = Mathf.Clamp(zoomCam * Mathf.Exp(z * 1.2f * Time.deltaTime), 0.1f, 5f);
        }
        if (Controles.Flanco("centrar_camara_orbital")) { yawCam = 0f; pitchCam = 0f; }
        float oyc = (Controles.Activa("girar_vista_derecha") ? 1f : 0f) - (Controles.Activa("girar_vista_izquierda") ? 1f : 0f);
        if (oyc != 0f) yawCam += oyc * 90f * Time.deltaTime;
        var gp = Gamepad.current;
        if (gp != null && !Controles.escribiendo)
        {
            Vector2 sd = gp.rightStick.ReadValue();
            if (sd.magnitude > 0.2f) { yawCam += sd.x * 90f * Time.deltaTime; pitchCam = Mathf.Clamp(pitchCam + sd.y * 60f * Time.deltaTime, -80f, 80f); }
        }
        Vector3 off = (-adel * 22f + arriba * 5f) * zoomCam;
        off = Quaternion.AngleAxis(yawCam, arriba) * off;
        Vector3 lat = Vector3.Cross(arriba, off);
        if (lat.sqrMagnitude > 0.0001f) off = Quaternion.AngleAxis(pitchCam, lat.normalized) * off;
        cam.transform.position = autoGO.transform.position + off;
        Quaternion destino = Quaternion.LookRotation(autoGO.transform.position - cam.transform.position, arriba);
        cam.transform.rotation = Quaternion.Slerp(cam.transform.rotation, destino, 1f - Mathf.Exp(-12f * Time.deltaTime));
    }

    // ====================== baldosas del mapa ======================
    Texture2D Baldosa(int z, int x, int y)
    {
        long k = Clave(z, x, y);
        Texture2D t;
        if (baldosas.TryGetValue(k, out t)) return t;
        if (!pendientes.Contains(k) && pendientes.Count < 4)
        {
            float prox; if (!proximoIntento.TryGetValue(k, out prox) || Time.time >= prox) Descargar(k, z, x, y);
        }
        return null;
    }

    async void Descargar(long k, int z, int x, int y)
    {
        pendientes.Add(k);
        try
        {
            using (var req = UnityWebRequestTexture.GetTexture("https://tile.openstreetmap.org/" + z + "/" + x + "/" + y + ".png"))
            {
                req.SetRequestHeader("User-Agent", "SimuladorDeVueloUnity/1.0");
                var op = req.SendWebRequest();
                while (!op.isDone) await Awaitable.NextFrameAsync();
                if (req.result == UnityWebRequest.Result.Success)
                {
                    var tex = DownloadHandlerTexture.GetContent(req);
                    tex.wrapMode = TextureWrapMode.Clamp;
                    if (baldosas.Count > 150) LimpiarBaldosas();
                    baldosas[k] = tex;
                }
                else { proximoIntento[k] = Time.time + 6f; Debug.LogWarning("[Mapa] baldosa fallo: " + req.error + " (" + req.responseCode + ")"); }
            }
        }
        catch (System.Exception ex) { proximoIntento[k] = Time.time + 6f; Debug.LogWarning("[Mapa] baldosa excepcion: " + ex.Message); }
        pendientes.Remove(k);
    }

    void LimpiarBaldosas()
    {
        foreach (var kv in baldosas) if (kv.Value != null) Destroy(kv.Value);
        baldosas.Clear();
    }

    // ====================== interfaz ======================
    void OnGUI()
    {
        float W = Screen.width, H = Screen.height;
        if (!iniciado)
        {
            rectMapa = new Rect(W - 264f, 151f, 264f, 255f);   // offsets de Godot
            rectMini = new Rect(20f, 110f, 220f, 220f);
            if (PlayerPrefs.HasKey("mapa_x")) rectMapa = new Rect(Mathf.Clamp(PlayerPrefs.GetFloat("mapa_x"), 0f, Mathf.Max(0f, W - 100f)), Mathf.Clamp(PlayerPrefs.GetFloat("mapa_y"), 0f, Mathf.Max(0f, H - 100f)), Mathf.Clamp(PlayerPrefs.GetFloat("mapa_w"), MAPA_MIN, MAPA_MAX), Mathf.Clamp(PlayerPrefs.GetFloat("mapa_h"), MAPA_MIN, MAPA_MAX));
            clavado = PlayerPrefs.GetInt("mapa_clavado", 0) == 1;
            mapaOculto = PlayerPrefs.GetInt("mapa_oculto", 0) == 1;
            iniciado = true;
        }
        var e = Event.current;
        ratonSobrePanel = rectMapa.Contains(e.mousePosition) || (MINI_CALLES && rectMini.Contains(e.mousePosition));

        // boton Bajar / Subir (arriba a la izquierda, antes de los instrumentos)
        bool enA = AvionDeCarton.enAuto;
        bool puedeBajar = avion != null && avion.EnTierra && !cargandoAuto;
        GUI.enabled = enA || puedeBajar;
        string txtBoton = enA ? "Subir al avion" : (puedeBajar || avion == null ? "Bajar del avion" : "Bajar (aterriza antes)");
        if (GUI.Button(new Rect(20f, 4f, 170f, 26f), txtBoton)) { if (enA) SubirAlAvion(); else selectorAbierto = !selectorAbierto; }
        GUI.enabled = true;
        if (enA && GUI.Button(new Rect(196f, 4f, 120f, 26f), "Dar vuelta auto"))
        {
            autoInvertido = !autoInvertido; PlayerPrefs.SetInt("auto_invertido", autoInvertido ? 1 : 0); PlayerPrefs.Save();
            AplicarOrientacionModelo(AUTOS[Mathf.Clamp(indiceAuto, 0, AUTOS.Length - 1)]);
        }
        if (Time.time < avisoHasta)
        {
            var sa = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontSize = 22, fontStyle = FontStyle.Bold };
            GUI.Label(new Rect(W * 0.5f - 350f, H * 0.5f - 20f, 700f, 40f), textoAviso, sa);
        }

        if (selectorAbierto && !enA)
        {
            var r = new Rect(W * 0.5f - 160f, 120f, 320f, 40f + AUTOS.Length * 44f + 40f);
            GUI.Box(r, "ELEGI TU AUTO");
            for (int i = 0; i < AUTOS.Length; i++)
                if (GUI.Button(new Rect(r.x + 14f, r.y + 32f + i * 44f, r.width - 28f, 38f), AUTOS[i].nombre)) { selectorAbierto = false; BajarAuto(i); }
            if (GUI.Button(new Rect(r.x + 14f, r.y + r.height - 34f, r.width - 28f, 26f), "Cancelar")) selectorAbierto = false;
        }

        DibujarMapa(e);
        if (MINI_CALLES) DibujarMini(e);

        // arrastre global (mover contenido / mover / agrandar)
        if (e.type == EventType.MouseDrag && arrastrando != Arr.Nada) ProcesarArrastre(e, W, H);
        if (e.type == EventType.MouseUp || (e.type == EventType.Repaint && arrastrando != Arr.Nada && (Mouse.current == null || !Mouse.current.leftButton.isPressed)))
        { if (arrastrando == Arr.MoverMapa || arrastrando == Arr.ResizeMapa) GuardarMapa(); arrastrando = Arr.Nada; }   // chequeo activo: nunca queda trabado
    }

    void GuardarMapa()
    {
        PlayerPrefs.SetFloat("mapa_x", rectMapa.x); PlayerPrefs.SetFloat("mapa_y", rectMapa.y); PlayerPrefs.SetFloat("mapa_w", rectMapa.width); PlayerPrefs.SetFloat("mapa_h", rectMapa.height); PlayerPrefs.Save();
    }

    void ProcesarArrastre(Event e, float W, float H)
    {
        Vector2 d = e.delta;
        switch (arrastrando)
        {
            case Arr.Contenido:
            {
                double cx = LonAPx(centroLon, zoomMapa) - d.x, cy = LatAPx(centroLat, zoomMapa) - d.y;
                centroLon = PxALon(cx, zoomMapa); centroLat = PxALat(cy, zoomMapa);
                break;
            }
            case Arr.MoverMapa: rectMapa.x += d.x; rectMapa.y += d.y; break;
            case Arr.ResizeMapa:
                rectMapa.width = Mathf.Clamp(rectMapa.width + d.x, MAPA_MIN, MAPA_MAX);
                rectMapa.height = Mathf.Clamp(rectMapa.height + d.y, MAPA_MIN, MAPA_MAX); break;
            case Arr.MoverMini: rectMini.x += d.x; rectMini.y += d.y; break;
            case Arr.ResizeMini:
                rectMini.width = Mathf.Clamp(rectMini.width + d.x, MINI_MIN, MINI_MAX);
                rectMini.height = rectMini.width; break;
        }
        e.Use();
    }

    void DibujarMapa(Event e)
    {
        var r = rectMapa;
        if (mapaOculto)
        {
            // el mapa se contrae a una tirita con el titulo y el boton Mostrar
            var tira = new Rect(r.x, r.y, Mathf.Min(r.width, 150f), 22f);
            GUI.Box(tira, "");
            GUI.Label(new Rect(tira.x + 6f, tira.y + 2f, 60f, 18f), "Mapa");
            if (GUI.Button(new Rect(tira.xMax - 76f, tira.y + 2f, 72f, 18f), "Mostrar")) { mapaOculto = false; PlayerPrefs.SetInt("mapa_oculto", 0); PlayerPrefs.Save(); }
            return;
        }
        var ancla = AnclaActiva;
        double lat = 0, lon = 0; float alt = 0f;
        if (ancla != null) { var llh = ancla.longitudeLatitudeHeight; lon = llh.x; lat = llh.y; alt = (float)llh.z; }
        if (sigueAvion) { centroLat = lat; centroLon = lon; }

        // zoom automatico por altura (la ruedita lo fuerza 1 minuto)
        // el zoom que dejes queda fijo (guardado), no se corrige solo

        GUI.DrawTexture(r, fondoOscuro);
        float alfa = transparente ? 0.3f : 1f;
        double cxPx = LonAPx(centroLon, zoomMapa), cyPx = LatAPx(centroLat, zoomMapa);
        double izq = cxPx - r.width * 0.5, arr = cyPx - r.height * 0.5;
        int n = 1 << zoomMapa;
        GUI.BeginGroup(r);
        if (e.type == EventType.Repaint)
        {
            int tx0 = (int)System.Math.Floor(izq / 256.0), tx1 = (int)System.Math.Floor((izq + r.width) / 256.0);
            int ty0 = (int)System.Math.Floor(arr / 256.0), ty1 = (int)System.Math.Floor((arr + r.height) / 256.0);
            var col = GUI.color; GUI.color = new Color(1, 1, 1, alfa);
            for (int ty = ty0; ty <= ty1; ty++)
            {
                if (ty < 0 || ty >= n) continue;
                for (int tx = tx0; tx <= tx1; tx++)
                {
                    int txw = ((tx % n) + n) % n;
                    var tex = Baldosa(zoomMapa, txw, ty);
                    if (tex == null) continue;
                    GUI.DrawTexture(new Rect((float)(tx * 256.0 - izq), (float)(ty * 256.0 - arr), 256f, 256f), tex);
                }
            }
            GUI.color = col;
            // punto rojo del avion / auto
            float px = (float)(LonAPx(lon, zoomMapa) - izq), py = (float)(LatAPx(lat, zoomMapa) - arr);
            if (px > -20f && py > -20f && px < r.width + 20f && py < r.height + 20f)
            {
                // rumbo (grados desde el norte, en sentido horario) segun la orientacion este-arriba-norte del ancla
                float rumbo = 0f;
                if (ancla != null) { var qe = ancla.rotationEastUpNorth; Vector3 f = new Quaternion(qe.value.x, qe.value.y, qe.value.z, qe.value.w) * Vector3.forward; rumbo = Mathf.Atan2(f.x, f.z) * Mathf.Rad2Deg; }
                var mat = GUI.matrix;
                GUIUtility.RotateAroundPivot(rumbo, new Vector2(px, py));
                GUI.DrawTexture(new Rect(px - 15f, py - 15f, 30f, 30f), iconoAvion);
                GUI.matrix = mat;
            }
            // aeropuertos (puntos amarillos tocables)
            RefrescarPuntos();
            if (planVista && planLinea && planOrigen != "" && planDestino != "") DibujarLineaPlan(r, izq, arr);
            if (planVista && puntosAero != null)
            {
                if (sAero == null) { sAero = new GUIStyle(GUI.skin.label) { fontSize = 10, fontStyle = FontStyle.Bold, normal = { textColor = Color.white } }; sAeroSel = new GUIStyle(sAero) { normal = { textColor = new Color(1f, 0.9f, 0.2f) } }; }
                var colAnt = GUI.color; var cir = Circulo();
                foreach (var pa in puntosAero)
                {
                    float ax = (float)(LonAPx(pa.lon, zoomMapa) - izq), ay = (float)(LatAPx(pa.lat, zoomMapa) - arr);
                    if (ax < -10f || ay < -10f || ax > r.width + 10f || ay > r.height + 10f) continue;
                    bool sel = pa.nombre == aeroSel; float rad = sel ? 8f : 5.5f;
                    GUI.color = Color.black; GUI.DrawTexture(new Rect(ax - rad - 1.5f, ay - rad - 1.5f, 2f * rad + 3f, 2f * rad + 3f), cir);
                    GUI.color = sel ? Color.white : (pa.tienePista ? new Color(1f, 0.85f, 0.1f) : new Color(0.7f, 0.7f, 0.7f)); GUI.DrawTexture(new Rect(ax - rad, ay - rad, 2f * rad, 2f * rad), cir);
                    if (pa.nombre == planOrigen || pa.nombre == planDestino) { GUI.color = new Color(1f, 0.2f, 0.9f); GUI.DrawTexture(new Rect(ax - 11f, ay - 11f, 22f, 22f), cir); GUI.color = Color.black; GUI.DrawTexture(new Rect(ax - 9f, ay - 9f, 18f, 18f), cir); GUI.color = Color.white; GUI.Label(new Rect(ax - 4f, ay - 8f, 14f, 16f), pa.nombre == planOrigen ? "1" : "2", sAeroSel); }
                    if (zoomMapa >= 10 || sel) { GUI.color = Color.white; GUI.Label(new Rect(ax + 8f, ay - 8f, 160f, 16f), pa.nombre, sel ? sAeroSel : sAero); }
                }
                GUI.color = colAnt;
            }
        }
        GUI.EndGroup();

        // botones T (transparencia) y Centrar, en las esquinas de abajo; el tirador va en la esquina derecha
        if (GUI.Button(new Rect(r.x + 4f, r.yMax - 26f, 26f, 22f), "T")) transparente = !transparente;
        if (GUI.Button(new Rect(r.x + 34f, r.yMax - 26f, 70f, 22f), "Centrar"))
        {
            sigueAvion = true; if (arrastrando == Arr.Contenido) arrastrando = Arr.Nada;
        }
        if (GUI.Button(new Rect(r.x + 108f, r.yMax - 26f, 64f, 22f), clavado ? "Clavado" : "Clavar")) { clavado = !clavado; PlayerPrefs.SetInt("mapa_clavado", clavado ? 1 : 0); GuardarMapa(); }
        if (r.width >= 250f && GUI.Button(new Rect(r.x + 176f, r.yMax - 26f, 66f, 22f), "Ocultar")) { mapaOculto = true; PlayerPrefs.SetInt("mapa_oculto", 1); PlayerPrefs.Save(); return; }
        GUI.Box(new Rect(r.xMax - 16f, r.yMax - 16f, 16f, 16f), "");   // tirador de tamaño
        GUI.Label(new Rect(r.x + 4f, r.y, r.width - 8f, 16f), "Mapa - z" + zoomMapa);

        // popup de 3 bolitas del aeropuerto elegido (se dibuja arriba del mapa, antes de procesar los clics del mapa)
        // boton arriba a la derecha: alterna entre el mapa normal y la vista PLAN DE VUELO (puntos + linea)
        var rBtnVista = new Rect(r.xMax - 108f, r.y + 1f, 106f, 15f);
        Instrucciones.Marcar("plan", rBtnVista);
        if (GUI.Button(rBtnVista, planVista ? "MAPA NORMAL" : "PLAN DE VUELO")) { planVista = !planVista; if (!planVista) { aeroSel = ""; if (planPaso > 0) PlanCancelar(); } }
        var rBtnRuta = new Rect(r.xMax - 108f, r.y + 17f, 106f, 15f);
        if (planVista)
        {
            bool hayRuta = planPaso > 0 || planLinea;
            if (GUI.Button(rBtnRuta, planPaso > 0 ? "CANCELAR RUTA" : (planLinea ? "BORRAR RUTA" : "ARMAR RUTA 1 > 2"))) { if (hayRuta) PlanCancelar(); else { planPaso = 1; aeroSel = ""; textoAviso = "Ruta: toca el ORIGEN (1)"; avisoHasta = Time.time + 3f; } }
        }
        if (planViaje && Carrera.HayViaje) planViajeVisto = true;
        if (planViaje && planViajeVisto && !Carrera.HayViaje) PlanCancelar();   // llegaste o cancelaste el viaje: se borra la linea
        if (planVista && planPaso == 3) DibujarPlanListo(e, r);
        else if (planVista && aeroSel != "") DibujarOpcionesAero(e, r);
        if (e.type == EventType.MouseDown && e.button == 0 && r.Contains(e.mousePosition) && !((aeroSel != "" || planPaso == 3) && planVista && rectOpcionesAero.Contains(e.mousePosition)) && !rBtnVista.Contains(e.mousePosition) && !(planVista && rBtnRuta.Contains(e.mousePosition)))
        {
            string pb = planVista && planPaso != 3 ? PuntoBajo(e.mousePosition, r, izq, arr) : "";
            if (pb != "") { aeroSel = pb; e.Use(); return; }
            if (clavado) { if (e.clickCount < 2) { if (sigueAvion) { centroLat = lat; centroLon = lon; sigueAvion = false; } arrastrando = Arr.Contenido; } }
            else if (e.clickCount >= 2) { rectMapa = new Rect(Screen.width - 264f, 151f, 264f, 255f); }   // boton de panico
            else if (new Rect(r.xMax - 18f, r.yMax - 18f, 18f, 18f).Contains(e.mousePosition)) arrastrando = Arr.ResizeMapa;
            else if (e.mousePosition.y < r.y + 16f) arrastrando = Arr.MoverMapa;
            else
            {
                if (sigueAvion) { centroLat = lat; centroLon = lon; sigueAvion = false; }
                arrastrando = Arr.Contenido;
            }
            e.Use();
        }
        if (e.type == EventType.ScrollWheel && r.Contains(e.mousePosition))
        {
            zoomMapa = Mathf.Clamp(zoomMapa + (e.delta.y < 0f ? 1 : -1), ZOOM_MIN, ZOOM_MAX);
            PlayerPrefs.SetInt("zoom_mapa", zoomMapa); PlayerPrefs.Save();
            e.Use();
        }
    }

    Rect rectOpcionesAero;
    // ---- vista PLAN DE VUELO: solo ahi se ven los puntos de aeropuertos y la linea; origen (despegar / en el aire) -> destino (aterrizar / en el aire) ----
    bool planVista;
    int planPaso;   // 0 nada, 1 elegir origen, 2 elegir destino, 3 listo para confirmar
    string planOrigen = "", planDestino = ""; int planModoO, planModoD; bool planLinea, planViaje, planViajeVisto;
    void PlanCancelar() { planPaso = 0; planOrigen = planDestino = ""; planLinea = false; planViaje = false; planViajeVisto = false; aeroSel = ""; }
    Aeropuertos.PuntoMapa PuntoPorNombre(string n) { if (puntosAero != null) foreach (var p in puntosAero) if (p.nombre == n) return p; return null; }
    void PlanConfirmar()
    {
        string msg; var d = PuntoPorNombre(planDestino);
        if (!Aeropuertos.Colocar(planOrigen, planModoO, out msg)) { textoAviso = msg; avisoHasta = Time.time + 4f; return; }
        planViaje = false; planViajeVisto = false;
        if (planModoD == 0 && d != null) { Carrera.IniciarViaje(d.nombre, d.lat, d.lon, 600f, Aeropuertos.PrecioDe(d.nombre)); planViaje = true; textoAviso = "Plan: " + planOrigen + " > " + planDestino + ". Para cobrar, aterriza"; }
        else textoAviso = "Paseo: " + planOrigen + " > " + planDestino + " (sin cobro)";
        avisoHasta = Time.time + 5f; planLinea = true; planPaso = 0; aeroSel = ""; sigueAvion = true;
    }
    void DibujarOpcionesAero(Event e, Rect r)
    {
        float W = Screen.width; float ancho = 252f, alto = 118f;
        float x = r.x - ancho - 6f; if (x < 4f) x = Mathf.Min(r.x + 6f, W - ancho - 4f);
        rectOpcionesAero = new Rect(x, r.y, ancho, alto);
        GUI.Box(rectOpcionesAero, "");
        var tit = new GUIStyle(GUI.skin.label) { fontSize = 12, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleLeft, normal = { textColor = new Color(1f, 0.9f, 0.2f) } };
        string cab = planPaso == 1 ? "ORIGEN: " : (planPaso == 2 ? "DESTINO: " : "");
        GUI.Label(new Rect(x + 8f, r.y + 3f, ancho - 40f, 20f), cab + aeroSel.ToUpper(), tit);
        if (GUI.Button(new Rect(x + ancho - 28f, r.y + 4f, 22f, 20f), "X")) { aeroSel = ""; return; }
        int[] modos = planPaso == 1 ? new[] { 1, 2 } : (planPaso == 2 ? new[] { 0, 2 } : new[] { 1, 2, 0 });
        string[] nomB = { "ATERRIZAR", "DESPEGAR", "EN EL AIRE" }; Color[] colB = { new Color(1f, 0.6f, 0.15f), new Color(0.3f, 0.9f, 0.35f), new Color(0.35f, 0.7f, 1f) }; float[] angB = { 35f, -35f, 0f };
        var pe = new GUIStyle(GUI.skin.label) { fontSize = 11, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter, normal = { textColor = Color.white } };
        var cir = Circulo(); var c0 = GUI.color; int nb = modos.Length;
        for (int i = 0; i < nb; i++)
        {
            int m = modos[i]; float paso = ancho / nb; float cx = x + paso * (i + 0.5f), cy = r.y + 56f; var rc = new Rect(cx - 30f, cy - 30f, 60f, 60f);
            bool sobre = rc.Contains(e.mousePosition);
            if (e.type == EventType.Repaint)
            {
                GUI.color = sobre ? Color.white : colB[m]; GUI.DrawTexture(new Rect(cx - 30f, cy - 30f, 60f, 60f), cir);
                GUI.color = new Color(0.08f, 0.08f, 0.1f); GUI.DrawTexture(new Rect(cx - 27f, cy - 27f, 54f, 54f), cir);
                var mat = GUI.matrix; GUIUtility.RotateAroundPivot(angB[m], new Vector2(cx, cy)); GUI.color = colB[m]; GUI.DrawTexture(new Rect(cx - 19f, cy - 19f, 38f, 38f), iconoAvion); GUI.matrix = mat;
                GUI.color = colB[m]; string flecha = m == 1 ? "\u25B2" : (m == 0 ? "\u25BC" : "\u25CF"); GUI.Label(new Rect(cx - 12f, cy - 31f, 24f, 14f), flecha, pe);
            }
            GUI.color = Color.white; GUI.Label(new Rect(cx - 40f, cy + 31f, 80f, 16f), nomB[m], pe);
            if (e.type == EventType.MouseDown && e.button == 0 && rc.Contains(e.mousePosition))
            {
                if (planPaso == 1) { planOrigen = aeroSel; planModoO = m; planPaso = 2; aeroSel = ""; textoAviso = "Ahora toca el DESTINO"; avisoHasta = Time.time + 3f; }
                else if (planPaso == 2)
                {
                    if (aeroSel == planOrigen) { textoAviso = "El destino tiene que ser otro aeropuerto"; avisoHasta = Time.time + 3f; }
                    else { planDestino = aeroSel; planModoD = m; planPaso = 3; aeroSel = ""; planLinea = true; }
                }
                else
                {
                    string msg; bool ok = Aeropuertos.Colocar(aeroSel, m, out msg);
                    textoAviso = msg; avisoHasta = Time.time + 4f; if (ok) { sigueAvion = true; aeroSel = ""; }
                }
                e.Use(); break;
            }
        }
        GUI.color = c0;
    }
    void DibujarPlanListo(Event e, Rect r)
    {
        float ancho = 252f, alto = 96f, W = Screen.width; float x = r.x - ancho - 6f; if (x < 4f) x = Mathf.Min(r.x + 6f, W - ancho - 4f);
        var rc = new Rect(x, r.y, ancho, alto); rectOpcionesAero = rc; GUI.Box(rc, "");
        var tit = new GUIStyle(GUI.skin.label) { fontSize = 12, fontStyle = FontStyle.Bold, normal = { textColor = new Color(1f, 0.9f, 0.2f) } };
        var txt = new GUIStyle(GUI.skin.label) { fontSize = 11, wordWrap = true, normal = { textColor = Color.white } };
        GUI.Label(new Rect(x + 8f, r.y + 3f, ancho - 16f, 20f), "PLAN DE VUELO", tit);
        GUI.Label(new Rect(x + 8f, r.y + 22f, ancho - 16f, 44f), "1: " + planOrigen + " (" + (planModoO == 1 ? "despega" : "en el aire") + ")\n2: " + planDestino + " (" + (planModoD == 0 ? "aterriza, cobra" : "en el aire, paseo sin cobro") + ")", txt);
        if (GUI.Button(new Rect(x + 8f, r.y + alto - 28f, 130f, 22f), "CONFIRMAR")) PlanConfirmar();
        if (GUI.Button(new Rect(x + 146f, r.y + alto - 28f, 98f, 22f), "CANCELAR")) PlanCancelar();
        if (e.type == EventType.MouseDown && rc.Contains(e.mousePosition)) e.Use();
    }
    void DibujarLineaPlan(Rect r, double izq, double arr)
    {
        var a = PuntoPorNombre(planOrigen); var b = PuntoPorNombre(planDestino); if (a == null || b == null) return;
        Vector2 p0 = new Vector2((float)(LonAPx(a.lon, zoomMapa) - izq), (float)(LatAPx(a.lat, zoomMapa) - arr)), p1 = new Vector2((float)(LonAPx(b.lon, zoomMapa) - izq), (float)(LatAPx(b.lat, zoomMapa) - arr));
        Vector2 d = p1 - p0; float L = d.magnitude; if (L < 1f) return; float ang = Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg;
        var m0 = GUI.matrix; var c0 = GUI.color; GUIUtility.RotateAroundPivot(ang, p0); GUI.color = new Color(1f, 0.2f, 0.9f, 0.95f); GUI.DrawTexture(new Rect(p0.x, p0.y - 1.5f, L, 3f), Texture2D.whiteTexture); GUI.matrix = m0; GUI.color = c0;
    }

    // Minimapa de calles (OpenStreetMap, como el mapa grande pero chico y siempre centrado en vos, norte arriba).
    // El zoom que dejes con la ruedita queda guardado y NO se corrige solo.
    void DibujarMini(Event e)
    {
        var r = rectMini;
        var ancla = AnclaActiva;
        double lat = 0, lon = 0;
        if (ancla != null) { var llh = ancla.longitudeLatitudeHeight; lon = llh.x; lat = llh.y; }
        GUI.DrawTexture(r, fondoOscuro);
        double cxPx = LonAPx(lon, zoomMini), cyPx = LatAPx(lat, zoomMini);
        double izq = cxPx - r.width * 0.5, arr = cyPx - r.height * 0.5;
        int n = 1 << zoomMini;
        GUI.BeginGroup(r);
        if (e.type == EventType.Repaint)
        {
            int tx0 = (int)System.Math.Floor(izq / 256.0), tx1 = (int)System.Math.Floor((izq + r.width) / 256.0);
            int ty0 = (int)System.Math.Floor(arr / 256.0), ty1 = (int)System.Math.Floor((arr + r.height) / 256.0);
            for (int ty = ty0; ty <= ty1; ty++)
            {
                if (ty < 0 || ty >= n) continue;
                for (int tx = tx0; tx <= tx1; tx++)
                {
                    var tex = Baldosa(zoomMini, ((tx % n) + n) % n, ty);
                    if (tex == null) continue;
                    GUI.DrawTexture(new Rect((float)(tx * 256.0 - izq), (float)(ty * 256.0 - arr), 256f, 256f), tex);
                }
            }
            GUI.DrawTexture(new Rect(r.width * 0.5f - 8f, r.height * 0.5f - 8f, 16f, 16f), puntoRojo);
        }
        GUI.EndGroup();
        GUI.Box(new Rect(r.xMax - 16f, r.yMax - 16f, 16f, 16f), "");
        GUI.Label(new Rect(r.x + 4f, r.y, r.width - 8f, 16f), "z" + zoomMini);
        if (e.type == EventType.MouseDown && e.button == 0 && r.Contains(e.mousePosition))
        {
            if (new Rect(r.xMax - 18f, r.yMax - 18f, 18f, 18f).Contains(e.mousePosition)) arrastrando = Arr.ResizeMini;
            else arrastrando = Arr.MoverMini;
            e.Use();
        }
        if (e.type == EventType.ScrollWheel && r.Contains(e.mousePosition))
        {
            zoomMini = Mathf.Clamp(zoomMini + (e.delta.y < 0f ? 1 : -1), ZOOM_MIN, 18);
            PlayerPrefs.SetInt("zoom_mini", zoomMini); PlayerPrefs.Save();
            e.Use();
        }
    }
}
