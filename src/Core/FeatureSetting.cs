using System;

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
}
