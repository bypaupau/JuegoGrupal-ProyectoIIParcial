using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class HudCorazones : MonoBehaviour
{
    public Image corazonPrefab;   // un prefab de Image (un corazon)
    public Sprite spriteLleno;    // HealthUI lleno
    public Sprite spriteVacio;    // HealthUI vacio (el de contorno)

    public int vidasSiSeJuegaSuelto = 5;

    [Header("Parpadeo al perder")]
    public int parpadeos = 3;
    public float duracionParpadeo = 0.1f;

    private List<Image> corazones = new List<Image>();
    private int vidasAnteriores;

    void OnEnable()  { Partida.AlCambiarVidas += ActualizarVidas; }
    void OnDisable() { Partida.AlCambiarVidas -= ActualizarVidas; }

    void Start()
    {
        // Para poder probar la escena sola (sin pasar por el menu).
        if (!Partida.EnCurso)
            Partida.Comenzar(vidasSiSeJuegaSuelto);

        // Crea un corazon lleno por cada vida inicial.
        int vidasIniciales = Partida.Vidas;
        for (int i = 0; i < vidasIniciales; i++)
        {
            Image c = Instantiate(corazonPrefab, transform);
            c.sprite = spriteLleno;
            corazones.Add(c);
        }
        vidasAnteriores = vidasIniciales;
    }

    private void ActualizarVidas(int vidas)
    {
        // Vacia (con parpadeo) los corazones que se acaban de perder.
        for (int i = vidasAnteriores - 1; i >= vidas; i--)
        {
            if (i >= 0 && i < corazones.Count)
                StartCoroutine(ParpadearYVaciar(corazones[i]));
        }
        vidasAnteriores = vidas;
    }

    private IEnumerator ParpadearYVaciar(Image corazon)
    {
        // Parpadea...
        for (int p = 0; p < parpadeos; p++)
        {
            corazon.enabled = false;
            yield return new WaitForSeconds(duracionParpadeo);
            corazon.enabled = true;
            yield return new WaitForSeconds(duracionParpadeo);
        }
        // ...y queda vacio.
        corazon.sprite = spriteVacio;
    }
}