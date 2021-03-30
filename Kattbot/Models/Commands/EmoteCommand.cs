using Kattbot.Services;
using System;
using System.Threading.Tasks;

namespace Kattbot.Models.Commands
{
    public abstract class EmoteCommand
    {
        private const int MaxRetryAttempts = 3;
        private const int RetryDelay = 10;

        public abstract Task ExecuteAsync(EmoteCommandReceiver emoteCommandReceiver);

        protected async Task ExecuteWithRetry(Func<Task<bool>> func)
        {
            var attemptCount = 0;

            while (attemptCount < MaxRetryAttempts)
            {
                var success = await func();

                if (success)
                    break;

                attemptCount++;

                await Task.Delay(RetryDelay);
            }
        }
    }
}
