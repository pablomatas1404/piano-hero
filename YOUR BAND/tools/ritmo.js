// Detecta el pulso (BPM y en qué instante cae el primer golpe) a partir de la pista de batería.
// Sirve para "cuantizar": acomodar las notas que saca Basic Pitch al ritmo real de la canción.
// Uso: NODE_PATH=<carpeta con mpg123-decoder> node ritmo.js <bateria.mp3> <salida ritmo.json>
const fs = require('fs');
const { MPEGDecoder } = require('mpg123-decoder');
(async () => {
  const d = new MPEGDecoder(); await d.ready;
  const r = d.decode(new Uint8Array(fs.readFileSync(process.argv[2])));
  const x = r.channelData[0], sr = r.sampleRate, hop = Math.floor(sr * 0.01); // 10 ms
  const e = []; for (let i = 0; i + hop <= x.length; i += hop) { let s = 0; for (let j = i; j < i + hop; j++) s += x[j] * x[j]; e.push(Math.sqrt(s / hop)); }
  const f = e.map((v, i) => Math.max(0, v - (e[i - 1] || 0))); // novedad (subidas de energía)
  const N = f.length;
  // autocorrelación: período del pulso entre 330 ms (181 BPM) y 900 ms (67 BPM)
  let mejor = 0, lagMejor = 50;
  for (let lag = 33; lag <= 90; lag++) { let s = 0; for (let i = 0; i + lag < N; i++) s += f[i] * f[i + lag]; s /= (N - lag); if (s > mejor) { mejor = s; lagMejor = lag; } }
  // afinar con décimas de lag (interpolación por promedio de múltiplos)
  let mejorB = 0, beat = lagMejor;
  for (let lag = lagMejor - 1.5; lag <= lagMejor + 1.5; lag += 0.05) {
    let s = 0, c = 0; for (let k = 0; k * lag < N; k++) { const i = Math.round(k * lag); if (i < N) { s += f[i]; c++; } } s /= c; if (s > mejorB) { mejorB = s; beat = lag; }
  }
  // fase: en qué desfasaje caen más golpes
  let fase = 0, mejorF = -1;
  for (let ph = 0; ph < beat; ph += 0.5) { let s = 0, c = 0; for (let t = ph; t < N - 1; t += beat) { const i = Math.round(t); s += f[i] + 0.5 * (f[i - 1] || 0) + 0.5 * (f[i + 1] || 0); c++; } s /= c; if (s > mejorF) { mejorF = s; fase = ph; } }
  const out = { beat: +(beat * 0.01).toFixed(4), bpm: +(60 / (beat * 0.01)).toFixed(1), fase: +(fase * 0.01).toFixed(3) };
  fs.writeFileSync(process.argv[3], JSON.stringify(out)); console.log(JSON.stringify(out));
})();
