using SpoiledCat.Threading;
using UnityEngine;

namespace SpoiledCat.UI
{
    public interface IView
    {
        void Initialize(bool firstRun);
        void Awake();
        void OnEnable();
        void OnDisable();
        void Refresh();
        void Redraw();
        void Finish(bool result);
        void Update();
        void OnFirstRepaint();
        void OnDataUpdate();
        void OnUI();
        void OnFocusChanged();
        void OnDestroy();
        void OnSelectionChange();

        Rect Position { get; }

        bool IsBusy { get; }
        bool HasFocus { get; }
        bool NeedsRefresh { get; set; }
        bool IsActive { get; set; }
        ITaskManager TaskManager { get; }
    }
}