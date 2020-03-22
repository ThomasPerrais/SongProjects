using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;

namespace Crawler
{
    public class HtmlExtractor
    {
        ProxyManager _proxyManager;
        public HtmlExtractor(ProxyManager proxyManager = null)
        {
            _proxyManager = proxyManager; 
        }

        public string Extract(string address, int maxRetry, int timeout, int verbose)
        {
            int defaultWaitTime = 180000;
            var waitIncr = 10000;
            int retry = 0;
            while (retry++ < maxRetry)
            {
                AvailableProxy proxy = null;
                try
                {
                    HttpWebRequest request = (HttpWebRequest)WebRequest.Create(address);
                    request.Timeout = timeout;
                    if (_proxyManager != null)
                    {
                        proxy = _proxyManager.GetAvailableProxy();
                        request.Proxy = proxy.Proxy;
                    }
                    HttpWebResponse response = (HttpWebResponse)request.GetResponse();

                    string data = null;
                    if (response.StatusCode == HttpStatusCode.OK)
                    {
                        var receiveStream = response.GetResponseStream();
                        StreamReader readStream;

                        if (string.IsNullOrWhiteSpace(response.CharacterSet))
                            readStream = new StreamReader(receiveStream);
                        else
                            readStream = new StreamReader(receiveStream, Encoding.GetEncoding(response.CharacterSet));

                        data = readStream.ReadToEnd();

                        response.Close();
                        readStream.Close();
                    }
                    if (verbose > 1)
                    {
                        if (proxy == null)
                            Console.WriteLine($"[SUCCESS] extracting content from {address} using proxy {proxy.Proxy.Address.AbsoluteUri}");
                        else
                            Console.Write($"[SUCCESS] extracting content from {address} without proxy");
                    }
                    return data;
                }
                catch (NoAvailableProxyException)
                {
                    if (verbose > 0)
                        Console.WriteLine($"[ERROR] No more available proxies ... waiting {defaultWaitTime / 1000}s\n");
                    // no more proxies, wait
                    Thread.Sleep(defaultWaitTime);
                    defaultWaitTime += waitIncr;
                    if (_proxyManager != null)
                    {
                        _proxyManager.EnableAllProxies();
                    }
                }
                catch (Exception e)
                {
                    // other exception: disable current proxy and retry immediatly
                    if (_proxyManager != null)
                    {
                        if (verbose > 1)
                            Console.WriteLine($"[ERROR] extracting content from {address} using proxy {proxy.Proxy.Address.AbsoluteUri}: disabling proxy\n");
                        _proxyManager.DisableProxy(proxy);
                    }
                    else
                    {
                        if (verbose > 0)
                            Console.WriteLine($"[ERROR] ... waiting {defaultWaitTime / 1000}s\n");
                        // no proxyManager, we wait
                        Thread.Sleep(defaultWaitTime);
                        defaultWaitTime += waitIncr;
                    }
                }
            }
            if (verbose > 1)
                Console.WriteLine($"reached max-retry limit ({maxRetry}), skipping this page");
            return null;
        }

        public string Extract2(string address)
        {
            int defaultWaitTime = 180000;
            var waitIncr = 10000;
            
            while (true)
            {
                AvailableProxy proxy = null;
                try
                {
                    HttpWebRequest request = (HttpWebRequest)WebRequest.Create(address);
                    if (_proxyManager != null)
                    {
                        proxy = _proxyManager.GetAvailableProxy();
                        request.Proxy = proxy.Proxy;
                    }
                    HttpWebResponse response = (HttpWebResponse)request.GetResponse();

                    string data = null;
                    if (response.StatusCode == HttpStatusCode.OK)
                    {
                        var receiveStream = response.GetResponseStream();
                        StreamReader readStream;

                        if (string.IsNullOrWhiteSpace(response.CharacterSet))
                            readStream = new StreamReader(receiveStream);
                        else
                            readStream = new StreamReader(receiveStream, Encoding.GetEncoding(response.CharacterSet));

                        data = readStream.ReadToEnd();

                        response.Close();
                        readStream.Close();
                    }
                    return data;
                }
                catch (NoAvailableProxyException)
                {
                    // no more proxies, wait
                    Thread.Sleep(defaultWaitTime);
                    defaultWaitTime += waitIncr;
                }
                catch (Exception e)
                {
                    // other exception: disable current proxy and retry immediatly
                    if (_proxyManager != null)
                    {
                        _proxyManager.DisableProxy(proxy, defaultWaitTime);
                    }
                    else
                    {
                        // no proxyManager, we wait
                        Thread.Sleep(defaultWaitTime);
                        defaultWaitTime += waitIncr;
                    }
                }
            }
        }
    }
}
