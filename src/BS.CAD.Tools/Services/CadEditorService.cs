using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.EditorInput;
using AcadApp = Autodesk.AutoCAD.ApplicationServices.Application;

namespace BS.CAD.Tools.Services
{
    /// <summary>
    /// AutoCAD Editor 输出、错误提示、事务辅助。
    /// 当前为占位类，后续封装常用 Editor 操作。
    /// </summary>
    public static class CadEditorService
    {
        /// <summary>
        /// 获取当前文档的 Editor
        /// </summary>
        public static Editor? GetEditor()
        {
            return AcadApp.DocumentManager.MdiActiveDocument?.Editor;
        }

        /// <summary>
        /// 向命令行输出消息
        /// </summary>
        public static void WriteMessage(string message)
        {
            var ed = GetEditor();
            if (ed != null)
                ed.WriteMessage($"\n{message}");
        }
    }
}
