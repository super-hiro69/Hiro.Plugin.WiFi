using Hiro.Plugin.Services;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace Hiro.Plugin.Official.WiFi
{
    internal class HWiFiHelper
    {
        internal static bool isDebug(IHiroService? _service)
        {
            var _list = _service?.GetData("Hiro.DFlag", null);
            if (_list == null || _list.Count == 0)
                return false;
            return _list[0] as bool? ?? false;
        }

        internal static string getLang(IHiroService? _service)
        {
            var _list = _service?.GetData("Hiro.Lang", null);
            if (_list == null || _list.Count == 0)
                return string.Empty;
            return _list[0].ToString() ?? string.Empty;
        }

        internal static string Get_Translate(IHiroService? _service, string key)
        {
            var _list = _service?.GetData("Hiro.Lang", null);
            if (_list == null || _list.Count == 0)
                return "<???>";
            return Read_Ini(_list[0].ToString() ?? "Default", key, Read_Ini("Default", key, "<???>"));
        }

        internal static string Get_Translate(string section, string key)
        {
            return Read_Ini(section, key, Read_Ini("Default", key, "<???>"));
        }


        [DllImport("kernel32")]//返回取得字符串缓冲区的长度
        internal static extern int GetPrivateProfileString(byte[] section, byte[] key, byte[] def, byte[] retVal, int size, string filePath);

        internal static string Read_Ini(string Section, string Key, string defaultText)
        {
            var iniFilePath = AppDomain.CurrentDomain.BaseDirectory + "\\lang\\lang.hlp";
            if (File.Exists(iniFilePath))
            {
                byte[] buffer = new byte[1024];
                int ret = GetPrivateProfileString(Encoding.GetEncoding("utf-8").GetBytes(Section), Encoding.GetEncoding("utf-8").GetBytes(Key), Encoding.GetEncoding("utf-8").GetBytes(defaultText), buffer, 1024, iniFilePath);
                return DeleteUnVisibleChar(Encoding.GetEncoding("utf-8").GetString(buffer, 0, ret)).Trim();
            }
            else
            {
                return defaultText;
            }
        }

        internal static string DeleteUnVisibleChar(string sourceString)
        {
            StringBuilder sBuilder = new(131);
            for (int i = 0; i < sourceString.Length; i++)
            {
                int Unicode = sourceString[i];
                if (Unicode >= 16)
                {
                    sBuilder.Append(sourceString[i]);
                }
            }
            return sBuilder.ToString();
        }
    }
}
