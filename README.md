# AcdDataBinding

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
| Workplaces | Bind workplace | `SysWorkplace` + `SysModuleInWorkplace` + `SysAdminUnitInWorkplace` |
| Section and detail view menus | Bind columns setup, Restore columns setup | `SysProfileData` |

The actions are visible to users with the `CanManageSolution` system operation. Bindings are saved to a
package with the current maintainer (the `Maintainer` system setting) that is editable and has the bound
schema in its hierarchy. The buttons have rounder corners and a slightly deeper blue, so they're easy to
tell apart from the original package's buttons.

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
- Only columns that belong to packages in the target package's hierarchy are bound. Other columns are
  reported as warnings: the platform's automatic package dependency handling isn't available to
  configuration code.
- Data localizations are saved with the binding when the platform supports it (the
  `IncludeDataLocalizationInPackage` feature).

## Build and test

This is a [clio](https://github.com/Advance-Technologies-Foundation/clio) workspace.
Reference binaries go to `.application/` (not committed):

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
