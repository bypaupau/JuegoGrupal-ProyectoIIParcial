using UnityEngine;

/// <summary>
/// Las vidas y el puntaje de la partida en curso, compartidos por los dos
/// minijuegos.
///
/// Es static por lo mismo que Dificultad: al cambiar de escena Unity destruye
/// todos los GameObjects, asi que un componente normal perderia las vidas al
/// pasar del Catcher al TopDown. Una clase static no vive en ninguna escena.
///
/// LO QUE VA AQUI Y LO QUE NO:
/// aqui van los DATOS. El HUD que los pinta va en un Canvas dentro de cada
/// escena, y cada minijuego hace el suyo. El Canvas no se comparte ni se
/// convierte en prefab: se suscribe a los eventos de abajo y muestra lo que
/// haga falta. Asi el HUD de Daniel puede verse distinto al tuyo y aun asi
/// los dos cuentan lo mismo.
///
/// A diferencia de Dificultad, esto NO se guarda en PlayerPrefs: son datos de
/// una partida concreta, no una preferencia del jugador.
/// </summary>
public static class Partida
{
    /// <summary>Vidas que le quedan al gatito. Al llegar a 0, se perdio.</summary>
    public static int Vidas { get; private set; }

    /// <summary>Puntos acumulados. Se suman los de los dos minijuegos.</summary>
    public static int Puntaje { get; private set; }

    /// <summary>True entre Comenzar() y quedarse sin vidas.</summary>
    public static bool EnCurso { get; private set; }

    /// <summary>Cambiaron las vidas. Parametro: las que quedan.</summary>
    public static event System.Action<int> AlCambiarVidas;

    /// <summary>Cambio el puntaje. Parametro: el total.</summary>
    public static event System.Action<int> AlCambiarPuntaje;

    /// <summary>Se acabaron las vidas. Aqui se engancha la pantalla de derrota.</summary>
    public static event System.Action AlPerder;

    /// <summary>
    /// Arranca una partida nueva. Lo llama el SelectorDeDificultad justo antes
    /// de cargar el primer minijuego.
    /// </summary>
    public static void Comenzar(int vidasIniciales)
    {
        Vidas = Mathf.Max(1, vidasIniciales);
        Puntaje = 0;
        EnCurso = true;

        AlCambiarVidas?.Invoke(Vidas);
        AlCambiarPuntaje?.Invoke(Puntaje);
    }

    /// <summary>Quita una vida. Devuelve true si con eso se acabo la partida.</summary>
    public static bool QuitarVida()
    {
        if (!EnCurso) return false;

        Vidas = Mathf.Max(0, Vidas - 1);
        AlCambiarVidas?.Invoke(Vidas);

        if (Vidas > 0) return false;

        EnCurso = false;
        AlPerder?.Invoke();
        return true;
    }

    public static void Sumar(int puntos)
    {
        Puntaje += puntos;
        AlCambiarPuntaje?.Invoke(Puntaje);
    }

    /// <summary>
    /// Igual que en Dificultad y MenuPrincipal: Unity 6 no recarga el dominio
    /// de C# al darle Play, asi que sin esto la segunda partida arrancaria con
    /// las vidas y el puntaje de la anterior.
    ///
    /// Tambien se limpian los eventos: los que se suscribieron en la ejecucion
    /// pasada apuntan a objetos que ya no existen.
    /// </summary>
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ReiniciarEstado()
    {
        Vidas = 0;
        Puntaje = 0;
        EnCurso = false;

        AlCambiarVidas = null;
        AlCambiarPuntaje = null;
        AlPerder = null;
    }
}
