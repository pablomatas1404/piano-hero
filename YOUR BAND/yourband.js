// YOUR BAND — vive DENTRO de Piano Hero: usa su teclado, sus vistas, el Modo Espera, el tempo,
// los colores, los números y los nombres de las notas. Este archivo solo agrega: la historia, el
// menú de misiones, y la banda de fondo (las pistas de la canción) pegada al reloj del juego.
(function () {
  'use strict';
  const CARPETA = 'YOUR BAND/';
  const MISIONES = [
    { id: 'quedate', titulo: 'Quédate un poco más', estilo: 'Bolero', nivelMin: 1, carpeta: CARPETA + 'canciones/quedate/' },
    { id: 'takeitslow', titulo: 'Take It Slow', estilo: 'Rock', nivelMin: 1, carpeta: CARPETA + 'canciones/take-it-slow/' },
    { id: 'takeme', titulo: 'Take Me Down This Road', estilo: 'Pop', nivelMin: 1, carpeta: CARPETA + 'canciones/take-me-down-this-road/' },
    { id: 'baila', titulo: 'Bailá Conmigo', estilo: 'Latino', nivelMin: 1, carpeta: CARPETA + 'canciones/baila-conmigo/' },
    { id: 'mueve', titulo: 'Mueve el Cuerpo', estilo: 'Merengue', nivelMin: 1, carpeta: CARPETA + 'canciones/mueve-el-cuerpo/' },
    { id: 'mi-blues', titulo: 'Mi Blues, Mi Blues', estilo: 'Blues', nivelMin: 1, carpeta: CARPETA + 'canciones/mi-blues/' },
    { id: 'la-noche-es-nuestra', titulo: 'La Noche es Nuestra', estilo: 'Cumbia', nivelMin: 1, carpeta: CARPETA + 'canciones/la-noche-es-nuestra/' },
    { id: 'soy-yo', titulo: 'Soy Yo', estilo: 'Rock', nivelMin: 1, carpeta: CARPETA + 'canciones/soy-yo/' },
    { id: 'somos-fuego', titulo: 'Somos Fuego', estilo: 'Rock pesado', nivelMin: 1, carpeta: CARPETA + 'canciones/somos-fuego/' },
    { id: 'somos-fuego-adam', titulo: 'Somos Fuego (feat. Adam)', estilo: 'Rock pesado, dúo', nivelMin: 1, carpeta: CARPETA + 'canciones/somos-fuego-adam/' },
    { id: 'volve-a-mi-lado', titulo: 'Volvé a mi lado', estilo: 'Balada rock', nivelMin: 1, carpeta: CARPETA + 'canciones/volve-a-mi-lado/' },
    { id: 'nadie-nos-baja', titulo: 'Nadie Nos Baja', estilo: 'Pop con rap', nivelMin: 1, carpeta: CARPETA + 'canciones/nadie-nos-baja/' },
    { id: 'mi-buenos-aires', titulo: 'Mi Buenos Aires', estilo: 'Tango', nivelMin: 1, carpeta: CARPETA + 'canciones/mi-buenos-aires/' },
    { id: 'quedate-conmigo', titulo: 'Quédate conmigo', estilo: 'Balada lenta', nivelMin: 1, carpeta: CARPETA + 'canciones/quedate-conmigo/' },
    { id: 'feel-the-groove', titulo: 'Feel the Groove', estilo: 'Groove (con Moises)', nivelMin: 1, carpeta: CARPETA + 'canciones/feel-the-groove/' },
    { id: 'cumbia-del-barrio', titulo: 'Cumbia del Barrio', estilo: 'Cumbia argentina', nivelMin: 1, carpeta: CARPETA + 'canciones/cumbia-del-barrio/' },
    { id: 'vamos-despacio', titulo: 'Vamos Despacio', estilo: 'Reggae', nivelMin: 1, carpeta: CARPETA + 'canciones/vamos-despacio/' },
    { id: 'ruta-del-sur', titulo: 'Ruta del Sur', estilo: 'Rock tipo Creedence', nivelMin: 1, carpeta: CARPETA + 'canciones/ruta-del-sur/' },
    { id: 'un-dia-asi', titulo: 'Un Día Así', estilo: 'Bossa nova', nivelMin: 1, carpeta: CARPETA + 'canciones/un-dia-asi/' },
  ];
  const NIVELES = [['facil', 'Fácil'], ['medio', 'Medio'], ['dificil', 'Difícil'], ['profesional', 'Profesional (la más completa)']];
  // la versión "muy fácil" está guardada pero recién aparece si el jugador la pide (botón en la lista)
  const nivelesVis = () => (datos.muyfacil ? [['muyfacil', '🐣 Muy fácil (práctica)']].concat(NIVELES) : NIVELES);
  const LS = 'yourband_v1';
  let datos = { banda: 'Your Band', rol: null, estrellas: {}, sync: 0, vistaHistoria: false, instr: { bajo: 'bajo', teclado: 'piano' } };
  try { const g = JSON.parse(localStorage.getItem(LS) || 'null'); if (g) Object.assign(datos, g); } catch (e) {}
  if (!datos.instr) datos.instr = { bajo: 'bajo', teclado: 'piano' };
  const CAPAS = [['bajo', '🎸 Bajo'], ['melodia', '🎤 Melodía'], ['acordes', '🎹 Acordes']];
  const capasDe = rol => rol === 'bajo' ? { bajo: 1, melodia: 0, acordes: 0 } : { bajo: 0, melodia: 1, acordes: 1 };
  if (!datos.capas) datos.capas = capasDe(datos.rol);
  // nombre corto de la combinación elegida (para guardar estrellas y el instrumento)
  const claveCapas = () => { const c = datos.capas; if (c.bajo && !c.melodia && !c.acordes) return 'bajo'; if (!c.bajo && c.melodia && c.acordes) return 'teclado'; return CAPAS.filter(([k]) => c[k]).map(([k]) => k).join('+'); };
  const nombreCapas = () => CAPAS.filter(([k]) => datos.capas[k]).map(([, n]) => n).join(' + ');
  // Cartel de aviso: al sumar una segunda capa (por ejemplo bajo + acordes) se pide confirmación, porque las dos
  // van juntas a la misma mano o a las dos manos y se vuelve mucho más difícil.
  function textoAviso(c) {
    const b = c.bajo, m = c.melodia, a = c.acordes;
    if (b && a && !m) return 'Estás por usar <b>BAJO y ACORDES</b> a la vez. Los dos se tocan con la <b>mano izquierda</b>, así que van a caer las notas de los dos juntas y se vuelve <b>mucho más difícil</b>.';
    if (b && m && !a) return 'Estás por usar <b>BAJO y MELODÍA</b> a la vez: el bajo con la mano izquierda y la melodía con la derecha. Vas a tocar con <b>las dos manos</b>.';
    if (m && a && !b) return 'Estás por usar <b>MELODÍA y ACORDES</b> a la vez: la melodía con la mano derecha y los acordes con la izquierda. Vas a tocar con <b>las dos manos</b>.';
    return 'Estás por usar <b>BAJO, MELODÍA y ACORDES</b> a la vez. Es lo más difícil de todo: <b>las dos manos</b> y muchas notas juntas.';
  }
  function alternarCapa(k, despues) {
    const ya = !!datos.capas[k], otras = CAPAS.filter(([q]) => q !== k && datos.capas[q]).length;
    if (ya) { if (!otras) return; datos.capas[k] = 0; guardar(); despues(); return; } // apagar una capa: sin aviso
    if (!otras) { datos.capas[k] = 1; guardar(); despues(); return; }
    const prueba = Object.assign({}, datos.capas, { [k]: 1 });
    const fondo = document.createElement('div');
    fondo.style.cssText = 'position:fixed;inset:0;z-index:99995;background:rgba(0,0,0,.65);display:flex;align-items:center;justify-content:center;padding:20px';
    fondo.innerHTML = '<div style="background:#1d1a2e;color:#f2efff;border:2px solid #ffb454;border-radius:18px;padding:22px 24px;max-width:420px;font-family:system-ui,sans-serif;line-height:1.5"><div style="font-size:1.1rem;margin-bottom:14px">⚠️ ' + textoAviso(prueba) + '</div><div style="color:#a9a3c9;margin-bottom:16px">¿Estás de acuerdo?</div><div style="display:flex;gap:10px;justify-content:flex-end"><button id="yb-av-no" style="font:inherit;cursor:pointer;border:0;border-radius:12px;padding:10px 18px;background:#272340;color:#f2efff;font-weight:600">No, dejar como estaba</button><button id="yb-av-si" style="font:inherit;cursor:pointer;border:0;border-radius:12px;padding:10px 18px;background:#ffb454;color:#241a00;font-weight:700">Sí, quiero las dos</button></div></div>';
    document.body.appendChild(fondo);
    fondo.querySelector('#yb-av-no').onclick = () => fondo.remove();
    fondo.querySelector('#yb-av-si').onclick = () => { fondo.remove(); datos.capas[k] = 1; guardar(); despues(); };
  }
  const INSTRS_BASE = [['piano', '🎹 Piano'], ['organo', '🎛️ Órgano'], ['guitarra', '🪕 Guitarra'], ['guitarraE', '🎸 Guitarra eléctrica'], ['violin', '🎻 Violín'], ['cello', '🎻 Cello'], ['flauta', '🪈 Flauta'], ['sax', '🎷 Saxo'], ['trompeta', '🎺 Trompeta'], ['vibrafono', '🔔 Vibráfono']];
  // Si el sonido va por el Yamaha (Sonido: Teclado MIDI), se le pide el instrumento con un cambio de programa (General MIDI, canal 1)
  const PROGRAMA_YAMAHA = { bajo: 33, piano: 0, organo: 18, guitarra: 25, guitarraE: 27, violin: 40, cello: 42, flauta: 73, sax: 65, trompeta: 56, vibrafono: 11 };
  // el bajo (guitarra más grave) solo cuando tocás SOLO el bajo
  const instrsDisp = () => claveCapas() === 'bajo' ? [['bajo', '🎸 Bajo (piano grave)']].concat(INSTRS_BASE) : INSTRS_BASE;
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
  #yb-nombre { background:#000 url('YOUR BAND/portada.webp') center top / cover no-repeat; justify-content:flex-end; padding-bottom:5vh; }
  .yb-pie { background:rgba(10,8,18,.74); padding:16px 24px; border-radius:18px; display:flex; flex-direction:column; align-items:center; gap:12px; backdrop-filter:blur(3px); border:1px solid rgba(255,255,255,.12); }
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
  #yb-flot { position:relative; z-index:5; display:none; }
  #yb-flot.on { display:block; }
  #yb-flot > button { padding:5px 12px; font-size:12px; white-space:nowrap; }
  #yb-pop { position:fixed; z-index:99980; display:none; background:#1d1a2e; color:#f2efff; border:1px solid #3b3660; border-radius:14px; padding:12px 14px; gap:10px; flex-direction:column; font-size:13px; width:min(92vw,330px); box-shadow:0 8px 30px rgba(0,0,0,.5); }
  #yb-pop.on { display:flex; } #yb-pop label { display:flex; align-items:center; gap:8px; color:#cfc9ee; justify-content:space-between; } #yb-pop input[type=range] { width:150px; }
  #yb-pop .yb-ops .yb-op { padding:6px 12px; font-size:12px; }
  #yb-pop button { font:inherit; cursor:pointer; border:0; border-radius:12px; padding:8px 14px; background:#272340; color:#f2efff; font-weight:600; }
  `;
  document.head.appendChild(css);

  // ---------- Pantallas ----------
  const ov = document.createElement('div'); ov.id = 'yb-ov';
  ov.innerHTML = `
  <section class="yb-p" id="yb-nombre"><div class="yb-pie"><p>Todo grupo necesita un nombre. ¿Cómo se va a llamar el tuyo?</p>
    <input type="text" id="yb-in-nombre" value="Your Band" maxlength="30" autocomplete="off"><div class="yb-fila"><button class="sec" id="yb-b-cerrar1">Volver a Piano Hero</button><button id="yb-b-nombre">Siguiente</button></div></div></section>
  <section class="yb-p" id="yb-historia"><div class="yb-cartel" id="yb-cartel"></div><button id="yb-b-hist">Siguiente</button></section>
  <section class="yb-p" id="yb-rol"><h2>¿Qué vas a tocar?</h2><div class="yb-fila">
    <div class="yb-tarj el" data-rol="bajo"><div class="yb-grande">🎸</div><h2>Bajo</h2><p class="yb-chico">La base de la banda. Notas graves, de a una.</p></div>
    <div class="yb-tarj el" data-rol="teclado"><div class="yb-grande">🎹</div><h2>Teclado</h2><p class="yb-chico">La melodía con la derecha y los acordes con la izquierda.</p></div></div></section>
  <section class="yb-p" id="yb-menu"><h2 id="yb-menu-t"></h2><div class="yb-chico" id="yb-menu-n"></div><div class="yb-misiones" id="yb-lista"></div>
    <div class="yb-fila"><button class="sec" id="yb-b-rol">Cambiar instrumento</button><button class="sec" id="yb-b-nom">Cambiar nombre</button><button class="sec" id="yb-b-cerrar2">Volver a Piano Hero</button></div></section>
  <section class="yb-p" id="yb-prep"><h2 id="yb-prep-t"></h2><div class="yb-chico" id="yb-prep-s"></div>
    <div><div class="yb-chico">Qué tocar (podés sumar más de una)</div><div class="yb-ops" id="yb-op-capas"></div></div>
    <div><div class="yb-chico">Dificultad</div><div class="yb-ops" id="yb-op-nivel"></div></div>
    <div><div class="yb-chico">Parte</div><div class="yb-ops" id="yb-op-parte"></div></div>
    <div><div class="yb-chico">Cómo</div><div class="yb-ops" id="yb-op-modo"></div></div>
    <div><div class="yb-chico">Sonido de lo que tocás</div><div class="yb-ops" id="yb-op-instr"></div></div>
    <p class="yb-chico">Adentro tenés el Modo Espera, el tempo, las vistas y los colores de siempre.</p>
    <div class="yb-carga" id="yb-carga-w"><div id="yb-carga-b"></div></div><div class="yb-chico" id="yb-carga-t">Cargando la banda…</div>
    <div class="yb-fila"><button class="sec" id="yb-b-pv">Volver</button><button id="yb-b-tocar" disabled>¡A tocar!</button></div></section>`;
  document.body.appendChild(ov);

  const flot = document.createElement('div'); flot.id = 'yb-flot';
  flot.innerHTML = '<button class="sec" id="yb-f-abrir" title="Instrumento, sincronización y volumen de la banda">🎸 BANDA ▾</button>';
  // va en el encabezado, al lado de los créditos (abajo tapaba teclas del piano)
  const colCred = document.querySelector('header .credits-col');
  if (colCred) colCred.insertAdjacentElement('afterend', flot); else document.body.appendChild(flot);
  // segundo botón, en la botonera de arriba del teclado (por si el encabezado está escondido)
  const btnWait = document.getElementById('btn-wait');
  const btn2 = document.createElement('button'); btn2.className = 'btn'; btn2.id = 'yb-f-abrir2'; btn2.textContent = '🎸 BANDA ▾'; btn2.style.display = 'none';
  if (btnWait) btnWait.insertAdjacentElement('beforebegin', btn2);
  // dos controles de volumen al lado de "Aprender": la banda y lo que tocás vos
  const btnAprender = [...document.querySelectorAll('button')].find(b => /Aprender/.test(b.textContent) && b.getAttribute('onclick') === 'openLearnHub()');
  const vols = document.createElement('span'); vols.id = 'yb-vols'; vols.style.cssText = 'display:none;align-items:center;gap:8px;margin-left:8px;font-size:11px;color:var(--muted)';
  vols.innerHTML = '<label title="Volumen de la banda de fondo" style="display:flex;align-items:center;gap:4px">🥁 Banda <input type="range" id="yb-t-banda" min="0" max="100" value="80" style="width:70px"></label><label title="Volumen de lo que tocás vos" style="display:flex;align-items:center;gap:4px">🎹 Yo <input type="range" id="yb-t-yo" min="0" max="100" value="80" style="width:70px"></label><label title="Cuánto más arriba caen los ladrillos (se corrige por canción)" style="display:flex;align-items:center;gap:4px">🧱 Ladrillos <input type="range" id="yb-t-lad" min="0" max="60" value="20" style="width:70px"> <span id="yb-t-lad-v" style="min-width:30px">0.20</span></label>';
  if (btnAprender) btnAprender.insertAdjacentElement('afterend', vols); else if (btnWait) btnWait.insertAdjacentElement('beforebegin', vols);
  const pop = document.createElement('div'); pop.id = 'yb-pop';
  pop.innerHTML = `<button id="yb-f-menu">🎸 Misiones y estrellas</button>
    <div><div class="yb-chico" style="margin-bottom:4px">Sonido de lo que tocás</div><div class="yb-ops" id="yb-f-instr"></div></div>
    <div class="yb-ops" id="yb-f-oct" style="display:none"><div class="yb-op" id="yb-f-grave">⬇ Bajo más grave</div><div class="yb-op" id="yb-f-agudo">⬆ Bajo más agudo</div></div>
    <label title="Si los ladrillos llegan antes que la música, subilo; si llegan después, bajalo">Sincro <input type="range" id="yb-f-sync" min="-500" max="500" step="10" value="0"><span id="yb-f-syncv">0 ms</span></label>
    <label id="yb-f-guial"><span>Oír mi instrumento de la banda</span><input type="checkbox" id="yb-f-guia"></label>`;
  document.body.appendChild(pop);

  const $ = id => document.getElementById(id);
  const pantalla = id => { ov.classList.add('on'); ov.querySelectorAll('.yb-p').forEach(p => p.classList.toggle('on', p.id === id)); };
  function cerrar() { ov.classList.remove('on'); }

  $('yb-in-nombre').value = datos.banda;
  $('yb-b-nombre').onclick = () => { datos.banda = $('yb-in-nombre').value.trim() || 'Your Band'; guardar(); if (datos.vistaHistoria) { if (datos.rol) { cerrar(); renderMiLista(); } else pantalla('yb-rol'); } else historia(0); };
  $('yb-in-nombre').addEventListener('keydown', e => { if (e.key === 'Enter') $('yb-b-nombre').click(); });
  $('yb-b-cerrar1').onclick = cerrar; $('yb-b-cerrar2').onclick = cerrar;
  const HIST = () => [
    `Bienvenido a <b>${esc(datos.banda)}</b>. Una banda con muchas ganas, un local de ensayo y una canción esperando.`,
    `Ya tenemos cantante, guitarrista y baterista. Suenan lindo… pero algo falta.`,
    `Falta alguien que sostenga la base de la banda. <b>Ese alguien sos vos.</b>`];
  let hi = 0;
  function historia(i) { hi = i; $('yb-cartel').innerHTML = HIST()[i]; pantalla('yb-historia'); }
  $('yb-b-hist').onclick = () => { if (hi < HIST().length - 1) historia(hi + 1); else { datos.vistaHistoria = true; guardar(); pantalla('yb-rol'); } };
  ov.querySelectorAll('[data-rol]').forEach(t => t.onclick = () => { datos.rol = t.dataset.rol; datos.capas = capasDe(datos.rol); guardar(); cerrar(); renderMiLista(); });
  $('yb-b-rol').onclick = () => pantalla('yb-rol');
  $('yb-b-nom').onclick = () => { $('yb-in-nombre').value = datos.banda; pantalla('yb-nombre'); };

  const claveEst = (m, rol, nv) => m + '|' + rol + '|' + nv;
  function menu() {
    $('yb-menu-t').textContent = datos.banda + ' · ' + nombreCapas();
    $('yb-menu-n').innerHTML = `Nivel ${nivelJugador()} · <span class="yb-est">★</span> ${totalEstrellas()}`;
    const c = $('yb-lista'); c.innerHTML = '';
    MISIONES.forEach(m => {
      const bloq = m.nivelMin > nivelJugador(), d = document.createElement('div');
      d.className = 'yb-tarj ' + (m.proximamente || bloq ? 'ap' : 'el');
      let est = '';
      if (!m.proximamente && !bloq) est = nivelesVis().map(([k, n]) => `${n}: <span class="yb-est">${'★'.repeat(datos.estrellas[claveEst(m.id, claveCapas(), k)] || 0) || '–'}</span>`).join('<br>');
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
  // Celular (pantalla chica): el audio se procesa a 24 kHz (la mitad de trabajo y de memoria que a 48 kHz) y la banda se re-sincroniza con más paciencia.
  const celu = (() => { try { return window.matchMedia("(pointer: coarse)").matches && Math.min(screen.width, screen.height) < 600; } catch (e) { return false; } })();
  function ctxAudio() { if (!AC) { try { AC = celu ? new (window.AudioContext || window.webkitAudioContext)({ sampleRate: 24000 }) : new (window.AudioContext || window.webkitAudioContext)(); } catch (e) { AC = new (window.AudioContext || window.webkitAudioContext)(); } } if (AC.state === 'suspended') AC.resume(); return AC; }
  // Memoria (tablet): cada canción decodificada ocupa cientos de MB. Se guarda UNA sola a la vez: al cargar otra,
  // se corta la que suena y se suelta la anterior. En pantallas táctiles las pistas se guardan en mono (la mitad de memoria).
  function liberarOtras(idActual) {
    try { if (activeSong && activeSong.__yb && activeSong.__yb.mision !== idActual) { try { pauseSong(); } catch (e) {} pararBanda(); } } catch (e) {}
    for (const k of Object.keys(cache)) {
      if (k === idActual) continue;
      const o = cache[k]; if (o && o.buffers) { for (const s in o.buffers) o.buffers[s] = null; o.buffers = {}; }
      delete cache[k];
    }
  }
  const tactil = (() => { try { return window.matchMedia('(pointer: coarse)').matches; } catch (e) { return false; } })();
  function aMono(buf) {
    if (!tactil || buf.numberOfChannels < 2) return buf;
    const m = ctxAudio().createBuffer(1, buf.length, buf.sampleRate), a = buf.getChannelData(0), b = buf.getChannelData(1), o = m.getChannelData(0);
    for (let i = 0; i < o.length; i++) o[i] = (a[i] + b[i]) * 0.5;
    return m;
  }
  async function cargar(m, prog) {
    if (cache[m.id]) return cache[m.id];
    liberarOtras(m.id);
    const capas = await (await fetch(m.carpeta + 'capas.json')).json();
    const buffers = {}; let n = 0;
    await Promise.all(capas.stems.map(async s => {
      const ab = await (await fetch(m.carpeta + s + '.mp3')).arrayBuffer();
      buffers[s] = aMono(await new Promise((ok, no) => ctxAudio().decodeAudioData(ab, ok, no)));
      prog(++n / capas.stems.length);
    }));
    return (cache[m.id] = { capas, buffers });
  }

  let sel = { m: null, nv: 'facil', parte: 'all', modo: 'tocar', d: null };
  function ops(cont, lista, actual, cb) {
    cont.innerHTML = '';
    lista.forEach(([k, n]) => { const b = document.createElement('div'); b.className = 'yb-op' + (String(k) === String(actual) ? ' sel' : ''); b.textContent = n; b.onclick = () => cb(k); cont.appendChild(b); });
  }
  function aplicarInstr() {
    let id = (datos.instr && datos.instr[claveCapas()]) || (claveCapas() === 'bajo' ? 'bajo' : 'piano');
    if (id === 'bajo' && claveCapas() !== 'bajo') id = 'piano';
    try { if (typeof currentInstrument !== 'undefined') { if (!tocoInstr) { guardadoInstr = currentInstrument; tocoInstr = true; } currentInstrument = id; } } catch (e) {}
    pedirAlYamaha(id);
  }
  function pedirAlYamaha(id) {
    try {
      if (typeof soundOutputMode !== 'undefined' && soundOutputMode === 'midi' && typeof midiOutputRef !== 'undefined' && midiOutputRef && PROGRAMA_YAMAHA[id] !== undefined) {
        midiOutputRef.send([0xC0, PROGRAMA_YAMAHA[id]]);
        showBigToast('🎹 Le pedí al teclado el sonido: ' + (instrsDisp().concat([['bajo', 'Bajo']]).find(x => x[0] === id) || [0, id])[1]);
      }
    } catch (e) {}
  }
  function dibujarOct() { const o = $('yb-f-oct'); if (o) o.style.display = (currentInstrument === 'bajo') ? 'flex' : 'none'; }
  function moverOct(d) { datos.bajoOct3 = Math.max(-36, Math.min(12, (datos.bajoOct3 == null ? -12 : datos.bajoOct3) + d)); datos.bajoOct2 = datos.bajoOct3; guardar(); try { showBigToast('Bajo: ' + (datos.bajoOct3 / 12) + ' octavas respecto de la tecla'); } catch (e) {} }
  function idInstr() { let id = (datos.instr && datos.instr[claveCapas()]) || (claveCapas() === 'bajo' ? 'bajo' : 'piano'); if (id === 'bajo' && claveCapas() !== 'bajo') id = 'piano'; return id; }
  function dibujarInstr() {
    dibujarOct();
    let id = (datos.instr && datos.instr[claveCapas()]) || (claveCapas() === 'bajo' ? 'bajo' : 'piano');
    if (id === 'bajo' && claveCapas() !== 'bajo') id = 'piano';
    [$('yb-f-instr'), $('yb-op-instr')].forEach(c => { if (c) ops(c, instrsDisp(), id, k => { datos.instr[claveCapas()] = k; guardar(); aplicarInstr(); dibujarInstr(); }); });
  }
  let tocoInstr = false, guardadoInstr = 'piano';
  function dibujarCapas() {
    const c = $('yb-op-capas'); c.innerHTML = '';
    CAPAS.forEach(([k, n]) => { const b = document.createElement('div'); b.className = 'yb-op' + (datos.capas[k] ? ' sel' : ''); b.textContent = n;
      b.onclick = () => alternarCapa(k, () => { $('yb-prep-s').textContent = sel.m.estilo + ' · ' + nombreCapas(); dibujarOps(); });
      c.appendChild(b); });
  }
  function dibujarOps() {
    dibujarCapas();
    dibujarInstr();
    ops($('yb-op-nivel'), nivelesVis(), sel.nv, k => { sel.nv = k; dibujarOps(); });
    const partes = [['all', 'Canción completa']]; if (sel.d) sel.d.capas.partes.forEach((p, i) => partes.push([i, p.n]));
    ops($('yb-op-parte'), partes, sel.parte, k => { sel.parte = k; dibujarOps(); });
    ops($('yb-op-modo'), [['tocar', 'Tocar con la banda'], ['practica', 'Practicar sin música']], sel.modo, k => { sel.modo = k; dibujarOps(); });
  }
  async function prep(m) {
    sel.m = m; sel.d = cache[m.id] || null; pantalla('yb-prep');
    $('yb-prep-t').textContent = m.titulo; $('yb-prep-s').textContent = m.estilo + ' · ' + nombreCapas();
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
    const fin = Math.min(c.duracion, c.fin || c.duracion); // donde termina de verdad la música (sin la cola de silencio)
    let a = 0, b = fin;
    if (sel.parte !== 'all') { const p = c.partes[sel.parte]; a = p.a; b = Math.min(p.b, fin); }
    const tr = lista => lista.filter(n => n[0] >= a - 0.01 && n[0] < b).map(n => [n[2], n[0] - a, n[1]]);
    const cp = datos.capas, solo = !cp.melodia && !cp.acordes; // solo bajo: va en la mano derecha; con otras capas, el bajo va a la izquierda
    let rh = [], lh = [];
    if (cp.bajo) (solo ? rh : lh).push(...tr(c.capas.bajo[nv] || c.capas.bajo.facil));
    if (cp.melodia) rh.push(...tr(c.capas.melodia[nv] || c.capas.melodia.facil));
    if (cp.acordes) {
      // con las dos manos a la vez, la izquierda va un escalón más liviana (Fácil -> Muy fácil, Medio -> Fácil)
      const aliviar = cp.melodia && { facil: 'muyfacil', medio: 'facil' }[nv];
      lh.push(...tr(c.capas.acordes[aliviar || nv] || c.capas.acordes[nv] || c.capas.acordes.facil));
    }
    rh.sort((p, q) => p[1] - q[1]); lh.sort((p, q) => p[1] - q[1]);
    const mute = [];
    if (cp.bajo) mute.push('bajo'); if (cp.acordes) mute.push('guitarra'); if (cp.melodia || cp.acordes) mute.push('teclado');
    const s = multiHandSong(sel.m.titulo + ' (' + nombreCapas() + ')', 'YOUR BAND', nv, rh, lh, [], [], { bpm: 70, timeSig: { num: 4, den: 4 } });
    s.duration = Math.max(s.duration, (b - a) + 1.2);
    s.__yb = { a, modo: sel.modo, rol: claveCapas(), mute, tieneR: rh.length > 0, tieneL: lh.length > 0, nv, mision: sel.m.id, parte: sel.parte, datos: sel.d };
    return s;
  }
  async function empezar() {
    ctxAudio();
    if (typeof audioReady !== 'undefined' && !audioReady) { try { await startAudio(); } catch (e) {} }
    const s = armarCancion();
    // menos retardo del sonido que tocás: Tone programa cada nota 0.1 s adelante por defecto
    try { if (window.Tone && Tone.context) Tone.context.lookAhead = 0.015; } catch (e) {}
    cerrar(); pop.classList.remove('on');
    aplicarInstr(); aplicarVolYo();
    setHandsMode(s.__yb.tieneR && s.__yb.tieneL ? 'two' : s.__yb.tieneL ? 'left' : 'one', null);
    loadSong(s);
    flot.classList.add('on');
    $('yb-f-guial').style.display = s.__yb.modo === 'tocar' ? '' : 'none';
    $('yb-f-guia').checked = false;
    finVisto = false;
    startPlayWithCountdown();
  }
  const abrirPop = ev => { dibujarInstr(); const b = ev.currentTarget.getBoundingClientRect(); pop.style.top = (b.bottom + 4) + 'px'; pop.style.right = '8px'; pop.classList.toggle('on'); };
  $('yb-f-abrir').onclick = abrirPop; btn2.onclick = abrirPop;
  $('yb-f-grave').onclick = () => moverOct(-12); $('yb-f-agudo').onclick = () => moverOct(12);
  $('yb-f-menu').onclick = () => { pop.classList.remove('on'); try { pauseSong(); } catch (e) {} pararBanda(); if (datos.rol) menu(); else historia(0); };
  $('yb-f-sync').value = datos.sync; $('yb-f-syncv').textContent = datos.sync + ' ms';
  $('yb-f-sync').oninput = () => { datos.sync = +$('yb-f-sync').value; $('yb-f-syncv').textContent = datos.sync + ' ms'; guardar(); };
  const factorBanda = () => ((activeSong && activeSong.__yb && activeSong.__yb.datos.capas.ganancia) || 1);
  const aplicarVolBanda = () => { if (volNodo) volNodo.gain.value = $('yb-t-banda').value / 100 * factorBanda(); };
  // Volumen de lo que tocás: se le suma/resta a cada instrumento de Piano Hero (guarda el volumen original de cada uno)
  let ultVolYo = null, ultPianoYo = null;
  const aplicarVolYo = () => {
    try {
      const g = $('yb-t-yo').value / 80, db = g <= 0 ? -60 : 20 * Math.log10(g);
      [piano, saxSynth, trumpetSynth, vibraphoneSynth, organSynth, guitarSynth, violinSynth, fluteSynth, celloSynth].forEach(o => {
        if (o && o.volume && typeof o.volume.value === 'number') { if (o.__ybBase === undefined) o.__ybBase = o.volume.value; o.volume.value = o.__ybBase + db; }
      });
      ultVolYo = $('yb-t-yo').value; ultPianoYo = piano;
    } catch (e) {}
  };
  // Ladrillos: cuánto más arriba caen (en centésimas de segundo); se recuerda por canción
  window.__ybAtraso = 0.2; let ultMisionLad = null;
  const verLad = v => { const e = $('yb-t-lad-v'); if (e) e.textContent = (v / 100).toFixed(2); const p = $('yb-p-lad'); if (p) p.value = v; };
  $('yb-t-lad').oninput = () => { const v = +$('yb-t-lad').value; window.__ybAtraso = v / 100; verLad(v); const S = activeSong && activeSong.__yb; if (S) { datos.ladr = datos.ladr || {}; datos.ladr[S.mision] = v / 100; guardar(); } };
  $('yb-t-banda').oninput = aplicarVolBanda; $('yb-t-yo').oninput = aplicarVolYo;

  // ---------- La banda de fondo, pegada al reloj de Piano Hero ----------
  let volNodo = null, fuentes = [], corriendo = false, iniCtx = 0, iniPos = 0, finVisto = false;
  function pararBanda() { fuentes.forEach(f => { try { f.stop(); } catch (e) {} }); fuentes = []; corriendo = false; }
  function arrancarBanda(p) {
    pararBanda();
    const S = activeSong.__yb, ac = ctxAudio(), dur = S.datos.capas.duracion;
    if (p >= dur) return;
    if (!volNodo) {
      // un limitador suave después del volumen, para que la suma de las pistas no sature
      const lim = ac.createDynamicsCompressor(); lim.threshold.value = -8; lim.knee.value = 6; lim.ratio.value = 12; lim.attack.value = 0.003; lim.release.value = 0.15;
      volNodo = ac.createGain(); volNodo.connect(lim); lim.connect(ac.destination);
    }
    aplicarVolBanda();
    const ya = ac.currentTime + 0.05, espera = p < 0 ? (-p) / (speed || 1) : 0;
    S.datos.capas.stems.forEach(st => {
      const src = ac.createBufferSource(); src.buffer = S.datos.buffers[st]; src.playbackRate.value = speed || 1;
      const g = ac.createGain(); const mio = S.mute.indexOf(st) >= 0;
      g.gain.value = mio ? ($('yb-f-guia').checked ? 0.4 : 0) : 1;
      src.connect(g); g.connect(volNodo); src._g = g; src._mio = mio;
      try { src.start(ya + espera, Math.max(0, p)); } catch (e) {}
      fuentes.push(src);
    });
    iniCtx = ya; iniPos = p; corriendo = true;
  }
  $('yb-f-guia').onchange = () => fuentes.forEach(s => { if (s._mio) s._g.gain.value = $('yb-f-guia').checked ? 0.4 : 0; });

  // Teclado más alto mientras tocás YOUR BAND (unos 3 cm más): con pocas octavas en pantalla las teclas quedaban chicas.
  let tecladoCrecido = false, altoObjetivo = 0;
  // cuánto crece (px): bastante, porque en la tablet sobra pantalla
  const EXTRA_TECLADO = () => (tactil ? 130 : 150);
  function crecerTeclado(si) {
    const kr = document.getElementById('normalKeysRegion'); if (!kr) return;
    if (si && !tecladoCrecido) {
      kr.style.removeProperty('height'); kr.style.removeProperty('min-height');
      const base = parseFloat(getComputedStyle(kr).height) || 164;
      // sin pantalla completa hay menos alto: el teclado solo puede crecer hasta la mitad del espacio que comparte con la cascada
      let tope = Infinity;
      try {
        const p = kr.parentElement, cv = document.getElementById('cascade') || document.querySelector('canvas'); let otros = 0;
        for (const ch of p.children) { if (ch === kr || (cv && ch.contains(cv))) continue; const cs = getComputedStyle(ch); if (cs.display === 'none' || cs.position === 'absolute' || cs.position === 'fixed') continue; otros += ch.getBoundingClientRect().height; }
        const disponible = p.clientHeight - otros; if (disponible > 200) tope = Math.floor(disponible * 0.5);
      } catch (e) {}
      altoObjetivo = Math.max(base, Math.min(base + EXTRA_TECLADO(), tope));
      kr.style.setProperty('height', altoObjetivo + 'px', 'important'); kr.style.setProperty('min-height', altoObjetivo + 'px', 'important');
      tecladoCrecido = true;
    } else if (!si && tecladoCrecido) { kr.style.removeProperty('height'); kr.style.removeProperty('min-height'); tecladoCrecido = false; }
    else if (si && tecladoCrecido && Math.abs(kr.getBoundingClientRect().height - altoObjetivo) > 8) { // algo lo achicó: se vuelve a pedir
      kr.style.setProperty('height', altoObjetivo + 'px', 'important'); kr.style.setProperty('min-height', altoObjetivo + 'px', 'important');
    }
  }
  const reCrecer = () => { if (tecladoCrecido) { tecladoCrecido = false; crecerTeclado(true); } };
  window.addEventListener('resize', reCrecer);
  window.addEventListener('orientationchange', () => setTimeout(reCrecer, 400));
  // por las dudas, se revisa cada segundo mientras hay una canción de YOUR BAND (no depende del dibujo de pantalla)
  setInterval(() => { try { crecerTeclado(!!(activeSong && activeSong.__yb)); } catch (e) {} }, 1000);
  let ultResync = 0;
  function bucle() {
    try {
      const S = activeSong && activeSong.__yb;
      crecerTeclado(!!S); flot.classList.toggle('on', !!S); btn2.style.display = S ? '' : 'none'; vols.style.display = S ? 'inline-flex' : 'none';
      // el botón BANDA del encabezado se ve solo si el encabezado está visible; si no, queda el de la botonera del teclado (junto a Modo Espera)
      if (!S && tocoInstr) { try { currentInstrument = guardadoInstr; if (soundOutputMode === 'midi' && midiOutputRef) midiOutputRef.send([0xC0, 0]); } catch (e) {} tocoInstr = false; }
      if (S) {
        if (ultMisionLad !== S.mision) { ultMisionLad = S.mision; const v = Math.round((datos.ladr && datos.ladr[S.mision] != null ? datos.ladr[S.mision] : 0.2) * 100); $('yb-t-lad').value = v; window.__ybAtraso = v / 100; verLad(v); }
        if ($('yb-t-yo').value !== ultVolYo || piano !== ultPianoYo) aplicarVolYo(); // reaplica si cambió el slider o se cargó el piano real
        const querida = S.modo === 'tocar' && isPlaying && !inCountdown && !isFrozenWaiting;
        if (querida) {
          const p = playhead - 1.0 - datos.sync / 1000 + S.a;
          if (!corriendo) arrancarBanda(p);
          else {
            const real = iniPos + (AC.currentTime - iniCtx) * (speed || 1);
            if (Math.abs(real - p) > (celu ? 0.4 : 0.15) && (!celu || performance.now() - ultResync > 2000)) { ultResync = performance.now(); arrancarBanda(p); }
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
      const k = claveEst(S.mision, S.rol, S.nv); // S.rol = combinación de capas
      if ((datos.estrellas[k] || 0) < est) { datos.estrellas[k] = est; guardar(); }
    }
    try { showBigToast('🎸 ' + '★'.repeat(est) + '☆'.repeat(3 - est) + (waitMode && pct >= 0.75 ? '  (sin Modo Espera se ganan las 3)' : '')); } catch (e) {}
  }
  requestAnimationFrame(bucle);
  window.__ybEstado = () => ({ corriendo, fuentes: fuentes.length, ctx: AC && AC.state, pos: AC ? +(iniPos + (AC.currentTime - iniCtx) * (speed || 1)).toFixed(2) : null });

  // ---------- Bajo = la guitarra de Piano Hero, más grave ----------
  // Se pide la misma nota pero 'oct' octavas más abajo (o arriba), así la guitarra suena como un bajo.
  // Bajo = el piano de Piano Hero, en las octavas graves, sin ningún efecto (suena como bajo y no satura).
  // Pide la misma nota 'oct' octavas más abajo (por defecto 12 semitonos = una octava).
  function bajoGuitarra() {
    const oct = () => (datos.bajoOct3 == null ? -12 : datos.bajoOct3);
    const mover = name => { try { return Tone.Frequency(name).transpose(oct()).toNote(); } catch (e) { return name; } };
    const v = () => (typeof piano !== 'undefined' ? piano : null);
    return {
      triggerAttack(name, ...r) { const p = v(); if (p) p.triggerAttack(mover(name), ...r); },
      triggerRelease(name, ...r) { const p = v(); if (p) p.triggerRelease(mover(name), ...r); },
      triggerAttackRelease(name, ...r) { const p = v(); if (p) p.triggerAttackRelease(mover(name), ...r); },
      releaseAll() { const p = v(); if (p && p.releaseAll) p.releaseAll(); }
    };
  }
  const vozBajo = bajoGuitarra();
  try { if (typeof INSTRUMENT_VOICES !== 'undefined') INSTRUMENT_VOICES.bajo = () => vozBajo; } catch (e) {}
  // guitarra eléctrica: en la compu usa la guitarra de Piano Hero; en el Yamaha, la guitarra eléctrica del teclado
  const vozGuitE = { triggerAttack(...a) { if (guitarSynth) guitarSynth.triggerAttack(...a); }, triggerRelease(...a) { if (guitarSynth) guitarSynth.triggerRelease(...a); }, triggerAttackRelease(...a) { if (guitarSynth) guitarSynth.triggerAttackRelease(...a); }, releaseAll() { if (guitarSynth) guitarSynth.releaseAll(); } };
  try { if (typeof INSTRUMENT_VOICES !== 'undefined') INSTRUMENT_VOICES.guitarraE = () => vozGuitE; } catch (e) {}

  // ---------- La lista de canciones de YOUR BAND, en el panel derecho de Piano Hero ----------
  let enPanel = false;
  const tabBtn = document.getElementById('tab-yourband');
  function fmtDur(s) { return Math.floor(s / 60) + ':' + String(Math.round(s % 60)).padStart(2, '0'); }
  function chips(cont, lista, esSel, cb) {
    const d = document.createElement('div'); d.className = 'yb-ops'; d.style.cssText = 'justify-content:flex-start;margin-bottom:6px';
    lista.forEach(([k, n]) => { const b = document.createElement('div'); b.className = 'yb-op' + (esSel(k) ? ' sel' : ''); b.textContent = n; b.style.cssText = (tactil ? 'padding:9px 13px;font-size:14px' : 'padding:5px 10px;font-size:11px') + ';touch-action:manipulation'; b.onclick = ev => { ev.stopPropagation(); ev.preventDefault(); setTimeout(() => cb(k), 0); }; d.appendChild(b); });
    cont.appendChild(d);
  }
  let vistaPanel = 'canciones'; // el panel tiene dos vistas: las canciones o las opciones (una por vez, así no hay que scrollear)
  function renderMiLista() {
    const list = document.getElementById('songList'); if (!list) return;
    list.innerHTML = '';
    // Cabecera FIJA (sticky): resumen + botones siempre a la vista; las opciones se abren y cierran. Las canciones scrollean debajo.
    const cab = document.createElement('div');
    cab.style.cssText = 'padding:4px 2px 8px;font-size:' + (tactil ? '14px' : '12px') + ';color:var(--muted)';
    const nivTxt = (nivelesVis().find(x => x[0] === sel.nv) || [0, sel.nv])[1];
    const instrTxt = (instrsDisp().concat([['bajo', 'Bajo']]).find(x => x[0] === idInstr()) || [0, idInstr()])[1];
    cab.innerHTML = '<div style="margin-bottom:4px"><b style="color:var(--text)">🎸 ' + esc(datos.banda) + '</b> · nivel ' + nivelJugador() + ' · <span class="yb-est">★</span> ' + totalEstrellas() + '</div>' +
      '<div style="margin-bottom:6px;color:var(--text)">' + esc(nombreCapas()) + ' · ' + esc(nivTxt) + ' · ' + (sel.modo === 'tocar' ? 'con la banda' : 'sin música') + '<br><span style="color:var(--muted)">Sonido: ' + esc(instrTxt) + '</span></div>';
    list.appendChild(cab);
    const barra = document.createElement('div'); barra.style.cssText = 'display:flex;gap:6px;flex-wrap:wrap';
    const mini = (t, f) => { const b = document.createElement('button'); b.className = 'btn'; b.style.cssText = (tactil ? 'font-size:14px;padding:10px 13px' : 'font-size:11px;padding:6px 9px') + ';touch-action:manipulation'; b.textContent = t; b.onclick = ev => { ev.stopPropagation(); ev.preventDefault(); setTimeout(f, 0); }; barra.appendChild(b); return b; };
    // el bajo se puede bajar o subir una octava: botones siempre a la vista cuando el sonido es el bajo
    if (idInstr() === 'bajo') { mini('⬇ Bajo más grave', () => moverOct(-12)); mini('⬆ Bajo más agudo', () => moverOct(12)); }
    cab.appendChild(barra);
    const pest = document.createElement('div'); pest.style.cssText = 'display:flex;gap:6px;margin-top:8px';
    [['canciones', '🎵 Canciones'], ['opciones', '⚙ Opciones']].forEach(([k, t]) => {
      const b = document.createElement('button'); b.className = 'btn'; b.textContent = t;
      const sel_ = vistaPanel === k;
      b.style.cssText = 'flex:1;touch-action:manipulation;font-weight:800;border-radius:12px;border:2px solid #ffb454;' + (sel_ ? 'background:#ffb454;color:#241a00;' : 'background:#272340;color:#f2efff;') + (tactil ? 'font-size:18px;padding:14px 8px' : 'font-size:13px;padding:9px');
      b.onclick = ev => { ev.stopPropagation(); ev.preventDefault(); setTimeout(() => { vistaPanel = k; renderMiLista(); }, 0); };
      pest.appendChild(b);
    });
    // (sin pestañas: opciones y canciones van juntas en una sola lista que se desliza, como al principio)
    // volúmenes siempre a la vista, arriba del panel
    { const vp = document.createElement('div'); vp.style.cssText = 'display:flex;gap:10px;flex-wrap:wrap;margin-top:8px;color:var(--text);font-size:' + (tactil ? '15px' : '12px');
      const mk = (txt, idTool, idLocal, mx) => { const l = document.createElement('label'); l.style.cssText = 'display:flex;align-items:center;gap:6px;flex:1;min-width:140px'; l.textContent = txt;
        const r = document.createElement('input'); r.type = 'range'; r.min = 0; r.max = mx || 100; r.id = idLocal; r.style.cssText = 'flex:1;min-width:70px'; const t = document.getElementById(idTool); r.value = t ? t.value : 80;
        r.oninput = () => { if (t) { t.value = r.value; t.dispatchEvent(new Event('input')); } }; l.appendChild(r); vp.appendChild(l); };
      mk('🥁 Banda', 'yb-t-banda', 'yb-p-banda'); mk('🎹 Yo', 'yb-t-yo', 'yb-p-yo'); mk('🧱 Ladrillos', 'yb-t-lad', 'yb-p-lad', 60); cab.appendChild(vp); }
    if (true) {
      const op = document.createElement('div'); op.style.cssText = 'margin-top:4px;padding:0 2px;font-size:' + (tactil ? '14px' : '12px') + ';color:var(--muted)';
      list.appendChild(op);
      const volverArriba_ = null; const volverArriba = document.createElement('button'); volverArriba.className = 'btn'; volverArriba.textContent = '🎵 VER LAS CANCIONES';
      volverArriba.style.cssText = 'width:100%;margin:6px 0 10px;touch-action:manipulation;font-weight:800;border-radius:12px;background:#ffb454;color:#241a00;border:0;' + (tactil ? 'font-size:18px;padding:15px' : 'font-size:13px;padding:10px');
      volverArriba.onclick = ev => { ev.stopPropagation(); ev.preventDefault(); setTimeout(() => { vistaPanel = 'canciones'; renderMiLista(); }, 0); };
      // (sin botón de volver: ya no hay pestañas)
      const lbl = t => { const d = document.createElement('div'); d.textContent = t; d.style.margin = '4px 0'; op.appendChild(d); };
      lbl('Qué tocar');
      chips(op, CAPAS, k => !!datos.capas[k], k => alternarCapa(k, () => renderMiLista()));
      lbl('Dificultad');
      chips(op, nivelesVis(), k => sel.nv === k, k => { sel.nv = k; renderMiLista(); });
      lbl('Sonido de lo que tocás (el del bajo viene elegido; podés cambiarlo)');
      { const s = document.createElement('select'); s.style.cssText = 'width:100%;margin-bottom:8px;border-radius:12px;border:2px solid #3b3660;background:#1d1a2e;color:#f2efff;font-family:inherit;touch-action:manipulation;' + (tactil ? 'font-size:17px;padding:12px' : 'font-size:13px;padding:8px');
        instrsDisp().forEach(([k, n]) => { const o = document.createElement('option'); o.value = k; o.textContent = n; if (k === idInstr()) o.selected = true; s.appendChild(o); });
        s.onchange = () => { datos.instr[claveCapas()] = s.value; guardar(); if (activeSong && activeSong.__yb) aplicarInstr(); renderMiLista(); };
        op.appendChild(s); }
      lbl('Cómo');
      chips(op, [['tocar', 'Con la banda'], ['practica', 'Sin música']], k => sel.modo === k, k => { sel.modo = k; renderMiLista(); });
      if (!datos.muyfacil) { const q = document.createElement('div'); q.style.cssText = 'margin:6px 0'; const bq = document.createElement('button'); bq.className = 'btn'; bq.style.cssText = 'font-size:11px;padding:5px 8px'; bq.textContent = '🐣 ¿Te cuesta el Fácil? Pedí una versión más fácil'; bq.onclick = () => { datos.muyfacil = true; sel.nv = 'muyfacil'; guardar(); renderMiLista(); }; q.appendChild(bq); op.appendChild(q); }
      const fila = document.createElement('div'); fila.style.cssText = 'display:flex;gap:6px;margin:6px 0 2px';
      [['🎸 Misiones y estrellas', () => { try { pauseSong(); } catch (e) {} menu(); }], ['🖼️ Portada y nombre', () => window.ybAbrir()]].forEach(([t, f]) => { const b = document.createElement('button'); b.className = 'btn'; b.style.cssText = 'font-size:11px;padding:5px 8px'; b.textContent = t; b.onclick = f; fila.appendChild(b); });
      op.appendChild(fila);
    }
    const aviso = document.createElement('div'); aviso.style.cssText = 'font-size:11px;color:var(--muted);margin:2px 2px 8px'; aviso.textContent = 'Doble clic (o doble toque) en una canción para tocarla.'; list.appendChild(aviso);
    MISIONES.filter(m => !m.proximamente).forEach(m => {
      const bloq = m.nivelMin > nivelJugador();
      const card = document.createElement('div'); card.className = 'song-card' + (bloq ? ' locked' : ''); card.dataset.title = m.titulo;
      const est = datos.estrellas[claveEst(m.id, claveCapas(), sel.nv)] || 0;
      const dur = cache[m.id] ? fmtDur(cache[m.id].capas.duracion) : '';
      card.innerHTML = '<div class="t">🎵 ' + esc(m.titulo) + '</div><div class="a" style="display:flex;justify-content:space-between"><span>' + esc(m.estilo) + (dur ? ' · ' + dur : '') + (bloq ? ' · se abre en el nivel ' + m.nivelMin : '') + '</span><span class="yb-est">' + ('★'.repeat(est) + '☆'.repeat(3 - est)) + '</span></div>';
      if (!bloq) {
        card.onclick = () => { list.querySelectorAll('.song-card').forEach(c => c.classList.remove('active')); card.classList.add('active'); };
        card.ondblclick = () => tocarMision(m, card);
      }
      list.appendChild(card);
    });
  }
  async function tocarMision(m, card) {
    sel.m = m; sel.parte = 'all'; const a = card.querySelector('.a span');
    try {
      ctxAudio();
      sel.d = await cargar(m, p => { if (a) a.textContent = 'Cargando la banda… ' + Math.round(p * 100) + '%'; });
      await empezar();
    } catch (e) { if (a) a.textContent = 'No pude cargar: ' + e.message; }
  }
  function entrarPanel() {
    enPanel = true;
    const sc = document.getElementById('sidePanelContent'); if (sc) { sc.style.overflowX = 'hidden'; }
    const sl = document.getElementById('songList'); if (sl) sl.style.maxWidth = '100%'; // (no se toca el overflow del panel: dejaría de deslizar)
    document.querySelectorAll('.tabs-row .tab-btn, #mainTabsRow .tab-btn').forEach(b => b.classList.remove('active'));
    if (tabBtn) tabBtn.classList.add('active');
    ['levelFilterRow', 'sideImportBlock', 'bulkDeleteMultipistaBtn', 'bulkPromoteFacilBtn'].forEach(id => { const e = document.getElementById(id); if (e) e.style.display = 'none'; });
    // el "Rendimiento" (Aciertos / Errores) de Piano Hero ocupaba lugar arriba de las canciones: se esconde mientras estás en YOUR BAND
    { const sc0 = document.getElementById('sidePanelContent'); const h = sc0 && sc0.querySelector('h2'); const g = sc0 && sc0.querySelector('.stat-grid'); if (h) h.style.display = 'none'; if (g) g.style.display = 'none'; }
    renderMiLista();
  }
  // (el panel solo desborda para arriba y abajo: se le oculta el desborde de costado al contenedor que scrollea)
  const _toggleHeader = window.toggleHeaderCollapse;
  if (typeof _toggleHeader === 'function') window.toggleHeaderCollapse = function () {
    const estabanOcultos = (typeof miniToolbarCollapsed !== 'undefined') && miniToolbarCollapsed;
    const r = _toggleHeader.apply(this, arguments);
    try { if (activeSong && activeSong.__yb && estabanOcultos && !miniToolbarCollapsed) toggleMiniToolbarCollapse(document.getElementById('miniToolbarCollapseBtn')); } catch (e) {}
    return r;
  };
  const _switchTab = window.switchTab, _renderSongList = window.renderSongList;
  window.switchTab = function (tab) {
    const estabaEnPanel = enPanel;
    enPanel = false; if (tabBtn) tabBtn.classList.remove('active');
    { const sc0 = document.getElementById('sidePanelContent'); const h = sc0 && sc0.querySelector('h2'); const g = sc0 && sc0.querySelector('.stat-grid'); if (h) h.style.display = ''; if (g) g.style.display = ''; }
    { const sc = document.getElementById('sidePanelContent'); if (sc) { sc.style.overscrollBehavior = ''; sc.style.touchAction = ''; sc.style.overflowX = ''; }
      const sp = document.getElementById('sidePanel'); if (sp) { sp.style.touchAction = ''; sp.style.overflowX = ''; }
      const sl = document.getElementById('songList'); if (sl) { sl.style.overflowX = ''; sl.style.maxWidth = ''; } }
    if (tab === 'yourband') { const yaAdentro = estabaEnPanel; _switchTab('songs'); entrarPanel(); if (!yaAdentro) window.ybAbrir(); return; }
    return _switchTab(tab);
  };
  window.renderSongList = function () { if (enPanel) return renderMiLista(); return _renderSongList.apply(this, arguments); };

  // ---------- Entrada ----------
  window.ybAbrir = function () { try { pauseSong(); } catch (e) {} pop.classList.remove('on'); $('yb-in-nombre').value = datos.banda; pantalla('yb-nombre'); };
  if (location.hash === '#yourband') setTimeout(() => window.switchTab('yourband'), 300);
})();
