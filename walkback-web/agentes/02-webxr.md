# Tarea 2: botón "Enter VR" para pararse en Gale a escala real (preparar, sin integrar)

## Contexto

Equipo MARSXR, NASA Space Apps 2026 (sede Santa Cruz). Proyecto **Walkback** ("The safest way to go far."): realidad mixta en Meta Quest 3 que le dice a un astronauta en Marte, en cada paso, si todavía puede volver al hábitat. Hay una página web estática (sin build, sin framework, sin npm en la página) que muestra 1 km² del cráter Gale en 3D con **three.js 0.170.0**, para que el jurado lo pruebe sin visor.

Te pasamos la carpeta `walkback-web/`. Después va a ser la carpeta `web/` del repo github.com/kendal-dev/Walkback, rama `web`.

- `web/index.html`, `web/style.css`, `web/app.js`: la página. **No los edites**: los integra otra persona.
- `web/vendor/three/`: `three.module.min.js` 0.170.0 y addons. El importmap de index.html mapea `"three"` → `./vendor/three/three.module.min.js` y `"three/addons/"` → `./vendor/three/addons/`.
- `web/data/`: `site.json` (con `habitat {x, y}`, `minElevation`, `maxElevation`, `heightmapSize` 513, `sideMeters` 1000), `heightmap.bin` (513 × 513 enteros de 16 bits little-endian, primera fila al norte; altura = min + v / 65535 × (max − min)), `stops.json` ([{id, name, x, y}]). Hoy son de prueba.

**Coordenadas:** metros desde la esquina suroeste del km², `x` al este, `y` al norte. En three.js se usa `X = x − 500`, `Z = 500 − y` e `Y = (altura − minElevation) × vex`, con `vex = 2` (exageración vertical, solo para la vista web). La página usa `renderer.setAnimationLoop`, que ya es compatible con WebXR.

## Reglas

- Primera línea de cada archivo de código, como comentario: `Written with <nombre de tu asistente> (AI), directed and tested by team MARSXR`.
- Todo lo que se ve, en inglés. Nada de logos de la NASA.
- Tiene que andar **sin internet**: nada de CDN, todo copiado dentro de `web/`.
- Tocá solo los archivos de "Entregables".
- Si trabajás sobre el repo: un commit por pieza, en inglés, con `Co-Authored-By` de tu asistente. Si no, entregá los archivos sin commit.

## Objetivo

Que desde el navegador del Meta Quest 3 alguien toque "Enter VR" y quede parado en el hábitat, en Gale, a escala real (1:1, sin exageración vertical).

## Entregables

1. **`web/vendor/three/addons/webxr/VRButton.js`**, copiado de three@0.170.0 (`npm pack three@0.170.0`).
2. **`web/xr.js`**: módulo ES que exporta `setupXR({ renderer, scene, camera, world, habitat, stops, toWorld, vex = 2, container })`.
   - `world` es un `THREE.Group` con el terreno y todos los marcadores. Al integrar, la sesión D mete todo dentro de ese grupo.
   - Pone el botón de three (texto "Enter VR") dentro de `container`. En compus y celulares sin VR, el botón deshabilitado de three ("VR NOT SUPPORTED") está bien, pero no tiene que tapar el terreno ni el resto de la interfaz.
   - Al entrar: `renderer.xr.enabled = true`, `world.scale.y = 1 / vex` (alturas reales) y el usuario parado en el hábitat. Para eso se mueve `world` de modo que el suelo del hábitat quede en el origen; la altura de los ojos la da el visor (reference space `local-floor`). Mirando hacia la primera parada, si hay.
   - Cielo de Marte simple (color o gradiente generado en código, sin descargar nada) y niebla suave para el horizonte.
   - Al salir: `world` y la cámara vuelven exactamente a como estaban, y el fondo de la página vuelve a ser transparente.
   - Opcional: las etiquetas HTML (CSS2DRenderer) no se ven en VR. Si te sobra tiempo, sumá etiquetas como sprites de canvas solo para VR ("Habitat", nombres de las paradas), legibles a 50 m.
3. **`web/tools/xr_test.html`**: página mínima, con el mismo importmap que `index.html`, que arma el terreno leyendo `../data/site.json` y `../data/heightmap.bin` y usa `xr.js`. Sirve para probar sin tocar `app.js`.

## Ojo

WebXR exige https (o localhost). En el Quest, entrar por http a la IP de la compu **no anda**. Probá con la extensión "Immersive Web Emulator" en Chrome de escritorio; en el Quest real se prueba cuando esté en GitHub Pages, que es https.

## Terminado cuando

- Con el emulador entrás y salís de VR dos veces seguidas sin errores en la consola.
- Adentro estás parado en el hábitat, el suelo queda a tus pies y las alturas son reales: un montículo de 40 m se ve de 40 m, no de 80.
- Al salir, la vista 3D normal queda igual que antes de entrar.

Al terminar, respondé en tres líneas: qué hiciste, cómo lo probaste y qué falta.
