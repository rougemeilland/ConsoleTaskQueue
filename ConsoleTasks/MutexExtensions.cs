using System.Threading;

namespace ConsoleTasks
{
    internal static class MutexExtensions
    {
        public static bool TryLockMutex(this Mutex lockObject)
        {
            while (true)
            {
                try
                {
                    return lockObject.WaitOne(0);
                }
                catch (AbandonedMutexException)
                {
                }
            }
        }

        public static void LockMutex(this Mutex lockObject)
        {
            while (true)
            {
                try
                {
                    _ = lockObject.WaitOne();
                    return;
                }
                catch (AbandonedMutexException)
                {
                }
            }
        }

        public static void UnlockMutex(this Mutex lockObject)
            => lockObject.ReleaseMutex();
    }
}
