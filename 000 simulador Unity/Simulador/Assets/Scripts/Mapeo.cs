// Herramientas de mapeo (marcar lugar con nombre, trackeo de ruta con el autito) + pestanas "Controles" y "Lugares" del panel de instrumentos.
// Todo va DENTRO del panel de abajo (pestanas), sin ventanas flotantes. Portado de Godot (principal.gd): _abrir_panel_marcar_lugar /
// _guardar_lugar_marcado / marcar_punto_ruta (ver RESPUESTA_JOYSTICK_Y_MAPEO.md).
using UnityEngine;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

public class Mapeo : MonoBehaviour
{
    // ---------------- cartel central (como cartel_central de Godot: texto blanco, desaparece solo)
    static string cartelTxt = ""; static float cartelHasta;
    public static void Cartel(string txt, float seg) { cartelTxt = txt; cartelHasta = Time.unscaledTime + seg; }

    // ---------------- archivos (contenido del mundo: carpeta del proyecto Unity, igual que res:// en Godot)
    static string Carpeta { get { return System.IO.Path.GetFullPath(System.IO.Path.Combine(Application.dataPath, "..", "..")); } }
    static string RutaLugares { get { return System.IO.Path.Combine(Carpeta, "lugares_marcados.json"); } }
    static string RutaRuta { get { return System.IO.Path.Combine(Carpeta, "ruta_trackeada.json"); } }
    static string LugaresGodot { get { return System.IO.Path.GetFullPath(System.IO.Path.Combine(Carpeta, "..", "simulador Claude", "lugares_marcados.json")); } }

    static Mapeo inst;
    public static bool abrirLugares;          // lo lee el panel para saltar a la pestana "Lugares" cuando apretas Marcar lugar
    public static bool PanelLugarAbierto { get { return inst != null && inst.panelLugar; } }

    // ---------------- marcar lugar
    bool panelLugar; float abiertoDesde; string nombreLugar = "";
    double latP, lonP; float altP, rumboP;
    int cantLugares = -1;

    // ---------------- ruta trackeada
    readonly List<double> rutaLat = new List<double>(), rutaLon = new List<double>();

    GUIStyle sVerde, sGris, sCartel, sCartelSombra;
    Vector2 scrollControles, scrollLugares;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Crear() { new GameObject("Mapeo").AddComponent<Mapeo>(); }

    void Start()
    {
        inst = this;
        // primera vez: se parte de los lugares ya marcados en Godot (sin perder ninguno)
        try
        {
            if (!System.IO.File.Exists(RutaLugares) && System.IO.File.Exists(LugaresGodot)) System.IO.File.Copy(LugaresGodot, RutaLugares);
        }
        catch (System.Exception e) { Debug.LogWarning("[Mapeo] no pude copiar los lugares de Godot: " + e.Message); }
        ContarLugares();
        CargarRuta();
    }

    void ContarLugares()
    {
        try { cantLugares = System.IO.File.Exists(RutaLugares) ? System.Text.RegularExpressions.Regex.Matches(System.IO.File.ReadAllText(RutaLugares), "\"nombre\"").Count : 0; }
        catch { cantLugares = -1; }
    }

    // ------------------------------------------------------------ ruta trackeada (array de {lat, lon})
    void CargarRuta()
    {
        try
        {
            if (!System.IO.File.Exists(RutaRuta)) return;
            string t = System.IO.File.ReadAllText(RutaRuta);
            var m = System.Text.RegularExpressions.Regex.Matches(t, "\"lat\"\\s*:\\s*(-?[0-9.eE+-]+)\\s*,\\s*\"lon\"\\s*:\\s*(-?[0-9.eE+-]+)");
            foreach (System.Text.RegularExpressions.Match x in m)
            {
                rutaLat.Add(double.Parse(x.Groups[1].Value, CultureInfo.InvariantCulture));
                rutaLon.Add(double.Parse(x.Groups[2].Value, CultureInfo.InvariantCulture));
            }
        }
        catch (System.Exception e) { Debug.LogWarning("[Mapeo] no pude leer ruta_trackeada.json: " + e.Message); }
    }

    void GuardarRuta()
    {
        try
        {
            var sb = new StringBuilder("[");
            for (int i = 0; i < rutaLat.Count; i++)
            {
                sb.Append(i == 0 ? "\n" : ",\n");
                sb.Append("\t{\n\t\t\"lat\": ").Append(rutaLat[i].ToString("R", CultureInfo.InvariantCulture))
                  .Append(",\n\t\t\"lon\": ").Append(rutaLon[i].ToString("R", CultureInfo.InvariantCulture)).Append("\n\t}");
            }
            sb.Append(rutaLat.Count > 0 ? "\n]" : "]");
            System.IO.File.WriteAllText(RutaRuta, sb.ToString());
        }
        catch (System.Exception e) { Debug.LogWarning("[Mapeo] no pude guardar la ruta: " + e.Message); }
    }

    // ------------------------------------------------------------ lugares marcados (se agrega al final, sin tocar lo que ya estaba)
    static string Esc(string s) { return s.Replace("\\", "\\\\").Replace("\"", "\\\""); }

    void GuardarLugar()
    {
        string nombre = nombreLugar.Trim();
        if (nombre == "") nombre = "Sin nombre (" + System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + ")";
        try
        {
            string entrada = "\t{\n\t\t\"altitud\": " + ((double)altP).ToString("R", CultureInfo.InvariantCulture) +
                ",\n\t\t\"lat\": " + latP.ToString("R", CultureInfo.InvariantCulture) +
                ",\n\t\t\"lon\": " + lonP.ToString("R", CultureInfo.InvariantCulture) +
                ",\n\t\t\"nombre\": \"" + Esc(nombre) + "\"" +
                ",\n\t\t\"orientacion\": " + ((double)rumboP).ToString("R", CultureInfo.InvariantCulture) + "\n\t}";
            string t = System.IO.File.Exists(RutaLugares) ? System.IO.File.ReadAllText(RutaLugares).TrimEnd() : "[]";
            int fin = t.LastIndexOf(']');
            if (fin < 0) { t = "[]"; fin = 1; }
            string previo = t.Substring(0, fin).TrimEnd();
            string nuevo = (previo.EndsWith("[") ? previo + "\n" : previo + ",\n") + entrada + "\n]";
            // guardado seguro: primero a un archivo temporal (si el disco esta lleno falla ACA y el original no se toca), despues se reemplaza y queda copia .bak
            string tmp = RutaLugares + ".tmp";
            System.IO.File.WriteAllText(tmp, nuevo);
            if (new System.IO.FileInfo(tmp).Length < nuevo.Length) { System.IO.File.Delete(tmp); throw new System.Exception("disco lleno"); }
            if (System.IO.File.Exists(RutaLugares)) System.IO.File.Replace(tmp, RutaLugares, RutaLugares + ".bak"); else System.IO.File.Move(tmp, RutaLugares);
            Cartel("Lugar guardado:\n" + nombre, 3f);
            ContarLugares();
        }
        catch (System.Exception e) { Cartel("No pude guardar el lugar: " + e.Message, 3f); }
        CerrarPanelLugar();
    }

    void AbrirPanelLugar()
    {
        double lon, lat; float alt, rumbo;
        if (!MapaAuto.PosActiva(out lon, out lat, out alt, out rumbo)) { Cartel("Todavia no hay avion para marcar un lugar", 2f); return; }
        latP = lat; lonP = lon; altP = alt; rumboP = rumbo;
        nombreLugar = ""; panelLugar = true; abiertoDesde = Time.unscaledTime; abrirLugares = true; Controles.escribiendo = true;
    }
    void CerrarPanelLugar() { panelLugar = false; Controles.escribiendo = false; GUIUtility.keyboardControl = 0; }
    public static void CancelarLugar() { if (inst != null && inst.panelLugar) inst.CerrarPanelLugar(); }

    // ------------------------------------------------------------ ciclo
    void Update()
    {
        if (Controles.Flanco("marcar_lugar") && !panelLugar && Controles.esperando == "" && Controles.esperandoTecla == "") AbrirPanelLugar();

        // trackeo de ruta: solo manejando el autito (en Godot es el "modo a pie")
        if (AvionDeCarton.enAuto)
        {
            if (Controles.Flanco("marcar_punto_ruta"))
            {
                double lon, lat; float alt, rumbo;
                if (MapaAuto.PosActiva(out lon, out lat, out alt, out rumbo))
                {
                    rutaLat.Add(lat); rutaLon.Add(lon); GuardarRuta();
                    Cartel("Punto de ruta #" + rutaLat.Count + " guardado", 1f);
                }
            }
            if (Controles.Flanco("deshacer_punto_ruta") && rutaLat.Count > 0)
            {
                rutaLat.RemoveAt(rutaLat.Count - 1); rutaLon.RemoveAt(rutaLon.Count - 1); GuardarRuta();
                Cartel("Ultimo punto borrado (quedan " + rutaLat.Count + ")", 1f);
            }
        }
    }

    // ------------------------------------------------------------ interfaz
    void Estilos(GUIStyle txt)
    {
        if (sVerde != null) return;
        sVerde = new GUIStyle(txt); sVerde.normal.textColor = new Color(0.6f, 1f, 0.6f);
        sGris = new GUIStyle(txt); sGris.normal.textColor = new Color(0.7f, 0.7f, 0.7f);
        sCartel = new GUIStyle(GUI.skin.label) { fontSize = 26, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter, wordWrap = true };
        sCartel.normal.textColor = Color.white;
        sCartelSombra = new GUIStyle(sCartel); sCartelSombra.normal.textColor = Color.black;
    }

    // solo el cartel central (breve, desaparece solo, como en Godot); lo demas va en las pestanas
    void OnGUI()
    {
        if (sCartel == null) return;
        if (Time.unscaledTime < cartelHasta)
        {
            float W = Screen.width, H = Screen.height;
            var rc = new Rect(W * 0.5f - 350f, H * 0.38f, 700f, 120f);
            GUI.Label(new Rect(rc.x + 2f, rc.y + 2f, rc.width, rc.height), cartelTxt, sCartelSombra);
            GUI.Label(rc, cartelTxt, sCartel);
        }
    }

    // ===== pestana "Controles" (se dibuja dentro del area del panel de abajo) =====
    public static void PestanaControles(GUIStyle txt, GUIStyle btn, GUIStyle titulo) { if (inst != null) inst.DibujarControles(txt, btn, titulo); }

    void FilaControl(Controles.Accion a, GUIStyle txt, GUIStyle btn)
    {
        GUILayout.BeginHorizontal(GUILayout.Width(530));
        GUILayout.Label(a.texto + (a.migrada ? "" : " *"), a.migrada ? txt : sGris, GUILayout.Width(205));
        GUILayout.Label("Joy: " + Controles.TextoJoystick(a), a.jTipo != 0 ? sVerde : sGris, GUILayout.Width(112));
        if (GUILayout.Button("Asignar", btn, GUILayout.Width(52), GUILayout.Height(20))) Controles.IniciarJoystick(a.id);
        if (a.conTecla)
        {
            GUILayout.Label("Tecla: " + Controles.TextoTecla(a), a.teclas.Length > 0 ? sVerde : sGris, GUILayout.Width(118));
            if (GUILayout.Button("Asignar", btn, GUILayout.Width(52), GUILayout.Height(20))) Controles.IniciarTecla(a.id);
        }
        else GUILayout.Label("(solo eje del joystick)", sGris, GUILayout.Width(170));
        GUILayout.EndHorizontal();
    }

    void DibujarControles(GUIStyle txt, GUIStyle btn, GUIStyle titulo)
    {
        Estilos(txt);
        GUILayout.BeginHorizontal();
        GUILayout.Label("CONTROLES   (verde = ya asignada; apreta Asignar y despues el boton del joystick o la tecla; Esc cancela)", titulo);
        GUILayout.FlexibleSpace();
        if (GUILayout.Button("Valores de fabrica", btn, GUILayout.Width(130), GUILayout.Height(20))) { Controles.RestaurarFabrica(); Cartel("Controles restaurados", 1.5f); }
        GUILayout.EndHorizontal();
        scrollControles = GUILayout.BeginScrollView(scrollControles, false, true);
        int n = Controles.ACCIONES.Count;
        int cols = (Screen.width - 470f) >= 1130f ? 2 : 1;   // en pantalla angosta: una columna, nada queda fuera (hay scroll vertical)
        for (int i = 0; i < n; i += cols)
        {
            GUILayout.BeginHorizontal();
            FilaControl(Controles.ACCIONES[i], txt, btn);
            if (cols == 2 && i + 1 < n) FilaControl(Controles.ACCIONES[i + 1], txt, btn);
            GUILayout.EndHorizontal();
        }
        GUILayout.Label("* aun no migrada de Godot (se puede asignar, todavia no hace nada)", sGris);
        GUILayout.EndScrollView();
    }

    // ===== pestana "Lugares" =====
    public static void PestanaLugares(GUIStyle txt, GUIStyle btn, GUIStyle titulo) { if (inst != null) inst.DibujarLugares(txt, btn, titulo); }

    void DibujarLugares(GUIStyle txt, GUIStyle btn, GUIStyle titulo)
    {
        Estilos(txt);
        scrollLugares = GUILayout.BeginScrollView(scrollLugares, false, true);
        DibujarLugaresInterno(txt, btn, titulo);
        GUILayout.EndScrollView();
    }

    void DibujarLugaresInterno(GUIStyle txt, GUIStyle btn, GUIStyle titulo)
    {
        GUILayout.Label("DESTINOS  (marcar un lugar con nombre y trackeo de ruta con el autito)", titulo);
        if (!panelLugar)
        {
            string tM = "M o Start"; var am = Controles.ACCIONES.Find(x => x.id == "marcar_lugar");
            if (am != null) tM = Controles.TextoTecla(am) + " / " + Controles.TextoJoystick(am);
            GUILayout.Label("Para marcar donde estas (avion o autito): " + tM + "   (se puede cambiar en la pestana Controles).", txt);
            GUILayout.Label("Lugares guardados: " + (cantLugares >= 0 ? cantLugares.ToString() : "?") + "   (archivo lugares_marcados.json)     Puntos de ruta del autito: " + rutaLat.Count, txt);
            if (GUILayout.Button("Marcar lugar ahora", btn, GUILayout.Width(170), GUILayout.Height(24))) AbrirPanelLugar();
            return;
        }
        GUILayout.Label("lat: " + latP.ToString("F6", CultureInfo.InvariantCulture) + "   lon: " + lonP.ToString("F6", CultureInfo.InvariantCulture) + "   alt: " + (int)altP + " m   rumbo: " + (int)rumboP +
            "      (si el nombre lleva la palabra \"aeropuerto\", queda anotado como aeropuerto)", txt);
        var e = Event.current;
        if (e.type == EventType.KeyDown)
        {
            if (e.keyCode == KeyCode.Return || e.keyCode == KeyCode.KeypadEnter) { GuardarLugar(); e.Use(); return; }
            if (e.keyCode == KeyCode.Escape) { CerrarPanelLugar(); e.Use(); return; }
        }
        GUILayout.BeginHorizontal();
        GUILayout.Label("Nombre:", txt, GUILayout.Width(60));
        GUI.SetNextControlName("nombreLugar");
        nombreLugar = GUILayout.TextField(nombreLugar, 90, GUILayout.Width(520), GUILayout.Height(24));
        if (GUILayout.Button("Guardar", btn, GUILayout.Width(80), GUILayout.Height(24))) { GuardarLugar(); GUILayout.EndHorizontal(); return; }
        if (GUILayout.Button("Cancelar", btn, GUILayout.Width(80), GUILayout.Height(24))) { CerrarPanelLugar(); GUILayout.EndHorizontal(); return; }
        GUILayout.EndHorizontal();
        if (Time.unscaledTime - abiertoDesde > 0.25f && GUI.GetNameOfFocusedControl() != "nombreLugar") GUI.FocusControl("nombreLugar");
        GUILayout.Label("Enter = guardar     Esc = cancelar", sGris);
    }
}
