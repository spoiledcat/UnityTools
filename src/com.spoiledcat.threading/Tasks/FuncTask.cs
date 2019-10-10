// Copyright 2016-2019 Andreia Gaita
//
// This work is licensed under the terms of the MIT license.
// For a copy, see <https://opensource.org/licenses/MIT>.

using System;
using System.Collections.Generic;

namespace SpoiledCat.Threading
{
    public partial class FuncTask<T> : TaskBase<T>
    {
        public FuncTask(Func<T> action) : this(TaskManager.Instance.Token, action) {}
        public FuncTask(Func<bool, T> action) : this(TaskManager.Instance.Token, action) {}
        public FuncTask(Func<bool, Exception, T> action) : this(TaskManager.Instance.Token, action) {}
    }

    public partial class FuncTask<T, TResult> : TaskBase<T, TResult>
    {
        public FuncTask(Func<bool, T, TResult> action, Func<T> getPreviousResult = null)
            : this(TaskManager.Instance.Token, action, getPreviousResult)
        {}

        public FuncTask(Func<bool, Exception, T, TResult> action, Func<T> getPreviousResult = null)
            : this(TaskManager.Instance.Token, action, getPreviousResult)
        {}
    }

    public partial class FuncListTask<T> : DataTaskBase<T, List<T>>
    {
        public FuncListTask(Func<bool, List<T>> action)
            : this(TaskManager.Instance.Token, action)
        {}

        public FuncListTask(Func<bool, Exception, List<T>> action)
            : this(TaskManager.Instance.Token, action)
        {}

        public FuncListTask(Func<bool, FuncListTask<T>, List<T>> action)
            : this(TaskManager.Instance.Token, action)
        {}
    }

    public partial class FuncListTask<T, TData, TResult> : DataTaskBase<T, TData, List<TResult>>
    {
        public FuncListTask(Func<bool, T, List<TResult>> action)
            : this(TaskManager.Instance.Token, action)
        {}

        public FuncListTask(Func<bool, Exception, T, List<TResult>> action)
            : this(TaskManager.Instance.Token, action)
        {}
    }
}
