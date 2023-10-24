using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using Newtonsoft.Json;

namespace SocketDebugger.Utils
{
    public static class MethodExtensions
    {
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