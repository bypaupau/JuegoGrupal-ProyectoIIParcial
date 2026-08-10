using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Panel para elegir dificultad. Va en la escena del menu, entre el boton de
/// Jugar y el primer minijuego.
///
/// Montaje:
///   1. Un GameObject vacio con este script, dentro del Canvas del menu.
///   2. Un panel hijo con los botones, asignado en panel. Empieza apagado.
///   3. El boton Jugar del menu, en vez de cargar la escena, llama a Mostrar().
///   4. Cada boton de dificultad llama a ElegirFacil() o ElegirDificil().
///
/// Los metodos publicos de abajo son los que se enganchan al OnClick de cada
/// boton en el Inspector, igual que en MenuPrincipal.
/// </summary>
public class SelectorDeDificultad : MonoBehaviour
{
    [Header("Panel")]
    [Tooltip("El panel con los botones. Se enciende al llamar a Mostrar().")]
    [SerializeField] private GameObject panel;

    [Tooltip("Objetos del menu que hay que apagar mientras se elige. Opcional.")]
    [SerializeField] private GameObject[] ocultarMientrasElige;

    [Tooltip("Opcional. Si le pones aqui el AparecerDeslizando del panel, entra " +
             "cayendo desde arriba igual que los botones del menu. El desplazamiento " +
             "y la duracion se configuran en ese componente, no aqui.")]
    [SerializeField] private AparecerDeslizando aparicion;

    [Header("Partida")]
    [Tooltip("Vidas con las que arranca el gatito. Son para toda la aventura: " +
             "las mismas en el Catcher y en el laberinto.")]
    [SerializeField] private ValorPorDificultad vidasIniciales = new ValorPorDificultad(5, 3);

    [Header("A donde va despues")]
    [Tooltip("Escena del primer minijuego. Tiene que estar en File > Build Profiles.")]
    [SerializeField] private string escenaSiguiente = "JuegoTopDown";

    [Tooltip("Si lo desmarcas, elegir dificultad no carga nada y se queda en el " +
             "menu. Util para probar el panel solo.")]
    [SerializeField] private bool cargarEscenaAlElegir = true;

    private void Awake()
    {
        if (panel != null) panel.SetActive(false);
    }

    // --- Metodos para enganchar a los botones (OnClick del Inspector) ---

    /// <summary>Abre el panel. Engancha aqui el boton Jugar del menu.</summary>
    public void Mostrar()
    {
        if (panel != null) panel.SetActive(true);

        foreach (var objeto in ocultarMientrasElige)
            if (objeto != null) objeto.SetActive(false);

        // Preparar() antes de Aparecer(): deja el panel invisible, corrido hacia
        // arriba y sin poder recibir clicks. Sin eso se veria un frame ya puesto
        // en su sitio y luego pegaria el salto hacia arriba para animarse.
        if (aparicion != null)
        {
            aparicion.Preparar();
            aparicion.Aparecer();
        }
    }

    /// <summary>Cierra el panel y vuelve al menu, sin elegir nada.</summary>
    public void Cancelar()
    {
        if (panel != null) panel.SetActive(false);

        foreach (var objeto in ocultarMientrasElige)
            if (objeto != null) objeto.SetActive(true);
    }

    public void ElegirFacil() => Elegir(NivelDeDificultad.Facil);

    public void ElegirDificil() => Elegir(NivelDeDificultad.Dificil);

    private void Elegir(NivelDeDificultad nivel)
    {
        // Este orden importa: vidasIniciales.Actual lee el nivel elegido, asi
        // que la dificultad tiene que estar puesta antes de arrancar la partida.
        Dificultad.Elegir(nivel);
        Partida.Comenzar(vidasIniciales.Actual);

        Debug.Log($"[SelectorDeDificultad] Partida nueva en {nivel} con {Partida.Vidas} vidas.");

        if (!cargarEscenaAlElegir) return;

        if (string.IsNullOrEmpty(escenaSiguiente))
        {
            Debug.LogError("[SelectorDeDificultad] No hay escena siguiente asignada.", this);
            return;
        }

        SceneManager.LoadScene(escenaSiguiente);
    }
}
