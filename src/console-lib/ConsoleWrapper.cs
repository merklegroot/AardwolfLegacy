using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace console_lib
{
    /// <summary>
    /// Adds a little bit of thread-safety to the Console.
    /// </summary>
    public static class ConsoleWrapper
    {
        private static object ConsoleLocker = new object();

        private static object QueueLocker = new object();
        private static Queue<string> _queue = new Queue<string>();

        public static void WriteLine()
        {
            WriteLine(string.Empty);
        }

        public static void WriteLine(Exception exception)
        {
            WriteLine(exception.Message);
        }

        public static void WriteLine(string contents)
        {
            lock (QueueLocker)
            {
                if (_queue.Count < 1000)
                {
                    _queue.Enqueue(contents);
                }
            }

            ProcessQueue();
        }

        private static object ProcessingCheckLocker = new object();
        private static bool IsProcessing = false;
        private static void ProcessQueue()
        {
            lock (ProcessingCheckLocker)
            {
                if (IsProcessing) { return; }
                IsProcessing = true;
            }

            var queueTask = new Task(() =>
            {
                try
                {
                    while (true)
                    {
                        string currentItem = null;
                        lock (QueueLocker)
                        {
                            if (_queue.Any())
                            {
                                currentItem = _queue.Dequeue();
                            }
                            else
                            {
                                lock (ProcessingCheckLocker)
                                {
                                    IsProcessing = false;
                                    return;
                                }
                            }
                        }

                        Console.WriteLine(currentItem);
                    }
                }
                catch
                {
                    lock (ProcessingCheckLocker)
                    {
                        IsProcessing = false;
                    }

                    throw;
                }
            }, TaskCreationOptions.LongRunning);

            queueTask.Start();
        }

        public static ConsoleKeyInfo ReadKey(bool intercept)
        {
            return Console.ReadKey(intercept);
        }

        public static ConsoleKeyInfo ReadKey()
        {
            return Console.ReadKey();
        }
    }
}
