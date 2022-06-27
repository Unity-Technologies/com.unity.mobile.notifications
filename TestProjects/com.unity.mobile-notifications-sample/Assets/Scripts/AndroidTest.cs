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

namespace Unity.Notifications.Tests.Sample
{
    public class AndroidTest : MonoBehaviour
    {
#if UNITY_ANDROID || UNITY_EDITOR

        private GameObjectReferences m_gameObjectReferences;
        private Dictionary<string, OrderedDictionary> m_groups;
        private Logger m_LOGGER;
        private int _notificationExplicitID;
        private Button ButtonModifyExplicitID;
        private Button ButtonCancelExplicitID;
        private Button ButtonCheckStatusExplicitID;
        private AndroidNotificationTemplate[] AndroidNotificationsTemplates;

        public int notificationExplicitID
        {
            get { return _notificationExplicitID; }
            set
            {
                _notificationExplicitID = value;
                bool buttonsEnabled = _notificationExplicitID != 0;
                ButtonModifyExplicitID.interactable = buttonsEnabled;
                ButtonCancelExplicitID.interactable = buttonsEnabled;
                ButtonCheckStatusExplicitID.interactable = buttonsEnabled;
            }
        }

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
            if (notificationIntentData.Id == notificationExplicitID)
            {
                AndroidNotificationCenter.CheckScheduledNotificationStatus(notificationExplicitID);
                notificationExplicitID = 0;
            }
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
            HandleLastNotificationIntent();
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

        private AndroidNotification parseNotificationTemplate(AndroidNotificationTemplate template)
        {
            AndroidNotification newNotification = new AndroidNotification()
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
                ShowTimestamp = template.ShowTimestamp,
                RepeatInterval = TimeSpan.FromSeconds(template.RepeatInterval),
                //ShowInForeground = template.ShowInForeground
            };
            return newNotification;
        }

        private void InstantiateAllTestButtons()
        {
            m_groups = new Dictionary<string, OrderedDictionary>();

            m_groups["General"] = new OrderedDictionary();
            m_groups["General"]["Clear Log"] = new Action(() => { m_LOGGER.Clear(); });
            m_groups["General"]["Open Settings"] = new Action(() =>
            {
                AndroidNotificationCenter.OpenNotificationSettings();
            });

            m_groups["Modify"] = new OrderedDictionary();
            //m_groups["Modify"]["Create notification preset"] = new Action(() => {  });
            m_groups["Modify"]["Modify pending Explicit notification"] = new Action(() => { ModifyExplicitNotification(); });
            m_groups["Modify"]["Cancel pending Explicit notification"] = new Action(() => { CancelExplicitNotification(); });
            m_groups["Modify"]["Check status of Explicit notification"] = new Action(() => { CheckStatusOfExplicitNotification (); });
            AndroidNotificationsTemplates = Resources.LoadAll<AndroidNotificationTemplate>("AndroidNotifications");
            m_groups["Send"] = new OrderedDictionary();
            foreach (AndroidNotificationTemplate template in AndroidNotificationsTemplates)
            {
                if (template == null) continue;
                m_groups["Send"][$"[{template.FireInSeconds}s] {template.ButtonName}"] = new Action(() =>
                {
                    SendNotification(parseNotificationTemplate(template), template.Channel, template.NotificationID);
                });
            }
            m_groups["Cancellation"] = new OrderedDictionary();
            m_groups["Cancellation"]["Cancel all notifications"] = new Action(() => { CancelAllNotifications(); });
            m_groups["Cancellation"]["Cancel pending notifications"] = new Action(() => { CancelPendingNotifications(); });
            m_groups["Cancellation"]["Cancel displayed notifications"] =
                new Action(() => { CancelDisplayedNotifications(); });

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
            m_groups["Channels"]["Open settings for secondary"] = new Action(() =>
            {
                AndroidNotificationCenter.OpenNotificationSettings("secondary_channel");
            });
            m_groups["Channels"]["Delete All Channels"] = new Action(() => { DeleteAllChannels(); });
            m_groups["Custom sequences"] = new OrderedDictionary();
            m_groups["Custom sequences"]["Run custom sequence"] = new Action(() => { StartCoroutine(RunCustomSequence()); });
            foreach (KeyValuePair<string, OrderedDictionary> group in m_groups)
            {
                // Instantiate group
                Transform buttonGroup =
                    GameObject.Instantiate(m_gameObjectReferences.ButtonGroupTemplate, m_gameObjectReferences.ButtonScrollViewContent);
                Transform buttonGroupName = buttonGroup.GetChild(0).transform;
                Transform buttonGameObject = buttonGroup.GetChild(1).transform;
                // Set group name
                buttonGroupName.GetComponentInChildren<Text>().text = group.Key.ToString();
                // Instantiate buttons
                foreach (DictionaryEntry test in group.Value)
                {
                    Transform button = GameObject.Instantiate(buttonGameObject, buttonGroup);
                    button.name = group.Key.ToString() + "/" + test.Key.ToString();
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
            ButtonModifyExplicitID = GameObject.Find("Modify/Modify pending Explicit notification").GetComponent<Button>();
            ButtonModifyExplicitID.interactable = false;
            ButtonCancelExplicitID = GameObject.Find("Modify/Cancel pending Explicit notification").GetComponent<Button>();
            ButtonCancelExplicitID.interactable = false;
            ButtonCheckStatusExplicitID = GameObject.Find("Modify/Check status of Explicit notification").GetComponent<Button>();
            ButtonCheckStatusExplicitID.interactable = false;

            m_gameObjectReferences.ButtonGroupTemplate.gameObject.SetActive(false);
        }

        public void ModifyExplicitNotification()
        {
            AndroidNotification template = new AndroidNotification() //TODO: TEMPORARY,Implement GUI for Notification building
            {
                Title = "Modified Explicit Notification title",
                Text = "Modified Explicit Notification text",
                FireTime = System.DateTime.Now.AddSeconds(3)
            };
            AndroidNotificationCenter.UpdateScheduledNotification(notificationExplicitID, template, "default_channel");
            m_LOGGER
                .Blue($"[{DateTime.Now.ToString("HH:mm:ss.ffffff")}] Call {MethodBase.GetCurrentMethod().Name}")
                .Properties(template, 1);
        }

        public void CancelExplicitNotification()
        {
            AndroidNotificationCenter.CancelScheduledNotification(notificationExplicitID);
            notificationExplicitID = 0;
            m_LOGGER
                .Blue($"[{DateTime.Now.ToString("HH:mm:ss.ffffff")}] Call {MethodBase.GetCurrentMethod().Name}");
        }

        public void CheckStatusOfExplicitNotification()
        {
            m_LOGGER
                .Blue($"[{DateTime.Now.ToString("HH:mm:ss.ffffff")}] Explicit notification (ID:{notificationExplicitID}) status: {AndroidNotificationCenter.CheckScheduledNotificationStatus(notificationExplicitID)}");

        }

        public void SendNotification(AndroidNotification notification, string channel = "default_channel", int notificationID = 0, bool log = true)
        {
            if (log)
            {
                m_LOGGER
                    .Blue($"[{DateTime.Now.ToString("HH:mm:ss.ffffff")}] Call {MethodBase.GetCurrentMethod().Name}")
                    .Properties(notification, 1);
            }
            if (notificationID != 0)
            {
                notification.Text = "ID: " + notificationID + " " + notification.Text;
                AndroidNotificationCenter.SendNotificationWithExplicitID(notification, channel, notificationID);
                notificationExplicitID = notificationID;
            }
            else
            {
                AndroidNotificationCenter.SendNotification(notification, channel);
            }
        }

        public void CancelAllNotifications()
        {
            m_LOGGER.Blue($"[{DateTime.Now.ToString("HH:mm:ss.ffffff")}] Call {MethodBase.GetCurrentMethod().Name}");
            AndroidNotificationCenter.CancelAllNotifications();
            notificationExplicitID = 0;
        }

        public void CancelPendingNotifications()
        {
            m_LOGGER.Blue($"[{DateTime.Now.ToString("HH:mm:ss.ffffff")}] Call {MethodBase.GetCurrentMethod().Name}");
            AndroidNotificationCenter.CancelAllScheduledNotifications();
            notificationExplicitID = 0;
        }

        public void CancelDisplayedNotifications()
        {
            m_LOGGER.Blue($"[{DateTime.Now.ToString("HH:mm:ss.ffffff")}] Call {MethodBase.GetCurrentMethod().Name}");
            AndroidNotificationCenter.CancelAllDisplayedNotifications();
            notificationExplicitID = 0;
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

        IEnumerator RunCustomSequence()
        {
            AndroidNotificationTemplate ant = AndroidNotificationsTemplates[4];
            SendNotification(parseNotificationTemplate(ant), ant.Channel, 15);
            SendNotification(parseNotificationTemplate(ant), ant.Channel, 20);
            SendNotification(parseNotificationTemplate(ant), ant.Channel, 28);
            SendNotification(parseNotificationTemplate(ant), ant.Channel, 14);
            yield return new WaitForSeconds(ant.FireInSeconds+5);
            AndroidNotificationCenter.CancelNotification(15);
            yield return new WaitForSeconds(5);
            AndroidNotificationCenter.CancelDisplayedNotification(20);
            yield return new WaitForSeconds(5);
            SendNotification(parseNotificationTemplate(ant), ant.Channel, 99);
            SendNotification(parseNotificationTemplate(ant), ant.Channel, 99);
            yield return new WaitForSeconds(ant.FireInSeconds-1);
            SendNotification(parseNotificationTemplate(ant), ant.Channel, 99);
            SendNotification(parseNotificationTemplate(ant), ant.Channel, 99);
        }

#endif
    }
}
