using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>
/// Generador de mapas 2D mediante BSP (Binary Space Partitioning).
///
/// Flujo:
///   1. Particiona el área total recursivamente en un árbol binario.
///   2. Crea una sala dentro de cada partición hoja.
///   3. Conecta las salas con pasillos en L, recorriendo el árbol
///      (conecta hermanos, luego niveles superiores).
///   4. Vuelca el resultado a un CellType[,] y lo pinta en un Tilemap.
///
/// Este componente es el punto de partida del pipeline: expone <see cref="Result"/>
/// (grid + lista de salas) para que Perlin Noise, Random Walk, L-System y la
/// Gramática de misiones trabajen sobre la misma estructura de datos.
/// </summary>
public class BspMapGenerator : MonoBehaviour
{
    [Header("Dimensiones del mapa (en celdas de grid)")]
    [SerializeField] private int mapWidth = 60;
    [SerializeField] private int mapHeight = 40;

    [Header("Parámetros de partición BSP")]
    [Tooltip("Tamaño mínimo de una partición. Evita salas o cortes demasiado pequeños.")]
    [SerializeField] private int minPartitionSize = 8;
    [Tooltip("Cuántas veces se intenta subdividir recursivamente. Más iteraciones = más salas.")]
    [SerializeField] private int maxIterations = 5;

    [Header("Parámetros de las salas")]
    [Tooltip("Margen entre el borde de la partición y el borde de la sala.")]
    [SerializeField] private int roomPadding = 1;
    [SerializeField] private int minRoomSize = 4;

    [Header("Pasillos")]
    [SerializeField] private int corridorWidth = 1;

    [Header("Semilla")]
    [SerializeField] private bool useRandomSeed = true;
    [SerializeField] private int seed = 0;

    [Header("Capa ambiental (opcional)")]
    [Tooltip("Si se asigna, permite consultar el valor de Perlin de cada sala tras generar (útil para el L-System y la Gramática de misiones más adelante). No afecta la geometría del BSP en sí.")]
    [SerializeField] private PerlinMapGenerator perlinGenerator;

    [Header("Debug")]
    [SerializeField] private bool showGizmo = true;

    [Header("Tilemap (opcional, para visualizar)")]
    [SerializeField] private Tilemap floorTilemap;
    [SerializeField] private Tilemap wallTilemap;
    [SerializeField] private TileBase floorTile;
    [SerializeField] private TileBase wallTile;

    private System.Random _rng;
    private BspNode _root;

    /// <summary>
    /// Resultado público del generador: grid + lista de salas.
    /// Los demás algoritmos del pipeline deben leer de aquí.
    /// </summary>
    public BspMapResult Result { get; private set; }

    /// <summary>
    /// Permite que un orquestador externo (PipelineManager) sincronice el
    /// tamaño del mapa, para que BSP, Random Walk y Perlin usen siempre las
    /// mismas dimensiones sin tener que editarlas a mano en cada Inspector.
    /// </summary>
    public void SetDimensions(int width, int height)
    {
        mapWidth = width;
        mapHeight = height;
    }

    /// <summary>
    /// Valor ambiental (Perlin) promedio de una sala, si hay un
    /// PerlinMapGenerator asignado y ya generó su mapa. Devuelve 0 si no hay
    /// datos disponibles, para que llamarlo sea siempre seguro.
    /// </summary>
    public float GetRoomEnvironmentValue(BspRoom room)
    {
        if (perlinGenerator == null || perlinGenerator.NoiseMap == null) return 0f;
        return perlinGenerator.GetAverageValueInRoom(room);
    }

    private void Start()
    {
        Generate();
    }

    /// <summary>
    /// Ejecuta el proceso completo de generación. Puede llamarse desde otro
    /// script (por ejemplo, un GameManager que orquesta todo el pipeline)
    /// en lugar de depender de Start().
    /// </summary>
    public BspMapResult Generate()
    {
        seed = useRandomSeed ? System.Environment.TickCount : seed;
        _rng = new System.Random(seed);

        Result = new BspMapResult(mapWidth, mapHeight);

        // 1. Construir el árbol BSP particionando el espacio completo.
        _root = new BspNode(new RectInt(0, 0, mapWidth, mapHeight));
        BuildTree(_root, maxIterations);

        // 2. Crear una sala dentro de cada hoja del árbol.
        int idCounter = 0;
        CreateRooms(_root, ref idCounter);

        // 3. Conectar las salas con pasillos, recorriendo el árbol de abajo hacia arriba.
        ConnectRooms(_root);

        // 4. Pintar paredes alrededor de todo lo que es piso.
        MapUtils.PaintWalls(Result);

        // 5. Volcar el resultado a los Tilemaps, si están asignados.
        if (floorTilemap != null || wallTilemap != null)
        {
            DrawTilemap();
        }

        return Result;
    }

    // ---------------------------------------------------------------------
    // 1. Partición recursiva
    // ---------------------------------------------------------------------
    private void BuildTree(BspNode node, int iterationsLeft)
    {
        if (iterationsLeft <= 0) return;

        if (node.Split(minPartitionSize, _rng))
        {
            BuildTree(node.Left, iterationsLeft - 1);
            BuildTree(node.Right, iterationsLeft - 1);
        }
        // Si Split() falla, el nodo se queda como hoja (área muy pequeña).
    }

    // ---------------------------------------------------------------------
    // 2. Generación de salas dentro de cada hoja
    // ---------------------------------------------------------------------
    private void CreateRooms(BspNode node, ref int idCounter)
    {
        if (node.IsLeaf)
        {
            RectInt area = node.Area;

            int maxRoomW = Mathf.Max(minRoomSize, area.width - roomPadding * 2);
            int maxRoomH = Mathf.Max(minRoomSize, area.height - roomPadding * 2);

            int roomW = Mathf.Clamp(_rng.Next(minRoomSize, maxRoomW + 1), minRoomSize, area.width - roomPadding * 2);
            int roomH = Mathf.Clamp(_rng.Next(minRoomSize, maxRoomH + 1), minRoomSize, area.height - roomPadding * 2);

            int maxOffsetX = area.width - roomW - roomPadding;
            int maxOffsetY = area.height - roomH - roomPadding;

            int offsetX = maxOffsetX > roomPadding ? _rng.Next(roomPadding, maxOffsetX) : roomPadding;
            int offsetY = maxOffsetY > roomPadding ? _rng.Next(roomPadding, maxOffsetY) : roomPadding;

            RectInt roomRect = new RectInt(area.x + offsetX, area.y + offsetY, roomW, roomH);

            var room = new BspRoom(roomRect, idCounter++);
            node.Room = room;
            Result.Rooms.Add(room);

            CarveRoom(roomRect);
            return;
        }

        CreateRooms(node.Left, ref idCounter);
        CreateRooms(node.Right, ref idCounter);
    }

    private void CarveRoom(RectInt rect)
    {
        for (int x = rect.x; x < rect.x + rect.width; x++)
        {
            for (int y = rect.y; y < rect.y + rect.height; y++)
            {
                if (Result.InBounds(x, y))
                    Result.Grid[x, y] = CellType.Floor;
            }
        }
    }

    // ---------------------------------------------------------------------
    // 3. Conexión de salas mediante pasillos en L
    // ---------------------------------------------------------------------

    /// <summary>
    /// Recorre el árbol de abajo hacia arriba: cada nodo interno conecta una
    /// sala representativa de su subárbol izquierdo con una de su subárbol
    /// derecho. Esto garantiza que el grafo de salas quede totalmente conexo.
    /// </summary>
    private BspRoom ConnectRooms(BspNode node)
    {
        if (node.IsLeaf) return node.Room;

        BspRoom leftRoom = ConnectRooms(node.Left);
        BspRoom rightRoom = ConnectRooms(node.Right);

        if (leftRoom != null && rightRoom != null)
        {
            CarveCorridor(leftRoom.Center, rightRoom.Center);
        }

        // Sube una sala representativa hacia el nivel superior del árbol.
        return leftRoom ?? rightRoom;
    }

    /// <summary>
    /// Excava un pasillo en forma de L entre dos puntos, eligiendo al azar si
    /// primero se mueve en horizontal o en vertical (evita que todos los
    /// pasillos tengan la misma forma visual).
    /// </summary>
    private void CarveCorridor(Vector2Int from, Vector2Int to)
    {
        if (_rng.NextDouble() > 0.5)
        {
            CarveHorizontal(from.x, to.x, from.y);
            CarveVertical(from.y, to.y, to.x);
        }
        else
        {
            CarveVertical(from.y, to.y, from.x);
            CarveHorizontal(from.x, to.x, to.y);
        }
    }

    private void CarveHorizontal(int x1, int x2, int y)
    {
        int start = Mathf.Min(x1, x2);
        int end = Mathf.Max(x1, x2);
        for (int x = start; x <= end; x++)
        {
            CarveCorridorCell(x, y);
        }
    }

    private void CarveVertical(int y1, int y2, int x)
    {
        int start = Mathf.Min(y1, y2);
        int end = Mathf.Max(y1, y2);
        for (int y = start; y <= end; y++)
        {
            CarveCorridorCell(x, y);
        }
    }

    /// <summary>
    /// Excava una celda de pasillo, expandiendo el ancho según corridorWidth.
    /// </summary>
    private void CarveCorridorCell(int cx, int cy)
    {
        int half = corridorWidth / 2;
        for (int dx = -half; dx <= half; dx++)
        {
            for (int dy = -half; dy <= half; dy++)
            {
                int x = cx + dx;
                int y = cy + dy;
                if (Result.InBounds(x, y))
                    Result.Grid[x, y] = CellType.Floor;
            }
        }
    }

    /// <summary>
    /// Borra el resultado actual y los tilemaps, sin generar uno nuevo.
    /// Pensado para el botón "Limpiar" del editor.
    /// </summary>
    public void ClearMap()
    {
        Result = null;

        if (floorTilemap != null) floorTilemap.ClearAllTiles();
        if (wallTilemap != null) wallTilemap.ClearAllTiles();
    }

    // ---------------------------------------------------------------------
    // 5. Volcado a Tilemap
    // ---------------------------------------------------------------------
    private void DrawTilemap()
    {
        if (floorTilemap != null) floorTilemap.ClearAllTiles();
        if (wallTilemap != null) wallTilemap.ClearAllTiles();

        for (int x = 0; x < Result.Width; x++)
        {
            for (int y = 0; y < Result.Height; y++)
            {
                var cell = Result.Grid[x, y];
                var pos = new Vector3Int(x, y, 0);

                if (cell == CellType.Floor && floorTilemap != null && floorTile != null)
                {
                    floorTilemap.SetTile(pos, floorTile);
                }
                else if (cell == CellType.Wall && wallTilemap != null && wallTile != null)
                {
                    wallTilemap.SetTile(pos, wallTile);
                }
            }
        }
    }

    // ---------------------------------------------------------------------
    // Debug visual en el editor (útil mientras no tienes tiles asignados)
    // ---------------------------------------------------------------------
    private void OnDrawGizmos()
    {
        if (Result == null || !showGizmo) return;

        for (int x = 0; x < Result.Width; x++)
        {
            for (int y = 0; y < Result.Height; y++)
            {
                var cell = Result.Grid[x, y];
                if (cell == CellType.Empty) continue;

                Gizmos.color = cell == CellType.Floor
                    ? new Color(0.3f, 0.3f, 0.3f, 0.6f)
                    : new Color(0.6f, 0.4f, 0.2f, 0.8f);

                Gizmos.DrawCube(new Vector3(x + 0.5f, y + 0.5f, 0), Vector3.one * 0.95f);
            }
        }

        // Marca el centro de cada sala con su Id, para verificar que la
        // Gramática de misiones podrá referenciarlas correctamente.
        Gizmos.color = Color.red;
        foreach (var room in Result.Rooms)
        {
            Gizmos.DrawSphere(new Vector3(room.Center.x + 0.5f, room.Center.y + 0.5f, 0), 0.3f);
        }
    }
}