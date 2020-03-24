using System;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using UnityEngine;
using UnityEngine.UI;

#if PLATFORM_ANDROID
using Unity.Notifications.Android;
#endif

public class AndroidTest : MonoBehaviour
{
    public List<AndroidNotificationTemplate> androidNotifications;

#if PLATFORM_ANDROID

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
        AndroidNotificationCenter.OnNotificationReceived += OnNotificationReceivedHandler;
    }

    void OnDisable()
    {
        AndroidNotificationCenter.OnNotificationReceived -= OnNotificationReceivedHandler;
    }

    void OnNotificationReceivedHandler(AndroidNotificationIntentData notificationIntentData)
    {
        LOGGER
            .Orange($"[{DateTime.Now.ToString("HH:mm:ss.ffffff")}] Received notification")
            .Orange($"Id: {notificationIntentData.Id}", 1)
            .Orange($"Channel: {notificationIntentData.Channel}", 1)
            .Properties(notificationIntentData.Notification, 1);
    }

    void Start()
    {
        InstantiateAllTestButtons();
        HandleLastNotificationIntent();
        // Ensure that we have required channel on start
        ((Action)groups["Channels"]["Create Default Simple Channel"]).Invoke();
        ((Action)groups["Channels"]["Create Secondary Simple Channel"]).Invoke();
        ((Action)groups["Channels"]["Create Fancy Channel"]).Invoke();
        LOGGER.Clear().White("Welcome!");
    }

    void OnApplicationPause(bool isPaused)
    {
        LOGGER
            .Gray($"[{DateTime.Now.ToString("HH:mm:ss.ffffff")}] Call {MethodBase.GetCurrentMethod().Name}")
            .Gray($"isPaused = {isPaused}", 1);
        if (isPaused == false)
        {
            HandleLastNotificationIntent();
        }
    }

    private void HandleLastNotificationIntent()
    {
        AndroidNotificationIntentData notificationIntentData = AndroidNotificationCenter.GetLastNotificationIntent();
        if (notificationIntentData != null)
        {
            LOGGER
                .Green($"Notification found:", 1)
                .Orange($"Id: {notificationIntentData.Id}", 1)
                .Orange($"Channel: {notificationIntentData.Channel}", 1)
                .Properties(notificationIntentData.Notification, 1);
        } else {
            LOGGER.Red("Notification not found!", 1);
        }
    }

    private void InstantiateAllTestButtons()
    {
        groups = new Dictionary<string, OrderedDictionary>();

        groups["General"] = new OrderedDictionary();
        groups["General"]["Clear Log"] = new Action(() => { LOGGER.Clear(); });

        groups["Send"] = new OrderedDictionary();
        foreach (AndroidNotificationTemplate template in androidNotifications)
        {
            if (template == null) continue;
            groups["Send"][$"[{template.fireInSeconds}s] {template.buttonName}"] = new Action(() => {
                SendNotification(
                    new AndroidNotification()
                    {
                        Title = template.title,
                        Text = template.text,
                        SmallIcon = template.smallIcon,
                        LargeIcon = template.largeIcon,
                        Style = template.notificationStyle,
                        FireTime = System.DateTime.Now.AddSeconds(template.fireInSeconds),
                        Color = template.color,
                        Number = template.number,
                        ShouldAutoCancel = template.shouldAutoCancel,
                        UsesStopwatch = template.usesStopWatch,
                        Group = template.group,
                        GroupSummary = template.groupSummary,
                        SortKey = template.sortKey,
                        IntentData = template.intentData,
                        ShowTimestamp = template.showTimestamp
                    },
                    template.channel
                );
            });
        }

        groups["Cancellation"] = new OrderedDictionary();
        groups["Cancellation"]["Cancel all notifications"] = new Action(() => { CancelAllNotifications(); });
        groups["Cancellation"]["Cancel pending notifications"] = new Action(() => { CancelPendingNotifications(); });
        groups["Cancellation"]["Cancel displayed notifications"] = new Action(() => { CancelDisplayedNotifications(); });

        groups["Channels"] = new OrderedDictionary();
        groups["Channels"]["List All Channels"] = new Action(() => { ListAllChannels(); });
        groups["Channels"]["Create Default Simple Channel"] = new Action(() => {
            CreateChannel(
                new AndroidNotificationChannel()
                {
                    Id = "default_channel",
                    Name = "Default Channel",
                    Importance = Importance.Default,
                    Description = "Default Notifications"
                }
            );
        });
        groups["Channels"]["Create Secondary Simple Channel"] = new Action(() => {
            CreateChannel(
                new AndroidNotificationChannel()
                {
                    Id = "secondary_channel",
                    Name = "Secondary Channel",
                    Importance = Importance.Low,
                    Description = "Secondary Notifications"
                }
            );
        });
        groups["Channels"]["Create Fancy Channel"] = new Action(() => {
            CreateChannel(
                new AndroidNotificationChannel()
                {
                    Id = "fancy_channel",
                    Name = "Fancy Channel",
                    Importance = Importance.High,
                    Description = "Fancy Notifications",
                    CanBypassDnd = true,
                    CanShowBadge = true,
                    EnableLights = true,
                    EnableVibration = true,
                    LockScreenVisibility = LockScreenVisibility.Secret,
                    VibrationPattern = new long[] { 0L, 1L, 2L }
                }
            );
        });
        groups["Channels"]["Delete All Channels"] = new Action(() => { DeleteAllChannels(); });

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

    public void SendNotification(AndroidNotification notification, string channel = "default_channel", bool log = true)
    {
        if (log)
        {
            LOGGER
                .Blue($"[{DateTime.Now.ToString("HH:mm:ss.ffffff")}] Call {MethodBase.GetCurrentMethod().Name}")
                .Properties(notification, 1);
        }
        AndroidNotificationCenter.SendNotification(notification, channel);
    }

    public void CancelAllNotifications()
    {
        LOGGER.Blue($"[{DateTime.Now.ToString("HH:mm:ss.ffffff")}] Call {MethodBase.GetCurrentMethod().Name}");
        AndroidNotificationCenter.CancelAllNotifications();
    }

    public void CancelPendingNotifications()
    {
        LOGGER.Blue($"[{DateTime.Now.ToString("HH:mm:ss.ffffff")}] Call {MethodBase.GetCurrentMethod().Name}");
        AndroidNotificationCenter.CancelAllScheduledNotifications();
    }

    public void CancelDisplayedNotifications()
    {
        LOGGER.Blue($"[{DateTime.Now.ToString("HH:mm:ss.ffffff")}] Call {MethodBase.GetCurrentMethod().Name}");
        AndroidNotificationCenter.CancelAllDisplayedNotifications();
    }

    public void ListAllChannels()
    {
        LOGGER
            .Blue($"[{DateTime.Now.ToString("HH:mm:ss.ffffff")}] Call {MethodBase.GetCurrentMethod().Name}")
            .Separator();
        foreach (AndroidNotificationChannel channel in AndroidNotificationCenter.GetNotificationChannels())
        {
            LOGGER
                .Properties(channel, 1)
                .Separator();
        }
    }

    public void CreateChannel(AndroidNotificationChannel channel)
    {
        LOGGER
            .Blue($"[{DateTime.Now.ToString("HH:mm:ss.ffffff")}] Call {MethodBase.GetCurrentMethod().Name}")
            .Properties(channel, 1);
        AndroidNotificationCenter.RegisterNotificationChannel(channel);
    }

    public void DeleteAllChannels()
    {
        LOGGER.Blue($"Call {MethodBase.GetCurrentMethod().Name}");
        foreach (AndroidNotificationChannel channel in AndroidNotificationCenter.GetNotificationChannels())
        {
            AndroidNotificationCenter.DeleteNotificationChannel(channel.Id);
        }
    }

    public void ScrollLogsToBottom()
    {
        Canvas.ForceUpdateCanvases();
        gameObjectReferences.logsScrollRect.verticalNormalizedPosition = 0f;
    }

#endif
}
