using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Authentication
{
    /// <summary>
    /// A dummy placeholder for an authentication and role provider.
    /// Note that the IAuthenticationProvider interface documents
    /// what the member function(s) should do.
    /// </summary>
    
    public class DummyAuthenticationProvider : IAuthenticationProvider
    {
        public IList<string>? RolesForUser(string name, string pass)
        {
            if(pass == name + "pw")
            {
                if(name == "admin")
                    return ["admin", "subadmin", "user"];
                if (name == "subadmin")
                    return ["subadmin", "user"];
                return ["user"];
            }
            return null;
        }
    }
}
