using NUnit.Framework;
using UnityEngine;

namespace Tests.EditMode
{
    /// <summary>
    /// SUPER SIMPLE test to verify PlayerAbilities.Instance gets set in EditMode
    /// </summary>
    [TestFixture]
    public class SimplePlayerAbilitiesTest
    {
        [Test]
        public void PlayerAbilities_InstanceGetsSet_WhenComponentAdded()
        {
            // Clean up first
            if (PlayerAbilities.Instance != null)
            {
                Object.DestroyImmediate(PlayerAbilities.Instance.gameObject);
            }
            var field = typeof(PlayerAbilities).GetField("Instance",
                System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public);
            if (field != null)
            {
                field.SetValue(null, null);
            }

            // Verify it's null
            Assert.IsNull(PlayerAbilities.Instance, "Instance should be null before creation");

            // Create GameObject and add component
            GameObject obj = new GameObject("TestAbilities");
            PlayerAbilities abilities = obj.AddComponent<PlayerAbilities>();
            abilities.InitializeForTesting(); // EditMode doesn't call Awake() automatically

            // Check if component was added
            Assert.IsNotNull(abilities, "Component should not be null after AddComponent");

            // Check if Instance was set
            UnityEngine.Debug.Log($"PlayerAbilities.Instance after AddComponent: {PlayerAbilities.Instance}");
            UnityEngine.Debug.Log($"abilities reference: {abilities}");
            UnityEngine.Debug.Log($"abilities.gameObject: {abilities.gameObject}");

            Assert.IsNotNull(PlayerAbilities.Instance,
                $"CRITICAL: PlayerAbilities.Instance is NULL after AddComponent! " +
                $"Component ref: {abilities}, GameObject: {abilities.gameObject}");

            // Clean up
            Object.DestroyImmediate(obj);
            field.SetValue(null, null);
        }
    }
}
