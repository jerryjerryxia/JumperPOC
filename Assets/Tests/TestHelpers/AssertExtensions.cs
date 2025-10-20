using NUnit.Framework;
using UnityEngine;

namespace Tests.Helpers
{
    /// <summary>
    /// Custom assertion extensions for Unity types and common game testing scenarios.
    /// </summary>
    public static class AssertExtensions
    {
        private const float DEFAULT_EPSILON = 0.0001f;

        /// <summary>
        /// Assert that two Vector2 values are approximately equal within epsilon tolerance.
        /// </summary>
        public static void AreApproximatelyEqual(Vector2 expected, Vector2 actual, float epsilon = DEFAULT_EPSILON, string message = null)
        {
            bool areEqual = Mathf.Abs(expected.x - actual.x) < epsilon &&
                           Mathf.Abs(expected.y - actual.y) < epsilon;

            if (!areEqual)
            {
                string failMessage = message ?? $"Expected: {expected}, Actual: {actual}, Epsilon: {epsilon}";
                Assert.Fail(failMessage);
            }
        }

        /// <summary>
        /// Assert that two float values are approximately equal within epsilon tolerance.
        /// </summary>
        public static void AreApproximatelyEqual(float expected, float actual, float epsilon = DEFAULT_EPSILON, string message = null)
        {
            bool areEqual = Mathf.Abs(expected - actual) < epsilon;

            if (!areEqual)
            {
                string failMessage = message ?? $"Expected: {expected}, Actual: {actual}, Epsilon: {epsilon}";
                Assert.Fail(failMessage);
            }
        }

        /// <summary>
        /// Assert that a float value is within a specified range (inclusive).
        /// </summary>
        public static void IsInRange(float value, float min, float max, string message = null)
        {
            bool isInRange = value >= min && value <= max;

            if (!isInRange)
            {
                string failMessage = message ?? $"Value {value} is not in range [{min}, {max}]";
                Assert.Fail(failMessage);
            }
        }

        /// <summary>
        /// Assert that a boolean condition becomes true within a timeout (useful for async operations).
        /// NOTE: This is a placeholder for EditMode tests - actual implementation requires coroutines in PlayMode.
        /// </summary>
        public static void BecomesTrue(System.Func<bool> condition, float timeoutSeconds = 1f, string message = null)
        {
            // In EditMode tests, we can only check immediately
            bool result = condition();

            if (!result)
            {
                string failMessage = message ?? "Condition did not become true";
                Assert.Fail(failMessage);
            }
        }
    }
}
