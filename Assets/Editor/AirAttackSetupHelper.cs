using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;

public class AirAttackSetupHelper : EditorWindow
{
    [MenuItem("Tools/Setup Air Attack Animation")]
    public static void ShowWindow()
    {
        GetWindow<AirAttackSetupHelper>("Air Attack Setup");
    }

    void OnGUI()
    {
        GUILayout.Label("Air Attack Animation Setup", EditorStyles.boldLabel);
        
        if (GUILayout.Button("Add Air Attack to Player Animator"))
        {
            SetupAirAttackAnimation();
        }
        
        GUILayout.Space(10);
        GUILayout.Label("Instructions:", EditorStyles.boldLabel);
        GUILayout.Label("1. Click the button above to add air attack state");
        GUILayout.Label("2. The script will:");
        GUILayout.Label("   - Find the PlayerAnimator controller");
        GUILayout.Label("   - Add IsAirAttacking parameter");
        GUILayout.Label("   - Create air attack state with animation");
        GUILayout.Label("   - Set up transitions from jump/fall states");
        GUILayout.Label("3. Add animation events to PlayerAirSwordSwing.anim:");
        GUILayout.Label("   - OnAirAttackAnimationStart at beginning");
        GUILayout.Label("   - OnAirAttackAnimationEnd at end");
    }

    void SetupAirAttackAnimation()
    {
        // Find the player animator controller
        string animatorPath = "Assets/Animations/PlayerAnimator.controller";
        AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(animatorPath);
        
        if (controller == null)
        {
            Debug.LogError($"Could not find PlayerAnimator.controller at {animatorPath}");
            return;
        }
        
        // Add IsAirAttacking parameter if it doesn't exist
        bool hasParameter = false;
        foreach (var param in controller.parameters)
        {
            if (param.name == "IsAirAttacking")
            {
                hasParameter = true;
                break;
            }
        }
        
        if (!hasParameter)
        {
            controller.AddParameter("IsAirAttacking", AnimatorControllerParameterType.Bool);
            Debug.Log("Added IsAirAttacking parameter");
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
        
        // Find or create the air attack state
        AnimatorState airAttackState = null;
        foreach (var state in baseLayer.stateMachine.states)
        {
            if (state.state.name == "PlayerAirSwordSwing")
            {
                airAttackState = state.state;
                break;
            }
        }
        
        if (airAttackState == null)
        {
            // Create the air attack state
            airAttackState = baseLayer.stateMachine.AddState("PlayerAirSwordSwing");
            airAttackState.tag = "Attack";
            
            // Load and assign the animation
            string animPath = "Assets/Animations/2D-Pixel-Art-Character-Template/Katana Continuous Attack/PlayerAirSwordSwing.anim";
            AnimationClip airAttackClip = AssetDatabase.LoadAssetAtPath<AnimationClip>(animPath);
            
            if (airAttackClip != null)
            {
                airAttackState.motion = airAttackClip;
                Debug.Log("Assigned PlayerAirSwordSwing animation to state");
            }
            else
            {
                Debug.LogWarning($"Could not find animation at {animPath}");
            }
        }
        
        // Find jump and fall states to create transitions from
        AnimatorState jumpState = null;
        AnimatorState fallState = null;
        AnimatorState idleState = null;
        
        foreach (var state in baseLayer.stateMachine.states)
        {
            if (state.state.name.Contains("Jump") && !state.state.name.Contains("Wall"))
            {
                jumpState = state.state;
            }
            else if (state.state.name.Contains("Fall"))
            {
                fallState = state.state;
            }
            else if (state.state.name.Contains("Idle") && !state.state.name.Contains("Combat"))
            {
                idleState = state.state;
            }
        }
        
        // Create transitions TO air attack
        if (jumpState != null)
        {
            var transition = jumpState.AddTransition(airAttackState);
            transition.AddCondition(AnimatorConditionMode.If, 0, "IsAirAttacking");
            transition.hasExitTime = false;
            transition.duration = 0.1f;
            Debug.Log("Added transition from Jump to Air Attack");
        }
        
        if (fallState != null)
        {
            var transition = fallState.AddTransition(airAttackState);
            transition.AddCondition(AnimatorConditionMode.If, 0, "IsAirAttacking");
            transition.hasExitTime = false;
            transition.duration = 0.1f;
            Debug.Log("Added transition from Fall to Air Attack");
        }
        
        // Create transitions FROM air attack
        if (fallState != null)
        {
            var transition = airAttackState.AddTransition(fallState);
            transition.AddCondition(AnimatorConditionMode.IfNot, 0, "IsAirAttacking");
            transition.AddCondition(AnimatorConditionMode.IfNot, 0, "IsGrounded");
            transition.hasExitTime = true;
            transition.exitTime = 0.9f;
            transition.duration = 0.1f;
            Debug.Log("Added transition from Air Attack to Fall");
        }
        
        if (idleState != null)
        {
            var transition = airAttackState.AddTransition(idleState);
            transition.AddCondition(AnimatorConditionMode.IfNot, 0, "IsAirAttacking");
            transition.AddCondition(AnimatorConditionMode.If, 0, "IsGrounded");
            transition.hasExitTime = true;
            transition.exitTime = 0.9f;
            transition.duration = 0.1f;
            Debug.Log("Added transition from Air Attack to Idle");
        }
        
        // Mark the controller as dirty to save changes
        EditorUtility.SetDirty(controller);
        AssetDatabase.SaveAssets();
        
        Debug.Log("Air attack setup complete! Remember to add animation events to PlayerAirSwordSwing.anim");
    }
}