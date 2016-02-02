#if DNXCORE50 || DOTNET5_4
namespace System.Transactions
{
    public class Transaction
    {
        public static Transaction Current
        {
            get
            {
                return null;
            }
        }
    }

    public class TransactionScope : IDisposable
    {
        public void Dispose()
        {
            GC.SuppressFinalize(this);
        }

        public void Complete()
        {
        }
    }
}
#endif