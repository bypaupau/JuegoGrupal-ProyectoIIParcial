using UnityEngine;

/// <summary>
/// Decide cuando se acaba el laberinto y con que resultado.
///
/// Se queda escuchando las dos condiciones de final que ya existian sueltas:
///   victoria -> SpawnerMonedas.AlGanar, cuando junta todas las monedas
///   derrota  -> Partida.AlPerder, cuando se queda sin vidas
///
/// De momento solo para la partida y avisa por Console. Cuando montes las
/// pantallas de Victoria y Game Over, se enganchan aqui: este es el unico
/// sitio que sabe que la partida termino.
///
/// Montaje: un GameObject vacio en la escena con este script.
/// </summary>
public class GestorTopDown : MonoBehaviour
{
    [Header("Quien avisa")]
    [SerializeField] private SpawnerMonedas monedas;
    [SerializeField] private SpawnerEnemigos enemigos;

    /// <summary>True cuando ya se gano o se perdio.</summary>
    public bool Terminado { get; private set; }

    private void Awake()
    {
        if (monedas == null) monedas = FindAnyObjectByType<SpawnerMonedas>();
        if (enemigos == null) enemigos = FindAnyObjectByType<SpawnerEnemigos>();
    }

    private void OnEnable()
    {
        if (monedas != null) monedas.AlGanar += Ganar;
        Partida.AlPerder += Perder;
    }

    // Desuscribirse siempre. Partida es static: si no se quita el enganche, el
    // evento sigue apuntando a este objeto despues de cambiar de escena, cuando
    // ya no existe, y Unity lanza una excepcion en la siguiente partida.
    private void OnDisable()
    {
        if (monedas != null) monedas.AlGanar -= Ganar;
        Partida.AlPerder -= Perder;
    }

    private void Ganar()
    {
        if (Terminado) return;
        Terminado = true;

        Debug.Log($"[GestorTopDown] VICTORIA. Puntaje {Partida.Puntaje}, " +
                  $"vidas restantes {Partida.Vidas}.", this);

        Detener();
    }

    private void Perder()
    {
        if (Terminado) return;
        Terminado = true;

        Debug.Log($"[GestorTopDown] DERROTA. Puntaje {Partida.Puntaje}.", this);

        Detener();
    }

    private void Detener()
    {
        if (enemigos != null) enemigos.Detener();

        // El gatito se queda quieto, pero los enemigos que ya estaban siguen
        // moviendose. Es a proposito: se ve mas vivo que congelar la escena.
        var gatito = MovimientoTopDown.Actual;
        if (gatito == null) return;

        gatito.enabled = false;

        // Apagar el script no frena el Rigidbody2D: se queda con la ultima
        // velocidad que le pusimos y el gatito seguiria deslizandose solo.
        var rb = gatito.GetComponent<Rigidbody2D>();
        if (rb != null) rb.linearVelocity = Vector2.zero;
    }
}
