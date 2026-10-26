// Mide cuánta energía hay en una pista (RMS cada 0.1 s) y la guarda en un .json.
// Sirve para descartar notas "fantasma" que Basic Pitch inventa donde la pista está en silencio.
// Uso: NODE_PATH=<carpeta con mpg123-decoder> node envolvente.js <pista.mp3> <salida.json>
const fs = require('fs');
const { MPEGDecoder } = require('mpg123-decoder');
(async () => {
  const d = new MPEGDecoder(); await d.ready;
  const r = d.decode(new Uint8Array(fs.readFileSync(process.argv[2])));
  const x = r.channelData[0], sr = r.sampleRate, bin = Math.floor(sr * 0.1), out = [];
  for (let i = 0; i < x.length; i += bin) { let s = 0; const e = Math.min(i + bin, x.length); for (let j = i; j < e; j++) s += x[j] * x[j]; out.push(+Math.sqrt(s / (e - i)).toFixed(4)); }
  fs.writeFileSync(process.argv[3], JSON.stringify(out));
})();
