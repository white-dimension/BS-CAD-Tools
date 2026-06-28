using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using AcadApp = Autodesk.AutoCAD.ApplicationServices.Application;

namespace BS.CAD.Tools.Services
{
    /// <summary>
    /// AutoCAD 事务辅助封装。
    /// 当前为占位类，后续封装常用 Transaction 操作模式。
    /// </summary>
    public static class CadTransactionService
    {
        /// <summary>
        /// 在执行操作前获取 DocumentLock
        /// </summary>
        public static DocumentLock? LockDocument()
        {
            var doc = AcadApp.DocumentManager.MdiActiveDocument;
            return doc?.LockDocument();
        }
    }
}
