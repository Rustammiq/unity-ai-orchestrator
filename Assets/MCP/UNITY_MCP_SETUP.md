# Unity MCP Setup Guide - Alle Servers Werkend

## ? Wat is Verbeterd

1. **Environment Variabelen Ondersteuning**: MCPClient ondersteunt nu environment variabelen
2. **Betere Argument Parsing**: Ondersteunt zowel enkele strings als arrays van argumenten
3. **Verbeterde Error Handling**: Betere logging en error messages
4. **Process Monitoring**: Controleert of processen nog draaien
5. **Stderr Reading**: Leest error output voor betere debugging

## ?? Configuratie Voorbeelden

### 1. GitHub MCP Server (Python)

**Unity Settings:**
- **Enable MCP**: ?
- **Use HTTP**: ? (unchecked)
- **Server Command**: `python3`
- **Server Arguments**: `/pad/naar/project/Assets/MCP/github_mcp_example.py`
- **Server Name**: `GitHub MCP`

**Voorbeeld pad aanpassen:**
```
/Users/innovars_lab/Downloads/coplay_orchestrator_v4/Assets/MCP/github_mcp_example.py
```

### 2. Blender MCP Server (via Blender)

**Unity Settings:**
- **Enable MCP**: ?
- **Use HTTP**: ? (unchecked)
- **Server Command**: `/Applications/Blender.app/Contents/MacOS/Blender`
- **Server Arguments**: `--background --python /pad/naar/project/Assets/MCP/blender_mcp_server.py`
- **Server Name**: `Blender MCP`

**Voorbeeld:**
```
Server Command: /Applications/Blender.app/Contents/MacOS/Blender
Server Arguments: --background --python /Users/innovars_lab/Downloads/coplay_orchestrator_v4/Assets/MCP/blender_mcp_server.py
```

### 3. Blender MCP via Wrapper Script

**Unity Settings:**
- **Enable MCP**: ?
- **Use HTTP**: ? (unchecked)
- **Server Command**: `/bin/bash`
- **Server Arguments**: `/pad/naar/project/Assets/MCP/blender_mcp_wrapper.sh`
- **Server Name**: `Blender MCP Wrapper`

**Belangrijk**: Zorg dat `blender_mcp_wrapper.sh` uitvoerbaar is:
```bash
chmod +x Assets/MCP/blender_mcp_wrapper.sh
```

En update het Blender pad in `blender_mcp_wrapper.sh`.

### 4. NPX-gebaseerde MCP Servers

Voor servers die via `npx` draaien (zoals Composio, Brave Search, etc.):

**Unity Settings:**
- **Enable MCP**: ?
- **Use HTTP**: ? (unchecked)
- **Server Command**: `npx`
- **Server Arguments**: `-y @composio/mcp@latest start --url https://...`
- **Server Name**: `Composio MCP`

**Let op**: Environment variabelen kunnen momenteel alleen via code worden toegevoegd. 
Voor production gebruik, voeg environment variabelen toe aan de Settings window (toekomstige feature).

### 5. HTTP-gebaseerde MCP Servers

Voor servers die via HTTP draaien:

**Unity Settings:**
- **Enable MCP**: ?
- **Use HTTP**: ? (checked)
- **Server URL**: `https://mcp.context7.com/mcp`
- **Server Name**: `Context7 MCP`

## ?? Troubleshooting

### "Process exited" Error

1. **Check het pad naar het commando**:
   - Zorg dat het pad absoluut is of in PATH staat
   - Test het commando in terminal: `which python3` of `which npx`

2. **Check script paden**:
   - Alle paden moeten absoluut zijn (begin met `/`)
   - Test of het script bestaat: `ls -la /pad/naar/script.py`

3. **Check permissions**:
   ```bash
   chmod +x Assets/MCP/blender_mcp_wrapper.sh
   ```

### "Timeout" Error

1. **Check Unity Console** voor error messages
2. **Check process output**: Kijk in Unity Console voor "MCPClient: Process error" messages
3. **Test server standalone**: Run het commando handmatig in terminal

### "Connection failed" Error

1. **Check initialization**:
   - Kijk in Unity Console voor "MCPClient: Started process" message
   - Check "MCPClient: Process started successfully"

2. **Check JSON parsing**:
   - Kijk voor "MCPClient: Failed to parse response" warnings
   - Check of de server JSON-RPC 2.0 compliant is

## ?? Testen

1. **Open Unity Console** (Window > General > Console)
2. **Configureer MCP** in Settings
3. **Test een prompt** via Unity AI Orchestrator
4. **Check de logs**:
   - `MCPClient: Started process...` - Process start
   - `MCPClient: Sending request...` - Request verzonden
   - `MCPClient: Received response...` - Response ontvangen
   - `MCPClient: Process error...` - Errors van server

## ?? Environment Variabelen (Toekomst)

Voor nu kunnen environment variabelen alleen via code worden toegevoegd. 
In de toekomst voegen we een UI toe in de Settings window om environment variabelen te configureren.

Voor nu, als je environment variabelen nodig hebt:
- Gebruik een wrapper script dat de environment variabelen set
- Of pas de code aan in `UnityAIOrchestratorWindow.cs` om env vars door te geven

## ?? Best Practices

1. **Gebruik absolute paden** voor alle commando's en scripts
2. **Test servers standalone** voordat je ze in Unity configureert
3. **Check Unity Console** voor gedetailleerde logs
4. **Start met eenvoudige servers** (zoals GitHub example) voordat je complexere probeert
5. **Gebruik wrapper scripts** voor servers die complexe setup nodig hebben

## ?? Voorbeelden

### Python Server (GitHub Example)
```bash
# Test standalone:
python3 /pad/naar/github_mcp_example.py

# In Unity Settings:
Command: python3
Args: /pad/naar/github_mcp_example.py
```

### Blender Server
```bash
# Test standalone:
/Applications/Blender.app/Contents/MacOS/Blender --background --python /pad/naar/blender_mcp_server.py

# In Unity Settings:
Command: /Applications/Blender.app/Contents/MacOS/Blender
Args: --background --python /pad/naar/blender_mcp_server.py
```

### NPX Server
```bash
# Test standalone:
npx -y @composio/mcp@latest start --url https://...

# In Unity Settings:
Command: npx
Args: -y @composio/mcp@latest start --url https://...
```

## ? Checklist

Voordat je een MCP server gebruikt:

- [ ] Server commando werkt standalone in terminal
- [ ] Script paden zijn absoluut en bestaan
- [ ] Permissions zijn correct (voor scripts)
- [ ] Environment variabelen zijn gezet (indien nodig)
- [ ] Unity Console is open voor debugging
- [ ] MCP is enabled in Unity Settings
- [ ] Server command en args zijn correct geconfigureerd
