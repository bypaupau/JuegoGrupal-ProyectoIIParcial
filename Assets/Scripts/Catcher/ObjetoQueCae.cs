using UnityEngine;

public class ObjetoQueCae : MonoBehaviour
{
    public float velocidadCaida = 3f;

    void Update()
    {
        // Solo caer. La destruccion la maneja el GarbageController del fondo.
        transform.Translate(Vector3.down * velocidadCaida * Time.deltaTime);
    }
}