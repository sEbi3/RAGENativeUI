namespace RAGENativeUI.Menus
{
    using System;
    using System.Globalization;

    using Rage;

    public class MenuItemNumericScroller : MenuItemScroller
    {
        private decimal currentValue;
        private decimal minimum = 0.0m;
        private decimal maximum = 100.0m;
        private decimal increment = 0.5m;
        private bool thousandsSeparator;
        private bool hexadecimal;
        private int decimalPlaces = 2;

        public decimal Value
        {
            get { return currentValue; }
            set
            {
                if (value != currentValue)
                {
                    Throw.IfOutOfRange(value, minimum, maximum, nameof(value), $"{nameof(Value)} can't be lower than {nameof(Minimum)} or higher than {nameof(Maximum)}.");

                    currentValue = value;

                    UpdateSelectedIndex();
                }
            }
        }

        public decimal Minimum
        {
            get => minimum;
            set
            {
                if (value != minimum)
                {
                    minimum = value;
                    if (minimum > maximum)
                    {
                        maximum = minimum;
                        OnPropertyChanged(nameof(Maximum));
                    }
                    OnPropertyChanged(nameof(Minimum));
                    OnPropertyChanged(nameof(OptionCount));
                    Value = EnsureValue(Value);

                    UpdateSelectedIndex();
                }
            }
        }

        public decimal Maximum
        {
            get => maximum;
            set
            {
                if (value != maximum)
                {
                    maximum = value;
                    if (minimum > maximum)
                    {
                        minimum = maximum;
                        OnPropertyChanged(nameof(Minimum));
                    }
                    OnPropertyChanged(nameof(Maximum));
                    OnPropertyChanged(nameof(OptionCount));
                    Value = EnsureValue(Value);

                    UpdateSelectedIndex();
                }
            }
        }

        public decimal Increment
        {
            get => increment;
            set
            {
                Throw.IfNegative(value, nameof(value));

                if (value != increment)
                {
                    increment = value;
                    OnPropertyChanged(nameof(Increment));
                    OnPropertyChanged(nameof(OptionCount));
                    UpdateSelectedIndex();
                }
            }
        }

        public override int SelectedIndex
        {
            get => base.SelectedIndex;
            set
            {
                int newIndex = RPH.MathHelper.Clamp(value, 0, RPH.MathHelper.Max(0, OptionCount - 1));
                if (newIndex != SelectedIndex)
                {
                    currentValue = Minimum + newIndex * Increment;
                    base.SelectedIndex = newIndex;
                }
            }
        }

        public bool ThousandsSeparator
        {
            get => thousandsSeparator;
            set
            {
                if(value != thousandsSeparator)
                {
                    thousandsSeparator = value;
                    OnPropertyChanged(nameof(ThousandsSeparator));
                }
            }
        }

        public bool Hexadecimal
        {
            get => hexadecimal;
            set
            {
                if (value != hexadecimal)
                {
                    hexadecimal = value;
                    OnPropertyChanged(nameof(Hexadecimal));
                }
            }
        }

        public int DecimalPlaces
        {
            get => decimalPlaces;
            set
            {
                if (value != decimalPlaces)
                {
                    decimalPlaces = value;
                    OnPropertyChanged(nameof(DecimalPlaces));
                }
            }
        }

        public override int OptionCount => (int)((Maximum - Minimum) / Increment) + 1;

        public override string SelectedOptionText
        {
            get
            {
                string text;

                if (Hexadecimal)
                {
                    text = ((Int64)currentValue).ToString("X", CultureInfo.InvariantCulture);
                }
                else
                {
                    text = currentValue.ToString((ThousandsSeparator ? "N" : "F") + DecimalPlaces.ToString(CultureInfo.CurrentCulture), CultureInfo.CurrentCulture);
                }

                return text;
            }
        }

        public MenuItemNumericScroller(string text, string description) : base(text, description)
        {
            SelectedIndex = OptionCount / 2;
        }

        public MenuItemNumericScroller(string text) : this(text, String.Empty)
        {
        }

        private decimal EnsureValue(decimal value)
        {
            if (value < minimum)
                value = minimum;

            if (value > maximum)
                value = maximum;

            return value;
        }

        private void UpdateSelectedIndex()
        {
            SelectedIndex = (int)((currentValue - Minimum) / Increment);
        }

        protected internal override void OnScrollingToPreviousValue()
        {
            if (IsDisabled)
            {
                return;
            }

            decimal newValue = currentValue;

            try
            {
                newValue -= Increment;

                if (newValue < minimum)
                    newValue = minimum;
            }
#if DEBUG
            catch (OverflowException ex)
            {
                Common.LogDebug("MenuItemNumericScroller.OnPreviewMoveLeft: OverflowException");
                Common.LogDebug(ex.ToString());

                newValue = minimum;
            }
#else
            catch (OverflowException)
            {
                newValue = minimum;
            }
#endif
            Value = newValue;
        }

        protected internal override void OnScrollingToNextValue()
        {
            if (IsDisabled)
            {
                return;
            }

            decimal newValue = currentValue;

            try
            {
                newValue += Increment;

                if (newValue > maximum)
                    newValue = maximum;
            }
#if DEBUG
            catch (OverflowException ex)
            {
                Common.LogDebug("MenuItemNumericScroller.OnPreviewMoveLeft: OverflowException");
                Common.LogDebug(ex.ToString());

                newValue = maximum;
            }
#else
            catch (OverflowException)
            {
                newValue = maximum;
            }
#endif
            Value = newValue;
        }

        protected override void OnPropertyChanged(string propertyName)
        {
            base.OnPropertyChanged(propertyName);

            if (propertyName == nameof(SelectedIndex))
            {
                OnPropertyChanged(nameof(Value));
            }
        }
    }
}

