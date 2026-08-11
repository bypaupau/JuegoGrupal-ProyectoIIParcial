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

    [Header("Pantallas de final")]
    [Tooltip("La secuencia de '¡Ganaste!' que lleva al siguiente minijuego. " +
             "Si lo dejas vacio se busca solo en la escena.")]
    [SerializeField] private PantallaVictoria pantallaVictoria;

    /// <summary>True cuando ya se gano o se perdio.</summary>
    public bool Terminado { get; private set; }

    private void Awake()
    {
        if (monedas == null) monedas = FindAnyObjectByType<SpawnerMonedas>();
        if (enemigos == null) enemigos = FindAnyObjectByType<SpawnerEnemigos>();

        // Include por si esta montada mal y su objeto quedo apagado: asi
        // PantallaVictoria puede avisarlo por Console en vez de fallar en
        // silencio al ganar, cuando ya es tarde para darse cuenta.
        if (pantallaVictoria == null)
            pantallaVictoria = FindAnyObjectByType<PantallaVictoria>(FindObjectsInactive.Include);
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

        // Solo al ganar. En la derrota se deja a proposito que los enemigos
        // sigan rondando al gatito caido, que es como estaba antes.
        Paralizar();

        // Primero paralizar, despues mostrar. Si fuera al reves, los enemigos
        // seguirian moviendose un frame por detras del fundido.
        if (pantallaVictoria != null)
            pantallaVictoria.Mostrar();
        else
            Debug.LogWarning("[GestorTopDown] No hay PantallaVictoria en la escena, " +
                             "asi que la partida se queda aqui parada.", this);
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
        // Esto solo corta las apariciones nuevas, no a los que ya andan sueltos.
        // De esos se encarga Paralizar(), y solo en la victoria.
        if (enemigos != null) enemigos.Detener();

        var gatito = MovimientoTopDown.Actual;
        if (gatito == null) return;

        gatito.enabled = false;

        // Apagar el script no frena el Rigidbody2D: se queda con la ultima
        // velocidad que le pusimos y el gatito seguiria deslizandose solo.
        var rb = gatito.GetComponent<Rigidbody2D>();
        if (rb != null) rb.linearVelocity = Vector2.zero;
    }

    /// <summary>
    /// Deja clavados a los enemigos que ya estaban en la arena.
    ///
    /// No se usa Time.timeScale = 0, que seria lo obvio: eso tambien congelaria
    /// el fundido y la maquina de escribir de PantallaVictoria, porque esperan
    /// con WaitForSeconds y ese si va con el reloj escalado. Apagando scripts
    /// se para lo que molesta y sigue funcionando lo que tiene que animarse.
    /// </summary>
    private void Paralizar()
    {
        if (enemigos == null) return;

        // SpawnerEnemigos crea cada enemigo como hijo suyo, asi que se piden
        // ahi en vez de rastrear la escena entera. Sale mas barato, solo coge
        // los que salieron de este spawner, y de paso evita FindObjectsByType,
        // cuyas sobrecargas fueron quedando obsoletas en Unity 6.5.
        foreach (var enemigo in enemigos.GetComponentsInChildren<Enemigo>())
        {
            enemigo.enabled = false;

            var rbEnemigo = enemigo.GetComponent<Rigidbody2D>();
            if (rbEnemigo != null) rbEnemigo.linearVelocity = Vector2.zero;
        }
    }
}
