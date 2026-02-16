using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Models.PublishedContent;

namespace BlockFarmEditor.Umbraco.Core.Models.BuilderModels
{
    public class PageDefinition : IContainerDefinition
    {
        public const string GuidUnique = "64D51B94-10C1-4225-9329-B2A54D0E47CE";
        public IEnumerable<BlockDefinition<IPublishedElement>> Blocks { get; set; } = [];
        public Guid? ContentTypeKey => new(GuidUnique);

        public Guid Unique { get; set; } = new(GuidUnique);
    }
}
