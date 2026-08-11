using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Controla la escena de inicio: primero el panel de narracion con la
/// maquina de escribir, y al terminar hace un fundido y muestra el menu.
///
/// La narracion solo se ve la primera vez. Si el jugador vuelve al menu
/// desde un minijuego, entra directo al menu.
/// </summary>
public class MenuPrincipal : MonoBehaviour
{
    [Header("Narracion")]
    [SerializeField] private GameObject panelNarracion;
    [SerializeField] private MaquinaDeEscribir narracion;
    [SerializeField] private Desvanecedor fundidoNarracion;

    [Tooltip("Segundos de pantalla negra entre que se va el texto y aparece el menu.")]
    [SerializeField] private float pausaEnNegro = 0.4f;

    [Header("Menu")]
    [SerializeField] private GameObject panelMenu;

    [Tooltip("Objetos del mundo que forman el menu: el Grid del suelo, las nubes " +
             "y el gatito. Se mantienen ocultos durante la narracion.")]
    [SerializeField] private GameObject mundoMenu;

    [Header("Titulo / logo")]
    [Tooltip("Componente LogoTitulo del Text (TMP) del titulo. Anima las letras " +
             "una a una. Si lo dejas vacio, el titulo sale de golpe con el menu.")]
    [SerializeField] private LogoTitulo logoTitulo;

    [Tooltip("Segundos que espera el titulo desde que el menu se hace visible. " +
             "Un respiro corto para que primero se asiente el fondo.")]
    [SerializeField] private float retrasoTitulo = 0.5f;

    [Header("Aparicion de los botones")]
    [Tooltip("Componente AparecerDeslizando del GrupoBotones. Va aparte del titulo " +
             "para que los botones entren despues. La opacidad inicial y el " +
             "desplazamiento se configuran en ese componente, no aqui.")]
    [SerializeField] private AparecerDeslizando aparicionBotones;

    [Tooltip("Segundos que esperan los botones DESDE QUE EL LOGO TERMINA de " +
             "montarse. Antes se contaba desde que el menu era visible, y por " +
             "eso el titulo y los botones se pisaban.")]
    [SerializeField] private float retrasoBotones = 0.6f;

    [Header("Musica")]
    [Tooltip("El MusicaDeEscena del menu, con 'Arrancar Sola' DESMARCADO. Se " +
             "dispara aqui y no en su propio Start para que la narracion quede " +
             "en silencio y la cancion entre con el menu.")]
    [SerializeField] private MusicaDeEscena musicaMenu;

    [Header("Escenas")]
    [SerializeField] private string escenaPrimerMinijuego = "JuegoTopDown";

    [Header("Pruebas")]
    [Tooltip("SALTA la narracion y entra directo al menu. Para iterar sobre el " +
             "menu sin tragarte la historia entera cada vez. ACUERDATE DE APAGARLO.")]
    [SerializeField] private bool saltarNarracion = false;

    [Tooltip("Narra SIEMPRE, aunque ya se haya visto al volver de un minijuego.")]
    [SerializeField] private bool siempreNarrar = false;

    // static: sobrevive a los cambios de escena dentro de una misma partida.
    private static bool yaSeVioLaNarracion = false;

    /// <summary>
    /// Los campos static NO se reinician al parar y volver a dar Play en el
    /// Editor, porque Unity 6 no recarga el dominio de C# por defecto. Sin
    /// esto, la narracion solo se veria la primera vez que le das Play y
    /// nunca mas hasta reiniciar Unity.
    ///
    /// SubsystemRegistration se ejecuta antes de que cargue ninguna escena.
    /// </summary>
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ReiniciarEstado()
    {
        yaSeVioLaNarracion = false;
    }

    private void Start()
    {
        if (saltarNarracion || (yaSeVioLaNarracion && !siempreNarrar))
        {
            panelNarracion.SetActive(false);
            MostrarMenu();
            return;
        }

        // Todo lo del menu apagado mientras se narra
        panelMenu.SetActive(false);
        if (mundoMenu != null) mundoMenu.SetActive(false);

        // Orden importante: suscribirse, activar el panel, y SOLO ENTONCES
        // arrancar la narracion. Si MaquinaDeEscribir arrancara sola en su
        // OnEnable, con el panel ya activo en el editor podria terminar antes
        // de que nos hubieramos suscrito, y el evento se perderia.
        narracion.AlTerminar += TerminoLaNarracion;

        if (fundidoNarracion != null) fundidoNarracion.PonerAlfa(1f);
        panelNarracion.SetActive(true);
        narracion.Comenzar();
    }

    private void OnDestroy()
    {
        if (narracion != null) narracion.AlTerminar -= TerminoLaNarracion;
    }

    private void TerminoLaNarracion()
    {
        StartCoroutine(TransicionAlMenu());
    }

    /// <summary>
    /// Cuando llega aqui, MaquinaDeEscribir ya fundio el texto y la pantalla
    /// esta en negro limpio. Ahora solo hay que fundir ese negro.
    /// </summary>
    private System.Collections.IEnumerator TransicionAlMenu()
    {
        // Pausa en negro, para que la transicion respire
        if (pausaEnNegro > 0f)
            yield return new WaitForSeconds(pausaEnNegro);

        // Encendemos el mundo del menu ANTES del fundido, para que aparezca
        // gradualmente por detras en vez de aparecer de golpe al final.
        if (mundoMenu != null) mundoMenu.SetActive(true);

        if (fundidoNarracion != null)
        {
            bool terminado = false;
            fundidoNarracion.Desvanecer(() => terminado = true);
            while (!terminado) yield return null;
        }

        panelNarracion.SetActive(false);
        MostrarMenu();
    }

    private void MostrarMenu()
    {
        // Hay que leerlo ANTES de marcarlo. Si el jugador ya estuvo aqui y
        // vuelve desde un minijuego, no le repetimos la intro del logo: se
        // la ha visto ya y solo quiere volver a pulsar JUGAR.
        bool primeraVezEnElMenu = !yaSeVioLaNarracion;

        yaSeVioLaNarracion = true;
        if (mundoMenu != null) mundoMenu.SetActive(true);
        panelMenu.SetActive(true);

        // La musica entra aqui, con el menu ya visible. Si el jugador vuelve
        // al menu desde un minijuego no se reinicia: MusicaFondo detecta que
        // es el mismo clip y lo deja seguir.
        if (musicaMenu != null) musicaMenu.Reproducir();

        if (!primeraVezEnElMenu)
        {
            // Vuelta desde un minijuego: todo puesto de una, sin ceremonia.
            if (logoTitulo != null) logoTitulo.MostrarYa();
            return;
        }

        // Ambos se dejan ocultos en el MISMO frame en que se enciende el panel.
        // Si esperaramos a la corrutina se veria un fogonazo de un frame con
        // el titulo y los botones ya puestos.
        if (logoTitulo != null) logoTitulo.Preparar();
        if (aparicionBotones != null) aparicionBotones.Preparar();

        StartCoroutine(SecuenciaDeEntrada());
    }

    /// <summary>
    /// Coreografia del menu: primero respira el fondo, luego se monta el
    /// titulo letra a letra, y solo cuando ese ha terminado entran los botones.
    ///
    /// Encadenar por callback en vez de por temporizadores sueltos evita que
    /// los botones se solapen con el logo si algun dia alargas la cascada.
    /// </summary>
    private System.Collections.IEnumerator SecuenciaDeEntrada()
    {
        if (logoTitulo != null)
        {
            if (retrasoTitulo > 0f)
                yield return new WaitForSeconds(retrasoTitulo);

            bool logoListo = false;
            logoTitulo.Aparecer(() => logoListo = true);
            while (!logoListo) yield return null;
        }

        if (aparicionBotones != null)
        {
            if (retrasoBotones > 0f)
                yield return new WaitForSeconds(retrasoBotones);

            aparicionBotones.Aparecer();
        }
    }

    // --- Metodos para enganchar a los botones (OnClick del Inspector) ---

    public void Jugar()
    {
        SceneManager.LoadScene(escenaPrimerMinijuego);
    }

    public void SaltarNarracion()
    {
        narracion.SaltarTodo();
    }

    public void Salir()
    {
        Application.Quit();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }
}
