// See https://aka.ms/new-console-template for more information
//Console.WriteLine("Hello, World!");
using ChillXThreading.Complete;
using System.Diagnostics;

namespace ChillXThreadingTest // Note: actual namespace depends on the project name.
{
    //Comments: For tightly coupled workloads which process rapidly Sync performance will be better than Async performance
    //This is most likely because for these workloads the context switch cost of the Async CPU yield is higher than the benefit of the Async CPU yield
    //Note: thread.sleep vs spinwait in simulating Unit of Work processing has completely different metrics. ProcessRequest vs ProcessRequestSpinWait
    //Consider that thread.sleep has greater similarity to an IO Bound task with an IO Wait while spinwait has greater similarity to a CPU bound compute task
    //Platform ThreadRipper 64 Core - 64GB Ram
    //-----------------------------------------------------------------------------------
    // Thread Controller Configuration: 4 Threads
    //-----------------------------------------------------------------------------------
    //    _MaxWorkItemLimitPerClient: 100
    //    _MaxWorkerThreads: 4
    //    _ThreadStartupPerWorkItems: 4
    //    _ThreadStartupMinQueueSize: 4
    //    _IdleWorkerThreadExitSeconds: 3
    //-----------------------------------------------------------------------------------
    //Baseline Fixed Overhead: 1000 Calls : 00:00:00.0030997
    //Baseline Processing Overhead At 1ms Per Unit Of Work: 1000 Calls : 00:00:15.7569780

    //Note: CPU Bound Test: Tightly coupled rapid processing workloads test
    //Via Concurrent Thread Controller(0ms Per Unit Of Work) - 1000 Calls From 1 Sync Client: 00:00:00.0144570
    //Via Concurrent Thread Controller(0ms Per Unit Of Work) - 1000 Calls From 1 Async Client: 00:00:00.0888282
    //Via Concurrent Thread Controller(0ms Per Unit Of Work) - 10,000 Calls Across 10 Sync Clients: 00:00:00.0885127
    //Via Concurrent Thread Controller(0ms Per Unit Of Work) - 10,000 Calls Across 10 Async Clients: 00:00:01.1148027
    //Via Concurrent Thread Controller(0ms Per Unit Of Work) - 32,000 Calls Across 32 Sync Clients: 00:00:00.2827826
    //Via Concurrent Thread Controller(0ms Per Unit Of Work) - 32,000 Calls Across 32 Async Clients: 00:00:09.0186848
    //Via Concurrent Thread Controller(0ms Per Unit Of Work) - 128,000 Calls Across 128 Sync Clients: 00:00:01.8898630
    //Via Concurrent Thread Controller(0ms Per Unit Of Work) - 128,000 Calls Across 128 Async Clients: 00:00:36.4808389
    //Thread Controller Thread Exited : Active: 00:00:02.5541398 - Idle: 00:00:49.2788173
    //Thread Controller Thread Exited : Active: 00:00:04.6159064 - Idle: 00:00:48.7397924
    //Thread Controller Thread Exited : Active: 00:00:04.2637679 - Idle: 00:00:50.3602462

    //Note: IO Wait Bound Test: Using Thread.Sleep for simulating unit of work processing
    //Via Concurrent Thread Controller(1ms Per Unit Of Work) - 1000 Calls From 1 Sync Client: 00:00:15.7075959
    //Via Concurrent Thread Controller(1ms Per Unit Of Work) - 1000 Calls From 1 Async Client: 00:00:15.7084864
    //Via Concurrent Thread Controller(1ms Per Unit Of Work) - 10,000 Calls Across 10 Sync Clients: 00:00:39.1704093
    //Via Concurrent Thread Controller(1ms Per Unit Of Work) - 10,000 Calls Across 10 Async Clients: 00:00:39.2729585
    //Via Concurrent Thread Controller(1ms Per Unit Of Work) - 32,000 Calls Across 32 Sync Clients: 00:02:05.3585335
    //Via Concurrent Thread Controller(1ms Per Unit Of Work) - 32,000 Calls Across 32 Async Clients: 00:00:31.2576092
    //Via Concurrent Thread Controller(1ms Per Unit Of Work) - 128,000 Calls Across 128 Sync Clients: 00:08:24.2550584
    //Via Concurrent Thread Controller(1ms Per Unit Of Work) - 128,000 Calls Across 128 Async Clients: 00:01:34.9617780
    //Thread Controller Thread Exited : Active: 00:13:53.1908736 - Idle: 00:00:19.2586859
    //Thread Controller Thread Exited : Active: 00:13:53.7039273 - Idle: 00:00:18.7398420
    //Thread Controller Thread Exited : Active: 00:13:53.4775544 - Idle: 00:00:18.9628950

    //Note: CPU Bound Test: Using Spinwait for simulating unit of work processing
    //Baseline Fixed Overhead: 1000 Calls : 00:00:00.0035736
    //Baseline Processing Overhead At 1ms Per Unit Of Work: 1000 Calls : 00:00:15.7463578
    //Via Concurrent Thread Controller(1ms Per Unit Of Work) - 1000 Calls From 1 Sync Client: 00:00:15.7414873
    //Via Concurrent Thread Controller(1ms Per Unit Of Work) - 1000 Calls From 1 Async Client: 00:00:15.7542709
    //Via Concurrent Thread Controller(1ms Per Unit Of Work) - 10,000 Calls Across 10 Sync Clients: 00:00:39.3320860
    //Via Concurrent Thread Controller(1ms Per Unit Of Work) - 10,000 Calls Across 10 Async Clients: 00:00:39.4104457
    //Via Concurrent Thread Controller(1ms Per Unit Of Work) - 32,000 Calls Across 32 Sync Clients: 00:02:05.0293517
    //Via Concurrent Thread Controller(1ms Per Unit Of Work) - 32,000 Calls Across 32 Async Clients: 00:00:46.3006233
    //Via Concurrent Thread Controller(1ms Per Unit Of Work) - 128,000 Calls Across 128 Sync Clients: 00:08:23.4227791
    //Via Concurrent Thread Controller(1ms Per Unit Of Work) - 128,000 Calls Across 128 Async Clients: 00:01:40.6661985
    //Thread Controller Thread Exited : Active: 00:14:12.7028225 - Idle: 00:00:03.7802707
    //Thread Controller Thread Exited : Active: 00:14:12.8585963 - Idle: 00:00:03.8552531
    //Thread Controller Thread Exited : Active: 00:14:44.5472368 - Idle: 00:00:04.1075683
    //Thread Controller Thread Exited : Active: 00:14:12.8367502 - Idle: 00:00:04.4745868


    //-----------------------------------------------------------------------------------
    // Thread Controller Configuration: 16 Threads
    //-----------------------------------------------------------------------------------
    //    _MaxWorkItemLimitPerClient: 100
    //    _MaxWorkerThreads: 16
    //    _ThreadStartupPerWorkItems: 4
    //    _ThreadStartupMinQueueSize: 4
    //    _IdleWorkerThreadExitSeconds: 10
    //-----------------------------------------------------------------------------------
    //Baseline Fixed Overhead: 1000 Calls : 00:00:00.0003302
    //Baseline Processing Overhead At 1ms Per Unit Of Work: 1000 Calls : 00:00:15.7817660

    //Note: CPU Bound Test: Tightly coupled rapid processing workloads test
    //Via Concurrent Thread Controller(0ms Per Unit Of Work) - 1000 Calls From 1 Sync Client: 00:00:00.0168385
    //Via Concurrent Thread Controller(0ms Per Unit Of Work) - 1000 Calls From 1 Async Client: 00:00:00.0767332
    //Via Concurrent Thread Controller(0ms Per Unit Of Work) - 10,000 Calls Across 10 Sync Clients: 00:00:00.1360409
    //Via Concurrent Thread Controller(0ms Per Unit Of Work) - 10,000 Calls Across 10 Async Clients: 00:00:01.3950883
    //Via Concurrent Thread Controller(0ms Per Unit Of Work) - 32,000 Calls Across 32 Sync Clients: 00:00:00.3184329
    //Via Concurrent Thread Controller(0ms Per Unit Of Work) - 32,000 Calls Across 32 Async Clients: 00:00:07.3446691
    //Via Concurrent Thread Controller(0ms Per Unit Of Work) - 128,000 Calls Across 128 Sync Clients: 00:00:01.8473372
    //Via Concurrent Thread Controller(0ms Per Unit Of Work) - 128,000 Calls Across 128 Async Clients: 00:00:38.1362445
    //Thread Controller Thread Exited : Active: 00:00:01.5104341 - Idle: 00:00:57.3794845
    //Thread Controller Thread Exited : Active: 00:00:02.0098906 - Idle: 00:01:06.8792660
    //Thread Controller Thread Exited : Active: 00:00:02.5485557 - Idle: 00:01:06.6254954
    //Thread Controller Thread Exited : Active: 00:00:01.6233682 - Idle: 00:01:10.9963926
    //Thread Controller Thread Exited : Active: 00:00:02.9170513 - Idle: 00:01:10.4343526
    //Thread Controller Thread Exited : Active: 00:00:02.2395156 - Idle: 00:01:11.6007862
    //Thread Controller Thread Exited : Active: 00:00:02.5602948 - Idle: 00:01:11.3624199
    //Thread Controller Thread Exited : Active: 00:00:02.0308069 - Idle: 00:01:11.9702175
    //Thread Controller Thread Exited : Active: 00:00:02.2830856 - Idle: 00:01:11.9273927
    //Thread Controller Thread Exited : Active: 00:00:03.2147865 - Idle: 00:01:11.3360625
    //Thread Controller Thread Exited : Active: 00:00:02.5042224 - Idle: 00:01:12.1925743
    //Thread Controller Thread Exited : Active: 00:00:02.2889656 - Idle: 00:01:12.6844794
    //Thread Controller Thread Exited : Active: 00:00:06.0067068 - Idle: 00:01:12.6298587
    //Thread Controller Thread Exited : Active: 00:00:01.9973336 - Idle: 00:01:16.8649831
    //Thread Controller Thread Exited : Active: 00:00:03.9983566 - Idle: 00:01:15.0663355


    //Note: IO Wait Bound Test: Using Thread.Sleep of 1 MS for simulating unit of work processing
    //Baseline Fixed Overhead: 1000 Calls : 00:00:00.0028940
    //Baseline Processing Overhead At 1ms Per Unit Of Work: 1000 Calls : 00:00:15.7583522
    //Via Concurrent Thread Controller(1ms Per Unit Of Work) - 1000 Calls From 1 Sync Client: 00:00:15.7395597
    //Via Concurrent Thread Controller(1ms Per Unit Of Work) - 1000 Calls From 1 Async Client: 00:00:16.0310528
    //Via Concurrent Thread Controller(1ms Per Unit Of Work) - 10,000 Calls Across 10 Sync Clients: 00:00:25.3664534
    //Via Concurrent Thread Controller(1ms Per Unit Of Work) - 10,000 Calls Across 10 Async Clients: 00:00:18.5945677
    //Via Concurrent Thread Controller(1ms Per Unit Of Work) - 32,000 Calls Across 32 Sync Clients: 00:00:30.6395311
    //Via Concurrent Thread Controller(1ms Per Unit Of Work) - 32,000 Calls Across 32 Async Clients: 00:00:26.2611328
    //Via Concurrent Thread Controller(1ms Per Unit Of Work) - 128,000 Calls Across 128 Sync Clients: 00:02:03.2886179
    //Via Concurrent Thread Controller(1ms Per Unit Of Work) - 128,000 Calls Across 128 Async Clients: 00:01:24.0135682
    //Thread Controller Thread Exited : Active: 00:03:31.4075038 - Idle: 00:03:11.9984820
    //Thread Controller Thread Exited : Active: 00:04:02.8784322 - Idle: 00:03:38.0278227
    //Thread Controller Thread Exited : Active: 00:03:33.7211787 - Idle: 00:03:35.3951102
    //Thread Controller Thread Exited : Active: 00:03:34.4590763 - Idle: 00:03:34.6493480
    //Thread Controller Thread Exited : Active: 00:03:36.9121454 - Idle: 00:03:32.1811151
    //Thread Controller Thread Exited : Active: 00:03:38.0511270 - Idle: 00:03:31.0115692
    //Thread Controller Thread Exited : Active: 00:03:40.4586506 - Idle: 00:03:28.5729453
    //Thread Controller Thread Exited : Active: 00:03:26.0137816 - Idle: 00:03:22.7964402
    //Thread Controller Thread Exited : Active: 00:03:23.4156922 - Idle: 00:03:20.1214970
    //Thread Controller Thread Exited : Active: 00:03:27.9747567 - Idle: 00:03:15.4968100
    //Thread Controller Thread Exited : Active: 00:03:27.1450053 - Idle: 00:03:16.3224640
    //Thread Controller Thread Exited : Active: 00:03:27.7215314 - Idle: 00:03:15.7210506
    //Thread Controller Thread Exited : Active: 00:03:30.5497400 - Idle: 00:03:12.8605447
    //Thread Controller Thread Exited : Active: 00:03:31.1018556 - Idle: 00:03:12.2766940
    //Thread Controller Thread Exited : Active: 00:03:31.5062900 - Idle: 00:03:11.8373755
    //Thread Controller Thread Exited : Active: 00:03:25.1289879 - Idle: 00:03:18.5659931


    //Note: CPU Bound Test: Using Spinwait of 1 MS for simulating unit of work processing
    //Note: Using task.delay(0) in the Async client (Task is free to choose async or sync)
    //Baseline Fixed Overhead: 1000 Calls : 00:00:00.0030622
    //Baseline Processing Overhead At 1ms Per Unit Of Work: 1000 Calls : 00:00:15.7465955
    //Via Concurrent Thread Controller(1ms Per Unit Of Work) - 1000 Calls From 1 Sync Client: 00:00:15.7429935
    //Via Concurrent Thread Controller(1ms Per Unit Of Work) - 1000 Calls From 1 Async Client: 00:00:15.7608963
    //Via Concurrent Thread Controller(1ms Per Unit Of Work) - 10,000 Calls Across 10 Sync Clients: 00:00:17.0866723
    //Via Concurrent Thread Controller(1ms Per Unit Of Work) - 10,000 Calls Across 10 Async Clients: 00:00:13.6182518
    //Via Concurrent Thread Controller(1ms Per Unit Of Work) - 32,000 Calls Across 32 Sync Clients: 00:00:30.7611625
    //Via Concurrent Thread Controller(1ms Per Unit Of Work) - 32,000 Calls Across 32 Async Clients: 00:00:16.4585066
    //Via Concurrent Thread Controller(1ms Per Unit Of Work) - 128,000 Calls Across 128 Sync Clients: 00:02:03.1249575
    //Via Concurrent Thread Controller(1ms Per Unit Of Work) - 128,000 Calls Across 128 Async Clients: 00:00:47.5274313
    //Thread Controller Thread Exited : Active: 00:03:11.3014210 - Idle: 00:02:40.3985153
    //Thread Controller Thread Exited : Active: 00:03:13.0973231 - Idle: 00:02:42.8006106
    //Thread Controller Thread Exited : Active: 00:04:10.7408115 - Idle: 00:02:52.2338188
    //Thread Controller Thread Exited : Active: 00:03:40.3554940 - Idle: 00:02:51.0953078
    //Thread Controller Thread Exited : Active: 00:03:39.8180589 - Idle: 00:02:51.6306923
    //Thread Controller Thread Exited : Active: 00:03:42.2931809 - Idle: 00:02:49.1406472
    //Thread Controller Thread Exited : Active: 00:03:41.8441920 - Idle: 00:02:49.5701390
    //Thread Controller Thread Exited : Active: 00:03:44.0822440 - Idle: 00:02:47.3199180
    //Thread Controller Thread Exited : Active: 00:03:23.0700979 - Idle: 00:02:44.4011345
    //Thread Controller Thread Exited : Active: 00:03:12.3268253 - Idle: 00:02:43.2237480
    //Thread Controller Thread Exited : Active: 00:03:13.5053949 - Idle: 00:02:40.3904723
    //Thread Controller Thread Exited : Active: 00:03:10.7683074 - Idle: 00:02:40.9471813
    //Thread Controller Thread Exited : Active: 00:03:09.9815723 - Idle: 00:02:41.7203904
    //Thread Controller Thread Exited : Active: 00:03:10.6191817 - Idle: 00:02:41.0542951
    //Thread Controller Thread Exited : Active: 00:03:14.5218300 - Idle: 00:02:37.1389252
    //Thread Controller Thread Exited : Active: 00:03:15.6247370 - Idle: 00:02:36.2080927

    //Note: CPU Bound Test: Using Spinwait of 1 MS for simulating unit of work processing
    //Note: Using task.yield() in the Async client (Task is forced to use Async)
    //Baseline Fixed Overhead: 1000 Calls : 00:00:00.0002874
    //Baseline Processing Overhead At 1ms Per Unit Of Work: 1000 Calls : 00:00:15.7708985
    //Via Concurrent Thread Controller(1ms Per Unit Of Work) - 1000 Calls From 1 Sync Client: 00:00:15.7688466
    //Via Concurrent Thread Controller(1ms Per Unit Of Work) - 1000 Calls From 1 Async Client: 00:00:15.6638802
    //Via Concurrent Thread Controller(1ms Per Unit Of Work) - 10,000 Calls Across 10 Sync Clients: 00:00:17.3859319
    //Via Concurrent Thread Controller(1ms Per Unit Of Work) - 10,000 Calls Across 10 Async Clients: 00:00:18.8908730
    //Via Concurrent Thread Controller(1ms Per Unit Of Work) - 32,000 Calls Across 32 Sync Clients: 00:00:30.4759494
    //Via Concurrent Thread Controller(1ms Per Unit Of Work) - 32,000 Calls Across 32 Async Clients: 00:00:23.6672730
    //Via Concurrent Thread Controller(1ms Per Unit Of Work) - 128,000 Calls Across 128 Sync Clients: 00:02:03.3421370
    //Via Concurrent Thread Controller(1ms Per Unit Of Work) - 128,000 Calls Across 128 Async Clients: 00:00:59.9787440
    //Thread Controller Thread Exited : Active: 00:03:50.9839834 - Idle: 00:00:45.4750125
    //Thread Controller Thread Exited : Active: 00:03:28.2322072 - Idle: 00:00:48.9484171
    //Thread Controller Thread Exited : Active: 00:03:48.9036053 - Idle: 00:00:47.5358561
    //Thread Controller Thread Exited : Active: 00:03:49.8201413 - Idle: 00:00:46.6713970
    //Thread Controller Thread Exited : Active: 00:03:19.2505098 - Idle: 00:00:51.5042771
    //Thread Controller Thread Exited : Active: 00:03:50.0279519 - Idle: 00:00:46.5138041
    //Thread Controller Thread Exited : Active: 00:03:49.1746199 - Idle: 00:00:47.3509812
    //Thread Controller Thread Exited : Active: 00:03:17.6695001 - Idle: 00:00:53.1341751
    //Thread Controller Thread Exited : Active: 00:03:08.7187709 - Idle: 00:01:01.3218314
    //Thread Controller Thread Exited : Active: 00:03:13.5672141 - Idle: 00:00:56.8415134
    //Thread Controller Thread Exited : Active: 00:03:10.0277515 - Idle: 00:01:00.0254320
    //Thread Controller Thread Exited : Active: 00:03:16.4145220 - Idle: 00:00:54.2715453
    //Thread Controller Thread Exited : Active: 00:03:08.3136204 - Idle: 00:01:01.2392170
    //Thread Controller Thread Exited : Active: 00:03:09.1624477 - Idle: 00:01:00.9043375
    //Thread Controller Thread Exited : Active: 00:03:10.7597847 - Idle: 00:00:59.3478263


    //Note: CPU Bound Test: Using Spinwait of 1 MS for simulating unit of work processing
    //Note: Using task.Delay(1) in the Async client (Task is forced to use Async with a 1 ms wait)
    //Baseline Fixed Overhead: 1000 Calls : 00:00:00.0003945
    //Baseline Processing Overhead At 1ms Per Unit Of Work: 1000 Calls : 00:00:15.7578766
    //Via Concurrent Thread Controller(1ms Per Unit Of Work) - 1000 Calls From 1 Sync Client: 00:00:15.7367911
    //Via Concurrent Thread Controller(1ms Per Unit Of Work) - 1000 Calls From 1 Async Client: 00:00:16.3562684
    //Via Concurrent Thread Controller(1ms Per Unit Of Work) - 10,000 Calls Across 10 Sync Clients: 00:00:17.1059828
    //Via Concurrent Thread Controller(1ms Per Unit Of Work) - 10,000 Calls Across 10 Async Clients: 00:00:19.0927514
    //Via Concurrent Thread Controller(1ms Per Unit Of Work) - 32,000 Calls Across 32 Sync Clients: 00:00:30.6154786
    //Via Concurrent Thread Controller(1ms Per Unit Of Work) - 32,000 Calls Across 32 Async Clients: 00:00:21.4271592
    //Via Concurrent Thread Controller(1ms Per Unit Of Work) - 128,000 Calls Across 128 Sync Clients: 00:02:03.2134529
    //Via Concurrent Thread Controller(1ms Per Unit Of Work) - 128,000 Calls Across 128 Async Clients: 00:01:06.1646710
    //Thread Controller Thread Exited : Active: 00:03:42.7883721 - Idle: 00:01:41.3414784
    //Thread Controller Thread Exited : Active: 00:04:09.6510713 - Idle: 00:01:46.6005559
    //Thread Controller Thread Exited : Active: 00:03:39.0913655 - Idle: 00:01:45.0522429
    //Thread Controller Thread Exited : Active: 00:03:40.9235962 - Idle: 00:01:43.2128493
    //Thread Controller Thread Exited : Active: 00:03:44.1283322 - Idle: 00:01:39.9941937
    //Thread Controller Thread Exited : Active: 00:03:45.5513637 - Idle: 00:01:38.5516912
    //Thread Controller Thread Exited : Active: 00:03:36.2102744 - Idle: 00:01:36.9101569
    //Thread Controller Thread Exited : Active: 00:03:24.1593491 - Idle: 00:01:35.7377693
    //Thread Controller Thread Exited : Active: 00:03:25.2142479 - Idle: 00:01:34.6229023
    //Thread Controller Thread Exited : Active: 00:03:25.7385407 - Idle: 00:01:34.0787880
    //Thread Controller Thread Exited : Active: 00:03:25.5626109 - Idle: 00:01:34.2513996
    //Thread Controller Thread Exited : Active: 00:03:25.0665866 - Idle: 00:01:34.6909919
    //Thread Controller Thread Exited : Active: 00:03:24.9326852 - Idle: 00:01:34.8079893
    //Thread Controller Thread Exited : Active: 00:03:26.0895249 - Idle: 00:01:33.6232426
    //Thread Controller Thread Exited : Active: 00:03:25.5835250 - Idle: 00:01:34.0952484
    //Thread Controller Thread Exited : Active: 00:03:26.6804041 - Idle: 00:01:33.1721094



    //Example Usage for WebAPI controller 
    //class Example
    //{
    //    private static ThreadedWorkItemProcessor<DummyRequest, DummyResponse, int, WorkItemPriority> ThreadedProcessorExample = new ThreadedWorkItemProcessor<DummyRequest, DummyResponse, int, WorkItemPriority>(
    //            _maxWorkItemLimitPerClient: 100 // Maximum number of concurrent requests in the processing queue per client. Set to int.MaxValue to disable concurrent request caps
    //            , _maxWorkerThreads: 16 // Maximum number of threads to scale upto
    //            , _threadStartupPerWorkItems: 4 // Consider starting a new processing thread ever X requests
    //            , _threadStartupMinQueueSize: 4 // Do NOT start a new processing thread if work item queue is below this size
    //            , _idleWorkerThreadExitSeconds: 10 // Idle threads will exit after X seconds
    //            , _abandonedResponseExpirySeconds: 60 // Expire processed work items after X seconds (Maybe the client terminated or the web request thread died)
    //            , _processRequestMethod: ProcessRequestMethod // Your Do Work method for processing the request
    //            , _logErrorMethod: Handler_LogError
    //            , _logMessageMethod: Handler_LogMessage
    //            );

    //    public async Task<DummyResponse> GetResponse([FromBody] DummyRequest _request)
    //    {
    //        int clientID = 1; //Replace with the client ID from your authentication mechanism if using per client request caps. Otherwise just hardcode to maybe 0 or whatever
    //        WorkItemPriority _priority;
    //        _priority = WorkItemPriority.Medium; //Assign the priority based on whatever prioritization rules.
    //        int RequestID = ThreadedProcessorExample.ScheduleWorkItem(_priority, _request, clientID);
    //        if (RequestID < 0)
    //        {
    //            //Client has exceeded maximum number of concurrent requests or Application Pool is shutting down
    //            //return a suitable error message here
    //            return new DummyResponse() { ErrorMessage = @"Maximum number of concurrent requests exceeded or service is restarting. Please retry request later." };
    //        }

    //        //If you need the result (Like in a webapi controller) then do this
    //        //Otherwise if it is say a backend processing sink where there is no client waiting for a response then we are done here. just return.

    //        KeyValuePair<bool, ThreadWorkItem<DummyRequest, DummyResponse, int>> workItemResult;

    //        workItemResult = await ThreadedProcessorExample.TryGetProcessedWorkItemAsync(RequestID,
    //            _timeoutMS: 1000, //Timeout of 1 second
    //            _taskWaitType: ThreadProcessorAsyncTaskWaitType.Delay_Specific,
    //            _delayMS: 10);
    //        if (!workItemResult.Key)
    //        {
    //            //Processing timeout or Application Pool is shutting down
    //            //return a suitable error message here
    //            return new DummyResponse() { ErrorMessage = @"Internal system timeout or service is restarting. Please retry request later." };
    //        }
    //        return workItemResult.Value.Response;
    //    }

    //    public static DummyResponse ProcessRequestMethod(DummyRequest request)
    //    {
    //        // Process the request and return the response
    //        return new DummyResponse() { orderID = request.orderID };
    //    }
    //    public static void Handler_LogError(Exception ex)
    //    {
    //        //Log unhandled exception here
    //    }

    //    public static void Handler_LogMessage(string Message)
    //    {
    //        //Log message here
    //    }
    //}

    public enum WorkItemPriority
    {
        Low = 0,
        Medium = 1,
        High = 2,
    }

    class Program
    {
        private static int BackendAPICallMS = 0;
        private static double BackendAPITimeoutSeconds = 90d;
        private static ThreadProcessorAsyncTaskWaitType WaitType = ThreadProcessorAsyncTaskWaitType.Yield;
        private static int AsyncDelayMS = 1;

        private static int UnitOfWorkProcessingMS_Start = 1;
        private static int UnitOfWorkProcessingMS_End = 1;

        static void Main(string[] args)
        {
            //SpinWait for simulating workload
            //ThreadedProcessor = new ThreadedWorkItemProcessor<DummyRequest, DummyResponse, int>(
            //    _maxWorkItemLimitPerClient: 100
            //    , _maxWorkerThreads: 16
            //    , _threadStartupPerWorkItems: 4
            //    , _threadStartupMinQueueSize: 4
            //    , _idleWorkerThreadExitSeconds: 10
            //    , _abandonedResponseExpirySeconds: 5
            //    , _processRequestMethod: ProcessRequestSpinWait
            //    , _logErrorMethod: Handler_LogError
            //    , _logMessageMethod: Handler_LogMessage
            //    );

            //Sleep for simulating workload
            ThreadedProcessor = new ThreadedWorkItemProcessor<DummyRequest, DummyResponse, int, WorkItemPriority>(
                _maxWorkItemLimitPerClient: int.MaxValue
                , _maxWorkerThreads: 16
                , _threadStartupPerWorkItems: 4
                , _threadStartupMinQueueSize: 4
                , _idleWorkerThreadExitSeconds: 10
                , _abandonedResponseExpirySeconds: 5
                , _processRequestMethod: ProcessRequest
                , _logErrorMethod: Handler_LogError
                , _logMessageMethod: Handler_LogMessage
                );


            ThreadedWorkItemProcessor<DummyRequest, DummyResponse, string, WorkItemPriority> blah;

            BackendAPICallMS = 0;
            Stopwatch SW;
            SW = new Stopwatch();

            SW.Reset();
            SW.Start();
            Test();
            SW.Stop();
            Console.WriteLine(string.Concat(@"Baseline Fixed Overhead: 1000 Calls : ", SW.Elapsed.ToString()));

            BackendAPICallMS = 1;

            SW.Reset();
            SW.Start();
            Test();
            SW.Stop();
            Console.WriteLine(string.Concat(@"Baseline Processing Overhead At 1ms Per Unit Of Work: 1000 Calls : ", SW.Elapsed.ToString()));


            for (int N = UnitOfWorkProcessingMS_Start; N <= UnitOfWorkProcessingMS_End; N++)
            {
                BackendAPICallMS = N;
                SW.Reset();
                SW.Start();
                TestThreadController();
                SW.Stop();
                Console.WriteLine(@"Via Concurrent Thread Controller ({0}ms Per Unit Of Work) - 1000 Calls From 1 Sync Client: {1}", BackendAPICallMS, SW.Elapsed.ToString());


                SW.Reset();
                SW.Start();
                TestThreadControllerAsync();
                SW.Stop();
                Console.WriteLine(@"Via Concurrent Thread Controller ({0}ms Per Unit Of Work) - 1000 Calls From 1 Async Client: {1}", BackendAPICallMS, SW.Elapsed.ToString());


                List<System.Threading.Thread> ThreadList = new List<System.Threading.Thread>();
                for (int I = 0; I < 10; I++)
                {
                    System.Threading.Thread RunThread;
                    RunThread = new System.Threading.Thread(new System.Threading.ThreadStart(TestThreadController));
                    ThreadList.Add(RunThread);
                }
                SW.Reset();
                SW.Start();
                foreach (System.Threading.Thread RunThread in ThreadList)
                {
                    RunThread.Start();
                }
                foreach (System.Threading.Thread RunThread in ThreadList)
                {
                    RunThread.Join();
                }
                SW.Stop();
                Console.WriteLine(@"Via Concurrent Thread Controller ({0}ms Per Unit Of Work) - 10,000 Calls Across 10 Sync Clients: {1}", BackendAPICallMS, SW.Elapsed.ToString());

                ThreadList = new List<System.Threading.Thread>();
                for (int I = 0; I < 10; I++)
                {
                    System.Threading.Thread RunThread;
                    RunThread = new System.Threading.Thread(new System.Threading.ThreadStart(TestThreadControllerAsync));
                    ThreadList.Add(RunThread);
                }
                SW.Reset();
                SW.Start();
                foreach (System.Threading.Thread RunThread in ThreadList)
                {
                    RunThread.Start();
                }
                foreach (System.Threading.Thread RunThread in ThreadList)
                {
                    RunThread.Join();
                }
                SW.Stop();
                Console.WriteLine(@"Via Concurrent Thread Controller ({0}ms Per Unit Of Work) - 10,000 Calls Across 10 Async Clients: {1}", BackendAPICallMS, SW.Elapsed.ToString());


                ThreadList = new List<System.Threading.Thread>();
                for (int I = 0; I < 32; I++)
                {
                    System.Threading.Thread RunThread;
                    RunThread = new System.Threading.Thread(new System.Threading.ThreadStart(TestThreadController));
                    ThreadList.Add(RunThread);
                }
                SW.Reset();
                SW.Start();
                foreach (System.Threading.Thread RunThread in ThreadList)
                {
                    RunThread.Start();
                }
                foreach (System.Threading.Thread RunThread in ThreadList)
                {
                    RunThread.Join();
                }
                SW.Stop();
                Console.WriteLine(@"Via Concurrent Thread Controller ({0}ms Per Unit Of Work) - 32,000 Calls Across 32 Sync Clients: {1}", BackendAPICallMS, SW.Elapsed.ToString());


                ThreadList = new List<System.Threading.Thread>();
                for (int I = 0; I < 32; I++)
                {
                    System.Threading.Thread RunThread;
                    RunThread = new System.Threading.Thread(new System.Threading.ThreadStart(TestThreadControllerAsync));
                    ThreadList.Add(RunThread);
                }
                SW.Reset();
                SW.Start();
                foreach (System.Threading.Thread RunThread in ThreadList)
                {
                    RunThread.Start();
                }
                foreach (System.Threading.Thread RunThread in ThreadList)
                {
                    RunThread.Join();
                }
                SW.Stop();
                Console.WriteLine(@"Via Concurrent Thread Controller ({0}ms Per Unit Of Work) - 32,000 Calls Across 32 Async Clients: {1}", BackendAPICallMS, SW.Elapsed.ToString());


                ThreadList = new List<System.Threading.Thread>();
                for (int I = 0; I < 128; I++)
                {
                    System.Threading.Thread RunThread;
                    RunThread = new System.Threading.Thread(new System.Threading.ThreadStart(TestThreadController));
                    ThreadList.Add(RunThread);
                }
                SW.Reset();
                SW.Start();
                foreach (System.Threading.Thread RunThread in ThreadList)
                {
                    RunThread.Start();
                }
                foreach (System.Threading.Thread RunThread in ThreadList)
                {
                    RunThread.Join();
                }
                SW.Stop();
                Console.WriteLine(@"Via Concurrent Thread Controller ({0}ms Per Unit Of Work) - 128,000 Calls Across 128 Sync Clients: {1}", BackendAPICallMS, SW.Elapsed.ToString());


                ThreadList = new List<System.Threading.Thread>();
                for (int I = 0; I < 128; I++)
                {
                    System.Threading.Thread RunThread;
                    RunThread = new System.Threading.Thread(new System.Threading.ThreadStart(TestThreadControllerAsync));
                    ThreadList.Add(RunThread);
                }
                SW.Reset();
                SW.Start();
                foreach (System.Threading.Thread RunThread in ThreadList)
                {
                    RunThread.Start();
                }
                foreach (System.Threading.Thread RunThread in ThreadList)
                {
                    RunThread.Join();
                }
                SW.Stop();
                Console.WriteLine(@"Via Concurrent Thread Controller ({0}ms Per Unit Of Work) - 128,000 Calls Across 128 Async Clients: {1}", BackendAPICallMS, SW.Elapsed.ToString());

                //Wait for all thread controller threads to exit
                System.Threading.Thread.Sleep(15);
                System.Threading.Thread.Sleep(15);
                System.Threading.Thread.Sleep(15);
                System.Threading.Thread.Sleep(15);
                System.Threading.Thread.Sleep(15);
                System.Threading.Thread.Sleep(15);
                System.Threading.Thread.Sleep(15);
            }


            DummyResponse response;
            response = GetResponse(new DummyRequest() { orderID = 1 }, 0);
            ThreadedProcessor.ShutDown(60);
        }

        private static ThreadedWorkItemProcessor<DummyRequest, DummyResponse, int, WorkItemPriority> ThreadedProcessor;
            
        private static Random Rnd = new Random();
        private static void Test()
        {
            for (int I = 0; I < 1000; I++)
            {
                int ClientID = Rnd.Next(0, 20);
                DummyResponse response;
                ThreadWorkItem<DummyRequest, DummyResponse, int> workItem;
                workItem = new ThreadWorkItem<DummyRequest, DummyResponse, int>(ClientID);
                workItem.Request = new DummyRequest() { orderID = Rnd.Next(0, 20) };

                response = ProcessRequest(workItem);
                if (response.orderID != I)
                {
                    Console.WriteLine(string.Concat(@"Request Response ID Missmatch:- RequestID: ", I.ToString(), @" - ResponseID: ", response.orderID.ToString()));
                }
            }
        }

        private static void TestThreadController()
        {
            for (int I = 0; I < 1000; I++)
            {
                int ClientID = Rnd.Next(0, 20);
                DummyResponse response;
                response = GetResponse(new DummyRequest() { orderID = I }, ClientID);
                if (response.orderID != I)
                {
                    Console.WriteLine(string.Concat(@"Request Response ID Missmatch:- RequestID: ", I.ToString(), @" - ResponseID: ", response.orderID.ToString()));
                }
            }
        }

        private static void TestThreadControllerAsync()
        {
            for (int I = 0; I < 1000; I++)
            {
                int ClientID = Rnd.Next(0, 20);
                Task<DummyResponse> task = Task.Run<DummyResponse>(async () => await GetResponseAsync(new DummyRequest() { orderID = I }, ClientID));
                if (task.Result.orderID != I)
                {
                    Console.WriteLine(string.Concat(@"Request Response ID Missmatch:- RequestID: ", I.ToString(), @" - ResponseID: ", task.Result.orderID.ToString()));
                }
            }
        }


        public static DummyResponse GetResponse(DummyRequest DummyRequest, int ClientID)
        {
            int RequestID = ThreadedProcessor.ScheduleWorkItem((WorkItemPriority)Rnd.Next(0, 2), DummyRequest, ClientID);
            if (RequestID < 0) { return new DummyResponse() { orderID = -1 }; }

            ThreadWorkItem<DummyRequest, DummyResponse, int> workItem;
            DateTime StartTime = DateTime.Now;
            while (!ThreadedProcessor.TryGetProcessedWorkItem(RequestID, out workItem))
            {
                System.Threading.Thread.Sleep(0);
                if (DateTime.Now.Subtract(StartTime).TotalSeconds > BackendAPITimeoutSeconds)
                {
                    return new DummyResponse() { orderID = -1 };
                }
            }
            if (workItem.ClientID == ClientID)
            {
                return workItem.Response;
            }
            return new DummyResponse() { orderID = -1 };
        }



        public static async Task<DummyResponse> GetResponseAsync(DummyRequest DummyRequest, int ClientID)
        {
            int RequestID = ThreadedProcessor.ScheduleWorkItem((WorkItemPriority)Rnd.Next(0, 2), DummyRequest, ClientID);
            KeyValuePair<bool,ThreadWorkItem<DummyRequest, DummyResponse, int>> workItemResult;
            workItemResult = await ThreadedProcessor.TryGetProcessedWorkItemAsync(RequestID, 1000, WaitType, AsyncDelayMS);
            //ThreadedWorkItem<DummyRequest, DummyResponse, int> workItem;
            //while (!ThreadedProcessor.TryGetProcessedWorkItem(RequestID, out workItem))
            //{
            //    switch (WaitType)
            //    {
            //        case ThreadProcessorAsyncTaskWaitType.Yield:
            //            await Task.Yield();
            //            break;
            //        case ThreadProcessorAsyncTaskWaitType.Delay_0:
            //            await Task.Delay(0);
            //            break;
            //        case ThreadProcessorAsyncTaskWaitType.Delay_1:
            //            await Task.Delay(1);
            //            break;
            //        case ThreadProcessorAsyncTaskWaitType.Delay_Specific:
            //            await Task.Delay(AsyncDelayMS);
            //            break;
            //    }
            //}
            if (!workItemResult.Key)
            {
                return new DummyResponse() { orderID = -1 };
            }
            else if (workItemResult.Value.ClientID == ClientID)
            {
                return workItemResult.Value.Response;
            }
            return new DummyResponse() { orderID = -1 };
        }

        public static DummyResponse ProcessRequest(ThreadWorkItem<DummyRequest, DummyResponse, int> _workitem)
        {
            if (BackendAPICallMS > 0)
            {
                System.Threading.Thread.Sleep(BackendAPICallMS);
            }
            return new DummyResponse() { orderID = _workitem.Request.orderID };
        }


        [ThreadStatic]
        private static Stopwatch SWProcessingWait;
        public static DummyResponse ProcessRequestSpinWait(DummyRequest request)
        {
            if (SWProcessingWait == null) { SWProcessingWait = new Stopwatch(); SWProcessingWait.Reset(); }
            // Include StopWatch in overhead calculations
            SWProcessingWait.Reset(); 
            SWProcessingWait.Start();
            if (BackendAPICallMS > 0)
            {
                System.Threading.SpinWait.SpinUntil(() => SWProcessingWait.Elapsed.TotalMilliseconds > ((double)BackendAPICallMS));
            }
            SWProcessingWait.Stop();
            return new DummyResponse() { orderID = request.orderID };
        }

        public static void Handler_LogError(Exception ex)
        {

        }

        public static void Handler_LogMessage(string Message)
        {

        }


    }
}