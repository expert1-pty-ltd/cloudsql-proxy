using Expert1.CloudSqlProxy;
using Microsoft.Data.SqlClient;
using System.Diagnostics;

namespace ConsoleTest
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            if (args.Length < 3)
            {
                Console.WriteLine("Usage: CloudSqlProxyTest <CredentialFile|JSON> <instance-connection-string> <credentials>");
                return;
            }

            string authMethod = args[0];
            string instance = args[1];
            string credentials = args[2];

            AuthenticationMethod authenticationMethod;
            if (authMethod.Equals("CredentialFile", StringComparison.OrdinalIgnoreCase))
            {
                authenticationMethod = AuthenticationMethod.CredentialFile;
            }
            else if (authMethod.Equals("JSON", StringComparison.OrdinalIgnoreCase))
            {
                authenticationMethod = AuthenticationMethod.JSON;
            }
            else
            {
                Console.WriteLine("Invalid authentication method. Use 'CredentialFile' or 'JSON'.");
                return;
            }

            try
            {
                using (var proxyInstance = await ProxyInstance.StartProxyAsync(authenticationMethod, instance, credentials))
                {
                    Console.WriteLine($"Proxy instance created for {instance}");
                    Console.WriteLine($"Listening on port {proxyInstance.Port}");

                    // Update the connection string to use the proxy's local port
                    var builder = new SqlConnectionStringBuilder()
                    {
                        DataSource = proxyInstance.DataSource,
                        InitialCatalog ="master",
                        UserID= "",
                        Password= "",
                        Encrypt=false,
                    };

                    using (var connection = new SqlConnection(builder.ConnectionString))
                    {
                        await connection.OpenAsync();
                        Console.WriteLine("Connected to the database.");

                        using (var command = new SqlCommand("SELECT TOP 1 name FROM sys.databases", connection))
                        {
                            var result = await command.ExecuteScalarAsync();
                            Console.WriteLine($"Query result: {result}");
                        }
                    }

                    Console.WriteLine("Press Enter to exit...");
                    Console.ReadLine();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }
            finally
            {
                ProxyInstance.StopAllInstances();
            }
        }
    }
}
