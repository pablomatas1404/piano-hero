// Radio, musica por genero e instrucciones de seguridad. Portado de principal.gd (ver PROMPT_CODIGO_LITERAL_RADIO_MUSICA...).
// Los mp3 se leen DIRECTO de la carpeta del Godot ("simulador Claude"), con streaming, sin copiarlos.
// Ninguno arranca solo: la radio NO se prende sola en Unity (pedido de Pablo), la musica y las instrucciones tampoco.
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class RadioMusica : MonoBehaviour
{
    const long TAMANO_MAXIMO_TEMA = 100L * 1024 * 1024;   // si pesa mas, algo anda mal con el archivo
    const string EMISORA_POR_DEFECTO = "Córdoba Torre (SACO)";
    static readonly string[] GENEROS = { "Ambiental", "Orquestal", "Rock", "Electronica" };

    static string Godot { get { return System.IO.Path.GetFullPath(System.IO.Path.Combine(Application.dataPath, "..", "..", "..", "simulador Claude")); } }
    static string CarpetaRadio { get { return System.IO.Path.Combine(Godot, "FX", "Radio"); } }
    static string CarpetaInstr { get { return System.IO.Path.Combine(Godot, "FX", "Instrucciones"); } }
    static string CarpetaMusica(string genero) { return System.IO.Path.Combine(Godot, "Música", genero); }

    AudioSource srcRadio, srcMusica, srcInstr;
    List<string> emisoras = new List<string>();
    string emisoraActual = ""; bool radioOn = true; int tokenRadio;   // radioOn = boton Radio: se prende solo y se acuerda de como lo dejaste
    bool cargandoRadio, radioPausadaPorMi; float tTorre = -99f, proxGuardado;
    List<string> playlist = new List<string>();
    int indice; string genero = ""; bool pausada, musicaActiva, cargandoMusica; int tokenMusica;
    string estadoMusica = "Elegi un genero";
    bool panelRadio, panelMusica; Vector2 scrR, scrM;
    int tokenInstr;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Crear()
    {
        if (FindFirstObjectByType<CesiumForUnity.CesiumGeoreference>() == null) return;
        new GameObject("RadioMusica").AddComponent<RadioMusica>();
    }

    void Awake()
    {
        srcRadio = Fuente(0.6f, true); srcMusica = Fuente(0.6f, false); srcInstr = Fuente(1f, false);
        RefrescarEmisoras();
        // emisora preferida guardada, o la de Cordoba por defecto, o la primera. NO se prende sola.
        radioOn = PlayerPrefs.GetInt("radio_on", 1) == 1;
        string guardada = PlayerPrefs.GetString("emisora_radio", "");
        if (guardada != "" && emisoras.Contains(guardada)) emisoraActual = guardada;
        else
        {
            foreach (var r in emisoras) if (System.IO.Path.GetFileNameWithoutExtension(r) == EMISORA_POR_DEFECTO) { emisoraActual = r; break; }
            if (emisoraActual == "" && emisoras.Count > 0) emisoraActual = emisoras[0];
        }
    }

    AudioSource Fuente(float vol, bool loop)
    {
        var a = gameObject.AddComponent<AudioSource>();
        a.playOnAwake = false; a.loop = loop; a.spatialBlend = 0f; a.volume = vol;
        return a;
    }

    static List<string> ListarMp3(string carpeta)
    {
        var l = new List<string>();
        try { if (System.IO.Directory.Exists(carpeta)) foreach (var f in System.IO.Directory.GetFiles(carpeta, "*.mp3")) l.Add(f); } catch { }
        l.Sort(System.StringComparer.OrdinalIgnoreCase);   // orden alfabetico fijo, sin mezclar
        return l;
    }

    void RefrescarEmisoras() { emisoras = ListarMp3(CarpetaRadio); }

    // lee el mp3 del disco con streaming (no lo carga entero a la memoria)
    static async Awaitable<AudioClip> CargarClip(string ruta)
    {
        try
        {
            using (var req = UnityWebRequestMultimedia.GetAudioClip(new System.Uri(ruta).AbsoluteUri, AudioType.MPEG))
            {
                ((DownloadHandlerAudioClip)req.downloadHandler).streamAudio = true;
                var op = req.SendWebRequest();
                while (!op.isDone) await Awaitable.NextFrameAsync();
                if (req.result != UnityWebRequest.Result.Success) { Debug.LogWarning("Audio: " + req.error + " (" + ruta + ")"); return null; }
                return DownloadHandlerAudioClip.GetContent(req);
            }
        }
        catch (System.Exception e) { Debug.LogWarning("Audio: " + e.Message); return null; }
    }

    // ---------------- radio ----------------
    // La radio de fondo (conversaciones) se prende sola y suena cuando la torre NO habla: si habla, se PAUSA (no se reinicia) y sigue donde estaba.
    // Se acuerda de en que segundo quedo cada emisora (tambien entre partidas). El boton Radio ON/OFF la apaga del todo o la deja.
    async void CargarRadio()
    {
        if (emisoraActual == "" || cargandoRadio) return;
        cargandoRadio = true; int mi = ++tokenRadio; string ruta = emisoraActual;
        var c = await CargarClip(ruta);
        cargandoRadio = false;
        if (mi != tokenRadio || c == null || ruta != emisoraActual) return;
        c.name = ruta; srcRadio.clip = c; srcRadio.loop = true; radioPausadaPorMi = false; primeraVez = true;
    }
    bool primeraVez;
    static string ClaveT(string ruta) { return "radio_t_" + System.IO.Path.GetFileNameWithoutExtension(ruta); }
    void GuardarRadio()
    {
        if (srcRadio == null || srcRadio.clip == null || srcRadio.clip.name != emisoraActual || !(srcRadio.isPlaying || radioPausadaPorMi)) return;
        PlayerPrefs.SetFloat(ClaveT(emisoraActual), srcRadio.time); PlayerPrefs.Save();
    }
    void ActualizarRadio()
    {
        if (Torre.Hablando) tTorre = Time.unscaledTime;
        bool quiere = radioOn && emisoraActual != "" && Time.unscaledTime - tTorre > 0.7f;
        if (quiere)
        {
            if (srcRadio.clip == null || srcRadio.clip.name != emisoraActual)
            {
                if (!cargandoRadio) { if (srcRadio.clip != null) { srcRadio.Stop(); srcRadio.clip = null; } CargarRadio(); }
            }
            else if (!srcRadio.isPlaying && Application.isFocused)
            {
                if (radioPausadaPorMi) { srcRadio.UnPause(); radioPausadaPorMi = false; }
                else
                {
                    srcRadio.Play();
                    if (primeraVez) { primeraVez = false; float t = PlayerPrefs.GetFloat(ClaveT(emisoraActual), 0f); if (t > 1f && t < srcRadio.clip.length - 3f) srcRadio.time = t; }
                }
            }
        }
        else if (srcRadio.isPlaying) { srcRadio.Pause(); radioPausadaPorMi = true; }
        if (Time.unscaledTime >= proxGuardado) { proxGuardado = Time.unscaledTime + 4f; GuardarRadio(); }
    }

    void AlternarRadio()
    {
        radioOn = !radioOn; PlayerPrefs.SetInt("radio_on", radioOn ? 1 : 0); PlayerPrefs.Save();
    }

    void SeleccionarEmisora(string ruta)
    {
        GuardarRadio();
        emisoraActual = ruta; srcRadio.Stop(); srcRadio.clip = null; radioPausadaPorMi = false; tokenRadio++; cargandoRadio = false;
        PlayerPrefs.SetString("emisora_radio", ruta); radioOn = true; PlayerPrefs.SetInt("radio_on", 1); PlayerPrefs.Save();
    }

    // ---------------- musica ----------------
    void ElegirGenero(string g)
    {
        genero = g; playlist = ListarMp3(CarpetaMusica(g));
        if (playlist.Count == 0) { estadoMusica = "No encontre temas en esa carpeta"; return; }
        estadoMusica = g + ": elegi un tema de la lista";
    }

    async void Siguiente()
    {
        if (playlist.Count == 0) return;
        int mi = ++tokenMusica; cargandoMusica = true;
        int intentos = 0;
        while (intentos < playlist.Count)
        {
            if (indice >= playlist.Count) indice = 0;
            string ruta = playlist[indice]; indice++; intentos++;
            long tam = 0; try { tam = new System.IO.FileInfo(ruta).Length; } catch { }
            if (tam < 100 || tam > TAMANO_MAXIMO_TEMA) { Debug.LogWarning("Tema descartado (vacio/corrupto/demasiado pesado): " + ruta); continue; }
            srcMusica.Stop();
            var c = await CargarClip(ruta);
            if (mi != tokenMusica) return;
            if (c == null) continue;
            srcMusica.clip = c; pausada = false; srcMusica.Play(); musicaActiva = true; cargandoMusica = false;
            estadoMusica = genero + ": " + System.IO.Path.GetFileNameWithoutExtension(ruta);
            return;
        }
        cargandoMusica = false;
        estadoMusica = genero + ": ningun tema de la lista se pudo reproducir";
    }

    void SeleccionarTema(int i) { indice = i; Siguiente(); }

    void Anterior()
    {
        if (playlist.Count == 0) return;
        indice -= 2; while (indice < 0) indice += playlist.Count;
        Siguiente();
    }

    void AlternarPausa()
    {
        if (!musicaActiva) return;
        pausada = !pausada;
        if (pausada) srcMusica.Pause(); else srcMusica.UnPause();
    }

    void DetenerMusica()
    {
        tokenMusica++; cargandoMusica = false; srcMusica.Stop(); srcMusica.clip = null;
        playlist.Clear(); genero = ""; musicaActiva = false; pausada = false; estadoMusica = "Detenida";
    }

    void Update()
    {
        srcRadio.volume = 0.6f * Mezcla.De(Mezcla.RADIO); srcMusica.volume = 0.6f * Mezcla.De(Mezcla.MUSICA); srcInstr.volume = Mezcla.De(Mezcla.AZAFATA);
        ActualizarMusica();
        ActualizarRadio();
    }
    void OnApplicationQuit() { GuardarRadio(); }
    void OnDisable() { GuardarRadio(); }

    void ActualizarMusica()
    {
        // al terminar un tema pasa solo al siguiente, en el mismo orden
        if (musicaActiva && !pausada && !cargandoMusica && srcMusica.clip != null && !srcMusica.isPlaying && Application.isFocused) Siguiente();
    }

    // ---------------- instrucciones de seguridad (play/stop, sin selector) ----------------
    async void AlternarInstrucciones()
    {
        if (srcInstr.isPlaying) { tokenInstr++; srcInstr.Stop(); return; }
        var l = ListarMp3(CarpetaInstr);
        if (l.Count == 0) return;
        int mi = ++tokenInstr;
        if (srcInstr.clip == null)
        {
            var c = await CargarClip(l[0]);   // el primer mp3 que haya en la carpeta
            if (mi != tokenInstr || c == null) return;
            srcInstr.clip = c;
        }
        srcInstr.Play();
    }

    // ---------------- interfaz (fila de botones arriba, a la derecha del relojito) ----------------
    void OnGUI()
    {
        float W = Screen.width;
        float x = DiaNoche.BarraX(W) + 395f, y = 4f;
        var tapaM = Disposicion.Bloque("radio", "RADIO Y MUSICA", new Rect(x, y, 392f, 26f));
        if (GUI.Button(new Rect(x, y, 110f, 26f), radioOn ? "Radio: ON" : "Radio: OFF")) AlternarRadio();
        if (GUI.Button(new Rect(x + 114f, y, 80f, 26f), "Emisoras")) { panelRadio = !panelRadio; panelMusica = false; if (panelRadio) RefrescarEmisoras(); }
        if (GUI.Button(new Rect(x + 198f, y, 70f, 26f), "Musica")) { panelMusica = !panelMusica; panelRadio = false; }
        if (GUI.Button(new Rect(x + 272f, y, 120f, 26f), srcInstr.isPlaying ? "Azafata: STOP" : "Azafata")) AlternarInstrucciones();

        var pc = GUI.color;
        if (panelRadio)
        {
            var r = new Rect(x, 34f, 392f, 150f); GUI.Box(r, "EMISORAS");
            if (emisoras.Count == 0) GUI.Label(new Rect(r.x + 8f, r.y + 28f, 370f, 22f), "No hay mp3 en FX/Radio");
            scrR = GUI.BeginScrollView(new Rect(r.x + 6f, r.y + 26f, 380f, 118f), scrR, new Rect(0, 0, 360f, emisoras.Count * 24f));
            for (int i = 0; i < emisoras.Count; i++)
            {
                GUI.color = emisoras[i] == emisoraActual ? new Color(0.5f, 1f, 0.5f) : Color.white;
                if (GUI.Button(new Rect(0, i * 24f, 360f, 22f), System.IO.Path.GetFileNameWithoutExtension(emisoras[i]))) SeleccionarEmisora(emisoras[i]);
            }
            GUI.EndScrollView(); GUI.color = pc;
        }
        if (panelMusica)
        {
            var r = new Rect(x, 34f, 392f, 290f); GUI.Box(r, "MUSICA");
            for (int g = 0; g < GENEROS.Length; g++)
            {
                GUI.color = genero == GENEROS[g] ? new Color(0.5f, 1f, 0.5f) : Color.white;
                if (GUI.Button(new Rect(r.x + 8f + g * 94f, r.y + 24f, 90f, 24f), GENEROS[g])) ElegirGenero(GENEROS[g]);
            }
            GUI.color = pc;
            GUI.Label(new Rect(r.x + 8f, r.y + 52f, 376f, 22f), estadoMusica + (cargandoMusica ? "  (cargando...)" : ""));
            scrM = GUI.BeginScrollView(new Rect(r.x + 6f, r.y + 76f, 380f, 170f), scrM, new Rect(0, 0, 360f, playlist.Count * 24f));
            for (int i = 0; i < playlist.Count; i++)
                if (GUI.Button(new Rect(0, i * 24f, 360f, 22f), System.IO.Path.GetFileNameWithoutExtension(playlist[i]))) SeleccionarTema(i);
            GUI.EndScrollView();
            if (GUI.Button(new Rect(r.x + 8f, r.y + 254f, 80f, 26f), "Anterior")) Anterior();
            if (GUI.Button(new Rect(r.x + 92f, r.y + 254f, 110f, 26f), pausada ? "Reanudar" : "Pausa")) AlternarPausa();
            if (GUI.Button(new Rect(r.x + 206f, r.y + 254f, 80f, 26f), "Siguiente")) Siguiente();
            if (GUI.Button(new Rect(r.x + 290f, r.y + 254f, 94f, 26f), "Detener")) DetenerMusica();
        }
        tapaM.Dispose();
    }
}
