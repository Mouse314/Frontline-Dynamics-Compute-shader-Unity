using UnityEngine;
using UnityEngine.InputSystem;

public class CameraController : MonoBehaviour
{
    [SerializeField] private Transform _target;
    [SerializeField] private float _minZoom = 5f;
    [SerializeField] private float _maxZoom = 50f;
    [SerializeField] private float _zoomSensitivity = 2f;
    [SerializeField] private float _dragSpeed = 1.2f;
    [SerializeField] private float _inertia = 0.86f;
    [SerializeField] private float _smoothTime = 0.12f;
    [SerializeField] private Vector3 _cameraOffset = new Vector3(0f, 18f, 0f);

    private Camera _camera;
    private Vector3 _velocity;
    private Vector2 _lastPointerPosition;
    private bool _isDragging;
    private float _targetZoom;
    private float _zoomVelocity;

    private void Awake()
    {
        _camera = GetComponent<Camera>();
        if (_camera == null)
        {
            _camera = Camera.main;
        }

        if (_camera == null)
        {
            enabled = false;
            return;
        }

        _camera.orthographic = true;
        transform.rotation = Quaternion.Euler(90f, -90f, 0f);
    }

    private void Start()
    {
        if (_target == null)
        {
            _target = GameObject.FindGameObjectWithTag("Map")?.transform;
        }

        if (_target == null)
        {
            _target = GameObject.Find("Plane")?.transform;
        }

        if (_target != null)
        {
            FitToMap();
        }
        else
        {
            _targetZoom = _camera.orthographicSize;
        }
    }

    private void Update()
    {
        if (_camera == null || Mouse.current == null)
        {
            return;
        }

        HandleZoom();
        HandleDragging();
        ClampToMapBounds();
    }

    private void HandleZoom()
    {
        Vector2 scroll = Mouse.current.scroll.ReadValue();
        float scrollValue = scroll.y;

        if (scrollValue != 0f)
        {
            _targetZoom = Mathf.Clamp(_targetZoom - scrollValue * _zoomSensitivity, _minZoom, _maxZoom);
        }

        _camera.orthographicSize = Mathf.SmoothDamp(_camera.orthographicSize, _targetZoom, ref _zoomVelocity, _smoothTime);
    }

    private void HandleDragging()
    {
        if (Mouse.current.middleButton.wasPressedThisFrame || Mouse.current.rightButton.wasPressedThisFrame)
        {
            _lastPointerPosition = Mouse.current.position.ReadValue();
            _isDragging = true;
            _velocity = Vector3.zero;
        }

        if ((Mouse.current.middleButton.isPressed || Mouse.current.rightButton.isPressed) && _isDragging)
        {
            Vector2 pointerPosition = Mouse.current.position.ReadValue();
            Vector2 delta = pointerPosition - _lastPointerPosition;
            _lastPointerPosition = pointerPosition;

            float zoomFactor = Mathf.Max(_camera.orthographicSize / 20f, 0.1f);
            Vector3 movement = (transform.right * -delta.x + transform.up * -delta.y) * (_dragSpeed * zoomFactor * 0.12f);
            movement.y = 0f;

            _velocity = movement * 8f;
            transform.position += movement;
        }
        else if (_isDragging && (Mouse.current.middleButton.wasReleasedThisFrame || Mouse.current.rightButton.wasReleasedThisFrame))
        {
            _isDragging = false;
        }

        if (!_isDragging)
        {
            transform.position += _velocity * Time.deltaTime * 60f;
            _velocity *= _inertia;

            if (_velocity.magnitude < 0.01f)
            {
                _velocity = Vector3.zero;
            }
        }
    }

    private void ClampToMapBounds()
    {
        if (_target == null)
        {
            return;
        }

        Bounds mapBounds = GetMapBounds();
        float halfWidth = _camera.orthographicSize * _camera.aspect;
        float halfHeight = _camera.orthographicSize;

        float minX = mapBounds.center.x - (mapBounds.extents.x - halfWidth);
        float maxX = mapBounds.center.x + (mapBounds.extents.x - halfWidth);
        float minZ = mapBounds.center.z - (mapBounds.extents.z - halfHeight);
        float maxZ = mapBounds.center.z + (mapBounds.extents.z - halfHeight);

        Vector3 position = transform.position;

        if (mapBounds.size.x > halfWidth * 2f)
        {
            position.x = Mathf.Clamp(position.x, minX, maxX);
        }
        else
        {
            position.x = mapBounds.center.x;
        }

        if (mapBounds.size.z > halfHeight * 2f)
        {
            position.z = Mathf.Clamp(position.z, minZ, maxZ);
        }
        else
        {
            position.z = mapBounds.center.z;
        }

        transform.position = new Vector3(position.x, transform.position.y, position.z);
    }

    public void FitToMap()
    {
        if (_target == null)
        {
            return;
        }

        Bounds mapBounds = GetMapBounds();
        float mapWidth = Mathf.Max(mapBounds.size.x, 0.1f);
        float mapHeight = Mathf.Max(mapBounds.size.z, 0.1f);

        float cameraHeight = Mathf.Max(mapHeight, mapWidth / Mathf.Max(_camera.aspect, 0.0001f));
        _targetZoom = Mathf.Clamp(cameraHeight * 0.5f, _minZoom, _maxZoom);
        Vector3 desiredPosition = mapBounds.center + _cameraOffset;
        desiredPosition.y = Mathf.Max(_cameraOffset.y, _targetZoom * 2f);

        transform.position = desiredPosition;
        transform.rotation = Quaternion.Euler(90f, -90f, 0f);
    }

    private Bounds GetMapBounds()
    {
        if (_target == null)
        {
            return new Bounds(Vector3.zero, Vector3.one);
        }

        Bounds bounds = new Bounds(_target.position, Vector3.zero);
        Renderer[] renderers = _target.GetComponentsInChildren<Renderer>();

        foreach (Renderer renderer in renderers)
        {
            bounds.Encapsulate(renderer.bounds);
        }

        if (bounds.size == Vector3.zero)
        {
            Vector3 scale = _target.localScale;
            bounds = new Bounds(_target.position, new Vector3(scale.x, 1f, scale.z));
        }

        return bounds;
    }
}
