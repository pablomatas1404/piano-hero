// SHOPPING + MALETIN: en que gastar la plata de las misiones. Dos pestanas del panel de abajo ("Shopping" y "Maletin").
// Lo comprado se guarda en el perfil del piloto (Carrera.cs). Autos, motos y casas por ahora solo se compran y quedan en el maletin;
// cuando haya modelos y casas, se vinculan por el id del articulo.
using UnityEngine;
using System.Collections.Generic;

public static class Tienda
{
    public class Art { public string id, cat, nombre, desc; public int precio; }
    static List<Art> catalogo;
    static string filtro = "Mi casa"; static Vector2 scrollS, scrollM; static string filtroM = "Todo";
    static GUIStyle sTxtW, sNom, sNomC, sPrecio, sNomG;

    public static readonly string[] CATEGORIAS = { "Concesionaria", "Mi casa", "Tecnologia", "Ropa", "Calzado", "Joyas y relojes", "Vehiculos", "Hogar", "Lujo y vida", "Viajes" };

    static void A(string cat, string nombre, string desc, int precio) { catalogo.Add(new Art { id = cat + "|" + nombre, cat = cat, nombre = nombre, desc = desc, precio = precio }); }

    static void Armar()
    {
        if (catalogo != null) return;
        catalogo = new List<Art>();
        // ---- MI CASA (19/11, prueba): al comprarlos aparecen en el living de la casa de Campo de Mayo (Casa.cs los muestra por este mismo nombre)
        string C = "Mi casa";
        A(C, "Planta grande", "Planta de interior en maceta. Le da vida al living.", 60);
        A(C, "Ventilador de techo", "Cuatro aspas, para el verano.", 150);
        A(C, "Sillon de lectura", "Sillon de cuero de los anos 60, para leer tranquilo.", 250);
        A(C, "Sillon de 3 cuerpos", "Sillon clasico tapizado, entra toda la familia.", 300);
        // ---- tecnologia
        string T = "Tecnologia";
        A(T, "Parlante bluetooth", "Suena fuerte y se carga por USB.", 80);
        A(T, "Auriculares inalambricos", "Con cancelacion de ruido.", 200);
        A(T, "Smartphone gama media", "Cumple con todo, sin lujos.", 350);
        A(T, "Smartwatch", "Cuenta pasos y avisa los mensajes.", 300);
        A(T, "iPad Pro 12.9 pulgadas", "Pantalla Liquid Retina XDR, para series y juegos en el sillon.", 400);
        A(T, "Barra de sonido home theater", "Cine en el living.", 350);
        A(T, "Smart TV 55 pulgadas", "4K con apps de streaming.", 600);
        A(T, "Drone con camara", "Para filmar desde arriba, como en el simulador.", 700);
        A(T, "Consola Xbox", "La de la casa de Pablo.", 550);
        A(T, "Consola PlayStation", "Con dos joysticks.", 600);
        A(T, "iPhone Pro", "256 GB, camara triple.", 1200);
        A(T, "Smart TV 65 pulgadas 4K", "Colores increibles.", 900);
        A(T, "Notebook de marca", "Liviana, ideal para trabajar.", 1000);
        A(T, "iPhone Pro Max", "El mas grande, con 1 TB.", 1500);
        A(T, "Camara profesional", "Lentes intercambiables.", 2500);
        A(T, "PC gamer", "Placa de video de ultima generacion.", 2500);
        A(T, "Smart TV 85 pulgadas", "Ocupa media pared. Pantalla enorme.", 2500);
        // ---- ropa
        string R = "Ropa";
        A(R, "Remera de marca", "Algodon peinado, basica y comoda.", 40);
        A(R, "Jean de marca", "Corte recto, azul oscuro.", 90);
        A(R, "Buzo con capucha", "Para el invierno.", 80);
        A(R, "Campera de cuero", "Negra, estilo aviador.", 350);
        A(R, "Campera de aviador de piloto", "Con parches y cuello de lana. Para volar con onda.", 500);
        string[] marcas = { "Armani", "Hugo Boss", "Zegna", "Tom Ford", "Brooks Brothers" };
        int[] pm = { 2500, 900, 3000, 4500, 800 };
        string[] colores = { "azul marino", "negro", "gris", "bordo" };
        for (int i = 0; i < marcas.Length; i++) for (int j = 0; j < colores.Length; j++) A(R, "Traje " + marcas[i] + " " + colores[j], "Traje de tres piezas, color " + colores[j] + ".", pm[i] + j * 20);
        // ---- calzado
        string Z = "Calzado";
        string[] zm = { "Nike", "Adidas", "Puma", "New Balance", "Converse" };
        int[] pz = { 140, 120, 90, 130, 80 };
        string[] zc = { "blancas", "negras", "rojas" };
        for (int i = 0; i < zm.Length; i++) for (int j = 0; j < zc.Length; j++) A(Z, "Zapatillas " + zm[i] + " " + zc[j], "Zapatillas " + zc[j] + " para todos los dias.", pz[i] + j * 5);
        A(Z, "Zapatos de vestir negros", "Cuero, para los trajes.", 220);
        A(Z, "Zapatos de vestir marrones", "Cuero con suela flexible.", 220);
        A(Z, "Botas de cuero", "Resistentes, para el campo.", 250);
        A(Z, "Mocasines de gamuza", "Livianos y elegantes.", 200);
        // ---- joyas y relojes
        string J = "Joyas y relojes";
        A(J, "Anteojos de sol de marca", "Polarizados, clasicos de aviador.", 200);
        A(J, "Cadena de plata", "Gruesa y brillante.", 150);
        A(J, "Reloj de acero", "Automatico, resistente al agua.", 800);
        A(J, "Anillo de oro", "Oro de 18 quilates.", 700);
        A(J, "Reloj de oro", "El que va con traje. Pesa y se nota.", 15000);
        A(J, "Reloj suizo de lujo", "Edicion limitada, mecanismo a cuerda.", 30000);
        A(J, "Rolex Lady-Datejust", "Elegante, con esfera de nacar. Un clasico.", 25000);
        A(J, "Rolex Daytona", "El cronografo mas deseado del mundo.", 60000);
        A(J, "Collar de diamantes", "Para una ocasion muy especial.", 20000);
        // ---- vehiculos (por ahora solo se compran: despues se podran usar en el juego)
        string V = "Vehiculos";
        A(V, "Bicicleta de paseo", "Con canasto. Para ir a comprar el pan.", 300);
        A(V, "Bicicleta electrica", "Pedalea ella sola en las subidas.", 1200);
        A(V, "Moto 110 cc", "Economica, anda a cualquier lado.", 2500);
        A(V, "Moto de enduro", "Para caminos de tierra y aventura.", 6000);
        A(V, "Auto usado (el pedorro)", "Anda de milagro, pero es tuyo.", 4000);
        A(V, "Moto deportiva", "Carenado, doble escape.", 9000);
        A(V, "Auto compacto", "Chico, confiable y con aire.", 14000);
        A(V, "Camioneta 4x4", "Para ir por tierra sin problema.", 40000);
        A(V, "BMW Serie 3", "Sedan deportivo, manejo de pura precision.", 55000);
        A(V, "Mercedes CLE Coupe", "SUV coupe 4MATIC, lujo y potencia.", 90000);
        A(V, "Lancha", "Para el Tigre y el Rio de la Plata.", 60000);
        A(V, "Auto deportivo", "Dos puertas, motor V8.", 80000);
        A(V, "Porsche 911", "El que ya conoces.", 150000);
        A(V, "Helicoptero privado", "Con helipuerto incluido en el techo.", 450000);
        // ---- hogar
        string H = "Hogar";
        A(H, "Heladera no frost", "Doble puerta, con dispenser.", 900);
        A(H, "Cama king size", "Colchon de espuma viscoelastica.", 800);
        A(H, "Sillon de living", "Cuero, de tres cuerpos.", 900);
        A(H, "Cuadro de arte", "Original de un pintor argentino.", 1500);
        A(H, "Departamento de 1 ambiente (alquiler)", "Chico, luminoso y bien ubicado.", 400);
        A(H, "Departamento de 2 ambientes", "Con balcon a la calle.", 90000);
        A(H, "Casa con pileta", "Tres dormitorios, quincho y parrilla.", 250000);
        A(H, "Quinta en el campo", "Con pista de aterrizaje propia.", 350000);
        A(H, "Mansion", "Seis dormitorios, cine y garage para diez autos.", 1500000);
        // ---- lujo y vida
        string L = "Lujo y vida";
        A(L, "Gato", "Duerme todo el dia.", 50);
        A(L, "Perro", "Te espera en la puerta.", 300);
        A(L, "Cena en restaurante de lujo", "Menu degustacion de siete pasos.", 250);
        A(L, "Guitarra electrica", "Con amplificador y pedalera.", 900);
        A(L, "Entrada a la final del mundial", "Platea, al lado de la cancha.", 1500);
        A(L, "Crucero por el Caribe", "Diez dias con todo incluido.", 4500);
        A(L, "Piano de cola", "Para tocar en el living.", 40000);
        A(L, "Hangar propio", "Con tu nombre en el porton.", 150000);
        // ---- viajes con descuento de empleado de aerolinea (pasaje + alojamiento)
        string W = "Viajes";
        A(W, "Viaje a Mar del Plata", "3 dias de playa. Pasaje con descuento de empleado.", 250);
        A(W, "Viaje al Norte Argentino", "Cerros de colores, Salta y Jujuy.", 600);
        A(W, "Viaje a Canasvieiras (Brasil)", "Playa tranquila y verano largo.", 650);
        A(W, "Viaje a Punta del Este", "Fin de semana largo del otro lado del rio.", 400);
        A(W, "Viaje a Mendoza", "Ruta del vino y cordillera.", 450);
        A(W, "Viaje a Bariloche", "Una semana de nieve y chocolate.", 600);
        A(W, "Viaje a Cataratas del Iguazu", "Las cataratas mas impresionantes.", 500);
        A(W, "Viaje a El Calafate", "Glaciar Perito Moreno.", 700);
        A(W, "Viaje a Ushuaia", "El fin del mundo en un avion.", 700);
        A(W, "Viaje a Rio de Janeiro", "Una semana en Copacabana.", 800);
        A(W, "Viaje a Florianopolis", "Playas y sol, todo el verano.", 700);
        A(W, "Viaje a Santiago de Chile", "Cordillera y buena comida.", 500);
        A(W, "Viaje a Cusco y Machu Picchu", "Historia inca en las alturas.", 1300);
        A(W, "Viaje a Cancun", "Mar turquesa y todo incluido.", 1400);
        A(W, "Viaje a Miami", "Shopping y playa. Pasaje con 70% de descuento.", 1500);
        A(W, "Viaje a Cordoba en jet privado", "Sierras y asado, llegando en tu propio jet.", 1500);
        A(W, "Viaje a Punta del Este en jet privado", "Aterrizas en la Barra, sin filas.", 1800);
        A(W, "Viaje a Orlando", "Todos los parques tematicos.", 1700);
        A(W, "Viaje a Nueva York", "Rascacielos y Broadway.", 2000);
        A(W, "Viaje a Madrid", "Tapas, museos y noches largas.", 2200);
        A(W, "Viaje a Paris", "La torre Eiffel de noche.", 2500);
        A(W, "Viaje a Roma", "Historia a cada paso.", 2300);
        A(W, "Viaje a Italia 10 dias", "Roma, Florencia, Venecia y la costa.", 3200);
        A(W, "Viaje a Tokio", "Tecnologia, sushi y templos.", 3500);
        A(W, "Viaje a Dubai y Maldivas", "Rascacielos y bungalows sobre el mar.", 5000);
        A(W, "Gran viaje por Europa", "El viaje soñado, de punta a punta.", 6000);
        A(W, "Viaje a Sidney", "Del otro lado del mundo.", 4000);
    }

    // ---- fotos: Assets/Resources/Tienda/<clave>.jpg; mapa.txt dice que texto del nombre usa que foto (creditos en CREDITOS.json)
    static List<string[]> mapaFotos; static readonly Dictionary<string, Texture2D> cacheFotos = new Dictionary<string, Texture2D>();
    // ---- fotos del usuario: carpeta "fotos_tienda" (junto a "perfiles"); el archivo se llama como el articulo, ej: "Smart TV 85 pulgadas.jpg". Pisan a las incluidas.
    static string CarpetaFotos { get { return System.IO.Path.GetFullPath(System.IO.Path.Combine(Application.dataPath, "..", "..", "fotos_tienda")); } }
    static readonly Dictionary<string, string> externas = new Dictionary<string, string>();   // nombre articulo (minuscula) -> ruta
    static readonly Dictionary<string, Texture2D> cacheExt = new Dictionary<string, Texture2D>(); static readonly Dictionary<string, long> mtExt = new Dictionary<string, long>();
    static float proxEscaneoFotos = -1f;
    static string Limpio(string s) { foreach (char c in System.IO.Path.GetInvalidFileNameChars()) s = s.Replace(c.ToString(), ""); return s.Trim(); }
    static void EscanearFotos()
    {
        if (avisoFoto != null) { var av = avisoFoto; avisoFoto = null; proxEscaneoFotos = -1f; if (av != "") Mapeo.Cartel(av, 3.5f); }
        if (Time.unscaledTime < proxEscaneoFotos) return; proxEscaneoFotos = Time.unscaledTime + 2f;
        try
        {
            externas.Clear(); if (!System.IO.Directory.Exists(CarpetaFotos)) return;
            foreach (var f in System.IO.Directory.GetFiles(CarpetaFotos))
            {
                string e = System.IO.Path.GetExtension(f).ToLowerInvariant(); if (e != ".jpg" && e != ".jpeg" && e != ".png") continue;
                externas[System.IO.Path.GetFileNameWithoutExtension(f).ToLowerInvariant()] = f;
            }
        }
        catch { }
    }
    static Texture2D FotoExterna(Art a)
    {
        EscanearFotos(); string k = Limpio(a.nombre).ToLowerInvariant(); string ruta;
        if (!externas.TryGetValue(k, out ruta)) return null;
        try
        {
            long mt = System.IO.File.GetLastWriteTimeUtc(ruta).Ticks; Texture2D t0;
            if (cacheExt.TryGetValue(k, out t0) && t0 != null && mtExt[k] == mt) return t0;
            var tx = new Texture2D(2, 2, TextureFormat.RGBA32, false); tx.LoadImage(System.IO.File.ReadAllBytes(ruta));
            cacheExt[k] = tx; mtExt[k] = mt; return tx;
        }
        catch { return null; }
    }
    static volatile string avisoFoto; static volatile bool eligiendoFoto;
    // abre el selector de archivos de Windows; la imagen elegida se copia sola a fotos_tienda con el nombre del articulo
    static void AbrirCarpetaFotos(Art a)
    {
        if (eligiendoFoto) return; eligiendoFoto = true;
        string nombre = Limpio(a.nombre), carpeta = CarpetaFotos;
        var th = new System.Threading.Thread(() =>
        {
            try
            {
                System.IO.Directory.CreateDirectory(carpeta);
                var psi = new System.Diagnostics.ProcessStartInfo("powershell.exe", "-NoProfile -STA -WindowStyle Hidden -Command \"Add-Type -AssemblyName System.Windows.Forms; $d=New-Object System.Windows.Forms.OpenFileDialog; $d.Title='Foto para: " + nombre.Replace("'", "") + "'; $d.Filter='Imagenes|*.jpg;*.jpeg;*.png;*.webp;*.bmp'; if($d.ShowDialog() -eq 'OK'){ [Console]::Out.Write($d.FileName) }\"")
                { UseShellExecute = false, RedirectStandardOutput = true, CreateNoWindow = true };
                string ruta; using (var p = System.Diagnostics.Process.Start(psi)) { ruta = p.StandardOutput.ReadToEnd().Trim(); p.WaitForExit(); }
                if (string.IsNullOrEmpty(ruta) || !System.IO.File.Exists(ruta)) { avisoFoto = ""; return; }
                string ext = System.IO.Path.GetExtension(ruta).ToLowerInvariant();
                if (ext != ".jpg" && ext != ".jpeg" && ext != ".png") { avisoFoto = "Ese formato no lo puedo leer: usa una imagen JPG o PNG."; return; }
                foreach (var viejo in System.IO.Directory.GetFiles(carpeta)) { if (System.IO.Path.GetFileNameWithoutExtension(viejo).ToLowerInvariant() == nombre.ToLowerInvariant()) System.IO.File.Delete(viejo); }
                System.IO.File.Copy(ruta, System.IO.Path.Combine(carpeta, nombre + ext), true);
                avisoFoto = "FOTO CAMBIADA: " + nombre;
            }
            catch { avisoFoto = "No pude cambiar la foto."; }
            finally { eligiendoFoto = false; }
        });
        th.IsBackground = true; th.SetApartmentState(System.Threading.ApartmentState.MTA); th.Start();
    }
    static Texture2D Foto(Art a)
    {
        var ext = FotoExterna(a); if (ext != null) return ext;
        if (mapaFotos == null)
        {
            mapaFotos = new List<string[]>(); var ta = Resources.Load<TextAsset>("Tienda/mapa");
            if (ta != null) foreach (var l in ta.text.Split('\n')) { var p = l.Trim().Split('|'); if (p.Length == 2) mapaFotos.Add(p); }
        }
        string clave = null; foreach (var p in mapaFotos) if (a.nombre.Contains(p[0])) { clave = p[1]; break; }
        if (clave == null) return null;
        Texture2D t; if (!cacheFotos.TryGetValue(clave, out t)) { t = Resources.Load<Texture2D>("Tienda/" + clave); cacheFotos[clave] = t; }
        return t;
    }

    // ---- precios editables (+/- en cada tarjeta); se guardan en precios_tienda.json junto a precios_torre.json
    class PrJ { public string id; public int precio; }
    class ListaPrJ { public PrJ[] items; }
    static Dictionary<string, int> ajustes;
    static string RutaPrecios { get { return System.IO.Path.Combine(System.IO.Path.GetFullPath(System.IO.Path.Combine(Application.dataPath, "..", "..")), "precios_tienda.json"); } }
    static void CargarAjustes()
    {
        if (ajustes != null) return; ajustes = new Dictionary<string, int>();
        try { if (System.IO.File.Exists(RutaPrecios)) { var l = JsonUtility.FromJson<ListaPrJ>(System.IO.File.ReadAllText(RutaPrecios)); if (l != null && l.items != null) foreach (var p in l.items) ajustes[p.id] = p.precio; } } catch { }
    }
    static void GuardarAjustes()
    {
        try { var v = new List<PrJ>(); foreach (var kv in ajustes) v.Add(new PrJ { id = kv.Key, precio = kv.Value }); System.IO.File.WriteAllText(RutaPrecios, JsonUtility.ToJson(new ListaPrJ { items = v.ToArray() }, true)); } catch { }
    }
    public static int Precio(Art a) { CargarAjustes(); int p; return ajustes.TryGetValue(a.id, out p) ? p : a.precio; }
    static void CambiarPrecio(Art a, int dir)
    {
        int p = Precio(a), paso = 1;   // de a $1, para que el precio coincida justo con el cartel
        ajustes[a.id] = Mathf.Max(0, p + dir * paso); GuardarAjustes();
    }

    // ---- ancho disponible: lo fija el panel lateral grande (AvionDeCarton) antes de dibujar
    public static class PanelLateral { public static Rect rect = new Rect(0, 0, 0, 0); public static float ancho = -1f; public static float Ancho() { return ancho > 0f ? ancho : Mathf.Max(300f, Screen.width - 500f); } }

    static float TARJ_W { get { return Mathf.Clamp((PanelLateral.Ancho() - 24f) / 10f - 6f, 92f, 126f); } } const float TARJ_H = 236f;
    static float SHOP_W { get { return Mathf.Clamp((PanelLateral.Ancho() - 30f) / 4f - 6f, 150f, 340f); } }   // 4 tarjetas por fila, fotos grandes
    const string FALTAN = "Faltan fotos";
    // "tiene foto" = una foto elegida por Pablo (carpeta fotos_tienda); las incluidas de antes cuentan como provisorias y van a Faltan fotos
    // boton que repite mientras lo mantenes apretado: al principio de a uno, despues cada vez mas rapido (para subir/bajar precios)
    static string repId = ""; static float repDesde, repProx;
    public static bool Repetir(string id, bool sostenido)
    {
        if (!sostenido) { if (repId == id) repId = ""; return false; }
        float ahora = Time.unscaledTime;
        if (repId != id) { repId = id; repDesde = ahora; repProx = ahora + 0.4f; return true; }
        if (ahora < repProx) return false;
        float h = ahora - repDesde; repProx = ahora + (h < 1.5f ? 0.1f : (h < 3f ? 0.05f : 0.02f)); return true;
    }
    static bool TieneFoto(Art a) { EscanearFotos(); return externas.ContainsKey(Limpio(a.nombre).ToLowerInvariant()); }
    static string Etiqueta(string c) { switch (c) { case "Vehiculos": return "Autos y motos"; case "Joyas y relojes": return "Lujo y relojes"; default: return c; } }
    static void FotoGrande(Art a, Texture2D foto, float w, float h)
    {
        var r = GUILayoutUtility.GetRect(w, h, GUILayout.Width(w), GUILayout.Height(h));
        if (Event.current.type == EventType.Repaint)
        {
            var c0 = GUI.color; GUI.color = foto != null ? Color.white : new Color(0.22f, 0.24f, 0.3f); GUI.DrawTexture(r, Texture2D.whiteTexture); GUI.color = c0;
            if (foto != null) GUI.DrawTexture(new Rect(r.x + 6f, r.y + 6f, r.width - 12f, r.height - 12f), foto, ScaleMode.ScaleToFit, true);
        }
        GUI.Label(r, new GUIContent(foto == null ? "sin foto" : "", a.nombre + ": " + a.desc), sTxtW);
    }

    // ---- pestana SHOPPING (tarjetas verticales con foto: caben unas 10 por fila en el panel lateral)
    public static void PestanaShopping(GUIStyle txt, GUIStyle btn, GUIStyle titulo)
    {
        Armar(); Estilos(txt);
        GUILayout.Label("SHOPPING   (gasta la plata de las misiones; lo que compras queda en la pestana Maletin)", titulo);
        GUILayout.Label("Tu dinero: $" + Carrera.Plata.ToString("N0"), sNom);
        BarraCategorias(btn, ref filtro, true);
        if (filtro == "Concesionaria")   // autos para probar, alquilar y comprar (Concesionaria.cs)
        {
            scrollS = GUILayout.BeginScrollView(scrollS, false, true);
            Concesionaria.Dibujar(txt, btn, PanelLateral.Ancho() - 40f);
            GUILayout.EndScrollView();
            return;
        }
        scrollS = GUILayout.BeginScrollView(scrollS, false, true);
        float sw = SHOP_W, ph = sw * 0.8f;
        int cols = Mathf.Max(1, (int)((PanelLateral.Ancho() - 24f) / (sw + 6f)));
        var lista = filtro == FALTAN ? catalogo.FindAll(a => !TieneFoto(a)) : catalogo.FindAll(a => (filtro == "Todo" || a.cat == filtro) && TieneFoto(a));
        lista.Sort((x, y) => { int d = x.precio.CompareTo(y.precio); return d != 0 ? d : string.CompareOrdinal(x.nombre, y.nombre); });   // orden fijo por el precio original: al tocar +/- la tarjeta no salta de lugar
        if (filtro == FALTAN) GUILayout.Label("Estos articulos todavia no tienen foto (" + lista.Count + "). Toca \"+ Foto\" en cada uno para elegirla.", sNom);
        var cc = GUI.contentColor; var bg = GUI.backgroundColor;
        for (int i = 0; i < lista.Count; i += cols)
        {
            GUILayout.BeginHorizontal();
            for (int j = i; j < i + cols && j < lista.Count; j++)
            {
                var a = lista[j]; bool tiene = Carrera.Tiene(a.id); int pr = Precio(a); bool alcanza = Carrera.Plata >= pr;
                GUILayout.BeginVertical(GUI.skin.box, GUILayout.Width(sw));
                var foto = Foto(a);
                FotoGrande(a, foto, sw - 10f, ph);
                GUILayout.Label(a.nombre, sNomG, GUILayout.Height(40));
                GUI.contentColor = alcanza || tiene ? new Color(0.6f, 1f, 0.6f) : new Color(1f, 0.55f, 0.45f);
                GUILayout.Label("$" + pr.ToString("N0"), sPrecio, GUILayout.Height(30));
                GUI.contentColor = cc;
                GUILayout.BeginHorizontal();
                if (Repetir("m" + a.id, GUILayout.RepeatButton("-", btn, GUILayout.Width(22), GUILayout.Height(30)))) CambiarPrecio(a, -1);
                if (tiene) GUILayout.Label("Tuyo", sNomG, GUILayout.ExpandWidth(true));
                else
                {
                    GUI.backgroundColor = alcanza ? new Color(0.4f, 1f, 0.4f) : new Color(0.55f, 0.4f, 0.4f);
                    if (GUILayout.Button(alcanza ? "COMPRAR" : "Falta", btn, GUILayout.ExpandWidth(true), GUILayout.Height(30)))
                    {
                        if (alcanza && Carrera.Comprar(a.id, pr)) Mapeo.Cartel("COMPRASTE: " + a.nombre + "\n-$" + pr.ToString("N0") + (a.cat == "Mi casa" ? "   (ya esta en el living de tu casa: acomodalo con MODO DECORAR)" : "   (esta en tu Maletin)"), 5f);
                        else Mapeo.Cartel("Te faltan $" + (pr - Carrera.Plata).ToString("N0") + " para comprar " + a.nombre + ".\nHace mas misiones!", 3.5f);
                    }
                    GUI.backgroundColor = bg;
                }
                if (Repetir("p" + a.id, GUILayout.RepeatButton("+", btn, GUILayout.Width(22), GUILayout.Height(30)))) CambiarPrecio(a, 1);
                GUILayout.EndHorizontal();
                if (GUILayout.Button("+ Foto", btn, GUILayout.Height(22))) AbrirCarpetaFotos(a);
                GUILayout.EndVertical();
                GUILayout.Space(4f);
            }
            GUILayout.EndHorizontal();
            GUILayout.Space(6f);
        }
        GUI.contentColor = cc; GUI.backgroundColor = bg;
        GUILayout.EndScrollView();
        GUILayout.Label(string.IsNullOrEmpty(GUI.tooltip) ? "Pasa el mouse sobre una foto para ver la descripcion. Los botones - y + cambian el precio." : GUI.tooltip, sTxtW);
    }

    // ---- pestana MALETIN (la coleccion de lo comprado, con fotos)
    public static void PestanaMaletin(GUIStyle txt, GUIStyle btn, GUIStyle titulo)
    {
        Armar(); Estilos(txt);
        var mios = new List<Art>();
        foreach (var id in Carrera.Compras) { var a = catalogo.Find(x => x.id == id); if (a != null) mios.Add(a); }
        long total = 0; foreach (var a in mios) total += Precio(a);
        GUILayout.Label("MALETIN   (todo lo que compraste)", titulo);
        GUILayout.Label("Articulos: " + mios.Count + "     Valor total: $" + total.ToString("N0") + "     Dinero: $" + Carrera.Plata.ToString("N0"), sNom);
        BarraCategorias(btn, ref filtroM);
        scrollM = GUILayout.BeginScrollView(scrollM, false, true);
        if (mios.Count == 0) GUILayout.Label("Todavia no compraste nada. Hace misiones y pasa por la pestana Shopping.", txt);
        int cols = Mathf.Max(1, (int)((PanelLateral.Ancho() - 24f) / (TARJ_W + 6f)));
        foreach (var cat in CATEGORIAS)
        {
            if (filtroM != "Todo" && filtroM != cat) continue;
            var l = mios.FindAll(a => a.cat == cat); if (l.Count == 0) continue;
            l.Sort((x, y) => Precio(y).CompareTo(Precio(x)));
            var cc = GUI.contentColor; GUI.contentColor = new Color(1f, 0.85f, 0.2f);
            GUILayout.Label(cat.ToUpper() + "  (" + l.Count + ")", sNom); GUI.contentColor = cc;
            for (int i = 0; i < l.Count; i += cols)
            {
                GUILayout.BeginHorizontal();
                for (int j = i; j < i + cols && j < l.Count; j++)
                {
                    var a = l[j];
                    GUILayout.BeginVertical(GUI.skin.box, GUILayout.Width(TARJ_W), GUILayout.Height(150));
                    var f = Foto(a);
                    if (f != null) GUILayout.Box(new GUIContent(f, a.nombre + ": " + a.desc), GUILayout.Width(TARJ_W - 10f), GUILayout.Height(88)); else GUILayout.Box(new GUIContent("sin foto", a.desc), GUILayout.Width(TARJ_W - 10f), GUILayout.Height(88));
                    GUILayout.Label(a.nombre, sNomC, GUILayout.Height(44));
                    GUILayout.EndVertical();
                }
                GUILayout.EndHorizontal();
            }
        }
        GUILayout.EndScrollView();
        GUILayout.Label(string.IsNullOrEmpty(GUI.tooltip) ? "Pasa el mouse sobre una foto para ver la descripcion." : GUI.tooltip, sTxtW);
    }

    static void BarraCategorias(GUIStyle btn, ref string actual, bool conFaltan = false)
    {
        GUILayout.BeginHorizontal();
        var bg = GUI.backgroundColor;
        GUI.backgroundColor = actual == "Todo" ? new Color(0.4f, 1f, 0.4f) : bg; if (GUILayout.Button("Todo", btn, GUILayout.Width(60), GUILayout.Height(22))) actual = "Todo";
        foreach (var c in CATEGORIAS) { GUI.backgroundColor = actual == c ? new Color(0.4f, 1f, 0.4f) : bg; if (GUILayout.Button(Etiqueta(c), btn, GUILayout.Height(22))) actual = c; }
        if (conFaltan) { int nf = 0; foreach (var a in catalogo) if (!TieneFoto(a)) nf++; GUI.backgroundColor = actual == FALTAN ? new Color(0.4f, 1f, 0.4f) : new Color(1f, 0.7f, 0.3f); if (GUILayout.Button(FALTAN + " (" + nf + ")", btn, GUILayout.Height(22))) actual = FALTAN; }
        GUI.backgroundColor = bg;
        GUILayout.EndHorizontal();
    }

    static void Estilos(GUIStyle txt)
    {
        if (sTxtW != null) return;
        sTxtW = new GUIStyle(txt) { wordWrap = true };
        sNom = new GUIStyle(txt) { fontStyle = FontStyle.Bold };
        sPrecio = new GUIStyle(txt) { fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter, fontSize = Mathf.Max(18, txt.fontSize + 6) };
        sNomG = new GUIStyle(txt) { fontStyle = FontStyle.Bold, wordWrap = true, alignment = TextAnchor.MiddleCenter, fontSize = Mathf.Max(13, txt.fontSize + 1) };
        sNomC = new GUIStyle(txt) { fontStyle = FontStyle.Bold, wordWrap = true, fontSize = Mathf.Max(11, txt.fontSize - 1) };
    }
}
