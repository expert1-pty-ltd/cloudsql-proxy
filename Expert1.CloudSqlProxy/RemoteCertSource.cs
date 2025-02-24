using Google;
using Google.Apis.SQLAdmin.v1beta4;
using Google.Apis.SQLAdmin.v1beta4.Data;
using System;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;

namespace Expert1.CloudSqlProxy
{
    /// <summary>
    /// Manages the retrieval and caching of certificates required for establishing
    /// secure connections to Google Cloud SQL instances. Handles RSA key generation,
    /// and fetching ephemeral certificates from the Cloud SQL Admin API.
    /// </summary>
    internal sealed class RemoteCertSource(SQLAdminService service)
    {
        private readonly object keyLock = new();
        private RSA privateKey;
        private readonly SQLAdminService service = service;

        private RSA GenerateKey()
        {
            lock (keyLock)
            {
                if (privateKey == null)
                {
                    privateKey = RSA.Create();
                    privateKey.KeySize = 2048;
                }
                return privateKey;
            }
        }

        public async Task<X509Certificate2> GetCertificateAsync(string instance)
        {
            RSA key = GenerateKey();
            (string project, string region, string name) = Utilities.SplitName(instance);
            string regionName = $"{region}~{name}";
            string publicKey = key.ExportSubjectPublicKeyInfoPem();
            GenerateEphemeralCertRequest generateCertRequest = new()
            {
                PublicKey = publicKey
            };

            ConnectResource.GenerateEphemeralCertRequest request = service.Connect.GenerateEphemeralCert(generateCertRequest, project, regionName);
            GenerateEphemeralCertResponse response = await RetryWithBackoffAsync(() => request.ExecuteAsync());
            using X509Certificate2 certificate = new(Convert.FromBase64String(Utilities.ExtractBase64FromPem(response.EphemeralCert.Cert)));
            using X509Certificate2 certWithKey = certificate.CopyWithPrivateKey(key);
            return new X509Certificate2(certWithKey.Export(X509ContentType.Pkcs12));
        }

        private static async Task<T> RetryWithBackoffAsync<T>(Func<Task<T>> action, int retries = 5)
        {
            TimeSpan baseBackoff = TimeSpan.FromMilliseconds(200);
            double backoffMultiplier = 1.618;

            for (int i = 0; i < retries; i++)
            {
                try
                {
                    return await action();
                }
                catch (Exception ex) when (IsRetryableException(ex))
                {
                    TimeSpan backoff = TimeSpan.FromMilliseconds(baseBackoff.TotalMilliseconds * Math.Pow(backoffMultiplier, i + 1));
                    await Task.Delay(backoff);
                }
            }
            return await action();
        }

        private static bool IsRetryableException(Exception ex)
        {
            if (ex is GoogleApiException gex)
            {
                return (int)gex.HttpStatusCode >= 500;
            }
            return false;
        }
    }
}
