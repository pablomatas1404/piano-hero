// Suma todas las pistas de una canción y la compara con el MP3 original: pico y volumen (RMS).
// Si el pico de la suma pasa de 1.0, suena saturada: hay que bajarle la ganancia.
// Uso: NODE_PATH=<carpeta con mpg123-decoder> node medir_mezcla.js <carpeta de pistas> <mp3 original>
const fs = require('fs'), path = require('path');
const { MPEGDecoder } = require('mpg123-decoder');
async function leer(p) { const d = new MPEGDecoder(); await d.ready; const r = d.decode(new Uint8Array(fs.readFileSync(p))); return r.channelData; }
(async () => {
  const dir = process.argv[2], orig = await leer(process.argv[3]);
  const n = orig[0].length, suma = [new Float32Array(n), new Float32Array(n)];
  for (const f of fs.readdirSync(dir).filter(f => f.endsWith('.mp3'))) {
    const c = await leer(path.join(dir, f));
    for (let k = 0; k < 2; k++) { const x = c[k] || c[0]; for (let i = 0; i < n && i < x.length; i++) suma[k][i] += x[i]; }
  }
  const est = a => { let pico = 0, s = 0, clip = 0; for (let k = 0; k < 2; k++) for (let i = 0; i < n; i += 3) { const v = Math.abs((a[k] || a[0])[i]); if (v > pico) pico = v; if (v > 1) clip++; s += v * v; } return { pico: +pico.toFixed(2), rms: +Math.sqrt(s / (n * 2 / 3)).toFixed(3), muestrasSobre1: clip }; };
  const o = est(orig), m = est(suma); console.log(JSON.stringify({ original: o, suma: m }));
  // si se pide, guarda la ganancia que iguala el volumen de la suma al del MP3 original
  if (process.argv[4]) { const f = process.argv[4], j = JSON.parse(fs.readFileSync(f, 'utf8')); j.ganancia = +Math.min(1, o.rms / m.rms).toFixed(3); fs.writeFileSync(f, JSON.stringify(j)); console.log('ganancia', j.ganancia); }
})();
