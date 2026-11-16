// Selector de avion antes de empezar el vuelo. Los modelos (.glb) y sonidos se leen DIRECTO de la carpeta del Godot
// ("simulador Claude"), sin copiarlos al proyecto de Unity. Escalas y sonidos: los de TIPOS_AVION de principal.gd.
using UnityEngine;

public class SelectorAvion : MonoBehaviour
{
    public class Tipo
    {
        public string nombre; public string archivo; public float escala; public string sonido; public bool recurso; public float mb;
        public bool helicoptero; public float ry = 180f; public string[] rotoresPrinc; public string rotorCola;   // solo helicopteros (rotores animados)
    }

    // helicopteros: modelos preparados con rotores separados, en la carpeta modelos_helicopteros del proyecto Unity
    static string Heli(string f) { return System.IO.Path.GetFullPath(System.IO.Path.Combine(Application.dataPath, "..", "..", "modelos_helicopteros", f)); }
    static Tipo H(string nombre, string archivo, float escala, string sonido, float ry, string[] rotoresPrinc, string rotorCola)
    {
        float mb = 0f;
        try { var fi = new System.IO.FileInfo(Heli(archivo)); if (fi.Exists) mb = fi.Length / 1048576f; } catch { }
        return new Tipo { nombre = nombre, archivo = Heli(archivo), escala = escala, sonido = FX(sonido), mb = mb, helicoptero = true, ry = ry, rotoresPrinc = rotoresPrinc, rotorCola = rotorCola };
    }

    public static string cargando = "";
    public static float cargandoDesde;
    static string Godot { get { return System.IO.Path.GetFullPath(System.IO.Path.Combine(Application.dataPath, "..", "..", "..", "simulador Claude")); } }
    static string M(string f) { return System.IO.Path.Combine(Godot, "modelos_aviones", f); }
    static string FX(string f) { return System.IO.Path.Combine(Godot, "FX", f); }

    const string HELICE = "motor_helice.mp3";
    const string AVIONETA = "freesound_community-prop-plane-14513 avioneta.mp3";
    const string GRANDE = "ernyslts-aircraft-cabin-sound-avion grande lindo.mp3";

    Tipo[] tipos;
    public static Tipo[] Lista;
    public static bool mostrar = true;   // true = todavia hay que elegir avion (la eleccion se hace en la pestana Aviones del panel de abajo, ver Hangar.cs)
    Vector2 scroll;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Crear()
    {
        if (FindFirstObjectByType<CesiumForUnity.CesiumGeoreference>() == null) return;
        new GameObject("SelectorAvion").AddComponent<SelectorAvion>();
    }

    static Tipo T(string nombre, string archivo, float escala, string sonido)
    {
        float mb = 0f;
        try { var fi = new System.IO.FileInfo(M(archivo)); if (fi.Exists) mb = fi.Length / 1048576f; } catch { }
        return new Tipo { nombre = nombre, archivo = M(archivo), escala = escala, sonido = FX(sonido), mb = mb };
    }

    void Awake()
    {
        Lista = tipos = new Tipo[]
        {
            new Tipo { nombre = "Emirates Boeing 777-200", recurso = true, escala = 0.3f },
            T("Cessna osmosikum (nuevo) - el azul", "cessna_osmosikum_nuevo.glb", 0.0076f, AVIONETA),
            T("Cessna 172SP (nuevo)", "cessna172sp_nuevo.glb", 3.0f, AVIONETA),
            T("Jet privado", "jet_privado.glb", 0.9f, GRANDE),
            T("F/A-18E Super Hornet (nuevo)", "fa18e_maverick.glb", 0.85f, GRANDE),
            T("Pilatus PC-24", "pilatus_pc24.glb", 0.75f, GRANDE),
            T("Miljet (interior 5 estrellas)", "miljet.glb", 16.2f, GRANDE),
            T("Saab 37 Viggen", "saab_viggen.glb", 1.82f, GRANDE),
            T("British Airways Boeing 747-400", "ba_boeing747.glb", 0.231f, GRANDE),
            T("Airbus A380-900", "airbus_a380.glb", 0.00021f, GRANDE),
            H("Mil Mi-35M (helicoptero ruso)", "mi35m_animable.glb", 0.8f, "freesound_community-helicopter-sound-41975.mp3", 0f, new[] { "RotorPrincipal" }, "RotorCola"),
        };
    }

    // al abrir el juego: ya arranca el Miljet (sin pantalla de eleccion); la eleccion se abre solo con el boton "Cambiar avion"
    void Start()
    {
        foreach (var t in tipos) if (t.nombre.StartsWith("Miljet")) { Elegir(t); return; }
    }

    void OnGUI()
    {
        float W = Screen.width, H = Screen.height;
        if (cargando != "")
        {
            var s = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontSize = 24, fontStyle = FontStyle.Bold };
            GUI.Label(new Rect(W * 0.5f - 400f, H * 0.5f - 20f, 800f, 40f), cargando + " (" + ((int)(Time.unscaledTime - cargandoDesde)) + " s)", s);
        }
        if (!mostrar)
        {
            // boton para volver a elegir (al costado del relojito): abre la pestana Aviones
            Instrucciones.Marcar("cambiar", new Rect(DiaNoche.BarraX(W) + 255f, 4f, 130f, 26f));
            if (cargando == "" && GUI.Button(new Rect(DiaNoche.BarraX(W) + 255f, 4f, 130f, 26f), "Cambiar avion")) { mostrar = true; Hangar.abrir = true; }
        }
    }

    // elegir un avion (lo llama la pestana Aviones)
    public static void Elegir(Tipo t) { mostrar = false; AvionDeCarton.Aparecer(t); }
}
