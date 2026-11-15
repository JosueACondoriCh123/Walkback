# Tareas para agentes externos (página web de Walkback)

Cada archivo es un pedido completo: se copia y se pega tal cual en el agente (Codex, Gemini, Cursor, Copilot, etc.). Junto con el pedido, pasale la carpeta `walkback-web/` (en zip o, cuando exista, el repo en la rama `web`).

| # | Archivo | Qué hace | Toca | Cuándo |
|---|---------|----------|------|--------|
| 1 | `01-curiosity.md` | Prepara el rover Curiosity (modelo achicado a menos de 10 MB, cargador y crédito) | archivos nuevos: `web/curiosity.js`, `web/tools/prepare_curiosity.mjs`, `web/data/curiosity.glb`, GLTFLoader | ya; se integra recién después de las 18:30 (punto 4a) |
| 2 | `02-webxr.md` | Prepara el botón "Enter VR" a escala real | archivos nuevos: `web/xr.js`, `web/tools/xr_test.html`, VRButton | ya; se integra después de 4a (punto 4b) |
| 3 | `03-check-web.md` | Script que revisa la página antes de publicar (sin internet, sin LFS, tamaños, comentario de autoría, datos) | archivo nuevo: `web/tools/check_web.mjs` | ya |
| 4 | `04-revision.md` | Revisión independiente de bugs, celular, rendimiento e inglés | nada (solo lectura) | ya |

## Por qué así

- Las cuatro tareas usan **archivos distintos y nuevos**, así que pueden correr al mismo tiempo sin pisarse.
- Ninguna edita `index.html`, `style.css` ni `app.js`: eso lo integra la sesión D (Claude), para que haya un solo responsable de la página.
- Ninguna calcula rutas ni tiempos de regreso: esos números son de la sesión C.

## Cuando te devuelvan el trabajo

Pasale a la sesión D los archivos y las tres líneas de respuesta (qué hizo, cómo lo probó, qué falta). En la revisión (tarea 4), pasale la lista de bugs tal cual. Si la tarea 1 avisa que `curiosity.glb` cae en Git LFS, eso va para Daniel (`.gitattributes`).
