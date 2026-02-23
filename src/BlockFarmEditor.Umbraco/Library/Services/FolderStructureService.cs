using BlockFarmEditor.Umbraco.Core.Interfaces;
using Microsoft.Extensions.Logging;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Services;

namespace BlockFarmEditor.Umbraco.Library.Services;

/// <summary>
/// Service for managing folder structures for content types and data types.
/// Provides helpers for building folder paths and ensuring folder structures exist during import.
/// </summary>
internal class FolderStructureService(
    IContentTypeContainerService contentTypeContainerService,
    IDataTypeContainerService dataTypeContainerService,
    IUserService userService,
    ILogger<FolderStructureService> logger) : IFolderStructureService
{
    /// <inheritdoc />
    public async Task<string?> BuildContentTypeFolderPathAsync(IContentType contentType)
    {
        var pathParts = new List<string>();
        
        var currentContainer = await contentTypeContainerService.GetParentAsync(contentType);
        while (currentContainer != null)
        {
            pathParts.Insert(0, currentContainer.Name ?? "Unnamed");
            currentContainer = await contentTypeContainerService.GetParentAsync(currentContainer);
        }

        return pathParts.Count > 0 ? string.Join("/", pathParts) : null;
    }

    /// <inheritdoc />
    public async Task<string?> BuildDataTypeFolderPathAsync(IDataType dataType)
    {
        var pathParts = new List<string>();
        
        var currentContainer = await dataTypeContainerService.GetParentAsync(dataType);
        while (currentContainer != null)
        {
            pathParts.Insert(0, currentContainer.Name ?? "Unnamed");
            currentContainer = await dataTypeContainerService.GetParentAsync(currentContainer);
        }

        return pathParts.Count > 0 ? string.Join("/", pathParts) : null;
    }

    /// <inheritdoc />
    public async Task<int> EnsureContentTypeFolderStructureAsync(string? folderPath)
    {
        if (string.IsNullOrWhiteSpace(folderPath))
        {
            return -1; // Root level
        }

        var pathParts = folderPath.Split('/', StringSplitOptions.RemoveEmptyEntries);
        if (pathParts.Length == 0)
        {
            return -1;
        }

        var userKey = await GetCurrentUserKeyAsync();
        Guid? currentParentKey = null;
        int currentParentId = -1;

        for (int i = 0; i < pathParts.Length; i++)
        {
            var folderName = pathParts[i];
            var level = i + 1; // Level is 1-indexed
            
            // Get containers at this level with this name
            var containersAtLevel = await contentTypeContainerService.GetAsync(folderName, level);
            var existingContainer = containersAtLevel.FirstOrDefault();

            if (existingContainer != null)
            {
                currentParentKey = existingContainer.Key;
                currentParentId = existingContainer.Id;
                logger.LogDebug("Found existing content type folder: {FolderName} at level {Level}", folderName, level);
            }
            else
            {
                // Create the container
                var result = await contentTypeContainerService.CreateAsync(null, folderName, currentParentKey, userKey);
                if (result.Success && result.Result != null)
                {
                    currentParentKey = result.Result.Key;
                    currentParentId = result.Result.Id;
                    logger.LogInformation("Created content type folder: {FolderName} at level {Level}", folderName, level);
                }
                else
                {
                    logger.LogWarning("Failed to create content type folder: {FolderName}. Status: {Status}",
                        folderName, result.Status);
                    return -1;
                }
            }
        }

        return currentParentId;
    }

    /// <inheritdoc />
    public async Task<int> EnsureDataTypeFolderStructureAsync(string? folderPath)
    {
        if (string.IsNullOrWhiteSpace(folderPath))
        {
            return -1; // Root level
        }

        var pathParts = folderPath.Split('/', StringSplitOptions.RemoveEmptyEntries);
        if (pathParts.Length == 0)
        {
            return -1;
        }

        var userKey = await GetCurrentUserKeyAsync();
        Guid? currentParentKey = null;
        int currentParentId = -1;

        for (int i = 0; i < pathParts.Length; i++)
        {
            var folderName = pathParts[i];
            var level = i + 1; // Level is 1-indexed
            
            // Get containers at this level with this name
            var containersAtLevel = await dataTypeContainerService.GetAsync(folderName, level);
            var existingContainer = containersAtLevel.FirstOrDefault();

            if (existingContainer != null)
            {
                currentParentKey = existingContainer.Key;
                currentParentId = existingContainer.Id;
                logger.LogDebug("Found existing data type folder: {FolderName} at level {Level}", folderName, level);
            }
            else
            {
                // Create the container
                var result = await dataTypeContainerService.CreateAsync(null, folderName, currentParentKey, userKey);
                if (result.Success && result.Result != null)
                {
                    currentParentKey = result.Result.Key;
                    currentParentId = result.Result.Id;
                    logger.LogInformation("Created data type folder: {FolderName} at level {Level}", folderName, level);
                }
                else
                {
                    logger.LogWarning("Failed to create data type folder: {FolderName}. Status: {Status}",
                        folderName, result.Status);
                    return -1;
                }
            }
        }

        return currentParentId;
    }

    private async Task<Guid> GetCurrentUserKeyAsync()
    {
        await Task.CompletedTask;
        var user = userService.GetUserById(-1); // Admin user as fallback
        return user?.Key ?? Guid.Empty;
    }
}
