using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

/// <summary>
/// Movimiento 2D del jugador con Rigidbody2D. Lee WASD y flechas.
/// Opcionalmente limita al jugador a lo que ve la camara (ideal para el
/// Catcher, con camara fija) y permite bloquear el eje vertical.
///
/// Es un script COMUN: lo usan el TopDown y el Catcher. Avisar antes de tocarlo.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class PlayerMovement2D : MonoBehaviour
{
    [Header("Movimiento")]
    [Tooltip("Unidades de mundo por segundo.")]
    [SerializeField] private float velocidad = 6f;

    [Tooltip("Desmarcado = solo se mueve en horizontal (clasico de Catcher).")]
    [SerializeField] private bool permitirVertical = true;

    [Header("Limites de camara")]
    [Tooltip("Mantiene al jugador dentro del encuadre. Usar con camara FIJA.")]
    [SerializeField] private bool limitarACamara = true;

    [Tooltip("Margen en los bordes, en unidades. ~medio sprite para que no se corte.")]
    [SerializeField] private float margen = 0.5f;

    private Rigidbody2D rb;
    private Camera camara;
    private Vector2 entrada;

    private void Awake()
    {
        // Guardamos referencias en Awake, nunca dentro de Update (rendimiento).
        rb = GetComponent<Rigidbody2D>();
        camara = Camera.main;
    }

    private void Update()
    {
        // El INPUT se lee en Update para no perder pulsaciones.
        float x = 0f, y = 0f;
#if ENABLE_INPUT_SYSTEM
        var kb = Keyboard.current;
        if (kb != null)
        {
            if (kb.aKey.isPressed || kb.leftArrowKey.isPressed)  x -= 1f;
            if (kb.dKey.isPressed || kb.rightArrowKey.isPressed) x += 1f;
            if (kb.wKey.isPressed || kb.upArrowKey.isPressed)    y += 1f;
            if (kb.sKey.isPressed || kb.downArrowKey.isPressed)  y -= 1f;
        }
#elif ENABLE_LEGACY_INPUT_MANAGER
        x = Input.GetAxisRaw("Horizontal");
        y = Input.GetAxisRaw("Vertical");
#endif
        if (!permitirVertical) y = 0f;

        entrada = new Vector2(x, y);
        // Normalizar para que en diagonal no vaya mas rapido.
        if (entrada.sqrMagnitude > 1f) entrada = entrada.normalized;
    }

    private void FixedUpdate()
    {
        // El MOVIMIENTO por fisica va en FixedUpdate con MovePosition:
        // tocar transform.position directo atraviesa paredes.
        Vector2 destino = rb.position + entrada * velocidad * Time.fixedDeltaTime;

        if (limitarACamara && camara != null)
            destino = ClampACamara(destino);

        rb.MovePosition(destino);
    }

    // Deriva los limites del encuadre en vez de hardcodear numeros.
    private Vector2 ClampACamara(Vector2 p)
    {
        float mitadAlto  = camara.orthographicSize;
        float mitadAncho = mitadAlto * camara.aspect;
        Vector3 c = camara.transform.position;

        p.x = Mathf.Clamp(p.x, c.x - mitadAncho + margen, c.x + mitadAncho - margen);
        p.y = Mathf.Clamp(p.y, c.y - mitadAlto  + margen, c.y + mitadAlto  - margen);
        return p;
    }
}