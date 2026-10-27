#!/bin/sh
# Pone una marca de versión nueva (fecha y hora) en index.html y en YOUR BAND/version.txt.
# Con eso el navegador baja siempre el yourband.js nuevo, y una página vieja guardada en la tablet se actualiza sola.
cd "$(dirname "$0")/../.."
STAMP=$(date +%Y%m%d%H%M%S)
sed -i -E "s/yourband\.js\?v=[0-9a-z]+/yourband.js?v=$STAMP/" index.html
printf "%s" "$STAMP" > "YOUR BAND/version.txt"
echo "version $STAMP"
