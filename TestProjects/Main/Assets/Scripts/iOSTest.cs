using System;
using System.IO;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using UnityEngine;
using UnityEngine.UI;

#if UNITY_IOS || UNITY_EDITOR
using Unity.Notifications.iOS;
#endif

namespace Unity.Notifications.Tests.Sample
{
    public class iOSTest : MonoBehaviour
    {
#if UNITY_IOS || UNITY_EDITOR

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
            m_LOGGER
                .Orange($"[{DateTime.Now.ToString("HH:mm:ss.ffffff")}] Received a notification")
                .Orange($"Setting BADGE to {iOSNotificationCenter.GetDeliveredNotifications().Length + 1}", 1)
                .Properties(notification, 1);

            if (notification.UserInfo.Count > 3)
            {
                m_LOGGER.Orange("Received user info:");
                foreach (var item in notification.UserInfo)
                {
                    m_LOGGER.Gray($"{item.Key}: {item.Value}");
                }
            }

            // Update badge
            iOSNotificationCenter.ApplicationBadge = iOSNotificationCenter.GetDeliveredNotifications().Length + 1;
        }

        void OnRemoteNotificationReceivedHandler(iOSNotification notification)
        {
            m_LOGGER
                .Orange($"[{DateTime.Now.ToString("HH:mm:ss.ffffff")}] Received a remote notification")
                .Orange($"Setting BADGE to {iOSNotificationCenter.GetDeliveredNotifications().Length + 1}", 1)
                .Red($"It will not show up in foreground and it will trigger OnNotificationReceived callback, that is by design")
                .Properties(notification, 1);
            // Application still receives OnNotificationReceived callback which updates the badge
            // iOSNotificationCenter.ApplicationBadge = iOSNotificationCenter.GetDeliveredNotifications().Length + 1;

            // If we want to show this remote notification in foreground, we have to reschedule it locally
            /* m_LOGGER
                .Separator()
                .Orange($"Rescheduling remote notification to be sent in 1 second", 1);
            iOSNotificationTimeIntervalTrigger newTrigger = new iOSNotificationTimeIntervalTrigger(){ TimeInterval = new TimeSpan(0, 0, 1) };
            notification.Trigger = newTrigger;
            notification.ShowInForeground = true;
            iOSNotificationCenter.ScheduleNotification(notification); */
        }

        void Start()
        {
            // in case a killed app was launched by clicking a notification
            iOSNotification notification = iOSNotificationCenter.GetLastRespondedNotification();
            string lastAction = iOSNotificationCenter.GetLastRespondedNotificationAction();
            string lastTextInput = iOSNotificationCenter.GetLastRespondedNotificationUserText();
            RegisterCategories();
            InstantiateAllTestButtons();
            ClearBadge();
            RemoveAllNotifications();
            m_LOGGER
                .Clear()
                .White("Welcome!");
            if (notification != null)
            {
                m_LOGGER.Green("Application launched via notification");
                if (notification.Data != "IGNORE")
                {
                    m_LOGGER
                        .Orange($"[{DateTime.Now.ToString("HH:mm:ss.ffffff")}] Received notification")
                        .Properties(notification, 1);
                    if (lastAction != null)
                    {
                        string output = lastTextInput != null ? $"Used action {lastAction} with input '{lastTextInput}'" : $"Used action {lastAction}";
                        m_LOGGER
                            .Orange(output);
                    }
                }
            }
        }

        void OnApplicationPause(bool isPaused)
        {
            m_LOGGER
                .Gray($"[{DateTime.Now.ToString("HH:mm:ss.ffffff")}] Call {MethodBase.GetCurrentMethod().Name}")
                .Gray($"isPaused = {isPaused}", 1);
            if (isPaused == false)
            {
                iOSNotification notification = iOSNotificationCenter.GetLastRespondedNotification();
                if (notification != null)
                {
                    m_LOGGER.Green($"Notification found:", 1);
                    if (notification.Data != "IGNORE")
                    {
                        m_LOGGER
                            .Orange($"[{DateTime.Now.ToString("HH:mm:ss.ffffff")}] Received notification")
                            .Orange($"Setting BADGE to {iOSNotificationCenter.GetDeliveredNotifications().Length + 1}", 1)
                            .Properties(notification, 1);
                        string lastAction = iOSNotificationCenter.GetLastRespondedNotificationAction();
                        string lastTextInput = iOSNotificationCenter.GetLastRespondedNotificationUserText();
                        if (lastAction != null)
                        {
                            string output = lastTextInput != null ? $"Used action {lastAction} with input '{lastTextInput}'" : $"Used action {lastAction}";
                            m_LOGGER
                                .Orange(output);
                        }
                        iOSNotificationCenter.ApplicationBadge =
                            iOSNotificationCenter.GetDeliveredNotifications().Length + 1;
                    }
                }
                else
                {
                    m_LOGGER.Red("Notification not found!", 1);
                }
            }
        }

        private void RegisterCategories()
        {
            var actionConfirm = new iOSNotificationAction("ACTION_CONFIRM", "Confirm", iOSNotificationActionOptions.Foreground|iOSNotificationActionOptions.Required);
            var actionLater = new iOSNotificationAction("ACTION_LATER", "Later");
            var actionReject = new iOSNotificationAction("ACTION_REJECT", "Reject", iOSNotificationActionOptions.Destructive);
            var actionInput = new iOSTextInputNotificationAction("ACTION_INPUT", "Respond", iOSNotificationActionOptions.Foreground, "Respond");
            var twoActions = new iOSNotificationCategory("THREE_ACTIONS");
            twoActions.AddActions(new[] { actionConfirm, actionLater, actionReject });
            var withInput = new iOSNotificationCategory("WITH_INPUT");
            withInput.AddActions(new[] { actionInput, actionReject });
            iOSNotificationCenter.SetNotificationCategories(new[] { twoActions, withInput });
        }

        private void InstantiateAllTestButtons()
        {
            m_groups = new Dictionary<string, OrderedDictionary>();

            m_groups["General"] = new OrderedDictionary();
            m_groups["General"]["Clear Log"] = new Action(() =>
            {
                m_LOGGER.Clear();
            });
            m_groups["General"]["Get Current Location"] = new Action(() =>
            {
                StartCoroutine(GetCurrentLocation());
            });
            m_groups["General"]["Request Authorization Sound + Badge + Alert"] = new Action(() =>
            {
                StartCoroutine(RequestAuthorization(AuthorizationOption.Alert | AuthorizationOption.Badge | AuthorizationOption.Sound));
            });
            m_groups["General"]["Request Authorization Sound + Badge"] = new Action(() =>
            {
                StartCoroutine(RequestAuthorization(AuthorizationOption.Sound | AuthorizationOption.Badge));
            });
            m_groups["General"]["Show Notification Settings"] = new Action(() =>
            {
                ShowNotificationSettings();
            });
            m_groups["General"]["List Scheduled Notifications"] = new Action(() =>
            {
                ListScheduledNotifications();
            });
            m_groups["General"]["List Delivered Notifications"] = new Action(() =>
            {
                ListDeliveredNotifications();
            });
            m_groups["General"]["Remove First Notification In Scheduled List By ID"] = new Action(() =>
            {
                RemoveFirstNotificationInListById();
            });
            m_groups["General"]["Set the badge"] = new Action(() =>
            {
                SetBadge();
            });
            m_groups["General"]["Clear the badge"] = new Action(() =>
            {
                ClearBadge();
            });
            m_groups["General"]["Open settings"] = new Action(iOSNotificationCenter.OpenNotificationSettings);

            m_groups["Schedule"] = new OrderedDictionary();
            foreach (iOSNotificationTemplateTimeTrigger template in Resources.LoadAll("iOSNotifications/TimeIntervalTrigger", typeof(iOSNotificationTemplateTimeTrigger)))
            {
                if (template == null) continue;
                m_groups["Schedule"][$"[{template.TimeTriggerInterval}s] {template.ButtonName}"] = new Action(() =>
                {
                    var notification = new iOSNotification()
                    {
                        Identifier = template.Identifier == "" ? null : template.Identifier,
                        CategoryIdentifier = template.CategoryIdentifier,
                        ThreadIdentifier = template.ThreadIdentifier,
                        Title = template.Title,
                        Subtitle = template.Subtitle,
                        Body = template.Body,
                        ShowInForeground = template.ShowInForeground,
                        ForegroundPresentationOption = template.PresentationOptions,
                        Badge = template.Badge,
                        Data = template.Data,
                        Trigger = new iOSNotificationTimeIntervalTrigger()
                        {
                            TimeInterval = TimeSpan.FromSeconds(template.TimeTriggerInterval),
                            Repeats = template.Repeats
                        }
                    };
                    foreach (var item in template.UserInfo)
                        notification.UserInfo[item.Key] = item.Value;
                    if (template.Attachments != null && template.Attachments.Length > 0)
                    {
                        var attachments = new List<iOSNotificationAttachment>();
                        foreach (var att in template.Attachments)
                            attachments.Add(new iOSNotificationAttachment() { Url = new Uri(Path.Combine(Application.streamingAssetsPath, att)).AbsoluteUri });
                        notification.Attachments = attachments;
                    }
                    ScheduleNotification(notification);
                });
            }
            foreach (iOSNotificationTemplateCalendarTrigger template in Resources.LoadAll("iOSNotifications/CalendarTrigger", typeof(iOSNotificationTemplateCalendarTrigger)))
            {
                if (template == null) continue;
                m_groups["Schedule"][$"{template.ButtonName}"] = new Action(() =>
                {
                    iOSNotificationCalendarTrigger trigger;
                    if (template.OffsetFromCurrentDate)
                    {
                        DateTime offsetDate = DateTime.Now;
                        if (template.Year >= 0) offsetDate = offsetDate.AddYears(template.Year);
                        if (template.Month >= 0) offsetDate = offsetDate.AddMonths(template.Month);
                        if (template.Day >= 0) offsetDate = offsetDate.AddDays(template.Day);
                        if (template.Hour >= 0) offsetDate = offsetDate.AddHours(template.Hour);
                        if (template.Minute >= 0) offsetDate = offsetDate.AddMinutes(template.Minute);
                        if (template.Second >= 0) offsetDate = offsetDate.AddSeconds(template.Second);
                        trigger = new iOSNotificationCalendarTrigger()
                        {
                            Year = offsetDate.Year,
                            Month = offsetDate.Month,
                            Day = offsetDate.Day,
                            Hour = offsetDate.Hour,
                            Minute = offsetDate.Minute,
                            Second = offsetDate.Second
                        };
                        m_LOGGER.Orange($"Will trigger on:\n{offsetDate.ToString("yyyy-MM-dd HH:mm:ss")}");
                    }
                    else
                    {
                        trigger = new iOSNotificationCalendarTrigger()
                        {
                            Year = template.Year,
                            Month = template.Month,
                            Day = template.Day,
                            Hour = template.Hour,
                            Minute = template.Minute,
                            Second = template.Second
                        };
                    }
                    ScheduleNotification(
                        new iOSNotification()
                        {
                            Identifier = template.Identifier == "" ? null : template.Identifier,
                            CategoryIdentifier = template.CategoryIdentifier,
                            ThreadIdentifier = template.ThreadIdentifier,
                            Title = template.Title,
                            Subtitle = template.Subtitle,
                            Body = template.Body,
                            ShowInForeground = template.ShowInForeground,
                            ForegroundPresentationOption = template.PresentationOptions,
                            Badge = template.Badge,
                            Data = template.Data,
                            Trigger = trigger
                        }
                    );
                });
            }
            foreach (iOSNotificationTemplateLocationTrigger template in Resources.LoadAll("iOSNotifications/LocationTrigger", typeof(iOSNotificationTemplateLocationTrigger)))
            {
                if (template == null) continue;
                m_groups["Schedule"][$"{template.ButtonName}"] = new Action(() =>
                {
                    ScheduleNotification(
                        new iOSNotification()
                        {
                            Identifier = template.Identifier == "" ? null : template.Identifier,
                            CategoryIdentifier = template.CategoryIdentifier,
                            ThreadIdentifier = template.ThreadIdentifier,
                            Title = template.Title,
                            Subtitle = template.Subtitle,
                            Body = template.Body,
                            ShowInForeground = template.ShowInForeground,
                            ForegroundPresentationOption = template.PresentationOptions,
                            Badge = template.Badge,
                            Data = template.Data,
                            Trigger = new iOSNotificationLocationTrigger()
                            {
                                Center = new Vector2(template.CenterX, template.CenterY),
                                Radius = template.Radius,
                                NotifyOnEntry = template.NotifyOnEntry,
                                NotifyOnExit = template.NotifyOnExit
                            }
                        }
                    );
                });
            }

            m_groups["Cancellation"] = new OrderedDictionary();
            m_groups["Cancellation"]["Cancel all notifications"] = new Action(() => { RemoveAllNotifications(); });
            m_groups["Cancellation"]["Cancel scheduled notifications"] =
                new Action(() => { RemoveScheduledNotifications(); });
            m_groups["Cancellation"]["Cancel delivered notifications"] =
                new Action(() => { RemoveDeliveredNotifications(); });

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
                    button.gameObject.GetComponentInChildren<Text>().text = test.Key.ToString();
                    button.GetComponent<Button>().onClick.AddListener(delegate
                    {
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

        IEnumerator GetCurrentLocation()
        {
            // First, check if user has location service enabled
            if (!Input.location.isEnabledByUser)
            {
                m_LOGGER.Red("Cannot use Location Services");
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
                m_LOGGER.Red("Timed out");
                yield break;
            }
            // Connection has failed
            if (Input.location.status == LocationServiceStatus.Failed)
            {
                m_LOGGER.Red("Unable to determine device location");
                yield break;
            }
            else
            {
                // Access granted and location value could be retrieved
                m_LOGGER.Green("Location: " + Input.location.lastData.latitude + " " + Input.location.lastData.longitude + " " + Input.location.lastData.altitude + " " + Input.location.lastData.horizontalAccuracy + " " + Input.location.lastData.timestamp);
            }
        }

        //Request authorization if it is not enabled in Editor UI
        IEnumerator RequestAuthorization(AuthorizationOption options)
        {
            m_LOGGER.Blue($"[{DateTime.Now.ToString("HH:mm:ss.ffffff")}] Call {MethodBase.GetCurrentMethod().Name}");
            using (AuthorizationRequest request = new AuthorizationRequest(options, true))
            {
                while (!request.IsFinished)
                    yield return null;

                if (request.Granted)
                {
                    m_LOGGER
                        .Green($"Authorization request was granted")
                        .Properties(request);
                    Debug.Log(request.DeviceToken);
                }
                else
                {
                    m_LOGGER.Red($"Authorization request was denied");
                }
            }
        }

        public void ShowNotificationSettings()
        {
            m_LOGGER.Blue($"[{DateTime.Now.ToString("HH:mm:ss.ffffff")}] Call {MethodBase.GetCurrentMethod().Name}");
            iOSNotificationSettings settings = iOSNotificationCenter.GetNotificationSettings();
            m_LOGGER.Fields(settings);
        }

        public void ListScheduledNotifications()
        {
            m_LOGGER.Blue($"[{DateTime.Now.ToString("HH:mm:ss.ffffff")}] Call {MethodBase.GetCurrentMethod().Name}");
            foreach (iOSNotification notification in iOSNotificationCenter.GetScheduledNotifications())
            {
                m_LOGGER
                    .Separator()
                    .Properties(notification)
                    .Separator();
            }
        }

        public void ListDeliveredNotifications()
        {
            m_LOGGER.Blue($"[{DateTime.Now.ToString("HH:mm:ss.ffffff")}] Call {MethodBase.GetCurrentMethod().Name}");
            foreach (iOSNotification notification in iOSNotificationCenter.GetDeliveredNotifications())
            {
                m_LOGGER
                    .Separator()
                    .Properties(notification)
                    .Separator();
            }
        }

        public void ClearBadge()
        {
            m_LOGGER.Blue($"[{DateTime.Now.ToString("HH:mm:ss.ffffff")}] Call {MethodBase.GetCurrentMethod().Name}");
            iOSNotificationCenter.ApplicationBadge = 0;
        }

        public void SetBadge()
        {
            m_LOGGER.Blue($"[{DateTime.Now.ToString("HH:mm:ss.ffffff")}] Call {MethodBase.GetCurrentMethod().Name}");
            iOSNotificationCenter.ApplicationBadge = 40;
        }

        public iOSNotification ScheduleNotification(iOSNotification notification, bool log = true)
        {
            if (log)
            {
                m_LOGGER
                    .Blue($"[{DateTime.Now.ToString("HH:mm:ss.ffffff")}] Call {MethodBase.GetCurrentMethod().Name}")
                    .Properties(notification);
            }
            iOSNotificationCenter.ScheduleNotification(notification);
            return notification;
        }

        public void RemoveFirstNotificationInListById()
        {
            m_LOGGER.Blue($"[{DateTime.Now.ToString("HH:mm:ss.ffffff")}] Call {MethodBase.GetCurrentMethod().Name}");
            iOSNotification[] scheduledNotifications = iOSNotificationCenter.GetScheduledNotifications();
            if (scheduledNotifications.Length > 0)
            {
                iOSNotificationCenter.RemoveScheduledNotification(scheduledNotifications[0].Identifier);
            }
        }

        public void RemoveAllNotifications()
        {
            m_LOGGER.Blue($"[{DateTime.Now.ToString("HH:mm:ss.ffffff")}] Call {MethodBase.GetCurrentMethod().Name}");
            iOSNotificationCenter.RemoveAllScheduledNotifications();
            iOSNotificationCenter.RemoveAllDeliveredNotifications();
        }

        public void RemoveScheduledNotifications()
        {
            m_LOGGER.Blue($"[{DateTime.Now.ToString("HH:mm:ss.ffffff")}] Call {MethodBase.GetCurrentMethod().Name}");
            iOSNotificationCenter.RemoveAllScheduledNotifications();
        }

        public void RemoveDeliveredNotifications()
        {
            m_LOGGER.Blue($"[{DateTime.Now.ToString("HH:mm:ss.ffffff")}] Call {MethodBase.GetCurrentMethod().Name}");
            iOSNotificationCenter.RemoveAllDeliveredNotifications();
        }

        public void ScrollLogsToBottom()
        {
            Canvas.ForceUpdateCanvases();
            m_gameObjectReferences.LogsScrollRect.verticalNormalizedPosition = 0f;
        }

#endif
    }
}
