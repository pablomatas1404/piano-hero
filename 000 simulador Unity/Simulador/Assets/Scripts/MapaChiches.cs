// Chiches del mapa (app Mapa de la tablet): estela, anillos de alcance, proa con tiempo, datos de vuelo,
// recorrido con varias paradas, marcadores propios y capas de aeropuertos. Todo se maneja desde el boton CHICHES del mapa.
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

public partial class MapaAuto
{
    class Wp { public double lat, lon; public string nombre; }

    // ---- estado ----
    bool chInit, chAbierto, chEstela = true, chAnillos, chProa = true, chDatos = true, capaAero, soloPista, chMarcas = true, modoRec, modoMarca, rutaSeguir;
    int rutaIdx, marcaSel = -1, marcaN;
    readonly List<Wp> chRuta = new List<Wp>(), chMarcasL = new List<Wp>();
    readonly List<double> estLat = new List<double>(), estLon = new List<double>();
    double ultLat, ultLon, ultAlt; float ultT, velMs, vsMs; bool hayUlt;
    Vector2 chDown; bool chDownOk; Rect rectPanelCh; GUIStyle sCh, sChB;

    // ---- utilidades ----
    static double DistM(double la1, double lo1, double la2, double lo2)
    {
        double dla = (la2 - la1) * 111320.0, dlo = (lo2 - lo1) * 111320.0 * System.Math.Cos((la1 + la2) * 0.5 * System.Math.PI / 180.0);
        return System.Math.Sqrt(dla * dla + dlo * dlo);
    }
    static float RumboA(double la1, double lo1, double la2, double lo2)
    {
        double dla = la2 - la1, dlo = (lo2 - lo1) * System.Math.Cos((la1 + la2) * 0.5 * System.Math.PI / 180.0);
        float b = (float)(System.Math.Atan2(dlo, dla) * 180.0 / System.Math.PI); if (b < 0f) b += 360f; return b;
    }
    Vector2 PxCh(double lat, double lon, double izq, double arr) { return new Vector2((float)(LonAPx(lon, zoomMapa) - izq), (float)(LatAPx(lat, zoomMapa) - arr)); }
    static string Dur(double seg) { if (seg < 0 || double.IsInfinity(seg) || double.IsNaN(seg)) return "--"; int m = Mathf.RoundToInt((float)seg / 60f); return m < 60 ? m + " min" : (m / 60) + " h " + (m % 60).ToString("00") + " min"; }
    static string Km(double m) { return m < 1000 ? Mathf.RoundToInt((float)m) + " m" : (m / 1000.0).ToString(m < 10000 ? "0.0" : "0", CultureInfo.InvariantCulture) + " km"; }

    void LineaPx(Vector2 a, Vector2 b, Color c, float w, Rect clip)
    {
        Vector2 d = b - a; float L = d.magnitude; if (L < 0.5f) return;
        if ((a.x < -8f && b.x < -8f) || (a.y < -8f && b.y < -8f) || (a.x > clip.width + 8f && b.x > clip.width + 8f) || (a.y > clip.height + 8f && b.y > clip.height + 8f)) return;
        var m0 = GUI.matrix; var c0 = GUI.color; GUIUtility.RotateAroundPivot(Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg, a); GUI.color = c; GUI.DrawTexture(new Rect(a.x, a.y - w * 0.5f, L, w), Texture2D.whiteTexture); GUI.matrix = m0; GUI.color = c0;
    }
    void TxtCh(Rect rc, string s, Color col)
    {
        if (sCh == null) { sCh = new GUIStyle(GUI.skin.label) { fontSize = 11, fontStyle = FontStyle.Bold, clipping = TextClipping.Overflow }; }
        var c0 = GUI.color; sCh.normal.textColor = Color.black; GUI.Label(new Rect(rc.x + 1f, rc.y + 1f, rc.width, rc.height), s, sCh); sCh.normal.textColor = col; GUI.Label(rc, s, sCh); GUI.color = c0;
    }

    // ---- guardado ----
    static string Serializar(List<Wp> l) { var sb = new System.Text.StringBuilder(); foreach (var w in l) sb.Append(w.lat.ToString("R", CultureInfo.InvariantCulture)).Append('|').Append(w.lon.ToString("R", CultureInfo.InvariantCulture)).Append('|').Append((w.nombre ?? "").Replace("|", " ").Replace(";", " ")).Append(';'); return sb.ToString(); }
    static void Leer(string s, List<Wp> l)
    {
        l.Clear(); if (string.IsNullOrEmpty(s)) return;
        foreach (var t in s.Split(';')) { var p = t.Split('|'); double la, lo; if (p.Length >= 2 && double.TryParse(p[0], NumberStyles.Float, CultureInfo.InvariantCulture, out la) && double.TryParse(p[1], NumberStyles.Float, CultureInfo.InvariantCulture, out lo)) l.Add(new Wp { lat = la, lon = lo, nombre = p.Length > 2 ? p[2] : "" }); }
    }
    void GuardarCh()
    {
        PlayerPrefs.SetString("mapa_ch_ruta", Serializar(chRuta)); PlayerPrefs.SetString("mapa_ch_marcas", Serializar(chMarcasL)); PlayerPrefs.SetInt("mapa_ch_idx", rutaIdx);
        int f = (chEstela ? 1 : 0) | (chAnillos ? 2 : 0) | (chProa ? 4 : 0) | (chDatos ? 8 : 0) | (capaAero ? 16 : 0) | (soloPista ? 32 : 0) | (chMarcas ? 64 : 0);
        PlayerPrefs.SetInt("mapa_ch_flags", f); PlayerPrefs.Save();
    }
    void ChichesInit()
    {
        chInit = true; Leer(PlayerPrefs.GetString("mapa_ch_ruta", ""), chRuta); Leer(PlayerPrefs.GetString("mapa_ch_marcas", ""), chMarcasL); rutaIdx = Mathf.Clamp(PlayerPrefs.GetInt("mapa_ch_idx", 0), 0, chRuta.Count);
        int f = PlayerPrefs.GetInt("mapa_ch_flags", 1 | 4 | 8 | 64); chEstela = (f & 1) != 0; chAnillos = (f & 2) != 0; chProa = (f & 4) != 0; chDatos = (f & 8) != 0; capaAero = (f & 16) != 0; soloPista = (f & 32) != 0; chMarcas = (f & 64) != 0;
        marcaN = chMarcasL.Count;
        if (PlayerPrefs.GetInt("mapa_ch_v2", 0) < 1) { chEstela = false; chProa = false; PlayerPrefs.SetInt("mapa_ch_v2", 1); GuardarCh(); }   // las lineas de estela y proa arrancan apagadas: se prenden desde CHICHES
    }

    // ---- se llama desde Update: va dejando la estela, calcula velocidad y avanza el recorrido ----
    void ChichesUpdate()
    {
        if (!chInit) ChichesInit();
        double lon, lat; float alt, rumbo; if (!PosActiva(out lon, out lat, out alt, out rumbo)) return;
        if (!hayUlt) { ultLat = lat; ultLon = lon; ultAlt = alt; ultT = Time.time; hayUlt = true; estLat.Add(lat); estLon.Add(lon); return; }
        float dt = Time.time - ultT; if (dt < 0.5f) return;
        double d = DistM(ultLat, ultLon, lat, lon);
        if (d > 5000.0) { estLat.Clear(); estLon.Clear(); velMs = 0f; vsMs = 0f; }   // te teletransportaste: la estela vieja ya no sirve
        else { velMs = Mathf.Lerp(velMs, (float)(d / dt), 0.35f); vsMs = Mathf.Lerp(vsMs, (float)((alt - ultAlt) / dt), 0.35f); }
        ultLat = lat; ultLon = lon; ultAlt = alt; ultT = Time.time;
        if (estLat.Count == 0 || DistM(estLat[estLat.Count - 1], estLon[estLon.Count - 1], lat, lon) >= 40.0)
        {
            estLat.Add(lat); estLon.Add(lon);
            if (estLat.Count > 1800) { estLat.RemoveRange(0, 300); estLon.RemoveRange(0, 300); }
        }
        if (rutaSeguir && rutaIdx < chRuta.Count && DistM(lat, lon, chRuta[rutaIdx].lat, chRuta[rutaIdx].lon) < 1500.0)
        {
            rutaIdx++; GuardarCh();
            if (rutaIdx >= chRuta.Count) { rutaSeguir = false; textoAviso = "Recorrido completo!"; }
            else textoAviso = "Parada alcanzada. Ahora: parada " + (rutaIdx + 1);
            avisoHasta = Time.time + 4f;
        }
    }

    // ---- dibujo dentro del mapa (coordenadas relativas al mapa) ----
    void DibujarChichesMapa(Rect r, double izq, double arr)
    {
        double lon, lat; float alt, rumbo; bool hay = PosActiva(out lon, out lat, out alt, out rumbo);
        var clip = new Rect(0f, 0f, r.width, r.height);
        // estela: por donde volaste
        if (chEstela && estLat.Count > 1)
        {
            var ce = new Color(0.25f, 0.9f, 1f, 0.9f); Vector2 ant = PxCh(estLat[0], estLon[0], izq, arr);
            for (int i = 1; i < estLat.Count; i++) { Vector2 p = PxCh(estLat[i], estLon[i], izq, arr); LineaPx(ant, p, ce, 2.5f, clip); ant = p; }
            if (hay) LineaPx(ant, PxCh(lat, lon, izq, arr), ce, 2.5f, clip);
        }
        // recorrido
        if (chRuta.Count > 0)
        {
            var co = new Color(1f, 0.62f, 0.1f, 0.95f); Vector2 ant = Vector2.zero; double total = 0;
            for (int i = 0; i < chRuta.Count; i++)
            {
                Vector2 p = PxCh(chRuta[i].lat, chRuta[i].lon, izq, arr);
                if (i > 0)
                {
                    bool hecho = rutaSeguir && i <= rutaIdx; LineaPx(ant, p, hecho ? new Color(0.6f, 0.6f, 0.6f, 0.7f) : co, 3f, clip);
                    double dseg = DistM(chRuta[i - 1].lat, chRuta[i - 1].lon, chRuta[i].lat, chRuta[i].lon); total += dseg;
                    if (zoomMapa >= 7 && (ant - p).magnitude > 70f) TxtCh(new Rect((ant.x + p.x) * 0.5f - 20f, (ant.y + p.y) * 0.5f - 18f, 80f, 16f), Km(dseg), new Color(1f, 0.85f, 0.5f));
                }
                ant = p;
            }
            var cir = Circulo(); var c0 = GUI.color;
            for (int i = 0; i < chRuta.Count; i++)
            {
                Vector2 p = PxCh(chRuta[i].lat, chRuta[i].lon, izq, arr); if (p.x < -20f || p.y < -20f || p.x > r.width + 20f || p.y > r.height + 20f) continue;
                bool sig = rutaSeguir && i == rutaIdx; float rad = sig ? 13f : 10f;
                GUI.color = Color.black; GUI.DrawTexture(new Rect(p.x - rad - 1.5f, p.y - rad - 1.5f, 2f * rad + 3f, 2f * rad + 3f), cir);
                GUI.color = sig ? new Color(0.4f, 1f, 0.5f) : new Color(1f, 0.62f, 0.1f); GUI.DrawTexture(new Rect(p.x - rad, p.y - rad, 2f * rad, 2f * rad), cir);
                if (sChB == null) sChB = new GUIStyle(GUI.skin.label) { fontSize = 12, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
                sChB.normal.textColor = Color.black; GUI.color = Color.white; GUI.Label(new Rect(p.x - rad, p.y - rad, 2f * rad, 2f * rad), (i + 1).ToString(), sChB);
                if (!string.IsNullOrEmpty(chRuta[i].nombre) && zoomMapa >= 9) TxtCh(new Rect(p.x + rad + 2f, p.y - 8f, 150f, 16f), chRuta[i].nombre, Color.white);
            }
            GUI.color = c0;
        }
        // anillos de alcance
        if (chAnillos && hay)
        {
            double mpp = 156543.03392 * System.Math.Cos(lat * System.Math.PI / 180.0) / System.Math.Pow(2.0, zoomMapa); Vector2 c = PxCh(lat, lon, izq, arr); var cw = new Color(1f, 1f, 1f, 0.55f);
            foreach (int km in new[] { 10, 25, 50 })
            {
                float rad = (float)(km * 1000.0 / mpp); if (rad < 12f || rad > 6000f) continue;
                int N = 64; Vector2 prev = c + new Vector2(rad, 0f);
                for (int i = 1; i <= N; i++) { float a = i * Mathf.PI * 2f / N; Vector2 p = c + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * rad; LineaPx(prev, p, cw, 1.5f, clip); prev = p; }
                TxtCh(new Rect(c.x + 4f, c.y - rad - 14f, 60f, 16f), km + " km", new Color(1f, 1f, 1f, 0.9f));
            }
        }
        // proa: hacia donde vas (o hacia la proxima parada)
        if (chProa && hay)
        {
            Vector2 a = PxCh(lat, lon, izq, arr); Vector2 b; double distM; float vv = Mathf.Max(velMs, 25f);
            bool haciaParada = rutaSeguir && rutaIdx < chRuta.Count;
            if (haciaParada) { var w = chRuta[rutaIdx]; b = PxCh(w.lat, w.lon, izq, arr); distM = DistM(lat, lon, w.lat, w.lon); }
            else
            {
                double mpp = 156543.03392 * System.Math.Cos(lat * System.Math.PI / 180.0) / System.Math.Pow(2.0, zoomMapa); distM = vv * 600.0;   // 10 minutos hacia adelante
                float rr = rumbo * Mathf.Deg2Rad; b = a + new Vector2(Mathf.Sin(rr), -Mathf.Cos(rr)) * (float)(distM / mpp);
            }
            LineaPx(a, b, new Color(0.45f, 1f, 0.55f, 0.9f), 2f, clip);
            TxtCh(new Rect(b.x + 6f, b.y - 8f, 170f, 16f), haciaParada ? "parada " + (rutaIdx + 1) + ": " + Km(distM) + " - " + Dur(distM / vv) : "10 min: " + Km(distM), new Color(0.7f, 1f, 0.75f));
        }
        // marcadores propios
        if (chMarcas)
        {
            var cir = Circulo(); var c0 = GUI.color;
            for (int i = 0; i < chMarcasL.Count; i++)
            {
                Vector2 p = PxCh(chMarcasL[i].lat, chMarcasL[i].lon, izq, arr); if (p.x < -20f || p.y < -30f || p.x > r.width + 20f || p.y > r.height + 20f) continue;
                bool sel = i == marcaSel;
                GUI.color = Color.black; GUI.DrawTexture(new Rect(p.x - 8f, p.y - 8f, 16f, 16f), cir);
                GUI.color = sel ? Color.white : new Color(1f, 0.9f, 0.15f); GUI.DrawTexture(new Rect(p.x - 6.5f, p.y - 6.5f, 13f, 13f), cir);
                TxtCh(new Rect(p.x + 9f, p.y - 8f, 150f, 16f), chMarcasL[i].nombre, new Color(1f, 0.95f, 0.5f));
            }
            GUI.color = c0;
        }
        Escolta.DibujarEnMapa(r, (la, lo) => PxCh(la, lo, izq, arr), Circulo());
        Casa.DibujarEnMapa(r, (la, lo) => PxCh(la, lo, izq, arr), Circulo());   // la casa del jugador (punto verde)   // avion guia de la escolta (punto rojo)
    }

    bool chBarraInit, mapaLimpio;
    bool Tg(Rect rc, string t, bool v) { var bg = GUI.backgroundColor; GUI.backgroundColor = v ? new Color(0.5f, 1f, 0.55f) : Color.white; bool n = GUI.Toggle(rc, v, t, GUI.skin.button); GUI.backgroundColor = bg; return n; }
    // en la tablet hay lugar: todos los botones repartidos en filas, siempre a la vista
    Rect PanelBarra(Event e, Rect r)
    {
        float x0 = r.x + 4f, w = (r.width - 8f - 9f) / 4f, h = 22f, g = 3f, y = r.y + 40f; int filas = 3 + (marcaSel >= 0 && marcaSel < chMarcasL.Count ? 1 : 0);
        var fondo = new Rect(r.x, r.y + 37f, r.width, 5f + filas * (h + g)); var c0 = GUI.color; GUI.color = new Color(0f, 0f, 0f, 0.72f); GUI.DrawTexture(fondo, Texture2D.whiteTexture); GUI.color = c0;
        bool ch0 = false, n; var bg = GUI.backgroundColor;
        n = Tg(new Rect(x0, y, w, h), "Estela celeste", chEstela); if (n != chEstela) { chEstela = n; ch0 = true; }
        n = Tg(new Rect(x0 + (w + g), y, w, h), "Proa verde", chProa); if (n != chProa) { chProa = n; ch0 = true; }
        n = Tg(new Rect(x0 + (w + g) * 2, y, w, h), "Anillos km", chAnillos); if (n != chAnillos) { chAnillos = n; ch0 = true; }
        n = Tg(new Rect(x0 + (w + g) * 3, y, w, h), "Datos vuelo", chDatos); if (n != chDatos) { chDatos = n; ch0 = true; }
        y += h + g;
        n = Tg(new Rect(x0, y, w, h), "Aeropuertos", capaAero); if (n != capaAero) { capaAero = n; ch0 = true; }
        n = Tg(new Rect(x0 + (w + g), y, w, h), "Solo con pista", soloPista); if (n != soloPista) { soloPista = n; ch0 = true; }
        n = Tg(new Rect(x0 + (w + g) * 2, y, w, h), "Mis marcas", chMarcas); if (n != chMarcas) { chMarcas = n; ch0 = true; }
        if (GUI.Button(new Rect(x0 + (w + g) * 3, y, w, h), "Borrar estela")) { estLat.Clear(); estLon.Clear(); }
        y += h + g;
        n = Tg(new Rect(x0, y, w, h), modoRec ? "TOCA EL MAPA" : "ARMAR RECORRIDO", modoRec); if (n != modoRec) { modoRec = n; if (n) modoMarca = false; }
        if (chRuta.Count > 0) { n = Tg(new Rect(x0 + (w + g), y, w, h), rutaSeguir ? "SIGUIENDO" : "SEGUIR RUTA", rutaSeguir); if (n != rutaSeguir) { rutaSeguir = n; if (n && rutaIdx >= chRuta.Count) rutaIdx = 0; GuardarCh(); } }
        if (chRuta.Count > 0 && GUI.Button(new Rect(x0 + (w + g) * 2, y, w, h), "Quitar parada")) { chRuta.RemoveAt(chRuta.Count - 1); rutaIdx = Mathf.Min(rutaIdx, chRuta.Count); if (chRuta.Count == 0) rutaSeguir = false; GuardarCh(); }
        if (chRuta.Count > 0 && GUI.Button(new Rect(x0 + (w + g) * 3, y, w, h), "Borrar recorrido")) { chRuta.Clear(); rutaIdx = 0; rutaSeguir = false; GuardarCh(); }
        y += h + g;
        if (marcaSel >= 0 && marcaSel < chMarcasL.Count)
        {
            var m = chMarcasL[marcaSel];
            n = Tg(new Rect(x0, y, w, h), modoMarca ? "TOCA EL MAPA" : "PONER MARCA", modoMarca); if (n != modoMarca) { modoMarca = n; if (n) modoRec = false; }
            if (GUI.Button(new Rect(x0 + (w + g), y, w, h), "A la ruta")) { chRuta.Add(new Wp { lat = m.lat, lon = m.lon, nombre = m.nombre }); GuardarCh(); }
            if (GUI.Button(new Rect(x0 + (w + g) * 2, y, w, h), "Centrar")) { centroLat = m.lat; centroLon = m.lon; sigueAvion = false; tPanMapa = Time.unscaledTime + 8f; }
            if (GUI.Button(new Rect(x0 + (w + g) * 3, y, w, h), "Borrar " + m.nombre)) { chMarcasL.RemoveAt(marcaSel); marcaSel = -1; GuardarCh(); }
        }
        else { n = Tg(new Rect(x0, y, w, h), modoMarca ? "TOCA EL MAPA" : "PONER MARCA", modoMarca); if (n != modoMarca) { modoMarca = n; if (n) modoRec = false; } }
        GUI.backgroundColor = bg; if (ch0) GuardarCh();
        if (e.type == EventType.MouseDown && fondo.Contains(e.mousePosition)) e.Use();
        return new Rect(r.x + 4f, r.y + 18f, r.width, fondo.yMax - (r.y + 18f));
    }

    // ---- panel de botones y clics (coordenadas de pantalla) ----
    bool TogCh(ref float y, Rect p, string t, bool v) { bool n = GUI.Toggle(new Rect(p.x + 8f, y, p.width - 16f, 20f), v, t); y += 21f; return n; }
    bool BotCh(ref float y, Rect p, string t) { bool b = GUI.Button(new Rect(p.x + 8f, y, p.width - 16f, 22f), t); y += 24f; return b; }

    void DibujarPanelChiches(Event e, Rect r, double izq, double arr)
    {
        if (!chInit) ChichesInit();
        if (mapaLimpio)   // modo "solo mapa": nada de botones, un cuadradito para volver a mostrarlos
        {
            rectPanelCh = new Rect(r.x + 4f, r.y + 4f, 30f, 24f);
            if (GUI.Button(rectPanelCh, "\u2630")) mapaLimpio = false;
            return;
        }
        double lon, lat; float alt, rumbo; bool hay = PosActiva(out lon, out lat, out alt, out rumbo);
        // tiras de datos abajo (arriba de los botones)
        float vv = Mathf.Max(velMs, 25f), yb = r.yMax - 30f;
        if (chRuta.Count > 0)
        {
            double total = 0, resto = 0; for (int i = 1; i < chRuta.Count; i++) total += DistM(chRuta[i - 1].lat, chRuta[i - 1].lon, chRuta[i].lat, chRuta[i].lon);
            if (hay && rutaSeguir && rutaIdx < chRuta.Count) { resto = DistM(lat, lon, chRuta[rutaIdx].lat, chRuta[rutaIdx].lon); for (int i = rutaIdx + 1; i < chRuta.Count; i++) resto += DistM(chRuta[i - 1].lat, chRuta[i - 1].lon, chRuta[i].lat, chRuta[i].lon); } else resto = total;
            string t = "RECORRIDO: " + chRuta.Count + " paradas - " + Km(total) + (rutaSeguir ? "   Falta " + Km(resto) + " (" + Dur(resto / vv) + ")" : "   a " + Mathf.RoundToInt(vv * 3.6f) + " km/h: " + Dur(total / vv));
            var br = new Rect(r.x + 4f, yb - 22f, Mathf.Min(r.width - 8f, 470f), 20f); GUI.color = new Color(0f, 0f, 0f, 0.6f); GUI.DrawTexture(br, Texture2D.whiteTexture); GUI.color = Color.white; TxtCh(new Rect(br.x + 4f, br.y + 2f, br.width - 8f, 16f), t, new Color(1f, 0.8f, 0.4f)); yb -= 22f;
        }
        if (chDatos && hay)
        {
            string t = "ALT " + Mathf.RoundToInt(alt) + " m (" + Mathf.RoundToInt(alt * 3.281f) + " ft)   VEL " + Mathf.RoundToInt(velMs * 3.6f) + " km/h (" + Mathf.RoundToInt(velMs * 1.944f) + " kt)   RUMBO " + Mathf.RoundToInt(rumbo).ToString("000") + "   V/S " + (vsMs >= 0 ? "+" : "") + Mathf.RoundToInt(vsMs * 60f) + " m/min";
            var br = new Rect(r.x + 4f, yb - 22f, Mathf.Min(r.width - 8f, 520f), 20f); GUI.color = new Color(0f, 0f, 0f, 0.6f); GUI.DrawTexture(br, Texture2D.whiteTexture); GUI.color = Color.white; TxtCh(new Rect(br.x + 4f, br.y + 2f, br.width - 8f, 16f), t, new Color(0.85f, 0.95f, 1f));
        }
        // boton CHICHES y panel
        if (GUI.Button(new Rect(r.x + 4f, r.y + 18f, 92f, 20f), chAbierto ? "CHICHES  ▴" : "CHICHES  ▾")) chAbierto = !chAbierto;
        if (GUI.Button(new Rect(r.x + 100f, r.y + 18f, 84f, 20f), "SOLO MAPA")) { mapaLimpio = true; return; }
        rectPanelCh = new Rect(r.x + 4f, r.y + 18f, 180f, 20f);
        bool barra = enTablet && r.width >= 420f;
        if (barra && !chBarraInit) { chBarraInit = true; chAbierto = true; }   // en la tablet los botones arrancan a la vista, repartidos en filas
        if (chAbierto && barra) { rectPanelCh = PanelBarra(e, r); }
        else if (chAbierto)
        {
            var p = new Rect(r.x + 4f, r.y + 40f, 200f, 300f); float y = p.y + 4f; var c0 = GUI.color;
            GUI.color = new Color(0f, 0f, 0f, 0.78f); GUI.DrawTexture(p, Texture2D.whiteTexture); GUI.color = c0;
            bool ch0 = false; bool n;
            n = TogCh(ref y, p, "Estela de mi vuelo", chEstela); if (n != chEstela) { chEstela = n; ch0 = true; }
            n = TogCh(ref y, p, "Anillos 10/25/50 km", chAnillos); if (n != chAnillos) { chAnillos = n; ch0 = true; }
            n = TogCh(ref y, p, "Proa y tiempo", chProa); if (n != chProa) { chProa = n; ch0 = true; }
            n = TogCh(ref y, p, "Datos de vuelo", chDatos); if (n != chDatos) { chDatos = n; ch0 = true; }
            n = TogCh(ref y, p, "Aeropuertos", capaAero); if (n != capaAero) { capaAero = n; ch0 = true; }
            n = TogCh(ref y, p, "   solo con pista", soloPista); if (n != soloPista) { soloPista = n; ch0 = true; }
            n = TogCh(ref y, p, "Mis marcas", chMarcas); if (n != chMarcas) { chMarcas = n; ch0 = true; }
            y += 4f;
            var bg = GUI.backgroundColor;
            GUI.backgroundColor = modoRec ? new Color(1f, 0.7f, 0.2f) : bg; if (BotCh(ref y, p, modoRec ? "ARMANDO RECORRIDO (tocá el mapa)" : "ARMAR RECORRIDO")) { modoRec = !modoRec; if (modoRec) modoMarca = false; }
            GUI.backgroundColor = rutaSeguir ? new Color(0.4f, 1f, 0.5f) : bg; if (chRuta.Count > 0 && BotCh(ref y, p, rutaSeguir ? "SIGUIENDO RUTA (parar)" : "SEGUIR RUTA")) { rutaSeguir = !rutaSeguir; if (rutaSeguir && rutaIdx >= chRuta.Count) rutaIdx = 0; GuardarCh(); }
            GUI.backgroundColor = bg;
            if (chRuta.Count > 0 && BotCh(ref y, p, "Quitar ultima parada")) { chRuta.RemoveAt(chRuta.Count - 1); rutaIdx = Mathf.Min(rutaIdx, chRuta.Count); if (chRuta.Count == 0) rutaSeguir = false; GuardarCh(); }
            if (chRuta.Count > 0 && BotCh(ref y, p, "Borrar recorrido")) { chRuta.Clear(); rutaIdx = 0; rutaSeguir = false; GuardarCh(); }
            GUI.backgroundColor = modoMarca ? new Color(1f, 0.95f, 0.3f) : bg; if (BotCh(ref y, p, modoMarca ? "PONIENDO MARCAS (tocá el mapa)" : "PONER MARCA")) { modoMarca = !modoMarca; if (modoMarca) modoRec = false; }
            GUI.backgroundColor = bg;
            if (estLat.Count > 1 && BotCh(ref y, p, "Borrar estela")) { estLat.Clear(); estLon.Clear(); }
            if (marcaSel >= 0 && marcaSel < chMarcasL.Count)
            {
                var m = chMarcasL[marcaSel]; y += 4f; GUI.Label(new Rect(p.x + 8f, y, p.width - 16f, 18f), "Marca: " + m.nombre); y += 20f;
                if (BotCh(ref y, p, "Agregar al recorrido")) { chRuta.Add(new Wp { lat = m.lat, lon = m.lon, nombre = m.nombre }); GuardarCh(); }
                if (BotCh(ref y, p, "Centrar mapa aca")) { centroLat = m.lat; centroLon = m.lon; sigueAvion = false; tPanMapa = Time.unscaledTime + 8f; }
                if (BotCh(ref y, p, "Borrar marca")) { chMarcasL.RemoveAt(marcaSel); marcaSel = -1; GuardarCh(); }
            }
            p.height = y - p.y + 4f; rectPanelCh = new Rect(rectPanelCh.x, rectPanelCh.y, p.width, p.yMax - rectPanelCh.y);
            if (ch0) GuardarCh();
            if (e.type == EventType.MouseDown && p.Contains(e.mousePosition)) e.Use();
        }
        // clics sobre el mapa: marcar, armar recorrido
        bool libre = r.Contains(e.mousePosition) && !rectPanelCh.Contains(e.mousePosition) && !(planVista && aeroSel != "" && rectOpcionesAero.Contains(e.mousePosition)) && e.mousePosition.y > r.y + 16f && e.mousePosition.y < r.yMax - 28f && !(e.mousePosition.y < r.y + 34f && e.mousePosition.x > r.xMax - 110f);
        if (e.type == EventType.MouseDown && e.button == 0 && libre) { chDown = e.mousePosition; chDownOk = true; }
        else if (e.type == EventType.MouseUp && e.button == 0 && chDownOk)
        {
            chDownOk = false;
            if (libre && Vector2.Distance(chDown, e.mousePosition) < 6f)
            {
                double px = izq + (e.mousePosition.x - r.x), py = arr + (e.mousePosition.y - r.y); double clon = PxALon(px, zoomMapa), clat = PxALat(py, zoomMapa);
                int hit = -1; if (chMarcas) for (int i = 0; i < chMarcasL.Count; i++) { var q = PxCh(chMarcasL[i].lat, chMarcasL[i].lon, izq, arr) + r.position; if (Vector2.Distance(q, e.mousePosition) < 13f) { hit = i; break; } }
                if (hit >= 0) { marcaSel = hit; chAbierto = true; }
                else if (modoRec)
                {
                    string nom = ""; string pb = (planVista || capaAero) ? PuntoBajo(e.mousePosition, r, izq, arr) : ""; var pt = pb != "" ? PuntoPorNombre(pb) : null;
                    if (pt != null) { clat = pt.lat; clon = pt.lon; nom = pt.nombre; }
                    chRuta.Add(new Wp { lat = clat, lon = clon, nombre = nom }); GuardarCh(); textoAviso = "Parada " + chRuta.Count + (nom != "" ? ": " + nom : ""); avisoHasta = Time.time + 2f;
                }
                else if (modoMarca)
                {
                    marcaN++; chMarcasL.Add(new Wp { lat = clat, lon = clon, nombre = "Marca " + marcaN }); marcaSel = chMarcasL.Count - 1; GuardarCh(); chAbierto = true;
                }
                else marcaSel = -1;
            }
        }
    }
}
