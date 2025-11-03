using System;
using UnityEditor;
using UnityEngine;

// Very small command router: parses simple commands and executes Unity Editor actions.
public static class CommandRouter
{
    public static string RouteAndExecute(string instruction)
    {
        try
        {
            instruction = instruction.Trim();

            // Create primitive
            if (instruction.StartsWith("create object", StringComparison.OrdinalIgnoreCase))
            {
                // Example: create object Cube named "MyCube"
                var name = ExtractQuoted(instruction) ?? "NewObject";
                var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
                go.name = name;
                Undo.RegisterCreatedObjectUndo(go, "AI: Create Object");
                return $"Created primitive cube named '{name}'";
            }

            // Rename selected
            if (instruction.StartsWith("rename selected to", StringComparison.OrdinalIgnoreCase))
            {
                var name = ExtractQuoted(instruction) ?? "RenamedObject";
                var sel = Selection.activeGameObject;
                if (sel == null) return "No active GameObject selected.";
                Undo.RecordObject(sel, "AI: Rename");
                sel.name = name;
                return $"Renamed selected to '{name}'";
            }

            // Add component
            if (instruction.StartsWith("add component", StringComparison.OrdinalIgnoreCase))
            {
                // Example: add component Rigidbody to selected
                var comp = ExtractQuoted(instruction) ?? "Rigidbody";
                var sel = Selection.activeGameObject;
                if (sel == null) return "No active GameObject selected.";
                Undo.RecordObject(sel, "AI: Add Component");
                var type = Type.GetType(comp) ?? typeof(UnityEngine.Rigidbody);
                sel.AddComponent(type);
                return $"Added component '{comp}' to selected.";
            }

            // Move selected by vector
            if (instruction.StartsWith("move selected by", StringComparison.OrdinalIgnoreCase))
            {
                // move selected by 1,0,0
                var parts = instruction.Split(new[]{' '}, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length >= 4)
                {
                    var vec = parts[3].Split(',');
                    if (vec.Length == 3)
                    {
                        float x = float.Parse(vec[0]);
                        float y = float.Parse(vec[1]);
                        float z = float.Parse(vec[2]);
                        var sel = Selection.activeGameObject;
                        if (sel == null) return "No active GameObject selected.";
                        Undo.RecordObject(sel.transform, "AI: Move");
                        sel.transform.position += new Vector3(x, y, z);
                        return $"Moved selected by ({x},{y},{z}).";
                    }
                }
            }

            return "No known command matched. You can extend CommandRouter with more mappings.";
        }
        catch (Exception ex)
        {
            Debug.LogError(ex);
            return "Error executing command: " + ex.Message;
        }
    }

    private static string ExtractQuoted(string s)
    {
        var first = s.IndexOf('"');
        if (first < 0) return null;
        var second = s.IndexOf('"', first + 1);
        if (second < 0) return null;
        return s.Substring(first + 1, second - first - 1);
    }
}
