using System;
using System.Collections.Generic;
using ColossalFramework.UI;
using TrafficAIOptimizer.Analysis;
using TrafficAIOptimizer.Core.Localization;
using TrafficAIOptimizer.Core.Providers;
using TrafficAIOptimizer.Core.Services;
using TrafficAIOptimizer.DeepSeek;
using TrafficAIOptimizer.Integration;
using TrafficAIOptimizer.Security;
using TrafficAIOptimizer.Visualizer;
using UnityEngine;

namespace TrafficAIOptimizer.UI
{
    public enum ActiveTab
    {
        Traffic,
        CityAlerts,
        CorrectionLog
    }

    /// <summary>
    /// Painel de interface de usuário para o mod City & Traffic AI Optimizer.
    /// Suporta múltiplos provedores de IA (DeepSeek, OpenAI, Anthropic, Gemini, Grok, Meta),
    /// múltiplos idiomas (Português, Inglês, Francês, Espanhol) e modelos dinâmicos.
    /// </summary>
    public class TrafficAIPanel : UIPanel
    {
        public static TrafficAIPanel Instance { get; private set; }

        private const float PANEL_WIDTH = 700f;
        private const float PANEL_HEIGHT = 880f;

        // --- Cabeçalho, Provedor e Chave ---
        private UILabel _titleLabel;
        private UILabel _secHeader;
        private UIButton _providerBtn;
        private UIButton _toggleModelBtn;
        private UIButton _fetchModelsBtn;
        private UITextField _keyField;
        private UILabel _keyStatusLabel;
        private UIButton _setKeyBtn;
        private UIButton _clearKeyBtn;
        private UITextField _customModelField;
        private UIButton _addCustomModelBtn;
        private UIButton _clearAllKeysBtn;

        // --- Abas de Navegação ---
        private UIButton _tabTrafficBtn;
        private UIButton _tabCityAlertsBtn;
        private UIButton _tabLogBtn;
        private ActiveTab _currentTab = ActiveTab.Traffic;

        // --- Controles de Tráfego ---
        private UIPanel _trafficTabPanel;
        private UIButton _analyzeTrafficBtn;
        private UIButton _fixAllTrafficBtn;
        private UIButton _reanalyzeTrafficBtn;
        private UIButton _clearTrafficBtn;
        private UIPanel _trafficResultsContainer;
        private List<TrafficBottleneck> _currentBottlenecks = new List<TrafficBottleneck>();
        private TrafficBottleneck _selectedBottleneck;
        private UILabel _trafficDetailTitle;
        private UILabel _trafficDetailCause;
        private UILabel _trafficDetailRec;
        private UIButton _applyTrafficFixBtn;
        private UIButton _redistributeLanesBtn;

        // --- Controles de Alertas da Cidade ---
        private UIPanel _cityAlertsTabPanel;
        private UIButton _analyzeAlertsBtn;
        private UIButton _fixAllAlertsBtn;
        private UIButton _reanalyzeAlertsBtn;
        private UIButton _clearAlertsBtn;
        private UIPanel _alertsResultsContainer;
        private List<CityAlert> _currentAlerts = new List<CityAlert>();
        private CityAlert _selectedAlert;
        private UILabel _alertDetailTitle;
        private UILabel _alertDetailCause;
        private UILabel _alertDetailRec;
        private UIButton _applyAlertFixBtn;

        // --- Controles de Log de Correções (10 min) ---
        private UIPanel _logTabPanel;
        private UIButton _refreshLogBtn;
        private UIButton _clearLogBtn;
        private UIPanel _logResultsContainer;
        private CorrectionLogEntry _selectedLogEntry;
        private UILabel _logDetailTitle;
        private UILabel _logDetailInfo;
        private UILabel _logDetailAlternative;
        private UIButton _requestAlternativeBtn;
        private UIButton _applyAlternativeBtn;

        // Status Geral
        private UILabel _statusLabel;

        public override void Awake()
        {
            base.Awake();
            Instance = this;
        }

        public override void Start()
        {
            base.Start();

            try
            {
                // Configuração da Janela Principal
                width = PANEL_WIDTH;
                height = PANEL_HEIGHT;
                backgroundSprite = "MenuPanel2";
                canFocus = true;
                isInteractive = true;
                isVisible = false;
                relativePosition = new Vector3(50f, 50f);

                // 1. Barra de Título com DragHandle
                UIPanel titleBar = AddUIComponent<UIPanel>();
                titleBar.width = width;
                titleBar.height = 40f;
                titleBar.relativePosition = Vector3.zero;

                UIDragHandle dragHandle = titleBar.AddUIComponent<UIDragHandle>();
                dragHandle.width = width - 40f;
                dragHandle.height = 40f;
                dragHandle.relativePosition = Vector3.zero;
                dragHandle.target = this;

                _titleLabel = titleBar.AddUIComponent<UILabel>();
                _titleLabel.text = LocalizationManager.Get("PanelTitle");
                _titleLabel.textScale = 1.0f;
                _titleLabel.textColor = new Color32(255, 215, 0, 255);
                _titleLabel.relativePosition = new Vector3(15f, 10f);

                UIButton closeBtn = titleBar.AddUIComponent<UIButton>();
                closeBtn.text = "✕";
                closeBtn.textScale = 0.9f;
                closeBtn.width = 30f;
                closeBtn.height = 30f;
                closeBtn.relativePosition = new Vector3(width - 38f, 5f);
                closeBtn.normalBgSprite = "ButtonMenu";
                closeBtn.hoveredBgSprite = "ButtonMenuHovered";
                closeBtn.eventClick += (c, e) => ToggleVisibility();

                float curY = 44f;

                // 2. Seção de Segurança da API Key e Seleção de Provedor/Modelo
                UIPanel keySection = AddUIComponent<UIPanel>();
                keySection.width = width - 24f;
                keySection.height = 160f;
                keySection.relativePosition = new Vector3(12f, curY);
                keySection.backgroundSprite = "SubPanel";

                _secHeader = keySection.AddUIComponent<UILabel>();
                _secHeader.text = LocalizationManager.Get("SecHeader");
                _secHeader.textScale = 0.82f;
                _secHeader.textColor = new Color32(180, 220, 255, 255);
                _secHeader.relativePosition = new Vector3(12f, 6f);

                // --- Linha 1: Provedor + Modelo + Buscar da API ---
                _providerBtn = keySection.AddUIComponent<UIButton>();
                _providerBtn.text = GetProviderButtonText();
                _providerBtn.width = 165f;
                _providerBtn.height = 28f;
                _providerBtn.relativePosition = new Vector3(12f, 26f);
                _providerBtn.normalBgSprite = "ButtonMenu";
                _providerBtn.hoveredBgSprite = "ButtonMenuHovered";
                _providerBtn.textScale = 0.78f;
                _providerBtn.textColor = new Color32(255, 215, 0, 255);
                _providerBtn.eventClick += (c, e) => OnProviderClicked();

                _toggleModelBtn = keySection.AddUIComponent<UIButton>();
                _toggleModelBtn.text = GetModelButtonText();
                _toggleModelBtn.width = 245f;
                _toggleModelBtn.height = 28f;
                _toggleModelBtn.relativePosition = new Vector3(184f, 26f);
                _toggleModelBtn.normalBgSprite = "ButtonMenu";
                _toggleModelBtn.hoveredBgSprite = "ButtonMenuHovered";
                _toggleModelBtn.textScale = 0.78f;
                _toggleModelBtn.textColor = new Color32(180, 230, 255, 255);
                _toggleModelBtn.eventClick += (c, e) => OnToggleModelClicked();

                _fetchModelsBtn = keySection.AddUIComponent<UIButton>();
                _fetchModelsBtn.text = LocalizationManager.Get("FetchModelsBtn");
                _fetchModelsBtn.width = 145f;
                _fetchModelsBtn.height = 28f;
                _fetchModelsBtn.relativePosition = new Vector3(keySection.width - 157f, 26f);
                _fetchModelsBtn.normalBgSprite = "ButtonMenu";
                _fetchModelsBtn.hoveredBgSprite = "ButtonMenuHovered";
                _fetchModelsBtn.textScale = 0.75f;
                _fetchModelsBtn.textColor = new Color32(140, 255, 170, 255);
                _fetchModelsBtn.eventClick += (c, e) => OnFetchModelsClicked();

                // --- Linha 2: Campo da Chave Mascarada + Botão Definir ---
                _keyField = keySection.AddUIComponent<UITextField>();
                _keyField.width = keySection.width - 165f;
                _keyField.height = 30f;
                _keyField.relativePosition = new Vector3(12f, 58f);
                _keyField.builtinKeyNavigation = true;
                _keyField.isPasswordField = true;
                _keyField.text = "";
                _keyField.normalBgSprite = "TextField";
                _keyField.hoveredBgSprite = "TextFieldHover";
                _keyField.focusedBgSprite = "TextFieldHover";
                _keyField.selectionSprite = "EmptySprite";
                _keyField.textScale = 0.85f;
                _keyField.textColor = new Color32(245, 250, 255, 255);
                _keyField.color = new Color32(42, 52, 68, 255);
                _keyField.padding = new RectOffset(8, 8, 7, 7);
                _keyField.cursorBlinkTime = 0.45f;
                _keyField.cursorWidth = 2;

                _setKeyBtn = keySection.AddUIComponent<UIButton>();
                _setKeyBtn.text = LocalizationManager.Get("SetKeyBtn");
                _setKeyBtn.width = 145f;
                _setKeyBtn.height = 30f;
                _setKeyBtn.relativePosition = new Vector3(keySection.width - 157f, 58f);
                _setKeyBtn.normalBgSprite = "ButtonMenu";
                _setKeyBtn.hoveredBgSprite = "ButtonMenuHovered";
                _setKeyBtn.textScale = 0.8f;
                _setKeyBtn.eventClick += (c, e) => OnSetKeyClicked();

                // --- Linha 3: Status da Chave Ativa ---
                _keyStatusLabel = keySection.AddUIComponent<UILabel>();
                _keyStatusLabel.text = GetKeyStatusText();
                _keyStatusLabel.textScale = 0.78f;
                _keyStatusLabel.textColor = Color.gray;
                _keyStatusLabel.relativePosition = new Vector3(12f, 94f);

                // --- Linha 4: Limpar Chave + Entrada de Modelo Customizado ---
                _clearKeyBtn = keySection.AddUIComponent<UIButton>();
                _clearKeyBtn.text = LocalizationManager.Get("ClearKeyBtn");
                _clearKeyBtn.width = 110f;
                _clearKeyBtn.height = 26f;
                _clearKeyBtn.relativePosition = new Vector3(12f, 120f);
                _clearKeyBtn.normalBgSprite = "ButtonMenu";
                _clearKeyBtn.hoveredBgSprite = "ButtonMenuHovered";
                _clearKeyBtn.textScale = 0.75f;
                _clearKeyBtn.eventClick += (c, e) => OnClearKeyClicked();

                _customModelField = keySection.AddUIComponent<UITextField>();
                _customModelField.width = 240f;
                _customModelField.height = 26f;
                _customModelField.relativePosition = new Vector3(130f, 120f);
                _customModelField.builtinKeyNavigation = true;
                _customModelField.normalBgSprite = "TextField";
                _customModelField.hoveredBgSprite = "TextFieldHover";
                _customModelField.focusedBgSprite = "TextFieldHover";
                _customModelField.selectionSprite = "EmptySprite";
                _customModelField.textScale = 0.75f;
                _customModelField.textColor = new Color32(230, 240, 255, 255);
                _customModelField.color = new Color32(35, 45, 60, 255);
                _customModelField.padding = new RectOffset(6, 6, 5, 5);

                _addCustomModelBtn = keySection.AddUIComponent<UIButton>();
                _addCustomModelBtn.text = LocalizationManager.Get("CustomModelBtn");
                _addCustomModelBtn.width = 110f;
                _addCustomModelBtn.height = 26f;
                _addCustomModelBtn.relativePosition = new Vector3(378f, 120f);
                _addCustomModelBtn.normalBgSprite = "ButtonMenu";
                _addCustomModelBtn.hoveredBgSprite = "ButtonMenuHovered";
                _addCustomModelBtn.textScale = 0.73f;
                _addCustomModelBtn.textColor = new Color32(255, 200, 100, 255);
                _addCustomModelBtn.eventClick += (c, e) => OnAddCustomModelClicked();

                _clearAllKeysBtn = keySection.AddUIComponent<UIButton>();
                _clearAllKeysBtn.text = "Limpar Tudo";
                _clearAllKeysBtn.width = 90f;
                _clearAllKeysBtn.height = 26f;
                _clearAllKeysBtn.relativePosition = new Vector3(keySection.width - 102f, 120f);
                _clearAllKeysBtn.normalBgSprite = "ButtonMenu";
                _clearAllKeysBtn.hoveredBgSprite = "ButtonMenuHovered";
                _clearAllKeysBtn.textScale = 0.73f;
                _clearAllKeysBtn.eventClick += (c, e) => OnClearAllKeysClicked();

                curY += 168f;

                // 3. Barra de Abas de Navegação
                float tabWidth = (width - 24f - 12f) / 3f;

                _tabTrafficBtn = AddUIComponent<UIButton>();
                _tabTrafficBtn.text = LocalizationManager.Get("TabTraffic");
                _tabTrafficBtn.width = tabWidth;
                _tabTrafficBtn.height = 32f;
                _tabTrafficBtn.relativePosition = new Vector3(12f, curY);
                _tabTrafficBtn.normalBgSprite = "ButtonMenu";
                _tabTrafficBtn.hoveredBgSprite = "ButtonMenuHovered";
                _tabTrafficBtn.textScale = 0.85f;
                _tabTrafficBtn.eventClick += (c, e) => SwitchTab(ActiveTab.Traffic);

                _tabCityAlertsBtn = AddUIComponent<UIButton>();
                _tabCityAlertsBtn.text = LocalizationManager.Get("TabCityAlerts");
                _tabCityAlertsBtn.width = tabWidth;
                _tabCityAlertsBtn.height = 32f;
                _tabCityAlertsBtn.relativePosition = new Vector3(12f + tabWidth + 6f, curY);
                _tabCityAlertsBtn.normalBgSprite = "ButtonMenu";
                _tabCityAlertsBtn.hoveredBgSprite = "ButtonMenuHovered";
                _tabCityAlertsBtn.textScale = 0.85f;
                _tabCityAlertsBtn.eventClick += (c, e) => SwitchTab(ActiveTab.CityAlerts);

                _tabLogBtn = AddUIComponent<UIButton>();
                _tabLogBtn.text = LocalizationManager.Get("TabLog");
                _tabLogBtn.width = tabWidth;
                _tabLogBtn.height = 32f;
                _tabLogBtn.relativePosition = new Vector3(12f + (tabWidth + 6f) * 2f, curY);
                _tabLogBtn.normalBgSprite = "ButtonMenu";
                _tabLogBtn.hoveredBgSprite = "ButtonMenuHovered";
                _tabLogBtn.textScale = 0.82f;
                _tabLogBtn.eventClick += (c, e) => SwitchTab(ActiveTab.CorrectionLog);

                curY += 38f;

                // Status Label compartilhado
                _statusLabel = AddUIComponent<UILabel>();
                _statusLabel.text = LocalizationManager.Get("ReadyStatus");
                _statusLabel.textScale = 0.8f;
                _statusLabel.textColor = Color.white;
                _statusLabel.width = width - 28f;
                _statusLabel.autoHeight = true;
                _statusLabel.wordWrap = true;
                _statusLabel.relativePosition = new Vector3(14f, curY);

                curY += 34f;

                // --- 4. Inicialização dos Contêineres de Cada Aba ---
                SetupTrafficTab(curY);
                SetupCityAlertsTab(curY);
                SetupLogTab(curY);

                SwitchTab(ActiveTab.Traffic);
                UpdateKeyDisplay();

                // Inscreve no evento dinâmico de troca de idioma do jogo
                LocalizationManager.OnLanguageChanged += UpdateLocalizedTexts;
            }
            catch (Exception ex)
            {
                Debug.LogError("[TrafficAI] Falha na inicialização da UI: " + ex.Message);
            }
        }

        public override void OnDestroy()
        {
            LocalizationManager.OnLanguageChanged -= UpdateLocalizedTexts;
            base.OnDestroy();
        }

        /// <summary>
        /// Atualiza dinamicamente os textos de toda a interface conforme o idioma configurado no jogo.
        /// </summary>
        public void UpdateLocalizedTexts()
        {
            if (_titleLabel != null) _titleLabel.text = LocalizationManager.Get("PanelTitle");
            if (_secHeader != null) _secHeader.text = LocalizationManager.Get("SecHeader");
            if (_providerBtn != null) _providerBtn.text = GetProviderButtonText();
            if (_toggleModelBtn != null) _toggleModelBtn.text = GetModelButtonText();
            if (_fetchModelsBtn != null) _fetchModelsBtn.text = LocalizationManager.Get("FetchModelsBtn");
            if (_setKeyBtn != null) _setKeyBtn.text = LocalizationManager.Get("SetKeyBtn");
            if (_clearKeyBtn != null) _clearKeyBtn.text = LocalizationManager.Get("ClearKeyBtn");
            if (_addCustomModelBtn != null) _addCustomModelBtn.text = LocalizationManager.Get("CustomModelBtn");

            if (_tabTrafficBtn != null) _tabTrafficBtn.text = LocalizationManager.Get("TabTraffic");
            if (_tabCityAlertsBtn != null) _tabCityAlertsBtn.text = LocalizationManager.Get("TabCityAlerts");
            if (_tabLogBtn != null) _tabLogBtn.text = LocalizationManager.Get("TabLog");

            if (_analyzeTrafficBtn != null) _analyzeTrafficBtn.text = LocalizationManager.Get("AnalyzeTraffic");
            if (_fixAllTrafficBtn != null) _fixAllTrafficBtn.text = LocalizationManager.Get("FixAllTrafficBtn");
            if (_reanalyzeTrafficBtn != null) _reanalyzeTrafficBtn.text = LocalizationManager.Get("Reanalyze");
            if (_clearTrafficBtn != null) _clearTrafficBtn.text = LocalizationManager.Get("Clear");

            if (_analyzeAlertsBtn != null) _analyzeAlertsBtn.text = LocalizationManager.Get("AnalyzeAlerts");
            if (_fixAllAlertsBtn != null) _fixAllAlertsBtn.text = LocalizationManager.Get("FixAllAlertsBtn");
            if (_reanalyzeAlertsBtn != null) _reanalyzeAlertsBtn.text = LocalizationManager.Get("Reanalyze");
            if (_clearAlertsBtn != null) _clearAlertsBtn.text = LocalizationManager.Get("Clear");

            if (_refreshLogBtn != null) _refreshLogBtn.text = LocalizationManager.Get("ReanalyzeLogDemand");
            if (_clearLogBtn != null) _clearLogBtn.text = LocalizationManager.Get("ClearHistory");

            if (_applyTrafficFixBtn != null && _selectedBottleneck == null) _applyTrafficFixBtn.text = LocalizationManager.Get("ApplyTrafficFix");
            if (_redistributeLanesBtn != null) _redistributeLanesBtn.text = LocalizationManager.Get("AntiSingleLaneBtn");
            if (_applyAlertFixBtn != null && _selectedAlert == null) _applyAlertFixBtn.text = LocalizationManager.Get("ApplyCityAlertFix");
            if (_requestAlternativeBtn != null) _requestAlternativeBtn.text = LocalizationManager.Get("RequestAlternativeBtn");
            if (_applyAlternativeBtn != null) _applyAlternativeBtn.text = LocalizationManager.Get("ApplyAlternativeBtn");

            if (_statusLabel != null) _statusLabel.text = LocalizationManager.Get("ReadyStatus");

            if (_currentTab == ActiveTab.CorrectionLog)
            {
                RefreshLogDisplay();
            }
        }

        // ==========================================
        // CONFIGURAÇÃO DA ABA DE TRÁFEGO
        // ==========================================
        private void SetupTrafficTab(float startY)
        {
            _trafficTabPanel = AddUIComponent<UIPanel>();
            _trafficTabPanel.width = width - 24f;
            _trafficTabPanel.height = height - startY - 14f;
            _trafficTabPanel.relativePosition = new Vector3(12f, startY);

            float localY = 0f;

            _analyzeTrafficBtn = _trafficTabPanel.AddUIComponent<UIButton>();
            _analyzeTrafficBtn.text = LocalizationManager.Get("AnalyzeTraffic");
            _analyzeTrafficBtn.width = 150f;
            _analyzeTrafficBtn.height = 34f;
            _analyzeTrafficBtn.relativePosition = new Vector3(0f, localY);
            _analyzeTrafficBtn.normalBgSprite = "ButtonMenu";
            _analyzeTrafficBtn.hoveredBgSprite = "ButtonMenuHovered";
            _analyzeTrafficBtn.textScale = 0.85f;
            _analyzeTrafficBtn.textColor = new Color32(100, 255, 100, 255);
            _analyzeTrafficBtn.eventClick += (c, e) => OnAnalyzeTrafficClicked();

            _fixAllTrafficBtn = _trafficTabPanel.AddUIComponent<UIButton>();
            _fixAllTrafficBtn.text = LocalizationManager.Get("FixAllTrafficBtn");
            _fixAllTrafficBtn.width = 190f;
            _fixAllTrafficBtn.height = 34f;
            _fixAllTrafficBtn.relativePosition = new Vector3(158f, localY);
            _fixAllTrafficBtn.normalBgSprite = "ButtonMenu";
            _fixAllTrafficBtn.hoveredBgSprite = "ButtonMenuHovered";
            _fixAllTrafficBtn.textScale = 0.82f;
            _fixAllTrafficBtn.textColor = new Color32(255, 215, 0, 255);
            _fixAllTrafficBtn.tooltip = "Aplica correções e otimizações viárias em todos os gargalos da lista sem necessidade de análise individual.";
            _fixAllTrafficBtn.eventClick += (c, e) => OnFixAllTrafficClicked();

            _reanalyzeTrafficBtn = _trafficTabPanel.AddUIComponent<UIButton>();
            _reanalyzeTrafficBtn.text = LocalizationManager.Get("Reanalyze");
            _reanalyzeTrafficBtn.width = 145f;
            _reanalyzeTrafficBtn.height = 34f;
            _reanalyzeTrafficBtn.relativePosition = new Vector3(356f, localY);
            _reanalyzeTrafficBtn.normalBgSprite = "ButtonMenu";
            _reanalyzeTrafficBtn.hoveredBgSprite = "ButtonMenuHovered";
            _reanalyzeTrafficBtn.textScale = 0.85f;
            _reanalyzeTrafficBtn.textColor = new Color32(150, 220, 255, 255);
            _reanalyzeTrafficBtn.eventClick += (c, e) => OnAnalyzeTrafficClicked();

            _clearTrafficBtn = _trafficTabPanel.AddUIComponent<UIButton>();
            _clearTrafficBtn.text = LocalizationManager.Get("Clear");
            _clearTrafficBtn.width = 150f;
            _clearTrafficBtn.height = 34f;
            _clearTrafficBtn.relativePosition = new Vector3(510f, localY);
            _clearTrafficBtn.normalBgSprite = "ButtonMenu";
            _clearTrafficBtn.hoveredBgSprite = "ButtonMenuHovered";
            _clearTrafficBtn.textScale = 0.85f;
            _clearTrafficBtn.textColor = new Color32(255, 160, 160, 255);
            _clearTrafficBtn.eventClick += (c, e) => OnClearTrafficClicked();

            localY += 40f;

            _trafficResultsContainer = _trafficTabPanel.AddUIComponent<UIPanel>();
            _trafficResultsContainer.width = _trafficTabPanel.width;
            _trafficResultsContainer.height = 232f;
            _trafficResultsContainer.relativePosition = new Vector3(0f, localY);
            _trafficResultsContainer.backgroundSprite = "SubPanel";
            EnableMouseWheelScrolling(_trafficResultsContainer);

            localY += 240f;

            UIPanel detailsPanel = _trafficTabPanel.AddUIComponent<UIPanel>();
            detailsPanel.width = _trafficTabPanel.width;
            detailsPanel.height = 275f;
            detailsPanel.relativePosition = new Vector3(0f, localY);
            detailsPanel.backgroundSprite = "SubPanel";
            detailsPanel.clipChildren = true;

            _trafficDetailTitle = detailsPanel.AddUIComponent<UILabel>();
            _trafficDetailTitle.text = LocalizationManager.Get("SelectBottleneckHint");
            _trafficDetailTitle.textScale = 0.85f;
            _trafficDetailTitle.textColor = new Color32(255, 200, 100, 255);
            _trafficDetailTitle.width = detailsPanel.width - 24f;
            _trafficDetailTitle.relativePosition = new Vector3(12f, 8f);

            _trafficDetailCause = detailsPanel.AddUIComponent<UILabel>();
            _trafficDetailCause.text = "";
            _trafficDetailCause.textScale = 0.76f;
            _trafficDetailCause.textColor = Color.white;
            _trafficDetailCause.width = detailsPanel.width - 24f;
            _trafficDetailCause.autoHeight = true;
            _trafficDetailCause.wordWrap = true;
            _trafficDetailCause.relativePosition = new Vector3(12f, 30f);

            _trafficDetailRec = detailsPanel.AddUIComponent<UILabel>();
            _trafficDetailRec.text = "";
            _trafficDetailRec.textScale = 0.76f;
            _trafficDetailRec.textColor = new Color32(180, 255, 180, 255);
            _trafficDetailRec.width = detailsPanel.width - 24f;
            _trafficDetailRec.autoHeight = true;
            _trafficDetailRec.wordWrap = true;
            _trafficDetailRec.relativePosition = new Vector3(12f, 62f);

            float actionBtnY = detailsPanel.height - 44f;

            _applyTrafficFixBtn = detailsPanel.AddUIComponent<UIButton>();
            _applyTrafficFixBtn.text = LocalizationManager.Get("ApplyTrafficFix");
            _applyTrafficFixBtn.width = 310f;
            _applyTrafficFixBtn.height = 36f;
            _applyTrafficFixBtn.relativePosition = new Vector3(12f, actionBtnY);
            _applyTrafficFixBtn.normalBgSprite = "ButtonMenu";
            _applyTrafficFixBtn.hoveredBgSprite = "ButtonMenuHovered";
            _applyTrafficFixBtn.textScale = 0.80f;
            _applyTrafficFixBtn.textColor = new Color32(255, 215, 0, 255);
            _applyTrafficFixBtn.isVisible = false;
            _applyTrafficFixBtn.eventClick += (c, e) => OnApplyTrafficFixClicked();

            _redistributeLanesBtn = detailsPanel.AddUIComponent<UIButton>();
            _redistributeLanesBtn.text = LocalizationManager.Get("AntiSingleLaneBtn");
            _redistributeLanesBtn.width = 330f;
            _redistributeLanesBtn.height = 36f;
            _redistributeLanesBtn.relativePosition = new Vector3(332f, actionBtnY);
            _redistributeLanesBtn.normalBgSprite = "ButtonMenu";
            _redistributeLanesBtn.hoveredBgSprite = "ButtonMenuHovered";
            _redistributeLanesBtn.textScale = 0.76f;
            _redistributeLanesBtn.textColor = new Color32(100, 230, 255, 255);
            _redistributeLanesBtn.tooltip = "Re-roteia veículos estagnados e balanceia as setas de faixa para forçar o uso de faixas vazias (Anti-Fila Indiana).";
            _redistributeLanesBtn.isVisible = false;
            _redistributeLanesBtn.eventClick += (c, e) => OnRedistributeLanesClicked();
        }

        // ==========================================
        // CONFIGURAÇÃO DA ABA DE ALERTAS URBANOS
        // ==========================================
        private void SetupCityAlertsTab(float startY)
        {
            _cityAlertsTabPanel = AddUIComponent<UIPanel>();
            _cityAlertsTabPanel.width = width - 24f;
            _cityAlertsTabPanel.height = height - startY - 14f;
            _cityAlertsTabPanel.relativePosition = new Vector3(12f, startY);

            float localY = 0f;

            _analyzeAlertsBtn = _cityAlertsTabPanel.AddUIComponent<UIButton>();
            _analyzeAlertsBtn.text = LocalizationManager.Get("AnalyzeAlerts");
            _analyzeAlertsBtn.width = 150f;
            _analyzeAlertsBtn.height = 34f;
            _analyzeAlertsBtn.relativePosition = new Vector3(0f, localY);
            _analyzeAlertsBtn.normalBgSprite = "ButtonMenu";
            _analyzeAlertsBtn.hoveredBgSprite = "ButtonMenuHovered";
            _analyzeAlertsBtn.textScale = 0.85f;
            _analyzeAlertsBtn.textColor = new Color32(255, 180, 80, 255);
            _analyzeAlertsBtn.eventClick += (c, e) => OnAnalyzeAlertsClicked();

            _fixAllAlertsBtn = _cityAlertsTabPanel.AddUIComponent<UIButton>();
            _fixAllAlertsBtn.text = LocalizationManager.Get("FixAllAlertsBtn");
            _fixAllAlertsBtn.width = 190f;
            _fixAllAlertsBtn.height = 34f;
            _fixAllAlertsBtn.relativePosition = new Vector3(158f, localY);
            _fixAllAlertsBtn.normalBgSprite = "ButtonMenu";
            _fixAllAlertsBtn.hoveredBgSprite = "ButtonMenuHovered";
            _fixAllAlertsBtn.textScale = 0.82f;
            _fixAllAlertsBtn.textColor = new Color32(255, 215, 0, 255);
            _fixAllAlertsBtn.tooltip = "Aplica despacho e resolução para todos os alertas municipais da lista sem necessidade de intervenção manual.";
            _fixAllAlertsBtn.eventClick += (c, e) => OnFixAllAlertsClicked();

            _reanalyzeAlertsBtn = _cityAlertsTabPanel.AddUIComponent<UIButton>();
            _reanalyzeAlertsBtn.text = LocalizationManager.Get("Reanalyze");
            _reanalyzeAlertsBtn.width = 145f;
            _reanalyzeAlertsBtn.height = 34f;
            _reanalyzeAlertsBtn.relativePosition = new Vector3(356f, localY);
            _reanalyzeAlertsBtn.normalBgSprite = "ButtonMenu";
            _reanalyzeAlertsBtn.hoveredBgSprite = "ButtonMenuHovered";
            _reanalyzeAlertsBtn.textScale = 0.85f;
            _reanalyzeAlertsBtn.textColor = new Color32(150, 220, 255, 255);
            _reanalyzeAlertsBtn.eventClick += (c, e) => OnAnalyzeAlertsClicked();

            _clearAlertsBtn = _cityAlertsTabPanel.AddUIComponent<UIButton>();
            _clearAlertsBtn.text = LocalizationManager.Get("Clear");
            _clearAlertsBtn.width = 150f;
            _clearAlertsBtn.height = 34f;
            _clearAlertsBtn.relativePosition = new Vector3(510f, localY);
            _clearAlertsBtn.normalBgSprite = "ButtonMenu";
            _clearAlertsBtn.hoveredBgSprite = "ButtonMenuHovered";
            _clearAlertsBtn.textScale = 0.85f;
            _clearAlertsBtn.textColor = new Color32(255, 160, 160, 255);
            _clearAlertsBtn.eventClick += (c, e) => OnClearAlertsClicked();

            localY += 40f;

            _alertsResultsContainer = _cityAlertsTabPanel.AddUIComponent<UIPanel>();
            _alertsResultsContainer.width = _cityAlertsTabPanel.width;
            _alertsResultsContainer.height = 232f;
            _alertsResultsContainer.relativePosition = new Vector3(0f, localY);
            _alertsResultsContainer.backgroundSprite = "SubPanel";
            EnableMouseWheelScrolling(_alertsResultsContainer);

            localY += 240f;

            UIPanel detailsPanel = _cityAlertsTabPanel.AddUIComponent<UIPanel>();
            detailsPanel.width = _cityAlertsTabPanel.width;
            detailsPanel.height = 275f;
            detailsPanel.relativePosition = new Vector3(0f, localY);
            detailsPanel.backgroundSprite = "SubPanel";
            detailsPanel.clipChildren = true;

            _alertDetailTitle = detailsPanel.AddUIComponent<UILabel>();
            _alertDetailTitle.text = LocalizationManager.Get("SelectAlertHint");
            _alertDetailTitle.textScale = 0.85f;
            _alertDetailTitle.textColor = new Color32(255, 200, 100, 255);
            _alertDetailTitle.width = detailsPanel.width - 24f;
            _alertDetailTitle.relativePosition = new Vector3(12f, 8f);

            _alertDetailCause = detailsPanel.AddUIComponent<UILabel>();
            _alertDetailCause.text = "";
            _alertDetailCause.textScale = 0.76f;
            _alertDetailCause.textColor = Color.white;
            _alertDetailCause.width = detailsPanel.width - 24f;
            _alertDetailCause.autoHeight = true;
            _alertDetailCause.wordWrap = true;
            _alertDetailCause.relativePosition = new Vector3(12f, 30f);

            _alertDetailRec = detailsPanel.AddUIComponent<UILabel>();
            _alertDetailRec.text = "";
            _alertDetailRec.textScale = 0.76f;
            _alertDetailRec.textColor = new Color32(180, 255, 180, 255);
            _alertDetailRec.width = detailsPanel.width - 24f;
            _alertDetailRec.autoHeight = true;
            _alertDetailRec.wordWrap = true;
            _alertDetailRec.relativePosition = new Vector3(12f, 62f);

            _applyAlertFixBtn = detailsPanel.AddUIComponent<UIButton>();
            _applyAlertFixBtn.text = LocalizationManager.Get("ApplyCityAlertFix");
            _applyAlertFixBtn.width = 330f;
            _applyAlertFixBtn.height = 36f;
            _applyAlertFixBtn.relativePosition = new Vector3(12f, detailsPanel.height - 44f);
            _applyAlertFixBtn.normalBgSprite = "ButtonMenu";
            _applyAlertFixBtn.hoveredBgSprite = "ButtonMenuHovered";
            _applyAlertFixBtn.textScale = 0.82f;
            _applyAlertFixBtn.textColor = new Color32(120, 230, 120, 255);
            _applyAlertFixBtn.isVisible = false;
            _applyAlertFixBtn.eventClick += (c, e) => OnApplyAlertFixClicked();
        }

        // ==========================================
        // CONFIGURAÇÃO DA ABA DE LOG & SOLUÇÕES ALTERNATIVAS
        // ==========================================
        private void SetupLogTab(float startY)
        {
            _logTabPanel = AddUIComponent<UIPanel>();
            _logTabPanel.width = width - 24f;
            _logTabPanel.height = height - startY - 14f;
            _logTabPanel.relativePosition = new Vector3(12f, startY);

            float localY = 0f;

            _refreshLogBtn = _logTabPanel.AddUIComponent<UIButton>();
            _refreshLogBtn.text = LocalizationManager.Get("ReanalyzeLogDemand");
            _refreshLogBtn.width = 280f;
            _refreshLogBtn.height = 34f;
            _refreshLogBtn.relativePosition = new Vector3(0f, localY);
            _refreshLogBtn.normalBgSprite = "ButtonMenu";
            _refreshLogBtn.hoveredBgSprite = "ButtonMenuHovered";
            _refreshLogBtn = _logTabPanel.AddUIComponent<UIButton>();
            _refreshLogBtn.text = LocalizationManager.Get("ReanalyzeLogDemand");
            _refreshLogBtn.width = 280f;
            _refreshLogBtn.height = 34f;
            _refreshLogBtn.relativePosition = new Vector3(0f, localY);
            _refreshLogBtn.normalBgSprite = "ButtonMenu";
            _refreshLogBtn.hoveredBgSprite = "ButtonMenuHovered";
            _refreshLogBtn.textScale = 0.82f;
            _refreshLogBtn.textColor = new Color32(150, 220, 255, 255);
            _refreshLogBtn.eventClick += (c, e) => RefreshLogDisplay();

            _clearLogBtn = _logTabPanel.AddUIComponent<UIButton>();
            _clearLogBtn.text = LocalizationManager.Get("ClearHistory");
            _clearLogBtn.width = 210f;
            _clearLogBtn.height = 34f;
            _clearLogBtn.relativePosition = new Vector3(290f, localY);
            _clearLogBtn.normalBgSprite = "ButtonMenu";
            _clearLogBtn.hoveredBgSprite = "ButtonMenuHovered";
            _clearLogBtn.textScale = 0.85f;
            _clearLogBtn.textColor = new Color32(255, 160, 160, 255);
            _clearLogBtn.eventClick += (c, e) =>
            {
                CorrectionLogManager.Instance.ClearLogs();
                RefreshLogDisplay();
            };

            localY += 40f;

            _logResultsContainer = _logTabPanel.AddUIComponent<UIPanel>();
            _logResultsContainer.width = _logTabPanel.width;
            _logResultsContainer.height = 232f;
            _logResultsContainer.relativePosition = new Vector3(0f, localY);
            _logResultsContainer.backgroundSprite = "SubPanel";
            EnableMouseWheelScrolling(_logResultsContainer);

            localY += 240f;

            UIPanel detailsPanel = _logTabPanel.AddUIComponent<UIPanel>();
            detailsPanel.width = _logTabPanel.width;
            detailsPanel.height = 275f;
            detailsPanel.relativePosition = new Vector3(0f, localY);
            detailsPanel.backgroundSprite = "SubPanel";
            detailsPanel.clipChildren = true;

            _logDetailTitle = detailsPanel.AddUIComponent<UILabel>();
            _logDetailTitle.text = LocalizationManager.Get("SelectLogHint");
            _logDetailTitle.textScale = 0.85f;
            _logDetailTitle.textColor = new Color32(255, 200, 100, 255);
            _logDetailTitle.width = detailsPanel.width - 24f;
            _logDetailTitle.relativePosition = new Vector3(12f, 8f);

            _logDetailInfo = detailsPanel.AddUIComponent<UILabel>();
            _logDetailInfo.text = "";
            _logDetailInfo.textScale = 0.76f;
            _logDetailInfo.textColor = Color.white;
            _logDetailInfo.width = detailsPanel.width - 24f;
            _logDetailInfo.autoHeight = true;
            _logDetailInfo.wordWrap = true;
            _logDetailInfo.relativePosition = new Vector3(12f, 30f);

            _logDetailAlternative = detailsPanel.AddUIComponent<UILabel>();
            _logDetailAlternative.text = "";
            _logDetailAlternative.textScale = 0.76f;
            _logDetailAlternative.textColor = new Color32(160, 230, 255, 255);
            _logDetailAlternative.width = detailsPanel.width - 24f;
            _logDetailAlternative.autoHeight = true;
            _logDetailAlternative.wordWrap = true;
            _logDetailAlternative.relativePosition = new Vector3(12f, 62f);

            float actionBtnY = detailsPanel.height - 44f;

            _requestAlternativeBtn = detailsPanel.AddUIComponent<UIButton>();
            _requestAlternativeBtn.text = LocalizationManager.Get("RequestAlternativeBtn");
            _requestAlternativeBtn.width = 320f;
            _requestAlternativeBtn.height = 36f;
            _requestAlternativeBtn.relativePosition = new Vector3(12f, actionBtnY);
            _requestAlternativeBtn.normalBgSprite = "ButtonMenu";
            _requestAlternativeBtn.hoveredBgSprite = "ButtonMenuHovered";
            _requestAlternativeBtn.textScale = 0.80f;
            _requestAlternativeBtn.textColor = new Color32(255, 215, 0, 255);
            _requestAlternativeBtn.isVisible = false;
            _requestAlternativeBtn.eventClick += (c, e) => OnRequestAlternativeClicked();

            _applyAlternativeBtn = detailsPanel.AddUIComponent<UIButton>();
            _applyAlternativeBtn.text = LocalizationManager.Get("ApplyAlternativeBtn");
            _applyAlternativeBtn.width = 310f;
            _applyAlternativeBtn.height = 36f;
            _applyAlternativeBtn.relativePosition = new Vector3(340f, actionBtnY);
            _applyAlternativeBtn.normalBgSprite = "ButtonMenu";
            _applyAlternativeBtn.hoveredBgSprite = "ButtonMenuHovered";
            _applyAlternativeBtn.textScale = 0.80f;
            _applyAlternativeBtn.textColor = new Color32(120, 255, 120, 255);
            _applyAlternativeBtn.isVisible = false;
            _applyAlternativeBtn.eventClick += (c, e) => OnApplyAlternativeClicked();
        }

        // ==========================================
        // GERENCIAMENTO DE ABAS E NAVEGAÇÃO
        // ==========================================
        private void SwitchTab(ActiveTab tab)
        {
            _currentTab = tab;

            if (_trafficTabPanel != null) _trafficTabPanel.isVisible = (tab == ActiveTab.Traffic);
            if (_cityAlertsTabPanel != null) _cityAlertsTabPanel.isVisible = (tab == ActiveTab.CityAlerts);
            if (_logTabPanel != null) _logTabPanel.isVisible = (tab == ActiveTab.CorrectionLog);

            HighlightTabButton(_tabTrafficBtn, tab == ActiveTab.Traffic);
            HighlightTabButton(_tabCityAlertsBtn, tab == ActiveTab.CityAlerts);
            HighlightTabButton(_tabLogBtn, tab == ActiveTab.CorrectionLog);

            if (tab == ActiveTab.CorrectionLog)
            {
                RefreshLogDisplay();
            }
        }

        private static void HighlightTabButton(UIButton btn, bool active)
        {
            if (btn == null) return;
            btn.textColor = active ? (Color)new Color32(255, 215, 0, 255) : Color.white;
        }

        public override void Update()
        {
            base.Update();
            // Sem loops de monitoramento constante por thread para evitar peso na simulação.
        }

        public void ToggleVisibility()
        {
            isVisible = !isVisible;
            if (isVisible)
            {
                BringToFront();
                UpdateKeyDisplay();
            }
            else
            {
                if (TrafficOverlayRenderer.Instance != null)
                {
                    TrafficOverlayRenderer.Instance.ClearHighlight();
                }
            }
        }

        // ==========================================
        // AÇÕES DA ABA DE TRÁFEGO
        // ==========================================
        private void OnAnalyzeTrafficClicked()
        {
            if (!SecureKeyStorage.HasKeyFor(AIClient.CurrentProvider))
            {
                var desc = AIProviderRegistry.GetDescriptor(AIClient.CurrentProvider);
                SetStatus(string.Format("Chave de API não informada para {0}. Insira sua chave acima.", desc.DisplayName), Color.red);
                return;
            }

            SetStatus(LocalizationManager.Get("ScanningTraffic"), Color.cyan);
            if (_analyzeTrafficBtn != null) _analyzeTrafficBtn.isEnabled = false;
            if (_reanalyzeTrafficBtn != null) _reanalyzeTrafficBtn.isEnabled = false;

            // Reavalia o status das correções anteriores sob demanda
            CorrectionLogManager.Instance.ReanalyzeCorrections();

            List<TrafficBottleneck> bottlenecks = TrafficAnalyzer.DetectTopBottlenecks();
            if (bottlenecks.Count == 0)
            {
                SetStatus(LocalizationManager.Get("NoBottlenecksFound"), Color.green);
                if (_analyzeTrafficBtn != null) _analyzeTrafficBtn.isEnabled = true;
                if (_reanalyzeTrafficBtn != null) _reanalyzeTrafficBtn.isEnabled = true;
                return;
            }

            var activeDesc = AIProviderRegistry.GetDescriptor(AIClient.CurrentProvider);
            SetStatus(string.Format("Enviando {0} gargalos para {1} ({2})...", bottlenecks.Count, activeDesc.DisplayName, AIClient.CurrentModel), Color.cyan);

            AIClient.AnalyzeBottlenecksAsync(
                bottlenecks,
                (results) =>
                {
                    if (_analyzeTrafficBtn != null) _analyzeTrafficBtn.isEnabled = true;
                    if (_reanalyzeTrafficBtn != null) _reanalyzeTrafficBtn.isEnabled = true;
                    SetStatus(string.Format("Análise concluída com sucesso via {0}! ({1} gargalos)", activeDesc.DisplayName, results.Count), Color.green);
                    _currentBottlenecks = results;
                    PopulateBottlenecksList(results);
                },
                (error) =>
                {
                    if (_analyzeTrafficBtn != null) _analyzeTrafficBtn.isEnabled = true;
                    if (_reanalyzeTrafficBtn != null) _reanalyzeTrafficBtn.isEnabled = true;
                    SetStatus(error, Color.red);
                }
            );
        }

        private void PopulateBottlenecksList(List<TrafficBottleneck> bottlenecks)
        {
            if (_trafficResultsContainer == null) return;
            ClearContainerComponents(_trafficResultsContainer);

            float itemY = 6f;
            for (int i = 0; i < bottlenecks.Count; i++)
            {
                TrafficBottleneck b = bottlenecks[i];
                UIButton itemBtn = _trafficResultsContainer.AddUIComponent<UIButton>();
                itemBtn.width = _trafficResultsContainer.width - 16f;
                itemBtn.height = 36f;
                itemBtn.relativePosition = new Vector3(8f, itemY);
                itemBtn.normalBgSprite = "ButtonMenu";
                itemBtn.hoveredBgSprite = "ButtonMenuHovered";
                itemBtn.textScale = 0.74f;
                itemBtn.textHorizontalAlignment = UIHorizontalAlignment.Left;

                string singleLaneBadge = b.IsSingleLaneCongestion ? " [Fila Indiana ⚠️]" : "";
                string line1 = string.Format("#{0} 🚦 {1} (Nó #{2}){3}", i + 1, b.RoadName, b.NodeId, singleLaneBadge);
                string line2 = string.Format("   Faixas: {0} | Score: {1} | Vel: {2}% | Sinal: {3} | Parados: {4}",
                    b.LaneCount,
                    b.CongestionScore,
                    b.AverageSpeedPercent,
                    b.HasTrafficLight ? "Sim" : "Não",
                    b.StuckVehicleCount
                );
                itemBtn.text = " " + line1 + "\n" + line2;
                itemBtn.tooltip = string.Format(
                    "{0} (Nó #{1})\nFaixas: {2} | Fila Indiana: {3}\nScore: {4} | Velocidade: {5}%\nSemáforo: {6} | Veículos Parados: {7}\nCausa: {8}\nRecomendação: {9}",
                    b.RoadName,
                    b.NodeId,
                    b.LaneCount,
                    b.IsSingleLaneCongestion ? "Detectada ⚠️" : "Não",
                    b.CongestionScore,
                    b.AverageSpeedPercent,
                    b.HasTrafficLight ? "Presente" : "Ausente",
                    b.StuckVehicleCount,
                    string.IsNullOrEmpty(b.AIRootCause) ? "Sobrecarga de tráfego" : b.AIRootCause,
                    string.IsNullOrEmpty(b.AIRecommendation) ? "Otimizar fluxo viário" : b.AIRecommendation
                );

                itemBtn.eventClick += (c, e) => SelectBottleneck(b);
                itemY += 38f;
            }

            if (bottlenecks.Count > 0)
            {
                SelectBottleneck(bottlenecks[0]);
            }
        }

        private void SelectBottleneck(TrafficBottleneck b)
        {
            _selectedBottleneck = b;

            if (TrafficOverlayRenderer.Instance != null)
            {
                TrafficOverlayRenderer.Instance.FocusCameraOn(b.NodeId, b.Position);
            }

            if (_trafficDetailTitle != null)
            {
                _trafficDetailTitle.text = string.Format("{0} (Nó #{1})", b.RoadName, b.NodeId);
            }
            if (_trafficDetailCause != null)
            {
                _trafficDetailCause.text = "🔍 " + (string.IsNullOrEmpty(b.AIRootCause) ? "Sobrecarga de fluxo viário." : b.AIRootCause);
            }
            if (_trafficDetailRec != null)
            {
                _trafficDetailRec.text = "💡 " + (string.IsNullOrEmpty(b.AIRecommendation) ? "Reorganize o fluxo viário." : b.AIRecommendation);
            }

            if (_applyTrafficFixBtn != null)
            {
                _applyTrafficFixBtn.isVisible = true;
                _applyTrafficFixBtn.text = b.HasTrafficLight ? "⚡ Remover Semáforo & Desobstruir" : "⚡ Adicionar Semáforo & Otimizar";
            }

            if (_redistributeLanesBtn != null)
            {
                _redistributeLanesBtn.isVisible = true;
                _redistributeLanesBtn.text = LocalizationManager.Get("AntiSingleLaneBtn");
            }
        }

        private void OnRedistributeLanesClicked()
        {
            if (_selectedBottleneck == null) return;

            int rerouted = TrafficActionExecutor.RedistributeLanesAndReroute(_selectedBottleneck.SegmentId, _selectedBottleneck.NodeId);
            string msg = string.Format("Anti-Fila Indiana executado: {0} veículo(s) re-roteados dinamicamente e faixas balanceadas!", rerouted);
            SetStatus(msg, Color.green);

            CorrectionLogManager.Instance.RecordCorrection(
                _selectedBottleneck.NodeId,
                CorrectionTargetType.Traffic,
                _selectedBottleneck.RoadName,
                "Fila Indiana / Rota Estática",
                "Re-roteamento dinâmico de veículos e balanceamento de faixas"
            );

            if (_redistributeLanesBtn != null)
            {
                _redistributeLanesBtn.isVisible = false;
            }
        }

        private void OnApplyTrafficFixClicked()
        {
            if (_selectedBottleneck == null) return;

            string res = TrafficActionExecutor.ApplyBottleneckActions(_selectedBottleneck);
            SetStatus(res, Color.green);

            if (_applyTrafficFixBtn != null)
            {
                _applyTrafficFixBtn.isVisible = false;
            }
        }

        private void OnFixAllTrafficClicked()
        {
            // Se a lista não estiver carregada, detecta automaticamente sem exigir ação prévia
            if (_currentBottlenecks == null || _currentBottlenecks.Count == 0)
            {
                CorrectionLogManager.Instance.ReanalyzeCorrections();
                _currentBottlenecks = TrafficAnalyzer.DetectTopBottlenecks();
                PopulateBottlenecksList(_currentBottlenecks);
            }

            if (_currentBottlenecks.Count == 0)
            {
                SetStatus(LocalizationManager.Get("NoBottlenecksFound"), Color.green);
                return;
            }

            SetStatus(string.Format("Aplicando correções em lote nos {0} gargalos viários...", _currentBottlenecks.Count), Color.cyan);
            int applied = TrafficActionExecutor.ApplyAllBottlenecks(_currentBottlenecks);
            SetStatus(string.Format("Correção em lote aplicada com sucesso em {0} nós/gargalos viários!", applied), Color.green);

            if (_applyTrafficFixBtn != null)
            {
                _applyTrafficFixBtn.isVisible = false;
            }
        }

        private void OnClearTrafficClicked()
        {
            ClearContainerComponents(_trafficResultsContainer);
            _currentBottlenecks.Clear();
            _selectedBottleneck = null;

            if (TrafficOverlayRenderer.Instance != null)
            {
                TrafficOverlayRenderer.Instance.ClearHighlight();
            }

            if (_trafficDetailTitle != null) _trafficDetailTitle.text = LocalizationManager.Get("SelectBottleneckHint");
            if (_trafficDetailCause != null) _trafficDetailCause.text = "";
            if (_trafficDetailRec != null) _trafficDetailRec.text = "";
            if (_applyTrafficFixBtn != null) _applyTrafficFixBtn.isVisible = false;
            if (_redistributeLanesBtn != null) _redistributeLanesBtn.isVisible = false;

            SetStatus("Análise de tráfego limpa.", Color.white);
        }

        // ==========================================
        // AÇÕES DA ABA DE ALERTAS URBANOS
        // ==========================================
        private void OnAnalyzeAlertsClicked()
        {
            if (!SecureKeyStorage.HasKeyFor(AIClient.CurrentProvider))
            {
                var desc = AIProviderRegistry.GetDescriptor(AIClient.CurrentProvider);
                SetStatus(string.Format("Chave de API não informada para {0}. Insira sua chave acima.", desc.DisplayName), Color.red);
                return;
            }

            SetStatus(LocalizationManager.Get("ScanningAlerts"), Color.cyan);
            if (_analyzeAlertsBtn != null) _analyzeAlertsBtn.isEnabled = false;
            if (_reanalyzeAlertsBtn != null) _reanalyzeAlertsBtn.isEnabled = false;

            CorrectionLogManager.Instance.ReanalyzeCorrections();

            List<CityAlert> alerts = CityAlertAnalyzer.DetectTopAlerts();
            if (alerts.Count == 0)
            {
                SetStatus(LocalizationManager.Get("NoAlertsFound"), Color.green);
                if (_analyzeAlertsBtn != null) _analyzeAlertsBtn.isEnabled = true;
                if (_reanalyzeAlertsBtn != null) _reanalyzeAlertsBtn.isEnabled = true;
                return;
            }

            var activeDesc = AIProviderRegistry.GetDescriptor(AIClient.CurrentProvider);
            SetStatus(string.Format("Enviando {0} alertas urbanos para {1} ({2})...", alerts.Count, activeDesc.DisplayName, AIClient.CurrentModel), Color.cyan);

            AIClient.AnalyzeCityAlertsAsync(
                alerts,
                (results) =>
                {
                    if (_analyzeAlertsBtn != null) _analyzeAlertsBtn.isEnabled = true;
                    if (_reanalyzeAlertsBtn != null) _reanalyzeAlertsBtn.isEnabled = true;
                    SetStatus(string.Format("Diagnóstico municipal concluído via {0}! ({1} alertas)", activeDesc.DisplayName, results.Count), Color.green);
                    _currentAlerts = results;
                    PopulateAlertsList(results);
                },
                (error) =>
                {
                    if (_analyzeAlertsBtn != null) _analyzeAlertsBtn.isEnabled = true;
                    if (_reanalyzeAlertsBtn != null) _reanalyzeAlertsBtn.isEnabled = true;
                    SetStatus(error, Color.red);
                }
            );
        }

        private void PopulateAlertsList(List<CityAlert> alerts)
        {
            if (_alertsResultsContainer == null) return;
            ClearContainerComponents(_alertsResultsContainer);

            float itemY = 6f;
            for (int i = 0; i < alerts.Count; i++)
            {
                CityAlert a = alerts[i];
                UIButton itemBtn = _alertsResultsContainer.AddUIComponent<UIButton>();
                itemBtn.width = _alertsResultsContainer.width - 16f;
                itemBtn.height = 36f;
                itemBtn.relativePosition = new Vector3(8f, itemY);
                itemBtn.normalBgSprite = "ButtonMenu";
                itemBtn.hoveredBgSprite = "ButtonMenuHovered";
                itemBtn.textScale = 0.74f;
                itemBtn.textHorizontalAlignment = UIHorizontalAlignment.Left;

                string icon = GetProblemIcon(a.ProblemType);
                string line1 = string.Format("#{0} {1} {2} (Edifício #{3})", i + 1, icon, a.BuildingName, a.BuildingId);
                string line2 = string.Format("   Problema: {0} | Gravidade: {1:0}", a.ProblemDescription, a.Severity);
                itemBtn.text = " " + line1 + "\n" + line2;
                itemBtn.tooltip = string.Format(
                    "{0} (Edifício #{1})\nProblema: {2}\nGravidade: {3:0}\nCausa: {4}\nRecomendação: {5}",
                    a.BuildingName,
                    a.BuildingId,
                    a.ProblemDescription,
                    a.Severity,
                    string.IsNullOrEmpty(a.AIRootCause) ? "Falta de atendimento urbano" : a.AIRootCause,
                    string.IsNullOrEmpty(a.AIRecommendation) ? "Ajustar capacidade dos serviços municipais" : a.AIRecommendation
                );

                itemBtn.eventClick += (c, e) => SelectCityAlert(a);
                itemY += 38f;
            }

            if (alerts.Count > 0)
            {
                SelectCityAlert(alerts[0]);
            }
        }

        private static string GetProblemIcon(CityProblemType type)
        {
            switch (type)
            {
                case CityProblemType.Death: return "⚰️";
                case CityProblemType.NoCustomers: return "🛍️";
                case CityProblemType.NoWorkers: return "👷";
                case CityProblemType.Water: return "💧";
                case CityProblemType.Electricity: return "⚡";
                case CityProblemType.Sewage: return "🚽";
                case CityProblemType.Garbage: return "🗑️";
                case CityProblemType.Fire: return "🔥";
                case CityProblemType.Crime: return "🚨";
                default: return "⚠️";
            }
        }

        private void SelectCityAlert(CityAlert a)
        {
            _selectedAlert = a;

            if (TrafficOverlayRenderer.Instance != null)
            {
                TrafficOverlayRenderer.Instance.FocusCameraOnBuilding(a.BuildingId, a.Position);
            }

            if (_alertDetailTitle != null)
            {
                _alertDetailTitle.text = string.Format("{0} ({1})", a.ProblemDescription, a.BuildingName);
            }
            if (_alertDetailCause != null)
            {
                _alertDetailCause.text = "🔍 " + (string.IsNullOrEmpty(a.AIRootCause) ? "Sobrecarga de infraestrutura local." : a.AIRootCause);
            }
            if (_alertDetailRec != null)
            {
                _alertDetailRec.text = "💡 " + (string.IsNullOrEmpty(a.AIRecommendation) ? "Ajuste a oferta de serviços na região." : a.AIRecommendation);
            }

            if (_applyAlertFixBtn != null)
            {
                _applyAlertFixBtn.isVisible = true;
                _applyAlertFixBtn.text = string.Format("🛠 {0}", a.ProblemDescription);
            }
        }

        private void OnApplyAlertFixClicked()
        {
            if (_selectedAlert == null) return;

            string res = CityActionExecutor.ApplyCityAlertAction(_selectedAlert);
            SetStatus(res, Color.green);

            if (_applyAlertFixBtn != null)
            {
                _applyAlertFixBtn.isVisible = false;
            }
        }

        private void OnFixAllAlertsClicked()
        {
            // Se a lista não estiver carregada, detecta automaticamente sem exigir ação prévia
            if (_currentAlerts == null || _currentAlerts.Count == 0)
            {
                CorrectionLogManager.Instance.ReanalyzeCorrections();
                _currentAlerts = CityAlertAnalyzer.DetectTopAlerts();
                PopulateAlertsList(_currentAlerts);
            }

            if (_currentAlerts.Count == 0)
            {
                SetStatus(LocalizationManager.Get("NoAlertsFound"), Color.green);
                return;
            }

            SetStatus(string.Format("Aplicando correções em lote nos {0} alertas municipais...", _currentAlerts.Count), Color.cyan);
            int applied = CityActionExecutor.ApplyAllCityAlerts(_currentAlerts);
            SetStatus(string.Format("Resolução em lote aplicada com sucesso em {0} alertas municipais!", applied), Color.green);

            if (_applyAlertFixBtn != null)
            {
                _applyAlertFixBtn.isVisible = false;
            }
        }

        private void OnClearAlertsClicked()
        {
            ClearContainerComponents(_alertsResultsContainer);
            _currentAlerts.Clear();
            _selectedAlert = null;

            if (TrafficOverlayRenderer.Instance != null)
            {
                TrafficOverlayRenderer.Instance.ClearHighlight();
            }

            if (_alertDetailTitle != null) _alertDetailTitle.text = LocalizationManager.Get("SelectAlertHint");
            if (_alertDetailCause != null) _alertDetailCause.text = "";
            if (_alertDetailRec != null) _alertDetailRec.text = "";
            if (_applyAlertFixBtn != null) _applyAlertFixBtn.isVisible = false;

            SetStatus("Alertas urbanos limpos.", Color.white);
        }

        // ==========================================
        // AÇÕES DA ABA DE LOG & SOLUÇÕES ALTERNATIVAS
        // ==========================================
        private void RefreshLogDisplay()
        {
            if (_logResultsContainer == null) return;
            ClearContainerComponents(_logResultsContainer);

            CorrectionLogManager.Instance.ReanalyzeCorrections();

            List<CorrectionLogEntry> logs = CorrectionLogManager.Instance.GetAllLogs();
            if (logs.Count == 0)
            {
                if (_logDetailTitle != null)
                {
                    _logDetailTitle.text = LocalizationManager.Get("NoLogsRecorded");
                }
                return;
            }

            float itemY = 6f;
            for (int i = 0; i < logs.Count; i++)
            {
                CorrectionLogEntry log = logs[i];
                UIButton itemBtn = _logResultsContainer.AddUIComponent<UIButton>();
                itemBtn.width = _logResultsContainer.width - 16f;
                itemBtn.height = 36f;
                itemBtn.relativePosition = new Vector3(8f, itemY);
                itemBtn.normalBgSprite = "ButtonMenu";
                itemBtn.hoveredBgSprite = "ButtonMenuHovered";
                itemBtn.textScale = 0.74f;
                itemBtn.textHorizontalAlignment = UIHorizontalAlignment.Left;

                string line1 = string.Format("#{0} 📋 {1} - {2}", i + 1, log.TargetName, log.ProblemDescription);
                string line2 = string.Format("   Ação: {0} | Status: {1} | ⏱ {2}",
                    log.AppliedAction,
                    log.GetStatusDisplay(),
                    log.GetFormattedElapsedTime()
                );
                itemBtn.text = " " + line1 + "\n" + line2;
                itemBtn.tooltip = string.Format(
                    "{0}\nProblema: {1}\nAção Aplicada: {2}\nHorário: {3:HH:mm:ss}\nTempo Decorrido: {4}\nStatus: {5}\nSolução Alternativa: {6}",
                    log.TargetName,
                    log.ProblemDescription,
                    log.AppliedAction,
                    log.Timestamp,
                    log.GetFormattedElapsedTime(),
                    log.GetStatusDisplay(),
                    string.IsNullOrEmpty(log.AlternativeRecommendation) ? "Nenhuma" : log.AlternativeRecommendation
                );

                if (log.Status == CorrectionStatus.PersistentExceeded10Min)
                {
                    itemBtn.textColor = new Color32(255, 120, 80, 255);
                }
                else if (log.Status == CorrectionStatus.Resolved)
                {
                    itemBtn.textColor = new Color32(100, 255, 100, 255);
                }

                itemBtn.eventClick += (c, e) => SelectLogEntry(log);
                itemY += 38f;
            }

            if (_selectedLogEntry == null && logs.Count > 0)
            {
                SelectLogEntry(logs[0]);
            }
        }

        private void SelectLogEntry(CorrectionLogEntry log)
        {
            _selectedLogEntry = log;

            if (_logDetailTitle != null)
            {
                _logDetailTitle.text = string.Format("{0} ({1}) - {2}",
                    log.TargetName, log.ProblemDescription, log.GetStatusDisplay());
            }

            if (_logDetailInfo != null)
            {
                _logDetailInfo.text = string.Format(
                    "⏱ {0} ({1})\n🔧 {2}",
                    log.Timestamp.ToString("HH:mm:ss"),
                    log.GetFormattedElapsedTime(),
                    log.AppliedAction
                );
            }

            bool isOver10Min = log.IsOver10Minutes() || log.Status == CorrectionStatus.PersistentExceeded10Min || log.Status == CorrectionStatus.AlternativeReady;

            if (_requestAlternativeBtn != null)
            {
                _requestAlternativeBtn.isVisible = isOver10Min && log.Status != CorrectionStatus.AlternativeApplied;
                _requestAlternativeBtn.isEnabled = log.Status != CorrectionStatus.AlternativeRequested;
            }

            if (_logDetailAlternative != null)
            {
                if (!string.IsNullOrEmpty(log.AlternativeRecommendation))
                {
                    _logDetailAlternative.text = string.Format(
                        "💡 Solução Alternativa da IA:\n• {0}\n• {1}",
                        log.AlternativeRootCause,
                        log.AlternativeRecommendation
                    );
                }
                else if (isOver10Min)
                {
                    _logDetailAlternative.text = "⚠️ ATENÇÃO: Esta intervenção ultrapassou 10 minutos! Clique em 'Pesquisar Solução Alternativa' para formular nova abordagem.";
                }
                else
                {
                    _logDetailAlternative.text = "Monitoramento ativo (limite de persistência: 10 minutos).";
                }
            }

            if (_applyAlternativeBtn != null)
            {
                _applyAlternativeBtn.isVisible = !string.IsNullOrEmpty(log.AlternativeRecommendation) && log.Status != CorrectionStatus.AlternativeApplied;
            }
        }

        private void OnRequestAlternativeClicked()
        {
            if (_selectedLogEntry == null) return;

            if (!SecureKeyStorage.HasKeyFor(AIClient.CurrentProvider))
            {
                var desc = AIProviderRegistry.GetDescriptor(AIClient.CurrentProvider);
                SetStatus(string.Format("Chave de API não informada para {0}. Insira sua chave acima.", desc.DisplayName), Color.red);
                return;
            }

            var activeDesc = AIProviderRegistry.GetDescriptor(AIClient.CurrentProvider);
            SetStatus(string.Format("Consultando {0} ({1}) para solução alternativa avançada (>10 min)...", activeDesc.DisplayName, AIClient.CurrentModel), Color.cyan);
            _selectedLogEntry.Status = CorrectionStatus.AlternativeRequested;

            if (_requestAlternativeBtn != null) _requestAlternativeBtn.isEnabled = false;

            AIClient.RequestAlternativeSolutionAsync(
                _selectedLogEntry,
                (updatedEntry) =>
                {
                    SetStatus(string.Format("Solução alternativa avançada recebida via {0}!", activeDesc.DisplayName), Color.green);
                    SelectLogEntry(updatedEntry);
                    RefreshLogDisplay();
                },
                (error) =>
                {
                    SetStatus(error, Color.red);
                    if (_requestAlternativeBtn != null) _requestAlternativeBtn.isEnabled = true;
                }
            );
        }

        private void OnApplyAlternativeClicked()
        {
            if (_selectedLogEntry == null) return;

            string res = CityActionExecutor.ApplyAlternativeAction(_selectedLogEntry);
            SetStatus(res, Color.green);
            SelectLogEntry(_selectedLogEntry);
            RefreshLogDisplay();
        }

        // ==========================================
        // MÉTODOS DE SUPORTE E UTILITÁRIOS
        // ==========================================
        private void SetStatus(string text, Color color)
        {
            if (_statusLabel != null)
            {
                _statusLabel.text = text;
                _statusLabel.textColor = color;
            }
        }

        private static void ClearContainerComponents(UIPanel container)
        {
            if (container == null) return;
            for (int i = container.components.Count - 1; i >= 0; i--)
            {
                UIComponent comp = container.components[i];
                if (comp != null)
                {
                    Destroy(comp.gameObject);
                }
            }
        }

        private static void EnableMouseWheelScrolling(UIPanel container)
        {
            if (container == null) return;
            container.clipChildren = true;
            container.eventMouseWheel += (c, e) =>
            {
                if (container.components.Count == 0) return;
                float delta = e.wheelDelta * 38f;
                float topY = container.components[0].relativePosition.y;
                float bottomY = container.components[container.components.Count - 1].relativePosition.y + 36f;

                // Não rolar para baixo se já estiver no topo
                if (delta > 0 && topY >= 6f) return;
                // Não rolar para cima se o último item já estiver visível no rodapé
                if (delta < 0 && bottomY <= container.height - 6f) return;

                for (int i = 0; i < container.components.Count; i++)
                {
                    UIComponent comp = container.components[i];
                    if (comp != null)
                    {
                        comp.relativePosition = new Vector3(comp.relativePosition.x, comp.relativePosition.y + delta);
                    }
                }
            };
        }

        private string GetProviderButtonText()
        {
            var desc = AIProviderRegistry.GetDescriptor(AIClient.CurrentProvider);
            return string.Format("🧠 {0} ▾", desc.DisplayName);
        }

        private string GetModelButtonText()
        {
            return string.Format("⚙️ {0} ▾", AIClient.CurrentModel);
        }

        private string GetKeyStatusText()
        {
            var desc = AIProviderRegistry.GetDescriptor(AIClient.CurrentProvider);
            return string.Format("Status ({0}): {1}", desc.DisplayName, SecureKeyStorage.GetMaskedDisplay(AIClient.CurrentProvider));
        }

        private void OnProviderClicked()
        {
            AIClient.CycleNextProvider();
            if (_providerBtn != null) _providerBtn.text = GetProviderButtonText();
            if (_toggleModelBtn != null) _toggleModelBtn.text = GetModelButtonText();
            UpdateKeyDisplay();

            var desc = AIProviderRegistry.GetDescriptor(AIClient.CurrentProvider);
            SetStatus(string.Format("Provedor ativo alterado para: {0} ({1})", desc.DisplayName, desc.Description), Color.cyan);
        }

        private void OnToggleModelClicked()
        {
            AIClient.CycleNextModel();
            if (_toggleModelBtn != null) _toggleModelBtn.text = GetModelButtonText();
            SetStatus(string.Format("Modelo ativo alterado para: {0}", AIClient.CurrentModel), Color.cyan);
        }

        private void OnFetchModelsClicked()
        {
            var desc = AIProviderRegistry.GetDescriptor(AIClient.CurrentProvider);
            if (!SecureKeyStorage.HasKeyFor(AIClient.CurrentProvider))
            {
                SetStatus(string.Format("Por favor, registre a chave de {0} antes de buscar os modelos da API.", desc.DisplayName), Color.yellow);
                return;
            }

            SetStatus(string.Format("Buscando modelos em tempo real na API de {0}...", desc.DisplayName), Color.cyan);
            if (_fetchModelsBtn != null) _fetchModelsBtn.isEnabled = false;

            AIClient.FetchAvailableModelsAsync(
                AIClient.CurrentProvider,
                (models) =>
                {
                    if (_fetchModelsBtn != null) _fetchModelsBtn.isEnabled = true;
                    if (_toggleModelBtn != null) _toggleModelBtn.text = GetModelButtonText();
                    SetStatus(string.Format("{0} modelos carregados da API de {1} com sucesso!", models.Count, desc.DisplayName), Color.green);
                },
                (error) =>
                {
                    if (_fetchModelsBtn != null) _fetchModelsBtn.isEnabled = true;
                    SetStatus(error, Color.red);
                }
            );
        }

        private void OnAddCustomModelClicked()
        {
            if (_customModelField == null) return;
            string custom = _customModelField.text;
            if (string.IsNullOrEmpty(custom) || custom.Trim().Length < 2)
            {
                SetStatus("Digite o nome ou identificador de um modelo válido para registrar.", Color.yellow);
                return;
            }

            string trimmed = custom.Trim();
            AIProviderRegistry.RegisterCustomModel(AIClient.CurrentProvider, trimmed);
            AIClient.CurrentModel = trimmed;
            _customModelField.text = "";
            if (_toggleModelBtn != null) _toggleModelBtn.text = GetModelButtonText();

            var desc = AIProviderRegistry.GetDescriptor(AIClient.CurrentProvider);
            SetStatus(string.Format("Novo modelo customizado registrado e selecionado para {0}: {1}", desc.DisplayName, trimmed), Color.green);
        }

        private void OnSetKeyClicked()
        {
            if (_keyField == null) return;

            string key = _keyField.text;
            if (string.IsNullOrEmpty(key) || key.Trim().Length < 8)
            {
                SetStatus("Por favor, digite uma chave de API válida antes de registrar.", Color.red);
                return;
            }

            SecureKeyStorage.SetKey(key, AIClient.CurrentProvider);
            _keyField.text = "";
            UpdateKeyDisplay();

            var desc = AIProviderRegistry.GetDescriptor(AIClient.CurrentProvider);
            SetStatus(string.Format("Chave para {0} registrada na memória RAM com sucesso!", desc.DisplayName), Color.green);
        }

        private void OnClearKeyClicked()
        {
            SecureKeyStorage.Clear(AIClient.CurrentProvider);
            if (_keyField != null)
            {
                _keyField.text = "";
            }
            UpdateKeyDisplay();

            var desc = AIProviderRegistry.GetDescriptor(AIClient.CurrentProvider);
            SetStatus(string.Format("Chave de {0} removida e purgada da RAM.", desc.DisplayName), Color.yellow);
        }

        private void OnClearAllKeysClicked()
        {
            SecureKeyStorage.ClearAll();
            if (_keyField != null)
            {
                _keyField.text = "";
            }
            UpdateKeyDisplay();
            SetStatus("Todas as chaves de todos os provedores foram purgadas da memória RAM.", Color.yellow);
        }

        private void UpdateKeyDisplay()
        {
            if (_keyStatusLabel != null)
            {
                _keyStatusLabel.text = GetKeyStatusText();
                _keyStatusLabel.textColor = SecureKeyStorage.HasKeyFor(AIClient.CurrentProvider) ? Color.green : Color.gray;
            }
        }

        private static string Truncate(string val, int maxLen)
        {
            if (string.IsNullOrEmpty(val)) return "";
            return val.Length <= maxLen ? val : val.Substring(0, maxLen - 3) + "...";
        }
    }
}
