using System.Runtime.Serialization;
using System.Xml.Serialization;

namespace BlockFarmEditor.Umbraco.Core.DTO
{
    /// <summary>
    /// Root container for a comprehensive BlockFarmEditor export package.
    /// Contains definitions, element types, data types, and partial views.
    /// </summary>
    [Serializable]
    [DataContract]
    [XmlRoot("BlockFarmEditorExportPackage")]
    public class BlockFarmEditorExportPackageDTO
    {
        /// <summary>
        /// Package format version for future compatibility.
        /// </summary>
        [DataMember]
        public string Version { get; set; } = "1.0";

        /// <summary>
        /// Timestamp when the package was created.
        /// </summary>
        [DataMember]
        public DateTime ExportedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// BlockFarmEditor definition records.
        /// </summary>
        [DataMember]
        [XmlArray("Definitions")]
        [XmlArrayItem("Definition")]
        public List<BlockFarmEditorDefinitionDTO> Definitions { get; set; } = [];

        /// <summary>
        /// Umbraco element types linked to the definitions.
        /// </summary>
        [DataMember]
        [XmlArray("ElementTypes")]
        [XmlArrayItem("ElementType")]
        public List<ContentTypeExportDTO> ElementTypes { get; set; } = [];

        /// <summary>
        /// Data types used by the element type properties.
        /// </summary>
        [DataMember]
        [XmlArray("DataTypes")]
        [XmlArrayItem("DataType")]
        public List<DataTypeExportDTO> DataTypes { get; set; } = [];

        /// <summary>
        /// Partial view files linked to the definitions.
        /// </summary>
        [DataMember]
        [XmlArray("PartialViews")]
        [XmlArrayItem("PartialView")]
        public List<PartialViewExportDTO> PartialViews { get; set; } = [];
    }
}
