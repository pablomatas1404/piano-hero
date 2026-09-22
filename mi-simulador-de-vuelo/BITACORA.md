# 📅 2026-09-21 — Sesión larga (~13 hs): navegación real arreglada, país entero jugable

## Lo más importante del día: el bug de navegación, RESUELTO DE VERDAD

Durante meses el "RUMBO" (hacia dónde apunta el avión) se calculaba mal: usaba
los ejes crudos del motor de Godot como si fueran este/norte reales, algo que
solo es aproximadamente cierto MUY cerca del punto de partida (Aeroparque). A
medida que se volaba lejos, la curvatura de la Tierra hacía que el rumbo
mostrado se desviara cada vez más (~1° cada varios km), hasta terminar
apuntando casi al revés a 10-15 km de distancia. Esto también afectaba la
posición de los carteles de misión (nunca se recentraban al mover el origen).

Se cazó con evidencia dura (Pablo marcó coordenadas reales con GPS en
Google Maps y las comparó contra lo que mostraba el juego, más una
herramienta de debug hecha sobre la marcha -- tecla N, vuelca un renglón con
los números exactos a `debug_rumbo.log` en vez de inundar la consola).
Confirmado con ayuda cruzada de Gemini. Arreglado en dos partes:
1. `_posicion_desde_lat_lon()` en `mundo.gd` usaba `get_latitude()/
   get_longitude()` del georeference, que quedan PEGADOS en el valor
   inicial y no seguían el origen flotante -- cambiado a `get_ecef_x/y/z()`
   (esos sí están sincronizados).
2. El cálculo del propio RUMBO ACTUAL del avión (en `principal.gd`) tenía el
   mismo problema de raíz (ejes crudos del motor) -- ahora se proyecta la
   nariz sobre el este/norte REALES de la posición actual (expuestos como
   `mundo.este_motor_actual`/`norte_motor_actual`, ya calculados para el
   recentrado de origen).

Probado y CONFIRMADO por Pablo con viajes reales (Aeroparque→Boca, Boca→
casa, etc.) sin ningún error, incluso en diagonal.

## Todo lo demás que se hizo hoy

- **HUD rediseñado**: panel de instrumentos digital chico (ALT/RUMBO/VEL/
  DIST, estilo avión, letras verdes sobre negro). Se sacó el panel viejo de
  "Torre: San Fernando" (redundante), quedó solo el rumbo/distancia/flechas.
- **Selector de tipo de avión**: Cessna, Boeing, Jet privado, Avión de
  guerra, Helicóptero -- modelos 3D reales descargados de poly.pizza (sin
  login, licencia CC-BY, créditos en `Aviones 3D/LEEME - Listado de
  modelos.md`), cada uno con su escala/rotación ajustada a ojo.
- **Helicóptero**: subida/bajada vertical con R/F o joystick, y sistema de
  **Misiones** (botón aparte): hospitales (estilo SAME) y sobrevuelos
  turísticos (Boca, River, Obelisco, Teatro Colón, etc.), con precio
  editable con flechitas hasta fijar precios reales.
- **"Marcar lugar" (tecla M)**: guarda la coordenada real exacta del avión
  en `lugares_marcados.json`, con nombre a elección. Si el nombre dice
  "aeropuerto", aparece EN EL ACTO en el juego con cartel y pista orientada
  según el rumbo que llevaba el avión al marcarlo.
- **Mapa de calles 2D** (panel lateral, estilo GeoFS): tesela real de
  OpenStreetMap, se actualiza sola al cruzar de cuadrante, con avioncito
  (ahora un punto rojo simple, más confiable que un ícono rotado).
- **Ayuda visual (línea guía)**: tramo fijo en el mundo real desde donde se
  activó el destino hasta el destino -- si te alejás, se queda esperando en
  su lugar, no persigue al avión.
- **Freno de emergencia** (tecla D): baja la velocidad de golpe a 200 (nunca
  la sube), para no pasarse de un destino yendo a la velocidad máxima nueva
  (1500 km/h, antes 800). Radio de giro y autoridad de cabeceo ampliados
  para poder corregirse rápido sin depender de una aproximación instrumental
  que todavía no existe.
- **53 aeropuertos reales de todo el país** agregados (extraídos de las
  rutas de Little Navmap en `Rutas LNM/`): Córdoba, Mendoza, Bariloche,
  Jujuy, Salta, Tucumán, El Calafate, Ushuaia, Río Gallegos, Rosario,
  Asunción, Foz de Iguazú, y más -- ya se puede volar por toda Argentina
  (y un poco de Uruguay/Paraguay/Brasil) con "Elegir vuelo".

## Pendiente para la próxima sesión

- Hangares y helipuerto "de verdad" (no se encontraron modelos 3D
  individuales gratis sin login -- evaluar armarlos simples en Blender).
- Sistema de recoger/entregar en las misiones (hoy es solo "llegar", falta
  la mecánica de carga/descarga y el pago real).
- Economía: plata acumulada, comprar/mejorar aviones.
- Sonido de motor y ambiente de cabina.
- Viento (para que no sea tan "quirúrgico" volar, más realista).
- Tren de aterrizaje / flaps animados (depende de tener los modelos con
  partes móviles, algunos del listado de poly.pizza ya las tienen).
- Seguir marcando aeropuertos de tierra/pistas chicas con "Marcar lugar" a
  medida que Pablo los va encontrando volando.
- Revisar si el síntoma viejo ("no avanzaba más allá de cierto punto" cerca
  de Necochea, reportado por Pablo de una sesión anterior) se repite ahora
  con las rutas largas nuevas -- si pasa, usar la tecla N para diagnosticarlo
  con datos reales en vez de a ciegas.

---

# Bitácora del Simulador de Vuelo — para retomar en otro chat

Este archivo existe para que, si esta conversación se corta o se satura,
cualquier chat nuevo (yo mismo, en otra sesión) pueda leer esto y entender
todo el proyecto sin tener que repreguntar desde cero. Actualizalo vos mismo
(o pedile a Claude que lo actualice) cada vez que haya un cambio grande.

## De qué se trata el proyecto

Pablo (el usuario) quiere el simulador de vuelo más realista posible, hecho
con herramientas gratis, con la ambición de acercarse a Microsoft Flight
Simulator / GeoFS. Es un proyecto TOTALMENTE APARTE del juego "Lex Air
Manager" (2D, HTML) que también existe en esta carpeta de PIANO HERO —
**nunca tocar Lex Air Manager.html, no tiene nada que ver con esto.**

Motor: **Godot Engine 4.7.2**, GDScript. Carpeta del proyecto:
`C:\00000000 PROYECTOS JUEGOS MATAS\PIANO HERO\mi-simulador-de-vuelo\`

## Cómo le gusta trabajar a Pablo (importante)

- Dicta por voz, a veces en varios audios seguidos. Si dice "sigo en otro
  audio" o "no me contestes", hay que quedarse en silencio total (sin usar
  herramientas) hasta que pida una respuesta.
- Prefiere que YO haga todo por código/archivos directamente, no que lo
  guíe a tocar mil botones en el editor de Godot. Solo le pido que haga
  clics cuando es imposible hacerlo desde archivos (plugins, paneles de
  editor, etc.).
- Prefiere cambios grandes de una sola vez ("no vayas corrigiendo de a
  poco, espero y veo todos los cambios juntos") antes que ir probando
  cositas chiquitas todo el tiempo.
- Cuando hay un bug raro y yo ya probé lo obvio, le gusta armar un prompt
  para pegarle a ChatGPT y/o Gemini en paralelo y comparar. Si le pido
  eso, tengo que efectivamente ESCRIBIR el prompt completo en el chat (no
  solo decir "ya te lo dejé en un archivo" — eso ya le pasó y se quejó).
- Es una persona grande (~53 años), le gustaban los simuladores de vuelo
  viejos. No es programador. Hay que explicarle todo en criollo, sin
  jerga, y confirmar antes de asumir que entendió instrucciones técnicas.

## Arquitectura actual (después del gran cambio a terreno real)

- `principal.tscn`: escena principal. Nodo raíz `Node3D` (script `mundo.gd`)
  con: `WorldEnvironment`, `DirectionalLight3D`, **`CesiumGeoreference`**
  (con su `Cesium3DTileset` hijo, terreno real de Google Photorealistic 3D
  Tiles), `Avion` (script `principal.gd`, arranca en y=50), `CamaraJuego`
  (Camera3D común, nuestra, no la del plugin de Cesium), `HUD`
  (CanvasLayer con todo el panel de vuelo).
- `principal.gd`: TODA la física de vuelo, HUD, Torre, selector de vuelo,
  acelerador. Esto es 100% nuestro, no depende de Cesium para nada excepto
  que ahora `altura_piso = 0.0` (antes era -3, porque el piso de Cesium
  está a nivel ~0 cerca de Aeroparque).
- `mundo.gd`: ahora MUY simplificado. Ya NO genera terreno, árboles,
  campos ni casitas de juguete (eso lo hacía antes con fotos horneadas a
  mano, todo ese sistema quedó reemplazado por el terreno real de Cesium).
  Lo único que hace es: por cada aeropuerto de `aeropuertos_lla` (lista con
  nombre + latitud/longitud reales), calcular su posición local llamando a
  `georeferencia.lat_lon_alt_rad_to_ecef()` y `get_tx_ecef_to_engine()`
  (funciones del PROPIO plugin de Cesium, no inventé la conversión a
  mano), y generar ahí una pista + cartel + poste (todo de juguete/simple,
  ya que el terreno real de abajo es lo que da el realismo visual).
- `addons/cesium_godot/` + `bin/`: el plugin "3D Tiles for Godot"
  (Battle-Road-Labs/3D-Tiles-For-Godot), copiado desde el proyecto de
  prueba `prueba-cesium-godot` (carpeta hermana, ya no hace falta pero se
  puede dejar/borrar). Usa Google Photorealistic 3D Tiles vía una cuenta
  de Cesium ion conectada (login con Google, cuenta gratis).
- `api_keys.gd`: tiene las claves de Mapbox/OpenTopography, que ERAN para
  el sistema viejo de fotos horneadas — con el cambio a Cesium, estas
  claves ya no se usan para el terreno, pero no hace falta borrarlas.
- `assets/terreno_real/`: fotos/heightmaps horneados del sistema VIEJO
  (Aeroparque + cadena a San Fernando). Con el cambio a Cesium esto quedó
  OBSOLETO -- ya no se genera ni se usa en `mundo.gd`. Se puede borrar en
  algún momento si se confirma que Cesium funciona bien para siempre, pero
  por ahora se deja por las dudas (no molesta, simplemente no se carga).

## Controles del avión (NO cambiaron con el tema Cesium)

- **Flechas** (↑↓←→): cabeceo (subir/bajar nariz) y alabeo (banco/giro
  coordinado). Pitch: arriba=nariz abajo, abajo=nariz arriba (así quedó
  después de probar inversiones y volver al original). Alabeo: izquierda
  = banca a la izquierda y gira a la izquierda (rumbo baja), etc. -- ya
  probado y confirmado que el signo está bien.
- **W / S**: acelerador de velocidad de crucero (W sube, S baja), rango
  0 a 60 (unidades = metros/seg aprox, ya que ahora el mundo está a
  escala real). Arranca en 4 (`VELOCIDAD_INICIAL`).
- **ESPACIO**: frenar al llegar (cuando la Torre dice "LLEGADA"), y
  confirmar en los carteles.
- El panel "Elegir vuelo" (desplegables Desde/Hasta + botón Confirmar)
  teletransporta al origen elegido, orientado hacia el destino.

## Estado real de las coordenadas (MUY IMPORTANTE, limitación conocida)

Todos los aeropuertos ahora tienen su latitud/longitud REAL (investigada
con OurAirports.com), y `mundo.gd` calcula su posición en el juego usando
las funciones del propio `CesiumGeoreference` (no una fórmula inventada a
mano). PERO: la conversión que usamos es una aproximación de "plano
tangente" centrada en Aeroparque -- funciona MUY bien para distancias
cortas (Aeroparque-San Fernando, ~20 km, error insignificante), pero para
aeropuertos MUY lejos (Villa Gesell y Mar del Plata, ~350-400 km) la
curvatura de la Tierra hace que la posición calculada esté mal (podría
aparecer varios kilómetros "hundida" respecto al terreno real, porque un
plano recto no seguye la curva del planeta a esa distancia).

**Solución pendiente**: el plugin de Cesium tiene funciones para
"re-centrar" el origen a medida que uno se aleja (`move_origin`,
`register_tileset_to_move_origin` en el código C++ de `CesiumGeoreference`)
-- eso es lo que hay que investigar/usar para que los aeropuertos lejanos
queden bien ubicados. Por ahora (para la sesión de esta noche) alcanza con
que Aeroparque y San Fernando estén bien, que es lo que se pidió probar.

Aeropuertos con lat/lon reales ya cargados en `mundo.gd`:
Aeroparque (-34.5589,-58.4164), San Fernando (-34.4532,-58.5896),
El Palomar (-34.6100,-58.6125), Morón (-34.6764,-58.6428),
Quilmes (-34.72,-58.27, aproximado, sin código OACI propio),
La Plata (-34.9744,-57.8956), Villa Gesell (-37.2344,-57.0214),
Mar del Plata (-37.9342,-57.5733).

Pendiente para el futuro (ya charlado con Pablo, coordenadas reales que
dio de un mapa de planificación de vuelo): agregar Ezeiza (SAEZ), Luján,
Mariano Moreno (cerca de José C. Paz/San Miguel, probablemente SADJ), y
más adelante Bariloche (SAZS, -41.1512,-71.1575) para cuando se sume esa
región con montañas reales.

## Cómo se llegó hasta el terreno real de Cesium (resumen de la saga)

1. Primero se intentó un sistema de "fotos horneadas a mano": bajar
   imágenes satelitales (Mapbox) + elevación real (OpenTopography SRTM)
   por código (Python, vía Bash), armar mallas 3D con SurfaceTool en
   GDScript, e ir encadenando "parches" cuadrados uno al lado del otro.
   Funcionó razonablemente bien para Aeroparque-San Fernando (con varios
   bugs resueltos en el camino: rotación de la textura mal orientada,
   z-fighting con el piso, baldosas corruptas por rate-limit de Mapbox,
   etc.) pero Pablo pidió algo más ambicioso: el país entero, con calidad
   tipo Google Earth/GeoFS.
2. Se descubrió que existe un plugin real para esto: **"3D Tiles for
   Godot"** (Battle-Road-Labs/3D-Tiles-For-Godot, en GitHub), que conecta
   Godot a Cesium ion / Google Photorealistic 3D Tiles vía streaming (la
   MISMA tecnología real que usa GeoFS).
3. Instalarlo y hacerlo funcionar llevó varias rondas de debugging real
   (con ayuda cruzada de ChatGPT y Gemini, comparando diagnósticos):
   - El panel del plugin no aparecía: bug de estado al desactivar/
     reactivar el plugin repetidas veces (no limpia bien sus nodos
     internos en `_exit_tree()`). Solución: cerrar Godot COMPLETO y
     volver a abrir, sin tocar el checkbox de Plugins.
   - Pantalla gris sin nada al darle Play: la `Camera3D` que crea el
     botón "Dynamic Camera" tenía el checkbox **"Current" destildado**, y
     el "Far" en el default de Godot (4000), insuficiente para ver algo a
     escala planetaria. Solución: tildar Current, subir Far a 100000+,
     bajar Near a ~0.01-0.1.
   - Aparecía una montaña random en vez de Buenos Aires: había que
     configurar el `CesiumGeoreference` con `Origin Type = Cartographic
     Origin`, `Latitude`, `Longitude`, `Altitude` reales (propiedades
     confirmadas leyendo el código fuente C++ del plugin, no adivinadas).
   - El teclado/mouse no respondían dentro de la ventana de juego
     embebida en el editor: la barra de esa ventana tiene un selector
     "Entrada / 2D / 3D" -- tiene que estar en **"Entrada"**, no en "3D"
     (que es el modo de navegación del editor, no del juego).
4. Con todo eso resuelto, se probó en un proyecto SEPARADO
   (`prueba-cesium-godot`, carpeta hermana) antes de tocar el simulador
   real, para no arriesgar lo que ya andaba.
5. Recién esta noche se integró todo al proyecto real
   (`mi-simulador-de-vuelo`), reemplazando el terreno horneado por el
   real de Cesium, y recalculando las posiciones de los aeropuertos con
   coordenadas reales usando las funciones del propio plugin.

## Claves / cuentas usadas (dónde están, no los valores en sí)

- **Mapbox** y **OpenTopography**: en `api_keys.gd` (gitignored). Del
  sistema VIEJO de fotos horneadas, ya no se usan activamente.
- **Cesium ion**: cuenta de Pablo, usuario `pmatas`, conectada por OAuth
  desde el panel del plugin en Godot (login con Google). No hay una
  "clave" que copiar a mano para esto -- la sesión queda guardada en el
  editor de Godot una vez conectada.
- **Google Photorealistic 3D Tiles**: se accede a través de la cuenta de
  Cesium ion de arriba (asset `ion_asset_id = 2275207`), no hace falta
  una cuenta de Google Cloud aparte.

## Pendientes / roadmap (en orden de prioridad, según lo último que dijo Pablo)

1. Confirmar que el vuelo Aeroparque→San Fernando anda bien con terreno
   real (esto se está probando ahora mismo, a la noche).
2. Arreglar la curvatura terrestre para que los aeropuertos lejanos
   (Villa Gesell, Mar del Plata) queden bien ubicados.
3. Agregar más aeropuertos reales de Capital Federal / alrededores
   (Ezeiza, Luján, Mariano Moreno) para armar misiones turísticas dentro
   de Capital con el Cessna (recorridos cortos, ida y vuelta, 2-3 puntos).
4. Sistema de misiones + economía (ya charlado conceptualmente): vuelos
   privados turísticos vs. comerciales, plata que gana el DUEÑO del avión
   (no el piloto), investigado con precios reales de paseos en helicóptero
   en Buenos Aires como referencia (~USD 60-110 por persona, 18-42 min).
5. Reemplazar el avión primitivo (cajas armadas a mano) por un modelo 3D
   de verdad, gratis.
6. Sonido de motor y otros "chiches" de inmersión.
7. Sistema de viento (para que el vuelo no sea tan preciso/quirúrgico,
   más realista para un Cessna).
8. Tren de aterrizaje, flaps (mencionado como referencia de otro
   simulador que usa Pablo, para el futuro).
9. A más largo plazo: expandir a TODO el país, y eventualmente cruzar el
   Río de la Plata hacia Colonia (Uruguay) como el primer destino
   internacional corto.

## ⚠️ BUG SIN RESOLVER (esto es lo primero que hay que atacar la próxima vez)

Después de integrar Cesium al simulador real, el terreno real CARGA (se ve
Buenos Aires real, confirmado) pero la relación entre el avión/cámara y el
terreno está rota: el mapa aparece "de costado" (como una pared inclinada
en un borde de la pantalla, no chato debajo del avión), objetos propios
simples (la pista, un cartel de texto) aparecen gigantes y en ángulos muy
raros, y subir/bajar la nariz del avión cambia el número de "ALTITUD" en
el HUD pero NO tiene ningún efecto visual sobre el terreno (como si el
cambio de altura solo le importara al instrumento). Terminó la sesión
SIN resolver esto -- no arrancar suponiendo que ya está arreglado.

**Hallazgo real más importante de la noche** (verificado leyendo el código
fuente C++ real del plugin, no es una teoría): en
`cesium_godot/Models/CesiumGDTileset.cpp`, función `update_tileset()`:

```cpp
if (isGeoreferenced) {
    camPos = this->m_georeference->get_ecef_position();  // <- SIEMPRE el ORIGEN
}
else {
    camPos = CesiumMathUtils::to_glm_dvec3(cameraTransform.origin);
}
```

O sea: cuando el `Cesium3DTileset` está georreferenciado (nuestro caso,
siempre), **ignora por completo la posición de cámara que le pasamos** en
`update_tileset(transform)` -- solo usa la DIRECCIÓN (`.basis`) de ese
transform, pero la POSICIÓN para decidir qué terreno mostrar es siempre la
del `CesiumGeoreference` mismo (`get_ecef_position()`). Esto explica todo:
mover el avión no cambia nada visualmente porque, para el sistema de
carga de terreno, la "cámara" nunca se despega de Aeroparque.

Por esto el script de ejemplo del plugin que SÍ funciona
(`georeference_camera_controller.gd`) NUNCA mueve una cámara común con
`translate()` -- mueve el ORIGEN mismo, todo el tiempo, con
`globe_node.ecefX/Y/Z += ...` cada cuadro que hay input de movimiento.

**Lo último que se probó** (implementado, PERO EL RESULTADO FUE PEOR, no
mejor -- Pablo reportó que ahora el mapa desaparece del todo, todo marrón):
recentrar el origen del `CesiumGeoreference` a la posición real del avión
EN CADA CUADRO (no cada 2 km como antes), para imitar lo que hace el
script de ejemplo. Está en `mundo.gd`, función `_recentrar_origen_en_avion()`,
llamada desde `_process()` sin ningún umbral de distancia. **Este cambio
quedó aplicado en el código pero NO funcionó bien -- puede tener un bug
en sí mismo** (por ejemplo: quizás al recentrar tan seguido, algo se
descalibra por errores de redondeo acumulados, o el orden de operaciones
dentro de `_recentrar_origen_en_avion()` tiene un problema, o recentrar
CADA cuadro no es realmente lo que hace falta y hay que revisar con más
cuidado qué datos concretos usa `update_tileset()` del `cameraTransform`
que SÍ le pasamos, ya que la dirección/orientación si se sigue usando).

**Cosas ya descartadas esta noche** (no perder tiempo repitiéndolas):
- Multiplicar por `get_tx_ecef_to_engine()` completo (con traslación) en
  vez de solo `.basis` sobre un delta -- ChatGPT y Gemini lo sugirieron
  más de una vez, pero es INCORRECTO para nuestra versión: confirmado
  leyendo el código fuente que `get_tx_ecef_to_engine()` = simplemente
  `get_global_transform()` del nodo `CesiumGeoreference`, que en su
  `_enter_tree()` SOLO hace `set_rotation_degrees(-90,0,0)` -- nunca toca
  la posición. Aplicarlo directo sobre un ECEF absoluto (millones) vuelve
  a dar números gigantes (el primer bug de toda la noche).
- Orientar los aeropuertos con `eus_at_ecef()` rotado por la matriz fija
  -- quedó mal (pista gigante, de canto). Se revirtió a `Basis.IDENTITY`.
- `register_tileset_to_move_origin()` -- existe en el código fuente en
  GitHub pero NO está en el .dll precompilado v1.0.1 que tenemos (choca
  "Nonexistent function"). No usar, o recompilar el plugin desde código
  fuente si hace falta de verdad (Visual Studio + SCons, más laborioso).

**Plan concreto para la próxima sesión** (en este orden):
1. Revisar bien la implementación actual de `_recentrar_origen_en_avion()`
   en `mundo.gd` en busca de un bug propio (no del plugin) -- por ejemplo,
   loguear con print() el resultado de `_ecef_a_lat_lon_alt()` cada vez
   que se llama, y verificar que de verdad dé una lat/lon sensata (cerca
   de -34.55/-58.4), no algo disparatado.
2. Si el recentrado por cuadro en sí está bien pero el problema persiste,
   revisar qué hace el plugin exactamente con la DIRECCIÓN/orientación
   del `cameraTransform` que le pasamos a `update_tileset()` (esa parte
   del código SÍ la usa, a diferencia de la posición) -- comparar línea
   por línea con cómo la arma `georeference_camera_controller.gd`.
3. Considerar, como alternativa más robusta (más trabajo pero más
   confiable a largo plazo): en vez de mover el avión con `translate()`
   normal y despues recentrar el origen para "avisarle" al tileset,
   directamente ADAPTAR el avance del avión para que empuje el origen
   (`georeferencia.ecef_x/y/z`) en la dirección que apunta el avión, en
   vez de mover `position` del avión -- calcando el patrón EXACTO del
   script de ejemplo (`camera_walk_ecef`), dejando el avión casi fijo
   cerca del (0,0,0) local todo el tiempo. Esto es más parecido a lo que
   el plugin espera nativamente, aunque implica tocar la función
   `_procesar_vuelo()` de `principal.gd` (el `translate()` del final).
4. Si después de un rato de intentos no se logra, considerar seriamente
   volver al sistema de fotos horneadas a mano (que SÍ funcionaba bien,
   ver la sección de arriba) para Aeroparque-San Fernando mientras se
   sigue investigando Cesium en paralelo sin bloquear el poder jugar.

## Última sesión: qué se estaba haciendo cuando se escribió esto

Se terminó la noche con el terreno real de Cesium cargando (visualmente
confirmado, se ve Buenos Aires real) pero el enganche entre avión/cámara y
terreno roto (ver bug sin resolver arriba). Se probaron muchas hipótesis
(rotaciones de 90°, orden de multiplicación de matrices, ser hijo/no-hijo
del CesiumGeoreference) con ayuda cruzada de ChatGPT y Gemini -- ninguna
resolvió el síntoma, hasta que se encontró el hallazgo real (arriba) leyendo
el código fuente C++ directamente en vez de teorizar. El último cambio
aplicado (recentrar cada cuadro) no se llegó a terminar de probar/depurar
bien -- Pablo cortó la sesión porque los resultados fueron empeorando, no
mejorando, y quedaron ganas de más pero se decidió parar por la noche.

Cosas que SÍ quedaron andando bien y no hay que retocar: los controles del
avión, el acelerador W/S, el panel de selección de vuelo, la cuenta de
Cesium ion conectada (en ESTE proyecto, no solo en el de prueba), el
plugin instalado y activado, las coordenadas reales de los 8 aeropuertos,
y el bug de la Torre/IAF (asumía eje Z fijo) que si se arregló bien.

Si hay errores al abrir `mi-simulador-de-vuelo`, revisar primero: que el
plugin `cesium_godot` esté activado en Proyecto → Configuración del
Proyecto → Plugins (debería venir solo), y si el panel no aparece o hay
errores raros, "cerrar Godot completo y reabrir sin tocar el checkbox"
(ya funcionó antes para esto).
