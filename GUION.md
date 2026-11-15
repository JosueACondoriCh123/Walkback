# Walkback · guion de 3 minutos


## 0:00–0:20 · Problema

Imaginen que salen del hábitat en Marte. Cada paso consume oxígeno, y una pendiente cambia el esfuerzo de regreso. Antes de seguir hacia una muestra, necesitan responder una pregunta: ¿todavía puedo volver? Para ayudar a tomar esa decisión construimos Walkback.


## 0:20–0:50 · Solución

Walkback es un prototipo de planificación de caminatas en realidad mixta para Meta Quest 3. Ponemos un kilómetro cuadrado real de Gale sobre la mesa, con relieve e imágenes de HiRISE. El plan conecta cuatro sitios donde perforó Curiosity y estima tiempo y oxígeno considerando el terreno y el trabajo en cada parada.
## Nota del presentador

La animación ámbar es ilustrativa: conserva la ruta por distancia del pitch original. No atribuirle tiempos de la app, cuyo cálculo provisional optimiza tiempo.


## 0:50–1:35 · Secuencia de demostración, 45 segundos

**0–15 s:** Primero vemos el recorrido y su veredicto estimado.
**15–30 s:** Ahora reducimos el oxígeno disponible: el escenario muestra dónde deja de alcanzar para volver conservando la reserva.
**30–45 s:** En modo misión, la ruta aparece sobre el suelo y el visor muestra el regreso estimado mientras avanzamos.
## Material de revisión

Las imágenes son capturas del editor incluidas en el pitch original; no documentan la ejecución del WHAT IF ni el modo misión actual en Quest. La tercera es una vista de la ruta, no una captura del HUD. Reemplazar por una grabación de 45 segundos del build comprobado antes de defender. La secuencia es manual: D cambia la imagen; para ensayarla con cambios cada 15 segundos, pulsa P. Si se usa esta vista previa ante terceros, explicar la procedencia de las capturas.


## 1:35–2:20 · Evidencia y límites

La base es verificable: elevación de un metro, fotografías de aproximadamente veinticinco centímetros por píxel y paradas ubicadas con posiciones reales del rover. Contrastamos las pendientes con registros de Curiosity en ochenta y cinco posiciones. Esa comparación respalda el terreno, pero no valida el consumo humano de oxígeno. El cálculo actual es provisional y está identificado como estimación. Además, el relieve no resuelve todos los obstáculos ni los huecos con arena. Por eso el prototipo ayuda a evaluar decisiones y todavía necesita validación para un uso operativo.
## Fuentes

DATA.md; ReturnFallback.cs; MissionFallback.cs; HudEstimate.cs. El umbral de 20° es la configuración del prototipo, no una garantía de seguridad.


## 2:20–2:55 · Impacto y cierre

El objetivo es que un equipo pueda comparar salidas científicas y reconocer cuándo una parada compromete el regreso. El siguiente paso es integrar el modelo definitivo de caminata y oxígeno, comprobar el recorrido completo en el visor y evaluar si las personas interpretan bien las alertas. Somos MARSXR. Con Walkback queremos que cada decisión de explorar mantenga visible el camino de vuelta. The safest way to go far. Gracias.
## 2:55–3:00

Dejar el QR en pantalla. No añadir otra explicación.