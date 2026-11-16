// Tren de aterrizaje: se baja/sube con el boton de la pestana "Tren" o con la accion "tren" de Controles (asignable al joystick; fuera del modo guerra
// comparte la cruceta con el disparo). Solo se puede bajar/subir a menos de 150 km/h. Sin animacion: las partes del tren (si el modelo las trae
// separadas) aparecen o desaparecen. Sonido sintetico (zumbido hidraulico + golpe), generado por codigo.
using UnityEngine;

public class TrenAterrizaje : MonoBehaviour
{
    const float VEL_MAX_KMH = 250f;   // limite para mover el tren (antes 150: en aproximacion con aviones grandes no se llegaba a tiempo)
    static TrenAterrizaje inst;
    bool abajo = true; Transform modelo; float espera; Renderer[] partes = new Renderer[0];
    string aviso = ""; float tAviso; AudioSource fuente; AudioClip clip; GUIStyle sAviso;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Crear() { new GameObject("TrenAterrizaje").AddComponent<TrenAterrizaje>(); }

    void Awake() { inst = this; }
    // para la torre: estado y comando del tren
    public static bool EsAbajo { get { return inst == null || inst.abajo; } }
    public static bool EsFijo { get { return Fijo(); } }
    public static bool Poner(bool bajar) { if (inst == null) return false; if (inst.abajo != bajar) inst.Alternar(); return inst.abajo == bajar; }   // true si quedo como se pidio
    // para reiniciar la cabina al empezar un vuelo: cambia el estado sin sonido ni limite de velocidad
    public static void PonerSinAviso(bool bajar) { if (inst != null && inst.modelo != null && !Fijo() && inst.abajo != bajar) { inst.abajo = bajar; inst.Aplicar(); } }
    public static void Zumbido() { if (inst != null) inst.Sonar(); }

    static bool EsParteDelTren(string n)
    {
        n = n.ToLowerInvariant();
        foreach (var k in new[] { "gear", "wheel", "tire", "tyre", "strut", "bogie", "rueda", "tren_", "landing_gear", "undercarriage" })
            if (n.Contains(k) && !n.Contains("light")) return true;
        return false;
    }

    void Update()
    {
        var m = AvionDeCarton.ModeloT;
        if (m != modelo) { modelo = m; espera = 1.2f; partes = new Renderer[0]; abajo = true; }
        if (modelo != null && espera > 0f)
        {
            espera -= Time.deltaTime;
            if (espera <= 0f)
            {
                var lista = new System.Collections.Generic.List<Renderer>();
                foreach (var r in modelo.GetComponentsInChildren<Renderer>(true))
                {
                    if (r is ParticleSystemRenderer || r.gameObject.name.StartsWith("Luz")) continue;
                    if (EsParteDelTren(r.gameObject.name) || (r.transform.parent != null && r.transform.parent != modelo && EsParteDelTren(r.transform.parent.name))) lista.Add(r);
                }
                partes = lista.ToArray(); Aplicar();
            }
        }
        if (Controles.Flanco("tren") && !Guerra.MiraActiva) Alternar();
        if (tAviso > 0f) tAviso -= Time.unscaledDeltaTime;
    }

    // tren fijo (no se recoge): avionetas Cessna/Piper y helicopteros
    static bool Fijo() { string n = (AvionDeCarton.nombreAvion ?? "").ToLowerInvariant(); return AvionDeCarton.EsHeli || n.Contains("cessna") || n.Contains("piper"); }

    void Aplicar() { foreach (var r in partes) if (r != null) r.enabled = abajo; }

    void Alternar()
    {
        if (modelo == null) return;
        if (Fijo()) { Avisar("Este avion tiene el tren fijo: no se recoge", true); return; }
        float kmh = AvionDeCarton.VelMs * 3.6f;
        if (kmh > VEL_MAX_KMH) { Avisar("Debe llegar a menos de " + VEL_MAX_KMH.ToString("F0") + " km/h para mover el tren", true); return; }
        abajo = !abajo; Aplicar(); Sonar();
        Avisar(abajo ? "Tren de aterrizaje: ABAJO" : "Tren de aterrizaje: arriba");
    }

    bool avisoRojo;
    void Avisar(string t, bool rojo = false) { aviso = t; tAviso = 2.5f; avisoRojo = rojo; }

    void Sonar()
    {
        if (clip == null) clip = Generar();
        if (fuente == null) { fuente = gameObject.AddComponent<AudioSource>(); fuente.spatialBlend = 0f; fuente.volume = 0.8f; }
        fuente.PlayOneShot(clip);
    }

    // zumbido hidraulico que sube y baja (1,6 s) + golpe seco al trabarse
    static AudioClip Generar()
    {
        int sr = 44100; float dur = 2.0f; int n = (int)(sr * dur); var d = new float[n]; var rnd = new System.Random(7);
        float fase = 0f, fase2 = 0f;
        for (int i = 0; i < n; i++)
        {
            float t = i / (float)sr;
            if (t < 1.6f)
            {
                float env = Mathf.Clamp01(t / 0.15f) * Mathf.Clamp01((1.6f - t) / 0.25f);
                float f = 150f + 70f * Mathf.Sin(Mathf.Clamp01(t / 1.6f) * Mathf.PI);
                fase += 2f * Mathf.PI * f / sr; fase2 += 2f * Mathf.PI * f * 2.01f / sr;
                float ruido = ((float)rnd.NextDouble() * 2f - 1f) * 0.12f;
                d[i] += (Mathf.Sin(fase) * 0.35f + Mathf.Sin(fase2) * 0.15f + ruido) * env * 0.6f;
            }
            if (t >= 1.62f)
            {
                float tt = t - 1.62f, e = Mathf.Exp(-tt * 28f);
                d[i] += (Mathf.Sin(2f * Mathf.PI * 68f * tt) * 0.9f + ((float)rnd.NextDouble() * 2f - 1f) * 0.5f * Mathf.Exp(-tt * 70f)) * e;
            }
        }
        var c = AudioClip.Create("TrenAterrizaje", n, 1, sr, false); c.SetData(d, 0); return c;
    }

    void OnGUI()
    {
        if (tAviso <= 0f) return;
        if (sAviso == null) { sAviso = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontSize = 15 }; sAviso.normal.textColor = new Color(1f, 1f, 1f, 0.9f); }
        GUI.depth = -50;
        float a = Mathf.Clamp01(tAviso / 0.6f);
        GUI.color = avisoRojo ? new Color(1f, 0.1f, 0.1f, a) : new Color(1f, 1f, 1f, a);
        GUI.Label(new Rect(0, Screen.height * 0.18f, Screen.width, 28f), aviso, sAviso);
        GUI.color = Color.white;
    }

    // ---------------- pestana "Tren" ----------------
    public static void PestanaTren(GUIStyle txt, GUIStyle btn, GUIStyle titulo) { if (inst != null) inst.Dibujar(txt, btn, titulo); }

    void Dibujar(GUIStyle txt, GUIStyle btn, GUIStyle titulo)
    {
        GUILayout.Label("TREN DE ATERRIZAJE   Solo se mueve a menos de " + VEL_MAX_KMH.ToString("F0") + " km/h. Tambien se puede asignar a un boton del joystick en la pestana Controles (accion \"Tren de aterrizaje\").", titulo);
        GUILayout.Label("Velocidad actual: " + (AvionDeCarton.VelMs * 3.6f).ToString("F0") + " km/h      Tren: " + (abajo ? "ABAJO" : "arriba")
            + (partes.Length > 0 ? "      (este modelo tiene " + partes.Length + " partes de tren que aparecen/desaparecen)" : "      (este modelo no trae el tren separado: solo cambia el aviso y el sonido)"), txt);
        if (Fijo()) { GUILayout.Label("Este avion tiene el tren FIJO (como en la realidad): el boton solo esta disponible en los aviones con tren retractil.", txt); return; }
        if (GUILayout.Button(abajo ? "SUBIR tren" : "BAJAR tren", btn, GUILayout.Width(200), GUILayout.Height(34))) Alternar();
    }
}
