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
node tools/generar_capas.js fuentes/feel-the-groove canciones/feel-the-groove "Feel the Groove" "Groove (con Moises)" 252.7 "coros,bajo,bateria,guitarra,leadguitar,voz,otros"
node tools/generar_capas.js fuentes/cumbia-del-barrio canciones/cumbia-del-barrio "Cumbia del Barrio" "Cumbia argentina" 143.2 "voz,coros,metales,bateria,bajo,guitarra,teclado,percusion,cuerdas,sintetizador,otros"
node tools/generar_capas.js fuentes/vamos-despacio canciones/vamos-despacio "Vamos Despacio" "Reggae" 160.8 "voz,coros,bateria,bajo,guitarra,teclado,percusion,sintetizador,otros,metales"
node tools/generar_capas.js fuentes/ruta-del-sur canciones/ruta-del-sur "Ruta del Sur" "Rock tipo Creedence" 184.8 "voz,coros,bateria,bajo,guitarra,percusion,sintetizador,otros"
node tools/generar_capas.js fuentes/un-dia-asi canciones/un-dia-asi "Un Día Así" "Bossa nova" 131.5 "voz,coros,bateria,bajo,guitarra,teclado,percusion,metales"
node tools/generar_capas.js fuentes/neon-summer-night canciones/neon-summer-night "Neon Summer Night" "Pop años 80 (en inglés)" 198.4 "voz,coros,metales,bateria,bajo,guitarra,teclado,percusion,cuerdas,sintetizador,otros"
node tools/generar_capas.js fuentes/cover-nos-siguen-pegando canciones/cover-nos-siguen-pegando "Nos Siguen Pegando Abajo (cover)" "Charly García · cover para practicar" 196.9 "bajo,bateria,guitarra,voz,otros,piano" rapido
node tools/generar_capas.js fuentes/cover-el-rito canciones/cover-el-rito "El Rito (cover)" "Soda Stereo · cover para practicar" 387.6 "bajo,bateria,guitarra,teclado,voz,otros"
node tools/generar_capas.js fuentes/cover-the-logical-song canciones/cover-the-logical-song "The Logical Song (cover)" "Supertramp · cover para practicar" 233.2 "bajo,bateria,guitarra,teclado,voz,otros"
node tools/generar_capas.js fuentes/cover-smooth-operator canciones/cover-smooth-operator "Smooth Operator (cover)" "Sade · cover para practicar" 262.9 "bajo,bateria,guitarra,teclado,voz,otros"
node tools/generar_capas.js fuentes/cover-endless-love canciones/cover-endless-love "Endless Love (cover)" "Diana Ross y Lionel Richie · cover para practicar" 244.6 "bajo,bateria,guitarra,voz,otros,piano"
node tools/generar_capas.js fuentes/cover-sweetest-taboo canciones/cover-sweetest-taboo "Sweetest Taboo (cover)" "Sade · cover para practicar" 264.8 "bajo,bateria,guitarra,voz,otros,wind"
