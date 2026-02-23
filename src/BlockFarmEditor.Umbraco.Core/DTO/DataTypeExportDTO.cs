using System.Runtime.Serialization;
using System.Xml.Serialization;

namespace BlockFarmEditor.Umbraco.Core.DTO
{
    /// <summary>
    /// Serializable DTO for exporting Umbraco data types.
    /// </summary>
    [Serializable]
    [DataContract]
    public class DataTypeExportDTO
    {
        [DataMember]
        public Guid Key { get; set; }

        [DataMember]
        public required string Name { get; set; }

        [DataMember]
        public required string EditorAlias { get; set; }

        [DataMember]
        public string? EditorUiAlias { get; set; }

        /// <summary>
        /// The folder path where this data type is located in Umbraco (e.g., "DataTypes/Custom").
        /// Used to recreate the folder structure on import.
        /// </summary>
        [DataMember]
        public string? FolderPath { get; set; }

        /// <summary>
        /// Configuration data serialized as XML string to preserve complex nested structures.
        /// </summary>
        [DataMember]
        public string? ConfigurationDataXml { get; set; }

        [DataMember]
        [XmlArray("ConfigurationItems")]
        [XmlArrayItem("ConfigurationItem")]
        public List<DataTypeConfigurationItemDTO> ConfigurationItems { get; set; } = [];
    }

    /// <summary>
    /// Represents a single configuration key-value pair.
    /// </summary>
    [Serializable]
    [DataContract]
    public class DataTypeConfigurationItemDTO
    {
        [DataMember]
        public required string Key { get; set; }

        /// <summary>
        /// Value stored as string. Complex objects should be serialized to XML/JSON string.
        /// </summary>
        [DataMember]
        public string? Value { get; set; }
    }
}
