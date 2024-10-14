# JSON Web Token Authenticated Blazor App
This is an example of how to secure a Blazor server application using Json Web Tokens. The application is built using .NET 8.

Based on the Microsoft template example (`Counter.razor` and `Weather.razor` content pages), it adds a login page and a navbar logout button.
A class library that issues a JWT token if the user's credentials are valid is included, though this has a mock authentication check method
that should be replaced by something that uses a real authentication provider.

Pages are secured using the `<AuthorizeView>`, `<Authorized>` and `<NotAuthorized>` tags, which in turn rely on the `AuthenticationStateProvider`
class, all of which is standard built-in functionality for Blazor. However, a custom `JwtAuthStateProvider` class is derived from the inbuilt
`AuthenticationStateProvider`, overriding its `GetAuthenticationState` method to use the `ClaimsPrincipal` derived from the web token, or an anonymous
principal if a token has not been issued.

For Blazor server, it is not possible to store the JWT into the browser local storage until after the SignalR circuit has been established.
In this implementation, the token is held in the scope-injected `JwtAuthStateProvider` until the `OnAfterRenderAsync` event handler for the login 
page is executed. Thereafter the token is also held in the `ProtectedSessionStorage` for the current tab in the browser.

Usually with Blazor applications that are server-side, the authentication state of the token is checked every thirty minutes, as the SignalR 
circuit is maintained for the session. To force a logout when there is user inactivity for a period of time, inactivity has to be detected
at the browser end of the connection, and then a forced logout that also invalidates the JWT token is executed.

This example detects browser mouse move and keydown events and uses them to reset a timer to its starting value. When the timer expires,
which will only happen if the mouse/keyboard events don't occur, JSInterop causes the logout to happen, and redirects the user back to
the login page.
