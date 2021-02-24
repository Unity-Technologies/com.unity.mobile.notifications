using System;
using NUnit.Framework;
using Unity.Notifications.iOS;

namespace Unity.Notifications.Tests
{
    public class ApiTestsiOS
    {
        [Test]
        public void TimeIntervalTrigger_MeasuredInSeconds()
        {
            var trigger = new iOSNotificationTimeIntervalTrigger();
            var interval = TimeSpan.FromMinutes(1);
            trigger.TimeInterval = interval;
            Assert.AreEqual(60, trigger.timeInterval);
            var outInterval = trigger.TimeInterval;
            Assert.AreEqual(interval, outInterval);

            trigger.TimeInterval = new TimeSpan(0, 0, 1, 5, 50);
            // milliseconds should get discarded
            Assert.AreEqual(65, trigger.timeInterval);
        }

        [Test]
        public void TimeIntervalTrigger_RejectsLessThanSecond()
        {
            CheckTimeSpanIsRected(new TimeSpan(0, 0, 0, 0, 100));
            CheckTimeSpanIsRected(TimeSpan.FromSeconds(-3));
        }

        void CheckTimeSpanIsRected(TimeSpan interval)
        {
            try
            {
                var trigger = new iOSNotificationTimeIntervalTrigger();
                trigger.TimeInterval = interval;
                Assert.Fail("Exception expected");
            }
            catch (ArgumentException)
            {
                // expected
            }
        }
    }
}
