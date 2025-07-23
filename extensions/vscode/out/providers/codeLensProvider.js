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
exports.TestGenCodeLensProvider = void 0;
const vscode = __importStar(require("vscode"));
class TestGenCodeLensProvider {
    constructor(testGeneratorService) {
        this.testGeneratorService = testGeneratorService;
        this._onDidChangeCodeLenses = new vscode.EventEmitter();
        this.onDidChangeCodeLenses = this._onDidChangeCodeLenses.event;
    }
    provideCodeLenses(document, token) {
        const codeLenses = [];
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
    isSupported(languageId) {
        return ['csharp', 'typescript', 'python'].includes(languageId);
    }
    isMethod(line) {
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
    isClass(line) {
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
    refresh() {
        this._onDidChangeCodeLenses.fire();
    }
}
exports.TestGenCodeLensProvider = TestGenCodeLensProvider;
//# sourceMappingURL=codeLensProvider.js.map