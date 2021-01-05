using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MyPublisherSubscriber
{
    public class PublisherSubscriberSingleThreadedMultiRead<T> : IPublisher<T>, ISubscriberMultiResult<T>
    {
        private interface IElement
        {
            
        }

        private record StopIt : IElement
        {
            
        }
        
        public readonly struct Element : IElement
        {
            public Element(T item)
            {
                Item = item;
                Published = DateTime.UtcNow;
            }
            public T Item { get; }
            public DateTime Published { get; }
        }

        private readonly Queue<IElement> _queue = new ();

        private readonly object _lockObject = new ();


        private TaskCompletionSource<IReadOnlyList<Element>> _awaitingTask = new();
        private int _maxElements = 0;

        private bool _stopped;

        public void Publish(T item)
        {

            if (_stopped)
                throw new ExecutionIsStopped();
            
            var nextElement = new Element(item);
         
            lock (_lockObject)
            {
                _queue.Enqueue(nextElement);

                if (_awaitingTask != null)
                {
                    var result = _awaitingTask;
                    _awaitingTask = null;
                    try
                    {
                        result.SetResult(GetElements(_maxElements));
                    }
                    catch (Exception e)
                    {
                        result.SetException(e);
                    }

                }
            }
        }
        
        private readonly List<Action<IReadOnlyList<Element>, Exception>> _statisticSubscribers = new ();

        public PublisherSubscriberSingleThreadedMultiRead<T> SubscribeToExecutionStatistic(Action<IReadOnlyList<Element>, Exception> executionStatistic)
        {
            _statisticSubscribers.Add(executionStatistic);
            return this;
        }


        private void InvokeStatistics(IReadOnlyList<Element> items, Exception ex)
        {
            if (_statisticSubscribers.Count == 0)
            {
                if (ex != null)
                    Console.WriteLine(
                        "Exception during the subscriber execution. . " + ex);
                return;
            }
            
            foreach (var subscriber in _statisticSubscribers)
                subscriber(items, ex);
        }

        private List<Element> GetElements(int maxElements)
        {

            if (_queue.Count == 0)
                return null;

            var result = new List<Element>();

            while (_queue.Count > 0 && maxElements > 0)
            {
                var item = _queue.Dequeue();
                if (item is StopIt)
                    throw new ExecutionIsStopped();

                if (item is Element element)
                    result.Add(element);
            }

            return result;
        }


        private async ValueTask Execute(Func<IReadOnlyList<T>, ValueTask> executeCallback, IReadOnlyList<Element> elements)
        {
            try
            {
                await executeCallback(elements.Select(itm => itm.Item).ToList());
                InvokeStatistics(elements, null);
            }
            catch (Exception e)
            {
                InvokeStatistics(elements, e);
            }
        }


        private async Task ExecuteAsync(Func<IReadOnlyList<T>, ValueTask> executeCallback)
        {
            _awaitingTask = new TaskCompletionSource<IReadOnlyList<Element>>();
            var elements = await _awaitingTask.Task;
            await Execute(executeCallback, elements);
        }

        public ValueTask GetNextElementAsync(int maxElements, Func<IReadOnlyList<T>, ValueTask> executeCallback)
        {

            List<Element> elements;
            
            lock (_lockObject)
            {
                if (_awaitingTask != null)
                    throw new Exception("Reading task is already on awaiting state");

                elements = GetElements(maxElements);

                if (elements == null)
                {
                    return new ValueTask(ExecuteAsync(executeCallback));
                }
            }

            return Execute(executeCallback, elements);
        }

        public void Stop()
        {
            lock (_lockObject)
            {
                _stopped = true;
                _queue.Enqueue(new StopIt());
            }
        }

    }
    
}