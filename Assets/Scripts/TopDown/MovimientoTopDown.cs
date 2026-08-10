using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Tilemaps;

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
/// movimiento se aplica en FixedUpdate sobre el Rigidbody2D (tocar
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

    [Tooltip("Marcalo si quieres movimiento solo en cruz (arriba, abajo, izquierda, " +
             "derecha) y nada de diagonales. Si pulsas dos teclas manda la de mas peso.")]
    [SerializeField] private bool soloCuatroDirecciones = false;

    [Header("Sprite")]
    [Tooltip("Voltea el sprite segun hacia donde camina. Se busca solo si lo dejas vacio.")]
    [SerializeField] private SpriteRenderer sprite;

    [Header("Animaciones (opcionales)")]
    [Tooltip("Si lo dejas vacio se busca en este objeto y sus hijos.")]
    [SerializeField] private AnimacionSprites animador;

    [Tooltip("Frames caminando de lado. Se voltean solos para ir a la izquierda.")]
    [SerializeField] private Sprite[] animacionLado;

    [SerializeField] private Sprite[] animacionArriba;
    [SerializeField] private Sprite[] animacionAbajo;

    [Tooltip("Frames cuando esta parado. Si lo dejas vacio, se queda en la ultima animacion.")]
    [SerializeField] private Sprite[] animacionQuieto;

    /// <summary>Direccion actual del input. Util para el Animator.</summary>
    public Vector2 Direccion { get; private set; }

    /// <summary>True si se esta moviendo. Util para el Animator.</summary>
    public bool EnMovimiento => Direccion.sqrMagnitude > 0.01f;

    /// <summary>
    /// El jugador que hay ahora mismo en la escena. Los enemigos y los spawners
    /// lo leen de aqui en vez de buscarlo con FindObjectByType, que ademas de
    /// lento quedo obsoleto en Unity 6.5.
    /// </summary>
    public static MovimientoTopDown Actual { get; private set; }

    [Header("Diagnostico")]
    [Tooltip("Escribe en la Console una vez por segundo lo que esta leyendo. " +
             "Desmarcalo cuando ya funcione.")]
    [SerializeField] private bool mostrarDiagnostico;

    private Rigidbody2D rb;
    private Vector2 entrada;
    private float proximoAviso;

    private void OnEnable() => Actual = this;

    private void OnDisable() { if (Actual == this) Actual = null; }

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        if (sprite == null) sprite = GetComponentInChildren<SpriteRenderer>();
        if (animador == null) animador = GetComponentInChildren<AnimacionSprites>();

        // Un Rigidbody2D quieto se duerme para ahorrar calculos. En el jugador
        // eso solo trae problemas: si se duerme mientras no pulsas nada, puede
        // no reaccionar al primer input. El personaje nunca debe dormirse.
        rb.sleepMode = RigidbodySleepMode2D.NeverSleep;

        // Errores tipicos de montaje: mejor avisarlos que perseguirlos despues.
        if (rb.bodyType != RigidbodyType2D.Dynamic)
            Debug.LogWarning("[MovimientoTopDown] El Rigidbody2D deberia ser Dynamic.", this);
        if (!Mathf.Approximately(rb.gravityScale, 0f))
            Debug.LogWarning("[MovimientoTopDown] Pon Gravity Scale en 0 o el gatito se cae.", this);
        if (!rb.freezeRotation)
            Debug.LogWarning("[MovimientoTopDown] Marca Freeze Rotation Z o el gatito gira al chocar.", this);
    }

    private void Start()
    {
        // Esta comprobacion va siempre, sin depender del toggle de diagnostico:
        // si el Input System no ve teclado, nada de lo demas puede funcionar y
        // conviene enterarse en el primer segundo.
        if (Keyboard.current == null)
            Debug.LogError("[MovimientoTopDown] El Input System no detecta ningun teclado. " +
                           "Revisa Edit > Project Settings > Input System Package.", this);

        DespegarDeLasParedes();
    }

    /// <summary>
    /// Si el gatito arranca con su collider metido dentro de una pared, lo saca
    /// al hueco libre mas cercano y avisa en la Console.
    ///
    /// Esto pasa muy facil: colocas el gatito a ojo en la vista Scene, el sprite
    /// se ve bien porque se dibuja encima del muro, pero el collider (0.7 con
    /// desfase -0.2, o sea corrido hacia abajo) se queda medio metido en el tile
    /// de abajo. El resultado en Play es que solo puedes salir hacia los lados
    /// libres, que es justo lo contrario de donde estas encajado: si estas
    /// clavado en la esquina de abajo-izquierda, solo responden arriba y derecha,
    /// y en diagonal, porque es la unica salida.
    ///
    /// Es una red de seguridad, no la solucion: lo correcto es colocar al gatito
    /// centrado en un pasillo desde el editor.
    /// </summary>
    private void DespegarDeLasParedes()
    {
        var propio = GetComponent<Collider2D>();
        if (propio == null)
        {
            Debug.LogError("[MovimientoTopDown] El gatito no tiene Collider2D, " +
                           "asi que atraviesa todas las paredes.", this);
            return;
        }

        // El collider casi nunca esta centrado en el pivote del sprite, asi que
        // se guarda el desfase para poder probar posiciones candidatas.
        Vector2 desfase = (Vector2)propio.bounds.center - rb.position;
        Vector2 tamano = propio.bounds.size * 0.9f;   // 0.9 para no contar el simple roce

        if (!ChocaConPared(rb.position, desfase, tamano)) return;

        for (float radio = 0.1f; radio <= 4f; radio += 0.1f)
        {
            for (int i = 0; i < 16; i++)
            {
                float angulo = i * Mathf.PI * 2f / 16f;
                Vector2 candidato = rb.position +
                    new Vector2(Mathf.Cos(angulo), Mathf.Sin(angulo)) * radio;

                if (ChocaConPared(candidato, desfase, tamano)) continue;

                Debug.LogWarning(
                    $"[MovimientoTopDown] El gatito arrancaba metido dentro de una pared " +
                    $"en {rb.position}. Lo movi a {candidato} para que se pueda mover. " +
                    $"Colocalo bien en la escena (centrado en un pasillo) y este aviso se va.",
                    this);

                rb.position = candidato;
                transform.position = new Vector3(candidato.x, candidato.y, transform.position.z);
                return;
            }
        }

        Debug.LogError("[MovimientoTopDown] El gatito esta dentro de una pared y no encontre " +
                       "ningun hueco libre a menos de 4 unidades. Muevelo a mano en la escena.", this);
    }

    // Se filtra por componente y no por LayerMask porque el proyecto todavia no
    // tiene layers de fisica. El tilemap de Paredes lleva TilemapCollider2D y
    // CompositeCollider2D en el mismo GameObject, asi que preguntar por el
    // TilemapCollider2D funciona aunque quien choque sea el composite.
    private static bool ChocaConPared(Vector2 posicion, Vector2 desfase, Vector2 tamano)
    {
        foreach (var col in Physics2D.OverlapBoxAll(posicion + desfase, tamano, 0f))
            if (col.GetComponent<TilemapCollider2D>() != null) return true;

        return false;
    }

    private void Update()
    {
        entrada = LeerTeclado();

        if (soloCuatroDirecciones && entrada.x != 0f && entrada.y != 0f)
            entrada = Mathf.Abs(entrada.x) >= Mathf.Abs(entrada.y)
                ? new Vector2(entrada.x, 0f)
                : new Vector2(0f, entrada.y);

        if (normalizarDiagonal && entrada.sqrMagnitude > 1f)
            entrada = entrada.normalized;

        Direccion = entrada;

        if (sprite != null && Mathf.Abs(entrada.x) > 0.01f)
            sprite.flipX = entrada.x < 0f;

        ActualizarAnimacion();

        if (mostrarDiagnostico) Diagnostico();
    }

    /// <summary>
    /// Imprime de un vistazo todo lo que puede estar fallando, para no tener
    /// que ir descartando cosas de a una.
    ///
    ///   teclado=NULL      -> el Input System no ve teclado
    ///   entrada=(0,0)     -> llegan las teclas pero no se estan leyendo
    ///   entrada distinta de (0,0) y velocidadRB=(0,0) -> algo lo esta frenando
    ///   simulado=False    -> el Rigidbody2D esta apagado
    ///   escalaTiempo=0    -> el juego esta en pausa
    /// </summary>
    private void Diagnostico()
    {
        if (Time.time < proximoAviso) return;
        proximoAviso = Time.time + 1f;

        Debug.Log($"[MovimientoTopDown] teclado={(Keyboard.current == null ? "NULL" : "ok")} " +
                  $"entrada={entrada} velocidadRB={rb.linearVelocity} " +
                  $"cuerpo={rb.bodyType} simulado={rb.simulated} " +
                  $"escalaTiempo={Time.timeScale} velocidad={velocidad}", this);
    }

    /// <summary>
    /// Elige la animacion segun hacia donde va. Manda el eje con mas peso: si
    /// camina en diagonal se ve la de lado, que es lo que espera el ojo.
    /// Reproducir() ignora la llamada si ya esta esa misma animacion, asi que
    /// no pasa nada por llamarlo cada frame.
    /// </summary>
    private void ActualizarAnimacion()
    {
        if (animador == null) return;

        if (!EnMovimiento)
        {
            if (animacionQuieto != null && animacionQuieto.Length > 0)
                animador.Reproducir(animacionQuieto);
            return;
        }

        if (Mathf.Abs(entrada.x) >= Mathf.Abs(entrada.y))
        {
            if (animacionLado != null && animacionLado.Length > 0)
                animador.Reproducir(animacionLado);
        }
        else if (entrada.y > 0f)
        {
            if (animacionArriba != null && animacionArriba.Length > 0)
                animador.Reproducir(animacionArriba);
        }
        else
        {
            if (animacionAbajo != null && animacionAbajo.Length > 0)
                animador.Reproducir(animacionAbajo);
        }
    }

    private void FixedUpdate()
    {
        // Antes esto era MovePosition. Se cambio a linearVelocity a proposito:
        //
        // MovePosition le exige al cuerpo estar en un punto exacto cada paso de
        // fisica, incluso cuando no pulsas nada (ahi le pide quedarse quieto).
        // Eso pelea contra la correccion que hace Unity para sacar un cuerpo de
        // dentro de un collider, asi que si el gatito queda medio metido en una
        // pared no logra salir nunca, y al cambiar de direccion se siente un
        // tiron raro porque la interpolacion tiene que alcanzar el objetivo.
        //
        // Con linearVelocity el solver puede resolver los contactos: el gatito
        // se desliza a lo largo de las paredes en vez de clavarse en ellas.
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
