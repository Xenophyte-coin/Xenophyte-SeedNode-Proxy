using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Xenophyte_SeedNode_Proxy.Log.Enum;
using Xenophyte_SeedNode_Proxy.Log.Object;
using Xenophyte_SeedNode_Proxy.Setting.Object;

namespace Xenophyte_SeedNode_Proxy.Log.Function
{
    public class LogSystem
    {
        private ProxySetting _proxySetting;
        private ConcurrentDictionary<LogEnum, List<LogData>> _listLog;
        private CancellationTokenSource _cancellationWriteLog;
        private SemaphoreSlim _semaphoreLog;
        private bool _logSystemStatus;

        
        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="proxySetting"></param>
        /// <param name="cancellationProxySystem"></param>
        public LogSystem(ProxySetting proxySetting, CancellationTokenSource cancellationProxySystem)
        {
            _proxySetting = proxySetting;
            _listLog = new ConcurrentDictionary<LogEnum, List<LogData>>();
            _semaphoreLog = new SemaphoreSlim(1, 1);
            _cancellationWriteLog = CancellationTokenSource.CreateLinkedTokenSource(cancellationProxySystem.Token);          
            InitLogSystem();
        }


        /// <summary>
        /// Initialize the log system.
        /// </summary>
        private void InitLogSystem()
        {
            if (!Directory.Exists(_proxySetting.ServerLogPath))
                Directory.CreateDirectory(_proxySetting.ServerLogPath);

            _listLog.TryAdd(LogEnum.GENERAL, new List<LogData>());
            _listLog.TryAdd(LogEnum.SERVER, new List<LogData>());
            _listLog.TryAdd(LogEnum.CLIENT_ONLINE, new List<LogData>());
            _listLog.TryAdd(LogEnum.CLIENT_REMOTE, new List<LogData>());
            _listLog.TryAdd(LogEnum.CLIENT_TOKEN, new List<LogData>());
            _listLog.TryAdd(LogEnum.SEED_ONLINE, new List<LogData>());
            _listLog.TryAdd(LogEnum.SEED_REMOTE, new List<LogData>());
            _listLog.TryAdd(LogEnum.SEED_TOKEN, new List<LogData>());
            _logSystemStatus = true;
            EnableWriteLog();
            WriteLine("LogSystem initialized successfully.", LogEnum.GENERAL, ConsoleColor.Green);
        }


        /// <summary>
        /// Write log and store it.
        /// </summary>
        /// <param name="content"></param>

        /// <param name="type"></param>
        /// <param name="color"></param>
        public void WriteLine(string content, LogEnum type, ConsoleColor color, bool show = true)
        {
            if (_listLog.ContainsKey(type))
            {
                string date = DateTime.Now.ToString();

                if (show)
                {
                    Console.ForegroundColor = color;
                    Console.WriteLine(date + " - " + content);
                    Console.ForegroundColor = ConsoleColor.White;
                }

                try
                {
                    Task.Factory.StartNew(async () =>
                    {
                        bool useSemaphore = false;
                        try
                        {
                            try
                            {
                                await _semaphoreLog.WaitAsync(_cancellationWriteLog.Token);
                                useSemaphore = true;

                                if (_listLog.ContainsKey(type))
                                {
                                    _listLog[type].Add(new LogData()
                                    {
                                        Content = content,
                                        DateTime = date
                                    });
                                }
                            }
                            catch
                            {
                                // Ignored.
                            }
                        }
                        finally
                        {
                            if (useSemaphore)
                                _semaphoreLog.Release();
                        }
                    }, _cancellationWriteLog.Token, TaskCreationOptions.LongRunning, TaskScheduler.Current).ConfigureAwait(false);
                }
                catch
                {
                    // Ignored, catch the exception once the task is cancelled.
                }
            }
        }

        /// <summary>
        /// Enable the write log system.
        /// </summary>
        public void EnableWriteLog()
        {
            foreach (LogEnum type in _listLog.Keys)
            {
                try
                {
                    Task.Factory.StartNew(async () =>
                    {

                        string logFilename = (System.Enum.GetName(typeof(LogEnum), type)).ToLower();

                        WriteLine(logFilename + " created successfully.", LogEnum.GENERAL, ConsoleColor.Green, true);

                        using (StreamWriter writerLog = new StreamWriter(_proxySetting.ServerLogPath + "//" + logFilename + ".log", true))
                        {
                            while (_logSystemStatus && !_cancellationWriteLog.IsCancellationRequested)
                            {
                                bool useSemaphore = false;
                                try
                                {
                                    try
                                    {
                                        await _semaphoreLog.WaitAsync(_cancellationWriteLog.Token);
                                        useSemaphore = true;

                                        if (useSemaphore)
                                        {
                                            if (_listLog[type].Count >= _proxySetting.ServerLogIntervalCount)
                                            {
                                                foreach (LogData logData in _listLog[type])
                                                    writerLog.WriteLine(logData.DateTime + " - " + logData.Content);

                                                writerLog.Flush();

                                                _listLog[type].Clear();
                                                _listLog[type].TrimExcess();
                                            }
                                        }
                                    }
                                    catch
                                    {
                                        // Ignored.
                                    }
                                }
                                finally
                                {
                                    if (useSemaphore)
                                        _semaphoreLog.Release();
                                }

                                await Task.Delay(1000);
                            }
                        }
                    }, _cancellationWriteLog.Token, TaskCreationOptions.LongRunning, TaskScheduler.Current).ConfigureAwait(false);
                }
                catch
                {
                    // Ignored, catch the exceptio once the task is cancelled.
                }
            }
        }
    }
}
