using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using Palmtree.IO;
using Palmtree.Linq;

namespace ConsoleTasks
{
    public class ConsoleTaskQueue
        : IDisposable
    {
        private static readonly TimeSpan _pollingInterval = TimeSpan.FromSeconds(1);

        private readonly Mutex _lockObject;
        private readonly EventWaitHandle _stopEventObject;
        private bool _isDisposed;

        static ConsoleTaskQueue()
        {
            BaseDirectory =
                new DirectoryPath(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile))
                .GetSubDirectory(".palmtree").Create()
                .GetSubDirectory("task_queue").Create();
        }

        public ConsoleTaskQueue()
        {
            _lockObject = new Mutex(false, $"{Constants.InterProcessResourceNamePrefix}.lockQueue");
            _stopEventObject = new EventWaitHandle(false, EventResetMode.ManualReset, $"{Constants.InterProcessResourceNamePrefix}.stopServer");
            _isDisposed = false;
        }

        public static DirectoryPath BaseDirectory { get; }

        public void Enqueue(ConsoleTask task)
        {
            _lockObject.LockMutex();
            try
            {
                var now = DateTime.UtcNow;
                var taskFile = BaseDirectory.CreateUniqueFile(prefix: $"{now.Year:d4}-{now.Month:d2}-{now.Day:d2}T{now.Hour:d2}{now.Minute:d2}{now.Second:d2}.{now.Millisecond:d3}-", suffix: ".json");
                taskFile.WriteAllText(task.Serialize());
            }
            finally
            {
                _lockObject.UnlockMutex();
            }
        }

        public IEnumerable<ConsoleTask> EnumerateConsoleTasks()
        {
            _lockObject.LockMutex();
            var result = new List<ConsoleTask>();
            try
            {
                return
                    EnumerateTasks()
                    .OrderBy(item => item.taskFile.LastWriteTimeUtc)
                    .ThenBy(item => item.task.RegisteredDateTime)
                    .Select(item => item.task)
                    .ToList();
            }
            finally
            {
                _lockObject.UnlockMutex();
            }

            static IEnumerable<(ConsoleTask task, FilePath taskFile)> EnumerateTasks()
            {
                foreach (var (taskFile, jsonText) in EnumerateConsoleTaskFiles())
                {
                    var task = ConsoleTask.Deserialize(taskFile, jsonText);
                    if (task is not null)
                        yield return (task, taskFile);
                }
            }
        }

        public void StopAllServers() => _stopEventObject.Set();

        public void DequeueAndExecute(Encoding encoding, Action waiting, Action<FilePath> starting, Action<FilePath> ending)
        {
            if (_stopEventObject.WaitOne(0))
                throw new OperationCanceledException();
            var taskState = Dequeue(encoding, waiting);
            var taskSharedMemoryHandle = (SharedMemoryHandle<ActiveTaskInformation.Model>?)null;
            try
            {
                if (OperatingSystem.IsWindows())
                {
                    taskSharedMemoryHandle = SharedMemoryHandle<ActiveTaskInformation.Model>.CreateNew(GetTaskResourceName(taskState.TaskId));
                    taskSharedMemoryHandle.Value = new ActiveTaskInformation(TaskStatus.Running, DateTime.UtcNow).ToModel();
                }

                starting(taskState.CommandFile);
                taskState.Execute();
                ending(taskState.CommandFile);
            }
            finally
            {
                _lockObject.LockMutex();
                try
                {
                    try
                    {
                        taskState.Complete();
                    }
                    catch (Exception)
                    {
                    }

                    try
                    {
                        if (taskSharedMemoryHandle is not null)
                            taskSharedMemoryHandle.Value = new ActiveTaskInformation(TaskStatus.Completed, Constants.NotAvailableDateTime).ToModel();
                    }
                    catch (Exception)
                    {
                    }

                    try
                    {
                        taskSharedMemoryHandle?.Dispose();
                    }
                    catch (Exception)
                    {
                    }
                }
                finally
                {
                    _lockObject.UnlockMutex();
                }
            }
        }

        internal static ActiveTaskInformation? GetAdditionalTaskInfo(string taskId)
        {
            if (!OperatingSystem.IsWindows())
                return null;
            var taskSharedMemoryHandle = (SharedMemoryHandle<ActiveTaskInformation.Model>?)null;
            try
            {
                taskSharedMemoryHandle = SharedMemoryHandle<ActiveTaskInformation.Model>.OpenExisting(GetTaskResourceName(taskId));
                return ActiveTaskInformation.FromModel(taskSharedMemoryHandle.Value);
            }
            catch (Exception)
            {
                return null;
            }
            finally
            {
                taskSharedMemoryHandle?.Dispose();
            }
        }

        private static string GetTaskResourceName(string taskId) => $".taskInfo.{taskId}";

        private ConsoleTaskState Dequeue(Encoding encoding, Action waiting)
        {
            while (true)
            {
                _lockObject.LockMutex();
                try
                {
                    var taskState = FindTask(encoding);
                    if (taskState is not null)
                        return taskState;
                }
                finally
                {
                    _lockObject.UnlockMutex();
                }

                waiting();

                Thread.Sleep(_pollingInterval);
            }
        }

        private static ConsoleTaskState? FindTask(Encoding encoding)
            => EnumerateConsoleTaskFiles()
                .Select(item =>
                {
                    var task = ConsoleTask.Deserialize(item.taskFile, item.jsonText);
                    if (task is null)
                        return null;
                    var lockObject = TaskLockObject.Lock(item.taskFile.NameWithoutExtension.ToUpperInvariant());
                    if (lockObject is null)
                        return null;
                    return ConsoleTaskState.CreateInstance(task, item.taskFile, lockObject, encoding);
                })
                .WhereNotNull()
                .FirstOrDefault();

        private static IEnumerable<(FilePath taskFile, string jsonText)> EnumerateConsoleTaskFiles()
            => BaseDirectory.EnumerateFiles()
                .Select(taskFile => (taskFile, dateTime: taskFile.LastWriteTimeUtc))
                .Where(item => string.Equals(item.taskFile.Extension, ".json", StringComparison.OrdinalIgnoreCase))
                .OrderBy(item => item.taskFile.LastWriteTimeUtc)
                .Select(item =>
                {
                    try
                    {
                        return (item.taskFile, item.taskFile.ReadAllText());
                    }
                    catch (IOException)
                    {
                        return ((FilePath taskFile, string jsonTex)?)null;
                    }
                })
                .WhereNotNull();

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_isDisposed)
            {
                if (disposing)
                {
                    _stopEventObject.Dispose();
                    _lockObject.Dispose();
                }

                _isDisposed = true;
            }
        }
    }
}
