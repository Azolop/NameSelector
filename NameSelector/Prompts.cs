using System;

namespace NameSelector
{
    /// <summary>
    /// 界面层统一使用的用户提示文案。
    /// </summary>
    internal static class Prompt
    {
        /// <summary>读取名单失败时的提示。</summary>
        public static string LoadFailure(Exception ex)
        {
            return "读取 namelist.json 失败，程序将使用默认名单重新开始。\n\n" + ex.Message +
                   "\n\n提示：namelist.json 需为 UTF-8 编码，请不要用记事本以 ANSI 保存，也不要在运行中手动修改。";
        }

        /// <summary>保存名单失败时的提示。</summary>
        public static string SaveFailure(Exception ex)
        {
            return "保存 namelist.json 失败，本次改动可能未保存。\n\n" + ex.Message +
                   "\n\n提示：请确认程序所在目录可写（不要放在 C:\\Program Files、桌面或教学机只读目录）。";
        }

        /// <summary>保存周次表失败时的提示。</summary>
        public static string WeeklySaveFailure(Exception ex)
        {
            return "保存周次表失败，本次改动可能未保存。\n\n" + ex.Message +
                   "\n\n提示：请确认程序所在目录可写（不要放在 C:\\Program Files、桌面或教学机只读目录）。";
        }
    }
}
