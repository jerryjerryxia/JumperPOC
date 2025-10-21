using UnityEngine;

/// <summary>
/// Handles moving platform behavior with support for horizontal, vertical, and diagonal movement.
/// Player automatically inherits platform velocity when standing on it.
/// Extendable to waypoints, circular paths, and other movement patterns.
/// </summary>
public class MovingPlatform : MonoBehaviour
{
    public enum MovementType
    {
        Horizontal,
        Vertical,
        Diagonal
    }

    [Header("Movement Configuration")]
    [SerializeField] private MovementType movementType = MovementType.Horizontal;
    [SerializeField] private float speed = 2f;
    [SerializeField] private float distance = 5f;
    [SerializeField] private bool autoReverse = true;

    [Header("Diagonal Movement (only if Diagonal type selected)")]
    [Tooltip("Angle in degrees for diagonal movement (0 = right, 90 = up, 180 = left, 270 = down)")]
    [SerializeField] private float diagonalAngle = 45f;

    [Header("Movement Behavior")]
    [SerializeField] private float pauseAtEndpoints = 0f;
    [Tooltip("If true, uses smooth easing. If false, uses constant speed.")]
    [SerializeField] private bool useSmoothMovement = false;

    [Header("Debug")]
    [SerializeField] private bool showDebugGizmos = true;
    [SerializeField] private Color gizmoColor = Color.yellow;

    // Public velocity for player to inherit
    public Vector3 Velocity { get; private set; }

    // Movement state
    private Vector3 startPosition;
    private Vector3 endPosition;
    private Vector3 previousPosition;
    private bool movingForward = true;
    private float pauseTimer = 0f;
    private bool isPaused = false;

    // For smooth movement
    private float journeyProgress = 0f;

    void Start()
    {
        // Store starting position
        startPosition = transform.position;
        previousPosition = startPosition;

        // Calculate end position based on movement type
        CalculateEndPosition();
    }

    void FixedUpdate()
    {
        // Store position before movement for velocity calculation
        previousPosition = transform.position;

        // Handle pause at endpoints
        if (isPaused)
        {
            pauseTimer -= Time.fixedDeltaTime;
            if (pauseTimer <= 0f)
            {
                isPaused = false;
            }
            Velocity = Vector3.zero;
            return;
        }

        // Move platform
        if (useSmoothMovement)
        {
            UpdateSmoothMovement();
        }
        else
        {
            UpdateConstantMovement();
        }

        // Calculate velocity for player inheritance
        Velocity = (transform.position - previousPosition) / Time.fixedDeltaTime;
    }

    private void CalculateEndPosition()
    {
        Vector3 direction = Vector3.zero;

        switch (movementType)
        {
            case MovementType.Horizontal:
                direction = Vector3.right;
                break;

            case MovementType.Vertical:
                direction = Vector3.up;
                break;

            case MovementType.Diagonal:
                // Convert angle to direction vector
                float radians = diagonalAngle * Mathf.Deg2Rad;
                direction = new Vector3(Mathf.Cos(radians), Mathf.Sin(radians), 0f);
                break;
        }

        endPosition = startPosition + direction * distance;
    }

    private void UpdateConstantMovement()
    {
        // Determine current target
        Vector3 target = movingForward ? endPosition : startPosition;

        // Move toward target at constant speed
        float step = speed * Time.fixedDeltaTime;
        transform.position = Vector3.MoveTowards(transform.position, target, step);

        // Check if reached endpoint
        if (Vector3.Distance(transform.position, target) < 0.01f)
        {
            // Snap to exact position
            transform.position = target;

            if (autoReverse)
            {
                // Reverse direction
                movingForward = !movingForward;

                // Start pause if configured
                if (pauseAtEndpoints > 0f)
                {
                    isPaused = true;
                    pauseTimer = pauseAtEndpoints;
                }
            }
        }
    }

    private void UpdateSmoothMovement()
    {
        // Calculate progress (0 to 1)
        float totalDistance = Vector3.Distance(startPosition, endPosition);
        journeyProgress += (speed * Time.fixedDeltaTime) / totalDistance;

        // Clamp progress
        journeyProgress = Mathf.Clamp01(journeyProgress);

        // Apply easing (sine wave for smooth acceleration/deceleration)
        float easedProgress = Mathf.Sin(journeyProgress * Mathf.PI * 0.5f);

        // Calculate position
        if (movingForward)
        {
            transform.position = Vector3.Lerp(startPosition, endPosition, easedProgress);
        }
        else
        {
            transform.position = Vector3.Lerp(endPosition, startPosition, easedProgress);
        }

        // Check if reached end
        if (journeyProgress >= 1f)
        {
            journeyProgress = 0f;

            if (autoReverse)
            {
                movingForward = !movingForward;

                // Start pause if configured
                if (pauseAtEndpoints > 0f)
                {
                    isPaused = true;
                    pauseTimer = pauseAtEndpoints;
                }
            }
        }
    }

    /// <summary>
    /// Get the platform's current velocity. Used by player to inherit movement.
    /// </summary>
    public Vector3 GetVelocity()
    {
        return Velocity;
    }

    /// <summary>
    /// Manually set the platform to pause for a duration
    /// </summary>
    public void Pause(float duration)
    {
        isPaused = true;
        pauseTimer = duration;
    }

    /// <summary>
    /// Resume platform movement immediately
    /// </summary>
    public void Resume()
    {
        isPaused = false;
        pauseTimer = 0f;
    }

    void OnDrawGizmos()
    {
        if (!showDebugGizmos) return;

        // Calculate positions for gizmos
        Vector3 start = Application.isPlaying ? startPosition : transform.position;
        Vector3 end = Application.isPlaying ? endPosition : start;

        // Calculate end position for editor preview
        if (!Application.isPlaying)
        {
            Vector3 direction = Vector3.zero;

            switch (movementType)
            {
                case MovementType.Horizontal:
                    direction = Vector3.right;
                    break;

                case MovementType.Vertical:
                    direction = Vector3.up;
                    break;

                case MovementType.Diagonal:
                    float radians = diagonalAngle * Mathf.Deg2Rad;
                    direction = new Vector3(Mathf.Cos(radians), Mathf.Sin(radians), 0f);
                    break;
            }

            end = start + direction * distance;
        }

        // Draw movement path
        Gizmos.color = gizmoColor;
        Gizmos.DrawLine(start, end);

        // Draw endpoint markers
        Gizmos.DrawWireSphere(start, 0.2f);
        Gizmos.DrawWireSphere(end, 0.2f);

        // Draw arrow indicating direction
        Vector3 arrowDir = (end - start).normalized;
        Vector3 midPoint = (start + end) * 0.5f;

        // Arrow head
        Vector3 arrowRight = Quaternion.Euler(0, 0, 150) * arrowDir * 0.3f;
        Vector3 arrowLeft = Quaternion.Euler(0, 0, -150) * arrowDir * 0.3f;

        Gizmos.DrawLine(midPoint, midPoint + arrowRight);
        Gizmos.DrawLine(midPoint, midPoint + arrowLeft);

        // Draw current velocity vector when playing
        if (Application.isPlaying && Velocity.magnitude > 0.01f)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(transform.position, transform.position + Velocity * 0.5f);
        }
    }

    void OnDrawGizmosSelected()
    {
        if (!showDebugGizmos) return;

        // Show more detailed info when selected
        Gizmos.color = Color.green;

        // Draw platform bounds (approximate)
        SpriteRenderer sprite = GetComponent<SpriteRenderer>();
        if (sprite != null)
        {
            Gizmos.DrawWireCube(transform.position, sprite.bounds.size);
        }
    }
}
