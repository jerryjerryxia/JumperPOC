using UnityEngine;
using System.Collections;

/// <summary>
/// Handles all input event routing from InputManager to PlayerController.
/// Separates input handling from game logic for cleaner architecture.
/// EXTRACTED FROM PlayerController lines 415-849
/// </summary>
[RequireComponent(typeof(PlayerController))]
public class PlayerInputHandler : MonoBehaviour
{
    // Events that PlayerController subscribes to
    public System.Action<Vector2> OnMove;
    public System.Action OnJumpPressed;
    public System.Action OnJumpReleased;
    public System.Action OnDashPressed;
    public System.Action OnAttackPressed;

    // Component references
    private InputManager inputManager;

    /// <summary>
    /// Subscribe to InputManager events on enable
    /// </summary>
    void OnEnable()
    {
        // Try to get InputManager instance, with retry for timing issues
        if (InputManager.Instance != null)
        {
            SetupInputManager();
        }
        else
        {
            // Retry after one frame if InputManager isn't ready yet
            StartCoroutine(RetryInputManagerSetup());
        }
    }

    /// <summary>
    /// Unsubscribe from InputManager events on disable
    /// </summary>
    void OnDisable()
    {
        if (inputManager != null)
        {
            // Unsubscribe from input events
            inputManager.OnMoveInput -= HandleMoveInput;
            inputManager.OnJumpPressed -= HandleJumpPressed;
            inputManager.OnJumpReleased -= HandleJumpReleased;
            inputManager.OnDashPressed -= HandleDashPressed;
            inputManager.OnAttackPressed -= HandleAttackPressed;
        }
    }

    /// <summary>
    /// Setup InputManager subscription
    /// </summary>
    private void SetupInputManager()
    {
        inputManager = InputManager.Instance;
        if (inputManager != null)
        {
            // Subscribe to input events
            inputManager.OnMoveInput += HandleMoveInput;
            inputManager.OnJumpPressed += HandleJumpPressed;
            inputManager.OnJumpReleased += HandleJumpReleased;
            inputManager.OnDashPressed += HandleDashPressed;
            inputManager.OnAttackPressed += HandleAttackPressed;
        }
    }

    /// <summary>
    /// Retry InputManager setup after one frame
    /// </summary>
    private IEnumerator RetryInputManagerSetup()
    {
        yield return null; // Wait one frame

        if (InputManager.Instance != null)
        {
            SetupInputManager();
        }
        else
        {
            Debug.LogError("InputManager instance not found! Make sure InputManager is in the scene.");
        }
    }

    /* ─── Input Event Handlers ─── */

    /// <summary>
    /// Handle move input from InputManager
    /// </summary>
    private void HandleMoveInput(Vector2 input)
    {
        OnMove?.Invoke(input);
    }

    /// <summary>
    /// Handle jump button press
    /// </summary>
    private void HandleJumpPressed()
    {
        OnJumpPressed?.Invoke();
    }

    /// <summary>
    /// Handle jump button release
    /// </summary>
    private void HandleJumpReleased()
    {
        OnJumpReleased?.Invoke();
    }

    /// <summary>
    /// Handle dash button press
    /// </summary>
    private void HandleDashPressed()
    {
        OnDashPressed?.Invoke();
    }

    /// <summary>
    /// Handle attack button press
    /// </summary>
    private void HandleAttackPressed()
    {
        OnAttackPressed?.Invoke();
    }

    /// <summary>
    /// Get current move input directly from InputManager
    /// </summary>
    public Vector2 GetMoveInput()
    {
        return inputManager != null ? inputManager.MoveInput : Vector2.zero;
    }
}
