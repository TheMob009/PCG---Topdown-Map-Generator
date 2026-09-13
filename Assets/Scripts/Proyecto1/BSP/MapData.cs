using System.Collections.Generic;
using UnityEngine;


public enum CellType
{
    Empty = 0,// roca sólida o espacio vacio
    Floor = 1,// piso de sala o pasillo
    Wall = 2, // borde de una zona 
}

[System.Serializable]
public class BspRoom
{
    public RectInt Bounds;// rectángulo de la sala en coordenadas de grid
    public int Id;

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


// Resultado completo que entrega el generador BSP.
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