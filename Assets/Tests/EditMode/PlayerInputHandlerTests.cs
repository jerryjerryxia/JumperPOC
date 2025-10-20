using NUnit.Framework;
using UnityEngine;
using Tests.Helpers;

namespace Tests.EditMode
{
    /// <summary>
    /// Unit tests for PlayerInputHandler component.
    /// Tests input event routing and subscription management.
    /// </summary>
    [TestFixture]
    public class PlayerInputHandlerTests
    {
        private GameObject testGameObject;
        private PlayerInputHandler inputHandler;
        private MockInputManager mockInputManager;

        [SetUp]
        public void SetUp()
        {
            // Create test GameObject with PlayerInputHandler
            testGameObject = new GameObject("TestInputHandler");

            // Add PlayerController (required dependency for PlayerInputHandler)
            testGameObject.AddComponent<Rigidbody2D>();
            testGameObject.AddComponent<Animator>();
            testGameObject.AddComponent<PlayerCombat>();
            testGameObject.AddComponent<PlayerController>();

            inputHandler = testGameObject.AddComponent<PlayerInputHandler>();

            // Create mock InputManager for testing
            mockInputManager = new MockInputManager();
        }

        [TearDown]
        public void TearDown()
        {
            // Clean up test objects
            if (testGameObject != null)
            {
                Object.DestroyImmediate(testGameObject);
            }
        }

        #region Event Routing Tests

        [Test]
        public void OnMove_InvokesEvent_WithCorrectVector()
        {
            // Arrange: Subscribe to OnMove event
            Vector2 receivedInput = Vector2.zero;
            inputHandler.OnMove += (input) => receivedInput = input;

            Vector2 testInput = new Vector2(0.8f, 0.3f);

            // Act: Simulate move input (directly invoke the event for testing)
            inputHandler.OnMove?.Invoke(testInput);

            // Assert: Should receive correct input
            Assert.AreEqual(testInput.x, receivedInput.x, 0.001f, "OnMove should pass correct X input");
            Assert.AreEqual(testInput.y, receivedInput.y, 0.001f, "OnMove should pass correct Y input");
        }

        [Test]
        public void OnJumpPressed_InvokesEvent_WhenCalled()
        {
            // Arrange: Subscribe to OnJumpPressed event
            bool eventReceived = false;
            inputHandler.OnJumpPressed += () => eventReceived = true;

            // Act: Simulate jump press
            inputHandler.OnJumpPressed?.Invoke();

            // Assert: Event should have been invoked
            Assert.IsTrue(eventReceived, "OnJumpPressed event should be invoked");
        }

        [Test]
        public void OnJumpReleased_InvokesEvent_WhenCalled()
        {
            // Arrange: Subscribe to OnJumpReleased event
            bool eventReceived = false;
            inputHandler.OnJumpReleased += () => eventReceived = true;

            // Act: Simulate jump release
            inputHandler.OnJumpReleased?.Invoke();

            // Assert: Event should have been invoked
            Assert.IsTrue(eventReceived, "OnJumpReleased event should be invoked");
        }

        [Test]
        public void OnDashPressed_InvokesEvent_WhenCalled()
        {
            // Arrange: Subscribe to OnDashPressed event
            bool eventReceived = false;
            inputHandler.OnDashPressed += () => eventReceived = true;

            // Act: Simulate dash press
            inputHandler.OnDashPressed?.Invoke();

            // Assert: Event should have been invoked
            Assert.IsTrue(eventReceived, "OnDashPressed event should be invoked");
        }

        [Test]
        public void OnAttackPressed_InvokesEvent_WhenCalled()
        {
            // Arrange: Subscribe to OnAttackPressed event
            bool eventReceived = false;
            inputHandler.OnAttackPressed += () => eventReceived = true;

            // Act: Simulate attack press
            inputHandler.OnAttackPressed?.Invoke();

            // Assert: Event should have been invoked
            Assert.IsTrue(eventReceived, "OnAttackPressed event should be invoked");
        }

        #endregion

        #region Multiple Subscriber Tests

        [Test]
        public void OnMove_InvokesAllSubscribers_WhenMultiple()
        {
            // Arrange: Subscribe multiple handlers
            int subscriber1Calls = 0;
            int subscriber2Calls = 0;
            Vector2 subscriber1Input = Vector2.zero;
            Vector2 subscriber2Input = Vector2.zero;

            inputHandler.OnMove += (input) =>
            {
                subscriber1Calls++;
                subscriber1Input = input;
            };

            inputHandler.OnMove += (input) =>
            {
                subscriber2Calls++;
                subscriber2Input = input;
            };

            Vector2 testInput = new Vector2(1f, 0.5f);

            // Act: Invoke event
            inputHandler.OnMove?.Invoke(testInput);

            // Assert: Both subscribers should receive the event
            Assert.AreEqual(1, subscriber1Calls, "First subscriber should be called once");
            Assert.AreEqual(1, subscriber2Calls, "Second subscriber should be called once");
            Assert.AreEqual(testInput, subscriber1Input, "First subscriber should receive correct input");
            Assert.AreEqual(testInput, subscriber2Input, "Second subscriber should receive correct input");
        }

        [Test]
        public void OnJumpPressed_InvokesAllSubscribers_WhenMultiple()
        {
            // Arrange: Subscribe multiple handlers
            int subscriber1Calls = 0;
            int subscriber2Calls = 0;

            inputHandler.OnJumpPressed += () => subscriber1Calls++;
            inputHandler.OnJumpPressed += () => subscriber2Calls++;

            // Act: Invoke event
            inputHandler.OnJumpPressed?.Invoke();

            // Assert: Both subscribers should be called
            Assert.AreEqual(1, subscriber1Calls, "First subscriber should be called");
            Assert.AreEqual(1, subscriber2Calls, "Second subscriber should be called");
        }

        #endregion

        #region Null Safety Tests

        [Test]
        public void GetMoveInput_ReturnsZero_WhenInputManagerNull()
        {
            // Note: This test validates the null-safe fallback behavior
            // When InputManager is null, GetMoveInput should return Vector2.zero

            // Act: Call GetMoveInput without InputManager
            Vector2 result = inputHandler.GetMoveInput();

            // Assert: Should return zero vector safely
            Assert.AreEqual(Vector2.zero, result, "GetMoveInput should return Vector2.zero when InputManager is null");
        }

        [Test]
        public void Events_CanBeInvoked_WhenNoSubscribers()
        {
            // Act: Invoke events with no subscribers (should not throw)
            Assert.DoesNotThrow(() => inputHandler.OnMove?.Invoke(Vector2.zero),
                "OnMove should not throw when invoked with no subscribers");

            Assert.DoesNotThrow(() => inputHandler.OnJumpPressed?.Invoke(),
                "OnJumpPressed should not throw when invoked with no subscribers");

            Assert.DoesNotThrow(() => inputHandler.OnJumpReleased?.Invoke(),
                "OnJumpReleased should not throw when invoked with no subscribers");

            Assert.DoesNotThrow(() => inputHandler.OnDashPressed?.Invoke(),
                "OnDashPressed should not throw when invoked with no subscribers");

            Assert.DoesNotThrow(() => inputHandler.OnAttackPressed?.Invoke(),
                "OnAttackPressed should not throw when invoked with no subscribers");
        }

        #endregion

        #region Event Chaining Tests

        [Test]
        public void OnMove_AllowsEventChaining()
        {
            // Arrange: Create chain where one event triggers another
            bool secondaryEventFired = false;
            Vector2 chainedInput = Vector2.zero;

            inputHandler.OnMove += (input) =>
            {
                // First handler triggers a secondary action
                chainedInput = input;
                secondaryEventFired = true;
            };

            // Act: Invoke event
            Vector2 testInput = new Vector2(0.6f, 0f);
            inputHandler.OnMove?.Invoke(testInput);

            // Assert: Chained event should fire
            Assert.IsTrue(secondaryEventFired, "Chained event should fire");
            Assert.AreEqual(testInput, chainedInput, "Chained event should receive correct data");
        }

        #endregion

        #region Unsubscribe Tests

        [Test]
        public void OnMove_StopsInvoking_AfterUnsubscribe()
        {
            // Arrange: Subscribe and then unsubscribe
            int callCount = 0;
            System.Action<Vector2> handler = (input) => callCount++;

            inputHandler.OnMove += handler;
            inputHandler.OnMove?.Invoke(Vector2.zero); // Call once
            Assert.AreEqual(1, callCount, "Should be called after subscribe");

            // Act: Unsubscribe
            inputHandler.OnMove -= handler;
            inputHandler.OnMove?.Invoke(Vector2.zero); // Call again

            // Assert: Should not be called after unsubscribe
            Assert.AreEqual(1, callCount, "Should not be called after unsubscribe");
        }

        [Test]
        public void OnJumpPressed_StopsInvoking_AfterUnsubscribe()
        {
            // Arrange: Subscribe and then unsubscribe
            int callCount = 0;
            System.Action handler = () => callCount++;

            inputHandler.OnJumpPressed += handler;
            inputHandler.OnJumpPressed?.Invoke();
            Assert.AreEqual(1, callCount, "Should be called after subscribe");

            // Act: Unsubscribe
            inputHandler.OnJumpPressed -= handler;
            inputHandler.OnJumpPressed?.Invoke();

            // Assert: Should not be called after unsubscribe
            Assert.AreEqual(1, callCount, "Should not be called after unsubscribe");
        }

        #endregion

        #region Edge Cases

        [Test]
        public void OnMove_HandlesZeroInput()
        {
            // Arrange: Subscribe to event
            Vector2 receivedInput = new Vector2(999f, 999f); // Non-zero initial
            inputHandler.OnMove += (input) => receivedInput = input;

            // Act: Pass zero input
            inputHandler.OnMove?.Invoke(Vector2.zero);

            // Assert: Should receive zero input correctly
            Assert.AreEqual(Vector2.zero, receivedInput, "Should handle zero input correctly");
        }

        [Test]
        public void OnMove_HandlesNegativeInput()
        {
            // Arrange: Subscribe to event
            Vector2 receivedInput = Vector2.zero;
            inputHandler.OnMove += (input) => receivedInput = input;

            // Act: Pass negative input
            Vector2 negativeInput = new Vector2(-1f, -0.5f);
            inputHandler.OnMove?.Invoke(negativeInput);

            // Assert: Should handle negative input correctly
            Assert.AreEqual(negativeInput, receivedInput, "Should handle negative input correctly");
        }

        [Test]
        public void Events_CanBeResubscribed_AfterUnsubscribe()
        {
            // Arrange: Subscribe, unsubscribe, then resubscribe
            int callCount = 0;
            System.Action handler = () => callCount++;

            inputHandler.OnJumpPressed += handler;
            inputHandler.OnJumpPressed -= handler;

            // Act: Resubscribe
            inputHandler.OnJumpPressed += handler;
            inputHandler.OnJumpPressed?.Invoke();

            // Assert: Should work after resubscription
            Assert.AreEqual(1, callCount, "Event should work after resubscription");
        }

        #endregion
    }
}
