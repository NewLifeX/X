using System;

namespace NewLife.Queue.Utilities
{
    public static class Helper
    {
        public static void EatException(Action action)
        {
            try
            {
                action();
            }
            catch
            {
                // ignored
            }
        }
        public static T EatException<T>(Func<T> action, T defaultValue = default(T))
        {
            try
            {
                return action();
            }
            catch (Exception)
            {
                return defaultValue;
            }
        }
    }
}
