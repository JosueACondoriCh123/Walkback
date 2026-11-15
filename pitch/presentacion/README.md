# Walkback · presentación de 3 minutos

Abrir `index.html` con doble clic. Todo es local: no hace falta internet.

7 láminas (título + 6 del reloj) y un anexo. **No hay capturas de pantalla:** cada imagen es una escena 3D hecha con los datos reales de la app (relieve y foto de HiRISE, las cuatro paradas de Curiosity, el globo Viking, la foto real de Nevado Sajama) y con **la misma cuenta que la app** (`walkback-model.js`, el plan B portado a JavaScript). Cada cifra del plan dice ESTIMADO / ESTIMATE. El cierre es el mismo Gale, desde Campo Marte mirando al hábitat por la ruta.

## Teclas

| Tecla | Qué hace |
|---|---|
| → / espacio / clic | siguiente lámina |
| ← | anterior |
| T | reinicia el reloj. El reloj **arranca solo** al pasar de la portada a la lámina 1; en la portada, T lo deja listo para arrancar (se pone rojo a los 3:00) |
| N | notas: lo que dice Daniel |
| F | pantalla completa |
| L | español ↔ inglés (versión para la NASA) |
| R | en la lámina DEMO: respaldo animado si falla la demo en vivo (plan → WHAT IF de 100 a 8 %); R otra vez lo corta |
| V | en la lámina DEMO: cargar un video local |
| A | anexo (respuestas al jurado) |
| P | imprimir a PDF: 7 páginas, la DEMO en el estado final del WHAT IF (Ctrl+P solo también imprime las 7) |

También: `index.html?lang=en` abre en inglés, `index.html#4` abre en la lámina 4, `index.html?lang=en&print=1` arma las 7 páginas del PDF al abrir.

## El PDF para la NASA (7 páginas, en inglés)

1. Antes: poner los nombres del equipo en `content.js`, lámina `title`, campo `members` (en `es` y en `en`). Vacío no se muestra.
2. Abrir `index.html?lang=en`, tecla **P**, y en el diálogo: Guardar como PDF, márgenes Ninguno, Gráficos de fondo marcado.
3. O sin diálogo, en PowerShell (Edge; con Chrome es igual), cambiando `<ruta>` por la carpeta de la presentación con barras `/`:
   `& "C:\Program Files (x86)\Microsoft\Edge\Application\msedge.exe" --headless=new --no-pdf-header-footer --virtual-time-budget=30000 --user-data-dir="$env:TEMP\wb-pdf" --print-to-pdf="$PWD\Walkback-NASA-demo.pdf" "file:///<ruta>/index.html?lang=en&print=1"`
   (`--user-data-dir` aparte para que no se cuelgue de un Edge ya abierto.)
4. Revisar: 7 páginas; la página 1 con equipo, integrantes y reto; la 5 con el tanque al 8 %, NOT VIABLE y el rojo.

## Cómo cambiar algo

- **Una frase, un tiempo o una nota:** solo `content.js`. Cada lámina tiene `es` y `en`. Recargar la página.
- **Un número del plan** (`{km}`, `{total}`, `{usedPct}`, `{red8}`…): **no se escribe a mano**. Lo llena `placeholders.js` con la cuenta de `walkback-model.js`, el relieve de `data/gale.js` y las paradas de `data/stops.js`. La lista de llaves la imprime `node tools/check-content.js`.
- **Un número que no sale de la cuenta** (85 posiciones, 2,0°, 72 fps, 424 controles): `content.js`, bloque `FACTS`, cada uno con su fuente al lado.
- **Después de cada cambio de texto:** `node tools/check-model.js && node tools/check-content.js` → los dos tienen que terminar en `ALL PASSED`. El primero compara la cuenta con los números del test de la app (18 controles); el segundo revisa los textos: llaves, tiempos (suman 180 s), palabras por segundo, ES = EN, ESTIMADO junto a cada cifra del plan, frases prohibidas, "con vuelta a pie", equipo y reto en la portada. Avisa (no falla) mientras `members` esté vacío.
- **Regenerar los datos** desde el paquete del sitio: `python -I tools/build_pitch_data.py <Assets/StreamingAssets/Site> data` (escribe `gale.js` y `globe.js`).
- **Probar solo el terreno 3D:** `prueba-gale.html` con doble clic (la página de prueba de la auditoría R20): teclas 1 a 5 para los cinco encuadres, W para el WHAT IF, 0 para el tanque lleno.

Si WebGL no anda en la máquina, ninguna lámina queda negra: se ve la foto real de cada escena (el globo Viking, la foto de Nevado Sajama, la foto de HiRISE donde iría el mapa) y las cifras, que no dependen del 3D.

## Archivos

| Archivo | Qué es |
|---|---|
| `index.html` | solo la estructura; ningún texto |
| `content.js` | todo el texto, los tiempos, las notas, los textos de la interfaz (`ui`) y los números con fuente |
| `placeholders.js` | arma cada `{llave}` con la cuenta de la app y `FACTS` (coma decimal en español, punto en inglés) |
| `deck.js`, `deck.css` | navegación, idioma, reloj, notas, respaldo, impresión; paleta de la app; dónde va el texto en cada lámina |
| `walkback-model.js` | la cuenta de la app (ReturnFallback, MissionFallback, HudEstimate) |
| `scenes/` | una escena por archivo: `gale.js` (+ `gale-core.js`, el terreno de la auditoría R20), `globe.js`, `panorama.js`, `board.js` (el tablero en HTML), `common.js` |
| `data/` | datos del sitio: `gale.js`, `stops.js`, `globe.js`, `sajama.js` |
| `img/` | imágenes de respaldo si WebGL falla (datos reales, no capturas) y el QR |
| `tools/` | `check-model.js`, `check-content.js`, `build_pitch_data.py` |
| `vendor/` | `three.min.js` (r128) |
| `fonts/` | Inter y su licencia |
| `prueba-gale.html` | página de prueba del terreno 3D (no es la presentación) |

## Créditos

Relieve HiRISE 1 m: NASA Mars Trek · foto HiRISE: USGS / NASA/JPL/University of Arizona · Curiosity Mastcam PIA26696: NASA/JPL-Caltech/MSSS · posiciones de Curiosity: NASA/JPL-Caltech · globo Viking MDIM 2.1: NASA/JPL/USGS. Three.js (MIT), Inter (SIL OFL 1.1). Sin logos de la NASA. **Ninguna imagen hecha con IA.** La presentación se hizo con ayuda de IA (Claude Code), declarado en `AI_USAGE.md`.
