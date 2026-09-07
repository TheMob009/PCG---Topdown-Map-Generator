using UnityEngine;

/// <summary>
/// Componente que genera una oscilacion vertical sutil (efecto flotante)
/// para los iconos de mision sobre las salas del mapa en perspectiva topdown 2D.
/// </summary>
public class FloatingMarker : MonoBehaviour
{
    [Tooltip("Amplitud de la oscilacion vertical en unidades de mundo.")]
    [SerializeField] private float amplitude = 0.15f;

    [Tooltip("Velocidad o frecuencia de la oscilacion.")]
    [SerializeField] private float frequency = 2.5f;

    private Vector3 _basePosition;
    private float _randomPhaseOffset;

    private void Awake()
    {
        _basePosition = transform.position;
        // Desfase aleatorio para que multiples iconos no oscilen al unisono
        _randomPhaseOffset = Random.Range(0f, Mathf.PI * 2f);
    }

    /// <summary>
    /// Actualiza la posicion base si el marcador es recolocado en runtime.
    /// </summary>
    public void SetBasePosition(Vector3 newBasePosition)
    {
        _basePosition = newBasePosition;
    }

    private void Update()
    {
        float verticalOffset = Mathf.Sin((Time.time * frequency) + _randomPhaseOffset) * amplitude;
        transform.position = _basePosition + new Vector3(0f, verticalOffset, 0f);
    }
}
