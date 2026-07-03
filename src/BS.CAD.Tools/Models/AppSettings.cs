using System.Collections.Generic;

namespace BS.CAD.Tools.Models
{
    public class AppSettings
    {
        public int SettingsSchemaVersion { get; set; }

        public Dictionary<string, bool> EnabledModules { get; set; } = new();

        /// <summary>
        /// 用户选择的中文输入法 Handle（十六进制字符串，如 "0x08040804"）
        /// </summary>
        public string? TargetChineseHkl { get; set; }

        /// <summary>
        /// 用户选择的英文键盘 Handle（十六进制字符串）
        /// </summary>
        public string? TargetEnglishHkl { get; set; }

        /// <summary>
        /// 中文输入法显示名称（用于在列表恢复时匹配）
        /// </summary>
        public string? ChineseImeName { get; set; }

        /// <summary>
        /// 英文键盘显示名称
        /// </summary>
        public string? EnglishImeName { get; set; }
    }
}
