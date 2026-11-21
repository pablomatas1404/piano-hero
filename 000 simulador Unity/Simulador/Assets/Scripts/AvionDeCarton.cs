using UnityEngine;
using UnityEngine.InputSystem;
using CesiumForUnity;

// Avioncito de cartón para probar el mapa. Se crea solo al apretar Play (no hay que arrastrar nada).
// Controles: joystick (stick izquierdo o cruceta = cabeceo y alabeo, RT acelera, LB/LT frena)
//            teclado (flechas = cabeceo y alabeo, W acelera, S frena)
public class AvionDeCarton : MonoBehaviour
{
    // ---- Ajustes (se pueden cambiar acá o en el Inspector) ----
    public float velocidadMaxMs = 140f;      // metros por segundo (140 m/s = 504 km/h)
    public float velocidadInicialMs = 70f;
    public float giroCabeceo = 40f;          // grados por segundo
    public float giroAlabeo = 80f;
    public float giroViraje = 35f;           // viraje que produce la inclinación
    public float zonaMuerta = 0.3f;          // radial, la misma que se usaba en Godot
    public bool invertirCabeceoJoystick = true;   // true = stick arriba baja la nariz (como un avión real)
    public float volumenMotor = 0.5f;         // 0..1, igual que el slider de Godot
    public float alturaInicialM = 150f;
    public float rumboInicialGrados = 130f;  // pista 13 de Aeroparque aprox.
    // Cámara: iguales a la CamaraJuego de Godot (principal.tscn: near 0.1, far 100000).
    // La cámara de Unity venía con far = 1000 m, y por eso lo lejano se veía gris.
    public float camaraCerca = 0.3f;
    public float camaraLejos = 100000f;

    // ---- Dinámica de vuelo portada de Godot (principal.gd) ----
    const float FACTOR_RITMO = 0.5f;   // pedido de Pablo: la velocidad sube y baja a la mitad de rapido
    const float ACELERACION = 40f;            // m/s² en el aire
    const float RAMPA_CONTROL = 0.65f;        // segundos de 0 al tope en cabeceo y alabeo
    const float BASE_OSCILACION_VIENTO = 0.55f;
    float cabeceoActual, bancoActual;         // bancoActual: + = ala derecha abajo
    float faseViento, rollVientoPrev, cabVientoPrev;
    bool vientoActivo; float vientoDir, vientoKmh;

    Vector3 camAdelUlt = Vector3.forward;
    string ultimoModelo;
    float giroEntrada;   // volante en tierra: entra a 1.8/s, sale a 3.5/s
    float ultimaRueda;
    float intensidadFreno; bool frenoGradualAnt, frenoRapidoTierra;
    bool reversaActiva, reversaAnt, emergenciaAnt; string aviso = ""; float avisoHasta;
    const float VELOCIDAD_REVERSA = 3f;
    // freno de emergencia (boton Select): baja la velocidad a este tope solo si vas mas rapido; aviones grandes 150 km/h, avionetas (Cessna) y helicopteros 100 km/h
    float TopeEmergencia() { string n = (nombreAvion ?? "").ToLowerInvariant(); bool chico = esHeli || n.Contains("cessna") || n.Contains("piper"); return (chico ? 100f : 150f) / 3.6f; }
    float tiempoEnAire, alturaPrev = -1f, tiempoSosp, tiempoSinToque, bloqueoDespegue;
    public bool enTierra; bool sueloDeRespaldo; public static string nombreAvion = ""; float pieM = 3f; float hSobrePiso = 999f;
    float zoomManual = 1f, orbitaYaw, orbitaPitch;
    string ultimoJson; float guardarEn = -1f;
    float velocidad;
    float acelerador = 0.5f;
    float fps = 60f;
    CesiumGlobeAnchor ancla;
    Camera cam;
    GUIStyle estilo;

    // ================= EDITOR DE ORIENTACIÓN DEL MODELO =================
    // Pablo orienta el avión con botones y aprieta "CLAVAR": queda guardado en
    // ajustes_aviones.json (carpeta del proyecto Unity) y se carga solo en cada Play.
    // Orientación del modelo (rx..oz, escala) + sensibilidad de vuelo (valores de fábrica de Godot, panel Sensibilidad)
    [System.Serializable] class AjusteModelo
    {
        public string nombre; public float rx, ry, rz, escala, ox, oy, oz;
        public float giro = 0.6f;        // grados de viraje por seg, por grado de inclinación
        public float angulo = 45f;       // inclinación máxima (alabeo)
        public float vertical = 40f;     // cabeceo máximo
        public float velMin = 0f;        // m/s
        public float velMax = 417f;      // m/s (~1500 km/h)
        public float velAnim = 1f;       // velocidad de animación de los controles
        public float viento = 0.35f;     // sensibilidad al viento (0.35 = avión grande)
        public float volumen = 0.5f;     // volumen del motor
        public float camVel = 60f;       // velocidad del stick derecho (giro de la vista)
        public float radioGiro = 40f;    // radio de giro en tierra (m) -- PROVISORIO, pedir el exacto a Godot
        public float giroSueloMax = 25f; // tope de giro en tierra (grados/seg) -- PROVISORIO
        public float nivelSuelo = 3f;    // suavidad del enderezado al tocar tierra
        public float acelSuelo = 10f;    // aceleracion en tierra (m/s2): mas lenta que Godot a pedido de Pablo
        public float frenoSuelo = 8f;    // freno en tierra (m/s2): 40 -> 0 en ~5 s
    }
    [System.Serializable] class ListaAjustes { public System.Collections.Generic.List<AjusteModelo> items = new System.Collections.Generic.List<AjusteModelo>(); }
    static Transform modeloT; public static Transform ModeloT { get { return modeloT; } }
    static Vector3 centroLocal;
    static AjusteModelo ajuste;
    static string nombreModelo = "Avion";   // = nombre del archivo en Assets/Resources
    bool editorAbierto = true;
    bool pausado = false;
    float paso = 5f;
    string mensaje = "";
    static string RutaAjustes { get { return System.IO.Path.GetFullPath(System.IO.Path.Combine(Application.dataPath, "..", "ajustes_aviones.json")); } }

    // ---- zoom de la camara guardado POR AVION (zoom_camara.json): lo dejas como te gusta y queda para la proxima vez
    [System.Serializable] class ZJ { public string nombre; public float zoom = 1f; }
    [System.Serializable] class ListaZ { public System.Collections.Generic.List<ZJ> items = new System.Collections.Generic.List<ZJ>(); }
    static string RutaZoom { get { return System.IO.Path.GetFullPath(System.IO.Path.Combine(Application.dataPath, "..", "zoom_camara.json")); } }
    string zoomModelo; float zoomUlt = -1f, zoomSucio = -1f;
    void ZoomPorModelo()
    {
        if (zoomModelo != nombreModelo)
        {
            zoomModelo = nombreModelo; zoomSucio = -1f; float z = 1f;
            try { if (System.IO.File.Exists(RutaZoom)) { var l = JsonUtility.FromJson<ListaZ>(System.IO.File.ReadAllText(RutaZoom)); if (l != null) foreach (var it in l.items) if (it.nombre == nombreModelo) z = it.zoom; } } catch { }
            zoomManual = Mathf.Clamp(z, 0.25f, 5f); zoomUlt = zoomManual; return;
        }
        if (!Mathf.Approximately(zoomManual, zoomUlt)) { zoomUlt = zoomManual; zoomSucio = Time.unscaledTime; }
        if (zoomSucio >= 0f && Time.unscaledTime - zoomSucio > 1.2f)   // se guarda cuando dejas de tocar el zoom
        {
            zoomSucio = -1f;
            try
            {
                var l = new ListaZ();
                if (System.IO.File.Exists(RutaZoom)) l = JsonUtility.FromJson<ListaZ>(System.IO.File.ReadAllText(RutaZoom)) ?? new ListaZ();
                l.items.RemoveAll(a => a.nombre == nombreModelo); l.items.Add(new ZJ { nombre = nombreModelo, zoom = zoomManual });
                System.IO.File.WriteAllText(RutaZoom, JsonUtility.ToJson(l, true));
            }
            catch { }
        }
    }

    static AjusteModelo AjustePorDefecto()
    {
        // Valores de Godot para el Boeing 777: escala 0.3, rotación Y 180
        return new AjusteModelo { nombre = nombreModelo, rx = 0f, ry = ryDefecto, rz = 0f, escala = escalaDefecto, ox = 0f, oy = 0f, oz = 0f, vertical = esHeli ? VELOCIDAD_VERTICAL_HELI : 40f };
    }

    static void CargarAjuste()
    {
        ajuste = AjustePorDefecto();
        try
        {
            if (System.IO.File.Exists(RutaAjustes))
            {
                var lista = JsonUtility.FromJson<ListaAjustes>(System.IO.File.ReadAllText(RutaAjustes));
                foreach (var a in lista.items) if (a.nombre == nombreModelo) { ajuste = a; break; }
            }
        }
        catch (System.Exception e) { Debug.LogWarning("No pude leer ajustes_aviones.json: " + e.Message); }
    }

    static string GuardarAjuste()
    {
        try
        {
            var lista = new ListaAjustes();
            if (System.IO.File.Exists(RutaAjustes))
                lista = JsonUtility.FromJson<ListaAjustes>(System.IO.File.ReadAllText(RutaAjustes)) ?? new ListaAjustes();
            lista.items.RemoveAll(a => a.nombre == nombreModelo);
            lista.items.Add(ajuste);
            System.IO.File.WriteAllText(RutaAjustes, JsonUtility.ToJson(lista, true));
            return "CLAVADO. Guardado en ajustes_aviones.json";
        }
        catch (System.Exception e) { return "ERROR al guardar: " + e.Message; }
    }

    static void AplicarAjuste()
    {
        if (modeloT == null || ajuste == null) return;
        var rot = Quaternion.Euler(ajuste.rx, ajuste.ry, ajuste.rz);
        modeloT.localRotation = rot;
        modeloT.localScale = Vector3.one * ajuste.escala;
        modeloT.localPosition = -(rot * (centroLocal * ajuste.escala)) + new Vector3(ajuste.ox, ajuste.oy, ajuste.oz);
    }

    // ================= INTERFAZ COMPACTA (misma disposición que el Godot) =================
    // Franja de abajo de 118 px; solapas arriba de la franja; instrumentos arriba a la izquierda.
    // Página 2 = Sensibilidad (igual que PanelSensibilidad del Godot). "Modelo" = editor de orientación.
    int solapa = 1;                 // 0 Página 1, 1 Página 2 (Sensibilidad), 2 Sonidos, 3 Modelo
    Vector2 scrollSens;
    Texture2D texOscura;
    GUIStyle sTxt, sBtn, sVerde, sTitulo;

    void PrepararEstilos()
    {
        if (sTxt != null) return;
        texOscura = new Texture2D(1, 1);
        texOscura.SetPixel(0, 0, new Color(0.03f, 0.03f, 0.03f, 0.92f));
        texOscura.Apply();
        sTxt = new GUIStyle(GUI.skin.label) { fontSize = 12 };
        sTxt.normal.textColor = Color.white;
        sTitulo = new GUIStyle(sTxt) { fontStyle = FontStyle.Bold };
        sTitulo.normal.textColor = new Color(1f, 0.85f, 0.2f);
        sBtn = new GUIStyle(GUI.skin.button) { fontSize = 12 };
        sVerde = new GUIStyle(GUI.skin.label) { fontSize = 15, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
        sVerde.normal.textColor = new Color(0.2f, 1f, 0.3f);
    }

    void Caja(Rect r) { GUI.DrawTexture(r, texOscura); }

    // fila "nombre  -  valor  +" con layout automático
    float FilaL(string nombre, float valor, float st, string fmt)
    {
        GUILayout.BeginHorizontal();
        GUILayout.Label(nombre, sTxt, GUILayout.Width(88));
        if (GUILayout.Button("-", sBtn, GUILayout.Width(24), GUILayout.Height(20))) valor -= st;
        GUILayout.Label(valor.ToString(fmt), sTxt, GUILayout.Width(40));
        if (GUILayout.Button("+", sBtn, GUILayout.Width(24), GUILayout.Height(20))) valor += st;
        GUILayout.EndHorizontal();
        return valor;
    }

    bool lateralPlegado;
    public static AvionDeCarton Inst; static bool panelBajoOculto, panelBajoCargado;
    void DibujarEditor()
    {
        Inst = this;
        PrepararEstilos();
        if (ajuste == null) ajuste = AjustePorDefecto();
        ajuste.volumen = volumenMotor;
        float W = Screen.width, H = Screen.height;
        if (Time.time < avisoHasta) { var sa = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontSize = 20, fontStyle = FontStyle.Bold }; GUI.Label(new Rect(W * 0.5f - 350f, 44f, 700f, 34f), aviso, sa); }

        // ---- instrumentos arriba a la izquierda (cajitas digitales como en Godot) ----
        var tapaI = Disposicion.Bloque("instrumentos", "INSTRUMENTOS", new Rect(20, 28, 565, 80));
        Instrucciones.Marcar("instr", new Rect(20, 28, 565, 80));
        string alt = ancla != null ? ancla.longitudeLatitudeHeight.z.ToString("F0") + " m" : "-";
        string[] titulos = { "ALTITUD", "VELOCIDAD", "RESERVA", "DINERO US$", "EN PESOS" };
        string[] valores = { alt, (enAuto ? velAutoTxt : Mathf.RoundToInt(velocidad * 3.6f) + " km/h")   /* velocidad esta en m/s: se convierte a km/h reales (Godot mostraba el numero crudo de m/s) */, Mathf.RoundToInt(Combustible.Reserva) + " L", Carrera.TextoDinero, Carrera.TextoPesos };
        for (int i = 0; i < 5; i++)
        {
            var r = new Rect(20 + i * 113, 28, 105, 50);
            Caja(r);
            GUI.Label(new Rect(r.x + 4, r.y + 3, r.width, 16), titulos[i], sTxt);
            GUI.Label(new Rect(r.x, r.y + 18, i == 0 ? r.width - 22f : r.width, 28), valores[i], sVerde);
            if (i == 0) Torre.FlechasAltura(r.xMax - 22f, r.y + 5f, 16f, 18f);   // triangulitos junto a la altitud: rojo = corregi, verde = bien
        }
        // tanque: diez ladrillitos rojos (100 L cada uno)
        { float lleno = Mathf.Clamp(Combustible.Ladrillos, 0f, 10f); for (int i = 0; i < 10; i++) { var rb = new Rect(20 + i * 22, 80, 20, 7); GUI.color = new Color(1f, 1f, 1f, 0.12f); GUI.DrawTexture(rb, Texture2D.whiteTexture); float f = Mathf.Clamp01(lleno - i); if (f > 0f) { GUI.color = lleno < 2f ? new Color(1f, 0.1f, 0.1f) : new Color(0.9f, 0.2f, 0.15f); GUI.DrawTexture(new Rect(rb.x, rb.y, rb.width * f, rb.height), Texture2D.whiteTexture); } } GUI.color = Color.white; }
        if (ancla != null)
        {
            var llh = ancla.longitudeLatitudeHeight;
            GUI.Label(new Rect(20, 90, 500, 18),
                "Lat " + llh.y.ToString("F4") + "  Lon " + llh.x.ToString("F4") + (Gamepad.current != null ? "   Joystick: SI  (LT acelera, LB frena)" : "   Joystick: no (W acelera, S frena)") + "   rueda:" + ultimaRueda.ToString("F0") + " zoom:" + zoomManual.ToString("F2") + "   TIERRA:" + (enTierra ? "SI" : "no") + " h:" + hSobrePiso.ToString("F1") + " pie:" + pieM.ToString("F1") + (sueloDeRespaldo ? "  [suelo: pista trackeada]" : ""), sTxt);
        }

        tapaI.Dispose();
        // ---- triangulito: pliega el panel de abajo (queda una barrita) para ganar pantalla si se trabaja con la tablet ----
        if (!panelBajoCargado) { panelBajoCargado = true; panelBajoOculto = PlayerPrefs.GetInt("panel_bajo_oculto", 0) == 1; }
        if (panelBajoOculto && (Mapeo.abrirLugares || Hangar.abrir)) panelBajoOculto = false;   // 19/11: con el panel plegado, la M (nombre del lugar) y Cambiar avion se abrian escondidos y dejaban todo trabado
        if (panelBajoOculto)
        {
            Tienda.PanelLateral.rect = new Rect(0, 0, 0, 0); Torre.lateralAbierto = false;
            var rbar = new Rect(10f, H - 26f, 120f, 22f);
            var tapaB = Disposicion.Bloque("panel_plegado", "BOTON PANEL PLEGADO", rbar);
            if (GUI.Button(rbar, "    Panel", sBtn)) { panelBajoOculto = false; PlayerPrefs.SetInt("panel_bajo_oculto", 0); }
            GUI.DrawTexture(new Rect(rbar.x + 8f, rbar.y + 4f, 14f, 14f), Tablet.Triangulo(true));
            tapaB.Dispose();
            goto fin_panel_bajo;
        }
        // ---- solapas (arriba de la franja de abajo) ----
        if (Mapeo.abrirLugares) { solapa = 5; Mapeo.abrirLugares = false; }
        if (Hangar.abrir) { solapa = 7; Hangar.abrir = false; }
        float extra = solapa == 7 ? 190f : 0f;   // la pestana Aviones es mas alta (foto + lista)
        if (solapa != 4) Controles.CancelarEspera();   // salir de Controles con una asignacion a medias la cancela (si no, bloqueaba el vuelo)
        if (solapa != 5) Mapeo.CancelarLugar();   // si sales de la pestana Lugares con un nombre a medias, se cancela y se liberan las teclas
        if (solapa != 6) Carrera.SalioDePestana();
        string[] nombres = { "Pagina 1", "Pagina 2", "Sonidos", "Modelo", "Controles", "Destinos", "Carrera", "Aviones", "Guerra", "Luces", "Tren", "Aeropuertos cercanos", "Shopping", "Maletin", "Teletransporte" };
        float tx = 270f;
        float twT = 0f; for (int q = 0; q < nombres.Length; q++) twT += (q == 11 ? 150f : (q == 14 ? 110f : 80f)) + 4f;
        var tapaT = Disposicion.Bloque("solapas", "SOLAPAS (botones de abajo)", new Rect(270, H - 194 - extra, twT - 4f, 20));
        for (int i = 0; i < nombres.Length; i++)
        {
            var antes = GUI.backgroundColor;
            if (solapa == i) GUI.backgroundColor = Color.green;
            float bw = i == 11 ? 150 : (i == 14 ? 110 : 80);
            if (i == 1 || i == 7 || i == 12) Instrucciones.Marcar(i == 7 ? "tab_aviones" : (i == 12 ? "tab_shopping" : "tab_p2"), new Rect(tx, H - 194 - extra, bw, 20));
            if (GUI.Button(new Rect(tx, H - 194 - extra, bw, 20), nombres[i], sBtn)) { solapa = i; lateralPlegado = false; }
            tx += bw + 4;
            GUI.backgroundColor = antes;
        }

        { var rtri = new Rect(270f, H - 194f - extra - 24f, 46f, 20f); if (GUI.Button(rtri, GUIContent.none, sBtn)) { panelBajoOculto = true; PlayerPrefs.SetInt("panel_bajo_oculto", 1); } GUI.DrawTexture(new Rect(rtri.x + 16f, rtri.y + 3f, 14f, 14f), Tablet.Triangulo(false)); }   // plegar el panel de abajo
        tapaT.Dispose();

        // ---- panel LATERAL grande (Destinos, Carrera, Aeropuertos cercanos, Shopping, Maletin): ocupa de arriba a abajo a la derecha ----
        bool lateralActivo = (solapa == 5 || solapa == 6 || solapa == 11 || solapa == 12 || solapa == 13) && !(solapa == 5 && Mapeo.PanelLugarAbierto);
        Tienda.PanelLateral.rect = new Rect(0, 0, 0, 0);
        Torre.lateralAbierto = lateralActivo && !lateralPlegado;
        if (lateralActivo && !lateralPlegado)
        {
            float xl = Mathf.Min(DiaNoche.BarraX(W) + 395f + 272f, W * 0.55f); if (solapa == 12) xl = Mathf.Min(xl, W * 0.3f);   // el Shopping usa casi toda la pantalla para que las fotos se vean grandes   // desde el boton Azafata (o antes, si quedaria muy angosto) hasta el borde derecho
            float topL = Carrera.HayViaje ? 118f : 34f;   // con un viaje activo el panel baja para no tapar el cartel del destino (ni el cartel a el)
            var rl = new Rect(xl, topL, W - xl - 10f, H - topL - 204f);
            var tapaL = Disposicion.Bloque("lateral", "PANEL GRANDE DERECHO", rl); Caja(rl); Tienda.PanelLateral.ancho = rl.width - 20f; Tienda.PanelLateral.rect = Disposicion.PantRect("lateral", rl);
            GUILayout.BeginArea(new Rect(rl.x + 8, rl.y + 6, rl.width - 16, rl.height - 12));
            GUILayout.BeginHorizontal(); GUILayout.FlexibleSpace(); if (GUILayout.Button("Replegar panel  >>", sBtn, GUILayout.Width(150), GUILayout.Height(22))) lateralPlegado = true; GUILayout.EndHorizontal();
            if (solapa == 5) Aeropuertos.PestanaDestinos(sTxt, sBtn, sTitulo);
            else if (solapa == 6) Carrera.PestanaCarrera(sTxt, sBtn, sTitulo);
            else if (solapa == 11) Aeropuertos.PestanaTorre(sTxt, sBtn, sTitulo);
            else if (solapa == 12) Tienda.PestanaShopping(sTxt, sBtn, sTitulo);
            else Tienda.PestanaMaletin(sTxt, sBtn, sTitulo);
            GUILayout.EndArea();
            tapaL.Dispose();
        }

        // ---- panel principal de abajo (x 220 .. W-170) ----
        var rp = new Rect(270, H - 172 - extra, Mathf.Max(300f, W - 440f), 118 + extra);
        var tapaP = Disposicion.Bloque("panel_principal", "PANEL DE ABAJO", rp);
        Caja(rp);
        GUILayout.BeginArea(new Rect(rp.x + 6, rp.y + 4, rp.width - 12, rp.height - 8));
        if (lateralActivo)
        {
            GUILayout.Label(nombres[solapa].ToUpper() + "   (se abre en el panel grande de la derecha)", sTitulo);
            if (GUILayout.Button(lateralPlegado ? "Mostrar panel grande  <<" : "Replegar panel  >>", sBtn, GUILayout.Width(200), GUILayout.Height(26))) lateralPlegado = !lateralPlegado;
        }
        else if (solapa == 0)
        {
            // Pagina 1: toda la sensibilidad en columnas, sin scroll
            float vMinK = ajuste.velMin * 3.6f, vMaxK = ajuste.velMax * 3.6f, nMinK = vMinK, nMaxK = vMaxK;   // se muestran y se editan en km/h reales; por dentro siguen guardados en m/s
            GUILayout.BeginHorizontal();
            GUILayout.BeginVertical(GUILayout.Width(250));
            ajuste.giro = Mathf.Clamp(FilaL("Giro", ajuste.giro, 0.05f, "F2"), 0.1f, 2f);
            ajuste.angulo = Mathf.Clamp(FilaL("Angulo", ajuste.angulo, 2f, "F0"), 10f, 80f);
            if (esHeli) ajuste.vertical = Mathf.Clamp(FilaL("Subida (m/s)", ajuste.vertical, 2f, "F0"), 2f, 40f);   // helicoptero: velocidad de subida/bajada (rango Godot 2..40)
            else ajuste.vertical = Mathf.Clamp(FilaL("Cabeceo (nariz)", ajuste.vertical, 2f, "F0"), 10f, 80f);
            nMinK = FilaL("Vel. min km/h", vMinK, 10f, "F0");
            GUILayout.EndVertical();
            GUILayout.BeginVertical(GUILayout.Width(250));
            nMaxK = FilaL("Vel. max km/h", vMaxK, 10f, "F0");
            ajuste.velAnim = Mathf.Clamp(FilaL("Vel. animacion", ajuste.velAnim, 0.1f, "F1"), 0.1f, 3f);
            ajuste.viento = Mathf.Clamp(FilaL("Viento", ajuste.viento, 0.1f, "F1"), 0f, 3f);
            ajuste.camVel = Mathf.Clamp(FilaL("Vel. camara", ajuste.camVel, 10f, "F0"), 10f, 200f);
            GUILayout.EndVertical();
            GUILayout.BeginVertical(GUILayout.Width(250));
            zoomManual = Mathf.Clamp(FilaL("Zoom camara", zoomManual, 0.1f, "F2"), 0.25f, 5f);
            ajuste.radioGiro = Mathf.Clamp(FilaL("Radio giro suelo", ajuste.radioGiro, 5f, "F0"), 5f, 500f);
            ajuste.giroSueloMax = Mathf.Clamp(FilaL("Giro suelo max", ajuste.giroSueloMax, 2f, "F0"), 2f, 90f);
            ajuste.acelSuelo = Mathf.Clamp(FilaL("Acel. suelo", ajuste.acelSuelo, 1f, "F0"), 1f, 100f);
            GUILayout.EndVertical();
            GUILayout.BeginVertical(GUILayout.Width(250));
            ajuste.frenoSuelo = Mathf.Clamp(FilaL("Freno suelo", ajuste.frenoSuelo, 1f, "F0"), 1f, 200f);
            ajuste.nivelSuelo = Mathf.Clamp(FilaL("Toque suave", ajuste.nivelSuelo, 0.5f, "F1"), 0.5f, 100f);
            GUILayout.BeginHorizontal();
            if (GUILayout.Button(vientoActivo ? "VIENTO: ON" : "VIENTO: OFF", sBtn, GUILayout.Width(110), GUILayout.Height(20)))
            {
                vientoActivo = !vientoActivo;
                if (vientoActivo) { vientoDir = Random.value * 360f; vientoKmh = Random.Range(10f, 35f); }
            }
            GUILayout.Label(vientoActivo ? ((int)vientoDir + " gr  " + (int)vientoKmh + " km/h") : "sin viento", sTxt);
            GUILayout.EndHorizontal();
            GUILayout.EndVertical();
            GUILayout.EndHorizontal();
            if (nMinK != vMinK) ajuste.velMin = Mathf.Clamp(nMinK, 0f, 100000f) / 3.6f;   // solo si tocaste los botones
            if (nMaxK != vMaxK) ajuste.velMax = Mathf.Clamp(nMaxK, 0f, 100000f) / 3.6f;
        }
        else if (solapa == 1)
        {
            Aeropuertos.PestanaTorre(sTxt, sBtn, sTitulo);
        }
        else if (solapa == 2)
        {
            GUILayout.Label("SONIDOS", sTitulo);
            GUILayout.BeginHorizontal();
            GUILayout.Label("Volumen motor", sTxt, GUILayout.Width(100));
            volumenMotor = GUILayout.HorizontalSlider(volumenMotor, 0f, 1f, GUILayout.Width(220));
            GUILayout.Label(volumenMotor.ToString("F2"), sTxt);
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            GUILayout.BeginVertical(GUILayout.Width(380));
            Mezcla.Fila(Mezcla.GENERAL, sTxt); Mezcla.Fila(Mezcla.MOTOR_AVION, sTxt); Mezcla.Fila(Mezcla.MOTOR_AUTO, sTxt);
            GUILayout.EndVertical();
            GUILayout.BeginVertical(GUILayout.Width(380));
            Mezcla.Fila(Mezcla.RADIO, sTxt); Mezcla.Fila(Mezcla.MUSICA, sTxt); Mezcla.Fila(Mezcla.AZAFATA, sTxt);
            GUILayout.EndVertical();
            GUILayout.BeginVertical(GUILayout.Width(380));
            Mezcla.Fila(Mezcla.FX, sTxt); Mezcla.Fila(Mezcla.TORRE, sTxt);
            GUILayout.EndVertical();
            GUILayout.EndHorizontal();
        }
        else if (solapa == 4) Mapeo.PestanaControles(sTxt, sBtn, sTitulo);
        else if (solapa == 5) { if (Mapeo.PanelLugarAbierto) Mapeo.PestanaLugares(sTxt, sBtn, sTitulo); else Aeropuertos.PestanaDestinos(sTxt, sBtn, sTitulo); }   // el tablero de destinos; el cuadro de nombre solo aparece al marcar un lugar
        else if (solapa == 6) Carrera.PestanaCarrera(sTxt, sBtn, sTitulo);
        else if (solapa == 7) Hangar.PestanaAviones(rp.width - 12, rp.height - 8);
        else if (solapa == 8) Guerra.PestanaGuerra(sTxt, sBtn, sTitulo);
        else if (solapa == 9) LucesAvion.PestanaLuces(sTxt, sBtn, sTitulo);
        else if (solapa == 10) TrenAterrizaje.PestanaTren(sTxt, sBtn, sTitulo);
        else if (solapa == 11) Aeropuertos.PestanaTorre(sTxt, sBtn, sTitulo);
        else if (solapa == 12) Tienda.PestanaShopping(sTxt, sBtn, sTitulo);
        else if (solapa == 14) Teletransporte.Pestana(sTxt, sBtn, sTitulo);
        else if (solapa == 13) Tienda.PestanaMaletin(sTxt, sBtn, sTitulo);   // aeropuertos cercanos (la misma torre de Pagina 2, en pestana propia)
        else
        {
            // ---- Editor de orientación del modelo (CLAVAR guarda en ajustes_aviones.json) ----
            GUILayout.BeginHorizontal();
            GUILayout.BeginVertical(GUILayout.Width(190));
            GUILayout.Label("GIRAR (grados)", sTitulo);
            ajuste.rx = FilaL("Cabeceo X", ajuste.rx, paso, "F0");
            ajuste.ry = FilaL("Rumbo Y", ajuste.ry, paso, "F0");
            ajuste.rz = FilaL("Alabeo Z", ajuste.rz, paso, "F0");
            GUILayout.EndVertical();
            GUILayout.BeginVertical(GUILayout.Width(190));
            GUILayout.Label("MOVER (metros)", sTitulo);
            ajuste.ox = FilaL("Izq/Der", ajuste.ox, paso * 0.1f, "F1");
            ajuste.oy = FilaL("Arr/Abajo", ajuste.oy, paso * 0.1f, "F1");
            ajuste.oz = FilaL("Adel/Atras", ajuste.oz, paso * 0.1f, "F1");
            GUILayout.EndVertical();
            GUILayout.BeginVertical(GUILayout.Width(220));
            GUILayout.Label("TOQUE (grados)   Tamano " + ajuste.escala.ToString("F3"), sTitulo);
            GUILayout.BeginHorizontal();
            foreach (float p in new float[] { 1f, 5f, 15f, 90f })
            {
                var antes = GUI.backgroundColor;
                if (paso == p) GUI.backgroundColor = Color.green;
                if (GUILayout.Button(p + "°", sBtn, GUILayout.Width(46), GUILayout.Height(22))) paso = p;
                GUI.backgroundColor = antes;
            }
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Chico", sBtn, GUILayout.Width(70), GUILayout.Height(22))) ajuste.escala *= 0.9f;
            if (GUILayout.Button("Grande", sBtn, GUILayout.Width(70), GUILayout.Height(22))) ajuste.escala *= 1.1f;
            if (GUILayout.Button(pausado ? "PAUSADO" : "Pausar", sBtn, GUILayout.Width(70), GUILayout.Height(22))) pausado = !pausado;
            GUILayout.EndHorizontal();
            var verde = GUI.backgroundColor;
            GUI.backgroundColor = new Color(0.4f, 1f, 0.4f);
            if (GUILayout.Button("CLAVAR RESULTADO", sBtn, GUILayout.Width(214), GUILayout.Height(26))) mensaje = GuardarAjuste();
            GUI.backgroundColor = verde;
            GUILayout.EndVertical();
            GUILayout.BeginVertical();
            GUILayout.Label(mensaje, sTxt);
            GUILayout.EndVertical();
            GUILayout.EndHorizontal();
        }
        GUILayout.EndArea();
        tapaP.Dispose();

    fin_panel_bajo:
        // autoguardado: si cambió cualquier ajuste, se guarda solo 1 segundo después (no hay que repetirlo al empezar)
        string js = JsonUtility.ToJson(ajuste);
        if (ultimoJson == null) ultimoJson = js;
        else if (js != ultimoJson) { ultimoJson = js; guardarEn = Time.unscaledTime + 1f; string mj = ajuste.rx + "," + ajuste.ry + "," + ajuste.rz + "," + ajuste.escala + "," + ajuste.ox + "," + ajuste.oy + "," + ajuste.oz; if (mj != ultimoModelo) { ultimoModelo = mj; if (Mathf.Abs(cabeceoActual) < 3f && Mathf.Abs(bancoActual) < 3f) { AplicarAjuste(); RecalcularPie(); } } }

        AplicarAjuste();
    }

    // ---- apps de la tablet: Sensibilidad y Modelo en una sola columna ----
    public static void AppSensibilidad() { if (Inst != null && ajuste != null) Inst.PanelSensibilidadTablet(); else GUILayout.Label("Sin datos todavia"); }
    public static void AppModelo() { if (Inst != null && ajuste != null) Inst.PanelModeloTablet(); else GUILayout.Label("Sin datos todavia"); }
    void PanelSensibilidadTablet()
    {
        float vMinK = ajuste.velMin * 3.6f, vMaxK = ajuste.velMax * 3.6f, nMinK = vMinK, nMaxK = vMaxK;
        GUILayout.Label("SENSIBILIDAD", sTitulo);
        ajuste.giro = Mathf.Clamp(FilaL("Giro", ajuste.giro, 0.05f, "F2"), 0.1f, 2f);
        ajuste.angulo = Mathf.Clamp(FilaL("Angulo", ajuste.angulo, 2f, "F0"), 10f, 80f);
        if (esHeli) ajuste.vertical = Mathf.Clamp(FilaL("Subida (m/s)", ajuste.vertical, 2f, "F0"), 2f, 40f);
        else ajuste.vertical = Mathf.Clamp(FilaL("Cabeceo (nariz)", ajuste.vertical, 2f, "F0"), 10f, 80f);
        nMinK = FilaL("Vel. min km/h", vMinK, 10f, "F0");
        nMaxK = FilaL("Vel. max km/h", vMaxK, 10f, "F0");
        ajuste.velAnim = Mathf.Clamp(FilaL("Vel. animacion", ajuste.velAnim, 0.1f, "F1"), 0.1f, 3f);
        ajuste.viento = Mathf.Clamp(FilaL("Viento", ajuste.viento, 0.1f, "F1"), 0f, 3f);
        ajuste.camVel = Mathf.Clamp(FilaL("Vel. camara", ajuste.camVel, 10f, "F0"), 10f, 200f);
        zoomManual = Mathf.Clamp(FilaL("Zoom camara", zoomManual, 0.1f, "F2"), 0.25f, 5f);
        ajuste.radioGiro = Mathf.Clamp(FilaL("Radio giro suelo", ajuste.radioGiro, 5f, "F0"), 5f, 500f);
        ajuste.giroSueloMax = Mathf.Clamp(FilaL("Giro suelo max", ajuste.giroSueloMax, 2f, "F0"), 2f, 90f);
        ajuste.acelSuelo = Mathf.Clamp(FilaL("Acel. suelo", ajuste.acelSuelo, 1f, "F0"), 1f, 100f);
        ajuste.frenoSuelo = Mathf.Clamp(FilaL("Freno suelo", ajuste.frenoSuelo, 1f, "F0"), 1f, 200f);
        ajuste.nivelSuelo = Mathf.Clamp(FilaL("Toque suave", ajuste.nivelSuelo, 0.5f, "F1"), 0.5f, 100f);
        GUILayout.BeginHorizontal();
        if (GUILayout.Button(vientoActivo ? "VIENTO: ON" : "VIENTO: OFF", sBtn, GUILayout.Width(110), GUILayout.Height(22)))
        {
            vientoActivo = !vientoActivo;
            if (vientoActivo) { vientoDir = Random.value * 360f; vientoKmh = Random.Range(10f, 35f); }
        }
        GUILayout.Label(vientoActivo ? ((int)vientoDir + " gr  " + (int)vientoKmh + " km/h") : "sin viento", sTxt);
        GUILayout.EndHorizontal();
        if (nMinK != vMinK) ajuste.velMin = Mathf.Clamp(nMinK, 0f, 100000f) / 3.6f;
        if (nMaxK != vMaxK) ajuste.velMax = Mathf.Clamp(nMaxK, 0f, 100000f) / 3.6f;
    }
    void PanelModeloTablet()
    {
        GUILayout.Label("GIRAR (grados)", sTitulo);
        ajuste.rx = FilaL("Cabeceo X", ajuste.rx, paso, "F0");
        ajuste.ry = FilaL("Rumbo Y", ajuste.ry, paso, "F0");
        ajuste.rz = FilaL("Alabeo Z", ajuste.rz, paso, "F0");
        GUILayout.Label("MOVER (metros)", sTitulo);
        ajuste.ox = FilaL("Izq/Der", ajuste.ox, paso * 0.1f, "F1");
        ajuste.oy = FilaL("Arr/Abajo", ajuste.oy, paso * 0.1f, "F1");
        ajuste.oz = FilaL("Adel/Atras", ajuste.oz, paso * 0.1f, "F1");
        GUILayout.Label("TOQUE (grados)   Tamano " + ajuste.escala.ToString("F3"), sTitulo);
        GUILayout.BeginHorizontal();
        foreach (float p in new float[] { 1f, 5f, 15f, 90f })
        {
            var antes = GUI.backgroundColor;
            if (paso == p) GUI.backgroundColor = Color.green;
            if (GUILayout.Button(p + "°", sBtn, GUILayout.Width(46), GUILayout.Height(22))) paso = p;
            GUI.backgroundColor = antes;
        }
        GUILayout.EndHorizontal();
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Chico", sBtn, GUILayout.Width(70), GUILayout.Height(22))) ajuste.escala *= 0.9f;
        if (GUILayout.Button("Grande", sBtn, GUILayout.Width(70), GUILayout.Height(22))) ajuste.escala *= 1.1f;
        if (GUILayout.Button(pausado ? "PAUSADO" : "Pausar", sBtn, GUILayout.Width(70), GUILayout.Height(22))) pausado = !pausado;
        GUILayout.EndHorizontal();
        var verde0 = GUI.backgroundColor; GUI.backgroundColor = new Color(0.4f, 1f, 0.4f);
        if (GUILayout.Button("CLAVAR RESULTADO", sBtn, GUILayout.Width(214), GUILayout.Height(26))) mensaje = GuardarAjuste();
        GUI.backgroundColor = verde0;
        GUILayout.Label(mensaje, sTxt);
    }

    static float escalaDefecto = 0.3f;
    // ---- helicopteros (Godot): mismo control que el avion + subir/bajar vertical a velocidad fija; sin modo tierra ----
    const float VELOCIDAD_VERTICAL_HELI = 20f;   // m/s (VELOCIDAD_VERTICAL_HELICOPTERO de Godot; rango del panel 2..40)
    const float CABECEO_MAX_HELI = 40f;          // el helicoptero siempre usa 40 grados de cabeceo (CABECEO_MAXIMO)
    const float MARGEN_SOBRE_PISO_HELI = 0.3f;   // metros (MARGEN_SOBRE_PISO)
    static bool esHeli; public static bool EsHeli { get { return esHeli; } } public static float VelMs; static float ryDefecto = 180f;
    public static bool SinAvion { get { return ocultarAvion; } }
    public static void AlternarSinAvion() { ocultarAvion = !ocultarAvion; Mapeo.Cartel(ocultarAvion ? "Vista sin avion: ON" : "Vista sin avion: OFF", 1.5f); }   // boton del ojito (junto al ?)
    static bool ocultarAvion; static bool ocultoAplicado;   // false = el modelo esta visible; se pone en false al cargar un modelo nuevo
    static Transform[] rotPrinc; static float[] rotSentido; static Transform rotCola;
    const float VELOCIDAD_DESPEGUE_MS = 50f / 3.6f;   // despega al pasar 50 km/h reales (antes 120 m/s = 432 km/h, imposible en pista corta)
    public static bool enAuto; public static string velAutoTxt = "0 km/h";
    public bool EnTierra { get { return enTierra; } }
    public float PieM { get { return pieM; } }
    public float AlturaSobrePiso { get { return hSobrePiso; } }
    // arranca ya volando (usado por el selector de aeropuertos del mapa): velocidad de crucero suave, sin quedar "en tierra"
    public void IniciarEnAire() { velocidad = esHeli ? 0f : Mathf.Clamp(velocidadInicialMs, ajuste.velMin, ajuste.velMax); enTierra = false; tiempoEnAire = 3f; tiempoSinToque = 3f; reversaActiva = false; intensidadFreno = 0f; giroEntrada = 0f; alturaPrev = -1f; }
    public void FijarVelocidad(float ms) { velocidad = Mathf.Clamp(ms, ajuste != null ? ajuste.velMin : 0f, ajuste != null ? ajuste.velMax : 417f); }   // la usa la Escolta para arrancar a la par del avion presidencial
    public void Reanudar() { velocidad = 0f; enTierra = true; reversaActiva = false; intensidadFreno = 0f; bloqueoDespegue = 4f; giroEntrada = 0f; alturaPrev = -1f; }

    // Aparece el avion elegido (lo llama el selector). Si ya habia uno volando, el nuevo sale en el mismo lugar.
    public static async void Aparecer(SelectorAvion.Tipo t)
    {
        var geo = Object.FindFirstObjectByType<CesiumGeoreference>();
        if (geo == null) return;
        nombreAvion = t.nombre;

        Vector3 pos = Vector3.zero; Quaternion rot = Quaternion.identity; bool enMundo = false;
        var previo = Object.FindFirstObjectByType<AvionDeCarton>();
        if (previo != null)
        {
            pos = previo.transform.position;
            Vector3 f = previo.transform.forward; f.y = 0f;
            rot = f.sqrMagnitude > 0.01f ? Quaternion.LookRotation(f.normalized, Vector3.up) : Quaternion.identity;
            enMundo = true;
            Destroy(previo.gameObject);
        }

        var go = new GameObject("AvionDeCarton");
        go.transform.SetParent(geo.transform, false);
        if (enMundo) go.transform.SetPositionAndRotation(pos, rot);
        else { go.transform.localPosition = new Vector3(0f, 150f, 0f); go.transform.localRotation = Quaternion.Euler(0f, 130f, 0f); }

        nombreModelo = t.recurso ? "Avion" : t.nombre;   // el Emirates conserva su ajuste ya clavado ("Avion")
        escalaDefecto = t.escala;
        esHeli = t.helicoptero; ryDefecto = t.ry; rotPrinc = null; rotCola = null; rotSentido = null;
        var pivote = new GameObject("ModeloAvion");
        pivote.transform.SetParent(go.transform, false);
        GameObject modelo = null;
        SelectorAvion.cargando = "Cargando " + t.nombre + "..."; SelectorAvion.cargandoDesde = Time.unscaledTime;
        try
        {
            if (t.recurso)
            {
                var prefab = Resources.Load<GameObject>("Avion");
                if (prefab != null) modelo = Instantiate(prefab, pivote.transform);
            }
            else
            {
                var gltf = new GLTFast.GltfImport();
                bool ok = await gltf.Load(new System.Uri(t.archivo).AbsoluteUri);
                if (go == null) { SelectorAvion.cargando = ""; return; }
                if (ok)
                {
                    modelo = new GameObject("modelo");
                    modelo.transform.SetParent(pivote.transform, false);
                    await gltf.InstantiateMainSceneAsync(modelo.transform);
                }
                else Debug.LogWarning("No pude cargar " + t.archivo);
            }
        }
        catch (System.Exception e) { Debug.LogWarning("Error cargando avion: " + e.Message); }
        if (go == null) { SelectorAvion.cargando = ""; return; }

        if (modelo != null)
        {
            modelo.transform.localPosition = Vector3.zero;
            modelo.transform.localRotation = Quaternion.identity;
            modelo.transform.localScale = Vector3.one;
            var rs = modelo.GetComponentsInChildren<Renderer>();
            centroLocal = Vector3.zero;
            if (rs.Length > 0)
            {
                Bounds b = rs[0].bounds;
                foreach (var r in rs) b.Encapsulate(r.bounds);
                centroLocal = pivote.transform.InverseTransformPoint(b.center);
            }
            modeloT = modelo.transform; ocultoAplicado = false;
            if (t.helicoptero)
            {
                // rotores: se buscan por nombre dentro del modelo (los giramos en Update)
                var trs = modelo.GetComponentsInChildren<Transform>(true);
                var lista = new System.Collections.Generic.List<Transform>();
                if (t.rotoresPrinc != null)
                    foreach (var nom in t.rotoresPrinc) foreach (var tr in trs) if (tr.name == nom) { lista.Add(tr); break; }
                rotPrinc = lista.ToArray();
                rotSentido = new float[rotPrinc.Length];
                for (int i = 0; i < rotSentido.Length; i++) rotSentido[i] = (i % 2 == 0) ? 1f : -1f;   // rotores dobles (Ka-27): giran en sentidos opuestos
                if (!string.IsNullOrEmpty(t.rotorCola)) foreach (var tr in trs) if (tr.name == t.rotorCola) { rotCola = tr; break; }
                Debug.Log("[Heli] rotores principales: " + rotPrinc.Length + ", cola: " + (rotCola != null));
            }
            CargarAjuste();
            AplicarAjuste();
        }
        else
        {
            modeloT = null;
            Parte(go.transform, "Fuselaje", new Vector3(0, 0, 0), new Vector3(1.4f, 1.4f, 12f), new Color(0.9f, 0.9f, 0.9f));
            Parte(go.transform, "Nariz", new Vector3(0, 0, 6.5f), new Vector3(1.1f, 1.1f, 2f), new Color(0.85f, 0.2f, 0.2f));
            Parte(go.transform, "Alas", new Vector3(0, 0, 0.5f), new Vector3(12f, 0.2f, 2.2f), new Color(0.2f, 0.45f, 0.85f));
            Parte(go.transform, "ColaHorizontal", new Vector3(0, 0, -5.5f), new Vector3(4.5f, 0.15f, 1.4f), new Color(0.2f, 0.45f, 0.85f));
            Parte(go.transform, "ColaVertical", new Vector3(0, 1.2f, -5.5f), new Vector3(0.2f, 2f, 1.6f), new Color(0.85f, 0.2f, 0.2f));
            CargarAjuste();
        }

        // sonido del motor: el del Emirates viene en Resources; los demas se leen del Godot (FX), sin copiar
        AudioClip clip = null;
        if (t.recurso) clip = Resources.Load<AudioClip>("MotorAvion");
        else if (System.IO.File.Exists(t.sonido))
        {
            try
            {
                using (var req = UnityEngine.Networking.UnityWebRequestMultimedia.GetAudioClip(new System.Uri(t.sonido).AbsoluteUri, AudioType.MPEG))
                {
                    var op = req.SendWebRequest();
                    while (!op.isDone) await Awaitable.NextFrameAsync();
                    if (req.result == UnityEngine.Networking.UnityWebRequest.Result.Success) clip = UnityEngine.Networking.DownloadHandlerAudioClip.GetContent(req);
                }
            }
            catch (System.Exception e) { Debug.LogWarning("Error cargando sonido: " + e.Message); }
        }
        if (go == null) { SelectorAvion.cargando = ""; return; }
        if (clip != null)
        {
            var fuente = go.AddComponent<AudioSource>();
            fuente.clip = clip; fuente.loop = true; fuente.spatialBlend = 0f; fuente.playOnAwake = false;
            fuente.Play();
        }

        go.AddComponent<CesiumGlobeAnchor>();   // lo mantiene pegado a la curvatura de la Tierra
        go.AddComponent<CesiumOriginShift>();   // mueve el origen del mundo mientras volas
        go.AddComponent<AvionDeCarton>();
        SelectorAvion.cargando = "";
    }

    static void Parte(Transform padre, string nombre, Vector3 pos, Vector3 escala, Color color)
    {
        var p = GameObject.CreatePrimitive(PrimitiveType.Cube);
        p.name = nombre;
        var col = p.GetComponent<Collider>();
        if (col != null) Destroy(col);
        p.transform.SetParent(padre, false);
        p.transform.localPosition = pos;
        p.transform.localScale = escala;
        p.GetComponent<Renderer>().material.color = color;
    }

    // altura desde el centro del avión hasta lo más bajo del modelo (ruedas), para apoyarlo en el piso
    void RecalcularPie()
    {
        var rs = GetComponentsInChildren<Renderer>();
        if (rs.Length == 0) { pieM = 1f; return; }
        float min = float.MaxValue; Vector3 up = Vector3.up;
        foreach (var r in rs)
        {
            var b = r.bounds;
            for (int i = 0; i < 8; i++)
            {
                var c = b.center + Vector3.Scale(b.extents, new Vector3((i & 1) == 0 ? -1f : 1f, (i & 2) == 0 ? -1f : 1f, (i & 4) == 0 ? -1f : 1f));
                min = Mathf.Min(min, Vector3.Dot(c - transform.position, up));
            }
        }
        pieM = Mathf.Max(0f, -min);
    }

    void Start()
    {
        ancla = GetComponent<CesiumGlobeAnchor>();
        velocidad = velocidadInicialMs;
        RecalcularPie();
        if (ajuste != null) volumenMotor = ajuste.volumen;
    }

    void Update()
    {
        float dt = Time.deltaTime;
        if (dt <= 0f) return;
        VelMs = velocidad;
        if (LucesAvion.Congelado) return;   // pestana Luces abierta: el avion se queda quieto (sin viento ni avance) para ubicar las luces con precision
        fps = Mathf.Lerp(fps, 1f / dt, 0.1f);
        // "ojito" (tecla V, como en Godot): oculta solo el DIBUJO del avion; la camara y el vuelo siguen igual
        if (Controles.Flanco("ver_sin_avion")) { ocultarAvion = !ocultarAvion; Mapeo.Cartel(ocultarAvion ? "Vista sin avion: ON" : "Vista sin avion: OFF", 1.5f); }
        if (modeloT != null && ocultoAplicado != ocultarAvion)
        {
            foreach (var rr in modeloT.GetComponentsInChildren<Renderer>(true)) rr.enabled = !ocultarAvion;
            ocultoAplicado = ocultarAvion;
        }
        if (pausado || enAuto) return;   // avión congelado mientras se ajusta en el editor

        var gp = Gamepad.current;
        var kb = Keyboard.current;
        if (Controles.escribiendo) { gp = null; kb = null; }   // escribiendo un nombre: el avion no reacciona al teclado/joystick

        // --- stick izquierdo con zona muerta radial ---
        Vector2 stick = gp != null ? gp.leftStick.ReadValue() : Vector2.zero;
        float mag = stick.magnitude;
        if (mag < zonaMuerta) stick = Vector2.zero;
        else stick = stick.normalized * ((mag - zonaMuerta) / (1f - zonaMuerta));

        float signoJoy = invertirCabeceoJoystick ? -1f : 1f;
        float cab = stick.y * signoJoy;   // joystick: invertido a pedido de Pablo
        float ala = stick.x;              // derecha = alabeo a la derecha

        // cabeceo y alabeo por botones/teclas: acciones reasignables (menu "Controles"); de fabrica cruceta y flechas
        if (Controles.Activa("cabeceo_arriba")) cab += 1f;
        if (Controles.Activa("cabeceo_abajo")) cab -= 1f;
        if (Controles.Activa("alabeo_derecha")) ala += 1f;
        if (Controles.Activa("alabeo_izquierda")) ala -= 1f;
        cab = Mathf.Clamp(cab, -1f, 1f);
        ala = Mathf.Clamp(ala, -1f, 1f);

        if (ajuste == null) ajuste = AjustePorDefecto();
        if (!esHeli) Torre.PilotoEje(ref cab, ref ala, dt, enTierra, ajuste.vertical, ajuste.angulo, ajuste.giro);   // piloto automatico de la torre (altura / rumbo)

        // --- velocidad (como Godot): W/RT acelera 40 m/s², S/LB/LT frena; sin inercia, sin gravedad ---
        // acelerar / frenar / emergencia: acciones reasignables (de fabrica LT o W, LB o S, Select o D)
        bool acel = Controles.Activa("acelerar"), fren = Controles.Activa("frenar");
        // Freno de emergencia (Godot): De un solo golpe a 200, solo si vas mas rapido.
        bool emergencia = Controles.Activa("freno_emergencia");
        if (emergencia && !emergenciaAnt && velocidad > TopeEmergencia()) { velocidad = TopeEmergencia(); aviso = "Freno de emergencia: " + Mathf.RoundToInt(TopeEmergencia() * 3.6f) + " km/h"; avisoHasta = Time.time + 2f; }
        emergenciaAnt = emergencia;
        // Reversa/pushback (Godot): boton X. Prende y apaga con cada apretada, solo en tierra; 3 m/s fijos, se puede doblar.
        bool reversaBoton = Controles.Activa("reversa_pushback");
        if (reversaBoton && !reversaAnt && enTierra)
        {
            reversaActiva = !reversaActiva;
            aviso = reversaActiva ? "Reversa/pushback ACTIVADA - apreta de nuevo para parar" : "Reversa/pushback apagada"; avisoHasta = Time.time + 2f;
        }
        reversaAnt = reversaBoton;
        if (!enTierra) reversaActiva = false;
        // Freno progresivo (Godot): tecla B o boton A. Una apretada = 0.5/s; cada apretada extra suma 1/s (tope 20/s).
        // En tierra y a mas de 50, un toque frena a 20/s solo hasta llegar a 50.
        bool frenoGradual = Controles.Activa("freno_gradual");
        if (frenoGradual && !frenoGradualAnt)
        {
            if (enTierra && velocidad > 50f) { intensidadFreno = 20f; frenoRapidoTierra = true; }
            else intensidadFreno = Mathf.Clamp(intensidadFreno > 0f ? intensidadFreno + 1f : 0.5f, 0f, 20f);
        }
        frenoGradualAnt = frenoGradual;
        if (reversaActiva) { velocidad = 0f; intensidadFreno = 0f; }   // mientras empuja la grua, el acelerador y el freno no hacen nada
        else if (acel) { intensidadFreno = 0f; frenoRapidoTierra = false; velocidad = Mathf.Min(ajuste.velMax, velocidad + (enTierra ? ajuste.acelSuelo : ACELERACION) * FACTOR_RITMO * dt); }   // en tierra acel_suelo = 18 (Godot)
        else if (fren) velocidad = Mathf.Max(enTierra ? 0f : ajuste.velMin, velocidad - (enTierra ? ajuste.frenoSuelo : ACELERACION) * FACTOR_RITMO * dt);   // en tierra freno_suelo = 25 (Godot)
        else if (intensidadFreno > 0f)
        {
            velocidad = Mathf.Max(0f, velocidad - intensidadFreno * dt);
            if (frenoRapidoTierra && velocidad <= 50f) { velocidad = 50f; intensidadFreno = 0f; frenoRapidoTierra = false; }
            if (velocidad <= 0f) intensidadFreno = 0f;
        }
        if (!esHeli) velocidad = Torre.PilotoVel(velocidad, enTierra);   // piloto automatico: mantiene la velocidad
        if (!esHeli && !enTierra && Combustible.Vacio) velocidad = Mathf.Max(ajuste.velMin, velocidad - 60f * dt);   // sin combustible: no hay empuje, solo planea
        acelerador = Mathf.Clamp01(velocidad / Mathf.Max(1f, ajuste.velMax));

        // --- cabeceo y alabeo: van al tope en 0,65 s y ahí quedan (sin autonivelado), como Godot ---
        // despegue automatico de Godot: en tierra, al llegar a max(120, vel_despegue) y pasado el bloqueo de 4 s
        if (enTierra)
        {
            bloqueoDespegue = Mathf.Max(0f, bloqueoDespegue - dt);
            if (velocidad >= VELOCIDAD_DESPEGUE_MS && bloqueoDespegue <= 0f && (Controles.Activa("acelerar") || cab > 0.3f)) { enTierra = false; tiempoEnAire = 0f; tiempoSinToque = 2f; }   // _despegar_de_pista
        }
        if (!enTierra)
        {
        float cabMax = esHeli ? CABECEO_MAX_HELI : ajuste.vertical;   // helicoptero: cabeceo fijo 40; su "vertical" es la subida/bajada
        float cabObj = cab * cabMax;   // + = nariz arriba
        float bancoObj = ala * ajuste.angulo;   // + = ala derecha abajo
        float cabAnt = cabeceoActual;
        if (cabObj != 0f)
            cabeceoActual = Mathf.MoveTowards(cabeceoActual, cabObj, (cabMax / RAMPA_CONTROL) * ajuste.velAnim * 0.5f * dt);
        float dCab = cabeceoActual - cabAnt;
        if (Mathf.Abs(dCab) > 0.0001f) transform.Rotate(-dCab, 0f, 0f, Space.Self);

        float bAnt = bancoActual;
        if (bancoObj != 0f)
            bancoActual = Mathf.MoveTowards(bancoActual, bancoObj, (ajuste.angulo / RAMPA_CONTROL) * ajuste.velAnim * 0.5f * dt);
        float dBanco = bancoActual - bAnt;
        if (Mathf.Abs(dBanco) > 0.0001f) transform.Rotate(0f, 0f, -dBanco, Space.Self);

        // el rumbo sale SOLO de cuánto está inclinado (banco * giro)
        if (Mathf.Abs(bancoActual) > 0.5f)
            transform.Rotate(Vector3.up, bancoActual * ajuste.giro * dt, Space.World);

        // --- oscilación por viento (siempre un poco; más fuerte con el botón de viento) ---
        faseViento += dt;
        float intensidad = (BASE_OSCILACION_VIENTO + (vientoActivo ? vientoKmh / 35f : 0f)) * ajuste.viento;
        float offRoll = Mathf.Sin(faseViento * Mathf.PI * 2f * 0.35f) * 3f * intensidad;
        float offCab = Mathf.Sin(faseViento * Mathf.PI * 2f * 0.5f + 1.3f) * 1.2f * intensidad;
        transform.Rotate(-(offCab - cabVientoPrev), 0f, -(offRoll - rollVientoPrev), Space.Self);
        rollVientoPrev = offRoll; cabVientoPrev = offCab;
        }
        else
        {
            // EN TIERRA: no gira como en el aire (no se hunden las alas). Se nivela solo y rota sobre su eje.
            Vector3 up = Vector3.up;
            Vector3 fr = transform.forward; fr -= Vector3.Dot(fr, up) * up;
            if (fr.sqrMagnitude < 0.01f) fr = camAdelUlt;
            Quaternion nivelada = Quaternion.LookRotation(fr.normalized, up);
            // toque suave: se endereza de a poco (frena al acercarse), no de golpe. Valor alto = rigido como Godot (100), bajo = blando.
            float angNivel = Quaternion.Angle(transform.rotation, nivelada);
            float pasoNivel = Mathf.Max(angNivel * (1f - Mathf.Exp(-ajuste.nivelSuelo * dt)), Mathf.Min(angNivel, 6f * dt));
            transform.rotation = Quaternion.RotateTowards(transform.rotation, nivelada, pasoNivel);
            cabeceoActual = 0f; bancoActual = 0f; rollVientoPrev = 0f; cabVientoPrev = 0f;
            // giro como un auto: velocidad / radio, con tope; parado no gira; alrededor de la vertical real
            giroEntrada = Mathf.MoveTowards(giroEntrada, ala, (Mathf.Abs(ala) > Mathf.Abs(giroEntrada) ? 1.8f : 3.5f) * dt);
            float vG = reversaActiva ? VELOCIDAD_REVERSA : velocidad;
            float tasaGiro = Mathf.Min(ajuste.giroSueloMax, vG / Mathf.Max(1f, ajuste.radioGiro) * Mathf.Rad2Deg);
            // realismo (pedido de Pablo): la rueda de nariz pierde autoridad con la velocidad (a 50 km/h ya no vira "como pelotita") y la guinada tiene inercia
            float autoridad = 1f / (1f + (vG / 20f) * (vG / 20f));
            float yawObj = tasaGiro * giroEntrada * autoridad;
            yawSuelo = Mathf.Lerp(yawSuelo, yawObj, 1f - Mathf.Exp(-3f * dt));
            // el avion pivota alrededor del tren principal (un poco detras del centro), no alrededor de su centro: la nariz barre y la cola casi no se mueve
            float dTren = LargoModelo() * 0.12f;
            Vector3 gTren = transform.position - transform.forward * dTren;
            Quaternion rotG = Quaternion.AngleAxis(yawSuelo * dt, up);
            transform.position = gTren + rotG * (transform.position - gTren);
            transform.rotation = rotG * transform.rotation;
        }

        // --- sonido del motor (mismos valores que Godot: volumen 0.5 -> lerp(-40,-6 dB), pitch 0.85..1.25) ---
        var audio = GetComponent<AudioSource>();
        if (audio != null)
        {
            audio.volume = Mathf.Pow(10f, Mathf.Lerp(-40f, -6f, volumenMotor) / 20f) * Mezcla.De(Mezcla.MOTOR_AVION);
            audio.pitch = Mathf.Lerp(0.85f, 1.25f, Mathf.Clamp01(velocidad / Mathf.Max(1f, ajuste.velMax)));
        }

        // --- helicoptero: subir/bajar en linea recta sobre la vertical real, velocidad fija mientras se mantiene el boton (Godot) ---
        if (esHeli)
        {
            bool sube = Controles.Activa("vertical_arriba");   // de fabrica R/O o RT
            bool baja = Controles.Activa("vertical_abajo");    // de fabrica F/L o RB
            if (sube) transform.position += Vector3.up * ajuste.vertical * dt;
            else if (baja) transform.position -= Vector3.up * ajuste.vertical * dt;
            // rotores: giran siempre; un poco mas rapido con el acelerador. Los dobles giran en sentidos opuestos.
            float k = 0.7f + 0.3f * acelerador;
            if (modeloT != null)
            {
                if (rotPrinc != null) for (int i = 0; i < rotPrinc.Length; i++) if (rotPrinc[i] != null) rotPrinc[i].Rotate(modeloT.up, 720f * k * rotSentido[i] * dt, Space.World);
                if (rotCola != null) rotCola.Rotate(modeloT.right, 1800f * k * dt, Space.World);
            }
        }

        // --- avance ---
        if (reversaActiva) transform.Translate(Vector3.back * VELOCIDAD_REVERSA * dt, Space.Self);   // empujado hacia atras, 3 m/s fijos
        transform.Translate(Vector3.forward * velocidad * dt, Space.Self);

        // --- PISO: port literal de _limitar_piso de Godot (con histeresis) ---
        Vector3 vArriba = Vector3.up;
        RaycastHit golpe;
        Vector3 puntoSuelo = Vector3.zero;
        bool haySuelo = Physics.Raycast(transform.position + vArriba * 20000f, -vArriba, out golpe, 40000f, ~0, QueryTriggerInteraction.Ignore);
        sueloDeRespaldo = false;
        if (haySuelo) puntoSuelo = golpe.point;
        if (haySuelo && !esHeli && Vector3.Dot(transform.position - puntoSuelo, vArriba) < 60f) puntoSuelo = ApoyoPlano(puntoSuelo);   // las alas y la cola no se hunden en terreno irregular
        if (!haySuelo) { haySuelo = Aeropuertos.SueloPista(transform.position, out puntoSuelo); sueloDeRespaldo = haySuelo; }   // sin colision de terreno todavia (mosaicos sin cargar): usa la altura trackeada de la pista cercana
        if (haySuelo)
        {
            float altura = Vector3.Dot(transform.position - puntoSuelo, vArriba);
            float tren = pieM;
            hSobrePiso = altura - tren;
            if (esHeli)
            {
                // helicoptero (Godot): nunca entra en modo tierra; si baja de 0,3 m sobre el piso, se lo sube justo a esa altura
                enTierra = false; alturaPrev = altura;
                if (altura < tren + MARGEN_SOBRE_PISO_HELI) transform.position = puntoSuelo + vArriba * (tren + MARGEN_SOBRE_PISO_HELI);
            }
            else {
            float vVert = 0f;
            if (alturaPrev >= 0f && dt > 0f) vVert = (altura - alturaPrev) / dt;
            if (Mathf.Abs(vVert) > 400f) vVert = 0f;   // salto enorme = teletransporte, no caida
            alturaPrev = altura;
            tiempoSinToque = Mathf.Max(0f, tiempoSinToque - dt);
            if (!enTierra)
            {
                if (altura > tren + 3f) tiempoEnAire += dt;
                if (altura <= tren + 0.5f && tiempoSinToque <= 0f && vVert <= 0.5f)
                {
                    if (tiempoEnAire >= 2f) { enTierra = true; bloqueoDespegue = 4f; tiempoEnAire = 0f; giroEntrada = 0f; ToqueVV = vVert; ToqueId++; }   // _tocar_tierra (guarda la velocidad vertical para calificar el toque)
                    else transform.position = puntoSuelo + vArriba * tren;   // sin haber volado: solo que no se entierre
                }
                else if (altura < tren) transform.position = puntoSuelo + vArriba * tren;   // recien despegando: que no se entierre
            }
            if (enTierra)
            {
                if (altura > tren + 40f)
                {
                    // salir de tierra exige un salto de mas de 40 m que dure mas de 0.2 s (un cuadro ruidoso no alcanza)
                    tiempoSosp += dt;
                    if (tiempoSosp > 0.2f) { enTierra = false; tiempoSosp = 0f; }
                }
                else
                {
                    tiempoSosp = 0f;
                    // correccion de altura gradual, nunca teletransporta (tope 8 m/s)
                    float err = tren - altura, tope = 8f * dt;
                    float corr = Mathf.Clamp(err * Mathf.Min(1f, 4f * dt), -tope, tope);
                    transform.position += vArriba * corr;
                }
            }
            }   // fin del else (no helicoptero)
        }
    }

    public static float ToqueVV; public static int ToqueId;   // ultimo aterrizaje: velocidad vertical al tocar y contador (la torre lo califica)
    public float RadioModelo { get { return LargoModelo() * 0.5f; } }

    // piso plano bajo el avion: mide el terreno en el centro, las dos puntas de ala, la nariz y la cola, y apoya sobre el punto MAS ALTO
    // (con tope de 3 m) para que el terreno irregular (dunas de Villa Gesell, etc.) no se coma las alas
    float proxApoyo; Vector3 apoyoUlt; bool apoyoHay;
    Vector3 ApoyoPlano(Vector3 centro)
    {
        if (Time.time < proxApoyo && apoyoHay) { return new Vector3(centro.x, Mathf.Max(centro.y, apoyoUlt.y), centro.z); }
        proxApoyo = Time.time + 0.1f; float m = LargoModelo(), ala = m * 0.45f, largo = m * 0.4f; float maxY = centro.y;
        Vector3 d = transform.right, f = transform.forward; d.y = 0f; f.y = 0f; if (d.sqrMagnitude < 0.01f || f.sqrMagnitude < 0.01f) { apoyoHay = false; return centro; } d.Normalize(); f.Normalize();
        Vector3[] of = { d * ala, -d * ala, f * largo, -f * largo };
        for (int i = 0; i < of.Length; i++)
        {
            RaycastHit h; Vector3 o = transform.position + of[i]; o.y = centro.y + 200f;
            if (Physics.Raycast(o, Vector3.down, out h, 600f, ~0, QueryTriggerInteraction.Ignore) && h.point.y > maxY) maxY = h.point.y;
        }
        maxY = Mathf.Min(maxY, centro.y + 3f); apoyoUlt = new Vector3(centro.x, maxY, centro.z); apoyoHay = true;
        return apoyoUlt;
    }

    float yawSuelo, largoModelo;
    float LargoModelo()
    {
        if (largoModelo > 1f) return largoModelo;
        var rs = GetComponentsInChildren<Renderer>(); if (rs.Length == 0) return 8f;
        Bounds b = rs[0].bounds; foreach (var r in rs) b.Encapsulate(r.bounds);
        largoModelo = Mathf.Clamp(Mathf.Max(b.size.x, b.size.z), 4f, 80f); return largoModelo;
    }

    void LateUpdate()
    {
        if (guardarEn > 0f && Time.unscaledTime > guardarEn) { guardarEn = -1f; GuardarAjuste(); }
        if (enAuto) return;   // manejando el auto: la camara la lleva MapaAuto
        if (cam == null) cam = Camera.main;
        if (cam == null) return;
        if (cam.farClipPlane != camaraLejos) { cam.nearClipPlane = camaraCerca; cam.farClipPlane = camaraLejos; }

        // Cámara como la de Godot (_actualizar_camara): detrás del avión según el rumbo HORIZONTAL,
        // arriba según la vertical real, y mirando al avión. NO gira con el cabeceo ni el alabeo,
        // así se ve cómo el avión inclina la nariz (y no parece un ascensor).
        Vector3 arriba = Vector3.up;
        Vector3 adel = transform.forward;
        adel -= Vector3.Dot(adel, arriba) * arriba;
        if (adel.sqrMagnitude > 0.0001f) { adel.Normalize(); camAdelUlt = adel; } else adel = camAdelUlt;
        // Distancia FIJA (sin zoom automático por altura: achicaba el avión al pasar los 100 m).
        // Rueda del mouse = acercar/alejar. Stick derecho = girar la vista 360° (vuelve solo al centro al soltar).
        ZoomPorModelo();
        var ratonCam = Mouse.current;
        // si el mouse esta sobre el panel de abajo o el mapa, la rueda es del scroll de esas pestanas (no del zoom del avion)
        bool ratonSobrePanel = false;
        if (ratonCam != null)
        {
            Vector2 mp = ratonCam.position.ReadValue(); float hh = Screen.height; float yG = hh - mp.y;
            float ext = solapa == 7 ? 190f : 0f;
            ratonSobrePanel = (mp.x >= 260f && yG >= hh - 200f - ext) || (mp.x <= 355f && yG >= 205f && yG <= 560f) || Tienda.PanelLateral.rect.Contains(new Vector2(mp.x, yG)) || Disposicion.SobreAlgo(new Vector2(mp.x, yG)) || Tablet.Sobre(new Vector2(mp.x, yG));
        }
        if (ratonCam != null && !ratonSobrePanel)
        {
            float rueda = ratonCam.scroll.ReadValue().y;
            if (rueda != 0f) { ultimaRueda = rueda; zoomManual = Mathf.Clamp(zoomManual * Mathf.Exp(-Mathf.Sign(rueda) * 0.18f), 0.25f, 5f); }   // cada toque de rueda = paso grande (~20%)
        }
        // Zoom alternativo por teclado: Z/PageUp acerca, X/PageDown aleja (mantener apretado)
        var tecCam = Keyboard.current;
        if (tecCam != null && !Controles.escribiendo)
        {
            float z = 0f;
            if (tecCam.zKey.isPressed || tecCam.pageUpKey.isPressed || tecCam.numpadPlusKey.isPressed) z -= 1f;
            if (tecCam.xKey.isPressed || tecCam.pageDownKey.isPressed || tecCam.numpadMinusKey.isPressed) z += 1f;
            if (z != 0f) zoomManual = Mathf.Clamp(zoomManual * Mathf.Exp(z * 1.2f * Time.deltaTime), 0.25f, 5f);
        }
        var gpCam = Gamepad.current;
        Vector2 sd = (gpCam != null && !Controles.escribiendo && !Guerra.MiraActiva) ? gpCam.rightStick.ReadValue() : Vector2.zero;
        if (sd.magnitude > 0.2f)
        {
            orbitaYaw += sd.x * ajuste.camVel * 2f * Time.deltaTime;
            orbitaPitch = Mathf.Clamp(orbitaPitch + sd.y * ajuste.camVel * 1.33f * Time.deltaTime, -80f, 80f);
        }
        // Girar la vista con el teclado (mantener): Q gira a la izquierda, E a la derecha (igual que el stick derecho)
        {
            float oy = (Controles.Activa("girar_vista_derecha") ? 1f : 0f) - (Controles.Activa("girar_vista_izquierda") ? 1f : 0f);
            if (oy != 0f) orbitaYaw += oy * ajuste.camVel * 2f * Time.deltaTime;
        }
        // La vista queda donde la dejes (NO vuelve sola al centro). Se centra a mano: clic del stick derecho (R3) o tecla C.
        if (Controles.Flanco("centrar_camara_orbital"))
        {
            orbitaYaw = 0f; orbitaPitch = 0f;
        }
        Vector3 offsetCam = (-adel * 22f + arriba * 5f) * zoomManual;
        offsetCam = Quaternion.AngleAxis(orbitaYaw, arriba) * offsetCam;
        Vector3 lateral = Vector3.Cross(arriba, offsetCam);
        if (lateral.sqrMagnitude > 0.0001f) offsetCam = Quaternion.AngleAxis(orbitaPitch, lateral.normalized) * offsetCam;
        cam.transform.position = transform.position + offsetCam;
        Vector3 mirar = transform.position - cam.transform.position;
        Quaternion destino = Quaternion.LookRotation(mirar, arriba);
        cam.transform.rotation = Quaternion.Slerp(cam.transform.rotation, destino, 1f - Mathf.Exp(-12f * Time.deltaTime));
    }

    void OnGUI()
    {
        DibujarEditor();
    }
}
