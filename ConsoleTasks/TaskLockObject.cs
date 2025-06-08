using System;
using System.Threading;
using Palmtree.IO;

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

        public static TaskLockObject? Lock(FilePath taskFile)
        {
            var success = false;
            var lockObject = (Mutex?)null;
            try
            {
                lockObject = new Mutex(false, $"{ConsoleTaskQueue.LockObjectNamePrefix}.task.{taskFile.NameWithoutExtension}");
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

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

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
