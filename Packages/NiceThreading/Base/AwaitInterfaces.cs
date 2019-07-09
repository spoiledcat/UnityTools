using System.Runtime.CompilerServices;

namespace SpoiledCat.Threading
{
    interface IAwaitable
    {
        IAwaiter GetAwaiter();
    }

    interface IAwaiter : INotifyCompletion
    {
        bool IsCompleted { get; }
        void GetResult();
    }
}