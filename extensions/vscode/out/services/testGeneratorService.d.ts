import * as vscode from 'vscode';
export interface TestGenerationResult {
    success: boolean;
    testsGenerated: number;
    outputPath: string;
    errors: string[];
    warnings: string[];
}
export declare class TestGeneratorService {
    private readonly outputChannel;
    constructor();
    /**
     * Generate tests for a single file
     */
    generateTestsForFile(fileUri: vscode.Uri, progress?: vscode.Progress<{
        message?: string;
        increment?: number;
    }>, token?: vscode.CancellationToken): Promise<TestGenerationResult>;
    /**
     * Generate tests for entire project
     */
    generateTestsForProject(projectUri: vscode.Uri, progress?: vscode.Progress<{
        message?: string;
        increment?: number;
    }>, token?: vscode.CancellationToken): Promise<TestGenerationResult>;
    /**
     * Get test suggestions for a method or class
     */
    getTestSuggestions(document: vscode.TextDocument, position: vscode.Position): Promise<string[]>;
    /**
     * Preview generated test content
     */
    previewTests(fileUri: vscode.Uri): Promise<string>;
    /**
     * Execute TestGen CLI command
     */
    private executeTestGen;
    /**
     * Parse progress information from CLI output
     */
    private parseProgressFromOutput;
    /**
     * Parse TestGen CLI output to extract results
     */
    private parseTestGenOutput;
    /**
     * Check if TestGen CLI is available
     */
    checkCliAvailability(): Promise<boolean>;
    dispose(): void;
}
//# sourceMappingURL=testGeneratorService.d.ts.map