using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMovement2D : MonoBehaviour
{
    public float velocidad = 6f;

    // Limites en X para que no se salga de la pantalla.
    // Ajustalos en el Inspector segun tu camara.
    public float limiteIzquierdo = -8f;
    public float limiteDerecho = 8f;

    private Rigidbody2D myRigidbody2D;

    void Start()
    {
        myRigidbody2D = GetComponent<Rigidbody2D>();
    }

    void Update()
    {
        // Leer izquierda / derecha (flechas o A/D)
        float x = 0f;
        if (Keyboard.current.leftArrowKey.isPressed || Keyboard.current.aKey.isPressed)
            x = -1f;
        if (Keyboard.current.rightArrowKey.isPressed || Keyboard.current.dKey.isPressed)
            x = 1f;

        // Mover solo en horizontal
        myRigidbody2D.linearVelocity = new Vector2(x * velocidad, 0f);

        // Que no se pase de los limites
        Vector3 posicion = transform.position;
        posicion.x = Mathf.Clamp(posicion.x, limiteIzquierdo, limiteDerecho);
        transform.position = posicion;
    }
}