# MCP Configuratie voor Unity

## Automatisch toevoegen

Run het Python script om automatisch de configuraties toe te voegen:

```bash
python3 add_mcp_config.py
```

Dit script:
- Voegt nieuwe Unity MCP configuraties toe aan `~/.cursor/mcp.json`
- Maakt automatisch een backup
- Overschrijft bestaande entries met dezelfde naam

## Handmatig toevoegen

Voeg deze entries toe aan `~/.cursor/mcp.json` onder het `mcpServers` object:

### Blender MCP Server

```json
"unity-blender-mcp-v2": {
  "_doc": "Unity Blender MCP Server - Verbeterde versie",
  "command": "/Applications/Blender.app/Contents/MacOS/Blender",
  "args": [
    "--background",
    "--python",
    "/Users/innovars_lab/Downloads/coplay_orchestrator_v4/Assets/MCP/blender_mcp_server.py"
  ],
  "env": {},
  "disabled": false
}
```

### GitHub MCP Server (Updated)

```json
"unity-github-mcp-updated": {
  "_doc": "Unity GitHub MCP Server - Updated versie",
  "command": "python3",
  "args": [
    "/Users/innovars_lab/Downloads/coplay_orchestrator_v4/Assets/MCP/github_mcp_example.py"
  ],
  "env": {
    "GITHUB_TOKEN": ""
  },
  "disabled": false
}
```

## Belangrijke aanpassingen

?? **Pas de volgende paden aan naar jouw systeem:**

1. **Blender pad**: 
   - macOS: `/Applications/Blender.app/Contents/MacOS/Blender`
   - Linux: `/usr/bin/blender` of jouw pad
   - Windows: `C:\\Program Files\\Blender Foundation\\Blender 4.0\\blender.exe`

2. **Script paden**: 
   - Update `/Users/innovars_lab/Downloads/coplay_orchestrator_v4/` naar jouw project locatie

3. **GitHub Token** (optioneel):
   - Als je GitHub functionaliteit wilt gebruiken, voeg je GitHub token toe in `env.GITHUB_TOKEN`

## Testen

Na het toevoegen:

1. Herstart Cursor
2. Test de MCP verbinding via Cursor's MCP interface
3. In Unity: Configureer MCP in `Window > Unity AI Orchestrator > Settings`

## Troubleshooting

- **"Process exited"**: Controleer of Blender pad correct is
- **"Timeout"**: Check of de Python script paden kloppen
- **"Connection failed"**: Zorg dat MCP enabled is in Unity Settings
