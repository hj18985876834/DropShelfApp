# Quality Guidelines

> Code quality standards for frontend development.

---

## Overview

DropShelf is a WPF desktop utility. Quality checks must cover both C# correctness and Windows desktop behavior.

---

## Forbidden Patterns

* Do not use broad `git add .` for task commits.
* Do not commit unrelated dirty files from another feature/session.
* Do not move or delete original user files from shelf clear/remove flows.
* Do not add cloud sync, accounts, telemetry, or external APIs in V1.
* Do not put persistence, registry, tray, or file-system side effects directly in Views.
* Do not leave Windows desktop behavior untested until the end of a feature.

---

## Required Patterns

* Run Windows-side `dotnet build` and `dotnet test` after each runnable feature slice.
* Run `dotnet format --verify-no-changes`; treat analyzer warnings as quality
  failures because this command returns non-zero for project analyzers.
* Keep file/folder shelf records as original path references.
* Store pathless/pasted images under app-owned local data.
* Use services for side-effect boundaries.
* Keep WPF UI styling quiet and Windows-native / Fluent-inspired.
* Validate actual desktop behavior incrementally on Windows, especially drag/drop, tray, window docking, startup, and installer behavior.

---

## Testing Requirements

Use MSTest for V1.

Unit-test:

* settings defaults and persistence
* shelf metadata persistence
* image store path/cleanup behavior
* ViewModel commands and selection behavior

For MSTest 4, use analyzer-preferred assertions so `dotnet format
--verify-no-changes` stays green:

* Use `Assert.IsEmpty(collection)` instead of `Assert.AreEqual(0, collection.Count)`.
* Use `Assert.HasCount(expected, collection)` instead of count equality asserts.
* Use `[TestMethod]` with `[DataRow]` for data-driven tests; do not use obsolete
  `[DataTestMethod]`.

Manual Windows validation is required for:

* app launch
* tray icon
* edge handle
* drag in
* drag out
* theme/density switching
* startup setting
* installer install/uninstall

---

## Code Review Checklist

* Does the change stay scoped to the current feature branch/task?
* Are unrelated dirty files excluded?
* Are service boundaries preserved?
* Are original files protected from accidental delete/move behavior?
* Do tests cover changed non-UI logic?
* Was the relevant Windows manual check run for UI/desktop behavior?
* Does the UI remain compact, quiet, and predictable?
