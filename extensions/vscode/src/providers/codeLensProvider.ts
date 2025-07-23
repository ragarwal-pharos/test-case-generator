import * as vscode from 'vscode';
import { TestGeneratorService } from '../services/testGeneratorService';

export class TestGenCodeLensProvider implements vscode.CodeLensProvider {
    private _onDidChangeCodeLenses: vscode.EventEmitter<void> = new vscode.EventEmitter<void>();
    public readonly onDidChangeCodeLenses: vscode.Event<void> = this._onDidChangeCodeLenses.event;

    constructor(private testGeneratorService: TestGeneratorService) {}

    provideCodeLenses(document: vscode.TextDocument, token: vscode.CancellationToken): vscode.CodeLens[] | Thenable<vscode.CodeLens[]> {
        const codeLenses: vscode.CodeLens[] = [];

        if (!this.isSupported(document.languageId)) {
            return codeLenses;
        }

        const text = document.getText();
        const lines = text.split('\n');

        for (let i = 0; i < lines.length; i++) {
            const line = lines[i];
            
            // Detect methods that need tests
            if (this.isMethod(line)) {
                const range = new vscode.Range(i, 0, i, line.length);
                
                // Generate Tests Code Lens
                const generateTestsLens = new vscode.CodeLens(range, {
                    title: '$(beaker) Generate Tests',
                    command: 'testgen.generateTestsForMethod',
                    arguments: [document.uri, i]
                });

                // Preview Tests Code Lens
                const previewTestsLens = new vscode.CodeLens(range, {
                    title: '$(preview) Preview Tests',
                    command: 'testgen.previewTestsForMethod',
                    arguments: [document.uri, i]
                });

                codeLenses.push(generateTestsLens, previewTestsLens);
            }

            // Detect classes that need tests
            if (this.isClass(line)) {
                const range = new vscode.Range(i, 0, i, line.length);
                
                const generateClassTestsLens = new vscode.CodeLens(range, {
                    title: '$(folder-library) Generate Class Tests',
                    command: 'testgen.generateTestsForClass',
                    arguments: [document.uri, i]
                });

                codeLenses.push(generateClassTestsLens);
            }
        }

        return codeLenses;
    }

    private isSupported(languageId: string): boolean {
        return ['csharp', 'typescript', 'python'].includes(languageId);
    }

    private isMethod(line: string): boolean {
        const trimmedLine = line.trim();
        
        // C# method detection
        if (trimmedLine.includes('public') || trimmedLine.includes('private') || trimmedLine.includes('protected')) {
            if (trimmedLine.includes('(') && trimmedLine.includes(')') && !trimmedLine.includes('class')) {
                return true;
            }
        }

        // TypeScript method detection
        if (trimmedLine.includes('function') || 
            (trimmedLine.includes('(') && trimmedLine.includes(')') && trimmedLine.includes('=>'))) {
            return true;
        }

        // Python method detection
        if (trimmedLine.startsWith('def ') && trimmedLine.includes('(') && trimmedLine.includes('):')) {
            return true;
        }

        return false;
    }

    private isClass(line: string): boolean {
        const trimmedLine = line.trim();
        
        // C# class detection
        if (trimmedLine.includes('public class') || trimmedLine.includes('internal class') || 
            trimmedLine.includes('private class') || trimmedLine.includes('protected class')) {
            return true;
        }

        // TypeScript class detection
        if (trimmedLine.startsWith('class ') || trimmedLine.includes('export class')) {
            return true;
        }

        // Python class detection
        if (trimmedLine.startsWith('class ') && trimmedLine.includes(':')) {
            return true;
        }

        return false;
    }

    public refresh(): void {
        this._onDidChangeCodeLenses.fire();
    }
}
