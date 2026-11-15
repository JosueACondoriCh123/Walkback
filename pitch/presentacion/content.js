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
    fps: 72,                  // log de la app en el Quest 3, 15/11 03:28-03:54: mesa, Marte a escala real, sala y foto 360°
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
            credit: 'Mosaico Viking MDIM 2.1 · NASA/JPL/USGS · color realzado',
            dice: '(Antes de empezar. El reloj arranca solo al pasar a la lámina siguiente.)' },
      en: { tag: 'NASA Space Apps 2026 · Santa Cruz, Bolivia · MARSXR', title: 'Walkback', sub: 'The safest way to go far.',
            challenge: 'Challenge: Interplanetary Survival Guide: Martian Map',
            members: '',
            credit: 'Viking MDIM 2.1 mosaic · NASA/JPL/USGS · colour enhanced', dice: '' }
    },
    {
      id: 'problem', scene: 'panorama', segundos: 20,
      es: { title: 'Ir es fácil.', titleAccent: 'Lo difícil es volver.', lines: [],
            credit: 'Foto real: Curiosity Mastcam · PIA26696 · NASA/JPL-Caltech/MSSS',
            dice: 'Ir es fácil. Lo difícil es volver. (pausa) En Marte, si una caminata sale mal, nadie va a venir a buscarte. Cada paso gasta oxígeno y cada pendiente encarece la vuelta. Hicimos Walkback para saber, en cada parada, si la vuelta sigue asegurada.' },
      en: { title: 'Going is easy.', titleAccent: 'Coming back is the hard part.', lines: [],
            credit: 'Real photo: Curiosity Mastcam · PIA26696 · NASA/JPL-Caltech/MSSS', dice: '' }
    },
    {
      id: 'solution', scene: 'gale', pose: 'table', segundos: 35,
      es: { tag: 'Un km² real de Gale · Meta Quest 3', title: 'Explorar no es ir lejos.', titleAccent: 'Es saber hasta dónde.',
            big: [['{km}', 'ida y vuelta'], ['{total}', 'caminata y trabajo'], ['{usedPct} %', 'del oxígeno']],
            estimate: 'ESTIMADO',
            // Defensa: la leyenda se dice en voz alta ("lo rayado no se pisa"); la versión en inglés, que se lee, la muestra.
            legend: [],
            // Las dos fases de Walkback. La primera se enciende: esta lámina es la de planificar.
            phases: [['1', 'PLANIFICAR', 'en la mesa'], ['2', 'EXPLORAR', 'en el terreno, a escala real']],
            credit: 'Relieve HiRISE 1 m: NASA Mars Trek · foto HiRISE: USGS / NASA/JPL/University of Arizona',
            dice: 'Explorar no es ir lejos: es saber hasta dónde. Walkback tiene dos fases: planificar en la mesa y explorar en el terreno. Esto es un kilómetro cuadrado real del cráter Gale, con relieve y foto de HiRISE, y cuatro sitios donde perforó Curiosity. Lo rayado es pendiente de más de veinte grados: no se pisa. La app arma la ida y la vuelta: un kilómetro ochocientos, dos horas cincuenta y cinco, treinta y seis por ciento del oxígeno. Estimado, y la app lo dice.' },
      en: { tag: 'One real km² of Gale · Meta Quest 3', title: 'Exploring is not going far.', titleAccent: 'It is knowing how far.',
            big: [['{kmEn}', 'out and back'], ['{total}', 'walking and work'], ['{usedPct} %', 'of the oxygen']],
            estimate: 'ESTIMATE',
            legend: ['Amber: the plan\'s route', 'Hatched: slope over {limit}°', 'Green: the habitat'],
            phases: [['1', 'PLAN', 'at the table: route, oxygen, verdict'], ['2', 'EXPLORE', 'on the terrain, at real scale']],
            credit: 'HiRISE 1 m relief: NASA Mars Trek · HiRISE photo: USGS / NASA/JPL/University of Arizona', dice: '' }
    },
    {
      id: 'verdict', scene: 'gale', pose: 'board', overlay: 'board', accent: 'safe', segundos: 10,
      es: { tag: 'Fase 1 · planificar · el tablero de la app', title: 'Con el tanque lleno:', titleAccent: 'VIABLE.',
            credit: 'Tablero redibujado con los textos y la cuenta de la app · ESTIMADO',
            dice: 'Con el tanque lleno, el plan es viable. ¿Y si el traje falla y queda el ocho por ciento del oxígeno? Veámoslo en el visor.' },
      en: { tag: 'Phase 1 · plan · the app\'s board', title: 'With a full tank:', titleAccent: 'VIABLE.',
            credit: 'Board redrawn with the app\'s texts and the app\'s numbers · ESTIMATE', dice: '' }
    },
    {
      id: 'demo', scene: 'gale', pose: 'whatif', overlay: 'demo', segundos: 45,
      es: { title: 'DEMO', sub: 'Meta Quest 3 · en vivo', hint: 'R: respaldo animado · V: video local',
            acts: [['FASE 1 · PLAN', 'El plan y su veredicto.'], ['FASE 1 · WHAT IF', 'El tanque baja al 8 %: {red8} % del terreno con vuelta a pie queda en rojo.'], ['FASE 2 · EXPLORAR', 'El mismo terreno, a escala real, con el visor.']],
            legend: ['Rojo: desde ahí el oxígeno ya no alcanza para volver', 'Gris: sin vuelta a pie (pendiente de más de {limit}°)', 'Anillos: cada 5 min de caminata de vuelta'],
            credit: 'Respaldo: la regla de la app, portada a JavaScript y corriendo en esta lámina · ESTIMADO',
            dice: '0–15 s: Este es el plan: hábitat, cuatro sitios donde perforó Curiosity, y vuelta. Estimación: 1,8 km, 2 h 55 min en total, 36 % del oxígeno. VIABLE. · 15–30 s: ¿Y si falla el traje y queda el 8 %? NOT VIABLE. El rojo es desde dónde ya no se vuelve caminando. · 30–45 s: Y esto es salir: el mismo terreno a escala real. En el casco, el oxígeno y los minutos para volver.' },
      en: { title: 'DEMO', sub: 'Meta Quest 3', hint: '',
            acts: [['PHASE 1 · PLAN', 'The plan and its verdict.'], ['PHASE 1 · WHAT IF', 'The tank drops to 8 %: {red8En} % of the ground with a way back on foot turns red.'], ['PHASE 2 · EXPLORE', 'The same terrain at real scale, with the visor.']],
            legend: ['Red: from there the oxygen no longer covers the walk back', 'Gray: no way back on foot (slope over {limit}°)', 'Rings: every 5 min of walking back'],
            credit: 'The app\'s own rule, ported to JavaScript and running in this slide · ESTIMATE', dice: '' }
    },
    {
      id: 'evidence', scene: 'gale', pose: 'evidence', segundos: 30,
      es: { tag: 'Por qué creernos', title: 'Terreno comprobado', titleAccent: 'contra el propio Curiosity.',
            big: [['{demMeters} m', 'relieve HiRISE'], ['{positions}', 'posiciones de Curiosity'], ['{slopeMeanDiff}', 'diferencia media de pendiente']],
            // Defensa: menos texto; lo que sale de acá se dice en voz alta. La versión en inglés lo muestra todo.
            lines: [],
            legend: [],
            // De dónde sale cada dato. El tercer valor 'live' marca el único que la app consulta al abrir (NASA DONKI);
            // todo lo demás viaja guardado en la app. El clima no entra en la cuenta del oxígeno.
            sourcesTitle: 'De dónde salen los datos',
            sources: [['Relieve y foto · HiRISE', 'guardado'], ['{positions} posiciones · Curiosity', 'guardado'],
                      ['Clima · último reporte de Curiosity', 'guardado'], ['Clima espacial · NASA DONKI', 'se consulta al abrir', 'live']],
            sourcesNote: 'El clima no entra en la cuenta del oxígeno.',
            credit: 'DATA.md · r = {slopeR}, diferencia máxima {slopeMax} · prueba automática de la app: {tests}',
            dice: '¿Por qué creernos? El terreno no lo inventamos: el relieve es de HiRISE, a un metro, y lo comparamos con el sensor de inclinación del propio Curiosity en ochenta y cinco posiciones. La diferencia media es de dos grados. Todo sale de datos públicos de la NASA, y el clima espacial se consulta al abrir. El oxígeno y el tiempo son estimaciones, y la app lo dice en pantalla. (Rocas chicas y arena: solo si preguntan.)' },
      en: { tag: 'Why trust it', title: 'Terrain checked', titleAccent: 'against Curiosity itself.',
            big: [['{demMeters} m', 'HiRISE relief'], ['{positions}', 'Curiosity positions'], ['{slopeMeanDiffEn}', 'mean slope difference']],
            lines: ['Oxygen and time: estimates, and the app says so.', 'The map does not see rocks under about 4 m, or sand.'],
            legend: ['Light blue: Curiosity\'s {positions} positions', 'Hatched: slope over {limit}°'],
            sourcesTitle: 'Where the data comes from',
            sources: [['{demMeters} m relief and {photoCm} cm photo · HiRISE', 'stored'], ['{positions} Curiosity positions', 'stored'],
                      ['Four stops · Curiosity blog', 'stored'], ['Weather: Curiosity\'s latest report (REMS)', 'stored'],
                      ['Space weather · NASA DONKI', 'asked at start', 'live']],
            sourcesNote: 'Weather does not feed the oxygen estimate.',
            credit: 'DATA.md · r = {slopeREn}, largest difference {slopeMax} · the app\'s automatic test: {testsEn}', dice: '' }
    },
    {
      // El cierre es el mismo Gale, desde Campo Marte mirando al hábitat por la ruta (AUDIT-R20-F4), en los dos idiomas.
      // El astronauta 3D de la NASA salió de la presentación: su textura lleva la insignia de la NASA (regla: sin logos).
      // SOLO en la laptop de la defensa y SOLO en español: si existe img/local/astronaut-realistic.png (fuera del
      // repositorio, en .gitignore), la lámina suma esa ilustración hecha con IA, marcada "Ilustración hecha con IA",
      // con el visor de la app dibujado encima y el crédito creditLocal. Sin ese archivo queda Gale con el crédito normal.
      id: 'closing', scene: 'gale', pose: 'closing', overlay: 'qr', segundos: 40,
      es: { tag: 'MARSXR · NASA Space Apps 2026', title: 'Porque volver también', titleAccent: 'es parte de la misión.',
            brand: 'Walkback', sub: '', qr: 'github.com/kendal-dev/Walkback',
            // Hoy y el próximo paso. La fila de abajo NO está hecha: se dice así en la lámina y en voz alta.
            flow: [['HOY', 'funciona', 'Datos de la NASA + regla', 'Ruta, veredicto, vuelta', 'Visor: Quest 3'],
                   ['PRÓXIMO PASO', 'no está hecho', 'Telemetría del traje', 'Misma regla, sin estimar', 'Visor del casco']],
            // Defensa: esta frase se dice en voz alta; la fila de abajo del diagrama ya dice "no está hecho". En inglés se muestra.
            next: '',
            credit: 'Gale, de Campo Marte al hábitat · relieve HiRISE 1 m: NASA Mars Trek · foto HiRISE: USGS / NASA/JPL/University of Arizona · ruta del plan: ESTIMADO · proyecto hecho con ayuda de IA, declarado en AI_USAGE.md',
            creditLocal: 'Astronauta: ilustración hecha con IA, solo para esta defensa (no está en el repositorio) · ruta del plan: ESTIMADO · Gale: relieve HiRISE 1 m, NASA Mars Trek; foto HiRISE, USGS / NASA/JPL/University of Arizona · IA declarada en AI_USAGE.md',
            dice: 'Walkback convierte una pregunta de vida o muerte en un color: verde, seguís; rojo, volvés. El astronauta no va a llevar un Quest: el producto es el software, y el visor puede ser el del casco. El próximo paso es leer la telemetría del traje, el tipo de problema que plantea el desafío NASA SUITS, en lugar de estimarla. Somos MARSXR, de Santa Cruz. Porque volver también es parte de la misión. Gracias. (Últimos 5 s: el QR en pantalla, sin hablar.)' },
      en: { tag: 'MARSXR · NASA Space Apps 2026', title: 'Because coming back', titleAccent: 'is part of the mission.',
            brand: 'Walkback', sub: '', qr: 'github.com/kendal-dev/Walkback',
            credit: 'Gale, from Campo Marte to the habitat · HiRISE 1 m relief: NASA Mars Trek · HiRISE photo: USGS / NASA/JPL/University of Arizona · the plan\'s route: ESTIMATE · project built with AI help, declared in AI_USAGE.md',
            flow: [['TODAY', 'works', 'NASA data + the rule', 'Route, verdict, way back', 'Visor: Quest 3'],
                   ['NEXT STEP', 'not done', 'Suit telemetry', 'Same rule, no estimate', 'Helmet visor']],
            next: 'Walkback reads no suit telemetry today: it is the kind of problem the NASA SUITS challenge sets. Today the Quest 3 stands in for the helmet display.', dice: '' }
    }
  ],

  // Respaldo (tecla A): no cuenta en los 3 minutos.
  backup: {
    es: { realtime: '¿Datos en tiempo real? Una cosa: al abrir, la app consulta NASA DONKI por los eventos de partículas solares de la semana y muestra la hora; la línea dice (live) o (stored in the app). El clima de Marte es el último reporte de Curiosity, con su fecha. Nada de eso entra en la cuenta del oxígeno.',
          fps: '{fps} fps en la mesa y en Marte a escala real (Quest 3, 15/11, del log de la app).' },
    en: { realtime: 'Real-time data? One thing: at start the app asks NASA DONKI for the week\'s solar particle events and shows the time; the line says (live) or (stored in the app). Mars weather is Curiosity\'s latest report, with its date. None of it feeds the oxygen estimate.',
          fps: '{fps} fps at the table and on Mars at real scale (Quest 3, 15 Nov, from the app\'s own log).' }
  }
};
