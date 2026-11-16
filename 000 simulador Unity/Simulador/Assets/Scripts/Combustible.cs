// COMBUSTIBLE: tanque de 10 ladrillos (100 L cada uno) que se gasta volando y alcanza para unos dos viajes.
// Los bidones que flotan en el aire (Torre) suman 100 L: si el tanque no esta lleno van al tanque, y si ya esta lleno van a la RESERVA (litros guardados).
// En tierra y detenido, el boton CARGAR de la torre llena el tanque desde la reserva (mas adelante se podra comprar).
using UnityEngine;

public class Combustible : MonoBehaviour
{
    public const float LITROS_LADRILLO = 100f, CAPACIDAD = 1000f, KM_TANQUE = 900f;   // el tanque lleno rinde ~900 km de vuelo (ida y vuelta Aeroparque-Rosario con margen)
    public const float EMERGENCIA = 300f;                                              // si no hay reserva, la torre te presta hasta 300 L
    public static float Tanque = CAPACIDAD, Reserva;
    public static bool Vacio { get { return Tanque <= 0.5f; } }
    public static float Ladrillos { get { return Tanque / LITROS_LADRILLO; } }

    static bool cargado; float proxGuardar; AvionDeCarton avion;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Crear() { new GameObject("Combustible").AddComponent<Combustible>(); }

    static void Cargar()
    {
        cargado = true;
        try { Tanque = Mathf.Clamp(PlayerPrefs.GetFloat("comb_tanque", CAPACIDAD), 0f, CAPACIDAD); if (Tanque < CAPACIDAD * 0.5f) Tanque = CAPACIDAD; /* al abrir el juego arranca lleno si estaba bajo */ Reserva = Mathf.Max(0f, PlayerPrefs.GetFloat("comb_reserva", 0f)); } catch { }
    }
    static void Guardar() { try { PlayerPrefs.SetFloat("comb_tanque", Tanque); PlayerPrefs.SetFloat("comb_reserva", Reserva); PlayerPrefs.Save(); } catch { } }

    void Update()
    {
        if (!cargado) Cargar();
        if (avion == null) avion = FindFirstObjectByType<AvionDeCarton>();
        if (avion != null && !AvionDeCarton.enAuto && !avion.EnTierra && !AvionDeCarton.EsHeli)
            Tanque = Mathf.Max(0f, Tanque - AvionDeCarton.VelMs * Time.deltaTime / 1000f * (CAPACIDAD / KM_TANQUE));
        if (Time.unscaledTime >= proxGuardar) { proxGuardar = Time.unscaledTime + 5f; Guardar(); }
    }

    // un bidon (u otra carga): lo que entra va al tanque, lo que sobra a la reserva
    public static void Agregar(float litros)
    {
        float lugar = CAPACIDAD - Tanque, alTanque = Mathf.Min(litros, lugar);
        Tanque += alTanque; Reserva += litros - alTanque; Guardar();
    }

    // llena el tanque con la reserva; si no alcanza y el tanque quedo casi vacio, la torre presta hasta EMERGENCIA. Devuelve un texto para el aviso
    public static string CargarTanque()
    {
        float lugar = CAPACIDAD - Tanque; if (lugar < 1f) return "EL TANQUE YA ESTÁ LLENO";
        // la carga en tierra SIEMPRE llena el tanque: primero se usa la reserva y el resto lo pone la torre
        float deReserva = Mathf.Min(lugar, Reserva); Reserva -= deReserva; Tanque = CAPACIDAD;
        string t = "TANQUE LLENO (" + Mathf.RoundToInt(CAPACIDAD) + " L)";
        if (deReserva > 0f) t += ". USASTE " + Mathf.RoundToInt(deReserva) + " L DE TU RESERVA";
        Guardar(); return t;
    }
}
