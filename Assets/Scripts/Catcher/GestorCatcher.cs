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

    [Tooltip("La pantalla de cierre de la aventura. Tiene que estar en Build Profiles.")]
    [SerializeField] private string escenaFinal = "Final";

    [Tooltip("Segundos que se queda el '¡Ganaste!' en pantalla antes de pasar " +
             "a la escena Final. Dale tiempo a leerlo entero.")]
    [SerializeField] private float esperaAntesDelFinal = 2.5f;

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
    }

    private void MostrarGanaste()
    {
        if (mensajeGanaste != null)
            mensajeGanaste.SetActive(true);

        // La cuenta atras arranca AQUI y no en Ganar() a proposito: este metodo
        // es el callback del fundido, asi que se ejecuta cuando la pantalla ya
        // esta en negro y el mensaje es visible. Si se lanzara desde Ganar(),
        // correria en paralelo al fundido y el jugador se comeria parte de la
        // espera mirando una pantalla que todavia se esta oscureciendo.
        StartCoroutine(IrAlFinal());
    }

    private System.Collections.IEnumerator IrAlFinal()
    {
        yield return new WaitForSeconds(esperaAntesDelFinal);

        if (string.IsNullOrEmpty(escenaFinal))
        {
            Debug.LogWarning("[GestorCatcher] No hay escena final asignada, " +
                             "asi que la aventura se queda aqui.", this);
            yield break;
        }

        SceneManager.LoadScene(escenaFinal);
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