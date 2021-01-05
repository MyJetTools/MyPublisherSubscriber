using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MyPublisherSubscriber
{
    public interface IPublisher<in T>
    {
        void Publish(T item);
    }

    public interface ISubscriber<out T>
    {
        ValueTask GetNextElementAsync(Func<T, ValueTask> callbackAsync);
    }
    
    public interface ISubscriberMultiResult<out T>
    {
        ValueTask GetNextElementAsync(int maxElements, Func<IReadOnlyList<T>, ValueTask> executeCallback);
    }
    
    
    public class ExecutionIsStopped : Exception{
    
    }
}