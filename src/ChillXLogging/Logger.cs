using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading;

namespace ChillXLogging
{
    public enum LogSeverity
    {
        debug = 0,
        info = 1,
        warning = 2,
        error = 3,
        unhandled = 4,
        fatal = 5,
    }

    public class Logger
    {
        private static object SyncRoot { get; } = new object();
        private volatile static Logger m_Instance;
        public static Logger Instance
        {
            get
            {
                if (m_Instance == null)
                {
                    lock (SyncRoot)
                    {
                        if (m_Instance == null)
                        {
                            m_Instance = new Logger();
                        }
                    }
                }
                return m_Instance;
            }
        }
        public LogEntry LogMessage(LogSeverity _severity, string _message, Exception _ex = null, DateTime? _eventTime = null)
        {
            return Instance.Log(_severity, _message, _ex, _eventTime);
        }
        public static void RegisterHandler(string _name, ILogHandler _handler)
        {
            Instance.RegisterHandlerInternal(_name, _handler);
        }
        public static int BatchSize { get { return Instance.BulkLoggingBatchSize; } set { Instance.BulkLoggingBatchSize = value; } }
        public static void ShutDown()
        {
            Instance.ShutDownInternal();
        }

        public Logger()
        {
            StartUp();
        }

        private bool m_IsRunning = false;
        private bool IsRunning
        {
            get
            {
                lock(SyncRoot)
                {
                    return m_IsRunning;
                }
            }
        }

        private int m_BulkLoggingBatchSize = 100;
        private int BulkLoggingBatchSize
        {
            get { lock (SyncRoot) { return m_BulkLoggingBatchSize; } }
            set { lock(SyncRoot) { if (value < 1) { value = 1; } m_BulkLoggingBatchSize = value; } }
        }

        private bool m_IsShutDown = false;
        private void ShutDownInternal()
        {
            lock(SyncRoot)
            {
                if (m_IsShutDown) { return; }
                m_IsShutDown = true;
                m_IsRunning = false;
            }
            AppDomain.CurrentDomain.UnhandledException -= CurrentDomain_UnhandledException;
            AppDomain.CurrentDomain.ProcessExit -= CurrentDomain_ProcessExit;
            Stopwatch SW = new Stopwatch();
            SW.Start();
            while (LogWriterThread.IsAlive)
            {
                System.Threading.Thread.Sleep(1);
                if (SW.ElapsedMilliseconds > 5000)
                {
                    try
                    {
                        LogWriterThread.Abort();
                    }
                    catch
                    {

                    }
                    break;
                }
            }
            LogWriterThread = null;
            foreach (KeyValuePair<string, ILogHandler> handler in LogHandlerDict)
            {
                try
                {
                    handler.Value.Dispose();
                }
                catch 
                {
                }
            }
            LogHandlerDict.Clear();
        }

        private Dictionary<string, ILogHandler> LogHandlerDict { get; } = new Dictionary<string, ILogHandler>();

        private void RegisterHandlerInternal(string _name, ILogHandler _handler)
        {
            _name = _name.ToLowerInvariant();
            lock (SyncRoot)
            {
                if (LogHandlerDict.ContainsKey(_name))
                {
                    LogHandlerDict[_name] = _handler;
                }
                else
                {
                    LogHandlerDict.Add(_name, _handler);
                }
            }
        }

        private object LogInboundSyncLock { get; } = new object();

        private Queue<LogEntry> LogEntryInbound { get; } = new Queue<LogEntry>();

        public LogEntry Log(LogSeverity _severity, string _message, Exception _ex = null, DateTime? _eventTime = null )
        {
            DateTime eventTime = _eventTime.HasValue ? _eventTime.Value : DateTime.Now;
            LogEntry entry = new LogEntry() { Severity = _severity, MessageText = _message, MessageException = _ex, EventTime = eventTime };
            lock(LogInboundSyncLock)
            {
                LogEntryInbound.Enqueue(entry);
            }
            return entry;
        }

        public int PendingLogItemCount()
        {
            lock (LogInboundSyncLock)
            {
                return LogEntryInbound.Count;
            }
        }

        private void StartUp()
        {
            lock(SyncRoot)
            {
                if (m_IsRunning) { return; }
                m_IsRunning = true;
            }
            LogWriterThread = new Thread(new ThreadStart(DoCommit));
            LogWriterThread.Start();
            AppDomain.CurrentDomain.ProcessExit += CurrentDomain_ProcessExit;
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
        }

        private void CurrentDomain_ProcessExit(object sender, EventArgs e)
        {
            ShutDownInternal();
        }

        private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            Exception ex = e.ExceptionObject as Exception;
            if (e.IsTerminating)
            {
                Log(LogSeverity.unhandled, @"Unhandled Exception", ex);
            }
            else
            {
                Log(LogSeverity.fatal, @"Fatal Unhandled Exception. Process is terminating", ex);
            }
        }

        private Thread LogWriterThread;

        private void DoCommit()
        {
            bool hasLogEntries;
            int batchSize;
            int numPending;
            bool running;
            string currentLogHanderName = string.Empty;
            LogEntry[] entries = null;
            List<KeyValuePair<string, ILogHandler>> logHandlers = new List<KeyValuePair<string, ILogHandler>>();
            running = true;
            while (running)
            {
                try
                {
                    hasLogEntries = false;
                    logHandlers.Clear();
                    currentLogHanderName = string.Empty;
                    lock (SyncRoot)
                    {
                        if (!m_IsRunning)
                        {
                            running = false;
                            batchSize = 1;
                        }
                        else
                        {
                            batchSize = m_BulkLoggingBatchSize;
                        }
                        logHandlers.AddRange(LogHandlerDict);
                    }
                    lock (LogInboundSyncLock)
                    {
                        numPending = LogEntryInbound.Count;
                        if (numPending >= batchSize)
                        {
                            hasLogEntries = true;
                            if (running)
                            {
                                numPending = batchSize;
                            }
                            entries = new LogEntry[numPending];
                            for (int I = 0; I < numPending; I++)
                            {
                                entries[I] = LogEntryInbound.Dequeue();
                            }
                        }
                    }
                    if (hasLogEntries)
                    {
                        foreach (KeyValuePair<string, ILogHandler> handler in logHandlers)
                        {
                            if (string.IsNullOrEmpty(handler.Key))
                            {
                                currentLogHanderName = @"Untitled";
                            }
                            else
                            {
                                currentLogHanderName = handler.Key;
                            }
                            handler.Value.WriteLogEntries(entries);
                        }
                    }
                    else
                    {
                        for (int I = 0; I < 10; I++)
                        {
                            System.Threading.Thread.Sleep(100);
                            if (!IsRunning) { break; }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Exception ex2;
                    if (string.IsNullOrEmpty(currentLogHanderName))
                    {
                        ex2 = new Exception(@"Unhandled expcetion in Logger.DoCommit(). This should not happen.");
                    }
                    else
                    {
                        ex2 = new Exception(string.Concat(@"Unhandled expcetion in application space Log handler method: ", currentLogHanderName, @" This is very bad!!! Log handlers methods should never throw an exception since part of why they exist to log exceptions"));
                    }
                    // This should not happen. Additionally there is nothing we can do to log the exception either
                    // Just in case this is a console app and STDIO is captured lets try write to the console.
                    try
                    {
                        Console.WriteLine(ex2.Message);
                        Console.WriteLine(ex2.ToString());
                    }
                    catch
                    {

                    }
                    // Maybe its running in the debugger ???. Just in case lets try write a message there
                    try
                    {
                        System.Diagnostics.Debug.WriteLine(ex2.Message);
                        System.Diagnostics.Debug.WriteLine(ex2.ToString());
                    }
                    catch
                    {

                    }
                }
                finally
                {

                }
            }
        }
    }
}
