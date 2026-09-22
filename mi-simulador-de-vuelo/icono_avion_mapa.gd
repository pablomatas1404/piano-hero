extends Control

# INTENTOS ANTERIORES (triángulo, luego silueta de avión con rotación según
# el rumbo) fallaban en casos límite -- en giros bruscos, la orientación
# calculada se "perdía" y apuntaba para cualquier lado. En vez de seguir
# persiguiendo ese bug, un punto sin orientación (pedido explícito del
# usuario como alternativa) es inmune por diseño: no hay rotación que pueda
# salir mal.
const COLOR_RELLENO = Color(0.9, 0.15, 0.15, 1)
const COLOR_BORDE = Color(1, 1, 1, 1)
const RADIO = 7.0

func _draw() -> void:
	draw_circle(Vector2.ZERO, RADIO, COLOR_RELLENO)
	draw_arc(Vector2.ZERO, RADIO, 0, TAU, 24, COLOR_BORDE, 1.5, true)
