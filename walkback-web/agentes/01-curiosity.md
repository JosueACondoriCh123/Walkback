# Tarea 1: el rover Curiosity sobre el terreno (preparar, sin integrar)

## Contexto

Equipo MARSXR, NASA Space Apps 2026 (sede Santa Cruz). Proyecto **Walkback** ("The safest way to go far."): realidad mixta en Meta Quest 3 que le dice a un astronauta en Marte, en cada paso, si todavía puede volver al hábitat. Hay una página web estática (sin build, sin framework, sin npm en la página) que muestra 1 km² del cráter Gale en 3D con **three.js 0.170.0**, para que el jurado lo pruebe sin visor.

Te pasamos la carpeta `walkback-web/`. Después va a ser la carpeta `web/` del repo github.com/kendal-dev/Walkback, rama `web`.

- `web/index.html`, `web/style.css`, `web/app.js`: la página. **No los edites**: los integra otra persona.
- `web/vendor/three/`: `three.module.min.js` 0.170.0 y addons. El importmap de index.html mapea `"three"` → `./vendor/three/three.module.min.js` y `"three/addons/"` → `./vendor/three/addons/`.
- `web/data/`: datos (site.json, heightmap.bin, stops.json, route.json, return_map.bin). Hoy son de prueba.
- `web/tools/`: scripts de Node sin dependencias.

**Coordenadas:** metros desde la esquina suroeste del km², `x` al este, `y` al norte. En three.js se usa `X = x − 500`, `Z = 500 − y` e `Y = (altura − minElevation) × 2`. Ese ×2 es una exageración vertical solo visual. `app.js` tiene `toWorld(x, y, lift)`, que hace esa cuenta y devuelve un `THREE.Vector3` apoyado en el terreno.

## Reglas

- Primera línea de cada archivo de código, como comentario: `Written with <nombre de tu asistente> (AI), directed and tested by team MARSXR`.
- Todo lo que se ve, en inglés y legible en un celular. Nada de logos de la NASA (el texto del crédito sí va).
- Tiene que andar **sin internet**: nada de CDN, todo copiado dentro de `web/`.
- En este repo `*.glb`, `*.r16` y `*.tif` van en Git LFS, y GitHub Pages **no sirve archivos de LFS**.
- Tocá solo los archivos de "Entregables".
- Si trabajás sobre el repo: un commit por pieza, en inglés, con `Co-Authored-By` de tu asistente. Si no, entregá los archivos sin commit.
- Probar en local: `python -m http.server 8137 --directory web` y abrir http://localhost:8137

## Objetivo

Dejar listo el modelo 3D del rover Curiosity, apoyado sobre el terreno en un punto donde el rover estuvo de verdad. Lo integra después la sesión D.

## Entregables

1. **`web/tools/prepare_curiosity.mjs`**: toma el `.glb` original y escribe `web/data/curiosity.glb`, de **menos de 10 MB** (ideal, menos de 5 MB).
   - La ruta del original va por argumento. Por defecto es `Assets/Walkback/Models3D/`, donde lo deja la sesión B. Para probar, podés bajar vos el modelo de Curiosity del sitio de modelos 3D de la NASA.
   - Puede usar `npx @gltf-transform/cli` (solo para preparar; la página no lo necesita): simplify, weld, quantize y texturas a 1024 px o menos en webp.
   - Si usás compresión meshopt, copiá `examples/jsm/libs/meshopt_decoder.module.js` de three@0.170.0 a `web/vendor/three/addons/libs/`. No uses Draco, porque necesita archivos wasm aparte.
2. **`web/vendor/three/addons/loaders/GLTFLoader.js`** y **`web/vendor/three/addons/utils/BufferGeometryUtils.js`**, copiados de three@0.170.0 (`npm pack three@0.170.0`). Respetá esas carpetas: GLTFLoader importa `'../utils/BufferGeometryUtils.js'`.
3. **`web/curiosity.js`**: módulo ES que exporta:
   - `async function addCuriosity(scene, toWorld, { x, y, headingDeg = 0, vex = 2, url = "data/curiosity.glb" })`: carga el modelo y lo escala a tamaño real. Curiosity mide unos 3 m de largo, 2,7 m de ancho y 2,2 m de alto; el alto se multiplica por `vex` para que combine con el terreno. Lo apoya en `toWorld(x, y)` y lo devuelve. Si la carga falla, no rompe la página: `console.warn` y devuelve `null`.
   - `function pickTraversePoint(traverse, sideMeters = 1000)`: la sesión B entrega `curiosity_traverse.json`, con un formato todavía no definido. Aceptá una lista de puntos `{x, y}` en coordenadas locales (o `{points: [...]}`) y devolvé uno dentro del km² (0 a 1000 en x e y), o `null`. Escribí en un comentario qué formato asumiste.
4. **`web/data/CURIOSITY_CREDIT.txt`**: de dónde sale el modelo, la URL de la fuente y el crédito exacto que pide (por ejemplo "NASA/JPL-Caltech").
5. **`web/tools/curiosity_test.html`**: página mínima con el mismo importmap que `index.html`. Muestra el rover sobre un plano, con OrbitControls (ya está en `web/vendor/three/addons/OrbitControls.js`).

## Ojo

`*.glb` va en LFS en este repo. Si `curiosity.glb` termina en LFS, la página publicada no lo va a poder cargar. Avisalo en tu respuesta para que Daniel agregue en `.gitattributes` una excepción para `web/`.

## Terminado cuando

- `curiosity_test.html` muestra el rover a escala, sin errores en la consola y sin pedidos a internet (revisá la pestaña Network).
- `curiosity.glb` pesa menos de 10 MB.

Al terminar, respondé en tres líneas: qué hiciste, cómo lo probaste y qué falta.
