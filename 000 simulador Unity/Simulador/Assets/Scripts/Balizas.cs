// Balizas temporales: un poste magenta altisimo y un cartel con distancia en lugares que Pablo todavia tiene que encontrar/mapear.
// Hoy: Don Torcuato (punto aproximado del aerodromo; no es la pista exacta). Cuando quede trackeado se saca de la lista.
using UnityEngine;
using CesiumForUnity;
using Unity.Mathematics;
using System.Collections.Generic;

public class Balizas : MonoBehaviour
{
    class B { public string texto; public double lat, lon, h; public GameObject go; }
    readonly List<B> lista = new List<B>();
    static readonly string[] BUSCAR = { };   // Don Torcuato ya esta trackeado (14/11)
    bool armadas; float prox; Transform padre; Material mat; GUIStyle sTxt, sSombra; Camera cam; AvionDeCarton avion;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Crear() { if (FindFirstObjectByType<CesiumGeoreference>() == null) return; new GameObject("Balizas").AddComponent<Balizas>(); }

    void Update()
    {
        if (armadas || Time.unscaledTime < prox) return;
        prox = Time.unscaledTime + 1f;
        var geo = FindFirstObjectByType<CesiumGeoreference>(); if (geo == null) return; padre = geo.transform;
        var sh = Shader.Find("Universal Render Pipeline/Unlit"); if (sh == null) sh = Shader.Find("Unlit/Color");
        foreach (var n in BUSCAR)
        {
            string nombre; double la, lo, h;
            if (!Aeropuertos.BuscarFrag(n, out nombre, out la, out lo)) return;   // todavia no cargaron los aeropuertos
            Aeropuertos.Buscar(nombre, out la, out lo, out h);
            lista.Add(new B { texto = nombre.ToUpper() + ": la pista deberia estar aca (punto aproximado)", lat = la, lon = lo, h = h });
        }
        mat = new Material(sh); var c = new Color(1f, 0.1f, 0.9f); mat.SetColor("_BaseColor", c); mat.SetColor("_Color", c);
        foreach (var b in lista)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cylinder); var col = go.GetComponent<Collider>(); if (col != null) Destroy(col);
            go.name = "Baliza_" + b.texto; go.transform.SetParent(padre, false); go.transform.localScale = new Vector3(12f, 150f, 12f);
            var anc = go.AddComponent<CesiumGlobeAnchor>(); anc.longitudeLatitudeHeight = new double3(b.lon, b.lat, b.h + 150.0);
            var r = go.GetComponent<Renderer>(); r.sharedMaterial = mat; r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            b.go = go;
        }
        armadas = true;
    }

    void OnGUI()
    {
        if (!armadas) return;
        if (cam == null) cam = Camera.main; if (avion == null) avion = FindFirstObjectByType<AvionDeCarton>();
        if (cam == null || avion == null) return;
        if (sTxt == null)
        {
            sTxt = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontSize = 15, fontStyle = FontStyle.Bold, wordWrap = true };
            sTxt.normal.textColor = new Color(1f, 0.4f, 0.95f); sSombra = new GUIStyle(sTxt); sSombra.normal.textColor = Color.black;
        }
        foreach (var b in lista)
        {
            if (b.go == null) continue;
            Vector3 p = b.go.transform.position + b.go.transform.up * 160f;
            float km = Vector3.Distance(avion.transform.position, b.go.transform.position) / 1000f;
            string txt = b.texto + "\n" + km.ToString("F1") + " km";
            Vector3 sp = cam.WorldToScreenPoint(p);
            if (sp.z > 0f)
            {
                var r = new Rect(sp.x - 150f, Screen.height - sp.y - 30f, 300f, 60f);
                GUI.Label(new Rect(r.x + 1f, r.y + 1f, r.width, r.height), txt, sSombra); GUI.Label(r, txt, sTxt);
            }
            else
            {
                bool izq = Vector3.Dot(cam.transform.right, p - cam.transform.position) < 0f;
                var r = new Rect(izq ? 10f : Screen.width - 330f, Screen.height * 0.3f, 320f, 60f);
                string t2 = izq ? "<<  " + txt : txt + "  >>";
                GUI.Label(new Rect(r.x + 1f, r.y + 1f, r.width, r.height), t2, sSombra); GUI.Label(r, t2, sTxt);
            }
        }
    }
}
