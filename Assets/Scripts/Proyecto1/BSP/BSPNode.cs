using System;
using UnityEngine;

/// <summary>
/// Nodo de un árbol Binary Space Partitioning. Cada nodo representa una región
/// rectangular del grid. Si el nodo se subdivide, guarda referencia a sus dos
/// hijos (Left/Right); si es una hoja, eventualmente contiene una sala (Room).
/// </summary>
public class BspNode
{
    public RectInt Area;         // región completa de este nodo (partición)
    public BspNode Left;
    public BspNode Right;
    public BspRoom Room;         // solo asignado si este nodo es hoja

    public bool IsLeaf => Left == null && Right == null;

    public BspNode(RectInt area)
    {
        Area = area;
    }

    /// <summary>
    /// Intenta dividir este nodo en dos, horizontal o verticalmente, siempre que
    /// el resultado respete minSize. Retorna false si el nodo ya es demasiado
    /// pequeño para seguir dividiéndose (se vuelve hoja).
    /// </summary>
    public bool Split(int minSize, System.Random rng)
    {
        if (!IsLeaf) return false; // ya dividido

        bool splitHorizontal = rng.NextDouble() > 0.5;

        // Si el área es mucho más ancha que alta (o viceversa), forzamos el
        // corte en la dirección que más lo necesita, para evitar salas muy alargadas.
        float ratio = (float)Area.width / Area.height;
        if (ratio > 1.25f) splitHorizontal = false;      // muy ancho -> corte vertical
        else if (ratio < 0.8f) splitHorizontal = true;    // muy alto  -> corte horizontal

        int max = (splitHorizontal ? Area.height : Area.width) - minSize;
        if (max <= minSize) return false; // no cabe una división válida

        int split = rng.Next(minSize, max);

        if (splitHorizontal)
        {
            Left = new BspNode(new RectInt(Area.x, Area.y, Area.width, split));
            Right = new BspNode(new RectInt(Area.x, Area.y + split, Area.width, Area.height - split));
        }
        else
        {
            Left = new BspNode(new RectInt(Area.x, Area.y, split, Area.height));
            Right = new BspNode(new RectInt(Area.x + split, Area.y, Area.width - split, Area.height));
        }

        return true;
    }

    /// <summary>
    /// Devuelve una sala representativa dentro de este subárbol: la propia si es
    /// hoja, o la de uno de sus hijos (recursivamente) si es un nodo interno.
    /// Se usa al conectar pasillos entre hermanos del árbol.
    /// </summary>
    public BspRoom GetAnyRoom(System.Random rng)
    {
        if (IsLeaf) return Room;

        BspNode first = rng.NextDouble() > 0.5 ? Left : Right;
        BspNode second = first == Left ? Right : Left;

        BspRoom room = first?.GetAnyRoom(rng);
        if (room != null) return room;

        return second?.GetAnyRoom(rng);
    }
}