using System;
using System.Collections.Generic;
using TrafficAIOptimizer.Analysis;
using TrafficAIOptimizer.Core.Contracts;
using TrafficAIOptimizer.Core.Localization;

namespace TrafficAIOptimizer.Core.Services
{
    /// <summary>
    /// Serviço desacoplado responsável por realizar o parsing da resposta JSON da DeepSeek
    /// para tráfego, alertas da cidade e soluções alternativas persistentes (>10 min),
    /// com suporte multilíngue para Português, Inglês, Francês e Espanhol.
    /// </summary>
    public class DeepSeekResponseParser : IResponseParser
    {
        public void ParseAndApply(string jsonResponse, List<TrafficBottleneck> targetBottlenecks)
        {
            if (targetBottlenecks == null || targetBottlenecks.Count == 0)
            {
                return;
            }

            try
            {
                string rawContent = ExtractContentFromJson(jsonResponse);
                if (!string.IsNullOrEmpty(rawContent))
                {
                    string[] lines = rawContent.Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);
                    TrafficBottleneck currentB = null;

                    foreach (string line in lines)
                    {
                        string trimmed = line.Trim();

                        if (trimmed.StartsWith("NODE:", StringComparison.OrdinalIgnoreCase))
                        {
                            string idStr = trimmed.Substring(5).Trim();
                            if (ushort.TryParse(idStr, out ushort parsedId))
                            {
                                currentB = targetBottlenecks.Find(b => b.NodeId == parsedId);
                            }
                        }
                        else if (currentB != null && trimmed.StartsWith("CAUSA:", StringComparison.OrdinalIgnoreCase))
                        {
                            currentB.AIRootCause = trimmed.Substring(6).Trim();
                        }
                        else if (currentB != null && trimmed.StartsWith("SOLUCAO:", StringComparison.OrdinalIgnoreCase))
                        {
                            currentB.AIRecommendation = trimmed.Substring(8).Trim();
                        }
                        else if (currentB != null && trimmed.StartsWith("ACAO_AUTO:", StringComparison.OrdinalIgnoreCase))
                        {
                            string action = trimmed.Substring(10).Trim();
                            if (!string.IsNullOrEmpty(action) && !action.Equals("NONE", StringComparison.OrdinalIgnoreCase))
                            {
                                if (!currentB.SuggestedActions.Contains(action))
                                {
                                    currentB.SuggestedActions.Add(action);
                                }
                                currentB.HasAutomatedAction = true;
                            }
                        }
                    }
                }
            }
            catch
            {
                // Tratamento defensivo
            }
            finally
            {
                // Fallback defensivo garantido multilíngue
                foreach (var b in targetBottlenecks)
                {
                    if (string.IsNullOrEmpty(b.AIRootCause))
                    {
                        b.AIRootCause = GetDefaultTrafficCause(b.HasTrafficLight);
                    }
                    if (string.IsNullOrEmpty(b.AIRecommendation))
                    {
                        b.AIRecommendation = GetDefaultTrafficRecommendation();
                    }
                }
            }
        }

        public void ParseAndApplyCityAlerts(string jsonResponse, List<CityAlert> targetAlerts)
        {
            if (targetAlerts == null || targetAlerts.Count == 0)
            {
                return;
            }

            try
            {
                string rawContent = ExtractContentFromJson(jsonResponse);
                if (!string.IsNullOrEmpty(rawContent))
                {
                    string[] lines = rawContent.Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);
                    CityAlert currentA = null;

                    foreach (string line in lines)
                    {
                        string trimmed = line.Trim();

                        if (trimmed.StartsWith("BUILDING:", StringComparison.OrdinalIgnoreCase))
                        {
                            string idStr = trimmed.Substring(9).Trim();
                            if (ushort.TryParse(idStr, out ushort parsedId))
                            {
                                currentA = targetAlerts.Find(a => a.BuildingId == parsedId);
                            }
                        }
                        else if (currentA != null && trimmed.StartsWith("CAUSA:", StringComparison.OrdinalIgnoreCase))
                        {
                            currentA.AIRootCause = trimmed.Substring(6).Trim();
                        }
                        else if (currentA != null && trimmed.StartsWith("SOLUCAO:", StringComparison.OrdinalIgnoreCase))
                        {
                            currentA.AIRecommendation = trimmed.Substring(8).Trim();
                        }
                        else if (currentA != null && trimmed.StartsWith("ACAO_AUTO:", StringComparison.OrdinalIgnoreCase))
                        {
                            string action = trimmed.Substring(10).Trim();
                            if (!string.IsNullOrEmpty(action) && !action.Equals("NONE", StringComparison.OrdinalIgnoreCase))
                            {
                                if (!currentA.SuggestedActions.Contains(action))
                                {
                                    currentA.SuggestedActions.Add(action);
                                }
                                currentA.SuggestedAction = action;
                                currentA.HasAutomatedAction = true;
                            }
                        }
                    }
                }
            }
            catch
            {
                // Defensivo
            }
            finally
            {
                foreach (var a in targetAlerts)
                {
                    if (string.IsNullOrEmpty(a.AIRootCause))
                    {
                        a.AIRootCause = GetDefaultAlertCause(a.ProblemType);
                    }
                    if (string.IsNullOrEmpty(a.AIRecommendation))
                    {
                        a.AIRecommendation = GetDefaultAlertRecommendation(a.ProblemType);
                    }
                    if (string.IsNullOrEmpty(a.SuggestedAction))
                    {
                        a.SuggestedAction = GetDefaultAlertAction(a.ProblemType);
                        a.HasAutomatedAction = true;
                    }
                }
            }
        }

        public void ParseAndApplyAlternativeSolution(string jsonResponse, CorrectionLogEntry entry)
        {
            if (entry == null) return;

            try
            {
                string rawContent = ExtractContentFromJson(jsonResponse);
                if (!string.IsNullOrEmpty(rawContent))
                {
                    string[] lines = rawContent.Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);

                    foreach (string line in lines)
                    {
                        string trimmed = line.Trim();
                        if (trimmed.StartsWith("CAUSA_FALHA:", StringComparison.OrdinalIgnoreCase))
                        {
                            entry.AlternativeRootCause = trimmed.Substring(12).Trim();
                        }
                        else if (trimmed.StartsWith("SOLUCAO_ALTERNATIVA:", StringComparison.OrdinalIgnoreCase))
                        {
                            entry.AlternativeRecommendation = trimmed.Substring(20).Trim();
                        }
                        else if (trimmed.StartsWith("ACAO_ALTERNATIVA:", StringComparison.OrdinalIgnoreCase))
                        {
                            entry.AlternativeAction = trimmed.Substring(17).Trim();
                        }
                    }
                }
            }
            catch
            {
                // Defensivo
            }
            finally
            {
                if (string.IsNullOrEmpty(entry.AlternativeRootCause))
                {
                    entry.AlternativeRootCause = GetDefaultAlternativeCause();
                }
                if (string.IsNullOrEmpty(entry.AlternativeRecommendation))
                {
                    entry.AlternativeRecommendation = GetDefaultAlternativeRecommendation();
                }
                if (string.IsNullOrEmpty(entry.AlternativeAction))
                {
                    entry.AlternativeAction = GetDefaultAlternativeAction();
                }

                entry.Status = CorrectionStatus.AlternativeReady;
            }
        }

        public static string ExtractContentFromJson(string jsonResponse)
        {
            if (string.IsNullOrEmpty(jsonResponse)) return null;

            // 1. Google Gemini (formato: "candidates": [{"content": {"parts": [{"text": "..."}]}}])
            if (jsonResponse.IndexOf("\"candidates\"", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                string geminiText = ExtractQuotedStringAfter(jsonResponse, "\"text\":");
                if (!string.IsNullOrEmpty(geminiText)) return geminiText;
            }

            // 2. Anthropic Claude (formato: "content": [{"type": "text", "text": "..."}])
            if (jsonResponse.IndexOf("\"type\":", StringComparison.OrdinalIgnoreCase) >= 0 &&
                jsonResponse.IndexOf("\"text\":", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                string anthropicText = ExtractQuotedStringAfter(jsonResponse, "\"text\":");
                if (!string.IsNullOrEmpty(anthropicText)) return anthropicText;
            }

            // 3. OpenAI / DeepSeek / Grok / Meta (formato: "choices": [{"message": {"content": "..."}}])
            if (jsonResponse.IndexOf("\"content\":", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                string openAiText = ExtractQuotedStringAfter(jsonResponse, "\"content\":");
                if (!string.IsNullOrEmpty(openAiText)) return openAiText;
            }

            // Fallback geral procurando por "text":
            return ExtractQuotedStringAfter(jsonResponse, "\"text\":");
        }

        private static string ExtractQuotedStringAfter(string json, string key)
        {
            if (string.IsNullOrEmpty(json)) return null;
            int idx = json.IndexOf(key, StringComparison.OrdinalIgnoreCase);
            if (idx < 0) return null;

            int startQuote = json.IndexOf("\"", idx + key.Length, StringComparison.Ordinal);
            if (startQuote < 0) return null;

            int endQuote = -1;
            for (int i = startQuote + 1; i < json.Length; i++)
            {
                if (json[i] == '\"' && json[i - 1] != '\\')
                {
                    endQuote = i;
                    break;
                }
            }

            if (endQuote > startQuote)
            {
                string raw = json.Substring(startQuote + 1, endQuote - startQuote - 1);
                return raw
                    .Replace("\\n", "\n")
                    .Replace("\\r", "\r")
                    .Replace("\\\"", "\"")
                    .Replace("\\\\", "\\");
            }

            return null;
        }

        // ==========================================
        // FALLBACKS MULTILÍNGUES DINÂMICOS
        // ==========================================
        private static string GetDefaultTrafficCause(bool hasTrafficLight)
        {
            switch (LocalizationManager.CurrentLanguage)
            {
                case AppLanguage.English:
                    return hasTrafficLight ? "Traffic light cycle conflict causing queues on primary road." : "Lane bottleneck with high vehicle density.";
                case AppLanguage.French:
                    return hasTrafficLight ? "Conflit de feux de circulation provoquant des files d'attente." : "Goulot d'étranglement avec forte densité de véhicules.";
                case AppLanguage.Spanish:
                    return hasTrafficLight ? "Conflicto de semáforos generando retenciones en vía principal." : "Cuello de botella en carriles con alta densidad de vehículos.";
                case AppLanguage.Portuguese:
                default:
                    return hasTrafficLight ? "Conflito de semáforo gerando fila em via principal." : "Afunilamento de pistas com alta densidade de veículos.";
            }
        }

        private static string GetDefaultTrafficRecommendation()
        {
            switch (LocalizationManager.CurrentLanguage)
            {
                case AppLanguage.English:
                    return "Consider synchronizing traffic lights, converting to a roundabout, or making it one-way.";
                case AppLanguage.French:
                    return "Pensez à synchroniser les feux, créer un rond-point ou passer en sens unique.";
                case AppLanguage.Spanish:
                    return "Considere sincronizar los semáforos, crear una rotonda o cambiar a sentido único.";
                case AppLanguage.Portuguese:
                default:
                    return "Avalie sincronizar os tempos de semáforo, criar rotatória ou transformar a via em sentido único.";
            }
        }

        private static string GetDefaultAlertCause(CityProblemType type)
        {
            AppLanguage lang = LocalizationManager.CurrentLanguage;

            switch (type)
            {
                case CityProblemType.Death:
                    if (lang == AppLanguage.English) return "Delay in hearse service due to distance from cemeteries/crematoriums or traffic.";
                    if (lang == AppLanguage.French) return "Retard dans le service funéraire dû à l'éloignement ou aux embouteillages.";
                    if (lang == AppLanguage.Spanish) return "Retraso en el servicio fúnebre por lejanía o congestión de tráfico.";
                    return "Atraso no atendimento funerário devido à distância de cemitérios/crematórios ou tráfego saturado.";

                case CityProblemType.NoCustomers:
                    if (lang == AppLanguage.English) return "Lack of residential density nearby or poor transit connection for commercial shoppers.";
                    if (lang == AppLanguage.French) return "Manque de résidences à proximité ou transports en commun insuffisants.";
                    if (lang == AppLanguage.Spanish) return "Falta de densidad residencial cercana o transporte público insuficiente.";
                    return "Falta de densidade residencial próxima ou transporte público ineficiente levando pedestres ao comércio.";

                case CityProblemType.NoWorkers:
                    if (lang == AppLanguage.English) return "Education mismatch or poor commute access for industrial/commercial workers.";
                    if (lang == AppLanguage.French) return "Inadéquation de niveau d'éducation ou accès difficile pour les travailleurs.";
                    if (lang == AppLanguage.Spanish) return "Desajuste educativo o falta de transporte para trabajadores de la zona.";
                    return "Descompasso entre o nível educacional da população e os requisitos das vagas industriais/comerciais.";

                case CityProblemType.Water:
                    if (lang == AppLanguage.English) return "Deficit in water pumping capacity or broken pipe network connection.";
                    if (lang == AppLanguage.French) return "Déficit de pompage d'eau ou rupture dans le réseau de tuyaux.";
                    if (lang == AppLanguage.Spanish) return "Déficit en bombeo de agua potable o ruptura en la red de tuberías.";
                    return "Déficit na capacidade de bombeamento de água potável ou ruptura na tubulação da rede.";

                case CityProblemType.Electricity:
                    if (lang == AppLanguage.English) return "Power demand exceeded generation capacity or grid line disconnected.";
                    if (lang == AppLanguage.French) return "La demande électrique dépasse la production ou le réseau est déconnecté.";
                    if (lang == AppLanguage.Spanish) return "La demanda eléctrica superó la generación o hay desconexión en la red.";
                    return "Demanda elétrica excedeu a produção das usinas ou a rede de alta tensão não está conectada ao quarteirão.";

                case CityProblemType.Sewage:
                    if (lang == AppLanguage.English) return "Sewage pipes clogged or waste treatment facilities running at full capacity.";
                    if (lang == AppLanguage.French) return "Tuyaux d'eaux usées saturés ou capacité de traitement insuffisante.";
                    if (lang == AppLanguage.Spanish) return "Tuberías de aguas residuales saturadas o falta de plantas de tratamiento.";
                    return "Tubos de esgoto saturados ou falta de capacidade nas estações de tratamento de esgoto.";

                case CityProblemType.Garbage:
                    if (lang == AppLanguage.English) return "Garbage trucks stuck in traffic or landfills/incinerators operating at maximum load.";
                    if (lang == AppLanguage.French) return "Camions poubelles bloqués dans le trafic ou décharges saturées.";
                    if (lang == AppLanguage.Spanish) return "Camiones de basura atascados en el tráfico o vertederos saturados.";
                    return "Caminhões de lixo travados no tráfego ou aterros/incineradores operando em capacidade máxima.";

                case CityProblemType.Fire:
                    if (lang == AppLanguage.English) return "Fire department response delay due to road congestion or insufficient coverage.";
                    if (lang == AppLanguage.French) return "Retard des pompiers dû au trafic ou couverture de caserne insuffisante.";
                    if (lang == AppLanguage.Spanish) return "Retraso de bomberos por congestión o falta de cobertura cercana.";
                    return "Atraso na resposta dos bombeiros por congestionamento ou cobertura insuficiente do corpo de bombeiros.";

                case CityProblemType.Crime:
                    if (lang == AppLanguage.English) return "Low land value and lack of regular police patrols in the district.";
                    if (lang == AppLanguage.French) return "Faible valeur foncière et manque de patrouilles de police régulières.";
                    if (lang == AppLanguage.Spanish) return "Bajo valor del suelo y falta de patrullas policiales en el distrito.";
                    return "Baixo valor da terra e falta de patrulhamento policial regular no distrito.";

                default:
                    if (lang == AppLanguage.English) return "Overload of municipal infrastructure in the area.";
                    if (lang == AppLanguage.French) return "Surcharge de l'infrastructure municipale dans le quartier.";
                    if (lang == AppLanguage.Spanish) return "Sobrecarga de infraestructura municipal en la zona.";
                    return "Sobrecarga de infraestrutura municipal na região.";
            }
        }

        private static string GetDefaultAlertRecommendation(CityProblemType type)
        {
            AppLanguage lang = LocalizationManager.CurrentLanguage;

            switch (type)
            {
                case CityProblemType.Death:
                    if (lang == AppLanguage.English) return "Add a crematorium in the district and clear traffic corridors for hearses.";
                    if (lang == AppLanguage.French) return "Ajoutez un crématorium dans le district et décongestionnez l'accès.";
                    if (lang == AppLanguage.Spanish) return "Añada un crematorio en el distrito y despeje el acceso para coches fúnebres.";
                    return "Adicione um crematório no distrito e desobstrua as rotas de acesso aos carros fúnebres.";

                case CityProblemType.NoCustomers:
                    if (lang == AppLanguage.English) return "Increase nearby residential zoning and establish bus/metro lines to the shops.";
                    if (lang == AppLanguage.French) return "Augmentez les zones résidentielles proches et ajoutez des lignes de bus.";
                    if (lang == AppLanguage.Spanish) return "Aumente el zonificado residencial cercano y cree líneas de transporte.";
                    return "Aumente o zoneamento residencial nas proximidades e adicione linhas de ônibus conectando os bairros ao comércio.";

                case CityProblemType.NoWorkers:
                    if (lang == AppLanguage.English) return "Improve mass transit connectivity and adjust schools/university capacity.";
                    if (lang == AppLanguage.French) return "Améliorez les transports en commun et ajustez l'offre éducative.";
                    if (lang == AppLanguage.Spanish) return "Mejore el transporte público y ajuste la oferta educativa local.";
                    return "Melhore a acessibilidade por transporte coletivo e ajuste a oferta de escolas/universidades.";

                case CityProblemType.Water:
                    if (lang == AppLanguage.English) return "Build additional water pumping stations or water towers and link the pipes.";
                    if (lang == AppLanguage.French) return "Construisez des châteaux d'eau ou pompes et raccordez le réseau de canalisations.";
                    if (lang == AppLanguage.Spanish) return "Construya bombas o torres de agua y conecte el trazado de tuberías.";
                    return "Construa novas bombas de água ou torres hídricas e conecte a malha de encanamento subterrâneo.";

                case CityProblemType.Electricity:
                    if (lang == AppLanguage.English) return "Expand power plants or build transmission lines to the disconnected block.";
                    if (lang == AppLanguage.French) return "Étendez les centrales ou raccordez des lignes haute tension au bloc isolé.";
                    if (lang == AppLanguage.Spanish) return "Amplíe centrales o tienda líneas eléctricas hacia el bloque desconectado.";
                    return "Expanda as usinas de energia ou conecte linhas de transmissão até o bloco sem energia.";

                case CityProblemType.Sewage:
                    if (lang == AppLanguage.English) return "Build water treatment plants or sewage drain pipes connected to the network.";
                    if (lang == AppLanguage.French) return "Installez des stations d'épuration ou évacuations raccordées.";
                    if (lang == AppLanguage.Spanish) return "Construya depuradoras o tuberías de desagüe conectadas a la red.";
                    return "Construa drenos de esgoto ou estações ecológicas de tratamento conectadas à rede.";

                case CityProblemType.Garbage:
                    if (lang == AppLanguage.English) return "Build incinerators or recycling centers and optimize local road traffic.";
                    if (lang == AppLanguage.French) return "Construisez des incinérateurs ou centres de recyclage et fluidifiez le trafic.";
                    if (lang == AppLanguage.Spanish) return "Construya incineradores o centros de reciclaje y mejore el flujo vial.";
                    return "Construa incineradores ou instalações de reciclagem e otimize o fluxo viário local.";

                case CityProblemType.Fire:
                    if (lang == AppLanguage.English) return "Place a fire station with fast road access and enable smoke detector policy.";
                    if (lang == AppLanguage.French) return "Placez une caserne de pompiers bien desservie et activez les détecteurs.";
                    if (lang == AppLanguage.Spanish) return "Ubique una estación de bomberos con acceso rápido y active detectores de humo.";
                    return "Instale um quartel de bombeiros com acesso rápido e ative a política de detectores de fumaça.";

                case CityProblemType.Crime:
                    if (lang == AppLanguage.English) return "Increase police budget and build a police station in the neighborhood.";
                    if (lang == AppLanguage.French) return "Augmentez le budget de sécurité et placez un commissariat dans le quartier.";
                    if (lang == AppLanguage.Spanish) return "Aumente el presupuesto policial y construya una comisaría en la zona.";
                    return "Aumente o orçamento policial e construa uma delegacia de polícia no bairro.";

                default:
                    if (lang == AppLanguage.English) return "Review essential services and district zoning.";
                    if (lang == AppLanguage.French) return "Révisez les services essentiels et le zonage du quartier.";
                    if (lang == AppLanguage.Spanish) return "Revise los servicios esenciales y la zonificación en el área.";
                    return "Revise os serviços essenciais e o zoneamento na área.";
            }
        }

        private static string GetDefaultAlertAction(CityProblemType type)
        {
            switch (type)
            {
                case CityProblemType.Death: return "DISPATCH_DEATH_CARE";
                case CityProblemType.NoCustomers: return "STIMULATE_CUSTOMERS";
                case CityProblemType.NoWorkers: return "REBALANCE_WORKERS";
                case CityProblemType.Water: return "RESTORE_WATER";
                case CityProblemType.Electricity: return "RESTORE_ELECTRICITY";
                case CityProblemType.Sewage: return "CLEAR_SEWAGE";
                case CityProblemType.Garbage: return "DISPATCH_GARBAGE";
                default: return "NONE";
            }
        }

        private static string GetDefaultAlternativeCause()
        {
            switch (LocalizationManager.CurrentLanguage)
            {
                case AppLanguage.English:
                    return "Conventional intervention did not reach the macrostructural root cause (e.g., severe traffic gridlock blocking service routes or regional zoning imbalance).";
                case AppLanguage.French:
                    return "L'intervention conventionnelle n'a pas atteint la cause macrostructurelle (ex: trafic bloquant les véhicules de service ou déséquilibre régional de zonage).";
                case AppLanguage.Spanish:
                    return "La intervención convencional no alcanzó la causa macroestructural (ej: retenciones que bloquean vehículos de servicio o desequilibrio de zonificación).";
                case AppLanguage.Portuguese:
                default:
                    return "A ação convencional não alcançou a causa macroestrutural (ex: tráfego bloqueando acesso de veículos de serviço ou desequilíbrio na malha urbana).";
            }
        }

        private static string GetDefaultAlternativeRecommendation()
        {
            switch (LocalizationManager.CurrentLanguage)
            {
                case AppLanguage.English:
                    return "Restructure dedicated service corridors, revise public transit connections, or expand capacity with a dedicated local municipal facility.";
                case AppLanguage.French:
                    return "Restructurez des couloirs de service dédiés, développez les transports publics ou ajoutez un équipement municipal plus proche.";
                case AppLanguage.Spanish:
                    return "Reestructure corredores de servicio exclusivos, mejore las conexiones de transporte público o amplíe con una instalación local cercana.";
                case AppLanguage.Portuguese:
                default:
                    return "Reestruture os corredores viários de serviço, revise as conexões de rede ou expanda a capacidade com uma instalação dedicada mais próxima.";
            }
        }

        private static string GetDefaultAlternativeAction()
        {
            switch (LocalizationManager.CurrentLanguage)
            {
                case AppLanguage.English:
                    return "Force structural rebalancing and municipal priority override.";
                case AppLanguage.French:
                    return "Forcer le rééquilibrage structurel et la priorité municipale.";
                case AppLanguage.Spanish:
                    return "Forzar rebalanceo estructural y prioridad municipal.";
                case AppLanguage.Portuguese:
                default:
                    return "Forçar rebalanceamento estrutural e prioridade municipal.";
            }
        }
    }
}
