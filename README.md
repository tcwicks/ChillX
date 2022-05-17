

# ChillX

ChillX is currently a work in progress for creating a Micro Services host with builtin Message Queue, Serialization and Transport in Dot Net.
Libraries are Dot Net Standard 2.0 so its usable in both Dot Net Framework and Dot Net Core

## Overall objectives / goals:
- Performance as a feature
- Seamless horizontal (load) and vertical (functionality) scalability of hosted micro services
- Independent of IIS. Microservices could be published / consumed using its built in Message Queue or IIS or Kestrel or custom gateway or a multi user winforms app etc...
- Atomic request / transaction processing

## Implementation roadmap:
Please note this is a work in progress.

### Complete:
- Application Logging Framework: [ChillX.Logging Description](https://github.com/tcwicks/ChillX/blob/master/docs/ChillXLogging.md "ChillX.Logging Description") - [Source Code](https://github.com/tcwicks/ChillX/tree/master/src/ChillX.Logging "Source Code")
- Serialization: ChillX.Serialization description coming soon - [Source Code](https://github.com/tcwicks/ChillX/tree/master/src/ChillX.Serialization "Source Code")
- Multi Threaded Unit of Work: [ChillX.Threading Description](https://github.com/tcwicks/ChillX/blob/master/docs/ChillXThreading.md "ChillX.Threading Description") - [Source Code](https://github.com/tcwicks/ChillX/tree/master/src/ChillX.Threading "Source Code")

### In Progress:
- Common functionality: [Source Code](https://github.com/tcwicks/ChillX/tree/master/src/ChillX.Core "Source Code")
- Microservices Host with built in reliable Message Queue and point to multi point message routing

## Motivation
So why do we need this when we already have message queue frameworks such as ZeroMQ, FastMQ, RabbitMQ and serializers such as MessagePackSharp ?

A Message queue plus serializer are simply two of the building  blocks for a Service / Microservices host. It is does not include any of the plumbing work required for:

- Reliable request processing
- Message routing
- Load management
- Minimizing GC overheads
- etc...

As a matter of fact ZeroMQ plus MessagePackSharp used to be my goto frameworks for implementing Services / Micro Services ranging from ETL to Machine Learning model processors (Tensorflow.Net / TorchSharp) to various other transaction processing backends. Unfortunately however each time I end up having to re-implement a significant amount of plumbing. Hence I decided to put this framework together combining the best features from each implementation.

### Custom serializer:

Another significant challenge is GC overheads. The standard implementation of BitConverter creates a ridiculous amount of garbage from intermediaries. When aiming for high throughput what we end up with instead is 80% time spent in GC which is quite counter productive. Resolving this issue is the motivation behind ChillX.Serializer which uses a pool of managed buffers of primitive types instead of temporary intermediaries. Additionally it includes a rewrite of BitConverter with significant added functionality. Serialization of type <T> itself is implemented using cached reflection and expression trees.

### Managed multi threading:

So why do we need a wrapper around the background threadpool? 
When implementing an API gateway the most straight forward and often used pattern is to spin up an WebAPI service with NewtonSoft JSON as a wrapper around the backend processing services and simply offload to the background thread pool using the Async keyword. Async / Task.Run  is both a curse and a blessing as it allows us to blindly fire and forget into the background threadpool. However there is one fundamental flaw in doing this. The backround threadpool scales much faster than the load handling capacity of the backend systems.
Consider the following scenario:
Imagine that we have a pricing and stock availability ecommerce API service where the requests are processed by an ERP system (SAP, Oracle, etc...)
The load (number of concurrent requests) generated is outside our control and is dependent on user / customer behavior. In real world usage this load is most likely going to have significant bursts / peaks which are probably an order of magnitude greater than the average load. What would happen if we suddenly receive 100 concurrent requests when the average load is say 10 concurrent requests? The ERP grinds to a halt, requests timeout, users trying to perform other tasks in the ERP might get timed out / kicked out, etc... With complex backend processing applications such as an ERP (or even just a DB server such as SQL server), when load exceeds capacity, performance degrades exponentially. 
As a simplified example: 
- If capacity is 10 requests per second.
- If average load is 5 requests per second. 

if we get a peak of 100 concurrent requests it will take a lot longer than 10 seconds to process these. Performance is most likely going to degrade to well below 50% of capacity. If the average load of 5 requests per second continues the system will not be able to recover. Sure the WebAPI requests will timeout however not before adding their requests to the pending queue in the ERP. The end result for the ERP or other backend processing application is a death spiral and crash.

### Offloaded logging

Application logging is essential however this should never be at the expense of performance. Consider a scenario where API requests are being logged to a database. If this is done synchronously then:
- At best we are reducing performance by adding the logging round trip time on top of the actual request processing time.
- At worst we are creating a performance bottleneck and limiting throughput

What if there were multiple log entries written to the database per API request... The load vs capacity problem now compounds.
What if the logging database is hosted on the same database server as the application as well. The load vs capacity problem now compounds further.

Log entries could be bulk written do a database using BCP. How would this be done of the log entries are being written synchronously one at a time?

ChillX.Logging is implemented as a log entry queue which adds each entry to a pending queue and returns immediately. Log entries themselves are committed to durable storage such as a DB server by a separate thread. 

### Message Queue
documentation to be continued...
