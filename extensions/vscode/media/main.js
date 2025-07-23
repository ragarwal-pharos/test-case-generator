// @ts-check

// Get access to the VS Code API
const vscode = acquireVsCodeApi();

document.addEventListener('DOMContentLoaded', function() {
    // Handle Generate Tests button
    const generateTestsBtn = document.getElementById('generateTests');
    if (generateTestsBtn) {
        generateTestsBtn.addEventListener('click', () => {
            vscode.postMessage({
                type: 'generateTests'
            });
        });
    }

    // Handle Open Settings button
    const openSettingsBtn = document.getElementById('openSettings');
    if (openSettingsBtn) {
        openSettingsBtn.addEventListener('click', () => {
            vscode.postMessage({
                type: 'openSettings'
            });
        });
    }
});

// Handle messages from the extension
window.addEventListener('message', event => {
    const message = event.data;
    
    switch (message.type) {
        case 'updatePreview':
            updatePreviewContent(message.content);
            break;
    }
});

function updatePreviewContent(content) {
    const previewElement = document.getElementById('previewContent');
    if (previewElement) {
        if (content && content.trim()) {
            previewElement.innerHTML = `<pre><code>${escapeHtml(content)}</code></pre>`;
        } else {
            previewElement.innerHTML = `
                <div class="placeholder">
                    <span class="codicon codicon-file-code"></span>
                    <p>No test preview available</p>
                    <p class="hint">Generate tests for a file to see preview here</p>
                </div>
            `;
        }
    }
}

function escapeHtml(text) {
    const div = document.createElement('div');
    div.textContent = text;
    return div.innerHTML;
}
