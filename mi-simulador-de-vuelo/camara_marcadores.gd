extends Camera3D

# Cámara "espejo" de la principal, pero que SOLO ve los marcadores propios
# (aros de ILS, luces de pista, faro de aeropuerto -- capa 6, ver
# CAPA_MARCADORES_NOCTURNOS en mundo.gd) y con su PROPIO Environment sin
# ningún post-proceso de noche (tonemap_exposure/adjustment_saturation
# siempre en su valor normal de pleno día).
#
# POR QUÉ EXISTE (pedido 2026-09-25, "el ILS y las luces de pista se ponen en
# blanco y negro también y se pierden"): el post-proceso día/noche del mundo
# principal (necesario porque el terreno de Cesium es "unlit" y no hay otra
# forma de oscurecerlo -- ver comentario largo en mundo.gd) es un filtro
# GLOBAL de pantalla completa. Godot no puede "exceptuar" un objeto puntual
# de ese filtro -- afecta a cada píxel ya renderizado, sea de la ciudad o de
# un aro de ILS carísimo en color. La única forma real de que algo mantenga
# SIEMPRE su color/brillo real es dibujarlo en una cámara/Viewport aparte que
# nunca pase por ese filtro, y superponer el resultado (transparente en todos
# lados salvo donde hay un marcador) encima de la vista principal ya oscura.
# Confirmado por Gemini y ChatGPT como la solución correcta (la alternativa,
# "compensar subiendo la emisión", funciona para la exposición pero NO para
# la desaturación -- eso arrastra cualquier color hacia el gris sin importar
# cuán brillante sea).
@onready var camara_principal: Camera3D = get_node("../../../CamaraJuego")

func _ready() -> void:
	current = true
	var env := Environment.new()
	# BG_CLEAR_COLOR + el SubViewport con transparent_bg=true (ver
	# MarcadoresViewport en la escena) es lo que deja transparente TODO lo
	# que esta cámara no dibuja -- si no, pintaría un cielo opaco encima de
	# la vista principal y taparía todo.
	env.background_mode = Environment.BG_CLEAR_COLOR
	env.tonemap_exposure = 1.0
	env.adjustment_enabled = false
	environment = env
	# OJO: el cull_mask de una cámara nueva viene con las 20 capas prendidas
	# por defecto -- tocar solo un par de bits sueltos dejaría visibles
	# también las capas 2-5/7-20 (por ejemplo la 5, que usa el mapa de
	# calles). Apagamos TODO y prendemos únicamente la 6.
	cull_mask = 0
	set_cull_mask_value(6, true)

func _process(_delta: float) -> void:
	if not camara_principal:
		return
	global_transform = camara_principal.global_transform
	fov = camara_principal.fov
	near = camara_principal.near
	far = camara_principal.far
