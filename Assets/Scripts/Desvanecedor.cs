using System.Collections;
using UnityEngine;

/// <summary>
/// Funde un CanvasGroup entre transparente y opaco.
/// Se usa para la transicion de la narracion al menu.
/// </summary>
[RequireComponent(typeof(CanvasGroup))]
public class Desvanecedor : MonoBehaviour
{
    [SerializeField] private float duracion = 1f;

    private CanvasGroup grupo;

    private void Awake()
    {
        grupo = GetComponent<CanvasGroup>();
    }

    /// <summary>De opaco a transparente. Llama a alTerminar cuando acaba.</summary>
    public void Desvanecer(System.Action alTerminar = null)
    {
        StartCoroutine(Fundir(grupo.alpha, 0f, alTerminar));
    }

    /// <summary>De transparente a opaco.</summary>
    public void Aparecer(System.Action alTerminar = null)
    {
        StartCoroutine(Fundir(grupo.alpha, 1f, alTerminar));
    }

    /// <summary>Pone el alfa de golpe, sin animar.</summary>
    public void PonerAlfa(float valor)
    {
        if (grupo == null) grupo = GetComponent<CanvasGroup>();
        grupo.alpha = valor;
        grupo.blocksRaycasts = valor > 0.5f;
    }

    private IEnumerator Fundir(float desde, float hasta, System.Action alTerminar)
    {
        // Mientras se funde no queremos que intercepte clicks
        grupo.blocksRaycasts = false;

        float t = 0f;
        while (t < duracion)
        {
            t += Time.deltaTime;
            grupo.alpha = Mathf.Lerp(desde, hasta, t / duracion);
            yield return null;
        }

        grupo.alpha = hasta;
        grupo.blocksRaycasts = hasta > 0.5f;
        alTerminar?.Invoke();
    }
}
