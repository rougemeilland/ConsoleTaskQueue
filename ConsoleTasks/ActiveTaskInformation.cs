using System;
using System.Runtime.InteropServices;
using Palmtree;

namespace ConsoleTasks
{
    public readonly struct ActiveTaskInformation
    {
        [StructLayout(LayoutKind.Sequential)]
        internal struct Model
        {
            public TaskStatus Status;
            public int RunningServerId;
            public long RunningStartDateTimeTicks;
        }

        public ActiveTaskInformation(TaskStatus status, int runningServerId, DateTime runningStartDateTime)
        {
            if (runningStartDateTime.Kind == DateTimeKind.Unspecified)
                throw Validation.GetFailErrorException();

            Status = status;
            RunningServerId = runningServerId;
            RunningStartDateTime = runningStartDateTime.ToUniversalTime();
        }

        public TaskStatus Status { get; }
        public int RunningServerId { get; }
        public DateTime RunningStartDateTime { get; }

        internal Model ToModel()
        {
            Validation.Assert(RunningStartDateTime.Kind == DateTimeKind.Utc);

            return new()
            {
                Status = Status,
                RunningServerId = RunningServerId,
                RunningStartDateTimeTicks = RunningStartDateTime.Ticks,
            };
        }

        internal static ActiveTaskInformation FromModel(Model model)
            => new(
                model.Status,
                model.RunningServerId,
                new DateTime(model.RunningStartDateTimeTicks, DateTimeKind.Utc));
    }
}
