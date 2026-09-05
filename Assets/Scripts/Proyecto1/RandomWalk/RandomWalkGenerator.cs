using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>
/// Segunda etapa del pipeline: parte de un mapa ya generado por BSP y agrega
/// caminos secundarios más irregulares mediante agentes de Random Walk.
///
/// En la mina se interpretan como galerías secundarias o túneles naturales;
/// en la colonia, como corredores o ductos de mantenimiento. A diferencia de
/// los pasillos del BSP (rectos, en L), estos agentes caminan al azar paso a
/// paso y pueden enroscarse libremente, dando un resultado más orgánico.
///
/// Requiere que un BspMapGenerator ya haya corrido antes (Result != null).
/// </summary>
public class RandomWalkGenerator : MonoBehaviour
{
    [Header("Fuente del mapa (debe generarse antes)")]
    [SerializeField] private BspMapGenerator bspGenerator;

    [Header("Agentes")]
    [Tooltip("Cantidad de agentes de Random Walk. Cada uno parte de una sala aleatoria del BSP.")]
    [SerializeField] private int agentCount = 4;
    [Tooltip("Cantidad fija de pasos que camina cada agente.")]
    [SerializeField] private int stepsPerAgent = 40;
    [Tooltip("Ancho del túnel tallado (1 = camino de una celda, más ancho da galerías más amplias).")]
    [SerializeField] private int walkWidth = 1;

    [Header("Semilla")]
    [SerializeField] private bool useRandomSeed = true;
    [SerializeField] private int seed = 0;

    [Header("Tilemap (opcional, para visualizar)")]
    [SerializeField] private Tilemap floorTilemap;
    [SerializeField] private Tilemap wallTilemap;
    [SerializeField] private TileBase floorTile;
    [SerializeField] private TileBase wallTile;

    private System.Random _rng;

    // Las 4 direcciones cardinales; el agente elige una al azar en cada paso.
    private static readonly Vector2Int[] Directions =
    {
        Vector2Int.up,
        Vector2Int.down,
        Vector2Int.left,
        Vector2Int.right,
    };

    /// <summary>
    /// Referencia al mismo resultado que generó el BSP. Se modifica in-place:
    /// Random Walk no crea un grid nuevo, sino que agrega piso sobre el existente.
    /// </summary>
    public BspMapResult Result => bspGenerator != null ? bspGenerator.Result : null;

    /// <summary>
    /// Celdas que este Random Walk agregó como piso nuevo (no las que ya
    /// venían del BSP). Se usa solo para diferenciar visualmente en los
    /// Gizmos; no es necesaria para el resto del pipeline.
    /// </summary>
    private readonly HashSet<Vector2Int> _carvedByWalk = new HashSet<Vector2Int>();

    /// <summary>
    /// Ejecuta el Random Walk sobre el grid ya generado por el BSP.
    /// </summary>
    public void Generate()
    {
        _carvedByWalk.Clear();

        if (bspGenerator == null)
        {
            Debug.LogError("[RandomWalkGenerator] No hay un BspMapGenerator asignado.");
            return;
        }

        if (bspGenerator.Result == null)
        {
            Debug.LogError("[RandomWalkGenerator] El BSP todavía no ha generado un mapa. Genera el BSP primero.");
            return;
        }

        if (bspGenerator.Result.Rooms.Count == 0)
        {
            Debug.LogWarning("[RandomWalkGenerator] El BSP no tiene salas registradas; no hay desde dónde partir.");
            return;
        }

        seed = useRandomSeed ? System.Environment.TickCount : seed;
        _rng = new System.Random(seed);

        var result = bspGenerator.Result;

        for (int i = 0; i < agentCount; i++)
        {
            RunAgent(result);
        }

        // Recalcula las paredes: los nuevos túneles también necesitan su borde.
        MapUtils.PaintWalls(result);

        if (floorTilemap != null || wallTilemap != null)
        {
            MapUtils.DrawTilemap(result, floorTilemap, wallTilemap, floorTile, wallTile);
        }
    }

    /// <summary>
    /// Hace caminar a un único agente: parte del centro de una sala aleatoria
    /// y da stepsPerAgent pasos en direcciones aleatorias, tallando piso.
    /// </summary>
    private void RunAgent(BspMapResult result)
    {
        var startRoom = result.Rooms[_rng.Next(result.Rooms.Count)];
        Vector2Int pos = startRoom.Center;

        for (int step = 0; step < stepsPerAgent; step++)
        {
            CarveAt(result, pos);

            var dir = Directions[_rng.Next(Directions.Length)];
            var next = pos + dir;

            // Si el siguiente paso se sale del mapa, se ignora ese paso y se
            // intenta de nuevo en el siguiente ciclo (el agente no se mueve
            // esa iteración, pero sigue gastando pasos, lo que evita loops
            // infinitos pegado al borde).
            if (result.InBounds(next.x, next.y))
            {
                pos = next;
            }
        }

        // Talla también la última posición alcanzada.
        CarveAt(result, pos);
    }

    /// <summary>
    /// Talla piso en la posición dada, expandiendo según walkWidth (igual
    /// criterio que el ancho de pasillo usado en el BSP).
    /// </summary>
    private void CarveAt(BspMapResult result, Vector2Int center)
    {
        int half = walkWidth / 2;
        for (int dx = -half; dx <= half; dx++)
        {
            for (int dy = -half; dy <= half; dy++)
            {
                int x = center.x + dx;
                int y = center.y + dy;
                if (!result.InBounds(x, y)) continue;

                if (result.Grid[x, y] != CellType.Floor)
                {
                    _carvedByWalk.Add(new Vector2Int(x, y));
                }
                result.Grid[x, y] = CellType.Floor;
            }
        }
    }

    // ---------------------------------------------------------------------
    // Debug visual en el editor
    // ---------------------------------------------------------------------
    private void OnDrawGizmos()
    {
        if (Result == null) return;

        // Pinta únicamente las celdas que este Random Walk agregó (no las
        // que ya venían del BSP), para poder distinguir visualmente su aporte.
        Gizmos.color = new Color(0.2f, 0.7f, 0.9f, 0.6f);

        foreach (var cell in _carvedByWalk)
        {
            Gizmos.DrawCube(new Vector3(cell.x + 0.5f, cell.y + 0.5f, 0), Vector3.one * 0.5f);
        }
    }
}