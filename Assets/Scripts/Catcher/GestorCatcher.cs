using UnityEngine;
using UnityEngine.SceneManagement;

public class GestorCatcher : MonoBehaviour
{
    [Header("Meta de objetos buenos (Facil / Dificil)")]
    [SerializeField] private ValorPorDificultad meta = new ValorPorDificultad(10, 15);

    [Header("Victoria")]
    [Tooltip("El Desvanecedor del panel negro que aparece al ganar.")]
    [SerializeField] private Desvanecedor fundidoVictoria;
    [Tooltip("El objeto con el texto '¡Ganaste!'. Empieza APAGADO.")]
    [SerializeField] private GameObject mensajeGanaste;

    [Header("Derrota")]
    [Tooltip("El panel 'Game Over' con sus botones. Empieza APAGADO.")]
    [SerializeField] private GameObject panelGameOver;

    [Header("Escenas")]
    [SerializeField] private string escenaMenu = "HistoriaInicio";

    private int recogidas;
    private bool terminado;

    void OnEnable()
    {
        Recolector.AlAtraparBueno += ContarBueno;
        Partida.AlPerder += Perder;
    }

    void OnDisable()
    {
        Recolector.AlAtraparBueno -= ContarBueno;
        Partida.AlPerder -= Perder;
    }

    private void ContarBueno()
    {
        if (terminado) return;

        recogidas++;
        if (recogidas >= meta.Actual)
            Ganar();
    }

    private void Ganar()
    {
        if (terminado) return;
        terminado = true;

        // Fundido a negro; cuando termina, aparece el mensaje.
        if (fundidoVictoria != null)
            fundidoVictoria.Aparecer(MostrarGanaste);
        else
            MostrarGanaste();

        // (Cargar la escena Final se deja pendiente, lo detallas luego.)
    }

    private void MostrarGanaste()
    {
        if (mensajeGanaste != null)
            mensajeGanaste.SetActive(true);
    }

    private void Perder()
    {
        if (terminado) return;
        terminado = true;

        if (panelGameOver != null)
            panelGameOver.SetActive(true);
    }

    // --- Botones del Game Over (se enganchan al OnClick en el Inspector) ---

    public void Reintentar()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void IrAlMenu()
    {
        SceneManager.LoadScene(escenaMenu);
    }
}