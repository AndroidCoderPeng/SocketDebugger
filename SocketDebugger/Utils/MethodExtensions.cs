using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Newtonsoft.Json;

namespace SocketDebugger.Utils
{
    public static class MethodExtensions
    {
        private static readonly Dictionary<string, Uri> UriDictionary = new Dictionary<string, Uri>();

        private static bool IsUriExist(this string xamlName)
        {
            return UriDictionary.Any(
                keyValuePair => keyValuePair.Key.Equals(xamlName)
            );
        }

        public static Uri CreateUri(this string xamlName)
        {
            if (xamlName.IsUriExist())
            {
                return UriDictionary[xamlName];
            }

            var uri = new Uri("/Pages/" + xamlName + ".xaml", UriKind.Relative);
            UriDictionary[xamlName] = uri;
            return uri;
        }

        public static bool IsHex(this string s)
        {
            return new Regex(@"[A-Fa-f0-9]+$").IsMatch(s);
        }

        public static string ToJson(this object obj)
        {
            return JsonConvert.SerializeObject(obj);
        }

        public static string ToHexString(this IEnumerable<byte> bytes)
        {
            var builder = new StringBuilder();
            foreach (var t in bytes)
            {
                builder.Append($"{t:X2} ");
            }

            return builder.ToString().Trim();
        }
    }
}