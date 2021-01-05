using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace MyPublisherSubscriber.Tests
{

    public class Instrument
    {
        public string Id { get; set; }
        public int Digits { get; set; }
    }
    


    public static class MyClassStatic
    {

        private static readonly Dictionary<string, Instrument> _items = new ();
        private static List<Instrument> _cacheAsList = new ();

        private static readonly ReaderWriterLockSlim _lock = new ();

        public static void Add(Instrument i)
        {
            _lock.EnterWriteLock();
            try
            {
                _items.Add(i.Id, i);
                _cacheAsList = _items.Values.ToList();
            }
            finally
            {
                _lock.ExitWriteLock();
            }
        }


        public static IReadOnlyList<Instrument> GetAll()
        {
            _lock.EnterReadLock();
            try
            {
                return _cacheAsList;
            }
            finally
            {
                _lock.ExitReadLock();
            }
            
        }

        public static void Test()
        {
            var a = GetAll();

            var count = a.Count;
            var count2 = a.Count;
        }


      
        
    }
}