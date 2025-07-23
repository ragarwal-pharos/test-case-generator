import * as vscode from 'vscode';
import { TestGeneratorService } from '../services/testGeneratorService';

export class TestGenHoverProvider implements vscode.HoverProvider {
    constructor(private testGeneratorService: TestGeneratorService) {}

    async provideHover(
        document: vscode.TextDocument, 
        position: vscode.Position, 
        token: vscode.CancellationToken
    ): Promise<vscode.Hover | undefined> {
        
        if (!this.isSupported(document.languageId)) {
            return undefined;
        }

        const line = document.lineAt(position.line);
        const lineText = line.text;

        // Only show hover for methods and classes
        if (!this.isMethodOrClass(lineText)) {
            return undefined;
        }

        try {
            const suggestions = await this.testGeneratorService.getTestSuggestions(document, position);
            
            if (suggestions.length === 0) {
                return undefined;
            }

            const markdownContent = new vscode.MarkdownString();
            markdownContent.isTrusted = true;
            
            markdownContent.appendMarkdown('## 🧪 Test Suggestions\n\n');
            
            suggestions.forEach((suggestion, index) => {
                markdownContent.appendMarkdown(`${index + 1}. ${suggestion}\n\n`);
            });

            markdownContent.appendMarkdown('---\n\n');
            markdownContent.appendMarkdown('[Generate Tests](command:testgen.generateTestsForFile) | ');
            markdownContent.appendMarkdown('[Preview Tests](command:testgen.showTestPreview)');

            return new vscode.Hover(markdownContent);
            
        } catch (error) {
            console.error('Error providing hover information:', error);
            return undefined;
        }
    }

    private isSupported(languageId: string): boolean {
        return ['csharp', 'typescript', 'python'].includes(languageId);
    }

    private isMethodOrClass(line: string): boolean {
        const trimmedLine = line.trim();
        
        // Check for methods
        if (trimmedLine.includes('public') || trimmedLine.includes('private') || 
            trimmedLine.includes('protected') || trimmedLine.includes('function') ||
            trimmedLine.startsWith('def ')) {
            if (trimmedLine.includes('(') && trimmedLine.includes(')')) {
                return true;
            }
        }

        // Check for classes
        if (trimmedLine.includes('class ')) {
            return true;
        }

        return false;
    }
}
