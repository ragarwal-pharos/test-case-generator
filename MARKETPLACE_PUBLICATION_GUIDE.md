# Publishing TestGen VS Code Extension to Marketplace

## Prerequisites for Marketplace Publication

### 1. **Azure DevOps Account & Publisher**
```bash
# Install vsce globally
npm install -g @vscode/vsce

# Create a publisher account
vsce create-publisher your-publisher-name

# Login with Personal Access Token
vsce login your-publisher-name
```

### 2. **Required Repository Setup**
- **Public GitHub repository** (required for marketplace)
- **README.md** with screenshots and usage examples
- **CHANGELOG.md** with version history
- **LICENSE** file (already created)
- **Icon** (128x128 PNG recommended)

### 3. **Package.json Requirements**
```json
{
  "repository": {
    "type": "git", 
    "url": "https://github.com/ragarwal-pharos/test-case-generator.git"
  },
  "bugs": {
    "url": "https://github.com/ragarwal-pharos/test-case-generator/issues"
  },
  "homepage": "https://github.com/ragarwal-pharos/test-case-generator#readme",
  "icon": "media/icon.png",
  "galleryBanner": {
    "color": "#0066CC",
    "theme": "dark"
  },
  "categories": ["Testing", "Other"],
  "keywords": ["test", "generation", "csharp", "dotnet", "ai", "testing"]
}
```

## Publication Steps

### Step 1: Prepare Extension
```bash
cd "D:\test case generator\extensions\vscode"

# Add icon and update package.json
# Create screenshots for README
# Add comprehensive documentation
```

### Step 2: Package & Publish
```bash
# Package the extension
vsce package

# Publish to marketplace
vsce publish

# Or publish specific version
vsce publish 1.0.1
```

### Step 3: Verification
- Extension appears in VS Code Marketplace within 5-10 minutes
- Users can search "TestGen" or "test case generator" to find it
- Install count and ratings become visible

## Alternative Distribution Methods

### Option A: **GitHub Releases**
- Upload `.vsix` file to GitHub releases
- Users download and install manually: `code --install-extension testgen-vscode-1.0.0.vsix`

### Option B: **Private Registry**
- Host on private package registry
- Share with specific organizations/teams

### Option C: **Direct Distribution**
- Share `.vsix` file directly
- Manual installation via VS Code: Extensions → Install from VSIX

## Timeline for Global Availability

### Immediate (Today)
- ✅ Extension works locally
- ✅ Can share `.vsix` file for manual installation

### 1-2 Days (Marketplace Ready)
- Add icon and polish package.json
- Create comprehensive README with screenshots
- Set up Azure DevOps publisher account

### 1 Week (Published)
- Extension live in VS Code Marketplace
- Globally searchable and installable
- Analytics and user feedback available

## Required Actions for Marketplace

1. **Create Extension Icon** (128x128 PNG)
2. **Update package.json** with repository URLs
3. **Add Screenshots** to README
4. **Set up Azure DevOps** publisher account
5. **Publish with vsce**

Would you like me to help prepare the extension for marketplace publication?
