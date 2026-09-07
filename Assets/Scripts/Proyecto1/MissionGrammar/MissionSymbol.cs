using UnityEngine;

/// <summary>
/// Tipos de acción que puede representar un símbolo de la gramática de misiones.
/// Cada tipo tiene asociado un color, una etiqueta corta y descripciones
/// contextuales (Mina / Colonia) que se usan para la visualización e
/// interpretación textual de la misión generada.
/// </summary>
public enum MissionSymbolType
{
    Start,    // S — inicio de la misión
    Explore,  // E — exploración
    Collect,  // R — recolección de recursos
    Combat,   // C — combate
    Key,      // K — obtener llave
    Lock,     // L — abrir cerradura / puerta
    Goal,     // G — objetivo final
}

/// <summary>
/// Contexto narrativo en el que se interpreta la misión.
/// Cambia las descripciones textuales, no la mecánica.
/// </summary>
public enum MissionContext
{
    Mina,
    Colonia,
}

/// <summary>
/// Metadata estática de cada símbolo: color para Gizmos, etiqueta corta
/// y descripciones por contexto narrativo.
/// </summary>
public static class MissionSymbolInfo
{
    // -----------------------------------------------------------------
    // Conversión caracter ↔ enum
    // -----------------------------------------------------------------

    /// <summary>
    /// Convierte un caracter de la cadena gramatical al tipo de símbolo
    /// correspondiente. Devuelve null si el caracter no es un símbolo
    /// terminal reconocido.
    /// </summary>
    public static MissionSymbolType? FromChar(char c)
    {
        switch (c)
        {
            case 'S': return MissionSymbolType.Start;
            case 'E': return MissionSymbolType.Explore;
            case 'R': return MissionSymbolType.Collect;
            case 'C': return MissionSymbolType.Combat;
            case 'K': return MissionSymbolType.Key;
            case 'L': return MissionSymbolType.Lock;
            case 'G': return MissionSymbolType.Goal;
            default:  return null;
        }
    }

    /// <summary>
    /// Caracter representativo del símbolo (para debug y consola).
    /// </summary>
    public static char ToChar(MissionSymbolType type)
    {
        switch (type)
        {
            case MissionSymbolType.Start:   return 'S';
            case MissionSymbolType.Explore: return 'E';
            case MissionSymbolType.Collect: return 'R';
            case MissionSymbolType.Combat:  return 'C';
            case MissionSymbolType.Key:     return 'K';
            case MissionSymbolType.Lock:    return 'L';
            case MissionSymbolType.Goal:    return 'G';
            default: return '?';
        }
    }

    // -----------------------------------------------------------------
    // Etiqueta corta para Gizmos
    // -----------------------------------------------------------------

    public static string GetLabel(MissionSymbolType type)
    {
        switch (type)
        {
            case MissionSymbolType.Start:   return "[S]";
            case MissionSymbolType.Explore: return "[E]";
            case MissionSymbolType.Collect: return "[R]";
            case MissionSymbolType.Combat:  return "[C]";
            case MissionSymbolType.Key:     return "[K]";
            case MissionSymbolType.Lock:    return "[L]";
            case MissionSymbolType.Goal:    return "[G]";
            default: return "[?]";
        }
    }

    // -----------------------------------------------------------------
    // Colores para Gizmos
    // -----------------------------------------------------------------

    public static Color GetColor(MissionSymbolType type)
    {
        switch (type)
        {
            case MissionSymbolType.Start:   return new Color(0.2f, 0.8f, 0.2f, 0.7f);  // verde
            case MissionSymbolType.Explore: return new Color(0.3f, 0.6f, 1.0f, 0.7f);  // azul claro
            case MissionSymbolType.Collect: return new Color(1.0f, 0.85f, 0.0f, 0.7f); // amarillo
            case MissionSymbolType.Combat:  return new Color(1.0f, 0.2f, 0.2f, 0.7f);  // rojo
            case MissionSymbolType.Key:     return new Color(1.0f, 0.7f, 0.0f, 0.7f);  // dorado
            case MissionSymbolType.Lock:    return new Color(0.6f, 0.6f, 0.6f, 0.7f);  // gris
            case MissionSymbolType.Goal:    return new Color(0.8f, 0.2f, 1.0f, 0.7f);  // violeta
            default: return Color.white;
        }
    }

    // -----------------------------------------------------------------
    // Descripciones contextuales
    // -----------------------------------------------------------------

    public static string GetDescription(MissionSymbolType type, MissionContext context)
    {
        if (context == MissionContext.Mina)
        {
            switch (type)
            {
                case MissionSymbolType.Start:   return "Comienza la mision en la galeria principal";
                case MissionSymbolType.Explore: return "Explora la galeria";
                case MissionSymbolType.Collect: return "Recolecta mineral";
                case MissionSymbolType.Combat:  return "Derrota a los enemigos";
                case MissionSymbolType.Key:     return "Encuentra la llave";
                case MissionSymbolType.Lock:    return "Abre la compuerta";
                case MissionSymbolType.Goal:    return "Alcanza el elevador";
                default: return "???";
            }
        }
        else // Colonia
        {
            switch (type)
            {
                case MissionSymbolType.Start:   return "Comienza la mision en el modulo central";
                case MissionSymbolType.Explore: return "Inspecciona el modulo";
                case MissionSymbolType.Collect: return "Recupera recursos";
                case MissionSymbolType.Combat:  return "Elimina la amenaza";
                case MissionSymbolType.Key:     return "Consigue la tarjeta";
                case MissionSymbolType.Lock:    return "Desbloquea la sala";
                case MissionSymbolType.Goal:    return "Reactiva el sistema";
                default: return "???";
            }
        }
    }
}
