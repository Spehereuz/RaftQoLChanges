using System;
using UnityEngine;

namespace QoLChanges.Core
{
    internal static class ModLog
    {
        private const string Prefix = "[QoL Changes] ";

        public static void Info(string message)
        {
            Debug.Log(Prefix + message);
        }

        public static void Warning(string message)
        {
            Debug.LogWarning(Prefix + message);
        }

        public static void Error(string message)
        {
            Debug.LogError(Prefix + message);
        }

        public static void Error(string message, Exception exception)
        {
            Debug.LogError(Prefix + message + Environment.NewLine + exception);
        }
    }
}
