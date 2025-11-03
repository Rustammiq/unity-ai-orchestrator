# MCP (Model Context Protocol) Configuratie

Deze map bevat voorbeelden en documentatie voor MCP server configuratie.

## Blender 4.x MCP Server

Een complete MCP server voor Blender 4.x is nu beschikbaar! Zie [BLENDER_MCP_README.md](./BLENDER_MCP_README.md) voor volledige installatie-instructies.

**Snelle start:**
1. Update `blender_mcp_wrapper.sh` (macOS/Linux) of `blender_mcp_wrapper.bat` (Windows) met je Blender pad
2. Configureer in Unity Settings: stdio mode, gebruik de wrapper script
3. Genereer 3D scenes via AI via Unity Orchestrator!

## GitHub MCP Server (Voorbeeld)

Een voorbeeld MCP server voor GitHub integratie: [github_mcp_example.py](./github_mcp_example.py)

**Features:**
- Issue aanmaken
- Repository zoeken
- Repository informatie opvragen
- Pull request management (uitbreidbaar)

**Configuratie:**
```bash
export GITHUB_TOKEN=your_github_token
python github_mcp_example.py
```

## MCP Servers

MCP servers kunnen via twee methoden worden gebruikt:

### 1. stdio (Command-line)
Voor lokale MCP servers die via command-line draaien.

**Voorbeeld configuratie:**
- Server Command: `node`
- Server Arguments: `/path/to/your/mcp-server.js`

### 2. HTTP
Voor HTTP-gebaseerde MCP servers.

**Voorbeeld configuratie:**
- Server URL: `http://localhost:3000/mcp`

## Populaire MCP Servers

- **Filesystem MCP**: Interactie met bestandssysteem
- **GitHub MCP**: GitHub API integratie
- **Database MCP**: Database queries
- **Custom MCP**: Maak je eigen MCP server

## Configuratie in Unity

Open `Window > Coplay Orchestrator > Settings` en schakel MCP in om je server te configureren.

## Troubleshooting

- **Connection failed**: Controleer of je MCP server daadwerkelijk draait
- **Timeout errors**: Verhoog timeout in MCPClient.cs indien nodig
- **Parse errors**: Check of je server JSON-RPC 2.0 gebruikt
