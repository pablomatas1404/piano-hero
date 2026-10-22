extends Node3D

enum Estado { VOLANDO, LLEGADA, FRENANDO, DETENIDO }
var estado = Estado.VOLANDO

# Aviones descargados 2026-09-20 (poly.pizza, ver "Aviones 3D/LEEME...").
# "modelo" vacío = usar las piezas primitivas de siempre (el avioncito gris
# de toda la vida), no un GLB. Todavía es solo un cambio VISUAL -- cada
# avión vuela igual (mismas físicas) hasta que hagamos físicas por tipo.
# "escala"/"rotacion" corrigen cómo vino modelado cada GLB (cada uno se
# hizo con una convención distinta de "adelante"/"arriba"/tamaño real) --
# PRIMER AJUSTE a ciegas según lo que se vio en pantalla (Cessna ya salió
# casi perfecto, sirve de referencia de "tamaño correcto"); puede necesitar
# otra vuelta de ajuste fino.
const TIPOS_AVION = [
	{"nombre": "Avioncito (clásico)", "modelo": "", "helicoptero": false, "escala": 1.0, "rotacion": Vector3.ZERO, "sonido": "res://FX/motor_helice.mp3"},
	{"nombre": "Cessna", "modelo": "res://modelos_aviones/cessna.glb", "helicoptero": false, "escala": 0.85, "rotacion": Vector3.ZERO, "sonido": "res://FX/motor_helice.mp3"},
	# SEGUNDA VUELTA: la rotación en Z (roll) fue un error -- el problema
	# original NO era que estuviera panza arriba, era solo el tamaño gigante
	# (la cámara quedaba metida adentro del fuselaje, lo que se veía raro y
	# se interpretó como "al revés"). El roll de 180° lo puso panza arriba DE
	# VERDAD. Sacado, y escala bajada mucho más.
	# TERCERA VUELTA (pedido 2026-09-21, "estaba mirando para mi lado y
	# gigante"): mismo motivo que Jet privado/Avión de guerra -- el modelo
	# tiene el "adelante" invertido, necesita el mismo yaw de 180°. Escala
	# bajada a la mitad de nuevo (0.03 seguía siendo grande); puede necesitar
	# otra vuelta de ajuste fino según lo que se vea en pantalla.
	# Achicado 60% (pedido explícito 2026-09-22, "el alerón de atrás tapa toda
	# la visión de la pantalla"): 0.015 -> 0.006 (0.015 * 0.4).
	# "luces_manual" 2026-10-01: posicionadas a mano con el editor de luces en
	# vivo (arrastrar + clavar), reemplazando el cálculo automático por AABB.
	{"nombre": "Boeing", "modelo": "res://modelos_aviones/boeing.glb", "helicoptero": false, "escala": 0.006, "rotacion": Vector3(0, 180, 0), "sonido": "res://FX/motor_jet.mp3",
		"luces_manual": {"izq": Vector3(-748.3093, -65.6160, -204.7245), "der": Vector3(766.6332, -71.7569, -212.8981), "estrobo": Vector3(9.4899, 148.2665, -224.9929)}},
	# Mostraban la trompa/cabina de frente a la cámara -- la cámara va SIEMPRE
	# detrás del avión mirando hacia adelante, así que si se ve la trompa de
	# frente es que el modelo tiene el "adelante" invertido. 180° en yaw (Y).
	{"nombre": "Jet privado", "modelo": "res://modelos_aviones/jet_privado.glb", "helicoptero": false, "escala": 0.9, "rotacion": Vector3(0, 180, 0), "sonido": "res://FX/motor_jet.mp3",
		"luces_manual": {"izq": Vector3(-3.9672, -0.1821, -0.5093), "der": Vector3(3.8563, -0.1934, -0.4900), "estrobo": Vector3(-0.0333, 1.1496, -0.4804)}},
	# Escala +40% (pedido 2026-09-27) -- las luces son hijas del modelo, así
	# que escalan proporcionalmente y quedan en el mismo lugar relativo.
	{"nombre": "Avión de guerra", "modelo": "res://modelos_aviones/avion_de_guerra.glb", "helicoptero": false, "escala": 0.85, "rotacion": Vector3(0, 180, 0), "sonido": "res://FX/motor_jet.mp3",
		"luces_manual": {"izq": Vector3(-5.5777, -1.5728, -0.7603), "der": Vector3(5.7251, -1.4666, -0.7423), "estrobo": Vector3(-0.0981, -0.8672, -0.4409)}},
	# Orientación ya perfecta -- solo un poco más grande.
	# SIN sonido de motor (pedido 2026-09-21, "no es constante, es un
	# helicóptero cuando pasa, no mientras va volando"): el loop de Mixkit que
	# habíamos bajado era un efecto de "sobrevuelo" (crece y se aleja), no un
	# motor sostenido -- sonaba raro en loop. Se saca hasta encontrar uno que
	# sí sea un zumbido constante de verdad.
	{"nombre": "Helicóptero", "modelo": "res://modelos_aviones/helicoptero.glb", "helicoptero": true, "escala": 0.22, "rotacion": Vector3(0, 90, 0), "sonido": "",
		"luces_manual": {"izq": Vector3(-14.1928, 8.0870, 1.5506), "der": Vector3(12.0029, -1.5471, -2.8637), "estrobo": Vector3(-1.2623, 7.6731, 0.0880)}},
	# NUEVO 2026-09-26 (pedido explícito, "aunque no tengan partes animadas,
	# si son lindos los agregamos igual") -- bajado por el usuario de
	# Sketchfab, único de la nueva tanda que ya venía en .glb (los demás son
	# .fbx/.obj/.blend, necesitan más trabajo de conversión). Escala ajustada
	# tras primera prueba en vivo 2026-09-26 (usuario: "50% más chico").
	{"nombre": "Avión genérico (nuevo)", "modelo": "res://modelos_aviones/plane_generico.glb", "helicoptero": false, "escala": 0.5, "rotacion": Vector3(0, 180, 0), "sonido": "res://FX/motor_helice.mp3",
		"luces_manual": {"izq": Vector3(-9.5640, -2.7933, -1.7048), "der": Vector3(9.8257, -2.6805, -1.7442), "estrobo": Vector3(0.0398, 0.4705, -1.4673)}},
	# NUEVO 2026-09-26, misma tanda -- estos 3 son .fbx (Godot 4 los importa
	# nativo, sin necesitar Blender instalado). CUARTA vuelta 2026-09-26:
	# tamaño del KF-30 y orientación del combate confirmados bien -- quedan.
	# KF-30 y Ka-27 quedaron mirando "para el lado del usuario" (de frente a
	# la cámara) -- giro de 180° sobre lo que ya tenían. Ka-27 duplicado de
	# tamaño, combate triplicado.
	# "luces_manual" 2026-09-26: posiciones EXACTAS (no calculadas por AABB
	# genérico) sacadas de piezas de referencia que ya traía cada modelo --
	# el AABB automático suponía "ancho siempre en X", pero cada .fbx tiene
	# su propio eje de envergadura según cómo lo armó el autor original
	# (confirmado con un dump de la escena: KF-30 mide su largo en X y su
	# envergadura en Z; el avión de combate al revés). Para KF-30 se usó la
	# posición real de sus propias lucecitas de punta de ala (オブジェクト_014/
	# 015); para el avión de combate, la posición de los misiles bajo el ala
	# (rocket/rocket_001). Ka-27 no tenía piezas de referencia parecidas, así
	# que sigue siendo una estimación a partir de su caja general.
	# ROTACIÓN 2026-09-26: en 90° quedaba mirando "para las 6", pedido giro
	# de 180° más sobre eso para llegar a las 12 -> 270°.
	# Posiciones de luces reemplazadas 2026-09-26 por las medidas a mano en
	# Blender (ver prompt_mapeo_luces_blender.txt) -- mucho más precisas que
	# la estimación anterior sacada de オブジェクト_014/015.
	# ROTACIÓN 2026-09-26 CORREGIDA: 270 (probado antes) resultó del "paso a
	# paso" verbal (90->6, 180->9, entonces 270->12), pero esa base (90->6)
	# nunca se confirmó con foto -- era un reporte viejo, de antes de tener
	# el tamaño/luces definitivos, y probablemente estaba mal descripto.
	# Recalculado con datos DUROS: abrí kf30.fbx en Blender (conector en
	# vivo) y confirmé que la trompa apunta al eje +X de Blender. Con la
	# fórmula de conversión de ejes ya verificada (Godot(X,Y,Z) =
	# Blender(X,Z,-Y)), eso da "adelante nativo" = +X en Godot también
	# (coincide con el propio dump de la escena: trompa en X=+23, cola en
	# X=-15). La rotación Y de Godot sigue la regla de la mano derecha
	# estándar: rotar (1,0,0) por θ da (cosθ, 0, -senθ). En θ=180° eso da
	# (-1,0,0) = "izquierda de cámara" -- EXACTO a la foto real que ya
	# confirmamos en 180° (trompa a las 9). En θ=90° da (0,0,-1), el
	# "adelante" correcto de Godot (-Z). Los dos cálculos (180 e 90)
	# cuadran perfecto entre sí y contra la foto real -- por eso ahora sí,
	# 90 es el valor matemáticamente correcto, no 270.
	# Posiciones re-afinadas 2026-10-01 con el editor de luces en vivo
	# (arrastrar + clavar), reemplazando las medidas en Blender de 2026-09-26.
	{"nombre": "KF-30 (nuevo)", "modelo": "res://modelos_aviones/kf30.fbx", "helicoptero": false, "escala": 0.2, "rotacion": Vector3(0, 90, 0), "sonido": "res://FX/motor_jet.mp3",
		"luces_manual": {"izq": Vector3(-2.5449, 2.7354, -11.2163), "der": Vector3(-3.2927, 3.8049, 11.1270), "estrobo": Vector3(-5.2741, 6.3697, 0.2356)}},
	# Escala +40% (pedido 2026-09-27, mismo motivo que "Avión de guerra").
	{"nombre": "Ka-27 (nuevo)", "modelo": "res://modelos_aviones/ka27.fbx", "helicoptero": true, "escala": 1.4, "rotacion": Vector3(0, 180, 0), "sonido": "res://FX/motor_helicoptero.mp3",
		"luces_manual": {"izq": Vector3(-0.1501, -0.3725, -0.0564), "der": Vector3(0.2247, -0.3934, -0.0834), "estrobo": Vector3(0.0290, 0.0713, -0.2773)}},
	{"nombre": "Avión de combate (nuevo)", "modelo": "res://modelos_aviones/fighter_jet_nuevo.fbx", "helicoptero": false, "escala": 0.9, "rotacion": Vector3(0, 0, 0), "sonido": "res://FX/motor_jet.mp3",
		"luces_manual": {"izq": Vector3(-1.144, 0.244, 0.809), "der": Vector3(1.156, 0.244, 0.809), "estrobo": Vector3(0.0, 0.95, 1.5)}},
	# Piper PA-18 (pedido 2026-10-01, bajado de CGTrader en FBX). Escala y
	# rotación SIN verificar todavía (valores de partida típicos para un
	# avión chico de este tamaño real) -- ajustar según cómo se vea en
	# juego, mismo proceso que KF-30/Ka-27 (probar y corregir con feedback).
	{"nombre": "Piper PA-18 (nuevo)", "modelo": "res://modelos_aviones/piper_pa18.fbx", "helicoptero": false, "escala": 1.0, "rotacion": Vector3(0, 180, 0), "sonido": "res://FX/motor_helice.mp3",
		"textura": "res://modelos_aviones/piper_diffuse.jpg",
		# Posicionadas a mano con el editor de luces en vivo (2026-10-01),
		# reemplaza el ajuste automático de 1cm que era solo una aproximación.
		"luces_manual": {"izq": Vector3(-4.5841, 2.1438, 0.3211), "der": Vector3(4.6055, 2.0977, 0.3096), "estrobo": Vector3(-0.0257, 1.7982, -0.4211)}},
	# F-16 EXP ELIMINADO 2026-10-03 (pedido explícito: "ese avión no nos
	# sirve para nada, tiene una parte transparente en el fuselaje, borrarlo
	# a la mierda"). De paso elimina al principal sospechoso de los crashes
	# recientes (132MB de texturas PBR sin comprimir, varias de 10-27MB cada
	# una -- footprint de VRAM enorme en una GPU integrada/memoria
	# compartida, sumado al terreno fotorrealista de Cesium que ya es
	# pesado de por sí).
	# F/A-18E Super Hornet con AGM-65E Maverick (pedido 2026-10-10, avión
	# PRINCIPAL -- no enemigo). Bajado de Sketchfab a 46-53MB, reexportado en
	# Blender: texturas comprimidas (AUTO, calidad 85) + mallas unidas en una
	# sola + Decimate moderado (584mil -> 204mil polígonos, ~35%, silueta
	# intacta, confirmado a ojo con captura) + compresión Draco -- quedó en
	# 19.25MB. Pedido explícito de Pablo: para los aviones PRINCIPALES
	# priorizar que la cáscara exterior se vea bien, no hace falta detalle de
	# interior/articulaciones. Escala y rotación sin verificar en juego
	# todavía -- 1.0/180° son valores de partida típicos, ajustar con
	# feedback como el resto de los aviones nuevos.
	# "luces_manual" 2026-10-10: posición clavada a mano por Pablo con el
	# editor de luces en vivo -- BUG REAL encontrado el mismo día (reportado:
	# "ya van 4 veces que la oriento y se desacomoda al reiniciar"): clavar
	# luces solo ESCRIBE el valor en ajustes_luces_debug.txt, no lo aplica
	# solo al juego -- faltaba copiarlo acá a mano. Ya quedó copiado, no
	# hace falta volver a clavarla cada sesión.
	# Pedido 2026-10-11 ("muy grande, no se le ve el caño de escape atrás
	# del panel de abajo -- achicarlo 15%"): las luces (luces_manual) no
	# hace falta recalcularlas -- son hijas del modelo, escalan solas.
	{"nombre": "F/A-18E Super Hornet (nuevo)", "modelo": "res://modelos_aviones/fa18e_maverick.glb", "helicoptero": false, "escala": 0.85, "rotacion": Vector3(0, 180, 0), "sonido": "res://FX/motor_jet.mp3",
		"luces_manual": {"izq": Vector3(-7.0490, -0.9577, -0.7419), "der": Vector3(6.9543, -0.9974, -0.7223), "estrobo": Vector3(0.0000, 1.9135, -0.0156)}},

	# Tanda 2026-10-11 (pedido de Pablo: "integrá TODOS los aviones que bajé,
	# los probamos todos juntos y vamos dando de baja los feos, nos quedamos
	# con los buenos"). Sin "luces_manual" todavía -- usan el cálculo
	# automático por AABB (_generar_luces_para_modelo_externo), que ya existe
	# justo para este caso. Escala 1.0 / rotación 180° de partida (pedido
	# explícito: "por las dudas los girás 180 grados"), SIN verificar en
	# juego -- van a necesitar la vuelta de ajuste de siempre.
	# BUG REAL encontrado 2026-10-11 (reportado: "el A380 nunca aparece a la
	# vista, y tampoco pega ningún disparo"): medido el .glb directamente
	# (bounding box real del glTF) -- viene modelado en metros de verdad,
	# ~80m de envergadura. Con escala 1.0 y la cámara fija a solo 8-17
	# unidades del avión (OFFSET_CAMARA_DISTANCIA), la cámara queda
	# literalmente DENTRO del fuselaje -- no es que esté mal orientado, hay
	# avión de sobra tapando todo alrededor de la cámara. Probablemente
	# también explica los disparos fallados: el origen (punto medio de las
	# luces de ala, ahora a ~40m del centro) queda muy lejos de donde mira
	# la cámara. Bajado a 0.25 como primer ajuste -- sin confirmar en
	# juego todavía, ajustar con feedback como siempre. Boeing 747-400
	# tiene dimensiones reales casi idénticas (mismo riesgo), bajado
	# preventivamente con el mismo valor.
	# BUG REAL arreglado 2026-10-11 con la tecla P: la medición anterior
	# ("metros reales") había salido mal por un problema de formato de
	# números al leer el archivo -- el tamaño nativo real es ~19750
	# unidades de envergadura, no ~80. Escala recalculada: 79.75m (envergadura
	# real del A380) / 19750 ≈ 0.004. "recentrar_pivote" corrige el pivote
	# descentrado (mismo bug que tuvo el aquitaine). Primer intento con
	# datos reales, igual sin confirmar en juego -- ajustar con feedback.
	# Pedido 2026-10-11, segunda vuelta ("apareció, pero gigante -- achicarlo
	# 300%, y girarlo 90° de las 3 a las 12"): "achicar 300%" interpretado
	# como dividir por 4 (0.004/4=0.001), igual que "agrandar 100%" ya
	# significó duplicar en este proyecto. Giro: 180°-90°=90° -- si queda al
	# revés (como pasó una vez con el Pilatus), es sumar en vez de restar.
	# Pedido 2026-10-11, tercera vuelta ("giró para el lado contrario -- quedó
	# en las 6, tiene que ir a las 12 -- y achicarlo 80% más"). DATO
	# IMPORTANTE para los próximos aviones: "girar a la izquierda" en este
	# proyecto corresponde a SUMAR grados en Y, no restar (confirmado acá y
	# con el Pilatus antes) -- quedar anotado. 6 a 12 es un giro de 180°
	# sin importar la dirección: 90+180=270. Escala: 0.001*0.2=0.0002.
	# Pedido 2026-10-11, cuarta vuelta ("orientación perfecta, agrandar 5% y
	# subirlo un poquito si se puede -- sabe que viene una revisión general
	# de cámaras después"): 0.0002*1.05=0.00021. Subida chica de cámara
	# como primer intento, se termina de ajustar con el sistema nuevo.
	# Pedido 2026-10-16 ("clavé las luces del A380 -- ponelas, y que las dos
	# tengan el mismo tamaño, el de la más chica"): posición copiada del editor
	# de luces (ajustes_luces_debug.txt, 17:34) -- las coordenadas son grandes
	# porque están en unidades NATIVAS del modelo (las luces son hijas de la
	# instancia y escalan solas con 0.00021). "luces_mismo_tamano": la
	# estroboscópica deja de ser 1.2x más grande y queda igual que la roja y
	# la verde (el radio de las tres ya es el de la más chica).
	{"nombre": "Airbus A380-900", "modelo": "res://modelos_aviones/airbus_a380.glb", "helicoptero": false, "escala": 0.00021, "rotacion": Vector3(0, 270, 0), "sonido": "res://FX/motor_jet.mp3", "recentrar_pivote": true, "ajuste_altura_camara": 1.0,
		"luces_manual": {"izq": Vector3(27410.0820, -15143.6631, 52452.7305), "der": Vector3(81279.7031, 19258.0371, -23609.3984), "estrobo": Vector3(48548.1211, 8782.8301, -262.2363)},
		"luces_mismo_tamano": true},
	# Pedido 2026-10-11 ("funciona todo, pero agrandarlo un 40%"):
	# Pedido 2026-10-11, segunda vuelta ("quedó perfecto, pero agrandalo otro
	# 15-20% más para que quede más lindo"): 1.4 * 1.175 ≈ 1.65.
	# Pedido 2026-10-11, tercera vuelta ("se luce mejor 10% más grande"):
	{"nombre": "Saab 37 Viggen (enemigo liviano)", "modelo": "res://modelos_aviones/saab_viggen.glb", "helicoptero": false, "escala": 1.82, "rotacion": Vector3(0, 180, 0), "sonido": "res://FX/motor_jet.mp3"},
	# Pedido 2026-10-11 ("funciona todo, pero muy gigante, achicarlo 50%"):
	# Pedido 2026-10-11 ("quedó bien, agrandarlo 20%, pero se ve muy abajo
	# como si lo mirara desde arriba -- subir la cámara"): primer intento
	# de "ajuste_altura_camara", sin confirmar, ajustar con feedback.
	# Pedido 2026-10-11, segunda vuelta ("10% más grande, y la cámara sigue
	# baja -- sentía que estaba en planta baja mirando el primer piso"):
	# +10% tamaño, duplicado el ajuste de altura de cámara (2.0 -> 4.0).
	# También reportó algo "como una bandera" flameando en el ala derecha --
	# pendiente revisar en Blender, no se puede diagnosticar a ciegas.
	# Pedido 2026-10-11, tercera vuelta ("sigue como antes, planta baja/primer
	# piso -- agrandar 40% más"): +40% tamaño, triplicado el ajuste de altura
	# de cámara (4.0 -> 6.0) ya que el doble no alcanzó. Lo del ala
	# "bandera" queda pendiente para Blender, como dijo Pablo.
	{"nombre": "British Airways Boeing 747-400", "modelo": "res://modelos_aviones/ba_boeing747.glb", "helicoptero": false, "escala": 0.231, "rotacion": Vector3(0, 180, 0), "sonido": "res://FX/motor_jet.mp3", "ajuste_altura_camara": 6.0},
	# Pedido 2026-10-11 ("funciona todo, pero muy grande, achicarlo 50%"):
	# Pedido 2026-10-11, segunda vuelta ("sigue gigante, no se ve completo --
	# achicarlo 40% más"): 0.5 * 0.6 = 0.3.
	{"nombre": "Emirates Boeing 777-200", "modelo": "res://modelos_aviones/emirates_boeing777.glb", "helicoptero": false, "escala": 0.3, "rotacion": Vector3(0, 180, 0), "sonido": "res://FX/motor_jet.mp3"},
	# Pedido explícito: "subílo con todos los chiches, con la textura y todo
	# -- si hay que alivianar después, le sacamos pedazos, pero primero hay
	# que probarlo completo" -- el .glb copiado es el original, sin tocar.
	# Pedido 2026-10-11 ("funciona todo, pero parece una hormiguita --
	# agrandarlo 400% -- y girarlo 180° más, está mirando para mi lado"):
	# el 180° de partida le quedaba al revés para este modelo en particular,
	# así que se cancela (0°) en vez de sumarse.
	# Pedido 2026-10-11, segunda vuelta ("orientación bien, pero queda un
	# poco abajo -- agrandar 80% más"): 5.0 * 1.8 = 9.0. Primer intento de
	# altura de cámara para este, sin confirmar.
	# Pedido 2026-10-11, tercera vuelta ("agrandar 80% más"): 9.0*1.8=16.2.
	{"nombre": "Miljet (interior 5 estrellas)", "modelo": "res://modelos_aviones/miljet.glb", "helicoptero": false, "escala": 16.2, "rotacion": Vector3(0, 0, 0), "sonido": "res://FX/motor_jet.mp3", "ajuste_altura_camara": 2.0},
	# Pedido 2026-10-11 ("anda bien, pero girarlo 90° a la izquierda -- de
	# las 3 a las 12 -- y achicarlo 20-30%"): 180°-90°=90° (primer intento,
	# si quedó al revés es simplemente sumar en vez de restar). Achicado
	# 25% (punto medio de lo pedido).
	# Pedido 2026-10-11, segunda vuelta ("quedó apuntando a las 6, tiene que
	# mirar a las 12 -- 180° más"): 6 y 12 son puntos opuestos del reloj, un
	# giro de 180° los intercambia sin importar para qué lado -- 90+180=270.
	{"nombre": "Pilatus PC-24", "modelo": "res://modelos_aviones/pilatus_pc24.glb", "helicoptero": false, "escala": 0.75, "rotacion": Vector3(0, 270, 0), "sonido": "res://FX/motor_jet.mp3"},
	# Pedido 2026-10-11 ("me olvidé del helicóptero ruso, agregalo, y que
	# tenga el comando subir-bajar como el otro helicóptero"):
	# "helicoptero": true activa ese control (ver _es_helicoptero()), igual
	# que el Ka-27 que ya está en la lista. Escala/rotación sin verificar
	# todavía, ajustar con feedback como todos los demás.
	# Pedido 2026-10-11 ("apunta a las 6, hay que hacerlo apuntar a las 12 --
	# 180°", "muy grande, achicarlo 20%"): 180°+180°=0°.
	{"nombre": "Mil Mi-35M (helicóptero ruso)", "modelo": "res://modelos_aviones/mi35m.glb", "helicoptero": true, "escala": 0.8, "rotacion": Vector3(0, 0, 0), "sonido": "res://FX/motor_helicoptero.mp3"},
]
var tipo_avion_indice: int = 0

# City Tour (pedido explícito 2026-10-01, "prueba inicial" para probar
# checkpoints grandes + sonido + estrellitas antes de armar el circuito
# bueno por lugares de la ciudad -- empieza por aeropuertos porque ya están
# todos mapeados). Aeroparque -> Ezeiza -> Morón -> Campo de Mayo ->
# San Fernando -> volver a Aeroparque y aterrizar. Arranca en el aire a
# ~2000m, no en pista ("así no tenemos quilombo").
const RUTA_CITY_TOUR: Array[String] = ["Ezeiza", "Morón", "Campo de Mayo", "San Fernando"]
# Coordenadas trackeadas a mano por el usuario en vivo (2026-10-01, con el
# botón/tecla T, volando el circuito real) -- se usan como punto de partida
# ya cargado al arrancar el City Tour, mismo formato que "city_tour_trackeado".
const COORDENADAS_TRACKEADAS_CITY_TOUR: Array = [
	{"lat": -34.801013, "lon": -58.516862, "alt": 1464.25},  # Ezeiza
	{"lat": -34.693623, "lon": -58.626800, "alt": 1242.05},  # Morón
	{"lat": -34.561913, "lon": -58.670213, "alt": 1351.45},  # Campo de Mayo
	{"lat": -34.470775, "lon": -58.613806, "alt": 1326.79},  # San Fernando
]
const RADIO_DETECCION_CITY_TOUR = 220.0  # generoso -- el aro mide ~240m de radio externo
const ALTURA_INICIO_CITY_TOUR = 2000.0
const ALTURA_ARO_CITY_TOUR = 400.0
var modo_city_tour: bool = false
# Pedido explícito 2026-10-01 ("poneme estrella en los cuatro aeropuertos",
# todas visibles a la vez, no una por vez): un array por checkpoint en vez de
# un único "actual" -- cada índice 0..3 corresponde a RUTA_CITY_TOUR[i].
var estrellas_city_tour: Array = [false, false, false, false, false]  # [0..3] checkpoints, [4] aterrizaje final
var city_tour_estrellas_mundo: Array = [null, null, null, null]  # Label3D de cada checkpoint, null una vez pasado
var city_tour_lats: Array = [0.0, 0.0, 0.0, 0.0]
var city_tour_lons: Array = [0.0, 0.0, 0.0, 0.0]
# Trackeado a mano (pedido explícito 2026-10-01, "como vos no vas a saber
# calcular cuando pasa DIRECTAMENTE por el medio, te aprieto un botón justo
# ahí y te marco la coordenada exacta"): mientras un checkpoint no esté
# trackeado, se usa la detección genérica por distancia a la estrella (más
# laxa, "o te acercaste bastante"); una vez trackeado, se usa ESA coordenada
# real con un radio bastante más chico -- mucho más preciso que cualquier
# cálculo.
var city_tour_trackeado: Array = [null, null, null, null]  # null o {"lat","lon","alt"}
var halos_city_tour: Array = [null, null, null, null]  # esfera translúcida por checkpoint trackeado
# BUG REAL encontrado 2026-10-01 (reportado: "se aleja como la Luna, no está
# en un punto fijo"): el nodo "mundo" (raíz, script mundo.gd) es HERMANO del
# CesiumGeoreference, no su hijo -- nada que se agregue como hijo de "mundo"
# se recentra solo con el floating-origin del addon. TODO lo que sí funciona
# bien (aeropuertos, aros de ILS, etc.) se recalcula A MANO desde lat/lon
# todos los cuadros (ver _actualizar_ils_dos_cabeceras_frame en mundo.gd) --
# fijar una estrella UNA sola vez con global_position la dejaba clavada en el
# espacio crudo del motor mientras el origen se recentraba en el avión cada
# cuadro. Por eso se guardan lat/lon arriba y todas se reposicionan TODOS los
# cuadros en _actualizar_city_tour, igual que el resto del mundo.
var tecla_trackear_anterior: bool = false
const RADIO_DETECCION_CITY_TOUR_TRACKEADO = 60.0
var estrellas_city_tour_labels: Array = []
var sonido_bonus_city_tour: AudioStreamPlayer = null

# Licencia de Helicóptero -- Modo Carrera (pedido 2026-09-21, "ya lo hablamos
# todo, hacerlo completo"): 3 viajes cortos y fáciles, punto A a punto B,
# CERCA (mismos lugares reales que ya usamos en las misiones de sobrevuelo de
# mundo.gd, así no hay que inventar coordenadas nuevas). Cada uno paga al
# completarlo (precio editable con ▲▼, "viaje más lejos paga más" como
# pediste) Y cuenta como parte de la licencia -- simplificación acordada en
# la charla de café: en vez de un trámite de pago aparte para "la licencia"
# en sí, completar las 3 alcanza para que se otorgue sola. Si más adelante
# querés separar "plata para pagar el trámite" de "aprobar el examen", lo
# picamos en dos pasos, pero por ahora esto ya es jugable de punta a punta.
var MISIONES_LICENCIA_HELICOPTERO = [
	{"nombre": "Aeroparque → Puerto Madero", "origen_lat": -34.5589, "origen_lon": -58.4164,
		"destino_lat": -34.6092507027205, "destino_lon": -58.3662333858193, "precio": 300},
	{"nombre": "Puerto Madero → Obelisco", "origen_lat": -34.6092507027205, "origen_lon": -58.3662333858193,
		"destino_lat": -34.6033765232351, "destino_lon": -58.3815493289313, "precio": 250},
	{"nombre": "Obelisco → Casa Rosada", "origen_lat": -34.6033765232351, "origen_lon": -58.3815493289313,
		"destino_lat": -34.6072774426033, "destino_lon": -58.3693545243094, "precio": 200},
]
const NOMBRE_LICENCIA_HELICOPTERO = "Helicóptero"
var mision_licencia_indice: int = -1  # -1 = ninguna misión de licencia en curso
var objetivo_licencia: Node3D = null  # nodo temporal con meta lat/lon, igual que objetivo_mision
const UMBRAL_LLEGADA_LICENCIA = 60.0
# Aros de la misión de licencia -- MISMO diseño visual que probamos para el
# ILS de aeropuertos (al usuario le encantó cómo quedaban), pero llevados
# acá porque para un punto A -> punto B cualquiera no hace falta el rumbo
# REAL de una pista (esa fue la parte que salió mal con el ILS) -- alcanza
# con una línea recta entre origen y destino, así que no hay riesgo de mala
# orientación. MÁS GRANDES que los del ILS (pedido explícito, "más margen de
# error"). OBLIGATORIOS durante la licencia -- no se pueden apagar (ver
# botón de ayuda visual).
const CANTIDAD_AROS_LICENCIA = 6
const RADIO_INTERNO_ARO_LICENCIA = 28.0
const RADIO_EXTERNO_ARO_LICENCIA = 34.0
const ALTURA_AROS_LICENCIA = 60.0
var contenedor_aros_licencia: Node3D = null
# Subida/bajada vertical del helicóptero -- pedido explícito, ya tiene sus
# propios botones de joystick configurados ("vertical_arriba"/"vertical_abajo"
# en ACCIONES_JOYSTICK) esperando desde que armamos el sistema de mapeo.
# Subido de 8.0 a 20.0 (pedido explícito 2026-09-22, "estoy a 3000 metros y
# baja un metro por segundo, no termina más") -- a 20 m/s, bajar 3000m tarda
# 2.5 minutos en vez de más de 6.
const VELOCIDAD_VERTICAL_HELICOPTERO = 20.0

# Antes esto era una velocidad fija (10.0). Ahora es un acelerador de
# verdad: arranca despacito (para que el mapa "rinda" en los tramos
# cortos) y el jugador la sube con W / la baja con S hasta un tope. El
# mínimo es 0 -- podés quedarte quieto en el aire si querés -- pero se
# arranca en VELOCIDAD_INICIAL, no en 0, para no sentir que el juego se
# congeló apenas empieza.
# TEMPORAL PARA TESTEAR (pedido explícito 2026-09-20): con velocidad inicial
# baja + cabeceo lento, bajar/subir 1000m tardaba una eternidad -- imposible
# probar el trayecto Aeroparque-San Fernando en un tiempo razonable. Subido
# fuerte a propósito SOLO para poder probar rápido; cuando el trayecto ya
# funcione bien, volvemos a bajar esto a algo realista (un Cessna no acelera
# ni cabecea así).
const VELOCIDAD_INICIAL = 20.0

# Orientación inicial (pedido 2026-09-21, segunda vuelta -- la primera, un
# giro de 180° a mano, no funcionó porque los ejes del motor no coinciden
# con el compás real): en vez de adivinar una matriz de rotación, calculamos
# el rumbo REAL de Aeroparque a San Fernando con la misma fórmula de
# navegación (great-circle bearing) que ya usamos para todo lo demás, y
# orientamos el avión con ella -- el mismo mecanismo probado de
# _orientar_aeropuerto_usuario/_confirmar_viaje, no una matriz a ciegas.
# Coordenadas iguales a las de aeropuertos_lla en mundo.gd (no se leen de
# ahí para no depender de que mundo._ready() ya haya corrido -- este avión
# se prepara ANTES que mundo, por ser hijo y no padre).
const LAT_AEROPARQUE = -34.5589
const LON_AEROPARQUE = -58.4164
const LAT_SAN_FERNANDO = -34.4532
const LON_SAN_FERNANDO = -58.5896
var _orientacion_inicial_aplicada: bool = false

# Cortina de carga (pedido 2026-09-21: "al arrancar se ve 2-5 segundos mirando
# al río con un palo negro gigante, antes de asentarse mirando a San
# Fernando"): eso es Cesium terminando de bajar las texturas reales del
# terreno (muestra un relleno feo mientras tanto) combinado con el instante
# en que recién se aplica la orientación inicial -- en vez de perseguir ese
# instante exacto, tapamos la pantalla con negro hasta que la orientación ya
# esté aplicada Y pasó un ratito extra para que el terreno tenga chance de
# cargar de verdad -- así nunca se ve la transición fea, se ve directo el
# resultado final.
@onready var cortina_de_carga: ColorRect = get_node("../HUD/CortinaDeCarga")
@onready var fondo_presentacion: TextureRect = get_node("../HUD/CortinaDeCarga/FondoPresentacion")
const ESPERA_CORTINA_DE_CARGA = 9.5  # -3s, pedido explícito ("tarda demasiado")
var _tiempo_desde_orientacion_inicial: float = -1.0
const VELOCIDAD_MINIMA = 0.0
const VELOCIDAD_MAXIMA = 1500.0  # AMPLIADO 2026-09-20 -- después se limita distinto por tipo de avión
const ACELERACION = 40.0  # unidades por segundo, por segundo
var velocidad_actual = VELOCIDAD_INICIAL
# CABECEO: antes se acumulaba sin límite mientras mantenías la flecha (podía
# terminar en un loop completo) y no volvía solo a nivel al soltar -- por eso
# el avión quedaba "torcidito" para siempre con cualquier toque mínimo, tanto
# en tierra como en vuelo. Ahora funciona igual que el banco: apunta a un
# ángulo máximo mientras mantenés la flecha, y se autonivela solo al soltar.
# AGRANDADO 2026-09-20 (mismo pedido que el radio de giro: "no importa que no
# sea realista, priorizar poder corregir rápido"): el usuario reportó quedar
# atrapado en picada sin poder recuperar a tiempo, con tan poca autoridad de
# cabeceo. Más margen para subir la nariz rápido.
const CABECEO_MAXIMO = 40.0          # grados de nariz arriba/abajo máximos
const VELOCIDAD_CABECEO_ENTRADA = 45.0 # grados por segundo, entrando
const VELOCIDAD_CABECEO_SALIDA = 60.0  # grados por segundo, autonivelando al soltar
var cabeceo_actual = 0.0
# Alabeo (banco): en vez de girar directo con la flecha, el avión se INCLINA de
# a poco hacia el lado que apretás (como uno real), y cuanto más inclinado está,
# más rápido gira -- así se llama "viraje coordinado". Investigado y confirmado:
# es el modelo real que usan los simuladores de vuelo (bank angle proporcional
# a la velocidad de giro). Al soltar la flecha, se va nivelando solo.
# AGRANDADO 2026-09-20 (pedido explícito, "no importa que no sea realista"):
# sin aproximación instrumental todavía, pasarse de largo de la pista con un
# radio de giro tan amplio significaba tardar mucho en volver a alinearse.
# Más adelante, cuando cada avión tenga sus propias físicas (Cessna ágil,
# Boeing lento), esto se vuelve a ajustar por tipo -- por ahora es un valor
# único para todos, y va para el lado de "más fácil de corregir".
const BANCO_MAXIMO = 45.0            # grados de inclinación máxima al girar
const VELOCIDAD_BANCO_ENTRADA = 35.0 # grados por segundo, entrando en la curva
const VELOCIDAD_BANCO_SALIDA = 100.0 # grados por segundo, SALIENDO de la curva --
									  # más rápido que entrar, para que soltar la
									  # flecha frene el giro enseguida y no "arrastre"
									  # de más (era la causa del salto grande que
									  # pasaba con toques cortos del teclado).
const FACTOR_GIRO_POR_BANCO = 0.6    # grados de giro por segundo, por cada grado de inclinación --
									  # vuelta completa a banco máximo, no unos pocos segundos)
var banco_actual = 0.0

# Panel de sensibilidad por avión (pedido 2026-09-21, herramienta de ajuste
# TEMPORAL mientras se prueban los aviones -- "para el juego final no va a
# tener esos botones, pero ahora sí"): tres números por avión (nombre del
# TIPOS_AVION como clave) que reemplazan a las constantes de arriba durante
# el vuelo -- "giro" (FACTOR_GIRO_POR_BANCO), "angulo" (BANCO_MAXIMO, "lo que
# corregiste ayer") y "vertical" (CABECEO_MAXIMO en aviones normales -- es su
# forma de subir/bajar -- o VELOCIDAD_VERTICAL_HELICOPTERO en el helicóptero,
# que tiene empuje vertical propio con R/F). Se guardan en disco por avión,
# así cada uno mantiene su propio ajuste entre partidas.
const RUTA_CONFIG_SENSIBILIDAD = "user://sensibilidad_aviones.cfg"
var ajustes_avion: Dictionary = {}

func _ajustes_por_defecto(indice: int) -> Dictionary:
	return {
		"giro": FACTOR_GIRO_POR_BANCO,
		"angulo": BANCO_MAXIMO,
		"vertical": VELOCIDAD_VERTICAL_HELICOPTERO if TIPOS_AVION[indice]["helicoptero"] else CABECEO_MAXIMO,
		"vel_minima": VELOCIDAD_MINIMA,
		"vel_maxima": VELOCIDAD_MAXIMA,
		# Pedido explícito 2026-09-27: "cada avión tiene su propia manera de
		# subir/bajar la nariz o girar" -- multiplicador de la velocidad con
		# la que la animación visual de cabeceo/banco se acerca a su
		# objetivo (VELOCIDAD_CABECEO_*/VELOCIDAD_BANCO_* de más arriba).
		# 1.0 = como está calibrado por defecto; menos que 1 = más suave/
		# lento (Boeing, jet privado); más que 1 = más brusco/ágil (Cessna,
		# avión de guerra). Independiente por avión, igual que el resto de
		# este panel.
		"velocidad_animacion": 1.0,
		# Pedido explícito 2026-10-03: sensibilidad al viento por avión (uno
		# más chico/liviano se mueve más con el mismo viento que uno más
		# grande/pesado) -- multiplicador sobre la deriva que aplica el
		# sistema de viento (ver _actualizar_viento). 1.0 = valor de partida,
		# se ajusta por avión igual que el resto de este panel.
		"viento_sensibilidad": 1.0,
	}

func _ajustes_avion_actual() -> Dictionary:
	var nombre: String = TIPOS_AVION[tipo_avion_indice]["nombre"]
	if not ajustes_avion.has(nombre):
		ajustes_avion[nombre] = _ajustes_por_defecto(tipo_avion_indice)
	return ajustes_avion[nombre]

func _cargar_sensibilidad_aviones() -> void:
	var cfg = ConfigFile.new()
	if cfg.load(RUTA_CONFIG_SENSIBILIDAD) != OK:
		return
	for nombre_avion in cfg.get_sections():
		ajustes_avion[nombre_avion] = {
			"giro": cfg.get_value(nombre_avion, "giro", FACTOR_GIRO_POR_BANCO),
			"angulo": cfg.get_value(nombre_avion, "angulo", BANCO_MAXIMO),
			"vertical": cfg.get_value(nombre_avion, "vertical", CABECEO_MAXIMO),
			"vel_minima": cfg.get_value(nombre_avion, "vel_minima", VELOCIDAD_MINIMA),
			"vel_maxima": cfg.get_value(nombre_avion, "vel_maxima", VELOCIDAD_MAXIMA),
			"velocidad_animacion": cfg.get_value(nombre_avion, "velocidad_animacion", 1.0),
			"viento_sensibilidad": cfg.get_value(nombre_avion, "viento_sensibilidad", 1.0),
		}

func _guardar_sensibilidad_aviones() -> void:
	var cfg = ConfigFile.new()
	cfg.load(RUTA_CONFIG_SENSIBILIDAD)
	var nombre: String = TIPOS_AVION[tipo_avion_indice]["nombre"]
	var ajustes := _ajustes_avion_actual()
	cfg.set_value(nombre, "giro", ajustes["giro"])
	cfg.set_value(nombre, "angulo", ajustes["angulo"])
	cfg.set_value(nombre, "vertical", ajustes["vertical"])
	cfg.set_value(nombre, "vel_minima", ajustes["vel_minima"])
	cfg.set_value(nombre, "vel_maxima", ajustes["vel_maxima"])
	cfg.set_value(nombre, "velocidad_animacion", ajustes["velocidad_animacion"])
	cfg.set_value(nombre, "viento_sensibilidad", ajustes["viento_sensibilidad"])
	cfg.save(RUTA_CONFIG_SENSIBILIDAD)

func _refrescar_panel_sensibilidad() -> void:
	var ajustes := _ajustes_avion_actual()
	label_giro_valor.text = "%.2f" % ajustes["giro"]
	label_angulo_valor.text = "%.0f°" % ajustes["angulo"]
	if _es_helicoptero():
		label_vertical_titulo.text = "Vertical"
		label_vertical_valor.text = "%.0f m/s" % ajustes["vertical"]
	else:
		label_vertical_titulo.text = "Subir/bajar"
		label_vertical_valor.text = "%.0f°" % ajustes["vertical"]
	label_vel_minima_valor.text = "%.0f" % ajustes["vel_minima"]
	label_vel_maxima_valor.text = "%.0f" % ajustes["vel_maxima"]
	label_velocidad_animacion_valor.text = "%.1fx" % ajustes["velocidad_animacion"]
	label_viento_valor.text = "%.1fx" % ajustes["viento_sensibilidad"]

func _ajustar_sensibilidad(clave: String, delta_valor: float, minimo: float, maximo: float) -> void:
	var ajustes := _ajustes_avion_actual()
	ajustes[clave] = clamp(ajustes[clave] + delta_valor, minimo, maximo)
	_refrescar_panel_sensibilidad()
	_guardar_sensibilidad_aviones()
# Altura del piso en el mundo (tiene que coincidir con la posición del piso en Mundo)
# Con el terreno real de Cesium, el nivel del piso cerca del origen (Aeroparque)
# es aproximadamente 0 (el CesiumGeoreference ya se configuró con esa altitud).
var altura_piso = 0.0

@onready var etiqueta_altimetro: Label = get_node("../HUD/PanelInstrumentos/CajaAltitud/AltimetroLabel")
@onready var etiqueta_rumbo: Label = get_node("../HUD/PanelInstrumentos/CajaRumbo/RumboLabel")
@onready var etiqueta_distancia: Label = get_node("../HUD/PanelInstrumentos/CajaDistancia/DistanciaLabel")

# Pedido 2026-10-16 ("como un relojito del avión, pero que me diga cuánta RAM
# libre queda -- así si veo un crash puedo chequear si fue por memoria"):
# una caja más en la fila de instrumentos (ALT/RUMBO/VEL/DIST), mismo estilo,
# con la RAM libre del sistema. Se actualiza cada INTERVALO_RAM segundos, no
# todos los cuadros (OS.get_memory_info() no es gratis).
var etiqueta_ram: Label = null
var _acumulador_ram: float = 0.0
const INTERVALO_RAM := 2.0

# PASO 1 -- ESQUELETO DE MISIONES (pedido 2026-10-16: "modo juego con
# diferentes misiones, que vaya avanzando" -- lore propio, modo práctica
# antes del nivel 1). Genérico a propósito: cada "paso" de una secuencia
# reusa contadores que YA EXISTEN (mision_verde_destruidos para Capital,
# objetivos_destruidos para aeropuertos) en vez de inventar un sistema de
# objetivos nuevo -- la idea es que agregar misiones futuras sea solo
# agregar entradas a una lista, no escribir código nuevo cada vez.
var secuencia_mision_actual: Array = []
var mision_paso_actual: int = -1
var etiqueta_objetivo_mision: Label = null

# Pedido 2026-10-16 ("modo práctica: primero Capital con un solo tiro,
# después los aeropuertos con un solo tiro"): el "chequeo" de cada paso es
# una función lambda que consulta el contador real -- así cualquier
# contador que ya exista en el juego (barcos, industria, invasión...) sirve
# para armar una misión nueva sin tocar su propio sistema.
const SECUENCIA_MODO_PRACTICA := [
	{
		"nombre": "Capital Federal",
		"texto": "MODO PRÁCTICA\nDestruí los objetivos verdes de Capital Federal",
	},
	{
		"nombre": "Aeropuertos",
		"texto": "Ahora: destruí los objetivos de los aeropuertos",
	},
]

# Pedido 2026-10-16 ("Nivel 1: ir y destruir tres barcos y volver a base"):
# el paso "Hundir 3 barcos" usa una FOTO del contador real de barcos
# (mision_barcos_base, tomada al arrancar el nivel) para pedir "3 MÁS", sin
# importar cuántos ya hundiste antes jugando sueltO. "Volver a base" mide
# la distancia real a Aeroparque -- mismo patrón que usa el resto del
# juego para "llegaste a destino".
var mision_barcos_base: int = 0
const RADIO_VOLVER_BASE := 600.0  # metros
const SECUENCIA_NIVEL_1 := [
	{"nombre": "Hundir 3 barcos", "texto": "NIVEL 1\nDestruí 3 barcos"},
	{"nombre": "Volver a base", "texto": "¡Bien! Ahora volvé a Aeroparque"},
]

# Pedido 2026-10-16 ("agregale cuatro o cinco misiones más, de a dos es muy
# lento"): reusan los 3 tipos de blanco que ya existían con su propio
# contador (industria, invasión) -- Nivel 4 los junta todos para un final
# más difícil, incluido Capital y aeropuertos del modo práctica.
const SECUENCIA_NIVEL_2 := [
	{"nombre": "Industria", "texto": "NIVEL 2\nDetectamos actividad industrial enemiga en La Plata. Destruila toda."},
]
var mision_invasores_base: int = 0
const SECUENCIA_NIVEL_3 := [
	{"nombre": "Invasores", "texto": "NIVEL 3\nUna oleada de tropas está desembarcando por la costa. Destruí 10."},
]
const SECUENCIA_NIVEL_4 := [
	{"nombre": "Defensa Total", "texto": "NIVEL 4 — FINAL\nAtaque combinado: barcos, aeropuertos, Capital e industria. ¡A por todo!"},
]

# MISIONES COMERCIALES (pedido 2026-10-17, "modo civil -- ir de un
# aeropuerto a otro, sin disparar, con un poco de lore, no 'ir de acá para
# allá' nomás"). Cada paso usa "tipo": "llegar_aeropuerto" (destino = nombre
# real de un aeropuerto del juego) o "llegar_punto" (lat/lon de un lugar
# marcado a mano con la tecla M -- acá, el Hangar 5 de Aeroparque). El
# Hangar 5 en especial: pedido explícito "usá los hangares, no solo para
# aterrizar, también para salir -- que no sea solo aterrizar, después hay
# que carretear hasta el lugar justo".
const COORD_HANGAR_5_AEROPARQUE := {"lat": -34.5569770646676, "lon": -58.4144436451155}
const COORD_AEROPARQUE := {"lat": -34.5589, "lon": -58.4164}

# Pedido 2026-10-17 ("destinos nacionales, todos los que tenemos trackeados,
# ordenados de más cerca a más lejos, cada uno con su precio en dólares,
# editable con el +/- -- y que cambie según qué avión uses"): "var", no
# "const" -- el precio se edita en vivo, mismo patrón que ya usan las
# misiones de helicóptero (_poblar_lista_misiones). Orden y distancias
# aproximadas desde Aeroparque.
# BUG REAL encontrado 2026-10-17 (reportado: "aterricé en Río Cuarto y no me
# pagó"): el lat/lon de cada destino se había tomado de una fuente aproximada
# (mapa online), que no coincide con dónde está en realidad la pista de ESE
# aeropuerto dentro del simulador (mundo.gd tiene su propio dato, separado,
# para el ILS/beacon de cada uno) -- la diferencia llegaba a 1,5km en varios
# casos (Rosario, Córdoba, Río Cuarto), bien por fuera de cualquier radio de
# llegada razonable. Ahora cada destino apunta, por nombre EXACTO, al
# aeropuerto real ya cargado en mundo.gd (campo "aeropuerto_real") -- al
# armar el viaje (_iniciar_destino_nacional) se usa la posición de ESE nodo,
# nunca el lat/lon escrito a mano acá abajo (que queda solo de respaldo por
# si algún día ese nombre no se encuentra).
var DESTINOS_NACIONALES := [
	{"nombre": "Chivilcoy", "aeropuerto_real": "Aeródromo Chivilcoy", "lat": -34.9617070587302, "lon": -60.0288016448983, "precio": 150},
	{"nombre": "Gualeguaychú (Entre Ríos)", "aeropuerto_real": "Gualeguaychú", "lat": -33.0087525020559, "lon": -58.6129206261459, "precio": 200},
	{"nombre": "Rosario (Santa Fe)", "aeropuerto_real": "Rosario (Islas Malvinas)", "lat": -32.9167810491964, "lon": -60.7877080510417, "precio": 320},
	{"nombre": "Río Cuarto (Córdoba)", "aeropuerto_real": "Río Cuarto (Las Higueras)", "lat": -33.0837345938044, "lon": -64.259578187267, "precio": 480},
	{"nombre": "Córdoba Capital", "aeropuerto_real": "Córdoba (Taravella)", "lat": -31.3228603321429, "lon": -64.2071143328457, "precio": 550},
	{"nombre": "San Luis", "aeropuerto_real": "San Luis (Brig. Mayor César Raúl Ojeda)", "lat": -33.2765558892837, "lon": -66.3569762482839, "precio": 600},
]
const PASO_PRECIO_DESTINO := 25
# Pedido 2026-10-17, SEGUNDA VUELTA ("si dice San Fernando-Morón, al apretar
# Volar te tiene que teletransportar a San Fernando, no respetar el destino
# nomás estés donde estés -- no quiero que sea así" -- reemplaza el pedido
# anterior de "nunca teletransportar"): cada misión ahora trae su propio
# "origen" (lat/lon reales, casi siempre un hangar trackeado) -- al apretar
# Volar, _iniciar_mision_comercial() primero teletransporta ahí.
const MISIONES_COMERCIALES := [
	{
		"nombre": "Carga militar a Campo de Mayo",
		"descripcion": "Llegó un cargamento de armamento por barco a Aeroparque. Hay que llevarlo a la guarnición de Campo de Mayo antes de que caiga la noche.",
		"origen": COORD_AEROPARQUE,
		"precio": 120,
		"pasos": [
			{"nombre": "Entregar en Campo de Mayo", "texto": "Base a piloto: cargamento militar asegurado. Destino: guarnición de Campo de Mayo.", "tipo": "llegar_aeropuerto", "destino": "Campo de Mayo"},
		],
	},
	{
		"nombre": "Turistas a Ezeiza (Cessna)",
		"descripcion": "Un grupo de turistas llegó en vuelo de cabotaje a Aeroparque y tiene que conectar su vuelo internacional en Ezeiza. Llevalos en el Cessna antes de que lo pierdan.",
		"origen": COORD_AEROPARQUE,
		"precio": 100,
		"pasos": [
			{"nombre": "Llevar turistas a Ezeiza", "texto": "Pasajeros a bordo. Próxima escala: Ezeiza, conexión internacional.", "tipo": "llegar_aeropuerto", "destino": "Ezeiza"},
		],
	},
	{
		"nombre": "Pasajero VIP a San Fernando",
		"descripcion": "Un pasajero muy importante necesita llegar a San Fernando sin demoras. Vuelo corto, pero no podés fallar.",
		"origen": COORD_AEROPARQUE,
		"precio": 150,
		"pasos": [
			{"nombre": "Llevar al VIP a San Fernando", "texto": "Pasajero VIP a bordo. Destino: San Fernando.", "tipo": "llegar_aeropuerto", "destino": "San Fernando"},
		],
	},
	{
		"nombre": "Capitán militar a Morón",
		"descripcion": "Un capitán del Ejército tiene que presentarse en la guarnición de Morón. Transporte prioritario.",
		"origen": COORD_AEROPARQUE,
		"precio": 130,
		"pasos": [
			{"nombre": "Llevar al capitán a Morón", "texto": "Oficial a bordo. Destino: guarnición de Morón.", "tipo": "llegar_aeropuerto", "destino": "Morón"},
		],
	},
	{
		"nombre": "Recorrido de entrega (con escalas)",
		"descripcion": "Entregas pendientes en tres paradas: El Palomar, Ezeiza, y de vuelta a Aeroparque -- esta vez no alcanza con aterrizar, hay que carretear hasta el Hangar 5 para cerrar la entrega.",
		"origen": COORD_AEROPARQUE,
		"precio": 300,
		"pasos": [
			{"nombre": "Escala en El Palomar", "texto": "RECORRIDO DE ENTREGA\nPrimera escala: El Palomar.", "tipo": "llegar_aeropuerto", "destino": "El Palomar"},
			{"nombre": "Escala en Ezeiza", "texto": "Segunda escala: Ezeiza.", "tipo": "llegar_aeropuerto", "destino": "Ezeiza"},
			{"nombre": "Volver a Aeroparque y estacionar en el Hangar 5", "texto": "Última escala: volvé a Aeroparque y estacioná en el Hangar 5.", "tipo": "llegar_punto", "lat": COORD_HANGAR_5_AEROPARQUE["lat"], "lon": COORD_HANGAR_5_AEROPARQUE["lon"], "radio": 80.0},
		],
	},
	# Pedido 2026-10-17 ("ahora las misiones van de hangar en hangar -- son
	# lugares espectaculares, no solo para aterrizar, también para
	# carretear hasta el lugar justo"): usan los hangares que fue trackeando
	# con la tecla M en cada aeropuerto -- la flecha (ver _actualizar_
	# marcador_destino_mision) ya apunta sola al hangar de destino.
	{
		"nombre": "Aeroparque → San Fernando (hangar)",
		"descripcion": "Salís del Hangar 5 de Aeroparque con una entrega para San Fernando. Estacioná en el hangar del medio.",
		"origen": COORD_HANGAR_5_AEROPARQUE,
		"precio": 160,
		"pasos": [
			{"nombre": "Ir al Hangar 2 de San Fernando", "texto": "Destino: Hangar 2, San Fernando.", "tipo": "llegar_punto", "lat": -34.4574557225892, "lon": -58.5888192459131, "radio": 80.0},
		],
	},
	{
		"nombre": "Ezeiza → Morón (hangar)",
		"descripcion": "Salís del Hangar 1 de Ezeiza. Hay que llevar el avión hasta el hangar de Morón, el que está al lado del Museo Nacional de Aeronáutica.",
		"origen": {"lat": -34.8133828902489, "lon": -58.536929830955},
		"precio": 180,
		"pasos": [
			{"nombre": "Ir al Hangar 1 de Morón", "texto": "Destino: Hangar 1, Morón.", "tipo": "llegar_punto", "lat": -34.6736493934052, "lon": -58.6376924863747, "radio": 80.0},
		],
	},
	{
		"nombre": "San Fernando → El Palomar (hangar)",
		"descripcion": "Salís del Hangar 3 de San Fernando (el de la salida directa a pista) con destino El Palomar.",
		"origen": {"lat": -34.4562481120316, "lon": -58.5861169810681},
		"precio": 140,
		"pasos": [
			{"nombre": "Ir al Hangar 2 de El Palomar", "texto": "Destino: Hangar 2, El Palomar.", "tipo": "llegar_punto", "lat": -34.6134659644805, "lon": -58.604843224449, "radio": 80.0},
		],
	},
	{
		"nombre": "Ezeiza VIP → Campo de Mayo (hangar)",
		"descripcion": "Salís del Hangar VIP de Ezeiza con un pasajero militar. Destino: Campo de Mayo, el hangar de la Torre de Control.",
		"origen": {"lat": -34.8153439965883, "lon": -58.5389747244369},
		"precio": 170,
		"pasos": [
			{"nombre": "Ir al Hangar Torre de Control de Campo de Mayo", "texto": "Destino: Hangar Torre de Control, Campo de Mayo.", "tipo": "llegar_punto", "lat": -34.5348325875828, "lon": -58.6749675029003, "radio": 80.0},
		],
	},
]

# Panel "Torre" (pedido 2026-09-27): muestra los 3 aeropuertos más cercanos
# con distancia real y permite prender/apagar el ILS de cada uno por
# separado, sin usar el interruptor global. _filas_torre se arma UNA vez en
# _ready() con referencias a los nodos de la escena; _actualizar_panel_torre()
# solo actualiza texto/rotación/metadata cada tanto (ver INTERVALO_TORRE).
var _filas_torre: Array = []
# Pedido 2026-10-17 ("encadenar viajes desde los aeropuertos cercanos, como
# ir parando el colectivo"): precio editable por aeropuerto, guardado por
# NOMBRE (no por índice de fila, que cambia cada refresco al reordenarse por
# distancia) -- así el ± que toques se mantiene mientras ese aeropuerto siga
# en la lista, en vez de volver a calcularse de cero cada 2 segundos.
var _precios_torre_override: Dictionary = {}
const TARIFA_TORRE_POR_KM := 0.55  # dólares por km, aproximado (pedido: "calculalo más o menos por distancia")
# Pedido explícito 2026-09-27: activar un ILS individual desde el panel
# Torre pisa el rumbo sugerido ("Ponete en rumbo X°"), apuntando a ESE
# aeropuerto en vez de al plan de vuelo Origen/Destino. Se apaga solo si
# se vuelve a apretar el mismo botón (apagándolo) -- ahí vuelve al plan de
# vuelo de siempre.
var rumbo_guia_override_activo: bool = false
var rumbo_guia_override_lat: float = 0.0
var rumbo_guia_override_lon: float = 0.0
var rumbo_guia_override_nombre: String = ""
var _acumulador_torre: float = 999.0
const INTERVALO_TORRE = 0.5  # bajado de 2.0 (pedido 2026-09-27, "paso por arriba y la distancia no se actualizó")
@onready var cartel_central: Label = get_node("../HUD/CartelCentral")
@onready var origen_option: OptionButton = get_node("../HUD/SelectorVuelo/VBox/OrigenOption")
@onready var destino_option: OptionButton = get_node("../HUD/SelectorVuelo/VBox/DestinoOption")
@onready var label_hasta: Label = get_node("../HUD/SelectorVuelo/VBox/LabelHasta")
@onready var check_vuelo_libre: CheckBox = get_node("../HUD/SelectorVuelo/VBox/CheckVueloLibre")
@onready var boton_confirmar: Button = get_node("../HUD/SelectorVuelo/VBox/BotonConfirmar")
@onready var tipo_avion_option: OptionButton = get_node("../HUD/SelectorVuelo/VBox/TipoAvionOption")
@onready var modelo_externo: Node3D = get_node("ModeloExterno")
@onready var pieza_fuselaje: MeshInstance3D = get_node("Fuselaje")
@onready var pieza_nariz: MeshInstance3D = get_node("Nariz")
@onready var pieza_alas: MeshInstance3D = get_node("Alas")
@onready var pieza_cola: MeshInstance3D = get_node("Cola")
@onready var pieza_timon: MeshInstance3D = get_node("Timon")

# Luces de navegación del avioncito propio (pedido 2026-09-26, "está muy
# negro, lo pierdo... que se vea hermoso") -- por ahora solo en este modelo
# (los otros 4 son GLB externos, cada uno necesitaría su propio ajuste de
# posición). Convención real de aviación: punta de ala IZQUIERDA = roja,
# DERECHA = verde, siempre fijas -- más una estroboscópica blanca arriba de
# todo (parpadeo corto, no suave) y una luz que ilumina el ala como en los
# aviones reales.
var luz_punta_ala_izq: MeshInstance3D
var luz_punta_ala_der: MeshInstance3D
var luz_estroboscopica: MeshInstance3D
var luz_iluminacion_ala: SpotLight3D
var luz_iluminacion_ala_2: SpotLight3D
var _tiempo_estrobo: float = 0.0

# Estroboscópica del modelo EXTERNO actual (glb/fbx elegido), si hay uno
# cargado -- se recrea cada vez que se cambia de avión en _aplicar_tipo_avion
# (pedido 2026-09-26: "esas mismas luces agregalas a todos los aviones,
# porque de noche si no es el avioncito tuyo todos se ven como una cosa
# negra"). null cuando el avión elegido es el primitivo "Avioncito clásico".
var luz_estroboscopica_externa: MeshInstance3D = null
# Editor de luces en vivo (pedido explícito 2026-10-01, "para no tener que
# renegar ajustando luces a ciegas en cada avión nuevo"): guarda referencia a
# las 3 lucecitas del avión externo actual para poder agarrarlas con el mouse
# y arrastrarlas -- se "clavan" apretando un botón, que escribe las
# coordenadas finales (en espacio LOCAL del modelo, mismo formato que
# "luces_manual") a un archivo de texto para copiar directo a TIPOS_AVION.
var luz_navegacion_izq_externa: MeshInstance3D = null
var luz_navegacion_der_externa: MeshInstance3D = null
# Pedido 2026-10-16 (A380: "una luz más grande que la otra"): si el avión trae
# "luces_mismo_tamano", la roja y la verde se igualan cada cuadro a como se
# ve la MÁS LEJANA (la más chica) -- ver _igualar_tamano_luces_navegacion.
var luces_igualar_tamano: bool = false

# ============================================================
# MODO TIERRA / ATERRIZAJE (pedido 2026-10-16)
# ============================================================
# Estado "en tierra": el avión se apoya en las RUEDAS (no en el centro), queda
# nivelado, gira como un auto y no se hunde ni entierra el ala. Todos los
# valores son editables por avión desde la pestaña "Aterrizaje" (ver
# _crear_solapa_aterrizaje) y se guardan en disco.
const RUTA_CONFIG_ATERRIZAJE = "user://aterrizaje_aviones.cfg"
const GRAVEDAD_REBOTE = 9.8
const ALTURA_ARRANQUE_VUELO = 1800.0   # metros sobre el aeropuerto de origen al apretar Empezar (pedido 2026-10-22: subido de 1000 -- más altura = más tiempo para que Cesium cargue las baldosas antes de estar cerca del piso. Pendiente: algún día dejar elegir "desde la pista" para el que lo prefiera)
var config_aterrizaje: Dictionary = {}
var en_tierra: bool = false

# Pedido 2026-10-17 ("bajar del avión, un cuadradito que camino al ras del
# piso, y voy trackeando una ruta a mano"): en vez de crear un Node3D nuevo
# con su propia cámara/colisión/recentrado de origen (reinventar todo lo que
# YA funciona para el avión), se reusa este mismo nodo (el avión) como
# "portador" -- se le esconden los mallados de avión, se le muestra un cubo
# chico, y mientras modo_a_pie esté activo un movimiento distinto (caminar,
# sin banco/cabeceo) lo controla en vez de la física de vuelo. Todo lo demás
# (cámara, recentrado de origen, lat/lon, minimapa) sigue funcionando gratis
# porque sigue siendo el mismo global_position de siempre.
var modo_a_pie: bool = false
var figura_a_pie: Node3D = null
var _pose_avion_antes_de_bajar: Transform3D = Transform3D.IDENTITY
const VELOCIDAD_A_PIE_MAXIMA := 55.56  # m/s (200 km/h, pedido explícito: "para trackear rápido", no para manejar realista)
const ACELERACION_A_PIE := 8.0         # m/s², rampa como el avión, no instantáneo
const FRENADO_A_PIE := 12.0            # m/s² al frenar a propósito
const VELOCIDAD_REVERSA_A_PIE := 4.0  # m/s, marcha atrás lenta a propósito (no hace falta mantener velocidad en reversa)
var velocidad_a_pie_actual: float = 0.0
const VELOCIDAD_GIRO_A_PIE := 40.0    # grados/segundo (pedido 2026-10-22, "muy brusco" -- bajado de 60; con joystick analógico además se escala por cuánto empujás el stick)
const ALTURA_FIGURA_A_PIE := 0.75     # m sobre el piso real (mitad de la altura aprox. del auto)
var boton_bajar_subir: Button = null

# Pedido 2026-10-17 ("reemplazá el cuadradito por un auto -- el de policía,
# el más liviano de los 3 que bajé"): mismo truco de autoescalar + recentrar
# pivote que ya usamos para el A380/los barcos (los modelos de Sketchfab
# vienen en escalas y pivotes random, nunca "1 metro = 1 unidad" ni
# centrados) -- ver _crear_modelo_auto más abajo.
const MODELO_AUTO := "res://modelos_vehiculos/police_car.glb"
const LARGO_AUTO_REAL := 5.52  # metros (pedido 2026-10-22: "agrandarlo un 20%" -- era 4.6)

# Ruta trackeada a mano (pedido explícito: "prefiero ir trackeando yo, para
# que no queden rutas falsas" -- un botón asignable marca el punto actual,
# otro borra el último por si se aprieta de más). Se guarda en su PROPIO
# archivo, separado de lugares_marcados.json (eso son lugares puntuales con
# nombre; esto es una lista ordenada de puntos de un camino).
const RUTA_TRACKEADA_ARCHIVO = "res://ruta_trackeada.json"
var _ruta_trackeada: Array = []
var _marcar_ruta_anterior: bool = false
var _deshacer_ruta_anterior: bool = false
var bloqueo_despegue: float = 0.0       # después de tocar, no se puede despegar de golpe (evita el "escupido" al aire)
var tiempo_en_aire: float = 0.0        # segundos seguidos bien separado del piso: sin esto no cuenta como aterrizaje
var tiempo_sin_toque: float = 0.0       # después de despegar, no se vuelve a detectar un "toque" por un rato
var rebote_activo: bool = false
var rebote_altura: float = 0.0
var rebote_velocidad: float = 0.0
var rebote_velocidad_inicial: float = 0.0
var rebotes_restantes: int = 0
var altura_tren_auto: float = 0.3       # medida del modelo (ver _medir_altura_tren)
var _altura_sobre_piso_prev: float = -1.0
# BUG REAL encontrado 2026-10-17 (diagnóstico de Pablo, confirmado por el
# código: "frenás en el aeroparque y de golpe acelera a 150" -- 150 era su
# velocidad mínima de VUELO configurada para ese avión, no un freno). El
# "salto de teletransporte" de más abajo sacaba del modo tierra con un
# solo cuadro ruidoso del rayo al piso (Aeroparque es la zona con más
# objetos cargados a la vez -- ver feedback_cuidar_espacio_disco -- así que
# es donde más probable es que el terreno de Cesium tiemble un cuadro).
# Apenas eso pasaba, el freno (pensado para el piso=0 en tierra) pasaba a
# usar el piso de VUELO (vel_minima) -- y como la velocidad real taxeando
# es menor a eso, "frenar" empujaba la velocidad PARA ARRIBA de golpe.
# Este contador exige que el salto dure más de un cuadro antes de creérselo.
var _tiempo_altura_sospechosa: float = 0.0
var cartel_aterrizaje: Label = null
var cartel_aterrizaje_tiempo: float = 0.0
var panel_aterrizaje_tab: Panel = null
var boton_solapa_6: Button = null
var panel_mision_tab: Panel = null
var boton_solapa_8: Button = null
var panel_destinos_tab: Panel = null
var boton_solapa_7: Button = null
var valores_aterrizaje_labels: Dictionary = {}
var boton_modo_tierra: Button = null

# MODO GUERRA (pedido 2026-10-16): los blancos (cuadrados rojos de los aeropuertos,
# verdes de Capital, barcos, industria, invasores) y sus contadores solo existen
# en modo guerra. En modo normal quedan ocultos Y SIN COLISION -- tenían
# colisión en medio de la pista y el rayo del piso los tomaba como suelo.
var modo_guerra: bool = false
var boton_modo_guerra: Button = null
var panel_modo_guerra_niveles: Panel = null
var _tiempo_sync_guerra: float = 0.0
var modo_editar_luces: bool = false
var luz_arrastrada: MeshInstance3D = null
var profundidad_arrastre_luz: float = 0.0
const RUTA_AJUSTES_LUCES_DEBUG = "res://ajustes_luces_debug.txt"
@onready var boton_despegar: Button = get_node("../HUD/BotonDespegar")
@onready var flecha_izquierda: Label = get_node("../HUD/FlechaIzquierda")
@onready var flecha_derecha: Label = get_node("../HUD/FlechaDerecha")
@onready var flecha_arriba: Label = get_node("../HUD/FlechaArriba")
@onready var flecha_abajo: Label = get_node("../HUD/FlechaAbajo")
@onready var etiqueta_rumbo_objetivo: Label = get_node("../HUD/LabelRumboObjetivo")
@onready var mundo = get_node("..")
@onready var camara: Camera3D = get_node("../CamaraJuego")
@onready var etiqueta_velocidad: Label = get_node("../HUD/PanelInstrumentos/CajaVelocidad/LabelVelocidad")
@onready var boton_misiones: Button = get_node("../HUD/BarraBotones/BotonMisiones")
@onready var panel_misiones: Panel = get_node("../HUD/PanelMisiones")
@onready var lista_misiones: VBoxContainer = get_node("../HUD/PanelMisiones/VBoxMisiones/ScrollMisiones/ListaMisiones")
@onready var boton_cerrar_misiones: Button = get_node("../HUD/PanelMisiones/VBoxMisiones/BotonCerrarMisiones")

# Ayuda visual (pedido 2026-09-20): una línea fina en el mundo 3D que marca
# el rumbo hacia el objetivo -- PURAMENTE VISUAL, no toca para nada el
# cálculo de rumbo/flechas/distancia (que ya cuesta mucho lograr que
# funcione bien) -- solo LEE rumbo_objetivo, nunca lo modifica.
@onready var boton_ayuda_visual: Button = get_node("../HUD/BarraBotones/BotonAyudaVisual")
# Lucecita roja de estado (pedido 2026-09-21, "que se note bien cuando está
# activada y se apague cuando no") -- al lado del botón, prendida/apagada.
@onready var luz_ayuda_visual: ColorRect = get_node("../HUD/BarraBotones/LuzAyudaVisual")
const COLOR_LUZ_ENCENDIDA = Color(1.0, 0.15, 0.15, 1.0)
const COLOR_LUZ_APAGADA = Color(0.25, 0.25, 0.25, 1.0)

# Radio (pedido 2026-09-21): botón ON/OFF -- el pedido original -- que prende
# y apaga en loop el MP3 de ambiente de cabina que grabó el usuario de un
# video de YouTube ("radio_fondo.mp3" en FX/, puesto ahí a mano por él).
# Aparte, como pedido extra, hay un segundo botón chico que abre la radio
# Aspen 102.3 EN VIVO de verdad, pero afuera del juego (con el navegador/
# reproductor de la computadora) -- meter un stream de internet en vivo
# DENTRO de Godot es mucho más laburo (no viene de fábrica, hay que armar la
# descarga en vivo a mano), así que por ahora queda como link externo aparte
# del botón de radio de ambiente. Link encontrado inspeccionando el
# reproductor oficial de fmaspen.com (usa la infraestructura StreamTheWorld).
const URL_RADIO_ASPEN = "https://playerservices.streamtheworld.com/api/livestream-redirect/ASPENAAC.aac"
@onready var boton_radio: Button = get_node("../HUD/BarraBotones/BotonRadio")
@onready var boton_radio_en_vivo: Button = get_node("../HUD/BarraBotones/BotonRadioEnVivo")
@onready var sonido_radio: AudioStreamPlayer = get_node("SonidoRadio")
var radio_activa: bool = false
# Aviso de seguridad automático al minuto de vuelo (pedido explícito
# 2026-10-01, "que active el sonido de azafata, así el que no sabe jugar
# dice uh, mirá, tiene esos sonidos"). Detecta el cambio de estado a VOLANDO
# de forma genérica en _process (en vez de tocar los 4 lugares del código
# donde arranca un vuelo nuevo), y dispara una sola vez por vuelo.
var _estado_anterior_para_timer_vuelo = null
var tiempo_vuelo_actual: float = 0.0
var anuncio_seguridad_reproducido: bool = false
const SEGUNDOS_ANUNCIO_SEGURIDAD = 60.0

# Selector de emisoras de radio (pedido 2026-09-27, "un selector tipo con la
# música" -- antes había un solo archivo fijo de fondo). Mismo patrón que
# CARPETAS_MUSICA/playlist_musica: escanea la carpeta, arma una lista para
# elegir, y guarda la última emisora elegida para la próxima partida.
const CARPETA_RADIO = "res://FX/Radio"
const RUTA_CONFIG_RADIO = "user://radio_config.cfg"
const EMISORA_POR_DEFECTO = "Córdoba Torre (SACO)"  # pedido explícito, "que quede predeterminado el de Córdoba"
@onready var panel_radio: Panel = get_node("../HUD/PanelRadio")
@onready var boton_emisoras: Button = get_node("../HUD/BarraBotones/BotonEmisoras")
@onready var lista_emisoras_radio: VBoxContainer = get_node("../HUD/PanelRadio/VBoxRadio/ScrollEmisoras/ListaEmisoras")
@onready var boton_detener_radio: Button = get_node("../HUD/PanelRadio/VBoxRadio/BotonDetenerRadio")
@onready var boton_cerrar_radio: Button = get_node("../HUD/PanelRadio/VBoxRadio/BotonCerrarRadio")
var playlist_radio: Array = []
var ruta_emisora_actual: String = ""

# Audio de instrucciones de seguridad (pedido 2026-09-27) -- a propósito
# MUCHO más simple que música/radio: un solo archivo fijo, play/stop nada
# más, sin selector. "Cuando encontremos uno mejor, directamente
# reemplazamos el archivo" -- por eso no hay lista, solo se busca el primer
# .mp3 que haya en la carpeta.
const CARPETA_INSTRUCCIONES = "res://FX/Instrucciones"
@onready var boton_instrucciones: Button = get_node("../HUD/BarraBotones/BotonInstrucciones")
@onready var sonido_instrucciones: AudioStreamPlayer = get_node("SonidoInstrucciones")

# Botón para salir de pantalla completa sin depender de ESCAPE (pedido
# 2026-09-21, la tecla no le funcionaba en su notebook).
@onready var boton_salir_pantalla_completa: Button = get_node("../HUD/BotonSalirPantallaCompleta")

# Música por estilo (pedido 2026-09-20/21): las carpetas Música/<Estilo> ya
# tenían 10 temas cada una desde la vez pasada, pero no había ningún botón
# para escucharlos -- esto arma una lista al azar de la carpeta elegida y la
# va reproduciendo sola, tema tras tema, en loop (se reordena de nuevo al
# terminar la vuelta completa).
@onready var boton_musica: Button = get_node("../HUD/BarraBotones/BotonMusica")
# ILS visual, prueba 2026-09-21 con datos reales de cabecera de pista (El
# Palomar y Mariano Moreno, ver mundo.gd) -- botón simple, prende/apaga todos
# los ILS confirmados a la vez.
@onready var boton_ils: Button = get_node("../HUD/BarraBotones/BotonILS")
var ils_activo: bool = false
# Cartel de aviso parpadeante (pedido 2026-09-21, "en ningún momento me
# avisó que tenía que activar el ILS, cuando vi los aros ya era tarde"):
# aparece cuando el destino actual tiene ILS real confirmado (tiene la meta
# "orientacion_usuario") y ya estás lo bastante cerca como para que los aros
# existan, pero todavía no activaste el ILS -- así lo ves ANTES de estar
# encima de los aros, no cuando ya es tarde para alinearte desde la punta.
@onready var label_sugerencia_ils: Label = get_node("../HUD/LabelSugerenciaILS")
var _tiempo_sugerencia_ils: float = 0.0
var _mostrar_sugerencia_ils: bool = false
@onready var panel_musica: Panel = get_node("../HUD/PanelMusica")
@onready var label_estado_musica: Label = get_node("../HUD/PanelMusica/VBoxMusica/LabelEstadoMusica")
@onready var boton_genero_ambiental: Button = get_node("../HUD/PanelMusica/VBoxMusica/BotonGeneroAmbiental")
@onready var boton_genero_orquestal: Button = get_node("../HUD/PanelMusica/VBoxMusica/BotonGeneroOrquestal")
@onready var boton_genero_rock: Button = get_node("../HUD/PanelMusica/VBoxMusica/BotonGeneroRock")
@onready var boton_genero_electronica: Button = get_node("../HUD/PanelMusica/VBoxMusica/BotonGeneroElectronica")
@onready var scroll_lista_temas: ScrollContainer = get_node("../HUD/PanelMusica/VBoxMusica/ScrollListaTemas")
@onready var lista_temas_musica: VBoxContainer = get_node("../HUD/PanelMusica/VBoxMusica/ScrollListaTemas/ListaTemas")
@onready var boton_anterior_musica: Button = get_node("../HUD/PanelMusica/VBoxMusica/HBoxControlesMusica/BotonAnteriorMusica")
@onready var boton_pausa_musica: Button = get_node("../HUD/PanelMusica/VBoxMusica/HBoxControlesMusica/BotonPausaMusica")
@onready var boton_siguiente_musica: Button = get_node("../HUD/PanelMusica/VBoxMusica/HBoxControlesMusica/BotonSiguienteMusica")
@onready var boton_detener_musica: Button = get_node("../HUD/PanelMusica/VBoxMusica/BotonDetenerMusica")
@onready var boton_cerrar_musica: Button = get_node("../HUD/PanelMusica/VBoxMusica/BotonCerrarMusica")
@onready var boton_cerrar_musica_x: Button = get_node("../HUD/PanelMusica/BotonCerrarMusicaX")
@onready var sonido_musica: AudioStreamPlayer = get_node("SonidoMusica")
const CARPETAS_MUSICA = {
	"Ambiental": "res://Música/Ambiental",
	"Orquestal": "res://Música/Orquestal",
	"Rock": "res://Música/Rock",
	"Electronica": "res://Música/Electronica",
}
var playlist_musica: Array = []
var indice_musica: int = 0
var genero_musica_actual: String = ""

# Sonido de motor (pedido 2026-09-21): un loop por FAMILIA de avión (hélice /
# jet / helicóptero, ver "sonido" en TIPOS_AVION) en vez de uno por modelo --
# el usuario aclaró explícitamente que no importa si no es 100% el sonido
# exacto de cada avión mientras no se note demasiado. El volumen tiene su
# propia perilla en Configuración ("Sonido motores", NO es un efecto de FX
# genérico) y el tono (pitch) sube un poco con la velocidad para dar
# sensación de aceleración, sin que quede estruendoso.
@onready var sonido_motor: AudioStreamPlayer = get_node("SonidoMotor")
@onready var slider_volumen_motor: HSlider = get_node("../HUD/PanelSonidos/ScrollSonidos/VBoxSonidos/HBoxVolumenMotor/SliderVolumenMotor")
# Pedido 2026-09-27 ("en pantalla completa es un lío bajar el volumen de
# Windows"): sliders propios para música y radio, mismo patrón que el de
# motor de arriba.
@onready var slider_volumen_musica: HSlider = get_node("../HUD/PanelSonidos/ScrollSonidos/VBoxSonidos/HBoxVolumenMusica/SliderVolumenMusica")
@onready var slider_volumen_radio: HSlider = get_node("../HUD/PanelSonidos/ScrollSonidos/VBoxSonidos/HBoxVolumenRadio/SliderVolumenRadio")
@onready var slider_volumen_instrucciones: HSlider = get_node("../HUD/PanelSonidos/ScrollSonidos/VBoxSonidos/HBoxVolumenInstrucciones/SliderVolumenInstrucciones")
const RUTA_CONFIG_AUDIO = "user://audio_config.cfg"
const VOLUMEN_MOTOR_DB_MINIMO = -40.0  # con el slider en 0, casi inaudible en vez de mudo de golpe
const VOLUMEN_MOTOR_DB_MAXIMO = -6.0   # con el slider al máximo, presente pero no estruendoso
const VOLUMEN_MUSICA_DB_MINIMO = -40.0
const VOLUMEN_MUSICA_DB_MAXIMO = 0.0
const VOLUMEN_RADIO_DB_MINIMO = -40.0
const VOLUMEN_RADIO_DB_MAXIMO = 0.0
const VOLUMEN_INSTRUCCIONES_DB_MINIMO = -40.0
const VOLUMEN_INSTRUCCIONES_DB_MAXIMO = 0.0
const VOLUMEN_DISPARO_DB_MINIMO = -20.0
const VOLUMEN_DISPARO_DB_MAXIMO = 10.0
const VOLUMEN_EXPLOSION_DB_MINIMO = -25.0
const VOLUMEN_EXPLOSION_DB_MAXIMO = 8.0
var volumen_motor: float = 0.5  # 0..1, lo que muestra/mueve el slider
# Volumen de efectos de armamento, separado de los demás (pedido 2026-10-07:
# "las balas están muy bajitas y las explosiones muy altas, un slider para
# cada uno"). Arrancan en valores distintos a 0.5 porque ya sabemos para qué
# lado está desbalanceado cada uno por defecto.
var volumen_disparo: float = 0.7
var volumen_explosion: float = 0.35
var slider_volumen_disparo: HSlider = null
var slider_volumen_explosion: HSlider = null
var volumen_musica: float = 0.7
var volumen_radio: float = 0.5  # arranca más bajo (antes sonaba fijo a -12db)
var volumen_instrucciones: float = 0.4  # pedido explícito: "está muy fuerte"
const PITCH_MOTOR_MINIMO = 0.85
const PITCH_MOTOR_MAXIMO = 1.25
@onready var linea_guia: MeshInstance3D = get_node("../LineaGuia")
@onready var label_giro_valor: Label = get_node("../HUD/PanelSensibilidad/ScrollSensibilidad/VBoxSensibilidad/FilaGiro/LabelGiroValor")
@onready var boton_giro_menos: Button = get_node("../HUD/PanelSensibilidad/ScrollSensibilidad/VBoxSensibilidad/FilaGiro/BotonGiroMenos")
@onready var boton_giro_mas: Button = get_node("../HUD/PanelSensibilidad/ScrollSensibilidad/VBoxSensibilidad/FilaGiro/BotonGiroMas")
@onready var label_angulo_valor: Label = get_node("../HUD/PanelSensibilidad/ScrollSensibilidad/VBoxSensibilidad/FilaAngulo/LabelAnguloValor")
@onready var boton_angulo_menos: Button = get_node("../HUD/PanelSensibilidad/ScrollSensibilidad/VBoxSensibilidad/FilaAngulo/BotonAnguloMenos")
@onready var boton_angulo_mas: Button = get_node("../HUD/PanelSensibilidad/ScrollSensibilidad/VBoxSensibilidad/FilaAngulo/BotonAnguloMas")
@onready var label_vertical_titulo: Label = get_node("../HUD/PanelSensibilidad/ScrollSensibilidad/VBoxSensibilidad/FilaVertical/LabelVertical")
@onready var label_vertical_valor: Label = get_node("../HUD/PanelSensibilidad/ScrollSensibilidad/VBoxSensibilidad/FilaVertical/LabelVerticalValor")
@onready var boton_vertical_menos: Button = get_node("../HUD/PanelSensibilidad/ScrollSensibilidad/VBoxSensibilidad/FilaVertical/BotonVerticalMenos")
@onready var boton_vertical_mas: Button = get_node("../HUD/PanelSensibilidad/ScrollSensibilidad/VBoxSensibilidad/FilaVertical/BotonVerticalMas")
@onready var label_vel_minima_valor: Label = get_node("../HUD/PanelSensibilidad/ScrollSensibilidad/VBoxSensibilidad/FilaVelMinima/LabelVelMinimaValor")
@onready var boton_vel_minima_menos: Button = get_node("../HUD/PanelSensibilidad/ScrollSensibilidad/VBoxSensibilidad/FilaVelMinima/BotonVelMinimaMenos")
@onready var boton_vel_minima_mas: Button = get_node("../HUD/PanelSensibilidad/ScrollSensibilidad/VBoxSensibilidad/FilaVelMinima/BotonVelMinimaMas")
@onready var label_vel_maxima_valor: Label = get_node("../HUD/PanelSensibilidad/ScrollSensibilidad/VBoxSensibilidad/FilaVelMaxima/LabelVelMaximaValor")
@onready var boton_vel_maxima_menos: Button = get_node("../HUD/PanelSensibilidad/ScrollSensibilidad/VBoxSensibilidad/FilaVelMaxima/BotonVelMaximaMenos")
@onready var boton_vel_maxima_mas: Button = get_node("../HUD/PanelSensibilidad/ScrollSensibilidad/VBoxSensibilidad/FilaVelMaxima/BotonVelMaximaMas")
@onready var label_velocidad_animacion_valor: Label = get_node("../HUD/PanelSensibilidad/ScrollSensibilidad/VBoxSensibilidad/FilaVelocidadAnimacion/LabelVelocidadAnimacionValor")
@onready var boton_velocidad_animacion_menos: Button = get_node("../HUD/PanelSensibilidad/ScrollSensibilidad/VBoxSensibilidad/FilaVelocidadAnimacion/BotonVelocidadAnimacionMenos")
@onready var boton_velocidad_animacion_mas: Button = get_node("../HUD/PanelSensibilidad/ScrollSensibilidad/VBoxSensibilidad/FilaVelocidadAnimacion/BotonVelocidadAnimacionMas")
@onready var label_viento_valor: Label = get_node("../HUD/PanelSensibilidad/ScrollSensibilidad/VBoxSensibilidad/FilaViento/LabelVientoValor")
@onready var boton_viento_menos: Button = get_node("../HUD/PanelSensibilidad/ScrollSensibilidad/VBoxSensibilidad/FilaViento/BotonVientoMenos")
@onready var boton_viento_mas: Button = get_node("../HUD/PanelSensibilidad/ScrollSensibilidad/VBoxSensibilidad/FilaViento/BotonVientoMas")
var ayuda_visual_activa: bool = false
# CAMBIADO 2026-09-21 (pedido explícito, "es como un láser en los ojos,
# pegado al avión como una bandita elástica"): antes la línea salía siempre
# del avión, persiguiéndolo -- ahora es un tramo FIJO en el mundo real,
# desde donde estabas cuando se activó el destino/misión hasta el destino.
# Si te alejás a pasear, la línea se queda quieta esperando en su lugar.
var objetivo_guia_actual: Node3D = null
var lat_origen_guia: float = 0.0
var lon_origen_guia: float = 0.0
const ALTURA_LINEA_GUIA = 300.0
# Cache del destino actual para la línea guía -- ver _actualizar_linea_guia_frame().
var _hay_destino_para_linea_guia: bool = false
var _lat_destino_linea_guia: float = 0.0
var _lon_destino_linea_guia: float = 0.0

# Marcar lugar (pedido 2026-09-20) -- el jugador vuela con el helicóptero,
# aterriza donde ve un helipuerto/lugar de interés real (a simple vista,
# porque nuestras coordenadas son aproximadas), aprieta el botón y le pone
# nombre -- así se va armando una base de datos de lugares reales sin tener
# que ir a buscar coordenadas a mano en Google Maps cada vez.
@onready var panel_marcar_lugar: Panel = get_node("../HUD/PanelMarcarLugar")
@onready var etiqueta_coordenadas_marcar_lugar: Label = get_node("../HUD/PanelMarcarLugar/VBoxMarcarLugar/LabelCoordenadasMarcarLugar")
@onready var campo_nombre_lugar: LineEdit = get_node("../HUD/PanelMarcarLugar/VBoxMarcarLugar/CampoNombreLugar")
@onready var boton_guardar_lugar: Button = get_node("../HUD/PanelMarcarLugar/VBoxMarcarLugar/HBoxBotonesMarcarLugar/BotonGuardarLugar")
@onready var boton_cancelar_lugar: Button = get_node("../HUD/PanelMarcarLugar/VBoxMarcarLugar/HBoxBotonesMarcarLugar/BotonCancelarLugar")
const RUTA_LUGARES_MARCADOS = "res://lugares_marcados.json"
var lugares_marcados: Array = []
var lat_lugar_pendiente: float = 0.0
var lon_lugar_pendiente: float = 0.0
var alt_lugar_pendiente: float = 0.0
var marcar_lugar_anterior: bool = false
# Última lectura de rumbo (cacheada en _actualizar_torre) -- la usa "Marcar
# lugar" para guardar la orientación real de una pista al aterrizar ahí.
var _ultimo_rumbo_actual: float = 0.0

# Freno de emergencia (pedido 2026-09-21): con la velocidad máxima ahora en
# 1500, frenar de a poco con S se queda corto para no pasarse de un destino.
# Tecla D ("Danger") o botón de joystick -- de un solo golpe (no mantenido,
# por eso necesita detección de flanco como marcar_lugar) baja la velocidad
# a upa a un piso de 200, NUNCA la sube -- si ya ibas más lento que eso, no
# hace nada. De ahí para abajo, el jugador frena a mano con S como siempre.
const VELOCIDAD_FRENO_EMERGENCIA = 200.0
var freno_emergencia_anterior: bool = false

# Freno progresivo -- ver comentario junto a freno_gradual_activo más abajo.
# AJUSTADO 2026-09-27 (reportado: "con dos apretadas ya llega a cero, queda
# clavado -- tiene que hacer falta un montón de apretadas para que sea
# rápido"): valores bajados bastante. Con 200 de velocidad y 2 apretadas
# (0.5+1.0=1.5/seg) tarda más de dos minutos en parar -- recién con muchas
# apretadas seguidas se acerca al tope y para rápido de verdad.
const FRENADO_GRADUAL_BASE = 0.5        # primera apretada: unidades/seg de frenado
const FRENADO_GRADUAL_INCREMENTO = 1.0  # cada apretada extra suma esto
const FRENADO_GRADUAL_MAXIMO = 20.0     # tope para que nunca sea instantáneo
# Modo tierra (pedido 2026-10-16): por encima de esta velocidad un toque de B frena
# fuerte hasta llegar acá (para bajar de 200 a 50 sin apretar 200 veces); de acá
# para abajo B vuelve a ser fino, de a poco.
const VEL_TIERRA_FINA = 50.0
var _freno_rapido_tierra: bool = false
var _tiempo_aviso_despegue: float = 0.0
var freno_gradual_anterior: bool = false
var intensidad_frenado_gradual: float = 0.0

# Debug del bug de rumbo (2026-09-20) -- N vuelca UNA línea con los números
# de este instante a un archivo de texto, en vez de imprimir en la consola
# todo el tiempo (imposible de copiar, se llenaba de números sin parar).
const RUTA_DEBUG_RUMBO = "res://debug_rumbo.log"
var _ultimo_debug_rumbo: String = ""
var debug_rumbo_anterior: bool = false
var tecla_ils_anterior: bool = false
var tecla_reset_mapa_anterior: bool = false

# Vista externa SIN el avión visible (pedido 2026-09-22, "no vamos a ver el
# avión... es como que uno ve lo que ve el que maneja") -- tecla V, mismo
# patrón de un solo golpe que M/N/I/D/U. Prueba chica y aislada 2026-09-25
# para descartar/confirmar si algo del ciclo día/noche quedó mal conectado
# (si esto tampoco se ve al probarlo, el problema es de Godot/build, no del
# código del ciclo día/noche).
var tecla_vista_sin_avion_anterior: bool = false
var vista_sin_avion_activa: bool = false
var boton_vista_avion: Button = null
var etiqueta_dinero: Label = null
var etiqueta_dinero_mp: Label = null

# Armamento de práctica (prototipo 2026-10-05, portado de la copia de
# ChatGPT): mira acumulativa por HUD + blancos fijos en 5 aeropuertos.
var armamento_anterior: bool = false
# Pedido 2026-10-11 ("disparo 10 veces en un segundo y medio, no le da
# tiempo a nada al barco"): cadencia de cañón del propio avión, separada
# del "recién apretado" de armamento_anterior -- aunque el gatillo
# dispare ediciones falsas/rápidas, no se puede disparar más seguido que
# esto. ~3 disparos por segundo, "tipo ametralladora" como pidió Pablo.
var tiempo_hasta_proximo_disparo_jugador: float = 0.0
const CADENCIA_DISPARO_JUGADOR := 0.35
var mira_hud: Control = null
var etiqueta_vida_objetivo: Label = null
# Pedido 2026-10-11 ("la mira arranca en el centro del avión, quiero que
# arranque perpendicular a la nariz, un poco para arriba por defecto"): un
# offset chico hacia arriba (Y negativo = arriba en coordenadas de
# pantalla) en vez de Vector2.ZERO -- el jugador la sigue pudiendo mover
# con el joystick como siempre, esto solo cambia el punto de partida.
var mira_hud_posicion: Vector2 = Vector2(0.0, -180.0)
var mira_feedback_tiempo: float = 0.0
var objetivos_armamento: Array[Node3D] = []
var sonido_disparo: AudioStreamPlayer = null
var sonido_disparo_archivo_real: bool = false
var sonido_explosion: AudioStreamPlayer = null
var etiqueta_objetivo_destruido: Label = null
var objetivos_destruidos: int = 0

# Misión verde (Capital Federal) y barquitos del río -- pedido 2026-10-06,
# prototipo aparte de los blancos de aeropuertos, con su propio contador.
var etiqueta_mision_verde: Label = null
var mision_verde_destruidos: int = 0
var sonido_mision_completada: AudioStreamPlayer = null
var objetivos_barcos: Array[Node3D] = []
var etiqueta_barcos: Label = null
var barcos_destruidos: int = 0
var sonido_explosion_barco: AudioStreamPlayer = null
var etiqueta_fuera_de_mira: Label = null
var panel_fuera_de_mira: PanelContainer = null

# Contraataque de los barcos (pedido 2026-10-11): un barco se "activa" con
# su primer impacto (ver bloque "sigue_con_vida" en _process) y empieza a
# tirar ráfagas de balas lentas y visibles hacia el avión, que SÍ se pueden
# esquivar cambiando de rumbo/altura (no son misiles que persiguen). Estado
# por barco guardado como meta ("rafaga_temporizador",
# "rafaga_balas_restantes") para no necesitar un Timer por barco.
var barcos_activados: Array[Node3D] = []
const CARGA_INICIAL_ATAQUE_BARCO := 0.6  # segundos de "aviso" antes del primer disparo
const RAFAGA_TAMANO_BARCO := 3           # cañonazos por ráfaga (pedido: que no sea un solo tiro fácil de esquivar)
const INTERVALO_ENTRE_BALAS_RAFAGA := 0.35  # segundos entre cada bala de la misma ráfaga -- cada una reapunta a la posición ACTUAL del avión
const ENFRIAMIENTO_ENTRE_RAFAGAS := 3.5     # segundos de pausa entre una ráfaga y la siguiente
var velocidad_bala_barco: float = 18.0  # m/s -- ajustable en vivo con los +/- del HUD (pedido explícito, "que lo pueda ir controlando yo")
const VELOCIDAD_BALA_BARCO_MIN := 5.0
const VELOCIDAD_BALA_BARCO_MAX := 60.0
const PASO_VELOCIDAD_BALA_BARCO := 5.0
const RADIO_IMPACTO_BALA_BARCO := 8.0    # metros -- distancia a la que se considera que la bala "te dio"
# BUG REAL encontrado 2026-10-11 (reportado: "nunca vi las balas, ni en
# aeropuertos ni con la H"): el límite anterior era por TIEMPO (12
# segundos), no por distancia -- con la bala lenta eso significaba un
# alcance real de apenas ~200m. La tecla H activaba barcos a 30+ km sin que
# nadie se diera cuenta de que la bala físicamente no podía llegar ni
# acercarse. Cambiado a un límite por DISTANCIA RECORRIDA, independiente
# de la velocidad (que además se puede ajustar en vivo con el HUD).
const ALCANCE_MAXIMO_BALA_BARCO := 6000.0  # metros
var balas_barcos: Array[Dictionary] = []  # cada entrada: {"nodo": MeshInstance3D, "direccion": Vector3, "distancia_recorrida": float}

# Vida del jugador (pedido 2026-10-11, "una barrita roja de 10 -- un solo
# tiro no me puede sacar del juego"). Por ahora solo baja; qué pasa al
# llegar a 0 lo definimos después (Pablo todavía no lo pidió).
var vida_jugador: int = 10
const VIDA_INICIAL_JUGADOR := 10
var segmentos_vida_jugador: Array[ColorRect] = []
var etiqueta_velocidad_invasion: Label = null

# INVASIÓN POR TIERRA -- boceto 2026-10-11 (idea de Pablo: "lo peor que te
# puede pasar no es que te bajen el avión, es que te invadan"). Un barco
# que llega a la costa sin ser hundido desembarca 5 unidades chicas que
# caminan por tierra hacia el aeropuerto más cercano SIN tomar. 5 tiros las
# bajan. Si una llega, el aeropuerto queda "tomado" (marrón, 40 de vida) y
# larga 5 unidades nuevas hacia otros aeropuertos libres. Tope duro de
# unidades vivas a la vez para que no se descontrole (reproducción sin
# límite = riesgo real de lag/crash). Sin balas de vuelta todavía -- esto
# es solo invasión por tierra, el jugador es la única amenaza para ellos.
var unidades_invasion: Array[Node3D] = []
# Pedido 2026-10-11, SEGUNDA VUELTA ("con 10 tiros no me da el tiempo -- si
# le pego de lejos no acierto, si estoy cerca me paso de largo y tengo que
# volver, no alcanzo a matarlo. Que sean 4, tipo semáforo: rojo, amarillo,
# verde y el otro disparo desaparece. Que entren más cantidad, pero más
# fáciles de bajar"): bajado de 10 a 4, compensado con más cantidad.
const VIDA_UNIDAD_INVASION := 4
const MAX_UNIDADES_INVASION := 60
# Pedido 2026-10-11 ("que no las trague la tierra, que floten un poco
# paralelo al piso, como si volaran bajito"): clearance sobre el terreno
# real encontrado por el rayo -- antes eran 5m, casi pegadas al piso.
const ALTURA_FLOTE_INVASION := 18.0
# Pedido 2026-10-11 ("que tenga tamaño de camión, no de auto... hacé
# diferentes tamaños, cambiále el color a cada uno, y yo te digo cuál va"):
# igual que hicimos con el pitch de los barcos -- en vez de adivinar un
# tamaño, se prueban varios a la vez y Pablo elige por color. Una vez
# elegido, dejar UN solo tamaño/color acá y sacar la variedad.
# Pedido 2026-10-11 ("un poquito más grandes"): +50% todos los tamaños.
const VARIANTES_TAMANO_INVASION := [
	{"lado": 22.0, "color": Color(1.0, 0.15, 0.1, 1.0)},   # rojo -- "auto"
	{"lado": 45.0, "color": Color(1.0, 0.9, 0.1, 1.0)},    # amarillo -- "camión"
	{"lado": 68.0, "color": Color(0.2, 0.9, 0.2, 1.0)},    # verde
	{"lado": 98.0, "color": Color(0.2, 0.6, 1.0, 1.0)},    # celeste -- "cuarto de barco"
	{"lado": 135.0, "color": Color(1.0, 0.3, 0.8, 1.0)},   # rosa -- "medio barco"
]
const LADO_UNIDAD_INVASION := 20.0  # mucho más chico que el cuadrado de aeropuerto (200) -- usado si no se pasa variante
const RADIO_CAPTURA_AEROPUERTO := 250.0  # metros -- a esta distancia del aeropuerto, "llegó"
# Longitud aproximada de la costa cerca de Aeroparque (-58.4164) -- boceto
# de hoy, sin geometría de costa real todavía. Un barco que cruza esta
# longitud (yendo al oeste) se considera "desembarcado".
const LON_COSTA_DESEMBARCO := -58.40
var velocidad_invasion: float = 4.0  # m/s -- "velocidad de los camiones", ajustable en vivo con el HUD (reemplaza al control de velocidad de bala, pausado)
const VELOCIDAD_INVASION_MIN := 1.0
const VELOCIDAD_INVASION_MAX := 20.0
const PASO_VELOCIDAD_INVASION := 1.0

# Pedido 2026-10-11 ("dejate de debuggear con teclas, trabajemos con el que
# ya tenemos centrado -- el T-80, bajalo y achicalo"): el modelo 3D
# reemplaza al cuadrado de cada unidad de invasión real, no solo en una
# prueba aislada. OJO: la prueba de las teclas X/Z usaba un ancla de altura
# DISTINTA (aeropuerto.global_position + 120m) -- acá se usa la fórmula de
# siempre de _crear_unidad_invasion (rayo real al terreno + ALTURA_FLOTE_
# INVASION), la misma que ya posiciona bien los cuadrados negros, así que
# el "bajar 180m" de la prueba NO aplica directamente acá. Arranca con el
# flote normal; si queda mal, se ajusta con un número real, no a ciegas.
const MODELO_TANQUE_INVASION := {"ruta": "res://modelos_objetivos/vehiculos/t80bv.glb", "eje_largo_es_x": true}
const LARGO_OBJETIVO_TANQUE_INVASION := 45.0
const FACTOR_ESCALA_TANQUE_INVASION := 0.6  # "achicalo 10 veces" respecto del escala_exagerada_prueba=6.0 de la prueba
var aeropuertos_tomados: Dictionary = {}  # nombre del aeropuerto (String) -> true
# Pedido 2026-10-11 ("si maté a 5 y son 15, que ponga 5/15 -- quiero saber
# los que di de baja y los que me quedan"): contadores acumulados, no solo
# "cuántos hay vivos ahora".
var invasores_destruidos: int = 0
var invasores_totales_sembrados: int = 0
# Pedido 2026-10-11 ("agregale más invasores, que a medida que los voy
# liquidando, vayan entrando más por la costa"): refuerzos periódicos,
# además de la siembra inicial.
var tiempo_hasta_proximo_refuerzo: float = 30.0
# Pedido 2026-10-11 ("prefiero que haya más invasores"): intervalo más
# corto y tandas más grandes.
const INTERVALO_REFUERZO_INVASION := 30.0
const UNIDADES_POR_REFUERZO := 5

# Misión Industria (pedido 2026-10-10): blancos con modelos 3D reales (no
# cuadraditos) en la zona Quilmes-La Plata, que hace falta golpear varias
# veces para destruir (no mueren de un solo tiro, como las fábricas/tanques
# grandes de verdad). "vida" se guarda como meta en cada nodo.
var objetivos_industria: Array[Node3D] = []
var etiqueta_industria: Label = null
var etiqueta_invasion: Label = null
var industria_destruidos: int = 0
# DEBUG TEMPORAL 2026-10-11 (ver tecla X): reporte de la prueba de
# vehículos EN PANTALLA, grande, en vez de la consola de Godot -- Pablo no
# tiene forma fácil de ver la consola mientras juega.
var etiqueta_diagnostico_vehiculo: Label = null
var nodos_prueba_vehiculos: Array[Node3D] = []
@onready var boton_elegir_vuelo: Button = get_node("../HUD/BarraBotones/BotonElegirVuelo")

# Modo Carrera (pedido 2026-09-21, arranque del esqueleto): perfiles de
# jugador con plata/horas de vuelo/rango -- ver perfil_jugador.gd (autoload
# "PerfilJugador"). Esto es SOLO el esqueleto: selector de piloto +
# credencial con rango e insignia. Los viajes de licencia con aros
# obligatorios y la tienda quedan para la próxima vuelta, ya avisado en el
# propio panel.
@onready var boton_carrera: Button = get_node("../HUD/BarraBotones/BotonCarrera")
@onready var panel_carrera: Panel = get_node("../HUD/PanelCarrera")
@onready var panel_seleccion_perfil: VBoxContainer = get_node("../HUD/PanelCarrera/PanelSeleccionPerfil")
@onready var lista_perfiles: VBoxContainer = get_node("../HUD/PanelCarrera/PanelSeleccionPerfil/ScrollPerfiles/ListaPerfiles")
@onready var campo_nuevo_perfil: LineEdit = get_node("../HUD/PanelCarrera/PanelSeleccionPerfil/HBoxNuevoPerfil/CampoNuevoPerfil")
@onready var boton_crear_perfil: Button = get_node("../HUD/PanelCarrera/PanelSeleccionPerfil/HBoxNuevoPerfil/BotonCrearPerfil")
@onready var boton_cerrar_carrera_1: Button = get_node("../HUD/PanelCarrera/PanelSeleccionPerfil/BotonCerrarCarrera1")
@onready var panel_credencial: VBoxContainer = get_node("../HUD/PanelCarrera/PanelCredencial")
@onready var label_nombre_piloto: Label = get_node("../HUD/PanelCarrera/PanelCredencial/LabelNombrePiloto")
@onready var insignia_piloto: Control = get_node("../HUD/PanelCarrera/PanelCredencial/HBoxRango/InsigniaPiloto")
@onready var label_rango: Label = get_node("../HUD/PanelCarrera/PanelCredencial/HBoxRango/LabelRango")
@onready var label_horas_valor: Label = get_node("../HUD/PanelCarrera/PanelCredencial/FilaHoras/LabelHorasValor")
@onready var boton_horas_menos: Button = get_node("../HUD/PanelCarrera/PanelCredencial/FilaHoras/BotonHorasMenos")
@onready var boton_horas_mas: Button = get_node("../HUD/PanelCarrera/PanelCredencial/FilaHoras/BotonHorasMas")
@onready var label_plata_valor_carrera: Label = get_node("../HUD/PanelCarrera/PanelCredencial/FilaPlataCarrera/LabelPlataValor")
@onready var boton_plata_menos: Button = get_node("../HUD/PanelCarrera/PanelCredencial/FilaPlataCarrera/BotonPlataMenos")
@onready var boton_plata_mas: Button = get_node("../HUD/PanelCarrera/PanelCredencial/FilaPlataCarrera/BotonPlataMas")
@onready var label_progreso_licencia: Label = get_node("../HUD/PanelCarrera/PanelCredencial/LabelProgresoLicencia")
@onready var lista_licencia_helicoptero: VBoxContainer = get_node("../HUD/PanelCarrera/PanelCredencial/ScrollLicenciaHelicoptero/ListaLicenciaHelicoptero")
@onready var boton_cambiar_piloto: Button = get_node("../HUD/PanelCarrera/PanelCredencial/BotonCambiarPiloto")
@onready var boton_cerrar_carrera_2: Button = get_node("../HUD/PanelCarrera/PanelCredencial/BotonCerrarCarrera2")
@onready var selector_vuelo: Panel = get_node("../HUD/SelectorVuelo")
@onready var minimapa_viewport: SubViewport = get_node("../HUD/MinimapaViewport")
@onready var minimapa_rect: TextureRect = get_node("../HUD/MinimapaRect")
@onready var marcadores_viewport: SubViewport = get_node("../HUD/MarcadoresViewport")
@onready var marcadores_rect: TextureRect = get_node("../HUD/MarcadoresRect")
@onready var asa_minimapa: ColorRect = get_node("../HUD/MinimapaRect/AsaMinimapa")
@onready var boton_city_tour: Button = get_node("../HUD/BarraBotones/BotonCityTour")
@onready var hbox_estrellas_city_tour: HBoxContainer = get_node("../HUD/HBoxEstrellasCityTour")
@onready var boton_mapa: Button = get_node("../HUD/BarraBotones/BotonMapa")
@onready var boton_mapa_auto: Button = get_node("../HUD/BarraBotones/BotonMapaAuto")
@onready var mapa_viewport: SubViewport = get_node("../HUD/MapaViewport")
@onready var mapa_rect: TextureRect = get_node("../HUD/MapaRect")
@onready var boton_transparencia_mapa: Button = get_node("../HUD/MapaRect/BotonTransparenciaMapa")
@onready var boton_solapa_1: Button = get_node("../HUD/SolapasInferior/BotonSolapa1")
@onready var boton_solapa_2: Button = get_node("../HUD/SolapasInferior/BotonSolapa2")
@onready var boton_solapa_3: Button = get_node("../HUD/SolapasInferior/BotonSolapa3")
@onready var boton_solapa_4: Button = get_node("../HUD/SolapasInferior/BotonSolapa4")
@onready var boton_solapa_5: Button = get_node("../HUD/SolapasInferior/BotonSolapa5")
@onready var panel_vuelo_tab: Panel = get_node("../HUD/PanelVueloTab")
@onready var contenedor_selector_vuelo: HBoxContainer = get_node("../HUD/PanelVueloTab/VBoxVueloTab/ContenedorSelectorVuelo")
@onready var panel_fotos_decorativas: Panel = get_node("../HUD/PanelFotosDecorativas")
@onready var aguja_velocidad: Label = get_node("../HUD/PanelFotosDecorativas/ScrollRelojes/HBoxRelojes/TarjetaVelocidad/EsferaVelocidad/AgujaVelocidad")
@onready var valor_velocidad_reloj: Label = get_node("../HUD/PanelFotosDecorativas/ScrollRelojes/HBoxRelojes/TarjetaVelocidad/ValorVelocidad")
@onready var aguja_altitud: Label = get_node("../HUD/PanelFotosDecorativas/ScrollRelojes/HBoxRelojes/TarjetaAltitud/EsferaAltitud/AgujaAltitud")
@onready var valor_altitud_reloj: Label = get_node("../HUD/PanelFotosDecorativas/ScrollRelojes/HBoxRelojes/TarjetaAltitud/ValorAltitud")
@onready var horizonte_giro: Control = get_node("../HUD/PanelFotosDecorativas/ScrollRelojes/HBoxRelojes/TarjetaHorizonte/EsferaHorizonte/HorizonteGiro")
@onready var boton_viento: Button = get_node("../HUD/PanelFotosDecorativas/ScrollRelojes/HBoxRelojes/TarjetaViento/VBoxViento/BotonViento")
@onready var flecha_viento: Label = get_node("../HUD/PanelFotosDecorativas/ScrollRelojes/HBoxRelojes/TarjetaViento/VBoxViento/HBoxIndicadorViento/FlechaViento")
@onready var valor_viento: Label = get_node("../HUD/PanelFotosDecorativas/ScrollRelojes/HBoxRelojes/TarjetaViento/VBoxViento/HBoxIndicadorViento/ValorViento")
@onready var panel_sonidos: Panel = get_node("../HUD/PanelSonidos")
@onready var panel_config_tab: Panel = get_node("../HUD/PanelConfigTab")
@onready var panel_torre: Panel = get_node("../HUD/PanelTorre")
@onready var panel_sensibilidad: Panel = get_node("../HUD/PanelSensibilidad")
var mapa_transparente: bool = true
# Viento (pedido explícito 2026-10-03) -- ver uso en el bloque de movimiento
# principal y en _alternar_viento()/_actualizar_indicador_viento().
var viento_activo: bool = false
var viento_direccion_grados: float = 0.0  # de dónde SOPLA (convención meteorológica)
var viento_velocidad_kmh: float = 0.0
@onready var asa_mapa: ColorRect = get_node("../HUD/MapaRect/AsaMapa")
@onready var asa_mapa_sup_izq: ColorRect = get_node("../HUD/MapaRect/AsaMapaSupIzq")
@onready var asa_mapa_sup_der: ColorRect = get_node("../HUD/MapaRect/AsaMapaSupDer")
@onready var asa_mapa_inf_izq: ColorRect = get_node("../HUD/MapaRect/AsaMapaInfIzq")
@onready var icono_avion_mapa: Control = get_node("../HUD/MapaRect/IconoAvionMapa")
@onready var peticion_mapa: HTTPRequest = get_node("../HUD/PeticionMapa")

# PLAN B para el mapa de calles (2026-09-20): después de varios intentos con
# el RasterOverlay de Cesium (siempre en blanco liso, con o sin georreferencia
# separada, con o sin overlay por código -- ver mundo.gd), lo más rápido y
# confiable es bajar directo una tesela de mapa real (OpenStreetMap) como
# imagen 2D, sin pasar por Cesium/3D Tiles para nada en este panel.
var zoom_mapa: int = 13  # REAJUSTADO 2026-10-17 (pedido: "que no aparezca en modo calle -- cuatro zoom para arriba por defecto, y después uno lo corrige con la ruedita y donde queda, queda"): 4 niveles más alejado que el nivel "calle" (17).
const ZOOM_MAPA_MINIMO = 3
const ZOOM_MAPA_MAXIMO = 19
var tile_x_mapa: int = -999999
var tile_y_mapa: int = -999999
var descargando_mapa: bool = false
var acumulador_mapa: float = 0.0

# Zoom automático del mapa según altitud (pedido 2026-09-27): en modo
# automático, cada INTERVALO_ZOOM_AUTOMATICO segundos se recalcula el zoom
# según la altura actual del avión -- así de bajo se ven las calles, de
# crucero se ve un área más amplia. El usuario puede seguir usando la
# ruedita del mouse para override manual en cualquier momento (ver
# _mapa_rect_gui_input) -- eso reinicia el cronómetro, dándole ~1 minuto
# de zoom manual antes de que el automático vuelva a tomar el control.
const INTERVALO_ZOOM_AUTOMATICO = 8.0  # bajado de 60 a 8 (pedido 2026-09-27, "que actualice rápido")

# Auto-retorno del panel de mapa a su tamaño chico (pedido 2026-09-27, panel
# de instrumentos abajo: "que a los pocos segundos vuelva a su estado
# inicial"). Se reinicia en cada resize/movida real (ver _input arriba);
# cuando pasan estos segundos sin tocarlo, si no está ya en el tamaño chico,
# vuelve solo.
var mapa_zoom_automatico: bool = true
var _tiempo_zoom_automatico: float = INTERVALO_ZOOM_AUTOMATICO  # "debido" ya al arrancar, para el primer ajuste rápido

# Pedido 2026-10-17 ("el mapa automático no me sirvió para nada, me perdí
# yendo a Rosario -- quiero que el mapa esté independiente [de la altura]
# y que se pueda arrastrar con el mouse para mirar al costado, quedando
# fijo donde se suelta, con un botón Centrar para volver al avión"): el
# mapa ahora arranca en modo manual (ver _ready, donde se apaga
# mapa_zoom_automatico) y tiene un "centro" propio, independiente del
# avión, que el arrastre desplaza y que "Centrar" repone.
var mapa_siguiendo_avion: bool = true
var mapa_centro_lat: float = 0.0
var mapa_centro_lon: float = 0.0
var _arrastrando_contenido_mapa: bool = false

# Misma capa que usa mundo.gd para las baldosas del mapa de calles -- la
# cámara principal la excluye de su cull_mask para no verlas encimadas con
# los edificios reales (ver _ready()).
const CAPA_RENDER_MAPA_CALLES = 5

# Marcadores propios que tienen que mantener SIEMPRE su color/brillo real
# (aros de ILS, luces de pista, faro de aeropuerto -- ver mundo.gd y
# camara_marcadores.gd) -- la cámara principal los excluye de su cull_mask
# porque los dibuja aparte una segunda cámara sin el post-proceso de noche,
# y ese resultado se superpone encima (MarcadoresRect en el HUD).
const CAPA_MARCADORES_NOCTURNOS = 6

# Agrandar/achicar el panel de mapa agarrando de la esquina (pedido
# explícito, "como en CorelDraw"). El panel de mapa de calles (lateral
# izquierdo) tiene las CUATRO esquinas (pedido 2026-09-22, "vértices para
# poder agrandarlo de cualquier lado, no solo de abajo") -- cada esquina
# crece manteniendo fija la esquina OPUESTA a la que se arrastra.
var _arrastrando_minimapa: bool = false

# BUG REAL encontrado 2026-09-22 ("agarro cualquier vértice y salta a mitad
# de pantalla de golpe", repetido 3 veces): el diseño anterior acumulaba
# `event.relative` cuadro a cuadro y lo sumaba directo a los offsets. Un
# análisis con ayuda de Gemini señaló dos problemas reales en ese enfoque:
# (a) con window/stretch/mode="canvas_items" (ver project.godot) y la
# resolución base por defecto (1152x648) distinta a la resolución real de
# la ventana, el mouse y los offsets del Canvas viven en escalas distintas,
# así que sumar relative crudo desincroniza la proporción; (b) acumular
# deltas cuadro a cuadro es frágil ante cualquier evento repetido o perdido.
# SOLUCIÓN: en vez de acumular, cada arrastre guarda la posición del mouse
# y el rect del panel en el instante del click (_pos_mouse_inicio_mapa /
# _rect_mapa_inicio), y el tamaño en cada cuadro se calcula siempre como
# "tamaño inicial + distancia total recorrida desde el click", usando
# `get_global_transform_with_canvas().affine_inverse()` para pasar la
# posición del mouse al mismo espacio de coordenadas que los offsets
# (absorbe cualquier escala de canvas_items automáticamente). Sin
# acumulación no hay forma de que un evento de más dispare un salto.
enum ModoResizeMapa { NINGUNO, INF_DER, SUP_IZQ, SUP_DER, INF_IZQ }
var _modo_resize_mapa: int = ModoResizeMapa.NINGUNO
var _pos_mouse_inicio_mapa: Vector2 = Vector2.ZERO
var _rect_mapa_inicio: Rect2 = Rect2()

const MINIMAPA_TAMANO_MINIMO = 150.0
const MINIMAPA_TAMANO_MAXIMO = 700.0
const MAPA_TAMANO_MINIMO = 200.0
const MAPA_TAMANO_MAXIMO = 900.0

# El mosaico de baldosas (bajar una grilla 5x5 y mostrarla con
# STRETCH_KEEP_CENTERED + clip_contents, para "revelar más mapa" al agrandar
# el panel en vez de hacer zoom) se sacó por completo el 2026-09-22 -- causó
# tres bugs visuales distintos en tres intentos separados de implementarlo
# bien (manija tapada, mosaico tapando toda la pantalla al arrancar, mapa
# desapareciendo y sin volver con el botón). Se sospecha que clip_contents
# en un TextureRect no recorta la textura propia que dibuja el control, solo
# a los nodos Control hijos -- sin certeza confirmada, pero el patrón de
# fallas repetidas justifica priorizar estabilidad. Se volvió al sistema
# simple de ANTES: una sola baldosa, estirada con el stretch_mode por
# defecto para llenar el panel -- por definición no puede desbordarlo, sea
# cual sea su tamaño. Se pierde el efecto "más área sin zoom", pero el panel
# ya no puede tapar la pantalla ni desaparecer por esto.

# Mover el panel de mapa de calles arrastrando de un bordecito arriba (pedido
# 2026-09-21) -- sin soltar el tamaño (eso lo sigue haciendo AsaMapa, la
# esquina), solo TRASLADA el panel entero a donde el usuario quiera dejarlo
# (por ejemplo, sacarlo del medio cuando la pista aparece del lado izquierdo).
@onready var asa_mover_mapa: ColorRect = get_node("../HUD/MapaRect/AsaMoverMapa")
var _arrastrando_mover_mapa: bool = false

@onready var etiqueta_estado_joystick: Label = get_node("../HUD/PanelConfigTab/ScrollConfig/VBoxConfig/LabelEstadoJoystick")
@onready var lista_acciones_joystick: VBoxContainer = get_node("../HUD/PanelConfigTab/ScrollConfig/VBoxConfig/ScrollAcciones/ListaAcciones")
@onready var option_calidad_graficos: OptionButton = get_node("../HUD/PanelConfigTab/ScrollConfig/VBoxConfig/HBoxCalidadGraficos/OptionCalidadGraficos")
@onready var cesium_tileset: Cesium3DTileset = get_node("../CesiumGeoreference/Cesium3DTileset")

# Control con mouse tipo GeoFS (pedido 2026-09-21): con esto activado, el
# centro de la pantalla es "neutro" y alejar el mouse del centro inclina/
# cabecea el avión proporcionalmente (como un joystick virtual invisible) --
# ver el uso de control_mouse_activo en _procesar_vuelo(). El acelerador
# sigue siendo siempre de teclado (W/S), en los dos modos.
@onready var check_control_mouse: CheckButton = get_node("../HUD/PanelConfigTab/ScrollConfig/VBoxConfig/CheckControlMouse")
@onready var check_editor_luces: CheckButton = get_node("../HUD/PanelConfigTab/ScrollConfig/VBoxConfig/CheckEditorLuces")
@onready var boton_clavar_luces: Button = get_node("../HUD/PanelConfigTab/ScrollConfig/VBoxConfig/BotonClavarLuces")
const RUTA_CONFIG_CONTROLES = "user://controles_config.cfg"
var control_mouse_activo: bool = false

# Ciclo día/noche -- estos botones solo llaman a las funciones de mundo.gd
# (ahí vive todo el cálculo real, ver ajustar_hora_del_dia/
# alternar_avance_automatico_hora), acá solo se refleja el valor en pantalla.
@onready var boton_hora_menos: Button = get_node("../HUD/BarraBotones/HBoxHoraBarra/BotonHoraMenos")
@onready var boton_hora_mas: Button = get_node("../HUD/BarraBotones/HBoxHoraBarra/BotonHoraMas")
@onready var label_hora_valor: Label = get_node("../HUD/BarraBotones/HBoxHoraBarra/LabelHoraValor")
@onready var check_avance_automatico_hora: CheckButton = get_node("../HUD/BarraBotones/CheckAvanceAutomaticoHora")

# JOYSTICK -- pedido explícito 2026-09-20. Godot no distingue cable vs.
# Bluetooth a nivel de código -- el sistema operativo hace el emparejamiento
# y a Godot le llega igual por la API de "joypad" sea como sea que esté
# conectado, así que ambos tipos funcionan sin código especial para cada uno.
const RUTA_CONFIG_JOYSTICK = "user://joystick_config.cfg"
const DEADZONE_EJE_JOYSTICK = 0.5
# Acciones mapeables -- las dos últimas ("vertical_arriba/abajo") todavía no
# se usan en ningún control real, son para el helicóptero que viene después,
# pero se pueden asignar ya mismo para no tener que volver a tocar esto.
const ACCIONES_JOYSTICK = [
	["acelerar", "Acelerar"],
	["frenar", "Frenar / desacelerar"],
	["cabeceo_arriba", "Cabecear arriba (subir nariz)"],
	["cabeceo_abajo", "Cabecear abajo (bajar nariz)"],
	["alabeo_izquierda", "Girar/inclinar izquierda"],
	["alabeo_derecha", "Girar/inclinar derecha"],
	["vertical_arriba", "Subir vertical (futuro helicóptero)"],
	["vertical_abajo", "Bajar vertical (futuro helicóptero)"],
	["marcar_lugar", "Marcar lugar (guardar coordenada actual)"],
	["freno_emergencia", "Freno de emergencia (baja a 200)"],
	["freno_gradual", "Freno progresivo"],
	["armamento", "Disparo / armamento"],
	["mira_horizontal", "Mira horizontal"],
	["mira_vertical", "Mira vertical"],
	["activar_ils", "Activar/desactivar ILS"],
	# Pedido 2026-10-01: "trackear" a mano el punto exacto donde el avión pasa
	# por el aro del City Tour (más preciso que yo calculando una distancia a
	# ciegas) -- reusa la fila reservada de 2026-09-26 (se mantiene el id
	# "reservado_1" para no romper asignaciones de joystick ya guardadas).
	["reservado_1", "Trackear checkpoint (City Tour)"],
	# Pedido 2026-10-17 ("modo a pie" + trackeo manual de rutas).
	["marcar_punto_ruta", "Marcar punto de ruta (modo a pie)"],
	["deshacer_punto_ruta", "Borrar último punto de ruta"],
]
var joystick_id: int = -1
var mapeo_joystick: Dictionary = {}   # nombre_accion -> {"tipo":"boton","indice":N} o {"tipo":"eje","indice":N,"signo":1.0}
var esperando_asignacion: String = ""  # nombre de la acción que se está esperando asignar ahora, "" si ninguna

# Offset de la cámara respecto del avión: la distancia/altura son las mismas
# de antes, pero ahora la cámara NO es hija del avión, así que hay que
# recalcular su posición todos los cuadros a mano (ver _actualizar_camara).
const OFFSET_CAMARA_ALTURA = 2.5
const OFFSET_CAMARA_DISTANCIA = 8.0

# Objetivo de una misión de helicóptero activa (nodo en mundo.gd) -- cuando
# no es null, la guía (rumbo/flechas/distancia) apunta ACÁ en vez de al
# sistema de aeropuertos/destinos de siempre.
var objetivo_mision: Node3D = null
var nombre_mision_actual: String = ""
# Pedido 2026-10-17 ("activá el rumbo/las flechas para los viajes de
# Destino Nacional, me perdí yendo a Rosario"): "objetivo_mision" es
# justo el enganche que ya usa el sistema de guía (rumbo/flechas/
# distancia/línea) para las misiones de helicóptero -- se reusa tal cual
# para CUALQUIER paso "llegar_aeropuerto"/"llegar_punto" de mi esqueleto
# de misiones nuevo, en vez de inventar un sistema de guía aparte. Esta
# bandera indica si el "objetivo_mision" actual es de ESTE sistema (para
# no pisar el de las misiones de helicóptero al limpiarlo).
var objetivo_mision_de_secuencia: bool = false
var precio_mision_actual: int = 0

var destinos: Array = []
# Arranca en 1, no 0: el destino[0] ahora es "Aeroparque", que es de donde
# salís (ahí mismo arranca el avión) -- si empezara apuntando a Aeroparque,
# creería que "llegaste" apenas arrancás el juego.
var indice_destino: int = 1
var temporizador_torre: float = 0.0
var selector_poblado: bool = false

const IMAGENES_PRESENTACION = [
	"res://imagenes_presentacion/presentacion_atardecer.png",
	"res://imagenes_presentacion/presentacion_noche.png",
	"res://imagenes_presentacion/presentacion_tierra.png",
]

func _ready() -> void:
	# REVERTIDO 2026-10-11: se había puesto "process_priority = 10" acá para
	# forzar que el avión procese DESPUÉS que mundo.gd (hipótesis del
	# disparo saliendo de atrás). Resultó INNECESARIO -- el nodo de mundo.gd
	# es la RAÍZ de toda la escena y "Avion" es hijo directo de esa raíz; en
	# Godot un padre siempre procesa su _process() ANTES que sus hijos por
	# default, así que el orden ya era correcto sin tocar nada. Y el cambio
	# tenía un efecto colateral real: otros scripts que dependen de la
	# posición del avión ya actualizada ese cuadro (camara_marcadores.gd,
	# minimapa_camara.gd, icono_avion_mapa.gd) procesaban naturalmente
	# DESPUÉS del avión -- al forzar al avión a prioridad 10 (la más baja,
	# corre último), esos scripts pasaron a leer la posición VIEJA del
	# avión, causando el reporte "las luces de las alas se corren hacia
	# adelante". Revertido. Si el disparo vuelve a salir mal, el debug
	# print de abajo en _disparar_hacia_mira() (distancia_sospechosa) sigue
	# activo para diagnosticar sin adivinar.
	print("El avión arrancó bien y el script está corriendo.")
	# Pedido 2026-10-17 ("cada vez que arranco, arranca de cero en la cuenta
	# de dinero"): antes la plata solo persistía si entrabas a mano a la
	# pestaña Carrera a elegir piloto -- ahora se autocarga el último perfil
	# usado (o se crea uno nuevo la primera vez) apenas arranca el juego, sin
	# que haga falta ir a elegirlo. La pestaña Carrera sigue funcionando
	# igual que siempre para crear perfiles nuevos (otros jugadores) o
	# cambiarse de piloto a mano.
	PerfilJugador.autocargar_ultimo_perfil()
	# Verificación pedida por Gemini 2026-10-03 (para no repetir la confusión
	# de "probamos Mobile" cuando en realidad nunca se aplicó por un project.godot
	# corrupto): esto lee el renderer REAL que está corriendo el motor ahora
	# mismo, sin confiar en lo que diga el archivo en disco.
	print(">>> RENDERER ACTIVO: ", ProjectSettings.get_setting("rendering/renderer/rendering_method"))
	# Pedido explícito 2026-10-03 ("sacá ese avioncito infantil, dame más
	# profesionalidad"): pantalla de carga con arte real (generado con
	# ChatGPT) en vez del fondo plano de siempre -- una de las 3 imágenes al
	# azar cada vez que arranca el juego, para no ver siempre la misma.
	fondo_presentacion.texture = load(IMAGENES_PRESENTACION[randi() % IMAGENES_PRESENTACION.size()])
	# Presentación limpia: la cortina se dibuja SIEMPRE por encima de todo el HUD
	# (cartel de aterrizaje, control de velocidad de invasión, etc.).
	cortina_de_carga.z_index = 4000
	_crear_luces_avion_clasico()
	_crear_mira_hud()
	sonido_disparo = AudioStreamPlayer.new()
	sonido_disparo.name = "SonidoDisparoPrototipo"
	# Pedido 2026-10-07: se probó "disparo_canon.mp3" pero sonaba raro en
	# juego (el offset de 1 segundo de inicio, abajo, estaba afinado para
	# cannon_fire.ogg específicamente) -- vuelve a cannon_fire.ogg.
	var ruta_disparo_real := "res://FX/Disparos/cannon_fire.ogg"
	sonido_disparo_archivo_real = ResourceLoader.exists(ruta_disparo_real)
	sonido_disparo.stream = load(ruta_disparo_real) if sonido_disparo_archivo_real else _generar_sonido_disparo()
	add_child(sonido_disparo)
	sonido_explosion = AudioStreamPlayer.new()
	sonido_explosion.name = "SonidoExplosionObjetivo"
	# Explosión "grande" para blancos en tierra (aeropuertos + misión verde).
	var ruta_explosion_real := "res://FX/Explosiones/explosion_impacto.mp3"
	sonido_explosion.stream = load(ruta_explosion_real) if ResourceLoader.exists(ruta_explosion_real) else _generar_sonido_explosion()
	sonido_explosion.volume_db = 8.0
	add_child(sonido_explosion)
	sonido_explosion_barco = AudioStreamPlayer.new()
	sonido_explosion_barco.name = "SonidoExplosionBarco"
	# Explosión distinta, más chica, para los barcos (pedido explícito: "uno
	# para los barcos y otro para los demás").
	var ruta_explosion_barco := "res://FX/Explosiones/explosion_barco.mp3"
	sonido_explosion_barco.stream = load(ruta_explosion_barco) if ResourceLoader.exists(ruta_explosion_barco) else _generar_sonido_explosion()
	sonido_explosion_barco.volume_db = 6.0
	add_child(sonido_explosion_barco)
	# Pedido 2026-10-07 (segunda vez -- la primera no llegué a aplicarla):
	# letra más chica, y ARRANCAN OCULTOS -- no tienen que verse durante la
	# pantalla de presentación/cortina de carga. Se hacen visibles recién
	# cuando la cortina se saca de encima (ver _process, junto a
	# "cortina_de_carga.visible = false").
	# Pedido 2026-10-10 ("no me gustan esos iconos tan infantiles, subilas
	# arriba, juntas, prolijas, letra más chica, letras electrónicas
	# verdes"): sin emojis, un solo color verde tipo display electrónico
	# para las 4, apiladas BIEN juntas justo debajo de la barra de botones
	# de arriba (no al lado del mapa como antes).
	const COLOR_HUD_MISIONES := Color(0.25, 1.0, 0.45, 1.0)
	etiqueta_objetivo_destruido = Label.new()
	etiqueta_objetivo_destruido.name = "ObjetivoDestruidoHUD"
	etiqueta_objetivo_destruido.set_anchors_preset(Control.PRESET_TOP_RIGHT)
	etiqueta_objetivo_destruido.position = Vector2(-200.0, 44.0)
	etiqueta_objetivo_destruido.size = Vector2(180.0, 20.0)
	etiqueta_objetivo_destruido.horizontal_alignment = HORIZONTAL_ALIGNMENT_RIGHT
	etiqueta_objetivo_destruido.add_theme_font_size_override("font_size", 12)
	etiqueta_objetivo_destruido.modulate = COLOR_HUD_MISIONES
	etiqueta_objetivo_destruido.text = "AEROPUERTOS: 0/5"
	etiqueta_objetivo_destruido.visible = false
	get_node("../HUD").add_child(etiqueta_objetivo_destruido)
	etiqueta_mision_verde = Label.new()
	etiqueta_mision_verde.name = "MisionVerdeHUD"
	etiqueta_mision_verde.set_anchors_preset(Control.PRESET_TOP_RIGHT)
	etiqueta_mision_verde.position = Vector2(-200.0, 62.0)
	etiqueta_mision_verde.size = Vector2(180.0, 20.0)
	etiqueta_mision_verde.horizontal_alignment = HORIZONTAL_ALIGNMENT_RIGHT
	etiqueta_mision_verde.add_theme_font_size_override("font_size", 12)
	etiqueta_mision_verde.modulate = COLOR_HUD_MISIONES
	etiqueta_mision_verde.text = "CAPITAL: 0/10"
	etiqueta_mision_verde.visible = false
	get_node("../HUD").add_child(etiqueta_mision_verde)
	etiqueta_barcos = Label.new()
	etiqueta_barcos.name = "MisionBarcosHUD"
	etiqueta_barcos.set_anchors_preset(Control.PRESET_TOP_RIGHT)
	etiqueta_barcos.position = Vector2(-200.0, 80.0)
	etiqueta_barcos.size = Vector2(180.0, 20.0)
	etiqueta_barcos.horizontal_alignment = HORIZONTAL_ALIGNMENT_RIGHT
	etiqueta_barcos.add_theme_font_size_override("font_size", 12)
	etiqueta_barcos.modulate = COLOR_HUD_MISIONES
	etiqueta_barcos.text = "BARCOS: 0/%d" % BARCOS_RIO_AEROPARQUE.size()
	etiqueta_barcos.visible = false
	get_node("../HUD").add_child(etiqueta_barcos)
	etiqueta_industria = Label.new()
	etiqueta_industria.name = "MisionIndustriaHUD"
	etiqueta_industria.set_anchors_preset(Control.PRESET_TOP_RIGHT)
	etiqueta_industria.position = Vector2(-200.0, 98.0)
	etiqueta_industria.size = Vector2(180.0, 20.0)
	etiqueta_industria.horizontal_alignment = HORIZONTAL_ALIGNMENT_RIGHT
	etiqueta_industria.add_theme_font_size_override("font_size", 12)
	etiqueta_industria.modulate = COLOR_HUD_MISIONES
	etiqueta_industria.text = "INDUSTRIA: 0/10"
	etiqueta_industria.visible = false
	get_node("../HUD").add_child(etiqueta_industria)
	# Pedido 2026-10-11 ("necesito saber cuántos invasores quedan, para
	# rastrear si me queda alguno corriendo por ahí"): contador simple,
	# mismo estilo que los demás, se actualiza solo en
	# _actualizar_unidades_invasion().
	etiqueta_invasion = Label.new()
	etiqueta_invasion.name = "InvasionHUD"
	etiqueta_invasion.set_anchors_preset(Control.PRESET_TOP_RIGHT)
	etiqueta_invasion.position = Vector2(-200.0, 116.0)
	etiqueta_invasion.size = Vector2(180.0, 20.0)
	etiqueta_invasion.horizontal_alignment = HORIZONTAL_ALIGNMENT_RIGHT
	etiqueta_invasion.add_theme_font_size_override("font_size", 12)
	etiqueta_invasion.modulate = Color(1.0, 0.55, 0.15, 1.0)
	etiqueta_invasion.text = "INVASORES: 0"
	etiqueta_invasion.visible = false
	get_node("../HUD").add_child(etiqueta_invasion)

	# DEBUG TEMPORAL 2026-10-11 (ver tecla X): reporte de la prueba de
	# vehículos EN PANTALLA, letra grande, centrado arriba -- Pablo no tiene
	# forma fácil de encontrar la consola de Godot mientras juega.
	etiqueta_diagnostico_vehiculo = Label.new()
	etiqueta_diagnostico_vehiculo.name = "DiagnosticoVehiculoHUD"
	etiqueta_diagnostico_vehiculo.set_anchors_preset(Control.PRESET_TOP_WIDE)
	etiqueta_diagnostico_vehiculo.position = Vector2(0.0, 150.0)
	etiqueta_diagnostico_vehiculo.size = Vector2(0.0, 120.0)
	etiqueta_diagnostico_vehiculo.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
	etiqueta_diagnostico_vehiculo.add_theme_font_size_override("font_size", 26)
	etiqueta_diagnostico_vehiculo.modulate = Color(1.0, 1.0, 0.2, 1.0)
	etiqueta_diagnostico_vehiculo.add_theme_color_override("font_outline_color", Color(0, 0, 0, 1))
	etiqueta_diagnostico_vehiculo.add_theme_constant_override("outline_size", 6)
	etiqueta_diagnostico_vehiculo.visible = false
	get_node("../HUD").add_child(etiqueta_diagnostico_vehiculo)

	# Pedido 2026-10-16 ("un cuadradito como el del altímetro, pero que diga
	# cuánta RAM libre queda -- así si veo un crash puedo chequear si fue por
	# memoria"): una caja más en la fila de instrumentos de arriba (ALT/
	# RUMBO/VEL/DIST), mismo estilo exacto (reusa el panel de CajaAltitud en
	# vez de armar uno nuevo, para que no se note la diferencia).
	var caja_altitud_ref: PanelContainer = get_node("../HUD/PanelInstrumentos/CajaAltitud")
	var caja_ram := PanelContainer.new()
	caja_ram.name = "CajaRAM"
	caja_ram.custom_minimum_size = Vector2(105, 58)
	caja_ram.add_theme_stylebox_override("panel", caja_altitud_ref.get_theme_stylebox("panel"))
	etiqueta_ram = Label.new()
	etiqueta_ram.name = "RamLabel"
	etiqueta_ram.add_theme_color_override("font_outline_color", Color(0, 0, 0, 1))
	etiqueta_ram.add_theme_constant_override("line_spacing", -4)
	etiqueta_ram.add_theme_constant_override("outline_size", 2)
	etiqueta_ram.add_theme_font_size_override("font_size", 15)
	etiqueta_ram.text = "RAM\n--- MB"
	etiqueta_ram.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
	etiqueta_ram.vertical_alignment = VERTICAL_ALIGNMENT_CENTER
	caja_ram.add_child(etiqueta_ram)
	get_node("../HUD/PanelInstrumentos").add_child(caja_ram)

	# PASO 1 -- recordatorio chico y persistente del objetivo de la misión
	# actual (pedido 2026-10-16: "que quede en un cuadradito el objetivo,
	# por si uno se lo olvida"), mismo estilo que los contadores de arriba
	# a la derecha (AEROPUERTOS/CAPITAL/etc).
	etiqueta_objetivo_mision = Label.new()
	etiqueta_objetivo_mision.name = "ObjetivoMisionHUD"
	etiqueta_objetivo_mision.set_anchors_preset(Control.PRESET_TOP_RIGHT)
	etiqueta_objetivo_mision.position = Vector2(-340.0, 86.0)
	etiqueta_objetivo_mision.size = Vector2(320.0, 20.0)
	etiqueta_objetivo_mision.horizontal_alignment = HORIZONTAL_ALIGNMENT_RIGHT
	etiqueta_objetivo_mision.add_theme_font_size_override("font_size", 13)
	etiqueta_objetivo_mision.modulate = Color(1.0, 0.9, 0.3, 1.0)
	etiqueta_objetivo_mision.visible = false
	get_node("../HUD").add_child(etiqueta_objetivo_mision)

	# Pedido 2026-10-11 ("una barrita roja de 10 para mi vida, y arriba en
	# el HUD -- no en Configuración -- un más/menos para ir ajustando la
	# velocidad de las balas de los barcos mientras pruebo"): dos controles
	# nuevos, mismo estilo que los paneles de instrumentos de arriba.
	var fila_vida := HBoxContainer.new()
	fila_vida.name = "FilaVidaJugador"
	fila_vida.set_anchors_preset(Control.PRESET_TOP_LEFT)
	fila_vida.position = Vector2(10.0, 100.0)
	fila_vida.add_theme_constant_override("separation", 3)
	for i in range(VIDA_INICIAL_JUGADOR):
		var segmento := ColorRect.new()
		segmento.custom_minimum_size = Vector2(16.0, 16.0)
		segmento.color = Color(0.85, 0.12, 0.08, 1.0)
		fila_vida.add_child(segmento)
		segmentos_vida_jugador.append(segmento)
	get_node("../HUD").add_child(fila_vida)

	# REPROPÓSITO 2026-10-11 (pedido explícito de Pablo, "el sistema de bala
	# es malísimo, olvidémonos por ahora... donde está la velocidad de las
	# balas, ponés la velocidad de los camiones de invasión"): este control
	# ya no toca velocidad_bala_barco (pausado) -- ahora mueve
	# velocidad_invasion en vivo.
	var fila_vel_invasion := HBoxContainer.new()
	fila_vel_invasion.name = "FilaVelocidadInvasion"
	fila_vel_invasion.set_anchors_preset(Control.PRESET_TOP_LEFT)
	fila_vel_invasion.position = Vector2(10.0, 122.0)
	fila_vel_invasion.add_theme_constant_override("separation", 4)
	var etiqueta_vel_titulo := Label.new()
	etiqueta_vel_titulo.text = "Vel. invasión:"
	etiqueta_vel_titulo.add_theme_font_size_override("font_size", 12)
	etiqueta_vel_titulo.modulate = Color(1.0, 0.7, 0.3, 1.0)
	fila_vel_invasion.add_child(etiqueta_vel_titulo)
	var boton_vel_menos := Button.new()
	boton_vel_menos.text = "－"
	boton_vel_menos.custom_minimum_size = Vector2(22, 20)
	boton_vel_menos.focus_mode = Control.FOCUS_NONE
	boton_vel_menos.pressed.connect(func():
		velocidad_invasion = max(VELOCIDAD_INVASION_MIN, velocidad_invasion - PASO_VELOCIDAD_INVASION)
		etiqueta_velocidad_invasion.text = "%d" % int(velocidad_invasion))
	fila_vel_invasion.add_child(boton_vel_menos)
	etiqueta_velocidad_invasion = Label.new()
	etiqueta_velocidad_invasion.text = "%d" % int(velocidad_invasion)
	etiqueta_velocidad_invasion.custom_minimum_size = Vector2(28, 0)
	etiqueta_velocidad_invasion.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
	etiqueta_velocidad_invasion.add_theme_font_size_override("font_size", 12)
	fila_vel_invasion.add_child(etiqueta_velocidad_invasion)
	var boton_vel_mas := Button.new()
	boton_vel_mas.text = "＋"
	boton_vel_mas.custom_minimum_size = Vector2(22, 20)
	boton_vel_mas.focus_mode = Control.FOCUS_NONE
	boton_vel_mas.pressed.connect(func():
		velocidad_invasion = min(VELOCIDAD_INVASION_MAX, velocidad_invasion + PASO_VELOCIDAD_INVASION)
		etiqueta_velocidad_invasion.text = "%d" % int(velocidad_invasion))
	fila_vel_invasion.add_child(boton_vel_mas)
	get_node("../HUD").add_child(fila_vel_invasion)

	# Pedido 2026-10-10 ("el cartel de fuera de alcance es gigante y
	# antiestético, poné un recuadro negro chico como el resto de los
	# instrumentos de arriba, letras electrónicas chiquitas"): panel chico
	# al lado de ALT/RUMBO/VEL/DIST, mismo estilo (fondo negro, texto verde).
	panel_fuera_de_mira = PanelContainer.new()
	panel_fuera_de_mira.name = "PanelFueraDeMira"
	var estilo_panel := StyleBoxFlat.new()
	estilo_panel.bg_color = Color(0.0, 0.0, 0.0, 0.75)
	estilo_panel.corner_radius_top_left = 6
	estilo_panel.corner_radius_top_right = 6
	estilo_panel.corner_radius_bottom_left = 6
	estilo_panel.corner_radius_bottom_right = 6
	estilo_panel.content_margin_left = 10
	estilo_panel.content_margin_right = 10
	estilo_panel.content_margin_top = 4
	estilo_panel.content_margin_bottom = 4
	panel_fuera_de_mira.add_theme_stylebox_override("panel", estilo_panel)
	panel_fuera_de_mira.set_anchors_preset(Control.PRESET_TOP_LEFT)
	panel_fuera_de_mira.position = Vector2(800.0, 65.0)
	panel_fuera_de_mira.visible = false
	etiqueta_fuera_de_mira = Label.new()
	etiqueta_fuera_de_mira.add_theme_font_size_override("font_size", 13)
	etiqueta_fuera_de_mira.modulate = Color(0.25, 1.0, 0.45, 1.0)
	etiqueta_fuera_de_mira.text = "OBJETIVO FUERA DE MIRA"
	panel_fuera_de_mira.add_child(etiqueta_fuera_de_mira)
	get_node("../HUD").add_child(panel_fuera_de_mira)
	sonido_mision_completada = AudioStreamPlayer.new()
	sonido_mision_completada.name = "SonidoMisionCompletada"
	sonido_mision_completada.stream = _generar_sonido_mision_completada()
	add_child(sonido_mision_completada)
	# call_deferred: a esta altura de _ready() el nodo "mundo" (padre) puede
	# no haber terminado el suyo todavía -- Godot llama _ready() de abajo
	# hacia arriba, así que mundo.georeferencia (un @onready de mundo.gd)
	# puede seguir null acá. BUG REAL encontrado 2026-10-07 ("no deja de
	# entrar al juego, pestañea y queda en el editor"): _posicion_desde_lat_lon
	# llamaba a georeferencia.get_ecef_x() sobre null. call_deferred espera
	# a que termine el cuadro actual (con TODOS los _ready() ya corridos).
	call_deferred("_crear_mision_verde")
	call_deferred("_crear_barcos_rio")
	call_deferred("_crear_mision_industria")
	call_deferred("_sembrar_invasion_inicial")
	# BUG REAL encontrado 2026-10-10 (reportado: "desaparecieron los 5
	# cuadraditos rojos de los aeropuertos"): esta función SOLO se llamaba
	# desde _confirmar_viaje() (al elegir un destino del menú) -- si el
	# jugador arranca y vuela directo sin pasar por esa pantalla (como
	# ahora, con el F/A-18E de arranque tentando a despegar ya), nunca se
	# creaban. Los otros 3 tipos (industria/CABA/barcos) ya se creaban solos
	# acá -- faltaba sumar este.
	call_deferred("_crear_objetivos_armamento")
	boton_confirmar.pressed.connect(_confirmar_viaje)
	boton_despegar.pressed.connect(_despegar)
	# IMPORTANTE: sin esto, un botón clickeado se queda con el FOCO de
	# teclado -- y en Godot, "ui_accept" (la tecla ESPACIO que usamos para
	# frenar) también "activa" el botón enfocado. Resultado: frenar con
	# espacio una segunda vez podía re-disparar "Confirmar viaje" sin
	# querer, tele-transportando al avión de vuelta al origen en pleno
	# vuelo (el bug de "apreté el freno y aparecí volando de nuevo").
	boton_confirmar.focus_mode = Control.FOCUS_NONE
	boton_despegar.focus_mode = Control.FOCUS_NONE
	boton_despegar.visible = false

	# El panel "Elegir vuelo" ocupaba media pantalla todo el tiempo, aunque
	# solo se usa al despegar -- ahora arranca escondido y se abre/cierra
	# con un botón chico arriba, junto a la velocidad.
	boton_elegir_vuelo.focus_mode = Control.FOCUS_NONE
	boton_elegir_vuelo.pressed.connect(func():
		selector_vuelo.visible = not selector_vuelo.visible)
	_actualizar_etiqueta_velocidad()

	for tipo in TIPOS_AVION:
		tipo_avion_option.add_item(tipo["nombre"])
	# Avión por defecto al arrancar (pedido 2026-10-10: "dejalo como avión
	# principal por default cuando arranca el juego" -- el F/A-18E, no el
	# Piper) -- buscado por nombre, no por índice fijo, para no romperse si
	# el orden de TIPOS_AVION cambia.
	var indice_avion_default: int = 0
	for i in TIPOS_AVION.size():
		if TIPOS_AVION[i]["nombre"] == "F/A-18E Super Hornet (nuevo)":
			indice_avion_default = i
			break
	tipo_avion_option.select(indice_avion_default)
	tipo_avion_option.focus_mode = Control.FOCUS_NONE
	# BUG REAL encontrado 2026-10-03 (reportado: "arranca con el avioncito
	# clásico igual"): seleccionar el ítem en el desplegable NO cambia el
	# modelo 3D visible -- eso solo pasa al confirmar un vuelo
	# (_aplicar_tipo_avion se llama ahí). Al arrancar el juego el avión ya
	# está volando (estado = VOLANDO por defecto) con el modelo clásico
	# puesto a mano en la escena, sin pasar nunca por esa función. Forzado
	# acá para que el Piper se vea desde el primer cuadro, no recién cuando
	# el jugador confirma un vuelo.
	_aplicar_tipo_avion(indice_avion_default)

	# Misiones de helicóptero (pedido 2026-09-20) -- esqueleto jugable: elegís
	# una de la lista, te teletransporta ahí mismo con el helicóptero puesto.
	# Nada de esto pasa "mientras se está viajando" (el panel es de
	# planificación, como "Elegir vuelo"), así que no importa que tape mapa
	# o pantalla al abrirse.
	boton_misiones.focus_mode = Control.FOCUS_NONE
	boton_misiones.pressed.connect(func():
		panel_misiones.visible = not panel_misiones.visible)
	boton_cerrar_misiones.focus_mode = Control.FOCUS_NONE
	boton_cerrar_misiones.pressed.connect(func():
		panel_misiones.visible = false)
	_poblar_lista_misiones()

	boton_ayuda_visual.focus_mode = Control.FOCUS_NONE
	boton_ayuda_visual.pressed.connect(func():
		# Obligatoria durante una misión de licencia -- no se puede apagar
		# (pedido explícito, "si está mal, los corregimos, pero no se puede
		# desactivar").
		if mision_licencia_indice >= 0:
			cartel_central.text = "🔒 La ayuda visual es obligatoria durante la licencia"
			cartel_central.visible = true
			get_tree().create_timer(2.0).timeout.connect(func(): cartel_central.visible = false)
			return
		ayuda_visual_activa = not ayuda_visual_activa
		luz_ayuda_visual.color = COLOR_LUZ_ENCENDIDA if ayuda_visual_activa else COLOR_LUZ_APAGADA
		if not ayuda_visual_activa:
			linea_guia.visible = false)

	boton_radio.focus_mode = Control.FOCUS_NONE
	boton_radio.pressed.connect(_alternar_radio)
	# volume_db ahora lo controla slider_volumen_radio (más abajo en _ready).

	boton_radio_en_vivo.focus_mode = Control.FOCUS_NONE
	boton_radio_en_vivo.pressed.connect(func(): OS.shell_open(URL_RADIO_ASPEN))

	boton_emisoras.focus_mode = Control.FOCUS_NONE
	boton_emisoras.pressed.connect(func():
		panel_radio.visible = not panel_radio.visible
		if panel_radio.visible:
			_refrescar_lista_emisoras_radio())
	boton_detener_radio.focus_mode = Control.FOCUS_NONE
	boton_detener_radio.pressed.connect(func():
		sonido_radio.stop()
		radio_activa = false
		boton_radio.text = "📻 Radio: OFF")
	boton_cerrar_radio.focus_mode = Control.FOCUS_NONE
	boton_cerrar_radio.pressed.connect(func(): panel_radio.visible = false)
	_cargar_emisora_radio()
	# Pedido explícito 2026-10-01 ("que la radio ya arranque activada... el
	# que no sabe jugar ya dice uh, mirá, tiene esos sonidos"): arranca
	# prendida sola al abrir el juego, el jugador la puede apagar con el
	# mismo botón de siempre si molesta.
	if ruta_emisora_actual != "":
		_alternar_radio()

	boton_instrucciones.focus_mode = Control.FOCUS_NONE
	boton_instrucciones.pressed.connect(_alternar_instrucciones)

	# Botón "✕" (pedido 2026-09-21, "la tecla escape no me funciona en la
	# notebook"): mismo efecto que ESCAPE cuando no hay ningún panel abierto
	# -- sale de pantalla completa. Siempre visible, arriba a la derecha.
	boton_salir_pantalla_completa.focus_mode = Control.FOCUS_NONE
	boton_salir_pantalla_completa.pressed.connect(func():
		if DisplayServer.window_get_mode() == DisplayServer.WINDOW_MODE_FULLSCREEN:
			DisplayServer.window_set_mode(DisplayServer.WINDOW_MODE_WINDOWED))

	boton_carrera.focus_mode = Control.FOCUS_NONE
	boton_crear_perfil.focus_mode = Control.FOCUS_NONE
	boton_cerrar_carrera_1.focus_mode = Control.FOCUS_NONE
	boton_horas_menos.focus_mode = Control.FOCUS_NONE
	boton_horas_mas.focus_mode = Control.FOCUS_NONE
	boton_plata_menos.focus_mode = Control.FOCUS_NONE
	boton_plata_mas.focus_mode = Control.FOCUS_NONE
	boton_cambiar_piloto.focus_mode = Control.FOCUS_NONE
	boton_cerrar_carrera_2.focus_mode = Control.FOCUS_NONE
	boton_carrera.pressed.connect(_abrir_panel_carrera)
	boton_crear_perfil.pressed.connect(_crear_perfil_desde_campo)
	campo_nuevo_perfil.text_submitted.connect(func(_texto): _crear_perfil_desde_campo())
	boton_cerrar_carrera_1.pressed.connect(func(): panel_carrera.visible = false)
	boton_cerrar_carrera_2.pressed.connect(func(): panel_carrera.visible = false)
	boton_cambiar_piloto.pressed.connect(_volver_a_seleccion_perfil)
	boton_horas_menos.pressed.connect(func(): PerfilJugador.agregar_horas_vuelo(-10.0); _refrescar_credencial())
	boton_horas_mas.pressed.connect(func(): PerfilJugador.agregar_horas_vuelo(10.0); _refrescar_credencial())
	boton_plata_menos.pressed.connect(func(): PerfilJugador.modificar_plata(-100); _refrescar_credencial())
	boton_plata_mas.pressed.connect(func(): PerfilJugador.modificar_plata(100); _refrescar_credencial())

	_cargar_sensibilidad_aviones()
	_cargar_aterrizaje_aviones()
	for boton in [boton_giro_menos, boton_giro_mas, boton_angulo_menos, boton_angulo_mas,
			boton_vertical_menos, boton_vertical_mas,
			boton_vel_minima_menos, boton_vel_minima_mas, boton_vel_maxima_menos, boton_vel_maxima_mas]:
		boton.focus_mode = Control.FOCUS_NONE
	boton_giro_menos.pressed.connect(func(): _ajustar_sensibilidad("giro", -0.05, 0.1, 2.0))
	boton_giro_mas.pressed.connect(func(): _ajustar_sensibilidad("giro", 0.05, 0.1, 2.0))
	boton_angulo_menos.pressed.connect(func(): _ajustar_sensibilidad("angulo", -2.0, 10.0, 80.0))
	boton_angulo_mas.pressed.connect(func(): _ajustar_sensibilidad("angulo", 2.0, 10.0, 80.0))
	boton_vertical_menos.pressed.connect(func():
		_ajustar_sensibilidad("vertical", -2.0 if _es_helicoptero() else -2.0,
			2.0 if _es_helicoptero() else 10.0, 40.0 if _es_helicoptero() else 80.0))
	boton_vertical_mas.pressed.connect(func():
		_ajustar_sensibilidad("vertical", 2.0 if _es_helicoptero() else 2.0,
			2.0 if _es_helicoptero() else 10.0, 40.0 if _es_helicoptero() else 80.0))
	# Velocidad mínima/máxima por avión (pedido 2026-09-21, "así voy
	# delimitando la mínima y la máxima de cada uno hasta que quede como
	# valor definitivo"). Paso de 10 en 10, sin techo fijo -- el usuario
	# quiere poder llevarlo a cualquier valor mientras prueba.
	boton_vel_minima_menos.pressed.connect(func(): _ajustar_sensibilidad("vel_minima", -10.0, 0.0, 100000.0))
	boton_vel_minima_mas.pressed.connect(func(): _ajustar_sensibilidad("vel_minima", 10.0, 0.0, 100000.0))
	boton_vel_maxima_menos.pressed.connect(func(): _ajustar_sensibilidad("vel_maxima", -10.0, 0.0, 100000.0))
	boton_vel_maxima_mas.pressed.connect(func(): _ajustar_sensibilidad("vel_maxima", 10.0, 0.0, 100000.0))
	boton_velocidad_animacion_menos.focus_mode = Control.FOCUS_NONE
	boton_velocidad_animacion_mas.focus_mode = Control.FOCUS_NONE
	boton_velocidad_animacion_menos.pressed.connect(func(): _ajustar_sensibilidad("velocidad_animacion", -0.1, 0.2, 3.0))
	boton_velocidad_animacion_mas.pressed.connect(func(): _ajustar_sensibilidad("velocidad_animacion", 0.1, 0.2, 3.0))
	boton_viento_menos.focus_mode = Control.FOCUS_NONE
	boton_viento_mas.focus_mode = Control.FOCUS_NONE
	boton_viento_menos.pressed.connect(func(): _ajustar_sensibilidad("viento_sensibilidad", -0.1, 0.0, 3.0))
	boton_viento_mas.pressed.connect(func(): _ajustar_sensibilidad("viento_sensibilidad", 0.1, 0.0, 3.0))
	boton_viento.focus_mode = Control.FOCUS_NONE
	boton_viento.pressed.connect(_alternar_viento)
	_refrescar_panel_sensibilidad()

	boton_musica.focus_mode = Control.FOCUS_NONE
	boton_musica.pressed.connect(func():
		panel_musica.visible = not panel_musica.visible)

	boton_ils.focus_mode = Control.FOCUS_NONE
	boton_ils.pressed.connect(func():
		ils_activo = not ils_activo
		mundo.alternar_ils(ils_activo)
		boton_ils.text = "🎯 ILS: ON" if ils_activo else "🎯 Activar ILS")
	boton_genero_ambiental.focus_mode = Control.FOCUS_NONE
	boton_genero_orquestal.focus_mode = Control.FOCUS_NONE
	boton_genero_rock.focus_mode = Control.FOCUS_NONE
	boton_genero_electronica.focus_mode = Control.FOCUS_NONE
	boton_anterior_musica.focus_mode = Control.FOCUS_NONE
	boton_pausa_musica.focus_mode = Control.FOCUS_NONE
	boton_siguiente_musica.focus_mode = Control.FOCUS_NONE
	boton_detener_musica.focus_mode = Control.FOCUS_NONE
	boton_cerrar_musica.focus_mode = Control.FOCUS_NONE
	boton_genero_ambiental.pressed.connect(_reproducir_genero_musica.bind("Ambiental"))
	boton_genero_orquestal.pressed.connect(_reproducir_genero_musica.bind("Orquestal"))
	boton_genero_rock.pressed.connect(_reproducir_genero_musica.bind("Rock"))
	boton_genero_electronica.pressed.connect(_reproducir_genero_musica.bind("Electronica"))
	boton_anterior_musica.pressed.connect(_anterior_musica)
	boton_pausa_musica.pressed.connect(_alternar_pausa_musica)
	boton_siguiente_musica.pressed.connect(_reproducir_siguiente_musica)
	boton_detener_musica.pressed.connect(_detener_musica)
	boton_cerrar_musica.pressed.connect(func(): panel_musica.visible = false)
	# Pedido 2026-10-17 ("ponele una cruz arriba, no seas tan boludo -- el
	# botón de abajo me quedaba tapado por el panel de instrumentos"): cruz
	# fija arriba a la derecha del panel, afuera del VBox, así SIEMPRE está
	# a mano sin importar cuánto crezca la lista de temas de abajo.
	boton_cerrar_musica_x.focus_mode = Control.FOCUS_NONE
	boton_cerrar_musica_x.pressed.connect(func(): panel_musica.visible = false)
	sonido_musica.finished.connect(_reproducir_siguiente_musica)

	_cargar_volumen_motor()
	slider_volumen_motor.min_value = 0.0
	slider_volumen_motor.max_value = 1.0
	slider_volumen_motor.step = 0.01
	slider_volumen_motor.value = volumen_motor
	slider_volumen_motor.focus_mode = Control.FOCUS_NONE
	slider_volumen_motor.value_changed.connect(_cambiar_volumen_motor)
	sonido_motor.volume_db = lerp(VOLUMEN_MOTOR_DB_MINIMO, VOLUMEN_MOTOR_DB_MAXIMO, volumen_motor)
	_aplicar_sonido_motor(0)
	sonido_motor.play()

	_cargar_volumen_musica()
	slider_volumen_musica.min_value = 0.0
	slider_volumen_musica.max_value = 1.0
	slider_volumen_musica.step = 0.01
	slider_volumen_musica.value = volumen_musica
	slider_volumen_musica.focus_mode = Control.FOCUS_NONE
	slider_volumen_musica.value_changed.connect(_cambiar_volumen_musica)
	sonido_musica.volume_db = lerp(VOLUMEN_MUSICA_DB_MINIMO, VOLUMEN_MUSICA_DB_MAXIMO, volumen_musica)

	_cargar_volumen_radio()
	slider_volumen_radio.min_value = 0.0
	slider_volumen_radio.max_value = 1.0
	slider_volumen_radio.step = 0.01
	slider_volumen_radio.value = volumen_radio
	slider_volumen_radio.focus_mode = Control.FOCUS_NONE
	slider_volumen_radio.value_changed.connect(_cambiar_volumen_radio)
	sonido_radio.volume_db = lerp(VOLUMEN_RADIO_DB_MINIMO, VOLUMEN_RADIO_DB_MAXIMO, volumen_radio)

	_cargar_volumen_instrucciones()
	slider_volumen_instrucciones.min_value = 0.0
	slider_volumen_instrucciones.max_value = 1.0
	slider_volumen_instrucciones.step = 0.01
	slider_volumen_instrucciones.value = volumen_instrucciones
	slider_volumen_instrucciones.focus_mode = Control.FOCUS_NONE
	slider_volumen_instrucciones.value_changed.connect(_cambiar_volumen_instrucciones)
	sonido_instrucciones.volume_db = lerp(VOLUMEN_INSTRUCCIONES_DB_MINIMO, VOLUMEN_INSTRUCCIONES_DB_MAXIMO, volumen_instrucciones)

	# Sliders de balas/explosiones (pedido 2026-10-07) -- el panel de Sonidos
	# no tiene estos nodos en la escena todavía, así que se arman por código
	# y se agregan AL FINAL de la misma lista (VBoxSonidos), sin tocar ni
	# reemplazar nada existente -- el aviso de Pablo es real: ya pasó antes
	# que al agregar algo nuevo algo viejo "desaparecía" en estos paneles.
	var contenedor_sonidos: VBoxContainer = get_node("../HUD/PanelSonidos/ScrollSonidos/VBoxSonidos")
	_cargar_volumen_disparo()
	slider_volumen_disparo = _crear_fila_slider_volumen(contenedor_sonidos, "Volumen de disparos", volumen_disparo, _cambiar_volumen_disparo)
	sonido_disparo.volume_db = lerp(VOLUMEN_DISPARO_DB_MINIMO, VOLUMEN_DISPARO_DB_MAXIMO, volumen_disparo)
	_cargar_volumen_explosion()
	slider_volumen_explosion = _crear_fila_slider_volumen(contenedor_sonidos, "Volumen de explosiones", volumen_explosion, _cambiar_volumen_explosion)
	sonido_explosion.volume_db = lerp(VOLUMEN_EXPLOSION_DB_MINIMO, VOLUMEN_EXPLOSION_DB_MAXIMO, volumen_explosion)
	sonido_explosion_barco.volume_db = lerp(VOLUMEN_EXPLOSION_DB_MINIMO, VOLUMEN_EXPLOSION_DB_MAXIMO, volumen_explosion)

	boton_guardar_lugar.focus_mode = Control.FOCUS_NONE
	boton_cancelar_lugar.focus_mode = Control.FOCUS_NONE
	boton_guardar_lugar.pressed.connect(_guardar_lugar_marcado)
	boton_cancelar_lugar.pressed.connect(func():
		panel_marcar_lugar.visible = false)
	campo_nombre_lugar.text_submitted.connect(func(_texto): _guardar_lugar_marcado())
	_cargar_lugares_marcados()

	# El mapa de calles ahora es un panel lateral SIEMPRE visible (a la vez
	# que se vuela, pedido explícito) -- este botón solo lo esconde/muestra
	# para el que quiera más pantalla libre, no cambia nada del terreno.
	boton_mapa.focus_mode = Control.FOCUS_NONE
	boton_mapa.pressed.connect(func():
		mapa_rect.visible = not mapa_rect.visible)

	boton_city_tour.focus_mode = Control.FOCUS_NONE
	boton_city_tour.pressed.connect(_iniciar_city_tour)
	for i in range(1, 6):
		estrellas_city_tour_labels.append(get_node("../HUD/HBoxEstrellasCityTour/EstrellaCityTour%d" % i))
	sonido_bonus_city_tour = AudioStreamPlayer.new()
	sonido_bonus_city_tour.stream = _generar_sonido_bonus()
	add_child(sonido_bonus_city_tour)
	# Botón "T" (pedido 2026-09-28): transparenta el mapa al 30% para ver el
	# terreno de fondo a través, útil mientras se reacomoda/agranda el panel.
	boton_transparencia_mapa.focus_mode = Control.FOCUS_NONE
	boton_transparencia_mapa.pressed.connect(func():
		mapa_transparente = not mapa_transparente
		mapa_rect.modulate.a = 0.3 if mapa_transparente else 1.0)
	# Pedido explícito 2026-10-03: arranca transparente por defecto (así no
	# se ve un cuadrado blanco feo antes de que cargue la primera baldosa).
	mapa_rect.modulate.a = 0.3 if mapa_transparente else 1.0

	# Solapas del panel de instrumentos de abajo (pedido explícito 2026-10-03,
	# "como en Excel, página 1, página 2, página 3"): Página 1 = fotos
	# decorativas (primera impresión al entrar), Página 2 = Torre +
	# Sensibilidad (lo que ya había), Página 3 = Sonidos, Página 4 =
	# Configuración (ambas movidas acá el 2026-10-03 desde el viejo menú
	# flotante de PanelConfiguracion, que quedaba desprolijo con botones
	# flotando fuera del panel). Los botones son "toggle" pero se comportan
	# como radio (solo uno prendido a la vez) -- más simple que un
	# TabContainer real porque Torre/Sensibilidad no se mueven de lugar,
	# solo se esconden/muestran.
	boton_solapa_1.focus_mode = Control.FOCUS_NONE
	boton_solapa_2.focus_mode = Control.FOCUS_NONE
	boton_solapa_3.focus_mode = Control.FOCUS_NONE
	boton_solapa_4.focus_mode = Control.FOCUS_NONE
	boton_solapa_5.focus_mode = Control.FOCUS_NONE
	boton_solapa_1.pressed.connect(func(): _mostrar_solapa_inferior(1))
	boton_solapa_2.pressed.connect(func(): _mostrar_solapa_inferior(2))
	boton_solapa_3.pressed.connect(func(): _mostrar_solapa_inferior(3))
	boton_solapa_4.pressed.connect(func(): _mostrar_solapa_inferior(4))
	boton_solapa_5.pressed.connect(func(): _mostrar_solapa_inferior(5))
	# Pestaña nueva "Aterrizaje" + cartel de aterrizaje (pedido 2026-10-16).
	_crear_solapa_aterrizaje()
	_crear_cartel_aterrizaje()
	_crear_boton_modo_guerra()
	_crear_solapa_mision()
	_crear_solapa_destinos()

	# SEGUNDA VUELTA 2026-10-17 ("en vez del símbolo choto de pesos, poné el
	# ícono de Banco Nación y al lado el número en dólares -- y un segundo
	# cuadradito con el ícono de Mercado Pago, arranca en cero, el puente
	# para transferir lo hacemos después"): dos cajitas chicas, ícono real +
	# número estilo electrónico (mismos logos que ya tenés en la app de
	# gastos, reusados tal cual). Banco Nación = la plata de las misiones
	# (PerfilJugador.plata); Mercado Pago = pozo aparte, todavía sin
	# conexión entre los dos.
	# TERCERA VUELTA 2026-10-17 (pedido: "la caja de Banco Nación, del mismo
	# ancho que Mercado Pago, con botón más/menos -- perdí 420 pesos, así me
	# los repongo yo mismo"): se ensancha para que entren los ▼▲ (ver
	# parámetro nuevo con_editar de _crear_caja_dinero).
	var caja_banco_nacion := _crear_caja_dinero("res://iconos_dinero/banco-nacion.png", -240.0, -104.0, true)
	etiqueta_dinero = caja_banco_nacion
	var caja_mercado_pago := _crear_caja_dinero("res://iconos_dinero/mercado-pago.png", -384.0, -248.0)
	etiqueta_dinero_mp = caja_mercado_pago
	_actualizar_etiqueta_dinero()

	# CUARTA VUELTA 2026-10-17 (reportado: "el botón 'vista avión' de arriba
	# tapa unos botones que hay atrás -- dejá solo el icono del ojo, al lado
	# de Banco Nación"): se saca del renglón de arriba (compartido con la
	# cruz de cerrar/el reloj, de ahí el choque) y se lo deja chiquito, solo
	# el ícono, en el mismo renglón que las cajitas de dinero.
	boton_vista_avion = Button.new()
	boton_vista_avion.text = "👁"
	boton_vista_avion.anchors_preset = 1
	boton_vista_avion.anchor_left = 1.0
	boton_vista_avion.anchor_right = 1.0
	boton_vista_avion.offset_left = -100.0
	boton_vista_avion.offset_top = 32.0
	boton_vista_avion.offset_right = -72.0
	boton_vista_avion.offset_bottom = 54.0
	boton_vista_avion.grow_horizontal = 0
	boton_vista_avion.add_theme_font_size_override("font_size", 13)
	boton_vista_avion.focus_mode = Control.FOCUS_NONE
	boton_vista_avion.tooltip_text = "Oculta o muestra el dibujo del avión (útil para fotos/aterrizajes). Mismo efecto que la tecla V."
	boton_vista_avion.pressed.connect(_alternar_vista_sin_avion)
	get_node("../HUD").add_child(boton_vista_avion)

	# Pedido 2026-10-17 ("bajar del avión" + cuadradito + trackeo de rutas a
	# pie): mismo renglón que el ojo/las cajas de dinero, más a la izquierda
	# (texto más largo). _actualizar_boton_bajar_subir (llamada cada cuadro)
	# decide cuándo mostrarlo y qué dice.
	boton_bajar_subir = Button.new()
	boton_bajar_subir.text = "🚶 Bajar del avión"
	boton_bajar_subir.visible = false
	boton_bajar_subir.anchors_preset = 1
	boton_bajar_subir.anchor_left = 1.0
	boton_bajar_subir.anchor_right = 1.0
	# Mismo renglón que las cajitas de dinero/el ojo (offset_top 32-54), NO
	# el renglón de arriba (2-24, compartido con la cruz/reloj -- ahí fue
	# donde "vista avión" tapó algo hace poco). Se pone a la IZQUIERDA de
	# Mercado Pago (que arranca en -384), con margen de sobra.
	boton_bajar_subir.offset_left = -560.0
	boton_bajar_subir.offset_top = 32.0
	boton_bajar_subir.offset_right = -388.0
	boton_bajar_subir.offset_bottom = 54.0
	boton_bajar_subir.grow_horizontal = 0
	boton_bajar_subir.add_theme_font_size_override("font_size", 11)
	boton_bajar_subir.focus_mode = Control.FOCUS_NONE
	boton_bajar_subir.pressed.connect(func():
		if modo_a_pie:
			_subir_al_avion()
		else:
			_bajar_del_avion())
	get_node("../HUD").add_child(boton_bajar_subir)
	_cargar_ruta_trackeada()

	# Pedido 2026-10-22 ("no tengo F1/F2/F3 cómodas ni en el teclado
	# Bluetooth ni en la notebook, ponelo en un botón arriba a la vista"):
	# alterna entre el terreno fotorrealista de Google y el modo mapa plano
	# (terreno + foto aérea Bing, sin edificios 3D) -- mismo botón que antes
	# pensábamos en F9, ahora con un clic nomás.
	var boton_mapa_plano := Button.new()
	boton_mapa_plano.text = "🗺️ Mapa: Plano" if mundo.modo_mapa_plano else "🗺️ Mapa: Google"
	boton_mapa_plano.anchors_preset = 1
	boton_mapa_plano.anchor_left = 1.0
	boton_mapa_plano.anchor_right = 1.0
	# BUG REAL encontrado 2026-10-22 (reportado: "pisa el botón de RAM"): a
	# -736 se salía del ancho de pantalla reservado para la fila de botones
	# de la derecha y caía encima del panel de instrumentos (que vive pegado
	# a la izquierda). En vez de seguir sumando columnas hacia la izquierda,
	# va en un renglón PROPIO, debajo de "Bajar del avión" -- mismo rango
	# horizontal ya probado seguro, nunca se va a cruzar con nada.
	boton_mapa_plano.offset_left = -560.0
	boton_mapa_plano.offset_top = 56.0
	boton_mapa_plano.offset_right = -388.0
	boton_mapa_plano.offset_bottom = 78.0
	boton_mapa_plano.grow_horizontal = 0
	boton_mapa_plano.add_theme_font_size_override("font_size", 11)
	boton_mapa_plano.focus_mode = Control.FOCUS_NONE
	boton_mapa_plano.tooltip_text = "Alterna entre el terreno fotorrealista de Google (con edificios 3D) y el mapa plano liviano (terreno + foto aérea Bing, sin edificios)."
	boton_mapa_plano.pressed.connect(func():
		mundo.modo_mapa_plano = not mundo.modo_mapa_plano
		mundo._aplicar_modo_mapa(mundo.modo_mapa_plano)
		boton_mapa_plano.text = "🗺️ Mapa: Plano" if mundo.modo_mapa_plano else "🗺️ Mapa: Google")
	get_node("../HUD").add_child(boton_mapa_plano)
	# Pedido 2026-10-11 ("ese cuadro de diálogo flotante nació mal parido,
	# se ve horrible -- hacé una pestaña nueva abajo"). SEGUNDA VUELTA
	# (pedido: "lo trasplantaste con el error puesto -- tenés todo el ancho
	# del panel, no uses scroll, hacelo en fila"): en vez de mover el VBox
	# entero (vertical, pensado para una ventana angosta), se reparenta
	# CADA control individual -- misma instancia, misma lógica ya
	# conectada -- a una fila horizontal, cada uno con su etiqueta arriba.
	# "LabelTitulo" (el título viejo del popup) queda oculto -- ya hay uno
	# nuevo arriba de toda la pestaña.
	var vbox_viejo: VBoxContainer = selector_vuelo.get_node("VBox")
	vbox_viejo.get_node("LabelTitulo").visible = false
	var pares_campo := [
		["LabelAvion", "TipoAvionOption"],
		["LabelDesde", "OrigenOption"],
		["LabelHasta", "DestinoOption"],
	]
	for par in pares_campo:
		var etiqueta: Label = vbox_viejo.get_node(par[0])
		var control: Control = vbox_viejo.get_node(par[1])
		vbox_viejo.remove_child(etiqueta)
		vbox_viejo.remove_child(control)
		var columna := VBoxContainer.new()
		columna.add_theme_constant_override("separation", 2)
		columna.add_child(etiqueta)
		columna.add_child(control)
		contenedor_selector_vuelo.add_child(columna)
	# Checkbox y botón van sueltos, sin etiqueta arriba (ya se explican solos).
	for nombre_suelto in ["CheckVueloLibre", "BotonConfirmar"]:
		var nodo: Control = vbox_viejo.get_node(nombre_suelto)
		vbox_viejo.remove_child(nodo)
		nodo.size_flags_vertical = Control.SIZE_SHRINK_CENTER
		contenedor_selector_vuelo.add_child(nodo)
	# OJO: no se hace queue_free() de "selector_vuelo" ni de "vbox_viejo" --
	# otras partes del código todavía tocan "selector_vuelo.visible" más
	# adelante (al confirmar vuelo, etc.); tocar esa propiedad en una
	# instancia ya liberada tira error. Quedan vivos, vacíos y ocultos --
	# inofensivos.
	selector_vuelo.visible = false
	boton_elegir_vuelo.visible = false
	# Pedido 2026-10-11 ("el destino que sea predeterminado vuelo libre,
	# ahora no arreglemos ese desplegable, probemos aviones"): tildado y
	# apagado a la vista para que coincida con lo que realmente hace
	# _confirmar_viaje() ahora mismo (modo_libre forzado).
	check_vuelo_libre.button_pressed = true
	destino_option.disabled = true
	_mostrar_solapa_inferior(1)
	boton_mapa_auto.focus_mode = Control.FOCUS_NONE
	boton_mapa_auto.pressed.connect(func():
		mapa_zoom_automatico = not mapa_zoom_automatico
		boton_mapa_auto.text = "🔍 Zoom: Auto" if mapa_zoom_automatico else "🔍 Zoom: Manual"
		_tiempo_zoom_automatico = INTERVALO_ZOOM_AUTOMATICO)
	# Pedido 2026-10-17: arranca en Manual (el automático "nunca funcionó" --
	# queda a un toque de distancia con el mismo botón de siempre).
	mapa_zoom_automatico = false
	boton_mapa_auto.text = "🔍 Zoom: Manual"

	# Botón "Centrar" (pedido 2026-10-17, "si me pierdo arrastrando el mapa,
	# aprieto Centrar y me aparece con el puntito donde está el avión") --
	# mismo rincón que BotonTransparenciaMapa pero del otro lado, deja libre
	# la esquina inferior derecha (ahí vive AsaMapa, el tirador de resize).
	var boton_centrar_mapa := Button.new()
	boton_centrar_mapa.text = "📍 Centrar"
	boton_centrar_mapa.anchors_preset = 3
	boton_centrar_mapa.anchor_left = 1.0
	boton_centrar_mapa.anchor_top = 1.0
	boton_centrar_mapa.anchor_right = 1.0
	boton_centrar_mapa.anchor_bottom = 1.0
	boton_centrar_mapa.offset_left = -72.0
	boton_centrar_mapa.offset_top = -15.0
	boton_centrar_mapa.offset_right = -10.0
	boton_centrar_mapa.offset_bottom = -1.0
	boton_centrar_mapa.grow_horizontal = 0
	boton_centrar_mapa.grow_vertical = 0
	boton_centrar_mapa.add_theme_font_size_override("font_size", 9)
	boton_centrar_mapa.focus_mode = Control.FOCUS_NONE
	boton_centrar_mapa.tooltip_text = "Vuelve a centrar el mapa en la posición actual del avión (por si arrastraste el mapa y te perdiste)."
	boton_centrar_mapa.pressed.connect(func():
		mapa_siguiendo_avion = true
		_arrastrando_contenido_mapa = false
		tile_x_mapa = -999999
		acumulador_mapa = 999.0)
	mapa_rect.add_child(boton_centrar_mapa)

	for i in range(1, 7):
		var tarjeta: VBoxContainer = get_node("../HUD/PanelTorre/VBoxTorre/HBoxTorre/TarjetaTorre%d" % i)
		var fila := {
			"flecha": get_node("../HUD/PanelTorre/VBoxTorre/HBoxTorre/TarjetaTorre%d/FlechaTorre%d" % [i, i]),
			"nombre": get_node("../HUD/PanelTorre/VBoxTorre/HBoxTorre/TarjetaTorre%d/NombreTorre%d" % [i, i]),
			"distancia": get_node("../HUD/PanelTorre/VBoxTorre/HBoxTorre/TarjetaTorre%d/DistanciaTorre%d" % [i, i]),
			"boton": get_node("../HUD/PanelTorre/VBoxTorre/HBoxTorre/TarjetaTorre%d/BotonIlsTorre%d" % [i, i]),
		}
		fila["boton"].focus_mode = Control.FOCUS_NONE
		fila["boton"].disabled = true
		fila["boton"].pressed.connect(_alternar_ils_torre.bind(_filas_torre.size()))

		# Pedido 2026-10-17 ("encadenar viajes -- en cada aeropuerto cercano
		# un precio en verde con más/menos y un botón Ir, como ir parando el
		# colectivo"): se agregan a mano (no están en la escena) debajo del
		# botón de ILS de siempre, en la MISMA tarjeta -- ver _actualizar_
		# panel_torre más abajo, que llena estos controles cada refresco.
		# BUG REAL encontrado 2026-10-17 (reportado: "no aparece el nombre
		# del aeropuerto, solo distancia/precio/Ir"): NombreTorreN es el
		# ÚNICO control de la columna con size_flags_vertical=EXPAND -- al
		# agregarle 2 filas fijas más (precio y Ir, cada una en su propia
		# fila), no quedaba espacio sobrante para repartir y el nombre se
		# achicaba a 0 de alto. Ahora precio + ▼▲ + Ir van TODOS en una sola
		# fila (una fila menos de altura fija) + se le puso un alto mínimo
		# a NombreTorreN en la escena, así nunca más puede desaparecer.
		var fila_precio := HBoxContainer.new()
		fila_precio.add_theme_constant_override("separation", 2)
		var boton_menos_torre := Button.new()
		boton_menos_torre.text = "▼"
		boton_menos_torre.custom_minimum_size = Vector2(14, 18)
		boton_menos_torre.add_theme_font_size_override("font_size", 8)
		boton_menos_torre.focus_mode = Control.FOCUS_NONE
		boton_menos_torre.pressed.connect(_ajustar_precio_torre.bind(_filas_torre.size(), -25))
		fila_precio.add_child(boton_menos_torre)
		var etiqueta_precio_torre := Label.new()
		etiqueta_precio_torre.text = "$0"
		etiqueta_precio_torre.add_theme_font_size_override("font_size", 9)
		etiqueta_precio_torre.add_theme_color_override("font_color", Color(0.3, 1.0, 0.4))
		etiqueta_precio_torre.size_flags_horizontal = Control.SIZE_EXPAND_FILL
		etiqueta_precio_torre.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
		etiqueta_precio_torre.clip_text = true
		fila_precio.add_child(etiqueta_precio_torre)
		var boton_mas_torre := Button.new()
		boton_mas_torre.text = "▲"
		boton_mas_torre.custom_minimum_size = Vector2(14, 18)
		boton_mas_torre.add_theme_font_size_override("font_size", 8)
		boton_mas_torre.focus_mode = Control.FOCUS_NONE
		boton_mas_torre.pressed.connect(_ajustar_precio_torre.bind(_filas_torre.size(), 25))
		fila_precio.add_child(boton_mas_torre)
		var boton_ir_torre := Button.new()
		boton_ir_torre.text = "Ir"
		boton_ir_torre.custom_minimum_size = Vector2(22, 18)
		boton_ir_torre.add_theme_font_size_override("font_size", 9)
		boton_ir_torre.focus_mode = Control.FOCUS_NONE
		boton_ir_torre.disabled = true
		boton_ir_torre.pressed.connect(_ir_a_aeropuerto_torre.bind(_filas_torre.size()))
		fila_precio.add_child(boton_ir_torre)
		tarjeta.add_child(fila_precio)

		fila["etiqueta_precio"] = etiqueta_precio_torre
		fila["boton_ir"] = boton_ir_torre
		_filas_torre.append(fila)
	asa_mapa.gui_input.connect(_asa_mapa_gui_input)
	asa_mapa_sup_izq.gui_input.connect(_asa_mapa_sup_izq_gui_input)
	asa_mapa_sup_der.gui_input.connect(_asa_mapa_sup_der_gui_input)
	asa_mapa_inf_izq.gui_input.connect(_asa_mapa_inf_izq_gui_input)
	asa_mover_mapa.gui_input.connect(_asa_mover_mapa_gui_input)
	peticion_mapa.request_completed.connect(_on_peticion_mapa_completada)
	mapa_rect.gui_input.connect(_mapa_rect_gui_input)
	# BUG encontrado 2026-09-20: el ícono (dibujado en _draw() centrado en su
	# propio (0,0) local) giraba alrededor de (12,12) -- un punto que NO es su
	# centro real -- entonces en vez de rotar en el lugar "orbitaba" en un
	# círculo, dando la sensación de apuntar para cualquier lado según el
	# rumbo. El pivote tiene que coincidir con el centro real del dibujo: (0,0).
	icono_avion_mapa.pivot_offset = Vector2.ZERO
	# BUG encontrado 2026-09-20: el filtro de mouse por defecto de un
	# TextureRect (STOP) se comía el arrastre para agrandar el panel apenas
	# el mouse salía del huequito de la esquina (AsaMapa) -- el evento nunca
	# llegaba a _unhandled_input, que es donde vive la lógica de resize.
	# PASS deja que el evento siga viaje después de pasar por acá.
	mapa_rect.mouse_filter = Control.MOUSE_FILTER_PASS
	minimapa_rect.mouse_filter = Control.MOUSE_FILTER_PASS
	# Explícito a propósito (coincide con el default de Godot, pero lo dejamos
	# escrito para que quede claro y no dependa de un default implícito):
	# STRETCH_SCALE ajusta la textura al rectángulo real del control, así que
	# NUNCA puede desbordar el panel -- confirmado con Gemini y ChatGPT como
	# la combinación segura, después de que clip_contents + STRETCH_KEEP_
	# CENTERED (que NO recorta el dibujo propio del TextureRect, solo a sus
	# hijos) causara 3 bugs visuales distintos (ver mundo.gd / memoria del
	# proyecto sobre clip_contents).
	mapa_rect.stretch_mode = TextureRect.STRETCH_SCALE
	mapa_rect.clip_contents = false

	# La cámara principal NUNCA tiene que ver las baldosas del mapa de
	# calles (capa 5, ver mundo.gd) -- si no, se verían los dos terrenos
	# encimados en la vista de vuelo normal.
	camara.set_cull_mask_value(CAPA_RENDER_MAPA_CALLES, false)
	# Tampoco tiene que ver los marcadores propios (capa 6) -- esos los
	# dibuja la cámara de camara_marcadores.gd, sin el filtro de noche, y el
	# resultado se superpone encima (ver MarcadoresRect/MarcadoresViewport).
	camara.set_cull_mask_value(CAPA_MARCADORES_NOCTURNOS, false)
	marcadores_rect.texture = marcadores_viewport.get_texture()

	# Mismo motivo que arriba (foco de teclado robando el ESPACIO) -- estos
	# botones nuevos también necesitan FOCUS_NONE.
	option_calidad_graficos.focus_mode = Control.FOCUS_NONE
	_cargar_calidad_graficos()
	option_calidad_graficos.selected = calidad_graficos_actual
	_aplicar_calidad_graficos(calidad_graficos_actual)
	option_calidad_graficos.item_selected.connect(func(indice: int):
		calidad_graficos_actual = indice
		_aplicar_calidad_graficos(indice)
		_guardar_calidad_graficos())

	check_control_mouse.focus_mode = Control.FOCUS_NONE
	_cargar_control_mouse()
	check_control_mouse.button_pressed = control_mouse_activo
	check_control_mouse.toggled.connect(func(activo: bool):
		control_mouse_activo = activo
		_guardar_control_mouse())

	check_editor_luces.focus_mode = Control.FOCUS_NONE
	check_editor_luces.toggled.connect(func(activo: bool):
		modo_editar_luces = activo
		luz_arrastrada = null)
	boton_clavar_luces.focus_mode = Control.FOCUS_NONE
	boton_clavar_luces.pressed.connect(_guardar_ajuste_luces_editadas)

	boton_hora_menos.focus_mode = Control.FOCUS_NONE
	boton_hora_mas.focus_mode = Control.FOCUS_NONE
	check_avance_automatico_hora.focus_mode = Control.FOCUS_NONE
	check_avance_automatico_hora.button_pressed = mundo.avance_automatico_hora
	boton_hora_menos.pressed.connect(func():
		mundo.ajustar_hora_del_dia(-1.0)
		_actualizar_etiqueta_hora())
	boton_hora_mas.pressed.connect(func():
		mundo.ajustar_hora_del_dia(1.0)
		_actualizar_etiqueta_hora())
	check_avance_automatico_hora.toggled.connect(func(activo: bool):
		mundo.alternar_avance_automatico_hora(activo))
	_actualizar_etiqueta_hora()

	_cargar_mapeo_joystick()
	_actualizar_estado_joystick()
	_refrescar_lista_acciones_joystick()
	# Godot avisa con esta señal cada vez que se conecta/desconecta un
	# joystick (cable o Bluetooth) -- así el panel se actualiza solo, sin
	# tener que apretar nada.
	Input.joy_connection_changed.connect(func(_dispositivo, _conectado):
		_actualizar_estado_joystick())

	# BUG REAL encontrado 2026-09-20: el minimapa (cámara satelital mirando
	# hacia abajo) estaba armado pero JAMÁS mostró nada -- faltaba conectar
	# la textura del SubViewport a la imagen que la muestra en pantalla.
	minimapa_rect.texture = minimapa_viewport.get_texture()
	asa_minimapa.gui_input.connect(_asa_minimapa_gui_input)

func _asa_minimapa_gui_input(event: InputEvent) -> void:
	if event is InputEventMouseButton and event.button_index == MOUSE_BUTTON_LEFT:
		_arrastrando_minimapa = event.pressed

# Las 4 manijas comparten esta misma lógica de "click inicial": al presionar,
# guardamos dónde estaba el mouse y cómo era el panel en ESE instante exacto
# (_pos_mouse_inicio_mapa / _rect_mapa_inicio) -- todo el cálculo de tamaño en
# _input() se hace después contra esa foto inicial, nunca acumulando.
func _iniciar_resize_mapa(modo: int) -> void:
	_modo_resize_mapa = modo
	# get_global_mouse_position() ya devuelve la posición del mouse en el
	# sistema de coordenadas del CANVAS (el mismo que usan offset_left/top/
	# right/bottom, relativo al HUD) -- absorbe automáticamente cualquier
	# escala que aplique window/stretch/mode="canvas_items" (ver
	# project.godot). OJO: no hay que volver a multiplicarla por la
	# transformada propia de mapa_rect -- esa transformada CAMBIA mientras
	# arrastramos (es la posición actual del panel), y restarla metería de
	# vuelta un bucle inestable, exactamente lo que queremos evitar.
	_pos_mouse_inicio_mapa = mapa_rect.get_global_mouse_position()
	_rect_mapa_inicio = Rect2(mapa_rect.offset_left, mapa_rect.offset_top, mapa_rect.size.x, mapa_rect.size.y)

func _asa_mapa_gui_input(event: InputEvent) -> void:
	if event is InputEventMouseButton and event.button_index == MOUSE_BUTTON_LEFT:
		if event.pressed:
			_iniciar_resize_mapa(ModoResizeMapa.INF_DER)
		elif _modo_resize_mapa == ModoResizeMapa.INF_DER:
			_modo_resize_mapa = ModoResizeMapa.NINGUNO

func _asa_mapa_sup_izq_gui_input(event: InputEvent) -> void:
	if event is InputEventMouseButton and event.button_index == MOUSE_BUTTON_LEFT:
		if event.pressed:
			_iniciar_resize_mapa(ModoResizeMapa.SUP_IZQ)
		elif _modo_resize_mapa == ModoResizeMapa.SUP_IZQ:
			_modo_resize_mapa = ModoResizeMapa.NINGUNO

func _asa_mapa_sup_der_gui_input(event: InputEvent) -> void:
	if event is InputEventMouseButton and event.button_index == MOUSE_BUTTON_LEFT:
		if event.pressed:
			_iniciar_resize_mapa(ModoResizeMapa.SUP_DER)
		elif _modo_resize_mapa == ModoResizeMapa.SUP_DER:
			_modo_resize_mapa = ModoResizeMapa.NINGUNO

func _asa_mapa_inf_izq_gui_input(event: InputEvent) -> void:
	if event is InputEventMouseButton and event.button_index == MOUSE_BUTTON_LEFT:
		if event.pressed:
			_iniciar_resize_mapa(ModoResizeMapa.INF_IZQ)
		elif _modo_resize_mapa == ModoResizeMapa.INF_IZQ:
			_modo_resize_mapa = ModoResizeMapa.NINGUNO

# Calcula el nuevo tamaño del panel de mapa a partir de la distancia TOTAL
# recorrida por el mouse desde el click inicial (nunca acumulando delta a
# delta cuadro a cuadro) -- ver el comentario largo junto a ModoResizeMapa.
func _procesar_resize_mapa() -> void:
	var pos_actual: Vector2 = mapa_rect.get_global_mouse_position()
	var delta_mouse: Vector2 = pos_actual - _pos_mouse_inicio_mapa
	var der_fijo: float = _rect_mapa_inicio.position.x + _rect_mapa_inicio.size.x
	var bot_fijo: float = _rect_mapa_inicio.position.y + _rect_mapa_inicio.size.y
	match _modo_resize_mapa:
		ModoResizeMapa.INF_DER:
			# Ancla fija: esquina superior izquierda. Crece hacia abajo-derecha.
			var delta = (delta_mouse.x + delta_mouse.y) / 2.0
			var nuevo = clamp(_rect_mapa_inicio.size.x + delta, MAPA_TAMANO_MINIMO, MAPA_TAMANO_MAXIMO)
			mapa_rect.offset_right = _rect_mapa_inicio.position.x + nuevo
			mapa_rect.offset_bottom = _rect_mapa_inicio.position.y + nuevo
		ModoResizeMapa.SUP_IZQ:
			# Ancla fija: esquina inferior derecha. Crece hacia arriba-izquierda.
			var delta = -(delta_mouse.x + delta_mouse.y) / 2.0
			var nuevo = clamp(_rect_mapa_inicio.size.x + delta, MAPA_TAMANO_MINIMO, MAPA_TAMANO_MAXIMO)
			mapa_rect.offset_left = der_fijo - nuevo
			mapa_rect.offset_top = bot_fijo - nuevo
		ModoResizeMapa.SUP_DER:
			# Ancla fija: esquina inferior izquierda. Crece hacia arriba-derecha.
			var delta = (delta_mouse.x - delta_mouse.y) / 2.0
			var nuevo = clamp(_rect_mapa_inicio.size.x + delta, MAPA_TAMANO_MINIMO, MAPA_TAMANO_MAXIMO)
			mapa_rect.offset_right = _rect_mapa_inicio.position.x + nuevo
			mapa_rect.offset_top = bot_fijo - nuevo
		ModoResizeMapa.INF_IZQ:
			# Ancla fija: esquina superior derecha. Crece hacia abajo-izquierda.
			var delta = (delta_mouse.y - delta_mouse.x) / 2.0
			var nuevo = clamp(_rect_mapa_inicio.size.x + delta, MAPA_TAMANO_MINIMO, MAPA_TAMANO_MAXIMO)
			mapa_rect.offset_left = der_fijo - nuevo
			mapa_rect.offset_bottom = _rect_mapa_inicio.position.y + nuevo

func _asa_mover_mapa_gui_input(event: InputEvent) -> void:
	if event is InputEventMouseButton and event.button_index == MOUSE_BUTTON_LEFT:
		_arrastrando_mover_mapa = event.pressed

func _actualizar_etiqueta_velocidad() -> void:
	etiqueta_velocidad.text = "VEL\n%d" % int(round(velocidad_actual))

func _actualizar_etiqueta_hora() -> void:
	var horas: int = int(mundo.hora_del_dia)
	var minutos: int = int(round((mundo.hora_del_dia - horas) * 60.0))
	if minutos == 60:
		minutos = 0
		horas = (horas + 1) % 24
	label_hora_valor.text = "%02d:%02d" % [horas, minutos]

func _actualizar_estado_joystick() -> void:
	var conectados = Input.get_connected_joypads()
	if conectados.size() > 0:
		joystick_id = conectados[0]
		etiqueta_estado_joystick.text = "Joystick: %s (conectado)" % Input.get_joy_name(joystick_id)
	else:
		joystick_id = -1
		etiqueta_estado_joystick.text = "Joystick: no detectado"

func _iniciar_asignacion_joystick(nombre_accion: String) -> void:
	esperando_asignacion = nombre_accion
	_refrescar_lista_acciones_joystick()

func _input(event: InputEvent) -> void:
	# Editor de luces en vivo (ver comentario junto a "modo_editar_luces"):
	# clic izquierdo cerca de una lucecita la agarra, arrastrar la mueve sobre
	# un plano paralelo a la pantalla a la misma profundidad donde estaba,
	# soltar la suelta. Tiene que ir ANTES que cualquier otra lógica de clic
	# (por el mismo motivo que el arrastre del mapa usa _input y no
	# _unhandled_input, ver comentario de más abajo).
	if modo_editar_luces:
		if event is InputEventMouseButton and event.button_index == MOUSE_BUTTON_LEFT:
			if event.pressed:
				var origen: Vector3 = camara.project_ray_origin(event.position)
				var direccion: Vector3 = camara.project_ray_normal(event.position)
				var mejor_luz: MeshInstance3D = null
				var mejor_distancia: float = 2.0
				for luz in [luz_navegacion_izq_externa, luz_navegacion_der_externa, luz_estroboscopica_externa]:
					if luz == null or not is_instance_valid(luz):
						continue
					var al_punto: Vector3 = luz.global_position - origen
					var proyeccion: float = al_punto.dot(direccion)
					if proyeccion < 0.0:
						continue
					var distancia: float = (origen + direccion * proyeccion).distance_to(luz.global_position)
					if distancia < mejor_distancia:
						mejor_distancia = distancia
						mejor_luz = luz
				luz_arrastrada = mejor_luz
				if mejor_luz:
					var al_punto_elegido: Vector3 = mejor_luz.global_position - origen
					profundidad_arrastre_luz = al_punto_elegido.dot(direccion)
			else:
				luz_arrastrada = null
			return
		if event is InputEventMouseMotion and luz_arrastrada and is_instance_valid(luz_arrastrada):
			var origen: Vector3 = camara.project_ray_origin(event.position)
			var direccion: Vector3 = camara.project_ray_normal(event.position)
			luz_arrastrada.global_position = origen + direccion * profundidad_arrastre_luz
			return

	# BUG encontrado 2026-09-20: usar _unhandled_input hacía que el arrastre
	# del vértice se cortara apenas el mouse pasaba por ENCIMA de cualquier
	# Control con filtro STOP (el valor por defecto en Godot) -- el evento
	# quedaba "consumido" por la GUI antes de llegar acá. _input() recibe
	# TODOS los eventos primero, antes de que la GUI los toque, así que el
	# arrastre ya no depende de qué Control tenga el mouse encima en cada
	# instante del movimiento.

	# F11 alterna pantalla completa/ventana -- el juego arranca en pantalla
	# completa por defecto (pedido explícito 2026-09-20), pero conviene
	# poder volver a ventana sin cerrar el juego (por ej. para algo puntual
	# de testeo, o si la pantalla completa da algún problema).
	if event is InputEventKey and event.pressed and not event.echo and event.physical_keycode == KEY_F11:
		if DisplayServer.window_get_mode() == DisplayServer.WINDOW_MODE_FULLSCREEN:
			DisplayServer.window_set_mode(DisplayServer.WINDOW_MODE_WINDOWED)
		else:
			DisplayServer.window_set_mode(DisplayServer.WINDOW_MODE_FULLSCREEN)
		return

	# DEBUG TEMPORAL 2026-10-11 (diagnóstico "se ve la luz, no se ve el
	# casco" de los barcos, sugerido por Gemini) -- K imprime en la consola
	# de Godot el estado real del barco más cercano (posición, visibilidad,
	# AABB de cada malla); J apaga/prende el terreno de Cesium para ver si
	# el casco estaba enterrado bajo la malla del tileset. Teclas de letra,
	# no de función (el teclado de Pablo no tiene F9/F10/etc). Sacar los dos
	# bloques cuando el problema de los barcos quede resuelto.
	if event is InputEventKey and event.pressed and not event.echo and event.physical_keycode == KEY_K:
		_diagnosticar_barco_mas_cercano()
		return
	if event is InputEventKey and event.pressed and not event.echo and event.physical_keycode == KEY_J:
		if mundo and mundo.tileset:
			mundo.tileset.visible = not mundo.tileset.visible
			print("🌍 Cesium3DTileset visible = ", mundo.tileset.visible)
		return
	# DEBUG TEMPORAL 2026-10-11 (pedido de Pablo: "prefiero que actives unos
	# cuantos barcos lejanos, para ver si llegan las balas, sin tener que
	# acertarle primero"): H activa el contraataque de los 3 barcos de la
	# flota real MÁS LEJANOS al avión, sin gastarles vida ni necesitar
	# impactarlos -- así se puede ver si las balas realmente salen y llegan,
	# con tiempo de reacción de sobra. Sacar cuando quede confirmado.
	if event is InputEventKey and event.pressed and not event.echo and event.physical_keycode == KEY_H:
		_activar_contraataque_de_prueba()
		return
	# DEBUG TEMPORAL 2026-10-11 (boceto de invasión): los barcos reales
	# tardan ~50 minutos en llegar solos a la costa a su velocidad normal
	# -- G fuerza el desembarco del barco más cercano al avión AHORA MISMO,
	# para probar el sistema de invasión sin esperar. Sacar cuando el
	# desembarco por tiempo real ya esté probado y ajustado.
	if event is InputEventKey and event.pressed and not event.echo and event.physical_keycode == KEY_G:
		_forzar_desembarco_de_prueba()
		return
	# DEBUG TEMPORAL 2026-10-11 (reportado: "el A380 sigue invisible aunque
	# ya lo achiqué" -- segunda vez que pasa, así que en vez de seguir
	# adivinando un número de escala, P imprime en consola el estado REAL
	# del modelo que está puesto ahora mismo: su AABB (¿está centrado en el
	# pivote, o lejos, como nos pasó con el aquitaine?), si Godot lo
	# considera visible, y dónde está la cámara respecto a él. Mismo
	# método que usamos para los barcos.
	if event is InputEventKey and event.pressed and not event.echo and event.physical_keycode == KEY_P:
		_diagnosticar_modelo_avion_actual()
		return
	# TERCERA VUELTA 2026-10-11 (reportado: "aprieto X y van rotando entre
	# distintos, ninguno queda derecho -- no me compliques con tantas
	# teclas"): UNA sola tecla, UN tanque y UN Jaguar siempre en el MISMO
	# lugar fijo en Aeroparque, mirando siempre para el mismo lado (ver
	# _prueba_vehiculos_fija) -- apretarla de nuevo solo actualiza esos
	# mismos dos vehículos con los valores de escala/roll actuales, nunca
	# elige otros. El reporte de altura aparece en letras grandes arriba de
	# la pantalla, no hace falta buscar la consola.
	if event is InputEventKey and event.pressed and not event.echo and event.physical_keycode == KEY_X:
		_prueba_vehiculos_fija()
		return
	# BUG REAL encontrado 2026-10-17 (reportado: "estoy haciendo una misión
	# civil y de repente aparece 'hundir barcos', nunca me deja terminar la
	# mía" -- causa real: las teclas Q y 1, de cuando recién armamos el
	# esqueleto de misiones, seguían activas. Si se tocaban sin querer
	# mientras volaba, pisaban silenciosamente la misión en curso -- el
	# cartel se veía solo 3.5s y desaparecía, pero el estado interno de la
	# misión YA había sido reemplazado para siempre, aunque pareciera que
	# "no pasó nada"). Ya existe la pestaña "Misión" con botones para todo
	# esto, así que estas teclas de prueba no hacen más falta -- sacadas.

	# Agrandar/achicar el minimapa o el panel de mapa arrastrando su esquina
	# (pedido explícito, "como en CorelDraw"). El minimapa queda pegado
	# abajo a la derecha (crece para arriba/izquierda); el mapa de calles
	# queda pegado a la izquierda, centrado verticalmente (crece a la
	# derecha y para los dos lados verticalmente).
	# BUG REAL encontrado 2026-09-21 (reportado: "agarro el vértice y se pone
	# solo mitad pantalla, y ni reseteándolo con la tecla U se arregla"): si
	# soltás el botón del mouse afuera de la ventana del juego (foco perdido,
	# arrastre hasta el borde de la pantalla), Godot nunca recibe el evento
	# de "botón soltado" y la bandera de arrastre queda trabada en `true`
	# para siempre -- a partir de ahí CUALQUIER movimiento del mouse (incluso
	# uno normal volando) seguía agrandando el panel sin que nadie lo tocara,
	# y ni resetear los offsets con la tecla U servía porque en el frame
	# siguiente el primer movimiento del mouse lo volvía a agrandar. Esta
	# chequeada cada cuadro corta el arrastre apenas el botón deja de estar
	# físicamente apretado, sin depender de que llegue ningún evento.
	if _arrastrando_minimapa and not Input.is_mouse_button_pressed(MOUSE_BUTTON_LEFT):
		_arrastrando_minimapa = false
	if _modo_resize_mapa != ModoResizeMapa.NINGUNO and not Input.is_mouse_button_pressed(MOUSE_BUTTON_LEFT):
		_modo_resize_mapa = ModoResizeMapa.NINGUNO
	if _arrastrando_mover_mapa and not Input.is_mouse_button_pressed(MOUSE_BUTTON_LEFT):
		_arrastrando_mover_mapa = false
	if _arrastrando_contenido_mapa and not Input.is_mouse_button_pressed(MOUSE_BUTTON_LEFT):
		_arrastrando_contenido_mapa = false

	if _arrastrando_contenido_mapa:
		if event is InputEventMouseMotion:
			# Arrastre del contenido (no del panel): desplaza el "centro"
			# propio del mapa en grados, convirtiendo el movimiento en
			# píxeles a fracción de tesela con el zoom actual -- mismas
			# fórmulas slippy-map que usa _actualizar_mapa_calles, pero
			# invertidas para la latitud (Mercator no es lineal en Y).
			var n: float = pow(2.0, zoom_mapa)
			mapa_centro_lon -= (event.relative.x / mapa_rect.size.x) * (360.0 / n)
			var lat_rad: float = deg_to_rad(mapa_centro_lat)
			var y_exacto: float = (1.0 - log(tan(lat_rad) + 1.0 / cos(lat_rad)) / PI) / 2.0 * n
			y_exacto -= event.relative.y / mapa_rect.size.y
			var y_frac: float = y_exacto / n
			mapa_centro_lat = rad_to_deg(atan(sinh(PI * (1.0 - 2.0 * y_frac))))
			tile_x_mapa = -999999  # fuerza a re-evaluar qué tesela pedir ya mismo
			acumulador_mapa = 999.0
			return
		elif event is InputEventMouseButton and not event.pressed:
			_arrastrando_contenido_mapa = false
			return

	if _arrastrando_minimapa:
		if event is InputEventMouseMotion:
			var delta = -(event.relative.x + event.relative.y) / 2.0
			var nuevo = clamp(minimapa_rect.size.x + delta, MINIMAPA_TAMANO_MINIMO, MINIMAPA_TAMANO_MAXIMO)
			minimapa_rect.offset_left = -20.0 - nuevo
			minimapa_rect.offset_top = -20.0 - nuevo
			minimapa_viewport.size = Vector2i(int(nuevo), int(nuevo))
			return
		elif event is InputEventMouseButton and not event.pressed:
			_arrastrando_minimapa = false
			return
	if _modo_resize_mapa != ModoResizeMapa.NINGUNO:
		if event is InputEventMouseMotion:
			_procesar_resize_mapa()
			return
		elif event is InputEventMouseButton and not event.pressed:
			_modo_resize_mapa = ModoResizeMapa.NINGUNO
			return
	if _arrastrando_mover_mapa:
		if event is InputEventMouseMotion:
			# Traslada el panel entero (los 4 offsets juntos) -- a diferencia
			# de AsaMapa (que solo cambia el TAMAÑO), esto mueve la POSICIÓN
			# sin tocar cuánto mide.
			mapa_rect.offset_left += event.relative.x
			mapa_rect.offset_right += event.relative.x
			mapa_rect.offset_top += event.relative.y
			mapa_rect.offset_bottom += event.relative.y
			return
		elif event is InputEventMouseButton and not event.pressed:
			_arrastrando_mover_mapa = false
			return

	# ESCAPE también cierra estos paneles (además de sus propios botones), y
	# si no hay nada abierto, sale de pantalla completa -- pedido explícito
	# (en pantalla completa no hay cruz para cerrar ni bordes de ventana, así
	# que ESCAPE tiene que poder sacarte de ahí como en cualquier programa).
	if event.is_action_pressed("ui_cancel") and esperando_asignacion == "":
		if panel_carrera.visible:
			panel_carrera.visible = false
			return
		if panel_marcar_lugar.visible:
			panel_marcar_lugar.visible = false
			return
		if panel_misiones.visible:
			panel_misiones.visible = false
			return
		if selector_vuelo.visible:
			selector_vuelo.visible = false
			return
		if DisplayServer.window_get_mode() == DisplayServer.WINDOW_MODE_FULLSCREEN:
			DisplayServer.window_set_mode(DisplayServer.WINDOW_MODE_WINDOWED)
			return
	if esperando_asignacion == "":
		return
	if event is InputEventJoypadButton and event.pressed:
		if esperando_asignacion == "mira_horizontal" or esperando_asignacion == "mira_vertical":
			cartel_central.text = "La mira necesita un eje del joystick"
			cartel_central.visible = true
			get_tree().create_timer(1.5).timeout.connect(func(): cartel_central.visible = false)
			return
		mapeo_joystick[esperando_asignacion] = {"tipo": "boton", "indice": event.button_index}
		esperando_asignacion = ""
		_guardar_mapeo_joystick()
		_refrescar_lista_acciones_joystick()
	elif event is InputEventJoypadMotion and abs(event.axis_value) > DEADZONE_EJE_JOYSTICK:
		mapeo_joystick[esperando_asignacion] = {"tipo": "eje", "indice": event.axis, "signo": signf(event.axis_value)}
		esperando_asignacion = ""
		_guardar_mapeo_joystick()
		_refrescar_lista_acciones_joystick()

func _texto_mapeo_joystick(nombre_accion: String) -> String:
	if esperando_asignacion == nombre_accion:
		return "Presioná algo..."
	if not mapeo_joystick.has(nombre_accion):
		return "Sin asignar"
	var m = mapeo_joystick[nombre_accion]
	if m["tipo"] == "boton":
		return "Botón %d" % m["indice"]
	return "Eje %d (%s)" % [m["indice"], "+" if m["signo"] > 0 else "-"]

func _refrescar_lista_acciones_joystick() -> void:
	for hijo in lista_acciones_joystick.get_children():
		hijo.queue_free()
	for par in ACCIONES_JOYSTICK:
		var nombre_accion: String = par[0]
		var etiqueta_texto: String = par[1]
		var fila = HBoxContainer.new()

		var label = Label.new()
		label.text = etiqueta_texto
		label.custom_minimum_size = Vector2(230, 0)
		# BUG REAL encontrado 2026-09-27 (reportado: "el botón Asignar no
		# aparece, quedó fuera de la pantalla"): sin recorte, un texto más
		# largo que 230px hacía crecer el Label horizontalmente, empujando
		# el botón "Asignar" fuera del panel. Con autowrap, un texto largo
		# simplemente ocupa más líneas hacia ABAJO en vez de invadir el
		# ancho de al lado -- pase lo que pase con el largo del texto, el
		# botón nunca se vuelve a mover.
		label.autowrap_mode = TextServer.AUTOWRAP_WORD_SMART
		label.size_flags_horizontal = Control.SIZE_SHRINK_BEGIN
		label.add_theme_font_size_override("font_size", 13)
		fila.add_child(label)

		var label_estado = Label.new()
		label_estado.text = _texto_mapeo_joystick(nombre_accion)
		label_estado.custom_minimum_size = Vector2(110, 0)
		label_estado.add_theme_font_size_override("font_size", 13)
		var tiene_mapeo = mapeo_joystick.has(nombre_accion)
		label_estado.add_theme_color_override("font_color", Color(0.6, 1, 0.6) if tiene_mapeo else Color(0.7, 0.7, 0.7))
		fila.add_child(label_estado)

		var boton = Button.new()
		boton.text = "Asignar"
		boton.focus_mode = Control.FOCUS_NONE
		boton.pressed.connect(_iniciar_asignacion_joystick.bind(nombre_accion))
		fila.add_child(boton)

		lista_acciones_joystick.add_child(fila)

func _guardar_mapeo_joystick() -> void:
	var cfg = ConfigFile.new()
	for nombre_accion in mapeo_joystick:
		cfg.set_value("mapeo", nombre_accion, mapeo_joystick[nombre_accion])
	cfg.save(RUTA_CONFIG_JOYSTICK)

func _cargar_mapeo_joystick() -> void:
	var cfg = ConfigFile.new()
	if cfg.load(RUTA_CONFIG_JOYSTICK) != OK:
		return
	for nombre_accion in cfg.get_section_keys("mapeo"):
		mapeo_joystick[nombre_accion] = cfg.get_value("mapeo", nombre_accion)

func _reproducir_genero_musica(genero: String) -> void:
	genero_musica_actual = genero
	playlist_musica.clear()
	var carpeta: String = CARPETAS_MUSICA[genero]
	var dir := DirAccess.open(carpeta)
	if dir:
		dir.list_dir_begin()
		var archivo := dir.get_next()
		while archivo != "":
			if not dir.current_is_dir() and archivo.to_lower().ends_with(".mp3"):
				playlist_musica.append(carpeta + "/" + archivo)
			archivo = dir.get_next()
		dir.list_dir_end()
	# Orden alfabético fijo (SIN shuffle) -- pedido explícito 2026-09-27:
	# "cuando pongo electrónica siempre tira el primero, quiero poder elegir
	# de una lista". Con orden fijo, la lista que se ve coincide siempre con
	# lo que realmente suena; antes se mezclaba al azar cada vez.
	playlist_musica.sort()
	if playlist_musica.is_empty():
		label_estado_musica.text = "No encontré temas en esa carpeta"
		scroll_lista_temas.visible = false
		return
	_refrescar_lista_temas_musica()
	scroll_lista_temas.visible = true
	label_estado_musica.text = "%s: elegí un tema de la lista" % genero_musica_actual

# Lista de botones, uno por tema, dentro del panel de música (pedido
# explícito 2026-09-27, "para no tener que ir pasando de a uno con
# siguiente cuando tenga 30 canciones cargadas"). Al elegir uno, arranca
# ESE tema y la reproducción automática sigue de ahí en más, en el mismo
# orden de la lista.
func _refrescar_lista_temas_musica() -> void:
	# BUG REAL encontrado 2026-09-27 (reportado con foto: "letrones, no se ve
	# el nombre completo, quiero que entren todas sin scroll salvo que la
	# lista sea muy larga"): los botones usaban el tamaño de fuente default
	# (grande) sin recortar texto largo. Achicado + clip_text para que el
	# nombre se corte con "..." en vez de desbordar, y filas más bajas para
	# que entren más de una vez en el mismo espacio visible.
	for hijo in lista_temas_musica.get_children():
		hijo.queue_free()
	for i in range(playlist_musica.size()):
		var boton := Button.new()
		boton.text = playlist_musica[i].get_file().trim_suffix(".mp3")
		boton.focus_mode = Control.FOCUS_NONE
		boton.custom_minimum_size = Vector2(0, 20)
		boton.add_theme_font_size_override("font_size", 11)
		boton.clip_text = true
		boton.alignment = HORIZONTAL_ALIGNMENT_LEFT
		boton.pressed.connect(_seleccionar_tema_musica.bind(i))
		lista_temas_musica.add_child(boton)

func _seleccionar_tema_musica(indice: int) -> void:
	indice_musica = indice
	_reproducir_siguiente_musica()

const TAMANO_MAXIMO_TEMA_MUSICA := 100 * 1024 * 1024  # 100MB -- ningún tema dura tanto; si pesa más, algo anda mal con el archivo

func _reproducir_siguiente_musica() -> void:
	if playlist_musica.is_empty():
		return
	# BUG REAL encontrado 2026-09-27 (reportado: "agregué temas míos a la
	# carpeta y no suenan, los viejos tenían un archivito de 1kb al lado que
	# los nuevos no tienen"): load(ruta) pasa por el sistema de import de
	# Godot, que necesita ese archivo ".import" -- se genera solo cuando el
	# EDITOR ve el archivo por primera vez, nunca si se copia un mp3 nuevo
	# directo por Windows y se juega sin pasar por el editor antes. Leyendo
	# los bytes crudos del archivo a mano (FileAccess) y armando el
	# AudioStreamMP3 directamente evita el sistema de import por completo --
	# cualquier mp3 que se copie a la carpeta suena al toque, sin depender
	# de que el editor lo haya "notado" antes.
	#
	# GUARDIA 2026-10-17 (reportado: "le puse música y se cerró el
	# programa"): un archivo vacío, corrupto, o demasiado pesado para la poca
	# RAM de esta PC puede tirar abajo el decodificador de audio del motor
	# ENTERO, no solo esta función -- en vez de arriesgarse a abrir ESE
	# archivo, se lo descarta por tamaño antes de tocarlo y se sigue de largo
	# al próximo tema de la lista (probamos como máximo toda la lista una
	# vez, para no colgarse en bucle si todos fallan).
	var intentos: int = 0
	while intentos < playlist_musica.size():
		if indice_musica >= playlist_musica.size():
			indice_musica = 0
		var ruta: String = playlist_musica[indice_musica]
		indice_musica += 1
		intentos += 1
		var bytes: PackedByteArray = FileAccess.get_file_as_bytes(ruta)
		if bytes.size() < 100 or bytes.size() > TAMANO_MAXIMO_TEMA_MUSICA:
			push_warning("Tema descartado (vacío/corrupto/demasiado pesado): %s" % ruta)
			continue
		var mp3 := AudioStreamMP3.new()
		mp3.data = bytes
		sonido_musica.stream = mp3
		sonido_musica.stream_paused = false
		sonido_musica.play()
		boton_pausa_musica.text = "⏸ Pausa"
		label_estado_musica.text = "%s: %s" % [genero_musica_actual, ruta.get_file().trim_suffix(".mp3")]
		return
	label_estado_musica.text = "%s: ningún tema de la lista se pudo reproducir" % genero_musica_actual

# Retrocede UN tema (pedido 2026-09-21, "por si te gustó y querés
# repetir"). _reproducir_siguiente_musica() ya deja indice_musica apuntando
# al tema DESPUÉS del que está sonando -- retroceder 2 y dejar que esa misma
# función avance 1 de vuelta es más simple que duplicar la lógica de carga.
func _anterior_musica() -> void:
	if playlist_musica.is_empty():
		return
	indice_musica -= 2
	while indice_musica < 0:
		indice_musica += playlist_musica.size()
	_reproducir_siguiente_musica()

func _alternar_pausa_musica() -> void:
	if not sonido_musica.playing and not sonido_musica.stream_paused:
		return
	sonido_musica.stream_paused = not sonido_musica.stream_paused
	boton_pausa_musica.text = "▶ Reanudar" if sonido_musica.stream_paused else "⏸ Pausa"

func _detener_musica() -> void:
	sonido_musica.stop()
	playlist_musica.clear()
	genero_musica_actual = ""
	label_estado_musica.text = "Detenida"
	boton_pausa_musica.text = "⏸ Pausa"
	scroll_lista_temas.visible = false

func _abrir_panel_carrera() -> void:
	panel_carrera.visible = true
	if PerfilJugador.hay_perfil_cargado():
		_mostrar_credencial()
	else:
		_volver_a_seleccion_perfil()

func _volver_a_seleccion_perfil() -> void:
	panel_credencial.visible = false
	panel_seleccion_perfil.visible = true
	campo_nuevo_perfil.text = ""
	_refrescar_lista_perfiles()

func _refrescar_lista_perfiles() -> void:
	for hijo in lista_perfiles.get_children():
		hijo.queue_free()
	for nombre in PerfilJugador.listar_perfiles():
		var boton := Button.new()
		boton.text = "👤 %s" % nombre
		boton.custom_minimum_size = Vector2(0, 34)
		boton.focus_mode = Control.FOCUS_NONE
		boton.pressed.connect(_elegir_perfil.bind(nombre))
		lista_perfiles.add_child(boton)

func _elegir_perfil(nombre: String) -> void:
	if PerfilJugador.cargar_perfil(nombre):
		_mostrar_credencial()

func _crear_perfil_desde_campo() -> void:
	var nombre := campo_nuevo_perfil.text.strip_edges()
	if nombre == "":
		return
	if PerfilJugador.listar_perfiles().has(nombre):
		# Ya existe un perfil con ese nombre -- lo cargamos en vez de pisarlo,
		# así nadie borra sin querer la billetera/horas de otra persona.
		PerfilJugador.cargar_perfil(nombre)
	else:
		PerfilJugador.crear_perfil(nombre)
	_mostrar_credencial()

func _mostrar_credencial() -> void:
	panel_seleccion_perfil.visible = false
	panel_credencial.visible = true
	_refrescar_credencial()

func _refrescar_credencial() -> void:
	label_nombre_piloto.text = PerfilJugador.nombre_perfil_actual
	var rango: Dictionary = PerfilJugador.obtener_rango()
	label_rango.text = rango["nombre"]
	insignia_piloto.fijar_rango(rango["barras"], rango["estrella"])
	label_horas_valor.text = "%d Hs" % int(PerfilJugador.perfil_actual.get("horas_vuelo", 0.0))
	label_plata_valor_carrera.text = "$%d" % int(PerfilJugador.perfil_actual.get("plata", 0))
	_poblar_lista_licencia_helicoptero()

# Arma las 3 filas de la licencia de helicóptero -- nombre, precio editable
# con ▲▼ (pedido explícito, "todo lo que tenga precio en pesos siempre el
# triangulito") y un botón "Volar" (o un tilde si ya está hecha). Se
# reconstruye entera cada vez que se abre/actualiza la credencial -- son
# solo 3 filas, no vale la pena complicarse con actualizar en el lugar.
func _poblar_lista_licencia_helicoptero() -> void:
	for hijo in lista_licencia_helicoptero.get_children():
		hijo.queue_free()

	var total: int = MISIONES_LICENCIA_HELICOPTERO.size()
	var progreso: Array = PerfilJugador.progreso_licencia(NOMBRE_LICENCIA_HELICOPTERO, total)
	var hechas: int = 0
	for hecha in progreso:
		if hecha:
			hechas += 1

	if PerfilJugador.licencia_completa(NOMBRE_LICENCIA_HELICOPTERO, total):
		label_progreso_licencia.text = "✅ Licencia obtenida -- %d/%d viajes hechos" % [hechas, total]
	else:
		label_progreso_licencia.text = "Progreso: %d/%d" % [hechas, total]

	for i in total:
		var datos = MISIONES_LICENCIA_HELICOPTERO[i]
		var fila = HBoxContainer.new()
		fila.add_theme_constant_override("separation", 4)

		var etiqueta_nombre = Label.new()
		etiqueta_nombre.text = ("✅ " if progreso[i] else "▶ ") + datos["nombre"]
		etiqueta_nombre.add_theme_font_size_override("font_size", 12)
		etiqueta_nombre.custom_minimum_size = Vector2(150, 0)
		etiqueta_nombre.autowrap_mode = TextServer.AUTOWRAP_WORD
		etiqueta_nombre.add_theme_color_override("font_color", Color(0.6, 1, 0.6) if progreso[i] else Color(1, 1, 1))
		fila.add_child(etiqueta_nombre)

		var boton_menos = Button.new()
		boton_menos.text = "－"
		boton_menos.custom_minimum_size = Vector2(24, 24)
		boton_menos.focus_mode = Control.FOCUS_NONE
		boton_menos.pressed.connect(func():
			datos["precio"] = max(0, int(datos["precio"]) - 25)
			_poblar_lista_licencia_helicoptero())
		fila.add_child(boton_menos)

		var etiqueta_precio = Label.new()
		etiqueta_precio.text = "$%d" % int(datos["precio"])
		etiqueta_precio.custom_minimum_size = Vector2(50, 0)
		etiqueta_precio.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
		etiqueta_precio.add_theme_font_size_override("font_size", 12)
		fila.add_child(etiqueta_precio)

		var boton_mas = Button.new()
		boton_mas.text = "＋"
		boton_mas.custom_minimum_size = Vector2(24, 24)
		boton_mas.focus_mode = Control.FOCUS_NONE
		boton_mas.pressed.connect(func():
			datos["precio"] = int(datos["precio"]) + 25
			_poblar_lista_licencia_helicoptero())
		fila.add_child(boton_mas)

		var boton_volar = Button.new()
		boton_volar.text = "🔁 Repetir" if progreso[i] else "✈️ Volar"
		boton_volar.custom_minimum_size = Vector2(80, 24)
		boton_volar.focus_mode = Control.FOCUS_NONE
		boton_volar.pressed.connect(_iniciar_mision_licencia.bind(i))
		fila.add_child(boton_volar)

		lista_licencia_helicoptero.add_child(fila)

func _alternar_radio() -> void:
	radio_activa = not radio_activa
	if radio_activa:
		if sonido_radio.stream == null and ruta_emisora_actual != "":
			_cargar_stream_emisora(ruta_emisora_actual)
		sonido_radio.play()
		boton_radio.text = "📻 Radio: ON"
	else:
		sonido_radio.stop()
		boton_radio.text = "📻 Radio: OFF"

# Escanea FX/Radio y arma la lista de emisoras para elegir (mismo patrón que
# _refrescar_lista_temas_musica). Cada botón carga y arranca ESA emisora al
# toque, en loop, y queda guardada como preferida para la próxima vez.
func _refrescar_lista_emisoras_radio() -> void:
	playlist_radio.clear()
	var dir := DirAccess.open(CARPETA_RADIO)
	if dir:
		dir.list_dir_begin()
		var archivo := dir.get_next()
		while archivo != "":
			if not dir.current_is_dir() and archivo.to_lower().ends_with(".mp3"):
				playlist_radio.append(CARPETA_RADIO + "/" + archivo)
			archivo = dir.get_next()
		dir.list_dir_end()
	playlist_radio.sort()
	for hijo in lista_emisoras_radio.get_children():
		hijo.queue_free()
	for i in range(playlist_radio.size()):
		var boton := Button.new()
		boton.text = playlist_radio[i].get_file().trim_suffix(".mp3")
		boton.focus_mode = Control.FOCUS_NONE
		boton.custom_minimum_size = Vector2(0, 20)
		boton.add_theme_font_size_override("font_size", 11)
		boton.clip_text = true
		boton.alignment = HORIZONTAL_ALIGNMENT_LEFT
		if playlist_radio[i] == ruta_emisora_actual:
			boton.modulate = Color(0.5, 1.0, 0.5, 1.0)
		boton.pressed.connect(_seleccionar_emisora_radio.bind(playlist_radio[i]))
		lista_emisoras_radio.add_child(boton)

func _seleccionar_emisora_radio(ruta: String) -> void:
	_cargar_stream_emisora(ruta)
	ruta_emisora_actual = ruta
	radio_activa = true
	sonido_radio.play()
	boton_radio.text = "📻 Radio: ON"
	_guardar_emisora_radio()
	_refrescar_lista_emisoras_radio()

# Mismo truco que la música (ver _reproducir_siguiente_musica): lee los
# bytes a mano con FileAccess en vez de load(), así una emisora nueva que se
# copie a la carpeta suena de una sin pasar antes por el editor de Godot.
func _cargar_stream_emisora(ruta: String) -> void:
	var bytes: PackedByteArray = FileAccess.get_file_as_bytes(ruta)
	if bytes.is_empty():
		return
	var mp3 := AudioStreamMP3.new()
	mp3.data = bytes
	mp3.loop = true
	sonido_radio.stream = mp3

func _guardar_emisora_radio() -> void:
	var cfg = ConfigFile.new()
	cfg.load(RUTA_CONFIG_RADIO)
	cfg.set_value("radio", "emisora", ruta_emisora_actual)
	cfg.save(RUTA_CONFIG_RADIO)

# Elige la emisora preferida guardada, o la de Córdoba por defecto (pedido
# explícito) si es la primera vez o esa ruta ya no existe. Solo prepara
# ruta_emisora_actual -- el audio en sí recién se carga cuando se prende la
# radio (_alternar_radio) o se abre el selector, para no gastar de arranque.
func _cargar_emisora_radio() -> void:
	var dir := DirAccess.open(CARPETA_RADIO)
	if not dir:
		return
	dir.list_dir_begin()
	var archivo := dir.get_next()
	var candidatos: Array = []
	while archivo != "":
		if not dir.current_is_dir() and archivo.to_lower().ends_with(".mp3"):
			candidatos.append(CARPETA_RADIO + "/" + archivo)
		archivo = dir.get_next()
	dir.list_dir_end()
	if candidatos.is_empty():
		return
	var cfg = ConfigFile.new()
	var guardada: String = ""
	if cfg.load(RUTA_CONFIG_RADIO) == OK:
		guardada = cfg.get_value("radio", "emisora", "")
	if guardada != "" and candidatos.has(guardada):
		ruta_emisora_actual = guardada
		return
	for ruta in candidatos:
		if ruta.get_file().trim_suffix(".mp3") == EMISORA_POR_DEFECTO:
			ruta_emisora_actual = ruta
			return
	ruta_emisora_actual = candidatos[0]

# Play/stop simple, sin selector (pedido explícito: "que no sea tanto
# trabajo, play/stop nada más, no como la música que tiene para elegir").
# Busca el primer .mp3 que encuentre en la carpeta -- cuando se reemplace
# el archivo por uno mejor, no hace falta tocar código, solo el archivo.
func _alternar_instrucciones() -> void:
	if sonido_instrucciones.playing:
		sonido_instrucciones.stop()
		boton_instrucciones.text = "🦺 Instrucciones"
		return
	if sonido_instrucciones.stream == null:
		var dir := DirAccess.open(CARPETA_INSTRUCCIONES)
		if not dir:
			return
		dir.list_dir_begin()
		var archivo := dir.get_next()
		var ruta := ""
		while archivo != "":
			if not dir.current_is_dir() and archivo.to_lower().ends_with(".mp3"):
				ruta = CARPETA_INSTRUCCIONES + "/" + archivo
				break
			archivo = dir.get_next()
		dir.list_dir_end()
		if ruta == "":
			return
		var bytes: PackedByteArray = FileAccess.get_file_as_bytes(ruta)
		if bytes.is_empty():
			return
		var mp3 := AudioStreamMP3.new()
		mp3.data = bytes
		sonido_instrucciones.stream = mp3
	sonido_instrucciones.play()
	boton_instrucciones.text = "🦺 Instrucciones: ON"

func _cargar_control_mouse() -> void:
	var cfg = ConfigFile.new()
	if cfg.load(RUTA_CONFIG_CONTROLES) != OK:
		return
	control_mouse_activo = cfg.get_value("vuelo", "mouse_activo", false)

func _guardar_control_mouse() -> void:
	var cfg = ConfigFile.new()
	cfg.load(RUTA_CONFIG_CONTROLES)
	cfg.set_value("vuelo", "mouse_activo", control_mouse_activo)
	cfg.save(RUTA_CONFIG_CONTROLES)

# Nivel de gráficos (pedido explícito 2026-10-03, "como tienen todos los
# programas, bajo medio y alto"): controla los mismos parámetros del
# Cesium3DTileset que se habían bajado a mano durante la investigación de los
# crashes (ver feedback_texturas_vram_aviones_nuevos.md y el commit que bajó
# maximum_screen_space_error / maximum_simultaneous_tile_loads /
# loading_descendant_limit) -- ahora el usuario los puede subir de nuevo si
# su máquina aguanta, en vez de quedar fijos en el valor más conservador.
# 0 = Bajo (igual al ajuste de seguridad post-crash), 1 = Medio, 2 = Alto
# (cerca del default de Cesium, más nitidez pero más carga).
var calidad_graficos_actual: int = 0
const RUTA_CONFIG_GRAFICOS = "user://graficos_config.cfg"

func _cargar_calidad_graficos() -> void:
	var cfg = ConfigFile.new()
	if cfg.load(RUTA_CONFIG_GRAFICOS) != OK:
		return
	calidad_graficos_actual = cfg.get_value("graficos", "calidad", 0)

func _guardar_calidad_graficos() -> void:
	var cfg = ConfigFile.new()
	cfg.load(RUTA_CONFIG_GRAFICOS)
	cfg.set_value("graficos", "calidad", calidad_graficos_actual)
	cfg.save(RUTA_CONFIG_GRAFICOS)

func _aplicar_calidad_graficos(indice: int) -> void:
	if not cesium_tileset:
		return
	# 2026-10-03, historial de esta función (para no repetir el mismo lío):
	# 1) Se probó activar forbid_holes/preload_ancestors/preload_siblings
	#    (propiedades reales del Cesium3DTileset, no documentadas en
	#    tooltips.gd) en Medio/Alto -- el terreno se puso "nublado" en los
	#    3 niveles. Se descartó cuota de Cesium ion con datos reales del
	#    dashboard (141 MiB esa semana). Se revirtieron esas 3 propiedades.
	# 2) Apareció un lag nuevo -- se solucionó reiniciando Godot completo
	#    (cambiarlas en caliente sin reiniciar no alcanza).
	# 3) Se separó más maximum_screen_space_error entre niveles (Alto a 8)
	#    para que la diferencia de nitidez se notara.
	# 4) Se probó forbid_holes=true solo (sin los otros 2) para evitar los
	#    parches marrones que se completaban de golpe.
	# 5) Con forbid_holes=true apareció/empeoró un patrón distinto: el
	#    fondo lejano terminaba de cargar rápido y nítido, pero la zona
	#    CERCANA a la cámara (la más importante, con mucha más densidad de
	#    edificios) quedaba trabada mostrando picos/placeholders mucho más
	#    tiempo -- Gemini lo explicó así: forbid_holes obliga a esperar a
	#    que TODOS los hijos de un tile estén listos antes de refinar, y la
	#    zona cercana tiene un sub-árbol de hijos mucho más grande (más
	#    densidad real de Buenos Aires) que el fondo lejano, así que tarda
	#    mucho más en "destrabarse". Conclusión de Gemini + nuestra: dejamos
	#    de iterar en caliente y volvemos a la ÚNICA configuración
	#    confirmada estable durante meses (la que corría ANTES de que
	#    existiera este selector de 3 niveles): 36/2/2, sin forbid_holes ni
	#    preload_ancestors/preload_siblings. Los 3 niveles quedan IGUALES
	#    por ahora a propósito -- no diferenciarlos hasta confirmar que esta
	#    base anda tan prolija como antes. Recién ahí, de a una variable por
	#    vez y con reinicio completo entre cada prueba, se vuelve a intentar
	#    diferenciar Medio/Alto.
	match indice:
		0, 1, 2:  # Bajo, Medio, Alto -- los 3 iguales a propósito (ver arriba)
			cesium_tileset.maximum_screen_space_error = 36.0
			cesium_tileset.maximum_simultaneous_tile_loads = 2
			cesium_tileset.loading_descendant_limit = 2
			cesium_tileset.forbid_holes = false

func _cambiar_volumen_motor(valor: float) -> void:
	volumen_motor = valor
	sonido_motor.volume_db = lerp(VOLUMEN_MOTOR_DB_MINIMO, VOLUMEN_MOTOR_DB_MAXIMO, volumen_motor)
	var cfg = ConfigFile.new()
	cfg.load(RUTA_CONFIG_AUDIO)  # si no existe todavía, sigue con un ConfigFile vacío
	cfg.set_value("audio", "volumen_motor", volumen_motor)
	cfg.save(RUTA_CONFIG_AUDIO)

func _cargar_volumen_motor() -> void:
	var cfg = ConfigFile.new()
	if cfg.load(RUTA_CONFIG_AUDIO) != OK:
		return
	volumen_motor = cfg.get_value("audio", "volumen_motor", volumen_motor)

func _cambiar_volumen_musica(valor: float) -> void:
	volumen_musica = valor
	sonido_musica.volume_db = lerp(VOLUMEN_MUSICA_DB_MINIMO, VOLUMEN_MUSICA_DB_MAXIMO, volumen_musica)
	var cfg = ConfigFile.new()
	cfg.load(RUTA_CONFIG_AUDIO)
	cfg.set_value("audio", "volumen_musica", volumen_musica)
	cfg.save(RUTA_CONFIG_AUDIO)

func _cargar_volumen_musica() -> void:
	var cfg = ConfigFile.new()
	if cfg.load(RUTA_CONFIG_AUDIO) != OK:
		return
	volumen_musica = cfg.get_value("audio", "volumen_musica", volumen_musica)

func _cambiar_volumen_radio(valor: float) -> void:
	volumen_radio = valor
	sonido_radio.volume_db = lerp(VOLUMEN_RADIO_DB_MINIMO, VOLUMEN_RADIO_DB_MAXIMO, volumen_radio)
	var cfg = ConfigFile.new()
	cfg.load(RUTA_CONFIG_AUDIO)
	cfg.set_value("audio", "volumen_radio", volumen_radio)
	cfg.save(RUTA_CONFIG_AUDIO)

func _cargar_volumen_radio() -> void:
	var cfg = ConfigFile.new()
	if cfg.load(RUTA_CONFIG_AUDIO) != OK:
		return
	volumen_radio = cfg.get_value("audio", "volumen_radio", volumen_radio)

func _cambiar_volumen_instrucciones(valor: float) -> void:
	volumen_instrucciones = valor
	sonido_instrucciones.volume_db = lerp(VOLUMEN_INSTRUCCIONES_DB_MINIMO, VOLUMEN_INSTRUCCIONES_DB_MAXIMO, volumen_instrucciones)
	var cfg = ConfigFile.new()
	cfg.load(RUTA_CONFIG_AUDIO)
	cfg.set_value("audio", "volumen_instrucciones", volumen_instrucciones)
	cfg.save(RUTA_CONFIG_AUDIO)

func _cargar_volumen_instrucciones() -> void:
	var cfg = ConfigFile.new()
	if cfg.load(RUTA_CONFIG_AUDIO) != OK:
		return
	volumen_instrucciones = cfg.get_value("audio", "volumen_instrucciones", volumen_instrucciones)

# Arma una fila "Label + HSlider" por código, igual que las filas de
# volumen que ya existen en la escena (motor/música/radio/instrucciones),
# y la agrega al final del contenedor sin tocar nada de lo que ya había.
# Cartel + fanfarria al completar CUALQUIERA de las 3 misiones (pedido
# 2026-10-07: "vos se la pusiste nada más que a la de CABA, pero que las
# tres tengan"). Compartido para no repetir el mismo bloque tres veces.
func _festejar_mision_completada() -> void:
	cartel_central.text = "🎉 MISIÓN COMPLETADA"
	cartel_central.visible = true
	get_tree().create_timer(3.0).timeout.connect(func(): cartel_central.visible = false)
	if sonido_mision_completada:
		sonido_mision_completada.play()

func _crear_fila_slider_volumen(contenedor: VBoxContainer, titulo: String, valor_inicial: float, al_cambiar: Callable) -> HSlider:
	var fila := HBoxContainer.new()
	var etiqueta := Label.new()
	etiqueta.text = titulo
	etiqueta.custom_minimum_size = Vector2(220, 0)
	fila.add_child(etiqueta)
	var slider := HSlider.new()
	slider.min_value = 0.0
	slider.max_value = 1.0
	slider.step = 0.01
	slider.value = valor_inicial
	slider.custom_minimum_size = Vector2(160, 0)
	slider.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	slider.focus_mode = Control.FOCUS_NONE
	fila.add_child(slider)
	contenedor.add_child(fila)
	slider.value_changed.connect(al_cambiar)
	return slider

func _cambiar_volumen_disparo(valor: float) -> void:
	volumen_disparo = valor
	if sonido_disparo:
		sonido_disparo.volume_db = lerp(VOLUMEN_DISPARO_DB_MINIMO, VOLUMEN_DISPARO_DB_MAXIMO, volumen_disparo)
	var cfg = ConfigFile.new()
	cfg.load(RUTA_CONFIG_AUDIO)
	cfg.set_value("audio", "volumen_disparo", volumen_disparo)
	cfg.save(RUTA_CONFIG_AUDIO)

func _cargar_volumen_disparo() -> void:
	var cfg = ConfigFile.new()
	if cfg.load(RUTA_CONFIG_AUDIO) != OK:
		return
	volumen_disparo = cfg.get_value("audio", "volumen_disparo", volumen_disparo)

func _cambiar_volumen_explosion(valor: float) -> void:
	volumen_explosion = valor
	# Un solo slider controla las dos explosiones (tierra y barco) -- pedido
	# "discriminado por balas y explosiones", dos categorías, no tres.
	if sonido_explosion:
		sonido_explosion.volume_db = lerp(VOLUMEN_EXPLOSION_DB_MINIMO, VOLUMEN_EXPLOSION_DB_MAXIMO, volumen_explosion)
	if sonido_explosion_barco:
		sonido_explosion_barco.volume_db = lerp(VOLUMEN_EXPLOSION_DB_MINIMO, VOLUMEN_EXPLOSION_DB_MAXIMO, volumen_explosion)
	var cfg = ConfigFile.new()
	cfg.load(RUTA_CONFIG_AUDIO)
	cfg.set_value("audio", "volumen_explosion", volumen_explosion)
	cfg.save(RUTA_CONFIG_AUDIO)

func _cargar_volumen_explosion() -> void:
	var cfg = ConfigFile.new()
	if cfg.load(RUTA_CONFIG_AUDIO) != OK:
		return
	volumen_explosion = cfg.get_value("audio", "volumen_explosion", volumen_explosion)

# Carga el loop que corresponde a la familia de avión (hélice/jet/helicóptero)
# y lo deja sonando -- se llama al arrancar y cada vez que se cambia de avión
# en el selector de vuelo.
func _aplicar_sonido_motor(indice: int) -> void:
	if indice < 0 or indice >= TIPOS_AVION.size():
		return
	var ruta: String = TIPOS_AVION[indice]["sonido"]
	if ruta == "":
		sonido_motor.stop()
		sonido_motor.stream = null
		return
	var estaba_sonando = sonido_motor.playing
	var stream: AudioStream = load(ruta)
	if stream is AudioStreamMP3:
		stream.loop = true
	sonido_motor.stream = stream
	if estaba_sonando:
		sonido_motor.play()

# Devuelve si la acción mapeada al joystick está "presionada" ahora --
# false si no hay joystick conectado o si esa acción no tiene nada asignado
# todavía. Se combina con el chequeo de teclado normal (OR) en _procesar_vuelo().
func _joystick_activo(nombre_accion: String) -> bool:
	if joystick_id < 0 or not mapeo_joystick.has(nombre_accion):
		return false
	var m = mapeo_joystick[nombre_accion]
	if m["tipo"] == "boton":
		return Input.is_joy_button_pressed(joystick_id, m["indice"])
	var valor = Input.get_joy_axis(joystick_id, m["indice"])
	return valor * m["signo"] > DEADZONE_EJE_JOYSTICK

func _joystick_eje(nombre_accion: String) -> float:
	if joystick_id < 0 or not mapeo_joystick.has(nombre_accion):
		return 0.0
	var m = mapeo_joystick[nombre_accion]
	if m["tipo"] != "eje":
		return 0.0
	var valor: float = Input.get_joy_axis(joystick_id, int(m["indice"])) * float(m["signo"])
	var magnitud: float = abs(valor)
	if magnitud <= DEADZONE_EJE_JOYSTICK:
		return 0.0
	return signf(valor) * clamp(
		(magnitud - DEADZONE_EJE_JOYSTICK) / (1.0 - DEADZONE_EJE_JOYSTICK), 0.0, 1.0)

func _crear_mira_hud() -> void:
	if mira_hud:
		return
	mira_hud = Control.new()
	mira_hud.name = "MiraArmamentoHUD"
	mira_hud.size = Vector2(64.0, 64.0)
	mira_hud.mouse_filter = Control.MOUSE_FILTER_IGNORE
	mira_hud.z_index = 50
	mira_hud.modulate = Color(1.0, 0.1, 0.1, 0.85)
	# Pedido 2026-10-10 ("la crucecita celeste molesta en la presentación,
	# que aparezca durante el juego, no antes"): mismo arreglo que ya se le
	# hizo a los 3 contadores de misión -- arranca oculta, se muestra recién
	# cuando se saca la cortina de carga (ver _process).
	mira_hud.visible = false
	get_node("../HUD").add_child(mira_hud)

	var segmentos := [
		Rect2(22.0, 31.0, 20.0, 2.0),
		Rect2(31.0, 22.0, 2.0, 20.0),
	]
	for recto in segmentos:
		var segmento := ColorRect.new()
		segmento.position = recto.position
		segmento.size = recto.size
		segmento.color = Color.WHITE
		segmento.mouse_filter = Control.MOUSE_FILTER_IGNORE
		mira_hud.add_child(segmento)

	# Pedido 2026-10-11 ("un numerito cerca de la mira mientras disparo"):
	# NO es hijo de mira_hud a propósito -- mira_hud ya tiene su propio
	# modulate que parpadea con cada disparo, y eso mezclaría mal con el
	# color del gradiente de vida. Se posiciona solo, siguiendo a la mira
	# cuadro a cuadro (ver _actualizar_mira_hud).
	etiqueta_vida_objetivo = Label.new()
	etiqueta_vida_objetivo.name = "VidaObjetivoHUD"
	etiqueta_vida_objetivo.z_index = 51
	etiqueta_vida_objetivo.mouse_filter = Control.MOUSE_FILTER_IGNORE
	etiqueta_vida_objetivo.add_theme_font_size_override("font_size", 26)
	etiqueta_vida_objetivo.add_theme_constant_override("outline_size", 5)
	etiqueta_vida_objetivo.add_theme_color_override("font_outline_color", Color.BLACK)
	etiqueta_vida_objetivo.visible = false
	get_node("../HUD").add_child(etiqueta_vida_objetivo)

func _actualizar_mira_hud(delta: float) -> void:
	if not mira_hud:
		return
	var entrada := Vector2(
		-_joystick_eje("mira_horizontal"),
		-_joystick_eje("mira_vertical"))
	var tamano_pantalla: Vector2 = get_viewport().get_visible_rect().size
	var centro: Vector2 = tamano_pantalla * 0.5
	var mitad_mira: Vector2 = mira_hud.size * 0.5
	var margen := Vector2(24.0, 24.0)
	var limite := Vector2(
		max(0.0, centro.x - mitad_mira.x - margen.x),
		max(0.0, centro.y - mitad_mira.y - margen.y))
	# La mira acumula el desplazamiento y queda fija al soltar el stick.
	# El eje horizontal se invierte porque este joystick lo entrega al revés.
	const VELOCIDAD_MIRA_PIXELES := 180.0
	mira_hud_posicion += entrada * VELOCIDAD_MIRA_PIXELES * delta
	mira_hud_posicion.x = clamp(mira_hud_posicion.x, -limite.x, limite.x)
	mira_hud_posicion.y = clamp(mira_hud_posicion.y, -limite.y, limite.y)
	mira_hud.position = centro + mira_hud_posicion - mitad_mira
	if etiqueta_vida_objetivo:
		etiqueta_vida_objetivo.position = mira_hud.position + Vector2(74.0, 20.0)
	mira_feedback_tiempo = max(0.0, mira_feedback_tiempo - delta)
	if mira_feedback_tiempo > 0.0:
		mira_hud.modulate = Color(1.0, 0.25, 0.05, 1.0)
	else:
		mira_hud.modulate = Color(1.0, 0.1, 0.1, 0.85)
		# Pedido 2026-10-11 ("un numerito cerca de la mira mientras le
		# disparo, para saber si estoy acertando sin tener que buscar el
		# cartel 3D arriba del blanco"): se apaga solo junto con el
		# parpadeo de la mira.
		if etiqueta_vida_objetivo:
			etiqueta_vida_objetivo.visible = false

# La vertical GEODÉSICA real en la posición ACTUAL del avión (no
# Vector3.UP, que es un eje fijo del motor y NO coincide con "arriba" en
# Buenos Aires -- ver todo el bug del origen/cámara). Como el origen del
# georeference siempre está centrado en el avión (ver mundo.gd), alcanza con
# pedirle la normal de superficie en el ECEF actual del georeference. La
# usan: la cámara (para no filmar el terreno "al revés"), el giro (para no
# desalinear al avión de a poco en cada curva) y la alineación inicial.
func _arriba_real() -> Vector3:
	if not mundo or not mundo.georeferencia:
		return Vector3.UP
	var ecef_actual := Vector3(
		mundo.georeferencia.get_ecef_x(),
		mundo.georeferencia.get_ecef_y(),
		mundo.georeferencia.get_ecef_z())
	var normal: Vector3 = mundo.georeferencia.get_normal_at_surface_pos(ecef_actual)
	if normal.length_squared() < 0.0001:
		return Vector3.UP
	return normal.normalized()

# IMPORTANTE (bug real encontrado 2026-09-20): al principio esto se llamaba
# UNA sola vez, al arrancar, cerca de Aeroparque. Pero la vertical real
# ROTA un poco con la curvatura de la Tierra a medida que te alejás -- en
# La Plata (~60km) ya hay medio grado de diferencia respecto a la vertical
# "congelada" en Aeroparque. Medio grado no suena a mucho, pero con la
# velocidad alta que usamos para probar, esa pequeña desviación se traduce
# en una PÉRDIDA DE ALTURA CONSTANTE Y REAL aunque el avión esté "nivelado"
# según su propia referencia vieja -- esto era lo que describía el usuario
# como "cuando lo dejo estabilizado, tiende a bajar".
# fix: en vez de una sola vez, se llama TODOS los cuadros en los que el
# avión está efectivamente nivelado (banco y cabeceo casi en cero) -- ahí
# es seguro corregir el desvío acumulado sin pelear contra un giro o
# cabeceo intencional del jugador (ver el chequeo en _process()).
# BUG REAL encontrado 2026-09-21 (reportado por el usuario: "cuando termino
# de nivelar después de un giro cerrado, a veces salta de golpe, como si se
# rompiera algo"): esta función aplicaba la corrección COMPLETA de un saque
# -- global_rotate(eje, angulo) con el ángulo entero, sin límite. Normal
# volando derecho, el desvío acumulado por cuadro es minúsculo (fracciones
# de grado), así que la corrección instantánea no se nota. Pero durante un
# giro cerrado (más ahora que ampliamos el banco máximo a 45°), el "arriba"
# del avión se aleja bastante más de la vertical real mientras estás
# inclinado -- y como esta función se salta a propósito mientras hay banco/
# cabeceo intencional (ver el chequeo en _process()), esa desviación grande
# se queda ACUMULADA sin corregir hasta el instante exacto en que soltás y
# el banco cruza por debajo de 0.5° -- ahí se dispara de nuevo, y de un
# saque intenta corregir TODO lo acumulado en un solo cuadro. Con un ángulo
# grande, eso se ve/siente como un salto o "rotura".
# FIX: limitar cuánto puede girar por segundo (igual que ya hacemos con
# banco/cabeceo) -- si la desviación es chica (vuelo derecho normal), se
# corrige toda igual, imperceptible; si es grande (recién salido de un giro
# cerrado), se corrige de a poco en varios cuadros, suave, nunca de un salto.
const VELOCIDAD_ALINEACION_MAXIMA = 90.0  # grados por segundo
func _alinear_con_vertical_real(delta: float) -> void:
	var arriba_real: Vector3 = _arriba_real()

	# PROBADO Y DESCARTADO (dos veces):
	# 1) Armar la base a mano con productos cruzados -- dependía del rumbo
	#    (bien en una dirección, mal en la contraria).
	# 2) Basis.looking_at(adelante, arriba) -- Godot tiene una ambigüedad
	#    conocida ahí sobre qué eje cuenta como "adelante" del modelo
	#    (parámetro use_model_front); terminó avanzando hacia donde apunta
	#    el ala en vez de la nariz -- un giro de 90° que ninguna de las dos
	#    reconstrucciones "desde cero" evitó de forma confiable.
	# SOLUCIÓN: no reconstruir la base desde cero. Partimos de la base actual
	# (la del mundo de juguete, que sabemos 100% correcta: nariz = -Z) y la
	# ROTAMOS en el lugar lo mínimo necesario para que su "arriba" pase a ser
	# la vertical real -- una única rotación rígida que por definición no
	# puede alterar la relación entre nariz/alas/arriba que ya tenía.
	var arriba_actual: Vector3 = global_transform.basis.y.normalized()
	var eje: Vector3 = arriba_actual.cross(arriba_real)
	if eje.length_squared() < 0.0001:
		return
	var angulo: float = arriba_actual.angle_to(arriba_real)
	var angulo_maximo: float = deg_to_rad(VELOCIDAD_ALINEACION_MAXIMA * delta)
	angulo = min(angulo, angulo_maximo)
	global_rotate(eje.normalized(), angulo)

func _process(delta: float) -> void:
	# "Modo a pie" corta TODA la lógica de vuelo/armamento/misiones de este
	# cuadro (no tiene sentido ninguna mientras estás caminando) -- solo la
	# cámara y el mapa de calles siguen andando, para que se sienta igual de
	# "vivo" que volando. Ver comentario largo junto a la declaración de
	# modo_a_pie, más arriba, sobre por qué esto reusa el mismo nodo del
	# avión en vez de crear uno nuevo.
	if modo_a_pie:
		_procesar_modo_a_pie(delta)
		_actualizar_camara()
		_actualizar_mapa_calles(delta)
		_actualizar_boton_bajar_subir()
		return
	_actualizar_estroboscopica(delta)
	_igualar_tamano_luces_navegacion()
	_actualizar_cartel_aterrizaje(delta)
	_actualizar_city_tour(delta)
	_actualizar_anuncio_seguridad(delta)
	_actualizar_relojes_panel(delta)
	# Orientación inicial hacia San Fernando -- ver comentario junto a las
	# constantes LAT/LON de arriba. Se aplica UNA sola vez, en el primer
	# cuadro (Godot procesa _process() de arriba hacia abajo en el árbol --
	# mundo.gd, el padre, ya corrió el suyo y calculó este_motor_actual/
	# norte_motor_actual reales para este mismo cuadro antes de llegar acá),
	# antes de que el jugador haya tocado nada.
	if not _orientacion_inicial_aplicada:
		_orientacion_inicial_aplicada = true
		var rumbo_inicial: float = _rumbo_verdadero_hacia(
			LAT_AEROPARQUE, LON_AEROPARQUE, LAT_SAN_FERNANDO, LON_SAN_FERNANDO)
		var rumbo_rad: float = deg_to_rad(rumbo_inicial)
		var direccion: Vector3 = (mundo.norte_motor_actual * cos(rumbo_rad) + mundo.este_motor_actual * sin(rumbo_rad)).normalized()
		look_at(global_position + direccion, _arriba_real())
		_tiempo_desde_orientacion_inicial = 0.0

	if _tiempo_desde_orientacion_inicial >= 0.0:
		_tiempo_desde_orientacion_inicial += delta
		if _tiempo_desde_orientacion_inicial >= ESPERA_CORTINA_DE_CARGA:
			cortina_de_carga.visible = false
			_tiempo_desde_orientacion_inicial = -1.0
			etiqueta_objetivo_destruido.visible = true
			etiqueta_mision_verde.visible = true
			etiqueta_barcos.visible = true
			etiqueta_industria.visible = true
			if mira_hud:
				mira_hud.visible = true

	_actualizar_mira_hud(delta)
	_sincronizar_modo_guerra(delta)
	_actualizar_objetivos_armamento()
	_actualizar_barcos(delta)
	# BUG REAL encontrado 2026-10-11 (reportado: "las balas de los barcos
	# salen para el lado contrario de donde estoy, aunque no me mueva"):
	# estas dos funciones estaban llamándose ARRIBA, antes de
	# _actualizar_barcos() -- en ese momento la posición de cada barco
	# todavía era la del cuadro ANTERIOR (mundo.gd ya recentró el origen
	# este cuadro, pero el barco recién se reubica acá abajo). El disparo
	# calculaba la dirección con una posición de barco desactualizada.
	# Movidas DESPUÉS de _actualizar_barcos() para que siempre usen la
	# posición ya corregida de este mismo cuadro.
	if modo_guerra:
		_actualizar_contraataque_barcos(delta)
		_actualizar_balas_barcos(delta)
		_actualizar_unidades_invasion(delta)

	# Tecla U: "botón de pánico" para el panel del mapa (pedido explícito
	# 2026-09-21, "no me quiero ir a dormir y dejarlo roto") -- el doble clic
	# no respondía, así que esto NO pasa por el mouse/GUI para nada (chequea
	# la tecla física directo, como M/N/I/O/L) -- funciona SIEMPRE, tape lo
	# que tape el panel en pantalla. Vuelve al tamaño y posición de siempre.
	var tecla_reset_mapa_activa = Input.is_physical_key_pressed(KEY_U)
	if tecla_reset_mapa_activa and not tecla_reset_mapa_anterior:
		mapa_rect.offset_left = -264.0
		mapa_rect.offset_top = 151.0
		mapa_rect.offset_right = 0.0
		mapa_rect.offset_bottom = 406.0
		_modo_resize_mapa = ModoResizeMapa.NINGUNO
		_arrastrando_mover_mapa = false
		cartel_central.text = "🗺️ Mapa restablecido"
		cartel_central.visible = true
		get_tree().create_timer(1.5).timeout.connect(func(): cartel_central.visible = false)
	tecla_reset_mapa_anterior = tecla_reset_mapa_activa

	# Tecla V: vista externa sin el avión visible/oculto (ver _alternar_vista_
	# sin_avion y el comentario junto a la declaración de vista_sin_avion_
	# activa, más arriba).
	var tecla_vista_sin_avion_activa = Input.is_physical_key_pressed(KEY_V)
	if tecla_vista_sin_avion_activa and not tecla_vista_sin_avion_anterior:
		_alternar_vista_sin_avion()
	tecla_vista_sin_avion_anterior = tecla_vista_sin_avion_activa

	# Disparo de prueba: proyecta un rayo desde la mira, muestra la traza y
	# comprueba impactos contra los blancos cuadrados creados al iniciar.
	tiempo_hasta_proximo_disparo_jugador = max(0.0, tiempo_hasta_proximo_disparo_jugador - delta)
	var armamento_activo := _joystick_activo("armamento")
	if armamento_activo and not armamento_anterior and esperando_asignacion == "" and tiempo_hasta_proximo_disparo_jugador <= 0.0:
		tiempo_hasta_proximo_disparo_jugador = CADENCIA_DISPARO_JUGADOR
		var disparo := _disparar_hacia_mira()
		# Pedido 2026-10-11 ("prefiero como lo teníamos, con la traza, que
		# salga de las dos luces apuntando a la cruz de la mira -- la
		# marca de impacto queda tapada DETRÁS de la mira, que es un
		# overlay 2D siempre encima"): vuelve la traza de las dos alas
		# convergiendo (ya estaba armada, solo hacía falta reactivarla).
		# Pedido 2026-10-11 ("una sola línea desde las luces hasta la mira,
		# nada más"): origen YA es el punto medio entre las dos luces de ala
		# (ver _disparar_hacia_mira) -- una línea derecha, simple.
		_crear_traza_armamento(disparo["origen"], disparo["destino"])
		if sonido_disparo:
			sonido_disparo.play(1.0 if sonido_disparo_archivo_real else 0.0)
		if disparo["impacto"]:
			var objetivo: Node3D = disparo["objetivo"]
			# Blancos con "vida" (pedido 2026-10-10, misión Industria): no
			# mueren de un tiro -- cada impacto resta 1 y actualiza el
			# numerito flotante. Recién cuando llega a 0 sigue el camino de
			# siempre (explosión, sonido, contador, destrucción). No se
			# usa "return" acá para no saltearse el resto de _process() en
			# el mismo cuadro (controles, etc.) -- se usa una bandera.
			var sigue_con_vida := false
			if objetivo.has_meta("vida"):
				var vida_restante: int = objetivo.get_meta("vida") - 1
				objetivo.set_meta("vida", vida_restante)
				if vida_restante > 0:
					sigue_con_vida = true
					# Pedido 2026-10-11 ("que cambie de color para saber si le
					# estoy pegando, que arranque bien oscuro/intenso y vaya
					# aclarando -- negro, marrón, azul, y cuando le falten
					# dos tiros, amarillo/blanco, casi liquidado"): gradiente
					# según el % de vida que le queda, no un color fijo.
					var vida_inicial_objetivo: int = objetivo.get_meta("vida_inicial", 10)
					var color_segun_vida: Color = (_color_semaforo_invasion(vida_restante) if objetivo.is_in_group("invasion") else _color_por_vida_restante(vida_restante, vida_inicial_objetivo))
					var etiqueta_vida: Label3D = objetivo.get_meta("etiqueta_vida")
					if is_instance_valid(etiqueta_vida):
						etiqueta_vida.text = str(vida_restante)
						etiqueta_vida.modulate = color_segun_vida
						etiqueta_vida.visible = true  # los barcos arrancan con el numerito oculto (ver _crear_barcos_rio) hasta el primer impacto
					# Pedido 2026-10-11 ("un numerito cerca de la mira mientras
					# disparo, para no tener que buscar el cartel 3D arriba del
					# blanco"): mismo número y mismo color, pero en el HUD 2D.
					if etiqueta_vida_objetivo:
						etiqueta_vida_objetivo.text = str(vida_restante)
						etiqueta_vida_objetivo.modulate = color_segun_vida
						etiqueta_vida_objetivo.visible = true
					# Pedido 2026-10-11 ("lo que yo quiero que cambie de color
					# es el CUADRADO, no el número"): el material real del
					# cuadrado -- albedo Y emission, si no con emission fija
					# el cambio de albedo casi no se nota.
					var material_cuadrado: StandardMaterial3D = objetivo.get_meta("material_cuadrado")
					if material_cuadrado:
						material_cuadrado.albedo_color = color_segun_vida
						material_cuadrado.emission = color_segun_vida
					# PAUSADO 2026-10-11 (pedido explícito de Pablo, "el sistema de
					# bala es malísimo, olvidémonos por ahora -- hay que pensar
					# otro sistema"): se deja de activar el contraataque. Código
					# de balas/ráfagas intacto más abajo por si se retoma, pero
					# ya no se dispara solo. Foco ahora: la invasión por tierra.
					if (objetivo.is_in_group("mision_barcos") or objetivo.is_in_group("mision_aeropuertos")) and not objetivo.has_meta("activado"):
						objetivo.set_meta("activado", true)
				else:
					var etiqueta_vida_final: Label3D = objetivo.get_meta("etiqueta_vida")
					if is_instance_valid(etiqueta_vida_final):
						etiqueta_vida_final.queue_free()
			if sigue_con_vida:
				# El sonido de disparo ya sonó arriba -- acá solo falta el
				# "feedback" de la mira (parpadeo rojo), sin explosión ni
				# destruir nada todavía.
				mira_feedback_tiempo = 0.25
			else:
				_crear_explosion_city_tour(objetivo.global_position)
				# Explosión distinta para barcos vs. el resto (aeropuertos/
				# misión verde/industria) -- pedido explícito 2026-10-07.
				var sonido_de_impacto: AudioStreamPlayer = sonido_explosion_barco if objetivo.is_in_group("mision_barcos") else sonido_explosion
				if sonido_de_impacto:
					# BUG REAL encontrado 2026-10-07 ("sueltos suenan bien, en
					# el juego suenan raro, la de barco parece un disparo"):
					# acá había un corte forzado a los 0.5s, afinado para el
					# sonido viejo (bien corto). Los sonidos nuevos son más
					# largos y ese corte les arrancaba la cola -- se escuchaba
					# solo el golpe inicial, parecido a un disparo seco. Se
					# deja que cada explosión termine sola; si entra un
					# impacto nuevo antes de que termine la anterior, play()
					# la reinicia sin problema.
					sonido_de_impacto.play()
				if objetivo.is_in_group("mision_verde"):
					mision_verde_destruidos += 1
					if etiqueta_mision_verde:
						etiqueta_mision_verde.text = "CAPITAL: %d/10" % mision_verde_destruidos
						etiqueta_mision_verde.visible = true
					if mision_verde_destruidos >= OBJETIVOS_MISION_VERDE_CABA.size():
						_festejar_mision_completada()
				elif objetivo.is_in_group("mision_aeropuertos"):
					objetivos_destruidos += 1
					if etiqueta_objetivo_destruido:
						etiqueta_objetivo_destruido.text = "AEROPUERTOS: %d/5" % objetivos_destruidos
						etiqueta_objetivo_destruido.visible = true
					if objetivos_destruidos >= 5:
						_festejar_mision_completada()
				elif objetivo.is_in_group("mision_industria"):
					industria_destruidos += 1
					if etiqueta_industria:
						etiqueta_industria.text = "INDUSTRIA: %d/10" % industria_destruidos
						etiqueta_industria.visible = true
					if industria_destruidos >= MISION_INDUSTRIA.size():
						_festejar_mision_completada()
				elif objetivo.is_in_group("mision_barcos"):
					barcos_destruidos += 1
					if etiqueta_barcos:
						etiqueta_barcos.text = "BARCOS: %d/%d" % [barcos_destruidos, BARCOS_RIO_AEROPARQUE.size()]
						etiqueta_barcos.visible = true
					if barcos_destruidos >= BARCOS_RIO_AEROPARQUE.size():
						_festejar_mision_completada()
				elif objetivo.is_in_group("aeropuerto_invadido"):
					# Boceto de invasión: bajaste las 40 vidas de la marca
					# marrón -- el aeropuerto queda liberado, sale del
					# diccionario de tomados (vuelve a ser blanco de
					# invasión posible más adelante).
					var nombre_liberado: String = objetivo.get_meta("nombre_aeropuerto_tomado", "")
					if nombre_liberado != "":
						aeropuertos_tomados.erase(nombre_liberado)
						print("✅ Aeropuerto LIBERADO: ", nombre_liberado)
				elif objetivo.is_in_group("invasion"):
					invasores_destruidos += 1
				objetivos_armamento.erase(objetivo)
				objetivos_barcos.erase(objetivo)
				objetivos_industria.erase(objetivo)
				barcos_activados.erase(objetivo)
				unidades_invasion.erase(objetivo)
				objetivo.queue_free()
			mira_feedback_tiempo = 0.25
		else:
			# Pedido 2026-10-10 ("el cartel gigante queda antiestético, y no
			# entiendo por qué si lo detecta cerca no cuenta"): ya no hay zona
			# gris -- la tolerancia ahora escala con la distancia y lo que
			# entra, cuenta siempre (ver _disparar_hacia_mira). Esto es
			# simplemente un fallo limpio: aviso chico y discreto, mismo
			# estilo que los instrumentos de arriba, no un cartel central.
			if panel_fuera_de_mira:
				panel_fuera_de_mira.visible = true
				get_tree().create_timer(0.8).timeout.connect(func():
					if is_instance_valid(panel_fuera_de_mira):
						panel_fuera_de_mira.visible = false)
	armamento_anterior = armamento_activo

	# "Marcar lugar" -- detectado a mano (con flag "anterior") porque
	# _joystick_activo() devuelve "está apretado ahora", no "recién lo
	# apretaste"; sin este chequeo, el panel se reabriría todos los cuadros
	# mientras se mantiene apretado el botón/tecla.
	var marcar_lugar_activo = Input.is_physical_key_pressed(KEY_M) or _joystick_activo("marcar_lugar")
	if marcar_lugar_activo and not marcar_lugar_anterior and not panel_marcar_lugar.visible and esperando_asignacion == "":
		_abrir_panel_marcar_lugar()
	marcar_lugar_anterior = marcar_lugar_activo

	# Tecla N: vuelca una línea de debug del bug de rumbo al archivo (una
	# sola vez por apretada, no un chorro continuo).
	var debug_rumbo_activo = Input.is_physical_key_pressed(KEY_N)
	if debug_rumbo_activo and not debug_rumbo_anterior:
		var archivo = FileAccess.open(RUTA_DEBUG_RUMBO, FileAccess.READ_WRITE if FileAccess.file_exists(RUTA_DEBUG_RUMBO) else FileAccess.WRITE)
		if archivo:
			archivo.seek_end()
			archivo.store_line("[%s] %s" % [Time.get_time_string_from_system(), _ultimo_debug_rumbo])
			archivo.close()
		cartel_central.text = "📝 Debug guardado"
		cartel_central.visible = true
		get_tree().create_timer(1.5).timeout.connect(func(): cartel_central.visible = false)
	debug_rumbo_anterior = debug_rumbo_activo

	# Freno de emergencia -- tecla D o botón de joystick, de un solo golpe.
	var freno_emergencia_activo = Input.is_physical_key_pressed(KEY_D) or _joystick_activo("freno_emergencia")
	if freno_emergencia_activo and not freno_emergencia_anterior and velocidad_actual > VELOCIDAD_FRENO_EMERGENCIA:
		velocidad_actual = VELOCIDAD_FRENO_EMERGENCIA
		_actualizar_etiqueta_velocidad()
		cartel_central.text = "🛑 Freno de emergencia"
		cartel_central.visible = true
		get_tree().create_timer(1.5).timeout.connect(func(): cartel_central.visible = false)
	freno_emergencia_anterior = freno_emergencia_activo

	# Freno progresivo (pedido 2026-09-27, "una vez que ya aterricé necesito
	# bajar de a poco, no un freno que se clave -- y que cada vez que lo
	# apriete, frene más rápido"): un solo botón de un golpe (tecla B o
	# joystick), a diferencia del freno de emergencia (que salta directo a
	# un valor fijo), este resta velocidad GRADUALMENTE cada cuadro. Cada
	# apretada ADICIONAL mientras ya está frenando sube la intensidad (más
	# rápido el descenso), en vez de reiniciarlo -- así "una vez" es un
	# frenado suave tipo "50, 49, 48...", y varias apretadas seguidas bajan
	# la velocidad mucho más rápido, hasta el tope.
	var freno_gradual_activo = Input.is_physical_key_pressed(KEY_B) or _joystick_activo("freno_gradual")
	if freno_gradual_activo and not freno_gradual_anterior:
		if en_tierra and not _es_helicoptero() and bool(_aterrizaje_actual()["modo_tierra"]) and velocidad_actual > VEL_TIERRA_FINA:
			intensidad_frenado_gradual = FRENADO_GRADUAL_MAXIMO
			_freno_rapido_tierra = true
		else:
			intensidad_frenado_gradual = clamp(
				intensidad_frenado_gradual + FRENADO_GRADUAL_INCREMENTO if intensidad_frenado_gradual > 0.0 else FRENADO_GRADUAL_BASE,
				0.0, FRENADO_GRADUAL_MAXIMO)
		cartel_central.text = "🛑 Freno progresivo (-%.0f/s)" % intensidad_frenado_gradual
		cartel_central.visible = true
		get_tree().create_timer(1.0).timeout.connect(func(): cartel_central.visible = false)
	freno_gradual_anterior = freno_gradual_activo

	# Tecla T (de "Trackear") o botón de joystick asignado a "reservado_1":
	# marca a mano la coordenada exacta del checkpoint actual del City Tour,
	# ver comentario junto a "city_tour_trackeado" más arriba.
	var tecla_trackear_activa = Input.is_physical_key_pressed(KEY_T) or _joystick_activo("reservado_1")
	if tecla_trackear_activa and not tecla_trackear_anterior:
		_trackear_checkpoint_city_tour()
	tecla_trackear_anterior = tecla_trackear_activa

	# Tecla I: prender/apagar el ILS sin soltar el mouse a buscar el botón
	# (pedido explícito, "estoy a oscuras con el teclado, hasta que agarro
	# el mouse ya me pasé"). Mismo de un solo golpe que M/D de arriba.
	# Pedido 2026-09-26: mismo toggle asignable a un botón de joystick
	# ("así no tengo que usar más mouse, activo/desactivo desde acá").
	var tecla_ils_activa = Input.is_physical_key_pressed(KEY_I) or _joystick_activo("activar_ils")
	if tecla_ils_activa and not tecla_ils_anterior:
		ils_activo = not ils_activo
		mundo.alternar_ils(ils_activo)
		boton_ils.text = "🎯 ILS: ON" if ils_activo else "🎯 Activar ILS"
	tecla_ils_anterior = tecla_ils_activa

	# El primer cuadro SIEMPRE está nivelado (banco_actual/cabeceo_actual
	# arrancan en 0.0), así que esta condición también cubre la alineación
	# inicial -- no hace falta un flag "_alineado_con_vertical_real" aparte.
	if abs(banco_actual) < 0.5 and abs(cabeceo_actual) < 0.5:
		_alinear_con_vertical_real(delta)
	match estado:
		Estado.VOLANDO, Estado.LLEGADA:
			_procesar_vuelo(delta)
		Estado.FRENANDO:
			_procesar_frenado(delta)
		Estado.DETENIDO:
			_procesar_detenido(delta)

	# PROBADO Y DESCARTADO: agregar acá un global_transform.basis =
	# global_transform.basis.orthonormalized() cada cuadro, pensando que era
	# una salvaguarda inofensiva, en realidad CAUSÓ una caída y deriva
	# constante incluso volando derecho sin tocar ninguna flecha (confirmado
	# por el usuario) -- lo saqué. No volver a agregarlo "por las dudas".

	# Sonido de motor: el tono sube un poco con la velocidad (sensación de
	# aceleración), en vez de quedar siempre igual.
	sonido_motor.pitch_scale = lerp(PITCH_MOTOR_MINIMO, PITCH_MOTOR_MAXIMO, clamp(velocidad_actual / _ajustes_avion_actual()["vel_maxima"], 0.0, 1.0))

	_actualizar_linea_guia_frame()
	_actualizar_aros_licencia_frame()

	if _mostrar_sugerencia_ils:
		_tiempo_sugerencia_ils += delta
		label_sugerencia_ils.visible = true
		label_sugerencia_ils.modulate.a = 0.4 + 0.6 * abs(sin(_tiempo_sugerencia_ils * 4.0))
	else:
		label_sugerencia_ils.visible = false

	_actualizar_camara()
	_actualizar_mapa_calles(delta)

# Convierte lat/lon a coordenadas de tesela "slippy map" (el estándar que usan
# OpenStreetMap, Google, Bing, etc.) y pide una nueva imagen solo cuando el
# avión se movió lo suficiente como para cambiar de tesela -- así no baja una
# imagen nueva cada cuadro, solo cuando hace falta.
func _actualizar_mapa_calles(delta: float) -> void:
	if mapa_zoom_automatico:
		_tiempo_zoom_automatico += delta
		if _tiempo_zoom_automatico >= INTERVALO_ZOOM_AUTOMATICO:
			_tiempo_zoom_automatico = 0.0
			var altura_actual: float = mundo.altitud_avion + position.y - altura_piso
			var nuevo_zoom: int = _zoom_mapa_para_altura(altura_actual)
			if nuevo_zoom != zoom_mapa:
				zoom_mapa = nuevo_zoom
				tile_x_mapa = -999999
				acumulador_mapa = 999.0

	var adelante_mapa = -global_transform.basis.z
	adelante_mapa.y = 0
	adelante_mapa = adelante_mapa.normalized()
	var rumbo_mapa = fposmod(rad_to_deg(atan2(adelante_mapa.x, -adelante_mapa.z)), 360.0)
	icono_avion_mapa.rotation = deg_to_rad(rumbo_mapa)

	# Coordenadas EXACTAS (con decimales) dentro de la grilla de teselas --
	# la parte entera (floor) dice qué tesela mostrar, la parte fraccionaria
	# dice EXACTAMENTE dónde cae un punto adentro de esa tesela (0..1). Antes
	# el avión quedaba siempre fijo en el centro y la imagen "saltaba" debajo
	# suyo -- ahora es al revés (como GeoFS/Google Maps): la imagen se queda
	# quieta y el avioncito se desliza suavemente sobre ella, y solo cuando
	# se sale de la tesela actual se pide una nueva.
	var n: float = pow(2.0, zoom_mapa)

	# Pedido 2026-10-17: la tesela que se MUESTRA depende del "centro" del
	# mapa (que por defecto es el avión, pero el arrastre lo puede mover a
	# mano -- ver _mapa_rect_gui_input/_input); el ícono siempre se dibuja
	# en la posición REAL del avión, relativa a esa tesela, así que si se
	# arrastró lejos el ícono puede quedar corrido hacia un borde del panel.
	var lat_centro: float = mundo.lat_avion
	var lon_centro: float = mundo.lon_avion
	if not mapa_siguiendo_avion:
		lat_centro = mapa_centro_lat
		lon_centro = mapa_centro_lon
	var lat_rad_centro: float = deg_to_rad(lat_centro)
	var x_exacto_centro: float = (lon_centro + 180.0) / 360.0 * n
	var y_exacto_centro: float = (1.0 - log(tan(lat_rad_centro) + 1.0 / cos(lat_rad_centro)) / PI) / 2.0 * n
	var xtile: int = int(floor(x_exacto_centro))
	var ytile: int = int(floor(y_exacto_centro))

	var lat_rad_avion: float = deg_to_rad(mundo.lat_avion)
	var x_exacto_avion: float = (mundo.lon_avion + 180.0) / 360.0 * n
	var y_exacto_avion: float = (1.0 - log(tan(lat_rad_avion) + 1.0 / cos(lat_rad_avion)) / PI) / 2.0 * n
	var frac_x: float = clamp(x_exacto_avion - xtile, -1.0, 2.0)
	var frac_y: float = clamp(y_exacto_avion - ytile, -1.0, 2.0)

	# El ícono se desliza dentro de la baldosa actual, en proporción al
	# tamaño del panel (si el panel es más grande, se desliza más lejos).
	icono_avion_mapa.position = Vector2(frac_x * mapa_rect.size.x, frac_y * mapa_rect.size.y)

	acumulador_mapa += delta
	if acumulador_mapa < 0.4:
		return
	acumulador_mapa = 0.0

	if xtile == tile_x_mapa and ytile == tile_y_mapa:
		return
	tile_x_mapa = xtile
	tile_y_mapa = ytile
	_pedir_baldosa_de_mapa(xtile, ytile)

# "Topes" de zoom según altura (pedido 2026-09-27, "que se vean las calles
# a 500-1000m, pero no perder el mapa completo si vuela alto"). Niveles de
# zoom estándar de OpenStreetMap (más alto = más detalle/más cerca).
func _zoom_mapa_para_altura(altura: float) -> int:
	# REAJUSTADO 2026-10-11 (pedido: "la bolita del avión va corriendo muy
	# rápido en el mapa, se ven las calles -- que recién se empiecen a ver
	# debajo de 1000m, y arriba de eso 4 niveles de zoom más alejado que
	# ahora"): por debajo de 1000m queda el detalle de calle de siempre; por
	# encima, un escalón de 4 niveles de golpe (mucho menos detalle) y sigue
	# alejándose cada 500m adicionales.
	if altura < 1000.0:
		return 17
	var escalon_extra: int = int(floor((altura - 1000.0) / 500.0))
	return clamp(17 - 4 - escalon_extra, ZOOM_MAPA_MINIMO, 17)

func _pedir_baldosa_de_mapa(xtile: int, ytile: int) -> void:
	if descargando_mapa:
		return
	descargando_mapa = true
	var url := "https://tile.openstreetmap.org/%d/%d/%d.png" % [zoom_mapa, xtile, ytile]
	var error := peticion_mapa.request(url, ["User-Agent: SimuladorDeVueloGodot/1.0"])
	if error != OK:
		descargando_mapa = false
		push_warning("No se pudo iniciar la descarga de la baldosa del mapa (error %d)" % error)

# Zoom con la ruedita del mouse -- fuerza que la próxima tesela se pida ya
# mismo (no espera los 2 segundos normales) y a un nivel de detalle distinto.
# Tamaño/posición chica de siempre del mapa -- pedido 2026-09-27, "que
# vuelva sola a los pocos segundos de soltarla" (auto-retorno) además del
# "botón de pánico" de doble clic que ya existía. El mapa vive anclado
# abajo a la derecha (dentro del panel de instrumentos), así que estos son
# offsets NEGATIVOS medidos desde ese borde.
func _resetear_mapa_a_tamano_default() -> void:
	mapa_rect.offset_left = -264.0
	mapa_rect.offset_top = 151.0
	mapa_rect.offset_right = 0.0
	mapa_rect.offset_bottom = 406.0
	_modo_resize_mapa = ModoResizeMapa.NINGUNO
	_arrastrando_mover_mapa = false

func _mapa_rect_gui_input(event: InputEvent) -> void:
	# Doble clic = "botón de pánico" (pedido explícito 2026-09-21, "no se ve
	# el vértice de abajo, quiero poder minimizarlo yo mismo"): vuelve al
	# tamaño chico de siempre de un solo golpe, en cualquier parte del panel
	# (no hace falta encontrar la manija), así siempre hay forma de recuperar
	# el control aunque el panel haya quedado gigante por lo que sea.
	if event is InputEventMouseButton and event.pressed and event.double_click:
		_resetear_mapa_a_tamano_default()
		return
	# Pedido 2026-10-17 ("que se pueda arrastrar con el mouse para mirar al
	# costado, y que quede fijo donde se suelta"): clic izquierdo normal (sin
	# ser doble clic) arranca el arrastre del CONTENIDO del mapa -- distinto
	# de AsaMoverMapa, que mueve el PANEL entero en la pantalla; esto mueve
	# qué parte del mundo se ve adentro del panel. El movimiento real se
	# procesa en _input() (ver ahí), igual que el resto de los arrastres.
	if event is InputEventMouseButton and event.button_index == MOUSE_BUTTON_LEFT:
		if event.pressed:
			if mapa_siguiendo_avion:
				mapa_centro_lat = mundo.lat_avion
				mapa_centro_lon = mundo.lon_avion
				mapa_siguiendo_avion = false
			_arrastrando_contenido_mapa = true
		else:
			_arrastrando_contenido_mapa = false
		return
	if event is InputEventMouseButton and event.pressed:
		if event.button_index == MOUSE_BUTTON_WHEEL_UP:
			zoom_mapa = clamp(zoom_mapa + 1, ZOOM_MAPA_MINIMO, ZOOM_MAPA_MAXIMO)
		elif event.button_index == MOUSE_BUTTON_WHEEL_DOWN:
			zoom_mapa = clamp(zoom_mapa - 1, ZOOM_MAPA_MINIMO, ZOOM_MAPA_MAXIMO)
		else:
			return
		# Zoom manual con la ruedita = override temporal del automático (si
		# está activo): reinicia el cronómetro, dándole ~1 minuto de zoom
		# manual antes de que el automático vuelva a ajustar según altura.
		_tiempo_zoom_automatico = 0.0
		tile_x_mapa = -999999
		acumulador_mapa = 999.0

func _on_peticion_mapa_completada(_resultado, codigo_respuesta: int, _headers, cuerpo: PackedByteArray) -> void:
	descargando_mapa = false
	if codigo_respuesta != 200:
		push_warning("El servidor del mapa devolvió código %d" % codigo_respuesta)
		return
	var imagen := Image.new()
	if imagen.load_png_from_buffer(cuerpo) == OK:
		mapa_rect.texture = ImageTexture.create_from_image(imagen)
	else:
		push_warning("No se pudo decodificar la baldosa del mapa")

# Cámara "estabilizada": sigue al avión en posición y en RUMBO (hacia dónde
# va), pero ignora a propósito su inclinación (banco) y cabeceo. Por eso el
# horizonte se ve siempre firme y lo que se ve moverse/inclinarse en pantalla
# es el avioncito, como pidió el usuario ("que no se mueva el paisaje").
func _actualizar_camara() -> void:
	var arriba_real: Vector3 = _arriba_real()

	# IMPORTANTE (este era el bug del "vuela al revés"): antes se hacía
	# "adelante.y = 0" para quedarnos solo con el RUMBO del avión, ignorando
	# a propósito su cabeceo/banco (para que el horizonte no se mueva en
	# pantalla). Eso descontaba la componente en el eje Y FIJO del motor --
	# válido solo cuando ese eje era "arriba". Ahora que el avión está
	# alineado con la vertical REAL (ver _alinear_con_vertical_real, que ya
	# no es (0,1,0)), hay que descontar la componente de ESA vertical real,
	# no la del eje Y fijo -- si no, el vector quedaba distorsionado (a veces
	# hasta con el sentido dominante invertido), y el avión parecía volar al
	# revés aunque translate() siempre avanzó bien en su -Z local.
	var adelante = -global_transform.basis.z
	adelante -= adelante.dot(arriba_real) * arriba_real
	if adelante.length() > 0.001:
		adelante = adelante.normalized()
	else:
		adelante = Vector3.FORWARD

	# Zoom según altitud: alto = cámara más lejos/alta (vista panorámica),
	# cerca del piso = cámara más pegada (sensación de detalle al aterrizar).
	var altura_vuelo = mundo.altitud_avion + position.y - altura_piso
	var factor_zoom = clamp(altura_vuelo / 80.0, 0.0, 1.0)
	var distancia = lerp(OFFSET_CAMARA_DISTANCIA, OFFSET_CAMARA_DISTANCIA * 2.2, factor_zoom)
	# Pedido 2026-10-11 ("el 747 se ve muy abajo, como si lo mirara desde
	# arriba"): algunos modelos tienen el pivote (0,0,0) descentrado
	# respecto al centro visual del fuselaje (distinto para cada GLB
	# bajado) -- "ajuste_altura_camara" (opcional, 0.0 por defecto) permite
	# subir/bajar la cámara SOLO para ese avión en particular, sin tocar la
	# altura global de los demás.
	var ajuste_altura_camara: float = float(TIPOS_AVION[tipo_avion_indice].get("ajuste_altura_camara", 0.0))
	var altura_cam = lerp(OFFSET_CAMARA_ALTURA, OFFSET_CAMARA_ALTURA * 3.0, factor_zoom) + ajuste_altura_camara
	# PROBADO Y DESCARTADO: invertir el signo acá (a "+ adelante") empeoró
	# todo -- confirma que la cámara SÍ va detrás del avión correctamente con
	# "- adelante". El problema de "viene hacia mí" es otra cosa, no esto.
	camara.global_position = global_position - adelante * distancia + arriba_real * altura_cam

	# Anti-singularidad (aporte de Gemini, válido): si la dirección de mirada
	# de la cámara queda casi paralela a "arriba_real", look_at() puede
	# pegar un salto/giro brusco (gimbal lock). Si eso pasa, usamos el
	# propio "arriba" del avión como referencia de respaldo para ese cuadro.
	var mirar_hacia: Vector3 = global_position + arriba_real * 0.5
	var direccion_mirada: Vector3 = (mirar_hacia - camara.global_position).normalized()
	var arriba_para_camara: Vector3 = arriba_real
	if abs(direccion_mirada.dot(arriba_real)) > 0.98:
		arriba_para_camara = global_transform.basis.y
	camara.look_at(mirar_hacia, arriba_para_camara)

func _procesar_vuelo(delta: float) -> void:
	# Sensibilidad ajustable por avión (pedido 2026-09-21, panel chico a la
	# derecha, "no va a estar en el juego final pero ahora sí" -- ver
	# _ajustes_avion_actual()). Para el helicóptero el cabeceo usa el tope fijo
	# de siempre (el "vertical" ajustable del helicóptero es el empuje R/F,
	# algo aparte -- ver más abajo), para los demás aviones el cabeceo ES el
	# ajuste "vertical" (es como suben/bajan, no tienen empuje vertical propio).
	var ajustes := _ajustes_avion_actual()
	var cabeceo_maximo_efectivo: float = CABECEO_MAXIMO if _es_helicoptero() else ajustes["vertical"]

	# Cabeceo/banco objetivo -- dos formas de controlarlo:
	# 1) Flechas/joystick de siempre (ON/OFF a tope, como un interruptor).
	# 2) Mouse tipo GeoFS (pedido 2026-09-21, "buscalo ahí si no sabés cómo
	#    es"): el centro de la pantalla es "neutro", y alejar el mouse del
	#    centro da cada vez más inclinación/cabeceo, PROPORCIONAL a la
	#    distancia -- como un joystick virtual dibujado invisible sobre la
	#    pantalla. El acelerador (W/S) sigue siendo siempre de teclado, en
	#    los dos modos -- por eso el modo se llama "mouse Y teclado".
	var cabeceo_objetivo = 0.0
	var banco_objetivo = 0.0
	if control_mouse_activo:
		var centro: Vector2 = get_viewport().get_visible_rect().size / 2.0
		var offset: Vector2 = get_viewport().get_mouse_position() - centro
		var factor_x: float = clamp(offset.x / centro.x, -1.0, 1.0)
		var factor_y: float = clamp(offset.y / centro.y, -1.0, 1.0)
		banco_objetivo = -factor_x * ajustes["angulo"]
		cabeceo_objetivo = -factor_y * cabeceo_maximo_efectivo
	else:
		# Cabeceo: apunta a un ángulo máximo mientras mantenés la flecha (como
		# el banco), y se autonivela solo al soltar -- ver comentario arriba.
		if Input.is_action_pressed("ui_up") or _joystick_activo("cabeceo_arriba"):
			cabeceo_objetivo = cabeceo_maximo_efectivo
		elif Input.is_action_pressed("ui_down") or _joystick_activo("cabeceo_abajo"):
			cabeceo_objetivo = -cabeceo_maximo_efectivo
		# Alabeo: la flecha define hacia dónde QUEREMOS inclinarnos, pero la
		# inclinación de verdad se acerca de a poco a ese objetivo
		# (move_toward), nunca salta de golpe. Si no apretás nada, se va
		# nivelando solo.
		if Input.is_action_pressed("ui_left") or _joystick_activo("alabeo_izquierda"):
			banco_objetivo = ajustes["angulo"]
		elif Input.is_action_pressed("ui_right") or _joystick_activo("alabeo_derecha"):
			banco_objetivo = -ajustes["angulo"]

	# MODO TIERRA (pedido 2026-10-16): en pista el avión queda nivelado (no
	# cabecea ni se inclina, así no puede enterrar el ala), gira sobre su eje
	# como un auto y solo despega si llega a la velocidad de despegue y tirás
	# para atrás. "entrada_giro" es +1 izquierda / -1 derecha, sale de la misma
	# flecha/mouse que en el aire.
	var entrada_giro: float = banco_objetivo / max(float(ajustes["angulo"]), 0.01)
	var en_modo_tierra: bool = en_tierra and not _es_helicoptero() and bool(_aterrizaje_actual()["modo_tierra"])
	if en_modo_tierra:
		bloqueo_despegue = max(0.0, bloqueo_despegue - delta)
		# BUG REAL encontrado 2026-10-17 (reportado: "nunca puedo despegar,
		# siempre lo tengo que hacer a mano" -- antes hacía falta ADEMÁS
		# tirar del stick para arriba, justo cuando ya se llegó a la
		# velocidad de despegue -- fácil de no coincidir). Pedido explícito:
		# "cuando pasás la velocidad, esté donde esté, podés despegar, se
		# desactiva solo el modo tierra" -- ya no hace falta tirar del stick,
		# solo la velocidad. SEGUNDA VUELTA (reportado: "a los 60 ya despega,
		# tiene que ser 120"): algunos aviones tienen "vel_despegue"/
		# "vel_minima" configurados muy bajo (vos mismo bajaste vel_minima a
		# 0 para arreglar el bug del freno) -- un piso de 120 real, pase lo
		# que pase la configuración del avión.
		if velocidad_actual >= max(120.0, _vel_despegue_efectiva()) and bloqueo_despegue <= 0.0:
			_despegar_de_pista()
			en_modo_tierra = false
		else:
			# Tira para arriba pero todavía no llega a la velocidad de despegue: avisar.
			_tiempo_aviso_despegue = max(0.0, _tiempo_aviso_despegue - delta)
			# PAUSADO 2026-10-17 (pedido: "aparece a cada rato un cartel molesto
			# que dice que hace falta tal velocidad -- desactivalo, marea y
			# confunde cuando todavía no cumplí la misión y estoy taxeando").
			# El otro aviso ("Esperá un instante") sigue activo, no se tocó.
			if false and cabeceo_objetivo > cabeceo_maximo_efectivo * 0.3 and velocidad_actual < _vel_despegue_efectiva() and _tiempo_aviso_despegue <= 0.0:
				_tiempo_aviso_despegue = 3.0
				_mostrar_cartel_aterrizaje("Muy lento para despegar: hace falta %d" % int(_vel_despegue_efectiva()), Color(1.0, 0.9, 0.4))
			elif cabeceo_objetivo > cabeceo_maximo_efectivo * 0.3 and bloqueo_despegue > 0.0 and velocidad_actual >= _vel_despegue_efectiva() and _tiempo_aviso_despegue <= 0.0:
				_tiempo_aviso_despegue = 3.0
				_mostrar_cartel_aterrizaje("Esperá un instante para despegar", Color(1.0, 0.9, 0.4))
			cabeceo_objetivo = 0.0
			banco_objetivo = 0.0

	# Multiplicador por avión (pedido 2026-09-27, panel de Sensibilidad,
	# "Vel. animación") -- cada avión puede tener su propia suavidad/
	# agresividad en cómo la nariz sube/baja o el avión se inclina, sin
	# tocar las constantes globales de arriba.
	var velocidad_cabeceo_efectiva = (VELOCIDAD_CABECEO_ENTRADA if cabeceo_objetivo != 0.0 else VELOCIDAD_CABECEO_SALIDA) * ajustes["velocidad_animacion"]
	var cabeceo_anterior = cabeceo_actual
	cabeceo_actual = move_toward(cabeceo_actual, cabeceo_objetivo, velocidad_cabeceo_efectiva * delta)
	var delta_cabeceo = cabeceo_actual - cabeceo_anterior
	if abs(delta_cabeceo) > 0.0001:
		# Mismo signo que antes: "ui_up" (cabeceo positivo) sube la nariz.
		rotate_object_local(Vector3.RIGHT, -deg_to_rad(delta_cabeceo))

	var velocidad_banco = (VELOCIDAD_BANCO_ENTRADA if banco_objetivo != 0.0 else VELOCIDAD_BANCO_SALIDA) * ajustes["velocidad_animacion"]
	var banco_anterior = banco_actual
	banco_actual = move_toward(banco_actual, banco_objetivo, velocidad_banco * delta)
	var delta_banco = banco_actual - banco_anterior
	if abs(delta_banco) > 0.0001:
		# Signo corregido: antes, al girar a la derecha, el modelo se inclinaba
		# visualmente para la izquierda (y viceversa) -- el rumbo giraba bien,
		# pero el banco visual quedaba invertido respecto al giro real.
		rotate_object_local(Vector3.FORWARD, -deg_to_rad(delta_banco))

	# El giro (rumbo) sale SOLO de la inclinación actual, no de apretar la
	# flecha directamente -- nivelado no gira, inclinado gira, más inclinado
	# gira más rápido.
	# VUELTA ATRÁS 2026-09-21 (pedido explícito: "giro a la derecha y de
	# golpe cabecea para abajo, es horrible e innecesario"): con el eje LOCAL
	# (rotate_object_local(Vector3.UP,...)) el giro rota alrededor del
	# "arriba" DEL PROPIO AVIÓN -- que ya está inclinado por el banco -- y
	# esa combinación mete un cabeceo hacia abajo no pedido por el jugador
	# (es el mismo efecto por el que un avión real "cae" en una curva si no
	# se compensa con la palanca, pero acá no lo queremos, prioridad
	# absoluta es que se sienta cómodo). Rotando en cambio alrededor del eje
	# MUNDIAL (_arriba_real(), la vertical geodésica) con global_rotate(), el
	# cabeceo y el banco quedan invariantes -- solo cambia hacia dónde
	# apunta la nariz, nunca cuánto sube/baja. El motivo por el que esto se
	# había revertido antes (rumbo desalineado del recorrido real) era de
	# cuando el cálculo de rumbo todavía tenía el bug de la curvatura de la
	# Tierra (ver mundo.gd/este_motor_actual) -- ya arreglado esa vez, así
	# que no debería reaparecer, pero probar bien un viaje largo con curvas.
	if en_modo_tierra:
		var cfg_giro: Dictionary = _aterrizaje_actual()
		# Como un auto: la velocidad de giro sale de velocidad / radio de giro, así
		# parado no gira (no puede pivotar en el lugar) y "giro_suelo" es el tope.
		var giro_por_radio: float = rad_to_deg(velocidad_actual / max(float(cfg_giro["radio_giro"]), 1.0))
		var giro_max: float = min(giro_por_radio, float(cfg_giro["giro_suelo"]))
		if abs(entrada_giro) > 0.02:
			global_rotate(_arriba_real(), deg_to_rad(clamp(entrada_giro, -1.0, 1.0) * giro_max * delta))
	elif abs(banco_actual) > 0.5:
		global_rotate(_arriba_real(), deg_to_rad(banco_actual * ajustes["giro"] * delta))

	# Acelerador: W sube la velocidad de crucero, S la baja, dentro de un
	# rango fijo (arranca despacito, se puede llevar hasta el tope si no
	# querés bancarte un viaje largo entero).
	# En tierra el freno puede llegar hasta 0 (no se queda en la velocidad
	# mínima de vuelo) y frena al ritmo propio de la pestaña "Aterrizaje".
	var piso_velocidad: float = 0.0 if en_modo_tierra else float(ajustes["vel_minima"])
	var tasa_freno: float = float(_aterrizaje_actual()["freno_suelo"]) if en_modo_tierra else ACELERACION
	if Input.is_physical_key_pressed(KEY_W) or _joystick_activo("acelerar"):
		intensidad_frenado_gradual = 0.0  # acelerar cancela el freno progresivo en curso
		_freno_rapido_tierra = false
		var tasa_acel: float = float(_aterrizaje_actual()["acel_suelo"]) if en_modo_tierra else ACELERACION
		velocidad_actual = min(ajustes["vel_maxima"], velocidad_actual + tasa_acel * delta)
		_actualizar_etiqueta_velocidad()
	elif Input.is_physical_key_pressed(KEY_S) or _joystick_activo("frenar"):
		velocidad_actual = max(piso_velocidad, velocidad_actual - tasa_freno * delta)
		_actualizar_etiqueta_velocidad()
	elif intensidad_frenado_gradual > 0.0:
		# Freno progresivo: resta velocidad de a poco (nunca de golpe), a un
		# ritmo que sube con cada apretada extra del botón (ver más arriba).
		# A diferencia de "S", este SÍ puede llegar hasta 0 -- pensado para
		# frenar del todo después de aterrizar.
		velocidad_actual = max(0.0, velocidad_actual - intensidad_frenado_gradual * delta)
		_actualizar_etiqueta_velocidad()
		if _freno_rapido_tierra and velocidad_actual <= VEL_TIERRA_FINA:
			velocidad_actual = VEL_TIERRA_FINA
			intensidad_frenado_gradual = 0.0
			_freno_rapido_tierra = false
		if velocidad_actual <= 0.0:
			intensidad_frenado_gradual = 0.0

	# Rodadura: resistencia natural al rodar en tierra (0 = sigue rodando sin
	# frenar solo, como pidió Pablo; se sube en la pestaña "Aterrizaje").
	if en_modo_tierra and not (Input.is_physical_key_pressed(KEY_W) or _joystick_activo("acelerar")):
		var rodadura: float = float(_aterrizaje_actual()["rodadura"])
		if rodadura > 0.0:
			velocidad_actual = max(0.0, velocidad_actual - rodadura * delta)

	translate(Vector3(0, 0, -velocidad_actual * delta))

	# Viento (pedido explícito 2026-10-03, "un botón que apretamos y pierde
	# un poco de estabilidad, si viene de la izquierda que te empuje"):
	# empuje lateral en espacio REAL (norte/este, no ejes crudos del motor,
	# misma regla de oro de siempre) hacia donde sopla el viento. La
	# sensibilidad es por avión (uno chico se mueve más con el mismo
	# viento que uno grande) -- panel Sensibilidad, fila "💨 Viento".
	if viento_activo:
		var viento_rad: float = deg_to_rad(viento_direccion_grados + 180.0)
		var direccion_empuje: Vector3 = mundo.norte_motor_actual * cos(viento_rad) + mundo.este_motor_actual * sin(viento_rad)
		var velocidad_viento_ms: float = viento_velocidad_kmh / 3.6
		global_position += direccion_empuje * velocidad_viento_ms * ajustes["viento_sensibilidad"] * delta

	# Subida/bajada vertical -- SOLO para el helicóptero (pedido explícito, ya
	# tiene los botones de joystick asignados desde que armamos el mapeo). Se
	# mueve en línea recta sobre la vertical REAL (no la del avión, que puede
	# estar inclinada) para que "subir" siempre sea subir de verdad.
	if _es_helicoptero():
		# O/L agregadas como alternativa a R/F (pedido explícito, "por si hay
		# que mapear otro trabajo, más fácil en el helicóptero") -- las dos
		# combinaciones funcionan igual, ninguna reemplaza a la otra.
		if Input.is_physical_key_pressed(KEY_R) or Input.is_physical_key_pressed(KEY_O) or _joystick_activo("vertical_arriba"):
			global_position += _arriba_real() * ajustes["vertical"] * delta
		elif Input.is_physical_key_pressed(KEY_F) or Input.is_physical_key_pressed(KEY_L) or _joystick_activo("vertical_abajo"):
			global_position -= _arriba_real() * ajustes["vertical"] * delta

	_limitar_piso(delta)
	_actualizar_hud(delta)

	# Una vez que llegaste, podés frenar apretando ESPACIO (siempre que estés
	# centrado sobre la pista, no en cualquier lado).
	if estado == Estado.LLEGADA and Input.is_action_just_pressed("ui_accept"):
		estado = Estado.FRENANDO

func _procesar_frenado(delta: float) -> void:
	velocidad_actual = max(0.0, velocidad_actual - 8.0 * delta)
	translate(Vector3(0, 0, -velocidad_actual * delta))
	_limitar_piso(delta)
	_actualizar_hud(delta)
	if velocidad_actual <= 0.05:
		velocidad_actual = 0.0
		estado = Estado.DETENIDO
		cartel_central.text = "🏁 MISIÓN CUMPLIDA"
		cartel_central.visible = true
		boton_despegar.visible = true

func _procesar_detenido(_delta: float) -> void:
	# Se queda esperando tranquilo hasta que apretés "Despegar" -- no arranca
	# solo, para que puedas hacer una pausa tranquila si querés.
	pass

func _ocultar_flechas() -> void:
	flecha_izquierda.visible = false
	flecha_derecha.visible = false
	flecha_arriba.visible = false
	flecha_abajo.visible = false
	etiqueta_rumbo_objetivo.visible = false
	linea_guia.visible = false
	_hay_destino_para_linea_guia = false

func _despegar() -> void:
	_reiniciar_estado_tierra()
	tiempo_sin_toque = 2.0
	indice_destino += 1
	velocidad_actual = VELOCIDAD_INICIAL
	_actualizar_etiqueta_velocidad()
	estado = Estado.VOLANDO
	cartel_central.visible = false
	boton_despegar.visible = false

# Piso REAL: el plugin genera colisión física de verdad para el terreno de
# Cesium (StaticBody3D por baldosa, activado por defecto). En vez de asumir
# un piso plano en Y=0 (que no tiene por qué coincidir con la altura real del
# terreno -- por eso el avión terminaba "enterrado" apenas arrancaba), tiramos
# un rayo hacia abajo (según la vertical real, no Y fijo) y usamos dónde
# choca de verdad. Si todavía no cargó ninguna baldosa ahí (rayo sin
# resultado), no hacemos nada -- mejor dejarlo flotar que inventar un piso.
const MARGEN_SOBRE_PISO = 0.3
func _limitar_piso(delta: float = 0.016) -> void:
	var arriba_real: Vector3 = _arriba_real()
	var space_state = get_world_3d().direct_space_state
	# IMPORTANTE: el rayo tiene que arrancar bien arriba de CUALQUIER terreno
	# real posible, no solo "un poco arriba del avión" -- si el avión ya
	# arrancó por error debajo del terreno (pasó, ver bitácora), "50m arriba
	# del avión" seguía estando bajo tierra, y el rayo agarraba resultados
	# inconsistentes cuadro a cuadro (el "loop" entre -75 y -30 que se vio).
	# 20.000m arriba es más que de sobra para cualquier altura real de vuelo.
	var origen_rayo = global_position + arriba_real * 20000.0
	var destino_rayo = global_position - arriba_real * 20000.0
	var consulta = PhysicsRayQueryParameters3D.create(origen_rayo, destino_rayo)
	consulta.collision_mask = 1  # solo terreno real: blancos, barcos e invasores (otras capas) no son piso
	var resultado = space_state.intersect_ray(consulta)
	if not resultado:
		return
	# Ponemos la posición DIRECTO al piso + margen (no sumamos un delta) --
	# así no se puede acumular error de un cuadro al otro si el rayo da
	# resultados apenas distintos cada vez.
	var punto_piso: Vector3 = resultado.position
	var altura_actual: float = (global_position - punto_piso).dot(arriba_real)
	var cfg: Dictionary = _aterrizaje_actual()

	# Sin modo tierra (apagado en la pestaña "Aterrizaje") o helicóptero (aterriza
	# en vertical, no rueda por pista): el comportamiento de siempre.
	if not bool(cfg["modo_tierra"]) or _es_helicoptero():
		_altura_sobre_piso_prev = -1.0
		en_tierra = false
		if altura_actual < MARGEN_SOBRE_PISO:
			global_position = punto_piso + arriba_real * MARGEN_SOBRE_PISO
		return

	var tren: float = _altura_tren_efectiva()
	var velocidad_vertical: float = 0.0
	if _altura_sobre_piso_prev >= 0.0 and delta > 0.0:
		velocidad_vertical = (altura_actual - _altura_sobre_piso_prev) / delta
	# Un salto enorme de un cuadro al otro es un teletransporte (nuevo viaje,
	# cambio de lugar), no una caída real.
	if abs(velocidad_vertical) > 400.0:
		velocidad_vertical = 0.0
	_altura_sobre_piso_prev = altura_actual
	tiempo_sin_toque = max(0.0, tiempo_sin_toque - delta)

	if not en_tierra:
		if altura_actual > tren + 3.0:
			tiempo_en_aire += delta
		# BUG REAL encontrado 2026-10-17 (reportado: "el modo tierra no se
		# activa solo al aterrizar, lo tengo que prender a mano"): el margen
		# de detección era de 2CM y 5CM/s -- con el terreno de Cesium
		# temblando de a ratos (mismo fenómeno de siempre en este proyecto),
		# casi nunca coincidían los dos a la vez en el mismo cuadro exacto
		# del aterrizaje real. Aflojado a un margen que sigue siendo "recién
		# tocando", pero alcanzable de verdad.
		if altura_actual <= tren + 0.5 and tiempo_sin_toque <= 0.0 and velocidad_vertical <= 0.5:
			if tiempo_en_aire >= 2.0:
				_tocar_tierra(-velocidad_vertical)
			else:
				# Sin haber volado (recién empezó, rozó algo): NO se activa el modo tierra;
				# solo se evita que se entierre.
				global_position = punto_piso + arriba_real * tren
		elif altura_actual < tren:
			# Recién despegando (o en la pausa después de despegar): que no se entierre.
			global_position = punto_piso + arriba_real * tren
		if not en_tierra:
			return

	# --- En tierra ---
	# Rebote (toque brusco): saltitos que se achican hasta que se apoya del todo.
	if rebote_activo:
		rebote_velocidad -= GRAVEDAD_REBOTE * delta
		rebote_altura += rebote_velocidad * delta
		if rebote_altura <= 0.0:
			rebote_altura = 0.0
			if rebotes_restantes > 0:
				rebotes_restantes -= 1
				rebote_velocidad_inicial *= 0.5
				rebote_velocidad = rebote_velocidad_inicial
				if rebote_velocidad < 0.3:
					rebote_activo = false
					rebote_velocidad = 0.0
			else:
				rebote_activo = false
				rebote_velocidad = 0.0
	elif altura_actual > tren + 40.0:
		# Teletransporte (nuevo viaje): ya no está en el piso. Los saltos chicos del
		# terreno (Cesium cargando) NO sacan del modo tierra: solo se sale al despegar.
		# Exige que el salto dure más de 0.2s seguidos (ver _tiempo_altura_
		# sospechosa) -- un solo cuadro ruidoso del rayo al piso ya NO alcanza
		# para sacar del modo tierra (esto es lo que causaba el bug del freno
		# que empujaba a 150 de golpe en Aeroparque).
		_tiempo_altura_sospechosa += delta
		if _tiempo_altura_sospechosa > 0.2:
			en_tierra = false
			_tiempo_altura_sospechosa = 0.0
		return
	else:
		_tiempo_altura_sospechosa = 0.0
	# Suavizado del piso: el terreno de Cesium cambia de detalle y el rayo a veces
	# da saltos de metros de un cuadro al otro (pantalla que parpadea / "escupe al
	# aire"). La altura solo puede corregirse a un ritmo limitado.
	var objetivo_altura: float = tren + rebote_altura
	var error_altura: float = objetivo_altura - altura_actual
	var tope_correccion: float = 8.0 * delta
	var correccion: float = clamp(error_altura * min(1.0, 4.0 * delta), -tope_correccion, tope_correccion)
	if rebote_activo or abs(error_altura) > 15.0:
		correccion = error_altura
	global_position += arriba_real * correccion

# ============================================================
# MODO TIERRA: configuración por avión (pestaña "Aterrizaje")
# ============================================================

func _aterrizaje_por_defecto() -> Dictionary:
	return {
		"modo_tierra": true,
		"altura_tren": -1.0,     # -1 = automática (se mide del modelo)
		"vel_despegue": 0.0,     # 0 = automática (80% de la velocidad mínima)
		"giro_suelo": 30.0,
		"radio_giro": 60.0,
		"acel_suelo": 6.0,
		"freno_suelo": 25.0,
		"rodadura": 0.0,
		"aterr_suave": 8.0,
		"aterr_brusco": 22.0,
		"rebote_fuerza": 1.0,
		"rebotes": 1,
		"version": 2,
	}

func _aterrizaje_actual() -> Dictionary:
	var nombre: String = TIPOS_AVION[tipo_avion_indice]["nombre"]
	if not config_aterrizaje.has(nombre):
		config_aterrizaje[nombre] = _aterrizaje_por_defecto()
	return config_aterrizaje[nombre]

func _cargar_aterrizaje_aviones() -> void:
	var cfg := ConfigFile.new()
	if cfg.load(RUTA_CONFIG_ATERRIZAJE) != OK:
		return
	for nombre_avion in cfg.get_sections():
		var valores: Dictionary = _aterrizaje_por_defecto()
		# Archivos viejos (version 1) traían umbrales de toque/rebote demasiado
		# sensibles: se descartan esos y se mantiene el resto de lo ajustado.
		var vieja: bool = int(cfg.get_value(nombre_avion, "version", 1)) < 2
		for clave in valores.keys():
			if vieja and clave in ["aterr_suave", "aterr_brusco", "rebotes", "rebote_fuerza"]:
				continue
			valores[clave] = cfg.get_value(nombre_avion, clave, valores[clave])
		valores["version"] = 2
		config_aterrizaje[nombre_avion] = valores

func _guardar_aterrizaje_aviones() -> void:
	var cfg := ConfigFile.new()
	cfg.load(RUTA_CONFIG_ATERRIZAJE)
	var nombre: String = TIPOS_AVION[tipo_avion_indice]["nombre"]
	var valores: Dictionary = _aterrizaje_actual()
	for clave in valores.keys():
		cfg.set_value(nombre, clave, valores[clave])
	cfg.save(RUTA_CONFIG_ATERRIZAJE)

func _altura_tren_efectiva() -> float:
	var valor: float = float(_aterrizaje_actual()["altura_tren"])
	return valor if valor >= 0.0 else altura_tren_auto

func _vel_despegue_efectiva() -> float:
	var valor: float = float(_aterrizaje_actual()["vel_despegue"])
	if valor > 0.0:
		return valor
	var vel_minima: float = float(_ajustes_avion_actual()["vel_minima"])
	return max(30.0, vel_minima * 0.8)

# Mide qué tan abajo del centro del avión quedan las ruedas (o la panza), a
# partir del propio modelo -- así cada avión arranca con una altura de tren
# razonable (un A380 no tiene las ruedas a la misma altura que un Cessna) y
# después se afina a ojo en la pestaña "Aterrizaje".
func _medir_altura_tren() -> void:
	altura_tren_auto = MARGEN_SOBRE_PISO
	if modelo_externo and modelo_externo.get_child_count() > 0:
		var caja: AABB = _calcular_aabb_local(modelo_externo)
		if caja.size != Vector3.ZERO:
			var minimo_y: float = INF
			for i in range(8):
				var p: Vector3 = modelo_externo.transform * caja.get_endpoint(i)
				minimo_y = min(minimo_y, p.y)
			if minimo_y < -0.05 and minimo_y > -60.0:
				altura_tren_auto = -minimo_y
	_refrescar_panel_aterrizaje()

func _reiniciar_estado_tierra() -> void:
	en_tierra = false
	tiempo_en_aire = 0.0
	rebote_activo = false
	rebote_altura = 0.0
	rebote_velocidad = 0.0
	rebotes_restantes = 0
	_altura_sobre_piso_prev = -1.0

# Toque de ruedas: clasifica el aterrizaje por la velocidad de descenso y, si
# fue brusco, arranca los rebotes (la cantidad, la altura y los umbrales se
# editan en la pestaña "Aterrizaje"). Todavía sin choque/crash.
func _tocar_tierra(velocidad_descenso: float) -> void:
	en_tierra = true
	bloqueo_despegue = 4.0
	tiempo_en_aire = 0.0
	rebote_activo = false
	rebote_altura = 0.0
	var cfg: Dictionary = _aterrizaje_actual()
	var vd: float = max(0.0, velocidad_descenso)
	var suave: float = float(cfg["aterr_suave"])
	var brusco: float = float(cfg["aterr_brusco"])
	var texto: String = "Aterrizaje suave"
	var color: Color = Color(0.4, 1.0, 0.4)
	if vd >= brusco:
		texto = "¡Venís muy fuerte! Aterrizaje brusco"
		color = Color(1.0, 0.45, 0.25)
		var cantidad: int = int(cfg["rebotes"])
		var fuerza: float = float(cfg["rebote_fuerza"])
		if cantidad > 0 and fuerza > 0.0:
			rebote_activo = true
			rebotes_restantes = cantidad - 1
			rebote_velocidad_inicial = vd * 0.6 * fuerza
			rebote_velocidad = rebote_velocidad_inicial
	elif vd >= suave:
		texto = "Aterrizaje normal"
		color = Color(1.0, 0.9, 0.4)
	_mostrar_cartel_aterrizaje(texto, color)

func _despegar_de_pista() -> void:
	en_tierra = false
	tiempo_en_aire = 0.0
	rebote_activo = false
	rebote_altura = 0.0
	tiempo_sin_toque = 2.0
	_mostrar_cartel_aterrizaje("Despegaste", Color(0.5, 0.85, 1.0))

# Pedido 2026-10-16 ("desde la misión 1 aparecen cuadrados rojos por todos
# lados que no tienen nada que ver -- si no conocés los aeropuertos, te
# volvés loco buscando el que importa"): antes "modo_guerra" prendía TODOS
# los objetivos_armamento a la vez (barcos, aeropuertos, industria,
# invasión, Capital), sin importar qué misión estuviera activa. Ahora, si
# hay un paso de misión en curso, solo se activan los objetivos del grupo
# que corresponde a ESE paso -- el resto queda oculto y sin colisión igual
# que con el modo guerra apagado, aunque modo_guerra siga en true. No se
# borra nada (otra misión futura los puede necesitar), solo no estorban.
func _grupos_relevantes_paso_actual() -> Array:
	if mision_paso_actual < 0 or mision_paso_actual >= secuencia_mision_actual.size():
		return []
	var nombre: String = secuencia_mision_actual[mision_paso_actual]["nombre"]
	if nombre == "Capital Federal":
		return ["mision_verde"]
	elif nombre == "Aeropuertos":
		return ["mision_aeropuertos"]
	elif nombre == "Hundir 3 barcos" or nombre == "Volver a base":
		return ["mision_barcos"]
	elif nombre == "Industria":
		return ["mision_industria"]
	elif nombre == "Invasores":
		return ["invasion"]
	elif nombre == "Defensa Total":
		return ["mision_verde", "mision_aeropuertos", "mision_barcos", "mision_industria"]
	return []

func _sincronizar_modo_guerra(delta: float, forzar: bool = false) -> void:
	_tiempo_sync_guerra -= delta
	if _tiempo_sync_guerra > 0.0 and not forzar:
		return
	_tiempo_sync_guerra = 0.5
	var grupos_del_paso: Array = _grupos_relevantes_paso_actual() if modo_guerra else []
	for objetivo in objetivos_armamento:
		if not is_instance_valid(objetivo):
			continue
		var debe_estar_activo: bool = modo_guerra and not grupos_del_paso.is_empty() and _objetivo_en_algun_grupo(objetivo, grupos_del_paso)
		if debe_estar_activo:
			if objetivo.has_meta("colision_pausada"):
				for forma in objetivo.find_children("*", "CollisionShape3D", true, false):
					forma.set_deferred("disabled", false)
				objetivo.remove_meta("colision_pausada")
			if objetivo.has_meta("oculto_por_paz"):
				objetivo.visible = true
				objetivo.remove_meta("oculto_por_paz")
		else:
			if not objetivo.has_meta("colision_pausada"):
				for forma in objetivo.find_children("*", "CollisionShape3D", true, false):
					forma.set_deferred("disabled", true)
				objetivo.set_meta("colision_pausada", true)
			if objetivo.visible:
				objetivo.visible = false
				objetivo.set_meta("oculto_por_paz", true)
	var fila: Node = get_node_or_null("../HUD/FilaVelocidadInvasion")
	if fila:
		fila.visible = modo_guerra and grupos_del_paso.has("invasion")
	if not modo_guerra:
		for etiqueta in [etiqueta_objetivo_destruido, etiqueta_mision_verde, etiqueta_barcos, etiqueta_industria, etiqueta_invasion]:
			if etiqueta:
				etiqueta.visible = false

func _objetivo_en_algun_grupo(objetivo: Node, grupos: Array) -> bool:
	for g in grupos:
		if objetivo.is_in_group(g):
			return true
	return false

# BUG REAL encontrado 2026-10-17 (reportado: "la crucecita tapa el reloj"):
# la barra de arriba (BarraBotones) ya estaba casi llena -- agregar un botón
# más ahí la desbordaba, empujando la hora contra el botón ✕ (que está
# anclado fijo en la esquina, no se mueve). El botón ahora NO se agrega a
# esa barra -- se crea sin padre todavío y se cuelga adentro de la pestaña
# "Misión" (ver _crear_solapa_mision), mismo lugar que pidió Pablo para
# todo lo de guerra/misiones.
# Pedido 2026-10-17 ("quedó espectacular para aterrizar sin el dibujito del
# avión -- quiero un botón a mano, sin buscar"): la tecla V ya hacía esto
# (ver comentario viejo junto a vista_sin_avion_activa) -- sacado a función
# propia para que un botón nuevo, al lado del reloj, dispare lo mismo.
# Pedido 2026-10-17 ("el ícono real del banco, y al lado un cuadradito con
# números electrónicos del dinero"): PanelContainer anclado fijo arriba a
# la derecha (NO adentro de BarraBotones -- ver el bug de la crucecita de
# antes) con el logo (TextureRect) + un Label verde estilo instrumento,
# mismo look que ALT/RUMBO/VEL/DIST. Devuelve el Label para que quien llama
# pueda actualizarle el número después.
func _crear_caja_dinero(ruta_icono: String, offset_izq: float, offset_der: float, con_editar: bool = false) -> Label:
	var caja := PanelContainer.new()
	caja.anchors_preset = 1
	caja.anchor_left = 1.0
	caja.anchor_right = 1.0
	# Pedido 2026-10-17 ("están pisando a lo que está atrás, bajalas un
	# centímetro y medio"): un renglón más abajo que el resto (que vive en
	# offset_top 2-24) -- no tapa nada de la fila de arriba.
	caja.offset_left = offset_izq
	caja.offset_top = 32.0
	caja.offset_right = offset_der
	caja.offset_bottom = 54.0
	caja.grow_horizontal = 0
	var caja_altitud_ref: PanelContainer = get_node("../HUD/PanelInstrumentos/CajaAltitud")
	var estilo: StyleBox = caja_altitud_ref.get_theme_stylebox("panel")
	if estilo:
		caja.add_theme_stylebox_override("panel", estilo)
	get_node("../HUD").add_child(caja)

	var fila := HBoxContainer.new()
	fila.add_theme_constant_override("separation", 4)
	caja.add_child(fila)

	var icono := TextureRect.new()
	if ResourceLoader.exists(ruta_icono):
		icono.texture = load(ruta_icono)
	icono.custom_minimum_size = Vector2(18, 18)
	icono.expand_mode = TextureRect.EXPAND_FIT_WIDTH_PROPORTIONAL
	icono.stretch_mode = TextureRect.STRETCH_KEEP_ASPECT_CENTERED
	fila.add_child(icono)

	var etiqueta := Label.new()
	etiqueta.text = "$0"
	etiqueta.add_theme_font_size_override("font_size", 13)
	etiqueta.modulate = Color(0.2, 1.0, 0.3, 1.0)
	etiqueta.add_theme_color_override("font_outline_color", Color(0, 0, 0, 1))
	etiqueta.add_theme_constant_override("outline_size", 2)
	fila.add_child(etiqueta)

	# Pedido 2026-10-17 ("perdí 420 pesos -- ponele el botón más/menos para
	# corregirlo yo mismo"): solo Banco Nación lo pide (con_editar=true);
	# Mercado Pago queda como antes, sin tocar.
	if con_editar:
		var boton_menos_dinero := Button.new()
		boton_menos_dinero.text = "▼"
		boton_menos_dinero.custom_minimum_size = Vector2(16, 16)
		boton_menos_dinero.add_theme_font_size_override("font_size", 9)
		boton_menos_dinero.focus_mode = Control.FOCUS_NONE
		boton_menos_dinero.pressed.connect(func():
			PerfilJugador.modificar_plata(-25)
			_actualizar_etiqueta_dinero())
		fila.add_child(boton_menos_dinero)
		var boton_mas_dinero := Button.new()
		boton_mas_dinero.text = "▲"
		boton_mas_dinero.custom_minimum_size = Vector2(16, 16)
		boton_mas_dinero.add_theme_font_size_override("font_size", 9)
		boton_mas_dinero.focus_mode = Control.FOCUS_NONE
		boton_mas_dinero.pressed.connect(func():
			PerfilJugador.modificar_plata(25)
			_actualizar_etiqueta_dinero())
		fila.add_child(boton_mas_dinero)
	return etiqueta

func _actualizar_etiqueta_dinero() -> void:
	if etiqueta_dinero:
		etiqueta_dinero.text = "$%d" % int(PerfilJugador.perfil_actual.get("plata", 0))
	if etiqueta_dinero_mp:
		etiqueta_dinero_mp.text = "$%d" % int(PerfilJugador.perfil_actual.get("plata_mercado_pago", 0))

func _alternar_vista_sin_avion() -> void:
	vista_sin_avion_activa = not vista_sin_avion_activa
	modelo_externo.visible = not vista_sin_avion_activa
	var mostrar_primitivas = TIPOS_AVION[tipo_avion_indice]["modelo"] == "" and not vista_sin_avion_activa
	pieza_fuselaje.visible = mostrar_primitivas
	pieza_nariz.visible = mostrar_primitivas
	pieza_alas.visible = mostrar_primitivas
	pieza_cola.visible = mostrar_primitivas
	pieza_timon.visible = mostrar_primitivas
	if luz_punta_ala_izq:
		luz_punta_ala_izq.visible = mostrar_primitivas
		luz_punta_ala_der.visible = mostrar_primitivas
		luz_estroboscopica.visible = mostrar_primitivas
		luz_iluminacion_ala.visible = mostrar_primitivas
		luz_iluminacion_ala_2.visible = mostrar_primitivas
	if boton_vista_avion:
		boton_vista_avion.text = "🙈" if vista_sin_avion_activa else "👁"
	cartel_central.text = "👁️ Vista sin avión: ON" if vista_sin_avion_activa else "👁️ Vista sin avión: OFF"
	cartel_central.visible = true
	get_tree().create_timer(1.5).timeout.connect(func(): cartel_central.visible = false)

# Movimiento mientras modo_a_pie está activo -- reusa self (el nodo del
# avión) como "cuerpo" caminando, en vez de la física de vuelo normal. Mismo
# estilo de input combinado teclado+joystick que ya usa _procesar_vuelo
# (W/S o acelerar/frenar, flechas o alabeo_izquierda/derecha), pero sin
# banco ni cabeceo -- solo avanzar/retroceder y girar sobre la vertical real.
func _procesar_modo_a_pie(delta: float) -> void:
	var arriba: Vector3 = _arriba_real()

	# BUG REAL encontrado 2026-10-17 (reportado: "voy a la izquierda y el
	# cuadradito va a la derecha, y viceversa"): girar alrededor del eje
	# "arriba" con global_rotate() tiene el sentido CONTRARIO al que
	# usa alabeo_izquierda/derecha para el banco del avión -- están
	# invertidos los signos.
	# Pedido 2026-10-22 ("el giro es muy brusco, quiero que sea progresivo,
	# no de a saltos"): si alabeo_izquierda/derecha están mapeados a un EJE
	# analógico del joystick (no un botón), se usa esa magnitud (0..1, según
	# cuánto empujás el stick) para que el giro arranque suave y vaya
	# acelerando -- con teclado o un botón digital, sigue girando a la
	# velocidad fija de siempre (no hay forma de "empujar poquito" un botón).
	var giro: float = 0.0
	var magnitud_giro: float = 1.0
	var eje_izquierda: float = _joystick_eje("alabeo_izquierda")
	var eje_derecha: float = _joystick_eje("alabeo_derecha")
	if Input.is_action_pressed("ui_left") or _joystick_activo("alabeo_izquierda"):
		giro = 1.0
		if eje_izquierda != 0.0:
			magnitud_giro = eje_izquierda
	elif Input.is_action_pressed("ui_right") or _joystick_activo("alabeo_derecha"):
		giro = -1.0
		if eje_derecha != 0.0:
			magnitud_giro = eje_derecha
	if giro != 0.0:
		global_rotate(arriba, deg_to_rad(giro * magnitud_giro * VELOCIDAD_GIRO_A_PIE * delta))

	# Pedido 2026-10-17 ("quiero que el acelerador sea igual que el avión --
	# la frenada la hago yo, si no la velocidad se mantiene constante"):
	# antes se movía SOLO mientras "acelerar" estaba apretado, y frenaba
	# solo al soltarlo. Ahora hay una velocidad propia que sube con
	# "acelerar", se queda IGUAL si no tocás nada (crucero), y solo baja si
	# apretás "frenar" a propósito -- mismo esquema que ya usa el avión.
	if Input.is_physical_key_pressed(KEY_W) or _joystick_activo("acelerar"):
		velocidad_a_pie_actual = min(VELOCIDAD_A_PIE_MAXIMA, velocidad_a_pie_actual + ACELERACION_A_PIE * delta)
	elif Input.is_physical_key_pressed(KEY_S) or _joystick_activo("frenar"):
		velocidad_a_pie_actual = max(0.0, velocidad_a_pie_actual - FRENADO_A_PIE * delta)

	# Pedido 2026-10-17 ("al auto le falta marcha atrás -- ponela en el
	# mismo botón que ya tiene el helicóptero para bajar"): reusa
	# vertical_abajo (F/L o joystick) -- no hay conflicto posible, ese
	# botón solo hace algo en vuelo normal de helicóptero, nunca en modo a
	# pie. A diferencia del acelerador normal, la reversa NO mantiene
	# velocidad sola al soltar -- es lenta y a propósito, como en cualquier
	# auto de verdad.
	var reversa_activa: bool = Input.is_physical_key_pressed(KEY_F) or Input.is_physical_key_pressed(KEY_L) or _joystick_activo("vertical_abajo")
	if reversa_activa:
		velocidad_a_pie_actual = 0.0
		var atras: Vector3 = global_transform.basis.z
		atras -= atras.dot(arriba) * arriba
		if atras.length() > 0.001:
			global_position += atras.normalized() * VELOCIDAD_REVERSA_A_PIE * delta
	elif velocidad_a_pie_actual > 0.001:
		var adelante: Vector3 = -global_transform.basis.z
		adelante -= adelante.dot(arriba) * arriba
		if adelante.length() > 0.001:
			global_position += adelante.normalized() * velocidad_a_pie_actual * delta

	# BUG REAL encontrado 2026-10-17 (reportado: "nunca funcionó el freno y
	# el acelerador, iba a velocidad constante"): el acelerador SÍ estaba
	# funcionando (velocidad_a_pie_actual subía bien) -- lo que nunca se
	# actualizaba era el VELOCÍMETRO del HUD, porque lee "velocidad_actual"
	# (la variable del avión) y acá se mueve "velocidad_a_pie_actual" (otra
	# variable, a propósito, para no pisar nada del avión al volver a
	# subir). Sin el número en pantalla moviéndose, parecía que no hacía
	# nada. Ahora el instrumento muestra la velocidad real del auto
	# (convertida de m/s a km/h) mientras estás en modo a pie.
	etiqueta_velocidad.text = "VEL\n%d" % int(round(velocidad_a_pie_actual * 3.6))

	# Clavado al piso real cada cuadro (sin tren de aterrizaje ni rebotes --
	# una persona caminando no "aterriza", simplemente está siempre parada
	# sobre el terreno).
	# BUG REAL encontrado 2026-10-17 (reportado: "tiembla mucho, sube y baja,
	# se mueve toda la pantalla"): el terreno 3D real (fotogrametría de
	# Cesium) no es perfectamente parejo -- el rayo pega en un triángulo
	# apenas distinto cada cuadro, y clavar la posición DIRECTO a ese
	# resultado (como hacíamos) hacía bailar la altura (y con ella la
	# cámara, que sigue a este mismo nodo) cuadro a cuadro. Ahora solo se
	# suaviza la componente VERTICAL con un lerp rápido -- filtra ese ruido
	# chico sin notarse, pero sigue las bajadas/subidas reales del terreno
	# sin demora perceptible. La posición horizontal (ya movida arriba por
	# el caminar) no se toca para nada, sigue respondiendo al toque.
	var space_state = get_world_3d().direct_space_state
	var origen_rayo: Vector3 = global_position + arriba * 2000.0
	var destino_rayo: Vector3 = global_position - arriba * 2000.0
	var consulta := PhysicsRayQueryParameters3D.create(origen_rayo, destino_rayo)
	consulta.collision_mask = 1
	var resultado := space_state.intersect_ray(consulta)
	if resultado:
		var altura_actual: float = global_position.dot(arriba)
		var altura_objetivo: float = (resultado["position"] + arriba * ALTURA_FIGURA_A_PIE).dot(arriba)
		var nueva_altura: float = lerp(altura_actual, altura_objetivo, clamp(delta * 10.0, 0.0, 1.0))
		global_position += arriba * (nueva_altura - altura_actual)

	# Botón de ruta asignable -- flanco de subida (apretar, no mantener),
	# mismo patrón que ya usa "armamento" en _process. Guarda lat/lon REAL
	# actual (mundo.lat_avion/lon_avion ya están frescos porque mundo.gd
	# recentra el origen en base a este mismo nodo, sin código extra).
	var marcar_activo: bool = Input.is_physical_key_pressed(KEY_R) or _joystick_activo("marcar_punto_ruta")
	if marcar_activo and not _marcar_ruta_anterior:
		_ruta_trackeada.append({"lat": mundo.lat_avion, "lon": mundo.lon_avion})
		_guardar_ruta_trackeada()
		cartel_central.text = "📍 Punto de ruta #%d guardado" % _ruta_trackeada.size()
		cartel_central.visible = true
		get_tree().create_timer(1.0).timeout.connect(func(): cartel_central.visible = false)
	_marcar_ruta_anterior = marcar_activo

	var deshacer_activo: bool = _joystick_activo("deshacer_punto_ruta")
	if deshacer_activo and not _deshacer_ruta_anterior and not _ruta_trackeada.is_empty():
		_ruta_trackeada.pop_back()
		_guardar_ruta_trackeada()
		cartel_central.text = "↩️ Último punto borrado (quedan %d)" % _ruta_trackeada.size()
		cartel_central.visible = true
		get_tree().create_timer(1.0).timeout.connect(func(): cartel_central.visible = false)
	_deshacer_ruta_anterior = deshacer_activo

func _guardar_ruta_trackeada() -> void:
	var archivo := FileAccess.open(RUTA_TRACKEADA_ARCHIVO, FileAccess.WRITE)
	if archivo:
		archivo.store_string(JSON.stringify(_ruta_trackeada, "\t"))
		archivo.close()

func _cargar_ruta_trackeada() -> void:
	if not FileAccess.file_exists(RUTA_TRACKEADA_ARCHIVO):
		return
	var archivo := FileAccess.open(RUTA_TRACKEADA_ARCHIVO, FileAccess.READ)
	var datos = JSON.parse_string(archivo.get_as_text())
	archivo.close()
	if datos is Array:
		_ruta_trackeada = datos
	# Pedido 2026-10-17 ("iluminar de noche la ruta que trackeé, con
	# bombitas cada tanto, pero ignorá el lío de puntos del estacionamiento
	# y cortala donde pego la vuelta"): se prueba cada vez que arranca el
	# juego, con lo que haya en el archivo en ese momento -- así, a medida
	# que sigas trackeando en próximas sesiones, las luces se actualizan
	# solas la próxima vez que abras el juego.
	# BUG REAL encontrado 2026-10-17 (reportado con captura: "Cannot call
	# method 'get_ecef_x' on a null value", pantalla de error al apretar
	# Play): esto se llama desde _ready() del avión -- en Godot los hijos
	# terminan SU _ready() antes que el padre (mundo.gd es el padre del
	# avión), así que mundo.georeferencia todavía no estaba configurada
	# (eso lo hace el propio _ready() de mundo.gd, que corre DESPUÉS). Se
	# pospone con call_deferred para que corra recién al final del cuadro,
	# cuando el _ready() de mundo.gd ya terminó de verdad.
	if _ruta_trackeada.size() >= 2:
		call_deferred("_iluminar_ruta_trackeada_inicial")

# Pedido 2026-10-17 ("tuve que abandonar porque se agotaba la RAM"): con
# rutas cortas, uno de cada 3 puntos está bien -- pero esta ruta ya es
# Lugones + Panamericana + 50 cuadras, con muchos más puntos que la
# Costanera original. En vez de un intervalo fijo, se calcula uno que deje
# como mucho ~70 bombitas prendidas sea cual sea el largo de la ruta (ruta
# corta = intervalo chico como antes; ruta larga = intervalo más grande
# automáticamente, para no comerse la RAM).
const MAXIMO_LUCES_RUTA := 70

func _iluminar_ruta_trackeada_inicial() -> void:
	var tramo: Array = _tramo_util_de_ruta(_ruta_trackeada)
	var intervalo: int = max(3, int(ceil(float(tramo.size()) / float(MAXIMO_LUCES_RUTA))))
	var elegidos: Array = []
	var i: int = 0
	while i < tramo.size():
		elegidos.append(tramo[i])
		i += intervalo
	mundo.iluminar_ruta(elegidos)

# Distancia aproximada en metros entre dos puntos {"lat":, "lon":} --
# equirrectangular (de sobra de preciso para unos pocos km, mucho más
# simple que haversine, mismo criterio que se usa en otras cuentas rápidas
# de este archivo).
func _distancia_entre_puntos(a: Dictionary, b: Dictionary) -> float:
	var lat_medio_rad: float = deg_to_rad((float(a["lat"]) + float(b["lat"])) * 0.5)
	var dy: float = (float(b["lat"]) - float(a["lat"])) * 111320.0
	var dx: float = (float(b["lon"]) - float(a["lon"])) * 111320.0 * cos(lat_medio_rad)
	return sqrt(dx * dx + dy * dy)

# Pedido 2026-10-17 ("establecé una regularidad... el estacionamiento con
# los 10.000 puntos ignoralo... y cuando pego la vuelta ahí cortala"): el
# archivo de ruta trackeada puede traer, mezclados, puntos de pruebas
# viejas y el ida-y-vuelta completo de una caminata -- esto se queda SOLO
# con el tramo de ida real:
#  1) si el primer tramo tiene un salto enorme (>300m de un punto al
#     siguiente), es una prueba vieja mezclada en el mismo archivo -- se
#     descarta todo lo anterior a ese salto.
#  2) desde ahí, se busca el punto MÁS LEJOS en línea recta del arranque --
#     ese es el momento justo antes de "doblar" y volver: todo lo que
#     venga después (la vuelta + el estacionamiento con el enjambre de
#     puntos) se descarta entero.
# La regularidad ("uno de cada 3, de cada 5") se decide aparte, en
# _iluminar_ruta_trackeada_inicial -- acá solo se limpia el tramo.
func _tramo_util_de_ruta(puntos_crudos: Array) -> Array:
	var inicio: int = 0
	for i in range(1, puntos_crudos.size()):
		if _distancia_entre_puntos(puntos_crudos[i - 1], puntos_crudos[i]) > 300.0:
			inicio = i

	var indice_mas_lejos: int = inicio
	var distancia_maxima: float = 0.0
	for i in range(inicio, puntos_crudos.size()):
		var distancia: float = _distancia_entre_puntos(puntos_crudos[inicio], puntos_crudos[i])
		if distancia > distancia_maxima:
			distancia_maxima = distancia
			indice_mas_lejos = i

	var tramo: Array = []
	for i in range(inicio, indice_mas_lejos + 1):
		tramo.append(puntos_crudos[i])
	return tramo

# Pedido 2026-10-17: aparece al frenar en tierra (en cualquier lado, no
# atado a un hangar puntual); el mismo botón sirve para volver a subir --
# ver _actualizar_boton_bajar_subir, que decide cuándo mostrarlo.
func _bajar_del_avion() -> void:
	if modo_a_pie:
		return
	_pose_avion_antes_de_bajar = global_transform
	modo_a_pie = true
	velocidad_actual = 0.0
	banco_actual = 0.0
	cabeceo_actual = 0.0
	velocidad_a_pie_actual = 0.0
	_actualizar_etiqueta_velocidad()
	modelo_externo.visible = false
	pieza_fuselaje.visible = false
	pieza_nariz.visible = false
	pieza_alas.visible = false
	pieza_cola.visible = false
	pieza_timon.visible = false
	if luz_punta_ala_izq:
		luz_punta_ala_izq.visible = false
		luz_punta_ala_der.visible = false
		luz_estroboscopica.visible = false
		luz_iluminacion_ala.visible = false
		luz_iluminacion_ala_2.visible = false
	if not figura_a_pie:
		figura_a_pie = _crear_modelo_auto()
		if figura_a_pie:
			add_child(figura_a_pie)
	if figura_a_pie:
		figura_a_pie.visible = true

	# Pedido 2026-10-17 ("el auto me lo dejás en el último punto que
	# tenemos trackeado de esa ruta, así sigo marcando desde ahí"): si ya
	# hay una ruta en progreso, arranca directo en su ÚLTIMO punto en vez
	# de en la posición del avión -- mismo rayo al piso real de siempre.
	if not _ruta_trackeada.is_empty():
		var ultimo: Dictionary = _ruta_trackeada[_ruta_trackeada.size() - 1]
		var arriba_tp: Vector3 = _arriba_real()
		var punto: Vector3 = mundo._posicion_desde_lat_lon(float(ultimo["lat"]), float(ultimo["lon"]), 0.0)
		var space_state_tp = get_world_3d().direct_space_state
		var consulta_tp := PhysicsRayQueryParameters3D.create(punto + arriba_tp * 2000.0, punto - arriba_tp * 2000.0)
		consulta_tp.collision_mask = 1
		var resultado_tp := space_state_tp.intersect_ray(consulta_tp)
		if resultado_tp:
			global_position = resultado_tp["position"] + arriba_tp * ALTURA_FIGURA_A_PIE

	cartel_central.text = "🚗 Bajaste del avión -- acelerar mantiene la velocidad, frenar la baja. R marca un punto de ruta"
	cartel_central.visible = true
	get_tree().create_timer(2.5).timeout.connect(func(): cartel_central.visible = false)

# Autoescala + recentra el pivote del modelo del auto (los modelos bajados
# de internet vienen con escalas y pivotes al azar -- mismo truco ya
# probado con el A380/los barcos: medir el tamaño real ANTES de escalar, y
# calcular la escala necesaria para que el lado más largo mida
# LARGO_AUTO_REAL metros de verdad).
func _crear_modelo_auto() -> Node3D:
	var escena: PackedScene = load(MODELO_AUTO)
	if not escena:
		return null
	var instancia = escena.instantiate()
	var caja_nativa: AABB = _calcular_aabb_local(instancia)
	var dimension_mas_larga: float = max(caja_nativa.size.x, max(caja_nativa.size.y, caja_nativa.size.z))
	var escala_auto: float = (LARGO_AUTO_REAL / dimension_mas_larga) if dimension_mas_larga > 0.001 else 1.0
	instancia.scale = Vector3.ONE * escala_auto
	# BUG REAL encontrado 2026-10-17 (reportado: "la trompa apunta para
	# atrás, tiene que apuntar para adelante"): el modelo venía mirando al
	# revés de "adelante" (-Z local, la misma convención que usa el avión)
	# -- 180° lo da vuelta.
	instancia.rotation_degrees = Vector3(0, 180, 0)
	var centro_nativo: Vector3 = caja_nativa.position + caja_nativa.size * 0.5
	instancia.position = -(instancia.basis * centro_nativo)

	var contenedor := Node3D.new()
	contenedor.name = "ModeloAuto"
	contenedor.add_child(instancia)
	contenedor.position = Vector3(0, ALTURA_FIGURA_A_PIE, 0)
	return contenedor

func _subir_al_avion() -> void:
	if not modo_a_pie:
		return
	modo_a_pie = false
	global_transform = _pose_avion_antes_de_bajar
	velocidad_actual = 0.0
	velocidad_a_pie_actual = 0.0
	# BUG REAL encontrado 2026-10-22 (reportado: "subo al avión y ya no
	# acelera, ni con un vuelo nuevo"): Estado.DETENIDO es del sistema VIEJO
	# de aterrizaje de precisión -- su _procesar_detenido() no hace NADA más
	# que esperar que aprietes "Despegar" (un botón que ni siquiera se
	# muestra en el flujo de hoy), así que W/S nunca se procesaban. El modo
	# tierra de verdad (el que usa esta función) necesita Estado.VOLANDO,
	# igual que _confirmar_viaje()/_teletransportar_para_mision() al dejarte
	# parado en tierra.
	estado = Estado.VOLANDO
	_reiniciar_estado_tierra()
	en_tierra = true
	if figura_a_pie:
		figura_a_pie.visible = false
	if not vista_sin_avion_activa:
		modelo_externo.visible = true
		var mostrar_primitivas = TIPOS_AVION[tipo_avion_indice]["modelo"] == ""
		pieza_fuselaje.visible = mostrar_primitivas
		pieza_nariz.visible = mostrar_primitivas
		pieza_alas.visible = mostrar_primitivas
		pieza_cola.visible = mostrar_primitivas
		pieza_timon.visible = mostrar_primitivas
		if luz_punta_ala_izq:
			luz_punta_ala_izq.visible = mostrar_primitivas
			luz_punta_ala_der.visible = mostrar_primitivas
			luz_estroboscopica.visible = mostrar_primitivas
			luz_iluminacion_ala.visible = mostrar_primitivas
			luz_iluminacion_ala_2.visible = mostrar_primitivas
	cartel_central.text = "🛩️ De vuelta en el avión"
	cartel_central.visible = true
	get_tree().create_timer(1.5).timeout.connect(func(): cartel_central.visible = false)

# Decide cuándo mostrar/ocultar y qué texto poner en el botón fijo de
# bajar/subir -- se llama cada cuadro desde _actualizar_hud (throttle de
# siempre, no hace falta más rápido para un botón).
func _actualizar_boton_bajar_subir() -> void:
	if not boton_bajar_subir:
		return
	if modo_a_pie:
		boton_bajar_subir.visible = true
		boton_bajar_subir.text = "🚗 Subir al avión"
	else:
		# BUG REAL encontrado 2026-10-17 (reportado: "frené y no me apareció
		# nada"): Estado.DETENIDO solo existe en el sistema VIEJO de
		# aterrizaje de precisión (LLEGADA -> ESPACIO -> FRENANDO ->
		# DETENIDO) -- el modo tierra de verdad (el que usan los Destinos
		# Nacionales, las misiones civiles y la pestaña Torre) nunca pasa
		# por ahí: frenás hasta 0 y el estado se queda en VOLANDO todo el
		# tiempo, aunque estés parado en tierra. El botón tiene que
		# aparecer con solo "parado en el piso", sin pedir ese estado viejo
		# que en la práctica casi nunca se alcanza.
		boton_bajar_subir.visible = en_tierra and abs(velocidad_actual) < 1.0
		boton_bajar_subir.text = "🚶 Bajar del avión"

func _crear_boton_modo_guerra() -> void:
	boton_modo_guerra = Button.new()
	boton_modo_guerra.text = "Modo guerra"
	boton_modo_guerra.toggle_mode = true
	boton_modo_guerra.custom_minimum_size = Vector2(150, 22)
	boton_modo_guerra.add_theme_font_size_override("font_size", 11)
	boton_modo_guerra.focus_mode = Control.FOCUS_NONE
	boton_modo_guerra.tooltip_text = "Activa los blancos (cuadrados rojos y verdes, barcos, invasores) y sus contadores. Apagado: vuelo y aterrizaje sin nada en pantalla. Las misiones de guerra lo prenden solas al elegirlas."
	boton_modo_guerra.toggled.connect(func(activo: bool):
		modo_guerra = activo
		_sincronizar_modo_guerra(0.0, true))

# Pedido 2026-10-17 ("sos un desastre haciendo los menús flotantes -- trasladá
# todo eso a una pestaña abajo, como hiciste con Aterrizaje, que quedó
# bien"): TODAS las misiones (guerra y civiles) en una sola pestaña nueva,
# mismo patrón EXACTO que _crear_solapa_aterrizaje (scroll horizontal,
# columnas fijas, cada una con su explicación debajo) -- nada de paneles
# flotantes centrados ni de listas escondidas adentro de otro panel.
# -- no usa "Modo guerra" para nada (nunca hay que disparar).
func _crear_solapa_mision() -> void:
	var hud: Node = get_node("../HUD")
	boton_solapa_7 = Button.new()
	boton_solapa_7.custom_minimum_size = Vector2(70, 20)
	boton_solapa_7.add_theme_font_size_override("font_size", 10)
	boton_solapa_7.toggle_mode = true
	boton_solapa_7.focus_mode = Control.FOCUS_NONE
	boton_solapa_7.text = "Misión"
	boton_solapa_7.pressed.connect(func(): _mostrar_solapa_inferior(7))
	get_node("../HUD/SolapasInferior").add_child(boton_solapa_7)

	panel_mision_tab = Panel.new()
	panel_mision_tab.anchor_left = panel_config_tab.anchor_left
	panel_mision_tab.anchor_top = panel_config_tab.anchor_top
	panel_mision_tab.anchor_right = panel_config_tab.anchor_right
	panel_mision_tab.anchor_bottom = panel_config_tab.anchor_bottom
	panel_mision_tab.offset_left = panel_config_tab.offset_left
	panel_mision_tab.offset_top = panel_config_tab.offset_top
	panel_mision_tab.offset_right = panel_config_tab.offset_right
	panel_mision_tab.offset_bottom = panel_config_tab.offset_bottom
	panel_mision_tab.grow_horizontal = panel_config_tab.grow_horizontal
	panel_mision_tab.grow_vertical = panel_config_tab.grow_vertical
	var estilo: StyleBox = panel_config_tab.get_theme_stylebox("panel")
	if estilo:
		panel_mision_tab.add_theme_stylebox_override("panel", estilo)
	panel_mision_tab.visible = false
	hud.add_child(panel_mision_tab)

	var scroll := ScrollContainer.new()
	scroll.anchor_right = 1.0
	scroll.anchor_bottom = 1.0
	scroll.offset_left = 6.0
	scroll.offset_top = 4.0
	scroll.offset_right = -6.0
	scroll.offset_bottom = -4.0
	scroll.vertical_scroll_mode = ScrollContainer.SCROLL_MODE_DISABLED
	panel_mision_tab.add_child(scroll)
	var fila := HBoxContainer.new()
	fila.add_theme_constant_override("separation", 16)
	scroll.add_child(fila)

	# Columna del interruptor general "Modo guerra" (ver comentario en
	# _crear_boton_modo_guerra -- se sacó de la barra de arriba, que ya
	# estaba desbordada).
	var col_modo_guerra := VBoxContainer.new()
	col_modo_guerra.custom_minimum_size = Vector2(170, 0)
	var titulo_modo_guerra := Label.new()
	titulo_modo_guerra.text = "Modo guerra"
	titulo_modo_guerra.add_theme_font_size_override("font_size", 11)
	col_modo_guerra.add_child(titulo_modo_guerra)
	col_modo_guerra.add_child(boton_modo_guerra)
	var ayuda_modo_guerra := Label.new()
	ayuda_modo_guerra.text = "Interruptor general: activa los blancos (cuadrados, barcos, invasores) y sus contadores. Las misiones de guerra de abajo lo prenden solas."
	ayuda_modo_guerra.autowrap_mode = TextServer.AUTOWRAP_WORD_SMART
	ayuda_modo_guerra.add_theme_font_size_override("font_size", 9)
	ayuda_modo_guerra.add_theme_color_override("font_color", Color(0.75, 0.8, 0.85))
	col_modo_guerra.add_child(ayuda_modo_guerra)
	fila.add_child(col_modo_guerra)

	# Misiones de GUERRA -- al elegir una, prende "Modo guerra" solo (antes
	# había que prenderlo aparte); las comerciales no lo tocan para nada.
	var misiones_guerra := [
		{"titulo": "Modo Práctica", "desc": "Entrenamiento: primero Capital Federal, después los aeropuertos. Un solo tiro cada uno.", "aviso": "Bienvenido, piloto. Antes de las misiones de verdad, vamos a entrenar la puntería.", "secuencia": SECUENCIA_MODO_PRACTICA, "final": "¡MODO PRÁCTICA COMPLETO!"},
		{"titulo": "Nivel 1 — Barcos", "desc": "Hundí 3 barcos en el río y volvé a Aeroparque.", "aviso": "Base a piloto: detectamos barcos enemigos en el río. Hundí 3 y volvé.", "secuencia": SECUENCIA_NIVEL_1, "final": "¡NIVEL 1 COMPLETO!"},
		{"titulo": "Nivel 2 — Industria", "desc": "Fábricas enemigas en La Plata. Destruilas todas.", "aviso": "Base a piloto: hay fábricas enemigas trabajando en La Plata. Destruilas.", "secuencia": SECUENCIA_NIVEL_2, "final": "¡NIVEL 2 COMPLETO!"},
		{"titulo": "Nivel 3 — Invasión", "desc": "Tropas desembarcando por la costa. Destruí 10.", "aviso": "Base a piloto: tropas enemigas desembarcando por la costa. No dejes que avancen.", "secuencia": SECUENCIA_NIVEL_3, "final": "¡NIVEL 3 COMPLETO!"},
		{"titulo": "Nivel 4 — Defensa Total", "desc": "El final: barcos, aeropuertos, Capital e industria, todo junto.", "aviso": "Base a piloto: ataque simultáneo en todos los frentes. Es ahora o nunca.", "secuencia": SECUENCIA_NIVEL_4, "final": "¡FELICITACIONES, COMPLETASTE MODO GUERRA!"},
	]
	for datos_mision in misiones_guerra:
		var columna := VBoxContainer.new()
		columna.custom_minimum_size = Vector2(205, 0)
		var titulo := Label.new()
		titulo.text = "⚔️ " + datos_mision["titulo"]
		titulo.add_theme_font_size_override("font_size", 11)
		columna.add_child(titulo)
		var boton := Button.new()
		boton.text = "Volar"
		boton.custom_minimum_size = Vector2(150, 22)
		boton.focus_mode = Control.FOCUS_NONE
		boton.pressed.connect(func():
			boton_modo_guerra.button_pressed = true
			_mostrar_cartel_aterrizaje(datos_mision["aviso"], Color(0.5, 0.8, 1.0))
			_iniciar_secuencia_mision(datos_mision["secuencia"], datos_mision["final"]))
		columna.add_child(boton)
		var ayuda := Label.new()
		ayuda.text = datos_mision["desc"]
		ayuda.autowrap_mode = TextServer.AUTOWRAP_WORD_SMART
		ayuda.add_theme_font_size_override("font_size", 9)
		ayuda.add_theme_color_override("font_color", Color(0.75, 0.8, 0.85))
		columna.add_child(ayuda)
		fila.add_child(columna)

	# Misiones COMERCIALES -- civiles, de un punto A a un punto B, nunca
	# teletransportan: arrancan desde donde esté el avión en ese momento.
	for mision in MISIONES_COMERCIALES:
		var columna := VBoxContainer.new()
		columna.custom_minimum_size = Vector2(205, 0)
		var titulo := Label.new()
		titulo.text = "✈️ " + mision["nombre"]
		titulo.add_theme_font_size_override("font_size", 11)
		columna.add_child(titulo)
		var etiqueta_precio_mision := Label.new()
		etiqueta_precio_mision.text = "💵 $%d base" % int(mision.get("precio", 0))
		etiqueta_precio_mision.add_theme_font_size_override("font_size", 10)
		etiqueta_precio_mision.add_theme_color_override("font_color", Color(1.0, 0.85, 0.3))
		columna.add_child(etiqueta_precio_mision)
		var boton := Button.new()
		boton.text = "Volar"
		boton.custom_minimum_size = Vector2(150, 22)
		boton.focus_mode = Control.FOCUS_NONE
		boton.pressed.connect(_iniciar_mision_comercial.bind(mision))
		columna.add_child(boton)
		var ayuda := Label.new()
		ayuda.text = mision["descripcion"]
		ayuda.autowrap_mode = TextServer.AUTOWRAP_WORD_SMART
		ayuda.add_theme_font_size_override("font_size", 9)
		ayuda.add_theme_color_override("font_color", Color(0.75, 0.8, 0.85))
		columna.add_child(ayuda)
		fila.add_child(columna)

# Pedido 2026-10-17 ("separá los destinos de las misiones -- pestaña
# aparte"): mismo patrón que _crear_solapa_mision, pestaña 8, solo con
# los Destinos Nacionales (precio editable ▲▼, igual que antes).
func _crear_solapa_destinos() -> void:
	var hud: Node = get_node("../HUD")
	boton_solapa_8 = Button.new()
	boton_solapa_8.custom_minimum_size = Vector2(70, 20)
	boton_solapa_8.add_theme_font_size_override("font_size", 10)
	boton_solapa_8.toggle_mode = true
	boton_solapa_8.focus_mode = Control.FOCUS_NONE
	boton_solapa_8.text = "Destinos"
	boton_solapa_8.pressed.connect(func(): _mostrar_solapa_inferior(8))
	get_node("../HUD/SolapasInferior").add_child(boton_solapa_8)

	panel_destinos_tab = Panel.new()
	panel_destinos_tab.anchor_left = panel_config_tab.anchor_left
	panel_destinos_tab.anchor_top = panel_config_tab.anchor_top
	panel_destinos_tab.anchor_right = panel_config_tab.anchor_right
	panel_destinos_tab.anchor_bottom = panel_config_tab.anchor_bottom
	panel_destinos_tab.offset_left = panel_config_tab.offset_left
	panel_destinos_tab.offset_top = panel_config_tab.offset_top
	panel_destinos_tab.offset_right = panel_config_tab.offset_right
	panel_destinos_tab.offset_bottom = panel_config_tab.offset_bottom
	panel_destinos_tab.grow_horizontal = panel_config_tab.grow_horizontal
	panel_destinos_tab.grow_vertical = panel_config_tab.grow_vertical
	var estilo: StyleBox = panel_config_tab.get_theme_stylebox("panel")
	if estilo:
		panel_destinos_tab.add_theme_stylebox_override("panel", estilo)
	panel_destinos_tab.visible = false
	hud.add_child(panel_destinos_tab)

	var scroll := ScrollContainer.new()
	scroll.anchor_right = 1.0
	scroll.anchor_bottom = 1.0
	scroll.offset_left = 6.0
	scroll.offset_top = 4.0
	scroll.offset_right = -6.0
	scroll.offset_bottom = -4.0
	scroll.vertical_scroll_mode = ScrollContainer.SCROLL_MODE_DISABLED
	panel_destinos_tab.add_child(scroll)
	var fila := HBoxContainer.new()
	fila.add_theme_constant_override("separation", 16)
	scroll.add_child(fila)

	for destino in DESTINOS_NACIONALES:
		var columna_destino := VBoxContainer.new()
		columna_destino.custom_minimum_size = Vector2(205, 0)
		var titulo_destino := Label.new()
		titulo_destino.text = "🌎 " + destino["nombre"]
		titulo_destino.add_theme_font_size_override("font_size", 11)
		columna_destino.add_child(titulo_destino)
		var fila_precio := HBoxContainer.new()
		var etiqueta_precio_destino := Label.new()
		etiqueta_precio_destino.text = "$%d" % int(destino["precio"])
		etiqueta_precio_destino.custom_minimum_size = Vector2(60, 0)
		etiqueta_precio_destino.add_theme_color_override("font_color", Color(1.0, 0.85, 0.3))
		fila_precio.add_child(etiqueta_precio_destino)
		var boton_menos_destino := Button.new()
		boton_menos_destino.text = "▼"
		boton_menos_destino.focus_mode = Control.FOCUS_NONE
		boton_menos_destino.pressed.connect(func():
			destino["precio"] = max(0, int(destino["precio"]) - PASO_PRECIO_DESTINO)
			etiqueta_precio_destino.text = "$%d" % int(destino["precio"]))
		fila_precio.add_child(boton_menos_destino)
		var boton_mas_destino := Button.new()
		boton_mas_destino.text = "▲"
		boton_mas_destino.focus_mode = Control.FOCUS_NONE
		boton_mas_destino.pressed.connect(func():
			destino["precio"] = int(destino["precio"]) + PASO_PRECIO_DESTINO
			etiqueta_precio_destino.text = "$%d" % int(destino["precio"]))
		fila_precio.add_child(boton_mas_destino)
		columna_destino.add_child(fila_precio)
		var boton_volar_destino := Button.new()
		boton_volar_destino.text = "Volar"
		boton_volar_destino.custom_minimum_size = Vector2(150, 22)
		boton_volar_destino.focus_mode = Control.FOCUS_NONE
		boton_volar_destino.pressed.connect(_iniciar_destino_nacional.bind(destino))
		columna_destino.add_child(boton_volar_destino)
		fila.add_child(columna_destino)

func _iniciar_mision_comercial(mision: Dictionary) -> void:
	if mision.has("origen"):
		_teletransportar_para_mision(float(mision["origen"]["lat"]), float(mision["origen"]["lon"]))
	_mostrar_cartel_aterrizaje("MISIÓN: %s\n%s" % [mision["nombre"], mision["descripcion"]], Color(0.6, 0.85, 1.0))
	_iniciar_secuencia_mision(mision["pasos"], "¡Misión cumplida: %s!" % mision["nombre"], int(mision.get("precio", 0)))

# Pedido 2026-10-17 ("destinos nacionales... todos los que tenemos
# trackeados, ordenados de más cerca a más lejos, con precio editable"):
# vuelo simple Aeroparque -> destino, en el aire (mismo arranque que el
# City Tour), paga al llegar.
func _iniciar_destino_nacional(destino: Dictionary) -> void:
	# Usa la posición del aeropuerto REAL (mundo.gd) si lo encuentra por
	# nombre exacto -- ver comentario largo junto a DESTINOS_NACIONALES.
	# Si por lo que sea no se encuentra (nombre no coincide), usa el lat/lon
	# escrito a mano como respaldo, para no romper el viaje.
	var lat_destino: float = float(destino["lat"])
	var lon_destino: float = float(destino["lon"])
	var aeropuerto_real_destino := _buscar_destino_por_nombre(destino.get("aeropuerto_real", ""))
	if aeropuerto_real_destino:
		lat_destino = float(aeropuerto_real_destino.get_meta("lat"))
		lon_destino = float(aeropuerto_real_destino.get_meta("lon"))

	var aeropuerto := _buscar_destino_por_nombre("Aeroparque")
	if aeropuerto:
		global_position = aeropuerto.global_position + _arriba_real() * 600.0
		var punto_destino: Vector3 = mundo._posicion_desde_lat_lon(lat_destino, lon_destino, 0.0)
		look_at(punto_destino, _arriba_real())
		banco_actual = 0.0
		var ajustes_actuales: Dictionary = _ajustes_avion_actual()
		velocidad_actual = lerp(float(ajustes_actuales["vel_minima"]), float(ajustes_actuales["vel_maxima"]), 0.4)
		_actualizar_etiqueta_velocidad()
		_reiniciar_estado_tierra()
		estado = Estado.VOLANDO
	_mostrar_cartel_aterrizaje("DESTINO NACIONAL: %s\nSalís de Aeroparque. Pago: $%d" % [destino["nombre"], int(round(float(destino["precio"]) * _multiplicador_precio_avion()))], Color(0.6, 0.85, 1.0))
	# Pedido 2026-10-17 ("ni bien aterrizo ya me da la plata -- si dice
	# aterrizar, que pague solo al aterrizar"): 1500m era demasiado laxo
	# (pagaba con solo pasar cerca). en_tierra (ver _paso_mision_actual_
	# completo) ya exige un aterrizaje de verdad -- el radio se subió de
	# 400 a 600m (reportado: "aterricé y no me pagó") porque varios
	# aeropuertos del interior solo tienen una coordenada aproximada, no la
	# pista real medida -- 600m es el mismo generoso que ya usa "Volver a
	# base" (RADIO_VOLVER_BASE) en otras misiones.
	var paso := [{"nombre": "Volar a " + destino["nombre"], "texto": "Destino: " + destino["nombre"] + ".", "tipo": "llegar_punto", "lat": lat_destino, "lon": lon_destino, "radio": RADIO_VOLVER_BASE}]
	_iniciar_secuencia_mision(paso, "¡Llegaste a %s!" % destino["nombre"], int(destino["precio"]))

# Pedido 2026-10-17, segunda vuelta ("al apretar Volar te tiene que
# teletransportar al punto de partida, no respetar el destino estés donde
# estés"): mismo patrón que ya usa _iniciar_city_tour para arrancar en un
# lugar exacto -- acá siempre en tierra (parado, modo tierra prendido si el
# avión lo tiene activado), porque casi todos los orígenes son hangares.
func _teletransportar_para_mision(lat: float, lon: float) -> void:
	var arriba: Vector3 = _arriba_real()
	var punto_aproximado: Vector3 = mundo._posicion_desde_lat_lon(lat, lon, 0.0)
	var space_state := get_world_3d().direct_space_state
	var consulta := PhysicsRayQueryParameters3D.create(punto_aproximado + arriba * 2000.0, punto_aproximado - arriba * 2000.0)
	consulta.collision_mask = 1
	var resultado := space_state.intersect_ray(consulta)
	var punto_piso: Vector3 = resultado["position"] if resultado.size() > 0 else punto_aproximado
	var tren: float = _altura_tren_efectiva()
	global_position = punto_piso + arriba * tren
	banco_actual = 0.0
	cabeceo_actual = 0.0
	velocidad_actual = 0.0
	_reiniciar_estado_tierra()
	en_tierra = true
	estado = Estado.VOLANDO
	_actualizar_etiqueta_velocidad()

func _crear_cartel_aterrizaje() -> void:
	cartel_aterrizaje = Label.new()
	cartel_aterrizaje.visible = false
	cartel_aterrizaje.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
	cartel_aterrizaje.anchor_left = 0.15
	cartel_aterrizaje.anchor_right = 0.85
	cartel_aterrizaje.anchor_top = 0.22
	cartel_aterrizaje.anchor_bottom = 0.22
	cartel_aterrizaje.grow_horizontal = Control.GROW_DIRECTION_BOTH
	# BUG REAL encontrado 2026-10-17 (reportado: "A por Todo queda encimado
	# arriba de las letras de abajo"): con GROW_BOTH, un texto de 2 líneas
	# (como los carteles de misión nuevos, que tienen \n) crece tanto para
	# arriba como para abajo de su punto de anclaje -- con una sola línea
	# nunca se notaba. Solo hacia abajo, para que nunca invada el
	# recordatorio de objetivo que está arriba.
	cartel_aterrizaje.grow_vertical = Control.GROW_DIRECTION_END
	cartel_aterrizaje.add_theme_font_size_override("font_size", 34)
	cartel_aterrizaje.add_theme_color_override("font_outline_color", Color(0, 0, 0))
	cartel_aterrizaje.add_theme_constant_override("outline_size", 8)
	cartel_aterrizaje.mouse_filter = Control.MOUSE_FILTER_IGNORE
	get_node("../HUD").add_child(cartel_aterrizaje)

func _mostrar_cartel_aterrizaje(texto: String, color: Color) -> void:
	if not cartel_aterrizaje or cortina_de_carga.visible:
		return
	cartel_aterrizaje.text = texto
	cartel_aterrizaje.add_theme_color_override("font_color", color)
	cartel_aterrizaje.visible = true
	cartel_aterrizaje_tiempo = 3.5

func _actualizar_cartel_aterrizaje(delta: float) -> void:
	if cartel_aterrizaje and cartel_aterrizaje.visible:
		cartel_aterrizaje_tiempo -= delta
		if cartel_aterrizaje_tiempo <= 0.0:
			cartel_aterrizaje.visible = false

# Una fila por cada valor editable: título, botones (grueso/fino) y un texto
# corto de para qué sirve. "paso" es el salto fino; el grueso es 10 veces eso.
const FILAS_ATERRIZAJE = [
	{"clave": "altura_tren", "titulo": "Altura del tren", "paso": 0.05, "min": 0.0, "max": 60.0,
		"ayuda": "Distancia del centro del avión a las ruedas. Si las ruedas se hunden en la pista, subila; si el avión flota, bajala. Viene medida sola del modelo (AUTO)."},
	{"clave": "vel_despegue", "titulo": "Vel. de despegue", "paso": 5.0, "min": 0.0, "max": 3000.0,
		"ayuda": "Por debajo de esta velocidad el avión queda pegado a la pista aunque tires para atrás. Al llegar a ella y tirar, despega."},
	{"clave": "giro_suelo", "titulo": "Giro en tierra (°/s)", "paso": 2.0, "min": 1.0, "max": 120.0,
		"ayuda": "Tope de qué tan rápido gira en pista con izquierda/derecha, aunque vaya rápido. El giro real también depende de la velocidad y del radio de giro."},
	{"clave": "radio_giro", "titulo": "Radio de giro", "paso": 5.0, "min": 5.0, "max": 1000.0,
		"ayuda": "Como en un auto: a menor radio, más cerrada la curva a la misma velocidad. Parado no gira. Subilo para que el avión necesite más espacio para doblar."},
	{"clave": "acel_suelo", "titulo": "Aceleración en tierra", "paso": 1.0, "min": 0.5, "max": 200.0,
		"ayuda": "Cuánta velocidad suma por segundo al apretar W en pista. Más bajo = más fino para rodar despacio hacia el gate; más alto = acelera de golpe."},
	{"clave": "freno_suelo", "titulo": "Freno en tierra", "paso": 5.0, "min": 1.0, "max": 1000.0,
		"ayuda": "Cuánto frena por segundo al apretar S (o el freno) estando en tierra. Más alto = frena más fuerte."},
	{"clave": "rodadura", "titulo": "Rodadura", "paso": 1.0, "min": 0.0, "max": 200.0,
		"ayuda": "Resistencia natural al rodar sin tocar nada. 0 = sigue rodando sin frenar solo (hay que usar el freno). Subilo para que se detenga de a poco."},
	{"clave": "aterr_suave", "titulo": "Toque suave hasta", "paso": 0.5, "min": 0.25, "max": 60.0,
		"ayuda": "Velocidad de descenso (m/s) al tocar. Por debajo de este valor el cartel dice Aterrizaje suave."},
	{"clave": "aterr_brusco", "titulo": "Toque brusco desde", "paso": 0.5, "min": 0.5, "max": 100.0,
		"ayuda": "Por encima de este descenso (m/s) el cartel avisa que venís muy fuerte y rebota. Entre este valor y el de suave es Aterrizaje normal."},
	{"clave": "rebote_fuerza", "titulo": "Fuerza del rebote", "paso": 0.1, "min": 0.0, "max": 4.0,
		"ayuda": "Qué tan alto rebota en un toque brusco. 0 = no rebota (queda solo el cartel)."},
	{"clave": "rebotes", "titulo": "Cantidad de rebotes", "paso": 1.0, "min": 0.0, "max": 6.0,
		"ayuda": "Cuántos rebotes da en un toque brusco antes de apoyarse del todo (cada uno más chico que el anterior)."},
]

func _texto_valor_aterrizaje(clave: String) -> String:
	var cfg: Dictionary = _aterrizaje_actual()
	match clave:
		"altura_tren":
			if float(cfg[clave]) < 0.0:
				return "AUTO %.2f m" % altura_tren_auto
			return "%.2f m" % float(cfg[clave])
		"vel_despegue":
			if float(cfg[clave]) <= 0.0:
				return "AUTO %.0f" % _vel_despegue_efectiva()
			return "%.0f" % float(cfg[clave])
		"giro_suelo":
			return "%.0f°/s" % float(cfg[clave])
		"radio_giro", "freno_suelo", "rodadura":
			return "%.0f" % float(cfg[clave])
		"acel_suelo":
			return "%.1f" % float(cfg[clave])
		"aterr_suave", "aterr_brusco":
			return "%.2f m/s" % float(cfg[clave])
		"rebote_fuerza":
			return "%.1fx" % float(cfg[clave])
		"rebotes":
			return "%d" % int(cfg[clave])
	return str(cfg[clave])

func _refrescar_panel_aterrizaje() -> void:
	if not panel_aterrizaje_tab:
		return
	for clave in valores_aterrizaje_labels.keys():
		valores_aterrizaje_labels[clave].text = _texto_valor_aterrizaje(clave)
	if boton_modo_tierra:
		var activo: bool = bool(_aterrizaje_actual()["modo_tierra"])
		boton_modo_tierra.set_pressed_no_signal(activo)
		boton_modo_tierra.text = "Modo tierra: SÍ" if activo else "Modo tierra: NO"

func _ajustar_aterrizaje(clave: String, delta_valor: float, minimo: float, maximo: float) -> void:
	var cfg: Dictionary = _aterrizaje_actual()
	var actual: float = float(cfg[clave])
	# Si estaba en AUTO, se parte del valor automático actual.
	if clave == "altura_tren" and actual < 0.0:
		actual = altura_tren_auto
	if clave == "vel_despegue" and actual <= 0.0:
		actual = _vel_despegue_efectiva()
	var nuevo: float = clamp(actual + delta_valor, minimo, maximo)
	if clave == "rebotes":
		cfg[clave] = int(round(nuevo))
	else:
		cfg[clave] = nuevo
	_refrescar_panel_aterrizaje()
	_guardar_aterrizaje_aviones()

func _poner_aterrizaje_auto(clave: String) -> void:
	var cfg: Dictionary = _aterrizaje_actual()
	cfg[clave] = -1.0 if clave == "altura_tren" else 0.0
	_refrescar_panel_aterrizaje()
	_guardar_aterrizaje_aviones()

func _boton_aterrizaje(texto: String, accion: Callable, ayuda: String = "") -> Button:
	var b := Button.new()
	b.tooltip_text = ayuda
	b.text = texto
	b.custom_minimum_size = Vector2(28, 22)
	b.add_theme_font_size_override("font_size", 11)
	b.focus_mode = Control.FOCUS_NONE
	b.pressed.connect(accion)
	return b

# Pestaña nueva en el panel de abajo (pedido 2026-10-16): todos los valores
# del aterrizaje y el modo tierra, editables por avión con botones, para
# afinar a ojo avión por avión sin tocar código.
func _crear_solapa_aterrizaje() -> void:
	var hud: Node = get_node("../HUD")
	boton_solapa_6 = Button.new()
	boton_solapa_6.custom_minimum_size = Vector2(70, 20)
	boton_solapa_6.add_theme_font_size_override("font_size", 10)
	boton_solapa_6.toggle_mode = true
	boton_solapa_6.focus_mode = Control.FOCUS_NONE
	boton_solapa_6.text = "Aterrizaje"
	boton_solapa_6.pressed.connect(func(): _mostrar_solapa_inferior(6))
	get_node("../HUD/SolapasInferior").add_child(boton_solapa_6)

	panel_aterrizaje_tab = Panel.new()
	panel_aterrizaje_tab.anchor_left = panel_config_tab.anchor_left
	panel_aterrizaje_tab.anchor_top = panel_config_tab.anchor_top
	panel_aterrizaje_tab.anchor_right = panel_config_tab.anchor_right
	panel_aterrizaje_tab.anchor_bottom = panel_config_tab.anchor_bottom
	panel_aterrizaje_tab.offset_left = panel_config_tab.offset_left
	panel_aterrizaje_tab.offset_top = panel_config_tab.offset_top
	panel_aterrizaje_tab.offset_right = panel_config_tab.offset_right
	panel_aterrizaje_tab.offset_bottom = panel_config_tab.offset_bottom
	panel_aterrizaje_tab.grow_horizontal = panel_config_tab.grow_horizontal
	panel_aterrizaje_tab.grow_vertical = panel_config_tab.grow_vertical
	var estilo: StyleBox = panel_config_tab.get_theme_stylebox("panel")
	if estilo:
		panel_aterrizaje_tab.add_theme_stylebox_override("panel", estilo)
	panel_aterrizaje_tab.visible = false
	hud.add_child(panel_aterrizaje_tab)

	var scroll := ScrollContainer.new()
	scroll.anchor_right = 1.0
	scroll.anchor_bottom = 1.0
	scroll.offset_left = 6.0
	scroll.offset_top = 4.0
	scroll.offset_right = -6.0
	scroll.offset_bottom = -4.0
	scroll.vertical_scroll_mode = ScrollContainer.SCROLL_MODE_DISABLED
	panel_aterrizaje_tab.add_child(scroll)
	var fila := HBoxContainer.new()
	fila.add_theme_constant_override("separation", 16)
	scroll.add_child(fila)

	# Columna del interruptor general
	var col_modo := VBoxContainer.new()
	col_modo.custom_minimum_size = Vector2(170, 0)
	var titulo_modo := Label.new()
	titulo_modo.text = "Modo tierra"
	titulo_modo.add_theme_font_size_override("font_size", 11)
	col_modo.add_child(titulo_modo)
	var texto_modo: String = "SÍ: el avión se apoya en las ruedas, queda nivelado en pista y gira como un auto. NO: todo como antes."
	titulo_modo.tooltip_text = texto_modo
	titulo_modo.mouse_filter = Control.MOUSE_FILTER_STOP
	boton_modo_tierra = Button.new()
	boton_modo_tierra.toggle_mode = true
	boton_modo_tierra.custom_minimum_size = Vector2(150, 22)
	boton_modo_tierra.add_theme_font_size_override("font_size", 11)
	boton_modo_tierra.focus_mode = Control.FOCUS_NONE
	boton_modo_tierra.pressed.connect(func():
		var cfg: Dictionary = _aterrizaje_actual()
		cfg["modo_tierra"] = not bool(cfg["modo_tierra"])
		if not bool(cfg["modo_tierra"]):
			_reiniciar_estado_tierra()
		_refrescar_panel_aterrizaje()
		_guardar_aterrizaje_aviones())
	boton_modo_tierra.tooltip_text = texto_modo
	col_modo.add_child(boton_modo_tierra)
	var ayuda_modo := Label.new()
	ayuda_modo.text = "SÍ: el avión se apoya en las ruedas, queda nivelado en pista y gira como un auto. NO: todo como antes. Cada avión tiene sus propios valores."
	ayuda_modo.autowrap_mode = TextServer.AUTOWRAP_WORD_SMART
	ayuda_modo.add_theme_font_size_override("font_size", 9)
	ayuda_modo.add_theme_color_override("font_color", Color(0.75, 0.8, 0.85))
	col_modo.add_child(ayuda_modo)
	fila.add_child(col_modo)

	for spec in FILAS_ATERRIZAJE:
		var clave: String = spec["clave"]
		var paso: float = float(spec["paso"])
		var minimo: float = float(spec["min"])
		var maximo: float = float(spec["max"])
		var columna := VBoxContainer.new()
		columna.custom_minimum_size = Vector2(205, 0)
		var titulo := Label.new()
		titulo.text = spec["titulo"]
		titulo.add_theme_font_size_override("font_size", 11)
		columna.add_child(titulo)
		# Al pasar el mouse por cualquier parte de la columna sale la explicación.
		columna.tooltip_text = spec["ayuda"]
		titulo.tooltip_text = spec["ayuda"]
		titulo.mouse_filter = Control.MOUSE_FILTER_STOP
		var botones := HBoxContainer.new()
		botones.add_theme_constant_override("separation", 3)
		botones.add_child(_boton_aterrizaje("◀◀", func(): _ajustar_aterrizaje(clave, -paso * 10.0, minimo, maximo), spec["ayuda"]))
		botones.add_child(_boton_aterrizaje("◀", func(): _ajustar_aterrizaje(clave, -paso, minimo, maximo), spec["ayuda"]))
		var valor := Label.new()
		valor.custom_minimum_size = Vector2(78, 0)
		valor.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
		valor.add_theme_font_size_override("font_size", 11)
		valor.tooltip_text = spec["ayuda"]
		valor.mouse_filter = Control.MOUSE_FILTER_STOP
		botones.add_child(valor)
		botones.add_child(_boton_aterrizaje("▶", func(): _ajustar_aterrizaje(clave, paso, minimo, maximo), spec["ayuda"]))
		botones.add_child(_boton_aterrizaje("▶▶", func(): _ajustar_aterrizaje(clave, paso * 10.0, minimo, maximo), spec["ayuda"]))
		if clave == "altura_tren" or clave == "vel_despegue":
			botones.add_child(_boton_aterrizaje("AUTO", func(): _poner_aterrizaje_auto(clave), spec["ayuda"]))
		columna.add_child(botones)
		valores_aterrizaje_labels[clave] = valor
		var ayuda := Label.new()
		ayuda.text = spec["ayuda"]
		ayuda.autowrap_mode = TextServer.AUTOWRAP_WORD_SMART
		ayuda.add_theme_font_size_override("font_size", 9)
		ayuda.add_theme_color_override("font_color", Color(0.75, 0.8, 0.85))
		columna.add_child(ayuda)
		fila.add_child(columna)
	_refrescar_panel_aterrizaje()

func _actualizar_hud(delta: float) -> void:
	var altura = mundo.altitud_avion + position.y - altura_piso
	etiqueta_altimetro.text = "ALT\n%dm" % int(round(altura))
	# Antes solo se actualizaba con el panel de Configuración abierto (ahí
	# vivía el reloj) -- ahora que se movió a la barra de arriba (siempre
	# visible), tiene que refrescarse siempre.
	_actualizar_etiqueta_hora()
	_actualizar_etiqueta_ram(delta)
	_actualizar_secuencia_mision(delta)
	_actualizar_animacion_marcador_destino(delta)
	_actualizar_boton_bajar_subir()

	if destinos.is_empty():
		destinos = mundo.obtener_destinos()
		if not destinos.is_empty():
			# Pedido explícito 2026-09-22 ("me volví loco buscando los
			# aeropuertos... que estén ordenados alfabéticamente") -- se
			# ordena ACÁ (no en mundo.gd) para no tocar el orden en el que se
			# generan/reubican los nodos, solo el orden en que aparecen en
			# los desplegables Desde/Hasta.
			destinos.sort_custom(func(a, b):
				return a.get_meta("nombre_bonito", a.name) < b.get_meta("nombre_bonito", b.name))
			_poblar_selector()

	temporizador_torre += delta
	if temporizador_torre >= 0.5:
		temporizador_torre = 0.0
		_actualizar_torre(altura)

	_actualizar_panel_torre(delta)

# Llena los dos desplegables ("Desde" / "Hasta") con los nombres de todos los
# aeropuertos que existan en Mundo -- así, si mañana agregás uno nuevo ahí,
# automáticamente aparece acá también, sin tocar nada más.
func _poblar_selector() -> void:
	if selector_poblado:
		return
	selector_poblado = true
	for nodo in destinos:
		var nombre = nodo.get_meta("nombre_bonito", nodo.name)
		origen_option.add_item(nombre)
		destino_option.add_item(nombre)
	# Mismo motivo que boton_confirmar/boton_despegar: un control con foco de
	# teclado puede reaccionar a ESPACIO -- estos dos quedaron afuera de esa
	# limpieza en su momento.
	origen_option.focus_mode = Control.FOCUS_NONE
	destino_option.focus_mode = Control.FOCUS_NONE
	# Antes esto faltaba: "Desde" quedaba SIN selección real (-1) hasta que el
	# usuario lo tocara a mano, y al apretar "Confirmar viaje" sin tocarlo el
	# código lo tomaba como "no elegiste nada" y no hacía nada -- por eso
	# parecía que el botón "no funcionaba".
	# Vuelo predeterminado (pedido 2026-10-01): Aeroparque -> Ezeiza. BUG REAL
	# encontrado 2026-10-01 (reportado: "en origen aparece Luján todavía"):
	# el índice 0 NO es Aeroparque -- se asumió que el orden alfabético lo
	# dejaba primero, pero no es así. Buscado por nombre, igual que "Hasta",
	# para no depender de en qué posición caiga cada uno.
	origen_option.select(0)
	var indice_origen_default: int = 0
	var indice_destino_default: int = 1
	for i in destinos.size():
		var nombre_destino: String = destinos[i].get_meta("nombre_bonito", destinos[i].name)
		if nombre_destino == "Aeroparque":
			indice_origen_default = i
		elif nombre_destino == "Ezeiza":
			indice_destino_default = i
	origen_option.select(indice_origen_default)
	destino_option.select(indice_destino_default)
	check_vuelo_libre.focus_mode = Control.FOCUS_NONE
	check_vuelo_libre.toggled.connect(func(activo: bool):
		destino_option.disabled = activo
		label_hasta.modulate.a = 0.4 if activo else 1.0)

# Cambia el modelo visible del avión (por ahora SOLO visual -- todos vuelan
# igual todavía, eso viene después con físicas por tipo). "" = mostrar las
# piezas primitivas de siempre; cualquier otra cosa = cargar ese GLB con su
# propia escala/rotación de corrección (ver TIPOS_AVION).
# Luces del avioncito clásico (ver comentario junto a la declaración de las
# variables) -- se crean UNA sola vez acá, y _aplicar_tipo_avion() solo las
# muestra/oculta según el avión elegido.
func _crear_luces_avion_clasico() -> void:
	# Punta de ala izquierda (roja) y derecha (verde) -- las alas (BoxMesh de
	# 5.5 de ancho, centradas en el origen) tienen la punta en X = ±2.75.
	# BUG REAL encontrado 2026-09-26 (reportado: "las luces rojas y verdes no
	# aparecen"): el ala mide solo 0.15 de alto (Y de -0.075 a +0.075) pero
	# la esfera estaba centrada en Y=0.05 con radio 0.07 -- más de la mitad
	# quedaba ENTERRADA dentro del BoxMesh sólido del ala, visible apenas
	# como un borde de 4-5cm. Subida por encima del ala (Y=0.14, con margen)
	# y agrandada un poco para que se note de verdad.
	luz_punta_ala_izq = _crear_luz_navegacion(Vector3(-2.75, 0.14, 0), Color(1.0, 0.1, 0.1))
	luz_punta_ala_der = _crear_luz_navegacion(Vector3(2.75, 0.14, 0), Color(0.1, 1.0, 0.2))

	# Estroboscópica blanca arriba de todo -- parpadeo CORTO y agudo (no una
	# onda suave), como un flash real, no una respiración.
	luz_estroboscopica = _crear_luz_navegacion(Vector3(0, 0.55, 0), Color(1.0, 1.0, 1.0))
	(luz_estroboscopica.mesh as SphereMesh).radius = 0.11
	(luz_estroboscopica.mesh as SphereMesh).height = 0.22

	# Luz que ilumina el ala (pedido explícito, "como tienen los aviones
	# reales que iluminan sobre el ala") -- un SpotLight3D real (acá SÍ vale
	# la pena, es UN solo avión, no 30 aeropuertos): montada cerca de la
	# raíz del ala, apuntando hacia afuera y un poco hacia abajo para bañar
	# la superficie del ala de luz cálida. Pedido 2026-09-26: más intensidad
	# y que cubra las DOS alas (antes solo había una, apuntando a un lado).
	luz_iluminacion_ala = SpotLight3D.new()
	luz_iluminacion_ala.name = "LuzIluminacionAla"
	luz_iluminacion_ala.position = Vector3(0, 0.35, -0.3)
	luz_iluminacion_ala.rotation_degrees = Vector3(-25, 90, 0)
	luz_iluminacion_ala.light_color = Color(1.0, 0.95, 0.85)
	luz_iluminacion_ala.light_energy = 3.5
	luz_iluminacion_ala.spot_range = 5.0
	luz_iluminacion_ala.spot_angle = 45.0
	add_child(luz_iluminacion_ala)

	luz_iluminacion_ala_2 = SpotLight3D.new()
	luz_iluminacion_ala_2.name = "LuzIluminacionAla2"
	luz_iluminacion_ala_2.position = Vector3(0, 0.35, -0.3)
	luz_iluminacion_ala_2.rotation_degrees = Vector3(-25, -90, 0)
	luz_iluminacion_ala_2.light_color = Color(1.0, 0.95, 0.85)
	luz_iluminacion_ala_2.light_energy = 3.5
	luz_iluminacion_ala_2.spot_range = 5.0
	luz_iluminacion_ala_2.spot_angle = 45.0
	add_child(luz_iluminacion_ala_2)

func _crear_luz_navegacion(posicion: Vector3, color: Color, padre: Node3D = null, radio: float = 0.09) -> MeshInstance3D:
	if padre == null:
		padre = self
	var luz = MeshInstance3D.new()
	var esfera = SphereMesh.new()
	esfera.radius = radio
	esfera.height = radio * 2.0
	luz.mesh = esfera
	var mat = StandardMaterial3D.new()
	mat.shading_mode = BaseMaterial3D.SHADING_MODE_UNSHADED
	mat.albedo_color = color
	mat.emission_enabled = true
	mat.emission = color
	mat.emission_energy_multiplier = 4.0
	# TRANSPARENCY_ALPHA habilitado acá (no solo en la estroboscópica) para
	# que el alfa del material pueda animarse -- en un material UNSHADED el
	# albedo se ve a full brillo SIEMPRE sin importar la emisión, así que
	# bajar solo emission_energy_multiplier no alcanza para "apagar" una
	# luz, hace falta bajar también el alfa.
	mat.transparency = BaseMaterial3D.TRANSPARENCY_ALPHA
	luz.set_surface_override_material(0, mat)
	luz.position = posicion
	# BUG REAL encontrado 2026-09-26 (reportado: "se ve el circulito pero
	# como que el LED no está prendido", y la estroboscópica "no aparece"):
	# estas lucecitas se quedaron en la capa normal (1), así que el mismo
	# post-proceso de noche que apagaba/desaturaba el cartel gigante del
	# aeropuerto (ver CAPA_MARCADORES_NOCTURNOS en mundo.gd) las apaga a
	# ELLAS también -- justo lo opuesto de lo que tiene que pasar con luces
	# de navegación reales, que se tienen que VER más de noche, no menos.
	# Mismo arreglo: pasarlas a la capa de marcadores (la cámara/Environment
	# aparte que ya usan ILS/luces de pista/beacon, sin el post-proceso).
	luz.set_layer_mask_value(1, false)
	luz.set_layer_mask_value(CAPA_MARCADORES_NOCTURNOS, true)
	padre.add_child(luz)
	# Pedido explícito 2026-10-01 ("las bolitas se ven, pero no iluminan el
	# avión"): hasta ahora la bolita era solo una malla que brilla ELLA
	# MISMA (emission), sin ningún Light3D real -- no alumbraba nada
	# alrededor. Un OmniLight3D chiquito y de corto alcance, del mismo
	# color, hace que el ala/fuselaje cercano se vea tenuemente iluminado
	# de noche, como en un avión real.
	var omni := OmniLight3D.new()
	omni.light_color = color
	omni.light_energy = 1.2
	omni.omni_range = radio * 6.0
	# Hija de la bolita, no hermana (posición LOCAL (0,0,0) = centrada en la
	# bolita) -- así si el editor de luces arrastra la bolita, la luz real la
	# sigue solita, no se queda atrás en la posición vieja.
	luz.add_child(omni)
	return luz

func _texto_a_vector3(t: String) -> Vector3:
	var p: PackedStringArray = t.split(",")
	return Vector3(float(p[0]), float(p[1]), float(p[2]))

# Lee ajustes_luces_debug.txt y devuelve las luces clavadas MÁS RECIENTES de
# un avión ({"izq", "der", "estrobo"}), o {} si ese avión nunca se clavó. Cada
# vez que se clava se agrega un renglón al final, así que si hay varios del
# mismo avión gana el último.
func _luces_clavadas_desde_archivo(nombre_avion: String) -> Dictionary:
	if not FileAccess.file_exists(RUTA_AJUSTES_LUCES_DEBUG):
		return {}
	var texto: String = FileAccess.get_file_as_string(RUTA_AJUSTES_LUCES_DEBUG)
	var rx := RegEx.new()
	rx.compile(r'# "([^"]+)" -- [^\n]*\n"luces_manual": \{"izq": Vector3\(([^)]*)\), "der": Vector3\(([^)]*)\), "estrobo": Vector3\(([^)]*)\)\}')
	var resultado: Dictionary = {}
	for m in rx.search_all(texto):
		if m.get_string(1) == nombre_avion:
			resultado = {
				"izq": _texto_a_vector3(m.get_string(2)),
				"der": _texto_a_vector3(m.get_string(3)),
				"estrobo": _texto_a_vector3(m.get_string(4))}
	return resultado

func _vector3_a_texto(v: Vector3) -> String:
	return "Vector3(%.4f, %.4f, %.4f)" % [v.x, v.y, v.z]

# Editor de luces en vivo: escribe la posición FINAL de las 3 lucecitas (en
# espacio local del modelo -- luz.position ya es exactamente eso, porque son
# hijas directas de "instancia") a un archivo de texto, listas para pegar en
# TIPOS_AVION como "luces_manual". No se guardan solas ni se tocan los
# valores en caliente. Desde 2026-10-16 el juego lee este archivo SOLO al
# crear el avión (ver _luces_clavadas_desde_archivo): clavar y reiniciar alcanza,
# no hace falta copiar nada a TIPOS_AVION.
func _guardar_ajuste_luces_editadas() -> void:
	if not luz_navegacion_izq_externa or not luz_navegacion_der_externa or not luz_estroboscopica_externa:
		return
	if not is_instance_valid(luz_navegacion_izq_externa) or not is_instance_valid(luz_navegacion_der_externa) or not is_instance_valid(luz_estroboscopica_externa):
		return
	var nombre_avion: String = TIPOS_AVION[tipo_avion_indice]["nombre"]
	var texto := "\n# \"%s\" -- %s\n" % [nombre_avion, Time.get_datetime_string_from_system()]
	texto += "\"luces_manual\": {\"izq\": %s, \"der\": %s, \"estrobo\": %s}\n" % [
		_vector3_a_texto(luz_navegacion_izq_externa.position),
		_vector3_a_texto(luz_navegacion_der_externa.position),
		_vector3_a_texto(luz_estroboscopica_externa.position)]
	var existente := ""
	if FileAccess.file_exists(RUTA_AJUSTES_LUCES_DEBUG):
		existente = FileAccess.get_file_as_string(RUTA_AJUSTES_LUCES_DEBUG)
	var archivo := FileAccess.open(RUTA_AJUSTES_LUCES_DEBUG, FileAccess.WRITE)
	archivo.store_string(existente + texto)
	archivo.close()
	print("📌 Luces clavadas para \"%s\" -- guardado en %s" % [nombre_avion, RUTA_AJUSTES_LUCES_DEBUG])
	print(texto)

# Pedido 2026-10-16 (A380: "hay una luz más grande que la otra -- ponelas las
# dos del mismo tamaño, del tamaño de la más chica"): las dos esferas tienen
# el MISMO radio real, pero la que queda más cerca de la cámara se ve más
# grande. Se compensa por distancia: la más lejana (la que se ve más chica)
# queda como está y la más cercana se achica en proporción, así las dos se ven
# del mismo tamaño en pantalla desde cualquier ángulo. Solo para los aviones
# con "luces_mismo_tamano" en TIPOS_AVION.
func _igualar_tamano_luces_navegacion() -> void:
	if not luces_igualar_tamano:
		return
	if not luz_navegacion_izq_externa or not luz_navegacion_der_externa:
		return
	if not is_instance_valid(luz_navegacion_izq_externa) or not is_instance_valid(luz_navegacion_der_externa):
		return
	if not camara:
		return
	var d_izq: float = camara.global_position.distance_to(luz_navegacion_izq_externa.global_position)
	var d_der: float = camara.global_position.distance_to(luz_navegacion_der_externa.global_position)
	var d_max: float = max(max(d_izq, d_der), 0.0001)
	luz_navegacion_izq_externa.scale = Vector3.ONE * (d_izq / d_max)
	luz_navegacion_der_externa.scale = Vector3.ONE * (d_der / d_max)

# Destello CORTO y agudo (no una onda suave) para la estroboscópica -- pow()
# con exponente alto deja la mayor parte del ciclo casi apagado y solo un
# pico breve bien brillante, como un flash real de anticolisión.
const VELOCIDAD_ESTROBOSCOPICA = 0.7
func _actualizar_estroboscopica(_delta: float) -> void:
	var t: float = Time.get_ticks_msec() * 0.001
	var destello: float = pow(max(0.0, sin(t * TAU * VELOCIDAD_ESTROBOSCOPICA)), 12.0)
	# Actualiza la del avioncito primitivo (si está activa) Y la del modelo
	# externo actual (si hay uno cargado) -- solo una de las dos existe/está
	# visible a la vez según el avión elegido, pero no cuesta nada chequear
	# ambas acá en vez de duplicar esta función.
	# Pedido 2026-10-11: las balizas de los barcos usan el MISMO parpadeo --
	# se suman acá a la lista en vez de duplicar la función.
	var luces: Array = [luz_estroboscopica, luz_estroboscopica_externa]
	for barco in objetivos_barcos:
		if is_instance_valid(barco):
			var baliza_barco: Node = barco.get_node_or_null("BalizaBarco")
			if baliza_barco:
				luces.append(baliza_barco)
	for objetivo_industria in objetivos_industria:
		if is_instance_valid(objetivo_industria):
			var baliza_industria: Node = objetivo_industria.get_node_or_null("BalizaParpadeante")
			if baliza_industria:
				luces.append(baliza_industria)
	for luz in luces:
		if not luz or not luz.visible:
			continue
		var mat: StandardMaterial3D = luz.get_surface_override_material(0)
		mat.emission_energy_multiplier = lerp(0.3, 6.0, destello)
		# BUG REAL encontrado 2026-09-26 (reportado: "queda una luz blanca
		# prendida en el medio, no hace flash"): en un material UNSHADED el
		# albedo_color se ve a brillo completo SIEMPRE, sin importar cuánto
		# baje emission_energy_multiplier -- por eso la bolita blanca se
		# veía sólida y fija en vez de apagarse entre destellos. El apagado
		# real tiene que venir del ALFA del material (habilitado en
		# _crear_luz_navegacion).
		mat.albedo_color.a = lerp(0.12, 1.0, destello)

# Calcula el AABB combinado de todos los MeshInstance3D bajo "raiz", en el
# espacio LOCAL de "raiz" (sin aplicar la propia transform de "raiz") --
# así las luces que se agreguen como hijas directas de "raiz" usando estas
# mismas coordenadas locales quedan bien ubicadas sin importar la escala o
# rotación que "raiz" tenga aplicada.
func _calcular_aabb_local(raiz: Node3D) -> AABB:
	var caja := AABB()
	var alguna := false
	var pila: Array = [[raiz, Transform3D.IDENTITY]]
	while not pila.is_empty():
		var item = pila.pop_back()
		var nodo: Node3D = item[0]
		var transform_acumulado: Transform3D = item[1]
		if nodo != raiz:
			transform_acumulado = transform_acumulado * nodo.transform
		if nodo is MeshInstance3D and nodo.mesh:
			var aabb_local: AABB = nodo.mesh.get_aabb()
			for i in range(8):
				var punto: Vector3 = transform_acumulado * aabb_local.get_endpoint(i)
				if alguna:
					caja = caja.expand(punto)
				else:
					caja = AABB(punto, Vector3.ZERO)
					alguna = true
		for hijo in nodo.get_children():
			if hijo is Node3D:
				pila.append([hijo, transform_acumulado])
	return caja

# Pedido explícito 2026-09-26 ("esas mismas luces agregalas a todos los
# aviones, porque de noche si no es el avioncito tuyo todos se ven como una
# cosa negra"): genera nav lights (rojo/verde en las puntas, estroboscópica
# arriba) + luces de iluminación de ala para CUALQUIER modelo externo
# (glb/fbx), calculando las posiciones a partir de su propio AABB en vez de
# coordenadas fijas (que solo tienen sentido para el mesh primitivo). Es una
# aproximación razonable para una forma "de avión" (ancho en X = envergadura,
# ya que todos los modelos se rotan para mirar hacia -Z), no va a quedar
# perfecto en formas raras (ej. un helicóptero), pero es mucho mejor que
# quedar como una silueta negra de noche.
func _generar_luces_para_modelo_externo(instancia: Node3D, datos: Dictionary) -> void:
	var caja: AABB = _calcular_aabb_local(instancia)
	if caja.size.length() < 0.001:
		return
	# BUG REAL encontrado 2026-09-27 (reportado: "el Boeing y los demás
	# aviones viejos no tienen luces de noche"): el radio/alcance se
	# calculaba sobre el tamaño NATIVO del modelo (caja.size, en las
	# unidades del archivo original), sin tener en cuenta la "escala" que
	# después se le aplica a "instancia" -- el Boeing mide ~1500 unidades en
	# su archivo original y se compensa con escala 0.006, así que un radio
	# calculado en esas unidades nativas (aunque tope en el clamp máximo de
	# 0.4) terminaba en 0.4*0.006 = 0.0024 al escalarse -- una luz
	# microscópica, invisible. Ahora se calcula el tamaño en unidades REALES
	# de mundo (caja * escala) y se vuelve a dividir por la escala al final,
	# para que el tamaño FINAL ya escalado quede siempre en un rango
	# razonable sin importar cuán grandes/chicas sean las unidades nativas
	# de cada modelo.
	var escala: float = datos.get("escala", 1.0)
	if escala <= 0.0:
		escala = 1.0
	var tamano_mundo: float = caja.size.length() * escala
	# Achicadas ~30% (pedido 2026-10-01, "achicá todas las luces un poquito,
	# la posición queda igual") -- NO toca ninguna posición clavada con el
	# editor de luces, solo el tamaño de la bolita visible.
	var radio_luz: float = clamp(tamano_mundo * 0.012, 0.03, 0.4) / escala * 0.7
	var y_medio: float = caja.position.y + caja.size.y * 0.55
	var z_medio: float = caja.position.z + caja.size.z * 0.5
	# La estroboscópica suele ser 1.2x más grande que la roja y la verde. Con
	# "luces_mismo_tamano": true en TIPOS_AVION (pedido 2026-10-16, A380) las
	# tres quedan del tamaño de la más chica.
	var radio_estrobo_relativo: float = 1.0 if datos.get("luces_mismo_tamano", false) else 1.2
	luces_igualar_tamano = datos.get("luces_mismo_tamano", false)

	# "luces_manual" 2026-09-26 (reportado: "las luces aparecen arriba o de
	# costado, nada que ver"): el cálculo automático de más abajo asume que
	# la envergadura siempre está en el eje X del modelo, pero cada .fbx
	# trae sus propios ejes según cómo lo armó el autor original -- para
	# KF-30 la envergadura real está en Z, no en X. En vez de seguir
	# adivinando el eje para cada modelo nuevo, se puede definir la posición
	# EXACTA por avión en TIPOS_AVION (sacada con un dump de la escena real,
	# usando piezas de referencia del propio modelo cuando las tenía).
	# Pedido 2026-10-16 ("que se aplique solo, sin tener que venir a avisarte
	# avión por avión"): primero se mira lo CLAVADO en ajustes_luces_debug.txt
	# (el último renglón de cada avión gana), después "luces_manual" de
	# TIPOS_AVION, y si no hay ninguno de los dos, el cálculo automático de
	# más abajo. Así clavar las luces con el editor y reiniciar alcanza.
	var lm: Dictionary = _luces_clavadas_desde_archivo(datos["nombre"])
	if lm.is_empty() and datos.has("luces_manual"):
		lm = datos["luces_manual"]
	if not lm.is_empty():
		luz_navegacion_izq_externa = _crear_luz_navegacion(lm["izq"], Color(1.0, 0.1, 0.1), instancia, radio_luz)
		luz_navegacion_der_externa = _crear_luz_navegacion(lm["der"], Color(0.1, 1.0, 0.2), instancia, radio_luz)
		luz_estroboscopica_externa = _crear_luz_navegacion(lm["estrobo"], Color(1.0, 1.0, 1.0), instancia, radio_luz * radio_estrobo_relativo)
	else:
		# "ajuste_izq"/"ajuste_der"/"ajuste_estrobo" (pedido 2026-10-01, Piper:
		# la roja y la verde un centímetro para arriba y un centímetro hacia
		# afuera CADA UNA para su lado, la blanca -estroboscópica- un
		# centímetro para abajo) -- cada luz necesita su propio ajuste, no uno
		# solo compartido (antes "ajuste_luces" único no podía mandar la
		# estroboscópica para un lado distinto al de las otras dos). Unidades
		# nativas, medidas con el avión en escala 1.0. Vector3.ZERO en los tres
		# para cualquier otro avión no cambia nada.
		var ajuste_izq: Vector3 = datos.get("ajuste_izq", Vector3.ZERO)
		var ajuste_der: Vector3 = datos.get("ajuste_der", Vector3.ZERO)
		var ajuste_estrobo: Vector3 = datos.get("ajuste_estrobo", Vector3.ZERO)
		var x_izq: float = caja.position.x + ajuste_izq.x
		var x_der: float = caja.end.x + ajuste_der.x
		var y_arriba: float = caja.end.y + caja.size.y * 0.08
		luz_navegacion_izq_externa = _crear_luz_navegacion(Vector3(x_izq, y_medio + ajuste_izq.y, z_medio + ajuste_izq.z), Color(1.0, 0.1, 0.1), instancia, radio_luz)
		luz_navegacion_der_externa = _crear_luz_navegacion(Vector3(x_der, y_medio + ajuste_der.y, z_medio + ajuste_der.z), Color(0.1, 1.0, 0.2), instancia, radio_luz)
		luz_estroboscopica_externa = _crear_luz_navegacion(Vector3(caja.position.x + caja.size.x * 0.5 + ajuste_estrobo.x, y_arriba + ajuste_estrobo.y, z_medio + ajuste_estrobo.z), Color(1.0, 1.0, 1.0), instancia, radio_luz * radio_estrobo_relativo)

	var rango_spot: float = clamp(tamano_mundo * 0.35, 1.0, 8.0) / escala
	for signo in [1.0, -1.0]:
		var spot := SpotLight3D.new()
		spot.position = Vector3(0, y_medio, z_medio)
		spot.rotation_degrees = Vector3(-25, 90.0 * signo, 0)
		spot.light_color = Color(1.0, 0.95, 0.85)
		spot.light_energy = 3.5
		spot.spot_range = rango_spot
		spot.spot_angle = 45.0
		instancia.add_child(spot)

func _aplicar_tipo_avion(indice: int) -> void:
	if indice < 0 or indice >= TIPOS_AVION.size():
		return
	tipo_avion_indice = indice
	_reiniciar_estado_tierra()
	call_deferred("_medir_altura_tren")
	_aplicar_sonido_motor(indice)
	if is_node_ready():
		_refrescar_panel_sensibilidad()
	var datos = TIPOS_AVION[indice]
	for hijo in modelo_externo.get_children():
		hijo.queue_free()
	# El modelo externo viejo (si había uno) se está por liberar junto con
	# su estroboscópica -- limpiar la referencia ACÁ, no solo cuando se crea
	# una nueva, para que _actualizar_estroboscopica no toque un nodo
	# liberado en el frame en que se cambia a "Avioncito clásico".
	luz_estroboscopica_externa = null
	luz_navegacion_izq_externa = null
	luz_navegacion_der_externa = null
	luz_arrastrada = null
	luces_igualar_tamano = false
	var mostrar_primitivas = datos["modelo"] == ""
	pieza_fuselaje.visible = mostrar_primitivas
	pieza_nariz.visible = mostrar_primitivas
	pieza_alas.visible = mostrar_primitivas
	pieza_cola.visible = mostrar_primitivas
	pieza_timon.visible = mostrar_primitivas
	if luz_punta_ala_izq:
		luz_punta_ala_izq.visible = mostrar_primitivas
		luz_punta_ala_der.visible = mostrar_primitivas
		luz_estroboscopica.visible = mostrar_primitivas
		luz_iluminacion_ala.visible = mostrar_primitivas
		luz_iluminacion_ala_2.visible = mostrar_primitivas
	if not mostrar_primitivas:
		var escena: PackedScene = load(datos["modelo"])
		if escena:
			var instancia = escena.instantiate()
			# BUG REAL encontrado 2026-10-11 (reportado: "el A380 sigue
			# invisible por más que lo achico"): mismo bug que el barco
			# aquitaine -- el pivote (0,0,0) de este GLB en particular está
			# clavado a miles de unidades del centro visual real (jerarquía
			# deforme de export, típico de Sketchfab). Ningún valor de
			# escala arregla eso, porque el casco entero queda lejos de
			# donde la cámara mira, no importa cuán chico sea. Si el avión
			# trae "recentrar_pivote": true, se mide el AABB nativo ANTES
			# de escalar/rotar y se corrige la posición para que el centro
			# geométrico real quede sobre el pivote, igual que se hizo con
			# los barcos.
			var caja_nativa_avion: AABB
			if datos.get("recentrar_pivote", false):
				caja_nativa_avion = _calcular_aabb_local(instancia)
			instancia.scale = Vector3.ONE * datos["escala"]
			instancia.rotation_degrees = datos["rotacion"]
			# BUG REAL encontrado 2026-09-26 (reportado: "hay una base
			# cuadrada pegada arriba del avión" en el avión de combate) --
			# el .fbx trae un nodo suelto llamado "Plane" (un plano de
			# referencia/fondo que quedó del archivo original de Sketchfab,
			# escalado ~221x) que no es parte del avión real. Se saca de
			# cualquier modelo que lo traiga, no solo de este.
			var plano_sobrante = instancia.get_node_or_null("Plane")
			if plano_sobrante:
				plano_sobrante.queue_free()
			modelo_externo.add_child(instancia)
			if datos.get("recentrar_pivote", false):
				var basis_final_avion: Basis = instancia.basis
				var centro_nativo_avion: Vector3 = caja_nativa_avion.position + caja_nativa_avion.size * 0.5
				instancia.position = -(basis_final_avion * centro_nativo_avion)
			# Piper PA-18 (pedido 2026-10-01, "los colores de la textura no
			# aparecieron"): investigado el .fbx a mano -- NO trae ninguna
			# referencia a archivo de textura adentro (el exportador la dejó
			# afuera del todo), así que Godot no tiene de dónde auto-resolverla
			# aunque el .jpg esté al lado con el nombre correcto. Clave
			# "textura" en TIPOS_AVION fuerza la textura a mano sobre TODAS las
			# mallas del modelo -- sirve para este caso y cualquier .fbx futuro
			# con el mismo problema.
			# BUG REAL encontrado 2026-10-01 (reportado: "las luces de colores
			# aparecen NEGRAS hasta de día"): esto se ejecutaba DESPUÉS de
			# _generar_luces_para_modelo_externo, así que el material forzado
			# (sin emisión) se les pisaba por encima a las bolitas de
			# navegación de colores TAMBIÉN -- son MeshInstance3D como
			# cualquier otra del modelo, _aplicar_material_recursivo no sabía
			# distinguirlas. Ahora la textura se fuerza ANTES de crear las
			# luces, así nunca las toca.
			if datos.has("textura"):
				# BUG REAL encontrado 2026-10-01 (reportado: "al darle a volar
				# se cuelga"): acá estaba tipado como "Image", pero load() de
				# un .jpg YA IMPORTADO devuelve una Texture2D (CompressedTexture2D),
				# no una Image -- el choque de tipos rompía esta función (y de
				# paso el `is MeshInstance3D` después de queue_free() de la
				# etiqueta "Plane" en modelos raros). Sin el paso intermedio por
				# Image hace falta: load() ya da directamente lo que
				# albedo_texture necesita.
				var textura: Texture2D = load(datos["textura"])
				if textura:
					var material_forzado := StandardMaterial3D.new()
					material_forzado.albedo_texture = textura
					_aplicar_material_recursivo(instancia, material_forzado)
			# Pedido explícito 2026-09-26: las mismas luces de navegación del
			# avioncito clásico, para TODOS los aviones (de noche se veían
			# "como una cosa negra" sin esto).
			_generar_luces_para_modelo_externo(instancia, datos)

func _aplicar_material_recursivo(nodo: Node, material: Material) -> void:
	if nodo is MeshInstance3D and nodo.mesh:
		for i in nodo.mesh.get_surface_count():
			nodo.set_surface_override_material(i, material)
	for hijo in nodo.get_children():
		_aplicar_material_recursivo(hijo, material)

func _es_helicoptero() -> bool:
	return TIPOS_AVION[tipo_avion_indice]["helicoptero"]

# Índice del "Helicóptero" dentro de TIPOS_AVION -- buscado por nombre en vez
# de hardcodear el número, para no romperse si el orden de la lista cambia.
func _indice_tipo_helicoptero() -> int:
	for i in TIPOS_AVION.size():
		if TIPOS_AVION[i]["helicoptero"]:
			return i
	return 0

const PASO_PRECIO_MISION = 50

# Arma la lista de misiones (una fila por misión) adentro del panel -- se
# llama una sola vez en _ready(). Cada fila: nombre, precio (editable con
# ▲▼ hasta que fijemos precios reales) y un botón para arrancarla.
func _poblar_lista_misiones() -> void:
	for i in mundo.misiones.size():
		var datos = mundo.misiones[i]
		var fila = HBoxContainer.new()

		var etiqueta_nombre = Label.new()
		etiqueta_nombre.text = datos["nombre"]
		etiqueta_nombre.size_flags_horizontal = Control.SIZE_EXPAND_FILL
		fila.add_child(etiqueta_nombre)

		var etiqueta_precio = Label.new()
		etiqueta_precio.custom_minimum_size = Vector2(70, 0)
		etiqueta_precio.text = "$%d" % datos["precio"]
		etiqueta_precio.horizontal_alignment = HORIZONTAL_ALIGNMENT_RIGHT
		fila.add_child(etiqueta_precio)

		var boton_bajar = Button.new()
		boton_bajar.text = "▼"
		boton_bajar.focus_mode = Control.FOCUS_NONE
		boton_bajar.pressed.connect(func():
			datos["precio"] = max(0, datos["precio"] - PASO_PRECIO_MISION)
			etiqueta_precio.text = "$%d" % datos["precio"])
		fila.add_child(boton_bajar)

		var boton_subir = Button.new()
		boton_subir.text = "▲"
		boton_subir.focus_mode = Control.FOCUS_NONE
		boton_subir.pressed.connect(func():
			datos["precio"] += PASO_PRECIO_MISION
			etiqueta_precio.text = "$%d" % datos["precio"])
		fila.add_child(boton_subir)

		var boton_volar = Button.new()
		boton_volar.text = "Volar"
		boton_volar.focus_mode = Control.FOCUS_NONE
		boton_volar.pressed.connect(_iniciar_mision.bind(i))
		fila.add_child(boton_volar)

		lista_misiones.add_child(fila)

# BUG encontrado 2026-09-20 (reportado por el usuario): elegir una misión no
# daba NINGUNA guía para llegar (ni rumbo, ni flechas, ni distancia) -- y
# encima te teletransportaba DIRECTO arriba del objetivo, lo cual hacía la
# guía inútil de por sí (ya estabas ahí). Ahora: NO teletransporta, te deja
# donde estés volando y activa la guía (rumbo/flechas/distancia, las mismas
# que ya existían para los aeropuertos) apuntando al objetivo de la misión,
# para volar hasta ahí de verdad.
# Arranca una de las 3 misiones de licencia de helicóptero (Modo Carrera):
# teletransporta al origen fijo de la misión, orientado hacia el destino,
# fuerza el helicóptero, fuerza la ayuda visual (obligatoria, no se puede
# apagar durante la licencia) y arma los aros -- ver comentario junto a
# MISIONES_LICENCIA_HELICOPTERO.
func _iniciar_mision_licencia(indice: int) -> void:
	if indice < 0 or indice >= MISIONES_LICENCIA_HELICOPTERO.size():
		return
	var datos = MISIONES_LICENCIA_HELICOPTERO[indice]

	tipo_avion_option.select(_indice_tipo_helicoptero())
	_aplicar_tipo_avion(_indice_tipo_helicoptero())

	var pos_origen: Vector3 = mundo._posicion_desde_lat_lon(datos["origen_lat"], datos["origen_lon"], 8.0)
	global_position = pos_origen
	var pos_destino: Vector3 = mundo._posicion_desde_lat_lon(datos["destino_lat"], datos["destino_lon"], 8.0)
	look_at(pos_destino, _arriba_real())

	# OJO: objetivo_licencia tiene que ser hijo de "mundo" (el root), NO del
	# propio avión -- si fuera hijo del avión, su posición viajaría PEGADA al
	# avión en vez de quedarse fija en el destino real. Se suma a
	# mundo.nodos_misiones para que el propio bucle de recentrado de mundo.gd
	# lo reubique todos los cuadros con el origen flotante actualizado --
	# mismo mecanismo ya probado que usan aeropuertos/misiones/pueblos,
	# ninguna lógica nueva que pueda tener el mismo bug que tuvo la línea guía.
	objetivo_licencia = Node3D.new()
	objetivo_licencia.set_meta("lat", datos["destino_lat"])
	objetivo_licencia.set_meta("lon", datos["destino_lon"])
	mundo.add_child(objetivo_licencia)
	mundo._actualizar_transform_aeropuerto(objetivo_licencia, datos["destino_lat"], datos["destino_lon"])
	mundo.nodos_misiones.append(objetivo_licencia)

	objetivo_mision = objetivo_licencia
	nombre_mision_actual = datos["nombre"]
	mision_licencia_indice = indice

	# Obligatoria: no se puede desactivar mientras dure la licencia (pedido
	# explícito, "para la licencia tiene que estar activado, no se puede
	# desactivar").
	ayuda_visual_activa = true
	luz_ayuda_visual.color = COLOR_LUZ_ENCENDIDA
	objetivo_guia_actual = null  # fuerza recaptura del origen (mismo motivo que en _confirmar_viaje)

	_generar_aros_licencia(datos)

	velocidad_actual = VELOCIDAD_INICIAL
	_actualizar_etiqueta_velocidad()
	banco_actual = 0.0
	estado = Estado.VOLANDO
	cartel_central.text = "🎓 LICENCIA DE HELICÓPTERO (%d/%d)\n%s\n\nSeguí los aros hasta el destino" % [
		indice + 1, MISIONES_LICENCIA_HELICOPTERO.size(), datos["nombre"]]
	cartel_central.visible = true
	get_tree().create_timer(4.0).timeout.connect(func():
		if cartel_central.visible and estado == Estado.VOLANDO:
			cartel_central.visible = false)
	panel_carrera.visible = false

func _completar_mision_licencia() -> void:
	var indice = mision_licencia_indice
	var datos = MISIONES_LICENCIA_HELICOPTERO[indice]
	PerfilJugador.modificar_plata(int(datos["precio"]))
	PerfilJugador.marcar_mision_licencia_completa(NOMBRE_LICENCIA_HELICOPTERO, indice, MISIONES_LICENCIA_HELICOPTERO.size())

	var ya_tenia_licencia = PerfilJugador.tiene_licencia(NOMBRE_LICENCIA_HELICOPTERO)
	var texto_extra = ""
	if not ya_tenia_licencia and PerfilJugador.licencia_completa(NOMBRE_LICENCIA_HELICOPTERO, MISIONES_LICENCIA_HELICOPTERO.size()):
		PerfilJugador.otorgar_licencia(NOMBRE_LICENCIA_HELICOPTERO)
		texto_extra = "\n\n🎓✅ ¡LICENCIA DE HELICÓPTERO OBTENIDA!"

	cartel_central.text = "✅ Viaje completado: %s\n💰 +$%d%s" % [datos["nombre"], int(datos["precio"]), texto_extra]
	cartel_central.visible = true
	get_tree().create_timer(4.0).timeout.connect(func(): cartel_central.visible = false)

	_finalizar_mision_licencia()

# Corta la misión de licencia en curso (al completarla, o si el jugador
# cierra sin terminarla) -- limpia el objetivo temporal y los aros.
func _finalizar_mision_licencia() -> void:
	mision_licencia_indice = -1
	objetivo_mision = null
	if is_instance_valid(objetivo_licencia):
		mundo.nodos_misiones.erase(objetivo_licencia)
		objetivo_licencia.queue_free()
	objetivo_licencia = null
	if is_instance_valid(contenedor_aros_licencia):
		contenedor_aros_licencia.queue_free()
	contenedor_aros_licencia = null
	estado = Estado.DETENIDO
	velocidad_actual = 0.0
	_actualizar_etiqueta_velocidad()
	boton_despegar.visible = true  # para poder seguir volando después, como en cualquier otra llegada

# Arma la fila de aros entre origen y destino de la misión de licencia --
# MISMO diseño que el ILS de aeropuertos (le gustó al usuario), pero más
# grandes ("más margen de error") y sobre una línea recta simple (no depende
# de ningún dato de orientación real, así que no puede salir mal alineado
# como pasó con el ILS de pistas). Se reposicionan cada cuadro (ver
# _actualizar_aros_licencia_frame) porque están en coordenadas reales
# lat/lon, y el origen del mundo se recentra en el avión todo el tiempo.
func _generar_aros_licencia(datos: Dictionary) -> void:
	if is_instance_valid(contenedor_aros_licencia):
		contenedor_aros_licencia.queue_free()
	contenedor_aros_licencia = Node3D.new()
	add_child(contenedor_aros_licencia)

	var mat_aro = StandardMaterial3D.new()
	mat_aro.albedo_color = Color(0.3, 1.0, 0.4, 0.85)
	mat_aro.transparency = BaseMaterial3D.TRANSPARENCY_ALPHA
	mat_aro.emission_enabled = true
	mat_aro.emission = Color(0.3, 1.0, 0.4)
	mat_aro.emission_energy_multiplier = 0.6

	for i in CANTIDAD_AROS_LICENCIA:
		var aro = MeshInstance3D.new()
		var malla_aro = TorusMesh.new()
		malla_aro.inner_radius = RADIO_INTERNO_ARO_LICENCIA
		malla_aro.outer_radius = RADIO_EXTERNO_ARO_LICENCIA
		aro.mesh = malla_aro
		aro.set_surface_override_material(0, mat_aro)
		aro.set_meta("fraccion", (i + 1.0) / (CANTIDAD_AROS_LICENCIA + 1.0))
		contenedor_aros_licencia.add_child(aro)

# Recoloca los aros TODOS los cuadros -- mismo motivo que la línea guía
# (ver _actualizar_linea_guia_frame): el origen del mundo se recentra en el
# avión cada cuadro, así que una posición calculada una sola vez queda vieja
# enseguida.
func _actualizar_aros_licencia_frame() -> void:
	if not is_instance_valid(contenedor_aros_licencia) or mision_licencia_indice < 0:
		return
	var datos = MISIONES_LICENCIA_HELICOPTERO[mision_licencia_indice]
	var punto_origen: Vector3 = mundo._posicion_desde_lat_lon(datos["origen_lat"], datos["origen_lon"], ALTURA_AROS_LICENCIA)
	var punto_destino: Vector3 = mundo._posicion_desde_lat_lon(datos["destino_lat"], datos["destino_lon"], ALTURA_AROS_LICENCIA)
	if punto_origen.distance_to(punto_destino) < 1.0:
		return
	# TorusMesh trae el agujero atravesando su propio eje Y local (como una
	# rosquilla apoyada en una mesa) -- para que el agujero mire A LO LARGO
	# del camino de vuelo (no hacia arriba), construimos la base a mano con
	# basis.y = dirección de vuelo. Como el torus es simétrico rotando sobre
	# ese eje, no importa hacia dónde queden los otros dos ejes -- CUALQUIER
	# par perpendicular sirve, por eso no hace falta look_at() ni ningún
	# dato de rumbo real (evita el problema que tuvo el ILS de pistas).
	var direccion: Vector3 = (punto_destino - punto_origen).normalized()
	var referencia: Vector3 = Vector3.UP if abs(direccion.dot(Vector3.UP)) < 0.99 else Vector3.RIGHT
	var eje_x: Vector3 = direccion.cross(referencia).normalized()
	var eje_z: Vector3 = eje_x.cross(direccion).normalized()
	var base_aros := Basis(eje_x, direccion, eje_z)
	for aro in contenedor_aros_licencia.get_children():
		var fraccion: float = aro.get_meta("fraccion")
		aro.global_transform = Transform3D(base_aros, punto_origen.lerp(punto_destino, fraccion))

func _iniciar_mision(indice: int) -> void:
	if indice < 0 or indice >= mundo.nodos_misiones.size():
		return
	var datos = mundo.misiones[indice]
	var nodo_mision = mundo.nodos_misiones[indice]

	tipo_avion_option.select(_indice_tipo_helicoptero())
	_aplicar_tipo_avion(_indice_tipo_helicoptero())

	objetivo_mision = nodo_mision
	nombre_mision_actual = datos["nombre"]
	precio_mision_actual = datos["precio"]
	objetivo_guia_actual = null  # mismo motivo que en _confirmar_viaje -- forzar recaptura del origen

	panel_misiones.visible = false
	cartel_central.text = "🚁 MISIÓN: %s\nRecompensa: $%d\n\nSeguí el rumbo/flechas para llegar" % [datos["nombre"], datos["precio"]]
	cartel_central.visible = true
	get_tree().create_timer(4.0).timeout.connect(func():
		cartel_central.visible = false)

	panel_misiones.visible = false
	cartel_central.text = "🚁 MISIÓN: %s\nRecompensa: $%d" % [datos["nombre"], datos["precio"]]
	cartel_central.visible = true
	get_tree().create_timer(4.0).timeout.connect(func():
		cartel_central.visible = false)

# "Marcar lugar" (pedido 2026-09-20): NO hace falta leer nada de ningún mapa
# -- mundo.gd ya calcula lat/lon reales del avión cada cuadro (los mismos que
# usa para pedir la tesela de calles correcta), así que la coordenada exacta
# ya está ahí, gratis. Guarda en un archivo del propio proyecto que se puede
# abrir con cualquier editor de texto y va CRECIENDO con cada lugar marcado.
func _abrir_panel_marcar_lugar() -> void:
	lat_lugar_pendiente = mundo.lat_avion
	lon_lugar_pendiente = mundo.lon_avion
	alt_lugar_pendiente = mundo.altitud_avion
	etiqueta_coordenadas_marcar_lugar.text = "lat: %.6f   lon: %.6f   alt: %dm" % [
		lat_lugar_pendiente, lon_lugar_pendiente, int(alt_lugar_pendiente)]
	campo_nombre_lugar.text = ""
	panel_marcar_lugar.visible = true
	campo_nombre_lugar.grab_focus()

func _guardar_lugar_marcado() -> void:
	var nombre = campo_nombre_lugar.text.strip_edges()
	if nombre == "":
		nombre = "Sin nombre (%s)" % Time.get_datetime_string_from_system()
	# La orientación (rumbo al momento de marcar) se guarda SIEMPRE -- no
	# cuesta nada guardarla, y si más adelante el nombre pasa a decir
	# "aeropuerto" (por ejemplo, si el usuario lo renombra a mano en el
	# archivo), ya va a estar disponible.
	lugares_marcados.append({
		"nombre": nombre,
		"lat": lat_lugar_pendiente,
		"lon": lon_lugar_pendiente,
		"altitud": alt_lugar_pendiente,
		"orientacion": _ultimo_rumbo_actual,
	})
	_guardar_lugares_marcados_en_archivo()

	# Pedido 2026-09-21: si el nombre dice "aeropuerto", aparece YA MISMO en
	# el mundo (cartel + pista orientada), en su propia lista separada de las
	# marcas de misión de helicóptero -- no hace falta reiniciar el juego.
	if nombre.to_lower().find("aeropuerto") != -1:
		mundo.agregar_aeropuerto_usuario(nombre, lat_lugar_pendiente, lon_lugar_pendiente, _ultimo_rumbo_actual)

	panel_marcar_lugar.visible = false
	cartel_central.text = "📍 Lugar guardado:\n%s" % nombre
	cartel_central.visible = true
	get_tree().create_timer(3.0).timeout.connect(func():
		cartel_central.visible = false)

func _cargar_lugares_marcados() -> void:
	if not FileAccess.file_exists(RUTA_LUGARES_MARCADOS):
		return
	var archivo = FileAccess.open(RUTA_LUGARES_MARCADOS, FileAccess.READ)
	var contenido = archivo.get_as_text()
	archivo.close()
	var resultado = JSON.parse_string(contenido)
	if resultado is Array:
		lugares_marcados = resultado

func _guardar_lugares_marcados_en_archivo() -> void:
	var archivo = FileAccess.open(RUTA_LUGARES_MARCADOS, FileAccess.WRITE)
	archivo.store_string(JSON.stringify(lugares_marcados, "\t"))
	archivo.close()

# Elegís de dónde a dónde volar y confirmás: te teletransporta al origen
# elegido, orientado hacia el destino, y la Torre empieza a guiarte para allá.
func _confirmar_viaje() -> void:
	# BUG REAL encontrado 2026-10-11 (reportado: "le doy a Empezar y no pasa
	# nada"): si el desplegable "Desde" todavía no quedó seleccionado por
	# algún motivo de timing (ej. recién se creó la pestaña nueva y
	# _poblar_selector() no terminó de correr ese cuadro), esto devolvía
	# -1 y la función cortaba en silencio, sin feedback -- parecía que el
	# botón no hacía nada. En modo libre no hace falta destino, así que acá
	# alcanza con un origen válido -- si no hay ninguno elegido a mano, se
	# usa el primero de la lista en vez de no hacer nada.
	var idx_origen = origen_option.selected
	if idx_origen < 0 and not destinos.is_empty():
		idx_origen = 0
	var idx_destino = destino_option.selected
	# Modo libre (pedido explícito 2026-09-28, "que sea un tilde chiquito
	# aparte, no un ítem escondido en el desplegable de 180 aeropuertos"):
	# ahora es un checkbox propio en el panel, no un ítem más de "Hasta".
	# Pedido 2026-10-11 ("el destino que sea predeterminado vuelo libre --
	# ahora no estamos para arreglar ese desplegable, probemos aviones"):
	# por ahora modo_libre queda forzado en true siempre, sin importar el
	# checkbox -- se vuelve a conectar al destino real cuando se arregle
	# el selector de Hasta.
	var modo_libre: bool = true
	if idx_origen < 0 or (not modo_libre and (idx_destino < 0 or idx_origen == idx_destino)):
		return

	var nodo_origen = destinos[idx_origen]

	_aplicar_tipo_avion(tipo_avion_option.selected)
	# Arranca en el aire (pedido 2026-10-16: "a mil metros, como hacía siempre"), así el
	# modo tierra no se activa hasta que realmente aterrices.
	global_position = nodo_origen.global_position + _arriba_real() * ALTURA_ARRANQUE_VUELO
	if modo_libre:
		# Sin destino hacia el cual orientarse -- arranca mirando al norte
		# real (no el eje crudo del motor, mismo motivo de siempre: la
		# Tierra curva el norte real respecto a esos ejes fijos).
		look_at(global_position + mundo.norte_motor_actual, _arriba_real())
		indice_destino = -1
	else:
		var nodo_destino = destinos[idx_destino]
		# Vector3.UP otra vez NO -- mismo motivo que en _alinear_con_vertical_real
		# y _actualizar_camara(): acá se está fijando la orientación completa del
		# avión, y con el eje fijo del motor quedaría torcido/inclinado respecto
		# a la vertical real de este punto del mapa.
		look_at(nodo_destino.global_position, _arriba_real())
		indice_destino = idx_destino
	_reiniciar_objetivos_armamento()
	banco_actual = 0.0
	velocidad_actual = VELOCIDAD_INICIAL
	_actualizar_etiqueta_velocidad()
	estado = Estado.VOLANDO
	cartel_central.visible = false
	# BUG REAL encontrado 2026-09-27 (reportado: "cada vez que entro al
	# juego tengo que ir a cerrar este panel a mano"): _confirmar_viaje()
	# nunca ocultaba el panel -- quedaba tapando la pantalla hasta que se
	# volvía a apretar "Vuelo" a mano. Ahora se cierra solo al confirmar, y
	# solo vuelve a aparecer si el usuario lo pide de nuevo desde ahí.
	selector_vuelo.visible = false
	objetivo_mision = null  # un vuelo de aeropuerto normal cancela cualquier misión activa
	# BUG REAL encontrado 2026-09-21 (reportado por el usuario: "un par de
	# viajes de ayer, la línea guía no aparecía"): la línea guía solo
	# recaptura su punto de partida cuando el DESTINO cambia de nodo (ver
	# _actualizar_torre). Si volabas dos veces seguidas AL MISMO destino pero
	# desde orígenes distintos (el nodo destino es el mismo objeto de
	# siempre), nunca se detectaba el cambio, y la línea quedaba dibujada
	# desde el origen del vuelo ANTERIOR -- en otra parte del mapa,
	# completamente invisible desde donde en realidad estabas volando ahora.
	# Forzar el reseteo acá garantiza una recaptura fresca en cada vuelo
	# nuevo, sin importar si el destino se repite.
	objetivo_guia_actual = null

# SACADO 2026-09-20 (pedido explícito): el panel "TORRE: San Fernando" con
# "Ponete en rumbo X° a una altura de Y m." quedó redundante -- el rumbo
# actual ya está siempre a la vista en el panel de instrumentos, y esa
# instrucción de la torre no aportaba nada más. El rumbo digital, las
# flechitas de corrección y la distancia se quedan (siguen sirviendo);
# lo único que se fue es el cartelón de texto con la instrucción.
# Dibuja el tramo completo (línea fina, "corredor" fijo en el mundo) desde
# donde estabas cuando arrancó este destino/misión hasta el destino mismo.
# REHECHO 2026-09-21 (pedido explícito: "es como un láser en los ojos, una
# bandita elástica pegada al avión" -- antes salía siempre de la posición
# ACTUAL del avión, persiguiéndolo). Ahora los dos extremos son puntos FIJOS
# del mundo real (lat_origen_guia/lon_origen_guia, capturados una sola vez
# en _actualizar_torre) -- si te alejás a pasear, el tramo se queda quieto
# esperando en su lugar, no te sigue. SOLO lee lat/lon, nunca toca el
# cálculo de rumbo/flechas/distancia (esos siguen siendo la única fuente de
# verdad para la navegación real).
func _actualizar_linea_guia_frame() -> void:
	if ayuda_visual_activa and _hay_destino_para_linea_guia:
		_actualizar_linea_guia(_lat_destino_linea_guia, _lon_destino_linea_guia)
	else:
		linea_guia.visible = false

func _actualizar_linea_guia(lat_destino: float, lon_destino: float) -> void:
	var punto_origen: Vector3 = mundo._posicion_desde_lat_lon(lat_origen_guia, lon_origen_guia, ALTURA_LINEA_GUIA)
	var punto_destino: Vector3 = mundo._posicion_desde_lat_lon(lat_destino, lon_destino, ALTURA_LINEA_GUIA)
	var largo: float = punto_origen.distance_to(punto_destino)
	if largo < 1.0:
		linea_guia.visible = false
		return
	linea_guia.global_position = punto_origen.lerp(punto_destino, 0.5)
	linea_guia.look_at(punto_destino, _arriba_real())
	# IMPORTANTE: look_at() reconstruye la base ortonormal -- si se aplica la
	# escala ANTES, se pierde. Por eso va DESPUÉS, no antes.
	linea_guia.scale = Vector3(1, 1, largo)
	linea_guia.visible = true

# Rumbo verdadero (bearing) de un punto lat/lon a otro -- fórmula estándar
# de navegación, la misma que usa cualquier GPS. Al trabajar directo sobre
# lat/lon (no sobre coordenadas del motor) es inmune al problema de que los
# ejes del motor no coincidan con este/norte reales.
func _rumbo_verdadero_hacia(lat1_deg: float, lon1_deg: float, lat2_deg: float, lon2_deg: float) -> float:
	var lat1 = deg_to_rad(lat1_deg)
	var lat2 = deg_to_rad(lat2_deg)
	var dlon = deg_to_rad(lon2_deg - lon1_deg)
	var y = sin(dlon) * cos(lat2)
	var x = cos(lat1) * sin(lat2) - sin(lat1) * cos(lat2) * cos(dlon)
	return fposmod(rad_to_deg(atan2(y, x)), 360.0)

# Panel "Torre" (pedido 2026-09-27): recalcula cada INTERVALO_TORRE segundos
# (no todos los cuadros -- recorre TODOS los aeropuertos/pueblos cargados,
# sería carísimo hacerlo cada frame) los 3 más cercanos, con distancia real
# (misma técnica de siempre: como el origen está recentrado en el avión, la
# posición de cada nodo YA es el vector avión->aeropuerto, separamos la
# componente vertical igual que en todos lados de este archivo) y la
# flechita apuntando al rumbo relativo (rumbo verdadero hacia el aeropuerto
# menos el rumbo actual del avión).
# Pedido 2026-10-16 ("un relojito que me diga cuánta RAM libre queda, así
# si veo un crash puedo ir diagnosticando si es por baja memoria"):
# OS.get_memory_info()["free"] da la RAM libre REAL del sistema (no la que
# usa el proceso de Godot solo) -- se actualiza cada INTERVALO_RAM segundos
# (no todos los cuadros, para no estar consultando al sistema operativo
# constantemente) y cambia de color: verde con margen, amarillo bajando,
# rojo en zona de riesgo de crash por "Out of memory" (ver memoria del
# crash de Morón).
func _actualizar_etiqueta_ram(delta: float) -> void:
	if not etiqueta_ram:
		return
	_acumulador_ram += delta
	if _acumulador_ram < INTERVALO_RAM:
		return
	_acumulador_ram = 0.0
	var info: Dictionary = OS.get_memory_info()
	var libre_mb: float = float(info.get("free", 0)) / 1024.0 / 1024.0
	etiqueta_ram.text = "RAM\n%d MB" % int(round(libre_mb))
	var color: Color
	if libre_mb >= 2048.0:
		color = Color(0.2, 1.0, 0.3, 1.0)  # verde, como los demás instrumentos
	elif libre_mb >= 1024.0:
		color = Color(1.0, 0.9, 0.2, 1.0)  # amarillo, empieza a estar justo
	else:
		color = Color(1.0, 0.25, 0.2, 1.0)  # rojo, zona de riesgo real de crash
	etiqueta_ram.add_theme_color_override("font_color", color)

# Arranca una secuencia de misiones. "texto_final" es lo que dice el cartel
# al completar el último paso -- un parámetro, no una cadena de "elif"
# comparando qué secuencia es (pedido 2026-10-17: misiones comerciales
# nuevas, iba a ser un "elif" más por cada una -- mejor un dato, escala
# solo con agregar misiones a las listas de abajo, sin tocar esta función).
var _texto_final_mision_actual: String = "¡MISIÓN COMPLETA!"
# Pedido 2026-10-17 ("cada misión tiene un precio en dólares, y según qué
# avión uses te da más o menos plata -- Boeing más, Cessna menos"):
# "pago_base" se multiplica acá mismo al arrancar la misión (no al
# terminarla) para que el cartel de bienvenida ya pueda avisar cuánto va a
# pagar con el avión actual.
var mision_pago_pendiente: int = 0
func _iniciar_secuencia_mision(secuencia: Array, texto_final: String = "¡MISIÓN COMPLETA!", pago_base: int = 0) -> void:
	secuencia_mision_actual = secuencia
	mision_paso_actual = 0
	_texto_final_mision_actual = texto_final
	mision_pago_pendiente = int(round(pago_base * _multiplicador_precio_avion()))
	if secuencia == SECUENCIA_NIVEL_1:
		mision_barcos_base = barcos_destruidos
	elif secuencia == SECUENCIA_NIVEL_3:
		mision_invasores_base = invasores_destruidos
	_mostrar_paso_mision_actual()

# Pedido 2026-10-17 ("en un Boeing que tiene más capacidad, la misión te da
# más dólares, en un Cessna menos"): multiplicador simple por nombre del
# avión actual -- no hay datos reales de capacidad de pasajeros todavía,
# así que se agrupa a ojo por tamaño/categoría visible. Fácil de afinar
# después con números reales por avión.
func _multiplicador_precio_avion() -> float:
	var nombre: String = String(TIPOS_AVION[tipo_avion_indice]["nombre"]).to_lower()
	if nombre.contains("boeing") or nombre.contains("a380") or nombre.contains("777"):
		return 3.0
	elif nombre.contains("jet privado") or nombre.contains("pilatus") or nombre.contains("miljet"):
		return 1.5
	elif nombre.contains("cessna") or nombre.contains("piper"):
		return 1.0
	return 1.2  # resto de los aviones (militares, genéricos, etc.) -- valor intermedio

func _mostrar_paso_mision_actual() -> void:
	if mision_paso_actual < 0 or mision_paso_actual >= secuencia_mision_actual.size():
		return
	var paso: Dictionary = secuencia_mision_actual[mision_paso_actual]
	_mostrar_cartel_aterrizaje(paso["texto"], Color(1.0, 0.9, 0.3))
	if etiqueta_objetivo_mision:
		etiqueta_objetivo_mision.text = "OBJETIVO: " + paso["nombre"]
		etiqueta_objetivo_mision.visible = true
	# Al cambiar de paso, re-sincronizar YA (no esperar los 0.5s del
	# throttle normal) -- que el cuadrado nuevo aparezca y el viejo
	# desaparezca en el mismo instante que cambia el cartel.
	_sincronizar_modo_guerra(0.0, true)
	_actualizar_contadores_mision_visibles()
	_actualizar_marcador_destino_mision(paso)
	_actualizar_guia_de_paso_mision(paso)

# Pedido 2026-10-17 ("activá el rumbo/las flechas, me perdí yendo a
# Rosario"): engancha "objetivo_mision" (el sistema de guía de rumbo/
# flechas/distancia/línea que YA FUNCIONA para las misiones de
# helicóptero) al paso actual, si es de tipo "llegar_aeropuerto" (reusa el
# nodo real del aeropuerto, ya tiene lat/lon) o "llegar_punto" (arma un
# marcador invisible temporal con la lat/lon exacta -- típicamente un
# hangar trackeado con M). Si el paso no es de ninguno de los dos tipos
# (una misión de guerra, por ejemplo), limpia el objetivo SOLO si era
# nuestro, para no pisar una misión de helicóptero que esté en curso.
func _actualizar_guia_de_paso_mision(paso: Dictionary) -> void:
	if objetivo_mision_de_secuencia and is_instance_valid(objetivo_mision):
		objetivo_mision.queue_free()
	objetivo_mision_de_secuencia = false
	var tipo: String = paso.get("tipo", "")
	if tipo == "llegar_aeropuerto":
		var aeropuerto := _buscar_destino_por_nombre(paso["destino"])
		if aeropuerto:
			objetivo_mision = aeropuerto
			nombre_mision_actual = paso["destino"]
			objetivo_mision_de_secuencia = false  # nodo real del mundo, no lo creamos ni lo borramos nosotros
			objetivo_guia_actual = null
			return
	elif tipo == "llegar_punto":
		var marcador := Node3D.new()
		marcador.name = "ObjetivoGuiaMision"
		var arriba: Vector3 = _arriba_real()
		marcador.global_position = mundo._posicion_desde_lat_lon(float(paso["lat"]), float(paso["lon"]), 0.0) + arriba * 100.0
		marcador.set_meta("lat", paso["lat"])
		marcador.set_meta("lon", paso["lon"])
		mundo.add_child(marcador)
		objetivo_mision = marcador
		nombre_mision_actual = paso["nombre"]
		objetivo_mision_de_secuencia = true
		objetivo_guia_actual = null
		return
	objetivo_mision = null

# TERCERA VUELTA 2026-10-17 (reportado: "esto ni siquiera se ve" -- ni el
# cartel bobeando ni el anillo de luces chicas funcionaron): esta vez se
# reusa el método que SÍ se probó y anduvo bien hoy mismo (la columna de
# luz + letra gigante de la prueba de los tanques) -- una columna fucsia
# de 1500m de alto, gruesa, imposible de no ver desde cualquier ángulo, más
# una "H" enorme flotando arriba. Fijo en tamaño real (NO se achica con la
# distancia como el anillo anterior -- a 1500m de alto, no hace falta).
var marcador_destino_mision: Node3D = null
func _actualizar_marcador_destino_mision(paso: Dictionary) -> void:
	if marcador_destino_mision and is_instance_valid(marcador_destino_mision):
		marcador_destino_mision.queue_free()
		marcador_destino_mision = null
	if paso.get("tipo", "") != "llegar_punto":
		return
	var arriba: Vector3 = _arriba_real()
	var punto: Vector3 = mundo._posicion_desde_lat_lon(float(paso["lat"]), float(paso["lon"]), 0.0)
	marcador_destino_mision = Node3D.new()
	marcador_destino_mision.name = "MarcadorDestinoMision"
	mundo.add_child(marcador_destino_mision)
	marcador_destino_mision.set_meta("latitud_objetivo", float(paso["lat"]))
	marcador_destino_mision.set_meta("longitud_objetivo", float(paso["lon"]))
	marcador_destino_mision.global_position = punto

	var pilar := MeshInstance3D.new()
	var malla := CylinderMesh.new()
	malla.height = 1500.0
	malla.top_radius = 20.0
	malla.bottom_radius = 20.0
	pilar.mesh = malla
	var material_pilar := StandardMaterial3D.new()
	material_pilar.albedo_color = Color(0.1, 1.0, 0.3, 1.0)
	material_pilar.emission_enabled = true
	material_pilar.emission = Color(0.1, 1.0, 0.3)
	material_pilar.emission_energy_multiplier = 5.0
	material_pilar.shading_mode = BaseMaterial3D.SHADING_MODE_UNSHADED
	pilar.material_override = material_pilar
	pilar.position = arriba * 750.0  # el cilindro queda centrado en su propio origen -- esto lo deja con la base al nivel del suelo
	marcador_destino_mision.add_child(pilar)
	marcador_destino_mision.set_meta("material_pilar", material_pilar)

	var etiqueta := Label3D.new()
	etiqueta.text = "⬇ HANGAR ⬇"
	etiqueta.font_size = 700
	etiqueta.outline_size = 70
	etiqueta.modulate = Color(0.1, 1.0, 0.3, 1.0)
	etiqueta.billboard = BaseMaterial3D.BILLBOARD_ENABLED
	etiqueta.no_depth_test = true
	etiqueta.position = arriba * 1600.0
	marcador_destino_mision.add_child(etiqueta)
	marcador_destino_mision.set_meta("etiqueta_marcador", etiqueta)

# Llamado cada cuadro desde _actualizar_hud -- reposiciona (el origen se
# recentra en el avión todo el tiempo) y hace parpadear la columna/letra.
var _tiempo_animacion_marcador: float = 0.0
func _actualizar_animacion_marcador_destino(delta: float) -> void:
	# BUG REAL encontrado 2026-10-17 (reportado: "el instrumento de arriba
	# decía 563 kilómetros cuando ya había aterrizado en Río Cuarto, pero la
	# pestaña Torre mostraba la distancia bien"): el marcador invisible que
	# usa _actualizar_torre para medir distancia a un "llegar_punto" (ver
	# _actualizar_guia_de_paso_mision) se posiciona UNA sola vez, al arrancar
	# el paso -- pero el origen del mundo se recentra en el avión TODO EL
	# TIEMPO (mundo.gd), así que su global_position queda vieja/corrida
	# después de volar lejos. Los aeropuertos reales no tienen este problema
	# porque mundo.gd los reubica solo en cada recentrado -- este marcador
	# ad-hoc no está en esa lista, así que lo recalculamos a mano, cada
	# cuadro, igual que ya hacíamos con marcador_destino_mision (la columna
	# de luz) un poco más abajo en esta misma función.
	if objetivo_mision_de_secuencia and is_instance_valid(objetivo_mision) and objetivo_mision.has_meta("lat") and objetivo_mision.has_meta("lon"):
		objetivo_mision.global_position = mundo._posicion_desde_lat_lon(
			float(objetivo_mision.get_meta("lat")), float(objetivo_mision.get_meta("lon")), 0.0) + _arriba_real() * 100.0

	if not marcador_destino_mision or not is_instance_valid(marcador_destino_mision):
		return
	_tiempo_animacion_marcador += delta
	var arriba: Vector3 = _arriba_real()
	var lat: float = marcador_destino_mision.get_meta("latitud_objetivo")
	var lon: float = marcador_destino_mision.get_meta("longitud_objetivo")
	marcador_destino_mision.global_position = mundo._posicion_desde_lat_lon(lat, lon, 0.0)
	var destello: float = 0.3 + 0.7 * abs(sin(_tiempo_animacion_marcador * 2.5))  # prende y apaga
	var material_pilar: StandardMaterial3D = marcador_destino_mision.get_meta("material_pilar")
	if material_pilar:
		material_pilar.emission_energy_multiplier = lerp(2.0, 9.0, destello)
	var etiqueta_marcador: Label3D = marcador_destino_mision.get_meta("etiqueta_marcador")
	if is_instance_valid(etiqueta_marcador):
		etiqueta_marcador.modulate.a = 0.4 + 0.6 * destello

# BUG REAL encontrado 2026-10-17 (reportado: "en Defensa Total solo aparecen
# los barcos, no sé cuántos aeropuertos o Capital me faltan"): los
# contadores de arriba a la derecha (CAPITAL/AEROPUERTOS/BARCOS/INDUSTRIA)
# antes solo se hacían visibles la PRIMERA VEZ que le pegabas a algo de ese
# tipo -- si una misión junta varios grupos (como Defensa Total) y todavía
# no le diste a, por ejemplo, ningún aeropuerto, ese contador seguía oculto
# aunque los blancos ya estuvieran activos y a la vista. Ahora se muestran
# (con el número real ya puesto, no en 0 a ciegas) apenas arranca el paso
# de misión que los necesita, y se ocultan los que no corresponden.
func _actualizar_contadores_mision_visibles() -> void:
	var grupos: Array = _grupos_relevantes_paso_actual()
	if etiqueta_mision_verde:
		etiqueta_mision_verde.visible = grupos.has("mision_verde")
		if grupos.has("mision_verde"):
			etiqueta_mision_verde.text = "CAPITAL: %d/10" % mision_verde_destruidos
	if etiqueta_objetivo_destruido:
		etiqueta_objetivo_destruido.visible = grupos.has("mision_aeropuertos")
		if grupos.has("mision_aeropuertos"):
			etiqueta_objetivo_destruido.text = "AEROPUERTOS: %d/5" % objetivos_destruidos
	if etiqueta_barcos:
		etiqueta_barcos.visible = grupos.has("mision_barcos")
		if grupos.has("mision_barcos"):
			etiqueta_barcos.text = "BARCOS: %d/%d" % [barcos_destruidos, BARCOS_RIO_AEROPARQUE.size()]
	if etiqueta_industria:
		etiqueta_industria.visible = grupos.has("mision_industria")
		if grupos.has("mision_industria"):
			etiqueta_industria.text = "INDUSTRIA: %d/%d" % [industria_destruidos, MISION_INDUSTRIA.size()]
	if etiqueta_invasion:
		etiqueta_invasion.visible = grupos.has("invasion")

# El "chequeo" va por nombre del paso (o por "tipo", para las misiones
# civiles genéricas) -- más simple de leer y de agregar pasos nuevos.
# Pedido 2026-10-17 ("misiones comerciales, de un aeropuerto a otro, sin
# disparar"): "tipo": "llegar_aeropuerto" / "llegar_punto" sirven para
# CUALQUIER misión civil nueva sin escribir un "elif" por cada una -- el
# propio paso trae el destino (nombre de aeropuerto, o lat/lon de un lugar
# marcado con M, como los hangares).
func _paso_mision_actual_completo() -> bool:
	if mision_paso_actual < 0 or mision_paso_actual >= secuencia_mision_actual.size():
		return false
	var paso: Dictionary = secuencia_mision_actual[mision_paso_actual]
	var tipo: String = paso.get("tipo", "")
	if tipo == "llegar_aeropuerto":
		var aeropuerto := _buscar_destino_por_nombre(paso["destino"])
		if not aeropuerto:
			return false
		return global_position.distance_to(aeropuerto.global_position) < float(paso.get("radio", RADIO_VOLVER_BASE))
	elif tipo == "llegar_punto":
		# BUG REAL encontrado 2026-10-17 (reportado: "ni bien aterrizo ya me
		# da la plata por ganada, si dice ir al hangar hay que llegar al
		# hangar de verdad"): antes solo medía distancia -- ahora, además de
		# estar cerca, exige estar REALMENTE en tierra (en_tierra == true),
		# no solo sobrevolando la zona.
		var punto: Vector3 = mundo._posicion_desde_lat_lon(float(paso["lat"]), float(paso["lon"]), 0.0)
		return en_tierra and global_position.distance_to(punto) < float(paso.get("radio", 150.0))
	var nombre: String = paso["nombre"]
	if nombre == "Capital Federal":
		return mision_verde_destruidos >= 10
	elif nombre == "Aeropuertos":
		return objetivos_destruidos >= 5
	elif nombre == "Hundir 3 barcos":
		return barcos_destruidos - mision_barcos_base >= 3
	elif nombre == "Volver a base":
		var aeropuerto := _buscar_destino_por_nombre("Aeroparque")
		if not aeropuerto:
			return false
		return global_position.distance_to(aeropuerto.global_position) < RADIO_VOLVER_BASE
	elif nombre == "Industria":
		return industria_destruidos >= MISION_INDUSTRIA.size()
	elif nombre == "Invasores":
		return invasores_destruidos - mision_invasores_base >= 10
	elif nombre == "Defensa Total":
		return mision_verde_destruidos >= 10 and objetivos_destruidos >= 5 and barcos_destruidos >= BARCOS_RIO_AEROPARQUE.size() and industria_destruidos >= MISION_INDUSTRIA.size()
	return false

func _actualizar_secuencia_mision(delta: float) -> void:
	if mision_paso_actual < 0:
		return
	if _paso_mision_actual_completo():
		mision_paso_actual += 1
		if mision_paso_actual >= secuencia_mision_actual.size():
			var texto_pago: String = ""
			if mision_pago_pendiente > 0:
				PerfilJugador.modificar_plata(mision_pago_pendiente)
				_actualizar_etiqueta_dinero()
				texto_pago = "\n+$%d" % mision_pago_pendiente
				mision_pago_pendiente = 0
			_mostrar_cartel_aterrizaje(_texto_final_mision_actual + texto_pago, Color(0.3, 1.0, 0.4))
			if etiqueta_objetivo_mision:
				etiqueta_objetivo_mision.visible = false
			if marcador_destino_mision and is_instance_valid(marcador_destino_mision):
				marcador_destino_mision.queue_free()
				marcador_destino_mision = null
			if objetivo_mision_de_secuencia and is_instance_valid(objetivo_mision):
				objetivo_mision.queue_free()
			objetivo_mision = null
			objetivo_mision_de_secuencia = false
			mision_paso_actual = -1
		else:
			_mostrar_paso_mision_actual()

func _actualizar_panel_torre(delta: float) -> void:
	_acumulador_torre += delta
	if _acumulador_torre < INTERVALO_TORRE:
		return
	_acumulador_torre = 0.0

	# Pedido explícito 2026-09-27 ("si tiene dos pistas, que aparezca pista 1
	# y pista 2 por separado, cada una con su propio ILS individual"):
	# Ezeiza (y cualquier otro aeropuerto que algún día se sume así) tiene
	# DOS entradas de ILS reales ("Ezeiza Pista 1"/"Pista 2"), cada una con
	# su propia posición -- se listan como candidatos SEPARADOS, usando el
	# punto medio entre sus dos cabeceras. El nodo del aeropuerto "de
	# siempre" (nodos_aeropuertos, un solo punto por lugar) se salta cuando
	# ya está representado por al menos una de esas entradas, para no
	# duplicar "Ezeiza" tres veces en la lista.
	var arriba_real: Vector3 = _arriba_real()
	var candidatos: Array = []
	var nombres_desde_ils: Array = []
	for entrada in mundo.contenedor_ils_dos_cabeceras:
		var lat_medio: float = (entrada["cab1_lat"] + entrada["cab2_lat"]) * 0.5
		var lon_medio: float = (entrada["cab1_lon"] + entrada["cab2_lon"]) * 0.5
		var p1: Vector3 = mundo._posicion_desde_lat_lon(entrada["cab1_lat"], entrada["cab1_lon"], entrada.get("cab1_alt", 8.0))
		var p2: Vector3 = mundo._posicion_desde_lat_lon(entrada["cab2_lat"], entrada["cab2_lon"], entrada.get("cab2_alt", 8.0))
		var medio: Vector3 = (p1 + p2) * 0.5
		var horizontal: Vector3 = medio - medio.dot(arriba_real) * arriba_real
		candidatos.append({"nombre": entrada["nombre"], "lat": lat_medio, "lon": lon_medio, "distancia": horizontal.length()})
		nombres_desde_ils.append(entrada["nombre"])

	for nodo in mundo.obtener_destinos():
		if not (nodo.has_meta("lat") and nodo.has_meta("lon")):
			continue
		var nombre_nodo: String = nodo.get_meta("nombre_bonito", nodo.name)
		var ya_representado := false
		for n in nombres_desde_ils:
			if n == nombre_nodo or n.begins_with(nombre_nodo + " "):
				ya_representado = true
				break
		if ya_representado:
			continue
		var horizontal_nodo: Vector3 = nodo.global_position - nodo.global_position.dot(arriba_real) * arriba_real
		candidatos.append({"nombre": nombre_nodo, "lat": nodo.get_meta("lat"), "lon": nodo.get_meta("lon"), "distancia": horizontal_nodo.length()})
	candidatos.sort_custom(func(a, b): return a["distancia"] < b["distancia"])

	for i in range(_filas_torre.size()):
		var fila: Dictionary = _filas_torre[i]
		if i >= candidatos.size():
			fila["nombre"].text = "---"
			fila["distancia"].text = "--"
			fila["flecha"].visible = false
			fila["boton"].disabled = true
			fila["etiqueta_precio"].text = "--"
			fila["boton_ir"].disabled = true
			continue
		var candidato: Dictionary = candidatos[i]
		var nombre: String = candidato["nombre"]
		var rumbo_hacia: float = _rumbo_verdadero_hacia(mundo.lat_avion, mundo.lon_avion, candidato["lat"], candidato["lon"])
		var relativo: float = fposmod(rumbo_hacia - _ultimo_rumbo_actual, 360.0)

		fila["nombre"].text = nombre
		fila["distancia"].text = "%.1f km" % (candidato["distancia"] / 1000.0)
		# Pedido explícito 2026-10-03 ("las flechitas amarillas se van de
		# lugar, por ahora sacalas"): quedan ocultas, solo texto.
		fila["flecha"].visible = false
		fila["flecha"].rotation_degrees = relativo
		fila["boton"].set_meta("nombre_aeropuerto", nombre)
		fila["boton"].set_meta("lat_aeropuerto", candidato["lat"])
		fila["boton"].set_meta("lon_aeropuerto", candidato["lon"])

		# Precio editable por distancia (ver _precios_torre_override arriba)
		# + botón "Ir" -- guardamos lat/lon/precio como metadata del botón,
		# mismo truco que ya usa BotonIlsTorre, para leerlos frescos al
		# apretar sin importar en qué índice haya quedado esta fila.
		if not _precios_torre_override.has(nombre):
			_precios_torre_override[nombre] = max(20.0, round(candidato["distancia"] / 1000.0 * TARIFA_TORRE_POR_KM / 5.0) * 5.0)
		var precio_torre: float = _precios_torre_override[nombre]
		fila["etiqueta_precio"].text = "$%d" % int(precio_torre)
		fila["boton_ir"].disabled = false
		fila["boton_ir"].set_meta("nombre_aeropuerto", nombre)
		fila["boton_ir"].set_meta("lat_aeropuerto", candidato["lat"])
		fila["boton_ir"].set_meta("lon_aeropuerto", candidato["lon"])
		fila["boton_ir"].set_meta("precio_aeropuerto", precio_torre)
		if not mundo.aeropuerto_tiene_ils(nombre):
			# Sin datos de ILS cargados para este -- deshabilitado en vez de
			# dejarlo "prenderse" un instante y apagarse solo (ver comentario
			# en aeropuerto_tiene_ils, mundo.gd).
			fila["boton"].disabled = true
			fila["boton"].text = "Sin ILS"
			fila["boton"].modulate = Color(0.5, 0.5, 0.5, 1.0)
		else:
			fila["boton"].disabled = false
			var ils_activo: bool = mundo.ils_activo_en_aeropuerto(nombre)
			fila["boton"].text = "ILS ON" if ils_activo else "ILS"
			fila["boton"].modulate = Color(0.5, 1.0, 0.5, 1.0) if ils_activo else Color(1, 1, 1, 1)

func _alternar_ils_torre(indice: int) -> void:
	var boton: Button = _filas_torre[indice]["boton"]
	var nombre: String = boton.get_meta("nombre_aeropuerto", "")
	if nombre == "":
		return
	var activo: bool = mundo.ils_activo_en_aeropuerto(nombre)
	mundo.alternar_ils_aeropuerto(nombre, not activo)
	# Refresco inmediato del botón (no esperar los 2s del próximo ciclo).
	boton.text = "ILS" if activo else "ILS ON"
	boton.modulate = Color(1, 1, 1, 1) if activo else Color(0.5, 1.0, 0.5, 1.0)

	if activo:
		# Se estaba apagando este mismo -- si era el que tenía el rumbo
		# tomado, soltarlo y volver al plan de vuelo de siempre.
		if rumbo_guia_override_nombre == nombre:
			rumbo_guia_override_activo = false
			rumbo_guia_override_nombre = ""
	else:
		# Se está prendiendo -- este pasa a ser el que manda en el rumbo
		# sugerido de arriba.
		rumbo_guia_override_activo = true
		rumbo_guia_override_nombre = nombre
		rumbo_guia_override_lat = boton.get_meta("lat_aeropuerto", 0.0)
		rumbo_guia_override_lon = boton.get_meta("lon_aeropuerto", 0.0)

# Pedido 2026-10-17 ("un botoncito que diga Ir, y arriba un precio con
# más/menos"): ajusta el precio GUARDADO por nombre de aeropuerto (ver
# _precios_torre_override) -- el índice de fila puede no corresponder más
# al mismo aeropuerto en el próximo refresco (la lista se reordena por
# distancia), por eso se guarda por nombre y no por índice.
func _ajustar_precio_torre(indice: int, delta: int) -> void:
	if indice < 0 or indice >= _filas_torre.size():
		return
	var boton: Button = _filas_torre[indice]["boton_ir"]
	var nombre: String = boton.get_meta("nombre_aeropuerto", "")
	if nombre == "" or not _precios_torre_override.has(nombre):
		return
	_precios_torre_override[nombre] = max(0.0, _precios_torre_override[nombre] + delta)
	_filas_torre[indice]["etiqueta_precio"].text = "$%d" % int(_precios_torre_override[nombre])
	boton.set_meta("precio_aeropuerto", _precios_torre_override[nombre])

# Pedido 2026-10-17 ("encadenar viajes -- de acá a Córdoba, de Córdoba a La
# Rioja, como ir parando el colectivo, hasta Brasil si quisiéramos"): arma
# el viaje SIN teletransportar (sale de donde estés volando ahora mismo,
# como pedido explícito), apuntando al aeropuerto real -- mismo mecanismo
# de guía/paso de misión que ya usan los Destinos Nacionales, así que
# también paga solo al tocar pista de verdad (en_tierra), sin hangar.
func _ir_a_aeropuerto_torre(indice: int) -> void:
	if indice < 0 or indice >= _filas_torre.size():
		return
	var boton: Button = _filas_torre[indice]["boton_ir"]
	var nombre: String = boton.get_meta("nombre_aeropuerto", "")
	if nombre == "":
		return
	var lat_destino: float = boton.get_meta("lat_aeropuerto", 0.0)
	var lon_destino: float = boton.get_meta("lon_aeropuerto", 0.0)
	var precio: int = int(boton.get_meta("precio_aeropuerto", 0.0))
	_mostrar_cartel_aterrizaje("VIAJE: %s\nSalís desde donde estás. Pago: $%d" % [nombre, int(round(float(precio) * _multiplicador_precio_avion()))], Color(0.6, 0.85, 1.0))
	# Radio generoso (ver mismo comentario en _iniciar_destino_nacional):
	# muchos aeropuertos solo tienen una coordenada aproximada, no la pista
	# real medida -- 600m da margen para no fallar el pago por eso.
	var paso := [{"nombre": "Volar a " + nombre, "texto": "Destino: " + nombre + ".", "tipo": "llegar_punto", "lat": lat_destino, "lon": lon_destino, "radio": RADIO_VOLVER_BASE}]
	_iniciar_secuencia_mision(paso, "¡Llegaste a %s!" % nombre, precio)

func _actualizar_torre(altura: float) -> void:
	# BUG REAL encontrado 2026-09-20 (el "reloj" del RUMBO, sospechado
	# correctamente por el usuario): esto asumía que el eje X del motor ES
	# el este real y -Z ES el norte real -- válido solo CERCA del origen
	# inicial (Aeroparque). A medida que te alejás, la curvatura de la
	# Tierra hace que el este/norte VERDADEROS giren respecto a esos ejes
	# fijos -- por eso el rumbo arrancaba bien y se iba desviando cada vez
	# más (mismo fenómeno que ya nos había roto la altitud, nunca corregido
	# acá). Ahora se proyecta la nariz del avión sobre el este/norte REALES
	# de la posición actual (calculados en mundo.gd, los mismos que usa el
	# recentrado de origen), no sobre los ejes crudos del motor.
	var adelante = -global_transform.basis.z
	var rumbo_actual = fposmod(rad_to_deg(atan2(
		adelante.dot(mundo.este_motor_actual),
		adelante.dot(mundo.norte_motor_actual))), 360.0)
	# El rumbo actual se muestra SIEMPRE, haya o no destino activo -- antes
	# se vaciaba (quedaba en blanco) apenas llegabas/frenabas/parabas, lo
	# cual rompía la idea de "panel de instrumentos siempre encendido".
	etiqueta_rumbo.text = "RUMBO\n%d°" % int(round(rumbo_actual))
	_ultimo_rumbo_actual = rumbo_actual

	# Unifica dos "objetivos" posibles: una misión de helicóptero activa
	# (prioridad) o, si no hay ninguna, el destino de aeropuerto de siempre.
	var destino_actual: Node3D = null
	var nombre_destino: String = ""
	if objetivo_mision:
		destino_actual = objetivo_mision
		nombre_destino = nombre_mision_actual
	elif not destinos.is_empty() and indice_destino >= 0 and indice_destino < destinos.size():
		destino_actual = destinos[indice_destino]
		nombre_destino = destino_actual.get_meta("nombre_bonito", destino_actual.name)

	# Pedido 2026-09-21: la línea guía tiene que quedar FIJA en el mundo real
	# (de dónde arrancaste el tramo actual hasta el destino), no perseguir al
	# avión cada cuadro -- por eso solo capturamos el punto de partida UNA
	# VEZ, cuando cambia el destino/misión activo (no en cada cuadro).
	if destino_actual != objetivo_guia_actual:
		objetivo_guia_actual = destino_actual
		if destino_actual != null:
			lat_origen_guia = mundo.lat_avion
			lon_origen_guia = mundo.lon_avion

	if destino_actual == null:
		etiqueta_distancia.text = "DIST\n---"
		_ocultar_flechas()
		# Pedido explícito 2026-09-27 (Modo libre): sin destino, normalmente
		# no hay nada que sugerir -- PERO si el usuario activó el ILS de un
		# aeropuerto puntual desde el panel Torre, ese rumbo tiene que
		# aparecer igual (es el único caso en el que Modo libre sí muestra
		# un rumbo sugerido).
		if rumbo_guia_override_activo:
			var rumbo_ils: float = _rumbo_verdadero_hacia(mundo.lat_avion, mundo.lon_avion, rumbo_guia_override_lat, rumbo_guia_override_lon)
			etiqueta_rumbo_objetivo.text = "Ponete en rumbo %d°" % int(round(rumbo_ils))
			etiqueta_rumbo_objetivo.visible = true
		return

	var centro_destino = destino_actual.global_position
	var hacia_centro = centro_destino - global_position
	# IMPORTANTE: antes se usaba Vector2(hacia_centro.x, hacia_centro.z), que
	# asume que X/Z del motor son "el plano horizontal" -- ya no es cierto
	# (el avión está inclinado ~34.5° respecto a esos ejes fijos). Restamos
	# la componente de la vertical real para obtener la distancia horizontal
	# de verdad.
	var arriba_real: Vector3 = _arriba_real()
	var hacia_centro_horizontal: Vector3 = hacia_centro - hacia_centro.dot(arriba_real) * arriba_real
	var distancia = hacia_centro_horizontal.length()
	etiqueta_distancia.text = "DIST\n%dm" % int(distancia)

	# Misión de licencia de Modo Carrera (pedido 2026-09-21): a diferencia de
	# las misiones de sobrevuelo/aterrizaje de siempre (que se aterrizan a
	# mano, sin punto exacto de "llegada"), acá SÍ queremos que se complete
	# sola al llegar -- es un examen de "punto A a punto B", no un aterrizaje
	# de precisión.
	if mision_licencia_indice >= 0 and estado == Estado.VOLANDO and distancia < UMBRAL_LLEGADA_LICENCIA:
		_completar_mision_licencia()
		return

	# El "LLEGADA/frenar con ESPACIO" es del sistema de aeropuertos (pista fija,
	# tenés que parar en seco). Una misión de helicóptero se aterriza a mano,
	# donde el jugador decida -- no hay un punto exacto de "llegada" que
	# disparar solo.
	if objetivo_mision == null:
		# AGRANDADO (pedido 2026-09-20): el usuario aterrizó de verdad en la
		# pista real (imágenes de Google) y "LLEGADA" no se disparó -- nuestro
		# marcador propio del aeropuerto es UN punto lat/lon puesto a mano, que
		# no tiene por qué coincidir exactamente con dónde está la pista real
		# dentro del predio (los aeropuertos reales son áreas grandes). 40m era
		# demasiado exigente para esa diferencia.
		# BUG REAL encontrado 2026-10-10 (reportado: "cartel gigante 'ya
		# llegaste a los parques' me persigue por todo el juego, tuve que
		# frenar con ESPACIO y volver a despegar"): 400m/30m era generoso
		# porque antes se volaba casi siempre a crucero hacia UN destino
		# elegido. Ahora que se vuela bajo cazando blancos de industria por
		# zonas con aeropuertos/destinos cerca (Quilmes-La Plata), es fácil
		# pasar sin querer cerca del destino por defecto (índice 1, no uno
		# elegido a propósito) y disparar una "llegada" falsa. Achicado a
		# 150m/15m -- sigue alcanzando para un aterrizaje real, pero ya no
		# salta por sobrevolar la zona en general.
		if estado == Estado.VOLANDO and distancia < 150.0 and altura < 15.0:
			estado = Estado.LLEGADA
			cartel_central.text = "✅ ¡LLEGASTE A %s!\n\nApretá ESPACIO para frenar" % nombre_destino.to_upper()
			cartel_central.visible = true
			if modo_city_tour and nombre_destino == "Aeroparque":
				_completar_city_tour()

	if estado != Estado.VOLANDO and estado != Estado.LLEGADA:
		_ocultar_flechas()
		return

	# BUG REAL encontrado 2026-09-20 (reportado por el usuario: "quise ir a
	# Parque Centenario siguiendo las flechas y me llevó al Río de la Plata"):
	# esto calculaba el rumbo objetivo con atan2(x, -z) sobre coordenadas
	# CRUDAS del motor -- exactamente el mismo error que ya habíamos
	# encontrado y corregido en otro lado de este proyecto (los ejes del
	# motor NO son este/norte reales, están inclinados ~34.5° en Buenos
	# Aires). Ahora se calcula el rumbo VERDADERO con la fórmula estándar de
	# navegación (great-circle bearing) directo sobre lat/lon reales -- sin
	# pasar por los ejes del motor para nada, así que este error no puede
	# volver a pasar más.
	var rumbo_objetivo = rumbo_actual
	# Pedido explícito 2026-09-27: al activar el ILS individual de un
	# aeropuerto puntual desde el panel Torre, el rumbo que se sugiere arriba
	# ("Ponete en rumbo X°") tiene que apuntar a ESE aeropuerto, ignorando el
	# plan de vuelo Origen/Destino elegido -- así uno se guía por el ILS que
	# realmente está usando, no por un destino que quedó seleccionado antes.
	# Solo pisa el NÚMERO de rumbo; la distancia/línea guía siguen atadas al
	# plan de vuelo de siempre (eso no es lo que pidió cambiar).
	if rumbo_guia_override_activo or (destino_actual.has_meta("lat") and destino_actual.has_meta("lon")):
		var lat_destino: float = rumbo_guia_override_lat if rumbo_guia_override_activo else destino_actual.get_meta("lat")
		var lon_destino: float = rumbo_guia_override_lon if rumbo_guia_override_activo else destino_actual.get_meta("lon")
		rumbo_objetivo = _rumbo_verdadero_hacia(
			mundo.lat_avion, mundo.lon_avion, lat_destino, lon_destino)
		# DEBUG TEMPORAL (vuelta 3) -- ya no se imprime solo (inundaba la
		# consola, imposible de copiar). Se guarda la última foto de estos
		# números en una variable, y se escribe a un archivo SOLO cuando se
		# aprieta la tecla N (ver _process) -- una línea por apretada, nada más.
		# Sugerencia de Gemini: comparar la distancia ECEF CRUDA (metros
		# reales, sin pasar por ninguna rotación/proyección) contra la
		# distancia ya calculada en espacio de Godot -- si difieren mucho, el
		# problema está en get_tx_ecef_to_engine()/la proyección; si son
		# iguales, el problema está en otro lado (el rumbo o los datos).
		var ecef_destino = mundo._lat_lon_alt_a_ecef_xyz(lat_destino, lon_destino, 5.0)
		var ecef_origen_actual = Vector3(mundo.georeferencia.get_ecef_x(), mundo.georeferencia.get_ecef_y(), mundo.georeferencia.get_ecef_z())
		var dist_ecef_cruda = ecef_origen_actual.distance_to(Vector3(ecef_destino[0], ecef_destino[1], ecef_destino[2]))
		# Registro de ILS acá también (pedido 2026-09-21, "no funciona nada en
		# El Palomar", "los amarillos de Moreno tampoco"): no encontré el bug
		# mirando el código, así que dejamos esto para tener datos reales de
		# la próxima apretada de N, en vez de seguir adivinando -- mismo
		# método que resolvió el bug del rumbo en su momento.
		var tiene_ils_este_destino: bool = destino_actual.has_meta("orientacion_usuario")
		var cuantos_ils_registrados: int = mundo.contenedores_ils_por_aeropuerto.size()
		var visible_positivo = "?"
		var visible_negativo = "?"
		for par_ils in mundo.contenedores_ils_por_aeropuerto:
			if par_ils["nodo"] == destino_actual:
				visible_positivo = str(par_ils["positivo"].visible)
				visible_negativo = str(par_ils["negativo"].visible)
				break
		# Lo mismo para el sistema NUEVO de "dos cabeceras" (El Palomar, Morón,
		# Ezeiza x2) -- reportado que NINGUNO de estos funciona mientras que
		# Mariano Moreno (el sistema viejo, de un solo rumbo) sí. Volcamos acá
		# el estado de CADA entrada: si el contenedor está visible, y dónde
		# calculó las dos cabeceras y un aro de muestra este mismo cuadro --
		# así vemos si el problema es que no se activan, o que se activan pero
		# quedan mal ubicadas.
		var texto_dos_cabeceras: String = ""
		for entrada_ils in mundo.contenedor_ils_dos_cabeceras:
			var p1_debug: Vector3 = mundo._posicion_desde_lat_lon(entrada_ils["cab1_lat"], entrada_ils["cab1_lon"], entrada_ils["cab1_alt"])
			var p2_debug: Vector3 = mundo._posicion_desde_lat_lon(entrada_ils["cab2_lat"], entrada_ils["cab2_lon"], entrada_ils["cab2_alt"])
			var hijos_debug: Array = entrada_ils["contenedor"].get_children()
			var pos_primer_aro: String = str(hijos_debug[0].global_position) if hijos_debug.size() > 0 else "sin_hijos"
			texto_dos_cabeceras += " [%s visible:%s p1:%s p2:%s largo:%dm cantHijos:%d primerAro:%s]" % [
				entrada_ils["nombre"], str(entrada_ils["contenedor"].visible),
				str(p1_debug), str(p2_debug), int(p1_debug.distance_to(p2_debug)),
				hijos_debug.size(), pos_primer_aro]

		_ultimo_debug_rumbo = "yo:(%.6f,%.6f) destino:'%s'(%.6f,%.6f) ecefOrigen:(%.1f,%.1f,%.1f) posMarcadorEngine:%s posAvionEngine:%s distGodot:%dm distECEFcruda:%dm diferencia:%dm rumbo_actual:%d° rumbo_objetivo:%d° tieneILS:%s ilsRegistrados:%d/2 ilsActivoGlobal:%s visiblePositivo:%s visibleNegativo:%s DOSCABECERAS:%s" % [
			mundo.lat_avion, mundo.lon_avion, nombre_destino, lat_destino, lon_destino,
			ecef_origen_actual.x, ecef_origen_actual.y, ecef_origen_actual.z,
			str(destino_actual.global_position), str(global_position),
			int(distancia), int(dist_ecef_cruda), int(abs(distancia - dist_ecef_cruda)),
			int(round(rumbo_actual)), int(round(rumbo_objetivo)),
			str(tiene_ils_este_destino), cuantos_ils_registrados, str(mundo.ils_activo_global),
			visible_positivo, visible_negativo, texto_dos_cabeceras]

	# Pedido de vuelta 2026-09-20: el número concreto ("ponete en rumbo X°")
	# que tenía el viejo panel de Torre y se sacó sin querer junto con el
	# resto del panel -- las flechas solas no alcanzaban, hacía falta el dato
	# exacto para terminar de alinearse.
	etiqueta_rumbo_objetivo.text = "Ponete en rumbo %d°" % int(round(rumbo_objetivo))
	etiqueta_rumbo_objetivo.visible = true

	# BUG REAL encontrado 2026-09-21 (reportado por el usuario: "cuando estoy
	# saliendo la línea sube hasta el cielo, baja, y no queda fija"): esto
	# recalculaba la línea acá, adentro de _actualizar_torre -- que corre
	# throtteado cada 0.5s (ver _actualizar_hud), NO todos los cuadros. El
	# origen flotante se recentra en el avión CADA CUADRO (ver mundo.gd), así
	# que la posición/escala en espacio-motor que le habíamos puesto a la
	# línea quedaba vieja/incorrecta durante esos 0.5s -- se veía flotar,
	# derivar y saltar hasta que el próximo recálculo la "corregía" de
	# golpe. FIX: ya no se recalcula acá -- solo guardamos el destino y sus
	# coordenadas, y un función aparte (_actualizar_linea_guia_frame, llamada
	# desde _process en cada cuadro) hace el recálculo real, siempre con el
	# origen del cuadro actual. Fija de verdad, no un tirón cada medio segundo.
	if destino_actual.has_meta("lat") and destino_actual.has_meta("lon"):
		_hay_destino_para_linea_guia = true
		_lat_destino_linea_guia = destino_actual.get_meta("lat")
		_lon_destino_linea_guia = destino_actual.get_meta("lon")
	else:
		_hay_destino_para_linea_guia = false

	# Cartel de aviso de ILS -- ver comentario junto a la declaración de
	# label_sugerencia_ils. Solo si el destino tiene ILS real confirmado,
	# estás dentro del rango donde ya habría aros esperando, y todavía no lo
	# activaste.
	var rango_ils: float = mundo.DISTANCIA_INICIO_AROS + mundo.DISTANCIAS_GATES_ILS[0]
	_mostrar_sugerencia_ils = (destino_actual.has_meta("orientacion_usuario")
		and distancia < rango_ils and not ils_activo)

	# Flechitas verdes: para no tener que hacer cuentas con los grados, mientras
	# vuelo visual (de día) -- muestra para qué lado corregir, nada más.
	# Girar a la IZQUIERDA hace bajar el rumbo (por eso la comparación es así);
	# si en la práctica sale al revés, es cambiar el "<" y ">" de acá abajo.
	const TOLERANCIA_RUMBO = 5.0
	var diferencia_rumbo = fposmod(rumbo_objetivo - rumbo_actual + 180.0, 360.0) - 180.0
	flecha_derecha.visible = diferencia_rumbo > TOLERANCIA_RUMBO
	flecha_izquierda.visible = diferencia_rumbo < -TOLERANCIA_RUMBO
	# DESCONECTADAS 2026-09-20 (pedido explícito, "son al pedo"): las de
	# arriba/abajo (altura recomendada) quedan siempre apagadas. El cálculo de
	# altura_objetivo/factor_planeo que las alimentaba ya no se usa para nada.
	flecha_arriba.visible = false
	flecha_abajo.visible = false

# ============================================================
# CITY TOUR (pedido 2026-10-01) -- ver comentario junto a RUTA_CITY_TOUR.
# ============================================================

func _buscar_destino_por_nombre(nombre: String) -> Node3D:
	for nodo in destinos:
		if nodo.get_meta("nombre_bonito", nodo.name) == nombre:
			return nodo
	return null

# Crea blancos de práctica delante del avión. Son cuerpos físicos reales para
# que el disparo pueda comprobar un impacto, no solo comparar posiciones HUD.
func _reiniciar_objetivos_armamento() -> void:
	objetivos_destruidos = 0
	if etiqueta_objetivo_destruido:
		etiqueta_objetivo_destruido.text = "AEROPUERTOS: 0/5"
		etiqueta_objetivo_destruido.visible = true
	# Pedido 2026-10-06: esto solo reinicia los blancos de AEROPUERTOS (grupo
	# "mision_aeropuertos") -- los de la misión verde de Capital y los
	# barquitos se crean una sola vez en _ready() y deben sobrevivir aunque
	# el jugador elija un destino nuevo (no son parte de "este vuelo").
	var conservar: Array[Node3D] = []
	for objetivo in objetivos_armamento:
		if not is_instance_valid(objetivo):
			continue
		if objetivo.is_in_group("mision_aeropuertos"):
			objetivo.queue_free()
		else:
			conservar.append(objetivo)
	objetivos_armamento = conservar
	_crear_objetivos_armamento()
	_sincronizar_modo_guerra(0.0, true)

func _crear_objetivos_armamento() -> void:
	var arriba: Vector3 = _arriba_real()
	# BUG REAL encontrado 2026-10-10 (reportado: "el contador está pero no
	# aparecen los cuadrados"): "destinos" solo se llena adentro de
	# _actualizar_hud(), que corre en _process() -- la llamada nueva desde
	# _ready() (call_deferred) dispara ANTES de que _process() haya corrido
	# ni una vez, así que "destinos" todavía estaba vacío y
	# _buscar_destino_por_nombre() no encontraba ningún aeropuerto (fallaba
	# en silencio, sin error). Nos aseguramos acá mismo de que esté lleno.
	if destinos.is_empty():
		destinos = mundo.obtener_destinos()
	call_deferred("_sincronizar_modo_guerra", 0.0, true)

	# Dejamos un blanco grande y fijo en cada aeropuerto conocido.
	for nombre in ["Ezeiza", "Morón", "El Palomar", "Campo de Mayo", "San Fernando"]:
		var aeropuerto := _buscar_destino_por_nombre(nombre)
		if aeropuerto:
			var objetivo_aeropuerto := _crear_objetivo_armamento_en_posicion(aeropuerto.global_position + arriba * 120.0, "Objetivo_" + nombre, 200.0, -global_transform.basis.z, arriba)
			objetivo_aeropuerto.add_to_group("mision_aeropuertos")
			objetivo_aeropuerto.set_meta("latitud_objetivo", aeropuerto.get_meta("lat"))
			objetivo_aeropuerto.set_meta("longitud_objetivo", aeropuerto.get_meta("lon"))
			objetivo_aeropuerto.set_meta("altura_objetivo", 120.0)

			# Pedido 2026-10-11 ("probemos el contraataque en tierra, en los
			# aeropuertos, que son fijos y más fáciles de depurar que el
			# agua -- que disparen desde las dos cabeceras de pista, como
			# cañones a cada lado"): mismo sistema de vida/contraataque que
			# los barcos, reusado tal cual. Las dos cabeceras (lat/lon reales
			# de cada punta de pista) ya existen en mundo.gd para el ILS --
			# se usan acá como los dos puntos de disparo.
			objetivo_aeropuerto.set_meta("vida", VIDA_INICIAL_BARCO)
			objetivo_aeropuerto.set_meta("vida_inicial", VIDA_INICIAL_BARCO)
			var etiqueta_vida_aero := Label3D.new()
			etiqueta_vida_aero.name = "VidaRestante"
			etiqueta_vida_aero.text = str(VIDA_INICIAL_BARCO)
			etiqueta_vida_aero.font_size = 150
			etiqueta_vida_aero.outline_size = 20
			etiqueta_vida_aero.modulate = Color(1.0, 0.3, 0.1, 1.0)
			etiqueta_vida_aero.billboard = BaseMaterial3D.BILLBOARD_ENABLED
			etiqueta_vida_aero.no_depth_test = true
			etiqueta_vida_aero.visible = false
			# Pedido 2026-10-11 ("no te compliques, el cuadrado ya lo tenés
			# hecho, en el medio ponés el número"): el cuadrado y la
			# etiqueta son los dos billboard (siempre de frente a cámara),
			# así que poniendo la etiqueta en el PIVOTE del cuadrado (0,0,
			# un pelito adelante para no pisar la superficie) queda
			# exactamente centrada en el medio, se la mire desde donde se
			# la mire.
			etiqueta_vida_aero.position = Vector3(0, 0, 1.5)
			objetivo_aeropuerto.add_child(etiqueta_vida_aero)
			objetivo_aeropuerto.set_meta("etiqueta_vida", etiqueta_vida_aero)
			var origenes_canon: Array = []
			for entrada_cab in mundo.contenedor_ils_dos_cabeceras:
				if entrada_cab["nombre"] == nombre:
					origenes_canon.append(mundo._posicion_desde_lat_lon(entrada_cab["cab1_lat"], entrada_cab["cab1_lon"], 0.0) + arriba * (float(entrada_cab.get("cab1_alt", 10.0)) + 15.0))
					origenes_canon.append(mundo._posicion_desde_lat_lon(entrada_cab["cab2_lat"], entrada_cab["cab2_lon"], 0.0) + arriba * (float(entrada_cab.get("cab2_alt", 10.0)) + 15.0))
					break
			if origenes_canon.is_empty():
				# Ezeiza no tiene datos de "dos cabeceras" todavía -- un solo
				# cañón desde el propio blanco, mejor que nada.
				origenes_canon = [objetivo_aeropuerto.global_position]
			objetivo_aeropuerto.set_meta("origenes_disparo", origenes_canon)

# Lugares reales dentro de Capital Federal para la "misión verde" de prueba
# (pedido 2026-10-06: "en vez de cuadrados rojos, poneme verdes, bastantes,
# dentro de Capital"). Coordenadas aproximadas de lugares conocidos.
const OBJETIVOS_MISION_VERDE_CABA = [
	{"nombre": "Nuñez", "lat": -34.545, "lon": -58.464},
	{"nombre": "Belgrano", "lat": -34.562, "lon": -58.456},
	{"nombre": "Planetario", "lat": -34.5677, "lon": -58.4136},
	{"nombre": "Chacarita", "lat": -34.588, "lon": -58.4555},
	{"nombre": "ParqueCentenario", "lat": -34.6061, "lon": -58.4342},
	{"nombre": "Congreso", "lat": -34.6095, "lon": -58.3927},
	{"nombre": "Caminito", "lat": -34.6345, "lon": -58.3631},
	{"nombre": "Flores", "lat": -34.6283, "lon": -58.4650},
	{"nombre": "Mataderos", "lat": -34.6598, "lon": -58.5094},
	{"nombre": "VillaSoldati", "lat": -34.6730, "lon": -58.4650},
]

# Se crea UNA sola vez (llamado desde _ready) -- a diferencia de los
# aeropuertos, esta misión no depende de qué destino elegiste, así que no
# hace falta recrearla cada vez que confirmás un viaje nuevo.
func _crear_mision_verde() -> void:
	var arriba: Vector3 = _arriba_real()
	for dato in OBJETIVOS_MISION_VERDE_CABA:
		var pos: Vector3 = mundo._posicion_desde_lat_lon(dato["lat"], dato["lon"], 0.0) + arriba * 120.0
		var objetivo := _crear_objetivo_armamento_en_posicion(pos, "MisionVerde_" + dato["nombre"], 200.0, -global_transform.basis.z, arriba, Color(0.08, 1.0, 0.2, 1.0))
		objetivo.add_to_group("mision_verde")
		objetivo.set_meta("latitud_objetivo", dato["lat"])
		objetivo.set_meta("longitud_objetivo", dato["lon"])
		objetivo.set_meta("altura_objetivo", 120.0)

# 10 barquitos de prueba, repartidos de la costa de Aeroparque hacia adentro
# del río (pedido 2026-10-07: "distribuidos, no uno al lado del otro, podés
# usar hasta la orilla de Uruguay de radio"). Cuadraditos negros con brillo
# por ahora -- el modelo 3D real de barco es un proyecto aparte para más
# adelante. Van a baja altura (casi al nivel del agua, no a 120m).
	# BUG REAL encontrado 2026-10-07, segunda vuelta (reportado: "ahora están
	# todos arriba de la tierra, la mayoría"): el intento anterior subestimó
	# cuánto se mete el PUERTO de Buenos Aires adentro del río (las
	# terminales portuarias llegan hasta casi -58.35 de longitud en esta
	# zona) -- varios puntos "cerca de la costa" caían arriba de las
	# terminales, no en agua. Verificado a ojo con un mapa real (OpenStreetMap):
	# a partir de -58.33 de longitud ya es agua abierta sin nada hasta
	# Uruguay, para cualquier latitud entre -34.50 y -34.62. Esta vez los 10
	# puntos arrancan TODOS en -58.33 o más al este (nunca más cerca de la
	# costa que eso), así no hay forma de que caigan en tierra ni en el puerto.
	# Pedido 2026-10-10 ("tenés un río gigante, no los amontones"): mucho más
	# separados que antes (antes cabían en ~13x15km, ahora en ~33x27km).
# Pedido 2026-10-10 ("tenés un río gigante, desparramalo" + "cerca de
# Aeroparque yendo para Colonia del Sacramento, así sé dónde buscarlos"):
# Pedido 2026-10-11 ("20 en total, 10 y 10, ya sacamos el submarino"):
# bajado de 30 a 20 (10 de cada uno de los 2 tipos reales), manteniendo el
# mismo recorrido completo Aeroparque -> Colonia del Sacramento, siempre en
# agua confirmada (longitud -58.32 o más al este).
const BARCOS_RIO_AEROPARQUE = [
	{"lat": -34.590, "lon": -58.320}, {"lat": -34.540, "lon": -58.304}, {"lat": -34.525, "lon": -58.272},
	{"lat": -34.560, "lon": -58.256}, {"lat": -34.545, "lon": -58.224}, {"lat": -34.575, "lon": -58.209},
	{"lat": -34.565, "lon": -58.177}, {"lat": -34.590, "lon": -58.161}, {"lat": -34.570, "lon": -58.129},
	{"lat": -34.525, "lon": -58.113}, {"lat": -34.585, "lon": -58.082}, {"lat": -34.545, "lon": -58.066},
	{"lat": -34.530, "lon": -58.034}, {"lat": -34.565, "lon": -58.018}, {"lat": -34.540, "lon": -57.987},
	{"lat": -34.570, "lon": -57.971}, {"lat": -34.560, "lon": -57.939}, {"lat": -34.585, "lon": -57.923},
	{"lat": -34.575, "lon": -57.891}, {"lat": -34.565, "lon": -57.859},
]
# Pedido 2026-10-11 ("de día no se ven, parecen hundidos -- subilos
# extremadamente, prioridad es verlos, no la precisión del calado"): el
# cálculo de línea de flotación por AABB evidentemente no alcanza para
# algunos modelos (o tienen un cabeceo propio de fábrica que lo hunde más
# de lo esperado) -- en vez de seguir afinando a ciegas, directamente se
# sube mucho el casco sobre el agua. Van a quedar "flotando alto" por
# ahora, se puede bajar de a poco después con feedback visual real.
# BAJADA 2026-10-11 (ahora que la orientación/posición de la flota de
# Aeroparque-Quilmes quedó resuelta con el pitch de +25°, ya no hace falta
# la altura de emergencia -- Pablo confirmó volando bien bajo que a 200m el
# avión le pasa por ABAJO al barco: "bajalos 180 metros, que no se le pueda
# pasar por abajo").
# SUBIDA 2026-10-11 (reportado tras probar a 20m: "se ve el barco pero el
# agua le llega a la cubierta, como hundido -- más hundido todavía yendo
# para el lado de Quilmes, ahí ya solo se ve el mástil"): 20m no alcanzaba
# -- la línea de flotación real varía un poco según la zona del río. Subido
# 30m más como primer ajuste; Pablo lo vuelve a probar volando.
const ALTURA_BARCO_FLOTA := 50.0
# Pedido 2026-10-11 ("es muy fácil voltear barcos, de a uno no, que tenga un
# contador, 10 está bien -- un misil (a futuro) resta 5, una bala resta 1"):
# mismo patrón que VIDA_INICIAL_INDUSTRIA.
const VIDA_INICIAL_BARCO := 10
const DANIO_BALA_BARCO := 1
const DANIO_MISIL_BARCO := 5  # todavía no usado -- el avión no carga misiles por ahora
# "Despacito, pero que se note" (pedido 2026-10-07) -- la versión anterior
# (0.000003 grados/seg) equivalía a ~0.27 m/s, invisible a simple vista en
# una prueba corta. 2.5 m/s (~9 km/h) es un paso de barco lento, pero el
# movimiento ya se nota. Convertimos metros/seg a grados de longitud usando
# la distancia real de un grado en ESTA latitud (111320m * cos(lat)).
const VELOCIDAD_BARCOS_MPS := 2.5
# BUG REAL encontrado 2026-10-10/11 (reportado: "gigantesco, enterrado",
# después "cae en picada, vertical"): con escala fija 1.0, los modelos
# salían del tamaño NATIVO del archivo -- cada uno con unidades distintas.
# Investigado en Blender 2026-10-11: el "sdf_destroyer" (el oscuro) resultó
# ser en realidad una NAVE ESPACIAL (el propio archivo original se llama
# "spaceship", con esqueleto de huesos tipo Source Engine) -- por eso nunca
# se comportaba como un barco por más que se ajustara. Pedido explícito de
# Pablo al confirmarlo: sacarlo de la rotación y repartir los 30 lugares
# entre los otros dos modelos, que sí son barcos de mar reales y ya andaban
# bien. "escala_extra" multiplica la escala automática (ambos +50%,
# "cuesta divisarlos"). "roll_extra_grados" corrige la banda/inclinación
# lateral del gris (zumwalt) -- unos pocos grados nomás, primer ajuste a
# ojo desde la foto, afinar con feedback si todavía no queda perfecto.
# BUG REAL encontrado 2026-10-11 investigando en Blender (pedido: "el gris
# no se encuentra, arreglalo vos, no hagas que yo lo busque"): el Aquitaine
# tiene su lado más largo en el eje X nativo (3.44 contra 0.74 de "largo" en
# Z) -- al revés que el Zumwalt. look_at() siempre asume que el barco
# "mira" hacia su -Z local, así que el Aquitaine quedaba orientado de
# costado (90° de más) en vez de apuntar a donde viaja -- probablemente por
# eso costaba reconocerlo como barco al pasar volando. "eje_largo_es_x"
# dispara una rotación de 90° extra ANTES del look_at para compensar.
# CORTADO POR LO SANO 2026-10-11 (diagnóstico de Gemini, confirmado con
# capturas): zumwalt.glb quedaba "parado como un cuchillo" con look_at() --
# el archivo trae sus ejes de proa/vertical desalineados de origen (no es
# algo que se arregle bien con un roll_extra_grados por código, y parchearlo
# iba a traer problemas colaterales el día que los barcos necesiten rotar
# según rumbo o colisionar bien). aquitaine.glb quedó perfecto -- pedido
# explícito de Pablo: sacar el zumwalt y quedarse solo con el que funciona,
# duplicado para los 20 lugares.
# "pitch_extra_grados" encontrado 2026-10-11 con el experimento de sweep
# (20 barcos, cada uno con un pitch distinto de -95° a +95° de a 10°,
# probado por Pablo en vuelo desde todos los ángulos): Barco_12 (pitch
# +25°) quedó perfecto de cualquier lado; Barco_11 (+15°) quedaba casi bien
# pero con una leve inclinación en la punta. Ganador: +25°.
const MODELOS_BARCOS = [
	{"ruta": "res://modelos_objetivos/barcos/aquitaine.glb", "escala_extra": 1.95, "ajuste_altura_extra": 0.0, "roll_extra_grados": 0.0, "eje_largo_es_x": true, "pitch_extra_grados": 25.0},
]
const LARGO_OBJETIVO_BARCO := 180.0

# DIAGNÓSTICO CONFIRMADO 2026-10-11 (prueba de aislamiento sugerida por
# Gemini, tras ver con la tecla J que el casco del aquitaine aparecía
# hundido y torcido debajo del terreno): con el offset de flotación en 0 y
# las correcciones de eje/roll desactivadas, los barcos aparecieron bien
# ubicados -- confirmado por Pablo con capturas. El culpable era el cálculo
# automático de "offset_flotacion" a partir del AABB nativo (roto para
# aquitaine.glb, que trae una malla "HLOD-9000" con una jerarquía de
# unidades deforme, típica de un export Sketchfab/FBX). Las correcciones de
# "eje_largo_es_x" y "roll_extra_grados" NO eran el problema -- se ven mal
# en las capturas de la prueba de aislamiento porque esa prueba también las
# apagaba a propósito. Dejamos esta constante como documentación del
# diagnóstico; ya no se usa en el código (offset_flotacion quedó fijo en 0
# más abajo, eje/roll vuelven a aplicarse siempre).
const DEBUG_BARCOS_AISLAR_TEST := false

func _crear_barcos_rio() -> void:
	var arriba: Vector3 = _arriba_real()
	# Rumbo real de los barcos = OESTE (longitud decreciente, ver
	# _actualizar_barcos) -- se orientan mirando hacia ahí con look_at, no
	# con una base arbitraria como industria (a un barco SÍ le importa para
	# qué lado mira, a un edificio no).
	var oeste: Vector3 = -mundo.este_motor_actual
	var indice := 0
	for dato in BARCOS_RIO_AEROPARQUE:
		var info_modelo: Dictionary = MODELOS_BARCOS[indice % MODELOS_BARCOS.size()]
		var escena: PackedScene = load(info_modelo["ruta"])
		if not escena:
			indice += 1
			continue
		var instancia: Node3D = escena.instantiate()
		# Escala automática: medimos el modelo a escala 1.0 (tamaño nativo
		# del archivo) y calculamos cuánto achicarlo/agrandarlo para que su
		# lado más largo (el barco puede estar modelado a lo largo de X o de
		# Z según cómo lo haya hecho el autor) mida LARGO_OBJETIVO_BARCO,
		# multiplicado por el ajuste manual de ese modelo en particular.
		var caja_nativa: AABB = _calcular_aabb_local(instancia)
		var largo_nativo: float = max(caja_nativa.size.x, caja_nativa.size.z)
		var escala: float = (LARGO_OBJETIVO_BARCO / largo_nativo if largo_nativo > 0.001 else 1.0) * info_modelo["escala_extra"]
		instancia.scale = Vector3.ONE * escala

		# BUG REAL encontrado 2026-10-11 con ayuda de Gemini (prueba de
		# aislamiento con la tecla J -- ver DEBUG_BARCOS_AISLAR_TEST más
		# arriba): este offset calculado desde "caja_nativa.position.y" se
		# rompía con aquitaine.glb -- esa malla ("HLOD-9000") viene con una
		# jerarquía de unidades deforme (export Sketchfab/FBX), y el pivote
		# calculado no correspondía a la base real del casco. Resultado: el
		# barco quedaba hundido ~100m bajo el agua y además torcido. Se saca
		# el cálculo automático -- con offset fijo en 0, ALTURA_BARCO solo,
		# los barcos aparecieron derechos y bien ubicados (confirmado por
		# Pablo con capturas). Si alguno queda con la línea de flotación fea
		# (mitad adentro/afuera del agua), se corrige a mano con
		# "ajuste_altura_extra" por modelo, no con este cálculo automático.
		var offset_flotacion: float = 0.0
		var punto_agua: Vector3 = mundo._posicion_desde_lat_lon(dato["lat"], dato["lon"], 0.0)
		var pos: Vector3 = punto_agua - arriba * offset_flotacion + arriba * (ALTURA_BARCO_FLOTA + float(info_modelo["ajuste_altura_extra"]))
		mundo.add_child(instancia)
		instancia.global_position = pos
		instancia.look_at(pos + oeste, arriba)

		var cuerpo := StaticBody3D.new()
		cuerpo.name = "Barco_%d" % indice
		cuerpo.add_to_group("objetivos_armamento")
		cuerpo.add_to_group("mision_barcos")
		cuerpo.collision_layer = 2
		cuerpo.collision_mask = 0
		mundo.add_child(cuerpo)
		cuerpo.global_position = pos
		cuerpo.look_at(pos + oeste, arriba)
		# Corrección fina de banda/inclinación lateral (pedido 2026-10-11,
		# zumwalt "ladeado hacia la izquierda, muy poquito") -- rota un poco
		# alrededor del eje de avance (oeste), DESPUÉS del look_at. Como
		# "instancia" va a quedar colgada de "cuerpo" sin rotación propia,
		# alcanza con rotar acá -- se propaga sola.
		var roll_grados: float = info_modelo.get("roll_extra_grados", 0.0)
		if roll_grados != 0.0:
			cuerpo.rotate_object_local(Vector3.FORWARD, deg_to_rad(roll_grados))
		instancia.get_parent().remove_child(instancia)
		cuerpo.add_child(instancia)
		var base_instancia := Basis.IDENTITY
		if info_modelo.get("eje_largo_es_x", false):
			base_instancia = base_instancia.rotated(Vector3.UP, deg_to_rad(90.0))
		# Corrección de pitch (cabeceo) encontrada con el experimento de
		# sweep de 20 barcos (ver comentario junto a MODELOS_BARCOS) -- el
		# aquitaine queda "parado como un cuchillo" visto de frente/popa sin
		# esto. Rotación sobre el eje local X (RIGHT), antes de escalar.
		var pitch_grados: float = info_modelo.get("pitch_extra_grados", 0.0)
		if pitch_grados != 0.0:
			base_instancia = base_instancia.rotated(Vector3.RIGHT, deg_to_rad(pitch_grados))
		var basis_final: Basis = base_instancia.scaled(Vector3.ONE * escala)
		# BUG REAL encontrado 2026-10-11 (reportado: "la lucecita quedó a un
		# costado, lejos del barco gris"): el pivote (0,0,0) de
		# aquitaine.glb NO está centrado en el casco -- viene corrido feo
		# por la jerarquía "HLOD" deforme de ese archivo (ver diagnóstico de
		# offset_flotacion más arriba). La baliza se cuelga de "cuerpo" en
		# el eje (0, altura, 0), siempre centrada en el pivote -- pero el
		# casco se dibujaba lejos de ahí porque su propio pivote interno no
		# coincide con su centro geométrico. Se recentra "instancia" acá
		# para que el centro horizontal (X/Z) del casco quede exactamente
		# sobre el pivote de "cuerpo" (mismo punto que la baliza), sin
		# tocar la altura (eje Y, ya resuelto por separado con
		# ALTURA_BARCO). Aplica a cualquier modelo con el pivote
		# descentrado, no solo a este.
		var centro_xz_nativo := Vector3(caja_nativa.position.x + caja_nativa.size.x * 0.5, 0.0, caja_nativa.position.z + caja_nativa.size.z * 0.5)
		instancia.transform = Transform3D(basis_final, -(basis_final * centro_xz_nativo))

		# Misma corrección que en industria: _calcular_aabb_local da el
		# tamaño NATIVO, hay que multiplicarlo por la escala a mano.
		var caja: AABB = _calcular_aabb_local(instancia)
		caja.position *= escala
		caja.size *= escala
		var forma := CollisionShape3D.new()
		var caja_forma := BoxShape3D.new()
		if caja.size.length() > 0.001:
			caja_forma.size = caja.size * 1.1
			forma.position = caja.position + caja.size * 0.5
		else:
			caja_forma.size = Vector3(180, 30, 25)
		forma.shape = caja_forma
		cuerpo.add_child(forma)

		# Pedido 2026-10-11 ("para misiones de noche, una lucecita blanca que
		# parpadee como la del avión, pero más chica -- la mitad"): reusa
		# EXACTAMENTE el mismo mecanismo de la estroboscópica del avión
		# (mismo material, mismo parpadeo en _actualizar_estroboscopica) para
		# que de noche no se apague por el post-proceso nocturno (ya
		# resuelto para las luces de avión, se hereda gratis acá).
		var baliza_barco := _crear_luz_navegacion(Vector3(0, caja.size.y + 3.0, 0), Color(1.0, 1.0, 1.0), cuerpo, 2.0)
		baliza_barco.name = "BalizaBarco"

		# Pedido 2026-10-11 ("es muy fácil voltear barcos, que para hundir
		# cada uno tenga que darle varios tiros, con un contador visible,
		# como ya tenemos en industria"): mismo sistema de "vida" +
		# numerito flotante que _crear_mision_industria(), reusado tal
		# cual. Arranca SIN número visible (barco "dormido") -- recién se
		# muestra con el primer impacto, que es también lo que activa el
		# contraataque del barco (ver VIDA_INICIAL_BARCO y el bloque de
		# "sigue_con_vida" en _process).
		var etiqueta_vida_barco := Label3D.new()
		etiqueta_vida_barco.name = "VidaRestante"
		etiqueta_vida_barco.text = str(VIDA_INICIAL_BARCO)
		etiqueta_vida_barco.font_size = 150
		etiqueta_vida_barco.outline_size = 20
		etiqueta_vida_barco.modulate = Color(1.0, 0.3, 0.1, 1.0)
		etiqueta_vida_barco.billboard = BaseMaterial3D.BILLBOARD_ENABLED
		etiqueta_vida_barco.no_depth_test = true
		etiqueta_vida_barco.visible = false
		# Pedido 2026-10-11 ("no lo encuentro, ponelo donde está la luz, que
		# ahí ya sé mirar"): EXACTAMENTE la misma posición local que la
		# baliza (no una cercana) -- y letra grande + el mismo reescalado
		# por distancia que ya usa la baliza (ver _actualizar_barcos), para
		# que se lea desde lejos igual que la lucecita.
		etiqueta_vida_barco.position = Vector3(0, caja.size.y + 3.0, 0)
		cuerpo.add_child(etiqueta_vida_barco)
		cuerpo.set_meta("vida", VIDA_INICIAL_BARCO)
		cuerpo.set_meta("vida_inicial", VIDA_INICIAL_BARCO)
		cuerpo.set_meta("etiqueta_vida", etiqueta_vida_barco)

		cuerpo.set_meta("latitud_objetivo", dato["lat"])
		cuerpo.set_meta("longitud_objetivo", dato["lon"])
		# Guardamos la altura YA CORREGIDA por la línea de flotación -- si no,
		# _actualizar_barcos() (que mueve el barco cada cuadro) lo volvería a
		# poner a la altura CRUDA sin la corrección, y el efecto se perdía
		# apenas el barco empezaba a moverse.
		cuerpo.set_meta("altura_objetivo", ALTURA_BARCO_FLOTA + float(info_modelo["ajuste_altura_extra"]) - offset_flotacion)
		objetivos_armamento.append(cuerpo)
		objetivos_barcos.append(cuerpo)
		indice += 1

# DEBUG TEMPORAL 2026-10-11 (ver bloque F9 en _input): imprime en la consola
# de Godot (ventana "Salida"/"Output" del editor, o godot.log) el estado real
# del barco más cercano a la cámara -- posición global, si Godot lo considera
# visible en el árbol, y el AABB de cada malla interna del GLB. Si
# "Visible in Tree" es true y el AABB tiene tamaño > 0 pero el barco no se ve
# en pantalla, el problema es oclusión por el terreno (ver F10) o cámara, NO
# el código de creación del barco.
func _diagnosticar_barco_mas_cercano() -> void:
	if objetivos_barcos.is_empty():
		print("🚢 DEBUG: no hay barcos creados todavía")
		return
	var mas_cercano: Node3D = null
	var mejor_distancia := INF
	for barco in objetivos_barcos:
		if not is_instance_valid(barco):
			continue
		var d: float = camara.global_position.distance_to(barco.global_position)
		if d < mejor_distancia:
			mejor_distancia = d
			mas_cercano = barco
	if not mas_cercano:
		print("🚢 DEBUG: ningún barco válido encontrado")
		return
	print("--- DIAGNÓSTICO DE BARCO: ", mas_cercano.name, " (distancia a cámara: ", mejor_distancia, "m) ---")
	print("Global Position: ", mas_cercano.global_position)
	print("Visible in Tree: ", mas_cercano.is_visible_in_tree())
	if mas_cercano.has_meta("vida"):
		print("Vida: ", mas_cercano.get_meta("vida"), " | activado: ", mas_cercano.get_meta("activado", false), " | en barcos_activados: ", barcos_activados.has(mas_cercano))
	if mas_cercano.has_meta("latitud_objetivo"):
		print("Lat/Lon: ", mas_cercano.get_meta("latitud_objetivo"), ", ", mas_cercano.get_meta("longitud_objetivo"))
	var mallas := mas_cercano.find_children("*", "MeshInstance3D", true, false)
	print("Cantidad de mallas encontradas: ", mallas.size())
	for m in mallas:
		if m is MeshInstance3D:
			var caja: AABB = m.get_aabb()
			print("  - Malla: ", m.name, " | visible local: ", m.visible, " | capa render: ", m.layers, " | AABB tamaño: ", caja.size, " | AABB posición: ", caja.position)
			if m.material_override:
				print("    material_override asignado (", m.material_override, ")")
			elif m.mesh and m.mesh.surface_get_material(0):
				print("    material de superficie 0: ", m.mesh.surface_get_material(0))
			else:
				print("    sin material propio -- usa el del GLB importado")

# Pedido 2026-10-11 ("de altura las lucecitas son imperceptibles, me tuve
# que acercar mucho -- que se vean bien parpadeantes desde 2000-3000m, y a
# medida que te acercás que se achiquen, que no quede un círculo gigante
# feo pegado al objetivo"): agranda la baliza según la distancia a la
# cámara -- chica/normal de cerca, grande de lejos, compartida entre
# barcos e industria.
const DISTANCIA_REF_BALIZA := 300.0  # por debajo de esto, tamaño normal
const FACTOR_MAXIMO_BALIZA := 14.0   # tope de agrandado a distancia
func _escalar_baliza_por_distancia(baliza: Node3D, posicion_mundo: Vector3) -> void:
	if not camara:
		return
	var distancia: float = camara.global_position.distance_to(posicion_mundo)
	var factor: float = clamp(distancia / DISTANCIA_REF_BALIZA, 1.0, FACTOR_MAXIMO_BALIZA)
	baliza.scale = Vector3.ONE * factor

func _actualizar_barcos(delta: float) -> void:
	var arriba: Vector3 = _arriba_real()
	# Boceto de invasión 2026-10-11: un barco que cruza la longitud de la
	# costa sin ser hundido "desembarca" -- se junta en una lista aparte
	# porque no se puede hacer queue_free()/sacarlo de objetivos_barcos
	# DENTRO del for que recorre esa misma lista.
	var barcos_para_desembarcar: Array[Node3D] = []
	for objetivo in objetivos_barcos:
		if not is_instance_valid(objetivo) or not objetivo.has_meta("longitud_objetivo"):
			continue
		var latitud: float = objetivo.get_meta("latitud_objetivo")
		var metros_por_grado_lon: float = 111320.0 * cos(deg_to_rad(latitud))
		var grados_por_seg: float = VELOCIDAD_BARCOS_MPS / metros_por_grado_lon
		var nueva_lon: float = objetivo.get_meta("longitud_objetivo") - grados_por_seg * delta
		if nueva_lon <= LON_COSTA_DESEMBARCO:
			barcos_para_desembarcar.append(objetivo)
			continue
		objetivo.set_meta("longitud_objetivo", nueva_lon)
		var altura: float = objetivo.get_meta("altura_objetivo")
		# look_at con origen=destino tiraría error si ya mira exactamente
		# para ahí -- acá el rumbo (oeste) no cambia cuadro a cuadro, así
		# que no hace falta reorientar cada vez, solo mover la posición.
		objetivo.global_position = mundo._posicion_desde_lat_lon(latitud, nueva_lon, 0.0) + arriba * altura
		var baliza_barco: Node3D = objetivo.get_node_or_null("BalizaBarco")
		if baliza_barco:
			_escalar_baliza_por_distancia(baliza_barco, baliza_barco.global_position)
		var etiqueta_vida_actualizar: Node3D = objetivo.get_node_or_null("VidaRestante")
		if etiqueta_vida_actualizar and etiqueta_vida_actualizar.visible:
			_escalar_baliza_por_distancia(etiqueta_vida_actualizar, etiqueta_vida_actualizar.global_position)
	for barco in barcos_para_desembarcar:
		_desembarcar_barco(barco)

const VIDA_CAPTURA_AEROPUERTO := 40

# Un barco que cruzó la línea de costa sin ser hundido desaparece y larga
# 5 unidades de invasión en su lugar.
func _desembarcar_barco(barco: Node3D) -> void:
	if not is_instance_valid(barco):
		return
	var lat: float = barco.get_meta("latitud_objetivo", 0.0)
	var lon: float = barco.get_meta("longitud_objetivo", 0.0)
	print("🪖 Desembarco -- barco llegó a la costa en lat=%.4f lon=%.4f, larga 5 unidades" % [lat, lon])
	objetivos_armamento.erase(barco)
	objetivos_barcos.erase(barco)
	barcos_activados.erase(barco)
	barco.queue_free()
	for i in range(5):
		_crear_unidad_invasion(lat, lon)

# DEBUG TEMPORAL 2026-10-11 (ver tecla Y en _input): modelos candidatos para
# reemplazar los cuadraditos negros de la invasión por vehículos 3D de
# verdad. "eje_largo_es_x": revisado en Blender el T-80BV (antes de que
# Blender crasheara por un problema del driver de video, no del modelo) --
# el casco viene largo en el eje X nativo, igual que el aquitaine.glb, así
# que arranca con la misma corrección de 90°. El EBRC Jaguar todavía no se
# pudo mirar en Blender -- por eso entra en la MISMA prueba con las mismas 4
# orientaciones, para que la fila visual confirme o corrija el valor en los
# dos casos.
# "roll_extra_grados" (pedido 2026-10-11, foto del T-80BV "tumbado para un
# lado" ~25-30°): mismo mecanismo que ya usamos para el zumwalt/aquitaine --
# un giro fijo sobre el eje de avance, DESPUÉS del yaw, para enderezar un
# modelo que viene ladeado de origen. Arranca en 0 hasta confirmar con
# Pablo si TODOS los T-80 salen torcidos igual (entonces es el modelo, se
# corrige acá y listo) o si fue nomás esa unidad en particular.
# Confirmado 2026-10-11 ("los marrones -- el Jaguar -- estaban torcidos, los
# negros -- el T-80 -- bien derechos, y los dos marrones eran de la misma
# marca"): es el modelo del Jaguar, no una unidad suelta. -25° es una
# primera prueba a ciegas (no hay forma de saber el signo sin probarlo) --
# avisame si quedó mejor, peor, o se fue para el otro lado, y seguimos
# ajustando como con el roll del zumwalt/aquitaine.
const MODELOS_PRUEBA_VEHICULOS := [
	{"nombre": "T-80BV", "ruta": "res://modelos_objetivos/vehiculos/t80bv.glb", "eje_largo_es_x": true, "roll_extra_grados": 0.0},
	{"nombre": "Jaguar", "ruta": "res://modelos_objetivos/vehiculos/jaguar.glb", "eje_largo_es_x": true, "roll_extra_grados": -25.0},
]
# "Tamaño muy grande" pedido explícito -- el objetivo de largo de referencia
# (1.0x) ya arranca grande a propósito; los otros 3 tamaños de la fila de
# tamaños van todavía más grandes, nunca más chicos.
const LARGO_OBJETIVO_VEHICULO_PRUEBA := 45.0
# BUG REAL encontrado 2026-10-11 (reportado con 3 capturas: "no veo nada,
# la A tampoco", volando hasta la cabecera de Aeroparque sin encontrar ni
# un camión): la versión anterior calculaba la posición con
# mundo._posicion_desde_lat_lon(lat, lon, 0.0) + raycast al terreno -- pero
# los CUADRADOS de misión que sí se ven siempre (ver _crear_objetivos_
# armamento) usan otra referencia distinta: aeropuerto.global_position (el
# nodo real ya puesto en la escena) + arriba*120.0, sin raycast. Evidentemente
# esas dos formas de calcular "la misma" posición no dan el mismo punto --
# mismo tipo de problema que ya tuvimos con el aquitaine y el A380 (un
# cálculo automático que parece correcto pero no lo es). Solución: dejar de
# inventar una posición nueva y clavar la prueba exactamente en la misma
# fórmula que ya sabemos que funciona, con offsets en METROS reales (no en
# grados de lat/lon) para separar cada copia.
# Pedido 2026-10-11, segunda vuelta: ya los encontramos (tecla X), ahora la
# foto de Pablo al lado del F/A-18 muestra el tanque 3-4 veces más largo que
# el caza -- un T-80 real (~9.5m) es más CHICO que un F/A-18 (~17m), así que
# 25x quedó muy exagerado. Bajado a un tamaño más realista como punto de
# partida; se sigue ajustando con los números reales que tire _diagnosticar_
# vehiculo_prueba_mas_cercano() (tecla Z) en vez de ir a ojo con capturas.
var escala_exagerada_prueba: float = 6.0

# Columna de luz fucsia bien gruesa y alta + una "A" gigante flotando
# arriba (ver pedido 2026-10-11 junto al llamado de esta función) --
# visible desde el aire a mucha distancia, no depende de encontrar
# camiones chicos entre el pasto. "pos" ya viene calculada (misma fórmula
# que los cuadrados de misión), no se recalcula acá.
func _crear_baliza_ancla_prueba(pos: Vector3, arriba: Vector3) -> Node3D:
	var cuerpo := Node3D.new()
	cuerpo.name = "BalizaPruebaVehiculos"
	mundo.add_child(cuerpo)
	cuerpo.global_position = pos

	var pilar := MeshInstance3D.new()
	var malla := CylinderMesh.new()
	malla.height = 1500.0
	malla.top_radius = 15.0
	malla.bottom_radius = 15.0
	pilar.mesh = malla
	var material_pilar := StandardMaterial3D.new()
	material_pilar.albedo_color = Color(1.0, 0.0, 1.0, 1.0)
	material_pilar.emission_enabled = true
	material_pilar.emission = Color(1.0, 0.0, 1.0)
	material_pilar.emission_energy_multiplier = 5.0
	material_pilar.shading_mode = BaseMaterial3D.SHADING_MODE_UNSHADED
	pilar.material_override = material_pilar
	pilar.position = arriba * 750.0  # el cilindro queda centrado en su propio origen -- esto lo deja con la base al nivel del suelo
	cuerpo.add_child(pilar)

	var etiqueta := Label3D.new()
	etiqueta.text = "A"
	etiqueta.font_size = 700
	etiqueta.outline_size = 70
	etiqueta.modulate = Color(1.0, 0.0, 1.0, 1.0)
	etiqueta.billboard = BaseMaterial3D.BILLBOARD_ENABLED
	etiqueta.no_depth_test = true
	etiqueta.position = arriba * 1600.0
	cuerpo.add_child(etiqueta)
	return cuerpo

# TERCERA VUELTA 2026-10-11 (reportado: "aprieto X varias veces y van
# rotando, ninguno queda derecho" -- el problema real: X agarraba las 4
# unidades de invasión MÁS CERCANAS AL AVIÓN, que cambian cada vez que
# Pablo se mueve, cada una con su propia orientación heredada de cuando se
# creó -- por eso parecía que "rotaban" entre apretadas sin ton ni son).
# Versión definitiva: SIEMPRE el mismo tanque y el mismo Jaguar, en el
# mismo lugar exacto (anclados a aeropuerto.global_position, la fórmula
# probada), mirando siempre para el mismo lado fijo (norte, para poder
# chequear con la brújula/rumbo del avión) -- apretar X de nuevo solo
# actualiza esos dos mismos vehículos con los valores de escala/roll
# actuales, no crea ni elige otros.
func _prueba_vehiculos_fija() -> void:
	var aeropuerto := _buscar_destino_por_nombre("Aeroparque")
	if not aeropuerto:
		print("⚠️ No se encontró Aeroparque para la prueba de vehículos")
		return
	for nodo in nodos_prueba_vehiculos:
		if is_instance_valid(nodo):
			nodo.queue_free()
	nodos_prueba_vehiculos.clear()

	var arriba: Vector3 = _arriba_real()
	var norte: Vector3 = mundo.norte_motor_actual
	var este: Vector3 = mundo.este_motor_actual
	var base: Vector3 = aeropuerto.global_position + arriba * 120.0
	nodos_prueba_vehiculos.append(_crear_baliza_ancla_prueba(base, arriba))

	var reporte := ""
	var posiciones := [base + norte * 300.0, base + norte * 300.0 + este * 200.0]
	for i in range(MODELOS_PRUEBA_VEHICULOS.size()):
		var info_modelo: Dictionary = MODELOS_PRUEBA_VEHICULOS[i]
		var escena: PackedScene = load(info_modelo["ruta"])
		if not escena:
			print("⚠️ No se pudo cargar ", info_modelo["ruta"])
			continue
		var pos: Vector3 = posiciones[i]
		var cuerpo := Node3D.new()
		cuerpo.name = "PruebaFija_" + info_modelo["nombre"]
		mundo.add_child(cuerpo)
		cuerpo.global_position = pos
		cuerpo.look_at(pos + norte, arriba)  # siempre mirando al norte, fijo
		var roll_grados: float = info_modelo.get("roll_extra_grados", 0.0)
		if roll_grados != 0.0:
			cuerpo.rotate_object_local(Vector3.FORWARD, deg_to_rad(roll_grados))

		var instancia: Node3D = escena.instantiate()
		var caja_nativa: AABB = _calcular_aabb_local(instancia)
		var largo_nativo: float = max(caja_nativa.size.x, caja_nativa.size.z)
		var escala: float = (LARGO_OBJETIVO_VEHICULO_PRUEBA / largo_nativo if largo_nativo > 0.001 else 1.0) * escala_exagerada_prueba
		mundo.add_child(instancia)
		var base_instancia := Basis.IDENTITY
		if info_modelo.get("eje_largo_es_x", false):
			base_instancia = base_instancia.rotated(Vector3.UP, deg_to_rad(90.0))
		var basis_final: Basis = base_instancia.scaled(Vector3.ONE * escala)
		var centro_xz_nativo := Vector3(caja_nativa.position.x + caja_nativa.size.x * 0.5, 0.0, caja_nativa.position.z + caja_nativa.size.z * 0.5)
		instancia.get_parent().remove_child(instancia)
		cuerpo.add_child(instancia)
		instancia.transform = Transform3D(basis_final, -(basis_final * centro_xz_nativo))

		var etiqueta := Label3D.new()
		etiqueta.text = info_modelo["nombre"]
		etiqueta.font_size = 400
		etiqueta.outline_size = 50
		etiqueta.modulate = Color(1.0, 1.0, 0.2, 1.0)
		etiqueta.billboard = BaseMaterial3D.BILLBOARD_ENABLED
		etiqueta.no_depth_test = true
		etiqueta.position = Vector3(0, 150.0, 0)
		cuerpo.add_child(etiqueta)
		nodos_prueba_vehiculos.append(cuerpo)

		# Diagnóstico de altura, mismo cálculo de antes pero ahora con la
		# posición fija que ya conocemos -- piso real (rayo a Cesium) contra
		# dónde termina de verdad la base del modelo ya escalado/rotado.
		var space_state := get_world_3d().direct_space_state
		var consulta := PhysicsRayQueryParameters3D.create(pos + arriba * 2000.0, pos - arriba * 2000.0)
		consulta.collision_mask = 1
		var resultado := space_state.intersect_ray(consulta)
		var altura_piso_real: Vector3 = resultado["position"] if resultado.size() > 0 else pos
		var transform_completo: Transform3D = cuerpo.global_transform * instancia.transform
		var punto_mas_bajo_local := Vector3(caja_nativa.position.x + caja_nativa.size.x * 0.5, caja_nativa.position.y, caja_nativa.position.z + caja_nativa.size.z * 0.5)
		var punto_mas_bajo_mundo: Vector3 = transform_completo * punto_mas_bajo_local
		var diferencia_altura: float = (altura_piso_real - punto_mas_bajo_mundo).dot(arriba)
		var estado: String = "ENTERRADO" if diferencia_altura > 0.0 else "FLOTANDO"
		reporte += "%s: escala x%.2f, %s %.1fm\n" % [info_modelo["nombre"], escala, estado, abs(diferencia_altura)]
		print("🪖 ", info_modelo["nombre"], " -- escala total: ", escala, " | ", estado, ": ", abs(diferencia_altura), "m")

	etiqueta_diagnostico_vehiculo.text = reporte
	etiqueta_diagnostico_vehiculo.visible = true
	print("🪖 Prueba fija sembrada en Aeroparque, mirando al norte -- mirá el cartel grande en la parte de arriba de la pantalla")

# Busca, entre los 5 aeropuertos conocidos, el más cercano a (lat,lon) que
# todavía NO esté tomado. Devuelve {} si ya están todos tomados.
# PAUSADO 2026-10-11: hubo una prueba acotando esto a solo Aeroparque/San
# Fernando, revertida por falta de tiempo para terminar de probarla -- vuelve
# a la lista original (Aeroparque y San Fernando quedan protegidos, como
# siempre).
func _aeropuerto_mas_cercano_libre(lat: float, lon: float) -> Dictionary:
	var mejor := {}
	var mejor_dist := INF
	for nombre in ["Ezeiza", "Morón", "El Palomar", "Campo de Mayo", "San Fernando"]:
		if aeropuertos_tomados.has(nombre):
			continue
		var aeropuerto := _buscar_destino_por_nombre(nombre)
		if not aeropuerto:
			continue
		var alat: float = aeropuerto.get_meta("lat")
		var alon: float = aeropuerto.get_meta("lon")
		# Aproximación en grados nomás (no hace falta precisión de metros
		# para elegir "cuál está más cerca" en un boceto).
		var d: float = Vector2(lon - alon, lat - alat).length()
		if d < mejor_dist:
			mejor_dist = d
			mejor = {"nombre": nombre, "lat": alat, "lon": alon}
	return mejor

# Crea una unidad chica de invasión en (lat,lon), con un raycast real al
# piso (mismo patrón que industria) para que no quede enterrada ni
# flotando. "destino_forzado" permite mandarla a un aeropuerto específico
# en vez del más cercano (usado por la siembra inicial). "variante_indice"
# elige tamaño/color de VARIANTES_TAMANO_INVASION (-1 = tamaño default).
func _crear_unidad_invasion(lat: float, lon: float, destino_forzado: String = "", variante_indice: int = -1) -> void:
	if unidades_invasion.size() >= MAX_UNIDADES_INVASION:
		print("⚠️ Tope de unidades de invasión alcanzado (%d) -- no se crea ninguna más" % MAX_UNIDADES_INVASION)
		return
	var nombre_destino: String = destino_forzado
	if nombre_destino == "":
		var destino: Dictionary = _aeropuerto_mas_cercano_libre(lat, lon)
		if destino.is_empty():
			print("🏳️ Todos los aeropuertos ya están tomados -- no hay adónde mandar la unidad")
			return
		nombre_destino = destino["nombre"]
	var arriba: Vector3 = _arriba_real()
	var punto_aproximado: Vector3 = mundo._posicion_desde_lat_lon(lat, lon, 0.0)
	var space_state := get_world_3d().direct_space_state
	var consulta := PhysicsRayQueryParameters3D.create(punto_aproximado + arriba * 2000.0, punto_aproximado - arriba * 2000.0)
	# BUG REAL encontrado 2026-10-11 (reportado: "salen cuadraditos disparados
	# para el cielo como fuegos artificiales" cerca de Quilmes, con muchas
	# unidades juntas): este rayo no tenía filtro de capa, así que podía
	# chocar contra OTRA unidad de invasión (capa 2, la misma que usan todos
	# los "objetivos_armamento") en vez del terreno real -- con varias
	# unidades moviéndose cerca entre sí, el resultado era una posición
	# saltando erráticamente. Capa 1 = terreno real únicamente.
	consulta.collision_mask = 1
	var resultado := space_state.intersect_ray(consulta)
	var pos: Vector3 = (resultado["position"] if resultado.size() > 0 else punto_aproximado) + arriba * ALTURA_FLOTE_INVASION
	# BUG REAL corregido 2026-10-11 (preocupación de Pablo desde el principio
	# de la charla de los camiones: "tengo miedo que terminen apuntando para
	# el otro lado, como pasó con los aviones"): antes "adelante" era la
	# dirección del AVIÓN del jugador en el instante de la creación -- una
	# foto fija que no tenía nada que ver hacia dónde iba a caminar la
	# unidad. Ahora mira hacia su destino real desde el arranque; en
	# _actualizar_unidades_invasion() se recalcula cada cuadro a medida que
	# avanza, así que sirve para cualquier dirección sin tocar nada por
	# zona.
	var aeropuerto_destino_inicial := _buscar_destino_por_nombre(nombre_destino)
	var adelante: Vector3 = -global_transform.basis.z
	if aeropuerto_destino_inicial:
		var lat_dest_inicial: float = aeropuerto_destino_inicial.get_meta("lat")
		var lon_dest_inicial: float = aeropuerto_destino_inicial.get_meta("lon")
		var punto_destino_inicial: Vector3 = mundo._posicion_desde_lat_lon(lat_dest_inicial, lon_dest_inicial, 0.0)
		if punto_destino_inicial.distance_to(pos) > 1.0:
			adelante = (punto_destino_inicial - pos).normalized()
	# Pedido 2026-10-11: sweep de tamaño terminado -- un solo tamaño fijo
	# (el más grande probado). "variante_indice" queda sin uso, se mantiene
	# el parámetro para no romper las llamadas existentes.
	# SEGUNDA VUELTA 2026-10-11 ("tipo semáforo: rojo, amarillo, verde y el
	# otro disparo desaparece"): arrancan ROJO (recién llegados, 4 de
	# vida), no negro -- el color real de ahí en más lo da
	# _color_semaforo_invasion() en cada impacto.
	var lado: float = VARIANTES_TAMANO_INVASION[VARIANTES_TAMANO_INVASION.size() - 1]["lado"]
	var color := Color(0.9, 0.1, 0.05, 1.0)
	var color_brillo := Color(0.9, 0.1, 0.05, 1.0)
	# Pedido 2026-10-11 ("exigir más precisión, es muy fácil"): colisión más
	# ajustada (1.3x el tamaño visual) que la de blancos grandes y quietos
	# como los aeropuertos (3x).
	var unidad := _crear_objetivo_armamento_en_posicion(pos, "Invasion_%d" % Time.get_ticks_msec(), lado, adelante, arriba, color, color_brillo, 1.3)
	unidad.add_to_group("invasion")
	invasores_totales_sembrados += 1
	unidad.set_meta("vida", VIDA_UNIDAD_INVASION)
	unidad.set_meta("vida_inicial", VIDA_UNIDAD_INVASION)
	unidad.set_meta("latitud_objetivo", lat)
	unidad.set_meta("longitud_objetivo", lon)
	unidad.set_meta("destino_aeropuerto", nombre_destino)
	var etiqueta := Label3D.new()
	etiqueta.name = "VidaRestante"
	etiqueta.text = str(VIDA_UNIDAD_INVASION)
	etiqueta.font_size = 90
	etiqueta.outline_size = 14
	etiqueta.modulate = Color(1.0, 1.0, 1.0, 1.0)
	etiqueta.billboard = BaseMaterial3D.BILLBOARD_ENABLED
	etiqueta.no_depth_test = true
	etiqueta.position = Vector3(0, 0, 1.5)
	unidad.add_child(etiqueta)
	unidad.set_meta("etiqueta_vida", etiqueta)
	# PAUSADO 2026-10-11 (pedido de Pablo -- se quedaba sin tokens para seguir
	# probando en vivo, "dejá los cuadraditos negros como estaban para que
	# pueda jugar"): el reemplazo visual por el tanque 3D funciona pero no
	# llegamos a terminar de confirmarlo -- vuelve el cuadrado de siempre.
	# Reactivar esta línea para retomar donde quedamos.
	# _vestir_unidad_invasion_con_modelo_3d(unidad)
	unidades_invasion.append(unidad)

# Pedido 2026-10-11 ("sustituí los cuadrados por los tanquecitos, que vayan
# yendo a su objetivo, pero antes achicalo 10 veces y bajalo"): apaga el
# cuadrado visual (la colisión, la vida y el numerito de arriba quedan
# intactos, se siguen usando igual) y le cuelga el T-80BV ya escalado, con
# la misma corrección de eje que confirmamos en la prueba (eje_largo_es_x).
func _vestir_unidad_invasion_con_modelo_3d(unidad: StaticBody3D) -> void:
	var escena: PackedScene = load(MODELO_TANQUE_INVASION["ruta"])
	if not escena:
		return
	for hijo in unidad.get_children():
		if hijo is MeshInstance3D:
			hijo.visible = false
	var instancia: Node3D = escena.instantiate()
	var caja_nativa: AABB = _calcular_aabb_local(instancia)
	var largo_nativo: float = max(caja_nativa.size.x, caja_nativa.size.z)
	var escala: float = (LARGO_OBJETIVO_TANQUE_INVASION / largo_nativo if largo_nativo > 0.001 else 1.0) * FACTOR_ESCALA_TANQUE_INVASION
	unidad.add_child(instancia)
	var base_instancia := Basis.IDENTITY
	if MODELO_TANQUE_INVASION.get("eje_largo_es_x", false):
		base_instancia = base_instancia.rotated(Vector3.UP, deg_to_rad(90.0))
	var basis_final: Basis = base_instancia.scaled(Vector3.ONE * escala)
	var centro_xz_nativo := Vector3(caja_nativa.position.x + caja_nativa.size.x * 0.5, 0.0, caja_nativa.position.z + caja_nativa.size.z * 0.5)
	instancia.transform = Transform3D(basis_final, -(basis_final * centro_xz_nativo))

# Mueve cada unidad de invasión en línea recta (por ahora, sin ruta real)
# hacia su aeropuerto destino, con el piso corregido cada cuadro (el
# terreno real varía a lo largo del camino). Si llega, captura el
# aeropuerto.
func _actualizar_unidades_invasion(delta: float) -> void:
	if etiqueta_invasion:
		# Pedido 2026-10-11 ("si maté a 5 y son 15, que ponga 5/15"):
		# bajas/total sembrado, no solo cuántos quedan vivos ahora.
		etiqueta_invasion.text = "INVASORES: %d/%d" % [invasores_destruidos, invasores_totales_sembrados]
		etiqueta_invasion.visible = invasores_totales_sembrados > 0
	# Pedido 2026-10-11 ("que a medida que los voy liquidando, vayan
	# entrando más por la costa"): refuerzos periódicos, no solo la
	# siembra inicial.
	tiempo_hasta_proximo_refuerzo -= delta
	if tiempo_hasta_proximo_refuerzo <= 0.0:
		tiempo_hasta_proximo_refuerzo = INTERVALO_REFUERZO_INVASION
		_sembrar_refuerzo_invasion()
	var arriba: Vector3 = _arriba_real()
	var space_state := get_world_3d().direct_space_state
	var i := unidades_invasion.size() - 1
	while i >= 0:
		var unidad: Node3D = unidades_invasion[i]
		if not is_instance_valid(unidad):
			unidades_invasion.remove_at(i)
			i -= 1
			continue
		var destino_nombre: String = unidad.get_meta("destino_aeropuerto", "")
		if destino_nombre == "" or aeropuertos_tomados.has(destino_nombre):
			var lat_u: float = unidad.get_meta("latitud_objetivo")
			var lon_u: float = unidad.get_meta("longitud_objetivo")
			var nuevo_destino: Dictionary = _aeropuerto_mas_cercano_libre(lat_u, lon_u)
			if nuevo_destino.is_empty():
				i -= 1
				continue  # no quedan aeropuertos libres -- se queda quieta donde está
			unidad.set_meta("destino_aeropuerto", nuevo_destino["nombre"])
			destino_nombre = nuevo_destino["nombre"]
		var aeropuerto_destino := _buscar_destino_por_nombre(destino_nombre)
		if not aeropuerto_destino:
			i -= 1
			continue
		var lat_dest: float = aeropuerto_destino.get_meta("lat")
		var lon_dest: float = aeropuerto_destino.get_meta("lon")
		var lat_actual: float = unidad.get_meta("latitud_objetivo")
		var lon_actual: float = unidad.get_meta("longitud_objetivo")
		var metros_por_grado_lat := 111320.0
		var metros_por_grado_lon: float = 111320.0 * cos(deg_to_rad(lat_actual))
		var distancia: float = Vector2((lon_dest - lon_actual) * metros_por_grado_lon, (lat_dest - lat_actual) * metros_por_grado_lat).length()
		if distancia < RADIO_CAPTURA_AEROPUERTO:
			_capturar_aeropuerto(destino_nombre)
			objetivos_armamento.erase(unidad)
			unidad.queue_free()
			unidades_invasion.remove_at(i)
			i -= 1
			continue
		var avance: float = velocidad_invasion * delta
		var fraccion: float = clamp(avance / distancia, 0.0, 1.0)
		var nueva_lat: float = lat_actual + (lat_dest - lat_actual) * fraccion
		var nueva_lon: float = lon_actual + (lon_dest - lon_actual) * fraccion
		unidad.set_meta("latitud_objetivo", nueva_lat)
		unidad.set_meta("longitud_objetivo", nueva_lon)
		var punto_aproximado: Vector3 = mundo._posicion_desde_lat_lon(nueva_lat, nueva_lon, 0.0)
		var consulta := PhysicsRayQueryParameters3D.create(punto_aproximado + arriba * 2000.0, punto_aproximado - arriba * 2000.0)
		consulta.collision_mask = 1  # solo terreno real -- ver comentario en _crear_unidad_invasion
		var resultado := space_state.intersect_ray(consulta)
		unidad.global_position = (resultado["position"] if resultado.size() > 0 else punto_aproximado) + arriba * ALTURA_FLOTE_INVASION
		# Reorienta cada cuadro hacia el destino actual (ver comentario en
		# _crear_unidad_invasion) -- así sirve para cualquier rumbo, incluso
		# si el destino cambia a mitad de camino (aeropuerto tomado por otro
		# lado, reasignación más arriba en esta misma función).
		var punto_destino_mundo: Vector3 = mundo._posicion_desde_lat_lon(lat_dest, lon_dest, 0.0)
		unidad.look_at(unidad.global_position + (punto_destino_mundo - unidad.global_position).normalized(), arriba)
		i -= 1

# Un aeropuerto queda "tomado": marca grande marrón con 40 de vida, y larga
# 5 unidades nuevas hacia otros aeropuertos todavía libres.
func _capturar_aeropuerto(nombre: String) -> void:
	if aeropuertos_tomados.has(nombre):
		return
	aeropuertos_tomados[nombre] = true
	print("🚩 Aeropuerto TOMADO por la invasión: ", nombre)
	var aeropuerto := _buscar_destino_por_nombre(nombre)
	if not aeropuerto:
		return
	var lat: float = aeropuerto.get_meta("lat")
	var lon: float = aeropuerto.get_meta("lon")
	var arriba: Vector3 = _arriba_real()
	var pos: Vector3 = mundo._posicion_desde_lat_lon(lat, lon, 0.0) + arriba * 120.0
	var marca := _crear_objetivo_armamento_en_posicion(pos, "AeropuertoTomado_" + nombre, 220.0, -global_transform.basis.z, arriba, Color(0.45, 0.28, 0.12, 1.0))
	marca.add_to_group("aeropuerto_invadido")
	marca.set_meta("vida", VIDA_CAPTURA_AEROPUERTO)
	marca.set_meta("vida_inicial", VIDA_CAPTURA_AEROPUERTO)
	marca.set_meta("latitud_objetivo", lat)
	marca.set_meta("longitud_objetivo", lon)
	marca.set_meta("altura_objetivo", 120.0)
	marca.set_meta("nombre_aeropuerto_tomado", nombre)
	var etiqueta := Label3D.new()
	etiqueta.name = "VidaRestante"
	etiqueta.text = str(VIDA_CAPTURA_AEROPUERTO)
	etiqueta.font_size = 150
	etiqueta.outline_size = 20
	etiqueta.modulate = Color(1.0, 1.0, 1.0, 1.0)
	etiqueta.billboard = BaseMaterial3D.BILLBOARD_ENABLED
	etiqueta.no_depth_test = true
	etiqueta.position = Vector3(0, 0, 1.5)
	marca.add_child(etiqueta)
	marca.set_meta("etiqueta_vida", etiqueta)
	for j in range(5):
		_crear_unidad_invasion(lat, lon)

# Pedido 2026-10-11 ("no esperemos a que desembarque un barco, de aburrido
# voy a matar a alguno -- que ya arranque la invasión desde tierra. Más
# cantidad, que entren por varias zonas distintas -- Quilmes, Olivos, cerca
# de Aeroparque y cerca de San Fernando -- para dispersarse como ejércitos
# reales, no todos en fila. Que NO tomen Aeroparque ni San Fernando -- esos
# son fáciles y no me puedo defender en todos lados. Que vayan a Morón,
# Ezeiza y El Palomar"): 4 zonas de entrada, cada una manda unidades a los
# 3 destinos permitidos, con offsets al azar (dispersión) y las 5 variantes
# de tamaño/color para que Pablo elija cuál queda mejor.
# PAUSADO 2026-10-11: hubo una prueba acotada a solo Quilmes->Aeroparque/San
# Fernando (para probar el tanque 3D en línea recta), revertida por falta
# de tiempo para terminar de confirmarla -- vuelve a esto, que ya sabemos
# que anda bien.
const ZONAS_SIEMBRA_INICIAL := [
	{"nombre": "Quilmes", "lat": -34.706667, "lon": -58.244444},
	{"nombre": "Olivos", "lat": -34.5095, "lon": -58.4875},
	{"nombre": "cerca de Aeroparque", "lat": -34.5800, "lon": -58.4000},
	{"nombre": "cerca de San Fernando", "lat": -34.4700, "lon": -58.5600},
]
func _sembrar_invasion_inicial() -> void:
	var objetivos_primera_oleada := ["Morón", "Ezeiza", "El Palomar"]
	var variante := 0
	var total := 0
	for zona in ZONAS_SIEMBRA_INICIAL:
		for nombre_destino in objetivos_primera_oleada:
			for j in range(3):  # pedido 2026-10-11: "más cantidad de invasores"
				var offset_lat: float = randf_range(-0.025, 0.025)
				var offset_lon: float = randf_range(-0.025, 0.025)
				_crear_unidad_invasion(float(zona["lat"]) + offset_lat, float(zona["lon"]) + offset_lon, nombre_destino, variante % VARIANTES_TAMANO_INVASION.size())
				variante += 1
				total += 1
	print("🪖 Invasión inicial sembrada en 4 zonas (Quilmes, Olivos, cerca de Aeroparque, cerca de San Fernando) -- %d unidades hacia Morón/Ezeiza/El Palomar, tamaños mezclados para elegir" % total)

# Pedido 2026-10-11 ("que a medida que los voy liquidando vayan entrando
# más por la costa"): tanda chica de refuerzos cada INTERVALO_REFUERZO_
# INVASION segundos, desde una zona al azar, hacia un destino libre al
# azar (respeta MAX_UNIDADES_INVASION -- _crear_unidad_invasion ya corta
# sola si se llega al tope).
func _sembrar_refuerzo_invasion() -> void:
	var objetivos_posibles := ["Morón", "Ezeiza", "El Palomar"].filter(func(n): return not aeropuertos_tomados.has(n))
	if objetivos_posibles.is_empty():
		print("🏳️ Refuerzo de invasión cancelado -- ya no quedan aeropuertos libres entre los permitidos")
		return
	var zona: Dictionary = ZONAS_SIEMBRA_INICIAL[randi() % ZONAS_SIEMBRA_INICIAL.size()]
	var creadas := 0
	for j in range(UNIDADES_POR_REFUERZO):
		var nombre_destino: String = objetivos_posibles[randi() % objetivos_posibles.size()]
		var offset_lat: float = randf_range(-0.025, 0.025)
		var offset_lon: float = randf_range(-0.025, 0.025)
		if unidades_invasion.size() >= MAX_UNIDADES_INVASION:
			break
		_crear_unidad_invasion(float(zona["lat"]) + offset_lat, float(zona["lon"]) + offset_lon, nombre_destino)
		creadas += 1
	print("🪖 Refuerzo de invasión -- %d unidades nuevas entrando por %s" % [creadas, zona["nombre"]])

# DEBUG TEMPORAL 2026-10-11 (ver bloque H en _input): activa el
# contraataque de los 3 barcos de la flota real más lejanos al avión, SIN
# impactarlos (no toca "vida" ni "activado" -- si después los herís de
# verdad, van a seguir contando sus 10 golpes normal).
# DEBUG TEMPORAL 2026-10-11 (ver bloque G en _input): fuerza el desembarco
# del barco real más cercano al avión, sin esperar a que llegue solo a la
# costa (a su velocidad normal tardaría demasiado para probar esta noche).
func _forzar_desembarco_de_prueba() -> void:
	var mas_cercano: Node3D = null
	var mejor_distancia := INF
	for barco in objetivos_barcos:
		if is_instance_valid(barco) and barco.is_in_group("mision_barcos"):
			var d: float = global_position.distance_to(barco.global_position)
			if d < mejor_distancia:
				mejor_distancia = d
				mas_cercano = barco
	if not mas_cercano:
		print("🧪 DEBUG G -- no hay ningún barco real para desembarcar")
		return
	print("🧪 DEBUG G -- forzando desembarco de ", mas_cercano.name, " (distancia: %.0fm)" % mejor_distancia)
	_desembarcar_barco(mas_cercano)

# DEBUG TEMPORAL 2026-10-11 (ver bloque P en _input): diagnóstico del avión
# puesto ahora mismo -- AABB real (en espacio LOCAL de modelo_externo, SIN
# la escala de TIPOS_AVION aplicada todavía, mismo criterio que usa
# _calcular_aabb_local en todo el resto del proyecto), posición global, y
# distancia/posición de la cámara respecto al avión. Si el AABB queda muy
# lejos de (0,0,0), el modelo tiene el pivote descentrado (como nos pasó
# con el aquitaine) y por eso no se ve aunque la escala esté bien.
func _diagnosticar_modelo_avion_actual() -> void:
	if not modelo_externo or modelo_externo.get_child_count() == 0:
		print("🧪 DEBUG P -- no hay modelo externo cargado (¿avión clásico sin GLB?)")
		return
	var caja: AABB = _calcular_aabb_local(modelo_externo)
	var escala_actual: float = TIPOS_AVION[tipo_avion_indice]["escala"]
	print("--- DIAGNÓSTICO AVIÓN: ", TIPOS_AVION[tipo_avion_indice]["nombre"], " ---")
	print("Escala aplicada: ", escala_actual)
	print("AABB nativo (sin escalar) -- tamaño: ", caja.size, " | posición (centro del pivote 0,0,0 si está bien centrado): ", caja.position)
	print("AABB tamaño YA escalado (tamaño real en el juego): ", caja.size * escala_actual)
	print("Avión global_position: ", global_position, " | visible: ", modelo_externo.visible)
	print("Cámara global_position: ", camara.global_position, " | distancia avión-cámara: ", global_position.distance_to(camara.global_position))

func _activar_contraataque_de_prueba() -> void:
	print("🧪 DEBUG H presionada -- objetivos_barcos totales: ", objetivos_barcos.size(), " | ya activados: ", barcos_activados.size())
	var candidatos: Array[Node3D] = []
	for barco in objetivos_barcos:
		if is_instance_valid(barco) and barco.is_in_group("mision_barcos"):
			candidatos.append(barco)
	print("🧪 DEBUG H -- candidatos válidos en grupo mision_barcos: ", candidatos.size())
	if candidatos.is_empty():
		print("🧪 DEBUG H -- no hay ningún barco real todavía (¿no se terminó de cargar la flota?)")
		return
	# BUG REAL encontrado 2026-10-11 (reportado: "nunca vi las balas"): acá
	# se elegían los 3 MÁS LEJANOS de TODA la flota (que puede estar a 30+
	# km, mucho más que el alcance real de la bala). Se filtra primero a
	# los que están a una distancia donde la bala realmente puede llegar
	# (bastante menos que ALCANCE_MAXIMO_BALA_BARCO, con margen), y de ESOS
	# se eligen los más lejanos -- así da tiempo de reacción sin ser
	# imposible de alcanzar.
	const DISTANCIA_MAXIMA_PARA_PRUEBA_H := 2500.0
	var candidatos_en_rango: Array[Node3D] = candidatos.filter(func(b): return global_position.distance_to(b.global_position) <= DISTANCIA_MAXIMA_PARA_PRUEBA_H)
	if candidatos_en_rango.is_empty():
		print("🧪 DEBUG H -- ningún barco a menos de %dm, usando los más cercanos de todos" % int(DISTANCIA_MAXIMA_PARA_PRUEBA_H))
		candidatos_en_rango = candidatos
		candidatos_en_rango.sort_custom(func(a, b): return global_position.distance_to(a.global_position) < global_position.distance_to(b.global_position))
	else:
		candidatos_en_rango.sort_custom(func(a, b): return global_position.distance_to(a.global_position) > global_position.distance_to(b.global_position))
	candidatos = candidatos_en_rango
	var activados := 0
	for barco in candidatos:
		if activados >= 3:
			break
		if not barcos_activados.has(barco):
			print("🧪 DEBUG H -- activando: ", barco.name, " (distancia: %.0fm, vida: %s)" % [global_position.distance_to(barco.global_position), str(barco.get_meta("vida", "SIN META VIDA"))])
			_activar_contraataque_barco(barco)
		else:
			print("🧪 DEBUG H -- ", barco.name, " ya estaba activado")
		activados += 1

# Pedido 2026-10-11: un barco recién impactado por primera vez queda
# "armado" -- un instante de carga (para que se note que empezó a
# apuntarte) y arranca su primera ráfaga.
func _activar_contraataque_barco(barco: Node3D) -> void:
	barco.set_meta("rafaga_temporizador", CARGA_INICIAL_ATAQUE_BARCO)
	barco.set_meta("rafaga_balas_restantes", RAFAGA_TAMANO_BARCO)
	barcos_activados.append(barco)

# Avanza el reloj de cada barco activado: cuenta regresiva -> dispara una
# bala de la ráfaga -> cuando se acaban las 3, enfriamiento más largo antes
# de la próxima ráfaga. Todo por metadata, sin un Timer por barco.
func _actualizar_contraataque_barcos(delta: float) -> void:
	var i := barcos_activados.size() - 1
	while i >= 0:
		var barco: Node3D = barcos_activados[i]
		if not is_instance_valid(barco):
			barcos_activados.remove_at(i)
			i -= 1
			continue
		var temporizador: float = barco.get_meta("rafaga_temporizador", 0.0) - delta
		if temporizador <= 0.0:
			var balas_restantes: int = barco.get_meta("rafaga_balas_restantes", 0)
			if balas_restantes > 0:
				_disparar_bala_barco(barco)
				barco.set_meta("rafaga_balas_restantes", balas_restantes - 1)
				barco.set_meta("rafaga_temporizador", INTERVALO_ENTRE_BALAS_RAFAGA)
			else:
				barco.set_meta("rafaga_balas_restantes", RAFAGA_TAMANO_BARCO)
				barco.set_meta("rafaga_temporizador", ENFRIAMIENTO_ENTRE_RAFAGAS)
		else:
			barco.set_meta("rafaga_temporizador", temporizador)
		i -= 1

# Pedido 2026-10-11 ("que no sea una sola bala fácil de esquivar, que sean
# cañonazos de a tres -- cada uno reapuntando a donde estoy en ESE
# instante, no que me persigan como un misil"): bala recta y visible, sin
# homing DESPUÉS de salir -- el "reapuntado" pasa entre bala y bala de la
# ráfaga (cada llamada a esta función apunta a la posición ACTUAL del
# avión), no durante el vuelo de cada bala individual.
func _disparar_bala_barco(barco: Node3D) -> void:
	# Pedido 2026-10-11 ("en los aeropuertos, que disparen desde las dos
	# cabeceras de pista, como cañones a cada lado"): si el blanco tiene
	# "origenes_disparo" (puntos fijos, ver _crear_objetivos_armamento),
	# dispara UNA bala desde CADA uno -- eso son los aeropuertos. Si no
	# tiene esa metadata, usa la posición actual del blanco (eso son los
	# barcos, que se mueven).
	var origenes: Array = barco.get_meta("origenes_disparo", [])
	if origenes.is_empty():
		origenes = [barco.global_position + _arriba_real() * 10.0]
	for origen in origenes:
		var direccion: Vector3 = (global_position - origen)
		if direccion.length() < 0.5:
			continue
		direccion = direccion.normalized()
		var bala := MeshInstance3D.new()
		bala.name = "BalaBarco"
		var esfera := SphereMesh.new()
		esfera.radius = 1.5
		esfera.height = 3.0
		bala.mesh = esfera
		var material := StandardMaterial3D.new()
		material.shading_mode = BaseMaterial3D.SHADING_MODE_UNSHADED
		material.albedo_color = Color(1.0, 0.45, 0.05, 1.0)
		material.emission_enabled = true
		material.emission = Color(1.0, 0.35, 0.02)
		material.emission_energy_multiplier = 5.0
		bala.material_override = material
		mundo.add_child(bala)
		bala.global_position = origen
		balas_barcos.append({"nodo": bala, "direccion": direccion, "distancia_recorrida": 0.0})
	# Sin sonido propio todavía a propósito -- reusar sonido_disparo (el
	# cañón del avión) sonaría idéntico al tiro del jugador y es un solo
	# AudioStreamPlayer compartido, se cortarían entre sí. Pendiente: sonido
	# dedicado cuando ajustemos "los efectos de las balas" (pedido explícito
	# de Pablo, para otra sesión).

# Mueve cada bala en línea recta (sin perseguir), chequea impacto contra el
# avión por proximidad, y la destruye si pasó su vida útil (tiro errado que
# se perdió de largo) o si impactó.
func _actualizar_balas_barcos(delta: float) -> void:
	var i := balas_barcos.size() - 1
	while i >= 0:
		var info: Dictionary = balas_barcos[i]
		var bala: MeshInstance3D = info["nodo"]
		if not is_instance_valid(bala):
			balas_barcos.remove_at(i)
			i -= 1
			continue
		var avance: float = velocidad_bala_barco * delta
		bala.global_position += info["direccion"] * avance
		var distancia_recorrida: float = info["distancia_recorrida"] + avance
		var distancia_al_avion: float = bala.global_position.distance_to(global_position)
		if distancia_al_avion < RADIO_IMPACTO_BALA_BARCO:
			_recibir_impacto_jugador()
			bala.queue_free()
			balas_barcos.remove_at(i)
		elif distancia_recorrida >= ALCANCE_MAXIMO_BALA_BARCO:
			bala.queue_free()
			balas_barcos.remove_at(i)
		else:
			balas_barcos[i]["distancia_recorrida"] = distancia_recorrida
		i -= 1

# Pedido 2026-10-11 ("una barrita roja de 10, por una bala no me va a sacar
# del juego"): baja un segmento del HUD. Qué pasa al llegar a 0 todavía no
# se definió -- por ahora se queda en 0 sin más efecto.
func _recibir_impacto_jugador() -> void:
	if vida_jugador <= 0:
		return
	vida_jugador -= 1
	mira_feedback_tiempo = 0.25
	if vida_jugador < segmentos_vida_jugador.size():
		var segmento: ColorRect = segmentos_vida_jugador[vida_jugador]
		if is_instance_valid(segmento):
			segmento.color = Color(0.15, 0.15, 0.15, 1.0)

# Pedido 2026-10-11 ("que cambie de color para saber si le estoy pegando --
# arranca bien oscuro/intenso, negro, después marrón, después azul, y
# cuando le faltan dos tiros, amarillo/blanco, casi liquidado"): gradiente
# de 4 colores según el % de vida que le queda al blanco (cualquiera, no
# solo invasión -- barcos, aeropuertos, industria también lo heredan).
func _color_por_vida_restante(vida_restante: int, vida_inicial: int) -> Color:
	if vida_inicial <= 0:
		vida_inicial = 1
	var negro := Color(0.05, 0.05, 0.05, 1.0)
	var marron := Color(0.45, 0.28, 0.12, 1.0)
	var azul := Color(0.15, 0.45, 0.95, 1.0)
	var blanco := Color(1.0, 0.95, 0.55, 1.0)
	# Los últimos 2 impactos SIEMPRE quedan blanco/amarillo, sin importar la
	# vida inicial (pedido explícito: "cuando le falten dos tiros").
	if vida_restante <= 2:
		return blanco
	var fraccion: float = float(vida_restante) / float(vida_inicial)  # 1.0 = recién empezado, 0.0 = casi muerto
	if fraccion > 0.66:
		return negro.lerp(marron, (1.0 - fraccion) / 0.34)
	elif fraccion > 0.33:
		return marron.lerp(azul, (0.66 - fraccion) / 0.33)
	else:
		return azul.lerp(blanco, (0.33 - fraccion) / 0.33)

# Pedido 2026-10-11, SEGUNDA VUELTA ("4 tiros, tipo semáforo: rojo,
# amarillo, verde y el otro disparo desaparece"): paleta propia y más
# simple para las unidades de invasión, en vez del gradiente de 4 colores
# de _color_por_vida_restante -- con VIDA_UNIDAD_INVASION=4, cada impacto
# avanza un color del semáforo.
func _color_semaforo_invasion(vida_restante: int) -> Color:
	match vida_restante:
		3:
			return Color(0.9, 0.1, 0.05, 1.0)   # rojo
		2:
			return Color(1.0, 0.85, 0.1, 1.0)   # amarillo
		_:
			return Color(0.15, 0.9, 0.2, 1.0)   # verde (vida_restante == 1, o cualquier otro caso)

# Misión Industria, QUINTA versión (pedido 2026-10-11, elegido por Pablo
# viendo las 37 piezas desplegadas juntas en Blender: "la única que me
# interesa son dos -- building-c (dos tanques, más vistoso, poné más
# cantidad) y building-r (un tanque, parecido). Los demás son redondelitos
# que no sirven"). Se sacan todos los modelos anteriores (tanques
# redondos, chimeneas, contenedores) -- solo estos dos, pensados para
# verse como una instalación real tipo central eléctrica/nuclear.
# Pedido 2026-10-17 ("la misión Quilmes es aburrida, están uno al lado del
# otro y hay que dar muchas vueltas -- dejala en La Plata, que están bien
# distribuidos, y agregale 3 más"): se sacan los 6 de Quilmes, quedan los 4
# de La Plata que ya andaban bien + 3 nuevos, bien separados entre sí
# (misma lógica de "bien afuera del predio" que los originales).
const MISION_INDUSTRIA = [
	# --- La Plata (aeropuerto real: -34.9744, -57.8956) -- bien afuera del predio ---
	{"lat": -34.9844, "lon": -57.9056, "modelo": "building-c"},
	{"lat": -34.9644, "lon": -57.8856, "modelo": "building-c"},
	{"lat": -34.9844, "lon": -57.8756, "modelo": "building-r"},
	{"lat": -34.9644, "lon": -57.9156, "modelo": "building-c"},
	{"lat": -34.9944, "lon": -57.8956, "modelo": "building-r"},
	{"lat": -34.9744, "lon": -57.8556, "modelo": "building-c"},
	{"lat": -34.9544, "lon": -57.9056, "modelo": "building-r"},
]
const ESCALA_MODELO_INDUSTRIA := 60.0  # pedido 2026-10-11: agrandar un 100% mas, despues se achica si hace falta
# Pedido 2026-10-10 ("pintalos de flúor para verlos primero, después les
# dejamos el color real"): true = tiñe todos los modelos de industria de
# magenta brillante, bien imposible de no ver, para confirmar que el
# posicionamiento anda. Poner en false cuando esté confirmado.
const DEBUG_INDUSTRIA_FLUOR := false  # pedido 2026-10-11: ya con la luz blanca se encuentran, mostrar el color real
# Pedido 2026-10-16 ("ponerle un amarillo tenue, tipo pastel, para poder
# identificarlos y que no se nos mezcle con el resto de los edificios"):
# a diferencia de DEBUG_INDUSTRIA_FLUOR (que reemplaza TODO el material
# por un color plano chillón, solo para diagnosticar posición), esto es
# permanente y suave -- mismo mecanismo (_aplicar_material_recursivo) pero
# con un color pastel y apenas brillo, para que se note sin dejar de
# parecer un edificio real.
const COLOR_TINTE_INDUSTRIA := Color(1.0, 0.92, 0.65, 1.0)  # amarillo pastel
const VIDA_INICIAL_INDUSTRIA := 5  # cantidad de impactos para destruir uno

func _crear_mision_industria() -> void:
	var arriba: Vector3 = _arriba_real()
	# Base ortonormal con Y = vertical REAL del lugar (no Vector3.UP) --
	# misma regla de oro de siempre. X/Z son arbitrarios pero perpendiculares
	# entre sí: a un edificio/tanque no le importa para qué lado "mira".
	var referencia: Vector3 = Vector3.FORWARD
	if abs(arriba.dot(referencia)) > 0.99:
		referencia = Vector3.RIGHT
	var derecha: Vector3 = referencia.cross(arriba).normalized()
	var adelante_prop: Vector3 = arriba.cross(derecha).normalized()
	var base_vertical := Basis(derecha, arriba, adelante_prop)

	var space_state := get_world_3d().direct_space_state
	for dato in MISION_INDUSTRIA:
		var escena: PackedScene = load("res://modelos_objetivos/%s.glb" % dato["modelo"])
		if not escena:
			continue
		var instancia: Node3D = escena.instantiate()
		instancia.scale = Vector3.ONE * ESCALA_MODELO_INDUSTRIA
		# BUG REAL encontrado 2026-10-10 (reportado: "fui a Quilmes, no me
		# aparecieron"): se posicionaba con altitud 0.0 fija, como si todo el
		# terreno estuviera al nivel del mar -- Quilmes/La Plata tienen
		# elevación real de varios metros, así que quedaban enterrados bajo
		# el terreno de Cesium. Mismo arreglo que ya se usó para las luces de
		# pista (mundo.gd) y el piso del avión (_limitar_piso): un rayo real
		# hacia abajo contra la colisión física del terreno, no una altura
		# asumida.
		var punto_aproximado: Vector3 = mundo._posicion_desde_lat_lon(dato["lat"], dato["lon"], 0.0)
		var origen_rayo: Vector3 = punto_aproximado + arriba * 2000.0
		var destino_rayo: Vector3 = punto_aproximado - arriba * 2000.0
		var consulta_piso := PhysicsRayQueryParameters3D.create(origen_rayo, destino_rayo)
		consulta_piso.collision_mask = 1  # solo terreno real, ver bug de unidades de invasión más arriba
		var resultado_piso := space_state.intersect_ray(consulta_piso)
		# BUG REAL encontrado 2026-10-10, segunda vuelta (reportado: "fui al
		# aeropuerto mismo con el ILS activado, no veo nada"): el rayo al
		# piso se tira UNA sola vez, apenas arranca el juego -- a esa altura
		# Cesium todavía no cargó el terreno de Quilmes/La Plata (están lejos
		# de Aeroparque, donde arranca el avión), así que el rayo no
		# encontraba nada y caía al cálculo viejo (altura asumida). Se marca
		# "piso_confirmado=false" para que _actualizar_objetivos_armamento()
		# reintente cuadro a cuadro hasta que el terreno real ya esté
		# cargado (cuando te acerques) y recién ahí se asiente de verdad.
		var piso_confirmado: bool = resultado_piso.size() > 0
		# Pedido 2026-10-11 ("uno de La Plata se ve la luz pero no el
		# edificio -- debe ser terreno raro ahí"): margen de seguridad chico
		# (8m) para que un punto de piso ligeramente mal encontrado (una
		# pendiente, un saliente del terreno) no deje el modelo semi-enterrado.
		var posicion: Vector3 = (resultado_piso["position"] if piso_confirmado else punto_aproximado) + arriba * 8.0
		# DIAGNÓSTICO pedido 2026-10-10 ("ponele un cuadrado al lado de cada
		# asset -- si no se ve ni el cuadrado, está enterrado; si se ve el
		# cuadrado pero no el asset, el problema es el asset en sí"): el
		# cuadrado usa EXACTAMENTE la misma función ya probada de los
		# aeropuertos rojos, 30m al costado de la posición real.
		if DEBUG_INDUSTRIA_FLUOR:
			# BUG REAL encontrado 2026-10-11 (reportado: "no vi ni un solo
			# cuadrado al lado"): con el modelo agrandado a escala 15, su
			# propio tamaño probablemente se tragaba un cuadrado puesto a
			# solo 30m -- lo alejamos mucho más, bien afuera de cualquier
			# modelo por más gigante que esté.
			var pos_cuadrado: Vector3 = posicion + derecha * 150.0
			var cuadrado_ref := _crear_objetivo_armamento_en_posicion(pos_cuadrado, "RefCuadrado_" + dato["modelo"], 15.0, adelante_prop, arriba, Color(1.0, 1.0, 0.0, 1.0), Color(1.0, 1.0, 0.0))
			cuadrado_ref.add_to_group("mision_industria")
		mundo.add_child(instancia)
		instancia.global_transform = Transform3D(base_vertical, posicion)
		if DEBUG_INDUSTRIA_FLUOR:
			# Pedido explícito 2026-10-10 ("pintalos de flúor para
			# visualizarlos primero, después les dejamos el color real"):
			# tinte bien visible mientras confirmamos que el posicionamiento
			# anda -- sacar DEBUG_INDUSTRIA_FLUOR (una sola constante, más
			# abajo) para volver al color normal de cada modelo.
			var material_fluor := StandardMaterial3D.new()
			material_fluor.shading_mode = BaseMaterial3D.SHADING_MODE_UNSHADED
			material_fluor.albedo_color = Color(1.0, 0.0, 1.0, 1.0)
			material_fluor.emission_enabled = true
			material_fluor.emission = Color(1.0, 0.0, 1.0)
			material_fluor.emission_energy_multiplier = 3.0
			_aplicar_material_recursivo(instancia, material_fluor)
		else:
			# Pedido 2026-10-16 (ver COLOR_TINTE_INDUSTRIA más arriba): tinte
			# pastel permanente, bien suave -- se nota al pasar volando pero
			# no grita como el flúor de diagnóstico.
			var material_industria := StandardMaterial3D.new()
			material_industria.albedo_color = COLOR_TINTE_INDUSTRIA
			_aplicar_material_recursivo(instancia, material_industria)

		# Colisión a medida: calculamos la caja delimitadora REAL del modelo
		# ya escalado (estos Kenney .glb son mallas simples, sin huesos, así
		# que esto sí funciona bien -- distinto del problema que tuvimos con
		# el avión de guerra).
		var cuerpo := StaticBody3D.new()
		cuerpo.name = "Industria_" + dato["modelo"] + "_" + str(objetivos_industria.size())
		cuerpo.add_to_group("objetivos_armamento")
		cuerpo.add_to_group("mision_industria")
		cuerpo.collision_layer = 2
		cuerpo.collision_mask = 0
		mundo.add_child(cuerpo)
		cuerpo.global_transform = Transform3D(base_vertical, posicion)
		instancia.get_parent().remove_child(instancia)
		cuerpo.add_child(instancia)
		instancia.transform = Transform3D(Basis.IDENTITY.scaled(Vector3.ONE * ESCALA_MODELO_INDUSTRIA), Vector3.ZERO)

		# BUG propio encontrado al escribir esto: _calcular_aabb_local()
		# devuelve el tamaño NATIVO de la malla (no aplica la escala de
		# "instancia" porque la función ignora el transform del nodo raíz
		# que le pasamos, solo usa el de sus hijos) -- sin corregir, la caja
		# de colisión quedaría 3 veces más chica que lo que se ve. Se
		# multiplica a mano por la misma escala que se le aplicó al modelo.
		var caja: AABB = _calcular_aabb_local(instancia)
		caja.position *= ESCALA_MODELO_INDUSTRIA
		caja.size *= ESCALA_MODELO_INDUSTRIA
		var forma := CollisionShape3D.new()
		var caja_forma := BoxShape3D.new()
		if caja.size.length() > 0.001:
			caja_forma.size = caja.size * 1.1  # un poco de margen
			forma.position = caja.position + caja.size * 0.5
		else:
			caja_forma.size = Vector3(8, 8, 8) * ESCALA_MODELO_INDUSTRIA
		forma.shape = caja_forma
		cuerpo.add_child(forma)

		# Numerito de "vida" flotando arriba del modelo (billboard, como los
		# blancos cuadrados) -- se actualiza con cada impacto no letal.
		var etiqueta_vida := Label3D.new()
		etiqueta_vida.name = "VidaRestante"
		etiqueta_vida.text = str(VIDA_INICIAL_INDUSTRIA)
		etiqueta_vida.font_size = 72
		etiqueta_vida.outline_size = 12
		etiqueta_vida.modulate = Color(1.0, 0.9, 0.2, 1.0)
		etiqueta_vida.billboard = BaseMaterial3D.BILLBOARD_ENABLED
		etiqueta_vida.no_depth_test = true
		etiqueta_vida.position = Vector3(0, max(caja.size.y, 4.0) + 2.0, 0)
		cuerpo.add_child(etiqueta_vida)

		# BUG REAL encontrado 2026-10-11 (reportado: "fui a La Plata, se ve la
		# luz pero no encuentro nada" -- en realidad: "la luz de industria
		# era roja, de noche se pierde contra el cielo oscuro; la de los
		# barcos es blanca y ESA sí se ve de lejos"): misma luz blanca que
		# los barcos, reusando _crear_luz_navegacion (que además ya trae el
		# arreglo de capa nocturna) y el mismo parpadeo suave de la
		# estroboscópica, en vez de un apagado/prendido brusco aparte.
		var baliza := _crear_luz_navegacion(Vector3(0, max(caja.size.y, 4.0) + 12.0, 0), Color(1.0, 1.0, 1.0), cuerpo, 3.0)
		baliza.name = "BalizaParpadeante"

		# Pedido 2026-10-17 ("ponerle un cuadradito abajo o al costado de la
		# luz, para que no quede la luz sola si el edificio no se llega a
		# ver"): PURAMENTE visual -- sin colisión ni "vida" propia, no es un
		# blanco nuevo para disparar, solo un refuerzo pegado al mismo cuerpo
		# que ya se dispara, para nunca depender solo del modelo 3D.
		var malla_cuadrado_ref := MeshInstance3D.new()
		malla_cuadrado_ref.name = "CuadradoReferencia"
		var caja_cuadrado_ref := BoxMesh.new()
		caja_cuadrado_ref.size = Vector3(25, 25, 2)
		malla_cuadrado_ref.mesh = caja_cuadrado_ref
		var material_cuadrado_ref := StandardMaterial3D.new()
		material_cuadrado_ref.shading_mode = BaseMaterial3D.SHADING_MODE_UNSHADED
		material_cuadrado_ref.billboard_mode = BaseMaterial3D.BILLBOARD_ENABLED
		material_cuadrado_ref.albedo_color = COLOR_TINTE_INDUSTRIA
		material_cuadrado_ref.emission_enabled = true
		material_cuadrado_ref.emission = COLOR_TINTE_INDUSTRIA
		material_cuadrado_ref.emission_energy_multiplier = 1.5
		malla_cuadrado_ref.material_override = material_cuadrado_ref
		malla_cuadrado_ref.position = Vector3(20, max(caja.size.y, 4.0) + 12.0, 0)
		cuerpo.add_child(malla_cuadrado_ref)

		cuerpo.set_meta("vida", VIDA_INICIAL_INDUSTRIA)
		cuerpo.set_meta("vida_inicial", VIDA_INICIAL_INDUSTRIA)
		cuerpo.set_meta("etiqueta_vida", etiqueta_vida)
		# BUG REAL encontrado 2026-10-10: faltaba esta metadata -- sin ella,
		# _actualizar_objetivos_armamento() NUNCA reubicaba estos objetos
		# cuando el origen se recentra (pasa todo el tiempo mientras volás).
		# Quedaban fijos en coordenadas de MOTOR, no de GPS real -- con el
		# tiempo se iban a la deriva de su posición real de verdad.
		cuerpo.set_meta("latitud_objetivo", dato["lat"])
		cuerpo.set_meta("longitud_objetivo", dato["lon"])
		# "altura_objetivo" guarda cuánto por encima del punto lat/lon/alt=0
		# está el piso real encontrado (o 0.3 si todavía no se confirmó
		# ningún piso real -- ver el reintento en _actualizar_objetivos_armamento).
		var altura_sobre_referencia: float = 0.3
		if piso_confirmado:
			altura_sobre_referencia = (resultado_piso["position"] - punto_aproximado).dot(arriba) + 0.3
		cuerpo.set_meta("altura_objetivo", altura_sobre_referencia)
		cuerpo.set_meta("piso_confirmado", piso_confirmado)
		objetivos_armamento.append(cuerpo)
		objetivos_industria.append(cuerpo)

func _crear_objetivo_armamento_en_posicion(posicion: Vector3, nombre: String, lado: float, adelante: Vector3, arriba: Vector3, color: Color = Color(1.0, 0.18, 0.03, 1.0), color_brillo: Color = Color(-1.0, -1.0, -1.0), multiplicador_colision: float = 3.0) -> StaticBody3D:
	var objetivo := StaticBody3D.new()
	objetivo.name = nombre
	objetivo.add_to_group("objetivos_armamento")
	objetivo.collision_layer = 2
	objetivo.collision_mask = 0
	var malla := MeshInstance3D.new()
	var cubo := BoxMesh.new()
	cubo.size = Vector3(lado, lado, 2.0)
	malla.mesh = cubo
	var material := StandardMaterial3D.new()
	material.shading_mode = BaseMaterial3D.SHADING_MODE_UNSHADED
	material.billboard_mode = BaseMaterial3D.BILLBOARD_ENABLED
	material.albedo_color = color
	material.emission_enabled = true
	# Si no se pasa un color de brillo aparte, se deriva del propio color
	# (como antes). Hace falta un color de brillo APARTE para los blancos
	# oscuros/negros (pedido 2026-10-07: barcos negros) -- si el brillo
	# también sale de un color casi negro, el cuadradito queda invisible.
	if color_brillo.r < 0.0:
		color_brillo = Color(color.r * 0.3, color.g * 0.3, color.b * 0.3)
	material.emission = color_brillo
	material.emission_energy_multiplier = 3.0
	malla.material_override = material
	objetivo.add_child(malla)
	# Pedido 2026-10-11 ("que cambie de color EL CUADRADO, no el número"):
	# se guarda el material como meta para que el manejador de impactos
	# pueda recolorearlo en vivo según la vida restante.
	objetivo.set_meta("material_cuadrado", material)
	var forma := CollisionShape3D.new()
	var caja := BoxShape3D.new()
	# Área de impacto algo mayor que lo visible para que la prueba no exija
	# acertar un píxel exacto en el centro. "multiplicador_colision" más
	# chico = hace falta apuntar más preciso (pedido 2026-10-11 para los
	# cuadraditos de invasión: "es muy fácil, casi no hay que corregir la
	# puntería aunque el blanco y el avión se estén moviendo").
	var lado_colision: float = lado * multiplicador_colision
	caja.size = Vector3(lado_colision, lado_colision, 200.0)
	forma.shape = caja
	objetivo.add_child(forma)
	mundo.add_child(objetivo)
	objetivo.global_position = posicion
	objetivo.look_at(posicion + adelante, arriba)
	objetivos_armamento.append(objetivo)
	return objetivo

func _actualizar_objetivos_armamento() -> void:
	var arriba: Vector3 = _arriba_real()
	var space_state := get_world_3d().direct_space_state
	for objetivo in objetivos_armamento:
		if not is_instance_valid(objetivo) or not objetivo.has_meta("latitud_objetivo"):
			continue
		if objetivo.is_in_group("mision_industria"):
			# El parpadeo en sí ahora lo hace _actualizar_estroboscopica()
			# (mismo mecanismo que barcos/avión) -- acá solo el tamaño según
			# distancia.
			var baliza: Node3D = objetivo.get_node_or_null("BalizaParpadeante")
			if baliza:
				_escalar_baliza_por_distancia(baliza, baliza.global_position)
		if objetivo.is_in_group("mision_barcos"):
			continue  # los barcos se actualizan aparte, en _actualizar_barcos (se mueven)
		# BUG REAL encontrado 2026-10-11 (reportado: "no arranca, error Nil a
		# float" -- pantallazo del depurador de Godot): las unidades de
		# invasión tienen "latitud_objetivo"/"longitud_objetivo" pero NUNCA
		# "altura_objetivo" (su altura la define el raycast al piso en
		# _actualizar_unidades_invasion, que es quien ya las mueve cada
		# cuadro) -- esta función intentaba leer esa meta inexistente y
		# crasheaba. Se excluyen acá, igual que los barcos.
		if objetivo.is_in_group("invasion"):
			continue  # las unidades de invasión se actualizan aparte, en _actualizar_unidades_invasion
		var latitud: float = objetivo.get_meta("latitud_objetivo")
		var longitud: float = objetivo.get_meta("longitud_objetivo")
		# Pedido 2026-10-10 ("fui al aeropuerto con el ILS puesto y no veo
		# nada"): para industria, si el piso real todavía no se confirmó
		# (Cesium puede no tener cargado el terreno de esa zona recién
		# arrancado el juego, lejos de Aeroparque), reintentamos el rayo al
		# piso CADA CUADRO hasta que encuentre algo -- una vez confirmado,
		# se guarda la altura real y se deja de tirar rayos (barato).
		if objetivo.is_in_group("mision_industria") and not objetivo.get_meta("piso_confirmado", false):
			var punto_aproximado: Vector3 = mundo._posicion_desde_lat_lon(latitud, longitud, 0.0)
			var consulta := PhysicsRayQueryParameters3D.create(punto_aproximado + arriba * 2000.0, punto_aproximado - arriba * 2000.0)
			consulta.collision_mask = 1  # solo terreno real -- no chocar con unidades de invasión cercanas (capa 2)
			var resultado := space_state.intersect_ray(consulta)
			if resultado:
				# Mismo margen de seguridad de 8m que en la creación inicial.
				var altura_real: float = (resultado["position"] - punto_aproximado).dot(arriba) + 8.0
				objetivo.set_meta("altura_objetivo", altura_real)
				objetivo.set_meta("piso_confirmado", true)
				objetivo.global_position = resultado["position"] + arriba * 8.0
				continue
		var altura: float = objetivo.get_meta("altura_objetivo")
		objetivo.global_position = mundo._posicion_desde_lat_lon(latitud, longitud, 0.0) + arriba * altura

func _disparar_hacia_mira() -> Dictionary:
	var centro_pantalla: Vector2 = get_viewport().get_visible_rect().size * 0.5
	var punto_mira := centro_pantalla + mira_hud_posicion
	# El origen técnico del avión queda retrasado respecto del modelo. La
	# traza sale unos metros hacia adelante, mientras la cámara solo define
	# la dirección.
	var origen: Vector3 = global_position
	# SOLUCIÓN SIMPLIFICADA 2026-10-11 (pedido explícito: "las dos luces ya
	# están orientadas, de ahí sale UNA línea hasta el centro de la mira,
	# no hace falta nada más complicado"): si las dos luces de ala existen,
	# el origen es el PUNTO MEDIO entre ambas -- un solo número, usado para
	# todo (el rayo de impacto Y la traza visual), nada de dos segmentos
	# que convergen ni de prioridades por nivel. Más simple, menos lugares
	# donde se puede romper.
	var hay_luz_izq: bool = luz_navegacion_izq_externa and is_instance_valid(luz_navegacion_izq_externa)
	var hay_luz_der: bool = luz_navegacion_der_externa and is_instance_valid(luz_navegacion_der_externa)
	if hay_luz_izq and hay_luz_der:
		origen = (luz_navegacion_izq_externa.global_position + luz_navegacion_der_externa.global_position) * 0.5
		# DEBUG TEMPORAL 2026-10-11 (sugerido por Gemini, para el reporte
		# "el tiro sigue saliendo de atrás" pese a que las luces se ven bien
		# en pantalla): si el origen calculado queda demasiado lejos del
		# avión, algo anda mal con el orden de recentrado -- ver
		# "process_priority = 10" en _ready(). Sacar este bloque una vez
		# confirmado que el disparo sale bien.
		var distancia_sospechosa: float = global_position.distance_to(origen)
		if distancia_sospechosa > 50.0:
			print("⚠️ DEBUG DISPARO -- origen lejos del avión (", distancia_sospechosa, "m)")
			print("   Pos avión: ", global_position, " | Luz izq: ", luz_navegacion_izq_externa.global_position, " | Luz der: ", luz_navegacion_der_externa.global_position, " | Origen: ", origen)
	elif hay_luz_izq:
		origen = luz_navegacion_izq_externa.global_position
	elif hay_luz_der:
		origen = luz_navegacion_der_externa.global_position
	elif luz_punta_ala_izq and is_instance_valid(luz_punta_ala_izq) and luz_punta_ala_izq.visible:
		origen = luz_punta_ala_izq.global_position
	elif modelo_externo and is_instance_valid(modelo_externo) and modelo_externo.visible:
		# Último recurso si ni siquiera las luces existen todavía: posición
		# del modelo visual corrida hacia adelante -- sigue siendo mejor que
		# el pivote del avión (atrás de verdad).
		var frente_modelo: Vector3 = -modelo_externo.global_transform.basis.z.normalized()
		origen = modelo_externo.global_position + frente_modelo * 6.0
	var direccion: Vector3 = camara.project_ray_normal(punto_mira).normalized()
	var destino: Vector3 = origen + direccion * 5000.0
	var consulta := PhysicsRayQueryParameters3D.create(origen, destino)
	consulta.collision_mask = 2
	consulta.collide_with_areas = true
	var resultado := get_world_3d().direct_space_state.intersect_ray(consulta)
	if not resultado.is_empty() and resultado["collider"] is Node3D and resultado["collider"].is_in_group("objetivos_armamento"):
		return {"origen": origen, "destino": resultado["position"], "impacto": true, "objetivo": resultado["collider"]}

	# TOLERANCIA POR CERCANÍA, REDISEÑADA 2026-10-10 (pregunta muy válida de
	# Pablo: "si el cartel de 'fuera de alcance' detecta que le estoy dando
	# cerca, ¿por qué no cuenta directamente?"). Tenía razón -- la versión
	# anterior tenía DOS umbrales (uno que contaba, otro más generoso que
	# solo avisaba sin contar), y esa zona gris era exactamente lo que se
	# sentía como un bug. Ahora hay un solo umbral, pero ESCALA con la
	# distancia: cerca sigue siendo exigente (40m), lejos se vuelve más
	# generoso (100m) -- lo que entra en el umbral, cuenta siempre, sin
	# excepciones ni zona intermedia.
	const TOLERANCIA_CERCA := 40.0   # metros, a corta distancia
	const TOLERANCIA_LEJOS := 100.0  # metros, a partir de DISTANCIA_LEJOS
	const DISTANCIA_LEJOS := 2500.0  # metros -- a partir de acá, tolerancia máxima
	var mejor_objetivo: Node3D = null
	var mejor_distancia := TOLERANCIA_LEJOS  # cota superior para el bucle; cada blanco usa SU propia tolerancia según su distancia
	var mejor_punto := destino
	for objetivo in objetivos_armamento:
		if not is_instance_valid(objetivo):
			continue
		var centro: Vector3 = objetivo.global_position
		var hacia_centro: Vector3 = centro - origen
		var avance: float = hacia_centro.dot(direccion)
		if avance <= 0.0:
			continue  # el blanco quedó detrás del disparo
		var tolerancia_aqui: float = lerp(TOLERANCIA_CERCA, TOLERANCIA_LEJOS, clamp(avance / DISTANCIA_LEJOS, 0.0, 1.0))
		# Pedido 2026-10-11 ("le pegué una vez y después casi no hay que
		# corregir la puntería aunque el blanco y yo nos movamos -- tendría
		# que exigir más precisión"): los cuadraditos de invasión usan una
		# tolerancia bastante más chica y FIJA (no la generosa de siempre,
		# pensada para blancos grandes y quietos como los aeropuertos).
		if objetivo.is_in_group("invasion"):
			tolerancia_aqui = min(tolerancia_aqui, 22.0)
		var punto_mas_cercano: Vector3 = origen + direccion * avance
		var distancia_al_centro: float = punto_mas_cercano.distance_to(centro)
		if distancia_al_centro < tolerancia_aqui and distancia_al_centro < mejor_distancia:
			mejor_distancia = distancia_al_centro
			mejor_objetivo = objetivo
			mejor_punto = punto_mas_cercano
	if mejor_objetivo:
		return {"origen": origen, "destino": mejor_punto, "impacto": true, "objetivo": mejor_objetivo}

	return {"origen": origen, "destino": destino, "impacto": false, "objetivo": null}

# Traza visible desde el centro delantero del avión hasta el punto alcanzado.
func _crear_segmento_traza(origen: Vector3, destino: Vector3) -> void:
	var traza := MeshInstance3D.new()
	traza.name = "TrazaArmamentoPrototipo"
	var malla := BoxMesh.new()
	var distancia: float = maxf(0.05, origen.distance_to(destino))
	malla.size = Vector3(0.12, 0.12, distancia)
	traza.mesh = malla
	var material := StandardMaterial3D.new()
	material.shading_mode = BaseMaterial3D.SHADING_MODE_UNSHADED
	material.albedo_color = Color(1.0, 0.22, 0.03, 0.9)
	material.emission_enabled = true
	material.emission = Color(1.0, 0.08, 0.01)
	material.emission_energy_multiplier = 4.0
	material.transparency = BaseMaterial3D.TRANSPARENCY_ALPHA
	traza.material_override = material
	mundo.add_child(traza)
	traza.global_position = origen.lerp(destino, 0.5)
	traza.look_at(destino, _arriba_real())
	# Pedido 2026-10-11 ("de noche no se ve el láser del disparo"): mismo
	# motivo y misma solución que las balizas (ver _crear_luz_navegacion) --
	# la capa normal (1) se apaga con tonemap_exposure de noche. La traza
	# pasa a la capa de marcadores (6), que la cámara de marcadores dibuja
	# siempre a brillo completo, sin el post-proceso nocturno.
	traza.set_layer_mask_value(1, false)
	traza.set_layer_mask_value(CAPA_MARCADORES_NOCTURNOS, true)
	get_tree().create_timer(0.12).timeout.connect(func():
		if is_instance_valid(traza):
			traza.queue_free())

# Pedido 2026-10-11 ("una sola línea desde las luces hasta la mira, nada
# más complicado"): el origen YA es el punto medio entre las dos luces de
# ala (ver _disparar_hacia_mira) -- acá solo se dibuja esa única línea
# recta. Se saca el efecto de dos líneas convergiendo (2026-10-10): sumaba
# complejidad sin resolver el problema real.
func _crear_traza_armamento(origen: Vector3, destino: Vector3) -> void:
	_crear_segmento_traza(origen, destino)

# Pedido 2026-10-10 ("la traza queda fea, sacala -- mejor un puntito rojo
# que titile donde impactó la bala, en el terreno o en el blanco"): esfera
# chica, emisiva, que titila 3 veces (prendido/apagado) y desaparece sola.
# Reemplaza a _crear_traza_armamento como feedback visual del disparo.
func _crear_marca_impacto(punto: Vector3) -> void:
	# BUG REAL encontrado 2026-10-11 (reportado: "esa bolita no aparece"):
	# 0.6m de radio y medio segundo de vida total -- a la distancia real a
	# la que se dispara (cientos/miles de metros), eso es invisible aunque
	# esté ahí. Agrandada y con no_depth_test para que nunca quede tapada
	# por el terreno/otro objeto, y titila más tiempo para dar tiempo a
	# verla.
	var marca := MeshInstance3D.new()
	marca.name = "MarcaImpacto"
	var esfera := SphereMesh.new()
	esfera.radius = 5.0
	esfera.height = 10.0
	marca.mesh = esfera
	var material := StandardMaterial3D.new()
	material.shading_mode = BaseMaterial3D.SHADING_MODE_UNSHADED
	material.albedo_color = Color(1.0, 0.1, 0.05, 1.0)
	material.emission_enabled = true
	material.emission = Color(1.0, 0.05, 0.0)
	material.emission_energy_multiplier = 8.0
	material.no_depth_test = true
	marca.material_override = material
	marca.extra_cull_margin = 16384.0  # que no desaparezca si la caja de recorte automática se equivoca
	mundo.add_child(marca)
	marca.global_position = punto

	# Titila prendido/apagado 3 veces (0.2s cada estado, ~1.2s en total) y
	# se borra sola.
	var pasos := [true, false, true, false, true, false]
	var t := 0.0
	for prendido in pasos:
		get_tree().create_timer(t).timeout.connect(func():
			if is_instance_valid(marca):
				marca.visible = prendido)
		t += 0.2
	get_tree().create_timer(t).timeout.connect(func():
		if is_instance_valid(marca):
			marca.queue_free())

func _generar_sonido_disparo() -> AudioStreamWAV:
	var muestras_por_segundo := 44100
	var duracion := 0.16
	var total_muestras := int(muestras_por_segundo * duracion)
	var datos := PackedByteArray()
	datos.resize(total_muestras * 2)
	for i in total_muestras:
		var t: float = float(i) / muestras_por_segundo
		var envolvente: float = exp(-24.0 * t)
		var golpe: float = sin(TAU * (95.0 - 55.0 * t) * t) * 0.7
		var ruido: float = randf_range(-1.0, 1.0) * 0.55
		var muestra: float = clamp((golpe + ruido) * envolvente, -1.0, 1.0)
		datos.encode_s16(i * 2, int(muestra * 32767.0))
	var stream := AudioStreamWAV.new()
	stream.format = AudioStreamWAV.FORMAT_16_BITS
	stream.mix_rate = muestras_por_segundo
	stream.stereo = false
	stream.data = datos
	return stream

func _generar_sonido_explosion() -> AudioStreamWAV:
	var muestras_por_segundo := 44100
	var duracion := 0.7
	var total_muestras := int(muestras_por_segundo * duracion)
	var datos := PackedByteArray()
	datos.resize(total_muestras * 2)
	for i in total_muestras:
		var t: float = float(i) / muestras_por_segundo
		var envolvente: float = exp(-5.8 * t)
		var golpe_grave: float = sin(TAU * (52.0 - 20.0 * t) * t) * 0.95
		var subgrave: float = sin(TAU * 31.0 * t) * 0.45
		var ruido: float = randf_range(-1.0, 1.0) * 0.8 * exp(-7.0 * t)
		var muestra: float = clamp((golpe_grave + subgrave + ruido) * envolvente, -1.0, 1.0)
		datos.encode_s16(i * 2, int(muestra * 32767.0))
	var stream := AudioStreamWAV.new()
	stream.format = AudioStreamWAV.FORMAT_16_BITS
	stream.mix_rate = muestras_por_segundo
	stream.stereo = false
	stream.data = datos
	return stream

# Arpegio corto ascendente (do-mi-sol-do agudo) para "misión completada" --
# sintetizado igual que el disparo/explosión, sin depender de ningún archivo.
func _generar_sonido_mision_completada() -> AudioStreamWAV:
	var muestras_por_segundo := 44100
	var notas := [523.25, 659.25, 783.99, 1046.50]  # Do5, Mi5, Sol5, Do6
	var duracion_nota := 0.14
	var total_muestras := int(muestras_por_segundo * duracion_nota * notas.size())
	var datos := PackedByteArray()
	datos.resize(total_muestras * 2)
	for i in total_muestras:
		var t: float = float(i) / muestras_por_segundo
		var indice_nota: int = min(int(t / duracion_nota), notas.size() - 1)
		var t_nota: float = t - indice_nota * duracion_nota
		var envolvente: float = exp(-6.0 * t_nota)
		var muestra: float = sin(TAU * notas[indice_nota] * t_nota) * 0.8 * envolvente
		datos.encode_s16(i * 2, int(clamp(muestra, -1.0, 1.0) * 32767.0))
	var stream := AudioStreamWAV.new()
	stream.format = AudioStreamWAV.FORMAT_16_BITS
	stream.mix_rate = muestras_por_segundo
	stream.stereo = false
	stream.data = datos
	return stream

func _iniciar_city_tour() -> void:
	var nodo_aeroparque := _buscar_destino_por_nombre("Aeroparque")
	var nodo_ezeiza := _buscar_destino_por_nombre("Ezeiza")
	if not nodo_aeroparque or not nodo_ezeiza:
		return
	selector_vuelo.visible = false
	cartel_central.visible = false
	panel_misiones.visible = false
	objetivo_mision = null
	indice_destino = -1
	# Pedido explícito 2026-10-01 ("poneme estrella en los cuatro
	# aeropuertos", todas juntas desde el arranque, no una por vez): sin un
	# único "próximo" destino no tiene sentido un solo rumbo sugerido -- las
	# 4 estrellas son gigantes y se ven solas, navegación a ojo.
	rumbo_guia_override_activo = false
	# Arranca en el aire, no en pista (pedido explícito, "así no tenemos
	# quilombo") -- mismo patrón que _confirmar_viaje para teletransportar.
	# BUG REAL encontrado 2026-10-01 (reportado: "apareció por Cañuelas"):
	# acá se sumaba Vector3(0, altura, 0) -- el eje Y CRUDO del motor, que en
	# Buenos Aires está inclinado ~34.5° respecto a la vertical real por la
	# curvatura de la Tierra (regla de oro del proyecto). Sumar 2000 "para
	# arriba" con ese eje torcido corre al avión casi 1150m de costado
	# ADEMÁS de no subirlo del todo -- hay que usar _arriba_real(), no Y.
	global_position = nodo_aeroparque.global_position + _arriba_real() * ALTURA_INICIO_CITY_TOUR
	look_at(nodo_ezeiza.global_position, _arriba_real())
	banco_actual = 0.0
	var ajustes_actuales: Dictionary = _ajustes_avion_actual()
	velocidad_actual = lerp(float(ajustes_actuales["vel_minima"]), float(ajustes_actuales["vel_maxima"]), 0.4)
	_actualizar_etiqueta_velocidad()
	estado = Estado.VOLANDO
	modo_city_tour = true
	estrellas_city_tour = [false, false, false, false, false]
	_refrescar_estrellas_city_tour()
	hbox_estrellas_city_tour.visible = true
	for i in RUTA_CITY_TOUR.size():
		if city_tour_estrellas_mundo[i] and is_instance_valid(city_tour_estrellas_mundo[i]):
			city_tour_estrellas_mundo[i].queue_free()
		if halos_city_tour[i] and is_instance_valid(halos_city_tour[i]):
			halos_city_tour[i].queue_free()
		halos_city_tour[i] = null
		# Precargadas con lo que el usuario ya trackeó a mano en vivo (ver
		# COORDENADAS_TRACKEADAS_CITY_TOUR) -- .duplicate() para no compartir
		# el mismo Dictionary entre la constante y el estado de la partida.
		if i < COORDENADAS_TRACKEADAS_CITY_TOUR.size():
			city_tour_trackeado[i] = COORDENADAS_TRACKEADAS_CITY_TOUR[i].duplicate()
			halos_city_tour[i] = _crear_halo_city_tour()
			halos_city_tour[i].global_position = mundo._posicion_desde_lat_lon(
				city_tour_trackeado[i]["lat"], city_tour_trackeado[i]["lon"], city_tour_trackeado[i]["alt"])
		else:
			city_tour_trackeado[i] = null
		var nodo := _buscar_destino_por_nombre(RUTA_CITY_TOUR[i])
		if not nodo:
			city_tour_estrellas_mundo[i] = null
			continue
		city_tour_lats[i] = nodo.get_meta("lat")
		city_tour_lons[i] = nodo.get_meta("lon")
		city_tour_estrellas_mundo[i] = _crear_estrella_city_tour()

# Trackea a mano la estrella INCOMPLETA mas cercana al avion en este momento
# -- con las 4 dando vueltas a la vez no hay un "checkpoint actual" unico,
# asi que se asume que el jugador esta marcando la que tiene mas cerca
# (pedido explicito: "paso, lo trackeo" uno por uno, volando la vuelta).
func _trackear_checkpoint_city_tour() -> void:
	if not modo_city_tour:
		return
	var indice_mas_cercano: int = -1
	var distancia_mas_cercana: float = INF
	for i in RUTA_CITY_TOUR.size():
		if estrellas_city_tour[i] or not city_tour_estrellas_mundo[i] or not is_instance_valid(city_tour_estrellas_mundo[i]):
			continue
		var distancia: float = global_position.distance_to(city_tour_estrellas_mundo[i].global_position)
		if distancia < distancia_mas_cercana:
			distancia_mas_cercana = distancia
			indice_mas_cercano = i
	if indice_mas_cercano < 0:
		return
	city_tour_trackeado[indice_mas_cercano] = {
		"lat": mundo.lat_avion, "lon": mundo.lon_avion, "alt": mundo.altitud_avion}
	# BUG REAL encontrado 2026-10-01 (reportado: "se colgo el juego y perdi
	# los 2 trackeos que habia hecho"): antes esto vivia solo en memoria de
	# esa partida, sin guardarse en ningun lado -- mismo motivo por el que
	# el editor de luces SI escribe a un archivo (ajustes_luces_debug.txt).
	# Ahora cada trackeo de City Tour se anota tambien, no se pierde mas.
	var texto_trackeo := "\n# City Tour \"%s\" -- %s\n\"lat\": %.6f, \"lon\": %.6f, \"alt\": %.2f\n" % [
		RUTA_CITY_TOUR[indice_mas_cercano], Time.get_datetime_string_from_system(),
		mundo.lat_avion, mundo.lon_avion, mundo.altitud_avion]
	var existente_trackeo := ""
	if FileAccess.file_exists(RUTA_AJUSTES_LUCES_DEBUG):
		existente_trackeo = FileAccess.get_file_as_string(RUTA_AJUSTES_LUCES_DEBUG)
	var archivo_trackeo := FileAccess.open(RUTA_AJUSTES_LUCES_DEBUG, FileAccess.WRITE)
	archivo_trackeo.store_string(existente_trackeo + texto_trackeo)
	archivo_trackeo.close()
	cartel_central.text = "📍 %s trackeado" % RUTA_CITY_TOUR[indice_mas_cercano]
	cartel_central.visible = true
	get_tree().create_timer(1.0).timeout.connect(func():
		if modo_city_tour:
			cartel_central.visible = false)
	# Pedido explicito 2026-10-01 ("ponele una aureola a la posicion
	# trackeada"): una vez trackeada, la deteccion de ESA estrella es por
	# distancia a un punto real (no por acercarse a la estrella en si, que
	# puede tener algo de holgura de renderizado) -- no importa desde que
	# lado te acerques.
	if halos_city_tour[indice_mas_cercano] and is_instance_valid(halos_city_tour[indice_mas_cercano]):
		halos_city_tour[indice_mas_cercano].queue_free()
	halos_city_tour[indice_mas_cercano] = _crear_halo_city_tour()

# Estrella gigante y visible de lejos (pedido explicito, tras investigar por
# que el aro de ILS reciclado no se podia atravesar por el centro: vivia en
# la "capa de marcadores nocturnos", que se renderiza en una camara aparte y
# se PEGA encima de toda la pantalla como una calcomania plana, sin
# profundidad real contra el propio avion -- de ahi la sensacion de nunca
# poder "entrar"). Una estrella en la capa NORMAL es un objeto 3D real, con
# profundidad de verdad. "no_depth_test" (mismo recurso que ya usa el cartel
# gigante del nombre de cada aeropuerto) la hace visible desde lejos aunque
# haya edificios/terreno en el medio -- pedido explicito, "que se vea apenas
# salis del aeropuerto, si no el que no conoce se pierde".
func _crear_estrella_city_tour() -> Label3D:
	var estrella := Label3D.new()
	estrella.name = "EstrellaCityTour"
	estrella.text = "⭐"
	estrella.billboard = BaseMaterial3D.BILLBOARD_ENABLED
	estrella.no_depth_test = true
	estrella.pixel_size = 2.0
	estrella.font_size = 200
	estrella.outline_size = 24
	estrella.modulate = Color(1.0, 0.85, 0.1, 1.0)
	estrella.outline_modulate = Color(0.3, 0.2, 0.0, 1.0)
	mundo.add_child(estrella)
	return estrella

# Esfera translucida en la coordenada EXACTA trackeada a mano -- ver
# comentario junto a "_trackear_checkpoint_city_tour".
func _crear_halo_city_tour() -> MeshInstance3D:
	var halo := MeshInstance3D.new()
	halo.name = "HaloCityTour"
	var malla := SphereMesh.new()
	malla.radius = RADIO_DETECCION_CITY_TOUR_TRACKEADO
	malla.height = RADIO_DETECCION_CITY_TOUR_TRACKEADO * 2.0
	halo.mesh = malla
	var color := Color(1.0, 0.85, 0.1)
	var mat := StandardMaterial3D.new()
	mat.albedo_color = Color(color.r, color.g, color.b, 0.25)
	mat.transparency = BaseMaterial3D.TRANSPARENCY_ALPHA
	mat.emission_enabled = true
	mat.emission = color
	mat.emission_energy_multiplier = 0.5
	mat.shading_mode = BaseMaterial3D.SHADING_MODE_UNSHADED
	halo.set_surface_override_material(0, mat)
	halo.set_layer_mask_value(1, false)
	halo.set_layer_mask_value(mundo.CAPA_MARCADORES_NOCTURNOS, true)
	mundo.add_child(halo)
	return halo

func _refrescar_estrellas_city_tour() -> void:
	for i in estrellas_city_tour.size():
		var etiqueta: Label = estrellas_city_tour_labels[i]
		etiqueta.add_theme_color_override("font_color",
			Color(1.0, 0.85, 0.1) if estrellas_city_tour[i] else Color(0.5, 0.5, 0.5))

# Pedido explicito 2026-10-01 ("cuando yo paso por la estrella, aparte de
# hacer el ruidito y sumarme la estrella, la estrella tiene que desaparecer
# para dar una sensacion"): a diferencia del diseno anterior (secuencial,
# una activaba la siguiente), ahora cada estrella se borra directamente al
# pasarla -- las otras (o menos) siguen ahi esperando.
func _pasar_checkpoint_city_tour(indice: int) -> void:
	estrellas_city_tour[indice] = true
	_refrescar_estrellas_city_tour()
	sonido_bonus_city_tour.play()
	var posicion_explosion: Vector3 = global_position
	if city_tour_estrellas_mundo[indice] and is_instance_valid(city_tour_estrellas_mundo[indice]):
		posicion_explosion = city_tour_estrellas_mundo[indice].global_position
		city_tour_estrellas_mundo[indice].queue_free()
	city_tour_estrellas_mundo[indice] = null
	if halos_city_tour[indice] and is_instance_valid(halos_city_tour[indice]):
		halos_city_tour[indice].queue_free()
	halos_city_tour[indice] = null
	_crear_explosion_city_tour(posicion_explosion)
	# Las 4 estrellas del tour completas -- falta volver a Aeroparque y
	# aterrizar (reusa el sistema normal de destino/aterrizaje de siempre).
	var todas_completas: bool = true
	for i in RUTA_CITY_TOUR.size():
		if not estrellas_city_tour[i]:
			todas_completas = false
			break
	if todas_completas and indice_destino < 0:
		var nodo_aeroparque := _buscar_destino_por_nombre("Aeroparque")
		if nodo_aeroparque:
			indice_destino = destinos.find(nodo_aeroparque)
		cartel_central.text = "🏁 ¡Volvé a Aeroparque y aterrizá!"
		cartel_central.visible = true
		get_tree().create_timer(3.0).timeout.connect(func():
			if modo_city_tour:
				cartel_central.visible = false)

func _completar_city_tour() -> void:
	if not estrellas_city_tour[4]:
		estrellas_city_tour[4] = true
		_refrescar_estrellas_city_tour()
		sonido_bonus_city_tour.play()
	for i in RUTA_CITY_TOUR.size():
		if city_tour_estrellas_mundo[i] and is_instance_valid(city_tour_estrellas_mundo[i]):
			city_tour_estrellas_mundo[i].queue_free()
		city_tour_estrellas_mundo[i] = null
		if halos_city_tour[i] and is_instance_valid(halos_city_tour[i]):
			halos_city_tour[i].queue_free()
		halos_city_tour[i] = null
	modo_city_tour = false

func _actualizar_city_tour(_delta: float) -> void:
	if not modo_city_tour:
		return
	for i in RUTA_CITY_TOUR.size():
		if estrellas_city_tour[i]:
			continue
		if city_tour_estrellas_mundo[i] and is_instance_valid(city_tour_estrellas_mundo[i]):
			# Reposiciona TODOS los cuadros desde lat/lon -- el origen se
			# recentra en el avion cada cuadro, y "mundo" no es descendiente
			# del CesiumGeoreference asi que nada se recentra solo.
			city_tour_estrellas_mundo[i].global_position = mundo._posicion_desde_lat_lon(city_tour_lats[i], city_tour_lons[i], 0.0) + _arriba_real() * ALTURA_ARO_CITY_TOUR
		var trackeado = city_tour_trackeado[i]
		if trackeado != null:
			var punto: Vector3 = mundo._posicion_desde_lat_lon(trackeado["lat"], trackeado["lon"], trackeado["alt"])
			if halos_city_tour[i] and is_instance_valid(halos_city_tour[i]):
				halos_city_tour[i].global_position = punto
			if global_position.distance_to(punto) < RADIO_DETECCION_CITY_TOUR_TRACKEADO:
				_pasar_checkpoint_city_tour(i)
		elif city_tour_estrellas_mundo[i] and is_instance_valid(city_tour_estrellas_mundo[i]):
			if global_position.distance_to(city_tour_estrellas_mundo[i].global_position) < RADIO_DETECCION_CITY_TOUR:
				_pasar_checkpoint_city_tour(i)

# Ráfaga corta de partículas doradas, a modo de "explosión" de bonus al
# pasar un checkpoint -- se borra sola después de terminar.
func _crear_explosion_city_tour(posicion: Vector3) -> void:
	var particulas := GPUParticles3D.new()
	particulas.amount = 24
	particulas.lifetime = 0.8
	particulas.one_shot = true
	particulas.explosiveness = 1.0
	var material := ParticleProcessMaterial.new()
	material.direction = Vector3(0, 1, 0)
	material.spread = 180.0
	material.initial_velocity_min = 8.0
	material.initial_velocity_max = 20.0
	material.gravity = Vector3(0, -9.8, 0)
	material.scale_min = 0.5
	material.scale_max = 1.5
	material.color = Color(1.0, 0.85, 0.1, 1.0)
	particulas.process_material = material
	var malla_particula := SphereMesh.new()
	malla_particula.radius = 1.0
	malla_particula.height = 2.0
	particulas.draw_pass_1 = malla_particula
	mundo.add_child(particulas)
	particulas.global_position = posicion
	particulas.emitting = true
	get_tree().create_timer(particulas.lifetime + 0.3).timeout.connect(func():
		if is_instance_valid(particulas):
			particulas.queue_free())

# Sintetiza un "bonus" de dos tonos (880Hz -> 1320Hz, 250ms) sin depender de
# ningún archivo de audio -- simple y autocontenido para esta primera prueba.
func _generar_sonido_bonus() -> AudioStreamWAV:
	var muestras_por_segundo := 44100
	var duracion := 0.25
	var total_muestras := int(muestras_por_segundo * duracion)
	var datos := PackedByteArray()
	datos.resize(total_muestras * 2)
	for i in total_muestras:
		var t: float = float(i) / muestras_por_segundo
		var frecuencia: float = 880.0 if t < duracion * 0.5 else 1320.0
		var envolvente: float = 1.0 - (t / duracion)
		var muestra: float = sin(TAU * frecuencia * t) * envolvente * 0.5
		var valor: int = int(clamp(muestra, -1.0, 1.0) * 32767.0)
		datos.encode_s16(i * 2, valor)
	var stream := AudioStreamWAV.new()
	stream.format = AudioStreamWAV.FORMAT_16_BITS
	stream.mix_rate = muestras_por_segundo
	stream.stereo = false
	stream.data = datos
	return stream

# Aviso de seguridad automático -- ver comentario junto a la declaración de
# "anuncio_seguridad_reproducido" más arriba.
func _actualizar_anuncio_seguridad(delta: float) -> void:
	if estado == Estado.VOLANDO:
		if _estado_anterior_para_timer_vuelo != Estado.VOLANDO:
			tiempo_vuelo_actual = 0.0
			anuncio_seguridad_reproducido = false
		tiempo_vuelo_actual += delta
		if not anuncio_seguridad_reproducido and tiempo_vuelo_actual >= SEGUNDOS_ANUNCIO_SEGURIDAD:
			anuncio_seguridad_reproducido = true
			if not sonido_instrucciones.playing:
				_alternar_instrucciones()
	_estado_anterior_para_timer_vuelo = estado

# Solapas del panel de instrumentos de abajo -- ver comentario junto a donde
# se conectan los botones en _ready().
func _mostrar_solapa_inferior(numero: int) -> void:
	panel_fotos_decorativas.visible = numero == 1
	panel_torre.visible = numero == 2
	panel_sensibilidad.visible = numero == 2
	panel_sonidos.visible = numero == 3
	panel_config_tab.visible = numero == 4
	panel_vuelo_tab.visible = numero == 5
	boton_solapa_1.button_pressed = numero == 1
	boton_solapa_2.button_pressed = numero == 2
	boton_solapa_3.button_pressed = numero == 3
	boton_solapa_4.button_pressed = numero == 4
	boton_solapa_5.button_pressed = numero == 5
	if panel_aterrizaje_tab:
		panel_aterrizaje_tab.visible = numero == 6
		boton_solapa_6.button_pressed = numero == 6
	if panel_mision_tab:
		panel_mision_tab.visible = numero == 7
		boton_solapa_7.button_pressed = numero == 7
	if panel_destinos_tab:
		panel_destinos_tab.visible = numero == 8
		boton_solapa_8.button_pressed = numero == 8
	if numero != 4:
		esperando_asignacion = ""

# ============================================================
# RELOJES DE LA PÁGINA 1 + VIENTO (pedido 2026-10-03)
# ============================================================

# Pedido explícito: "llename el panel de relojes, que después les damos
# función". Velocímetro/altímetro/horizonte son los únicos 3 realmente
# funcionales por ahora (RPM/combustible/temperatura/voltaje quedan fijos,
# decorativos, a la espera de implementar motor de verdad). El usuario avisó
# que no espera que el horizonte quede perfecto de entrada -- se ajusta con
# feedback en vivo, mismo proceso que todo lo demás en este proyecto.
func _actualizar_relojes_panel(_delta: float) -> void:
	# Velocímetro: aguja de -135° (0) a +135° (300 km/h), escala nominal --
	# no es un velocímetro real calibrado por avión, es decorativo con datos
	# reales.
	var fraccion_vel: float = clamp(velocidad_actual / 300.0, 0.0, 1.0)
	aguja_velocidad.rotation_degrees = -135.0 + 270.0 * fraccion_vel
	valor_velocidad_reloj.text = "%d km/h" % int(round(velocidad_actual))

	var altura_actual: float = mundo.altitud_avion + position.y - altura_piso
	var fraccion_alt: float = clamp(altura_actual / 3000.0, 0.0, 1.0)
	aguja_altitud.rotation_degrees = -135.0 + 270.0 * fraccion_alt
	valor_altitud_reloj.text = "%d m" % int(round(altura_actual))

	# Horizonte artificial simplificado: solo banco (inclinación), no
	# cabeceo -- gira el cielo/tierra al revés del banco del avión, como un
	# horizonte real (el avión se inclina, la línea de horizonte "se queda
	# quieta" respecto al mundo).
	horizonte_giro.rotation_degrees = -banco_actual

	_actualizar_indicador_viento()

func _alternar_viento() -> void:
	viento_activo = not viento_activo
	if viento_activo:
		viento_direccion_grados = randf() * 360.0
		viento_velocidad_kmh = randf_range(10.0, 35.0)
		boton_viento.text = "💨 VIENTO: ON"
	else:
		boton_viento.text = "💨 VIENTO: OFF"
	_actualizar_indicador_viento()

func _actualizar_indicador_viento() -> void:
	if not viento_activo:
		flecha_viento.visible = false
		valor_viento.text = "sin viento"
		return
	flecha_viento.visible = true
	# Misma convención que las flechitas de Torre: apunta hacia de dónde
	# sopla, relativo al rumbo actual del avión.
	flecha_viento.rotation_degrees = fposmod(viento_direccion_grados - _ultimo_rumbo_actual, 360.0)
	valor_viento.text = "%d° %d km/h" % [int(viento_direccion_grados), int(round(viento_velocidad_kmh))]
