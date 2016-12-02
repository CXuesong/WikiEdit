using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Threading;
using Unclassified.TxLib;

namespace WikiEdit
{
    internal static class WpfUtility
    {
        /// <summary>
        /// This constant is used in XAML to specify a full date & time format.
        /// </summary>
        public const TxTime FullDateTime = TxTime.YearMonthDay | TxTime.HourMinuteSecond;


        public static T FindAncestor<T>(DependencyObject obj) where T : DependencyObject
        {
            // We'll skip the obj itself.
            if (obj == null) throw new ArgumentNullException(nameof(obj));
            do
            {
                obj = VisualTreeHelper.GetParent(obj);
                var t = obj as T;
                if (t != null) return t;
            } while (obj != null);
            return null;
        }

        /// <summary>
        /// Invoke this extension method on MAIN thread to explicitly
        /// forget the task, leave it running. This method is used to
        /// suppress CS4014 warning.
        /// </summary>
        /// <param name="task">The task to be forgotten.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Forget(this Task task)
        {
            if (task == null) throw new ArgumentNullException(nameof(task));
            // This method should only be called from main thread to avoid possible deadlocks.
            // See http://stackoverflow.com/questions/22629951/suppressing-warning-cs4014-because-this-call-is-not-awaited-execution-of-the#comment58040933_22630057 .
            Debug.Assert(Application.Current.Dispatcher == Dispatcher.CurrentDispatcher,
                "Attempting to forget a Task when the invoker is not on the main thread.");
        }

        /// <summary>
        /// Invoke a function from the given <see cref="Dispatcher"/>. If the current
        /// dispatcher is the same as given one, the function is called directly.
        /// </summary>
        public static void AutoInvoke(this Dispatcher dispatcher, Action action)
        {
            if (dispatcher == null) throw new ArgumentNullException(nameof(dispatcher));
            if (action == null) throw new ArgumentNullException(nameof(action));
            if (Thread.CurrentThread == dispatcher.Thread)
            {
                action();
            }
            else
            {
                dispatcher.Invoke(action);
            }
        }

        /// <summary>
        /// Invoke a function from the given <see cref="Dispatcher"/>. If the current
        /// dispatcher is the same as given one, the function is called directly.
        /// </summary>
        public static T AutoInvoke<T>(this Dispatcher dispatcher, Func<T> action)
        {
            if (dispatcher == null) throw new ArgumentNullException(nameof(dispatcher));
            if (action == null) throw new ArgumentNullException(nameof(action));
            if (Thread.CurrentThread == dispatcher.Thread)
            {
                return action();
            }
            else
            {
                return dispatcher.Invoke(action);
            }
        }

        public static Color ColorFromString(string value)
        {
            return (Color) ColorConverter.ConvertFromString(value);
        }
    }

    /// <summary>
    /// An extended BooleanToVisibilityConverter.
    /// </summary>
    public class UniversalBooleanConverter : IValueConverter
    {
        /// <summary>
        /// Determines whether the parameter passed to this Converter has certain flag.
        /// </summary>
        private static bool HasFlag(object parameter, string testFlag)
        {
            if (parameter == null) return false;
            var s = parameter.ToString();
            // This is a simple test.
            // For parameter, we recommend a style like
            // flag1, flag2, flag3, ...
            return s.Contains(testFlag);
        }

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var v = value != null;
            if (value is bool) v = (bool) value;
            if (value is string) v = !string.IsNullOrEmpty((string)value);
            if (HasFlag(parameter, "Inverse")) v = !v;
            if (targetType == typeof(bool) || targetType == typeof(object))
                return v;
            if (targetType == typeof(Visibility))
            {
                if (v) return Visibility.Visible;
                return HasFlag(parameter, "PreserveLayout") ? Visibility.Hidden : Visibility.Collapsed;
            }
            throw new NotSupportedException();
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return DependencyProperty.UnsetValue;
        }
    }

    public class LocalizableBooleanConverter : IValueConverter
    {
        private static string AutoLocalize(string expression)
        {
            if (expression.StartsWith("@")) return expression.Substring(1);
            return Tx.T(expression);
        }

        // parameter
        //      true_text_key
        // OR
        //      true_text_key|false_text_key
        /// <inheritdoc />
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var b = (bool)value;
            if (targetType != typeof(string) && targetType != typeof(object))
                throw new NotSupportedException();
            var pm = ((string) parameter)?.Split('|');
            if (pm == null) return b.ToString();
            if (pm.Length == 1) return b ? AutoLocalize(pm[0]) : null;
            return b ? AutoLocalize(pm[0]) : AutoLocalize(pm[1]);
        }

        /// <inheritdoc />
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// There seems to be a b&#117;g with fluent:Button.Header that it cannot bind
    /// to and display values other than string.
    /// </summary>
    public class FluentHeaderCompatibleConverter : IValueConverter
    {
        /// <inheritdoc />
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var format = parameter as string;
            var ifmt = value as IFormattable;
            if (ifmt != null)
                return ifmt.ToString(format, culture);
            return value.ToString();
        }

        /// <inheritdoc />
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return DependencyProperty.UnsetValue;
        }
    }

    public class TxTimeConverter : IValueConverter
    {

        /// <inheritdoc />
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string) return (string) value;
            if (value is DateTime)
            {
                var ps = parameter as string;
                switch (ps)
                {
                    case "T":
                        return Tx.Time((DateTime) value, TxTime.HourMinuteSecond);
                    case "TS":
                        return Tx.Time((DateTime) value, TxTime.HourMinute);
                    case null:
                    default:
                        return Tx.Time((DateTime)value, TxTime.YearMonthDay | TxTime.HourMinuteSecond);
                }
            }
            if (value is TimeSpan) return Tx.TimeSpan((TimeSpan)value);
            throw new NotSupportedException();
        }

        /// <inheritdoc />
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return DependencyProperty.UnsetValue;
        }

    }

    public class TxSizeConverter : IValueConverter
    {

        /// <inheritdoc />
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string) return (string)value;
            var v = System.Convert.ToInt64(value);
            return Tx.DataSize(v);
        }

        /// <inheritdoc />
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return DependencyProperty.UnsetValue;
        }

    }
}
