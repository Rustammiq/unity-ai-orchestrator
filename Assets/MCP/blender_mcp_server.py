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
            },
            {
                "name": "manage_users",
                "description": "Manage datablock users (remap, clear, find usage)",
                "inputSchema": {
                    "type": "object",
                    "properties": {
                        "action": {"type": "string", "enum": ["CLEAR_USERS", "FIND_USERS", "USER_REMAP"]},
                        "data_type": {"type": "string", "enum": ["OBJECT", "MATERIAL", "MESH", "TEXTURE"]},
                        "data_name": {"type": "string"},
                        "remap_old": {"type": "string"},
                        "remap_new": {"type": "string"}
                    },
                    "required": ["action", "data_type", "data_name"]
                }
            },
            {
                "name": "manage_scenes",
                "description": "Create, delete, or switch scenes",
                "inputSchema": {
                    "type": "object",
                    "properties": {
                        "action": {"type": "string", "enum": ["CREATE", "DELETE", "SWITCH"]},
                        "scene_name": {"type": "string"},
                        "template_scene": {"type": "string"}
                    },
                    "required": ["action", "scene_name"]
                }
            },
            {
                "name": "manage_collections",
                "description": "Create/delete collections and manage object membership",
                "inputSchema": {
                    "type": "object",
                    "properties": {
                        "action": {"type": "string", "enum": ["CREATE", "DELETE", "ADD_OBJECT", "REMOVE_OBJECT", "LINK_OBJECT", "UNLINK_OBJECT"]},
                        "collection_name": {"type": "string"},
                        "object_name": {"type": "string"}
                    },
                    "required": ["action", "collection_name"]
                }
            },
            {
                "name": "manage_animation_data",
                "description": "Create or clear animation data for datablocks",
                "inputSchema": {
                    "type": "object",
                    "properties": {
                        "action": {"type": "string", "enum": ["CREATE", "CLEAR"]},
                        "data_type": {"type": "string", "enum": ["OBJECT", "MATERIAL", "MESH", "TEXTURE"]},
                        "data_name": {"type": "string"}
                    },
                    "required": ["action", "data_type", "data_name"]
                }
            },
            {
                "name": "create_library_override",
                "description": "Create library overrides for linked datablocks",
                "inputSchema": {
                    "type": "object",
                    "properties": {
                        "data_type": {"type": "string", "enum": ["OBJECT", "MATERIAL", "MESH", "TEXTURE"]},
                        "data_name": {"type": "string"}
                    },
                    "required": ["data_type", "data_name"]
                }
            },
            {
                "name": "mesh_operations",
                "description": "Perform mesh operations (subdivide, decimate, triangulate, etc.)",
                "inputSchema": {
                    "type": "object",
                    "properties": {
                        "object_name": {"type": "string"},
                        "operation": {"type": "string", "enum": ["SUBDIVIDE", "DECIMATE", "TRIANGULATE", "QUAD_TO_TRI", "TRI_TO_QUAD"]},
                        "iterations": {"type": "integer", "default": 1},
                        "ratio": {"type": "number", "default": 0.5}
                    },
                    "required": ["object_name", "operation"]
                }
            },
            {
                "name": "uv_operations",
                "description": "Perform UV operations (unwrap, pack islands, etc.)",
                "inputSchema": {
                    "type": "object",
                    "properties": {
                        "object_name": {"type": "string"},
                        "operation": {"type": "string", "enum": ["SMART_UV_UNWRAP", "LIGHTMAP_PACK", "AVERAGE_ISLANDS_SCALE"]},
                        "angle_limit": {"type": "number", "default": 66.0},
                        "island_margin": {"type": "number", "default": 0.02},
                        "user_area_weight": {"type": "number", "default": 0.0}
                    },
                    "required": ["object_name", "operation"]
                }
            },
            {
                "name": "bake_textures",
                "description": "Bake textures (normal, diffuse, roughness, etc.)",
                "inputSchema": {
                    "type": "object",
                    "properties": {
                        "object_name": {"type": "string"},
                        "bake_type": {"type": "string", "enum": ["NORMAL", "DIFFUSE", "ROUGHNESS", "METALLIC", "EMISSION", "AO", "COMBINED"]},
                        "image_name": {"type": "string"},
                        "width": {"type": "integer", "default": 1024},
                        "height": {"type": "integer", "default": 1024},
                        "margin": {"type": "integer", "default": 16}
                    },
                    "required": ["object_name", "bake_type", "image_name"]
                }
            },
            {
                "name": "preview_ensure",
                "description": "Ensure preview images exist for datablocks",
                "inputSchema": {
                    "type": "object",
                    "properties": {
                        "data_type": {"type": "string", "enum": ["OBJECT", "MATERIAL", "MESH", "TEXTURE"]},
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
            elif tool_name == "manage_users":
                result = self.manage_users(arguments)
            elif tool_name == "manage_scenes":
                result = self.manage_scenes(arguments)
            elif tool_name == "manage_collections":
                result = self.manage_collections(arguments)
            elif tool_name == "manage_animation_data":
                result = self.manage_animation_data(arguments)
            elif tool_name == "create_library_override":
                result = self.create_library_override(arguments)
            elif tool_name == "mesh_operations":
                result = self.mesh_operations(arguments)
            elif tool_name == "uv_operations":
                result = self.uv_operations(arguments)
            elif tool_name == "bake_textures":
                result = self.bake_textures(arguments)
            elif tool_name == "preview_ensure":
                result = self.preview_ensure(arguments)
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

    def manage_users(self, args: Dict[str, Any]) -> Dict[str, Any]:
        """Manage datablock users using ID.user_* methods"""
        action = args.get("action").upper()
        data_type = args.get("data_type").upper()
        data_name = args.get("data_name")

        data_collection = self._get_data_collection(data_type)
        if data_name not in data_collection:
            raise ValueError(f"{data_type} '{data_name}' not found")

        data_block = data_collection[data_name]

        if action == "CLEAR_USERS":
            data_block.user_clear()
            return {"success": True, "action": "cleared_users", "name": data_name}

        elif action == "FIND_USERS":
            users = data_block.user_of_id()
            user_objects = []
            for obj in users:
                user_objects.append({
                    "name": obj.name,
                    "type": obj.type if hasattr(obj, 'type') else 'UNKNOWN'
                })
            return {"success": True, "users": user_objects, "count": len(user_objects)}

        elif action == "USER_REMAP":
            remap_old = args.get("remap_old")
            remap_new = args.get("remap_new")

            if not remap_old or not remap_new:
                raise ValueError("remap_old and remap_new are required for USER_REMAP")

            old_collection = self._get_data_collection(data_type)
            new_collection = self._get_data_collection(data_type)

            if remap_old not in old_collection or remap_new not in new_collection:
                raise ValueError(f"Source '{remap_old}' or target '{remap_new}' not found")

            data_block.user_remap(old_collection[remap_old], new_collection[remap_new])
            return {"success": True, "action": "remapped_users", "from": remap_old, "to": remap_new}

        else:
            raise ValueError(f"Unknown action: {action}")

    def manage_scenes(self, args: Dict[str, Any]) -> Dict[str, Any]:
        """Create, delete, or switch scenes"""
        action = args.get("action").upper()
        scene_name = args.get("scene_name")

        if action == "CREATE":
            template_scene = args.get("template_scene", bpy.context.scene.name)
            if template_scene not in bpy.data.scenes:
                raise ValueError(f"Template scene '{template_scene}' not found")

            template = bpy.data.scenes[template_scene]
            new_scene = bpy.data.scenes.new(scene_name)

            # Copy basic settings from template
            new_scene.world = template.world
            new_scene.use_gravity = template.use_gravity
            new_scene.gravity = template.gravity

            return {"success": True, "action": "created_scene", "name": scene_name}

        elif action == "DELETE":
            if scene_name not in bpy.data.scenes:
                raise ValueError(f"Scene '{scene_name}' not found")

            if len(bpy.data.scenes) <= 1:
                raise ValueError("Cannot delete the last scene")

            scene = bpy.data.scenes[scene_name]
            bpy.data.scenes.remove(scene)
            return {"success": True, "action": "deleted_scene", "name": scene_name}

        elif action == "SWITCH":
            if scene_name not in bpy.data.scenes:
                raise ValueError(f"Scene '{scene_name}' not found")

            bpy.context.window.scene = bpy.data.scenes[scene_name]
            return {"success": True, "action": "switched_scene", "name": scene_name}

        else:
            raise ValueError(f"Unknown action: {action}")

    def manage_collections(self, args: Dict[str, Any]) -> Dict[str, Any]:
        """Create/delete collections and manage object membership"""
        action = args.get("action").upper()
        collection_name = args.get("collection_name")
        object_name = args.get("object_name")

        if action == "CREATE":
            if collection_name in bpy.data.collections:
                raise ValueError(f"Collection '{collection_name}' already exists")

            collection = bpy.data.collections.new(collection_name)
            bpy.context.scene.collection.children.link(collection)
            return {"success": True, "action": "created_collection", "name": collection_name}

        elif action == "DELETE":
            if collection_name not in bpy.data.collections:
                raise ValueError(f"Collection '{collection_name}' not found")

            collection = bpy.data.collections[collection_name]
            bpy.data.collections.remove(collection)
            return {"success": True, "action": "deleted_collection", "name": collection_name}

        else:
            # Actions that require both collection and object
            if not object_name:
                raise ValueError(f"object_name required for action {action}")

            if collection_name not in bpy.data.collections:
                raise ValueError(f"Collection '{collection_name}' not found")

            if object_name not in bpy.data.objects:
                raise ValueError(f"Object '{object_name}' not found")

            collection = bpy.data.collections[collection_name]
            obj = bpy.data.objects[object_name]

            if action == "ADD_OBJECT":
                if obj.name not in collection.objects:
                    collection.objects.link(obj)
                return {"success": True, "action": "added_object", "collection": collection_name, "object": object_name}

            elif action == "REMOVE_OBJECT":
                if obj.name in collection.objects:
                    collection.objects.unlink(obj)
                return {"success": True, "action": "removed_object", "collection": collection_name, "object": object_name}

            elif action == "LINK_OBJECT":
                if obj.name not in collection.objects:
                    collection.objects.link(obj)
                return {"success": True, "action": "linked_object", "collection": collection_name, "object": object_name}

            elif action == "UNLINK_OBJECT":
                if obj.name in collection.objects:
                    collection.objects.unlink(obj)
                return {"success": True, "action": "unlinked_object", "collection": collection_name, "object": object_name}

            else:
                raise ValueError(f"Unknown action: {action}")

    def manage_animation_data(self, args: Dict[str, Any]) -> Dict[str, Any]:
        """Create or clear animation data for datablocks"""
        action = args.get("action").upper()
        data_type = args.get("data_type").upper()
        data_name = args.get("data_name")

        data_collection = self._get_data_collection(data_type)
        if data_name not in data_collection:
            raise ValueError(f"{data_type} '{data_name}' not found")

        data_block = data_collection[data_name]

        if action == "CREATE":
            if not data_block.animation_data:
                data_block.animation_data_create()
            return {"success": True, "action": "created_animation_data", "name": data_name}

        elif action == "CLEAR":
            if data_block.animation_data:
                data_block.animation_data_clear()
            return {"success": True, "action": "cleared_animation_data", "name": data_name}

        else:
            raise ValueError(f"Unknown action: {action}")

    def create_library_override(self, args: Dict[str, Any]) -> Dict[str, Any]:
        """Create library overrides for linked datablocks"""
        data_type = args.get("data_type").upper()
        data_name = args.get("data_name")

        data_collection = self._get_data_collection(data_type)
        if data_name not in data_collection:
            raise ValueError(f"{data_type} '{data_name}' not found")

        data_block = data_collection[data_name]

        # Check if it's a linked datablock
        if not data_block.library:
            raise ValueError(f"{data_type} '{data_name}' is not a linked datablock")

        # Create library override
        override = data_block.override_create(remap_local_usages=True)
        return {
            "success": True,
            "action": "created_library_override",
            "name": data_name,
            "override_name": override.name if hasattr(override, 'name') else data_name
        }

    def mesh_operations(self, args: Dict[str, Any]) -> Dict[str, Any]:
        """Perform mesh operations like subdivide, decimate, etc."""
        object_name = args.get("object_name")
        operation = args.get("operation").upper()
        iterations = args.get("iterations", 1)
        ratio = args.get("ratio", 0.5)

        if object_name not in bpy.data.objects:
            raise ValueError(f"Object '{object_name}' not found")

        obj = bpy.data.objects[object_name]
        if obj.type != 'MESH':
            raise ValueError(f"Object '{object_name}' is not a mesh")

        # Ensure we're in object mode and select the object
        bpy.ops.object.select_all(action='DESELECT')
        obj.select_set(True)
        bpy.context.view_layer.objects.active = obj

        # Switch to edit mode for mesh operations
        bpy.ops.object.mode_set(mode='EDIT')

        if operation == "SUBDIVIDE":
            bpy.ops.mesh.subdivide(number_cuts=iterations)
            bpy.ops.object.mode_set(mode='OBJECT')
            return {"success": True, "operation": "subdivided", "iterations": iterations}

        elif operation == "DECIMATE":
            bpy.ops.mesh.decimate(ratio=ratio)
            bpy.ops.object.mode_set(mode='OBJECT')
            return {"success": True, "operation": "decimated", "ratio": ratio}

        elif operation == "TRIANGULATE":
            bpy.ops.mesh.quads_convert_to_tris()
            bpy.ops.object.mode_set(mode='OBJECT')
            return {"success": True, "operation": "triangulated"}

        elif operation == "QUAD_TO_TRI":
            bpy.ops.mesh.quads_convert_to_tris()
            bpy.ops.object.mode_set(mode='OBJECT')
            return {"success": True, "operation": "quad_to_tri"}

        elif operation == "TRI_TO_QUAD":
            bpy.ops.mesh.tris_convert_to_quads()
            bpy.ops.object.mode_set(mode='OBJECT')
            return {"success": True, "operation": "tri_to_quad"}

        else:
            bpy.ops.object.mode_set(mode='OBJECT')
            raise ValueError(f"Unknown mesh operation: {operation}")

    def uv_operations(self, args: Dict[str, Any]) -> Dict[str, Any]:
        """Perform UV operations like unwrap, pack islands"""
        object_name = args.get("object_name")
        operation = args.get("operation").upper()
        angle_limit = args.get("angle_limit", 66.0)
        island_margin = args.get("island_margin", 0.02)
        user_area_weight = args.get("user_area_weight", 0.0)

        if object_name not in bpy.data.objects:
            raise ValueError(f"Object '{object_name}' not found")

        obj = bpy.data.objects[object_name]
        if obj.type != 'MESH':
            raise ValueError(f"Object '{object_name}' is not a mesh")

        # Ensure we're in edit mode with UV editor
        bpy.ops.object.select_all(action='DESELECT')
        obj.select_set(True)
        bpy.context.view_layer.objects.active = obj

        # Switch to edit mode
        bpy.ops.object.mode_set(mode='EDIT')

        # Select all faces
        bpy.ops.mesh.select_all(action='SELECT')

        if operation == "SMART_UV_UNWRAP":
            bpy.ops.uv.smart_project(angle_limit=angle_limit, island_margin=island_margin, user_area_weight=user_area_weight)
            bpy.ops.object.mode_set(mode='OBJECT')
            return {"success": True, "operation": "smart_uv_unwrap", "angle_limit": angle_limit}

        elif operation == "LIGHTMAP_PACK":
            bpy.ops.uv.lightmap_pack()
            bpy.ops.object.mode_set(mode='OBJECT')
            return {"success": True, "operation": "lightmap_pack"}

        elif operation == "AVERAGE_ISLANDS_SCALE":
            bpy.ops.uv.average_islands_scale()
            bpy.ops.object.mode_set(mode='OBJECT')
            return {"success": True, "operation": "average_islands_scale"}

        else:
            bpy.ops.object.mode_set(mode='OBJECT')
            raise ValueError(f"Unknown UV operation: {operation}")

    def bake_textures(self, args: Dict[str, Any]) -> Dict[str, Any]:
        """Bake textures using Cycles bake system"""
        object_name = args.get("object_name")
        bake_type = args.get("bake_type").upper()
        image_name = args.get("image_name")
        width = args.get("width", 1024)
        height = args.get("height", 1024)
        margin = args.get("margin", 16)

        if object_name not in bpy.data.objects:
            raise ValueError(f"Object '{object_name}' not found")

        obj = bpy.data.objects[object_name]
        if obj.type != 'MESH':
            raise ValueError(f"Object '{object_name}' is not a mesh")

        # Create or get image
        if image_name in bpy.data.images:
            image = bpy.data.images[image_name]
        else:
            image = bpy.data.images.new(image_name, width=width, height=height)

        # Ensure Cycles is active
        bpy.context.scene.render.engine = 'CYCLES'

        # Setup bake settings
        bpy.context.scene.cycles.bake_type = bake_type.lower()

        # Select object and set active
        bpy.ops.object.select_all(action='DESELECT')
        obj.select_set(True)
        bpy.context.view_layer.objects.active = obj

        # Create UV map if it doesn't exist
        if not obj.data.uv_layers:
            bpy.ops.object.mode_set(mode='EDIT')
            bpy.ops.mesh.select_all(action='SELECT')
            bpy.ops.uv.smart_project()
            bpy.ops.object.mode_set(mode='OBJECT')

        # Set the image in the active UV layer
        uv_layer = obj.data.uv_layers.active
        if uv_layer:
            for face in obj.data.polygons:
                for loop_index in face.loop_indices:
                    uv_layer.data[loop_index].image = image

        # Bake
        bpy.ops.object.bake(type=bake_type, margin=margin)

        # Save the image
        image.filepath_raw = f"//{image_name}.png"
        image.save()

        return {
            "success": True,
            "operation": "baked_texture",
            "bake_type": bake_type,
            "image_name": image_name,
            "resolution": f"{width}x{height}"
        }

    def preview_ensure(self, args: Dict[str, Any]) -> Dict[str, Any]:
        """Ensure preview images exist for datablocks using ID.preview_ensure()"""
        data_type = args.get("data_type").upper()
        data_name = args.get("data_name")

        data_collection = self._get_data_collection(data_type)
        if data_name not in data_collection:
            raise ValueError(f"{data_type} '{data_name}' not found")

        data_block = data_collection[data_name]

        # Ensure preview exists
        data_block.preview_ensure()

        return {
            "success": True,
            "action": "preview_ensured",
            "name": data_name,
            "has_preview": data_block.preview is not None
        }

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

