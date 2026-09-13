using UnityEngine;

/// <summary>
/// Controlador de cámara ortográfica 2D para Proyecto 1.
/// - Rueda del ratón: zoom in/out (modifica orthographicSize).
/// - Click izquierdo + arrastrar: panning (mueve la cámara).
///
/// Adjuntar este componente directamente a la Main Camera.
/// </summary>
public class CameraController : MonoBehaviour
{
    [Header("Zoom")]
    [Tooltip("Velocidad de zoom al girar la rueda del ratón.")]
    [SerializeField] private float zoomSpeed = 2f;

    [Tooltip("Tamaño ortográfico mínimo (máximo zoom in).")]
    [SerializeField] private float minZoom = 2f;

    [Tooltip("Tamaño ortográfico máximo (máximo zoom out).")]
    [SerializeField] private float maxZoom = 30f;

    [Tooltip("Suavidad del zoom (0 = instantáneo, valores mayores = más suave).")]
    [SerializeField] private float zoomSmoothTime = 0.1f;

    [Header("Panning")]
    [Tooltip("Botón del ratón para hacer panning (0 = izquierdo, 1 = derecho, 2 = central).")]
    [SerializeField] private int panMouseButton = 0;

    [Tooltip("Suavidad del panning (0 = instantáneo, valores mayores = más suave).")]
    [SerializeField] private float panSmoothTime = 0.05f;

    [Header("Límites de la cámara (opcional)")]
    [Tooltip("Si está activo, la cámara no podrá salir de estos límites.")]
    [SerializeField] private bool useBounds = false;

    [SerializeField] private Vector2 boundsMin = new Vector2(-50f, -50f);
    [SerializeField] private Vector2 boundsMax = new Vector2(50f, 50f);

    // ── Estado interno ───────────────────────────────────────────────────────
    private Camera _cam;
    private float _targetZoom;
    private float _zoomVelocity;

    private Vector3 _dragOriginWorld;
    private bool _isDragging;

    private Vector3 _targetPosition;
    private Vector3 _panVelocity;

    // ────────────────────────────────────────────────────────────────────────

    private void Awake()
    {
        _cam = GetComponent<Camera>();

        if (_cam == null)
        {
            Debug.LogError("[CameraController] No se encontró un componente Camera en este GameObject. " +
                           "Adjunta este script directamente a la cámara.");
            enabled = false;
            return;
        }

        if (!_cam.orthographic)
        {
            Debug.LogWarning("[CameraController] La cámara no es ortográfica. " +
                             "El zoom podría no comportarse como se espera.");
        }

        _targetZoom = _cam.orthographicSize;
        _targetPosition = transform.position;
    }

    private void Update()
    {
        HandleZoom();
        HandlePan();
        ApplyBounds();
    }

    // ── Zoom ─────────────────────────────────────────────────────────────────

    private void HandleZoom()
    {
        float scroll = Input.mouseScrollDelta.y;   // +arriba / -abajo
        if (Mathf.Abs(scroll) > 0.001f)
        {
            // Rueda arriba  (scroll > 0) → acercar (orthographicSize menor)
            // Rueda abajo   (scroll < 0) → alejar  (orthographicSize mayor)
            _targetZoom -= scroll * zoomSpeed;
            _targetZoom = Mathf.Clamp(_targetZoom, minZoom, maxZoom);
        }

        _cam.orthographicSize = Mathf.SmoothDamp(
            _cam.orthographicSize,
            _targetZoom,
            ref _zoomVelocity,
            zoomSmoothTime);
    }

    // ── Panning ───────────────────────────────────────────────────────────────

    private void HandlePan()
    {
        if (Input.GetMouseButtonDown(panMouseButton))
        {
            _dragOriginWorld = GetMouseWorldPosition();
            _isDragging = true;
        }

        if (_isDragging && Input.GetMouseButton(panMouseButton))
        {
            Vector3 currentMouseWorld = GetMouseWorldPosition();
            Vector3 delta = _dragOriginWorld - currentMouseWorld;

            _targetPosition = transform.position + delta;

            transform.position = Vector3.SmoothDamp(
                transform.position,
                _targetPosition,
                ref _panVelocity,
                panSmoothTime);

            // Recalcular el origen para el siguiente frame
            _dragOriginWorld = GetMouseWorldPosition();
        }

        if (Input.GetMouseButtonUp(panMouseButton))
        {
            _isDragging = false;
        }
    }

    // ── Utilidades ───────────────────────────────────────────────────────────

    /// <summary>
    /// Convierte la posición del cursor en pantalla a coordenadas del mundo,
    /// usando el plano Z = 0 (plano del tilemap).
    /// </summary>
    private Vector3 GetMouseWorldPosition()
    {
        Vector3 screenPos = Input.mousePosition;
        screenPos.z = -transform.position.z;
        return _cam.ScreenToWorldPoint(screenPos);
    }

    /// <summary>Restringe la posición de la cámara dentro de los límites configurados.</summary>
    private void ApplyBounds()
    {
        if (!useBounds) return;

        Vector3 pos = transform.position;
        pos.x = Mathf.Clamp(pos.x, boundsMin.x, boundsMax.x);
        pos.y = Mathf.Clamp(pos.y, boundsMin.y, boundsMax.y);
        transform.position = pos;
    }

    // ── API pública ──────────────────────────────────────────────────────────

    /// <summary>Centra la cámara en un punto del mundo (sin animación).</summary>
    public void CenterOn(Vector3 worldPosition)
    {
        worldPosition.z = transform.position.z;
        transform.position = worldPosition;
        _targetPosition = worldPosition;
    }

    /// <summary>
    /// Ajusta el zoom para encuadrar un área dada (en unidades del mundo).
    /// Útil para llamar desde PipelineManager tras generar el mapa.
    /// </summary>
    public void FitToArea(float worldWidth, float worldHeight)
    {
        float aspectRatio = (float)Screen.width / Screen.height;
        float zoomByWidth = worldWidth * 0.5f / aspectRatio;
        float zoomByHeight = worldHeight * 0.5f;
        _targetZoom = Mathf.Clamp(Mathf.Max(zoomByWidth, zoomByHeight), minZoom, maxZoom);
    }
}
