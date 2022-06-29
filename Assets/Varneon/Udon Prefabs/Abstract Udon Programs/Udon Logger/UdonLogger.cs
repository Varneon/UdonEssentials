#pragma warning disable IDE1006

using UdonSharp;
using UnityEngine;

namespace Varneon.UdonPrefabs.Abstract
{
    /// <summary>
    /// Abstract logger for UdonBehaviours
    /// </summary>
    public abstract class UdonLogger : UdonSharpBehaviour
    {
        /// <summary>
        /// Logs a message
        /// </summary>
        /// <param name="message">String or object to be converted to string representation for display</param>
        public virtual void _Log(object message)
        {
            Log(LogType.Log, message);
        }

        /// <summary>
        /// Logs a message with context
        /// </summary>
        /// <param name="message">String or object to be converted to string representation for display</param>
        /// <param name="context">Object to which the message applies</param>
        public virtual void _Log(object message, Object context)
        {
            Log(LogType.Log, message, context);
        }

        /// <summary>
        /// Logs a formatted message
        /// </summary>
        /// <param name="format">A composite format string</param>
        /// <param name="args">Format arguments</param>
        public virtual void _LogFormat(string format, params object[] args)
        {
            LogFormat(LogType.Log, format, args);
        }

        /// <summary>
        /// Logs a formatted message
        /// </summary>
        /// <param name="context">Object to which the message applies</param>
        /// <param name="format">A composite format string</param>
        /// <param name="args">Format arguments</param>
        public virtual void _LogFormat(Object context, string format, params object[] args)
        {
            LogFormat(LogType.Log, context, format, args);
        }

        /// <summary>
        /// A variant of _Log that logs a warning message
        /// </summary>
        /// <param name="message">String or object to be converted to string representation for display</param>
        public virtual void _LogWarning(object message)
        {
            Log(LogType.Warning, message);
        }

        /// <summary>
        /// A variant of _Log that logs a warning message with context
        /// </summary>
        /// <param name="message">String or object to be converted to string representation for display</param>
        /// <param name="context">Object to which the message applies</param>
        public virtual void _LogWarning(object message, Object context)
        {
            Log(LogType.Warning, message, context);
        }

        /// <summary>
        /// Logs a formatted warning message
        /// </summary>
        /// <param name="format">A composite format string</param>
        /// <param name="args">Format arguments</param>
        public virtual void _LogWarningFormat(string format, params object[] args)
        {
            LogFormat(LogType.Warning, format, args);
        }

        /// <summary>
        /// Logs a formatted warning message
        /// </summary>
        /// <param name="context">Object to which the message applies</param>
        /// <param name="format">A composite format string</param>
        /// <param name="args">Format arguments</param>
        public virtual void _LogWarningFormat(Object context, string format, params object[] args)
        {
            LogFormat(LogType.Warning, context, format, args);
        }

        /// <summary>
        /// A variant of _Log that logs an error message
        /// </summary>
        /// <param name="message">String or object to be converted to string representation for display</param>
        public virtual void _LogError(object message)
        {
            Log(LogType.Error, message);
        }

        /// <summary>
        /// A variant of _Log that logs an error message with context
        /// </summary>
        /// <param name="message">String or object to be converted to string representation for display</param>
        /// <param name="context">Object to which the message applies</param>
        public virtual void _LogError(object message, Object context)
        {
            Log(LogType.Error, message, context);
        }

        /// <summary>
        /// Logs a formatted error message
        /// </summary>
        /// <param name="format">A composite format string</param>
        /// <param name="args">Format arguments</param>
        public virtual void _LogErrorFormat(string format, params object[] args)
        {
            LogFormat(LogType.Error, format, args);
        }

        /// <summary>
        /// Logs a formatted error message
        /// </summary>
        /// <param name="context">Object to which the message applies</param>
        /// <param name="format">A composite format string</param>
        /// <param name="args">Format arguments</param>
        public virtual void _LogErrorFormat(Object context, string format, params object[] args)
        {
            LogFormat(LogType.Error, context, format, args);
        }

        /// <summary>
        /// Assert a condition and logs an error message on failure
        /// </summary>
        /// <param name="condition">Condition you expect to be true</param>
        public virtual void _Assert(bool condition)
        {
            if (!condition)
            {
                Log(LogType.Assert, "Assertion failed");
            }
        }

        /// <summary>
        /// Assert a condition and logs an error message on failure
        /// </summary>
        /// <param name="condition">Condition you expect to be true</param>
        /// <param name="context">Object to which the message applies</param>
        public virtual void _Assert(bool condition, Object context)
        {
            if (!condition)
            {
                Log(LogType.Assert, "Assertion failed", context);
            }
        }

        /// <summary>
        /// Assert a condition and logs an error message on failure
        /// </summary>
        /// <param name="condition">Condition you expect to be true</param>
        /// <param name="message">String or object to be converted to string representation for display</param>
        public virtual void _Assert(bool condition, object message)
        {
            if (!condition)
            {
                Log(LogType.Assert, message);
            }
        }

        /// <summary>
        /// Assert a condition and logs an error message on failure
        /// </summary>
        /// <param name="condition">Condition you expect to be true</param>
        /// <param name="message">String or object to be converted to string representation for display</param>
        /// <param name="context">Object to which the message applies</param>
        public virtual void _Assert(bool condition, object message, Object context)
        {
            if (!condition)
            {
                Log(LogType.Assert, message, context);
            }
        }

        /// <summary>
        /// Clears the logs
        /// </summary>
        public virtual void _ClearLogs() { }

        /// <summary>
        /// Clears the logs
        /// </summary>
        /// <param name="logType">Type of message e.g. warn or error etc</param>
        public virtual void _ClearLogs(LogType logType) { }

        /// <summary>
        /// Converts the message object to string and handles null reference
        /// </summary>
        /// <param name="message">String or object to be converted to string representation for display</param>
        /// <returns>Message object's string representation</returns>
        protected virtual string MessageObjectToString(object message)
        {
            if (message == null) { return "null"; }

            return message.ToString();
        }

        /// <summary>
        /// Formats the context object to string and handles null reference
        /// </summary>
        /// <param name="context">Object to which the message applies</param>
        /// <returns>Context object's string representation</returns>
        protected virtual string ContextObjectToString(Object context)
        {
            if(context == null) { return "<color=grey>(<color=silver>null</color>)</color>"; }

            return string.Format("<color=grey>(<color=silver>{0}</color>, <color=silver>{1}</color>)</color>", context.name, context.GetInstanceID());
        }

        /// <summary>
        /// Get the default log entry prefix for provided log type
        /// </summary>
        /// <param name="logType">Type of message e.g. warn or error etc</param>
        /// <returns>Default log entry prefix of LogType</returns>
        protected virtual string GetLogTypePrefix(LogType logType)
        {
            switch (logType)
            {
                case LogType.Error:
                    return "<color=#FF0000>ERROR</color>:";
                case LogType.Assert:
                    return "<color=#FF0000>ASSERT</color>:";
                case LogType.Warning:
                    return "<color=#FFFF00>WARNING</color>:";
                case LogType.Log:
                    return "<color=#666666>LOG</color>:";
                case LogType.Exception:
                    return "<color=#FF0000>EXCEPTION</color>:";
                default:
                    return string.Empty;
            }
        }

        /// <summary>
        /// Logs message
        /// </summary>
        /// <param name="logType">Type of message e.g. warn or error etc</param>
        /// <param name="message">String or object to be converted to string representation for display</param>
        protected virtual void Log(LogType logType, object message) { }

        /// <summary>
        /// Logs message
        /// </summary>
        /// <param name="logType">Type of message e.g. warn or error etc</param>
        /// <param name="message">String or object to be converted to string representation for display</param>
        /// <param name="context">Object to which the message applies</param>
        protected virtual void Log(LogType logType, object message, Object context) { }

        /// <summary>
        /// Logs a formatted message
        /// </summary>
        /// <param name="logType">Type of message e.g. warn or error etc</param>
        /// <param name="format">A composite format string</param>
        /// <param name="args">Format arguments</param>
        protected virtual void LogFormat(LogType logType, string format, params object[] args) { }

        /// <summary>
        /// Logs a formatted message
        /// </summary>
        /// <param name="logType">Type of message e.g. warn or error etc</param>
        /// <param name="context">Object to which the message applies</param>
        /// <param name="format">A composite format string</param>
        /// <param name="args">Format arguments</param>
        protected virtual void LogFormat(LogType logType, Object context, string format, params object[] args) { }
    }
}
