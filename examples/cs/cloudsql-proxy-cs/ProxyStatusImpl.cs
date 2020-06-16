using Grpc.Core;
using System.Threading.Tasks;
using System;

namespace cloudsql_proxy_cs
{
    /// <summary>
    /// Implementation of proxy status gRPC Server
    /// </summary>
    public partial class ProxyStatusImpl: ProxyStatus.ProxyStatusBase
    {
        /// <summary>
        /// Sets the status of the proxy
        /// </summary>
        /// <param name="request">status request</param>
        /// <param name="context">server context</param>
        /// <returns></returns>
        public override Task<ProxyStatusReply> SetProxyStatus(ProxyStatusRequest request, ServerCallContext context)
        {
            // get the current static singleton instance of the proxy
            var proxy = Proxy.GetInstance();

            // if the callback is valid
            if (proxy.IsCallbackValid(request.Id))
            {
                // set the status based on the instance provided in the callbak
                proxy.SetStatus(request.Instance, request.Status, request.Error);

                // return the response
                return Task.FromResult(new ProxyStatusReply { Success = true });
            } else {
                return Task.FromResult(new ProxyStatusReply { Success = false });
            }
        }
    }
}