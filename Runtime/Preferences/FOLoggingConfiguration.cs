using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace FloatingOffset.Runtime
{
    /// <summary>
    /// The FOLoggingConfiguration is used to log warnings and messages conditionally, depending on the configuration set by the user.
    /// </summary>
    public class FOLoggingConfiguration : ScriptableObject
    {
        [field: SerializeField] public LogLevel LoggingLevel { get; private set; }
        /// <summary>
        /// Logs a new warning. Only logs if LoggingLevel == Warnings || LoggingLevel == Full.
        /// </summary>
        /// <param name="message">The message to log.</param>
        public void LogWarning(object message)
        {
            if (LoggingLevel == LogLevel.None)
                return;

            Debug.LogWarning(message);
        }
        /// <summary>
        /// Logs a new message.  Only logs if LoggingLevel == Full.
        /// </summary>
        /// <param name="message">The message to log.</param>
        public void Log(object message)
        {
            if (LoggingLevel != LogLevel.Full)
                return;

            Debug.Log(message);
        }
    }
    /// <summary>
    /// LogLevel is used to define what gets logged by FloatingOffset's internal logger.
    /// </summary>
    public enum LogLevel : byte
    {
        /// <summary>
        /// Log nothing.
        /// </summary>
        None,
        /// <summary>
        /// Log only warnings.
        /// </summary>
        Warnings,
        /// <summary>
        /// Log everything.
        /// </summary>
        Full
    }
}
