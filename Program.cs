using System.CommandLine;
using System.CommandLine.Invocation;
using System.Threading.Tasks;
using DbDeployer.CommandHandlers;
using DbDeployer.Enumerations;

namespace db_deployer
{
    class Program
    {
        static async Task Main(string[] args)
        {
            await BuildRootCommand().InvokeAsync(args);
        }

        #region Private methods

        private static RootCommand BuildRootCommand()
        {
            var rootCommand = new RootCommand
            {
                new Option<DeploymentEnvironment>(
                    new[] { "--environment", "-e" },
                    getDefaultValue: () => DeploymentEnvironment.DEVELOPMENT,
                    description: "The environment to deploy to, defaults to DEVELOPMENT."),
                new Option<string>(
                    new[] { "--dacpacFilepath", "-f" },
                    description: "The full path to the dacpac to deploy.")
                {
                    IsRequired = true
                }
            };
            rootCommand.Description = "Database deployer";
            rootCommand.Handler = CommandHandler.Create<DeploymentEnvironment, string, string>(
                (environment, postfix, dacpacFilepath) => DeploymentHandler.ExecuteDeployments(environment, postfix, dacpacFilepath));

            return rootCommand;
        }

        #endregion
    }
}
