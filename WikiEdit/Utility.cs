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
        public static void Alert(string prompt)
        {
            MessageBox.Show(prompt, ApplicationTitle, MessageBoxButton.OK, MessageBoxImage.Exclamation);
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

        /// <summary>
        /// Evaluates the best interval for updating the relative datetime.
        /// </summary>
        public static TimeSpan FitRelativeDateUpdateInterval(TimeSpan currentDateChange)
        {
            if (currentDateChange < TimeSpan.FromSeconds(1)) return TimeSpan.FromSeconds(10);
            if (currentDateChange < TimeSpan.FromMinutes(1)) return TimeSpan.FromSeconds(10);
            if (currentDateChange < TimeSpan.FromHours(1)) return TimeSpan.FromMinutes(1);
            if (currentDateChange < TimeSpan.FromHours(4)) return TimeSpan.FromMinutes(10);
            return TimeSpan.FromMinutes(20);
        }

        /// <summary>
        /// Opens the specified URL in the system default browser.
        /// </summary>
        public static void OpenUrl(string url)
        {
            if (url == null) throw new ArgumentNullException(nameof(url));
            var si = new ProcessStartInfo(url) {UseShellExecute = true};
            var proc = Process.Start(si);
            proc?.Dispose();
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
