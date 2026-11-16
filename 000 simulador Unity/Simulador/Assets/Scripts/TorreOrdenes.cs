// TORRE INTERACTIVA: la torre habla (voces de ElevenLabs en Resources/voz, con efecto de radio), da ordenes con el dato en pantalla
// (altitud, rumbo, velocidad, frecuencia de radio, luces...), pide datos por escrito y valida lo que hace el piloto. Boton OK para confirmar.
// Radio COM con digitos y +/-: cada zona del mapa tiene su frecuencia; al cambiar de zona la torre pide sintonizarla.
using UnityEngine;
using System.Collections.Generic;
using System.Globalization;

public partial class Torre
{
    // ------------------------------------------------------------------ voz con efecto de radio
    class VozItem { public string nombre; public float desde; }
    readonly List<VozItem> vozCola = new List<VozItem>();
    readonly Dictionary<string, AudioClip> vozCache = new Dictionary<string, AudioClip>();
    AudioSource vozSrc; bool ordenesOn = true; string ultimaVoz = "";
    const float VOZ_PITCH = 0.94f;   // un poquito mas lenta y grave: a veces hablan rapido

    public static void Hablar(string nombre, float retraso = 0f) { if (inst != null) inst.Decir(nombre, retraso); }

    // dice el primer sonido que exista (si todavia no esta grabado el preferido, usa el siguiente)
    void DecirPrimero(params string[] nombres)
    {
        foreach (var n in nombres) if (ClipVoz(n) != null) { Decir(n); return; }
    }

    void Decir(string nombre, float retraso = 0f)
    {
        if (!ordenesOn || ClipVoz(nombre) == null) return;
        ultimaVoz = nombre; vozCola.Add(new VozItem { nombre = nombre, desde = Time.unscaledTime + retraso });
    }

    // true mientras la torre habla (o tiene una frase por decir): la radio de fondo se pausa y sigue despues
    public static bool Hablando { get { return inst != null && inst.vozSrc != null && (inst.vozSrc.isPlaying || inst.vozCola.Count > 0); } }

    void VozTick()
    {
        if (vozSrc == null) { vozSrc = gameObject.AddComponent<AudioSource>(); vozSrc.spatialBlend = 0f; vozSrc.playOnAwake = false; vozSrc.pitch = VOZ_PITCH; }
        vozSrc.volume = Mathf.Clamp01(0.95f * Mezcla.De(Mezcla.TORRE));   // slider propio "Voz de torre" (pestana Sonidos)
        if (vozSrc.isPlaying || vozCola.Count == 0) return;
        for (int i = 0; i < vozCola.Count; i++)
            if (Time.unscaledTime >= vozCola[i].desde) { var c = ClipVoz(vozCola[i].nombre); vozCola.RemoveAt(i); if (c != null) { vozSrc.clip = c; vozSrc.Play(); } return; }
    }

    // carga el wav y le pone voz de radio: pasa-banda (~380 a 3200 Hz), saturacion suave, un poco de ruido y un "tick" al abrir y cerrar el microfono
    AudioClip ClipVoz(string n)
    {
        AudioClip c; if (vozCache.TryGetValue(n, out c)) return c;
        float[] d; int canales, len, sr;
        if (n.Contains("+"))   // transmision compuesta: varios clips seguidos con una pausita (indicativo + mensaje)
        {
            var partes = n.Split('+'); var lista = new List<float[]>(); sr = 0; int tot = 0;
            for (int pi = 0; pi < partes.Length; pi++)
            {
                var s2 = Resources.Load<AudioClip>("voz/" + partes[pi]); if (s2 == null) { if (pi == partes.Length - 1) { vozCache[n] = null; return null; } continue; }
                var dd = new float[s2.samples * s2.channels]; s2.GetData(dd, 0); var mono = new float[s2.samples];
                for (int i = 0; i < s2.samples; i++) { float x = 0f; for (int k = 0; k < s2.channels; k++) x += dd[i * s2.channels + k]; mono[i] = x / s2.channels; }
                if (sr == 0) sr = s2.frequency; lista.Add(mono); tot += mono.Length;
            }
            int pausa = (int)(0.22f * sr); d = new float[tot + pausa * Mathf.Max(0, lista.Count - 1)]; int pos = 0;
            for (int i = 0; i < lista.Count; i++) { System.Array.Copy(lista[i], 0, d, pos, lista[i].Length); pos += lista[i].Length + (i < lista.Count - 1 ? pausa : 0); }
            canales = 1; len = d.Length;
        }
        else
        {
            var src = Resources.Load<AudioClip>("voz/" + n); if (src == null) { vozCache[n] = null; return null; }
            canales = src.channels; len = src.samples; sr = src.frequency; d = new float[len * canales]; src.GetData(d, 0);
        }
        int pre = (int)(0.10f * sr), post = (int)(0.16f * sr); var o = new float[pre + len + post];
        float dt = 1f / sr, rcH = 1f / (2f * Mathf.PI * 380f), aH = rcH / (rcH + dt), rcL = 1f / (2f * Mathf.PI * 3200f), aL = dt / (rcL + dt);
        float xPrev = 0f, yH = 0f, l1 = 0f, l2 = 0f; var rnd = new System.Random(n.GetHashCode());
        for (int i = 0; i < len; i++)
        {
            float x = 0f; for (int k = 0; k < canales; k++) x += d[i * canales + k]; x /= canales;
            yH = aH * (yH + x - xPrev); xPrev = x; l1 += aL * (yH - l1); l2 += aL * (l1 - l2);
            o[pre + i] = (float)System.Math.Tanh(l2 * 2.2f) * 0.8f;
        }
        for (int i = 0; i < o.Length; i++)
        {
            float ruido = ((float)rnd.NextDouble() * 2f - 1f) * 0.014f;
            float env = i < pre ? 1f - i / (float)pre * 0.4f : (i >= pre + len ? 1f - (i - pre - len) / (float)post : 1f);
            o[i] += ruido * env;
        }
        for (int i = 0; i < (int)(0.012f * sr) && i < pre; i++) o[i] += ((float)rnd.NextDouble() * 2f - 1f) * 0.25f * (1f - i / (0.012f * sr));       // tick al abrir
        for (int i = 0; i < (int)(0.05f * sr); i++) { int j = pre + len + i; if (j < o.Length) o[j] += ((float)rnd.NextDouble() * 2f - 1f) * 0.18f * (1f - i / (0.05f * sr)); }   // soplido al cerrar
        float pico = 0.001f; for (int i = 0; i < o.Length; i++) pico = Mathf.Max(pico, Mathf.Abs(o[i])); float g = 0.9f / pico; for (int i = 0; i < o.Length; i++) o[i] *= g;
        c = AudioClip.Create("vozR_" + n, o.Length, 1, sr, false); c.SetData(o, 0); vozCache[n] = c; return c;
    }

    // ------------------------------------------------------------------ radio COM y zonas
    int radioKhz = 118000;   // 118.000 a 136.950 MHz, de a 50 kHz
    int zonaPrev;
    static int ZonaFreq(double lat, double lon)
    {
        int z = (int)System.Math.Floor(lat * 4.0) * 131 + (int)System.Math.Floor(lon * 4.0) * 17; int n = ((z % 380) + 380) % 380; return 118000 + n * 50;
    }
    static string FreqTxt(int khz) { return (khz / 1000f).ToString("F2", CultureInfo.InvariantCulture); }
    void MoverRadio(int dKhz) { radioKhz = Mathf.Clamp(radioKhz + dKhz, 118000, 136950); }

    // ------------------------------------------------------------------ ordenes
    class Orden { public string tipo, clip, titulo, unidad, objTxt, pedido; public float obj; public bool texto; public float nace; public int fallos; }
    Orden orden; float proxOrden = 40f; string textoRespuesta = "";
    double oLat, oLon; float oAlt, oRumbo, oVel;   // datos vivos del avion (se refrescan cada cuadro)
    bool g8, g5, g3, miEsc, volverRumbo; string forzarSiguiente; float estableDesde = -1f, vueloDesde = -1f, velPrev, ultOrden; int ordenesVuelo;

    static string Norm(string t)
    {
        if (t == null) return "";
        string d = t.Normalize(System.Text.NormalizationForm.FormD); var sb = new System.Text.StringBuilder();
        foreach (char c in d) { var cat = CharUnicodeInfo.GetUnicodeCategory(c); if (cat == UnicodeCategory.NonSpacingMark) continue; if (char.IsLetterOrDigit(c)) sb.Append(char.ToLowerInvariant(c)); }
        return sb.ToString();
    }

    void NuevaOrden(string forzado, int param)
    {
        float agl = avion != null ? avion.AlturaSobrePiso : 999f; float kmh = oVel * 3.6f; var o = new Orden { nace = Time.unscaledTime };
        string tipo = forzado;
        if (tipo == null)
        {
            float q = UnityEngine.Random.value * 100f;
            tipo = q < 22 ? "alt" : q < 42 ? "rum" : q < 54 ? "vel" : q < 62 ? "mant_alt" : q < 70 ? "mant_rum" : q < 88 ? "datos" : "manual";
        }
        string clipForzado = null; if (tipo != null && tipo.StartsWith("m:")) { clipForzado = tipo.Substring(2); tipo = "manual"; }
        if (tipo == "vel" && kmh < 80f) tipo = "rum";
        bool subirSi = tipo == "alt_subir"; if (subirSi) tipo = "alt";
        o.tipo = tipo;
        switch (tipo)
        {
            case "alt":
            {
                bool sube = agl < 700f || oAlt < 2500f ? UnityEngine.Random.value < 0.7f : UnityEngine.Random.value < 0.4f; if (agl < 700f || subirSi) sube = true;
                float k = subirSi ? Mathf.Round(R(600f, 1200f) / 100f) * 100f : Mathf.Round(R(100f, 900f) / 100f) * 100f; float t = Mathf.Max(400f, Mathf.Round((sube ? oAlt + k : oAlt - k) / 100f) * 100f);
                if (Mathf.Abs(t - oAlt) < 150f) t = Mathf.Round((oAlt + 300f) / 100f) * 100f;
                sube = t > oAlt; o.clip = sube ? "gen_sube_alt" : "gen_baja_alt"; o.titulo = sube ? "SUBÍ A LA ALTITUD" : "BAJÁ A LA ALTITUD"; o.obj = t; o.objTxt = Mathf.RoundToInt(t).ToString(); o.unidad = "M"; break;
            }
            case "rum":
            {
                string dnr; double ra, rb2; float t; o.titulo = "GIRÁ AL RUMBO";
                if (Carrera.Destino(out dnr, out ra, out rb2))
                {
                    float rbd = Rumbo(oLat, oLon, ra, rb2);
                    if (Mathf.Abs(Mathf.DeltaAngle(oRumbo, rbd)) > 12f) { t = Mathf.Round(rbd / 10f) * 10f; o.titulo = "VOLVÉ AL RUMBO DEL DESTINO"; }
                    else { t = rbd + (UnityEngine.Random.value < 0.5f ? -1f : 1f) * Mathf.Round(R(15f, 25f) / 5f) * 5f; t = Mathf.Round(((t % 360f) + 360f) % 360f / 5f) * 5f; o.titulo = "DESVIATE UN POCO AL RUMBO"; volverRumbo = true; }
                }
                else { float delta = Mathf.Round(R(20f, 40f) / 10f) * 10f * (UnityEngine.Random.value < 0.5f ? -1f : 1f); t = Mathf.Round(((oRumbo + delta) % 360f + 360f) % 360f / 10f) * 10f; }
                if (t >= 360f) t = 0f;
                o.clip = "gen_gira_rumbo"; o.obj = t; o.objTxt = Mathf.RoundToInt(t).ToString("000"); o.unidad = "GRADOS"; break;
            }
            case "vel":
            {
                float t = Mathf.Round(kmh * R(0.85f, 1.15f) / 10f) * 10f; if (Mathf.Abs(t - kmh) < kmh * 0.07f) t = Mathf.Round(kmh * 1.12f / 10f) * 10f;
                o.clip = "gen_vel"; o.titulo = "AJUSTÁ LA VELOCIDAD"; o.obj = t; o.objTxt = Mathf.RoundToInt(t).ToString(); o.unidad = "KM/H"; break;
            }
            case "radio":
            case "freq":
            {
                int t = tipo == "freq" ? param : 118000 + UnityEngine.Random.Range(0, 380) * 50; if (t == radioKhz) t = Mathf.Clamp(t + 100, 118000, 136950);
                o.clip = tipo == "freq" ? "cambia_frec" : "gen_radio_freq"; o.titulo = tipo == "freq" ? "CAMBIASTE DE ZONA. SINTONIZÁ" : "SINTONIZÁ LA FRECUENCIA"; o.obj = t; o.objTxt = FreqTxt(t); o.unidad = "MHZ"; break;
            }
            case "mant_alt":
                o.clip = "gen_mantene_alt"; o.titulo = "MANTENÉ LA ALTITUD"; o.obj = Mathf.Round(oAlt / 10f) * 10f; o.objTxt = Mathf.RoundToInt(o.obj).ToString(); o.unidad = "M"; break;
            case "mant_rum":
                o.clip = "gen_mantene_rumbo"; o.titulo = "MANTENÉ EL RUMBO"; o.obj = Mathf.Round(oRumbo / 10f) * 10f; if (o.obj >= 360f) o.obj = 0f; o.objTxt = Mathf.RoundToInt(o.obj).ToString("000"); o.unidad = "GRADOS"; break;
            case "datos":
            {
                string dn; double a1, a2; bool hayDest = Carrera.Destino(out dn, out a1, out a2);
                string[,] L = { { "pide_altura", "ESCRIBÍ TU ALTURA ACTUAL" }, { "pide_rumbo", "ESCRIBÍ TU RUMBO ACTUAL" }, { "pide_velocidad", "ESCRIBÍ TU VELOCIDAD ACTUAL" }, { "pide_locacion", "ESCRIBÍ LA LOCALIDAD MÁS CERCANA" },
                    { "pide_destino", "ESCRIBÍ TU DESTINO" }, { "pide_matricula", "ESCRIBÍ LA MATRÍCULA" }, { "pide_nombre", "ESCRIBÍ TU NOMBRE DE PILOTO" }, { "pide_tipo_avion", "ESCRIBÍ EL TIPO DE AVIÓN" },
                    { "pide_hora", "ESCRIBÍ LA HORA DE TU RELOJ" }, { "pide_clima", "ESCRIBÍ CÓMO ESTÁ EL CLIMA" }, { "pide_combustible", "ESCRIBÍ TU COMBUSTIBLE" } };
                int i = UnityEngine.Random.Range(0, L.GetLength(0)); if (L[i, 0] == "pide_destino" && !hayDest) i = 0;
                o.clip = L[i, 0]; o.titulo = L[i, 1]; o.texto = true; o.pedido = L[i, 0]; o.objTxt = ""; o.unidad = ""; break;
            }
            default:
            {
                string mc = clipForzado ?? ManualAlAzar(); bool conoc; CondManual(mc, out conoc);
                o.clip = mc; o.titulo = TituloManual(mc); o.objTxt = ""; o.unidad = conoc ? "USÁ LOS BOTONES" : "CONFIRMÁ CON OK"; o.tipo = "manual"; break;
            }
        }
        orden = o; textoRespuesta = ""; DecirA(o.clip);
        Aviso("TORRE: " + o.titulo + (o.objTxt != "" ? " " + o.objTxt + " " + o.unidad : ""), 6f);
    }

    // chequea la orden activa con los datos reales; devuelve true si el piloto cumplio
    bool Cumple(Orden o, string txt)
    {
        float kmh = oVel * 3.6f;
        switch (o.tipo)
        {
            case "alt": case "mant_alt": return Mathf.Abs(oAlt - o.obj) <= (o.tipo == "alt" ? 80f : 50f);
            case "rum": case "mant_rum": return Mathf.Abs(Mathf.DeltaAngle(oRumbo, o.obj)) <= 10f;
            case "vel": return Mathf.Abs(kmh - o.obj) <= Mathf.Max(15f, o.obj * 0.06f);
            case "radio": case "freq": return radioKhz == Mathf.RoundToInt(o.obj);
            case "datos":
            {
                string t = (txt ?? "").Trim(); if (t == "") return false; float v; string num = "";
                foreach (char ch in t) if (char.IsDigit(ch) || ch == '.' || ch == ',') num += ch == ',' ? '.' : ch;
                bool tieneNum = float.TryParse(num, NumberStyles.Float, CultureInfo.InvariantCulture, out v);
                switch (o.pedido)
                {
                    case "pide_altura": return tieneNum && Mathf.Abs(v - oAlt) <= 150f;
                    case "pide_rumbo": return tieneNum && Mathf.Abs(Mathf.DeltaAngle(oRumbo, v)) <= 20f;
                    case "pide_velocidad": return tieneNum && Mathf.Abs(v - kmh) <= Mathf.Max(20f, kmh * 0.15f);
                    case "pide_locacion":
                    {
                        string nt = Norm(t); if (nt.Length < 3) return false; var cerca = Pueblos.Cercanos(oLat, oLon, 70.0);
                        if (cerca.Count == 0) return true; foreach (var p in cerca) { string np = Norm(p); if (np.Contains(nt) || nt.Contains(np)) return true; } return false;
                    }
                    case "pide_destino":
                    {
                        string dn; double a1, a2; if (!Carrera.Destino(out dn, out a1, out a2)) return true; string nd = Norm(dn), nt = Norm(t); return nt.Length >= 3 && (nd.Contains(nt) || nt.Contains(nd));
                    }
                    default: return true;   // matricula, nombre, hora, clima...: se acepta cualquier respuesta
                }
            }
            case "manual": { bool conoc; bool c = CondManual(o.clip, out conoc); return conoc ? c : true; }
            default: return true;
        }
    }

    void Confirmar()
    {
        if (orden == null) return;
        if (Cumple(orden, textoRespuesta)) Completar();
        else
        {
            orden.fallos++; DecirPrimero("incorrecto", "repeti");
            Aviso(orden.texto ? "TORRE: ESA RESPUESTA NO COINCIDE. REVISÁ LOS DATOS" : "TORRE: TODAVÍA NO. REVISÁ LOS DATOS Y APRETÁ OK", 5f);
            if (orden.fallos >= 3) { orden = null; ultOrden = Time.unscaledTime; proxOrden = Time.unscaledTime + 100f; Aviso("TORRE: ORDEN CANCELADA", 4f); }
        }
    }

    void Completar()
    {
        var o = orden; if (o == null) return;
        subtotal += 5; DecirPrimero("correcto", "bien_piloto", "bonus_cobrado", "buen_vuelo");
        if (o.texto) EcoRespuesta(o.pedido, textoRespuesta); else Aviso("TORRE: CORRECTO" + (NombrePiloto() != "" ? ", " + NombrePiloto() : "") + ". +$5   ACUMULADO $" + subtotal, 5f);
        orden = null; textoRespuesta = ""; ultOrden = Time.unscaledTime; proxOrden = Time.unscaledTime + R(40f, 80f);
        if (volverRumbo && o.tipo == "rum") { volverRumbo = false; proxOrden = Time.unscaledTime + 30f; forzarSiguiente = "rum"; }
    }

    // suelta el teclado: despues de enviar, el avion vuelve a responder al joystick y al teclado
    void SoltarTeclado() { GUI.FocusControl(""); GUIUtility.keyboardControl = 0; if (miEsc) { Controles.escribiendo = false; miEsc = false; } }

    void EnviarTexto()
    {
        string t = (textoRespuesta ?? "").Trim(); SoltarTeclado(); if (t == "") return;
        if (orden != null && orden.texto) { Confirmar(); return; }
        DecirPrimero("no_copio", "repeti"); Aviso("TORRE: NO ESPERO ESE MENSAJE AHORA. ESCRIBÍ CUANDO TE PIDA UN DATO", 5f); textoRespuesta = "";
    }

    void LlamarTorre()
    {
        if (EnFrecuencia) { DecirPrimero("copio_claro", "buen_vuelo"); Aviso("TORRE: TE COPIO FUERTE Y CLARO" + (NombrePiloto() != "" ? ", " + NombrePiloto() : ""), 4f); }
        else { radioKhz = FreqNecesaria(); DecirPrimero("copio_claro", "buen_vuelo"); Aviso("TORRE: TE COPIO. SINTONIZAMOS TU RADIO EN " + FreqTxt(radioKhz) + " MHZ", 6f); }   // antes cortaba con "no te copio": ahora sintoniza sola
    }

    // se llama cada cuadro desde Update (con el avion ya ubicado)
    void UpdateOrdenes(double lat, double lon, float alt, float rumbo, float vel, bool volando)
    {
        oLat = lat; oLon = lon; oAlt = alt; oRumbo = rumbo; oVel = vel;
        if (!ordenesOn) return; float ahora = Time.unscaledTime; bool tierra = avion != null && avion.EnTierra;
        int zf = ZonaFreq(lat, lon); if (zonaPrev == 0) zonaPrev = zf;
        // el avion esta "estable" cuando vuela, a mas de 300 m del piso y sin acelerar ni frenar fuerte durante 20 s (crucero)
        float dt = Mathf.Max(0.001f, Time.unscaledDeltaTime), acel = Mathf.Abs(vel - velPrev) / dt; velPrev = vel;
        float agl = avion != null ? avion.AlturaSobrePiso : 0f;
        if (!volando) { estableDesde = -1f; vueloDesde = -1f; if (vel < 5f) ordenesVuelo = 0; }
        else
        {
            if (vueloDesde < 0f) vueloDesde = ahora;
            if (agl > 300f && acel < 3f) { if (estableDesde < 0f) estableDesde = ahora; } else estableDesde = -1f;
        }
        bool crucero = estableDesde >= 0f && ahora - estableDesde > 12f;
        UpdateSalida(ahora, tierra, vel);
        UpdateAproximacion(ahora, lat, lon, volando);
        if (consulta != null && ahora > consulta.vence) consulta = null;
        // ordenes de cabina: si el piloto ya apreto el boton, se confirman solas
        if (orden != null && orden.tipo == "manual") { bool conoc; if (CondManual(orden.clip, out conoc) && conoc) Completar(); }
        // espaciado: se reparte el viaje en unas 6 ordenes (entre 100 y 300 s); sin destino, cada ~3 minutos
        string dn0; double dla0, dlo0; float espacio = 90f;
        if (Carrera.Destino(out dn0, out dla0, out dlo0) && vel > 20f) espacio = Mathf.Clamp((float)(Dist(lat, lon, dla0, dlo0) / vel) / 12f, 50f, 140f);
        if (aprox) zonaPrev = zf;
        else if (zf != zonaPrev && volando) { zonaPrev = zf; if (crucero && ahora - ultOrden > 30f && (orden == null || orden.tipo != "freq")) NuevaOrden("freq", zf); }
        if (orden != null && ahora - orden.nace > 240f) { Aviso("TORRE: ORDEN VENCIDA", 4f); orden = null; ultOrden = ahora; proxOrden = ahora + espacio; }
        // primera orden del vuelo: subir a una altitud, sin apuro, apenas despega. Despues, solo en crucero y bien espaciadas (y nunca en la aproximacion)
        if (!aprox)
        {
            if (orden == null && volando && ordenesVuelo == 0 && agl > 100f && ahora - vueloDesde > (faseSalida == 5 ? 45f : 12f)) { NuevaOrden("alt_subir", 0); ordenesVuelo++; ultOrden = ahora; proxOrden = ahora + espacio; }
            else if (orden == null && crucero && ahora >= proxOrden) { NuevaOrden(forzarSiguiente, 0); forzarSiguiente = null; ordenesVuelo++; ultOrden = ahora; }
        }
        // charla de fondo: la torre habla sola cada ~1 minuto (solo si estas sintonizado en la frecuencia de la zona)
        if (!aprox && volando && crucero && consulta == null && ahora >= proxHabla && EnFrecuencia && vozCola.Count == 0 && (vozSrc == null || !vozSrc.isPlaying))
        { CharlaFondo(lat, lon); proxHabla = ahora + R(20f, 35f); }
    }

    // ------------------------------------------------------------------ interfaz de la torre (se dibuja en el espacio virtual del cuadro)
    GUIStyle sTexto;
    void OrdenesGUIViejo(Rect r, float x0, float y4, Color ambar, Color verde, Color cian, Color blanco)
    {
        if (sTexto == null) { sTexto = new GUIStyle(GUI.skin.textField) { font = fuente, fontSize = 14, fontStyle = FontStyle.Bold }; sTexto.normal.textColor = Color.white; sTexto.focused.textColor = Color.white; }
        var bgc = GUI.backgroundColor; float W = r.width - 20f;
        Rc(x0, y4 - 3f, W, 1f, new Color(0.2f, 0.75f, 0.95f, 0.3f));
        GUI.Label(new Rect(x0, y4, 200f, 14f), "ORDEN DE LA TORRE", sEtq);
        GUI.backgroundColor = ordenesOn ? new Color(0.3f, 1f, 0.45f) : new Color(0.75f, 0.75f, 0.75f);
        if (GUI.Button(new Rect(x0 + W - 150f, y4 - 1f, 150f, 18f), ordenesOn ? "TORRE HABLA: ON" : "TORRE HABLA: OFF", sBtn)) { ordenesOn = !ordenesOn; if (!ordenesOn) { orden = null; vozCola.Clear(); } else { proxOrden = Time.unscaledTime + 8f; Decir("torre_llama"); } }
        GUI.backgroundColor = bgc;
        float y = y4 + 16f;
        if (orden != null)
        {
            GUI.Label(new Rect(x0, y, W * 0.62f, 24f), Recortar(orden.titulo, sTit, W * 0.62f - 4f), sTit);
            if (orden.objTxt != "") { Seg7(x0 + W * 0.62f, y - 2f, 30f, orden.objTxt, ambar); GUI.Label(new Rect(x0 + W * 0.62f, y + 31f, W * 0.38f, 14f), orden.unidad + AhoraTxt(orden), sEtq); }
            else GUI.Label(new Rect(x0, y + 22f, W, 14f), orden.texto ? "ESCRIBÍ LA RESPUESTA ABAJO Y APRETÁ ENVIAR" : "HACELO Y CONFIRMÁ CON OK", sEtq);
        }
        else GUI.Label(new Rect(x0, y + 4f, W, 24f), ordenesOn ? "SIN ÓRDENES. LA TORRE TE VA A LLAMAR" : "ACTIVÁ LA TORRE PARA RECIBIR ÓRDENES", sMsg);
        y += 50f;
        GUI.backgroundColor = orden != null ? new Color(0.3f, 1f, 0.45f) : new Color(0.5f, 0.5f, 0.5f);
        if (GUI.Button(new Rect(x0, y, 118f, 24f), "OK, LISTO", sBtn) && orden != null) Confirmar();
        GUI.backgroundColor = bgc;
        if (GUI.Button(new Rect(x0 + 126f, y, 110f, 24f), "REPETIR", sBtn) && orden != null) DecirA(orden.clip);
        if (GUI.Button(new Rect(x0 + 244f, y, 150f, 24f), "LLAMAR A TORRE", sBtn)) LlamarTorre();
        y += 32f;
        GUI.Label(new Rect(x0, y, 200f, 14f), "RADIO COM  MHZ", sEtq);
        string ft = FreqTxt(radioKhz); Seg7(x0, y + 15f, 28f, ft, cian);
        float bx = x0 + AnchoSeg(ft, 28f) + 14f, by = y + 16f;
        if (GUI.Button(new Rect(bx, by, 40f, 26f), "-1", sBtn)) MoverRadio(-1000);
        if (GUI.Button(new Rect(bx + 44f, by, 44f, 26f), "-.05", sBtn)) MoverRadio(-50);
        if (GUI.Button(new Rect(bx + 92f, by, 44f, 26f), "+.05", sBtn)) MoverRadio(50);
        if (GUI.Button(new Rect(bx + 140f, by, 40f, 26f), "+1", sBtn)) MoverRadio(1000);
        y += 50f;
        GUI.Label(new Rect(x0, y, 200f, 14f), "RESPUESTA A LA TORRE", sEtq);
        GUI.SetNextControlName("torre_txt");
        textoRespuesta = GUI.TextField(new Rect(x0, y + 15f, W - 102f, 24f), textoRespuesta, 40, sTexto);
        bool foco = GUI.GetNameOfFocusedControl() == "torre_txt";
        { var e0 = Event.current; var rt = new Rect(x0, y + 15f, W - 102f, 24f); if (foco && (e0.type == EventType.MouseDown || e0.rawType == EventType.MouseDown) && !rt.Contains(e0.mousePosition)) { SoltarTeclado(); foco = false; } }   // click afuera = deja de escribir
        if (foco) { Controles.escribiendo = true; miEsc = true; } else if (miEsc) { Controles.escribiendo = false; miEsc = false; }
        var ev = Event.current; if (foco && ev.type == EventType.KeyDown && (ev.keyCode == KeyCode.Return || ev.keyCode == KeyCode.KeypadEnter)) { EnviarTexto(); ev.Use(); }
        if (foco && ev.type == EventType.KeyDown && ev.keyCode == KeyCode.Escape) { GUI.FocusControl(""); ev.Use(); }
        if (GUI.Button(new Rect(x0 + W - 96f, y + 15f, 96f, 24f), "ENVIAR", sBtn)) EnviarTexto();
    }

    string AhoraTxt(Orden o)
    {
        switch (o.tipo)
        {
            case "alt": case "mant_alt": return "   AHORA " + Mathf.RoundToInt(oAlt);
            case "rum": case "mant_rum": return "   AHORA " + Mathf.RoundToInt(oRumbo).ToString("000");
            case "vel": return "   AHORA " + Mathf.RoundToInt(oVel * 3.6f);
            case "radio": case "freq": return "   AHORA " + FreqTxt(radioKhz);
            default: return "";
        }
    }
}
