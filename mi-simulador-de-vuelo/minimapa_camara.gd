extends Camera3D

# Cámara del minimapa: mira siempre derecho hacia abajo, y se mueve para
# seguir al avión (en X y Z), pero mantiene una altura fija bien alta.
# No gira con el avión -- como un mapa de GPS real, el norte siempre queda
# para el mismo lado, no como si "girara con vos".
#
# IMPORTANTE: usa perspectiva con FOV bien angosto, NO ortográfica. Con
# proyección ortográfica, Cesium no puede calcular el "screen space error"
# (el tamaño en píxeles que ocupa cada tesela) porque en ortográfica un
# objeto no achica con la distancia -- Cesium se confunde, cree que el error
# es infinito o nulo, y termina cargando solo la tesela raíz de baja
# resolución (la mancha oscura) dejando todo lo demás sin cargar (el gris).
# Con un FOV bajo (unos pocos grados) y la cámara bien alta, el resultado
# visual es indistinguible de una vista ortográfica cenital, pero el LOD de
# Cesium funciona normal.
const FOV_ESTRECHO = 6.0

@export var altura_camara: float = 5000.0

@onready var avion = get_node("../../../Avion")

func _ready() -> void:
	rotation_degrees = Vector3(-90, 0, 0)
	projection = Camera3D.PROJECTION_PERSPECTIVE
	fov = FOV_ESTRECHO
	# BUG encontrado 2026-09-20: el "far" por defecto de Godot es 4000m -- con
	# la cámara a 5000-19000m de altura, TODO quedaba más allá del plano de
	# corte y no se dibujaba nada (panel gris plano, ni siquiera la mancha de
	# antes). Hay que estirar el rango bien por encima de la altura máxima
	# que vaya a usar cualquiera de las cámaras que usan este script.
	near = 10.0
	far = 60000.0

func _process(_delta: float) -> void:
	global_position = Vector3(avion.global_position.x, altura_camara, avion.global_position.z)
