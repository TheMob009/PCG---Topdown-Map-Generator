using System.Collections.Generic;
using UnityEngine;

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

    [Header("Forma del trazo")]
    [Tooltip("Probabilidad de mantener la misma dirección del paso anterior en vez de elegir una nueva al azar. 0 = completamente aleatorio (comportamiento original, trazo errante tipo galería). Valores altos (ej. 0.85) producen trazos rectos con giros ocasionales, como un ducto de ventilación.")]
    [Range(0f, 1f)]
    [SerializeField] private float directionPersistence = 0f;

    [Tooltip("Si es true, cada agente nace en una esquina/borde de su sala de partida en vez del centro. Útil para que los túneles se sientan como ductos que corren pegados a los muros de los módulos, en vez de perforar el centro de las salas.")]
    [SerializeField] private bool spawnFromRoomEdge = false;

    [Header("Semilla")]
    [SerializeField] private bool useRandomSeed = true;
    [SerializeField] private int seed = 0;



    [Header("Visualización")]
[SerializeField] private bool showGizmo = true;


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
    /// venían del BSP). Expuesto para que MapVisualizer pueda diferenciar
    /// visualmente los túneles del Random Walk de las salas del BSP.
    /// </summary>
    public HashSet<Vector2Int> CarvedByWalk => _carvedByWalk;
    private readonly HashSet<Vector2Int> _carvedByWalk = new HashSet<Vector2Int>();

    /// <summary>
    /// Fija la semilla manualmente y desactiva la generacion aleatoria.
    /// </summary>
    public void SetSeed(int newSeed) { useRandomSeed = false; seed = newSeed; }
    public void SetAgentCount(int value) { agentCount = value; }
    public void SetStepsPerAgent(int value) { stepsPerAgent = value; }
    public void SetWalkWidth(int value) { walkWidth = value; }
    public void SetDirectionPersistence(float value) { directionPersistence = Mathf.Clamp01(value); }
    public void SetSpawnFromRoomEdge(bool value) { spawnFromRoomEdge = value; }

    // Getters para inicializar la UI con los valores actuales
    public int Seed => seed;
    public int GetAgentCount() => agentCount;
    public int GetStepsPerAgent() => stepsPerAgent;
    public int GetWalkWidth() => walkWidth;
    public float GetDirectionPersistence() => directionPersistence;
    public bool GetSpawnFromRoomEdge() => spawnFromRoomEdge;

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
    }

    /// <summary>
    /// Hace caminar a un único agente: parte del centro de una sala aleatoria
    /// y da stepsPerAgent pasos en direcciones aleatorias, tallando piso.
    /// </summary>
    private void RunAgent(BspMapResult result)
    {
        var startRoom = result.Rooms[_rng.Next(result.Rooms.Count)];
        Vector2Int pos = spawnFromRoomEdge
            ? GetRoomEdgeSpawnPoint(startRoom)
            : startRoom.Center;

        // Dirección inicial aleatoria; se reutiliza entre pasos según
        // directionPersistence para sesgar el trazo (más recto o más errante)
        // sin cambiar el algoritmo: sigue siendo el mismo Random Walk, solo
        // con una probabilidad de repetir la última dirección elegida.
        Vector2Int lastDir = Directions[_rng.Next(Directions.Length)];

        for (int step = 0; step < stepsPerAgent; step++)
        {
            CarveAt(result, pos);

            Vector2Int dir = _rng.NextDouble() < directionPersistence
                ? lastDir
                : Directions[_rng.Next(Directions.Length)];

            var next = pos + dir;

            // Si la dirección persistida se sale del mapa, se prueba una
            // dirección alternativa al azar para este paso, evitando que un
            // agente con alta persistencia quede pegado sin avanzar contra
            // el borde del mapa.
            if (!result.InBounds(next.x, next.y))
            {
                dir = Directions[_rng.Next(Directions.Length)];
                next = pos + dir;
            }

            if (result.InBounds(next.x, next.y))
            {
                pos = next;
            }

            lastDir = dir;
        }

        // Talla también la última posición alcanzada.
        CarveAt(result, pos);
    }

    /// <summary>
    /// Elige un punto de partida en una esquina de la sala (con un pequeño
    /// margen hacia adentro para seguir siendo piso válido), en vez del
    /// centro. Simula que el túnel/ducto nace pegado al borde del módulo.
    /// </summary>
    private Vector2Int GetRoomEdgeSpawnPoint(BspRoom room)
    {
        var b = room.Bounds;

        int insetX = Mathf.Clamp(b.width - 1, 0, 1);
        int insetY = Mathf.Clamp(b.height - 1, 0, 1);

        int corner = _rng.Next(4);
        int x, y;

        switch (corner)
        {
            case 0: x = b.x + insetX; y = b.y + insetY; break;                               // inferior-izquierda
            case 1: x = b.x + b.width - 1 - insetX; y = b.y + insetY; break;                  // inferior-derecha
            case 2: x = b.x + insetX; y = b.y + b.height - 1 - insetY; break;                 // superior-izquierda
            default: x = b.x + b.width - 1 - insetX; y = b.y + b.height - 1 - insetY; break;  // superior-derecha
        }

        return new Vector2Int(
            Mathf.Clamp(x, b.x, b.x + b.width - 1),
            Mathf.Clamp(y, b.y, b.y + b.height - 1));
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
            if (Result == null || !showGizmo) return;
            Gizmos.DrawCube(new Vector3(cell.x + 0.5f, cell.y + 0.5f, 0), Vector3.one * 0.5f);
        }
    }
}