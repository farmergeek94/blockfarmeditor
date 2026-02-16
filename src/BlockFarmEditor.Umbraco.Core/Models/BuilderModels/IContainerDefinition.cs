using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Models.PublishedContent;

namespace BlockFarmEditor.Umbraco.Core.Models.BuilderModels
{
    public interface IContainerDefinition
    {
        Guid? ContentTypeKey { get; }
        Guid Unique { get; set; }
        IEnumerable<BlockDefinition<IPublishedElement>> Blocks { get; set; }    }
}
