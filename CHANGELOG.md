# Changelog

All notable changes to **Data Binding kit for Creatio** (package `AcdDataBinding`, maintainer Creatio Academy).
Versions follow the X.Y.Z format required by Creatio Marketplace.

## 1.1.0 - 2026-10-07 (branch general-package, experimental)

### Changed
- The package is a **General** package instead of an Assembly package. Its C# is delivered as source-code schemas
  (`AcdDataBindingService`, `AcdDataBindingStore`, ...) and compiled by the installer, like the original
  GlbDataBinding. Goal: the app works right after installation, with no restart or package compile. Trade-offs:
  installation compiles the configuration (slower, briefly reloads the app), and Creatio Marketplace asks for
  functionality in an assembly package.
- The release ships no prebuilt DLLs.

## 1.0.0 - 2026-10-06

First release.

### Added
- "Bind data" actions on Classic UI system pages: lookups (Bind lookup, Bind data, Bind all data), system
  settings (Bind setting), system operations (Bind operation), organizational roles (Bind role), workplaces
  (Bind workplace) and section/detail column setups (Bind columns setup, Restore columns setup).
- Workplaces offer two options: Bind new workplace (workplace, sections and roles) and Bind sections (sections
  only, for workplaces that already exist on target environments).
- Lookup values and roles are bound with forced update on `Name`, so renamed values reach every environment.
- `AcdDataBindingService` web service builds and saves the data bindings on the server, so the app works on
  both .NET Framework and .NET Creatio.
- Server-side validation of all requests: `CanManageSolution` permission, identifier-only schema and column
  names, binding names of Latin letters, digits and underscores (up to 250 characters), at most 10 bindings
  per request and 10,000 record ids per binding.

### Compatibility
- Minimum Creatio version: 8.3.0. Verified on Creatio 10.0 (.NET Framework, Microsoft SQL Server); install tested on Creatio 8.3.4 (.NET Framework, cloud).
- Ships only `AcdDataBinding.dll` for each runtime; no third-party assemblies. The .NET Framework assembly is
  built against Creatio 8.3.0 and references only Terrasoft.Core, Terrasoft.Common, Terrasoft.Web.Common and
  Common.Logging, which Creatio redirects to the installed versions.
- If the service is unreachable (for example, the app's server code isn't active yet), the browser console logs
  the failing call (method, URL, HTTP status) and the error message lists the possible causes.
