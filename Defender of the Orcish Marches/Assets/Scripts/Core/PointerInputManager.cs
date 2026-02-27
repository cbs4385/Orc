using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.EnhancedTouch;
using UnityEngine.InputSystem.Utilities;
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;

/// <summary>
/// Singleton that provides a unified pointer API over mouse and touch.
/// Reads from the most recently active input source for seamless hybrid device support.
/// Auto-created like InputBindingManager.
/// </summary>
public class PointerInputManager : MonoBehaviour
{
    public static PointerInputManager Instance { get; private set; }

    private enum InputSource { Mouse, Touch }
    private InputSource lastInputSource = InputSource.Mouse;

    // Pinch tracking
    private float previousPinchDistance;
    private bool wasPinching;

    // Public API

    /// <summary>Current pointer position in screen coordinates.</summary>
    public Vector2 PointerPosition { get; private set; }

    /// <summary>True while pointer/finger is held down. False when 2+ fingers active (pinch).</summary>
    public bool IsPointerDown { get; private set; }

    /// <summary>True the frame the pointer/finger went down.</summary>
    public bool WasPointerPressedThisFrame { get; private set; }

    /// <summary>True the frame the pointer/finger was released.</summary>
    public bool WasPointerReleasedThisFrame { get; private set; }

    /// <summary>Pointer movement delta this frame.</summary>
    public Vector2 PointerDelta { get; private set; }

    /// <summary>Number of active touches (0 when using mouse).</summary>
    public int ActiveTouchCount { get; private set; }

    /// <summary>True when exactly 2 fingers are active.</summary>
    public bool IsPinching { get; private set; }

    /// <summary>Frame-to-frame change in distance between 2 pinch fingers.</summary>
    public float PinchDelta { get; private set; }

    /// <summary>True if the pointer is over a UI element.</summary>
    public bool IsPointerOverUI { get; private set; }

    /// <summary>True when the current frame's input came from touch.</summary>
    public bool IsTouchActive => lastInputSource == InputSource.Touch;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void AutoCreate()
    {
        if (Instance != null) return;
        var go = new GameObject("PointerInputManager");
        go.AddComponent<PointerInputManager>();
    }

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        Debug.Log("[PointerInputManager] Initialized.");
    }

    private void OnEnable()
    {
        EnhancedTouchSupport.Enable();
    }

    private void OnDisable()
    {
        EnhancedTouchSupport.Disable();
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    private void Update()
    {
        // Determine active input source
        var activeTouches = Touch.activeTouches;
        int touchCount = activeTouches.Count;
        bool hasTouchInput = touchCount > 0;
        bool hasMouseInput = Mouse.current != null && (
            Mouse.current.delta.ReadValue().sqrMagnitude > 0.01f ||
            Mouse.current.leftButton.isPressed ||
            Mouse.current.leftButton.wasPressedThisFrame ||
            Mouse.current.leftButton.wasReleasedThisFrame
        );

        if (hasTouchInput)
            lastInputSource = InputSource.Touch;
        else if (hasMouseInput)
            lastInputSource = InputSource.Mouse;

        ActiveTouchCount = hasTouchInput ? touchCount : 0;

        if (lastInputSource == InputSource.Touch && hasTouchInput)
        {
            UpdateFromTouch(activeTouches, touchCount);
        }
        else
        {
            UpdateFromMouse();
        }

        // UI overlap check
        UpdatePointerOverUI();
    }

    private void UpdateFromTouch(ReadOnlyArray<Touch> activeTouches, int touchCount)
    {
        // Primary touch (first finger)
        var primary = activeTouches[0];
        PointerPosition = primary.screenPosition;
        PointerDelta = primary.delta;

        // Suppress pointer down when pinching (2+ fingers)
        if (touchCount >= 2)
        {
            IsPointerDown = false;
            WasPointerPressedThisFrame = false;
            WasPointerReleasedThisFrame = false;
        }
        else
        {
            IsPointerDown = primary.phase == UnityEngine.InputSystem.TouchPhase.Moved
                         || primary.phase == UnityEngine.InputSystem.TouchPhase.Stationary
                         || primary.phase == UnityEngine.InputSystem.TouchPhase.Began;
            WasPointerPressedThisFrame = primary.phase == UnityEngine.InputSystem.TouchPhase.Began;
            WasPointerReleasedThisFrame = primary.phase == UnityEngine.InputSystem.TouchPhase.Ended
                                       || primary.phase == UnityEngine.InputSystem.TouchPhase.Canceled;
        }

        // Pinch detection
        if (touchCount >= 2)
        {
            IsPinching = true;
            float currentDist = Vector2.Distance(activeTouches[0].screenPosition, activeTouches[1].screenPosition);
            if (wasPinching)
            {
                PinchDelta = currentDist - previousPinchDistance;
            }
            else
            {
                PinchDelta = 0f;
            }
            previousPinchDistance = currentDist;
            wasPinching = true;
        }
        else
        {
            IsPinching = false;
            PinchDelta = 0f;
            wasPinching = false;
        }
    }

    private void UpdateFromMouse()
    {
        IsPinching = false;
        PinchDelta = 0f;
        wasPinching = false;

        if (Mouse.current == null)
        {
            PointerPosition = Vector2.zero;
            IsPointerDown = false;
            WasPointerPressedThisFrame = false;
            WasPointerReleasedThisFrame = false;
            PointerDelta = Vector2.zero;
            return;
        }

        PointerPosition = Mouse.current.position.ReadValue();
        IsPointerDown = Mouse.current.leftButton.isPressed;
        WasPointerPressedThisFrame = Mouse.current.leftButton.wasPressedThisFrame;
        WasPointerReleasedThisFrame = Mouse.current.leftButton.wasReleasedThisFrame;
        PointerDelta = Mouse.current.delta.ReadValue();
    }

    private void UpdatePointerOverUI()
    {
        if (EventSystem.current == null)
        {
            IsPointerOverUI = false;
            return;
        }

        if (lastInputSource == InputSource.Touch)
        {
            var activeTouches = Touch.activeTouches;
            if (activeTouches.Count > 0)
            {
                IsPointerOverUI = EventSystem.current.IsPointerOverGameObject(activeTouches[0].touchId);
            }
            else
            {
                IsPointerOverUI = false;
            }
        }
        else
        {
            IsPointerOverUI = EventSystem.current.IsPointerOverGameObject();
        }
    }
}
