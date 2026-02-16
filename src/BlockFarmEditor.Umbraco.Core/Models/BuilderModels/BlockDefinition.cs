using Umbraco.Cms.Core.Models.PublishedContent;

namespace BlockFarmEditor.Umbraco.Core.Models.BuilderModels
{
    public class BlockDefinition<T> : IContainerDefinition where T : IPublishedElement
    {
        private T? _properties = default!;

        public Guid? ContentTypeKey => Properties?.ContentType.Key;
        public T? Properties { get => _properties; set { 
                _properties = value;
                if(_properties != null)
                {
                    Unique = _properties.Key;
                }
            } 
        }
        public IEnumerable<BlockDefinition<IPublishedElement>> Blocks { get; set; } = [];
        public Guid Unique { get; set; }
    }
}
