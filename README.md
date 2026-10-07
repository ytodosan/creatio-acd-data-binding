# Data Binding kit for Creatio

Package `AcdDataBinding` · maintainer Creatio Academy · version 1.1.0 · minimum Creatio 8.3.0 ·
see [CHANGELOG.md](CHANGELOG.md).

> **Branch `general-package`:** experimental 1.1.0 delivered as a General package (source-code schemas compiled by
> the installer, like GlbDataBinding) to make the app work right after installation. `main` stays an Assembly
> package, as Creatio Marketplace requires.

Adds "Bind data" actions to Creatio **classic UI** system pages, so configuration data can be captured
as data bindings in a user package. Works on **.NET Framework and .NET** Creatio.

It's a port of the GlbDataBinding package, which only works on .NET Framework.

> Status: in testing. Binding a system setting has been confirmed on Creatio 10.0 (.NET Framework).

## What it adds

| Page | Action | Data bindings created |
|---|---|---|
| Lookups section and lookup page | Bind lookup | `Lookup` |
| Any lookup's records | Bind data, Bind all data | the lookup's records |
| System settings | Bind setting | `SysSettings` + `SysSettingsValue` |
| System operations | Bind operation | `SysAdminOperation` + `SysAdminOperationGrantee` |
| Organizational roles | Bind role | `SysAdminUnit` |
| Workplaces | Bind workplace → Bind new workplace | `SysWorkplace` + `SysModuleInWorkplace` + `SysAdminUnitInWorkplace` |
| Workplaces | Bind workplace → Bind sections | `SysModuleInWorkplace` only (for workplaces that already exist on the target, such as ones installed with Creatio apps) |
| Section and detail view menus | Bind columns setup, Restore columns setup | `SysProfileData` |

The actions are visible to users with the `CanManageSolution` system operation. Bindings are saved to a
package with the current maintainer (the `Maintainer` system setting) that is editable and has the bound
schema in its hierarchy.

## How it works

- **Client:** `AcdDataBindingMixin` plus 13 classic schema extensions (`BasePageV2`, `BaseSectionV2`,
  `BaseGridDetailV2`, `LookupSection`, `SysSettingPage`, …). The browser selects the records and names the
  bindings.
- **Server:** the `AcdDataBindingService` web service (`GetPackages`, `RestoreColumnSetup`, `SaveBindings`)
  builds and saves the bindings through the public `Terrasoft.Core.Packages.PackageElementUtilities` API.
  - Route on .NET Framework: `/0/rest/AcdDataBindingService/<method>`
  - Route on .NET: `/rest/AcdDataBindingService/<method>`

The original package sent bindings from the browser to the platform `SchemaDataDesignerService`, and its
own C# used `ServiceStack.Text`, which .NET Creatio doesn't ship. This package moves that work into its own
service.

Differences from the platform data designer:
- Lookup values (and roles) are bound with **forced update on `Name`**, so a renamed value reaches every
  environment where the package is installed. System settings, operations, workplaces and the `Lookup`
  registration keep their own force-update rules. Re-binding resets column settings changed by hand in the
  data designer.
- Only columns that belong to packages in the target package's hierarchy are bound. Other columns are
  reported as warnings: the platform's automatic package dependency handling isn't available to
  configuration code.
- Data localizations are saved with the binding when the platform supports it (the
  `IncludeDataLocalizationInPackage` feature).

## Build and test

This is a [clio](https://github.com/Advance-Technologies-Foundation/clio) workspace.
Reference binaries go to `.application/` (not committed):

- `.application/net-framework-floor/core-bin`: the `lib/net40` DLLs of the `CreatioSDK` NuGet package for the
  **minimum supported Creatio** (8.3.0.3074), plus `Common.Logging.dll` / `Common.Logging.Core.dll` 3.4.1. The
  .NET Framework DLL is built against these: Creatio redirects older Terrasoft versions to the installed one,
  never newer ones, so a DLL built against a newer Creatio fails to load on older versions.
  `build-release.ps1` refuses to build if a shipped DLL references a version newer than the minimum.

- `.application/net-framework/core-bin`, `packages`, `bin`: the `bin`, `Terrasoft.Configuration/Pkg` and
  `Terrasoft.Configuration/bin` folders of a .NET Framework Creatio
- `.application/net-core/core-bin`: the `lib/netstandard2.0` DLLs of the `CreatioSDK` NuGet package

```bash
dotnet build packages/AcdDataBinding/Files/AcdDataBinding.csproj -c dev-nf
dotnet build packages/AcdDataBinding/Files/AcdDataBinding.csproj -c dev-n8
dotnet test tests/AcdDataBinding/AcdDataBinding.Tests.csproj -c dev-nf
```

`dev-nf` writes `Files/Bin/AcdDataBinding.dll` (.NET Framework). `dev-n8` writes
`Files/Bin/netstandard/AcdDataBinding.dll` (.NET).

## Install on an environment in file system mode

```bash
clio link-from-repository -e <ENV> --repoPath <this repo>/packages --packages AcdDataBinding --skip-preparation
clio pkg-to-db -e <ENV>
clio restart -e <ENV> --wait-ready
```

Then hard-reload the browser (Ctrl+Shift+R). Disable or remove GlbDataBinding first: both packages extend
the same pages.

## Release

```powershell
./build-release.ps1
```

It writes `release/Data-Binding-kit-for-Creatio-<X.Y.Z>.zip`, a Marketplace-ready archive with
`AcdDataBinding.gz` at its root, and a copy of `release/AcdDataBinding.gz` for direct installs. Before a release, raise the version in **both**
`packages/AcdDataBinding/descriptor.json` (`PackageVersion`) and `packages/AcdDataBinding/Files/app-descriptor.json`
(`Version`), then add a `CHANGELOG.md` entry. The script stops if the two versions differ.
The app icon source is `assets/app-icon.svg` (it's embedded in `app-descriptor.json`).

## Install a release on an environment

Before you start: deactivate or remove GlbDataBinding if it's installed (both extend the same pages). The user
who binds data needs the `CanManageSolution` system operation.

**Application Hub:** Application Hub → New application → Install from file → select
`Data-Binding-kit-for-Creatio-<X.Y.Z>.zip` → Install.

**clio:**

```bash
clio push-pkg "release/Data-Binding-kit-for-Creatio-1.0.0.zip" -e <ENV>
```

**Then restart the application. This step is required:** until the restart, the buttons appear but every
action fails, because the server returns 404 for `AcdDataBindingService`. Restart so it loads `AcdDataBinding.dll` (`clio restart -e <ENV> --wait-ready`, or recycle
the IIS application pool), and hard-reload the browser (Ctrl+Shift+R).

**If binding still fails with "service was not found (HTTP 404)"** after the restart, the server code isn't
active yet. Compile the package, then reload the browser:

```bash
clio compile-package AcdDataBinding -e <ENV> --wait
```

(or Configuration → Compile). This was needed on a Creatio 8.3.4 cloud environment, where installing and
`clio restart` weren't enough. The browser console shows the exact URL and status of the failing call
(`[Data Binding kit] ... failed: HTTP 404 ...`).

Check: Application Hub lists "Data Binding kit for Creatio" 1.0.0. Lookups shows **Bind lookup**, a system
setting page shows **Bind setting**, and a workplace shows **Bind workplace** with two options.
