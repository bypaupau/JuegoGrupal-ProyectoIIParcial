using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Cierre de la aventura. Va en la escena Final, a la que se llega despues de
/// ganar el ultimo minijuego.
///
/// A diferencia de PantallaVictoria, que es una TRANSICION dentro del laberinto
/// y termina cargando otra escena, esta pantalla es un DESTINO: monta su
/// secuencia y se queda esperando a que el jugador decida.
///
/// LA SECUENCIA
///   1. el gatito entra caminando desde la izquierda y se para en el centro
///   2. aparece el cofre
///   3. del cofre sale el pescadito, subiendo desde detras
///   4. entra el texto de VICTORIA
///   5. se muestran el puntaje y los botones
///
/// Cada paso espera a que el anterior termine de verdad (por callback), no por
/// temporizadores sueltos. Asi, si alargas un paso, los siguientes se corren
/// solos en vez de encimarse.
///
/// El puntaje NO se le pasa por parametro: lo lee de Partida, que es una clase
/// static y por eso sigue viva despues del cambio de escena.
/// </summary>
public class PantallaFinal : MonoBehaviour
{
    [Header("1. El gatito que entra caminando")]
    [Tooltip("El gatito. Colocalo en el editor FUERA de pantalla por la izquierda: " +
             "de ahi arranca.")]
    [SerializeField] private Transform gatito;

    [Tooltip("Un GameObject vacio en el punto donde se para. Ponlo hacia el centro.")]
    [SerializeField] private Transform destinoGatito;

    [Tooltip("Unidades por segundo. Ajustalo hasta que la caminata no patine: " +
             "si va muy rapido para su animacion, parece que se desliza.")]
    [SerializeField] private float velocidadGatito = 2f;

    [Tooltip("Su AnimacionSprites. Se apaga al llegar para que no siga " +
             "moviendo las patas parado.")]
    [SerializeField] private AnimacionSprites animacionGatito;

    [Tooltip("Opcional. El cuadro en el que se queda quieto al llegar. " +
             "Si lo dejas vacio se queda en el que le tocara.")]
    [SerializeField] private Sprite spriteQuieto;

    [Header("2. El cofre")]
    [SerializeField] private AparecerSprite cofre;

    [Tooltip("Segundos entre que el gatito se para y aparece el cofre.")]
    [SerializeField] private float pausaAntesDelCofre = 0.5f;

    [Header("3. El pescadito")]
    [Tooltip("Su AparecerSprite. Ponle un Desplazamiento con Y NEGATIVA para " +
             "que arranque dentro del cofre y suba al aparecer.")]
    [SerializeField] private AparecerSprite pescadito;

    [Tooltip("Segundos entre el cofre y el pescadito.")]
    [SerializeField] private float pausaAntesDelPescadito = 0.3f;

    [Header("4. El texto de victoria")]
    [Tooltip("El AparecerDeslizando del texto. Usa este O el LogoTitulo de " +
             "abajo, no los dos.")]
    [SerializeField] private AparecerDeslizando aparicionVictoria;

    [Tooltip("Alternativa: si prefieres que VICTORIA se monte letra a letra " +
             "como el titulo del menu, ponle un LogoTitulo y asignalo aqui.")]
    [SerializeField] private LogoTitulo logoVictoria;

    [Tooltip("Segundos entre el pescadito y el texto.")]
    [SerializeField] private float pausaAntesDelTexto = 0.4f;

    [Header("5. Puntaje y botones")]
    [SerializeField] private TMP_Text textoPuntaje;

    [Tooltip("Se le pega el numero detras.")]
    [SerializeField] private string formatoPuntaje = "Puntaje final: ";

    [Tooltip("El AparecerDeslizando del grupo de botones.")]
    [SerializeField] private AparecerDeslizando aparicionBotones;

    [Tooltip("Segundos entre el texto de victoria y los botones.")]
    [SerializeField] private float pausaAntesDeBotones = 0.6f;

    [Header("Musica")]
    [Tooltip("Un AudioSource normal con la fanfarria de victoria. " +
             "Play On Awake y Loop DESMARCADOS: la dispara este script y " +
             "suena una sola vez.")]
    [SerializeField] private AudioSource fanfarriaVictoria;

    [Tooltip("Segundos desde que arranca la escena hasta que suena. " +
             "0 = desde el primer frame.")]
    [SerializeField] private float retrasoMusica = 0f;

    [Tooltip("Apaga la musica de fondo que venia sonando del minijuego " +
             "anterior. Si no, se encima con la fanfarria.")]
    [SerializeField] private bool apagarMusicaDeFondo = true;

    [Tooltip("Lo que tarda en irse esa musica de fondo.")]
    [SerializeField] private float fadeOutFondo = 1f;

    [Header("Escenas")]
    [SerializeField] private string escenaMenu = "HistoriaInicio";

    private void Start()
    {
        // Todo lo que tiene que entrar se esconde en el MISMO frame en que
        // arranca la escena. Si se esperara a la corrutina se veria un fogonazo
        // de un frame con el cofre y los botones ya puestos.
        if (cofre != null) cofre.Preparar();
        if (pescadito != null) pescadito.Preparar();
        if (aparicionVictoria != null) aparicionVictoria.Preparar();
        if (logoVictoria != null) logoVictoria.Preparar();
        if (aparicionBotones != null) aparicionBotones.Preparar();

        if (textoPuntaje != null)
        {
            // Se lee UNA vez. No hace falta suscribirse a Partida.AlCambiarPuntaje:
            // en esta escena ya nadie suma puntos.
            textoPuntaje.text = formatoPuntaje + Partida.Puntaje;
        }

        // MusicaFondo vive fuera de las escenas (DontDestroyOnLoad), asi que la
        // cancion del Catcher seguiria sonando aqui encima de la fanfarria.
        // Apagar() es static y no falla aunque no haya ninguna sonando, asi que
        // tambien funciona si abres esta escena suelta para probarla.
        if (apagarMusicaDeFondo) MusicaFondo.Apagar(fadeOutFondo);

        if (fanfarriaVictoria != null) StartCoroutine(Fanfarria());

        StartCoroutine(Secuencia());
    }

    /// <summary>
    /// La victoria NO usa MusicaDeEscena a proposito.
    ///
    /// MusicaFondo es un reproductor de musica AMBIENTE: hace crossfade entre
    /// escenas y pone loop = true siempre, porque una cancion de fondo tiene
    /// que sonar sin fin. Una fanfarria de victoria es lo contrario: suena una
    /// vez, se acaba, y el silencio es parte del efecto.
    ///
    /// Por eso va en un AudioSource normal de esta escena, con su Loop
    /// desmarcado. Ademas asi no hay que tocar MusicaFondo, que esta en Comun/
    /// y lo usan los dos minijuegos.
    /// </summary>
    private IEnumerator Fanfarria()
    {
        if (retrasoMusica > 0f) yield return new WaitForSeconds(retrasoMusica);
        fanfarriaVictoria.Play();
    }

    private IEnumerator Secuencia()
    {
        // --- 1. el gatito entra caminando ---
        yield return CaminarGatito();

        // --- 2. el cofre ---
        if (cofre != null)
        {
            if (pausaAntesDelCofre > 0f)
                yield return new WaitForSeconds(pausaAntesDelCofre);

            yield return EsperarA(cofre.Aparecer);
        }

        // --- 3. el pescadito sale del cofre ---
        if (pescadito != null)
        {
            if (pausaAntesDelPescadito > 0f)
                yield return new WaitForSeconds(pausaAntesDelPescadito);

            yield return EsperarA(pescadito.Aparecer);
        }

        // --- 4. VICTORIA ---
        if (pausaAntesDelTexto > 0f)
            yield return new WaitForSeconds(pausaAntesDelTexto);

        if (logoVictoria != null)
            yield return EsperarA(logoVictoria.Aparecer);
        else if (aparicionVictoria != null)
            yield return EsperarA(aparicionVictoria.Aparecer);

        // --- 5. los botones ---
        if (aparicionBotones != null)
        {
            if (pausaAntesDeBotones > 0f)
                yield return new WaitForSeconds(pausaAntesDeBotones);

            aparicionBotones.Aparecer();
        }
    }

    /// <summary>
    /// Mueve al gatito a velocidad constante hasta el destino.
    ///
    /// Velocidad constante y no Lerp a proposito: un Lerp frena al final, y un
    /// personaje que camina no desacelera solo. Ademas asi la velocidad casa
    /// con los cuadros de la animacion y no parece que patine.
    /// </summary>
    private IEnumerator CaminarGatito()
    {
        if (gatito == null || destinoGatito == null) yield break;

        Vector3 destino = destinoGatito.position;

        // Solo se camina en horizontal: se conserva la Y a la que lo pusiste.
        destino.y = gatito.position.y;
        destino.z = gatito.position.z;

        while (Vector3.Distance(gatito.position, destino) > 0.01f)
        {
            gatito.position = Vector3.MoveTowards(
                gatito.position, destino, velocidadGatito * Time.deltaTime);
            yield return null;
        }

        gatito.position = destino;

        // Ya llego: se para de mover las patas.
        if (animacionGatito != null)
        {
            animacionGatito.enabled = false;

            if (spriteQuieto != null)
            {
                var render = animacionGatito.GetComponent<SpriteRenderer>();
                if (render != null) render.sprite = spriteQuieto;
            }
        }
    }

    /// <summary>
    /// Convierte cualquier "Aparecer(callback)" en algo que se puede esperar
    /// con yield. Evita repetir el bloque bool + while en cada paso.
    /// </summary>
    private IEnumerator EsperarA(System.Action<System.Action> lanzar)
    {
        bool termino = false;
        lanzar(() => termino = true);
        while (!termino) yield return null;
    }

    // --- Metodos para enganchar al OnClick de los botones ---

    /// <summary>Vuelve al menu principal.</summary>
    public void VolverAlMenu()
    {
        // No hace falta reiniciar Partida a mano: al elegir dificultad otra vez,
        // SelectorDeDificultad llama a Partida.Comenzar() y eso pone vidas y
        // puntaje a cero. Hacerlo aqui seria duplicar esa responsabilidad.
        SceneManager.LoadScene(escenaMenu);
    }

    /// <summary>Cierra el juego.</summary>
    public void Salir()
    {
        Application.Quit();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }
}
