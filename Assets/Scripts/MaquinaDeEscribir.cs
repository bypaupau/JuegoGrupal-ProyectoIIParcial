using System.Collections;
using TMPro;
using UnityEngine;

/// <summary>
/// Muestra un texto caracter por caracter, como una maquina de escribir,
/// reproduciendo un sonido cada cierto numero de letras.
/// Avisa con el evento AlTerminar cuando acaba.
/// </summary>
[RequireComponent(typeof(TMP_Text))]
public class MaquinaDeEscribir : MonoBehaviour
{
    [Header("Texto")]
    [SerializeField, TextArea(3, 10)] private string texto;

    [Tooltip("Segundos entre caracter y caracter. 0.05 es un ritmo comodo de lectura.")]
    [SerializeField] private float segundosPorCaracter = 0.05f;

    [Tooltip("Segundos de pausa al terminar de escribir, antes de avisar.")]
    [SerializeField] private float esperaAlTerminar = 1.5f;

    [Header("Sonido")]
    [SerializeField] private AudioSource fuenteAudio;
    [SerializeField] private AudioClip sonidoTecla;

    [Tooltip("Suena una vez cada N caracteres. Con 1 satura; 2 o 3 se oye mejor.")]
    [SerializeField] private int cadaCuantosCaracteresSuena = 2;

    /// <summary>Se dispara cuando el texto termino de escribirse.</summary>
    public event System.Action AlTerminar;

    private TMP_Text campo;
    private Coroutine rutina;
    private bool escribiendo;

    private void Awake()
    {
        campo = GetComponent<TMP_Text>();
    }

    private void OnEnable()
    {
        rutina = StartCoroutine(Escribir());
    }

    private void OnDisable()
    {
        if (rutina != null) StopCoroutine(rutina);
        escribiendo = false;
    }

    private void Update()
    {
        // Cualquier tecla o click salta la animacion. Detalle pequeno que
        // se agradece mucho cuando alguien vuelve a jugar.
        if (escribiendo && (Input.anyKeyDown || Input.GetMouseButtonDown(0)))
            Completar();
    }

    private IEnumerator Escribir()
    {
        escribiendo = true;

        campo.text = texto;
        campo.ForceMeshUpdate();                       // necesario para que characterCount sea valido
        int total = campo.textInfo.characterCount;
        campo.maxVisibleCharacters = 0;

        for (int i = 1; i <= total; i++)
        {
            campo.maxVisibleCharacters = i;

            if (sonidoTecla != null && fuenteAudio != null &&
                i % Mathf.Max(1, cadaCuantosCaracteresSuena) == 0)
            {
                fuenteAudio.PlayOneShot(sonidoTecla);
            }

            yield return new WaitForSeconds(segundosPorCaracter);
        }

        escribiendo = false;
        yield return new WaitForSeconds(esperaAlTerminar);
        AlTerminar?.Invoke();
    }

    /// <summary>Muestra el texto completo de golpe y avisa que termino.</summary>
    public void Completar()
    {
        if (rutina != null) StopCoroutine(rutina);
        escribiendo = false;
        campo.maxVisibleCharacters = int.MaxValue;
        AlTerminar?.Invoke();
    }
}
