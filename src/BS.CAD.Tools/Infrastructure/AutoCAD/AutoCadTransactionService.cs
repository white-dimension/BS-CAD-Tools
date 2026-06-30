using Autodesk.AutoCAD.DatabaseServices;

namespace BS.CAD.Tools.Infrastructure.AutoCAD
{
    public sealed class AutoCadTransactionService
    {
        public Transaction StartTransaction(Database database)
        {
            return database.TransactionManager.StartTransaction();
        }
    }
}
