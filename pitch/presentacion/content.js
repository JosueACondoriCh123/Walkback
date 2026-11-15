// Walkback · contenido de la presentación.
// JOSUÉ: este es el ÚNICO archivo que hay que tocar para cambiar una frase, un tiempo o una nota.
// - Cada lámina tiene "es" (defensa) y "en" (NASA). La tecla L cambia de idioma.
// - Lo que está entre llaves, como {km} o {total}, lo calcula la presentación con la misma cuenta que la app
//   (walkback-model.js). NO escribas esos números a mano: si cambia el terreno, cambian solos.
// - "dice" es lo que dice Daniel (sale en las notas, tecla N). "segundos" es lo que dura la lámina: suman 180.
// - Los números de FACTS no salen de la cuenta: cada uno dice de dónde sale. Si cambiás uno, cambiá su fuente.
window.WALKBACK_CONTENT = {
  lang: 'es',

  // Números que NO calcula el modelo. Fuente al lado. No agregar ninguno sin fuente.
  FACTS: {
    positions: 85,            // DATA.md, tabla de controles: posiciones de Curiosity
    slopeMeanDiff: '2,0°',    // DATA.md: diferencia MEDIA entre la pendiente del DEM y el sensor de inclinación (r = 0,77; máx. 21°)
    slopeR: '0,77',
    slopeMax: '21°',
    demMeters: 1,             // DATA.md: HiRISE DEM (Mars Trek), 1 m
    photoCm: 25,              // DATA.md: foto HiRISE, 25 cm por píxel
    fps: 72,                  // CLAUDE.md: en la mesa, Quest 3, 14/11 (Marte a escala real: sin medir)
    tests: '424 de 424',      // CLAUDE.md, 15/11
    testsEn: '424 of 424'
  },

  // Valores que llena el modelo al abrir (se listan acá solo para saber que existen):
  // {km} 1,8 km · {meters} 1812 · {walk} 43 min · {work} 2 h 12 min · {total} 2 h 55 min · {usedL} 181 · {usedPct} 36
  // {spareL} 285 · {tankL} 497 · {reserveL} 31 · {limit} 20 · {red8} 57,5 · {reach8} 8,5 · {farStopMin} 14,7

  slides: [
    {
      id: 'title', scene: 'globe', segundos: 0,
      es: { tag: 'NASA Space Apps 2026 · Santa Cruz, Bolivia · MARSXR', title: 'Walkback', sub: 'The safest way to go far.',
            credit: 'Mosaico Viking MDIM 2.1 · NASA/JPL/USGS · color realzado',
            dice: '(Antes de empezar. El reloj arranca al pasar a la lámina siguiente.)' },
      en: { tag: 'NASA Space Apps 2026 · Santa Cruz, Bolivia · MARSXR', title: 'Walkback', sub: 'The safest way to go far.',
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
            credit: 'Relieve HiRISE 1 m: NASA Mars Trek · foto HiRISE: USGS / NASA/JPL/University of Arizona',
            dice: 'Explorar no es ir lejos: es saber hasta dónde. Walkback es una herramienta para el astronauta y para quien planifica la salida. Esto es un kilómetro cuadrado real del cráter Gale, con relieve y foto de HiRISE, y cuatro sitios donde perforó Curiosity. Lo rayado es pendiente de más de veinte grados: no se pisa. La app arma la ida y la vuelta: un kilómetro ochocientos, dos horas cincuenta y cinco, treinta y seis por ciento del oxígeno. Estimado, y la app lo dice.' },
      en: { tag: 'One real km² of Gale · Meta Quest 3', title: 'Exploring is not going far.', titleAccent: 'It is knowing how far.',
            big: [['{kmEn}', 'out and back'], ['{total}', 'walking and work'], ['{usedPct} %', 'of the oxygen']],
            estimate: 'ESTIMATE',
            credit: 'HiRISE 1 m relief: NASA Mars Trek · HiRISE photo: USGS / NASA/JPL/University of Arizona', dice: '' }
    },
    {
      id: 'verdict', scene: 'gale', pose: 'board', overlay: 'board', segundos: 10,
      es: { tag: 'El tablero de la app', title: 'Con el tanque lleno:', titleAccent: 'VIABLE.',
            credit: 'Tablero redibujado con los textos y la cuenta de la app · ESTIMADO',
            dice: 'Con el tanque lleno, el plan es viable. ¿Y si el traje falla y queda el ocho por ciento del oxígeno? Veámoslo en el visor.' },
      en: { tag: 'The app\'s board', title: 'With a full tank:', titleAccent: 'VIABLE.',
            credit: 'Board redrawn with the app\'s texts and the app\'s numbers · ESTIMATE', dice: '' }
    },
    {
      id: 'demo', scene: 'gale', pose: 'whatif', overlay: 'demo', segundos: 45,
      es: { title: 'DEMO', sub: 'Meta Quest 3 · en vivo', hint: 'R: respaldo animado · V: video local',
            acts: [['PLAN', 'El plan y su veredicto.'], ['WHAT IF', 'El tanque baja al 8 %: {red8} % del terreno con vuelta queda en rojo.'], ['MISIÓN', 'El mismo terreno, a escala real, con el visor.']],
            credit: 'Respaldo: la misma cuenta que la app, corriendo en esta lámina · ESTIMADO',
            dice: '0–15 s: Este es el plan: hábitat, cuatro sitios donde perforó Curiosity, y vuelta. Estimación: 1,8 km, 2 h 55 min en total, 36 % del oxígeno. VIABLE. · 15–30 s: ¿Y si falla el traje y queda el 8 %? NOT VIABLE. El rojo es desde dónde ya no se vuelve caminando. · 30–45 s: Y esto es salir: el mismo terreno a escala real. En el casco, el oxígeno y los minutos para volver.' },
      en: { title: 'DEMO', sub: 'Meta Quest 3', hint: '',
            acts: [['PLAN', 'The plan and its verdict.'], ['WHAT IF', 'The tank drops to 8 %: {red8En} % of the ground with a way back turns red.'], ['MISSION', 'The same terrain at real scale, with the visor.']],
            credit: 'The app\'s own rule, running in this slide · ESTIMATE', dice: '' }
    },
    {
      id: 'evidence', scene: 'gale', pose: 'evidence', segundos: 30,
      es: { tag: 'Por qué creernos', title: 'Terreno comprobado', titleAccent: 'contra el propio Curiosity.',
            big: [['{demMeters} m', 'relieve HiRISE'], ['{positions}', 'posiciones de Curiosity'], ['{slopeMeanDiff}', 'diferencia media de pendiente']],
            lines: ['Oxígeno y tiempo: estimados, y la app lo dice.'],
            credit: 'DATA.md · r = {slopeR}, diferencia máxima {slopeMax} · prueba automática de la app: {tests}',
            dice: '¿Por qué creernos? El terreno no lo inventamos: el relieve es de HiRISE, a un metro, y lo comparamos con el sensor de inclinación del propio Curiosity en ochenta y cinco posiciones. La diferencia media es de dos grados. El oxígeno y el tiempo son estimaciones, y la app lo dice en pantalla. No escondemos nada. (Rocas chicas y arena: solo si preguntan.)' },
      en: { tag: 'Why trust it', title: 'Terrain checked', titleAccent: 'against Curiosity itself.',
            big: [['{demMeters} m', 'HiRISE relief'], ['{positions}', 'Curiosity positions'], ['{slopeMeanDiffEn}', 'mean slope difference']],
            lines: ['Oxygen and time: estimates, and the app says so.', 'The map does not see rocks under about 4 m, or sand.'],
            credit: 'DATA.md · r = {slopeREn}, largest difference {slopeMax} · the app\'s automatic test: {testsEn}', dice: '' }
    },
    {
      id: 'closing', scene: 'astronaut', overlay: 'qr', segundos: 40,
      es: { tag: 'MARSXR · NASA Space Apps 2026', title: 'Porque volver también', titleAccent: 'es parte de la misión.',
            brand: 'Walkback', sub: '', qr: 'github.com/kendal-dev/Walkback',
            credit: 'Astronauta: ilustración hecha con IA, vista ilustrativa · HUD: los textos de la app con los números de su plan (ESTIMADO) · fondo: foto real Curiosity Mastcam PIA26696, NASA/JPL-Caltech/MSSS · IA declarada en AI_USAGE.md',
            dice: 'Walkback convierte una pregunta de vida o muerte en un color: verde, seguís; rojo, volvés. Hoy el Quest 3 hace de visor del casco. Lo que sigue: el modelo definitivo de oxígeno y leer la telemetría real del traje en lugar de estimarla. Somos MARSXR, de Santa Cruz. Porque volver también es parte de la misión. Gracias. (Últimos 5 s: el QR en pantalla, sin hablar.)' },
      en: { tag: 'MARSXR · NASA Space Apps 2026', title: 'Because coming back', titleAccent: 'is part of the mission.',
            brand: 'Walkback', sub: '', qr: 'github.com/kendal-dev/Walkback',
            credit: 'Astronaut: 3D model from NASA 3D Resources (public domain), illustrative view · background: real photo, Curiosity Mastcam PIA26696, NASA/JPL-Caltech/MSSS · project built with AI help, declared in AI_USAGE.md',
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
