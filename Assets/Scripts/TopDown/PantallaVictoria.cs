using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// La secuencia de victoria del laberinto, de principio a fin:
///
///   1. El juego se paraliza (de eso se encarga GestorTopDown antes de llamar).
///   2. Fundido a negro que tapa la arena.
///   3. Se escribe el texto letra por letra, con el mismo sonido de tecleo que
///      la narracion del inicio. El jugador pasa de pagina pulsando una tecla.
///   4. Confirmada la ultima pagina, el gatito cruza la pantalla caminando de
///      izquierda a derecha hasta salirse de cuadro.
///   5. Se carga el siguiente minijuego, todavia en negro.
///
/// NO SE REINVENTA NADA: reutiliza Desvanecedor y MaquinaDeEscribir, que son
/// los mismos componentes que usa HistoriaInicio. La animacion del gatito va
/// con AnimacionImagenUI porque aqui vive dentro de un Canvas.
///
/// POR QUE NO SE USA Time.timeScale = 0 PARA CONGELAR:
/// seria lo primero que uno piensa, pero rompe esta misma pantalla. Las
/// corrutinas de MaquinaDeEscribir y Desvanecedor esperan con WaitForSeconds
/// y Time.deltaTime, que van con el tiempo escalado: a timeScale 0 el texto
/// no se escribiria nunca y el fundido se quedaria a medias. Por eso
/// GestorTopDown paraliza apagando scripts, no tocando el reloj.
///
/// Montaje: mira GUIA-VICTORIA.md.
/// </summary>
public class PantallaVictoria : MonoBehaviour
{
    [Header("Piezas de la pantalla")]
    [Tooltip("El panel entero, con el fondo negro y todo lo de dentro. " +
             "Empieza APAGADO en la escena.")]
    [SerializeField] private GameObject panel;

    [Tooltip("El Desvanecedor del fondo negro. Es quien hace el fundido.")]
    [SerializeField] private Desvanecedor fundidoNegro;

    [Tooltip("El gatito que cruza la pantalla al final. Permanece apagado " +
             "durante todo el texto y solo sale para el paseo.")]
    [SerializeField] private GameObject gatito;

    [Tooltip("La MaquinaDeEscribir del texto de victoria. En su Inspector marca " +
             "Avanzar Con Tecla y deja Comenzar Al Activarse DESACTIVADO.")]
    [SerializeField] private MaquinaDeEscribir texto;

    [Header("Ritmo")]
    [Tooltip("Segundos de negro limpio antes de que arranque el texto.")]
    [SerializeField] private float pausaEnNegro = 0.5f;

    [Tooltip("Segundos entre el negro y la primera letra.")]
    [SerializeField] private float pausaAntesDelTexto = 0.8f;

    [Header("Musica")]
    [Tooltip("Segundos que tarda la musica del laberinto en apagarse, a la vez " +
             "que la pantalla se va a negro. Se apaga AQUI y no al cambiar de " +
             "escena porque toda la secuencia de victoria pasa dentro de la " +
             "misma escena: si se esperara al LoadScene, la cancion seguiria " +
             "sonando durante todo el texto. Pon 0 para no tocar la musica.")]
    [SerializeField] private float fadeMusica = 2f;

    [Header("El paseo final")]
    [Tooltip("Lo que tarda el gatito en cruzar la pantalla entera, de borde a borde.")]
    [SerializeField] private float duracionPaseo = 3f;

    [Tooltip("Marcalo si el sprite del gatito mira a la izquierda y quieres que " +
             "camine mirando hacia donde va.")]
    [SerializeField] private bool voltearGatito = false;

    [Tooltip("Segundos de negro despues de que el gatito sale, antes de cargar.")]
    [SerializeField] private float pausaAntesDeCargar = 0.6f;

    [Header("A donde va")]
    [Tooltip("Nombre exacto de la escena del siguiente minijuego. Tiene que " +
             "estar en File > Build Profiles > Scene List.")]
    [SerializeField] private string escenaSiguiente = "JuegoCatcher";

    private bool yaSeLanzo;

    private void Awake()
    {
        // Por si quedo encendido en el editor despues de montarlo.
        if (panel != null) panel.SetActive(false);
    }

    private void OnDestroy()
    {
        // Siempre desuscribirse: si no, el evento sigue apuntando a este
        // objeto cuando ya cargamos la escena siguiente y Unity tira excepcion.
        if (texto != null) texto.AlTerminar -= Continuar;
    }

    /// <summary>
    /// Arranca toda la secuencia. Lo llama GestorTopDown al ganar.
    /// </summary>
    public void Mostrar()
    {
        // Guarda por si dos cosas cantan victoria a la vez: la secuencia no
        // se puede lanzar dos veces o el texto se reiniciaria a media pagina.
        if (yaSeLanzo) return;
        yaSeLanzo = true;

        // Trampa clasica de Unity: StartCoroutine no arranca nada si el objeto
        // que la lanza esta apagado, y no avisa. Este script va en un objeto
        // ENCENDIDO; el que empieza apagado es el panel, que es hijo suyo.
        if (!gameObject.activeInHierarchy)
        {
            Debug.LogError("[PantallaVictoria] Este script esta en un GameObject apagado, " +
                           "asi que la secuencia no puede correr. Ponlo en uno encendido " +
                           "y deja apagado solo el Panel. Cargo la escena siguiente directo.", this);
            SceneManager.LoadScene(escenaSiguiente);
            return;
        }

        if (panel == null)
        {
            Debug.LogError("[PantallaVictoria] Falta asignar el Panel. " +
                           "Cargo la escena siguiente directo.", this);
            SceneManager.LoadScene(escenaSiguiente);
            return;
        }

        StartCoroutine(Secuencia());
    }

    private IEnumerator Secuencia()
    {
        panel.SetActive(true);

        // Todo apagado menos el negro: el gatito y el texto entran despues.
        if (gatito != null) gatito.SetActive(false);

        // La musica se va con la imagen, no despues.
        if (fadeMusica > 0f) MusicaFondo.Apagar(fadeMusica);

        // El fundido arranca desde transparente, si no el corte a negro seria seco.
        if (fundidoNegro != null)
        {
            fundidoNegro.PonerAlfa(0f);

            bool listo = false;
            fundidoNegro.Aparecer(() => listo = true);
            while (!listo) yield return null;
        }

        if (pausaEnNegro > 0f) yield return new WaitForSeconds(pausaEnNegro);
        if (pausaAntesDelTexto > 0f) yield return new WaitForSeconds(pausaAntesDelTexto);

        if (texto == null)
        {
            Debug.LogWarning("[PantallaVictoria] No hay MaquinaDeEscribir asignada.", this);
            yield break;
        }

        // Orden importante, el mismo que usa MenuPrincipal: primero suscribirse
        // y SOLO ENTONCES arrancar. Si se hiciera al reves y el texto fuera muy
        // corto, podria terminar antes de que nadie estuviera escuchando y la
        // pantalla se quedaria colgada para siempre.
        texto.AlTerminar += Continuar;
        texto.Comenzar();
    }

    /// <summary>
    /// Lo llama MaquinaDeEscribir cuando el jugador confirma la ultima pagina.
    /// Para entonces el texto ya se fundio a 0 el solo, asi que la pantalla
    /// esta en negro limpio y el gatito tiene el escenario para el.
    /// </summary>
    private void Continuar()
    {
        texto.AlTerminar -= Continuar;
        StartCoroutine(PaseoYSalida());
    }

    private IEnumerator PaseoYSalida()
    {
        if (gatito != null)
        {
            gatito.SetActive(true);
            yield return Pasear();
            gatito.SetActive(false);
        }

        if (pausaAntesDeCargar > 0f) yield return new WaitForSeconds(pausaAntesDeCargar);

        // Se carga con la pantalla todavia en negro. El corte no se nota
        // porque el Catcher empieza dibujando su propio fondo encima.
        SceneManager.LoadScene(escenaSiguiente);
    }

    /// <summary>
    /// Cruza al gatito de lado a lado, entrando y saliendo por fuera de los
    /// bordes para que no se le vea aparecer ni desaparecer de golpe.
    /// </summary>
    private IEnumerator Pasear()
    {
        var rect = gatito.GetComponent<RectTransform>();
        if (rect == null)
        {
            Debug.LogWarning("[PantallaVictoria] El gatito no es un objeto de UI, " +
                             "no puedo pasearlo.", this);
            yield break;
        }

        // El recorrido se mide contra el CANVAS, no contra el panel que
        // contiene al gatito.
        //
        // Podria parecer mas natural usar el panel, pero es fragil: si al
        // montarlo queda con un rect pequeno o descolocado (cosa facil al
        // copiar y pegar entre escenas con distinto Render Mode), el gatito
        // cruzaria solo ese trocito y se pararia a medio camino. El canvas
        // siempre mide la pantalla entera, pase lo que pase con el panel.
        //
        // Tampoco se usa Screen.width: el Canvas Scaler ya normaliza todo a la
        // resolucion de referencia, asi que el paseo dura lo mismo en
        // cualquier monitor.
        var padre = rect.parent as RectTransform;
        var canvas = gatito.GetComponentInParent<Canvas>();
        var lienzo = canvas != null ? canvas.rootCanvas.transform as RectTransform : null;

        float medioAncho;
        float centroDelPadre = 0f;

        if (lienzo != null && padre != null)
        {
            medioAncho = lienzo.rect.width * 0.5f;

            // Donde cae el centro del panel dentro del canvas. Como
            // anchoredPosition se mide desde ahi, hay que restarlo para que el
            // gatito cruce la pantalla y no el panel.
            centroDelPadre = lienzo.InverseTransformPoint(padre.position).x;
        }
        else
        {
            medioAncho = padre != null ? padre.rect.width * 0.5f : 160f;
        }

        // Una anchura de gatito de mas, para que arranque y termine del todo
        // fuera de cuadro en vez de asomando media cabeza.
        float borde = medioAncho + rect.rect.width;

        // anchoredPosition se mide desde el centro del panel, pero los bordes
        // los calculamos desde el centro del canvas. Restando el desfase se
        // pasa de un sistema al otro.
        float xInicio = -borde - centroDelPadre;
        float xFin = borde - centroDelPadre;

        if (voltearGatito)
        {
            var escala = rect.localScale;
            escala.x = -Mathf.Abs(escala.x);
            rect.localScale = escala;
        }

        float y = rect.anchoredPosition.y;
        float t = 0f;

        // Velocidad constante a proposito: es un gatito caminando, no un
        // elemento de UI. Un ease-out haria que frenara al llegar al borde.
        while (t < duracionPaseo)
        {
            t += Time.deltaTime;
            float p = Mathf.Clamp01(t / duracionPaseo);
            rect.anchoredPosition = new Vector2(Mathf.Lerp(xInicio, xFin, p), y);
            yield return null;
        }
    }
}
