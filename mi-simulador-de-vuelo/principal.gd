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
	{"nombre": "Boeing", "modelo": "res://modelos_aviones/boeing.glb", "helicoptero": false, "escala": 0.006, "rotacion": Vector3(0, 180, 0), "sonido": "res://FX/motor_jet.mp3"},
	# Mostraban la trompa/cabina de frente a la cámara -- la cámara va SIEMPRE
	# detrás del avión mirando hacia adelante, así que si se ve la trompa de
	# frente es que el modelo tiene el "adelante" invertido. 180° en yaw (Y).
	{"nombre": "Jet privado", "modelo": "res://modelos_aviones/jet_privado.glb", "helicoptero": false, "escala": 0.9, "rotacion": Vector3(0, 180, 0), "sonido": "res://FX/motor_jet.mp3"},
	{"nombre": "Avión de guerra", "modelo": "res://modelos_aviones/avion_de_guerra.glb", "helicoptero": false, "escala": 0.75, "rotacion": Vector3(0, 180, 0), "sonido": "res://FX/motor_jet.mp3"},
	# Orientación ya perfecta -- solo un poco más grande.
	# SIN sonido de motor (pedido 2026-09-21, "no es constante, es un
	# helicóptero cuando pasa, no mientras va volando"): el loop de Mixkit que
	# habíamos bajado era un efecto de "sobrevuelo" (crece y se aleja), no un
	# motor sostenido -- sonaba raro en loop. Se saca hasta encontrar uno que
	# sí sea un zumbido constante de verdad.
	{"nombre": "Helicóptero", "modelo": "res://modelos_aviones/helicoptero.glb", "helicoptero": true, "escala": 0.22, "rotacion": Vector3(0, 90, 0), "sonido": ""},
	# NUEVO 2026-09-26 (pedido explícito, "aunque no tengan partes animadas,
	# si son lindos los agregamos igual") -- bajado por el usuario de
	# Sketchfab, único de la nueva tanda que ya venía en .glb (los demás son
	# .fbx/.obj/.blend, necesitan más trabajo de conversión). Escala ajustada
	# tras primera prueba en vivo 2026-09-26 (usuario: "50% más chico").
	{"nombre": "Avión genérico (nuevo)", "modelo": "res://modelos_aviones/plane_generico.glb", "helicoptero": false, "escala": 0.5, "rotacion": Vector3(0, 180, 0), "sonido": "res://FX/motor_helice.mp3"},
	# NUEVO 2026-09-26, misma tanda -- estos 3 son .fbx (Godot 4 los importa
	# nativo, sin necesitar Blender instalado). CUARTA vuelta 2026-09-26:
	# tamaño del KF-30 y orientación del combate confirmados bien -- quedan.
	# KF-30 y Ka-27 quedaron mirando "para el lado del usuario" (de frente a
	# la cámara) -- giro de 180° sobre lo que ya tenían. Ka-27 duplicado de
	# tamaño, combate triplicado.
	{"nombre": "KF-30 (nuevo)", "modelo": "res://modelos_aviones/kf30.fbx", "helicoptero": false, "escala": 0.2, "rotacion": Vector3(0, 90, 0), "sonido": "res://FX/motor_jet.mp3"},
	{"nombre": "Ka-27 (nuevo)", "modelo": "res://modelos_aviones/ka27.fbx", "helicoptero": true, "escala": 1.0, "rotacion": Vector3(0, 180, 0), "sonido": "res://FX/motor_helicoptero.mp3"},
	{"nombre": "Avión de combate (nuevo)", "modelo": "res://modelos_aviones/fighter_jet_nuevo.fbx", "helicoptero": false, "escala": 0.9, "rotacion": Vector3(0, 0, 0), "sonido": "res://FX/motor_jet.mp3"},
]
var tipo_avion_indice: int = 0

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
@onready var cartel_central: Label = get_node("../HUD/CartelCentral")
@onready var origen_option: OptionButton = get_node("../HUD/SelectorVuelo/VBox/OrigenOption")
@onready var destino_option: OptionButton = get_node("../HUD/SelectorVuelo/VBox/DestinoOption")
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
const RUTA_RADIO_FONDO = "res://FX/radio_fondo.mp3"
@onready var boton_radio: Button = get_node("../HUD/BarraBotones/BotonRadio")
@onready var boton_radio_en_vivo: Button = get_node("../HUD/BarraBotones/BotonRadioEnVivo")
@onready var sonido_radio: AudioStreamPlayer = get_node("SonidoRadio")
var radio_activa: bool = false

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
@onready var boton_anterior_musica: Button = get_node("../HUD/PanelMusica/VBoxMusica/HBoxControlesMusica/BotonAnteriorMusica")
@onready var boton_pausa_musica: Button = get_node("../HUD/PanelMusica/VBoxMusica/HBoxControlesMusica/BotonPausaMusica")
@onready var boton_siguiente_musica: Button = get_node("../HUD/PanelMusica/VBoxMusica/HBoxControlesMusica/BotonSiguienteMusica")
@onready var boton_detener_musica: Button = get_node("../HUD/PanelMusica/VBoxMusica/BotonDetenerMusica")
@onready var boton_cerrar_musica: Button = get_node("../HUD/PanelMusica/VBoxMusica/BotonCerrarMusica")
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
@onready var slider_volumen_motor: HSlider = get_node("../HUD/PanelConfiguracion/VBoxConfig/HBoxVolumenMotor/SliderVolumenMotor")
const RUTA_CONFIG_AUDIO = "user://audio_config.cfg"
const VOLUMEN_MOTOR_DB_MINIMO = -40.0  # con el slider en 0, casi inaudible en vez de mudo de golpe
const VOLUMEN_MOTOR_DB_MAXIMO = -6.0   # con el slider al máximo, presente pero no estruendoso
var volumen_motor: float = 0.5  # 0..1, lo que muestra/mueve el slider
const PITCH_MOTOR_MINIMO = 0.85
const PITCH_MOTOR_MAXIMO = 1.25
@onready var linea_guia: MeshInstance3D = get_node("../LineaGuia")
@onready var label_giro_valor: Label = get_node("../HUD/PanelSensibilidad/VBoxSensibilidad/FilaGiro/LabelGiroValor")
@onready var boton_giro_menos: Button = get_node("../HUD/PanelSensibilidad/VBoxSensibilidad/FilaGiro/BotonGiroMenos")
@onready var boton_giro_mas: Button = get_node("../HUD/PanelSensibilidad/VBoxSensibilidad/FilaGiro/BotonGiroMas")
@onready var label_angulo_valor: Label = get_node("../HUD/PanelSensibilidad/VBoxSensibilidad/FilaAngulo/LabelAnguloValor")
@onready var boton_angulo_menos: Button = get_node("../HUD/PanelSensibilidad/VBoxSensibilidad/FilaAngulo/BotonAnguloMenos")
@onready var boton_angulo_mas: Button = get_node("../HUD/PanelSensibilidad/VBoxSensibilidad/FilaAngulo/BotonAnguloMas")
@onready var label_vertical_titulo: Label = get_node("../HUD/PanelSensibilidad/VBoxSensibilidad/FilaVertical/LabelVertical")
@onready var label_vertical_valor: Label = get_node("../HUD/PanelSensibilidad/VBoxSensibilidad/FilaVertical/LabelVerticalValor")
@onready var boton_vertical_menos: Button = get_node("../HUD/PanelSensibilidad/VBoxSensibilidad/FilaVertical/BotonVerticalMenos")
@onready var boton_vertical_mas: Button = get_node("../HUD/PanelSensibilidad/VBoxSensibilidad/FilaVertical/BotonVerticalMas")
@onready var label_vel_minima_valor: Label = get_node("../HUD/PanelSensibilidad/VBoxSensibilidad/FilaVelMinima/LabelVelMinimaValor")
@onready var boton_vel_minima_menos: Button = get_node("../HUD/PanelSensibilidad/VBoxSensibilidad/FilaVelMinima/BotonVelMinimaMenos")
@onready var boton_vel_minima_mas: Button = get_node("../HUD/PanelSensibilidad/VBoxSensibilidad/FilaVelMinima/BotonVelMinimaMas")
@onready var label_vel_maxima_valor: Label = get_node("../HUD/PanelSensibilidad/VBoxSensibilidad/FilaVelMaxima/LabelVelMaximaValor")
@onready var boton_vel_maxima_menos: Button = get_node("../HUD/PanelSensibilidad/VBoxSensibilidad/FilaVelMaxima/BotonVelMaximaMenos")
@onready var boton_vel_maxima_mas: Button = get_node("../HUD/PanelSensibilidad/VBoxSensibilidad/FilaVelMaxima/BotonVelMaximaMas")
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
@onready var boton_mapa: Button = get_node("../HUD/BarraBotones/BotonMapa")
@onready var mapa_viewport: SubViewport = get_node("../HUD/MapaViewport")
@onready var mapa_rect: TextureRect = get_node("../HUD/MapaRect")
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
var zoom_mapa: int = 11  # 4 niveles más alejado por defecto (pedido explícito, "el puntito se va del mapa a los pedos")
const ZOOM_MAPA_MINIMO = 3
const ZOOM_MAPA_MAXIMO = 19
var tile_x_mapa: int = -999999
var tile_y_mapa: int = -999999
var descargando_mapa: bool = false
var acumulador_mapa: float = 0.0

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

@onready var boton_configuracion: Button = get_node("../HUD/BotonConfiguracion")
@onready var panel_configuracion: Panel = get_node("../HUD/PanelConfiguracion")
@onready var etiqueta_estado_joystick: Label = get_node("../HUD/PanelConfiguracion/VBoxConfig/LabelEstadoJoystick")
@onready var lista_acciones_joystick: VBoxContainer = get_node("../HUD/PanelConfiguracion/VBoxConfig/ScrollAcciones/ListaAcciones")
@onready var boton_cerrar_config: Button = get_node("../HUD/PanelConfiguracion/VBoxConfig/BotonCerrarConfig")

# Control con mouse tipo GeoFS (pedido 2026-09-21): con esto activado, el
# centro de la pantalla es "neutro" y alejar el mouse del centro inclina/
# cabecea el avión proporcionalmente (como un joystick virtual invisible) --
# ver el uso de control_mouse_activo en _procesar_vuelo(). El acelerador
# sigue siendo siempre de teclado (W/S), en los dos modos.
@onready var check_control_mouse: CheckButton = get_node("../HUD/PanelConfiguracion/VBoxConfig/CheckControlMouse")
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
	["activar_ils", "Activar/desactivar ILS"],
	# Pedido 2026-09-26: "el otro dejalo, después le daré una función" --
	# fila reservada, asignable ya mismo, sin comportamiento todavía. Cuando
	# se defina qué hace, agregar el chequeo con _joystick_activo("reservado_1")
	# donde corresponda (mismo patrón que activar_ils/marcar_lugar arriba).
	["reservado_1", "Reservado (función futura)"],
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
var precio_mision_actual: int = 0

var destinos: Array = []
# Arranca en 1, no 0: el destino[0] ahora es "Aeroparque", que es de donde
# salís (ahí mismo arranca el avión) -- si empezara apuntando a Aeroparque,
# creería que "llegaste" apenas arrancás el juego.
var indice_destino: int = 1
var temporizador_torre: float = 0.0
var selector_poblado: bool = false

func _ready() -> void:
	print("El avión arrancó bien y el script está corriendo.")
	_crear_luces_avion_clasico()
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
	tipo_avion_option.select(0)
	tipo_avion_option.focus_mode = Control.FOCUS_NONE

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
	var stream_radio: AudioStream = load(RUTA_RADIO_FONDO)
	if stream_radio is AudioStreamMP3:
		stream_radio.loop = true
	sonido_radio.stream = stream_radio
	sonido_radio.volume_db = -12.0

	boton_radio_en_vivo.focus_mode = Control.FOCUS_NONE
	boton_radio_en_vivo.pressed.connect(func(): OS.shell_open(URL_RADIO_ASPEN))

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
	boton_configuracion.focus_mode = Control.FOCUS_NONE
	boton_cerrar_config.focus_mode = Control.FOCUS_NONE
	boton_configuracion.pressed.connect(func():
		panel_configuracion.visible = not panel_configuracion.visible)
	boton_cerrar_config.pressed.connect(func():
		panel_configuracion.visible = false
		esperando_asignacion = "")

	check_control_mouse.focus_mode = Control.FOCUS_NONE
	_cargar_control_mouse()
	check_control_mouse.button_pressed = control_mouse_activo
	check_control_mouse.toggled.connect(func(activo: bool):
		control_mouse_activo = activo
		_guardar_control_mouse())

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
		if panel_configuracion.visible:
			panel_configuracion.visible = false
			return
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
	playlist_musica.shuffle()
	indice_musica = 0
	if playlist_musica.is_empty():
		label_estado_musica.text = "No encontré temas en esa carpeta"
		return
	_reproducir_siguiente_musica()

func _reproducir_siguiente_musica() -> void:
	if playlist_musica.is_empty():
		return
	if indice_musica >= playlist_musica.size():
		playlist_musica.shuffle()
		indice_musica = 0
	var ruta: String = playlist_musica[indice_musica]
	indice_musica += 1
	var stream: AudioStream = load(ruta)
	sonido_musica.stream = stream
	sonido_musica.stream_paused = false
	sonido_musica.play()
	boton_pausa_musica.text = "⏸ Pausa"
	label_estado_musica.text = "%s: %s" % [genero_musica_actual, ruta.get_file().trim_suffix(".mp3")]

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
		sonido_radio.play()
		boton_radio.text = "📻 Radio: ON"
	else:
		sonido_radio.stop()
		boton_radio.text = "📻 Radio: OFF"

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
	_actualizar_estroboscopica(delta)
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

	# Tecla U: "botón de pánico" para el panel del mapa (pedido explícito
	# 2026-09-21, "no me quiero ir a dormir y dejarlo roto") -- el doble clic
	# no respondía, así que esto NO pasa por el mouse/GUI para nada (chequea
	# la tecla física directo, como M/N/I/O/L) -- funciona SIEMPRE, tape lo
	# que tape el panel en pantalla. Vuelve al tamaño y posición de siempre.
	var tecla_reset_mapa_activa = Input.is_physical_key_pressed(KEY_U)
	if tecla_reset_mapa_activa and not tecla_reset_mapa_anterior:
		mapa_rect.offset_left = 20.0
		mapa_rect.offset_top = 130.0
		mapa_rect.offset_right = 220.0
		mapa_rect.offset_bottom = 330.0
		_modo_resize_mapa = ModoResizeMapa.NINGUNO
		_arrastrando_mover_mapa = false
		cartel_central.text = "🗺️ Mapa restablecido"
		cartel_central.visible = true
		get_tree().create_timer(1.5).timeout.connect(func(): cartel_central.visible = false)
	tecla_reset_mapa_anterior = tecla_reset_mapa_activa

	# Tecla V: vista externa sin el avión visible/oculto (ver comentario junto
	# a la declaración de vista_sin_avion_activa, más arriba).
	var tecla_vista_sin_avion_activa = Input.is_physical_key_pressed(KEY_V)
	if tecla_vista_sin_avion_activa and not tecla_vista_sin_avion_anterior:
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
		cartel_central.text = "👁️ Vista sin avión: ON" if vista_sin_avion_activa else "👁️ Vista sin avión: OFF"
		cartel_central.visible = true
		get_tree().create_timer(1.5).timeout.connect(func(): cartel_central.visible = false)
	tecla_vista_sin_avion_anterior = tecla_vista_sin_avion_activa

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
	var adelante_mapa = -global_transform.basis.z
	adelante_mapa.y = 0
	adelante_mapa = adelante_mapa.normalized()
	var rumbo_mapa = fposmod(rad_to_deg(atan2(adelante_mapa.x, -adelante_mapa.z)), 360.0)
	icono_avion_mapa.rotation = deg_to_rad(rumbo_mapa)

	# Coordenadas EXACTAS (con decimales) dentro de la grilla de teselas --
	# la parte entera (floor) dice qué tesela mostrar, la parte fraccionaria
	# dice EXACTAMENTE dónde cae el avión adentro de esa tesela (0..1). Antes
	# el avión quedaba siempre fijo en el centro y la imagen "saltaba" debajo
	# suyo -- ahora es al revés (como GeoFS/Google Maps): la imagen se queda
	# quieta y el avioncito se desliza suavemente sobre ella, y solo cuando
	# se sale de la tesela actual se pide una nueva.
	var n: float = pow(2.0, zoom_mapa)
	var lat_rad: float = deg_to_rad(mundo.lat_avion)
	var x_exacto: float = (mundo.lon_avion + 180.0) / 360.0 * n
	var y_exacto: float = (1.0 - log(tan(lat_rad) + 1.0 / cos(lat_rad)) / PI) / 2.0 * n
	var xtile: int = int(floor(x_exacto))
	var ytile: int = int(floor(y_exacto))
	var frac_x: float = clamp(x_exacto - xtile, 0.0, 1.0)
	var frac_y: float = clamp(y_exacto - ytile, 0.0, 1.0)

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
func _mapa_rect_gui_input(event: InputEvent) -> void:
	# Doble clic = "botón de pánico" (pedido explícito 2026-09-21, "no se ve
	# el vértice de abajo, quiero poder minimizarlo yo mismo"): vuelve al
	# tamaño chico de siempre de un solo golpe, en cualquier parte del panel
	# (no hace falta encontrar la manija), así siempre hay forma de recuperar
	# el control aunque el panel haya quedado gigante por lo que sea.
	if event is InputEventMouseButton and event.pressed and event.double_click:
		mapa_rect.offset_left = 20.0
		mapa_rect.offset_top = 130.0
		mapa_rect.offset_right = 220.0
		mapa_rect.offset_bottom = 330.0
		_modo_resize_mapa = ModoResizeMapa.NINGUNO
		_arrastrando_mover_mapa = false
		return
	if event is InputEventMouseButton and event.pressed:
		if event.button_index == MOUSE_BUTTON_WHEEL_UP:
			zoom_mapa = clamp(zoom_mapa + 1, ZOOM_MAPA_MINIMO, ZOOM_MAPA_MAXIMO)
		elif event.button_index == MOUSE_BUTTON_WHEEL_DOWN:
			zoom_mapa = clamp(zoom_mapa - 1, ZOOM_MAPA_MINIMO, ZOOM_MAPA_MAXIMO)
		else:
			return
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
	var altura_cam = lerp(OFFSET_CAMARA_ALTURA, OFFSET_CAMARA_ALTURA * 3.0, factor_zoom)
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

	var velocidad_cabeceo_efectiva = VELOCIDAD_CABECEO_ENTRADA if cabeceo_objetivo != 0.0 else VELOCIDAD_CABECEO_SALIDA
	var cabeceo_anterior = cabeceo_actual
	cabeceo_actual = move_toward(cabeceo_actual, cabeceo_objetivo, velocidad_cabeceo_efectiva * delta)
	var delta_cabeceo = cabeceo_actual - cabeceo_anterior
	if abs(delta_cabeceo) > 0.0001:
		# Mismo signo que antes: "ui_up" (cabeceo positivo) sube la nariz.
		rotate_object_local(Vector3.RIGHT, -deg_to_rad(delta_cabeceo))

	var velocidad_banco = VELOCIDAD_BANCO_ENTRADA if banco_objetivo != 0.0 else VELOCIDAD_BANCO_SALIDA
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
	if abs(banco_actual) > 0.5:
		global_rotate(_arriba_real(), deg_to_rad(banco_actual * ajustes["giro"] * delta))

	# Acelerador: W sube la velocidad de crucero, S la baja, dentro de un
	# rango fijo (arranca despacito, se puede llevar hasta el tope si no
	# querés bancarte un viaje largo entero).
	if Input.is_physical_key_pressed(KEY_W) or _joystick_activo("acelerar"):
		velocidad_actual = min(ajustes["vel_maxima"], velocidad_actual + ACELERACION * delta)
		_actualizar_etiqueta_velocidad()
	elif Input.is_physical_key_pressed(KEY_S) or _joystick_activo("frenar"):
		velocidad_actual = max(ajustes["vel_minima"], velocidad_actual - ACELERACION * delta)
		_actualizar_etiqueta_velocidad()

	translate(Vector3(0, 0, -velocidad_actual * delta))

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

	_limitar_piso()
	_actualizar_hud(delta)

	# Una vez que llegaste, podés frenar apretando ESPACIO (siempre que estés
	# centrado sobre la pista, no en cualquier lado).
	if estado == Estado.LLEGADA and Input.is_action_just_pressed("ui_accept"):
		estado = Estado.FRENANDO

func _procesar_frenado(delta: float) -> void:
	velocidad_actual = max(0.0, velocidad_actual - 8.0 * delta)
	translate(Vector3(0, 0, -velocidad_actual * delta))
	_limitar_piso()
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
func _limitar_piso() -> void:
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
	var resultado = space_state.intersect_ray(consulta)
	if not resultado:
		return
	# Ponemos la posición DIRECTO al piso + margen (no sumamos un delta) --
	# así no se puede acumular error de un cuadro al otro si el rayo da
	# resultados apenas distintos cada vez.
	var punto_piso: Vector3 = resultado.position
	var altura_actual: float = (global_position - punto_piso).dot(arriba_real)
	if altura_actual < MARGEN_SOBRE_PISO:
		global_position = punto_piso + arriba_real * MARGEN_SOBRE_PISO

func _actualizar_hud(delta: float) -> void:
	var altura = mundo.altitud_avion + position.y - altura_piso
	etiqueta_altimetro.text = "ALT\n%dm" % int(round(altura))
	# Antes solo se actualizaba con el panel de Configuración abierto (ahí
	# vivía el reloj) -- ahora que se movió a la barra de arriba (siempre
	# visible), tiene que refrescarse siempre.
	_actualizar_etiqueta_hora()

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
	origen_option.select(0)
	destino_option.select(1)  # por default, "Hasta" apunta al segundo (no Aeroparque)

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
	return luz

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
	for luz in [luz_estroboscopica, luz_estroboscopica_externa]:
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
func _generar_luces_para_modelo_externo(instancia: Node3D) -> void:
	var caja: AABB = _calcular_aabb_local(instancia)
	if caja.size.length() < 0.001:
		return
	var x_izq: float = caja.position.x
	var x_der: float = caja.end.x
	var y_medio: float = caja.position.y + caja.size.y * 0.55
	var y_arriba: float = caja.end.y + caja.size.y * 0.08
	var z_medio: float = caja.position.z + caja.size.z * 0.5
	var radio_luz: float = clamp(caja.size.length() * 0.012, 0.03, 0.4)

	_crear_luz_navegacion(Vector3(x_izq, y_medio, z_medio), Color(1.0, 0.1, 0.1), instancia, radio_luz)
	_crear_luz_navegacion(Vector3(x_der, y_medio, z_medio), Color(0.1, 1.0, 0.2), instancia, radio_luz)
	luz_estroboscopica_externa = _crear_luz_navegacion(Vector3(caja.position.x + caja.size.x * 0.5, y_arriba, z_medio), Color(1.0, 1.0, 1.0), instancia, radio_luz * 1.2)

	var rango_spot: float = clamp(caja.size.length() * 0.35, 1.0, 8.0)
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
			# Pedido explícito 2026-09-26: las mismas luces de navegación del
			# avioncito clásico, para TODOS los aviones (de noche se veían
			# "como una cosa negra" sin esto).
			_generar_luces_para_modelo_externo(instancia)

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
		etiqueta_nombre.text = ("🛬 " if datos["tipo"] == "aterrizaje" else "🎯 ") + datos["nombre"]
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
	var idx_origen = origen_option.selected
	var idx_destino = destino_option.selected
	if idx_origen < 0 or idx_destino < 0 or idx_origen == idx_destino:
		return

	var nodo_origen = destinos[idx_origen]
	var nodo_destino = destinos[idx_destino]

	_aplicar_tipo_avion(tipo_avion_option.selected)
	global_position = nodo_origen.global_position + Vector3(0, 8, 0)
	# Vector3.UP otra vez NO -- mismo motivo que en _alinear_con_vertical_real
	# y _actualizar_camara(): acá se está fijando la orientación completa del
	# avión, y con el eje fijo del motor quedaría torcido/inclinado respecto
	# a la vertical real de este punto del mapa.
	look_at(nodo_destino.global_position, _arriba_real())
	banco_actual = 0.0
	velocidad_actual = VELOCIDAD_INICIAL
	_actualizar_etiqueta_velocidad()
	indice_destino = idx_destino
	estado = Estado.VOLANDO
	cartel_central.visible = false
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
	elif not destinos.is_empty() and indice_destino < destinos.size():
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
		if estado == Estado.VOLANDO and distancia < 400.0 and altura < 30.0:
			estado = Estado.LLEGADA
			cartel_central.text = "✅ ¡LLEGASTE A %s!\n\nApretá ESPACIO para frenar" % nombre_destino.to_upper()
			cartel_central.visible = true

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
	if destino_actual.has_meta("lat") and destino_actual.has_meta("lon"):
		var lat_destino = destino_actual.get_meta("lat")
		var lon_destino = destino_actual.get_meta("lon")
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
