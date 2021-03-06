﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace WoL.Extensions
{
    public static class TaskExtensions
    {
        // see https://github.com/davidfowl/AspNetCoreDiagnosticScenarios/blob/master/AsyncGuidance.md#always-dispose-cancellationtokensources-used-for-timeouts
        public static async Task TimeoutAfter(this Task task, TimeSpan timeout)
        {
            if (task == null)
                throw new ArgumentNullException(nameof(task));
            using var cts = new CancellationTokenSource();
            var delayTask = Task.Delay(timeout, cts.Token);

            var resultTask = await Task.WhenAny(task, delayTask).ConfigureAwait(false);
            if (resultTask == delayTask)
            {
                // Operation cancelled
                throw new OperationCanceledException();
            }
            else
            {
                // Cancel the timer task so that it does not fire
                cts.Cancel();
            }

            await task.ConfigureAwait(false);
        }

        // see https://github.com/davidfowl/AspNetCoreDiagnosticScenarios/blob/master/AsyncGuidance.md#always-dispose-cancellationtokensources-used-for-timeouts
        public static async Task<T> WithCancellation<T>(this Task<T> task, CancellationToken cancellationToken)
        {
            if (task == null)
                throw new ArgumentNullException(nameof(task));
            var tcs = new TaskCompletionSource<object>(TaskCreationOptions.RunContinuationsAsynchronously);

            // This disposes the registration as soon as one of the tasks trigger
            await using (cancellationToken.Register(state =>
            {
                ((TaskCompletionSource<object>)state)!.TrySetResult(null);
            },
            tcs))
            {
                var resultTask = await Task.WhenAny(task, tcs.Task).ConfigureAwait(false);
                if (resultTask == tcs.Task)
                {
                    // Operation cancelled
                    throw new OperationCanceledException(cancellationToken);
                }

                return await task.ConfigureAwait(false);
            }
        }
    }
}
