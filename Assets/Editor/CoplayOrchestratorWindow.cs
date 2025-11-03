using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;

public class UnityAIOrchestratorWindow : EditorWindow
{
    private string prompt = "";
    private string response = "";
    private Vector2 scroll;
    private LLMOrchestrator orchestrator;
    private ILLMProvider claude, openai, gemini, lmStudio, mcp;
    private int modeIndex = 2; // 0=Solo,1=Dual,2=Tri,3=Quad (Claude+OpenAI+Gemini+LMStudio),4=Penta (+MCP)
    private bool recording = false;

    [MenuItem("Window/Unity AI Orchestrator")]
    public static void ShowWindow() => GetWindow<UnityAIOrchestratorWindow>("Unity AI Orchestrator");

    private void OnEnable()
    {
        // Load keys from EditorPrefs
        var anc = EditorPrefs.GetString("unityai_anthropic_api_key", "");
        var oid = EditorPrefs.GetString("unityai_openai_api_key", "");
        var gid = EditorPrefs.GetString("unityai_gemini_api_key", "");

        claude = new ClaudeAdapter(anc);
        openai = new OpenAIAdapter(oid);
        gemini = new GeminiAdapter(gid);

        // Load LM Studio settings and create adapter if enabled
        var lmStudioEnabled = EditorPrefs.GetBool("unityai_lmstudio_enabled", false);
        if (lmStudioEnabled)
        {
            var lmStudioUrl = EditorPrefs.GetString("unityai_lmstudio_url", "http://localhost:1234/v1/chat/completions");
            var lmStudioModel = EditorPrefs.GetString("unityai_lmstudio_model", "");
            var lmStudioAuthHeader = EditorPrefs.GetString("unityai_lmstudio_auth_header", "");
            lmStudio = new LMStudioAdapter(lmStudioUrl, string.IsNullOrEmpty(lmStudioModel) ? null : lmStudioModel, string.IsNullOrEmpty(lmStudioAuthHeader) ? null : lmStudioAuthHeader);
        }
        else
        {
            lmStudio = null;
        }

        // Load MCP settings and create adapter if enabled
        var mcpEnabled = EditorPrefs.GetBool("unityai_mcp_enabled", false);
        if (mcpEnabled)
        {
            var mcpCommand = EditorPrefs.GetString("unityai_mcp_server_command", "");
            var mcpArgs = EditorPrefs.GetString("unityai_mcp_server_args", "");
            var mcpUrl = EditorPrefs.GetString("unityai_mcp_server_url", "");
            var mcpName = EditorPrefs.GetString("unityai_mcp_server_name", "MCP Server");
            var mcpUseHttp = EditorPrefs.GetBool("unityai_mcp_use_http", false);

            if (mcpUseHttp && !string.IsNullOrEmpty(mcpUrl))
            {
                mcp = new MCPAdapter(null, null, mcpUrl, mcpName);
            }
            else if (!mcpUseHttp && !string.IsNullOrEmpty(mcpCommand))
            {
                mcp = new MCPAdapter(mcpCommand, mcpArgs, null, mcpName);
            }
            else
            {
                mcp = null;
                Debug.LogWarning("MCP enabled but configuration incomplete. Check Settings window.");
            }
        }
        else
        {
            mcp = null;
        }

        RebuildOrchestrator();
    }

    private void OnDisable()
    {
        // Cleanup MCP connection
        if (mcp is MCPAdapter adapter)
        {
            adapter.Disconnect();
        }
    }

    private void RebuildOrchestrator()
    {
        var providers = new List<ILLMProvider>();
        // modeIndex: 0=Solo (Claude),1=Dual (Claude+OpenAI),2=Tri (Claude+OpenAI+Gemini),3=Quad (+LMStudio),4=Penta (+MCP)
        providers.Add(claude);
        if (modeIndex >= 1) providers.Add(openai);
        if (modeIndex >= 2) providers.Add(gemini);
        if (modeIndex >= 3 && lmStudio != null) providers.Add(lmStudio);
        if (modeIndex >= 4 && mcp != null) providers.Add(mcp);
        orchestrator = new LLMOrchestrator(providers);
    }

    private void OnGUI()
    {
        var hasLMStudio = lmStudio != null;
        var hasMCP = mcp != null;
        var labelText = "Coplay Orchestrator";
        if (hasMCP && hasLMStudio) labelText += " (Triple + LM Studio + MCP)";
        else if (hasLMStudio) labelText += " (Triple + LM Studio)";
        else if (hasMCP) labelText += " (Triple + MCP)";
        else labelText += " (Triple)";
        
        GUILayout.Label(labelText, EditorStyles.boldLabel);

        GUILayout.BeginHorizontal();
        GUILayout.Label("Mode:");
        
        // Build modes array dynamically
        var modesList = new List<string> { "Solo", "Dual", "Tri" };
        if (hasLMStudio) modesList.Add("Quad");
        if (hasMCP) modesList.Add("Penta");
        
        var modes = modesList.ToArray();
        var maxIndex = modes.Length - 1;
        modeIndex = Mathf.Clamp(modeIndex, 0, maxIndex);
        var newIndex = GUILayout.Toolbar(modeIndex, modes);
        if (newIndex != modeIndex) { modeIndex = newIndex; RebuildOrchestrator(); }
        if (GUILayout.Button("Settings", GUILayout.Width(80))) 
        { 
            CoplaySettingsWindow.ShowWindow();
            // Reload settings after settings window might have changed them
            OnEnable();
        }
        GUILayout.EndHorizontal();

        if (hasLMStudio && modeIndex == 3)
        {
            EditorGUILayout.HelpBox("LM Studio mode enabled. Make sure LM Studio is running on the configured URL.", MessageType.Info);
        }
        if (hasMCP && modeIndex == 4)
        {
            EditorGUILayout.HelpBox("MCP mode enabled. Make sure your MCP server is running and properly configured in Settings.", MessageType.Info);
        }

        prompt = EditorGUILayout.TextArea(prompt, GUILayout.Height(80));
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Send to Orchestrator")) SendPrompt();
        if (!recording)
        {
            if (GUILayout.Button("Start Recording")) { PipelineRecorder.Start(); recording = true; }
        }
        else
        {
            if (GUILayout.Button("Stop & Save Recording")) { PipelineRecorder.StopAndSave("pipeline_" + DateTime.Now.ToString("yyyyMMdd_HHmmss")); recording = false; }
        }
        GUILayout.EndHorizontal();

        GUILayout.Space(8);
        GUILayout.Label("Response (merged):");
        scroll = EditorGUILayout.BeginScrollView(scroll, GUILayout.Height(220));
        EditorGUILayout.TextArea(response, GUILayout.ExpandHeight(true));
        EditorGUILayout.EndScrollView();

        GUILayout.Space(6);
        GUILayout.Label("Execute as Command:");
        if (GUILayout.Button("Execute AI as Command (CommandRouter)"))
        {
            var res = CommandRouter.RouteAndExecute(prompt);
            PipelineRecorder.Record(prompt);
            Debug.Log("CommandRouter result: " + res);
        }
    }

    private async void SendPrompt()
    {
        response = "...waiting...";
        Repaint();
        try
        {
            var ct = new CancellationTokenSource(120000).Token;
            var res = await orchestrator.SendPromptAsync(prompt, ct);
            response = res;
        }
        catch (Exception ex)
        {
            response = "Error: " + ex.Message;
        }
        Repaint();
    }
}
