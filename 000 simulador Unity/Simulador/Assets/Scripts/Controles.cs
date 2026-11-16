// Controles reasignables (joystick y teclado) + menu "Controles".
// Las acciones, sus textos y los valores de fabrica son los de ACCIONES_JOYSTICK de Godot y el joystick_config.cfg de Pablo
// (ver RESPUESTA_JOYSTICK_Y_MAPEO.md). Los indices de boton/eje son los de Godot (Xbox: 0 A, 1 B, 2 X, 3 Y, 4 Back, 6 Start,
// 7/8 clic de stick, 9 LB, 10 RB, 11-14 cruceta; ejes 0 LX, 1 LY, 2 RX, 3 RY, 4 LT, 5 RT).
// Mejora sobre Godot: las TECLAS tambien se pueden reasignar. Se guarda en la carpeta de datos del jugador (persistentDataPath).
using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;

[DefaultExecutionOrder(-1000)]
public class Controles : MonoBehaviour
{
    public class Accion
    {
        public string id, texto, nota = "";
        public Key[] teclas, teclasDef;
        public int jTipo, jIndice, jSigno;           // 0 ninguno, 1 boton, 2 eje
        public int jTipoDef, jIndiceDef, jSignoDef;
        public bool migrada = true;
        public bool conTecla = true;                 // false: solo eje/boton de joystick (la mira)
    }

    public static readonly List<Accion> ACCIONES = new List<Accion>();
    static readonly Dictionary<string, Accion> porId = new Dictionary<string, Accion>();
    static readonly Dictionary<string, bool> ahora = new Dictionary<string, bool>();
    static readonly Dictionary<string, bool> antes = new Dictionary<string, bool>();
    static readonly Dictionary<string, bool> flanco = new Dictionary<string, bool>();
    const float ZONA_MUERTA = 0.3f;   // DEADZONE_EJE_JOYSTICK de Godot

    public static bool escribiendo;            // true mientras hay un cuadro de texto abierto: se bloquean los controles de vuelo
    public static string esperando = "";       // accion que espera un boton/eje de joystick
    public static string esperandoTecla = "";  // accion que espera una tecla
    public static bool menuAbierto;

    static string Ruta { get { return System.IO.Path.Combine(Application.persistentDataPath, "controles.json"); } }

    static void A(string id, string texto, Key[] teclas, int jTipo, int jIndice, int jSigno, bool migrada = true, bool conTecla = true, string nota = "")
    {
        var a = new Accion { id = id, texto = texto, teclas = teclas, teclasDef = (Key[])teclas.Clone(), jTipo = jTipo, jIndice = jIndice, jSigno = jSigno,
            jTipoDef = jTipo, jIndiceDef = jIndice, jSignoDef = jSigno, migrada = migrada, conTecla = conTecla, nota = nota };
        ACCIONES.Add(a); porId[id] = a; ahora[id] = false; antes[id] = false; flanco[id] = false;
    }
    static readonly Key[] NADA = new Key[0];

    static void Definir()
    {
        if (ACCIONES.Count > 0) return;
        A("acelerar", "Acelerar", new[] { Key.W }, 2, 4, 1);
        A("frenar", "Frenar / desacelerar", new[] { Key.S }, 1, 9, 0);
        A("cabeceo_arriba", "Cabecear arriba (subir nariz)", new[] { Key.UpArrow }, 1, 12, 0);
        A("cabeceo_abajo", "Cabecear abajo (bajar nariz)", new[] { Key.DownArrow }, 1, 11, 0);
        A("alabeo_izquierda", "Girar/inclinar izquierda", new[] { Key.LeftArrow }, 1, 13, 0);
        A("alabeo_derecha", "Girar/inclinar derecha", new[] { Key.RightArrow }, 1, 14, 0);
        A("vertical_arriba", "Subir vertical (helicoptero)", new[] { Key.R, Key.O }, 2, 5, 1);
        A("vertical_abajo", "Bajar vertical (helicoptero)", new[] { Key.F, Key.L }, 1, 10, 0);
        A("marcar_lugar", "Marcar lugar (guardar coordenada actual)", new[] { Key.M }, 1, 6, 0);
        A("freno_emergencia", "Freno de emergencia (baja a 150 km/h; avionetas 100)", new[] { Key.D }, 1, 4, 0);
        A("freno_gradual", "Freno progresivo", new[] { Key.B }, 1, 0, 0);
        A("tren", "Tren de aterrizaje (bajar/subir; fuera del modo guerra)", new[] { Key.G }, 0, 0, 0, true, true, "Se puede asignar a la misma cruceta que el disparo: en modo guerra dispara, afuera mueve el tren");
        A("armamento", "Disparo / armamento (modo guerra)", new[] { Key.Space }, 1, 1, 0, true, true, "Espacio = extra de Unity para probar sin joystick");
        A("mira_horizontal", "Mira horizontal (modo guerra)", NADA, 2, 2, -1, true, false);
        A("mira_vertical", "Mira vertical (modo guerra)", NADA, 2, 3, -1, true, false);
        A("activar_ils", "Activar/desactivar ILS", new[] { Key.I }, 1, 3, 0);
        A("reservado_1", "Trackear checkpoint (City Tour)", new[] { Key.T }, 1, 2, 0, false);
        A("marcar_punto_ruta", "Marcar punto de ruta (con el autito)", new[] { Key.R }, 1, 1, 0);
        A("deshacer_punto_ruta", "Borrar ultimo punto de ruta", NADA, 1, 2, 0);
        A("reversa_pushback", "Reversa/pushback en tierra (prender/apagar)", NADA, 1, 2, 0);
        A("centrar_camara_orbital", "Centrar camara orbital (clic del stick)", new[] { Key.C }, 1, 8, 0);
        A("girar_vista_izquierda", "Girar la vista a la izquierda", new[] { Key.Q }, 0, 0, 0, true, true, "extra de Unity");
        A("ver_sin_avion", "Ocultar/mostrar el avion (ojito, para fotos)", new[] { Key.V }, 0, 0, 0, true, true, "como la tecla V de Godot");
        A("girar_vista_derecha", "Girar la vista a la derecha", new[] { Key.E }, 0, 0, 0, true, true, "extra de Unity");
    }

    // ------------------------------------------------------------ guardado
    [System.Serializable] class FilaG { public string id; public string[] teclas; public int jTipo, jIndice, jSigno; }
    [System.Serializable] class ListaG { public List<FilaG> items = new List<FilaG>(); }

    static void Cargar()
    {
        try
        {
            if (!System.IO.File.Exists(Ruta)) return;
            var l = JsonUtility.FromJson<ListaG>(System.IO.File.ReadAllText(Ruta));
            if (l == null) return;
            foreach (var f in l.items)
            {
                Accion a; if (!porId.TryGetValue(f.id, out a)) continue;
                a.jTipo = f.jTipo; a.jIndice = f.jIndice; a.jSigno = f.jSigno;
                var ks = new List<Key>();
                if (f.teclas != null) foreach (var s in f.teclas) { Key k; if (System.Enum.TryParse(s, out k)) ks.Add(k); }
                a.teclas = ks.ToArray();
            }
        }
        catch (System.Exception e) { Debug.LogWarning("[Controles] no pude leer controles.json: " + e.Message); }
    }

    static void Guardar()
    {
        try
        {
            var l = new ListaG();
            foreach (var a in ACCIONES)
            {
                var f = new FilaG { id = a.id, jTipo = a.jTipo, jIndice = a.jIndice, jSigno = a.jSigno };
                var ts = new List<string>(); foreach (var k in a.teclas) ts.Add(k.ToString());
                f.teclas = ts.ToArray(); l.items.Add(f);
            }
            System.IO.File.WriteAllText(Ruta, JsonUtility.ToJson(l, true));
        }
        catch (System.Exception e) { Debug.LogWarning("[Controles] no pude guardar: " + e.Message); }
    }

    public static void RestaurarFabrica()
    {
        foreach (var a in ACCIONES) { a.teclas = (Key[])a.teclasDef.Clone(); a.jTipo = a.jTipoDef; a.jIndice = a.jIndiceDef; a.jSigno = a.jSignoDef; }
        Guardar();
    }

    // ------------------------------------------------------------ lectura de joystick (indices de Godot)
    static UnityEngine.InputSystem.Controls.ButtonControl Boton(Gamepad gp, int i)
    {
        switch (i)
        {
            case 0: return gp.buttonSouth; case 1: return gp.buttonEast; case 2: return gp.buttonWest; case 3: return gp.buttonNorth;
            case 4: return gp.selectButton; case 6: return gp.startButton; case 7: return gp.leftStickButton; case 8: return gp.rightStickButton;
            case 9: return gp.leftShoulder; case 10: return gp.rightShoulder;
            case 11: return gp.dpad.up; case 12: return gp.dpad.down; case 13: return gp.dpad.left; case 14: return gp.dpad.right;
        }
        return null;
    }
    // valor del eje al estilo Godot (el eje Y de los sticks es positivo hacia abajo)
    static float Eje(Gamepad gp, int i)
    {
        switch (i)
        {
            case 0: return gp.leftStick.x.ReadValue(); case 1: return -gp.leftStick.y.ReadValue();
            case 2: return gp.rightStick.x.ReadValue(); case 3: return -gp.rightStick.y.ReadValue();
            case 4: return gp.leftTrigger.ReadValue(); case 5: return gp.rightTrigger.ReadValue();
        }
        return 0f;
    }

    static bool JoyActivo(Gamepad gp, Accion a)
    {
        if (gp == null || a.jTipo == 0) return false;
        if (a.jTipo == 1) { var b = Boton(gp, a.jIndice); return b != null && b.isPressed; }
        return Eje(gp, a.jIndice) * a.jSigno > ZONA_MUERTA;
    }

    // ------------------------------------------------------------ API para el resto del juego
    public static bool Activa(string id) { bool v; return !escribiendo && ahora.TryGetValue(id, out v) && v; }
    public static bool Flanco(string id) { bool v; return !escribiendo && flanco.TryGetValue(id, out v) && v; }

    public static string TextoJoystick(Accion a)
    {
        if (esperando == a.id) return "Presiona algo...";
        if (a.jTipo == 0) return "Sin asignar";
        if (a.jTipo == 1) return "Boton " + a.jIndice;
        return "Eje " + a.jIndice + " (" + (a.jSigno > 0 ? "+" : "-") + ")";
    }
    public static string TextoTecla(Accion a)
    {
        if (esperandoTecla == a.id) return "Presiona una tecla...";
        if (a.teclas.Length == 0) return "Sin asignar";
        var s = new List<string>(); foreach (var k in a.teclas) s.Add(NombreTecla(k));
        return string.Join(" / ", s);
    }
    static string NombreTecla(Key k)
    {
        switch (k)
        {
            case Key.UpArrow: return "Flecha arriba"; case Key.DownArrow: return "Flecha abajo";
            case Key.LeftArrow: return "Flecha izq."; case Key.RightArrow: return "Flecha der.";
            case Key.Space: return "Espacio"; case Key.Enter: return "Enter";
        }
        return k.ToString().Replace("Digit", "");
    }

    static float tEspera;
    public static void CancelarEspera() { esperando = ""; esperandoTecla = ""; }
    public static void IniciarJoystick(string id) { esperando = id; esperandoTecla = ""; tEspera = 10f; }
    public static void IniciarTecla(string id) { esperandoTecla = id; esperando = ""; tEspera = 10f; }

    // ------------------------------------------------------------ ciclo
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Crear()
    {
        Definir(); Cargar();
        new GameObject("Controles").AddComponent<Controles>();
    }

    void Update()
    {
        var gp = Gamepad.current; var kb = Keyboard.current;

        // si nadie toca nada en 10 s la asignacion se cancela sola (antes quedaba colgada y bloqueaba todos los controles de vuelo)
        if (esperando != "" || esperandoTecla != "") { tEspera -= Time.unscaledDeltaTime; if (tEspera <= 0f) { CancelarEspera(); Mapeo.Cartel("Asignacion cancelada", 1.5f); } }
        // asignacion en curso: el primer boton/eje (o tecla) que se mueva se guarda
        if (esperando != "" && gp != null)
        {
            Accion a = porId[esperando];
            bool hecho = false;
            for (int i = 0; i <= 14 && !hecho; i++)
            {
                var b = Boton(gp, i);
                if (b != null && b.wasPressedThisFrame)
                {
                    if (a.id == "mira_horizontal" || a.id == "mira_vertical") { Mapeo.Cartel("La mira necesita un eje del joystick", 1.5f); continue; }
                    a.jTipo = 1; a.jIndice = i; a.jSigno = 0; hecho = true;
                }
            }
            for (int i = 0; i <= 5 && !hecho; i++)
            {
                float v = Eje(gp, i);
                if (Mathf.Abs(v) > ZONA_MUERTA) { a.jTipo = 2; a.jIndice = i; a.jSigno = v > 0f ? 1 : -1; hecho = true; }
            }
            if (hecho) { Mapeo.Cartel("Asignado: " + a.texto, 1.5f); esperando = ""; Guardar(); }
        }
        if (esperandoTecla != "" && kb != null)
        {
            Accion a = porId[esperandoTecla];
            if (kb.escapeKey.wasPressedThisFrame) esperandoTecla = "";
            else
            {
                foreach (var kc in kb.allKeys)
                {
                    if (kc == null || !kc.wasPressedThisFrame) continue;
                    if (kc.keyCode == Key.None) continue;
                    a.teclas = new[] { kc.keyCode }; esperandoTecla = ""; Guardar(); break;
                }
            }
        }

        bool bloquear = escribiendo || esperando != "" || esperandoTecla != "";
        foreach (var a in ACCIONES)
        {
            bool v = false;
            if (!bloquear)
            {
                if (kb != null) foreach (var k in a.teclas) { var kc = kb[k]; if (kc != null && kc.isPressed) { v = true; break; } }
                if (!v) v = JoyActivo(gp, a);
            }
            antes[a.id] = ahora[a.id];
            ahora[a.id] = v;
            flanco[a.id] = v && !antes[a.id];
        }
    }
}
