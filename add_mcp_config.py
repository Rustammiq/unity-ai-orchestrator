#!/usr/bin/env python3
"""
Script om Unity MCP configuraties toe te voegen aan Cursor mcp.json
"""
import json
import os
from pathlib import Path

# Pad naar mcp.json
MCP_JSON_PATH = Path.home() / ".cursor" / "mcp.json"

# Nieuwe configuraties om toe te voegen
NEW_CONFIGS = {
    "unity-blender-mcp-v2": {
        "_doc": "Unity Blender MCP Server - Verbeterde versie",
        "command": "/Applications/Blender.app/Contents/MacOS/Blender",
        "args": [
            "--background",
            "--python",
            "/Users/innovars_lab/Downloads/coplay_orchestrator_v4/Assets/MCP/blender_mcp_server.py"
        ],
        "env": {},
        "disabled": False
    },
    "unity-github-mcp-updated": {
        "_doc": "Unity GitHub MCP Server - Updated versie",
        "command": "python3",
        "args": [
            "/Users/innovars_lab/Downloads/coplay_orchestrator_v4/Assets/MCP/github_mcp_example.py"
        ],
        "env": {
            "GITHUB_TOKEN": ""
        },
        "disabled": False
    }
}

def update_mcp_json():
    """Update mcp.json met nieuwe configuraties"""
    
    # Lees bestaande config
    if MCP_JSON_PATH.exists():
        with open(MCP_JSON_PATH, 'r', encoding='utf-8') as f:
            config = json.load(f)
    else:
        print(f"Error: {MCP_JSON_PATH} niet gevonden!")
        print(f"Maak eerst het bestand aan of controleer het pad.")
        return False
    
    # Voeg nieuwe configs toe (overschrijf bestaande met dezelfde naam)
    if "mcpServers" not in config:
        config["mcpServers"] = {}
    
    updated = False
    for name, new_config in NEW_CONFIGS.items():
        if name not in config["mcpServers"]:
            config["mcpServers"][name] = new_config
            print(f"? Toegevoegd: {name}")
            updated = True
        else:
            # Update bestaande config
            old_config = config["mcpServers"][name]
            if old_config.get("disabled", False) and not new_config.get("disabled", False):
                config["mcpServers"][name] = new_config
                print(f"? Ge?pdatet: {name} (was disabled, nu enabled)")
                updated = True
            else:
                print(f"- Overslaan: {name} (bestaat al en is enabled)")
    
    if updated:
        # Maak backup
        backup_path = MCP_JSON_PATH.with_suffix('.json.backup')
        if MCP_JSON_PATH.exists():
            import shutil
            shutil.copy2(MCP_JSON_PATH, backup_path)
            print(f"\n? Backup gemaakt: {backup_path}")
        
        # Schrijf nieuwe config
        with open(MCP_JSON_PATH, 'w', encoding='utf-8') as f:
            json.dump(config, f, indent=2, ensure_ascii=False)
        
        print(f"\n? {MCP_JSON_PATH} is bijgewerkt!")
        print("\n??  Let op: Pas de paden aan in de configuratie als nodig:")
        print("   - Blender pad kan anders zijn op jouw systeem")
        print("   - Script paden moeten overeenkomen met jouw project locatie")
        return True
    else:
        print("\n- Geen wijzigingen nodig.")
        return False

if __name__ == "__main__":
    print("Unity MCP Configuratie Tool")
    print("=" * 50)
    print(f"Updating: {MCP_JSON_PATH}\n")
    
    try:
        update_mcp_json()
    except Exception as e:
        print(f"\n? Error: {e}")
        import traceback
        traceback.print_exc()
