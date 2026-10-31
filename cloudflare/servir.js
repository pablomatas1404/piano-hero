// Servidor mínimo para probar cloudflare/dist en local: node cloudflare/servir.js
const http = require('http'), fs = require('fs'), path = require('path');
const dist = path.join(__dirname, 'dist');
const tipos = { '.html': 'text/html; charset=utf-8', '.js': 'text/javascript; charset=utf-8', '.json': 'application/json', '.mp3': 'audio/mpeg', '.webp': 'image/webp', '.txt': 'text/plain' };
http.createServer((q, r) => {
  let u = decodeURIComponent(q.url.split('?')[0]); if (u.endsWith('/')) u += 'index.html';
  const f = path.join(dist, u);
  if (!f.startsWith(dist) || !fs.existsSync(f) || fs.statSync(f).isDirectory()) { r.writeHead(404); return r.end('no'); }
  r.writeHead(200, { 'Content-Type': tipos[path.extname(f)] || 'application/octet-stream' }); fs.createReadStream(f).pipe(r);
}).listen(8788, () => console.log('http://localhost:8788'));
