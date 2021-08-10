using System;
using System.Numerics;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ST4I.Vision.Core
{
    /// <summary>
    /// Value between range value [LowerValue, UpperValue]
    /// </summary>
    /// <typeparam name="T">T must be numberic type</typeparam>
    public class ValueRange<T> where T : struct, IConvertible
    {
        private T _lowerValue;
        private T _upperValue;
        private T _minimum;
        private T _maximum;
        public ValueRange()
        {
            ValueNumeric.GetMinValueOfType<T>(ref _minimum);
            ValueNumeric.GetMaxValueOfType<T>(ref _maximum);
            _lowerValue = _minimum;
            _upperValue = _maximum;
        }
        /// <summary>
        /// Minimum value can be set
        /// </summary>
        public T Minimum
        {
            get
            {
                return _minimum;
            }
            set
            {
                _minimum = value;
            }
        }
        /// <summary>
        /// Maximum value can be set
        /// </summary>
        public T Maximum
        {
            get
            {
                return _maximum;
            }
            set
            {
                _maximum = value;
            }
        }
        /// <summary>
        /// Limit lower value
        /// </summary>
        public T LowerValue
        {
            get { return _lowerValue; }
            set
            {
                var cmp = Comparer<T>.Default;
                if (cmp.Compare(value, Minimum) <= 0)
                {
                    _lowerValue = Minimum;
                }
                else
                {
                    if (cmp.Compare(value, _upperValue) <= 0)
                    {
                        _lowerValue = value;
                    }
                    else
                    {
                        _lowerValue = _upperValue;
                    }
                }
            }
        }
        /// <summary>
        /// Limit upper value
        /// </summary>
        public T UpperValue
        {
            get { return _upperValue; }
            set
            {
                var cmp = Comparer<T>.Default;
                if (cmp.Compare(value, Maximum) <= 0)
                {
                    if (cmp.Compare(value, _lowerValue) >= 0)
                    {
                        _upperValue = value;
                    }
                    else
                    {
                        _upperValue = _lowerValue;
                    }
                }
                else
                {
                    _upperValue = Maximum;
                }
            }
        }
    }
    /// <summary>
    /// Property in Range Value
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class PropRange<T> : ValueRange<T>, ICloneable where T : struct, IConvertible
    {
        /// <summary>
        /// Enable property
        /// </summary>
        public bool IsEnabled { get; set; }
        /// <summary>
        /// Clone object
        /// </summary>
        /// <returns></returns>
        public object Clone()
        {
            return new PropRange<T>()
            {
                IsEnabled = this.IsEnabled,
                Minimum = this.Minimum,
                Maximum = this.Maximum,
                LowerValue = this.LowerValue,
                UpperValue = this.UpperValue
            };
        }
    }
    /// <summary>
    /// Base class for limitation with thresh value
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class LimitThresh<T> : ICloneable
    {
        /// <summary>
        /// Enable/Disable this judgement
        /// </summary>
        public bool IsEnabled { get; set; }
        /// <summary>
        /// Min value
        /// </summary>
        public T ThreshValue { get; set; }
        /// <summary>
        /// Clone object
        /// </summary>
        /// <returns></returns>
        public object Clone()
        {
            return new LimitThresh<T>()
            {
                IsEnabled = this.IsEnabled,
                ThreshValue = this.ThreshValue
            };
        }
    }
    /// <summary>
    /// Limitation for min value
    /// </summary>
    /// <typeparam name="T">number type</typeparam>
    public class LimitMin<T> : LimitThresh<T>, ICloneable
    {
        /// <summary>
        /// Verify value
        /// </summary>
        /// <param name="value">T value</param>
        /// <returns>true if value >= ThreshValue or if limitation is disable</returns>
        public bool Verify(T value)
        {

            if (IsEnabled)
                return Comparer<T>.Default.Compare(value, ThreshValue) >= 0;
            else return true;
        }
        /// <summary>
        /// Clone object
        /// </summary>
        /// <returns></returns>
        public new object Clone()
        {
            return new LimitMin<T>()
            {
                IsEnabled = this.IsEnabled,
                ThreshValue = this.ThreshValue
            };
        }
    }
    /// <summary>
    /// Limitation for max value
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class LimitMax<T> : LimitThresh<T>, ICloneable
    {
        /// <summary>
        /// Verify value
        /// </summary>
        /// <param name="value">T value</param>
        /// <returns>true if value <= ThreshValue or if limitation is disable</returns>
        public bool Verify(T value)
        {
            if (IsEnabled)
                return Comparer<T>.Default.Compare(value, ThreshValue) <= 0;
            else return true;
        }
        /// <summary>
        /// Clone object
        /// </summary>
        /// <returns></returns>
        public new object Clone()
        {
            return new LimitMin<T>()
            {
                IsEnabled = this.IsEnabled,
                ThreshValue = this.ThreshValue
            };
        }
    }
    /// <summary>
    /// Limitation in Range Value
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class LimitRange<T> : PropRange<T>, ICloneable where T : struct, IConvertible
    {
        /// <summary>
        /// Verify input data
        /// </summary>
        /// <param name="value">T value</param>
        /// <returns>true if LowerValue <= value <= UpperValue or if limitation is disable</returns>
        public bool Verify(T value)
        {
            if (IsEnabled)
            {
                var cmp = Comparer<T>.Default;
                if ((cmp.Compare(LowerValue, value) <= 0) && (cmp.Compare(value, UpperValue) <= 0))
                {
                    return true;
                }
                else return false;
            }
            else return true;
        }
        /// <summary>
        /// Clone object
        /// </summary>
        /// <returns></returns>
        public new object Clone()
        {
            return new LimitRange<T>()
            {
                IsEnabled = this.IsEnabled,
                Minimum = this.Minimum,
                Maximum = this.Maximum,
                LowerValue = this.LowerValue,
                UpperValue = this.UpperValue
            };
        }
    }

    public static class ValueNumeric
    {
        public static bool IsInteger(ValueType value)
        {
            return (value is SByte || value is Int16 || value is Int32
                    || value is Int64 || value is Byte || value is UInt16
                    || value is UInt32 || value is UInt64);
        }

        public static bool IsFloat(ValueType value)
        {
            return (value is float | value is double | value is Decimal);
        }

        public static bool IsNumeric(ValueType value)
        {
            return (value is Byte ||
                    value is Int16 ||
                    value is Int32 ||
                    value is Int64 ||
                    value is SByte ||
                    value is UInt16 ||
                    value is UInt32 ||
                    value is UInt64 ||
                    value is Decimal ||
                    value is Double ||
                    value is Single);
        }
        /// <summary>
        /// Get mininum defined value of type
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="value"></param>
        public static void GetMinValueOfType<T>(ref T value)
        {
            if (typeof(T) == typeof(Byte))
            {
                value = (T)(object)Convert.ChangeType(Byte.MinValue, typeof(T));
            }
            else if (typeof(T) == typeof(Int16))
            {
                value = (T)(object)Convert.ChangeType(Int16.MinValue, typeof(T));
            }
            else if (typeof(T) == typeof(Int32))
            {
                value = (T)(object)Convert.ChangeType(Int32.MinValue, typeof(T));
            }
            else if (typeof(T) == typeof(Int64))
            {
                value = (T)(object)Convert.ChangeType(Int64.MinValue, typeof(T));
            }
            else if (typeof(T) == typeof(SByte))
            {
                value = (T)(object)Convert.ChangeType(SByte.MinValue, typeof(T));
            }
            else if (typeof(T) == typeof(UInt16))
            {
                value = (T)(object)Convert.ChangeType(UInt16.MinValue, typeof(T));
            }
            else if (typeof(T) == typeof(UInt32))
            {
                value = (T)(object)Convert.ChangeType(UInt32.MinValue, typeof(T));
            }
            else if (typeof(T) == typeof(UInt64))
            {
                value = (T)(object)Convert.ChangeType(UInt64.MinValue, typeof(T));
            }
            else if (typeof(T) == typeof(Decimal))
            {
                value = (T)(object)Convert.ChangeType(Decimal.MinValue, typeof(T));
            }
            else if (typeof(T) == typeof(Double))
            {
                value = (T)(object)Convert.ChangeType(Double.MinValue, typeof(T));
            }
            else if (typeof(T) == typeof(Single))
            {
                value = (T)(object)Convert.ChangeType(Single.MinValue, typeof(T));
            }
        }
        /// <summary>
        /// Get maximum defined value of type
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="value"></param>
        public static void GetMaxValueOfType<T>(ref T value)
        {
            if (typeof(T) == typeof(Byte))
            {
                value = (T)(object)Convert.ChangeType(Byte.MaxValue, typeof(T));
            }
            else if (typeof(T) == typeof(Int16))
            {
                value = (T)(object)Convert.ChangeType(Int16.MaxValue, typeof(T));
            }
            else if (typeof(T) == typeof(Int32))
            {
                value = (T)(object)Convert.ChangeType(Int32.MaxValue, typeof(T));
            }
            else if (typeof(T) == typeof(Int64))
            {
                value = (T)(object)Convert.ChangeType(Int64.MaxValue, typeof(T));
            }
            else if (typeof(T) == typeof(SByte))
            {
                value = (T)(object)Convert.ChangeType(SByte.MaxValue, typeof(T));
            }
            else if (typeof(T) == typeof(UInt16))
            {
                value = (T)(object)Convert.ChangeType(UInt16.MaxValue, typeof(T));
            }
            else if (typeof(T) == typeof(UInt32))
            {
                value = (T)(object)Convert.ChangeType(UInt32.MaxValue, typeof(T));
            }
            else if (typeof(T) == typeof(UInt64))
            {
                value = (T)(object)Convert.ChangeType(UInt64.MaxValue, typeof(T));
            }
            else if (typeof(T) == typeof(Decimal))
            {
                value = (T)(object)Convert.ChangeType(Decimal.MaxValue, typeof(T));
            }
            else if (typeof(T) == typeof(Double))
            {
                value = (T)(object)Convert.ChangeType(Double.MaxValue, typeof(T));
            }
            else if (typeof(T) == typeof(Single))
            {
                value = (T)(object)Convert.ChangeType(Single.MaxValue, typeof(T));
            }
        }
    }
}
