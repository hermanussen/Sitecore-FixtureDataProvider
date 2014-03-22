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

using Sitecore.Security.AccessControl;
using Sitecore.Security.Accounts;

namespace FixtureDataProvider.Mocking
{
    /// <summary>
    ///     An authorization provider that allows anything.
    ///     Intended for use in unit tests that do not require to test security aspects.
    /// </summary>
    public class AlwaysAllowAuthorizationProvider : AuthorizationProvider
    {
        protected override AccessResult GetAccessCore(ISecurable entity, Account account, AccessRight accessRight)
        {
            return new AccessResult(AccessPermission.Allow,
                new AccessExplanation("Always allow authorization provider used"));
        }

        public override AccessRuleCollection GetAccessRules(ISecurable entity)
        {
            return new AccessRuleCollection();
        }

        public override void SetAccessRules(ISecurable entity, AccessRuleCollection rules)
        {
        }
    }
}