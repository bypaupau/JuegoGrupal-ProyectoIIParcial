using UnityEngine;

/// <summary>
/// Moneda recolectable. Al tocarla el gatito, avisa a quien la creo y se destruye.
///
/// Montaje del prefab:
///   SpriteRenderer  -> Sorting Layer: Objetos
///   Collider2D      -> Is Trigger MARCADO
///
/// Ojo con la trampa numero uno de Unity 2D: OnTriggerEnter2D solo dispara si
/// al menos uno de los dos objetos tiene Rigidbody2D. Aqui lo pone el gatito,
/// asi que la moneda NO necesita uno.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class Moneda : MonoBehaviour
{
    [Tooltip("Cuanto suma al puntaje.")]
    [SerializeField] private int puntos = 1;

    [Tooltip("Opcional. Sonido al recogerla.")]
    [SerializeField] private AudioClip sonido;

    [Tooltip("Opcional. Efecto que aparece donde estaba la moneda.")]
    [SerializeField] private GameObject efectoAlRecoger;

    public int Puntos => puntos;

    /// <summary>Lo escucha el SpawnerMonedas para llevar la cuenta.</summary>
    public event System.Action<Moneda> AlRecoger;

    private bool yaRecogida;

    // Al agregar el componente en el editor, deja el collider como trigger.
    private void Reset()
    {
        var col = GetComponent<Collider2D>();
        if (col != null) col.isTrigger = true;
    }

    private void Awake()
    {
        if (!GetComponent<Collider2D>().isTrigger)
            Debug.LogWarning("[Moneda] El Collider2D tiene que estar en Is Trigger " +
                             "o el gatito va a chocar contra la moneda en vez de recogerla.", this);
    }

    private void OnTriggerEnter2D(Collider2D otro)
    {
        if (yaRecogida) return;

        // Se identifica al jugador por su script, no por tag: asi no hay que
        // configurar tags y no se rompe si alguien escribe mal el nombre.
        if (otro.GetComponentInParent<MovimientoTopDown>() == null) return;

        yaRecogida = true;

        AlRecoger?.Invoke(this);

        if (sonido != null) AudioSource.PlayClipAtPoint(sonido, transform.position);
        if (efectoAlRecoger != null) Instantiate(efectoAlRecoger, transform.position, Quaternion.identity);

        Destroy(gameObject);
    }
}
