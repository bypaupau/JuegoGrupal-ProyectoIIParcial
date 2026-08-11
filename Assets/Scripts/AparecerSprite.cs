using System.Collections;
using UnityEngine;

/// <summary>
/// El equivalente de AparecerDeslizando pero para sprites del mundo, no para UI.
///
/// Hace falta uno aparte porque son dos sistemas distintos: la UI se atenua con
/// el alfa de un CanvasGroup y se mueve con anchoredPosition, mientras que un
/// sprite se atenua con el color del SpriteRenderer y se mueve con el Transform.
///
/// Igual que AparecerDeslizando, la posicion final es la que tenga el objeto en
/// el editor: el componente lo desplaza al empezar y lo trae de vuelta. Asi
/// colocas las cosas donde se van a ver y no donde arrancan.
/// </summary>
[RequireComponent(typeof(SpriteRenderer))]
public class AparecerSprite : MonoBehaviour
{
    [Tooltip("Cuanto dura la aparicion completa.")]
    [SerializeField] private float duracion = 0.8f;

    [Tooltip("Desde donde entra, relativo a su sitio. (0, -0.5) = sube medio " +
             "tile; (0, 0) = solo se atenua, sin moverse.")]
    [SerializeField] private Vector2 desplazamiento = Vector2.zero;

    [Tooltip("Opacidad de la que parte. 0 = invisible.")]
    [Range(0f, 1f)]
    [SerializeField] private float alfaInicial = 0f;

    private SpriteRenderer render;
    private Vector3 posicionFinal;

    private void Awake()
    {
        render = GetComponent<SpriteRenderer>();

        // Se guarda ANTES de que nadie lo mueva.
        posicionFinal = transform.position;
    }

    /// <summary>Deja el sprite invisible y desplazado, listo para entrar.</summary>
    public void Preparar()
    {
        if (render == null) Awake();

        PonerAlfa(alfaInicial);
        transform.position = posicionFinal + (Vector3)desplazamiento;
    }

    /// <summary>Lanza la aparicion. Avisa por alTerminar cuando acaba.</summary>
    public void Aparecer(System.Action alTerminar = null)
    {
        StartCoroutine(Rutina(alTerminar));
    }

    /// <summary>Lo pone visible y en su sitio de golpe, sin animar.</summary>
    public void MostrarYa()
    {
        if (render == null) Awake();

        PonerAlfa(1f);
        transform.position = posicionFinal;
    }

    private IEnumerator Rutina(System.Action alTerminar)
    {
        Vector3 posicionInicial = transform.position;
        float alfaDesde = render.color.a;
        float t = 0f;

        while (t < duracion)
        {
            t += Time.deltaTime;
            float p = Mathf.Clamp01(t / duracion);

            // Mismo ease-out cubico que AparecerDeslizando, para que todo el
            // juego se sienta igual: entra rapido y frena al llegar.
            float suave = 1f - Mathf.Pow(1f - p, 3f);

            PonerAlfa(Mathf.Lerp(alfaDesde, 1f, suave));
            transform.position = Vector3.Lerp(posicionInicial, posicionFinal, suave);

            yield return null;
        }

        MostrarYa();
        alTerminar?.Invoke();
    }

    /// <summary>
    /// El alfa de un sprite vive en su color, y Color es un struct: hay que
    /// sacarlo, modificarlo y volver a asignarlo entero. Tocar
    /// render.color.a directamente no compila.
    /// </summary>
    private void PonerAlfa(float alfa)
    {
        Color c = render.color;
        c.a = Mathf.Clamp01(alfa);
        render.color = c;
    }
}
