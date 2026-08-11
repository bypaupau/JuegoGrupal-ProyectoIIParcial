using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Reproductor unico de musica de fondo. Vive fuera de las escenas
/// (DontDestroyOnLoad), asi que la cancion no se corta al cambiar de escena:
/// hace un crossfade entre la que sonaba y la nueva.
///
/// NO hay que ponerlo en ninguna escena ni arrastrarlo a ningun lado. Se crea
/// solo la primera vez que alguien pide musica. Lo unico que se toca en el
/// editor es el componente <see cref="MusicaDeEscena"/>.
///
/// Los efectos de sonido (monedas, golpes) siguen igual que siempre, con sus
/// propios AudioSource. Esto solo maneja la musica.
///
/// COMO FUNCIONA EL CROSSFADE:
/// Hay dos AudioSource. En cada cambio, la nueva cancion entra por el que
/// esta libre subiendo de 0 al volumen pedido, mientras la vieja baja a 0 por
/// el otro. Con un solo AudioSource seria imposible: no puede reproducir dos
/// clips a la vez.
///
/// Los fundidos usan Time.unscaledDeltaTime a proposito, para que sigan
/// funcionando con el juego en pausa (Time.timeScale = 0), como en la
/// pantalla de victoria.
/// </summary>
public class MusicaFondo : MonoBehaviour
{
    private static MusicaFondo instancia;

    /// <summary>
    /// El reproductor. Si todavia no existe se crea solo.
    /// </summary>
    public static MusicaFondo Instancia
    {
        get
        {
            if (instancia == null)
            {
                var go = new GameObject("~MusicaFondo");
                instancia = go.AddComponent<MusicaFondo>();
                DontDestroyOnLoad(go);
            }
            return instancia;
        }
    }

    /// <summary>
    /// Unity 6 no recarga el dominio de C# al dar Play, asi que los campos
    /// static conservan el valor de la sesion anterior. Sin esto, la segunda
    /// vez que le das Play 'instancia' apuntaria a un objeto ya destruido.
    /// </summary>
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ReiniciarEstado()
    {
        instancia = null;
    }

    private AudioSource fuenteA;
    private AudioSource fuenteB;

    // Cual de las dos esta sonando ahora. null = silencio.
    private AudioSource activa;

    private Coroutine transicion;

    // Lo pone Reproducir(). Sirve para saber si la escena que acaba de cargar
    // pidio musica o no.
    private bool alguienPidioMusica;

    // Con cuanto fundido se apaga la musica al entrar a una escena que no
    // tiene ninguna asignada.
    private float ultimoFadeOut = 1.5f;

    /// <summary>La cancion que suena ahora, o null si hay silencio.</summary>
    public AudioClip ClipActual => activa != null ? activa.clip : null;

    private void Awake()
    {
        if (instancia != null && instancia != this)
        {
            Destroy(gameObject);
            return;
        }

        instancia = this;
        DontDestroyOnLoad(gameObject);

        fuenteA = CrearFuente();
        fuenteB = CrearFuente();

        SceneManager.sceneLoaded += AlCargarEscena;
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= AlCargarEscena;
        if (instancia == this) instancia = null;
    }

    private AudioSource CrearFuente()
    {
        var fuente = gameObject.AddComponent<AudioSource>();
        fuente.playOnAwake = false;
        fuente.loop = true;
        fuente.volume = 0f;
        // 2D puro: la musica no depende de donde este el AudioListener.
        fuente.spatialBlend = 0f;
        return fuente;
    }

    // --- API ---

    /// <summary>
    /// Pone una cancion en bucle. Si ya estaba sonando esa misma, no la
    /// reinicia (solo ajusta el volumen), asi que se puede llamar sin miedo
    /// aunque la escena se recargue.
    /// </summary>
    /// <param name="clip">La cancion. Si es null, equivale a Detener().</param>
    /// <param name="volumen">De 0 a 1.</param>
    /// <param name="fadeIn">Segundos que tarda en entrar.</param>
    /// <param name="fadeOut">Segundos que tarda en irse la anterior.</param>
    public void Reproducir(AudioClip clip, float volumen = 0.5f,
                           float fadeIn = 1.5f, float fadeOut = 1.5f)
    {
        alguienPidioMusica = true;
        ultimoFadeOut = fadeOut;

        if (clip == null)
        {
            Detener(fadeOut);
            return;
        }

        // Misma cancion que ya suena: no se reinicia, se deja seguir.
        if (activa != null && activa.clip == clip && activa.isPlaying)
        {
            if (transicion == null) activa.volume = volumen;
            return;
        }

        if (transicion != null) StopCoroutine(transicion);

        AudioSource sale = activa;
        AudioSource entra = (activa == fuenteA) ? fuenteB : fuenteA;

        entra.clip = clip;
        entra.volume = 0f;
        entra.loop = true;
        entra.Play();

        activa = entra;
        transicion = StartCoroutine(Fundir(entra, volumen, fadeIn, sale, fadeOut));
    }

    /// <summary>Apaga la musica con fundido.</summary>
    public void Detener(float fadeOut = 1.5f)
    {
        if (activa == null) return;

        if (transicion != null) StopCoroutine(transicion);

        AudioSource sale = activa;
        activa = null;
        transicion = StartCoroutine(Fundir(null, 0f, 0f, sale, fadeOut));
    }

    /// <summary>
    /// Apaga la musica si hay alguna sonando. A diferencia de
    /// Instancia.Detener(), esta version NO crea el reproductor si todavia no
    /// existe: se puede llamar desde cualquier escena aunque nadie haya
    /// pedido musica nunca.
    /// </summary>
    public static void Apagar(float fadeOut = 1.5f)
    {
        if (instancia != null) instancia.Detener(fadeOut);
    }

    /// <summary>Cambia el volumen de lo que este sonando, sin fundido.</summary>
    public void PonerVolumen(float volumen)
    {
        if (activa != null) activa.volume = Mathf.Clamp01(volumen);
    }

    // --- Interno ---

    private IEnumerator Fundir(AudioSource entra, float volumenFinal, float fadeIn,
                               AudioSource sale, float fadeOut)
    {
        float volumenInicialSale = sale != null ? sale.volume : 0f;
        float duracion = Mathf.Max(fadeIn, fadeOut, 0.0001f);

        float t = 0f;
        while (t < duracion)
        {
            t += Time.unscaledDeltaTime;

            if (entra != null)
                entra.volume = fadeIn > 0f
                    ? Mathf.Lerp(0f, volumenFinal, Mathf.Clamp01(t / fadeIn))
                    : volumenFinal;

            if (sale != null)
                sale.volume = fadeOut > 0f
                    ? Mathf.Lerp(volumenInicialSale, 0f, Mathf.Clamp01(t / fadeOut))
                    : 0f;

            yield return null;
        }

        if (entra != null) entra.volume = volumenFinal;

        if (sale != null)
        {
            sale.Stop();
            sale.clip = null;
            sale.volume = 0f;
        }

        transicion = null;
    }

    private void AlCargarEscena(Scene escena, LoadSceneMode modo)
    {
        alguienPidioMusica = false;
        StartCoroutine(RevisarSiLaEscenaPidioMusica());
    }

    /// <summary>
    /// Si la escena que acaba de cargar no tiene ningun MusicaDeEscena (o
    /// todavia no se lo pusieron), la musica anterior se va con fundido en
    /// vez de quedarse sonando encima. En cuanto alguien arrastre el
    /// componente a esa escena, su cancion entra sola.
    ///
    /// El 'yield return null' es clave: sceneLoaded se dispara ANTES de los
    /// Start() de la escena nueva. Hay que esperar un frame para darles
    /// tiempo a pedir su musica.
    /// </summary>
    private IEnumerator RevisarSiLaEscenaPidioMusica()
    {
        yield return null;

        if (!alguienPidioMusica) Detener(ultimoFadeOut);
    }
}
