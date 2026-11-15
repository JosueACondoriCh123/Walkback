# Walkback · presentación de 3 minutos

Abrir `index.html` con doble clic. Todo es local: no hace falta internet.

7 láminas (título + 6 del reloj) y un anexo. **No hay capturas de pantalla:** cada imagen es una escena 3D hecha con los datos reales de la app (relieve y foto de HiRISE, las cuatro paradas de Curiosity, el globo Viking, la foto real de Nevado Sajama) y con **la misma cuenta que la app** (`walkback-model.js`, el plan B portado a JavaScript). Cada cifra del plan dice ESTIMADO / ESTIMATE.

## Teclas

| Tecla | Qué hace |
|---|---|
| → / espacio / clic | siguiente lámina |
| ← | anterior |
| T | reloj (se pone rojo a los 3:00) |
| N | notas: lo que dice Daniel |
| F | pantalla completa |
| L | español ↔ inglés (versión para la NASA) |
| R | en la lámina DEMO: respaldo animado si falla la demo en vivo (plan → WHAT IF al 8 %) |
| V | en la lámina DEMO: cargar un video local |
| A | anexo (respuestas al jurado) |
| P | imprimir a PDF (cada lámina con su escena) |

También: `index.html?lang=en` abre en inglés.

## Cómo cambiar algo

- **Una frase, un tiempo o una nota:** solo `content.js`. Cada lámina tiene `es` y `en`. Recargar la página.
- **Un número del plan** (`{km}`, `{total}`, `{usedPct}`, `{red8}`…): **no se escribe a mano**. Lo calcula `walkback-model.js` con el relieve de `data/gale.js` y las paradas de `data/stops.js`.
- **Un número que no sale de la cuenta** (85 posiciones, 2,0°, 72 fps, 424 controles): `content.js`, bloque `FACTS`, cada uno con su fuente al lado.
- **Comprobar que la cuenta sigue igual a la app:** `node tools/check-model.js` → tiene que terminar en `ALL PASSED` (18 controles contra los números del test de la app).
- **Regenerar los datos** desde el paquete del sitio: `python -I tools/build_pitch_data.py <Assets/StreamingAssets/Site> data <Astronaut.glb>`.

## Archivos

| Archivo | Qué es |
|---|---|
| `index.html` | solo la estructura; ningún texto |
| `content.js` | todo el texto, los tiempos, las notas y los números con fuente |
| `deck.js`, `deck.css` | navegación, idioma, reloj, notas, respaldo, impresión; paleta de la app |
| `walkback-model.js` | la cuenta de la app (ReturnFallback, MissionFallback, HudEstimate) |
| `scenes/` | una escena por archivo: `gale.js` (+ `gale-core.js`), `globe.js`, `panorama.js`, `astronaut.js`, `board.js` (el tablero en HTML) |
| `data/` | datos generados del sitio: `gale.js`, `stops.js`, `globe.js`, `sajama.js`, `astronaut.js` |
| `img/` | imágenes de respaldo si WebGL falla (datos reales, no capturas) y el QR |

## Créditos

Relieve HiRISE 1 m: NASA Mars Trek · foto HiRISE: USGS / NASA/JPL/University of Arizona · Curiosity Mastcam PIA26696: NASA/JPL-Caltech/MSSS · posiciones de Curiosity: NASA/JPL-Caltech · globo Viking MDIM 2.1: NASA/JPL/USGS · astronauta: modelo 3D de NASA 3D Resources (dominio público), vista ilustrativa. Three.js (MIT), Draco (Apache 2.0), Inter (SIL OFL 1.1). Sin logos de la NASA. **Ninguna imagen hecha con IA.** La presentación se hizo con ayuda de IA (Claude Code), declarado en `AI_USAGE.md`.
