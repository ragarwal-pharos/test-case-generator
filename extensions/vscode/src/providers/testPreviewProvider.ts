import * as vscode from 'vscode';

export class TestPreviewProvider implements vscode.WebviewViewProvider {
    public static readonly viewType = 'testPreview';
    private _view?: vscode.WebviewView;

    constructor(private readonly _extensionUri: vscode.Uri) {}

    public resolveWebviewView(
        webviewView: vscode.WebviewView,
        context: vscode.WebviewViewResolveContext,
        _token: vscode.CancellationToken,
    ) {
        this._view = webviewView;

        webviewView.webview.options = {
            enableScripts: true,
            localResourceRoots: [this._extensionUri]
        };

        webviewView.webview.html = this._getHtmlForWebview(webviewView.webview);

        // Handle messages from the webview
        webviewView.webview.onDidReceiveMessage(data => {
            switch (data.type) {
                case 'generateTests':
                    vscode.commands.executeCommand('testgen.generateTestsForFile');
                    break;
                case 'openSettings':
                    vscode.commands.executeCommand('testgen.openSettings');
                    break;
            }
        });
    }

    public async showPreview(testContent?: string) {
        if (this._view) {
            this._view.show?.(true);
            
            if (testContent) {
                this._view.webview.postMessage({
                    type: 'updatePreview',
                    content: testContent
                });
            }
        }
    }

    private _getHtmlForWebview(webview: vscode.Webview) {
        // Get the local path to main script run in the webview
        const scriptUri = webview.asWebviewUri(vscode.Uri.joinPath(this._extensionUri, 'media', 'main.js'));
        const styleResetUri = webview.asWebviewUri(vscode.Uri.joinPath(this._extensionUri, 'media', 'reset.css'));
        const styleVSCodeUri = webview.asWebviewUri(vscode.Uri.joinPath(this._extensionUri, 'media', 'vscode.css'));
        const styleMainUri = webview.asWebviewUri(vscode.Uri.joinPath(this._extensionUri, 'media', 'main.css'));

        // Use a nonce to only allow specific scripts to be run
        const nonce = getNonce();

        return `<!DOCTYPE html>
            <html lang="en">
            <head>
                <meta charset="UTF-8">
                <meta http-equiv="Content-Security-Policy" content="default-src 'none'; style-src ${webview.cspSource}; script-src 'nonce-${nonce}';">
                <meta name="viewport" content="width=device-width, initial-scale=1.0">
                <link href="${styleResetUri}" rel="stylesheet">
                <link href="${styleVSCodeUri}" rel="stylesheet">
                <link href="${styleMainUri}" rel="stylesheet">
                <title>Test Preview</title>
            </head>
            <body>
                <div class="container">
                    <h1>🧪 Test Preview</h1>
                    
                    <div class="actions">
                        <button class="button primary" id="generateTests">
                            <span class="codicon codicon-beaker"></span>
                            Generate Tests
                        </button>
                        <button class="button secondary" id="openSettings">
                            <span class="codicon codicon-settings-gear"></span>
                            Settings
                        </button>
                    </div>

                    <div class="preview-section">
                        <h2>Preview</h2>
                        <div id="previewContent" class="preview-content">
                            <div class="placeholder">
                                <span class="codicon codicon-file-code"></span>
                                <p>No test preview available</p>
                                <p class="hint">Generate tests for a file to see preview here</p>
                            </div>
                        </div>
                    </div>

                    <div class="info-section">
                        <h3>Quick Start</h3>
                        <ul>
                            <li>Open a C#, TypeScript, or Python file</li>
                            <li>Right-click and select "Generate Tests"</li>
                            <li>Or use the Command Palette: <code>TestGen: Generate Tests</code></li>
                        </ul>
                    </div>
                </div>

                <script nonce="${nonce}" src="${scriptUri}"></script>
            </body>
            </html>`;
    }
}

function getNonce() {
    let text = '';
    const possible = 'ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789';
    for (let i = 0; i < 32; i++) {
        text += possible.charAt(Math.floor(Math.random() * possible.length));
    }
    return text;
}
