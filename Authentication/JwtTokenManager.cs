using System.Security.Claims;
using Microsoft.IdentityModel.Tokens;
using Microsoft.IdentityModel.JsonWebTokens;

namespace Authentication
{

    public static class JwtTokenManager
    {
        private static readonly string issuer = "https://Abbott.com/"; //Identifies the issuer of the token
        private static readonly string audience = "https://Abbott.com/"; //Identifies the recipient of the token

        public static string BuildTokenForUser(string userName, string givenName, byte[] key)
        {
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new[]
                {
                    new Claim("Id", Guid.NewGuid().ToString()),
                    new Claim(JwtRegisteredClaimNames.Name, userName),
                    new Claim(JwtRegisteredClaimNames.GivenName, givenName)
                }),
                Expires = DateTime.UtcNow.AddMinutes(45),
                Issuer = issuer,
                Audience = audience,
                SigningCredentials = new SigningCredentials
                    (new SymmetricSecurityKey(key), 
                    SecurityAlgorithms.HmacSha512Signature)
            };
            return (new JsonWebTokenHandler()).CreateToken(tokenDescriptor);
        }
    }
}
