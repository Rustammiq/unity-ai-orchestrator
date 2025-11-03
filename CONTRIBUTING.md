# Contributing to Unity AI Orchestrator

Thank you for your interest in contributing to Unity AI Orchestrator! This document provides guidelines and information for contributors.

## 📋 Table of Contents
- [Code of Conduct](#code-of-conduct)
- [Getting Started](#getting-started)
- [Development Setup](#development-setup)
- [How to Contribute](#how-to-contribute)
- [Development Guidelines](#development-guidelines)
- [Testing](#testing)
- [Documentation](#documentation)
- [Pull Request Process](#pull-request-process)

## 🤝 Code of Conduct

This project follows a code of conduct to ensure a welcoming environment for all contributors. By participating, you agree to:
- Be respectful and inclusive
- Focus on constructive feedback
- Accept responsibility for mistakes
- Show empathy towards other contributors
- Help create a positive community

## 🚀 Getting Started

### Prerequisites
- **Unity 2020.3 or higher**
- **Git**
- **Basic knowledge of C# and Unity development**
- **For Blender MCP development**: Python 3.x and Blender 4.x

### Quick Setup
1. Fork the repository on GitHub
2. Clone your fork:
   ```bash
   git clone https://github.com/YOUR_USERNAME/unity-ai-orchestrator.git
   cd unity-ai-orchestrator
   ```
3. Open the project in Unity
4. Go to `Window > Unity AI Orchestrator > Settings` to configure API keys

## 🛠️ Development Setup

### For Unity Development
1. Open the project in Unity Editor
2. All C# scripts are located in `Assets/Editor/`
3. The main orchestrator window: `UnityAIOrchestratorWindow.cs`
4. Settings window: `UnityAISettingsWindow.cs`
5. LLM adapters: `ClaudeAdapter.cs`, `OpenAIAdapter.cs`, etc.

### For MCP Development
1. MCP server implementations are in `Assets/MCP/`
2. Blender MCP server: `blender_mcp_server.py`
3. Example MCP server: `github_mcp_example.py`
4. Documentation: `BLENDER_MCP_README.md`

### For Testing
1. Create a new Unity scene for testing
2. Test each orchestrator mode (Solo, Dual, Tri, Quad, Penta)
3. Test MCP connections if available
4. Verify pipeline recording functionality

## 💡 How to Contribute

### Types of Contributions
- 🐛 **Bug fixes** - Fix existing issues
- ✨ **Features** - Add new functionality
- 📚 **Documentation** - Improve documentation
- 🧪 **Tests** - Add or improve tests
- 🎨 **UI/UX** - Improve user interface
- 🔧 **Tools** - Add new MCP tools or adapters

### Finding Issues to Work On
1. Check the [Issues](https://github.com/Rustammiq/unity-ai-orchestrator/issues) page
2. Look for issues labeled `good first issue` or `help wanted`
3. Comment on the issue to indicate you're working on it

### Reporting Bugs
When reporting bugs, please include:
- Unity version and operating system
- Steps to reproduce the issue
- Expected vs actual behavior
- Any error messages or logs
- Screenshots if applicable

## 📝 Development Guidelines

### Code Style
- Follow C# coding conventions
- Use meaningful variable and method names
- Add XML documentation comments for public methods
- Keep methods focused on single responsibilities
- Use async/await for asynchronous operations

### Commit Messages
Use clear, descriptive commit messages:
```
feat: add new MCP tool for mesh operations
fix: resolve API key validation bug
docs: update installation instructions
refactor: simplify orchestrator mode selection
```

### Naming Conventions
- **Classes**: PascalCase (e.g., `UnityAIOrchestratorWindow`)
- **Methods**: PascalCase (e.g., `SendPromptAsync`)
- **Variables**: camelCase (e.g., `apiKey`)
- **Constants**: UPPER_SNAKE_CASE (e.g., `DEFAULT_TIMEOUT`)
- **Files**: PascalCase for C# files, snake_case for Python files

### Error Handling
- Use try-catch blocks for API calls
- Provide meaningful error messages to users
- Log errors appropriately for debugging
- Gracefully handle network timeouts and failures

## 🧪 Testing

### Manual Testing Checklist
- [ ] All orchestrator modes work (Solo, Dual, Tri, Quad, Penta)
- [ ] API key validation works correctly
- [ ] MCP server connections are stable
- [ ] Pipeline recording and replay functions
- [ ] Error handling for invalid inputs
- [ ] UI responsiveness and layout

### Testing MCP Tools
For each new MCP tool, test:
1. **Basic functionality** - Tool executes without errors
2. **Parameter validation** - Invalid inputs are handled gracefully
3. **Error scenarios** - Network failures, invalid data, etc.
4. **Integration** - Works correctly with the orchestrator

### Blender MCP Testing
When testing Blender MCP tools:
1. Start Blender MCP server manually first
2. Test individual tools before integration testing
3. Verify Blender state changes as expected
4. Test with different Blender versions if possible

## 📚 Documentation

### Code Documentation
- Add XML comments to all public methods and classes
- Document method parameters and return values
- Include usage examples where helpful
- Keep comments up-to-date with code changes

### User Documentation
- Update README.md for new features
- Add examples in the appropriate documentation files
- Create tutorials for complex features
- Update troubleshooting guides

### API Documentation
- Document MCP tool schemas clearly
- Provide examples for each tool
- Explain parameter requirements and constraints
- Include error response information

## 🔄 Pull Request Process

### Before Submitting
1. **Test thoroughly** - Ensure your changes work as expected
2. **Update documentation** - Add docs for new features
3. **Check code style** - Follow the established conventions
4. **Run existing tests** - Make sure you didn't break anything

### Creating a Pull Request
1. **Fork the repository** if you haven't already
2. **Create a feature branch** from `main`:
   ```bash
   git checkout -b feature/your-feature-name
   ```
3. **Make your changes** and commit them
4. **Push to your fork**:
   ```bash
   git push origin feature/your-feature-name
   ```
5. **Create a Pull Request** on GitHub

### Pull Request Template
Please fill out the PR template with:
- **Description**: What changes were made and why
- **Type of change**: Bug fix, feature, documentation, etc.
- **Testing**: How the changes were tested
- **Breaking changes**: Any breaking changes
- **Additional notes**: Any other relevant information

### Review Process
1. **Automated checks** will run (if configured)
2. **Maintainer review** will be performed
3. **Feedback** will be provided if changes are needed
4. **Approval** and merge once requirements are met

## 🎯 Areas for Contribution

### High Priority
- [ ] Additional LLM provider support
- [ ] More MCP server examples
- [ ] Improved error handling
- [ ] Performance optimizations

### Medium Priority
- [ ] UI/UX improvements
- [ ] More Blender MCP tools
- [ ] Pipeline sharing features
- [ ] Multi-language support

### Future Ideas
- [ ] Voice input support
- [ ] Plugin marketplace
- [ ] Collaboration features
- [ ] Advanced pipeline editing

## 📞 Getting Help

If you need help:
1. **Check existing documentation** first
2. **Search existing issues** on GitHub
3. **Create a new issue** if needed
4. **Join the discussion** in relevant issues

## 🙏 Recognition

Contributors will be:
- Listed in the contributors file
- Mentioned in release notes
- Recognized for their contributions

Thank you for contributing to Unity AI Orchestrator! 🚀
