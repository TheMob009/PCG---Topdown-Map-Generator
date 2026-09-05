using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>
/// Funciones compartidas entre los distintos generadores del pipeline
/// (BSP, Random Walk, y a futuro L-System). Evita duplicar lógica de
/// "repintar paredes" o "volcar el grid a un Tilemap" en cada script.
/// </summary>
public static class MapUtils
{
    /// <summary>
    /// Recalcula las paredes de todo el grid: cualquier celda vacía que
    /// tenga al menos un vecino de tipo Floor pasa a ser Wall. Se puede
    /// llamar de nuevo después de que otro algoritmo (Random Walk, L-System)
    /// agregue más piso, para que las paredes se ajusten a la nueva forma.
    /// </summary>
    public static void PaintWalls(BspMapResult result)
    {
        var wallPositions = new List<Vector2Int>();

        for (int x = 0; x < result.Width; x++)
        {
            for (int y = 0; y < result.Height; y++)
            {
                if (result.Grid[x, y] != CellType.Floor) continue;

                for (int dx = -1; dx <= 1; dx++)
                {
                    for (int dy = -1; dy <= 1; dy++)
                    {
                        if (dx == 0 && dy == 0) continue;
                        int nx = x + dx;
                        int ny = y + dy;

                        if (!result.InBounds(nx, ny)) continue;
                        if (result.Grid[nx, ny] == CellType.Empty)
                            wallPositions.Add(new Vector2Int(nx, ny));
                    }
                }
            }
        }

        foreach (var pos in wallPositions)
        {
            result.Grid[pos.x, pos.y] = CellType.Wall;
        }
    }

    /// <summary>
    /// Vuelca el grid completo a los Tilemaps de piso/pared. Se puede llamar
    /// tras cada etapa del pipeline para refrescar la visualización.
    /// </summary>
    public static void DrawTilemap(BspMapResult result, Tilemap floorTilemap, Tilemap wallTilemap, TileBase floorTile, TileBase wallTile)
    {
        if (floorTilemap != null) floorTilemap.ClearAllTiles();
        if (wallTilemap != null) wallTilemap.ClearAllTiles();

        for (int x = 0; x < result.Width; x++)
        {
            for (int y = 0; y < result.Height; y++)
            {
                var cell = result.Grid[x, y];
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
}