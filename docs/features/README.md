# DropShelf Feature Specifications

These documents are the development contracts for feature branches. Each branch should read:

1. The relevant feature spec in this directory.
2. `.trellis/tasks/07-04-windows-daily-light-app-ideas/prd.md`
3. `.trellis/tasks/07-04-windows-daily-light-app-ideas/info.md`
4. `.trellis/spec/frontend/directory-structure.md`
5. `.trellis/spec/frontend/state-management.md`
6. `.trellis/spec/frontend/quality-guidelines.md`

## Feature Order

1. [App Shell](01-app-shell.md)
2. [Shelf Persistence](02-shelf-persistence.md)
3. [File and Folder Flow](03-file-folder-flow.md)
4. [Drag-Out Copy](04-drag-out-copy.md)
5. [Text, URL, and Image Input](05-text-url-image-input.md)
6. [UX Polish](06-ux-polish.md)
7. [Settings](07-settings.md)
8. [Installer](08-installer.md)

## Branching

Use one focused branch per feature:

```text
feature/app-shell
feature/shelf-persistence
feature/file-folder-flow
feature/drag-out-copy
feature/text-url-image-input
feature/ux-polish
feature/settings
feature/installer
```

Do not use `git add .`. Stage only the files owned by the branch.

## Definition Of Ready

A feature branch is ready to start when:

* Its dependencies are merged or the branch is based on a compatible commit.
* The feature spec is understood.
* Existing dirty files are reviewed and unrelated files are left untouched.

## Definition Of Done

A feature branch is done when:

* The acceptance criteria in its spec pass.
* Required unit tests pass.
* Relevant manual Windows checklist items pass.
* `dotnet build DropShelf.sln` passes on Windows.
* `dotnet test DropShelf.sln` passes on Windows.
* `dotnet format DropShelf.sln --verify-no-changes --verbosity minimal` passes.
* The commit only includes files intentionally changed by that branch.
