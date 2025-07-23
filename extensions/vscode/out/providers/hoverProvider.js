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
exports.TestGenHoverProvider = void 0;
const vscode = __importStar(require("vscode"));
class TestGenHoverProvider {
    constructor(testGeneratorService) {
        this.testGeneratorService = testGeneratorService;
    }
    async provideHover(document, position, token) {
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
        }
        catch (error) {
            console.error('Error providing hover information:', error);
            return undefined;
        }
    }
    isSupported(languageId) {
        return ['csharp', 'typescript', 'python'].includes(languageId);
    }
    isMethodOrClass(line) {
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
exports.TestGenHoverProvider = TestGenHoverProvider;
//# sourceMappingURL=hoverProvider.js.map