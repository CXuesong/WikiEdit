using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unclassified.TxLib;
using WikiEdit.ViewModels.TextEditors;

namespace WikiEdit.Services
{
    /// <summary>
    /// A factory interface that creates <see cref="TextEditorViewModel"/>.
    /// </summary>
    public interface ITextEditorFactory
    {
        /// <summary>
        /// Creates a <see cref="TextEditorViewModel"/> according to the given language.
        /// </summary>
        /// <exception cref="ArgumentNullException"><paramref name="language"/> is <c>null</c>.</exception>
        /// <exception cref="NotSupportedException">The language is not supported.</exception>
        TextEditorViewModel CreateTextEditor(string language);

        /// <summary>
        /// Registers a new factory function with the language name.
        /// </summary>
        void Register(string language, Func<TextEditorViewModel> factoryFunc);
    }

    public class TextEditorFactory : ITextEditorFactory
    {
        private readonly Dictionary<string, Func<TextEditorViewModel>> _LanguageTextEditorDict =
            new Dictionary<string, Func<TextEditorViewModel>>();

        public TextEditorViewModel CreateTextEditor(string language)
        {
            var fact = _LanguageTextEditorDict.TryGetValue(language);
            if (fact == null)
                throw new NotSupportedException(Tx.T("errors.editor language not supported", "name", language));
            var inst = fact();
            Debug.Assert(inst != null, "TextEditor factory returned null for language: " + language + ".");
            return inst;
        }

        public void Register(string language, Func<TextEditorViewModel> factoryFunc)
        {
            if (language == null) throw new ArgumentNullException(nameof(language));
            if (factoryFunc == null) throw new ArgumentNullException(nameof(factoryFunc));
            if (string.IsNullOrWhiteSpace(language)) throw new ArgumentException(nameof(language));
            _LanguageTextEditorDict.Add(language, factoryFunc);
        }

        public TextEditorFactory()
        {
            
        }
    }
}
