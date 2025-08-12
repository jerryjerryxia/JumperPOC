using UnityEngine;

/// <summary>
/// Simple trigger handler for head stomp detection.
/// This component is automatically created by SimpleHeadStomp.
/// </summary>
public class HeadStompTriggerHandler : MonoBehaviour
{
    private SimpleHeadStomp parentStomp;
    
    public void Initialize(SimpleHeadStomp stomp)
    {
        parentStomp = stomp;
    }
    
    void OnTriggerEnter2D(Collider2D other)
    {
        if (parentStomp != null)
        {
            parentStomp.OnPlayerEnterTrigger(other);
        }
    }
}