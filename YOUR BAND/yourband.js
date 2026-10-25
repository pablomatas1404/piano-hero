// YOUR BAND — vive DENTRO de Piano Hero: usa su teclado, sus vistas, el Modo Espera, el tempo,
// los colores, los números y los nombres de las notas. Este archivo solo agrega: la historia, el
// menú de misiones, y la banda de fondo (las pistas de la canción) pegada al reloj del juego.
(function () {
  'use strict';
  const CARPETA = 'YOUR BAND/';
  const MISIONES = [
    { id: 'quedate', titulo: 'Quédate un poco más', estilo: 'Bolero', nivelMin: 1, carpeta: CARPETA + 'canciones/quedate/' },
    { id: 'prox2', titulo: 'Próximamente', estilo: '', nivelMin: 1, proximamente: true },
    { id: 'prox3', titulo: 'Próximamente', estilo: '', nivelMin: 1, proximamente: true },
    { id: 'prox4', titulo: 'Próximamente', estilo: '', nivelMin: 1, proximamente: true },
    { id: 'prox5', titulo: 'Próximamente', estilo: '', nivelMin: 2, proximamente: true },
    { id: 'prox6', titulo: 'Próximamente', estilo: '', nivelMin: 3, proximamente: true }
  ];
  const NIVELES = [['facil', 'Fácil'], ['medio', 'Medio'], ['dificil', 'Difícil']];
  const LS = 'yourband_v1';
  let datos = { banda: 'Your Band', rol: null, estrellas: {}, sync: 0, vistaHistoria: false };
  try { const g = JSON.parse(localStorage.getItem(LS) || 'null'); if (g) Object.assign(datos, g); } catch (e) {}
  const guardar = () => { try { localStorage.setItem(LS, JSON.stringify(datos)); } catch (e) {} };
  const totalEstrellas = () => Object.values(datos.estrellas).reduce((a, b) => a + b, 0);
  const nivelJugador = () => 1 + Math.floor(totalEstrellas() / 4);
  const esc = s => String(s).replace(/[&<>"]/g, c => ({ '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;' }[c]));

  // ---------- Estilos ----------
  const css = document.createElement('style');
  css.textContent = `
  #yb-ov { position:fixed; inset:0; z-index:99990; background:#12101c; color:#f2efff; display:none; font-family:system-ui,Segoe UI,Roboto,sans-serif; }
  #yb-ov.on { display:block; }
  .yb-p { position:absolute; inset:0; display:none; flex-direction:column; align-items:center; justify-content:center; padding:20px; text-align:center; gap:16px; overflow:auto; }
  .yb-p.on { display:flex; }
  .yb-p h1 { font-size:2.3rem; margin:0; letter-spacing:.04em; } .yb-p h2 { margin:0; font-size:1.45rem; }
  .yb-p p { margin:0; color:#a9a3c9; max-width:34em; line-height:1.5; font-size:1.05rem; }
  .yb-cartel { background:#1d1a2e; border:1px solid #3b3660; border-radius:18px; padding:26px 28px; max-width:34em; font-size:1.3rem; line-height:1.5; }
  .yb-p button, #yb-flot button { font:inherit; cursor:pointer; border:0; border-radius:14px; padding:11px 24px; background:#ffb454; color:#241a00; font-weight:700; font-size:1.02rem; }
  .yb-p button.sec, #yb-flot button.sec { background:#272340; color:#f2efff; font-weight:600; }
  .yb-p button:disabled { opacity:.4; cursor:default; }
  .yb-p input[type=text] { font:inherit; font-size:1.35rem; padding:12px 16px; border-radius:12px; border:2px solid #3b3660; background:#1d1a2e; color:#f2efff; text-align:center; width:min(90vw,20em); }
  .yb-fila { display:flex; gap:14px; flex-wrap:wrap; justify-content:center; align-items:center; }
  .yb-tarj { background:#1d1a2e; border:2px solid #3b3660; border-radius:18px; padding:18px 22px; min-width:180px; }
  .yb-tarj.el { cursor:pointer; } .yb-tarj.el:hover { border-color:#ffb454; } .yb-tarj.ap { opacity:.45; }
  .yb-grande { font-size:2.8rem; } .yb-chico { font-size:.9rem; color:#a9a3c9; } .yb-est { color:#ffb454; letter-spacing:.1em; }
  .yb-misiones { display:grid; grid-template-columns:repeat(auto-fit,minmax(210px,1fr)); gap:14px; width:min(900px,100%); }
  .yb-ops { display:flex; gap:8px; flex-wrap:wrap; justify-content:center; }
  .yb-op { background:#272340; border:2px solid transparent; border-radius:12px; padding:9px 16px; cursor:pointer; font-weight:600; }
  .yb-op.sel { border-color:#ffb454; }
  .yb-carga { width:min(360px,80vw); height:10px; background:#272340; border-radius:6px; overflow:hidden; } .yb-carga div { height:100%; width:0; background:#ffb454; transition:width .2s; }
  #yb-flot { position:relative; z-index:5; display:none; background:rgba(18,16,28,.92); color:#f2efff; border:1px solid #3b3660; border-radius:12px; padding:4px 8px; gap:4px 10px; align-items:center; flex-wrap:wrap; font-size:11px; max-width:340px; flex:0 1 340px; }
  #yb-flot.on { display:flex; } #yb-flot label { display:flex; align-items:center; gap:5px; color:#cfc9ee; } #yb-flot input[type=range] { width:64px; } #yb-flot button { padding:4px 10px; font-size:11px; }
  `;
  document.head.appendChild(css);

  // ---------- Pantallas ----------
  const ov = document.createElement('div'); ov.id = 'yb-ov';
  ov.innerHTML = `
  <section class="yb-p" id="yb-nombre"><div class="yb-grande">🎸🥁🎹</div><h1>YOUR BAND</h1><p>Todo grupo necesita un nombre. ¿Cómo se va a llamar el tuyo?</p>
    <input type="text" id="yb-in-nombre" value="Your Band" maxlength="30" autocomplete="off"><div class="yb-fila"><button class="sec" id="yb-b-cerrar1">Volver a Piano Hero</button><button id="yb-b-nombre">Siguiente</button></div></section>
  <section class="yb-p" id="yb-historia"><div class="yb-cartel" id="yb-cartel"></div><button id="yb-b-hist">Siguiente</button></section>
  <section class="yb-p" id="yb-rol"><h2>¿Qué vas a tocar?</h2><div class="yb-fila">
    <div class="yb-tarj el" data-rol="bajo"><div class="yb-grande">🎸</div><h2>Bajo</h2><p class="yb-chico">La base de la banda. Notas graves, de a una.</p></div>
    <div class="yb-tarj el" data-rol="teclado"><div class="yb-grande">🎹</div><h2>Teclado</h2><p class="yb-chico">La melodía con la derecha y los acordes con la izquierda.</p></div></div></section>
  <section class="yb-p" id="yb-menu"><h2 id="yb-menu-t"></h2><div class="yb-chico" id="yb-menu-n"></div><div class="yb-misiones" id="yb-lista"></div>
    <div class="yb-fila"><button class="sec" id="yb-b-rol">Cambiar instrumento</button><button class="sec" id="yb-b-nom">Cambiar nombre</button><button class="sec" id="yb-b-cerrar2">Volver a Piano Hero</button></div></section>
  <section class="yb-p" id="yb-prep"><h2 id="yb-prep-t"></h2><div class="yb-chico" id="yb-prep-s"></div>
    <div><div class="yb-chico">Dificultad</div><div class="yb-ops" id="yb-op-nivel"></div></div>
    <div><div class="yb-chico">Parte</div><div class="yb-ops" id="yb-op-parte"></div></div>
    <div><div class="yb-chico">Cómo</div><div class="yb-ops" id="yb-op-modo"></div></div>
    <p class="yb-chico">Adentro tenés el Modo Espera, el tempo, las vistas y los colores de siempre.</p>
    <div class="yb-carga" id="yb-carga-w"><div id="yb-carga-b"></div></div><div class="yb-chico" id="yb-carga-t">Cargando la banda…</div>
    <div class="yb-fila"><button class="sec" id="yb-b-pv">Volver</button><button id="yb-b-tocar" disabled>¡A tocar!</button></div></section>`;
  document.body.appendChild(ov);

  const flot = document.createElement('div'); flot.id = 'yb-flot';
  flot.innerHTML = `<button class="sec" id="yb-f-menu">🎸 Misiones</button>
    <label title="Si los ladrillos llegan antes que la música, subilo; si llegan después, bajalo">Sincro <input type="range" id="yb-f-sync" min="-500" max="500" step="10" value="0"><span id="yb-f-syncv">0 ms</span></label>
    <label>Banda <input type="range" id="yb-f-vol" min="0" max="100" value="80"></label>
    <label id="yb-f-guial"><input type="checkbox" id="yb-f-guia">Oír mi instrumento de la banda</label>`;
  // va en el encabezado, al lado de los créditos (abajo tapaba teclas del piano)
  const colCred = document.querySelector('header .credits-col');
  if (colCred) colCred.insertAdjacentElement('afterend', flot); else document.body.appendChild(flot);

  const $ = id => document.getElementById(id);
  const pantalla = id => { ov.classList.add('on'); ov.querySelectorAll('.yb-p').forEach(p => p.classList.toggle('on', p.id === id)); };
  function cerrar() { ov.classList.remove('on'); }

  $('yb-in-nombre').value = datos.banda;
  $('yb-b-nombre').onclick = () => { datos.banda = $('yb-in-nombre').value.trim() || 'Your Band'; guardar(); historia(0); };
  $('yb-in-nombre').addEventListener('keydown', e => { if (e.key === 'Enter') $('yb-b-nombre').click(); });
  $('yb-b-cerrar1').onclick = cerrar; $('yb-b-cerrar2').onclick = cerrar;
  const HIST = () => [
    `Bienvenido a <b>${esc(datos.banda)}</b>. Una banda con muchas ganas, un local de ensayo y una canción esperando.`,
    `Ya tenemos cantante, guitarrista y baterista. Suenan lindo… pero algo falta.`,
    `Falta alguien que sostenga la base de la banda. <b>Ese alguien sos vos.</b>`];
  let hi = 0;
  function historia(i) { hi = i; $('yb-cartel').innerHTML = HIST()[i]; pantalla('yb-historia'); }
  $('yb-b-hist').onclick = () => { if (hi < HIST().length - 1) historia(hi + 1); else { datos.vistaHistoria = true; guardar(); pantalla('yb-rol'); } };
  ov.querySelectorAll('[data-rol]').forEach(t => t.onclick = () => { datos.rol = t.dataset.rol; guardar(); menu(); });
  $('yb-b-rol').onclick = () => pantalla('yb-rol');
  $('yb-b-nom').onclick = () => { $('yb-in-nombre').value = datos.banda; pantalla('yb-nombre'); };

  const claveEst = (m, rol, nv) => m + '|' + rol + '|' + nv;
  function menu() {
    $('yb-menu-t').textContent = datos.banda + ' · ' + (datos.rol === 'bajo' ? '🎸 Bajo' : '🎹 Teclado');
    $('yb-menu-n').innerHTML = `Nivel ${nivelJugador()} · <span class="yb-est">★</span> ${totalEstrellas()}`;
    const c = $('yb-lista'); c.innerHTML = '';
    MISIONES.forEach(m => {
      const bloq = m.nivelMin > nivelJugador(), d = document.createElement('div');
      d.className = 'yb-tarj ' + (m.proximamente || bloq ? 'ap' : 'el');
      let est = '';
      if (!m.proximamente && !bloq) est = NIVELES.map(([k, n]) => `${n}: <span class="yb-est">${'★'.repeat(datos.estrellas[claveEst(m.id, datos.rol, k)] || 0) || '–'}</span>`).join('<br>');
      d.innerHTML = `<div class="yb-grande">${m.proximamente ? '🔒' : '🎵'}</div><h2>${esc(m.titulo)}</h2><div class="yb-chico">${esc(m.estilo)}</div>` +
        (bloq ? `<div class="yb-chico">Se abre en el nivel ${m.nivelMin}</div>` : '') + (m.proximamente && !bloq ? `<div class="yb-chico">Todavía no está lista</div>` : '') +
        (est ? `<div class="yb-chico" style="margin-top:8px">${est}</div>` : '');
      if (!m.proximamente && !bloq) d.onclick = () => prep(m);
      c.appendChild(d);
    });
    pantalla('yb-menu');
  }

  // ---------- Carga de la banda (las pistas) ----------
  let AC = null;
  const cache = {};
  function ctxAudio() { if (!AC) AC = new (window.AudioContext || window.webkitAudioContext)(); if (AC.state === 'suspended') AC.resume(); return AC; }
  async function cargar(m, prog) {
    if (cache[m.id]) return cache[m.id];
    const capas = await (await fetch(m.carpeta + 'capas.json')).json();
    const buffers = {}; let n = 0;
    await Promise.all(capas.stems.map(async s => {
      const ab = await (await fetch(m.carpeta + s + '.mp3')).arrayBuffer();
      buffers[s] = await new Promise((ok, no) => ctxAudio().decodeAudioData(ab, ok, no));
      prog(++n / capas.stems.length);
    }));
    return (cache[m.id] = { capas, buffers });
  }

  let sel = { m: null, nv: 'facil', parte: 'all', modo: 'tocar', d: null };
  function ops(cont, lista, actual, cb) {
    cont.innerHTML = '';
    lista.forEach(([k, n]) => { const b = document.createElement('div'); b.className = 'yb-op' + (String(k) === String(actual) ? ' sel' : ''); b.textContent = n; b.onclick = () => cb(k); cont.appendChild(b); });
  }
  function dibujarOps() {
    ops($('yb-op-nivel'), NIVELES, sel.nv, k => { sel.nv = k; dibujarOps(); });
    const partes = [['all', 'Canción completa']]; if (sel.d) sel.d.capas.partes.forEach((p, i) => partes.push([i, p.n]));
    ops($('yb-op-parte'), partes, sel.parte, k => { sel.parte = k; dibujarOps(); });
    ops($('yb-op-modo'), [['tocar', 'Tocar con la banda'], ['practica', 'Practicar sin música']], sel.modo, k => { sel.modo = k; dibujarOps(); });
  }
  async function prep(m) {
    sel.m = m; sel.d = cache[m.id] || null; pantalla('yb-prep');
    $('yb-prep-t').textContent = m.titulo; $('yb-prep-s').textContent = m.estilo + ' · ' + (datos.rol === 'bajo' ? 'Bajo' : 'Teclado');
    dibujarOps(); $('yb-b-tocar').disabled = true; $('yb-carga-w').style.display = ''; $('yb-carga-t').textContent = 'Cargando la banda…'; $('yb-carga-b').style.width = '0';
    try {
      sel.d = await cargar(m, p => { $('yb-carga-b').style.width = Math.round(p * 100) + '%'; });
      $('yb-carga-w').style.display = 'none'; $('yb-carga-t').textContent = 'Lista la banda.'; $('yb-b-tocar').disabled = false; dibujarOps();
    } catch (e) { $('yb-carga-t').textContent = 'No pude cargar la canción: ' + e.message; }
  }
  $('yb-b-pv').onclick = menu;
  $('yb-b-tocar').onclick = () => empezar();

  // ---------- Armar la canción con el motor de Piano Hero ----------
  function armarCancion() {
    const c = sel.d.capas, nv = sel.nv;
    let a = 0, b = c.duracion;
    if (sel.parte !== 'all') { const p = c.partes[sel.parte]; a = p.a; b = p.b; }
    const tr = lista => lista.filter(n => n[0] >= a - 0.01 && n[0] < b).map(n => [n[2], n[0] - a, n[1]]);
    let rh, lh;
    if (datos.rol === 'bajo') { rh = tr(c.capas.bajo[nv]); lh = []; }
    else { rh = tr(c.capas.melodia[nv]); lh = tr(c.capas.acordes[nv]); }
    const s = multiHandSong(sel.m.titulo + ' (' + (datos.rol === 'bajo' ? 'Bajo' : 'Teclado') + ')', 'YOUR BAND', nv, rh, lh, [], [], { bpm: 70, timeSig: { num: 4, den: 4 } });
    s.duration = Math.max(s.duration, (b - a) + 1.5);
    s.__yb = { a, modo: sel.modo, rol: datos.rol, nv, mision: sel.m.id, parte: sel.parte, datos: sel.d };
    return s;
  }
  async function empezar() {
    ctxAudio();
    if (typeof audioReady !== 'undefined' && !audioReady) { try { await startAudio(); } catch (e) {} }
    const s = armarCancion();
    // menos retardo del sonido que tocás: Tone programa cada nota 0.1 s adelante por defecto
    try { if (window.Tone && Tone.context) Tone.context.lookAhead = 0.015; } catch (e) {}
    cerrar();
    setHandsMode(datos.rol === 'bajo' ? 'one' : 'two', null);
    loadSong(s);
    flot.classList.add('on');
    $('yb-f-guial').style.display = s.__yb.modo === 'tocar' ? '' : 'none';
    $('yb-f-guia').checked = false;
    finVisto = false;
    startPlayWithCountdown();
  }
  $('yb-f-menu').onclick = () => { try { pauseSong(); } catch (e) {} pararBanda(); if (datos.rol) menu(); else historia(0); };
  $('yb-f-sync').value = datos.sync; $('yb-f-syncv').textContent = datos.sync + ' ms';
  $('yb-f-sync').oninput = () => { datos.sync = +$('yb-f-sync').value; $('yb-f-syncv').textContent = datos.sync + ' ms'; guardar(); };
  $('yb-f-vol').oninput = () => { if (volNodo) volNodo.gain.value = $('yb-f-vol').value / 100; };

  // ---------- La banda de fondo, pegada al reloj de Piano Hero ----------
  let volNodo = null, fuentes = [], corriendo = false, iniCtx = 0, iniPos = 0, finVisto = false;
  function pararBanda() { fuentes.forEach(f => { try { f.stop(); } catch (e) {} }); fuentes = []; corriendo = false; }
  function arrancarBanda(p) {
    pararBanda();
    const S = activeSong.__yb, ac = ctxAudio(), dur = S.datos.capas.duracion;
    if (p >= dur) return;
    if (!volNodo) { volNodo = ac.createGain(); volNodo.connect(ac.destination); }
    volNodo.gain.value = $('yb-f-vol').value / 100;
    const ya = ac.currentTime + 0.05, espera = p < 0 ? (-p) / (speed || 1) : 0;
    S.datos.capas.stems.forEach(st => {
      const src = ac.createBufferSource(); src.buffer = S.datos.buffers[st]; src.playbackRate.value = speed || 1;
      const g = ac.createGain(); const mio = (st === (S.rol === 'bajo' ? 'bajo' : 'teclado'));
      g.gain.value = mio ? ($('yb-f-guia').checked ? 0.4 : 0) : 1;
      src.connect(g); g.connect(volNodo); src._g = g; src._mio = mio;
      try { src.start(ya + espera, Math.max(0, p)); } catch (e) {}
      fuentes.push(src);
    });
    iniCtx = ya; iniPos = p; corriendo = true;
  }
  $('yb-f-guia').onchange = () => fuentes.forEach(s => { if (s._mio) s._g.gain.value = $('yb-f-guia').checked ? 0.4 : 0; });

  function bucle() {
    try {
      const S = activeSong && activeSong.__yb;
      flot.classList.toggle('on', !!S && !ov.classList.contains('on'));
      if (S) {
        const querida = S.modo === 'tocar' && isPlaying && !inCountdown && !isFrozenWaiting;
        if (querida) {
          const p = playhead - 1.0 - datos.sync / 1000 + S.a;
          if (!corriendo) arrancarBanda(p);
          else {
            const real = iniPos + (AC.currentTime - iniCtx) * (speed || 1);
            if (Math.abs(real - p) > 0.15) arrancarBanda(p);
          }
        } else if (corriendo) pararBanda();
        // fin de la canción
        if (!isPlaying && !inCountdown && playhead >= activeSong.duration - 0.05 && !finVisto) { finVisto = true; terminar(S); }
      } else if (corriendo) pararBanda();
    } catch (e) {}
    requestAnimationFrame(bucle);
  }
  function terminar(S) {
    const mias = fallingNotes.filter(n => isPlayerNote(n)), hechas = mias.filter(n => n.hit).length;
    const pct = mias.length ? hechas / mias.length : 0;
    let est = pct >= 0.9 ? 3 : pct >= 0.75 ? 2 : pct >= 0.5 ? 1 : 0;
    if (waitMode) est = Math.min(est, 1); // con Modo Espera siempre se llega al final: 1 estrella; las otras se ganan sin esperar
    if (S.parte === 'all' && S.modo === 'tocar') {
      const k = claveEst(S.mision, S.rol, S.nv);
      if ((datos.estrellas[k] || 0) < est) { datos.estrellas[k] = est; guardar(); }
    }
    try { showBigToast('🎸 ' + '★'.repeat(est) + '☆'.repeat(3 - est) + (waitMode && pct >= 0.75 ? '  (sin Modo Espera se ganan las 3)' : '')); } catch (e) {}
  }
  requestAnimationFrame(bucle);
  window.__ybEstado = () => ({ corriendo, fuentes: fuentes.length, ctx: AC && AC.state, pos: AC ? +(iniPos + (AC.currentTime - iniCtx) * (speed || 1)).toFixed(2) : null });

  // ---------- Entrada ----------
  window.ybAbrir = function () { try { pauseSong(); } catch (e) {} if (datos.vistaHistoria && datos.rol) menu(); else if (datos.vistaHistoria) pantalla('yb-rol'); else { $('yb-in-nombre').value = datos.banda; pantalla('yb-nombre'); } };
  if (location.hash === '#yourband') setTimeout(window.ybAbrir, 300);
})();
