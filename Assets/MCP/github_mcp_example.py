#!/usr/bin/env python3
"""
GitHub MCP Server Example
Een MCP server voor GitHub integratie die zou kunnen werken met Coplay Orchestrator.
Dit is een voorbeeld van hoe je MCP zou kunnen gebruiken voor GitHub operaties.
"""

import sys
import json
import os
from typing import Dict, List, Any, Optional
import requests

class GitHubMCPServer:
    """MCP Server voor GitHub operaties"""

    def __init__(self):
        self.request_id = None
        self.version = "1.0.0"
        self.github_token = os.environ.get("GITHUB_TOKEN", "")
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

            if method == "initialize":
                return self.initialize(params)
            elif method == "tools/list":
                return self.list_tools()
            elif method == "tools/call":
                return self.call_tool(params)
            else:
                return self.error_response(-32601, f"Method not found: {method}")
        except Exception as e:
            return self.error_response(-32603, f"Internal error: {str(e)}")

    def initialize(self, params: Dict[str, Any]) -> Dict[str, Any]:
        return {
            "jsonrpc": "2.0",
            "id": self.request_id,
            "result": {
                "protocolVersion": "2024-11-05",
                "capabilities": self.capabilities,
                "serverInfo": {
                    "name": "github-mcp-server",
                    "version": self.version
                }
            }
        }

    def list_tools(self) -> Dict[str, Any]:
        tools = [
            {
                "name": "create_issue",
                "description": "Create a new GitHub issue",
                "inputSchema": {
                    "type": "object",
                    "properties": {
                        "repo": {"type": "string"},
                        "title": {"type": "string"},
                        "body": {"type": "string"}
                    },
                    "required": ["repo", "title"]
                }
            },
            {
                "name": "search_repositories",
                "description": "Search for GitHub repositories",
                "inputSchema": {
                    "type": "object",
                    "properties": {
                        "query": {"type": "string"},
                        "language": {"type": "string"}
                    },
                    "required": ["query"]
                }
            },
            {
                "name": "get_repository_info",
                "description": "Get information about a GitHub repository",
                "inputSchema": {
                    "type": "object",
                    "properties": {
                        "owner": {"type": "string"},
                        "repo": {"type": "string"}
                    },
                    "required": ["owner", "repo"]
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
        tool_name = params.get("name", "")
        arguments = params.get("arguments", {})

        try:
            if tool_name == "create_issue":
                result = self.create_issue(arguments)
            elif tool_name == "search_repositories":
                result = self.search_repositories(arguments)
            elif tool_name == "get_repository_info":
                result = self.get_repository_info(arguments)
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
            return self.error_response(-32603, f"Tool execution error: {str(e)}")

    def create_issue(self, args: Dict[str, Any]) -> Dict[str, Any]:
        """Create a GitHub issue"""
        repo = args.get("repo")
        title = args.get("title")
        body = args.get("body", "")

        if not self.github_token:
            return {"error": "GitHub token not configured"}

        url = f"https://api.github.com/repos/{repo}/issues"
        headers = {
            "Authorization": f"token {self.github_token}",
            "Accept": "application/vnd.github.v3+json"
        }
        data = {"title": title, "body": body}

        response = requests.post(url, json=data, headers=headers)
        return response.json()

    def search_repositories(self, args: Dict[str, Any]) -> Dict[str, Any]:
        """Search GitHub repositories"""
        query = args.get("query")
        language = args.get("language", "")

        url = f"https://api.github.com/search/repositories?q={query}"
        if language:
            url += f"+language:{language}"

        response = requests.get(url)
        return response.json()

    def get_repository_info(self, args: Dict[str, Any]) -> Dict[str, Any]:
        """Get repository information"""
        owner = args.get("owner")
        repo = args.get("repo")

        url = f"https://api.github.com/repos/{owner}/{repo}"
        response = requests.get(url)
        return response.json()

    def error_response(self, code: int, message: str) -> Dict[str, Any]:
        return {
            "jsonrpc": "2.0",
            "id": self.request_id,
            "error": {
                "code": code,
                "message": message
            }
        }


def main():
    server = GitHubMCPServer()

    for line in sys.stdin:
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


if __name__ == "__main__":
    main()
