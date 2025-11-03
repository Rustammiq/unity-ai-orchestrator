# Unity AI Orchestrator Plugin

Deze plugin integreert vier LLM-providers (Claude, OpenAI GPT, Gemini Pro 2.5, LM Studio) via een centrale orchestrator,
plus command-to-action mapping, undo/redo, pipeline recording, en **volledige Blender MCP integratie** voor 3D-workflows.

## Installatie
1. Download de nieuwste release van GitHub.
2. Kopieer de map `Assets` naar de root van je Unity project.
3. Open Unity en ga naar `Window > Unity AI Orchestrator`.

## Configuratie

### API Keys
- Open `Window > Unity AI Orchestrator > Settings` om je API-keys in te vullen.
- Standaard opgeslagen in EditorPrefs onder:
  - `unityai_anthropic_api_key`
  - `unityai_openai_api_key`
  - `unityai_gemini_api_key`

### LM Studio (Lokale LLM) Configuratie
LM Studio is een gratis lokale LLM server - perfect voor privacy en geen API costs!

1. Download en installeer [LM Studio](https://lmstudio.ai/)
2. Open LM Studio en download een model (bijv. Llama 3, Mistral, etc.)
3. Start de lokale server in LM Studio (klik op "Local Server")
4. In Unity: `Window > Unity AI Orchestrator > Settings`
   - Schakel "Enable LM Studio" in
   - API URL: `http://localhost:1234/v1/chat/completions` (standaard)
   - Model Name: Laat leeg om het geladen model te gebruiken
5. Klik op "Save"

Zie `Assets/Config/lmstudio_example.md` voor gedetailleerde instructies.

### MCP (Model Context Protocol) Configuratie
De plugin ondersteunt nu MCP servers voor extra functionaliteit:

1. Open `Window > Unity AI Orchestrator > Settings`
2. Schakel "Enable MCP" in
3. Kies tussen twee modi:
   - **stdio**: Voor lokale MCP servers die via command-line draaien
     - Server Command: bijv. `node`, `python`, `/bin/bash`, etc.
     - Server Arguments: bijv. `path/to/mcp-server.js` of wrapper script
   - **HTTP**: Voor HTTP-gebaseerde MCP servers
     - Server URL: bijv. `http://localhost:3000/mcp`
4. Vul een Server Name in voor identificatie
5. Klik op "Save"

In de Orchestrator window verschijnt automatisch een "Penta" mode wanneer MCP is geconfigureerd.

#### Blender 4.x MCP Server
Een complete Blender MCP server is inbegrepen! Zie `Assets/MCP/BLENDER_MCP_README.md` voor installatie.

**Blender MCP Features:**
- ✅ Creëer en beheer 3D objecten (primitieven, meshes)
- ✅ Import/Export van modellen (FBX, OBJ, GLTF, USD, Alembic)
- ✅ Materialen en modifiers (subsurf, mirror, array, etc.)
- ✅ Rendering (Cycles/EEVEE) met customizable instellingen
- ✅ Animation keyframes en keyframe management
- ✅ Python scripting (veiligheidsmodus beschikbaar)
- ✅ Scene optimalisatie en cleanup
- ✅ Pipeline recording voor Blender-acties
- ✅ **Asset Management** (mark/clear/generate previews using ID.asset_*)
- ✅ **Data Duplicatie** (copy() voor alle datablocks)
- ✅ **Library Management** (make_local, library info using ID properties)
- ✅ **Data Informatie** (comprehensive ID properties: users, library, assets, etc.)
- ✅ **User Management** (user_clear, user_of_id, user_remap)
- ✅ **Scene & Collection Management** (create/delete/switch scenes, collection operations)
- ✅ **Animation Data Management** (create/clear animation data)
- ✅ **Library Overrides** (create overrides for linked datablocks)
- ✅ **Mesh Operations** (subdivide, decimate, triangulate, quad/tri conversion)
- ✅ **UV Operations** (smart unwrap, lightmap pack, island scaling)
- ✅ **Texture Baking** (normal, diffuse, roughness, metallic, emission, AO, combined)

## Gebruik

### Orchestrator Modes
- **Solo**: Alleen Claude
- **Dual**: Claude + OpenAI
- **Tri**: Claude + OpenAI + Gemini
- **Quad**: Alle drie + LM Studio (wanneer geconfigureerd)
- **Penta**: Alle vier + MCP (wanneer geconfigureerd)

*Modes worden dynamisch aangepast op basis van welke providers zijn ingeschakeld.*

### Pipeline Recording
- Klik op "Start Recording" om acties vast te leggen
- Alle uitgevoerde commando's worden opgeslagen als JSON in `Assets/Pipelines/`
- Gebruik `PipelineRecorder.Replay(path)` om pipelines te herhalen

## Mappenstructuur

```
Assets/
├── Editor/          # Alle C# scripts (adapters, orchestrator, UI)
├── MCP/            # MCP configuratie en documentatie
├── Config/         # Configuratie voorbeelden (LM Studio, etc.)
└── Pipelines/      # Opgeslagen pipeline recordings (automatisch aangemaakt)
```

## Belangrijk
- Vervang placeholders en controleer endpoints / request-formats voor jouw account.
- Gebruik deze plugin alleen in een veilige ontwikkelomgeving; sla nooit keys in git.
- Voor LM Studio: zorg dat LM Studio draait en een model is geladen voordat je prompts verstuurt.
- Voor MCP servers: zorg dat je server draait voordat je prompts verstuurt.
- Pipelines worden opgeslagen in `Assets/Pipelines` als JSON en kunnen herhaald worden via `PipelineRecorder.Replay(path)`.

## Voordelen van LM Studio
- ✅ Volledig lokaal (geen API costs)
- ✅ Privacy (data blijft op je computer)
- ✅ Snelle response tijden (afhankelijk van je hardware)
- ✅ Offline werken mogelijk
- ✅ Gratis te gebruiken
