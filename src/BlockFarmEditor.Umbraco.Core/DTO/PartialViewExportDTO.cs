using System.Runtime.Serialization;

namespace BlockFarmEditor.Umbraco.Core.DTO
{
    /// <summary>
    /// Serializable DTO for exporting partial view files.
    /// </summary>
    [Serializable]
    [DataContract]
    public class PartialViewExportDTO
    {
        /// <summary>
        /// Relative path from Views folder (e.g., "Partials/BlockFarm/MyBlock.cshtml").
        /// </summary>
        [DataMember]
        public required string Path { get; set; }

        /// <summary>
        /// The full content of the .cshtml file.
        /// </summary>
        [DataMember]
        public required string Content { get; set; }
    }
}
