// Copyright 2016-2019 Andreia Gaita
//
// This work is licensed under the terms of the MIT license.
// For a copy, see <https://opensource.org/licenses/MIT>.

using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace SpoiledCat.UI
{
    using System.Linq;
    using Threading;

    public abstract class BaseWindow : EditorWindow, IView
    {
        [NonSerialized] private bool firstRepaint = true;
        [NonSerialized] private bool initializeCalled;
        [NonSerialized] private bool awakeCalled;

        private List<BaseView> views;
        public IEnumerable<IView> Views => views == null ? views = new List<BaseView>() : views;

        protected BaseWindow()
        {
        }

        public static T GetWindowDontShow<T>() where T : EditorWindow
        {
            UnityEngine.Object[] windows = Resources.FindObjectsOfTypeAll(typeof(T));
            return (windows.Length > 0) ? (T)windows[0] : CreateInstance<T>();
        }

        public virtual void Initialize(bool firstRun)
        {
            foreach (var view in Views)
                view.Initialize(firstRun);
        }

        public virtual void Redraw()
        {
            Repaint();
        }

        public virtual void Refresh()
        {
            NeedsRefresh = true;
        }

        public virtual void Finish(bool result)
        {
            foreach (var view in Views)
                view.Finish(result);
        }

        public virtual void Awake()
        {
            awakeCalled = true;
            InitializeWindow(false);
        }

        public virtual void OnEnable()
        {
            InitializeWindow(false);
        }

        public virtual void OnDisable()
        {
            foreach (var view in Views.Where(x => x.IsActive))
                view.OnDisable();
        }

        public virtual void Update()
        {
            foreach (var view in Views.Where(x => x.IsActive))
                view.Update();
        }

        public virtual void OnFirstRepaint()
        {
        }

        private void InternalOnFirstRepaint()
        {
            OnFirstRepaint();
            foreach (var view in Views.Where(x => x.IsActive))
                view.OnFirstRepaint();
        }

        public virtual void OnDataUpdate()
        {
        }

        private void InternalOnDataUpdate()
        {
            OnDataUpdate();
            foreach (var view in Views.Where(x => x.IsActive))
                view.OnDataUpdate();
        }

        // OnGUI calls this everytime, so override it to render as you would OnGUI
        public virtual void OnUI()
        {
            foreach (var view in Views.Where(x => x.IsActive))
                view.OnUI();
        }

        public virtual void OnFocusChanged()
        {
            foreach (var view in Views.Where(x => x.IsActive))
                view.OnFocusChanged();
        }

        public virtual void OnDestroy()
        {
            foreach (var view in Views)
                view.OnDestroy();
        }

        public virtual void OnSelectionChange()
        {
            foreach (var view in Views.Where(x => x.IsActive))
                view.OnSelectionChange();
        }

        public void AddView(IView view)
        {
            ((List<BaseView>)Views).Add((BaseView)view);
            if (initializeCalled)
                view.Initialize(ApplicationState.Instance.FirstRun);
            if (awakeCalled)
                view.Awake();
        }

        public void RemoveView(IView view)
        {
            ((List<BaseView>)Views).Remove((BaseView)view);
        }

        public void ActivateView(IView view)
        {
            view.IsActive = true;
            view.OnEnable();
        }

        public void DeactivateView(IView view)
        {
            view.OnDisable();
            view.IsActive = false;
        }

        private void InitializeWindow(bool requiresRedraw = true)
        {
            initializeCalled = true;
            if (!ApplicationState.Instance.Initialized)
            {
                ApplicationState.Instance.Initialized = true;
                InternalInitialize();
                if (requiresRedraw)
                    Redraw();
            }
        }

        private void InternalInitialize()
        {
            Initialize(ApplicationState.Instance.FirstRun);
            if (TaskManager == null)
            {
                TaskManager = new TaskManager();
                TaskManager.Initialize();
            }
        }

        // This is Unity's magic method
        private void OnGUI()
        {
            if (Event.current.type == EventType.Layout)
            {
                InLayout = true;
                InternalOnDataUpdate();
                NeedsRefresh = false;
                if (firstRepaint)
                {
                    InternalOnFirstRepaint();
                    firstRepaint = false;
                }
            }

            OnUI();

            if (Event.current.type == EventType.Layout)
            {
                InLayout = false;
            }
        }

        public virtual void OnFocus()
        {
            HasFocus = true;
            OnFocusChanged();
        }

        public virtual void OnLostFocus()
        {
            HasFocus = false;
            OnFocusChanged();
        }

        protected void SetPosition(Rect? pos) => _position = pos;

        private Rect? _position;
        public Rect Position => _position.HasValue ? _position.Value : position;

        public bool IsBusy { get; set; }
        public bool HasFocus { get; private set; }
        protected bool InLayout { get; private set; }
     
        protected bool InRepaint => Event.current.type == EventType.Repaint;

        public bool NeedsRefresh { get; set; }

        public ITaskManager TaskManager { get; set; }

        public bool IsActive
        {
            get => true;
            set
            { }
        }
    }
}
