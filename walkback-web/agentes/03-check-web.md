# Tarea 3: script que revisa la página antes de publicar

## Contexto

Equipo MARSXR, NASA Space Apps 2026 (sede Santa Cruz). Proyecto **Walkback** ("The safest way to go far."): realidad mixta en Meta Quest 3 que le dice a un astronauta en Marte si todavía puede volver al hábitat. Hay una página web estática en `web/` (HTML, CSS y JS, sin build) publicada en GitHub Pages. La defensa es sin internet y el código se congela el domingo a las 10:00.

Te pasamos la carpeta `walkback-web/`. Después va a ser la carpeta `web/` del repo github.com/kendal-dev/Walkback, rama `web`.

- `web/index.html`, `web/style.css`, `web/app.js`: la página. **No los edites.**
- `web/vendor/`: three.js y la letra Inter, de terceros. Sus archivos no llevan nuestro comentario.
- `web/data/`: `site.json`, `heightmap.bin`, `stops.json`, `route.json`, `return_map.bin` y `status.json` (`{"test": true}` mientras los datos son inventados). `ortho.jpg` puede faltar.
- `web/tools/`: scripts de Node sin dependencias.

## Reglas

- Primera línea de cada archivo de código, como comentario: `Written with <nombre de tu asistente> (AI), directed and tested by team MARSXR`.
- Node puro, **sin dependencias** (nada de `npm install`).
- Tocá solo los archivos de "Entregables".
- Si trabajás sobre el repo: un commit en inglés, con `Co-Authored-By` de tu asistente. Si no, entregá el archivo sin commit.

## Entregable

**`web/tools/check_web.mjs`**: se corre como `node web/tools/check_web.mjs`, imprime un resumen claro con ✔ / ✖ / ⚠ por chequeo y termina con código 1 si hay algún ✖.

Chequeos (✖ = error, ⚠ = advertencia):

1. ✖ **Sin internet:** ningún `.html`, `.js`, `.mjs` o `.css` de `web/` fuera de `web/vendor/` carga recursos de afuera: `src=`, `<link href=`, `import ... from`, `fetch(`, `url(` o el importmap con `http://` o `https://`. Los `<a href>` hacia afuera (video, APK) **sí** se permiten, porque son enlaces y no recursos.
2. ✖ **Referencias rotas:** toda ruta local de esos lugares, más los literales `fetch("data/...")` de `app.js`, apunta a un archivo que existe. Excepción: `data/ortho.jpg` es opcional (⚠ si falta).
3. ✖ **Comentario de autoría:** la primera línea de cada `.html`, `.js`, `.mjs` y `.css` propio (fuera de `vendor/`), y de `.github/workflows/pages.yml` si existe, contiene `directed and tested by team MARSXR`.
4. ✖ **LFS y tamaños:** ningún archivo de `web/` es un puntero de Git LFS (empieza con `version https://git-lfs.github.com/spec/`). Ningún archivo pesa más de 10 MB y `web/` entera, menos de 50 MB. Imprimí los 10 archivos más pesados.
5. ✖ **Git LFS:** si la carpeta está dentro de un repo git, `git lfs ls-files` no lista nada bajo `web/`. Si no hay git o git-lfs, ⚠ y seguir.
6. ✖ **Datos:**
   - `site.json` tiene `name`, `centerLat`, `centerLon`, `sideMeters`, `heightmapSize` (513), `gridSize` (257), `minElevation`, `maxElevation`, `habitat {x, y}`, `crs` y `sources` (lista con `layer`, `mission`, `instrument`, `resolution`, `id`, `credit`).
   - `heightmap.bin` pesa exactamente 513 × 513 × 2 bytes.
   - `return_map.bin`, si está, pesa 257 × 257 × 8 bytes.
   - `stops.json` es una lista con `id`, `name`, `x`, `y` dentro de 0..1000.
7. ⚠ **Listo para entregar:**
   - Si `status.json` dice `"test": true`, imprimí bien visible `STILL TEST DATA`.
   - Listá cada `TODO` o `aria-disabled` que quede en `index.html`, con número de línea; sirve para el congelado del domingo.

## Terminado cuando

- Corre sobre la carpeta tal como está y el resumen tiene sentido. Hoy debería dar ⚠ por datos de prueba y por los TODO del video y el APK, sin ✖.
- Probaste que falla con casos rotos, sobre una **copia** de la carpeta:
  - un `<script src="https://cdn...">`;
  - un `heightmap.bin` truncado;
  - un `.js` sin el comentario de autoría;
  - un archivo que es un puntero de LFS.

Al terminar, respondé en tres líneas: qué hiciste, cómo lo probaste y qué falta.
