using System;

namespace OutpostZero.Core
{
    public static class GameplayFeedback
    {
        public static event Action<string> OnToast;

        public static void Toast(string message)
        {
            if (!string.IsNullOrEmpty(message))
            {
                OnToast?.Invoke(message);
            }
        }
    }
}
