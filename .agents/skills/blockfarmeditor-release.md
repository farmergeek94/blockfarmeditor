# BlockFarmEditor Release Skill

## Description

Check local NuGet packages against NuGet.org and upload newer versions.

## Packages (in dependency order)

| Package | Csproj Path |
|---------|-------------|
| `BlockFarmEditor.Umbraco.Core` | `src/BlockFarmEditor.Umbraco.Core/BlockFarmEditor.Umbraco.Core.csproj` |
| `BlockFarmEditor.ClientScripts.RCL` | `src/BlockFarmEditor.ClientScripts.RCL/BlockFarmEditor.ClientScripts.RCL.csproj` |
| `BlockFarmEditor.Umbraco` | `src/BlockFarmEditor.Umbraco/BlockFarmEditor.Umbraco.csproj` |
| `BlockFarmEditor.USync` | `src/BlockFarmEditor.USync/BlockFarmEditor.USync.csproj` |

## Steps

1. **Build**: `dotnet build BlockFarmEditor.slnx -c Release`
2. **Get local version**: Read `<VersionPrefix>` from each `.csproj`
3. **Get NuGet version**: Query `https://api.nuget.org/v3-flatcontainer/{package-name-lowercase}/index.json` - last item in `versions` array is latest
4. **Compare**: If local > NuGet (or package doesn't exist on NuGet), upload
5. **Upload**: `dotnet nuget push src/{PackageName}.{Version}.nupkg --api-key $env:NUGET_API_KEY --source https://api.nuget.org/v3/index.json --skip-duplicate`

## Notes

- Package output path is `src/` (configured via `<PackageOutputPath>../</PackageOutputPath>`)
- Use `--skip-duplicate` to handle already-published versions gracefully
- Prompt securely with `Read-Host -AsSecureString "NuGet API Key"` and convert with `[Runtime.InteropServices.Marshal]::PtrToStringAuto([Runtime.InteropServices.Marshal]::SecureStringToBSTR($secureKey))`
