using System.Web;
using BlockFarmEditor.Umbraco.Core.Interfaces;
using BlockFarmEditor.Umbraco.Core.Models;
using BlockFarmEditor.Umbraco.Core.Models.BuilderModels;
using Microsoft.Extensions.Caching.Memory;
using Umbraco.Cms.Core.Models.PublishedContent;
using Umbraco.Cms.Core.Security;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Core.Web;

namespace BlockFarmEditor.Umbraco.Library.Services
{
    // Scoped Service
    internal class BlockFarmEditorContext(
        IUmbracoContextAccessor umbracoContextAccessor, 
        IMemberManager memberManager,
        IPublicAccessService publicAccessService,
        IMemoryCache memoryCache) : IBlockFarmEditorContext
    {        
        public const string BlockFarmEditorKey = "blockFarmEditor";
        public const string BlockFarmEditorEditorAlias = "blockfarmeditor_page_propertyeditor";
        private bool _isEditMode = false;
        private bool _isPreview = false;
        public bool IsEditMode => _isEditMode;

        public bool IsPreview => _isPreview;

        public Guid ContentUnique { get; set; } = Guid.Empty;

        public PageDefinition? PageDefinition { get; set; }

        public BlockFarmEditorContainerScope? _currentScope { get; set; } = null;

        public BlockFarmEditorContainerScope? GetBlockScope() => _currentScope ?? new BlockFarmEditorContainerScope(this, PageDefinition!, _currentScope);

        public BlockFarmEditorContainerScope? GetBlockScope(IContainerDefinition block) => _currentScope = new BlockFarmEditorContainerScope(this, block, _currentScope);

        /// <summary>
        /// Sets the page definition for the current context using the published context.
        /// </summary>
        public async Task SetPageDefinition()
        {
            if (umbracoContextAccessor.TryGetUmbracoContext(out IUmbracoContext? context))
            {
                var query = HttpUtility.ParseQueryString(context.OriginalRequestUrl.Query);
                var editmode = query.Get("editmode");
                bool isEditMode = editmode == "true";
                var content = context.PublishedRequest?.PublishedContent;
                var domain = (context.PublishedRequest?.Domain?.Uri ?? context.CleanedUmbracoUrl).Host;
                _isPreview = context.InPreviewMode;

                await SetPageDefinition(context, content, domain, context.PublishedRequest?.Culture, isEditMode, context.InPreviewMode);
            }
        }

        private async Task<IPublishedContent?> ValidateMemberAccessAsync(IPublishedContent? content, IUmbracoContext context)
        {
            if (content == null)
            {
                return content;
            }

            var hasPermissions = publicAccessService.IsProtected(content.Path);

            if((hasPermissions.Result?.HasIdentity ?? false) == false) {
                return content;
            }

            // Check permissions only for authenticated users
            var currentMember = await memberManager.GetCurrentMemberAsync();

            if (currentMember == null)
            {
                // User is unauthorized - redirect to error page
                var loginId = hasPermissions.Result.LoginNodeId;
                // override the edit mode to false
                _isEditMode = false;
                return context.Content?.GetById(loginId);
            }

            // Generate cache key from user ID and content ID
            var userId = currentMember.Key.ToString();
            var cacheKey = $"BlockFarmEditor_Permission_{userId}_{content.Key}";

            // Check cache first, create if not exists
            var hasAccess = await memoryCache.GetOrCreateAsync(cacheKey, async entry =>
            {
                entry.SlidingExpiration = TimeSpan.FromMinutes(5); // Cache expires after 5 min of inactivity
                entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1); // Max 1 hour cache
                
                return await publicAccessService.HasAccessAsync(content.Path, currentMember.UserName ?? string.Empty, async () => await memberManager.GetRolesAsync(currentMember));
            });

            if (!hasAccess)
            {
                // override the edit mode to false
                _isEditMode = false;
                // User is unauthorized - redirect to error page
                var errorPageId = hasPermissions.Result.NoAccessNodeId;
                var errorPage = context.Content?.GetById(errorPageId);

                if (errorPage != null)
                {
                    // Set the error page as the content for this request
                    return errorPage;
                }
                else
                {
                    // No error page configured - set content to null to prevent rendering
                    return null;
                }
            }

            return content;
        }

        public async Task SetPageDefinition(IUmbracoContext context, IPublishedContent? content, string? domain, string? culture = null, bool editMode = false, bool preview = false)
        {
            if (content != null)
            {
                ContentUnique = content.Key;
                _isEditMode = editMode;
                if(content.HasProperty(BlockFarmEditorEditorAlias))
                {
                    content = await ValidateMemberAccessAsync(content, context);
                    if (content == null)
                    {
                        PageDefinition = new PageDefinition();
                        return;
                    }

                    var value = content.Value(BlockFarmEditorEditorAlias, culture);

                    if (value is PageDefinition pageDefinition)
                    {
                        PageDefinition = pageDefinition;
                    }
                    else
                    {
                        PageDefinition = new PageDefinition();
                    }
                }
                else
                {
                    PageDefinition = new PageDefinition();
                }
            }
        }
    }
}
