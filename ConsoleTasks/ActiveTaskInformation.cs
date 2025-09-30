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
            public long RunningStartDateTimeTicks;
        }

        public ActiveTaskInformation(TaskStatus status, DateTime runningStartDateTime)
        {
            if (runningStartDateTime.Kind == DateTimeKind.Unspecified)
                throw Validation.GetFailErrorException();

            Status = status;
            RunningStartDateTime = runningStartDateTime.ToUniversalTime();
        }

        public TaskStatus Status { get; }
        public DateTime RunningStartDateTime { get; }

        internal Model ToModel()
        {
            Validation.Assert(RunningStartDateTime.Kind == DateTimeKind.Utc);

            return new()
            {
                Status = Status,
                RunningStartDateTimeTicks = RunningStartDateTime.Ticks,
            };
        }

        internal static ActiveTaskInformation FromModel(Model model)
            => new(
                model.Status,
                new DateTime(model.RunningStartDateTimeTicks, DateTimeKind.Utc));
    }
}
