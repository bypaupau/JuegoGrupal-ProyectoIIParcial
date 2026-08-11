# Guía: la escena Final

Cómo montar la pantalla de victoria y engancharla al final del Catcher.

---

## La secuencia

```
1. el gatito entra caminando desde la izquierda y se para en el centro
2. aparece el cofre  (fade)
3. del cofre sube el pescadito  (fade + desplazamiento hacia arriba)
4. entra el texto VICTORIA
5. se muestran el puntaje y los botones
```

Cada paso **espera a que el anterior termine de verdad**, encadenado por
callback y no por temporizadores sueltos. Así, si mañana alargas el fade del
cofre, todo lo que viene detrás se corre solo en vez de encimarse.

---

## Por qué una escena y no un panel

Ya estaba decidido: `Final.unity` existe y está en **Build Settings**. Solo le
falta contenido — hoy dentro únicamente hay una Main Camera.

| | Qué es | Al terminar |
|---|---|---|
| `PantallaVictoria` (laberinto) | Una **transición** dentro de la escena | Carga `JuegoCatcher` |
| Panel de Game Over | Un **panel** dentro de la escena | Recarga o vuelve al menú |
| **`Final.unity`** | Un **destino** | Se queda esperando al jugador |

---

## Scripts nuevos

| Script | Para qué |
|---|---|
| `AparecerSprite.cs` | El gemelo de `AparecerDeslizando`, pero para sprites del mundo |
| `PantallaFinal.cs` | Coreografía la secuencia y expone los botones |

Hizo falta un `AparecerSprite` aparte porque son dos sistemas distintos: la UI
se atenúa con el alfa de un `CanvasGroup` y se mueve con `anchoredPosition`,
mientras que un sprite se atenúa con el color del `SpriteRenderer` y se mueve
con el `Transform`. Igual que en la UI, **la posición final es la que le pongas
en el editor**: el componente lo desplaza al empezar y lo trae de vuelta.

---

## Montaje

Abre `Assets/Scenes/Final.unity`.

### 1. Fondo y cámara

Para que la victoria se sienta parte del mismo mundo, reusa el fondo del menú:
copia de `HistoriaInicio` el objeto del cielo y el suelo (el Grid con los
Tilemaps), o al menos ponle a la **Main Camera** el mismo color de Background.

### 2. El gatito

1. Arrastra `Prefabs/Comunes/Gatito.prefab` a la escena.
2. **Colócalo fuera de pantalla por la izquierda** — de ahí arranca. Con la
   cámara ortográfica por defecto, algo como `x = -12` sirve.
3. Comprueba que tiene **Animacion Sprites** con los cuadros de caminata y
   `En Bucle ✓`.
4. Crea un `GameObject → Create Empty`, llámalo `DestinoGatito` y ponlo donde
   quieras que se pare (`x = 0` para el centro, un poco a la izquierda si
   quieres dejarle sitio al cofre).

### 3. El cofre

1. Arrastra `Sprites/Items/Golden_Chest.png` a la escena.
2. Colócalo **donde se tiene que ver** (a la derecha del gatito, apoyado en el
   suelo).
3. En su `SpriteRenderer`: **Sorting Layer → Objetos**, **Order in Layer → 1**.
4. Añádele el componente **Aparecer Sprite**:
   - Duracion `0.8`
   - Desplazamiento `(0, 0)` ← solo se atenúa, no se mueve
   - Alfa Inicial `0`

### 4. El pescadito

1. Arrastra `Sprites/Items/mediuml fish.png` a la escena.
2. Colócalo **en su posición final**: flotando por encima del cofre.
3. En su `SpriteRenderer`: **Sorting Layer → Objetos**, **Order in Layer → 0**.

   > ⚠️ **Order in Layer 0, menor que el del cofre.** Eso es lo que hace que
   > se vea salir *de dentro* del cofre y no por delante. Si te sale flotando
   > por encima de la madera, es este número.

4. Añádele **Aparecer Sprite**:
   - Duracion `0.9`
   - Desplazamiento `(0, -1)` ← **Y negativa**: arranca un tile más abajo
     (dentro del cofre) y sube hasta donde lo pusiste
   - Alfa Inicial `0`

Ajusta ese `-1` según lo alto que hayas puesto el pescadito: tiene que empezar
tapado por el cofre.

### 5. El texto VICTORIA

`GameObject → UI → Canvas` (Scale With Screen Size, 1920x1080), y dentro un
`Text - TextMeshPro`:

- Texto: `VICTORIA`
- Font Asset: **PressStart2P-Regular SDF**
- Material Preset: **PressStart2P-Regular SDF - Logo** (el del contorno y sombra)
- **Vertex Color: amarillo.** Prueba `#FFD84D` en vez de amarillo puro: el
  amarillo saturado sobre cielo azul vibra y cansa la vista.
- **Raycast Target: desmarcado**

Añádele **Canvas Group** y luego una de estas dos:

| Quieres | Componente | Campo del gestor |
|---|---|---|
| Que entre deslizándose | `Aparecer Deslizando` | *Aparicion Victoria* |
| Que se monte letra a letra, como el título del menú | `Logo Titulo` | *Logo Victoria* |

Asigna **solo uno de los dos**. Si pones el `LogoTitulo`, gana ese.

### 6. Puntaje y botones

En el mismo Canvas:

- Un `Text - TextMeshPro` llamado `TextoPuntaje`, **vacío** (lo rellena el
  script). Raycast Target desmarcado.
- Un objeto vacío `GrupoBotones` con dos `Button - TextMeshPro`: **MENU** y
  **SALIR**. Al `GrupoBotones` añádele **Canvas Group** + **Aparecer
  Deslizando** (duración `0.8`, desplazamiento Y `20`, alfa inicial `0`).

### 7. La música

Primero mete tu canción: arrástrala a `Assets/Audio/`. Con eso Unity la importa
y le crea su `.meta` — que **tiene que viajar con el archivo** al commitear
(CONVENCIONES §1).

Luego, en cualquier objeto de la escena, añade **Musica De Escena**:

- Musica → tu clip de victoria
- **Arrancar Sola → DESMARCADO** ← la dispara `PantallaFinal`
- Fade In `0.5` (una fanfarria de victoria quiere entrar rápido, no derretirse)

### 8. El gestor

`GameObject → Create Empty` llamado `GestorFinal`, con el componente
**Pantalla Final**. Asigna:

| Campo | Qué arrastrar |
|---|---|
| Gatito | el gatito de la escena |
| Destino Gatito | `DestinoGatito` |
| Velocidad Gatito | `2` |
| Animacion Gatito | el `AnimacionSprites` del gatito |
| Sprite Quieto | *(opcional)* el cuadro en el que se queda parado |
| Cofre | el `AparecerSprite` del cofre |
| Pescadito | el `AparecerSprite` del pescadito |
| Aparicion Victoria **o** Logo Victoria | el texto VICTORIA |
| Texto Puntaje | `TextoPuntaje` |
| Aparicion Botones | `GrupoBotones` |
| Musica Victoria | el `MusicaDeEscena` |
| Escena Menu | `HistoriaInicio` |

Y los botones, desde su `On Click ()`:

- **MENU** → `GestorFinal` → `PantallaFinal.VolverAlMenu ()`
- **SALIR** → `GestorFinal` → `PantallaFinal.Salir ()`

### 9. Guarda

`Cmd+S`, y confirma que `Final` sigue marcada en
`File → Build Profiles → Scene List`.

---

## Ajustar el ritmo

Todas las pausas están en el gestor. Valores de partida:

| Campo | Valor |
|---|---|
| Pausa Antes Del Cofre | `0.5` |
| Pausa Antes Del Pescadito | `0.3` |
| Pausa Antes Del Texto | `0.4` |
| Pausa Antes De Botones | `0.6` |

Con la caminata, la secuencia entera dura unos 5 segundos. Si se te hace larga,
lo primero que hay que acortar es la caminata: sube `Velocidad Gatito` o acerca
el `DestinoGatito`.

**Si el gatito parece que patina**, es que la velocidad no casa con su
animación: baja `Velocidad Gatito` o sube los `Frames Por Segundo` del
`AnimacionSprites`.

---

## Enganchar el Catcher con la escena Final

Este es el último cabo suelto. En `GestorCatcher.Ganar()` hay este comentario:

```csharp
// (Cargar la escena Final se deja pendiente, lo detallas luego.)
```

O sea que al ganar el Catcher se ve el "¡Ganaste!" y el juego se queda ahí.

**`GestorCatcher.cs` está en `Scripts/Catcher/`, que es de Daniel.** Avísale
antes de tocarlo. El cambio son tres líneas:

```csharp
[SerializeField] private string escenaFinal = "Final";

[Tooltip("Segundos que se queda el '¡Ganaste!' antes de ir a la escena Final.")]
[SerializeField] private float esperaAntesDelFinal = 2f;
```

Y en `MostrarGanaste()`:

```csharp
private void MostrarGanaste()
{
    if (mensajeGanaste != null)
        mensajeGanaste.SetActive(true);

    StartCoroutine(IrAlFinal());
}

private System.Collections.IEnumerator IrAlFinal()
{
    yield return new WaitForSeconds(esperaAntesDelFinal);
    SceneManager.LoadScene(escenaFinal);
}
```

Va en `MostrarGanaste()` y no en `Ganar()` a propósito: `Ganar()` lanza el
fundido a negro y el mensaje aparece **cuando ese fundido termina**. Si la
espera arrancara en `Ganar()`, correría en paralelo al fundido y el jugador
podría no llegar a leer el "¡Ganaste!".

---

## Probarlo sin jugar la aventura entera

- Abre `Final.unity` y dale Play directamente. El puntaje saldrá en `0` porque
  `Partida` arranca vacía, pero verás la secuencia completa. Es así como vas a
  querer iterar sobre los tiempos.
- Para probarla de verdad con puntaje: en `JuegoCatcher → GestorCatcher → Meta`
  pon `1` temporalmente. **Acuérdate de devolverlo a 10 / 15.**

---

## Checklist

- [ ] El pescadito tiene **menor** Order in Layer que el cofre
- [ ] El desplazamiento del pescadito tiene la **Y negativa**
- [ ] `Arrancar Sola` desmarcado en el `MusicaDeEscena`
- [ ] El `.meta` de la canción nueva entra en el commit
- [ ] `Raycast Target` desmarcado en los textos
- [ ] Los dos botones enganchados
- [ ] `Final` marcada en Build Profiles
- [ ] El gatito no patina al caminar
