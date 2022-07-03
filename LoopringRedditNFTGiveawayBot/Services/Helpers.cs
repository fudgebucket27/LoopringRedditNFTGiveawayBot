using Reddit.AuthTokenRetriever;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


public class Helpers
{
    public static (string, string) AuthorizeUser(string appId, string appSecret = null, int port = 8080)
    {
        // Create a new instance of the auth token retrieval library.  --Kris
        AuthTokenRetrieverLib authTokenRetrieverLib = new AuthTokenRetrieverLib(appId, appSecret, port);

        // Start the callback listener.  --Kris
        // Note - Ignore the logging exception message if you see it.  You can use Console.Clear() after this call to get rid of it if you're running a console app.
        authTokenRetrieverLib.AwaitCallback();

        Console.Clear();

        // Open the browser to the Reddit authentication page.  Once the user clicks "accept", Reddit will redirect the browser to localhost:8080, where AwaitCallback will take over.  --Kris
        OpenBrowser(authTokenRetrieverLib.AuthURL());

        // Replace this with whatever you want the app to do while it waits for the user to load the auth page and click Accept.  --Kris
        while (true) { }

        // Cleanup.  --Kris
        authTokenRetrieverLib.StopListening();

        return (authTokenRetrieverLib.RefreshToken, authTokenRetrieverLib.AccessToken);
    }

    public static void OpenBrowser(string authUrl, string browserPath = @"C:\Program Files (x86)\Microsoft\Edge\Application\msedge.exe")
    {
        try
        {
            ProcessStartInfo processStartInfo = new ProcessStartInfo(authUrl);
            Process.Start(processStartInfo);
        }
        catch (System.ComponentModel.Win32Exception e)
        {
            Console.WriteLine(e.Message);
            // This typically occurs if the runtime doesn't know where your browser is.  Use BrowserPath for when this happens.  --Kris
            ProcessStartInfo processStartInfo = new ProcessStartInfo(browserPath)
            {
                Arguments = authUrl
            };
            Process.Start(processStartInfo);
        }
    }
}
