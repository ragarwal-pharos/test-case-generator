import * as vscode from 'vscode';
export declare class TestExplorerProvider implements vscode.TreeDataProvider<TestItem> {
    private _onDidChangeTreeData;
    readonly onDidChangeTreeData: vscode.Event<TestItem | undefined | null | void>;
    constructor();
    refresh(): void;
    getTreeItem(element: TestItem): vscode.TreeItem;
    getChildren(element?: TestItem): Promise<TestItem[]>;
    private getTestFiles;
    private getTestMethods;
    private isTestFile;
    private isTestMethod;
    private extractMethodName;
}
export declare class TestItem extends vscode.TreeItem {
    readonly label: string;
    readonly collapsibleState: vscode.TreeItemCollapsibleState;
    readonly type: 'workspace' | 'testFile' | 'testMethod' | 'noTests';
    readonly resourceUri?: vscode.Uri | undefined;
    readonly tooltip?: string | undefined;
    constructor(label: string, collapsibleState: vscode.TreeItemCollapsibleState, type: 'workspace' | 'testFile' | 'testMethod' | 'noTests', resourceUri?: vscode.Uri | undefined, tooltip?: string | undefined);
}
//# sourceMappingURL=testExplorerProvider.d.ts.map