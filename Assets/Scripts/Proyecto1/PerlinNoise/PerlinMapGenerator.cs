using UnityEngine;

/// <summary>
/// Primera etapa conceptual del pipeline (aunque se ejecute como script
/// independiente): genera un mapa de ruido Perlin del mismo tamaño que el
/// grid del BSP (width x height), usado como capa de datos ambiental.
///
/// Los valores NO representan altura física. Indican características del
/// terreno: en la mina, zonas con valores altos pueden tener más presencia
/// de minerales; en la colonia, más presencia de cristales o condiciones
/// ambientales particulares. El BSP y el L-System consultan este mapa para
/// decidir dónde ubicar contenido.
///
/// Reutiliza el algoritmo de Perlin/Gradient Noise 2D implementado en el
/// laboratorio (PerlinNoiseGenerator + HeightmapGenerator), sin modificarlos.
/// Esos scripts generan mapas cuadrados (resolution x resolution) pensados
/// para un Terrain; aquí se llama directamente a GetNoiseValue() celda por
/// celda para poder soportar un grid rectangular (width x height) igual al
/// del BSP.
/// </summary>
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

    /// <summary>
    /// Mapa de ruido normalizado a [0,1], mismo sistema de coordenadas
    /// [x, y] que BspMapResult.Grid (a diferencia del heightmap original
    /// del laboratorio, que usa [y, x] por convención de Terrain).
    /// </summary>
    public float[,] NoiseMap { get; private set; }

    public int Width => width;
    public int Height => height;

    /// <summary>
    /// Permite que el PipelineManager sincronice el tamaño con el del BSP.
    /// </summary>
    public void SetDimensions(int newWidth, int newHeight)
    {
        width = newWidth;
        height = newHeight;
    }

    /// <summary>
    /// Genera el mapa de ruido. Independiente del BSP: solo depende del
    /// tamaño configurado, por lo que puede ejecutarse antes que el BSP
    /// (como indica el flujo del plan: SEED -> PERLIN -> BSP -> ...).
    /// </summary>
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

        return NoiseMap;
    }

    /// <summary>
    /// Valor de ruido en una celda del grid. Devuelve 0 si está fuera de rango
    /// o si el mapa todavía no se ha generado.
    /// </summary>
    public float GetValueAt(int x, int y)
    {
        if (NoiseMap == null) return 0f;
        if (x < 0 || y < 0 || x >= width || y >= height) return 0f;
        return NoiseMap[x, y];
    }

    /// <summary>
    /// Promedio del ruido dentro de los límites de una sala del BSP. Pensado
    /// para que el L-System pueda elegir salas con mayor concentración de
    /// "mineral" o "cristal" como puntos de origen de las vetas.
    /// </summary>
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

    // ---------------------------------------------------------------------
    // Debug visual en el editor: pinta el mapa de ruido como escala de grises
    // ---------------------------------------------------------------------
    //
    // Nota: Gizmos.DrawCube en 3D se ve afectado por el ángulo de cámara y
    // el sombreado de la Scene view, lo que puede hacer que valores de gris
    // intermedios (típicos de Perlin, que rara vez toca 0 o 1) se vean casi
    // uniformes. Por eso aquí se remapea el contraste antes de pintar, y se
    // recomienda mirar el mapa desde arriba (vista Top) para evitar
    // distorsión por perspectiva.
    private void OnDrawGizmos()
    {
        if (NoiseMap == null || !showGizmo) return;

        // Encuentra el rango real de valores para estirar el contraste.
        // Gradient Noise casi nunca toca 0 o 1, así que sin este paso el
        // mapa se ve plano aunque los datos varíen correctamente.
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
        float range = Mathf.Max(0.0001f, max - min);

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                float v = NoiseMap[x, y];
                float contrasted = (v - min) / range; // ahora ocupa todo [0,1]

                // Z = -0.5: se dibuja "detrás" del plano donde el BSP y el
                // Random Walk pintan sus propios gizmos (Z = 0), para que
                // ambas capas puedan inspeccionarse sin que una tape a la otra.
                Gizmos.color = new Color(contrasted, contrasted, contrasted, 1f);
                Gizmos.DrawCube(new Vector3(x + 0.5f, y + 0.5f, -0.5f), Vector3.one);
            }
        }
    }
}