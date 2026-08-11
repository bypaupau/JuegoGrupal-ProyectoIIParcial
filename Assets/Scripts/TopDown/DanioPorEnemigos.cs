using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Le quita una vida al gatito cuando lo toca un enemigo, y le da unos
/// segundos de invulnerabilidad parpadeando para que no lo maten en cadena.
///
/// Va en el mismo GameObject que MovimientoTopDown.
///
/// POR QUE NO USA OnTriggerEnter2D NI OnCollisionEnter2D:
/// Enemigo.Start() llama a Physics2D.IgnoreCollision entre el enemigo y el
/// gatito. Eso hay que mantenerlo: sin ello los enemigos empujan al gatito
/// contra los muros y lo dejan clavado. Pero apagar el contacto tambien apaga
/// los dos eventos, asi que aqui se pregunta a la fisica directamente con
/// OverlapCircle, que SI ve los colliders aunque no choquen entre ellos.
/// </summary>
[RequireComponent(typeof(MovimientoTopDown))]
public class DanioPorEnemigos : MonoBehaviour
{
    [Header("Contacto")]
    [Tooltip("A que distancia se considera que un enemigo lo toco. Un poco mas " +
             "que medio gatito mas medio enemigo.")]
    [SerializeField] private float radioDeContacto = 0.6f;

    [Header("Invulnerabilidad")]
    [Tooltip("Segundos sin poder recibir mas dano despues de un golpe.")]
    [SerializeField] private float segundosInvulnerable = 1.5f;

    [Tooltip("Parpadeos por segundo mientras es invulnerable.")]
    [SerializeField] private float parpadeosPorSegundo = 6f;

    [Header("Sprite")]
    [Tooltip("El que parpadea. Si lo dejas vacio se busca solo.")]
    [SerializeField] private SpriteRenderer sprite;

    [Header("Sonido")]
    [Tooltip("Opcional. Suena cada vez que un enemigo lo toca.")]
    [SerializeField] private AudioClip sonidoGolpe;

    [Range(0f, 1f)]
    [Tooltip("Volumen del golpe, de 0 a 1.")]
    [SerializeField] private float volumenGolpe = 1f;

    [Header("Pruebas")]
    [Tooltip("Vidas que se dan si le das Play directamente a esta escena sin " +
             "pasar por el menu. En una partida de verdad manda el selector.")]
    [SerializeField] private int vidasSiSeJuegaSuelto = 5;

    /// <summary>Se dispara en cada golpe. Parametro: las vidas que quedan.</summary>
    public event System.Action<int> AlRecibirGolpe;

    private float invulnerableHasta;

    // Se reutilizan entre frames para no generar basura en cada consulta.
    private readonly List<Collider2D> tocados = new List<Collider2D>();
    private ContactFilter2D filtro;

    // Por donde sale el sonido del golpe. Se crea sola si no hay ninguna.
    private AudioSource fuente;

    private bool EsInvulnerable => Time.time < invulnerableHasta;

    private void Awake()
    {
        if (sprite == null) sprite = GetComponentInChildren<SpriteRenderer>();

        // NoFilter: interesan todos los colliders, incluidos los triggers.
        // Ya se descarta lo que no sea enemigo mirando el componente.
        filtro = new ContactFilter2D().NoFilter();

        // Aqui si conviene un AudioSource propio, al reves que en Moneda: el
        // gatito no se destruye al recibir el golpe, asi que el componente
        // sigue vivo para terminar de sonar.
        fuente = GetComponent<AudioSource>();
        if (fuente == null) fuente = gameObject.AddComponent<AudioSource>();

        // Sin esto sonaria una vez al arrancar la escena.
        fuente.playOnAwake = false;

        // 0 = sonido plano, se oye igual este donde este el gatito. Si se deja
        // en 3D, el volumen cambia segun lo lejos que ande de la camara.
        fuente.spatialBlend = 0f;
    }

    private void Start()
    {
        // Para poder probar el laberinto suelto sin arrancar desde el menu.
        if (!Partida.EnCurso)
        {
            Partida.Comenzar(vidasSiSeJuegaSuelto);
            Debug.LogWarning($"[DanioPorEnemigos] No venias del menu, asi que empiezo " +
                             $"una partida de prueba con {Partida.Vidas} vidas.", this);
        }
    }

    private void Update()
    {
        Parpadear();

        if (EsInvulnerable || !Partida.EnCurso) return;
        if (!HayEnemigoTocando()) return;

        Golpe();
    }

    private bool HayEnemigoTocando()
    {
        // Esta sobrecarga con lista es la de Unity 6. La vieja de toda la vida,
        // OverlapCircleAll, crea un array nuevo en cada llamada, y esto corre
        // en cada frame.
        Physics2D.OverlapCircle((Vector2)transform.position, radioDeContacto, filtro, tocados);

        foreach (var col in tocados)
            if (col != null && col.GetComponentInParent<Enemigo>() != null)
                return true;

        return false;
    }

    private void Golpe()
    {
        invulnerableHasta = Time.time + segundosInvulnerable;

        // PlayOneShot y no Play: se pueden encimar varios golpes sin que uno
        // corte al anterior, y no pisa el clip que tenga puesto el AudioSource.
        if (sonidoGolpe != null) fuente.PlayOneShot(sonidoGolpe, volumenGolpe);

        bool seAcabo = Partida.QuitarVida();

        AlRecibirGolpe?.Invoke(Partida.Vidas);

        Debug.Log($"[DanioPorEnemigos] Golpe. Quedan {Partida.Vidas} vidas.", this);

        if (seAcabo) Parar();
    }

    private void Parar()
    {
        // Se apaga el movimiento y se deja el sprite visible, sin parpadeo a
        // medias. Del resto (pantalla de derrota) se encarga el GestorTopDown.
        GetComponent<MovimientoTopDown>().enabled = false;

        var rb = GetComponent<Rigidbody2D>();
        if (rb != null) rb.linearVelocity = Vector2.zero;

        invulnerableHasta = 0f;
        if (sprite != null) sprite.enabled = true;
    }

    private void Parpadear()
    {
        if (sprite == null) return;

        if (!EsInvulnerable)
        {
            if (!sprite.enabled) sprite.enabled = true;
            return;
        }

        // El seno alterna entre positivo y negativo a la frecuencia que pidas;
        // solo hace falta mirarle el signo para encender y apagar.
        sprite.enabled = Mathf.Sin(Time.time * parpadeosPorSegundo * Mathf.PI * 2f) > 0f;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.3f, 0.3f, 0.8f);
        Gizmos.DrawWireSphere(transform.position, radioDeContacto);
    }
}
