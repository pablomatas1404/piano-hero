using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

// Editor de la disposicion de la pantalla (F8 o el boton de Pagina 1): permite mover y agrandar/achicar los bloques de la interfaz.
// Cada bloque se dibuja dentro de Disposicion.Bloque(...) / Dispose(); el desplazamiento y la escala se guardan en disposicion_pantalla.json
// (el desplazamiento se guarda como fraccion de la pantalla, asi sirve aunque cambie la resolucion).
public class Disposicion : MonoBehaviour
{
    class Blq
    {
        public string id, nombre; public Rect orig; public float dxp, dyp, esc = 1f; public int visto = -999;
        public bool Tiene { get { return dxp != 0f || dyp != 0f || esc != 1f; } }
    }
    [Serializable] class Reg { public string id; public float dx, dy, s; }
    [Serializable] class Guardado { public List<Reg> bloques = new List<Reg>(); }

    static readonly Dictionary<string, Blq> bl = new Dictionary<string, Blq>();
    static readonly List<string> orden = new List<string>();
    static bool cargado;
    public static bool Editando { get; private set; }
    static string Ruta { get { return Path.GetFullPath(Path.Combine(Application.dataPath, "..", "disposicion_pantalla.json")); } }

    public struct Tapa : IDisposable
    {
        public Matrix4x4 m; public bool activo;
        public void Dispose() { if (activo) { GUI.matrix = m; activo = false; } }
    }

    static void Cargar()
    {
        cargado = true;
        try
        {
            if (!File.Exists(Ruta)) return;
            var g = JsonUtility.FromJson<Guardado>(File.ReadAllText(Ruta));
            if (g == null) return;
            foreach (var r in g.bloques)
            {
                Blq b; if (!bl.TryGetValue(r.id, out b)) { b = new Blq { id = r.id, nombre = r.id }; bl[r.id] = b; orden.Add(r.id); }
                b.dxp = r.dx; b.dyp = r.dy; b.esc = Mathf.Clamp(r.s <= 0f ? 1f : r.s, 0.4f, 3f);
            }
        }
        catch { }
    }

    static void Guardar()
    {
        try
        {
            var g = new Guardado();
            foreach (var id in orden) { var b = bl[id]; if (b.Tiene) g.bloques.Add(new Reg { id = b.id, dx = b.dxp, dy = b.dyp, s = b.esc }); }
            File.WriteAllText(Ruta, JsonUtility.ToJson(g, true));
        }
        catch { }
    }

    static Blq Obtener(string id, string nombre, Rect orig)
    {
        if (!cargado) Cargar();
        Blq b; if (!bl.TryGetValue(id, out b)) { b = new Blq { id = id }; bl[id] = b; orden.Add(id); }
        b.nombre = nombre; b.orig = orig; b.visto = Time.frameCount;
        return b;
    }

    static Rect Pantalla(Blq b)
    {
        float W = Screen.width, H = Screen.height;
        return new Rect(b.orig.x + b.dxp * W, b.orig.y + b.dyp * H, b.orig.width * b.esc, b.orig.height * b.esc);
    }

    // abre el bloque: todo lo que se dibuje hasta Dispose() queda movido / escalado
    public static Tapa Bloque(string id, string nombre, Rect orig)
    {
        var t = new Tapa { m = GUI.matrix, activo = false };
        var b = Obtener(id, nombre, orig);
        if (!b.Tiene) return t;
        var p = Pantalla(b);
        GUI.matrix = GUI.matrix * Matrix4x4.TRS(new Vector3(p.x, p.y, 0f), Quaternion.identity, new Vector3(b.esc, b.esc, 1f)) * Matrix4x4.TRS(new Vector3(-orig.x, -orig.y, 0f), Quaternion.identity, Vector3.one);
        t.activo = true;
        return t;
    }

    // rectangulo real en pantalla de un bloque (para los que necesitan saber donde quedo)
    public static Rect PantRect(string id, Rect orig)
    {
        if (!cargado) Cargar();
        Blq b; if (!bl.TryGetValue(id, out b) || !b.Tiene) return orig;
        float W = Screen.width, H = Screen.height;
        return new Rect(orig.x + b.dxp * W, orig.y + b.dyp * H, orig.width * b.esc, orig.height * b.esc);
    }

    // true si el puntero (coordenadas de GUI) esta sobre un bloque movido, o si se esta editando
    public static bool SobreAlgo(Vector2 p)
    {
        if (Editando) return true;
        foreach (var id in orden) { var b = bl[id]; if (b.Tiene && Time.frameCount - b.visto < 30 && Pantalla(b).Contains(p)) return true; }
        return false;
    }

    // ---------------- editor ----------------
    static Texture2D blanco;
    static Dictionary<string, Vector3> copia;   // dx, dy, s al entrar (para CANCELAR)
    string arrastrando = ""; int modo; Vector2 off; GUIStyle sEt, sBt;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Crear() { new GameObject("DisposicionEditor").AddComponent<Disposicion>(); }

    public static void Entrar()
    {
        if (Editando) return;
        if (!cargado) Cargar();
        copia = new Dictionary<string, Vector3>();
        foreach (var id in orden) { var b = bl[id]; copia[id] = new Vector3(b.dxp, b.dyp, b.esc); }
        Editando = true;
    }
    static void Salir(bool guardar)
    {
        if (!Editando) return;
        if (guardar) Guardar();
        else if (copia != null) foreach (var kv in copia) { Blq b; if (bl.TryGetValue(kv.Key, out b)) { b.dxp = kv.Value.x; b.dyp = kv.Value.y; b.esc = kv.Value.z; } }
        Editando = false; copia = null;
    }
    static void Restablecer() { foreach (var id in orden) { var b = bl[id]; b.dxp = 0f; b.dyp = 0f; b.esc = 1f; } }

    static void Rc(Rect r, Color c) { var a = GUI.color; GUI.color = c; GUI.DrawTexture(r, blanco); GUI.color = a; }

    void OnGUI()
    {
        if (blanco == null) { blanco = Texture2D.whiteTexture; }
        var e = Event.current;
        if (e.type == EventType.KeyDown && e.keyCode == KeyCode.F8) { if (Editando) Salir(true); else Entrar(); e.Use(); return; }
        if (!Editando) return;
        GUI.depth = -2000;
        if (sEt == null)
        {
            sEt = new GUIStyle(GUI.skin.label) { fontSize = 13, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter }; sEt.normal.textColor = Color.white;
            sBt = new GUIStyle(GUI.skin.button) { fontSize = 13, fontStyle = FontStyle.Bold };
        }
        float W = Screen.width, H = Screen.height;
        var amarillo = new Color(1f, 0.84f, 0.2f, 1f);

        // barra de herramientas (arriba al centro)
        var rb = new Rect(W * 0.5f - 300f, 6f, 600f, 62f);
        Rc(rb, new Color(0.03f, 0.05f, 0.08f, 0.95f)); Rc(new Rect(rb.x, rb.y, rb.width, 2f), amarillo);
        GUI.Label(new Rect(rb.x, rb.y + 2f, rb.width, 22f), "EDITAR PANTALLA: arrastra cada cuadro; el cuadradito amarillo de la esquina cambia el tamano", sEt);
        var bgc = GUI.backgroundColor;
        GUI.backgroundColor = new Color(0.4f, 1f, 0.5f);
        if (GUI.Button(new Rect(rb.x + 10f, rb.y + 28f, 200f, 28f), "GUARDAR Y SALIR", sBt)) { Salir(true); GUI.backgroundColor = bgc; return; }
        GUI.backgroundColor = bgc;
        if (GUI.Button(new Rect(rb.x + 220f, rb.y + 28f, 200f, 28f), "RESTABLECER TODO", sBt)) Restablecer();
        if (GUI.Button(new Rect(rb.x + 430f, rb.y + 28f, 160f, 28f), "CANCELAR", sBt)) { Salir(false); return; }

        // cajas de cada bloque (la mas chica que contiene al puntero tiene prioridad)
        var vivos = new List<Blq>();
        foreach (var id in orden) { var b = bl[id]; if (Time.frameCount - b.visto < 30) vivos.Add(b); }
        if (e.type == EventType.MouseDown && e.button == 0)
        {
            if (rb.Contains(e.mousePosition)) { e.Use(); }
            else
            {
                Blq mejor = null; float area = float.MaxValue; int m = 0;
                foreach (var b in vivos)
                {
                    var p = Pantalla(b);
                    var asa = new Rect(p.xMax - 28f, p.yMax - 28f, 34f, 34f);
                    if (asa.Contains(e.mousePosition)) { mejor = b; m = 2; break; }
                    if (p.Contains(e.mousePosition) && p.width * p.height < area) { mejor = b; area = p.width * p.height; m = 1; }
                }
                if (mejor != null) { arrastrando = mejor.id; modo = m; var p = Pantalla(mejor); off = e.mousePosition - new Vector2(p.x, p.y); e.Use(); }
            }
        }
        if (e.type == EventType.MouseDrag && arrastrando != "")
        {
            Blq b; if (bl.TryGetValue(arrastrando, out b))
            {
                var p = Pantalla(b);
                if (modo == 1)
                {
                    float nx = Mathf.Clamp(e.mousePosition.x - off.x, 0f, W - 40f), ny = Mathf.Clamp(e.mousePosition.y - off.y, 0f, H - 24f);
                    b.dxp = (nx - b.orig.x) / W; b.dyp = (ny - b.orig.y) / H;
                }
                else
                {
                    float nw = Mathf.Max(30f, e.mousePosition.x - p.x);
                    b.esc = Mathf.Clamp(nw / Mathf.Max(1f, b.orig.width), 0.4f, 3f);
                }
            }
            e.Use();
        }
        if (e.type == EventType.MouseUp) { arrastrando = ""; }

        foreach (var b in vivos)
        {
            var p = Pantalla(b);
            bool act = b.id == arrastrando;
            Rc(p, new Color(1f, 0.84f, 0.2f, act ? 0.32f : 0.18f));
            Rc(new Rect(p.x, p.y, p.width, 2f), amarillo); Rc(new Rect(p.x, p.yMax - 2f, p.width, 2f), amarillo);
            Rc(new Rect(p.x, p.y, 2f, p.height), amarillo); Rc(new Rect(p.xMax - 2f, p.y, 2f, p.height), amarillo);
            Rc(new Rect(p.xMax - 28f, p.yMax - 28f, 28f, 28f), amarillo);
            var et = new Rect(p.x + 2f, p.y + 2f, Mathf.Min(p.width - 4f, 260f), 20f);
            Rc(et, new Color(0f, 0f, 0f, 0.6f)); GUI.Label(et, b.nombre, sEt);
        }
        // mientras se edita, nada de lo de abajo recibe clics
        if (e.type == EventType.MouseDown || e.type == EventType.MouseUp || e.type == EventType.MouseDrag) e.Use();
    }
}
