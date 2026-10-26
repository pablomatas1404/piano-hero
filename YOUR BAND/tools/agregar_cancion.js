// Suma una canción nueva a YOUR BAND en un solo paso.
// Uso (desde la carpeta YOUR BAND):
//   node tools/agregar_cancion.js <carpeta fuente> <id> "<Título>" "<Estilo>" [denso|desplazado]
// La carpeta fuente tiene: cancion.mp3, stems/ (el zip descomprimido) y bajo/voz/guitarra _notas.json y _rms.json.
// Hace: copia las pistas con nombres sin tildes, arma los niveles, suma la misión al menú (usa el primer
// "Próximamente" libre), la anota en tools/regenerar_todo.sh y sube el ?v= de index.html.
const fs = require('fs'), path = require('path'), cp = require('child_process');
const [fuente, id, titulo, estilo, opcion] = process.argv.slice(2);
if (!fuente || !id || !titulo || !estilo) { console.error('Faltan datos. Mirá el comentario de arriba.'); process.exit(1); }
const raiz = path.resolve(__dirname, '..');
const NOMBRES = { 'lead vocals': 'voz', 'backing vocals': 'coros', 'drums': 'bateria', 'bass': 'bajo', 'guitar': 'guitarra', 'keyboard': 'teclado', 'percussion': 'percusion', 'synth': 'sintetizador', 'other': 'otros', 'brass': 'metales', 'strings': 'cuerdas', 'woodwinds': 'maderas' };
const srcStems = path.join(raiz, fuente, 'stems'), dest = path.join(raiz, 'canciones', id);
fs.mkdirSync(dest, { recursive: true });
const stems = [];
for (const f of fs.readdirSync(srcStems).filter(f => f.endsWith('.mp3'))) {
  const base = f.replace(/^\d+\s+/, '').replace(/\.mp3$/, '').toLowerCase();
  const n = NOMBRES[base] || base.replace(/[^a-z0-9]+/g, '');
  fs.copyFileSync(path.join(srcStems, f), path.join(dest, n + '.mp3')); stems.push(n);
}
// duración leyendo los cuadros del mp3
function dur(p) {
  const d = fs.readFileSync(p); let i = 0, t = 0;
  if (d.slice(0, 3).toString() === 'ID3') i = 10 + ((d[6] << 21) | (d[7] << 14) | (d[8] << 7) | d[9]);
  const br = [0, 32, 40, 48, 56, 64, 80, 96, 112, 128, 160, 192, 224, 256, 320, 0], sr = [44100, 48000, 32000];
  while (i + 4 < d.length) {
    if (d[i] === 255 && (d[i + 1] & 0xE0) === 0xE0 && ((d[i + 1] >> 3) & 3) === 3 && ((d[i + 1] >> 1) & 3) === 1) {
      const b = br[d[i + 2] >> 4], s = sr[(d[i + 2] >> 2) & 3], pad = (d[i + 2] >> 1) & 1;
      if (!b || s === undefined) { i++; continue; } t += 1152 / s; i += Math.floor(144000 * b / s) + pad;
    } else i++;
  }
  return Math.round(t * 10) / 10;
}
const duracion = dur(path.join(dest, 'bajo.mp3'));
const args = ['tools/generar_capas.js', fuente, 'canciones/' + id, titulo, estilo, String(duracion), stems.join(',')].concat(opcion ? [opcion] : []);
const r = cp.spawnSync('node', args, { cwd: raiz, stdio: 'inherit' });
if (r.status !== 0) process.exit(r.status);
// misión nueva en el menú
const jsPath = path.join(raiz, 'yourband.js'); let js = fs.readFileSync(jsPath, 'utf8');
const sinTildes = s => s.replace(/'/g, "\\'");
const nueva = `{ id: '${id}', titulo: '${sinTildes(titulo)}', estilo: '${sinTildes(estilo)}', nivelMin: 1, carpeta: CARPETA + 'canciones/${id}/' },`;
if (js.includes(`id: '${id}'`)) console.log('La misión ya estaba en el menú.');
else {
  const m = js.match(/\{ id: 'prox\d+'[^}]*\},?/);
  if (m) js = js.replace(m[0], () => nueva);
  else js = js.replace(/,?(\s*)\];(\s*\n\s*const NIVELES)/, (x, sp, resto) => ',' + sp + '  ' + nueva + sp + '];' + resto);
  fs.writeFileSync(jsPath, js); console.log('Misión sumada al menú.');
}
// registro en regenerar_todo.sh
const sh = path.join(__dirname, 'regenerar_todo.sh');
fs.appendFileSync(sh, `node tools/generar_capas.js ${fuente} canciones/${id} "${titulo}" "${estilo}" ${duracion} "${stems.join(',')}"${opcion ? ' ' + opcion : ''}\n`);
// subir la versión del script para evitar caché viejo
const idx = path.join(raiz, '..', 'index.html'); let h = fs.readFileSync(idx, 'utf8');
h = h.replace(/yourband.js?v=[0-9a-z]+/, 'yourband.js?v=' + new Date().toISOString().replace(/[^0-9]/g, '').slice(0, 12));
fs.writeFileSync(idx, h);
console.log('Listo:', id, duracion + ' s,', stems.length + ' pistas. Falta probar y publicar.');
