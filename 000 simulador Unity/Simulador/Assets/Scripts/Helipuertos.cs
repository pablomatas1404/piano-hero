// HELIPUERTOS con aro de luces neon que corren en circulo. Siempre encendidos (de dia y de noche) y se agrandan con la distancia
// para verse desde lejos aunque no se pueda aterrizar. Hoy: Hotel Hermitage (Mar del Plata), marcado por Pablo.
using UnityEngine;
using CesiumForUnity;
using Unity.Mathematics;
using System.Collections.Generic;

public class Helipuertos : MonoBehaviour
{
    class H { public string nombre; public double lat, lon, alt; public float radio; public Transform raiz; public List<Luz> luces = new List<Luz>(); }
    class Luz { public Transform t; public Renderer r; public Color col; public float fase; public int tipo; }   // tipo 0 aro exterior, 1 aro interior, 2 H central
    readonly List<H> lista = new List<H>();
    Material mat; MaterialPropertyBlock mpb; Mesh malla; Camera cam; AvionDeCarton avion; Transform padre; bool armado;

    static readonly object[][] DEF = {
        new object[]{ "Helipuerto Hotel Hermitage", -38.00728257141979, -57.539408030315876, 26.834619522094727, 40f },
    };

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Crear() { if (FindFirstObjectByType<CesiumGeoreference>() == null) return; new GameObject("Helipuertos").AddComponent<Helipuertos>(); }

    void Armar()
    {
        var geo = FindFirstObjectByType<CesiumGeoreference>(); if (geo == null) return; padre = geo.transform;
        var sh = Shader.Find("Universal Render Pipeline/Unlit"); if (sh == null) sh = Shader.Find("Unlit/Color");
        mat = new Material(sh); mpb = new MaterialPropertyBlock();
        var tmp = GameObject.CreatePrimitive(PrimitiveType.Sphere); malla = tmp.GetComponent<MeshFilter>().sharedMesh; Destroy(tmp);
        foreach (var d in DEF)
        {
            var h = new H { nombre = (string)d[0], lat = (double)d[1], lon = (double)d[2], alt = (double)d[3], radio = (float)d[4] };
            var go = new GameObject("Helipuerto_" + h.nombre); go.transform.SetParent(padre, false);
            var an = go.AddComponent<CesiumGlobeAnchor>(); an.longitudeLatitudeHeight = new double3(h.lon, h.lat, h.alt + 0.8);
            an.rotationEastUpNorth = quaternion.identity;
            h.raiz = go.transform;
            var cian = new Color(0.1f, 1f, 1f); var rosa = new Color(1f, 0.15f, 0.85f); var verde = new Color(0.3f, 1f, 0.4f);
            int n1 = 56; for (int i = 0; i < n1; i++) { float a = i * Mathf.PI * 2f / n1; Nueva(h, new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)) * h.radio, cian, i / (float)n1, 0); }
            int n2 = 32; for (int i = 0; i < n2; i++) { float a = i * Mathf.PI * 2f / n2; Nueva(h, new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)) * h.radio * 0.55f, rosa, i / (float)n2, 1); }
            // una "H" en el centro: dos barras y el travesaño (cada 2,5 m)
            for (float z = -6f; z <= 6.01f; z += 2.5f) { Nueva(h, new Vector3(-3.5f, 0f, z), verde, 0f, 2); Nueva(h, new Vector3(3.5f, 0f, z), verde, 0f, 2); }
            for (float x = -1.0f; x <= 1.01f; x += 1.0f) Nueva(h, new Vector3(x, 0f, 0f), verde, 0f, 2);
            Nueva(h, new Vector3(0f, 0f, 8f), verde, 0f, 3);   // la lucecita sola de la punta de la H (solo se ve de cerca)
            lista.Add(h);
        }
        armado = true;
    }

    void Nueva(H h, Vector3 pos, Color c, float fase, int tipo)
    {
        var go = new GameObject("luz"); go.layer = DiaNoche.CAPA_MARCADORES; go.transform.SetParent(h.raiz, false); go.transform.localPosition = pos;
        go.AddComponent<MeshFilter>().sharedMesh = malla;
        var r = go.AddComponent<MeshRenderer>(); r.sharedMaterial = mat; r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; r.receiveShadows = false;
        h.luces.Add(new Luz { t = go.transform, r = r, col = c, fase = fase, tipo = tipo });
    }

    void Update()
    {
        if (!armado) { if (Time.unscaledTime > 3f && Time.frameCount % 30 == 0) Armar(); return; }
        if (cam == null) cam = Camera.main; if (cam == null) return; if (avion == null) avion = FindFirstObjectByType<AvionDeCarton>();
        foreach (var h in lista)
        {
            float dist = Vector3.Distance(cam.transform.position, h.raiz.position);
            float k = Mathf.Clamp(dist / 450f, 1f, 35f);   // de lejos los puntos crecen para que se vea el aro como un faro
            float t = Time.time;
            float dAv = avion != null ? Vector3.Distance(avion.transform.position, h.raiz.position) : dist;
            bool cerca = dAv < 100f || dist < 100f;   // a menos de 100 m desaparecen los aros y queda una sola luz verde titilando en la punta de la H
            foreach (var l in h.luces)
            {
                bool ver = l.tipo == 3 ? cerca : !cerca; if (l.r.enabled != ver) l.r.enabled = ver; if (!ver) continue;
                float br = 1f;
                if (l.tipo == 0) { float f = Mathf.Repeat(t * 0.45f - l.fase, 1f); br = Mathf.Lerp(1f, 0.12f, Mathf.Clamp01(f * 2.2f)); }   // cabeza brillante con cola que corre en circulo
                else if (l.tipo == 1) { float f = Mathf.Repeat(t * 0.7f + l.fase, 1f); br = Mathf.Lerp(1f, 0.15f, Mathf.Clamp01(f * 2.2f)); }   // el aro interior corre al reves
                else if (l.tipo == 3) br = Mathf.Sin(t * Mathf.PI * 2f * 1.5f) > 0f ? 1f : 0.05f;
                else br = 0.75f + 0.25f * Mathf.Sin(t * 3f);
                Color c = l.col * br; c.a = 1f;
                mpb.SetColor("_BaseColor", c); mpb.SetColor("_Color", c); l.r.SetPropertyBlock(mpb);
                float esc = (l.tipo == 2 ? 0.9f : (l.tipo == 3 ? 1.6f : 1.5f)) * (l.tipo == 3 ? 1f : k); l.t.localScale = new Vector3(esc, esc, esc);
            }
        }
    }
}
