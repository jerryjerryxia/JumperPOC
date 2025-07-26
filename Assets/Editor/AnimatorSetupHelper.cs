using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
using System.Collections.Generic;

public class AnimatorSetupHelper : EditorWindow
{
    private AnimatorController animatorController;
    private Vector2 scrollPosition;
    
    // Animation state names
    private readonly string[] stateNames = {
        "DaggerIdle",
        "DaggerRun", 
        "PlayerJump",
        "PlayerLand",
        "LedgeGrab",
        "PlayerAirSpin",
        "PlayerClimb-back",
        "PlayerSide-climb",
        "PlayerSwordAtk64x64",
        "PlayerSwordStab96x48",
        "PlayerSwordSwing3",
        "Dash",
        "PlayerWallLand",
        "PlayerWallLand(left)",
        "PlayerWallSlide",
        "PlayerWallSlide(left)"
    };

    // Parameters we'll need
    private readonly string[] parameterNames = {
        "IsGrounded",
        "IsMoving",
        "IsJumping",
        "IsAttacking",
        "IsDashing",
        "IsClimbing",
        "IsWallSliding",
        "IsLedgeGrabbing",
        "AttackCombo",
        "FacingDirection", // 1 for right, -1 for left
        "HorizontalInput",
        "VerticalInput"
    };

    [MenuItem("Tools/Animator Setup Helper")]
    public static void ShowWindow()
    {
        GetWindow<AnimatorSetupHelper>("Animator Setup Helper");
    }

    private void OnGUI()
    {
        GUILayout.Label("Player Animator Setup Helper", EditorStyles.boldLabel);
        
        // Animator Controller Selection
        animatorController = (AnimatorController)EditorGUILayout.ObjectField(
            "Animator Controller", 
            animatorController, 
            typeof(AnimatorController), 
            false
        );

        if (animatorController == null)
        {
            EditorGUILayout.HelpBox("Please select the PlayerAnimator controller", MessageType.Warning);
            return;
        }

        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

        GUILayout.Space(10);
        
        // Setup Parameters
        if (GUILayout.Button("Setup Parameters", GUILayout.Height(30)))
        {
            SetupParameters();
        }

        GUILayout.Space(10);

        // Setup Basic State Machine
        if (GUILayout.Button("Setup Basic State Machine", GUILayout.Height(30)))
        {
            SetupBasicStateMachine();
        }

        GUILayout.Space(10);

        // Setup Advanced State Machine with Sub-State Machines
        if (GUILayout.Button("Setup Advanced State Machine", GUILayout.Height(30)))
        {
            SetupAdvancedStateMachine();
        }

        GUILayout.Space(10);

        // Setup All Transitions
        if (GUILayout.Button("Setup All Transitions", GUILayout.Height(30)))
        {
            SetupAllTransitions();
        }

        GUILayout.Space(10);

        // Clear All Transitions
        if (GUILayout.Button("Clear All Transitions", GUILayout.Height(30)))
        {
            ClearAllTransitions();
        }

        GUILayout.Space(10);

        // Reset to Default State
        if (GUILayout.Button("Reset to Default State", GUILayout.Height(30)))
        {
            ResetToDefaultState();
        }

        GUILayout.Space(20);

        // Information
        EditorGUILayout.HelpBox(
            "This tool will help you set up your player animator controller with proper states and transitions.\n\n" +
            "1. First, setup parameters\n" +
            "2. Then choose either basic or advanced state machine setup\n" +
            "3. Finally, setup all transitions\n\n" +
            "The advanced setup creates sub-state machines for better organization.",
            MessageType.Info
        );

        EditorGUILayout.EndScrollView();
    }

    private void SetupParameters()
    {
        if (animatorController == null) return;

        foreach (string paramName in parameterNames)
        {
            // Check if parameter already exists
            bool exists = false;
            foreach (var param in animatorController.parameters)
            {
                if (param.name == paramName)
                {
                    exists = true;
                    break;
                }
            }

            if (!exists)
            {
                AnimatorControllerParameter newParam = new AnimatorControllerParameter();
                newParam.name = paramName;
                
                // Set parameter type based on name
                if (paramName == "FacingDirection" || paramName == "HorizontalInput" || paramName == "VerticalInput")
                {
                    newParam.type = AnimatorControllerParameterType.Float;
                }
                else if (paramName == "AttackCombo")
                {
                    newParam.type = AnimatorControllerParameterType.Int;
                }
                else
                {
                    newParam.type = AnimatorControllerParameterType.Bool;
                }

                animatorController.AddParameter(newParam);
                Debug.Log($"Added parameter: {paramName}");
            }
        }

        EditorUtility.SetDirty(animatorController);
        AssetDatabase.SaveAssets();
        Debug.Log("Parameters setup complete!");
    }

    private void SetupBasicStateMachine()
    {
        if (animatorController == null) return;

        var rootStateMachine = animatorController.layers[0].stateMachine;
        
        // Clear existing states and sub-state machines
        ClearStateMachine(rootStateMachine);
        
        // Add all states
        foreach (string stateName in stateNames)
        {
            var clip = FindAnimationClip(stateName);
            if (clip != null)
            {
                var state = rootStateMachine.AddState(stateName);
                state.motion = clip;
                Debug.Log($"Added state: {stateName}");
            }
            else
            {
                Debug.LogWarning($"Could not find animation clip for: {stateName}");
            }
        }

        // Set default state
        if (rootStateMachine.states.Length > 0)
        {
            rootStateMachine.defaultState = rootStateMachine.states[0].state;
        }

        EditorUtility.SetDirty(animatorController);
        AssetDatabase.SaveAssets();
        Debug.Log("Basic state machine setup complete!");
    }

    private void ClearStateMachine(AnimatorStateMachine stateMachine)
    {
        // Remove all states
        while (stateMachine.states.Length > 0)
        {
            stateMachine.RemoveState(stateMachine.states[0].state);
        }
        
        // Remove all sub-state machines
        while (stateMachine.stateMachines.Length > 0)
        {
            stateMachine.RemoveStateMachine(stateMachine.stateMachines[0].stateMachine);
        }
        
        // Clear transitions
        stateMachine.anyStateTransitions = new AnimatorStateTransition[0];
        stateMachine.entryTransitions = new AnimatorTransition[0];
    }

    private void SetupAdvancedStateMachine()
    {
        if (animatorController == null) return;

        var rootStateMachine = animatorController.layers[0].stateMachine;
        
        // Clear existing states and sub-state machines
        ClearStateMachine(rootStateMachine);

        // Create sub-state machines
        var locomotionSM = rootStateMachine.AddStateMachine("Locomotion");
        var combatSM = rootStateMachine.AddStateMachine("Combat");
        var wallInteractionSM = rootStateMachine.AddStateMachine("WallInteraction");
        var ledgeInteractionSM = rootStateMachine.AddStateMachine("LedgeInteraction");

        // Add states to appropriate sub-state machines
        AddStateToSubMachine(locomotionSM, "DaggerIdle");
        AddStateToSubMachine(locomotionSM, "DaggerRun");
        AddStateToSubMachine(locomotionSM, "PlayerJump");
        AddStateToSubMachine(locomotionSM, "PlayerLand");
        AddStateToSubMachine(locomotionSM, "PlayerAirSpin");
        AddStateToSubMachine(locomotionSM, "Dash");

        AddStateToSubMachine(combatSM, "PlayerSwordAtk64x64");
        AddStateToSubMachine(combatSM, "PlayerSwordStab96x48");

        AddStateToSubMachine(wallInteractionSM, "PlayerWallSlide");
        AddStateToSubMachine(wallInteractionSM, "PlayerWallSlide(left)");
        AddStateToSubMachine(wallInteractionSM, "PlayerWallLand");
        AddStateToSubMachine(wallInteractionSM, "PlayerWallLand(left)");

        AddStateToSubMachine(ledgeInteractionSM, "LedgeGrab");
        AddStateToSubMachine(ledgeInteractionSM, "PlayerClimb-back");
        AddStateToSubMachine(ledgeInteractionSM, "PlayerSide-climb");

        // Set default states
        if (locomotionSM.states.Length > 0)
            locomotionSM.defaultState = locomotionSM.states[0].state;
        if (combatSM.states.Length > 0)
            combatSM.defaultState = combatSM.states[0].state;
        if (wallInteractionSM.states.Length > 0)
            wallInteractionSM.defaultState = wallInteractionSM.states[0].state;
        if (ledgeInteractionSM.states.Length > 0)
            ledgeInteractionSM.defaultState = ledgeInteractionSM.states[0].state;

        EditorUtility.SetDirty(animatorController);
        AssetDatabase.SaveAssets();
        Debug.Log("Advanced state machine setup complete!");
    }

    private void AddStateToSubMachine(AnimatorStateMachine subMachine, string stateName)
    {
        var clip = FindAnimationClip(stateName);
        if (clip != null)
        {
            var state = subMachine.AddState(stateName);
            state.motion = clip;
            Debug.Log($"Added state {stateName} to {subMachine.name}");
        }
        else
        {
            Debug.LogWarning($"Could not find animation clip for: {stateName}");
        }
    }

    private void SetupAllTransitions()
    {
        if (animatorController == null) return;

        var rootStateMachine = animatorController.layers[0].stateMachine;
        
        // Clear existing transitions
        ClearAllTransitions();

        // Setup basic locomotion transitions
        SetupLocomotionTransitions(rootStateMachine);
        
        // Setup combat transitions
        SetupCombatTransitions(rootStateMachine);
        
        // Setup wall interaction transitions
        SetupWallInteractionTransitions(rootStateMachine);
        
        // Setup ledge interaction transitions
        SetupLedgeInteractionTransitions(rootStateMachine);

        EditorUtility.SetDirty(animatorController);
        AssetDatabase.SaveAssets();
        Debug.Log("All transitions setup complete!");
    }

    private void SetupLocomotionTransitions(AnimatorStateMachine stateMachine)
    {
        // Idle transitions
        AddTransition(stateMachine, "DaggerIdle", "DaggerRun", "IsMoving", true);
        AddTransition(stateMachine, "DaggerIdle", "PlayerJump", "IsJumping", true);
        AddTransition(stateMachine, "DaggerIdle", "Dash", "IsDashing", true);
        AddTransition(stateMachine, "DaggerIdle", "PlayerSwordAtk64x64", "IsAttacking", true);

        // Run transitions
        AddTransition(stateMachine, "DaggerRun", "DaggerIdle", "IsMoving", false);
        AddTransition(stateMachine, "DaggerRun", "PlayerJump", "IsJumping", true);
        AddTransition(stateMachine, "DaggerRun", "Dash", "IsDashing", true);
        AddTransition(stateMachine, "DaggerRun", "PlayerSwordAtk64x64", "IsAttacking", true);

        // Jump transitions
        AddTransition(stateMachine, "PlayerJump", "PlayerLand", "IsGrounded", true);
        AddTransition(stateMachine, "PlayerJump", "PlayerAirSpin", "IsAttacking", true);
        AddTransition(stateMachine, "PlayerJump", "PlayerWallSlide", "IsWallSliding", true);

        // Land transitions
        AddTransition(stateMachine, "PlayerLand", "DaggerIdle", "IsGrounded", true);
        AddTransition(stateMachine, "PlayerLand", "DaggerRun", "IsMoving", true);

        // Air Spin transitions
        AddTransition(stateMachine, "PlayerAirSpin", "PlayerLand", "IsGrounded", true);
        AddTransition(stateMachine, "PlayerAirSpin", "PlayerJump", "IsJumping", true);

        // Dash transitions
        AddTransition(stateMachine, "Dash", "DaggerIdle", "IsDashing", false);
        AddTransition(stateMachine, "Dash", "DaggerRun", "IsMoving", true);
    }

    private void SetupCombatTransitions(AnimatorStateMachine stateMachine)
    {
        // Attack transitions - 3-hit combo
        AddTransition(stateMachine, "PlayerSwordAtk64x64", "PlayerSwordStab96x48", "AttackCombo", 1);
        AddTransition(stateMachine, "PlayerSwordAtk64x64", "DaggerIdle", "IsAttacking", false);
        AddTransition(stateMachine, "PlayerSwordAtk64x64", "DaggerRun", "IsMoving", true);

        AddTransition(stateMachine, "PlayerSwordStab96x48", "PlayerSwordSwing3", "AttackCombo", 2);
        AddTransition(stateMachine, "PlayerSwordStab96x48", "DaggerIdle", "IsAttacking", false);
        AddTransition(stateMachine, "PlayerSwordStab96x48", "DaggerRun", "IsMoving", true);

        AddTransition(stateMachine, "PlayerSwordSwing3", "DaggerIdle", "IsAttacking", false);
        AddTransition(stateMachine, "PlayerSwordSwing3", "DaggerRun", "IsMoving", true);
    }

    private void SetupWallInteractionTransitions(AnimatorStateMachine stateMachine)
    {
        // Wall Slide transitions
        AddTransition(stateMachine, "PlayerWallSlide", "PlayerWallLand", "IsGrounded", true);
        AddTransition(stateMachine, "PlayerWallSlide", "PlayerJump", "IsJumping", true);
        AddTransition(stateMachine, "PlayerWallSlide", "PlayerWallSlide(left)", "FacingDirection", -1f);

        AddTransition(stateMachine, "PlayerWallSlide(left)", "PlayerWallLand(left)", "IsGrounded", true);
        AddTransition(stateMachine, "PlayerWallSlide(left)", "PlayerJump", "IsJumping", true);
        AddTransition(stateMachine, "PlayerWallSlide(left)", "PlayerWallSlide", "FacingDirection", 1f);

        // Wall Land transitions
        AddTransition(stateMachine, "PlayerWallLand", "DaggerIdle", "IsGrounded", true);
        AddTransition(stateMachine, "PlayerWallLand", "DaggerRun", "IsMoving", true);

        AddTransition(stateMachine, "PlayerWallLand(left)", "DaggerIdle", "IsGrounded", true);
        AddTransition(stateMachine, "PlayerWallLand(left)", "DaggerRun", "IsMoving", true);
    }

    private void SetupLedgeInteractionTransitions(AnimatorStateMachine stateMachine)
    {
        // Ledge Grab transitions
        AddTransition(stateMachine, "LedgeGrab", "PlayerClimb-back", "VerticalInput", 1f);
        AddTransition(stateMachine, "LedgeGrab", "PlayerSide-climb", "HorizontalInput", 1f);
        AddTransition(stateMachine, "LedgeGrab", "PlayerJump", "IsJumping", true);

        // Climb transitions
        AddTransition(stateMachine, "PlayerClimb-back", "DaggerIdle", "IsClimbing", false);
        AddTransition(stateMachine, "PlayerSide-climb", "DaggerIdle", "IsClimbing", false);
    }

    private void AddTransition(AnimatorStateMachine stateMachine, string fromState, string toState, string parameter, object value)
    {
        var from = FindState(stateMachine, fromState);
        var to = FindState(stateMachine, toState);

        if (from != null && to != null)
        {
            var transition = from.AddTransition(to);
            
            if (value is bool boolValue)
            {
                transition.AddCondition(AnimatorConditionMode.If, boolValue ? 1 : 0, parameter);
            }
            else if (value is int intValue)
            {
                transition.AddCondition(AnimatorConditionMode.Equals, intValue, parameter);
            }
            else if (value is float floatValue)
            {
                transition.AddCondition(AnimatorConditionMode.Greater, floatValue, parameter);
            }

            Debug.Log($"Added transition: {fromState} -> {toState} (when {parameter} = {value})");
        }
    }

    private AnimatorState FindState(AnimatorStateMachine stateMachine, string stateName)
    {
        foreach (var childState in stateMachine.states)
        {
            if (childState.state.name == stateName)
                return childState.state;
        }

        // Check sub-state machines
        foreach (var childSM in stateMachine.stateMachines)
        {
            var state = FindState(childSM.stateMachine, stateName);
            if (state != null)
                return state;
        }

        return null;
    }

    private AnimationClip FindAnimationClip(string clipName)
    {
        string[] guids = AssetDatabase.FindAssets($"t:AnimationClip {clipName}");
        
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
            if (clip != null && clip.name == clipName)
            {
                return clip;
            }
        }

        return null;
    }

    private void ClearAllTransitions()
    {
        if (animatorController == null) return;

        var rootStateMachine = animatorController.layers[0].stateMachine;
        ClearTransitionsRecursive(rootStateMachine);
        
        EditorUtility.SetDirty(animatorController);
        AssetDatabase.SaveAssets();
        Debug.Log("All transitions cleared!");
    }

    private void ClearTransitionsRecursive(AnimatorStateMachine stateMachine)
    {
        // Clear transitions from states
        foreach (var childState in stateMachine.states)
        {
            childState.state.transitions = new AnimatorStateTransition[0];
        }

        // Clear transitions from sub-state machines
        foreach (var childSM in stateMachine.stateMachines)
        {
            ClearTransitionsRecursive(childSM.stateMachine);
        }

        // Clear transitions from the state machine itself
        stateMachine.anyStateTransitions = new AnimatorStateTransition[0];
        stateMachine.entryTransitions = new AnimatorTransition[0];
    }

    private void ResetToDefaultState()
    {
        if (animatorController == null) return;

        var rootStateMachine = animatorController.layers[0].stateMachine;
        
        // Clear everything
        ClearStateMachine(rootStateMachine);
        
        // Add just the idle state as default
        var idleClip = FindAnimationClip("DaggerIdle");
        if (idleClip != null)
        {
            var idleState = rootStateMachine.AddState("DaggerIdle");
            idleState.motion = idleClip;
            rootStateMachine.defaultState = idleState;
            Debug.Log("Reset to default state (DaggerIdle)");
        }
        else
        {
            Debug.LogWarning("Could not find DaggerIdle clip for default state");
        }

        EditorUtility.SetDirty(animatorController);
        AssetDatabase.SaveAssets();
        Debug.Log("Reset to default state complete!");
    }
} 