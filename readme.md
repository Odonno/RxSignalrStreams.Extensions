# Extensions for Reactive SignalR streaming

Since ASP.NET Core 2.1, we can now use SignalR streaming to push data to or receive data from client/server.

By design, only `ChannelReader<T>` class is supported to create a stream from the server to the client. For your information, since ASP.NET Core 3.0, the `IAsyncEnumerable<T>` is also supported.

If you desire to consume an `IObservable<T>` from the server, you must know that you can be exposed to errors due to backpressure, the fact that the consumer can be slower than the producer which can lead to an inconsistent system. To prevent this error, there is a way to change the streaming behavior by creating a `ChannelReader<T>` from an `IObservable<T>`. There are currently 2 possible scenario you can encounter: `ignore` or `buffer`.

### ToNewestValueStream

The `ToNewestValueStream` method is made for the `ignore` all previous values scenario. So, you will always send/receive the latest value as soon as possible even when the consumer is slower than the producer. Previous data can be skipped in favor of the latest one.

Use this scenario when you are in a *I don't care about not so fresh data* scenario.

```cs
public ChannelReader<int> RealtimeWeather()
{
    return _realtimeValuesService.Observe()
        .ToNewestValueStream(Context.ConnectionAborted);
}
```

### ToBufferedStream

The `ToBufferedStream` method is made for the `buffer` all previous values scenario. So, you will always send/receive the values from the producer, even when the consumer is slower than the producer. No data will be skipped but it can take a while before having the latest data.

*Be aware that it will create a buffer of all the entire data not delivered, which can drastically increase the memory footprint.*

Use this scenario when you are in a *I care about every single data that was sent* scenario.

```cs
public ChannelReader<int> RealtimeWeather()
{
    return _realtimeValuesService.Observe()
        .ToBufferedStream(Context.ConnectionAborted);
}
```