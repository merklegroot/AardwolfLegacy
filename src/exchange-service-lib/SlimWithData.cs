using System;
using System.Threading;

namespace exchange_service_lib
{
    public class SlimWithData
    {
        private readonly ManualResetEventSlim _slim;
        private readonly TimeSpan _maxWaitTime;
        private readonly TimeSpan _maxRunTime;
        private readonly int _maxQueueLength;

        private bool _mustEmptyQueue = false;
        private int _queueLength = 0;

        public SlimWithData(
            TimeSpan maxWaitTime,
            TimeSpan maxRunTime,
            int maxQueueLength)
        {
            _maxWaitTime = maxWaitTime;
            _maxRunTime = maxRunTime;
            _maxQueueLength = maxQueueLength;

            _slim = new ManualResetEventSlim();
        }

        public T Execute<T>(Func<T> method)
        {
            if (_mustEmptyQueue)
            {
                if (_queueLength > 0)
                {
                    throw new ApplicationException("Queue was full. Waiting for all items to complete before allowing more to be enqueued.");
                }
                else
                {
                    _mustEmptyQueue = false;
                }
            }

            if (_queueLength >= _maxQueueLength)
            {
                _mustEmptyQueue = true;
                throw new ApplicationException("Max Queue length execeed.");
            }

            _queueLength++;
            var waitResult = _slim.Wait(_maxWaitTime);

            if (!waitResult)
            {
                _queueLength--;
                throw new ApplicationException("Took too long.");
            }
            try
            {
                return method();
            }
            finally
            {
                _queueLength--;
            }
        }
    }
}
