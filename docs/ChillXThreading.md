# ChillXThreading
This library is a Unit Of Work processor with an auto scaling thread pool designed to support discrete unit of work processor such as API end points. Additionally it implements basic per client concurrent request limits.
Example Use Case:
Implementing API end points in IIS as Web API, MVC etc... is relatively straight forward.
However managing concurrency and backend load etc... are not always as straight forward. While adding a Message Queue is one way of solving this it is also a pretty heavy weight approach to solving this issue.

### Caters for the following requirements:

 - Offloading requests from the web server thread with an Async Await of the processed response
 - Throttling the number of concurrent backend API calls for processing requests
 - Throttling the number of concurrent API calls per client
 - Completes processing of any pending request work items in case of Process Shutdown
 - In the case of Application Pool Recycle completes processing of as many pending request work items until terminated by IIS (Available time window depends on timeout settings in IIS)
 - Gracefull shutdown when Application Pool recycles or when process exists.

### Key features
 - Built for pure performance.
 - Extremely lightweight

### Example Usage

    //Example Usage for WebAPI controller
   public enum WorkItemPriority
    {
        Low = 0,
        Medium = 1,
        High = 2,
    }
    
    private static ThreadedWorkItemProcessor<DummyRequest, DummyResponse, int, WorkItemPriority> ThreadedProcessorExample = new ThreadedWorkItemProcessor<DummyRequest, DummyResponse, int, WorkItemPriority>(
            _maxWorkItemLimitPerClient: 100 // Maximum number of concurrent requests in the processing queue per client. Set to int.MaxValue to disable concurrent request caps
            , _maxWorkerThreads: 16 // Maximum number of threads to scale upto
            , _threadStartupPerWorkItems: 4 // Consider starting a new processing thread ever X requests
            , _threadStartupMinQueueSize: 4 // Do NOT start a new processing thread if work item queue is below this size
            , _idleWorkerThreadExitSeconds: 10 // Idle threads will exit after X seconds
            , _abandonedResponseExpirySeconds: 60 // Expire processed work items after X seconds (Maybe the client terminated or the web request thread died)
            , _processRequestMethod: ProcessRequestMethod // Your Do Work method for processing the request
            , _logErrorMethod: Handler_LogError
            , _logMessageMethod: Handler_LogMessage
            );
    //[FromBody]
    public async Task<DummyResponse> GetResponse([FromBody] DummyRequest _request)
    {
        int clientID = 1; //Replace with the client ID from your authentication mechanism if using per client request caps. Otherwise just hardcode to maybe 0 or whatever
        WorkItemPriority _priority;
        _priority = WorkItemPriority.Medium; //Assign the priority based on whatever prioritization rules.
        int RequestID = ThreadedProcessorExample.ScheduleWorkItem(_priority, _request, clientID);
        if (RequestID < 0)
        {
            //Client has exceeded maximum number of concurrent requests or Application Pool is shutting down
            //return a suitable error message here
            return new DummyResponse() { ErrorMessage = @"Maximum number of concurrent requests exceeded or service is restarting. Please retry request later." };
        }

        //If you need the result (Like in a webapi controller) then do this
        //Otherwise if it is say a backend processing sink where there is no client waiting for a response then we are done here. just return.

        KeyValuePair<bool, ThreadWorkItem<DummyRequest, DummyResponse, int>> workItemResult;

        workItemResult = await ThreadedProcessorExample.TryGetProcessedWorkItemAsync(RequestID,
            _timeoutMS: 1000, //Timeout of 1 second
            _taskWaitType: ThreadProcessorAsyncTaskWaitType.Delay_Specific,
            _delayMS: 10);
        if (!workItemResult.Key)
        {
            //Processing timeout or Application Pool is shutting down
            //return a suitable error message here
            return new DummyResponse() { ErrorMessage = @"Internal system timeout or service is restarting. Please retry request later." };
        }
        return workItemResult.Value.Response;
    }

    public static DummyResponse ProcessRequestMethod(DummyRequest request)
    {
        // Process the request and return the response
        return new DummyResponse() { orderID = request.orderID };
    }
    public static void Handler_LogError(Exception ex)
    {
        //Log unhandled exception here
    }

    public static void Handler_LogMessage(string Message)
    {
        //Log message here
    }


### Performance stats

#### CPU Bound Test: Tightly coupled rapid processing workloads test

    Baseline Fixed Overhead: 1000 Calls : 00:00:00.0003302
    Baseline Processing Overhead At 1ms Per Unit Of Work: 1000 Calls : 00:00:15.7817660
    (0ms Per Unit Of Work) - 1000 Calls From 1 Sync Client: 00:00:00.0168385
    (0ms Per Unit Of Work) - 1000 Calls From 1 Async Client: 00:00:00.0767332
    (0ms Per Unit Of Work) - 10,000 Calls Across 10 Sync Clients: 00:00:00.1360409
    (0ms Per Unit Of Work) - 10,000 Calls Across 10 Async Clients: 00:00:01.3950883
    (0ms Per Unit Of Work) - 32,000 Calls Across 32 Sync Clients: 00:00:00.3184329
    (0ms Per Unit Of Work) - 32,000 Calls Across 32 Async Clients: 00:00:07.3446691
    (0ms Per Unit Of Work) - 128,000 Calls Across 128 Sync Clients: 00:00:01.8473372
    (0ms Per Unit Of Work) - 128,000 Calls Across 128 Async Clients: 00:00:38.1362445

#### CPU Bound Test: Using Spinwait of 1 ms for simulating unit of work processing

    Note: Using task.Delay(1) in the Async client (Task is forced to use Async with a 1 ms wait)
    Baseline Fixed Overhead: 1000 Calls : 00:00:00.0003945
    Baseline Processing Overhead At 1ms Per Unit Of Work: 1000 Calls : 00:00:15.7578766
    (1ms Per Unit Of Work) - 1000 Calls From 1 Sync Client: 00:00:15.7367911
    (1ms Per Unit Of Work) - 1000 Calls From 1 Async Client: 00:00:16.3562684
    (1ms Per Unit Of Work) - 10,000 Calls Across 10 Sync Clients: 00:00:17.1059828
    (1ms Per Unit Of Work) - 10,000 Calls Across 10 Async Clients: 00:00:19.0927514
    (1ms Per Unit Of Work) - 32,000 Calls Across 32 Sync Clients: 00:00:30.6154786
    (1ms Per Unit Of Work) - 32,000 Calls Across 32 Async Clients: 00:00:21.4271592
    (1ms Per Unit Of Work) - 128,000 Calls Across 128 Sync Clients: 00:02:03.2134529
    (1ms Per Unit Of Work) - 128,000 Calls Across 128 Async Clients: 00:01:06.1646710

#### IO Wait Bound Test: Using Thread.Sleep of 1 ms for simulating unit of work processing

    Baseline Fixed Overhead: 1000 Calls : 00:00:00.0028940
    Baseline Processing Overhead At 1ms Per Unit Of Work: 1000 Calls : 00:00:15.7583522
    (1ms Per Unit Of Work) - 1000 Calls From 1 Sync Client: 00:00:15.7395597
    (1ms Per Unit Of Work) - 1000 Calls From 1 Async Client: 00:00:16.0310528
    (1ms Per Unit Of Work) - 10,000 Calls Across 10 Sync Clients: 00:00:25.3664534
    (1ms Per Unit Of Work) - 10,000 Calls Across 10 Async Clients: 00:00:18.5945677
    (1ms Per Unit Of Work) - 32,000 Calls Across 32 Sync Clients: 00:00:30.6395311
    (1ms Per Unit Of Work) - 32,000 Calls Across 32 Async Clients: 00:00:26.2611328
    (1ms Per Unit Of Work) - 128,000 Calls Across 128 Sync Clients: 00:02:03.2886179
    (1ms Per Unit Of Work) - 128,000 Calls Across 128 Async Clients: 00:01:24.0135682

