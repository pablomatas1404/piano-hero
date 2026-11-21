// CONCESIONARIA (20/11, pedido de Pablo): los autos que se bajaron de Sketchfab (casas/descargas/autos) se venden en el Shopping,
// del mas barato al mas caro. Cada uno con foto de afuera y del interior (fotos_tienda/<nombre>.jpg y "<nombre> (interior).jpg").
// Botones: PROBAR (5 minutos, gratis), ALQUILAR 1 dia / 3 dias, COMPRAR. El Jetta es el auto que da la empresa (gratis, ya es tuyo).
// "Dia" de alquiler = 60 minutos de juego (se cuentan solo mientras el juego esta abierto). Cada auto tiene su velocidad, aceleracion y giro.
// MapaAuto los maneja: al bajar del avion solo aparecen los autos que tenes (comprados, alquilados, en prueba o el de la empresa).
using UnityEngine;
using System.Collections.Generic;

public class Concesionaria : MonoBehaviour
{
    public class Auto
    {
        public string id, nombre, archivo, desc; public float largo, rotExtra; public int precio, alq1, alq3;
        public float velKmh, acel, giro; public bool empresa;
        public float VelMs { get { return velKmh / 3.6f; } }
    }
    static string Carpeta { get { return System.IO.Path.GetFullPath(System.IO.Path.Combine(Application.dataPath, "..", "..", "casas", "descargas", "autos")); } }
    public static string RutaOriginal(Auto a) { return System.IO.Path.Combine(Carpeta, a.archivo); }
    // ---- modelos pesados (21/11): el Volvo (163 MB) tiro abajo Unity por falta de memoria. Los pesados tienen una version liviana (<nombre>_liviano.glb,
    // texturas a 1024 px): se usa esa salvo que Pablo elija la original, corriendo el riesgo de que el juego se trabe.
    public const long PESADO_BYTES = 60L * 1024 * 1024;
    public static string RutaLiviana(Auto a) { return System.IO.Path.Combine(Carpeta, a.archivo.Replace(".glb", "_liviano.glb")); }
    public static bool Pesado(Auto a) { try { return a.archivo.EndsWith(".glb") && new System.IO.FileInfo(RutaOriginal(a)).Length >= PESADO_BYTES; } catch { return false; } }
    public static bool TieneLiviana(Auto a) { return System.IO.File.Exists(RutaLiviana(a)); }
    public static bool UsaOriginal(Auto a) { return PlayerPrefs.GetInt("auto_original_" + a.id, 0) == 1; }
    public static long MB(Auto a) { try { return new System.IO.FileInfo(RutaOriginal(a)).Length / (1024 * 1024); } catch { return 0; } }
    public static string Ruta(Auto a) { return Pesado(a) && TieneLiviana(a) && !UsaOriginal(a) ? RutaLiviana(a) : RutaOriginal(a); }

    // largo = medida real del auto en metros (los modelos vienen a cualquier escala); rotExtra = giro para que la trompa mire adelante
    static Auto A(string nombre, string archivo, float largo, float rotExtra, int precio, float vel, float acel, float giro, string desc, bool empresa = false)
    {
        return new Auto { id = "Autos|" + nombre, nombre = nombre, archivo = archivo, largo = largo, rotExtra = rotExtra, precio = precio,
            alq1 = Mathf.RoundToInt(precio * 0.03f / 10f) * 10, alq3 = Mathf.RoundToInt(precio * 0.07f / 10f) * 10, velKmh = vel, acel = acel, giro = giro, desc = desc, empresa = empresa };
    }
    public static readonly Auto[] LISTA = {
        A("Volkswagen Jetta 1986",  "jetta/scene.gltf",                             4.38f, 0f, 0,      150f, 4.0f, 34f, "El auto que te da la empresa. Viejo, pero arranca.", true),
        A("Honda Integra",          "honda_integra_db8_type-r.glb",                 4.38f, 0f, 8000,   210f, 6.5f, 44f, "Deportivo japones de los 90, liviano y nervioso."),
        A("Toyota GT86",            "toyota_gt86_3d_model_free.glb",                4.24f, 0f, 15000,  220f, 7.0f, 46f, "Coupe chico de traccion trasera, muy divertido."),
        A("Nissan 350Z",            "nissan_350z_rocket_bunny.glb",                 4.42f, 0f, 18000,  240f, 7.5f, 45f, "Tuneado Rocket Bunny, ancho y bajo."),
        A("BMW M3 E30",             "bmw_m3_e30.glb",                               4.36f, 0f, 22000,  230f, 7.5f, 44f, "El clasico de los 80. Una leyenda."),
        A("Hyundai Santa Fe",       "2024_hyundai_santa_fe.glb",                    4.83f, 0f, 25000,  190f, 6.0f, 32f, "Camioneta familiar, alta y comoda."),
        A("Volvo S90",              "volvo_s90_recharge_free.glb",                  4.96f, 0f, 35000,  220f, 7.0f, 36f, "Sedan de lujo sueco, interior de madera."),
        A("BMW M4",                 "bmw_m4_f82.glb",                               4.67f, 0f, 45000,  280f, 10f,  42f, "Deportivo aleman moderno, muy rapido."),
        A("Dodge Charger Hellcat",  "dodge_charger_srt_hellcat__high_quality.glb",  5.10f, 0f, 60000,  300f, 11f,  36f, "Muscle car americano de 700 caballos."),
        A("Mercedes Maybach",       "mercedes-benz_maybach_2022.glb",               5.47f, -90f, 120000, 250f, 8.0f, 32f, "El auto de los presidentes. Lujo total."),
    };
    public static Auto Buscar(string id) { foreach (var a in LISTA) if (a.id == id) return a; return null; }

    // ---- tiempo de juego (para los alquileres) y prueba ----
    static Concesionaria inst; static float jugado;   // minutos de juego acumulados (se guardan)
    public static string pruebaId = ""; static float pruebaResta;   // segundos de prueba que quedan
    const float MIN_POR_DIA = 60f, PRUEBA_SEG = 300f;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Crear() { new GameObject("Concesionaria").AddComponent<Concesionaria>(); }
    void Awake() { inst = this; jugado = PlayerPrefs.GetFloat("conc_jugado", 0f); }
    float proxGuardar;
    void Update()
    {
        jugado += Time.unscaledDeltaTime / 60f;
        if (Time.unscaledTime > proxGuardar) { proxGuardar = Time.unscaledTime + 20f; PlayerPrefs.SetFloat("conc_jugado", jugado); }
        if (pruebaId != "")
        {
            if (AvionDeCarton.enAuto && MapaAuto.AutoActualId == pruebaId) pruebaResta -= Time.deltaTime;
            if (pruebaResta <= 0f)
            {
                var a = Buscar(pruebaId); pruebaId = "";
                Mapeo.Cartel("Termino la prueba del " + (a != null ? a.nombre : "auto") + ".\nSi te gusto, alquilalo o compralo en el Shopping.", 5f);
                if (a != null && MapaAuto.AutoActualId == a.id && !Disponible(a.id)) MapaAuto.VolverAlAvion();
            }
        }
    }
    void OnApplicationQuit() { PlayerPrefs.SetFloat("conc_jugado", jugado); }

    public static bool Tuyo(Auto a) { return a.empresa || Carrera.Tiene(a.id); }
    public static float AlquilerResta(Auto a) { return PlayerPrefs.GetFloat("conc_alq_" + a.id, -1f) - jugado; }   // minutos
    public static bool Disponible(string id)
    {
        var a = Buscar(id); if (a == null) return true;   // autos viejos (BMW / patrullero) siguen disponibles hasta que los borremos
        return Tuyo(a) || AlquilerResta(a) > 0f || pruebaId == id;
    }
    public static string Estado(Auto a)
    {
        if (a.empresa) return "Te lo da la empresa";
        if (Carrera.Tiene(a.id)) return "ES TUYO";
        float r = AlquilerResta(a); if (r > 0f) return "Alquilado: quedan " + Mathf.CeilToInt(r) + " min";
        if (pruebaId == a.id) return "En prueba: quedan " + Mathf.CeilToInt(Mathf.Max(0f, pruebaResta)) + " s";
        return "";
    }
    public static string PruebaTexto { get { if (pruebaId == "") return ""; var a = Buscar(pruebaId); return a == null ? "" : "PRUEBA " + a.nombre + ": " + Mathf.CeilToInt(Mathf.Max(0f, pruebaResta)) + " s"; } }

    static void Probar(Auto a)
    {
        pruebaId = a.id; pruebaResta = PRUEBA_SEG;
        if (!MapaAuto.Usar(a.id)) Mapeo.Cartel("Prueba del " + a.nombre + " lista (5 minutos).\nAterriza y baja del avion: el auto va a estar en la lista.", 4f);
    }
    static void Alquilar(Auto a, int dias)
    {
        int precio = dias == 1 ? a.alq1 : a.alq3;
        if (Carrera.Plata < precio) { Mapeo.Cartel("Te faltan $" + (precio - Carrera.Plata).ToString("N0") + " para alquilar el " + a.nombre, 3f); return; }
        Carrera.Modificar(-precio);
        float desde = Mathf.Max(jugado, PlayerPrefs.GetFloat("conc_alq_" + a.id, -1f));
        PlayerPrefs.SetFloat("conc_alq_" + a.id, desde + dias * MIN_POR_DIA); PlayerPrefs.Save();
        Mapeo.Cartel("ALQUILASTE el " + a.nombre + " por " + dias + (dias == 1 ? " dia" : " dias") + "  (-$" + precio.ToString("N0") + ")", 4f);
    }
    static void Comprar(Auto a)
    {
        if (Carrera.Comprar(a.id, a.precio)) Mapeo.Cartel("COMPRASTE el " + a.nombre + "!\n-$" + a.precio.ToString("N0") + "   Es tuyo para siempre.", 5f);
        else Mapeo.Cartel("Te faltan $" + (a.precio - Carrera.Plata).ToString("N0") + " para comprar el " + a.nombre + ".\nHace mas misiones!", 3.5f);
    }

    // ---- fotos ----
    static readonly Dictionary<string, Texture2D> fotos = new Dictionary<string, Texture2D>();
    static readonly Dictionary<string, bool> verInterior = new Dictionary<string, bool>();
    static Texture2D Foto(string nombre)
    {
        Texture2D t; if (fotos.TryGetValue(nombre, out t)) return t;
        string ruta = System.IO.Path.GetFullPath(System.IO.Path.Combine(Application.dataPath, "..", "..", "fotos_tienda", nombre + ".jpg"));
        t = null;
        try { if (System.IO.File.Exists(ruta)) { t = new Texture2D(2, 2); t.LoadImage(System.IO.File.ReadAllBytes(ruta)); } } catch { t = null; }
        fotos[nombre] = t; return t;
    }

    // ---- la seccion del Shopping ----
    public static void Dibujar(GUIStyle txt, GUIStyle btn, float ancho)
    {
        var sNom = new GUIStyle(txt) { fontStyle = FontStyle.Bold, fontSize = Mathf.Max(15, txt.fontSize + 3), wordWrap = true };
        var sPre = new GUIStyle(txt) { fontStyle = FontStyle.Bold, fontSize = Mathf.Max(18, txt.fontSize + 6) };
        var sChico = new GUIStyle(txt) { wordWrap = true };
        var sEst = new GUIStyle(txt) { fontStyle = FontStyle.Bold }; sEst.normal.textColor = new Color(0.5f, 1f, 0.55f);
        GUILayout.Label("CONCESIONARIA  -  Proba gratis 5 minutos, alquila por dias o compralo. Tu dinero: $" + Carrera.Plata.ToString("N0") + "   (1 dia de alquiler = 60 minutos de juego)", sChico);
        float fw = Mathf.Clamp(ancho * 0.42f, 220f, 420f), fh = fw * 0.8f;
        var bg = GUI.backgroundColor;
        foreach (var a in LISTA)
        {
            GUILayout.BeginHorizontal(GUI.skin.box);
            // fotos: afuera / adentro
            GUILayout.BeginVertical(GUILayout.Width(fw));
            bool vi; verInterior.TryGetValue(a.id, out vi);
            var f = Foto(vi ? a.nombre + " (interior)" : a.nombre);
            var r = GUILayoutUtility.GetRect(fw, fh, GUILayout.Width(fw), GUILayout.Height(fh));
            if (f != null) GUI.DrawTexture(r, f, ScaleMode.ScaleAndCrop); else GUI.Box(r, "sin foto");
            GUILayout.BeginHorizontal();
            GUI.backgroundColor = !vi ? new Color(0.4f, 1f, 0.5f) : bg; if (GUILayout.Button("AFUERA", btn, GUILayout.Height(26))) verInterior[a.id] = false;
            GUI.backgroundColor = vi ? new Color(0.4f, 1f, 0.5f) : bg; if (GUILayout.Button("ADENTRO", btn, GUILayout.Height(26))) verInterior[a.id] = true;
            GUI.backgroundColor = bg; GUILayout.EndHorizontal();
            GUILayout.EndVertical();
            // datos y botones
            GUILayout.BeginVertical();
            GUILayout.Label(a.nombre, sNom);
            GUILayout.Label(a.desc, sChico);
            if (Pesado(a))
            {
                var sAv = new GUIStyle(sChico) { fontStyle = FontStyle.Bold }; sAv.normal.textColor = new Color(1f, 0.8f, 0.25f);
                bool orig = UsaOriginal(a);
                GUILayout.Label("ATENCION: modelo pesado (" + MB(a) + " MB). Si tu compu tiene poca memoria, el juego puede quedar trabado o cerrarse." + (TieneLiviana(a) ? "\nTiene VERSION LIVIANA (casi igual de linda, mucho mas liviana): se usa esa. Si queres la original, elegila abajo y corres el riesgo." : ""), sAv);
                if (TieneLiviana(a))
                {
                    GUILayout.BeginHorizontal();
                    GUI.backgroundColor = !orig ? new Color(0.4f, 1f, 0.5f) : bg; if (GUILayout.Button("VERSION LIVIANA (recomendada)", btn, GUILayout.Height(28))) { PlayerPrefs.SetInt("auto_original_" + a.id, 0); PlayerPrefs.Save(); }
                    GUI.backgroundColor = orig ? new Color(1f, 0.55f, 0.3f) : bg; if (GUILayout.Button("ORIGINAL (puede trabar)", btn, GUILayout.Height(28))) { PlayerPrefs.SetInt("auto_original_" + a.id, 1); PlayerPrefs.Save(); }
                    GUI.backgroundColor = bg; GUILayout.EndHorizontal();
                }
            }
            GUILayout.Label("Velocidad maxima: " + Mathf.RoundToInt(a.velKmh) + " km/h     0 a 100: " + (27.8f / a.acel).ToString("0.0") + " s     Giro: " + (a.giro >= 44f ? "agil" : a.giro >= 36f ? "normal" : "pesado"), sChico);
            string est = Estado(a); if (est != "") GUILayout.Label(est, sEst);
            if (!a.empresa) GUILayout.Label("$" + a.precio.ToString("N0"), sPre);
            GUILayout.BeginHorizontal();
            if (Disponible(a.id))
            {
                GUI.backgroundColor = new Color(0.4f, 1f, 0.5f);
                if (GUILayout.Button("USAR AHORA", btn, GUILayout.Height(34))) { if (!MapaAuto.Usar(a.id)) Mapeo.Cartel("Aterriza y baja del avion: el " + a.nombre + " esta en tu lista de autos.", 3.5f); }
                GUI.backgroundColor = bg;
            }
            if (!Tuyo(a))
            {
                if (pruebaId != a.id && GUILayout.Button("PROBAR 5 MIN", btn, GUILayout.Height(34))) Probar(a);
                if (GUILayout.Button("ALQUILAR 1 DIA\n$" + a.alq1.ToString("N0"), btn, GUILayout.Height(34))) Alquilar(a, 1);
                if (GUILayout.Button("ALQUILAR 3 DIAS\n$" + a.alq3.ToString("N0"), btn, GUILayout.Height(34))) Alquilar(a, 3);
                GUI.backgroundColor = Carrera.Plata >= a.precio ? new Color(1f, 0.85f, 0.3f) : new Color(0.6f, 0.45f, 0.45f);
                if (GUILayout.Button("COMPRAR", btn, GUILayout.Height(34))) Comprar(a);
                GUI.backgroundColor = bg;
            }
            GUILayout.EndHorizontal();
            GUILayout.EndVertical();
            GUILayout.EndHorizontal();
            GUILayout.Space(6f);
        }
    }

    // cartel chico arriba mientras dura la prueba
    void OnGUI()
    {
        if (pruebaId == "" || !AvionDeCarton.enAuto || MapaAuto.AutoActualId != pruebaId) return;
        var s = new GUIStyle(GUI.skin.label) { fontSize = 18, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter }; s.normal.textColor = new Color(1f, 0.85f, 0.3f);
        var rc = new Rect(Screen.width * 0.5f - 220f, 44f, 440f, 30f); var c = GUI.color; GUI.color = new Color(0, 0, 0, 0.6f); GUI.DrawTexture(rc, Texture2D.whiteTexture); GUI.color = c;
        GUI.Label(rc, PruebaTexto, s);
    }
}
