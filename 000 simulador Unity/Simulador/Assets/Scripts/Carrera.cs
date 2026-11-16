// Perfil de jugador + dinero + viajes pagos (motor de pasos reducido al tipo "llegar_punto"). Portado de perfil_jugador.gd y principal.gd
// (RESPUESTA_DINERO_Y_MISIONES.md). Pestana "Carrera" del panel de abajo + cajita DINERO en el HUD. Nada flotante.
// Perfiles: carpeta "perfiles" dentro de "000 simulador Unity" (un JSON por jugador, se guarda en cada cambio).
using UnityEngine;
using System.Collections.Generic;
using System.Globalization;

public class Carrera : MonoBehaviour
{
    [System.Serializable] class PerfilJ { public string nombre; public int plata; public float horas_vuelo; public List<string> compras = new List<string>(); public List<string> hechas = new List<string>(); }

    static Carrera inst;
    PerfilJ perfil;
    string nuevoNombre = ""; Vector2 scroll; bool yoEscribo;

    // viaje activo (un solo paso "llegar_punto": aterrizar a menos de `radio` metros del punto)
    bool viajeActivo; string viajeReq = "cualquiera", viajeTitulo = ""; string viajeNombre = ""; double viajeLat, viajeLon; float viajeRadio = 600f; int viajePago; string viajeTextoFinal = "";

    static string Carpeta { get { return System.IO.Path.GetFullPath(System.IO.Path.Combine(Application.dataPath, "..", "..")); } }
    static string CarpetaPerfiles { get { return System.IO.Path.Combine(Carpeta, "perfiles"); } }
    static string RutaUltimo { get { return System.IO.Path.Combine(CarpetaPerfiles, "ultimo_perfil.txt"); } }
    static string RutaPerfil(string n) { return System.IO.Path.Combine(CarpetaPerfiles, n + ".json"); }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Crear() { new GameObject("Carrera").AddComponent<Carrera>(); }

    void Start() { inst = this; AutocargarUltimo(); }

    // ------------------------------------------------------------ perfiles
    static string Limpio(string n) { foreach (char c in System.IO.Path.GetInvalidFileNameChars()) n = n.Replace(c.ToString(), ""); return n.Trim(); }

    List<string> ListarPerfiles()
    {
        var l = new List<string>();
        try { if (System.IO.Directory.Exists(CarpetaPerfiles)) foreach (var f in System.IO.Directory.GetFiles(CarpetaPerfiles, "*.json")) l.Add(System.IO.Path.GetFileNameWithoutExtension(f)); }
        catch { }
        l.Sort(); return l;
    }

    bool Cargar(string nombre)
    {
        try
        {
            if (!System.IO.File.Exists(RutaPerfil(nombre))) return false;
            var p = JsonUtility.FromJson<PerfilJ>(System.IO.File.ReadAllText(RutaPerfil(nombre)));
            if (p == null) return false;
            if (string.IsNullOrEmpty(p.nombre)) p.nombre = nombre;
            if (p.compras == null) p.compras = new List<string>();
            if (p.hechas == null) p.hechas = new List<string>();
            perfil = p; plataMostrada = p.plata; GuardarUltimo(); return true;
        }
        catch { return false; }
    }

    void Crear(string nombre) { perfil = new PerfilJ { nombre = nombre, plata = 0, horas_vuelo = 0f }; Guardar(); GuardarUltimo(); }

    void Guardar()
    {
        if (perfil == null) return;
        try { System.IO.Directory.CreateDirectory(CarpetaPerfiles); System.IO.File.WriteAllText(RutaPerfil(perfil.nombre), JsonUtility.ToJson(perfil, true)); }
        catch (System.Exception e) { Debug.LogWarning("[Carrera] no pude guardar el perfil: " + e.Message); }
    }
    void GuardarUltimo() { try { System.IO.Directory.CreateDirectory(CarpetaPerfiles); System.IO.File.WriteAllText(RutaUltimo, perfil.nombre); } catch { } }

    // como autocargar_ultimo_perfil: el ultimo usado; si no, el primero que haya; si no hay ninguno, "Piloto"
    void AutocargarUltimo()
    {
        try
        {
            if (System.IO.File.Exists(RutaUltimo) && Cargar(System.IO.File.ReadAllText(RutaUltimo).Trim())) return;
        }
        catch { }
        var l = ListarPerfiles();
        if (l.Count > 0 && Cargar(l[0])) return;
        Crear("Piloto");
    }

    // ------------------------------------------------------------ dinero
    public static int Plata { get { return (inst != null && inst.perfil != null) ? inst.perfil.plata : 0; } }
    public static string NombrePerfil { get { return (inst != null && inst.perfil != null) ? inst.perfil.nombre : "-"; } }
    public static void Modificar(int delta)
    {
        if (inst == null || inst.perfil == null) return;
        inst.perfil.plata = Mathf.Max(0, inst.perfil.plata + delta); inst.plataMostrada = inst.perfil.plata; inst.Guardar();
    }

    // ---- cobro con efecto: suena la contadora de billetes y el numero del HUD sube de a poco
    float plataMostrada; AudioSource audioCaja; AudioClip clipCaja;
    public static System.Action AlLlegar;   // la torre lo usa para pasar el acumulado del viaje a la cuenta
    public static void Pagar(int monto, bool sonido = true)
    {
        if (inst == null || inst.perfil == null || monto <= 0) return;
        inst.perfil.plata += monto; inst.Guardar();
        if (!sonido) { inst.velDinero = Mathf.Max(1f, (inst.perfil.plata - inst.plataMostrada) / 1.2f); return; }
        if (inst.audioCaja == null) { inst.audioCaja = inst.gameObject.AddComponent<AudioSource>(); inst.audioCaja.spatialBlend = 0f; inst.audioCaja.playOnAwake = false; inst.audioCaja.volume = 0.8f; }
        if (inst.clipCaja == null) { inst.clipCaja = Resources.Load<AudioClip>("sonidos/contador_billetes"); if (inst.clipCaja == null) inst.clipCaja = CrearClipCaja(); }
        inst.audioCaja.Stop(); inst.audioCaja.clip = inst.clipCaja; inst.audioCaja.Play();
        inst.velDinero = Mathf.Max(1f, (inst.perfil.plata - inst.plataMostrada) / 2.4f);
    }
    float velDinero; int pagoPend; float pagoPendHasta;
    // sonido hecho a mano (sin archivos): ~2.4 s de "rrrrrr" de contadora de billetes (clicks metalicos que van frenando) y un "cling" de caja al final
    static AudioClip CrearClipCaja()
    {
        int sr = 44100; float dur = 2.8f; var d = new float[(int)(sr * dur)];
        var rnd = new System.Random(7);
        float t = 0f;
        while (t < 2.4f)
        {
            int i0 = (int)(t * sr);
            float f = 2800f + (float)rnd.NextDouble() * 900f;
            for (int k = 0; k < 520 && i0 + k < d.Length; k++)
            {
                float env = Mathf.Exp(-k / 70f);
                d[i0 + k] += (Mathf.Sin(2f * Mathf.PI * f * k / sr) * 0.5f + ((float)rnd.NextDouble() * 2f - 1f) * 0.5f) * env * 0.45f;
            }
            t += Mathf.Lerp(0.032f, 0.075f, Mathf.Clamp01(t / 2.4f));   // los clicks se van espaciando: la maquina frena
        }
        int ic = (int)(2.35f * sr);
        for (int k = 0; ic + k < d.Length; k++)
        {
            float env = Mathf.Exp(-k / 9000f);
            d[ic + k] += (Mathf.Sin(2f * Mathf.PI * 2349f * k / sr) + 0.6f * Mathf.Sin(2f * Mathf.PI * 3136f * k / sr) + 0.3f * Mathf.Sin(2f * Mathf.PI * 4698f * k / sr)) * env * 0.28f;
        }
        for (int k = 0; k < d.Length; k++) d[k] = Mathf.Clamp(d[k], -1f, 1f);
        var c = AudioClip.Create("caja", d.Length, 1, sr, false); c.SetData(d, 0); return c;
    }

    // ---- shopping: lo comprado se guarda en el perfil
    public static bool Tiene(string id) { return inst != null && inst.perfil != null && inst.perfil.compras != null && inst.perfil.compras.Contains(id); }
    public static IEnumerable<string> Compras { get { if (inst != null && inst.perfil != null && inst.perfil.compras != null) return inst.perfil.compras; return new List<string>(); } }
    public static bool Comprar(string id, int precio)
    {
        if (inst == null || inst.perfil == null || Tiene(id) || inst.perfil.plata < precio) return false;
        inst.perfil.plata -= precio; inst.perfil.compras.Add(id); inst.Guardar(); return true;
    }

    // principal.gd:7648: segun el avion elegido (agrupado "a ojo" por nombre, como en Godot)
    public static float MultiplicadorAvion()
    {
        string n = (AvionDeCarton.nombreAvion ?? "").ToLowerInvariant();
        if (n.Contains("boeing") || n.Contains("a380") || n.Contains("777")) return 3f;
        if (n.Contains("jet privado") || n.Contains("pilatus") || n.Contains("miljet")) return 1.5f;
        if (n.Contains("cessna") || n.Contains("piper")) return 1f;
        return 1.2f;
    }

    // ------------------------------------------------------------ viajes (motor de pasos: solo "llegar_punto")
    public static void IniciarViaje(string nombre, double lat, double lon, float radio, int precioBase)
    {
        if (inst == null) return;
        inst.rec = null; inst.viajeActivo = true; inst.viajeNombre = nombre; inst.viajeLat = lat; inst.viajeLon = lon; inst.viajeRadio = radio;
        inst.viajePago = Mathf.RoundToInt(precioBase * MultiplicadorAvion()); inst.viajeReq = "cualquiera"; inst.viajeTitulo = "";
        inst.viajeTextoFinal = "Llegaste a " + nombre + "!";
        Mapeo.Cartel("VIAJE: " + nombre + "\nSalis desde donde estas. Pago: $" + inst.viajePago, 3.5f);
    }

    static double Haversine(double la1, double lo1, double la2, double lo2)
    {
        double r = 6371000.0, dLa = (la2 - la1) * System.Math.PI / 180.0, dLo = (lo2 - lo1) * System.Math.PI / 180.0;
        double h = System.Math.Sin(dLa / 2) * System.Math.Sin(dLa / 2) + System.Math.Cos(la1 * System.Math.PI / 180.0) * System.Math.Cos(la2 * System.Math.PI / 180.0) * System.Math.Sin(dLo / 2) * System.Math.Sin(dLo / 2);
        return 2 * r * System.Math.Asin(System.Math.Min(1.0, System.Math.Sqrt(h)));
    }

    public static bool HayViaje { get { return inst != null && inst.viajeActivo; } }
    // para la torre: destino del viaje y rumbo hacia el
    public static bool Destino(out string nombre, out double lat, out double lon) { nombre = ""; lat = lon = 0; if (inst == null || !inst.viajeActivo) return false; nombre = inst.viajeNombre; lat = inst.viajeLat; lon = inst.viajeLon; return true; }
    public static float DestinoRumbo(double lat, double lon, float porDefecto) { if (inst == null || !inst.viajeActivo) return porDefecto; double n = inst.viajeLat - lat, e = (inst.viajeLon - lon) * System.Math.Cos(lat * System.Math.PI / 180.0); float r = (float)(System.Math.Atan2(e, n) * 180.0 / System.Math.PI); return r < 0 ? r + 360f : r; }
    public static string ViajeNombre { get { return inst != null && inst.viajeActivo ? inst.viajeNombre : ""; } }
    public static void CancelarViaje() { if (inst != null && inst.viajeActivo) inst.Abortar(); }
    GUIStyle sDest;
    // cartelito arriba: "TU DESTINO: X - N km (pago $Y)" mientras hay un viaje elegido; ese es el que paga al aterrizar sobre su pista
    void OnGUI()
    {
        if (!viajeActivo || avion == null || AvionDeCarton.enAuto) return;
        if (sDest == null) { sDest = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontSize = 14, fontStyle = FontStyle.Bold }; sDest.normal.textColor = new Color(0.6f, 1f, 0.6f); }
        string km = distViaje >= 0f ? ((distViaje / 1000f).ToString("F1") + " km") : "...";
        var r = new Rect(Screen.width * 0.5f - 260f, 34f, 520f, 22f);
        var tapaD = Disposicion.Bloque("destino", "CARTEL DE DESTINO", new Rect(r.x, r.y, r.width, 110f));
        GUI.Box(r, "");
        GUI.Label(r, (viajeTitulo != "" ? viajeTitulo + "  >  " : "TU DESTINO: ") + viajeNombre + "  -  quedan " + km + "  -  pago $" + viajePago, sDest);

        // guia GPS: latitud/longitud de donde estas y del destino + triangulos verdes que dicen para que lado girar
        double lon, lat; float alt, rumbo;
        if (!MapaAuto.PosActiva(out lon, out lat, out alt, out rumbo)) { tapaD.Dispose(); return; }
        var rg = new Rect(r.x, r.yMax + 2f, r.width, 18f);
        var st = new GUIStyle(sDest) { fontSize = 12, fontStyle = FontStyle.Normal };
        st.normal.textColor = Color.white;
        GUI.Label(rg, "Estas en  lat " + lat.ToString("F3", CultureInfo.InvariantCulture) + "  lon " + lon.ToString("F3", CultureInfo.InvariantCulture) + "     Destino  lat " + viajeLat.ToString("F3", CultureInfo.InvariantCulture) + "  lon " + viajeLon.ToString("F3", CultureInfo.InvariantCulture), st);
        double dN = (viajeLat - lat), dE = (viajeLon - lon) * System.Math.Cos(lat * System.Math.PI / 180.0);
        float rumboDest = (float)(System.Math.Atan2(dE, dN) * 180.0 / System.Math.PI);
        float dif = Mathf.DeltaAngle(rumbo, rumboDest);   // + = girar a la derecha, - = a la izquierda
        if (texTri == null) texTri = CrearTriangulo();
        float cx = Screen.width * 0.5f, cy = rg.yMax + 22f;
        bool izq = dif < -8f, der = dif > 8f, ok = !izq && !der;
        var col0 = GUI.color;
        int n = Mathf.Clamp(Mathf.CeilToInt(Mathf.Abs(dif) / 30f), 1, 4);   // mas triangulos = mas lejos de la direccion correcta
        for (int i = 0; i < 4; i++)
        {
            bool enc = i < n;
            GUI.color = new Color(0.2f, 1f, 0.3f, (izq && enc) ? 1f : 0.15f);
            var mat0 = GUI.matrix; GUIUtility.RotateAroundPivot(180f, new Vector2(cx - 40f - i * 26f, cy));
            GUI.DrawTexture(new Rect(cx - 52f - i * 26f, cy - 12f, 24f, 24f), texTri); GUI.matrix = mat0;
            GUI.color = new Color(0.2f, 1f, 0.3f, (der && enc) ? 1f : 0.15f);
            GUI.DrawTexture(new Rect(cx + 28f + i * 26f, cy - 12f, 24f, 24f), texTri);
        }
        if (ok) { GUI.color = new Color(0.2f, 1f, 0.3f, 1f); GUI.Label(new Rect(cx - 40f, cy - 10f, 80f, 20f), "RUMBO OK", st); }
        GUI.color = col0;
        tapaD.Dispose();
    }

    Texture2D texTri;
    static Texture2D CrearTriangulo()
    {
        var t = new Texture2D(32, 32, TextureFormat.RGBA32, false);
        for (int y = 0; y < 32; y++) for (int x = 0; x < 32; x++)
        {
            float lim = (1f - x / 31f) * 16f;   // apunta a la derecha
            t.SetPixel(x, y, Mathf.Abs(y - 15.5f) <= lim ? Color.white : Color.clear);
        }
        t.Apply(); return t;
    }

    AvionDeCarton avion;
    float distViaje = -1f;    void Update()
    {
        // el numero del HUD sube de a poco mientras suena la contadora
        if (pagoPend > 0 && Time.unscaledTime >= pagoPendHasta) { int pp = pagoPend; pagoPend = 0; Pagar(pp); }
        if (perfil != null && plataMostrada < perfil.plata) plataMostrada = Mathf.Min(perfil.plata, plataMostrada + velDinero * Time.deltaTime);
        else if (perfil != null && plataMostrada > perfil.plata) plataMostrada = perfil.plata;
        if (!viajeActivo) return;
        if (avion == null) avion = FindFirstObjectByType<AvionDeCarton>();
        double lon, lat; float alt, rumbo;
        if (avion == null || AvionDeCarton.enAuto || !MapaAuto.PosActiva(out lon, out lat, out alt, out rumbo)) return;
        distViaje = (float)Haversine(lat, lon, viajeLat, viajeLon);
        if (rec != null) { ActualizarMision(); return; }
        // principal.gd:7834: el paso se cumple SOLO si esta en tierra y a menos del radio (no alcanza con pasar cerca)
        // la pista mide ~2 km: el toque puede ser a 1 km del "centro". Se cuenta tambien la distancia a cada cabecera (el ILS no interviene: es solo visual)
        // se cobra al tocar tierra SOBRE LA PISTA trackeada del destino (sin radio de 600 m); si ese destino no tiene pista trackeada, vale el radio
        int sp = Aeropuertos.SobrePista(viajeNombre, avion.transform.position);
        bool llego = sp == 1 || (sp == -1 && distViaje < viajeRadio);
        if (avion.enTierra && llego)
        {
            viajeActivo = false;
            if (!Cumple(viajeReq)) { Mapeo.Cartel("No cobraste: esta mision pedia " + NombreReq(viajeReq) + "\ny volaste con otro avion.", 5f); return; }
            int pago = viajePago;
            if (pago > 0) { pagoPend = pago; pagoPendHasta = Time.unscaledTime + 6.5f; }   // primero se oye el frenado; unos segundos despues, el pago
            Mapeo.Cartel(viajeTextoFinal + (pago > 0 ? "\n+$" + pago : ""), 4f);
            if (AlLlegar != null) AlLlegar(); Torre.NivelCompleto(6.5f);
        }
    }

    // ------------------------------------------------------------ interfaz
    // cajita del HUD (la dibuja AvionDeCarton junto a ALTITUD/VELOCIDAD/FPS)
    public const int PESOS_POR_DOLAR = 1560;
    public static string Pesos(long dolares) { return "$" + (dolares * PESOS_POR_DOLAR).ToString("N0", new System.Globalization.CultureInfo("es-AR")); }
    public static string TextoPesos { get { return Pesos(inst != null ? Mathf.RoundToInt(inst.plataMostrada) : Plata); } }
    public static string TextoDinero { get { return "$" + (inst != null ? Mathf.RoundToInt(inst.plataMostrada) : Plata); } }

    // se llama cuando se sale de la pestana: libera las teclas si se estaba escribiendo un nombre
    public static void SalioDePestana() { if (inst != null && inst.yoEscribo) { inst.yoEscribo = false; Controles.escribiendo = false; GUIUtility.keyboardControl = 0; } }

    public static void PestanaCarrera(GUIStyle txt, GUIStyle btn, GUIStyle titulo) { if (inst != null) inst.Dibujar(txt, btn, titulo); }

    void DibujarPerfil(GUIStyle txt, GUIStyle btn, GUIStyle titulo)
    {
        GUILayout.BeginHorizontal();
        GUILayout.Label("Piloto: " + NombrePerfil, txt, GUILayout.Width(220));
        GUILayout.Label("Dinero:  $" + Plata, txt, GUILayout.Width(120));
        if (GUILayout.Button("-25", btn, GUILayout.Width(44), GUILayout.Height(20))) Modificar(-25);
        if (GUILayout.Button("+25", btn, GUILayout.Width(44), GUILayout.Height(20))) Modificar(25);
        GUILayout.Label("   (ajuste a mano, por si falta o sobra plata)", txt);
        GUILayout.EndHorizontal();

        if (viajeActivo)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label("Viaje: " + viajeNombre + "   pago $" + viajePago + (distViaje >= 0f ? "   distancia " + (distViaje / 1000f).ToString("F1") + " km   (cobras al ATERRIZAR a menos de " + (int)viajeRadio + " m)" : ""), txt);
            if (GUILayout.Button("Cancelar viaje", btn, GUILayout.Width(110), GUILayout.Height(20))) { viajeActivo = false; Mapeo.Cartel("Viaje cancelado", 1.5f); }
            GUILayout.EndHorizontal();
        }
        else GUILayout.Label("Sin viaje activo. Elegi una mision de las tarjetas de arriba, o un destino en la pestana Destinos.", txt);

        // perfiles
        GUILayout.BeginHorizontal();
        GUILayout.Label("Perfiles:", txt, GUILayout.Width(60));
        foreach (var n in ListarPerfiles())
        {
            var antes = GUI.backgroundColor;
            if (perfil != null && n == perfil.nombre) GUI.backgroundColor = Color.green;
            if (GUILayout.Button(n, btn, GUILayout.Height(20))) Cargar(n);
            GUI.backgroundColor = antes;
        }
        GUILayout.EndHorizontal();
        GUILayout.BeginHorizontal();
        GUILayout.Label("Nuevo perfil:", txt, GUILayout.Width(90));
        GUI.SetNextControlName("nuevoPerfil");
        nuevoNombre = GUILayout.TextField(nuevoNombre, 30, GUILayout.Width(200), GUILayout.Height(22));
        bool enfocado = GUI.GetNameOfFocusedControl() == "nuevoPerfil";
        if (Event.current.type == EventType.Layout) { yoEscribo = enfocado; Controles.escribiendo = enfocado || Mapeo.PanelLugarAbierto; }   // mientras escribis, el avion no se mueve
        if (GUILayout.Button("Crear / elegir", btn, GUILayout.Width(100), GUILayout.Height(22)) && Limpio(nuevoNombre) != "")
        {
            string n = Limpio(nuevoNombre);
            if (!Cargar(n)) Crear(n);   // si ya existe se carga, no se pisa
            nuevoNombre = "";
        }
        GUILayout.EndHorizontal();
    }

    // ------------------------------------------------------------ MISIONES (normales y recorridos con escalas; catalogo en Misiones.cs)
    class Parada { public string nombre, llegada; public double lat, lon; public bool aterriza = true; }
    class Item { public Misiones.Def d; public List<Parada> ps = new List<Parada>(); public int ajuste; }
    List<Item> items = new List<Item>(); int sub; float proxIntento; System.Random rndM;
    Item act; int tramoIdx, pagoFinal, pagoEsc;
    Misiones.Def rec { get { return act != null ? act.d : null; } set { if (value == null) act = null; } }
    const int MULTA_ABORTAR = 75;

    public static string CategoriaAvion()
    {
        string n = (AvionDeCarton.nombreAvion ?? "").ToLowerInvariant();
        if (AvionDeCarton.EsHeli) return "heli";
        if (n.Contains("boeing") || n.Contains("a380") || n.Contains("777")) return "pesado";
        if (n.Contains("jet privado") || n.Contains("pilatus") || n.Contains("miljet")) return "jet";
        if (n.Contains("cessna") || n.Contains("piper")) return "liviano";
        return "otro";
    }
    public static bool Cumple(string req) { return req == "cualquiera" || CategoriaAvion() == req; }
    public static string NombreReq(string req)
    {
        switch (req) { case "jet": return "un JET (Jet privado, Miljet o Pilatus)"; case "pesado": return "un avion GRANDE (Boeing, A380 o 777)"; case "liviano": return "una AVIONETA (Cessna)"; default: return "cualquier avion"; }
    }

    // ---- armado del catalogo: resuelve los nombres contra los aeropuertos trackeados (las que no se puedan resolver se saltean)
    void ArmarItems()
    {
        string nm; double la, lo;
        if (!Aeropuertos.BuscarFrag("Aeroparque", out nm, out la, out lo)) return;   // los aeropuertos todavia no cargaron
        items.Clear();
        foreach (var d in Misiones.Todas())
        {
            var it = new Item { d = d }; bool ok = true;
            if (!d.recorrido)
            {
                if (Aeropuertos.BuscarFrag(d.dest, out nm, out la, out lo)) it.ps.Add(new Parada { nombre = nm, lat = la, lon = lo, llegada = d.titulo }); else ok = false;
            }
            else foreach (var s in d.paradas)
            {
                var c = s.Split('|');
                if (s.StartsWith("@") && c.Length >= 4)
                {
                    double a, b; double.TryParse(c[1], NumberStyles.Float, CultureInfo.InvariantCulture, out a); double.TryParse(c[2], NumberStyles.Float, CultureInfo.InvariantCulture, out b);
                    it.ps.Add(new Parada { nombre = c[0].Substring(1), lat = a, lon = b, aterriza = false, llegada = c[3] });
                }
                else if (c.Length >= 2 && Aeropuertos.BuscarFrag(c[0], out nm, out la, out lo)) it.ps.Add(new Parada { nombre = nm, lat = la, lon = lo, llegada = c[1] });
                else { ok = false; Debug.LogWarning("[Misiones] no encuentro el aeropuerto '" + c[0] + "' (mision " + d.id + ")"); }
            }
            if (ok && it.ps.Count > 0) items.Add(it);
        }
        Mezclar();
    }
    void Mezclar()
    {
        if (rndM == null) rndM = new System.Random(System.Environment.TickCount);
        for (int i = items.Count - 1; i > 0; i--) { int j = rndM.Next(i + 1); var t = items[i]; items[i] = items[j]; items[j] = t; }
    }
    bool Hecha(Item it) { return perfil != null && perfil.hechas != null && perfil.hechas.Contains(it.d.id); }

    // ---- numeros en vivo (siempre desde donde estas parado, asi la tarjeta no miente)
    bool PosAvion(out double lat, out double lon) { float alt, rumbo; return MapaAuto.PosActiva(out lon, out lat, out alt, out rumbo); }
    double KmHasta(Parada p) { double la, lo; return PosAvion(out la, out lo) ? Haversine(la, lo, p.lat, p.lon) / 1000.0 : 0.0; }
    double KmRuta(Item it)   // entre escalas (sin contar el tramo hasta la primera)
    {
        double k = 0; for (int i = 1; i < it.ps.Count; i++) k += Haversine(it.ps[i - 1].lat, it.ps[i - 1].lon, it.ps[i].lat, it.ps[i].lon) / 1000.0; return k;
    }
    int PagoNormal(Item it) { return Mathf.Max(10, Mathf.RoundToInt((float)(it.d.bas + KmHasta(it.ps[0]) * it.d.tarifa) / 10f) * 10 + it.ajuste); }
    int BonoRec(Item it) { return Mathf.Max(50, it.d.bono + it.ajuste); }
    int PagoEscala(Item it) { return Mathf.Max(10, Mathf.RoundToInt(BonoRec(it) * 0.12f / 10f) * 10); }
    int TotalRec(Item it) { return PagoEscala(it) * (it.ps.Count - 1) + BonoRec(it); }

    static string Hora(double min) { int m = Mathf.Max(1, Mathf.RoundToInt((float)min)); return m >= 60 ? (m / 60) + " h " + (m % 60).ToString("00") + " min" : m + " min"; }
    string Tiempos(Item it, double kmTotal)
    {
        int aterrizajes = 0; foreach (var p in it.ps) if (p.aterriza) aterrizajes++;
        double extra = 2.0 * aterrizajes;
        string c = "Cessna ~" + Hora(kmTotal / 230.0 * 60.0 + extra), j = "Jet ~" + Hora(kmTotal / 1100.0 * 60.0 + extra), g = "Avion grande ~" + Hora(kmTotal / 1100.0 * 60.0 + extra);
        switch (it.d.req) { case "liviano": return c; case "jet": return j; case "pesado": return g; default: return c + "   |   " + j + "   |   " + g; }
    }

    // ---- la mision en curso
    void SetParada()
    {
        var p = act.ps[tramoIdx];
        viajeNombre = p.nombre; viajeLat = p.lat; viajeLon = p.lon; viajeRadio = 600f; distViaje = -1f;
        viajeTitulo = act.d.recorrido ? act.d.titulo + " [" + (tramoIdx + 1) + " de " + act.ps.Count + "]" : act.d.titulo;
        int quedan = act.ps.Count - 1 - tramoIdx;
        viajePago = act.d.recorrido ? pagoEsc * quedan + pagoFinal : pagoFinal;
    }
    void IniciarMision(Item it)
    {
        act = it; tramoIdx = 0; viajeActivo = true; viajeReq = it.d.req;
        pagoFinal = it.d.recorrido ? BonoRec(it) : PagoNormal(it); pagoEsc = it.d.recorrido ? PagoEscala(it) : 0;
        viajeTextoFinal = "";
        SetParada();
        if (it.d.recorrido)
        {
            string r = ""; for (int i = 0; i < it.ps.Count; i++) r += (i + 1) + ") " + it.ps[i].nombre + (i < it.ps.Count - 1 ? "\n" : "");
            Mapeo.Cartel("RECORRIDO: " + it.d.titulo + "\n" + r + "\nPor escala $" + pagoEsc + "  -  Bono final $" + pagoFinal, 6f);
        }
        else Mapeo.Cartel("MISION: " + it.d.titulo + "\nDestino: " + viajeNombre + "   Pago: $" + pagoFinal, 4f);
    }
    void Abortar()
    {
        if (!viajeActivo) return;
        bool esRec = act != null && act.d.recorrido;
        viajeActivo = false; act = null;
        if (esRec) { int multa = Mathf.Min(MULTA_ABORTAR, Plata); if (multa > 0) Modificar(-multa); Mapeo.Cartel("Recorrido abortado.\nMulta por combustible: -$" + multa + "   (no cobras el bono)", 4f); }
        else Mapeo.Cartel("Viaje cancelado", 1.5f);
    }
    void ActualizarMision()
    {
        var p = act.ps[tramoIdx];
        bool ok;
        if (p.aterriza) { int sp = Aeropuertos.SobrePista(p.nombre, avion.transform.position); ok = avion.enTierra && (sp == 1 || (sp == -1 && distViaje < viajeRadio)); }
        else ok = distViaje < 700f;   // paseo: alcanza con pasar por encima
        if (!ok) return;
        if (!Cumple(act.d.req)) { viajeActivo = false; act = null; Mapeo.Cartel("No cobraste: esta mision pedia " + NombreReq(viajeReq) + "\ny volaste con otro avion.", 5f); return; }
        bool ultimo = tramoIdx == act.ps.Count - 1;
        int N = act.ps.Count;
        if (ultimo)
        {
            Pagar(pagoFinal); if (AlLlegar != null) AlLlegar(); Torre.NivelCompleto();
            if (perfil != null && !perfil.hechas.Contains(act.d.id)) { perfil.hechas.Add(act.d.id); Guardar(); }
            Mapeo.Cartel((act.d.recorrido ? "RECORRIDO COMPLETO (" + N + " de " + N + "): " : "MISION CUMPLIDA: ") + act.d.titulo + "\n" + p.llegada + "\n+$" + pagoFinal + (act.d.recorrido ? "  (bono final)" : "") + "     Saldo: $" + Plata, 6f);
            viajeActivo = false; act = null;
        }
        else
        {
            Pagar(pagoEsc);
            Mapeo.Cartel("ESCALA " + (tramoIdx + 1) + " de " + N + ": " + p.nombre + "\n" + p.llegada + "\n+$" + pagoEsc + "     Saldo: $" + Plata + "\nSiguiente: " + act.ps[tramoIdx + 1].nombre, 6f);
            tramoIdx++; SetParada();
        }
    }

    // ---- pantalla
    GUIStyle sCardTxt, sCardTit;
    void Dibujar(GUIStyle txt, GUIStyle btn, GUIStyle titulo)
    {
        GUILayout.Label("MISIONES   (cobras al aterrizar; los recorridos pagan un poco por escala y un bono grande al terminar; +/- cambia el precio)", titulo);
        if (sCardTxt == null)
        {
            sCardTxt = new GUIStyle(txt) { wordWrap = true };
            sCardTit = new GUIStyle(txt) { fontStyle = FontStyle.Bold, wordWrap = true };
        }
        if (items.Count == 0 && Time.unscaledTime >= proxIntento) { ArmarItems(); proxIntento = Time.unscaledTime + 1f; }
        scroll = GUILayout.BeginScrollView(scroll, false, true);
        GUILayout.BeginHorizontal();
        GUILayout.Label("Piloto: " + NombrePerfil + "     Dinero: $" + Plata + "     Tu avion: " + (string.IsNullOrEmpty(AvionDeCarton.nombreAvion) ? "-" : AvionDeCarton.nombreAvion), txt);
        if (GUILayout.Button("Mezclar orden", btn, GUILayout.Width(110), GUILayout.Height(22))) Mezclar();
        GUILayout.EndHorizontal();
        GUILayout.BeginHorizontal();
        var bg0 = GUI.backgroundColor;
        GUI.backgroundColor = sub == 0 ? new Color(0.4f, 1f, 0.4f) : bg0;
        if (GUILayout.Button("Misiones normales", btn, GUILayout.Width(160), GUILayout.Height(26))) sub = 0;
        GUI.backgroundColor = sub == 1 ? new Color(0.4f, 1f, 0.4f) : bg0;
        if (GUILayout.Button("Recorridos (con escalas)", btn, GUILayout.Width(190), GUILayout.Height(26))) sub = 1;
        GUI.backgroundColor = bg0;
        GUILayout.EndHorizontal();
        if (viajeActivo)
        {
            GUILayout.BeginHorizontal();
            bool esRec = act != null && act.d.recorrido;
            GUILayout.Label("EN CURSO: " + viajeTitulo + " > " + viajeNombre + "   quedan " + (distViaje >= 0f ? (distViaje / 1000f).ToString("F1") + " km" : "...") + "   por cobrar $" + viajePago, txt);
            if (GUILayout.Button(esRec ? "Abortar (-$" + MULTA_ABORTAR + ")" : "Cancelar", btn, GUILayout.Width(130), GUILayout.Height(22))) Abortar();
            GUILayout.EndHorizontal();
        }
        if (items.Count == 0) GUILayout.Label("Esperando a que carguen los aeropuertos para armar las misiones...", txt);
        float ancho = Tienda.PanelLateral.Ancho() - 24f;
        const float CARD = 400f;
        int cols = Mathf.Max(1, (int)(ancho / (CARD + 12f)));
        var lista = new List<Item>();
        foreach (var it in items) if (it.d.recorrido == (sub == 1) && !Hecha(it)) lista.Add(it);
        var hechas = new List<Item>();
        foreach (var it in items) if (it.d.recorrido == (sub == 1) && Hecha(it)) hechas.Add(it);
        var cc = GUI.contentColor;
        for (int i = 0; i < lista.Count; i += cols)
        {
            GUILayout.BeginHorizontal();
            for (int j = i; j < i + cols && j < lista.Count; j++) TarjetaMision(lista[j], CARD, txt, btn, cc);
            GUILayout.EndHorizontal();
        }
        if (hechas.Count > 0)
        {
            GUILayout.Space(6);
            GUI.contentColor = new Color(0.65f, 0.65f, 0.65f);
            GUILayout.Label("YA HECHAS (" + hechas.Count + ")", sCardTit);
            foreach (var h in hechas) GUILayout.Label("[X]  " + h.d.titulo + (h.d.recorrido ? "   (" + h.ps.Count + " escalas)" : "   ->  " + h.ps[0].nombre), sCardTxt);
            GUI.contentColor = cc;
        }
        GUILayout.Space(6);
        if (GUILayout.Button(verPerfil ? "Ocultar perfil de piloto" : "Perfil de piloto (cambiar jugador, ajustar dinero)", btn, GUILayout.Width(280), GUILayout.Height(22))) verPerfil = !verPerfil;
        if (verPerfil) DibujarPerfil(txt, btn, titulo);
        GUILayout.EndScrollView();
    }
    bool verPerfil;

    void TarjetaMision(Item it, float CARD, GUIStyle txt, GUIStyle btn, Color cc)
    {
        var d = it.d;
        GUILayout.BeginVertical(GUI.skin.box, GUILayout.Width(CARD));
        GUI.contentColor = d.vip ? new Color(1f, 0.85f, 0.2f) : Color.white;
        GUILayout.Label((d.vip ? "* " : "") + d.titulo, sCardTit);
        GUI.contentColor = cc;
        GUILayout.Label(d.texto, sCardTxt);
        double kmPrim = KmHasta(it.ps[0]), kmRuta = KmRuta(it), kmTot = kmPrim + kmRuta;
        if (!d.recorrido) GUILayout.Label("Destino: " + it.ps[0].nombre + "  -  " + kmPrim.ToString("F0") + " km desde donde estas", sCardTxt);
        else
        {
            string r = ""; double ant = kmPrim;
            for (int i = 0; i < it.ps.Count; i++)
            {
                double leg = i == 0 ? kmPrim : Haversine(it.ps[i - 1].lat, it.ps[i - 1].lon, it.ps[i].lat, it.ps[i].lon) / 1000.0;
                r += (i + 1) + ") " + it.ps[i].nombre + (it.ps[i].aterriza ? "" : " (pasar por encima)") + "   " + leg.ToString("F0") + " km" + (i < it.ps.Count - 1 ? "\n" : "");
            }
            GUILayout.Label("ESCALAS EN ORDEN (el 1er tramo es desde donde estas):\n" + r, sCardTxt);
            GUILayout.Label("Total: " + kmTot.ToString("F0") + " km", sCardTxt);
        }
        GUILayout.Label("Tiempo estimado: " + Tiempos(it, kmTot), sCardTxt);
        bool ok = Cumple(d.req);
        GUI.contentColor = ok ? new Color(0.6f, 1f, 0.6f) : new Color(1f, 0.45f, 0.4f);
        GUILayout.Label("Avion: " + NombreReq(d.req) + (d.req == "cualquiera" ? "" : (ok ? "   (OK, el tuyo sirve)" : "   (el tuyo NO sirve: cambialo en la pestana Aviones)")), sCardTxt);
        GUI.contentColor = cc;
        int paso = d.recorrido ? 100 : 50;
        if (d.recorrido) GUILayout.Label("Por escala: $" + PagoEscala(it) + "     Bono final: $" + BonoRec(it) + "     TOTAL: $" + TotalRec(it) + "\nSi abortas pagas $" + MULTA_ABORTAR + " de combustible y no cobras el bono.", sCardTxt);
        GUILayout.BeginHorizontal();
        GUILayout.Label(d.recorrido ? "Bono:" : "Pago:", txt, GUILayout.Width(44));
        if (GUILayout.Button("-", btn, GUILayout.Width(26), GUILayout.Height(22))) it.ajuste -= paso;
        GUI.contentColor = new Color(0.6f, 1f, 0.6f); GUILayout.Label("$" + (d.recorrido ? BonoRec(it) : PagoNormal(it)), sCardTit, GUILayout.Width(80)); GUI.contentColor = cc;
        if (GUILayout.Button("+", btn, GUILayout.Width(26), GUILayout.Height(22))) it.ajuste += paso;
        GUILayout.FlexibleSpace();
        bool esta = viajeActivo && act == it;
        var bgI = GUI.backgroundColor; GUI.backgroundColor = esta ? new Color(0.4f, 1f, 0.4f) : (ok ? bgI : new Color(0.6f, 0.4f, 0.4f));
        if (GUILayout.Button(esta ? (d.recorrido ? "En curso (abortar)" : "En curso (cancelar)") : "Aceptar", btn, GUILayout.Width(140), GUILayout.Height(24)))
        {
            if (esta) Abortar();
            else if (!ok) Mapeo.Cartel("Esta mision pide " + NombreReq(d.req) + ".\nCambia de avion en la pestana Aviones.", 4f);
            else IniciarMision(it);
        }
        GUI.backgroundColor = bgI;
        GUILayout.EndHorizontal();
        GUILayout.EndVertical();
    }
}
