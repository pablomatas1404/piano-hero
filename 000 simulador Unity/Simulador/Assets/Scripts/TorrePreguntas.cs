// TORRE: flechitas de altura (rojo = corregi, verde = bien), fila de combustible, preguntas con cuadraditos y calificacion del aterrizaje.
using UnityEngine;
using System.Globalization;

public partial class Torre
{
    // ================================================================== flechitas de altura
    // 0 = sin guia (no estas aproximando), 1 = subi (estas bajo), -1 = baja (estas alto), 2 = altura correcta
    public static int EstadoAltura
    {
        get
        {
            if (inst == null || !inst.aprox) return 0;
            if (inst.aproxAgl > inst.aproxHi) return -1;
            if (inst.aproxAgl < inst.aproxLo) return 1;
            return 2;
        }
    }

    static void Triangulo(float x, float y, float w, float h, bool arriba, Color c)
    {
        int n = Mathf.Max(4, Mathf.RoundToInt(h)); float fh = h / n;
        for (int i = 0; i < n; i++)
        {
            float f = (i + 0.5f) / n; float ww = arriba ? w * f : w * (1f - f);
            Rc(x + (w - ww) * 0.5f, y + i * fh, ww, fh + 0.6f, c);
        }
    }

    // dos triangulitos (arriba / abajo). Rojo el que hay que seguir; los dos verdes cuando la altura esta bien; grises si no hay guia
    public static void FlechasAltura(float x, float y, float w, float h)
    {
        int e = EstadoAltura;
        Color rojo = new Color(1f, 0.2f, 0.15f), verde = new Color(0.25f, 1f, 0.4f), gris = new Color(0.45f, 0.5f, 0.55f, 0.55f);
        Color cu = e == 1 ? rojo : (e == 2 ? verde : gris), cd = e == -1 ? rojo : (e == 2 ? verde : gris);
        Triangulo(x, y, w, h, true, cu); Triangulo(x, y + h + 3f, w, h, false, cd);
    }

    // ================================================================== combustible: fila de ladrillos + cargar
    void FilaCombustible(float x0, float y, float W, float bh)
    {
        var bgc = GUI.backgroundColor;
        float bw = W - 128f; float lad = (bw - 9f * 2f) / 10f; float lleno = Mathf.Clamp(Combustible.Ladrillos, 0f, 10f);
        GUI.Label(new Rect(x0, y - 2f, bw, 12f), "COMBUSTIBLE " + Mathf.RoundToInt(Combustible.Tanque) + " L   RESERVA " + Mathf.RoundToInt(Combustible.Reserva) + " L", sEtq);
        for (int i = 0; i < 10; i++)
        {
            float fx = x0 + i * (lad + 2f); Rc(fx, y + 11f, lad, bh - 12f, new Color(1f, 1f, 1f, 0.08f));
            float f = Mathf.Clamp01(lleno - i); if (f > 0f) Rc(fx, y + 11f, lad * f, bh - 12f, lleno < 2f ? new Color(1f, 0.15f, 0.1f) : new Color(0.9f, 0.2f, 0.15f));
        }
        bool quieto = avion != null && avion.EnTierra && oVel < 3f;
        GUI.enabled = quieto; GUI.backgroundColor = new Color(1f, 0.6f, 0.2f);
        if (GUI.Button(new Rect(x0 + W - 122f, y, 122f, bh), quieto ? "CARGAR COMBUSTIBLE" : "CARGAR (EN TIERRA)", sBtn)) { string t = Combustible.CargarTanque(); Aviso("COMBUSTIBLE: " + t, 6f); DecirPrimero("carga_ok", "recibido"); }
        GUI.enabled = true; GUI.backgroundColor = bgc;
    }

    bool av20, av5, av0;
    void TickCombustible()
    {
        float l = Combustible.Tanque;
        if (l > 250f) { av20 = false; av5 = false; av0 = false; }
        if (l <= 250f && !av20) { av20 = true; Aviso("TORRE: COMBUSTIBLE BAJO. AGARRÁ UN BIDÓN O ATERRIZÁ PARA CARGAR", 8f); DecirPrimero("resp_comb_bajo", "combustible"); }
        if (l <= 80f && !av5) { av5 = true; Aviso("TORRE: COMBUSTIBLE CASI AGOTADO. ATERRIZÁ YA", 8f); DecirPrimero("resp_comb_critico", "combustible"); }
        if (Combustible.Vacio && !av0) { av0 = true; Aviso("SIN COMBUSTIBLE. EL AVIÓN SOLO PLANEA", 8f); DecirPrimero("resp_comb_vacio", "combustible"); }
    }

    // ================================================================== preguntas a la torre (respuestas con datos reales del juego)
    bool preguntasAbiertas = false;   // arranca plegado ("PREGUNTALE A LA TORRE (mostrar)")
    static readonly string[] PREG = { "¿QUÉ FRECUENCIA?", "¿CUÁNTO FALTA?", "¿QUÉ ALTURA?", "¿MI COMBUSTIBLE?", "¿DÓNDE ESTOY?", "¿QUÉ HAGO AHORA?", "¿QUÉ HAY ADELANTE?", "¿MI PLATA?" };

    float Preguntas(float x0, float y, float W, float k)
    {
        var bgc = GUI.backgroundColor;
        GUI.backgroundColor = new Color(0.6f, 0.85f, 1f);
        if (GUI.Button(new Rect(x0, y, W, 20f), preguntasAbiertas ? "PREGUNTALE A LA TORRE  (ocultar)" : "PREGUNTALE A LA TORRE  (mostrar)", sBtn)) preguntasAbiertas = !preguntasAbiertas;
        y += 24f;
        if (preguntasAbiertas)
        {
            float bw = (W - 12f) / 4f, bh = 24f * Mathf.Min(k, 1.2f);
            for (int i = 0; i < PREG.Length; i++)
            {
                GUI.backgroundColor = new Color(1f, 0.85f, 0.3f);
                if (GUI.Button(new Rect(x0 + (i % 4) * (bw + 4f), y + (i / 4) * (bh + 3f), bw, bh), PREG[i], sBtn2)) Preguntar(i);
            }
            y += 2f * (bh + 3f) + 2f;
        }
        GUI.backgroundColor = bgc; return y;
    }

    void Responder(string texto, string clip, string respaldo = "recibido")
    {
        if (!EnFrecuencia) radioKhz = FreqNecesaria();   // no corta la charla: sintoniza sola
        DecirPrimero(clip, respaldo); Aviso("TORRE: " + texto, 9f);
    }

    void Preguntar(int i)
    {
        string dn; double dla, dlo; bool dest = Carrera.Destino(out dn, out dla, out dlo);
        switch (i)
        {
            case 0: Responder("SINTONIZÁ " + FreqTxt(FreqNecesaria()) + " MHZ. " + (aprox ? "ES LA FRECUENCIA DE " + aproxDest.ToUpper() : "ES LA FRECUENCIA DE ESTA ZONA"), "resp_frecuencia", "copio_claro"); break;
            case 1:
                if (!dest) Responder("NO TENÉS DESTINO. ELEGÍ UNO EN LA PESTAÑA DESTINOS", "resp_sin_destino");
                else
                {
                    double km = Dist(oLat, oLon, dla, dlo) / 1000.0; string t = "FALTAN " + km.ToString("F1", CultureInfo.InvariantCulture) + " KM A " + dn.ToUpper();
                    if (oVel > 20f) t += ". A ESTA VELOCIDAD SON UNOS " + Mathf.Max(1, Mathf.RoundToInt((float)(km * 1000.0 / oVel / 60.0))) + " MIN";
                    Responder(t, "resp_faltan", "recibido");
                }
                break;
            case 2:
                if (aprox) Responder("A " + aproxKm.ToString("F1", CultureInfo.InvariantCulture) + " KM TENÉS QUE ESTAR ENTRE " + Mathf.RoundToInt(aproxLo) + " Y " + Mathf.RoundToInt(aproxHi) + " M SOBRE EL PISO. VOS ESTÁS EN " + Mathf.RoundToInt(aproxAgl) + " M", "resp_altura_aprox", "aprox_estable");
                else Responder("SIN ÓRDENES DE ALTURA. MANTENÉ TU NIVEL, AHORA ESTÁS EN " + Mathf.RoundToInt(oAlt) + " M", "resp_altura_libre", "gen_mantene_alt");
                break;
            case 3: Responder("TENÉS " + Mathf.RoundToInt(Combustible.Tanque) + " L EN EL TANQUE Y " + Mathf.RoundToInt(Combustible.Reserva) + " L DE RESERVA", "resp_combustible", "combustible"); break;
            case 4:
                {
                    string pn; double pk; string t = "VOLÁS A " + Mathf.RoundToInt(oAlt) + " M, RUMBO " + Mathf.RoundToInt(oRumbo).ToString("000");
                    if (Pueblos.ProximoAdelante(oLat, oLon, oRumbo, out pn, out pk)) t += ". ADELANTE: " + pn.ToUpper() + " A " + pk.ToString("F0", CultureInfo.InvariantCulture) + " KM";
                    Responder(t, "resp_posicion", "recibido");
                }
                break;
            case 5: Responder(QueHago(), "resp_que_hago", "recibido"); break;
            case 6:
                if (premio != null) { double km; string h = Hora(oLat, oLon, oRumbo, premio, out km); Responder((premio.tipo == Tipo.Celeste ? "CELESTE" : premio.tipo == Tipo.Combustible ? "BIDÓN DE COMBUSTIBLE" : premio.tipo == Tipo.Rojo ? "ROJO" : "CHANCHA NEGRA") + " A LAS " + h + ", " + km.ToString("F1", CultureInfo.InvariantCulture) + " KM", "resp_adelante", "recibido"); }
                else Responder("NO HAY NADA CERCA. ACTIVÁ BONUS PARA QUE APAREZCAN PREMIOS", "resp_nada", "recibido");
                break;
            case 7: Responder("ACUMULADO DEL VIAJE: $" + subtotal + ". EN TU CUENTA: " + Carrera.TextoDinero, "resp_plata", "cuenta_ok"); break;
        }
    }

    string QueHago()
    {
        bool habil; string acc = AccionContextual(out habil);
        if (acc != null) return habil ? "APRETÁ EL BOTÓN AMARILLO: " + acc : "ESPERÁ: " + acc.ToLowerInvariant();
        if (aprox) return aproxKm > 4f ? "SEGUÍ LA ALTURA DE LAS FLECHITAS Y ALINEATE CON LA PISTA" : "ALINEATE CON LA PISTA Y APOYÁ SUAVE";
        if (Carrera.HayViaje) return "VOLÁ AL DESTINO. TE AVISO CUANDO FALTEN 30 KM";
        if (avion != null && avion.EnTierra) return "ELEGÍ UN DESTINO EN LA PESTAÑA DESTINOS Y PEDÍ PUSHBACK";
        return "ELEGÍ UN DESTINO EN LA PESTAÑA DESTINOS";
    }

    // ================================================================== calificacion del aterrizaje (velocidad vertical al tocar + desvio del eje)
    int toqueVisto;
    void TickToque()
    {
        if (AvionDeCarton.ToqueId == toqueVisto) return;
        toqueVisto = AvionDeCarton.ToqueId; float vv = Mathf.Abs(AvionDeCarton.ToqueVV); float lado; bool enPista = avion != null && Aeropuertos.DesvioPista(avion.transform.position, out lado);
        lado = 0f; if (avion != null) Aeropuertos.DesvioPista(avion.transform.position, out lado);
        string cal, clip, txt;
        if (!enPista || lado > 25f) { cal = "DESVIADO"; clip = "toque_desviado"; txt = "TOQUE DESVIADO: APOYASTE A " + Mathf.RoundToInt(lado) + " M DEL EJE DE LA PISTA"; }
        else if (vv <= 2f) { cal = "SUAVE"; clip = "toque_suave"; txt = "TOQUE SUAVE. EXCELENTE. " + vv.ToString("F1", CultureInfo.InvariantCulture) + " M/S"; }
        else if (vv <= 4f) { cal = "MEDIO"; clip = "toque_medio"; txt = "TOQUE MEDIO. ACEPTABLE. " + vv.ToString("F1", CultureInfo.InvariantCulture) + " M/S"; }
        else { cal = "DURO"; clip = "toque_duro"; txt = "TOQUE DURO. " + vv.ToString("F1", CultureInfo.InvariantCulture) + " M/S. SUAVIZÁ EL DESCENSO"; }
        Aviso("TORRE: " + txt, 9f); DecirPrimero(clip, "aprox_toque");
    }
}
