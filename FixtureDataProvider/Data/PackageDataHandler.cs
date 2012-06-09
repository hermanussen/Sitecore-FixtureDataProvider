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
using System.Linq;
using System.Text;
using Sitecore.Data.Serialization.ObjectModel;
using Sitecore.SecurityModel;
using Sitecore.Data.Proxies;
using Sitecore.Data.Engines;
using Sitecore.Install.Framework;
using Sitecore.Install.Items;
using Sitecore.Install;
using Sitecore.Install.Utils;
using Sitecore.Install.Zip;
using Sitecore;
using Sitecore.Install.Metadata;
using Sitecore.Zip;
using System.IO;
using Sitecore.Xml;
using System.Xml;
using Sitecore.Data;
using Sitecore.Data.Items;
using Sitecore.Globalization;

namespace FixtureDataProvider.Data
{
    /// <summary>
    /// Loads fixture data items from a Sitecore package.
    /// </summary>
    public class PackageDataHandler : IDataHandler
    {
        private string PackagePath { get; set; }

        public PackageDataHandler(string packagePath)
        {
            PackagePath = packagePath;
        }

        public List<SyncItem> LoadItems()
        {
            List<SyncItem> items = new List<SyncItem>();

            using (new SecurityDisabler())
            {
                using (new ProxyDisabler())
                {
                    ZipReader reader = new ZipReader(PackagePath, Encoding.UTF8);
                    ZipEntry entry = reader.GetEntry("package.zip");

                    using (MemoryStream stream = new MemoryStream())
                    {
                        StreamUtil.Copy(entry.GetStream(), stream, 0x4000);

                        reader = new ZipReader(stream);

                        foreach (ZipEntry zipEntry in reader.Entries)
                        {
                            ZipEntryData entryData = new ZipEntryData(zipEntry);
                            try
                            {
                                if (entryData.Key.EndsWith("/xml"))
                                {
                                    string xml = new StreamReader(entryData.GetStream().Stream, Encoding.UTF8).ReadToEnd();
                                    if (!string.IsNullOrWhiteSpace(xml))
                                    {
                                        XmlDocument document = XmlUtil.LoadXml(xml);
                                        if (document != null)
                                        {
                                            SyncItem loadedItem = LoadItem(document);
                                            if (loadedItem != null)
                                            {
                                                items.Add(loadedItem);
                                            }
                                        }
                                    }
                                }
                            }
                            catch (Exception exc)
                            {
                                Console.WriteLine(string.Format("Unable to load xml from file {0}", entryData.Key));
                            }
                        }
                    }
                }
            }

            Console.WriteLine(string.Format("Read {0} items from package {1}", items.Count, PackagePath));

            return items;
        }

        private static SyncItem LoadItem(XmlDocument document)
        {
            SyncItem loadedItem = new SyncItem();

            XmlNode itemNode = document.DocumentElement;
            loadedItem.ID = XmlUtil.GetAttribute("id", itemNode);
            loadedItem.Name = XmlUtil.GetAttribute("name", itemNode);
            loadedItem.ParentID = XmlUtil.GetAttribute("parentid", itemNode);
            loadedItem.TemplateID = XmlUtil.GetAttribute("tid", itemNode);
            loadedItem.MasterID = XmlUtil.GetAttribute("mid", itemNode);
            loadedItem.BranchId = XmlUtil.GetAttribute("bid", itemNode);
            loadedItem.TemplateName = XmlUtil.GetAttribute("template", itemNode);

            SyncVersion loadedVersion = loadedItem.AddVersion(
                XmlUtil.GetAttribute("language", itemNode),
                XmlUtil.GetAttribute("version", itemNode),
                string.Empty);

            foreach (XmlNode node in itemNode.SelectNodes("fields/field"))
            {
                XmlNode content = node.SelectSingleNode("content");
                loadedVersion.AddField(
                    XmlUtil.GetAttribute("tfid", node),
                    XmlUtil.GetAttribute("key", node),
                    XmlUtil.GetAttribute("key", node),
                    content != null ? XmlUtil.GetValue(content) : null,
                    content != null);
            }
            return loadedItem;
        }
    }
}
