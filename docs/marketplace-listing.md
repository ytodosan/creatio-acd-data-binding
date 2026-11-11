# Marketplace listing draft: Data Binding kit for Creatio

Text entered in the listing form (work.creatio.com, MarketplaceListingVersions_InternalFormPage) on 2026-10-07.
Nothing was saved or submitted. Copy each block into the matching field.

## Overview

**Name**

```text
Data Binding kit for Creatio
```

**Summary** (237 of 250 characters)

```text
Bind lookups, system settings, operations, roles, workplaces and column setups to a package in one click, right from Creatio system pages. By the Creatio Academy team; replaces the older data binding tool that ran only on .NET Framework.
```

## Details

**Description**

```text
Data Binding kit for Creatio is built and maintained by the Creatio Academy team. It replaces the older data binding tool (GlbDataBinding), which many Creatio developers rely on but which runs only on .NET Framework. The kit keeps the familiar workflow and is designed so the same package runs on both .NET Framework and .NET Creatio.

Who it's for: Creatio developers and system administrators who move configuration between environments with packages.

The problem it solves: to ship a lookup value, a system setting with its values, or a workplace with its sections, you normally open Configuration, create a data binding by hand, pick the object and the records, then choose the columns and mark the key and forced-update columns. Data Binding kit does this in one click from the system page you're already on (Lookups, System settings, Operation permissions, Organizational roles, Workplaces), with sensible defaults:
- System settings are bound together with their values, operations with their grantees, and workplaces with their sections and roles.
- Section and detail column setups ("for all users") can be bound, and restored to the default.
- Every result is a regular Creatio data binding: open it in the data binding designer from the result message and adjust it as usual.

What's new compared with the older tool:
- Runs on .NET Framework and .NET: the data binding logic moved from the browser into the app's own web service.
- Lookup values are bound with forced update on Name, so a renamed value reaches every environment where the package is installed.
- Bind sections: for workplaces that already exist on target environments (for example, ones installed with Creatio apps), bind only the sections instead of the whole workplace and its roles.
- Access uses the standard CanManageSolution system operation, so there's no custom permission to set up.
- Clearer diagnostics: if the app's server part isn't reachable, the browser console shows the failing call and the message lists the likely causes.

Bindings are saved to an editable package of the current maintainer that has the bound object in its hierarchy.
```

**Key features** (not entered: separate records in the form; 1 to 7 allowed)

```text
1. One-click binding from Lookups, System settings, Operation permissions, Organizational roles and Workplaces.
2. Related data included automatically: setting values, operation grantees, workplace sections and roles.
3. Renamed lookup values reach every environment (forced update on Name).
4. Bind only the sections of workplaces installed by other apps.
5. Bind and restore "for all users" column setups of sections and details.
6. Regular Creatio data bindings, editable in the data binding designer.
```

## Installation and setup

**How to setup**

```text
1. If the older GlbDataBinding package is installed, deactivate or remove it first: both extend the same pages.
2. Install the app.
3. Restart the Creatio application, then reload the browser page (Ctrl+Shift+R). The app's server part becomes available only after the restart. If an action still reports "service was not found (HTTP 404)", compile the AcdDataBinding package (Configuration > Compile) and reload the page.
4. Make sure the users who bind data have the CanManageSolution system operation.
5. Prepare a package to receive the bindings: an editable package whose maintainer matches the Maintainer system setting and that has the bound object's package in its hierarchy.
6. Open Lookups, System settings, Operation permissions, Organizational roles or Workplaces and use the Bind actions. After saving, select View details to open the binding in the data binding designer.
```

## Compatibility

**Compatibility notes**

```text
Works on the Creatio system pages that still use Classic UI (Lookups, System settings, Operation permissions, Organizational roles, Workplaces, and section and detail column setups). These are administrator pages, not end-user pages. The app doesn't change Freedom UI pages.
Verified on Creatio 10.0 and 8.3.4 (.NET Framework, Microsoft SQL Server). Not yet verified on PostgreSQL or .NET.
Columns added by packages outside the target package's hierarchy aren't bound; they're listed as warnings, because Creatio's automatic package dependency handling isn't available to apps. When you bind sections of a workplace installed by another app, add that app's package as a dependency of your package.
```

## Release details

**What's new**

```text
First release, replacing the older .NET Framework-only data binding tool (GlbDataBinding). Runs on .NET Framework and .NET. Bind actions for lookups, lookup values, system settings, system operations, organizational roles, workplaces (whole workplace or only its sections) and section and detail column setups. Lookup values are bound with forced update on Name.
```

## Not entered (your decisions)

- **Application file:** release 1.0.0 from `main` (`release/Data-Binding-kit-for-Creatio-1.0.0.zip`) is the tested build. 1.1.0 (branch `general-package`) is untested.
- **Compatibility selections:** suggested Creatio version from 8.3.0; .NET Framework; English; Classic UI. The form states that cloud deployment requires MSSQL and PostgreSQL support, and PostgreSQL is untested.
- **Pricing and support:** payment model Free; support-compliance checkbox and terms are your commitments.
- **Type, categories, industries:** suggested no industries (the tool isn't industry-specific).
- **Logo** (240x240 PNG/JPG) and **2 to 10 screenshots** (at least 1200x600): not prepared yet.
- **Wording to check:** the Description and What's new say the kit runs on .NET. It's built for .NET, but only .NET Framework has been verified so far.
