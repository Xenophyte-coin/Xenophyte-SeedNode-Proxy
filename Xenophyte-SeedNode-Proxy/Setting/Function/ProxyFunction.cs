using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Xenophyte_SeedNode_Proxy.Log.Enum;
using Xenophyte_SeedNode_Proxy.Setting.Object;

namespace Xenophyte_SeedNode_Proxy.Setting.Function
{
    public class ProxyFunction
    {
        /// <summary>
        /// Base settings.
        /// </summary>
        private const string ProxyConfigFilename = "proxy-setting.json";
        private readonly string ProxyConfigPath = (AppContext.BaseDirectory + ProxyConfigFilename).Replace("\\", "//");
        private readonly string ProxyLogPath = (AppContext.BaseDirectory + "Log\\").Replace("\\", "//");

        /// <summary>
        /// Proxy setting.
        /// </summary>
        public ProxySetting ProxySetting { private set; get; }

 

        /// <summary>
        /// Load the proxy setting.
        /// </summary>
        /// <returns></returns>
        public bool LoadProxySetting()
        {
            if (!File.Exists(ProxyConfigPath))
            {
                Console.WriteLine("The setting file: " + ProxyConfigFilename + " does not exist.");
                return false;
            }

            using(StreamReader reader = new StreamReader(ProxyConfigPath))
                ProxySetting = JsonConvert.DeserializeObject<ProxySetting>(reader.ReadToEnd());

            if (ProxySetting == null)
            {
                Console.WriteLine("The setting file is invalid, this one cannot be deserialized.");
                return false;
            }

            if (!IPAddress.TryParse(ProxySetting.ServerIp, out _))
            {
                Console.WriteLine("The setting file is invalid, the Server IP: " + ProxySetting.ServerIp + " is invalid.");
                return false;
            }

            return true;
        }

        /// <summary>
        /// Initialize the proxy setting.
        /// </summary>
        /// <returns></returns>
        public bool InitializeProxySetting()
        {
            try
            {
               
                using (StreamWriter writerProxySetting = new StreamWriter(ProxyConfigPath))
                {
                    Console.WriteLine("Please write the Server IP used to listen incoming connection: ", LogEnum.GENERAL, ConsoleColor.Yellow);

                    string serverIP = Console.ReadLine();

                    while (!IPAddress.TryParse(serverIP, out _))
                    {
                        Console.WriteLine("The input Server IP is invalid, please try again:");
                        serverIP = Console.ReadLine();
                    }

                    ProxySetting = new ProxySetting(serverIP, ProxyLogPath);

                    writerProxySetting.Write(JsonConvert.SerializeObject(ProxySetting, Formatting.Indented));
                    writerProxySetting.Flush();
                }
            }
            catch(Exception error)
            {
                Console.WriteLine("Failed to initialize the proxy setting. Exception: " + error.Message);
                return false;
            }
            return true;
        }

    }
}
