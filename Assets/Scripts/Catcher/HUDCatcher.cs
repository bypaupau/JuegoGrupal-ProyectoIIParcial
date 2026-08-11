using UnityEngine;
using TMPro;

public class HUDCatcher : MonoBehaviour
{
    public TMP_Text textoPuntaje;
    public TMP_Text textoVidas;

    // Suscribirse a los eventos de Partida al activarse...
    void OnEnable()
    {
        Partida.AlCambiarPuntaje += MostrarPuntaje;
        Partida.AlCambiarVidas += MostrarVidas;
    }

    // ...y desuscribirse al desactivarse (para no dejar enganches colgando).
    void OnDisable()
    {
        Partida.AlCambiarPuntaje -= MostrarPuntaje;
        Partida.AlCambiarVidas -= MostrarVidas;
    }

    void Start()
    {
        // Pintar los valores actuales al arrancar la escena.
        MostrarPuntaje(Partida.Puntaje);
        MostrarVidas(Partida.Vidas);
    }

    void MostrarPuntaje(int puntaje)
    {
        if (textoPuntaje != null)
            textoPuntaje.text = "Puntaje: " + puntaje;
    }

    void MostrarVidas(int vidas)
    {
        if (textoVidas != null)
            textoVidas.text = "Vidas: " + vidas;
    }
}