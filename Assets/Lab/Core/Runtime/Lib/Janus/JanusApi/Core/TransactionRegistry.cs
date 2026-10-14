using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace VaSiLi.Janus
{
    internal class TransactionRegistry
    {
        readonly Dictionary<string, TaskCompletionSource<JanusEnvelope>> _pending
            = new Dictionary<string, TaskCompletionSource<JanusEnvelope>>();
        readonly object _lock = new object();

        public Task<JanusEnvelope> Track(string transactionId)
        {
            var tcs = new TaskCompletionSource<JanusEnvelope>(TaskCreationOptions.RunContinuationsAsynchronously);
            lock (_lock) _pending[transactionId] = tcs;
            return tcs.Task;
        }

        public bool TryComplete(string transactionId, JanusEnvelope envelope)
        {
            TaskCompletionSource<JanusEnvelope> tcs;
            lock (_lock)
            {
                if (!_pending.TryGetValue(transactionId, out tcs)) return false;
                _pending.Remove(transactionId);
            }
            return tcs.TrySetResult(envelope);
        }

        public bool TryFail(string transactionId, Exception error)
        {
            TaskCompletionSource<JanusEnvelope> tcs;
            lock (_lock)
            {
                if (!_pending.TryGetValue(transactionId, out tcs)) return false;
                _pending.Remove(transactionId);
            }
            return tcs.TrySetException(error);
        }

        public bool Has(string transactionId)
        {
            lock (_lock) return _pending.ContainsKey(transactionId);
        }

        public void FailAll(Exception error)
        {
            List<TaskCompletionSource<JanusEnvelope>> all;
            lock (_lock)
            {
                all = new List<TaskCompletionSource<JanusEnvelope>>(_pending.Values);
                _pending.Clear();
            }
            foreach (var tcs in all) tcs.TrySetException(error);
        }
    }
}
