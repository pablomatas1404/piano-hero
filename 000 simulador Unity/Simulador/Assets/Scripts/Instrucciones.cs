using System.Collections.Generic;
using UnityEngine;

// Boton INSTRUCCIONES (con cruz para sacarlo): reproduce el audio hablado "instrucciones" (Assets/Resources/voz/instrucciones.mp3 o .wav).
// La cruz lo oculta para siempre (se guarda); se puede mover con el editor de pantalla (F8). Para volver a verlo: tecla F9.
public class Instrucciones : MonoBehaviour
{
    // ---- flechas guiadas: cada tramo del audio (separado por una pausa larga de ~3 s) senala algo de la pantalla ----
    // el resto del juego avisa donde esta cada cosa con Instrucciones.Marcar("id", rect) en su OnGUI
    class Marca { public Rect r; public int cuadro; }
    static readonly Dictionary<string, Marca> marcas = new Dictionary<string, Marca>();
    public static void Marcar(string id, Rect r)
    {
        if (Event.current == null || Event.current.type != EventType.Repaint) return;
        var m = GUI.matrix; Vector3 a = m.MultiplyPoint3x4(new Vector3(r.x, r.y, 0f)), b = m.MultiplyPoint3x4(new Vector3(r.xMax, r.yMax, 0f));
        Marca k; if (!marcas.TryGetValue(id, out k)) { k = new Marca(); marcas[id] = k; }
        k.r = new Rect(a.x, a.y, b.x - a.x, b.y - a.y); k.cuadro = Time.frameCount;
    }
    // tramos del audio, en orden; cada uno lista las cosas que se senalan (texto = lo que dice el cartel de la flecha)
    static readonly string[][] TRAMOS = {
        new string[]{ },                                   // 0 bienvenida
        new string[]{ "instr|Tus instrumentos" },          // 1 arranque
        new string[]{ "cambiar|Cambiar avion", "tab_aviones|Pestana Aviones" },   // 2 cambiar de avion
        new string[]{ "plan|Plan de vuelo" },              // 3 mapa y plan de vuelo
        new string[]{ "tab_p2|Torre de control" },         // 4 despegar y aterrizar
        new string[]{ "tab_shopping|Shopping" },           // 5 la plata
        new string[]{ },                                   // 6 autito y radio
        new string[]{ },                                   // 7 cierre
    };
    float[] cortes;   // segundos donde empieza cada tramo nuevo (se detectan las pausas largas del audio)

    bool eligiendo, ingles; AudioClip clipEs, clipEn;   // al apretar INSTRUCCIONES se elige idioma (voz/instrucciones.mp3 = espanol, voz/instrucciones_en.mp3 = ingles)
    static readonly Dictionary<string, string> ROTULO_EN = new Dictionary<string, string> { { "instr", "Your instruments" }, { "cambiar", "Change aircraft" }, { "tab_aviones", "Aircraft tab" }, { "plan", "Flight plan" }, { "tab_p2", "Control tower" }, { "tab_shopping", "Shopping" } };

    AudioSource fuente; AudioClip clip; bool buscado, oculto; string aviso = ""; float avisoHasta;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Crear() { new GameObject("Instrucciones").AddComponent<Instrucciones>(); }

    void Awake() { oculto = PlayerPrefs.GetInt("instr_oculto2", 0) == 1; fuente = gameObject.AddComponent<AudioSource>(); fuente.playOnAwake = false; fuente.spatialBlend = 0f; }

    // ---- estilo: amarillo transparente con bordes redondeados ----
    static Texture2D Redondo(Color col, float relleno, float borde)
    {
        const int N = 48; const float R = 16f;
        var t = new Texture2D(N, N, TextureFormat.RGBA32, false); t.wrapMode = TextureWrapMode.Clamp; t.filterMode = FilterMode.Bilinear;
        for (int y = 0; y < N; y++) for (int x = 0; x < N; x++)
        {
            float px = Mathf.Abs(x + 0.5f - N * 0.5f) - (N * 0.5f - R), py = Mathf.Abs(y + 0.5f - N * 0.5f) - (N * 0.5f - R);
            float d = Mathf.Sqrt(Mathf.Max(px, 0f) * Mathf.Max(px, 0f) + Mathf.Max(py, 0f) * Mathf.Max(py, 0f)) + Mathf.Min(Mathf.Max(px, py), 0f) - R;   // <0 adentro
            float a = d > 0.5f ? 0f : (d > -2.2f ? borde : relleno); if (d > -0.5f && d <= 0.5f) a *= 0.5f + (0.5f - d) * 0.5f;
            t.SetPixel(x, y, new Color(col.r, col.g, col.b, a));
        }
        t.Apply(); return t;
    }
    GUIStyle estAm, estRoj, estQ; Texture2D texOjo, texOjoTachado;
    // ojito dibujado a mano (no depende de ninguna fuente): elipse con pupila; tachado cuando el avion esta oculto
    static Texture2D HacerOjo(bool tachado)
    {
        const int N = 32; var t = new Texture2D(N, N, TextureFormat.RGBA32, false); t.filterMode = FilterMode.Bilinear;
        for (int y = 0; y < N; y++) for (int x = 0; x < N; x++)
        {
            float dx = (x + 0.5f - N * 0.5f) / (N * 0.5f), dy = (y + 0.5f - N * 0.5f) / (N * 0.5f); float a = 0f;
            float e = dx * dx / 0.95f + dy * dy / 0.32f;   // elipse horizontal
            if (e < 1f && e > 0.62f) a = 1f;                // contorno
            if (dx * dx + dy * dy < 0.12f) a = 1f;          // pupila
            if (tachado && Mathf.Abs(dx - dy) < 0.10f && Mathf.Abs(dx) < 0.95f) a = 1f;   // tacha diagonal
            t.SetPixel(x, y, new Color(1f, 1f, 1f, a));
        }
        t.Apply(); return t;
    }
    void BotonOjo(Rect r)
    {
        if (texOjo == null) { texOjo = HacerOjo(false); texOjoTachado = HacerOjo(true); }
        if (estQ == null) estQ = new GUIStyle(estAm) { fontSize = 15 };
        if (GUI.Button(r, new GUIContent("", "Ver sin avion"), estQ)) AvionDeCarton.AlternarSinAvion();
        if (Event.current.type == EventType.Repaint) GUI.DrawTexture(new Rect(r.x + 4f, r.y + 4f, r.width - 8f, r.height - 8f), AvionDeCarton.SinAvion ? texOjoTachado : texOjo);
    }
    GUIStyle Estilo(Color col)
    {
        var st = new GUIStyle { fontSize = 17, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter, border = new RectOffset(16, 16, 16, 16) };
        st.normal.background = Redondo(col, 0.28f, 0.95f); st.hover.background = Redondo(col, 0.48f, 1f); st.active.background = Redondo(col, 0.68f, 1f);
        st.normal.textColor = Color.white; st.hover.textColor = Color.white; st.active.textColor = Color.white;
        return st;
    }

    void OnGUI()
    {
        var e = Event.current;
        { int dp = GUI.depth; GUI.depth = -3000; DibujarFlechas(); GUI.depth = dp; }
        if (e.type == EventType.KeyDown && e.keyCode == KeyCode.F9) { oculto = false; PlayerPrefs.SetInt("instr_oculto2", 0); PlayerPrefs.Save(); e.Use(); return; }
        if (estAm == null) { estAm = Estilo(new Color(1f, 0.85f, 0.15f)); estRoj = Estilo(new Color(1f, 0.3f, 0.25f)); }
        {   // ojito: a la izquierda del ? (mismo tamano), oculta/muestra el avion para ver la pantalla mas limpia
            var ro = new Rect(DiaNoche.BarraX(Screen.width) + 395f + 272f + 47f - 30f, 34f, 26f, 26f);
            var to = Disposicion.Bloque("ojo", "OJITO (ver sin avion)", ro); BotonOjo(ro); to.Dispose();
        }
        if (oculto)
        {   // escondido con la X: queda solo un "?" chiquito para volver a abrirlo (sin teclas)
            var rq = new Rect(DiaNoche.BarraX(Screen.width) + 395f + 272f + 47f, 34f, 26f, 26f);   // justo debajo del boton Azafata, chiquito
            var tq = Disposicion.Bloque("instrucciones_q", "BOTON ? (instrucciones)", rq);
            if (estQ == null) { estQ = new GUIStyle(estAm) { fontSize = 15 }; }
            if (GUI.Button(rq, "?", estQ)) { oculto = false; PlayerPrefs.SetInt("instr_oculto2", 0); PlayerPrefs.Save(); }
            tq.Dispose(); return;
        }
        float W = Screen.width;
        var orig = new Rect(W * 0.5f - 160f, 62f, 320f, 34f);
        var t = Disposicion.Bloque("instrucciones", "BOTON INSTRUCCIONES", orig);
        bool suena = fuente != null && fuente.isPlaying;
        var st = estAm;
        if (GUI.Button(new Rect(orig.x, orig.y, 282f, 34f), suena || cargandoAudio ? (cargandoAudio ? "Cargando audio..." : "DETENER") : "INSTRUCCIONES (escuchar)", st)) { if (suena) { fuente.Stop(); } else if (!cargandoAudio) eligiendo = !eligiendo; }
        if (eligiendo && !suena && !cargandoAudio)
        {
            var rE = new Rect(orig.x, orig.y + 38f, 154f, 34f); var rI = new Rect(orig.x + 164f, orig.y + 38f, 154f, 34f);
            if (GUI.Button(rE, "ESPAÑOL", st)) { eligiendo = false; Reproducir(false); }
            if (GUI.Button(rI, "ENGLISH", st)) { eligiendo = false; Reproducir(true); }
        }
        if (GUI.Button(new Rect(orig.x + 286f, orig.y, 34f, 34f), "X", estRoj)) { if (suena) fuente.Stop(); oculto = true; PlayerPrefs.SetInt("instr_oculto2", 1); PlayerPrefs.Save(); }
        if (aviso != "" && Time.unscaledTime < avisoHasta)
        {
            var s2 = new GUIStyle(GUI.skin.label) { fontSize = 12, wordWrap = true, alignment = TextAnchor.UpperRight }; s2.normal.textColor = Color.white;
            GUI.Label(new Rect(orig.x - 60f, orig.y + (eligiendo ? 76f : 36f), 260f, 40f), aviso, s2);
        }
        t.Dispose();
    }

    // busca las pausas largas (>= 2,3 s de silencio): ahi cambia el tramo. Si no coincide la cantidad, reparte el tiempo en partes iguales.
    static float[] DetectarCortes(AudioClip c)
    {
        var lista = new List<float>();
        try
        {
            int ch = c.channels, fr = c.frequency, ventana = fr / 20;   // 50 ms
            var d = new float[c.samples * ch]; c.GetData(d, 0);
            int nv = c.samples / ventana, silencio = 0, inicio = 0;
            for (int w = 0; w < nv; w++)
            {
                float mx = 0f; int b0 = w * ventana * ch, b1 = Mathf.Min(d.Length, b0 + ventana * ch);
                for (int i = b0; i < b1; i += 4) { float v = Mathf.Abs(d[i]); if (v > mx) mx = v; }
                if (mx < 0.012f) { if (silencio == 0) inicio = w; silencio++; }
                else { if (silencio * 0.05f >= 2.3f && inicio > 0) lista.Add((inicio + silencio * 0.5f) * 0.05f); silencio = 0; }
            }
        }
        catch { }
        int esperados = TRAMOS.Length - 1;
        if (lista.Count != esperados) { lista.Clear(); for (int i = 1; i <= esperados; i++) lista.Add(c.length * i / (float)(esperados + 1)); }
        return lista.ToArray();
    }

    void DibujarFlechas()
    {
        if (fuente == null || !fuente.isPlaying || cortes == null) return;
        float t = fuente.time; int tramo = 0; for (int i = 0; i < cortes.Length; i++) if (t >= cortes[i]) tramo = i + 1;
        if (tramo >= TRAMOS.Length) return;
        float W = Screen.width, H = Screen.height; float rebote = Mathf.Abs(Mathf.Sin(Time.unscaledTime * 5f)) * 12f;
        var fl = new GUIStyle(GUI.skin.label) { fontSize = 44, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter }; fl.normal.textColor = new Color(1f, 0.9f, 0.1f);
        var so = new GUIStyle(fl); so.normal.textColor = Color.black;
        var tx = new GUIStyle(GUI.skin.label) { fontSize = 14, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter }; tx.normal.textColor = Color.white;
        foreach (var cfg in TRAMOS[tramo])
        {
            var par = cfg.Split('|'); Marca k; if (!marcas.TryGetValue(par[0], out k) || Time.frameCount - k.cuadro > 6) continue;
            var r = k.r; bool abajo = r.y < 90f;   // si esta pegado arriba, la flecha va debajo apuntando hacia arriba
            float cx = Mathf.Clamp(r.center.x, 40f, W - 40f);
            var rf = abajo ? new Rect(cx - 30f, r.yMax + 2f + rebote, 60f, 56f) : new Rect(cx - 30f, r.y - 58f - rebote, 60f, 56f);
            string ch = abajo ? "▲" : "▼";
            GUI.Label(new Rect(rf.x + 2f, rf.y + 2f, rf.width, rf.height), ch, so); GUI.Label(rf, ch, fl);
            var rt = new Rect(cx - 90f, abajo ? rf.yMax - 2f : rf.y - 20f, 180f, 20f);
            var cc = GUI.color; GUI.color = new Color(0f, 0f, 0f, 0.7f); GUI.DrawTexture(rt, Texture2D.whiteTexture); GUI.color = cc;
            string rot = par.Length > 1 ? par[1] : ""; if (ingles) { string en; if (ROTULO_EN.TryGetValue(par[0], out en)) rot = en; }
            GUI.Label(rt, rot, tx);
            var cl = GUI.color; GUI.color = new Color(1f, 0.9f, 0.1f, 0.9f);
            GUI.DrawTexture(new Rect(r.x - 3f, r.y - 3f, r.width + 6f, 3f), Texture2D.whiteTexture); GUI.DrawTexture(new Rect(r.x - 3f, r.yMax, r.width + 6f, 3f), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(r.x - 3f, r.y, 3f, r.height), Texture2D.whiteTexture); GUI.DrawTexture(new Rect(r.xMax, r.y, 3f, r.height), Texture2D.whiteTexture); GUI.color = cl;
        }
    }

    // ---- volumen: la voz se amplifica (x3 con limitador suave) y mientras habla el resto de los sonidos baja a ~15% (se escuchan de fondo) ----
    const float GANANCIA_VOZ = 3f, VOL_FONDO = 0.15f;
    static readonly HashSet<AudioClip> amplificados = new HashSet<AudioClip>();
    static void Amplificar(AudioClip c)
    {
        if (c == null || amplificados.Contains(c)) return;
        try
        {
            var d = new float[c.samples * c.channels]; if (!c.GetData(d, 0)) return;
            for (int i = 0; i < d.Length; i++) d[i] = (float)System.Math.Tanh(d[i] * GANANCIA_VOZ);
            c.SetData(d, 0); amplificados.Add(c);
        }
        catch { }
    }
    class Duck { public float orig, puesto; }
    readonly Dictionary<AudioSource, Duck> duck = new Dictionary<AudioSource, Duck>();
    float factor = 1f, proxBusqueda;
    void LateUpdate()
    {
        bool habla = fuente != null && fuente.isPlaying;
        factor = Mathf.MoveTowards(factor, habla ? VOL_FONDO : 1f, Time.unscaledDeltaTime * 4f);
        if (!habla && factor >= 1f && duck.Count == 0) return;
        if (Time.unscaledTime >= proxBusqueda)
        {
            proxBusqueda = Time.unscaledTime + 0.5f;
            foreach (var a in FindObjectsByType<AudioSource>(FindObjectsSortMode.None)) if (a != fuente && !duck.ContainsKey(a)) duck[a] = new Duck { orig = a.volume, puesto = -1f };
        }
        var quitar = new List<AudioSource>();
        foreach (var kv in duck)
        {
            var a = kv.Key; if (a == null) { quitar.Add(a); continue; }
            var d = kv.Value;
            if (d.puesto < 0f || Mathf.Abs(a.volume - d.puesto) > 0.0001f) d.orig = a.volume;   // otro script cambio el volumen: ese es el nuevo original
            a.volume = d.orig * factor; d.puesto = a.volume;
            if (!habla && factor >= 1f) { a.volume = d.orig; quitar.Add(a); }
        }
        foreach (var a in quitar) duck.Remove(a);
    }

    bool cargandoAudio; float cargaDesde;
    void Update()
    {
        if (cargandoAudio)
        {
            if (clip != null && clip.loadState == AudioDataLoadState.Loaded) { cargandoAudio = false; Empezar(); }
            else if (clip == null || clip.loadState == AudioDataLoadState.Failed || Time.unscaledTime - cargaDesde > 20f) { cargandoAudio = false; aviso = "No pude cargar el audio (" + (clip == null ? "no existe" : clip.loadState.ToString()) + ")."; avisoHasta = Time.unscaledTime + 6f; }
        }
        if (fuente != null) fuente.volume = 1f;
    }
    void Empezar()
    {
        if (FindFirstObjectByType<AudioListener>() == null) { aviso = "No hay AudioListener en la escena."; avisoHasta = Time.unscaledTime + 6f; }
        Amplificar(clip);
        fuente.Stop(); fuente.clip = clip; fuente.volume = 1f; fuente.mute = false; fuente.time = 0f; fuente.Play(); cortes = DetectarCortes(clip);
        if (aviso == "") { aviso = "Reproduciendo (" + (int)clip.length + " s)"; avisoHasta = Time.unscaledTime + 3f; }
    }
    void Reproducir(bool en)
    {
        ingles = en; clip = en ? clipEn : clipEs;
        if (clip == null) { clip = Resources.Load<AudioClip>(en ? "voz/instrucciones_en" : "voz/instrucciones"); if (en) clipEn = clip; else clipEs = clip; }
        if (clip == null) { aviso = (ingles ? "The English audio is not here yet." : "Todavia no esta el audio de las instrucciones."); avisoHasta = Time.unscaledTime + 4f; return; }
        aviso = ""; if (clip.loadState != AudioDataLoadState.Loaded) { clip.LoadAudioData(); cargandoAudio = true; cargaDesde = Time.unscaledTime; return; }
        Empezar();
    }
}
