using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using Prism.Services.Dialogs;

namespace SocketDebugger.Utils
{
    public static class MethodExtensions
    {
        public static bool IsHex(this string s)
        {
            return new Regex(@"[A-Fa-f0-9]+$").IsMatch(s);
        }

        public static bool IsNumber(this string s)
        {
            return new Regex(@"^\d+$").IsMatch(s);
        }

        /// <summary>
        /// 显示普通提示对话框
        /// </summary>
        public static void ShowAlertMessageDialog(this string message, IDialogService dialogService, AlertType type)
        {
            dialogService.ShowDialog("AlertMessageDialog", new DialogParameters
                {
                    { "AlertType", type }, { "Message", message }
                },
                delegate { }
            );
        }

        /// <summary>
        /// 将需要发送的Hex序列化为byte[]
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        public static byte[] GetBytesWithUtf8(this string message)
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
            return Encoding.UTF8.GetBytes(message);
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