using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Media;
using Newtonsoft.Json;
using Prism.Events;
using Prism.Mvvm;
using WikiEdit.Models;

namespace WikiEdit.Services
{
    public class SettingsService : BindableBase
    {

        private readonly IEventAggregator _EventAggregator;

        public SettingsService(IEventAggregator eventAggregator)
        {
            if (eventAggregator == null) throw new ArgumentNullException(nameof(eventAggregator));
            _EventAggregator = eventAggregator;
        }

        #region Persistence

        private static readonly JsonSerializer SettingsSerializer = new JsonSerializer();

        private Settings _RawSettings;

        public Settings RawSettings
        {
            get { return _RawSettings; }
            set { SetProperty(ref _RawSettings, value); }
        }

        /// <summary>
        /// Reset application settings.
        /// </summary>
        public void Reset()
        {
            var s = new Settings
            {
                TextEditor = new TextEditorSettings
                {
                    LanguageSettings = new Dictionary<string, TextEditorLanguageSettings>
                    {
                        {
                            "",
                            new TextEditorLanguageSettings
                            {
                                FontFamily = "Courier New, Courier, Monospace",
                                FontSize = 14
                            }
                        },
                        {"Wikitext", new TextEditorLanguageSettings {WikiContentModels = new[] {"wikitext"}}},
                        {"Lua", new TextEditorLanguageSettings {WikiContentModels = new[] {"Scribunto"}}},
                        {"JSON", new TextEditorLanguageSettings {WikiContentModels = new[] {"json"}}},
                        {"JavaScript", new TextEditorLanguageSettings {WikiContentModels = new[] {"javascript"}}},
                        {"CSS", new TextEditorLanguageSettings {WikiContentModels = new[] {"css"}}},
                    }
                }
            };
            RawSettings = s;
            RefreshSettings();
        }

        public void Load()
        {
            if (!File.Exists(GlobalConfigurations.SettingsFile))
            {
                Reset();
            }
            else
            {
                using (var sr = File.OpenText(GlobalConfigurations.SettingsFile))
                using (var jr = new JsonTextReader(sr))
                    RawSettings = SettingsSerializer.Deserialize<Settings>(jr);
                RefreshSettings();
            }
        }

        public void Save()
        {
            using (var sw = File.CreateText(GlobalConfigurations.SettingsFile))
            using (var jw = new JsonTextWriter(sw))
                SettingsSerializer.Serialize(jw, RawSettings);
        }

        #endregion

        private readonly Dictionary<string, RuntimeTextEditorLanguageSettings> _LanguageSettings =
            new Dictionary<string, RuntimeTextEditorLanguageSettings>();

        private readonly Dictionary<string, RuntimeTextEditorLanguageSettings> _WikiContentModelLanguageSettings =
            new Dictionary<string, RuntimeTextEditorLanguageSettings>();

        /// <summary>
        /// Get language settings from language name.
        /// </summary>
        /// <param name="languageName">Language name. Use <see cref="string.Empty"/> to get the base language settings.</param>
        /// <returns>Language-specific settings or base settings (fallback).</returns>
        public RuntimeTextEditorLanguageSettings GetSettingsByLanguage(string languageName)
        {
            if (languageName == null) return _LanguageSettings[""];
            return _LanguageSettings.TryGetValue(languageName) ?? _LanguageSettings[""];
        }

        /// <summary>
        /// Get language settings from wiki content model name.
        /// </summary>
        /// <param name="contentModel">Content model name. Use <see cref="string.Empty"/> to get the base language settings.</param>
        /// <returns>Language-specific settings or base settings (fallback).</returns>
        public RuntimeTextEditorLanguageSettings GetSettingsByWikiContentModel(string contentModel)
        {
            if (contentModel == null) return _WikiContentModelLanguageSettings[""];
            return _WikiContentModelLanguageSettings.TryGetValue(contentModel) ?? _WikiContentModelLanguageSettings[""];
        }

        /// <summary>
        /// Load settings from <see cref="RawSettings"/> to this service.
        /// </summary>
        /// <remarks>This method should be invoked after <see cref="RawSettings"/> has been changed.</remarks>
        public void RefreshSettings()
        {
            {
                // Associates language settings with wiki ContentModels.
                _LanguageSettings.Clear();
                _WikiContentModelLanguageSettings.Clear();
                var defaultLanguageSettings = _RawSettings.TextEditor.LanguageSettings[""];
                foreach (var p in _RawSettings.TextEditor.LanguageSettings)
                {
                    var settings = new RuntimeTextEditorLanguageSettings(p.Key, p.Value, defaultLanguageSettings);
                    _LanguageSettings[p.Key] = settings;
                    if (p.Key == "")
                    {
                        _WikiContentModelLanguageSettings[""] = settings;
                    }
                    else
                    {
                        foreach (var model in p.Value.WikiContentModels)
                            _WikiContentModelLanguageSettings[model] = settings;
                    }
                }
            }
            // Notify about the changes.
            _EventAggregator.GetEvent<SettingsChangedEvent>().Publish();
        }
    }
    
    public class RuntimeTextEditorLanguageSettings
    {
        private static readonly FontFamily DefaultFontFamily = (FontFamily)TextBlock.FontFamilyProperty.DefaultMetadata.DefaultValue;

        public string LanguageName { get; }

        public FontFamily FontFamily { get; }

        public float FontSize { get; }

        public RuntimeTextEditorLanguageSettings(string languageName, TextEditorLanguageSettings settings,
            TextEditorLanguageSettings baseSettings)
        {
            if (languageName == null) throw new ArgumentNullException(nameof(languageName));
            if (settings == null) throw new ArgumentNullException(nameof(settings));
            LanguageName = languageName;
            var ff = settings.FontFamily ?? baseSettings.FontFamily;
            FontFamily = string.IsNullOrWhiteSpace(ff)
                ? DefaultFontFamily
                : new FontFamily(ff);
            FontSize = settings.FontSize ?? baseSettings.FontSize ?? 9;
        }
    }
}
