using Azure.Core;
using Azure.Identity;

using Microsoft.SqlServer.Dac;

namespace DbDeployer.AuthProviders
{
    public class BugsAuthProvider : IUniversalAuthProvider
    {
        #region Constants

        private const string SQL_RESOURCE = "https://database.windows.net//.default";

        #endregion

        public string GetValidAccessToken()
        {
            var defaultAzureCredential = new DefaultAzureCredential();
            var token = defaultAzureCredential.GetToken(new TokenRequestContext(new[] { SQL_RESOURCE }));

            return token.Token;
        }
    }
}