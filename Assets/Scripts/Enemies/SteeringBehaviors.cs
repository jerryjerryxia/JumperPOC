using UnityEngine;

namespace Enemies
{
    /// <summary>
    /// Steering behaviors for AI movement based on Craig Reynolds' work.
    /// Provides smooth, natural-looking obstacle avoidance and navigation.
    /// Used for flying enemies to avoid platforms, walls, and other obstacles.
    /// </summary>
    public class SteeringBehaviors : MonoBehaviour
    {
        [Header("Obstacle Avoidance")]
        [SerializeField] private float obstacleAvoidDistance = 2.5f; // How far ahead to detect obstacles
        [SerializeField] private float avoidanceForce = 8f; // Strength of avoidance steering
        [SerializeField] private int rayCount = 5; // Number of detection rays (more = smoother but more expensive)
        [SerializeField] private float raySpreadAngle = 45f; // Angular spread of detection rays
        [SerializeField] private LayerMask obstacleLayer = 1 << 6; // What layers to avoid (Ground by default)

        [Header("Force Limits")]
        [SerializeField] private float maxForce = 15f; // Maximum steering force that can be applied
        [SerializeField] private float slowdownRadius = 1.5f; // Start slowing down when this close to obstacle

        [Header("Debug Visualization")]
        [SerializeField] private bool showDebugRays = true;
        [SerializeField] private bool showSteeringForces = true;
        [SerializeField] private Color rayHitColor = Color.red;
        [SerializeField] private Color rayMissColor = Color.green;

        // Components
        private Rigidbody2D rb;

        // Debug info
        private Vector2 lastAvoidanceForce;
        private RaycastHit2D[] lastHits = new RaycastHit2D[0];

        private void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
            if (rb == null)
            {
                Debug.LogError($"[SteeringBehaviors] {name} requires Rigidbody2D component!");
            }
        }

        /// <summary>
        /// Configure steering behavior parameters from external source (e.g., FlyingEnemy).
        /// Call this after adding the component to override default values.
        /// </summary>
        public void Configure(
            float obstacleAvoidDist,
            float avoidForce,
            int numRays,
            float raySpread,
            float maxForceLimit,
            bool showDebug,
            LayerMask obstacles)
        {
            obstacleAvoidDistance = obstacleAvoidDist;
            avoidanceForce = avoidForce;
            rayCount = numRays;
            raySpreadAngle = raySpread;
            maxForce = maxForceLimit;
            showDebugRays = showDebug;
            showSteeringForces = showDebug;
            obstacleLayer = obstacles;
        }

        /// <summary>
        /// Calculate steering force to seek toward a target position.
        /// </summary>
        public Vector2 Seek(Vector2 targetPosition)
        {
            Vector2 currentPos = transform.position;
            Vector2 desired = (targetPosition - currentPos).normalized;

            // Get current velocity (for flying enemies using manual velocity tracking)
            Vector2 currentVelocity = rb != null ? rb.linearVelocity : Vector2.zero;

            // Steering = desired - current
            Vector2 steer = desired - currentVelocity.normalized;

            return steer.normalized * maxForce;
        }

        /// <summary>
        /// Calculate steering force to avoid obstacles using raycast detection.
        /// This is the core obstacle avoidance system.
        /// </summary>
        public Vector2 AvoidObstacles(Vector2 currentVelocity)
        {
            if (currentVelocity.sqrMagnitude < 0.01f)
            {
                // Not moving, no avoidance needed
                lastAvoidanceForce = Vector2.zero;
                lastHits = new RaycastHit2D[0];
                return Vector2.zero;
            }

            Vector2 avoidance = Vector2.zero;
            Vector2 currentPos = transform.position;
            Vector2 forwardDir = currentVelocity.normalized;

            // Cast multiple rays in a cone pattern ahead of movement
            System.Collections.Generic.List<RaycastHit2D> hitsList = new System.Collections.Generic.List<RaycastHit2D>();

            for (int i = 0; i < rayCount; i++)
            {
                // Calculate ray angle (-spreadAngle/2 to +spreadAngle/2)
                float angleOffset = Mathf.Lerp(-raySpreadAngle / 2f, raySpreadAngle / 2f, i / (float)(rayCount - 1));
                Vector2 rayDir = Quaternion.Euler(0, 0, angleOffset) * forwardDir;

                // Dynamic ray distance based on speed
                float rayDistance = obstacleAvoidDistance;

                // Cast ray
                RaycastHit2D hit = Physics2D.Raycast(currentPos, rayDir, rayDistance, obstacleLayer);

                if (hit.collider != null)
                {
                    hitsList.Add(hit);

                    // Calculate avoidance force
                    // Force is stronger when:
                    // 1. Obstacle is closer (inverse distance)
                    // 2. Obstacle is more directly ahead (center rays weighted higher)
                    float closeness = 1f - (hit.distance / rayDistance);
                    float centerWeight = 1f - Mathf.Abs(angleOffset) / (raySpreadAngle / 2f); // Center rays = 1.0, edge rays = 0.0

                    // Direction to steer: perpendicular to obstacle surface
                    Vector2 avoidDir = Vector2.Perpendicular(hit.normal).normalized;

                    // Choose direction that aligns with current movement (don't reverse)
                    if (Vector2.Dot(avoidDir, currentVelocity) < 0)
                    {
                        avoidDir = -avoidDir;
                    }

                    // Add weighted avoidance force
                    float forceMagnitude = avoidanceForce * closeness * centerWeight;
                    avoidance += avoidDir * forceMagnitude;

                    // Also add upward component if obstacle is below (helps flying enemies rise over platforms)
                    if (hit.normal.y > 0.3f) // Horizontal or upward-facing surface
                    {
                        avoidance += Vector2.up * forceMagnitude * 0.5f;
                    }
                }
            }

            lastHits = hitsList.ToArray();

            // Clamp to max force
            if (avoidance.magnitude > maxForce)
            {
                avoidance = avoidance.normalized * maxForce;
            }

            lastAvoidanceForce = avoidance;
            return avoidance;
        }

        /// <summary>
        /// Calculate steering force to maintain separation from nearby objects.
        /// Useful for swarms/flocking behavior.
        /// </summary>
        public Vector2 Separate(Collider2D[] neighbors, float separationDistance)
        {
            if (neighbors == null || neighbors.Length == 0)
                return Vector2.zero;

            Vector2 separation = Vector2.zero;
            Vector2 currentPos = transform.position;

            foreach (Collider2D neighbor in neighbors)
            {
                if (neighbor == null || neighbor.gameObject == gameObject)
                    continue;

                Vector2 neighborPos = neighbor.transform.position;
                float distance = Vector2.Distance(currentPos, neighborPos);

                if (distance < separationDistance && distance > 0.01f)
                {
                    // Steer away from neighbor, stronger when closer
                    Vector2 awayDir = (currentPos - neighborPos).normalized;
                    float strength = 1f - (distance / separationDistance);
                    separation += awayDir * strength;
                }
            }

            if (separation.magnitude > maxForce)
            {
                separation = separation.normalized * maxForce;
            }

            return separation;
        }

        /// <summary>
        /// Combine multiple steering forces with weights.
        /// </summary>
        public Vector2 CombineForces(params (Vector2 force, float weight)[] forces)
        {
            Vector2 combined = Vector2.zero;

            foreach (var (force, weight) in forces)
            {
                combined += force * weight;
            }

            if (combined.magnitude > maxForce)
            {
                combined = combined.normalized * maxForce;
            }

            return combined;
        }

        // Debug visualization
        private void OnDrawGizmos()
        {
            if (!Application.isPlaying || !showDebugRays)
                return;

            Vector2 currentPos = transform.position;
            Vector2 currentVelocity = rb != null ? rb.linearVelocity : Vector2.zero;

            if (currentVelocity.sqrMagnitude < 0.01f)
                return;

            Vector2 forwardDir = currentVelocity.normalized;

            // Draw detection rays
            for (int i = 0; i < rayCount; i++)
            {
                float angleOffset = Mathf.Lerp(-raySpreadAngle / 2f, raySpreadAngle / 2f, i / (float)(rayCount - 1));
                Vector2 rayDir = Quaternion.Euler(0, 0, angleOffset) * forwardDir;

                // Check if this ray hit something
                bool didHit = false;
                float hitDistance = obstacleAvoidDistance;
                foreach (var hit in lastHits)
                {
                    if (hit.collider != null)
                    {
                        Vector2 hitRayDir = (hit.point - currentPos).normalized;
                        if (Vector2.Dot(hitRayDir, rayDir) > 0.99f) // Same direction
                        {
                            didHit = true;
                            hitDistance = hit.distance;
                            break;
                        }
                    }
                }

                Gizmos.color = didHit ? rayHitColor : rayMissColor;
                Gizmos.DrawLine(currentPos, currentPos + rayDir * hitDistance);
            }

            // Draw avoidance force arrow
            if (showSteeringForces && lastAvoidanceForce.magnitude > 0.01f)
            {
                Gizmos.color = Color.yellow;
                Vector2 endPos = currentPos + lastAvoidanceForce * 0.5f;
                Gizmos.DrawLine(currentPos, endPos);

                // Draw arrowhead
                Vector2 arrowDir = lastAvoidanceForce.normalized;
                Vector2 perpDir = Vector2.Perpendicular(arrowDir);
                Gizmos.DrawLine(endPos, endPos - arrowDir * 0.2f + perpDir * 0.1f);
                Gizmos.DrawLine(endPos, endPos - arrowDir * 0.2f - perpDir * 0.1f);
            }
        }
    }
}
