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
        internal static readonly string LockObjectNamePrefix = $"{typeof(ConsoleTaskQueue).FullName}-{{127006A4-719F-4ED8-AA99-F5F3828E55F9}}";

        private static readonly TimeSpan _pollingInterval = TimeSpan.FromSeconds(1);

        private readonly Mutex _lockObject;
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
            _lockObject = new Mutex(false, $"{LockObjectNamePrefix}.queue");
            _isDisposed = false;
        }

        public static DirectoryPath BaseDirectory { get; }

        public void Enqueue(ConsoleTask task)
        {
            _lockObject.LockMutex();
            try
            {
                for (var count = 0; ; ++count)
                {
                    var taskFileName = $"{Environment.TickCount64}-{count}.json";
                    var taskFile = BaseDirectory.GetFile(taskFileName);
                    if (!taskFile.Exists)
                    {
                        taskFile.WriteAllText(task.Serialize());
                        return;
                    }
                }
            }
            finally
            {
                _lockObject.UnlockMutex();
            }
        }

        public IEnumerable<ConsoleTask> EnumerateConsoleTasks()
        {
            _lockObject.LockMutex();
            var tasks = new List<ConsoleTask>();
            try
            {
                foreach (var (_, jsonText) in EnumerateConsoleTaskFiles())
                {
                    var task = ConsoleTask.Deserialize(jsonText);
                    if (task is not null)
                        tasks.Add(task);
                }

                return tasks;
            }
            finally
            {
                _lockObject.UnlockMutex();
            }
        }

        public void DequeueAndExecute(Encoding encoding, Action waiting, Action<FilePath> starting, Action<FilePath> ending)
        {
            var taskState = Dequeue(encoding, waiting);
            try
            {
                starting(taskState.CommandFile);
                taskState.Execute();
                ending(taskState.CommandFile);
            }
            finally
            {
                _lockObject.LockMutex();
                try
                {
                    taskState.Complete();
                }
                finally
                {
                    _lockObject.UnlockMutex();
                }
            }
        }

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
                    var task = ConsoleTask.Deserialize(item.jsonText);
                    if (task is null)
                        return null;
                    var lockObject = TaskLockObject.Lock(item.taskFile);
                    if (lockObject is null)
                        return null;
                    return ConsoleTaskState.CreateInstance(task, item.taskFile, lockObject, encoding);
                })
                .WhereNotNull()
                .FirstOrDefault();

        private static IEnumerable<(FilePath taskFile, string jsonText)> EnumerateConsoleTaskFiles()
            => BaseDirectory.EnumerateFiles()
                .Where(file => string.Equals(file.Extension, ".json", StringComparison.OrdinalIgnoreCase))
                .Select(file =>
                {
                    try
                    {
                        return (file, file.ReadAllText());
                    }
                    catch (IOException)
                    {
                        return ((FilePath taskFile, string jsonText)?)null;
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
                    _lockObject.Dispose();
                }

                _isDisposed = true;
            }
        }
    }
}
