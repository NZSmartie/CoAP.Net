using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace CoAPNet.Utils
{

    public class AsyncAutoResetEvent
    {
        private readonly Queue<TaskCompletionSource<bool>> _waits = new Queue<TaskCompletionSource<bool>>();
        private bool _signaled;

        public AsyncAutoResetEvent(bool signaled)
        {
            _signaled = signaled;
        }

        public async Task WaitAsync(CancellationToken token)
        {
            var tcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            lock (_waits)
            {
                if (_signaled)
                {
                    _signaled = false;
                    return;
                }

                _waits.Enqueue(tcs);
            }

            token.Register(() => tcs.TrySetCanceled(token));
            await tcs.Task;
        }


        public void Set()
        {
            TaskCompletionSource<bool> toRelease = null;
            lock (_waits)
            {
                do
                {
                    if (_waits.Count > 0)
                    {
                        toRelease = _waits.Dequeue();
                    }
                    else if (!_signaled)
                    {
                        toRelease = null;
                        _signaled = true;
                        break;
                    }
                } while (toRelease?.Task.IsCanceled ?? false);
            }

            toRelease?.SetResult(true);
        }
    }
}