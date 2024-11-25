using Google.Apis.Auth.OAuth2;
using Google.Apis.Drive.v3;
using Google.Apis.Drive.v3.Data;
using Google.Apis.Services;
using Google.Apis.Util.Store;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace LAB2.Google_Drive_Services
{
    internal class Authentication
    {
        public static async Task<DriveService> GetDriveServiceAsync(string[] scopes)
        {
            UserCredential credential;
            string credPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "token.json");

            if (Directory.Exists(credPath))
            {
                Directory.Delete(credPath, true);
            }

            using (var stream = new FileStream("clientjson.json", FileMode.Open, FileAccess.Read))
            {
                credential = await GoogleWebAuthorizationBroker.AuthorizeAsync(
                    GoogleClientSecrets.Load(stream).Secrets,
                    scopes,
                    "user",
                    CancellationToken.None,
                    new FileDataStore(credPath, true));
            }

            return new DriveService(new BaseClientService.Initializer()
            {
                HttpClientInitializer = credential,
                ApplicationName = "LAB2*",
            });
        }
    }
}
