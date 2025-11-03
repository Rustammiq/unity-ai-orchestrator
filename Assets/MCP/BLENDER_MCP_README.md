# Blender 4.x MCP Server

Een Master Control Processor (MCP) server voor Blender 4.x die via JSON-RPC 2.0 communiceert met Unity AI Orchestrator.

## Installatie

### Methode 1: Stdio Server (Aanbevolen voor Unity)

1. **Locateer je Blender executable:**
   - macOS: `/Applications/Blender.app/Contents/MacOS/Blender`
   - Linux: `/usr/bin/blender` of `/opt/blender/blender`
   - Windows: `C:\Program Files\Blender Foundation\Blender 4.0\blender.exe`

2. **Update de wrapper script:**
   - Open `blender_mcp_wrapper.sh` (macOS/Linux) of maak een `.bat` bestand voor Windows
   - Update het `BLENDER_PATH` naar je Blender locatie

3. **Maak het script executable (macOS/Linux):**
   ```bash
   chmod +x Assets/MCP/blender_mcp_wrapper.sh
   ```

4. **Configureren in Unity:**
   - Open `Window > Unity AI Orchestrator > Settings`
   - Schakel "Enable MCP" in
   - Selecteer "Use HTTP" = **OFF** (stdio mode)
   - Server Command: `/bin/bash` (of `bash.exe` op Windows)
   - Server Arguments: `Assets/MCP/blender_mcp_wrapper.sh` (absolute pad)
   - Server Name: `Blender MCP`
   - Klik "Save"

### Methode 2: Als Blender Addon

1. Kopieer `blender_mcp_server.py` naar `~/.config/blender/4.0/scripts/addons/`
2. Herstart Blender
3. Ga naar Edit > Preferences > Add-ons
4. Zoek naar "MCP Server" en activeer het
5. Gebruik HTTP mode in Unity Settings

## Beschikbare Tools

De Blender MCP server ondersteunt de volgende operaties:

### Object Management
- **create_object**: Creëer primitieven (cube, sphere, plane, etc.)
- **delete_object**: Verwijder objecten
- **set_object_transform**: Positie, rotatie, schaal aanpassen

### Modifiers & Materials
- **add_modifier**: Voeg modifiers toe (subsurf, mirror, array, etc.)
- **create_material**: Creëer en assign materialen

### Import/Export
- **import_model**: Import FBX, OBJ, GLTF, USD, Alembic
- **export_model**: Export naar verschillende formaten

### Rendering
- **render_scene**: Render scene naar afbeelding
- Configuratie van Cycles/EEVEE/Workbench

### Scripting
- **execute_python**: Voer Python code uit in Blender context
- **get_scene_info**: Haal scene informatie op
- **optimize_scene**: Optimaliseer scene voor performance

### Animation
- **create_animation**: Creëer keyframe animaties
- Ondersteuning voor location, rotation, scale keyframes

### Asset & Data Management (Nieuw - gebaseerd op ID API)
- **manage_asset**: Mark/clear assets, genereer previews (ID.asset_mark/clear/generate_preview)
- **copy_data**: Dupliceer datablocks (ID.copy())
- **manage_library**: Make local, get library info (ID.make_local, ID.library)
- **get_data_info**: Comprehensive datablock info (alle ID properties)
- **manage_users**: User management (clear, find, remap) (ID.user_clear/user_of_id/user_remap)
- **preview_ensure**: Zorg voor preview images (ID.preview_ensure)

### Scene & Collection Management
- **manage_scenes**: Create/delete/switch scenes
- **manage_collections**: Create/delete collections, add/remove/link/unlink objects

### Animation & Library Overrides
- **manage_animation_data**: Create/clear animation data (ID.animation_data_create/clear)
- **create_library_override**: Create library overrides voor linked datablocks

### Mesh & UV Operations
- **mesh_operations**: Subdivide, decimate, triangulate, quad/tri conversion
- **uv_operations**: Smart UV unwrap, lightmap pack, island scaling

### Texture Baking
- **bake_textures**: Bake normal, diffuse, roughness, metallic, emission, AO, combined textures

## Voorbeeld Gebruik

### Via Unity Orchestrator

1. Open Unity en ga naar `Window > Unity AI Orchestrator`
2. Zorg dat MCP is geconfigureerd in Settings
3. Selecteer "Penta" mode (met MCP enabled)
4. Stuur een prompt zoals:

```
Create a red cube in Blender at position 2,0,0 and add a subsurf modifier
```

### Direct JSON-RPC Voorbeeld

```json
{
  "jsonrpc": "2.0",
  "id": 1,
  "method": "tools/call",
  "params": {
    "name": "create_object",
    "arguments": {
      "type": "CUBE",
      "name": "MyCube",
      "location": [2, 0, 0]
    }
  }
}
```

## Troubleshooting

### "Blender not found"
- Check of het pad naar Blender correct is in de wrapper script
- Test of Blender start: `blender --version`

### "Connection failed"
- Zorg dat Blender kan starten in background mode
- Check of er geen andere Blender instance draait die poorten blokkeert

### "Tool execution error"
- Check de Unity console voor error details
- Veel Blender operaties vereisen dat je in een scene context bent

### Performance
- Background mode is sneller dan GUI mode
- Voor complexe operaties kan het langer duren

## AI Integration

De Blender MCP server kan gebruikt worden met AI modules:

1. **Via Unity Orchestrator**: Combineer met Claude/GPT/Gemini voor AI-gestuurde Blender workflows
2. **Procedural Generation**: Gebruik AI om automatisch scenes te genereren
3. **Animation**: AI-gestuurde animatie suggesties en retargeting

## Advanced: Custom Tools Toevoegen

Voeg je eigen tools toe aan `blender_mcp_server.py`:

1. Voeg tool definitie toe aan `list_tools()` methode
2. Implementeer de tool in `call_tool()` methode
3. Maak een handler functie (bijv. `my_custom_tool()`)
4. Restart de MCP server

## Veiligheid

⚠️ **Waarschuwing**: De `execute_python` tool kan willekeurige Python code uitvoeren. Gebruik alleen in vertrouwde omgevingen.

## License

## Testen & Voorbeelden

### Basis Setup Test
1. Start Blender in achtergrondmodus met MCP-server
2. Start LM Studio met een geladen model
3. Open Unity en ga naar `Window > Coplay Orchestrator`
4. Configureer MCP in Settings (stdio mode met wrapper script)
5. Selecteer "Penta" mode en test deze prompts:

### Voorbeeld 1: Object Creëren
```
Create a red cube named "TestCube" at location (2, 0, 0)
```

### Voorbeeld 2: Materialen en Modifiers
```
Add a material to TestCube, make it red, and add a subdivision modifier
```

### Voorbeeld 3: Rendering
```
Render the current scene to //blender_render.png with 1920x1080 resolution
```

### Voorbeeld 4: Export
```
Export the scene as GLTF to //exported_scene.gltf using selection only
```

### Voorbeeld 5: Pipeline Recording
1. Klik op "Start Recording" in Unity AI Orchestrator
2. Voer meerdere Blender-acties uit via prompts
3. Klik op "Stop Recording"
4. Bekijk de opgenomen pipeline in `Assets/Pipelines/`

### Voorbeeld 6: Asset Management
```
Mark een material als asset: Mark het "RedMaterial" material als asset in catalog "Materials/Red"
```

### Voorbeeld 7: Data Duplicatie
```
Dupliceer een object: Kopieer het "Cube" object naar "CubeCopy"
```

### Voorbeeld 8: Library Management
```
Maak data lokaal: Maak het gelinkte object "LinkedCube" lokaal
```

### Voorbeeld 9: User Management
```
Zoek welke objecten material "RedPlastic" gebruiken: Vind alle gebruikers van het "RedPlastic" material
```

### Voorbeeld 10: Scene Management
```
Maak nieuwe scene: Creëer een scene genaamd "GameLevel" gebaseerd op "Scene"
```

### Voorbeeld 11: Collection Management
```
Voeg object toe aan collection: Voeg "PlayerModel" toe aan collection "Characters"
```

### Voorbeeld 12: Mesh Operations
```
Subdivideer mesh: Subdivideer het "Cube" object 2 keer
```

### Voorbeeld 13: UV Unwrapping
```
Unwrap UVs: Doe smart UV unwrap voor het "Character" object met angle limit 45
```

### Voorbeeld 14: Texture Baking
```
Bake normal map: Bake een normal map voor "BakedModel" naar "NormalMap" image (2048x2048)
```

## Unity Integration Code

```csharp
// Stuur Blender commando vanuit C# script
var mcpAdapter = FindObjectOfType<MCPAdapter>();
await mcpAdapter.SendPromptAsync("Create a spaceship model with 4 engines");

// Record in pipeline
PipelineRecorder.RecordBlenderAction("create_object", new {
    type = "CUBE",
    location = new float[] {0f, 0f, 0f},
    name = "Spaceship_Body"
});
```

## Troubleshooting

### "Connection failed"
- Controleer of Blender draait: `ps aux | grep blender`
- Controleer wrapper script: `chmod +x blender_mcp_wrapper.sh`
- Controleer pad naar Blender in wrapper script

### "Tool execution error"
- Open Blender GUI en controleer scene status
- Sommige tools vereisen een actieve scene/object

### "Python execution failed"
- Python execution is standaard uitgeschakeld in safe mode
- Gebruik alleen vertrouwde scripts

### Performance
- Achtergrondmodus is sneller dan GUI mode
- Vermijd onnodige scene updates in scripts

Deel van Unity AI Orchestrator Plugin - gebruik zoals gedefinieerd in de hoofdlicentie.

