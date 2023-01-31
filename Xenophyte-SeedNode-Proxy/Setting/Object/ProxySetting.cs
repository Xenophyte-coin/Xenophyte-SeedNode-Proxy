using Newtonsoft.Json;
using System.Collections.Generic;
using System.Linq;
using Xenophyte_Connector_All.Setting;

namespace Xenophyte_SeedNode_Proxy.Setting.Object
{
    public class ProxySetting
    {
        public string ServerIp;

        public string ServerLogPath;
        public int ServerLogIntervalCount;
        public bool ServerTargetTokenNetwork;

        public int[] ServerPort = new int[] { ClassConnectorSetting.SeedNodePort, ClassConnectorSetting.RemoteNodePort, ClassConnectorSetting.SeedNodeTokenPort };

        public List<string> SeedNodeHostList = new List<string>();


        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="serverIp"></param>
        /// <param name="serverLogPath"></param>
        public ProxySetting(string serverIp, string serverLogPath)
        {
            ServerIp = serverIp;
            ServerLogPath = serverLogPath;
            ServerLogIntervalCount = 10;

            foreach(string seedNodeIp in ClassConnectorSetting.SeedNodeIp.Keys)
            {
                if (seedNodeIp != serverIp)
                    SeedNodeHostList.Add(seedNodeIp);
            }
        }
    }
}
