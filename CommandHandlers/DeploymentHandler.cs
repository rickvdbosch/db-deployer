using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

using Microsoft.Azure.Management.ResourceManager.Fluent;
using Microsoft.Azure.Management.ResourceManager.Fluent.Authentication;
using Microsoft.Azure.Management.Sql.Fluent;
using Microsoft.SqlServer.Dac;

using DbDeployer.AuthProviders;
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
			var databases = new List<ISqlDatabase>();
            var credentials = SdkContext.AzureCredentialsFactory.FromFile(AUTH_FILE);
            SetEnvironmentVariables(credentials);

            var azure = Microsoft.Azure.Management.Fluent.Azure
				.Configure()
                .Authenticate(credentials)
                .WithDefaultSubscription();

			//You can add any filtering on servers or database you would like here 👇🏻
            var servers = azure.SqlServers.List();
            foreach (var server in servers)
            {
				databases.AddRange(server.Databases.List().ToList());
            }

            using var dacpac = DacPackage.Load(dacpacFilepath);
            foreach (var database in databases)
            {
                //TODO: We could (should?) store the result of each deploy and report on them
                _ = DeployDatabase(database, dacpac);
            }
		}

		private static bool DeployDatabase(ISqlDatabase database, DacPackage dacpac)
		{
            bool success = true;
            var dacOptions = new DacDeployOptions
            {
                BlockOnPossibleDataLoss = false,
                DoNotDropObjectTypes = new[] 
                {
                    ObjectType.Permissions,
                    ObjectType.RoleMembership,
                    ObjectType.Users
                },
                DropObjectsNotInSource = true
            };
            var connectionString = $"Server=tcp:{database.SqlServerName}.database.windows.net,1433;";
            var dacServiceInstance = new DacServices(connectionString, new SqlAuthProvider());
            dacServiceInstance.ProgressChanged += (s, e) => Console.WriteLine(e.Message);
            dacServiceInstance.Message += (s, e) => Console.WriteLine(e.Message.Message);

            try
            {
                Console.WriteLine($"Starting work on database '{database.Name}'");
                dacServiceInstance.Deploy(dacpac, database.Name, true, dacOptions);
            }
            catch (Exception ex)
            {
                success = false;
                Console.ForegroundColor = ConsoleColor.Red;
                WriteException(ex);
            }
            finally
            {
                Console.ForegroundColor = ConsoleColor.Gray;
                Console.WriteLine($"Finishing work on database '{database.Name}'");
            }

            return success;
		}

		/// <summary>
		/// Writes out error message recursively for '<paramref name="level" />' amount of nested exceptions.
		/// </summary>
        /// <param name="exception">The <see cref="Exception" /> to write the message from to Console.</param>
        /// <param name="level">The level of nested exceptions to process (if inner exceptions are available).</param>
		private static void WriteException(Exception exception, int level = 0)
        {
            var prefix = "MESSAGE:";
            for (int i = 0; i < level; i++)
            {
                prefix = "\t" + prefix;
            }
            Console.WriteLine($"{prefix} - {exception.Message}");
            if (exception.InnerException != null)
            {
                WriteException(exception.InnerException, ++level);
            }
        }

		/// <summary>
        /// Sets environment variables based on an <see cref="AzureCredentials"/> instance.
        /// </summary>
		private static void SetEnvironmentVariables(AzureCredentials credentials)
		{
            Environment.SetEnvironmentVariable(ENVVAR_CLIENT_ID, credentials.ClientId);
            Environment.SetEnvironmentVariable(ENVVAR_TENANT_ID, credentials.TenantId);

            var name = nameof(ServicePrincipalLoginInformation);
            name = char.ToLower(name[0]) + name[1..];
            var spliField = credentials.GetType().GetField(name, BindingFlags.NonPublic | BindingFlags.Instance);
            var spli = (ServicePrincipalLoginInformation)spliField.GetValue(credentials);
            Environment.SetEnvironmentVariable(ENVVAR_CLIENT_SECRET, spli.ClientSecret);
		}
	}
}