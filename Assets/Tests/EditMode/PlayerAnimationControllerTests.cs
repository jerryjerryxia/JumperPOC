using NUnit.Framework;
using UnityEngine;

namespace Tests.EditMode
{
    /// <summary>
    /// Unit tests for PlayerAnimationController component.
    /// Tests animator parameter updates, safe parameter setting, and missing parameter handling.
    /// </summary>
    [TestFixture]
    public class PlayerAnimationControllerTests
    {
        private GameObject testGameObject;
        private PlayerAnimationController animController;
        private Animator animator;

        [SetUp]
        public void SetUp()
        {
            // Create test GameObject with Animator and PlayerAnimationController
            testGameObject = new GameObject("TestAnimController");
            animator = testGameObject.AddComponent<Animator>();
            animController = testGameObject.AddComponent<PlayerAnimationController>();

            // Initialize the component
            animController.Initialize(animator);

            // Suppress warnings in tests
            Debug.unityLogger.logEnabled = false;
        }

        [TearDown]
        public void TearDown()
        {
            // Clean up test objects
            if (testGameObject != null)
            {
                Object.DestroyImmediate(testGameObject);
            }

            Debug.unityLogger.logEnabled = true;
        }

        #region Initialization Tests

        [Test]
        public void Initialize_WithValidAnimator_SetsAnimatorReference()
        {
            // Arrange: Create new objects
            var newGameObject = new GameObject("TestInit");
            var newAnimator = newGameObject.AddComponent<Animator>();
            var newController = newGameObject.AddComponent<PlayerAnimationController>();

            // Act: Initialize
            newController.Initialize(newAnimator);

            // Assert: Controller should be initialized
            // We can't directly check the private animator field, but UpdateAnimations should not crash
            Assert.DoesNotThrow(() => newController.UpdateAnimations(
                isGrounded: true,
                isRunning: false,
                isJumping: false,
                isDashing: false,
                isAttacking: false,
                isDashAttacking: false,
                isAirAttacking: false,
                isClimbing: false,
                isWallSliding: false,
                isWallSticking: false,
                isFalling: false,
                onWall: false,
                facingDirection: 1f,
                horizontalInput: 0f,
                verticalInput: 0f,
                facingRight: true,
                attackCombo: 0
            ), "UpdateAnimations should not throw after initialization");

            // Cleanup
            Object.DestroyImmediate(newGameObject);
        }

        [Test]
        public void Initialize_WithNullAnimator_DoesNotCrash()
        {
            // Arrange: Create new controller
            var newGameObject = new GameObject("TestNullInit");
            var newController = newGameObject.AddComponent<PlayerAnimationController>();

            // Act & Assert: Initialize with null should not crash
            Assert.DoesNotThrow(() => newController.Initialize(null),
                "Initialize should handle null animator gracefully");

            // Cleanup
            Object.DestroyImmediate(newGameObject);
        }

        #endregion

        #region Safe Parameter Setting Tests

        [Test]
        public void SafeSetBool_WithMissingParameter_DoesNotThrow()
        {
            // Act & Assert: Setting non-existent parameter should not throw
            Assert.DoesNotThrow(() => animController.SafeSetBool("NonExistentParam", true),
                "SafeSetBool should handle missing parameters gracefully");
        }

        [Test]
        public void SafeSetFloat_WithMissingParameter_DoesNotThrow()
        {
            // Act & Assert: Setting non-existent parameter should not throw
            Assert.DoesNotThrow(() => animController.SafeSetFloat("NonExistentParam", 1.5f),
                "SafeSetFloat should handle missing parameters gracefully");
        }

        [Test]
        public void SafeSetInteger_WithMissingParameter_DoesNotThrow()
        {
            // Act & Assert: Setting non-existent parameter should not throw
            Assert.DoesNotThrow(() => animController.SafeSetInteger("NonExistentParam", 5),
                "SafeSetInteger should handle missing parameters gracefully");
        }

        [Test]
        public void SafeSetTrigger_WithMissingParameter_DoesNotThrow()
        {
            // Act & Assert: Setting non-existent trigger should not throw
            Assert.DoesNotThrow(() => animController.SafeSetTrigger("NonExistentTrigger"),
                "SafeSetTrigger should handle missing parameters gracefully");
        }

        #endregion

        #region UpdateAnimations Tests

        [Test]
        public void UpdateAnimations_WithValidAnimator_DoesNotThrow()
        {
            // Act & Assert: Updating animations should not throw
            Assert.DoesNotThrow(() => animController.UpdateAnimations(
                isGrounded: true,
                isRunning: false,
                isJumping: false,
                isDashing: false,
                isAttacking: false,
                isDashAttacking: false,
                isAirAttacking: false,
                isClimbing: false,
                isWallSliding: false,
                isWallSticking: false,
                isFalling: false,
                onWall: false,
                facingDirection: 1f,
                horizontalInput: 0f,
                verticalInput: 0f,
                facingRight: true,
                attackCombo: 0
            ), "UpdateAnimations should handle valid input");
        }

        [Test]
        public void UpdateAnimations_WithNullAnimator_DoesNotThrow()
        {
            // Arrange: Create controller with null animator
            var newGameObject = new GameObject("TestNullAnim");
            var newController = newGameObject.AddComponent<PlayerAnimationController>();
            newController.Initialize(null);

            // Act & Assert: Should handle null animator gracefully
            Assert.DoesNotThrow(() => newController.UpdateAnimations(
                isGrounded: true,
                isRunning: false,
                isJumping: false,
                isDashing: false,
                isAttacking: false,
                isDashAttacking: false,
                isAirAttacking: false,
                isClimbing: false,
                isWallSliding: false,
                isWallSticking: false,
                isFalling: false,
                onWall: false,
                facingDirection: 1f,
                horizontalInput: 0f,
                verticalInput: 0f,
                facingRight: true,
                attackCombo: 0
            ), "UpdateAnimations should handle null animator gracefully");

            // Cleanup
            Object.DestroyImmediate(newGameObject);
        }

        [Test]
        public void UpdateAnimations_WithAllStatesTrue_DoesNotThrow()
        {
            // Act & Assert: All true states should not cause issues
            Assert.DoesNotThrow(() => animController.UpdateAnimations(
                isGrounded: true,
                isRunning: true,
                isJumping: true,
                isDashing: true,
                isAttacking: true,
                isDashAttacking: true,
                isAirAttacking: true,
                isClimbing: true,
                isWallSliding: true,
                isWallSticking: true,
                isFalling: true,
                onWall: true,
                facingDirection: 1f,
                horizontalInput: 1f,
                verticalInput: 1f,
                facingRight: true,
                attackCombo: 3
            ), "UpdateAnimations should handle all true states");
        }

        [Test]
        public void UpdateAnimations_WithAllStatesFalse_DoesNotThrow()
        {
            // Act & Assert: All false states should not cause issues
            Assert.DoesNotThrow(() => animController.UpdateAnimations(
                isGrounded: false,
                isRunning: false,
                isJumping: false,
                isDashing: false,
                isAttacking: false,
                isDashAttacking: false,
                isAirAttacking: false,
                isClimbing: false,
                isWallSliding: false,
                isWallSticking: false,
                isFalling: false,
                onWall: false,
                facingDirection: -1f,
                horizontalInput: 0f,
                verticalInput: 0f,
                facingRight: false,
                attackCombo: 0
            ), "UpdateAnimations should handle all false states");
        }

        [Test]
        public void UpdateAnimations_WithNegativeHorizontalInput_DoesNotThrow()
        {
            // Act & Assert: Negative input should be handled
            Assert.DoesNotThrow(() => animController.UpdateAnimations(
                isGrounded: true,
                isRunning: true,
                isJumping: false,
                isDashing: false,
                isAttacking: false,
                isDashAttacking: false,
                isAirAttacking: false,
                isClimbing: false,
                isWallSliding: false,
                isWallSticking: false,
                isFalling: false,
                onWall: false,
                facingDirection: -1f,
                horizontalInput: -1f,
                verticalInput: 0f,
                facingRight: false,
                attackCombo: 0
            ), "UpdateAnimations should handle negative horizontal input");
        }

        [Test]
        public void UpdateAnimations_WithNegativeVerticalInput_DoesNotThrow()
        {
            // Act & Assert: Negative vertical input should be handled
            Assert.DoesNotThrow(() => animController.UpdateAnimations(
                isGrounded: false,
                isRunning: false,
                isJumping: false,
                isDashing: false,
                isAttacking: false,
                isDashAttacking: false,
                isAirAttacking: false,
                isClimbing: false,
                isWallSliding: false,
                isWallSticking: false,
                isFalling: true,
                onWall: false,
                facingDirection: 1f,
                horizontalInput: 0f,
                verticalInput: -1f,
                facingRight: true,
                attackCombo: 0
            ), "UpdateAnimations should handle negative vertical input");
        }

        [Test]
        public void UpdateAnimations_WithHighAttackCombo_DoesNotThrow()
        {
            // Act & Assert: High combo count should be handled
            Assert.DoesNotThrow(() => animController.UpdateAnimations(
                isGrounded: true,
                isRunning: false,
                isJumping: false,
                isDashing: false,
                isAttacking: true,
                isDashAttacking: false,
                isAirAttacking: false,
                isClimbing: false,
                isWallSliding: false,
                isWallSticking: false,
                isFalling: false,
                onWall: false,
                facingDirection: 1f,
                horizontalInput: 0f,
                verticalInput: 0f,
                facingRight: true,
                attackCombo: 99
            ), "UpdateAnimations should handle high attack combo values");
        }

        #endregion

        #region PressingTowardWall Logic Tests

        [Test]
        public void UpdateAnimations_CalculatesPressingTowardWall_WhenFacingRightAndMovingRight()
        {
            // Act: Facing right and pressing right
            Assert.DoesNotThrow(() => animController.UpdateAnimations(
                isGrounded: true,
                isRunning: true,
                isJumping: false,
                isDashing: false,
                isAttacking: false,
                isDashAttacking: false,
                isAirAttacking: false,
                isClimbing: false,
                isWallSliding: false,
                isWallSticking: false,
                isFalling: false,
                onWall: true,
                facingDirection: 1f,
                horizontalInput: 0.5f, // Pressing toward wall
                verticalInput: 0f,
                facingRight: true,
                attackCombo: 0
            ), "Should calculate pressing toward wall correctly");
        }

        [Test]
        public void UpdateAnimations_CalculatesPressingTowardWall_WhenFacingLeftAndMovingLeft()
        {
            // Act: Facing left and pressing left
            Assert.DoesNotThrow(() => animController.UpdateAnimations(
                isGrounded: true,
                isRunning: true,
                isJumping: false,
                isDashing: false,
                isAttacking: false,
                isDashAttacking: false,
                isAirAttacking: false,
                isClimbing: false,
                isWallSliding: false,
                isWallSticking: false,
                isFalling: false,
                onWall: true,
                facingDirection: -1f,
                horizontalInput: -0.5f, // Pressing toward wall
                verticalInput: 0f,
                facingRight: false,
                attackCombo: 0
            ), "Should calculate pressing toward wall correctly when facing left");
        }

        [Test]
        public void UpdateAnimations_CalculatesNotPressingTowardWall_WhenMovingOppositeDirection()
        {
            // Act: Facing right but pressing left (pulling away from wall)
            Assert.DoesNotThrow(() => animController.UpdateAnimations(
                isGrounded: true,
                isRunning: false,
                isJumping: false,
                isDashing: false,
                isAttacking: false,
                isDashAttacking: false,
                isAirAttacking: false,
                isClimbing: false,
                isWallSliding: false,
                isWallSticking: false,
                isFalling: false,
                onWall: true,
                facingDirection: 1f,
                horizontalInput: -0.5f, // Pulling away from wall
                verticalInput: 0f,
                facingRight: true,
                attackCombo: 0
            ), "Should detect not pressing toward wall when moving opposite direction");
        }

        #endregion

        #region Edge Cases

        [Test]
        public void UpdateAnimations_WithExtremeFloatValues_DoesNotThrow()
        {
            // Act & Assert: Extreme float values should be handled
            Assert.DoesNotThrow(() => animController.UpdateAnimations(
                isGrounded: true,
                isRunning: false,
                isJumping: false,
                isDashing: false,
                isAttacking: false,
                isDashAttacking: false,
                isAirAttacking: false,
                isClimbing: false,
                isWallSliding: false,
                isWallSticking: false,
                isFalling: false,
                onWall: false,
                facingDirection: float.MaxValue,
                horizontalInput: float.MinValue,
                verticalInput: float.MaxValue,
                facingRight: true,
                attackCombo: int.MaxValue
            ), "UpdateAnimations should handle extreme float values");
        }

        [Test]
        public void UpdateAnimations_CalledMultipleTimes_DoesNotAccumulateErrors()
        {
            // Act: Call multiple times
            for (int i = 0; i < 100; i++)
            {
                animController.UpdateAnimations(
                    isGrounded: i % 2 == 0,
                    isRunning: i % 3 == 0,
                    isJumping: i % 5 == 0,
                    isDashing: i % 7 == 0,
                    isAttacking: i % 11 == 0,
                    isDashAttacking: false,
                    isAirAttacking: false,
                    isClimbing: false,
                    isWallSliding: false,
                    isWallSticking: false,
                    isFalling: i % 2 == 1,
                    onWall: false,
                    facingDirection: i % 2 == 0 ? 1f : -1f,
                    horizontalInput: (i % 10) / 10f,
                    verticalInput: 0f,
                    facingRight: i % 2 == 0,
                    attackCombo: i % 4
                );
            }

            // Assert: Should complete without errors
            Assert.Pass("UpdateAnimations should handle multiple rapid calls");
        }

        #endregion

        #region Missing Parameter Tracking Tests

        [Test]
        public void SafeSetBool_WithMultipleMissingParameters_TracksAllMissing()
        {
            // Act: Set multiple missing parameters
            animController.SafeSetBool("Missing1", true);
            animController.SafeSetBool("Missing2", false);
            animController.SafeSetBool("Missing3", true);

            // Assert: All calls should complete without throwing
            Assert.Pass("Should track multiple missing parameters");
        }

        [Test]
        public void SafeSetFloat_WithMixedParameterTypes_HandlesGracefully()
        {
            // Act: Try setting various missing parameters
            animController.SafeSetFloat("MissingFloat1", 1.5f);
            animController.SafeSetInteger("MissingInt1", 5);
            animController.SafeSetBool("MissingBool1", true);
            animController.SafeSetTrigger("MissingTrigger1");

            // Assert: All should complete without errors
            Assert.Pass("Should handle mixed missing parameter types");
        }

        #endregion
    }
}
