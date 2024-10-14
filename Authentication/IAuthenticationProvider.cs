using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Authentication
{
    /// <summary>
    /// Interface that must be implemented by an external provider
    /// such as an AD or LDAP service. Authenticates users, and provides
    /// the lists of roles a particular user has.
    /// </summary>
    
    public interface IAuthenticationProvider
    {
        /// <summary>
        /// Given a user's credentials validate these credentials.
        /// If valid, return a list of roles of which this user
        /// is a member. If invalid, return null. Note that
        /// an empty list returned means the user was authenticated
        /// successfully, but has no specific roles assigned.
        /// </summary>
        /// <param name="name">The user name</param>
        /// <param name="pass">The user's password</param>
        /// <returns>The list of roles this user is a
        /// member of, or null if not authenticated</returns>
        
        IList<string>? RolesForUser(string name, string pass);
    }
}
