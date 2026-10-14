# Changelog

All notable changes to **Data Binding kit for Creatio** (package `AcdDataBinding`, maintainer Creatio Academy).
Versions follow the X.Y.Z format required by Creatio Marketplace.

## 1.0.1 - 2026-10-14

### Fixed
- The app works right after installation, with no restart or package compile. Creatio reloads the application after
  installing an Assembly package only if the package contains a compilable schema; the C# was all in Files/src, so
  the web service stayed unloaded (HTTP 404) until a restart. The web service entry point is now the source-code
  schema AcdDataBindingService (still compiled into AcdDataBinding.dll; the package stays an Assembly package).
- build-release.ps1 refuses to build an Assembly package without a compilable schema.

### Removed
- The special "service was not found" error text. A failed call shows the usual request error, and the browser console
  logs the method, URL and HTTP status.

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
