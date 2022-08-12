
#pragma warning disable IDE0044 // Making serialized fields readonly hides them from the inspector
#pragma warning disable IDE1006 // VRChat public method network execution prevention using underscore
#pragma warning disable 649

using System;
using UdonSharp;
using UnityEngine;
using UnityEngine.UI;
using VRC.SDKBase;
using Varneon.UdonPrefabs.Abstract;
using Varneon.VInspector;

namespace Varneon.UdonPrefabs.RuntimeTools
{
    /// <summary>
    /// In-game console window for debugging UdonBehaviours
    /// </summary>
    [SelectionBase]
    [DefaultExecutionOrder(-2146483648)]
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class UdonConsole : UdonLogger
    {
        #region Variables

        #region Serialized
        [Header("Settings")]
        [SerializeField]
        [FieldParentElement("Foldout_Settings")]
        private bool ShowTimestamps = false;

        [SerializeField]
        [FieldParentElement("Foldout_Settings")]
        private int MaxLogEntries = 100;

        [SerializeField, Range(8, 32)]
        [FieldParentElement("Foldout_Settings")]
        private int FontSize = 24;

        [SerializeField]
        [FieldParentElement("Foldout_Settings")]
        private bool ProxyEntriesToLogs;

        [Space]
        [Header("References")]
        [SerializeField]
        [FieldParentElement("Foldout_References")]
        private RectTransform LogWindow;

        [SerializeField]
        [FieldParentElement("Foldout_References")]
        private GameObject LogItem;

        [SerializeField]
        [FieldParentElement("Foldout_References")]
        private Toggle ToggleLog, ToggleWarning, ToggleError, ToggleTimestamps;

        [SerializeField]
        [FieldParentElement("Foldout_References")]
        private InputField MaxLogEntriesField, FontSizeField;
        #endregion

        #region Private
        private Scrollbar scrollbar;

        private RectTransform canvasRoot;
        #endregion

        #region Constants
        private const string LOG_PREFIX = "[<color=#00FFFF>UdonConsole</color>]:";

        private const string
            JOIN_PREFIX = "[<color=lime>JOIN</color>]",
            LEAVE_PREFIX = "[<color=red>LEAVE</color>]";

        private const string WHITESPACE = " ";

        private const int MAX_ENTRY_ADJUSTMENT_STEP = 10;

        private const int
            FONT_MIN_SIZE = 8,
            FONT_MAX_SIZE = 32;

        private const int ENTRIES_HARDCAP = 1000;
        #endregion

        #endregion

        #region Private Methods
        private void Start()
        {
            LogItem.GetComponentInChildren<Text>(true).fontSize = FontSize;
            ToggleTimestamps.isOn = !ShowTimestamps;
            scrollbar = GetComponentInChildren<Scrollbar>(true);
            canvasRoot = GetComponentInChildren<Canvas>(true).GetComponent<RectTransform>();

            _Log($"{LOG_PREFIX} This is Varneon's Udon Essentials Console!");
            _LogWarning($"{LOG_PREFIX} It can show warnings if something is out of the ordinary");
            _LogError($"{LOG_PREFIX} And errors can also be shown if something goes completely wrong");
            _Log($"{LOG_PREFIX} Context objects are also supported:", this);
            _Log($"{LOG_PREFIX} As well as assertions:");
            _Assert(false, null);
        }

        /// <summary>
        /// Reloads all entries and applies filters
        /// </summary>
        private void ReloadLogs()
        {
            int entryOverflow = GetCurrentLogEntryCount() - MaxLogEntries;

            if (entryOverflow > 0)
            {
                for (int i = 0; i < entryOverflow; i++)
                {
                    Destroy(LogWindow.GetChild(i).gameObject);
                }
            }

            for (int i = 0; i < GetCurrentLogEntryCount(); i++)
            {
                GameObject item = LogWindow.GetChild(i).gameObject;

                string[] info = item.name.Split(' ');

                LogType type = (LogType)int.Parse(info[0]);

                string timestamp = string.Join(WHITESPACE, new string[] { info[1], info[2] });

                Text text = item.GetComponent<Text>();

                string textContent = text.text;

                bool hasTimestamp = textContent.StartsWith(timestamp);

                if (ShowTimestamps && !hasTimestamp) { text.text = string.Join(WHITESPACE, new string[] { timestamp, textContent }); }
                else if (!ShowTimestamps && hasTimestamp) { text.text = text.text.Substring(timestamp.Length + 1); }

                SetLogEntryActive(item, type);
            }
        }

        /// <summary>
        /// Sets the log entry active based on current filter states
        /// </summary>
        /// <param name="logEntry"></param>
        /// <param name="type"></param>
        private void SetLogEntryActive(GameObject logEntry, LogType type)
        {
            logEntry.SetActive(
                (type == LogType.Log && !ToggleLog.isOn) ||
                (type == LogType.Warning && !ToggleWarning.isOn) ||
                ((type == LogType.Error || type == LogType.Exception || type == LogType.Assert) && !ToggleError.isOn)
                );
        }

        /// <summary>
        /// Gets the current timestamp formatted as "yyyy.MM.dd HH:mm:ss"
        /// </summary>
        /// <returns>Formatted timestamp string</returns>
        private string GetTimestamp()
        {
            return DateTime.UtcNow.ToLocalTime().ToString("yyyy.MM.dd HH:mm:ss");
        }

        /// <summary>
        /// Write line to the console
        /// </summary>
        /// <param name="logType"></param>
        /// <param name="text"></param>
        private void WriteLine(LogType logType, string message)
        {
            Text textComponent;

            Transform newEntry;

            string timestamp = GetTimestamp();

            if (GetCurrentLogEntryCount() < MaxLogEntries)
            {
                newEntry = Instantiate(LogItem).transform;
                newEntry.SetParent(LogWindow);
                newEntry.localPosition = Vector3.zero;
                newEntry.localRotation = Quaternion.identity;
                newEntry.localScale = Vector3.one;
            }
            else
            {
                newEntry = LogWindow.GetChild(0);
                newEntry.SetAsLastSibling();
            }

            GameObject newEntryGO = newEntry.gameObject;

            newEntryGO.name = string.Join(WHITESPACE, new string[] { ((int)logType).ToString(), timestamp });
            textComponent = newEntry.GetComponent<Text>();

            textComponent.text = ShowTimestamps ? message : message.Substring(timestamp.Length + 1);

            SetLogEntryActive(newEntryGO, logType);

            LayoutRebuilder.ForceRebuildLayoutImmediate(canvasRoot);

            SendCustomEventDelayedFrames(nameof(_ScrollToBottom), 3);
        }

        /// <summary>
        /// Get the number of entries in the console
        /// </summary>
        /// <returns>Log entry count</returns>
        private int GetCurrentLogEntryCount()
        {
            return LogWindow.childCount;
        }

        private string BuildLogStringOutput(LogType logType, object message)
        {
            return string.Join(" ", GetTimestamp(), GetLogTypePrefix(logType), MessageObjectToString(message));
        }

        private string BuildLogStringOutput(LogType logType, object message, UnityEngine.Object context)
        {
            return string.Join(" ", GetTimestamp(), GetLogTypePrefix(logType), MessageObjectToString(message), ContextObjectToString(context));
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// Scrolls to the bottom of the window
        /// </summary>
        public void _ScrollToBottom()
        {
            scrollbar.value = 0f;
        }

        /// <summary>
        /// Clears the log entries
        /// </summary>
        public void _Clear()
        {
            for (int i = 0; i < GetCurrentLogEntryCount(); i++)
            {
                Destroy(LogWindow.GetChild(i).gameObject);
            }
        }
        #endregion

        #region Logger Overrides
        protected override void Log(LogType logType, object message)
        {
            WriteLine(logType, BuildLogStringOutput(logType, message));
        }

        protected override void Log(LogType logType, object message, UnityEngine.Object context)
        {
            WriteLine(logType, BuildLogStringOutput(logType, message, context));
        }

        protected override void LogFormat(LogType logType, string format, params object[] args)
        {
            WriteLine(logType, BuildLogStringOutput(logType, string.Format(format, args)));
        }

        protected override void LogFormat(LogType logType, UnityEngine.Object context, string format, params object[] args)
        {
            WriteLine(logType, BuildLogStringOutput(logType, string.Format(format, args), context));
        }
        #endregion

        #region Player Events
        public override void OnPlayerJoined(VRCPlayerApi player)
        {
            _Log(string.Join(WHITESPACE, new string[] { LOG_PREFIX, JOIN_PREFIX, player.displayName }));
        }

        public override void OnPlayerLeft(VRCPlayerApi player)
        {
            if (!Utilities.IsValid(player)) { return; }

            _Log(string.Join(WHITESPACE, new string[] { LOG_PREFIX, LEAVE_PREFIX, player.displayName }));
        }
        #endregion

        #region Filter Toggles
        /// <summary>
        /// Toggles Log entry type filtering
        /// </summary>
        public void _ToggleFilterLog()
        {
            ReloadLogs();
        }

        /// <summary>
        /// Toggles Warning entry type filtering
        /// </summary>
        public void _ToggleFilterWarning()
        {
            ReloadLogs();
        }

        /// <summary>
        /// Toggles Error entry type filtering
        /// </summary>
        public void _ToggleFilterError()
        {
            ReloadLogs();
        }

        /// <summary>
        /// Toggles timestamp display on log entries
        /// </summary>
        public void _ToggleTimestamps()
        {
            ShowTimestamps = !ToggleTimestamps.isOn;

            ReloadLogs();
        }
        #endregion

        #region Log Entry Limit
        /// <summary>
        /// Applies the max log entry value from MaxLogEntriesField
        /// </summary>
        public void _ApplyMaxLogEntries()
        {
            int.TryParse(MaxLogEntriesField.text, out MaxLogEntries);

            SetMaxLogEntries(MaxLogEntries);
        }

        /// <summary>
        /// Decreases the maximum amount of log entries by 10
        /// </summary>
        public void _DecreaseMaxEntries()
        {
            SetMaxLogEntries(MaxLogEntries - MAX_ENTRY_ADJUSTMENT_STEP);
        }

        /// <summary>
        /// Increases the maximum amount of log entries by 10
        /// </summary>
        public void _IncreaseMaxEntries()
        {
            SetMaxLogEntries(MaxLogEntries + MAX_ENTRY_ADJUSTMENT_STEP);
        }

        /// <summary>
        /// Changes the maximum number of log entries based on the provided number
        /// </summary>
        /// <param name="maxEntries"></param>
        private void SetMaxLogEntries(int maxEntries)
        {
            MaxLogEntries = Mathf.Clamp(maxEntries, 0, ENTRIES_HARDCAP);

            MaxLogEntriesField.text = MaxLogEntries.ToString();

            ReloadLogs();
        }
        #endregion

        #region Font Size
        /// <summary>
        /// Applies the size of the log window font from FontSizeField
        /// </summary>
        public void _ApplyFontSize()
        {
            int.TryParse(FontSizeField.text, out FontSize);

            SetFontSize(FontSize);
        }

        /// <summary>
        /// Decreases the size of the log window font
        /// </summary>
        public void _DecreaseFontSize()
        {
            SetFontSize(--FontSize);
        }

        /// <summary>
        /// Increases the size of the log window font
        /// </summary>
        public void _IncreaseFontSize()
        {
            SetFontSize(++FontSize);
        }

        /// <summary>
        /// Changes the size of the log window font based on the provided number
        /// </summary>
        /// <param name="fontSize"></param>
        private void SetFontSize(int fontSize)
        {
            FontSize = Mathf.Clamp(fontSize, FONT_MIN_SIZE, FONT_MAX_SIZE);

            FontSizeField.text = FontSize.ToString();

            LogItem.GetComponentInChildren<Text>(true).fontSize = FontSize;

            foreach (Text text in LogWindow.GetComponentsInChildren<Text>(true))
            {
                text.fontSize = FontSize;
            }
        }
        #endregion
    }
}
