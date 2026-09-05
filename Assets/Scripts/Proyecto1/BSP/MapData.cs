using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Tipos de celda del grid final. Los demás algoritmos del pipeline
/// (Perlin, Random Walk, L-System, Gramática) pueden leer/escribir sobre
/// este mismo grid para no duplicar la representación del mapa.
/// </summary>
public enum CellType
{
    Empty = 0,   // roca sólida / vacío, no transitable
    Floor = 1,   // piso de sala o pasillo, transitable
    Wall = 2,    // borde de una zona transitable
}

/// <summary>
/// Representa una sala generada por el BSP en coordenadas de grid (no de mundo).
/// Se expone tal cual para que otros algoritmos (L-System, Gramática de misiones)
/// puedan elegir salas, calcular su centro, o marcarlas con contenido.
/// </summary>
[System.Serializable]
public class BspRoom
{
    public RectInt Bounds;      // rectángulo de la sala en coordenadas de grid
    public int Id;              // índice único, útil para la Gramática de misiones

    public BspRoom(RectInt bounds, int id)
    {
        Bounds = bounds;
        Id = id;
    }

    public Vector2Int Center => new Vector2Int(
        Bounds.x + Bounds.width / 2,
        Bounds.y + Bounds.height / 2
    );
}

/// <summary>
/// Resultado completo que entrega el generador BSP: el grid con tipos de celda
/// y la lista de salas con su metadata. Esto es lo que consumen los siguientes
/// pasos del pipeline (Perlin Noise, Random Walk, L-System, Gramática).
/// </summary>
public class BspMapResult
{
    public CellType[,] Grid;
    public List<BspRoom> Rooms;
    public int Width;
    public int Height;

    public BspMapResult(int width, int height)
    {
        Width = width;
        Height = height;
        Grid = new CellType[width, height];
        Rooms = new List<BspRoom>();
    }

    public bool InBounds(int x, int y) => x >= 0 && y >= 0 && x < Width && y < Height;
}