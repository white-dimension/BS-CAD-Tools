using System;
using System.IO;

namespace BS.CAD.Tools.Services
{
    /// <summary>
    /// 标准配置路径解析服务。
    /// 查找顺序：
    ///   1. 插件 DLL 所在目录下的 config\
    ///   2. external/BS-CAD-Standard/config\
    /// 未来将从以下路径读取 BS-CAD-Standard 的标准规则：
    ///   - BS_Layer_Standard.json
    ///   - BS_TextStyle_Standard.json
    ///   - BS_DimStyle_Standard.json
    ///   - BS_MLeader_Standard.json
    ///   - BS_Plot_Standard.json
    ///   - BS_Layer_Migration_Rules.json
    /// </summary>
    public static class ConfigPathService
    {
        /// <summary>
        /// 标准配置根目录名称
        /// </summary>
        public const string StandardConfigDir = "BS-CAD-Standard";

        /// <summary>
        /// 标准配置子目录名称（存放 JSON 标准规则）
        /// </summary>
        public const string ConfigSubDir = "config";

        /// <summary>
        /// 解析标准配置目录路径。
        /// 返回第一个找到的有效目录，如果都找不到则返回 null。
        /// </summary>
        public static string? ResolveConfigDirectory()
        {
            // 1. 插件 DLL 所在目录下的 config
            string assemblyDir = AppDomain.CurrentDomain.BaseDirectory;
            string localConfig = Path.Combine(assemblyDir, ConfigSubDir);
            if (Directory.Exists(localConfig))
                return localConfig;

            // 2. 尝试从插件 DLL 上一级找 external/BS-CAD-Standard/config
            string? parentDir = Path.GetDirectoryName(assemblyDir.TrimEnd('\\', '/'));
            if (parentDir != null)
            {
                // 向上查找项目根目录（src/BS.CAD.Tools/ 的上级）
                string externalConfig = Path.Combine(parentDir, "external", StandardConfigDir, ConfigSubDir);
                if (Directory.Exists(externalConfig))
                    return externalConfig;

                // 再向上一级
                string? grandParent = Path.GetDirectoryName(parentDir);
                if (grandParent != null)
                {
                    externalConfig = Path.Combine(grandParent, "external", StandardConfigDir, ConfigSubDir);
                    if (Directory.Exists(externalConfig))
                        return externalConfig;
                }
            }

            // 未找到
            return null;
        }

        /// <summary>
        /// 获取标准配置文件的完整路径。
        /// </summary>
        public static string? GetConfigFilePath(string fileName)
        {
            string? configDir = ResolveConfigDirectory();
            if (configDir == null) return null;

            string filePath = Path.Combine(configDir, fileName);
            return File.Exists(filePath) ? filePath : null;
        }

        /// <summary>
        /// 获取标准配置目录下的前 M 个文件列表（用于诊断输出）
        /// </summary>
        public static string[] ListAvailableConfigs()
        {
            string? configDir = ResolveConfigDirectory();
            if (configDir == null || !Directory.Exists(configDir))
                return Array.Empty<string>();

            try { return Directory.GetFiles(configDir, "*.json"); }
            catch { return Array.Empty<string>(); }
        }
    }
}
