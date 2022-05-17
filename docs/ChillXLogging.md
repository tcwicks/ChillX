# ChillXLogging
When engineering for performance the last thing we want to do is to tie up application threads in writing log entries. Additionally in the case of concurrent log writes to the same device be it file system or database this can result in lock contentions adding further latency to the application threads.

This library is a light weight, extremely fast logging framework which offloads the actual logging from the application thread to a separate dedicated logging thread. In addition it automatically captures any unhandled exceptions within the process space. calls to the Log method captures the log entry to a queue and immediately returns. The actual log entry is written by a separate dedicated thread via a configurable number of log writers. The library comes with a file system log writer. Adding log writers is trivial. 

### Key Features

 - Lightweight and therefore extremely fast
 - Disconnects the process of submitting a log entry and committing the log entry to storage
 - Convenience extension methods
 - Commits pending log entries in case of Process Shutdown
 - In the case of Application Pool Recycle commits as many pending log entries until terminated by IIS (Available time window depends on timeout settings in IIS)
 - Gracefull shutdown when Application Pool recycles or when process exists.

### Example Usage

    ChillXLogging.Handlers.LogHandlerFile FileLogHandler_RolloverByCount;
    FileLogHandler_RolloverByCount = new ChillXLogging.Handlers.LogHandlerFile(@"C:\Temp\LogTestByCount",
        _fileNamePrepend: @"ByCount_", _fileExtension: @".txt", _fileRollOverPerEntries: 10000, _fileRollOverDays: 99, _fileRollOverHours: 1, _fileRollOverMinutes: 1);

    ChillXLogging.Handlers.LogHandlerFile FileLogHandler_RolloverByTime;
    FileLogHandler_RolloverByTime = new ChillXLogging.Handlers.LogHandlerFile(@"C:\Temp\LogTestByTime",
        _fileNamePrepend: @"ByCount_", _fileExtension: @".txt", _fileRollOverPerEntries: int.MaxValue, _fileRollOverDays: 0, _fileRollOverHours: 0, _fileRollOverMinutes: 1);

    Logger.BatchSize = 100;
    Logger.RegisterHandler(@"RolloverByCount", FileLogHandler_RolloverByCount);
    Logger.RegisterHandler(@"RolloverByTime", FileLogHandler_RolloverByTime);

    //Example usage
    Logger.LogMessage(LogSeverity.info, @"Some message text");
    Logger.LogMessage(LogSeverity.info, @"Some message text", _ex: new Exception(@""), DateTime.Now);
    @"This is a log message".Log(LogSeverity.info);
    LogSeverity.debug.Log(@"This is another log entry");
    LogSeverity.error.Log(@"Lets log a post dated exception", new Exception(@"Some message here"), DateTime.Now.AddHours(8));
    try
    {
        throw new InvalidOperationException(@"test exception");
    }
    catch (Exception ex)
    {
        ex.Log(@"This is a test exception", LogSeverity.debug);
        //Supports chaining
        throw ex.Log(@"This is a test exception", LogSeverity.debug).MessageException;
        @"Some Messageto be logged".Log(LogSeverity.error, ex);
        string.Format(@"Logging Message {0}",1).Log(LogSeverity.error);
    }
