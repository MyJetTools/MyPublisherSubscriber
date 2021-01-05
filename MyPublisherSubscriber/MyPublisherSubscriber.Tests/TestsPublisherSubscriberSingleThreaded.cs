using System.Threading.Tasks;
using NUnit.Framework;

namespace MyPublisherSubscriber.Tests
{
    public class TestsPublisherSubscriberSingleThreaded
    {

        [Test]
        public async Task TestOnePubOneSub()
        {
            var pubSub = new PublisherSubscriberSingleThreaded<int>();
            
            pubSub.Publish(5);

            var result = 0;
            await pubSub.GetNextElementAsync(el =>
            {
                result = el;
                return new ValueTask();
            });

            Assert.AreEqual(5, result);
        }
        
        
        [Test]
        public async Task TestTwoPubOneTwoSub()
        {
            var pubSub = new PublisherSubscriberSingleThreaded<int>();
            
            pubSub.Publish(5);
            pubSub.Publish(6);

            var result1 = 0;
            var result2 = 0;            
            await pubSub.GetNextElementAsync(el =>
            {
                result1 = el;
                return new ValueTask();
            });
            
            await pubSub.GetNextElementAsync(el =>
            {
                result2 = el;
                return new ValueTask();
            });

            Assert.AreEqual(5, result1);
            Assert.AreEqual(6, result2);
        }
        
        
        [Test]
        public async Task TestTwoSubTwoPub()
        {
            var pubSub = new PublisherSubscriberSingleThreaded<int>();

            var result1 = 0;
            var result2 = 0;
            var task1 = pubSub.GetNextElementAsync(el =>
            {
                result1 = el;
                return new ValueTask();
            });


            pubSub.Publish(5);
            pubSub.Publish(6);

            await task1;
                        
            await pubSub.GetNextElementAsync(el =>
            {
                result2 = el;
                return new ValueTask();
            });
            
            Assert.AreEqual(5, result1);
            Assert.AreEqual(6, result2);
        }
        
        [Test]
        public async Task TestOneSubOnePub()
        {
            var pubSub = new PublisherSubscriberSingleThreaded<int>();

            var result = 0;
            var task = pubSub.GetNextElementAsync(el =>
            {
                result = el;
                return new ValueTask();
            });

            pubSub.Publish(5);

            await task;
            
            Assert.AreEqual(5, result);
        }
        
    }
}