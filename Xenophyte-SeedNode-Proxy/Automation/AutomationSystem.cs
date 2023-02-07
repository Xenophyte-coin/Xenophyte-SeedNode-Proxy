using System.Collections.Generic;
using Xenophyte_SeedNode_Proxy.TCP.Enum;
using Xenophyte_SeedNode_Proxy.TCP.Server;

namespace Xenophyte_SeedNode_Proxy.Automation
{
    public class AutomationSystem
    {
        /// <summary>
        /// List of server listener objects.
        /// </summary>
        private List<ProxyServerListener> _proxyServerNetwork;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="proxyServerNetwork"></param>
        public AutomationSystem(List<ProxyServerListener> proxyServerNetwork)
        {
            _proxyServerNetwork = proxyServerNetwork;
        }

        /// <summary>
        /// Auto clean proxy server.
        /// </summary>
        public void AutoCleanProxyServer()
        {
            for (int i = 0; i < _proxyServerNetwork.Count; i++)
            {
                if (i < _proxyServerNetwork.Count)
                    _proxyServerNetwork[i].ClearProxyClientListing(false);
            }
        }

        /// <summary>
        /// Stop the proxy server objects.
        /// </summary>
        /// <returns></returns>
        public bool StopProxyServer()
        {
            for(int i = 0; i < _proxyServerNetwork.Count; i++)
            {
                if (i < _proxyServerNetwork.Count)
                    _proxyServerNetwork[i].StopServer();
            }

            return true;
        }

        #region Network stats command line(s).

        /// <summary>
        /// Return the amount of connections opened from the Online Network depending of the enumeration.
        /// </summary>
        /// <param name="enumStats"></param>
        /// <returns></returns>
        public long GetTotalOnlineNetworkConnection(ProxyServerEnumStats enumStats)
        {
            return _proxyServerNetwork[0].GetProxyClientCount(enumStats);
        }

        /// <summary>
        /// Return the amount of connections opened from the Remote HTTP Network depending of the enumeration.
        /// </summary> 
        /// <param name="enumStats"></param>
        /// <returns></returns>
        public long GetTotalRemoteHttpNetworkConnection(ProxyServerEnumStats enumStats)
        {
            return _proxyServerNetwork[1].GetProxyClientCount(enumStats);
        }

        /// <summary>
        /// Return the amount of connections opened from the Remote Network depending of the enumeration.
        /// </summary>
        /// <param name="enumStats"></param>
        /// <returns></returns>
        public long GetTotalRemoteNetworkConnection(ProxyServerEnumStats enumStats)
        {
            return _proxyServerNetwork[2].GetProxyClientCount(enumStats);
        }

        /// <summary>
        /// Return the amount of connections opened from the Token Network depending of the enumeration.
        /// </summary>
        /// <param name="enumStats"></param>
        /// <returns></returns>
        public long GetTotalTokenNetworkConnection(ProxyServerEnumStats enumStats)
        {
            return _proxyServerNetwork[3].GetProxyClientCount(enumStats);
        }

        #endregion
    }
}
