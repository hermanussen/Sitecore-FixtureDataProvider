using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SampleSitecoreProject;
using FixtureDataProvider.Test;
using System.Configuration;
using Sitecore.Data.Items;
using System.Xml;

namespace SampleTestProject
{
    /// <summary>
    /// A sample unit test that utilizes the Sitecore Content API
    /// </summary>
    [TestClass]
    public class UnitTest : SitecoreUnitTestBase
    {

        [TestMethod]
        [Description("Tests the import of a KML file")]
        public void TestKmlImport()
        {
            string sampleKmlFile = ConfigurationManager.AppSettings["samplekml"];
            DateTime timeStamp = DateTime.Today;
            SampleSitecoreLogic.ImportKml(sampleKmlFile, timeStamp);

            // Check if the import root node is available
            Item imported = Sitecore.Context.Database.GetItem(string.Format("/sitecore/content/Imported content {0}", timeStamp.ToString("yyyy MM dd")));

            Assert.IsNotNull(imported, "Import root item could not be found");

            // Check if all 35 placemarks in the KML file were imported
            Assert.AreEqual(35, imported.Children.Count, "Not the right amount of imported placemarks found");

            // Check if all values are filled
            foreach (Item child in imported.Children)
            {
                Assert.IsFalse(string.IsNullOrWhiteSpace(child["Description"]), string.Format("Description field empty for {0}", child.Paths.FullPath));
                Assert.IsFalse(string.IsNullOrWhiteSpace(child["Longitude"]), string.Format("Longitude field empty for {0}", child.Paths.FullPath));
                Assert.IsFalse(string.IsNullOrWhiteSpace(child["Latitude"]), string.Format("Latitude field empty for {0}", child.Paths.FullPath));
                Assert.IsFalse(string.IsNullOrWhiteSpace(child["Altitude"]), string.Format("Altitude field empty for {0}", child.Paths.FullPath));
            }

            // Check the contents of an individual item
            Item muiderSlotItem = imported.Axes.GetChild("Muiderslot");
            Assert.IsNotNull(muiderSlotItem, "Castle 'Muiderslot' was not found after import");
            Assert.IsTrue(muiderSlotItem["Description"].Contains("Muiderslot<br>Country: Netherlands<br>Region: Noord Holland<br>Place: Muiden"),
                "Castle 'Muiderslot' does not have a correct description");
            Assert.AreEqual("5.0718055556", muiderSlotItem["Longitude"]);
            Assert.AreEqual("52.33423055599999", muiderSlotItem["Latitude"]);
            Assert.AreEqual("0", muiderSlotItem["Altitude"]);

            Console.WriteLine(string.Format("=== SITECORE TREE STRUCTURE DUMP (for debugging) ==="));
            LogTreeStructure(Sitecore.Context.Database.GetRootItem());
        }

        [TestMethod]
        [Description("Tests the import of a KML file that is not valid XML; the expected behaviour is to encounter an XmlException")]
        public void TestKmlImportUnparseable()
        {
            string sampleKmlFile = ConfigurationManager.AppSettings["samplekml_unparseable"];
            DateTime timeStamp = DateTime.Today;
            try
            {
                SampleSitecoreLogic.ImportKml(sampleKmlFile, timeStamp);
                Assert.Fail("An exception was expected here");
            }
            catch (XmlException exc)
            {
                Assert.AreEqual("'<' is an unexpected token. The expected token is '>'. Line 291, position 7.", exc.Message);
            }
        }
    }
}
