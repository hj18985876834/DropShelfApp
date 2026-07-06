# Document remote default branch governance

## Goal

Capture the repository governance rule learned from the release session: the GitHub repository default branch must be `main`, and local `origin/HEAD` should be synchronized after changing the remote default branch.

## What I Already Know

* The GitHub repository default branch was changed from `feature/feature-specs` to `main`.
* `gh repo edit hj18985876834/DropShelfApp --default-branch main` successfully changed the remote default branch.
* `git remote set-head origin -a` synchronized local `origin/HEAD` to `origin/main`.
* Current unrelated dirty work exists for long text/link card behavior and must not be included in this documentation commit.

## Requirements

* Document that maintained source and release work uses `main` as the GitHub default branch.
* Document the exact verification commands for remote default branch and local `origin/HEAD`.
* Keep changes scoped to governance/release docs and specs.

## Acceptance Criteria

* `.trellis/spec/` includes a concrete rule for default branch governance.
* Release docs mention verifying the GitHub default branch is `main`.
* Existing unrelated dirty files are not staged or committed.

## Out Of Scope

* Modifying app code.
* Changing feature branches.
* Cleaning or committing existing long text/link card work.
