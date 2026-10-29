using System;
using System.Collections.Generic;

namespace QoLChanges.Core
{
    /// <summary>Declarative description of a feature option; the settings menu is built from these.</summary>
    internal abstract class FeatureSetting
    {
        protected FeatureSetting(string label)
        {
            Label = label;
        }

        public string Label { get; }

        /// <summary>
        /// While it returns false the row is shown inactive, for an option that another option makes meaningless; the value
        /// is kept. Null: always available.
        /// </summary>
        public Func<bool> AvailableWhen { get; set; }

        public bool IsAvailable => AvailableWhen == null || AvailableWhen();

        /// <summary>Other options depend on this one: the settings page is built again after it changes.</summary>
        public bool RebuildsPage { get; set; }
    }

    internal sealed class IntSliderSetting : FeatureSetting
    {
        private readonly Func<int> _getter;
        private readonly Action<int> _setter;
        private readonly Func<int, string> _formatter;

        public IntSliderSetting(string label, int min, int max, Func<int> getter, Action<int> setter, Func<int, string> formatter)
            : base(label)
        {
            if (min > max)
            {
                throw new ArgumentException("min must not exceed max.", nameof(min));
            }

            Min = min;
            Max = max;
            _getter = getter ?? throw new ArgumentNullException(nameof(getter));
            _setter = setter ?? throw new ArgumentNullException(nameof(setter));
            _formatter = formatter ?? (value => value.ToString());
        }

        public int Min { get; }

        public int Max { get; }

        public int Value
        {
            get { return _getter(); }
            set { _setter(Math.Max(Min, Math.Min(Max, value))); }
        }

        public string Format(int value)
        {
            return _formatter(value);
        }
    }

    internal sealed class ToggleSetting : FeatureSetting
    {
        private readonly Func<bool> _getter;
        private readonly Action<bool> _setter;

        public ToggleSetting(string label, Func<bool> getter, Action<bool> setter)
            : base(label)
        {
            _getter = getter ?? throw new ArgumentNullException(nameof(getter));
            _setter = setter ?? throw new ArgumentNullException(nameof(setter));
        }

        public bool Value
        {
            get { return _getter(); }
            set { _setter(value); }
        }
    }

    /// <summary>One value out of a fixed list, shown as a dropdown. The value is the index of the option.</summary>
    internal sealed class ChoiceSetting : FeatureSetting
    {
        private readonly Func<int> _getter;
        private readonly Action<int> _setter;

        public ChoiceSetting(string label, IList<string> options, Func<int> getter, Action<int> setter)
            : base(label)
        {
            if (options == null || options.Count == 0)
            {
                throw new ArgumentException("At least one option is required.", nameof(options));
            }

            Options = new List<string>(options).AsReadOnly();
            _getter = getter ?? throw new ArgumentNullException(nameof(getter));
            _setter = setter ?? throw new ArgumentNullException(nameof(setter));
        }

        public IReadOnlyList<string> Options { get; }

        public int Value
        {
            get { return Clamp(_getter()); }
            set { _setter(Clamp(value)); }
        }

        private int Clamp(int index)
        {
            return Math.Max(0, Math.Min(Options.Count - 1, index));
        }
    }
}
