#!/bin/sh
# Vuelve a armar los niveles de todas las canciones con el generador actual.
cd "$(dirname "$0")/.."
node tools/generar_capas.js tools canciones/quedate "Quédate un poco más" "Bolero" 147.7 "voz,bateria,bajo,guitarra,teclado,percusion" desplazado
node tools/generar_capas.js fuentes/take-it-slow canciones/take-it-slow "Take It Slow" "Rock" 150.0 "voz,coros,bateria,bajo,guitarra,teclado,percusion,cuerdas" desplazado
node tools/generar_capas.js fuentes/take-me-down-this-road canciones/take-me-down-this-road "Take Me Down This Road" "Pop" 143.3 "voz,coros,bateria,bajo,guitarra,percusion,metales"
node tools/generar_capas.js "fuentes/bailá-conmigo" canciones/baila-conmigo "Bailá Conmigo" "Latino" 169.0 "voz,coros,metales,bateria,bajo,guitarra,teclado,percusion,cuerdas,sintetizador,otros" denso
node tools/generar_capas.js fuentes/mueve-el-cuerpo canciones/mueve-el-cuerpo "Mueve el Cuerpo" "Merengue" 129.6 "voz,coros,bateria,bajo,guitarra,teclado,percusion,sintetizador,otros,metales" desplazado
node tools/generar_capas.js fuentes/mi-blues canciones/mi-blues "Mi Blues, Mi Blues" "Blues" 236.4 "voz,coros,bateria,bajo,guitarra,teclado,percusion" denso
node tools/generar_capas.js fuentes/la-noche-es-nuestra canciones/la-noche-es-nuestra "La Noche es Nuestra" "Cumbia" 162 "voz,coros,metales,bateria,bajo,guitarra,teclado,percusion,cuerdas,sintetizador,otros"
node tools/generar_capas.js fuentes/soy-yo canciones/soy-yo "Soy Yo" "Rock" 136 "voz,coros,maderas,bateria,bajo,guitarra,teclado,percusion,sintetizador,otros,metales" cuantizar
node tools/generar_capas.js fuentes/somos-fuego canciones/somos-fuego "Somos Fuego" "Rock pesado" 150 "voz,coros,bateria,bajo,guitarra,sintetizador,otros"
node tools/generar_capas.js fuentes/somos-fuego-adam canciones/somos-fuego-adam "Somos Fuego (feat. Adam)" "Rock pesado, dúo" 154 "voz,coros,bateria,bajo,guitarra,teclado,percusion,sintetizador,otros"
node tools/generar_capas.js fuentes/volve-a-mi-lado canciones/volve-a-mi-lado "Volvé a mi lado" "Balada rock" 200 "voz,bateria,bajo,guitarra,teclado,percusion,cuerdas,sintetizador" tranqui
node tools/generar_capas.js fuentes/nadie-nos-baja canciones/nadie-nos-baja "Nadie Nos Baja" "Pop con rap" 89.2 "voz,coros,bateria,bajo,guitarra,teclado,percusion,cuerdas,sintetizador"
node tools/generar_capas.js fuentes/mi-buenos-aires canciones/mi-buenos-aires "Mi Buenos Aires" "Tango" 201.8 "voz,coros,bajo,guitarra,teclado,cuerdas,sintetizador,metales,maderas"
node tools/generar_capas.js fuentes/quedate-conmigo canciones/quedate-conmigo "Quédate conmigo" "Balada lenta" 232 "voz,coros,bateria,bajo,guitarra,teclado,percusion,sintetizador"
