using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.Services
{

    //these service us basically to keep a gate so that the backgrund service waits untill the bacfilling is completed
    public class SeedingGate
    {
        private readonly TaskCompletionSource _tcs =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

      
        public Task WaitUntilReadyAsync(CancellationToken ct = default)
        {
            return _tcs.Task.WaitAsync(ct);
        }

   
        public void SignalReady()
        {
            _tcs.TrySetResult();
        }

        public bool IsReady => _tcs.Task.IsCompleted;
    }
}
