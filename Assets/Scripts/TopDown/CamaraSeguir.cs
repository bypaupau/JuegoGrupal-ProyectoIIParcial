using UnityEngine;

/// <summary>
/// Hace que la camara siga al jugador sin salirse del laberinto.
///
/// Va en el Main Camera. Si le asignas el GeneradorLaberinto, toma los limites
/// solo y espera a que el laberinto exista antes de encuadrar.
///
/// El seguimiento va en LateUpdate: si se hiciera en Update la camara podria
/// moverse antes que el jugador y la imagen tiembla.
/// </summary>
[RequireComponent(typeof(Camera))]
public class CamaraSeguir : MonoBehaviour
{
    [Header("A quien sigue")]
    [SerializeField] private Transform objetivo;

    [Tooltip("Si lo asignas, la camara se limita al rectangulo del laberinto.")]
    [SerializeField] private GeneradorLaberinto generador;

    [Header("Suavizado")]
    [Tooltip("Segundos que tarda en alcanzar al objetivo. 0 = pegada, sin suavizado.")]
    [SerializeField] private float suavizado = 0.15f;

    [Tooltip("Corrimiento fijo, por si quieres la camara un poco arriba del gatito.")]
    [SerializeField] private Vector2 desfase = Vector2.zero;

    [Header("Limites manuales")]
    [Tooltip("Solo se usan si NO hay generador asignado.")]
    [SerializeField] private bool usarLimites = false;
    [SerializeField] private Vector2 limiteMin = new Vector2(-20f, -20f);
    [SerializeField] private Vector2 limiteMax = new Vector2(20f, 20f);

    private Camera cam;
    private Vector3 velocidad;   // lo usa SmoothDamp internamente

    private void Awake()
    {
        cam = GetComponent<Camera>();
    }

    private void Start()
    {
        // Si hay generador, el jugador puede no estar en su sitio todavia.
        // Encuadramos de golpe la primera vez para no ver un barrido feo.
        if (generador != null && generador.Listo) Encuadrar(true);
        else if (generador != null) generador.AlGenerar += () => Encuadrar(true);
        else Encuadrar(true);
    }

    private void LateUpdate()
    {
        Encuadrar(false);
    }

    private void Encuadrar(bool inmediato)
    {
        if (objetivo == null) return;

        Vector3 deseada = new Vector3(objetivo.position.x + desfase.x,
                                      objetivo.position.y + desfase.y,
                                      transform.position.z);

        deseada = Limitar(deseada);

        transform.position = (inmediato || suavizado <= 0f)
            ? deseada
            : Vector3.SmoothDamp(transform.position, deseada, ref velocidad, suavizado);
    }

    /// <summary>
    /// Recorta la posicion para que el borde de la camara no muestre el vacio
    /// de afuera del laberinto. Si el laberinto es mas chico que la pantalla en
    /// algun eje, en ese eje la camara se queda centrada.
    /// </summary>
    private Vector3 Limitar(Vector3 p)
    {
        Vector2 min, max;

        if (generador != null && generador.Listo)
        {
            min = generador.LimitesMundo.min;
            max = generador.LimitesMundo.max;
        }
        else if (usarLimites)
        {
            min = limiteMin;
            max = limiteMax;
        }
        else return p;

        float mitadAlto = cam.orthographicSize;
        float mitadAncho = mitadAlto * cam.aspect;

        // Eje X
        if (max.x - min.x <= mitadAncho * 2f) p.x = (min.x + max.x) * 0.5f;
        else p.x = Mathf.Clamp(p.x, min.x + mitadAncho, max.x - mitadAncho);

        // Eje Y
        if (max.y - min.y <= mitadAlto * 2f) p.y = (min.y + max.y) * 0.5f;
        else p.y = Mathf.Clamp(p.y, min.y + mitadAlto, max.y - mitadAlto);

        return p;
    }

    private void OnDrawGizmosSelected()
    {
        if (generador == null && !usarLimites) return;

        Vector2 min = limiteMin, max = limiteMax;
        if (generador != null && generador.Listo)
        { min = generador.LimitesMundo.min; max = generador.LimitesMundo.max; }

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireCube((min + max) * 0.5f, max - min);
    }
}
