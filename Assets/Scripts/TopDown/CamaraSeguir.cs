using UnityEngine;

/// <summary>
/// Hace que la camara siga al gatito sin salirse de la arena.
///
/// Va en el Main Camera. Si le asignas el AreaJugable, toma los limites de ahi
/// y no hay que escribir ningun numero a mano.
///
/// El seguimiento va en LateUpdate: si se hiciera en Update la camara podria
/// moverse antes que el jugador y la imagen tiembla.
/// </summary>
[RequireComponent(typeof(Camera))]
public class CamaraSeguir : MonoBehaviour
{
    [Header("A quien sigue")]
    [SerializeField] private Transform objetivo;

    [Tooltip("De aqui saca hasta donde puede moverse la camara.")]
    [SerializeField] private AreaJugable area;

    [Header("Suavizado")]
    [Tooltip("Segundos que tarda en alcanzar al objetivo. 0 = pegada, sin suavizado.")]
    [SerializeField] private float suavizado = 0.15f;

    [Tooltip("Corrimiento fijo, por si quieres la camara un poco arriba del gatito.")]
    [SerializeField] private Vector2 desfase = Vector2.zero;

    [Header("Limites manuales")]
    [Tooltip("Solo se usan si no hay AreaJugable asignada.")]
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
        // La primera vez encuadramos de golpe para no ver un barrido feo.
        Encuadrar(true);
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
    /// de afuera. Si la arena es mas chica que la pantalla en algun eje, en ese
    /// eje la camara se queda centrada.
    /// </summary>
    private Vector3 Limitar(Vector3 p)
    {
        Vector2 min, max;

        if (area != null)
        {
            min = area.Limites.min;
            max = area.Limites.max;
        }
        else if (usarLimites)
        {
            min = limiteMin;
            max = limiteMax;
        }
        else return p;

        float mitadAlto = cam.orthographicSize;
        float mitadAncho = mitadAlto * cam.aspect;

        if (max.x - min.x <= mitadAncho * 2f) p.x = (min.x + max.x) * 0.5f;
        else p.x = Mathf.Clamp(p.x, min.x + mitadAncho, max.x - mitadAncho);

        if (max.y - min.y <= mitadAlto * 2f) p.y = (min.y + max.y) * 0.5f;
        else p.y = Mathf.Clamp(p.y, min.y + mitadAlto, max.y - mitadAlto);

        return p;
    }

    private void OnDrawGizmosSelected()
    {
        if (area == null && !usarLimites) return;

        Vector2 min = limiteMin, max = limiteMax;
        if (area != null) { min = area.Limites.min; max = area.Limites.max; }

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireCube((min + max) * 0.5f, max - min);
    }
}
