using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>
/// Enemigo que deambula por la arena y persigue al gatito si lo tiene cerca
/// y a la vista. Cuando lo pierde, vuelve a deambular.
///
/// Deambular: elige una direccion al azar y camina hasta que se le acaba el
/// tiempo o choca con algo. No usa raycasts para detectar paredes; deja que la
/// fisica le avise con OnCollisionEnter2D, que es mas barato y mas fiable en
/// un mapa de tiles.
///
/// Montaje del prefab:
///   Rigidbody2D  -> Dynamic, Gravity Scale 0, Freeze Rotation Z, Interpolate
///   Collider2D   -> un poco mas chico que el sprite
///   SpriteRenderer -> Sorting Layer: Personajes
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class Enemigo : MonoBehaviour
{
    [Header("Velocidades")]
    [SerializeField] private float velocidadDeambular = 1.5f;
    [SerializeField] private float velocidadPerseguir = 2.5f;

    [Header("Vision")]
    [Tooltip("A que distancia detecta al gatito.")]
    [SerializeField] private float radioVision = 5f;

    [Tooltip("Si esta marcado, solo persigue cuando no hay una pared en medio. " +
             "Si lo desmarcas, lo detecta a traves de los muros.")]
    [SerializeField] private bool requiereLineaDeVista = true;

    [Tooltip("Deja de perseguir un poco mas lejos de lo que empieza, " +
             "para que no titile al borde del radio.")]
    [SerializeField] private float margenDeAbandono = 1.5f;

    [Header("Deambular")]
    [SerializeField] private float tiempoMinimoPorDireccion = 0.8f;
    [SerializeField] private float tiempoMaximoPorDireccion = 2.5f;

    [Tooltip("Si esta marcado, solo se mueve en 4 direcciones. " +
             "Desmarcado, en cualquier angulo.")]
    [SerializeField] private bool soloCuatroDirecciones = true;

    [Header("Sprite")]
    [SerializeField] private SpriteRenderer sprite;

    [Header("Animaciones (opcionales)")]
    [Tooltip("Si lo dejas vacio se busca en este objeto y sus hijos.")]
    [SerializeField] private AnimacionSprites animador;

    [Tooltip("Frames de lado. Se voltean solos para ir a la izquierda, " +
             "asi que no hace falta dibujarlos dos veces.")]
    [SerializeField] private Sprite[] animacionLado;

    [Tooltip("Opcional. Si lo dejas vacio usa la de lado para todo.")]
    [SerializeField] private Sprite[] animacionArriba;

    [Tooltip("Opcional. Si lo dejas vacio usa la de lado para todo.")]
    [SerializeField] private Sprite[] animacionAbajo;

    /// <summary>True si ahora mismo esta persiguiendo. Util para el Animator.</summary>
    public bool Persiguiendo { get; private set; }

    private Rigidbody2D rb;
    private Transform jugador;
    private Vector2 direccion;
    private float tiempoParaCambiar;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        if (sprite == null) sprite = GetComponentInChildren<SpriteRenderer>();
        if (animador == null) animador = GetComponentInChildren<AnimacionSprites>();

        // El error mas comun: RequireComponent agrega el Rigidbody2D solo, pero
        // Collider2D es abstracto y Unity no puede elegir cual poner. Sin el, el
        // enemigo atraviesa todas las paredes.
        if (GetComponent<Collider2D>() == null)
            Debug.LogError("[Enemigo] No tiene Collider2D, por eso atraviesa las paredes. " +
                           "Agregale un BoxCollider2D o un CircleCollider2D.", this);

        if (!Mathf.Approximately(rb.gravityScale, 0f))
            Debug.LogWarning("[Enemigo] Pon Gravity Scale en 0 o el enemigo se cae.", this);
        if (!rb.freezeRotation)
            Debug.LogWarning("[Enemigo] Marca Freeze Rotation Z o va a girar al chocar.", this);
    }

    private void Start()
    {
        // El gatito se registra solo en su OnEnable, y todos los OnEnable
        // corren antes que cualquier Start. Asi no hace falta buscarlo.
        if (MovimientoTopDown.Actual != null)
        {
            jugador = MovimientoTopDown.Actual.transform;
            IgnorarChoquesConElJugador();
        }

        NuevaDireccion();
    }

    /// <summary>
    /// Apaga la colision fisica entre este enemigo y el gatito.
    ///
    /// Sin esto el enemigo, que es un Rigidbody2D Dynamic empujando a 2.5 u/s
    /// hacia el jugador, se le pega encima y lo aplasta contra la pared: el
    /// gatito queda clavado y parece que el script de movimiento no funciona.
    /// Con varios enemigos vivos a la vez ya es imposible caminar.
    ///
    /// Se ignora la colision en vez de poner el collider como Trigger porque el
    /// Trigger tambien deja de chocar con las paredes y el enemigo las atraviesa.
    ///
    /// Si mas adelante hay que quitarle vida al jugador al tocarlo, NO se puede
    /// usar OnCollisionEnter2D ni OnTriggerEnter2D con el (aqui se apaga el
    /// contacto): hay que medir la distancia, o pasar a usar layers de fisica.
    /// </summary>
    private void IgnorarChoquesConElJugador()
    {
        var mios = GetComponentsInChildren<Collider2D>();
        var delGatito = MovimientoTopDown.Actual.GetComponentsInChildren<Collider2D>();

        foreach (var mio in mios)
            foreach (var suyo in delGatito)
                Physics2D.IgnoreCollision(mio, suyo);
    }

    private void Update()
    {
        Persiguiendo = DeberiaPerseguir();

        if (Persiguiendo)
        {
            direccion = ((Vector2)jugador.position - rb.position).normalized;
        }
        else
        {
            tiempoParaCambiar -= Time.deltaTime;
            if (tiempoParaCambiar <= 0f) NuevaDireccion();
        }

        if (sprite != null && Mathf.Abs(direccion.x) > 0.01f)
            sprite.flipX = direccion.x < 0f;

        ActualizarAnimacion();
    }

    /// <summary>
    /// Elige la animacion segun el eje con mas peso. Si no le pusiste frames de
    /// arriba o de abajo, usa la de lado para todo, que con el volteo ya se ve
    /// bien en la mayoria de los sprites.
    /// </summary>
    private void ActualizarAnimacion()
    {
        if (animador == null) return;

        Sprite[] elegida = animacionLado;

        if (Mathf.Abs(direccion.x) < Mathf.Abs(direccion.y))
        {
            if (direccion.y > 0f && animacionArriba != null && animacionArriba.Length > 0)
                elegida = animacionArriba;
            else if (direccion.y <= 0f && animacionAbajo != null && animacionAbajo.Length > 0)
                elegida = animacionAbajo;
        }

        if (elegida != null && elegida.Length > 0) animador.Reproducir(elegida);
    }

    private void FixedUpdate()
    {
        float v = Persiguiendo ? velocidadPerseguir : velocidadDeambular;
        rb.linearVelocity = direccion * v;
    }

    private bool DeberiaPerseguir()
    {
        if (jugador == null) return false;

        float distancia = Vector2.Distance(rb.position, jugador.position);

        // Histeresis: para dejar de perseguir hay que alejarse un poco mas
        // de lo que costo que empezara. Sin esto parpadea en el borde.
        float limite = Persiguiendo ? radioVision + margenDeAbandono : radioVision;
        if (distancia > limite) return false;

        if (!requiereLineaDeVista) return true;

        return !HayParedEntre(rb.position, jugador.position);
    }

    // Mira si el segmento entre los dos puntos cruza el collider del tilemap.
    // No usa LayerMask a proposito: el proyecto todavia no tiene layers de
    // fisica definidas, asi que se filtra por componente.
    private static bool HayParedEntre(Vector2 desde, Vector2 hasta)
    {
        foreach (var golpe in Physics2D.LinecastAll(desde, hasta))
            if (golpe.collider.GetComponent<TilemapCollider2D>() != null) return true;

        return false;
    }

    private void NuevaDireccion()
    {
        if (soloCuatroDirecciones)
        {
            Vector2[] opciones = { Vector2.up, Vector2.down, Vector2.left, Vector2.right };
            direccion = opciones[Random.Range(0, opciones.Length)];
        }
        else
        {
            float angulo = Random.Range(0f, Mathf.PI * 2f);
            direccion = new Vector2(Mathf.Cos(angulo), Mathf.Sin(angulo));
        }

        tiempoParaCambiar = Random.Range(tiempoMinimoPorDireccion, tiempoMaximoPorDireccion);
    }

    // Si choca mientras deambula, se da la vuelta. Si va persiguiendo no hace
    // nada: la direccion se recalcula sola en el siguiente Update.
    private void OnCollisionEnter2D(Collision2D choque)
    {
        if (!Persiguiendo) NuevaDireccion();
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.4f, 0.3f, 0.7f);
        Gizmos.DrawWireSphere(transform.position, radioVision);
    }
}
