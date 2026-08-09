using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Movimiento del gatito en el laberinto: ocho direcciones con WASD o flechas.
///
/// Es solo del minijuego TopDown. El Catcher tiene el suyo aparte, asi que este
/// script se puede cambiar sin avisarle a nadie.
///
/// IMPORTANTE: el proyecto tiene Active Input Handling = Input System (nuevo),
/// asi que NO se puede usar Input.GetAxisRaw. Eso compila pero lanza
/// InvalidOperationException al darle Play. Aqui se lee con Keyboard.current,
/// que es la API nueva y no necesita configurar ningun asset.
///
/// El input se lee en Update (en FixedUpdate se perderian pulsaciones) y el
/// movimiento se aplica en FixedUpdate con linearVelocity (tocar
/// transform.position directamente hace que el gatito atraviese paredes).
///
/// Montaje:
///   Rigidbody2D  -> Dynamic, Gravity Scale 0, Freeze Rotation Z, Interpolate
///   Collider2D   -> mas chico que el sprite (~0.7) o se atora en las esquinas
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class MovimientoTopDown : MonoBehaviour
{
    [Header("Movimiento")]
    [Tooltip("Unidades por segundo. Con tiles de 16 px y pasillos de 2, 4 se siente comodo.")]
    [SerializeField] private float velocidad = 4f;

    [Tooltip("Si esta marcado, en diagonal no va mas rapido que en recto.")]
    [SerializeField] private bool normalizarDiagonal = true;

    [Header("Sprite")]
    [Tooltip("Voltea el sprite segun hacia donde camina. Se busca solo si lo dejas vacio.")]
    [SerializeField] private SpriteRenderer sprite;

    /// <summary>Direccion actual del input. Util para el Animator.</summary>
    public Vector2 Direccion { get; private set; }

    /// <summary>True si se esta moviendo. Util para el Animator.</summary>
    public bool EnMovimiento => Direccion.sqrMagnitude > 0.01f;

    private Rigidbody2D rb;
    private Vector2 entrada;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        if (sprite == null) sprite = GetComponentInChildren<SpriteRenderer>();

        // Errores tipicos de montaje: mejor avisarlos que perseguirlos despues.
        if (rb.bodyType != RigidbodyType2D.Dynamic)
            Debug.LogWarning("[MovimientoTopDown] El Rigidbody2D deberia ser Dynamic.", this);
        if (!Mathf.Approximately(rb.gravityScale, 0f))
            Debug.LogWarning("[MovimientoTopDown] Pon Gravity Scale en 0 o el gatito se cae.", this);
        if (!rb.freezeRotation)
            Debug.LogWarning("[MovimientoTopDown] Marca Freeze Rotation Z o el gatito gira al chocar.", this);
    }

    private void Update()
    {
        entrada = LeerTeclado();

        if (normalizarDiagonal && entrada.sqrMagnitude > 1f)
            entrada = entrada.normalized;

        Direccion = entrada;

        if (sprite != null && Mathf.Abs(entrada.x) > 0.01f)
            sprite.flipX = entrada.x < 0f;
    }

    private void FixedUpdate()
    {
        rb.linearVelocity = entrada * velocidad;
    }

    // WASD y flechas. Keyboard.current es null si no hay teclado conectado.
    private static Vector2 LeerTeclado()
    {
        var t = Keyboard.current;
        if (t == null) return Vector2.zero;

        float x = 0f, y = 0f;

        if (t.aKey.isPressed || t.leftArrowKey.isPressed) x -= 1f;
        if (t.dKey.isPressed || t.rightArrowKey.isPressed) x += 1f;
        if (t.sKey.isPressed || t.downArrowKey.isPressed) y -= 1f;
        if (t.wKey.isPressed || t.upArrowKey.isPressed) y += 1f;

        return new Vector2(x, y);
    }
}
