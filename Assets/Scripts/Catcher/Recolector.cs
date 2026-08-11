using UnityEngine;

public class Recolector : MonoBehaviour
{
    public int puntosPorBueno = 1;

    // Para poder probar la escena SOLA (sin pasar por el menu):
    // si nadie arranco una partida, arranca una de prueba.
    public int vidasSiSeJuegaSuelto = 5;

    void Start()
    {
        if (!Partida.EnCurso)
            Partida.Comenzar(vidasSiSeJuegaSuelto);
    }

    void OnTriggerEnter2D(Collider2D collision)
    {
        // Solo nos importan los objetos que caen.
        ObjetoQueCae objeto = collision.GetComponent<ObjetoQueCae>();
        if (objeto == null) return;

        if (objeto.esMalo)
        {
            Partida.QuitarVida();
            Debug.Log($"Atrapaste un MALO. Vidas: {Partida.Vidas}");
        }
        else
        {
            Partida.Sumar(puntosPorBueno);
            Debug.Log($"Atrapaste un bueno. Puntaje: {Partida.Puntaje}");
        }

        Destroy(collision.gameObject);
    }
}