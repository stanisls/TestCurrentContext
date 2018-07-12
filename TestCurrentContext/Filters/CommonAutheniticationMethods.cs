using System;
using System.Security.Claims;
using System.Security.Principal;
using System.Text;
using System.Web.Security;
using TestCurrentContext.Memberships;

namespace TestCurrentContext.Filters
{
    internal static class CommonAutheniticationMethods
    {
        internal static IPrincipal Authenticate(string userName, string password)
        {
            if (string.IsNullOrWhiteSpace(userName) || string.IsNullOrWhiteSpace(password))
            {
                return null;
            }

            var membershipProvider = Membership.Providers[nameof(JsonMembershipProvider)];
            if (membershipProvider != null && membershipProvider.ValidateUser(userName, password))
            {
                ClaimsIdentity identity = new GenericIdentity(userName, "Basic");
                return new RolePrincipal(nameof(JsonRoleProvider), identity);
            }

            return null;
        }


        internal static Tuple<string, string> ExtractUserNameAndPassword(string authorizationParameter)
        {
            byte[] credentialBytes;

            try
            {
                credentialBytes = Convert.FromBase64String(authorizationParameter);
            }
            catch (FormatException)
            {
                return null;
            }

            // The currently approved HTTP 1.1 specification says characters here are ISO-8859-1.
            // However, the current draft updated specification for HTTP 1.1 indicates this encoding is infrequently
            // used in practice and defines behaviour only for ASCII.
            Encoding encoding = Encoding.ASCII;
            // Make a writeable copy of the encoding to enable setting a decoder fallback.
            encoding = (Encoding)encoding.Clone();
            // Fail on invalid bytes rather than silently replacing and continuing.
            encoding.DecoderFallback = DecoderFallback.ExceptionFallback;
            string decodedCredentials;

            try
            {
                decodedCredentials = encoding.GetString(credentialBytes);
            }
            catch (DecoderFallbackException)
            {
                return null;
            }

            if (string.IsNullOrEmpty(decodedCredentials))
            {
                return null;
            }

            int colonIndex = decodedCredentials.IndexOf(':');

            if (colonIndex == -1)
            {
                return null;
            }

            string userName = decodedCredentials.Substring(0, colonIndex);
            string password = decodedCredentials.Substring(colonIndex + 1);
            return new Tuple<string, string>(userName, password);
        }
    }
}
