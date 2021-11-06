using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace trade_lib.Cache
{
    [Obsolete]
    public class CacheRefresher : IDisposable
    {
        private readonly ISimpleWebCache _cache;
        private bool _keepRunning = false;

        private object _locker = new object();
        private List<string> _urls = new List<string>();

        public CacheRefresher(ISimpleWebCache cache)
        {
            _cache = cache;
        }

        public void RegisterUrl(string url)
        {
            lock (_locker)
            {
                _urls.Add(url);
            }
        }

        public void Start()
        {
            _keepRunning = true;
            Task.Run(() => InnerThread());
        }

        public void Stop()
        {
            _keepRunning = false;
        }

        private void InnerThread()
        {
            while (_keepRunning)
            {
                Iterate();
                Thread.Sleep(100);
            }
        }

        private void Iterate()
        {
            List<string> urlsClone;
            lock (_locker)
            {
                urlsClone = _urls.Select(item => item).ToList();
            }

            foreach (var url in urlsClone)
            {
                if(!_keepRunning) { break; }
                RefreshItem(url);
            }
        }

        private void RefreshItem(string url)
        {
            _cache.RefreshIfCloseToExpiring(url);
        }

        public void Dispose()
        {
            _keepRunning = false;
        }
    }
}
