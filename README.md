## Cloud SQL Proxy .NET Wrapper

The Cloud SQL Proxy .NET Wrapper allows the usage, management, and packaging of [The Cloud SQL Proxy](https://github.com/GoogleCloudPlatform/cloudsql-proxy)
inside a .Net application. This may be useful for an application which is installed on a workstation by an inexperienced user, such as a winform/wpf/windows service.

To build from source, ensure you have [go installed](https://golang.org/doc/install)
,set [GOPATH](https://github.com/golang/go/wiki/GOPATH), and
[Installed a C Compiler](http://mingw-w64.org/doku.php/download/mingw-builds)

## To use from third party applications

### Install via Nuget

For convienience the Package is available via Nuget with [Installation Instructions](https://www.nuget.org/packages/cloudsql-proxy-cs).

### Using library (C#)

The Proxy uses a singleton class to access functionality of the proxy. As proxy connections persist in a separate thread.

#### Get the Proxy Singleton

```
var proxy =  Proxy.GetInstance();
```

#### Status Change Event

There is also an event which can be subscribed to for changes to the status.

```
proxy.OnStatusChanged += (object sender, StatusEventArgs status) =>
{
	Console.WriteLine($"Status from instance: {status.Instance}: {status.Status}");
};
```

#### Start the Proxy

To start and Instance.

```
proxy.StartProxy(AuthenticationMethod.CredentialFile, instance, tokenFile);
```

#### Stop the Proxy

Methods are exposed to Stop a specific instance, or Stop All instances.
The StopProxy() method maintains a counter internally for the number of active calls to start the proxy. This helps keep the connection alive when multiple
callers are making use of it. It is particularly helpful when implementing a Using pattern.

```
proxy.StopProxy(instance);
```

or

```
proxy.StopAll();
```

or

```
proxy.Dispose();
```

#### Helpful Methods

GetPort() returns the port of the specified proxy instance. It is recommended that you set the port to zero when starting
the proxy and use the GetPort method to configure your DB connection.

```
Console.WriteLine($"Port: {proxy.GetPort(instance)}");
```

GetStatus() returns the current status of the proxy.

```
Console.WriteLine($"Status: {proxy.GetStatus(instance)}");
```

#### IDisposable

The Proxy Wrapper implements IDisposable. If it is disposed, then it will call StopAll. This will close all proxy connections.

StartProxy() and StartProxyAsync return a ProxyInstance object, which also implements IDisposable. It will close its individual
proxy instance connection when disposed. This allows the following usage pattern.

```
using (var i = await Proxy.GetInstance.StartProxy(AuthenticationMethod.JSON, instance, credentials)
{
	// wait for connection
	int port = i.GetPort();
	log.Add($"Proxy started on port {port}");

	// get status
	var status = i.GetStatus();
	log.Add($"Proxy status: {status}");

	// log closing
	log.Add($"Closing proxy");
}
```

#### gRPC server in C

Super easy, just edit the .proto file and everything else will be done for you. Server implementation example is in ProxyStatusImpl.cs.

```
<ItemGroup>
	<Protobuf Include="protos\proxy-status.proto" GrpcServices="Server" />
</ItemGroup>
```

The above ItemGroup is needed so the csproj knows it needs to generate the code stubs from the .proto file. GrpcServices="Server" means that only the server components are required. The Microsoft documentations leads you to believe that this means that Grpc.Tools is the only package required. This is incorrect and Google.Protobuf, Grpc, Grpc.Tools are all required.

## Building cloud_sql_proxy binaries

### Building go library

This has been developed using Debian WSL in Windows 10.

#### Setup environment

```
sudo apt-get update -y
sudo apt-get install -y --no-install-recommends g++ gcc libc6-dev make pkg-config
sudo rm -rf /var/lib/apt/lists/*
GOLANG_VERSION=1.14.1
dpkgArch="$(dpkg --print-architecture)";
goRelArch='linux-amd64';
goRelSha256='2f49eb17ce8b48c680cdb166ffd7389702c0dec6effa090c324804a5cac8a7f8';
url="https://golang.org/dl/go${GOLANG_VERSION}.${goRelArch}.tar.gz";
sudo apt-get install wget -y
sudo wget -O go.tgz "$url";
sudo tar -C /usr/local -xzf go.tgz;
sudo apt-get update -y
sudo apt-get install -y gcc-multilib
sudo apt-get install -y gcc-mingw-w64
export GOPATH=/go
export PATH=$GOPATH/bin:/usr/local/go/bin:$PATH
sudo mkdir -p "$GOPATH/src" "$GOPATH/bin" && chmod -R 777 "$GOPATH"
```

add the following to ~/.profile

```
export GOLANG_VERSION=1.14.1
export GOPATH=/go
export PATH="$GOPATH/bin:/usr/local/go/bin:$PATH"
```

#### Run build

From lib\cloud_sql_proxy

```
. build-linux
```

or

```
. build-windows-64
```

#### gRPC in go

The following notes is to prevent the inevitable insanity that will come with trying to figuring it out on your own.

Get the latest version of protoc, we need at least v3 from [here](https://github.com/protocolbuffers/protobuf/releases/tag/v3.12.3).
Download the protoc-3.xx.x-win64.zip for win x64.
Extract this somewhere and add to your path
Open cmd window and go to ~\lib\cloud_sql_proxy
type the following

```
protoc --go_out=plugins=grpc:. --go_opt=paths=source_relative proxy_status\proxy_status.proto
```

this will generate ~\lib\cloud_sql_proxy\proxy_status\proxy_status.pb.go
this is referenced in cloud_sql_proxy.go as the following import

```
pb "github.com/expert1-pty-ltd/cloudsql-proxy/lib/cloud_sql_proxy/proxy_status"
```

the following is required in go.mod which basically tells the compiler where to find the expert1 module locally

```
replace github.com/expert1-pty-ltd/cloudsql-proxy => ./
```

the dialer code in SetStatus is called each time it's needed

### Building dotnet core app in linux

#### Register Microsoft key and feed

```
sudo apt-get update
sudo apt-get install gpg
wget -O- https://packages.microsoft.com/keys/microsoft.asc | gpg --dearmor > microsoft.asc.gpg
sudo mv microsoft.asc.gpg /etc/apt/trusted.gpg.d/
wget https://packages.microsoft.com/config/debian/10/prod.list
sudo mv prod.list /etc/apt/sources.list.d/microsoft-prod.list
sudo chown root:root /etc/apt/trusted.gpg.d/microsoft.asc.gpg
sudo chown root:root /etc/apt/sources.list.d/microsoft-prod.list
```

#### Install .NET Core SDK

```
sudo apt-get update
sudo apt-get install apt-transport-https
sudo apt-get install dotnet-sdk-3.1
```

#### Run build

From examples\cs\cloud_sql_proxy directory

```
sudo dotnet build -f netcoreapp3.1
```

From examples\cs\cmd directory

```
sudo dotnet build
```
