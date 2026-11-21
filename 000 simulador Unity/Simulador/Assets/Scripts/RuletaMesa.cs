using System.Collections.Generic;
using UnityEngine;

// Mesa de la ruleta. Ventanas independientes que se mueven y se estiran desde 8 nodos (esquinas y lados):
// billetes (caja), ventanilla (horizontal), fichas, paño (girado, con el 0 arriba) con sus botones, cartel del resultado y la vista de la ruleta.
public class RuletaMesa
{
    static readonly int[] BV = { 1, 5, 10, 20, 50, 100, 1000 };
    static readonly int[] CV = { 1, 5, 10, 25, 50, 100, 500, 1000, 5000 };
    static readonly int[] ROJOS = { 1, 3, 5, 7, 9, 12, 14, 16, 18, 19, 21, 23, 25, 27, 30, 32, 34, 36 };
    class Marca { public int n; public bool puesta; public Vector2 uv; }
    class Ficha { public bool placa; public bool ganadora; public int v; public Vector2 p; public bool pano; public Vector2 uv; public Vector2 pPrev; public bool panoPrev; public Vector2 uvPrev; }
    class Vent { public string id, titulo; public Rect r; public bool fija, barra; public Vector2 min; public float reservaAbajo; }

    Texture2D[] bill = new Texture2D[7]; Texture2D[][] chips = new Texture2D[7][];
    int diseno = 1; int[] stock = { 0, 0, 0, 0, 0, 0, 0 }; int maxFicha = 8;   // maxFicha: indice de la ficha mas grande que se pide en el cambio
    List<Ficha> fichas = new List<Ficha>(); List<Ficha> apuesta = new List<Ficha>(); List<int> vent = new List<int>(); List<int> ventPla = new List<int>(); GUIStyle sTc; int pagoPend, pagoDen, pagoRes, cambioPend; List<string> ganLineas = new List<string>(); string ganTitulo = ""; float tGan; AudioClip clipGan;
    static readonly int[] PL = { 5000, 10000, 50000, 100000 };   // placas: rectangulares y grandes, para premios grandes
    static readonly string[] PLN = { "CINCO MIL", "DIEZ MIL", "CINCUENTA MIL", "CIEN MIL" };
    const float PLA_W = 420f, PLA_H = 190f, PLW = 92f, PLH = 57f;
    List<KeyValuePair<int, Vector2>> ultima = new List<KeyValuePair<int, Vector2>>();   // jugada de la tirada anterior (valor y lugar en el paño)
    Vent vBil, vVen, vFic, vPano, vRes, vRul, vPla, vApu, vTot, vApos, vFoto, vBtns; Texture2D texFoto; public bool tirarPedido; static Texture2D oroTex; Vent[] todas; List<int> hist = new List<int>(); bool hayPl;
    // ---- plata real: la tablet conecta estos ganchos con la cuenta del avion (solo, sin tablet, usa una cuenta de prueba)
    public static System.Func<int> PlataLeer; public static System.Action<int> PlataSumar;
    // ---- dentro de la tablet la mesa se dibuja en un lienzo virtual (VW x VH) que la tablet escala: el mouse se convierte con OrigenV y EscV
    public static float VW, VH, EscV = 1f; public static Vector2 OrigenV;
    public static float SW { get { return VW > 0f ? VW : Screen.width; } }
    public static float SH { get { return VW > 0f ? VH : Screen.height; } }
    public static System.Action<bool> Escribiendo;   // la tablet lo conecta para que el avion no reaccione al teclado mientras escribis un monto
    // MODO PRUEBA: ingresar no descuenta de tu plata real y retirarse no suma (asi se puede apostar mucho para probar); se cambia desde la fila de arriba
    static int pr = -1;
    public static bool modoPrueba { get { if (pr < 0) pr = PlayerPrefs.GetInt("rul14_prueba", 1); return pr == 1; } }
    public static void PonerPrueba(bool v) { pr = v ? 1 : 0; PlayerPrefs.SetInt("rul14_prueba", pr); PlayerPrefs.Save(); }
    public bool cerrarPedido;                     // la ruleta lo lee para cerrarse (RETIRARSE)
    const bool CAJA = true;                         // la caja muestra TUS billetes ingresados (la casa, en cambio, tiene billetes infinitos para el vuelto)
    const int TOPE_INGRESO = 10000;
    int modal; string ingTxt = "", ingMsg = ""; bool ordenPend;   // modal: 1 = ingresar dinero, 2 = retirarse
    Ficha badgeF; int badgeN; float tBadge;          // numerito de cuantas fichas llevas en esa apuesta
    static int PlataAhora() { return PlataLeer != null ? PlataLeer() : PlayerPrefs.GetInt("rul11_plata", 50000); }
    static void PlataMover(int d) { if (PlataSumar != null) PlataSumar(d); else { PlayerPrefs.SetInt("rul11_plata", Mathf.Max(0, PlataAhora() + d)); PlayerPrefs.Save(); } }
    Vent vAct; int modo = -1; Vector2 m0, off; Rect r0;   // modo: 0..7 nodo, 8 mover
    Ficha dragF; int dragB = -1; Vector2 mouse;
    Marca[] marcas = new Marca[73];   // 0-36 plenos, 37-43 chances (par, impar, mayor, menor, 1a/2a/3a docena)
    static readonly string[] NCH = { "PAR", "IMPAR", "MAYOR", "MENOR", "1ª\nDOC", "2ª\nDOC", "3ª\nDOC", "ROJO", "NEGRO", "1ª\nCOL", "2ª\nCOL", "3ª\nCOL", "1-3", "4-6", "7-9", "10-12", "13-15", "16-18", "19-21", "22-24", "25-27", "28-30", "31-33", "34-36", "1-3b", "4-6b", "7-9b", "10-12b", "13-15b", "16-18b", "19-21b", "22-24b", "25-27b", "28-30b", "31-33b", "34-36b" };
    static readonly string[] NCHL = { "Par", "Impar", "Mayor (19-36)", "Menor (1-18)", "Primera docena", "Segunda docena", "Tercera docena", "Rojo", "Negro", "Primera columna", "Segunda columna", "Tercera columna" }; bool modoMarcas, mapeado; int dragM = -1; Texture2D disco;   // marcas blancas con el numero: se ponen en el centro de cada pleno del paño
    const float ASP = 600f / 276f;            // alto/ancho del paño ya girado (solo para el tamaño inicial)
    const float BIL_W = 372f, BIL_H = 284f;   // ventana de billetes sin escalar
    const float VEN_W = 720f, VEN_H = 150f;   // ventanilla horizontal sin escalar
    const float FIC_W = 700f;                 // zona de fichas sin escalar
    const float CS = 66f;                     // ficha grande: la que se lleva agarrada y la de la pila
    const float CP = 26f;                     // ficha ya apostada sobre el paño (tamaño de antes)
    string aviso = ""; float tAviso; bool muestrario; int pantW, pantH, sinBoton; bool lanzadaAntes; int resultadoAntes = -1;
    Dictionary<Vent, bool> guardada = new Dictionary<Vent, bool>();
    static readonly Vector2[] HD = { new Vector2(0.5f, 0f), new Vector2(1f, 0f), new Vector2(1f, 0.5f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, 1f), new Vector2(0f, 0.5f), new Vector2(0f, 0f) };   // N NE E SE S SW W NW
    static readonly Vector2[] HX = { new Vector2(0.25f, 0f), new Vector2(0.75f, 0f), new Vector2(1f, 0.25f), new Vector2(1f, 0.75f), new Vector2(0.25f, 1f), new Vector2(0.75f, 1f), new Vector2(0f, 0.25f), new Vector2(0f, 0.75f) };   // extras: 2 por lado
    static readonly int[] HXK = { 0, 0, 2, 2, 4, 4, 6, 6 };   // a que lado pertenece cada extra (N E S W)
    bool HExtra(Vent v, int j) { bool horiz = j < 2 || (j >= 4 && j < 6); return horiz ? v.r.width >= 150f : v.r.height >= 150f; }   // en ventanas chicas no se ponen los extras

    public RuletaMesa()
    {
        for (int i = 0; i < BV.Length; i++) bill[i] = Resources.Load<Texture2D>("Ruleta/billetes/billete_" + BV[i]);
        diseno = Mathf.Clamp(PlayerPrefs.GetInt("rul6_diseno", 1), 1, 6);
        if (PlayerPrefs.GetInt("rul21_d6", 0) == 0) { PlayerPrefs.SetInt("rul21_d6", 1); diseno = 6; PlayerPrefs.SetInt("rul6_diseno", 6); }   // 21/11: el diseño 6 (casino en vivo) queda elegido la primera vez
        for (int d = 1; d <= 6; d++) { chips[d] = new Texture2D[CV.Length]; for (int i = 0; i < CV.Length; i++) chips[d][i] = Resources.Load<Texture2D>("Ruleta/fichas/d" + d + "/ficha_" + CV[i]); }
        vBil = new Vent { id = "bil", titulo = "caja de billetes", barra = true, min = new Vector2(150f, 110f) };
        vVen = new Vent { id = "ven", titulo = "ventanilla", barra = true, min = new Vector2(280f, 50f) };
        vFic = new Vent { id = "fic", titulo = "fichas", barra = true, min = new Vector2(220f, 150f), reservaAbajo = 4f };
        vPano = new Vent { id = "pano", titulo = "paño", barra = true, min = new Vector2(90f, 120f), reservaAbajo = 78f };
        vRes = new Vent { id = "res", titulo = "cartel", barra = false, min = new Vector2(70f, 50f) };
        vRul = new Vent { id = "rul", titulo = "ruleta", barra = false, min = new Vector2(220f, 160f) };
        vPla = new Vent { id = "pla", titulo = "placas", barra = true, min = new Vector2(180f, 90f) };
        vApu = new Vent { id = "apu", titulo = "apostado", barra = false, min = new Vector2(110f, 40f) };
        if (PlayerPrefs.GetInt("rul10_v", 0) < 1) { foreach (var id in new[] { "fic", "res", "pla" }) foreach (var suf in new[] { "_x", "_y", "_w", "_h", "_f" }) PlayerPrefs.DeleteKey("rul7_" + id + suf); PlayerPrefs.SetInt("rul10_v", 1); PlayerPrefs.Save(); }   // medidas nuevas de fichas y cartel
        for (int n = 0; n < marcas.Length; n++) marcas[n] = new Marca { n = n };
        CargarMarcas();
        vTot = new Vent { id = "tot", titulo = "total", barra = false, min = new Vector2(100f, 40f) };
        vApos = new Vent { id = "apos", titulo = "apostar", barra = false, min = new Vector2(80f, 34f) };
        vBtns = new Vent { id = "btns", titulo = "botones", barra = false, min = new Vector2(120f, 50f) };
        vFoto = new Vent { id = "foto", titulo = "rueda", barra = false, min = new Vector2(60f, 60f) }; texFoto = Resources.Load<Texture2D>("Ruleta/rueda/rueda_numeros");
        todas = new[] { vRes, vBil, vVen, vFic, vPano, vRul, vPla, vApu, vTot, vApos };
        if (PlayerPrefs.GetInt("rul15_v", 0) < 1) { foreach (var id in new[] { "bil", "ven", "fic", "pano", "res", "rul", "pla", "apu" }) { string kk = "rul7_" + id + "_y"; if (PlayerPrefs.HasKey(kk)) PlayerPrefs.SetFloat(kk, Mathf.Max(0f, PlayerPrefs.GetFloat(kk) - 30f)); } PlayerPrefs.SetInt("rul15_v", 1); PlayerPrefs.Save(); }   // los botones de arriba pasaron a la barra azul: todo sube un escalon
        if (VW > 0f && PlayerPrefs.GetInt("rul13_v", 0) < 1) { foreach (var v in todas) foreach (var suf in new[] { "_x", "_y", "_w", "_h", "_f", "_cw", "_ch" }) PlayerPrefs.DeleteKey("rul7_" + v.id + suf); PlayerPrefs.SetInt("rul13_v", 1); PlayerPrefs.Save(); }
        foreach (var v in todas) Cargar(v);
        if (PlayerPrefs.GetInt("rul17_hist", 0) < 1 && vRes.r.width > 1f) { var rq = vRes.r; rq.height = Mathf.Min(rq.height * 2.3f, Mathf.Max(60f, SH - rq.y - 20f)); vRes.r = rq; Guardar(vRes); PlayerPrefs.SetInt("rul17_hist", 1); }   // ahora son los ultimos 10
        CargarMesa();
    }

    // ---------------------------------------------------------------- ventanas
    bool Cargar(Vent v, string pre = "rul7_")
    {
        string k = pre + v.id; if (!PlayerPrefs.HasKey(k + "_w")) return false;
        v.r = new Rect(PlayerPrefs.GetFloat(k + "_x"), PlayerPrefs.GetFloat(k + "_y"), PlayerPrefs.GetFloat(k + "_w"), PlayerPrefs.GetFloat(k + "_h")); v.fija = false; guardada[v] = true;
        float cw0 = PlayerPrefs.GetFloat(k + "_cw", 0f), ch0 = PlayerPrefs.GetFloat(k + "_ch", 0f); if (cw0 > 1f && ch0 > 1f) { float sx = SW / cw0, sy = SH / ch0; v.r = new Rect(v.r.x * sx, v.r.y * sy, v.r.width * sx, v.r.height * sy); }
        return true;
    }
    void Guardar(Vent v)
    {
        if (apostarGrande && v != vPano && v != vFic && v != vFoto && v != vApu && v != vTot && v != vBtns) return;   // en el paño grande solo se guardan esas dos ventanas, aparte de la disposicion normal
        string k = (apostarGrande ? "rul21g_" : "rul7_") + v.id; PlayerPrefs.SetFloat(k + "_x", v.r.x); PlayerPrefs.SetFloat(k + "_y", v.r.y); PlayerPrefs.SetFloat(k + "_w", v.r.width); PlayerPrefs.SetFloat(k + "_h", v.r.height); PlayerPrefs.SetInt(k + "_f", v.fija ? 1 : 0); PlayerPrefs.SetFloat(k + "_cw", lW > 0f ? lW : SW); PlayerPrefs.SetFloat(k + "_ch", lH > 0f ? lH : SH); PlayerPrefs.Save(); guardada[v] = true;
    }
    void Alternar(Vent v) { v.fija = !v.fija; Guardar(v); }

    void Defecto(Vent v) { v.r = LayoutRect(v); }
    // disposicion automatica pensada para jugar: la rueda bien a la vista, el paño a la derecha, billetes y ventanilla al costado y abajo
    // compu (sin pantalla tactil): la misma disposicion que Pablo armo en el iPad (foto REFERENCIA_layout_ipad_2026-11-19.jpg), en proporcion a la pantalla
    static bool EsCompu { get { return Application.platform == RuntimePlatform.WebGLPlayer && !Input.touchSupported; } }
    Rect LayoutPC(Vent v)
    {
        float W = SW, H = SH; System.Func<float, float, float, float, Rect> R = (x, y, w, h) => new Rect(x / 1280f * W, y / 922f * H, w / 1280f * W, h / 922f * H);
        if (v == vRul) return R(0, 2, 712, 880);
        if (v == vApu) return R(634, 104, 288, 70);
        if (v == vTot) return R(629, 175, 288, 67);
        if (v == vRes) return R(848, 282, 67, 530);
        if (v == vPano) return R(922, 144, 352, 776);
        if (v == vFic) return R(0, 140, 836, 780);
        if (v == vVen) return R(10, 96, 600, 125);
        if (v == vBil) return R(10, 228, 520, 360);
        if (v == vPla) return R(640, 300, 200, 110);
        return R(10, 210, 300, 150);
    }
    Rect LayoutRect(Vent v)
    {
        if (v == vApos) { var rr0 = LayoutRect(vRul); return new Rect(rr0.center.x - 62f, rr0.center.y - 22f, 124f, 44f); }   // boton APOSTAR: al medio de la ruleta (la cruz dorada), movible
        float W = SW, H = SH, m = 8f;
        if (EsCompu && W > H) return LayoutPC(v);
        if (H > W * 0.95f)   // pantalla alta: la rueda arriba y todo lo demas abajo
        {
            float rh = Mathf.Min(H * 0.34f, (W - 16f) * 0.8f), y0 = 4f + rh + 34f, pw = W * 0.38f, ph = pw * ASP, lw = W - pw - 24f, vh = lw / (VEN_W / VEN_H), cw = Mathf.Min(lw - 72f, 280f), ch = cw * BIL_H / BIL_W, yv = y0 + 62f + ch + 34f;
            if (v == vRul) return new Rect(8, 4, W - 16, rh);
            if (v == vTot) return new Rect(W - 16f - 176f, 10f, 176f, 54f);
            if (v == vPano) return new Rect(W - pw - 8, y0, pw, ph);
            if (v == vRes) return new Rect(8, y0, 64, 150);
            if (v == vApu) return new Rect(80, y0, Mathf.Max(110f, lw - 72f), 56);
            if (v == vBil) return new Rect(80, y0 + 62, cw, ch);
            if (v == vPla) return new Rect(80, y0 + 62, Mathf.Max(180f, lw - 72f), Mathf.Max(90f, (lw - 72f) * PLA_H / PLA_W));
            if (v == vVen) return new Rect(8, yv, lw, vh);
            if (v == vFic) { float y = yv + vh + 34f; return new Rect(8, y, lw, Mathf.Max(150f, H - y - 10)); }
            return new Rect(10, 210, BIL_W, BIL_H);
        }
        float pw2 = Mathf.Min(0.27f * W, (H - 120f) / ASP), ph2 = pw2 * ASP, Lw = W - pw2 - 3f * m;
        float ws = Mathf.Min(Lw * 0.58f, H * 0.58f), hr = ws * 0.88f, yb = 34f + hr + 34f, x1 = m + ws + m, w1 = Lw - ws - m;
        float cw2 = Mathf.Clamp(w1 - 72f, 150f, 300f), ch2 = cw2 * BIL_H / BIL_W, vw = Mathf.Min(Lw * 0.5f, 520f), vh2 = vw / (VEN_W / VEN_H);
        if (v == vRul) return new Rect(m, 4, ws, hr);
        if (v == vTot) return new Rect(x1 + 72, 38 + 60, Mathf.Max(110f, w1 - 72f), 54);
        if (v == vPano) return new Rect(W - pw2 - m, 38, pw2, ph2);
        if (v == vRes) return new Rect(x1, 38, 64, Mathf.Min(190f, hr * 0.55f));
        if (v == vApu) return new Rect(x1 + 72, 38, Mathf.Max(110f, w1 - 72f), 56);
        if (v == vBil) return new Rect(x1 + 72, 100, cw2, ch2);
        if (v == vPla) { float pwid = Mathf.Max(180f, Mathf.Min(w1 - 72f, PLA_W)); return new Rect(x1 + 72, 100 + ch2 + 8, pwid, pwid * PLA_H / PLA_W); }
        if (v == vVen) return new Rect(m, yb, vw, vh2);
        if (v == vFic) return new Rect(m + vw + m, yb, Lw - vw - m, Mathf.Max(150f, H - yb - 8));
        return new Rect(10, 210, BIL_W, BIL_H);
    }
    public void OrdenarTodo() { foreach (var v in todas) { v.r = LayoutRect(v); Acomodar(v); Guardar(v); } }   // deja todo acomodado para jugar y lo guarda
    void GuardarTodas() { foreach (var v in todas) Guardar(v); }
    const float REC0 = 33f / 342f, RECW = 276f / 342f;   // recorte del paño verde: desde la linea blanca de afuera hasta la otra
    bool recMig;
    public bool cajaVis;   // la caja de billetes aparece sobre el paño solo cuando se aprieta el boton
    void MigrarRecorte()   // una sola vez: lo ya marcado/apostado/acomodado sobre el paño viejo pasa al paño recortado
    {
        recMig = true; if (PlayerPrefs.GetInt("rul14_crop", 0) >= 1) return;
        for (int i = 0; i < marcas.Length; i++) { var m = marcas[i]; if (m != null && m.puesta) m.uv = new Vector2((m.uv.x - REC0) / RECW, m.uv.y); }
        GuardarChances();
        foreach (var f in fichas) if (f.pano) f.uv = new Vector2((f.uv.x - REC0) / RECW, f.uv.y);
        if (vPano.r.width >= 1f) { var r = vPano.r; vPano.r = new Rect(r.x + r.width * REC0, r.y, r.width * RECW, r.height); Guardar(vPano); }
        GuardarMesa(); PlayerPrefs.SetInt("rul14_crop", 1); PlayerPrefs.Save();
    }
    Texture2D girSrc, girTex;
    // paño girado 90 grados (21/11, para el celu): el paño se dibuja rotado dentro de su ventana; todo lo de adentro (fichas, numeros,
    // boton de tirar) se calcula sobre el paño "sin girar" (PR) y se pasa a la pantalla con DePano / desde la pantalla con APano.
    public static bool panoRot, panoInv;   // panoInv: gira para el otro lado (el 0 mira hacia la ruleta y las chances quedan abajo)
    Rect PR { get { var r = vPano.r; if (!panoRot) return r; var c = r.center; return new Rect(c.x - r.height / 2f, c.y - r.width / 2f, r.height, r.width); } }
    Vector2 APano(Vector2 p) { if (!panoRot) return p; var c = vPano.r.center; var d = p - c; return panoInv ? c + new Vector2(-d.y, d.x) : c + new Vector2(d.y, -d.x); }    // pantalla -> paño sin girar
    Vector2 DePano(Vector2 p) { if (!panoRot) return p; var c = vPano.r.center; var d = p - c; return panoInv ? c + new Vector2(d.y, -d.x) : c + new Vector2(-d.y, d.x); }   // paño sin girar -> pantalla
    Rect BtnGirar { get { var c = vPano.r.center; float w = tactil ? 300f : 180f, h = tactil ? 64f : 44f; return new Rect(c.x - w / 2f, c.y - h / 2f, w, h); } }
    void GirarPano()
    {
        bool antes = panoRot;   // ciclo: acostado -> parado (un lado) -> parado (el otro lado) -> acostado
        if (!panoRot) { panoRot = true; panoInv = true; } else if (panoInv) panoInv = false; else { panoRot = false; panoInv = false; }
        PlayerPrefs.SetInt("rul21_pano_rot", panoRot ? 1 : 0); PlayerPrefs.SetInt("rul21_pano_inv", panoInv ? 1 : 0);
        var r = vPano.r; if (antes != panoRot) { var c = r.center; vPano.r = new Rect(c.x - r.height / 2f, c.y - r.width / 2f, r.height, r.width); } Guardar(vPano); PlayerPrefs.Save();
    }
    Texture2D PanoGirado(Texture2D t)   // paño girado 90 grados y recortado (el verde sobrante del paño 1), ya listo para dibujar sin matrices
    {
        if (t == girSrc && girTex != null) return girTex;
        int w = t.width, h = t.height, ya = 0, ny = h; if (t.name == "pano1_verde") { ya = Mathf.RoundToInt(33f * h / 342f); ny = Mathf.RoundToInt(276f * h / 342f); }   // por nombre y no por tamaño: Unity puede reescalar la imagen
        var rt = RenderTexture.GetTemporary(w, h, 0); var prev = RenderTexture.active; Graphics.Blit(t, rt); RenderTexture.active = rt;
        var rd = new Texture2D(w, h, TextureFormat.RGBA32, false); rd.ReadPixels(new Rect(0, 0, w, h), 0, 0); RenderTexture.active = prev; RenderTexture.ReleaseTemporary(rt);
        var src = rd.GetPixels32(); var dst = new Color32[ny * w];
        for (int yd = 0; yd < w; yd++) for (int xd = 0; xd < ny; xd++) { int ytop = ya + ny - 1 - xd; dst[(w - 1 - yd) * ny + xd] = src[(h - 1 - ytop) * w + yd]; }
        UnityEngine.Object.Destroy(rd); if (girTex != null) UnityEngine.Object.Destroy(girTex);
        girTex = new Texture2D(ny, w, TextureFormat.RGBA32, false); girTex.SetPixels32(dst); girTex.wrapMode = TextureWrapMode.Clamp; girTex.Apply(); girSrc = t; return girTex;
    }
    void Iniciar()
    {
        if (!recMig) MigrarRecorte();
        panoRot = PlayerPrefs.GetInt("rul21_pano_rot", 0) == 1; panoInv = PlayerPrefs.GetInt("rul21_pano_inv", 0) == 1;
        int iw = Mathf.RoundToInt(SW), ih = Mathf.RoundToInt(SH); bool cambio = pantW != iw || pantH != ih; bool primera = pantW == 0; pantW = iw; pantH = ih;
        if (VW > 0f) cambio = false;   // dentro de la tablet el tamaño cambia seguido: las ventanas no se reacomodan solas
        float cW = SW, cH = SH;
        if (lW <= 0f) { lW = cW; lH = cH; }
        else if (Mathf.Abs(cW - lW) > 0.5f || Mathf.Abs(cH - lH) > 0.5f)   // cambio el tamaño: las ventanas se achican o agrandan juntas, manteniendo su disposicion
        {
            float sx = cW / lW, sy = cH / lH;
            foreach (var v in todas) if (v.r.width >= 1f) v.r = new Rect(v.r.x * sx, v.r.y * sy, v.r.width * sx, v.r.height * sy);
            lW = cW; lH = cH;
        }
        if (EsCompu && SW > SH && PlayerPrefs.GetInt("rul20_pc", 0) < 1) { PlayerPrefs.SetInt("rul20_pc", 1); foreach (var v in todas) { v.r = LayoutPC(v); Guardar(v); } lW = SW; lH = SH; }
        foreach (var v in todas)
        {
            if (v.r.width < 1f) Defecto(v);
            Acomodar(v);
        }
    }
    float tOrd = -10f;
    public void PedirOrdenar()   // ORDENAR borra tu disposicion: pide confirmar con un segundo toque
    {
        if (Time.realtimeSinceStartup - tOrd < 4f) { tOrd = -10f; Restaurar(); Aviso("Todo volvió a su lugar original"); }
        else { tOrd = Time.realtimeSinceStartup; Aviso("¿Seguro? Esto desarma lo que acomodaste. Tocá ORDENAR otra vez para confirmar"); }
    }
    public void Restaurar() { foreach (var v in todas) v.fija = false; OrdenarTodo(); }
    void FijarTodas(bool f) { foreach (var v in todas) { v.fija = f; Guardar(v); } }

    void Acomodar(Vent v)   // siempre completa dentro de la pantalla
    {
        float W = SW, H = SH, top = v.barra ? 30f : 0f, bot = v.reservaAbajo;
        var r = v.r; r.width = Mathf.Clamp(r.width, v.min.x, Mathf.Max(v.min.x, W)); r.height = Mathf.Clamp(r.height, v.min.y, Mathf.Max(v.min.y, H - top - bot));
        r.x = Mathf.Clamp(r.x, 0f, Mathf.Max(0f, W - r.width)); r.y = Mathf.Clamp(r.y, top, Mathf.Max(top, H - bot - r.height)); v.r = r;
    }
    static bool tactil { get { return Application.isMobilePlatform || Application.platform == RuntimePlatform.WebGLPlayer; } }
    float NS(Vent v) { return tactil ? Mathf.Clamp(Mathf.Min(v.r.width, v.r.height) / 3f, 22f, 42f) : 18f; }   // en celu/tablet los nodos son grandes para agarrarlos con el dedo
    Rect HRect(Vent v, int i)
    {
        var r = v.r; var c = new Vector2(r.x + r.width * HD[i].x, r.y + r.height * HD[i].y); float ns = NS(v), m = ns / 2f; c.x = Mathf.Clamp(c.x, m, SW - m); c.y = Mathf.Clamp(c.y, m, SH - m);
        return new Rect(c.x - m, c.y - m, ns, ns);
    }
    Rect HRectX(Vent v, int j) { var r = v.r; var c = new Vector2(r.x + r.width * HX[j].x, r.y + r.height * HX[j].y); float ns = NS(v), m = ns / 2f; c.x = Mathf.Clamp(c.x, m, SW - m); c.y = Mathf.Clamp(c.y, m, SH - m); return new Rect(c.x - m, c.y - m, ns, ns); }
    int HitNodo(Vent v, Vector2 p)
    {
        if (v.fija || !editando) return -1;
        for (int i = 1; i < 8; i += 2) if (HRect(v, i).Contains(p)) return i;   // solo las 4 esquinas (21/11: los demas confundian)
        return -1;
    }
    Rect Barra(Vent v) { return new Rect(v.r.x + (v == vPano ? 104f : 0f), v.r.y - 26f, v.r.width - (v == vPano ? 196f : 0f), 26f); }
    void DibujarNodos(Vent v, bool borde)
    {
        if (v.fija || !editando) return;
        if (borde) { var c = new Color(1f, 1f, 1f, 0.35f); Fondo(new Rect(v.r.x, v.r.y, v.r.width, 2), c); Fondo(new Rect(v.r.x, v.r.yMax - 2, v.r.width, 2), c); Fondo(new Rect(v.r.x, v.r.y, 2, v.r.height), c); Fondo(new Rect(v.r.xMax - 2, v.r.y, 2, v.r.height), c); }
        for (int i = 1; i < 8; i += 2) { var h = HRect(v, i); Fondo(h, new Color(0f, 0f, 0f, 0.85f)); Fondo(new Rect(h.x + 2, h.y + 2, h.width - 4, h.height - 4), new Color(1f, 0.9f, 0.3f, 0.95f)); }
    }
    void DibujarBarra(Vent v, GUIStyle sT)
    {
        var b = Barra(v); Fondo(new Rect(v.r.x, b.y, v.r.width, 26), new Color(0.1f, 0.1f, 0.1f, 0.95f));
        GUI.Label(new Rect(b.x + 6, b.y, b.width, 26), v.titulo + (v.fija ? "  (clavada)" : ""), new GUIStyle(sT) { fontSize = 14 });
    }
    void Redimensionar(Vent v, int h, Vector2 m)
    {
        Vector2 d = m - m0; float L = r0.x, R = r0.xMax, T = r0.y, B = r0.yMax;
        bool n = h == 0 || h == 1 || h == 7, e = h == 1 || h == 2 || h == 3, s = h == 3 || h == 4 || h == 5, w = h == 5 || h == 6 || h == 7;
        if (w) L += d.x; if (e) R += d.x; if (n) T += d.y; if (s) B += d.y;
        float W = SW, H = SH, top = v.barra ? 30f : 0f, bot = v.reservaAbajo;
        L = Mathf.Max(L, 0f); R = Mathf.Min(R, W); T = Mathf.Max(T, top); B = Mathf.Min(B, H - bot);
        if (R - L < v.min.x) { if (w) L = R - v.min.x; else R = L + v.min.x; }
        if (B - T < v.min.y) { if (n) T = B - v.min.y; else B = T + v.min.y; }
        v.r = Rect.MinMaxRect(L, T, R, B);
    }

    // ---------------------------------------------------------------- coordenadas locales de cada ventana -> pantalla
    float KB { get { return Mathf.Min(vBil.r.width / BIL_W, vBil.r.height / BIL_H); } }
    float KV { get { return Mathf.Min(vVen.r.width / VEN_W, vVen.r.height / VEN_H); } }
    float KF { get { return vFic.r.width / FIC_W; } }
    float KP { get { return Mathf.Min(vPla.r.width / PLA_W, vPla.r.height / PLA_H); } }
    Rect SRB(Rect l) { float k = KB; return new Rect(vBil.r.x + l.x * k, vBil.r.y + l.y * k, l.width * k, l.height * k); }
    Rect SRV(Rect l) { float k = KV; return new Rect(vVen.r.x + l.x * k, vVen.r.y + l.y * k, l.width * k, l.height * k); }
    Rect SRF(Rect l) { float k = KF; return new Rect(vFic.r.x + l.x * k, vFic.r.y + l.y * k, l.width * k, l.height * k); }
    Vector2 SPF(Vector2 l) { return vFic.r.position + l * KF; }
    Vector2 LPF(Vector2 s) { return (s - vFic.r.position) / KF; }
    float HLF { get { return vFic.r.height / KF; } }
    Rect RFich { get { return new Rect(0, 0, FIC_W, Mathf.Max(100f, HLF)); } }
    Rect RBill(int i) { return new Rect(8 + (i % 3) * 120, 30 + (i / 3) * 84, 114, 64); }
    Rect RTotalCaja { get { return new Rect(128, 198, 234, 64); } }
    Rect RDrop { get { return new Rect(8, 30, 238, 72); } }
    Rect RChipSel(int i) { return new Rect(440 + i * 30, 58, 28, 28); }
    Rect RMuestra { get { return new Rect(FIC_W - 175, 8, 74, 22); } }

    static int Cual(int v, int[] a) { for (int i = 0; i < a.Length; i++) if (a[i] == v) return i; return 0; }
    Vector2 PosFicha(Ficha f) { if (f.placa) return vPla.r.position + f.p * KP; return f.pano ? DePano(PR.position + new Vector2(f.uv.x * PR.width, f.uv.y * PR.height)) : SPF(f.p); }
    Texture2D Tex(int v) { return chips[diseno][Cual(v, CV)]; }
    float ColX(int ci) { return 40f + ci * 72f; }
    float TamFicha(Ficha f) { if (f.placa) return PLW * KP; return f.pano ? (apostarGrande ? CP * 1.3f : CP) : Mathf.Max(chipS * KF, 12f); }

    bool RomperFicha(Ficha f, float lx)
    {
        foreach (var g in pilasRango)
        {
            int v = (int)g.z; if (lx < g.x || lx > g.y || v >= f.v || f.v % v != 0) continue;
            int n = f.v / v; fichas.Remove(f); apuesta.Remove(f); for (int k = 0; k < n; k++) fichas.Add(new Ficha { v = v, p = Vector2.zero });
            Sfx(SFX_SUMAR); Aviso("Cambiaste una ficha de $" + f.v + " por " + n + " de $" + v); Ordenar(); return true;
        }
        return false;
    }
    void Ordenar() { OrdenarFichas(); OrdenarPlacas(); GuardarMesa(); }

    // ---- lo que tenes en la mesa se guarda solo (si el juego se cierra de golpe, no se pierde ni se duplica plata)
    static string F2(float x) { return x.ToString("0.####", System.Globalization.CultureInfo.InvariantCulture); }
    void GuardarMesa()
    {
        var sb = new System.Text.StringBuilder();
        foreach (var f in fichas) sb.Append(f.v).Append(',').Append(f.placa ? 1 : 0).Append(',').Append(f.pano ? 1 : 0).Append(',').Append(F2(f.uv.x)).Append(',').Append(F2(f.uv.y)).Append(',').Append(f.ganadora ? 1 : 0).Append(';');
        PlayerPrefs.SetString("rul11_f", sb.ToString());
        PlayerPrefs.SetString("rul11_b", string.Join(",", vent.ConvertAll(x => x.ToString()).ToArray()));
        PlayerPrefs.SetString("rul11_p", string.Join(",", ventPla.ConvertAll(x => x.ToString()).ToArray()));
        PlayerPrefs.SetString("rul11_s", string.Join(",", System.Array.ConvertAll(stock, x => x.ToString())));
        PlayerPrefs.Save();
    }
    void CargarMesa()
    {
        var ci = System.Globalization.CultureInfo.InvariantCulture;
        foreach (var it in PlayerPrefs.GetString("rul11_f", "").Split(';'))
        {
            var q = it.Split(','); if (q.Length < 6) continue; int v; if (!int.TryParse(q[0], out v) || v <= 0) continue;
            float ux, uy; float.TryParse(q[3], System.Globalization.NumberStyles.Float, ci, out ux); float.TryParse(q[4], System.Globalization.NumberStyles.Float, ci, out uy);
            var f = new Ficha { v = v, placa = q[1] == "1", pano = q[2] == "1", uv = new Vector2(ux, uy), ganadora = q[5] == "1" }; fichas.Add(f); if (f.pano) apuesta.Add(f);
        }
        foreach (var s in PlayerPrefs.GetString("rul11_b", "").Split(',')) { int v; if (int.TryParse(s, out v) && v > 0) vent.Add(v); }
        foreach (var s in PlayerPrefs.GetString("rul11_p", "").Split(',')) { int v; if (int.TryParse(s, out v) && v > 0) ventPla.Add(v); }
        { var ps = PlayerPrefs.GetString("rul11_s", "").Split(','); for (int i = 0; i < ps.Length && i < stock.Length; i++) { int q; if (int.TryParse(ps[i], out q) && q > 0) stock[i] = q; } }
        if (PlayerPrefs.GetInt("rul12_regalo", 0) == 0) { for (int i = 0; i < BV.Length; i++) stock[i]++; PlayerPrefs.SetInt("rul12_regalo", 1); }   // regalo de bienvenida: un billete de cada uno en tu caja
#if UNITY_WEBGL   // plata de regalo solo en la ruleta web de prueba; en el simulador la plata es la del juego
        if (PlayerPrefs.GetInt("rul19_mix", 0) >= 1 && PlayerPrefs.GetInt("rul21_mas20k", 0) < 1)   // 21/11: otros 20.000 de prueba (fichas y billetes) sin tocar la plata del juego
        {
            foreach (int v in new[] { 1, 5, 10, 25, 50, 100, 500 }) for (int k = 0; k < 10; k++) fichas.Add(new Ficha { v = v, p = Vector2.zero });
            foreach (var kv in new[] { new[] { 1000, 8 }, new[] { 100, 30 }, new[] { 50, 20 }, new[] { 20, 25 }, new[] { 10, 30 }, new[] { 5, 38 }, new[] { 1, 100 } }) stock[Cual(kv[0], BV)] += kv[1];
            PlayerPrefs.SetInt("rul21_mas20k", 1); PlayerPrefs.Save(); GuardarMesa();
        }
        if (PlayerPrefs.GetInt("rul19_mix", 0) < 1)   // arranque: 20.000 = 10 fichas de cada valor (1 a 500) + el resto en billetes de todas las denominaciones
        {
            fichas.Clear(); apuesta.Clear(); vent.Clear(); ventPla.Clear(); System.Array.Clear(stock, 0, stock.Length);
            foreach (int v in new[] { 1, 5, 10, 25, 50, 100, 500 }) for (int k = 0; k < 10; k++) fichas.Add(new Ficha { v = v, p = Vector2.zero });
            foreach (var kv in new[] { new[] { 1000, 8 }, new[] { 100, 30 }, new[] { 50, 20 }, new[] { 20, 25 }, new[] { 10, 30 }, new[] { 5, 38 }, new[] { 1, 100 } }) stock[Cual(kv[0], BV)] += kv[1];
            PlayerPrefs.SetInt("rul19_mix", 1); PlayerPrefs.SetInt("rul21_mas20k", 1); PlayerPrefs.SetInt("rul18_20kb", 1); PlayerPrefs.SetInt("rul17_20k", 1); PlayerPrefs.Save(); GuardarMesa();
        }
        if (PlayerPrefs.GetInt("rul18_20kb", 0) < 1)   // plata de prueba: 20.000 en BILLETES de todas las denominaciones (las fichas las elige uno en la boleteria)
        {
            fichas.Clear(); apuesta.Clear(); vent.Clear(); ventPla.Clear(); System.Array.Clear(stock, 0, stock.Length);
            foreach (var kv in new[] { new[] { 1000, 10 }, new[] { 100, 40 }, new[] { 50, 40 }, new[] { 20, 50 }, new[] { 10, 100 }, new[] { 5, 200 }, new[] { 1, 1000 } }) stock[Cual(kv[0], BV)] += kv[1];
            PlayerPrefs.SetInt("rul18_20kb", 1); PlayerPrefs.SetInt("rul17_20k", 1); PlayerPrefs.Save(); GuardarMesa();
        }
#endif
        ordenPend = true;
    }
    int TotalEnMesa() { int t = 0; foreach (var f in fichas) t += f.v; foreach (int b in vent) t += b; foreach (int b in ventPla) t += b; for (int i = 0; i < BV.Length; i++) t += stock[i] * BV[i]; return t; }
    void Ingresar(int monto)
    {
        if (monto <= 0) { ingMsg = "Poné un monto."; return; }
        if (monto % 100 != 0) { ingMsg = "Ese monto no se arma con billetes: redondealo a múltiplos de $100."; return; }
        if (monto > TOPE_INGRESO) { ingMsg = "El máximo es $" + Fmt(TOPE_INGRESO) + " por ingreso (podés ingresar de nuevo)."; return; }
        if (!modoPrueba && monto > PlataAhora()) { ingMsg = "No tenés tanta plata en tu cuenta: tenés $" + Fmt(PlataAhora()) + "."; return; }
        if (!modoPrueba) PlataMover(-monto);
        int resto = monto; int[] den = { 1000, 100, 50, 20, 10, 5, 1 };
        foreach (int d in den) { int n = resto / d; resto -= n * d; stock[Cual(d, BV)] += n; }
        modal = 0; ingTxt = ""; ingMsg = ""; GuardarMesa(); Aviso("Ingresaste $" + Fmt(monto) + ": ya están en tu caja de billetes");
    }
    static Texture2D OroTex(float k)
    {
        const int N = 32; var t = new Texture2D(1, N, TextureFormat.RGBA32, false); t.wrapMode = TextureWrapMode.Clamp;
        for (int y = 0; y < N; y++) { float u = y / (N - 1f); Color c = u < 0.5f ? Color.Lerp(new Color(0.62f, 0.4f, 0.07f), new Color(1f, 0.9f, 0.5f), u * 2f) : Color.Lerp(new Color(1f, 0.9f, 0.5f), new Color(0.95f, 0.72f, 0.22f), (u - 0.5f) * 2f); t.SetPixel(0, y, new Color(Mathf.Min(1f, c.r * k), Mathf.Min(1f, c.g * k), Mathf.Min(1f, c.b * k), 1f)); }
        t.Apply(); return t;
    }
    GUIStyle sTirar;
    void BotonTirar(Rect r, bool lanzada) { if (BotonOro(r, lanzada ? "..." : "TIRAR") && !lanzada) tirarPedido = true; }   // boton dorado para tirar la bolita sin salir del paño
    bool BotonOro(Rect r, string txt)
    {
        if (sTirar == null) { var a = OroTex(1f); var h = OroTex(1.12f); sTirar = new GUIStyle(GUI.skin.button) { fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter, clipping = TextClipping.Clip, border = new RectOffset(0, 0, 0, 0) }; sTirar.normal.background = a; sTirar.hover.background = h; sTirar.active.background = a; sTirar.focused.background = a; sTirar.normal.textColor = sTirar.hover.textColor = sTirar.active.textColor = sTirar.focused.textColor = new Color(0.22f, 0.1f, 0f); }
        sTirar.fontSize = Mathf.Max(8, Mathf.RoundToInt(Mathf.Min(r.height * 0.5f, r.width * 0.26f)));
        Fondo(new Rect(r.x - 2, r.y - 2, r.width + 4, r.height + 4), new Color(0.25f, 0.13f, 0.02f, 0.95f));
        return GUI.Button(r, txt, sTirar);
    }
    void Retirarse()
    {
        int t = TotalEnMesa();
        if (modoPrueba)   // en prueba la plata queda guardada y vuelve como billetes (las fichas y placas se pasan a billetes)
        {
            int deFichas = 0; foreach (var q in fichas) deFichas += q.v; foreach (int q in vent) stock[Cual(q, BV)]++; foreach (int q in ventPla) deFichas += q;
            int resto = deFichas; int[] den = { 1000, 100, 50, 20, 10, 5, 1 }; foreach (int d in den) { int n = resto / d; resto -= n * d; stock[Cual(d, BV)] += n; }
            fichas.Clear(); apuesta.Clear(); vent.Clear(); ventPla.Clear(); ultima.Clear(); estampa = 0; badgeF = null;
        }
        else { fichas.Clear(); apuesta.Clear(); vent.Clear(); ventPla.Clear(); System.Array.Clear(stock, 0, stock.Length); ultima.Clear(); estampa = 0; badgeF = null; if (t > 0) PlataMover(t); }
        GuardarMesa(); modal = 0; cerrarPedido = true;
    }
    void DibujarIngreso(GUIStyle sT, GUIStyle sB0)
    {
        GUI.enabled = true;
        float W = Mathf.Min(SW - 40f, 620f), H = 330f; var rm = new Rect((SW - W) / 2f, Mathf.Max(10f, (SH - H) / 2f), W, H);
        Fondo(new Rect(0, 0, SW, SH), new Color(0f, 0f, 0f, 0.6f)); Fondo(rm, new Color(0.07f, 0.2f, 0.12f, 0.98f));
        GUI.Label(new Rect(rm.x + 16, rm.y + 8, W - 32, 40), "INGRESAR DINERO", new GUIStyle(sT) { fontSize = 28 });
        GUI.Label(new Rect(rm.x + 16, rm.y + 50, W - 32, 26), "Tu plata: $" + Fmt(PlataAhora()) + "     (máximo $" + Fmt(TOPE_INGRESO) + " por ingreso)" + (modoPrueba ? "   MODO PRUEBA: no se descuenta" : ""), new GUIStyle(sT) { fontSize = 16 });
        int[] op = { 1000, 2000, 5000, 10000 }; float bw = (W - 32f - 3 * 8f) / 4f; var sb = new GUIStyle(sB0) { fontSize = 20 };
        for (int i = 0; i < 4; i++) if (GUI.Button(new Rect(rm.x + 16 + i * (bw + 8f), rm.y + 84, bw, 52), "$" + Fmt(op[i]), sb)) { Ingresar(op[i]); return; }
        GUI.Label(new Rect(rm.x + 16, rm.y + 150, 140, 36), "Otro monto:", new GUIStyle(sT) { fontSize = 18 });
        var st = new GUIStyle(GUI.skin.textField) { fontSize = 22, alignment = TextAnchor.MiddleLeft };
        string nuevo = GUI.TextField(new Rect(rm.x + 150, rm.y + 146, 200, 40), ingTxt, 6, st); var dig = new System.Text.StringBuilder(); foreach (char c in nuevo) if (c >= '0' && c <= '9') dig.Append(c); ingTxt = dig.ToString();
        if (GUI.Button(new Rect(rm.x + 362, rm.y + 146, W - 378, 40), "INGRESAR", sb)) { int m; if (int.TryParse(ingTxt, out m)) { Ingresar(m); return; } else ingMsg = "Escribí un monto."; }
        if (ingMsg != "") { var sm = new GUIStyle(sT) { fontSize = 15, wordWrap = true }; sm.normal.textColor = new Color(1f, 0.75f, 0.5f); GUI.Label(new Rect(rm.x + 16, rm.y + 196, W - 32, 60), ingMsg, sm); }
        if (GUI.Button(new Rect(rm.x + 16, rm.yMax - 56, 200, 40), "CANCELAR", new GUIStyle(sB0) { fontSize = 18 })) { modal = 0; ingTxt = ""; ingMsg = ""; }
    }
    void DibujarRetiro(GUIStyle sT, GUIStyle sB0)
    {
        GUI.enabled = true;
        int tf = 0, tb = 0; foreach (var f in fichas) tf += f.v; foreach (int b in vent) tb += b; foreach (int b in ventPla) tb += b; for (int i = 0; i < BV.Length; i++) tb += stock[i] * BV[i];
        float W = Mathf.Min(SW - 40f, 620f), H = 290f; var rm = new Rect((SW - W) / 2f, Mathf.Max(10f, (SH - H) / 2f), W, H);
        Fondo(new Rect(0, 0, SW, SH), new Color(0f, 0f, 0f, 0.6f)); Fondo(rm, new Color(0.07f, 0.2f, 0.12f, 0.98f));
        GUI.Label(new Rect(rm.x + 16, rm.y + 8, W - 32, 40), "RETIRARSE", new GUIStyle(sT) { fontSize = 28 });
        var s2 = new GUIStyle(sT) { fontSize = 17 };
        GUI.Label(new Rect(rm.x + 16, rm.y + 56, W - 32, 26), "Fichas y placas (incluye lo apostado): $" + Fmt(tf), s2);
        GUI.Label(new Rect(rm.x + 16, rm.y + 84, W - 32, 26), "Billetes y placas (caja y ventanilla): $" + Fmt(tb), s2);
        var s3 = new GUIStyle(sT) { fontSize = 24 }; s3.normal.textColor = new Color(1f, 0.88f, 0.4f);
        GUI.Label(new Rect(rm.x + 16, rm.y + 122, W - 32, 36), modoPrueba ? "MODO PRUEBA: no se suma a tu cuenta" : "Vuelve a tu cuenta: $" + Fmt(tf + tb), s3);
        GUI.Label(new Rect(rm.x + 16, rm.y + 164, W - 32, 26), "Después podés volver a entrar y a ingresar de nuevo.", new GUIStyle(sT) { fontSize = 14 });
        var sb = new GUIStyle(sB0) { fontSize = 18 };
        if (GUI.Button(new Rect(rm.x + 16, rm.yMax - 62, W * 0.58f, 46), "RETIRARME Y CERRAR LA APP", sb)) { Retirarse(); return; }
        if (GUI.Button(new Rect(rm.x + 16 + W * 0.58f + 10f, rm.yMax - 62, W - 32 - W * 0.58f - 10f, 46), "CANCELAR", sb)) modal = 0;
    }
    int ContarEn(Ficha f)   // cuantas fichas llevas puestas en la misma apuesta que esta
    {
        var ap = Cubre(f.uv); int n = 0; float R = CP * 0.9f;
        foreach (var g in fichas)
        {
            if (!g.pano) continue;
            if (ap != null) { var a2 = Cubre(g.uv); if (a2 != null && a2.nombre == ap.nombre) n++; }
            else { var d = new Vector2((g.uv.x - f.uv.x) * PR.width, (g.uv.y - f.uv.y) * PR.height); if (d.magnitude < R) n++; }
        }
        return n;
    }
    void Badge(Ficha f) { int n = ContarEn(f); if (n >= 2) { badgeF = f; badgeN = n; tBadge = Time.realtimeSinceStartup; } }
    void OrdenarPlacas()
    {
        var cnt = new int[PL.Length];
        foreach (var f in fichas) if (f.placa && !f.pano) { int i = System.Array.IndexOf(PL, f.v); if (i < 0) i = 0; int k = cnt[i]++; f.p = new Vector2(52f + i * 100f, 104f - Mathf.Min(k, 12) * 5f); }
    }
    // ---- fichas paradas en pilas, vistas en perspectiva: arriba se ve el valor, al costado el canto de cada ficha
    const int C3W = 128, C3RY = 32, C3T = 10, C3H = 76, C3TOP = 33;   // tamaño del dibujo, radio vertical de la elipse, grosor, alto total y centro de la cara de arriba
    const int H_PILA = 16;   // fichas por columna: si hay mas, se arma otra columna al lado
    Dictionary<int, Texture2D> c3 = new Dictionary<int, Texture2D>(); float chipS = CS;
    List<Vector3> pilasCol = new List<Vector3>(); List<Vector2> pilasGrupo = new List<Vector2>(); List<Vector3> pilasRango = new List<Vector3>();   // pilasRango: (x desde, x hasta, valor) de cada grupo, para romper fichas   // columnas (x, y base, cantidad) y grupos por valor (x centro, cantidad)
    Texture2D Chip3D(int v)
    {
        int key = diseno * 100 + Cual(v, CV); Texture2D res; if (c3.TryGetValue(key, out res) && res != null) return res;
        var src = Tex(v); if (src == null) return null;
        const int N = 256; Color[] P;
        if (src.isReadable) { P = new Color[N * N]; for (int y = 0; y < N; y++) for (int x = 0; x < N; x++) P[y * N + x] = src.GetPixelBilinear((x + 0.5f) / N, (y + 0.5f) / N); }
        else
        {
            var rt = RenderTexture.GetTemporary(N, N, 0); var prev = RenderTexture.active; Graphics.Blit(src, rt); RenderTexture.active = rt;
            var rd = new Texture2D(N, N, TextureFormat.RGBA32, false); rd.ReadPixels(new Rect(0, 0, N, N), 0, 0); RenderTexture.active = prev; RenderTexture.ReleaseTemporary(rt);
            P = rd.GetPixels(); UnityEngine.Object.Destroy(rd);
        }
        System.Func<float, float, Color> S = (u, vt) => { float x = Mathf.Clamp01(u) * (N - 1), y = (1f - Mathf.Clamp01(vt)) * (N - 1); int x0 = (int)x, y0 = (int)y, x1 = Mathf.Min(N - 1, x0 + 1), y1 = Mathf.Min(N - 1, y0 + 1); float fx = x - x0, fy = y - y0; return Color.Lerp(Color.Lerp(P[y0 * N + x0], P[y0 * N + x1], fx), Color.Lerp(P[y1 * N + x0], P[y1 * N + x1], fx), fy); };
        float rx = C3W / 2f - 1f, cx = C3W / 2f, topC = C3TOP, botC = C3TOP + C3T, ry = C3RY;
        var o = new Color32[C3W * C3H];
        for (int py = 0; py < C3H; py++) for (int px = 0; px < C3W; px++)
        {
            float sr = 0f, sg = 0f, sb = 0f, sa = 0f;
            for (int i = 0; i < 3; i++) for (int j = 0; j < 3; j++)
            {
                float sx = px + (i + 0.5f) / 3f, sy = py + (j + 0.5f) / 3f, dx = (sx - cx) / rx, dT = (sy - topC) / ry, dB = (sy - botC) / ry;
                if (dx * dx + dT * dT <= 1f) { var c = S(0.5f + 0.5f * dx, 0.5f + 0.5f * dT); sr += c.r * c.a; sg += c.g * c.a; sb += c.b * c.a; sa += c.a; }
                else if (dx * dx + dB * dB <= 1f && sy > topC)
                {
                    float q = Mathf.Sqrt(Mathf.Max(0f, 1f - dx * dx)); var c = S(0.5f + 0.5f * dx * 0.93f, 0.5f + 0.5f * q * 0.93f); if (c.a < 0.5f) c = new Color(0.22f, 0.22f, 0.25f, 1f);
                    float sh = Mathf.Lerp(0.5f, 0.86f, 1f - Mathf.Abs(dx)); sr += c.r * sh; sg += c.g * sh; sb += c.b * sh; sa += 1f;
                }
            }
            if (sa > 0.001f) o[(C3H - 1 - py) * C3W + px] = new Color(sr / sa, sg / sa, sb / sa, sa / 9f);
        }
        res = new Texture2D(C3W, C3H, TextureFormat.RGBA32, false); res.wrapMode = TextureWrapMode.Clamp; res.filterMode = FilterMode.Bilinear; res.SetPixels32(o); res.Apply(); c3[key] = res; return res;
    }
    void DibujarFicha3D(Vector2 p, float s, int v)   // p = centro de la cara de arriba
    {
        var t = Chip3D(v); if (t == null) return; float k = s / C3W; GUI.DrawTexture(new Rect(p.x - s / 2f, p.y - C3TOP * k, s, C3H * k), t);
    }
    void OrdenarFichas()
    {
        var ra = RFich; var lista = fichas.FindAll(f => !f.pano && !f.placa); int[] cnt = new int[CV.Length];
        foreach (var f in lista) cnt[Cual(f.v, CV)]++;
        int grupos = 0, cols = 0, maxN = 1; for (int i = 0; i < CV.Length; i++) if (cnt[i] > 0) { grupos++; cols += (cnt[i] + H_PILA - 1) / H_PILA; maxN = Mathf.Max(maxN, Mathf.Min(cnt[i], H_PILA)); }
        float S = Mathf.Clamp((FIC_W - 24f) / (Mathf.Max(1, cols) * 1.04f + 0.3f * grupos), 26f, CS);
        float paso = S * C3T / C3W * 0.9f, disp = ra.height - 34f - 56f - S * 0.5f; if (maxN * paso > disp) paso = Mathf.Max(1.2f, disp / maxN);
        float baseY = ra.yMax - 58f - S * 0.1f; chipS = S; pilasCol.Clear(); pilasGrupo.Clear(); pilasRango.Clear();
        lista.Sort((a, b) => a.v.CompareTo(b.v)); float cur = 12f + S / 2f;
        for (int ci = 0; ci < CV.Length; ci++)
        {
            if (cnt[ci] <= 0) continue; int nc = (cnt[ci] + H_PILA - 1) / H_PILA; var del = lista.FindAll(f => Cual(f.v, CV) == ci);
            for (int k = 0; k < del.Count; k++) del[k].p = new Vector2(cur + (k / H_PILA) * S * 1.04f, baseY - (k % H_PILA) * paso);
            for (int c = 0; c < nc; c++) pilasCol.Add(new Vector3(cur + c * S * 1.04f, baseY, Mathf.Min(H_PILA, cnt[ci] - c * H_PILA)));
            pilasGrupo.Add(new Vector2(cur + (nc - 1) * S * 1.04f / 2f, cnt[ci])); pilasRango.Add(new Vector3(cur - S * 0.6f, cur + (nc - 1) * S * 1.04f + S * 0.6f, CV[ci])); cur += nc * S * 1.04f + S * 0.3f;
        }
    }
    void DibujarPilas(GUIStyle fch)
    {
        foreach (var c in pilasCol) { var p = SPF(new Vector2(c.x, c.y)); float s = chipS * KF; var gc = GUI.color; GUI.color = new Color(0f, 0f, 0f, 0.38f); GUI.DrawTexture(new Rect(p.x - s * 0.56f, p.y - s * 0.02f, s * 1.12f, s * 0.46f), Disco()); GUI.color = gc; }   // sombra en la mesa
        var tl = fichas.FindAll(f => !f.pano && !f.placa && f != dragF); tl.Sort((a, b) => b.p.y.CompareTo(a.p.y)); foreach (var f in tl) Ficha1(f);
        foreach (var g in pilasGrupo) GUI.Label(SRF(new Rect(g.x - 30, RFich.yMax - 46, 60, 22)), "x" + (int)g.y, fch);
    }

    void Cambiar()
    {
        int total = 0; foreach (int b in vent) total += b; foreach (int b in ventPla) total += b; if (total <= 0) { Aviso("Primero poné billetes o placas en la ventanilla"); return; }
        cambioPend = total;   // se abre el cartel con las opciones de fichas
    }
    List<KeyValuePair<int, bool>> Greedy(int total, int idx)
    {
        var l = new List<KeyValuePair<int, bool>>(); int resto = total;
        for (int i = idx; i >= 0; i--) { int n = resto / CV[i]; resto -= n * CV[i]; for (int k = 0; k < n; k++) l.Add(new KeyValuePair<int, bool>(CV[i], false)); }
        return l;
    }
    void CambiarCon(int idx)
    {
        foreach (var kv in Greedy(cambioPend, idx)) fichas.Add(new Ficha { v = kv.Key, p = Vector2.zero });
        vent.Clear(); ventPla.Clear(); maxFicha = idx; int t = cambioPend; cambioPend = 0; Ordenar(); Aviso("Cambiaste $" + Fmt(t) + " por fichas");
    }
    void CambiarPuro(int idx)   // todo el cambio en fichas de una sola denominacion
    {
        int n = cambioPend / CV[idx]; for (int k = 0; k < n; k++) fichas.Add(new Ficha { v = CV[idx], p = Vector2.zero });
        vent.Clear(); ventPla.Clear(); maxFicha = idx; int t = cambioPend; cambioPend = 0; Ordenar(); Aviso("Cambiaste $" + Fmt(t) + " por fichas");
    }
    void DibujarCambio(GUIStyle sT, GUIStyle sB0)
    {
        var ops = new List<int>(); var txt = new List<string>();
        for (int i = CV.Length - 1; i >= 0; i--) { if (CV[i] > cambioPend) continue; var l = Greedy(cambioPend, i); if (l.Count > 300) continue; string t = Resumen(l); if (txt.Contains(t)) continue; ops.Add(i); txt.Add(t); }
        for (int i = CV.Length - 1; i >= 0; i--) { if (CV[i] > cambioPend || cambioPend % CV[i] != 0 || cambioPend / CV[i] > 300) continue; int n = cambioPend / CV[i]; string t = n + (n == 1 ? " ficha de $" : " fichas de $") + CV[i]; if (txt.Contains(t)) continue; ops.Add(i + 100); txt.Add(t); }   // tambien todo en fichas de una sola denominacion (ej: $1000 en fichas de $10)
        float W = Mathf.Min(SW - 40f, 760f), fh = Mathf.Clamp((SH - 230f) / Mathf.Max(1, ops.Count), 30f, 52f), H = 120f + ops.Count * fh + 56f; var rm = new Rect((SW - W) / 2f, Mathf.Max(6f, (SH - H) / 2f), W, H);
        Fondo(new Rect(0, 0, SW, SH), new Color(0f, 0f, 0f, 0.6f)); Fondo(rm, new Color(0.07f, 0.2f, 0.12f, 0.98f));
        GUI.Label(new Rect(rm.x + 16, rm.y + 10, W - 32, 40), "Cambiar $" + Fmt(cambioPend) + " por fichas", new GUIStyle(sT) { fontSize = 30 });
        { var comp = new SortedDictionary<int, int>(); foreach (int b in vent) { comp.TryGetValue(b, out int c0); comp[b] = c0 + 1; } foreach (int b in ventPla) { comp.TryGetValue(b, out int c1); comp[b] = c1 + 1; } string ct = ""; foreach (var kv in comp) ct += (ct == "" ? "" : "  +  ") + kv.Value + " de $" + kv.Key;
          GUI.Label(new Rect(rm.x + 16, rm.y + 56, W - 32, 28), "En la ventanilla pusiste: " + ct + "   ¿Cómo las querés?", new GUIStyle(sT) { fontSize = 16 }); }
        var sb = new GUIStyle(sB0) { fontSize = fh < 40f ? 17 : 22 };
        for (int k = 0; k < ops.Count; k++) if (GUI.Button(new Rect(rm.x + 20, rm.y + 92 + k * fh, W - 40, fh - 6f), txt[k], sb)) { if (ops[k] >= 100) CambiarPuro(ops[k] - 100); else CambiarCon(ops[k]); return; }
        if (GUI.Button(new Rect(rm.x + 20, rm.y + 98 + ops.Count * fh, 200, 40), "CANCELAR", new GUIStyle(sB0) { fontSize = 18 })) cambioPend = 0;
    }
    void Vaciar() { if (vent.Count > 0) Aviso("Los billetes volvieron a tu caja"); foreach (int b in vent) stock[Cual(b, BV)]++; vent.Clear(); foreach (int v in ventPla) fichas.Add(new Ficha { v = v, placa = System.Array.IndexOf(PL, v) >= 0 }); ventPla.Clear(); Ordenar(); }
    void Aviso(string s) { aviso = s; tAviso = Time.realtimeSinceStartup; }

    // ---------------------------------------------------------------- marcas de los plenos
    Vector2 UvPano(float px, float py) { return new Vector2((309f - py) / 276f, px / 600f); }   // punto de la imagen del paño (600x342) -> lugar en la ventana ya girada y recortada
    void AutoMarcas()
    {
        for (int n = 1; n <= 36; n++) { int k = (n - 1) / 3, f = n % 3; float py = f == 1 ? 188f : f == 2 ? 127f : 66f; marcas[n].uv = UvPano(101f + 38.8f * k, py); marcas[n].puesta = true; }
        marcas[0].uv = UvPano(50f, 127f); marcas[0].puesta = true;
        float[,] ch = { { 508f, 284f }, { 431f, 284f }, { 508f, 284f }, { 120f, 284f }, { 159f, 240f }, { 314f, 240f }, { 470f, 240f }, { 275f, 284f }, { 353f, 284f }, { 565f, 188f }, { 565f, 127f }, { 565f, 66f } };
        ch[0, 0] = 197f; ch[0, 1] = 284f; ch[1, 0] = 431f; ch[2, 0] = 508f;   // par = EVEN, impar = ODD, mayor = 19to36, menor = 1to18
        for (int i = 0; i < 12; i++) { marcas[37 + i].uv = UvPano(ch[i, 0], ch[i, 1]); marcas[37 + i].puesta = true; }
        for (int cl = 0; cl < 12; cl++) { marcas[49 + cl].uv = UvPano(101f + 38.8f * cl, 224f); marcas[49 + cl].puesta = true; marcas[61 + cl].uv = UvPano(101f + 38.8f * cl, 36f); marcas[61 + cl].puesta = true; }
        mapeado = true;
    }
    void CargarMarcas()
    {
        bool legado = PlayerPrefs.GetInt("rul8_ok", 0) == 1; int pn = 0;
        for (int n = 0; n < 37; n++)
            if (PlayerPrefs.GetInt("rul8_" + n + "p", legado ? 1 : 0) == 1) { marcas[n].uv = new Vector2(PlayerPrefs.GetFloat("rul8_" + n + "x"), PlayerPrefs.GetFloat("rul8_" + n + "y")); marcas[n].puesta = true; pn++; }
        mapeado = pn == 37;
        for (int k = 0; k < 36; k++) if (PlayerPrefs.GetInt("rul9_c" + k + "p", 0) == 1) { marcas[37 + k].uv = new Vector2(PlayerPrefs.GetFloat("rul9_c" + k + "x"), PlayerPrefs.GetFloat("rul9_c" + k + "y")); marcas[37 + k].puesta = true; }
    }
    void GuardarChances()   // todas las marcas (numeros, chances y calles) se guardan solas cada vez que se sueltan
    {
        int pn = 0;
        for (int n = 0; n < 37; n++) { var m = marcas[n]; PlayerPrefs.SetInt("rul8_" + n + "p", m.puesta ? 1 : 0); PlayerPrefs.SetFloat("rul8_" + n + "x", m.uv.x); PlayerPrefs.SetFloat("rul8_" + n + "y", m.uv.y); if (m.puesta) pn++; }
        PlayerPrefs.SetInt("rul8_ok", pn == 37 ? 1 : 0); mapeado = pn == 37;
        for (int k = 0; k < 36; k++) { var m = marcas[37 + k]; PlayerPrefs.SetInt("rul9_c" + k + "p", m.puesta ? 1 : 0); PlayerPrefs.SetFloat("rul9_c" + k + "x", m.uv.x); PlayerPrefs.SetFloat("rul9_c" + k + "y", m.uv.y); }
        PlayerPrefs.Save();
    }
    void GuardarMarcas() { GuardarChances(); }
    void CargarMarcasSiHace()   // al salir sin clavar: se vuelve a lo guardado
    {
        foreach (var m in marcas) m.puesta = false; dragM = -1;
        CargarMarcas();
    }
    // numero del pleno donde cae un punto del paño (-1 si no esta sobre ninguno o aun no se marcaron)
    class Apu { public string nombre; public int pago; public int[] nums; public bool Gana(int r) { return System.Array.IndexOf(nums, r) >= 0; } }
    static Apu Mk(string nombre, int pago, params int[] nums) { return new Apu { nombre = nombre, pago = pago, nums = nums }; }
    static bool Rojo(int n) { return System.Array.IndexOf(new[] { 1, 3, 5, 7, 9, 12, 14, 16, 18, 19, 21, 23, 25, 27, 30, 32, 34, 36 }, n) >= 0; }
    static int[] Nums(System.Func<int, bool> f) { var l = new List<int>(); for (int n = 1; n <= 36; n++) if (f(n)) l.Add(n); return l.ToArray(); }
    // las 37 marcas de los numeros definen la grilla del paño (ajuste por minimos cuadrados): de ahi salen solos semiplenos, cuadros y trios con el cero
    bool Ajuste(out Vector2 a, out Vector2 ec, out Vector2 er, out float rz)
    {
        a = ec = er = Vector2.zero; rz = -1f; if (!mapeado) return false; var R = PR;
        Vector2 sum = Vector2.zero, sc = Vector2.zero, sr = Vector2.zero; float qc = 0f, qr = 0f;
        for (int n = 1; n <= 36; n++) { int r = (n - 1) / 3, c = (n - 1) % 3; var p = new Vector2(marcas[n].uv.x * R.width, marcas[n].uv.y * R.height); sum += p; sc += (c - 1f) * p; qc += (c - 1f) * (c - 1f); sr += (r - 5.5f) * p; qr += (r - 5.5f) * (r - 5.5f); }
        a = sum / 36f; ec = sc / qc; er = sr / qr;
        float det = ec.x * er.y - ec.y * er.x; if (Mathf.Abs(det) < 1e-3f) return false;
        var d0 = new Vector2(marcas[0].uv.x * R.width, marcas[0].uv.y * R.height) - a; rz = (ec.x * d0.y - ec.y * d0.x) / det + 5.5f; return true;
    }
    Apu Cubre(Vector2 uv)   // que apuesta es una ficha puesta en este punto del paño (null si no esta sobre nada)
    {
        if (!Ajuste(out var a, out var ec, out var er, out var rz)) return null;
        var R = PR; var d = new Vector2(uv.x * R.width, uv.y * R.height) - a; float det = ec.x * er.y - ec.y * er.x;
        float c = (d.x * er.y - d.y * er.x) / det + 1f, r = (ec.x * d.y - ec.y * d.x) / det + 5.5f; const float B = 0.3f;
        if (c >= -B && c <= 2f + B && r >= rz - 0.5f && r <= 11f + B)
        {
            float rb = rz / 2f;
            if (r < rb - 0.2f) return Mk("el 0 (pleno)", 35, 0);
            if (r < rb + 0.2f)
            {
                int cz = Mathf.Clamp(Mathf.RoundToInt(c), 0, 2); float dz = c - cz;
                if (Mathf.Abs(dz) <= B) return Mk("0 y " + (cz + 1) + " (semipleno)", 17, 0, cz + 1);
                int lo = dz > 0 ? cz : cz - 1; lo = Mathf.Clamp(lo, 0, 1); return Mk("0, " + (lo + 1) + " y " + (lo + 2) + " (trío)", 11, 0, lo + 1, lo + 2);
            }
            int rr = Mathf.Clamp(Mathf.RoundToInt(r), 0, 11), cc = Mathf.Clamp(Mathf.RoundToInt(c), 0, 2); float dr = r - rr, dc = c - cc;
            int sr = (dr > B && rr < 11) ? 1 : (dr < -B && rr > 0) ? -1 : 0, sc = (dc > B && cc < 2) ? 1 : (dc < -B && cc > 0) ? -1 : 0;
            int n = 3 * rr + cc + 1;
            if (sr == 0 && sc == 0) return Mk("el " + n + " (pleno)", 35, n);
            if (sc == 0) return Mk(n + " y " + (n + 3 * sr) + " (semipleno)", 17, n, n + 3 * sr);
            if (sr == 0) return Mk(n + " y " + (n + sc) + " (semipleno)", 17, n, n + sc);
            return Mk(n + ", " + (n + sc) + ", " + (n + 3 * sr) + " y " + (n + 3 * sr + sc) + " (cuadro)", 8, n, n + sc, n + 3 * sr, n + 3 * sr + sc);
        }
        // fuera de la grilla: chances y calles, por la marca mas cercana
        float u = 0.045f * (R.width + R.height) * 2.2f, ul = 0.7f * Mathf.Max(ec.magnitude, er.magnitude), best = 1f; int bz = -1;
        for (int z = 37; z < marcas.Length; z++)
        {
            var m = marcas[z]; if (!m.puesta) continue; float dx = (m.uv.x - uv.x) * R.width, dy = (m.uv.y - uv.y) * R.height, q = Mathf.Sqrt(dx * dx + dy * dy) / (z >= 49 ? ul : u);
            if (q < best) { best = q; bz = z; }
        }
        if (bz < 0) return null; int k = bz - 37;
        switch (k)
        {
            case 0: return Mk(NCHL[0], 1, Nums(n => n % 2 == 0));
            case 1: return Mk(NCHL[1], 1, Nums(n => n % 2 == 1));
            case 2: return Mk(NCHL[2], 1, Nums(n => n >= 19));
            case 3: return Mk(NCHL[3], 1, Nums(n => n <= 18));
            case 4: return Mk(NCHL[4], 2, Nums(n => n <= 12));
            case 5: return Mk(NCHL[5], 2, Nums(n => n >= 13 && n <= 24));
            case 6: return Mk(NCHL[6], 2, Nums(n => n >= 25));
            case 7: return Mk(NCHL[7], 1, Nums(n => Rojo(n)));
            case 8: return Mk(NCHL[8], 1, Nums(n => !Rojo(n)));
            case 9: return Mk(NCHL[9], 2, Nums(n => n % 3 == 1));
            case 10: return Mk(NCHL[10], 2, Nums(n => n % 3 == 2));
            case 11: return Mk(NCHL[11], 2, Nums(n => n % 3 == 0));
            default: { int cl = (k - 12) % 12; return Mk("calle " + (3 * cl + 1) + "-" + (3 * cl + 3), 11, 3 * cl + 1, 3 * cl + 2, 3 * cl + 3); }
        }
    }
    Texture2D Disco()
    {
        if (disco != null) return disco; int S = 96; disco = new Texture2D(S, S, TextureFormat.RGBA32, false); disco.wrapMode = TextureWrapMode.Clamp;
        for (int y = 0; y < S; y++) for (int x = 0; x < S; x++) { float d = Mathf.Sqrt((x - S / 2f + 0.5f) * (x - S / 2f + 0.5f) + (y - S / 2f + 0.5f) * (y - S / 2f + 0.5f)); float a = Mathf.Clamp01(S / 2f - d); float g = d > S / 2f - 5f ? 0.3f : 1f; disco.SetPixel(x, y, new Color(g, g, g, a)); }
        disco.Apply(); return disco;
    }
    Rect RMarcaTray(int n)   // solo se acomodan las que faltan poner, en una grilla que siempre entra en la ventana
    {
        var r = vFic.r; int idx = 0, tot = 0; foreach (var m in marcas) { if (m.n == n) idx = tot; if (!marcas[m.n].puesta || m.n == dragM) tot++; }
        idx = 0; int c0 = 0; for (int k = 0; k < marcas.Length; k++) { if (k == n) { idx = c0; break; } if (!marcas[k].puesta) c0++; }
        tot = Mathf.Max(tot, 1); float aw = r.width - 16f, ah = Mathf.Max(40f, r.height - 118f), best = 0f; int bc = 1;
        for (int c = 1; c <= 16; c++) { int rows = Mathf.CeilToInt(tot / (float)c); float sz = Mathf.Min(aw / c, ah / rows, 62f); if (sz > best) { best = sz; bc = c; } }
        float cell = best, s = Mathf.Max(14f, cell - 4f);
        return new Rect(r.x + 8f + (idx % bc) * cell + (cell - s) / 2f, r.y + 112f + (idx / bc) * cell + (cell - s) / 2f, s, s);
    }
    Rect RMarcaPano(Marca m) { float s = 36f; var p = DePano(PR.position + new Vector2(m.uv.x * PR.width, m.uv.y * PR.height)); return new Rect(p.x - s / 2f, p.y - s / 2f, s, s); }
    void DibujarMarca(Rect r, int n, GUIStyle sT)
    {
        var gc = GUI.color; if (n >= 37) GUI.color = n == 44 ? new Color(0.9f, 0.1f, 0.1f) : n == 45 ? new Color(0.12f, 0.12f, 0.12f) : n >= 49 ? new Color(0.55f, 0.8f, 1f) : new Color(1f, 0.9f, 0.45f); GUI.DrawTexture(r, Disco()); GUI.color = gc;
        var st = new GUIStyle(sT) { fontSize = Mathf.Max(8, Mathf.RoundToInt(r.height * (n < 37 ? 0.45f : 0.27f))), alignment = TextAnchor.MiddleCenter }; st.normal.textColor = n == 44 || n == 45 ? Color.white : Color.black;
        GUI.Label(r, n < 37 ? n.ToString() : NCH[n - 37], st);
    }
    void DibujarBandeja(GUIStyle sT, GUIStyle sB0)
    {
        var r = vFic.r; var sb = new GUIStyle(sB0) { fontSize = 13 }; int puestas = 0; foreach (var m in marcas) if (m.puesta) puestas++;
        GUI.Label(new Rect(r.x + 8, r.y + 4, r.width - 16, 50), "MARCAR: agarrá cada ficha (números y chances doradas) y soltala en el centro de su casillero del paño. Las chances se guardan solas.", new GUIStyle(sT) { fontSize = 14, wordWrap = true });
        GUI.Label(new Rect(r.x + 8, r.y + 56, 220, 24), "Puestos: " + puestas + " de 73 (las de colores son chances y calles; cada calle va dos veces: un extremo y el otro)", new GUIStyle(sT) { fontSize = 15 });
        float bw = (r.width - 24f) / 3f;
        if (GUI.Button(new Rect(r.x + 8, r.y + 80, bw, 26), "CLAVAR NÚMEROS", sb)) { int pn = 0; for (int q = 0; q < 37; q++) if (marcas[q].puesta) pn++; if (pn < 37) Aviso("Faltan " + (37 - pn) + " números por poner"); else { GuardarMarcas(); modoMarcas = false; Aviso("Números clavados: ya se sabe a qué pleno corresponde cada lugar"); } }
        if (GUI.Button(new Rect(r.x + 12 + bw, r.y + 80, bw, 26), "BORRAR CHANCES", sb)) { for (int q = 37; q < marcas.Length; q++) marcas[q].puesta = false; GuardarChances(); }
        if (GUI.Button(new Rect(r.x + 16 + 2 * bw, r.y + 80, bw, 26), "CANCELAR", sb)) { modoMarcas = false; CargarMarcasSiHace(); }
        for (int n = 0; n < marcas.Length; n++) { if (marcas[n].puesta) continue; var rr = RMarcaTray(n); if (n != dragM) DibujarMarca(rr, n, sT); }
    }

    // ---------------------------------------------------------------- jugadas
    void SoltarGanadoras() { foreach (var f in fichas) f.ganadora = false; }   // la ficha ganadora se queda en el paño (de suerte, o por si repetís): solo se le saca la marca
    void RecogerTablero()   // BORRAR APUESTA: todas tus fichas del paño vuelven a tu pila
    {
        SoltarGanadoras();
        foreach (var f in fichas) if (f.pano) f.pano = false;
        apuesta.Clear(); Ordenar();
    }
    void LimpiarTablero()   // RECOGER: las fichas del paño vuelven a tu pila (ya no hay ningun boton que las haga desaparecer)
    {
        int n = 0; foreach (var f in fichas) if (f.pano) n += f.v;
        RecogerTablero(); Aviso(n > 0 ? "Recogiste $" + n + ": volvieron a tus fichas" : "El tablero ya estaba limpio");
    }
    void Deshacer()
    {
        if (apuesta.Count == 0) { Aviso("No hay nada para deshacer"); return; }
        var f = apuesta[apuesta.Count - 1]; apuesta.RemoveAt(apuesta.Count - 1); f.pano = false; Ordenar();
    }
    void Repetir()
    {
        if (ultima.Count == 0) { Aviso("Todavía no hubo una jugada anterior"); return; }
        RecogerTablero();
        var libres = fichas.FindAll(f => !f.pano && !f.placa && f != dragF); var elegidas = new List<Ficha>();
        foreach (var kv in ultima) { int v = kv.Key; var f = libres.Find(x => x.v == v && !elegidas.Contains(x)); if (f == null) { Aviso("No te alcanzan las fichas para repetir la jugada"); return; } elegidas.Add(f); }
        for (int i = 0; i < elegidas.Count; i++) { elegidas[i].pano = true; elegidas[i].uv = ultima[i].Value; apuesta.Add(elegidas[i]); }
        Ordenar(); int tot = 0; foreach (var f in elegidas) tot += f.v; Aviso("Repetiste la jugada: $" + tot);
    }
    void AgregarFichas(int total)   // fichas nuevas para tu pila (pagos)
    {
        int resto = total;
        for (int i = CV.Length - 1; i >= 0; i--) { int n = resto / CV[i]; resto -= n * CV[i]; for (int k = 0; k < n; k++) fichas.Add(new Ficha { v = CV[i], p = Vector2.zero }); }
    }
    void NuevaTirada()   // al lanzar: se anota la jugada para poder repetirla
    {
        SoltarGanadoras();
        var jug = new List<KeyValuePair<int, Vector2>>(); foreach (var f in apuesta) jug.Add(new KeyValuePair<int, Vector2>(f.v, f.uv));
        if (jug.Count > 0) ultima = jug; Ordenar();
    }
    float tDing; int estampa;   // estampa: valor de la ficha que se sigue poniendo con cada clic en el paño (0 = apagado)
    void Estampar(Vector2 pos)
    {
        var f = fichas.Find(x => !x.pano && !x.placa && x.v == estampa && x != dragF);
        if (f == null) { Aviso("No te quedan fichas de $" + Fmt(estampa)); estampa = 0; return; }
        Sfx(SFX_SUMAR); f.pano = true; { var q = APano(pos); f.uv = new Vector2((q.x - PR.x) / PR.width, (q.y - PR.y) / PR.height); } apuesta.Add(f);
        fichas.Remove(f); fichas.Add(f); Ordenar();
        var ap = Cubre(f.uv); if (ap != null) Aviso("$" + Fmt(f.v) + " a " + ap.nombre + ": paga " + ap.pago + " a 1");
        Badge(f);
    }
    // sonidos de fichas (Kenney, CC0): Resources/Ruleta/fichas_sonidos/s1..s5. Cual va con cada accion se cambia aca:
    const int SFX_APOYAR = 1, SFX_SUMAR = 2;   // apoyar una ficha en el paño / sumar otra con REPETIR
    static AudioClip[] sfx;
    void Sfx(int k)
    {
        if (sfx == null) { sfx = new AudioClip[5]; for (int i = 0; i < 5; i++) sfx[i] = Resources.Load<AudioClip>("Ruleta/fichas_sonidos/s" + (i + 1)); }
        var c = k >= 1 && k <= 5 ? sfx[k - 1] : null; if (c == null) return;
        Tocar(c, 0.55f);
    }
    // un grupo fijo de fuentes de audio que se reusan (antes se creaba y destruia una por sonido y se acababan)
    static GameObject audGO; static AudioSource[] pool; static int pIdx;
    static void Tocar(AudioClip c, float vol)
    {
        if (audGO == null) { audGO = new GameObject("rul_audio"); pool = new AudioSource[8]; for (int i = 0; i < pool.Length; i++) { pool[i] = audGO.AddComponent<AudioSource>(); pool[i].spatialBlend = 0f; pool[i].playOnAwake = false; } }
        AudioSource a = null; for (int i = 0; i < pool.Length; i++) if (!pool[i].isPlaying) { a = pool[i]; break; }
        if (a == null) { a = pool[pIdx++ % pool.Length]; a.Stop(); }
        a.volume = vol; a.PlayOneShot(c);
    }
    void Voz(string nombre)   // voz del crupier: Resources/Ruleta/voz/<nombre>
    {
        var c = Resources.Load<AudioClip>("Ruleta/voz/" + nombre); if (c == null) return;
        Tocar(c, 1f);
    }
    // ---- 21/11: fichas perdidas se quedan a la vista 3 s y despues pasa el crupier con el rastrillo ----
    List<Ficha> perdidas = new List<Ficha>(); float tRastr, tBarr = -1f; AudioClip clipRastr;
    const float ESPERA_RASTRILLO = 3f, DUR_RASTRILLO = 1.1f;
    void Rastrillo()   // sonido de barrido (ruido filtrado generado por codigo)
    {
        if (clipRastr == null)
        {
            int sr = 44100, n = (int)(sr * DUR_RASTRILLO); var d = new float[n]; var rnd = new System.Random(7); float lp = 0f;
            for (int i = 0; i < n; i++) { float t = i / (float)sr, env = Mathf.Sin(Mathf.PI * Mathf.Clamp01(t / DUR_RASTRILLO)); lp += ((float)(rnd.NextDouble() * 2.0 - 1.0) - lp) * 0.25f; d[i] = lp * env * 0.55f + (i % 4400 < 300 ? (float)(rnd.NextDouble() * 2.0 - 1.0) * 0.25f * env : 0f); }
            clipRastr = AudioClip.Create("rastrillo", n, 1, sr, false); clipRastr.SetData(d, 0);
        }
        Tocar(clipRastr, 0.7f);
    }
    void TickPerdidas(bool lanzada)
    {
        if (perdidas.Count == 0) { tBarr = -1f; return; }
        float now = Time.realtimeSinceStartup;
        if (lanzada) { perdidas.Clear(); tBarr = -1f; }
        else if (tBarr < 0f && now >= tRastr) { tBarr = now; Rastrillo(); }
        else if (tBarr >= 0f && now - tBarr > DUR_RASTRILLO) { perdidas.Clear(); tBarr = -1f; }
    }
    void DibujarPerdidas(Rect rp)
    {
        if (perdidas.Count == 0) return;
        float xr = tBarr < 0f ? -1f : rp.x + Mathf.Clamp01((Time.realtimeSinceStartup - tBarr) / DUR_RASTRILLO) * (rp.width + 30f);
        foreach (var f in perdidas) { if (tBarr >= 0f && PosFicha(f).x < xr) continue; Ficha1(f); }
        if (tBarr >= 0f)
        {
            Fondo(new Rect(xr - 7f, rp.y - 4f, 9f, rp.height + 8f), new Color(0.45f, 0.28f, 0.12f, 0.95f));
            for (float y = rp.y; y < rp.yMax; y += 14f) Fondo(new Rect(xr - 15f, y, 9f, 6f), new Color(0.35f, 0.2f, 0.08f, 0.95f));
        }
    }
    // llamada por la ruleta cuando la mesa no se dibuja (modo solo ruleta): sigue contando la tirada, el resultado y los pagos
    public void SoloTick(int resultado, bool lanzada)
    {
        Iniciar();
        if (lanzada && !lanzadaAntes) { perdidas.Clear(); NuevaTirada(); }
        if (resultado >= 0 && resultadoAntes < 0) FinTirada(resultado);
        lanzadaAntes = lanzada; resultadoAntes = resultado; TickPerdidas(lanzada);
    }
    // ---- apostar en grande: el paño ocupa casi toda la pantalla junto con las fichas; MINIMIZAR deja todo como estaba ----
    public bool apostarGrande; Rect[] gRect;
    void ResetGrande() { foreach (var id in new[] { "pano", "fic", "foto", "apu", "tot", "btns" }) foreach (var suf in new[] { "_x", "_y", "_w", "_h", "_f", "_cw", "_ch" }) PlayerPrefs.DeleteKey("rul21g_" + id + suf); apostarGrande = true; ApostarGrande(false); ApostarGrande(true); }
    public void ApostarGrande(bool on)
    {
        if (on == apostarGrande) return; Iniciar();
        if (on)
        {
            gRect = new Rect[todas.Length]; for (int i = 0; i < todas.Length; i++) gRect[i] = todas[i].r;
            var r0 = vPano.r; foreach (var v in todas) if (v != vPano && v != vFic && v != vApu && v != vTot) v.r = new Rect(-9000f, -9000f, v.r.width, v.r.height);
            float fh = Mathf.Clamp(SH * 0.2f, 100f, 160f), top = 36f;   // paño a todo el ancho, fichas chicas abajo a la izquierda, botones REPETIR/LIMPIAR al lado de las fichas
            vPano.r = new Rect(6f, top, SW - 12f, SH - top - fh - 8f);
            vFic.r = new Rect(0f, SH - fh, Mathf.Min(SW, Mathf.Max(fh * 6.2f, SW * 0.5f)), fh);
            vFoto.r = new Rect(SW - fh - 6f, SH - fh, fh, fh);   // foto de la rueda con los numeros: a la derecha de las fichas, se agranda y se mueve al editar
            vPano.reservaAbajo = 4f; Cargar(vPano, "rul21g_"); Cargar(vFic, "rul21g_"); { float x0 = vFic.r.xMax + 8f; vBtns.r = new Rect(x0, vFic.r.y, Mathf.Max(120f, vFoto.r.x - x0 - 8f), vFic.r.height); }
            Cargar(vFoto, "rul21g_"); Cargar(vBtns, "rul21g_");
            vApu.r = new Rect(6f, 2f, 200f, 32f); vTot.r = new Rect(212f, 2f, 200f, 32f); Cargar(vApu, "rul21g_"); Cargar(vTot, "rul21g_");   // cuanto apostas y cuanto hay en mesa, arriba a la izquierda   // si Pablo lo acomodo en grande, se usa lo guardado
        }
        else { vPano.reservaAbajo = 78f; if (gRect != null && gRect.Length == todas.Length) for (int i = 0; i < todas.Length; i++) todas[i].r = gRect[i]; }
        apostarGrande = on; Ordenar();
    }
    void FinTirada(int resultado)
    {
        Voz("n_" + resultado); estampa = 0; hist.Insert(0, resultado); if (hist.Count > 10) hist.RemoveAt(10);
        Debug.Log("[RULETA] salio " + resultado + " mapeado=" + mapeado + " fichasEnPano=" + fichas.FindAll(f => f.pano).Count);
        if (!mapeado) { if (fichas.Exists(f => f.pano)) { RecogerTablero(); Aviso("Marcá los números del paño para que se paguen las apuestas"); } return; }
        perdidas.Clear(); int ganado = 0, perdido = 0, dmax = 0; bool habia = false; var gan = new List<Ficha>();
        var gNom = new List<string>(); var gApu = new List<int>(); var gPag = new List<int>(); var gGan = new List<int>();
        for (int i = fichas.Count - 1; i >= 0; i--)
        {
            var f = fichas[i]; if (!f.pano) continue; habia = true;
            var ap = Cubre(f.uv); int pag = ap != null && ap.Gana(resultado) ? ap.pago : 0;
            if (pag > 0)
            {
                f.ganadora = true; ganado += pag * f.v; dmax = Mathf.Max(dmax, f.v); gan.Add(f);   // la postura queda puesta en el paño
                int ix = gNom.IndexOf(ap.nombre); if (ix < 0) { gNom.Add(ap.nombre); gApu.Add(f.v); gPag.Add(ap.pago); gGan.Add(pag * f.v); } else { gApu[ix] += f.v; gGan[ix] += pag * f.v; }
            }
            else { perdido += f.v; fichas.RemoveAt(i); perdidas.Add(f); }                          // las que no salieron no vuelven: quedan a la vista un rato y las retira el crupier
        }
        apuesta.Clear(); apuesta.AddRange(gan);
        if (perdidas.Count > 0) { tRastr = Time.realtimeSinceStartup + ESPERA_RASTRILLO; tBarr = -1f; }
        Ordenar();
        if (!habia) return;
        if (ganado > 0)
        {
            pagoPend = ganado; pagoDen = Mathf.Max(1, dmax); pagoRes = resultado;
            ganLineas.Clear(); ganTitulo = "★  ¡GANASTE!  +$" + Fmt(ganado) + "  ★";
            for (int i = 0; i < gNom.Count; i++) { if (i == 7) { ganLineas.Add("... y " + (gNom.Count - 7) + " más"); break; } ganLineas.Add(gNom[i] + "   (paga " + gPag[i] + " a 1)   +$" + Fmt(gGan[i])); }
            tGan = Time.realtimeSinceStartup; tDing = Time.realtimeSinceStartup + 1.8f;
            Aviso("¡Salió el " + resultado + "! Ganaste $" + Fmt(ganado));
        }
        else Aviso("Salió el " + resultado + ". Perdiste $" + Fmt(perdido));
    }
    void Ding()   // sonidito de acierto (campanitas generadas por codigo)
    {
        if (clipGan == null)
        {
            int sr = 44100, n = (int)(sr * 1.0f); var d = new float[n]; float[] fr = { 1046.5f, 1318.5f, 1568f, 2093f };
            for (int k = 0; k < 4; k++) { int st = (int)(k * 0.09f * sr); for (int i = st; i < n; i++) { float t = (i - st) / (float)sr; d[i] += Mathf.Sin(2f * Mathf.PI * fr[k] * t) * Mathf.Exp(-t * 5f) * 0.22f; } }
            clipGan = AudioClip.Create("ganaste", n, 1, sr, false); clipGan.SetData(d, 0);
        }
        var go = new GameObject("rul_ding"); var a = go.AddComponent<AudioSource>(); a.spatialBlend = 0f; a.volume = 0.9f; a.PlayOneShot(clipGan); Object.Destroy(go, 2f);
    }
    void DibujarToast(GUIStyle sT)   // cartelito de que ganaste y con que: aparece, queda unos segundos y se va
    {
        float dt = Time.realtimeSinceStartup - tGan; if (ganLineas.Count == 0 || dt > 6f) return;
        float a = dt > 5f ? 6f - dt : 1f; var gc = GUI.color; GUI.color = new Color(1f, 1f, 1f, a);
        float W = Mathf.Min(SW - 40f, 380f), H = 40f + ganLineas.Count * 22f; var r = new Rect((SW - W) / 2f, 50f, W, H);
        Fondo(new Rect(r.x - 2, r.y - 2, r.width + 4, r.height + 4), new Color(1f, 0.82f, 0.25f, a)); Fondo(r, new Color(0.04f, 0.15f, 0.09f, 0.97f * a));
        Fondo(new Rect(r.x + 4, r.y + 4, r.width - 8, 28), new Color(1f, 0.82f, 0.25f, 0.14f * a));
        var ts = new GUIStyle(sT) { fontSize = 19, alignment = TextAnchor.MiddleCenter }; ts.normal.textColor = new Color(1f, 0.88f, 0.4f);
        GUI.Label(new Rect(r.x + 4, r.y + 4, r.width - 8, 28), ganTitulo, ts);
        var ls = new GUIStyle(sT) { fontSize = 14, alignment = TextAnchor.MiddleCenter }; ls.normal.textColor = new Color(0.9f, 1f, 0.92f);
        for (int i = 0; i < ganLineas.Count; i++) GUI.Label(new Rect(r.x + 8, r.y + 36 + i * 22, r.width - 16, 22), ganLineas[i], ls);
        GUI.color = gc;
    }

    // ---------------------------------------------------------------- dibujo
    public Rect RectRuleta { get { Iniciar(); return vRul.r; } }
    float lW, lH;   // tamaño del lienzo en el que estan expresadas las ventanas: si cambia (tablet mas grande o mas chica, zoom), todo se escala junto y no se desarma
    bool escribAntes; public bool editando;   // editando = se ven los nodos y se pueden mover las ventanas (se activa desde los ajustes)
    bool chequeoIng; float swAnt, shAnt;
    public void AlCerrar() { if (apostarGrande) ApostarGrande(false); perdidas.Clear(); chequeoIng = false; modal = 0; if (escribAntes) { escribAntes = false; if (Escribiendo != null) Escribiendo(false); } }

    public void Dibujar(Event e, bool config, Texture2D tPano, GUIStyle sB0, GUIStyle sT, int resultado, bool lanzada)
    {
        Iniciar(); sTc = sT; mouse = e.mousePosition;
        if (PlayerPrefs.GetInt("rul21_reset2", 0) == 0 && e.type == EventType.Layout) { PlayerPrefs.SetInt("rul21_reset2", 1); panoRot = false; panoInv = false; PlayerPrefs.SetInt("rul21_pano_rot", 0); PlayerPrefs.SetInt("rul21_pano_inv", 0); Restaurar(); PlayerPrefs.Save(); }   // 21/11: una sola vez, las ventanas vuelven a su lugar (una disposicion de prueba habia dejado el paño tapando ACOMODAR)
        if (e.type == EventType.Layout && !editando && modo < 0 && dragF == null && dragB < 0 && dragM < 0)   // cambio de tamano (pantalla completa): cada ventana vuelve a lo guardado, en proporcion a la pantalla nueva; mientras se acomoda, no se toca nada
        { if (swAnt > 1f && (Mathf.Abs(SW - swAnt) > 1f || Mathf.Abs(SH - shAnt) > 1f)) { foreach (var v in todas) Cargar(v); ordenPend = true; } swAnt = SW; shAnt = SH; }
        if (ordenPend) { ordenPend = false; Ordenar(); }
        GUI.enabled = modal == 0 && pagoPend <= 0 && cambioPend <= 0 && !editando && !muestrario;   // con un cartel abierto, lo de atras no responde
        if ((modo >= 0 || dragF != null || dragB >= 0 || dragM >= 0) && PosMouse(out var pm)) mouse = pm;
        if (e.type == EventType.Layout)
        {
            if (Escribiendo != null && (modal == 1) != escribAntes) { escribAntes = modal == 1; Escribiendo(escribAntes); }
            if (tDing > 0f && Time.realtimeSinceStartup >= tDing) { tDing = 0f; Ding(); }
            if (!chequeoIng) { chequeoIng = true; if (TotalEnMesa() <= 0) { modal = 1; ingTxt = ""; ingMsg = "Te quedaste sin nada en la mesa: ingresá dinero para seguir."; } }
            if (lanzada && !lanzadaAntes) { perdidas.Clear(); NuevaTirada(); }
            if (resultado >= 0 && resultadoAntes < 0) FinTirada(resultado);
            lanzadaAntes = lanzada; resultadoAntes = resultado; TickPerdidas(lanzada);
            if (apostarGrande && (config || modoMarcas)) ApostarGrande(false);
        }
        bool panoVisible = tPano != null; if (panoVisible && !mapeado && !modoMarcas && tPano.name == "pano1_verde") AutoMarcas(); var rp = vPano.r; bool dinero = !config && !modoMarcas && cajaVis;   // billetes y ventanilla
        bool cajaOn = CAJA && dinero && (cajaVis || editando); bool fichasVis = !config; var sBt = new GUIStyle(sB0) { fontSize = 12 };

        DibujarNodos(vRul, true);   // la vista de la ruleta queda detras de todo

        // ---- zona de fichas (o la bandeja de marcas) ----
        if (fichasVis)
        {
            float kf = KF; var chico = new GUIStyle(sT) { fontSize = Mathf.Max(9, Mathf.RoundToInt(14 * kf)) }; var sB = new GUIStyle(sB0) { fontSize = Mathf.Max(9, Mathf.RoundToInt(13 * kf)) };
            var fch = new GUIStyle(sT) { fontSize = Mathf.Max(8, Mathf.RoundToInt(13 * kf)), alignment = TextAnchor.MiddleCenter };
            if (editando) { DibujarBarra(vFic, sT); Fondo(vFic.r, new Color(0f, 0f, 0f, 0.4f)); } 
            if (modoMarcas) DibujarBandeja(sT, sB0);
            else
            {
                { int tf = 0; foreach (var f in fichas) if (!f.pano) tf += f.v; var rt2 = SRF(new Rect(8, RFich.yMax - 24, 360, 22)); Fondo(rt2, new Color(0f, 0f, 0f, 0.5f)); var st2 = new GUIStyle(fch) { alignment = TextAnchor.MiddleLeft }; st2.normal.textColor = new Color(1f, 0.9f, 0.5f); GUI.Label(new Rect(rt2.x + 6, rt2.y, rt2.width - 8, rt2.height), "TOTAL EN FICHAS:  $" + Fmt(tf), st2); }
                { var sbc0 = new GUIStyle(sB) { fontStyle = FontStyle.Bold }; if (GUI.Button(SRF(new Rect(FIC_W - 224, RFich.yMax - 26, 106, 24)), "DISEÑO", sbc0)) muestrario = true; if (GUI.Button(SRF(new Rect(FIC_W - 336, RFich.yMax - 26, 106, 24)), "ORDENAR", sbc0)) Ordenar(); }
                { var sbc = new GUIStyle(sB) { fontStyle = FontStyle.Bold }; if (GUI.Button(SRF(new Rect(FIC_W - 112, RFich.yMax - 26, 106, 24)), "CAMBIAR", sbc)) { cajaVis = true; var sueltas = fichas.FindAll(q => !q.pano); foreach (var q in sueltas) { fichas.Remove(q); ventPla.Add(q.v); } Ordenar(); Cambiar(); } }
                DibujarPilas(fch);
            }
            DibujarNodos(vFic, false);
        }

        // ---- ventanilla (horizontal) ----
        if (dinero)
        {
            float k = KV; var chico = new GUIStyle(sT) { fontSize = Mathf.Max(9, Mathf.RoundToInt(15 * k)) }; var sB = new GUIStyle(sB0) { fontSize = Mathf.Max(9, Mathf.RoundToInt(14 * k)) };
            DibujarBarra(vVen, sT); Fondo(vVen.r, new Color(0.08f, 0.2f, 0.1f, 0.95f));
            int tot = 0; foreach (int b in vent) tot += b; foreach (int b in ventPla) tot += b;
            { var sp = new GUIStyle(chico); sp.normal.textColor = new Color(1f, 0.9f, 0.5f); GUI.Label(SRV(new Rect(8, 2, 330, 26)), "TU PLATA:  $" + Fmt(PlataAhora()) + (modoPrueba ? "   [PRUEBA]" : ""), sp); GUI.Label(SRV(new Rect(360, 2, 350, 26)), "EN VENTANILLA:  $" + Fmt(tot), chico); }
            var rd = SRV(RDrop); Fondo(rd, dragB >= 0 && rd.Contains(mouse) ? new Color(0.2f, 0.5f, 0.2f, 0.97f) : new Color(0.04f, 0.12f, 0.06f, 0.97f));
            for (int i = 0; i < vent.Count && i < 7; i++) { var t = bill[Cual(vent[i], BV)]; if (t != null) GUI.DrawTexture(SRV(new Rect(14 + i * 12, 34, 100, 64)), t, ScaleMode.StretchToFill); }
            for (int i = 0; i < ventPla.Count && i < 6; i++) DibujarPlaca(SRV(new Rect(14 + i * 28, 78, 46, 29)), ventPla[i]);
            if (vent.Count == 0 && ventPla.Count == 0) { var sv = new GUIStyle(chico) { alignment = TextAnchor.MiddleCenter, wordWrap = true }; sv.normal.textColor = new Color(0.7f, 0.85f, 0.7f, 0.8f); GUI.Label(rd, "soltá acá tus billetes", sv); }
            GUI.Label(SRV(new Rect(256, 32, 456, 26)), vent.Count + " billetes en la ventanilla", chico);
            { var sw = new GUIStyle(chico) { wordWrap = true, fontSize = Mathf.Max(8, Mathf.RoundToInt(12 * k)) }; GUI.Label(SRV(new Rect(256, 58, 300, 44)), "Llevá billetes o fichas hasta acá (o tocá PASAR FICHAS) y después CAMBIAR POR FICHAS.", sw); }
            var sbig = new GUIStyle(sB) { fontStyle = FontStyle.Bold }; var bgc = GUI.backgroundColor; GUI.backgroundColor = new Color(0.45f, 1f, 0.5f);
            if (GUI.Button(SRV(new Rect(8, 108, 150, 36)), "INGRESAR $", sbig)) { modal = 1; ingTxt = ""; ingMsg = ""; }
            GUI.backgroundColor = bgc;
            if (GUI.Button(SRV(new Rect(164, 108, 200, 36)), "CAMBIAR POR FICHAS", sB)) Cambiar();
            if (GUI.Button(SRV(new Rect(370, 108, 190, 36)), "VACIAR VENTANILLA", sB)) Vaciar();
            if (GUI.Button(SRV(new Rect(566, 108, 146, 36)), "RETIRARSE", sB)) modal = 2;
            if (GUI.Button(SRV(new Rect(566, 66, 146, 36)), "CAMBIAR FICHAS", sB)) { var sueltas = fichas.FindAll(q => !q.pano); foreach (var q in sueltas) { fichas.Remove(q); ventPla.Add(q.v); } Ordenar(); Cambiar(); }
            DibujarNodos(vVen, false);
        }
        hayPl = false; foreach (var f in fichas) if (f.placa) { hayPl = true; break; }
        // ---- placas (premios grandes): la ventana aparece solo cuando tenes alguna ----
        if (fichasVis && hayPl)
        {
            float k = KP; var fch = new GUIStyle(sT) { fontSize = Mathf.Max(8, Mathf.RoundToInt(14 * k)), alignment = TextAnchor.MiddleCenter };
            DibujarBarra(vPla, sT); Fondo(vPla.r, new Color(0.12f, 0.06f, 0.16f, 0.92f));
            for (int i = 0; i < PL.Length; i++) { int n = 0; foreach (var f in fichas) if (f.placa && !f.pano && f.v == PL[i]) n++; var rc = new Rect(vPla.r.x + (52f + i * 100f - 40f) * k, vPla.r.y + 150f * k, 80f * k, 26f * k); GUI.Label(rc, n > 0 ? "x" + n : "", fch); }
            foreach (var f in fichas) if (f.placa && !f.pano && f != dragF) Ficha1(f);
            DibujarNodos(vPla, false);
        }
        if (Time.realtimeSinceStartup - tAviso < 3f) { var s = new GUIStyle(sT) { fontSize = 20 }; s.normal.textColor = new Color(1f, 0.9f, 0.5f); GUI.Label(new Rect(14, SH - 64, 700, 28), aviso, s); }

        // ---- paño (girado 90 grados: el 0 queda arriba) + botones de jugada ----
        if (panoVisible)
        {
            DibujarBarra(vPano, sT);
            if (GUI.Button(new Rect(rp.xMax - 90, rp.y - 24, 88, 22), modoMarcas ? "SALIR" : "MARCAR", sBt)) { modoMarcas = !modoMarcas; if (modoMarcas) { dragF = null; Aviso("Arrastrá cada número al centro de su casillero"); } else CargarMarcasSiHace(); }
            if (!apostarGrande && GUI.Button(new Rect(rp.x + 2, rp.y - 24, 100, 22), cajaVis ? "▲ OCULTAR $" : "▼ BILLETES", sBt)) cajaVis = !cajaVis;
            { var mRot = GUI.matrix; if (panoRot) { var pc = new Vector3(rp.center.x, rp.center.y, 0f); GUI.matrix = mRot * Matrix4x4.TRS(pc, Quaternion.Euler(0f, 0f, panoInv ? -90f : 90f), Vector3.one) * Matrix4x4.TRS(-pc, Quaternion.identity, Vector3.one); } var pr = PR;   // giro a mano: GUIUtility.RotateAroundPivot ignora la escala de la tablet y corria el paño de lugar
              var tg2 = PanoGirado(tPano); if (tg2 != null) GUI.DrawTexture(pr, tg2, ScaleMode.StretchToFill);
              if (!modoMarcas) BotonTirar(new Rect(pr.x + pr.width * 0.035f, pr.y + pr.height * 0.02f, pr.width * 0.27f, pr.height * 0.095f), lanzada);
              GUI.matrix = mRot; }
            if (apostarGrande && tirarPedido) ApostarGrande(false);   // TIRAR en el paño grande: se minimiza solo y la ruleta pasa a verse de cerca
            if (editando) { var ge = GUI.enabled; GUI.enabled = true; if (GUI.Button(BtnGirar, !panoRot ? "GIRAR PAÑO" : panoInv ? "GIRAR AL OTRO LADO" : "PAÑO ACOSTADO", new GUIStyle(sB0) { fontSize = tactil ? 22 : 15, fontStyle = FontStyle.Bold })) GirarPano(); GUI.enabled = ge; }
            float bx = rp.x, bw = rp.width / 2f, by = rp.yMax + 4f, bh = 32f, bg = 36f; if (apostarGrande) { var ba = vBtns.r; bx = ba.x; bw = ba.width / 2f; by = ba.y; bh = Mathf.Clamp((ba.height - 6f) / 2f, 20f, 120f); bg = bh + 4f; } var sj = new GUIStyle(sB0) { fontSize = apostarGrande ? Mathf.Clamp(Mathf.RoundToInt(Mathf.Min(bw / 7f, bh * 0.5f)), 9, 34) : Mathf.Clamp(Mathf.RoundToInt(bw / 9f), 9, 16) };
            if (GUI.Button(new Rect(bx, by, bw - 2, bh), "REPETIR", sj)) Repetir();
            if (GUI.Button(new Rect(bx + bw, by, bw, bh), "RECOGER", sj)) LimpiarTablero();
            if (GUI.Button(new Rect(bx, by + bg, bw - 2, bh), "DESHACER", sj)) Deshacer();
            if (GUI.Button(new Rect(bx + bw, by + bg, bw, bh), "BORRAR APUESTA", sj)) { RecogerTablero(); Aviso("Apuesta borrada: tus fichas volvieron"); }
            DibujarNodos(vPano, false);
        }
        if (!modoMarcas) foreach (var f in fichas) if (f.pano && f != dragF) Ficha1(f);
        if (!modoMarcas) DibujarPerdidas(rp);
        // ---- billetes (caja): ya no se usa, la casa tiene billetes infinitos ----
        if (cajaOn)
        {
            float k = KB; var chico = new GUIStyle(sT) { fontSize = Mathf.Max(9, Mathf.RoundToInt(14 * k)) };
            var fch = new GUIStyle(sT) { fontSize = Mathf.Max(8, Mathf.RoundToInt(13 * k)), alignment = TextAnchor.MiddleCenter };
            DibujarBarra(vBil, sT); Fondo(vBil.r, new Color(0.18f, 0.1f, 0.05f, 0.92f)); GUI.Label(SRB(new Rect(8, 2, 360, 26)), "TUS BILLETES  (agarrá uno y llevalo a la ventanilla)", chico);
            for (int i = 0; i < BV.Length; i++)
            {
                var r = SRB(RBill(i)); Fondo(new Rect(r.x - 3 * k, r.y - 3 * k, r.width + 6 * k, r.height + 6 * k), new Color(0.05f, 0.03f, 0.02f, 0.92f));
                if (bill[i] != null) { var cc = GUI.color; if (stock[i] <= 0) GUI.color = new Color(1f, 1f, 1f, 0.25f); GUI.DrawTexture(r, bill[i], ScaleMode.StretchToFill); GUI.color = cc; }
                { var pv = new Rect(r.x + r.width * 0.12f, r.y + r.height * 0.22f, r.width * 0.76f, r.height * 0.56f); Fondo(pv, new Color(0f, 0f, 0f, stock[i] <= 0 ? 0.35f : 0.72f));
              var sv = new GUIStyle(sT) { fontSize = Mathf.Max(10, Mathf.RoundToInt(r.height * 0.36f)), alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold }; sv.normal.textColor = new Color(1f, 0.92f, 0.45f); GUI.Label(pv, "$" + BV[i], sv); }   // el valor del billete bien grande, para que se lea siempre
            GUI.Label(new Rect(r.x, r.yMax + 1 * k, r.width, 18 * k), "tenés " + stock[i], fch);
            }
            int enCaja = 0; for (int i = 0; i < BV.Length; i++) enCaja += stock[i] * BV[i];
            { var rt = SRB(RTotalCaja); Fondo(rt, new Color(0.05f, 0.03f, 0.02f, 0.92f)); GUI.Label(new Rect(rt.x, rt.y + 2 * k, rt.width, 22 * k), "TOTAL EN BILLETES", fch); GUI.Label(new Rect(rt.x, rt.y + 24 * k, rt.width, 38 * k), "$ " + enCaja, new GUIStyle(sT) { fontSize = Mathf.Max(10, Mathf.RoundToInt(28 * k)), alignment = TextAnchor.MiddleCenter }); }
            DibujarNodos(vBil, false);
        }

        if (badgeF != null)   // numerito: cuantas fichas llevas en esa apuesta (aparece y se va)
        {
            float db = Time.realtimeSinceStartup - tBadge;
            if (db > 1.8f || !fichas.Contains(badgeF) || !badgeF.pano) badgeF = null;
            else
            {
                float a = db > 1.2f ? (1.8f - db) / 0.6f : 1f; var pb = PosFicha(badgeF) + new Vector2(CP * 0.55f, -CP * 0.75f); float sz = 24f; var rb = new Rect(pb.x - sz / 2f, pb.y - sz / 2f, sz, sz);
                var gc = GUI.color; GUI.color = new Color(0.05f, 0.1f, 0.06f, 0.95f * a); GUI.DrawTexture(new Rect(rb.x - 2, rb.y - 2, sz + 4, sz + 4), Disco()); GUI.color = new Color(1f, 0.85f, 0.25f, a); GUI.DrawTexture(rb, Disco()); GUI.color = gc;
                var sbd = new GUIStyle(sT) { fontSize = 15, alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold }; sbd.normal.textColor = new Color(0.05f, 0.08f, 0.05f, a); GUI.Label(rb, badgeN.ToString(), sbd);
            }
        }
        if (modoMarcas && panoVisible) foreach (var m in marcas) if (m.puesta && m.n != dragM) DibujarMarca(RMarcaPano(m), m.n, sT);

        // ---- cuanto llevas apostado (se va sumando) ----
        {
            int ap = 0; foreach (var f in fichas) if (f.pano) ap += f.v; var ra = vApu.r;
            Fondo(new Rect(ra.x - 2, ra.y - 2, ra.width + 4, ra.height + 4), new Color(1f, 0.82f, 0.25f, 0.9f)); Fondo(ra, new Color(0.04f, 0.15f, 0.09f, 0.95f));
            var s1 = new GUIStyle(sT) { fontSize = Mathf.Max(9, Mathf.RoundToInt(ra.height * 0.22f)), alignment = TextAnchor.MiddleCenter }; s1.normal.textColor = new Color(0.8f, 0.95f, 0.85f);
            var s2 = new GUIStyle(sT) { fontSize = Mathf.Max(10, Mathf.RoundToInt(ra.height * 0.46f)), alignment = TextAnchor.MiddleCenter }; s2.normal.textColor = new Color(1f, 0.88f, 0.4f);
            GUI.Label(new Rect(ra.x, ra.y + ra.height * 0.04f, ra.width, ra.height * 0.32f), "APOSTASTE", s1); GUI.Label(new Rect(ra.x, ra.y + ra.height * 0.32f, ra.width, ra.height * 0.64f), "$ " + Fmt(ap), s2);
            DibujarNodos(vApu, true);
        }
        if (apostarGrande) DibujarNodos(vBtns, false);
        if (apostarGrande && texFoto != null) { GUI.DrawTexture(vFoto.r, texFoto, ScaleMode.ScaleToFit); DibujarNodos(vFoto, false); }
        if (!apostarGrande && !modoMarcas && panoVisible && !config) { if (BotonOro(vApos.r, "APOSTAR") && !editando) ApostarGrande(true); DibujarNodos(vApos, true); }

        // ---- total de todo lo que hay en la mesa: fichas (tambien las apostadas) + billetes ----
        if (!config)
        {
            int tF = 0; foreach (var f in fichas) tF += f.v; int tB = 0; foreach (int b in vent) tB += b; foreach (int b in ventPla) tB += b; for (int i = 0; i < BV.Length; i++) tB += stock[i] * BV[i];
            var rt3 = vTot.r; Fondo(new Rect(rt3.x - 2, rt3.y - 2, rt3.width + 4, rt3.height + 4), new Color(1f, 0.82f, 0.25f, 0.9f)); Fondo(rt3, new Color(0.04f, 0.15f, 0.09f, 0.95f));
            var t1 = new GUIStyle(sT) { fontSize = Mathf.Max(8, Mathf.RoundToInt(rt3.height * 0.2f)), alignment = TextAnchor.MiddleCenter, clipping = TextClipping.Clip }; t1.normal.textColor = new Color(0.8f, 0.95f, 0.85f);
            var t2 = new GUIStyle(sT) { fontSize = Mathf.Max(10, Mathf.RoundToInt(rt3.height * 0.44f)), alignment = TextAnchor.MiddleCenter, clipping = TextClipping.Clip }; t2.normal.textColor = new Color(1f, 0.88f, 0.4f);
            GUI.Label(new Rect(rt3.x, rt3.y + rt3.height * 0.04f, rt3.width, rt3.height * 0.32f), "TOTAL EN MESA", t1); GUI.Label(new Rect(rt3.x, rt3.y + rt3.height * 0.3f, rt3.width, rt3.height * 0.68f), "$ " + Fmt(tF + tB), t2);
            DibujarNodos(vTot, true);
        }

        // ---- cartel del resultado: el ultimo arriba (grande) y los anteriores abajo ----
        {
            var rr = vRes.r; Fondo(new Rect(rr.x - 1, rr.y - 1, rr.width + 2, rr.height + 2), new Color(1f, 1f, 1f, 0.25f)); Fondo(rr, new Color(0f, 0f, 0f, 0.6f));
            float ladoG = Mathf.Max(8f, Mathf.Min(rr.width - 6f, (rr.height - 6f - 30f) / (1f + 9f * 0.62f))), y0 = rr.y + 3f;
            for (int i = 0; i < 10; i++)
            {
                float lado = i == 0 ? ladoG : ladoG * 0.62f; var fr = new Rect(rr.x + (rr.width - lado) / 2f, y0, lado, lado); y0 += lado + 3f;
                int num = i < hist.Count ? hist[i] : -1;
                Color cn = num < 0 ? new Color(0.12f, 0.12f, 0.12f, 0.5f) : (num == 0 ? new Color(0.1f, 0.6f, 0.25f) : (Rojo(num) ? new Color(0.8f, 0.1f, 0.12f) : new Color(0.08f, 0.08f, 0.1f)));
                Fondo(new Rect(fr.x - 1, fr.y - 1, fr.width + 2, fr.height + 2), new Color(1f, 1f, 1f, num < 0 ? 0.12f : 0.4f)); Fondo(fr, cn);
                var stn = new GUIStyle(sT) { fontSize = Mathf.Max(9, Mathf.RoundToInt(fr.height * (num >= 10 ? 0.5f : 0.62f))), alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold };
                GUI.Label(fr, num >= 0 ? num.ToString() : "-", stn);
            }
            DibujarNodos(vRes, true);
        }

        if (editando && modal == 0)
        {
            GUI.enabled = true;
            var gbl = GUI.backgroundColor; GUI.backgroundColor = new Color(1f, 0.85f, 0.2f);
            float bw = tactil ? 170f : 98f, bh = tactil ? 58f : 34f; int fs2 = tactil ? 22 : 15;
            if (GUI.Button(new Rect(SW - bw - 8f, SH - bh - 8f, bw, bh), "LISTO", new GUIStyle(sBt) { fontStyle = FontStyle.Bold, fontSize = fs2 })) { editando = false; GuardarTodas(); }
            if (tactil && GUI.Button(new Rect(SW - bw * 2f - 20f, SH - bh - 8f, bw + 20f, bh), "ORDENAR TODO", new GUIStyle(sBt) { fontStyle = FontStyle.Bold, fontSize = 18 })) { if (apostarGrande) ResetGrande(); else PedirOrdenar(); }
            GUI.backgroundColor = gbl;
        }
        // ---- muestrario: los 5 diseños completos para elegir ----
        if (muestrario)
        {
            GUI.enabled = true;   // con el muestrario abierto, lo de atras no responde (un toque en un diseño apretaba el MARCAR de abajo)
            float W = Mathf.Min(SW - 40f, 900f), H = Mathf.Min(SH - 40f, 640f); var rm = new Rect((SW - W) / 2f, (SH - H) / 2f, W, H);
            Fondo(new Rect(0, 0, SW, SH), new Color(0f, 0f, 0f, 0.55f)); Fondo(rm, new Color(0.07f, 0.2f, 0.12f, 0.98f));
            GUI.Label(new Rect(rm.x + 14, rm.y + 6, 600, 34), "MUESTRARIO DE FICHAS  (tocá el diseño que te guste)", new GUIStyle(sT) { fontSize = 22 });
            if (GUI.Button(new Rect(rm.xMax - 130, rm.y + 8, 116, 32), "CERRAR", sBt)) muestrario = false;
            float fila = (H - 60f) / 6f, cs = Mathf.Min(fila - 18f, (W - 190f) / 9f);
            for (int d = 1; d <= 6; d++)
            {
                var rr = new Rect(rm.x + 10, rm.y + 50 + (d - 1) * fila, W - 20, fila - 6); Fondo(rr, d == diseno ? new Color(0.9f, 0.75f, 0.2f, 0.45f) : new Color(0f, 0f, 0f, 0.25f));
                GUI.Label(new Rect(rr.x + 8, rr.y + rr.height / 2f - 16, 150, 32), "Diseño " + d + (d == diseno ? "  ✔" : ""), new GUIStyle(sT) { fontSize = 20 });
                for (int i = 0; i < CV.Length; i++) { var t = chips[d][i]; if (t != null) GUI.DrawTexture(new Rect(rr.x + 160 + i * (cs + 6), rr.y + (rr.height - cs) / 2f, cs, cs), t); }
                if (GUI.Button(rr, GUIContent.none, GUIStyle.none)) { diseno = d; PlayerPrefs.SetInt("rul6_diseno", d); PlayerPrefs.Save(); }
            }
            return;
        }

        if (modal == 1) { DibujarIngreso(sT, sB0); return; }
        if (modal == 2) { DibujarRetiro(sT, sB0); return; }
        if (pagoPend > 0) { GUI.enabled = true; DibujarPago(sT, sB0); DibujarToast(sT); return; }
        if (cambioPend > 0) { GUI.enabled = true; DibujarCambio(sT, sB0); return; }

        // ---- lo que se lleva agarrado (las fichas, grandes) ----
        if (dragM >= 0) { float ss = 40f; DibujarMarca(new Rect(mouse.x - ss / 2f, mouse.y - ss / 2f, ss, ss), dragM, sT); }
        if (dragF != null) { var q = mouse + off; float s = (dragF.placa ? PLW * KP * 1.15f : CS * KF * 1.25f); var rq = new Rect(q.x - s / 2, q.y - (dragF.placa ? s * PLH / PLW : s) / 2 - 4, s, dragF.placa ? s * PLH / PLW : s); if (dragF.placa) DibujarPlaca(rq, dragF.v); else DibujarFicha3D(new Vector2(q.x, q.y), s, dragF.v); }
        if (dragB >= 0 && bill[dragB] != null) GUI.DrawTexture(new Rect(mouse.x - 57, mouse.y - 37, 114, 74), bill[dragB], ScaleMode.StretchToFill);

        if (estampa > 0 && panoVisible && !config && !modoMarcas)
        {
            if (dragF == null && modo < 0 && vPano.r.Contains(mouse)) { var tg = Tex(estampa); if (tg != null) { var gc2 = GUI.color; GUI.color = new Color(1f, 1f, 1f, 0.75f); GUI.DrawTexture(new Rect(mouse.x - CP / 2f, mouse.y - CP / 2f, CP, CP), tg); GUI.color = gc2; } }
        }
        DibujarToast(sT);

        if (!editando && !apostarGrande && !modoMarcas && !config && modal == 0 && pagoPend <= 0 && cambioPend <= 0) { var ge2 = GUI.enabled; GUI.enabled = true; if (GUI.Button(new Rect(SW - 112f, SH - 30f, 106f, 26f), "ACOMODAR", sBt)) editando = true; GUI.enabled = ge2; }   // siempre a la vista (en el celu los demas quedaban tapados)
        { var ge3 = GUI.enabled; GUI.enabled = true;   // EDITAR y MINIMIZAR del paño grande: se dibujan al final para que el paño no los tape
            if (apostarGrande && !editando && !modoMarcas && !config && modal == 0 && GUI.Button(new Rect(SW - 190f - 128f, 4f, 120f, 44f), "EDITAR", new GUIStyle(sB0) { fontSize = 18, fontStyle = FontStyle.Bold })) editando = true;
            if (apostarGrande && !editando && !modoMarcas && !config && modal == 0) { var gb = GUI.backgroundColor; GUI.backgroundColor = new Color(1f, 0.85f, 0.2f); if (GUI.Button(new Rect(SW - 190f, 4f, 184f, 44f), "MINIMIZAR", new GUIStyle(sB0) { fontSize = 20, fontStyle = FontStyle.Bold })) ApostarGrande(false); GUI.backgroundColor = gb; }
        GUI.enabled = ge3; }
        // ---- mouse ----
        if (estampa > 0 && ((e.type == EventType.MouseDown && e.button == 1) || (e.type == EventType.KeyDown && e.keyCode == KeyCode.Escape))) { estampa = 0; e.Use(); }
        if (e.type == EventType.MouseDown && e.button == 0)
        {
            modo = -1; vAct = null; int h;
            if (modoMarcas)
            {
                for (int n = marcas.Length - 1; n >= 0 && dragM < 0; n--) { var m = marcas[n]; Rect rr = m.puesta ? RMarcaPano(m) : RMarcaTray(n); if (rr.Contains(mouse) && (m.puesta ? panoVisible : !config)) { dragM = n; m.puesta = false; e.Use(); } }
            }
            // 1. nodos y barras de las ventanas (de adelante hacia atras)
            Vent[] orden = { vBtns, vFoto, vApos, vBil, vRes, vApu, vTot, vPano, vVen, vFic, vPla };
            foreach (var v in orden)
            {
                if (modo >= 0 || dragM >= 0) break;
                if (editando && v == vPano && panoVisible && BtnGirar.Contains(mouse)) break;   // el boton GIRAR PAÑO no arrastra la ventana
                if (v == vBtns && !apostarGrande) continue;
                if (v == vFoto && (!apostarGrande || texFoto == null)) continue;
                if (v == vApos && (apostarGrande || modoMarcas || !panoVisible || config)) continue;
                if (v == vPano && !panoVisible) continue; if ((v == vFic || v == vPla) && !fichasVis) continue; if (v == vPla && !hayPl) continue; if (v == vBil && !cajaOn) continue; if (v == vVen && !dinero) continue;
                if ((h = HitNodo(v, mouse)) >= 0) { vAct = v; modo = h; m0 = mouse; r0 = v.r; e.Use(); }
                else if ((editando || v == vBil) && !v.fija && v.barra && Barra(v).Contains(mouse)) { vAct = v; modo = 8; off = v.r.position - mouse; e.Use(); }
                else if (editando && !v.fija && v.r.Contains(mouse)) { vAct = v; modo = 8; off = v.r.position - mouse; e.Use(); }
            }
            // repetir: con una ficha elegida, cada clic en el paño apuesta otra igual (sin ir a buscarla a la pila)
            bool est = false;
            if (estampa > 0 && modo < 0 && dragM < 0 && !modoMarcas && panoVisible && !config && vPano.r.Contains(mouse)) { Estampar(mouse); est = true; e.Use(); }
            else if (estampa > 0) estampa = 0;
            // 2. fichas (de arriba hacia abajo)
            for (int i = fichas.Count - 1; i >= 0 && !est && dragF == null && modo < 0 && dragM < 0 && !modoMarcas; i--)
            {
                var f = fichas[i]; if (!f.pano && !fichasVis) continue; var p = PosFicha(f); float rad = Mathf.Max(TamFicha(f), 22f) / 2f; float k3 = TamFicha(f) / C3W; bool tray3 = !f.pano && !f.placa; float ex = (mouse.x - p.x) / rad, ey = (mouse.y - (p.y + 5f * k3)) / Mathf.Max(38f * k3, 11f);
                if (tray3 ? ex * ex + ey * ey < 1f : (mouse - p).sqrMagnitude < rad * rad) { dragF = f; off = p - mouse; f.pPrev = f.p; f.panoPrev = f.pano; f.uvPrev = f.uv; fichas.RemoveAt(i); fichas.Add(f); e.Use(); }
            }
            // 3. billetes de la caja
            if (cajaOn && dragF == null && !est && modo < 0 && dragM < 0) for (int i = 0; i < BV.Length; i++) if (SRB(RBill(i)).Contains(mouse) && stock[i] > 0) { dragB = i; stock[i]--; e.Use(); break; }
            // 4. la vista de la ruleta: nodos y mover desde cualquier lugar libre
            if (dragF == null && !est && dragB < 0 && modo < 0 && dragM < 0 && !vRul.fija && editando)
            {
                if ((h = HitNodo(vRul, mouse)) >= 0) { vAct = vRul; modo = h; m0 = mouse; r0 = vRul.r; e.Use(); }
                else if (vRul.r.Contains(mouse)) { vAct = vRul; modo = 8; off = vRul.r.position - mouse; e.Use(); }
            }
        }
        else if (e.type == EventType.MouseUp && e.button == 0) Soltar(mouse, config, panoVisible);

        // el arrastre se sostiene mirando el mouse cada cuadro (los eventos de arrastre no siempre llegan)
        if (e.type == EventType.Repaint)
        {
            bool algo = modo >= 0 || dragF != null || dragB >= 0 || dragM >= 0;
            if (algo)
            {
                if (vAct != null && modo == 8) { vAct.r.position = mouse + off; Acomodar(vAct); }
                else if (vAct != null && modo >= 0) Redimensionar(vAct, modo, mouse);
                if (!Apretado()) { sinBoton++; if (sinBoton >= 2) { Soltar(mouse, config, panoVisible); sinBoton = 0; } } else sinBoton = 0;
            }
            else sinBoton = 0;
        }
    }

    // suelta lo que se lleva agarrado en la posicion dada
    void Soltar(Vector2 pos, bool config, bool panoVisible)
    {
        if (vAct != null) Guardar(vAct);   // lo que movi o agrande queda guardado
        if (vAct == vFic) ordenPend = true;   // si cambio de lugar o de tamaño, las pilas se acomodan de nuevo
        modo = -1; vAct = null; bool hubo = dragF != null || dragB >= 0;
        if (dragM >= 0)
        {
            var m = marcas[dragM]; dragM = -1;
            if (panoVisible && vPano.r.Contains(pos)) { m.puesta = true; var q = APano(pos); m.uv = new Vector2((q.x - PR.x) / PR.width, (q.y - PR.y) / PR.height); }
            else m.puesta = false;
            GuardarChances();
        }
        if (dragF != null)
        {
            var f = dragF; var p = pos + off; dragF = null;
            if (f.placa) { if (!config && (cajaVis || editando) && vVen.r.Contains(p)) { fichas.Remove(f); ventPla.Add(f.v); } else if (config || !vPla.r.Contains(p)) f.p = f.pPrev; Ordenar(); }
            else if (!config && (cajaVis || editando) && vVen.r.Contains(p)) { fichas.Remove(f); apuesta.Remove(f); ventPla.Add(f.v); Ordenar(); }
            else if (panoVisible && vPano.r.Contains(p)) { Sfx(SFX_APOYAR); f.pano = true; { var q = APano(p); f.uv = new Vector2((q.x - PR.x) / PR.width, (q.y - PR.y) / PR.height); } if (!apuesta.Contains(f)) apuesta.Add(f); estampa = f.v; var ap = Cubre(f.uv); if (ap != null) Aviso("$" + f.v + " a " + ap.nombre + ": paga " + ap.pago + " a 1"); Badge(f); }
            else if (!config && !f.pano && vFic.r.Contains(p) && RomperFicha(f, LPF(p).x)) { }   // ficha grande soltada sobre una pila mas chica: se cambia por esas
            else if (!config && vFic.r.Contains(p)) { estampa = 0; f.pano = false; apuesta.Remove(f); var l = LPF(p); f.p = new Vector2(Mathf.Clamp(l.x, CS / 2f, FIC_W - CS / 2f), Mathf.Clamp(l.y, 36f + CS / 2f, RFich.yMax - 30f)); }
            else { estampa = 0; f.pano = f.panoPrev; f.p = f.pPrev; f.uv = f.uvPrev; }
        }
        if (dragB >= 0)
        {
            int romper = -1;   // soltar un billete grande sobre uno mas chico de la caja lo cambia (100 sobre 50 = dos de 50)
            if (CAJA && !config && !modoMarcas && (cajaVis || editando) && !SRV(RDrop).Contains(pos)) for (int j = 0; j < BV.Length; j++) if (j != dragB && BV[j] < BV[dragB] && BV[dragB] % BV[j] == 0 && SRB(RBill(j)).Contains(pos)) { romper = j; break; }
            if (romper >= 0) { int n = BV[dragB] / BV[romper]; stock[romper] += n; Aviso("Cambiaste $" + BV[dragB] + " por " + n + " de $" + BV[romper]); }
            else if (!config && SRV(RDrop).Contains(pos)) vent.Add(BV[dragB]); else stock[dragB]++;
            dragB = -1; hubo = true;
        }
        if (hubo) GuardarMesa();
    }

    // ---------------------------------------------------------------- configuracion (la dibuja la ruleta dentro de su panel de ajustes)
    public void DibujarConfig(float x, float y, GUIStyle sB, GUIStyle sT)
    {
        Iniciar();
        GUI.Label(new Rect(x, y, 420, 24), "Ventanas (están clavadas; acá se acomodan)", new GUIStyle(sT) { fontSize = 16 });
        if (GUI.Button(new Rect(x, y + 28, 296, 32), "ACOMODAR VENTANAS (muestra los nodos)", sB)) editando = true;
        if (GUI.Button(new Rect(x, y + 64, 296, 28), "ORDENAR TODO (disposición automática)", sB)) PedirOrdenar();
    }

    static bool PosMouse(out Vector2 p)
    {
#if ENABLE_INPUT_SYSTEM
        var m = UnityEngine.InputSystem.Mouse.current; if (m == null) { p = Vector2.zero; return false; }
        var v = m.position.ReadValue(); p = new Vector2(v.x, Screen.height - v.y);
#else
        p = new Vector2(Input.mousePosition.x, Screen.height - Input.mousePosition.y);
#endif
        if (VW > 0f) p = (p - OrigenV) / Mathf.Max(0.01f, EscV);   // dentro de la tablet: de pantalla al lienzo virtual
        return true;
    }
    static bool Apretado()
    {
#if ENABLE_INPUT_SYSTEM
        var m = UnityEngine.InputSystem.Mouse.current; return m != null && m.leftButton.isPressed;
#else
        return Input.GetMouseButton(0);
#endif
    }
    void Ficha1(Ficha f)
    {
        var p = PosFicha(f); float s = TamFicha(f);
        if (f.placa) { DibujarPlaca(new Rect(p.x - s / 2, p.y - s * PLH / PLW / 2, s, s * PLH / PLW), f.v); return; }
        if (f.ganadora) { var gc = GUI.color; GUI.color = new Color(1f, 0.85f, 0.2f, 0.85f); GUI.DrawTexture(new Rect(p.x - s * 0.66f, p.y - s * 0.66f, s * 1.32f, s * 1.32f), Disco()); GUI.color = gc; }
        if (!f.pano) { DibujarFicha3D(p, s, f.v); return; }
        var t = Tex(f.v); if (t != null) GUI.DrawTexture(new Rect(p.x - s / 2, p.y - s / 2, s, s), t);
    }
    static Color Osc(Color c, float k) { return new Color(c.r * k, c.g * k, c.b * k, 1f); }
    static string Fmt(int v) { string t = v.ToString(); for (int i = t.Length - 3; i > 0; i -= 3) t = t.Insert(i, "."); return t; }
    // placa rectangular de plastico con marco de color, remaches, rombos en las esquinas y una etiqueta de papel con el valor
    void DibujarPlaca(Rect r, int v)
    {
        int i = System.Array.IndexOf(PL, v); if (i < 0) i = 0;
        Color[] cols = { new Color(0.45f, 0.72f, 0.9f), new Color(0.35f, 0.75f, 0.45f), new Color(0.95f, 0.6f, 0.25f), new Color(0.72f, 0.5f, 0.88f) }; Color c = cols[i];
        Fondo(new Rect(r.x + 2, r.y + 3, r.width, r.height), new Color(0f, 0f, 0f, 0.35f));
        Fondo(r, Osc(c, 0.62f)); Fondo(new Rect(r.x + r.width * 0.012f, r.y + r.height * 0.02f, r.width * 0.976f, r.height * 0.96f), c);
        var gc = GUI.color; GUI.color = new Color(Mathf.Min(1f, c.r + 0.18f), Mathf.Min(1f, c.g + 0.18f), Mathf.Min(1f, c.b + 0.18f));
        float d = r.height * 0.09f;
        for (int k = 0; k < 4; k++) { float x = r.x + r.width * (0.17f + k * 0.22f); GUI.DrawTexture(new Rect(x - d / 2, r.y + r.height * 0.09f - d / 2, d, d), Disco()); GUI.DrawTexture(new Rect(x - d / 2, r.yMax - r.height * 0.09f - d / 2, d, d), Disco()); }
        for (int k = 0; k < 3; k++) { float y = r.y + r.height * (0.28f + k * 0.22f); GUI.DrawTexture(new Rect(r.x + r.width * 0.045f - d / 2, y - d / 2, d, d), Disco()); GUI.DrawTexture(new Rect(r.xMax - r.width * 0.045f - d / 2, y - d / 2, d, d), Disco()); }
        GUI.color = gc; float q = r.height * 0.07f;
        foreach (var cx in new[] { r.x + r.width * 0.035f, r.xMax - r.width * 0.035f - q }) foreach (var cy in new[] { r.y + r.height * 0.05f, r.yMax - r.height * 0.05f - q }) Fondo(new Rect(cx, cy, q, q), Osc(c, 0.4f));
        var L = new Rect(r.x + r.width * 0.13f, r.y + r.height * 0.21f, r.width * 0.74f, r.height * 0.58f);
        Fondo(new Rect(L.x - 2, L.y - 2, L.width + 4, L.height + 4), new Color(0.25f, 0.12f, 0.1f)); Fondo(L, new Color(0.93f, 0.91f, 0.86f));
        Fondo(new Rect(L.x + L.width * 0.04f, L.y + L.height * 0.06f, L.width * 0.92f, L.height * 0.88f), new Color(c.r * 0.4f + 0.6f, c.g * 0.4f + 0.6f, c.b * 0.4f + 0.6f, 0.45f));
        var st = new GUIStyle(sTc) { alignment = TextAnchor.MiddleCenter, fontSize = Mathf.Max(7, Mathf.RoundToInt(L.height * 0.42f)), fontStyle = FontStyle.Bold }; st.normal.textColor = new Color(0.1f, 0.1f, 0.12f);
        var s2 = new GUIStyle(st) { fontSize = Mathf.Max(6, Mathf.RoundToInt(L.height * 0.15f)), fontStyle = FontStyle.Normal };
        GUI.Label(L, Fmt(v), st);
        GUI.Label(new Rect(L.x + L.width * 0.05f, L.y + L.height * 0.04f, L.width * 0.3f, L.height * 0.22f), "SC", new GUIStyle(s2) { alignment = TextAnchor.MiddleLeft, fontStyle = FontStyle.Bold });
        GUI.Label(new Rect(L.x + L.width * 0.4f, L.y + L.height * 0.04f, L.width * 0.56f, L.height * 0.22f), PLN[i], new GUIStyle(s2) { alignment = TextAnchor.MiddleRight });
        GUI.Label(new Rect(L.x + L.width * 0.05f, L.y + L.height * 0.74f, L.width * 0.5f, L.height * 0.22f), PLN[i], new GUIStyle(s2) { alignment = TextAnchor.MiddleLeft });
        GUI.Label(new Rect(L.x + L.width * 0.5f, L.y + L.height * 0.74f, L.width * 0.46f, L.height * 0.22f), "0" + (12000 + v / 1000 * 7).ToString(), new GUIStyle(s2) { alignment = TextAnchor.MiddleRight });
        var s3 = new GUIStyle(s2) { fontSize = Mathf.Max(5, Mathf.RoundToInt(r.height * 0.08f)) }; s3.normal.textColor = Osc(c, 0.45f);
        GUI.Label(new Rect(r.x, r.y + r.height * 0.005f, r.width, r.height * 0.17f), "SKY COMMANDER", s3); GUI.Label(new Rect(r.x, r.yMax - r.height * 0.175f, r.width, r.height * 0.17f), "CASINO", s3);
    }
    List<KeyValuePair<int, bool>> Componer(int monto, bool grande, int d)   // valor y si es placa
    {
        var l = new List<KeyValuePair<int, bool>>(); int resto = monto;
        if (!grande) { int n = Mathf.Min(20, resto / d); for (int k = 0; k < n; k++) l.Add(new KeyValuePair<int, bool>(d, false)); resto -= n * d; }   // en cambio: hasta 20 fichas de lo que jugabas, el resto en placas
        for (int i = PL.Length - 1; i >= 0; i--) { int n = resto / PL[i]; resto -= n * PL[i]; for (int k = 0; k < n; k++) l.Add(new KeyValuePair<int, bool>(PL[i], true)); }
        for (int i = 7; i >= 0; i--) { int n = resto / CV[i]; resto -= n * CV[i]; for (int k = 0; k < n; k++) l.Add(new KeyValuePair<int, bool>(CV[i], false)); }
        return l;
    }
    string Resumen(List<KeyValuePair<int, bool>> l)
    {
        var orden = new List<KeyValuePair<int, bool>>(); var cuenta = new List<int>();
        foreach (var kv in l) { int ix = orden.FindIndex(x => x.Key == kv.Key && x.Value == kv.Value); if (ix < 0) { orden.Add(kv); cuenta.Add(1); } else cuenta[ix]++; }
        var sb = new System.Text.StringBuilder(); for (int i = 0; i < orden.Count; i++) { if (i > 0) sb.Append("  +  "); sb.Append(cuenta[i] + (orden[i].Value ? (cuenta[i] == 1 ? " placa de $" : " placas de $") : (cuenta[i] == 1 ? " ficha de $" : " fichas de $")) + Fmt(orden[i].Key)); }
        return sb.ToString();
    }
    void Entregar(List<KeyValuePair<int, bool>> l) { foreach (var kv in l) fichas.Add(new Ficha { v = kv.Key, placa = kv.Value }); Ordenar(); Aviso("Cobraste $" + Fmt(pagoPend)); pagoPend = 0; }
    void DibujarPago(GUIStyle sT, GUIStyle sB0)   // cartel de cobro: en grande (lo mas grande posible) o en cambio (partido en fichas mas chicas, a elegir)
    {
        var ops = new List<List<KeyValuePair<int, bool>>>(); var txt = new List<string>(); var rot = new List<string>();
        var g = Componer(pagoPend, true, pagoDen); string tg = Resumen(g); ops.Add(g); txt.Add(tg); rot.Add("EN GRANDE");
        for (int i = 7; i >= 0; i--)
        {
            if (CV[i] > pagoPend) continue; var l = Greedy(pagoPend, i); if (l.Count > 40) continue;
            string t = Resumen(l); if (txt.Contains(t)) continue; ops.Add(l); txt.Add(t); rot.Add("EN CAMBIO");
        }
        float W = Mathf.Min(SW - 40f, 560f), H = 104f + ops.Count * 46f; var rm = new Rect((SW - W) / 2f, Mathf.Max(10f, (SH - H) / 2f), W, H);
        Fondo(new Rect(0, 0, SW, SH), new Color(0f, 0f, 0f, 0.6f)); Fondo(rm, new Color(0.07f, 0.2f, 0.12f, 0.98f));
        GUI.Label(new Rect(rm.x + 16, rm.y + 8, W - 32, 44), "¡Salió el " + pagoRes + "!   Ganaste $" + Fmt(pagoPend), new GUIStyle(sT) { fontSize = 24 });
        GUI.Label(new Rect(rm.x + 16, rm.y + 46, W - 32, 26), "¿Cómo querés cobrar?   (tu postura queda en el paño)", new GUIStyle(sT) { fontSize = 15 });
        var sb = new GUIStyle(sB0) { fontSize = 16 };
        for (int k = 0; k < ops.Count; k++) if (GUI.Button(new Rect(rm.x + 16, rm.y + 80 + k * 46, W - 32, 40), rot[k] + ":   " + txt[k], sb)) { Entregar(ops[k]); return; }
    }
    static void Fondo(Rect r, Color c) { var cc = GUI.color; GUI.color = c; GUI.DrawTexture(r, Texture2D.whiteTexture); GUI.color = cc; }
}
