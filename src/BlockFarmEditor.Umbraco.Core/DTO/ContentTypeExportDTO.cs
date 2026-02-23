using System.Runtime.Serialization;
using System.Xml.Serialization;

namespace BlockFarmEditor.Umbraco.Core.DTO
{
    /// <summary>
    /// Serializable DTO for exporting Umbraco element types (content types).
    /// </summary>
    [Serializable]
    [DataContract]
    public class ContentTypeExportDTO
    {
        [DataMember]
        public Guid Key { get; set; }

        [DataMember]
        public required string Alias { get; set; }

        [DataMember]
        public required string Name { get; set; }

        [DataMember]
        [XmlElement(IsNullable = true)]
        public string? Description { get; set; }

        [DataMember]
        [XmlElement(IsNullable = true)]
        public string? Icon { get; set; }

        [DataMember]
        public bool IsElement { get; set; } = true;

        [DataMember]
        public bool AllowedAsRoot { get; set; }

        [DataMember]
        public bool VariesByCulture { get; set; }

        [DataMember]
        public bool VariesBySegment { get; set; }

        /// <summary>
        /// The folder path where this element type is located in Umbraco (e.g., "Elements/Blocks/Hero").
        /// Used to recreate the folder structure on import.
        /// </summary>
        [DataMember]
        [XmlElement(IsNullable = true)]
        public string? FolderPath { get; set; }

        [DataMember]
        [XmlArray("PropertyGroups")]
        [XmlArrayItem("PropertyGroup")]
        public List<PropertyGroupExportDTO> PropertyGroups { get; set; } = [];

        /// <summary>
        /// Property types that are not assigned to any group (shown in "Generic properties" tab).
        /// </summary>
        [DataMember]
        [XmlArray("NoGroupPropertyTypes")]
        [XmlArrayItem("PropertyType")]
        public List<PropertyTypeExportDTO> NoGroupPropertyTypes { get; set; } = [];

        [DataMember]
        [XmlArray("CompositionAliases")]
        [XmlArrayItem("Alias")]
        public List<string> CompositionAliases { get; set; } = [];
    }

    /// <summary>
    /// Serializable DTO for property groups (tabs/groups).
    /// </summary>
    [Serializable]
    [DataContract]
    public class PropertyGroupExportDTO
    {
        [DataMember]
        public Guid Key { get; set; }

        [DataMember]
        public required string Alias { get; set; }

        [DataMember]
        public required string Name { get; set; }

        [DataMember]
        public int SortOrder { get; set; }

        /// <summary>
        /// "Tab" or "Group"
        /// </summary>
        [DataMember]
        public required string Type { get; set; }

        [DataMember]
        [XmlArray("PropertyTypes")]
        [XmlArrayItem("PropertyType")]
        public List<PropertyTypeExportDTO> PropertyTypes { get; set; } = [];
    }

    /// <summary>
    /// Serializable DTO for property types.
    /// </summary>
    [Serializable]
    [DataContract]
    public class PropertyTypeExportDTO
    {
        [DataMember]
        public Guid Key { get; set; }

        [DataMember]
        public required string Alias { get; set; }

        [DataMember]
        public required string Name { get; set; }

        [DataMember]
        [XmlElement(IsNullable = true)]
        public string? Description { get; set; }

        [DataMember]
        public int SortOrder { get; set; }

        [DataMember]
        public Guid DataTypeKey { get; set; }

        [DataMember]
        public bool Mandatory { get; set; }

        [DataMember]
        [XmlElement(IsNullable = true)]
        public string? MandatoryMessage { get; set; }

        [DataMember]
        [XmlElement(IsNullable = true)]
        public string? ValidationRegExp { get; set; }

        [DataMember]
        [XmlElement(IsNullable = true)]
        public string? ValidationRegExpMessage { get; set; }

        [DataMember]
        public bool VariesByCulture { get; set; }

        [DataMember]
        public bool VariesBySegment { get; set; }

        [DataMember]
        public int LabelOnTop { get; set; }
    }
}
