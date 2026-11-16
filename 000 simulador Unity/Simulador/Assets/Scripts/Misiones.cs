// Catalogo de misiones: normales (un destino) y recorridos (varias escalas, se cobra un poco por escala y un bono grande al final).
// Los destinos se buscan por pedazo de nombre (sin acentos) entre los aeropuertos que YA estan trackeados. "@Nombre|lat|lon|texto" = punto de paseo (se sobrevuela, no se aterriza).
using System.Collections.Generic;

public static class Misiones
{
    public class Def
    {
        public string id, titulo, texto, req, dest;
        public string[] paradas;      // recorridos: "pedazo|texto de llegada" o "@Nombre|lat|lon|texto"
        public int bas, bono, tramo;  // normales: bas + km*tarifa (se calcula con la distancia real); recorridos: bono final y pago por escala
        public double tarifa;
        public bool vip, recorrido, tour;
    }

    static double Tarifa(string req) { switch (req) { case "liviano": return 2.5; case "jet": return 6.0; case "pesado": return 5.0; default: return 3.0; } }

    static Def N(string id, string tit, string txt, string dest, string req, int bas, bool vip = false)
    {
        return new Def { id = id, titulo = tit, texto = txt, dest = dest, req = req, bas = bas, tarifa = Tarifa(req), vip = vip };
    }
    // bono y pago por escala: se fijan a mano (bono) y por escala ~ 12% del bono
    static Def R(string id, string tit, string txt, string req, int bono, bool vip, bool tour, params string[] paradas)
    {
        return new Def { id = id, titulo = tit, texto = txt, req = req, bono = bono, tramo = (int)(System.Math.Round(bono * 0.12 / 10.0) * 10), vip = vip, recorrido = true, tour = tour, paradas = paradas };
    }

    public static List<Def> Todas()
    {
        var l = new List<Def>();
        // ===================== NORMALES (con su historia) =====================
        // --- Capital y alrededores (Cessna) ---
        l.Add(N("n01", "Repuesto urgente", "Un mecanico de Campo de Mayo espera una bomba hidraulica para dejar listo un avion antes de la noche.", "Campo de Mayo", "liviano", 60));
        l.Add(N("n02", "Papeles de escribania", "Hay una compraventa que se firma hoy a las 17. Los originales con sello tienen que llegar a Moron antes de que cierre el registro.", "Moron", "liviano", 70));
        l.Add(N("n03", "Muestras de laboratorio", "Un laboratorio necesita llevar muestras a El Palomar para analizarlas antes de que pierdan validez.", "El Palomar", "liviano", 70));
        l.Add(N("n04", "Valija olvidada", "Una pasajera dejo su valija con los remedios de su madre. Hay que llevarsela a Aeroparque antes del vuelo de las 20.", "Aeroparque", "liviano", 60));
        l.Add(N("n05", "Pasajero con apuro", "Un hombre de negocios perdio el auto y llega tarde a una reunion en San Fernando. Paga bien por ahorrarse el trafico.", "San Fernando", "liviano", 80));
        l.Add(N("n06", "Entrega de planos", "Un estudio de arquitectura necesita que los planos firmados lleguen a Rosario antes de licitar una obra.", "Rosario", "cualquiera", 110));
        l.Add(N("n07", "Pieza de motor", "Un taller de Quilmes tiene un cliente esperando un inyector que solo se consigue en otro hangar.", "Quilmes", "liviano", 80));
        l.Add(N("n08", "Casting de television", "Una productora necesita llevar a tres actores al estudio de Santa Rosa. Tienen que llegar peinados, asi que vuela suave.", "Santa Rosa", "cualquiera", 150));
        l.Add(N("n09", "Medicamentos de alto costo", "Una farmacia de La Plata se quedo sin una medicacion especial. Hay una paciente esperando la dosis de hoy.", "La Plata", "liviano", 90));
        l.Add(N("n10", "Sobre del banco", "Un banco manda un sobre con firmas originales a Ezeiza. No puede viajar en moto ni en auto: tiene horario.", "Ezeiza", "liviano", 70));
        l.Add(N("n11", "Torta de casamiento", "La torta de tres pisos de un casamiento en La Plata quedo lista en Capital. El salon espera. No la sacudas.", "La Plata", "liviano", 90));
        l.Add(N("n12", "Aerofotografia de un campo", "Un productor quiere fotos aereas de su campo cerca de Aero Club Lujan para vender una parte. Vuelo tranquilo, a baja velocidad.", "Aero Club Lujan", "liviano", 90));
        l.Add(N("n13", "Encargo para Chivilcoy", "Hay que llevar un paquete de repuestos al aerodromo de Chivilcoy. El dueno de la chacra los necesita para la cosecha.", "Chivilcoy", "liviano", 100));
        l.Add(N("n14", "Escapada de fin de semana", "Una pareja quiere pasar el sabado en Paysandu, del otro lado del rio. Equipaje liviano.", "Paysandu", "cualquiera", 130));
        l.Add(N("n15", "Visita a la estancia", "Una familia tiene un asado en una estancia en las sierras de Tandil. Los abuelos no quieren manejar mas de dos horas.", "Tandil", "cualquiera", 120));
        // --- Costa y provincia (cualquiera) ---
        l.Add(N("n16", "Congreso en Mar del Plata", "Tres profesionales llegan tarde a un congreso medico en Mar del Plata. Pagan el vuelo y cargan poco equipaje.", "Mar del Plata", "cualquiera", 120));
        l.Add(N("n17", "Bodas de oro en Villa Gesell", "Una pareja celebra 50 anos de casados y la familia les regalo el viaje a Villa Gesell. Quieren aterrizar con vista al mar.", "Villa Gesell", "cualquiera", 120));
        l.Add(N("n18", "Pesca de trucha", "Un grupo de pescadores amigos va a San Martin de los Andes por la trucha. Llevan canas y cajas de hielo.", "San Martin de los Andes", "cualquiera", 200));
        l.Add(N("n19", "Casamiento en Tandil", "Los novios se casan en Tandil y el padrino llega en avion. Tiene que aterrizar antes de la ceremonia.", "Tandil", "cualquiera", 110));
        l.Add(N("n20", "Veterinaria de urgencia", "Un caballo de polo necesita un medicamento que solo hay en Capital. Lo espera el veterinario en Villa Gesell.", "Villa Gesell", "cualquiera", 120));
        l.Add(N("n21", "Fin de semana en Viedma", "Un empresario quiere desconectarse a orillas del rio Negro. Va con su perro y un ukelele.", "Viedma", "cualquiera", 170));
        l.Add(N("n22", "Documentos del juzgado", "Un expediente judicial tiene que estar en Gualeguaychu antes de que venza el plazo. No hay otro modo de llegar a tiempo.", "Gualeguaychu", "cualquiera", 110));
        l.Add(N("n23", "Excursion a Uruguay", "Dos amigos quieren almorzar en Carmelo, del otro lado del rio. Cruzan, comen, y despues veremos si vuelven.", "Carmelo", "cualquiera", 130));
        l.Add(N("n24", "Casa en Salto", "Una familia uruguaya vuelve a Salto despues de un tratamiento medico en Buenos Aires. Quieren llegar antes del anochecer.", "Salto", "cualquiera", 140));
        l.Add(N("n25", "Partido en Rosario", "Un hincha loco quiere ver el clasico en Rosario. Le compraron la entrada y le falta el vuelo.", "Rosario", "cualquiera", 140));
        l.Add(N("n26", "Termas en Concordia", "Un matrimonio mayor se escapa a las termas de Concordia. Pidieron viaje suave y sin turbulencia.", "Concordia", "cualquiera", 150));
        l.Add(N("n27", "Festival en las sierras", "Una banda de rock llega en avion a Merlo para tocar en un festival en las sierras puntanas. Llevan guitarras y amplificadores.", "Villa de Merlo", "cualquiera", 160));
        l.Add(N("n28", "Dia de campo en Rio Cuarto", "Un ingeniero agronomo tiene que visitar un lote en Rio Cuarto antes de que llueva. Paga por ahorrarse la ruta.", "Rio Cuarto", "cualquiera", 160));
        l.Add(N("n29", "Entrega en Bahia Blanca", "Una carga de repuestos navales sale de Capital hacia Bahia Blanca. El barco no espera.", "Bahia Blanca", "cualquiera", 180));
        l.Add(N("n30", "Termas de Rio Hondo", "Un piloto retirado quiere conocer las termas. Dice que lo unico que extrana es sentir el viento en la cara.", "Termas de Rio Hondo", "cualquiera", 200));
        l.Add(N("n43", "Artesanias de Catamarca", "Un feriante necesita traer a Capital una partida de ponchos tejidos a mano desde Catamarca antes de la feria del fin de semana.", "Catamarca", "pesado", 800));
        l.Add(N("n44", "Festival folklorico", "Un grupo de folkloristas debe llegar a Santiago del Estero para abrir la noche del festival. Llevan bombos y guitarras.", "Santiago del Estero", "cualquiera", 180));
        l.Add(N("n45", "Vendimia en San Juan", "Una bodega sanjuanina invita a periodistas a la cosecha. Hay que llevarlos sanos y a horario.", "San Juan", "jet", 650, true));
        l.Add(N("n46", "El viejo tren de la Patagonia", "Una familia quiere subirse a La Trochita en Esquel y paga un vuelo directo para no perder el tren.", "Esquel", "jet", 900, true));
        l.Add(N("n47", "Lana del sur", "Un cargamento de fardos de lana esperan en Rio Gallegos para exportar. Hace falta avion grande.", "Rio Gallegos", "pesado", 1100));
        l.Add(N("n48", "Cumpleanos en Formosa", "Una abuela cumple 90 anos en Formosa y toda la familia viaja a sorprenderla. Llevan torta, globos y un mariachi.", "Formosa", "cualquiera", 190));
        l.Add(N("n49", "Fotos en Villa Gesell", "Un fotografo de bodas quiere hacer la sesion de una pareja frente al mar de Villa Gesell, al atardecer.", "Villa Gesell", "cualquiera", 120));
        l.Add(N("n50", "Mensaje a Mercedes", "Una carta certificada tiene que llegar en mano a Mercedes, Uruguay. Del otro lado del rio esperan la firma.", "Mercedes (Uru", "cualquiera", 140));
        // --- VIP y jets privados ---
        l.Add(N("n31", "Bodeguero en Mendoza", "Un bodeguero necesita llegar a Mendoza antes de que se cierre una cosecha especial. Paga bien por cabina cuidada.", "Plumerillo", "jet", 600, true));
        l.Add(N("n32", "Retiro de directorio", "Un directorio completo se reune dos dias en Bariloche. Pide jet privado y nada de ruido en cabina.", "Bariloche", "jet", 700, true));
        l.Add(N("n33", "Luna de miel en El Calafate", "Una pareja de recien casados viaja a ver el glaciar. Quieren llegar de noche para despertar frente al hielo.", "El Calafate", "jet", 800, true));
        l.Add(N("n34", "Reunion en Cordoba", "Un empresario tiene una reunion decisiva en Cordoba y despues vuelve a Capital. Solo quiere que llegue a horario.", "Taravella", "jet", 600, true));
        l.Add(N("n35", "Concierto en Salta", "Una cantante famosa debe llegar a Salta para el show de esta noche. Su manager aprieta los dientes.", "Salta", "jet", 800, true));
        l.Add(N("n36", "Cataratas privadas", "Una pareja de artistas quiere ver las Cataratas desde un jet privado. Piden ventanas limpias.", "Puerto Iguazu", "jet", 900, true));
        l.Add(N("n37", "Fin del mundo", "Un coleccionista viaja a Ushuaia para ver las ultimas luces del dia. No acepta escalas.", "Ushuaia", "jet", 1100, true));
        // --- Carga pesada ---
        l.Add(N("n38", "Carga de petroleo", "Un equipo de repuestos para pozos petroleros sale hacia Neuquen. Hay una cuadrilla parada esperandolo.", "Neuquen", "pesado", 800, false));
        l.Add(N("n39", "Pesca de Trelew", "Un cargamento de pescado congelado esta listo en Trelew para exportar. Tiene que salir hoy para cumplir la cadena de frio.", "Trelew", "pesado", 900, false));
        l.Add(N("n40", "Yerba mate a Posadas", "Pallets de yerba mate de un molino esperan en Posadas. Hay que traerlos o salen a otro destino.", "Posadas", "pesado", 800, false));
        l.Add(N("n41", "Maquinaria a Comodoro", "Una perforadora desarmada tiene que llegar a Comodoro Rivadavia. Es mucha carga y poca tolerancia a la demora.", "Comodoro", "pesado", 1100, false));
        l.Add(N("n42", "Limones de Tucuman", "Una exportadora necesita enviar cajones de limon a Tucuman para el empaque. El calor corre en contra.", "Tucuman", "pesado", 800, false));

        // ===================== RECORRIDOS (con escalas) =====================
        // --- Capital: Cessna ---
        l.Add(R("r01", "Mensajeria de Mercado Pago", "Tres paquetes con tarjetas y documentos para entregar en mano en tres aerodromos del conurbano. Cada entrega se confirma al aterrizar.", "liviano", 700, false, false,
            "El Palomar|Entregaste el primer paquete en El Palomar. Falta Moron.", "Moron|Segundo paquete firmado. Ultima parada: Campo de Mayo.", "Campo de Mayo|Ultimo paquete entregado. Recorrido completo!"));
        l.Add(R("r02", "Reparto de farmacias", "Una cadena de farmacias reparte insumos por aire porque en auto no llega: tres paradas hacia el oeste.", "liviano", 700, false, false,
            "San Fernando|Primera caja recibida en San Fernando.", "Moron|Segunda caja entregada en Moron.", "Aero Club Lujan|Ultimas cajas entregadas en Lujan."));
        l.Add(R("r03", "Paquetes de Mercado Libre", "Una cuenta grande de ventas online manda paquetes a tres aerodromos. Hay que respetar el orden que dice el despachante.", "liviano", 750, false, false,
            "Moron|Primer envio entregado en Moron.", "Ezeiza|Segundo envio en Ezeiza.", "Quilmes|Ultimo envio entregado en Quilmes."));
        l.Add(R("r04", "Diario de la manana", "Los paquetes del diario tienen que llegar a cuatro aerodromos antes del amanecer. Si te demoras, el kiosquero se enoja.", "liviano", 900, false, false,
            "Aeroparque|Primera tanda entregada en Aeroparque.", "San Fernando|Segunda tanda en San Fernando.", "Campo de Mayo|Tercera tanda entregada.", "El Palomar|Ultima tanda entregada. Buen laburo!"));
        l.Add(R("r05", "Muestras clinicas", "Una red de laboratorios lleva tubos de analisis a tres puntos. Cada espera de mas arruina la muestra.", "liviano", 800, false, false,
            "Quilmes|Primera muestra entregada.", "Ezeiza|Segunda muestra entregada.", "Aeroparque|Las muestras llegaron enteras. Recorrido completo!"));
        l.Add(R("r06", "Tour fotografico: Buenos Aires desde el aire", "Una agencia de turismo quiere fotos de la ciudad con luz de la tarde. Pasa por encima de cada punto, sin aterrizar, volando despacio.", "liviano", 1200, false, true,
            "@Obelisco|-34.6037|-58.3816|Primera foto: el Obelisco.", "@Puerto Madero|-34.6118|-58.3636|Segunda foto: Puerto Madero.", "@Estadio Monumental|-34.5453|-58.4498|Tercera foto: el Monumental.", "@La Bombonera|-34.6356|-58.3647|Ultima foto: La Bombonera. Tour completo!"));
        l.Add(R("r07", "Tour de los estadios", "Un canal deportivo pide un recorrido aereo por cuatro estadios para una nota especial. Sobrevolalos sin aterrizar.", "liviano", 1400, false, true,
            "@Estadio Monumental|-34.5453|-58.4498|El Monumental, listo.", "@La Bombonera|-34.6356|-58.3647|La Bombonera, listo.", "@Estadio Velez|-34.6353|-58.5207|El Amalfitani, listo.", "@Cilindro de Avellaneda|-34.6676|-58.3672|Cilindro de Avellaneda. Tour completo!"));
        l.Add(R("r08", "Recorrido de ejecutivos", "Un jet privado levanta a tres ejecutivos distintos por Capital. Cada uno sube en un aeropuerto distinto.", "jet", 2600, true, false,
            "Aeroparque|Subio el primer ejecutivo.", "San Fernando|Subio la segunda ejecutiva.", "Ezeiza|Los tres ya estan a bordo y el tercero va para el exterior. Recorrido completo!"));
        // --- Costa y provincia ---
        l.Add(R("r09", "Gira por la costa", "Un grupo de turistas recorre la costa atlantica en jet. La Plata, Villa Gesell y Mar del Plata.", "jet", 3000, true, false,
            "La Plata|Primera parada en La Plata.", "Villa Gesell|Segunda parada: Villa Gesell.", "Mar del Plata|Llegaron a Mar del Plata. Recorrido completo!"));
        l.Add(R("r10", "Verano en la costa", "Un hotel manda a sus huespedes a tres localidades de la costa antes del cierre de temporada.", "cualquiera", 1500, false, false,
            "La Plata|Primera parada en La Plata.", "Villa Gesell|Segunda parada: Villa Gesell.", "Mar del Plata|Tercera parada en Mar del Plata.", "Tandil|Ultimo huesped en Tandil. Recorrido completo!"));
        l.Add(R("r11", "Charter de Semana Santa", "Una agencia vende un paquete completo: Mar del Plata, Tandil y Bahia Blanca. Hace falta avion grande.", "pesado", 4200, true, false,
            "Mar del Plata|Bajaron los primeros pasajeros en Mar del Plata.", "Tandil|Segunda parada en Tandil.", "Bahia Blanca|Ultimos pasajeros en Bahia Blanca. Recorrido completo!"));
        l.Add(R("r12", "Cabotaje por la Pampa", "Un transportista lleva encargos entre pueblos pampeanos. No hay rutas buenas: por aire se llega antes.", "cualquiera", 1400, false, false,
            "Chivilcoy|Primer encargo en Chivilcoy.", "Santa Rosa|Segundo encargo en Santa Rosa.", "Bahia Blanca|Tercer encargo entregado.", "Tandil|Ultimo encargo entregado. Recorrido completo!"));
        // --- Largos ---
        l.Add(R("r13", "Hacia Cordoba con escalas", "Una empresa manda a su equipo a Cordoba pasando por Rosario y Rio Cuarto para cargar y descargar gente.", "cualquiera", 2200, false, false,
            "Rosario|Primera escala en Rosario.", "Rio Cuarto|Segunda escala en Rio Cuarto.", "Taravella|Llegaron a Cordoba. Recorrido completo!"));
        l.Add(R("r14", "Cruce de la pampa", "Un productor viaja a Mendoza haciendo escalas en La Pampa y San Luis para cerrar negocios en el camino.", "jet", 3800, true, false,
            "Santa Rosa|Primera reunion cerrada en Santa Rosa.", "San Luis|Segunda reunion cerrada en San Luis.", "Plumerillo|Llegaron a Mendoza. Recorrido completo!"));
        l.Add(R("r15", "Ruta del vino", "Un sommelier recorre bodegas de cuatro provincias para elegir una cosecha. Quiere viajar rapido y probar mas.", "jet", 4200, true, false,
            "Plumerillo|Primera bodega visitada en Mendoza.", "San Juan|Segunda bodega en San Juan.", "La Rioja|Tercera bodega en La Rioja.", "San Rafael|Ultima bodega: San Rafael. Recorrido completo!"));
        l.Add(R("r16", "Norte argentino", "Un grupo de turistas jubilados recorre el norte del pais con una agenda armada al detalle.", "cualquiera", 3000, false, false,
            "Taravella|Primer dia en Cordoba.", "Tucuman|Segundo dia en Tucuman.", "Salta|Tercer dia en Salta.", "Jujuy|Ultimo dia en Jujuy. Recorrido completo!"));
        l.Add(R("r17", "Litoral y cataratas", "Una agencia de ecoturismo lleva a un grupo de fotografos por el rio Uruguay hasta Iguazu.", "cualquiera", 3200, false, false,
            "Gualeguaychu|Primera parada en Gualeguaychu.", "Concordia|Segunda parada en Concordia.", "Posadas|Tercera parada en Posadas.", "Puerto Iguazu|Llegaron a las Cataratas. Recorrido completo!"));
        l.Add(R("r18", "Cataratas VIP", "Un empresario brasilero quiere ver las Cataratas con su familia y dos socios, parando en Rosario, Resistencia y Posadas.", "jet", 5200, true, false,
            "Rosario|Primera escala: recoge a los socios.", "Resistencia|Segunda escala en Resistencia.", "Posadas|Tercera escala en Posadas.", "Puerto Iguazu|Llegaron a Iguazu. Recorrido completo!"));
        l.Add(R("r19", "Patagonia al sur", "Una empresa petrolera hace su ronda de visitas por la costa patagonica: Bahia Blanca, Viedma, Trelew, Comodoro y Rio Gallegos.", "cualquiera", 4500, false, false,
            "Bahia Blanca|Primera visita en Bahia Blanca.", "Viedma|Segunda visita en Viedma.", "Trelew|Tercera visita en Trelew.", "Comodoro|Cuarta visita en Comodoro Rivadavia.", "Rio Gallegos|Ultima visita en Rio Gallegos. Recorrido completo!"));
        l.Add(R("r20", "Hacia el fin del mundo", "Un viaje de lujo por la Patagonia: Neuquen, Bariloche, El Calafate y Ushuaia. Cada noche, un hotel distinto.", "jet", 7500, true, false,
            "Neuquen|Primera noche en Neuquen.", "Bariloche|Segunda noche en Bariloche.", "El Calafate|Tercera noche en El Calafate.", "Ushuaia|Llegaron al fin del mundo. Recorrido completo!"));
        l.Add(R("r21", "Cordillera de los lagos", "Un grupo de esquiadores recorre cinco aeropuertos cordilleranos buscando la mejor nieve.", "jet", 6500, true, false,
            "Plumerillo|Salieron de Mendoza.", "Neuquen|Escala en Neuquen.", "San Martin de los Andes|Escala en San Martin de los Andes.", "Bariloche|Escala en Bariloche.", "Esquel|Llegaron a Esquel. Recorrido completo!"));
        l.Add(R("r22", "Rio de la Plata y Uruguay", "Un escribano va a tres ciudades uruguayas a legalizar documentos. Paga todo, hasta el almuerzo.", "cualquiera", 2600, false, false,
            "Carmelo|Primer tramite en Carmelo.", "Mercedes (Uruguay|Segundo tramite en Mercedes.", "Paysandu|Tercer tramite en Paysandu.", "Salto|Ultimo tramite en Salto. Recorrido completo!"));
        l.Add(R("r23", "Gran carga federal", "Una exportadora arma una cadena con cuatro puntos: Ezeiza, Cordoba, Mendoza y Neuquen. Necesita un avion grande.", "pesado", 8500, true, false,
            "Ezeiza|Primera carga cargada en Ezeiza.", "Taravella|Segunda carga en Cordoba.", "Plumerillo|Tercera carga en Mendoza.", "Neuquen|Ultima carga entregada en Neuquen. Recorrido completo!"));
        l.Add(R("r24", "Charter a Asuncion", "Un grupo de ejecutivos viaja a Paraguay con paradas en Resistencia y Formosa.", "cualquiera", 3000, false, false,
            "Resistencia|Primera escala en Resistencia.", "Formosa|Segunda escala en Formosa.", "Asuncion|Llegaron a Asuncion. Recorrido completo!"));
        l.Add(R("r25", "Termas y sierras", "Un hotel de lujo lleva a sus huespedes por sierras y termas: Rio Cuarto, Merlo, Rio Hondo y Santiago del Estero.", "cualquiera", 3500, false, false,
            "Rio Cuarto|Primer dia en Rio Cuarto.", "Merlo|Segundo dia en Merlo.", "Termas de Rio Hondo|Tercer dia en las termas.", "Santiago del Estero|Ultimo dia en Santiago. Recorrido completo!"));
        l.Add(R("r26", "Pueblos de la llanura", "Un viajante de comercio visita cuatro pueblos para cobrar cuentas. Paga a tiempo si llegas a horario.", "cualquiera", 2400, false, false,
            "Aero Club Lujan|Primera cuenta cobrada en Lujan.", "Chivilcoy|Segunda cuenta en Chivilcoy.", "Rosario|Tercera cuenta en Rosario.", "Gualeguaychu|Ultima cuenta cobrada. Recorrido completo!"));
        return l;
    }
}
