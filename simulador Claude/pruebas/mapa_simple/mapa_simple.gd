extends Control

# Escena de prueba ULTRA SIMPLE (pedido explicito 2026-10-03, "un mapa de
# fondo y un cuadradito que se mueva arriba, no es tan dificil"): nada de
# Cesium, nada de terreno 3D, nada de curvatura de la Tierra -- un mapa
# plano de calles (igual al mapita que YA funciona perfecto en el juego
# principal, mismo servidor, misma formula de tiles) de fondo fijo, con un
# cuadradito que se mueve arriba con flechas/WASD/joystick. Cero streaming
# 3D, cero riesgo de crash del plugin nativo de Cesium.

const LAT_CENTRO: float = -34.5589   # Aeroparque, mismo punto de siempre
const LON_CENTRO: float = -58.4164
const ZOOM: int = 15
const VELOCIDAD: float = 300.0  # pixeles por segundo

@onready var mapa: TextureRect = get_node("Mapa")
@onready var cuadradito: ColorRect = get_node("Cuadradito")
@onready var peticion: HTTPRequest = get_node("PeticionMapa")
@onready var label_estado: Label = get_node("LabelEstado")

func _ready() -> void:
	peticion.request_completed.connect(_on_mapa_descargado)
	_pedir_mapa()

func _pedir_mapa() -> void:
	label_estado.text = "Descargando mapa..."
	var n: float = pow(2.0, ZOOM)
	var lat_rad: float = deg_to_rad(LAT_CENTRO)
	var xtile: int = int(floor((LON_CENTRO + 180.0) / 360.0 * n))
	var ytile: int = int(floor((1.0 - log(tan(lat_rad) + 1.0 / cos(lat_rad)) / PI) / 2.0 * n))
	var url: String = "https://tile.openstreetmap.org/%d/%d/%d.png" % [ZOOM, xtile, ytile]
	var error: int = peticion.request(url, ["User-Agent: SimuladorDeVueloGodot/1.0"])
	if error != OK:
		label_estado.text = "Error al pedir el mapa (código %d)" % error

func _on_mapa_descargado(_resultado, codigo_respuesta: int, _headers, cuerpo: PackedByteArray) -> void:
	if codigo_respuesta != 200:
		label_estado.text = "El servidor del mapa devolvió código %d" % codigo_respuesta
		return
	var imagen := Image.new()
	if imagen.load_png_from_buffer(cuerpo) != OK:
		label_estado.text = "No se pudo leer la imagen del mapa"
		return
	mapa.texture = ImageTexture.create_from_image(imagen)
	label_estado.text = "Mapa cargado -- WASD/flechas/joystick: mover el cuadradito. ESC: salir."

func _input(event: InputEvent) -> void:
	if event.is_action_pressed("ui_cancel"):
		get_tree().quit()

func _process(delta: float) -> void:
	var direccion := Vector2.ZERO
	if Input.is_key_pressed(KEY_A) or Input.is_key_pressed(KEY_LEFT):
		direccion.x -= 1.0
	if Input.is_key_pressed(KEY_D) or Input.is_key_pressed(KEY_RIGHT):
		direccion.x += 1.0
	if Input.is_key_pressed(KEY_W) or Input.is_key_pressed(KEY_UP):
		direccion.y -= 1.0
	if Input.is_key_pressed(KEY_S) or Input.is_key_pressed(KEY_DOWN):
		direccion.y += 1.0

	if Input.get_connected_joypads().size() > 0:
		var id: int = Input.get_connected_joypads()[0]
		var eje_x: float = Input.get_joy_axis(id, JOY_AXIS_LEFT_X)
		var eje_y: float = Input.get_joy_axis(id, JOY_AXIS_LEFT_Y)
		if absf(eje_x) > 0.2:
			direccion.x += eje_x
		if absf(eje_y) > 0.2:
			direccion.y += eje_y

	cuadradito.position += direccion.normalized() * VELOCIDAD * delta if direccion.length() > 0.0 else Vector2.ZERO
	cuadradito.position = cuadradito.position.clamp(Vector2.ZERO, get_viewport_rect().size - cuadradito.size)
