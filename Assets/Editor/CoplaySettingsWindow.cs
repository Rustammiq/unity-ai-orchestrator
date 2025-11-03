using UnityEditor;
using UnityEngine;

public class UnityAISettingsWindow : EditorWindow
{
    private string anthropicKey;
    private string openaiKey;
    private string geminiKey;

    // LM Studio Settings
    private bool lmStudioEnabled;
    private string lmStudioUrl;
    private string lmStudioModel;
    private string lmStudioAuthHeader;

    // MCP Settings
    private bool mcpEnabled;
    private string mcpServerCommand;
    private string mcpServerArgs;
    private string mcpServerUrl;
    private string mcpServerName;
    private bool mcpUseHttp;

    [MenuItem("Window/Unity AI Orchestrator/Settings")]
    public static void ShowWindow() => GetWindow<UnityAISettingsWindow>("Unity AI Settings");

    private void OnEnable()
    {
        anthropicKey = EditorPrefs.GetString("unityai_anthropic_api_key", "");
        openaiKey = EditorPrefs.GetString("unityai_openai_api_key", "");
        geminiKey = EditorPrefs.GetString("unityai_gemini_api_key", "");

        // Load LM Studio settings
        lmStudioEnabled = EditorPrefs.GetBool("unityai_lmstudio_enabled", false);
        lmStudioUrl = EditorPrefs.GetString("unityai_lmstudio_url", "http://localhost:1234/v1/chat/completions");
        lmStudioModel = EditorPrefs.GetString("unityai_lmstudio_model", "");
        lmStudioAuthHeader = EditorPrefs.GetString("unityai_lmstudio_auth_header", "");

        // Load MCP settings
        mcpEnabled = EditorPrefs.GetBool("unityai_mcp_enabled", false);
        mcpServerCommand = EditorPrefs.GetString("unityai_mcp_server_command", "");
        mcpServerArgs = EditorPrefs.GetString("unityai_mcp_server_args", "");
        mcpServerUrl = EditorPrefs.GetString("unityai_mcp_server_url", "");
        mcpServerName = EditorPrefs.GetString("unityai_mcp_server_name", "MCP Server");
        mcpUseHttp = EditorPrefs.GetBool("unityai_mcp_use_http", false);
    }

    private void OnGUI()
    {
        GUILayout.Label("API Keys (store only locally in EditorPrefs)", EditorStyles.boldLabel);
        anthropicKey = EditorGUILayout.TextField("Anthropic (Claude)", anthropicKey);
        openaiKey = EditorGUILayout.TextField("OpenAI (GPT)", openaiKey);
        geminiKey = EditorGUILayout.TextField("Gemini (Google)", geminiKey);

        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
        EditorGUILayout.Space(10);

        GUILayout.Label("LM Studio (Local LLM)", EditorStyles.boldLabel);
        lmStudioEnabled = EditorGUILayout.Toggle("Enable LM Studio", lmStudioEnabled);
        
        if (lmStudioEnabled)
        {
            lmStudioUrl = EditorGUILayout.TextField("API URL", lmStudioUrl);
            lmStudioModel = EditorGUILayout.TextField("Model Name (optional)", lmStudioModel);
            lmStudioAuthHeader = EditorGUILayout.TextField("Auth Header (optional)", lmStudioAuthHeader);
            EditorGUILayout.HelpBox("LM Studio typically runs on http://localhost:1234/v1/chat/completions. Leave model empty to use the currently loaded model in LM Studio.", MessageType.Info);
        }

        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
        EditorGUILayout.Space(10);

        GUILayout.Label("MCP (Model Context Protocol) Settings", EditorStyles.boldLabel);
        mcpEnabled = EditorGUILayout.Toggle("Enable MCP", mcpEnabled);
        
        if (mcpEnabled)
        {
            mcpUseHttp = EditorGUILayout.Toggle("Use HTTP (instead of stdio)", mcpUseHttp);
            mcpServerName = EditorGUILayout.TextField("Server Name", mcpServerName);
            
            if (mcpUseHttp)
            {
                mcpServerUrl = EditorGUILayout.TextField("Server URL", mcpServerUrl);
                EditorGUILayout.HelpBox("For HTTP-based MCP servers. Example: http://localhost:3000/mcp", MessageType.Info);
            }
            else
            {
                mcpServerCommand = EditorGUILayout.TextField("Server Command", mcpServerCommand);
                mcpServerArgs = EditorGUILayout.TextField("Server Arguments", mcpServerArgs);
                EditorGUILayout.HelpBox("For stdio-based MCP servers. Example command: 'node' with args: 'path/to/mcp-server.js'", MessageType.Info);
            }
        }

        EditorGUILayout.Space(10);
        
        if (GUILayout.Button("Save")) Save();
        if (GUILayout.Button("Clear keys")) 
        { 
            anthropicKey = openaiKey = geminiKey = ""; 
            Save(); 
        }
    }

    private void Save()
    {
        EditorPrefs.SetString("unityai_anthropic_api_key", anthropicKey);
        EditorPrefs.SetString("unityai_openai_api_key", openaiKey);
        EditorPrefs.SetString("unityai_gemini_api_key", geminiKey);

        // Save LM Studio settings
        EditorPrefs.SetBool("unityai_lmstudio_enabled", lmStudioEnabled);
        EditorPrefs.SetString("unityai_lmstudio_url", lmStudioUrl);
        EditorPrefs.SetString("unityai_lmstudio_model", lmStudioModel);
        EditorPrefs.SetString("unityai_lmstudio_auth_header", lmStudioAuthHeader);

        // Save MCP settings
        EditorPrefs.SetBool("unityai_mcp_enabled", mcpEnabled);
        EditorPrefs.SetString("unityai_mcp_server_command", mcpServerCommand);
        EditorPrefs.SetString("unityai_mcp_server_args", mcpServerArgs);
        EditorPrefs.SetString("unityai_mcp_server_url", mcpServerUrl);
        EditorPrefs.SetString("unityai_mcp_server_name", mcpServerName);
        EditorPrefs.SetBool("unityai_mcp_use_http", mcpUseHttp);

        Debug.Log("Unity AI settings saved in EditorPrefs.");
    }
}
