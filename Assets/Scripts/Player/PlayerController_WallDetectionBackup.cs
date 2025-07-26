/*
 * BACKUP OF COMPLEX WALL DETECTION LOGIC
 * This file contains the original complex wall detection system that was replaced
 * with simpler logic. Kept for reference and potential future use.
 * 
 * DO NOT COMPILE - This is a comment-only backup file
 */

#if false

// Original complex variables that were removed:
private float lastHorizontalMovementTime = 0f;
private float wallConditionStartTime = -1f;
private bool wallStickAllowed; 
private bool wallContactDetectedDebug;
private int validWallHitsDebug;
private float minWallDistanceDebug;

// Original complex CheckWallDetection method:
private void CheckWallDetection()
{
    Collider2D playerCollider = GetComponent<Collider2D>();
    int groundLayer = LayerMask.NameToLayer("Ground");
    int groundMask = 1 << groundLayer;
    
    // Use short-range contact detection instead of long raycasts
    // wallCheckDistance is now a public Inspector variable
    bool wallContactDetected = false;
    int validWallHits = 0;
    float minWallDistance = float.MaxValue;
    
    Vector2[] wallCheckOrigins = {
        transform.position + Vector3.up * 0.4f,
        transform.position + Vector3.up * 0.2f,
        transform.position,
        transform.position + Vector3.up * -0.2f,
        transform.position + Vector3.up * -0.4f
    };
    
    Vector2 wallDirection = facingRight ? Vector2.right : Vector2.left;
    
    for (int i = 0; i < wallCheckOrigins.Length; i++)
    {
        var origin = wallCheckOrigins[i];
        RaycastHit2D hit = Physics2D.Raycast(origin, wallDirection, wallCheckDistance, groundMask);
        
        if (hit.collider != null && hit.collider != playerCollider)
        {
            // Strict wall normal validation - must be a vertical wall
            bool normalValid = Mathf.Abs(hit.normal.x) > 0.98f && Mathf.Abs(hit.normal.y) < 0.15f;
            if (normalValid)
            {
                validWallHits++;
                minWallDistance = Mathf.Min(minWallDistance, hit.distance);
            }
            // Debug each raycast result
            Debug.Log($"[RAY {i}] HIT: {hit.collider.name} | Distance: {hit.distance:F3} | Normal: {hit.normal} | ValidNormal: {normalValid}");
        }
        else
        {
            Debug.Log($"[RAY {i}] MISS: No hit or hit player collider");
        }
    }
    
    // Only consider it a wall contact if we're very close (actual contact)
    wallContactDetected = validWallHits >= 2 && minWallDistance < 0.1f;
    wallContactDetectedDebug = wallContactDetected; // Store for debug GUI
    validWallHitsDebug = validWallHits;
    minWallDistanceDebug = minWallDistance;
    
    // Input conditions
    bool pressingTowardWall = (facingRight && moveInput.x > 0.1f) || (!facingRight && moveInput.x < -0.1f);
    bool notMovingAwayFromWall = !((facingRight && moveInput.x < -0.1f) || (!facingRight && moveInput.x > 0.1f));
    
    // Wall slide physics: requires active input toward wall
    bool wallSlideConditions = wallContactDetected && !isGrounded && !isBufferClimbing && pressingTowardWall;
    
    // Wall contact for animation: more lenient, allows wall land animation with minimal input
    bool minimalInputTowardWall = (facingRight && moveInput.x > 0.05f) || (!facingRight && moveInput.x < -0.05f);
    bool wallAnimationConditions = wallContactDetected && !isGrounded && !isBufferClimbing && 
                                  (pressingTowardWall || minimalInputTowardWall || notMovingAwayFromWall);
    
    // Wall stick animation: allow immediate wall stick if recently jumped
    bool noRecentHorizontalMovement = Time.time - lastHorizontalMovementTime > 0.01f; // 10ms
    bool recentlyJumped = Time.time - lastJumpTime < 0.15f; // Allow wall stick for 150ms after jump
    bool allowWallStick = noRecentHorizontalMovement || recentlyJumped;
    
    bool wallStickConditions = wallContactDetected && !isGrounded && !isBufferClimbing && 
                              notMovingAwayFromWall && allowWallStick;
    
    // Set physics state (controls slower fall speed) - requires strong input
    onWall = wallSlideConditions;
    
    // Store wall stick permission for animation system
    wallStickAllowed = wallStickConditions;
    
    // Store separate wall contact state for animation that's more lenient
    wallContact = wallAnimationConditions;
    
    // Final override: buffer climbing always disables wall state
    if (isBufferClimbing)
    {
        onWall = false;
        wallStickAllowed = false;
        wallContact = false;
    }
    
    if (onWall != prevOnWall)
    {
        prevOnWall = onWall;
    }
}

// Original complex horizontal movement tracking:
// Track horizontal movement for wall stick restriction
// Only track movement if it's not from a recent jump (allow immediate wall stick after jumping)
bool recentJump = Time.time - lastJumpTime < 0.15f; // Match the wall detection timing
if (Mathf.Abs(rb.linearVelocity.x) > 0.1f && !recentJump)
{
    lastHorizontalMovementTime = Time.time;
}

#endif