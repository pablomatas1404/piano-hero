#!/bin/sh
# Calcula y guarda la ganancia de la banda de cada canción (para que la suma de pistas no suene más fuerte ni sature).
# Uso: NODE_PATH=<carpeta con mpg123-decoder> sh tools/medir_todo.sh
cd "$(dirname "$0")/.."
for p in quedate:quedate take-it-slow:take-it-slow take-me-down-this-road:take-me-down-this-road baila-conmigo:bailá-conmigo mueve-el-cuerpo:mueve-el-cuerpo mi-blues:mi-blues la-noche-es-nuestra:la-noche-es-nuestra soy-yo:soy-yo; do
  c="${p%%:*}"; f="${p##*:}"
  echo "== $c"; node tools/medir_mezcla.js "canciones/$c" "fuentes/$f/cancion.mp3" "canciones/$c/capas.json"
done
