using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.Runtime;
using System;
using System.Runtime.InteropServices;
using AcadApp = Autodesk.AutoCAD.ApplicationServices.Application;

using BS.CAD.Tools.Utils;

namespace BS.CAD.Tools
{
    public class CadApp : IExtensionApplication
    {
        public static IntPtr TargetChineseHKL = IntPtr.Zero;
        public static IntPtr TargetEnglishHKL = IntPtr.Zero;
        private static bool _lastNeedsChineseInput = false;

        public static string SelectedShx = "txt.shx";
        public static string SelectedBigFont = "gbcbig.shx";

        [DllImport("user32.dll")]
        private static extern bool PostMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);
        [DllImport("user32.dll")]
        private static extern IntPtr GetKeyboardLayout(uint idThread);
        [DllImport("user32.dll")]
        private static extern int GetKeyboardLayoutList(int nBuff, [Out] IntPtr[] lpList);

        private const uint WM_INPUTLANGCHANGEREQUEST = 0x0050;

        public void Initialize()
        {
            TraceLog.Step("CadApp.Initialize: ENTER");

            DetectIMEs();

            AppDomain.CurrentDomain.UnhandledException += (s, e) =>
            {
                var ex = e.ExceptionObject as System.Exception;
                string msg = $"未捕获异常: {ex?.GetType().Name}\n{ex?.Message}\n{ex?.StackTrace}";
                Logger.Error(msg);
                TraceLog.Step($"AppDomain.UnhandledException: {msg}");
                try { System.Windows.Forms.MessageBox.Show(msg, "CAD助手 致命错误", System.Windows.Forms.MessageBoxButtons.OK, System.Windows.Forms.MessageBoxIcon.Error); } catch { }
            };

            AcadApp.DocumentManager.DocumentLockModeChanged += DocumentManager_DocumentLockModeChanged;
            AcadApp.Idle += AcadApp_Idle;
            CadImeBridge.Notify("PluginReady", "en", "CAD 插件已连接");
            Logger.Info("插件已初始化，输入法监听已启动");
            TraceLog.Step("CadApp.Initialize: DONE");
        }

        private void DetectIMEs()
        {
            try
            {
                int count = GetKeyboardLayoutList(0, Array.Empty<IntPtr>());
                if (count > 0)
                {
                    IntPtr[] layouts = new IntPtr[count];
                    GetKeyboardLayoutList(count, layouts);

                    foreach (var layout in layouts)
                    {
                        long val = layout.ToInt64();
                        if ((val & 0xFFFF) == 0x0804 && TargetChineseHKL == IntPtr.Zero)
                            TargetChineseHKL = layout;
                        if ((val & 0xFFFF) == 0x0409 && TargetEnglishHKL == IntPtr.Zero)
                            TargetEnglishHKL = layout;
                    }

                    if (TargetEnglishHKL == IntPtr.Zero && layouts.Length > 0)
                        TargetEnglishHKL = layouts[0];

                    Logger.Info($"输入法检测完成: 中文={TargetChineseHKL}, 英文={TargetEnglishHKL}");
                }
            }
            catch (System.Exception ex) { Logger.Error("DetectIMEs失败: " + ex.Message); }
        }

        public void Terminate()
        {
            try
            {
                AcadApp.DocumentManager.DocumentLockModeChanged -= DocumentManager_DocumentLockModeChanged;
                AcadApp.Idle -= AcadApp_Idle;
                CadImeBridge.Notify("PluginDisconnected", "en", "CAD 插件已断开");
                Logger.Info("插件已卸载，输入法监听已停止");
            }
            catch (System.Exception ex) { Logger.Error(ex); }
        }

        private void AcadApp_Idle(object? sender, EventArgs e)
        {
            try
            {
                string cmdNames = AcadApp.GetSystemVariable("CMDNAMES")?.ToString() ?? "";
                if (string.IsNullOrEmpty(cmdNames) && _lastNeedsChineseInput)
                {
                    if (!CadImeBridge.Notify("TextEditEnded", "en", "CAD 命令模式"))
                    {
                        SwitchToIME(TargetEnglishHKL);
                    }

                    _lastNeedsChineseInput = false;
                }
            }
            catch (System.Exception ex) { Logger.Error(ex); }
        }

        private void DocumentManager_DocumentLockModeChanged(object sender, DocumentLockModeChangedEventArgs e)
        {
            string cmd = e.GlobalCommandName?.ToUpper() ?? "";
            if (NeedsChineseInput(cmd))
            {
                _lastNeedsChineseInput = true;
                if (!CadImeBridge.Notify("TextEditStarted", "zh", GetChineseInputMode(cmd)))
                {
                    SwitchToIME(TargetChineseHKL);
                }
            }
        }

        private static bool NeedsChineseInput(string cmd)
        {
            if (string.IsNullOrWhiteSpace(cmd)) return false;

            return IsTextCommand(cmd)
                   || IsAttributeCommand(cmd)
                   || IsMLeaderCommand(cmd)
                   || IsNamingCommand(cmd)
                   || IsTableOrFieldCommand(cmd)
                   || IsDimensionTextCommand(cmd)
                   || IsPropertiesCommand(cmd);
        }

        private static string GetChineseInputMode(string cmd)
        {
            if (IsAttributeCommand(cmd)) return "CAD 块属性编辑";
            if (IsMLeaderCommand(cmd)) return "CAD 多重引线编辑";
            if (IsNamingCommand(cmd)) return "CAD 名称/说明编辑";
            if (IsTableOrFieldCommand(cmd)) return "CAD 表格/字段编辑";
            if (IsDimensionTextCommand(cmd)) return "CAD 标注文字编辑";
            if (IsPropertiesCommand(cmd)) return "CAD 特性编辑";
            return "CAD 文字编辑";
        }

        private static bool IsTextCommand(string cmd)
        {
            return cmd == "T"
                   || cmd == "TEXT"
                   || cmd == "DTEXT"
                   || cmd == "MTEXT"
                   || cmd == "MTEDIT"
                   || cmd == "DDEDIT"
                   || cmd == "ED"
                   || cmd.Contains("TEXT");
        }

        private static bool IsAttributeCommand(string cmd)
        {
            return cmd == "ATTEDIT"
                   || cmd == "ATTIPEDIT"
                   || cmd == "EATTEDIT"
                   || cmd == "DDATTE"
                   || cmd == "BATTMAN"
                   || cmd == "BATTORDER"
                   || cmd.Contains("ATTRIB")
                   || cmd.Contains("ATTDEF");
        }

        private static bool IsMLeaderCommand(string cmd)
        {
            return cmd == "MLEADER"
                   || cmd == "MLEADEREDIT"
                   || cmd == "MLEADERSTYLE"
                   || cmd.Contains("MLEADER");
        }

        private static bool IsNamingCommand(string cmd)
        {
            return cmd == "LAYER"
                   || cmd == "CLASSICLAYER"
                   || cmd == "LA"
                   || cmd == "RENAME"
                   || cmd == "-RENAME"
                   || cmd == "BLOCK"
                   || cmd == "-BLOCK"
                   || cmd == "BEDIT"
                   || cmd == "REFEDIT"
                   || cmd == "WBLOCK"
                   || cmd == "STYLE"
                   || cmd == "DIMSTYLE"
                   || cmd == "LAYOUT"
                   || cmd == "UCSMAN"
                   || cmd.Contains("LAYER")
                   || cmd.Contains("BLOCK")
                   || cmd.Contains("STYLE");
        }

        private static bool IsTableOrFieldCommand(string cmd)
        {
            return cmd == "TABLE"
                   || cmd == "TABLEDIT"
                   || cmd == "TABLESTYLE"
                   || cmd == "FIELD"
                   || cmd == "UPDATEFIELD"
                   || cmd.Contains("TABLE")
                   || cmd.Contains("FIELD");
        }

        private static bool IsDimensionTextCommand(string cmd)
        {
            return cmd == "DIM"
                   || cmd == "DIMEDIT"
                   || cmd == "DIMTEDIT"
                   || cmd == "QDIM"
                   || cmd.Contains("DIM");
        }

        private static bool IsPropertiesCommand(string cmd)
        {
            return cmd == "PROPERTIES"
                   || cmd == "PR"
                   || cmd == "CHPROP"
                   || cmd == "CHANGE"
                   || cmd == "MATCHPROP"
                   || cmd.Contains("PROPERTIES");
        }

        public static void SwitchToIME(IntPtr hKL)
        {
            if (hKL == IntPtr.Zero) return;
            try { PostMessage(AcadApp.MainWindow.Handle, WM_INPUTLANGCHANGEREQUEST, IntPtr.Zero, hKL); } catch (System.Exception ex) { Logger.Error(ex); }
        }
    }
}
