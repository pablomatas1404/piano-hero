extends Control
# Charretera de piloto (pedido 2026-09-21: "las tiritas que tienen los
# comandantes en la camisa, con estrellas") -- dibujada a mano con _draw(),
# sin depender de ninguna imagen bajada de internet (mismo criterio que
# icono_avion_mapa.gd: simple, liviano, sin líos de licencias de assets).
# Barras doradas horizontales (1 a 4 según el rango) + una estrella arriba
# para el rango más alto (Comandante).

var barras: int = 1
var con_estrella: bool = false

const COLOR_BARRA = Color(0.85, 0.68, 0.15, 1.0)
const COLOR_BORDE = Color(0.35, 0.25, 0.0, 1.0)
const ALTO_BARRA = 6.0
const SEPARACION = 3.0

func fijar_rango(cantidad_barras: int, tiene_estrella: bool) -> void:
	barras = cantidad_barras
	con_estrella = tiene_estrella
	queue_redraw()

func _draw() -> void:
	var ancho: float = size.x
	var y: float = size.y - ALTO_BARRA
	for i in barras:
		var rect := Rect2(0, y, ancho, ALTO_BARRA)
		draw_rect(rect, COLOR_BARRA)
		draw_rect(rect, COLOR_BORDE, false, 1.0)
		y -= ALTO_BARRA + SEPARACION
	if con_estrella:
		var cx: float = ancho / 2.0
		var cy: float = y - 6.0
		var puntos := PackedVector2Array()
		for i in 10:
			var angulo: float = i * PI / 5.0 - PI / 2.0
			var radio: float = 7.0 if i % 2 == 0 else 3.0
			puntos.append(Vector2(cx + cos(angulo) * radio, cy + sin(angulo) * radio))
		draw_colored_polygon(puntos, COLOR_BARRA)
