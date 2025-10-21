using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using System.Collections;

public class {ClassName}CoroutineTests
{
    private {ClassName} _component;
    private GameObject _gameObject;

    [SetUp]
    public void SetUp()
    {
        _gameObject = new GameObject();
        _component = _gameObject.AddComponent<{ClassName}>();
    }

    [TearDown]
    public void TearDown()
    {
        Object.DestroyImmediate(_gameObject);
    }

    [UnityTest]
    public IEnumerator {CoroutineName}_CompletesSuccessfully()
    {
        // Arrange
        var completed = false;
        
        // Act
        yield return _component.{CoroutineName}();
        completed = true;

        // Assert
        Assert.IsTrue(completed);
    }

    [UnityTest]
    public IEnumerator {CoroutineName}_UpdatesValueOverTime()
    {
        // Arrange
        var initialValue = /* get initial value */;

        // Act - Wait for a few frames
        yield return null;
        yield return null;
        
        var updatedValue = /* get updated value */;

        // Assert
        Assert.AreNotEqual(initialValue, updatedValue);
    }

    [UnityTest]
    public IEnumerator {CoroutineName}_WithDelay_WaitsCorrectDuration()
    {
        // Arrange
        var startTime = Time.time;
        var expectedDelay = 1.0f;

        // Act
        yield return _component.{CoroutineName}();
        
        var endTime = Time.time;
        var actualDelay = endTime - startTime;

        // Assert
        Assert.GreaterOrEqual(actualDelay, expectedDelay, 0.1f);
    }
}
