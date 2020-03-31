## Cloud SQL Proxy .NET Wrapper

The Cloud SQL Proxy .NET Wrapper allows the usage, management, and packaging of [The Cloud SQL Proxy](https://github.com/GoogleCloudPlatform/cloudsql-proxy)
inside a .Net application. This may be useful for an application which is installed on a workstation by an inexperienced user, such as a winform/wpf/windows service.

To build from source, ensure you have [go installed](https://golang.org/doc/install)
,set [GOPATH](https://github.com/golang/go/wiki/GOPATH), and
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
