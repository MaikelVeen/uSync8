﻿using System;

using Umbraco.Core;
using Umbraco.Core.Cache;
using Umbraco.Core.Logging;
using Umbraco.Core.Models;
using Umbraco.Core.Models.Entities;
using Umbraco.Core.Services;
using Umbraco.Core.Services.Implement;
using Umbraco.Core.Strings;

using uSync.BackOffice.Configuration;
using uSync.BackOffice.Services;
using uSync.Core;
using uSync.Core.Dependency;
using uSync.Core.Serialization;
using uSync.Core.Tracking;

using static Umbraco.Core.Constants;

namespace uSync.BackOffice.SyncHandlers.Handlers
{
    [SyncHandler("contentTypeHandler", "DocTypes", "ContentTypes", uSyncBackOfficeConstants.Priorites.ContentTypes,
            IsTwoPass = true, Icon = "icon-item-arrangement", EntityType = UdiEntityType.DocumentType)]
    public class ContentTypeHandler : SyncHandlerContainerBase<IContentType, IContentTypeService>, ISyncExtendedHandler, ISyncPostImportHandler
    {
        private readonly IContentTypeService contentTypeService;

        public ContentTypeHandler(
            IShortStringHelper shortStringHelper,
            IContentTypeService contentTypeService,
            IEntityService entityService,
            IProfilingLogger logger,
            AppCaches appCaches,
            ISyncSerializer<IContentType> serializer,
            ISyncItemFactory syncItemFactory,
            SyncFileService syncFileService)
            : base(shortStringHelper, entityService, logger, appCaches, serializer, syncItemFactory, syncFileService)
        {
            this.contentTypeService = contentTypeService;
        }


        #region Import
        // default import behavior is in the base class.
        #endregion

        #region Export
        // most of export is now in the base 


        #endregion


        protected override void InitializeEvents(HandlerSettings settings)
        {
            ContentTypeService.Saved += EventSavedItem;
            ContentTypeService.Deleted += EventDeletedItem;
            ContentTypeService.Moved += EventMovedItem;

            ContentTypeService.SavedContainer += EventContainerSaved;
        }

        protected override string GetItemFileName(IUmbracoEntity item, bool useGuid)
        {
            if (useGuid) return item.Key.ToString();

            if (item is IContentType contentItem)
            {
                return contentItem.Alias.ToSafeFileName(shortStringHelper);
            }

            return item.Name.ToSafeFileName(shortStringHelper);
        }

        protected override IContentType GetFromService(int id)
            => contentTypeService.Get(id);

        protected override IContentType GetFromService(Guid key)
            => contentTypeService.Get(key);

        protected override IContentType GetFromService(string alias)
            => contentTypeService.Get(alias);

        protected override IEntity GetContainer(int id)
            => contentTypeService.GetContainer(id);

        protected override IEntity GetContainer(Guid key)
            => contentTypeService.GetContainer(key);

        protected override void DeleteFolder(int id)
            => contentTypeService.DeleteContainer(id);

        protected override void DeleteViaService(IContentType item)
            => contentTypeService.Delete(item);

        protected override string GetItemAlias(IContentType item)
            => item.Alias;
    }
}