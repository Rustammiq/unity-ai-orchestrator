#!/usr/bin/env python3
"""
Blender 4.x Master Control Processor (MCP) Server
JSON-RPC 2.0 server for managing Blender projects, assets, and workflows.
Can be run as stdio server or integrated into Blender as an addon.

Usage:
    python blender_mcp_server.py
    OR in Blender: Install as addon and enable MCP Server addon
"""

import sys
import json
import re
import bpy
import bmesh
from typing import Dict, List, Any, Optional
import traceback

class BlenderMCPServer:
    """MCP Server for Blender 4.x - handles all Blender operations via JSON-RPC 2.0"""

    def __init__(self):
        self.request_id = None
        self.version = "1.0.0"
        self.safe_mode = True  # Beveiligde modus standaard aan
        self.capabilities = {
            "tools": {
                "listChanged": True
            },
            "resources": {},
            "prompts": {}
        }
    
    def handle_request(self, request: Dict[str, Any]) -> Dict[str, Any]:
        """Handle incoming JSON-RPC 2.0 request"""
        try:
            jsonrpc = request.get("jsonrpc", "2.0")
            method = request.get("method", "")
            params = request.get("params", {})
            request_id = request.get("id")
            
            self.request_id = request_id
            
            # Handle initialization
            if method == "initialize":
                return self.initialize(params)
            
            # Handle tools
            elif method == "tools/list":
                return self.list_tools()
            
            elif method == "tools/call":
                return self.call_tool(params)
            
            # Handle notifications
            elif method == "notifications/initialized":
                return {"jsonrpc": "2.0", "result": None}
            
            else:
                return self.error_response(-32601, f"Method not found: {method}")
        
        except Exception as e:
            return self.error_response(-32603, f"Internal error: {str(e)}", traceback.format_exc())
    
    def initialize(self, params: Dict[str, Any]) -> Dict[str, Any]:
        """Initialize MCP protocol"""
        return {
            "jsonrpc": "2.0",
            "id": self.request_id,
            "result": {
                "protocolVersion": "2024-11-05",
                "capabilities": self.capabilities,
                "serverInfo": {
                    "name": "blender-mcp-server",
                    "version": self.version
                }
            }
        }
    
    def list_tools(self) -> Dict[str, Any]:
        """List all available Blender tools"""
        tools = [
            {
                "name": "create_object",
                "description": "Create a primitive object (cube, sphere, plane, etc.)",
                "inputSchema": {
                    "type": "object",
                    "properties": {
                        "type": {"type": "string", "enum": ["CUBE", "SPHERE", "PLANE", "CYLINDER", "CONE", "TORUS"]},
                        "name": {"type": "string"},
                        "location": {"type": "array", "items": {"type": "number"}, "minItems": 3, "maxItems": 3}
                    },
                    "required": ["type"]
                }
            },
            {
                "name": "delete_object",
                "description": "Delete an object by name",
                "inputSchema": {
                    "type": "object",
                    "properties": {
                        "name": {"type": "string"}
                    },
                    "required": ["name"]
                }
            },
            {
                "name": "set_object_transform",
                "description": "Set position, rotation, or scale of an object",
                "inputSchema": {
                    "type": "object",
                    "properties": {
                        "name": {"type": "string"},
                        "location": {"type": "array", "items": {"type": "number"}},
                        "rotation": {"type": "array", "items": {"type": "number"}},
                        "scale": {"type": "array", "items": {"type": "number"}}
                    },
                    "required": ["name"]
                }
            },
            {
                "name": "add_modifier",
                "description": "Add a modifier to an object",
                "inputSchema": {
                    "type": "object",
                    "properties": {
                        "object_name": {"type": "string"},
                        "modifier_type": {"type": "string", "enum": ["SUBSURF", "MIRROR", "ARRAY", "BEVEL", "SOLIDIFY"]},
                        "modifier_name": {"type": "string"}
                    },
                    "required": ["object_name", "modifier_type"]
                }
            },
            {
                "name": "create_material",
                "description": "Create a new material and optionally assign it to an object",
                "inputSchema": {
                    "type": "object",
                    "properties": {
                        "material_name": {"type": "string"},
                        "object_name": {"type": "string"},
                        "color": {"type": "array", "items": {"type": "number"}}
                    },
                    "required": ["material_name"]
                }
            },
            {
                "name": "import_model",
                "description": "Import a 3D model (FBX, OBJ, GLTF, etc.)",
                "inputSchema": {
                    "type": "object",
                    "properties": {
                        "filepath": {"type": "string"},
                        "file_type": {"type": "string", "enum": ["FBX", "OBJ", "GLTF", "USD", "ABC"]}
                    },
                    "required": ["filepath"]
                }
            },
            {
                "name": "export_model",
                "description": "Export the scene or selected objects",
                "inputSchema": {
                    "type": "object",
                    "properties": {
                        "filepath": {"type": "string"},
                        "file_type": {"type": "string", "enum": ["FBX", "OBJ", "GLTF", "USD", "ABC"]},
                        "use_selection": {"type": "boolean"}
                    },
                    "required": ["filepath", "file_type"]
                }
            },
            {
                "name": "render_scene",
                "description": "Render the current scene to an image",
                "inputSchema": {
                    "type": "object",
                    "properties": {
                        "filepath": {"type": "string"},
                        "resolution_x": {"type": "integer"},
                        "resolution_y": {"type": "integer"},
                        "engine": {"type": "string", "enum": ["CYCLES", "EEVEE", "WORKBENCH"]}
                    },
                    "required": ["filepath"]
                }
            },
            {
                "name": "execute_python",
                "description": "Execute arbitrary Python code in Blender context",
                "inputSchema": {
                    "type": "object",
                    "properties": {
                        "code": {"type": "string"}
                    },
                    "required": ["code"]
                }
            },
            {
                "name": "get_scene_info",
                "description": "Get information about the current scene",
                "inputSchema": {
                    "type": "object",
                    "properties": {}
                }
            },
            {
                "name": "optimize_scene",
                "description": "Optimize scene for performance",
                "inputSchema": {
                    "type": "object",
                    "properties": {
                        "remove_unused": {"type": "boolean"},
                        "merge_objects": {"type": "boolean"}
                    }
                }
            },
            {
                "name": "create_animation",
                "description": "Create keyframe animation",
                "inputSchema": {
                    "type": "object",
                    "properties": {
                        "object_name": {"type": "string"},
                        "property": {"type": "string"},
                        "frames": {"type": "array", "items": {"type": "object"}},
                        "interpolation": {"type": "string", "enum": ["LINEAR", "BEZIER", "CONSTANT"]}
                    },
                    "required": ["object_name", "property"]
                }
            },
            {
                "name": "manage_asset",
                "description": "Mark or unmark data as asset, generate previews",
                "inputSchema": {
                    "type": "object",
                    "properties": {
                        "data_type": {"type": "string", "enum": ["OBJECT", "MATERIAL", "MESH", "TEXTURE"]},
                        "data_name": {"type": "string"},
                        "action": {"type": "string", "enum": ["MARK_ASSET", "CLEAR_ASSET", "GENERATE_PREVIEW"]},
                        "catalog_path": {"type": "string"}
                    },
                    "required": ["data_type", "data_name", "action"]
                }
            },
            {
                "name": "copy_data",
                "description": "Duplicate any Blender datablock (object, material, mesh, etc.)",
                "inputSchema": {
                    "type": "object",
                    "properties": {
                        "data_type": {"type": "string", "enum": ["OBJECT", "MATERIAL", "MESH", "TEXTURE", "COLLECTION"]},
                        "source_name": {"type": "string"},
                        "target_name": {"type": "string"}
                    },
                    "required": ["data_type", "source_name", "target_name"]
                }
            },
            {
                "name": "manage_library",
                "description": "Make data local or get library information",
                "inputSchema": {
                    "type": "object",
                    "properties": {
                        "data_type": {"type": "string", "enum": ["OBJECT", "MATERIAL", "MESH", "TEXTURE"]},
                        "data_name": {"type": "string"},
                        "action": {"type": "string", "enum": ["MAKE_LOCAL", "GET_INFO"]}
                    },
                    "required": ["data_type", "data_name", "action"]
                }
            },
            {
                "name": "get_data_info",
                "description": "Get detailed information about any Blender datablock",
                "inputSchema": {
                    "type": "object",
                    "properties": {
                        "data_type": {"type": "string", "enum": ["OBJECT", "MATERIAL", "MESH", "TEXTURE", "COLLECTION"]},
                        "data_name": {"type": "string"}
                    },
                    "required": ["data_type", "data_name"]
                }
            }
        ]
        
        return {
            "jsonrpc": "2.0",
            "id": self.request_id,
            "result": {
                "tools": tools
            }
        }
    
    def call_tool(self, params: Dict[str, Any]) -> Dict[str, Any]:
        """Call a specific Blender tool"""
        tool_name = params.get("name", "")
        arguments = params.get("arguments", {})
        
        try:
            if tool_name == "create_object":
                result = self.create_object(arguments)
            elif tool_name == "delete_object":
                result = self.delete_object(arguments)
            elif tool_name == "set_object_transform":
                result = self.set_object_transform(arguments)
            elif tool_name == "add_modifier":
                result = self.add_modifier(arguments)
            elif tool_name == "create_material":
                result = self.create_material(arguments)
            elif tool_name == "import_model":
                result = self.import_model(arguments)
            elif tool_name == "export_model":
                result = self.export_model(arguments)
            elif tool_name == "render_scene":
                result = self.render_scene(arguments)
            elif tool_name == "execute_python":
                result = self.execute_python(arguments)
            elif tool_name == "get_scene_info":
                result = self.get_scene_info(arguments)
            elif tool_name == "optimize_scene":
                result = self.optimize_scene(arguments)
            elif tool_name == "create_animation":
                result = self.create_animation(arguments)
            elif tool_name == "manage_asset":
                result = self.manage_asset(arguments)
            elif tool_name == "copy_data":
                result = self.copy_data(arguments)
            elif tool_name == "manage_library":
                result = self.manage_library(arguments)
            elif tool_name == "get_data_info":
                result = self.get_data_info(arguments)
            else:
                return self.error_response(-32601, f"Unknown tool: {tool_name}")
            
            return {
                "jsonrpc": "2.0",
                "id": self.request_id,
                "result": {
                    "content": [
                        {
                            "type": "text",
                            "text": json.dumps(result, indent=2)
                        }
                    ]
                }
            }
        
        except Exception as e:
            return self.error_response(-32603, f"Tool execution error: {str(e)}", traceback.format_exc())
    
    def create_object(self, args: Dict[str, Any]) -> Dict[str, Any]:
        """Create a primitive object"""
        obj_type = args.get("type", "CUBE").upper()
        name = args.get("name", f"{obj_type.lower()}")
        location = args.get("location", [0, 0, 0])
        
        primitive_map = {
            "CUBE": lambda: bpy.ops.mesh.primitive_cube_add(),
            "SPHERE": lambda: bpy.ops.mesh.primitive_uv_sphere_add(),
            "PLANE": lambda: bpy.ops.mesh.primitive_plane_add(),
            "CYLINDER": lambda: bpy.ops.mesh.primitive_cylinder_add(),
            "CONE": lambda: bpy.ops.mesh.primitive_cone_add(),
            "TORUS": lambda: bpy.ops.mesh.primitive_torus_add()
        }
        
        if obj_type not in primitive_map:
            raise ValueError(f"Unknown object type: {obj_type}")
        
        primitive_map[obj_type]()
        obj = bpy.context.active_object
        obj.name = name
        obj.location = location
        
        return {"success": True, "object_name": name, "type": obj_type}
    
    def delete_object(self, args: Dict[str, Any]) -> Dict[str, Any]:
        """Delete an object"""
        name = args.get("name")
        if name not in bpy.data.objects:
            raise ValueError(f"Object '{name}' not found")
        
        bpy.data.objects.remove(bpy.data.objects[name])
        return {"success": True, "deleted": name}
    
    def set_object_transform(self, args: Dict[str, Any]) -> Dict[str, Any]:
        """Set object transform"""
        name = args.get("name")
        if name not in bpy.data.objects:
            raise ValueError(f"Object '{name}' not found")
        
        obj = bpy.data.objects[name]
        
        if "location" in args:
            obj.location = args["location"]
        if "rotation" in args:
            obj.rotation_euler = args["rotation"]
        if "scale" in args:
            obj.scale = args["scale"]
        
        return {"success": True, "object": name}
    
    def add_modifier(self, args: Dict[str, Any]) -> Dict[str, Any]:
        """Add modifier to object"""
        obj_name = args.get("object_name")
        modifier_type = args.get("modifier_type")
        modifier_name = args.get("modifier_name", modifier_type.lower())
        
        if obj_name not in bpy.data.objects:
            raise ValueError(f"Object '{obj_name}' not found")
        
        obj = bpy.data.objects[obj_name]
        modifier = obj.modifiers.new(name=modifier_name, type=modifier_type)
        
        return {"success": True, "object": obj_name, "modifier": modifier_name}
    
    def create_material(self, args: Dict[str, Any]) -> Dict[str, Any]:
        """Create material"""
        mat_name = args.get("material_name")
        obj_name = args.get("object_name")
        color = args.get("color", [0.8, 0.8, 0.8, 1.0])
        
        mat = bpy.data.materials.new(name=mat_name)
        mat.use_nodes = True
        if mat.node_tree:
            bsdf = mat.node_tree.nodes.get("Principled BSDF")
            if bsdf:
                bsdf.inputs["Base Color"].default_value = color
        
        if obj_name and obj_name in bpy.data.objects:
            obj = bpy.data.objects[obj_name]
            obj.data.materials.append(mat)
        
        return {"success": True, "material": mat_name}
    
    def import_model(self, args: Dict[str, Any]) -> Dict[str, Any]:
        """Import 3D model"""
        filepath = args.get("filepath")
        file_type = args.get("file_type", "FBX").upper()
        
        import_map = {
            "FBX": lambda: bpy.ops.import_scene.fbx(filepath=filepath),
            "OBJ": lambda: bpy.ops.wm.obj_import(filepath=filepath),
            "GLTF": lambda: bpy.ops.import_scene.gltf(filepath=filepath),
            "USD": lambda: bpy.ops.wm.usd_import(filepath=filepath),
            "ABC": lambda: bpy.ops.wm.alembic_import(filepath=filepath)
        }
        
        if file_type not in import_map:
            raise ValueError(f"Unsupported file type: {file_type}")
        
        import_map[file_type]()
        
        return {"success": True, "filepath": filepath, "type": file_type}
    
    def export_model(self, args: Dict[str, Any]) -> Dict[str, Any]:
        """Export model"""
        filepath = args.get("filepath")
        file_type = args.get("file_type").upper()
        use_selection = args.get("use_selection", False)
        
        export_map = {
            "FBX": lambda: bpy.ops.export_scene.fbx(filepath=filepath, use_selection=use_selection),
            "OBJ": lambda: bpy.ops.wm.obj_export(filepath=filepath, export_selected_objects_only=use_selection),
            "GLTF": lambda: bpy.ops.export_scene.gltf(filepath=filepath, use_selection=use_selection),
            "USD": lambda: bpy.ops.wm.usd_export(filepath=filepath, selected_objects_only=use_selection),
            "ABC": lambda: bpy.ops.wm.alembic_export(filepath=filepath, selected=True if use_selection else False)
        }
        
        if file_type not in export_map:
            raise ValueError(f"Unsupported file type: {file_type}")
        
        export_map[file_type]()
        
        return {"success": True, "filepath": filepath}
    
    def render_scene(self, args: Dict[str, Any]) -> Dict[str, Any]:
        """Render scene"""
        filepath = args.get("filepath")
        res_x = args.get("resolution_x", 1920)
        res_y = args.get("resolution_y", 1080)
        engine = args.get("engine", "CYCLES")
        
        scene = bpy.context.scene
        scene.render.resolution_x = res_x
        scene.render.resolution_y = res_y
        scene.render.engine = engine
        
        scene.render.filepath = filepath
        bpy.ops.render.render(write_still=True)
        
        return {"success": True, "filepath": filepath, "resolution": [res_x, res_y]}
    
    def execute_python(self, args: Dict[str, Any]) -> Dict[str, Any]:
        """Execute Python code"""
        if self.safe_mode:
            return {"success": False, "error": "Python execution disabled in safe mode. Use only trusted scripts."}

        code = args.get("code", "")

        # Extra beveiliging tegen gevaarlijke operaties
        dangerous_patterns = [
            "import os", "import sys", "__import__", "eval(", "exec(",
            "subprocess", "shutil.rmtree", "open("
        ]

        if any(pattern in code for pattern in dangerous_patterns):
            return {"success": False, "error": "Dangerous Python operation detected"}

        try:
            exec(code, {"__builtins__": __builtins__, "bpy": bpy, "bmesh": bmesh})
            return {"success": True, "executed": True}
        except Exception as e:
            return {"success": False, "error": str(e)}
    
    def get_scene_info(self, args: Dict[str, Any]) -> Dict[str, Any]:
        """Get scene information"""
        scene = bpy.context.scene
        return {
            "scene_name": scene.name,
            "object_count": len(scene.objects),
            "objects": [obj.name for obj in scene.objects],
            "material_count": len(bpy.data.materials),
            "frame_start": scene.frame_start,
            "frame_end": scene.frame_end,
            "render_engine": scene.render.engine
        }
    
    def optimize_scene(self, args: Dict[str, Any]) -> Dict[str, Any]:
        """Optimize scene"""
        remove_unused = args.get("remove_unused", True)
        merge_objects = args.get("merge_objects", False)
        
        removed = 0
        if remove_unused:
            # Remove unused materials
            for material in list(bpy.data.materials):
                if not material.users:
                    bpy.data.materials.remove(material)
                    removed += 1
        
        return {"success": True, "removed_unused": removed}
    
    def create_animation(self, args: Dict[str, Any]) -> Dict[str, Any]:
        """Create keyframe animation"""
        obj_name = args.get("object_name")
        property_name = args.get("property", "location")
        frames = args.get("frames", [])
        interpolation = args.get("interpolation", "BEZIER")
        
        if obj_name not in bpy.data.objects:
            raise ValueError(f"Object '{obj_name}' not found")
        
        obj = bpy.data.objects[obj_name]
        scene = bpy.context.scene
        
        for frame_data in frames:
            frame_num = frame_data.get("frame", 0)
            value = frame_data.get("value")
            
            scene.frame_set(frame_num)
            
            if property_name == "location":
                obj.location = value
            elif property_name == "rotation":
                obj.rotation_euler = value
            elif property_name == "scale":
                obj.scale = value
            
            obj.keyframe_insert(data_path=property_name, frame=frame_num)
        
        return {"success": True, "keyframes": len(frames)}

    def manage_asset(self, args: Dict[str, Any]) -> Dict[str, Any]:
        """Manage asset marking and previews using ID methods"""
        data_type = args.get("data_type").upper()
        data_name = args.get("data_name")
        action = args.get("action").upper()
        catalog_path = args.get("catalog_path", "")

        # Get the data block
        data_collection = self._get_data_collection(data_type)
        if data_name not in data_collection:
            raise ValueError(f"{data_type} '{data_name}' not found")

        data_block = data_collection[data_name]

        if action == "MARK_ASSET":
            data_block.asset_mark()
            if catalog_path:
                # Set asset catalog if provided
                data_block.asset_data.catalog_id = catalog_path
            return {"success": True, "action": "marked_as_asset", "name": data_name}

        elif action == "CLEAR_ASSET":
            data_block.asset_clear()
            return {"success": True, "action": "cleared_asset", "name": data_name}

        elif action == "GENERATE_PREVIEW":
            data_block.asset_generate_preview()
            return {"success": True, "action": "generated_preview", "name": data_name}

        else:
            raise ValueError(f"Unknown action: {action}")

    def copy_data(self, args: Dict[str, Any]) -> Dict[str, Any]:
        """Duplicate any Blender datablock using ID.copy()"""
        data_type = args.get("data_type").upper()
        source_name = args.get("source_name")
        target_name = args.get("target_name")

        # Get the data block
        data_collection = self._get_data_collection(data_type)
        if source_name not in data_collection:
            raise ValueError(f"{data_type} '{source_name}' not found")

        source_data = data_collection[source_name]

        # Create copy
        copied_data = source_data.copy()
        copied_data.name = target_name

        return {
            "success": True,
            "action": "copied",
            "source": source_name,
            "target": target_name,
            "type": data_type
        }

    def manage_library(self, args: Dict[str, Any]) -> Dict[str, Any]:
        """Make data local or get library information"""
        data_type = args.get("data_type").upper()
        data_name = args.get("data_name")
        action = args.get("action").upper()

        # Get the data block
        data_collection = self._get_data_collection(data_type)
        if data_name not in data_collection:
            raise ValueError(f"{data_type} '{data_name}' not found")

        data_block = data_collection[data_name]

        if action == "MAKE_LOCAL":
            data_block.make_local()
            return {"success": True, "action": "made_local", "name": data_name}

        elif action == "GET_INFO":
            library_info = {
                "name": data_block.name,
                "library": data_block.library.filepath if data_block.library else None,
                "users": data_block.users,
                "use_fake_user": data_block.use_fake_user,
                "is_library_indirect": data_block.is_library_indirect,
                "is_missing": data_block.is_missing,
                "is_embedded_data": data_block.is_embedded_data
            }
            return {"success": True, "info": library_info}

        else:
            raise ValueError(f"Unknown action: {action}")

    def get_data_info(self, args: Dict[str, Any]) -> Dict[str, Any]:
        """Get detailed information about any Blender datablock using ID properties"""
        data_type = args.get("data_type").upper()
        data_name = args.get("data_name")

        # Get the data block
        data_collection = self._get_data_collection(data_type)
        if data_name not in data_collection:
            raise ValueError(f"{data_type} '{data_name}' not found")

        data_block = data_collection[data_name]

        # Get comprehensive info using ID properties
        info = {
            "name": data_block.name,
            "name_full": getattr(data_block, 'name_full', data_block.name),
            "users": data_block.users,
            "use_fake_user": data_block.use_fake_user,
            "use_extra_user": getattr(data_block, 'use_extra_user', False),
            "library": data_block.library.filepath if data_block.library else None,
            "is_library_indirect": data_block.is_library_indirect,
            "is_missing": data_block.is_missing,
            "is_embedded_data": data_block.is_embedded_data,
            "is_editable": data_block.is_editable,
            "is_evaluated": getattr(data_block, 'is_evaluated', False),
            "session_uid": data_block.session_uid,
            "tag": getattr(data_block, 'tag', False)
        }

        # Add type-specific information
        if hasattr(data_block, 'asset_data') and data_block.asset_data:
            info["asset_info"] = {
                "is_asset": True,
                "catalog_id": data_block.asset_data.catalog_id,
                "description": data_block.asset_data.description
            }
        else:
            info["asset_info"] = {"is_asset": False}

        return {"success": True, "info": info}

    def _get_data_collection(self, data_type: str):
        """Helper method to get the appropriate data collection"""
        collections = {
            "OBJECT": bpy.data.objects,
            "MATERIAL": bpy.data.materials,
            "MESH": bpy.data.meshes,
            "TEXTURE": bpy.data.textures,
            "COLLECTION": bpy.data.collections,
            "IMAGE": bpy.data.images,
            "LIGHT": bpy.data.lights,
            "CAMERA": bpy.data.cameras
        }

        if data_type not in collections:
            raise ValueError(f"Unsupported data type: {data_type}")

        return collections[data_type]

    def error_response(self, code: int, message: str, data: Optional[str] = None) -> Dict[str, Any]:
        """Create error response"""
        error = {
            "code": code,
            "message": message
        }
        if data:
            error["data"] = data
        
        return {
            "jsonrpc": "2.0",
            "id": self.request_id,
            "error": error
        }


def main():
    """Main stdio server loop"""
    server = BlenderMCPServer()
    
    # Read from stdin line by line (JSON-RPC 2.0 over stdio)
    try:
        while True:
            line = sys.stdin.readline()
            if not line:
                break
            
            line = line.strip()
            if not line:
                continue
            
            try:
                request = json.loads(line)
                response = server.handle_request(request)
                print(json.dumps(response, ensure_ascii=False))
                sys.stdout.flush()
            except json.JSONDecodeError as e:
                error_response = {
                    "jsonrpc": "2.0",
                    "id": None,
                    "error": {
                        "code": -32700,
                        "message": f"Parse error: {str(e)}"
                    }
                }
                print(json.dumps(error_response, ensure_ascii=False))
                sys.stdout.flush()
            except Exception as e:
                error_response = {
                    "jsonrpc": "2.0",
                    "id": None,
                    "error": {
                        "code": -32603,
                        "message": f"Internal error: {str(e)}"
                    }
                }
                print(json.dumps(error_response, ensure_ascii=False))
                sys.stdout.flush()
    
    except KeyboardInterrupt:
        pass
    except Exception as e:
        error_response = {
            "jsonrpc": "2.0",
            "id": None,
            "error": {
                "code": -32603,
                "message": f"Internal error: {str(e)}"
            }
        }
        print(json.dumps(error_response, ensure_ascii=False))
        sys.stdout.flush()


if __name__ == "__main__":
    # This script MUST run inside Blender
    try:
        import bpy
        # Verify we're in Blender
        if not hasattr(bpy, "context"):
            raise ImportError("Blender context not available")
        main()
    except ImportError:
        print("Error: This script must run inside Blender.", file=sys.stderr)
        print("Usage: blender --background --python blender_mcp_server.py", file=sys.stderr)
        sys.exit(1)
    except Exception as e:
        print(f"Error initializing Blender MCP Server: {str(e)}", file=sys.stderr)
        sys.exit(1)

