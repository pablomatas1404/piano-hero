// TELETRANSPORTE TEMPORAL para trackear pistas sin hacer todo el viaje: un boton a la izquierda abre una lista de lugares;
// al apretar uno, el avion/helicoptero aparece a 80 m sobre ese punto (mismo rumbo). Despues se puede borrar este archivo.
using UnityEngine;
using CesiumForUnity;
using Unity.Mathematics;
using System.Collections.Generic;

public class Teletransporte : MonoBehaviour
{
    class L { public string rotulo; public string frag; public double lat, lon, h; public bool aprox; public float rumbo = -1f; }
    readonly List<L> lugares = new List<L>();
    bool abierto, armado; float prox; string aviso = ""; float avisoHasta; GUIStyle sBtn, sTxt;
    // frag = texto para buscar en la lista de aeropuertos; si no esta, se usa lat/lon de respaldo (aproximados)
    static readonly object[][] DEF = {
        new object[]{ "Punta Indio (base aeronaval)", "Punta Indio", -35.3556, -57.2889, false },
        new object[]{ "Chascomus", "Chascomus", -35.5953, -58.0178, false },
        new object[]{ "General Madariaga", "Madariaga", -37.0, -57.1333, false },
        new object[]{ "Don Torcuato", "Don Torcuato", -34.49778, -58.60556, false },
        new object[]{ "Mar del Plata - Hotel Hermitage (helipuerto)", "Hermitage", -38.00728, -57.53941, false },
        new object[]{ "Santa Teresita", "Santa Teresita", -36.5422, -56.7215, false },
    };

    // estacionamientos de hangar marcados por Pablo (salida desde el hangar: pushback, rodaje, cabecera, despegue)
    static readonly object[][] HANGARES = {
        new object[]{ "Aeroparque: salir del hangar 15 (comercial)", -34.5593987400633, -58.4109402547462, 25.44, 47f },
        new object[]{ "Aeroparque: salir del hangar 5", -34.5569770646676, -58.4144436451155, 25.61, 18.6f },
        new object[]{ "Aeroparque: salir del hangar 1", -34.5599939928741, -58.4094390137567, 25.0, 69.4f },
    };
    bool inicioHecho; float inicioDesde = -1f;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Crear() { new GameObject("Teletransporte").AddComponent<Teletransporte>(); }

    void Update()
    {
        if (armado || Time.unscaledTime < prox) return;
        prox = Time.unscaledTime + 1f;
        foreach (var d in DEF)
        {
            var l = new L { rotulo = (string)d[0], frag = (string)d[1], lat = (double)d[2], lon = (double)d[3], aprox = (bool)d[4], h = 20 };
            string nom; double la, lo, h;
            if (l.frag != "" && Aeropuertos.BuscarFrag(l.frag, out nom, out la, out lo)) { Aeropuertos.Buscar(nom, out la, out lo, out h); l.lat = la; l.lon = lo; l.h = h; }
            lugares.Add(l);
        }
        foreach (var hg in HANGARES) lugares.Insert(0, new L { rotulo = (string)hg[0], frag = "", lat = (double)hg[1], lon = (double)hg[2], h = (double)hg[3], rumbo = (float)hg[4] });
        armado = true;
    }

    void LateUpdate()
    {   // al abrir el juego el avion (Miljet) arranca volando a 1500 m sobre Aeroparque
        if (!armado || inicioHecho || lugares.Count == 0 || SelectorAvion.cargando != "") return;
        var av = FindFirstObjectByType<AvionDeCarton>(); if (av == null || av.GetComponent<CesiumGlobeAnchor>() == null) return;
        if (inicioDesde < 0f) { inicioDesde = Time.unscaledTime; return; }
        if (Time.unscaledTime - inicioDesde < 4f) return;
        inicioHecho = true;
        double la0, lo0, h0; if (!Aeropuertos.Buscar("Aeroparque", out la0, out lo0, out h0)) { la0 = -34.5592; lo0 = -58.4156; h0 = 25.0; }
        var an0 = av.GetComponent<CesiumGlobeAnchor>();
        an0.longitudeLatitudeHeight = new double3(lo0, la0, h0 + 1500.0);
        an0.rotationEastUpNorth = Quaternion.Euler(0f, 130f, 0f);
        av.IniciarEnAire(); Torre.ReiniciarCabina(true); Aviso("Volando a 1500 m sobre Aeroparque");
    }

    void Ir(L l)
    {
        var av = FindFirstObjectByType<AvionDeCarton>(); if (av == null) { Aviso("Todavia no hay avion"); return; }
        var an = av.GetComponent<CesiumGlobeAnchor>(); if (an == null) { Aviso("El avion no tiene ancla"); return; }
        an.longitudeLatitudeHeight = new double3(l.lon, l.lat, l.h + (l.rumbo >= 0f ? 5.0 : 80.0));
        if (l.rumbo >= 0f) an.rotationEastUpNorth = Quaternion.Euler(0f, l.rumbo, 0f);
        av.Reanudar();
        abierto = false; Aviso("Estas en: " + l.rotulo + (l.aprox ? " (punto aproximado, busca la pista)" : ""));
    }
    void Aviso(string t) { aviso = t; avisoHasta = Time.unscaledTime + 5f; }

    static Teletransporte inst; void Awake() { inst = this; }
    public static void Pestana(GUIStyle txt, GUIStyle btn, GUIStyle titulo)
    {
        GUILayout.Label("TELETRANSPORTE (temporal, para trackear pistas): apareces a 80 m sobre el punto", titulo);
        if (inst == null || !inst.armado) { GUILayout.Label("Esperando que carguen los aeropuertos...", txt); return; }
        var bg = GUI.backgroundColor; GUI.backgroundColor = new Color(1f, 0.85f, 0.3f);
        for (int i = 0; i < inst.lugares.Count; i += 3)
        {
            GUILayout.BeginHorizontal();
            for (int j = i; j < i + 3 && j < inst.lugares.Count; j++) { var l = inst.lugares[j]; if (GUILayout.Button(l.rotulo + (l.aprox ? " (aprox.)" : ""), btn, GUILayout.Height(24), GUILayout.Width(330))) inst.Ir(l); }
            GUILayout.EndHorizontal();
        }
        GUI.backgroundColor = bg;
        if (inst.aviso != "" && Time.unscaledTime < inst.avisoHasta) GUILayout.Label(inst.aviso, txt);
    }
}
