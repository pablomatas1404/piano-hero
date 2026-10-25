// Arma las capas de juego (bajo / melodía / acordes) y sus 3 niveles a partir de las notas que saca Basic Pitch.
// Uso: node generar_capas.js   ->  escribe ../canciones/quedate/capas.json
const fs = require('fs'), path = require('path');
const leer = f => JSON.parse(fs.readFileSync(path.join(__dirname, f), 'utf8'));
const redondeo = x => Math.round(x * 1000) / 1000;

function plegar(m, lo, hi) { while (m < lo) m += 12; while (m > hi) m -= 12; return m; }

// Junta repeticiones pegadas de la misma nota.
function fusionar(notas) {
  const out = [];
  for (const n of notas.slice().sort((a, b) => a.t - b.t)) {
    const u = out[out.length - 1];
    if (u && u.m === n.m && n.t - (u.t + u.d) < 0.08) u.d = n.t + n.d - u.t;
    else out.push({ ...n });
  }
  return out;
}
// Deja las notas largas y separadas: así queda más fácil.
function ralear(notas, durMin, sepMin) {
  const out = [];
  for (const n of notas) {
    if (n.d < durMin) continue;
    const u = out[out.length - 1];
    if (u && n.t - u.t < sepMin) continue;
    out.push(n);
  }
  return out;
}

// ---------- BAJO ----------
const bajoCrudo = leer('bajo_notas.json')
  .filter(n => n.durationSeconds >= 0.1 && n.amplitude >= 0.3)
  .map(n => ({ t: n.startTimeSeconds, d: n.durationSeconds, m: plegar(n.pitchMidi, 36, 59), h: 'B' }));
const bajo = fusionar(bajoCrudo);

// ---------- MELODÍA (voz) : una sola nota a la vez ----------
const vozCrudo = leer('voz_notas.json')
  .filter(n => n.pitchMidi >= 52 && n.durationSeconds >= 0.12 && n.amplitude >= 0.3)
  .sort((a, b) => a.startTimeSeconds - b.startTimeSeconds);
const vozMono = [];
for (const n of vozCrudo) {
  const u = vozMono[vozMono.length - 1];
  const nota = { t: n.startTimeSeconds, d: n.durationSeconds, m: plegar(n.pitchMidi, 57, 81), h: 'R' };
  if (u && nota.t - u.t < 0.06) { if (nota.m > u.m) vozMono[vozMono.length - 1] = nota; } else vozMono.push(nota);
}
// que no se pisen
for (let i = 0; i < vozMono.length - 1; i++) vozMono[i].d = Math.min(vozMono[i].d, vozMono[i + 1].t - vozMono[i].t - 0.02);
const voz = fusionar(vozMono.filter(n => n.d > 0.05));

// ---------- ACORDES (guitarra) : se agrupan las notas que arrancan juntas ----------
const guiCrudo = leer('guitarra_notas.json')
  .filter(n => n.durationSeconds >= 0.15 && n.amplitude >= 0.3)
  .sort((a, b) => a.startTimeSeconds - b.startTimeSeconds);
const acordes = [];
for (const n of guiCrudo) {
  const u = acordes[acordes.length - 1];
  const m = plegar(n.pitchMidi, 40, 64);
  if (u && n.startTimeSeconds - u.t < 0.35) { if (!u.ms.includes(m)) u.ms.push(m); u.d = Math.max(u.d, n.startTimeSeconds + n.durationSeconds - u.t); }
  else acordes.push({ t: n.startTimeSeconds, d: n.durationSeconds, ms: [m] });
}
function acordeANotas(a, cuantas) {
  const ms = [...new Set(a.ms)].sort((x, y) => x - y);
  const cls = new Set(); const elegidas = [];
  for (const m of ms) { if (!cls.has(m % 12) && elegidas.length < cuantas) { cls.add(m % 12); elegidas.push(m); } }
  return elegidas.map(m => ({ t: a.t, d: Math.max(0.3, Math.min(a.d, 2)), m, h: 'L' }));
}
function nivelAcordes(cuantas, durMin, sepMin) {
  const out = []; let ult = -9;
  for (const a of acordes) {
    if (a.d < durMin || a.t - ult < sepMin) continue;
    ult = a.t; out.push(...acordeANotas(a, cuantas));
  }
  return out;
}
function nivelFacil(notas) { return notas; }

const capas = {
  bajo: {
    facil: ralear(bajo, 0.5, 1.2),
    medio: ralear(bajo, 0.3, 0.45),
    dificil: bajo
  },
  melodia: {
    facil: ralear(voz, 0.45, 1.0),
    medio: ralear(voz, 0.28, 0.4),
    dificil: voz
  },
  acordes: {
    facil: nivelAcordes(1, 0.4, 1.6),
    medio: nivelAcordes(3, 0.35, 0.9),
    dificil: nivelAcordes(3, 0.2, 0.4)
  }
};
const dur = 147.7;
const salida = {
  titulo: 'Quédate un poco más', estilo: 'Bolero', duracion: dur,
  partes: [{ n: 'Intro', a: 0, b: 45 }, { n: 'Medio', a: 45, b: 98 }, { n: 'Final', a: 98, b: dur }],
  stems: ['voz', 'bateria', 'bajo', 'guitarra', 'teclado', 'percusion'],
  capas: {}
};
for (const k of Object.keys(capas)) {
  salida.capas[k] = {};
  for (const nv of Object.keys(capas[k])) {
    salida.capas[k][nv] = capas[k][nv].map(n => [redondeo(n.t), redondeo(n.d), n.m]);
    console.log(k, nv, capas[k][nv].length);
  }
}
fs.writeFileSync(path.join(__dirname, '../canciones/quedate/capas.json'), JSON.stringify(salida));
