extends Node3D

# Escena de prueba AISLADA (pedido explicito 2026-10-03: "como si fuera un
# juego nuevo, para no romper nada") -- compara el terreno pesado que usa
# el juego principal (Google Photorealistic 3D Tiles, ion_asset_id=2275207,
# malla fotorrealista completa) contra el enfoque liviano que usa GeoFS
# (terreno de altura + foto satelital pegada encima, sin mallas de
# edificios reales). No toca principal.tscn/principal.gd para nada.
#
# Asset IDs de Cesium ion (globales, gratis para cualquier cuenta,
# confirmados por busqueda -- no son invento):
#   1     = Cesium World Terrain (terreno de altura, como el que usa GeoFS)
#   2     = Bing Maps Aerial (foto satelital para pegar sobre el terreno)
#   96188 = Cesium OSM Buildings (cajas 3D genericas por edificio, sin
#           fotorrealismo, mucho mas livianas que las de Google)

@onready var georeferencia: CesiumGeoreference = get_node("CesiumGeoreference")
@onready var tileset_terreno: Cesium3DTileset = get_node("CesiumGeoreference/TerrenoLigero")
@onready var tileset_edificios: Cesium3DTileset = get_node("CesiumGeoreference/EdificiosOSM")
@onready var avion: Node3D = get_node("Avion")
@onready var camara: Camera3D = get_node("Avion/Camara")
@onready var check_edificios: CheckButton = get_node("HUD/PanelControles/VBoxControles/CheckEdificios")
@onready var label_fps: Label = get_node("HUD/PanelControles/VBoxControles/LabelFPS")
@onready var label_ayuda: Label = get_node("HUD/PanelControles/VBoxControles/LabelAyuda")

const VELOCIDAD_AVANCE = 60.0  # m/s, crucero chico tipo Piper
const VELOCIDAD_GIRO = 1.0     # rad/s aprox para pitch/yaw/roll de prueba

# Joystick generico (pedido 2026-10-03, "activame el joystick con todos los
# comandos, arriba, abajo, costado"): esta escena es solo de prueba, no usa
# el sistema de asignacion de botones del juego principal (ver principal.gd
# ~linea 931/1467 para esa referencia) -- pero sí copia el patron importante
# de ahi: detectar el joystick conectado en vez de asumir "dispositivo 0" a
# ciegas, y guardar su id.
const ZONA_MUERTA_JOYSTICK = 0.15
const VELOCIDAD_ACELERADOR = 40.0  # m/s que suma/resta el acelerador del joystick
const VELOCIDAD_ROLL = 1.5         # rad/s para el banco visual (rotate_z)

var joystick_id: int = -1          # -1 = no hay joystick conectado

const WGS84_A = 6378137.0                 # radio ecuatorial de la Tierra (metros)
const WGS84_F = 1.0 / 298.257223563       # achatamiento de la Tierra
const ALTURA_INICIAL = 1500.0

func _ready() -> void:
	# Arranca con los edificios OSM apagados -- primero mostramos "solo
	# terreno + foto" (lo mas parecido a GeoFS gratis), el usuario los
	# prende con el check cuando quiera comparar.
	tileset_edificios.visible = false
	check_edificios.button_pressed = false
	check_edificios.toggled.connect(func(activo: bool):
		tileset_edificios.visible = activo)

	_ubicar_avion_sobre_el_origen()
	_detectar_joystick()

func _input(event: InputEvent) -> void:
	# Pedido explicito 2026-10-03 ("no hay un boton para salir, hay que
	# salir con alt-tab") -- Escape cierra esta escena de prueba.
	if event.is_action_pressed("ui_cancel"):
		get_tree().quit()

# BUG REAL encontrado 2026-10-03 (mismo que ya documenta la "regla de oro"
# del proyecto sobre la curvatura de la Tierra): esta escena ponia al avion
# en Vector3(0, 1500, 0) a mano, asumiendo que el eje Y crudo del motor
# apunta "para arriba de verdad". En Buenos Aires el "arriba" real esta
# inclinado respecto a ese eje -- resultado: el avion/camara quedaban
# mirando para un lugar sin nada cargado (todo cielo, el usuario mando una
# captura confirmandolo). Fix: calcular arriba/norte REALES en espacio del
# motor (mismo metodo que ya usa mundo.gd en el juego principal,
# _lat_lon_alt_a_ecef_xyz + get_tx_ecef_to_engine) y usarlos para ubicar y
# orientar el avion, en vez de adivinar con los ejes crudos.
func _ubicar_avion_sobre_el_origen() -> void:
	var lat_rad: float = deg_to_rad(georeferencia.get_latitude())
	var lon_rad: float = deg_to_rad(georeferencia.get_longitude())
	var up_ecef := Vector3(cos(lat_rad) * cos(lon_rad), cos(lat_rad) * sin(lon_rad), sin(lat_rad))
	var east_ecef := Vector3(-sin(lon_rad), cos(lon_rad), 0.0)
	var north_ecef := up_ecef.cross(east_ecef)

	var basis_ecef_a_motor: Basis = georeferencia.get_tx_ecef_to_engine().basis
	var arriba_motor: Vector3 = (basis_ecef_a_motor * up_ecef).normalized()
	var norte_motor: Vector3 = (basis_ecef_a_motor * north_ecef).normalized()

	avion.global_position = arriba_motor * ALTURA_INICIAL
	avion.look_at(avion.global_position + norte_motor, arriba_motor)
	Input.joy_connection_changed.connect(func(_dispositivo: int, _conectado: bool):
		_detectar_joystick())

func _detectar_joystick() -> void:
	var conectados: Array = Input.get_connected_joypads()
	if conectados.size() > 0:
		joystick_id = conectados[0]
	else:
		joystick_id = -1
	_actualizar_label_ayuda()

func _actualizar_label_ayuda() -> void:
	var estado_joystick: String
	if joystick_id != -1:
		estado_joystick = "joystick detectado: %s" % Input.get_joy_name(joystick_id)
	else:
		estado_joystick = "sin joystick conectado"
	label_ayuda.text = ("W/S o joystick: acelerar/frenar   Flechas o stick: girar/cabecear   " +
		"Stick derecho o boton: banco   Check de abajo: edificios OSM on/off\n%s" % estado_joystick)

func _process(delta: float) -> void:
	label_fps.text = "FPS: %d" % Engine.get_frames_per_second()

	var velocidad: float = VELOCIDAD_AVANCE
	if Input.is_key_pressed(KEY_S):
		velocidad *= 0.3

	var giro_yaw: float = 0.0
	var giro_pitch: float = 0.0
	var giro_roll: float = 0.0

	if joystick_id != -1:
		# Stick principal (izquierdo): eje X = yaw, eje Y = pitch.
		var eje_x: float = Input.get_joy_axis(joystick_id, JOY_AXIS_LEFT_X)
		var eje_y: float = Input.get_joy_axis(joystick_id, JOY_AXIS_LEFT_Y)
		if absf(eje_x) > ZONA_MUERTA_JOYSTICK:
			giro_yaw -= eje_x * VELOCIDAD_GIRO * delta
		if absf(eje_y) > ZONA_MUERTA_JOYSTICK:
			giro_pitch -= eje_y * VELOCIDAD_GIRO * delta

		# Stick secundario (derecho), si el mando lo tiene -- opcional, se usa
		# solo para banco visual (rotate_z). Si no existe, Godot devuelve 0.0
		# y simplemente no hace nada (no rompe nada si no hay stick derecho).
		var eje_roll: float = Input.get_joy_axis(joystick_id, JOY_AXIS_RIGHT_X)
		if absf(eje_roll) > ZONA_MUERTA_JOYSTICK:
			giro_roll -= eje_roll * VELOCIDAD_ROLL * delta

		# Acelerador: primero probamos el gatillo derecho (muchos mandos tipo
		# Xbox/Playstation lo usan para acelerar), y lo restamos con el
		# gatillo izquierdo para frenar si el mando los tiene separados.
		var gatillo_derecho: float = Input.get_joy_axis(joystick_id, JOY_AXIS_TRIGGER_RIGHT)
		var gatillo_izquierdo: float = Input.get_joy_axis(joystick_id, JOY_AXIS_TRIGGER_LEFT)
		if gatillo_derecho > ZONA_MUERTA_JOYSTICK:
			velocidad += gatillo_derecho * VELOCIDAD_ACELERADOR
		if gatillo_izquierdo > ZONA_MUERTA_JOYSTICK:
			velocidad -= gatillo_izquierdo * VELOCIDAD_ACELERADOR

		# Fallback generico: como no sabemos el modelo exacto del joystick del
		# usuario, CUALQUIER boton (0 en adelante) tambien acelera un poco.
		# Es una aproximacion simple a proposito -- no reemplaza al teclado,
		# solo se suma, asi que no puede romper nada que ya funcione.
		for indice_boton in range(JOY_BUTTON_SDL_MAX):
			if Input.is_joy_button_pressed(joystick_id, indice_boton):
				velocidad += VELOCIDAD_ACELERADOR * 0.5
				break

	avion.translate(Vector3(0, 0, -velocidad * delta))

	if Input.is_key_pressed(KEY_LEFT):
		giro_yaw += VELOCIDAD_GIRO * delta
	if Input.is_key_pressed(KEY_RIGHT):
		giro_yaw -= VELOCIDAD_GIRO * delta
	if Input.is_key_pressed(KEY_UP):
		giro_pitch += VELOCIDAD_GIRO * delta
	if Input.is_key_pressed(KEY_DOWN):
		giro_pitch -= VELOCIDAD_GIRO * delta
	avion.rotate_y(giro_yaw)
	avion.rotate_x(giro_pitch)
	if giro_roll != 0.0:
		avion.rotate_z(giro_roll)
