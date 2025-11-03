@echo off
REM Blender MCP Server Wrapper Script for Windows
REM This script runs the Blender MCP server in background mode

REM Configuration - Update this path to your Blender installation
set BLENDER_PATH=C:\Program Files\Blender Foundation\Blender 4.0\blender.exe

REM Get script directory
set SCRIPT_DIR=%~dp0
set MCP_SERVER=%SCRIPT_DIR%blender_mcp_server.py

REM Check if Blender exists
if not exist "%BLENDER_PATH%" (
    echo Error: Blender not found at %BLENDER_PATH%
    echo Please update BLENDER_PATH in this script to point to your Blender executable
    exit /b 1
)

REM Run Blender in background mode with the MCP server
"%BLENDER_PATH%" --background --python "%MCP_SERVER%"

