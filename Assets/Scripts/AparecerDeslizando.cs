using System.Collections;
using UnityEngine;

/// <summary>
/// Hace aparecer un elemento de UI combinando dos cosas a la vez:
/// se desvanece desde transparente y se desliza hasta su posicion final.
///
/// La posicion final es la que tenga el objeto en el editor. El componente
/// lo mueve hacia arriba al empezar y lo trae de vuelta.
/// </summary>
[RequireComponent(typeof(CanvasGroup))]
[RequireComponent(typeof(RectTransform))]
public class AparecerDeslizando : MonoBehaviour
{
    [Tooltip("Cuanto dura la animacion completa.")]
    [SerializeField] private float duracion = 0.8f;

    [Tooltip("Cuantas unidades por encima de su sitio arranca. Positivo = cae desde arriba.")]
    [SerializeField] private float desplazamientoY = 20f;

    [Tooltip("Opacidad de la que parte. 0 = invisible, 0.3 = fantasma.")]
    [Range(0f, 1f)]
    [SerializeField] private float alfaInicial = 0f;

    private CanvasGroup grupo;
    private RectTransform rect;
    private Vector2 posicionFinal;

    private void Awake()
    {
        grupo = GetComponent<CanvasGroup>();
        rect = GetComponent<RectTransform>();

        // Se guarda ANTES de que nadie lo mueva: es la posicion que pusiste
        // en el editor y a la que hay que volver.
        posicionFinal = rect.anchoredPosition;
    }

    /// <summary>Deja el elemento invisible, desplazado y sin recibir clicks.</summary>
    public void Preparar()
    {
        if (grupo == null) Awake();

        grupo.alpha = alfaInicial;
        grupo.blocksRaycasts = false;
        grupo.interactable = false;
        rect.anchoredPosition = posicionFinal + Vector2.up * desplazamientoY;
    }

    /// <summary>Lanza la animacion de entrada.</summary>
    public void Aparecer(System.Action alTerminar = null)
    {
        StartCoroutine(Rutina(alTerminar));
    }

    private IEnumerator Rutina(System.Action alTerminar)
    {
        Vector2 posicionInicial = rect.anchoredPosition;
        float alfaDesde = grupo.alpha;
        float t = 0f;

        while (t < duracion)
        {
            t += Time.deltaTime;
            float p = Mathf.Clamp01(t / duracion);

            // Ease-out cubico: arranca rapido y frena al llegar. Una
            // interpolacion lineal se siente mecanica; esto se siente natural.
            float suave = 1f - Mathf.Pow(1f - p, 3f);

            grupo.alpha = Mathf.Lerp(alfaDesde, 1f, suave);
            rect.anchoredPosition = Vector2.Lerp(posicionInicial, posicionFinal, suave);

            yield return null;
        }

        grupo.alpha = 1f;
        rect.anchoredPosition = posicionFinal;
        grupo.blocksRaycasts = true;
        grupo.interactable = true;

        alTerminar?.Invoke();
    }
}
