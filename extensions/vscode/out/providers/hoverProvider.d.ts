import * as vscode from 'vscode';
import { TestGeneratorService } from '../services/testGeneratorService';
export declare class TestGenHoverProvider implements vscode.HoverProvider {
    private testGeneratorService;
    constructor(testGeneratorService: TestGeneratorService);
    provideHover(document: vscode.TextDocument, position: vscode.Position, token: vscode.CancellationToken): Promise<vscode.Hover | undefined>;
    private isSupported;
    private isMethodOrClass;
}
//# sourceMappingURL=hoverProvider.d.ts.map