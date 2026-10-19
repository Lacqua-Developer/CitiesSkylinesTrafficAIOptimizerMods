using System;
using System.Collections.Generic;
using ColossalFramework.Globalization;

namespace TrafficAIOptimizer.Core.Localization
{
    /// <summary>
    /// Gerenciador central de internacionalização (i18n).
    /// Detecta o idioma configurado no Cities: Skylines (LocaleManager) e fornece traduções completas
    /// para Português (pt-BR), Inglês (en), Francês (fr) e Espanhol (es), com fallback padrão para Inglês.
    /// </summary>
    public static class LocalizationManager
    {
        private static AppLanguage? _forcedLanguage = null;
        private static bool _eventHooked = false;

        public static event Action OnLanguageChanged;

        /// <summary>
        /// Permite forçar um idioma específico (essencial para testes unitários ou preferência do usuário).
        /// </summary>
        public static void ForceLanguage(AppLanguage? lang)
        {
            _forcedLanguage = lang;
            OnLanguageChanged?.Invoke();
        }

        /// <summary>
        /// Obtém o idioma atualmente ativo.
        /// </summary>
        public static AppLanguage CurrentLanguage
        {
            get
            {
                if (_forcedLanguage.HasValue)
                {
                    return _forcedLanguage.Value;
                }

                HookLocaleEventIfNeeded();
                return DetectGameLanguage();
            }
        }

        private static void HookLocaleEventIfNeeded()
        {
            if (_eventHooked) return;
            try
            {
                if (LocaleManager.instance != null)
                {
                    LocaleManager.eventLocaleChanged += OnGameLocaleChanged;
                    _eventHooked = true;
                }
            }
            catch
            {
                // Silencioso fora do jogo
            }
        }

        private static void OnGameLocaleChanged()
        {
            OnLanguageChanged?.Invoke();
        }

        /// <summary>
        /// Identifica o idioma nativo do jogo via LocaleManager.
        /// </summary>
        public static AppLanguage DetectGameLanguage()
        {
            try
            {
                if (LocaleManager.instance != null)
                {
                    string lang = LocaleManager.instance.language;
                    return ParseLanguageCode(lang);
                }
            }
            catch
            {
                // Fallback defensivo
            }

            return AppLanguage.English;
        }

        /// <summary>
        /// Mapeia o código de idioma do Cities: Skylines para o enum AppLanguage.
        /// Regra: pt -> Português, fr -> Francês, es -> Espanhol, outros -> Inglês (fallback).
        /// </summary>
        public static AppLanguage ParseLanguageCode(string code)
        {
            if (string.IsNullOrEmpty(code)) return AppLanguage.English;

            string lower = code.Trim().ToLowerInvariant();

            if (lower.StartsWith("pt"))
            {
                return AppLanguage.Portuguese;
            }
            if (lower.StartsWith("fr"))
            {
                return AppLanguage.French;
            }
            if (lower.StartsWith("es"))
            {
                return AppLanguage.Spanish;
            }

            // Qualquer outro idioma (en, de, ru, zh, ja, etc.) utiliza Inglês como fallback obrigatório
            return AppLanguage.English;
        }

        /// <summary>
        /// Retorna a instrução de idioma para o system prompt da DeepSeek.
        /// </summary>
        public static string GetAiLanguageInstruction()
        {
            switch (CurrentLanguage)
            {
                case AppLanguage.Portuguese:
                    return "Responda estritamente em Português Brasileiro.";
                case AppLanguage.French:
                    return "Répondez strictement en Français.";
                case AppLanguage.Spanish:
                    return "Responda estrictamente en Español.";
                case AppLanguage.English:
                default:
                    return "Respond strictly in English.";
            }
        }

        /// <summary>
        /// Dicionário estático estruturado de traduções para performance máxima.
        /// </summary>
        private static readonly Dictionary<string, Dictionary<AppLanguage, string>> _translations = new Dictionary<string, Dictionary<AppLanguage, string>>
        {
            // Títulos e Cabeçalhos
            ["PanelTitle"] = new Dictionary<AppLanguage, string>
            {
                [AppLanguage.Portuguese] = "🚦 City & Traffic AI Optimizer",
                [AppLanguage.English] = "🚦 City & Traffic AI Optimizer",
                [AppLanguage.French] = "🚦 City & Traffic AI Optimizer",
                [AppLanguage.Spanish] = "🚦 City & Traffic AI Optimizer"
            },
            ["SecHeader"] = new Dictionary<AppLanguage, string>
            {
                [AppLanguage.Portuguese] = "🔒 Segurança: Chave de API (100% Volátil em Memória RAM)",
                [AppLanguage.English] = "🔒 Security: API Key (100% Volatile in RAM)",
                [AppLanguage.French] = "🔒 Sécurité : Clé d'API (100% Volatile en Mémoire RAM)",
                [AppLanguage.Spanish] = "🔒 Seguridad: Clave DeepSeek (100% Volátil en Memoria RAM)"
            },
            ["SetKeyBtn"] = new Dictionary<AppLanguage, string>
            {
                [AppLanguage.Portuguese] = "Definir na RAM",
                [AppLanguage.English] = "Set in RAM",
                [AppLanguage.French] = "Définir en RAM",
                [AppLanguage.Spanish] = "Guardar en RAM"
            },
            ["ClearKeyBtn"] = new Dictionary<AppLanguage, string>
            {
                [AppLanguage.Portuguese] = "🗑 Limpar RAM",
                [AppLanguage.English] = "🗑 Clear RAM",
                [AppLanguage.French] = "🗑 Vider la RAM",
                [AppLanguage.Spanish] = "🗑 Borrar RAM"
            },
            ["ProviderLabel"] = new Dictionary<AppLanguage, string>
            {
                [AppLanguage.Portuguese] = "Provedor",
                [AppLanguage.English] = "Provider",
                [AppLanguage.French] = "Fournisseur",
                [AppLanguage.Spanish] = "Proveedor"
            },
            ["ModelLabel"] = new Dictionary<AppLanguage, string>
            {
                [AppLanguage.Portuguese] = "Modelo",
                [AppLanguage.English] = "Model",
                [AppLanguage.French] = "Modèle",
                [AppLanguage.Spanish] = "Modelo"
            },
            ["FetchModelsBtn"] = new Dictionary<AppLanguage, string>
            {
                [AppLanguage.Portuguese] = "🔄 Buscar Modelos",
                [AppLanguage.English] = "🔄 Fetch Models",
                [AppLanguage.French] = "🔄 Obtenir Modèles",
                [AppLanguage.Spanish] = "🔄 Obtener Modelos"
            },
            ["CustomModelBtn"] = new Dictionary<AppLanguage, string>
            {
                [AppLanguage.Portuguese] = "➕ Outro Modelo",
                [AppLanguage.English] = "➕ Other Model",
                [AppLanguage.French] = "➕ Autre Modèle",
                [AppLanguage.Spanish] = "➕ Otro Modelo"
            },
            ["ModelChat"] = new Dictionary<AppLanguage, string>
            {
                [AppLanguage.Portuguese] = "Modelo: Chat (V3)",
                [AppLanguage.English] = "Model: Chat (V3)",
                [AppLanguage.French] = "Modèle : Chat (V3)",
                [AppLanguage.Spanish] = "Modelo: Chat (V3)"
            },
            ["ModelReasoner"] = new Dictionary<AppLanguage, string>
            {
                [AppLanguage.Portuguese] = "Modelo: Reasoner (R1)",
                [AppLanguage.English] = "Model: Reasoner (R1)",
                [AppLanguage.French] = "Modèle : Reasoner (R1)",
                [AppLanguage.Spanish] = "Modelo: Reasoner (R1)"
            },

            // Abas
            ["TabTraffic"] = new Dictionary<AppLanguage, string>
            {
                [AppLanguage.Portuguese] = "🚦 Tráfego",
                [AppLanguage.English] = "🚦 Traffic",
                [AppLanguage.French] = "🚦 Trafic",
                [AppLanguage.Spanish] = "🚦 Tráfico"
            },
            ["TabCityAlerts"] = new Dictionary<AppLanguage, string>
            {
                [AppLanguage.Portuguese] = "🏢 Alertas da Cidade",
                [AppLanguage.English] = "🏢 City Alerts",
                [AppLanguage.French] = "🏢 Alertes Ville",
                [AppLanguage.Spanish] = "🏢 Alertas Ciudad"
            },
            ["TabLog"] = new Dictionary<AppLanguage, string>
            {
                [AppLanguage.Portuguese] = "📋 Log & Alternativas (10m)",
                [AppLanguage.English] = "📋 Log & Alternatives (10m)",
                [AppLanguage.French] = "📋 Journal & Alternatives (10m)",
                [AppLanguage.Spanish] = "📋 Registro & Alternativas (10m)"
            },

            // Botões de Ação
            ["AnalyzeTraffic"] = new Dictionary<AppLanguage, string>
            {
                [AppLanguage.Portuguese] = "⚡ Diagnóstico Local",
                [AppLanguage.English] = "⚡ Local Diagnosis",
                [AppLanguage.French] = "⚡ Diagnostic Local",
                [AppLanguage.Spanish] = "⚡ Diagnóstico Local"
            },
            ["FixAllTrafficBtn"] = new Dictionary<AppLanguage, string>
            {
                [AppLanguage.Portuguese] = "🚀 Corrigir Toda a Lista",
                [AppLanguage.English] = "🚀 Fix All Bottlenecks",
                [AppLanguage.French] = "🚀 Corriger Toute la Liste",
                [AppLanguage.Spanish] = "🚀 Corregir Toda la Lista"
            },
            ["AnalyzeAlerts"] = new Dictionary<AppLanguage, string>
            {
                [AppLanguage.Portuguese] = "⚡ Diagnóstico Local",
                [AppLanguage.English] = "⚡ Local Diagnosis",
                [AppLanguage.French] = "⚡ Diagnostic Local",
                [AppLanguage.Spanish] = "⚡ Diagnóstico Local"
            },
            ["FixAllAlertsBtn"] = new Dictionary<AppLanguage, string>
            {
                [AppLanguage.Portuguese] = "🚀 Corrigir Toda a Lista",
                [AppLanguage.English] = "🚀 Fix All City Alerts",
                [AppLanguage.French] = "🚀 Corriger Toutes les Alertes",
                [AppLanguage.Spanish] = "🚀 Corregir Todas las Alertas"
            },
            ["Reanalyze"] = new Dictionary<AppLanguage, string>
            {
                [AppLanguage.Portuguese] = "🤖 Consultar IA",
                [AppLanguage.English] = "🤖 Consult AI",
                [AppLanguage.French] = "🤖 Consulter l'IA",
                [AppLanguage.Spanish] = "🤖 Consultar IA"
            },
            ["Clear"] = new Dictionary<AppLanguage, string>
            {
                [AppLanguage.Portuguese] = "🧹 Limpar",
                [AppLanguage.English] = "🧹 Clear",
                [AppLanguage.French] = "🧹 Effacer",
                [AppLanguage.Spanish] = "🧹 Limpiar"
            },
            ["ReanalyzeLogDemand"] = new Dictionary<AppLanguage, string>
            {
                [AppLanguage.Portuguese] = "🔄 Reavaliar Correções Sob Demanda",
                [AppLanguage.English] = "🔄 Re-evaluate Fixes On Demand",
                [AppLanguage.French] = "🔄 Réévaluer les Correctifs",
                [AppLanguage.Spanish] = "🔄 Reevaluar Correcciones"
            },
            ["ClearHistory"] = new Dictionary<AppLanguage, string>
            {
                [AppLanguage.Portuguese] = "🧹 Limpar Histórico",
                [AppLanguage.English] = "🧹 Clear History",
                [AppLanguage.French] = "🧹 Effacer l'historique",
                [AppLanguage.Spanish] = "🧹 Borrar Historial"
            },
            ["ApplyTrafficFix"] = new Dictionary<AppLanguage, string>
            {
                [AppLanguage.Portuguese] = "🛠 Aplicar Ajustes Automáticos",
                [AppLanguage.English] = "🛠 Apply Automated Fixes",
                [AppLanguage.French] = "🛠 Appliquer les Ajustements",
                [AppLanguage.Spanish] = "🛠 Aplicar Ajustes Automáticos"
            },
            ["ApplyCityAlertFix"] = new Dictionary<AppLanguage, string>
            {
                [AppLanguage.Portuguese] = "🛠 Aplicar Resolução Municipal",
                [AppLanguage.English] = "🛠 Apply Municipal Resolution",
                [AppLanguage.French] = "🛠 Appliquer la Résolution",
                [AppLanguage.Spanish] = "🛠 Aplicar Resolución Municipal"
            },
            ["RequestAlternativeBtn"] = new Dictionary<AppLanguage, string>
            {
                [AppLanguage.Portuguese] = "💡 Pesquisar Solução Alternativa com IA",
                [AppLanguage.English] = "💡 Research Alternative Solution with AI",
                [AppLanguage.French] = "💡 Rechercher une Solution Alternative avec l'IA",
                [AppLanguage.Spanish] = "💡 Buscar Solución Alternativa con IA"
            },
            ["ApplyAlternativeBtn"] = new Dictionary<AppLanguage, string>
            {
                [AppLanguage.Portuguese] = "⚡ Aplicar Solução Alternativa",
                [AppLanguage.English] = "⚡ Apply Alternative Solution",
                [AppLanguage.French] = "⚡ Appliquer la Solution Alternative",
                [AppLanguage.Spanish] = "⚡ Aplicar Solución Alternativa"
            },
            ["AntiSingleLaneBtn"] = new Dictionary<AppLanguage, string>
            {
                [AppLanguage.Portuguese] = "⚡ Redistribuir Faixas & Re-rotear (Anti-Fila)",
                [AppLanguage.English] = "⚡ Redistribute Lanes & Reroute (Anti-Single Lane)",
                [AppLanguage.French] = "⚡ Redistribuer Voies & Re-router (Anti-File)",
                [AppLanguage.Spanish] = "⚡ Redistribuir Carriles & Re-enrutar (Anti-Fila)"
            },
            ["SingleLaneBadge"] = new Dictionary<AppLanguage, string>
            {
                [AppLanguage.Portuguese] = "Fila Indiana ⚠️ ({0} faixas)",
                [AppLanguage.English] = "Single Lane ⚠️ ({0} lanes)",
                [AppLanguage.French] = "File Indienne ⚠️ ({0} voies)",
                [AppLanguage.Spanish] = "Fila India ⚠️ ({0} carriles)"
            },

            // Placeholders e Mensagens de Detalhe
            ["SelectBottleneckHint"] = new Dictionary<AppLanguage, string>
            {
                [AppLanguage.Portuguese] = "Selecione um gargalo da lista acima para ver o diagnóstico.",
                [AppLanguage.English] = "Select a bottleneck from the list above to view diagnosis.",
                [AppLanguage.French] = "Sélectionnez un goulot d'étranglement ci-dessus pour voir le diagnostic.",
                [AppLanguage.Spanish] = "Seleccione un cuello de botella arriba para ver el diagnóstico."
            },
            ["SelectAlertHint"] = new Dictionary<AppLanguage, string>
            {
                [AppLanguage.Portuguese] = "Selecione um alerta urbano acima (Cadáver, Compradores, Água, Luz, etc.).",
                [AppLanguage.English] = "Select a city alert above (Hearse, Customers, Water, Power, etc.).",
                [AppLanguage.French] = "Sélectionnez une alerte ci-dessus (Corbillard, Clients, Eau, Électricité, etc.).",
                [AppLanguage.Spanish] = "Seleccione una alerta arriba (Cadáver, Clientes, Agua, Luz, etc.)."
            },
            ["SelectLogHint"] = new Dictionary<AppLanguage, string>
            {
                [AppLanguage.Portuguese] = "Selecione uma correção para acompanhar ou pesquisar solução alternativa (>10 min).",
                [AppLanguage.English] = "Select a correction to track or research alternative solution (>10 min).",
                [AppLanguage.French] = "Sélectionnez un correctif à suivre ou cherchez une alternative (>10 min).",
                [AppLanguage.Spanish] = "Seleccione una corrección para monitorear o buscar alternativa (>10 min)."
            },
            ["NoLogsRecorded"] = new Dictionary<AppLanguage, string>
            {
                [AppLanguage.Portuguese] = "Nenhuma correção registrada ainda. Aplique correções no trânsito ou alertas para monitorar.",
                [AppLanguage.English] = "No corrections recorded yet. Apply traffic or city fixes to monitor.",
                [AppLanguage.French] = "Aucun correctif enregistré. Appliquez des actions pour surveiller.",
                [AppLanguage.Spanish] = "Aún no hay correcciones registradas. Aplique ajustes para monitorear."
            },

            // Descrições de Alertas Urbanos
            ["AlertDeath"] = new Dictionary<AppLanguage, string>
            {
                [AppLanguage.Portuguese] = "Aguardando transporte de cadáver",
                [AppLanguage.English] = "Waiting for hearse / death care",
                [AppLanguage.French] = "En attente de corbillard",
                [AppLanguage.Spanish] = "Esperando coche fúnebre"
            },
            ["AlertNoCustomers"] = new Dictionary<AppLanguage, string>
            {
                [AppLanguage.Portuguese] = "Poucos compradores comerciais",
                [AppLanguage.English] = "Not enough commercial customers",
                [AppLanguage.French] = "Pas assez de clients commerciaux",
                [AppLanguage.Spanish] = "Pocos clientes comerciales"
            },
            ["AlertNoWorkers"] = new Dictionary<AppLanguage, string>
            {
                [AppLanguage.Portuguese] = "Poucos trabalhadores para operação",
                [AppLanguage.English] = "Not enough workers for operation",
                [AppLanguage.French] = "Pas assez de travailleurs",
                [AppLanguage.Spanish] = "Pocos trabajadores"
            },
            ["AlertWater"] = new Dictionary<AppLanguage, string>
            {
                [AppLanguage.Portuguese] = "Falta de abastecimento de água",
                [AppLanguage.English] = "Lack of drinking water supply",
                [AppLanguage.French] = "Manque d'approvisionnement en eau",
                [AppLanguage.Spanish] = "Falta de suministro de agua"
            },
            ["AlertSewage"] = new Dictionary<AppLanguage, string>
            {
                [AppLanguage.Portuguese] = "Esgoto acumulado / sem conexão",
                [AppLanguage.English] = "Sewage backup / disconnected",
                [AppLanguage.French] = "Eaux usées refoulées / non raccordé",
                [AppLanguage.Spanish] = "Aguas residuales atascadas / sin conexión"
            },
            ["AlertElectricity"] = new Dictionary<AppLanguage, string>
            {
                [AppLanguage.Portuguese] = "Falta de energia elétrica",
                [AppLanguage.English] = "Lack of electricity / blackout",
                [AppLanguage.French] = "Panne de courant / manque d'électricité",
                [AppLanguage.Spanish] = "Falta de electricidad"
            },
            ["AlertGarbage"] = new Dictionary<AppLanguage, string>
            {
                [AppLanguage.Portuguese] = "Lixo acumulado sem coleta",
                [AppLanguage.English] = "Garbage piling up without collection",
                [AppLanguage.French] = "Déchets accumulés sans collecte",
                [AppLanguage.Spanish] = "Basura acumulada sin recolección"
            },
            ["AlertFire"] = new Dictionary<AppLanguage, string>
            {
                [AppLanguage.Portuguese] = "Edifício em chamas (Incêndio)",
                [AppLanguage.English] = "Building on fire",
                [AppLanguage.French] = "Bâtiment en flammes (Incendie)",
                [AppLanguage.Spanish] = "Edificio en llamas (Incendio)"
            },
            ["AlertCrime"] = new Dictionary<AppLanguage, string>
            {
                [AppLanguage.Portuguese] = "Alto índice de criminalidade",
                [AppLanguage.English] = "High local crime rate",
                [AppLanguage.French] = "Taux de criminalité élevé",
                [AppLanguage.Spanish] = "Alta tasa de criminalidad"
            },

            // Status de Auditoria
            ["StatusActive"] = new Dictionary<AppLanguage, string>
            {
                [AppLanguage.Portuguese] = "Ativo ({0})",
                [AppLanguage.English] = "Active ({0})",
                [AppLanguage.French] = "Actif ({0})",
                [AppLanguage.Spanish] = "Activo ({0})"
            },
            ["StatusResolved"] = new Dictionary<AppLanguage, string>
            {
                [AppLanguage.Portuguese] = "Resolvido",
                [AppLanguage.English] = "Resolved",
                [AppLanguage.French] = "Résolu",
                [AppLanguage.Spanish] = "Resuelto"
            },
            ["StatusPersistentExceeded"] = new Dictionary<AppLanguage, string>
            {
                [AppLanguage.Portuguese] = "⚠️ Persiste (>10 min: {0})",
                [AppLanguage.English] = "⚠️ Persisting (>10 min: {0})",
                [AppLanguage.French] = "⚠️ Persiste (>10 min : {0})",
                [AppLanguage.Spanish] = "⚠️ Persiste (>10 min: {0})"
            },
            ["StatusAlternativeRequested"] = new Dictionary<AppLanguage, string>
            {
                [AppLanguage.Portuguese] = "Pesquisando Alternativa...",
                [AppLanguage.English] = "Researching Alternative...",
                [AppLanguage.French] = "Recherche d'Alternative...",
                [AppLanguage.Spanish] = "Buscando Alternativa..."
            },
            ["StatusAlternativeReady"] = new Dictionary<AppLanguage, string>
            {
                [AppLanguage.Portuguese] = "Alternativa Disponível!",
                [AppLanguage.English] = "Alternative Available!",
                [AppLanguage.French] = "Alternative Disponible !",
                [AppLanguage.Spanish] = "¡Alternativa Disponible!"
            },
            ["StatusAlternativeApplied"] = new Dictionary<AppLanguage, string>
            {
                [AppLanguage.Portuguese] = "Alternativa Aplicada",
                [AppLanguage.English] = "Alternative Applied",
                [AppLanguage.French] = "Alternative Appliquée",
                [AppLanguage.Spanish] = "Alternativa Aplicada"
            },

            // Feedback e Mensagens do Sistema
            ["ReadyStatus"] = new Dictionary<AppLanguage, string>
            {
                [AppLanguage.Portuguese] = "Pronto. Selecione uma aba e clique em Analisar para iniciar.",
                [AppLanguage.English] = "Ready. Select a tab and click Analyze to begin.",
                [AppLanguage.French] = "Prêt. Sélectionnez un onglet et cliquez sur Analyser pour commencer.",
                [AppLanguage.Spanish] = "Listo. Seleccione una pestaña y haga clic en Analizar para comenzar."
            },
            ["KeySavedSuccess"] = new Dictionary<AppLanguage, string>
            {
                [AppLanguage.Portuguese] = "Chave armazenada na memória RAM com sucesso (protegida contra disco/logs).",
                [AppLanguage.English] = "Key stored safely in RAM memory (protected from disk/logs).",
                [AppLanguage.French] = "Clé stockée en RAM avec succès (protégée contre les disques/logs).",
                [AppLanguage.Spanish] = "Clave guardada en RAM con éxito (protegida contra disco/logs)."
            },
            ["KeyClearedSuccess"] = new Dictionary<AppLanguage, string>
            {
                [AppLanguage.Portuguese] = "Chave removida permanentemente da memória RAM.",
                [AppLanguage.English] = "Key permanently cleared from RAM memory.",
                [AppLanguage.French] = "Clé supprimée définitivement de la mémoire RAM.",
                [AppLanguage.Spanish] = "Clave eliminada permanentemente de la memoria RAM."
            },
            ["KeyMissingError"] = new Dictionary<AppLanguage, string>
            {
                [AppLanguage.Portuguese] = "Defina sua API Key da DeepSeek na RAM antes de realizar a análise.",
                [AppLanguage.English] = "Please set your DeepSeek API Key in RAM before running analysis.",
                [AppLanguage.French] = "Veuillez définir votre clé API DeepSeek en RAM avant l'analyse.",
                [AppLanguage.Spanish] = "Guarde su clave de DeepSeek en RAM antes de realizar el análisis."
            },
            ["ScanningTraffic"] = new Dictionary<AppLanguage, string>
            {
                [AppLanguage.Portuguese] = "Escaneando malha viária e veículos...",
                [AppLanguage.English] = "Scanning road network and vehicles...",
                [AppLanguage.French] = "Analyse du réseau routier et des véhicules...",
                [AppLanguage.Spanish] = "Escaneando red vial y vehículos..."
            },
            ["ScanningAlerts"] = new Dictionary<AppLanguage, string>
            {
                [AppLanguage.Portuguese] = "Varrendo edifícios e serviços municipais...",
                [AppLanguage.English] = "Scanning buildings and municipal services...",
                [AppLanguage.French] = "Analyse des bâtiments et services municipaux...",
                [AppLanguage.Spanish] = "Escaneando edificios y servicios municipales..."
            },
            ["NoBottlenecksFound"] = new Dictionary<AppLanguage, string>
            {
                [AppLanguage.Portuguese] = "Ótimo! Nenhum gargalo viário crítico detectado no momento.",
                [AppLanguage.English] = "Great! No critical traffic bottlenecks detected right now.",
                [AppLanguage.French] = "Super ! Aucun goulot d'étranglement critique détecté.",
                [AppLanguage.Spanish] = "¡Excelente! No se detectaron cuellos de botella críticos."
            },
            ["NoAlertsFound"] = new Dictionary<AppLanguage, string>
            {
                [AppLanguage.Portuguese] = "Excelente! Nenhum problema crítico de serviços detectado na cidade.",
                [AppLanguage.English] = "Excellent! No critical service issues detected in the city.",
                [AppLanguage.French] = "Excellent ! Aucun problème critique de service détecté.",
                [AppLanguage.Spanish] = "¡Excelente! No se detectaron problemas críticos en la ciudad."
            },
            ["AlternativeSuccessApplied"] = new Dictionary<AppLanguage, string>
            {
                [AppLanguage.Portuguese] = "Solução alternativa avançada aplicada com êxito!",
                [AppLanguage.English] = "Advanced alternative solution applied successfully!",
                [AppLanguage.French] = "Solution alternative avancée appliquée avec succès !",
                [AppLanguage.Spanish] = "¡Solución alternativa avanzada aplicada con éxito!"
            }
        };

        /// <summary>
        /// Recupera a tradução para a chave especificada no idioma ativo.
        /// </summary>
        public static string Get(string key)
        {
            if (string.IsNullOrEmpty(key)) return "";

            if (_translations.TryGetValue(key, out var langDict))
            {
                AppLanguage lang = CurrentLanguage;
                if (langDict.TryGetValue(lang, out var text))
                {
                    return text;
                }
                // Fallback para inglês
                if (langDict.TryGetValue(AppLanguage.English, out var enText))
                {
                    return enText;
                }
            }

            return key;
        }

        /// <summary>
        /// Recupera a tradução com suporte a formatação string.Format.
        /// </summary>
        public static string Get(string key, params object[] args)
        {
            string raw = Get(key);
            try
            {
                return string.Format(raw, args);
            }
            catch
            {
                return raw;
            }
        }
    }
}
