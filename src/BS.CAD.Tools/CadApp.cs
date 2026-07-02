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
        private static bool _lastWasText = false;

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
                if (string.IsNullOrEmpty(cmdNames) && _lastWasText)
                {
                    if (!CadImeBridge.Notify("TextEditEnded", "en", "CAD 命令模式"))
                    {
                        SwitchToIME(TargetEnglishHKL);
                    }

                    _lastWasText = false;
                }
            }
            catch (System.Exception ex) { Logger.Error(ex); }
        }

        private void DocumentManager_DocumentLockModeChanged(object sender, DocumentLockModeChangedEventArgs e)
        {
            string cmd = e.GlobalCommandName?.ToUpper() ?? "";
            if (cmd.Contains("TEXT") || cmd.Contains("ATTEDIT") || cmd.Contains("MTEDIT") || cmd == "T" || cmd == "MTEXT")
            {
                _lastWasText = true;
                if (!CadImeBridge.Notify("TextEditStarted", "zh", "CAD 文字编辑"))
                {
                    SwitchToIME(TargetChineseHKL);
                }
            }
        }

        public static void SwitchToIME(IntPtr hKL)
        {
            if (hKL == IntPtr.Zero) return;
            try { PostMessage(AcadApp.MainWindow.Handle, WM_INPUTLANGCHANGEREQUEST, IntPtr.Zero, hKL); } catch (System.Exception ex) { Logger.Error(ex); }
        }
    }
}
