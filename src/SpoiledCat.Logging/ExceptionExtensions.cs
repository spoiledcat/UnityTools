// Copyright 2016-2019 Andreia Gaita
//
// This work is licensed under the terms of the MIT license.
// For a copy, see <https://opensource.org/licenses/MIT>.

using System;
using System.Linq;

namespace SpoiledCat.Extensions
{
    public static class ExceptionExtensions
    {
        public static string GetExceptionMessage(this Exception ex)
        {
            var message = ex.ToString();
            var inner = ex.InnerException;
            while (inner != null)
            {
                message += Environment.NewLine + inner.ToString();
                inner = inner.InnerException;
            }
            var caller = Environment.StackTrace;
            var stack = caller.Split(new string[] { Environment.NewLine }, StringSplitOptions.None);
            message += Environment.NewLine + "=======";
            message += Environment.NewLine + String.Join(Environment.NewLine, stack.Skip(1).SkipWhile(x => x.Contains("Git.Logging")).ToArray());
            return message;
        }

        public static string GetExceptionMessageShort(this Exception ex)
        {
            var message = ex.ToString();
            var inner = ex.InnerException;
            while (inner != null)
            {
                message += Environment.NewLine + inner.ToString();
                inner = inner.InnerException;
            }
            return message;
        }
    }
}
