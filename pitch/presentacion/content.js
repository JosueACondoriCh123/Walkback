// Written with Claude Code (AI), directed and tested by team MARSXR
// Walkback · contenido de la presentación.
// JOSUÉ: este es el ÚNICO archivo que hay que tocar para cambiar una frase, un tiempo o una nota.
// - Cada lámina tiene "es" (defensa) y "en" (NASA). La tecla L cambia de idioma.
// - Lo que está entre llaves, como {km} o {total}, lo calcula la presentación con la misma cuenta que la app
//   (walkback-model.js). NO escribas esos números a mano: si cambia el terreno, cambian solos.
// - "dice" es lo que dice Daniel (sale en las notas, tecla N). "segundos" es lo que dura la lámina: suman 180.
// - Los números de FACTS no salen de la cuenta: cada uno dice de dónde sale. Si cambiás uno, cambiá su fuente.
// - Después de tocar este archivo:  node tools/check-content.js   (tiene que terminar en ALL PASSED).
window.WALKBACK_CONTENT = {
  lang: 'es',

  // Números que NO calcula el modelo. Fuente al lado. No agregar ninguno sin fuente.
  FACTS: {
    positions: 85,            // DATA.md, tabla de controles: posiciones de Curiosity
    slopeMeanDiff: '2,0°',    // DATA.md: diferencia MEDIA entre la pendiente del DEM y el sensor de inclinación (r = 0,77; máx. 21°)
    slopeMeanDiffEn: '2.0°',
    slopeR: '0,77',
    slopeREn: '0.77',
    slopeMax: '21°',
    demMeters: 1,             // DATA.md: HiRISE DEM (Mars Trek), 1 m
    photoCm: 25,              // DATA.md: foto HiRISE, 25 cm por píxel
    fps: 72,                  // CLAUDE.md: en la mesa, Quest 3, 14/11 (Marte a escala real: sin medir)
    tests: '424 de 424',      // CLAUDE.md, 15/11. OJO: poner la cuenta del commit del tag v1.0-submission si cambió
    testsEn: '424 of 424'
  },

  // Valores que llena el modelo al abrir (se listan acá solo para saber que existen):
  // {km} 1,8 km · {meters} 1812 · {walk} 43 min · {work} 2 h 12 min · {total} 2 h 55 min · {usedL} 181 · {usedPct} 36
  // {spareL} 285 · {tankL} 497 · {reserveL} 31 · {limit} 20 · {red8} 57,5 · {reach8} 8,5 · {farStopMin} 14,7
  // {redStops8} Campo Marte · {okStops8} Altadena, Valle de la Luna y Nevado Sajama · {short8L} 172 · {firstRedPct} 12,5
  // La lista completa la imprime  node tools/check-content.js  (línea "keys available"). Las arma placeholders.js.

  // Textos de la interfaz de la presentación (no de una lámina en particular).
  ui: {
    es: { estimate: 'ESTIMADO', habitat: 'HÁBITAT', clock: 'T reinicia el reloj · N notas · F pantalla completa · L idioma · R respaldo de la demo · P PDF',
          tank: 'TANQUE', covers: 'cubre {reach} min de caminata de vuelta', noRed: 'sin rojo en el mapa',
          redShare: '{red} % del terreno con vuelta a pie queda en rojo', redStops: '{stops} queda en el rojo',
          noWebgl: 'Vista 3D no disponible en este equipo: se muestra la foto real de HiRISE.',
          project: 'El proyecto', annexTag: 'Anexo · fuera de los 3 minutos', annexTitle: 'Si pregunta el jurado',
          annex: 'anexo', beforeClock: 'antes del reloj', clockArmed: 'el reloj arranca al pasar de lámina', clockOff: 'reloj parado (T)' },
    en: { estimate: 'ESTIMATE', habitat: 'HABITAT', clock: 'T restarts the clock · N notes · F full screen · L language · R demo backup · P PDF',
          tank: 'TANK', covers: 'covers {reach} min of walking back', noRed: 'no red on the map',
          redShare: '{red} % of the ground with a way back on foot turns red', redStops: '{stops} is in the red',
          noWebgl: '3D view not available on this machine: showing the real HiRISE photo.',
          project: 'The project', annexTag: 'Annex · outside the 3 minutes', annexTitle: 'If the jury asks',
          annex: 'annex', beforeClock: 'before the clock', clockArmed: 'the clock starts on the next slide', clockOff: 'clock stopped (T)' }
  },

  slides: [
    {
      // La NASA pide en las láminas: el nombre del equipo (está en tag), los nombres de los integrantes y el reto.
      // members: VACÍO = no se muestra. Los nombres los da Daniel; no inventar ninguno.
      //   Forma sugerida: 'Equipo MARSXR: Nombre Apellido · Nombre Apellido · …' (en: 'Team MARSXR: …').
      id: 'title', scene: 'globe', segundos: 0,
      es: { tag: 'NASA Space Apps 2026 · Santa Cruz, Bolivia · MARSXR', title: 'Walkback', sub: 'The safest way to go far.',
            challenge: 'Reto: Interplanetary Survival Guide: Martian Map',
            members: '',
            credit: 'Mosaico Viking MDIM 2.1 · NASA/JPL/USGS',
            dice: '(Antes de empezar. El reloj arranca solo al pasar a la lámina siguiente.)' },
      en: { tag: 'NASA Space Apps 2026 · Santa Cruz, Bolivia · MARSXR', title: 'Walkback', sub: 'The safest way to go far.',
            challenge: 'Challenge: Interplanetary Survival Guide: Martian Map',
            members: '',
            credit: 'Viking MDIM 2.1 mosaic · NASA/JPL/USGS', dice: '' }
    },
    {
      id: 'problem', scene: 'panorama', segundos: 20,
      es: { title: '¿Todavía puedo', titleAccent: 'volver?', lines: ['Cada paso gasta oxígeno.', 'Cada pendiente cambia la vuelta.'],
            credit: 'Foto real: Curiosity Mastcam · PIA26696 · NASA/JPL-Caltech/MSSS',
            dice: 'Imaginen que salen del hábitat en Marte. Cada paso gasta oxígeno, y cada pendiente cambia cuánto cuesta volver. Antes de seguir hacia la próxima muestra hay una sola pregunta que importa: ¿todavía puedo volver? Para responderla hicimos Walkback.' },
      en: { title: 'Can I still', titleAccent: 'walk back?', lines: ['Every step spends oxygen.', 'Every slope changes the way back.'],
            credit: 'Real photo: Curiosity Mastcam · PIA26696 · NASA/JPL-Caltech/MSSS', dice: '' }
    },
    {
      id: 'solution', scene: 'gale', pose: 'table', segundos: 30,
      es: { tag: 'Meta Quest 3 · realidad mixta', title: 'Un km² real de Gale.', titleAccent: 'Una caminata para decidir.',
            big: [['{km}', 'ida y vuelta'], ['{total}', 'caminata y trabajo'], ['{usedPct} %', 'del oxígeno']],
            estimate: 'ESTIMADO · plan B sobre el relieve · {limit}° de pendiente como límite',
            list: 'Cuatro sitios donde perforó Curiosity',
            legend: ['Ámbar: la ruta del plan', 'Rayado: pendiente de más de {limit}°', 'Verde: el hábitat'],
            credit: 'Relieve HiRISE 1 m: NASA Mars Trek · foto HiRISE: USGS / NASA/JPL/University of Arizona',
            dice: 'Walkback no es un juego: es una herramienta para decidir una caminata. Este es un kilómetro cuadrado real del cráter Gale, con el relieve y la foto de HiRISE. Las cuatro paradas son sitios donde perforó Curiosity. Sobre ese relieve la app arma la ida y la vuelta y estima: un kilómetro ochocientos, dos horas cincuenta y cinco, treinta y seis por ciento del oxígeno. Son estimaciones, y la app lo dice en pantalla.' },
      en: { tag: 'Meta Quest 3 · mixed reality', title: 'One real km² of Gale.', titleAccent: 'One walk to decide.',
            big: [['{kmEn}', 'out and back'], ['{total}', 'walking and work'], ['{usedPct} %', 'of the oxygen']],
            estimate: 'ESTIMATE · plan B over the relief · {limit}° slope limit',
            list: 'Four sites where Curiosity drilled',
            legend: ['Amber: the plan\'s route', 'Hatched: slope over {limit}°', 'Green: the habitat'],
            credit: 'HiRISE 1 m relief: NASA Mars Trek · HiRISE photo: USGS / NASA/JPL/University of Arizona', dice: '' }
    },
    {
      id: 'verdict', scene: 'gale', pose: 'board', overlay: 'board', accent: 'safe', segundos: 10,
      es: { tag: 'El tablero de la app', title: 'Con el tanque lleno:', titleAccent: 'VIABLE.',
            credit: 'Tablero redibujado con los textos y la cuenta de la app · ESTIMADO',
            dice: 'Con el tanque lleno, el plan es viable. ¿Y si el traje falla y queda el ocho por ciento del oxígeno? Veámoslo en el visor.' },
      en: { tag: 'The app\'s board', title: 'With a full tank:', titleAccent: 'VIABLE.',
            credit: 'Board redrawn with the app\'s texts and the app\'s numbers · ESTIMATE', dice: '' }
    },
    {
      id: 'demo', scene: 'gale', pose: 'whatif', overlay: 'demo', segundos: 45,
      es: { title: 'DEMO', sub: 'Meta Quest 3 · en vivo', hint: 'R: respaldo animado · V: video local',
            acts: [['PLAN', 'El plan y su veredicto.'], ['WHAT IF', 'El tanque baja al 8 %: {red8} % del terreno con vuelta a pie queda en rojo.'], ['MISIÓN', 'El mismo terreno, a escala real, con el visor.']],
            legend: ['Rojo: desde ahí el oxígeno ya no alcanza para volver', 'Gris: sin vuelta a pie (pendiente de más de {limit}°)', 'Anillos: cada 5 min de caminata de vuelta'],
            credit: 'Respaldo: la regla de la app, portada a JavaScript y corriendo en esta lámina · ESTIMADO',
            dice: '0–15 s: Este es el plan: hábitat, cuatro sitios donde perforó Curiosity, y vuelta. Estimación: 1,8 km, 2 h 55 min en total, 36 % del oxígeno. VIABLE. · 15–30 s: ¿Y si falla el traje y queda el 8 %? NOT VIABLE. El rojo es desde dónde ya no se vuelve caminando. · 30–45 s: Y esto es salir: el mismo terreno a escala real. En el casco, el oxígeno y los minutos para volver.' },
      en: { title: 'DEMO', sub: 'Meta Quest 3', hint: '',
            acts: [['PLAN', 'The plan and its verdict.'], ['WHAT IF', 'The tank drops to 8 %: {red8En} % of the ground with a way back on foot turns red.'], ['MISSION', 'The same terrain at real scale, with the visor.']],
            legend: ['Red: from there the oxygen no longer covers the walk back', 'Gray: no way back on foot (slope over {limit}°)', 'Rings: every 5 min of walking back'],
            credit: 'The app\'s own rule, ported to JavaScript and running in this slide · ESTIMATE', dice: '' }
    },
    {
      id: 'evidence', scene: 'gale', pose: 'evidence', segundos: 35,
      es: { tag: 'Evidencia y límites', title: 'Datos que se pueden comprobar.', titleAccent: 'Estimaciones que se dicen.',
            big: [['{demMeters} m', 'relieve HiRISE'], ['{positions}', 'posiciones de Curiosity'], ['{slopeMeanDiff}', 'diferencia media de pendiente']],
            lines: ['El contraste con el sensor de inclinación de Curiosity respalda el terreno.', 'El oxígeno y el tiempo son estimaciones: falta validarlos.', 'El mapa no ve rocas de menos de unos 4 m ni la arena.'],
            legend: ['Celeste: las {positions} posiciones de Curiosity', 'Rayado: pendiente de más de {limit}°'],
            credit: 'DATA.md · r = {slopeR}, diferencia máxima {slopeMax} · prueba automática de la app: {tests}',
            dice: 'Todo esto se puede comprobar. El relieve es de HiRISE, a un metro, y lo contrastamos con el sensor de inclinación del propio Curiosity en ochenta y cinco posiciones: la diferencia media es de dos grados. Eso respalda el terreno; no valida el oxígeno. El oxígeno y el tiempo son estimaciones, y la app lo dice en pantalla. El mapa tampoco ve las rocas chicas ni la arena. Preferimos decir qué es dato y qué es estimación.' },
      en: { tag: 'Evidence and limits', title: 'Data you can check.', titleAccent: 'Estimates that say so.',
            big: [['{demMeters} m', 'HiRISE relief'], ['{positions}', 'Curiosity positions'], ['{slopeMeanDiffEn}', 'mean slope difference']],
            lines: ['The check against Curiosity\'s own tilt sensor supports the terrain.', 'Oxygen and time are estimates: not validated yet.', 'The map does not see rocks under about 4 m, or sand.'],
            legend: ['Light blue: Curiosity\'s {positions} positions', 'Hatched: slope over {limit}°'],
            credit: 'DATA.md · r = {slopeREn}, largest difference {slopeMax} · the app\'s automatic test: {testsEn}', dice: '' }
    },
    {
      // El cierre es el mismo Gale, desde Campo Marte mirando al hábitat por la ruta (AUDIT-R20-F4).
      // El astronauta 3D de la NASA salió de la presentación: su textura lleva la insignia de la NASA (regla: sin logos).
      id: 'closing', scene: 'gale', pose: 'closing', overlay: 'qr', segundos: 40,
      es: { tag: 'MARSXR · NASA Space Apps 2026', title: 'Explorar más,', titleAccent: 'con el camino de vuelta a la vista.',
            brand: 'Walkback', sub: 'The safest way to go far.', qr: 'github.com/kendal-dev/Walkback',
            next: 'Lo que sigue (no está hecho): el modelo definitivo de caminata y oxígeno; leer la telemetría de un traje en lugar de estimarla. Hoy Walkback no lee telemetría de ningún traje.',
            credit: 'Gale, de Campo Marte al hábitat · relieve HiRISE 1 m: NASA Mars Trek · foto HiRISE: USGS / NASA/JPL/University of Arizona · ruta del plan: ESTIMADO · proyecto hecho con ayuda de IA, declarado en AI_USAGE.md',
            dice: 'Walkback sirve para comparar salidas y saber, en cada parada, si seguir compromete la vuelta. Hoy el Quest 3 hace de visor del casco. Lo que sigue es el modelo definitivo de caminata y oxígeno, y leer la telemetría de un traje, como plantea el desafío NASA SUITS, en lugar de estimarla. Somos MARSXR, de Santa Cruz. Queremos que cada decisión de explorar mantenga a la vista el camino de vuelta. The safest way to go far. Gracias. (Últimos 5 s: el QR en pantalla, sin hablar.)' },
      en: { tag: 'MARSXR · NASA Space Apps 2026', title: 'Go farther,', titleAccent: 'with the way back in sight.',
            brand: 'Walkback', sub: 'The safest way to go far.', qr: 'github.com/kendal-dev/Walkback',
            credit: 'Gale, from Campo Marte to the habitat · HiRISE 1 m relief: NASA Mars Trek · HiRISE photo: USGS / NASA/JPL/University of Arizona · the plan\'s route: ESTIMATE · project built with AI help, declared in AI_USAGE.md',
            next: 'Next (not done): the final walking and oxygen model; reading suit telemetry instead of estimating it. Walkback reads no suit telemetry today.', dice: '' }
    }
  ],

  // Respaldo (tecla A): no cuenta en los 3 minutos.
  backup: {
    es: { realtime: '¿Datos en tiempo real? Una cosa: al abrir, la app consulta NASA DONKI por los eventos de partículas solares de la semana y muestra la hora; la línea dice (live) o (stored in the app). El clima de Marte es el último reporte de Curiosity, con su fecha. Nada de eso entra en la cuenta del oxígeno.',
          fps: '{fps} fps en la mesa (Quest 3, 14/11). Marte a escala real: sin medir.' },
    en: { realtime: 'Real-time data? One thing: at start the app asks NASA DONKI for the week\'s solar particle events and shows the time; the line says (live) or (stored in the app). Mars weather is Curiosity\'s latest report, with its date. None of it feeds the oxygen estimate.',
          fps: '{fps} fps at the table (Quest 3, 14 Nov). Mars at real scale: not measured.' }
  }
};
