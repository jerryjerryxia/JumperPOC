using UnityEngine;

/// <summary>
/// Handles all debug visualization for PlayerController.
/// Separated from main controller to reduce code clutter.
/// EXTRACTED FROM PlayerController lines 885-1210
/// </summary>
[RequireComponent(typeof(PlayerController))]
public class PlayerDebugVisualizer : MonoBehaviour
{
    [Header("Debug Settings")]
    [SerializeField] private bool enableWallDebugPanel = false; // Wall debug UI
    [SerializeField] private bool enableJumpDebugPanel = false; // Variable jump debug UI
    [SerializeField] private bool enableGizmos = true; // Scene view gizmos

    // Component references
    private PlayerController player;
    private Rigidbody2D rb;
    private InputManager inputManager;
    private PlayerRespawnSystem respawnSystem;
    private PlayerWallDetection wallDetection;
    private PlayerGroundDetection groundDetection;
    private PlayerJumpSystem jumpSystem;
    private PlayerMovement movement;

    /// <summary>
    /// Initialize component references
    /// </summary>
    public void Initialize(PlayerController playerController, Rigidbody2D rigidbody,
                          InputManager input, PlayerRespawnSystem respawn)
    {
        player = playerController;
        rb = rigidbody;
        inputManager = input;
        respawnSystem = respawn;

        // Get other component references for parameter access
        wallDetection = playerController.GetComponent<PlayerWallDetection>();
        groundDetection = playerController.GetComponent<PlayerGroundDetection>();
        jumpSystem = playerController.GetComponent<PlayerJumpSystem>();
        movement = playerController.GetComponent<PlayerMovement>();
    }

    // Debug info - OnGUI panels
    void OnGUI()
    {
        if (!Application.isPlaying || player == null) return;

        // Wall Debug Panel
        if (enableWallDebugPanel)
        {
            DrawWallDebugPanel();
        }

        // Variable Jump Debug Panel
        if (enableJumpDebugPanel)
        {
            DrawVariableJumpDebugPanel();
        }
    }

    /// <summary>
    /// Draw wall mechanics debug panel
    /// </summary>
    private void DrawWallDebugPanel()
    {
        GUILayout.BeginArea(new Rect(10, 10, 350, 600));
        GUILayout.Label("=== WALL LAND DEBUG ===", GUI.skin.label);

        // Ability status
        bool hasWallStickAbility = PlayerAbilities.Instance != null && PlayerAbilities.Instance.HasWallStick;
        GUI.contentColor = hasWallStickAbility ? Color.green : Color.red;
        GUILayout.Label($"Wall Stick Ability: {(hasWallStickAbility ? "ENABLED" : "DISABLED")}");
        GUI.contentColor = Color.white;

        // Key states only
        GUILayout.Label($"Grounded: {player.IsGrounded}");
        GUILayout.Label($"Moving: {player.IsRunning}");
        GUI.contentColor = player.IsWallSticking ? Color.green : Color.red;
        GUILayout.Label($"Wall Sticking: {player.IsWallSticking}");
        GUI.contentColor = player.IsWallSliding ? Color.green : Color.red;
        GUILayout.Label($"Wall Sliding: {player.IsWallSliding}");
        GUI.contentColor = player.OnWall ? Color.green : Color.red;
        GUILayout.Label($"On Wall: {player.OnWall}");
        GUI.contentColor = Color.white;

        // Simple wall detection breakdown
        GUILayout.Label("\n--- SIMPLE WALL DETECTION ---");
        float wallCheckDist = wallDetection?.WallCheckDistance ?? 0.15f;
        GUILayout.Label($"wallCheckDistance: {wallCheckDist:F2}");
        GUILayout.Label($"Player Position: {player.transform.position:F2}");
        GUILayout.Label($"Facing Right: {player.FacingRight}");
        GUILayout.Label($"moveInput.x: {player.MoveInput.x:F3}");

        // Simple wall stick conditions
        GUILayout.Label("\n--- SIMPLE WALL STICK CONDITIONS ---");
        bool pressingTowardWall = (player.FacingRight && player.MoveInput.x > 0.1f) ||
                                  (!player.FacingRight && player.MoveInput.x < -0.1f);

        GUI.contentColor = !player.IsGrounded ? Color.green : Color.red;
        GUILayout.Label($"1. !isGrounded: {!player.IsGrounded}");
        GUI.contentColor = player.OnWall ? Color.green : Color.red;
        GUILayout.Label($"2. onWall: {player.OnWall}");
        GUI.contentColor = pressingTowardWall ? Color.green : Color.red;
        GUILayout.Label($"3. pressingTowardWall: {pressingTowardWall}");
        GUI.contentColor = Color.white;

        // Sequential wall logic status
        GUILayout.Label("\n--- SEQUENTIAL WALL LOGIC ---");
        // Note: wallSlideSpeed is in PlayerMovement but not exposed - using IsWallSliding instead
        GUI.contentColor = player.IsWallSliding ? Color.green : Color.red;
        GUILayout.Label($"Is Wall Sliding: {player.IsWallSliding}");
        GUI.contentColor = Color.white;
        GUILayout.Label($"Velocity Y: {rb.linearVelocity.y:F2}");

        // Animation triggers
        GUILayout.Label("\n--- ANIMATION TRIGGERS ---");
        GUILayout.Label($"IsWallSticking: {player.IsWallSticking}");
        GUILayout.Label($"PressingTowardWall: {pressingTowardWall}");

        GUILayout.EndArea();
    }

    /// <summary>
    /// Draw variable jump debug panel
    /// </summary>
    private void DrawVariableJumpDebugPanel()
    {
        GUILayout.BeginArea(new Rect(Screen.width - 320, 10, 310, 300));
        GUILayout.Label("=== VARIABLE JUMP DEBUG ===", GUI.skin.label);

        // Variable jump settings - read from PlayerJumpSystem
        bool enableVariableJump = jumpSystem != null; // Assume enabled if component exists
        GUI.contentColor = enableVariableJump ? Color.green : Color.red;
        GUILayout.Label($"Variable Jump: {(enableVariableJump ? "ENABLED" : "DISABLED")}");
        GUI.contentColor = Color.white;

        if (enableVariableJump && jumpSystem != null)
        {
            // Note: Min/Max velocities are private in PlayerJumpSystem
            // Display basic info instead
            GUILayout.Label($"Jump System: Active");

            GUILayout.Space(10);

            // Current state
            GUI.contentColor = jumpSystem.IsVariableJumpActive ? Color.green : Color.gray;
            GUILayout.Label($"Variable Jump Active: {jumpSystem.IsVariableJumpActive}");
            GUI.contentColor = Color.white;

            if (jumpSystem.IsVariableJumpActive)
            {
                GUILayout.Label($"Current Gravity: {rb.gravityScale:F2}");
                GUILayout.Label($"Y Velocity: {rb.linearVelocity.y:F2}");
            }

            // Input state from InputManager
            if (inputManager != null)
            {
                GUILayout.Space(5);
                GUI.contentColor = inputManager.JumpHeld ? Color.green : Color.gray;
                GUILayout.Label($"Input Jump Held: {inputManager.JumpHeld}");
                GUI.contentColor = Color.white;
            }
        }

        GUILayout.EndArea();
    }

    // Debug visualization for wall detection
    void OnDrawGizmos()
    {
        if (!enableGizmos || player == null) return;

        Vector3 playerPos = player.transform.position;
        Vector3 direction = player.FacingRight ? Vector3.right : Vector3.left;

        // Main wall detection rays - 3 rays (read from PlayerWallDetection)
        float wallCheckDist = wallDetection?.WallCheckDistance ?? 0.15f;
        float wallRayTop = wallDetection?.WallRaycastTop ?? 0.32f;
        float wallRayMid = wallDetection?.WallRaycastMiddle ?? 0.28f;
        float wallRayBot = wallDetection?.WallRaycastBottom ?? 0.02f;

        if (!player.IsGrounded)
        {
            Gizmos.color = player.OnWall ? Color.red : Color.yellow;
            // Draw the 3 wall check rays
            Gizmos.DrawRay(playerPos + Vector3.up * wallRayTop, direction * wallCheckDist);    // Top
            Gizmos.DrawRay(playerPos + Vector3.up * wallRayMid, direction * wallCheckDist);    // Middle
            Gizmos.DrawRay(playerPos + Vector3.up * wallRayBot, direction * wallCheckDist);    // Bottom
        }
        else
        {
            Gizmos.color = Color.gray;
            // Show disabled wall detection when grounded
            Gizmos.DrawRay(playerPos + Vector3.up * wallRayTop, direction * wallCheckDist);
            Gizmos.DrawRay(playerPos + Vector3.up * wallRayMid, direction * wallCheckDist);
            Gizmos.DrawRay(playerPos + Vector3.up * wallRayBot, direction * wallCheckDist);
        }

        // Draw small spheres at raycast origins for clarity
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(playerPos + Vector3.up * wallRayTop, 0.02f);    // Top
        Gizmos.DrawWireSphere(playerPos + Vector3.up * wallRayMid, 0.02f);    // Middle
        Gizmos.DrawWireSphere(playerPos + Vector3.up * wallRayBot, 0.02f);    // Bottom

        // Ground detection
        Gizmos.color = player.IsGrounded ? Color.green : Color.blue;
        Gizmos.DrawWireSphere(playerPos + Vector3.down * 0.6f, 0.1f);

        // Precise ground check visualization - read from PlayerGroundDetection
        Collider2D col = player.GetComponent<Collider2D>();
        if (col != null && groundDetection != null)
        {
            // Access parameters via reflection since they're private
            var type = groundDetection.GetType();
            float groundCheckOffsetY = (float)(type.GetField("groundCheckOffsetY",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.GetValue(groundDetection) ?? -0.02f);
            float groundCheckRadius = (float)(type.GetField("groundCheckRadius",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.GetValue(groundDetection) ?? 0.03f);

            float feetY = col.bounds.min.y;
            Vector2 feetPos = new Vector2(player.transform.position.x, feetY + groundCheckOffsetY);

            // Smaller, less prominent ground check visualization
            Gizmos.color = new Color(1f, 0f, 1f, 0.3f); // Transparent magenta
            Gizmos.DrawWireSphere(feetPos, groundCheckRadius);

            if (Application.isPlaying && rb != null)
            {
                int groundLayer = LayerMask.NameToLayer("Ground");
                int bufferLayer = LayerMask.NameToLayer("LandingBuffer");
                int platformMask = (1 << groundLayer);
                int bufferMask = (1 << bufferLayer);

                bool groundedByPlatform = Physics2D.OverlapCircle(feetPos, groundCheckRadius, platformMask);
                bool groundedByBuffer = Physics2D.OverlapCircle(feetPos, groundCheckRadius, bufferMask);

                // Apply velocity restriction to buffer detection (same as main logic)
                if (groundedByBuffer && rb.linearVelocity.y > 0.1f)
                {
                    groundedByBuffer = false;
                }

                // More subtle ground detection visualization
                Gizmos.color = groundedByPlatform ? new Color(0f, 1f, 0f, 0.4f) : new Color(1f, 0f, 0f, 0.4f);
                Gizmos.DrawWireSphere(feetPos, groundCheckRadius + 0.02f);

                Gizmos.color = groundedByBuffer ? new Color(1f, 1f, 0f, 0.4f) : new Color(0f, 1f, 1f, 0.4f);
                Gizmos.DrawWireSphere(feetPos, groundCheckRadius + 0.04f);
            }
        }

        // Draw death zone in scene view - read from PlayerRespawnSystem
        if (respawnSystem != null && respawnSystem.ShowDeathZone)
        {
            Gizmos.color = Color.red;

            // Use adjustable width for death zone visualization
            float deathZoneY = respawnSystem.DeathZoneY;
            float deathZoneWidth = respawnSystem.DeathZoneWidth;
            float halfWidth = deathZoneWidth / 2f;
            float leftBound = -halfWidth;
            float rightBound = halfWidth;

            // Center on player's initial X position for better visibility
            if (respawnSystem.InitialPosition != Vector3.zero)
            {
                leftBound = respawnSystem.InitialPosition.x - halfWidth;
                rightBound = respawnSystem.InitialPosition.x + halfWidth;
            }

            // Draw death zone line
            Vector3 leftPoint = new Vector3(leftBound, deathZoneY, 0);
            Vector3 rightPoint = new Vector3(rightBound, deathZoneY, 0);
            Gizmos.DrawLine(leftPoint, rightPoint);

            // Draw danger zone (area below death line)
            Gizmos.color = new Color(1f, 0f, 0f, 0.1f); // Semi-transparent red
            Vector3[] dangerZone = new Vector3[4];
            dangerZone[0] = leftPoint;
            dangerZone[1] = rightPoint;
            dangerZone[2] = new Vector3(rightBound, deathZoneY - 10f, 0);
            dangerZone[3] = new Vector3(leftBound, deathZoneY - 10f, 0);

            // Draw filled danger zone
            Gizmos.DrawLine(dangerZone[0], dangerZone[1]);
            Gizmos.DrawLine(dangerZone[1], dangerZone[2]);
            Gizmos.DrawLine(dangerZone[2], dangerZone[3]);
            Gizmos.DrawLine(dangerZone[3], dangerZone[0]);

            // Add warning text indicators
            Gizmos.color = Color.red;
            for (float x = leftBound; x <= rightBound; x += 10f)
            {
                // Draw small downward arrows to indicate danger
                Vector3 arrowTop = new Vector3(x, deathZoneY, 0);
                Vector3 arrowBottom = new Vector3(x, deathZoneY - 0.5f, 0);
                Vector3 arrowLeft = new Vector3(x - 0.2f, deathZoneY - 0.3f, 0);
                Vector3 arrowRight = new Vector3(x + 0.2f, deathZoneY - 0.3f, 0);

                Gizmos.DrawLine(arrowTop, arrowBottom);
                Gizmos.DrawLine(arrowBottom, arrowLeft);
                Gizmos.DrawLine(arrowBottom, arrowRight);
            }
        }
    }
}
