# TestGen VS Code Extension

The TestGen VS Code Extension brings powerful AI-driven test case generation directly into your IDE. Generate comprehensive test suites for your C#/.NET projects with intelligent analysis and real-time progress tracking.

## Features

🚀 **One-Click Test Generation** - Generate tests for any C# class or method with CodeLens actions
🧠 **AI-Powered Analysis** - Smart test suggestions based on your code structure and patterns  
📊 **Real-Time Progress** - Visual progress tracking with detailed phase information
🔍 **Test Explorer Integration** - Navigate and manage generated tests in the built-in Test Explorer
👀 **Live Preview** - Preview generated tests in an interactive webview before saving
⚙️ **Configurable Settings** - Customize output paths, test frameworks, and generation options

## Quick Start

1. Install the extension from the VS Code marketplace
2. Open a C#/.NET project in VS Code
3. Click the **"Generate Tests"** CodeLens above any class or method
4. Watch the progress and preview your generated tests
5. Tests are automatically saved to your specified test directory

## Commands

- `TestGen: Generate Tests for Active File` - Generate tests for the currently open file
- `TestGen: Generate Tests for Workspace` - Generate tests for the entire workspace
- `TestGen: Open Test Preview` - Open the test preview panel
- `TestGen: Configure Settings` - Open extension settings

## Configuration

Configure the extension through VS Code settings:

```json
{
  "testgen.outputPath": "./Tests",
  "testgen.testFramework": "xUnit",
  "testgen.verboseOutput": false,
  "testgen.autoSave": true
}
```

## Requirements

- VS Code 1.85.0 or higher
- .NET SDK 6.0 or higher
- TestGen CLI tool installed and accessible in PATH

## Installation

The TestGen CLI tool will be automatically detected if installed globally. To install:

```bash
dotnet tool install -g TestGen.CLI
```

## Support

For issues and feature requests, visit our [GitHub repository](https://github.com/your-org/testgen).

---

**Enjoy generating comprehensive test cases with TestGen! 🎯**
- **Smart test framework detection**: xUnit, NUnit, MSTest, Jest, pytest

### 🎯 **Seamless Integration**
- **Code Lens**: Generate tests directly from your code
- **Context menus**: Right-click to generate tests
- **Command palette**: Quick access to all TestGen commands
- **Test Explorer**: Browse and manage generated tests

### ⚡ **Real-time Features**
- **Live preview** of generated tests
- **Progress indicators** with detailed status
- **Hover suggestions** for test improvement ideas
- **Auto-generation** on file save (optional)

## Quick Start

1. **Install the extension** from VS Code Marketplace
2. **Install TestGen CLI**: Ensure the TestGen command-line tool is available
3. **Open a project** with C#, TypeScript, or Python files
4. **Right-click on a file** and select "Generate Tests"

## Commands

| Command | Description | Shortcut |
|---------|-------------|----------|
| `TestGen: Generate Tests` | Generate tests for selected file/folder | - |
| `TestGen: Generate Tests for Current File` | Generate tests for active editor | `Ctrl+Shift+T` |
| `TestGen: Generate Tests for Project` | Generate tests for entire project | - |
| `TestGen: Open Settings` | Open TestGen settings | - |
| `TestGen: Preview Tests` | Show test preview panel | - |

## Configuration

Configure TestGen in VS Code settings (`Ctrl+,` → search "testgen"):

```json
{
  "testgen.cliPath": "testgen",
  "testgen.outputDirectory": "tests",
  "testgen.testFramework": "xunit",
  "testgen.autoGenerate": false,
  "testgen.showProgress": true,
  "testgen.enableAI": true,
  "testgen.maxTestsPerMethod": 5
}
```

### Settings Reference

- **`testgen.cliPath`**: Path to TestGen CLI executable
- **`testgen.outputDirectory`**: Default output directory for tests
- **`testgen.testFramework`**: Default test framework (xunit, nunit, mstest, jest, pytest)
- **`testgen.autoGenerate`**: Auto-generate tests when saving files
- **`testgen.showProgress`**: Show progress notifications
- **`testgen.enableAI`**: Enable AI-powered test suggestions
- **`testgen.maxTestsPerMethod`**: Maximum test cases per method

## Usage Examples

### Generate Tests for a C# Class

1. Open a C# file with a class you want to test
2. Right-click in the editor → "Generate Tests for Current File"
3. Tests will be generated in the configured output directory

### Preview Tests Before Generation

1. Open a file you want to test
2. Use Command Palette: `TestGen: Preview Tests`
3. Review the generated test structure
4. Click "Generate Tests" to create the files

### Bulk Test Generation

1. Right-click on a project folder in Explorer
2. Select "Generate Tests"
3. TestGen will analyze all supported files and generate comprehensive tests

## Language Support

### C# (.cs)
- **Frameworks**: xUnit, NUnit, MSTest
- **Features**: Controller tests, service tests, model tests
- **Mocking**: Moq, NSubstitute integration
- **Async/await** support

### TypeScript (.ts, .tsx)
- **Frameworks**: Jest, Mocha, Jasmine
- **Features**: Component tests, service tests, function tests
- **React/Angular** component testing
- **Mock generation** for dependencies

### Python (.py)
- **Frameworks**: pytest, unittest
- **Features**: Class tests, function tests, module tests
- **Mock support** with unittest.mock
- **Fixture generation**

## Test Explorer

The TestGen Test Explorer provides:

- **Project overview** of all test files
- **Test method listing** with quick navigation
- **Test file status** and statistics
- **Quick actions** for test generation and management

## Code Lens Integration

TestGen adds helpful Code Lens actions above your methods and classes:

- **🧪 Generate Tests**: Create tests for the specific method
- **👁️ Preview Tests**: See what tests would be generated
- **📁 Generate Class Tests**: Create comprehensive tests for the entire class

## Requirements

- **VS Code**: Version 1.85.0 or higher
- **TestGen CLI**: Must be installed and accessible from command line
- **Node.js**: For TypeScript/JavaScript test generation
- **.NET SDK**: For C# test generation
- **Python**: For Python test generation

## Installation

### From VS Code Marketplace
1. Open VS Code
2. Go to Extensions (`Ctrl+Shift+X`)
3. Search for "TestGen"
4. Click Install

### From VSIX File
1. Download the `.vsix` file
2. Open VS Code
3. Press `Ctrl+Shift+P` → "Extensions: Install from VSIX"
4. Select the downloaded file

## Troubleshooting

### TestGen CLI Not Found
Ensure the TestGen CLI is installed and accessible:
```bash
# Test CLI availability
testgen --version

# If not found, add to PATH or set full path in settings
```

### Permission Issues
On macOS/Linux, ensure the CLI has execute permissions:
```bash
chmod +x /path/to/testgen
```

### Output Directory Issues
- Ensure the output directory exists or can be created
- Check write permissions for the target directory
- Use relative paths from workspace root

## Contributing

We welcome contributions! Please see our [Contributing Guide](https://github.com/ragarwal-pharos/test-case-generator/blob/main/CONTRIBUTING.md).

### Development Setup

1. Clone the repository
2. Install dependencies: `npm install`
3. Open in VS Code
4. Press `F5` to launch Extension Development Host

## License

This extension is licensed under the [MIT License](LICENSE).

## Support

- **GitHub Issues**: [Report bugs and feature requests](https://github.com/ragarwal-pharos/test-case-generator/issues)
- **Documentation**: [Full documentation](https://github.com/ragarwal-pharos/test-case-generator#readme)
- **Discussions**: [Community discussions](https://github.com/ragarwal-pharos/test-case-generator/discussions)

---

**Happy Testing!** 🧪✨
