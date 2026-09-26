extends Node3D

# A partir de ahora el "piso" de todo el mapa es terreno REAL de verdad
# (Google Photorealistic 3D Tiles, vía el plugin de Cesium) en vez de fotos
# horneadas a mano -- por eso ya no hace falta generar campos, árboles ni
# casitas de juguete acá. Lo único que este script arma por código es la
# pista/cartel de cada aeropuerto, ubicado en su posición REAL (lat/long)
# convertida a coordenadas locales por el propio CesiumGeoreference.
@onready var georeferencia: CesiumGeoreference = get_node("CesiumGeoreference")
@onready var tileset: Cesium3DTileset = get_node("CesiumGeoreference/Cesium3DTileset")
# Segundo terreno, con la capa "Google Maps 2D Roadmap" (calles, nombres de
# barrio) encima de un terreno base gratis (Cesium World Terrain) -- pedido
# 2026-09-20, inspirado en el mapa de GeoFS. IMPORTANTE: las baldosas de
# "Google Photorealistic 3D Tiles" son mallas ya texturizadas (no tienen las
# coordenadas extra que usa Cesium para "pintar" una capa encima) -- por eso
# el mapa de calles NECESITA su propio terreno aparte, no se puede pintar
# sobre los edificios reales.
#
# INTENTO 1 (revertido): mover las baldosas del segundo terreno a otra capa
# de render de Godot (misma CesiumGeoreference para los dos tilesets). Rompió
# la vista principal (terreno fragmentado) -- dos Cesium3DTileset
# georreferenciados activos a la vez, aunque en capas distintas, comparten
# demasiado estado interno en Cesium Native (frustums/LOD/caché de baldosas)
# y se pisan entre sí.
#
# INTENTO 2 (actual): el segundo terreno vive en su PROPIA CesiumGeoreference,
# adentro de un SubViewport con own_world_3d=true (ver HUD/MapaViewport en la
# escena) -- un "universo" de Godot totalmente aislado del principal, con su
# propio World3D. Cada cuadro le copiamos a esa georreferencia el mismo
# ecefX/Y/Z que tiene la principal (ver _process), para que ambas sigan al
# avión sincronizadas sin compartir ningún estado interno de Cesium.
@onready var georeferencia_mapa: CesiumGeoreference = get_node("HUD/MapaViewport/CesiumGeoreferenceMapa")
@onready var tileset_mapa: Cesium3DTileset = get_node("HUD/MapaViewport/CesiumGeoreferenceMapa/Cesium3DTilesetMapa")
@onready var camara_juego: Camera3D = get_node("CamaraJuego")
@onready var avion: Node3D = get_node("Avion")
# BUG REAL encontrado 2026-09-20: el minimapa (la cámara chica que mira hacia
# abajo) se veía como una mancha borrosa en vez de foto nítida -- porque
# nunca le avisábamos al sistema de baldosas dónde está ESA cámara, solo
# dónde está la principal. Cesium elige qué tanto detalle cargar en cada
# zona según la cámara que le avisás -- si nunca le avisamos de esta, la
# zona que mira (justo debajo del avión) quedaba con el detalle que le
# tocara "de rebote", no el que necesita.
@onready var camara_minimapa: Camera3D = get_node("HUD/MinimapaViewport/CamaraMinimapa")
@onready var camara_mapa: Camera3D = get_node("HUD/MapaViewport/CamaraMapa")

# Ciclo día/noche (pedido 2026-09-25) -- "hora_del_dia" (0.0 a 24.0) maneja
# TODO: la posición del sol en el cielo, su color/intensidad, y los colores
# del cielo procedural. Por defecto avanza sola durante el vuelo (un día
# completo cada SEGUNDOS_POR_DIA_COMPLETO segundos reales), pero también se
# puede mover a mano desde el panel de Configuración (ver ajustar_hora_del_dia
# en principal.gd) para probar/demostrar el efecto al instante sin esperar.
@onready var luz_sol: DirectionalLight3D = get_node("DirectionalLight3D")
@onready var entorno_mundo: WorldEnvironment = get_node("WorldEnvironment")
var hora_del_dia: float = 12.0
var avance_automatico_hora: bool = true
# 0.0 = pleno día, 1.0 = noche cerrada -- publicado para que las luces de
# pista (más abajo) sepan cuánto brillar sin recalcular la hora ellas mismas.
var factor_noche_actual: float = 0.0
const SEGUNDOS_POR_DIA_COMPLETO = 9000.0  # 2.5 horas reales = 1 día de juego (x5, pedido 2026-09-26)
const COLOR_CIELO_DIA_ARRIBA = Color(0.385, 0.454, 0.55)
const COLOR_CIELO_DIA_HORIZONTE = Color(0.646, 0.656, 0.671)
const COLOR_SUELO_DIA = Color(0.2, 0.169, 0.133)
const COLOR_CIELO_NOCHE_ARRIBA = Color(0.02, 0.03, 0.08)
const COLOR_CIELO_NOCHE_HORIZONTE = Color(0.05, 0.06, 0.12)
const COLOR_SUELO_NOCHE = Color(0.01, 0.01, 0.02)
const COLOR_LUZ_AMANECER = Color(1.0, 0.55, 0.3)
const COLOR_LUZ_DIA = Color(1.0, 0.98, 0.92)

# Coordenadas reales (lat/long en grados, investigadas antes con OurAirports).
# SEGUNDO INTENTO DE ILS VISUAL (2026-09-21): el primer intento (mismo día,
# revertido) usaba un rumbo investigado a mano MÁS la coordenada aproximada
# del aeropuerto -- eso alcanza el rumbo bien pero no garantiza que el punto
# de rotación sea el de la pista correcta (San Fernando tiene una pista de
# pasto paralela a la principal, y el ILS salió alineado con la de pasto).
# Esta vez "orientacion" sale de las coordenadas EXACTAS de las dos cabeceras
# de pista (le_latitude_deg/he_latitude_deg del dataset de OurAirports,
# https://davidmegginson.github.io/ourairports-data/runways.csv), no de un
# número de pista interpretado a mano -- y como bonus de verificación, el
# punto medio entre esas dos cabeceras coincidió casi exacto con la
# coordenada aproximada que ya teníamos para El Palomar y Mariano Moreno
# (no así en San Fernando, que tiene más de una pista -- de ahí el problema).
var aeropuertos_lla = [
	{"nombre": "Aeroparque", "lat": -34.5589, "lon": -58.4164},
	{"nombre": "San Fernando", "lat": -34.4532, "lon": -58.5896},
	{"nombre": "Ezeiza", "lat": -34.8222, "lon": -58.5358},
	{"nombre": "El Palomar", "lat": -34.6100, "lon": -58.6125},
	{"nombre": "Morón", "lat": -34.6764, "lon": -58.6428},
	# Pedido explícito 2026-09-21 -- SADD, aeródromo de aviación privada/
	# ejecutiva en el gran Buenos Aires. Coordenadas oficiales investigadas
	# (34°29'52"S 58°36'20"W); el usuario se ofreció a mapear la orientación
	# real de la pista con "Marcar lugar" cuando pueda, igual que El Palomar/
	# Morón/Ezeiza.
	{"nombre": "Don Torcuato", "lat": -34.49778, "lon": -58.60556},
	# Pedido explícito 2026-09-21 -- base militar (aviones de guerra), con
	# hospital. Coordenada = punto medio de las dos cabeceras reales que
	# marcó el usuario (ver aeropuertos_dos_cabeceras).
	{"nombre": "Campo de Mayo", "lat": -34.53476, "lon": -58.67191},
	# Coordenadas reales del Aeródromo de Quilmes (OACI SADQ, pista 18/36 de
	# pasto, 1010m), corregidas 2026-09-22 -- la aproximación anterior
	# (centro del pueblo) quedaba a ~3km de la pista real y no se encontraba.
	{"nombre": "Quilmes", "lat": -34.706667, "lon": -58.244444},
	{"nombre": "La Plata", "lat": -34.9744, "lon": -57.8956},
	{"nombre": "Villa Gesell", "lat": -37.2344, "lon": -57.0214},
	{"nombre": "Mar del Plata", "lat": -37.9342, "lon": -57.5733},
]

# AMPLIADO 2026-09-20/21 (pedido explícito, "para recorrer todo el país"):
# extraídos de las rutas reales de Little Navmap en "Rutas LNM/" (waypoints
# marcados como "Airport" en los .gpx). Cubren Argentina, Uruguay, Paraguay,
# Brasil y São Borja. TODOS los nombres genéricos ("Aeropuerto XXXX") de la
# primera pasada se investigaron y corrigieron 2026-09-21 -- los de código
# ICAO de 4 letras (SAxx/SUxx) se buscaron en Wikipedia/OurAirports; los de
# código corto de 3 letras (pistas chicas sin ICAO oficial, probablemente
# privadas/estancias) se identificaron por geocodificación inversa (el
# pueblo/zona real más cercano a esa coordenada exacta) -- mejor el nombre
# real del lugar que un código sin sentido para el jugador.
var aeropuertos_pais_lla = [
	{"nombre": "Córdoba (Taravella)", "lat": -31.3100, "lon": -64.2083},
	{"nombre": "Mendoza (El Plumerillo)", "lat": -32.8317, "lon": -68.7928},
	{"nombre": "San Carlos de Bariloche", "lat": -41.1511, "lon": -71.1578},
	{"nombre": "Jujuy (Gob. Guzmán)", "lat": -24.3928, "lon": -65.0978},
	{"nombre": "El Calafate", "lat": -50.2803, "lon": -72.0533},
	{"nombre": "Río Gallegos", "lat": -51.6089, "lon": -69.3128},
	{"nombre": "Ushuaia (Malvinas Argentinas)", "lat": -54.8433, "lon": -68.2956},
	{"nombre": "San Juan", "lat": -31.5714, "lon": -68.4183},
	{"nombre": "Tandil", "lat": -37.2344, "lon": -59.2286},
	{"nombre": "Necochea/Quequén", "lat": -36.5422, "lon": -56.7214},
	# Pedido explícito 2026-09-21 -- "ahí vive mi familia, voy seguido, para
	# excursiones a la Basílica". Pista de tierra, coordenada = punto medio
	# de las dos cabeceras reales que marcó el usuario.
	{"nombre": "Aero Club Luján", "lat": -34.55114, "lon": -59.07861},
	{"nombre": "Aeródromo Chivilcoy", "lat": -34.96232, "lon": -60.03182},
	{"nombre": "Tucumán", "lat": -26.8382, "lon": -65.1043},
	{"nombre": "Salta", "lat": -24.8597, "lon": -65.4869},
	{"nombre": "Catamarca", "lat": -28.5931, "lon": -65.7512},
	{"nombre": "La Rioja", "lat": -29.3804, "lon": -66.7957},
	{"nombre": "Rosario (Islas Malvinas)", "lat": -32.9036, "lon": -60.7844},
	{"nombre": "Gualeguaychú", "lat": -33.0056, "lon": -58.6128},
	{"nombre": "Asunción (Paraguay)", "lat": -25.2411, "lon": -57.5168},
	{"nombre": "Foz de Iguazú (Brasil)", "lat": -25.6003, "lon": -54.4850},
	{"nombre": "Estancia Hércules (Misiones)", "lat": -25.4617, "lon": -54.5972},
	{"nombre": "Concordia (Entre Ríos)", "lat": -31.2970, "lon": -57.9966},
	{"nombre": "Punta Indio (Base Aeronaval)", "lat": -35.3533, "lon": -57.2900},
	{"nombre": "La Cumbre (Córdoba)", "lat": -31.0050, "lon": -64.5328},
	{"nombre": "Coronel Olmedo (Córdoba)", "lat": -31.4878, "lon": -64.1419},
	# SADF/SADL/SADQ (San Fernando, La Plata, Quilmes) NO se agregan acá --
	# son los MISMOS 3 aeropuertos que ya están en aeropuertos_lla (arriba),
	# solo con una coordenada casi idéntica -- habría quedado duplicado.
	{"nombre": "Mariano Moreno (José C. Paz)", "lat": -34.5604, "lon": -58.7895, "orientacion": 161.0},  # pista 16/34, cabeceras reales OurAirports
	{"nombre": "Las Termas de Río Hondo", "lat": -27.4966, "lon": -64.9360},
	{"nombre": "Marcos Juárez (Córdoba)", "lat": -32.6944, "lon": -62.1528},
	{"nombre": "Monte Caseros (Corrientes)", "lat": -30.2703, "lon": -57.6394},
	{"nombre": "Posadas (Misiones)", "lat": -27.3858, "lon": -55.9706},
	{"nombre": "Curuzú Cuatiá (Corrientes)", "lat": -29.7725, "lon": -57.9828},
	{"nombre": "Viedma (Río Negro)", "lat": -40.8701, "lon": -62.9966},
	{"nombre": "São Borja (Brasil)", "lat": -28.6531, "lon": -56.0328},
	{"nombre": "Apóstoles (Misiones)", "lat": -27.8994, "lon": -55.7672},
	{"nombre": "Alvear (Santa Fe)", "lat": -33.0464, "lon": -60.5969},
	{"nombre": "Bell Ville (Córdoba)", "lat": -32.6583, "lon": -62.7019},
	{"nombre": "Las Flores (Bs. As.)", "lat": -36.0664, "lon": -59.1006},
	{"nombre": "Cañada de Gómez (Santa Fe)", "lat": -32.8086, "lon": -61.3650},
	{"nombre": "General Madariaga (Bs. As.)", "lat": -37.0386, "lon": -57.1364},
	{"nombre": "Chascomús (Bs. As.)", "lat": -35.5422, "lon": -58.0519},
	{"nombre": "Rauch (Bs. As.)", "lat": -36.7497, "lon": -59.0672},
	{"nombre": "Mercedes (Corrientes)", "lat": -29.2222, "lon": -58.0881},
	{"nombre": "Vidal (Mar Chiquita, Bs. As.)", "lat": -37.4678, "lon": -57.7672},
	{"nombre": "Carmelo (Uruguay)", "lat": -33.9661, "lon": -58.3253},
	{"nombre": "Mercedes (Uruguay)", "lat": -33.2486, "lon": -58.0728},
	{"nombre": "Paysandú (Uruguay)", "lat": -32.3631, "lon": -58.0664},
	{"nombre": "Salto (Uruguay)", "lat": -31.4347, "lon": -57.9842},
	# Destinos nuevos 2026-09-22 -- aeropuertos importantes/turísticos de
	# Argentina que faltaban del todo (ni siquiera existían como destino),
	# investigados a pedido explícito ("aeropuertos importantes que nos estén
	# faltando, turísticos, los importantes"). Coordenada = punto medio entre
	# las dos cabeceras reales (ver aeropuertos_dos_cabeceras para el ILS).
	{"nombre": "Neuquén (Presidente Perón)", "lat": -38.949, "lon": -68.155667},
	{"nombre": "Comodoro Rivadavia (Gral. Mosconi)", "lat": -45.784667, "lon": -67.460917},
	{"nombre": "Trelew (Almirante Zar)", "lat": -43.210417, "lon": -65.270417},
	{"nombre": "Bahía Blanca (Comandante Espora)", "lat": -38.727083, "lon": -62.1535},
	{"nombre": "Resistencia (Chaco)", "lat": -27.449917, "lon": -59.056},
	{"nombre": "Formosa (El Pucú)", "lat": -26.212833, "lon": -58.228167},
	{"nombre": "Santiago del Estero", "lat": -27.765667, "lon": -64.309917},
	{"nombre": "Santa Rosa (La Pampa)", "lat": -36.588167, "lon": -64.275583},
	{"nombre": "San Luis (Brig. Mayor César Raúl Ojeda)", "lat": -33.273167, "lon": -66.3565},
	{"nombre": "Puerto Iguazú (Cataratas del Iguazú)", "lat": -25.73725, "lon": -54.473417},
	{"nombre": "San Martín de los Andes (Chapelco)", "lat": -40.075333, "lon": -71.13725},
	{"nombre": "Esquel", "lat": -42.903833, "lon": -71.1355},
	{"nombre": "San Rafael (Mendoza)", "lat": -34.587833, "lon": -68.403583},
	{"nombre": "Villa de Merlo (Valle del Conlara)", "lat": -32.3845, "lon": -65.18575},
	{"nombre": "Río Cuarto (Las Higueras)", "lat": -33.092167, "lon": -64.269333},
]

# ILS de dos cabeceras MEDIDAS por el usuario (pedido 2026-09-21) -- después
# de que el ILS de El Palomar saliera desalineado usando un rumbo de
# internet (165.3°, de OurAirports) sobre una sola coordenada aproximada, el
# usuario voló hasta las dos puntas REALES del asfalto (con velocidad cero,
# "la mejor idea que se me ocurrió del mundo" para poder posicionarse bien)
# y las marcó con "Marcar lugar". Confirmado: la POSICIÓN de esos dos puntos
# coincide casi exacto con las cabeceras oficiales (a menos de 50m), pero el
# RUMBO calculado entre ellos da 157°, no 165.3° -- 8° de diferencia, que a
# varios km de distancia alcanza para que el corredor de aros se vea
# desalineado del asfalto real. Con dos puntos reales medidos no hace falta
# ROTAR ningún nodo por compás (la fuente del problema) -- se arma la fila
# de aros directo entre los dos puntos, mismo mecanismo ya probado en los
# aros de la licencia de helicóptero (ver _actualizar_aros_licencia_frame en
# principal.gd), extendida más allá de CADA punta para las dos direcciones
# de aproximación -- las DOS activas siempre a la vez (pedido explícito,
# "para que no haya complicaciones, activar los dos, uno de un color y otro
# de otro, y elegís vos de qué lado meterte").
var aeropuertos_dos_cabeceras = [
	# "cab1_alt"/"cab2_alt" (pedido 2026-09-21, "el aro de cerca quedó
	# enterrado"): antes usábamos una altura fija de 8m para cualquier
	# aeropuerto -- pero El Palomar y Morón están más elevados que eso de
	# verdad, y el aro más cercano (poca altura de glideslope todavía) quedó
	# clavado bajo tierra. Usamos la altitud REAL que ya midió el usuario al
	# marcar cada punto con "Marcar lugar" (la misma que quedó guardada en
	# lugares_marcados.json), no un número inventado.
	{"nombre": "El Palomar", "cab1_lat": -34.6009427588694, "cab1_lon": -58.6171759926138, "cab1_alt": 40.4461734620854,
		"cab2_lat": -34.6182983867754, "cab2_lon": -58.6083196699711, "cab2_alt": 49.8827434657142},
	{"nombre": "Morón", "cab1_lat": -34.6885488419284, "cab1_lon": -58.6462909490502, "cab1_alt": 48.0545884827152,
		"cab2_lat": -34.6641891470396, "cab2_lon": -58.6393783149291, "cab2_alt": 43.2744749914855},
	# Ezeiza tiene DOS pistas cruzadas de verdad (pedido explícito, "me
	# interesa que estén las dos") -- cada una es su propia entrada acá, con
	# su propio par de aros independiente. Las dos activas a la vez cuando
	# se prende el ILS, igual que las dos puntas de una pista sola.
	{"nombre": "Ezeiza Pista 1", "cab1_lat": -34.8190632489792, "cab1_lon": -58.5533262507912, "cab1_alt": 37.2082823291421,
		"cab2_lat": -34.8253907177614, "cab2_lon": -58.518147409345, "cab2_alt": 38.4050613399595},
	{"nombre": "Ezeiza Pista 2", "cab1_lat": -34.8352106117019, "cab1_lon": -58.5245659448607, "cab1_alt": 36.6112232338637,
		"cab2_lat": -34.8085286713313, "cab2_lon": -58.5338650217645, "cab2_alt": 37.6294713849202},
	{"nombre": "Campo de Mayo", "cab1_lat": -34.5274077113893, "cab1_lon": -58.6722874442012, "cab1_alt": 44.9625268978998,
		"cab2_lat": -34.5421162803178, "cab2_lon": -58.6715322190383, "cab2_alt": 45.1745231309906},
	{"nombre": "Aero Club Luján", "cab1_lat": -34.5544970604824, "cab1_lon": -59.0763310141286, "cab1_alt": 37.872753161937,
		"cab2_lat": -34.5477829338129, "cab2_lon": -59.0808948351915, "cab2_alt": 40.5593090755865},
	{"nombre": "Aeródromo Chivilcoy", "cab1_lat": -34.9584894215692, "cab1_lon": -60.0288016448983, "cab1_alt": 64.8651760295033,
		"cab2_lat": -34.9661466452173, "cab2_lon": -60.0348295528628, "cab2_alt": 65.5044843032956},
	{"nombre": "Rosario", "cab1_lat": -32.8899887834222, "cab1_lon": -60.7815807340708, "cab1_alt": 52.070948225446,
		"cab2_lat": -32.9167810491964, "cab2_lon": -60.7877080510417, "cab2_alt": 49.031780987978},
	{"nombre": "Quilmes", "cab1_lat": -34.7010847292481, "cab1_lon": -58.2453531748818, "cab1_alt": 19.4314980823547,
		"cab2_lat": -34.7110662434358, "cab2_lon": -58.2438698890939, "cab2_alt": 19.3805471602827},
	{"nombre": "La Plata", "cab1_lat": -34.9617070587302, "cab1_lon": -57.8908018869423, "cab1_alt": 35.0786370728165,
		"cab2_lat": -34.9755390857504, "cab2_lon": -57.8949704513399, "cab2_alt": 34.4030680740252},
	# PRUEBA 2026-09-22 (a diferencia de TODAS las entradas de arriba, estas
	# dos NO se midieron volando -- son coordenadas de umbral tomadas 100% de
	# SkyVector, para comprobar en vuelo si calzan bien contra la pista real
	# o si aparece un desfasaje. Si al pasar por acá los aros quedan bien
	# alineados con el asfalto, confirma que se puede confiar en datos
	# públicos para mapear aeropuertos nuevos sin medir las dos cabeceras a
	# mano -- un ahorro grande de tiempo (ver el análisis de patrón de error
	# hecho por los agentes/Gemini/ChatGPT, que no encontró un desfasaje
	# sistemático corregible, pero esta es la prueba real, en el propio
	# simulador, no solo en el papel).
	# ancho_medio_pista: el dato oficial (SkyVector/AIP, 45m) dejó las luces
	# vivas todavía angostas contra el asfalto real de Cesium (comparación
	# del usuario: "como un carril y medio de una autopista de tres" -- hay
	# que duplicar). Subido a ojo por encima del dato oficial, prioridad a
	# la prueba real en el simulador.
	{"nombre": "Aeroparque", "cab1_lat": -34.554, "cab1_lon": -58.425333, "cab1_alt": 6.1,
		"cab2_lat": -34.563833, "cab2_lon": -58.4075, "cab2_alt": 4.88, "ancho_medio_pista": 45.0},
	{"nombre": "Villa Gesell", "cab1_lat": -37.234, "cab1_lon": -57.037667, "cab1_alt": 5.49,
		"cab2_lat": -37.236833, "cab2_lon": -57.02, "cab2_alt": 5.49},
	# Villa Gesell (arriba) dio "precisión quirúrgica" en la prueba en vuelo
	# 2026-09-22 -- Mar del Plata no tenía ILS agregado (por eso no aparecían
	# aros), se suma con el mismo método (solo SkyVector, sin medir a mano).
	{"nombre": "Mar del Plata", "cab1_lat": -37.928833, "cab1_lon": -57.583833, "cab1_alt": 21.64,
		"cab2_lat": -37.9395, "cab2_lon": -57.562667, "cab2_alt": 21.64},
	# Tanda grande 2026-09-22 -- ILS para todos los aeropuertos que ya eran
	# destino en el juego pero no tenían aros (pedido explícito, "todos los
	# aeropuertos que tenemos lo tienen que tener"), datos de SkyVector, mismo
	# método/fuente que los de arriba. Quedaron afuera (sin página real de
	# aeródromo con pista en SkyVector, verificado): Don Torcuato, Necochea/
	# Quequén, Marcos Juárez, Monte Caseros, Curuzú Cuatiá, Mercedes
	# (Corrientes), Apóstoles, Alvear, Cañada de Gómez, Bell Ville, Las
	# Flores, General Madariaga, Chascomús, Rauch, São Borja -- ninguno tiene
	# ficha de aeródromo en SkyVector pese a estar en la lista de destinos.
	# ancho_medio_pista: medido por el usuario con "Marcar lugar" (puntos
	# "sanfer 5"/"sanfer 6" en lugares_marcados.json) -- ~33m de ancho real
	# de punta a punta, la mitad para el offset desde el eje central.
	# ORIENTACIÓN: 2026-09-26 se giró +12° en sentido horario (~42°->~54°),
	# pero después de verlo en vivo el usuario aclaró que las luces y el ILS
	# YA estaban bien coordinados ENTRE SÍ desde el principio -- lo que hacía
	# falta era girar TODO el conjunto 12° hacia la izquierda (antihorario)
	# respecto de esa última posición, o sea volver exactamente a las cab1/
	# cab2 ORIGINALES (~42°). Revertido.
	# ANCHO: el usuario reporta que necesita bastante más que Aeroparque
	# (que solo pedía "un poquito de cada lado" y quedó en 45.0) -- acá
	# describió "muy angostas", así que sube proporcionalmente más que los
	# 33m medidos con "sanfer 5"/"sanfer 6". A ojo, pendiente de otra
	# vuelta de ajuste en vivo.
	{"nombre": "San Fernando", "cab1_lat": -34.459167, "cab1_lon": -58.595667, "cab1_alt": 10.06,
		"cab2_lat": -34.45, "cab2_lon": -58.5855, "cab2_alt": 3.35, "ancho_medio_pista": 40.0},
	{"nombre": "Córdoba (Taravella)", "cab1_lat": -31.3245, "cab1_lon": -64.208167, "cab1_alt": 465.15,
		"cab2_lat": -31.295667, "cab2_lon": -64.2085, "cab2_alt": 488.99},
	{"nombre": "Mendoza (El Plumerillo)", "cab1_lat": -32.819167, "cab1_lon": -68.792667, "cab1_alt": 698.02,
		"cab2_lat": -32.844333, "cab2_lon": -68.793, "cab2_alt": 704.11},
	{"nombre": "San Carlos de Bariloche", "cab1_lat": -41.146667, "cab1_lon": -71.170667, "cab1_alt": 834.87,
		"cab2_lat": -41.1555, "cab2_lon": -71.145167, "cab2_alt": 841.57},
	{"nombre": "Jujuy (Gob. Guzmán)", "cab1_lat": -24.381167, "cab1_lon": -65.105, "cab1_alt": 920.19,
		"cab2_lat": -24.404333, "cab2_lon": -65.090667, "cab2_alt": 884.83},
	{"nombre": "El Calafate", "cab1_lat": -50.281333, "cab1_lon": -72.071, "cab1_alt": 197.20,
		"cab2_lat": -50.279167, "cab2_lon": -72.035333, "cab2_alt": 192.63},
	{"nombre": "Río Gallegos", "cab1_lat": -51.61, "cab1_lon": -69.337167, "cab1_alt": 18.90,
		"cab2_lat": -51.607667, "cab2_lon": -69.287167, "cab2_alt": 15.85},
	{"nombre": "Ushuaia (Malvinas Argentinas)", "cab1_lat": -54.844, "cab1_lon": -68.314667, "cab1_alt": 24.99,
		"cab2_lat": -54.8425, "cab2_lon": -68.2765, "cab2_alt": 21.03},
	{"nombre": "San Juan", "cab1_lat": -31.560333, "cab1_lon": -68.418333, "cab1_alt": 593.83,
		"cab2_lat": -31.5825, "cab2_lon": -68.418167, "cab2_alt": 591.70},
	{"nombre": "Tandil", "cab1_lat": -37.245833, "cab1_lon": -59.229833, "cab1_alt": 175.56,
		"cab2_lat": -37.223, "cab2_lon": -59.227333, "cab2_alt": 167.02},
	{"nombre": "Tucumán", "cab1_lat": -26.853833, "cab1_lon": -65.107333, "cab1_alt": 435.90,
		"cab2_lat": -26.822667, "cab2_lon": -65.101333, "cab2_alt": 455.40},
	{"nombre": "Salta", "cab1_lat": -24.872833, "cab1_lon": -65.49, "cab1_alt": 1246.40,
		"cab2_lat": -24.846333, "cab2_lon": -65.484167, "cab2_alt": 1230.60},
	{"nombre": "Catamarca", "cab1_lat": -28.6055, "cab1_lon": -65.754167, "cab1_alt": 459.62,
		"cab2_lat": -28.580833, "cab2_lon": -65.748167, "cab2_alt": 474.24},
	{"nombre": "La Rioja", "cab1_lat": -29.3915, "cab1_lon": -66.8025, "cab1_alt": 441.30,
		"cab2_lat": -29.369333, "cab2_lon": -66.788833, "cab2_alt": 428.51},
	{"nombre": "Gualeguaychú", "cab1_lat": -33.0115, "cab1_lon": -58.613, "cab1_alt": 22.86,
		"cab2_lat": -32.9995, "cab2_lon": -58.612667, "cab2_alt": 16.16},
	{"nombre": "Concordia (Entre Ríos)", "cab1_lat": -31.303333, "cab1_lon": -58.0005, "cab1_alt": 34.14,
		"cab2_lat": -31.2905, "cab2_lon": -57.992667, "cab2_alt": 23.17},
	{"nombre": "Las Termas de Río Hondo", "cab1_lat": -27.507833, "cab1_lon": -64.9365, "cab1_alt": 278.29,
		"cab2_lat": -27.485333, "cab2_lon": -64.935333, "cab2_alt": 280.42},
	{"nombre": "Posadas (Misiones)", "cab1_lat": -27.396, "cab1_lon": -55.970333, "cab1_alt": 130.47,
		"cab2_lat": -27.375667, "cab2_lon": -55.970667, "cab2_alt": 114.32},
	{"nombre": "Viedma (Río Negro)", "cab1_lat": -40.867, "cab1_lon": -63.011167, "cab1_alt": 6.10,
		"cab2_lat": -40.873167, "cab2_lon": -62.982, "cab2_alt": 4.57},
	{"nombre": "Asunción (Paraguay)", "cab1_lat": -25.254667, "cab1_lon": -57.522667, "cab1_alt": 88.39,
		"cab2_lat": -25.225, "cab2_lon": -57.5155, "cab2_alt": 75.90},
	{"nombre": "Foz de Iguazú (Brasil)", "cab1_lat": -25.589333, "cab1_lon": -54.4955, "cab1_alt": 232.28,
		"cab2_lat": -25.602333, "cab2_lon": -54.479, "cab2_alt": 239.60},
	{"nombre": "Paysandú (Uruguay)", "cab1_lat": -32.370167, "cab1_lon": -58.063333, "cab1_alt": 49.07,
		"cab2_lat": -32.356667, "cab2_lon": -58.060833, "cab2_alt": 38.10},
	{"nombre": "Salto (Uruguay)", "cab1_lat": -31.444, "cab1_lon": -57.9905, "cab1_alt": 42.98,
		"cab2_lat": -31.432833, "cab2_lon": -57.98, "cab2_alt": 39.02},
	{"nombre": "Carmelo (Uruguay)", "cab1_lat": -33.9615, "cab1_lon": -58.327167, "cab1_alt": 10.97,
		"cab2_lat": -33.970667, "cab2_lon": -58.323667, "cab2_alt": 10.06},
	{"nombre": "Mercedes (Uruguay)", "cab1_lat": -33.242833, "cab1_lon": -58.0775, "cab1_alt": 14.02,
		"cab2_lat": -33.252667, "cab2_lon": -58.071333, "cab2_alt": 20.12},
	# Aeropuertos NUEVOS (no existían como destino en absoluto) -- pedido
	# explícito de completar los importantes/turísticos de Argentina que
	# faltaban del todo. Mismo método, datos de SkyVector. Se agregan acá Y
	# como destino nuevo en aeropuertos_pais_lla más abajo.
	{"nombre": "Neuquén (Presidente Perón)", "cab1_lat": -38.949, "cab1_lon": -68.1705, "cab1_alt": 271.58,
		"cab2_lat": -38.949, "cab2_lon": -68.140833, "cab2_alt": 271.58},
	{"nombre": "Comodoro Rivadavia (Gral. Mosconi)", "cab1_lat": -45.786333, "cab1_lon": -67.473833, "cab1_alt": 56.39,
		"cab2_lat": -45.783, "cab2_lon": -67.448, "cab2_alt": 56.39},
	{"nombre": "Trelew (Almirante Zar)", "cab1_lat": -43.2145, "cab1_lon": -65.285167, "cab1_alt": 34.75,
		"cab2_lat": -43.206333, "cab2_lon": -65.255667, "cab2_alt": 33.53},
	{"nombre": "Bahía Blanca (Comandante Espora)", "cab1_lat": -38.716667, "cab1_lon": -62.157333, "cab1_alt": 75.59,
		"cab2_lat": -38.7375, "cab2_lon": -62.149667, "cab2_alt": 75.59},
	{"nombre": "Resistencia (Chaco)", "cab1_lat": -27.461667, "cab1_lon": -59.061, "cab1_alt": 52.12,
		"cab2_lat": -27.438167, "cab2_lon": -59.051, "cab2_alt": 52.73},
	{"nombre": "Formosa (El Pucú)", "cab1_lat": -26.220167, "cab1_lon": -58.232, "cab1_alt": 59.14,
		"cab2_lat": -26.2055, "cab2_lon": -58.224333, "cab2_alt": 59.14},
	{"nombre": "Santiago del Estero", "cab1_lat": -27.775333, "cab1_lon": -64.315667, "cab1_alt": 200.90,
		"cab2_lat": -27.756, "cab2_lon": -64.304167, "cab2_alt": 200.90},
	{"nombre": "Santa Rosa (La Pampa)", "cab1_lat": -36.598333, "cab1_lon": -64.278167, "cab1_alt": 189.88,
		"cab2_lat": -36.578, "cab2_lon": -64.273, "cab2_alt": 189.88},
	{"nombre": "San Luis (Brig. Mayor César Raúl Ojeda)", "cab1_lat": -33.286333, "cab1_lon": -66.358333, "cab1_alt": 700.73,
		"cab2_lat": -33.26, "cab2_lon": -66.354667, "cab2_alt": 710.79},
	{"nombre": "Puerto Iguazú (Cataratas del Iguazú)", "cab1_lat": -25.731, "cab1_lon": -54.488333, "cab1_alt": 279.20,
		"cab2_lat": -25.7435, "cab2_lon": -54.4585, "cab2_alt": 279.20},
	{"nombre": "San Martín de los Andes (Chapelco)", "cab1_lat": -40.0795, "cab1_lon": -71.150833, "cab1_alt": 789.13,
		"cab2_lat": -40.071167, "cab2_lon": -71.123667, "cab2_alt": 789.13},
	{"nombre": "Esquel", "cab1_lat": -42.909667, "cab1_lon": -71.147833, "cab1_alt": 799.19,
		"cab2_lat": -42.898, "cab2_lon": -71.123167, "cab2_alt": 781.20},
	{"nombre": "San Rafael (Mendoza)", "cab1_lat": -34.584833, "cab1_lon": -68.4145, "cab1_alt": 754.69,
		"cab2_lat": -34.590833, "cab2_lon": -68.392667, "cab2_alt": 754.69},
	{"nombre": "Villa de Merlo (Valle del Conlara)", "cab1_lat": -32.3955, "cab1_lon": -65.189833, "cab1_alt": 615.70,
		"cab2_lat": -32.3735, "cab2_lon": -65.181667, "cab2_alt": 615.70},
	{"nombre": "Río Cuarto (Las Higueras)", "cab1_lat": -33.0995, "cab1_lon": -64.277833, "cab1_alt": 420.32,
		"cab2_lat": -33.084833, "cab2_lon": -64.260833, "cab2_alt": 420.32},
]

# Pueblos y referencias reales a lo largo de las rutas (pedido 2026-09-21,
# "para que sea más entretenido ver por dónde va pasando"). Extraídos de los
# waypoints tipo "User" de TODAS las rutas .gpx en "Rutas LNM/" -- se
# descartaron los cruces de ruta, los waypoints IFR sin nombre (WP1, WP
# AKRAS, etc.) y las anotaciones de vuelo del propio Pablo ("A la izquierda
# se ve...", "Próximos a Aeropuerto..."), dejando pueblos, parajes, islas,
# lagunas y reservas -- son carteles CHICOS y de cerca nada más (no
# gigantes como los aeropuertos), para no saturar el mapa con casi 180 de
# estos.
var pueblos_lla = [
	{"nombre": "Abufera Mar Chiquita", "lat": -37.6512, "lon": -57.3487},
	{"nombre": "Aero club Escobar", "lat": -34.2971, "lon": -58.7916},
	{"nombre": "Aeroclub Baradero", "lat": -33.8158, "lon": -59.487},
	{"nombre": "Alsina", "lat": -33.8961, "lon": -59.3827},
	{"nombre": "Altos de Mendoza - Rosario", "lat": -32.9427, "lon": -60.7321},
	{"nombre": "Alvear - Rosario", "lat": -32.9787, "lon": -60.6852},
	{"nombre": "Area Natural protegida Rocas Coloradas", "lat": -45.5429, "lon": -67.1688},
	{"nombre": "Armstrong", "lat": -32.7752, "lon": -61.6017},
	{"nombre": "Arroyo Moyano", "lat": -34.5513, "lon": -59.6363},
	{"nombre": "Arroyo Seco", "lat": -33.1347, "lon": -60.459},
	{"nombre": "Arroyo Tia Lucha", "lat": -33.5614, "lon": -59.8473},
	{"nombre": "Balneario Parque Mar Chiquita", "lat": -37.7422, "lon": -57.4208},
	{"nombre": "Baniado de Castro", "lat": -33.6058, "lon": -59.784},
	{"nombre": "Baradero", "lat": -33.8085, "lon": -59.5083},
	{"nombre": "Barrio Islas", "lat": -34.349, "lon": -58.7458},
	{"nombre": "Barrio Los Lagos", "lat": -34.4008, "lon": -58.6701},
	{"nombre": "Barrio San Agustin", "lat": -34.3917, "lon": -58.683},
	{"nombre": "Barrio San Francisco", "lat": -34.3857, "lon": -58.6923},
	{"nombre": "Barrio Santa Barbara", "lat": -34.4358, "lon": -58.6196},
	{"nombre": "Barrio Santa Ines", "lat": -34.5529, "lon": -59.0699},
	{"nombre": "Barrio Santa Isabel", "lat": -34.365, "lon": -58.7226},
	{"nombre": "Barrio de Belgrano - Caba", "lat": -34.5593, "lon": -58.4591},
	{"nombre": "Barrio de Palermo - Caba", "lat": -34.579, "lon": -58.4253},
	{"nombre": "Beccar", "lat": -34.4708, "lon": -58.5455},
	{"nombre": "Belen de Escobar", "lat": -34.3162, "lon": -58.776},
	{"nombre": "Bell Ville - Pcia de Cordoba", "lat": -32.6421, "lon": -62.7204},
	{"nombre": "Berazategui", "lat": -34.7465, "lon": -58.1755},
	{"nombre": "Biedma - Capital de Rio Negro", "lat": -42.4235, "lon": -65.1775},
	{"nombre": "CORDOBA CAPITAL", "lat": -31.4348, "lon": -64.162},
	{"nombre": "Camet Norte", "lat": -37.8212, "lon": -57.4843},
	{"nombre": "Campana", "lat": -34.1789, "lon": -58.9772},
	{"nombre": "Caniada de Gomez", "lat": -32.8044, "lon": -61.395},
	{"nombre": "Carcarana", "lat": -32.849, "lon": -61.1489},
	{"nombre": "Carmen de Areco", "lat": -34.5569, "lon": -59.8126},
	{"nombre": "Central Termica Costanera", "lat": -34.6227, "lon": -58.3353},
	{"nombre": "Cerca del pueblo Irunia", "lat": -26.1505, "lon": -54.9428},
	{"nombre": "Cerro Chapa - 210 Metros", "lat": -25.6294, "lon": -54.5092},
	{"nombre": "Cerro Despensa - 1150 Metros", "lat": -24.5382, "lon": -65.2181},
	{"nombre": "Chacabuco", "lat": -34.5541, "lon": -60.0111},
	{"nombre": "Chacras del Parana", "lat": -33.9787, "lon": -59.2659},
	{"nombre": "Chicoana - Pcia de Salta", "lat": -25.1686, "lon": -65.427},
	{"nombre": "Cipolletti - Pcia dde Neuquen", "lat": -38.899, "lon": -68.0205},
	{"nombre": "City Bell", "lat": -34.8542, "lon": -58.0382},
	{"nombre": "Correa", "lat": -32.8336, "lon": -61.2457},
	{"nombre": "Cortines", "lat": -34.5594, "lon": -59.2063},
	{"nombre": "Cosquin - Cordoba", "lat": -31.243, "lon": -64.4659},
	{"nombre": "Costa Atlantica Tierra del Fuego", "lat": -53.5142, "lon": -67.9357},
	{"nombre": "Crotera Cerro do Jarau", "lat": -30.1772, "lon": -56.4282},
	{"nombre": "Cruz del Eje - Cordoba", "lat": -30.3664, "lon": -65.2978},
	{"nombre": "Dique Lujan", "lat": -34.3702, "lon": -58.7017},
	{"nombre": "Dolores - Uruguay", "lat": -33.5345, "lon": -58.1787},
	{"nombre": "El Carril - Pcia de Salta", "lat": -25.0632, "lon": -65.4462},
	{"nombre": "El Tala", "lat": -26.0993, "lon": -65.2495},
	{"nombre": "Embalse Arroyito -Depto de Confluencia - Pcia de Neuquen", "lat": -39.2147, "lon": -68.5039},
	{"nombre": "Embalse Cabra Corral", "lat": -25.3083, "lon": -65.3995},
	{"nombre": "Embalse Ezequiel Ramos Mexia", "lat": -39.3805, "lon": -68.7298},
	{"nombre": "Embalse Piedra del Aguila", "lat": -40.3957, "lon": -70.1264},
	{"nombre": "Estancia Villa Maria", "lat": -34.9581, "lon": -58.574},
	{"nombre": "Ezpeleta", "lat": -34.7309, "lon": -58.1963},
	{"nombre": "General Mansilla", "lat": -35.0634, "lon": -57.7446},
	{"nombre": "General Rodriguez", "lat": -34.5611, "lon": -58.944},
	{"nombre": "Guachipas - Pcia de Salta", "lat": -25.7127, "lon": -65.3219},
	{"nombre": "Guichon", "lat": -32.3022, "lon": -57.36},
	{"nombre": "Hipodromo de San Isidro", "lat": -34.488, "lon": -58.5205},
	{"nombre": "Hudson", "lat": -34.7755, "lon": -58.1386},
	{"nombre": "Isla Bodega", "lat": -33.413, "lon": -60.0593},
	{"nombre": "Isla Cambacua", "lat": -32.5613, "lon": -58.2344},
	{"nombre": "Isla Cattaneo", "lat": -33.3449, "lon": -60.1579},
	{"nombre": "Isla La Espera", "lat": -33.4274, "lon": -60.0394},
	{"nombre": "Isla Nueva e Isla Guaca", "lat": -33.3866, "lon": -60.0973},
	{"nombre": "Isla San Pedro", "lat": -33.6558, "lon": -59.7138},
	{"nombre": "Isla del Paraguayo", "lat": -33.1397, "lon": -60.4492},
	{"nombre": "Itapua Porty Km 40", "lat": -26.6941, "lon": -55.3969},
	{"nombre": "James Craik", "lat": -32.1297, "lon": -63.4098},
	{"nombre": "Jockey Club -Rosario", "lat": -32.9302, "lon": -60.7479},
	{"nombre": "Jockey Club de Rosario", "lat": -32.9308, "lon": -60.746},
	{"nombre": "La Aguadita - Pcia de Tucuman", "lat": -26.741, "lon": -65.1245},
	{"nombre": "La Falda - Cordoba", "lat": -31.1094, "lon": -64.4862},
	{"nombre": "La Lucila", "lat": -34.5039, "lon": -58.4976},
	{"nombre": "La Plata (Pueblo)", "lat": -34.9045, "lon": -57.975},
	{"nombre": "La Union", "lat": -34.8697, "lon": -58.5483},
	{"nombre": "Laboulaye - Pcia de Cordoba", "lat": -34.1192, "lon": -63.3502},
	{"nombre": "Lago Fagnano", "lat": -54.5418, "lon": -68.1333},
	{"nombre": "Lago Pellegrini - Pcia de Neuquen", "lat": -38.8044, "lon": -67.8715},
	{"nombre": "Laguna Chascomus", "lat": -35.5719, "lon": -58.0147},
	{"nombre": "Laguna Cuero de ZORRO", "lat": -35.7872, "lon": -62.9156},
	{"nombre": "Laguna La Picasa", "lat": -34.3332, "lon": -62.1582},
	{"nombre": "Laguna La Salada Grande", "lat": -36.912, "lon": -56.8866},
	{"nombre": "Laguna Larga", "lat": -31.7574, "lon": -63.7751},
	{"nombre": "Laguna Los Patos - Reserva Ecologica - Caba", "lat": -34.6047, "lon": -58.3581},
	{"nombre": "Laguna de San Miguel del Monte - Lugar Turistico", "lat": -35.4604, "lon": -58.7989},
	{"nombre": "Lagunas Las Chilcas", "lat": -36.8274, "lon": -56.8488},
	{"nombre": "Leandro Alem", "lat": -34.4692, "lon": -61.3651},
	{"nombre": "Leones", "lat": -32.6125, "lon": -62.2786},
	{"nombre": "Lima", "lat": -34.033, "lon": -59.1873},
	{"nombre": "Lincoln", "lat": -34.9173, "lon": -61.4876},
	{"nombre": "Loma Hermosa", "lat": -34.5609, "lon": -58.6055},
	{"nombre": "Lujan", "lat": -34.56, "lon": -59.1057},
	{"nombre": "Manfredi", "lat": -31.8221, "lon": -63.714},
	{"nombre": "Mar de Cobo", "lat": -37.7707, "lon": -57.4448},
	{"nombre": "Marabo -  Provincia de Buenos Aires", "lat": -34.5533, "lon": -58.9381},
	{"nombre": "Marco Juarez", "lat": -32.7008, "lon": -62.1077},
	{"nombre": "Martinez", "lat": -34.4951, "lon": -58.5104},
	{"nombre": "Mercado Central", "lat": -34.7123, "lon": -58.4852},
	{"nombre": "Monasterio Mariapolis Lia", "lat": -34.5443, "lon": -60.6951},
	{"nombre": "Morrison", "lat": -32.5624, "lon": -62.8107},
	{"nombre": "Municipio de Puerto Iguazu", "lat": -25.7584, "lon": -54.6162},
	{"nombre": "Muniz - Provincia de Buenos Aires", "lat": -34.5614, "lon": -58.7078},
	{"nombre": "NORDELTA", "lat": -34.4286, "lon": -58.6295},
	{"nombre": "Nuevo Paysandu - Uruguay", "lat": -32.2848, "lon": -58.0603},
	{"nombre": "Obligado", "lat": -33.588, "lon": -59.8092},
	{"nombre": "Oliva", "lat": -32.0197, "lon": -63.5193},
	{"nombre": "Olivos", "lat": -34.5102, "lon": -58.4882},
	{"nombre": "Oncativo", "lat": -31.8948, "lon": -63.645},
	{"nombre": "POLLOS SAPUCAI - Granja San Juan", "lat": -32.0266, "lon": -68.5719},
	{"nombre": "Paisandu - Uruguay", "lat": -32.3388, "lon": -58.0651},
	{"nombre": "Pampa Nogueira - Pcia de Neuquen", "lat": -40.0707, "lon": -69.6712},
	{"nombre": "Parque Centenario - Barrio Caballito - Caba", "lat": -34.6048, "lon": -58.4368},
	{"nombre": "Parque Industrial Campana", "lat": -34.2104, "lon": -58.9176},
	{"nombre": "Parque Nacional Bosques petrificados de Jaramillo", "lat": -47.6117, "lon": -67.9076},
	{"nombre": "Parque Nacional Ciervo de las Pantanos", "lat": -34.2528, "lon": -58.8567},
	{"nombre": "Parque Penia", "lat": -37.9136, "lon": -57.5471},
	{"nombre": "Parque Rafael Aguiar - San Nicolas de los Arroyos", "lat": -33.2974, "lon": -60.2258},
	{"nombre": "Parque Sarmiento - Caba", "lat": -34.5594, "lon": -58.4979},
	{"nombre": "Parque industrial Tigre", "lat": -34.4301, "lon": -58.6148},
	{"nombre": "Partido Carlos Tejedor", "lat": -35.3122, "lon": -62.1284},
	{"nombre": "Pasa el Ferrocarril Belgrano", "lat": -34.672, "lon": -58.467},
	{"nombre": "Pico Truncado", "lat": -46.7907, "lon": -67.9433},
	{"nombre": "Piedras Coloradas", "lat": -32.4229, "lon": -57.4135},
	{"nombre": "Pirapey Km 45", "lat": -26.5937, "lon": -55.3139},
	{"nombre": "Planicie de Panquehuau", "lat": -40.7998, "lon": -70.6984},
	{"nombre": "Pueblo Esther", "lat": -33.0703, "lon": -60.55},
	{"nombre": "Pueblo Lavalleja - Uruguay", "lat": -31.1611, "lon": -56.8491},
	{"nombre": "Puerto Esther - Vista Aerodromo", "lat": -33.069, "lon": -60.5496},
	{"nombre": "Punta Chacra San JERONIMO", "lat": -32.8689, "lon": -61.0165},
	{"nombre": "RIO PARANA", "lat": -25.7738, "lon": -54.629},
	{"nombre": "Ramallo", "lat": -33.471, "lon": -59.9855},
	{"nombre": "Reserva Biosfera del Parana- Zona B. Tampon", "lat": -34.0976, "lon": -58.4403},
	{"nombre": "Reserva Natural Mixta Integral Punta Lara", "lat": -34.7914, "lon": -58.118},
	{"nombre": "Reserva Natural de Objetivo Definido Rincon del Ajo", "lat": -36.3295, "lon": -57.0081},
	{"nombre": "Reserva de Biosfera Patagonia Azul", "lat": -44.5474, "lon": -66.3146},
	{"nombre": "Reserva de uso multiple Corazon de la Isla", "lat": -54.3638, "lon": -68.0412},
	{"nombre": "Reseva Natural Aguas Chiquitas", "lat": -26.5406, "lon": -65.1644},
	{"nombre": "Rio Coyle", "lat": -51.3894, "lon": -69.8047},
	{"nombre": "Rio Gallegos Canal Norte", "lat": -51.5796, "lon": -69.3751},
	{"nombre": "Rio Iguazu - Pcia de Misiones - Arg", "lat": -25.617, "lon": -54.4988},
	{"nombre": "Rio Negro", "lat": -38.3573, "lon": -67.1484},
	{"nombre": "Rio Parana vista a Laguna de Pereira", "lat": -33.5002, "lon": -59.9352},
	{"nombre": "Rio Salado", "lat": -35.6771, "lon": -58.8532},
	{"nombre": "Rio Segundo - Pcia de Cordoba", "lat": -31.654, "lon": -63.8719},
	{"nombre": "Roldan -Rosario", "lat": -32.8864, "lon": -60.9006},
	{"nombre": "Rufino", "lat": -34.2374, "lon": -62.6979},
	{"nombre": "SALTA CAPITAL", "lat": -24.7839, "lon": -65.4246},
	{"nombre": "SCSS San Sebastian", "lat": -53.2455, "lon": -68.1527},
	{"nombre": "San Clemente del Tuyu", "lat": -36.4191, "lon": -56.8866},
	{"nombre": "San Fernando (Pueblo)", "lat": -34.4512, "lon": -58.5738},
	{"nombre": "San Isidro", "lat": -34.4777, "lon": -58.5431},
	{"nombre": "San Nicolas de los Arroyos", "lat": -33.3368, "lon": -60.1751},
	{"nombre": "San Pedro", "lat": -33.6838, "lon": -59.6875},
	{"nombre": "Santa Clara del Mar", "lat": -37.8347, "lon": -57.4973},
	{"nombre": "Santa Elena del Mar", "lat": -37.867, "lon": -57.5197},
	{"nombre": "Sarmiento - Pcia de San Juan", "lat": -32.3177, "lon": -68.6393},
	{"nombre": "Timote", "lat": -35.3618, "lon": -62.2094},
	{"nombre": "Tio Pujio", "lat": -32.2392, "lon": -63.302},
	{"nombre": "Tiro Suizo - Rosario", "lat": -32.9993, "lon": -60.6582},
	{"nombre": "Tortugas", "lat": -32.7435, "lon": -61.8168},
	{"nombre": "Toyota", "lat": -34.1336, "lon": -59.029},
	{"nombre": "Unquillo", "lat": -31.2342, "lon": -64.3145},
	{"nombre": "Vicunia Mackena", "lat": -33.9271, "lon": -64.3723},
	{"nombre": "Villa  Carlos Paz - Cordoba", "lat": -31.4155, "lon": -64.4999},
	{"nombre": "Villa Constitucion", "lat": -33.2339, "lon": -60.3166},
	{"nombre": "Villa Dolores", "lat": -33.033, "lon": -60.6145},
	{"nombre": "Villa Elisa", "lat": -34.834, "lon": -58.0639},
	{"nombre": "Villa Gobernador Galvez", "lat": -33.0147, "lon": -60.6282},
	{"nombre": "Villa Riachuelo . Caba", "lat": -34.6876, "lon": -58.4739},
	{"nombre": "Villa Urquiza - Rosario", "lat": -32.9575, "lon": -60.712},
	{"nombre": "Virreyes", "lat": -34.4596, "lon": -58.5714},
	{"nombre": "Washintong", "lat": -33.8661, "lon": -64.6875},
	{"nombre": "Zarate", "lat": -34.1004, "lon": -59.0905},
]
var nodos_pueblos: Array = []

var nodos_aeropuertos: Array = []

# Misiones de helicóptero -- REEMPLAZADO 2026-09-20 con las coordenadas que
# el propio usuario trackeó a mano volando (ver lugares_marcados.json),
# muchísimo más precisas que mis geocodificaciones por dirección de antes
# (esas quedaron TODAS mal -- "hospital" ya no es la categoría, ahora es
# "aterrizaje" para helipuertos/terrazas reales o "sobrevuelo" para puntos
# panorámicos). "precio" es un var (no const) porque el panel de misiones
# deja subirlo/bajarlo a mano hasta que se fijen precios reales.
var misiones = [
	{"nombre": "Estadio de Boca Juniors (Cancha auxiliar)", "lat": -34.6338317354298, "lon": -58.366212241872, "tipo": "sobrevuelo", "precio": 400},
	{"nombre": "Estadio de Racing Club de Avellaneda", "lat": -34.6675291760371, "lon": -58.3684602652719, "tipo": "sobrevuelo", "precio": 400},
	{"nombre": "Estadio de Independiente", "lat": -34.6702036999615, "lon": -58.3709553972648, "tipo": "sobrevuelo", "precio": 400},
	{"nombre": "Obelisco", "lat": -34.6033765232351, "lon": -58.3815493289313, "tipo": "sobrevuelo", "precio": 250},
	{"nombre": "Teatro Colón", "lat": -34.6006617938084, "lon": -58.382897796275, "tipo": "sobrevuelo", "precio": 250},
	{"nombre": "Tribunales", "lat": -34.6017203852364, "lon": -58.385457182003, "tipo": "sobrevuelo", "precio": 250},
	{"nombre": "Casa Rosada (Helipuerto)", "lat": -34.6072774426033, "lon": -58.3693545243094, "tipo": "aterrizaje", "precio": 450},
	{"nombre": "Puerto Madero (Fragata Sarmiento)", "lat": -34.6092507027205, "lon": -58.3662333858193, "tipo": "sobrevuelo", "precio": 300},
	{"nombre": "Madero Harbor (Helipuerto)", "lat": -34.6209548043447, "lon": -58.3608972797835, "tipo": "aterrizaje", "precio": 400},
	{"nombre": "Hotel Hilton (Terraza)", "lat": -34.6121284211702, "lon": -58.3594125315534, "tipo": "aterrizaje", "precio": 350},
	{"nombre": "Reserva Ecológica Costanera Sur", "lat": -34.6067941564482, "lon": -58.3466122893817, "tipo": "sobrevuelo", "precio": 250},
	{"nombre": "Estadio Ferro Carril Oeste", "lat": -34.6186176961409, "lon": -58.4477442094574, "tipo": "sobrevuelo", "precio": 350},
	{"nombre": "Casa", "lat": -34.6119389327306, "lon": -58.4443453209025, "tipo": "aterrizaje", "precio": 0},
	{"nombre": "Parque de la Ciudad", "lat": -34.6698802, "lon": -58.4551198, "tipo": "sobrevuelo", "precio": 250},
]
var nodos_misiones: Array = []

# Altura geodésica REAL del avión (WGS84), actualizada cada cuadro en
# _recentrar_origen_en_avion(). principal.gd la usa para el altímetro, el
# límite de piso y el zoom de cámara, en vez de position.y -- porque ahora
# position.y se resetea a cero cada cuadro (ver comentario más abajo).
var altitud_avion: float = 5.0
# Lat/lon REALES actuales del avión -- las usa principal.gd para pedir la
# tesela correcta del mapa de calles (Plan B: mapa 2D real vía OpenStreetMap,
# en vez de pelear más con el RasterOverlay de Cesium que quedó en blanco).
var lat_avion: float = -34.5589
var lon_avion: float = -58.4164

# BUG REAL encontrado 2026-09-20: el "RUMBO" del HUD (rumbo_actual, en
# principal.gd) se calculaba con atan2(adelante.x, -adelante.z) sobre
# coordenadas CRUDAS del motor -- asumiendo que el eje X del motor ES el
# este real y -Z ES el norte real. Eso es solo aproximadamente cierto CERCA
# del origen inicial (Aeroparque); a medida que te alejás, la curvatura de
# la Tierra hace que el este/norte VERDADEROS giren respecto a esos ejes
# fijos del motor -- exactamente el mismo fenómeno que ya nos había roto la
# altitud en su momento (ver _alinear_con_vertical_real), pero nunca lo
# habíamos corregido para el RUMBO. Por eso el usuario veía un rumbo
# correcto recién arrancando (cerca del origen) y cada vez más desviado
# cuanto más lejos volaba (~1° cada varios km, coherente con curvatura).
# Exponemos acá el este/norte reales YA calculados en espacio motor (los
# mismos que se usan para recentrar el origen) para que principal.gd pueda
# proyectar la nariz del avión sobre ellos en vez de sobre los ejes crudos.
var este_motor_actual: Vector3 = Vector3.RIGHT
var norte_motor_actual: Vector3 = Vector3.FORWARD
var arriba_motor_actual: Vector3 = Vector3.UP

# Aeropuertos que el propio USUARIO va marcando en pleno vuelo con "Marcar
# lugar" (pedido 2026-09-21) -- pistas reales (a veces de tierra, difíciles
# de ver) que él encuentra volando y quiere dejar señaladas para la próxima.
# Lista APARTE de nodos_aeropuertos/nodos_misiones a propósito ("que no se
# mezclen" con las marcas de helicóptero) -- mismo cartel/pista que los
# aeropuertos de siempre, pero ORIENTADOS según el rumbo que llevaba
# el avión al aterrizar ahí (la mejor aproximación posible a la orientación
# real de la pista sin tener el dato exacto).
var nodos_aeropuertos_usuario: Array = []

func _ready() -> void:
	randomize()
	# Duplicamos el material del cielo antes de tocarlo por código -- así lo
	# que mutamos en tiempo real (colores día/noche) queda aislado a esta
	# partida, sin arriesgarse a pisar el recurso original de la escena.
	if entorno_mundo and entorno_mundo.environment and entorno_mundo.environment.sky:
		entorno_mundo.environment.sky.sky_material = entorno_mundo.environment.sky.sky_material.duplicate()
	# Lo ponemos por código (no solo en el archivo de la escena) para
	# asegurarnos de que se aplique bien, en un orden controlado, DESPUÉS
	# de que el nodo ya se haya inicializado solo.
	georeferencia.origin_type = 0  # Cartographic Origin
	georeferencia.latitude = -34.5589
	georeferencia.longitude = -58.4164
	# IMPORTANTE: 5.0 (altura real aproximada de Aeroparque) resultó estar MUY
	# por debajo de donde el terreno 3D real de Cesium realmente aparece ahí
	# -- el avión arrancaba enterrado ~70-80m bajo tierra (confirmado por el
	# usuario: "ALTITUD: -66m" apenas arrancaba). Subimos el valor como red de
	# seguridad para el primer cuadro (antes de que las baldosas terminen de
	# cargar); el piso de verdad después lo corrige el rayo en _limitar_piso()
	# de principal.gd, que usa la colisión real del terreno.
	georeferencia.altitude = 50.0
	print("🌍 Origen configurado -- lat: %s, lon: %s, ecef: (%s, %s, %s)" % [
		georeferencia.get_latitude(), georeferencia.get_longitude(),
		georeferencia.get_ecef_x(), georeferencia.get_ecef_y(), georeferencia.get_ecef_z()])

	for datos in aeropuertos_lla:
		var nodo = _generar_aeropuerto(datos["nombre"])
		_actualizar_transform_aeropuerto(nodo, datos["lat"], datos["lon"])
		if datos.has("orientacion"):
			nodo.set_meta("orientacion_usuario", datos["orientacion"])
			_orientar_aeropuerto_usuario(nodo)
			_generar_gates_ils(nodo)
		nodos_aeropuertos.append(nodo)

	# Aeropuertos de todo el país (ver declaración de aeropuertos_pais_lla) --
	# van a la MISMA lista nodos_aeropuertos que los de Buenos Aires (misma
	# forma de reubicarlos al recentrar el origen, más abajo).
	for datos_pais in aeropuertos_pais_lla:
		var nodo_pais = _generar_aeropuerto(datos_pais["nombre"])
		_actualizar_transform_aeropuerto(nodo_pais, datos_pais["lat"], datos_pais["lon"])
		if datos_pais.has("orientacion"):
			nodo_pais.set_meta("orientacion_usuario", datos_pais["orientacion"])
			_orientar_aeropuerto_usuario(nodo_pais)
			_generar_gates_ils(nodo_pais)
		nodos_aeropuertos.append(nodo_pais)

	for datos_mision in misiones:
		var nodo_mision = _generar_marcador_mision(datos_mision)
		_actualizar_transform_aeropuerto(nodo_mision, datos_mision["lat"], datos_mision["lon"])
		nodos_misiones.append(nodo_mision)

	for datos_dos_cabeceras in aeropuertos_dos_cabeceras:
		_generar_ils_dos_cabeceras(datos_dos_cabeceras)
		_generar_luces_pista(datos_dos_cabeceras)
		_generar_faro_aeropuerto(datos_dos_cabeceras)

	for datos_pueblo in pueblos_lla:
		var nodo_pueblo = _generar_marcador_pueblo(datos_pueblo["nombre"])
		_actualizar_transform_aeropuerto(nodo_pueblo, datos_pueblo["lat"], datos_pueblo["lon"])
		nodos_pueblos.append(nodo_pueblo)

	# Aeropuertos que el usuario ya había marcado en sesiones anteriores
	# (guardados por principal.gd en lugares_marcados.json) -- los que digan
	# "aeropuerto" en el nombre y tengan orientación guardada se recrean acá
	# como aeropuertos de usuario de verdad, visibles desde el arranque.
	_cargar_aeropuertos_usuario_desde_archivo()

	if georeferencia_mapa:
		georeferencia_mapa.origin_type = 0
		georeferencia_mapa.latitude = georeferencia.latitude
		georeferencia_mapa.longitude = georeferencia.longitude
		georeferencia_mapa.altitude = georeferencia.altitude

	# El overlay de calles se ve BLANCO LISO (terreno sin textura) si se
	# declara como hijo fijo en la escena -- sospecha: en Godot los nodos
	# hijos entran al árbol (_ready) ANTES que el padre, y si el overlay
	# intenta engancharse al Cesium3DTileset apenas nace, el tileset todavía
	# no terminó de inicializarse del lado de C++. Por eso lo creamos por
	# código, unos cuadros después de que el tileset ya lleva un rato vivo.
	if tileset_mapa:
		await get_tree().process_frame
		await get_tree().process_frame
		var overlay_calles := CesiumIonRasterOverlay.new()
		overlay_calles.name = "CesiumIonRasterOverlayCalles"
		overlay_calles.asset_id = 3830184
		tileset_mapa.add_child(overlay_calles)
		print("🗺️ Overlay de calles agregado por código al tileset del mapa")

# El tileset de Cesium necesita que le avisemos, cuadro a cuadro, dónde está
# la cámara -- si no, no sabe qué baldosas cargar y no muestra nada (esto lo
# hacía solo el script de la cámara de paseo del plugin; nuestra cámara
# propia no lo hace sola, hay que pedírselo a mano).
func _process(delta: float) -> void:
	# ESTO ES LO IMPORTANTE -- leyendo el código fuente real del plugin
	# (Cesium3DTileset::update_tileset), encontré que cuando el tileset está
	# georreferenciado (nuestro caso), IGNORA POR COMPLETO la posición que le
	# pasamos acá -- usa siempre `georeferencia->get_ecef_position()` (o sea,
	# el ORIGEN) como si fuera la posición del que mira. Por eso mover el
	# avión no cambiaba nada visualmente: para el tileset, la cámara nunca
	# se movía de Aeroparque. La única forma de que "sepa" que nos movimos
	# es mover el ORIGEN mismo -- por eso ahora recentramos TODOS los
	# cuadros (no cada 2 km), como hace el script de ejemplo del plugin
	# (que jamás mueve una cámara común, mueve el origen directamente).
	_recentrar_origen_en_avion()
	_actualizar_ciclo_dia_noche(delta)
	_actualizar_beacons(delta)
	_actualizar_ils_dos_cabeceras_frame()
	_actualizar_luces_pista_frame()
	_actualizar_faros_frame()
	if tileset and camara_juego:
		var camara_xform_ecef = georeferencia.get_tx_engine_to_ecef() * camara_juego.global_transform
		tileset.update_tileset(camara_xform_ecef)
	# El mapa de calles ahora tiene su PROPIA georreferencia (georeferencia_mapa),
	# aislada en su propio World3D -- por eso usa su propia conversión
	# engine->ecef, no la de la georreferencia principal.
	if tileset_mapa and georeferencia_mapa and camara_mapa:
		var mapa_xform_ecef = georeferencia_mapa.get_tx_engine_to_ecef() * camara_mapa.global_transform
		tileset_mapa.update_tileset(mapa_xform_ecef)

func _recentrar_origen_en_avion() -> void:
	# IMPORTANTE (precision): el ECEF tiene magnitud ~6.37 millones de metros.
	# Un Vector3 de Godot guarda sus componentes en float de 32 bits (~7
	# digitos significativos) -- si empaquetamos el ECEF ahi y le sumamos el
	# delta chico del avion CON Vector3, el redondeo se come casi todo el
	# delta, y como esto se repite CADA CUADRO y el resultado se reescribe
	# como el origen nuevo, el error se acumula sin parar (esto es lo que
	# causaba el "drift" infinito de latitud/altitud que se veia en el log).
	# Por eso acá la suma se hace con "float" sueltos de GDScript, que SI son
	# doubles de 64 bits -- igual que ya se hace en _lat_lon_alt_a_ecef_xyz/
	# _posicion_desde_lat_lon para los aeropuertos.
	var ox: float = georeferencia.get_ecef_x()
	var oy: float = georeferencia.get_ecef_y()
	var oz: float = georeferencia.get_ecef_z()

	# IMPORTANTE (geodesia -- BUG ENCONTRADO ESTA RONDA): la versión anterior
	# trataba las coordenadas X/Y/Z CRUDAS del motor como si YA fueran
	# "metros al Este/Arriba/Sur" -- eso solo es válido si los ejes del motor
	# coinciden con esas direcciones reales, y NO coinciden (el eje Y del
	# motor está a ~34.5° de la vertical real en Buenos Aires). Como el avión
	# está alineado con la vertical real (ver _alinear_con_vertical_real en
	# principal.gd), avanzar "derecho" mueve al avión en una dirección que,
	# vista en crudo desde los ejes del MOTOR, sí tiene una componente en Y
	# -- y la versión anterior contaba esa componente como altura real
	# ganada, por eso la altitud subía siempre sin importar el cabeceo.
	# FIX: en vez de usar las coordenadas crudas del motor, proyectamos el
	# movimiento sobre la vertical/este/norte REALES -- pero expresadas EN
	# ESPACIO DEL MOTOR (no en ECEF), usando la misma rotación fija que ya
	# usa get_normal_at_surface_pos() internamente para pasar de ECEF a
	# motor. Así "cuánto subió de verdad" sale de un producto punto contra
	# la vertical real, no de la coordenada Y cruda.
	var lla_origen = _ecef_a_lat_lon_alt(ox, oy, oz)
	var lat_rad: float = deg_to_rad(lla_origen[0])
	var lon_rad: float = deg_to_rad(lla_origen[1])
	var sin_lat := sin(lat_rad)
	var cos_lat := cos(lat_rad)
	var sin_lon := sin(lon_rad)
	var cos_lon := cos(lon_rad)

	var up_ecef := Vector3(cos_lat * cos_lon, cos_lat * sin_lon, sin_lat)
	var east_ecef := Vector3(-sin_lon, cos_lon, 0.0)
	var north_ecef := up_ecef.cross(east_ecef)  # = (-sin_lat*cos_lon, -sin_lat*sin_lon, cos_lat)

	# Las mismas 3 direcciones (Este/Arriba/Norte), pero convertidas a espacio
	# del motor con la rotación fija del propio georeference (la misma que
	# usa get_normal_at_surface_pos() para devolver "arriba" en espacio motor).
	var basis_ecef_a_motor: Basis = georeferencia.get_tx_ecef_to_engine().basis
	var este_motor: Vector3 = basis_ecef_a_motor * east_ecef
	var arriba_motor: Vector3 = basis_ecef_a_motor * up_ecef
	var norte_motor: Vector3 = basis_ecef_a_motor * north_ecef

	# Publicados para que principal.gd calcule el RUMBO real (ver comentario
	# junto a la declaración de estas variables, más arriba en este archivo).
	este_motor_actual = este_motor
	norte_motor_actual = norte_motor
	arriba_motor_actual = arriba_motor

	var pos: Vector3 = avion.global_position  # coordenadas CRUDAS del motor

	# Proyectamos el movimiento real (pos) sobre las 3 direcciones reales
	# (en espacio motor) para saber cuánto avanzó de verdad al Este/Arriba/Norte.
	var cuanto_este: float = pos.dot(este_motor)
	var cuanto_arriba: float = pos.dot(arriba_motor)
	var cuanto_norte: float = pos.dot(norte_motor)

	var dx: float = east_ecef.x * cuanto_este + up_ecef.x * cuanto_arriba + north_ecef.x * cuanto_norte
	var dy: float = east_ecef.y * cuanto_este + up_ecef.y * cuanto_arriba + north_ecef.y * cuanto_norte
	var dz: float = east_ecef.z * cuanto_este + up_ecef.z * cuanto_arriba + north_ecef.z * cuanto_norte

	var true_x: float = ox + dx
	var true_y: float = oy + dy
	var true_z: float = oz + dz

	var lla = _ecef_a_lat_lon_alt(true_x, true_y, true_z)
	altitud_avion = lla[2]
	lat_avion = lla[0]
	lon_avion = lla[1]

	# Escribir directo en ecefX/ecefY/ecefZ (no latitude/longitude/altitude)
	# SI dispara move_origin(), que reposiciona correctamente todas las
	# baldosas ya existentes -- ver comentario en _process().
	georeferencia.ecefX = true_x
	georeferencia.ecefY = true_y
	georeferencia.ecefZ = true_z

	# La georreferencia del mapa de calles vive en su propio mundo aislado
	# (own_world_3d), pero tiene que seguir al avión igual que la principal --
	# si no, el panel del mapa se queda mirando siempre Aeroparque.
	if georeferencia_mapa:
		georeferencia_mapa.ecefX = true_x
		georeferencia_mapa.ecefY = true_y
		georeferencia_mapa.ecefZ = true_z

	# PROBADO Y DESCARTADO: pisar georeferencia.transform.basis cada cuadro
	# (para "alinear" la rotación del nodo con la vertical real) rompe todo
	# -- las baldosas ya cargadas por el plugin quedan con la rotación VIEJA
	# mientras las nuevas usan la rotación nueva, y como el sistema interno
	# de tiles del plugin solo sabe corregir POSICIÓN (move_origin), no
	# ROTACIÓN del nodo padre, el resultado es terreno superpuesto/roto en
	# capas (confirmado en captura -- NO volver a intentar esto).

	# El origen absorbe TODO el movimiento del avión, incluida la altura --
	# igual que hace la cámara de ejemplo del propio plugin (camera_walk_ecef,
	# que nunca mueve la cámara: solo mueve el origen y la deja siempre
	# pegada al (0,0,0) local). Por eso el altímetro/piso/zoom de cámara ya
	# NO pueden usar position.y (ver altitud_avion arriba, usada por
	# principal.gd en su lugar).
	var offset = avion.global_position
	avion.global_position = Vector3.ZERO
	if camara_juego:
		camara_juego.global_position -= offset

	# Los aeropuertos ya puestos quedaron calculados para el origen VIEJO --
	# los volvemos a ubicar con el origen nuevo (sin destruirlos, para no
	# invalidar las referencias que ya tiene guardadas el script del avión).
	# LIMPIEZA 2026-09-20: antes esto leía lat/lon de un array PARALELO
	# (aeropuertos_lla[i]), que dependía de que el orden de creación y el
	# orden del array coincidieran siempre -- frágil (agregar una lista nueva
	# de aeropuertos, como la de todo el país, podía desalinear los índices
	# sin que se notara hasta romper la navegación). Cada nodo YA tiene su
	# propio lat/lon guardado como metadata (ver _actualizar_transform_aeropuerto)
	# -- leerlo directo del nodo es imposible de desalinear.
	for nodo in nodos_aeropuertos:
		_actualizar_transform_aeropuerto(nodo, nodo.get_meta("lat"), nodo.get_meta("lon"))
		if nodo.has_meta("orientacion_usuario"):
			_orientar_aeropuerto_usuario(nodo)

	# BUG REAL encontrado 2026-09-20 (el de las flechas de las misiones
	# apuntando cada vez peor cuanto más lejos volabas): esto SOLO reubicaba
	# los aeropuertos -- los marcadores de misión (nodos_misiones) se
	# quedaban con su posición vieja para siempre, calculada para el origen
	# de hace rato. Cuanto más volabas (más veces se recentró el origen sin
	# que estos se actualizaran), más desfasada quedaba su posición real --
	# por eso cerca de Aeroparque el rumbo salía bien y lejos se rompía todo.
	for nodo_mision in nodos_misiones:
		_actualizar_transform_aeropuerto(nodo_mision, nodo_mision.get_meta("lat"), nodo_mision.get_meta("lon"))

	# Mismo motivo -- los carteles de pueblos también necesitan reubicarse.
	for nodo_pueblo in nodos_pueblos:
		_actualizar_transform_aeropuerto(nodo_pueblo, nodo_pueblo.get_meta("lat"), nodo_pueblo.get_meta("lon"))

	# Los aeropuertos marcados por el usuario también necesitan reubicarse
	# Y reorientarse (la orientación se pierde cada vez que
	# _actualizar_transform_aeropuerto resetea el transform a Basis.IDENTITY).
	for nodo_usuario in nodos_aeropuertos_usuario:
		_actualizar_transform_aeropuerto(nodo_usuario, nodo_usuario.get_meta("lat"), nodo_usuario.get_meta("lon"))
		_orientar_aeropuerto_usuario(nodo_usuario)

	print("🔄 Origen re-centrado -- lat: %.5f, lon: %.5f, alt: %.1f" % [lla[0], lla[1], lla[2]])

# Mueve el sol a lo largo del día y ajusta su color/intensidad + los colores
# del cielo procedural en base a "hora_del_dia" (0.0 a 24.0). Se llama todos
# los cuadros, DESPUÉS de _recentrar_origen_en_avion() (necesita
# arriba_motor_actual/este_motor_actual ya frescos de este cuadro).
#
# Geometría: "theta" mide el ángulo del sol arrancando derecho hacia ARRIBA
# (0°) y rotando hacia el ESTE (90°) -- con eso: mediodía (hora 12) = sol
# arriba de todo (theta 0°), amanecer (hora 6) = sol en el horizonte al este
# (theta 90°), atardecer (hora 18) = sol en el horizonte al oeste (theta
# -90°), medianoche (hora 0/24) = sol abajo de todo, del otro lado del mundo
# (theta 180°). De ahí sale la fórmula theta = (12 - hora) * 15° (15°/hora,
# 360° en 24hs). La "elevación" sobre el horizonte es 90° menos el ángulo
# absoluto desde arriba (90 - |theta|): positiva de día, negativa de noche.
#
# MISMA REGLA DE ORO que ya rompió otros bugs en este proyecto (curvatura de
# la Tierra): la dirección del sol se arma sobre arriba_motor_actual/
# este_motor_actual (la vertical/horizontal REAL), nunca sobre Vector3.UP
# crudo -- si no, en Buenos Aires el sol saldría ~34.5° inclinado de más.
func _actualizar_ciclo_dia_noche(delta: float) -> void:
	if avance_automatico_hora:
		hora_del_dia = fposmod(hora_del_dia + (delta / SEGUNDOS_POR_DIA_COMPLETO) * 24.0, 24.0)

	var theta_grados: float = (12.0 - hora_del_dia) * 15.0
	var theta_rad: float = deg_to_rad(theta_grados)
	var direccion_al_sol: Vector3 = (cos(theta_rad) * arriba_motor_actual + sin(theta_rad) * este_motor_actual).normalized()
	# look_at apunta el eje -Z local hacia el punto dado -- como la luz brilla
	# hacia -Z, mirando hacia "posición menos dirección_al_sol" el rayo de luz
	# termina viajando en la dirección CONTRARIA al sol (del sol hacia el
	# suelo), que es justo lo que tiene que hacer.
	if luz_sol:
		luz_sol.look_at(luz_sol.global_position - direccion_al_sol, arriba_motor_actual)

	var elevacion_grados: float = 90.0 - abs(theta_grados)
	var t_dia: float = clamp((elevacion_grados + 6.0) / 26.0, 0.0, 1.0)
	factor_noche_actual = 1.0 - t_dia

	if luz_sol:
		luz_sol.light_energy = lerp(0.05, 1.0, clamp(elevacion_grados / 60.0, 0.0, 1.0))
		luz_sol.light_color = COLOR_LUZ_AMANECER.lerp(COLOR_LUZ_DIA, t_dia)

	if entorno_mundo and entorno_mundo.environment:
		var env := entorno_mundo.environment
		if env.sky:
			var mat_cielo := env.sky.sky_material
			if mat_cielo is ProceduralSkyMaterial:
				mat_cielo.sky_top_color = COLOR_CIELO_NOCHE_ARRIBA.lerp(COLOR_CIELO_DIA_ARRIBA, t_dia)
				mat_cielo.sky_horizon_color = COLOR_CIELO_NOCHE_HORIZONTE.lerp(COLOR_CIELO_DIA_HORIZONTE, t_dia)
				mat_cielo.ground_bottom_color = COLOR_SUELO_NOCHE.lerp(COLOR_SUELO_DIA, t_dia)
				mat_cielo.ground_horizon_color = mat_cielo.sky_horizon_color
		env.ambient_light_source = Environment.AMBIENT_SOURCE_SKY
		env.ambient_light_energy = lerp(0.12, 1.0, t_dia)

		# BUG REAL, causa de fondo encontrada 2026-09-25 (reportado en vivo:
		# "el avión se oscurece de noche pero el terreno de Cesium queda
		# igual de claro, ni el cielo cambia"), confirmada con ayuda de
		# Gemini: las baldosas de Google Photorealistic 3D Tiles vienen con
		# la extensión glTF KHR_materials_unlit -- la iluminación ya está
		# "horneada" en la foto satelital, así que el plugin de Cesium las
		# dibuja SIN responder a ninguna luz de Godot (ni DirectionalLight3D
		# ni ambient_light_energy les hace nada, por diseño). Por eso ARRIBA
		# el avión (con material normal, sí sensible a la luz) se oscurecía
		# bien pero el terreno no se movía un pixel.
		# SOLUCIÓN (la misma que recomendó Gemini): bajar la EXPOSICIÓN
		# global de la escena (tonemap_exposure), que es un multiplicador
		# aplicado al frame entero ya renderizado -- afecta a TODO lo que se
		# ve en pantalla por igual, sea "lit" o "unlit", porque no es parte
		# del cálculo de luces por superficie sino del paso final de
		# tonemapping. Esto es lo que de verdad oscurece el terreno de noche.
		env.tonemap_exposure = lerp(0.04, 1.0, t_dia)

		# Pedido explícito 2026-09-25 ("se ve como las 7 de la tarde, no como
		# noche cerrada... GeoFS de noche lo pasa casi a blanco y negro"):
		# bajar SOLO la exposición no alcanza para que se sienta "noche" de
		# verdad. Godot tiene un ajuste de color de pantalla completa hecho
		# para esto (Environment.adjustment_*, activado una sola vez acá) --
		# bajamos la SATURACIÓN hacia casi cero de noche (el ojo humano ve
		# poco color con poca luz, el mismo truco que notó el usuario en
		# GeoFS), sumado a la exposición ya más oscura de arriba.
		if not env.adjustment_enabled:
			env.adjustment_enabled = true
		env.adjustment_saturation = lerp(0.15, 1.0, t_dia)

# Llamado desde el panel de Configuración (botones -/+ de "Hora del día") --
# +delta_horas para adelantar, negativo para atrasar, con vuelta redonda a
# las 24hs.
func ajustar_hora_del_dia(delta_horas: float) -> void:
	hora_del_dia = fposmod(hora_del_dia + delta_horas, 24.0)

func alternar_avance_automatico_hora(activo: bool) -> void:
	avance_automatico_hora = activo

# Convierte ECEF a latitud/longitud/altitud (el camino inverso de
# _lat_lon_alt_a_ecef_xyz), con la misma fórmula estándar que usa el propio
# plugin en su código C++ (Ferrari, para no iterar con Newton).
func _ecef_a_lat_lon_alt(x: float, y: float, z: float) -> Array:
	var a = WGS84_A
	var f = WGS84_F
	var b = a * (1.0 - f)
	var e2 = (a * a - b * b) / (a * a)
	var ep2 = (a * a - b * b) / (b * b)
	var p = sqrt(x * x + y * y)
	var th = atan2(a * z, b * p)
	var lon = atan2(y, x)
	var lat = atan2(z + ep2 * b * pow(sin(th), 3), p - e2 * a * pow(cos(th), 3))
	var n = a / sqrt(1.0 - e2 * sin(lat) * sin(lat))
	var alt = p / cos(lat) - n
	return [rad_to_deg(lat), rad_to_deg(lon), alt]

# Convierte lat/lon/altura real a coordenadas ECEF (el sistema que usa
# Cesium por adentro), con la fórmula geodésica estándar (WGS84). La versión
# del plugin que tenemos compilada (v1.0.1) no trae una función lista para
# esto, así que la hacemos acá -- es la misma cuenta que usa cualquier GPS.
# Devuelve las 3 componentes SUELTAS (no un Vector3 todavía) porque son
# números de millones de metros -- un Vector3 solo tiene precisión de 32
# bits, y si empaquetamos ahí ANTES de restar el origen, perdemos toda la
# precisión útil (eso pasó en el primer intento: daba resultados gigantes
# en vez de la distancia chica que corresponde).
const WGS84_A = 6378137.0                 # radio ecuatorial de la Tierra (metros)
const WGS84_F = 1.0 / 298.257223563       # achatamiento de la Tierra
func _lat_lon_alt_a_ecef_xyz(lat_deg: float, lon_deg: float, alt_m: float) -> Array:
	var lat = deg_to_rad(lat_deg)
	var lon = deg_to_rad(lon_deg)
	var e2 = WGS84_F * (2.0 - WGS84_F)
	var n = WGS84_A / sqrt(1.0 - e2 * sin(lat) * sin(lat))
	var x = (n + alt_m) * cos(lat) * cos(lon)
	var y = (n + alt_m) * cos(lat) * sin(lon)
	var z = (n * (1.0 - e2) + alt_m) * sin(lat)
	return [x, y, z]

# Convierte una coordenada real (latitud/longitud/altitud) a la posición
# local del motor: la diferencia (en ECEF, con números sueltos de precisión
# completa) entre ese punto y el ORIGEN del CesiumGeoreference, rotada con
# la matriz del propio plugin (para no adivinar para qué lado quedan los
# ejes X/Z locales).
func _posicion_desde_lat_lon(lat_deg: float, lon_deg: float, alt_m: float) -> Vector3:
	# BUG REAL encontrado 2026-09-20 (confirmado con Gemini): get_latitude()/
	# get_longitude()/get_altitude() del CesiumGeoreference quedan PEGADOS en
	# el valor de cuando el nodo estuvo en modo Cartographic (el origen
	# INICIAL, Aeroparque) -- no se actualizan solos cuando movemos el origen
	# a mano vía ecefX/ecefY/ecefZ cada cuadro (nuestro "origen flotante").
	# Antes esta función mezclaba un origen VIEJO (de esos getters) con una
	# rotación get_tx_ecef_to_engine() que SÍ está al día -- la combinación
	# rompía la geometría y desviaba el rumbo cada vez más cuanto más lejos
	# volabas del punto de partida. Usar directo get_ecef_x/y/z (esos SÍ
	# están sincronizados, son los mismos valores que nosotros seteamos) es
	# la corrección.
	var p = _lat_lon_alt_a_ecef_xyz(lat_deg, lon_deg, alt_m)
	var delta = Vector3(
		p[0] - georeferencia.get_ecef_x(),
		p[1] - georeferencia.get_ecef_y(),
		p[2] - georeferencia.get_ecef_z())
	var local_pos: Vector3 = georeferencia.get_tx_ecef_to_engine().basis * delta
	return local_pos

# El avión llama a esto para saber, en orden, a dónde tiene que ir.
func obtener_destinos() -> Array:
	return nodos_aeropuertos

# Calcula la posición Y la orientación reales (según el terreno curvo en ese
# punto exacto, usando eus_at_ecef) y se las aplica al nodo YA EXISTENTE --
# se usa tanto al crear el aeropuerto por primera vez como al recentrar el
# origen (ahí solo hay que recalcular, no destruir y crear de nuevo).
func _actualizar_transform_aeropuerto(nodo: Node3D, lat_deg: float, lon_deg: float) -> void:
	# Guardamos lat/lon reales como metadata -- principal.gd los necesita para
	# calcular el RUMBO VERDADERO hacia el lugar (ver bug de las flechas, más
	# abajo en este archivo).
	nodo.set_meta("lat", lat_deg)
	nodo.set_meta("lon", lon_deg)
	var pos = _posicion_desde_lat_lon(lat_deg, lon_deg, 5.0)
	# OJO: el ajuste de orientación con eus_at_ecef que había puesto acá
	# (para que la pista no pareciera "un palo" lejos del origen) resultó
	# estar mal calculado -- volvió la pista un cuadrilátero gigante y
	# desorientado. Con el origen flotante ya recentrando seguido, la pista
	# del aeropuerto ACTUAL siempre va a estar cerca del origen, así que ni
	# hace falta ese ajuste fino: la dejamos plana (sin rotación extra).
	nodo.transform = Transform3D(Basis.IDENTITY, pos)

# Aeropuertos con ILS de dos cabeceras REALES (ver aeropuertos_dos_cabeceras
# más arriba): ya tienen el asfalto de verdad (terreno fotorrealista) más el
# corredor de aros preciso -- la pista/línea de juguete de _generar_aeropuerto
# (un rectángulo genérico de 150m, sin rotar) queda desalineada de verdad y
# encima de más, así que se saca directamente (pedido explícito 2026-09-21,
# "el palo negro gigante en El Palomar no sirve para nada").
func _generar_aeropuerto(nombre: String, longitud_pista: float = 150.0) -> Node3D:
	var raiz = Node3D.new()
	raiz.name = nombre.replace(" ", "").replace("ó", "o")
	raiz.set_meta("nombre_bonito", nombre)
	raiz.set_meta("longitud_pista", longitud_pista)
	add_child(raiz)

	# SACADA 2026-09-21 (pedido explícito, "sacá todas las pistas negras de
	# juguete, ninguna cumple una función y quedan mal a la vista, hasta la
	# de Don Torcuato está torcida"): antes acá se dibujaba un rectángulo
	# gris genérico de 150m (sin orientación real salvo en los 4 aeropuertos
	# con ILS de dos cabeceras) simulando una pista -- con el terreno
	# fotorrealista real ya mostrando el asfalto de verdad, esa pista de
	# juguete no aportaba nada y en la mayoría de los casos ni siquiera
	# quedaba alineada. El cartel/beacon con el nombre se queda igual, solo
	# se sacó el rectángulo y la línea blanca del medio.

	# Cartel discreto de cerca (queda igual que antes, para cuando estás
	# realmente sobre la pista -- no lo tocamos).
	var etiqueta = Label3D.new()
	etiqueta.text = nombre.to_upper()
	etiqueta.billboard = BaseMaterial3D.BILLBOARD_ENABLED
	etiqueta.pixel_size = 0.035
	etiqueta.font_size = 110
	etiqueta.outline_size = 14
	etiqueta.position = Vector3(-28, 14, -75)
	raiz.add_child(etiqueta)

	var mat_poste = StandardMaterial3D.new()
	mat_poste.albedo_color = Color(0.9, 0.15, 0.15)
	mat_poste.emission_enabled = true
	mat_poste.emission = Color(0.9, 0.15, 0.15)
	mat_poste.emission_energy_multiplier = 0.25
	var poste = MeshInstance3D.new()
	var malla_poste = CylinderMesh.new()
	malla_poste.top_radius = 0.15
	malla_poste.bottom_radius = 0.15
	malla_poste.height = 6.0
	poste.mesh = malla_poste
	poste.set_surface_override_material(0, mat_poste)
	poste.position = Vector3(-28, 3, -75)
	raiz.add_child(poste)

	# BEACON tipo neón, visible desde lejos y en altura -- pedido explícito
	# (antes no había forma de ubicar un aeropuerto a lo lejos, y guiarse
	# solo por "rumbo objetivo" no sirve porque cambia de golpe al pasar
	# waypoints intermedios). Bien arriba (300m, por encima de casi
	# cualquier edificio real) y GIGANTE para que se lea desde varios km.
	# `no_depth_test = true` -- lo clave: se dibuja SIEMPRE arriba de todo
	# (terreno, edificios), como una baliza real, en vez de taparse detrás
	# del primer edificio que se cruce en el medio.
	# CRITERIO DE OCULTAMIENTO corregido 2026-09-26 (pedido explícito: "me
	# sirve verlo mientras voy en el aire, para orientación -- hay que
	# sacarlo cuando uno está a baja altura", NO es un tema de distancia
	# horizontal como se venía probando). Ver _actualizar_beacons(): ahora
	# se desvanece según la ALTITUD del avión, no la distancia al
	# aeropuerto -- volando alto ayuda a orientarse, bajo (aterrizando)
	# molesta y se apaga.
	var beacon = Label3D.new()
	beacon.name = "Beacon"
	beacon.text = nombre.to_upper()
	beacon.billboard = BaseMaterial3D.BILLBOARD_ENABLED
	beacon.no_depth_test = true
	beacon.pixel_size = 1.4
	beacon.font_size = 130
	beacon.outline_size = 22
	beacon.modulate = Color(1.0, 0.82, 0.25, 1.0)
	beacon.outline_modulate = Color(0.25, 0.15, 0.0, 1.0)
	beacon.position = Vector3(0, 300, 0)
	# BUG REAL encontrado 2026-09-26 (reportado: "se ve poco iluminada desde
	# el aire, cuesta saber dónde está cada aeropuerto"): este cartel se
	# quedó en la capa 1 de siempre, así que el post-proceso de noche
	# (tonemap_exposure/adjustment_saturation) lo apaga y desatura igual que
	# al terreno en cualquier horario que no sea pleno mediodía -- nunca se
	# lo pasó a la capa de marcadores (CAPA_MARCADORES_NOCTURNOS) como sí se
	# hizo con los aros de ILS/luces de pista/faro. Con esto vuelve a verse
	# siempre a full brillo/color, como antes de que existiera el ciclo
	# día/noche.
	beacon.set_layer_mask_value(1, false)
	beacon.set_layer_mask_value(CAPA_MARCADORES_NOCTURNOS, true)
	raiz.add_child(beacon)
	beacons_para_animar.append(beacon)

	# PROBADO Y DESCARTADO (pedido explícito): el mástil que conectaba el
	# beacon con la pista -- el usuario prefiere solo el cartel, sin palito.

	return raiz

# Ver comentario en el beacon de _generar_aeropuerto: lejos parpadea suave
# (llama la atención), cerca se apaga del todo (para no tapar la vista con
# letras gigantes) y deja el protagonismo al cartel chico de siempre.
# Como el origen SIEMPRE está recentrado en el avión (ver
# _recentrar_origen_en_avion), la distancia al avión es simplemente el
# largo del vector de posición global de cada beacon -- no hace falta
# guardar ni pedir la posición del avión para nada.
var beacons_para_animar: Array = []
# CRITERIO CAMBIADO 2026-09-26 (pedido explícito: "me sirve verlo mientras
# voy en el aire, para orientación -- hay que sacarlo cuando uno está a
# baja altura", no es un tema de qué tan lejos está el aeropuerto). Antes
# se ocultaba por DISTANCIA horizontal al aeropuerto; ahora se oculta según
# la ALTITUD del avión sobre el terreno de ESE aeropuerto -- volando alto
# (crucero) ayuda a orientarse hacia varios aeropuertos a la vez, volando
# bajo (aproximación/aterrizaje) tapa la vista y se apaga.
const ALTURA_BEACON_OCULTO = 300.0
const ALTURA_BEACON_VISIBLE = 900.0
var _tiempo_beacons: float = 0.0
func _actualizar_beacons(delta: float) -> void:
	_tiempo_beacons += delta
	# Parpadeo lento (medio ciclo por segundo aprox.), nunca llega a apagarse
	# del todo -- oscila entre 55% y 100% de opacidad, para que se note sin
	# quedar tipo cartel de neón roto.
	var parpadeo: float = 0.775 + 0.225 * sin(_tiempo_beacons * 3.0)
	for beacon in beacons_para_animar:
		# Como el origen SIEMPRE está recentrado en el avión, la posición
		# del aeropuerto (su nodo padre, a nivel del piso) YA es el vector
		# aeropuerto->avión invertido -- la componente vertical de ese
		# vector, con el signo dado vuelta, es cuántos metros por ENCIMA
		# del piso de ese aeropuerto está el avión ahora mismo.
		var vector_al_aeropuerto: Vector3 = beacon.get_parent().global_position
		var altura_sobre_aeropuerto: float = -vector_al_aeropuerto.dot(arriba_motor_actual)
		var desvanecimiento: float = clamp(
			(altura_sobre_aeropuerto - ALTURA_BEACON_OCULTO) / (ALTURA_BEACON_VISIBLE - ALTURA_BEACON_OCULTO),
			0.0, 1.0)
		beacon.modulate.a = desvanecimiento * parpadeo

# Aeropuerto marcado por el usuario en pleno vuelo (pedido 2026-09-21) --
# mismo cartel/pista que _generar_aeropuerto, pero en su PROPIA lista (no se
# mezcla con nodos_aeropuertos ni con las misiones de helicóptero) y con la
# pista ORIENTADA según el rumbo que llevaba el avión al marcarlo (la mejor
# aproximación disponible a la orientación real, sin tener el dato exacto).
const RUTA_LUGARES_MARCADOS_MUNDO = "res://lugares_marcados.json"
func _cargar_aeropuertos_usuario_desde_archivo() -> void:
	if not FileAccess.file_exists(RUTA_LUGARES_MARCADOS_MUNDO):
		return
	var archivo = FileAccess.open(RUTA_LUGARES_MARCADOS_MUNDO, FileAccess.READ)
	var contenido = archivo.get_as_text()
	archivo.close()
	var lista = JSON.parse_string(contenido)
	if not (lista is Array):
		return
	for lugar in lista:
		if not (lugar is Dictionary):
			continue
		var nombre: String = lugar.get("nombre", "")
		if nombre.to_lower().find("aeropuerto") == -1:
			continue
		if not lugar.has("orientacion"):
			continue
		agregar_aeropuerto_usuario(nombre, lugar["lat"], lugar["lon"], lugar["orientacion"])

func agregar_aeropuerto_usuario(nombre: String, lat: float, lon: float, orientacion_grados: float) -> void:
	var nodo = _generar_aeropuerto(nombre)
	nodo.set_meta("orientacion_usuario", orientacion_grados)
	_actualizar_transform_aeropuerto(nodo, lat, lon)
	_orientar_aeropuerto_usuario(nodo)
	nodos_aeropuertos_usuario.append(nodo)

# Rota la pista/línea/beacon del aeropuerto de usuario para que apunte en la
# dirección real (compás) guardada -- usando el este/norte YA calculados en
# espacio motor (los mismos de siempre), no los ejes crudos.
func _orientar_aeropuerto_usuario(nodo: Node3D) -> void:
	var orientacion: float = nodo.get_meta("orientacion_usuario")
	var rumbo_rad: float = deg_to_rad(orientacion)
	var direccion: Vector3 = (norte_motor_actual * cos(rumbo_rad) + este_motor_actual * sin(rumbo_rad)).normalized()
	nodo.look_at(nodo.global_position + direccion, arriba_motor_actual)

# ILS visual, TERCER intento (2026-09-21) -- dos correcciones sobre lo
# reportado por el usuario probando El Palomar y Mariano Moreno:
#
# 1) "Los aros me dejan en la mitad de la pista": el arranque de los aros
#    estaba atado a la mitad de nuestro modelo de pista de JUGUETE (150m),
#    no a la pista real (El Palomar mide 2.110m de verdad). El usuario
#    aclaró después que no hace falta calcular el largo real de cada
#    pista -- alcanza con arrancar los aros bien lejos SIEMPRE ("si sobran
#    aros no pasa nada, los agarrás antes") -- así que ahora el arranque es
#    una distancia fija y generosa (DISTANCIA_INICIO_AROS), no la mitad de
#    nada. Igual usamos el largo real como bonus SOLO para el dibujo de la
#    pista en sí (ver "longitud_pista" en _generar_aeropuerto), no para los
#    aros.
#
# 2) "En Mariano Moreno me obligó a un giro imposible viniendo de
#    Aeroparque": antes solo había aros de UN lado de la pista (aterrizando
#    en la dirección de "orientacion" nomás). Ahora hay aros a los DOS
#    lados -- una pista real se puede aterrizar desde cualquier punta según
#    de dónde vengas. Al principio el juego elegía solo automáticamente qué
#    lado mostrar según por dónde venías volando, pero eso generaba dudas
#    ("a veces uno no sabe cómo está la pista") -- ahora los DOS lados
#    quedan siempre visibles a la vez (con colores distintos), y el jugador
#    elige con cuál alinearse (ver alternar_ils()).
# Pedido explícito 2026-09-22 ("agrandalos un poco" y "ponele más cantidad
# de aros... para que la visualización desde lejos se aprecie mejor"):
# más rings (casi el doble, con huecos más chicos entre cada uno) y más
# grandes -- esto es puramente visual/de referencia a distancia, no hace
# falta pasar exactamente por el medio de ninguno para aterrizar bien.
# Capa aparte para que el ILS, las luces de pista y el faro de aeropuerto
# mantengan SIEMPRE su color/brillo real, de día o de noche (pedido
# 2026-09-25, "el ILS se pone en blanco y negro también y se pierde") -- ver
# el comentario largo en camara_marcadores.gd. Todo lo de esta capa lo
# excluye la cámara principal y lo dibuja aparte una segunda cámara sin el
# post-proceso de noche.
const CAPA_MARCADORES_NOCTURNOS = 6
# Pedido explícito 2026-09-26 ("una vez que ya estás orientado en la pista
# ya no hace falta, es preferible que se vea bien la pista"): se sacan los 4
# aros más cercanos (650/450/300/150m) -- el corredor sigue guiando desde
# lejos, pero ya no tapa la pista ni el cartel al estar encima aterrizando.
const DISTANCIAS_GATES_ILS = [6000.0, 5250.0, 4500.0, 3750.0, 3000.0, 2500.0, 2200.0, 1850.0, 1500.0, 1150.0, 900.0]
const DISTANCIA_INICIO_AROS = 1200.0  # fijo y generoso, no depende del largo real de cada pista
const PENDIENTE_ILS = 0.0524  # tangente de 3°, la misma senda de descenso que usa un ILS real
const RADIO_INTERNO_GATE_ILS = 65.0  # agrandado (pedido 2026-09-22)
const RADIO_EXTERNO_GATE_ILS = 80.0
var contenedores_ils_por_aeropuerto: Array = []  # cada entrada: {"positivo": Node3D, "negativo": Node3D, "nodo": Node3D}
var ils_activo_global: bool = false

func _generar_gates_ils(nodo: Node3D) -> void:
	var orientacion: float = nodo.get_meta("orientacion_usuario")
	# "signo" +1 arma el corredor del lado +Z (para aterrizar en la dirección
	# de "orientacion", viniendo desde ese lado); -1 arma el espejo del otro
	# lado (para aterrizar en la dirección RECÍPROCA, viniendo del lado
	# opuesto). Mismo diseño, solo cambia el signo de la posición en Z.
	# Colores distintos por lado (pedido explícito, "para no marearte viendo
	# 500 aros, uno amarillo y otro verde flúor") -- como los dos lados están
	# visibles a la vez (ver alternar_ils), el color ayuda a reconocer de un
	# vistazo a qué pista/lado corresponde cada corredor.
	var contenedor_positivo = _generar_un_lado_de_ils(nodo, 1.0, orientacion, Color(1.0, 0.85, 0.1))
	var contenedor_negativo = _generar_un_lado_de_ils(nodo, -1.0, fposmod(orientacion + 180.0, 360.0), Color(0.25, 1.0, 0.25))
	contenedores_ils_por_aeropuerto.append({
		"positivo": contenedor_positivo,
		"negativo": contenedor_negativo,
		"nodo": nodo,
	})

func _generar_un_lado_de_ils(nodo: Node3D, signo: float, orientacion_de_este_lado: float, color: Color) -> Node3D:
	var contenedor = Node3D.new()
	contenedor.name = "GatesILS_%s" % ("Positivo" if signo > 0 else "Negativo")
	contenedor.visible = false
	nodo.add_child(contenedor)

	var mat_aro = StandardMaterial3D.new()
	mat_aro.albedo_color = Color(color.r, color.g, color.b, 0.85)
	mat_aro.transparency = BaseMaterial3D.TRANSPARENCY_ALPHA
	mat_aro.emission_enabled = true
	mat_aro.emission = color
	mat_aro.emission_energy_multiplier = 0.6

	for distancia in DISTANCIAS_GATES_ILS:
		var aro = MeshInstance3D.new()
		var malla_aro = TorusMesh.new()
		malla_aro.inner_radius = RADIO_INTERNO_GATE_ILS
		malla_aro.outer_radius = RADIO_EXTERNO_GATE_ILS
		aro.mesh = malla_aro
		aro.set_surface_override_material(0, mat_aro)
		aro.set_layer_mask_value(1, false)
		aro.set_layer_mask_value(CAPA_MARCADORES_NOCTURNOS, true)
		# El agujero del TorusMesh atraviesa su propio eje Y local -- para que
		# mire a lo largo de la pista (eje Z local del nodo padre, ya
		# orientado hacia el rumbo real) hace falta pararlo con este giro fijo.
		aro.rotation_degrees = Vector3(90, 0, 0)
		aro.position = Vector3(0, distancia * PENDIENTE_ILS, signo * (DISTANCIA_INICIO_AROS + distancia))
		contenedor.add_child(aro)

	# Cartelito con el número de pista real (pedido explícito) -- sale solo
	# del rumbo: el número de pista ES el rumbo ÷ 10 redondeado, la misma
	# convención real que usan todos los aeropuertos del mundo. Puesto en el
	# aro más lejano, el primero que se ve al entrar al corredor.
	var numero_pista: int = int(round(orientacion_de_este_lado / 10.0))
	if numero_pista <= 0:
		numero_pista += 36
	var etiqueta_pista = Label3D.new()
	etiqueta_pista.text = "PISTA %02d" % numero_pista
	etiqueta_pista.billboard = BaseMaterial3D.BILLBOARD_ENABLED
	etiqueta_pista.modulate = color
	etiqueta_pista.outline_modulate = Color(0, 0, 0, 1)
	etiqueta_pista.pixel_size = 0.25
	etiqueta_pista.font_size = 80
	etiqueta_pista.outline_size = 14
	var distancia_maxima: float = DISTANCIAS_GATES_ILS[0]
	etiqueta_pista.position = Vector3(0, distancia_maxima * PENDIENTE_ILS + 20.0, signo * (DISTANCIA_INICIO_AROS + distancia_maxima))
	contenedor.add_child(etiqueta_pista)

	return contenedor

# Prende/apaga TODOS los ILS de golpe -- un botón único y simple, en vez de
# uno por aeropuerto.
# VUELTA ATRÁS 2026-09-21 (pedido explícito, "para que no haya complicaciones,
# activá los dos, uno de un color y otro de otro, y elegís vos de qué lado
# meterte"): antes elegíamos un solo lado automático según de dónde venías --
# más "inteligente" pero más lío ("a veces uno no sabe cómo está la pista").
# Ahora los dos lados quedan SIEMPRE visibles juntos (colores distintos para
# no confundirlos), y el jugador elige con cuál alinearse.
# ILS de dos cabeceras reales medidas por el usuario -- ver comentario largo
# junto a aeropuertos_dos_cabeceras. A diferencia del ILS "de un rumbo"
# (_generar_gates_ils), acá no rotamos ningún nodo por compás -- se arma un
# solo corredor recto entre los dos puntos reales, extendido más allá de
# CADA punta (dos colores, las dos direcciones siempre activas). Como usa
# dos coordenadas lat/lon independientes (no hijos de un mismo nodo ya
# orientado), necesita recalcularse TODOS los cuadros -- mismo motivo que la
# línea guía y los aros de la licencia (el origen del mundo se recentra en
# el avión cada cuadro).
var contenedor_ils_dos_cabeceras: Array = []
const COLOR_CABECERA_1 = Color(1.0, 0.85, 0.1)   # amarillo
const COLOR_CABECERA_2 = Color(0.25, 1.0, 0.25)  # verde flúor

func _generar_ils_dos_cabeceras(datos: Dictionary) -> void:
	var contenedor = Node3D.new()
	contenedor.name = "ILS_%s" % datos["nombre"].replace(" ", "")
	contenedor.visible = false
	add_child(contenedor)

	for signo in [1.0, -1.0]:
		var color: Color = COLOR_CABECERA_1 if signo > 0 else COLOR_CABECERA_2
		var mat_aro = StandardMaterial3D.new()
		mat_aro.albedo_color = Color(color.r, color.g, color.b, 0.85)
		mat_aro.transparency = BaseMaterial3D.TRANSPARENCY_ALPHA
		mat_aro.emission_enabled = true
		mat_aro.emission = color
		mat_aro.emission_energy_multiplier = 0.6
		for distancia in DISTANCIAS_GATES_ILS:
			var aro = MeshInstance3D.new()
			var malla_aro = TorusMesh.new()
			malla_aro.inner_radius = RADIO_INTERNO_GATE_ILS
			malla_aro.outer_radius = RADIO_EXTERNO_GATE_ILS
			aro.mesh = malla_aro
			aro.set_surface_override_material(0, mat_aro)
			aro.set_layer_mask_value(1, false)
			aro.set_layer_mask_value(CAPA_MARCADORES_NOCTURNOS, true)
			# Guardamos la distancia real en METROS más allá de la cabecera
			# correspondiente (no una fracción -- eso se calcula cada cuadro
			# con el largo REAL medido entre las dos cabeceras, ver
			# _actualizar_ils_dos_cabeceras_frame, para que sea exacto sin
			# importar cuánto midan de verdad).
			aro.set_meta("distancia_extra", distancia)
			aro.set_meta("lado_cab1", signo > 0)
			aro.set_meta("altura", distancia * PENDIENTE_ILS)
			contenedor.add_child(aro)

	contenedor_ils_dos_cabeceras.append({
		"nombre": datos["nombre"],
		"cab1_lat": datos["cab1_lat"], "cab1_lon": datos["cab1_lon"], "cab1_alt": datos.get("cab1_alt", 8.0),
		"cab2_lat": datos["cab2_lat"], "cab2_lon": datos["cab2_lon"], "cab2_alt": datos.get("cab2_alt", 8.0),
		"contenedor": contenedor,
	})

func _actualizar_ils_dos_cabeceras_frame() -> void:
	# Pedido 2026-09-27 (panel "Torre", ILS individual por aeropuerto
	# cercano): antes este chequeo global cortaba TODO si el interruptor
	# maestro estaba apagado, así que un ILS prendido a mano en un solo
	# aeropuerto (con alternar_ils_aeropuerto) nunca se actualizaba de
	# posición mientras el global estuviera off. Ahora cada aro ya se salta
	# solo si SU contenedor puntual está apagado (la línea de abajo), así
	# que no hace falta el gate global acá.
	for entrada in contenedor_ils_dos_cabeceras:
		if not entrada["contenedor"].visible:
			continue
		var p1: Vector3 = _posicion_desde_lat_lon(entrada["cab1_lat"], entrada["cab1_lon"], entrada["cab1_alt"])
		var p2: Vector3 = _posicion_desde_lat_lon(entrada["cab2_lat"], entrada["cab2_lon"], entrada["cab2_alt"])
		var largo_real: float = p1.distance_to(p2)
		if largo_real < 1.0:
			continue
		# BUG REAL encontrado 2026-09-21 (el motivo de que NINGÚN aro de este
		# sistema se viera -- El Palomar, Morón, Ezeiza x2): acá solo se
		# actualizaba la POSICIÓN de cada aro, nunca su ORIENTACIÓN -- se
		# quedaban con la rotación de fábrica (el agujero del TorusMesh
		# mirando hacia arriba, acostado como una argolla en el piso) en vez
		# de pararse mirando a lo largo de la pista. De costado son casi
		# invisibles volando normal. Mismo arreglo que ya usamos en los aros
		# de la licencia de helicóptero (ver _actualizar_aros_licencia_frame
		# en principal.gd): basis.y = dirección del corredor, el torus es
		# simétrico rotando sobre ese eje así que cualquier par perpendicular
		# para los otros dos ejes sirve.
		var direccion: Vector3 = (p2 - p1).normalized()
		var referencia: Vector3 = Vector3.UP if abs(direccion.dot(Vector3.UP)) < 0.99 else Vector3.RIGHT
		var eje_x: Vector3 = direccion.cross(referencia).normalized()
		var eje_z: Vector3 = eje_x.cross(direccion).normalized()
		var base_aros := Basis(eje_x, direccion, eje_z)

		# SEGUNDO BUG REAL encontrado 2026-09-21 (el que encontró el propio
		# usuario mirando la altura en el debug: "primerAro" aparecía a
		# Y=5127 con el aeropuerto a un par de cientos de metros): para los
		# aros MÁS ALLÁ de las cabeceras usábamos lerp(p1, p2, fraccion) con
		# fraccion bien fuera de [0,1] (hasta -2.8 o 3.8, porque el corredor
		# de 6km es mucho más largo que la pista real de ~2km) -- eso estira
		# el vector CRUDO p2-p1, que no es perfectamente horizontal (la
		# inclinación de siempre entre los ejes del motor y la vertical real
		# en Buenos Aires), y estirarlo 3 veces amplifica esa inclinación
		# hasta mandar el aro a miles de metros de altura. Fix: separamos la
		# dirección HORIZONTAL (sin la componente vertical) para extrapolar,
		# y sumamos la altura del glideslope aparte, como ya hacíamos --
		# mismo patrón de "no mezclar horizontal con vertical" que resolvió
		# el bug de altitud/rumbo original de este proyecto.
		var direccion_horizontal: Vector3 = direccion - direccion.dot(arriba_motor_actual) * arriba_motor_actual
		if direccion_horizontal.length_squared() > 0.0001:
			direccion_horizontal = direccion_horizontal.normalized()

		for aro in entrada["contenedor"].get_children():
			var distancia_extra: float = aro.get_meta("distancia_extra")
			var punto: Vector3
			if aro.get_meta("lado_cab1"):
				punto = p1 - direccion_horizontal * distancia_extra
			else:
				punto = p2 + direccion_horizontal * distancia_extra
			# TERCER BUG REAL encontrado 2026-09-21 (reportado por el usuario:
			# "los aros suben como ganchos raros desde la tierra, tienen que
			# venir del cielo hacia abajo prolijo"): esto sumaba la altura
			# del glideslope directo sobre el eje Y CRUDO del motor
			# ("punto.y += altura"), pero ese eje NO es la vertical real acá
			# (Buenos Aires tiene ~34.5° de inclinación entre ambos, el mismo
			# problema de fondo que ya rompió el rumbo y la altitud antes en
			# este proyecto). Sumar sobre el eje equivocado desviaba cada
			# aro en diagonal en vez de subirlo derecho, y cuanto más
			# lejos/alto tenía que subir, peor se notaba. Ahora se suma
			# sobre arriba_motor_actual (la vertical real, ya calculada cada
			# cuadro), así el aro sube derecho de verdad.
			punto += arriba_motor_actual * aro.get_meta("altura")
			aro.global_transform = Transform3D(base_aros, punto)

# Luces de pista reales (pedido 2026-09-25, "que las pistas aparezcan con la
# iluminación que tienen las pistas reales de noche") -- investigado un
# overlay real de Cesium ion para esto (asset 3812, "Earth at Night"/NASA
# Black Marble), pero NO SIRVE para la vista principal: como dice el
# comentario grande al principio de este archivo, las baldosas de Google
# Photorealistic 3D Tiles no soportan overlays pintados encima (por eso el
# mapa de calles vive en su propio terreno aparte). En vez de eso, se
# generan luces DE VERDAD (mesh emisivos chiquitos, sin costo de luces
# dinámicas reales de Godot) a lo largo de cada pista que ya tenemos medida
# con precisión en aeropuertos_dos_cabeceras -- mismo patrón de
# recentrado-cada-cuadro que los aros de ILS (ver _actualizar_luces_pista_frame),
# pero SIN depender de que el ILS esté activado: son luces de la pista en sí,
# siempre están (solo se ven de noche, moduladas por factor_noche_actual).
var contenedor_luces_pista: Array = []
const ANCHO_MEDIO_PISTA_LUCES = 20.0  # separación de las luces de borde respecto al eje central
const ESPACIADO_LUCES_PISTA = 60.0    # cada cuántos metros va una luz de borde
# Agrandadas (2.5, antes 1.4) -- confirmada la causa real por Gemini/ChatGPT:
# quedaban enterradas por confiar en altura interpolada en vez de la
# colisión física real (ver _actualizar_luces_pista_frame). Con el rayo real
# ya alcanza un margen chico y seguro sobre el piso (1.0m).
const RADIO_LUZ_PISTA = 2.5
const ALTURA_LUCES_SOBRE_PISO = 1.0
const DESVIO_MAXIMO_RAYCAST_LUCES = 12.0  # metros -- más que esto, se descarta el rayo (ver _actualizar_luces_pista_frame)
const COLOR_LUZ_PISTA_UMBRAL_DEFECTO = Color(0.25, 1.0, 0.35)  # verde, como las luces de umbral reales
# De lejos (>3000m de la cámara) las luces de borde quedan a su tamaño
# normal; acercándose se van achicando hasta un mínimo (nunca desaparecen
# del todo) -- pedido explícito, "de cerca quedan como bolitas feas".
const DISTANCIA_ACHIQUE_LUCES_PISTA = 3000.0
const ESCALA_MINIMA_LUCES_PISTA = 0.35

# PRUEBA A/B/C/D (pedido explícito 2026-09-25, "hacemos cuatro diferentes...
# el que se vea más lindo desde arriba lo aplicamos a todos"): en vez de un
# selector manual (mucho más trabajo, se deja para más adelante si hace
# falta), 4 aeropuertos ya mapeados se llevan un estilo bien distinto entre
# sí para comparar de una. El resto de los aeropuertos usa "clasico_dorado"
# (el más parecido a lo que ya había) hasta que se elija un ganador.
#
# "modo":
#   "secuencial"        -- una sola luz "viaja" de punta a punta de la pista,
#                          en bucle (como las luces de aproximación reales).
#   "secuencial_doble"  -- dos ondas arrancan de cada punta y se cruzan en el
#                          medio, en bucle.
#   "pulso_conjunto"     -- todas las luces de borde prenden/apagan juntas
#                          (como si la pista entera "respirara").
# "velocidad": ciclos completos por segundo a lo largo de TODA la pista.
# "ancho_pulso": fracción de la pista que queda "encendida" a la vez (más
#   chico = destello más agudo y corto, como un flash real).
const PRESETS_LUCES_PISTA = {
	"clasico_dorado": {
		"color_borde": Color(1.0, 0.85, 0.4), "modo": "secuencial",
		"velocidad": 0.35, "ancho_pulso": 0.22,
	},
	"neon_celeste": {
		"color_borde": Color(0.25, 0.85, 1.0), "modo": "pulso_conjunto",
		"velocidad": 0.5, "ancho_pulso": 1.0,
	},
	"secuencial_doble_rosa": {
		"color_borde": Color(1.0, 0.35, 0.75), "modo": "secuencial_doble",
		"velocidad": 0.55, "ancho_pulso": 0.18,
	},
	"estroboscopico_blanco": {
		"color_borde": Color(1.0, 1.0, 1.0), "modo": "pulso_conjunto",
		"velocidad": 1.6, "ancho_pulso": 0.12,
	},
	# Pedido explícito 2026-09-25 ("tipo Palomar, que esas estén fijas, pero
	# que también tenga un efecto de que vaya y venga por arriba"): mismo
	# celeste de Palomar, pero las luces quedan SIEMPRE prendidas (brillo
	# base) y por encima pasa un destello más brillante que recorre la
	# pista de punta a punta y vuelve (no en bucle hacia un solo lado).
	"neon_celeste_vaiven": {
		"color_borde": Color(0.25, 0.85, 1.0), "modo": "fijo_con_viajero",
		"velocidad": 0.4, "ancho_pulso": 0.22, "brillo_base": 0.55,
	},
}
const ASIGNACION_ESTILO_PRUEBA = {
	"Morón": "clasico_dorado",
	"El Palomar": "neon_celeste",
	"San Fernando": "secuencial_doble_rosa",
	"Aeroparque": "neon_celeste_vaiven",
}
const ESTILO_POR_DEFECTO = "clasico_dorado"

func _generar_luces_pista(datos: Dictionary) -> void:
	var contenedor = Node3D.new()
	contenedor.name = "LucesPista_%s" % datos["nombre"].replace(" ", "")
	add_child(contenedor)

	var nombre_estilo: String = ASIGNACION_ESTILO_PRUEBA.get(datos["nombre"], ESTILO_POR_DEFECTO)
	var estilo: Dictionary = PRESETS_LUCES_PISTA[nombre_estilo]
	var color_borde: Color = estilo["color_borde"]
	# Ancho real de pista, cuando el usuario lo midió a mano (pedido
	# 2026-09-25) -- si no está el dato, se usa el genérico de siempre.
	var ancho_medio: float = datos.get("ancho_medio_pista", ANCHO_MEDIO_PISTA_LUCES)

	var mat_umbral = StandardMaterial3D.new()
	mat_umbral.shading_mode = BaseMaterial3D.SHADING_MODE_UNSHADED
	mat_umbral.albedo_color = COLOR_LUZ_PISTA_UMBRAL_DEFECTO
	mat_umbral.emission_enabled = true
	mat_umbral.emission = COLOR_LUZ_PISTA_UMBRAL_DEFECTO
	mat_umbral.emission_energy_multiplier = 3.0

	# Largo aproximado SOLO para decidir cuántas luces de borde poner (no
	# necesita ser exacto -- se recalcula la posición real cada cuadro en
	# _actualizar_luces_pista_frame, esto solo define la cantidad de puntos).
	var largo_aprox: float = _posicion_desde_lat_lon(datos["cab1_lat"], datos["cab1_lon"], datos.get("cab1_alt", 8.0)).distance_to(
		_posicion_desde_lat_lon(datos["cab2_lat"], datos["cab2_lon"], datos.get("cab2_alt", 8.0)))
	var cantidad_luces: int = max(2, int(largo_aprox / ESPACIADO_LUCES_PISTA))

	for i in range(cantidad_luces + 1):
		var t: float = float(i) / float(cantidad_luces)
		for lado in [1.0, -1.0]:
			var luz = MeshInstance3D.new()
			var esfera = SphereMesh.new()
			esfera.radius = RADIO_LUZ_PISTA
			esfera.height = RADIO_LUZ_PISTA * 2.0
			luz.mesh = esfera
			# Material PROPIO por luz (no compartido) -- necesario para que
			# cada una pueda tener su propio brillo en cada instante (la
			# esencia del efecto "viajando"/secuencial). Antes todas las
			# luces de borde compartían un único material, así que solo se
			# podían prender/apagar todas juntas.
			var mat_luz = StandardMaterial3D.new()
			mat_luz.shading_mode = BaseMaterial3D.SHADING_MODE_UNSHADED
			mat_luz.albedo_color = color_borde
			mat_luz.emission_enabled = true
			mat_luz.emission = color_borde
			mat_luz.emission_energy_multiplier = 0.0
			luz.set_surface_override_material(0, mat_luz)
			luz.set_layer_mask_value(1, false)
			luz.set_layer_mask_value(CAPA_MARCADORES_NOCTURNOS, true)
			luz.set_meta("t", t)
			luz.set_meta("lado", lado)
			contenedor.add_child(luz)

	# 2 luces de umbral verdes por cabecera (izquierda y derecha del eje),
	# marcando exactamente dónde empieza/termina la pista de verdad -- estas
	# quedan FIJAS (no siguen el estilo de la prueba), como las reales.
	for t_umbral in [0.0, 1.0]:
		for lado in [1.0, -1.0]:
			var luz_umbral = MeshInstance3D.new()
			var esfera_umbral = SphereMesh.new()
			esfera_umbral.radius = RADIO_LUZ_PISTA * 1.3
			esfera_umbral.height = RADIO_LUZ_PISTA * 2.6
			luz_umbral.mesh = esfera_umbral
			luz_umbral.set_surface_override_material(0, mat_umbral)
			luz_umbral.set_layer_mask_value(1, false)
			luz_umbral.set_layer_mask_value(CAPA_MARCADORES_NOCTURNOS, true)
			luz_umbral.set_meta("t", t_umbral)
			luz_umbral.set_meta("lado", lado)
			contenedor.add_child(luz_umbral)

	contenedor_luces_pista.append({
		"cab1_lat": datos["cab1_lat"], "cab1_lon": datos["cab1_lon"], "cab1_alt": datos.get("cab1_alt", 8.0),
		"cab2_lat": datos["cab2_lat"], "cab2_lon": datos["cab2_lon"], "cab2_alt": datos.get("cab2_alt", 8.0),
		"estilo": estilo,
		"ancho_medio_pista": ancho_medio,
		"contenedor": contenedor,
		"mat_umbral": mat_umbral,
	})

# Calcula cuánto tiene que brillar (0.0 a 1.0) la luz de borde que está en la
# posición "t" (0=cabecera 1, 1=cabecera 2) de la pista, en el instante
# "tiempo", según el "modo" del estilo asignado a ese aeropuerto.
func _brillo_luz_pista(t: float, tiempo: float, estilo: Dictionary) -> float:
	var velocidad: float = estilo["velocidad"]
	var ancho_pulso: float = max(estilo["ancho_pulso"], 0.02)
	match estilo["modo"]:
		"pulso_conjunto":
			# Todas las luces de la pista comparten el mismo valor -- no
			# depende de "t", la pista entera "respira" o destella junta.
			var fase_conjunta: float = fposmod(tiempo * velocidad, 1.0)
			var distancia_conjunta: float = min(fase_conjunta, 1.0 - fase_conjunta)
			return clamp(1.0 - distancia_conjunta / (ancho_pulso * 0.5), 0.0, 1.0)
		"secuencial_doble":
			# "Pliega" la pista al medio (t=0 y t=1 quedan en 1.0, t=0.5 en
			# 0.0) -- una misma onda viajera aplicada sobre esta coordenada
			# plegada aparenta dos ondas saliendo de cada punta a la vez.
			var t_plegado: float = abs(t - 0.5) * 2.0
			var fase_doble: float = fposmod(t_plegado - tiempo * velocidad, 1.0)
			var distancia_doble: float = min(fase_doble, 1.0 - fase_doble)
			return clamp(1.0 - distancia_doble / (ancho_pulso * 0.5), 0.0, 1.0)
		"fijo_con_viajero":
			# Brillo base SIEMPRE prendido (no llega nunca a apagarse del
			# todo) + un destello más fuerte que recorre la pista de punta a
			# punta y VUELVE (onda triangular, no en bucle hacia un solo
			# lado -- por eso "va y viene").
			var brillo_base: float = estilo.get("brillo_base", 0.5)
			var t_viajero: float = abs(fposmod(tiempo * velocidad, 2.0) - 1.0)
			var distancia_viajero: float = abs(t - t_viajero)
			var onda: float = clamp(1.0 - distancia_viajero / (ancho_pulso * 0.5), 0.0, 1.0)
			return clamp(brillo_base + onda * (1.0 - brillo_base), 0.0, 1.0)
		_:  # "secuencial" -- una sola luz viajando de punta a punta, en bucle
			var fase: float = fposmod(t - tiempo * velocidad, 1.0)
			var distancia: float = min(fase, 1.0 - fase)
			return clamp(1.0 - distancia / (ancho_pulso * 0.5), 0.0, 1.0)

# Recentra y prende/apaga las luces de pista cada cuadro -- mismo patrón de
# "separar horizontal de vertical" que ya resolvió los bugs de los aros de
# ILS (ver comentarios ahí), reusado acá para el offset lateral (ancho de
# pista) y la altura sobre el piso.
func _actualizar_luces_pista_frame() -> void:
	var visibles: bool = factor_noche_actual > 0.03
	for entrada in contenedor_luces_pista:
		entrada["contenedor"].visible = visibles
		if not visibles:
			continue
		var p1: Vector3 = _posicion_desde_lat_lon(entrada["cab1_lat"], entrada["cab1_lon"], entrada["cab1_alt"])
		var p2: Vector3 = _posicion_desde_lat_lon(entrada["cab2_lat"], entrada["cab2_lon"], entrada["cab2_alt"])
		var direccion: Vector3 = (p2 - p1)
		if direccion.length_squared() < 1.0:
			continue
		direccion = direccion.normalized()
		var direccion_horizontal: Vector3 = direccion - direccion.dot(arriba_motor_actual) * arriba_motor_actual
		if direccion_horizontal.length_squared() < 0.0001:
			continue
		direccion_horizontal = direccion_horizontal.normalized()
		var perpendicular: Vector3 = direccion_horizontal.cross(arriba_motor_actual).normalized()
		entrada["mat_umbral"].emission_energy_multiplier = 6.0 * factor_noche_actual

		var estilo: Dictionary = entrada["estilo"]
		var tiempo: float = Time.get_ticks_msec() * 0.001

		# SOLUCIÓN DEFINITIVA 2026-09-25 (confirmada por Gemini y ChatGPT):
		# la altura interpolada linealmente entre las dos cabeceras (p1.lerp
		# p2) es una buena aproximación SOBRE el eje de la pista (por eso
		# funciona bien para los aros de ILS, que nunca se despegan de ese
		# eje), pero acá las luces de BORDE se desplazan 20m a los costados
		# -- ahí el terreno real de Cesium (pasto, banquina, lomos) puede no
		# coincidir con esa altura interpolada, y quedaban enterradas. En vez
		# de confiar en la interpolación, tiramos un rayo real hacia abajo
		# contra la colisión física del terreno (mismo patrón que
		# principal.gd::_limitar_piso() ya usa para el avión) y apoyamos la
		# luz ahí, con un margen chico y seguro.
		var space_state := get_world_3d().direct_space_state
		for luz in entrada["contenedor"].get_children():
			var t: float = luz.get_meta("t")
			var lado: float = luz.get_meta("lado")
			var punto_horizontal: Vector3 = p1.lerp(p2, t) + perpendicular * (lado * entrada["ancho_medio_pista"])
			var origen_rayo: Vector3 = punto_horizontal + arriba_motor_actual * 500.0
			var destino_rayo: Vector3 = punto_horizontal - arriba_motor_actual * 500.0
			var consulta := PhysicsRayQueryParameters3D.create(origen_rayo, destino_rayo)
			var resultado := space_state.intersect_ray(consulta)
			# BUG REAL encontrado 2026-09-26 (reportado: "con el efecto
			# secuencial, alguna luz suelta parece desviar toda la línea"):
			# la dirección/eje es IDÉNTICO al de los aros de ILS (que
			# encajan perfecto con la pista real, confirmado por el
			# usuario) -- el problema es puntual del raycast: a veces pega
			# contra un auto/árbol/borde de techo en vez del asfalto, y esa
			# UNA luz mal ubicada arruina la sensación de línea recta ahora
			# que el efecto secuencial solo muestra 2-3 luces prendidas a
			# la vez (antes, con todas prendidas juntas, un error así se
			# disimulaba entre el resto). Si el rayo pega MUY lejos de la
			# altura que esperábamos ahí (según la interpolación entre las
			# dos cabeceras, que sabemos que es confiable a lo largo del
			# eje de la pista), descartamos ese resultado puntual y usamos
			# la línea recta en su lugar -- mejor una luz sin acomodar al
			# milímetro que una luz saltando a un lugar random.
			var punto_base: Vector3 = punto_horizontal
			if resultado:
				var desvio: float = (resultado["position"] - punto_horizontal).dot(arriba_motor_actual)
				if abs(desvio) <= DESVIO_MAXIMO_RAYCAST_LUCES:
					punto_base = resultado["position"]
			var punto: Vector3 = punto_base + arriba_motor_actual * ALTURA_LUCES_SOBRE_PISO
			luz.global_position = punto

			# Brillo de ESTA luz en este instante, según el "modo" del estilo
			# asignado al aeropuerto (ver PRESETS_LUCES_PISTA) -- esto es lo
			# que arma el efecto de "luces viajando" en vez de todas fijas.
			var brillo: float = _brillo_luz_pista(t, tiempo, estilo)
			var mat_luz: StandardMaterial3D = luz.get_surface_override_material(0)
			mat_luz.emission_energy_multiplier = lerp(0.4, 8.0, brillo) * factor_noche_actual

			# Pedido explícito ("de cerca quedan como bolitas amarillas
			# feas... que se achiquen, no las necesitamos tanto"): de lejos
			# quedan a su tamaño normal (se ven como lucecitas de ciudad,
			# que ya gustó); acercándose al avión se van achicando, sin
			# desaparecer del todo, para un efecto más "destello" y menos
			# "bolita sólida".
			if camara_juego:
				var distancia_camara: float = camara_juego.global_position.distance_to(punto)
				var escala: float = clamp(distancia_camara / DISTANCIA_ACHIQUE_LUCES_PISTA, ESCALA_MINIMA_LUCES_PISTA, 1.0)
				luz.scale = Vector3.ONE * escala

# Faro giratorio de aeropuerto (pedido explícito 2026-09-25: "desde
# Aeroparque se tendrían que ver las de Quilmes o Palomar... como que
# prendan y apagan con un color muy intenso") -- un billboard emisivo por
# aeropuerto, pensado para verse desde 20-30km, no para aterrizar (para eso
# están las luces de pista/ILS). Recomendado por Gemini y ChatGPT: nada de
# luces dinámicas reales de Godot (con ~30 aeropuertos sería un desastre de
# rendimiento) -- un quad con material emisivo, sin sombreado, siempre
# mirando a la cámara (billboard), con la energía de emisión pulsando fuerte
# para simular el destello.
var contenedor_faros: Array = []
const ALTURA_FARO_SOBRE_PISO = 45.0  # por encima de los edificios más altos típicos
const RADIO_FARO = 25.0
const VELOCIDAD_DESTELLO_FARO = 0.22  # ~1 destello cada 4.5s, como un faro real
var _textura_destello_faro: ImageTexture = null

# BUG REAL encontrado 2026-09-25 (reportado: "aparece un cuadrado blanco"):
# el faro era un QuadMesh con color sólido -- sin ninguna textura con
# transparencia, un quad SIEMPRE se ve como lo que es, un cuadrado con bordes
# duros, por más "transparency = ALPHA" que tenga el material (eso solo
# habilita la transparencia, no la crea sola). Generamos acá una textura
# chiquita con un degradado radial (blanco en el centro, totalmente
# transparente en el borde) para que se vea como un destello/resplandor
# real y no como un bloque. Se genera UNA sola vez y se reusa en todos los
# faros (son todos iguales).
func _obtener_textura_destello_faro() -> ImageTexture:
	if _textura_destello_faro:
		return _textura_destello_faro
	const LADO = 64
	var imagen := Image.create_empty(LADO, LADO, false, Image.FORMAT_RGBA8)
	var centro := Vector2(LADO / 2.0, LADO / 2.0)
	var radio_max: float = LADO / 2.0
	for y in range(LADO):
		for x in range(LADO):
			var distancia: float = Vector2(x + 0.5, y + 0.5).distance_to(centro) / radio_max
			var alfa: float = clamp(1.0 - distancia, 0.0, 1.0)
			alfa = alfa * alfa  # cae más suave hacia el borde, no lineal
			imagen.set_pixel(x, y, Color(1.0, 1.0, 1.0, alfa))
	_textura_destello_faro = ImageTexture.create_from_image(imagen)
	return _textura_destello_faro

func _generar_faro_aeropuerto(datos: Dictionary) -> void:
	var lat_medio: float = (datos["cab1_lat"] + datos["cab2_lat"]) / 2.0
	var lon_medio: float = (datos["cab1_lon"] + datos["cab2_lon"]) / 2.0
	var alt_medio: float = (datos.get("cab1_alt", 8.0) + datos.get("cab2_alt", 8.0)) / 2.0

	var faro = MeshInstance3D.new()
	faro.name = "Faro_%s" % datos["nombre"].replace(" ", "")
	var quad = QuadMesh.new()
	quad.size = Vector2(RADIO_FARO, RADIO_FARO)
	faro.mesh = quad

	var mat_faro = StandardMaterial3D.new()
	mat_faro.shading_mode = BaseMaterial3D.SHADING_MODE_UNSHADED
	mat_faro.albedo_color = Color(1.0, 1.0, 1.0, 1.0)
	mat_faro.albedo_texture = _obtener_textura_destello_faro()
	mat_faro.transparency = BaseMaterial3D.TRANSPARENCY_ALPHA
	mat_faro.emission_enabled = true
	mat_faro.emission = Color(1.0, 1.0, 0.9)
	mat_faro.emission_energy_multiplier = 2.0
	# El "glow" aditivo suma luz en vez de tapar lo de atrás con un
	# cuadrado -- junto con la textura radial, esto es lo que da el
	# aspecto de destello real en vez de bloque sólido.
	mat_faro.blend_mode = BaseMaterial3D.BLEND_MODE_ADD
	mat_faro.billboard_mode = BaseMaterial3D.BILLBOARD_ENABLED
	faro.set_surface_override_material(0, mat_faro)
	faro.set_layer_mask_value(1, false)
	faro.set_layer_mask_value(CAPA_MARCADORES_NOCTURNOS, true)
	add_child(faro)

	contenedor_faros.append({
		"lat": lat_medio, "lon": lon_medio, "alt": alt_medio,
		"nodo": faro,
	})

func _actualizar_faros_frame() -> void:
	var visibles: bool = factor_noche_actual > 0.03
	var space_state := get_world_3d().direct_space_state
	var t: float = Time.get_ticks_msec() * 0.001
	# Pulso agudo (potencia alta) en vez de una onda suave -- se ve como un
	# "flash" corto, no como una respiración lenta.
	var destello: float = pow(max(0.0, sin(t * TAU * VELOCIDAD_DESTELLO_FARO)), 8.0)
	for entrada in contenedor_faros:
		var faro: MeshInstance3D = entrada["nodo"]
		faro.visible = visibles
		if not visibles:
			continue
		var punto_horizontal: Vector3 = _posicion_desde_lat_lon(entrada["lat"], entrada["lon"], entrada["alt"])
		var origen_rayo: Vector3 = punto_horizontal + arriba_motor_actual * 500.0
		var destino_rayo: Vector3 = punto_horizontal - arriba_motor_actual * 500.0
		var consulta := PhysicsRayQueryParameters3D.create(origen_rayo, destino_rayo)
		var resultado := space_state.intersect_ray(consulta)
		var base: Vector3 = (resultado["position"] if resultado else punto_horizontal)
		faro.global_position = base + arriba_motor_actual * ALTURA_FARO_SOBRE_PISO

		# Un billboard normal se achica con la distancia como cualquier
		# objeto 3D -- a 20-30km ocuparía menos de un píxel y desaparecería.
		# Lo agrandamos en proporción a la distancia a la cámara para que
		# mantenga un tamaño APARENTE mínimo en pantalla, sin importar qué
		# tan lejos esté (esto es justamente lo que pidió el usuario: verse
		# desde Aeroparque hasta Quilmes o El Palomar).
		if camara_juego:
			var distancia: float = camara_juego.global_position.distance_to(faro.global_position)
			faro.scale = Vector3.ONE * max(1.0, distancia / 700.0)

		var mat: StandardMaterial3D = faro.get_surface_override_material(0)
		mat.emission_energy_multiplier = lerp(2.0, 35.0, destello)

func alternar_ils(activo: bool) -> void:
	ils_activo_global = activo
	for par in contenedores_ils_por_aeropuerto:
		par["positivo"].visible = activo
		par["negativo"].visible = activo
	for entrada in contenedor_ils_dos_cabeceras:
		entrada["contenedor"].visible = activo

# BUG REAL encontrado 2026-09-27 (reportado: "en Ezeiza el ILS individual
# nunca se activa, aunque el general sí lo prende"): Ezeiza tiene DOS pistas
# reales, registradas como "Ezeiza Pista 1" y "Ezeiza Pista 2" (cada una su
# propio par de aros), pero el NODO del aeropuerto -- el que usa el panel
# Torre para saber a quién le tocás el botón -- se llama simplemente
# "Ezeiza". Una comparación de igualdad exacta (nombre == entrada["nombre"])
# nunca podía coincidir. Esta función centraliza el criterio: coincide si es
# el mismo nombre exacto, O si el nombre de la entrada empieza con
# "<nombre> " (para agrupar "Ezeiza Pista 1"/"Ezeiza Pista 2" bajo "Ezeiza").
func _nombre_ils_coincide(nombre_entrada: String, nombre_buscado: String) -> bool:
	return nombre_entrada == nombre_buscado or nombre_entrada.begins_with(nombre_buscado + " ")

# ILS individual por aeropuerto (pedido 2026-09-27, panel "Torre" -- "poder
# activar/desactivar los ILS de los 3 aeropuertos más cercanos desde ahí"),
# independiente del interruptor maestro de arriba. Busca por nombre en las
# DOS listas posibles (dos cabeceras reales, o el sistema viejo de un solo
# rumbo) -- prende/apaga TODAS las que coincidan (un aeropuerto con varias
# pistas, como Ezeiza, tiene que prender las dos juntas).
func alternar_ils_aeropuerto(nombre: String, activo: bool) -> void:
	var encontro_alguna := false
	for entrada in contenedor_ils_dos_cabeceras:
		if _nombre_ils_coincide(entrada["nombre"], nombre):
			entrada["contenedor"].visible = activo
			encontro_alguna = true
	if encontro_alguna:
		return
	for par in contenedores_ils_por_aeropuerto:
		if _nombre_ils_coincide(par["nodo"].get_meta("nombre_bonito", ""), nombre):
			par["positivo"].visible = activo
			par["negativo"].visible = activo

func ils_activo_en_aeropuerto(nombre: String) -> bool:
	for entrada in contenedor_ils_dos_cabeceras:
		if _nombre_ils_coincide(entrada["nombre"], nombre):
			if entrada["contenedor"].visible:
				return true
	for par in contenedores_ils_por_aeropuerto:
		if _nombre_ils_coincide(par["nodo"].get_meta("nombre_bonito", ""), nombre):
			if par["positivo"].visible:
				return true
	return false

# BUG REAL encontrado 2026-09-27 (reportado: "el botón de ILS individual a
# veces no funciona o demora"): el panel Torre mostraba el botón "ILS"
# igual de activo para CUALQUIER aeropuerto cercano, pero varios de la
# lista de destinos no tienen NINGÚN dato de ILS cargado (ver
# project_ils_pendientes.md) -- al apretar, alternar_ils_aeropuerto() no
# encontraba nada que prender, así que el botón parecía "prenderse" un
# instante (actualización optimista) y se apagaba solo 0.5s después,
# cuando el refresco periódico confirmaba que en realidad nunca se activó
# nada. Con esto el panel puede consultar antes si vale la pena mostrar el
# botón como usable.
func aeropuerto_tiene_ils(nombre: String) -> bool:
	for entrada in contenedor_ils_dos_cabeceras:
		if _nombre_ils_coincide(entrada["nombre"], nombre):
			return true
	for par in contenedores_ils_por_aeropuerto:
		if _nombre_ils_coincide(par["nodo"].get_meta("nombre_bonito", ""), nombre):
			return true
	return false

# Marcador de misión (hospital o punto de interés para sobrevolar) -- versión
# liviana del de aeropuerto, SIN pista (no hay dónde aterrizar de verdad,
# todavía) -- pedido explícito: "no sé si aterrizar, pero sobrevolar". Los
# hospitales llevan una "H" grande y roja bien arriba, para encontrarlos a
# simple vista como pidió el usuario ("la busco a golpe de vista, la H, y ahí
# aterrizo"); los puntos de interés llevan su nombre, en dorado.
func _generar_marcador_mision(datos: Dictionary) -> Node3D:
	var raiz = Node3D.new()
	raiz.name = "Mision_" + datos["nombre"].replace(" ", "").replace("(", "").replace(")", "")
	raiz.set_meta("nombre_bonito", datos["nombre"])
	raiz.set_meta("tipo_mision", datos["tipo"])
	add_child(raiz)

	var es_aterrizaje: bool = datos["tipo"] == "aterrizaje"
	var color_texto := Color(1.0, 0.25, 0.25, 1.0) if es_aterrizaje else Color(1.0, 0.85, 0.2, 1.0)
	var color_borde := Color(0.2, 0.0, 0.0, 1.0) if es_aterrizaje else Color(0.2, 0.15, 0.0, 1.0)

	# SEGUNDO INTENTO 2026-09-20 (el primero, con no_depth_test + pixel_size
	# 0.4, TODAVÍA se veía gigante y se leía "Monumental" estando parado en
	# Avellaneda, en la otra punta de la ciudad -- confirmado por captura).
	# `no_depth_test` es justo lo que hacía que se dibujara SIEMPRE encima de
	# todo sin importar la distancia -- para los AEROPUERTOS eso es lo que
	# queremos (guiarse desde lejos), pero para misiones DENTRO de la ciudad,
	# a pocos km entre sí, es lo que las hacía superponerse y verse desde
	# cualquier lado. Sacado por completo + tamaño mucho más chico: ahora se
	# ocluye detrás de edificios como cualquier objeto normal, y solo se lee
	# de cerca (que es cuando hace falta, para encontrar dónde aterrizar).
	var etiqueta = Label3D.new()
	etiqueta.text = "H" if es_aterrizaje else datos["nombre"].to_upper()
	etiqueta.billboard = BaseMaterial3D.BILLBOARD_ENABLED
	etiqueta.pixel_size = 0.06
	etiqueta.font_size = 60 if es_aterrizaje else 50
	etiqueta.outline_size = 8
	etiqueta.modulate = color_texto
	etiqueta.outline_modulate = color_borde
	etiqueta.position = Vector3(0, 35, 0)
	raiz.add_child(etiqueta)

	return raiz

# Pueblo/paraje/referencia a lo largo de una ruta (pedido 2026-09-21) --
# cartel CHICO, sin beacon en altura ni pista -- son casi 180, uno con
# beacon gigante como los aeropuertos saturaría el mapa. Se ve de cerca
# nada más, como un cartel real de ruta al pasar por el pueblo.
func _generar_marcador_pueblo(nombre: String) -> Node3D:
	var raiz = Node3D.new()
	raiz.name = "Pueblo_" + nombre.replace(" ", "").replace(".", "").replace("-", "")
	raiz.set_meta("nombre_bonito", nombre)
	add_child(raiz)

	var etiqueta = Label3D.new()
	etiqueta.text = nombre.to_upper()
	etiqueta.billboard = BaseMaterial3D.BILLBOARD_ENABLED
	etiqueta.pixel_size = 0.05
	etiqueta.font_size = 46
	etiqueta.outline_size = 7
	# Rojo (pedido 2026-09-27, "de noche se confunden con los aeropuertos" --
	# antes eran celestes, muy parecido de lejos/desaturado de noche al
	# dorado del cartel de los aeropuertos reales). Rojo no se usa en ningún
	# otro cartel del juego, así que ahora un pueblo no se puede confundir
	# con un aeropuerto con ILS.
	etiqueta.modulate = Color(1.0, 0.25, 0.2, 1.0)
	etiqueta.outline_modulate = Color(0.2, 0.02, 0.0, 1.0)
	etiqueta.position = Vector3(0, 20, 0)
	raiz.add_child(etiqueta)

	return raiz
