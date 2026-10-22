extends Node
# Autoload (Singleton) -- perfil del piloto en Modo Carrera: plata, horas de
# vuelo, rango, licencias y aviones propios. Pedido 2026-09-21: "acordate que
# después vamos a poner varios jugadores... entonces tengan billeteras
# diferentes cada uno". Cada perfil es un archivo propio en user://perfiles/,
# así en la misma compu cada persona tiene su progreso separado sin pisarse.
#
# Las horas de vuelo y los rangos son un pedido explícito, tomados del
# esquema que YA estaba probado y andaba bien en Lex Air Manager (ver
# getRangoEstrellas() en "OTROS PROGRAMAS/Lex Air Manager.html", NUNCA
# modificado -- solo leído como referencia) -- mismos umbrales de horas,
# adaptados acá a "barras" de charretera (galones dorados) en vez de
# estrellitas, porque el pedido esta vez fue "las tiritas que tienen los
# comandantes en la camisa".

const CARPETA_PERFILES = "user://perfiles/"

# Umbral de horas de vuelo, nombre de rango y cantidad de barras de
# charretera (1 a 4; el Comandante suma una estrella dorada aparte, ver
# insignia_piloto.gd). Mismos números que Lex Air Manager (0/10/50/150/300/600).
const RANGOS_VUELO = [
	{"horas": 0, "nombre": "Aprendiz", "barras": 1, "estrella": false},
	{"horas": 10, "nombre": "Oficial Jr.", "barras": 2, "estrella": false},
	{"horas": 50, "nombre": "Oficial", "barras": 2, "estrella": false},
	{"horas": 150, "nombre": "Primer Oficial", "barras": 3, "estrella": false},
	{"horas": 300, "nombre": "Capitán", "barras": 4, "estrella": false},
	{"horas": 600, "nombre": "Comandante", "barras": 4, "estrella": true},
]

var perfil_actual: Dictionary = {}
var nombre_perfil_actual: String = ""

func _perfil_por_defecto(nombre: String) -> Dictionary:
	return {
		"nombre": nombre,
		"plata": 0,
		"horas_vuelo": 0.0,
		"licencias": {},          # ej: {"Helicóptero": true}
		"aviones_propios": {},    # ej: {"Helicóptero": true}
		"tickets_alquiler": {},   # ej: {"Helicóptero": 3}
		"progreso_licencias": {}, # ej: {"Helicóptero": [true, false, false]}
	}

func _ruta_perfil(nombre: String) -> String:
	return CARPETA_PERFILES + nombre.strip_edges() + ".json"

# Lista los nombres de todos los perfiles guardados en esta computadora
# (cada archivo .json en user://perfiles/ es un jugador distinto).
func listar_perfiles() -> Array:
	var nombres: Array = []
	DirAccess.make_dir_recursive_absolute(CARPETA_PERFILES)
	var dir := DirAccess.open(CARPETA_PERFILES)
	if not dir:
		return nombres
	dir.list_dir_begin()
	var archivo := dir.get_next()
	while archivo != "":
		if not dir.current_is_dir() and archivo.ends_with(".json"):
			nombres.append(archivo.trim_suffix(".json"))
		archivo = dir.get_next()
	dir.list_dir_end()
	nombres.sort()
	return nombres

func crear_perfil(nombre: String) -> void:
	nombre = nombre.strip_edges()
	if nombre == "":
		return
	nombre_perfil_actual = nombre
	perfil_actual = _perfil_por_defecto(nombre)
	guardar_perfil()
	_guardar_ultimo_perfil_usado(nombre)

func cargar_perfil(nombre: String) -> bool:
	var ruta := _ruta_perfil(nombre)
	if not FileAccess.file_exists(ruta):
		return false
	var archivo := FileAccess.open(ruta, FileAccess.READ)
	var datos = JSON.parse_string(archivo.get_as_text())
	archivo.close()
	if not (datos is Dictionary):
		return false
	nombre_perfil_actual = nombre
	# merge con los valores por defecto -- así si mañana agregamos un campo
	# nuevo (ej. "tickets_alquiler"), los perfiles viejos no se rompen.
	perfil_actual = _perfil_por_defecto(nombre)
	for clave in datos:
		perfil_actual[clave] = datos[clave]
	_guardar_ultimo_perfil_usado(nombre)
	return true

# Pedido 2026-10-17 (reportado: "cada vez que arranco, arranca de cero en la
# cuenta de dinero"): la plata SOLO persiste si hay un perfil cargado (ver
# guardar_perfil) -- pero nada cargaba ninguno a menos que el usuario entrara
# a mano a la pestaña "Carrera" y elegiera un piloto. Si volás "directo" sin
# pasar por ahí (el caso de Pablo todos los días), todo lo que ganás vive
# solo en memoria y se pierde al cerrar el juego. FIX: se recuerda el último
# perfil usado en un archivito chico aparte (RUTA_ULTIMO_PERFIL) y se carga
# SOLO al arrancar (ver _autocargar_ultimo_perfil, llamada una vez desde
# principal.gd _ready()) -- así nunca más arranca en cero sin pedírselo.
# Si en el futuro hay varios jugadores con su propio login, cada uno sigue
# pudiendo entrar a la pestaña Carrera y elegirse/crearse el suyo a mano
# (eso sigue funcionando exactamente igual, sin cambios).
const RUTA_ULTIMO_PERFIL = "user://ultimo_perfil.txt"

func _guardar_ultimo_perfil_usado(nombre: String) -> void:
	var archivo := FileAccess.open(RUTA_ULTIMO_PERFIL, FileAccess.WRITE)
	if archivo:
		archivo.store_string(nombre)
		archivo.close()

# Se llama UNA vez al arrancar el juego. Prioridad: 1) el último perfil
# usado (si el archivito existe y ese perfil todavía existe en disco),
# 2) si hay un único perfil guardado, ese, 3) si no hay ninguno todavía,
# se crea uno nuevo llamado "Piloto" (nombre genérico, cualquiera lo puede
# renombrar más adelante creando otro perfil desde la pestaña Carrera).
func autocargar_ultimo_perfil() -> void:
	if FileAccess.file_exists(RUTA_ULTIMO_PERFIL):
		var archivo := FileAccess.open(RUTA_ULTIMO_PERFIL, FileAccess.READ)
		var nombre_guardado := archivo.get_as_text().strip_edges()
		archivo.close()
		if nombre_guardado != "" and cargar_perfil(nombre_guardado):
			return
	var perfiles: Array = listar_perfiles()
	if perfiles.size() >= 1:
		cargar_perfil(perfiles[0])
		return
	crear_perfil("Piloto")

func guardar_perfil() -> void:
	if nombre_perfil_actual == "":
		return
	DirAccess.make_dir_recursive_absolute(CARPETA_PERFILES)
	var archivo := FileAccess.open(_ruta_perfil(nombre_perfil_actual), FileAccess.WRITE)
	archivo.store_string(JSON.stringify(perfil_actual, "\t"))
	archivo.close()

func hay_perfil_cargado() -> bool:
	return nombre_perfil_actual != ""

func modificar_plata(delta: int) -> void:
	perfil_actual["plata"] = max(0, int(perfil_actual.get("plata", 0)) + delta)
	guardar_perfil()

func agregar_horas_vuelo(horas: float) -> void:
	perfil_actual["horas_vuelo"] = max(0.0, float(perfil_actual.get("horas_vuelo", 0.0)) + horas)
	guardar_perfil()

func tiene_licencia(avion: String) -> bool:
	return perfil_actual.get("licencias", {}).get(avion, false)

func otorgar_licencia(avion: String) -> void:
	perfil_actual["licencias"][avion] = true
	guardar_perfil()

# Progreso de las misiones de licencia de un avión -- un array de bool, uno
# por misión (ej. [true, false, false] = ya hizo la primera de tres). Si el
# avión todavía no tiene progreso guardado, devuelve todo en false del
# tamaño pedido (para no romper si mañana agregamos una misión más).
func progreso_licencia(avion: String, total_misiones: int) -> Array:
	var progreso: Array = perfil_actual.get("progreso_licencias", {}).get(avion, [])
	while progreso.size() < total_misiones:
		progreso.append(false)
	return progreso

func marcar_mision_licencia_completa(avion: String, indice: int, total_misiones: int) -> void:
	var progreso: Array = progreso_licencia(avion, total_misiones)
	progreso[indice] = true
	perfil_actual["progreso_licencias"][avion] = progreso
	guardar_perfil()

func licencia_completa(avion: String, total_misiones: int) -> bool:
	for hecha in progreso_licencia(avion, total_misiones):
		if not hecha:
			return false
	return true

# Devuelve {nombre, barras, estrella} según las horas de vuelo actuales --
# recorre RANGOS_VUELO de mayor a menor umbral y se queda con el primero que
# cumple (mismo criterio que getRangoEstrellas() en Lex Air Manager).
func obtener_rango() -> Dictionary:
	var horas: float = perfil_actual.get("horas_vuelo", 0.0)
	var elegido = RANGOS_VUELO[0]
	for r in RANGOS_VUELO:
		if horas >= r["horas"]:
			elegido = r
	return elegido
