using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
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

    public class WikiEditSessionContractResolver : CamelCasePropertyNamesContractResolver
    {
        protected override IList<JsonProperty> CreateProperties(Type type, MemberSerialization memberSerialization)
        {
            // CookieContainer is hard to serialize…
            if (type == typeof(CookieContainer))
            {
                //memberSerialization = MemberSerialization.Fields;
                var props = type.GetFields(BindingFlags.Public
                                           | BindingFlags.NonPublic
                                           | BindingFlags.Instance)
                    .Select(f => CreateProperty(f, MemberSerialization.Fields))
                    .ToList();
                return props;
            }
            return base.CreateProperties(type, memberSerialization);
        }
    }
}
