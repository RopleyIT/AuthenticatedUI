using Microsoft.Extensions.Configuration;
using System.Text;

namespace Authentication
{
    /// <summary>
    /// This class is a placeholder for an Authentication HTTP client.
    /// The interface is correct, but the rest of this is implemented
    /// as a local mockup of a proper JWT authentication service.
    /// </summary>
    public class AuthenticationService : IAuthenticationService
    {
        private readonly byte[] jwtSecretKey;

        public AuthenticationService(IConfiguration config)
        {
            string? key = config["JWTSecretKey"] 
                ?? throw new ArgumentException("Config contains no JWT secret key");
            jwtSecretKey = Encoding.ASCII.GetBytes(key);
        }

        /// <summary>
        /// Given a user name and password in an AuthenticationRequest
        /// object, go and find the JWT token. If the user is authenticated
        /// return their token complete with its claims. If not, return
        /// a null object reference.
        /// </summary>
        /// <param name="authenticationRequest">The user name and password</param>
        /// <returns>A JWT token wrapped in an AuthenticationResponse object
        /// or null if not authenticated</returns>
        public AuthenticationResponse? Authenticate(AuthenticationRequest authenticationRequest)
        {
            ArgumentNullException.ThrowIfNull(authenticationRequest, nameof(authenticationRequest));

            string name = authenticationRequest.UserName;
            string pass = authenticationRequest.Password;
            if (IsAuthenticatedUser(name, pass))
            {
                string jwtToken = JwtTokenManager
                    .BuildTokenForUser(name, pass, jwtSecretKey);
                return new AuthenticationResponse { JwtToken = jwtToken };
            }
            return null;
        }

        // The following code should be replaced by something
        // that goes to a real authentication provider, such as
        // an Active Directory LDAP provider or similar

        private static bool IsAuthenticatedUser(string name, string pass)
        {
            return pass == name + "pw";
        }
    }
}
