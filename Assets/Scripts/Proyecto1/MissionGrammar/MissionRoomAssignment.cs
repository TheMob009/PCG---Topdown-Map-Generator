/// <summary>
/// Vincula una sala del BSP con un símbolo de la gramática de misiones.
/// Representa un nodo del recorrido: "en esta sala, el jugador hace esto".
/// </summary>
[System.Serializable]
public class MissionRoomAssignment
{
    /// <summary>Sala del BSP asignada a esta acción.</summary>
    public BspRoom Room;

    /// <summary>Tipo de acción de misión asignada.</summary>
    public MissionSymbolType SymbolType;

    /// <summary>Caracter original de la cadena gramatical.</summary>
    public char Symbol;

    public MissionRoomAssignment(BspRoom room, MissionSymbolType symbolType, char symbol)
    {
        Room = room;
        SymbolType = symbolType;
        Symbol = symbol;
    }
}
