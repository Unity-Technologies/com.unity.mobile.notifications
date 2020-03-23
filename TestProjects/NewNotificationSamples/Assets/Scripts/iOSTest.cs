using System;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using UnityEngine;
using UnityEngine.UI;

#if PLATFORM_IOS
using Unity.Notifications.iOS;
#endif

public class iOSTest : MonoBehaviour
{

public List<iOSNotificationTemplateTimeTrigger> iOSNotificationsTimeTriggered;
public List<iOSNotificationTemplateCalendarTrigger> iOSNotificationsCalendarTriggered;
public List<iOSNotificationTemplateLocationTrigger> iOSNotificationsLocationTriggered;

#if PLATFORM_IOS

    private GameObjectReferences gameObjectReferences;
    private Dictionary<string, OrderedDictionary> groups;
    private Logger LOGGER;

    void Awake()
    {
        gameObjectReferences = gameObject.GetComponent<GameObjectReferences>();
        LOGGER = new Logger(gameObjectReferences.logsText);
    }

    void OnEnable()
    {
        iOSNotificationCenter.OnNotificationReceived += OnNotificationReceivedHandler;
        iOSNotificationCenter.OnRemoteNotificationReceived += OnRemoteNotificationReceivedHandler;
        Input.location.Start();
    }

    void OnDisable()
    {
        iOSNotificationCenter.OnNotificationReceived -= OnNotificationReceivedHandler;
        iOSNotificationCenter.OnRemoteNotificationReceived -= OnRemoteNotificationReceivedHandler;
        Input.location.Stop();
    }

    void OnNotificationReceivedHandler(iOSNotification notification)
    {
        // Update badge with the number of delivered notifications
        // This probably needs a separate method - sending a notification just to update the badge is weird
        if (notification.Data != "IGNORE")
        {
            LOGGER
                .Orange($"[{DateTime.Now.ToString("HH:mm:ss.ffffff")}] Received a local notification")
                .Orange($"Setting BADGE to {iOSNotificationCenter.GetDeliveredNotifications().Length + 1}", 1)
                .Properties(notification, 1);
            iOSNotificationCenter.ApplicationBadge = iOSNotificationCenter.GetDeliveredNotifications().Length + 1;

            LOGGER
                .Separator()
                .Orange($"Rescheduling local notification to be sent in 15 seconds", 1);
            notification.Trigger = new iOSNotificationTimeIntervalTrigger(){ TimeInterval = new TimeSpan(0, 0, 15) };
            iOSNotificationCenter.ScheduleNotification(notification);
        }
    }
    
    void OnRemoteNotificationReceivedHandler(iOSNotification notification)
    {
        Debug.Log(notification.Title);
        // iOSNotificationCenter.ApplicationBadge = iOSNotificationCenter.GetDeliveredNotifications().Length + 1;
        /* LOGGER
            .Orange($"[{DateTime.Now.ToString("HH:mm:ss.ffffff")}] Received a remote notification")
            .Orange($"Setting BADGE to {iOSNotificationCenter.GetDeliveredNotifications().Length + 1}", 1)
            .Properties(notification, 1);

        LOGGER
            .Separator()
            .Orange($"Rescheduling remote notification to be sent in 10 seconds", 1);
        iOSNotificationTimeIntervalTrigger newTrigger = new iOSNotificationTimeIntervalTrigger(){ TimeInterval = new TimeSpan(0, 0, 10) };
        Debug.Log(notification.Title);
        iOSNotification newNotification = new iOSNotification()
        {
            Title = "TITLE", // notification.Title,
            // Body =  notification.Body,
            // Subtitle = notification.Subtitle,
            ShowInForeground = true,
            ForegroundPresentationOption = PresentationOption.Sound | PresentationOption.Alert,
            // CategoryIdentifier = notification.CategoryIdentifier,
            // ThreadIdentifier = notification.ThreadIdentifier,
            Trigger = newTrigger,
            Data = "IGNORE"
        };
        iOSNotificationCenter.ScheduleNotification(newNotification);
        // iOSNotificationCenter.ScheduleNotification(notification); // Crashes */
    }

    void Start()
    {
        InstantiateAllTestButtons();
        ClearBadge();
        RemoveAllNotifications();
        LOGGER.Clear().White("Welcome!");
    }

    void OnApplicationPause(bool isPaused)
    {
        LOGGER.Gray($"[{DateTime.Now.ToString("HH:mm:ss.ffffff")}] Call {MethodBase.GetCurrentMethod().Name}");
        LOGGER.Gray($"isPaused = {isPaused}", 1);
        if (isPaused == false)
        {
            iOSNotification notification = iOSNotificationCenter.GetLastRespondedNotification();
            if (notification != null)
            {
                LOGGER.Green($"Notification found:", 1);
                if (notification.Data != "IGNORE")
                {
                    LOGGER.Orange($"[{DateTime.Now.ToString("HH:mm:ss.ffffff")}] Received notification");
                    LOGGER.Orange($"Setting BADGE to {iOSNotificationCenter.GetDeliveredNotifications().Length + 1}", 1);
                    Helpers.LogProperties(notification, LOGGER);
                    iOSNotificationCenter.ApplicationBadge = iOSNotificationCenter.GetDeliveredNotifications().Length + 1;
                }
            } else {
                LOGGER.Red("Notification not found!", 1);
            }
        }
    }

    private void InstantiateAllTestButtons()
    {
        groups = new Dictionary<string, OrderedDictionary>();

        groups["General"] = new OrderedDictionary();
        groups["General"]["Clear Log"] = new Action(() => { LOGGER.Clear(); });
        groups["General"]["Get Current Location"] = new Action(() => {
            StartCoroutine(GetCurrentLocation());
        });
        groups["General"]["Request Authorization"] = new Action(() => {
            StartCoroutine(RequestAuthorization());
        });
        groups["General"]["Request Authorization Sound + Badge"] = new Action(() => {
            StartCoroutine(RequestAuthorization(AuthorizationOption.Sound | AuthorizationOption.Badge));
        });
        groups["General"]["Show Notification Settings"] = new Action(() => {
            ShowNotificationSettings();
        });

        groups["Generated"] = new OrderedDictionary();
        foreach (iOSNotificationTemplateTimeTrigger template in iOSNotificationsTimeTriggered)
        {
            if (template == null) continue;
            groups["Generated"][$"{template.buttonName}"] = new Action(() => {
                ScheduleNotification(
                    new iOSNotification()
                    {
                        Identifier = template.identifier,
                        CategoryIdentifier = template.categoryIdentifier,
                        ThreadIdentifier = template.threadIdentifier,
                        Title = template.title,
                        Subtitle = template.subtitle,
                        Body = template.body,
                        ShowInForeground = template.showInForeground,
                        ForegroundPresentationOption = template.presentationOptions,
                        Badge = template.badge,
                        Data = template.data,
                        Trigger = new iOSNotificationTimeIntervalTrigger()
                        {
                            TimeInterval = TimeSpan.FromSeconds(template.timeTriggerInterval),
                            Repeats = template.repeats
                        }
                    }
                );
            });
        }
        foreach (iOSNotificationTemplateCalendarTrigger template in iOSNotificationsCalendarTriggered)
        {
            if (template == null) continue;
            groups["Generated"][$"{template.buttonName}"] = new Action(() => {
                ScheduleNotification(
                    new iOSNotification()
                    {
                        Identifier = template.identifier,
                        CategoryIdentifier = template.categoryIdentifier,
                        ThreadIdentifier = template.threadIdentifier,
                        Title = template.title,
                        Subtitle = template.subtitle,
                        Body = template.body,
                        ShowInForeground = template.showInForeground,
                        ForegroundPresentationOption = template.presentationOptions,
                        Badge = template.badge,
                        Data = template.data,
                        Trigger = new iOSNotificationCalendarTrigger()
                        {
                            Year = template.calendarTriggerYear,
                            Month = template.calendarTriggerMonth,
                            Day = template.calendarTriggerDay,
                            Hour = template.calendarTriggerHour,
                            Minute = template.calendarTriggerMinute,
                            Second = template.calendarTriggerSecond
                        }
                    }
                );
            });
        }
        foreach (iOSNotificationTemplateLocationTrigger template in iOSNotificationsLocationTriggered)
        {
            if (template == null) continue;
            groups["Generated"][$"{template.buttonName}"] = new Action(() => {
                ScheduleNotification(
                    new iOSNotification()
                    {
                        Identifier = template.identifier,
                        CategoryIdentifier = template.categoryIdentifier,
                        ThreadIdentifier = template.threadIdentifier,
                        Title = template.title,
                        Subtitle = template.subtitle,
                        Body = template.body,
                        ShowInForeground = template.showInForeground,
                        ForegroundPresentationOption = template.presentationOptions,
                        Badge = template.badge,
                        Data = template.data,
                        Trigger = new iOSNotificationLocationTrigger()
                        {
                            Center = new Vector2(template.locationTriggerCenterX, template.locationTriggerCenterY),
                            Radius = template.locationTriggerRadius,
                            NotifyOnEntry = template.locationTriggerNotifyOnEntry,
                            NotifyOnExit = template.locationTriggerNotifyOnExit
                        }
                    }
                );
            });
        }

        groups["Schedule"] = new OrderedDictionary();
        groups["Schedule"]["List Scheduled Notifications"] = new Action(() => { ListScheduledNotifications(); });
        groups["Schedule"]["List Delivered Notifications"] = new Action(() => { ListDeliveredNotifications(); });
        groups["Schedule"]["Clear the badge"] = new Action(() => { ClearBadge(); });
        groups["Schedule"]["Schedule Empty Notification"] = new Action(() => {
            ScheduleNotification(new iOSNotification());
        });
        groups["Schedule"]["Schedule Notification that sets the badge to 9000"] = new Action(() => {
            ScheduleNotification(
                new iOSNotification()
                {
                    Badge = 9000,
                    ShowInForeground = true,
                    ForegroundPresentationOption = PresentationOption.Badge,
                    Trigger = new iOSNotificationTimeIntervalTrigger(){ TimeInterval = new TimeSpan(0, 0, 1), Repeats = false },
                    Data = "IGNORE" // Do this to ignore auto incrementing of the badge on delivered notification
                }
            );
        });
        groups["Schedule"]["Schedule Notification in 0 seconds\n<color=#e74c3c>EXCEPTION</color>"] = new Action(() => {
            ScheduleNotification(
                new iOSNotification()
                {
                    Title = "Example Notification",
                    Subtitle = "You might want to know this!",
                    Body = $"Seconds: 0",
                    ShowInForeground = true,
                    ForegroundPresentationOption = PresentationOption.Sound | PresentationOption.Alert,
                    ThreadIdentifier = "default_thread",
                    Trigger = new iOSNotificationTimeIntervalTrigger(){ TimeInterval = new TimeSpan(0, 0, 0), Repeats = false },
                    Data = "Arbitrary Data"
                }
            );
        });
        groups["Schedule"]["Schedule Notification in 1 seconds\n(secondary_thread, sound)"] = new Action(() => {
            ScheduleNotification(
                new iOSNotification()
                {
                    Title = "Example Notification",
                    Subtitle = "You might want to know this!",
                    Body = $"Seconds: 1",
                    Badge = 10, // Should not increase counter, because it doesn't have required PresentationOption!!!
                    ShowInForeground = true,
                    ForegroundPresentationOption = PresentationOption.Sound,
                    ThreadIdentifier = "secondary_thread",
                    Trigger = new iOSNotificationTimeIntervalTrigger(){ TimeInterval = new TimeSpan(0, 0, 1), Repeats = false },
                    Data = "Arbitrary data, from a simple notification with sound"
                }
            );
        });
        groups["Schedule"]["Schedule Notification in 3 second\n(default_thread, sound, alert)"] = new Action(() => {
            ScheduleNotification(
                new iOSNotification()
                {
                    Title = "Example Notification: default_thread",
                    Subtitle = "You might want to know this!",
                    Body = $"Seconds: 3",
                    Badge = 3, // Should not increase counter, because it doesn't have required PresentationOption!!!
                    ShowInForeground = true,
                    ForegroundPresentationOption = PresentationOption.Sound | PresentationOption.Alert,
                    ThreadIdentifier = "default_thread",
                    Trigger = new iOSNotificationTimeIntervalTrigger(){ TimeInterval = new TimeSpan(0, 0, 3), Repeats = false },
                    Data = "Arbitrary data, from a simple notification with sound and alert"
                }
            );
        });
        groups["Schedule"]["Schedule Notification in 3 second\n(secondary_thread, sound, alert)"] = new Action(() => {
            ScheduleNotification(
                new iOSNotification()
                {
                    Title = "Example Notification: secondary_thread",
                    Subtitle = "You might want to know this!",
                    Body = $"Seconds: 3",
                    Badge = 3, // Should not increase counter, because it doesn't have required PresentationOption!!!
                    ShowInForeground = true,
                    ForegroundPresentationOption = PresentationOption.Sound | PresentationOption.Alert,
                    ThreadIdentifier = "secondary_thread",
                    Trigger = new iOSNotificationTimeIntervalTrigger(){ TimeInterval = new TimeSpan(0, 0, 3), Repeats = false },
                    Data = "Arbitrary data, from a simple notification with sound and alert"
                }
            );
        });
        groups["Schedule"]["Schedule Notification without Title, Subtitle, Body in 1 second\n(default_thread, sound, alert)"] = new Action(() => {
            ScheduleNotification(
                new iOSNotification()
                {
                    ShowInForeground = true,
                    ForegroundPresentationOption = PresentationOption.Sound | PresentationOption.Alert,
                    ThreadIdentifier = "default_thread",
                    Trigger = new iOSNotificationTimeIntervalTrigger(){ TimeInterval = new TimeSpan(0, 0, 1), Repeats = false }
                }
            );
        });
        groups["Schedule"]["Schedule Notification with Title Only in 1 second\n(default_thread, alert)"] = new Action(() => {
            ScheduleNotification(
                new iOSNotification()
                {
                    Title = "Example Notification",
                    ShowInForeground = true,
                    ForegroundPresentationOption = PresentationOption.Alert,
                    ThreadIdentifier = "default_thread",
                    Trigger = new iOSNotificationTimeIntervalTrigger(){ TimeInterval = new TimeSpan(0, 0, 1), Repeats = false }
                }
            );
        });
        groups["Schedule"]["Schedule Notification with Subtitle Only in 1 second\n(default_thread, alert)"] = new Action(() => {
            ScheduleNotification(
                new iOSNotification()
                {
                    Subtitle = "You might want to know this!",
                    ShowInForeground = true,
                    ForegroundPresentationOption = PresentationOption.Alert,
                    ThreadIdentifier = "default_thread",
                    Trigger = new iOSNotificationTimeIntervalTrigger(){ TimeInterval = new TimeSpan(0, 0, 1), Repeats = false }
                }
            );
        });
        groups["Schedule"]["Schedule Notification with Body Only in 1 second\n(secondary_thread, alert)"] = new Action(() => {
            ScheduleNotification(
                new iOSNotification()
                {
                    Body = $"Seconds: 3",
                    ShowInForeground = true,
                    ForegroundPresentationOption = PresentationOption.Alert,
                    ThreadIdentifier = "secondary_thread",
                    Trigger = new iOSNotificationTimeIntervalTrigger(){ TimeInterval = new TimeSpan(0, 0, 1), Repeats = false }
                }
            );
        });
        groups["Schedule"]["Schedule Repeated Notification to be sent 60 seconds\n(default_thread, sound, alert)"] = new Action(() => {
            ScheduleNotification(
                new iOSNotification()
                {
                    Title = "Repeated Notification",
                    Subtitle = "This could become a little bit annoying",
                    Body = $"Clear scheduled notifications if it does, it'll do the trick",
                    ShowInForeground = true,
                    ForegroundPresentationOption = PresentationOption.Sound | PresentationOption.Alert,
                    ThreadIdentifier = "default_thread",
                    Trigger = new iOSNotificationTimeIntervalTrigger(){ TimeInterval = new TimeSpan(0, 0, 60), Repeats = true }
                }
            );
        });
        groups["Schedule"]["Schedule Repeated Notification to be sent 20 seconds\n(default_thread, sound, alert)\n<color=#e74c3c>EXCEPTION</color>"] = new Action(() => {
            ScheduleNotification(
                new iOSNotification()
                {
                    Title = "Repeated Notification",
                    Subtitle = "This could become a little bit annoying",
                    Body = $"Clear scheduled notifications if it does, it'll do the trick",
                    ShowInForeground = true,
                    ForegroundPresentationOption = PresentationOption.Sound | PresentationOption.Alert,
                    ThreadIdentifier = "default_thread",
                    Trigger = new iOSNotificationTimeIntervalTrigger(){ TimeInterval = new TimeSpan(0, 0, 20), Repeats = true }
                }
            );
        });
        groups["Schedule"]["Schedule Calendar Notification to be sent in 1 minute\n(default_thread, sound, alert)"] = new Action(() => {
            DateTime now = DateTime.Now.AddMinutes(1);
            ScheduleNotification(
                new iOSNotification()
                {
                    Title = "Calendar Notification",
                    Subtitle = "Yep, they also exist",
                    Body = $"Calendar notifications can be scheduled for wayyy into the future",
                    ShowInForeground = true,
                    ForegroundPresentationOption = PresentationOption.Sound | PresentationOption.Alert,
                    ThreadIdentifier = "default_thread",
                    Trigger = new iOSNotificationCalendarTrigger()
                    {
                        Year = now.Year,
                        Month = now.Month,
                        Day = now.Day,
                        Hour = now.Hour,
                        Minute = now.Minute,
                        Second = now.Second
                    }
                }
            );
        });
        groups["Schedule"]["Schedule Location Triggered Notification\n(default_thread, sound, alert)"] = new Action(() => {
            // First, check if user has location service enabled
            if (!Input.location.isEnabledByUser)
            {
                LOGGER.Red("Cannot use Location Services");
                return;
            }
            iOSNotification thisNotification = ScheduleNotification(
                new iOSNotification()
                {
                    Title = "Wowzerz!",
                    Subtitle = "A Location Triggered Notification",
                    Body = $"Now that's interesting...",
                    ShowInForeground = true,
                    ForegroundPresentationOption = PresentationOption.Sound | PresentationOption.Alert,
                    CategoryIdentifier = "Location",
                    ThreadIdentifier = "default_thread",
                    Trigger = new iOSNotificationLocationTrigger()
                    {
                        Center = new Vector2(22.2847f, 114.1582f),
                        Radius = 5000f,
                        NotifyOnEntry = true,
                        NotifyOnExit = true,
                    }
                }
            );
            LOGGER.Orange($"Should be triggered when:", 1);
            LOGGER.Orange($"Center: {((iOSNotificationLocationTrigger)thisNotification.Trigger).Center}", 2);
            LOGGER.Orange($"Radius: {((iOSNotificationLocationTrigger)thisNotification.Trigger).Radius}", 2);
        });
        groups["Schedule"]["Schedule Notification in 3 seconds and cancel after 1 (default_thread, sound, alert)"] = new Action(() => {
            StartCoroutine(ScheduleNotificationAndRemoveItAfterSomeTime());
        });

        groups["Cancellation"] = new OrderedDictionary();
        groups["Cancellation"]["Cancel all notifications"] = new Action(() => { RemoveAllNotifications(); });
        groups["Cancellation"]["Cancel scheduled notifications"] = new Action(() => { RemoveScheduledNotifications(); });
        groups["Cancellation"]["Cancel delivered notifications"] = new Action(() => { RemoveDeliveredNotifications(); });

        foreach (KeyValuePair<string, OrderedDictionary> group in groups)
        {
            // Instantiate group
            Transform buttonGroup = GameObject.Instantiate(gameObjectReferences.buttonGroupTemplate, gameObjectReferences.buttonScrollViewContent);
            Transform buttonGroupName = buttonGroup.GetChild(0).transform;
            Transform buttonGameObject = buttonGroup.GetChild(1).transform;
            // Set group name
            buttonGroupName.GetComponentInChildren<Text>().text = group.Key.ToString();
            // Instantiate buttons
            foreach (DictionaryEntry test in group.Value)
            {
                Transform button = GameObject.Instantiate(buttonGameObject, buttonGroup);
                button.gameObject.GetComponentInChildren<Text>().text = test.Key.ToString();
                button.GetComponent<Button>().onClick.AddListener(delegate {
                    try {
                        ((Action)test.Value).Invoke();
                    } catch (Exception exception) {
                        LOGGER.Red(exception.Message);
                    }
                });
                button.GetComponent<Button>().onClick.AddListener(delegate { ScrollLogsToBottom(); });
            }
            buttonGameObject.gameObject.SetActive(false);
        }
        gameObjectReferences.buttonGroupTemplate.gameObject.SetActive(false);
    }

    IEnumerator GetCurrentLocation()
    {
        // First, check if user has location service enabled
        if (!Input.location.isEnabledByUser)
        {
            LOGGER.Red("Cannot use Location Services");
            yield break;
        }
        // Wait until service initializes
        int maxWait = 20;
        while (Input.location.status == LocationServiceStatus.Initializing && maxWait > 0)
        {
            yield return new WaitForSeconds(1);
            maxWait--;
        }
        // Service didn't initialize in 20 seconds
        if (maxWait < 1)
        {
            LOGGER.Red("Timed out");
            yield break;
        }
        // Connection has failed
        if (Input.location.status == LocationServiceStatus.Failed)
        {
            LOGGER.Red("Unable to determine device location");
            yield break;
        }
        else
        {
            // Access granted and location value could be retrieved
            LOGGER.Green("Location: " + Input.location.lastData.latitude + " " + Input.location.lastData.longitude + " " + Input.location.lastData.altitude + " " + Input.location.lastData.horizontalAccuracy + " " + Input.location.lastData.timestamp);
        }
    }


    //Request authorization if it is not enabled in Editor UI
    IEnumerator RequestAuthorization(AuthorizationOption options = AuthorizationOption.Alert | AuthorizationOption.Badge | AuthorizationOption.Sound)
    {
        LOGGER.Blue($"[{DateTime.Now.ToString("HH:mm:ss.ffffff")}] Call {MethodBase.GetCurrentMethod().Name}");
        AuthorizationRequest request = new AuthorizationRequest(options, true);
        yield return request;
        using (AuthorizationRequest req = new AuthorizationRequest(AuthorizationOption.Alert | AuthorizationOption.Badge, true))
        {
            while (!req.IsFinished) { yield return null; };
            if (req.Granted) {
                LOGGER.Green($"Authorization request was granted");
                LOGGER.Properties(req);
                Debug.Log(req.DeviceToken);
            } else {
                LOGGER.Red($"Authorization request was denied");
            }
        }
    }

    public void ShowNotificationSettings()
    {
        LOGGER.Blue($"[{DateTime.Now.ToString("HH:mm:ss.ffffff")}] Call {MethodBase.GetCurrentMethod().Name}");
        iOSNotificationSettings settings = iOSNotificationCenter.GetNotificationSettings();
        Helpers.LogProperties(settings, LOGGER);
    }

    public void ListScheduledNotifications()
    {
        LOGGER.Blue($"[{DateTime.Now.ToString("HH:mm:ss.ffffff")}] Call {MethodBase.GetCurrentMethod().Name}");
        foreach (iOSNotification notification in iOSNotificationCenter.GetScheduledNotifications())
        {
            LOGGER.Separator();
            Helpers.LogProperties(notification, LOGGER);
            LOGGER.Separator();
        }
    }

    public void ListDeliveredNotifications()
    {
        LOGGER.Blue($"[{DateTime.Now.ToString("HH:mm:ss.ffffff")}] Call {MethodBase.GetCurrentMethod().Name}");
        foreach (iOSNotification notification in iOSNotificationCenter.GetDeliveredNotifications())
        {
            LOGGER.Separator();
            Helpers.LogProperties(notification, LOGGER);
            LOGGER.Separator();
        }
    }

    public void ClearBadge()
    {
        LOGGER.Blue($"[{DateTime.Now.ToString("HH:mm:ss.ffffff")}] Call {MethodBase.GetCurrentMethod().Name}");
        iOSNotificationCenter.ApplicationBadge = 0;
    }

    public iOSNotification ScheduleNotification(iOSNotification notification, bool log = true)
    {
        if (log)
        {
            LOGGER.Blue($"[{DateTime.Now.ToString("HH:mm:ss.ffffff")}] Call {MethodBase.GetCurrentMethod().Name}");
            Helpers.LogProperties(notification, LOGGER);
        }
        iOSNotificationCenter.ScheduleNotification(notification);
        return notification;
    }

    IEnumerator ScheduleNotificationAndRemoveItAfterSomeTime()
    {
        iOSNotification thisNotification = ScheduleNotification(
            new iOSNotification()
            {
                Title = "Example Notification: default_thread",
                Subtitle = "You might want to know this!",
                Body = $"Seconds: 3",
                Badge = 3, // Should not increase counter, because it doesn't have required PresentationOption!!!
                ShowInForeground = true,
                ForegroundPresentationOption = PresentationOption.Sound | PresentationOption.Alert,
                ThreadIdentifier = "default_thread",
                Trigger = new iOSNotificationTimeIntervalTrigger(){ TimeInterval = new TimeSpan(0, 0, 3), Repeats = false },
                Data = "Arbitrary data, from a simple notification with sound and alert"
            }
        );
        LOGGER.Red("This should not be delivered as it is going to be canceled in 1 second!");
        yield return new WaitForSeconds(1.0f);
        iOSNotificationCenter.RemoveScheduledNotification(thisNotification.Identifier);
    }

    public void RemoveAllNotifications()
    {
        LOGGER.Blue($"[{DateTime.Now.ToString("HH:mm:ss.ffffff")}] Call {MethodBase.GetCurrentMethod().Name}");
        iOSNotificationCenter.RemoveAllScheduledNotifications();
        iOSNotificationCenter.RemoveAllDeliveredNotifications();
    }

    public void RemoveScheduledNotifications()
    {
        LOGGER.Blue($"[{DateTime.Now.ToString("HH:mm:ss.ffffff")}] Call {MethodBase.GetCurrentMethod().Name}");
        iOSNotificationCenter.RemoveAllScheduledNotifications();
    }

    public void RemoveDeliveredNotifications()
    {
        LOGGER.Blue($"[{DateTime.Now.ToString("HH:mm:ss.ffffff")}] Call {MethodBase.GetCurrentMethod().Name}");
        iOSNotificationCenter.RemoveAllDeliveredNotifications();
    }

    public void ScrollLogsToBottom()
    {
        Canvas.ForceUpdateCanvases();
        gameObjectReferences.logsScrollRect.verticalNormalizedPosition = 0f;
    }

#endif
}
