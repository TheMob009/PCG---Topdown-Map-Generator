using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;


public enum MapRenderStage
{
    BSPOnly,
    WithRandomWalk,
    Full
}

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
public class MapVisualizer : MonoBehaviour
{
    [Header("Tilemaps")]
    [SerializeField] private Tilemap floorTilemap;
    [SerializeField] private Tilemap wallTilemap;

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

    [Header("Fuentes de datos")]
    [SerializeField] private BspMapGenerator bspGenerator;
    [SerializeField] private RandomWalkGenerator randomWalkGenerator;
    [SerializeField] private PerlinMapGenerator perlinGenerator;
    private CellType[,] _bspGridSnapshot;
    private int _snapshotWidth;
    private int _snapshotHeight;

    public MapContext ActiveContext => activeContext;
    public MapContextTiles CaveTiles => caveTiles;
    public MapContextTiles SpaceTiles => spaceTiles;
    public Tilemap FloorTilemap => floorTilemap;
    public Tilemap WallTilemap => wallTilemap;

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

    public void Clear()
    {
        if (floorTilemap != null) floorTilemap.ClearAllTiles();
        if (wallTilemap != null) wallTilemap.ClearAllTiles();
    }

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
