using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Visualizador in-game de la gramatica de misiones.
/// Instancia sprites independientes del grid en el centro de las salas asignadas,
/// flotando sobre el terreno 2D en perspectiva topdown.
/// </summary>
public class MissionVisualizer : MonoBehaviour
{
    [Header("Fuente de Misiones")]
    [SerializeField] private MissionGrammarGenerator missionGenerator;

    [Header("Sprites de Objetivos (16x16 o personalizado)")]
    [Tooltip("Icono para el inicio de la mision (S).")]
    [SerializeField] private Sprite startSprite;
    [Tooltip("Icono para combate / enemigos (C).")]
    [SerializeField] private Sprite combatSprite;
    [Tooltip("Icono para recoleccion de minerales o recursos (R).")]
    [SerializeField] private Sprite collectSprite;
    [Tooltip("Icono para llaves o detonadores (K).")]
    [SerializeField] private Sprite keySprite;
    [Tooltip("Icono para puertas bloqueadas o derrumbes (L).")]
    [SerializeField] private Sprite lockSprite;
    [Tooltip("Icono para exploracion o puntos de interes (E).")]
    [SerializeField] private Sprite exploreSprite;
    [Tooltip("Icono para el objetivo final o salida de la cueva (G).")]
    [SerializeField] private Sprite goalSprite;

    [Header("Ajustes Visuales")]
    [Tooltip("Escala del icono en el mundo.")]
    [SerializeField] private Vector3 iconScale = Vector3.one;

    [Tooltip("Capa de ordenamiento para renderizar por encima del terreno.")]
    [SerializeField] private string sortingLayerName = "Default";

    [Tooltip("Orden dentro de la capa para asegurar que este sobre el piso y paredes.")]
    [SerializeField] private int orderInLayer = 10;

    [Tooltip("Aplica una animacion de flotacion vertical continua.")]
    [SerializeField] private bool enableFloatingAnimation = true;

    [Tooltip("Contenedor opcional para organizar la jerarquia. Si es nulo, usa este mismo Transform.")]
    [SerializeField] private Transform markersContainer;

    // Cache de sprites planos de color por si el usuario aun no asigna sprites en el inspector
    private static readonly Dictionary<Color, Sprite> _fallbackSprites = new Dictionary<Color, Sprite>();

    /// <summary>
    /// Limpia los marcadores actuales e instancia los nuevos segun las asignaciones de la mision.
    /// </summary>
    public void RenderMissionMarkers()
    {
        ClearMarkers();

        if (missionGenerator == null)
        {
            Debug.LogError("[MissionVisualizer] No hay un MissionGrammarGenerator asignado.", this);
            return;
        }

        var assignments = missionGenerator.Assignments;
        if (assignments == null || assignments.Count == 0)
        {
            Debug.LogWarning("[MissionVisualizer] No hay asignaciones de mision para visualizar.", this);
            return;
        }

        Transform container = markersContainer != null ? markersContainer : transform;

        foreach (var assignment in assignments)
        {
            if (assignment.Room == null) continue;

            // Centro de la celda de la sala (desplazado +0.5 para quedar en el medio de la baldosa)
            Vector2Int center = assignment.Room.Center;
            Vector3 worldPos = new Vector3(center.x + 0.5f, center.y + 0.5f, -0.1f);

            GameObject markerObj = new GameObject($"Marker_{assignment.Symbol}_Room{assignment.Room.Id}");
            markerObj.transform.SetParent(container);
            markerObj.transform.position = worldPos;
            markerObj.transform.localScale = iconScale;

            SpriteRenderer sr = markerObj.AddComponent<SpriteRenderer>();
            sr.sortingLayerName = sortingLayerName;
            sr.sortingOrder = orderInLayer;

            Sprite icon = GetSpriteForType(assignment.SymbolType);
            if (icon != null)
            {
                sr.sprite = icon;
            }
            else
            {
                // Si no hay sprite asignado en el Inspector, proveemos un fallback visible
                Color symbolColor = MissionSymbolInfo.GetColor(assignment.SymbolType);
                sr.sprite = GetOrCreateFallbackSprite(symbolColor);
                sr.color = symbolColor;
            }

            if (enableFloatingAnimation)
            {
                markerObj.AddComponent<FloatingMarker>();
            }
        }
    }

    /// <summary>
    /// Elimina todos los marcadores instanciados previamente.
    /// </summary>
    public void ClearMarkers()
    {
        Transform container = markersContainer != null ? markersContainer : transform;
        var children = new List<GameObject>();

        foreach (Transform child in container)
        {
            children.Add(child.gameObject);
        }

        for (int i = children.Count - 1; i >= 0; i--)
        {
            if (Application.isPlaying)
            {
                Destroy(children[i]);
            }
            else
            {
                DestroyImmediate(children[i]);
            }
        }
    }

    private Sprite GetSpriteForType(MissionSymbolType type)
    {
        switch (type)
        {
            case MissionSymbolType.Start:   return startSprite;
            case MissionSymbolType.Combat:  return combatSprite;
            case MissionSymbolType.Collect: return collectSprite;
            case MissionSymbolType.Key:     return keySprite;
            case MissionSymbolType.Lock:    return lockSprite;
            case MissionSymbolType.Explore: return exploreSprite;
            case MissionSymbolType.Goal:    return goalSprite;
            default:                        return null;
        }
    }

    private Sprite GetOrCreateFallbackSprite(Color color)
    {
        if (_fallbackSprites.TryGetValue(color, out Sprite existing) && existing != null)
        {
            return existing;
        }

        // Crea una textura basica de 16x16 con borde para fallback
        int size = 16;
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Point;

        Color[] pixels = new Color[size * size];
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                // Borde oscuro y centro brillante
                bool isBorder = (x == 0 || x == size - 1 || y == 0 || y == size - 1);
                pixels[y * size + x] = isBorder ? new Color(0.1f, 0.1f, 0.1f, 0.9f) : Color.white;
            }
        }

        tex.SetPixels(pixels);
        tex.Apply();

        Sprite fallback = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 16f);
        _fallbackSprites[color] = fallback;
        return fallback;
    }
}
