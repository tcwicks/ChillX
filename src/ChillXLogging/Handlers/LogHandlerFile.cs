using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace ChillXLogging.Handlers
{
    public class LogHandlerFile : ILogHandler, IDisposable
    {
        public LogHandlerFile(string _path, string _fileNamePrepend = @"Log_", string _fileExtension = @".log",
            int _fileRollOverPerEntries = 100000,
            int _fileRollOverDays = 1, int _fileRollOverHours = 0, int _fileRollOverMinutes = 0)
        {
            //This will throw an exception if there are any filesystem permission issues or if path is invalid
            Path = _path;
            FileNamePrepend = _fileNamePrepend;
            FileExtension = _fileExtension;
            FileRolloverPerEntries = _fileRollOverPerEntries;
            FileRolloverPerTime = TimeSpan.FromDays(_fileRollOverDays).Add(TimeSpan.FromHours(_fileRollOverHours)).Add(TimeSpan.FromMinutes(_fileRollOverMinutes));
        }

        private object SyncRoot { get; } = new object();

        private string m_Path = string.Empty;
        public string Path
        {
            get
            {
                lock(SyncRoot)
                {
                    return m_Path;
                }
            }
            set
            {
                if (!System.IO.Directory.Exists(value))
                {
                    value = System.IO.Directory.CreateDirectory(value).FullName;
                }
                else
                {
                    value = new System.IO.DirectoryInfo(value).FullName;
                }
                if (value.Contains(@"\")) { value = string.Concat(value, @"\"); }
                else if (value.Contains(@"/")) { value = string.Concat(value, @"/"); }

                // Check permissions. if we don't have access then this will throw an exception
                string testFile;
                testFile = string.Concat(value, @"_logger_temp.test");
                if (System.IO.File.Exists(testFile)) { System.IO.File.Delete(testFile); }
                System.IO.File.WriteAllText(testFile, @".");
                System.IO.File.Delete(testFile);
                lock (SyncRoot)
                {
                    m_Path = value;
                }
            }
        }

        private string m_FileNamePrepend = string.Empty;
        public string FileNamePrepend
        {
            get
            {
                lock(SyncRoot)
                {
                    return (m_FileNamePrepend);
                }
            }
            set
            {
                lock(SyncRoot)
                {
                    m_FileNamePrepend = value;
                }
            }
        }

        private string m_FileExtension = @".log";
        public string FileExtension
        {
            get
            {
                lock(SyncRoot)
                {
                    return m_FileExtension;
                }
            }
            set
            {
                if (!value.StartsWith(@"."))
                {
                    value = string.Concat(@".", value);
                }
                lock(SyncRoot)
                {
                    m_FileExtension = value;
                }
            }
        }

        private int m_FileRolloverPerEntries = int.MaxValue;
        public int FileRolloverPerEntries
        {
            get
            {
                lock(SyncRoot)
                {
                    return m_FileRolloverPerEntries;
                }
            }
            set
            {
                if (value < 1) { value = 1; } //WTH!!!
                lock (SyncRoot)
                {
                    m_FileRolloverPerEntries = value;
                }
            }
        }
        private TimeSpan m_FileRolloverPerTime = TimeSpan.FromDays(1);
        public TimeSpan FileRolloverPerTime
        {
            get
            {
                lock(SyncRoot)
                {
                    return m_FileRolloverPerTime;
                }
            }
            set
            {
                if (value.TotalMinutes < 1) { value = TimeSpan.FromMinutes(1); } //WTH!!!
                lock (SyncRoot)
                {
                    m_FileRolloverPerTime = value;
                }
            }
        }

        private string BuildFileName()
        {
            return string.Concat(Path, FileNamePrepend, DateTime.Now.ToString(@"yyyyMMdd_HHmmss"), FileExtension);
        }

        private System.IO.StreamWriter m_LogWriter;
        private Stopwatch LogWriterAge { get; } = new Stopwatch();
        private int m_LogWriterNumEntries = 0;

        private void CreateNewLogWriter()
        {
            if (m_IsDisposed) { return; }
            if (m_LogWriter != null)
            {
                m_LogWriter.Flush();
                m_LogWriter.Dispose();
                m_LogWriter = null;
            }
            string logFileName = BuildFileName();
            if (System.IO.File.Exists(logFileName))
            {
                m_LogWriter = System.IO.File.AppendText(logFileName);
            }
            else
            {
                m_LogWriter = System.IO.File.CreateText(logFileName);
            }
            LogWriterAge.Restart();
            m_LogWriterNumEntries = 0;
        }

        public void WriteLogEntries(IEnumerable<LogEntry> _entries)
        {
            System.IO.StreamWriter writer;
            lock (SyncRoot)
            {
                if (m_LogWriter == null)
                {
                    CreateNewLogWriter();
                }
                else if (LogWriterAge.Elapsed > m_FileRolloverPerTime)
                {
                    CreateNewLogWriter();
                }
                else if (m_LogWriterNumEntries > m_FileRolloverPerEntries)
                {
                    CreateNewLogWriter();
                }
                writer = m_LogWriter;
            }
            int counter = 0;
            foreach (LogEntry entry in _entries)
            {
                counter++;
                writer.WriteLine(entry.ToFormattedText());
            }
            lock (SyncRoot)
            {
                m_LogWriterNumEntries += counter;
            }
            writer.Flush();
        }

        private bool m_IsDisposed = false;
        public void Dispose()
        {
            if (m_IsDisposed) { return; }
            DoDispose(true);
        }

        private void DoDispose(bool IsDisposing = false)
        {
            if (IsDisposing)
            {
                m_IsDisposed = true;
                if (m_LogWriter != null)
                {
                    m_LogWriter.Flush();
                    m_LogWriter.Dispose();
                    m_LogWriter = null;
                }
                GC.SuppressFinalize(this);
            }
        }
    }
}
