using System;
using System.Reflection;
using NUnit.Framework;
using Unity.Notifications;


namespace Unity.Notifications.Tests
{
    internal class InEditorNotificationsTests
    {
        [Test]
        public void CanGetAllPropertiesOnDefaultCreatedNotification()
        {
            foreach (var prop in typeof(Notification).GetProperties())
                try
                {
                    Notification n = default;
                    prop.GetValue(n);
                }
                catch (Exception e)
                {
                    Assert.Fail($"Failed to get property {prop.Name}, {e.Message}");
                }
        }

        [Test]
        public void CanSetAllPropertiesOnDefaultCreatedNotification()
        {
            foreach (var prop in typeof(Notification).GetProperties())
                SetProperty(prop);
        }

        void SetProperty(PropertyInfo prop)
        {
            Notification n = default;
            object boxed = n;  // need to box so we access same thing twice, not a copy
            var val = ValueForProperty(prop);

            try
            {
                // check if just created property can be set and read without exception
                prop.SetValue(boxed, val);
            }
            catch (Exception e)
            {
                Assert.Fail($"Failed to set property {prop.Name}, {e.Message}");
            }

            if (prop.Name != "IsGroupSummary") // android only property, not actually writable elsewhere, so skip
                Assert.AreEqual(val, prop.GetValue(boxed), $"Unexpected value for {prop.Name}");
        }

        Object ValueForProperty(PropertyInfo prop)
        {
            var type = prop.PropertyType;
            if (type == typeof(int) || type == typeof(int?))
                return 1;
            if (type == typeof(bool))
                return true;
            if (type == typeof(string))
                return "x";

            throw new Exception($"Unsupported type {prop.PropertyType}");
        }
    }
}
