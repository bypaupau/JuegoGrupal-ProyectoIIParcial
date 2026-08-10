using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMovement2D : MonoBehaviour
{
    public float velocidad = 6f;

    // Espacio que se deja en los bordes para que el sprite no se corte.
    public float margen = 0.5f;

    private Rigidbody2D myRigidbody2D;
    private Camera camara;

    void Start()
    {
        myRigidbody2D = GetComponent<Rigidbody2D>();
        camara = Camera.main;
    }

    void Update()
    {
        // Leer izquierda / derecha 
        float x = 0f;
        if (Keyboard.current.leftArrowKey.isPressed || Keyboard.current.aKey.isPressed)
            x = -1f;
        if (Keyboard.current.rightArrowKey.isPressed || Keyboard.current.dKey.isPressed)
            x = 1f;

        // Mover solo en horizontal
        myRigidbody2D.linearVelocity = new Vector2(x * velocidad, 0f);

        // Calcular los bordes visibles de la camara
        float mitadAncho = camara.orthographicSize * camara.aspect;
        float limiteIzquierdo = camara.transform.position.x - mitadAncho + margen;
        float limiteDerecho  = camara.transform.position.x + mitadAncho - margen;

        // Que no se pase de esos bordes
        Vector3 posicion = transform.position;
        posicion.x = Mathf.Clamp(posicion.x, limiteIzquierdo, limiteDerecho);
        transform.position = posicion;
    }
}