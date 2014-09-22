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
using System.Text;
using System.Xml;
using Sitecore.Data.Proxies;
using Sitecore.Data.Serialization.ObjectModel;
using Sitecore.Install;
using Sitecore.Install.Zip;
using Sitecore.SecurityModel;
using Sitecore.Xml;
using Sitecore.Zip;

namespace FixtureDataProvider.Data
{
    /// <summary>
    ///     Loads fixture data items from a Sitecore package.
    /// </summary>
    public class PackageDataHandler : IDataHandler
    {
        public PackageDataHandler(string packagePath)
        {
            PackagePath = packagePath;
        }

        private string PackagePath { get; set; }

        public List<SyncItem> LoadItems()
        {
            var items = new List<SyncItem>();

            using (new SecurityDisabler())
            {
                using (new ProxyDisabler())
                {
                    var reader = new ZipReader(PackagePath, Encoding.UTF8);
                    ZipEntry entry = reader.GetEntry("package.zip");

                    using (var stream = new MemoryStream())
                    {
                        StreamUtil.Copy(entry.GetStream(), stream, 0x4000);

                        reader = new ZipReader(stream);

                        foreach (ZipEntry zipEntry in reader.Entries)
                        {
                            var entryData = new ZipEntryData(zipEntry);
                            try
                            {
                                if (entryData.Key.EndsWith("/xml"))
                                {
                                    string xml =
                                        new StreamReader(entryData.GetStream().Stream, Encoding.UTF8).ReadToEnd();
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
                            catch (Exception)
                            {
                                Console.WriteLine("Unable to load xml from file {0}", entryData.Key);
                            }
                        }
                    }
                }
            }

            Console.WriteLine("Read {0} items from package {1}", items.Count, PackagePath);

            return items;
        }

        private static SyncItem LoadItem(XmlDocument document)
        {
            var loadedItem = new SyncItem();

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