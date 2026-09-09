using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Generador de mapas 2D mediante BSP (Binary Space Partitioning).
///
/// Flujo:
///   1. Particiona el �rea total recursivamente en un �rbol binario.
///   2. Crea una sala dentro de cada partici�n hoja.
///   3. Conecta las salas con pasillos en L, recorriendo el �rbol
///      (conecta hermanos, luego niveles superiores).
///   4. Vuelca el resultado a un CellType[,] y lo pinta en un Tilemap.
///
/// Este componente es el punto de partida del pipeline: expone <see cref="Result"/>
/// (grid + lista de salas) para que Perlin Noise, Random Walk, L-System y la
/// Gram�tica de misiones trabajen sobre la misma estructura de datos.
/// </summary>
public class BspMapGenerator : MonoBehaviour
{
    [Header("Dimensiones del mapa (en celdas de grid)")]
    [SerializeField] private int mapWidth = 60;
    [SerializeField] private int mapHeight = 40;

    [Header("Parámetros de partición BSP")]
    [Tooltip("Tamaño minimo de una partición. Evita salas o cortes demasiado pequeños.")]
    [SerializeField] private int minPartitionSize = 8;
    [Tooltip("Cuantas veces se intenta subdividir recursivamente. M�s iteraciones = más salas.")]
    [SerializeField] private int maxIterations = 5;

    [Header("Parámetros de las salas")]
    [Tooltip("Margen entre el borde de la partición y el borde de la sala.")]
    [SerializeField] private int roomPadding = 1;
    [SerializeField] private int minRoomSize = 4;

    [Header("Pasillos")]
    [SerializeField] private int corridorWidth = 1;

    [Header("Tipo de corredor")]
    [Tooltip("Si es true, los corredores son rectos (horizontal O vertical). Si es false, son en forma de L (estilo caverna).")]
    [SerializeField] private bool useStraightCorridors = false;

    [Header("Semilla")]
    [SerializeField] private bool useRandomSeed = true;
    [SerializeField] private int seed = 0;

    [Header("Capa ambiental (opcional)")]
    [Tooltip("Si se asigna, permite consultar el valor de Perlin de cada sala tras generar (til para el L-System y la Gramtica de misiones ms adelante). No afecta la geometra del BSP en s.")]
    [SerializeField] private PerlinMapGenerator perlinGenerator;

    [Header("Debug")]
    [SerializeField] private bool showGizmo = true;



    [Header("Ejecucion")]
    [Tooltip("Genera en Start(). Desactivalo si PipelineManager coordina la ejecucion completa.")]
    [SerializeField] private bool generateOnStart = false;

    private System.Random _rng;
    private BspNode _root;

    /// <summary>
    /// Raiz del arbol BSP. Expuesta como lectura para que otros algoritmos
    /// del pipeline (como la Gramatica de misiones) puedan reconstruir la
    /// adyacencia entre salas recorriendo la estructura del arbol.
    /// </summary>
    public BspNode Root => _root;

    /// <summary>
    /// Resultado pblico del generador: grid + lista de salas.
    /// Los dems algoritmos del pipeline deben leer de aqu.
    /// </summary>
    public BspMapResult Result { get; private set; }

    /// <summary>
    /// Permite que un orquestador externo (PipelineManager) sincronice el
    /// tamao del mapa, para que BSP, Random Walk y Perlin usen siempre las
    /// mismas dimensiones sin tener que editarlas a mano en cada Inspector.
    /// </summary>
    public void SetDimensions(int width, int height)
    {
        mapWidth = width;
        mapHeight = height;
    }

    /// <summary>
    /// Fija la semilla manualmente y desactiva la generacion aleatoria.
    /// </summary>
    public void SetSeed(int newSeed) { useRandomSeed = false; seed = newSeed; }
    public void SetMinPartitionSize(int value) { minPartitionSize = value; }
    public void SetMaxIterations(int value) { maxIterations = value; }
    public void SetRoomPadding(int value) { roomPadding = value; }
    public void SetMinRoomSize(int value) { minRoomSize = value; }
    public void SetCorridorWidth(int value) { corridorWidth = value; }
    public void SetUseStraightCorridors(bool value) { useStraightCorridors = value; }

    // Getters para inicializar la UI con los valores actuales
    public int Seed => seed;
    public int MapWidthValue => mapWidth;
    public int MapHeightValue => mapHeight;
    public int GetMinPartitionSize() => minPartitionSize;
    public int GetMaxIterations() => maxIterations;
    public int GetRoomPadding() => roomPadding;
    public int GetMinRoomSize() => minRoomSize;
    public int GetCorridorWidth() => corridorWidth;
    public bool GetUseStraightCorridors() => useStraightCorridors;


    /// <summary>
    /// Valor ambiental (Perlin) promedio de una sala, si hay un
    /// PerlinMapGenerator asignado y ya gener su mapa. Devuelve 0 si no hay
    /// datos disponibles, para que llamarlo sea siempre seguro.
    /// </summary>
    public float GetRoomEnvironmentValue(BspRoom room)
    {
        if (perlinGenerator == null || perlinGenerator.NoiseMap == null) return 0f;
        return perlinGenerator.GetAverageValueInRoom(room);
    }

    private void Start()
    {
        if (generateOnStart)
        {
            Generate();
        }
    }

    /// <summary>
    /// Ejecuta el proceso completo de generacin. Puede llamarse desde otro
    /// script (por ejemplo, un GameManager que orquesta todo el pipeline)
    /// en lugar de depender de Start().
    /// </summary>
    public BspMapResult Generate()
    {
        seed = useRandomSeed ? System.Environment.TickCount : seed;
        _rng = new System.Random(seed);

        Result = new BspMapResult(mapWidth, mapHeight);

        // 1. Construir el �rbol BSP particionando el espacio completo.
        _root = new BspNode(new RectInt(0, 0, mapWidth, mapHeight));
        BuildTree(_root, maxIterations);

        // 2. Crear una sala dentro de cada hoja del �rbol.
        int idCounter = 0;
        CreateRooms(_root, ref idCounter);

        // 3. Conectar las salas con pasillos, recorriendo el rbol de abajo hacia arriba.
        ConnectRooms(_root);

        // 4. Pintar paredes alrededor de todo lo que es piso.
        MapUtils.PaintWalls(Result);

        return Result;
    }

    // ---------------------------------------------------------------------
    // 1. Particin recursiva
    // ---------------------------------------------------------------------
    private void BuildTree(BspNode node, int iterationsLeft)
    {
        if (iterationsLeft <= 0) return;

        if (node.Split(minPartitionSize, _rng))
        {
            BuildTree(node.Left, iterationsLeft - 1);
            BuildTree(node.Right, iterationsLeft - 1);
        }
        // Si Split() falla, el nodo se queda como hoja (�rea muy peque�a).
    }

    // ---------------------------------------------------------------------
    // 2. Generaci�n de salas dentro de cada hoja
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
    // 3. Conexi�n de salas mediante pasillos en L
    // ---------------------------------------------------------------------

    /// <summary>
    /// Recorre el �rbol de abajo hacia arriba: cada nodo interno conecta una
    /// sala representativa de su sub�rbol izquierdo con una de su sub�rbol
    /// derecho. Esto garantiza que el grafo de salas quede totalmente conexo.
    /// </summary>
    private BspRoom ConnectRooms(BspNode node)
    {
        if (node.IsLeaf) return node.Room;

        BspRoom leftRoom = ConnectRooms(node.Left);
        BspRoom rightRoom = ConnectRooms(node.Right);

        if (leftRoom != null && rightRoom != null)
        {
            if (useStraightCorridors)
                CarveCorridorStraight(node, leftRoom, rightRoom);
            else
                CarveCorridor(leftRoom.Center, rightRoom.Center);
        }

        // Sube una sala representativa hacia el nivel superior del �rbol.
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

    /// <summary>
    /// Excava un corredor RECTO entre dos salas usando la direccion de
    /// particion del nodo padre (SplitHorizontal):
    ///   true  = salas arriba/abajo = corredor vertical en X promedio.
    ///   false = salas izq/der      = corredor horizontal en Y promedio.
    /// </summary>
    private void CarveCorridorStraight(BspNode parentNode, BspRoom roomA, BspRoom roomB)
    {
        Vector2Int a = roomA.Center;
        Vector2Int b = roomB.Center;

        if (parentNode.SplitHorizontal)
        {
            int midX = Mathf.Clamp((a.x + b.x) / 2, 0, Result.Width - 1);
            CarveVertical(a.y, b.y, midX);
        }
        else
        {
            int midY = Mathf.Clamp((a.y + b.y) / 2, 0, Result.Height - 1);
            CarveHorizontal(a.x, b.x, midY);
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
    /// Excava una celda de pasillo, expandiendo el ancho seg�n corridorWidth.
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
    /// Pensado para el bot�n "Limpiar" del editor.
    /// </summary>
    public void ClearMap()
    {
        Result = null;
    }



    // ---------------------------------------------------------------------
    // Debug visual en el editor (�til mientras no tienes tiles asignados)
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
        // Gram�tica de misiones podr� referenciarlas correctamente.
        Gizmos.color = Color.red;
        foreach (var room in Result.Rooms)
        {
            Gizmos.DrawSphere(new Vector3(room.Center.x + 0.5f, room.Center.y + 0.5f, 0), 0.3f);
        }
    }
}
