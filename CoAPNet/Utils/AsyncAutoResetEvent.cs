using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace CoAPNet.Utils
{
    // Source: https://stackoverflow.com/a/43012490/2255933
    // Edited and added AwaitAsync() without timespan parameter
    public class AsyncAutoResetEvent
    {
        private readonly LinkedList<TaskCompletionSource<bool>> _waiters =
            new LinkedList<TaskCompletionSource<bool>>();

        private bool _isSignaled;

        public AsyncAutoResetEvent(bool signaled)
        {
            _isSignaled = signaled;
        }

        public Task<bool> WaitAsync(TimeSpan timeout)
        {
            return WaitAsync(timeout, CancellationToken.None);
        }

        public async Task<bool> WaitAsync(TimeSpan timeout, CancellationToken cancellationToken)
        {
            TaskCompletionSource<bool> tcs;

            lock (_waiters)
            {
                if (_isSignaled)
                {
                    _isSignaled = false;
                    return true;
                }

                if (timeout == TimeSpan.Zero)
                    return _isSignaled;

                tcs = new TaskCompletionSource<bool>();
                _waiters.AddLast(tcs);

            }

            var winner = await Task.WhenAny(tcs.Task, Task.Delay(timeout, cancellationToken));

            // The task was signaled.
            if (winner == tcs.Task)
                return true;

            // We timed-out; remove our reference to the task.
            // This is an O(n) operation since waiters is a LinkedList<T>.
            lock (_waiters)
            {
                bool removed = _waiters.Remove(tcs);
                Debug.Assert(removed);
                return false;
            }
        }

        public Task<bool> WaitAsync(CancellationToken cancellationToken)
        {
            TaskCompletionSource<bool> tcs;

            lock (_waiters)
            {
                if (_isSignaled)
                {
                    _isSignaled = false;
                    return Task.FromResult(true);
                }

                tcs = new TaskCompletionSource<bool>();
                _waiters.AddLast(tcs);
                return Task.Run(() => tcs.Task.GetAwaiter().GetResult(), cancellationToken);
            }
        }

        public void Set()
        {
            TaskCompletionSource<bool> toRelease = null;

            lock (_waiters)
            {
                if (_waiters.Count > 0)
                {
                    // Signal the first task in the waiters list.
                    toRelease = _waiters.First.Value;
                    _waiters.RemoveFirst();
                }
                else if (!_isSignaled)
                {
                    // No tasks are pending
                    _isSignaled = true;
                }
            }

            toRelease?.SetResult(true);
        }
    }
}