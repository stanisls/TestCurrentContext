#pragma warning disable 1591
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Configuration.Provider;
using System.IO;
using System.Security.Permissions;
using System.Web;
using System.Web.Hosting;
using System.Web.Security;
using Serilog;

namespace TestCurrentContext.Memberships
{
    public class JsonRoleProvider : RoleProvider
    {
        private IDictionary<string, string[]> _usersAndRoles;
        private IDictionary<string, string[]> _rolesAndUsers;

        private string _jsonFileName;
        private ILogger _logger;

        private readonly object _lockObj = new object();

        // RoleProvider properties
        public override string ApplicationName
        {
            get { throw new NotSupportedException(); }
            set { throw new NotSupportedException(); }
        }
        
        // RoleProvider methods
        public override void Initialize(string name, NameValueCollection config)
        {
            _logger = Log.Logger.ForContext<JsonRoleProvider>();

            try
            {
                // Assign the provider a default name if it doesn't have one
                if (string.IsNullOrEmpty(name))
                {
                    name = nameof(JsonRoleProvider);
                }

                // Add a default "description" attribute to config if the
                // attribute doesn't exist or is empty
                if (string.IsNullOrEmpty(config["description"]))
                {
                    config.Remove("description");
                    config.Add("description", "Varner Integration Hub role provider");
                }

                // Call the base class's Initialize method
                base.Initialize(name, config);

                // Initialize _jsonFileName and make sure the path is app-relative
                var path = config["fileName"];
                if (string.IsNullOrEmpty(path))
                {
                    path = "~/App_Data/Users.json";
                }

                if (!VirtualPathUtility.IsAppRelative(path))
                {
                    throw new ArgumentException("fileName must be app-relative");
                }

                var fullyQualifiedPath = VirtualPathUtility.Combine(VirtualPathUtility.AppendTrailingSlash(HttpRuntime.AppDomainAppVirtualPath), path);
                _jsonFileName = HostingEnvironment.MapPath(fullyQualifiedPath);
                config.Remove("fileName");

                // Make sure we have permission to read the XML data source and
                // throw an exception if we don't
                FileIOPermission permission = new FileIOPermission(FileIOPermissionAccess.Read, _jsonFileName);
                permission.Demand();
                // Throw an exception if unrecognised attributes remain
                if (config.Count > 0)
                {
                    var attr = config.GetKey(0);
                    if (!string.IsNullOrEmpty(attr))
                    {
                        throw new ProviderException("Unrecognised attribute: " + attr);
                    }
                }

                ReadRoleDataStore();
            }
            catch (Exception e)
            {
                _logger.Error(e, e.Message);
                throw;
            }
        }

        public override bool IsUserInRole(string userName, string roleName)
        {
            try
            {
                // Validate input parameters
                if (string.IsNullOrWhiteSpace(userName) || string.IsNullOrWhiteSpace(roleName))
                {
                    _logger.Error("Empty userName or roleName");
                    return false;
                }

                ReadRoleDataStore();

                // Make sure the user name and role name are valid
                if (!_usersAndRoles.ContainsKey(userName))
                {
                    _logger.Error("Invalid user name {UserName}", userName);
                    return false;
                }

                if (!_rolesAndUsers.ContainsKey(roleName))
                {
                    _logger.Error("Invalid role name {RoleName}", roleName);
                    return false;
                }

                // Determine whether the user is in the specified role 
                var roles = _usersAndRoles[userName];
                foreach (string role in roles)
                {
                    if (string.Compare(role, roleName, StringComparison.OrdinalIgnoreCase) == 0)
                    {
                        return true;
                    }
                }

                return false;
            }
            catch (Exception e)
            {
                _logger.Error(e, e.Message);
                throw;
            }
        }

        public override string[] GetRolesForUser(string userName)
        {
            try
            {
                // Validate input parameters
                if (string.IsNullOrWhiteSpace(userName))
                {
                    _logger.Error("No userName is provided.");
                    return new string[0];
                }

                ReadRoleDataStore();

                // Make sure the user name is valid
                string[] roles;
                if (!_usersAndRoles.TryGetValue(userName, out roles))
                {
                    _logger.Error("Invalid user name {UserName}", userName);
                    return new string[0];
                }

                // Return role names
                return roles;
            }
            catch (Exception e)
            {
                _logger.Error(e, e.Message);
                throw;
            }
        }

        public override string[] GetUsersInRole(string roleName)
        {
            try
            {
                // Validate input parameters
                if (string.IsNullOrWhiteSpace(roleName))
                {
                    _logger.Error("No roleName is provided.");
                    return new string[0];
                }

                ReadRoleDataStore();

                // Make sure the role name is valid
                string[] users;
                if (!_rolesAndUsers.TryGetValue(roleName, out users))
                {
                    _logger.Error("Invalid role name {RoleName}", roleName);
                    return new string[0];
                }
                // Return user names
                return users;
            }
            catch (Exception e)
            {
                _logger.Error(e, e.Message);
                throw;
            }
        }

        public override string[] GetAllRoles()
        {
            try
            {
                ReadRoleDataStore();

                var i = 0;
                var roles = new string[_rolesAndUsers.Count];
                foreach (var pair in _rolesAndUsers)
                {
                    roles[i++] = pair.Key;
                }

                return roles;
            }
            catch (Exception e)
            {
                _logger.Error(e, e.Message);
                throw;
            }
        }

        public override bool RoleExists(string roleName)
        {
            try
            {
                // Validate input parameters
                if (string.IsNullOrWhiteSpace(roleName))
                {
                    _logger.Error("No roleName is provided.");
                    return false;
                }

                ReadRoleDataStore();

                // Determine whether the role exists
                return _rolesAndUsers.ContainsKey(roleName);
            }
            catch (Exception e)
            {
                _logger.Error(e, e.Message);
                throw;
            }
        }

        #region Unimplemented overrides

        public override void CreateRole(string roleName)
        {
            throw new NotSupportedException();
        }

        public override bool DeleteRole(string roleName, bool throwOnPopulatedRole)
        {
            throw new NotSupportedException();
        }

        public override void AddUsersToRoles(string[] usernames, string[] roleNames)
        {
            throw new NotSupportedException();
        }

        public override string[] FindUsersInRole(string roleName, string usernameToMatch)
        {
            throw new NotSupportedException();
        }

        public override void RemoveUsersFromRoles(string[] usernames, string[] roleNames)
        {
            throw new NotSupportedException();
        }

        #endregion

        // Helper method
        private void ReadRoleDataStore()
        {
            lock (_lockObj)
            {
                _usersAndRoles = new Dictionary<string, string[]>(16, StringComparer.InvariantCultureIgnoreCase);
                _rolesAndUsers = new Dictionary<string, string[]>(16, StringComparer.InvariantCultureIgnoreCase);

                var users = JsonWrapper.Deserialize<List<User>>(File.OpenRead(_jsonFileName));

                foreach (var user in users)
                {
                    if (string.IsNullOrEmpty(user.UserName))
                    {
                        throw new ProviderException("Empty UserName element");
                    }

                    if (user.Roles == null || user.Roles.Length == 0)
                    {
                        _usersAndRoles.Add(user.UserName, new string[0]);
                    }
                    else
                    {
                        _usersAndRoles.Add(user.UserName, user.Roles);
                        foreach (var role in user.Roles)
                        {
                            // Add the user name to _RolesAndUsers and
                            // key it by role names
                            string[] users1;
                            if (_rolesAndUsers.TryGetValue(role, out users1))
                            {
                                string[] users2 = new string[users1.Length + 1];
                                users1.CopyTo(users2, 0);
                                users2[users1.Length] = user.UserName;
                                _rolesAndUsers.Remove(role);
                                _rolesAndUsers.Add(role, users2);
                            }
                            else
                            {
                                _rolesAndUsers.Add(role, new[] { user.UserName });
                            }
                        }
                    }
                }
            }
        }
    }
}