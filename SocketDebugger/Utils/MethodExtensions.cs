using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using Newtonsoft.Json;

namespace SocketDebugger.Utils
{
    public static class MethodExtensions
    {
        public static bool IsHex(this string command)
        {
            return new Regex(@"[A-Fa-f0-9]+$").IsMatch(command);
        }

        public static bool IsNumber(this string s)
        {
            return new Regex(@"^\d+$").IsMatch(s);
        }

        /// <summary>
        /// 将需要发送的Hex序列化为byte[]
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        public static (string, byte[]) GetBytesWithUtf8(this string message)
        {
            if (message.Contains(" "))
            {
                message = message.Replace(" ", "");
            }
            else if (message.Contains("-"))
            {
                message = message.Replace("-", "");
            }

            //以UTF-8的编码同步发送字符串
            return (message, Encoding.UTF8.GetBytes(message));
        }

        /// <summary>
        /// 格式化Hex字符串
        /// </summary>
        /// <returns></returns>
        public static string FormatHexString(this string command)
        {
            var builder = new StringBuilder();
            for (var i = 0; i < command.Length; i += 2)
            {
                var hex = command.Substring(i, 2);
                if (i == command.Length - 2)
                {
                    builder.Append(hex);
                }
                else
                {
                    builder.Append(hex).Append(" ");
                }
            }

            return builder.ToString();
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