using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>
/// Enum que controla qué etapas del pipeline se visualizan en el tilemap.
/// </summary>
public enum MapRenderStage
{
    /// <summary>Solo salas y pasillos del BSP (sin túneles de RandomWalk).</summary>
    BSPOnly,
    /// <summary>BSP + túneles de RandomWalk, sin diferenciación por ruido.</summary>
    WithRandomWalk,
    /// <summary>BSP + RandomWalk + tiles diferenciados según el Perlin Noise.</summary>
    Full
}

/// <summary>
/// Contexto temático del mapa para la asignación de tiles.
/// </summary>
public enum MapContext
{
    Caverna,
    EstacionEspacial
}

/// <summary>
/// Contenedor serializable para los sets de tiles de un contexto particular.
/// Cada contexto soporta:
///   - Paredes
///   - Celdas vacías (ej: roca lunar)
///   - Set A (ruido bajo / normal de Perlin)
///   - Set B (ruido alto / alternativo de Perlin)
/// </summary>
[System.Serializable]
public class MapContextTiles
{
    [Tooltip("Nombre descriptivo del contexto.")]
    public string contextName = "Contexto";

    [Header("Paredes")]
    [Tooltip("Tile para las paredes del mapa.")]
    public TileBase wallTile;

    [Header("Celdas Vacías (ej: Roca Lunar)")]
    [Tooltip("Tile para celdas Empty. Solo se usa si renderEmptyAsRock está activo.")]
    public TileBase emptyOrRockTile;

    [Tooltip("Si es true, las celdas Empty se pintan con emptyOrRockTile.")]
    public bool renderEmptyAsRock = false;

    [Header("Set A — Ruido bajo (< threshold)")]
    [Tooltip("Tile de piso para salas y pasillos del BSP cuando el ruido es bajo.")]
    public TileBase floorTileA;
    [Tooltip("Tile de piso para túneles de Random Walk cuando el ruido es bajo.")]
    public TileBase walkFloorTileA;

    [Header("Set B — Ruido alto (>= threshold)")]
    [Tooltip("Tile de piso para salas y pasillos del BSP cuando el ruido es alto.")]
    public TileBase floorTileB;
    [Tooltip("Tile de piso para túneles de Random Walk cuando el ruido es alto.")]
    public TileBase walkFloorTileB;
}

/// <summary>
/// Visualizador centralizado del mapa generado por el pipeline PCG.
///
/// Recibe los datos de los tres generadores (BSP, RandomWalk, PerlinNoise)
/// y compone la visualización final en Tilemaps, seleccionando el tile
/// adecuado para cada celda según:
///   - El contexto temático activo (Caverna vs Estación Espacial).
///   - El tipo de celda (Floor, Wall, Empty).
///   - Si fue tallada por RandomWalk o por BSP.
///   - El valor de ruido Perlin en esa posición (Set A vs Set B alternativo).
/// </summary>
public class MapVisualizer : MonoBehaviour
{
    // =================================================================
    // Tilemaps destino
    // =================================================================

    [Header("Tilemaps")]
    [SerializeField] private Tilemap floorTilemap;
    [SerializeField] private Tilemap wallTilemap;

    // =================================================================
    // Contextos de Tiles (4 Sets de Piso en total + Paredes + Celdas Vacías)
    // =================================================================

    [Header("Contexto Activo")]
    [SerializeField] private MapContext activeContext = MapContext.Caverna;

    [Header("Contexto: Caverna (Set A y Set B alternativo)")]
    [SerializeField] private MapContextTiles caveTiles = new MapContextTiles
    {
        contextName = "Caverna Excavada",
        renderEmptyAsRock = false
    };

    [Header("Contexto: Estación Espacial (Set A y Set B alternativo)")]
    [SerializeField] private MapContextTiles spaceTiles = new MapContextTiles
    {
        contextName = "Estación Espacial",
        renderEmptyAsRock = true
    };

    // =================================================================
    // Fuentes de datos
    // =================================================================

    [Header("Fuentes de datos")]
    [SerializeField] private BspMapGenerator bspGenerator;
    [SerializeField] private RandomWalkGenerator randomWalkGenerator;
    [SerializeField] private PerlinMapGenerator perlinGenerator;

    // =================================================================
    // Estado interno
    // =================================================================

    /// <summary>
    /// Snapshot del grid BSP ANTES de que RandomWalk lo modifique.
    /// Permite renderizar "solo BSP" sin depender del estado actual del grid
    /// (que ya fue modificado por RandomWalk).
    /// </summary>
    private CellType[,] _bspGridSnapshot;
    private int _snapshotWidth;
    private int _snapshotHeight;

    // =================================================================
    // Métodos públicos y Getters
    // =================================================================

    public MapContext ActiveContext => activeContext;
    public MapContextTiles CaveTiles => caveTiles;
    public MapContextTiles SpaceTiles => spaceTiles;
    public Tilemap FloorTilemap => floorTilemap;
    public Tilemap WallTilemap => wallTilemap;

    /// <summary>
    /// Cambia el contexto temático actual (Caverna o Estación Espacial).
    /// </summary>
    public void SetContext(MapContext context)
    {
        activeContext = context;
    }

    /// <summary>
    /// Obtiene la configuración de tiles correspondiente al contexto activo.
    /// </summary>
    public MapContextTiles GetActiveContextTiles()
    {
        if (activeContext == MapContext.EstacionEspacial && spaceTiles != null)
            return spaceTiles;

        return caveTiles ?? new MapContextTiles();
    }

    public void SetRenderEmptyAsRock(bool value)
    {
        var active = GetActiveContextTiles();
        if (active != null) active.renderEmptyAsRock = value;
    }

    public bool GetRenderEmptyAsRock() => GetActiveContextTiles()?.renderEmptyAsRock ?? false;

    /// <summary>
    /// Clona el grid actual del BSP para preservar su estado antes de que
    /// RandomWalk lo modifique. Debe llamarse DESPUÉS de BSP.Generate()
    /// y ANTES de RandomWalk.Generate().
    /// </summary>
    public void SnapshotBSPGrid()
    {
        if (bspGenerator == null || bspGenerator.Result == null)
        {
            Debug.LogWarning("[MapVisualizer] No hay resultado BSP para hacer snapshot.");
            return;
        }

        var result = bspGenerator.Result;
        _snapshotWidth = result.Width;
        _snapshotHeight = result.Height;
        _bspGridSnapshot = new CellType[_snapshotWidth, _snapshotHeight];

        System.Array.Copy(result.Grid, _bspGridSnapshot, result.Grid.Length);
    }

    /// <summary>
    /// Renderiza el mapa en los tilemaps según la etapa seleccionada.
    /// </summary>
    /// <param name="stage">Qué etapas del pipeline incluir en la visualización.</param>
    public void Render(MapRenderStage stage)
    {
        Clear();

        switch (stage)
        {
            case MapRenderStage.BSPOnly:
                RenderBSPOnly();
                break;

            case MapRenderStage.WithRandomWalk:
                RenderWithRandomWalk(useNoise: false);
                break;

            case MapRenderStage.Full:
                RenderWithRandomWalk(useNoise: true);
                break;
        }
    }

    /// <summary>
    /// Limpia ambos tilemaps.
    /// </summary>
    public void Clear()
    {
        if (floorTilemap != null) floorTilemap.ClearAllTiles();
        if (wallTilemap != null) wallTilemap.ClearAllTiles();
    }

    // =================================================================
    // Renderizado interno
    // =================================================================

    /// <summary>
    /// Renderiza solo el grid del BSP (usando el snapshot tomado antes de RandomWalk).
    /// No aplica diferenciación por ruido; usa siempre los tiles del Set A del contexto activo.
    /// </summary>
    private void RenderBSPOnly()
    {
        if (_bspGridSnapshot == null)
        {
            Debug.LogWarning("[MapVisualizer] No hay snapshot BSP disponible. " +
                "Genera el BSP primero (el snapshot se toma automáticamente).");
            return;
        }

        var activeTiles = GetActiveContextTiles();

        for (int x = 0; x < _snapshotWidth; x++)
        {
            for (int y = 0; y < _snapshotHeight; y++)
            {
                var cellType = _bspGridSnapshot[x, y];
                var pos = new Vector3Int(x, y, 0);

                if (cellType == CellType.Empty)
                {
                    if (activeTiles.renderEmptyAsRock && activeTiles.emptyOrRockTile != null)
                        SetFloorTile(pos, activeTiles.emptyOrRockTile);
                    continue;
                }

                if (cellType == CellType.Floor)
                {
                    SetFloorTile(pos, activeTiles.floorTileA);
                }
                else if (cellType == CellType.Wall)
                {
                    SetWallTile(pos, activeTiles.wallTile);
                }
            }
        }
    }

    /// <summary>
    /// Renderiza el grid completo (BSP + RandomWalk), opcionalmente con
    /// diferenciación de tiles (Set A vs Set B) según el Perlin Noise.
    /// </summary>
    /// <param name="useNoise">Si true, consulta el NoiseMap para elegir Set A o B.</param>
    private void RenderWithRandomWalk(bool useNoise)
    {
        if (bspGenerator == null || bspGenerator.Result == null)
        {
            Debug.LogWarning("[MapVisualizer] No hay resultado BSP disponible.");
            return;
        }

        var result = bspGenerator.Result;
        var carvedByWalk = randomWalkGenerator != null
            ? randomWalkGenerator.CarvedByWalk
            : null;

        bool hasNoise = useNoise
            && perlinGenerator != null
            && perlinGenerator.NoiseMap != null;

        float threshold = hasNoise ? perlinGenerator.Threshold : 0f;
        var activeTiles = GetActiveContextTiles();

        for (int x = 0; x < result.Width; x++)
        {
            for (int y = 0; y < result.Height; y++)
            {
                var cellType = result.Grid[x, y];
                var pos = new Vector3Int(x, y, 0);

                if (cellType == CellType.Empty)
                {
                    if (activeTiles.renderEmptyAsRock && activeTiles.emptyOrRockTile != null)
                        SetFloorTile(pos, activeTiles.emptyOrRockTile);
                    continue;
                }

                if (cellType == CellType.Wall)
                {
                    SetWallTile(pos, activeTiles.wallTile);
                    continue;
                }

                // cellType == Floor
                bool isWalkCell = carvedByWalk != null
                    && carvedByWalk.Contains(new Vector2Int(x, y));

                if (hasNoise)
                {
                    bool highNoise = perlinGenerator.GetNormalizedValueAt(x, y) >= threshold;
                    TileBase tile = PickFloorTile(activeTiles, isWalkCell, highNoise);
                    SetFloorTile(pos, tile);
                }
                else
                {
                    // Sin noise: usar siempre Set A, pero diferenciar BSP vs RW
                    TileBase tile = isWalkCell
                        ? (activeTiles.walkFloorTileA != null ? activeTiles.walkFloorTileA : activeTiles.floorTileA)
                        : activeTiles.floorTileA;
                    SetFloorTile(pos, tile);
                }
            }
        }
    }

    /// <summary>
    /// Selecciona el tile de piso apropiado del contexto activo según si es túnel de RW y si
    /// el ruido es alto (Set B) o bajo (Set A). Aplica fallback al Set A si el Set B no
    /// tiene tiles asignados.
    /// </summary>
    private TileBase PickFloorTile(MapContextTiles activeTiles, bool isWalkCell, bool highNoise)
    {
        if (activeTiles == null) return null;

        if (isWalkCell)
        {
            if (highNoise)
                return activeTiles.walkFloorTileB != null ? activeTiles.walkFloorTileB : (activeTiles.walkFloorTileA ?? activeTiles.floorTileA);
            else
                return activeTiles.walkFloorTileA != null ? activeTiles.walkFloorTileA : activeTiles.floorTileA;
        }
        else
        {
            if (highNoise)
                return activeTiles.floorTileB != null ? activeTiles.floorTileB : activeTiles.floorTileA;
            else
                return activeTiles.floorTileA;
        }
    }

    // =================================================================
    // Helpers de tilemap
    // =================================================================

    private void SetFloorTile(Vector3Int pos, TileBase tile)
    {
        if (floorTilemap != null && tile != null)
            floorTilemap.SetTile(pos, tile);
    }

    private void SetWallTile(Vector3Int pos, TileBase tile)
    {
        if (wallTilemap != null && tile != null)
            wallTilemap.SetTile(pos, tile);
    }
}
