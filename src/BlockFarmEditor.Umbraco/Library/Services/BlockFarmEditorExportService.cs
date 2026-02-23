using BlockFarmEditor.Umbraco.Core.DTO;
using BlockFarmEditor.Umbraco.Core.Interfaces;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging;
using System.IO.Compression;
using System.Text.Json;
using System.Xml.Serialization;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.PropertyEditors;
using Umbraco.Cms.Core.Serialization;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Core.Strings;
using Umbraco.Cms.Infrastructure.Persistence;
using IOFile = System.IO.File;

namespace BlockFarmEditor.Umbraco.Library.Services
{
    /// <summary>
    /// Implementation of comprehensive export/import service for BlockFarmEditor.
    /// Exports definitions, element types, data types, and partial views.
    /// </summary>
    internal class BlockFarmEditorExportService(
        IBlockFarmEditorDefinitionService definitionService,
        IContentTypeService contentTypeService,
        IDataTypeService dataTypeService,
        IFolderStructureService folderStructureService,
        IUmbracoDatabaseFactory umbracoDatabaseFactory,
        IWebHostEnvironment webHostEnvironment,
        IUserService userService,
        IShortStringHelper shortStringHelper,
        PropertyEditorCollection propertyEditors,
        IConfigurationEditorJsonSerializer configurationEditorJsonSerializer,
        ILogger<BlockFarmEditorExportService> logger) : IBlockFarmEditorExportService
    {
        private const string BasePath = "BlockFarmEditor";
        private const string DefinitionsFolder = "Definitions";
        private const string ElementTypesFolder = "ElementTypes";
        private const string DataTypesFolder = "DataTypes";
        private const string PartialViewsFolder = "PartialViews";

        public async Task<BlockFarmEditorExportPackageDTO> BuildExportPackageAsync(IEnumerable<Guid> definitionKeys)
        {
            var package = new BlockFarmEditorExportPackageDTO
            {
                ExportedAt = DateTime.UtcNow
            };

            using var db = umbracoDatabaseFactory.CreateDatabase();

            // Get definitions
            var allDefinitions = await definitionService.GetAllAsync(db);
            var definitionsList = new List<BlockFarmEditorDefinitionDTO>();
            var keySet = definitionKeys.ToHashSet();

            await foreach (var def in allDefinitions ?? AsyncEnumerable.Empty<BlockFarmEditorDefinitionDTO>())
            {
                if (def != null && (keySet.Count == 0 || keySet.Contains(def.Key)))
                {
                    definitionsList.Add(def);
                }
            }

            package.Definitions = definitionsList;

            // Collect element types and data types
            var elementTypeAliases = definitionsList.Select(d => d.ContentTypeAlias).Distinct().ToHashSet();
            var allElementTypes = contentTypeService.GetAllElementTypes();
            var relevantElementTypes = allElementTypes.Where(et => elementTypeAliases.Contains(et.Alias)).ToList();

            // Track data type keys to export
            var dataTypeKeys = new HashSet<Guid>();
            
            // Track all element types to export (including compositions)
            var exportedAliases = new HashSet<string>();
            var elementTypesToExport = new List<IContentType>();

            // Collect all element types and their compositions recursively
            foreach (var elementType in relevantElementTypes)
            {
                CollectElementTypeAndCompositions(elementType, allElementTypes, exportedAliases, elementTypesToExport);
            }

            // Export all element types (sorted by dependency - compositions first)
            var sortedElementTypes = SortByDependency(elementTypesToExport);
            foreach (var elementType in sortedElementTypes)
            {
                var exportDto = await MapContentTypeToExportDTOAsync(elementType);
                package.ElementTypes.Add(exportDto);

                // Collect data type keys from property types
                foreach (var propertyType in elementType.CompositionPropertyTypes)
                {
                    dataTypeKeys.Add(propertyType.DataTypeKey);
                }
            }

            // Export data types
            foreach (var dataTypeKey in dataTypeKeys)
            {
                var dataType = await dataTypeService.GetAsync(dataTypeKey);
                if (dataType != null)
                {
                    var exportDto = await MapDataTypeToExportDTOAsync(dataType);
                    package.DataTypes.Add(exportDto);
                }
            }

            // Export partial views
            var viewsPath = Path.Combine(webHostEnvironment.ContentRootPath, "Views");
            foreach (var definition in definitionsList)
            {
                if (definition.Type == "partial" && !string.IsNullOrEmpty(definition.ViewPath))
                {
                    var viewPath = definition.ViewPath;
                    // Handle paths like ~/Views/Partials/... or just Partials/...
                    if (viewPath.StartsWith("~/Views/"))
                    {
                        viewPath = viewPath.Substring(8); // Remove ~/Views/
                    }
                    else if (viewPath.StartsWith("~/"))
                    {
                        viewPath = viewPath.Substring(2);
                    }

                    var fullPath = Path.Combine(viewsPath, viewPath);
                    if (IOFile.Exists(fullPath))
                    {
                        try
                        {
                            var content = await IOFile.ReadAllTextAsync(fullPath);
                            package.PartialViews.Add(new PartialViewExportDTO
                            {
                                Path = viewPath,
                                Content = content
                            });
                        }
                        catch (Exception ex)
                        {
                            logger.LogWarning(ex, "Failed to read partial view: {Path}", fullPath);
                        }
                    }
                }
            }

            return package;
        }

        public async Task ExportToFolderAsync(BlockFarmEditorExportPackageDTO package)
        {
            // Create folder structure
            var definitionsPath = Path.Combine(BasePath, DefinitionsFolder);
            var elementTypesPath = Path.Combine(BasePath, ElementTypesFolder);
            var dataTypesPath = Path.Combine(BasePath, DataTypesFolder);
            var partialViewsPath = Path.Combine(BasePath, PartialViewsFolder);

            Directory.CreateDirectory(definitionsPath);
            Directory.CreateDirectory(elementTypesPath);
            Directory.CreateDirectory(dataTypesPath);
            Directory.CreateDirectory(partialViewsPath);

            // Export definitions
            var definitionSerializer = new XmlSerializer(typeof(BlockFarmEditorDefinitionDTO));
            foreach (var definition in package.Definitions)
            {
                var filePath = Path.Combine(definitionsPath, $"{definition.ContentTypeAlias}.xml");
                await using var fs = IOFile.Create(filePath);
                definitionSerializer.Serialize(fs, definition);
            }

            // Export element types
            var elementTypeSerializer = new XmlSerializer(typeof(ContentTypeExportDTO));
            foreach (var elementType in package.ElementTypes)
            {
                var filePath = Path.Combine(elementTypesPath, $"{elementType.Alias}.xml");
                await using var fs = IOFile.Create(filePath);
                elementTypeSerializer.Serialize(fs, elementType);
            }

            // Export data types
            var dataTypeSerializer = new XmlSerializer(typeof(DataTypeExportDTO));
            foreach (var dataType in package.DataTypes)
            {
                var filePath = Path.Combine(dataTypesPath, $"{dataType.Key}.xml");
                await using var fs = IOFile.Create(filePath);
                dataTypeSerializer.Serialize(fs, dataType);
            }

            // Export partial views
            var partialViewSerializer = new XmlSerializer(typeof(PartialViewExportDTO));
            foreach (var partialView in package.PartialViews)
            {
                var safeName = partialView.Path.Replace("/", "_").Replace("\\", "_");
                var filePath = Path.Combine(partialViewsPath, $"{safeName}.xml");
                await using var fs = IOFile.Create(filePath);
                partialViewSerializer.Serialize(fs, partialView);
            }

            await Task.CompletedTask;
        }

        public async Task<byte[]> ExportToZipAsync(BlockFarmEditorExportPackageDTO package)
        {
            using var memoryStream = new MemoryStream();
            using (var archive = new ZipArchive(memoryStream, ZipArchiveMode.Create, true))
            {
                // Add package manifest
                var packageSerializer = new XmlSerializer(typeof(BlockFarmEditorExportPackageDTO));
                var manifestEntry = archive.CreateEntry("package.xml");
                await using (var entryStream = manifestEntry.Open())
                {
                    packageSerializer.Serialize(entryStream, package);
                }

                // Add individual files for clarity
                var definitionSerializer = new XmlSerializer(typeof(BlockFarmEditorDefinitionDTO));
                foreach (var definition in package.Definitions)
                {
                    var entry = archive.CreateEntry($"{DefinitionsFolder}/{definition.ContentTypeAlias}.xml");
                    await using var entryStream = entry.Open();
                    definitionSerializer.Serialize(entryStream, definition);
                }

                var elementTypeSerializer = new XmlSerializer(typeof(ContentTypeExportDTO));
                foreach (var elementType in package.ElementTypes)
                {
                    var entry = archive.CreateEntry($"{ElementTypesFolder}/{elementType.Alias}.xml");
                    await using var entryStream = entry.Open();
                    elementTypeSerializer.Serialize(entryStream, elementType);
                }

                var dataTypeSerializer = new XmlSerializer(typeof(DataTypeExportDTO));
                foreach (var dataType in package.DataTypes)
                {
                    var entry = archive.CreateEntry($"{DataTypesFolder}/{dataType.Key}.xml");
                    await using var entryStream = entry.Open();
                    dataTypeSerializer.Serialize(entryStream, dataType);
                }

                var partialViewSerializer = new XmlSerializer(typeof(PartialViewExportDTO));
                foreach (var partialView in package.PartialViews)
                {
                    var safeName = partialView.Path.Replace("/", "_").Replace("\\", "_");
                    var entry = archive.CreateEntry($"{PartialViewsFolder}/{safeName}.xml");
                    await using var entryStream = entry.Open();
                    partialViewSerializer.Serialize(entryStream, partialView);
                }
            }

            return memoryStream.ToArray();
        }

        public async Task<BlockFarmEditorExportPackageDTO> ReadFromFolderAsync()
        {
            var package = new BlockFarmEditorExportPackageDTO();

            var definitionsPath = Path.Combine(BasePath, DefinitionsFolder);
            var elementTypesPath = Path.Combine(BasePath, ElementTypesFolder);
            var dataTypesPath = Path.Combine(BasePath, DataTypesFolder);
            var partialViewsPath = Path.Combine(BasePath, PartialViewsFolder);

            // Read definitions
            if (Directory.Exists(definitionsPath))
            {
                var definitionSerializer = new XmlSerializer(typeof(BlockFarmEditorDefinitionDTO));
                foreach (var file in Directory.EnumerateFiles(definitionsPath, "*.xml"))
                {
                    try
                    {
                        await using var fs = IOFile.OpenRead(file);
                        if (definitionSerializer.Deserialize(fs) is BlockFarmEditorDefinitionDTO dto)
                        {
                            package.Definitions.Add(dto);
                        }
                    }
                    catch (Exception ex)
                    {
                        logger.LogWarning(ex, "Failed to read definition from {File}", file);
                    }
                }
            }

            // Read element types
            var elementTypeSerializer = new XmlSerializer(typeof(ContentTypeExportDTO));
            if (Directory.Exists(elementTypesPath))
            {
                foreach (var file in Directory.EnumerateFiles(elementTypesPath, "*.xml"))
                {
                    try
                    {
                        await using var fs = IOFile.OpenRead(file);
                        if (elementTypeSerializer.Deserialize(fs) is ContentTypeExportDTO dto)
                        {
                            package.ElementTypes.Add(dto);
                        }
                    }
                    catch (Exception ex)
                    {
                        logger.LogWarning(ex, "Failed to read element type from {File}", file);
                    }
                }
            }

            // Read data types
            if (Directory.Exists(dataTypesPath))
            {
                var dataTypeSerializer = new XmlSerializer(typeof(DataTypeExportDTO));
                foreach (var file in Directory.EnumerateFiles(dataTypesPath, "*.xml"))
                {
                    try
                    {
                        await using var fs = IOFile.OpenRead(file);
                        if (dataTypeSerializer.Deserialize(fs) is DataTypeExportDTO dto)
                        {
                            package.DataTypes.Add(dto);
                        }
                    }
                    catch (Exception ex)
                    {
                        logger.LogWarning(ex, "Failed to read data type from {File}", file);
                    }
                }
            }

            // Read partial views
            if (Directory.Exists(partialViewsPath))
            {
                var partialViewSerializer = new XmlSerializer(typeof(PartialViewExportDTO));
                foreach (var file in Directory.EnumerateFiles(partialViewsPath, "*.xml"))
                {
                    try
                    {
                        await using var fs = IOFile.OpenRead(file);
                        if (partialViewSerializer.Deserialize(fs) is PartialViewExportDTO dto)
                        {
                            package.PartialViews.Add(dto);
                        }
                    }
                    catch (Exception ex)
                    {
                        logger.LogWarning(ex, "Failed to read partial view from {File}", file);
                    }
                }
            }

            return package;
        }

        public async Task<BlockFarmEditorExportPackageDTO> ReadFromZipAsync(Stream zipStream)
        {
            using var archive = new ZipArchive(zipStream, ZipArchiveMode.Read);

            // Try to read the combined package.xml first
            var packageEntry = archive.GetEntry("package.xml");
            if (packageEntry != null)
            {
                var packageSerializer = new XmlSerializer(typeof(BlockFarmEditorExportPackageDTO));
                await using var entryStream = packageEntry.Open();
                if (packageSerializer.Deserialize(entryStream) is BlockFarmEditorExportPackageDTO package)
                {
                    return package;
                }
            }

            // Fall back to reading individual files
            var result = new BlockFarmEditorExportPackageDTO();

            var definitionSerializer = new XmlSerializer(typeof(BlockFarmEditorDefinitionDTO));
            var elementTypeSerializer = new XmlSerializer(typeof(ContentTypeExportDTO));
            var dataTypeSerializer = new XmlSerializer(typeof(DataTypeExportDTO));
            var partialViewSerializer = new XmlSerializer(typeof(PartialViewExportDTO));

            foreach (var entry in archive.Entries)
            {
                if (entry.FullName.EndsWith(".xml", StringComparison.OrdinalIgnoreCase))
                {
                    try
                    {
                        await using var entryStream = entry.Open();
                        using var ms = new MemoryStream();
                        await entryStream.CopyToAsync(ms);
                        ms.Position = 0;

                        if (entry.FullName.StartsWith($"{DefinitionsFolder}/"))
                        {
                            if (definitionSerializer.Deserialize(ms) is BlockFarmEditorDefinitionDTO dto)
                                result.Definitions.Add(dto);
                        }
                        else if (entry.FullName.StartsWith($"{ElementTypesFolder}/"))
                        {
                            if (elementTypeSerializer.Deserialize(ms) is ContentTypeExportDTO dto)
                                result.ElementTypes.Add(dto);
                        }
                        else if (entry.FullName.StartsWith($"{DataTypesFolder}/"))
                        {
                            if (dataTypeSerializer.Deserialize(ms) is DataTypeExportDTO dto)
                                result.DataTypes.Add(dto);
                        }
                        else if (entry.FullName.StartsWith($"{PartialViewsFolder}/"))
                        {
                            if (partialViewSerializer.Deserialize(ms) is PartialViewExportDTO dto)
                                result.PartialViews.Add(dto);
                        }
                    }
                    catch (Exception ex)
                    {
                        logger.LogWarning(ex, "Failed to read entry {Entry} from ZIP", entry.FullName);
                    }
                }
            }

            return result;
        }

        public async Task<ImportResultDTO> ImportPackageAsync(
            BlockFarmEditorExportPackageDTO package, 
            bool overwriteElementTypes = true,
            bool overwriteCompositions = true,
            bool overwriteBlockDefinitions = true, 
            bool overwritePartialViews = true, 
            bool overwriteDataTypes = false)
        {
            var result = new ImportResultDTO();
            var currentUser = await GetCurrentUserIdAsync();

            // Identify which aliases are used as compositions by other element types
            var compositionAliases = package.ElementTypes
                .SelectMany(et => et.CompositionAliases)
                .ToHashSet();

            // 1. Import data types first (they're dependencies for element types)
            // Cache imported data types for property type creation
            var importedDataTypes = new Dictionary<Guid, IDataType>();

            foreach (var dataTypeDto in package.DataTypes)
            {
                try
                {
                    var existingDataType = await dataTypeService.GetAsync(dataTypeDto.Key);
                    if (existingDataType != null)
                    {
                        if (overwriteDataTypes)
                        {
                            // Update existing data type
                            existingDataType.Name = dataTypeDto.Name;
                            existingDataType.EditorUiAlias = dataTypeDto.EditorUiAlias;

                            // Update configuration
                            var configDict = DeserializeConfigurationData(dataTypeDto.ConfigurationItems);
                            existingDataType.ConfigurationData = configDict;

                            await dataTypeService.UpdateAsync(existingDataType, currentUser);
                            result.DataTypes.Updated++;
                            logger.LogInformation("Updated data type: {Name}", dataTypeDto.Name);
                        }
                        else
                        {
                            result.DataTypes.Skipped++;
                            logger.LogInformation("Skipped existing data type (overwrite disabled): {Name}", dataTypeDto.Name);
                        }
                        importedDataTypes[dataTypeDto.Key] = existingDataType;
                    }
                    else
                    {
                        // Create new data type
                        var editor = propertyEditors[dataTypeDto.EditorAlias];
                        if (editor == null)
                        {
                            result.DataTypes.Failed++;
                            logger.LogWarning("Property editor not found: {EditorAlias}. Skipping data type: {Name}", dataTypeDto.EditorAlias, dataTypeDto.Name);
                            continue;
                        }

                        // Ensure folder structure exists and get the parent ID
                        var parentId = await folderStructureService.EnsureDataTypeFolderStructureAsync(dataTypeDto.FolderPath);

                        var newDataType = new DataType(editor, configurationEditorJsonSerializer, parentId)
                        {
                            Key = dataTypeDto.Key,
                            Name = dataTypeDto.Name,
                            EditorUiAlias = dataTypeDto.EditorUiAlias,
                            ConfigurationData = DeserializeConfigurationData(dataTypeDto.ConfigurationItems)
                        };

                        var createResult = await dataTypeService.CreateAsync(newDataType, currentUser);
                        if (createResult.Success)
                        {
                            importedDataTypes[dataTypeDto.Key] = createResult.Result;
                            result.DataTypes.Created++;
                            logger.LogInformation("Created data type: {Name}", dataTypeDto.Name);
                        }
                        else
                        {
                            result.DataTypes.Failed++;
                            logger.LogError("Failed to create data type: {Name}. Status: {Status}", dataTypeDto.Name, createResult.Status);
                        }
                    }
                }
                catch (Exception ex)
                {
                    result.DataTypes.Failed++;
                    logger.LogError(ex, "Failed to import data type: {Name}", dataTypeDto.Name);
                }
            }

            // 2. Import element types (sorted by dependency - compositions first)
            var sortedElementTypes = SortContentTypesByDependency(package.ElementTypes);
            
            foreach (var elementTypeDto in sortedElementTypes)
            {
                // Determine if this element type is used as a composition
                var isComposition = compositionAliases.Contains(elementTypeDto.Alias);
                var shouldOverwrite = isComposition ? overwriteCompositions : overwriteElementTypes;
                var counts = isComposition ? result.Compositions : result.ElementTypes;
                var typeLabel = isComposition ? "composition" : "element type";

                try
                {
                    var existingContentType = contentTypeService.Get(elementTypeDto.Alias);
                    if (existingContentType != null)
                    {
                        if (shouldOverwrite)
                        {
                            // Update existing
                            await UpdateContentTypeFromDTO(existingContentType, elementTypeDto, importedDataTypes);
                            await contentTypeService.UpdateAsync(existingContentType, currentUser);
                            counts.Updated++;
                            logger.LogInformation("Updated {TypeLabel}: {Alias}", typeLabel, elementTypeDto.Alias);
                        }
                        else
                        {
                            counts.Skipped++;
                            logger.LogInformation("Skipped existing {TypeLabel} (overwrite disabled): {Alias}", typeLabel, elementTypeDto.Alias);
                        }
                    }
                    else
                    {
                        // Create new
                        var newContentType = await CreateContentTypeFromDTO(elementTypeDto, importedDataTypes);
                        await contentTypeService.CreateAsync(newContentType, currentUser);
                        counts.Created++;
                        logger.LogInformation("Created {TypeLabel}: {Alias}", typeLabel, elementTypeDto.Alias);
                    }
                }
                catch (Exception ex)
                {
                    counts.Failed++;
                    logger.LogError(ex, "Failed to import {TypeLabel}: {Alias}", typeLabel, elementTypeDto.Alias);
                }
            }

            // 3. Link compositions to element types (must be done after all are created)
            foreach (var elementTypeDto in sortedElementTypes)
            {
                if (elementTypeDto.CompositionAliases.Count == 0) continue;
                
                try
                {
                    var contentType = contentTypeService.Get(elementTypeDto.Alias);
                    if (contentType != null)
                    {
                        await LinkCompositionsToContentType(contentType, elementTypeDto.CompositionAliases, currentUser);
                    }
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Failed to link compositions for element type: {Alias}", elementTypeDto.Alias);
                }
            }

            // 4. Import definitions
            using var db = umbracoDatabaseFactory.CreateDatabase();
            foreach (var definitionDto in package.Definitions)
            {
                try
                {
                    var existingDefinition = await definitionService.GetByAliasAsync(db, definitionDto.ContentTypeAlias);
                    if (existingDefinition != null)
                    {
                        if (overwriteBlockDefinitions)
                        {
                            await definitionService.UpdateAsync(db, existingDefinition.Id, definitionDto.Type, definitionDto.ViewPath, definitionDto.Category, definitionDto.Enabled, currentUser);
                            result.Definitions.Updated++;
                            logger.LogInformation("Updated definition: {Alias}", definitionDto.ContentTypeAlias);
                        }
                        else
                        {
                            result.Definitions.Skipped++;
                            logger.LogInformation("Skipped existing definition (overwrite disabled): {Alias}", definitionDto.ContentTypeAlias);
                        }
                    }
                    else
                    {
                        await definitionService.CreateAsync(db, definitionDto, currentUser);
                        result.Definitions.Created++;
                        logger.LogInformation("Created definition: {Alias}", definitionDto.ContentTypeAlias);
                    }
                }
                catch (Exception ex)
                {
                    result.Definitions.Failed++;
                    logger.LogError(ex, "Failed to import definition: {Alias}", definitionDto.ContentTypeAlias);
                }
            }

            // 5. Import partial views
            var viewsPath = Path.Combine(webHostEnvironment.ContentRootPath, "Views");
            foreach (var partialViewDto in package.PartialViews)
            {
                try
                {
                    var targetPath = Path.Combine(viewsPath, partialViewDto.Path);
                    var targetDir = Path.GetDirectoryName(targetPath);
                    
                    // Check if file exists
                    if (IOFile.Exists(targetPath))
                    {
                        if (overwritePartialViews)
                        {
                            if (!string.IsNullOrEmpty(targetDir))
                            {
                                Directory.CreateDirectory(targetDir);
                            }
                            await IOFile.WriteAllTextAsync(targetPath, partialViewDto.Content);
                            result.PartialViews.Updated++;
                            logger.LogInformation("Updated partial view: {Path}", partialViewDto.Path);
                        }
                        else
                        {
                            result.PartialViews.Skipped++;
                            logger.LogInformation("Skipped existing partial view (overwrite disabled): {Path}", partialViewDto.Path);
                        }
                    }
                    else
                    {
                        // Create new partial view
                        if (!string.IsNullOrEmpty(targetDir))
                        {
                            Directory.CreateDirectory(targetDir);
                        }
                        await IOFile.WriteAllTextAsync(targetPath, partialViewDto.Content);
                        result.PartialViews.Created++;
                        logger.LogInformation("Created partial view: {Path}", partialViewDto.Path);
                    }
                }
                catch (Exception ex)
                {
                    result.PartialViews.Failed++;
                    logger.LogError(ex, "Failed to import partial view: {Path}", partialViewDto.Path);
                }
            }

            return result;
        }

        #region Helper Methods

        /// <summary>
        /// Recursively collects an element type and all its compositions.
        /// </summary>
        private static void CollectElementTypeAndCompositions(
            IContentType contentType,
            IEnumerable<IContentType> allElementTypes,
            HashSet<string> exportedAliases,
            List<IContentType> elementTypesToExport)
        {
            if (!exportedAliases.Add(contentType.Alias)) return;

            // First collect all compositions recursively
            foreach (var composition in contentType.ContentTypeComposition)
            {
                var fullComposition = allElementTypes.FirstOrDefault(et => et.Alias == composition.Alias);
                if (fullComposition != null)
                {
                    CollectElementTypeAndCompositions(fullComposition, allElementTypes, exportedAliases, elementTypesToExport);
                }
            }

            // Then add this element type
            elementTypesToExport.Add(contentType);
        }

        /// <summary>
        /// Sorts content types by their dependencies so that compositions come before types that use them.
        /// </summary>
        private static List<IContentType> SortByDependency(List<IContentType> contentTypes)
        {
            var aliases = contentTypes.Select(c => c.Alias).ToHashSet();
            var sorted = new List<IContentType>();
            var processed = new HashSet<string>();

            void Process(IContentType ct)
            {
                if (processed.Contains(ct.Alias)) return;

                // Process dependencies first
                foreach (var comp in ct.ContentTypeComposition)
                {
                    if (aliases.Contains(comp.Alias))
                    {
                        var dep = contentTypes.FirstOrDefault(c => c.Alias == comp.Alias);
                        if (dep != null) Process(dep);
                    }
                }

                processed.Add(ct.Alias);
                sorted.Add(ct);
            }

            foreach (var ct in contentTypes) Process(ct);
            return sorted;
        }

        private async Task<Guid> GetCurrentUserIdAsync()
        {
            await Task.CompletedTask;
            var user = userService.GetUserById(-1); // Admin user as fallback
            return user?.Key ?? Guid.Empty;
        }

        private static IDictionary<string, object> DeserializeConfigurationData(List<DataTypeConfigurationItemDTO> items)
        {
            var result = new Dictionary<string, object>();
            foreach (var item in items)
            {
                if (item.Value != null)
                {
                    try
                    {
                        var obj = JsonSerializer.Deserialize<object>(item.Value);
                        result[item.Key] = obj ?? item.Value;
                    }
                    catch
                    {
                        result[item.Key] = item.Value;
                    }
                }
            }
            return result;
        }

        /// <summary>
        /// Links composition content types to a content type based on the aliases from the DTO.
        /// </summary>
        private async Task LinkCompositionsToContentType(IContentType contentType, List<string> compositionAliases, Guid currentUserKey)
        {
            var existingCompositionAliases = contentType.ContentTypeComposition.Select(c => c.Alias).ToHashSet();
            var needsUpdate = false;

            foreach (var compositionAlias in compositionAliases)
            {
                if (existingCompositionAliases.Contains(compositionAlias)) continue;

                var composition = contentTypeService.Get(compositionAlias);
                if (composition != null)
                {
                    contentType.AddContentType(composition);
                    needsUpdate = true;
                    logger.LogInformation("Linked composition {CompositionAlias} to {ContentTypeAlias}", compositionAlias, contentType.Alias);
                }
                else
                {
                    logger.LogWarning("Composition not found: {Alias} for content type {ContentTypeAlias}", compositionAlias, contentType.Alias);
                }
            }

            if (needsUpdate)
            {
                await contentTypeService.UpdateAsync(contentType, currentUserKey);
            }
        }

        /// <summary>
        /// Sorts content types (DTOs) by their dependencies so that compositions come before types that use them.
        /// </summary>
        private static List<ContentTypeExportDTO> SortContentTypesByDependency(List<ContentTypeExportDTO> contentTypes)
        {
            var aliases = contentTypes.Select(c => c.Alias).ToHashSet();
            var sorted = new List<ContentTypeExportDTO>();
            var processed = new HashSet<string>();

            void Process(ContentTypeExportDTO ct)
            {
                if (processed.Contains(ct.Alias)) return;

                // Process dependencies first
                foreach (var depAlias in ct.CompositionAliases)
                {
                    if (aliases.Contains(depAlias))
                    {
                        var dependency = contentTypes.FirstOrDefault(c => c.Alias == depAlias);
                        if (dependency != null) Process(dependency);
                    }
                }

                processed.Add(ct.Alias);
                sorted.Add(ct);
            }

            foreach (var ct in contentTypes) Process(ct);
            return sorted;
        }

        #endregion

        private async Task<ContentTypeExportDTO> MapContentTypeToExportDTOAsync(IContentType contentType)
        {
            var dto = new ContentTypeExportDTO
            {
                Key = contentType.Key,
                Alias = contentType.Alias,
                Name = contentType.Name ?? contentType.Alias,
                Description = contentType.Description,
                Icon = contentType.Icon,
                IsElement = contentType.IsElement,
                AllowedAsRoot = contentType.AllowedAsRoot,
                VariesByCulture = contentType.VariesByCulture(),
                VariesBySegment = contentType.VariesBySegment(),
                FolderPath = await folderStructureService.BuildContentTypeFolderPathAsync(contentType)
            };

            foreach (var group in contentType.PropertyGroups)
            {
                var groupDto = new PropertyGroupExportDTO
                {
                    Key = group.Key,
                    Alias = group.Alias,
                    Name = group.Name ?? group.Alias,
                    SortOrder = group.SortOrder,
                    Type = group.Type.ToString()
                };

                if (group.PropertyTypes != null)
                {
                    foreach (var propertyType in group.PropertyTypes)
                    {
                        groupDto.PropertyTypes.Add(MapPropertyTypeToExportDTO(propertyType));
                    }
                }

                dto.PropertyGroups.Add(groupDto);
            }

            // Export property types that are not in any group
            foreach (var propertyType in contentType.NoGroupPropertyTypes)
            {
                dto.NoGroupPropertyTypes.Add(MapPropertyTypeToExportDTO(propertyType));
            }

            foreach (var composition in contentType.ContentTypeComposition)
            {
                dto.CompositionAliases.Add(composition.Alias);
            }

            return dto;
        }

        private async Task<DataTypeExportDTO> MapDataTypeToExportDTOAsync(IDataType dataType)
        {
            var dto = new DataTypeExportDTO
            {
                Key = dataType.Key,
                Name = dataType.Name ?? "Unknown",
                EditorAlias = dataType.EditorAlias,
                EditorUiAlias = dataType.EditorUiAlias,
                FolderPath = await folderStructureService.BuildDataTypeFolderPathAsync(dataType)
            };

            // Serialize configuration data
            foreach (var configItem in dataType.ConfigurationData)
            {
                var valueStr = configItem.Value switch
                {
                    string s => s,
                    null => null,
                    _ => JsonSerializer.Serialize(configItem.Value)
                };

                dto.ConfigurationItems.Add(new DataTypeConfigurationItemDTO
                {
                    Key = configItem.Key,
                    Value = valueStr
                });
            }

            return dto;
        }

        private async Task<ContentType> CreateContentTypeFromDTO(ContentTypeExportDTO dto, Dictionary<Guid, IDataType> importedDataTypes)
        {
            // Ensure folder structure exists and get parent ID
            var parentId = await folderStructureService.EnsureContentTypeFolderStructureAsync(dto.FolderPath);

            var contentType = new ContentType(shortStringHelper, parentId)
            {
                Key = dto.Key,
                Alias = dto.Alias,
                Name = dto.Name,
                Description = dto.Description,
                Icon = dto.Icon,
                IsElement = dto.IsElement,
                AllowedAsRoot = dto.AllowedAsRoot
            };

            contentType.Variations = GetContentVariation(dto.VariesByCulture, dto.VariesBySegment);

            // Add property groups and properties
            foreach (var groupDto in dto.PropertyGroups)
            {
                var groupType = Enum.TryParse<PropertyGroupType>(groupDto.Type, out var gt) ? gt : PropertyGroupType.Group;
                var group = new PropertyGroup(false)
                {
                    Key = groupDto.Key,
                    Alias = groupDto.Alias,
                    Name = groupDto.Name,
                    SortOrder = groupDto.SortOrder,
                    Type = groupType
                };

                foreach (var propDto in groupDto.PropertyTypes)
                {
                    var propertyType = await CreatePropertyTypeFromDTO(propDto, importedDataTypes);
                    if (propertyType != null)
                    {
                        group.PropertyTypes?.Add(propertyType);
                    }
                }

                contentType.PropertyGroups.Add(group);
            }

            // Add property types that are not in any group
            foreach (var propDto in dto.NoGroupPropertyTypes)
            {
                var propertyType = await CreatePropertyTypeFromDTO(propDto, importedDataTypes);
                if (propertyType != null)
                {
                    contentType.AddPropertyType(propertyType);
                }
            }

            return contentType;
        }

        private async Task UpdateContentTypeFromDTO(IContentType contentType, ContentTypeExportDTO dto, Dictionary<Guid, IDataType> importedDataTypes)
        {
            contentType.Name = dto.Name;
            contentType.Description = dto.Description;
            contentType.Icon = dto.Icon;
            contentType.IsElement = dto.IsElement;
            contentType.AllowedAsRoot = dto.AllowedAsRoot;
            contentType.Variations = GetContentVariation(dto.VariesByCulture, dto.VariesBySegment);

            // Update property groups
            foreach (var groupDto in dto.PropertyGroups)
            {
                var existingGroup = contentType.PropertyGroups.FirstOrDefault(g => g.Alias == groupDto.Alias);
                if (existingGroup == null)
                {
                    var groupType = Enum.TryParse<PropertyGroupType>(groupDto.Type, out var gt) ? gt : PropertyGroupType.Group;
                    existingGroup = new PropertyGroup(false)
                    {
                        Key = groupDto.Key,
                        Alias = groupDto.Alias,
                        Name = groupDto.Name,
                        SortOrder = groupDto.SortOrder,
                        Type = groupType
                    };
                    contentType.PropertyGroups.Add(existingGroup);
                }
                else
                {
                    existingGroup.Name = groupDto.Name;
                    existingGroup.SortOrder = groupDto.SortOrder;
                }

                foreach (var propDto in groupDto.PropertyTypes)
                {
                    var existingProp = contentType.PropertyTypes.FirstOrDefault(p => p.Alias == propDto.Alias);
                    if (existingProp == null)
                    {
                        var propertyType = await CreatePropertyTypeFromDTO(propDto, importedDataTypes);
                        if (propertyType != null)
                        {
                            existingGroup.PropertyTypes?.Add(propertyType);
                        }
                    }
                    else
                    {
                        UpdatePropertyTypeFromDTO(existingProp, propDto);
                    }
                }
            }

            // Handle property types that are not in any group
            foreach (var propDto in dto.NoGroupPropertyTypes)
            {
                var existingProp = contentType.PropertyTypes.FirstOrDefault(p => p.Alias == propDto.Alias);
                if (existingProp == null)
                {
                    var propertyType = await CreatePropertyTypeFromDTO(propDto, importedDataTypes);
                    if (propertyType != null)
                    {
                        contentType.AddPropertyType(propertyType);
                    }
                }
                else
                {
                    UpdatePropertyTypeFromDTO(existingProp, propDto);
                }
            }
        }

        /// <summary>
        /// Maps an Umbraco IPropertyType to a PropertyTypeExportDTO.
        /// </summary>
        private static PropertyTypeExportDTO MapPropertyTypeToExportDTO(IPropertyType propertyType)
        {
            return new PropertyTypeExportDTO
            {
                Key = propertyType.Key,
                Alias = propertyType.Alias,
                Name = propertyType.Name ?? propertyType.Alias,
                Description = propertyType.Description,
                SortOrder = propertyType.SortOrder,
                DataTypeKey = propertyType.DataTypeKey,
                Mandatory = propertyType.Mandatory,
                MandatoryMessage = propertyType.MandatoryMessage,
                ValidationRegExp = propertyType.ValidationRegExp,
                ValidationRegExpMessage = propertyType.ValidationRegExpMessage,
                VariesByCulture = propertyType.VariesByCulture(),
                VariesBySegment = propertyType.VariesBySegment(),
                LabelOnTop = propertyType.LabelOnTop ? 1 : 0
            };
        }

        /// <summary>
        /// Updates an existing IPropertyType from a PropertyTypeExportDTO.
        /// </summary>
        private static void UpdatePropertyTypeFromDTO(IPropertyType existingProp, PropertyTypeExportDTO propDto)
        {
            existingProp.Name = propDto.Name;
            existingProp.Description = propDto.Description;
            existingProp.SortOrder = propDto.SortOrder;
            existingProp.Mandatory = propDto.Mandatory;
            existingProp.MandatoryMessage = propDto.MandatoryMessage;
            existingProp.ValidationRegExp = propDto.ValidationRegExp;
            existingProp.ValidationRegExpMessage = propDto.ValidationRegExpMessage;
            existingProp.LabelOnTop = propDto.LabelOnTop == 1;
        }

        private async Task<PropertyType?> CreatePropertyTypeFromDTO(PropertyTypeExportDTO propDto, Dictionary<Guid, IDataType> importedDataTypes)
        {
            // Get the data type
            IDataType? dataType = null;
            if (importedDataTypes.TryGetValue(propDto.DataTypeKey, out var imported))
            {
                dataType = imported;
            }
            else
            {
                dataType = await dataTypeService.GetAsync(propDto.DataTypeKey);
            }

            if (dataType == null)
            {
                logger.LogWarning("Data type not found for property type {Alias}. DataTypeKey: {Key}", propDto.Alias, propDto.DataTypeKey);
                return null;
            }

            var propertyType = new PropertyType(shortStringHelper, dataType, propDto.Alias)
            {
                Key = propDto.Key,
                Name = propDto.Name,
                Description = propDto.Description,
                SortOrder = propDto.SortOrder,
                Mandatory = propDto.Mandatory,
                MandatoryMessage = propDto.MandatoryMessage,
                ValidationRegExp = propDto.ValidationRegExp,
                ValidationRegExpMessage = propDto.ValidationRegExpMessage,
                Variations = GetContentVariation(propDto.VariesByCulture, propDto.VariesBySegment),
                LabelOnTop = propDto.LabelOnTop == 1
            };

            return propertyType;
        }

        /// <summary>
        /// Calculates ContentVariation flags from boolean properties.
        /// </summary>
        private static ContentVariation GetContentVariation(bool variesByCulture, bool variesBySegment)
        {
            var variation = ContentVariation.Nothing;
            if (variesByCulture) variation |= ContentVariation.Culture;
            if (variesBySegment) variation |= ContentVariation.Segment;
            return variation;
        }
    }
}
