#!/usr/bin/env python3
"""
Analyze a C# Unity class and suggest what to test.

Usage:
    python suggest_tests.py <path_to_cs_file>
"""

import re
import sys
import os

def analyze_class(cs_content):
    """Analyze C# class and suggest tests."""
    suggestions = []
    
    # Extract class name
    class_match = re.search(r'class\s+(\w+)', cs_content)
    if not class_match:
        return ["Could not find class definition"]
    
    class_name = class_match.group(1)
    suggestions.append(f"=== Testing Suggestions for {class_name} ===\n")
    
    # Check if MonoBehaviour
    if 'MonoBehaviour' in cs_content:
        suggestions.append("✓ MonoBehaviour detected")
        suggestions.append("  - Test component initialization in SetUp")
        suggestions.append("  - Test Awake() initialization if present")
        suggestions.append("  - Test Start() setup if present")
        suggestions.append("  - Create and destroy GameObject in SetUp/TearDown\n")
    
    # Find public methods
    method_pattern = r'public\s+(?!class|interface|struct)(\w+)\s+(\w+)\s*\([^)]*\)'
    methods = re.findall(method_pattern, cs_content)
    
    if methods:
        suggestions.append("Public Methods to Test:")
        for return_type, method_name in methods:
            if method_name == class_name:  # Skip constructor
                continue
            suggestions.append(f"  • {method_name}()")
            
            # Suggest test cases based on return type
            if return_type == "bool":
                suggestions.append(f"    - Test when returns true")
                suggestions.append(f"    - Test when returns false")
            elif return_type == "void":
                suggestions.append(f"    - Test state changes")
                suggestions.append(f"    - Test side effects")
            elif return_type in ["int", "float", "double"]:
                suggestions.append(f"    - Test with positive values")
                suggestions.append(f"    - Test with zero")
                suggestions.append(f"    - Test with negative values")
            else:
                suggestions.append(f"    - Test valid return values")
                suggestions.append(f"    - Test with null inputs (if applicable)")
        suggestions.append("")
    
    # Find public properties
    property_pattern = r'public\s+(\w+)\s+(\w+)\s*\{\s*get'
    properties = re.findall(property_pattern, cs_content)
    
    if properties:
        suggestions.append("Public Properties to Test:")
        for prop_type, prop_name in properties:
            suggestions.append(f"  • {prop_name}")
            suggestions.append(f"    - Test initial value")
            if 'set' in cs_content:
                suggestions.append(f"    - Test setting and getting value")
        suggestions.append("")
    
    # Check for coroutines
    coroutine_pattern = r'IEnumerator\s+(\w+)\s*\('
    coroutines = re.findall(coroutine_pattern, cs_content)
    
    if coroutines:
        suggestions.append("⚠ Coroutines detected - Use [UnityTest]:")
        for coroutine in coroutines:
            suggestions.append(f"  • {coroutine}()")
            suggestions.append(f"    - Test coroutine completion")
            suggestions.append(f"    - Test state changes during execution")
            if 'WaitForSeconds' in cs_content:
                suggestions.append(f"    - Test timing/delays")
        suggestions.append("")
    
    # Check for Unity events
    if 'UnityEvent' in cs_content:
        suggestions.append("⚠ Unity Events detected:")
        suggestions.append("  - Test that events are invoked when expected")
        suggestions.append("  - Test event listeners receive correct parameters")
        suggestions.append("")
    
    # Check for collision/trigger methods
    collision_methods = ['OnCollisionEnter', 'OnCollisionExit', 'OnTriggerEnter', 'OnTriggerExit']
    found_collision = [m for m in collision_methods if m in cs_content]
    if found_collision:
        suggestions.append("⚠ Collision/Trigger methods detected:")
        for method in found_collision:
            suggestions.append(f"  • {method}")
            suggestions.append(f"    - Use PlayMode tests")
            suggestions.append(f"    - Test collision detection")
            suggestions.append(f"    - Test response behavior")
        suggestions.append("")
    
    # Check for SerializeField or public fields
    if '[SerializeField]' in cs_content or re.search(r'public\s+(?!class|void|int|float|bool|string)\w+\s+\w+;', cs_content):
        suggestions.append("⚠ Serialized/Public fields detected:")
        suggestions.append("  - Consider testing field initialization")
        suggestions.append("  - Test behavior with null/unassigned references")
        suggestions.append("")
    
    # Check for Update methods
    if 'void Update()' in cs_content or 'void FixedUpdate()' in cs_content:
        suggestions.append("⚠ Update method detected:")
        suggestions.append("  - Consider PlayMode tests for frame-by-frame behavior")
        suggestions.append("  - Test state changes over multiple frames")
        suggestions.append("")
    
    # Check for static methods/classes
    if 'static class' in cs_content or re.search(r'public static \w+', cs_content):
        suggestions.append("⚠ Static methods/class detected:")
        suggestions.append("  - Use simple EditMode tests")
        suggestions.append("  - Test pure functions with various inputs")
        suggestions.append("  - Use [TestCase] for multiple input scenarios")
        suggestions.append("")
    
    # Edge cases and error handling
    suggestions.append("General Testing Recommendations:")
    suggestions.append("  ✓ Test edge cases (null, empty, boundary values)")
    suggestions.append("  ✓ Test error conditions")
    suggestions.append("  ✓ Test state before and after operations")
    suggestions.append("  ✓ Use descriptive test names: MethodName_WhenCondition_ShouldBehavior")
    suggestions.append("  ✓ Follow AAA pattern: Arrange, Act, Assert")
    suggestions.append("")
    
    # Suggest test type
    if 'MonoBehaviour' in cs_content:
        if any(m in cs_content for m in collision_methods) or 'IEnumerator' in cs_content:
            suggestions.append("Recommended Test Mode: PlayMode (requires Unity runtime)")
        else:
            suggestions.append("Recommended Test Mode: EditMode (faster, preferred)")
    else:
        suggestions.append("Recommended Test Mode: EditMode (pure C# logic)")
    
    return suggestions

def main():
    if len(sys.argv) < 2:
        print("Usage: python suggest_tests.py <path_to_cs_file>")
        sys.exit(1)
    
    input_file = sys.argv[1]
    
    if not os.path.exists(input_file):
        print(f"Error: File '{input_file}' not found")
        sys.exit(1)
    
    # Read the C# file
    with open(input_file, 'r') as f:
        cs_content = f.read()
    
    # Analyze and print suggestions
    suggestions = analyze_class(cs_content)
    for suggestion in suggestions:
        print(suggestion)

if __name__ == "__main__":
    main()
