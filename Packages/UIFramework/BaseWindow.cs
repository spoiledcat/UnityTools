﻿using System;
using System.Globalization;
using UnityEditor;
using UnityEngine;

namespace SpoiledCat.UI
{
    class ApplicationState : ScriptableSingleton<ApplicationState>
    {
        [SerializeField] private bool firstRun = true;
        [SerializeField] public string firstRunAtString;
        [SerializeField] private bool initialized = false;

        [NonSerialized] private Guid? instanceId;
        [NonSerialized] private bool? firstRunValue;
        [NonSerialized] public DateTimeOffset? firstRunAtValue;

        public static ApplicationState Instance => instance;

        public bool FirstRun {
            get {
                EnsureFirstRun();
                return firstRunValue.Value;
            }
        }

        public DateTimeOffset FirstRunAt {
            get {
                EnsureFirstRun();

                if (!firstRunAtValue.HasValue)
                {
                    DateTimeOffset dt;
                    if (!DateTimeOffset.TryParseExact(firstRunAtString.ToEmptyIfNull(), Constants.Iso8601Formats,
                            CultureInfo.InvariantCulture, DateTimeStyles.None, out dt))
                    {
                        dt = DateTimeOffset.Now;
                    }
                    FirstRunAt = dt;
                }

                return firstRunAtValue.Value;
            }
            private set {
                firstRunAtString = value.ToString(Constants.Iso8601Format);
                firstRunAtValue = value;
                Save(true);
            }
        }

        private void EnsureFirstRun()
        {
            if (!firstRunValue.HasValue)
            {
                firstRunValue = firstRun;
            }
        }

        public bool Initialized {
            get { return initialized; }
            set {
                initialized = value;
                if (initialized && firstRun)
                {
                    firstRun = false;
                    FirstRunAt = DateTimeOffset.Now;
                }
                Save(true);
            }
        }
    }

    public abstract class BaseWindow : EditorWindow
    {
        [NonSerialized] private bool firstRepaint = true;

        void InitializeWindow(bool requiresRedraw = true)
        {
            if (!ApplicationState.Instance.Initialized)
            {
                ApplicationState.Instance.Initialized = true;
                Initialize();
                if (requiresRedraw)
                    Redraw();
            }
        }

        protected BaseWindow()
        {
        }

        public virtual void Initialize()
        {
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
        }

        public virtual void Awake()
        {
            InitializeWindow(false);
        }

        public virtual void OnEnable()
        {
            InitializeWindow(false);
        }

        public virtual void OnDisable()
        {
        }

        public virtual void Update()
        {
        }

        public virtual void OnFirstRepaint()
        {

        }

        public virtual void OnDataUpdate()
        {
        }

        // OnGUI calls this everytime, so override it to render as you would OnGUI
        public virtual void OnUI() { }

        // This is Unity's magic method
        private void OnGUI()
        {
            if (Event.current.type == EventType.Layout)
            {
                InLayout = true;
                OnDataUpdate();
                NeedsRefresh = false;
                if (firstRepaint)
                {
                    OnFirstRepaint();
                    firstRepaint = false;
                }
            }

            OnUI();

            if (Event.current.type == EventType.Layout)
            {
                InLayout = false;
            }
        }

        private void OnFocus()
        {
            HasFocus = true;
            OnFocusChanged();
        }

        private void OnLostFocus()
        {
            HasFocus = false;
            OnFocusChanged();
        }

        public virtual void OnFocusChanged()
        {
        }

        public virtual void OnDestroy()
        {
        }

        public virtual void OnSelectionChange()
        {
        }

        public static T GetWindowDontShow<T>() where T : EditorWindow
        {
            UnityEngine.Object[] windows = Resources.FindObjectsOfTypeAll(typeof(T));
            return (windows.Length > 0) ? (T)windows[0] : ScriptableObject.CreateInstance<T>();
        }

        public Rect Position => position;
        public bool IsBusy { get; set; }
        public bool HasFocus { get; private set; }
        protected bool InLayout { get; private set; }
        protected bool InRepaint => Event.current.type == EventType.Repaint;

        public bool NeedsRefresh { get; set; }
    }
}