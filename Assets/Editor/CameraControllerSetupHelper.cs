using UnityEngine;
using UnityEditor;
using Unity.Cinemachine;

/// <summary>
/// Editor helper for setting up CameraController component.
/// Provides easy setup and validation tools for the camera system.
/// </summary>
public class CameraControllerSetupHelper : EditorWindow
{
    [MenuItem("Tools/Camera/Setup Camera Controller")]
    public static void ShowWindow()
    {
        GetWindow<CameraControllerSetupHelper>("Camera Controller Setup");
    }
    
    void OnGUI()
    {
        GUILayout.Label("Camera Controller Setup", EditorStyles.boldLabel);
        GUILayout.Space(10);
        
        // Find Cinemachine camera in scene
        CinemachineCamera[] cameras = FindObjectsByType<CinemachineCamera>(FindObjectsSortMode.None);
        
        if (cameras.Length == 0)
        {
            EditorGUILayout.HelpBox("No CinemachineCamera found in scene!", MessageType.Warning);
            return;
        }
        
        foreach (var cam in cameras)
        {
            GUILayout.Label($"Found Camera: {cam.name}", EditorStyles.label);
            
            // Check if CameraController is already attached
            CameraController controller = cam.GetComponent<CameraController>();
            
            if (controller == null)
            {
                GUILayout.Label("Status: CameraController not attached", EditorStyles.label);
                
                if (GUILayout.Button($"Add CameraController to {cam.name}"))
                {
                    controller = cam.gameObject.AddComponent<CameraController>();
                    EditorUtility.SetDirty(cam.gameObject);
                    Debug.Log($"[CameraControllerSetup] Added CameraController to {cam.name}");
                }
            }
            else
            {
                GUI.contentColor = Color.green;
                GUILayout.Label("Status: CameraController attached ✓", EditorStyles.label);
                GUI.contentColor = Color.white;
                
                // Show current settings
                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Current Settings:", EditorStyles.boldLabel);
                
                using (new EditorGUI.DisabledScope(true))
                {
                    EditorGUILayout.FloatField("Vertical Range", GetPrivateField<float>(controller, "verticalRange"));
                    EditorGUILayout.FloatField("Adjustment Speed", GetPrivateField<float>(controller, "adjustmentSpeed"));
                    EditorGUILayout.FloatField("Return Speed", GetPrivateField<float>(controller, "returnSpeed"));
                }
                
                if (GUILayout.Button("Select Camera Controller"))
                {
                    Selection.activeObject = controller;
                    EditorGUIUtility.PingObject(controller);
                }
            }
            
            GUILayout.Space(10);
        }
        
        // Validation section
        GUILayout.Space(20);
        GUILayout.Label("Validation", EditorStyles.boldLabel);
        
        if (GUILayout.Button("Validate Scene Setup"))
        {
            ValidateSceneSetup();
        }
    }
    
    private static T GetPrivateField<T>(object obj, string fieldName)
    {
        var field = obj.GetType().GetField(fieldName, 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        return field != null ? (T)field.GetValue(obj) : default(T);
    }
    
    private static void ValidateSceneSetup()
    {
        bool hasErrors = false;
        
        // Check for InputManager
        if (FindFirstObjectByType<InputManager>() == null)
        {
            Debug.LogError("[CameraControllerSetup] No InputManager found in scene!");
            hasErrors = true;
        }
        else
        {
            Debug.Log("[CameraControllerSetup] InputManager found ✓");
        }
        
        // Check for Cinemachine cameras
        CinemachineCamera[] cameras = FindObjectsByType<CinemachineCamera>(FindObjectsSortMode.None);
        if (cameras.Length == 0)
        {
            Debug.LogError("[CameraControllerSetup] No CinemachineCamera found in scene!");
            hasErrors = true;
        }
        else
        {
            Debug.Log($"[CameraControllerSetup] Found {cameras.Length} CinemachineCamera(s) ✓");
            
            foreach (var cam in cameras)
            {
                // Check for CinemachineFollow component
                if (cam.GetComponent<CinemachineFollow>() == null)
                {
                    Debug.LogWarning($"[CameraControllerSetup] Camera '{cam.name}' missing CinemachineFollow component!");
                    hasErrors = true;
                }
                
                // Check for tracking target
                if (cam.Target.TrackingTarget == null)
                {
                    Debug.LogWarning($"[CameraControllerSetup] Camera '{cam.name}' has no tracking target set!");
                    hasErrors = true;
                }
                else
                {
                    Debug.Log($"[CameraControllerSetup] Camera '{cam.name}' tracking target: {cam.Target.TrackingTarget.name} ✓");
                }
                
                // Check for CameraController component
                if (cam.GetComponent<CameraController>() == null)
                {
                    Debug.LogWarning($"[CameraControllerSetup] Camera '{cam.name}' missing CameraController component!");
                }
                else
                {
                    Debug.Log($"[CameraControllerSetup] Camera '{cam.name}' has CameraController ✓");
                }
            }
        }
        
        // Check for Main Camera with CinemachineBrain
        Camera mainCamera = Camera.main;
        if (mainCamera == null)
        {
            Debug.LogError("[CameraControllerSetup] No Main Camera found in scene!");
            hasErrors = true;
        }
        else if (mainCamera.GetComponent<CinemachineBrain>() == null)
        {
            Debug.LogWarning("[CameraControllerSetup] Main Camera missing CinemachineBrain component!");
            hasErrors = true;
        }
        else
        {
            Debug.Log("[CameraControllerSetup] Main Camera with CinemachineBrain found ✓");
        }
        
        if (!hasErrors)
        {
            Debug.Log("[CameraControllerSetup] All validation checks passed! ✓");
        }
    }
}