using BlockFarmEditor.Umbraco.Core.Attributes;
using BlockFarmEditor.Umbraco.Core.DTO;
using BlockFarmEditor.Umbraco.Core.Interfaces;
using BlockFarmEditor.Umbraco.Core.Models;
using BlockFarmEditor.Umbraco.Core.Models.BuilderModels;
using BlockFarmEditor.Umbraco.Core.Models.ConfigModels;
using BlockFarmEditor.Umbraco.Library.Converters;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using System.Reflection;
using System.Text.Json;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Models.ContentTypeEditing;
using Umbraco.Cms.Core.Scoping;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Infrastructure.Persistence;

namespace BlockFarmEditor.Umbraco.Library.Services
{
    internal class BlockDefinitionService(
        IServiceProvider serviceProvider, 
        IDataTypeService dataTypeService, 
        ILogger<BlockDefinitionService> logger,
        ICoreScopeProvider coreScopeProvider,
        IUmbracoDatabaseFactory umbracoDatabaseFactory,
        IContentTypeService contentTypeService) : IBlockDefinitionService
    {
        private readonly ConcurrentDictionary<string, BlockFarmEditorDefinitionAttribute> _typeMap = new();
        private readonly ConcurrentDictionary<string, IEnumerable<BlockFarmEditorConfigurationAttribute>> _configMap = new();

        private readonly ConcurrentDictionary<Guid, BlockFarmEditorDefinitionExpanded> _blockFarmEditorDefinition = new();
        private readonly object _initializationLock = new object();

        private Dictionary<string, BlockFarmEditorDefinitionAttribute> GetTypeMap()
        {
            if (_typeMap.Count == 0)
            {
                lock (_initializationLock)
                {
                    if (_typeMap.Count == 0)
                    {
                        var typeToSearch = typeof(BlockFarmEditorDefinitionAttribute);
                        Assembly[] _assemblies = AppDomain.CurrentDomain.GetAssemblies();
                        var attributes = _assemblies.SelectMany(a => a.GetCustomAttributes(typeToSearch, true));

                        var attrs = attributes.Cast<BlockFarmEditorDefinitionAttribute>();

                        foreach(var attr in attrs)
                        {
                            // Register the attribute identifier
                            // Add to the type map
                            if(!_typeMap.TryAdd(attr.Identifier, attr))
                            {
                                throw new InvalidOperationException($"The identifier '{attr.Identifier}' is already registered as a BlockFarmEditor Block.");
                            }
                        }
                    }
                }
            }
            return _typeMap.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
        }

        public Dictionary<string, IEnumerable<BlockFarmEditorConfigurationAttribute>> GetConfigMaps()
        {
            if (_configMap.Count == 0)
            {
                lock (_initializationLock)
                {
                    if (_configMap.Count == 0)
                    {
                        var typeToSearch = typeof(BlockFarmEditorConfigurationAttribute);
                        Assembly[] _assemblies = AppDomain.CurrentDomain.GetAssemblies();
                        var attributes = _assemblies.SelectMany(a => a.GetCustomAttributes(typeToSearch, true));

                        var attrs = attributes.Cast<BlockFarmEditorConfigurationAttribute>().GroupBy(x => x.Alias);
                        foreach(var group in attrs)
                        {
                            _configMap.TryAdd(group.Key, group);
                        }
                    }
                }
            }
            return _configMap.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
        }
        
        public void ClearCache()
        {
            _blockFarmEditorDefinition.Clear();
        }

        public IDictionary<Guid, BlockFarmEditorDefinitionExpanded> RetrieveBlockFarmEditorDefinitions(bool force = false)
        {
            if (force)
            {
                ClearCache();
            }

            if (_blockFarmEditorDefinition.Count == 0)
            {
                lock (_initializationLock)
                {
                    // Double-check pattern: verify the collection is still empty after acquiring the lock
                    if (_blockFarmEditorDefinition.Count == 0)
                    {
                        using var scope = coreScopeProvider.CreateCoreScope();
                        using var umbracoDatabase = umbracoDatabaseFactory.CreateDatabase();
                        var query = umbracoDatabase.Query<BlockFarmEditorDefinitionDTO>();
                        var queryResults = query.Where(x => x.Enabled == true).ToList();
                        scope.Complete();

                        var definitionAliases = queryResults.Select(x => x.ContentTypeAlias);

                        var contentTypes = contentTypeService.GetAllElementTypes().Where(x => definitionAliases.Contains(x.Alias)).ToDictionary(x => x.Alias, x => x);

                        var typeMaps = GetTypeMap();

                        var configMaps = GetConfigMaps();

                        foreach (var dto in queryResults)
                        {
                            var item = BlockFarmEditorDefinitionExpanded.ExpandDto(dto);
                            item.DefinitionAttribute = typeMaps.GetValueOrDefault(item.ContentTypeAlias);
                            item.ContentType = contentTypes.GetValueOrDefault(item.ContentTypeAlias);
                            item.PropertyConfigs = configMaps.GetValueOrDefault(item.ContentTypeAlias) ?? [];
                            if(item.ContentType != null)
                                _blockFarmEditorDefinition.TryAdd(item.ContentType.Key, item);
                        }
                    }
                }
            }
            return _blockFarmEditorDefinition;
        }

        private async Task<IDictionary<string, BlockFarmEditorPropertyEditorModel>> _retrievePropertyTypes(IEnumerable<IPropertyType> propertyTypes, IEnumerable<BlockFarmEditorConfigurationAttribute> configs)
        {
            var propertyEditors = new Dictionary<string, BlockFarmEditorPropertyEditorModel>();

            // Get the properties of the type
            foreach (var property in propertyTypes)
            {
                var dataType = await dataTypeService.GetAsync(property.DataTypeKey);

                if (dataType == null)
                {
                    // If the data type is not found, skip this property
                    logger.LogWarning("Data type with key {DataTypeKey} not found for property {PropertyAlias}.", property.DataTypeKey, property.Alias);
                    continue;
                }

                var configuration = configs.FirstOrDefault(x => x.PropertyAlias == property.Alias);


                // If a block farm editor property editor attribute is present, use it to get the configurations
                IEnumerable<IBlockFarmEditorConfigItem> configurations = [];
                if (configuration != null)
                {
                    // Create an instance of the config type
                    // Using the service provider to resolve the dependencies
                    IBlockFarmEditorConfig pbConfig = (IBlockFarmEditorConfig)ActivatorUtilities.CreateInstance(serviceProvider, configuration.GetConfigType);

                    // Get the configurations from the config instance
                    configurations = await pbConfig.GetItems();
                }
                else
                {
                    // Get the configurations from the data type
                    configurations = dataType.ConfigurationData.Select(x => new BlockFarmEditorConfigItem()
                    {
                        Alias = x.Key,
                        Value = x.Value
                    });
                }
                // Add the property editor to the dictionary
                propertyEditors.Add(property.Alias, new BlockFarmEditorPropertyEditorModel()
                {
                    Alias = property.PropertyEditorAlias,
                    Label = property.Name,
                    Description = property.Description,
                    Configurations = configurations,
                    Validation = new PropertyTypeValidation
                    {
                        Mandatory = property.Mandatory,
                        MandatoryMessage = property.MandatoryMessage,
                        RegularExpression = property.ValidationRegExp,
                        RegularExpressionMessage = property.ValidationRegExpMessage,
                    }
                });
            }
            return propertyEditors;
        }

        public async Task<IEnumerable<BlockFarmEditorPropertyGroupModel>> RetrievePropertyEditors(Guid contentTyperKey)
        {
            if (RetrieveBlockFarmEditorDefinitions().TryGetValue(contentTyperKey, out var dto) && dto.ContentType != null)
            {
                var configs = GetConfigMaps().GetValueOrDefault(dto.ContentTypeAlias) ?? [];


                var orphanGroups = dto.ContentType.CompositionPropertyGroups.Where(x => x.GetParentAlias() == null && x.Type == PropertyGroupType.Group).ToList();

                var tabs = dto.ContentType.CompositionPropertyGroups.Where(x => x.Type == PropertyGroupType.Tab);

                var groups = dto.ContentType.CompositionPropertyGroups
                    .Where(x => x.Type == PropertyGroupType.Group);

                // if we have orphaned groups, display the Generic Tab and only if we have tabs period.  
                if(orphanGroups.Count != 0 && tabs.Any())
                {

                    tabs = tabs.Prepend(new PropertyGroup(false)
                    {
                        Alias = "orphaned",
                        Name = "Generic",
                        Type = PropertyGroupType.Tab,
                    });

                }

                var groupsToLoop = tabs.Any() ? tabs : groups;

                var groupModels = new List<BlockFarmEditorPropertyGroupModel>();

                foreach (var group in groupsToLoop)
                {
                    var groupModel = new BlockFarmEditorPropertyGroupModel(group.Alias, group.Name, group.Type);

                    if (group.PropertyTypes?.Any() == true)
                    {
                        var propertyEditors = await _retrievePropertyTypes(group.PropertyTypes, configs);
                        groupModel.Editors = propertyEditors;
                    }
                    else if (group.Type == PropertyGroupType.Group)
                    {
                        continue; // Skip groups without properties
                    }

                    if (group.Type == PropertyGroupType.Tab)
                    {
                        var childGroups = groups.Where(x => x.GetParentAlias() == group.Alias || (x.GetParentAlias() == null && group.Alias == "orphaned"));
                        var childGroupList = new List<BlockFarmEditorPropertyGroupModel>();
                        foreach (var childGroup in childGroups)
                        {
                            var childGroupModel = new BlockFarmEditorPropertyGroupModel(childGroup.Alias, childGroup.Name, childGroup.Type);
                            if (childGroup.PropertyTypes?.Any() == true)
                            {
                                var propertyEditors = await _retrievePropertyTypes(childGroup.PropertyTypes, configs);
                                childGroupModel.Editors = propertyEditors;
                            }

                            childGroupList.Add(childGroupModel);
                        }
                        groupModel.Groups = childGroupList;
                    }

                    groupModels.Add(groupModel);
                }


                return groupModels;
            }
            return [];
        }

        // setup the converters for the serializer
        // this allows us to serilize and deserilize using the concrete types.
        public JsonSerializerOptions JsonSerializerReaderOptions => new()
        {
            WriteIndented = false,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DictionaryKeyPolicy = JsonNamingPolicy.CamelCase,
            Converters = {
                new BuilderPropertiesConverter(serviceProvider)
            }
        };

        // setup the converters for the serializer
        // this allows us to serilize and deserilize using the concrete types.
        public JsonSerializerOptions JsonSerializerWriterOptions => new()
        {
            WriteIndented = false,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            Converters = {
                new BuilderPropertiesWriter(serviceProvider)
            }
        };
    }
}
