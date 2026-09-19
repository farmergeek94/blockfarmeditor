using BlockFarmEditor.Umbraco.Core.Interfaces;
using BlockFarmEditor.Umbraco.Library.Models;
using BlockFarmEditor.Umbraco.Library.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Umbraco.Cms.Core.IO;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Models.Editors;
using Umbraco.Cms.Core.PropertyEditors;
using Umbraco.Cms.Core.Serialization;
using Umbraco.Cms.Core.Strings;
using static Umbraco.Cms.Core.Constants;

namespace BlockFarmEditor.Umbraco.Library.Editors
{
    [DataEditor(
    BlockFarmEditorContext.BlockFarmEditorEditorAlias
    )] // Icon for the editor
    public class BlockFarmEditorPropertyEditor(IDataValueEditorFactory dataValueEditorFactory) : DataEditor(dataValueEditorFactory)
    {
        protected override IDataValueEditor CreateValueEditor() =>
            DataValueEditorFactory.Create<BlockFarmEditorValueEditor>(Attribute!);
    }

    // provides the property value editor
    internal class BlockFarmEditorValueEditor(
        IShortStringHelper shortStringHelper,
        IJsonSerializer jsonSerializer,
        IIOHelper ioHelper,
        DataEditorAttribute attribute,
        IBlockPropertyValueMapper blockPropertyValueMapper
            ) : DataValueEditor(shortStringHelper, jsonSerializer, ioHelper, attribute)
    {
        public override object? FromEditor(ContentPropertyData editorValue, object? currentValue)
        {
            object? value = editorValue.Value;
            if (editorValue.Value is JsonObject obj && BlockData.Parse(obj) is BlockData blockData)
            {
                blockPropertyValueMapper.FromEditor(blockData);
                value = blockData.ToJson();
            }
            else if (editorValue.Value is JsonNode node)
            {
                value = node.ToJsonString();
            }

            ContentPropertyData contentPropertyData = new(value, editorValue.DataTypeConfiguration);
            var result = base.FromEditor(contentPropertyData, currentValue);
            return result;
        }

        public override object? ToEditor(IProperty property, string? culture = null, string? segment = null)
        {
            var value = property.GetValue(culture, segment);
            if (value == null)
            {
                return string.Empty;
            }
            if(value is not string strValue)
            {
                strValue = value?.ToString() ?? "";
            }
            var result = JsonNode.Parse(strValue);
            if (result is not JsonObject || BlockData.Parse(result) is not BlockData blockData)
            {
                return result;
            }
            blockPropertyValueMapper.ToEditor(blockData);
            return blockData.ToJsonNode();
        }
    }
}
