﻿
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

using Umbraco.Core;
using Umbraco.Core.Composing;
using Umbraco.Core.Models;

using uSync.BackOffice;
using uSync.BackOffice.Models;
using uSync.ContentEdition.Mapping;
using uSync.ContentEdition.Serializers;
using uSync.Core;
using uSync.Core.Serialization;

namespace uSync.ContentEdition
{
    [JsonObject(NamingStrategyType = typeof(DefaultNamingStrategy))]
    public class uSyncContent : ISyncAddOn
    {
        public string Name => "Content Edition";
        public string Version => "8.0.1";

        /// The following if you are an add on that displays like an app
        
        // but content edition doesn't have an interface, so the view is empty. this hides it. 
        public string View => string.Empty;
        public string Icon => "icon-globe";
        public string Alias => "Content";
        public string DisplayName => "Content";

        public int SortOrder => 10;

        public static int DependencyCountMax = 204800;
    }

    [ComposeAfter(typeof(uSyncCoreComposer))]
    [ComposeBefore(typeof(uSyncBackOfficeComposer))]
    public class uSyncContentComposer : IUserComposer
    {
        public uSyncContentComposer()
        {
        }

        public void Compose(Composition composition)
        {
            composition.RegisterUnique<SyncValueMapperFactory>();

            composition.Register<ISyncSerializer<IContent>, ContentSerializer>();
            composition.Register<ContentTemplateSerializer>();
            composition.Register<ISyncSerializer<IMedia>, MediaSerializer>();
            composition.Register<ISyncSerializer<IDictionaryItem>, DictionaryItemSerializer>();
            composition.Register<ISyncSerializer<IDomain>, DomainSerializer>();
            composition.Register<ISyncSerializer<IRelationType>, RelationTypeSerializer>();

            composition.WithCollectionBuilder<SyncValueMapperCollectionBuilder>()
                .Add(composition.TypeLoader.GetTypes<ISyncMapper>());
        }
    }
}