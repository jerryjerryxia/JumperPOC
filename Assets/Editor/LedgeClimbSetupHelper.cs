using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;

public class LedgeClimbSetupHelper : EditorWindow
{
    [MenuItem("Tools/Setup Ledge Climb Animation")]
    public static void ShowWindow()
    {
        GetWindow<LedgeClimbSetupHelper>("Ledge Climb Setup");
    }

    void OnGUI()
    {
        GUILayout.Label("Ledge Climb Animation Setup", EditorStyles.boldLabel);
        
        if (GUILayout.Button("Add Ledge Climb to Player Animator"))
        {
            SetupLedgeClimbAnimation();
        }
        
        GUILayout.Space(10);
        GUILayout.Label("Instructions:", EditorStyles.boldLabel);
        GUILayout.Label("1. Click the button above to add ledge climb state");
        GUILayout.Label("2. The script will:");
        GUILayout.Label("   - Find the PlayerAnimator controller");
        GUILayout.Label("   - Add IsPerformingLedgeClimb parameter");
        GUILayout.Label("   - Create ledge climb state with animation");
        GUILayout.Label("   - Set up transitions from wall slide states");
        GUILayout.Label("3. Add animation event to PlayerLedgeClimb.anim:");
        GUILayout.Label("   - OnLedgeClimbAnimationEnd at end of animation");
        GUILayout.Label("4. Adjust ledge climb parameters in PlayerController:");
        GUILayout.Label("   - ledgeClimbDuration (0.8s recommended)");
        GUILayout.Label("   - ledgeClimbUpDistance (0.6f recommended)");
        GUILayout.Label("   - ledgeClimbForwardDistance (1.2f recommended)");
        GUILayout.Label("   - ledgeDetectionOffset (0.1f recommended)");
    }

    void SetupLedgeClimbAnimation()
    {
        // Find the player animator controller
        string animatorPath = "Assets/Animations/PlayerAnimator.controller";
        AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(animatorPath);
        
        if (controller == null)
        {
            Debug.LogError($"Could not find PlayerAnimator.controller at {animatorPath}");
            return;
        }
        
        // Add IsPerformingLedgeClimb parameter if it doesn't exist
        bool hasPerformingParameter = false;
        foreach (var param in controller.parameters)
        {
            if (param.name == "IsPerformingLedgeClimb")
            {
                hasPerformingParameter = true;
                break;
            }
        }
        
        if (!hasPerformingParameter)
        {
            controller.AddParameter("IsPerformingLedgeClimb", AnimatorControllerParameterType.Bool);
            Debug.Log("Added IsPerformingLedgeClimb parameter");
        }
        
        // Remove the redundant IsLedgeClimbing parameter - we only need IsPerformingLedgeClimb
        
        // Add LedgeClimb trigger if it doesn't exist
        bool hasTrigger = false;
        foreach (var param in controller.parameters)
        {
            if (param.name == "LedgeClimb")
            {
                hasTrigger = true;
                break;
            }
        }
        
        if (!hasTrigger)
        {
            controller.AddParameter("LedgeClimb", AnimatorControllerParameterType.Trigger);
            Debug.Log("Added LedgeClimb trigger parameter");
        }
        
        // Find the base layer
        AnimatorControllerLayer baseLayer = null;
        foreach (var layer in controller.layers)
        {
            if (layer.name == "Base Layer")
            {
                baseLayer = layer;
                break;
            }
        }
        
        if (baseLayer == null)
        {
            Debug.LogError("Could not find Base Layer in animator");
            return;
        }
        
        // Find or create the ledge climb state
        AnimatorState ledgeClimbState = null;
        foreach (var state in baseLayer.stateMachine.states)
        {
            if (state.state.name == "PlayerLedgeClimb")
            {
                ledgeClimbState = state.state;
                break;
            }
        }
        
        if (ledgeClimbState == null)
        {
            // Create the ledge climb state
            ledgeClimbState = baseLayer.stateMachine.AddState("PlayerLedgeClimb");
            ledgeClimbState.tag = "LedgeClimb";
            
            // Load and assign the animation
            string animPath = "Assets/Animations/2D-Pixel-Art-Character-Template/Ledge Grab-Climb/PlayerLedgeClimb.anim";
            AnimationClip ledgeClimbClip = AssetDatabase.LoadAssetAtPath<AnimationClip>(animPath);
            
            if (ledgeClimbClip != null)
            {
                ledgeClimbState.motion = ledgeClimbClip;
                Debug.Log("Assigned PlayerLedgeClimb animation to state");
            }
            else
            {
                Debug.LogWarning($"Could not find animation at {animPath}");
            }
        }
        
        // Find wall slide and wall jump states to create transitions from
        AnimatorState wallSlideState = null;
        AnimatorState jumpState = null;
        AnimatorState idleState = null;
        
        foreach (var state in baseLayer.stateMachine.states)
        {
            if (state.state.name.Contains("WallSlide"))
            {
                wallSlideState = state.state;
            }
            else if (state.state.name.Contains("Jump") && !state.state.name.Contains("Wall"))
            {
                jumpState = state.state;
            }
            else if (state.state.name.Contains("Idle") && !state.state.name.Contains("Combat"))
            {
                idleState = state.state;
            }
        }
        
        // Create transitions TO ledge climb from wall slide
        if (wallSlideState != null)
        {
            var transition = wallSlideState.AddTransition(ledgeClimbState);
            transition.AddCondition(AnimatorConditionMode.If, 0, "IsPerformingLedgeClimb");
            transition.hasExitTime = false;
            transition.duration = 0.1f;
            Debug.Log("Added transition from Wall Slide to Ledge Climb");
        }
        
        // Create transitions TO ledge climb from jump (for wall jump scenarios)
        if (jumpState != null)
        {
            var transition = jumpState.AddTransition(ledgeClimbState);
            transition.AddCondition(AnimatorConditionMode.If, 0, "IsPerformingLedgeClimb");
            transition.hasExitTime = false;
            transition.duration = 0.1f;
            Debug.Log("Added transition from Jump to Ledge Climb");
        }
        
        // Create transitions FROM ledge climb to idle
        if (idleState != null)
        {
            var transition = ledgeClimbState.AddTransition(idleState);
            transition.AddCondition(AnimatorConditionMode.IfNot, 0, "IsPerformingLedgeClimb");
            transition.hasExitTime = true;
            transition.exitTime = 0.9f;
            transition.duration = 0.1f;
            Debug.Log("Added transition from Ledge Climb to Idle");
        }
        
        // Mark the controller as dirty to save changes
        EditorUtility.SetDirty(controller);
        AssetDatabase.SaveAssets();
        
        Debug.Log("Ledge climb setup complete! Remember to add animation event to PlayerLedgeClimb.anim");
        Debug.Log("Animation event: OnLedgeClimbAnimationEnd at the end of the animation");
    }
}