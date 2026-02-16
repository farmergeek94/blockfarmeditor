using BlockFarmEditor.Umbraco.Core.Interfaces;
using BlockFarmEditor.Umbraco.Core.Models.BuilderModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlockFarmEditor.Umbraco.Core.Models
{
    /// <summary>
    /// This class is used to manage the scope of the BlockFarmEditor container.
    /// </summary>
    /// <param name="blockFarmEditorDataContextService"></param>
    /// <param name="oldScope"></param>
    public class BlockFarmEditorContainerScope(IBlockFarmEditorContext blockFarmEditorContext, IContainerDefinition containerDefinition, BlockFarmEditorContainerScope? oldScope) : IDisposable
    {
        public IContainerDefinition Block { get; } = containerDefinition;

        public IEnumerable<Guid> IdentifierList { get; } = !containerDefinition.ContentTypeKey.HasValue ? [.. oldScope?.IdentifierList ?? []] : [.. oldScope?.IdentifierList ?? [], containerDefinition.ContentTypeKey.Value];

        public void Dispose()
        {
            blockFarmEditorContext._currentScope = oldScope;
        }

        public string GetIdentityPath()
        {
            // Convert the input string to a byte array
            byte[] inputBytes = Encoding.UTF8.GetBytes(string.Join("|", IdentifierList));

            // Convert the byte array to a Base64 string
            return Convert.ToBase64String(inputBytes);
        }

        public static IEnumerable<string> ParseIdentityPath(string identityPath)
        {
            // Convert the Base64 string back to a byte array
            byte[] outputBytes = Convert.FromBase64String(identityPath);
            // Convert the byte array back to a string
            var parsedString = Encoding.UTF8.GetString(outputBytes);

            return parsedString.Split('|');
        }
    }
}
