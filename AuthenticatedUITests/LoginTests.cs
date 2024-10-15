using Microsoft.VisualStudio.TestTools.UnitTesting;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Interactions;
using OpenQA.Selenium.Support.UI;
using System.Diagnostics;
using System.IO;
using System.Xml.Linq;

namespace AuthenticatedUITests;

[TestClass]
public class LoginTests
{
    const bool Headless = true; // Set false to see tests
    public readonly static string SiteUrl = "https://localhost:5443";
    public static readonly string WorkingDirectory 
        = @"..\..\..\..\AuthenticatedUI";
    ChromeDriver? driver = null;
    //WebDriverWait? wait = null;
    static Process? kestrelServer = null;

    [ClassInitialize]
    public async static Task ClassSetup(TestContext _)
    {
        // Make the test runner launch the Kestrel web server hosting
        // the Blazor application. We are then able to automate the
        // browser to test it.

        ProcessStartInfo startInfo = new()
        {
            CreateNoWindow = true,
            UseShellExecute = true,
            FileName = "dotnet",
            Arguments = $"run --urls {SiteUrl}",
            WorkingDirectory = WorkingDirectory
        };
        kestrelServer = Process.Start(startInfo);
        await Task.Delay(6000);
    }

    [ClassCleanup]
    public static void ClassTearDown()
    {
        // Shut down the Kestrel server after tests have
        // been run. Prevents lock-ups when trying to
        // rebuild the application between tests.

        if(kestrelServer != null)
        {
            kestrelServer.Kill(true);
            kestrelServer.WaitForExit();
        }
    }

    /// <summary>
    /// Helper function to ensure all page navigations
    /// wait while the blazor server in debug mode
    /// renders the new page layout
    /// </summary>
    /// <param name="url">The URL to navigate to</param>
    /// <returns>A task to await</returns>
    
    private async Task GoToUrl(string url)
    {
        driver?.Navigate().GoToUrl(url);
        await Task.Delay(1000);
    }

    [TestInitialize]
    public async Task Setup()
    {
        ChromeOptions options = new();
        if(Headless)
            // NOTE: Because of a bug in the Chrome 129 code, the
            // following will display a blank window for each test
            // until 130 is released. Fix merged at Google, but
            // not released until 130 comes out.
            options.AddArgument("--headless");
        driver = new ChromeDriver(options);
        //wait = new WebDriverWait(driver, TimeSpan.FromSeconds(10));
        await GoToUrl(SiteUrl);
    }

    [TestCleanup]
    public void TearDown() => driver?.Quit();

    [TestMethod]
    public void DisplayLoginPage()
    {
        var loginTitle = driver?.FindElement(By.TagName("h3"));
        Assert.AreEqual("Login", loginTitle?.Text);
    }

    [TestMethod]
    public async Task ValidCredentialsLogsIn()
    {
        var userNameElement = driver?.FindElement(By.Id("username"));
        var passwordElement = driver?.FindElement(By.Id("password"));
        var submitButton = driver?.FindElement(By.Id("loginsubmit"));

        // Fill in name and password, then click the login button

        userNameElement?.SendKeys("admin");
        passwordElement?.SendKeys("adminpw");
        submitButton?.Click();

        // Give Blazor time to load the next page, then verify
        // we arrived at the correct logged in page content

        await Task.Delay(1000);
        var heading = driver?.FindElement(By.Id("banner"));
        Assert.AreEqual("Welcome! You are now logged in.", heading?.Text);

        // Validate that the additional nav menu items
        // have been shown for authenticated users

        var navElements = driver?.FindElements(By.XPath("//a")).Select(e => e.Text);
        Assert.IsNotNull(navElements);
        Assert.IsTrue(navElements.Any(e => e == "Counter"));
        Assert.IsTrue(navElements.Any(e => e == "Weather"));
        Assert.IsTrue(navElements.Any(e => e == "Logout"));
    }

    [TestMethod]
    public async Task InvalidValidCredentialsRejected()
    {
        var userNameElement = driver?.FindElement(By.Id("username"));
        var passwordElement = driver?.FindElement(By.Id("password"));
        var submitButton = driver?.FindElement(By.Id("loginsubmit"));

        // Try logging in with bad credentials

        userNameElement?.SendKeys("fred");
        passwordElement?.SendKeys("xyzzy");
        submitButton?.Click();
        await Task.Delay(1000);

        // Confirm that the application remains on
        // the login page, and that the error message
        // has been displayed.

        var heading = driver?.FindElement(By.Id("banner"));
        Assert.AreEqual("Login", heading?.Text);
        var errorElement = driver?.FindElement(By.Id("loginError"));
        Assert.AreEqual("Incorrect name or password. Please try again.", errorElement?.Text);

        // Confirm that none of the logged in navigation
        // menu items has been made visible

        var navElements = driver?.FindElements(By.XPath("//a")).Select(e => e.Text);
        Assert.IsNotNull(navElements);
        Assert.IsFalse(navElements.Any(e => e == "Counter"));
        Assert.IsFalse(navElements.Any(e => e == "Weather"));
        Assert.IsFalse(navElements.Any(e => e == "Logout"));
    }

    [TestMethod]
    public async Task CanLogout()
    {
        // First login so that we can then try logging out

        var userNameElement = driver?.FindElement(By.Id("username"));
        var passwordElement = driver?.FindElement(By.Id("password"));
        var submitButton = driver?.FindElement(By.Id("loginsubmit"));
        userNameElement?.SendKeys("fred");
        passwordElement?.SendKeys("fredpw");
        submitButton?.Click();
        await Task.Delay(1000);

        // Check that we departed from the login page

        var heading = driver?.FindElement(By.Id("banner"));
        Assert.AreEqual("Welcome! You are now logged in.", heading?.Text);
        
        // Find and click the logout link in the nav bar

        var logoutElement = driver?
            .FindElements(By.XPath("//a"))
            .First(e => e.Text == "Logout");
        Assert.IsNotNull(logoutElement);
        logoutElement.Click();
        await Task.Delay(1000);

        // Check that we returned to the login page

        heading = driver?.FindElement(By.Id("banner"));
        Assert.AreEqual("Login", heading?.Text);

        // Confirm that none of the logged in navigation
        // menu items is visible any longer

        var navElements = driver?.FindElements(By.XPath("//a")).Select(e => e.Text);
        Assert.IsNotNull(navElements);
        Assert.IsFalse(navElements.Any(e => e == "Counter"));
        Assert.IsFalse(navElements.Any(e => e == "Weather"));
        Assert.IsFalse(navElements.Any(e => e == "Logout"));
    }

    [DataTestMethod]
    [DataRow("/counter", "Either you are not logged in,")]
    [DataRow("/weather", "You are not logged in.")]
    public async Task CannotVisitAuthorizedPages(string path, string errorMsg)
    {
        driver?.Navigate().GoToUrl($"{SiteUrl}{path}");
        await Task.Delay(1000);
        var articleElement = driver?.FindElement(By.TagName("article"));
        Assert.IsNotNull(articleElement);
        Assert.IsTrue(articleElement
            .Text.StartsWith(errorMsg));
    }

    [TestMethod]
    public async Task InactivityCausesLogout()
    {
        var userNameElement = driver?.FindElement(By.Id("username"));
        var passwordElement = driver?.FindElement(By.Id("password"));
        var submitButton = driver?.FindElement(By.Id("loginsubmit"));

        // Try logging in with bad credentials

        userNameElement?.SendKeys("fred");
        passwordElement?.SendKeys("fredpw");
        submitButton?.Click();
        await Task.Delay(1000);

        // Check that we departed from the login page

        var heading = driver?.FindElement(By.Id("banner"));
        Assert.AreEqual("Welcome! You are now logged in.", heading?.Text);

        // Wait for the timeout period, which has been set to
        // 10 seconds in appsettings.config

        await Task.Delay(11 * 1000);

        // Confirm that the application has logged the
        // user out because of inactivity on the page

        heading = driver?.FindElement(By.Id("banner"));
        Assert.AreEqual("Login", heading?.Text);

        // Confirm that none of the logged in navigation
        // menu items has been left visible

        var navElements = driver?.FindElements(By.XPath("//a")).Select(e => e.Text);
        Assert.IsNotNull(navElements);
        Assert.IsFalse(navElements.Any(e => e == "Counter"));
        Assert.IsFalse(navElements.Any(e => e == "Weather"));
        Assert.IsFalse(navElements.Any(e => e == "Logout"));
    }

    [TestMethod]
    public async Task InputActivityPreventsTimeOut()
    {
        var userNameElement = driver?.FindElement(By.Id("username"));
        var passwordElement = driver?.FindElement(By.Id("password"));
        var submitButton = driver?.FindElement(By.Id("loginsubmit"));

        // Try logging in with bad credentials

        userNameElement?.SendKeys("fred");
        passwordElement?.SendKeys("fredpw");
        submitButton?.Click();
        await Task.Delay(1000);

        // Check that we departed from the login page

        var heading = driver?.FindElement(By.Id("banner"));
        Assert.AreEqual("Welcome! You are now logged in.", heading?.Text);

        // Wait for the timeout period, which has been set to
        // 10 seconds in appsettings.config

        await Task.Delay(7 * 1000);

        // Wiggle the mouse

        new Actions(driver)
        .MoveByOffset(100, 100)
        .Perform();

        // Wait beyond the original timeout period, nine
        // seconds of which has already elapsed

        await Task.Delay(5 * 1000);

        // Confirm we have not left the landing page

        heading = driver?.FindElement(By.Id("banner"));
        Assert.AreEqual("Welcome! You are now logged in.", heading?.Text);
    }

    [TestMethod]
    public async Task NonExistentPagesReturn404()
    {
        driver?.Navigate().GoToUrl($"{SiteUrl}/xyzzy");
        await Task.Delay(1000);
        Assert.IsTrue(driver?.PageSource.Contains("404"));
    }

    [DataTestMethod]
    [DataRow("admin", "adminpw")]
    [DataRow("subadmin", "subadminpw")]
    public async Task SpecifiedRolesCanVisitRoleProtectedPages(string name, string pass)
    {
        var userNameElement = driver?.FindElement(By.Id("username"));
        var passwordElement = driver?.FindElement(By.Id("password"));
        var submitButton = driver?.FindElement(By.Id("loginsubmit"));

        // Fill in name and password, then click the login button

        userNameElement?.SendKeys(name);
        passwordElement?.SendKeys(pass);
        submitButton?.Click();
        await Task.Delay(1000);

        // Check that we departed from the login page

        var heading = driver?.FindElement(By.Id("banner"));
        Assert.AreEqual("Welcome! You are now logged in.", heading?.Text);

        // Navigate to the role-protected /counter page

        // Find and click the logout link in the nav bar

        var counterNavElement = driver?
            .FindElements(By.XPath("//a"))
            .First(e => e.Text == "Counter");
        Assert.IsNotNull(counterNavElement);
        counterNavElement.Click();
        await Task.Delay(1000);

        // Check that we have role-permitted content on the page

        var element = driver?.FindElement(By.TagName("h1"));
        Assert.AreEqual("Counter", element?.Text);
    }

    [DataTestMethod]
    [DataRow("admin", "adminpw", true)]
    [DataRow("subadmin", "subadminpw", false)]
    [DataRow("fred", "fredpw", false)]
    public async Task NavBarAuthorizedViewHidesWeatherPage
        (string name, string pass, bool result)
    {
        var userNameElement = driver?.FindElement(By.Id("username"));
        var passwordElement = driver?.FindElement(By.Id("password"));
        var submitButton = driver?.FindElement(By.Id("loginsubmit"));

        // Fill in name and password, then click the login button

        userNameElement?.SendKeys(name);
        passwordElement?.SendKeys(pass);
        submitButton?.Click();
        await Task.Delay(1000);

        // Validate the nav bar buttons

        var weatherNavElement = driver?
        .FindElements(By.XPath("//a"))
        .FirstOrDefault(e => e.Text == "Weather");

        Assert.AreEqual(result, weatherNavElement != null);
    }
}