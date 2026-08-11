using UnityEngine;
using UnityEngine.SceneManagement;

public class GameOverUI : MonoBehaviour
{
    [SerializeField] private GameObject panel;          // el panel visible, empieza apagado
    [SerializeField] private string escenaMenu = "HistoriaInicio";

    void OnEnable()  { Partida.AlPerder += Mostrar; }
    void OnDisable() { Partida.AlPerder -= Mostrar; }

    private void Mostrar()
    {
        if (panel != null) panel.SetActive(true);
    }

    public void Reintentar() => SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    public void IrAlMenu()   => SceneManager.LoadScene(escenaMenu);
}