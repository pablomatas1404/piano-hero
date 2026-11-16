// INDICATIVO DE LLAMADA: antes de los mensajes dirigidos al piloto la torre dice "<tipo de avion> <matricula>, ..." (Resources/voz: av_*, mat_*).
// Tambien dice "rumbo a <destino>" al empezar un viaje (dest_<nombre sin acentos>). Selector de matricula dentro del panel de la torre.
using UnityEngine;

public partial class Torre
{
    static readonly string[] MATS = { "LV-ABC", "LV-MJK", "LV-PQR", "LV-FDE", "LV-TUW", "LV-HNS" };
    int matIdx = -1;        // 0..5, -1 = sin elegir
    bool elegirMat, matInit; float ultIndic = -100f;

    int Matricula() { if (!matInit) { matInit = true; matIdx = PlayerPrefs.GetInt("torre_mat", -1); if (matIdx < 0 || matIdx >= MATS.Length) { matIdx = -1; elegirMat = true; } } return matIdx; }
    string MatTxt() { int m = Matricula(); return m >= 0 ? MATS[m] : "SIN MATRÍCULA"; }

    static string ClipAvion()
    {
        string n = (AvionDeCarton.nombreAvion ?? "").ToLowerInvariant();
        if (n.Contains("777")) return "av_777";
        if (n.Contains("osmosikum")) return "av_cessna_azul";
        if (n.Contains("172")) return "av_cessna172";
        if (n.Contains("jet privado")) return "av_jet_privado";
        if (n.Contains("hornet") || n.Contains("f/a-18")) return "av_hornet";
        if (n.Contains("pc-24")) return "av_pc24";
        if (n.Contains("miljet")) return "av_miljet";
        if (n.Contains("viggen")) return "av_viggen";
        if (n.Contains("747")) return "av_747";
        if (n.Contains("a380")) return "av_a380";
        if (n.Contains("mi-35")) return "av_mi35";
        return "";
    }

    static string Slug(string s)
    {
        string d = (s ?? "").Normalize(System.Text.NormalizationForm.FormD); var sb = new System.Text.StringBuilder(); bool sep = false;
        foreach (char c in d)
        {
            var cat = System.Globalization.CharUnicodeInfo.GetUnicodeCategory(c); if (cat == System.Globalization.UnicodeCategory.NonSpacingMark) continue;
            char l = char.ToLowerInvariant(c);
            if ((l >= 'a' && l <= 'z') || (l >= '0' && l <= '9')) { if (sep && sb.Length > 0) sb.Append('_'); sep = false; sb.Append(l); } else sep = true;
        }
        return sb.ToString();
    }

    // dice "<avion>, <matricula>, <mensaje>" como una sola transmision de radio (si falta algun audio de indicativo, dice solo el mensaje)
    void DecirA(string nombre, float retraso = 0f)
    {
        if (!ordenesOn) return;
        string av = ClipAvion(); int m = Matricula(); float ahora = Time.unscaledTime; string pre = "";
        if (ahora - ultIndic > 6f)
        {
            if (av != "" && ClipVoz(av) != null) pre += av + "+";
            if (m >= 0 && ClipVoz("mat_" + (m + 1)) != null) pre += "mat_" + (m + 1) + "+";
        }
        if (pre != "" && ClipVoz(pre + nombre) != null) { ultIndic = ahora; Decir(pre + nombre, retraso); }
        else Decir(nombre, retraso);
    }

    // rumbo al destino del viaje: "<avion>, <matricula>, rumbo a <destino>"
    bool DecirRumbo(string destino, float retraso = 0f)
    {
        string c = "dest_" + Slug(destino); if (ClipVoz(c) == null) return false; DecirA(c, retraso); return true;
    }

    void MatGUI(float x0, float y, float W)
    {
        var bgc = GUI.backgroundColor;
        GUI.Label(new Rect(x0, y, W, 20f), "ELEGÍ LA MATRÍCULA DE TU AVIÓN", sTit);
        GUI.Label(new Rect(x0, y + 22f, W, 14f), "LA TORRE TE VA A LLAMAR POR ESTE NOMBRE Y ESTA MATRÍCULA", sEtq);
        float bw = (W - 8f) / 3f; y += 44f;
        for (int i = 0; i < MATS.Length; i++)
        {
            float bx = x0 + (i % 3) * (bw + 4f), by = y + (i / 3) * 40f;
            GUI.backgroundColor = i == matIdx ? new Color(0.3f, 1f, 0.45f) : new Color(1f, 0.85f, 0.3f);
            if (GUI.Button(new Rect(bx, by, bw, 34f), MATS[i], sBtn)) { matIdx = i; PlayerPrefs.SetInt("torre_mat", i); PlayerPrefs.Save(); elegirMat = false; ultIndic = -100f; Aviso("MATRÍCULA " + MATS[i] + ". LA TORRE TE LLAMA " + (ClipAvion() != "" ? "POR TU AVIÓN Y ESTA MATRÍCULA" : ""), 6f); DecirA("copio_claro"); }
        }
        GUI.backgroundColor = bgc;
        if (matIdx >= 0 && GUI.Button(new Rect(x0, y + 86f, W, 24f), "CANCELAR", sBtn)) elegirMat = false;
    }
}
