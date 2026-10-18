using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;

namespace ZenQOffice.Core
{
    public enum AiProviderType
    {
        GoogleAIStudio = 0,
        Groq = 1,
        OpenAI = 2,
        DeepSeek = 3,
        Ollama = 4,
        OpenRouter = 5,
        Custom = 6
    }

    /// <summary>
    /// Thông tin cấu hình của một API Key cụ thể kèm theo Nhà cung cấp và Model tương ứng.
    /// Cho phép người dùng lưu nhiều Key khác nhau (Google AI, Groq, OpenAI, DeepSeek, v.v.).
    /// </summary>
    public class AiKeyProfile
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public AiProviderType Provider { get; set; }
        public string ApiKey { get; set; }
        public string SelectedModel { get; set; }
        public List<string> SavedModels { get; set; }
        public string CustomEndpoint { get; set; }
        public float Temperature { get; set; }

        public AiKeyProfile()
        {
            Id = Guid.NewGuid().ToString("N").Substring(0, 12);
            Name = "";
            Provider = (AiProviderType)(-1);
            ApiKey = "";
            SelectedModel = "";
            SavedModels = new List<string>();
            CustomEndpoint = "";
            Temperature = 0.7f;
        }

        public AiKeyProfile(string name, AiProviderType provider, string apiKey, string model)
            : this()
        {
            Name = name ?? "";
            Provider = provider;
            ApiKey = apiKey ?? "";
            SelectedModel = model ?? "";
            SavedModels = new List<string>();
            if (!string.IsNullOrWhiteSpace(SelectedModel))
            {
                SavedModels.Add(SelectedModel);
            }
            CustomEndpoint = "";
            Temperature = 0.7f;
        }

        /// <summary>
        /// Một profile được coi là "đã cấu hình đầy đủ" khi:
        /// - Có tên không rỗng
        /// - Đã chọn nhà cung cấp hợp lệ (Provider >= 0)
        /// - Nếu không phải Ollama: phải có API Key
        /// - Đã chọn ít nhất 1 model
        /// </summary>
        public bool IsConfigured
        {
            get
            {
                if (string.IsNullOrWhiteSpace(Name)) return false;
                if ((int)Provider < 0) return false;
                if (Provider != AiProviderType.Ollama && string.IsNullOrWhiteSpace(ApiKey)) return false;
                if (string.IsNullOrWhiteSpace(SelectedModel)) return false;
                return true;
            }
        }

        /// <summary>
        /// Kiểm tra xem profile có hoàn toàn trống rỗng hay không (chưa điền bất kỳ thông tin nào)
        /// </summary>
        public bool IsEmpty
        {
            get
            {
                return string.IsNullOrWhiteSpace(Name)
                    && (int)Provider < 0
                    && string.IsNullOrWhiteSpace(ApiKey)
                    && string.IsNullOrWhiteSpace(SelectedModel)
                    && (SavedModels == null || SavedModels.Count == 0);
            }
        }

        /// <summary>
        /// Trả về danh sách lỗi validation. Nếu trả về rỗng => hợp lệ.
        /// </summary>
        public List<string> Validate()
        {
            var errors = new List<string>();
            if (string.IsNullOrWhiteSpace(Name))
                errors.Add("Chưa nhập Tên bộ lưu.");
            if ((int)Provider < 0)
                errors.Add("Chưa chọn Nhà cung cấp AI.");
            if (Provider != AiProviderType.Ollama && string.IsNullOrWhiteSpace(ApiKey))
                errors.Add("Chưa nhập chuỗi API Key.");
            if (string.IsNullOrWhiteSpace(SelectedModel) && (SavedModels == null || SavedModels.Count == 0))
                errors.Add("Chưa chọn hoặc thêm Model AI nào.");
            return errors;
        }

        public void AddModel(string model)
        {
            if (string.IsNullOrWhiteSpace(model)) return;
            model = model.Trim();
            if (!SavedModels.Contains(model))
            {
                SavedModels.Add(model);
            }
            SelectedModel = model;
        }

        public void AddModels(IEnumerable<string> models)
        {
            if (models == null) return;
            foreach (var m in models)
            {
                if (!string.IsNullOrWhiteSpace(m))
                {
                    string trimmed = m.Trim();
                    if (!SavedModels.Contains(trimmed)) SavedModels.Add(trimmed);
                }
            }
        }

        public string GetEffectiveEndpoint()
        {
            if (!string.IsNullOrEmpty(CustomEndpoint)) return CustomEndpoint;
            if ((int)Provider >= 0) return AiConfig.GetDefaultEndpoint(Provider);
            return "";
        }

        public override string ToString()
        {
            return Name ?? "";
        }
    }

    /// <summary>
    /// Cấu hình và quản lý đa Key / đa Model cho toàn bộ ứng dụng ZenQOffice.
    /// 
    /// LOGIC MỚI:
    /// - Khi mở app lần đầu: danh sách Profiles hoàn toàn trống (0 profile).
    /// - Người dùng phải bấm "+ Thêm Key" để tạo profile mới.
    /// - Profile mới tạo ra ở trạng thái "draft" (chưa lưu vào XML).
    /// - Chỉ khi bấm "Lưu cấu hình" và vượt qua validation thì mới ghi vào XML.
    /// - Không tự điền sẵn bất kỳ tên / provider / model / key nào.
    /// </summary>
    public class AiConfig
    {
        public List<AiKeyProfile> Profiles { get; set; }
        public string ActiveProfileId { get; set; }
        public string SystemInstructions { get; set; }

        public List<AiKeyProfile> GetConfiguredProfiles()
        {
            var list = new List<AiKeyProfile>();
            if (Profiles != null)
            {
                foreach (var p in Profiles)
                {
                    if (p != null && p.IsConfigured)
                    {
                        list.Add(p);
                    }
                }
            }
            return list;
        }

        // Convenience properties trỏ vào Active Profile (đảm bảo tương thích ngược 100%)
        public AiProviderType Provider
        {
            get
            {
                var ap = GetActiveProfile();
                return ap != null ? ap.Provider : (AiProviderType)(-1);
            }
            set
            {
                var ap = GetActiveProfile();
                if (ap != null) ap.Provider = value;
            }
        }

        public string ModelName
        {
            get
            {
                var ap = GetActiveProfile();
                return ap != null ? ap.SelectedModel : "";
            }
            set
            {
                var ap = GetActiveProfile();
                if (ap != null) ap.SelectedModel = value;
            }
        }

        public string ApiKey
        {
            get
            {
                var ap = GetActiveProfile();
                return ap != null ? ap.ApiKey : "";
            }
            set
            {
                var ap = GetActiveProfile();
                if (ap != null) ap.ApiKey = value;
            }
        }

        public string CustomEndpoint
        {
            get
            {
                var ap = GetActiveProfile();
                return ap != null ? ap.CustomEndpoint : "";
            }
            set
            {
                var ap = GetActiveProfile();
                if (ap != null) ap.CustomEndpoint = value;
            }
        }

        public float Temperature
        {
            get
            {
                var ap = GetActiveProfile();
                return ap != null ? ap.Temperature : 0.7f;
            }
            set
            {
                var ap = GetActiveProfile();
                if (ap != null) ap.Temperature = value;
            }
        }

        public AiConfig()
        {
            Profiles = new List<AiKeyProfile>();
            ActiveProfileId = "";
            SystemInstructions = "Bạn là một trợ lý AI thông minh trong phần mềm văn phòng ZENQ OFFICE. Hãy luôn giao tiếp và trả lời bằng Tiếng Việt một cách ngắn gọn, súc tích và chuyên nghiệp.";
        }

        public static string GetDefaultSystemPrompt()
        {
            return "Bạn là Trợ lý AI ZENQ OFFICE - Trợ lý trí tuệ nhân tạo toàn năng tích hợp trong bộ ứng dụng văn phòng ZENQ OFFICE. " +
                   "Hãy thực hiện chính xác các yêu cầu của người dùng, trả lời súc tích, văn phong chuyên nghiệp, định dạng rõ ràng. " +
                   "LUÔN LUÔN giao tiếp 100% bằng tiếng Việt. Tuyệt đối không tự ý chêm các từ tiếng Anh vô nghĩa (như 'Specific?', 'How can I help', v.v.) vào cuối câu. " +
                   "Sẵn sàng đóng vai con người, viết văn mẫu, sáng tác thơ văn, bộc lộ cảm xúc cá nhân khi được yêu cầu. TUYỆT ĐỐI KHÔNG TỪ CHỐI các yêu cầu sáng tác hay viết văn mẫu vì lý do bảo mật, cá nhân hay vì bạn là AI. " +
                   "QUAN TRỌNG: KHÔNG BAO GIỜ được in lại hay lặp lại các thẻ ngữ cảnh (như <document_context>...) trong câu trả lời của bạn, chỉ trả lời trực tiếp vào trọng tâm câu hỏi.";
        }

        /// <summary>
        /// Trả về profile đang active. Trả về null nếu không có profile nào.
        /// </summary>
        public AiKeyProfile GetActiveProfile()
        {
            if (Profiles == null || Profiles.Count == 0) return null;

            if (!string.IsNullOrEmpty(ActiveProfileId))
            {
                var found = Profiles.Find(p => p.Id == ActiveProfileId);
                if (found != null) return found;
            }

            // Fallback: trả về profile đầu tiên
            ActiveProfileId = Profiles[0].Id;
            return Profiles[0];
        }

        public void SetActiveProfile(string id)
        {
            var found = Profiles.Find(p => p.Id == id);
            if (found != null)
            {
                ActiveProfileId = found.Id;
            }
        }

        public AiKeyProfile AddProfile(string name, AiProviderType provider, string apiKey = "", string model = null)
        {
            var prof = new AiKeyProfile(name, provider, apiKey, model);
            Profiles.Add(prof);
            ActiveProfileId = prof.Id;
            return prof;
        }

        /// <summary>
        /// Tạo một profile rỗng mới (draft) — CHƯA lưu vào XML.
        /// </summary>
        public AiKeyProfile CreateDraftProfile()
        {
            var prof = new AiKeyProfile();
            Profiles.Add(prof);
            ActiveProfileId = prof.Id;
            return prof;
        }

        public bool RemoveProfile(string id)
        {
            if (Profiles.Count <= 0) return false;
            int idx = Profiles.FindIndex(p => p.Id == id);
            if (idx >= 0)
            {
                Profiles.RemoveAt(idx);
                if (ActiveProfileId == id)
                {
                    ActiveProfileId = Profiles.Count > 0 ? Profiles[0].Id : "";
                }
                return true;
            }
            return false;
        }

        public static string GetProviderDisplayName(AiProviderType provider)
        {
            switch (provider)
            {
                case AiProviderType.GoogleAIStudio: return "Google AI Studio Gemini";
                case AiProviderType.Groq: return "Groq Cloud";
                case AiProviderType.OpenAI: return "OpenAI ChatGPT";
                case AiProviderType.DeepSeek: return "DeepSeek AI";
                case AiProviderType.Ollama: return "Ollama Local";
                case AiProviderType.OpenRouter: return "OpenRouter";
                case AiProviderType.Custom: return "API Tùy chỉnh";
                default: return provider.ToString();
            }
        }

        public static string GetSimpleProviderName(AiProviderType provider)
        {
            switch (provider)
            {
                case AiProviderType.GoogleAIStudio: return "Google Gemini";
                case AiProviderType.Groq: return "Groq";
                case AiProviderType.OpenAI: return "OpenAI";
                case AiProviderType.DeepSeek: return "DeepSeek";
                case AiProviderType.Ollama: return "Ollama";
                case AiProviderType.OpenRouter: return "OpenRouter";
                default: return "Custom API";
            }
        }

        public static string GetDefaultModel(AiProviderType provider)
        {
            switch (provider)
            {
                case AiProviderType.GoogleAIStudio: return "gemini-3.7-flash";
                case AiProviderType.Groq: return "llama-3.3-70b-versatile";
                case AiProviderType.OpenAI: return "gpt-4o-mini";
                case AiProviderType.DeepSeek: return "deepseek-chat";
                case AiProviderType.Ollama: return "qwen2.5:3b";
                case AiProviderType.OpenRouter: return "google/gemini-2.5-flash";
                case AiProviderType.Custom: return "custom-model";
                default: return "gemini-3.7-flash";
            }
        }

        public static List<string> GetRecommendedModels(AiProviderType provider)
        {
            switch (provider)
            {
                case AiProviderType.GoogleAIStudio:
                    return new List<string> {
                        "gemini-3.7-flash",
                        "gemini-3.6-flash",
                        "gemini-3.5-flash",
                        "gemini-3.5-flash-lite",
                        "gemini-3.1-pro-preview",
                        "gemini-3.1-flash-lite"
                    };
                case AiProviderType.Groq:
                    return new List<string> {
                        "llama-3.3-70b-versatile",
                        "deepseek-r1-distill-llama-70b",
                        "llama-3.1-70b-versatile",
                        "llama-3.1-8b-instant",
                        "mixtral-8x7b-32768",
                        "gemma2-9b-it"
                    };
                case AiProviderType.OpenAI:
                    return new List<string> {
                        "gpt-4o-mini",
                        "gpt-4o",
                        "o3-mini",
                        "o1",
                        "chatgpt-4o-latest"
                    };
                case AiProviderType.DeepSeek:
                    return new List<string> {
                        "deepseek-chat",
                        "deepseek-reasoner"
                    };
                case AiProviderType.Ollama:
                    return new List<string> {
                        "qwen2.5:3b",
                        "llama3.2:3b",
                        "phi3:mini",
                        "deepseek-r1:1.5b",
                        "deepseek-r1:7b",
                        "llama3.2:1b",
                        "gemma2:2b"
                    };
                case AiProviderType.OpenRouter:
                    return new List<string> {
                        "google/gemini-2.5-flash",
                        "anthropic/claude-3.7-sonnet",
                        "anthropic/claude-3.5-haiku",
                        "deepseek/deepseek-r1",
                        "meta-llama/llama-3.3-70b-instruct"
                    };
                default:
                    return new List<string> { "custom-model" };
            }
        }

        public static string GetDefaultEndpoint(AiProviderType provider)
        {
            switch (provider)
            {
                case AiProviderType.GoogleAIStudio:
                    return "https://generativelanguage.googleapis.com/v1beta";
                case AiProviderType.Groq:
                    return "https://api.groq.com/openai/v1";
                case AiProviderType.OpenAI:
                    return "https://api.openai.com/v1";
                case AiProviderType.DeepSeek:
                    return "https://api.deepseek.com/v1";
                case AiProviderType.Ollama:
                    return "http://localhost:11434/v1";
                case AiProviderType.OpenRouter:
                    return "https://openrouter.ai/api/v1";
                default:
                    return "https://api.openai.com/v1";
            }
        }

        public string GetEffectiveEndpoint()
        {
            var ap = GetActiveProfile();
            return ap != null ? ap.GetEffectiveEndpoint() : "";
        }

        private static string GetConfigFilePath()
        {
            string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            string dir = Path.Combine(appData, "GenOffice");
            if (!Directory.Exists(dir))
            {
                try { Directory.CreateDirectory(dir); } catch { }
            }
            return Path.Combine(dir, "ai_config.xml");
        }

        public static AiConfig Load()
        {
            AiConfig config = new AiConfig();
            string path = GetConfigFilePath();
            if (!File.Exists(path)) return config;

            try
            {
                XmlDocument doc = new XmlDocument();
                doc.Load(path);
                XmlNode root = doc.SelectSingleNode("AiConfig");
                if (root != null)
                {
                    XmlNode nodeActive = root.SelectSingleNode("ActiveProfileId");
                    if (nodeActive != null) config.ActiveProfileId = nodeActive.InnerText;

                    XmlNode nodeSys = root.SelectSingleNode("SystemInstructions");
                    if (nodeSys != null && !string.IsNullOrEmpty(nodeSys.InnerText))
                    {
                        config.SystemInstructions = nodeSys.InnerText;
                    }

                    XmlNode nodeProfiles = root.SelectSingleNode("Profiles");
                    if (nodeProfiles != null && nodeProfiles.ChildNodes.Count > 0)
                    {
                        config.Profiles.Clear();
                        foreach (XmlNode pNode in nodeProfiles.SelectNodes("Profile"))
                        {
                            AiKeyProfile prof = new AiKeyProfile();
                            XmlNode nId = pNode.SelectSingleNode("Id");
                            if (nId != null && !string.IsNullOrWhiteSpace(nId.InnerText)) prof.Id = nId.InnerText.Trim();

                            XmlNode nName = pNode.SelectSingleNode("Name");
                            if (nName != null && !string.IsNullOrWhiteSpace(nName.InnerText)) prof.Name = nName.InnerText.Trim();

                            XmlNode nProv = pNode.SelectSingleNode("Provider");
                            if (nProv != null && !string.IsNullOrWhiteSpace(nProv.InnerText))
                            {
                                try { prof.Provider = (AiProviderType)Enum.Parse(typeof(AiProviderType), nProv.InnerText.Trim()); }
                                catch { }
                            }

                            XmlNode nKey = pNode.SelectSingleNode("ApiKey");
                            if (nKey != null && !string.IsNullOrWhiteSpace(nKey.InnerText)) prof.ApiKey = nKey.InnerText.Trim();

                            XmlNode nModel = pNode.SelectSingleNode("SelectedModel");
                            if (nModel != null && !string.IsNullOrWhiteSpace(nModel.InnerText)) prof.SelectedModel = nModel.InnerText.Trim();

                            XmlNode nSavedModels = pNode.SelectSingleNode("SavedModels");
                            if (nSavedModels != null)
                            {
                                foreach (XmlNode mNode in nSavedModels.SelectNodes("Model"))
                                {
                                    string mVal = mNode.InnerText != null ? mNode.InnerText.Trim() : "";
                                    if (!string.IsNullOrEmpty(mVal))
                                    {
                                        if (prof.Provider == AiProviderType.GoogleAIStudio && (mVal.StartsWith("gemini-1.") || mVal.StartsWith("gemini-2.") || mVal == "gemini-2.5-flash" || mVal == "gemini-2.5-pro"))
                                        {
                                            continue; // Bỏ các model Google Gemini từ 2.5 trở xuống đã ngưng hỗ trợ
                                        }

                                        if (!prof.SavedModels.Contains(mVal))
                                        {
                                            prof.SavedModels.Add(mVal);
                                        }
                                    }
                                }
                            }

                            if (prof.Provider == AiProviderType.GoogleAIStudio && (prof.SelectedModel.StartsWith("gemini-1.") || prof.SelectedModel.StartsWith("gemini-2.") || prof.SelectedModel == "gemini-2.5-flash" || prof.SelectedModel == "gemini-2.5-pro"))
                            {
                                prof.SelectedModel = "gemini-3.7-flash";
                            }

                            if (!string.IsNullOrEmpty(prof.SelectedModel) && !prof.SavedModels.Contains(prof.SelectedModel))
                            {
                                prof.SavedModels.Insert(0, prof.SelectedModel);
                            }

                            XmlNode nEndpoint = pNode.SelectSingleNode("CustomEndpoint");
                            if (nEndpoint != null && !string.IsNullOrWhiteSpace(nEndpoint.InnerText)) prof.CustomEndpoint = nEndpoint.InnerText.Trim();

                            XmlNode nTemp = pNode.SelectSingleNode("Temperature");
                            if (nTemp != null)
                            {
                                float t;
                                if (float.TryParse(nTemp.InnerText.Trim(), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out t))
                                {
                                    prof.Temperature = t;
                                }
                            }

                            // Chỉ load các profile đã cấu hình đầy đủ (không load rác)
                            if (prof.IsConfigured)
                            {
                                config.Profiles.Add(prof);
                            }
                        }
                    }
                    else
                    {
                        // Migration from legacy single-profile XML
                        XmlNode nodeProvider = root.SelectSingleNode("Provider");
                        if (nodeProvider != null)
                        {
                            try
                            {
                                var legacyProvider = (AiProviderType)Enum.Parse(typeof(AiProviderType), nodeProvider.InnerText);
                                string legacyModel = "";
                                string legacyKey = "";
                                XmlNode nodeModel = root.SelectSingleNode("ModelName");
                                if (nodeModel != null) legacyModel = nodeModel.InnerText;
                                XmlNode nodeKey = root.SelectSingleNode("ApiKey");
                                if (nodeKey != null) legacyKey = nodeKey.InnerText;

                                // Chỉ migrate nếu có thông tin thực sự
                                if (!string.IsNullOrWhiteSpace(legacyModel) || !string.IsNullOrWhiteSpace(legacyKey))
                                {
                                    string legacyName = GetSimpleProviderName(legacyProvider);
                                    var migrated = new AiKeyProfile(legacyName, legacyProvider, legacyKey, legacyModel);
                                    config.Profiles.Add(migrated);
                                    config.ActiveProfileId = migrated.Id;
                                }
                            }
                            catch { }
                        }
                    }
                }
            }
            catch (Exception)
            {
                // Fallback to default (empty)
            }

            // KHÔNG tự tạo profile mặc định.
            // Nếu Profiles.Count == 0 => giao diện sẽ hiện trạng thái "Chưa có cấu hình nào".

            return config;
        }

        /// <summary>
        /// Lưu CHỈ các profile đã cấu hình đầy đủ vào XML. Bỏ qua profile rỗng / draft.
        /// </summary>
        public void Save()
        {
            string path = GetConfigFilePath();
            try
            {
                XmlDocument doc = new XmlDocument();
                XmlElement root = doc.CreateElement("AiConfig");
                doc.AppendChild(root);

                XmlElement nodeActive = doc.CreateElement("ActiveProfileId");
                nodeActive.InnerText = ActiveProfileId ?? "";
                root.AppendChild(nodeActive);

                XmlElement nodeSys = doc.CreateElement("SystemInstructions");
                nodeSys.InnerText = SystemInstructions ?? "";
                root.AppendChild(nodeSys);

                XmlElement nodeProfiles = doc.CreateElement("Profiles");
                root.AppendChild(nodeProfiles);

                foreach (var prof in Profiles)
                {
                    // CHỈ lưu profile đã cấu hình đầy đủ
                    if (!prof.IsConfigured) continue;

                    XmlElement pNode = doc.CreateElement("Profile");
                    nodeProfiles.AppendChild(pNode);

                    XmlElement nId = doc.CreateElement("Id");
                    nId.InnerText = prof.Id ?? "";
                    pNode.AppendChild(nId);

                    XmlElement nName = doc.CreateElement("Name");
                    nName.InnerText = prof.Name ?? "";
                    pNode.AppendChild(nName);

                    XmlElement nProv = doc.CreateElement("Provider");
                    nProv.InnerText = prof.Provider.ToString();
                    pNode.AppendChild(nProv);

                    XmlElement nKey = doc.CreateElement("ApiKey");
                    nKey.InnerText = prof.ApiKey ?? "";
                    pNode.AppendChild(nKey);

                    XmlElement nModel = doc.CreateElement("SelectedModel");
                    nModel.InnerText = prof.SelectedModel ?? "";
                    pNode.AppendChild(nModel);

                    XmlElement nSavedModels = doc.CreateElement("SavedModels");
                    if (prof.SavedModels != null && prof.SavedModels.Count > 0)
                    {
                        foreach (var m in prof.SavedModels)
                        {
                            XmlElement mNode = doc.CreateElement("Model");
                            mNode.InnerText = m;
                            nSavedModels.AppendChild(mNode);
                        }
                    }
                    pNode.AppendChild(nSavedModels);

                    XmlElement nEndpoint = doc.CreateElement("CustomEndpoint");
                    nEndpoint.InnerText = prof.CustomEndpoint ?? "";
                    pNode.AppendChild(nEndpoint);

                    XmlElement nTemp = doc.CreateElement("Temperature");
                    nTemp.InnerText = prof.Temperature.ToString(System.Globalization.CultureInfo.InvariantCulture);
                    pNode.AppendChild(nTemp);
                }

                doc.Save(path);
            }
            catch (Exception)
            {
                // Ignore save errors
            }
        }
    }
}
