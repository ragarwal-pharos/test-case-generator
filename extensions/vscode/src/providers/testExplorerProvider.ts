import * as vscode from 'vscode';
import * as path from 'path';

export class TestExplorerProvider implements vscode.TreeDataProvider<TestItem> {
    private _onDidChangeTreeData: vscode.EventEmitter<TestItem | undefined | null | void> = new vscode.EventEmitter<TestItem | undefined | null | void>();
    readonly onDidChangeTreeData: vscode.Event<TestItem | undefined | null | void> = this._onDidChangeTreeData.event;

    constructor() {}

    refresh(): void {
        this._onDidChangeTreeData.fire();
    }

    getTreeItem(element: TestItem): vscode.TreeItem {
        return element;
    }

    async getChildren(element?: TestItem): Promise<TestItem[]> {
        if (!vscode.workspace.workspaceFolders) {
            return [];
        }

        if (!element) {
            // Root level - show workspace folders
            return vscode.workspace.workspaceFolders.map(folder => 
                new TestItem(
                    folder.name,
                    vscode.TreeItemCollapsibleState.Expanded,
                    'workspace',
                    folder.uri
                )
            );
        }

        if (element.type === 'workspace') {
            // Show test folders and files
            return await this.getTestFiles(element.resourceUri!);
        }

        if (element.type === 'testFile') {
            // Show test methods in file
            return await this.getTestMethods(element.resourceUri!);
        }

        return [];
    }

    private async getTestFiles(workspaceUri: vscode.Uri): Promise<TestItem[]> {
        const testItems: TestItem[] = [];

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
                                testItems.push(new TestItem(
                                    name,
                                    vscode.TreeItemCollapsibleState.Collapsed,
                                    'testFile',
                                    fileUri
                                ));
                            }
                        }
                    }
                } catch {
                    // Directory doesn't exist, continue
                }
            }

            // If no test directories found, show option to generate tests
            if (testItems.length === 0) {
                testItems.push(new TestItem(
                    'No tests found',
                    vscode.TreeItemCollapsibleState.None,
                    'noTests',
                    undefined,
                    'Generate tests to see them here'
                ));
            }

        } catch (error) {
            console.error('Error getting test files:', error);
        }

        return testItems;
    }

    private async getTestMethods(fileUri: vscode.Uri): Promise<TestItem[]> {
        const testMethods: TestItem[] = [];

        try {
            const document = await vscode.workspace.openTextDocument(fileUri);
            const text = document.getText();
            const lines = text.split('\n');

            for (let i = 0; i < lines.length; i++) {
                const line = lines[i];
                if (this.isTestMethod(line)) {
                    const methodName = this.extractMethodName(line);
                    if (methodName) {
                        testMethods.push(new TestItem(
                            methodName,
                            vscode.TreeItemCollapsibleState.None,
                            'testMethod',
                            fileUri,
                            `Test method at line ${i + 1}`
                        ));
                    }
                }
            }

        } catch (error) {
            console.error('Error getting test methods:', error);
        }

        return testMethods;
    }

    private isTestFile(filename: string): boolean {
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

    private isTestMethod(line: string): boolean {
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

    private extractMethodName(line: string): string | undefined {
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

export class TestItem extends vscode.TreeItem {
    constructor(
        public readonly label: string,
        public readonly collapsibleState: vscode.TreeItemCollapsibleState,
        public readonly type: 'workspace' | 'testFile' | 'testMethod' | 'noTests',
        public readonly resourceUri?: vscode.Uri,
        public readonly tooltip?: string
    ) {
        super(label, collapsibleState);

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
