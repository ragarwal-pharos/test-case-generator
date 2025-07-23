"use strict";
var __createBinding = (this && this.__createBinding) || (Object.create ? (function(o, m, k, k2) {
    if (k2 === undefined) k2 = k;
    var desc = Object.getOwnPropertyDescriptor(m, k);
    if (!desc || ("get" in desc ? !m.__esModule : desc.writable || desc.configurable)) {
      desc = { enumerable: true, get: function() { return m[k]; } };
    }
    Object.defineProperty(o, k2, desc);
}) : (function(o, m, k, k2) {
    if (k2 === undefined) k2 = k;
    o[k2] = m[k];
}));
var __setModuleDefault = (this && this.__setModuleDefault) || (Object.create ? (function(o, v) {
    Object.defineProperty(o, "default", { enumerable: true, value: v });
}) : function(o, v) {
    o["default"] = v;
});
var __importStar = (this && this.__importStar) || function (mod) {
    if (mod && mod.__esModule) return mod;
    var result = {};
    if (mod != null) for (var k in mod) if (k !== "default" && Object.prototype.hasOwnProperty.call(mod, k)) __createBinding(result, mod, k);
    __setModuleDefault(result, mod);
    return result;
};
Object.defineProperty(exports, "__esModule", { value: true });
exports.TestItem = exports.TestExplorerProvider = void 0;
const vscode = __importStar(require("vscode"));
const path = __importStar(require("path"));
class TestExplorerProvider {
    constructor() {
        this._onDidChangeTreeData = new vscode.EventEmitter();
        this.onDidChangeTreeData = this._onDidChangeTreeData.event;
    }
    refresh() {
        this._onDidChangeTreeData.fire();
    }
    getTreeItem(element) {
        return element;
    }
    async getChildren(element) {
        if (!vscode.workspace.workspaceFolders) {
            return [];
        }
        if (!element) {
            // Root level - show workspace folders
            return vscode.workspace.workspaceFolders.map(folder => new TestItem(folder.name, vscode.TreeItemCollapsibleState.Expanded, 'workspace', folder.uri));
        }
        if (element.type === 'workspace') {
            // Show test folders and files
            return await this.getTestFiles(element.resourceUri);
        }
        if (element.type === 'testFile') {
            // Show test methods in file
            return await this.getTestMethods(element.resourceUri);
        }
        return [];
    }
    async getTestFiles(workspaceUri) {
        const testItems = [];
        try {
            // Look for test directories
            const testDirs = ['tests', 'test', 'Test', 'Tests', '__tests__', 'spec'];
            for (const testDir of testDirs) {
                const testDirPath = path.join(workspaceUri.fsPath, testDir);
                const testDirUri = vscode.Uri.file(testDirPath);
                try {
                    const stat = await vscode.workspace.fs.stat(testDirUri);
                    if (stat.type === vscode.FileType.Directory) {
                        const files = await vscode.workspace.fs.readDirectory(testDirUri);
                        for (const [name, type] of files) {
                            if (type === vscode.FileType.File && this.isTestFile(name)) {
                                const fileUri = vscode.Uri.file(path.join(testDirPath, name));
                                testItems.push(new TestItem(name, vscode.TreeItemCollapsibleState.Collapsed, 'testFile', fileUri));
                            }
                        }
                    }
                }
                catch {
                    // Directory doesn't exist, continue
                }
            }
            // If no test directories found, show option to generate tests
            if (testItems.length === 0) {
                testItems.push(new TestItem('No tests found', vscode.TreeItemCollapsibleState.None, 'noTests', undefined, 'Generate tests to see them here'));
            }
        }
        catch (error) {
            console.error('Error getting test files:', error);
        }
        return testItems;
    }
    async getTestMethods(fileUri) {
        const testMethods = [];
        try {
            const document = await vscode.workspace.openTextDocument(fileUri);
            const text = document.getText();
            const lines = text.split('\n');
            for (let i = 0; i < lines.length; i++) {
                const line = lines[i];
                if (this.isTestMethod(line)) {
                    const methodName = this.extractMethodName(line);
                    if (methodName) {
                        testMethods.push(new TestItem(methodName, vscode.TreeItemCollapsibleState.None, 'testMethod', fileUri, `Test method at line ${i + 1}`));
                    }
                }
            }
        }
        catch (error) {
            console.error('Error getting test methods:', error);
        }
        return testMethods;
    }
    isTestFile(filename) {
        const testPatterns = [
            /\.test\./,
            /\.spec\./,
            /Tests?\./,
            /Test\.cs$/,
            /_test\./,
            /_spec\./
        ];
        return testPatterns.some(pattern => pattern.test(filename));
    }
    isTestMethod(line) {
        const trimmedLine = line.trim();
        // C# test methods
        if (trimmedLine.includes('[Test]') || trimmedLine.includes('[Fact]') ||
            trimmedLine.includes('[Theory]') || trimmedLine.includes('[TestMethod]')) {
            return true;
        }
        // TypeScript/JavaScript test methods
        if (trimmedLine.startsWith('it(') || trimmedLine.startsWith('test(') ||
            trimmedLine.startsWith('describe(')) {
            return true;
        }
        // Python test methods
        if (trimmedLine.startsWith('def test_')) {
            return true;
        }
        return false;
    }
    extractMethodName(line) {
        const trimmedLine = line.trim();
        // C# method name extraction
        const csharpMatch = trimmedLine.match(/public\s+(?:void|Task|async\s+Task)\s+(\w+)/);
        if (csharpMatch) {
            return csharpMatch[1];
        }
        // TypeScript/JavaScript test name extraction
        const jsMatch = trimmedLine.match(/(?:it|test|describe)\(['"`]([^'"`]+)['"`]/);
        if (jsMatch) {
            return jsMatch[1];
        }
        // Python test name extraction
        const pythonMatch = trimmedLine.match(/def\s+(test_\w+)/);
        if (pythonMatch) {
            return pythonMatch[1];
        }
        return undefined;
    }
}
exports.TestExplorerProvider = TestExplorerProvider;
class TestItem extends vscode.TreeItem {
    constructor(label, collapsibleState, type, resourceUri, tooltip) {
        super(label, collapsibleState);
        this.label = label;
        this.collapsibleState = collapsibleState;
        this.type = type;
        this.resourceUri = resourceUri;
        this.tooltip = tooltip;
        this.tooltip = tooltip || this.label;
        this.contextValue = type;
        // Set appropriate icons
        switch (type) {
            case 'workspace':
                this.iconPath = new vscode.ThemeIcon('folder');
                break;
            case 'testFile':
                this.iconPath = new vscode.ThemeIcon('file-code');
                this.command = {
                    command: 'vscode.open',
                    title: 'Open Test File',
                    arguments: [resourceUri]
                };
                break;
            case 'testMethod':
                this.iconPath = new vscode.ThemeIcon('beaker');
                break;
            case 'noTests':
                this.iconPath = new vscode.ThemeIcon('info');
                this.command = {
                    command: 'testgen.generateTestsForProject',
                    title: 'Generate Tests'
                };
                break;
        }
    }
}
exports.TestItem = TestItem;
//# sourceMappingURL=testExplorerProvider.js.map