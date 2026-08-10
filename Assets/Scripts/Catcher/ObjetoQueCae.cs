using UnityEngine;

public class ObjetoQueCae : MonoBehaviour
{
    public float velocidadCaida = 3f;

    private Camera camara;
    private float limiteInferior;

    void Start()
    {
        camara = Camera.main;
        // El borde de abajo de la pantalla, con 1 unidad extra para que se
        // destruya un poquito despues de desaparecer de la vista.
        limiteInferior = camara.transform.position.y - camara.orthographicSize - 1f;
    }

    void Update()
    {
        // Caer hacia abajo
        transform.Translate(Vector3.down * velocidadCaida * Time.deltaTime);

        // Si ya salio por abajo, destruirlo (asi no se acumulan infinitos)
        if (transform.position.y < limiteInferior)
        {
            Destroy(gameObject);
        }
    }
}