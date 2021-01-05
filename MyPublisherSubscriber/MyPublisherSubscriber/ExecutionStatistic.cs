using System;

namespace MyPublisherSubscriber
{
    public struct ExecutionStatistic<T>
    {
        public ExecutionStatistic(T item, Exception ex, DateTime created, DateTime startExecution)
        {
            Item = item;
            Published = created;
            StartExecution = startExecution;
            Exception = ex;
        }
            
        public T Item { get; }
            
        public DateTime Published { get; }
        public DateTime StartExecution { get; }
        public Exception Exception { get; }
            
    }
    

}