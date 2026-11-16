// TORRE INTERACTIVA, parte 2: estado del avion (flaps, tren, luces, piloto automatico) con botones y cuadraditos de estado,
// salida (rodaje, cabecera, permiso de despegue), aproximacion guiada (pedir pista, ILS, rangos de altura), charla de fondo con respuestas por botones.
using UnityEngine;
using System.Collections.Generic;
using System.Globalization;

public partial class Torre
{
    // ================================================================== piloto automatico (altura / velocidad / rumbo)
    public static bool paAlt, paVel, paRum; static float paAltObj, paVelObj, paRumObj;
    public static bool AutoPiloto { get { return paAlt || paVel || paRum; } }

    // se llama desde AvionDeCarton con los ejes ya leidos: si el eje esta en automatico, se ignora el joystick y se pone lo que hace falta
    public static void PilotoEje(ref float cab, ref float ala, float dt, bool suelo, float cabMax, float bancoMax, float giro)
    {
        if (inst == null || suelo || (!paAlt && !paRum)) return;
        if (paAlt)
        {
            float err = paAltObj - inst.oAlt, vz = Mathf.Clamp(err * 0.12f, -22f, 22f), v = Mathf.Max(60f, inst.oVel);
            float p = Mathf.Asin(Mathf.Clamp(vz / v, -0.6f, 0.6f)) * Mathf.Rad2Deg, c = p / Mathf.Max(1f, cabMax);
            cab = Mathf.Abs(c) < 0.004f ? 0.004f * (err >= 0f ? 1f : -1f) : Mathf.Clamp(c, -1f, 1f);   // un minimo para que tambien se nivele
        }
        if (paRum)
        {
            float rel = Mathf.DeltaAngle(inst.oRumbo, paRumObj), tasa = Mathf.Clamp(rel * 0.4f, -5f, 5f);
            float a = tasa / Mathf.Max(0.05f, giro) / Mathf.Max(1f, bancoMax);
            ala = Mathf.Abs(a) < 0.004f ? 0.004f * (rel >= 0f ? 1f : -1f) : Mathf.Clamp(a, -1f, 1f);
        }
    }
    public static float PilotoVel(float v, bool suelo) { return inst != null && paVel && !suelo ? paVelObj : v; }

    void PaToggle(int cual)
    {
        if (cual == 0) { paAlt = !paAlt; if (paAlt) paAltObj = Mathf.Round(oAlt / 10f) * 10f; }
        else if (cual == 1) { paVel = !paVel; if (paVel) paVelObj = oVel; }
        else { paRum = !paRum; if (paRum) paRumObj = Mathf.Round(oRumbo); }
        Decir(AutoPiloto ? "pa_on" : "pa_off"); Aviso(AutoPiloto ? "PILOTO AUTOMÁTICO: " + (paAlt ? "ALTURA " : "") + (paVel ? "VELOCIDAD " : "") + (paRum ? "RUMBO" : "") : "PILOTO AUTOMÁTICO APAGADO", 4f);
    }

    // ================================================================== flaps (estado y aviso; la fisica queda para despues)
    // al empezar un vuelo nuevo: todo apagado (flaps 0, luz de aterrizaje apagada, piloto automatico apagado); el tren queda arriba si se sale desde el aire
    public static void ReiniciarCabina() { ReiniciarCabina(true); }
    public static void ReiniciarCabina(bool enAire)
    {
        if (inst == null) return;
        inst.flapsIdx = 0; paAlt = false; paVel = false; paRum = false;
        LucesAvion.Aterr = false;
        if (enAire) TrenAterrizaje.PonerSinAviso(false);
    }
    int flapsIdx; static readonly string[] FLAPS_TXT = { "0", "10", "20", "30", "FULL" };
    void FlapsMover(int d)
    {
        int n = Mathf.Clamp(flapsIdx + d, 0, 4); if (n == flapsIdx) return;
        flapsIdx = n; TrenAterrizaje.Zumbido(); Decir("flaps_ok"); Aviso("FLAPS " + FLAPS_TXT[n], 3f);
    }

    // condicion actual de una orden de cabina (para confirmarla sola cuando el piloto aprieta el boton)
    bool CondManual(string clip, out bool conocida)
    {
        conocida = true;
        switch (clip)
        {
            case "landing_on": return LucesAvion.Aterr;
            case "landing_off": return !LucesAvion.Aterr;
            case "strobe_on": case "luces_nav_on": return LucesAvion.Nav;
            case "strobe_off": case "luces_nav_off": return !LucesAvion.Nav;
            case "piloto_on": return AutoPiloto;
            case "piloto_off": return !AutoPiloto;
            case "tren_ext": return TrenAterrizaje.EsFijo || TrenAterrizaje.EsAbajo;
            case "tren_ret": return TrenAterrizaje.EsFijo || !TrenAterrizaje.EsAbajo;
            case "flaps_ext": return flapsIdx >= 2;
            case "flaps_ret": return flapsIdx == 0;
            case "flaps_despegue": return flapsIdx == 2;
            case "flaps_full": return flapsIdx == 4;
        }
        conocida = false; return false;
    }

    static string TituloManual(string clip)
    {
        switch (clip)
        {
            case "landing_on": return "ENCENDÉ LUCES DE ATERRIZAJE"; case "landing_off": return "APAGÁ LUCES DE ATERRIZAJE";
            case "strobe_on": return "ENCENDÉ LAS ESTROBOSCÓPICAS"; case "luces_nav_on": return "ENCENDÉ LUCES DE NAVEGACIÓN";
            case "piloto_on": return "ACTIVÁ EL PILOTO AUTOMÁTICO"; case "piloto_off": return "APAGÁ EL PILOTO AUTOMÁTICO";
            case "tren_ext": return "EXTENDÉ EL TREN DE ATERRIZAJE"; case "tren_ret": return "RECOGÉ EL TREN DE ATERRIZAJE";
            case "flaps_ext": return "EXTENDÉ LOS FLAPS"; case "flaps_ret": return "RETRAÉ LOS FLAPS";
            case "flaps_despegue": return "FLAPS EN POSICIÓN DE DESPEGUE (20)"; case "flaps_full": return "FLAPS COMPLETOS PARA ATERRIZAR";
            case "cinturones": return "ABROCHATE EL CINTURÓN"; case "combustible": return "VERIFICÁ EL COMBUSTIBLE";
            case "checklist": return "COMPLETÁ LA LISTA DE VERIFICACIÓN"; case "transponder": return "AJUSTÁ EL TRANSPONDEDOR";
        }
        return clip.ToUpper();
    }

    // una orden de cabina al azar que todavia no este cumplida
    string ManualAlAzar()
    {
        var c = new List<string> { "cinturones", "combustible", "checklist", "transponder" };
        if (!LucesAvion.Aterr) c.Add("landing_on"); if (!AutoPiloto) c.Add("piloto_on");
        return c[UnityEngine.Random.Range(0, c.Count)];
    }

    // ================================================================== nombre del piloto y eco en pantalla
    string nombrePiloto = ""; bool nombreCargado;
    string NombrePiloto() { if (!nombreCargado) { nombreCargado = true; nombrePiloto = PlayerPrefs.GetString("torre_nombre", ""); } return nombrePiloto; }
    void EcoRespuesta(string pedido, string txt)
    {
        string t = (txt ?? "").Trim().ToUpper();
        switch (pedido)
        {
            case "pide_nombre": nombrePiloto = t; PlayerPrefs.SetString("torre_nombre", t); Aviso("TORRE: COPIADO, " + t + ". BIENVENIDO A BORDO", 6f); break;
            case "pide_matricula": Aviso("TORRE: MATRÍCULA " + t + " REGISTRADA", 6f); break;
            case "pide_destino": Aviso("TORRE: DESTINO " + t + " CONFIRMADO", 6f); break;
            case "pide_locacion": Aviso("TORRE: TE UBICO SOBRE " + t + ". GRACIAS" + (NombrePiloto() != "" ? ", " + NombrePiloto() : ""), 6f); break;
            case "pide_tipo_avion": Aviso("TORRE: " + t + ", TOMADO", 6f); break;
            case "pide_hora": Aviso("TORRE: HORA " + t + " REGISTRADA", 6f); break;
            case "pide_clima": Aviso("TORRE: CLIMA \"" + t + "\" ANOTADO", 6f); break;
            case "pide_combustible": Aviso("TORRE: COMBUSTIBLE " + t + " ANOTADO", 6f); break;
            default: Aviso("TORRE: " + t + " RECIBIDO" + (NombrePiloto() != "" ? ", " + NombrePiloto() : ""), 6f); break;
        }
    }

    // ================================================================== radio: frecuencia que hace falta segun la zona o el aeropuerto de destino
    static int FreqAero(string nombre) { int h = 17; foreach (char c in nombre ?? "") h = h * 31 + c; return 118000 + (((h % 380) + 380) % 380) * 50; }
    int FreqNecesaria() { return aprox ? FreqAero(aproxDest) : ZonaFreq(oLat, oLon); }
    bool EnFrecuencia { get { return radioKhz == FreqNecesaria(); } }

    // ================================================================== consultas con botones (la torre pregunta, el piloto contesta con botones)
    class Consulta { public string texto; public string[] ops; public System.Action<int> cb; public float vence; }
    Consulta consulta; float proxHabla = 60f;
    void Consultar(string texto, string[] ops, System.Action<int> cb, float seg)
    {
        consulta = new Consulta { texto = texto, ops = ops, cb = cb, vence = Time.unscaledTime + seg };
    }
    void Responder(int i)
    {
        if (consulta == null) return; var c = consulta; consulta = null; c.cb(i);
        subtotal += 2; proxHabla = Mathf.Max(proxHabla, Time.unscaledTime + 25f);
    }

    // ================================================================== salida: rodaje, cabecera y permiso de despegue
    int faseSalida;   // 0 parado, 6 pushback autorizado, 1 rodando al punto de espera, 7 espera pista, 2 en pista, 3 espera respuesta, 4 autorizado, 5 en vuelo
    float arranqueEn = -1f, entradaEn = -1f; bool avisoLlegada;
    Vector3 cabTarget; bool hayCab; float despRespondeEn = -1f, despLibreEn = -1f, postDespegue = -1f; int postStage;
    string origenNombre = ""; float distCab; bool avisoFrenar;

    bool BuscarOrigen()
    {
        try
        {
            var l = Aeropuertos.ListaDestinos(); if (l == null || l.Count == 0) return false;
            Aeropuertos.DestinoInfo mejor = null; foreach (var d in l) if (mejor == null || d.km < mejor.km) mejor = d;
            if (mejor == null || mejor.km > 8.0) return false; origenNombre = mejor.nombre; return true;
        }
        catch { return false; }
    }
    void ElegirCabecera()
    {
        hayCab = false; var cabs = new List<Vector3>(); Aeropuertos.Cabeceras(origenNombre, cabs); if (cabs.Count == 0 || avion == null) return;
        Vector3 p = avion.transform.position, f = avion.transform.forward; float mejorD = 1e9f;
        foreach (var c in cabs) { float dd0 = (c - p).magnitude; if (dd0 < 200f) { cabTarget = c; hayCab = true; return; } }   // ya estas en una cabecera
        foreach (var c in cabs) { Vector3 d = c - p; float dist = d.magnitude; bool delante = Vector3.Dot(d.normalized, f) > 0.2f; if (delante && dist > 150f && dist < mejorD) { mejorD = dist; cabTarget = c; hayCab = true; } }
        if (!hayCab) foreach (var c in cabs) { float dist = (c - p).magnitude; if (dist < mejorD) { mejorD = dist; cabTarget = c; hayCab = true; } }
    }

    void ReiniciarSalida() { arranqueEn = entradaEn = -1f; avisoLlegada = false; faseSalida = 0; despRespondeEn = despLibreEn = postDespegue = -1f; hayCab = false; avisoFrenar = false; }

    void UpdateSalida(float ahora, bool enTierra, float vel)
    {
        if (avion == null) return;
        if (!enTierra) // en el aire
        {
            if (faseSalida > 0 && faseSalida < 5) { faseSalida = 5; Decir("despegue_ascenso"); postDespegue = ahora + 12f; postStage = 0; Aviso("TORRE: DESPEGUE EXITOSO. SEGUÍ EL ASCENSO", 6f); }
            if (faseSalida == 5 && postDespegue > 0f && ahora >= postDespegue && orden == null)
            {
                if (postStage == 0) { postStage = 1; postDespegue = ahora + 25f; if (!TrenAterrizaje.EsFijo && TrenAterrizaje.EsAbajo) { NuevaOrden("m:tren_ret", 0); return; } }
                if (postStage == 1) { postStage = 2; postDespegue = -1f; if (flapsIdx > 0) NuevaOrden("m:flaps_ret", 0); }
            }
            return;
        }
        if (faseSalida == 5 && vel < 3f) faseSalida = 0;    // aterrizo y paro: puede volver a pedir salida
        if (faseSalida == 0 && vel < 3f && !Carrera.HayViaje) { }
        if (faseSalida == 1)
        {
            distCab = hayCab ? Vector3.Distance(avion.transform.position, cabTarget) : 0f;
            if (hayCab && distCab < 300f && vel > 8f && !avisoFrenar) { avisoFrenar = true; Decir("freno_mano"); Aviso("TORRE: FRENÁ. TE ESTÁS ACERCANDO A LA CABECERA", 5f); }
            if (!avisoLlegada && hayCab && distCab < 160f && vel < 2.5f) { avisoLlegada = true; Decir("taxi_detengase"); Aviso("TORRE: LLEGASTE AL PUNTO DE ESPERA. REPORTÁ TU POSICIÓN CON EL BOTÓN", 9f); }
        }
        if (arranqueEn > 0f && ahora >= arranqueEn) { arranqueEn = -1f; if (faseSalida == 6) Decir("arranque_motores"); }
        if (faseSalida == 7 && entradaEn > 0f && ahora >= entradaEn) { entradaEn = -1f; faseSalida = 2; DecirA("despegue_posicion"); Aviso("TORRE: PISTA LIBRE. INGRESÁ A LA PISTA, ALINEATE Y PEDÍ PERMISO PARA DESPEGAR", 9f); }
        if (faseSalida == 3 && despRespondeEn > 0f && ahora >= despRespondeEn)
        {
            despRespondeEn = -1f;
            if (UnityEngine.Random.value < 0.7f) { faseSalida = 4; DecirA("despegue_ok"); Aviso("TORRE: AUTORIZADO A DESPEGAR. ACELERÁ Y DESPEGÁ", 8f); }
            else { DecirA("despegue_espera_trafico"); Aviso("TORRE: MANTENÉ POSICIÓN. HAY TRÁFICO ATERRIZANDO", 8f); despLibreEn = ahora + 10f; }
        }
        if (faseSalida == 3 && despLibreEn > 0f && ahora >= despLibreEn) { despLibreEn = -1f; faseSalida = 4; DecirA("despegue_pista_libre"); Aviso("TORRE: PISTA LIBRE. AUTORIZADO A DESPEGAR", 8f); }
    }

    // ================================================================== aproximacion guiada al destino
    bool aprox; string aproxDest = ""; float aproxKm, aproxLo, aproxHi, aproxAgl; bool s30, s15, s10, s7, s5, s4, sIls; float proxPaso;
    static readonly float[] ALT_KM = { 25f, 20f, 15f, 12f, 10f, 8f, 6f }; bool[] sAlt = new bool[7];
    bool pistaPedida, pistaLibre; int negativas; float pistaRespondeEn = -1f, pistaReintento = -1f, proxAvisoAltura, proxRecAlt; int ultMarcaAlt = -1; bool estabaFuera;
    public static bool Aproximando { get { return inst != null && inst.aprox; } }

    void ReiniciarAproximacion()
    {
        aprox = false; s30 = s15 = s10 = s7 = s5 = s4 = false; for (int ai = 0; ai < sAlt.Length; ai++) sAlt[ai] = false; pistaPedida = pistaLibre = false; sIls = false; proxPaso = 0f; negativas = 0; pistaRespondeEn = pistaReintento = -1f; proxAvisoAltura = 0f; proxRecAlt = 0f; ultMarcaAlt = -1; estabaFuera = false; ReiniciarSalida();
    }

    static void RangoAltura(float km, out float lo, out float hi)
    {
        if (km >= 25f) { lo = 1000f; hi = 1500f; } else if (km >= 15f) { lo = 800f; hi = 1200f; } else if (km >= 10f) { lo = 550f; hi = 900f; }
        else if (km >= 7f) { lo = 380f; hi = 650f; } else if (km >= 5f) { lo = 250f; hi = 450f; } else if (km >= 3f) { lo = 140f; hi = 300f; } else { lo = 50f; hi = 180f; }
    }

    void UpdateAproximacion(float ahora, double lat, double lon, bool volando)
    {
        string dn; double dla, dlo;
        if (!(Carrera.Destino(out dn, out dla, out dlo) && volando)) { if (!Carrera.HayViaje && aprox) ReiniciarAproximacion(); aprox = aprox && Carrera.HayViaje && volando; return; }
        double d = Dist(lat, lon, dla, dlo); aproxKm = (float)(d / 1000.0); aproxDest = dn;
        aproxAgl = avion != null ? avion.AlturaSobrePiso : 0f; RangoAltura(aproxKm, out aproxLo, out aproxHi);
        if (d > 35000.0) { aprox = false; return; }
        if (!aprox) aprox = true;
        // las indicaciones van de a UNA, separadas por 14 s: 35 km radio, 30 km ILS, 20 km luces, 14 km flaps, 9 km tren, 6,5 km flaps completos
        if (!s30 && ahora >= proxPaso)
        {
            s30 = true; proxPaso = ahora + 14f; DecirA("aprox_30", 0f); Aviso("TORRE: FALTAN " + Mathf.RoundToInt(aproxKm) + " KM A " + dn.ToUpper() + ". CAMBIÁ A LA FRECUENCIA DEL AEROPUERTO", 8f);
            orden = null; NuevaOrden("freq", FreqAero(dn));
        }
        if (s30 && !sIls && d < 30000.0 && ahora >= proxPaso)
        {
            sIls = true; proxPaso = ahora + 14f;
            if (!Aeropuertos.IlsActivo(dn)) DecirA("ils_pregunta", 0f);
            if (!Aeropuertos.IlsActivo(dn)) Consultar("¿QUERÉS ACTIVAR EL ILS PARA LA APROXIMACIÓN?", new[] { "SÍ, ACTIVAR ILS", "NO, GRACIAS" }, k =>
            {
                if (k == 0) { Aeropuertos.IlsDe(dn, true); Decir("ils_on"); Aviso("TORRE: ILS ACTIVADO. SEGUÍ LOS AROS", 6f); } else { Decir("recibido"); Aviso("TORRE: ENTENDIDO, SIN ILS", 4f); }
            }, 120f);
        }
        // preparacion del avion, bien espaciada (que no lleguen todas las ordenes juntas mientras te alineas con la pista)
        if (d < 20000.0 && !s15 && sIls && LucesAvion.Aterr) s15 = true;
        if (d < 20000.0 && !s15 && sIls && ahora >= proxPaso) { s15 = true; proxPaso = ahora + 14f; Decir("landing_on"); Aviso("TORRE: FALTAN 20 KM. ENCENDÉ LAS LUCES DE ATERRIZAJE", 8f); }
        if (d < 14000.0 && !s10 && s15 && flapsIdx >= 1) s10 = true;
        if (d < 14000.0 && !s10 && s15 && ahora >= proxPaso) { s10 = true; proxPaso = ahora + 14f; Decir("flaps_ext"); Aviso("TORRE: FALTAN 14 KM. EXTENDÉ LOS PRIMEROS FLAPS (BOTÓN FLAPS +)", 8f); }
        if (d < 9000.0 && !s7 && s10 && ahora >= proxPaso) { s7 = true; proxPaso = ahora + 14f; if (!TrenAterrizaje.EsFijo) Decir("tren_ext"); Aviso(TrenAterrizaje.EsFijo ? "TORRE: FALTAN 9 KM. SEGUÍ ALINEÁNDOTE CON LA PISTA" : "TORRE: FALTAN 9 KM. BAJÁ EL TREN DE ATERRIZAJE", 8f); }
        if (d < 6500.0 && !s5 && s7 && flapsIdx == 4) s5 = true;
        if (d < 6500.0 && !s5 && s7 && ahora >= proxPaso) { s5 = true; proxPaso = ahora + 14f; Decir("flaps_full"); Aviso("TORRE: FALTAN 6,5 KM. FLAPS COMPLETOS. DESPUÉS SOLO ALINEATE", 8f); }
        // altura: recordatorio en cada marca de 2 km y cada 22 s mientras sigas fuera de rango; confirmacion al entrar ("ponete en la altura que figura en pantalla")
        int marca = Mathf.FloorToInt((30000f - (float)d) / 2000f); bool nuevaMarca = marca > ultMarcaAlt; if (nuevaMarca) ultMarcaAlt = marca;
        bool fueraAlt = aproxAgl > aproxHi || aproxAgl < aproxLo;
        if (d > 3500.0 && d <= 30000.0 && sIls)
        {
            if (fueraAlt && orden == null && ahora >= proxAvisoAltura && ahora >= proxPaso && (nuevaMarca || ahora >= proxRecAlt) && aproxAgl > 5f)
            {
                bool alto = aproxAgl > aproxHi;
                DecirPrimero("aprox_pone_alt", alto ? "aprox_alto" : "aprox_bajo", alto ? "gen_baja_alt" : "gen_sube_alt");
                Aviso("TORRE: FALTAN " + Mathf.RoundToInt(aproxKm) + " KM. " + (alto ? "BAJÁ" : "SUBÍ") + " A LA ALTURA QUE FIGURA EN PANTALLA: " + Mathf.RoundToInt(aproxLo) + " A " + Mathf.RoundToInt(aproxHi) + " M SOBRE EL PISO. VOS: " + Mathf.RoundToInt(aproxAgl) + " M", 9f);
                proxAvisoAltura = ahora + 12f; proxRecAlt = ahora + 22f; estabaFuera = true;
            }
            else if (!fueraAlt && estabaFuera) { estabaFuera = false; DecirPrimero("aprox_en_rango", "aprox_estable"); Aviso("TORRE: ALTURA CORRECTA PARA ESTA DISTANCIA. MANTENELA", 6f); proxAvisoAltura = ahora + 8f; }
        }
        if (pistaRespondeEn > 0f && ahora >= pistaRespondeEn)
        {
            pistaRespondeEn = -1f;
            if (negativas >= 1 || UnityEngine.Random.value < 0.65f)
            {
                pistaLibre = true; DecirA(UnityEngine.Random.value < 0.5f ? "aprox_ok" : "aprox_circuito"); Aviso("TORRE: PISTA LIBRE. AUTORIZADO A ATERRIZAR EN " + dn.ToUpper(), 8f);
            }
            else
            {
                negativas++; pistaPedida = false; pistaReintento = ahora + 40f; float q = UnityEngine.Random.value;
                if (q < 0.4f) { DecirA("aprox_ocupada"); Aviso("TORRE: PISTA OCUPADA. SOBREVOLÁ LA ZONA Y VOLVÉ A PEDIR EN 40 S", 9f); }
                else if (q < 0.75f) { DecirA("aprox_orbita"); Aviso("TORRE: ORBITÁ A LA IZQUIERDA Y VOLVÉ A PEDIR EN 40 S", 9f); }
                else { DecirA("aprox_numero2"); Aviso("TORRE: SOS EL NÚMERO DOS. MANTENÉ SEPARACIÓN Y VOLVÉ A PEDIR EN 40 S", 9f); }
            }
        }
    }

    // el boton grande de la situacion (cambia segun donde estes): null = ninguno
    string AccionContextual(out bool habilitada)
    {
        habilitada = true;
        if (avion == null) return null;
        bool tierra = avion.EnTierra;
        if (tierra && (faseSalida == 0 || faseSalida == 6) && oVel < 15f && EnCabeceraRapido()) return "PEDIR PERMISO DE DESPEGUE";   // ya estas en la cabecera: no hace falta rodar
        if (tierra && faseSalida == 0 && oVel < 3f && BuscarOrigenRapido()) return "PEDIR PUSHBACK";
        if (tierra && faseSalida == 6) return "PEDIR RODAJE";
        if (faseSalida == 1) return "REPORTAR: EN EL PUNTO DE ESPERA";
        if (faseSalida == 7) { habilitada = false; return "ESPERANDO AUTORIZACIÓN PARA INGRESAR A LA PISTA..."; }
        if (faseSalida == 2) return "PEDIR PERMISO DE DESPEGUE";
        if (aprox && !pistaLibre && aproxKm <= 30.5f)
        {
            if (pistaPedida) { habilitada = false; return "ESPERANDO RESPUESTA DE LA TORRE..."; }
            if (Time.unscaledTime < pistaReintento) { habilitada = false; return "VOLVER A PEDIR PISTA EN " + Mathf.CeilToInt(pistaReintento - Time.unscaledTime) + " S"; }
            return "SOLICITAR PISTA PARA ATERRIZAR";
        }
        return null;
    }
    float proxCab; bool enCab;
    bool EnCabeceraRapido()
    {
        if (avion == null) return false;
        if (Time.unscaledTime < proxCab) return enCab; proxCab = Time.unscaledTime + 1f; enCab = false;
        if (!BuscarOrigenRapido()) return false;
        var cabs = new List<Vector3>(); Aeropuertos.Cabeceras(origenNombre, cabs); Vector3 p = avion.transform.position;
        foreach (var c in cabs) if ((c - p).magnitude < 200f) { enCab = true; break; }
        return enCab;
    }
    string AccionSecundaria() { return avion != null && avion.EnTierra && faseSalida == 0 && oVel < 3f && !EnCabeceraRapido() && BuscarOrigenRapido() ? "PEDIR RODAJE (SIN PUSHBACK)" : null; }
    float proxChequeoOrigen; bool origenOk;
    bool BuscarOrigenRapido() { if (Time.unscaledTime >= proxChequeoOrigen) { proxChequeoOrigen = Time.unscaledTime + 3f; origenOk = BuscarOrigen(); } return origenOk; }

    void PresionarAccion(string a)
    {
        if (a == "PEDIR PUSHBACK") { BuscarOrigen(); faseSalida = 6; DecirA("pushback"); arranqueEn = Time.unscaledTime + 7f; Aviso("TORRE: AUTORIZADO A RETROCEDER. ORIENTÁ EL AVIÓN Y PEDÍ RODAJE", 9f); }
        else if (a.StartsWith("REPORTAR"))
        {
            float dc = hayCab && avion != null ? Vector3.Distance(avion.transform.position, cabTarget) : 0f;
            if (hayCab && dc > 450f) { Decir("taxi_calle"); Aviso("TORRE: TODAVÍA NO LLEGASTE. TE FALTAN " + Mathf.RoundToInt(dc) + " M. SEGUÍ POR LA CALLE DE RODAJE", 7f); }
            else if (UnityEngine.Random.value < 0.4f) { faseSalida = 7; entradaEn = Time.unscaledTime + 9f; DecirA("pista_en_uso"); Aviso("TORRE: PISTA EN USO. MANTENÉ POSICIÓN Y ESPERÁ AUTORIZACIÓN", 9f); }
            else { faseSalida = 2; DecirA("despegue_posicion"); Aviso("TORRE: PISTA LIBRE. INGRESÁ A LA PISTA, ALINEATE Y PEDÍ PERMISO PARA DESPEGAR", 9f); }
        }
        else if (a.StartsWith("PEDIR RODAJE"))
        {
            BuscarOrigen(); ElegirCabecera(); faseSalida = 1; avisoFrenar = false; avisoLlegada = false; DecirA("despegue_rodar");
            Aviso("TORRE: AUTORIZADO A RODAR HASTA LA CABECERA DE " + origenNombre.ToUpper(), 8f);
        }
        else if (a == "PEDIR PERMISO DE DESPEGUE") { if (faseSalida == 0 || faseSalida == 6) { Aviso("TORRE: YA ESTÁS EN LA CABECERA. SALTEAMOS EL RODAJE", 5f); } faseSalida = 3; despRespondeEn = Time.unscaledTime + 4f; Decir("espera_momento"); Aviso("TORRE: ESPERÁ, VERIFICANDO LA PISTA", 5f); }
        else if (a.StartsWith("SOLICITAR PISTA")) { pistaPedida = true; pistaRespondeEn = Time.unscaledTime + 5f; DecirA("aprox_recibida"); Aviso("TORRE: SOLICITUD RECIBIDA. ESPERÁ INSTRUCCIONES", 6f); }
    }

    // ================================================================== charla de fondo: la torre habla sola y pide respuestas con botones
    static readonly string[] PIL_RESP = { "", "pil_mantengo", "pil_cambio_frec", "pil_virando", "pil_descendiendo", "pil_trafico_vista", "pil_recibo_claro", "pil_solicita_aterrizar" };
    void CharlaFondo(double lat, double lon)
    {
        float q = R(0f, 100f);
        if (q < 20f)
        {
            string[] t = { "trafico_3", "trafico_9", "trafico_12", "trafico_alto", "trafico_bajo" }; string[] tx = { "A TUS 3 EN PUNTO, 5 KM", "A TUS 9 EN PUNTO, 8 KM", "AL FRENTE, 10 KM, TU MISMO NIVEL", "POR ENCIMA DE TU NIVEL", "POR DEBAJO DE TU NIVEL, EN DESCENSO" };
            int i = UnityEngine.Random.Range(0, t.Length); DecirA(t[i]); Decir("pide_trafico_vista", 0.2f); Aviso("TORRE: TRÁFICO CERCANO " + tx[i], 7f);
            Consultar("TRÁFICO CERCANO: " + tx[i], new[] { "A LA VISTA", "SIN CONTACTO" }, k => { Decir("trafico_ok"); Aviso(k == 0 ? "TORRE: TRÁFICO REPORTADO. GRACIAS" : "TORRE: ENTENDIDO, SEGUIMOS ATENTOS", 5f); }, 50f);
        }
        else if (q < 33f)
        {
            string[] c = { "clima_viento", "clima_visib", "clima_nubes", "clima_turbulencia", "clima_altimetro" }; string[] tx = { "VIENTO MODERADO DEL SUDESTE", "VISIBILIDAD REDUCIDA EN LA ZONA", "NUBOSIDAD DISPERSA A 1500 M", "TURBULENCIA LEVE EN TU RUTA", "AJUSTÁ EL ALTÍMETRO" };
            int i = UnityEngine.Random.Range(0, c.Length); DecirA(c[i]); Decir("pide_clima_ok", 0.3f); Aviso("TORRE: " + tx[i], 7f);
            Consultar("INFORME DEL TIEMPO: " + tx[i], new[] { "ENTERADO" }, k => { Decir("recibido"); }, 50f);
        }
        else if (q < 52f)
        {
            int k = UnityEngine.Random.Range(1, 8); Decir("otro_" + k); Decir(PIL_RESP[k], 1.8f); string[] n = { "", "CÓNDOR 41", "HALCÓN 2", "PAMPERO 7", "CÓNDOR 3", "HALCÓN 9", "PAMPERO 5", "CÓNDOR 8" };
            Aviso("RADIO: LA TORRE HABLA CON " + n[k], 7f);
        }
        else if (q < 63f)
        {
            var cerca = Pueblos.Cercanos(lat, lon, 12.0);
            if (cerca.Count > 0) { DecirA("pueblo_sobre"); Aviso("TORRE: ESTÁS SOBREVOLANDO " + cerca[0].ToUpper(), 8f); Consultar("SOBREVOLÁS " + cerca[0].ToUpper() + ". ¿LO CONFIRMÁS?", new[] { "CONFIRMADO" }, k => { Decir("reporte_ok"); }, 50f); }
            else { DecirA("pide_reporte_pueblo"); Consultar("REPORTÁ CUANDO PASES LA PRÓXIMA LOCALIDAD", new[] { "PASÉ UNA LOCALIDAD" }, k => { Decir("reporte_ok"); Aviso("TORRE: REPORTE RECIBIDO. GRACIAS", 4f); }, 80f); }
        }
        else if (q < 72f)
        {
            DecirA("pide_nivel"); Consultar("INFORMÁ TU NIVEL DE VUELO", new[] { "REPORTAR " + Mathf.RoundToInt(oAlt) + " M" }, k => { Decir("reporte_ok"); Aviso("TORRE: NIVEL " + Mathf.RoundToInt(oAlt) + " M, RECIBIDO", 4f); }, 50f);
        }
        else if (q < 80f)
        {
            DecirA("pide_intenciones"); Consultar("INFORMÁ TUS INTENCIONES", new[] { "SEGUIR AL DESTINO", "NECESITO ESPERAR" }, k => { Decir(k == 0 ? "afirmativo" : "recibido"); Aviso(k == 0 ? "TORRE: AFIRMATIVO. SEGUÍ AL DESTINO" : "TORRE: RECIBIDO. AVISÁ CUANDO ESTÉS LISTO", 5f); }, 50f);
        }
        else if (q < 87f)
        {
            DecirA("pide_asistencia"); Consultar("¿NECESITÁS ASISTENCIA?", new[] { "TODO BIEN", "NECESITO AYUDA" }, k => { Decir(k == 0 ? "afirmativo" : "espera_momento"); Aviso(k == 0 ? "TORRE: AFIRMATIVO. BUEN VUELO" : "TORRE: ESPERÁ UN MOMENTO, ESTAMOS VERIFICANDO", 5f); }, 50f);
        }
        else if (q < 94f)
        {
            DecirA("torre_llama"); Consultar("LA TORRE TE LLAMA", new[] { "RESPONDER" }, k => { Decir("copio_claro"); Aviso("TORRE: TE COPIO FUERTE Y CLARO", 4f); }, 40f);
        }
        else
        {
            string[] c = { "buen_vuelo", "escucha", "frec_ocupada", "recibido" }; Decir(c[UnityEngine.Random.Range(0, c.Length)]);
        }
    }

    // ================================================================== interfaz
    // ---- halo neon parpadeante: marca donde hay que contestar cuando la torre pregunta (3 pulsos al aparecer la pregunta)
    object haloClave; float haloDesde = -99f;
    void HaloClave(object k) { if (k == null) { haloClave = null; return; } if (!ReferenceEquals(k, haloClave)) { haloClave = k; haloDesde = Time.unscaledTime; } }
    void Halo(Rect r)
    {
        float t = Time.unscaledTime - haloDesde; if (t < 0f) return;
        // 3 parpadeos al aparecer la pregunta y despues queda prendido (con un latido suave) hasta que se contesta
        float p = t < 3f ? Mathf.Abs(Mathf.Sin(t * Mathf.PI)) : 0.85f + 0.15f * Mathf.Sin(Time.unscaledTime * 3f);
        for (int i = 0; i < 5; i++)
        {
            float o = 1.5f + i * 2.2f, a = p * (0.95f - i * 0.17f), g = 2.2f; var col = new Color(0.2f, 1f, 0.95f, Mathf.Max(0f, a)); var q = new Rect(r.x - o, r.y - o, r.width + 2f * o, r.height + 2f * o);
            Rc(q.x, q.y, q.width, g, col); Rc(q.x, q.yMax - g, q.width, g, col); Rc(q.x, q.y, g, q.height, col); Rc(q.xMax - g, q.y, g, q.height, col);
        }
    }

    GUIStyle sChip, sBtn2;
    float Chip(float x, float y, string t, Color fondo, Color letra)
    {
        float w = sChip.CalcSize(new GUIContent(t)).x + 10f; Rc(x, y, w, 16f, fondo); sChip.normal.textColor = letra; GUI.Label(new Rect(x, y, w, 16f), t, sChip); return x + w + 5f;
    }

    const bool RADIO_EN_PANEL = false;
    public static void AppRadio(Rect a) { if (inst != null && inst.sEtq != null) inst.RadioCom(a.x + 10f, a.y + 10f, a.width - 20f, GUI.backgroundColor); else GUI.Label(a, "Radio no disponible todavia"); }
    float RadioCom(float x0, float y, float W, Color bgc)
    {
        Color cian = new Color(0.3f, 0.9f, 1f);
                GUI.Label(new Rect(x0, y, 120f, 14f), "RADIO COM  MHZ", sEtq);
        { bool okF = EnFrecuencia; var cz = GUI.color; GUI.color = okF ? new Color(0.5f, 1f, 0.6f) : new Color(1f, 0.6f, 0.2f); GUI.Label(new Rect(x0 + 120f, y, W - 120f, 14f), (okF ? "SINTONIZADO EN LA ZONA: " : "FRECUENCIA QUE NECESITÁS: ") + FreqTxt(FreqNecesaria()) + " MHZ", sEtq); GUI.color = cz; }
        string ft = FreqTxt(radioKhz); Seg7(x0, y + 15f, 28f, ft, cian);
        float rx = x0 + AnchoSeg(ft, 28f) + 14f, ry = y + 16f;
        if (GUI.Button(new Rect(rx, ry, 40f, 26f), "-1", sBtn)) MoverRadio(-1000);
        if (GUI.Button(new Rect(rx + 44f, ry, 44f, 26f), "-.05", sBtn)) MoverRadio(-50);
        if (GUI.Button(new Rect(rx + 92f, ry, 44f, 26f), "+.05", sBtn)) MoverRadio(50);
        if (GUI.Button(new Rect(rx + 140f, ry, 40f, 26f), "+1", sBtn)) MoverRadio(1000);
        y += 48f;
        if (!EnFrecuencia) { GUI.backgroundColor = new Color(1f, 0.6f, 0.2f); if (GUI.Button(new Rect(x0, y, W, 24f), "SINTONIZAR " + FreqTxt(FreqNecesaria()) + " (AUTOMÁTICO)", sBtn)) { radioKhz = FreqNecesaria(); Aviso("RADIO EN " + FreqTxt(radioKhz) + " MHZ", 3f); } GUI.backgroundColor = bgc; y += 30f; }
        return y;
    }

    void OrdenesGUI(Rect r, float x0, float y4, Color ambar, Color verde, Color cian, Color blanco)
    {
        if (sTexto == null) { sTexto = new GUIStyle(GUI.skin.textField) { font = fuente, fontSize = 14, fontStyle = FontStyle.Bold }; sTexto.normal.textColor = Color.white; sTexto.focused.textColor = Color.white; }
        if (sChip == null) { sChip = new GUIStyle(GUI.skin.label) { font = fuente, fontSize = 11, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter }; sBtn2 = new GUIStyle(sBtn) { fontSize = 11 }; }
        var bgc = GUI.backgroundColor; float W = r.width - 20f, y = y4;
        Matricula(); if (elegirMat) { MatGUI(x0, y, W); return; }
        Rc(x0, y - 3f, W, 1f, new Color(0.2f, 0.75f, 0.95f, 0.3f));

        // ---- cuadraditos de estado: aparecen cuando algo esta activado y desaparecen cuando se apaga
        float cx = x0;
        if (flapsIdx > 0) cx = Chip(cx, y, "FLAPS " + FLAPS_TXT[flapsIdx], new Color(1f, 0.82f, 0f), Color.black);
        if (!TrenAterrizaje.EsFijo && TrenAterrizaje.EsAbajo) cx = Chip(cx, y, "TREN ABAJO", new Color(0.82f, 0.88f, 0.95f), Color.black);
        if (LucesAvion.Aterr) cx = Chip(cx, y, "LUZ ATERR.", new Color(1f, 0.95f, 0.6f), Color.black);
        if (LucesAvion.Nav) cx = Chip(cx, y, "LUCES NAV", new Color(0.35f, 0.9f, 0.5f), Color.black);
        if (paAlt) cx = Chip(cx, y, "PA ALT " + Mathf.RoundToInt(paAltObj), new Color(0.4f, 0.85f, 1f), Color.black);
        if (paRum) cx = Chip(cx, y, "PA RUM " + Mathf.RoundToInt(paRumObj).ToString("000"), new Color(0.4f, 0.85f, 1f), Color.black);
        if (paVel) cx = Chip(cx, y, "PA VEL " + Mathf.RoundToInt(paVelObj * 3.6f), new Color(0.4f, 0.85f, 1f), Color.black);
        bool enF = EnFrecuencia; string ftxt = enF ? "EN FRECUENCIA" : "FUERA DE FRECUENCIA"; float fw2 = sChip.CalcSize(new GUIContent(ftxt)).x + 10f;
        Chip(x0 + W - fw2, y, ftxt, enF ? new Color(0.2f, 0.7f, 0.3f) : new Color(0.85f, 0.15f, 0.15f), Color.white);
        y += 22f;

        // ---- controles del avion (botones anchos: 3 por fila; se estiran para aprovechar el alto del panel)
        float k = Mathf.Clamp(r.height / 720f, 1f, 1.6f); float bh = 28f * k, gap = 6f * k; float bw3 = (W - 8f) / 3f; float bx;
        bx = x0; if (GUI.Button(new Rect(bx, y, bw3, bh), "TREN ABAJO", sBtn2)) { if (TrenAterrizaje.EsFijo) Aviso("ESTE AVIÓN TIENE EL TREN FIJO", 3f); else { if (TrenAterrizaje.Poner(true)) Decir("tren_abajo_ok"); } }
        bx += bw3 + 4f; if (GUI.Button(new Rect(bx, y, bw3, bh), "TREN ARRIBA", sBtn2)) { if (TrenAterrizaje.EsFijo) Aviso("ESTE AVIÓN TIENE EL TREN FIJO", 3f); else { if (TrenAterrizaje.Poner(false)) Decir("tren_arriba_ok"); } }
        bx += bw3 + 4f; GUI.backgroundColor = LucesAvion.Aterr ? new Color(1f, 0.95f, 0.5f) : bgc; if (GUI.Button(new Rect(bx, y, bw3, bh), "LUZ ATERRIZAJE", sBtn2)) { LucesAvion.Aterr = !LucesAvion.Aterr; Decir("luces_ok"); }
        GUI.backgroundColor = bgc; y += bh + 4f;
        bx = x0; if (GUI.Button(new Rect(bx, y, bw3, bh), "FLAPS -", sBtn2)) FlapsMover(-1);
        bx += bw3 + 4f; if (GUI.Button(new Rect(bx, y, bw3, bh), "FLAPS +", sBtn2)) FlapsMover(1);
        bx += bw3 + 4f; GUI.backgroundColor = LucesAvion.Nav ? new Color(0.4f, 1f, 0.55f) : bgc; if (GUI.Button(new Rect(bx, y, bw3, bh), "LUCES NAV", sBtn2)) { LucesAvion.Nav = !LucesAvion.Nav; Decir("luces_ok"); }
        GUI.backgroundColor = bgc; y += bh + gap;

        // ---- piloto automatico: tres botones y ajuste fino abajo de cada uno
        var nombres = new[] { "PA ALTURA", "PA VELOCIDAD", "PA RUMBO" }; var on = new[] { paAlt, paVel, paRum };
        for (int i = 0; i < 3; i++)
        {
            float gx = x0 + i * (bw3 + 4f); GUI.backgroundColor = on[i] ? new Color(0.4f, 0.9f, 1f) : bgc;
            if (GUI.Button(new Rect(gx, y, bw3, bh), nombres[i], sBtn2)) PaToggle(i);
            GUI.backgroundColor = bgc;
            if (GUI.Button(new Rect(gx, y + bh + 2f, bw3 / 2f - 1f, 22f * k), "-", sBtn2)) { if (i == 0 && paAlt) paAltObj -= 100f; if (i == 1 && paVel) paVelObj = Mathf.Max(20f, paVelObj - 10f / 3.6f); if (i == 2 && paRum) paRumObj = (paRumObj + 350f) % 360f; }
            if (GUI.Button(new Rect(gx + bw3 / 2f + 1f, y + bh + 2f, bw3 / 2f - 1f, 22f * k), "+", sBtn2)) { if (i == 0 && paAlt) paAltObj += 100f; if (i == 1 && paVel) paVelObj += 10f / 3.6f; if (i == 2 && paRum) paRumObj = (paRumObj + 10f) % 360f; }
        }
        y += bh + 2f + 22f * k + gap;
        FilaCombustible(x0, y, W, 28f); y += 34f;
        Rc(x0, y - 3f, W, 1f, new Color(0.2f, 0.75f, 0.95f, 0.3f));
        // ---- orden de la torre / rodaje / aproximacion
        GUI.Label(new Rect(x0, y, 200f, 14f), aprox ? "APROXIMACIÓN" : (faseSalida == 1 ? "RODAJE" : "ORDEN DE LA TORRE"), sEtq);
        GUI.backgroundColor = new Color(0.6f, 0.85f, 1f); if (GUI.Button(new Rect(x0 + W - 150f - 118f, y - 1f, 114f, 18f), MatTxt(), sBtn)) elegirMat = true;
        GUI.backgroundColor = ordenesOn ? new Color(0.3f, 1f, 0.45f) : new Color(0.75f, 0.75f, 0.75f);
        if (GUI.Button(new Rect(x0 + W - 150f, y - 1f, 150f, 18f), ordenesOn ? "TORRE HABLA: ON" : "TORRE HABLA: OFF", sBtn)) { ordenesOn = !ordenesOn; if (!ordenesOn) { orden = null; consulta = null; vozCola.Clear(); } else { proxOrden = Time.unscaledTime + 8f; DecirA("torre_llama"); } }
        GUI.backgroundColor = bgc; y += 16f;
        if (aprox)
        {
            bool dentro = aproxAgl >= aproxLo && aproxAgl <= aproxHi; Color cc = dentro ? verde : (aproxAgl > aproxHi ? new Color(1f, 0.45f, 0.3f) : cian);
            GUI.Label(new Rect(x0, y, W * 0.56f, 24f), Recortar("FALTAN " + aproxKm.ToString("F1", CultureInfo.InvariantCulture) + " KM A " + aproxDest.ToUpper(), sTit, W * 0.56f - 4f), sTit);
            GUI.Label(new Rect(x0, y + 22f, W * 0.56f, 14f), "SOBRE EL PISO: ENTRE " + Mathf.RoundToInt(aproxLo) + " Y " + Mathf.RoundToInt(aproxHi) + " M", sEtq);
            FlechasAltura(x0 + W - 34f, y - 2f, 28f, 16f);
            { var cz = GUI.color; GUI.color = cc; GUI.Label(new Rect(x0 + W * 0.56f, y, W * 0.44f - 40f, 24f), Mathf.RoundToInt(aproxAgl) + " M", sTit); GUI.Label(new Rect(x0 + W * 0.56f, y + 22f, W * 0.44f - 40f, 14f), dentro ? "TU ALTURA: BIEN" : (aproxAgl > aproxHi ? "TU ALTURA: BAJÁ" : "TU ALTURA: SUBÍ"), sEtq); GUI.color = cz; }
        }
        else if (faseSalida == 1 && hayCab)
        {
            GUI.Label(new Rect(x0, y, W * 0.6f, 24f), "RODÁ A LA CABECERA", sTit); GUI.Label(new Rect(x0, y + 22f, W * 0.6f, 14f), "FRENÁ Y DETENETE AL LLEGAR", sEtq);
            Seg7(x0 + W * 0.62f, y - 2f, 30f, Mathf.RoundToInt(distCab).ToString(), ambar); GUI.Label(new Rect(x0 + W * 0.62f, y + 31f, W * 0.38f, 14f), "METROS HASTA LA CABECERA", sEtq);
        }
        else if (orden != null)
        {
            GUI.Label(new Rect(x0, y, W * 0.62f, 24f), Recortar(orden.titulo, sTit, W * 0.62f - 4f), sTit);
            if (orden.objTxt != "") { Seg7(x0 + W * 0.62f, y - 2f, 30f, orden.objTxt, ambar); GUI.Label(new Rect(x0 + W * 0.62f, y + 31f, W * 0.38f, 14f), orden.unidad + AhoraTxt(orden), sEtq); }
            else GUI.Label(new Rect(x0, y + 22f, W, 14f), orden.texto ? "ESCRIBÍ LA RESPUESTA ABAJO Y APRETÁ ENVIAR" : (orden.tipo == "manual" ? "USÁ LOS BOTONES DE ARRIBA" : "HACELO Y CONFIRMÁ CON OK"), sEtq);
        }
        else GUI.Label(new Rect(x0, y + 4f, W, 24f), ordenesOn ? "SIN ÓRDENES. LA TORRE TE VA A LLAMAR" : "ACTIVÁ LA TORRE PARA RECIBIR ÓRDENES", sMsg);
        y += 50f * Mathf.Min(k, 1.25f);
        bool pideOk = orden != null && !orden.texto && orden.tipo != "manual" && orden.objTxt == "";
        bool habil; string acc = AccionContextual(out habil);
        float ox = x0;
        if (acc != null)
        {
            GUI.enabled = habil; GUI.backgroundColor = new Color(1f, 0.85f, 0.2f);
            string sec = AccionSecundaria(); float ah = 30f * Mathf.Min(k, 1.2f);
            if (sec == null) { if (GUI.Button(new Rect(x0, y, W, ah), acc, sBtn) && habil) PresionarAccion(acc); }
            else { if (GUI.Button(new Rect(x0, y, W * 0.5f - 2f, ah), acc, sBtn) && habil) PresionarAccion(acc); if (GUI.Button(new Rect(x0 + W * 0.5f + 2f, y, W * 0.5f - 2f, ah), sec, sBtn) && habil) PresionarAccion(sec); }
            GUI.backgroundColor = bgc; GUI.enabled = true; y += ah + 4f;
        }
        else
        {
            GUI.backgroundColor = orden != null && orden.tipo != "manual" ? new Color(0.3f, 1f, 0.45f) : new Color(0.5f, 0.5f, 0.5f);
            if (pideOk) Halo(new Rect(ox, y, 118f, 24f));
            if (GUI.Button(new Rect(ox, y, 118f, 24f), "OK, LISTO", sBtn) && orden != null) Confirmar();
            GUI.backgroundColor = bgc;
            if (GUI.Button(new Rect(ox + 126f, y, 110f, 24f), "REPETIR", sBtn)) { if (orden != null) DecirA(orden.clip); else if (ultimaVoz != "") Decir(ultimaVoz); }
            if (GUI.Button(new Rect(ox + 244f, y, 150f, 24f), "LLAMAR A TORRE", sBtn)) LlamarTorre();
            y += 30f;
        }

        HaloClave(consulta != null ? (object)consulta : (orden != null && (orden.texto || pideOk) ? (object)orden : null));
        // ---- consulta con botones: la torre pregunta y se contesta tocando
        if (consulta != null)
        {
            Rc(x0, y - 1f, W, 1f, new Color(1f, 0.8f, 0.1f, 0.5f));
            GUI.Label(new Rect(x0, y, W, 18f), Recortar(consulta.texto, sMsg, W - 4f), sMsg); y += 20f;
            float n = consulta.ops.Length, w2 = (W - 4f * (n - 1f)) / n;
            for (int i = 0; i < consulta.ops.Length; i++) { GUI.backgroundColor = new Color(1f, 0.85f, 0.3f); Halo(new Rect(x0 + i * (w2 + 4f), y, w2, 24f)); if (GUI.Button(new Rect(x0 + i * (w2 + 4f), y, w2, 24f), consulta.ops[i], sBtn)) { Responder(i); GUI.backgroundColor = bgc; break; } GUI.backgroundColor = bgc; }
            y += 30f;
        }

        // ---- preguntas a la torre (cuadraditos: la torre contesta con datos reales del juego)
        y = Preguntas(x0, y, W, k) + 4f;

        // ---- radio COM: ahora vive en la tablet (app Radio COM); RADIO_EN_PANEL la devuelve al panel si hiciera falta
        if (RADIO_EN_PANEL) y = RadioCom(x0, y, W, bgc);
        y += 6f * Mathf.Min(k, 1.3f);

        // ---- respuesta escrita
        GUI.Label(new Rect(x0, y, 200f, 14f), "RESPUESTA A LA TORRE (CUANDO PIDE UN DATO)", sEtq);
        GUI.SetNextControlName("torre_txt");
        textoRespuesta = GUI.TextField(new Rect(x0, y + 15f, W - 102f, 24f), textoRespuesta, 40, sTexto);
        bool foco = GUI.GetNameOfFocusedControl() == "torre_txt";
        { var e0 = Event.current; var rt = new Rect(x0, y + 15f, W - 102f, 24f); if (foco && (e0.type == EventType.MouseDown || e0.rawType == EventType.MouseDown) && !rt.Contains(e0.mousePosition)) { SoltarTeclado(); foco = false; } }
        if (foco) { Controles.escribiendo = true; miEsc = true; } else if (miEsc) { Controles.escribiendo = false; miEsc = false; }
        var ev = Event.current; if (foco && ev.type == EventType.KeyDown && (ev.keyCode == KeyCode.Return || ev.keyCode == KeyCode.KeypadEnter)) { EnviarTexto(); ev.Use(); }
        if (foco && ev.type == EventType.KeyDown && ev.keyCode == KeyCode.Escape) { GUI.FocusControl(""); ev.Use(); }
        if (GUI.Button(new Rect(x0 + W - 96f, y + 15f, 96f, 24f), "ENVIAR", sBtn)) EnviarTexto();
        if (orden != null && orden.texto) { Halo(new Rect(x0, y + 15f, W - 102f, 24f)); Halo(new Rect(x0 + W - 96f, y + 15f, 96f, 24f)); }
        finDiseno = y + 15f + 24f + 12f - r.y;
    }
}
