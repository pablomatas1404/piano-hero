# Pendientes de YOUR BAND (anotados 2026-10-25)

- Buscar una **guitarra eléctrica** con buen sonido (que arrastre, no tan seca como la de Piano Hero) para el bajo y para temas como el blues. Ver qué guitarra del Yamaha PSR-E473 se parece; no usar la de la banda.
- **Capa Metales** (trompeta/saxo) sacada de la pista de metales de cada canción, con los instrumentos trompeta y saxo de Piano Hero.
- **Remix / modo show en vivo**: una vez aprobada la canción en todos los niveles, una versión remix con gente gritando de fondo.
- **Niveles de práctica de acordes**: tríadas de notas naturales (do, re, mi, fa, sol, la, si), a lo sumo un menor; pedirlo también en el prompt de Suno.
- Agregar un **hombre barbudo** a la portada (la banda con su cantante).
- Subir las canciones a Spotify (no para ganar plata) cuando estén todas.
- Salida de sonido por el Yamaha/mixer; versión de baja latencia (Godot) a futuro.
- Reintentar la canción pop con voz de mujer (Take Me Down This Road salió rock/pop con otra voz).
- Canción de salsa tranquila con la misma voz de la chica (Persona de Suno).

## Para la próxima sesión (anotado 2026-10-26, noche)
- Probar y afinar según el reporte de Pablo: acordes Fácil (ahora solo la base grave al cambiar el acorde), Teclado con las dos manos, final de cada canción (sin cola de silencio), sliders Banda/Yo.
- Cuantizar al ritmo otras canciones si Pablo lo pide (herramienta lista: `tools/ritmo.js` + opción `cuantizar`; ya está en Soy Yo).
- Salida de sonido por el Yamaha / mixer (el Yamaha no sirve de salida de audio por USB, solo MIDI; ver si aparece como dispositivo de sonido en Windows).
- Volver a la presentación: sumar un hombre barbudo a la portada.
- Salsa tranquila con la misma voz de la chica (Persona de Suno) y la canción pop con voz de mujer que salió rock.
- Capa Metales, remix/modo show, subir a Spotify, versión de baja latencia (Godot) si el retardo sigue.
- Dar forma "profesional" a YOUR BAND: pulir pantallas, sonidos, portada, tutorial.

## Reporte de Pablo (2026-10-27) — a corregir
- **Nadie Nos Baja**: el Fácil le queda demasiado fácil ("muy simplón"). Sugerencia: que el Medio pase a Fácil. Antes de cambiar, Pablo va a probar el Medio para ver si es muy complejo (y recién ahí se decide). Probado con auriculares: suena muy bien.
- **Mi Buenos Aires** (tango) y **Quédate conmigo**: perfectos en Fácil, no tocar.
- **Volvé a mi lado**: "está fácil, pero está muy buena, se disfruta mucho". Dejar como está (ya tiene el Fácil tranquilo); si más adelante le parece demasiado fácil, subir un escalón.
- **Somos Fuego (feat. Adam)** — reporte, para corregir después (no tocar ahora): (1) el bajo suena "una octava abajo"; Pablo quiere probar un bajo más presente/grave moviéndolo una octava (con los botones ⬇/⬆ del panel se mueve; ver cuál queda mejor y dejarlo fijo en esa canción). (2) En el Fácil, los primeros ~30 s no cae ninguna nota (revisar si es la intro de la canción o un hueco de los niveles). Pablo prueba siempre en Fácil; en Medio quizá queda mejor.
- **La Noche es Nuestra** (cumbia): "muy bueno", pero algunas notas del bajo no entran en ritmo con la música (desfasadas). Pablo la probó en Fácil; probar en Medio para ver si tiene más adornos. Solución probable: aplicar la cuantización al ritmo (herramienta lista: `tools/ritmo.js` + opción `cuantizar` del generador, como se hizo en Soy Yo). Recordarle que la pruebe en Medio y que mire los tiempos.
- **Mi Blues, Mi Blues** — Modo Espera: la canción se frena antes de que él llegue a la nota (cuando va con el dedo al ritmo). Pide más "elástico": hasta ~0.5 s de margen. Hoy el margen es 0.22 s (en `index.html`, línea del congelado del Modo Espera: `0.04 - ((activeSong && activeSong.__yb) ? 0.22 : 0)`). Cambio de una línea; decidir si sube para todas las canciones de YOUR BAND o solo para esta.
- **Mueve el Cuerpo** — lo mismo que Mi Blues: más "elástico" en el Modo Espera para que entre al ritmo cuando va con el dedo siguiendo la música. Como ya van dos canciones con el mismo pedido (Mi Blues y Mueve el Cuerpo), conviene subir el margen del Modo Espera para TODAS las de YOUR BAND (de 0.22 s a ~0.4-0.5 s).

## Aclaración de Pablo sobre el margen del Modo Espera (2026-10-27, noche)
- El margen elástico de 0.45 s es un arreglo **provisorio para hoy**. El arreglo real: **subir un poquito los ladrillos** (que lleguen/queden un poco más arriba respecto de la línea) para que no desaparezcan antes de que él toque la nota. Con el margen, la canción ya no se frena, pero el ladrillo igual desaparece antes de que llegue el dedo.
- A hacer: correr los ladrillos un poquito (unos 0.1–0.2 s) respecto de la línea de golpe, probablemente como desplazamiento visual/de tiempo de los ladrillos; probar con Mi Blues y Mueve el Cuerpo. Ojo: el slider "Sincro" de la ventana BANDA ya mueve ladrillos vs. audio (datos.sync) — ver si sirve de base o si hace falta un ajuste aparte solo para el ladrillo.

## Hecho 2026-10-28: ladrillos levantados
Los ladrillos de YOUR BAND se dibujan 0.3 s mas arriba (llegan al teclado 0.3 s despues de la nota); el Modo Espera frena cuando el ladrillo toca el teclado (margen 0.3 s en vez de 0.45). Vale para todas las canciones (esta en drawCascadaView, variable ybAtraso).
