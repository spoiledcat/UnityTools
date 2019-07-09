using System;

namespace SpoiledCat.Threading
{
    public partial class ActionTask : TaskBase
    {
        public ActionTask(Action action) : this(TaskManager.Instance.Token, action) {}
        public ActionTask(Action<bool> action) : this(TaskManager.Instance.Token, action) {}
        public ActionTask(Action<bool, Exception> action) : this(TaskManager.Instance.Token, action) {}
    }

    public partial class ActionTask<T> : TaskBase
    {
        /// <summary>
        /// </summary>
        /// <param name="action"></param>
        /// <param name="getPreviousResult">Method to call that returns the value that this task is going to work with. You can also use the PreviousResult property to set this value</param>
        public ActionTask(Action<bool, T> action, Func<T> getPreviousResult = null) : this(TaskManager.Instance.Token, action, getPreviousResult) {}

        /// <summary>
        /// </summary>
        /// <param name="action"></param>
        /// <param name="getPreviousResult">Method to call that returns the value that this task is going to work with. You can also use the PreviousResult property to set this value</param>
        public ActionTask(Action<bool, Exception, T> action, Func<T> getPreviousResult = null) {}
    }
}
