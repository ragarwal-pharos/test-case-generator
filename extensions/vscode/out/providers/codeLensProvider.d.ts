import * as vscode from 'vscode';
import { TestGeneratorService } from '../services/testGeneratorService';
export declare class TestGenCodeLensProvider implements vscode.CodeLensProvider {
    private testGeneratorService;
    private _onDidChangeCodeLenses;
    readonly onDidChangeCodeLenses: vscode.Event<void>;
    constructor(testGeneratorService: TestGeneratorService);
    provideCodeLenses(document: vscode.TextDocument, token: vscode.CancellationToken): vscode.CodeLens[] | Thenable<vscode.CodeLens[]>;
    private isSupported;
    private isMethod;
    private isClass;
    refresh(): void;
}
//# sourceMappingURL=codeLensProvider.d.ts.map