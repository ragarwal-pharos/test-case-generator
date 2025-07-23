import * as vscode from 'vscode';
import * as path from 'path';
import { spawn, ChildProcessWithoutNullStreams } from 'child_process';

export interface TestGenerationResult {
    success: boolean;
    testsGenerated: number;
    outputPath: string;
    errors: string[];
    warnings: string[];
}

export class TestGeneratorService {
    private readonly outputChannel: vscode.OutputChannel;

    constructor() {
        this.outputChannel = vscode.window.createOutputChannel('TestGen');
    }

    /**
     * Generate tests for a single file
     */
    async generateTestsForFile(
        fileUri: vscode.Uri, 
        progress?: vscode.Progress<{ message?: string; increment?: number }>,
        token?: vscode.CancellationToken
    ): Promise<TestGenerationResult> {
        const config = vscode.workspace.getConfiguration('testgen');
        const cliPath = config.get<string>('cliPath', 'testgen');
        const outputDir = config.get<string>('outputDirectory', 'tests');
        const framework = config.get<string>('testFramework', 'xunit');

        progress?.report({ message: 'Analyzing file...', increment: 10 });

        try {
            const workspaceFolder = vscode.workspace.getWorkspaceFolder(fileUri);
            if (!workspaceFolder) {
                throw new Error('File is not part of a workspace');
            }

            const args = [
                'generate',
                '--project', workspaceFolder.uri.fsPath,
                '--files', fileUri.fsPath,
                '--output', path.join(workspaceFolder.uri.fsPath, outputDir),
                '--framework', framework
            ];

            progress?.report({ message: 'Generating tests...', increment: 50 });

            const result = await this.executeTestGen(cliPath, args, progress, token);
            
            progress?.report({ message: 'Tests generated successfully!', increment: 40 });

            return result;
        } catch (error) {
            this.outputChannel.appendLine(`Error generating tests: ${error}`);
            throw error;
        }
    }

    /**
     * Generate tests for entire project
     */
    async generateTestsForProject(
        projectUri: vscode.Uri,
        progress?: vscode.Progress<{ message?: string; increment?: number }>,
        token?: vscode.CancellationToken
    ): Promise<TestGenerationResult> {
        const config = vscode.workspace.getConfiguration('testgen');
        const cliPath = config.get<string>('cliPath', 'testgen');
        const outputDir = config.get<string>('outputDirectory', 'tests');
        const framework = config.get<string>('testFramework', 'xunit');

        progress?.report({ message: 'Analyzing project structure...', increment: 5 });

        try {
            const args = [
                'generate',
                '--project', projectUri.fsPath,
                '--output', path.join(projectUri.fsPath, outputDir),
                '--framework', framework
            ];

            progress?.report({ message: 'Discovering files...', increment: 10 });

            const result = await this.executeTestGen(cliPath, args, progress, token);
            
            return result;
        } catch (error) {
            this.outputChannel.appendLine(`Error generating project tests: ${error}`);
            throw error;
        }
    }

    /**
     * Get test suggestions for a method or class
     */
    async getTestSuggestions(document: vscode.TextDocument, position: vscode.Position): Promise<string[]> {
        // This would integrate with your AI service to get intelligent test suggestions
        const config = vscode.workspace.getConfiguration('testgen');
        const enableAI = config.get<boolean>('enableAI', true);

        if (!enableAI) {
            return [];
        }

        try {
            // For now, return some mock suggestions
            // In a full implementation, this would call your AI service
            const text = document.getText();
            const line = document.lineAt(position.line);
            
            const suggestions: string[] = [];

            // Detect method signatures and suggest test scenarios
            if (line.text.includes('public') && (line.text.includes('(') && line.text.includes(')'))) {
                suggestions.push('Test with valid input parameters');
                suggestions.push('Test with null/empty parameters');
                suggestions.push('Test boundary conditions');
                suggestions.push('Test exception scenarios');
            }

            return suggestions;
        } catch (error) {
            this.outputChannel.appendLine(`Error getting test suggestions: ${error}`);
            return [];
        }
    }

    /**
     * Preview generated test content
     */
    async previewTests(fileUri: vscode.Uri): Promise<string> {
        try {
            const config = vscode.workspace.getConfiguration('testgen');
            const cliPath = config.get<string>('cliPath', 'testgen');
            
            const workspaceFolder = vscode.workspace.getWorkspaceFolder(fileUri);
            if (!workspaceFolder) {
                throw new Error('File is not part of a workspace');
            }

            const args = [
                'generate',
                '--project', workspaceFolder.uri.fsPath,
                '--files', fileUri.fsPath,
                '--preview', // Assuming your CLI supports preview mode
                '--no-write'
            ];

            const result = await this.executeTestGen(cliPath, args);
            return result.success ? 'Preview generated successfully' : 'Failed to generate preview';
        } catch (error) {
            this.outputChannel.appendLine(`Error previewing tests: ${error}`);
            throw error;
        }
    }

    /**
     * Execute TestGen CLI command
     */
    private async executeTestGen(
        cliPath: string, 
        args: string[],
        progress?: vscode.Progress<{ message?: string; increment?: number }>,
        token?: vscode.CancellationToken
    ): Promise<TestGenerationResult> {
        return new Promise((resolve, reject) => {
            this.outputChannel.appendLine(`Executing: ${cliPath} ${args.join(' ')}`);
            
            const process: ChildProcessWithoutNullStreams = spawn(cliPath, args, {
                cwd: vscode.workspace.workspaceFolders?.[0]?.uri.fsPath
            });

            let stdout = '';
            let stderr = '';

            process.stdout.on('data', (data) => {
                const output = data.toString();
                stdout += output;
                this.outputChannel.append(output);
                
                // Parse progress from CLI output
                this.parseProgressFromOutput(output, progress);
            });

            process.stderr.on('data', (data) => {
                const error = data.toString();
                stderr += error;
                this.outputChannel.append(error);
            });

            process.on('close', (code) => {
                if (token?.isCancellationRequested) {
                    reject(new Error('Operation was cancelled'));
                    return;
                }

                if (code === 0) {
                    const result = this.parseTestGenOutput(stdout);
                    resolve(result);
                } else {
                    reject(new Error(`TestGen CLI exited with code ${code}: ${stderr}`));
                }
            });

            process.on('error', (error) => {
                reject(new Error(`Failed to start TestGen CLI: ${error.message}`));
            });

            // Handle cancellation
            if (token) {
                token.onCancellationRequested(() => {
                    process.kill();
                });
            }
        });
    }

    /**
     * Parse progress information from CLI output
     */
    private parseProgressFromOutput(
        output: string, 
        progress?: vscode.Progress<{ message?: string; increment?: number }>
    ): void {
        if (!progress) return;

        // Parse your CLI's progress output format
        const progressMatches = output.match(/(\d+)%/);
        if (progressMatches) {
            const percentage = parseInt(progressMatches[1]);
            progress.report({ increment: percentage });
        }

        // Parse phase descriptions
        if (output.includes('📋 Analyzing project structure')) {
            progress.report({ message: 'Analyzing project structure...' });
        } else if (output.includes('🔍 Analyzing existing tests')) {
            progress.report({ message: 'Analyzing existing tests...' });
        } else if (output.includes('📁 Discovering files')) {
            progress.report({ message: 'Discovering files...' });
        } else if (output.includes('🔬 Analyzing source files')) {
            progress.report({ message: 'Analyzing source files...' });
        } else if (output.includes('⚡ Generating test cases')) {
            progress.report({ message: 'Generating test cases...' });
        } else if (output.includes('✅ Validating tests')) {
            progress.report({ message: 'Validating generated tests...' });
        }
    }

    /**
     * Parse TestGen CLI output to extract results
     */
    private parseTestGenOutput(output: string): TestGenerationResult {
        const result: TestGenerationResult = {
            success: true,
            testsGenerated: 0,
            outputPath: '',
            errors: [],
            warnings: []
        };

        try {
            // Parse test generation statistics from CLI output
            const testsMatch = output.match(/Test Cases Generated │\s*(\d+)/);
            if (testsMatch) {
                result.testsGenerated = parseInt(testsMatch[1]);
            }

            // Parse output path
            const pathMatch = output.match(/Output directory: (.+)/);
            if (pathMatch) {
                result.outputPath = pathMatch[1].trim();
            }

            // Parse errors and warnings
            const errorLines = output.split('\n').filter(line => line.includes('ERROR') || line.includes('fail:'));
            result.errors = errorLines;

            const warningLines = output.split('\n').filter(line => line.includes('WARNING') || line.includes('warn:'));
            result.warnings = warningLines;

        } catch (error) {
            result.success = false;
            result.errors.push(`Failed to parse CLI output: ${error}`);
        }

        return result;
    }

    /**
     * Check if TestGen CLI is available
     */
    async checkCliAvailability(): Promise<boolean> {
        const config = vscode.workspace.getConfiguration('testgen');
        const cliPath = config.get<string>('cliPath', 'testgen');

        return new Promise((resolve) => {
            const process = spawn(cliPath, ['--version'], { stdio: 'pipe' });
            
            process.on('close', (code) => {
                resolve(code === 0);
            });

            process.on('error', () => {
                resolve(false);
            });
        });
    }

    dispose(): void {
        this.outputChannel.dispose();
    }
}
