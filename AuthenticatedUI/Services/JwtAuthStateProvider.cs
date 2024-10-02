using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Server.ProtectedBrowserStorage;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;
using Authentication;
using System.Text;

namespace AuthenticatedUI.Services;

public class JwtAuthStateProvider : AuthenticationStateProvider
{
    /// <summary>
    /// The state returned whenever there is nobody
    /// logged in, or the JWT token is invalid
    /// </summary>
    private static readonly AuthenticationState anonUser
        = new(new ClaimsPrincipal(new ClaimsIdentity()));

    /// <summary>
    /// Stores the current JWT token in the browser's local storage. Will
    /// only work after the SignalR connection has been established.
    /// </summary>
    private readonly ProtectedSessionStorage sessionStorage;
    private readonly IAuthenticationService authService;
    private readonly byte[] jwtSecretKey;
    private string? currentToken;
    private bool connectionEstablished;

    /// <summary>
    /// Constructor. Manages UI authentication state using a JWT token
    /// retrieved from an authentication service upon provision of
    /// valid user name and password. Protected session storage is
    /// used in the client browser so that:
    /// (a) The authentication token is remembered for the current
    ///     tab, not the whole browser window.
    /// (b) The loss of the tab and subsequent reconnection to the
    ///     same server application URLs will continue the session
    ///     if performed before the JwtToken expires.
    /// (c) The encryption and decryption of the stored item takes
    ///     place on the server end of the SignalR connection, not
    ///     in JavaScript on the client, meaning the token is safe
    ///     to leave in the browser storage.
    /// </summary>
    /// <param name="browserStorage">The injected reference to a
    /// protected browser storage service</param>
    public JwtAuthStateProvider
        (ProtectedSessionStorage browserStorage, IConfiguration config,
        Authentication.IAuthenticationService authSvc)
    {
        connectionEstablished = false;
        currentToken = null;
        sessionStorage = browserStorage;
        authService = authSvc;
        string? key = config["JWTSecretKey"] 
            ?? throw new ArgumentException("Config contains no JWT secret key");
        jwtSecretKey = Encoding.ASCII.GetBytes(key);
    }

    /// <summary>
    /// Attempt to log this user into the application. If
    /// successful, store the access token and notify the
    /// authorization subsystem that the user has changed.
    /// </summary>
    /// <param name="userName">The user's typed login name</param>
    /// <param name="password">The user's typed password</param>
    /// <returns>True if logged in successfully, 
    /// false if not</returns>
    public async Task<bool> Login(AuthenticationRequest? authRequest)
    {
        if (authRequest == null)
            return false;

        var authResponse = authService.Authenticate(authRequest);
        if (authResponse == null)
            return false;

        currentToken = authResponse.JwtToken;
        if(connectionEstablished)
            await sessionStorage.SetAsync("jwttoken", authResponse.JwtToken);
        await AuthenticateUser(authResponse.JwtToken!);
        return true;
    }

    /// <summary>
    /// Remove the JWT token from session storage
    /// and notify the authorization subsystem that
    /// the user has changed to the anonymous user.
    /// </summary>
    public async Task Logout()
    {
        currentToken = null;
        if (connectionEstablished)
            await sessionStorage.DeleteAsync("jwttoken");
        await AuthenticateUser(string.Empty);
    }

    /// <summary>
    /// Reauthenticate the user, and notify the
    /// application if there was any change of
    /// user, e.g. after logout or successful
    /// login.
    /// </summary>
    /// <param name="jwt">The token to authenticate</param>
    public async Task AuthenticateUser(string jwt)
    {
        AuthenticationState authState = await DecodeJwt(jwt);
        NotifyAuthenticationStateChanged
            (Task.FromResult(authState));
    }

    /// <summary>
    /// Used by Blazor to implement authorization in
    /// routing and via the <AuthorizeView></AuthorizeView>
    /// tags in Blazor pages
    /// </summary>
    /// <returns>The authentication state of the current
    /// user</returns>
    public override async Task<AuthenticationState>
        GetAuthenticationStateAsync()
    {
        if (connectionEstablished)
        {
            var token = await sessionStorage
                .GetAsync<string>("jwttoken");
            if (!token.Success || token.Value == null)
                return anonUser;

            return await DecodeJwt(token.Value);
        }
        else if (string.IsNullOrWhiteSpace(currentToken))
            return anonUser;
        else
            return await DecodeJwt(currentToken);
    }

    /// <summary>
    /// Given an encoded JWT token, convert it into a
    /// ClaimsPrincipal so that its user identity,
    /// roles, etc. can be accessed.
    /// </summary>
    /// <param name="jwt">The encoded JWT token
    /// string</param>
    /// <returns>The authentication state based on the
    /// claims principal derived from the token,
    /// or on the anonymous user principal implying the
    /// user is not authenticated.</returns>
    private async Task<AuthenticationState> DecodeJwt(string jwt)
    {
        JsonWebTokenHandler handler = new();
        TokenValidationParameters validationParams = new()
        {
            ValidIssuer = "https://Abbott.com/",
            ValidAudience = "https://Abbott.com/",
            ValidateIssuer = true,
            ValidateAudience = true,
            IssuerSigningKey = new SymmetricSecurityKey(jwtSecretKey),
            ValidateIssuerSigningKey = true,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromSeconds(5)
        };
        TokenValidationResult result = await handler
            .ValidateTokenAsync(jwt, validationParams);
        if (result.IsValid)
        {
            ClaimsPrincipal principal = new(result.ClaimsIdentity);
            return new AuthenticationState(principal);
        }
        return anonUser;
    }

    /// <summary>
    /// Used to indicate to the state provider that the
    /// browser's local storage is now accessible, as the
    /// SignalR circuit has been opened. This is true once
    /// we get to the first call of OnAfterRenderAsync()
    /// on the first page displayed.
    /// </summary>
    public void ConnectionEstablished()
        => connectionEstablished = true;
}
