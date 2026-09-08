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
/// Visualizador centralizado del mapa generado por el pipeline PCG.
///
/// Recibe los datos de los tres generadores (BSP, RandomWalk, PerlinNoise)
/// y compone la visualización final en Tilemaps, seleccionando el tile
/// adecuado para cada celda según:
///   - El tipo de celda (Floor, Wall, Empty).
///   - Si fue tallada por RandomWalk o por BSP.
///   - El valor de ruido Perlin en esa posición (Set A vs Set B).
///
/// Sigue el mismo patrón de separación datos/visualización que
/// MissionGrammarGenerator / MissionVisualizer.
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
    // Tiles — Pared (sin diferenciación por ruido)
    // =================================================================

    [Header("Pared (sin diferenciación por ruido)")]
    [SerializeField] private TileBase wallTile;

    [Header("Roca lunar (celdas vacias)")]
    [Tooltip("Tile para celdas Empty (roca lunar). Solo se usa si renderEmptyAsRock esta activo. Si no se asigna, las celdas vacias quedan sin tile.")]
    [SerializeField] private TileBase lunarRockTile;

    [Tooltip("Si es true, las celdas Empty se pintan con lunarRockTile. False = comportamiento original (vacias).")]
    [SerializeField] private bool renderEmptyAsRock = false;

    // =================================================================
    // Tiles — Set A (ruido bajo, < threshold)
    // =================================================================

    [Header("Set A — Ruido bajo (< threshold)")]
    [Tooltip("Tile de piso para salas y pasillos del BSP cuando el ruido es bajo.")]
    [SerializeField] private TileBase floorTileA;
    [Tooltip("Tile de piso para túneles de Random Walk cuando el ruido es bajo.")]
    [SerializeField] private TileBase walkFloorTileA;

    // =================================================================
    // Tiles — Set B (ruido alto, >= threshold)
    // =================================================================

    [Header("Set B — Ruido alto (>= threshold)")]
    [Tooltip("Tile de piso para salas y pasillos del BSP cuando el ruido es alto.")]
    [SerializeField] private TileBase floorTileB;
    [Tooltip("Tile de piso para túneles de Random Walk cuando el ruido es alto.")]
    [SerializeField] private TileBase walkFloorTileB;

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
    // Métodos públicos
    // =================================================================

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
    // Getters para que TopdownSceneSetup pueda acceder a las propiedades
    // =================================================================

    public Tilemap FloorTilemap => floorTilemap;
    public Tilemap WallTilemap => wallTilemap;
    public void SetRenderEmptyAsRock(bool value) { renderEmptyAsRock = value; }
    public bool GetRenderEmptyAsRock() => renderEmptyAsRock;

    // =================================================================
    // Renderizado interno
    // =================================================================

    /// <summary>
    /// Renderiza solo el grid del BSP (usando el snapshot tomado antes de RandomWalk).
    /// No aplica diferenciación por ruido; usa siempre los tiles del Set A.
    /// </summary>
    private void RenderBSPOnly()
    {
        if (_bspGridSnapshot == null)
        {
            Debug.LogWarning("[MapVisualizer] No hay snapshot BSP disponible. " +
                "Genera el BSP primero (el snapshot se toma automáticamente).");
            return;
        }

        for (int x = 0; x < _snapshotWidth; x++)
        {
            for (int y = 0; y < _snapshotHeight; y++)
            {
                var cellType = _bspGridSnapshot[x, y];
                var pos = new Vector3Int(x, y, 0);
                if (cellType == CellType.Empty)
                {
                    if (renderEmptyAsRock && lunarRockTile != null)
                        SetFloorTile(pos, lunarRockTile);
                    continue;
                }


                if (cellType == CellType.Floor)
                {
                    SetFloorTile(pos, floorTileA);
                }
                else if (cellType == CellType.Wall)
                {
                    SetWallTile(pos, wallTile);
                }
            }
        }
    }

    /// <summary>
    /// Renderiza el grid completo (BSP + RandomWalk), opcionalmente con
    /// diferenciación de tiles según el Perlin Noise.
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

        for (int x = 0; x < result.Width; x++)
        {
            for (int y = 0; y < result.Height; y++)
            {
                var cellType = result.Grid[x, y];
                var pos = new Vector3Int(x, y, 0);
                if (cellType == CellType.Empty)
                {
                    if (renderEmptyAsRock && lunarRockTile != null)
                        SetFloorTile(pos, lunarRockTile);
                    continue;
                }


                if (cellType == CellType.Wall)
                {
                    SetWallTile(pos, wallTile);
                    continue;
                }

                // cellType == Floor
                bool isWalkCell = carvedByWalk != null
                    && carvedByWalk.Contains(new Vector2Int(x, y));

                if (hasNoise)
                {
                    bool highNoise = perlinGenerator.GetNormalizedValueAt(x, y) >= threshold;
                    TileBase tile = PickFloorTile(isWalkCell, highNoise);
                    SetFloorTile(pos, tile);
                }
                else
                {
                    // Sin noise: usar siempre Set A, pero diferenciar BSP vs RW
                    TileBase tile = isWalkCell
                        ? (walkFloorTileA != null ? walkFloorTileA : floorTileA)
                        : floorTileA;
                    SetFloorTile(pos, tile);
                }
            }
        }
    }

    /// <summary>
    /// Selecciona el tile de piso apropiado según si es túnel de RW y si
    /// el ruido es alto o bajo. Aplica fallback al Set A si el Set B no
    /// tiene tiles asignados.
    /// </summary>
    private TileBase PickFloorTile(bool isWalkCell, bool highNoise)
    {
        if (isWalkCell)
        {
            if (highNoise)
                return walkFloorTileB != null ? walkFloorTileB : walkFloorTileA ?? floorTileA;
            else
                return walkFloorTileA != null ? walkFloorTileA : floorTileA;
        }
        else
        {
            if (highNoise)
                return floorTileB != null ? floorTileB : floorTileA;
            else
                return floorTileA;
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
