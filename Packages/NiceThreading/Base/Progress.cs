#pragma warning disable 414
using System;
using System.Collections.Generic;
using System.Linq;

namespace SpoiledCat.Threading
{
    public interface IProgress
    {
        IProgress UpdateProgress(long value, long total, string message = null, IProgress innerProgress = null,
            bool dontInvoke = false);
        ITask Task { get; }
        /// <summary>
        /// From 0 to 1
        /// </summary>
        float Percentage { get; }
        long Value { get; }
        long Total { get; }
        string Message { get; }
        bool Changed { get; }

        // if this is an aggregate progress reporter, this will have more data
        IProgress InnerProgress { get; }
        event Action<IProgress> OnProgress;
    }

    public class ProgressReporter
    {
        public event Action<IProgress> OnProgress;
        private Dictionary<ITask, IProgress> tasks = new Dictionary<ITask, IProgress>();
        private Progress progress = new Progress(TaskBase.Default);
        public string Message { get; set; }
        private long totalDone;
        private long valueDone;
        public void UpdateProgress(IProgress prog)
        {
            long total = 0;
            long value = 0;
            IProgress data;
            lock (tasks)
            {
                if (!tasks.ContainsKey(prog.Task))
                    tasks.Add(prog.Task, prog);
                else
                    tasks[prog.Task] = prog;

                if (prog.Percentage == 1f) {
                    tasks.Remove(prog.Task);
                    totalDone += prog.Total;
                    valueDone += prog.Value;
                }

                total = tasks.Values.Select(x => x.Total).Sum() + totalDone;
                value = tasks.Values.Select(x => x.Value).Sum() + valueDone;

                data = progress.UpdateProgress(value, total, Message, prog, true);
            }
            OnProgress?.Invoke(data);
        }
    }

    struct ProgressData : IProgress
    {
        public ITask Task { get; }
        public float Percentage { get; private set; }
        public long Value { get; private set; }
        public long Total { get; private set; }
        public string Message { get; private set; }
        public bool Changed { get; private set; }
        // if this is an aggregate progress reporter, this will have more data
        public IProgress InnerProgress { get; private set; }

        public event Action<IProgress> OnProgress;

        public ProgressData(ITask task, float percentage, long value, long total, string message,
            IProgress innerProgress)
        {
            this.Task = task;
            this.Percentage = percentage;
            this.Value = value;
            this.Total = total;
            this.Message = message;
            this.InnerProgress = innerProgress;
            this.OnProgress = null;
            this.Changed = true;
        }

        public IProgress UpdateProgress(long value, long total, string message = null,
            IProgress innerProgress = null,
            bool dontInvoke = false)
        {
            return this;
        }
    }

    public class Progress : IProgress
    {
        public ITask Task { get; }
        public float Percentage { get; private set; }
        public long Value { get; private set; }
        public long Total { get; set; }
        public string Message { get; private set; }
        public bool Changed { get; } = false;
        // if this is an aggregate progress reporter, this will have more data
        public IProgress InnerProgress { get; private set; }

        private long previousValue = -1;

        public event Action<IProgress> OnProgress;

        public Progress(ITask task)
        {
            this.Task = task;
        }

        public void UpdateProgress(IProgress progress)
        {
            UpdateProgress(progress.Value, progress.Total, progress.Message, progress.InnerProgress);
        }

        public IProgress UpdateProgress(long value, long total, string message = null, IProgress innerProgress = null,
            bool dontInvoke = false)
        {
            InnerProgress = innerProgress;
            Total = total == 0 ? 100 : total;
            Value = value > Total ? Total : value;
            var previousMessage = Message;
            Message = String.IsNullOrEmpty(message) ? Message : message;

            float fTotal = Total;
            float fValue = Value;
            Percentage = fValue / fTotal;
            var delta = (fValue / fTotal - previousValue / fTotal) * 100f;

            var shouldSignal = Value != previousValue && (
                (innerProgress?.Changed ?? false) ||
                previousMessage != Message ||
                fValue == 0f || delta > 1f || fValue == fTotal ||
                innerProgress?.Percentage == 1f
            );

            if (shouldSignal)
            { // signal progress in 1% increments or if we don't know what the total is
                previousValue = Value;

                var ret = new ProgressData(Task, Percentage, Value, Total, Message, innerProgress);
                if (!dontInvoke)
                    OnProgress?.Invoke(ret);
                return ret;
            }
            return this;
        }
    }
}
