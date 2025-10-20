using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Manages all animator parameter updates and animation state synchronization.
/// Handles missing parameter detection and safe parameter setting.
/// EXTRACTED FROM PlayerController lines 828-935
/// </summary>
[RequireComponent(typeof(Animator))]
public class PlayerAnimationController : MonoBehaviour
{
    // Component references
    private Animator animator;

    // Missing parameter tracking
    private HashSet<string> missingAnimatorParams = new HashSet<string>();
    private bool hasLoggedAnimatorWarnings = false;

    /// <summary>
    /// Initialize component references
    /// </summary>
    public void Initialize(Animator playerAnimator)
    {
        animator = playerAnimator;
    }

    /// <summary>
    /// Update all animator parameters based on current player state
    /// </summary>
    public void UpdateAnimations(bool isGrounded, bool isRunning, bool isJumping, bool isDashing,
                                 bool isAttacking, bool isDashAttacking, bool isAirAttacking,
                                 bool isClimbing, bool isWallSliding, bool isWallSticking, bool isFalling,
                                 bool onWall, float facingDirection, float horizontalInput, float verticalInput,
                                 bool facingRight, int attackCombo)
    {
        if (animator == null) return;

        SafeSetBool("IsGrounded", isGrounded);
        SafeSetBool("IsRunning", isRunning);
        SafeSetBool("IsJumping", isJumping);
        SafeSetBool("IsDashing", isDashing);
        SafeSetBool("IsAttacking", isAttacking);
        SafeSetBool("IsDashAttacking", isDashAttacking);
        SafeSetBool("IsAirAttacking", isAirAttacking);
        SafeSetBool("IsClimbing", isClimbing);
        SafeSetBool("IsWallSliding", isWallSliding);
        SafeSetBool("IsWallSticking", isWallSticking);
        SafeSetBool("IsFalling", isFalling);
        SafeSetBool("onWall", onWall); // Use onWall physics state for animator (both stick and slide)
        SafeSetFloat("FacingDirection", facingDirection);
        SafeSetFloat("HorizontalInput", horizontalInput);

        // Combined parameter for wall land animation: use same threshold as onWall logic for consistency
        bool pressingTowardWallStrong = (facingRight && horizontalInput > 0.1f) || (!facingRight && horizontalInput < -0.1f);
        SafeSetBool("PressingTowardWall", pressingTowardWallStrong);

        SafeSetFloat("VerticalInput", verticalInput);
        SafeSetInteger("AttackCombo", attackCombo);

        // Debug animator parameter updates when falling (commented out for performance)
        // if (isFalling)
        // {
        //     Debug.Log($"Animator Update - IsFalling: {isFalling}, IsGrounded: {isGrounded}, isDashing: {isDashAttacking}, velocity.y: {rb.linearVelocity.y:F2}");
        // }

        // Log missing parameters once
        if (!hasLoggedAnimatorWarnings && missingAnimatorParams.Count > 0)
        {
            hasLoggedAnimatorWarnings = true;
            Debug.LogWarning($"[PlayerAnimationController] Animator is missing the following parameters: {string.Join(", ", missingAnimatorParams)}\n" +
                "Please add these parameters to your Animator Controller or the animations may not work correctly.");
        }
    }

    /// <summary>
    /// Safely set a boolean parameter (handles missing parameters)
    /// </summary>
    public void SafeSetBool(string paramName, bool value)
    {
        if (HasAnimatorParameter(paramName))
        {
            // Debug critical animator parameters (commented out for performance)
            // if (paramName == "IsGrounded" || paramName == "IsFalling")
            // {
            //     Debug.Log($"Setting Animator: {paramName} = {value}");
            // }
            animator.SetBool(paramName, value);
        }
        else
        {
            missingAnimatorParams.Add(paramName);
        }
    }

    /// <summary>
    /// Safely set a float parameter (handles missing parameters)
    /// </summary>
    public void SafeSetFloat(string paramName, float value)
    {
        if (HasAnimatorParameter(paramName))
        {
            animator.SetFloat(paramName, value);
        }
        else
        {
            missingAnimatorParams.Add(paramName);
        }
    }

    /// <summary>
    /// Safely set an integer parameter (handles missing parameters)
    /// </summary>
    public void SafeSetInteger(string paramName, int value)
    {
        if (HasAnimatorParameter(paramName))
        {
            animator.SetInteger(paramName, value);
        }
        else
        {
            missingAnimatorParams.Add(paramName);
        }
    }

    /// <summary>
    /// Safely set a trigger parameter (handles missing parameters)
    /// </summary>
    public void SafeSetTrigger(string paramName)
    {
        if (HasAnimatorParameter(paramName))
        {
            animator.SetTrigger(paramName);
        }
        else
        {
            missingAnimatorParams.Add(paramName);
            Debug.LogWarning($"[PlayerAnimationController] Animator trigger '{paramName}' not found in Animator Controller!");
        }
    }

    /// <summary>
    /// Check if animator has a specific parameter
    /// </summary>
    private bool HasAnimatorParameter(string paramName)
    {
        if (animator == null) return false;

        foreach (var param in animator.parameters)
        {
            if (param.name == paramName)
            {
                return true;
            }
        }
        return false;
    }
}
