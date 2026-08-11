using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Anima un Image de UI recorriendo un arreglo de sprites, igual que hace
/// AnimacionSprites con un SpriteRenderer.
///
/// POR QUE HACE FALTA UN SCRIPT APARTE:
/// AnimacionSprites pide un SpriteRenderer, que vive en el mundo 2D. Un Canvas
/// en Screen Space - Overlay se dibuja SIEMPRE por encima de todo lo del
/// mundo, asi que un SpriteRenderer nunca podria verse sobre el panel negro
/// de la victoria. Dentro de un Canvas el que pinta es Image, y ese no lo
/// mueve AnimacionSprites.
///
/// Montaje: en el GameObject del gatito de la pantalla de victoria, que ya
/// tiene un Image, agrega este componente y arrastra los frames gatito_0..N.
/// </summary>
[RequireComponent(typeof(Image))]
public class AnimacionImagenUI : MonoBehaviour
{
    [Header("Animacion")]
    [Tooltip("Frames en orden. Arrastra gatito_0, gatito_1, ... desde gatito.png.")]
    [SerializeField] private Sprite[] frames;

    [Tooltip("Cuadros por segundo. Para pixel art, entre 6 y 12 se ve bien.")]
    [SerializeField] private float framesPorSegundo = 8f;

    [Tooltip("Si esta desmarcado, se reproduce una vez y se queda en el ultimo frame.")]
    [SerializeField] private bool enBucle = true;

    private Image imagen;
    private float temporizador;
    private int indice;
    private bool terminada;

    private void Awake()
    {
        imagen = GetComponent<Image>();
    }

    // Cada vez que se enciende el objeto la animacion empieza de cero. Esto
    // importa aqui: el panel de victoria arranca apagado y se activa al ganar.
    private void OnEnable()
    {
        indice = 0;
        temporizador = 0f;
        terminada = false;

        if (frames != null && frames.Length > 0) imagen.sprite = frames[0];
    }

    private void Update()
    {
        if (frames == null || frames.Length <= 1 || terminada) return;

        float duracionFrame = 1f / Mathf.Max(0.01f, framesPorSegundo);
        temporizador += Time.deltaTime;

        // while y no if: si hay un tiron de FPS puede tocar avanzar varios frames.
        while (temporizador >= duracionFrame)
        {
            temporizador -= duracionFrame;
            indice++;

            if (indice < frames.Length) continue;

            if (enBucle)
            {
                indice = 0;
            }
            else
            {
                indice = frames.Length - 1;
                terminada = true;
                break;
            }
        }

        imagen.sprite = frames[indice];
    }
}
