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

    [Tooltip("Separacion minima entre enemigos al nacer, para que no salgan " +
             "amontonados. Igual que la separacion entre monedas.")]
    [SerializeField] private float separacionEntreEnemigos = 4f;

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

    [Header("Reciclado")]
    [Tooltip("Como nada mata a los enemigos, sin esto se acumulan hasta el tope " +
             "y el spawner se calla para siempre. Un enemigo que lleva mucho rato " +
             "lejos del gatito se retira y deja su sitio libre.\n\n" +
             "Ponlo en 0 para desactivarlo y que no desaparezca ninguno.")]
    [SerializeField] private float segundosLejosParaRetirarse = 8f;

    [Tooltip("A partir de que distancia se considera que esta lejos. Ponlo mas " +
             "grande que media pantalla o el jugador vera enemigos esfumarse " +
             "delante de sus narices.")]
    [SerializeField] private float distanciaParaRetirarse = 14f;

    /// <summary>Cuantos enemigos hay ahora mismo en la arena.</summary>
    public int Vivos => vivos.Count;

    // Cada enemigo con el tiempo que lleva seguido lejos del gatito.
    private class Vigilado
    {
        public Enemigo enemigo;
        public float segundosLejos;
    }

    private readonly List<Vigilado> vivos = new List<Vigilado>();
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

            bool salio = vivos.Count < maximoVivos && Aparecer();

            // Si no salio (no habia sitio libre, o esta lleno) no se gasta el
            // intervalo entero esperando: se reintenta enseguida. Antes se
            // perdia el ciclo completo y se notaba como un hueco sin enemigos.
            yield return new WaitForSeconds(salio ? intervalo : Mathf.Min(intervalo, 0.5f));

            // El ritmo solo acelera cuando de verdad aparecio alguien.
            if (salio) intervalo = Mathf.Max(intervaloMinimo, intervalo * factorDeAceleracion);
        }
    }

    /// <summary>Devuelve true si consiguio sacar un enemigo.</summary>
    private bool Aparecer()
    {
        if (!BuscarSitio(out Vector3 sitio)) return false;

        var prefab = prefabsEnemigo[Random.Range(0, prefabsEnemigo.Length)];
        var enemigo = Instantiate(prefab, sitio, Quaternion.identity, transform);
        vivos.Add(new Vigilado { enemigo = enemigo });
        return true;
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
        {
            // Varios intentos: el area sabe evitar los muros y al gatito, pero
            // no sabe nada de los otros enemigos. Eso se comprueba aqui.
            for (int intento = 0; intento < 20; intento++)
            {
                if (!area.PuntoLibreAleatorio(out sitio, distanciaMinimaDelJugador, jugador))
                    break;

                if (!HayOtroEnemigoCerca(sitio)) return true;
            }

            sitio = transform.position;
            return false;
        }

        Debug.LogWarning("[SpawnerEnemigos] Sin puntos de aparicion ni AreaJugable, " +
                         "no se donde poner al enemigo.", this);
        sitio = transform.position;
        return false;
    }

    private bool HayOtroEnemigoCerca(Vector3 sitio)
    {
        if (separacionEntreEnemigos <= 0f) return false;

        foreach (var v in vivos)
            if (v.enemigo != null &&
                Vector3.Distance(v.enemigo.transform.position, sitio) < separacionEntreEnemigos)
                return true;

        return false;
    }

    /// <summary>
    /// Retira a los que llevan mucho rato lejos, para que el spawner no se
    /// quede atascado en el tope y siga habiendo movimiento toda la partida.
    /// Se comprueba lejos de la camara, asi que el jugador nunca ve a nadie
    /// desaparecer de golpe.
    /// </summary>
    private void Update()
    {
        if (jugador == null || segundosLejosParaRetirarse <= 0f) return;

        for (int i = vivos.Count - 1; i >= 0; i--)
        {
            var v = vivos[i];

            if (v.enemigo == null) { vivos.RemoveAt(i); continue; }

            float distancia = Vector2.Distance(v.enemigo.transform.position, jugador.position);

            if (distancia < distanciaParaRetirarse)
            {
                v.segundosLejos = 0f;
                continue;
            }

            v.segundosLejos += Time.deltaTime;

            if (v.segundosLejos >= segundosLejosParaRetirarse)
            {
                Destroy(v.enemigo.gameObject);
                vivos.RemoveAt(i);
            }
        }
    }

    // Los destruidos quedan como null en la lista; hay que sacarlos o el tope
    // de vivos deja de funcionar.
    private void LimpiarMuertos()
    {
        vivos.RemoveAll(v => v.enemigo == null);
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
