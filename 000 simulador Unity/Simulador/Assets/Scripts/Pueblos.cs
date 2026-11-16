// Cartelitos con el nombre de los pueblos y referencias que Pablo trackeo a mano (datos/pueblos.json en la carpeta del proyecto).
// Al pasar volando (o manejando) a menos de ~3 km de uno, aparece su nombre un rato arriba, en tono marron-amarillento. Pensado para los viajes de noche.
using UnityEngine;
using System.Collections.Generic;

public class Pueblos : MonoBehaviour
{
    [System.Serializable] class P { public string nombre; public double lat, lon; }
    [System.Serializable] class Lista { public List<P> items = new List<P>(); }

    List<P> pueblos = new List<P>();
    readonly HashSet<string> cerca = new HashSet<string>();   // los que ya se mostraron y todavia no te alejaste
    string cartel = ""; float cartelDesde = -99f; float proximoChequeo;
    const float RADIO_M = 3000f, SALIDA_M = 6000f, DURACION = 5f;
    GUIStyle sTxt, sSombra;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Crear() { new GameObject("Pueblos").AddComponent<Pueblos>(); }

    static Pueblos inst;
    // para la torre: el pueblo mas cercano que esta adelante (a +-45 grados del rumbo) dentro de 30 km
    public static bool ProximoAdelante(double lat, double lon, float rumbo, out string nombre, out double km)
    {
        nombre = ""; km = 0; if (inst == null) return false; double mejor = 30000.0;
        foreach (var p in inst.pueblos)
        {
            double d = Dist(lat, lon, p.lat, p.lon); if (d > mejor || d < 1500.0) continue;
            double n = p.lat - lat, e = (p.lon - lon) * System.Math.Cos(lat * System.Math.PI / 180.0); float rb = (float)(System.Math.Atan2(e, n) * 180.0 / System.Math.PI);
            if (Mathf.Abs(Mathf.DeltaAngle(rumbo, rb)) > 45f) continue;
            mejor = d; nombre = p.nombre; km = d / 1000.0;
        }
        return nombre != "";
    }

    // para la torre: nombres de los pueblos a menos de radioM metros (para validar "escribi la localidad mas cercana")
    public static List<string> Cercanos(double lat, double lon, double radioKm)
    {
        var l = new List<string>(); if (inst == null) return l;
        foreach (var p in inst.pueblos) if (Dist(lat, lon, p.lat, p.lon) <= radioKm * 1000.0) l.Add(p.nombre);
        return l;
    }

    void Start()
    {
        inst = this;
        try
        {
            string ruta = System.IO.Path.GetFullPath(System.IO.Path.Combine(Application.dataPath, "..", "..", "datos", "pueblos.json"));
            if (!System.IO.File.Exists(ruta)) { Debug.LogWarning("[Pueblos] no encuentro " + ruta); return; }
            var l = JsonUtility.FromJson<Lista>("{\"items\":" + System.IO.File.ReadAllText(ruta) + "}");
            if (l != null) pueblos = l.items;
            Debug.Log("[Pueblos] cargados " + pueblos.Count);
        }
        catch (System.Exception e) { Debug.LogWarning("[Pueblos] error al leer pueblos.json: " + e.Message); }
    }

    static double Dist(double la1, double lo1, double la2, double lo2)
    {
        double dLa = (la2 - la1) * 111320.0, dLo = (lo2 - lo1) * 111320.0 * System.Math.Cos(la1 * System.Math.PI / 180.0);
        return System.Math.Sqrt(dLa * dLa + dLo * dLo);
    }

    void Update()
    {
        if (pueblos.Count == 0 || Time.unscaledTime < proximoChequeo) return;
        proximoChequeo = Time.unscaledTime + 0.5f;
        double lon, lat; float alt, rumbo;
        if (!MapaAuto.PosActiva(out lon, out lat, out alt, out rumbo)) return;
        P mejor = null; double dMejor = RADIO_M;
        foreach (var p in pueblos)
        {
            double d = Dist(lat, lon, p.lat, p.lon);
            if (d > SALIDA_M) { cerca.Remove(p.nombre); continue; }
            if (d < dMejor && !cerca.Contains(p.nombre)) { mejor = p; dMejor = d; }
        }
        if (mejor != null && Time.unscaledTime - cartelDesde > 2.5f) { cerca.Add(mejor.nombre); cartel = mejor.nombre; cartelDesde = Time.unscaledTime; }
    }

    void OnGUI()
    {
        float t = Time.unscaledTime - cartelDesde;
        if (t > DURACION || string.IsNullOrEmpty(cartel)) return;
        if (sTxt == null)
        {
            sTxt = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontSize = 20, fontStyle = FontStyle.Bold };
            sTxt.normal.textColor = new Color(0.86f, 0.72f, 0.42f);
            sSombra = new GUIStyle(sTxt); sSombra.normal.textColor = new Color(0f, 0f, 0f, 0.8f);
        }
        float a = Mathf.Clamp01(Mathf.Min(t / 0.6f, (DURACION - t) / 1.2f));   // aparece y se va suave
        var r = new Rect(Screen.width * 0.5f - 250f, Screen.height * 0.2f, 500f, 30f);
        var c0 = GUI.color; GUI.color = new Color(1f, 1f, 1f, a);
        GUI.Label(new Rect(r.x + 2f, r.y + 2f, r.width, r.height), cartel, sSombra);
        GUI.Label(r, cartel, sTxt);
        GUI.color = c0;
    }
}
