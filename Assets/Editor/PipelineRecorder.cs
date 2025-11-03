using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using System;

public static class PipelineRecorder
{
    private static List<string> actions = new List<string>();
    private static bool recording = false;
    private static string pipelinesDir = "Assets/Pipelines";

    public static void Start()
    {
        actions.Clear();
        recording = true;
        if (!Directory.Exists(pipelinesDir)) Directory.CreateDirectory(pipelinesDir);
    }

    public static void StopAndSave(string name)
    {
        recording = false;
        var payload = new PipelinePayload { name = name, steps = actions.ToArray(), created = DateTime.UtcNow.ToString("o") };
        var json = JsonUtility.ToJson(payload, true);
        var path = Path.Combine(pipelinesDir, name + ".json");
        File.WriteAllText(path, json);
        AssetDatabase.Refresh();
    }

    public static void Record(string action)
    {
        if (!recording) return;
        actions.Add(action);
    }

    public static void Replay(string path)
    {
        if (!File.Exists(path)) { Debug.LogError("Pipeline not found: " + path); return; }
        var json = File.ReadAllText(path);
        var payload = JsonUtility.FromJson<PipelinePayload>(json);
        foreach (var step in payload.steps)
        {
            // Very simple: treat each step as a raw instruction for CommandRouter
            var res = CommandRouter.RouteAndExecute(step);
            Debug.Log("Replayed step: " + step + " => " + res);
        }
    }

    [Serializable]
    private class PipelinePayload { public string name; public string[] steps; public string created; }
}
