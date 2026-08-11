using UnityEngine;

public class ObjetoQueCae : MonoBehaviour
{
    public float velocidadCaida = 3f;

    // Marca el tipo de objeto. Los buenos lo dejan en false;
    // el prefab del item malo lo pondra en true.
    // Mas adelante, atrapar un malo quitara vida.
    public bool esMalo = false;

    void Update()
    {
        // Solo caer. La destruccion la maneja el GarbageController del fondo.
        transform.Translate(Vector3.down * velocidadCaida * Time.deltaTime);
    }
}