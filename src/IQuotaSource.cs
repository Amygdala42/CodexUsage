using System;
using System.Threading;
using System.Threading.Tasks;

namespace CodexQuotaLite
{
    public interface IQuotaSource : IDisposable
    {
        Task<QuotaSnapshot> FetchAsync(CancellationToken cancellationToken);
    }
}
