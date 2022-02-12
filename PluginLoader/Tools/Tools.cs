using Steamworks;
using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;

namespace MEPluginLoader.Tools
{
    public static class Tools
    {
        public static readonly UTF8Encoding Utf8 = new UTF8Encoding();

        public static string Sha1HexDigest(string text)
        {
            using SHA1Managed sha1 = new SHA1Managed();
            byte[] buffer = Utf8.GetBytes(text);
            byte[] digest = sha1.ComputeHash(buffer);
            return BytesToHex(digest);
        }

        private static string BytesToHex(IReadOnlyCollection<byte> ba)
        {
            StringBuilder hex = new StringBuilder(2 * ba.Count);

            foreach (byte t in ba)
            {
                hex.Append(t.ToString("x2"));
            }

            return hex.ToString();
        }

        public static string FormatDateIso8601(DateTime dt)
        {
            return dt.ToString("s").Substring(0, 10);
        }

        public static ulong GetSteamId()
        {
            return SteamUser.GetSteamID().m_SteamID;
        }

        // FIXME: Replace this with the proper library call, I could not find one
        public static string FormatUriQueryString(Dictionary<string, string> parameters)
        {
            StringBuilder query = new StringBuilder();
            foreach (KeyValuePair<string, string> p in parameters)
            {
                if (query.Length > 0)
                {
                    query.Append('&');
                }

                query.Append($"{Uri.EscapeDataString(p.Key)}={Uri.EscapeDataString(p.Value)}");
            }
            return query.ToString();
        }
    }
}