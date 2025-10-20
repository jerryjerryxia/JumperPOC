#!/usr/bin/env python3
"""
Unity Project Analyzer
Analyzes a Unity project and generates metrics on code structure, complexity, and organization.
"""

import os
import sys
import re
from pathlib import Path
from collections import defaultdict
from dataclasses import dataclass, field
from typing import Dict, List, Set

@dataclass
class ScriptMetrics:
    """Metrics for a single C# script."""
    path: str
    lines_of_code: int = 0
    comment_lines: int = 0
    blank_lines: int = 0
    class_count: int = 0
    method_count: int = 0
    property_count: int = 0
    field_count: int = 0
    complexity: int = 0  # Cyclomatic complexity estimate
    dependencies: Set[str] = field(default_factory=set)

@dataclass
class ProjectMetrics:
    """Overall project metrics."""
    total_scripts: int = 0
    total_loc: int = 0
    total_comments: int = 0
    scripts_by_directory: Dict[str, List[str]] = field(default_factory=lambda: defaultdict(list))
    script_metrics: List[ScriptMetrics] = field(default_factory=list)
    prefab_count: int = 0
    scene_count: int = 0
    animation_count: int = 0
    material_count: int = 0

def count_lines(file_path: str) -> ScriptMetrics:
    """Analyze a single C# script file."""
    metrics = ScriptMetrics(path=file_path)
    
    try:
        with open(file_path, 'r', encoding='utf-8', errors='ignore') as f:
            content = f.read()
            lines = content.split('\n')
            
            in_multiline_comment = False
            
            for line in lines:
                stripped = line.strip()
                
                # Handle multiline comments
                if '/*' in stripped:
                    in_multiline_comment = True
                if '*/' in stripped:
                    in_multiline_comment = False
                    metrics.comment_lines += 1
                    continue
                
                if in_multiline_comment:
                    metrics.comment_lines += 1
                elif not stripped:
                    metrics.blank_lines += 1
                elif stripped.startswith('//'):
                    metrics.comment_lines += 1
                else:
                    metrics.lines_of_code += 1
            
            # Count classes
            metrics.class_count = len(re.findall(r'\bclass\s+\w+', content))
            
            # Count methods (public, private, protected)
            metrics.method_count = len(re.findall(
                r'(public|private|protected|internal)\s+\w+\s+\w+\s*\([^)]*\)\s*{',
                content
            ))
            
            # Count properties
            metrics.property_count = len(re.findall(r'\bproperty\s+\w+', content)) + \
                                    len(re.findall(r'{\s*get\s*;', content))
            
            # Count fields
            metrics.field_count = len(re.findall(
                r'(public|private|protected|internal)\s+\w+\s+\w+\s*[;=]',
                content
            ))
            
            # Estimate cyclomatic complexity
            complexity_keywords = ['if', 'else', 'for', 'foreach', 'while', 'case', 'catch', '&&', '||']
            for keyword in complexity_keywords:
                metrics.complexity += len(re.findall(r'\b' + keyword + r'\b', content))
            
            # Find dependencies (using statements)
            using_pattern = r'using\s+([\w.]+);'
            metrics.dependencies = set(re.findall(using_pattern, content))
            
    except Exception as e:
        print(f"Error reading {file_path}: {e}")
    
    return metrics

def analyze_unity_project(project_path: str) -> ProjectMetrics:
    """Analyze the entire Unity project."""
    project_path = Path(project_path)
    assets_path = project_path / "Assets"
    
    if not assets_path.exists():
        print(f"Error: Assets folder not found at {assets_path}")
        sys.exit(1)
    
    metrics = ProjectMetrics()
    
    # Analyze C# scripts
    scripts_path = assets_path / "Scripts"
    if scripts_path.exists():
        for cs_file in scripts_path.rglob("*.cs"):
            script_metrics = count_lines(str(cs_file))
            metrics.script_metrics.append(script_metrics)
            metrics.total_scripts += 1
            metrics.total_loc += script_metrics.lines_of_code
            metrics.total_comments += script_metrics.comment_lines
            
            # Categorize by directory
            relative_path = cs_file.relative_to(scripts_path)
            directory = str(relative_path.parent)
            metrics.scripts_by_directory[directory].append(cs_file.name)
    
    # Count other assets
    metrics.prefab_count = len(list(assets_path.rglob("*.prefab")))
    metrics.scene_count = len(list(assets_path.rglob("*.unity")))
    metrics.animation_count = len(list(assets_path.rglob("*.anim")))
    metrics.material_count = len(list(assets_path.rglob("*.mat")))
    
    return metrics

def print_report(metrics: ProjectMetrics, project_path: str):
    """Print a formatted report of the project metrics."""
    print("=" * 80)
    print(f"UNITY PROJECT ANALYSIS: {project_path}")
    print("=" * 80)
    print()
    
    print("PROJECT OVERVIEW")
    print("-" * 80)
    print(f"Total C# Scripts:     {metrics.total_scripts}")
    print(f"Total Lines of Code:  {metrics.total_loc:,}")
    print(f"Total Comment Lines:  {metrics.total_comments:,}")
    print(f"Average LOC/Script:   {metrics.total_loc // metrics.total_scripts if metrics.total_scripts > 0 else 0}")
    print()
    
    print(f"Prefabs:             {metrics.prefab_count}")
    print(f"Scenes:              {metrics.scene_count}")
    print(f"Animations:          {metrics.animation_count}")
    print(f"Materials:           {metrics.material_count}")
    print()
    
    print("SCRIPT ORGANIZATION")
    print("-" * 80)
    for directory, scripts in sorted(metrics.scripts_by_directory.items()):
        if directory == ".":
            directory = "Scripts (root)"
        print(f"{directory}/")
        print(f"  Scripts: {len(scripts)}")
    print()
    
    print("TOP 10 LARGEST SCRIPTS")
    print("-" * 80)
    sorted_scripts = sorted(metrics.script_metrics, key=lambda x: x.lines_of_code, reverse=True)[:10]
    for i, script in enumerate(sorted_scripts, 1):
        script_name = Path(script.path).name
        print(f"{i:2}. {script_name:40} {script.lines_of_code:6} LOC")
    print()
    
    print("TOP 10 MOST COMPLEX SCRIPTS")
    print("-" * 80)
    sorted_by_complexity = sorted(metrics.script_metrics, key=lambda x: x.complexity, reverse=True)[:10]
    for i, script in enumerate(sorted_by_complexity, 1):
        script_name = Path(script.path).name
        print(f"{i:2}. {script_name:40} Complexity: {script.complexity}")
    print()
    
    print("COMMON DEPENDENCIES (Top 15)")
    print("-" * 80)
    all_dependencies = defaultdict(int)
    for script in metrics.script_metrics:
        for dep in script.dependencies:
            all_dependencies[dep] += 1
    
    sorted_deps = sorted(all_dependencies.items(), key=lambda x: x[1], reverse=True)[:15]
    for dep, count in sorted_deps:
        print(f"{dep:40} Used in {count} files")
    print()
    
    print("CODE QUALITY METRICS")
    print("-" * 80)
    avg_complexity = sum(s.complexity for s in metrics.script_metrics) / len(metrics.script_metrics) if metrics.script_metrics else 0
    avg_methods = sum(s.method_count for s in metrics.script_metrics) / len(metrics.script_metrics) if metrics.script_metrics else 0
    comment_ratio = (metrics.total_comments / metrics.total_loc * 100) if metrics.total_loc > 0 else 0
    
    print(f"Average Complexity per Script:  {avg_complexity:.2f}")
    print(f"Average Methods per Script:     {avg_methods:.2f}")
    print(f"Comment to Code Ratio:          {comment_ratio:.2f}%")
    print()
    
    # Identify potential issues
    print("POTENTIAL ISSUES")
    print("-" * 80)
    large_scripts = [s for s in metrics.script_metrics if s.lines_of_code > 500]
    if large_scripts:
        print(f"⚠ {len(large_scripts)} scripts exceed 500 LOC (consider refactoring)")
        for script in large_scripts[:5]:
            print(f"  - {Path(script.path).name}: {script.lines_of_code} LOC")
    
    complex_scripts = [s for s in metrics.script_metrics if s.complexity > 50]
    if complex_scripts:
        print(f"⚠ {len(complex_scripts)} scripts have high complexity (>50)")
        for script in complex_scripts[:5]:
            print(f"  - {Path(script.path).name}: Complexity {script.complexity}")
    
    low_comment_scripts = [s for s in metrics.script_metrics if s.lines_of_code > 100 and s.comment_lines < s.lines_of_code * 0.05]
    if low_comment_scripts:
        print(f"⚠ {len(low_comment_scripts)} scripts lack sufficient comments (<5%)")
    
    if not large_scripts and not complex_scripts:
        print("✓ No major issues detected!")
    
    print()
    print("=" * 80)

def main():
    if len(sys.argv) < 2:
        print("Usage: python analyze_unity_project.py <path_to_unity_project>")
        sys.exit(1)
    
    project_path = sys.argv[1]
    
    if not os.path.exists(project_path):
        print(f"Error: Path '{project_path}' does not exist")
        sys.exit(1)
    
    print(f"Analyzing Unity project at: {project_path}")
    print("This may take a moment...\n")
    
    metrics = analyze_unity_project(project_path)
    print_report(metrics, project_path)

if __name__ == "__main__":
    main()
