using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Reparte una cantidad fija de monedas por la arena y lleva la cuenta.
/// Cuando el gatito las junta todas, dispara AlGanar.
///
/// Las coloca en puntos libres al azar usando AreaJugable, asi que nunca caen
/// dentro de un muro ni encima del jugador.
/// </summary>
public class SpawnerMonedas : MonoBehaviour
{
    [Header("Que y cuantas")]
    [SerializeField] private Moneda prefabMoneda;

    [Tooltip("Cuantas hay que juntar para ganar, segun la dificultad que se " +
             "eligio en el menu. Este es el nivel de dificultad del TopDown.")]
    [SerializeField] private ValorPorDificultad metaDeMonedas = new ValorPorDificultad();

    // Cuantas se van a repartir de verdad. Sale de metaDeMonedas al empezar, y
    // baja si alguna no cupo en el laberinto.
    private int cantidad;

    [Header("Donde")]
    [SerializeField] private AreaJugable area;

    [Tooltip("No las pone mas cerca que esto del gatito, para que no salgan regaladas.")]
    [SerializeField] private float distanciaMinimaDelJugador = 4f;

    [Tooltip("Separacion minima entre monedas, para que no salgan amontonadas.")]
    [SerializeField] private float separacionEntreMonedas = 2f;

    [Header("Puntaje")]
    [SerializeField] private int puntaje;

    /// <summary>Monedas que ya recogio el jugador.</summary>
    public int Recogidas { get; private set; }

    /// <summary>Cuantas hay en total en este nivel.</summary>
    public int Total => cantidad;

    /// <summary>Puntos acumulados. Lo puede leer el HUD.</summary>
    public int Puntaje => puntaje;

    /// <summary>Se dispara cada vez que recoge una. Parametros: recogidas, total.</summary>
    public event System.Action<int, int> AlCambiarCuenta;

    /// <summary>Se dispara cuando junta todas.</summary>
    public event System.Action AlGanar;

    private readonly List<Vector3> colocadas = new List<Vector3>();
    private Transform jugador;

    private void Start()
    {
        if (prefabMoneda == null) { Debug.LogError("[SpawnerMonedas] Falta el prefab de moneda.", this); return; }
        if (area == null) { Debug.LogError("[SpawnerMonedas] Falta asignar el AreaJugable.", this); return; }

        if (MovimientoTopDown.Actual != null) jugador = MovimientoTopDown.Actual.transform;

        cantidad = metaDeMonedas.Actual;
        Debug.Log($"[SpawnerMonedas] Dificultad {Dificultad.Nivel}: hay que juntar {cantidad} monedas.", this);

        Repartir();
    }

    private void Repartir()
    {
        colocadas.Clear();
        int puestas = 0;

        for (int i = 0; i < cantidad; i++)
        {
            if (!BuscarSitio(out Vector3 sitio)) continue;

            var moneda = Instantiate(prefabMoneda, sitio, Quaternion.identity, transform);
            moneda.AlRecoger += Recoger;
            colocadas.Add(sitio);
            puestas++;
        }

        if (puestas < cantidad)
            Debug.LogWarning($"[SpawnerMonedas] Solo cupieron {puestas} de {cantidad} monedas. " +
                             "El area es chica o esta muy llena de muros.", this);

        cantidad = puestas;
        AlCambiarCuenta?.Invoke(Recogidas, cantidad);
    }

    // Reintenta hasta dar con un hueco que respete las dos distancias minimas.
    private bool BuscarSitio(out Vector3 sitio)
    {
        for (int intento = 0; intento < 40; intento++)
        {
            if (!area.PuntoLibreAleatorio(out sitio, distanciaMinimaDelJugador, jugador)) break;

            bool muyCerca = false;
            foreach (var otra in colocadas)
                if (Vector3.Distance(otra, sitio) < separacionEntreMonedas) { muyCerca = true; break; }

            if (!muyCerca) return true;
        }

        sitio = Vector3.zero;
        return false;
    }

    private void Recoger(Moneda moneda)
    {
        Recogidas++;
        puntaje += moneda.Puntos;

        // Tambien al marcador compartido: el puntaje se acumula entre los dos
        // minijuegos, asi que lo que sumes aqui viaja al Catcher y al reves.
        Partida.Sumar(moneda.Puntos);

        AlCambiarCuenta?.Invoke(Recogidas, cantidad);

        if (Recogidas >= cantidad)
        {
            Debug.Log($"[SpawnerMonedas] Ganaste. {Recogidas} monedas, {puntaje} puntos.");
            AlGanar?.Invoke();
        }
    }
}
