// ESCOLTA DEL AVION PRESIDENCIAL (pedido de Pablo 19/11, segunda version).
// No es una carrera: el Boeing presidencial (el Emirates 777 del proyecto) sale de arriba de Aeroparque arranca a 400 km/h y acelera de a poco
// hasta ~600 km/h, rumbo a Punta Indio. Vuela plano como un avion de linea: cada tanto corrige el rumbo 10-40 grados, se queda asi un rato y vuelve. Nunca da la vuelta.
// Al elegir el nivel: si no estas en un caza se cambia solo al F/A-18, y apareces en el aire unos metros detras del Boeing, a su misma velocidad.
// Vos lo custodias: podes rodearlo, ir por arriba o por abajo; lo que cuenta es no alejarte (entre 60 y 800 m = en formacion).
// Al llegar a Punta Indio se cobra segun el % del viaje en formacion (minimo 50%). Punto rojo "PRESIDENCIAL" en el mapa y marca en pantalla.
using UnityEngine;
using CesiumForUnity;
using Unity.Mathematics;
using Random = UnityEngine.Random;

public class Escolta : MonoBehaviour
{
    static Escolta inst;
    public static bool Activa { get { return inst != null && inst.estado == 2; } }

    // ---- niveles: todos con la misma ruta; cambia cuanto se mece, sube/baja y la velocidad ----
    // cambia de rumbo como un avion de linea: gira despacio unos grados, se queda un rato derecho y despues corrige (sin pendulo)
    class Nivel { public string nombre, desc; public float kmh, angMin, angMax, holdMin, holdMax, giroRate, ondaAlt; public int pago; }
    static readonly Nivel[] NIV = {
        new Nivel { nombre = "Nivel 1 - Tranquilo",  desc = "Acelera hasta 600 km/h. Cada tanto corrige el rumbo 10 a 20 grados y se queda un rato asi.", kmh = 600f, angMin = 10f, angMax = 20f, holdMin = 25f, holdMax = 40f, giroRate = 1.5f, ondaAlt = 0f,   pago = 600 },
        new Nivel { nombre = "Nivel 2 - Movido",     desc = "Hasta 650 km/h. Correcciones de 10 a 30 grados mas seguidas y cambia de altura (hasta 150 m).", kmh = 650f, angMin = 10f, angMax = 30f, holdMin = 18f, holdMax = 30f, giroRate = 2f,   ondaAlt = 150f, pago = 1000 },
        new Nivel { nombre = "Nivel 3 - Exigente",   desc = "Hasta 700 km/h. Giros de 20 a 40 grados, cambios de altura de hasta 300 m. Hay que estar atento.", kmh = 700f, angMin = 20f, angMax = 40f, holdMin = 12f, holdMax = 20f, giroRate = 3f,   ondaAlt = 300f, pago = 1600 },
    };
    const float VEL_INICIAL_KMH = 400f, ACELERACION = 0.4f;   // arranca a 400 km/h y va acelerando de a poco (0,4 m/s2 = ~1,4 km/h por segundo), como un avion grande
    const float FORM_MIN = 60f, FORM_MAX = 800f, LEJOS = 2500f, PERDIDO = 8000f, ESPERA_PERDIDO = 30f;
    const double ALTURA = 600.0, LLEGADA_M = 3000.0, DETRAS_M = 350.0;

    // ---- estado: 0 nada, 1 preparando (cambiando de avion), 2 en vuelo ----
    int estado, nivel; float prepDesde; AvionDeCarton avionViejo;
    float tiempo, enForm, perdidoT;
    double lat, lon, h, hBase, latA, lonA, latP, lonP, distTotal; float rumbo, vel, giroAct, bankAct, pitchAct, offObj, holdT; double hObj; float vsAct; int ultimoLado;
    float distAct = -1f, difAlt;
    public static bool marcaPantalla = true;

    Transform padre; GameObject guia; CesiumGlobeAnchor anc;
    GUIStyle sGrande, sChico, sMarca;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Crear() { if (FindFirstObjectByType<CesiumGeoreference>() == null) return; new GameObject("Escolta").AddComponent<Escolta>(); }

    void Start()
    {
        inst = this;
        var geo = FindFirstObjectByType<CesiumGeoreference>(); padre = geo != null ? geo.transform : null;
        marcaPantalla = PlayerPrefs.GetInt("escolta_marca", 1) == 1;
    }

    // ------------------------------------------------------------ geografia
    const double R = 6371000.0;
    static void Mover(double la, double lo, float rumboDeg, double metros, out double la2, out double lo2)
    {
        double r = rumboDeg * System.Math.PI / 180.0;
        la2 = la + (metros * System.Math.Cos(r) / R) * 180.0 / System.Math.PI;
        lo2 = lo + (metros * System.Math.Sin(r) / (R * System.Math.Cos(la * System.Math.PI / 180.0))) * 180.0 / System.Math.PI;
    }
    static float Rumbo(double la1, double lo1, double la2, double lo2)
    {
        double dN = (la2 - la1) * System.Math.PI / 180.0 * R, dE = (lo2 - lo1) * System.Math.PI / 180.0 * R * System.Math.Cos(la1 * System.Math.PI / 180.0);
        float r = (float)(System.Math.Atan2(dE, dN) * 180.0 / System.Math.PI); return r < 0f ? r + 360f : r;
    }
    static double Dist(double la1, double lo1, double la2, double lo2)
    {
        double dN = (la2 - la1) * System.Math.PI / 180.0 * R, dE = (lo2 - lo1) * System.Math.PI / 180.0 * R * System.Math.Cos(la1 * System.Math.PI / 180.0);
        return System.Math.Sqrt(dN * dN + dE * dE);
    }

    // ------------------------------------------------------------ el Boeing presidencial (Emirates 777 de Resources, ajuste "Avion": rotacion Y 180, escala 0.3)
    void CrearGuia()
    {
        if (guia != null) return;
        guia = new GameObject("AvionPresidencial"); guia.transform.SetParent(padre, false);
        anc = guia.AddComponent<CesiumGlobeAnchor>();
        var piv = new GameObject("pivote"); piv.transform.SetParent(guia.transform, false);
        var prefab = Resources.Load<GameObject>("Avion");
        if (prefab != null)
        {
            var m = Instantiate(prefab, piv.transform); var giroFabrica = m.transform.localRotation; m.transform.localPosition = Vector3.zero; m.transform.localScale = Vector3.one;   // giroFabrica: el archivo (Sketchfab) trae un giro de 90 grados en su raiz; si se borra queda con la nariz al piso
            var rs = m.GetComponentsInChildren<Renderer>(); Vector3 c = Vector3.zero;
            if (rs.Length > 0) { Bounds b = rs[0].bounds; foreach (var r in rs) b.Encapsulate(r.bounds); c = piv.transform.InverseTransformPoint(b.center); }
            var rot = Quaternion.Euler(0f, GIRO_Y, 0f) * giroFabrica; const float ESC = 0.78f;   // 0.78 = ~50 m de largo (19/11: Pablo pidio +30% sobre 0.6), mas del doble del Emirates del juego (el 777 real seria 1.0 = 64 m)
            m.transform.localRotation = rot; m.transform.localScale = Vector3.one * ESC; m.transform.localPosition = -(Quaternion.Euler(0f, GIRO_Y, 0f) * (c * ESC));
            foreach (var col in m.GetComponentsInChildren<Collider>()) Destroy(col);
        }
        else
        {
            var c = GameObject.CreatePrimitive(PrimitiveType.Cube); Destroy(c.GetComponent<Collider>()); c.transform.SetParent(piv.transform, false); c.transform.localScale = new Vector3(60f, 6f, 64f);
        }
    }

    const float GIRO_Y = 0f;   // 19/11: con 180 iba nariz con nariz con el caza (foto de Pablo); con el giro de fabrica respetado, la nariz ya mira para adelante

    static bool EsCaza(string n) { return n.Contains("F/A-18") || n.Contains("Viggen") || n.ToLower().Contains("guerra"); }

    // ------------------------------------------------------------ empezar
    void Empezar(int n)
    {
        if (padre == null) return;
        if (AvionDeCarton.enAuto) { Mapeo.Cartel("Subite al avion para empezar la escolta", 3f); return; }
        nivel = n;
        avionViejo = FindFirstObjectByType<AvionDeCarton>();
        if (!EsCaza(AvionDeCarton.nombreAvion) && SelectorAvion.Lista != null)
        {
            foreach (var t in SelectorAvion.Lista) if (t.nombre.StartsWith("F/A-18")) { SelectorAvion.Elegir(t); break; }
        }
        else avionViejo = null;   // ya estas en un caza: no hace falta esperar el cambio
        estado = 1; prepDesde = Time.unscaledTime;
        Mapeo.Cartel("Preparando la escolta del avion presidencial...", 3f);
    }

    // cuando el caza ya esta cargado: lo pone en el aire detras del Boeing, sobre Aeroparque, mirando a Punta Indio
    void Arrancar(AvionDeCarton av)
    {
        double hA; if (!Aeropuertos.Buscar("Aeroparque", out latA, out lonA, out hA)) { latA = -34.5592; lonA = -58.4156; hA = 25.0; }
        string nom; double hP; latP = -35.3556; lonP = -57.2889;
        if (Aeropuertos.BuscarFrag("Punta Indio", out nom, out latP, out lonP)) { } else { latP = -35.3556; lonP = -57.2889; }
        hBase = hA + ALTURA; distTotal = Dist(latA, lonA, latP, lonP);
        rumbo = Rumbo(latA, lonA, latP, lonP);
        Mover(latA, lonA, rumbo, 1500.0, out lat, out lon); h = hBase;   // el Boeing arranca 1,5 km adelante de Aeroparque
        vel = VEL_INICIAL_KMH / 3.6f; giroAct = 0f; bankAct = 0f; pitchAct = 0f; vsAct = 0f; offObj = 0f; holdT = 20f; hObj = hBase; ultimoLado = 0;
        CrearGuia(); guia.SetActive(true); Aplicar();
        // el caza: 350 m detras, un poquito mas alto, misma direccion y misma velocidad
        double la2, lo2; Mover(lat, lon, rumbo + 180f, DETRAS_M, out la2, out lo2);
        var an = av.GetComponent<CesiumGlobeAnchor>();
        an.longitudeLatitudeHeight = new double3(lo2, la2, h + 20.0);
        an.rotationEastUpNorth = Quaternion.Euler(0f, rumbo, 0f);
        av.IniciarEnAire(); av.FijarVelocidad(vel);
        tiempo = 0f; enForm = 0f; perdidoT = 0f; estado = 2;
        Mapeo.Cartel("ESCOLTA: el avion presidencial va a Punta Indio. Acompanalo de cerca (hasta 800 m).", 5f);
    }

    void Terminar(bool llego, string porque = "")
    {
        estado = 0; if (guia != null) guia.SetActive(false);
        if (!llego) { Mapeo.Cartel(porque != "" ? porque : "Escolta cancelada", 4f); return; }
        float pct = tiempo > 0f ? enForm / tiempo : 0f; int p100 = Mathf.RoundToInt(pct * 100f);
        if (p100 > PlayerPrefs.GetInt("escolta_mejor_" + nivel, 0)) PlayerPrefs.SetInt("escolta_mejor_" + nivel, p100);
        if (pct >= 0.5f)
        {
            int pago = Mathf.RoundToInt(NIV[nivel].pago * pct / 10f) * 10; int antes = Carrera.Plata; Carrera.Pagar(pago); Debug.Log("[Escolta] pago " + pago + ": " + antes + " -> " + Carrera.Plata);
            Mapeo.Cartel("EL PRESIDENTE LLEGO A PUNTA INDIO!  Escolta " + p100 + "% en formacion.  Cobraste $" + pago + "  (tenias $" + antes + ", ahora $" + Carrera.Plata + ")", 9f);
        }
        else Mapeo.Cartel("Llego a Punta Indio, pero lo acompanaste solo " + p100 + "% del viaje (hace falta 50%).", 7f);
    }

    // ------------------------------------------------------------ cada cuadro
    void Update()
    {
        if (estado == 1)
        {
            var av = FindFirstObjectByType<AvionDeCarton>();
            bool listo = av != null && av != avionViejo && SelectorAvion.cargando == "" && av.GetComponent<CesiumGlobeAnchor>() != null;
            if (listo) Arrancar(av);
            else if (Time.unscaledTime - prepDesde > 40f) { estado = 0; Mapeo.Cartel("No pude preparar el caza. Proba de nuevo.", 4f); }
            return;
        }
        if (estado != 2) return;
        float dt = Time.deltaTime; if (dt <= 0f) return;
        if (AvionDeCarton.enAuto) { Terminar(false, "Escolta cancelada: te bajaste del avion"); return; }
        var yo = FindFirstObjectByType<AvionDeCarton>();
        tiempo += dt;

        float d = -1f;
        if (yo != null && guia != null) d = Vector3.Distance(yo.transform.position, guia.transform.position);
        if (d > 40000f) { Terminar(false, "Escolta cancelada (te teletransportaste)"); return; }
        double plon, plat; float palt, prum; difAlt = MapaAuto.PosActiva(out plon, out plat, out palt, out prum) ? (float)(h - palt) : 0f;
        distAct = d;
        if (d >= FORM_MIN && d <= FORM_MAX) enForm += dt;
        if (d > PERDIDO) { perdidoT += dt; if (perdidoT > ESPERA_PERDIDO) { Terminar(false, "Dejaste solo al avion presidencial. Escolta fallida."); return; } } else perdidoT = 0f;

        // velocidad: acelera progresivamente hasta la del nivel (si te quedaste muy atras afloja un poco; nunca acelera para escaparse)
        var N = NIV[nivel]; float vObj = N.kmh / 3.6f * (d > PERDIDO * 0.5f ? 0.55f : d > LEJOS ? 0.75f : 1f);
        vel = Mathf.MoveTowards(vel, vObj, ACELERACION * dt);

        // rumbo: tramos derechos con una correccion de unos grados respecto del rumbo a Punta Indio; cada tanto elige otra correccion
        holdT -= dt;
        double faltaM = Dist(lat, lon, latP, lonP);
        if (holdT <= 0f)
        {
            int lado = ultimoLado == 0 ? (Random.value < 0.5f ? -1 : 1) : (Random.value < 0.75f ? -ultimoLado : ultimoLado);   // casi siempre corrige para el otro lado
            float ang = Random.Range(N.angMin, N.angMax);
            offObj = Random.value < 0.2f ? 0f : lado * ang; if (offObj != 0f) ultimoLado = lado;
            holdT = Random.Range(N.holdMin, N.holdMax);
            if (N.ondaAlt > 0f && Random.value < 0.5f) hObj = hBase + Random.Range(0f, N.ondaAlt);
        }
        if (faltaM < 15000.0) offObj = 0f;   // los ultimos 15 km va derecho a Punta Indio
        float deseado = Mathf.Repeat(Rumbo(lat, lon, latP, lonP) + offObj, 360f);
        float dif = Mathf.DeltaAngle(rumbo, deseado);
        float giroObj = Mathf.Clamp(dif * 0.25f, -N.giroRate, N.giroRate);   // se acerca suave al rumbo nuevo y ahi se queda
        giroAct = Mathf.MoveTowards(giroAct, giroObj, 0.6f * dt);
        rumbo = Mathf.Repeat(rumbo + giroAct * dt, 360f);
        Mover(lat, lon, rumbo, vel * dt, out lat, out lon);
        // altura: sube o baja despacio hasta la altura elegida (nunca por debajo de la de arranque)
        float vsObj = Mathf.Clamp((float)(hObj - h) * 0.1f, -6f, 6f);
        vsAct = Mathf.MoveTowards(vsAct, vsObj, 0.8f * dt);
        h += vsAct * dt; if (h < hBase) h = hBase;
        float bankObj = Mathf.Atan(vel * giroAct * Mathf.Deg2Rad / 9.81f) * Mathf.Rad2Deg;
        bankAct = Mathf.Lerp(bankAct, bankObj, 1.2f * dt);
        pitchAct = Mathf.Lerp(pitchAct, Mathf.Atan2(vsAct, Mathf.Max(vel, 1f)) * Mathf.Rad2Deg, 1.2f * dt);
        Aplicar();

        if (Dist(lat, lon, latP, lonP) < LLEGADA_M) Terminar(true);
    }

    void Aplicar()
    {
        if (anc == null) return;
        anc.longitudeLatitudeHeight = new double3(lon, lat, h);
        anc.rotationEastUpNorth = Quaternion.Euler(-pitchAct, rumbo, -bankAct);
    }

    // ------------------------------------------------------------ pantalla
    Color ColorEstado() { return distAct < 0f ? Color.white : distAct < FORM_MIN ? new Color(1f, 0.6f, 0.1f) : distAct <= FORM_MAX ? new Color(0.3f, 1f, 0.35f) : distAct <= LEJOS ? new Color(1f, 0.9f, 0.2f) : new Color(1f, 0.3f, 0.25f); }
    string TextoEstado() { return distAct < 0f ? "" : distAct < FORM_MIN ? "MUY PEGADO" : distAct <= FORM_MAX ? "EN FORMACION" : distAct <= LEJOS ? "TE ESTAS ALEJANDO" : "VOLVE CON EL PRESIDENTE"; }
    static string Metros(float m) { return m < 1000f ? Mathf.RoundToInt(m) + " m" : (m / 1000f).ToString("0.0") + " km"; }
    void Caja(Rect r, float a) { var c = GUI.color; GUI.color = new Color(0f, 0f, 0f, a); GUI.DrawTexture(r, Texture2D.whiteTexture); GUI.color = c; }
    void Marco(Rect r, float g, Color col) { var c = GUI.color; GUI.color = col; var t = Texture2D.whiteTexture; GUI.DrawTexture(new Rect(r.x, r.y, r.width, g), t); GUI.DrawTexture(new Rect(r.x, r.yMax - g, r.width, g), t); GUI.DrawTexture(new Rect(r.x, r.y, g, r.height), t); GUI.DrawTexture(new Rect(r.xMax - g, r.y, g, r.height), t); GUI.color = c; }

    void OnGUI()
    {
        if (estado != 2) return;
        GUI.depth = -50;
        if (sGrande == null)
        {
            sGrande = new GUIStyle(GUI.skin.label) { fontSize = 22, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
            sChico = new GUIStyle(sGrande) { fontSize = 16 };
            sMarca = new GUIStyle(sGrande) { fontSize = 16, alignment = TextAnchor.UpperCenter };
        }
        float W = Screen.width, H = Screen.height; var col = ColorEstado();
        float pct = tiempo > 0f ? enForm / tiempo * 100f : 0f;
        double falta = Dist(lat, lon, latP, lonP);
        var rc = new Rect(W * 0.5f - 300f, 84f, 600f, 64f); Caja(rc, 0.6f);
        sGrande.normal.textColor = col; GUI.Label(new Rect(rc.x, rc.y + 2f, rc.width, 30f), TextoEstado() + (distAct >= 0f ? "   " + Metros(distAct) : ""), sGrande);
        sChico.normal.textColor = Color.white;
        string alt = Mathf.Abs(difAlt) > 150f ? (difAlt > 0f ? "   (esta " : "   (esta ") + Mathf.RoundToInt(Mathf.Abs(difAlt)) + " m " + (difAlt > 0f ? "mas arriba)" : "mas abajo)") : "";
        GUI.Label(new Rect(rc.x, rc.y + 34f, rc.width, 26f), "Punta Indio a " + Metros((float)falta) + "   formacion " + Mathf.RoundToInt(pct) + "%" + alt, sChico);

        if (!marcaPantalla || guia == null) return;
        var cam = Camera.main; if (cam == null) return;
        Vector3 sp = cam.WorldToScreenPoint(guia.transform.position);
        bool detras = sp.z < 0f; Vector2 p = new Vector2(sp.x, H - sp.y);
        if (detras) p = new Vector2(W - p.x, H - p.y);
        float m = 50f; bool fuera = detras || p.x < m || p.x > W - m || p.y < m || p.y > H - m;
        if (!fuera)
        {
            if (distAct < 250f) return;   // de cerca se ve solo, no hace falta el cuadrito
            float s = Mathf.Clamp(20000f / Mathf.Max(distAct, 1f), 30f, 80f);
            var rb = new Rect(p.x - s * 0.5f, p.y - s * 0.5f, s, s); Marco(rb, 3f, Color.black); Marco(new Rect(rb.x + 1f, rb.y + 1f, rb.width - 2f, rb.height - 2f), 2f, col);
            sMarca.normal.textColor = col; var rt = new Rect(p.x - 80f, rb.yMax + 2f, 160f, 24f); Caja(new Rect(p.x - 50f, rt.y, 100f, 22f), 0.55f); GUI.Label(rt, Metros(distAct), sMarca);
        }
        else
        {
            Vector2 c = new Vector2(W * 0.5f, H * 0.5f), dir = (p - c); if (dir.sqrMagnitude < 1f) dir = Vector2.up; dir.Normalize();
            float k = Mathf.Min((W * 0.5f - m) / Mathf.Max(Mathf.Abs(dir.x), 0.001f), (H * 0.5f - m) / Mathf.Max(Mathf.Abs(dir.y), 0.001f));
            Vector2 q = c + dir * k; float ang = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
            var m0 = GUI.matrix; GUIUtility.RotateAroundPivot(ang, q);
            var c0 = GUI.color; GUI.color = Color.black; GUI.DrawTexture(new Rect(q.x - 22f, q.y - 7f, 44f, 14f), Texture2D.whiteTexture);
            GUI.color = col; GUI.DrawTexture(new Rect(q.x - 20f, q.y - 5f, 40f, 10f), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(q.x + 10f, q.y - 13f, 10f, 26f), Texture2D.whiteTexture);
            GUI.color = c0; GUI.matrix = m0;
            sMarca.normal.textColor = col; var rt = new Rect(q.x - 90f - dir.x * 50f, q.y - 12f - dir.y * 34f, 180f, 24f);
            Caja(new Rect(rt.center.x - 75f, rt.y, 150f, 22f), 0.55f); GUI.Label(rt, "PRESIDENTE " + Metros(distAct), sMarca);
        }
    }

    // ------------------------------------------------------------ punto en el mapa (lo llama MapaChiches)
    public static void DibujarEnMapa(Rect r, System.Func<double, double, Vector2> px, Texture2D cir)
    {
        if (!Activa) return;
        var e = inst; var c0 = GUI.color; var rojo = new Color(1f, 0.25f, 0.2f);
        // linea punteada de la ruta hasta Punta Indio
        Vector2 a = px(e.lat, e.lon), b = px(e.latP, e.lonP);
        for (int i = 0; i < 40; i += 2)
        {
            Vector2 p1 = Vector2.Lerp(a, b, i / 40f), p2 = Vector2.Lerp(a, b, (i + 1) / 40f);
            var m1 = GUI.matrix; var dd = p2 - p1; GUIUtility.RotateAroundPivot(Mathf.Atan2(dd.y, dd.x) * Mathf.Rad2Deg, p1);
            GUI.color = new Color(1f, 0.35f, 0.3f, 0.8f); GUI.DrawTexture(new Rect(p1.x, p1.y - 1.5f, dd.magnitude, 3f), Texture2D.whiteTexture); GUI.matrix = m1;
        }
        if (b.x > -20f && b.y > -20f && b.x < r.width + 20f && b.y < r.height + 20f) { GUI.color = Color.black; GUI.DrawTexture(new Rect(b.x - 8f, b.y - 8f, 16f, 16f), cir); GUI.color = Color.white; GUI.DrawTexture(new Rect(b.x - 6f, b.y - 6f, 12f, 12f), cir); }
        if (a.x < -20f || a.y < -20f || a.x > r.width + 20f || a.y > r.height + 20f) { GUI.color = c0; return; }
        var m0 = GUI.matrix; GUIUtility.RotateAroundPivot(e.rumbo - 90f, a);
        GUI.color = Color.black; GUI.DrawTexture(new Rect(a.x, a.y - 3f, 26f, 6f), Texture2D.whiteTexture);
        GUI.color = rojo; GUI.DrawTexture(new Rect(a.x, a.y - 2f, 24f, 4f), Texture2D.whiteTexture);
        GUI.matrix = m0;
        GUI.color = Color.black; GUI.DrawTexture(new Rect(a.x - 11f, a.y - 11f, 22f, 22f), cir);
        GUI.color = rojo; GUI.DrawTexture(new Rect(a.x - 9f, a.y - 9f, 18f, 18f), cir);
        GUI.color = Color.white; GUI.DrawTexture(new Rect(a.x - 3.5f, a.y - 3.5f, 7f, 7f), cir);
        var s = new GUIStyle(GUI.skin.label) { fontSize = 14, fontStyle = FontStyle.Bold }; s.normal.textColor = Color.black;
        GUI.Label(new Rect(a.x + 13f, a.y - 9f, 140f, 20f), "PRESIDENCIAL", s); s.normal.textColor = new Color(1f, 0.4f, 0.35f); GUI.Label(new Rect(a.x + 12f, a.y - 10f, 140f, 20f), "PRESIDENCIAL", s);
        GUI.color = c0;
    }

    // ------------------------------------------------------------ app de la tablet
    public static void Pestana(GUIStyle txt, GUIStyle btn, GUIStyle tit) { if (inst != null) inst.DibujarPestana(txt, btn, tit); else GUILayout.Label("Cargando...", txt); }

    void DibujarPestana(GUIStyle txt, GUIStyle btn, GUIStyle tit)
    {
        GUILayout.Label("ESCOLTA DEL AVION PRESIDENCIAL", tit);
        var tw = new GUIStyle(txt) { wordWrap = true };
        GUILayout.Label("El Boeing presidencial sale de Aeroparque hacia Punta Indio, acelerando de a poco hasta unos 600 km/h y corrigiendo el rumbo cada tanto. Vos vas en el caza (se cambia solo) y lo custodias: rodealo, anda por arriba o por abajo, pero no te alejes mas de 800 m. Al llegar cobras si lo acompanaste al menos la mitad del viaje.", tw);
        GUILayout.Space(6);
        if (estado == 1) GUILayout.Label("Preparando el caza...", tit);
        else if (estado == 2)
        {
            float pct = tiempo > 0f ? enForm / tiempo * 100f : 0f;
            GUILayout.Label("En curso: " + NIV[nivel].nombre + "  -  formacion " + Mathf.RoundToInt(pct) + "%  -  " + (distAct >= 0f ? Metros(distAct) : ""), tit);
            if (GUILayout.Button("CANCELAR ESCOLTA", btn, GUILayout.Height(30))) Terminar(false);
        }
        else
        {
            for (int i = 0; i < NIV.Length; i++)
            {
                int mejor = PlayerPrefs.GetInt("escolta_mejor_" + i, 0);
                if (GUILayout.Button(NIV[i].nombre + "   (hasta $" + NIV[i].pago + ")" + (mejor > 0 ? "   mejor: " + mejor + "%" : ""), btn, GUILayout.Height(32))) Empezar(i);
                GUILayout.Label(NIV[i].desc, tw);
                GUILayout.Space(4);
            }
        }
        GUILayout.Space(6);
        bool n = GUILayout.Toggle(marcaPantalla, " Marca del presidencial en pantalla (cuadrito con la distancia)");
        if (n != marcaPantalla) { marcaPantalla = n; PlayerPrefs.SetInt("escolta_marca", n ? 1 : 0); }
    }
}
