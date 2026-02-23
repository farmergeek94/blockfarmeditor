using BlockFarmEditor.Umbraco.Core.DTO;

namespace BlockFarmEditor.Umbraco.Core.Interfaces
{
    /// <summary>
    /// Service for comprehensive export/import of BlockFarmEditor definitions
    /// along with linked element types, data types, and partial views.
    /// </summary>
    public interface IBlockFarmEditorExportService
    {
        /// <summary>
        /// Builds a complete export package containing definitions, element types, data types, and partial views.
        /// </summary>
        /// <param name="definitionKeys">The keys of the definitions to export. If empty, exports all.</param>
        /// <returns>The export package DTO.</returns>
        Task<BlockFarmEditorExportPackageDTO> BuildExportPackageAsync(IEnumerable<Guid> definitionKeys);

        /// <summary>
        /// Exports the package to folder structure on the server.
        /// Creates: BlockFarmEditor/Definitions/, BlockFarmEditor/ElementTypes/, 
        /// BlockFarmEditor/DataTypes/, BlockFarmEditor/PartialViews/
        /// </summary>
        /// <param name="package">The export package.</param>
        Task ExportToFolderAsync(BlockFarmEditorExportPackageDTO package);

        /// <summary>
        /// Exports the package to a ZIP file and returns the bytes.
        /// </summary>
        /// <param name="package">The export package.</param>
        /// <returns>ZIP file bytes.</returns>
        Task<byte[]> ExportToZipAsync(BlockFarmEditorExportPackageDTO package);

        /// <summary>
        /// Reads a package from the server folder structure.
        /// </summary>
        /// <returns>The imported package DTO.</returns>
        Task<BlockFarmEditorExportPackageDTO> ReadFromFolderAsync();

        /// <summary>
        /// Reads a package from a ZIP file stream.
        /// </summary>
        /// <param name="zipStream">The ZIP file stream.</param>
        /// <returns>The imported package DTO.</returns>
        Task<BlockFarmEditorExportPackageDTO> ReadFromZipAsync(Stream zipStream);

        /// <summary>
        /// Imports the package into Umbraco, creating or updating definitions, element types, 
        /// data types, and partial views.
        /// </summary>
        /// <param name="package">The package to import.</param>
        /// <param name="overwriteElementTypes">If true, overwrites existing element types (including compositions). Default true.</param>
        /// <param name="overwriteBlockDefinitions">If true, overwrites existing block definitions. Default true.</param>
        /// <param name="overwritePartialViews">If true, overwrites existing partial views. Default true.</param>
        /// <param name="overwriteDataTypes">If true, overwrites existing data types. Default false.</param>
        /// <returns>Import result with counts of created, updated, skipped, and failed items.</returns>
        Task<ImportResultDTO> ImportPackageAsync(
            BlockFarmEditorExportPackageDTO package, 
            bool overwriteElementTypes = true, 
            bool overwriteBlockDefinitions = true, 
            bool overwritePartialViews = true, 
            bool overwriteDataTypes = false);
    }
}
