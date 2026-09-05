using UnityEngine;

/// <summary>
/// Orquesta el pipeline completo de generación en el orden definido por el
/// plan del proyecto:
///
///   SEED -> PERLIN NOISE -> BSP -> RANDOM WALK -> (L-System) -> (Gramática)
///
/// Este componente no genera nada por sí mismo: coordina a los tres
/// generadores ya existentes, sincroniza sus dimensiones y respeta el orden
/// de dependencias (Random Walk necesita el resultado del BSP; el BSP puede
/// opcionalmente consultar a Perlin, pero no depende de él para su geometría).
///
/// Asigna aquí las referencias a PerlinMapGenerator, BspMapGenerator y
/// RandomWalkGenerator ya existentes en la escena.
/// </summary>
public class PipelineManager : MonoBehaviour
{
    [Header("Dimensiones del mapa (fuente única de verdad)")]
    [Tooltip("Se sincroniza automáticamente hacia Perlin y BSP antes de generar, para que nunca queden desincronizados.")]
    [SerializeField] private int mapWidth = 60;
    [SerializeField] private int mapHeight = 40;

    [Header("Referencias a los generadores")]
    [SerializeField] private PerlinMapGenerator perlinGenerator;
    [SerializeField] private BspMapGenerator bspGenerator;
    [SerializeField] private RandomWalkGenerator randomWalkGenerator;

    public int MapWidth => mapWidth;
    public int MapHeight => mapHeight;

    /// <summary>
    /// Corre el pipeline completo en orden: Perlin -> BSP -> Random Walk.
    /// Cada etapa deja su resultado disponible antes de que arranque la
    /// siguiente, tal como exige el flujo del plan.
    /// </summary>
    public void GenerateAll()
    {
        SyncDimensions();

        GeneratePerlinOnly();
        GenerateBspOnly();
        GenerateRandomWalkOnly();
    }

    /// <summary>
    /// Sincroniza las dimensiones configuradas aquí hacia Perlin y BSP.
    /// Random Walk no necesita esto: usa directamente el grid del BSP.
    /// </summary>
    public void SyncDimensions()
    {
        if (perlinGenerator != null) perlinGenerator.SetDimensions(mapWidth, mapHeight);
        if (bspGenerator != null) bspGenerator.SetDimensions(mapWidth, mapHeight);
    }

    public void GeneratePerlinOnly()
    {
        if (perlinGenerator == null)
        {
            Debug.LogError("[PipelineManager] No hay un PerlinMapGenerator asignado.");
            return;
        }

        perlinGenerator.Generate();
    }

    public void GenerateBspOnly()
    {
        if (bspGenerator == null)
        {
            Debug.LogError("[PipelineManager] No hay un BspMapGenerator asignado.");
            return;
        }

        bspGenerator.Generate();
    }

    public void GenerateRandomWalkOnly()
    {
        if (randomWalkGenerator == null)
        {
            Debug.LogError("[PipelineManager] No hay un RandomWalkGenerator asignado.");
            return;
        }

        if (bspGenerator == null || bspGenerator.Result == null)
        {
            Debug.LogError("[PipelineManager] El BSP debe generarse antes que el Random Walk.");
            return;
        }

        randomWalkGenerator.Generate();
    }
}