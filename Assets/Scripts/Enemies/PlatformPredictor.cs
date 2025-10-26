using UnityEngine;
using System.Collections.Generic;

namespace Enemies
{
    /// <summary>
    /// Predicts moving platform positions to enable proactive avoidance.
    /// Works alongside SteeringBehaviors for two-layer intelligence:
    /// - Strategic: Predict and avoid platforms 1-3 seconds ahead
    /// - Tactical: Reactive steering for unexpected obstacles
    /// </summary>
    public class PlatformPredictor : MonoBehaviour
    {
        [Header("Prediction Settings")]
        [SerializeField] private float detectionRadius = 10f; // How far to detect platforms
        [SerializeField] private float predictionTime = 2f; // How many seconds ahead to predict
        [SerializeField] private float updateInterval = 0.5f; // How often to recalculate predictions
        [SerializeField] private float dangerThreshold = 2f; // How close predicted platform needs to be to trigger avoidance
        [SerializeField] private LayerMask platformLayer = 1 << 6; // What layer are platforms on?

        [Header("Avoidance Response")]
        [SerializeField] private float heightAdjustment = 1.5f; // How much to adjust height when platform predicted
        [SerializeField] private float horizontalAdjustment = 1.0f; // How much to shift horizontally
        [SerializeField] private bool enableProactiveAvoidance = true; // Toggle prediction system

        [Header("Debug Visualization")]
        [SerializeField] private bool showPredictions = true;
        [SerializeField] private Color safeColor = Color.green;
        [SerializeField] private Color dangerColor = Color.red;

        // Cached platform data
        private class PlatformData
        {
            public MovingPlatform platform;
            public Vector2 predictedPosition;
            public bool isDangerous;
            public float distanceToEnemy;
            public float platformRadius; // Half of platform's collision bounds
        }

        private List<PlatformData> trackedPlatforms = new List<PlatformData>();
        private float nextUpdateTime = 0f;
        private float enemyRadius = 0.5f; // Cached enemy collision radius

        // Public interface for FlyingEnemy
        public Vector2 SuggestedAvoidanceOffset { get; private set; }
        public bool ShouldAvoidPlatforms { get; private set; }
        public int DangerousPlatformCount => GetDangerousCount();

        private void Start()
        {
            // Calculate enemy's collision radius
            enemyRadius = CalculateEnemyRadius();

            // Initial scan for platforms
            DetectNearbyPlatforms();
        }

        /// <summary>
        /// Calculate enemy's collision radius (largest dimension of collider)
        /// </summary>
        private float CalculateEnemyRadius()
        {
            Collider2D col = GetComponent<Collider2D>();
            if (col == null)
            {
                Debug.LogWarning($"[PlatformPredictor] {name} has no Collider2D, using default radius 0.5");
                return 0.5f;
            }

            Bounds bounds = col.bounds;
            // Use the larger of width or height as radius (conservative estimate)
            float radius = Mathf.Max(bounds.size.x, bounds.size.y) * 0.5f;

            Debug.Log($"[PlatformPredictor] {name} collision radius: {radius:F2} units");
            return radius;
        }

        /// <summary>
        /// Calculate platform's collision radius
        /// </summary>
        private float CalculatePlatformRadius(MovingPlatform platform)
        {
            Collider2D col = platform.GetComponent<Collider2D>();
            if (col == null)
            {
                return 1.0f; // Default fallback
            }

            Bounds bounds = col.bounds;
            // Use the larger dimension as radius
            return Mathf.Max(bounds.size.x, bounds.size.y) * 0.5f;
        }

        private void Update()
        {
            if (!enableProactiveAvoidance)
            {
                ShouldAvoidPlatforms = false;
                SuggestedAvoidanceOffset = Vector2.zero;
                return;
            }

            // Update predictions at interval to save performance
            if (Time.time >= nextUpdateTime)
            {
                UpdatePredictions();
                nextUpdateTime = Time.time + updateInterval;
            }
        }

        /// <summary>
        /// Detect all moving platforms within detection radius
        /// </summary>
        private void DetectNearbyPlatforms()
        {
            trackedPlatforms.Clear();

            Collider2D[] platformColliders = Physics2D.OverlapCircleAll(transform.position, detectionRadius, platformLayer);

            foreach (Collider2D col in platformColliders)
            {
                MovingPlatform platform = col.GetComponent<MovingPlatform>();
                if (platform != null)
                {
                    float platformRadius = CalculatePlatformRadius(platform);

                    trackedPlatforms.Add(new PlatformData
                    {
                        platform = platform,
                        predictedPosition = Vector2.zero,
                        isDangerous = false,
                        distanceToEnemy = 0f,
                        platformRadius = platformRadius
                    });

                    Debug.Log($"[PlatformPredictor] Tracking platform '{platform.name}' with radius {platformRadius:F2}");
                }
            }

            if (trackedPlatforms.Count > 0)
            {
                Debug.Log($"[PlatformPredictor] {name} detected {trackedPlatforms.Count} platforms within {detectionRadius} units");
            }
        }

        /// <summary>
        /// Update predictions for all tracked platforms
        /// </summary>
        private void UpdatePredictions()
        {
            if (trackedPlatforms.Count == 0)
            {
                ShouldAvoidPlatforms = false;
                SuggestedAvoidanceOffset = Vector2.zero;
                return;
            }

            Vector2 myPosition = transform.position;
            Vector2 avoidanceVector = Vector2.zero;
            int dangerCount = 0;

            foreach (PlatformData data in trackedPlatforms)
            {
                if (data.platform == null) continue;

                // Predict platform position after predictionTime seconds
                Vector3 currentVelocity = data.platform.Velocity;
                Vector2 currentPos = data.platform.transform.position;
                Vector2 predictedPos = currentPos + (Vector2)(currentVelocity * predictionTime);

                data.predictedPosition = predictedPos;

                // Calculate distance between predicted platform position and enemy
                float distance = Vector2.Distance(predictedPos, myPosition);
                data.distanceToEnemy = distance;

                // Calculate safe distance accounting for both collision radii
                // Enemy radius + Platform radius + Danger threshold (extra safety margin)
                float combinedRadius = enemyRadius + data.platformRadius;
                float safeDistance = dangerThreshold + combinedRadius;

                // Check if this is dangerous (platform will be close to enemy)
                // Now accounts for actual collision bounds, not just center points
                data.isDangerous = distance < safeDistance;

                if (data.isDangerous)
                {
                    dangerCount++;

                    // Calculate avoidance direction (perpendicular to platform velocity)
                    Vector2 platformDirection = currentVelocity.normalized;

                    if (platformDirection.sqrMagnitude > 0.01f) // Platform is moving
                    {
                        // Prefer moving up/down for horizontal platforms, left/right for vertical
                        if (Mathf.Abs(platformDirection.x) > Mathf.Abs(platformDirection.y))
                        {
                            // Horizontal platform - move vertically
                            float directionSign = (myPosition.y > currentPos.y) ? 1f : -1f;
                            avoidanceVector += Vector2.up * heightAdjustment * directionSign;
                        }
                        else
                        {
                            // Vertical platform - move horizontally
                            float directionSign = (myPosition.x > currentPos.x) ? 1f : -1f;
                            avoidanceVector += Vector2.right * horizontalAdjustment * directionSign;
                        }
                    }
                }
            }

            // Set avoidance state
            ShouldAvoidPlatforms = dangerCount > 0;
            SuggestedAvoidanceOffset = avoidanceVector.normalized * Mathf.Min(avoidanceVector.magnitude, 3f); // Cap at 3 units
        }

        /// <summary>
        /// Get count of dangerous platforms
        /// </summary>
        private int GetDangerousCount()
        {
            int count = 0;
            foreach (var data in trackedPlatforms)
            {
                if (data.isDangerous) count++;
            }
            return count;
        }

        /// <summary>
        /// Manually trigger platform re-detection (useful if platforms spawn/despawn)
        /// </summary>
        public void RefreshPlatformDetection()
        {
            DetectNearbyPlatforms();
        }

        // Debug visualization
        private void OnDrawGizmosSelected()
        {
            if (!showPredictions) return;

            // Draw detection radius
            Gizmos.color = new Color(0, 1, 1, 0.2f); // Cyan, transparent
            DrawCircle(transform.position, detectionRadius);

            // Draw enemy collision radius
            Gizmos.color = new Color(1, 0, 1, 0.4f); // Magenta, semi-transparent
            DrawCircle(transform.position, enemyRadius);

            // Draw predicted platform positions and danger zones
            foreach (var data in trackedPlatforms)
            {
                if (data.platform == null) continue;

                Vector2 currentPos = data.platform.transform.position;
                Vector2 predictedPos = data.predictedPosition;

                // Draw platform collision radius at predicted position
                Gizmos.color = data.isDangerous ? new Color(1, 0, 0, 0.3f) : new Color(0, 1, 0, 0.2f);
                DrawCircle(predictedPos, data.platformRadius);

                // Draw combined safe zone (enemy radius + platform radius + danger threshold)
                float combinedRadius = enemyRadius + data.platformRadius;
                float safeDistance = dangerThreshold + combinedRadius;
                Gizmos.color = data.isDangerous ? new Color(1, 0.5f, 0, 0.2f) : new Color(0.5f, 1, 0.5f, 0.1f);
                DrawCircle(predictedPos, safeDistance);

                // Draw line from current to predicted position
                Gizmos.color = data.isDangerous ? dangerColor : safeColor;
                Gizmos.DrawLine(currentPos, predictedPos);

                // Draw predicted position marker
                Gizmos.DrawWireSphere(predictedPos, 0.3f);

                // Draw distance line from enemy to predicted position
                if (data.isDangerous)
                {
                    Gizmos.color = new Color(1, 0, 0, 0.5f);
                    Gizmos.DrawLine(transform.position, predictedPos);
                }
            }

            // Draw suggested avoidance vector
            if (ShouldAvoidPlatforms && SuggestedAvoidanceOffset.magnitude > 0.01f)
            {
                Vector2 start = transform.position;
                Vector2 end = start + SuggestedAvoidanceOffset;

                Gizmos.color = Color.yellow;
                Gizmos.DrawLine(start, end);

                // Draw arrow head
                Vector2 dir = SuggestedAvoidanceOffset.normalized;
                Vector2 perpDir = new Vector2(-dir.y, dir.x);
                Gizmos.DrawLine(end, end - dir * 0.3f + perpDir * 0.15f);
                Gizmos.DrawLine(end, end - dir * 0.3f - perpDir * 0.15f);
            }
        }

        private void DrawCircle(Vector2 center, float radius)
        {
            int segments = 32;
            float angleStep = 360f / segments;

            for (int i = 0; i < segments; i++)
            {
                float angle1 = i * angleStep * Mathf.Deg2Rad;
                float angle2 = (i + 1) * angleStep * Mathf.Deg2Rad;

                Vector2 point1 = center + new Vector2(Mathf.Cos(angle1) * radius, Mathf.Sin(angle1) * radius);
                Vector2 point2 = center + new Vector2(Mathf.Cos(angle2) * radius, Mathf.Sin(angle2) * radius);

                Gizmos.DrawLine(point1, point2);
            }
        }
    }
}
