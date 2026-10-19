using System;
using System.Collections.Generic;
using TrafficAIOptimizer.Core.Contracts;
using TrafficAIOptimizer.Core.Providers;

namespace TrafficAIOptimizer.Security
{
    /// <summary>
    /// Gerenciador estritamente em memória RAM para as chaves de API dos provedores suportados
    /// (DeepSeek, OpenAI, Anthropic, Gemini, Grok, Meta).
    /// Garante que as chaves nunca sejam salvas em disco, PlayerPrefs, XML ou logs.
    /// Projetado para compatibilidade total com o runtime Mono clássico do Cities: Skylines.
    /// </summary>
    public class SecureKeyStorage : ISecureKeyStorage
    {
        private static readonly object _syncRoot = new object();
        private static readonly Dictionary<AIProvider, char[]> _providerKeys = new Dictionary<AIProvider, char[]>();
        private static AIProvider _activeProvider = AIProvider.DeepSeek;

        private static ISecureKeyStorage _instance = new SecureKeyStorage();
        public static ISecureKeyStorage Instance
        {
            get => _instance;
            set => _instance = value ?? new SecureKeyStorage();
        }

        public static AIProvider ActiveProvider
        {
            get => _activeProvider;
            set => _activeProvider = value;
        }

        // --- Implementação da Interface ISecureKeyStorage ---
        AIProvider ISecureKeyStorage.ActiveProvider
        {
            get => ActiveProvider;
            set => ActiveProvider = value;
        }

        bool ISecureKeyStorage.HasKey => HasKey;
        void ISecureKeyStorage.SetKey(string key) => SetKey(key);
        string ISecureKeyStorage.GetKeyUnsafe() => GetKeyUnsafe();
        void ISecureKeyStorage.Clear() => Clear();
        string ISecureKeyStorage.GetMaskedDisplay() => GetMaskedDisplay();

        bool ISecureKeyStorage.HasKeyFor(AIProvider provider) => HasKeyFor(provider);
        void ISecureKeyStorage.SetKey(string key, AIProvider provider) => SetKey(key, provider);
        string ISecureKeyStorage.GetKeyUnsafe(AIProvider provider) => GetKeyUnsafe(provider);
        void ISecureKeyStorage.Clear(AIProvider provider) => Clear(provider);
        void ISecureKeyStorage.ClearAll() => ClearAll();
        string ISecureKeyStorage.GetMaskedDisplay(AIProvider provider) => GetMaskedDisplay(provider);

        // --- Métodos Estáticos Públicos para o Provedor Ativo ---
        public static bool HasKey => HasKeyFor(_activeProvider);

        public static void SetKey(string key) => SetKey(key, _activeProvider);

        public static string GetKeyUnsafe() => GetKeyUnsafe(_activeProvider);

        public static void Clear() => Clear(_activeProvider);

        public static string GetMaskedDisplay() => GetMaskedDisplay(_activeProvider);

        // --- Métodos Específicos por Provedor ---
        public static bool HasKeyFor(AIProvider provider)
        {
            lock (_syncRoot)
            {
                if (_providerKeys.TryGetValue(provider, out char[] keyChars))
                {
                    return keyChars != null && keyChars.Length > 0;
                }
                return false;
            }
        }

        public static void SetKey(string key, AIProvider provider)
        {
            lock (_syncRoot)
            {
                Clear(provider);

                if (!string.IsNullOrEmpty(key))
                {
                    string trimmed = key.Trim();
                    _providerKeys[provider] = trimmed.ToCharArray();
                }
            }
        }

        public static string GetKeyUnsafe(AIProvider provider)
        {
            lock (_syncRoot)
            {
                if (_providerKeys.TryGetValue(provider, out char[] keyChars))
                {
                    if (keyChars != null && keyChars.Length > 0)
                    {
                        return new string(keyChars);
                    }
                }
                return null;
            }
        }

        public static void Clear(AIProvider provider)
        {
            lock (_syncRoot)
            {
                if (_providerKeys.TryGetValue(provider, out char[] keyChars))
                {
                    if (keyChars != null)
                    {
                        for (int i = 0; i < keyChars.Length; i++)
                        {
                            keyChars[i] = '\0';
                        }
                    }
                    _providerKeys.Remove(provider);
                }
            }
        }

        public static void ClearAll()
        {
            lock (_syncRoot)
            {
                foreach (var kvp in _providerKeys)
                {
                    char[] keyChars = kvp.Value;
                    if (keyChars != null)
                    {
                        for (int i = 0; i < keyChars.Length; i++)
                        {
                            keyChars[i] = '\0';
                        }
                    }
                }
                _providerKeys.Clear();
            }
        }

        public static string GetMaskedDisplay(AIProvider provider)
        {
            lock (_syncRoot)
            {
                if (!_providerKeys.TryGetValue(provider, out char[] current) || current == null || current.Length == 0)
                {
                    return "Nenhuma chave definida (Memória vazia)";
                }

                if (current.Length <= 8)
                {
                    return "••••••••";
                }

                string prefix = new string(current, 0, Math.Min(4, current.Length));
                string suffix = new string(current, Math.Max(0, current.Length - 4), Math.Min(4, current.Length));
                return string.Format("{0}••••••••{1}", prefix, suffix);
            }
        }
    }
}
