using SpoiledCat.Threading;
using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.Serialization;

namespace SpoiledCat.UI
{
    [Serializable]
    public abstract class BaseView : IView
    {
        private const string NullParentError = "Subview parent is null";

        public BaseView()
        {
        }

        public virtual void InitializeView(IView parent)
        {
            Debug.Assert(parent != null, NullParentError);
            Parent = parent;
        }

        public void Initialize(bool firstRun) { }

        public virtual void Awake() { }

        public virtual void OnEnable() { }

        public virtual void OnDisable() { }

        public virtual void OnDataUpdate() { }

        public virtual void Update() { }

        public virtual void OnFirstRepaint() { }

        public virtual void OnUI() { }


        public virtual void OnGUI() { }

        public virtual void OnSelectionChange() { }

        public virtual void OnFocusChanged() { }

        public virtual void Refresh() { }

        public virtual void OnDestroy() { }

        public virtual void Redraw() => Parent.Redraw();
        public void Repaint() => Redraw();

        public virtual void Finish(bool result) => Parent.Finish(result);


        protected IView Parent { get; private set; }

        protected EditorWindow Window
        {
            get
            {
                IView p = this;
                while (p is BaseView v) p = v.Parent;
                return (EditorWindow)p;
            }
        }

        public ITaskManager TaskManager => Parent.TaskManager;

        public bool HasFocus => Parent.HasFocus;
        public bool NeedsRefresh
        {
            get => Parent.NeedsRefresh;
            set => Parent.NeedsRefresh = value;
        }

        public virtual bool IsBusy => Parent.IsBusy;

        private Rect? _position;

        public Rect Position
        {
            get => _position.HasValue ? _position.Value : Parent.Position;
            set => _position = value;
        }

        public Rect position => Position;
        [field: SerializeField, FormerlySerializedAs("isActive")] public bool IsActive { get; set; }

        [field: SerializeField, FormerlySerializedAs("title")] public string Title { get; protected set; }
        [field: SerializeField, FormerlySerializedAs("size")] public Vector2 Size { get; protected set; }
    }
}