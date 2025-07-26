using UnityEngine;

/// <summary>
/// Centralized input management system for the game.
/// Provides a single point of access for all input actions and state.
/// </summary>
public class InputManager : MonoBehaviour
{
    [Header("Input Settings")]
    [SerializeField] private bool enableInputDebugging = false;
    
    // Singleton instance
    public static InputManager Instance { get; private set; }
    
    // Input system
    private Controls controls;
    
    // Input state properties
    public Vector2 MoveInput { get; private set; }
    public bool JumpPressed { get; private set; }
    public bool JumpHeld { get; private set; }
    public bool DashPressed { get; private set; }
    public bool AttackPressed { get; private set; }
    
    // Input events - other systems can subscribe to these
    public System.Action OnJumpPressed;
    public System.Action OnDashPressed;
    public System.Action OnAttackPressed;
    public System.Action<Vector2> OnMoveInput;
    
    private void Awake()
    {
        // Singleton pattern
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            InitializeInput();
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    private void InitializeInput()
    {
        controls = new Controls();
        
        // Subscribe to input events
        controls.Gameplay.Move.performed += OnMovePerformed;
        controls.Gameplay.Move.canceled += OnMoveCanceled;
        
        controls.Gameplay.Jump.started += OnJumpStarted;
        controls.Gameplay.Jump.canceled += OnJumpCanceled;
        
        controls.Gameplay.Dash.started += OnDashStarted;
        controls.Gameplay.Attack.started += OnAttackStarted;
        
        if (enableInputDebugging)
        {
            // Debug.Log("InputManager: Input system initialized");
        }
    }
    
    private void OnEnable()
    {
        controls?.Enable();
    }
    
    private void OnDisable()
    {
        controls?.Disable();
    }
    
    private void OnDestroy()
    {
        if (controls != null)
        {
            // Unsubscribe from input events
            controls.Gameplay.Move.performed -= OnMovePerformed;
            controls.Gameplay.Move.canceled -= OnMoveCanceled;
            controls.Gameplay.Jump.started -= OnJumpStarted;
            controls.Gameplay.Jump.canceled -= OnJumpCanceled;
            controls.Gameplay.Dash.started -= OnDashStarted;
            controls.Gameplay.Attack.started -= OnAttackStarted;
            
            controls.Dispose();
        }
    }
    
    private void Update()
    {
        // Reset single-frame input flags
        JumpPressed = false;
        DashPressed = false;
        AttackPressed = false;
    }
    
    // Input event handlers
    private void OnMovePerformed(UnityEngine.InputSystem.InputAction.CallbackContext context)
    {
        MoveInput = context.ReadValue<Vector2>();
        OnMoveInput?.Invoke(MoveInput);
        
        if (enableInputDebugging)
        {
            // Debug.Log($"InputManager: Move input - {MoveInput}");
        }
    }
    
    private void OnMoveCanceled(UnityEngine.InputSystem.InputAction.CallbackContext context)
    {
        MoveInput = Vector2.zero;
        OnMoveInput?.Invoke(MoveInput);
        
        if (enableInputDebugging)
        {
            // Debug.Log("InputManager: Move input canceled");
        }
    }
    
    private void OnJumpStarted(UnityEngine.InputSystem.InputAction.CallbackContext context)
    {
        JumpPressed = true;
        JumpHeld = true;
        OnJumpPressed?.Invoke();
        
        if (enableInputDebugging)
        {
            // Debug.Log("InputManager: Jump pressed");
        }
    }
    
    private void OnJumpCanceled(UnityEngine.InputSystem.InputAction.CallbackContext context)
    {
        JumpHeld = false;
        
        if (enableInputDebugging)
        {
            // Debug.Log("InputManager: Jump released");
        }
    }
    
    private void OnDashStarted(UnityEngine.InputSystem.InputAction.CallbackContext context)
    {
        DashPressed = true;
        OnDashPressed?.Invoke();
        
        if (enableInputDebugging)
        {
            // Debug.Log("InputManager: Dash pressed");
        }
    }
    
    private void OnAttackStarted(UnityEngine.InputSystem.InputAction.CallbackContext context)
    {
        AttackPressed = true;
        OnAttackPressed?.Invoke();
        
        if (enableInputDebugging)
        {
            // Debug.Log("InputManager: Attack pressed");
        }
    }
    
    // Public utility methods
    public bool IsMoving()
    {
        return MoveInput.magnitude > 0.1f;
    }
    
    public bool IsMovingHorizontally()
    {
        return Mathf.Abs(MoveInput.x) > 0.1f;
    }
    
    public bool IsMovingVertically()
    {
        return Mathf.Abs(MoveInput.y) > 0.1f;
    }
    
    public float GetHorizontalInput()
    {
        return MoveInput.x;
    }
    
    public float GetVerticalInput()
    {
        return MoveInput.y;
    }
    
    public void EnableInput()
    {
        controls?.Enable();
    }
    
    public void DisableInput()
    {
        controls?.Disable();
    }
    
    // Debug methods
    public void SetInputDebugging(bool enabled)
    {
        enableInputDebugging = enabled;
    }
    
    #if UNITY_EDITOR
    private void OnGUI()
    {
        if (!enableInputDebugging) return;
        
        GUILayout.BeginArea(new Rect(Screen.width - 200, 10, 190, 150));
        GUILayout.Label("Input Debug:", GUI.skin.label);
        GUILayout.Label($"Move: {MoveInput}");
        GUILayout.Label($"Jump Held: {JumpHeld}");
        GUILayout.Label($"Moving: {IsMoving()}");
        GUILayout.Label($"Horizontal: {GetHorizontalInput():F2}");
        GUILayout.Label($"Vertical: {GetVerticalInput():F2}");
        GUILayout.EndArea();
    }
    #endif
}