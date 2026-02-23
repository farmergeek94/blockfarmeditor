using Umbraco.Cms.Core.Models;

namespace BlockFarmEditor.Umbraco.Core.Interfaces;

/// <summary>
/// Service for managing folder structures for content types and data types.
/// Provides helpers for building folder paths and ensuring folder structures exist during import.
/// </summary>
public interface IFolderStructureService
{
    /// <summary>
    /// Builds a folder path string for a content type by walking up its container hierarchy.
    /// </summary>
    /// <param name="contentType">The content type to get the folder path for.</param>
    /// <returns>A path string like "Folder1/Folder2/Folder3" or null if at root level.</returns>
    Task<string?> BuildContentTypeFolderPathAsync(IContentType contentType);

    /// <summary>
    /// Builds a folder path string for a data type by walking up its container hierarchy.
    /// </summary>
    /// <param name="dataType">The data type to get the folder path for.</param>
    /// <returns>A path string like "Folder1/Folder2/Folder3" or null if at root level.</returns>
    Task<string?> BuildDataTypeFolderPathAsync(IDataType dataType);

    /// <summary>
    /// Ensures the folder structure exists for a content type and returns the parent container ID.
    /// Creates missing folders if needed.
    /// </summary>
    /// <param name="folderPath">The folder path string like "Folder1/Folder2/Folder3".</param>
    /// <returns>The ID of the deepest folder container, or -1 for root level.</returns>
    Task<int> EnsureContentTypeFolderStructureAsync(string? folderPath);

    /// <summary>
    /// Ensures the folder structure exists for a data type and returns the parent container ID.
    /// Creates missing folders if needed.
    /// </summary>
    /// <param name="folderPath">The folder path string like "Folder1/Folder2/Folder3".</param>
    /// <returns>The ID of the deepest folder container, or -1 for root level.</returns>
    Task<int> EnsureDataTypeFolderStructureAsync(string? folderPath);
}
