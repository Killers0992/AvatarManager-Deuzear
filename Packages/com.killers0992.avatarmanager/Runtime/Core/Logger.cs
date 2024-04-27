using UnityEngine;

namespace AvatarManager.Core
{
    public class Logger
    {
        public const string TagName = "AvatarManager";

        public static string Tag => $" <color=gray><b>[</b></color><color=orange>{TagName}</color><color=gray><b>]</b></color> ";

        public static void Info(object message) => Debug.Log($"{Tag}{message}");

        public static void Warn(object message) => Debug.LogWarning($"{Tag}{message}");

        public static void Error(object message) => Debug.LogError($"{Tag}{message}");
    }
}