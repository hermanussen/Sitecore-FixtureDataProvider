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
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sitecore;
using Sitecore.Collections;
using Sitecore.Common;
using Sitecore.Configuration;
using Sitecore.Data;
using Sitecore.Data.Events;
using Sitecore.Data.Fields;
using Sitecore.Data.Items;
using Sitecore.Security.Accounts;
using Sitecore.Sites;
using Sitecore.Web;

namespace FixtureDataProvider.Test
{
    /// <summary>
    ///     Base class that sets up the Sitecore configuration and provides some helper methods.
    /// </summary>
    public abstract class SitecoreUnitTestBase
    {
        private static Stack<EventDisablerState> eventDisablerStack;
        private static Stack<bool> cacheDisablerStack;
        private static Stack<User> userStack;

        /// <summary>
        ///     Constructor that sets the context database to 'master' and sets up a basic sitecontext.
        /// </summary>
        protected SitecoreUnitTestBase()
        {
            Context.Database = Factory.GetDatabase("master");
            Context.Site = new SiteContext(new SiteInfo(new StringDictionary()));
        }

        /// <summary>
        ///     Gets or sets the test context which provides
        ///     information about and functionality for the current test run.
        /// </summary>
        public TestContext TestContext { get; set; }

        /// <summary>
        ///     Disables events, database cache and sets a user context (sitecore\admin)
        /// </summary>
        /// <param name="testContext"></param>
        [ClassInitialize]
        public static void Initialize(TestContext testContext)
        {
            eventDisablerStack = new Stack<EventDisablerState>();
            eventDisablerStack.Push(EventDisablerState.Disabled);
            Context.Items[Switcher<EventDisablerState, EventDisabler>.ItemsKey] = eventDisablerStack;

            cacheDisablerStack = new Stack<bool>();
            cacheDisablerStack.Push(true);
            Context.Items[Switcher<bool, DatabaseCacheDisabler>.ItemsKey] = cacheDisablerStack;

            userStack = new Stack<User>();
            userStack.Push(User.FromName(@"sitecore\admin", true));
            Context.Items[Switcher<User>.ItemsKey] = userStack;
        }

        [ClassCleanup]
        public static void Cleanup()
        {
            eventDisablerStack.Pop();
            cacheDisablerStack.Pop();
            userStack.Pop();
        }

        /// <summary>
        ///     Helper method that logs the Sitecore tree structure starting from the item that is passed.
        /// </summary>
        /// <param name="item"></param>
        /// <param name="level">Level; for indentation</param>
        public static void LogTreeStructure(Item item, int level = 0)
        {
            Console.WriteLine("{0}>{1}", new string(' ', level*2), item.Name);
            // LogItemFields(item, level);
            foreach (Item child in item.GetChildren())
            {
                LogTreeStructure(child, level + 1);
            }
        }

        /// <summary>
        ///     Helper method that logs the field contents of an item that is passed.
        /// </summary>
        /// <param name="item"></param>
        /// <param name="level">Level; for indentation</param>
        public static void LogItemFields(Item item, int level = 0)
        {
            foreach (Field field in item.Fields)
            {
                Console.WriteLine("{0}  ({1}={2})", new string(' ', level*2), field.Name, field.Value);
            }
        }
    }
}