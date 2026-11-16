// Aeropuertos, luces de pista, ILS y carteles de nombre.
// Portado de Godot (mundo.gd). Los numeros son los reales de Godot (ver RESPUESTA_AEROPUERTOS_Y_TIERRA.md).
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Mathematics;
using CesiumForUnity;

public class Aeropuertos : MonoBehaviour
{
    // ---------- datos del JSON ----------
    [Serializable] class CabJ { public double lat; public double lon; public double alt_m; }
    [Serializable] class PistaJ { public CabJ cabecera_1; public CabJ cabecera_2; public float ancho_medio_pista_m; }
    [Serializable] class AeroJ { public string nombre; public double lat_aproximada; public double lon_aproximada; public bool tiene_ils_dos_cabeceras; public PistaJ pista; public PistaJ[] pistas; }
    [Serializable] class ListaJ { public AeroJ[] items; }

    // ---------- constantes de Godot ----------
    const float INTERVALO_LUCES = 0.12f;           // INTERVALO_ACTUALIZACION_LUCES_PISTA
    const float ANCHO_MEDIO_DEFECTO = 20f;         // ANCHO_MEDIO_PISTA_LUCES
    const float ESPACIADO_LUCES = 60f;             // ESPACIADO_LUCES_PISTA
    const float RADIO_LUZ = 2.5f;                  // RADIO_LUZ_PISTA
    const float ALTURA_LUCES_SOBRE_PISO = 1f;
    const float DESVIO_MAXIMO_RAYCAST = 12f;       // mas que esto se descarta el rayo
    const float DISTANCIA_ACHIQUE = 3000f;
    const float ESCALA_MINIMA = 0.35f;
    const float ALTURA_BEACON_OCULTO = 800f;   // el cartel del nombre desaparece por debajo de 800 m (antes 500: en Chile el terreno esta a ~600 m y molestaba al aterrizar)
    const float ALTURA_BEACON_VISIBLE = 1100f;
    const float TANGENTE_PLANEO = 0.0524f;         // 3 grados
    static readonly float[] DIST_ARO = { 6000, 5250, 4500, 3750, 3000, 2500, 2200, 1850, 1500, 1150, 900 };
    const float RADIO_INT_ARO = 65f, RADIO_EXT_ARO = 80f;
    static readonly Color COL_UMBRAL = new Color(0.25f, 1f, 0.35f);
    static readonly Color COL_DORADO = new Color(1f, 0.85f, 0.4f);     // clasico_dorado
    static readonly Color COL_ARO1 = new Color(1f, 0.85f, 0.1f);       // amarillo
    static readonly Color COL_ARO2 = new Color(0.25f, 1f, 0.25f);      // verde fluor
    const float RADIO_TIERRA = 6378137f;
    const float DISTANCIA_ACTIVA = 25000f;         // solo se arman/muestran luces de pistas a menos de 25 km
    const float DISTANCIA_RAYOS = 8000f;           // el terreno con colision solo existe cerca del avion

    // factor dia/noche: 0 = dia, 1 = noche (despues lo maneja el ciclo dia/noche; tecla N para probar)
    public static float factorNoche = 0f;

    class Luz { public Transform t; public Renderer r; public float u; public Color col; public bool umbral; public bool fija; public bool blanca; public bool lejana; }
    class Aero
    {
        public string nombre; public double lat0, lon0, h0;
        public GameObject raiz; public bool tienePista;
        public Vector2 c1, c2; public float a1, a2, ancho, largo; public double la1, lo1, la2, lo2;
        public bool lucesCreadas; public GameObject lucesRaiz; public List<Luz> luces = new List<Luz>();
        public GameObject ils; public bool ilsVisible; public float dY; public bool sinCartel; public bool multiPista; public int indicePista;
    }

    List<Aero> aeros = new List<Aero>();
    AvionDeCarton avion; Camera cam;
    Mesh mallaEsfera, mallaAro;
    Material matLuz;
    MaterialPropertyBlock mpb;
    bool ilsEncendido;
    float proxRayos; string avisoIls = ""; float avisoHasta;
    GUIStyle sBeacon;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Crear()
    {
        var geo = FindFirstObjectByType<CesiumGeoreference>();
        if (geo == null) return;
        var go = new GameObject("Aeropuertos");
        go.transform.SetParent(geo.transform, false);
        go.AddComponent<Aeropuertos>();
    }

    // ---------- coordenadas ----------
    static double MetrosPorGradoLat(double latDeg) { double p = latDeg * Math.PI / 180.0; return 111132.92 - 559.82 * Math.Cos(2 * p) + 1.175 * Math.Cos(4 * p); }
    static double MetrosPorGradoLon(double latDeg) { double p = latDeg * Math.PI / 180.0; return 111412.84 * Math.Cos(p) - 93.5 * Math.Cos(3 * p); }
    static Vector2 ENU(Aero a, double lat, double lon) { return new Vector2((float)((lon - a.lon0) * MetrosPorGradoLon(a.lat0)), (float)((lat - a.lat0) * MetrosPorGradoLat(a.lat0))); }
    // altura local: la Tierra baja con la distancia al centro del aeropuerto (curvatura)
    static float LocalY(Aero a, float altElipsoide, float e, float n) { return (float)(altElipsoide - a.h0) - (e * e + n * n) / (2f * RADIO_TIERRA); }

    void Start()
    {
        inst = this; CargarPrecios();
        mallaEsfera = MallaEsfera();
        mallaAro = MallaToroide(0.5f * (RADIO_INT_ARO + RADIO_EXT_ARO), 0.5f * (RADIO_EXT_ARO - RADIO_INT_ARO), 32, 10);
        var sh = Shader.Find("Universal Render Pipeline/Unlit");
        if (sh == null) sh = Shader.Find("Unlit/Color");
        matLuz = new Material(sh);
        if (matLuz.HasProperty("_Cull")) matLuz.SetFloat("_Cull", 0f);   // se ve de los dos lados (el aro, por las dudas)
        mpb = new MaterialPropertyBlock();

        var ta = Resources.Load<TextAsset>("aeropuertos");
        if (ta == null) { Debug.LogWarning("Aeropuertos: falta Resources/aeropuertos.json"); return; }
        string limpio = System.Text.RegularExpressions.Regex.Replace(ta.text, @"""ancho_medio_pista_m""\s*:\s*""[^""]*""", @"""ancho_medio_pista_m"": 0");   // Ezeiza trae un texto en vez de numero
        var lista = JsonUtility.FromJson<ListaJ>("{\"items\":" + limpio + "}");
        if (lista == null || lista.items == null) return;

        foreach (var j in lista.items)
        {
            // una o varias pistas por aeropuerto (Ezeiza tiene 2 cruzadas)
            var pistas = new List<PistaJ>();
            if (j.pistas != null && j.pistas.Length > 0) pistas.AddRange(j.pistas);
            else if (j.tiene_ils_dos_cabeceras && j.pista != null) pistas.Add(j.pista);
            if (pistas.Count == 0) pistas.Add(null);   // sin pista medida: solo el cartel

            for (int k = 0; k < pistas.Count; k++)
            {
                var p = pistas[k];
                var a = new Aero { nombre = j.nombre, sinCartel = k > 0, multiPista = pistas.Count > 1, indicePista = k };
                bool conPista = p != null && p.cabecera_1 != null && p.cabecera_2 != null;
                if (conPista)
                {
                    a.lat0 = (p.cabecera_1.lat + p.cabecera_2.lat) * 0.5;
                    a.lon0 = (p.cabecera_1.lon + p.cabecera_2.lon) * 0.5;
                    a.a1 = (float)p.cabecera_1.alt_m; a.a2 = (float)p.cabecera_2.alt_m;
                    a.h0 = (a.a1 + a.a2) * 0.5;
                    a.la1 = p.cabecera_1.lat; a.lo1 = p.cabecera_1.lon; a.la2 = p.cabecera_2.lat; a.lo2 = p.cabecera_2.lon;
                    a.c1 = ENU(a, p.cabecera_1.lat, p.cabecera_1.lon);
                    a.c2 = ENU(a, p.cabecera_2.lat, p.cabecera_2.lon);
                    a.ancho = p.ancho_medio_pista_m > 0.5f ? p.ancho_medio_pista_m : ANCHO_MEDIO_DEFECTO;
                    a.largo = (a.c2 - a.c1).magnitude;
                    a.tienePista = a.largo > 50f;
                }
                else { a.lat0 = j.lat_aproximada; a.lon0 = j.lon_aproximada; a.h0 = 5.0; }

                var go = new GameObject("Aeropuerto_" + a.nombre + (pistas.Count > 1 ? "_pista" + (k + 1) : ""));
                go.transform.SetParent(transform, false);
                var ancla = go.AddComponent<CesiumGlobeAnchor>();
                ancla.longitudeLatitudeHeight = new double3(a.lon0, a.lat0, a.h0);
                ancla.rotationEastUpNorth = quaternion.identity;   // x = Este, y = Arriba, z = Norte
                a.raiz = go;
                aeros.Add(a);
            }
        }
    }

    // =====================================================================================================
    // ---------- TORRE DE CONTROL (pestana "Pagina 2" del panel de abajo) ----------
    // Portado de principal.gd:7898-8054. Lista de aeropuertos por cercania, boton ILS por aeropuerto, precio por distancia (+/- $25).
    // Nada se guarda en disco (igual que Godot). El boton "Ir" y el cobro dependen del sistema de misiones/dinero (todavia no migrado).
    static Aeropuertos inst;
    class FilaT { public string nombre; public double km, lat, lon; public bool tienePista, ils; }
    List<FilaT> filasT = new List<FilaT>();
    List<FilaT> filasD = new List<FilaT>(); Vector2 scrollD;
    bool ordenAlfa; string letraD = "";   // pestana Destinos: orden alfabetico / por cercania, y filtro por letra inicial
    public class DestinoInfo { public string nombre; public double km, lat, lon; }
    public static List<DestinoInfo> ListaDestinos()   // para las misiones de la pestana Carrera (de menos a mas km)
    {
        var r = new List<DestinoInfo>();
        if (inst != null) foreach (var f in inst.filasD) r.Add(new DestinoInfo { nombre = f.nombre, km = f.km, lat = f.lat, lon = f.lon });
        return r;
    }
    static string SinAcento(string t)
    {
        try { var d = t.Normalize(System.Text.NormalizationForm.FormD); var sb = new System.Text.StringBuilder(); foreach (char c in d) if (System.Globalization.CharUnicodeInfo.GetUnicodeCategory(c) != System.Globalization.UnicodeCategory.NonSpacingMark) sb.Append(c); return sb.ToString().ToUpperInvariant(); }
        catch { return t.ToUpperInvariant(); }
    }
    readonly Dictionary<string, float> preciosT = new Dictionary<string, float>();   // precio por NOMBRE (la lista se reordena sola)
    float proxT; Vector2 scrollT;

    // Precios corregidos a mano: SE GUARDAN (pedido de Pablo; en Godot se perdian). Solo se guardan los que tocas; el resto sigue calculandose por distancia.
    [Serializable] class PrecioJ { public string nombre; public float precio; }
    [Serializable] class ListaPJ { public PrecioJ[] items; }
    readonly HashSet<string> preciosEditados = new HashSet<string>();
    static string RutaPrecios { get { return System.IO.Path.Combine(System.IO.Path.GetFullPath(System.IO.Path.Combine(Application.dataPath, "..", "..")), "precios_torre.json"); } }
    void CargarPrecios()
    {
        try
        {
            if (!System.IO.File.Exists(RutaPrecios)) return;
            var l = JsonUtility.FromJson<ListaPJ>(System.IO.File.ReadAllText(RutaPrecios));
            if (l == null || l.items == null) return;
            foreach (var p in l.items) { preciosT[p.nombre] = p.precio; preciosEditados.Add(p.nombre); }
        }
        catch (Exception e) { Debug.LogWarning("[Torre] no pude leer precios_torre.json: " + e.Message); }
    }
    void GuardarPrecios()
    {
        try
        {
            var l = new ListaPJ(); var v = new List<PrecioJ>();
            foreach (var n in preciosEditados) v.Add(new PrecioJ { nombre = n, precio = preciosT[n] });
            l.items = v.ToArray();
            System.IO.File.WriteAllText(RutaPrecios, JsonUtility.ToJson(l, true));
        }
        catch (Exception e) { Debug.LogWarning("[Torre] no pude guardar precios_torre.json: " + e.Message); }
    }
    // un solo precio (lo que cobras con el avion que estas usando; la maquina aplica sola el multiplicador del avion); +/- sube o baja de a $5
    void FilaPrecio(string nombre, GUIStyle txt, GUIStyle btn, float wb, float wl)
    {
        float p; preciosT.TryGetValue(nombre, out p); float m = Mathf.Max(0.01f, Carrera.MultiplicadorAvion()); int eff = Mathf.RoundToInt(p * m);
        if (Tienda.Repetir("m" + nombre, GUILayout.RepeatButton("-", btn, GUILayout.Width(wb), GUILayout.Height(20)))) CambiarPrecio(nombre, Mathf.Max(0, eff - 1) / m);
        var c0 = GUI.contentColor; GUI.contentColor = new Color(0.6f, 1f, 0.6f); GUILayout.Label("$" + eff, txt, GUILayout.Width(wl)); GUI.contentColor = c0;
        if (Tienda.Repetir("p" + nombre, GUILayout.RepeatButton("+", btn, GUILayout.Width(wb), GUILayout.Height(20)))) CambiarPrecio(nombre, (eff + 1) / m);
    }
    void CambiarPrecio(string nombre, float nuevo) { preciosT[nombre] = nuevo; preciosEditados.Add(nombre); GuardarPrecios(); }
    const float TARIFA_TORRE_POR_KM = 0.55f;   // principal.gd:872
    const int FILAS_TORRE = 15;

    static double Haversine(double la1, double lo1, double la2, double lo2)
    {
        double r = 6371.0, dLa = (la2 - la1) * Math.PI / 180.0, dLo = (lo2 - lo1) * Math.PI / 180.0;
        double h = Math.Sin(dLa / 2) * Math.Sin(dLa / 2) + Math.Cos(la1 * Math.PI / 180.0) * Math.Cos(la2 * Math.PI / 180.0) * Math.Sin(dLo / 2) * Math.Sin(dLo / 2);
        return 2 * r * Math.Asin(Math.Min(1.0, Math.Sqrt(h)));
    }

    void RefrescarTorre()
    {
        double lon, lat; float alt, rumbo;
        if (!MapaAuto.PosActiva(out lon, out lat, out alt, out rumbo)) return;
        var d = new Dictionary<string, FilaT>();
        foreach (var a in aeros)
        {
            double km = Haversine(lat, lon, a.lat0, a.lon0);
            FilaT f;
            if (!d.TryGetValue(a.nombre, out f)) { f = new FilaT { nombre = a.nombre, km = km, lat = a.lat0, lon = a.lon0 }; d[a.nombre] = f; }
            if (km < f.km) f.km = km;
            if (a.tienePista) f.tienePista = true;
            if (a.ilsVisible) f.ils = true;
        }
        var l = new List<FilaT>(d.Values);
        l.Sort((x, y) => x.km.CompareTo(y.km));
        foreach (var f in l)   // tablero de Destinos: todos los aeropuertos, cada uno con su precio
            if (!preciosT.ContainsKey(f.nombre)) preciosT[f.nombre] = Mathf.Max(20f, Mathf.Round((float)f.km * TARIFA_TORRE_POR_KM / 5f) * 5f);
        filasD = new List<FilaT>(l);
        if (l.Count > FILAS_TORRE) l.RemoveRange(FILAS_TORRE, l.Count - FILAS_TORRE);
        foreach (var f in l)
            if (!preciosT.ContainsKey(f.nombre)) preciosT[f.nombre] = Mathf.Max(20f, Mathf.Round((float)f.km * TARIFA_TORRE_POR_KM / 5f) * 5f);   // solo la primera vez, queda fijo
        filasT = l;
    }

    // prende/apaga el ILS de todas las pistas de ese aeropuerto (solo mostrar/ocultar, como mundo.gd:2668)
    void AlternarIlsTorre(string nombre, bool activo)
    {
        foreach (var a in aeros)
        {
            if (a.nombre != nombre || !a.tienePista) continue;
            if (activo && a.ils == null) { CrearIls(a); a.ils.transform.localPosition = new Vector3(0f, a.dY, 0f); }
            if (a.ils != null) { a.ilsVisible = activo; a.ils.SetActive(activo); }
        }
        avisoIls = nombre + (activo ? ": ILS encendido" : ": ILS apagado"); avisoHasta = Time.time + 3f;
    }

    public static void IlsDe(string nombre, bool activo) { if (inst != null) inst.AlternarIlsTorre(nombre, activo); }
    public static bool IlsActivo(string nombre) { if (inst == null) return false; foreach (var a in inst.aeros) if (a.nombre == nombre && a.ilsVisible) return true; return false; }

    // ---- datos para el modo guerra (Guerra.cs): ubicacion de un aeropuerto y sus cabeceras en el mundo ----
    // ---------- selector del mapa: aeropuertos como puntos y colocar el avion (aterrizar / despegar / en el aire) ----------
    public class PuntoMapa { public string nombre; public double lat, lon; public bool tienePista; }
    public static List<PuntoMapa> PuntosMapa()
    {
        var r = new List<PuntoMapa>(); if (inst == null) return r;
        foreach (var a in inst.aeros) if (!a.sinCartel) r.Add(new PuntoMapa { nombre = a.nombre, lat = a.lat0, lon = a.lon0, tienePista = a.tienePista });
        return r;
    }
    // modo 0 = aterrizar (a ~7 km de la cabecera, alineado, en descenso de 3 grados), 1 = despegar (en la cabecera, parado), 2 = en el aire (1500 m sobre el aeropuerto)
    public static bool Colocar(string nombre, int modo, out string msg)
    {
        msg = ""; if (inst == null) { msg = "Todavia no cargaron los aeropuertos"; return false; }
        var av = FindFirstObjectByType<AvionDeCarton>(); if (av == null) { msg = "Todavia no hay avion"; return false; }
        if (AvionDeCarton.enAuto) { msg = "Subi al avion primero"; return false; }
        var an = av.GetComponent<CesiumGlobeAnchor>(); if (an == null) { msg = "El avion no tiene ancla"; return false; }
        Aero ae = null; foreach (var a in inst.aeros) if (a.nombre == nombre && (ae == null || (a.tienePista && !ae.tienePista))) ae = a;
        if (ae == null) { msg = "No encuentro ese aeropuerto"; return false; }
        Torre.ReiniciarCabina(modo != 1);
        double mLat = 111320.0, mLon = 111320.0 * System.Math.Cos(ae.lat0 * System.Math.PI / 180.0);
        var llh = an.longitudeLatitudeHeight; double pe = (llh.x - ae.lon0) * mLon, pn = (llh.y - ae.lat0) * mLat;
        if (modo == 1 && ae.nombre == "Aeroparque")
        {   // salir de pista en Aeroparque: se sale del hangar 1 (mas despejado que el 5)
            an.longitudeLatitudeHeight = new double3(-58.4094390137567, -34.5599939928741, 30.0);
            an.rotationEastUpNorth = Quaternion.Euler(0f, 69.4f, 0f);
            av.Reanudar(); msg = "En el hangar 1 de Aeroparque, listo para salir"; return true;
        }
        if (modo == 2 || !ae.tienePista)
        {
            if (modo == 1) { msg = "Ese aeropuerto no tiene pista medida: te dejo en el aire"; }
            double dir = System.Math.Atan2(-pe, -pn);   // mirando hacia el aeropuerto desde donde estas (si estas lejos); si estas encima, al norte
            if (System.Math.Sqrt(pe * pe + pn * pn) < 500.0) dir = 0.0;
            double dist = modo == 0 ? 6000.0 : 0.0, alt = modo == 0 ? ae.h0 + 400.0 : System.Math.Max(ae.h0 + 2500.0, 3500.0);   // en el aire: bien alto, para no quedar entre montanas (Bariloche)
            double e = -System.Math.Sin(dir) * dist, n = -System.Math.Cos(dir) * dist;
            an.longitudeLatitudeHeight = new double3(ae.lon0 + e / mLon, ae.lat0 + n / mLat, alt);
            an.rotationEastUpNorth = Quaternion.Euler(0f, (float)(dir * 180.0 / System.Math.PI), 0f);
            av.IniciarEnAire(); if (msg == "") msg = modo == 0 ? "Aproximacion a " + nombre + " (sin pista medida)" : "En el aire sobre " + nombre; return true;
        }
        // con pista medida: elegir la cabecera mas conveniente (la del lado donde estas)
        double e1 = (ae.lo1 - ae.lon0) * mLon, n1 = (ae.la1 - ae.lat0) * mLat, e2 = (ae.lo2 - ae.lon0) * mLon, n2 = (ae.la2 - ae.lat0) * mLat;
        double d1 = System.Math.Sqrt((pe - e1) * (pe - e1) + (pn - n1) * (pn - n1)), d2 = System.Math.Sqrt((pe - e2) * (pe - e2) + (pn - n2) * (pn - n2));
        bool usa1 = d1 <= d2;   // umbral elegido; el rumbo va del umbral hacia la otra cabecera
        double ue = usa1 ? e1 : e2, un = usa1 ? n1 : n2, oe = usa1 ? e2 : e1, on = usa1 ? n2 : n1; double uh = usa1 ? ae.a1 : ae.a2;
        double rumbo = System.Math.Atan2(oe - ue, on - un); double se = System.Math.Sin(rumbo), sn = System.Math.Cos(rumbo);
        if (modo == 1)
        {
            an.longitudeLatitudeHeight = new double3(ae.lon0 + (ue + se * 30.0) / mLon, ae.lat0 + (un + sn * 30.0) / mLat, uh + 5.0);
            an.rotationEastUpNorth = Quaternion.Euler(0f, (float)(rumbo * 180.0 / System.Math.PI), 0f);
            av.Reanudar(); msg = "En la cabecera de " + nombre + ", listo para despegar"; return true;
        }
        double dd = 7000.0;
        an.longitudeLatitudeHeight = new double3(ae.lon0 + (ue - se * dd) / mLon, ae.lat0 + (un - sn * dd) / mLat, uh + dd * 0.0524 + 30.0);
        an.rotationEastUpNorth = Quaternion.Euler(0f, (float)(rumbo * 180.0 / System.Math.PI), 0f);
        av.IniciarEnAire(); msg = "En aproximacion a " + nombre + " (7 km de la cabecera)"; return true;
    }
    public static int PrecioDe(string nombre) { float p; if (inst != null && inst.preciosT.TryGetValue(nombre, out p) && p > 0f) return (int)p; return 100; }
    public static bool Buscar(string nombre, out double lat, out double lon, out double h)
    {
        lat = lon = h = 0;
        if (inst == null) return false;
        foreach (var a in inst.aeros) if (a.nombre == nombre) { lat = a.lat0; lon = a.lon0; h = a.h0; return true; }
        return false;
    }
    // busca un aeropuerto por pedazo de nombre (sin acentos ni mayusculas); devuelve el nombre completo
    public static bool BuscarFrag(string frag, out string nombre, out double lat, out double lon)
    {
        nombre = null; lat = lon = 0;
        if (inst == null || inst.aeros.Count == 0) return false;
        string f = SinAcento(frag);
        foreach (var a in inst.aeros) if (SinAcento(a.nombre).Contains(f)) { nombre = a.nombre; lat = a.lat0; lon = a.lon0; return true; }
        return false;
    }
    public static Vector3 PosMundo(string nombre)
    {
        if (inst != null) foreach (var a in inst.aeros) if (a.nombre == nombre) return a.raiz.transform.position;
        return Vector3.zero;
    }
    // 1 = esta sobre alguna pista trackeada de ese aeropuerto (a lo ancho hasta 80 m del eje, y hasta 150 m mas alla de cada cabecera); 0 = no; -1 = ese destino no tiene pista trackeada
    public static int SobrePista(string nombre, Vector3 posMundo)
    {
        if (inst == null) return -1;
        bool hay = false;
        foreach (var a in inst.aeros)
        {
            if (a.nombre != nombre || !a.tienePista) continue;
            hay = true;
            Vector3 l = a.raiz.transform.InverseTransformPoint(posMundo);
            Vector2 p = new Vector2(l.x, l.z), eje = a.c2 - a.c1; float largo = eje.magnitude; if (largo < 1f) continue;
            eje /= largo; Vector2 r = p - a.c1;
            float along = Vector2.Dot(r, eje), lado = Mathf.Abs(r.x * eje.y - r.y * eje.x);
            if (lado < 80f && along > -150f && along < largo + 150f) return 1;
        }
        return hay ? 0 : -1;
    }

    // distancia (m) al eje de la pista trackeada mas cercana si estas sobre ella (para calificar el toque); false = no estas sobre ninguna
    public static bool DesvioPista(Vector3 posMundo, out float lado)
    {
        lado = 0f; if (inst == null) return false; bool hay = false; float mejor = 1e9f;
        foreach (var a in inst.aeros)
        {
            if (!a.tienePista) continue;
            Vector3 l = a.raiz.transform.InverseTransformPoint(posMundo);
            Vector2 p = new Vector2(l.x, l.z), eje = a.c2 - a.c1; float largo = eje.magnitude; if (largo < 1f) continue;
            eje /= largo; Vector2 r = p - a.c1; float along = Vector2.Dot(r, eje), ld = Mathf.Abs(r.x * eje.y - r.y * eje.x);
            if (along > -150f && along < largo + 150f && ld < 150f && ld < mejor) { mejor = ld; hay = true; }
        }
        lado = hay ? mejor : 0f; return hay;
    }

    // respaldo cuando todavia no hay colision de terreno bajo el avion: altura de la pista trackeada si estas a menos de 2,5 km de ella
    public static bool SueloPista(Vector3 posMundo, out Vector3 punto)
    {
        punto = Vector3.zero;
        if (inst == null) return false;
        foreach (var a in inst.aeros)
        {
            if (!a.tienePista) continue;
            Vector3 l = a.raiz.transform.InverseTransformPoint(posMundo);
            Vector2 medio = (a.c1 + a.c2) * 0.5f; float radio = (a.c2 - a.c1).magnitude * 0.5f + 2500f;
            if (new Vector2(l.x - medio.x, l.z - medio.y).magnitude > radio) continue;
            float y = LocalY(a, a.a1, a.c1.x, a.c1.y) + a.dY;
            punto = a.raiz.transform.TransformPoint(new Vector3(l.x, y, l.z));
            return true;
        }
        return false;
    }

    // las dos cabeceras de la primera pista con datos, 15 m sobre el umbral (como origenes_disparo de Godot)
    public static void Cabeceras(string nombre, List<Vector3> salida)
    {
        if (inst == null) return;
        foreach (var a in inst.aeros)
        {
            if (a.nombre != nombre || !a.tienePista) continue;
            Vector3 arriba = a.raiz.transform.up * 15f;
            salida.Add(a.raiz.transform.TransformPoint(new Vector3(a.c1.x, LocalY(a, a.a1, a.c1.x, a.c1.y) + a.dY, a.c1.y)) + arriba);
            salida.Add(a.raiz.transform.TransformPoint(new Vector3(a.c2.x, LocalY(a, a.a2, a.c2.x, a.c2.y) + a.dY, a.c2.y)) + arriba);
            return;
        }
    }

    public static void PestanaTorre(GUIStyle txt, GUIStyle btn, GUIStyle titulo) { if (inst != null) inst.DibujarTorre(txt, btn, titulo); else GUILayout.Label("Torre: todavia no cargaron los aeropuertos", txt); }

    // ===== DESTINOS: tablero tipo "salidas" con TODOS los aeropuertos, del mas cerca al mas lejos =====
    public static void PestanaDestinos(GUIStyle txt, GUIStyle btn, GUIStyle titulo) { if (inst != null) inst.DibujarDestinos(txt, btn, titulo); else GUILayout.Label("Destinos: todavia no cargaron los aeropuertos", txt); }

    void DibujarDestinos(GUIStyle txt, GUIStyle btn, GUIStyle titulo)
    {
        GUILayout.Label("DESTINOS   (Ir = viaje pago: aterrizas sobre su pista y cobras; +/- cambia el precio y queda guardado)", titulo);
        // orden + filtro por letra
        var vista = new List<FilaT>(filasD);
        var letras = new SortedSet<string>(); foreach (var f0 in vista) letras.Add(SinAcento(f0.nombre).Substring(0, 1));
        if (letraD != "" && !letras.Contains(letraD)) letraD = "";
        GUILayout.BeginHorizontal();
        var bg0 = GUI.backgroundColor;
        GUI.backgroundColor = !ordenAlfa ? new Color(0.4f, 1f, 0.4f) : bg0; if (GUILayout.Button("Por cercania", btn, GUILayout.Width(100), GUILayout.Height(22))) ordenAlfa = false;
        GUI.backgroundColor = ordenAlfa ? new Color(0.4f, 1f, 0.4f) : bg0; if (GUILayout.Button("Alfabetico (A-Z)", btn, GUILayout.Width(120), GUILayout.Height(22))) ordenAlfa = true;
        GUI.backgroundColor = letraD == "" ? new Color(0.4f, 1f, 0.4f) : bg0; if (GUILayout.Button("Todos", btn, GUILayout.Width(52), GUILayout.Height(22))) letraD = "";
        foreach (var L in letras) { GUI.backgroundColor = letraD == L ? new Color(0.4f, 1f, 0.4f) : bg0; if (GUILayout.Button(L, btn, GUILayout.Width(24), GUILayout.Height(22))) { letraD = L; ordenAlfa = true; } }
        GUI.backgroundColor = bg0;
        GUILayout.EndHorizontal();
        if (letraD != "") vista = vista.FindAll(x => SinAcento(x.nombre).StartsWith(letraD));
        if (ordenAlfa) vista.Sort((x, y) => string.CompareOrdinal(SinAcento(x.nombre), SinAcento(y.nombre)));
        scrollD = GUILayout.BeginScrollView(scrollD, false, true);
        if (filasD.Count == 0) GUILayout.Label("Esperando posicion del avion...", txt);
        float ancho = Tienda.PanelLateral.Ancho() - 24f;
        const float CELDA = 540f;
        int cols = Mathf.Max(1, (int)(ancho / CELDA));
        var cc = GUI.contentColor;
        for (int i = 0; i < vista.Count; i += cols)
        {
            GUILayout.BeginHorizontal();
            for (int j = i; j < i + cols && j < vista.Count; j++)
            {
                var f = vista[j];
                GUILayout.BeginHorizontal(GUILayout.Width(CELDA - 10f));
                GUI.contentColor = new Color(1f, 0.85f, 0.2f);
                GUILayout.Label(f.nombre, txt, GUILayout.Width(190));
                GUI.contentColor = new Color(0.7f, 1f, 0.7f);
                GUILayout.Label(f.km.ToString("F0") + " km", txt, GUILayout.Width(70));
                GUI.contentColor = cc;
                float p; preciosT.TryGetValue(f.nombre, out p);
                FilaPrecio(f.nombre, txt, btn, 24f, 70f);
                GUI.contentColor = cc;
                bool esDest = Carrera.ViajeNombre == f.nombre; var bgI = GUI.backgroundColor; if (esDest) GUI.backgroundColor = new Color(0.4f, 1f, 0.4f);
                if (GUILayout.Button(esDest ? "Yendo" : "Ir", btn, GUILayout.Width(50), GUILayout.Height(20))) { if (esDest) Carrera.CancelarViaje(); else Carrera.IniciarViaje(f.nombre, f.lat, f.lon, 600f, (int)p); }
                GUI.backgroundColor = bgI;
                GUILayout.EndHorizontal();
            }
            GUILayout.EndHorizontal();
        }
        GUI.contentColor = cc;
        GUILayout.EndScrollView();
    }

    void DibujarTorre(GUIStyle txt, GUIStyle btn, GUIStyle titulo)
    {
        GUILayout.Label("TORRE DE CONTROL   (aeropuertos mas cercanos; ILS = aros de aproximacion; Ir = viaje pago: aterrizas sobre su pista y cobras ese precio, que cambia solo segun tu avion; +/- sube o baja de a $1; mantenelo apretado para que vaya mas rapido)", titulo);
        scrollT = GUILayout.BeginScrollView(scrollT, false, true);
        if (filasT.Count == 0) GUILayout.Label("Esperando posicion del avion...", txt);
        for (int i = 0; i < filasT.Count; i++)
        {
            var f = filasT[i];
            GUILayout.BeginHorizontal(GUILayout.Width(Tablet.Compacto ? 455 : 760));
            GUILayout.Label(f.nombre, txt, GUILayout.Width(Tablet.Compacto ? 150 : 300));
            GUILayout.Label(f.km.ToString("F1") + " km", txt, GUILayout.Width(Tablet.Compacto ? 62 : 80));
            var antes = GUI.backgroundColor;
            bool hab = GUI.enabled;
            bool cp = Tablet.Compacto;
            if (!f.tienePista) { GUI.enabled = false; GUILayout.Button("Sin ILS", btn, GUILayout.Width(cp ? 56 : 80), GUILayout.Height(20)); GUI.enabled = hab; }
            else
            {
                if (f.ils) GUI.backgroundColor = new Color(0.5f, 1f, 0.5f);
                if (GUILayout.Button(f.ils ? "ILS ON" : "ILS", btn, GUILayout.Width(cp ? 56 : 80), GUILayout.Height(20))) { AlternarIlsTorre(f.nombre, !f.ils); f.ils = !f.ils; }
                GUI.backgroundColor = antes;
            }
            float p; preciosT.TryGetValue(f.nombre, out p);
            FilaPrecio(f.nombre, txt, btn, cp ? 22f : 26f, cp ? 46f : 90f);
            bool esDest2 = Carrera.ViajeNombre == f.nombre; var bgI2 = GUI.backgroundColor; if (esDest2) GUI.backgroundColor = new Color(0.4f, 1f, 0.4f);
            if (GUILayout.Button(esDest2 ? "Yendo" : "Ir", btn, GUILayout.Width(cp ? 40 : 50), GUILayout.Height(20))) { if (esDest2) Carrera.CancelarViaje(); else Carrera.IniciarViaje(f.nombre, f.lat, f.lon, 600f, (int)p); }
            GUI.backgroundColor = bgI2;   // viaje de un paso: aterrizar a menos de 600 m (RADIO_VOLVER_BASE)
            GUILayout.EndHorizontal();
        }
        GUILayout.EndScrollView();
    }

    void Update()
    {
        if (aeros.Count == 0) return;
        proxT -= Time.unscaledDeltaTime;
        if (proxT <= 0f) { proxT = 0.5f; RefrescarTorre(); }   // INTERVALO_TORRE = 0.5 (principal.gd:883)
        if (avion == null) avion = FindFirstObjectByType<AvionDeCarton>();
        if (cam == null) cam = Camera.main;
        if (avion == null || cam == null) return;

        var kb = Keyboard.current; var gp = Gamepad.current;
        bool pideIls = Controles.Flanco("activar_ils");

        Vector3 pp = avion.transform.position;
        bool hacerRayos = false;
        proxRayos -= Time.deltaTime;
        if (proxRayos <= 0f) { proxRayos = INTERVALO_LUCES; hacerRayos = true; }
        Vector3 pc = cam.transform.position;
        float mulNoche = factorNoche;   // Godot: emision * factor_noche_actual
        float fase = Time.time * 0.35f; fase -= Mathf.Floor(fase);
        // silueta del avion en pantalla (para que las luces de aproximacion que quedan detras de el no se vean por encima)
        float dPlano = Vector3.Distance(pc, pp); Vector3 sPlano = cam.WorldToScreenPoint(pp); bool tapaOn = sPlano.z > 0f && dPlano > 1f;
        float rPx = tapaOn ? avion.RadioModelo * 1.05f / dPlano * (Screen.height * 0.5f / Mathf.Tan(cam.fieldOfView * 0.5f * Mathf.Deg2Rad)) : 0f;

        Aero masCerca = null; float dMin = float.MaxValue;
        foreach (var a in aeros)
        {
            float d = Vector3.Distance(pp, a.raiz.transform.position);
            if (a.tienePista) { Vector3 v = a.raiz.transform.position - pp; Vector3 f = avion.transform.forward; f.y = 0f; v.y = 0f; float cosA = (f.sqrMagnitude > 0.01f && v.sqrMagnitude > 1f) ? Vector3.Dot(f.normalized, v.normalized) : 1f; float score = (d < 80000f && cosA > 0.5f) ? d / cosA : d * 100f; if (score < dMin) { dMin = score; masCerca = a; } }
            if (!a.tienePista) continue;
            bool activa = d < DISTANCIA_ACTIVA && factorNoche > 0.03f;   // de dia no se dibuja nada (como Godot: factor_noche <= 0.03 = oculto)
            if (activa && !a.lucesCreadas) CrearLuces(a);
            if (a.lucesCreadas && a.lucesRaiz.activeSelf != activa) a.lucesRaiz.SetActive(activa);
            if (!activa || !a.lucesCreadas) continue;

            bool rayos = hacerRayos && d < DISTANCIA_RAYOS;
            float sumaDesvio = 0f; int cuentaDesvio = 0;
            foreach (var l in a.luces)
            {
                // brillo: clasico_dorado = una luz viaja de punta a punta (0.35 ciclos/seg, pulso angosto 22%)
                float lum = 1f;
                if (l.umbral || l.fija) lum = 6f / 8f; else
                {
                    float dd = Mathf.Abs(l.u - fase); dd = Mathf.Min(dd, 1f - dd);
                    float pulso = Mathf.Clamp01(1f - dd / (0.22f * 0.5f));
                    lum = Mathf.Lerp(0.4f, 8f, pulso) / 8f;   // Godot: lerp(0.4, 8.0, brillo)
                }
                Color c = l.col * (lum * mulNoche); c.a = 1f;
                mpb.SetColor("_BaseColor", c); mpb.SetColor("_Color", c);
                l.r.SetPropertyBlock(mpb);

                // de cerca se achican (minimo 35%)
                float dc = Vector3.Distance(pc, l.t.position);
                float esc = 2f * RADIO_LUZ * Mathf.Lerp(ESCALA_MINIMA, 1f, Mathf.Clamp01(dc / DISTANCIA_ACHIQUE));
                if (l.fija || l.lejana) esc *= Mathf.Clamp(dc / 2500f, 1f, 8f);   // las luces fijas crecen con la distancia para verse bien desde el aire
                if (l.blanca || l.lejana || l.fija)   // a menos de 100 m del avion o de la camara desaparecen (centro, aproximacion y laterales fijas): para aterrizar se ve la pista
                {
                    bool oculta = (l.blanca || l.lejana) && (dc < 100f || Vector3.Distance(pp, l.t.position) < 100f);
                    // el avion va una capa POR ENCIMA de las luces: las que quedan detras de su silueta (mas lejos que el avion desde la camara) no se dibujan encima
                    if (!oculta && tapaOn && dc > dPlano) { Vector3 s = cam.WorldToScreenPoint(l.t.position); if (s.z > 0f && (new Vector2(s.x - sPlano.x, s.y - sPlano.y)).sqrMagnitude < rPx * rPx) oculta = true; }
                    if (l.r.enabled == oculta) l.r.enabled = !oculta;
                }
                l.t.localScale = new Vector3(esc, esc, esc);

                if (rayos)
                {
                    Vector3 o = l.t.position + a.raiz.transform.up * 300f;
                    RaycastHit h;
                    if (Physics.Raycast(o, -a.raiz.transform.up, out h, 600f, ~0, QueryTriggerInteraction.Ignore))
                    {
                        Vector3 objetivo = h.point + a.raiz.transform.up * ALTURA_LUCES_SOBRE_PISO;
                        float desvio = Vector3.Dot(objetivo - l.t.position, a.raiz.transform.up);
                        if (l.umbral) { sumaDesvio += desvio; cuentaDesvio++; }   // las de umbral miden cuanto se desvia el piso real de los datos
                        else if (Mathf.Abs(desvio) <= DESVIO_MAXIMO_RAYCAST) l.t.position += a.raiz.transform.up * desvio;   // si da algo raro, se descarta
                    }
                }
            }
            // el desvio promedio en las cabeceras corre TODO el aeropuerto (luces y aros del ILS) a la altura real del piso
            if (cuentaDesvio > 0)
            {
                float prom = sumaDesvio / cuentaDesvio;
                if (Mathf.Abs(prom) < 80f)
                {
                    a.dY += prom * 0.3f;
                    a.lucesRaiz.transform.localPosition = new Vector3(0f, a.dY, 0f);
                    if (a.ils != null) a.ils.transform.localPosition = new Vector3(0f, a.dY, 0f);
                }
            }
        }

        if (pideIls)
        {
            // como en Godot: un solo boton prende TODOS los ILS (los aros de todas las pistas) o los apaga todos
            ilsEncendido = !ilsEncendido;
            foreach (var a in aeros)
            {
                if (!a.tienePista) continue;
                if (ilsEncendido && a.ils == null) { CrearIls(a); a.ils.transform.localPosition = new Vector3(0f, a.dY, 0f); }
                if (a.ils != null) { a.ilsVisible = ilsEncendido; a.ils.SetActive(ilsEncendido); }
            }
            avisoIls = ilsEncendido ? "ILS: ENCENDIDO (todas las pistas)" : "ILS: apagado";
            avisoHasta = Time.time + 3f;
        }
    }

    // ---------- luces de pista ----------
    void CrearLuces(Aero a)
    {
        a.lucesCreadas = true;
        a.lucesRaiz = new GameObject("Luces"); a.lucesRaiz.transform.SetParent(a.raiz.transform, false);
        Vector2 eje = (a.c2 - a.c1).normalized;
        Vector2 lado = new Vector2(-eje.y, eje.x);
        int n = Mathf.FloorToInt(a.largo / ESPACIADO_LUCES);
        for (int i = 0; i <= n; i++)
        {
            float t = i * ESPACIADO_LUCES;
            float u = t / a.largo;
            for (int s = -1; s <= 1; s += 2) NuevaLuz(a, a.c1 + eje * t + lado * (s * a.ancho), Mathf.Lerp(a.a1, a.a2, u), u, COL_DORADO, false);
        }
        // luces de umbral: verdes fijas en cada punta exacta
        for (int s = -1; s <= 1; s += 2)
        {
            NuevaLuz(a, a.c1 + lado * (s * a.ancho), a.a1, 0f, COL_UMBRAL, true);
            NuevaLuz(a, a.c2 + lado * (s * a.ancho), a.a2, 1f, COL_UMBRAL, true);
        }
        // laterales fijas, bien visibles desde lejos (orientacion de la pista); desaparecen a menos de 100 m
        for (float t = 30f; t < a.largo; t += 120f)
            for (int s = -1; s <= 1; s += 2) { var lz = NuevaLuz(a, a.c1 + eje * t + lado * (s * a.ancho), Mathf.Lerp(a.a1, a.a2, t / a.largo), t / a.largo, new Color(1f, 0.95f, 0.8f), false, true); lz.lejana = true; }
        // ---- estilo Aeroparque: varias filas ----
        var COL_BLANCO = new Color(1f, 1f, 1f); var COL_ROJO = new Color(1f, 0.1f, 0.1f);
        // fila central blanca, cada 30 m
        int nc = Mathf.FloorToInt(a.largo / 30f);
        for (int i = 1; i < nc; i++) { float t = i * 30f; NuevaLuz(a, a.c1 + eje * t, Mathf.Lerp(a.a1, a.a2, t / a.largo), t / a.largo, COL_BLANCO, false, true); }
        // barra de umbral verde: de borde a borde de la pista, cada ~5 m, en las dos puntas
        int nb = Mathf.Max(2, Mathf.CeilToInt(2f * a.ancho / 5f));
        for (int k = 0; k < 2; k++)
        {
            Vector2 cab = k == 0 ? a.c1 : a.c2; float altc = k == 0 ? a.a1 : a.a2; Vector2 fuera = k == 0 ? -eje : eje;
            for (int i = 0; i <= nb; i++) NuevaLuz(a, cab + lado * (-a.ancho + 2f * a.ancho * i / nb), altc, k, COL_UMBRAL, false, true);
            // luces rojas de fin de pista, una fila al final (se ven desde el lado contrario)
            for (int i = 0; i <= nb; i += 2) NuevaLuz(a, cab - fuera * 3f + lado * (-a.ancho + 2f * a.ancho * i / nb), altc, k, COL_ROJO, false, true);
            // luces de aproximacion: eje blanco cada 30 m y barras cruzadas cada 90 m, hasta 600 m antes de cada cabecera
            for (int d = 30; d <= 600; d += 30)
            {
                Vector2 c = cab + fuera * d;
                NuevaLuz(a, c, altc, k, COL_BLANCO, false, true);
                if (d % 90 == 0) for (int w = -2; w <= 2; w++) if (w != 0) NuevaLuz(a, c + lado * (w * 3.5f), altc, k, COL_BLANCO, false, true);
            }
        }
    }

    Luz NuevaLuz(Aero a, Vector2 en, float alt, float u, Color col, bool umbral, bool fija = false)
    {
        var go = new GameObject("luz"); go.layer = DiaNoche.CAPA_MARCADORES; go.transform.SetParent(a.lucesRaiz.transform, false);
        go.transform.localPosition = new Vector3(en.x, LocalY(a, alt, en.x, en.y) + ALTURA_LUCES_SOBRE_PISO, en.y);
        go.AddComponent<MeshFilter>().sharedMesh = mallaEsfera;
        var r = go.AddComponent<MeshRenderer>();
        r.sharedMaterial = matLuz; r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; r.receiveShadows = false;
        var luz = new Luz { t = go.transform, r = r, u = u, col = col, umbral = umbral, fija = fija, blanca = fija && col.r > 0.9f && col.g > 0.9f && col.b > 0.9f };
        a.luces.Add(luz); return luz;
    }

    // ---------- ILS: 11 aros por direccion ----------
    void CrearIls(Aero a)
    {
        a.ils = new GameObject("ILS"); a.ils.transform.SetParent(a.raiz.transform, false);
        Vector2 eje = (a.c2 - a.c1).normalized;   // de cab1 hacia cab2
        for (int k = 0; k < 2; k++)
        {
            Vector2 cab = k == 0 ? a.c1 : a.c2; float altCab = k == 0 ? a.a1 : a.a2;
            Vector2 haciaAfuera = k == 0 ? -eje : eje;   // el corredor se extiende mas alla de la cabecera, solo en horizontal
            Color col = a.multiPista ? (a.indicePista == 0 ? COL_ARO1 : COL_ARO2) : (k == 0 ? COL_ARO1 : COL_ARO2);   // con 2 pistas (Ezeiza): una toda amarilla, la otra toda verde flúor
            var mat = new Material(matLuz); mat.SetColor("_BaseColor", col); mat.SetColor("_Color", col);
            foreach (float d in DIST_ARO)
            {
                Vector2 p = cab + haciaAfuera * d;
                float y = (altCab - (float)a.h0) + d * TANGENTE_PLANEO;   // como Godot: sobre el plano tangente de la cabecera (sin caida por curvatura)
                var go = new GameObject("aro"); go.layer = DiaNoche.CAPA_MARCADORES; go.transform.SetParent(a.ils.transform, false);
                go.transform.localPosition = new Vector3(p.x, y, p.y);
                go.transform.localRotation = Quaternion.LookRotation(new Vector3(haciaAfuera.x, 0f, haciaAfuera.y), Vector3.up);   // parado, mirando a lo largo de la pista
                go.AddComponent<MeshFilter>().sharedMesh = mallaAro;
                var r = go.AddComponent<MeshRenderer>(); r.sharedMaterial = mat; r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; r.receiveShadows = false;
            }
        }
        a.ils.SetActive(false);
    }

    // ---------- carteles de nombre (beacon) ----------
    void OnGUI()
    {
        if (Event.current.type != EventType.Repaint) return;
        if (avion == null || cam == null || aeros.Count == 0) return;
        if (sBeacon == null) { sBeacon = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold, wordWrap = false }; }

        float parpadeo = 0.775f + 0.225f * Mathf.Sin(Time.time * 3f);
        float focal = (Screen.height * 0.5f) / Mathf.Tan(cam.fieldOfView * 0.5f * Mathf.Deg2Rad);
        Vector3 pp = avion.transform.position;
        Color pc = GUI.color;
        foreach (var a in aeros)
        {
            Transform t = a.raiz.transform;
            if (a.sinCartel) continue;
            float alt = Vector3.Dot(pp - t.position, t.up);   // altura del avion sobre ESE aeropuerto
            if (alt < ALTURA_BEACON_OCULTO) continue;
            float fundido = Mathf.Clamp01((alt - ALTURA_BEACON_OCULTO) / (ALTURA_BEACON_VISIBLE - ALTURA_BEACON_OCULTO));
            Vector3 mundo = t.position + t.up * 300f;
            Vector3 sp = cam.WorldToScreenPoint(mundo);
            if (sp.z <= 0f) continue;
            float px = 180f * focal / sp.z;   // letra de ~180 m de alto (pixel_size 1.4 x fuente 130 de Godot)
            if (px < 6f) continue;
            px = Mathf.Min(px, 140f);
            sBeacon.fontSize = Mathf.RoundToInt(px);
            string txt = a.nombre.ToUpperInvariant();
            float ancho = px * 0.75f * txt.Length + 20f, alto = px * 1.4f;
            var rect = new Rect(sp.x - ancho * 0.5f, Screen.height - sp.y - alto * 0.5f, ancho, alto);
            if (rect.xMax < 0 || rect.xMin > Screen.width || rect.yMax < 0 || rect.yMin > Screen.height) continue;
            float alfa = fundido * parpadeo;
            float o = Mathf.Max(1f, px * 0.06f);
            GUI.color = new Color(0.25f, 0.15f, 0f, alfa);   // contorno marron oscuro
            for (int dx = -1; dx <= 1; dx++) for (int dy = -1; dy <= 1; dy++) if (dx != 0 || dy != 0) GUI.Label(new Rect(rect.x + dx * o, rect.y + dy * o, rect.width, rect.height), txt, sBeacon);
            GUI.color = new Color(1f, 0.82f, 0.25f, alfa);   // relleno amarillo
            GUI.Label(rect, txt, sBeacon);
        }
        GUI.color = pc;
        if (Time.time < avisoHasta)
        {
            GUI.color = Color.white;
            GUI.Label(new Rect(20, 300, 600, 26), avisoIls + "   (Y / tecla I)  luces: tecla N dia/noche");
        }
        GUI.color = pc;
    }

    // ---------- mallas ----------
    static Mesh MallaEsfera()
    {
        var tmp = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        var m = tmp.GetComponent<MeshFilter>().sharedMesh;
        Destroy(tmp);
        return m;
    }

    static Mesh MallaToroide(float R, float r, int seg, int lados)
    {
        var v = new List<Vector3>(); var tri = new List<int>();
        for (int i = 0; i <= seg; i++)
        {
            float a = (float)i / seg * Mathf.PI * 2f;
            Vector3 centro = new Vector3(Mathf.Cos(a) * R, Mathf.Sin(a) * R, 0f);   // el aro esta en el plano XY, su eje es Z
            Vector3 radial = new Vector3(Mathf.Cos(a), Mathf.Sin(a), 0f);
            for (int j = 0; j <= lados; j++)
            {
                float b = (float)j / lados * Mathf.PI * 2f;
                v.Add(centro + radial * (Mathf.Cos(b) * r) + new Vector3(0f, 0f, Mathf.Sin(b) * r));
            }
        }
        int fila = lados + 1;
        for (int i = 0; i < seg; i++)
            for (int j = 0; j < lados; j++)
            {
                int p = i * fila + j;
                tri.Add(p); tri.Add(p + fila); tri.Add(p + 1);
                tri.Add(p + 1); tri.Add(p + fila); tri.Add(p + fila + 1);
            }
        var m = new Mesh { indexFormat = UnityEngine.Rendering.IndexFormat.UInt32 };
        m.SetVertices(v); m.SetTriangles(tri, 0); m.RecalculateNormals(); m.RecalculateBounds();
        return m;
    }
}
