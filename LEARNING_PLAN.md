# WellEdge — Full-Stack Learning Plan
### Real-Time Well Intervention System (Backend-Focused)

---

## Context

This plan prepares you for a Full-Stack Developer role on a real-time well intervention platform running on edge servers at wellsite locations. The system ingests live sensor data, executes engineering calculations at 4–8 Hz, and streams results to operator dashboards — with strict requirements for reliability, low latency, and offline-capable operation.

You already know Angular. MongoDB is familiar. **C# and the .NET ecosystem are new territory**, so the plan front-loads that foundation and builds depth on the backend patterns that make this role technically distinctive.

---

## The Project: WellEdge

A mini edge monitoring system for a single oil well. It never gets thrown away — each phase adds a real layer to the same codebase.

| Phase | What WellEdge becomes |
|-------|-----------------------|
| 1 | Console app logging fake sensor readings every 250 ms |
| 2 | Worker Service running the simulator as a proper background process |
| 3 | Channels pipeline: readings flow through validate → calculate → publish stages |
| 4 | MediatR decouples internal events — producers don't know about consumers |
| 5 | SignalR hub broadcasts processed readings to any connected client |
| 6 | MongoDB time-series collection persists every reading |
| 7 | MQTT replaces the fake generator — Mosquitto broker as the data source |
| 7b | Angular dashboard consumes the SignalR hub with Signals and RxJS |
| 8 | Serilog logging, graceful shutdown, automatic reconnection |

**Simulated sensors:** surface pressure (PSI), flow rate (bbl/min), pipe temperature (°C).  
No real hardware needed. You run Mosquitto locally for MQTT and a local MongoDB instance.

---

## Stack

| Layer | Technology |
|-------|-----------|
| Language | C# 13 / .NET 9 |
| Runtime | ASP.NET Core, Worker Services |
| Pipeline | System.Threading.Channels |
| Events | MediatR |
| Real-time | SignalR |
| Database | MongoDB 7 (time-series collections) |
| Messaging | MQTT via MQTTnet + Mosquitto (local broker) |
| Frontend | Angular 18+, Signals, RxJS, SignalR JS client |
| Observability | Serilog |

---

## How to Use This Plan

Each phase has three parts:

1. **Learn** — concepts to study with pointers to free resources
2. **Build** — what to add to WellEdge (instructions, not code)
3. **Check** — questions you should be able to answer before moving on

The check questions are not answered here. You find the answers by reading, experimenting, and breaking things. If you can explain an answer out loud in plain English, you understand it.

---

---

# Phase 1 — C# Foundations
**Duration:** Week 1 (4h/day)  
**Goal:** Be productive in C# coming from TypeScript. Understand the concepts that differ, not just the syntax.

---

### Learn

**Start here — C# from a TypeScript developer's perspective:**

The mental model shift matters more than the syntax. These are the concepts that will trip you up if you treat C# like TypeScript:

- **Value types vs reference types** — `struct` vs `class`, stack vs heap. TypeScript has no equivalent. Understanding this prevents subtle bugs in concurrent code later.
- **`async`/`await` in C#** — Similar to JS, but `Task<T>` is not a Promise. There is no event loop. The thread pool manages continuations differently. Read how `ConfigureAwait(false)` works and why it matters in library code.
- **Interfaces and DI** — C# has first-class DI built into .NET. `IServiceCollection`, `AddSingleton`, `AddScoped`, `AddTransient`. You will use this constantly.
- **Generics** — More powerful than TypeScript generics. Constraints (`where T : class`), covariance/contravariance.
- **`using` and `IDisposable`** — Deterministic resource cleanup. No garbage collection for I/O handles, connections, or channels. You must dispose things.
- **`record` types** — Immutable data carriers. Heavily used in clean architecture. Similar to TypeScript's readonly interfaces but with value equality built in.
- **Nullable reference types** — The `?` means something different from TypeScript. Understand `#nullable enable` and why it matters.

**Free Resources:**
- [Microsoft Learn — C# Fundamentals](https://learn.microsoft.com/en-us/dotnet/csharp/tour-of-csharp/) — official, free, structured
- [Nick Chapsas — YouTube](https://www.youtube.com/@nickchapsas) — best practical .NET content on YouTube, watch his C# for beginners series
- C# language reference docs — bookmark and use as reference, not tutorial

---

### Build — Phase 1

Create a solution called `WellEdge` with a single console project `WellEdge.Simulator`.

The console app should:
1. Define a `SensorReading` record with: `WellId`, `Timestamp`, `PressurePsi`, `FlowRateBblPerMin`, `TemperatureCelsius`
2. Write a `SensorSimulator` class with a method that generates a realistic random reading every 250 ms
3. Use a `CancellationToken` hooked to `Ctrl+C` to shut down cleanly
4. Print each reading to the console in a readable format

Do not use any NuGet packages yet. Only `System` namespaces.

The goal is to get comfortable with: records, async/await, CancellationToken, basic loop structure, and how a .NET console app is structured.

---

### Check — Phase 1

Before moving to Phase 2, you should be able to answer these without looking:

1. What is the difference between `Task` and `Task<T>`? When do you use each?
2. What does `await` actually do to the thread while it waits?
3. Why does `async void` exist and why is it almost always wrong to use it?
4. What is the difference between `IEnumerable<T>` and `IAsyncEnumerable<T>`? When would you choose one over the other?
5. What does `IDisposable` protect you from, and what happens if you forget to call `Dispose()`?
6. What is the difference between a `class` and a `record` in C#? Why would you prefer a record for `SensorReading`?
7. If you have a method `public async Task DoSomething()` that you call without `await`, what happens? Is it a compile error? A runtime error? Something else?
8. What is a `CancellationToken` and why is it the correct way to signal shutdown instead of a boolean flag?

---

---

# Phase 2 — Worker Services + Clean Architecture
**Duration:** Week 2  
**Goal:** Restructure WellEdge as a proper long-running service with a clean, layered architecture. Understand how .NET hosts services.

---

### Learn

**Worker Services and the .NET Generic Host:**

A Worker Service is a console application that knows how to host long-running background work, manage startup/shutdown, configure DI, and read configuration. It is the foundation for everything else in this stack.

Key concepts:
- `IHostedService` and `BackgroundService` — the base class for all background work. Understand the difference between `StartAsync`, `ExecuteAsync`, and `StopAsync`, and why the lifecycle contract matters.
- `IHost` and `IHostBuilder` — how the host is assembled, how DI is configured, how configuration sources are layered (appsettings.json, environment variables, command line).
- **Options pattern** — `IOptions<T>`, `IOptionsSnapshot<T>`. How to bind configuration sections to typed classes. You will use this for sensor frequency, MongoDB connection strings, MQTT broker address, etc.
- **Hosted service lifetime** — what happens when an unhandled exception escapes `ExecuteAsync`? How does the host respond? How do you prevent a crashed worker from silently dying?

**Clean Architecture (simplified):**

The profile requires Clean Architecture patterns. For WellEdge you don't need all layers — you need to understand the core principle: **dependencies point inward, never outward**. Domain knows nothing about infrastructure; infrastructure implements interfaces defined by domain.

Layers for WellEdge:
- `Domain` — `SensorReading`, `ProcessedReading`, `IReadingRepository` (interface only)
- `Application` — calculation logic, pipeline orchestration, use cases
- `Infrastructure` — MongoDB implementation, MQTT client, SignalR hub
- `Worker` — the host entry point, DI wiring, startup

Key read: understand what a "port" (interface) and "adapter" (implementation) means. This vocabulary appears in every code review on this team.

**Free Resources:**
- [Microsoft Learn — Worker Services](https://learn.microsoft.com/en-us/dotnet/core/extensions/workers)
- [Microsoft Learn — Generic Host](https://learn.microsoft.com/en-us/dotnet/core/extensions/generic-host)
- [Microsoft Learn — Options pattern](https://learn.microsoft.com/en-us/dotnet/core/extensions/options)
- Nick Chapsas — "Background Services in .NET" (YouTube)

---

### Build — Phase 2

Restructure `WellEdge` into a multi-project solution:

```
WellEdge/
  WellEdge.Domain/          ← records, interfaces, no dependencies
  WellEdge.Application/     ← calculation logic
  WellEdge.Infrastructure/  ← (empty for now, will grow)
  WellEdge.Worker/          ← host entry point
```

In `WellEdge.Worker`:
1. Replace the console app with a proper Worker Service using `BackgroundService`
2. Move `SensorSimulator` into `Application`, register it via DI
3. Add a `SimulatorOptions` class and bind it from `appsettings.json` (configurable frequency in Hz and well ID)
4. The `BackgroundService` uses `SimulatorOptions` to control the reading interval
5. Handle cancellation: when the host shuts down, the worker exits cleanly within 5 seconds

The reading should still just print to console — persistence and streaming come later.

---

### Check — Phase 2

1. What is the difference between `AddSingleton`, `AddScoped`, and `AddTransient`? For a `SensorSimulator` that runs continuously, which lifetime is correct and why?
2. If `ExecuteAsync` throws an unhandled exception, does the host crash? How do you control this behavior?
3. What is the difference between `IHostedService` and `BackgroundService`? When would you implement `IHostedService` directly instead of inheriting from `BackgroundService`?
4. What is `CancellationToken stoppingToken` in `ExecuteAsync`? Who cancels it and when?
5. The Options pattern has three interfaces: `IOptions<T>`, `IOptionsSnapshot<T>`, `IOptionsMonitor<T>`. What is the difference? Which one would you use for a setting that can change at runtime without restarting the service?
6. In Clean Architecture, why can't the `Domain` project reference the `Infrastructure` project? What problem does this solve in practice?
7. If you need to run two independent background tasks in the same Worker Service (e.g., one that reads sensors and one that checks health), what are your options?

---

---

# Phase 3 — System.Threading.Channels
**Duration:** Week 3  
**Goal:** Understand the core pipeline pattern used for high-frequency data processing. This is the most technically distinctive concept in the backend role.

---

### Learn

**Why Channels, and not just `Task.Run`?**

This is the first question to answer before reading any API docs. The team uses `System.Threading.Channels` to move data between concurrent stages of a pipeline. Understanding the problem it solves makes the API obvious.

Key concepts:
- **Producer/Consumer pattern** — one side writes, another side reads, without knowing about each other. This decouples the sensor reading rate from the processing rate.
- **`Channel<T>`** — an in-memory, thread-safe queue. `Channel.CreateBounded<T>(capacity)` vs `Channel.CreateUnbounded<T>()`. Understand why bounded channels exist and what backpressure means.
- **`ChannelWriter<T>` and `ChannelReader<T>`** — the two sides of the channel. The writer never blocks a reader; the reader consumes asynchronously.
- **`await reader.ReadAllAsync(cancellationToken)`** — the idiomatic way to consume all items until the channel is closed and drained.
- **Backpressure** — what happens when a `BoundedChannel` is full and the producer tries to write? You have options: `Wait`, `DropOldest`, `DropNewest`, `DropWrite`. Each makes a different engineering trade-off for high-frequency sensor data.
- **Pipeline stages** — you can chain channels. Stage 1 reads from MQTT and writes raw readings to Channel A. Stage 2 reads from Channel A, validates, and writes to Channel B. Stage 3 reads from Channel B and publishes via SignalR. Each stage is an independent `BackgroundService`.

**Free Resources:**
- [Microsoft Docs — System.Threading.Channels](https://learn.microsoft.com/en-us/dotnet/core/extensions/channels)
- Stephen Toub — "An Introduction to System.Threading.Channels" (devblogs.microsoft.com)
- Nick Chapsas — "System.Threading.Channels" (YouTube)

---

### Build — Phase 3

Add a two-stage pipeline to WellEdge:

**Stage 1 — Ingestion Worker:**
- Replaces the previous simple simulator
- Writes raw `SensorReading` objects to a `Channel<SensorReading>` at 4 Hz
- Uses a `BoundedChannel` with capacity 100 and `DropOldest` overflow policy

**Stage 2 — Processing Worker:**
- Reads from the channel
- Validates readings (reject negative pressure, reject flow rate above physical max)
- Calculates a derived value: annular pressure = surface pressure + (depth * fluid gradient). Use a hardcoded depth of 2000m and fluid gradient of 0.45 PSI/m for now.
- Produces a `ProcessedReading` record that includes the original reading plus the calculated value
- For now, prints the `ProcessedReading` to the console

Both stages are separate `BackgroundService` implementations registered in DI. The `Channel<SensorReading>` is registered as a singleton so both services share the same instance.

---

### Check — Phase 3

1. What is the difference between a `BoundedChannel` and an `UnboundedChannel`? What is the risk of using an unbounded channel in a production system with a fast producer?
2. Explain backpressure in plain English. Why does it matter at 4–8 Hz with engineering calculations that take variable time?
3. What happens to the consuming worker if `channel.Writer.Complete()` is called while it is blocked on `ReadAsync`?
4. If your processing stage takes 300 ms per reading but readings arrive every 250 ms, what happens over time? How does a bounded channel with `DropOldest` change the behavior?
5. Why would you run each pipeline stage as a separate `BackgroundService` instead of one big service that does everything sequentially in a single loop?
6. How would you add a third stage to the pipeline (e.g., a persistence stage that writes to MongoDB) without modifying either Stage 1 or Stage 2?
7. What is `ConfigureAwait(false)` and why should library-level pipeline code use it?

---

---

# Phase 4 — MediatR + Event-Driven Architecture
**Duration:** Week 4  
**Goal:** Learn how MediatR decouples the pipeline from its consumers. Understand when to use notifications vs direct method calls.

---

### Learn

**The problem MediatR solves:**

In Phase 3, Stage 2 calls a specific service to print to console. In production, Stage 2 needs to trigger: save to MongoDB, publish to SignalR, write to a local log file, and update an in-memory alarm checker. If Stage 2 knows about all of these, it becomes tightly coupled.

MediatR's `INotification` pattern solves this: Stage 2 publishes a `ReadingProcessed` notification and knows nothing about who handles it.

Key concepts:
- **`IRequest<T>` vs `INotification`** — Requests expect exactly one handler and a response (like a query or command). Notifications expect zero or more handlers and no response. For sensor pipeline events, notifications are correct.
- **`INotificationHandler<T>`** — implement this to react to a notification. Register multiple handlers for the same notification type.
- **`IPublisher.Publish()`** — publishes a notification to all registered handlers. By default, handlers run sequentially. Understand why this matters for determinism in a real-time system.
- **Pipeline behaviors (`IPipelineBehavior<T>`)** — middleware for request handling: logging, validation, timing. Learn what they are even if you don't build one in this phase.
- **When NOT to use MediatR** — tight inner loops at 4 Hz are not good candidates for MediatR if handler dispatch adds measurable latency. The team uses Channels for the hot path and MediatR for lifecycle events and cross-cutting concerns.

**Free Resources:**
- [MediatR GitHub — README and wiki](https://github.com/jbogard/MediatR)
- Nick Chapsas — "MediatR in .NET" (YouTube)
- Jimmy Bogard's blog posts on MediatR (the library author)

---

### Build — Phase 4

Replace the direct console print in Stage 2 with a MediatR notification:

1. Define a `ReadingProcessed` notification record in `Domain` or `Application`
2. Stage 2's processing worker publishes `ReadingProcessed` after calculating the derived values
3. Create two handlers:
   - `ConsoleLoggingHandler` — prints the reading to console (replaces current behavior)
   - `AlarmCheckHandler` — checks if pressure exceeds 5000 PSI and prints a warning
4. Both handlers implement `INotificationHandler<ReadingProcessed>` and are registered in DI

Stage 2 now knows nothing about console, alarms, MongoDB, or SignalR. It only knows about `IPublisher`.

---

### Check — Phase 4

1. What is the difference between `IRequest` and `INotification` in MediatR? Give an example of when you would use each in WellEdge.
2. If two handlers are registered for the same `INotification`, in what order do they run by default? Is this guaranteed?
3. Why is it wrong to use `INotification` for the hot data path (the 4 Hz reading loop) in a production system?
4. What is a `IPipelineBehavior`? Describe a real use case where you would add one.
5. MediatR uses `IServiceProvider` to resolve handlers at runtime. What does this mean for the handler lifetime? If a handler needs a scoped service (like a database context), how do you handle that in a singleton pipeline?
6. What is the difference between `Publish` (sequential) and a parallel publish strategy? When would parallel handlers be dangerous in a deterministic real-time system?
7. A teammate says "just use MediatR everywhere, it decouples everything." What are the real costs of this approach?

---

---

# Phase 5 — SignalR
**Duration:** Week 5  
**Goal:** Stream processed readings to any connected client in real time. Understand hub contracts, groups, and reconnection behavior.

---

### Learn

**SignalR in a .NET backend:**

SignalR is the real-time bridge between the edge server and the operator dashboard. The backend sends data; connected Angular clients receive it without polling.

Key concepts:
- **Hub** — the server-side class that manages connections and sends messages. Methods on the hub can be called from clients; clients can also be called from the server.
- **`IHubContext<T>`** — lets you send messages to connected clients from outside the hub class (e.g., from your MediatR handler). You will use this constantly because the hub itself isn't where messages originate.
- **Typed hubs** — instead of `Clients.All.SendAsync("MethodName", data)`, you define an interface and use `Clients.All.ReceiveReading(reading)`. This gives you compile-time safety on the contract between server and client.
- **Groups** — a client can join a group (e.g., by well ID). The server can then broadcast to all clients watching a specific well. Understand how groups survive reconnections (they don't — the client must rejoin).
- **Connection lifetime** — what happens when a client disconnects? What happens when it reconnects? Who is responsible for re-subscribing to groups?
- **Transport fallback** — WebSockets, Server-Sent Events, Long Polling. In a LAN-only wellsite environment with a controlled network, you can pin to WebSockets.
- **Hub vs minimal API endpoint** — SignalR is for push (server → client). If the client needs to send data to the server, that can be a regular HTTP endpoint or a hub method. Know when to use each.

**Free Resources:**
- [Microsoft Docs — ASP.NET Core SignalR overview](https://learn.microsoft.com/en-us/aspnet/core/signalr/introduction)
- [Microsoft Docs — Use hubs in SignalR](https://learn.microsoft.com/en-us/aspnet/core/signalr/hubs)
- [Microsoft Docs — Send messages from outside a hub](https://learn.microsoft.com/en-us/aspnet/core/signalr/hubcontext)
- Nick Chapsas — "SignalR in .NET" series (YouTube)

---

### Build — Phase 5

Add a SignalR hub and a new MediatR handler that broadcasts readings:

1. Add `Microsoft.AspNetCore.SignalR` — switch `WellEdge.Worker` to a minimal ASP.NET Core host (not just a Worker Service) so it can expose HTTP endpoints
2. Define a typed hub interface `IReadingClient` with one method: `ReceiveReading(ProcessedReading reading)`
3. Create `WellHub : Hub<IReadingClient>` — for now it does nothing special, clients just connect
4. Add a `SignalRBroadcastHandler` implementing `INotificationHandler<ReadingProcessed>` — it uses `IHubContext<WellHub, IReadingClient>` to call `Clients.All.ReceiveReading(reading)`
5. Register SignalR in DI and map the hub endpoint at `/hubs/well`

Test with the SignalR client in a browser console or a simple HTML file — confirm readings arrive every 250 ms.

---

### Check — Phase 5

1. Why can't you inject `WellHub` directly into your MediatR handler to send messages? Why must you use `IHubContext<T>` instead?
2. What is the difference between `Clients.All`, `Clients.Group("groupName")`, and `Clients.Caller`?
3. When a client disconnects and reconnects, does it automatically rejoin groups it was in? What does this mean for your application design?
4. What is a typed hub? What problem does it solve compared to using string-based method names?
5. Your hub is at `/hubs/well`. A client connects and immediately starts receiving data. If the client's network drops for 10 seconds and reconnects, what does it see? What data was missed? How would you handle this?
6. SignalR supports three transports: WebSockets, SSE, Long Polling. Which one is used by default? Why would you want to force WebSockets-only in a LAN-only wellsite environment?
7. What happens to hub connections when the server restarts? What is the client's responsibility?

---

---

# Phase 6 — MongoDB with .NET
**Duration:** Week 6 (3 days backend + 2 days consolidation)  
**Goal:** Persist readings using the MongoDB .NET driver and time-series collections. Apply repository pattern.

---

### Learn

**You already know MongoDB. This phase is about the .NET driver and time-series specifics.**

Key concepts:
- **MongoDB.Driver NuGet** — the official .NET driver. `MongoClient`, `IMongoDatabase`, `IMongoCollection<T>`. Thread-safe; one `MongoClient` instance per application (singleton).
- **BSON serialization** — how C# records/classes map to BSON documents. `[BsonId]`, `[BsonElement]`, `BsonSerializer`. Understand what happens with `DateTime` (always store UTC), `ObjectId`, and enum serialization.
- **Time-series collections** — MongoDB 5+ feature. You declare a collection as time-series with a `timeField` and an optional `metaField`. The engine optimizes storage and queries for time-ordered data. This is exactly what `SensorReading` data is.
- **Repository pattern** — define `IReadingRepository` in `Domain`, implement `MongoReadingRepository` in `Infrastructure`. The application layer never imports `MongoDB.Driver`.
- **Async driver usage** — all driver methods have async variants. Use `InsertOneAsync`, `FindAsync`, `Builders<T>.Filter`. Avoid synchronous driver calls in async code.
- **`IMongoCollection.InsertManyAsync`** — for high-frequency data you batch writes, not insert one at a time. Understand why and how to buffer.

**Free Resources:**
- [MongoDB C# Driver docs](https://www.mongodb.com/docs/drivers/csharp/current/)
- [MongoDB — Time Series Collections](https://www.mongodb.com/docs/manual/core/timeseries-collections/)
- [MongoDB University — M220N (.NET)](https://learn.mongodb.com/courses/m220n) — free official course

---

### Build — Phase 6

Add persistence to the pipeline:

1. Create a `MongoReadingRepository` in `Infrastructure` implementing `IReadingRepository` from `Domain`
2. The repository has one method: `SaveReadingAsync(ProcessedReading reading, CancellationToken ct)`
3. Create the MongoDB collection as a **time-series collection** using the .NET driver (time field: `Timestamp`, meta field: `WellId`)
4. Add a `PersistenceHandler` implementing `INotificationHandler<ReadingProcessed>` — it calls the repository
5. Register `MongoClient` as singleton in DI; register the repository

**Optimization task:** at 4 Hz, you produce 14,400 readings per hour. Inserting one document per reading is inefficient. Modify the persistence handler to buffer 20 readings and flush with `InsertManyAsync`. Think about thread safety and what happens if the service shuts down with 15 buffered readings.

---

### Check — Phase 6

1. Why should `MongoClient` be registered as a singleton? What happens if you register it as transient?
2. What is a MongoDB time-series collection? What optimization does it provide over a regular collection for sensor data?
3. The `PersistenceHandler` buffers 20 readings before flushing. If the service shuts down with 15 readings in the buffer, they are lost. How would you handle this without adding complex persistence middleware?
4. MongoDB stores `DateTime` without timezone info internally. If your simulator runs in Mexico City (UTC-6) and the data is read in India (UTC+5:30), what could go wrong if you store local time instead of UTC?
5. Why does the `Domain` project define `IReadingRepository` as an interface, and why does only `Infrastructure` implement it? What would break if `Application` referenced `MongoDB.Driver` directly?
6. What is the difference between `Find` with a filter and an aggregation pipeline for querying time-series data? When would you use each?
7. A new team member asks: "Can't we just use Entity Framework Core instead of the MongoDB driver?" What is your answer?

---

---

# Phase 7 — MQTT + Angular Signals
**Duration:** Week 7  
**Goal:** Replace the fake sensor generator with a real MQTT source. Build the Angular dashboard using Signals and RxJS where each is appropriate.

---

### Learn

**Part A — MQTT with MQTTnet (.NET):**

MQTT is the industry-standard lightweight protocol for sensor data in IoT and industrial systems. The wellsite edge server subscribes to sensor topics and receives readings published by field equipment.

Key concepts:
- **Broker/client model** — there is a central broker (Mosquitto, in your case running locally). Publishers send to topics; subscribers receive from topics. The edge server is a subscriber.
- **Topics** — hierarchical strings like `well/M-001/sensor/pressure`. You can subscribe with wildcards: `well/M-001/sensor/#` gets all sensor types for that well.
- **QoS levels** — 0 (fire and forget), 1 (at least once), 2 (exactly once). For 4 Hz sensor data, QoS 0 is typically correct. Understand why.
- **MQTTnet** — the .NET MQTT library. `MqttFactory`, `IMqttClient`, `ConnectAsync`, `SubscribeAsync`, `ApplicationMessageReceivedAsync`.
- **Reconnection** — MQTT connections drop. The client must detect disconnection and reconnect with backoff. MQTTnet has built-in retry support.
- **Payload deserialization** — MQTT payloads are raw bytes. You define the format (JSON, MessagePack, or binary). For WellEdge use JSON.

**Part B — Angular Signals and RxJS:**

These two reactive systems coexist in Angular 17+. They solve different problems.

| Use case | Right tool | Why |
|----------|-----------|-----|
| Component state | `signal()` | Synchronous, fine-grained, no subscription management |
| Computed values | `computed()` | Derived from signals, lazy, cached until dependency changes |
| Async streams (SignalR, HTTP) | RxJS `Observable` | Designed for async, time-based, or infinite streams |
| Bridging async to template | `toSignal()` | Converts Observable to Signal, handles subscription lifecycle |
| Side effects from signal changes | `effect()` | Runs when a signal changes, like a reactive `ngOnInit` |

**Signals in practice for WellEdge:**
- `currentReading = signal<ProcessedReading | null>(null)` — the latest reading
- `isConnected = signal(false)` — connection state
- `maxPressure = computed(() => currentReading()?.pressurePsi ?? 0)` — derived value
- SignalR stream arrives as Observable → `toSignal(signalRStream$)` → template reads it as a signal

**Free Resources:**
- [MQTTnet GitHub](https://github.com/dotnet/MQTTnet) — README and samples
- [Mosquitto download](https://mosquitto.org/download/) — free local MQTT broker
- [Angular Docs — Signals](https://angular.dev/guide/signals)
- [Angular Docs — toSignal](https://angular.dev/guide/rxjs-interop)
- Deborah Kurata — "Angular Signals" (YouTube)

---

### Build — Phase 7A (MQTT)

Replace `SensorSimulatorWorker` with an MQTT subscriber:

1. Install Mosquitto locally. Write a small script (or a second console project) that publishes fake readings to `well/M-001/sensor/data` as JSON at 4 Hz
2. Create `MqttIngestionWorker` implementing `BackgroundService` using MQTTnet
3. On message received, deserialize the JSON payload into a `SensorReading` and write it to the existing `Channel<SensorReading>`
4. Handle reconnection: if the broker is unreachable on startup, retry every 5 seconds. If connection drops during operation, reconnect automatically.
5. Log connection state changes with Serilog (Phase 8 preview — add Serilog now)

The rest of the pipeline (Channels → MediatR → SignalR → MongoDB) remains unchanged. Only the data source changes.

### Build — Phase 7B (Angular)

Create a minimal Angular app `welldash` that connects to the SignalR hub:

1. Use `@microsoft/signalr` npm package
2. Create a `WellHubService` that manages the SignalR connection lifecycle and exposes the stream as an `Observable<ProcessedReading>`
3. In your dashboard component:
   - Convert the Observable to a signal with `toSignal()`
   - Use `computed()` to derive: `isPressureWarning = computed(() => (this.reading()?.pressurePsi ?? 0) > 4500)`
   - Use `effect()` to log to console whenever the reading changes (diagnostic only)
4. Template: display current pressure, flow rate, temperature, and the warning state. No charting library needed — just numeric values that update live.

---

### Check — Phase 7

1. What is the difference between MQTT QoS 0, 1, and 2? Why is QoS 0 usually the right choice for high-frequency sensor data at the wellsite?
2. If the MQTT broker goes offline for 30 seconds and comes back, what happens to the readings published during that window (QoS 0)? Is that acceptable for this use case?
3. What is the difference between `signal()` and `computed()`? Can a `computed()` signal be set directly?
4. Why use `toSignal()` instead of subscribing to the Observable in `ngOnInit`? What problem does it solve?
5. You have a SignalR `Observable<ProcessedReading>` emitting 4 times per second. If the template re-renders on every emission, is this a problem in Angular with Signals? Why or why not compared to using `ngOnChanges`?
6. A teammate says "just use RxJS for everything, Signals are new and risky." When would that be the wrong choice? Give a concrete example from the WellEdge dashboard.
7. The Angular `effect()` in your dashboard logs every reading. In what zone does this run? What are the rules about what you can and cannot do inside `effect()`?

---

---

# Phase 8 — Reliability & Production Patterns
**Duration:** Week 8  
**Goal:** Make WellEdge behave like production software. Logging, graceful shutdown, reconnection, and self-diagnostics.

---

### Learn

**This is what separates a prototype from edge software that runs unattended at a wellsite:**

- **Serilog** — structured logging library. Every log event is a structured object, not a string. Sinks write to console, file, Seq (local log UI), or any target. Learn: `Log.Information("Reading {WellId} at {Pressure}", ...)` vs `$"Reading {wellId} at {pressure}"` — understand why structured logging wins.
- **`ILogger<T>`** — the .NET abstraction. Serilog implements it. Your code always depends on `ILogger<T>`, never on Serilog directly.
- **Graceful shutdown** — when Windows sends a stop signal to your service, you have a window (default 5 seconds) to finish in-flight work, flush buffers, close connections, and exit cleanly. Understand the `IHostApplicationLifetime` hooks: `ApplicationStarted`, `ApplicationStopping`, `ApplicationStopped`.
- **Health checks** — `IHealthCheck` lets you expose `/health` endpoints. In WellEdge: is the MQTT connection up? Is MongoDB reachable? Is the channel drained or full? Monitoring systems poll this endpoint.
- **Automatic reconnection** — you already added MQTT reconnection. Apply the same pattern to SignalR server-side: if a client disconnects, the hub doesn't need to do anything. But the Angular client must implement reconnection with exponential backoff and rejoin groups on reconnect.
- **Self-diagnostics on startup** — the profile explicitly mentions this. On startup, verify: MongoDB is reachable, MQTT broker is reachable, configuration is valid. Log the result of each check before starting the pipeline. Fail fast with a clear error if a required dependency is unavailable.

**Free Resources:**
- [Serilog docs](https://serilog.net/) — official, excellent
- [Microsoft Docs — Health checks in ASP.NET Core](https://learn.microsoft.com/en-us/aspnet/core/host-and-deploy/health-checks)
- [Microsoft Docs — IHostApplicationLifetime](https://learn.microsoft.com/en-us/dotnet/core/extensions/generic-host#ihostapplicationlifetime)

---

### Build — Phase 8

1. **Serilog** — replace all `Console.WriteLine` and `ILogger` console-only output with Serilog. Add a rolling file sink to `logs/pozo-.txt`. Log every pipeline stage transition as a structured event with `WellId` and `Timestamp` as structured properties.

2. **Startup diagnostics** — before `StartAsync` returns on any worker, verify its dependency is available. MongoDB: attempt a ping. MQTT: attempt connection with 3 retries. If a critical dependency fails all retries, log a fatal error and call `IHostApplicationLifetime.StopApplication()`.

3. **Graceful shutdown** — in the processing worker, when `stoppingToken` is cancelled, drain the channel completely before returning (within a 3-second timeout). Log how many readings were in the buffer at shutdown.

4. **Health check endpoint** — add three health checks: `MongoHealthCheck`, `MqttHealthCheck`, `ChannelHealthCheck` (reports healthy if channel fill is below 80%). Expose at `GET /health`.

5. **Angular reconnection** — in `WellHubService`, implement automatic reconnection with exponential backoff (1s, 2s, 4s, 8s, max 30s). Expose `isConnected` as an Observable. Show a connection status indicator in the dashboard.

---

### Check — Phase 8

1. What is structured logging? Why is `Log.Information("Pressure {Psi}", value)` better than `Log.Information($"Pressure {value}")` in a production system?
2. When the Windows Service stop signal is received, what is the sequence of events in your Worker Service? What is the default shutdown timeout and how do you change it?
3. The channel has 15 buffered readings when shutdown begins. Your drain loop has a 3-second timeout. What happens if draining takes longer than 3 seconds? Who enforces the timeout?
4. What is the difference between `Healthy`, `Degraded`, and `Unhealthy` in .NET health checks? Give a concrete example of each from WellEdge.
5. Your `ChannelHealthCheck` reports `Degraded` when the channel is 80% full. What does this tell an operator? What action should they take?
6. In the Angular client, you implement exponential backoff for SignalR reconnection. If the edge server restarts and the client reconnects after 8 seconds, what state is the client missing? How do you handle the gap?
7. A wellsite engineer reports that the service "just stopped" overnight with no log messages after 02:00. What are the first three things you check, and what logs would you look for?

---

---

# Appendix A — RxJS vs Signals Quick Reference

Use this as a decision guide when building Angular features:

| Scenario | Use |
|----------|-----|
| Local UI state (selected tab, open/closed panel) | `signal()` |
| Value derived from other state | `computed()` |
| HTTP request result | RxJS + `toSignal()` |
| SignalR / WebSocket stream | RxJS + `toSignal()` |
| Form `valueChanges` stream | RxJS |
| Debounced search input | RxJS (`debounceTime`) |
| React to a signal change (side effect) | `effect()` |
| Combine multiple async sources | RxJS (`combineLatest`, `merge`) |
| Template binding that updates frequently | `signal()` or `toSignal()` |

**The key rule:** if it involves time, async, or combining multiple streams — reach for RxJS. If it is synchronous state that the template reads — reach for Signals.

---

# Appendix B — C# ↔ TypeScript Cheat Sheet

| TypeScript | C# |
|-----------|-----|
| `interface` | `interface` (but also used for DI contracts) |
| `type Foo = { ... }` | `record Foo(...)` or `class Foo` |
| `Promise<T>` | `Task<T>` |
| `async/await` | `async/await` (same concept, different scheduler) |
| `readonly` | `init` setter or `record` |
| Generic `<T>` | Generic `<T>` |
| `?.` optional chain | `?.` (same) |
| `??` nullish coalesce | `??` (same) |
| `Array<T>` | `List<T>`, `T[]`, `IEnumerable<T>` |
| `Map<K,V>` | `Dictionary<TKey, TValue>` |
| `Symbol` | No direct equivalent |
| Module imports | `using` namespaces |
| `console.log` | `Console.WriteLine` / `logger.LogInformation` |
| `try/catch/finally` | `try/catch/finally` (same) |
| `enum` | `enum` (value type in C#, different behavior) |

---

# Appendix C — Free Tools Setup Checklist

- [ ] [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)
- [ ] [Visual Studio 2022 Community](https://visualstudio.microsoft.com/vs/community/) — free, for backend
- [ ] [VS Code](https://code.visualstudio.com/) — frontend + GitHub Copilot
- [ ] [MongoDB Community Server](https://www.mongodb.com/try/download/community) — local instance
- [ ] [MongoDB Compass](https://www.mongodb.com/products/tools/compass) — free GUI
- [ ] [Mosquitto MQTT Broker](https://mosquitto.org/download/) — local broker for Phase 7
- [ ] [MQTT Explorer](https://mqtt-explorer.com/) — free GUI to inspect MQTT topics
- [ ] [Node.js LTS](https://nodejs.org/) — for Angular
- [ ] [Angular CLI](https://angular.dev/tools/cli) — `npm install -g @angular/cli`

---

*Last updated: July 2026 | Target role: Full-Stack Developer — Real-Time Well Intervention System*
