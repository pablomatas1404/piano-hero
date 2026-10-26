#!/bin/sh
# Vuelve a armar los niveles de todas las canciones con el generador actual.
cd "$(dirname "$0")/.."
node tools/generar_capas.js tools canciones/quedate "Quédate un poco más" "Bolero" 147.7 "voz,bateria,bajo,guitarra,teclado,percusion"
node tools/generar_capas.js fuentes/take-it-slow canciones/take-it-slow "Take It Slow" "Rock" 150.0 "voz,coros,bateria,bajo,guitarra,teclado,percusion,cuerdas"
node tools/generar_capas.js fuentes/take-me-down-this-road canciones/take-me-down-this-road "Take Me Down This Road" "Pop" 143.3 "voz,coros,bateria,bajo,guitarra,percusion,metales"
node tools/generar_capas.js "fuentes/bailá-conmigo" canciones/baila-conmigo "Bailá Conmigo" "Latino" 169.0 "voz,coros,metales,bateria,bajo,guitarra,teclado,percusion,cuerdas,sintetizador,otros" denso
