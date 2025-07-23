import * as vscode from 'vscode';
export declare class TestPreviewProvider implements vscode.WebviewViewProvider {
    private readonly _extensionUri;
    static readonly viewType = "testPreview";
    private _view?;
    constructor(_extensionUri: vscode.Uri);
    resolveWebviewView(webviewView: vscode.WebviewView, context: vscode.WebviewViewResolveContext, _token: vscode.CancellationToken): void;
    showPreview(testContent?: string): Promise<void>;
    private _getHtmlForWebview;
}
//# sourceMappingURL=testPreviewProvider.d.ts.map