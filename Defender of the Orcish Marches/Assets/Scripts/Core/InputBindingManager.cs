using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

/// <summary>
/// All rebindable game actions. Mouse aim/scroll/left-click/right-click are NOT rebindable
/// (they are fundamental pointer actions), but keyboard and gamepad equivalents are.
/// </summary>
public enum GameAction
{
    Pause,
    OpenMenu,         // Escape — pause menu
    ToggleBuildMode,  // B
    RotateWallLeft,   // A
    RotateWallRight,  // D
    ExitBuildMode,    // Right-click / Escape (keyboard binding only)
    ToggleUpgrades,   // U
    Recall,           // R
    SwitchBallista,   // Tab
    Upgrade1, Upgrade2, Upgrade3, Upgrade4, Upgrade5,
    Upgrade6, Upgrade7, Upgrade8, Upgrade9
}

/// <summary>
/// Centralized input binding manager. Replaces all hardcoded Keyboard.current.xKey checks.
/// Supports keyboard + gamepad bindings, persistence via PlayerPrefs, and runtime rebinding.
/// </summary>
public class InputBindingManager : MonoBehaviour
{
    public static InputBindingManager Instance { get; private set; }

    /// <summary>Fired when any binding changes (for UI refresh).</summary>
    public event Action OnBindingsChanged;

    // --- Default keyboard bindings ---
    private static readonly Dictionary<GameAction, Key> DefaultKeyboard = new Dictionary<GameAction, Key>
    {
        { GameAction.Pause,            Key.Space },
        { GameAction.OpenMenu,         Key.Escape },
        { GameAction.ToggleBuildMode,  Key.B },
        { GameAction.RotateWallLeft,   Key.A },
        { GameAction.RotateWallRight,  Key.D },
        { GameAction.ExitBuildMode,    Key.Escape },
        { GameAction.ToggleUpgrades,   Key.U },
        { GameAction.Recall,           Key.R },
        { GameAction.SwitchBallista,   Key.Tab },
        { GameAction.Upgrade1,         Key.Digit1 },
        { GameAction.Upgrade2,         Key.Digit2 },
        { GameAction.Upgrade3,         Key.Digit3 },
        { GameAction.Upgrade4,         Key.Digit4 },
        { GameAction.Upgrade5,         Key.Digit5 },
        { GameAction.Upgrade6,         Key.Digit6 },
        { GameAction.Upgrade7,         Key.Digit7 },
        { GameAction.Upgrade8,         Key.Digit8 },
        { GameAction.Upgrade9,         Key.Digit9 },
    };

    // --- Default gamepad bindings ---
    private static readonly Dictionary<GameAction, GamepadBinding> DefaultGamepad = new Dictionary<GameAction, GamepadBinding>
    {
        { GameAction.Pause,            new GamepadBinding(GamepadButton.Start) },
        { GameAction.OpenMenu,         new GamepadBinding(GamepadButton.Start) },
        { GameAction.ToggleBuildMode,  new GamepadBinding(GamepadButton.West) },
        { GameAction.RotateWallLeft,   new GamepadBinding(GamepadButton.LeftShoulder) },
        { GameAction.RotateWallRight,  new GamepadBinding(GamepadButton.RightShoulder) },
        { GameAction.ExitBuildMode,    new GamepadBinding(GamepadButton.East) },
        { GameAction.ToggleUpgrades,   new GamepadBinding(GamepadButton.North) },
        { GameAction.Recall,           new GamepadBinding(GamepadButton.South) },
        { GameAction.SwitchBallista,   new GamepadBinding(GamepadButton.RightShoulder) },
        { GameAction.Upgrade1,         new GamepadBinding(GamepadButton.DpadUp) },
        { GameAction.Upgrade2,         new GamepadBinding(GamepadButton.DpadRight) },
        { GameAction.Upgrade3,         new GamepadBinding(GamepadButton.DpadDown) },
        { GameAction.Upgrade4,         new GamepadBinding(GamepadButton.DpadLeft) },
    };

    // Runtime bindings
    private Dictionary<GameAction, Key> keyboardBindings = new Dictionary<GameAction, Key>();
    private Dictionary<GameAction, GamepadBinding> gamepadBindings = new Dictionary<GameAction, GamepadBinding>();

    // --- Rebinding state ---
    private bool isListening;
    private GameAction listeningAction;
    private bool listeningForGamepad;
    private Action<bool> listeningCallback; // true = success

    public bool IsListeningForRebind => isListening;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void AutoCreate()
    {
        if (Instance != null) return;
        var go = new GameObject("InputBindingManager");
        go.AddComponent<InputBindingManager>();
    }

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        LoadBindings();
        Debug.Log($"[InputBindingManager] Initialized with {keyboardBindings.Count} keyboard and {gamepadBindings.Count} gamepad bindings.");
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    // ========== Query API ==========

    /// <summary>Returns true the frame the action's key/button was pressed.</summary>
    public bool WasPressedThisFrame(GameAction action)
    {
        if (isListening) return false; // Block input during rebind

        // Keyboard
        if (Keyboard.current != null && keyboardBindings.TryGetValue(action, out Key key))
        {
            if (Keyboard.current[key].wasPressedThisFrame)
                return true;
        }

        // Gamepad
        if (Gamepad.current != null && gamepadBindings.TryGetValue(action, out GamepadBinding gpb))
        {
            var ctrl = GetGamepadControl(gpb.button);
            if (ctrl != null && ctrl.wasPressedThisFrame)
                return true;
        }

        return false;
    }

    /// <summary>Returns true while the action's key/button is held.</summary>
    public bool IsPressed(GameAction action)
    {
        if (isListening) return false;

        if (Keyboard.current != null && keyboardBindings.TryGetValue(action, out Key key))
        {
            if (Keyboard.current[key].isPressed)
                return true;
        }

        if (Gamepad.current != null && gamepadBindings.TryGetValue(action, out GamepadBinding gpb))
        {
            var ctrl = GetGamepadControl(gpb.button);
            if (ctrl != null && ctrl.isPressed)
                return true;
        }

        return false;
    }

    // ========== Display Names ==========

    /// <summary>Get the display name for the keyboard binding of an action.</summary>
    public string GetKeyboardDisplayName(GameAction action)
    {
        if (keyboardBindings.TryGetValue(action, out Key key))
            return KeyToDisplayName(key);
        return "---";
    }

    /// <summary>Get the display name for the gamepad binding of an action.</summary>
    public string GetGamepadDisplayName(GameAction action)
    {
        if (gamepadBindings.TryGetValue(action, out GamepadBinding gpb))
            return GamepadButtonToDisplayName(gpb.button);
        return "---";
    }

    /// <summary>Get a context-aware display name (keyboard name if no gamepad connected, else both).</summary>
    public string GetDisplayName(GameAction action)
    {
        return GetKeyboardDisplayName(action);
    }

    // ========== Rebinding ==========

    /// <summary>Start listening for a new key/button press to rebind an action.</summary>
    public void StartRebind(GameAction action, bool forGamepad, Action<bool> callback)
    {
        isListening = true;
        listeningAction = action;
        listeningForGamepad = forGamepad;
        listeningCallback = callback;
        Debug.Log($"[InputBindingManager] Listening for rebind: {action} (gamepad={forGamepad})");
    }

    /// <summary>Cancel an in-progress rebind.</summary>
    public void CancelRebind()
    {
        if (!isListening) return;
        isListening = false;
        listeningCallback?.Invoke(false);
        listeningCallback = null;
        Debug.Log("[InputBindingManager] Rebind cancelled.");
    }

    private void Update()
    {
        if (!isListening) return;

        // Cancel on Escape (always)
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            CancelRebind();
            return;
        }

        if (listeningForGamepad)
        {
            // Listen for gamepad button
            if (Gamepad.current == null) return;
            foreach (GamepadButton btn in Enum.GetValues(typeof(GamepadButton)))
            {
                var ctrl = GetGamepadControl(btn);
                if (ctrl != null && ctrl.wasPressedThisFrame)
                {
                    gamepadBindings[listeningAction] = new GamepadBinding(btn);
                    SaveBindings();
                    isListening = false;
                    OnBindingsChanged?.Invoke();
                    listeningCallback?.Invoke(true);
                    listeningCallback = null;
                    Debug.Log($"[InputBindingManager] Rebound {listeningAction} gamepad to {btn}");
                    return;
                }
            }
        }
        else
        {
            // Listen for keyboard key
            if (Keyboard.current == null) return;
            foreach (Key k in Enum.GetValues(typeof(Key)))
            {
                if (k == Key.None || k == Key.IMESelected) continue;
                KeyControl ctrl;
                try { ctrl = Keyboard.current[k]; }
                catch { continue; }
                if (ctrl == null) continue;

                if (ctrl.wasPressedThisFrame)
                {
                    keyboardBindings[listeningAction] = k;
                    SaveBindings();
                    isListening = false;
                    OnBindingsChanged?.Invoke();
                    listeningCallback?.Invoke(true);
                    listeningCallback = null;
                    Debug.Log($"[InputBindingManager] Rebound {listeningAction} keyboard to {k}");
                    return;
                }
            }
        }
    }

    // ========== Reset ==========

    public void ResetToDefaults()
    {
        keyboardBindings = new Dictionary<GameAction, Key>(DefaultKeyboard);
        gamepadBindings = new Dictionary<GameAction, GamepadBinding>(DefaultGamepad);
        SaveBindings();
        OnBindingsChanged?.Invoke();
        Debug.Log("[InputBindingManager] All bindings reset to defaults.");
    }

    public Key GetDefaultKey(GameAction action)
    {
        return DefaultKeyboard.TryGetValue(action, out Key k) ? k : Key.None;
    }

    public GamepadButton? GetDefaultGamepadButton(GameAction action)
    {
        return DefaultGamepad.TryGetValue(action, out GamepadBinding gpb) ? gpb.button : (GamepadButton?)null;
    }

    // ========== Persistence ==========

    private const string PREFS_PREFIX = "InputBinding_";

    private void SaveBindings()
    {
        foreach (var kvp in keyboardBindings)
            PlayerPrefs.SetString(PREFS_PREFIX + "KB_" + kvp.Key, kvp.Value.ToString());

        foreach (var kvp in gamepadBindings)
            PlayerPrefs.SetString(PREFS_PREFIX + "GP_" + kvp.Key, kvp.Value.button.ToString());

        PlayerPrefs.Save();
    }

    private void LoadBindings()
    {
        // Start with defaults
        keyboardBindings = new Dictionary<GameAction, Key>(DefaultKeyboard);
        gamepadBindings = new Dictionary<GameAction, GamepadBinding>(DefaultGamepad);

        // Override with saved values
        foreach (GameAction action in Enum.GetValues(typeof(GameAction)))
        {
            string kbKey = PREFS_PREFIX + "KB_" + action;
            if (PlayerPrefs.HasKey(kbKey))
            {
                string val = PlayerPrefs.GetString(kbKey);
                if (Enum.TryParse<Key>(val, out Key parsed))
                    keyboardBindings[action] = parsed;
            }

            string gpKey = PREFS_PREFIX + "GP_" + action;
            if (PlayerPrefs.HasKey(gpKey))
            {
                string val = PlayerPrefs.GetString(gpKey);
                if (Enum.TryParse<GamepadButton>(val, out GamepadButton parsed))
                    gamepadBindings[action] = new GamepadBinding(parsed);
            }
        }
    }

    // ========== Helpers ==========

    private ButtonControl GetGamepadControl(GamepadButton button)
    {
        if (Gamepad.current == null) return null;
        switch (button)
        {
            case GamepadButton.South: return Gamepad.current.buttonSouth;
            case GamepadButton.North: return Gamepad.current.buttonNorth;
            case GamepadButton.East: return Gamepad.current.buttonEast;
            case GamepadButton.West: return Gamepad.current.buttonWest;
            case GamepadButton.Start: return Gamepad.current.startButton;
            case GamepadButton.Select: return Gamepad.current.selectButton;
            case GamepadButton.LeftShoulder: return Gamepad.current.leftShoulder;
            case GamepadButton.RightShoulder: return Gamepad.current.rightShoulder;
            case GamepadButton.LeftTrigger: return Gamepad.current.leftTrigger;
            case GamepadButton.RightTrigger: return Gamepad.current.rightTrigger;
            case GamepadButton.LeftStick: return Gamepad.current.leftStickButton;
            case GamepadButton.RightStick: return Gamepad.current.rightStickButton;
            case GamepadButton.DpadUp: return Gamepad.current.dpad.up;
            case GamepadButton.DpadDown: return Gamepad.current.dpad.down;
            case GamepadButton.DpadLeft: return Gamepad.current.dpad.left;
            case GamepadButton.DpadRight: return Gamepad.current.dpad.right;
            default: return null;
        }
    }

    public static string KeyToDisplayName(Key key)
    {
        switch (key)
        {
            case Key.Space: return "Space";
            case Key.Escape: return "Esc";
            case Key.Tab: return "Tab";
            case Key.LeftShift: return "L Shift";
            case Key.RightShift: return "R Shift";
            case Key.LeftCtrl: return "L Ctrl";
            case Key.RightCtrl: return "R Ctrl";
            case Key.LeftAlt: return "L Alt";
            case Key.RightAlt: return "R Alt";
            case Key.Enter: return "Enter";
            case Key.Backspace: return "Backspace";
            case Key.Delete: return "Delete";
            case Key.Digit0: return "0";
            case Key.Digit1: return "1";
            case Key.Digit2: return "2";
            case Key.Digit3: return "3";
            case Key.Digit4: return "4";
            case Key.Digit5: return "5";
            case Key.Digit6: return "6";
            case Key.Digit7: return "7";
            case Key.Digit8: return "8";
            case Key.Digit9: return "9";
            default: return key.ToString();
        }
    }

    public static string GamepadButtonToDisplayName(GamepadButton button)
    {
        switch (button)
        {
            case GamepadButton.South: return "A / Cross";
            case GamepadButton.North: return "Y / Triangle";
            case GamepadButton.East: return "B / Circle";
            case GamepadButton.West: return "X / Square";
            case GamepadButton.Start: return "Start";
            case GamepadButton.Select: return "Select";
            case GamepadButton.LeftShoulder: return "LB";
            case GamepadButton.RightShoulder: return "RB";
            case GamepadButton.LeftTrigger: return "LT";
            case GamepadButton.RightTrigger: return "RT";
            case GamepadButton.LeftStick: return "L Stick";
            case GamepadButton.RightStick: return "R Stick";
            case GamepadButton.DpadUp: return "D-Up";
            case GamepadButton.DpadDown: return "D-Down";
            case GamepadButton.DpadLeft: return "D-Left";
            case GamepadButton.DpadRight: return "D-Right";
            default: return button.ToString();
        }
    }

    /// <summary>Get the list of actions that should be shown in the rebind UI (excludes upgrades 5-9 for gamepad).</summary>
    public static GameAction[] GetRebindableActions()
    {
        return new GameAction[]
        {
            GameAction.Pause,
            GameAction.OpenMenu,
            GameAction.ToggleBuildMode,
            GameAction.RotateWallLeft,
            GameAction.RotateWallRight,
            GameAction.ExitBuildMode,
            GameAction.ToggleUpgrades,
            GameAction.Recall,
            GameAction.SwitchBallista,
            GameAction.Upgrade1,
            GameAction.Upgrade2,
            GameAction.Upgrade3,
            GameAction.Upgrade4,
            GameAction.Upgrade5,
            GameAction.Upgrade6,
            GameAction.Upgrade7,
            GameAction.Upgrade8,
            GameAction.Upgrade9,
        };
    }

    /// <summary>Human-readable name for each action.</summary>
    public static string GetActionDisplayName(GameAction action)
    {
        switch (action)
        {
            case GameAction.Pause: return "Pause";
            case GameAction.OpenMenu: return "Pause Menu";
            case GameAction.ToggleBuildMode: return "Toggle Build Mode";
            case GameAction.RotateWallLeft: return "Rotate Wall Left";
            case GameAction.RotateWallRight: return "Rotate Wall Right";
            case GameAction.ExitBuildMode: return "Exit Build Mode";
            case GameAction.ToggleUpgrades: return "Toggle Upgrades";
            case GameAction.Recall: return "Recall Units";
            case GameAction.SwitchBallista: return "Switch Ballista";
            case GameAction.Upgrade1: return "Upgrade Slot 1";
            case GameAction.Upgrade2: return "Upgrade Slot 2";
            case GameAction.Upgrade3: return "Upgrade Slot 3";
            case GameAction.Upgrade4: return "Upgrade Slot 4";
            case GameAction.Upgrade5: return "Upgrade Slot 5";
            case GameAction.Upgrade6: return "Upgrade Slot 6";
            case GameAction.Upgrade7: return "Upgrade Slot 7";
            case GameAction.Upgrade8: return "Upgrade Slot 8";
            case GameAction.Upgrade9: return "Upgrade Slot 9";
            default: return action.ToString();
        }
    }
}

/// <summary>Gamepad button enum wrapper for storage.</summary>
[Serializable]
public struct GamepadBinding
{
    public GamepadButton button;
    public GamepadBinding(GamepadButton b) { button = b; }
}

/// <summary>
/// Gamepad button identifiers matching Unity Input System layout.
/// </summary>
public enum GamepadButton
{
    South, North, East, West,
    Start, Select,
    LeftShoulder, RightShoulder,
    LeftTrigger, RightTrigger,
    LeftStick, RightStick,
    DpadUp, DpadDown, DpadLeft, DpadRight
}
