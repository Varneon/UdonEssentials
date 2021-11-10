#pragma warning disable IDE1006 // VRChat public method network execution prevention using underscore
#pragma warning disable 649

using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using UnityEngine.UI;
using System;

namespace Varneon.UdonPrefabs.RuntimeTools
{
    /// <summary>
    /// In-game console window for debugging UdonBehaviours
    /// </summary>
    [DefaultExecutionOrder(-2147483647)]
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class UdonConsole : UdonSharpBehaviour
    {
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

        private const string LOG_PREFIX = "[<color=#00FFFF>UdonConsole</color>]: ";

        private const int ENTRIES_HARDCAP = 1000;

        private Scrollbar scrollbar;

        private RectTransform canvasRoot;

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

                string timestamp = string.Join(" ", new string[] { info[1], info[2] });

                Text text = item.GetComponent<Text>();

                string textContent = text.text;

                bool hasTimestamp = textContent.StartsWith(timestamp);

                if (ShowTimestamps && !hasTimestamp) { text.text = string.Join(" ", new string[] { timestamp, textContent }); }
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
                (type == "LOG" && !ToggleLog.isOn) ||
                (type == "WARNING" && !ToggleWarning.isOn) ||
                (type == "ERROR" && !ToggleError.isOn)
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

            newEntryGO.name = string.Join(" ", new string[] { type, timestamp });
            textComponent = newEntry.GetComponent<Text>();

            string prefix = string.Empty;

            switch (type)
            {
                case "LOG":
                    prefix = "<color=#666666>LOG</color>:";
                    break;
                case "WARNING":
                    prefix = "<color=#FFFF00>WARNING</color>:";
                    break;
                case "ERROR":
                    prefix = "<color=#FF0000>ERROR</color>:";
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

            textComponent.text = string.Join(" ", message);

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

            WriteLine("LOG", message.ToString());
        }

        /// <summary>
        /// A variant of _Log that logs a warning message to the Udon Console
        /// </summary>
        /// <param name="message"></param>
        public void _LogWarning(object message)
        {
            if (ProxyEntriesToLogs) { Debug.LogWarning(message); }

            WriteLine("WARNING", message.ToString());
        }

        /// <summary>
        /// A variant of _Log that logs an error message to the Udon Console
        /// </summary>
        /// <param name="message"></param>
        public void _LogError(object message)
        {
            if (ProxyEntriesToLogs) { Debug.LogError(message); }

            WriteLine("ERROR", message.ToString());
        }
        #endregion

        #region Player Events
        public override void OnPlayerJoined(VRCPlayerApi player)
        {
            _Log($"{LOG_PREFIX} {player.displayName} Joined");
        }

        public override void OnPlayerLeft(VRCPlayerApi player)
        {
            if (!Utilities.IsValid(player)) { return; }

            _Log($"{LOG_PREFIX} {player.displayName} Left!");
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
            SetMaxLogEntries(MaxLogEntries - 10);
        }

        /// <summary>
        /// Increases the maximum amount of log entries by 10
        /// </summary>
        public void _IncreaseMaxEntries()
        {
            SetMaxLogEntries(MaxLogEntries + 10);
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
            SetFontSize(FontSize - 1);
        }

        /// <summary>
        /// Increases the size of the log window font
        /// </summary>
        public void _IncreaseFontSize()
        {
            SetFontSize(FontSize + 1);
        }

        /// <summary>
        /// Changes the size of the log window font based on the provided number
        /// </summary>
        /// <param name="fontSize"></param>
        private void SetFontSize(int fontSize)
        {
            FontSize = Mathf.Clamp(fontSize, 8, 32);

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
