using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Threading;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using Prism.Mvvm;
using WikiClientLibrary;
using Xceed.Wpf.AvalonDock.Controls;

namespace WikiEdit
{
    internal static class Utility
    {
        #region Application Information

        private static string _ApplicationTitle;
        private static string _ProductName;
        private static Version _ProductVersion;

        public static string ApplicationTitle
        {
            get
            {
                if (_ApplicationTitle == null)
                {
                    var titleAttribute = typeof(Utility).Assembly.GetCustomAttribute<AssemblyTitleAttribute>();
                    _ApplicationTitle = titleAttribute != null ? titleAttribute.Title : "";
                }
                return _ApplicationTitle;
            }
        }

        public static string ProductName
        {
            get
            {
                if (_ProductName == null)
                {
                    var productAttribute = typeof(Utility).Assembly.GetCustomAttribute<AssemblyProductAttribute>();
                    _ProductName = productAttribute != null ? productAttribute.Product : "";
                }
                return _ProductName;
            }
        }

        public static Version ProductVersion
        {
            get
            {
                if (_ProductVersion == null) _ProductVersion = typeof(Utility).Assembly.GetName().Version;
                return _ProductVersion;
            }
        }

        #endregion

        public static TValue TryGetValue<TKey, TValue>(this IDictionary<TKey, TValue> dict, TKey key)
        {
            if (dict == null) throw new ArgumentNullException(nameof(dict));
            TValue v;
            if (dict.TryGetValue(key, out v))
                return v;
            return default(TValue);
        }

        public static void AddRange<T>(this IList<T> list, IEnumerable<T> items)
        {
            if (list == null) throw new ArgumentNullException(nameof(list));
            if (items == null) throw new ArgumentNullException(nameof(items));
            foreach (var item in items) list.Add(item);
        }

        public static bool? Confirm(string prompt, bool cancellable = false)
        {
            switch (MessageBox.Show(prompt, ApplicationTitle,
                cancellable ? MessageBoxButton.YesNoCancel : MessageBoxButton.YesNo,
                MessageBoxImage.Question))
            {
                case MessageBoxResult.Yes:
                    return true;
                case MessageBoxResult.No:
                    return false;
                case MessageBoxResult.Cancel:
                    return null;
            }
            return null;
        }

        /// <summary>
        /// Makes exception messages easier to read.
        /// </summary>
        public static string GetExceptionMessage(Exception ex)
        {
            if (ex == null) throw new ArgumentNullException(nameof(ex));
            var ae = ex as AggregateException;
            if (ae != null)
                return string.Join(" ", ae.InnerExceptions.Select(GetExceptionMessage));
            if (ex is HttpRequestException) // The description is usually useless
                return ex.InnerException?.Message ?? ex.Message;
            return ex.Message;
        }

        public static void ReportException(Exception ex, string prompt = null)
        {
#if DEBUG
            MessageBox.Show((prompt == null ? null : prompt + "\n") + ex, ApplicationTitle,
                MessageBoxButton.OK, MessageBoxImage.Exclamation);
#else
            MessageBox.Show((prompt == null ? null : (prompt + "\n")) + GetExceptionMessage(ex),
                ApplicationTitle, MessageBoxButton.OK, MessageBoxImage.Exclamation);
#endif
        }

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

        public static void SetErrors<T>(this ErrorsContainer<T> errorsContainer, string propertyName, params T[] errors)
        {
            if (errorsContainer == null) throw new ArgumentNullException(nameof(errorsContainer));
            errorsContainer.SetErrors(propertyName, (IEnumerable<T>) errors);
        }

        /// <summary>
        /// Do some simple validations on the provided API endpoint URL.
        /// </summary>
        public static bool ValidateApiEndpointBasic(string endpointUrl)
        {
            Uri u;
            if (!Uri.TryCreate(endpointUrl, UriKind.Absolute, out u)) return false;
            if (!string.IsNullOrEmpty(u.Query)) return false;
            return true;
        }
    }

    /// <summary>
    /// An extended BooleanToVisibilityConverter.
    /// </summary>
    public class UniversalBooleanConverter : IValueConverter
    {
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
            if (value is bool) v = (bool)value;
            if (value is string) v = !string.IsNullOrEmpty((string) value);
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
            throw new NotSupportedException();
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
            throw new NotSupportedException();
        }
    }

    /// <summary>
    /// This converter serializes CookieContainer as binary string.
    /// </summary>
    public class CookieContainerJsonConverter : JsonConverter
    {
        private static readonly BinaryFormatter formatter = new BinaryFormatter();

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            var cc = (CookieContainer) value;
            if (cc.Count == 0)
            {
                writer.WriteValue("");
                return;
            }
            using (var ms = new MemoryStream())
            {
                formatter.Serialize(ms, cc);
                writer.WriteValue(ms.ToArray());
            }
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue,
            JsonSerializer serializer)
        {
            var data = Convert.FromBase64String((string) reader.Value);
            if (data.Length == 0) return new CookieContainer();
            using (var ms = new MemoryStream(data))
            {
                return (CookieContainer) formatter.Deserialize(ms);
            }
        }

        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(CookieContainer);
        }
    }

    public class TraceLogger : ILogger
    {
        public static TraceLogger Default { get; } = new TraceLogger();

        public void Trace(string message)
        {
            System.Diagnostics.Trace.WriteLine(message);
        }

        public void Info(string message)
        {
            System.Diagnostics.Trace.TraceInformation(message);
        }

        public void Warn(string message)
        {
            System.Diagnostics.Trace.TraceWarning(message);
        }

        public void Error(Exception exception, string message)
        {
            System.Diagnostics.Trace.TraceError(message);
        }
    }

    /// <summary>
    /// An accessor helper that allows XAML bind to a value using the PropertyPath expression
    /// like <c>Accessor[key]</c>.
    /// </summary>
    public sealed class ObservableItemAccessor<TValue> : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        private readonly Func<string, TValue> _AccessorHandler;

        /// <param name="accessorHandler">The function that is invoked by <see cref="this"/></param>
        public ObservableItemAccessor(Func<string, TValue> accessorHandler)
        {
            if (accessorHandler == null) throw new ArgumentNullException(nameof(accessorHandler));
            _AccessorHandler = accessorHandler;
        }

        // It's WPF's limitation that the key should only be string or object.

        /// <summary>
        /// Gets the value of the specified key.
        /// </summary>
        /// <returns>The value, or <c>default(TValue)</c> if the value doesn't exist.</returns>
        public TValue this[string key] => _AccessorHandler(key);

        /// <summary>
        /// Notifies that one or more values of <see cref="this"/> has been changed.
        /// </summary>
        public void NotifyAccessorChanged()
        {
            // There's no way to tell the binding which item has been changed.
            OnPropertyChanged(Binding.IndexerName);
        }

        private void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
