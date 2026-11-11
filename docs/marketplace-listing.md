# Marketplace listing: Data Binding kit for Creatio

Listing text as revised on 2026-11-11 after verification report round 1 (case SR-01555495).
Must-fix items addressed: logo, description, screenshots, key features, What's new.

Assets:
- Logo: `assets/marketplace-logo-240.png` (the original data binding tool's knot logo, cropped to 240x240)
- Screenshots and key-feature images: `assets/screenshots/01-...` to `07-...` (1568x737, browser working area only)

## Overview

**Name:** Data Binding kit for Creatio

**Summary** (approved in round 1, unchanged):

```text
Bind lookups, system settings, operations, roles, workplaces and column setups to a package in one click, right from Creatio system pages. By the Creatio Academy team; replaces the older data binding tool that ran only on .NET Framework.
```

**Screenshots:** `01` to `07` from `assets/screenshots/`.

## Details

**Description**

```text
Data Binding kit for Creatio helps Creatio developers and system administrators move configuration between environments faster and with fewer mistakes.

Configuration that lives in system data, such as lookup values, system settings, operation permissions, organizational roles, workplaces and default column setups, has to travel in packages as data bindings. Creating each binding by hand takes several steps and is easy to get wrong: a missing related record or a wrong update setting only shows up after deployment.

With Data Binding kit, users add this data to their package directly from the Creatio system page they are already working on, in one action. The app includes the related records, applies sensible update settings, and produces standard Creatio data bindings that the team can review and adjust as usual.

Built and maintained by the Creatio Academy team.
```

**Key features** (title, image, text)

| # | Title | Image | Text |
|---|---|---|---|
| 1 | Faster binding | 01 | Create data bindings in one action, right from the Lookups, System settings, Operation permissions, Organizational roles and Workplaces pages, instead of setting each one up by hand. |
| 2 | Settings with their values | 03 | Bind a system setting from its page and its values come along automatically, so every environment gets the same configuration. |
| 3 | Permissions travel with operations | 05 | Bind a system operation and the users and roles allowed to run it are included, so access rules match in every environment. |
| 4 | Renamed values stay in sync | 02 | Bind one, several or all values of a lookup. When a value is renamed in development, the new name reaches every environment where the package is installed. |
| 5 | Reuse existing workplaces | 06 | For workplaces that come with Creatio apps, add only your sections to the package. The workplace and its user access stay as each environment has them. |
| 6 | Default columns in packages | 07 | Ship the default column setup of sections and details with your package, and bring a section back to its default columns in one action. |
| 7 | Choose where the data goes | 04 | Pick the target package when several fit. The result is a standard Creatio data binding that you can review and adjust in the data binding designer. |

## Installation and setup

**How to setup**

```text
Setup
1. If the GlbDataBinding package is installed, deactivate or remove it first: both extend the same pages.
2. Install the app. No restart is needed; reload the browser page after the installation.
3. Make sure the users who bind data have the "Can manage solution" (CanManageSolution) system operation. The Bind actions are hidden for other users.
4. Prepare a package to receive the bindings: an editable package whose maintainer matches the Maintainer system setting and that has the bound object's package in its dependencies.

Usage
- Lookups: use Bind lookup to bind the lookup registration, and Bind data or Bind all data in a lookup to bind its values.
- System settings: open a setting and use Bind setting. Its values are bound together with the setting.
- Operation permissions: open an operation and use Bind operation. The roles and users that have access are bound too.
- Organizational roles: select a role and use Bind role.
- Workplaces: use Bind workplace > Bind new workplace for a workplace you created, or Bind workplace > Bind sections to bind only the sections of a workplace that already exists in target environments.
- Column setup: in a section or detail view menu, use Bind columns setup to bind the default ("for all users") columns, or Restore columns setup to return to them.
- If the object can belong to several packages, choose the target package in the window that opens. After saving, select View details to open the binding in the data binding designer.

Works on the Creatio system pages that still use Classic UI (Lookups, System settings, Operation permissions, Organizational roles, Workplaces, and section and detail column setups). These are administrator pages, not end-user pages. The app doesn't change Freedom UI pages.
```

## Release details

**What's new**

```text
First release. Teams can now add lookup values, system settings, operation permissions, organizational roles, workplaces and default column setups to their packages in one action, right from the page they are working on. Related records are included automatically, so moving configuration from development to test and production takes less time and fewer manual steps, with fewer surprises after deployment.
```
