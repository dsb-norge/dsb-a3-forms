using System.Collections.Generic;
using System.Threading.Tasks;

namespace DsbA3Forms.Clients
{
    public interface IBringClient
    {
        public Task<string> GetCity(string postalCode);
    }
}