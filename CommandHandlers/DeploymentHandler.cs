using DbDeployer.Enumerations;

namespace DbDeployer.CommandHandlers
{
	public class DeploymentHandler
	{
		#region Constants

		private const string AUTH_FILE = "azureauth.json";
		private const string ENVVAR_CLIENT_ID = "AZURE_CLIENT_ID";
		private const string ENVVAR_TENANT_ID = "AZURE_TENANT_ID";
		private const string ENVVAR_CLIENT_SECRET = "AZURE_CLIENT_SECRET";

		#endregion

		public static void ExecuteDeployments(DeploymentEnvironment environment, string postfix, string dacpacFilepath)
		{
			//TODO: Implement
		}
	}
}