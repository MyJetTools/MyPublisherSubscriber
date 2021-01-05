using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MyPublisherSubscriber
{
    public class PublisherSubscriberSingleThreadedMultiRead<T> : IPublisher<T>, ISubscriberMultiResult<T>
    {
        public readonly struct Element
        {
            public Element(T item)
            {
                Item = item;
                Published = DateTime.UtcNow;
            }
            public T Item { get; }
            public DateTime Published { get; }
        }

        private readonly Queue<Element> _queue = new ();

        private readonly object _lockObject = new ();


        private TaskCompletionSource<IReadOnlyList<Element>> _awaitingTask = new();
        private int _maxElements = 0;

        public void Publish(T item)
        {
            var nextElement = new Element(item);
         
            lock (_lockObject)
            {
                _queue.Enqueue(nextElement);

                if (_awaitingTask != null)
                {
                    var result = _awaitingTask;
                    _awaitingTask = null;
                    result.SetResult(GetElements(_maxElements));
                }
            }
        }
        
        private readonly List<Action<IReadOnlyList<Element>, Exception>> _statisticSubscribers = new ();

        public PublisherSubscriberSingleThreadedMultiRead<T> SubscribeToExecutionStatistic(Action<IReadOnlyList<Element>, Exception> executionStatistic)
        {
            _statisticSubscribers.Add(executionStatistic);
            return this;
        }


        private void InvokeStatistics(IReadOnlyList<Element> items, Exception e)
        {
            if (_statisticSubscribers.Count == 0)
                return;
            
            foreach (var subscriber in _statisticSubscribers)
                subscriber(items, e);
            
        }

        private List<Element> GetElements(int maxElements)
        {

            if (_queue.Count == 0)
                return null;

            var result = new List<Element>();

            while (_queue.Count > 0 && maxElements > 0)
            {
                var item = _queue.Dequeue();
                result.Add(item);
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

    }
    
}