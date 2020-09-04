﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

using Umbraco.Core;
using Umbraco.Core.Logging;
using Umbraco.Core.Models;
using Umbraco.Core.Models.Entities;
using Umbraco.Core.Services;
using Umbraco.Core.Strings;

using uSync.Core.Extensions;
using uSync.Core.Models;

namespace uSync.Core.Serialization.Serializers
{
    [SyncSerializer("F45B5C7B-C206-4971-858B-6D349E153ACE", "MemberTypeSerializer", uSyncConstants.Serialization.MemberType)]
    public class MemberTypeSerializer : ContentTypeBaseSerializer<IMemberType>, ISyncOptionsSerializer<IMemberType>
    {
        private readonly IMemberTypeService memberTypeService;

        public MemberTypeSerializer(
            IEntityService entityService, ILogger logger,
            IDataTypeService dataTypeService,
            IMemberTypeService memberTypeService,
            IShortStringHelper shortStringHelper)
            : base(entityService, logger, dataTypeService, memberTypeService, shortStringHelper, UmbracoObjectTypes.Unknown)
        {
            this.memberTypeService = memberTypeService;
        }

        protected override SyncAttempt<XElement> SerializeCore(IMemberType item, SyncSerializerOptions options)
        {
            var node = SerializeBase(item);
            var info = SerializeInfo(item);

            var parent = item.ContentTypeComposition.FirstOrDefault(x => x.Id == item.ParentId);
            if (parent != null)
            {
                info.Add(new XElement("Parent", parent.Alias,
                    new XAttribute("Key", parent.Key)));
            }
            else if (item.Level != 1)
            {
                // in a folder
                var folderNode = GetFolderNode(memberTypeService.GetContainers(item));
                if (folderNode != null)
                    info.Add(folderNode);
            }

            info.Add(SerializeCompostions((ContentTypeCompositionBase)item));

            node.Add(info);
            node.Add(SerializeProperties(item));
            node.Add(SerializeStructure(item));
            node.Add(SerializeTabs(item));

            return SyncAttempt<XElement>.Succeed(item.Name, node, typeof(IMediaType), ChangeType.Export);

        }

        protected override void SerializeExtraProperties(XElement node, IMemberType item, PropertyType property)
        {
            node.Add(new XElement("CanEdit", item.MemberCanEditProperty(property.Alias)));
            node.Add(new XElement("CanView", item.MemberCanViewProperty(property.Alias)));
            node.Add(new XElement("IsSensitive", item.IsSensitiveProperty(property.Alias)));
        }

        //
        // for the member type, the built in properties are created with guid's that are really int values
        // as a result the Key value you get back for them, can change between reboots. 
        //
        // here we tag on to the SerializeProperties step, and blank the Key value for any of the built in 
        // properties. 
        //
        //   this means we don't get false posistives between reboots, 
        //   it also means that these properties won't get deleted if/when they are removed - but 
        //   we limit it only to these items by listing them (so custom items in a member type will still
        //   get removed when required. 
        // 

        private static string[] buildInProperties = new string[] {
            "umbracoMemberApproved", "umbracoMemberComments", "umbracoMemberFailedPasswordAttempts",
            "umbracoMemberLastLockoutDate", "umbracoMemberLastLogin", "umbracoMemberLastPasswordChangeDate",
            "umbracoMemberLockedOut", "umbracoMemberPasswordRetrievalAnswer", "umbracoMemberPasswordRetrievalQuestion"
        };

        protected override XElement SerializeProperties(IMemberType item)
        {
            var node = base.SerializeProperties(item);
            foreach (var property in node.Elements("GenericProperty"))
            {
                var alias = property.Element("Alias").ValueOrDefault(string.Empty);
                if (!string.IsNullOrWhiteSpace(alias) && buildInProperties.InvariantContains(alias))
                {
                    property.Element("Key").Value = Guid.Empty.ToString();
                }
            }
            return node;
        }

        protected override SyncAttempt<IMemberType> DeserializeCore(XElement node, SyncSerializerOptions options)
        {
            var attempt = FindOrCreate(node);
            if (!attempt.Success)
                throw attempt.Exception;

            var item = attempt.Result;

            var details = new List<uSyncChange>();

            details.AddRange(DeserializeBase(item, node));
            details.AddRange(DeserializeTabs(item, node));
            details.AddRange(DeserializeProperties(item, node));

            CleanTabs(item, node);

            // memberTypeService.Save(item);

            return SyncAttempt<IMemberType>.Succeed(item.Name, item, ChangeType.Import, details);
        }

        protected override IEnumerable<uSyncChange> DeserializeExtraProperties(IMemberType item, PropertyType property, XElement node)
        {
            var changes = new List<uSyncChange>();

            var canEdit = node.Element("CanEdit").ValueOrDefault(false);
            if (item.MemberCanEditProperty(property.Alias) != canEdit)
            {
                changes.AddUpdate("CanEdit", !canEdit, canEdit, $"{property.Alias}/CanEdit");
                item.SetMemberCanEditProperty(property.Alias, canEdit);
            }

            var canView = node.Element("CanView").ValueOrDefault(false);
            if (item.MemberCanViewProperty(property.Alias) != canView)
            {
                changes.AddUpdate("CanView", !canView, canView, $"{property.Alias}/CanView");
                item.SetMemberCanViewProperty(property.Alias, canView);
            }

            var isSensitive = node.Element("IsSensitive").ValueOrDefault(true);
            if (item.IsSensitiveProperty(property.Alias) != isSensitive)
            {
                changes.AddUpdate("IsSensitive", !isSensitive, isSensitive, $"{property.Alias}/IsSensitive");
                item.SetIsSensitiveProperty(property.Alias, isSensitive);
            }

            return changes;
        }

        protected override Attempt<IMemberType> CreateItem(string alias, ITreeEntity parent, string extra)
        {
            var item = new MemberType(shortStringHelper, -1)
            {
                Alias = alias
            };

            if (parent != null)
            {
                if (parent is IMediaType mediaTypeParent)
                    item.AddContentType(mediaTypeParent);

                item.SetParent(parent);
            }


            return Attempt.Succeed((IMemberType)item);
        }
    }
}