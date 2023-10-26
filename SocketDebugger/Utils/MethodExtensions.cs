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