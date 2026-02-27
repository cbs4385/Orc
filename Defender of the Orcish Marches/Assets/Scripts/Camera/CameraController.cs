using UnityEngine;
using UnityEngine.InputSystem;

public class CameraController : MonoBehaviour
{
    [Header("Zoom Settings")]
    [SerializeField] private float minZoom = 8f;
    [SerializeField] private float maxZoom = 23.5f;
    [SerializeField] private float zoomSpeed = 9f;
    [SerializeField] private float zoomSmoothing = 8f;
    [SerializeField] private float pinchZoomSpeed = 0.01f;

    private UnityEngine.Camera cam;
    private float targetZoom;

    private void Awake()
    {
        cam = GetComponent<UnityEngine.Camera>();
        if (cam == null) cam = UnityEngine.Camera.main;
    }

    private void Start()
    {
        targetZoom = cam.orthographicSize;
    }

    private void Update()
    {
        // Mouse scroll zoom
        if (Mouse.current != null)
        {
            float scroll = Mouse.current.scroll.ReadValue().y;
            if (Mathf.Abs(scroll) > 0.01f)
            {
                // Logarithmic zoom: multiply by a ratio so each scroll tick
                // feels proportional regardless of current zoom level
                float zoomFactor = Mathf.Exp(-scroll * zoomSpeed * 0.005f);
                targetZoom *= zoomFactor;
                targetZoom = Mathf.Clamp(targetZoom, minZoom, maxZoom);
            }
        }

        // Pinch-to-zoom (touch devices)
        var pointer = PointerInputManager.Instance;
        if (pointer != null && pointer.IsPinching)
        {
            float pinchFactor = Mathf.Exp(-pointer.PinchDelta * pinchZoomSpeed);
            targetZoom *= pinchFactor;
            targetZoom = Mathf.Clamp(targetZoom, minZoom, maxZoom);
        }

        cam.orthographicSize = Mathf.Lerp(cam.orthographicSize, targetZoom, Time.deltaTime * zoomSmoothing);
    }
}
