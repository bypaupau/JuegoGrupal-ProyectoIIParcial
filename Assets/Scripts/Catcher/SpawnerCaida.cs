using UnityEngine;
using System.Collections;

public class SpawnerCaida : MonoBehaviour
{
    public GameObject[] objetosPrefab;   // los objetos que pueden caer

    public float tiempoMinimo = 0.7f;
    public float tiempoMaximo = 1.5f;

    // Que tan lejos a izquierda y derecha pueden aparecer
    public float rangoX = 7f;

    void Start()
    {
        StartCoroutine(SpawnCorutine(0f));
    }

    IEnumerator SpawnCorutine(float tiempoEspera)
    {
        yield return new WaitForSeconds(tiempoEspera);

        // Posicion arriba (la Y del Spawner) con X al azar
        float x = Random.Range(-rangoX, rangoX);
        Vector3 posicion = new Vector3(x, transform.position.y, 0f);

        // Elegir un objeto al azar del arreglo y crearlo
        GameObject prefab = objetosPrefab[Random.Range(0, objetosPrefab.Length)];
        Instantiate(prefab, posicion, Quaternion.identity);

        // Repetir, esperando un tiempo al azar (dificultad la ajustamos despues)
        StartCoroutine(SpawnCorutine(Random.Range(tiempoMinimo, tiempoMaximo)));
    }
}