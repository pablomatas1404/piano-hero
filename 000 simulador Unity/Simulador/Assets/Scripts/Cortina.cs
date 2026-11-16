// Pantalla de carga de Godot (CortinaDeCarga): una foto al azar de 3, a pantalla completa por encima de todo, con barra de progreso.
// Diferencia con Godot: la barra sigue la carga REAL de Cesium (en Godot era un progreso falso por tiempo) y se cierra sola al llegar a 100%
// (o a los 28 s como maximo, que es lo que esperaba Godot). Cualquier tecla o clic la cierra despues de 2 s.
using UnityEngine;
using UnityEngine.InputSystem;
using CesiumForUnity;

public class Cortina : MonoBehaviour
{
    const float ESPERA_MAXIMA = 28f, ESPERA_MINIMA = 2f;
    Texture2D foto; float t; bool visible = true; float progreso; GUIStyle sTxt; Cesium3DTileset[] sets; float proxBusqueda;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Crear() { new GameObject("Cortina").AddComponent<Cortina>(); }

    void Start()
    {
        string[] nombres = { "presentacion_atardecer.png", "presentacion_noche.png", "presentacion_tierra.png" };
        string carpeta = System.IO.Path.GetFullPath(System.IO.Path.Combine(Application.dataPath, "..", "..", "..", "simulador Claude", "imagenes_presentacion"));
        string ruta = System.IO.Path.Combine(carpeta, nombres[Random.Range(0, nombres.Length)]);
        try
        {
            if (System.IO.File.Exists(ruta)) { foto = new Texture2D(2, 2, TextureFormat.RGB24, false); foto.LoadImage(System.IO.File.ReadAllBytes(ruta)); }
        }
        catch { foto = null; }
        if (foto == null) Debug.LogWarning("[Cortina] no encuentro las fotos de presentacion en " + carpeta);
    }

    void Update()
    {
        if (!visible) return;
        t += Time.unscaledDeltaTime;
        proxBusqueda -= Time.unscaledDeltaTime;
        if (sets == null || proxBusqueda <= 0f) { sets = FindObjectsByType<Cesium3DTileset>(FindObjectsSortMode.None); proxBusqueda = 2f; }
        float m = 0f; int n = 0;
        foreach (var s in sets) { if (s == null) continue; m += Mathf.Clamp(s.ComputeLoadProgress(), 0f, 100f); n++; }
        float real = n > 0 ? m / n : 0f;
        progreso = Mathf.Max(progreso, Mathf.Max(real, t / ESPERA_MAXIMA * 100f * 0.6f));   // nunca retrocede; si Cesium tarda, avanza igual por tiempo hasta ~60%
        bool listo = (n > 0 && real >= 99.9f && t >= ESPERA_MINIMA) || t >= ESPERA_MAXIMA;
        bool saltar = t >= ESPERA_MINIMA && ((Keyboard.current != null && Keyboard.current.anyKey.wasPressedThisFrame) || (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame));
        if (listo || saltar) { progreso = 100f; visible = false; }
    }

    void OnGUI()
    {
        if (!visible) return;
        GUI.depth = -10000;   // por encima de todo (como z_index 4000 de Godot)
        float W = Screen.width, H = Screen.height;
        GUI.color = Color.black; GUI.DrawTexture(new Rect(0, 0, W, H), Texture2D.whiteTexture); GUI.color = Color.white;
        if (foto != null)
        {
            float asp = (float)foto.width / foto.height, w = W, h = W / asp;
            if (h < H) { h = H; w = H * asp; }   // cubre la pantalla entera
            GUI.DrawTexture(new Rect((W - w) * 0.5f, (H - h) * 0.5f, w, h), foto, ScaleMode.StretchToFill);
        }
        if (sTxt == null) { sTxt = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontSize = 16, fontStyle = FontStyle.Bold }; sTxt.normal.textColor = Color.white; }
        float bw = Mathf.Min(520f, W - 80f), bx = (W - bw) * 0.5f, by = H - 70f;
        GUI.color = new Color(0f, 0f, 0f, 0.6f); GUI.DrawTexture(new Rect(bx - 4f, by - 4f, bw + 8f, 22f), Texture2D.whiteTexture);
        GUI.color = new Color(0.3f, 0.8f, 1f); GUI.DrawTexture(new Rect(bx, by, bw * Mathf.Clamp01(progreso / 100f), 14f), Texture2D.whiteTexture);
        GUI.color = Color.white;
        GUI.Label(new Rect(bx, by - 28f, bw, 24f), "Cargando el escenario... " + Mathf.RoundToInt(progreso) + "%" + (t >= ESPERA_MINIMA ? "   (toca una tecla para entrar)" : ""), sTxt);
    }
}
