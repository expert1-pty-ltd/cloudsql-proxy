# Cloud SQL Proxy

This project provides a .NET Standard library for creating and managing a secure connection to Google Cloud SQL instances using a local proxy. The proxy establishes an SSL/TLS connection to the Cloud SQL instance, allowing local applications to communicate with the database securely.

## Features

- Secure connection to Google Cloud SQL instances
- Automatic SSL/TLS certificate management
- Supports multiple concurrent connections
- Handles periodic certificate refresh

## Prerequisites

- .NET Standard 2.0 or later
- Google Cloud SDK
- Google Cloud SQL instance

## Installation

To install the library, add it to your project via NuGet Package Manager:

PM> Install-Package Expert1.CloudSqlProxy

## Usage

### Authentication

The proxy supports two methods of authentication:

1. **Credential File**: Path to the Google credentials JSON file.
2. **JSON String**: Google credentials JSON file content as a string.

### Creating a Proxy Instance

To create and start a proxy instance, use the `ProxyInstance.StartProxyAsync` method. You can provide the authentication method, instance connection string, and credentials.

```csharp
using System;
using System.Threading.Tasks;

class Program
{
    static async Task Main(string[] args)
    {
        string instance = "your-project:your-region:your-instance-id";
        string credentialsPath = "path/to/your/credentials.json";
        
        try
        {
            var proxyInstance = await ProxyInstance.StartProxyAsync(
                AuthenticationMethod.CredentialFile, 
                instance, 
                credentialsPath
            );

            Console.WriteLine($"Proxy started. Connect to your database using DataSource: {proxyInstance.DataSource}");
            
            // Use proxyInstance.DataSource to connect to your database

            // When done, dispose the instance to stop the proxy
            proxyInstance.Dispose();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to start proxy: {ex.Message}");
        }
    }
}
```

### Connecting to the Database

Once the proxy is started, you can connect to your Cloud SQL database using the DataSource property of the ProxyInstance. This property provides the 127.0.0.1:<port> string, which can be used in your database connection string.

For example, to connect to a SQL Server instance:
```csharp
string connectionString = $"Server={proxyInstance.DataSource};Database=your-database;User Id=your-username;Password=your-password;";
using (var connection = new SqlConnection(connectionString))
{
    connection.Open();
    // Perform database operations
}
```

### Stopping the Proxy

To stop the proxy, you can call the Stop method or dispose of the ProxyInstance:

```csharp

proxyInstance.Stop(); // Stops the proxy

// Or, simply dispose the instance
proxyInstance.Dispose();
```

### Stopping All Proxies

To stop all running proxies, use the InstanceManager.StopAllInstances method:

```csharp
InstanceManager.StopAllInstances();
```