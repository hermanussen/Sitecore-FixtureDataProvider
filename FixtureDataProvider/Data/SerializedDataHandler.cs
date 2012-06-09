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
using Sitecore.Diagnostics;
using System.IO;
using Sitecore.Install;
using Sitecore.Install.Serialization;
using Sitecore.IO;
using Sitecore.Data.Serialization.ObjectModel;
using Sitecore.Data;

namespace FixtureDataProvider.Data
{
    /// <summary>
    /// Loads fixture data items from a folder where items have been serialized (.item files).
    /// </summary>
    public class SerializedDataHandler : IDataHandler
    {
        protected DirectoryInfo StartInfo { get; set; }

        public SerializedDataHandler(string startPath)
        {
            Assert.IsNotNullOrEmpty(startPath, "Please provide a path on the filesystem where serialized items are");
            StartInfo = new DirectoryInfo(startPath);
            Assert.IsTrue(StartInfo.Exists, string.Format("The path {0} could not be found", startPath));
        }

        public List<SyncItem> LoadItems()
        {
            List<SyncItem> items = new List<SyncItem>();

            foreach (FileInfo itemFile in StartInfo.GetFiles("*.item", SearchOption.AllDirectories))
            {
                try
                {
                    items.Add(SyncItem.ReadItem(new Tokenizer(itemFile.OpenText())));
                }
                catch (Exception exception)
                {
                    Console.WriteLine(string.Format("Unable to read item from file {0}: {1}", itemFile.FullName, exception.Message));
                }
            }

            Console.WriteLine(string.Format("Deserialized {0} items from {1}", items.Count, StartInfo.FullName));

            return items;
        }
    }
}
