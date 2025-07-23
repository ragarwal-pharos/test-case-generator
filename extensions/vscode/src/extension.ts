import * as vscode from 'vscode';
import { TestGeneratorService } from './services/testGeneratorService';
import { TestPreviewProvider } from './providers/testPreviewProvider';
import { TestExplorerProvider } from './providers/testExplorerProvider';
import { TestGenCodeLensProvider } from './providers/codeLensProvider';
import { TestGenHoverProvider } from './providers/hoverProvider';

let testGeneratorService: TestGeneratorService;
let testPreviewProvider: TestPreviewProvider;
let testExplorerProvider: TestExplorerProvider;

export function activate(context: vscode.ExtensionContext) {
    console.log('TestGen extension is now active!');

    // Initialize services
    testGeneratorService = new TestGeneratorService();
    testPreviewProvider = new TestPreviewProvider(context.extensionUri);
    testExplorerProvider = new TestExplorerProvider();

    // Register providers
    registerProviders(context);
    
    // Register commands
    registerCommands(context);
    
    // Register event handlers
    registerEventHandlers(context);
    
    // Show welcome message on first activation
    showWelcomeMessage(context);
}

function registerProviders(context: vscode.ExtensionContext) {
    // Code Lens Provider for inline test generation
    const codeLensProvider = new TestGenCodeLensProvider(testGeneratorService);
    context.subscriptions.push(
        vscode.languages.registerCodeLensProvider(
            [{ language: 'csharp' }, { language: 'typescript' }, { language: 'python' }],
            codeLensProvider
        )
    );

    // Hover Provider for test suggestions
    const hoverProvider = new TestGenHoverProvider(testGeneratorService);
    context.subscriptions.push(
        vscode.languages.registerHoverProvider(
            [{ language: 'csharp' }, { language: 'typescript' }, { language: 'python' }],
            hoverProvider
        )
    );

    // Tree Data Provider for Test Explorer
    context.subscriptions.push(
        vscode.window.registerTreeDataProvider('testgenExplorer', testExplorerProvider)
    );

    // Webview Provider for Test Preview
    context.subscriptions.push(
        vscode.window.registerWebviewViewProvider('testPreview', testPreviewProvider)
    );
}

function registerCommands(context: vscode.ExtensionContext) {
    // Generate Tests Command
    context.subscriptions.push(
        vscode.commands.registerCommand('testgen.generateTests', async (uri?: vscode.Uri) => {
            try {
                await generateTestsCommand(uri);
            } catch (error) {
                vscode.window.showErrorMessage(`Failed to generate tests: ${error}`);
            }
        })
    );

    // Generate Tests for Current File
    context.subscriptions.push(
        vscode.commands.registerCommand('testgen.generateTestsForFile', async () => {
            try {
                const activeEditor = vscode.window.activeTextEditor;
                if (!activeEditor) {
                    vscode.window.showWarningMessage('No active file to generate tests for.');
                    return;
                }
                await generateTestsCommand(activeEditor.document.uri);
            } catch (error) {
                vscode.window.showErrorMessage(`Failed to generate tests: ${error}`);
            }
        })
    );

    // Generate Tests for Entire Project
    context.subscriptions.push(
        vscode.commands.registerCommand('testgen.generateTestsForProject', async () => {
            try {
                if (!vscode.workspace.workspaceFolders || vscode.workspace.workspaceFolders.length === 0) {
                    vscode.window.showWarningMessage('No workspace folder open.');
                    return;
                }
                
                const workspaceFolder = vscode.workspace.workspaceFolders[0];
                await generateProjectTestsCommand(workspaceFolder.uri);
            } catch (error) {
                vscode.window.showErrorMessage(`Failed to generate project tests: ${error}`);
            }
        })
    );

    // Open Settings Command
    context.subscriptions.push(
        vscode.commands.registerCommand('testgen.openSettings', () => {
            vscode.commands.executeCommand('workbench.action.openSettings', 'testgen');
        })
    );

    // Show Test Preview Command
    context.subscriptions.push(
        vscode.commands.registerCommand('testgen.showTestPreview', async (testContent?: string) => {
            await testPreviewProvider.showPreview(testContent);
        })
    );

    // Refresh Test Explorer Command
    context.subscriptions.push(
        vscode.commands.registerCommand('testgen.refreshExplorer', () => {
            testExplorerProvider.refresh();
        })
    );
}

function registerEventHandlers(context: vscode.ExtensionContext) {
    // Auto-generate tests on file save (if enabled)
    context.subscriptions.push(
        vscode.workspace.onDidSaveTextDocument(async (document) => {
            const config = vscode.workspace.getConfiguration('testgen');
            const autoGenerate = config.get<boolean>('autoGenerate', false);
            
            if (autoGenerate && isSupportedLanguage(document.languageId)) {
                await generateTestsCommand(document.uri, true);
            }
        })
    );

    // Update test explorer when workspace changes
    context.subscriptions.push(
        vscode.workspace.onDidChangeWorkspaceFolders(() => {
            testExplorerProvider.refresh();
        })
    );
}

async function generateTestsCommand(uri?: vscode.Uri, silent: boolean = false) {
    if (!uri) {
        const activeEditor = vscode.window.activeTextEditor;
        if (!activeEditor) {
            vscode.window.showWarningMessage('No file selected or active.');
            return;
        }
        uri = activeEditor.document.uri;
    }

    if (!isSupportedLanguage(getLanguageFromUri(uri))) {
        vscode.window.showWarningMessage('Unsupported file type. TestGen supports C#, TypeScript, and Python files.');
        return;
    }

    const config = vscode.workspace.getConfiguration('testgen');
    const showProgress = config.get<boolean>('showProgress', true);

    if (showProgress && !silent) {
        await vscode.window.withProgress({
            location: vscode.ProgressLocation.Notification,
            title: 'Generating Tests',
            cancellable: true
        }, async (progress, token) => {
            return await testGeneratorService.generateTestsForFile(uri!, progress, token);
        });
    } else {
        await testGeneratorService.generateTestsForFile(uri);
    }

    // Refresh test explorer
    testExplorerProvider.refresh();

    if (!silent) {
        vscode.window.showInformationMessage('Tests generated successfully!');
    }
}

async function generateProjectTestsCommand(workspaceUri: vscode.Uri) {
    const config = vscode.workspace.getConfiguration('testgen');
    const showProgress = config.get<boolean>('showProgress', true);

    if (showProgress) {
        await vscode.window.withProgress({
            location: vscode.ProgressLocation.Notification,
            title: 'Generating Tests for Project',
            cancellable: true
        }, async (progress, token) => {
            return await testGeneratorService.generateTestsForProject(workspaceUri, progress, token);
        });
    } else {
        await testGeneratorService.generateTestsForProject(workspaceUri);
    }

    // Refresh test explorer
    testExplorerProvider.refresh();
    
    vscode.window.showInformationMessage('Project tests generated successfully!');
}

function isSupportedLanguage(languageId: string): boolean {
    return ['csharp', 'typescript', 'python'].includes(languageId);
}

function getLanguageFromUri(uri: vscode.Uri): string {
    const extension = uri.path.split('.').pop()?.toLowerCase();
    switch (extension) {
        case 'cs': return 'csharp';
        case 'ts': 
        case 'tsx': return 'typescript';
        case 'py': return 'python';
        default: return '';
    }
}

async function showWelcomeMessage(context: vscode.ExtensionContext) {
    const hasShownWelcome = context.globalState.get<boolean>('hasShownWelcome', false);
    
    if (!hasShownWelcome) {
        const result = await vscode.window.showInformationMessage(
            'Welcome to TestGen! Generate intelligent test cases with AI-powered analysis.',
            'Get Started',
            'Settings',
            'Don\'t Show Again'
        );

        switch (result) {
            case 'Get Started':
                vscode.commands.executeCommand('vscode.open', vscode.Uri.parse('https://github.com/ragarwal-pharos/test-case-generator#readme'));
                break;
            case 'Settings':
                vscode.commands.executeCommand('testgen.openSettings');
                break;
            case 'Don\'t Show Again':
                await context.globalState.update('hasShownWelcome', true);
                break;
        }
    }
}

export function deactivate() {
    console.log('TestGen extension is now deactivated.');
}
