using Xenophyte_SeedNode_Proxy.TCP.Enum;
using Xenophyte_SeedNode_Proxy.TCP.Server;

namespace Xenophyte_SeedNode_Proxy.Automation
{
    public class AutomationSystem
    {
        /// <summary>
        /// List of server listener objects.
        /// </summary>
        private ProxyServerListener _proxyServerOnlineNetwork;
        private ProxyServerListener _proxyServerRemoteNetwork;
        private ProxyServerListener _proxyServerTokenNetwork;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="proxyServerOnlineNetwork"></param>
        /// <param name="proxyServerRemoteNetwork"></param>
        /// <param name="proxyServerTokenNetwork"></param>
        public AutomationSystem(ProxyServerListener proxyServerOnlineNetwork, 
                                ProxyServerListener proxyServerRemoteNetwork,
                                ProxyServerListener proxyServerTokenNetwork)
        {
            _proxyServerOnlineNetwork = proxyServerOnlineNetwork;
            _proxyServerRemoteNetwork = proxyServerRemoteNetwork;
            _proxyServerTokenNetwork = proxyServerTokenNetwork;
        }

        /// <summary>
        /// Stop the proxy server objects.
        /// </summary>
        /// <returns></returns>
        public bool StopProxyServer()
        {
            if (!_proxyServerOnlineNetwork.StopServer())
                return false;

            if (!_proxyServerRemoteNetwork.StopServer())
                return false;

            if (!_proxyServerTokenNetwork.StopServer())
                return false;

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
            return _proxyServerOnlineNetwork.GetProxyClientCount(enumStats);
        }

        /// <summary>
        /// Return the amount of connections opened from the Remote Network depending of the enumeration.
        /// </summary>
        /// <param name="enumStats"></param>
        /// <returns></returns>
        public long GetTotalRemoteNetworkConnection(ProxyServerEnumStats enumStats)
        {
            return _proxyServerRemoteNetwork.GetProxyClientCount(enumStats);
        }

        /// <summary>
        /// Return the amount of connections opened from the Token Network depending of the enumeration.
        /// </summary>
        /// <param name="enumStats"></param>
        /// <returns></returns>
        public long GetTotalTokenNetworkConnection(ProxyServerEnumStats enumStats)
        {
            return _proxyServerTokenNetwork.GetProxyClientCount(enumStats);
        }

        #endregion
    }
}
