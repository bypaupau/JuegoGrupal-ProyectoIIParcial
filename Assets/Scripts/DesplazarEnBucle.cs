using UnityEngine;

/// <summary>
/// Mueve el objeto horizontalmente y lo reposiciona al otro extremo cuando
/// sale de pantalla. Sirve para las nubes del fondo y para el gatito que
/// cruza corriendo en el menu.
///
/// Velocidad positiva = se mueve a la derecha (reaparece por la izquierda).
/// Velocidad negativa = se mueve a la izquierda (reaparece por la derecha).
/// </summary>
public class DesplazarEnBucle : MonoBehaviour
{
    [Tooltip("Unidades por segundo. Negativo para ir hacia la izquierda.")]
    [SerializeField] private float velocidad = 1f;

    [Header("Limites en X (unidades de mundo)")]
    [SerializeField] private float limiteIzquierdo = -12f;
    [SerializeField] private float limiteDerecho = 12f;

    private void Update()
    {
        transform.Translate(Vector3.right * velocidad * Time.deltaTime);

        Vector3 p = transform.position;

        if (velocidad > 0f && p.x > limiteDerecho)
            p.x = limiteIzquierdo;
        else if (velocidad < 0f && p.x < limiteIzquierdo)
            p.x = limiteDerecho;
        else
            return;

        transform.position = p;
    }

    // Dibuja los limites en la vista Scene para poder ajustarlos a ojo.
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        float y = transform.position.y;
        Gizmos.DrawLine(new Vector3(limiteIzquierdo, y - 1f), new Vector3(limiteIzquierdo, y + 1f));
        Gizmos.DrawLine(new Vector3(limiteDerecho, y - 1f), new Vector3(limiteDerecho, y + 1f));
    }
}
