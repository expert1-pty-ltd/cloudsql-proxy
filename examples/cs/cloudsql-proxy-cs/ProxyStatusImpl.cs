using Grpc.Core;
using System.Threading.Tasks;
using System;

namespace cloudsql_proxy_cs
{
    /// <summary>
    /// ProxyStatusImpl
    /// </summary>
    public partial class ProxyStatusImpl: ProxyStatus.ProxyStatusBase
    {
        /// <summary>
        /// SetStatus
        /// </summary>
        /// <param name="request"></param>
        /// <param name="context"></param>
        /// <returns></returns>
        public override Task<ProxyStatusReply> SetProxyStatus(ProxyStatusRequest request, ServerCallContext context)
        {
            var proxy = Proxy.GetInstance();

            proxy.SetStatus(request.Name, request.Status, request.Error);

            return Task.FromResult(new ProxyStatusReply { Success = true });
        }
    }
}