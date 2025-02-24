using System;

namespace Expert1.CloudSqlProxy
{
    internal static class Utilities
    {
        public const string UserAgent = "cloud_sql_proxy/1.35.4";

        public static (string project, string region, string name) SplitName(string instance)
        {
            ReadOnlySpan<char> span = instance.AsSpan();
            int firstColon = span.IndexOf(':');
            if (firstColon == -1)
            {
                return ("", "", instance);
            }

            int secondColon = span[(firstColon + 1)..].IndexOf(':');
            if (secondColon != -1)
            {
                secondColon += firstColon + 1;
            }

            int dotIndex = span[..firstColon].IndexOf('.');

            if (dotIndex != -1 && secondColon != -1)
            {
                // Handle case where first segment contains a dot and there are two colons
                string project = new(span[..secondColon]);
                int thirdColon = span[(secondColon + 1)..].IndexOf(':');
                if (thirdColon != -1)
                {
                    thirdColon += secondColon + 1;
                    string region = new(span.Slice(secondColon + 1, thirdColon - secondColon - 1));
                    string name = new(span[(thirdColon + 1)..]);
                    return (project, region, name);
                }
                else
                {
                    string region = new(span[(secondColon + 1)..]);
                    return (project, region, "");
                }
            }
            else if (secondColon == -1)
            {
                string project = new(span[..firstColon]);
                string name = new(span[(firstColon + 1)..]);
                return (project, "", name);
            }
            else
            {
                string project = new(span[..firstColon]);
                string region = new(span.Slice(firstColon + 1, secondColon - firstColon - 1));
                string name = new(span[(secondColon + 1)..]);
                return (project, region, name);
            }
        }

        public static string ExtractBase64FromPem(string pemString)
        {
            const string header = "-----BEGIN CERTIFICATE-----";
            const string footer = "-----END CERTIFICATE-----";

            ReadOnlySpan<char> pemSpan = pemString.AsSpan();

            int start = pemSpan.IndexOf(header.AsSpan());
            if (start == -1)
            {
                throw new ArgumentException("Invalid PEM format");
            }

            int end = pemSpan[(start + header.Length)..].IndexOf(footer.AsSpan());
            if (end == -1)
            {
                throw new ArgumentException("Invalid PEM format");
            }

            end += start + header.Length;

            ReadOnlySpan<char> base64Span = pemSpan.Slice(start + header.Length, end - start - header.Length);
            return base64Span.Trim().ToString();
        }
    }
}
