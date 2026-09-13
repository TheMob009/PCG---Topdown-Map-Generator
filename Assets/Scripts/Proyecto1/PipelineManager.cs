using UnityEngine;

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
    [SerializeField] private MapVisualizer mapVisualizer;

    [Header("Ejecucion")]
    [Tooltip("Si esta activo, genera el mapa completo con sus misiones al iniciar el juego en Play Mode.")]
    [SerializeField] private bool generateOnStart = true;

    public int MapWidth => mapWidth;
    public int MapHeight => mapHeight;

    public void SetDimensions(int width, int height)
    {
        mapWidth = width;
        mapHeight = height;
    }

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
    public MapVisualizer MapVisualizer => mapVisualizer;

    private void Start()
    {
        if (generateOnStart)
        {
            GenerateAllDefault();
        }
    }

    [ContextMenu("Generar Todo (Por Defecto: Caverna)")]
    public void GenerateAllDefault()
    {
        ApplyExcavatedCaveParameters();

        int randomMasterSeed = System.Environment.TickCount ^ System.Guid.NewGuid().GetHashCode();
        SetGlobalSeed(randomMasterSeed);

        GenerateAll();
    }

    public void GenerateAll()
    {
        SyncDimensions();

        GeneratePerlinOnly();
        GenerateBspOnly();
        GenerateRandomWalkOnly();
        GenerateMissionGrammarOnly();
        RenderMissionVisualizer();
        RenderMap(MapRenderStage.Full);
    }

    public void ApplyExcavatedCaveParameters()
    {
        SetDimensions(60, 40);
        SyncDimensions();

        if (perlinGenerator != null)
        {
            perlinGenerator.SetFrequency(2.0f);
            perlinGenerator.SetInterpolationMode(HeightmapGenerator.InterpolationMode.Bicubic);
            perlinGenerator.SetThreshold(0.5f);
        }

        if (bspGenerator != null)
        {
            bspGenerator.SetMinPartitionSize(14);
            bspGenerator.SetMaxIterations(3);
            bspGenerator.SetRoomPadding(3);
            bspGenerator.SetMinRoomSize(8);
            bspGenerator.SetCorridorWidth(1);
            bspGenerator.SetUseStraightCorridors(false);
        }

        if (randomWalkGenerator != null)
        {
            randomWalkGenerator.SetAgentCount(6);
            randomWalkGenerator.SetStepsPerAgent(100);
            randomWalkGenerator.SetWalkWidth(2);
            randomWalkGenerator.SetDirectionPersistence(0.35f);
            randomWalkGenerator.SetSpawnFromRoomEdge(false);
        }

        if (missionGrammarGenerator != null)
        {
            missionGrammarGenerator.SetMissionContext(MissionContext.Mina);
            missionGrammarGenerator.SetExpansionSteps(2);
        }

        if (mapVisualizer != null)
        {
            mapVisualizer.SetContext(MapContext.Caverna);
            mapVisualizer.SetRenderEmptyAsRock(false);
        }
    }

    [ContextMenu("Generar Todo: Caverna Excavada")]
    public void GenerateAllExcavatedCave()
    {
        ApplyExcavatedCaveParameters();

        int randomMasterSeed = System.Environment.TickCount ^ System.Guid.NewGuid().GetHashCode();
        SetGlobalSeed(randomMasterSeed);

        GenerateAll();
    }
    public void ApplyLunarStationParameters()
    {
        SetDimensions(80, 60);
        SyncDimensions();

        if (perlinGenerator != null)
        {
            perlinGenerator.SetFrequency(6.0f);
            perlinGenerator.SetInterpolationMode(HeightmapGenerator.InterpolationMode.Bicubic);
            perlinGenerator.SetThreshold(0.55f);
        }

        if (bspGenerator != null)
        {
            bspGenerator.SetMinPartitionSize(25);
            bspGenerator.SetMaxIterations(4);
            bspGenerator.SetRoomPadding(6);
            bspGenerator.SetMinRoomSize(15);
            bspGenerator.SetCorridorWidth(3);
            bspGenerator.SetUseStraightCorridors(true);
        }

        if (randomWalkGenerator != null)
        {
            randomWalkGenerator.SetAgentCount(3);
            randomWalkGenerator.SetStepsPerAgent(80);
            randomWalkGenerator.SetWalkWidth(1);
            randomWalkGenerator.SetDirectionPersistence(0.85f);
            randomWalkGenerator.SetSpawnFromRoomEdge(true);
        }

        if (missionGrammarGenerator != null)
        {
            missionGrammarGenerator.SetMissionContext(MissionContext.Colonia);
            missionGrammarGenerator.SetExpansionSteps(3);
        }

        if (mapVisualizer != null)
        {
            mapVisualizer.SetContext(MapContext.EstacionEspacial);
            mapVisualizer.SetRenderEmptyAsRock(true);
        }
    }

    [ContextMenu("Generar Todo: Estacion Espacial")]
    public void GenerateAllLunarStation()
    {
        ApplyLunarStationParameters();

        int randomMasterSeed = System.Environment.TickCount ^ System.Guid.NewGuid().GetHashCode();
        SetGlobalSeed(randomMasterSeed);

        GenerateAll();
    }

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

        // Snapshot del grid BSP antes de que RandomWalk lo modifique,
        // para que MapVisualizer pueda renderizar "solo BSP" más adelante.
        if (mapVisualizer != null) mapVisualizer.SnapshotBSPGrid();
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

    public void RenderMap(MapRenderStage stage)
    {
        if (mapVisualizer != null)
        {
            mapVisualizer.Render(stage);
        }
    }

    public void ClearAll()
    {
        if (bspGenerator != null)
        {
            bspGenerator.ClearMap();
        }
        if (mapVisualizer != null)
        {
            mapVisualizer.Clear();
        }
        if (missionVisualizer != null)
        {
            missionVisualizer.ClearMarkers();
        }
    }
}
