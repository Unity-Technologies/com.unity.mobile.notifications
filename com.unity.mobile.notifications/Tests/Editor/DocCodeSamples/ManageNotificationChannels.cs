var group = new AndroidNotificationChannelGroup()
{
    Id = "Main",
    Name = "Main notifications",
};
AndroidNotificationCenter.RegisterNotificationChannelGroup(group);
var channel = new AndroidNotificationChannel()
{
    Id = "channel_id",
    Name = "Default Channel",
    Importance = Importance.Default,
    Description = "Generic notifications",
    Group = "Main",  // must be same as Id of previously registered group
};
AndroidNotificationCenter.RegisterNotificationChannel(channel);