// Copyright 2015 Google Inc. All Rights Reserved.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//      http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

// cloudsql-proxy can be used as a proxy to Cloud SQL databases. It supports
// connecting to many instances and authenticating via different means.
// Specifically, a list of instances may be provided on the command line, in
// GCE metadata (for VMs), or provided during connection time via a
// FUSE-mounted directory. See flags for a more specific explanation.

// Modifications Copyright (C) 2020 Expert 1 Pty Ltd

// cloudsql-proxy can be used as a proxy to Cloud SQL databases. This package has
// been created so that static external methods are exposed so this proxy can be used
// within third party applications. 
// It still supports connecting to many instances and authenticating via different means.
// Specifically, a list of instances may be provided on the command line, in
// GCE metadata (for VMs). Credentials can be passed by tokenFile parameter or
// via GOOGLE_APPLICATION_CREDENTIALS environment variable.

package main

import (
	"errors"
	"flag"
	"fmt"
	"io/ioutil"
	"log"
	"net/http"
	"os"
	"os/signal"
	"path/filepath"
	"strings"
	"sync"
	"syscall"
	"time"

	"github.com/GoogleCloudPlatform/cloudsql-proxy/logging"
	"github.com/GoogleCloudPlatform/cloudsql-proxy/proxy/certs"
	"github.com/GoogleCloudPlatform/cloudsql-proxy/proxy/fuse"
	"github.com/GoogleCloudPlatform/cloudsql-proxy/proxy/limits"
	"github.com/GoogleCloudPlatform/cloudsql-proxy/proxy/proxy"
	"github.com/GoogleCloudPlatform/cloudsql-proxy/proxy/util"

	"cloud.google.com/go/compute/metadata"
	"golang.org/x/net/context"
	"golang.org/x/oauth2"
	goauth "golang.org/x/oauth2/google"
	sqladmin "google.golang.org/api/sqladmin/v1beta4"
)

// #include "extern.h"
import "C"

// we need to include the c code from seperate files or cgo complains when compiling

var (
	version = false
	verbose = true
	quiet = false
	logDebugStdout = false

	refreshCfgThrottle = proxy.DefaultRefreshCfgThrottle
	checkRegion = false

	// Settings for how to choose which instance to connect to.
	dir = ""
	projects = ""
	instances = ""
	instanceSrc = ""
	useFuse = false
	fuseTmp = defaultTmp

	// Settings for limits
	maxConnections uint64 = 0
	fdRlimit = limits.ExpectedFDs
	termTimeout time.Duration = 0

	// Settings for authentication.
	token = ""
	tokenFile = ""
	tokenJson = ""
	ipAddressTypes = "PUBLIC,PRIVATE"

	// Setting to choose what API to connect to
	host = ""

	// Track connection status
	status = "disconnected"

	proxyClient proxy.Client

	g_cb C.callbackFunc
)

const (
	minimumRefreshCfgThrottle = time.Second

	port = 3307
)

var defaultTmp = filepath.Join(os.TempDir(), "cloudsql-proxy-tmp")

const defaultVersionString = "NO_VERSION_SET"

var versionString = defaultVersionString

// userAgentFromVersionString returns an appropriate user agent string for
// identifying this proxy process, or a blank string if versionString was not
// set to an interesting value.
func userAgentFromVersionString() string {
	if versionString == defaultVersionString {
		return ""
	}

	// Example versionString (see build.sh):
	//    version 1.05; sha 0f69d99588991aba0879df55f92562f7e79d7ca1 built Mon May  2 17:57:05 UTC 2016
	//
	// We just want the part before the semicolon.
	semi := strings.IndexByte(versionString, ';')
	if semi == -1 {
		return ""
	}
	return "cloud_sql_proxy " + versionString[:semi]
}

const accountErrorSuffix = `Please create a new VM with Cloud SQL access (scope) enabled under "Identity and API access". Alternatively, create a new "service account key" and specify it using the -credential_file parameter`

func checkFlags(onGCE bool) error {
	if !onGCE {
		if instanceSrc != "" {
			return errors.New("-instances_metadata unsupported outside of Google Compute Engine")
		}
		return nil
	}

	if token != "" || tokenFile != "" || tokenJson != "" || os.Getenv("GOOGLE_APPLICATION_CREDENTIALS") != "" {
		return nil
	}

	// Check if gcloud credentials are available and if so, skip checking the GCE VM service account scope.
	_, err := util.GcloudConfig()
	if err == nil {
		return nil
	}

	scopes, err := metadata.Scopes("default")
	if err != nil {
		if _, ok := err.(metadata.NotDefinedError); ok {
			return errors.New("no service account found for this Compute Engine VM. " + accountErrorSuffix)
		}
		return fmt.Errorf("error checking scopes: %T %v | %+v", err, err, err)
	}

	ok := false
	for _, sc := range scopes {
		if sc == proxy.SQLScope || sc == "https://www.googleapis.com/auth/cloud-platform" {
			ok = true
			break
		}
	}
	if !ok {
		return errors.New(`the default Compute Engine service account is not configured with sufficient permissions to access the Cloud SQL API from this VM. ` + accountErrorSuffix)
	}
	return nil
}

func authenticatedClientFromPath(ctx context.Context, f string) (*http.Client, error) {
	all, err := ioutil.ReadFile(f)
	if err != nil {
		return nil, fmt.Errorf("invalid json file %q: %v", f, err)
	}
	// First try and load this as a service account config, which allows us to see the service account email:
	if cfg, err := goauth.JWTConfigFromJSON(all, proxy.SQLScope); err == nil {
		logging.Infof("using credential file for authentication; email=%s", cfg.Email)
		return cfg.Client(ctx), nil
	}

	cred, err := goauth.CredentialsFromJSON(ctx, all, proxy.SQLScope)
	if err != nil {
		return nil, fmt.Errorf("invalid json file %q: %v", f, err)
	}
	logging.Infof("using credential file for authentication; path=%q", f)
	return oauth2.NewClient(ctx, cred.TokenSource), nil
}

func authenticatedClientFromJson(ctx context.Context, json string) (*http.Client, error) {
	all := []byte(json);
	// First try and load this as a service account config, which allows us to see the service account email:
	if cfg, err := goauth.JWTConfigFromJSON(all, proxy.SQLScope); err == nil {
		logging.Infof("using credential json for authentication; email=%s", cfg.Email)
		return cfg.Client(ctx), nil
	}

	cred, err := goauth.CredentialsFromJSON(ctx, all, proxy.SQLScope)
	if err != nil {
		return nil, fmt.Errorf("invalid json: %v", err)
	}
	logging.Infof("using credential json for authentication")
	return oauth2.NewClient(ctx, cred.TokenSource), nil
}

func authenticatedClient(ctx context.Context) (*http.Client, error) {
	if tokenJson != "" {
		return authenticatedClientFromJson(ctx, tokenJson)
	} else if tokenFile != "" {
		return authenticatedClientFromPath(ctx, tokenFile)
	} else if tok := token; tok != "" {
		src := oauth2.StaticTokenSource(&oauth2.Token{AccessToken: tok})
		return oauth2.NewClient(ctx, src), nil
	} else if f := os.Getenv("GOOGLE_APPLICATION_CREDENTIALS"); f != "" {
		return authenticatedClientFromPath(ctx, f)
	}

	// If flags or env don't specify an auth source, try either gcloud or application default
	// credentials.
	src, err := util.GcloudTokenSource(ctx)
	if err != nil {
		src, err = goauth.DefaultTokenSource(ctx, proxy.SQLScope)
	}
	if err != nil {
		return nil, err
	}

	return oauth2.NewClient(ctx, src), nil
}

func stringList(s string) []string {
	spl := strings.Split(s, ",")
	if len(spl) == 1 && spl[0] == "" {
		return nil
	}
	return spl
}

func listInstances(ctx context.Context, cl *http.Client, projects []string) ([]string, error) {
	if len(projects) == 0 {
		// No projects requested.
		return nil, nil
	}

	sql, err := sqladmin.New(cl)
	if err != nil {
		return nil, err
	}
	if host != "" {
		sql.BasePath = host
	}

	ch := make(chan string)
	var wg sync.WaitGroup
	wg.Add(len(projects))
	for _, proj := range projects {
		proj := proj
		go func() {
			err := sql.Instances.List(proj).Pages(ctx, func(r *sqladmin.InstancesListResponse) error {
				for _, in := range r.Items {
					// The Proxy is only support on Second Gen
					if in.BackendType == "SECOND_GEN" {
						ch <- fmt.Sprintf("%s:%s:%s", in.Project, in.Region, in.Name)
					}
				}
				return nil
			})
			if err != nil {
				logging.Errorf("Error listing instances in %v: %v", proj, err)
			}
			wg.Done()
		}()
	}
	go func() {
		wg.Wait()
		close(ch)
	}()
	var ret []string
	for x := range ch {
		ret = append(ret, x)
	}
	if len(ret) == 0 {
		return nil, fmt.Errorf("no Cloud SQL Instances found in these projects: %v", projects)
	}
	return ret, nil
}

func gcloudProject() ([]string, error) {
	cfg, err := util.GcloudConfig()
	if err != nil {
		return nil, err
	}
	if cfg.Configuration.Properties.Core.Project == "" {
		return nil, fmt.Errorf("gcloud has no active project, you can set it by running `gcloud config set project <project>`")
	}
	return []string{cfg.Configuration.Properties.Core.Project}, nil
}

func main() {}

//export Echo
func Echo(message *C.char)(*C.char) {
	return C.CString(fmt.Sprintf("From DLL: %s", C.GoString(message)))
}

//export StartProxyWithCredentialFile
func StartProxyWithCredentialFile(_instances *C.char, _tokenFile *C.char) {
	StartProxy(_instances, _tokenFile, C.CString(""));
}

//export StartProxyWithCredentialJson
func StartProxyWithCredentialJson(_instances *C.char, _tokenJson *C.char) {
	StartProxy(_instances, C.CString(""), _tokenJson);
}

func StartProxy(_instances *C.char, _tokenFile *C.char, _tokenJson *C.char) {
	SetStatus("connecting")

	instances = C.GoString(_instances)
	tokenFile = C.GoString(_tokenFile)
	tokenJson = C.GoString(_tokenJson)

	if version {
		fmt.Println("Cloud SQL Proxy:", versionString)
		return
	}

	if logDebugStdout {
		logging.LogDebugToStdout()
	}

	if !verbose {
		logging.LogVerboseToNowhere()
	}

	if quiet {
		log.Println("Cloud SQL Proxy logging has been disabled by the -quiet flag. All messages (including errors) will be suppressed.")
		log.SetFlags(0)
		log.SetOutput(ioutil.Discard)
	}

	// Split the input ipAddressTypes to the slice of string
	ipAddrTypeOptsInput := strings.Split(ipAddressTypes, ",")

	if host != "" && !strings.HasSuffix(host, "/") {
		logging.Errorf("Flag host should always end with /")
		flag.PrintDefaults()
		SetStatus("disconnected")
		return
	}

	// TODO: needs a better place for consolidation
	// if instances is blank and env var INSTANCES is supplied use it
	if envInstances := os.Getenv("INSTANCES"); instances == "" && envInstances != "" {
		instances = envInstances
	}

	instList := stringList(instances)
	projList := stringList(projects)

	// TODO: it'd be really great to consolidate flag verification in one place.
	if len(instList) == 0 && instanceSrc == "" && len(projList) == 0 && !useFuse {
		var err error
		projList, err = gcloudProject()
		if err == nil {
			logging.Infof("Using gcloud's active project: %v", projList)
		} else if gErr, ok := err.(*util.GcloudError); ok && gErr.Status == util.GcloudNotFound {
			SetStatus("disconnected")
			log.Fatalf("gcloud is not in the path and -instances and -projects are empty")
		} else {
			SetStatus("disconnected")
			log.Fatalf("unable to retrieve the active gcloud project and -instances and -projects are empty: %v", err)
		}
	}

	onGCE := metadata.OnGCE()
	if err := checkFlags(onGCE); err != nil {
		SetStatus("disconnected")
		log.Fatal(err)
	}

	ctx := context.Background()
	client, err := authenticatedClient(ctx)
	if err != nil {
		SetStatus("disconnected")
		log.Fatal(err)
	}

	ins, err := listInstances(ctx, client, projList)
	if err != nil {
		SetStatus("disconnected")
		log.Fatal(err)
	}
	instList = append(instList, ins...)
	cfgs, err := CreateInstanceConfigs(dir, useFuse, instList, instanceSrc, client)
	if err != nil {
		SetStatus("disconnected")
		log.Fatal(err)
	}

	// We only need to store connections in a ConnSet if FUSE is used; otherwise
	// it is not efficient to do so.
	var connset *proxy.ConnSet

	// Initialize a source of new connections to Cloud SQL instances.
	var connSrc <-chan proxy.Conn
	if useFuse {
		connset = proxy.NewConnSet()
		c, fuse, err := fuse.NewConnSrc(dir, fuseTmp, connset)
		if err != nil {
			SetStatus("disconnected")
			log.Fatalf("Could not start fuse directory at %q: %v", dir, err)
		}
		connSrc = c
		defer fuse.Close()
	} else {
		updates := make(chan string)
		if instanceSrc != "" {
			go func() {
				for {
					err := metadata.Subscribe(instanceSrc, func(v string, ok bool) error {
						if ok {
							updates <- v
						}
						return nil
					})
					if err != nil {
						SetStatus("disconnected")
						logging.Errorf("Error on receiving new instances from metadata: %v", err)
					}
					time.Sleep(5 * time.Second)
				}
			}()
		}

		c, err := WatchInstances(dir, cfgs, updates, client)
		if err != nil {
			SetStatus("disconnected")
			log.Fatal(err)
		}
		connSrc = c
	}

	refreshCfgThrottle := refreshCfgThrottle
	if refreshCfgThrottle < minimumRefreshCfgThrottle {
		refreshCfgThrottle = minimumRefreshCfgThrottle
	}
	logging.Infof("Ready for new connections")
	SetStatus("connected")

	proxyClient := &proxy.Client{
		Port:           port,
		MaxConnections: maxConnections,
		Certs: certs.NewCertSourceOpts(client, certs.RemoteOpts{
			APIBasePath:    host,
			IgnoreRegion:   !checkRegion,
			UserAgent:      userAgentFromVersionString(),
			IPAddrTypeOpts: ipAddrTypeOptsInput,
		}),
		Conns:              connset,
		RefreshCfgThrottle: refreshCfgThrottle,
	}

	signals := make(chan os.Signal, 1)
	signal.Notify(signals, syscall.SIGTERM, syscall.SIGINT)

	go func() {
		<-signals
		logging.Infof("Received TERM signal. Waiting up to %s before terminating.", termTimeout)
		SetStatus("disconnected")

		proxyClient.Shutdown(termTimeout)
	}()

	proxyClient.Run(connSrc)
}

//export StopProxy
func StopProxy() {
	SetStatus("disconnected")
	logging.Infof("Stopping proxy. Waiting up to %s before terminating.", termTimeout)
	proxyClient.Shutdown(termTimeout)
}

//export GetStatus
func GetStatus()(*C.char)  {
	return C.CString(status)
}

//export SetCallback
func SetCallback(cb C.callbackFunc) {
	// This function stores the callback function pointer above as a global variable
	// the function pointer comes from the C# wrapper
	g_cb = cb
}
/*
* This GO function is used to set the state of the proxy connection.
* It calls invokeFunctionPointer from extern.c, it passes it the stored function pointer
* of the delegate from the C# wrapper
*/
func SetStatus(s string) {
	status = s
	C.invokeFunctionPointer(g_cb, C.CString(s));
}
