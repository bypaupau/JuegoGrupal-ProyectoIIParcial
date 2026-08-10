using UnityEngine;

/// <summary>
/// Los niveles de dificultad del juego. Uno por integrante del grupo, como
/// pide la rubrica.
///
/// Los numeros estan puestos a mano (Facil = 0, Dificil = 1) porque se guardan
/// en PlayerPrefs. Si algun dia agregas un nivel en medio, ponle un numero
/// nuevo al final en vez de renumerar, o las partidas guardadas van a leer el
/// nivel equivocado.
/// </summary>
public enum NivelDeDificultad
{
    Facil = 0,
    Dificil = 1,
}

/// <summary>
/// Guarda que dificultad eligio el jugador y la mantiene viva entre escenas.
///
/// POR QUE ES static Y NO UN MonoBehaviour:
/// al cambiar de escena Unity destruye todos los GameObjects, asi que un
/// componente normal perderia el dato al pasar del menu al TopDown, y otra vez
/// al pasar al Catcher. Una clase static no vive en ninguna escena, asi que
/// sobrevive a todos los cambios.
///
/// COMO SE USA (esto responde a lo del panel compartido):
/// el panel para elegir dificultad se monta UNA SOLA VEZ, en la escena del
/// menu. No hace falta convertirlo en prefab ni copiarlo a la escena del
/// Catcher. Lo que viaja de una escena a otra no es el panel, es este dato.
/// Cada minijuego solo tiene que leer Dificultad.Nivel y traducirlo a sus
/// propios numeros con un ValorPorDificultad (ver abajo), asi nadie tiene que
/// tocar los scripts del otro.
/// </summary>
public static class Dificultad
{
    private const string ClaveGuardado = "dificultad";

    /// <summary>El nivel elegido. Si nadie eligio todavia, arranca en Facil.</summary>
    public static NivelDeDificultad Nivel { get; private set; } = NivelDeDificultad.Facil;

    /// <summary>Se dispara cuando cambia el nivel. Util para refrescar el HUD.</summary>
    public static event System.Action<NivelDeDificultad> AlCambiar;

    /// <summary>
    /// Guarda el nivel elegido. Llamalo desde los botones del panel.
    ///
    /// Se escribe tambien en PlayerPrefs para que puedas darle Play
    /// directamente a la escena del TopDown o a la del Catcher mientras
    /// desarrollas, sin tener que pasar por el menu cada vez.
    /// </summary>
    public static void Elegir(NivelDeDificultad nivel)
    {
        if (Nivel == nivel) return;

        Nivel = nivel;
        PlayerPrefs.SetInt(ClaveGuardado, (int)nivel);
        PlayerPrefs.Save();

        AlCambiar?.Invoke(nivel);
    }

    /// <summary>
    /// Lee el nivel guardado antes de que cargue ninguna escena.
    ///
    /// Igual que en MenuPrincipal: Unity 6 no recarga el dominio de C# al darle
    /// Play, asi que los campos static conservan el valor de la ejecucion
    /// anterior. Sin este metodo, cambiar la dificultad en una partida se
    /// arrastraria a la siguiente aunque no pases por el menu.
    /// </summary>
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void CargarGuardado()
    {
        Nivel = (NivelDeDificultad)PlayerPrefs.GetInt(ClaveGuardado, (int)NivelDeDificultad.Facil);
        AlCambiar = null;   // los suscriptores de la partida anterior ya no existen
    }
}

/// <summary>
/// Un numero que vale distinto en cada dificultad.
///
/// Se pone como campo en cualquier componente y aparece en el Inspector con
/// una casilla por nivel. Luego se lee con .Actual, que devuelve el valor del
/// nivel que el jugador eligio en el menu.
///
/// La gracia es que cada minijuego decide SUS propios numeros: lo unico que
/// comparten tu escena y la de Daniel es el nivel elegido, no las cantidades.
/// Asi el TopDown puede pedir 5 o 10 monedas y el Catcher lo que le convenga,
/// sin que ninguno de los dos tenga que editar el script del otro.
///
/// Ejemplo:
///     [SerializeField] private ValorPorDificultad monedas = new ValorPorDificultad();
///     ...
///     int meta = monedas.Actual;
/// </summary>
[System.Serializable]
public class ValorPorDificultad
{
    [Tooltip("Valor que se usa en el nivel Facil.")]
    [SerializeField] private int facil = 5;

    [Tooltip("Valor que se usa en el nivel Dificil.")]
    [SerializeField] private int dificil = 10;

    /// <summary>El valor que toca segun la dificultad elegida.</summary>
    public int Actual => Dificultad.Nivel == NivelDeDificultad.Facil ? facil : dificil;

    /// <summary>Para leer un nivel concreto sin depender del elegido.</summary>
    public int En(NivelDeDificultad nivel) =>
        nivel == NivelDeDificultad.Facil ? facil : dificil;
}
