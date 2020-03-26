## Cloud SQL Proxy

The Cloud SQL Proxy allows a user with the appropriate permissions to connect
to a Second Generation Cloud SQL database without having to deal with IP whitelisting or SSL
certificates manually. It works by opening unix/tcp sockets on the local machine
and proxying connections to the associated Cloud SQL instances when the sockets
are used.

To build from source, ensure you have [go installed](https://golang.org/doc/install)
and have set [GOPATH](https://github.com/golang/go/wiki/GOPATH). Then, simply do a go get:

    go get github.com/GoogleCloudPlatform/cloudsql-proxy/cmd/cloud_sql_proxy

The cloud_sql_proxy will be placed in \$GOPATH/bin after go get completes.

cloud_sql_proxy takes a few arguments to configure what instances to connect to and connection behavior:

- `-fuse`: requires access to `/dev/fuse` as well as the `fusermount` binary. An
  optional `-fuse_tmp` flag can specify where to place temporary files. The
  directory indicated by `-dir` is mounted.
- `-instances="project1:region:instance1,project3:region:instance1"`: A comma-separated list
  of instances to open inside `-dir`. Also supports exposing a tcp port and renaming the default Unix Domain Sockets; see examples below.
  Same list can be provided via INSTANCES environment variable, in case when both are provided - proxy will use command line flag.
- `-instances_metadata=metadata_key`: Usable on [GCE](https://cloud.google.com/compute/docs/quickstart) only. The given [GCE metadata](https://cloud.google.com/compute/docs/metadata) key will be
  polled for a list of instances to open in `-dir`. The metadata key is relative from `computeMetadata/v1/`. The format for the value is the same as the 'instances' flag. A hanging-poll strategy is used, meaning that changes to
  the metadata value will be reflected in the `-dir` even while the proxy is
  running. When an instance is removed from the list the corresponding socket
  will be removed from `-dir` as well (unless it was also specified in
  `-instances`), but any existing connections to this instance will NOT be
  terminated.
- `-ip_address_types=PUBLIC,PRIVATE`: A comma-delimited list of preferred IP
  types for connecting to an instance. For example, setting this to PRIVATE will
  force the proxy to connect to instances using an instance's associated private
  IP. Defaults to `PUBLIC,PRIVATE`
- `-term_timeout=30s`: How long to wait for connections to close before shutting
  down the proxy. Defaults to 0.

Note: `-instances` and `-instances_metadata` may be used at the same time but
are not compatible with the `-fuse` flag.

cloud_sql_proxy authentication can be configured in a few different ways. Those listed higher on the list will override options lower on the list:

1. `credential_file` flag
2. `token` flag
3. Service account key at path stored in `GOOGLE_APPLICATION_CREDENTIALS`
4. gcloud _user_ credentials (set from `gcloud auth login`)
5. Default Application Credentials via goauth:

   1. `GOOGLE_APPLICATION_CREDENTIALS` (again)
   2. gcloud _application default_ credentials (set from `gcloud auth application-default login`)
   3. appengine.AccessToken (for App Engine Go < =1.9)
   4. GCE/GAE metadata credentials

When the proxy authenticates under the default service account of the
Compute Engine VM it is running on the VM must have at least the
sqlservice.admin API scope ("https://www.googleapis.com/auth/sqlservice.admin")
and the associated project must have the SQL Admin API
enabled. The default service account must also have at least WRITER/EDITOR
priviledges to any projects of target SQL instances.

Specifying the `-credential_file` flag allows use of the proxy outside of
Google's cloud. Simply [create a new service
account](https://cloud.google.com/sql/docs/mysql/sql-proxy#create-service-account),
download the associated JSON file, and set `-credential_file` to the path of the
JSON file. You can also set the GOOGLE_APPLICATION_CREDENTIALS environment variable
instead of passing this flag.

## Example invocations:

    ./cloud_sql_proxy -dir=/cloudsql -instances=my-project:us-central1:sql-inst &
    mysql -u root -S /cloudsql/my-project:us-central1:sql-inst

    # To retrieve instances from a custom metadata value (only when running on GCE)
    ./cloud_sql_proxy -dir=/cloudsql -instances_metadata instance/attributes/<custom-metadata-key> &
    mysql -u root -S /cloudsql/my-project:us-central1:sql-inst

    # For -fuse you do not need to specify instance names ahead of time:
    ./cloud_sql_proxy -dir=/cloudsql -fuse &
    mysql -u root -S /cloudsql/my-project:us-central1:sql-inst

    # For programs which do not support using Unix Domain Sockets, specify tcp:
    ./cloud_sql_proxy -dir=/cloudsql -instances=my-project:us-central1:sql-inst=tcp:3306 &
    mysql -u root -h 127.0.0.1

    # For programs which require a certain Unix Domain Socket name:
    ./cloud_sql_proxy -dir=/cloudsql -instances=my-project:us-central1:sql-inst=unix:custom_socket_name &
    mysql -u root -S /cloudsql/custom_socket_name

    # For programs which require a the Unix Domain Socket at a specific location, set an absolute path (overrides -dir):
    ./cloud_sql_proxy -dir=/cloudsql -instances=my-project:us-central1:sql-inst=unix:/my/custom/sql-socket &
    mysql -u root -S /my/custom/sql-socket

## Container Images

For convenience, we currently host containerized versions of the proxy in the following GCR repos:

- `gcr.io/cloudsql-docker/gce-proxy`
- `us.gcr.io/cloudsql-docker/gce-proxy`
- `eu.gcr.io/cloudsql-docker/gce-proxy`
- `asia.gcr.io/cloudsql-docker/gce-proxy`

Images are tagged to the version of the proxy they contain. It's strongly suggested to use the
latest version of the proxy, and to update the version often.

## To use from Kubernetes:

### Deploying Cloud SQL Proxy as a sidecar container

Follow this [page](https://github.com/GoogleCloudPlatform/kubernetes-engine-samples/tree/master/cloudsql). See also
[Connecting from Google Kubernetes Engine](https://cloud.google.com/sql/docs/mysql/connect-kubernetes-engine).

## Third Party

**WARNING**: _These distributions are not officially supported by Google._

### Installing via Homebrew

You can find a formula for with Homebrew [here](https://github.com/tclass/homebrew-cloud_sql_proxy).

### K8s Cluster Service using Helm

Follow these [instructions](https://github.com/kubernetes/charts/tree/master/stable/gcloud-sqlproxy).
This chart creates a Deployment and a Service, but we recommend deploying the proxy as a sidecar container in your pods.

## To use from third party applications

### Building library

Run the following commands to build the package into a DLL which can then be imported by third
party code.

`cd ~/lib/cloud_sql_proxy`
If windows: `build.bat`
Alternatively: `go build -buildmode=c-shared -o cloud_sql_proxy.dll`

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
