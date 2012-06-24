using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Sitecore.Data.Items;
using System.Xml.Linq;
using System.IO;
using Sitecore.Data;
using Sitecore.Resources.Media;

namespace SampleSitecoreProject
{
    public class SampleSitecoreLogic
    {
        public static void ImportKml(string kmlFilePath, DateTime timeStamp)
        {
            XDocument kml = XDocument.Load(kmlFilePath);
            ImportKml(kml, timeStamp);
        }

        /// <summary>
        /// Takes KML XML input and creates some Sitecore items with the 'Placemark' template.
        /// </summary>
        /// <param name="kmlDocument"></param>
        /// <param name="timeStamp">Current date, to be used for the Sitecore folder name under which imported items are placed</param>
        public static void ImportKml(XDocument kmlDocument, DateTime timeStamp)
        {
            Item folderTemplate = Sitecore.Context.Database.GetItem(Sitecore.TemplateIDs.Folder);
            Item placemarkTemplate = Sitecore.Context.Database.GetItem("/sitecore/templates/User Defined/Kml/Placemark");
            Item contentRoot = Sitecore.Context.Database.GetItem(Sitecore.ItemIDs.ContentRoot);

            Item importRoot = contentRoot.Add(string.Format("Imported content {0}", timeStamp.ToString("yyyy MM dd")), new TemplateItem(folderTemplate));

            const string kmlNamespace = "http://www.opengis.net/kml/2.2";

            foreach (XElement placemarkElement in kmlDocument.Descendants(XName.Get("Placemark", kmlNamespace)))
            {
                XElement name = placemarkElement.Element(XName.Get("name", kmlNamespace));
                XElement description = placemarkElement.Element(XName.Get("description", kmlNamespace));
                XElement point = placemarkElement.Element(XName.Get("Point", kmlNamespace));

                if (name != null && ! string.IsNullOrWhiteSpace(name.Value))
                {
                    Item placemarkItem = importRoot.Add(ItemUtil.ProposeValidItemName(name.Value.Replace(":","")), new TemplateItem(placemarkTemplate));
                    using (new EditContext(placemarkItem))
                    {
                        if (description != null && !string.IsNullOrWhiteSpace(description.Value))
                        {
                            placemarkItem["Description"] = description.Value;
                        }
                        if (point != null)
                        {
                            XElement coordinates = point.Element(XName.Get("coordinates", kmlNamespace));
                            if (coordinates != null && !string.IsNullOrWhiteSpace(coordinates.Value))
                            {
                                string[] splitCoordinates = coordinates.Value.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                                if (splitCoordinates.Length > 0)
                                {
                                    placemarkItem["Longitude"] = splitCoordinates[0];
                                }
                                if (splitCoordinates.Length > 1)
                                {
                                    placemarkItem["Latitude"] = splitCoordinates[1];
                                }
                                if (splitCoordinates.Length > 2)
                                {
                                    placemarkItem["Altitude"] = splitCoordinates[2];
                                }
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Imports an image into the root of the Sitecore media library
        /// </summary>
        /// <param name="sampleImage"></param>
        public static void ImportImage(string sampleImage)
        {
            FileInfo imageFile = new FileInfo(sampleImage);
            Item parentItem = Sitecore.Context.Database.GetItem("/sitecore/media library");
            
            var mediaCreatorOptions = new MediaCreatorOptions();
            mediaCreatorOptions.Database = Sitecore.Context.Database;
            mediaCreatorOptions.Language = Sitecore.Context.Language;
            mediaCreatorOptions.Versioned = false;
            mediaCreatorOptions.Destination = string.Format("{0}/{1}", parentItem.Paths.FullPath, ItemUtil.ProposeValidItemName(Path.GetFileNameWithoutExtension(sampleImage)));
            mediaCreatorOptions.FileBased = Sitecore.Configuration.Settings.Media.UploadAsFiles;

            var mc = new MediaCreator();
            mc.CreateFromFile(sampleImage, mediaCreatorOptions);
        }
    }
}
