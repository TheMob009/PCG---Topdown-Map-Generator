/// <summary>
/// Vincula una sala del BSP con un símbolo de la gramática de misiones.
/// Representa un nodo del recorrido: "en esta sala, el jugador hace esto".
/// </summary>
[System.Serializable]
public class MissionRoomAssignment
{
    //Sala del BSP asignada a esta acción
    public BspRoom Room;

    //Tipo de acción de misión asignada
    public MissionSymbolType SymbolType;

    //Caracter original de la cadena gramatical
    public char Symbol;

    public MissionRoomAssignment(BspRoom room, MissionSymbolType symbolType, char symbol)
    {
        Room = room;
        SymbolType = symbolType;
        Symbol = symbol;
    }
}
