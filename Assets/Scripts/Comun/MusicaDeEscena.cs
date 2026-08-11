using UnityEngine;

/// <summary>
/// La musica de fondo de una escena, en un solo componente.
///
/// COMO SE USA (es todo lo que hay que hacer):
///   1. GameObject vacio en la escena, llamado por ejemplo "Musica".
///   2. Add Component -> Musica de Escena.
///   3. Arrastrar el .mp3 al campo "Musica".
///
/// Ya esta. Al entrar a la escena la cancion aparece con fade in, y al salir
/// se funde con la de la escena siguiente (o se apaga sola si la siguiente no
/// tiene musica). Los efectos de sonido no se tocan, siguen igual.
///
/// El objeto NO necesita AudioSource: quien reproduce de verdad es
/// <see cref="MusicaFondo"/>, que se crea solo y sobrevive a los cambios de
/// escena. Por eso no hay corte entre escenas.
/// </summary>
[AddComponentMenu("Audio/Musica de Escena")]
[DisallowMultipleComponent]
public class MusicaDeEscena : MonoBehaviour
{
    [Tooltip("La cancion. Se reproduce en bucle.")]
    [SerializeField] private AudioClip musica;

    [Range(0f, 1f)]
    [Tooltip("Volumen de la musica. Dejalo bajito (0.3 - 0.5) para que no " +
             "tape los efectos de sonido.")]
    [SerializeField] private float volumen = 0.4f;

    [Tooltip("Segundos que tarda en aparecer al entrar a la escena.")]
    [SerializeField] private float fadeIn = 1.5f;

    [Tooltip("Segundos que tarda en irse la musica anterior al entrar esta, " +
             "y en apagarse al salir a una escena sin musica.")]
    [SerializeField] private float fadeOut = 1.5f;

    [Tooltip("Desmarcalo si quieres arrancar la musica desde otro script en " +
             "un momento concreto (por ejemplo, cuando aparece el menu " +
             "despues de la narracion). Entonces hay que llamar a Reproducir().")]
    [SerializeField] private bool arrancarSola = true;

    private void Start()
    {
        if (arrancarSola) Reproducir();
    }

    /// <summary>
    /// Arranca la musica de esta escena. Se puede llamar desde otro script o
    /// desde el OnClick de un boton. Llamarla dos veces no reinicia nada.
    /// </summary>
    public void Reproducir()
    {
        if (musica == null)
        {
            Debug.LogWarning("[MusicaDeEscena] No hay ningun clip asignado en " +
                             "el campo 'Musica'.", this);
            return;
        }

        MusicaFondo.Instancia.Reproducir(musica, volumen, fadeIn, fadeOut);
    }

    /// <summary>Apaga la musica con fundido, sin esperar al cambio de escena.</summary>
    public void Detener()
    {
        MusicaFondo.Instancia.Detener(fadeOut);
    }
}
