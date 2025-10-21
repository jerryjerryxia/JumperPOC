#!/usr/bin/env python3
"""
Generate Unity test boilerplate from a C# class file.

Usage:
    python generate_test_boilerplate.py <path_to_cs_file> [--output <output_path>]
"""

import re
import sys
import os
from pathlib import Path

def extract_class_info(cs_content):
    """Extract class name and methods from C# file."""
    # Find class name
    class_match = re.search(r'class\s+(\w+)', cs_content)
    if not class_match:
        return None, []
    
    class_name = class_match.group(1)
    
    # Find public methods (excluding constructors)
    method_pattern = r'public\s+(?!class|interface|struct)\w+\s+(\w+)\s*\('
    methods = re.findall(method_pattern, cs_content)
    
    # Filter out constructors
    methods = [m for m in methods if m != class_name]
    
    return class_name, methods

def is_monobehaviour(cs_content):
    """Check if class inherits from MonoBehaviour."""
    return 'MonoBehaviour' in cs_content

def generate_test_class(class_name, methods, is_monobehaviour_class):
    """Generate test class boilerplate."""
    
    setup_teardown = ""
    if is_monobehaviour_class:
        setup_teardown = f"""
    private {class_name} _component;
    private GameObject _gameObject;

    [SetUp]
    public void SetUp()
    {{
        _gameObject = new GameObject();
        _component = _gameObject.AddComponent<{class_name}>();
    }}

    [TearDown]
    public void TearDown()
    {{
        Object.DestroyImmediate(_gameObject);
    }}
"""
    else:
        setup_teardown = f"""
    private {class_name} _instance;

    [SetUp]
    public void SetUp()
    {{
        _instance = new {class_name}();
    }}
"""
    
    test_methods = []
    for method in methods:
        test_methods.append(f"""
    [Test]
    public void {method}_When{{Condition}}_Should{{ExpectedBehavior}}()
    {{
        // Arrange: Set up test conditions
        
        // Act: Execute the method under test
        {('_component' if is_monobehaviour_class else '_instance')}.{method}();
        
        // Assert: Verify expected behavior
        Assert.Fail("Test not implemented");
    }}""")
    
    test_class = f"""using NUnit.Framework;
using UnityEngine;

public class {class_name}Tests
{{{setup_teardown}
    [Test]
    public void {class_name}_Initializes_Correctly()
    {{
        // Arrange & Act
        {('Assert.IsNotNull(_component);' if is_monobehaviour_class else 'Assert.IsNotNull(_instance);')}
        
        // Assert: Verify initialization state
        Assert.Fail("Test not implemented");
    }}
{''.join(test_methods)}
}}
"""
    return test_class

def main():
    if len(sys.argv) < 2:
        print("Usage: python generate_test_boilerplate.py <path_to_cs_file> [--output <output_path>]")
        sys.exit(1)
    
    input_file = sys.argv[1]
    
    # Check if output path is provided
    output_file = None
    if "--output" in sys.argv:
        output_index = sys.argv.index("--output")
        if output_index + 1 < len(sys.argv):
            output_file = sys.argv[output_index + 1]
    
    if not os.path.exists(input_file):
        print(f"Error: File '{input_file}' not found")
        sys.exit(1)
    
    # Read the C# file
    with open(input_file, 'r') as f:
        cs_content = f.read()
    
    # Extract information
    class_name, methods = extract_class_info(cs_content)
    
    if not class_name:
        print("Error: Could not find a class definition in the file")
        sys.exit(1)
    
    is_mono = is_monobehaviour(cs_content)
    
    # Generate test class
    test_code = generate_test_class(class_name, methods, is_mono)
    
    # Output
    if output_file:
        with open(output_file, 'w') as f:
            f.write(test_code)
        print(f"Test file generated: {output_file}")
    else:
        print(test_code)
    
    # Print summary
    print(f"\n--- Summary ---", file=sys.stderr)
    print(f"Class: {class_name}", file=sys.stderr)
    print(f"Type: {'MonoBehaviour' if is_mono else 'Regular Class'}", file=sys.stderr)
    print(f"Methods found: {len(methods)}", file=sys.stderr)
    if methods:
        print(f"Methods: {', '.join(methods)}", file=sys.stderr)

if __name__ == "__main__":
    main()
