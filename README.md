# Unity AI Orchestrator Plugin 🚀

[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)
[![Unity](https://img.shields.io/badge/Unity-2020.3+-blue.svg)](https://unity.com/)
[![Blender](https://img.shields.io/badge/Blender-4.x-orange.svg)](https://www.blender.org/)

**Unity AI Orchestrator** is een revolutionaire Unity plugin die vier LLM-providers (Claude, OpenAI GPT, Gemini Pro 2.5, LM Studio) combineert met professionele 3D workflows via Blender MCP integratie. Transformeer natuurlijke taal naar Unity en Blender operaties!

## ✨ Features

### 🤖 Multi-Model AI Orchestration
- **Claude** (Anthropic) - Uitstekende code generation
- **GPT-4** (OpenAI) - Algemene AI taken
- **Gemini Pro 2.5** (Google) - Snelle responses
- **LM Studio** - Lokale, private AI modellen

### 🎨 Blender MCP Integration
- **25 professionele tools** voor complete 3D workflows
- Asset management, mesh operations, UV unwrapping, texture baking
- Scene & collection management, animation data, library overrides
- Directe integratie met Blender 4.x via JSON-RPC 2.0

### 🔧 Advanced Features
- **Pipeline Recording** - Record en replay workflows
- **Dynamic Orchestrator Modes** - Solo, Dual, Tri, Quad, Penta
- **Real-time Collaboration** - Multi-model consensus
- **Safety Controls** - Configurable safe mode voor Python execution

## 📦 Installation Guide

### Stap 1: Unity Plugin Installatie

#### Methode A: Direct Download (Aanbevolen)
```bash
# Download de laatste release van GitHub
# https://github.com/Rustammiq/unity-ai-orchestrator/releases

# Of clone de repository
git clone https://github.com/Rustammiq/unity-ai-orchestrator.git
```

#### Methode B: Unity Package Manager
1. Open Unity (2020.3 of hoger)
2. Ga naar `Window > Package Manager`
3. Klik op `+` > `Add package from git URL`
4. Voer in: `https://github.com/Rustammiq/unity-ai-orchestrator.git`
5. Klik `Add`

#### Methode C: Manual Installation
1. Download het ZIP bestand van [GitHub Releases](https://github.com/Rustammiq/unity-ai-orchestrator/releases)
2. Unzip het bestand
3. Kopieer de `Assets` map naar je Unity project root
4. Import de nieuwe assets in Unity

### Stap 2: API Keys Configuratie

#### Claude (Anthropic) Setup
1. Ga naar [Anthropic Console](https://console.anthropic.com/)
2. Maak een nieuwe API key aan
3. Kopieer de API key

#### OpenAI Setup
1. Ga naar [OpenAI Platform](https://platform.openai.com/api-keys)
2. Maak een nieuwe API key aan
3. Kopieer de API key

#### Google Gemini Setup
1. Ga naar [Google AI Studio](https://makersuite.google.com/app/apikey)
2. Maak een nieuwe API key aan
3. Kopieer de API key

#### In Unity:
1. Open Unity Editor
2. Ga naar `Window > Unity AI Orchestrator > Settings`
3. Vul je API keys in:
   - **Anthropic API Key**: Je Claude API key
   - **OpenAI API Key**: Je GPT API key
   - **Gemini API Key**: Je Google API key
4. Klik `Save Settings`

### Stap 3: LM Studio Setup (Optioneel - Voor Lokale AI)

LM Studio geeft je privacy en geen API kosten!

#### Installatie:
1. Download [LM Studio](https://lmstudio.ai/) voor je platform
2. Installeer en start LM Studio
3. Ga naar de "Discover" tab
4. Download een model, bijvoorbeeld:
   - **Llama 3.1 8B** (goed voor algemeen gebruik)
   - **Mistral 7B** (goed voor code)
   - **Phi-3 Mini** (klein maar krachtig)

#### Configuratie:
1. Klik op "Local Server" in LM Studio
2. Klik "Start Server"
3. Noteer de server URL (meestal `http://localhost:1234`)

#### In Unity:
1. Ga naar `Window > Unity AI Orchestrator > Settings`
2. Schakel "Enable LM Studio" aan
3. API URL: `http://localhost:1234/v1/chat/completions`
4. Model Name: Laat leeg (gebruikt automatisch het geladen model)
5. Klik `Save Settings`

Zie `Assets/Config/lmstudio_example.md` voor gedetailleerde instructies.

### Stap 4: Blender MCP Setup (Optioneel - Voor Professionele 3D Workflows)

De Blender MCP server geeft je **25 professionele 3D tools** voor complete workflows!

#### Blender Installatie:
1. Download [Blender 4.x](https://www.blender.org/download/) voor je platform
2. Zorg dat Python 3.x is geïnstalleerd (komt standaard met Blender)

#### MCP Server Configuratie:
1. Open Unity Editor
2. Ga naar `Window > Unity AI Orchestrator > Settings`
3. Schakel "Enable MCP" aan
4. Kies tussen twee modi:

   **Stdio Mode (Aanbevolen voor Blender):**
   - **Use HTTP**: Uitgeschakeld
   - **Server Command**: `python3` (of volledig pad naar Python)
   - **Server Args**: `Assets/MCP/blender_mcp_server.py`
   - **Server Name**: `Blender MCP Server`

   **HTTP Mode (Voor web servers):**
   - **Use HTTP**: Aangeschakeld
   - **Server URL**: `http://localhost:PORT/mcp`
   - **Server Name**: `HTTP MCP Server`

#### Wrapper Scripts (Gemakkelijkere Setup):
Voor eenvoudig gebruik zijn er wrapper scripts:
- **Mac/Linux**: `Assets/MCP/blender_mcp_wrapper.sh`
- **Windows**: `Assets/MCP/blender_mcp_wrapper.bat`

```bash
# Maak executable (Linux/Mac)
chmod +x Assets/MCP/blender_mcp_wrapper.sh

# Gebruik als server command
# Server Command: ./Assets/MCP/blender_mcp_wrapper.sh
# Server Args: (leeg laten)
```

#### Test MCP Connection:
1. Open `Window > Unity AI Orchestrator`
2. Selecteer "Penta" mode (verschijnt automatisch met MCP)
3. Probeer een commando:
   ```
   Create a red cube in Blender and add a subsurface modifier
   ```
4. Of meer complexe workflows:
   ```
   Set up a game asset pipeline: create a character model, UV unwrap it, and bake normal maps
   ```

#### Blender MCP Features:
- ✅ **Asset Management**: Mark/clear assets, generate previews
- ✅ **Mesh Operations**: Subdivide, decimate, triangulate
- ✅ **UV Operations**: Smart unwrap, lightmap pack
- ✅ **Texture Baking**: Normal, diffuse, roughness, metallic maps
- ✅ **Scene Management**: Create/switch/delete scenes
- ✅ **Collection Management**: Organize objects in collections
- ✅ **Library Operations**: Make local, create overrides
- ✅ **Animation Data**: Create/clear animation data
- ✅ **User Management**: Find datablock users, remap references

Zie `Assets/MCP/BLENDER_MCP_README.md` voor alle 25 tools en voorbeelden.

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

## 🎮 Gebruik

### Stap 1: Open de Orchestrator
1. Open Unity Editor
2. Ga naar `Window > Unity AI Orchestrator`
3. Kies je gewenste mode (zie hieronder)

### Orchestrator Modes

| Mode | Modellen | Gebruik |
|------|----------|---------|
| **Solo** | Claude | Snelle, accurate code generation |
| **Dual** | Claude + GPT | Consensus voor belangrijke beslissingen |
| **Tri** | Claude + GPT + Gemini | Uitgebreide vergelijking en optimalisatie |
| **Quad** | Claude + GPT + Gemini + LM Studio | Inclusief lokale AI (geen API kosten) |
| **Penta** | Alle bovenstaande + MCP | **Volledige workflow automation** |

*Modes worden dynamisch aangepast op basis van welke providers zijn ingeschakeld.*

### Stap 2: Begin met Prompts

#### Voor Unity Scripts:
```
Create a player movement script with WASD controls and jumping
```

#### Voor Game Objects:
```
Create a complete level with platforms, enemies, and collectibles
```

#### Voor Blender MCP (als geconfigureerd):
```
Create a low-poly game character model with proper UV mapping
Bake normal and diffuse maps for the character
```

### Stap 3: Pipeline Recording
1. Klik "Start Recording" voor complexe workflows
2. Voer meerdere operaties uit
3. Klik "Stop Recording"
4. Opslaan als JSON bestand voor replay

**Replay pipelines:**
```csharp
PipelineRecorder.Replay("Assets/Pipelines/my_workflow.json");
```

## Mappenstructuur

```
Assets/
├── Editor/          # Alle C# scripts (adapters, orchestrator, UI)
├── MCP/            # MCP configuratie en documentatie
├── Config/         # Configuratie voorbeelden (LM Studio, etc.)
└── Pipelines/      # Opgeslagen pipeline recordings (automatisch aangemaakt)
```

## 💡 Gebruik Voorbeelden

### 🎯 Eenvoudige Voorbeelden

**Unity Scripting:**
```
Create a camera follow script that smoothly follows the player
```

**Blender Modeling:**
```
Create a table with 4 legs and a wooden texture
```

**Game Design:**
```
Set up a basic enemy AI that patrols between waypoints
```

### 🚀 Geavanceerde Voorbeelden

**Complete Game Level:**
```
Create a 2D platformer level with moving platforms, spikes, and a flag at the end
Add collision detection and player checkpoint system
```

**Character Pipeline:**
```
Create a fantasy character with armor pieces
Set up LOD levels and optimize for mobile
Bake textures and create material variants
```

**Blender to Unity Workflow:**
```
Model a weapon in Blender with proper topology
UV unwrap and bake PBR textures
Export as FBX with materials
Import into Unity and set up physics
```

## 🔧 Troubleshooting

### Veelvoorkomende Problemen

#### API Key Errors
- Controleer of je API keys correct zijn gekopieerd
- Zorg dat er geen extra spaties zijn
- Check je account limits op de provider websites

#### MCP Connection Issues
- Controleer of de MCP server correct draait
- Test met een eenvoudig commando eerst
- Check console logs voor foutmeldingen

#### LM Studio Connection
- Zorg dat LM Studio server draait op poort 1234
- Controleer of een model is geladen
- Test de API endpoint in browser eerst

#### Blender MCP Issues
- Zorg dat Blender 4.x is geïnstalleerd
- Controleer Python path in wrapper script
- Test Blender MCP server standalone eerst

### Debug Mode
1. Open `Window > Unity AI Orchestrator > Settings`
2. Schakel debug logging aan (indien beschikbaar)
3. Check Unity Console voor gedetailleerde foutmeldingen

## 📚 Meer Informatie

- **Blender MCP Tools**: `Assets/MCP/BLENDER_MCP_README.md`
- **LM Studio Setup**: `Assets/Config/lmstudio_example.md`
- **GitHub MCP Example**: `Assets/MCP/github_mcp_example.py`
- **API Documentation**: Zie provider websites voor limits en features

## 🤝 Bijdragen

Issues, feature requests en pull requests zijn welkom!

1. Fork de repository
2. Maak een feature branch (`git checkout -b feature/amazing-feature`)
3. Commit je changes (`git commit -m 'Add amazing feature'`)
4. Push naar branch (`git push origin feature/amazing-feature`)
5. Open een Pull Request

## ⚠️ Belangrijke Notities

- **API Keys**: Vervang placeholders en controleer endpoints voor jouw account
- **Veiligheid**: Gebruik alleen in veilige ontwikkelomgeving; sla nooit keys in git
- **LM Studio**: Zorg dat server draait en model geladen is voordat je prompts verstuurt
- **MCP Servers**: Kunnen extra configuratie vereisen - zie MCP documentatie
- **Blender**: Vereist Blender 4.x met Python support voor MCP server
- **Pipelines**: Worden opgeslagen in `Assets/Pipelines` als JSON

## 📄 Licentie

Dit project is gelicenseerd onder de MIT License - zie het [LICENSE](LICENSE) bestand voor details.

---

**Made with ❤️ for Unity & Blender developers**
