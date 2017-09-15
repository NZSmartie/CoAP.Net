#region License
// Copyright 2017 Roman Vaughan (NZSmartie)
//  
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// 
//     http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
#endregion

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