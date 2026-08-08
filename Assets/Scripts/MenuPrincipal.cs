using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Controla la escena de inicio: primero el panel de narracion con la
/// maquina de escribir, y al terminar el panel del menu.
///
/// La narracion solo se ve la primera vez. Si el jugador vuelve al menu
/// desde un minijuego, entra directo al menu.
/// </summary>
public class MenuPrincipal : MonoBehaviour
{
    [Header("Paneles")]
    [SerializeField] private GameObject panelNarracion;
    [SerializeField] private GameObject panelMenu;

    [Header("Referencias")]
    [SerializeField] private MaquinaDeEscribir narracion;

    [Header("Escenas")]
    [SerializeField] private string escenaPrimerMinijuego = "JuegoTopDown";

    // static: sobrevive a los cambios de escena, pero se reinicia al cerrar el juego.
    private static bool yaSeVioLaNarracion = false;

    private void Start()
    {
        panelNarracion.SetActive(false);
        panelMenu.SetActive(false);

        if (yaSeVioLaNarracion)
        {
            MostrarMenu();
            return;
        }

        // Nos suscribimos antes de activar el panel: al activarlo se dispara
        // su OnEnable y arranca la corrutina. Si lo hicieramos al reves,
        // podriamos perder el evento.
        narracion.AlTerminar += MostrarMenu;
        panelNarracion.SetActive(true);
    }

    private void OnDestroy()
    {
        if (narracion != null) narracion.AlTerminar -= MostrarMenu;
    }

    private void MostrarMenu()
    {
        yaSeVioLaNarracion = true;
        panelNarracion.SetActive(false);
        panelMenu.SetActive(true);
    }

    // Metodos para enganchar a los botones (OnClick del Inspector) 

    public void Jugar()
    {
        SceneManager.LoadScene(escenaPrimerMinijuego);
    }

    public void SaltarNarracion()
    {
        narracion.Completar();
    }

    public void Salir()
    {
        Application.Quit();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }
}
