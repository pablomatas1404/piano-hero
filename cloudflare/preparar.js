// Arma cloudflare/dist con lo que la página necesita para publicar en Cloudflare.
// Cloudflare no acepta archivos de más de 25 MB: el script gigante de index.html se parte en 2 archivos.
// Uso: node cloudflare/preparar.js     (desde la carpeta PIANO HERO)
const fs = require('fs'), path = require('path');
const NL = String.fromCharCode(10);
const raiz = path.resolve(__dirname, '..'), dist = path.join(__dirname, 'dist');
fs.rmSync(dist, { recursive: true, force: true }); fs.mkdirSync(dist, { recursive: true });
let h = fs.readFileSync(path.join(raiz, 'index.html'), 'utf8');
const re = /<script>([\s\S]*?)<\/script>/g; let m, mayor = null;
while ((m = re.exec(h))) if (!mayor || m[1].length > mayor[1].length) mayor = m;
const lineas = mayor[1].split(NL), total = mayor[1].length;
// la lista enorme de MIDIs vive dentro de function buildMidisFolderSongs() { return [ ... ]; }:
// se parte en dos funciones (A y B) que se juntan al llamarla
const ini = lineas.findIndex(l => l === 'function buildMidisFolderSongs() { return [');
const fin = lineas.findIndex((l, i) => i > ini && l === ']; }');
if (ini < 0 || fin < 0) throw new Error('no encontré buildMidisFolderSongs');
let acum = 0, corte = -1;
for (let i = 0; i < lineas.length; i++) { acum += lineas[i].length + 1; if (i > ini && i < fin - 1 && acum > total / 2 && /^  midiFolderSong\(/.test(lineas[i + 1])) { corte = i + 1; break; } }
if (corte < 0) throw new Error('no encontré dónde cortar');
const cab = [].concat(lineas.slice(0, ini), ['function buildMidisFolderSongs() { return (window.__midisA && window.__midisB) ? window.__midisA().concat(window.__midisB()) : []; }']).join(NL);
const datA = [].concat(['window.__midisA = function () { return ['], lineas.slice(ini + 1, corte), ['];};']).join(NL);
const datB = [].concat(['window.__midisB = function () { return ['], lineas.slice(corte, fin), ['];};']).join(NL);
const cola = '';
const cuerpo = cab + NL + lineas.slice(fin + 1).join(NL);
// cabeza y cola son livianas; los datos (27 MB de MIDIs) solo se cargan si NO es celular
fs.writeFileSync(path.join(dist, 'app-1.js'), cuerpo); fs.writeFileSync(path.join(dist, 'midis-a.js'), datA);
fs.writeFileSync(path.join(dist, 'midis-b.js'), datB); 
const cierre = "<" + String.fromCharCode(92) + "/script>";
h = h.replace(mayor[0], () => [
  `<script>if (!window.__MOVIL || window.__MOVIL_MIDIS) document.write('<script src="midis-a.js">${cierre}<script src="midis-b.js">${cierre}');</script>`,
  '<script src="app-1.js"></script>'
].join(NL));
fs.writeFileSync(path.join(dist, 'index.html'), h);
const cp = (a, b) => fs.cpSync(path.join(raiz, a), path.join(dist, b || a), { recursive: true });
['yourband.js', 'version.txt', 'portada.webp', 'index.html', 'canciones'].forEach(f => cp(path.join('YOUR BAND', f)));
['acordes', 'lector-de-libros', 'mudanza.js'].forEach(f => { if (fs.existsSync(path.join(raiz, f))) cp(f); });
fs.cpSync(path.join(__dirname, 'piano'), path.join(dist, 'piano'), { recursive: true });
let grande = 0; (function w(d) { for (const f of fs.readdirSync(d, { withFileTypes: true })) { const p = path.join(d, f.name); if (f.isDirectory()) w(p); else if (fs.statSync(p).size > 25 * 1048576) { console.log('MUY GRANDE', p); grande++; } } })(dist);
console.log('listo', ['app-1.js','midis-a.js','midis-b.js'].map(f => f + ' ' + fs.statSync(path.join(dist, f)).size).join(', '), 'archivos grandes:', grande);
