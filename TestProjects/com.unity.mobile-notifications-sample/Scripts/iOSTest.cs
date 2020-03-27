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
        LOGGER
            .Orange($"[{DateTime.Now.ToString("HH:mm:ss.ffffff")}] Received a notification")
            .Orange($"Setting BADGE to {iOSNotificationCenter.GetDeliveredNotifications().Length + 1}", 1)
            .Properties(notification, 1);
        // Update badge
        iOSNotificationCenter.ApplicationBadge = iOSNotificationCenter.GetDeliveredNotifications().Length + 1;
    }

    void OnRemoteNotificationReceivedHandler(iOSNotification notification)
    {
        LOGGER
            .Orange($"[{DateTime.Now.ToString("HH:mm:ss.ffffff")}] Received a remote notification")
            .Orange($"Setting BADGE to {iOSNotificationCenter.GetDeliveredNotifications().Length + 1}", 1)
            .Red($"It will not show up in foreground and it will trigger OnNotificationReceived callback, that is by design")
            .Properties(notification, 1);
        // Application still receives OnNotificationReceived callback which updates the badge
        // iOSNotificationCenter.ApplicationBadge = iOSNotificationCenter.GetDeliveredNotifications().Length + 1;

        // If we want to show this remote notification in foreground, we have to reschedule it locally
        /* LOGGER
            .Separator()
            .Orange($"Rescheduling remote notification to be sent in 1 second", 1);
        iOSNotificationTimeIntervalTrigger newTrigger = new iOSNotificationTimeIntervalTrigger(){ TimeInterval = new TimeSpan(0, 0, 1) };
        notification.Trigger = newTrigger;
        notification.ShowInForeground = true;
        iOSNotificationCenter.ScheduleNotification(notification); */
    }

    void Start()
    {
        InstantiateAllTestButtons();
        ClearBadge();
        RemoveAllNotifications();
        LOGGER
            .Clear()
            .White("Welcome!");
    }

    void OnApplicationPause(bool isPaused)
    {
        LOGGER
            .Gray($"[{DateTime.Now.ToString("HH:mm:ss.ffffff")}] Call {MethodBase.GetCurrentMethod().Name}")
            .Gray($"isPaused = {isPaused}", 1);
        if (isPaused == false)
        {
            iOSNotification notification = iOSNotificationCenter.GetLastRespondedNotification();
            if (notification != null)
            {
                LOGGER.Green($"Notification found:", 1);
                if (notification.Data != "IGNORE")
                {
                    LOGGER
                        .Orange($"[{DateTime.Now.ToString("HH:mm:ss.ffffff")}] Received notification")
                        .Orange($"Setting BADGE to {iOSNotificationCenter.GetDeliveredNotifications().Length + 1}", 1)
                        .Properties(notification, 1);
                    iOSNotificationCenter.ApplicationBadge = iOSNotificationCenter.GetDeliveredNotifications().Length + 1;
                }
            }
            else
            {
                LOGGER.Red("Notification not found!", 1);
            }
        }
    }

    private void InstantiateAllTestButtons()
    {
        groups = new Dictionary<string, OrderedDictionary>();

        groups["General"] = new OrderedDictionary();
        groups["General"]["Clear Log"] = new Action(() => {
            LOGGER.Clear();
        });
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
        groups["General"]["List Scheduled Notifications"] = new Action(() => {
            ListScheduledNotifications();
        });
        groups["General"]["List Delivered Notifications"] = new Action(() => {
            ListDeliveredNotifications();
        });
        groups["General"]["Remove First Notification In Scheduled List By ID"] = new Action(() => {
            RemoveFirstNotificationInListById();
        });
        groups["General"]["Clear the badge"] = new Action(() => {
            ClearBadge();
        });

        groups["Schedule"] = new OrderedDictionary();
        foreach (iOSNotificationTemplateTimeTrigger template in iOSNotificationsTimeTriggered)
        {
            if (template == null) continue;
            groups["Schedule"][$"[{template.timeTriggerInterval}s] {template.buttonName}"] = new Action(() => {
                ScheduleNotification(
                    new iOSNotification()
                    {
                        Identifier = template.identifier == "" ? null : template.identifier,
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
            groups["Schedule"][$"{template.buttonName}"] = new Action(() => {
                iOSNotificationCalendarTrigger trigger;
                if (template.offsetFromCurrentDate)
                {
                    DateTime offsetDate = DateTime.Now;
                    if (template.year >= 0) offsetDate = offsetDate.AddYears(template.year);
                    if (template.month >= 0) offsetDate = offsetDate.AddMonths(template.month);
                    if (template.day >= 0) offsetDate = offsetDate.AddDays(template.day);
                    if (template.hour >= 0) offsetDate = offsetDate.AddHours(template.hour);
                    if (template.minute >= 0) offsetDate = offsetDate.AddMinutes(template.minute);
                    if (template.second >= 0) offsetDate = offsetDate.AddSeconds(template.second);
                    trigger = new iOSNotificationCalendarTrigger()
                    {
                        Year = offsetDate.Year,
                        Month = offsetDate.Month,
                        Day = offsetDate.Day,
                        Hour = offsetDate.Hour,
                        Minute = offsetDate.Minute,
                        Second = offsetDate.Second
                    };
                    LOGGER.Orange($"Will trigger on:\n{offsetDate.ToString("yyyy-MM-dd HH:mm:ss")}");
                }
                else
                {
                    trigger = new iOSNotificationCalendarTrigger()
                    {
                        Year = template.year,
                        Month = template.month,
                        Day = template.day,
                        Hour = template.hour,
                        Minute = template.minute,
                        Second = template.second
                    };
                }
                ScheduleNotification(
                    new iOSNotification()
                    {
                        Identifier = template.identifier == "" ? null : template.identifier,
                        CategoryIdentifier = template.categoryIdentifier,
                        ThreadIdentifier = template.threadIdentifier,
                        Title = template.title,
                        Subtitle = template.subtitle,
                        Body = template.body,
                        ShowInForeground = template.showInForeground,
                        ForegroundPresentationOption = template.presentationOptions,
                        Badge = template.badge,
                        Data = template.data,
                        Trigger = trigger
                    }
                );
            });
        }
        foreach (iOSNotificationTemplateLocationTrigger template in iOSNotificationsLocationTriggered)
        {
            if (template == null) continue;
            groups["Schedule"][$"{template.buttonName}"] = new Action(() => {
                ScheduleNotification(
                    new iOSNotification()
                    {
                        Identifier = template.identifier == "" ? null : template.identifier,
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
                            Center = new Vector2(template.centerX, template.centerY),
                            Radius = template.radius,
                            NotifyOnEntry = template.notifyOnEntry,
                            NotifyOnExit = template.notifyOnExit
                        }
                    }
                );
            });
        }

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
                    try
                    {
                        ((Action)test.Value).Invoke();
                    }
                    catch (Exception exception)
                    {
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
            if (req.Granted)
            {
                LOGGER
                    .Green($"Authorization request was granted")
                    .Properties(req);
                Debug.Log(req.DeviceToken);
            }
            else
            {
                LOGGER.Red($"Authorization request was denied");
            }
        }
    }

    public void ShowNotificationSettings()
    {
        LOGGER.Blue($"[{DateTime.Now.ToString("HH:mm:ss.ffffff")}] Call {MethodBase.GetCurrentMethod().Name}");
        iOSNotificationSettings settings = iOSNotificationCenter.GetNotificationSettings();
        LOGGER.Properties(settings);
    }

    public void ListScheduledNotifications()
    {
        LOGGER.Blue($"[{DateTime.Now.ToString("HH:mm:ss.ffffff")}] Call {MethodBase.GetCurrentMethod().Name}");
        foreach (iOSNotification notification in iOSNotificationCenter.GetScheduledNotifications())
        {
            LOGGER
                .Separator()
                .Properties(notification)
                .Separator();
        }
    }

    public void ListDeliveredNotifications()
    {
        LOGGER.Blue($"[{DateTime.Now.ToString("HH:mm:ss.ffffff")}] Call {MethodBase.GetCurrentMethod().Name}");
        foreach (iOSNotification notification in iOSNotificationCenter.GetDeliveredNotifications())
        {
            LOGGER
                .Separator()
                .Properties(notification)
                .Separator();
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
            LOGGER
                .Blue($"[{DateTime.Now.ToString("HH:mm:ss.ffffff")}] Call {MethodBase.GetCurrentMethod().Name}")
                .Properties(notification);
        }
        iOSNotificationCenter.ScheduleNotification(notification);
        return notification;
    }

    public void RemoveFirstNotificationInListById()
    {
        LOGGER.Blue($"[{DateTime.Now.ToString("HH:mm:ss.ffffff")}] Call {MethodBase.GetCurrentMethod().Name}");
        iOSNotification[] scheduledNotifications = iOSNotificationCenter.GetScheduledNotifications();
        if (scheduledNotifications.Length > 0)
        {
            iOSNotificationCenter.RemoveScheduledNotification(scheduledNotifications[0].Identifier);
        }
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
