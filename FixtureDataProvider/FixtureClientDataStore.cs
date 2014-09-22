using System;
using System.Globalization;
using Sitecore;
using Sitecore.Configuration;

namespace FixtureDataProvider
{
    public class FixtureClientDataStore : ClientDataStore
    {
        public FixtureClientDataStore(string connectionString, string objectLifetime)
            : base(ParseTimeSpan(objectLifetime))
        {
        }

        protected override void CompactData()
        {
            // noop
        }

        protected override string LoadData(string key)
        {
            return null;
        }

        protected override void SaveData(string key, string data)
        {
            // noop
        }

        protected override void RemoveData(string key)
        {
            // noop
        }

        private static TimeSpan ParseTimeSpan(string objectLifetime)
        {
            return DateUtil.ParseTimeSpan(objectLifetime, TimeSpan.Zero, CultureInfo.CurrentCulture);
        }
    }
}