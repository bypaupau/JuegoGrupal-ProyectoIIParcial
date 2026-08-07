using UnityEngine;

/// <summary>
/// Anima un SpriteRenderer recorriendo un arreglo de sprites a una velocidad fija.
/// Se puede cambiar de animacion en tiempo de ejecucion llamando a Reproducir().
/// </summary>
[RequireComponent(typeof(SpriteRenderer))]
public class AnimacionSprites : MonoBehaviour
{
    [Header("Animacion")]
    [Tooltip("Frames que se reproducen al iniciar. Arrastra aqui gatito_0, gatito_1, ...")]
    [SerializeField] private Sprite[] animacionInicial;

    [Tooltip("Cuadros por segundo. Para pixel art, entre 6 y 12 se ve bien.")]
    [SerializeField] private float framesPorSegundo = 8f;

    [Tooltip("Si esta desmarcado, la animacion se reproduce una sola vez y se queda en el ultimo frame.")]
    [SerializeField] private bool enBucle = true;

    private SpriteRenderer spriteRenderer;
    private Sprite[] animacionActual;
    private float temporizador;
    private int indice;
    private bool terminada;

    /// <summary>Frames de la animacion que se esta reproduciendo ahora.</summary>
    public Sprite[] AnimacionActual => animacionActual;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        animacionActual = animacionInicial;
    }

    private void Update()
    {
        if (animacionActual == null || animacionActual.Length == 0 || terminada)
            return;

        // Con un solo frame no hay nada que animar: lo pintamos y salimos.
        if (animacionActual.Length == 1)
        {
            spriteRenderer.sprite = animacionActual[0];
            return;
        }

        float duracionFrame = 1f / Mathf.Max(0.01f, framesPorSegundo);
        temporizador += Time.deltaTime;

        // while y no if: si hay un tiron de FPS puede tocar avanzar varios frames.
        while (temporizador >= duracionFrame)
        {
            temporizador -= duracionFrame;
            indice++;

            if (indice >= animacionActual.Length)
            {
                if (enBucle)
                {
                    indice = 0;
                }
                else
                {
                    indice = animacionActual.Length - 1;
                    terminada = true;
                    break;
                }
            }
        }

        spriteRenderer.sprite = animacionActual[indice];
    }

    /// <summary>
    /// Cambia la animacion en reproduccion. Si ya se esta reproduciendo esa
    /// misma, no hace nada (evita que se reinicie en cada frame del Update).
    /// </summary>
    public void Reproducir(Sprite[] nuevaAnimacion)
    {
        if (nuevaAnimacion == null || nuevaAnimacion.Length == 0) return;
        if (animacionActual == nuevaAnimacion) return;

        animacionActual = nuevaAnimacion;
        indice = 0;
        temporizador = 0f;
        terminada = false;
        spriteRenderer.sprite = animacionActual[0];
    }

    /// <summary>Reinicia la animacion actual desde el primer frame.</summary>
    public void Reiniciar()
    {
        indice = 0;
        temporizador = 0f;
        terminada = false;
    }
}
