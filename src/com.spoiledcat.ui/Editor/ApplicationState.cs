using System;
using System.Globalization;
using SpoiledCat.Extensions;
using UnityEditor;
using UnityEngine;

namespace SpoiledCat.UI
{
    class ApplicationState : ScriptableSingleton<ApplicationState>
    {
        [NonSerialized] public DateTimeOffset? firstRunAtValue;
        [NonSerialized] private bool? firstRunValue;

        [NonSerialized] private bool initialized = false;
        [SerializeField] private bool firstRun = true;
        [SerializeField] public string firstRunAtString;

        private void EnsureFirstRun()
        {
            if (!firstRunValue.HasValue)
            {
                firstRunValue = firstRun;
            }
        }

        public static ApplicationState Instance => instance;

        public bool FirstRun
        {
            get
            {
                EnsureFirstRun();
                return firstRunValue.Value;
            }
        }

        public DateTimeOffset FirstRunAt
        {
            get
            {
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
            private set
            {
                firstRunAtString = value.ToString(Constants.Iso8601Format);
                firstRunAtValue = value;
                Save(true);
            }
        }

        public bool Initialized
        {
            get { return initialized; }
            set
            {
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
}