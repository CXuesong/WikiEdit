using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
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

        public static void ReportException(Exception ex)
        {
#if DEBUG
            MessageBox.Show(ex.ToString(), ApplicationTitle, MessageBoxButton.OK, MessageBoxImage.Exclamation);
#else
            MessageBox.Show(ex.Message, ApplicationTitle, MessageBoxButton.OK, MessageBoxImage.Exclamation);
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
    }

    /// <summary>
    /// An extended BooleanToVisibilityConverter.
    /// </summary>
    public class UniversalBooleanConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var v = value != null;
            if (value is bool) v = (bool)value;
            if (parameter?.ToString() == "Inverse") v = !v;
            if (targetType == typeof(bool) || targetType == typeof(object))
                return v;
            if (targetType == typeof(Visibility))
                return v ? Visibility.Visible : Visibility.Collapsed;
            throw new NotSupportedException();
        }

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
                return (CookieContainer)formatter.Deserialize(ms);
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
}
