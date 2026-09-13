using UnityEngine;

public class PerlinMapGenerator : MonoBehaviour
{
    [Header("Dimensiones (deben coincidir con el grid del BSP)")]
    [SerializeField] private int width = 60;
    [SerializeField] private int height = 40;

    [Header("Parámetros de ruido (laboratorio de terrenos)")]
    [Tooltip("Cantidad de variaciones del ruido distribuidas sobre el mapa. Valores mayores producen características más pequeñas y frecuentes.")]
    [Range(0.1f, 20f)]
    [SerializeField] private float frequency = 4f;

    [Tooltip("Método utilizado para combinar las contribuciones de los gradientes vecinos.")]
    [SerializeField]
    private HeightmapGenerator.InterpolationMode interpolationMode =
        HeightmapGenerator.InterpolationMode.Bicubic;

    [Header("Semilla")]
    [SerializeField] private bool useRandomSeed = true;
    [SerializeField] private int seed = 12345;

    [Header("Debug")]
    [Tooltip("El gizmo de Perlin pinta un mosaico sólido que puede tapar visualmente al BSP y al Random Walk (que ocupan el mismo espacio). Desactiva esto para inspeccionar la estructura del mapa sin la capa de ruido encima.")]
    [SerializeField] private bool showGizmo = true;

    [Header("Threshold de visualización")]
    [Tooltip("Valor de corte para la visualización con tiles: ruido < threshold → Set A, ruido >= threshold → Set B.")]
    [Range(0f, 1f)]
    [SerializeField] private float threshold = 0.5f;

    public float[,] NoiseMap { get; private set; }

    public float NoiseMin { get; private set; }


    public float NoiseMax { get; private set; }

    public int Width => width;
    public int Height => height;

 
    public void SetDimensions(int newWidth, int newHeight)
    {
        width = newWidth;
        height = newHeight;
    }

    public void SetSeed(int newSeed) { useRandomSeed = false; seed = newSeed; }
    public void SetFrequency(float value) { frequency = value; }
    public void SetInterpolationMode(HeightmapGenerator.InterpolationMode mode) { interpolationMode = mode; }

    // Getters para inicializar la UI con los valores actuales
    public int Seed => seed;
    public float GetFrequency() => frequency;
    public HeightmapGenerator.InterpolationMode GetInterpolationMode() => interpolationMode;

    // Threshold
    public float Threshold => threshold;
    public void SetThreshold(float value) { threshold = Mathf.Clamp01(value); }
    public float GetThreshold() => threshold;

    public float[,] Generate()
    {
        int actualSeed = useRandomSeed ? System.Environment.TickCount : seed;
        seed = actualSeed;

        NoiseMap = new float[width, height];

        float freq = Mathf.Max(0.001f, frequency);

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                // Cada eje se normaliza con su propia dimensión, para que
                // frequency represente la misma escala de detalle en X y en Y
                // sin importar si el mapa es más ancho que alto.
                float normalizedX = width > 1 ? x / (float)(width - 1) : 0f;
                float normalizedY = height > 1 ? y / (float)(height - 1) : 0f;

                float sampleX = normalizedX * freq;
                float sampleY = normalizedY * freq;

                float noise = PerlinNoiseGenerator.GetNoiseValue(
                    sampleX,
                    sampleY,
                    seed,
                    interpolationMode
                );

                // Igual que en el laboratorio: Gradient Noise da valores
                // aproximadamente en [-1,1]; se lleva a [0,1].
                NoiseMap[x, y] = Mathf.Clamp01((noise + 1f) * 0.5f);
            }
        }

        // Cachear el rango real para que tanto el Gizmo como el
        // MapVisualizer normalicen con la misma escala.
        float min = float.MaxValue, max = float.MinValue;
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                float v = NoiseMap[x, y];
                if (v < min) min = v;
                if (v > max) max = v;
            }
        }
        NoiseMin = min;
        NoiseMax = max;

        return NoiseMap;
    }

    public float GetValueAt(int x, int y)
    {
        if (NoiseMap == null) return 0f;
        if (x < 0 || y < 0 || x >= width || y >= height) return 0f;
        return NoiseMap[x, y];
    }

    public float GetNormalizedValueAt(int x, int y)
    {
        if (NoiseMap == null) return 0f;
        if (x < 0 || y < 0 || x >= width || y >= height) return 0f;

        float range = Mathf.Max(0.0001f, NoiseMax - NoiseMin);
        return (NoiseMap[x, y] - NoiseMin) / range;
    }

    public float GetAverageValueInRoom(BspRoom room)
    {
        if (NoiseMap == null) return 0f;

        var bounds = room.Bounds;
        float sum = 0f;
        int count = 0;

        for (int x = bounds.x; x < bounds.x + bounds.width; x++)
        {
            for (int y = bounds.y; y < bounds.y + bounds.height; y++)
            {
                sum += GetValueAt(x, y);
                count++;
            }
        }

        return count > 0 ? sum / count : 0f;
    }

    private void OnDrawGizmos()
    {
        if (NoiseMap == null || !showGizmo) return;

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                // Usa la misma normalización que MapVisualizer para
                // que el Gizmo y los tiles coincidan visualmente.
                float contrasted = GetNormalizedValueAt(x, y);

                // Z = -0.5: se dibuja "detrás" del plano donde el BSP y el
                // Random Walk pintan sus propios gizmos (Z = 0), para que
                // ambas capas puedan inspeccionarse sin que una tape a la otra.
                Gizmos.color = new Color(contrasted, contrasted, contrasted, 1f);
                Gizmos.DrawCube(new Vector3(x + 0.5f, y + 0.5f, -0.5f), Vector3.one);
            }
        }
    }
}