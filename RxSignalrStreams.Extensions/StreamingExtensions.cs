using System;
using System.Threading;
using System.Threading.Channels;

namespace RxSignalrStreams.Extensions
{
    public static class StreamingExtensions
    {
        /// <summary>
        /// Shows adapting an observable to a ChannelReader without back pressure.
        /// 
        /// If the connection is slower than the producer,
        /// The stream will only keep the latest value produced, it will drop other items if any.
        /// </summary>
        /// <typeparam name="T">Type of data to stream.</typeparam>
        /// <param name="observable">Observable of values to listen to.</param>
        /// <param name="connectionAborted">Cancellation token received from <c>Context.ConnectionAborted</c>.</param>
        /// <returns>Returns a <see cref="ChannelReader{T}"/> to use as a SignalR stream.</returns>
        public static ChannelReader<T> ToNewestValueStream<T>(
            this IObservable<T> observable,
            CancellationToken connectionAborted
        )
        {
            var channel = Channel.CreateBounded<T>(
                new BoundedChannelOptions(1)
                {
                    FullMode = BoundedChannelFullMode.DropOldest
                }
            );

            var disposable = observable.Subscribe(
                value => channel.Writer.TryWrite(value),
                error => channel.Writer.TryComplete(error),
                () => channel.Writer.TryComplete()
            );
            var abortRegistration = connectionAborted.Register(() => channel.Writer.TryComplete());

            // Complete the subscription on the reader completing
            channel.Reader.Completion.ContinueWith(task =>
            {
                disposable.Dispose();
                abortRegistration.Dispose();
            });

            return channel.Reader;
        }

        /// <summary>
        /// Shows adapting an observable to a ChannelReader without back pressure.
        /// 
        /// If the connection is slower than the producer, memory will start to increase and
        /// the stream will keep all the values produced in a buffer,
        /// in order to be used in the stream lately.
        /// </summary>
        /// <typeparam name="T">Type of data to stream</typeparam>
        /// <param name="observable">Observable of values to listen to.</param>
        /// <param name="connectionAborted">Cancellation token received from <c>Context.ConnectionAborted</c>.</param>
        /// <returns>Returns a <see cref="ChannelReader{T}"/> to use as a SignalR stream.</returns>
        public static ChannelReader<T> ToBufferedStream<T>(
            this IObservable<T> observable,
            CancellationToken connectionAborted
        )
        {
            var channel = Channel.CreateUnbounded<T>();

            var disposable = observable.Subscribe(
                value => channel.Writer.TryWrite(value),
                error => channel.Writer.TryComplete(error),
                () => channel.Writer.TryComplete()
            );
            var abortRegistration = connectionAborted.Register(() => channel.Writer.TryComplete());

            // Complete the subscription on the reader completing
            channel.Reader.Completion.ContinueWith(task =>
            {
                disposable.Dispose();
                abortRegistration.Dispose();
            });

            return channel.Reader;
        }
    }
}
