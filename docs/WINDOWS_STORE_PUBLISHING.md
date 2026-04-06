# Publishing MermaidDiagramApp to the Microsoft Store

This guide walks through publishing the app as an MSIX package to the Microsoft Store via Partner Center.

## Prerequisites

- **Microsoft Partner Center account** — Register at [partner.microsoft.com](https://partner.microsoft.com). Individual accounts cost a one-time $19 USD fee; company accounts cost $99 USD.
- **Visual Studio 2022** with the Windows App SDK workload installed
- **.NET 8.0 SDK** and **Windows 10/11 SDK** (10.0.26100 or later)
- The project builds successfully for all target platforms (x86, x64, ARM64)

## 1. Reserve Your App Name

1. Sign in to [Partner Center](https://partner.microsoft.com/dashboard)
2. Go to **Apps and Games** → **New product** → **MSIX or PWA app**
3. Reserve a unique app name (e.g., "Mermaid Diagram Editor")
4. Note the **Package/Identity/Name** and **Package/Identity/Publisher** values from the app identity page — you'll need these next

## 2. Update Package.appxmanifest

Replace the placeholder identity values with the ones from Partner Center:

```xml
<Identity
  Name="YOUR_PARTNER_CENTER_IDENTITY_NAME"
  Publisher="CN=YOUR_PARTNER_CENTER_PUBLISHER_ID"
  Version="1.0.0.0" />

<Properties>
  <DisplayName>Mermaid Diagram Editor</DisplayName>
  <PublisherDisplayName>Your Publisher Name</PublisherDisplayName>
  <Logo>Assets\StoreLogo.png</Logo>
</Properties>
```

Current values to replace:
- `Name="7b97c586-59ba-4e5a-81e5-2650612782d6"` → Partner Center identity name
- `Publisher="CN=leonw"` → Partner Center publisher CN
- `DisplayName` and `Description` in `<uap:VisualElements>` — update to your store listing name

> The Store handles code signing for Store-distributed apps, so you don't need your own certificate for submission.

## 3. Version Management

The Store requires each submission to have a higher version number than the previous one. Update the `Version` attribute in `Package.appxmanifest`:

```xml
<Identity ... Version="1.0.1.0" />
```

Use the format `Major.Minor.Build.Revision`. The Store ignores the Revision field (always treated as 0), so increment Build or Minor for updates.

## 4. Store Asset Requirements

### Required Logos (already in project)

The project already includes these in `Assets/`:

| Asset | Current File | Required Sizes |
|---|---|---|
| Square 44x44 | `Square44x44Logo.scale-200.png` | 88x88 px (scale-200) |
| Square 150x150 | `Square150x150Logo.scale-200.png` | 300x300 px (scale-200) |
| Wide 310x150 | `Wide310x150Logo.scale-200.png` | 620x300 px (scale-200) |
| Store Logo | `StoreLogo.png` | 50x50 px minimum |
| Splash Screen | `SplashScreen.scale-200.png` | 1240x600 px (scale-200) |

### Store Listing Screenshots

Partner Center requires at least one screenshot per device family. Recommended:
- **Desktop**: 1366x768 or 1920x1080 PNG screenshots (at least 1, up to 10)
- Capture the app showing a rendered Mermaid diagram and the split-pane editor

### Store Listing Content

Prepare the following for your submission:
- **Description** (up to 10,000 characters) — what the app does, key features
- **Release notes** — what's new in this version
- **Search terms** (up to 7, max 30 chars each) — e.g., "mermaid", "diagram", "UML", "markdown", "flowchart"
- **Category** — Developer tools or Productivity
- **Privacy policy URL** (required if the app accesses the network — the AI features do)

## 5. Build the MSIX Package

### Option A: Visual Studio (Recommended)

1. Open `MermaidDiagramApp.sln` in Visual Studio 2022
2. Set configuration to **Release**
3. Right-click the project → **Package and Publish** → **Create App Packages...**
4. Select **Microsoft Store under a new app name** (or associate with your reserved name)
5. Follow the wizard to associate with your Partner Center app
6. Select all target architectures: **x86**, **x64**, **ARM64**
7. Click **Create** — this produces an `.msixupload` file

The `.msixupload` bundle contains all three architecture packages and is what you upload to Partner Center.

### Option B: Command Line

```powershell
# Build the MSIX bundle for all platforms
& "C:\Program Files\dotnet\dotnet.exe" publish MermaidDiagramApp/MermaidDiagramApp.csproj `
  -c Release -p:Platform=x64

& "C:\Program Files\dotnet\dotnet.exe" publish MermaidDiagramApp/MermaidDiagramApp.csproj `
  -c Release -p:Platform=x86

& "C:\Program Files\dotnet\dotnet.exe" publish MermaidDiagramApp/MermaidDiagramApp.csproj `
  -c Release -p:Platform=ARM64
```

> For Store submission, the Visual Studio wizard is strongly recommended because it handles bundle creation, Store association, and manifest validation in one step.

### Build Notes

- **PublishReadyToRun** and **PublishTrimmed** are enabled for Release builds, producing smaller and faster packages
- **WindowsAppSDKSelfContained** is `true`, so the Windows App SDK runtime is bundled — users don't need to install it separately
- The `runFullTrust` capability is required for WebView2 and file system access. This is standard for WinUI 3 desktop apps and won't cause Store certification issues

## 6. Test the Package Locally

Before submitting, validate the package:

1. In Visual Studio, after creating the package, click **Launch Windows App Certification Kit**
2. WACK runs automated tests for crashes, performance, and security compliance
3. Fix any failures before submitting

You can also sideload the `.msix` to test installation:

```powershell
# Install the package locally for testing
Add-AppPackage -Path "path\to\MermaidDiagramApp_1.0.0.0_x64.msix"
```

## 7. Submit to Partner Center

1. Go to your app in [Partner Center](https://partner.microsoft.com/dashboard)
2. Click **Start a submission**
3. Fill in each section:

| Section | Action |
|---|---|
| **Packages** | Upload the `.msixupload` file |
| **Store listing** | Add description, screenshots, search terms |
| **Pricing** | Set to Free or choose a price tier |
| **Age ratings** | Complete the IARC questionnaire (likely rated for all ages) |
| **Properties** | Set category, privacy policy URL |

4. Click **Submit to the Store**

Certification typically takes 1–3 business days. You'll receive an email when the app is published or if changes are needed.

## 8. Post-Publication

### Updating the App

1. Increment the version in `Package.appxmanifest`
2. Build a new package (Step 5)
3. Create a new submission in Partner Center with the updated package
4. Add release notes describing the changes

### Monitoring

Partner Center provides:
- **Health reports** — crash analytics and error reports
- **Ratings and reviews** — user feedback
- **Acquisition reports** — download and install metrics
- **Usage reports** — daily/monthly active users

## Troubleshooting

| Issue | Solution |
|---|---|
| WACK fails on API usage | Check for any Win32 APIs not allowed in Store apps. WebView2 and WinUI 3 APIs are all permitted. |
| Package upload rejected | Ensure the Identity Name and Publisher in the manifest exactly match Partner Center values |
| Certification fails on privacy | Add a privacy policy URL if the app uses network (AI features use HTTP) |
| Version conflict | The new version number must be strictly higher than the currently published version |
| Large package size | Self-contained deployment increases size. This is expected with `WindowsAppSDKSelfContained=true`. Trimming in Release mode helps reduce it. |
