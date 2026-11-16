// Panel "Instrucciones": guia dentro del juego, por categorias. Una parte es para quien JUEGA y otra para quien CONSTRUYE el juego
// (marcar lugares, rutas, ajustar modelos). Las teclas/botones se leen en vivo de Controles, asi que siempre muestran lo que hay asignado.
// Para agregar una instruccion nueva: sumar una categoria en CATEGORIAS y un case en Contenido().
using UnityEngine;
using System.Text;

public class Ayuda : MonoBehaviour
{
    static readonly string[] CATEGORIAS = {
        "Como se juega", "Controles", "Panel de abajo", "Mapa y camara", "Autito y helicoptero", "Radio y sonido", "Creditos",
        "CONSTRUCTOR: lugares", "CONSTRUCTOR: pistas", "CONSTRUCTOR: ruta del autito", "CONSTRUCTOR: modelos", "CONSTRUCTOR: archivos"
    };
    static readonly int PRIMERA_CONSTRUCTOR = 7;

    bool abierto; int cat; Vector2 scroll;
    GUIStyle sTxt, sTit, sBtn, sSel; Texture2D fondo;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Crear() { new GameObject("Ayuda").AddComponent<Ayuda>(); }

    void Estilos()
    {
        if (sTxt != null) return;
        sTxt = new GUIStyle(GUI.skin.label) { fontSize = 14, wordWrap = true, richText = true };
        sTxt.normal.textColor = Color.white;
        sTit = new GUIStyle(sTxt) { fontSize = 18, fontStyle = FontStyle.Bold };
        sTit.normal.textColor = new Color(1f, 0.85f, 0.2f);
        sBtn = new GUIStyle(GUI.skin.button) { fontSize = 13, alignment = TextAnchor.MiddleLeft };
        sSel = new GUIStyle(sBtn); sSel.normal.textColor = new Color(0.5f, 1f, 0.5f); sSel.fontStyle = FontStyle.Bold;
        fondo = new Texture2D(1, 1); fondo.SetPixel(0, 0, new Color(0.04f, 0.05f, 0.08f, 0.96f)); fondo.Apply();
    }

    // texto "Tecla: W   Joystick: Eje 4 (+)" de una accion, tal como esta asignada ahora
    static string Asig(string id)
    {
        var a = Controles.ACCIONES.Find(x => x.id == id);
        if (a == null) return "(sin asignar)";
        return "[b]" + (a.conTecla ? Controles.TextoTecla(a) : "-") + "[/b]  /  joystick: [b]" + Controles.TextoJoystick(a) + "[/b]";
    }

    void OnGUI()
    {
        Estilos();
        float W = Screen.width, H = Screen.height;
        // boton chico arriba, a la izquierda del relojito (lugar libre de la fila de botones)
        if (GUI.Button(new Rect(Mathf.Max(8f, DiaNoche.BarraX(W) - 120f), 4f, 110f, 26f), abierto ? "Cerrar ayuda" : "Instrucciones")) abierto = !abierto;
        if (!abierto) return;

        GUI.depth = -1000;
        var r = new Rect(W * 0.5f - Mathf.Min(520f, W * 0.5f - 20f), 40f, Mathf.Min(1040f, W - 40f), Mathf.Min(H - 80f, 640f));
        GUI.DrawTexture(r, fondo);
        GUI.Label(new Rect(r.x + 12f, r.y + 6f, r.width - 140f, 28f), "INSTRUCCIONES", sTit);
        if (GUI.Button(new Rect(r.xMax - 110f, r.y + 8f, 100f, 24f), "Cerrar")) { abierto = false; return; }

        // columna de categorias (con scroll por si la pantalla es baja)
        float colW = 210f;
        var vista = new Rect(r.x + 8f, r.y + 40f, colW, r.height - 48f);
        float alto = CATEGORIAS.Length * 28f + 24f;
        scrollCat = GUI.BeginScrollView(vista, scrollCat, new Rect(0, 0, colW - 20f, alto));
        float y = 0f;
        for (int i = 0; i < CATEGORIAS.Length; i++)
        {
            if (i == PRIMERA_CONSTRUCTOR) y += 24f;   // separador entre "para jugar" y "para construir"
            if (i == 0) { }
            if (GUI.Button(new Rect(0, y, colW - 22f, 26f), CATEGORIAS[i], cat == i ? sSel : sBtn)) { cat = i; scroll = Vector2.zero; }
            y += 28f;
        }
        GUI.EndScrollView();

        // contenido
        var area = new Rect(r.x + colW + 20f, r.y + 40f, r.width - colW - 30f, r.height - 48f);
        string t = Contenido(cat);
        float ancho = area.width - 24f;
        float h = sTxt.CalcHeight(new GUIContent(t), ancho) + 20f;
        scroll = GUI.BeginScrollView(area, scroll, new Rect(0, 0, ancho, h));
        if (cat >= PRIMERA_CONSTRUCTOR) GUI.Label(new Rect(0, 0, ancho, 24f), "Seccion para quien arma el juego (despues se saca de la version de los jugadores)", sTit);
        GUI.Label(new Rect(0, cat >= PRIMERA_CONSTRUCTOR ? 28f : 0f, ancho, h), t, sTxt);
        GUI.EndScrollView();
    }
    Vector2 scrollCat;

    static string Contenido(int c)
    {
        var s = new StringBuilder();
        switch (c)
        {
            case 0:
                s.AppendLine("[b]COMO SE JUEGA[/b]\n");
                s.AppendLine("1. Al empezar, elegi tu avion (o helicoptero) de la lista. Despues podes cambiarlo con el boton [b]Cambiar avion[/b] de arriba.");
                s.AppendLine("2. Empezas volando sobre la ciudad. Acelera, inclina el avion y mira el mapa de la izquierda para saber donde estas.");
                s.AppendLine("3. Para despegar de una pista: acelera hasta pasar los 50 km/h y tira de la nariz para arriba.");
                s.AppendLine("4. Para aterrizar: baja la velocidad, apuntale a la pista y toca suave. Con [b]Bajar (aterriza antes)[/b] podes dejar el avion y salir en autito.");
                s.AppendLine("5. El velocimetro (arriba a la izquierda) esta en km/h reales.");
                s.AppendLine("\nEn la categoria [b]Controles[/b] ves todas las teclas y botones; los podes cambiar en la pestana [b]Controles[/b] del panel de abajo.");
                break;
            case 1:
                s.AppendLine("[b]CONTROLES[/b]  (se muestran como estan asignados ahora; para cambiarlos: pestana Controles del panel de abajo > Asignar)\n");
                foreach (var a in Controles.ACCIONES)
                {
                    if (EsConstructor(a.id)) continue;
                    s.AppendLine("- " + a.texto + ":  " + Asig(a.id) + (a.migrada ? "" : "   (todavia no hace nada)"));
                }
                s.AppendLine("\nPara cambiar uno: apreta [b]Asignar[/b] al lado de la accion y despues toca el boton del joystick (o la tecla). Esc cancela. [b]Valores de fabrica[/b] vuelve todo a como venia.");
                break;
            case 2:
                s.AppendLine("[b]PANEL DE ABAJO (pestanas)[/b]\n");
                s.AppendLine("Todo lo configurable esta en pestanas del panel de abajo; nada tapa la pantalla. Si no entran las opciones, usa la barra de desplazamiento del costado.\n");
                s.AppendLine("- [b]Pagina 1[/b]: viento (prender/apagar).");
                s.AppendLine("- [b]Pagina 2[/b]: Torre de control: los aeropuertos mas cercanos con su distancia; boton [b]ILS[/b] prende/apaga los aros de aproximacion de ese aeropuerto (\"Sin ILS\" = todavia no tiene datos); precio por distancia con - y + de a $25 (todavia no se cobra; el boton Ir llega con el sistema de misiones). A la izquierda esta la Sensibilidad: giro, angulo, cabeceo, velocidad minima y maxima (en km/h), camara, giro en el suelo, etc.");
                s.AppendLine("- [b]Sonidos[/b]: volumen del motor y mezcla general, motor del avion y del auto, radio, musica y azafata.");
                s.AppendLine("- [b]Modelo[/b]: orientacion, posicion y tamano del avion (ver CONSTRUCTOR: modelos).");
                s.AppendLine("- [b]Controles[/b]: asignar teclas y botones del joystick.");
                s.AppendLine("- [b]Destinos[/b]: marcar un lugar con nombre (ver CONSTRUCTOR: lugares).");
                s.AppendLine("- [b]Aviones[/b]: elegir el avion o helicoptero. Toca uno de la lista y ves una foto de costado (la primera vez se saca sola y queda guardada en la carpeta previews), con su velocidad maxima y minima. [b]Rotar en 3D[/b] lo carga y lo das vuelta arrastrando con el mouse (o [b]Giro automatico[/b]). [b]ELEGIR ESTE AVION[/b] lo pone a volar. Se abre sola al empezar y con el boton [b]Cambiar avion[/b].");
                s.AppendLine("- [b]Carrera[/b]: tu perfil de piloto, el dinero (tambien arriba a la izquierda, cajita DINERO), el viaje actual y perfiles nuevos. Para ganar plata: en Pagina 2 elegi un aeropuerto, toca [b]Ir[/b] y aterriza a menos de 600 m. El pago se multiplica segun el avion (Boeing x3, jets x1,5, Cessna x1, el resto x1,2). Los precios que cambies con - y + en la Torre quedan guardados.");
                break;
            case 3:
                s.AppendLine("[b]MAPA Y CAMARA[/b]\n");
                s.AppendLine("- Mapa chico (izquierda): muestra el avion como un avioncito rojo con la punta hacia donde vas. Boton [b]Centrar[/b] lo vuelve a centrar en tu posicion.");
                s.AppendLine("- Zoom de la camara: [b]Z[/b] acerca y [b]X[/b] aleja.");
                s.AppendLine("- Girar la vista alrededor del avion: " + Asig("girar_vista_izquierda") + " (izquierda) y " + Asig("girar_vista_derecha") + " (derecha). Tambien con el stick derecho del joystick.");
                s.AppendLine("- Centrar la camara: " + Asig("centrar_camara_orbital"));
                s.AppendLine("- Hora del dia: barra de arriba (- / + y Auto).");
                break;
            case 4:
                s.AppendLine("[b]AUTITO Y HELICOPTERO[/b]\n");
                s.AppendLine("- [b]Autito[/b]: con el avion en tierra, el boton [b]Bajar del avion[/b] (arriba a la izquierda) te pasa al autito; [b]Subir al avion[/b] vuelve. Acelerar/frenar/girar usan los mismos controles; reversa: " + Asig("reversa_pushback") + ".");
                s.AppendLine("- [b]Helicoptero[/b] (Mi-35M y Ka-27 en la lista de aviones): subir " + Asig("vertical_arriba") + ", bajar " + Asig("vertical_abajo") + ". Las helices giran solas. La velocidad de subida se ajusta en Sensibilidad (Subida m/s).");
                break;
            case 5:
                s.AppendLine("[b]RADIO Y SONIDO[/b]\n");
                s.AppendLine("- Barra de arriba: [b]Radio: ON/OFF[/b], [b]Emisoras[/b] (elegis cual), [b]Musica[/b] (lista de temas).");
                s.AppendLine("- El boton [b]Azafata[/b] de arriba a la derecha reproduce el audio de seguridad de la azafata.");
                s.AppendLine("- Volumenes separados en la pestana [b]Sonidos[/b] del panel de abajo.");
                break;
            case 6:
                s.AppendLine("[b]CREDITOS[/b]\n");
                s.AppendLine("Imagenes de los blancos del modo guerra (Magnific, recursos gratis):");
                s.AppendLine("- Objetivo circular colorido: imagen de [b]tohamina[/b] en Magnific.");
                s.AppendLine("- Boton rojo con flecha en el blanco: imagen de [b]muhammad.abdullah[/b] en Magnific.");
                s.AppendLine("- Objetivo de tiro deportivo: imagen de [b]negoo-s[/b] en Magnific.");
                break;
            case 7:
                s.AppendLine("[b]MARCAR UN LUGAR CON NOMBRE[/b]  (ej.: Estadio Ferrocarril Oeste)\n");
                s.AppendLine("1. Pone el avion (o el autito) justo sobre el lugar.");
                s.AppendLine("2. Apreta " + Asig("marcar_lugar") + ". El panel de abajo salta solo a la pestana [b]Destinos[/b] (tambien podes tocar esa pestana y el boton [b]Marcar lugar ahora[/b]).");
                s.AppendLine("3. Escribi el nombre con el teclado. Mientras escribis, el avion no se mueve.");
                s.AppendLine("4. [b]Enter[/b] guarda (aparece un cartel \"Lugar guardado\"). [b]Esc[/b] o [b]Cancelar[/b] no guarda nada.");
                s.AppendLine("\nSe guarda: nombre, latitud, longitud, altura y rumbo. Si el nombre lleva la palabra \"aeropuerto\" queda anotado como aeropuerto (todavia no se crea solo en el juego: se le pasa el archivo a Claude).");
                s.AppendLine("Archivo: [b]lugares_marcados.json[/b] en la carpeta \"000 simulador Unity\". Los 63 lugares que venian de Godot ya estan ahi; los nuevos se agregan al final sin borrar nada.");
                break;
            case 8:
                s.AppendLine("[b]MARCAR LAS PUNTAS DE UNA PISTA[/b]  (para corregir luces u orientacion)\n");
                s.AppendLine("No hay una funcion aparte: se usa la de marcar lugar, dos veces.");
                s.AppendLine("1. Anda a una punta de la pista, alineado con ella, y apreta " + Asig("marcar_lugar") + ".");
                s.AppendLine("2. Escribi un nombre claro, por ejemplo: [b]Pista Aeroparque 13 - cabecera NORTE[/b]. Enter.");
                s.AppendLine("3. Anda a la otra punta, marca de nuevo: [b]Pista Aeroparque 13 - cabecera SUR[/b].");
                s.AppendLine("4. Pasale a Claude esos dos nombres (o el archivo lugares_marcados.json): con las dos coordenadas calcula la orientacion de la pista.");
                s.AppendLine("\nSi el nombre incluye \"aeropuerto\", queda marcado como tal.");
                break;
            case 9:
                s.AppendLine("[b]TRACKEAR UNA RUTA CON EL AUTITO[/b]  (para pintar calles/rutas con luces de noche)\n");
                s.AppendLine("1. Aterriza, toca [b]Bajar del avion[/b] y maneja el autito por la ruta.");
                s.AppendLine("2. Cada tanto (cada ~10 segundos, como en Godot) apreta " + Asig("marcar_punto_ruta") + ": guarda un punto (cartel \"Punto de ruta #N guardado\").");
                s.AppendLine("3. Si te equivocas: " + Asig("deshacer_punto_ruta") + " borra el ultimo punto.");
                s.AppendLine("4. Los puntos quedan en [b]ruta_trackeada.json[/b] (carpeta \"000 simulador Unity\"). En la pestana [b]Destinos[/b] ves cuantos puntos van.");
                s.AppendLine("\nCuidado: el mismo boton del joystick puede estar compartido con la reversa. Si te molesta, reasignalo en la pestana [b]Controles[/b].");
                break;
            case 10:
                s.AppendLine("[b]AJUSTAR UN MODELO DE AVION[/b]  (orientacion, posicion, tamano)\n");
                s.AppendLine("1. Elegi el avion y abri la pestana [b]Modelo[/b] del panel de abajo.");
                s.AppendLine("2. [b]GIRAR[/b]: Cabeceo X / Rumbo Y / Alabeo Z con - y +. [b]MOVER[/b]: izquierda-derecha, arriba-abajo, adelante-atras.");
                s.AppendLine("3. [b]TOQUE[/b] define cuantos grados mueve cada click (1, 5, 15, 90). [b]Chico/Grande[/b] cambia el tamano; [b]Pausar[/b] congela el avion para mirarlo.");
                s.AppendLine("4. Se guarda solo 1 segundo despues de tocar algo. [b]CLAVAR RESULTADO[/b] lo guarda en el momento y muestra el mensaje.");
                s.AppendLine("\nSe guarda por modelo en [b]ajustes_aviones.json[/b] (carpeta Simulador). Tus ajustes pisan los valores por defecto.");
                break;
            default:
                s.AppendLine("[b]DONDE SE GUARDA CADA COSA[/b]  (carpeta \"000 simulador Unity\")\n");
                s.AppendLine("- [b]lugares_marcados.json[/b]: lugares con nombre (nombre, latitud, longitud, altura, rumbo).");
                s.AppendLine("- [b]ruta_trackeada.json[/b]: puntos de la ruta del autito (lat/lon).");
                s.AppendLine("- [b]precios_torre.json[/b]: los precios de aeropuertos que corregiste a mano (los demas se calculan por distancia).");
                s.AppendLine("- [b]perfiles\\[/b] (una carpeta): un archivo por piloto con su dinero; [b]ultimo_perfil.txt[/b] recuerda con cual arrancar.");
                s.AppendLine("- [b]Simulador\\ajustes_aviones.json[/b]: orientacion, posicion, tamano y sensibilidad de cada modelo.");
                s.AppendLine("- Teclas y botones: archivo controles.json en la carpeta de datos de Unity; se cambian desde la pestana Controles.");
                s.AppendLine("\nPara pasarle algo a Claude: decile el nombre del archivo y que lo lea; no hace falta copiar nada.");
                break;
        }
        return s.ToString().Replace("[b]", "<b>").Replace("[/b]", "</b>");
    }

    // acciones que son para quien arma el juego (no para el jugador comun)
    static bool EsConstructor(string id) { return id == "marcar_lugar" || id == "marcar_punto_ruta" || id == "deshacer_punto_ruta"; }
}
