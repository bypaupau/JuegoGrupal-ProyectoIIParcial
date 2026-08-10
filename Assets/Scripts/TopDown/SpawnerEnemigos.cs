using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Va soltando enemigos mientras juegas, cada vez mas seguido.
///
/// Puede sacarlos por puntos de aparicion que coloques a mano (las entradas de
/// tu arena) o, si no le das ninguno, por cualquier hueco libre del AreaJugable.
///
/// Usa una corrutina y no un contador en Update: es mas claro y el intervalo se
/// ajusta en un solo sitio. Ese intervalo que baja poco a poco es, gratis, el
/// requisito de niveles de dificultad.
/// </summary>
public class SpawnerEnemigos : MonoBehaviour
{
    [Header("Que aparece")]
    [Tooltip("Puedes poner varios prefabs distintos: elige uno al azar cada vez.")]
    [SerializeField] private Enemigo[] prefabsEnemigo;

    [Header("Donde aparece")]
    [Tooltip("Las entradas de tu arena. Crea GameObjects vacios y arrastralos aqui. " +
             "Si lo dejas vacio, usa cualquier hueco libre del area.")]
    [SerializeField] private Transform[] puntosDeAparicion;

    [SerializeField] private AreaJugable area;

    [Tooltip("Si aparecen por el area, no los pone mas cerca que esto del gatito.")]
    [SerializeField] private float distanciaMinimaDelJugador = 6f;

    [Header("Ritmo")]
    [Tooltip("Segundos antes del primer enemigo.")]
    [SerializeField] private float esperaInicial = 2f;

    [Tooltip("Segundos entre enemigos al empezar.")]
    [SerializeField] private float intervaloInicial = 4f;

    [Tooltip("Nunca baja de aqui, por mucho que avance la partida.")]
    [SerializeField] private float intervaloMinimo = 1.2f;

    [Range(0.80f, 0.99f)]
    [Tooltip("Cuanto se acorta el intervalo tras cada enemigo. 0.95 = 5% mas rapido cada vez.")]
    [SerializeField] private float factorDeAceleracion = 0.95f;

    [Tooltip("Tope de enemigos vivos a la vez. Evita que la pantalla se sature.")]
    [SerializeField] private int maximoVivos = 8;

    /// <summary>Cuantos enemigos hay ahora mismo en la arena.</summary>
    public int Vivos => vivos.Count;

    private readonly List<Enemigo> vivos = new List<Enemigo>();
    private Transform jugador;
    private float intervalo;
    private bool activo = true;

    private void Start()
    {
        if (prefabsEnemigo == null || prefabsEnemigo.Length == 0)
        {
            Debug.LogError("[SpawnerEnemigos] No hay ningun prefab de enemigo asignado.", this);
            return;
        }

        if (MovimientoTopDown.Actual != null) jugador = MovimientoTopDown.Actual.transform;

        intervalo = intervaloInicial;
        StartCoroutine(Bucle());
    }

    private IEnumerator Bucle()
    {
        yield return new WaitForSeconds(esperaInicial);

        while (activo)
        {
            LimpiarMuertos();

            if (vivos.Count < maximoVivos) Aparecer();

            yield return new WaitForSeconds(intervalo);

            intervalo = Mathf.Max(intervaloMinimo, intervalo * factorDeAceleracion);
        }
    }

    private void Aparecer()
    {
        if (!BuscarSitio(out Vector3 sitio)) return;

        var prefab = prefabsEnemigo[Random.Range(0, prefabsEnemigo.Length)];
        var enemigo = Instantiate(prefab, sitio, Quaternion.identity, transform);
        vivos.Add(enemigo);
    }

    private bool BuscarSitio(out Vector3 sitio)
    {
        // Preferimos las entradas que coloco Pau a mano.
        if (puntosDeAparicion != null && puntosDeAparicion.Length > 0)
        {
            var punto = puntosDeAparicion[Random.Range(0, puntosDeAparicion.Length)];
            if (punto != null) { sitio = punto.position; return true; }
        }

        if (area != null)
            return area.PuntoLibreAleatorio(out sitio, distanciaMinimaDelJugador, jugador);

        Debug.LogWarning("[SpawnerEnemigos] Sin puntos de aparicion ni AreaJugable, " +
                         "no se donde poner al enemigo.", this);
        sitio = transform.position;
        return false;
    }

    // Los destruidos quedan como null en la lista; hay que sacarlos o el tope
    // de vivos deja de funcionar.
    private void LimpiarMuertos()
    {
        vivos.RemoveAll(e => e == null);
    }

    /// <summary>Para de generar. Llamalo al ganar o al perder.</summary>
    public void Detener() => activo = false;

    /// <summary>Sube la dificultad de golpe: arranca mas rapido y admite mas enemigos.</summary>
    public void AplicarNivel(int nivel)
    {
        int extra = Mathf.Max(0, nivel - 1);
        intervalo = Mathf.Max(intervaloMinimo, intervaloInicial - extra * 0.5f);
        maximoVivos += extra * 2;
    }

    private void OnDrawGizmosSelected()
    {
        if (puntosDeAparicion == null) return;

        Gizmos.color = new Color(1f, 0.3f, 0.3f, 0.9f);
        foreach (var p in puntosDeAparicion)
            if (p != null) Gizmos.DrawWireSphere(p.position, 0.5f);
    }
}
