using System.Collections;
using TMPro;
using UnityEngine;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

/// <summary>
/// Escribe uno o varios textos caracter por caracter, como una maquina de
/// escribir, con sonido. Entre pagina y pagina hace un fundido a negro del
/// texto. Avisa con el evento AlTerminar cuando acaba la ultima.
///
/// Para varias pantallas de narracion NO hace falta crear mas objetos de
/// texto: se agregan elementos al array Paginas.
/// </summary>
[RequireComponent(typeof(TMP_Text))]
public class MaquinaDeEscribir : MonoBehaviour
{
    [Header("Texto")]
    [Tooltip("Cada elemento es una pantalla de narracion. Se muestran en orden.")]
    [SerializeField, TextArea(3, 8)] private string[] paginas;

    [Tooltip("Segundos entre caracter y caracter. 0.05 es un ritmo comodo de lectura.")]
    [SerializeField] private float segundosPorCaracter = 0.05f;

    [Header("Ritmo")]
    [Tooltip("Segundos que se queda la pagina completa en pantalla antes de irse.")]
    [SerializeField] private float esperaAlTerminarPagina = 1.5f;

    [Tooltip("En vez de esperar por tiempo, espera a que el jugador pulse algo.")]
    [SerializeField] private bool avanzarConTecla = false;

    [Tooltip("Duracion del fundido del texto entre pagina y pagina.")]
    [SerializeField] private float duracionFundido = 0.6f;

    [Header("Sonido")]
    [SerializeField] private AudioSource fuenteAudio;
    [SerializeField] private AudioClip sonidoTecla;

    [Tooltip("ACTIVADO: el clip suena en bucle mientras escribe (para clips largos). " +
             "DESACTIVADO: se dispara cada N caracteres (necesita un clip cortisimo).")]
    [SerializeField] private bool sonidoContinuo = true;

    [Tooltip("Solo si Sonido Continuo esta desactivado.")]
    [SerializeField] private int cadaCuantosCaracteresSuena = 2;

    [Header("Interaccion")]
    [Tooltip("Pulsar una tecla completa la pagina que se esta escribiendo.")]
    [SerializeField] private bool sePuedeSaltar = true;

    [Tooltip("Arrancar solo al activarse el objeto. DEJALO DESACTIVADO si quien " +
             "manda es MenuPrincipal: el llama a Comenzar() cuando toca. Si arranca " +
             "solo, puede terminar antes de que nadie se haya suscrito a AlTerminar.")]
    [SerializeField] private bool comenzarAlActivarse = false;

    /// <summary>Se dispara cuando termino la ultima pagina.</summary>
    public event System.Action AlTerminar;

    private TMP_Text campo;
    private Coroutine rutina;
    private bool escribiendo;
    private bool saltarPaginaActual;

    private void Awake()
    {
        campo = GetComponent<TMP_Text>();
    }

    private void OnEnable()
    {
        if (comenzarAlActivarse) Comenzar();
    }

    private void OnDisable()
    {
        if (rutina != null) StopCoroutine(rutina);
        DetenerSonido();
        escribiendo = false;
    }

    /// <summary>
    /// Arranca la narracion desde la primera pagina.
    /// Lo llama MenuPrincipal despues de suscribirse a AlTerminar.
    /// </summary>
    public void Comenzar()
    {
        if (paginas == null || paginas.Length == 0)
        {
            Debug.LogWarning($"[{name}] MaquinaDeEscribir no tiene paginas. " +
                             "Rellena el array 'Paginas' en el Inspector.", this);
        }

        if (rutina != null) StopCoroutine(rutina);
        rutina = StartCoroutine(Narrar());
    }

    private void Update()
    {
        if (sePuedeSaltar && escribiendo && HuboPulsacion())
            saltarPaginaActual = true;
    }

    private IEnumerator Narrar()
    {
        if (paginas == null || paginas.Length == 0)
        {
            AlTerminar?.Invoke();
            yield break;
        }

        for (int p = 0; p < paginas.Length; p++)
        {
            yield return EscribirPagina(paginas[p]);

            // Pausa para que se pueda leer
            if (avanzarConTecla)
            {
                yield return null;                       // evita comerse la misma pulsacion del salto
                while (!HuboPulsacion()) yield return null;
            }
            else
            {
                yield return new WaitForSeconds(esperaAlTerminarPagina);
            }

            // El texto se funde siempre, incluida la ultima pagina. Asi la
            // transicion queda en orden: texto se va -> pantalla negra ->
            // (MenuPrincipal funde el negro) -> menu.
            yield return FundirTexto(1f, 0f);
        }

        AlTerminar?.Invoke();
    }

    private IEnumerator EscribirPagina(string textoPagina)
    {
        escribiendo = true;
        saltarPaginaActual = false;

        campo.alpha = 1f;
        campo.text = textoPagina;
        campo.ForceMeshUpdate();                        // necesario para que characterCount sea valido
        int total = campo.textInfo.characterCount;
        campo.maxVisibleCharacters = 0;

        IniciarSonido();

        for (int i = 1; i <= total; i++)
        {
            if (saltarPaginaActual)
            {
                campo.maxVisibleCharacters = total;
                break;
            }

            campo.maxVisibleCharacters = i;

            if (!sonidoContinuo && sonidoTecla != null && fuenteAudio != null &&
                i % Mathf.Max(1, cadaCuantosCaracteresSuena) == 0)
            {
                fuenteAudio.PlayOneShot(sonidoTecla);
            }

            yield return new WaitForSeconds(segundosPorCaracter);
        }

        DetenerSonido();
        escribiendo = false;
        saltarPaginaActual = false;
    }

    private IEnumerator FundirTexto(float desde, float hasta)
    {
        if (duracionFundido <= 0f)
        {
            campo.alpha = hasta;
            yield break;
        }

        float t = 0f;
        while (t < duracionFundido)
        {
            t += Time.deltaTime;
            campo.alpha = Mathf.Lerp(desde, hasta, t / duracionFundido);
            yield return null;
        }
        campo.alpha = hasta;
    }

    private void IniciarSonido()
    {
        if (!sonidoContinuo || fuenteAudio == null || sonidoTecla == null) return;
        fuenteAudio.clip = sonidoTecla;
        fuenteAudio.loop = true;
        fuenteAudio.Play();
    }

    private void DetenerSonido()
    {
        if (!sonidoContinuo || fuenteAudio == null) return;
        fuenteAudio.loop = false;
        fuenteAudio.Stop();
    }

    /// <summary>Termina toda la narracion de golpe. Util para un boton "Saltar".</summary>
    public void SaltarTodo()
    {
        if (rutina != null) StopCoroutine(rutina);
        DetenerSonido();
        escribiendo = false;
        campo.maxVisibleCharacters = int.MaxValue;
        AlTerminar?.Invoke();
    }

    private bool HuboPulsacion()
    {
#if ENABLE_INPUT_SYSTEM
        bool tecla = Keyboard.current != null && Keyboard.current.anyKey.wasPressedThisFrame;
        bool click = Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame;
        return tecla || click;
#elif ENABLE_LEGACY_INPUT_MANAGER
        return Input.anyKeyDown || Input.GetMouseButtonDown(0);
#else
        return false;
#endif
    }
}
