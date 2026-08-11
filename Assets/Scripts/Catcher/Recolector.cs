using UnityEngine;

public class Recolector : MonoBehaviour
{
    public int puntosPorBueno = 1;
    public int vidasSiSeJuegaSuelto = 5;

    // Aviso de que se atrapo un objeto BUENO. El GestorCatcher lo escucha
    // para contar cuantos llevas y decidir la victoria.
    public static event System.Action AlAtraparBueno;

    // Unity 6 no recarga el dominio de C# al dar Play: hay que limpiar el
    // evento static para que no queden enganchados objetos de la partida
    // anterior (mismo patron que usa Partida y Dificultad).
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ReiniciarEstado()
    {
        AlAtraparBueno = null;
    }

    void Start()
    {
        if (!Partida.EnCurso)
            Partida.Comenzar(vidasSiSeJuegaSuelto);
    }

    void OnTriggerEnter2D(Collider2D collision)
    {
        ObjetoQueCae objeto = collision.GetComponent<ObjetoQueCae>();
        if (objeto == null) return;

        if (objeto.esMalo)
        {
            Partida.QuitarVida();
        }
        else
        {
            Partida.Sumar(puntosPorBueno);
            AlAtraparBueno?.Invoke();   // avisa al gestor
        }

        Destroy(collision.gameObject);
    }
}