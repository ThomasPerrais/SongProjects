using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Crawler
{
    public class AvailableProxy
    {
        public WebProxy Proxy;
        public bool Available;
    }

    public class ProxyManager
    {
        readonly List<AvailableProxy> _proxies;
        readonly Random rand;

        public ProxyManager(IEnumerable<(string ip, int port)> proxies)
        {
            _proxies = proxies.Select(p => new AvailableProxy()
            {
                Proxy = new WebProxy(p.ip, p.port),
                Available = true
            }).ToList();
            rand = new Random(); 
        }

        public AvailableProxy GetAvailableProxy()
        {
            int p = rand.Next(_proxies.Count);
            for (int i = 0; i < _proxies.Count; i++)
            {
                if (_proxies[(p + i) % _proxies.Count].Available)
                {
                    return _proxies[(p + i) % _proxies.Count];
                }
            }
            throw new NoAvailableProxyException("no available proxies at the moment");
        }

        public async void DisableProxy(AvailableProxy proxy, int duration = -1)
        {
            if (duration == -1)
            {
                // disable proxy forever
                proxy.Available = false;
            }
            if (proxy.Available)
            {
                proxy.Available = false;
                void Sleep() => Thread.Sleep(duration);
                await Task.Run(Sleep);
                proxy.Available = true;
            }
        }

        public void EnableAllProxies()
        {
            foreach (var proxy in _proxies)
                proxy.Available = true;
        }
    }

    public class NoAvailableProxyException: Exception
    {
        public NoAvailableProxyException(string msg):
            base(msg)
        { }
    }
}
