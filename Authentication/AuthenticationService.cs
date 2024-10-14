using Microsoft.Extensions.Configuration;
using System.Text;

namespace Authentication
{
    /// <summary>
    /// This class is a placeholder for an Authentication HTTP client.
    /// The interface is correct, but the rest of this is implemented
    /// as a simple example of a proper JWT authentication service.
    /// </summary>
    
    public class AuthenticationService : IAuthenticationService
    {
        private readonly byte[] jwtSecretKey;
        private readonly IAuthenticationProvider authProvider;
        public AuthenticationService(IConfiguration config, IAuthenticationProvider provider)
        {
            authProvider = provider;
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
            IList<string>? roles = authProvider.RolesForUser(name, pass);
            if (roles != null) // We provided valid credentials
            {
                string jwtToken = JwtTokenManager
                    .BuildTokenForUser(name, name, roles, jwtSecretKey);
                return new AuthenticationResponse { JwtToken = jwtToken };
            }
            return null;
        }
    }
}
