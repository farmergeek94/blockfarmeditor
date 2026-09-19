---
name: blockfarmeditor-versioning
description: Bump BlockFarmEditor package versions and release notes - prompts for projects, version, and release notes, then updates VersionPrefix and PackageReleaseNotes in the .csproj files plus CHANGELOG.md and the readme.md history. Use when the user asks to bump, update, or increment the version, or to prepare release notes for BlockFarmEditor.
---

# BlockFarmEditor Versioning Skill

## Description

Automates version bumping and release note updates for BlockFarmEditor projects. Prompts the user for project selection, version, release notes, and readme history, then applies changes to .csproj and readme.md files.

## Workflow Steps

1. **Prompt user to select projects:**  
   - BlockFarmEditor.ClientScripts.RCL  
   - BlockFarmEditor.Umbraco  
   - BlockFarmEditor.Umbraco.Core  
   - BlockFarmEditor.USync

2. **Ask for new version number:**  
   - Prompt: “Enter the new version number (or type ‘auto’ to auto-increment).”
   - Use a consistent release version based on the BlockFarmEditor.Umbraco project's current `<VersionPrefix>`.

3. **Ask for release notes:**  
   - Prompt: “Enter the PackageReleaseNotes comment for this release.”

4. **Update .csproj files:**  
   - Automatically select the BlockFarmEditor.Umbraco project always
   - For each selected project:  
     - Update `<VersionPrefix>` to the new version.  
     - Update `<PackageReleaseNotes>` with the new comment.

5. **Update CHANGELOG.md:**
   - Add the new version history along with the new comment.

6. **Update readme.md:**  
   - Add the new version history entry at the top of the version history section along with the new comment.
   - Only keep last 5 version changes.

## Usage

Invoke with `/blockfarmeditor-versioning`, or ask Claude to bump the version. To publish the resulting packages to NuGet, follow up with `/blockfarmeditor-release`.
