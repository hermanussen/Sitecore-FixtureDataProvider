/*
    FixtureDataProvider Sitecore module
    Copyright (C) 2012  Robin Hermanussen

    This program is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    This program is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with this program.  If not, see <http://www.gnu.org/licenses/>.
*/

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using FixtureDataProvider.Data;
using Sitecore;
using Sitecore.Collections;
using Sitecore.Data;
using Sitecore.Data.DataProviders;
using Sitecore.Data.Items;
using Sitecore.Data.Managers;
using Sitecore.Data.Serialization.ObjectModel;
using Sitecore.Diagnostics;
using Sitecore.Globalization;
using Version = Sitecore.Data.Version;

namespace FixtureDataProvider
{
    /// <summary>
    ///     An in-memory data provider for Sitecore that can read fixtures on startup and keeps track of changes in Sitecore as
    ///     long as the program runs.
    /// </summary>
    public class FixtureDataProvider : DataProvider
    {
        /// <summary>
        ///     Creates a new fixture data provider and reads the data into it.
        /// </summary>
        /// <param name="sources">
        ///     Pipe separated paths to directories containing serialized data, directories containing TDS items
        ///     or individual package files (zip)
        /// </param>
        public FixtureDataProvider(string sources)
        {
//            Assert.IsNotNullOrEmpty(sources, "You must provide sources to load items from");

            ItemsById = new Dictionary<ID, SyncItem>();
            ItemsByParentId = new List<KeyValuePair<ID, SyncItem>>();

            Blobs = new Dictionary<Guid, byte[]>();

            DataHandlers = new List<IDataHandler>();

            if (string.IsNullOrEmpty(sources)) return;

            string[] sourcesArray = sources.Split(new[] {'|'}, StringSplitOptions.RemoveEmptyEntries);
            foreach (string source in sourcesArray)
            {
                if (source.EndsWith(".zip"))
                {
                    // Add package data handler
                    DataHandlers.Add(new PackageDataHandler(source));
                }
                else
                {
                    // Add serialized/TDS data handler
                    DataHandlers.Add(new SerializedDataHandler(source));
                }
            }

            LoadItems();
        }

        public IDictionary<ID, SyncItem> ItemsById { get; private set; }
        protected List<KeyValuePair<ID, SyncItem>> ItemsByParentId { get; private set; }
        protected List<IDataHandler> DataHandlers { get; private set; }

        protected IDictionary<Guid, byte[]> Blobs { get; private set; }

        /// <summary>
        ///     Returns a definition containing the id, name, template id, branch id and parent id of the Item that corresponds
        ///     with the itemId parameter.
        /// </summary>
        /// <param name="itemId">The item id to search for</param>
        /// <param name="context"></param>
        /// <returns></returns>
        public override ItemDefinition GetItemDefinition(ID itemId, CallContext context)
        {
            if (ItemsById.ContainsKey(itemId))
            {
                var data = new PrefetchData(
                    new ItemDefinition(
                        itemId,
                        ItemsById[itemId].Name,
                        ParseId(ItemsById[itemId].TemplateID) ?? ID.Null,
                        ParseId(ItemsById[itemId].BranchId) ?? ID.Null),
                    ParseId(ItemsById[itemId].ParentID) ?? ID.Null);
                if (data != null)
                {
                    return data.ItemDefinition;
                }
            }
            return null;
        }

        /// <summary>
        ///     Get a list of all available versions in different languages.
        /// </summary>
        /// <param name="itemDefinition">Used to identify the particular item</param>
        /// <param name="context"></param>
        /// <returns></returns>
        public override VersionUriList GetItemVersions(ItemDefinition itemDefinition, CallContext context)
        {
            if (ItemsById.ContainsKey(itemDefinition.ID)
                && ItemsById[itemDefinition.ID].Versions != null)
            {
                var versionsList = new List<VersionUri>();
                foreach (SyncVersion version in ItemsById[itemDefinition.ID].Versions)
                {
                    var newVersionUri = new VersionUri(
                        LanguageManager.GetLanguage(version.Language),
                        new Version(version.Version));
                    versionsList.Add(newVersionUri);
                }

                var versions = new VersionUriList();
                foreach (VersionUri version in versionsList)
                {
                    versions.Add(version);
                }

                return versions;
            }
            return null;
        }

        /// <summary>
        ///     Get a list of all the item's fields and their values.
        /// </summary>
        /// <param name="itemDefinition">Used to identify the particular item</param>
        /// <param name="versionUri">The language and version of the item to get field values for</param>
        /// <param name="context"></param>
        /// <returns></returns>
        public override FieldList GetItemFields(ItemDefinition itemDefinition, VersionUri versionUri,
            CallContext context)
        {
            Language language = versionUri.Language;
            if (Language.Invariant.Equals(language))
            {
                language = LanguageManager.DefaultLanguage;
            }
            if (ItemsById.ContainsKey(itemDefinition.ID))
            {
                var fields = new FieldList();
                foreach (SyncField sharedField in ItemsById[itemDefinition.ID].SharedFields)
                {
                    fields.Add(ParseId(sharedField.FieldID), sharedField.FieldValue);
                }

                if (ItemsById[itemDefinition.ID].Versions != null)
                {
                    foreach (SyncVersion version in ItemsById[itemDefinition.ID].Versions)
                    {
                        if (language.Name.Equals(version.Language)
                            && versionUri.Version.Number.ToString().Equals(version.Version))
                        {
                            foreach (SyncField fieldValue in version.Fields)
                            {
                                fields.Add(ParseId(fieldValue.FieldID), fieldValue.FieldValue);
                            }
                            break;
                        }
                    }
                }

                return fields;
            }
            return null;
        }

        /// <summary>
        ///     Determines what items are children of the item and returns a list of their IDs.
        /// </summary>
        /// <param name="itemDefinition">Used to identify the particular item</param>
        /// <param name="context"></param>
        /// <returns></returns>
        public override IDList GetChildIDs(ItemDefinition itemDefinition, CallContext context)
        {
            IEnumerable<KeyValuePair<ID, SyncItem>> childItems =
                ItemsByParentId.Where(item => item.Key == itemDefinition.ID);
            return IDList.Build(childItems.Select(item => ParseId(item.Value.ID)).ToArray());
        }

        /// <summary>
        ///     Get the ID of the parent of an item.
        /// </summary>
        /// <param name="itemDefinition">Used to identify the particular item</param>
        /// <param name="context"></param>
        /// <returns></returns>
        public override ID GetParentID(ItemDefinition itemDefinition, CallContext context)
        {
            if (ItemsById.ContainsKey(itemDefinition.ID)
                && ItemsById[itemDefinition.ID].Versions != null)
            {
                return string.IsNullOrWhiteSpace(ItemsById[itemDefinition.ID].ParentID)
                    ? null
                    : ParseId(ItemsById[itemDefinition.ID].ParentID);
            }
            return null;
        }

        /// <summary>
        ///     Create a new item as a child of another item.
        ///     Note that this does not create any versions or field values.
        /// </summary>
        /// <param name="itemID">The item ID (not the parent's)</param>
        /// <param name="itemName">The name of the new item</param>
        /// <param name="templateID">The ID of the content item that represents its template</param>
        /// <param name="parent">The parent item's definition</param>
        /// <param name="context"></param>
        /// <returns></returns>
        public override bool CreateItem(ID itemID, string itemName, ID templateID, ItemDefinition parent,
            CallContext context)
        {
            if (ItemsById.ContainsKey(itemID))
            {
                // item already exists
                return false;
            }

            if (parent != null)
            {
                if (! ItemsById.ContainsKey(parent.ID))
                {
                    // parent item does not exist in this provider
                    return false;
                }
            }

            var newItem = new SyncItem
            {
                ID = GetIdAsString(itemID),
                Name = itemName,
                TemplateID = GetIdAsString(templateID),
                ParentID = GetIdAsString(parent != null ? parent.ID : new ID(Guid.Empty)),
                ItemPath =
                    parent == null
                        ? string.Format("/{0}", itemName)
                        : string.Format("{0}/{1}", GetItemPath(parent.ID), itemName),
                BranchId = GetIdAsString(new ID(Guid.Empty)),
                DatabaseName = Database.Name
            };

            AddItem(newItem);

            AddVersion(new ItemDefinition(itemID, itemName, templateID, new ID(Guid.Empty)),
                new VersionUri(Language.Invariant, null), context);

            return true;
        }

        /// <summary>
        ///     Changes the in-memory data when an item is moved to a different position in the tree.
        /// </summary>
        /// <param name="itemDefinition"></param>
        /// <param name="destination"></param>
        /// <param name="context"></param>
        /// <returns></returns>
        public override bool MoveItem(ItemDefinition itemDefinition, ItemDefinition destination, CallContext context)
        {
            if (! ItemsById.ContainsKey(itemDefinition.ID) || ! ItemsById.ContainsKey(destination.ID))
            {
                return false;
            }

            SyncItem item = ItemsById[itemDefinition.ID];
            ID parentId = ParseId(item.ParentID);
            SyncItem destinationIt = ItemsById[destination.ID];

            ItemsByParentId.RemoveAll(pair => pair.Key == parentId && pair.Value == item);
            ItemsByParentId.Add(new KeyValuePair<ID, SyncItem>(destination.ID, item));
            item.ItemPath = ItemsById.ContainsKey(parentId)
                ? string.Format("{0}/{1}", GetItemPath(parentId), item.Name)
                : string.Format("/{0}", item.Name);

            return true;
        }

        /// <summary>
        ///     Creates a new version for a content item in a particular language.
        /// </summary>
        /// <param name="itemDefinition">Used to identify the particular item</param>
        /// <param name="baseVersion">The version to copy off of</param>
        /// <param name="context"></param>
        /// <returns></returns>
        public override int AddVersion(ItemDefinition itemDefinition, VersionUri baseVersion, CallContext context)
        {
            if (! ItemsById.ContainsKey(itemDefinition.ID))
            {
                return -1;
            }
            SyncItem current = ItemsById[itemDefinition.ID];

            int num = -1;

            Language baseVersionLanguage = baseVersion.Language;
            if (Language.Invariant.Equals(baseVersionLanguage))
            {
                baseVersionLanguage = LanguageManager.DefaultLanguage;
            }

            if (baseVersion.Version != null && baseVersion.Version.Number > 0)
            {
                // copy version
                SyncVersion matchingVersion = (current.Versions ?? new List<SyncVersion>())
                    .OrderByDescending(vr => int.Parse(vr.Version))
                    .FirstOrDefault(vr => vr.Language.Equals(baseVersionLanguage.Name));

                int? maxVersionNumber = matchingVersion != null ? int.Parse(matchingVersion.Version) : null as int?;
                num = maxVersionNumber.HasValue && maxVersionNumber > 0 ? maxVersionNumber.Value + 1 : -1;

                if (num > 0)
                {
                    SyncVersion newVersion = current.AddVersion(matchingVersion.Language, num.ToString(),
                        matchingVersion.Revision);

                    IList<SyncField> currentFieldValues = matchingVersion.Fields;

                    foreach (SyncField fieldValue in currentFieldValues)
                    {
                        newVersion.AddField(fieldValue.FieldID, fieldValue.FieldName, fieldValue.FieldKey,
                            fieldValue.FieldValue, true);
                    }
                }
            }
            if (num == -1)
            {
                num = 1;

                // add blank version
                current.AddVersion(baseVersionLanguage.Name, num.ToString(), Guid.NewGuid().ToString());
            }

            return num;
        }

        /// <summary>
        ///     Removes an item from the database completely.
        /// </summary>
        /// <param name="itemDefinition">Used to identify the particular item</param>
        /// <param name="context"></param>
        /// <returns></returns>
        public override bool DeleteItem(ItemDefinition itemDefinition, CallContext context)
        {
            if (ItemsById.ContainsKey(itemDefinition.ID))
            {
                // recursively remove descendants
                foreach (var child in ItemsByParentId.Where(item => item.Key == itemDefinition.ID))
                {
                    DeleteItem(
                        new ItemDefinition(ParseId(child.Value.ID), child.Value.Name, ParseId(child.Value.TemplateID),
                            ParseId(child.Value.BranchId)), context);
                }

                // remove the item
                ItemsById.Remove(itemDefinition.ID);
                foreach (
                    var remove in
                        ItemsByParentId.Where(
                            it => it.Value != null && it.Value.ID.Equals(GetIdAsString(itemDefinition.ID))).ToList())
                {
                    ItemsByParentId.Remove(remove);
                }
                return true;
            }

            return false;
        }

        /// <summary>
        ///     Save changes that were made to an item to the database.
        /// </summary>
        /// <param name="itemDefinition">Used to identify the particular item</param>
        /// <param name="changes">A holder object that keeps track of the changes</param>
        /// <param name="context"></param>
        /// <returns></returns>
        public override bool SaveItem(ItemDefinition itemDefinition, ItemChanges changes, CallContext context)
        {
            if (! ItemsById.ContainsKey(itemDefinition.ID))
            {
                return false;
            }

            SyncItem current = ItemsById[itemDefinition.ID];
            if (changes.HasPropertiesChanged)
            {
                current.Name = StringUtil.GetString(changes.GetPropertyValue("name"), itemDefinition.Name);

                var templateId =
                    MainUtil.GetObject(changes.GetPropertyValue("templateid"), itemDefinition.TemplateID) as ID;
                current.TemplateID = templateId != ID.Null ? GetIdAsString(templateId) : null;

                var branchId = MainUtil.GetObject(changes.GetPropertyValue("branchid"), itemDefinition.BranchId) as ID;
                current.BranchId = branchId != ID.Null ? GetIdAsString(branchId) : null;
            }
            if (changes.HasFieldsChanged)
            {
                foreach (FieldChange change in changes.FieldChanges)
                {
                    string changeFieldId = GetIdAsString(change.FieldID);
                    IEnumerable<SyncField> matchingSharedFields =
                        current.SharedFields.Where(fv => changeFieldId.Equals(fv.FieldID));
                    IEnumerable<SyncVersion> matchingVersions = current.Versions
                        .Where(
                            vr =>
                                vr.Version.Equals(change.Version.Number.ToString()) &&
                                vr.Language.Equals(change.Language.Name));
                    var matchingNonSharedFields = matchingVersions
                        .SelectMany(vr => vr.Fields.Select(fl => new {Ver = vr, Field = fl}))
                        .Where(fv => changeFieldId.Equals(fv.Field.FieldID));
                    if (change.RemoveField)
                    {
                        if (matchingSharedFields.Any())
                        {
                            current.SharedFields.Remove(matchingSharedFields.First());
                        }
                        if (matchingNonSharedFields.Any())
                        {
                            matchingNonSharedFields.First()
                                .Ver.RemoveField(matchingNonSharedFields.First().Field.FieldName);
                        }
                    }
                    else
                    {
                        bool changeMade = false;
                        if (matchingSharedFields.Any())
                        {
                            matchingSharedFields.First().FieldValue = change.Value;
                            changeMade = true;
                        }
                        if (matchingNonSharedFields.Any())
                        {
                            matchingNonSharedFields.First().Field.FieldValue = change.Value;
                            changeMade = true;
                        }
                        if (! changeMade && change.Definition != null)
                        {
                            if (change.Definition.IsShared || change.Definition.IsUnversioned)
                            {
                                current.AddSharedField(changeFieldId, change.Definition.Name, change.Definition.Key,
                                    change.Value, true);
                            }
                            else if (matchingVersions.Any())
                            {
                                matchingVersions.First()
                                    .AddField(changeFieldId, change.Definition.Name, change.Definition.Key, change.Value,
                                        true);
                            }
                        }
                    }
                }
            }
            return true;
        }

        public override IdCollection GetTemplateItemIds(CallContext context)
        {
            var ids = new IdCollection();
            foreach (
                ID id in
                    ItemsById.Values.Where(it => TemplateIDs.Template == ParseId(it.TemplateID))
                        .Select(it => ParseId(it.ID)))
            {
                ids.Add(id);
            }
            return ids;
        }

        public override ID GetRootID(CallContext context)
        {
            return ItemIDs.RootID;
        }

        /// <summary>
        ///     Check if blob data (like media library contents) is available.
        /// </summary>
        /// <param name="blobId"></param>
        /// <param name="context"></param>
        /// <returns></returns>
        public override bool BlobStreamExists(Guid blobId, CallContext context)
        {
            return Blobs.ContainsKey(blobId);
        }

        /// <summary>
        ///     Get the binary blob data (like media library contents).
        /// </summary>
        /// <param name="blobId"></param>
        /// <param name="context"></param>
        /// <returns></returns>
        public override Stream GetBlobStream(Guid blobId, CallContext context)
        {
            if (Blobs.ContainsKey(blobId))
            {
                return new MemoryStream(Blobs[blobId]);
            }
            return null;
        }

        /// <summary>
        ///     Set the binary blob data (like media library contents).
        /// </summary>
        /// <param name="stream"></param>
        /// <param name="blobId"></param>
        /// <param name="context"></param>
        /// <returns></returns>
        public override bool SetBlobStream(Stream stream, Guid blobId, CallContext context)
        {
            var memoryStream = new MemoryStream();
            stream.CopyTo(memoryStream);
            Blobs[blobId] = memoryStream.ToArray();
            return true;
        }

        /// <summary>
        ///     Adds an item to the in-memory set of items.
        /// </summary>
        /// <param name="syncItem"></param>
        protected void AddItem(SyncItem syncItem)
        {
            ID itemId = ParseId(syncItem.ID);
            if (!ItemsById.ContainsKey(itemId))
            {
                ItemsById.Add(itemId, syncItem);
                ItemsByParentId.Add(new KeyValuePair<ID, SyncItem>(ParseId(syncItem.ParentID), syncItem));
            }
        }

        /// <summary>
        ///     Clear all data and reload from the handlers.
        /// </summary>
        private void LoadItems()
        {
            ItemsById.Clear();
            ItemsByParentId.Clear();

            foreach (IDataHandler dataHandler in DataHandlers)
            {
                foreach (SyncItem item in dataHandler.LoadItems())
                {
                    AddItem(item);
                }
            }
        }

        private string GetItemPath(ID id)
        {
            return ItemsById.ContainsKey(id)
                ? string.Format("{0}/{1}", GetItemPath(ParseId(ItemsById[id].ParentID)), ItemsById[id].Name)
                : string.Empty;
        }

        private ID ParseId(string id)
        {
            return !string.IsNullOrWhiteSpace(id) && ID.IsID(id)
                ? new ID(id)
                : null;
        }

        private string GetIdAsString(ID id, bool stripHooks = false)
        {
            return stripHooks ? id.ToString().Substring(1, id.ToString().Length - 2) : id.ToString();
        }
    }
}