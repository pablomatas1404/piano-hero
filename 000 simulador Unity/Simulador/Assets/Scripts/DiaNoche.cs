// Ciclo dia/noche + relojito. Port de _actualizar_ciclo_dia_noche (mundo.gd) y del widget de hora (principal.gd).
// Los numeros y las formulas son los de Godot (ver PROMPT_CODIGO_LITERAL_DIA_NOCHE y RESPUESTA_RELOJ_DIA_NOCHE).
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using CesiumForUnity;

[DefaultExecutionOrder(10000)]   // despues de que el avion y la camara se muevan: la camara de marcadores (luces) no debe ir un cuadro atrasada
public class DiaNoche : MonoBehaviour
{
    const float HORA_AMANECER = 6f;
    const float HORA_ATARDECER = 21f;               // a esta hora el sol se pone
    const float SEGUNDOS_POR_DIA_COMPLETO = 9000f;  // 2.5 horas reales = 1 dia de juego
    public static float BarraX(float W) { return Mathf.Max(8f, W - 300f - 790f); }   // inicio de la fila de botones: queda a la izquierda del panel de estadisticas
    public const int CAPA_MARCADORES = 6;           // equivale a CAPA_MARCADORES_NOCTURNOS de Godot

    static readonly Color COLOR_CIELO_DIA_ARRIBA = new Color(0.385f, 0.454f, 0.55f);
    static readonly Color COLOR_CIELO_DIA_HORIZONTE = new Color(0.646f, 0.656f, 0.671f);
    static readonly Color COLOR_SUELO_DIA = new Color(0.5f, 0.56f, 0.62f);
    static readonly Color COLOR_CIELO_NOCHE_ARRIBA = new Color(0.02f, 0.03f, 0.08f);
    static readonly Color COLOR_CIELO_NOCHE_HORIZONTE = new Color(0.05f, 0.06f, 0.12f);
    static readonly Color COLOR_SUELO_NOCHE = new Color(0.04f, 0.05f, 0.1f);
    static readonly Color COLOR_LUZ_AMANECER = new Color(1f, 0.55f, 0.3f);
    static readonly Color COLOR_LUZ_DIA = new Color(1f, 0.98f, 0.92f);

    float horaDelDia = 12f;          // arranca al mediodia
    bool avanceAutomatico = true;    // arranca prendido

    AvionDeCarton avion; CesiumGlobeAnchor ancla; Camera cam; Light sol;
    Material cielo; ColorAdjustments ajusteColor; Camera camMarcadores;
    UniversalAdditionalCameraData datosCam;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Crear()
    {
        if (FindFirstObjectByType<CesiumGeoreference>() == null) return;
        new GameObject("DiaNoche").AddComponent<DiaNoche>();
    }

    void Start()
    {
        // volumen global: exposicion + saturacion (el terreno de Google es "unlit": solo se oscurece asi)
        var vol = gameObject.AddComponent<Volume>();
        vol.isGlobal = true; vol.priority = 100f;
        var perfil = ScriptableObject.CreateInstance<VolumeProfile>();
        ajusteColor = perfil.Add<ColorAdjustments>(true);
        ajusteColor.postExposure.Override(0f);
        ajusteColor.saturation.Override(0f);
        vol.profile = perfil;

        // sol y cielo
        sol = RenderSettings.sun;
        if (sol == null) foreach (var l in FindObjectsByType<Light>(FindObjectsSortMode.None)) if (l.type == LightType.Directional) { sol = l; break; }
        if (RenderSettings.skybox != null)
        {
            cielo = new Material(RenderSettings.skybox);
            RenderSettings.skybox = cielo;
        }
        RenderSettings.sun = sol;
    }

    void PrepararCamaras()
    {
        cam = Camera.main;
        if (cam == null) return;
        datosCam = cam.GetUniversalAdditionalCameraData();
        datosCam.renderPostProcessing = true;
        // segunda camara: solo ve la capa de marcadores (luces de pista, ILS), sin el oscurecimiento de noche
        cam.cullingMask &= ~(1 << CAPA_MARCADORES);
        var go = new GameObject("CamaraMarcadores");
        camMarcadores = go.AddComponent<Camera>();
        camMarcadores.cullingMask = 1 << CAPA_MARCADORES;
        var d = camMarcadores.GetUniversalAdditionalCameraData();
        d.renderType = CameraRenderType.Overlay;
        d.renderPostProcessing = false;
        datosCam.cameraStack.Add(camMarcadores);
    }

    void Update()
    {
        if (avion == null) { avion = FindFirstObjectByType<AvionDeCarton>(); if (avion != null) ancla = avion.GetComponent<CesiumGlobeAnchor>(); }
        if (cam == null) { PrepararCamaras(); }
        if (avion == null || ancla == null) return;

        if (avanceAutomatico) horaDelDia = Mathf.Repeat(horaDelDia + (Time.deltaTime / SEGUNDOS_POR_DIA_COMPLETO) * 24f, 24f);

        // hora solar reajustada: el atardecer cae a las 21 y el amanecer a las 6
        float horaSolar;
        if (horaDelDia >= HORA_AMANECER && horaDelDia < HORA_ATARDECER)
            horaSolar = 6f + (horaDelDia - HORA_AMANECER) * 12f / (HORA_ATARDECER - HORA_AMANECER);
        else
        {
            float desdeAtardecer = Mathf.Repeat(horaDelDia - HORA_ATARDECER, 24f);
            horaSolar = 18f + desdeAtardecer * 12f / (24f - HORA_ATARDECER + HORA_AMANECER);
        }
        float thetaGrados = (12f - horaSolar) * 15f;
        float theta = thetaGrados * Mathf.Deg2Rad;

        // ejes Este / Arriba en la posicion del avion
        var q = ancla.rotationEastUpNorth;
        Quaternion qEun = new Quaternion(q.value.x, q.value.y, q.value.z, q.value.w);
        Quaternion marco = avion.transform.rotation * Quaternion.Inverse(qEun);
        Vector3 este = marco * Vector3.right, arriba = marco * Vector3.up;
        Vector3 haciaSol = (Mathf.Cos(theta) * arriba + Mathf.Sin(theta) * este).normalized;
        if (sol != null) sol.transform.rotation = Quaternion.LookRotation(-haciaSol, arriba);

        float elevacion = 90f - Mathf.Abs(thetaGrados);
        float tDia = Mathf.Clamp01((elevacion + 6f) / 26f);
        float factorNoche = 1f - tDia;
        Aeropuertos.factorNoche = factorNoche;

        if (sol != null)
        {
            sol.intensity = Mathf.Lerp(0.05f, 1f, Mathf.Clamp01(elevacion / 60f));
            sol.color = Color.Lerp(COLOR_LUZ_AMANECER, COLOR_LUZ_DIA, tDia);
        }
        if (cielo != null)
        {
            // el cielo de Unity no es el mismo que el de Godot: se acerca con el color del horizonte y el suelo
            if (cielo.HasProperty("_SkyTint")) cielo.SetColor("_SkyTint", Color.Lerp(COLOR_CIELO_NOCHE_HORIZONTE, COLOR_CIELO_DIA_HORIZONTE, tDia));
            if (cielo.HasProperty("_GroundColor")) cielo.SetColor("_GroundColor", Color.Lerp(COLOR_SUELO_NOCHE, COLOR_SUELO_DIA, tDia));
            if (cielo.HasProperty("_Exposure")) cielo.SetFloat("_Exposure", Mathf.Lerp(0.05f, 1.3f, tDia));
        }
        RenderSettings.ambientIntensity = Mathf.Lerp(0.12f, 1f, tDia);

        // tonemap_exposure = lerp(0.04, 1.0, t_dia) -> en estops;  adjustment_saturation = lerp(0.15, 1.0, t_dia) -> -100..0
        if (ajusteColor != null)
        {
            ajusteColor.postExposure.value = Mathf.Log(Mathf.Lerp(0.04f, 1f, tDia), 2f);
            ajusteColor.saturation.value = (Mathf.Lerp(0.15f, 1f, tDia) - 1f) * 100f;
        }
    }

    void OnEnable() { Application.onBeforeRender += SincronizarMarcadores; }
    void OnDisable() { Application.onBeforeRender -= SincronizarMarcadores; }
    void LateUpdate() { SincronizarMarcadores(); }
    void SincronizarMarcadores()
    {
        if (cam != null && camMarcadores != null)
        {
            camMarcadores.transform.SetPositionAndRotation(cam.transform.position, cam.transform.rotation);
            camMarcadores.fieldOfView = cam.fieldOfView; camMarcadores.nearClipPlane = cam.nearClipPlane; camMarcadores.farClipPlane = cam.farClipPlane;
        }
    }

    // ---------- relojito (arriba): sol, -, HH:MM, +, Auto ----------
    void OnGUI()
    {
        float w = 250f, x = BarraX(Screen.width), y = 4f;
        var tapaR = Disposicion.Bloque("reloj", "RELOJ (hora)", new Rect(x, y, w, 26f));
        GUI.Box(new Rect(x, y, w, 26f), GUIContent.none);
        GUI.Label(new Rect(x + 6f, y + 3f, 40f, 20f), "Hora");
        if (GUI.Button(new Rect(x + 48f, y + 3f, 24f, 20f), "-")) horaDelDia = Mathf.Repeat(horaDelDia - 1f, 24f);
        int h = (int)horaDelDia; int m = Mathf.RoundToInt((horaDelDia - h) * 60f);
        if (m == 60) { m = 0; h = (h + 1) % 24; }
        var centro = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter };
        GUI.Label(new Rect(x + 74f, y + 3f, 50f, 20f), h.ToString("00") + ":" + m.ToString("00"), centro);
        if (GUI.Button(new Rect(x + 126f, y + 3f, 24f, 20f), "+")) horaDelDia = Mathf.Repeat(horaDelDia + 1f, 24f);
        avanceAutomatico = GUI.Toggle(new Rect(x + 160f, y + 3f, 80f, 20f), avanceAutomatico, " Auto");
        tapaR.Dispose();
    }
}
