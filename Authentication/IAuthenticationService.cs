namespace Authentication;

public interface IAuthenticationService
{
    AuthenticationResponse? Authenticate(AuthenticationRequest authenticationRequest);
}
