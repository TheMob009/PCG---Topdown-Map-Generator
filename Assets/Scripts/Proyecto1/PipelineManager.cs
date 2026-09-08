using UnityEngine;

/// <summary>
/// Orquesta el pipeline completo de generaci�n en el orden definido por el
/// plan del proyecto:
///
///   SEED -> PERLIN NOISE -> BSP -> RANDOM WALK -> (L-System) -> (Gram�tica)
///
/// Este componente no genera nada por s� mismo: coordina a los tres
/// generadores ya existentes, sincroniza sus dimensiones y respeta el orden
/// de dependencias (Random Walk necesita el resultado del BSP; el BSP puede
/// opcionalmente consultar a Perlin, pero no depende de �l para su geometr�a).
///
/// Asigna aqu� las referencias a PerlinMapGenerator, BspMapGenerator y
/// RandomWalkGenerator ya existentes en la escena.
/// </summary>
public class PipelineManager : MonoBehaviour
{
    [Header("Dimensiones del mapa (fuente �nica de verdad)")]
    [Tooltip("Se sincroniza autom�ticamente hacia Perlin y BSP antes de generar, para que nunca queden desincronizados.")]
    [SerializeField] private int mapWidth = 60;
    [SerializeField] private int mapHeight = 40;

    [Header("Referencias a los generadores")]
    [SerializeField] private PerlinMapGenerator perlinGenerator;
    [SerializeField] private BspMapGenerator bspGenerator;
    [SerializeField] private RandomWalkGenerator randomWalkGenerator;
    [SerializeField] private MissionGrammarGenerator missionGrammarGenerator;
    [SerializeField] private MissionVisualizer missionVisualizer;

    [Header("Ejecucion")]
    [Tooltip("Si esta activo, genera el mapa completo con sus misiones al iniciar el juego en Play Mode.")]
    [SerializeField] private bool generateOnStart = true;

    public int MapWidth => mapWidth;
    public int MapHeight => mapHeight;

    /// <summary>
    /// Permite configurar las dimensiones desde la UI.
    /// </summary>
    public void SetDimensions(int width, int height)
    {
        mapWidth = width;
        mapHeight = height;
    }

    /// <summary>
    /// Propaga una seed maestra a todos los generadores, derivando sub-seeds
    /// deterministas para que cada algoritmo genere resultados distintos pero
    /// reproducibles con la misma seed.
    /// </summary>
    public void SetGlobalSeed(int masterSeed)
    {
        var masterRng = new System.Random(masterSeed);
        if (perlinGenerator != null) perlinGenerator.SetSeed(masterRng.Next());
        if (bspGenerator != null) bspGenerator.SetSeed(masterRng.Next());
        if (randomWalkGenerator != null) randomWalkGenerator.SetSeed(masterRng.Next());
        if (missionGrammarGenerator != null) missionGrammarGenerator.SetSeed(masterRng.Next());
    }

    // Getters para que la UI pueda acceder a los generadores
    public PerlinMapGenerator PerlinGenerator => perlinGenerator;
    public BspMapGenerator BspGenerator => bspGenerator;
    public RandomWalkGenerator RWGenerator => randomWalkGenerator;
    public MissionGrammarGenerator MGGenerator => missionGrammarGenerator;

    private void Start()
    {
        if (generateOnStart)
        {
            GenerateAll();
        }
    }

    /// <summary>
    /// Corre el pipeline completo en orden: Perlin -> BSP -> Random Walk -> Mision -> Visualizacion.
    /// Cada etapa deja su resultado disponible antes de que arranque la
    /// siguiente, tal como exige el flujo del plan.
    /// </summary>
    public void GenerateAll()
    {
        SyncDimensions();

        GeneratePerlinOnly();
        GenerateBspOnly();
        GenerateRandomWalkOnly();
        GenerateMissionGrammarOnly();
        RenderMissionVisualizer();
    }

    /// <summary>
    /// Sincroniza las dimensiones configuradas aqui hacia Perlin y BSP.
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

    public void GenerateMissionGrammarOnly()
    {
        if (missionGrammarGenerator == null)
        {
            Debug.LogError("[PipelineManager] No hay un MissionGrammarGenerator asignado.");
            return;
        }

        if (bspGenerator == null || bspGenerator.Result == null)
        {
            Debug.LogError("[PipelineManager] El BSP debe generarse antes que la Gramatica de misiones.");
            return;
        }

        missionGrammarGenerator.Generate();
        RenderMissionVisualizer();
    }

    public void RenderMissionVisualizer()
    {
        if (missionVisualizer != null)
        {
            missionVisualizer.RenderMissionMarkers();
        }
    }

    /// <summary>
    /// Limpia los tilemaps del BSP y los marcadores de mision.
    /// </summary>
    public void ClearAll()
    {
        if (bspGenerator != null)
        {
            bspGenerator.ClearMap();
        }
        if (missionVisualizer != null)
        {
            missionVisualizer.ClearMarkers();
        }
    }
}
