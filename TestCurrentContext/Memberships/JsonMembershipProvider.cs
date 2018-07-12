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
    public class JsonMembershipProvider : MembershipProvider
    {
        private readonly object _lockObj = new object();

        private IDictionary<string, MembershipUser> _users;
        private string _jsonFileName;
        private ILogger _logger;

        // MembershipProvider Properties
        public override string ApplicationName
        {
            get { throw new NotSupportedException(); }
            set { throw new NotSupportedException(); }
        }

        public override bool EnablePasswordRetrieval => false;

        public override bool EnablePasswordReset => false;

        // MembershipProvider Methods
        public override void Initialize(string name, NameValueCollection config)
        {
            _logger = Log.Logger.ForContext<JsonMembershipProvider>();

            try
            {
                // Assign the provider a default name if it doesn't have one
                if (string.IsNullOrEmpty(name))
                {
                    name = nameof(JsonMembershipProvider);
                }

                // Add a default "description" attribute to config if the
                // attribute doesn't exist or is empty
                if (string.IsNullOrEmpty(config["description"]))
                {
                    config.Remove("description");
                    config.Add("description", "Varner Integration Hub membership provider");
                }

                // Call the base class's Initialize method
                base.Initialize(name, config);

                // Initialize _jsonFileName and make sure the path
                // is app-relative
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

                // Make sure we have permission to read the JSON data source and throw an exception if we don't
                FileIOPermission permission = new FileIOPermission(FileIOPermissionAccess.Read, _jsonFileName);
                permission.Demand();

                // Throw an exception if unrecognised attributes remain
                if (config.Count > 0)
                {
                    string attr = config.GetKey(0);
                    if (!string.IsNullOrEmpty(attr))
                    {
                        throw new ProviderException("Unrecognised attribute: " + attr);
                    }
                }
            }
            catch (Exception e)
            {
                _logger.Error(e, e.Message);
                throw;
            }
        }

        public override bool ValidateUser(string username, string password)
        {
            // Validate input parameters
            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password)) return false;

            // Make sure the data source has been loaded
            ReadMembershipDataStore();
            // Validate the user name and password
            MembershipUser user;
            if (_users.TryGetValue(username, out user))
            {
                if (user.Comment == password) // Case-sensitive
                {
                    // NOTE: A read/write membership provider
                    // would update the user's LastLoginDate here.
                    // A fully featured provider would also fire
                    // an AuditMembershipAuthenticationSuccess
                    // Web event
                    return true;
                }
            }

            _logger.Debug("Failed to validate user {UserName}", username);

            // NOTE: A fully featured membership provider would
            // fire an AuditMembershipAuthenticationFailure
            // Web event here
            return false;
        }

        public override MembershipUser GetUser(string username, bool userIsOnline)
        {
            // Note: This implementation ignores userIsOnline
            // Validate input parameters
            if (string.IsNullOrEmpty(username)) return null;

            // Make sure the data source has been loaded
            ReadMembershipDataStore();
            // Retrieve the user from the data source
            MembershipUser user;
            if (_users.TryGetValue(username, out user))
            {
                return user;
            }

            _logger.Debug("Failed to get user {UserName}", username);
 
            return null;
        }

        public override MembershipUserCollection GetAllUsers(int pageIndex, int pageSize, out int totalRecords)
        {
            // Note: This implementation ignores pageIndex and pageSize,
            // and it doesn't sort the MembershipUser objects returned
            // Make sure the data source has been loaded
            ReadMembershipDataStore();
            var users = new MembershipUserCollection();
            foreach (var pair in _users)
            {
                users.Add(pair.Value);
            }

            totalRecords = users.Count;
            return users;
        }

        #region Unimplemented overrides

        public override int MaxInvalidPasswordAttempts
        {
            get { throw new NotSupportedException(); }
        }

        public override int MinRequiredNonAlphanumericCharacters
        {
            get { throw new NotSupportedException(); }
        }

        public override int MinRequiredPasswordLength
        {
            get { throw new NotSupportedException(); }
        }

        public override int PasswordAttemptWindow
        {
            get { throw new NotSupportedException(); }
        }

        public override MembershipPasswordFormat PasswordFormat
        {
            get { throw new NotSupportedException(); }
        }

        public override string PasswordStrengthRegularExpression
        {
            get { throw new NotSupportedException(); }
        }

        public override bool RequiresQuestionAndAnswer
        {
            get { throw new NotSupportedException(); }
        }

        public override bool RequiresUniqueEmail
        {
            get { throw new NotSupportedException(); }
        }

        public override int GetNumberOfUsersOnline()
        {
            throw new NotSupportedException();
        }

        public override bool ChangePassword(string username, string oldPassword, string newPassword)
        {
            throw new NotSupportedException();
        }

        public override bool ChangePasswordQuestionAndAnswer(string username, string password, string newPasswordQuestion, string newPasswordAnswer)
        {
            throw new NotSupportedException();
        }

        public override MembershipUser CreateUser(string username, string password, string email, string passwordQuestion, string passwordAnswer, bool isApproved, object providerUserKey, out MembershipCreateStatus status)
        {
            throw new NotSupportedException();
        }

        public override bool DeleteUser(string username, bool deleteAllRelatedData)
        {
            throw new NotSupportedException();
        }

        public override MembershipUserCollection FindUsersByEmail(string emailToMatch, int pageIndex, int pageSize, out int totalRecords)
        {
            throw new NotSupportedException();
        }

        public override MembershipUserCollection FindUsersByName(string usernameToMatch, int pageIndex, int pageSize, out int totalRecords)
        {
            throw new NotSupportedException();
        }

        public override string GetPassword(string username, string answer)
        {
            throw new NotSupportedException();
        }

        public override MembershipUser GetUser(object providerUserKey, bool userIsOnline)
        {
            throw new NotSupportedException();
        }

        public override string GetUserNameByEmail(string email)
        {
            throw new NotSupportedException();
        }

        public override string ResetPassword(string username, string answer)
        {
            throw new NotSupportedException();
        }

        public override bool UnlockUser(string userName)
        {
            throw new NotSupportedException();
        }

        public override void UpdateUser(MembershipUser user)
        {
            throw new NotSupportedException();
        }

        #endregion

        // Helper method
        private void ReadMembershipDataStore()
        {
            lock (_lockObj)
            {
                try
                {
                    _users = new Dictionary<string, MembershipUser>(16, StringComparer.InvariantCultureIgnoreCase);

                    var users = JsonWrapper.Deserialize<IList<User>>(File.OpenRead(_jsonFileName));

                    foreach (var user in users)
                    {
                        var membershipUser = new MembershipUser(
                           Name,                       // Provider name
                           user.UserName,              // Username
                           null,                       // providerUserKey
                           string.Empty,               // Email
                           string.Empty,               // passwordQuestion
                           user.Password,              // Comment
                           true,                       // isApproved
                           false,                      // isLockedOut
                           DateTime.Now,               // creationDate
                           DateTime.Now,               // lastLoginDate
                           DateTime.Now,               // lastActivityDate
                           DateTime.Now,               // lastPasswordChangedDate
                           new DateTime(1980, 1, 1)    // lastLockoutDate
                        );
                        _users.Add(user.UserName, membershipUser);
                    }
                }
                catch (Exception e)
                {
                    _logger.Error(e, e.Message);
                    throw;
                }
            }
        }
    }
}