/* Mudanza de Piano Hero: GitHub Pages -> Cloudflare. El progreso (estrellas, notas, ajustes, canciones sacadas...)
   vive en el localStorage de cada dirección, así que se copia de la vieja a la nueva con un botón. */
(function () {
  var VIEJO = 'https://pablomatas1404.github.io', NUEVO = 'https://piano-hero.gastos-pablo.workers.dev';
  var o = location.origin, ls;
  try { ls = window.localStorage; ls.length; } catch (e) { return; }
  function cartel(html) {
    var d = document.createElement('div');
    d.style.cssText = 'position:fixed;left:0;right:0;top:0;z-index:99999;background:#7c4dff;color:#fff;font:600 15px/1.35 system-ui,sans-serif;padding:10px 14px;text-align:center;box-shadow:0 2px 10px rgba(0,0,0,.4)';
    d.innerHTML = html; document.body.appendChild(d); return d;
  }
  function listo(f) { if (document.body) f(); else document.addEventListener('DOMContentLoaded', f); }
  if (o === VIEJO) {
    var quien = window.opener || (window.parent !== window ? window.parent : null);
    if (/[?&]exportar=1/.test(location.search) && quien) {
      var dump = {}; for (var i = 0; i < ls.length; i++) { var k = ls.key(i); dump[k] = ls.getItem(k); }
      try { quien.postMessage({ ph: 'progreso', datos: dump }, NUEVO); } catch (e) {}
      listo(function () { cartel('Listo, copiando tu progreso... ya podés cerrar esta ventana.'); });
      setTimeout(function () { try { window.close(); } catch (e) {} }, 1500);
      return;
    }
    listo(function () { var c = cartel('🚀 Piano Hero se mudó a una dirección nueva (más lugar para canciones). <a href="' + NUEVO + '/?traer=1" style="color:#ffe27a;text-decoration:underline">Tocá acá para ir</a>'); });
  } else if (o === NUEVO) {
    if (ls.getItem('ph_mudanza_hecha')) return;
    window.addEventListener('message', function (ev) {
      if (ev.origin !== VIEJO || !ev.data || ev.data.ph !== 'progreso') return;
      var d = ev.data.datos || {}, n = 0;
      for (var k in d) { if (Object.prototype.hasOwnProperty.call(d, k)) { try { ls.setItem(k, d[k]); n++; } catch (e) {} } }
      ls.setItem('ph_mudanza_hecha', '1');
      location.replace(NUEVO + '/');
    });
    listo(function () {
      var c = cartel('Para traer tu progreso de la página anterior (estrellas, ajustes, canciones sacadas): <button id="ph-traer" style="margin-left:8px;padding:6px 14px;border:0;border-radius:8px;font:700 15px system-ui;background:#ffe27a;color:#222">Traer mi progreso</button> <button id="ph-no" style="margin-left:6px;padding:6px 10px;border:0;border-radius:8px;font:600 14px system-ui;background:rgba(255,255,255,.25);color:#fff">Empezar de cero</button>');
      document.getElementById('ph-traer').onclick = function () { window.open(VIEJO + '/piano-hero/?exportar=1', 'ph_mudanza', 'width=520,height=360'); };
      document.getElementById('ph-no').onclick = function () { ls.setItem('ph_mudanza_hecha', '1'); c.parentNode.removeChild(c); };
    });
  }
})();
