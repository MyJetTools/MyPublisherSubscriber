using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MyPublisherSubscriber
{
    public class PublisherSubscriberSingleThreaded<T> : IPublisher<T>, ISubscriber<T>
    {
        public struct NextElement
        {
            public NextElement(TaskCompletionSource<T> task)
            {
                Published = DateTime.UtcNow;
                Task = task;
            }
            
            public TaskCompletionSource<T> Task { get; }
            public DateTime Published { get; }

            public static NextElement Create()
            {
                return new NextElement(new TaskCompletionSource<T>());
            }
        }
        
        
        private readonly LinkedList<NextElement> _queue = new ();

        private TaskCompletionSource<T> GetNextTaskToPublish()
        {
            lock (_queue)
            {
                var result = _queue.Last;
                _queue.AddLast(NextElement.Create());
                return result.Value.Task;
            }
        }
        
        public PublisherSubscriberSingleThreaded()
        {
            _queue.AddLast(NextElement.Create());
        }
        
        
        public void Publish(T item)
        {
            var nextTask = GetNextTaskToPublish();
            nextTask.SetResult(item);
        }

        private NextElement GetNextTaskToSubscribe()
        {
            lock (_queue)
                return _queue.First.Value;
        }

        private void RemoveAwaitedTask()
        {
            lock (_queue)
                _queue.RemoveFirst();
        }



        private readonly List<Action<ExecutionStatistic<T>>> _statisticSubscribers = new List<Action<ExecutionStatistic<T>>>();

        public PublisherSubscriberSingleThreaded<T> SubscribeToExecutionStatistic(Action<ExecutionStatistic<T>> executionStatistic)
        {
            _statisticSubscribers.Add(executionStatistic);
            return this;
        }


        public void PublishStatistics(in NextElement nextElement, DateTime startExecution, Exception ex = null)
        {
            if (_statisticSubscribers.Count == 0)
            {
                if (ex != null)
                {
                    Console.WriteLine(
                        $"Exception during the subscriber execution. Publish Time:{nextElement.Published}. " +
                        $"Start execution time: {startExecution}. " +
                        $"Delay between publish and execution: {nextElement.Published - startExecution}");
                }
                return;
            }

            var element =
                new ExecutionStatistic<T>(nextElement.Task.Task.Result, ex, nextElement.Published, startExecution);

            foreach (var subscriber in _statisticSubscribers)
            {
                subscriber(element);
            }
        }

        public async ValueTask GetNextElementAsync(Func<T, ValueTask> callbackAsync)
        {
            var nextTask = GetNextTaskToSubscribe();

            var result = await nextTask.Task.Task;
            
            RemoveAwaitedTask();
            var executionStart = DateTime.UtcNow;
            try
            {
                await callbackAsync(result);
                PublishStatistics(nextTask, executionStart);
            }
            catch (Exception e)
            {
                PublishStatistics(nextTask, executionStart, e);
            }
        }
    }
}