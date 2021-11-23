
#pragma warning disable IDE0044 // Making serialized fields readonly hides them from the inspector
#pragma warning disable IDE1006 // VRChat public method network execution prevention using underscore
#pragma warning disable 649

using System;
using UdonSharp;
using UnityEngine;
using UnityEngine.UI;
using VRC.SDKBase;

namespace Varneon.UdonPrefabs.RuntimeTools
{
    /// <summary>
    /// In-game console window for debugging UdonBehaviours
    /// </summary>
    [DefaultExecutionOrder(-2147483647)]
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class UdonConsole : UdonSharpBehaviour
    {
        #region Variables

        #region Serialized
        [Header("Settings")]
        [SerializeField]
        private bool ShowTimestamps = false;

        [SerializeField]
        private int MaxLogEntries = 100;

        [SerializeField, Range(8, 32)]
        private int FontSize = 24;

        [SerializeField]
        private bool ProxyEntriesToLogs;

        [Space]
        [Header("References")]
        [SerializeField]
        private RectTransform LogWindow;

        [SerializeField]
        private GameObject LogItem;

        [SerializeField]
        private Toggle ToggleLog, ToggleWarning, ToggleError, ToggleTimestamps;

        [SerializeField]
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

        private const string
            LOGTYPE_LOG = "LOG",
            LOGTYPE_WARNING = "WARNING",
            LOGTYPE_ERROR = "ERROR";

        private const string
            LOGTYPE_LOG_PREFIX = "<color=#666666>LOG</color>:",
            LOGTYPE_WARNING_PREFIX = "<color=#FFFF00>WARNING</color>:",
            LOGTYPE_ERROR_PREFIX = "<color=#FF0000>ERROR</color>:";

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

                string type = info[0];

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
        private void SetLogEntryActive(GameObject logEntry, string type)
        {
            logEntry.SetActive(
                (type == LOGTYPE_LOG && !ToggleLog.isOn) ||
                (type == LOGTYPE_WARNING && !ToggleWarning.isOn) ||
                (type == LOGTYPE_ERROR && !ToggleError.isOn)
                );
        }

        /// <summary>
        /// Write line to the console
        /// </summary>
        /// <param name="type"></param>
        /// <param name="text"></param>
        private void WriteLine(string type, string text)
        {
            Text textComponent;

            Transform newEntry;

            string timestamp = DateTime.UtcNow.ToLocalTime().ToString("yyyy.MM.dd HH:mm:ss");

            if (GetCurrentLogEntryCount() < MaxLogEntries)
            {
                newEntry = VRCInstantiate(LogItem).transform;
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

            newEntryGO.name = string.Join(WHITESPACE, new string[] { type, timestamp });
            textComponent = newEntry.GetComponent<Text>();

            string prefix = string.Empty;

            switch (type)
            {
                case LOGTYPE_LOG:
                    prefix = LOGTYPE_LOG_PREFIX;
                    break;
                case LOGTYPE_WARNING:
                    prefix = LOGTYPE_WARNING_PREFIX;
                    break;
                case LOGTYPE_ERROR:
                    prefix = LOGTYPE_ERROR_PREFIX;
                    break;
            }

            string[] message = ShowTimestamps ?
                new string[]
                {
                    timestamp,
                    prefix,
                    text
                } :
                new string[]
                {
                    prefix,
                    text
                };

            textComponent.text = string.Join(WHITESPACE, message);

            SetLogEntryActive(newEntryGO, type);

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

        #region Logging Methods
        /// <summary>
        /// Log a message to the Udon Console
        /// </summary>
        /// <param name="message"></param>
        public void _Log(object message)
        {
            if(ProxyEntriesToLogs) { Debug.Log(message); }

            WriteLine(LOGTYPE_LOG, message.ToString());
        }

        /// <summary>
        /// A variant of _Log that logs a warning message to the Udon Console
        /// </summary>
        /// <param name="message"></param>
        public void _LogWarning(object message)
        {
            if (ProxyEntriesToLogs) { Debug.LogWarning(message); }

            WriteLine(LOGTYPE_WARNING, message.ToString());
        }

        /// <summary>
        /// A variant of _Log that logs an error message to the Udon Console
        /// </summary>
        /// <param name="message"></param>
        public void _LogError(object message)
        {
            if (ProxyEntriesToLogs) { Debug.LogError(message); }

            WriteLine(LOGTYPE_ERROR, message.ToString());
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
