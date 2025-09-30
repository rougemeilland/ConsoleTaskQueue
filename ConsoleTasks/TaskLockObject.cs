using System;
using System.Threading;

namespace ConsoleTasks
{
    internal sealed class TaskLockObject
        : IDisposable
    {
        private readonly Mutex _lockObject;
        private bool _isDisposed;

        public TaskLockObject(Mutex lockObject)
        {
            _lockObject = lockObject;
        }

        public static TaskLockObject? Lock(string taskId)
        {
            var success = false;
            var lockObject = (Mutex?)null;
            try
            {
                lockObject = CreateLockObject(taskId);
                while (true)
                {
                    try
                    {
                        if (!lockObject.TryLockMutex())
                            return null;
                        break;
                    }
                    catch (AbandonedMutexException)
                    {
                    }
                }

                var taskLockObject = new TaskLockObject(lockObject);
                success = true;
                return taskLockObject;
            }
            catch (Exception)
            {
                return null;
            }
            finally
            {
                if (!success)
                {
                    try
                    {
                        lockObject?.UnlockMutex();
                    }
                    catch (Exception)
                    {
                    }

                    lockObject?.Dispose();
                }
            }
        }

        public void Unlock()
            => Dispose();

        public static bool IsLocked(string taskId)
        {
            var lockObject = (Mutex?)null;
            try
            {
                lockObject = CreateLockObject(taskId);
                return !lockObject.TryLockMutex();
            }
            catch (Exception)
            {
                return false;
            }
            finally
            {
                try
                {
                    lockObject?.UnlockMutex();
                }
                catch (Exception)
                {
                }

                lockObject?.Dispose();
            }
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        private static Mutex CreateLockObject(string taskId)
            => new(false, $"{Constants.InterProcessResourceNamePrefix}.lockTask.{taskId}");

        private void Dispose(bool disposing)
        {
            if (!_isDisposed)
            {
                if (disposing)
                {
                    _lockObject.UnlockMutex();
                    _lockObject.Dispose();
                }

                _isDisposed = true;
            }
        }
    }
}
