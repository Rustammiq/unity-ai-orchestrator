#!/bin/bash
# Blender MCP Server Wrapper Script
# This script runs the Blender MCP server in background mode

# Configuration
BLENDER_PATH="/Applications/Blender.app/Contents/MacOS/Blender"  # macOS default
# BLENDER_PATH="/usr/bin/blender"  # Linux default
# BLENDER_PATH="C:\Program Files\Blender Foundation\Blender 4.0\blender.exe"  # Windows default

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
MCP_SERVER="$SCRIPT_DIR/blender_mcp_server.py"

# Check if Blender exists
if [ ! -f "$BLENDER_PATH" ]; then
    echo "Error: Blender not found at $BLENDER_PATH" >&2
    echo "Please update BLENDER_PATH in this script to point to your Blender executable" >&2
    exit 1
fi

# Run Blender in background mode with the MCP server
exec "$BLENDER_PATH" --background --python "$MCP_SERVER"

