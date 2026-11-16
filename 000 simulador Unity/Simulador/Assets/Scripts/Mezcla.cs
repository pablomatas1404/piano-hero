// Mezclador de sonidos (solapa "Sonidos"): volumen y silencio por canal, mas un volumen general. Se guarda solo.
using UnityEngine;

public static class Mezcla
{
    public static readonly string[] NOMBRES = { "General", "Motor avion", "Motor auto", "Radio", "Musica", "Azafata", "Efectos FX", "Voz de torre" };
    public const int GENERAL = 0, MOTOR_AVION = 1, MOTOR_AUTO = 2, RADIO = 3, MUSICA = 4, AZAFATA = 5, FX = 6, TORRE = 7;
    static float[] vol; static bool[] mudo;

    static void Cargar()
    {
        if (vol != null) return;
        vol = new float[NOMBRES.Length]; mudo = new bool[NOMBRES.Length];
        for (int i = 0; i < NOMBRES.Length; i++)
        {
            vol[i] = PlayerPrefs.GetFloat("mezcla_vol_" + i, 1f);
            mudo[i] = PlayerPrefs.GetInt("mezcla_mudo_" + i, 0) == 1;
        }
    }

    // multiplicador final (0..1) de un canal: ya incluye el volumen general
    public static float De(int canal)
    {
        Cargar();
        if (mudo[GENERAL] || mudo[canal]) return 0f;
        return canal == GENERAL ? vol[GENERAL] : vol[canal] * vol[GENERAL];
    }

    // fila de la interfaz: nombre, slider, silenciar
    public static void Fila(int canal, GUIStyle txt)
    {
        Cargar();
        GUILayout.BeginHorizontal();
        GUILayout.Label(NOMBRES[canal], txt, GUILayout.Width(90));
        float nv = GUILayout.HorizontalSlider(vol[canal], 0f, 1f, GUILayout.Width(150));
        GUILayout.Label(Mathf.RoundToInt(nv * 100f) + "%", txt, GUILayout.Width(44));
        bool nm = GUILayout.Toggle(mudo[canal], " Anular", GUILayout.Width(70));
        GUILayout.EndHorizontal();
        if (nv != vol[canal] || nm != mudo[canal])
        {
            vol[canal] = nv; mudo[canal] = nm;
            PlayerPrefs.SetFloat("mezcla_vol_" + canal, nv); PlayerPrefs.SetInt("mezcla_mudo_" + canal, nm ? 1 : 0);
        }
    }
}
