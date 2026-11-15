# Tarea 4: revisión independiente de la página (solo lectura)

## Contexto

Equipo MARSXR, NASA Space Apps 2026 (sede Santa Cruz). Proyecto **Walkback** ("The safest way to go far."): realidad mixta en Meta Quest 3 que le dice a un astronauta en Marte, en cada paso, si todavía puede volver al hábitat. La página web, en `web/`, es para que el jurado lo pruebe **desde el celular o la compu, sin visor**.

Qué muestra:

- 1 km² del cráter Gale en 3D con three.js 0.170.0, que se gira con el mouse o el dedo.
- Las zonas de más de 20° rayadas, el hábitat, las paradas y la ruta segura.
- El veredicto (VIABLE o punto de no retorno), con minutos y % de O₂.
- Al tocar un punto del terreno: minutos y % de O₂ para volver al hábitat, leídos de `data/return_map.bin` (257 × 257 pares float32 little-endian, minutos y litros, fila 0 al norte).

Coordenadas: metros desde la esquina suroeste, `x` al este, `y` al norte. `heightmap.bin`: 513 × 513 uint16 little-endian, fila 0 al norte. Los números de ruta y regreso vienen del modelo de otra sesión: la página no los calcula, solo los muestra.

Archivos: `web/index.html`, `web/style.css`, `web/app.js`. Lo de `web/vendor/` es three.js e Inter, de terceros; no hace falta revisarlo. Para verla: `python -m http.server 8137 --directory web` y abrir http://localhost:8137

## Reglas

- **No edites ningún archivo.** Esta tarea es solo de lectura y prueba; los arreglos los aplica otra persona.
- No propongas frameworks, npm ni pasos de build: la página es estática y tiene que andar sin internet.
- No propongas calcular rutas ni tiempos en la página.

## Qué revisar

1. **Bugs:**
   - Conversión de coordenadas (norte y sur, este y oeste, fila 0 al norte) en el terreno, la textura, el rayado, el toque y la lectura de `return_map.bin`.
   - Eventos táctiles: diferenciar un toque de un arrastre o pellizco.
   - Redimensionado (ResizeObserver, giro del celular) y valores NaN.
2. **Datos raros:** `route.json` vacío o con otros nombres de campos, `return_map` con NaN o negativos, paradas fuera del km², sin `ortho.jpg`, `site.json` sin `sources`. La página no se tiene que romper.
3. **Rendimiento en un celular de gama media:** textura de 2048 px, 66.000 vértices, pixelRatio 2, render continuo y batería.
4. **Celular:** tamaños de letra, contraste, botones de al menos 44 px, que la página se pueda bajar con el dedo aunque el visor 3D ocupe media pantalla, y la tarjeta del toque sin tapar todo.
5. **Inglés:** todos los textos visibles, claros para un jurado no técnico, cortos y sin jerga.

## Formato de la respuesta

Lista ordenada por gravedad, **máximo 15 puntos**, separada en **Bugs** y **Sugerencias**. Cada punto con:

- `archivo:línea`
- qué pasa
- cómo reproducirlo
- arreglo propuesto, como diff corto

Al final, en tres líneas: qué revisaste, cómo lo probaste (navegador, celular emulado o real) y qué no pudiste revisar.
