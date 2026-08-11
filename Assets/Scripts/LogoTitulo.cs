using System.Collections;
using TMPro;
using UnityEngine;

/// <summary>
/// Convierte un TextMeshProUGUI en un "logo" animado.
///
/// Hace dos cosas encadenadas:
///   1. ENTRADA en cascada: las letras van cayendo una a una desde arriba,
///      con un rebotito al aterrizar y un fundido propio de cada letra.
///   2. FLOTACION en bucle: cuando termina la entrada, el titulo entero
///      respira con una onda suave para que no quede muerto en pantalla.
///
/// Como funciona por dentro
/// ------------------------
/// TextMeshPro construye una malla donde CADA caracter son 4 vertices.
/// Este script no toca el Transform: lee esa malla, mueve los 4 vertices de
/// cada letra por separado y se la devuelve a TMP. Por eso se puede animar
/// letra por letra sin crear un GameObject por cada una.
///
/// El orden es siempre el mismo:
///   texto.ForceMeshUpdate()  -> obliga a TMP a generar la malla YA
///   textInfo.meshInfo[...]   -> copia de trabajo de los vertices
///   texto.UpdateVertexData() -> sube los cambios a la malla real
///
/// IMPORTANTE: se guarda una copia limpia de los vertices originales
/// (verticesOriginales). Si no, cada frame animariamos sobre lo ya animado
/// y las letras se irian volando al infinito.
/// </summary>
[RequireComponent(typeof(TextMeshProUGUI))]
public class LogoTitulo : MonoBehaviour
{
    [Header("Entrada en cascada")]
    [Tooltip("Cuanto tarda CADA letra en caer y asentarse.")]
    [SerializeField] private float duracionPorLetra = 0.45f;

    [Tooltip("Retraso entre una letra y la siguiente. Mas alto = cascada mas lenta.")]
    [SerializeField] private float separacionEntreLetras = 0.07f;

    [Tooltip("Desde cuantas unidades por encima cae cada letra.")]
    [SerializeField] private float alturaDeCaida = 30f;

    [Tooltip("Cuanto se hunde la letra al aterrizar antes de volver a su sitio. " +
             "0 = sin rebote.")]
    [SerializeField] private float rebote = 6f;

    [Header("Flotacion en bucle")]
    [Tooltip("Si esta apagado, el titulo se queda quieto al terminar la entrada.")]
    [SerializeField] private bool flotarAlTerminar = true;

    [Tooltip("Cuantas unidades sube y baja la onda.")]
    [SerializeField] private float amplitudFlotacion = 1.5f;

    [Tooltip("Velocidad de la onda. Mas alto = mas nervioso.")]
    [SerializeField] private float velocidadFlotacion = 2f;

    [Tooltip("Desfase de la onda entre letras. 0 = todas suben a la vez; " +
             "valores altos = efecto bandera.")]
    [SerializeField] private float desfaseEntreLetras = 0.3f;

    [Header("Salida")]
    [Tooltip("Lo que tarda en desvanecerse al entrar a un submenu. " +
             "Engancha Desvanecer() al OnClick del boton JUGAR.")]
    [SerializeField] private float duracionSalida = 0.3f;

    private TMP_Text texto;

    // Copia intacta de la malla recien generada. Todo se calcula SOBRE esto,
    // nunca sobre el resultado del frame anterior.
    private Vector3[][] verticesOriginales;

    // Cuantas letras hay de verdad (los espacios no generan geometria).
    private int totalCaracteres;

    private bool entradaTerminada;
    private Coroutine rutinaActual;

    private void Awake()
    {
        texto = GetComponent<TMP_Text>();

        // Un titulo es decoracion: nunca debe interceptar clicks.
        //
        // El RectTransform de un texto es bastante mas grande que sus letras y
        // aqui se solapa con GrupoBotones. Como el Titulo es hermano POSTERIOR
        // en la jerarquia, se dibuja encima y recibe el raycast primero: si
        // sigue siendo raycast target, se traga las pulsaciones de JUGAR y el
        // boton parece roto aunque su OnClick este bien puesto.
        //
        // Se apaga aqui y no solo en el Inspector para que el bug no pueda
        // volver si alguien mueve o reusa el titulo mas adelante.
        texto.raycastTarget = false;
    }

    /// <summary>
    /// Deja el titulo invisible y listo para animar. Llamalo ANTES de
    /// Aparecer(), en el mismo frame en que enciendes el panel.
    /// </summary>
    public void Preparar()
    {
        if (texto == null) texto = GetComponent<TMP_Text>();

        // Corta una flotacion anterior. Si no, al volver a preparar quedarian
        // dos corrutinas peleandose por los mismos vertices.
        if (rutinaActual != null) StopCoroutine(rutinaActual);

        CapturarMalla();
        entradaTerminada = false;

        // Todas las letras a alfa 0. Sin esto se ve un fogonazo del titulo
        // completo durante un frame antes de que arranque la cascada.
        for (int i = 0; i < totalCaracteres; i++)
            PonerAlfaDeCaracter(i, 0f);

        texto.UpdateVertexData(TMP_VertexDataUpdateFlags.All);
    }

    /// <summary>Lanza la cascada. Al acabar encadena la flotacion.</summary>
    public void Aparecer(System.Action alTerminar = null)
    {
        if (rutinaActual != null) StopCoroutine(rutinaActual);
        rutinaActual = StartCoroutine(RutinaEntrada(alTerminar));
    }

    /// <summary>
    /// Muestra el titulo entero de golpe, ya asentado. Util para el modo de
    /// pruebas o si el jugador vuelve al menu y no quieres repetir la intro.
    /// </summary>
    public void MostrarYa()
    {
        if (rutinaActual != null) StopCoroutine(rutinaActual);

        CapturarMalla();
        RestaurarMalla();
        entradaTerminada = true;

        if (flotarAlTerminar) rutinaActual = StartCoroutine(RutinaFlotacion());
    }

    /// <summary>
    /// Funde el titulo hasta invisible. Pensado para engancharlo directamente
    /// al OnClick del boton JUGAR: al abrirse el panel de dificultad, el
    /// titulo se quita de en medio en vez de quedar debajo de los botones.
    ///
    /// Sin parametros a proposito: el OnClick del Inspector solo lista
    /// metodos publicos, void y con cero o un argumento simple.
    /// </summary>
    public void Desvanecer()
    {
        if (rutinaActual != null) StopCoroutine(rutinaActual);

        // Por si alguien lo llama antes de que el titulo haya llegado a
        // animarse: sin malla capturada no sabriamos cuantas letras hay.
        if (totalCaracteres == 0) CapturarMalla();

        // Corta la flotacion. Si siguiera viva seguiria escribiendo vertices
        // mientras se funde, y el titulo temblaria al desaparecer.
        entradaTerminada = false;

        rutinaActual = StartCoroutine(RutinaSalida());
    }

    /// <summary>
    /// Lo trae de vuelta a opacidad total. Engancha esto al boton de cancelar
    /// del panel de dificultad, si algun dia le pones uno.
    /// </summary>
    public void Reaparecer()
    {
        MostrarYa();
    }

    // ------------------------------------------------------------------
    // Corrutinas
    // ------------------------------------------------------------------

    private IEnumerator RutinaEntrada(System.Action alTerminar)
    {
        // Un frame de margen: si el objeto se acaba de activar, TMP todavia
        // no ha hecho su primer layout y la malla vendria vacia.
        yield return null;
        CapturarMalla();

        float t = 0f;
        float duracionTotal = duracionPorLetra + separacionEntreLetras * Mathf.Max(0, totalCaracteres - 1);

        while (t < duracionTotal)
        {
            t += Time.deltaTime;

            TMP_TextInfo info = texto.textInfo;

            for (int i = 0; i < totalCaracteres; i++)
            {
                // Progreso individual de esta letra dentro de su ventana.
                float inicio = i * separacionEntreLetras;
                float p = Mathf.Clamp01((t - inicio) / duracionPorLetra);

                TMP_CharacterInfo caracter = info.characterInfo[i];
                if (!caracter.isVisible) continue;

                int material = caracter.materialReferenceIndex;
                int vertice = caracter.vertexIndex;

                // Ease-out cubico: entra rapido y frena al llegar.
                float suave = 1f - Mathf.Pow(1f - p, 3f);

                float desplazamiento = Mathf.Lerp(alturaDeCaida, 0f, suave);

                // El rebote es medio seno aplicado solo en el ultimo tramo:
                // la letra pasa de largo hacia abajo y vuelve a subir.
                if (rebote > 0f && p > 0.6f)
                {
                    float pr = (p - 0.6f) / 0.4f;
                    desplazamiento -= Mathf.Sin(pr * Mathf.PI) * rebote;
                }

                Vector3 offset = new Vector3(0f, desplazamiento, 0f);
                Vector3[] origen = verticesOriginales[material];
                Vector3[] destino = info.meshInfo[material].vertices;

                for (int v = 0; v < 4; v++)
                    destino[vertice + v] = origen[vertice + v] + offset;

                PonerAlfaDeCaracter(i, p);
            }

            SubirMalla();
            yield return null;
        }

        RestaurarMalla();
        entradaTerminada = true;
        alTerminar?.Invoke();

        if (flotarAlTerminar) rutinaActual = StartCoroutine(RutinaFlotacion());
    }

    private IEnumerator RutinaSalida()
    {
        // Se funde el titulo entero de golpe, no letra a letra. Una salida
        // en cascada haria esperar al jugador que ya decidio que quiere jugar:
        // las entradas pueden lucirse, las salidas tienen que quitarse de en
        // medio rapido.
        float t = 0f;

        while (t < duracionSalida)
        {
            t += Time.deltaTime;
            float alfa = 1f - Mathf.Clamp01(t / duracionSalida);

            for (int i = 0; i < totalCaracteres; i++)
                PonerAlfaDeCaracter(i, alfa);

            SubirMalla();
            yield return null;
        }

        for (int i = 0; i < totalCaracteres; i++)
            PonerAlfaDeCaracter(i, 0f);

        SubirMalla();
    }

    private IEnumerator RutinaFlotacion()
    {
        while (entradaTerminada)
        {
            TMP_TextInfo info = texto.textInfo;

            for (int i = 0; i < totalCaracteres; i++)
            {
                TMP_CharacterInfo caracter = info.characterInfo[i];
                if (!caracter.isVisible) continue;

                int material = caracter.materialReferenceIndex;
                int vertice = caracter.vertexIndex;

                // Cada letra va desfasada respecto a la anterior: eso es lo
                // que convierte un sube-baja plano en una onda.
                float onda = Mathf.Sin(Time.time * velocidadFlotacion + i * desfaseEntreLetras);
                Vector3 offset = new Vector3(0f, onda * amplitudFlotacion, 0f);

                Vector3[] origen = verticesOriginales[material];
                Vector3[] destino = info.meshInfo[material].vertices;

                for (int v = 0; v < 4; v++)
                    destino[vertice + v] = origen[vertice + v] + offset;
            }

            SubirMalla();
            yield return null;
        }
    }

    // ------------------------------------------------------------------
    // Utilidades de malla
    // ------------------------------------------------------------------

    /// <summary>
    /// Regenera la malla y guarda una copia de los vertices tal cual los
    /// coloco TMP. Esa copia es la "posicion de reposo" de cada letra.
    /// </summary>
    private void CapturarMalla()
    {
        texto.ForceMeshUpdate();

        TMP_TextInfo info = texto.textInfo;
        totalCaracteres = info.characterCount;

        verticesOriginales = new Vector3[info.meshInfo.Length][];
        for (int m = 0; m < info.meshInfo.Length; m++)
            verticesOriginales[m] = (Vector3[])info.meshInfo[m].vertices.Clone();
    }

    /// <summary>Devuelve cada letra a su sitio y a opacidad total.</summary>
    private void RestaurarMalla()
    {
        TMP_TextInfo info = texto.textInfo;

        for (int m = 0; m < info.meshInfo.Length; m++)
            verticesOriginales[m].CopyTo(info.meshInfo[m].vertices, 0);

        for (int i = 0; i < totalCaracteres; i++)
            PonerAlfaDeCaracter(i, 1f);

        SubirMalla();
    }

    /// <summary>
    /// El color vive en meshInfo.colors32, no en los vertices, y son 4
    /// entradas por caracter igual que la posicion.
    /// </summary>
    private void PonerAlfaDeCaracter(int indice, float alfa)
    {
        TMP_TextInfo info = texto.textInfo;
        TMP_CharacterInfo caracter = info.characterInfo[indice];
        if (!caracter.isVisible) return;

        Color32[] colores = info.meshInfo[caracter.materialReferenceIndex].colors32;
        byte a = (byte)(Mathf.Clamp01(alfa) * 255f);

        for (int v = 0; v < 4; v++)
        {
            Color32 c = colores[caracter.vertexIndex + v];
            c.a = a;
            colores[caracter.vertexIndex + v] = c;
        }
    }

    /// <summary>
    /// Sube posiciones y colores modificados a la malla real. Un solo
    /// UpdateVertexData con flag All se encarga de todas las submallas.
    /// </summary>
    private void SubirMalla()
    {
        texto.UpdateVertexData(TMP_VertexDataUpdateFlags.All);
    }
}
