using UnityEngine;
using UnityEditor;
using System.IO;

public class EnemyAnimationSetup : EditorWindow
{
    [MenuItem("Tools/Enemy Animation Setup")]
    public static void ShowWindow()
    {
        GetWindow<EnemyAnimationSetup>("Enemy Animation Setup");
    }
    
    private string enemyName = "Enemy1";
    private string animationPath = "Assets/Animations/Enemies/";
    
    private void OnGUI()
    {
        GUILayout.Label("Enemy Animation Setup", EditorStyles.boldLabel);
        
        enemyName = EditorGUILayout.TextField("Enemy Name:", enemyName);
        animationPath = EditorGUILayout.TextField("Animation Path:", animationPath);
        
        if (GUILayout.Button("Create Animation Controller"))
        {
            CreateAnimationController();
        }
        
        if (GUILayout.Button("Setup Enemy Layers"))
        {
            SetupEnemyLayers();
        }
        
        EditorGUILayout.Space();
        EditorGUILayout.HelpBox("1. First click 'Setup Enemy Layers' to create the Enemy layer if it doesn't exist\n2. Then click 'Create Animation Controller' to generate the animator controller\n3. Assign the controller to your enemy GameObject", MessageType.Info);
    }
    
    private void CreateAnimationController()
    {
        // Create directory if it doesn't exist
        if (!Directory.Exists(animationPath))
        {
            Directory.CreateDirectory(animationPath);
        }
        
        // Create animator controller
        UnityEditor.Animations.AnimatorController controller = UnityEditor.Animations.AnimatorController.CreateAnimatorControllerAtPath($"{animationPath}{enemyName}_Controller.controller");
        
        // Create states
        var rootStateMachine = controller.layers[0].stateMachine;
        
        // Create parameters
        controller.AddParameter("IsMoving", AnimatorControllerParameterType.Bool);
        controller.AddParameter("IsGrounded", AnimatorControllerParameterType.Bool);
        controller.AddParameter("IsChasing", AnimatorControllerParameterType.Bool);
        controller.AddParameter("IsAttacking", AnimatorControllerParameterType.Bool);
        controller.AddParameter("VelocityY", AnimatorControllerParameterType.Float);
        controller.AddParameter("Hit", AnimatorControllerParameterType.Trigger);
        controller.AddParameter("Death", AnimatorControllerParameterType.Trigger);
        controller.AddParameter("Alert", AnimatorControllerParameterType.Trigger);
        controller.AddParameter("Attack", AnimatorControllerParameterType.Trigger);
        
        // Create states
        var idleState = rootStateMachine.AddState("Idle");
        var moveState = rootStateMachine.AddState("Move");
        var attackState = rootStateMachine.AddState("Attack");
        var hitState = rootStateMachine.AddState("Hit");
        var deathState = rootStateMachine.AddState("Death");
        
        // Set default state
        rootStateMachine.defaultState = idleState;
        
        // Create transitions
        // Idle to Move
        var idleToMove = idleState.AddTransition(moveState);
        idleToMove.AddCondition(UnityEditor.Animations.AnimatorConditionMode.If, 0, "IsMoving");
        idleToMove.hasExitTime = false;
        idleToMove.duration = 0.1f;
        
        // Move to Idle
        var moveToIdle = moveState.AddTransition(idleState);
        moveToIdle.AddCondition(UnityEditor.Animations.AnimatorConditionMode.IfNot, 0, "IsMoving");
        moveToIdle.hasExitTime = false;
        moveToIdle.duration = 0.1f;
        
        // Any State to Attack
        var anyToAttack = rootStateMachine.AddAnyStateTransition(attackState);
        anyToAttack.AddCondition(UnityEditor.Animations.AnimatorConditionMode.If, 0, "Attack");
        anyToAttack.hasExitTime = false;
        anyToAttack.duration = 0.1f;
        
        // Attack back to Idle
        var attackToIdle = attackState.AddTransition(idleState);
        attackToIdle.hasExitTime = true;
        attackToIdle.duration = 0.1f;
        
        // Any State to Hit
        var anyToHit = rootStateMachine.AddAnyStateTransition(hitState);
        anyToHit.AddCondition(UnityEditor.Animations.AnimatorConditionMode.If, 0, "Hit");
        anyToHit.hasExitTime = false;
        anyToHit.duration = 0.1f;
        
        // Hit back to Idle
        var hitToIdle = hitState.AddTransition(idleState);
        hitToIdle.hasExitTime = true;
        hitToIdle.duration = 0.1f;
        
        // Any State to Death
        var anyToDeath = rootStateMachine.AddAnyStateTransition(deathState);
        anyToDeath.AddCondition(UnityEditor.Animations.AnimatorConditionMode.If, 0, "Death");
        anyToDeath.hasExitTime = false;
        anyToDeath.duration = 0.1f;
        
        Debug.Log($"Created animator controller at: {animationPath}{enemyName}_Controller.controller");
    }
    
    private void SetupEnemyLayers()
    {
        // Check if Enemy layer exists
        bool enemyLayerExists = false;
        for (int i = 0; i < 32; i++)
        {
            string layerName = LayerMask.LayerToName(i);
            if (layerName == "Enemy")
            {
                enemyLayerExists = true;
                break;
            }
        }
        
        if (!enemyLayerExists)
        {
            // Find first empty layer
            for (int i = 8; i < 32; i++)
            {
                string layerName = LayerMask.LayerToName(i);
                if (string.IsNullOrEmpty(layerName))
                {
                    // Create Enemy layer
                    SerializedObject tagManager = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
                    SerializedProperty layersProp = tagManager.FindProperty("layers");
                    
                    SerializedProperty layerProp = layersProp.GetArrayElementAtIndex(i);
                    layerProp.stringValue = "Enemy";
                    
                    tagManager.ApplyModifiedProperties();
                    
                    Debug.Log($"Created Enemy layer at index {i}");
                    break;
                }
            }
        }
        else
        {
            Debug.Log("Enemy layer already exists");
        }
        
        // Update physics collision matrix
        int playerLayer = LayerMask.NameToLayer("Player");
        int enemyLayer = LayerMask.NameToLayer("Enemy");
        
        if (playerLayer != -1 && enemyLayer != -1)
        {
            Physics2D.IgnoreLayerCollision(enemyLayer, enemyLayer, true);
        }
    }
}