using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>
/// Define el rectangulo de la arena y sabe responder si un punto esta libre.
///
/// Lo usan los spawners para no soltar monedas ni enemigos dentro de una pared,
/// y la camara para no mostrar el vacio de afuera.
///
/// Para saber si hay pared no mira los tiles: lanza un circulo de fisica y
/// pregunta si toco el collider del tilemap. Asi funciona igual con el mapa
/// pintado a mano, sin que nadie tenga que decirle donde estan los muros.
///
/// Montaje: un GameObject vacio en la escena, colocado en el centro de la arena.
/// Ajusta el tamano mirando el recuadro verde en la vista Scene.
/// </summary>
public class AreaJugable : MonoBehaviour
{
    [Tooltip("Ancho y alto de la arena en unidades. 1 unidad = 1 tile.")]
    [SerializeField] private Vector2 tamano = new Vector2(30f, 20f);

    [Tooltip("Radio que se usa para comprobar si cabe algo en un punto. " +
             "Ponlo un poco mas grande que medio enemigo.")]
    [SerializeField] private float radioDeHolgura = 0.45f;

    public Bounds Limites =>
        new Bounds(transform.position, new Vector3(tamano.x, tamano.y, 0.1f));

    /// <summary>True si el punto esta dentro del area y no toca ninguna pared.</summary>
    public bool EsLibre(Vector2 punto, float radio = -1f)
    {
        if (radio < 0f) radio = radioDeHolgura;

        Vector2 centro = transform.position;
        if (Mathf.Abs(punto.x - centro.x) > tamano.x * 0.5f - radio) return false;
        if (Mathf.Abs(punto.y - centro.y) > tamano.y * 0.5f - radio) return false;

        foreach (var col in Physics2D.OverlapCircleAll(punto, radio))
            if (col.GetComponent<TilemapCollider2D>() != null) return false;

        return true;
    }

    /// <summary>
    /// Busca un punto libre al azar dentro del area. Devuelve false si despues
    /// de tantos intentos no encontro ninguno, que suele significar que el area
    /// esta mal colocada o es demasiado chica.
    /// </summary>
    public bool PuntoLibreAleatorio(out Vector3 punto, float distanciaMinimaA = 0f,
                                    Transform deQuien = null, int intentos = 60)
    {
        Vector2 centro = transform.position;

        for (int i = 0; i < intentos; i++)
        {
            var candidato = new Vector2(
                Random.Range(centro.x - tamano.x * 0.5f, centro.x + tamano.x * 0.5f),
                Random.Range(centro.y - tamano.y * 0.5f, centro.y + tamano.y * 0.5f));

            if (!EsLibre(candidato)) continue;

            if (deQuien != null && distanciaMinimaA > 0f &&
                Vector2.Distance(candidato, deQuien.position) < distanciaMinimaA) continue;

            punto = new Vector3(candidato.x, candidato.y, 0f);
            return true;
        }

        punto = transform.position;
        return false;
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = new Color(0.3f, 1f, 0.4f, 0.9f);
        Gizmos.DrawWireCube(transform.position, new Vector3(tamano.x, tamano.y, 0f));
    }
}
