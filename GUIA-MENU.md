# Guía paso a paso: escena de inicio (narración + menú)

Construye `HistoriaInicio` hasta dejarla jugable: narración con máquina de
escribir y sonido, fade, y menú con cielo, nubes, gatito corriendo y botones.

Los scripts ya están en `Assets/Scripts/`:
`MaquinaDeEscribir.cs`, `DesplazarEnBucle.cs`, `MenuPrincipal.cs`.

---

## Paso 0 — Dejar una sola escena cargada

En el Hierarchy tienes `HistoriaInicio` y `JuegoTopDown` a la vez. Por eso sale
el warning de *"There are 2 audio listeners"*.

Click derecho sobre `JuegoTopDown` → **Remove Scene**. Trabaja siempre con una.

---

## Paso 1 — La cámara

Selecciona `Main Camera`:

| Campo | Valor | Por qué |
|---|---|---|
| Projection | Orthographic | Es 2D |
| Background Type | **Solid Color** | El cielo va a ser este color, no una imagen |
| Background | azul pastel (ej. `#8CD3F0`) | Sácalo con el cuentagotas de tu tileset de nubes para que combine |
| Size | 5 | 10 unidades de alto visibles |

Luego **Add Component → Pixel Perfect Camera**:

- Assets Pixels Per Unit: **16**
- Reference Resolution: **320 × 180**
- Marca **Upscale Render Texture** y **Pixel Snapping**

Sin esto, al moverse las nubes los píxeles vibran y se deforman.

---

## Paso 2 — Los tiles del suelo

### 2.1 Crear la paleta

`Window → 2D → Tile Palette`. Si el menú no existe, instala **2D Tilemap Editor**
desde Package Manager → Unity Registry.

En la ventana: **Create New Palette** → nombre `PaletaMenu` → guardar en una
carpeta nueva `Assets/Tiles/`.

### 2.2 Meter los sprites

Arrastra `Assets/Sprites/Tiles/ground_dirt_orange.png` desde el Project a la
ventana de la paleta. Unity pide carpeta destino: `Assets/Tiles/` otra vez.

Ahí genera un archivo `.asset` por cada sprite. Son cientos — es normal.

Repite con `Cloud Tileset.png` si quieres pintar nubes; si las vas a mover con
script, mejor déjalas como sprites sueltos (ver paso 4).

### 2.3 Crear el Tilemap en la escena

`GameObject → 2D Object → Tilemap → Rectangular`.

Unity crea dos objetos anidados:

```
Grid                  <- define el tamaño de celda
  └ Tilemap           <- donde se pintan los tiles
```

Renombra el `Tilemap` hijo a `TilemapSuelo`. En su componente **Tilemap Renderer**:

- Sorting Layer: **Fondo** (créalo si no existe, debe ir antes que `Personajes`)

En el **Grid** padre, Cell Size debe ser `1, 1, 0`. Como tus sprites son PPU 16,
cada tile de 16 px mide exactamente 1 unidad y encaja en la celda.

### 2.4 Pintar

En la ventana Tile Palette elige el pincel (icono de brocha), selecciona un tile,
y pinta en la vista Scene una franja horizontal en la parte de abajo.

**Componentes que NO necesita el suelo del menú:** ningún collider. El gatito del
menú no colisiona con nada, solo se desplaza. Los colliders son para el laberinto.

---

## Paso 3 — Panel de narración

### 3.1 Estructura

Sobre el `Canvas` que ya tienes:

1. Selecciona el `Canvas` → componente Canvas → **Render Mode: Screen Space -
   Camera** → arrastra `Main Camera` a **Render Camera**.
2. Click derecho sobre Canvas → **Create Empty** → nómbralo `PanelNarracion`.
3. Arrastra tu `TextoInicio` dentro de `PanelNarracion`.
4. Click derecho sobre `PanelNarracion` → **UI → Image** → nómbrala `FondoNegro`,
   Color negro, y estira sus anchors a pantalla completa (en el widget de anchors,
   la opción de abajo a la derecha con Alt presionado).

> **El orden en el Hierarchy importa y es contraintuitivo.** En UI de Unity, los
> hermanos que están **más abajo en la lista se dibujan encima**. `FondoNegro`
> tiene que quedar **por encima de `TextoInicio`** en el Hierarchy para dibujarse
> *detrás* de él. Si lo dejas debajo, el fondo tapa el texto y parece que no
> funciona nada.
>
> ```
> PanelNarracion
>   ├ FondoNegro     <- primero = se dibuja detras
>   └ TextoInicio    <- despues = se dibuja encima
> ```

### 3.2 El texto

En `TextoInicio` (TextMeshPro - Text):

- Font Asset: tu `PressStart2P` con Extended ASCII
- Font Size: ~14
- Font Style: **Bold desactivado**
- Alignment: centrado horizontal y vertical
- Color: blanco

### 3.3 El script y el sonido

1. En `PanelNarracion` → **Add Component → Audio Source**. Desmarca **Play On
   Awake** (el sonido lo dispara el script, no el arranque).
2. En `TextoInicio` → **Add Component → Maquina De Escribir**.
3. Rellena en el Inspector:
   - **Texto**: tu narración completa
   - **Segundos Por Caracter**: `0.05`
   - **Espera Al Terminar**: `1.5`
   - **Fuente Audio**: arrastra el `PanelNarracion` (el que tiene el Audio Source)
   - **Sonido Tecla**: `typing-sound-idragon-studio`
   - **Sonido Continuo**: ✅ **activado**

### Los dos modos de sonido

`typing-sound-idragon-studio.mp3` dura **8.1 segundos**. Es una grabación de
tecleo continuo, no una pulsación suelta.

- **Sonido Continuo activado** → el clip suena en bucle mientras se escribe y se
  corta al terminar. Es lo correcto para este clip.
- **Sonido Continuo desactivado** → dispara el clip cada N caracteres. Solo sirve
  con clips de menos de ~0.2 s. Si lo usas con el mp3 largo, lanzas 80 copias
  solapadas y suena a estática.

Si prefieres el modo por tecla, el único clip corto que tienes es
`impactWood_medium_001.ogg` (0.33 s). Los `.wav` de Sprout Lands también duran
8 segundos, así que tampoco sirven sueltos.

---

## Paso 4 — Panel de menú

Click derecho sobre Canvas → **Create Empty** → `PanelMenu`. Dentro:

- **UI → Text (TMP)** → el título del juego, fuente grande
- **UI → Button - TextMeshPro** ×2 o ×3 → `Iniciar juego`, `Niveles`, `Salir`

### Las nubes y el gatito (fuera del Canvas)

Estos van en el **mundo**, no en el Canvas, porque son sprites que se mueven en
unidades de mundo:

1. Arrastra un sprite de nube del `Cloud Tileset` a la escena.
2. Sorting Layer: `Fondo`, Order in Layer: 1 (encima del suelo).
3. **Add Component → Desplazar En Bucle**:
   - Velocidad: `-0.4` (negativa = hacia la izquierda, lento)
   - Límite Izquierdo: `-12`, Límite Derecho: `12`
4. Duplica la nube 3 o 4 veces (Cmd+D), cámbiales la Y, la escala y la velocidad
   para que no se muevan en bloque.

Para el gatito: arrastra tu prefab `Gatito` a la escena, ponlo sobre la franja de
tierra, y agrégale también **Desplazar En Bucle** con velocidad positiva (`2`).
Ya trae `AnimacionSprites`, así que corre solo.

> Al prefab `Gatito` NO le agregues el script permanentemente: hazlo sobre la
> instancia de esta escena y dale **Overrides → Apply** solo si lo quieres en
> todos lados. Para el menú, déjalo como override local.

---

## Paso 4.5 — Agrupar el mundo del menú

El suelo, las nubes y el gatito viven en el mundo, no en el Canvas, así que el
fondo negro de la narración **no los tapa**. Hay que apagarlos aparte.

1. `GameObject → Create Empty` en la raíz → nómbralo `MundoMenu`.
2. Arrastra dentro de él el `Grid` (con su `TilemapSuelo`), las nubes y el gatito.

Queda así:

```
HistoriaInicio
  ├ Main Camera
  ├ Canvas
  │   ├ PanelNarracion   (FondoNegro + TextoInicio)
  │   └ PanelMenu        (titulo + botones)
  ├ MundoMenu            <- se apaga durante la narracion
  │   ├ Grid / TilemapSuelo
  │   ├ Nube1, Nube2, Nube3
  │   └ Gatito
  └ GestorMenu
```

Alternativa si prefieres no agrupar: sube el **Sorting Layer** del Canvas por
encima de `Fondo` y estira el `FondoNegro` a pantalla completa. Funciona, pero
seguirías renderizando cosas invisibles y es más frágil.

---

## Paso 4.6 — El fundido

1. Selecciona `PanelNarracion` → **Add Component → Canvas Group**.
2. **Add Component → Desvanecedor**. Duración: `1` segundo.

El `Desvanecedor` funde el panel entero — texto y fondo negro a la vez — así que
el menú aparece por detrás gradualmente.

Para el fundido **entre páginas** de la narración no hace falta nada más: eso lo
hace `MaquinaDeEscribir` con su campo **Duracion Fundido**, que solo afecta al
texto y deja el negro quieto.

---

## Paso 5 — Conectar todo

1. Crea un GameObject vacío en la raíz → `GestorMenu`.
2. **Add Component → Menu Principal**.
3. Rellena:
   - **Panel Narracion**: arrastra `PanelNarracion`
   - **Narracion**: arrastra `TextoInicio` (el que tiene MaquinaDeEscribir)
   - **Fundido Narracion**: arrastra `PanelNarracion` (el del Desvanecedor)
   - **Panel Menu**: arrastra `PanelMenu`
   - **Mundo Menu**: arrastra `MundoMenu`
   - **Escena Primer Minijuego**: `JuegoTopDown`
4. En el botón `Iniciar juego` → componente Button → **On Click ()** → `+` →
   arrastra `GestorMenu` → en el desplegable elige `MenuPrincipal → Jugar()`.
5. Igual con `Salir` → `MenuPrincipal → Salir()`.

Deja todo **activado** en el editor mientras lo montas; el script apaga y enciende
lo que toca al entrar en Play.

---

## Varias pantallas de narración

**No crees más objetos de texto ni un prefab.** `MaquinaDeEscribir` tiene un array
**Paginas**: pon el Size en 3 y escribe una parte de la historia en cada elemento.

Se muestran en orden, cada una con su animación y su sonido, con un fundido del
texto entre medias. El evento `AlTerminar` solo salta después de la última.

Dos campos controlan el ritmo:

- **Espera Al Terminar Pagina**: segundos que la página completa se queda en
  pantalla antes de irse.
- **Avanzar Con Tecla**: si lo activas, en vez de esperar por tiempo espera a que
  el jugador pulse. Más cómodo para textos largos.

---

## Paso 6 — Build Settings

`File → Build Profiles` (o Build Settings) → **Add Open Scenes** con cada escena
abierta, o arrástralas desde el Project.

El orden importa: la escena de índice 0 es la que arranca. `HistoriaInicio` debe
ser la primera.

---

## Paso 7 — Probar

Play. Deberías ver:

1. Pantalla negra, texto apareciendo letra por letra con sonido
2. Pausa de 1.5 s
3. Cambio al menú: cielo, nubes moviéndose, gatito corriendo, título y botones
4. "Iniciar juego" carga `JuegoTopDown`

Si algo falla, mira la Console. Los dos errores más probables:

- *"Scene 'JuegoTopDown' couldn't be loaded"* → falta agregarla a Build Settings
- El texto no aparece → el Canvas sigue en Screen Space - Overlay, o el panel
  quedó desactivado y sin referencia en `GestorMenu`

---

## Después de esto

El fade entre paneles (ahora es un corte seco), el `GameManager` con
`DontDestroyOnLoad` para llevar puntaje y dificultad entre escenas, y el selector
de dificultad del botón "Niveles".
