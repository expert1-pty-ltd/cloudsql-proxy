using System;

namespace cloudsql_proxy_cs
{
    /// <summary>
    /// An exception thrown when a method is called that requires the proxy to be connected.
    /// </summary>
    class ProxyNotConnectedException : Exception
    {
        public ProxyNotConnectedException() : base("The Proxy is not connected.")
        {
        }

    }
}
