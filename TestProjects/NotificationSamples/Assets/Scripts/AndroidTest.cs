using System;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using UnityEngine;
using UnityEngine.UI;

#if UNITY_ANDROID || UNITY_EDITOR
using Unity.Notifications.Android;
#endif

public class AndroidTest : MonoBehaviour
{
    #if UNITY_ANDROID || UNITY_EDITOR

    private GameObjectReferences m_gameObjectReferences;
    private Dictionary<string, OrderedDictionary> m_groups;
    private Logger m_LOGGER;

    void Awake()
    {
        m_gameObjectReferences = gameObject.GetComponent<GameObjectReferences>();
        m_LOGGER = new Logger(m_gameObjectReferences.LogsText);
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
        m_LOGGER
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
        ((Action)m_groups["Channels"]["Create Default Simple Channel"]).Invoke();
        ((Action)m_groups["Channels"]["Create Secondary Simple Channel"]).Invoke();
        ((Action)m_groups["Channels"]["Create Fancy Channel"]).Invoke();
        m_LOGGER.Clear().White("Welcome!");
    }

    void OnApplicationPause(bool isPaused)
    {
        m_LOGGER
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
            m_LOGGER
                .Green($"Notification found:", 1)
                .Orange($"Id: {notificationIntentData.Id}", 1)
                .Orange($"Channel: {notificationIntentData.Channel}", 1)
                .Properties(notificationIntentData.Notification, 1);
        }
        else
        {
            m_LOGGER.Red("Notification not found!", 1);
        }
    }

    private void InstantiateAllTestButtons()
    {
        m_groups = new Dictionary<string, OrderedDictionary>();

        m_groups["General"] = new OrderedDictionary();
        m_groups["General"]["Clear Log"] = new Action(() => { m_LOGGER.Clear(); });

        m_groups["Send"] = new OrderedDictionary();
        foreach (AndroidNotificationTemplate template in Resources.LoadAll("AndroidNotifications", typeof(AndroidNotificationTemplate)))
        {
            if (template == null) continue;
            m_groups["Send"][$"[{template.FireInSeconds}s] {template.ButtonName}"] = new Action(() => {
                SendNotification(
                    new AndroidNotification()
                    {
                        Title = template.Title,
                        Text = template.Text,
                        SmallIcon = template.SmallIcon,
                        LargeIcon = template.LargeIcon,
                        Style = template.NotificationStyle,
                        FireTime = System.DateTime.Now.AddSeconds(template.FireInSeconds),
                        Color = template.Color,
                        Number = template.Number,
                        ShouldAutoCancel = template.ShouldAutoCancel,
                        UsesStopwatch = template.UsesStopWatch,
                        Group = template.Group,
                        GroupSummary = template.GroupSummary,
                        SortKey = template.SortKey,
                        IntentData = template.IntentData,
                        ShowTimestamp = template.ShowTimestamp
                    },
                    template.Channel
                );
            });
        }

        m_groups["Cancellation"] = new OrderedDictionary();
        m_groups["Cancellation"]["Cancel all notifications"] = new Action(() => { CancelAllNotifications(); });
        m_groups["Cancellation"]["Cancel pending notifications"] = new Action(() => { CancelPendingNotifications(); });
        m_groups["Cancellation"]["Cancel displayed notifications"] = new Action(() => { CancelDisplayedNotifications(); });

        m_groups["Channels"] = new OrderedDictionary();
        m_groups["Channels"]["List All Channels"] = new Action(() => { ListAllChannels(); });
        m_groups["Channels"]["Create Default Simple Channel"] = new Action(() => {
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
        m_groups["Channels"]["Create Secondary Simple Channel"] = new Action(() => {
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
        m_groups["Channels"]["Create Fancy Channel"] = new Action(() => {
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
        m_groups["Channels"]["Delete All Channels"] = new Action(() => { DeleteAllChannels(); });

        foreach (KeyValuePair<string, OrderedDictionary> group in m_groups)
        {
            // Instantiate group
            Transform buttonGroup = GameObject.Instantiate(m_gameObjectReferences.ButtonGroupTemplate, m_gameObjectReferences.ButtonScrollViewContent);
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
                        m_LOGGER.Red(exception.Message);
                    }
                });
                button.GetComponent<Button>().onClick.AddListener(delegate { ScrollLogsToBottom(); });
            }
            buttonGameObject.gameObject.SetActive(false);
        }
        m_gameObjectReferences.ButtonGroupTemplate.gameObject.SetActive(false);
    }

    public void SendNotification(AndroidNotification notification, string channel = "default_channel", bool log = true)
    {
        if (log)
        {
            m_LOGGER
                .Blue($"[{DateTime.Now.ToString("HH:mm:ss.ffffff")}] Call {MethodBase.GetCurrentMethod().Name}")
                .Properties(notification, 1);
        }
        AndroidNotificationCenter.SendNotification(notification, channel);
    }

    public void CancelAllNotifications()
    {
        m_LOGGER.Blue($"[{DateTime.Now.ToString("HH:mm:ss.ffffff")}] Call {MethodBase.GetCurrentMethod().Name}");
        AndroidNotificationCenter.CancelAllNotifications();
    }

    public void CancelPendingNotifications()
    {
        m_LOGGER.Blue($"[{DateTime.Now.ToString("HH:mm:ss.ffffff")}] Call {MethodBase.GetCurrentMethod().Name}");
        AndroidNotificationCenter.CancelAllScheduledNotifications();
    }

    public void CancelDisplayedNotifications()
    {
        m_LOGGER.Blue($"[{DateTime.Now.ToString("HH:mm:ss.ffffff")}] Call {MethodBase.GetCurrentMethod().Name}");
        AndroidNotificationCenter.CancelAllDisplayedNotifications();
    }

    public void ListAllChannels()
    {
        m_LOGGER
            .Blue($"[{DateTime.Now.ToString("HH:mm:ss.ffffff")}] Call {MethodBase.GetCurrentMethod().Name}")
            .Separator();
        foreach (AndroidNotificationChannel channel in AndroidNotificationCenter.GetNotificationChannels())
        {
            m_LOGGER
                .Properties(channel, 1)
                .Separator();
        }
    }

    public void CreateChannel(AndroidNotificationChannel channel)
    {
        m_LOGGER
            .Blue($"[{DateTime.Now.ToString("HH:mm:ss.ffffff")}] Call {MethodBase.GetCurrentMethod().Name}")
            .Properties(channel, 1);
        AndroidNotificationCenter.RegisterNotificationChannel(channel);
    }

    public void DeleteAllChannels()
    {
        m_LOGGER.Blue($"Call {MethodBase.GetCurrentMethod().Name}");
        foreach (AndroidNotificationChannel channel in AndroidNotificationCenter.GetNotificationChannels())
        {
            AndroidNotificationCenter.DeleteNotificationChannel(channel.Id);
        }
    }

    public void ScrollLogsToBottom()
    {
        Canvas.ForceUpdateCanvases();
        m_gameObjectReferences.LogsScrollRect.verticalNormalizedPosition = 0f;
    }

    #endif
}
