using System.Collections.Generic;
using System.Text;
using UnityEngine;

/// <summary>
/// Etapa 5 del pipeline: genera una misión sobre el mapa BSP.
///
/// 1. Genera una cadena simbólica usando gramática secuencial configurable.
/// 2. Asigna cada símbolo terminal a una sala del BSP siguiendo un
///    recorrido por adyacencia (BFS sobre el grafo de conexiones del árbol BSP).
/// 3. Dibuja Gizmos de colores y etiquetas sobre las salas asignadas.
/// 4. Imprime en consola la derivación, cadena final y misión interpretada
///    en el contexto narrativo elegido (Mina o Colonia).
///
/// Requiere que el BSP ya haya generado su resultado antes de ejecutarse.
/// </summary>
public class MissionGrammarGenerator : MonoBehaviour
{
    [Header("Gramatica")]

    [Tooltip("Simbolo inicial de la gramatica (no-terminal).")]
    [SerializeField] private string startSymbol = "M";

    [Tooltip("Produccion del simbolo inicial. Debe contener al menos un taskSymbol para que haya expansion.")]
    [SerializeField] private string startProduction = "SETG";

    [Tooltip("Caracter no-terminal que se expande en cada paso (debe ser un solo caracter).")]
    [SerializeField] private string taskSymbol = "T";

    [Tooltip("Producciones posibles para el taskSymbol. Se eligen aleatoriamente en cada expansion.")]
    [SerializeField]
    private List<string> taskProductions = new List<string>()
    {
        "RT",
        "CT",
        "KTL",
        "ET",
    };

    [Tooltip("Produccion terminal: reemplaza todos los taskSymbol restantes al finalizar las expansiones.")]
    [SerializeField] private string terminalProduction = "C";

    [Tooltip("Cantidad de expansiones antes de finalizar. Mas pasos = cadena mas larga.")]
    [Range(1, 10)]
    [SerializeField] private int expansionSteps = 4;

    [Header("Referencias")]
    [Tooltip("BspMapGenerator del que se toman las salas y el arbol para la adyacencia.")]
    [SerializeField] private BspMapGenerator bspGenerator;

    [Header("Semilla")]
    [SerializeField] private bool useRandomSeed = true;
    [SerializeField] private int seed = 42;

    [Header("Contexto narrativo")]
    [Tooltip("Cambia las descripciones textuales de la mision (no la mecanica).")]
    [SerializeField] private MissionContext missionContext = MissionContext.Mina;

    [Header("Debug")]
    [SerializeField] private bool showGizmo = true;

    public string FinalChain { get; private set; }

    public List<MissionRoomAssignment> Assignments { get; private set; }

    public List<string> Derivation { get; private set; }

    public void SetSeed(int newSeed) { useRandomSeed = false; seed = newSeed; }
    public void SetStartSymbol(string value) { startSymbol = value; }
    public void SetTaskSymbol(string value) { taskSymbol = value; }
    public void SetStartProduction(string value) { startProduction = value; }
    public void SetExpansionSteps(int value) { expansionSteps = value; }
    public void SetMissionContext(MissionContext ctx) { missionContext = ctx; }
    public void SetTerminalProduction(string value) { terminalProduction = value; }
    public void SetTaskProductions(System.Collections.Generic.List<string> productions) { taskProductions = productions; }

    // Getters para inicializar la UI con los valores actuales
    public int Seed => seed;
    public string GetStartSymbol() => startSymbol;
    public string GetTaskSymbol() => taskSymbol;
    public string GetStartProduction() => startProduction;
    public int GetExpansionSteps() => expansionSteps;
    public MissionContext GetMissionContext() => missionContext;
    public string GetTerminalProduction() => terminalProduction;
    public System.Collections.Generic.List<string> GetTaskProductions() => taskProductions;

    /// <summary>
    /// Ejecuta el proceso completo: gramática → asignación → impresión.
    /// </summary>
    public void Generate()
    {
        Assignments = null;
        FinalChain = null;

        if (bspGenerator == null)
        {
            Debug.LogError("[MissionGrammar] No hay un BspMapGenerator asignado.", this);
            return;
        }

        if (bspGenerator.Result == null || bspGenerator.Result.Rooms.Count == 0)
        {
            Debug.LogError("[MissionGrammar] El BSP debe generar salas antes de crear la mision.", this);
            return;
        }

        if (!ValidateGrammar()) return;

        seed = useRandomSeed ? System.Environment.TickCount : seed;
        var rng = new System.Random(seed);

        // 1. Generar la cadena simbólica
        FinalChain = ExpandGrammar(rng);

        // 2. Extraer sólo los símbolos terminales reconocidos
        var symbols = ParseChain(FinalChain);

        if (symbols.Count == 0)
        {
            Debug.LogWarning("[MissionGrammar] La cadena final no contiene simbolos terminales reconocidos.", this);
            return;
        }

        // 3. Construir grafo de adyacencia desde el árbol BSP
        var adjacency = BuildAdjacencyGraph(bspGenerator.Root, bspGenerator.Result.Rooms);

        // 4. Recorrer por BFS y asignar símbolos a salas
        Assignments = AssignToRooms(symbols, bspGenerator.Result.Rooms, adjacency, rng);

        // 5. Imprimir en consola
        PrintMission();
    }

    private string ExpandGrammar(System.Random rng)
    {
        Derivation = new List<string>();

        // Paso 0: axioma → producción inicial
        string current = startProduction;
        Derivation.Add(startSymbol + " -> " + current);

        // Expansiones secuenciales
        for (int step = 0; step < expansionSteps; step++)
        {
            int taskIndex = current.IndexOf(taskSymbol[0]);
            if (taskIndex == -1) break; // no quedan T por expandir

            int productionIndex = rng.Next(0, taskProductions.Count);
            string production = taskProductions[productionIndex];

            var sb = new StringBuilder(current);
            sb.Remove(taskIndex, 1);
            sb.Insert(taskIndex, production);
            current = sb.ToString();

            Derivation.Add(current);
        }

        // Finalización: reemplazar las T restantes por la producción terminal
        current = current.Replace(taskSymbol, terminalProduction);
        Derivation.Add(current);

        return current;
    }

    private List<MissionSymbolType> ParseChain(string chain)
    {
        var result = new List<MissionSymbolType>();
        foreach (char c in chain)
        {
            var type = MissionSymbolInfo.FromChar(c);
            if (type.HasValue)
                result.Add(type.Value);
        }
        return result;
    }

    /// <summary>
    /// Recorre el árbol BSP y conecta las salas que el BSP unió con pasillos
    /// (hermanos en el árbol = salas adyacentes). El resultado es un diccionario
    /// roomId → lista de roomIds vecinos.
    /// </summary>
    private Dictionary<int, HashSet<int>> BuildAdjacencyGraph(BspNode root, List<BspRoom> rooms)
    {
        var adj = new Dictionary<int, HashSet<int>>();

        // Inicializar entrada para cada sala
        foreach (var room in rooms)
        {
            adj[room.Id] = new HashSet<int>();
        }

        // Recorrer el árbol: cada nodo interno conecta una sala del subárbol
        // izquierdo con una del subárbol derecho.
        CollectAdjacency(root, adj);

        return adj;
    }

    /// <summary>
    /// Recorrido recursivo del árbol. En cada nodo interno, la sala representativa
    /// del hijo izquierdo y la del hijo derecho están conectadas por un pasillo.
    /// También conectamos todas las hojas dentro de cada subárbol transitivamente.
    /// </summary>
    private BspRoom CollectAdjacency(BspNode node, Dictionary<int, HashSet<int>> adj)
    {
        if (node == null) return null;

        if (node.IsLeaf)
            return node.Room;

        BspRoom leftRoom = CollectAdjacency(node.Left, adj);
        BspRoom rightRoom = CollectAdjacency(node.Right, adj);

        if (leftRoom != null && rightRoom != null)
        {
            // Estas dos salas están conectadas por un pasillo del BSP
            adj[leftRoom.Id].Add(rightRoom.Id);
            adj[rightRoom.Id].Add(leftRoom.Id);
        }

        return leftRoom ?? rightRoom;
    }
    /// <summary>
    /// Hace un BFS desde la primera sala (sala de inicio del jugador, S) y
    /// asigna cada símbolo de la misión a una sala en el orden del recorrido.
    /// Si hay más símbolos que salas, reutiliza las últimas salas disponibles.
    /// </summary>
    private List<MissionRoomAssignment> AssignToRooms(
        List<MissionSymbolType> symbols,
        List<BspRoom> rooms,
        Dictionary<int, HashSet<int>> adjacency,
        System.Random rng)
    {
        // BFS desde la sala 0 (sala de inicio del jugador)
        var visited = new List<BspRoom>();
        var visitedIds = new HashSet<int>();
        var queue = new Queue<BspRoom>();

        BspRoom startRoom = rooms[0]; // primera sala generada por el BSP
        queue.Enqueue(startRoom);
        visitedIds.Add(startRoom.Id);

        while (queue.Count > 0)
        {
            var current = queue.Dequeue();
            visited.Add(current);

            // Obtener vecinos y mezclarlos para variedad
            if (adjacency.ContainsKey(current.Id))
            {
                var neighbors = new List<int>(adjacency[current.Id]);
                // Shuffle de Fisher-Yates para que el BFS no sea siempre igual
                for (int i = neighbors.Count - 1; i > 0; i--)
                {
                    int j = rng.Next(0, i + 1);
                    int tmp = neighbors[i];
                    neighbors[i] = neighbors[j];
                    neighbors[j] = tmp;
                }

                foreach (int neighborId in neighbors)
                {
                    if (!visitedIds.Contains(neighborId))
                    {
                        visitedIds.Add(neighborId);
                        BspRoom neighborRoom = rooms.Find(r => r.Id == neighborId);
                        if (neighborRoom != null)
                        {
                            queue.Enqueue(neighborRoom);
                        }

                    }
                }
            }
        }

        // Asignar símbolos a salas en orden BFS
        var assignments = new List<MissionRoomAssignment>();
        for (int i = 0; i < symbols.Count; i++)
        {
            // Fallback: si estamos en la última sala disponible y aún quedan
            // símbolos por asignar, saltar directamente al símbolo final de
            // la cadena y descartar los intermedios que no caben.
            if (i == visited.Count - 1 && i < symbols.Count - 1)
            {
                int discarded = symbols.Count - 1 - i;
                Debug.LogWarning(
                    "[MissionGrammar] Salas insuficientes: se descartaron "
                    + discarded + " simbolo(s) intermedios. "
                    + "La ultima sala recibe el simbolo final de la cadena.", this);

                var lastSymbol = symbols[symbols.Count - 1];
                assignments.Add(new MissionRoomAssignment(
                    visited[i],
                    lastSymbol,
                    MissionSymbolInfo.ToChar(lastSymbol)
                ));
                break;
            }

            assignments.Add(new MissionRoomAssignment(
                visited[i],
                symbols[i],
                MissionSymbolInfo.ToChar(symbols[i])
            ));
        }

        return assignments;
    }

    private void PrintMission()
    {
        var sb = new StringBuilder();

        sb.AppendLine("===== GRAMATICA DE MISIONES =====");
        sb.AppendLine();

        // Reglas
        sb.AppendLine("REGLAS:");
        sb.AppendLine(startSymbol + " -> " + startProduction);
        foreach (var prod in taskProductions)
        {
            sb.AppendLine(taskSymbol + " -> " + prod);
        }
        sb.AppendLine(taskSymbol + " -> " + terminalProduction + " (terminal)");
        sb.AppendLine();

        // Derivación
        sb.AppendLine("DERIVACION:");
        for (int i = 0; i < Derivation.Count; i++)
        {
            sb.AppendLine("Paso " + i + ": " + Derivation[i]);
        }
        sb.AppendLine();

        // Cadena final
        sb.AppendLine("CADENA FINAL: " + FinalChain);
        sb.AppendLine("SEMILLA: " + seed);
        sb.AppendLine();

        // Asignación a salas
        sb.AppendLine("ASIGNACION A SALAS:");
        if (Assignments != null)
        {
            foreach (var a in Assignments)
            {
                sb.AppendLine("  Sala " + a.Room.Id + " [" + a.Symbol + "]: "
                    + MissionSymbolInfo.GetDescription(a.SymbolType, missionContext));
            }
        }
        sb.AppendLine();

        // Misión interpretada
        string contextName = missionContext == MissionContext.Mina ? "Mina" : "Colonia";
        sb.AppendLine("MISION (" + contextName + "):");
        if (Assignments != null)
        {
            for (int i = 0; i < Assignments.Count; i++)
            {
                string desc = MissionSymbolInfo.GetDescription(Assignments[i].SymbolType, missionContext);
                string arrow = (i < Assignments.Count - 1) ? " -> " : ".";
                sb.Append(desc + arrow);
            }
            sb.AppendLine();
        }

        Debug.Log(sb.ToString(), this);
    }

    private bool ValidateGrammar()
    {
        if (string.IsNullOrEmpty(startSymbol))
        {
            Debug.LogError("[MissionGrammar] startSymbol no puede estar vacio.", this);
            return false;
        }

        if (string.IsNullOrEmpty(startProduction))
        {
            Debug.LogError("[MissionGrammar] startProduction no puede estar vacia.", this);
            return false;
        }

        if (string.IsNullOrEmpty(taskSymbol))
        {
            Debug.LogError("[MissionGrammar] taskSymbol no puede estar vacio.", this);
            return false;
        }

        if (taskProductions == null || taskProductions.Count == 0)
        {
            Debug.LogError("[MissionGrammar] Debe existir al menos una produccion para taskSymbol.", this);
            return false;
        }

        if (string.IsNullOrEmpty(terminalProduction))
        {
            Debug.LogError("[MissionGrammar] terminalProduction no puede estar vacia.", this);
            return false;
        }

        if (terminalProduction.Contains(taskSymbol))
        {
            Debug.LogError("[MissionGrammar] terminalProduction no debe contener el taskSymbol '" + taskSymbol + "'.", this);
            return false;
        }

        return true;
    }

    private void OnDrawGizmos()
    {
        if (Assignments == null || !showGizmo) return;

        foreach (var assignment in Assignments)
        {
            if (assignment.Room == null) continue;

            var bounds = assignment.Room.Bounds;
            Color color = MissionSymbolInfo.GetColor(assignment.SymbolType);
            Gizmos.color = color;

            // Cubo semi-transparente sobre toda la sala
            Vector3 center = new Vector3(
                bounds.x + bounds.width * 0.5f,
                bounds.y + bounds.height * 0.5f,
                0.1f // ligeramente delante del mapa BSP
            );
            Vector3 size = new Vector3(bounds.width, bounds.height, 0.1f);
            Gizmos.DrawCube(center, size);

            // Borde del cubo para mayor visibilidad
            Color wireColor = color;
            wireColor.a = 1f;
            Gizmos.color = wireColor;
            Gizmos.DrawWireCube(center, size);
        }

#if UNITY_EDITOR
        // Etiquetas de texto en cada sala asignada
        var style = new GUIStyle();
        style.fontSize = 14;
        style.fontStyle = FontStyle.Bold;
        style.alignment = TextAnchor.MiddleCenter;

        foreach (var assignment in Assignments)
        {
            if (assignment.Room == null) continue;

            var bounds = assignment.Room.Bounds;
            Vector3 labelPos = new Vector3(
                bounds.x + bounds.width * 0.5f,
                bounds.y + bounds.height * 0.5f,
                -0.1f
            );

            style.normal.textColor = Color.white;
            string label = MissionSymbolInfo.GetLabel(assignment.SymbolType);
            UnityEditor.Handles.Label(labelPos, label, style);
        }
#endif
    }
}
