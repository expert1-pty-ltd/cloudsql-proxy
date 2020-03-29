## Cloud SQL Proxy .NET Wrapper

The Cloud SQL Proxy .NET Wrapper allows the usage, management, and packaging of [The Cloud SQL Proxy](github.com/GoogleCloudPlatform/cloudsql-proxy)
inside a .Net application.

To build from source, ensure you have [go installed](https://golang.org/doc/install)
,set [GOPATH](https://github.com/golang/go/wiki/GOPATH), 
[Installed a C Compiler](http://mingw-w64.org/doku.php/download/mingw-builds)

## To use from third party applications

### Nuget

For convienience the Package is available via Nuget with [Installation Instructions](https://www.nuget.org/packages/cloudsql-proxy-cs/1.0.1).

### Exposed methods

- char* Echo(char* message);
  - echo's back whatever is passed in message. Message is prepended by `From DLL:`. E.g. passing `1234` returns `From DLL: 1234`
- StartProxy(char* instances, char* tokenFile);
  - start the proxy. Method blocks until proxy is shut down.
- StopProxy();
  - stop the proxy. Will cause StartProxy to exit and return.

### Using library (C#)

This is a basic example of declaring methods that can be called from C#. The DLL must be in the same path as the C# executable.

```
[DllImport("cloud_sql_proxy.dll", CharSet = CharSet.Unicode. CallingConvention = CallingConvention.StdCall)]
public extern static IntPtr Echo(byte[] message);

[DllImport("cloud_sql_proxy.dll", CharSet = CharSet.Unicode. CallingConvention = CallingConvention.StdCall)]
public extern static void StartProxy(byte[] instances, byte[] tokenFile);

[DllImport("cloud_sql_proxy.dll", CharSet = CharSet.Unicode. CallingConvention = CallingConvention.StdCall)]
public extern static void StopProxy();
```
