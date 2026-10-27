using System;
using UnityEngine;

namespace QoLDevTools
{
    internal static class DevLog
    {
        private const string Prefix = "[QoL Dev Tools] ";

        public static void Info(string message)
        {
            Debug.Log(Prefix + message);
        }

        public static void Error(string message, Exception exception)
        {
            Debug.LogError(Prefix + message + Environment.NewLine + exception);
        }
    }
}
