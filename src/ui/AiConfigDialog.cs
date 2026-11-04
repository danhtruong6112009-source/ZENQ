using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using ZenQOffice.Core;

namespace ZenQOffice.UI
{
    /// <summary>
    /// Cửa sổ Cấu hình AI Copilot Đa nền tảng cho ZenQOffice.
    /// Cho phép tùy chỉnh Nhà cung cấp (Google AI Studio, Groq, OpenAI, DeepSeek, Ollama, Custom),
    /// Tên Model, Chuỗi API Key, Endpoint, và Kiểm tra kết nối trực tiếp.
    /// </summary>
    public class AiConfigDialog : Form
    {
        private ComboBox cboProvider;
        private ComboBox cboModel;
        private TextBox txtApiKey;
        private Button btnToggleKey;
        private TextBox txtEndpoint;
        private TextBox txtSystemPrompt;
        private TrackBar trkTemp;
        private Label lblTempValue;
        private Label lblStatus;
        private Button btnTest;
        private Button btnSave;
        private Button btnClose;

        public AiConfig Config { get; private set; }

        public AiConfigDialog()
        {
            Config = AiConfig.Load();
            InitializeComponent();
            LoadConfigToUi();
        }

        private void InitializeComponent()
        {
            Text = "Cài đặt trợ lý AI"; ClientSize = new Size(600, 610);
            MinimumSize = new Size(560, 520); StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.Sizable; MaximizeBox = false; MinimizeBox = false;
            ShowInTaskbar = false; BackColor = Color.White; Font = new Font("Segoe UI", 9.5f);
            AutoScaleMode = AutoScaleMode.Dpi; AutoScaleDimensions = new SizeF(96,96);
            var shell = new TableLayoutPanel { Dock=DockStyle.Fill, ColumnCount=1, RowCount=3, Margin=Padding.Empty };
            shell.RowStyles.Add(new RowStyle(SizeType.Absolute,76));shell.RowStyles.Add(new RowStyle(SizeType.Percent,100));shell.RowStyles.Add(new RowStyle(SizeType.Absolute,100));Controls.Add(shell);
            var header=new Panel{Dock=DockStyle.Fill,Padding=new Padding(24,14,24,8)};
            header.Controls.Add(new Label{Text="Kết nối mô hình bạn muốn sử dụng cho ZenQ Office.",Dock=DockStyle.Bottom,Height=22,ForeColor=Color.FromArgb(100,110,125)});
            header.Controls.Add(new Label{Text="Trợ lý AI",Dock=DockStyle.Top,Height=30,Font=new Font("Segoe UI",17,FontStyle.Bold),ForeColor=Color.FromArgb(28,37,52)});
            shell.Controls.Add(header,0,0);
            var scroll=new Panel{Dock=DockStyle.Fill,AutoScroll=true,Padding=new Padding(24,4,24,8)};shell.Controls.Add(scroll,0,1);
            var fields=new TableLayoutPanel{Dock=DockStyle.Top,AutoSize=true,ColumnCount=2,Padding=Padding.Empty};fields.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,50));fields.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,50));scroll.Controls.Add(fields);
            Control Field(string label,Control input,int height=32) {
                var group=new Panel{Dock=DockStyle.Fill,Height=height+29,Margin=new Padding(0,0,0,12)};
                group.Controls.Add(new Label{Text=label,Dock=DockStyle.Top,Height=23,ForeColor=Color.FromArgb(65,75,90),Font=new Font("Segoe UI",9f)});
                input.Dock=DockStyle.Bottom;input.Height=height;group.Controls.Add(input);return group;
            }
            void Row(Control control,int height){int row=fields.RowCount++;fields.RowStyles.Add(new RowStyle(SizeType.Absolute,height));fields.Controls.Add(control,0,row);fields.SetColumnSpan(control,2);}
            cboProvider=new FluentComboBox{DropDownStyle=ComboBoxStyle.DropDownList,IntegralHeight=false,MaxDropDownItems=8};
            foreach(AiProviderType provider in Enum.GetValues(typeof(AiProviderType)))cboProvider.Items.Add(AiConfig.GetProviderDisplayName(provider));
            cboModel=new FluentComboBox{DropDownStyle=ComboBoxStyle.DropDown,IntegralHeight=false,MaxDropDownItems=8};
            fields.RowStyles.Add(new RowStyle(SizeType.Absolute,72));fields.RowCount=1;
            var providerField=Field("Nhà cung cấp",cboProvider);providerField.Margin=new Padding(0,0,10,12);fields.Controls.Add(providerField,0,0);
            fields.Controls.Add(Field("Mô hình",cboModel),1,0);
            txtApiKey=new TextBox{BorderStyle=BorderStyle.None,UseSystemPasswordChar=true};
            var keyBox=new SettingsInputFrame(txtApiKey);
            btnToggleKey=new SettingsActionButton{Text="Hiện",Size=new Size(48,24),TabStop=true,AccessibleName="Hiện hoặc ẩn khóa API"};keyBox.Controls.Add(btnToggleKey);keyBox.Padding=new Padding(10,7,58,5);keyBox.Layout+=(s,e)=>btnToggleKey.SetBounds(Math.Max(0,keyBox.Width-54),4,48,24);
            btnToggleKey.Click+=(s,e)=>{txtApiKey.UseSystemPasswordChar=!txtApiKey.UseSystemPasswordChar;btnToggleKey.Text=txtApiKey.UseSystemPasswordChar?"Hiện":"Ẩn";};
            Row(Field("Khóa API",keyBox),72);
            txtEndpoint=new TextBox{BorderStyle=BorderStyle.None,PlaceholderText="Dùng địa chỉ mặc định của nhà cung cấp"};
            Row(Field("Địa chỉ API · tùy chọn",new SettingsInputFrame(txtEndpoint)),72);
            var temperature=new Panel{Dock=DockStyle.Fill,Margin=new Padding(0,0,0,8)};
            temperature.Controls.Add(new Label{Text="Độ sáng tạo",Location=new Point(0,0),AutoSize=true,ForeColor=Color.FromArgb(65,75,90)});
            lblTempValue=new Label{Text="0.7",Dock=DockStyle.Right,Width=40,TextAlign=ContentAlignment.MiddleRight,ForeColor=Color.FromArgb(79,70,229)};temperature.Controls.Add(lblTempValue);
            trkTemp=new TrackBar{Minimum=0,Maximum=15,Value=7,TickStyle=TickStyle.None,AutoSize=false,Height=28,Dock=DockStyle.Bottom,BackColor=Color.White};temperature.Controls.Add(trkTemp);
            trkTemp.ValueChanged+=(s,e)=>lblTempValue.Text=(trkTemp.Value/10f).ToString("0.0");Row(temperature,58);
            txtSystemPrompt=new TextBox{BorderStyle=BorderStyle.None,Multiline=true,ScrollBars=ScrollBars.Vertical,AcceptsReturn=true};
            Row(Field("Chỉ dẫn hệ thống",new SettingsInputFrame(txtSystemPrompt),100),140);
            var footer=new Panel{Dock=DockStyle.Fill,Padding=new Padding(24,6,24,16),BackColor=Color.FromArgb(248,250,252)};shell.Controls.Add(footer,0,2);
            lblStatus=new Label{Text="Chưa kiểm tra kết nối",Dock=DockStyle.Top,Height=30,ForeColor=Color.FromArgb(100,110,125),AutoEllipsis=true};footer.Controls.Add(lblStatus);
            var actions=new TableLayoutPanel{Dock=DockStyle.Bottom,Height=36,ColumnCount=4,RowCount=1};
            actions.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute,138));actions.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100));actions.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute,80));actions.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute,120));
            btnTest=new SettingsActionButton{Text="Kiểm tra kết nối",Dock=DockStyle.Fill,Margin=Padding.Empty};btnTest.Click+=BtnTest_Click;
            btnClose=new SettingsActionButton{Text="Đóng",Dock=DockStyle.Fill,Margin=new Padding(0,0,8,0),DialogResult=DialogResult.Cancel};btnClose.Click+=(s,e)=>Close();
            btnSave=new SettingsActionButton{Text="Lưu thiết lập",Primary=true,Dock=DockStyle.Fill,Margin=Padding.Empty};btnSave.Click+=BtnSave_Click;
            actions.Controls.Add(btnTest,0,0);actions.Controls.Add(btnClose,2,0);actions.Controls.Add(btnSave,3,0);footer.Controls.Add(actions);
            CancelButton=btnClose; AcceptButton=btnSave;
            cboProvider.SelectedIndexChanged+=CboProvider_SelectedIndexChanged;
            Shown+=(s,e)=>{FluentUi.RoundWindow(this);btnClose.Focus();};
        }

        private void LoadConfigToUi()
        {
            if (Config == null) Config = AiConfig.Load();

            var activeProf = Config.GetActiveProfile();
            if (activeProf != null)
            {
                int pIdx = (int)activeProf.Provider;
                if (pIdx >= 0 && pIdx < cboProvider.Items.Count)
                {
                    cboProvider.SelectedIndex = pIdx;
                }
                else
                {
                    cboProvider.SelectedIndex = 0;
                }

                UpdateModelList();
                string modelToSelect = activeProf.SelectedModel ?? "";
                if (activeProf.Provider == AiProviderType.GoogleAIStudio && modelToSelect == "gemini-3.1-flash")
                {
                    modelToSelect = "gemini-3.1-flash-lite";
                }
                if (!string.IsNullOrEmpty(modelToSelect))
                {
                    if (!cboModel.Items.Contains(modelToSelect))
                    {
                        cboModel.Items.Insert(0, modelToSelect);
                    }
                    cboModel.Text = modelToSelect;
                }
                txtApiKey.Text = activeProf.ApiKey ?? "";
                txtEndpoint.Text = activeProf.CustomEndpoint ?? "";
                trkTemp.Value = Math.Max(0, Math.Min(15, (int)(activeProf.Temperature * 10)));
                lblTempValue.Text = (trkTemp.Value / 10.0f).ToString("0.0");
                txtSystemPrompt.Text = Config.SystemInstructions ?? "";
            }
            else
            {
                int pIdx = (int)Config.Provider;
                if (pIdx >= 0 && pIdx < cboProvider.Items.Count)
                {
                    cboProvider.SelectedIndex = pIdx;
                }
                else
                {
                    cboProvider.SelectedIndex = 0;
                }

                UpdateModelList();
                string modelToSelect = Config.ModelName ?? "";
                if (Config.Provider == AiProviderType.GoogleAIStudio && modelToSelect == "gemini-3.1-flash")
                {
                    modelToSelect = "gemini-3.1-flash-lite";
                }
                cboModel.Text = modelToSelect;
                txtApiKey.Text = Config.ApiKey ?? "";
                txtEndpoint.Text = Config.CustomEndpoint ?? "";
                trkTemp.Value = Math.Max(0, Math.Min(15, (int)(Config.Temperature * 10)));
                lblTempValue.Text = (trkTemp.Value / 10.0f).ToString("0.0");
                txtSystemPrompt.Text = Config.SystemInstructions ?? "";
            }
        }

        private void UpdateModelList()
        {
            int pIdx = cboProvider.SelectedIndex;
            if (pIdx < 0) return;
            AiProviderType prov = (AiProviderType)pIdx;

            string curText = cboModel.Text;
            cboModel.Items.Clear();
            List<string> models = AiConfig.GetRecommendedModels(prov);
            foreach (var m in models)
            {
                cboModel.Items.Add(m);
            }

            if (!string.IsNullOrEmpty(curText) && models.Contains(curText))
            {
                cboModel.Text = curText;
            }
            else if (models.Count > 0)
            {
                cboModel.SelectedIndex = 0;
            }
        }

        private void CboProvider_SelectedIndexChanged(object sender, EventArgs e)
        {
            int pIdx = cboProvider.SelectedIndex;
            if (pIdx < 0) return;
            AiProviderType prov = (AiProviderType)pIdx;

            UpdateModelList();
            if (prov == AiProviderType.GoogleAIStudio)
            {
                txtEndpoint.Text = "https://generativelanguage.googleapis.com/v1beta";
            }
            else if (prov == AiProviderType.Groq)
            {
                txtEndpoint.Text = "https://api.groq.com/openai/v1";
            }
            else if (prov == AiProviderType.OpenAI)
            {
                txtEndpoint.Text = "https://api.openai.com/v1";
            }
            else if (prov == AiProviderType.DeepSeek)
            {
                txtEndpoint.Text = "https://api.deepseek.com/v1";
            }
            else if (prov == AiProviderType.Ollama)
            {
                txtEndpoint.Text = "http://localhost:11434/v1";
            }
            else if (prov == AiProviderType.OpenRouter)
            {
                txtEndpoint.Text = "https://openrouter.ai/api/v1";
            }
        }

        private void ReadUiToConfig()
        {
            AiProviderType prov = (AiProviderType)Math.Max(0, cboProvider.SelectedIndex);
            string model = cboModel.Text.Trim();
            if (prov == AiProviderType.GoogleAIStudio && model == "gemini-3.1-flash")
            {
                model = "gemini-3.1-flash-lite";
            }
            string apiKey = txtApiKey.Text.Trim();
            string endpoint = txtEndpoint.Text.Trim();
            float temp = trkTemp.Value / 10.0f;
            string sysPrompt = txtSystemPrompt.Text.Trim();

            Config.Provider = prov;
            Config.ModelName = model;
            Config.ApiKey = apiKey;
            Config.CustomEndpoint = endpoint;
            Config.Temperature = temp;
            Config.SystemInstructions = sysPrompt;

            var activeProf = Config.GetActiveProfile();
            if (activeProf == null && !string.IsNullOrWhiteSpace(apiKey))
            {
                activeProf = new AiKeyProfile(AiConfig.GetSimpleProviderName(prov), prov, apiKey, model);
                Config.Profiles.Add(activeProf);
                Config.ActiveProfileId = activeProf.Id;
            }

            if (activeProf != null)
            {
                activeProf.Provider = prov;
                activeProf.ApiKey = apiKey;
                activeProf.SelectedModel = model;
                activeProf.CustomEndpoint = endpoint;
                activeProf.Temperature = temp;

                if (activeProf.SavedModels == null) activeProf.SavedModels = new List<string>();
                if (!string.IsNullOrEmpty(model) && AiKeyProfile.IsModelCompatible(prov, model) && !activeProf.SavedModels.Contains(model))
                {
                    activeProf.SavedModels.Insert(0, model);
                }
                // Sanitize obsolete and incompatible models
                activeProf.SavedModels.RemoveAll(m => m == "gemini-3.1-flash" || (prov == AiProviderType.GoogleAIStudio && (m.StartsWith("gemini-1.") || m.StartsWith("gemini-2.") || m == "gemini-2.5-flash" || m == "gemini-2.5-pro")));
                activeProf.RemoveIncompatibleModels();
                if (activeProf.SavedModels.Count == 0 && !string.IsNullOrEmpty(model) && AiKeyProfile.IsModelCompatible(prov, model))
                {
                    activeProf.SavedModels.Add(model);
                }
            }
        }

        private void BtnTest_Click(object sender, EventArgs e)
        {
            ReadUiToConfig();
            lblStatus.Text = "Đang gửi yêu cầu kiểm tra kết nối tới " + Config.ModelName + "...";
            lblStatus.ForeColor = Color.FromArgb(202, 138, 4);
            btnTest.Enabled = false;

            AiService.SendPromptAsync(Config, "System check", "Xin chào! Trả lời 'OK' nếu kết nối hoạt động.", "", resp =>
            {
                if(IsDisposed || Disposing || !IsHandleCreated) return;
                this.BeginInvoke(new Action(() =>
                {
                    if(IsDisposed || Disposing) return;
                    btnTest.Enabled = true;
                    if (resp.Success)
                    {
                        lblStatus.Text = "Kết nối thành công! Phản hồi sau " + resp.DurationMs + " ms từ " + resp.ModelUsed;
                        lblStatus.ForeColor = Color.FromArgb(22, 163, 74);
                    }
                    else
                    {
                        lblStatus.Text = "Kết nối thất bại: " + resp.ErrorMessage;
                        lblStatus.ForeColor = Color.FromArgb(220, 38, 38);
                    }
                }));
            });
        }

        private void BtnSave_Click(object sender, EventArgs e)
        {
            ReadUiToConfig();
            Config.Save();
            if (AppForm.Instance != null)
            {
                AppForm.Instance.RefreshAiChatProfileSelectors();
                if (AppForm.Instance.AiSidebar != null)
                {
                    AppForm.Instance.AiSidebar.ReloadProfilesAndModels();
                }
            }
            MessageBox.Show("Đã lưu thiết lập Trợ lý AI thành công!\nBạn có thể sử dụng ngay trên tất cả các thẻ làm việc.", "ZENQ AI", MessageBoxButtons.OK, MessageBoxIcon.Information);
            this.DialogResult = DialogResult.OK;
            this.Close();
        }

        private static GraphicsPath CreateRoundedRectanglePath(RectangleF rect, float radius)
        {
            GraphicsPath path = new GraphicsPath();
            float r = Math.Min(radius, Math.Min(rect.Width / 2f, rect.Height / 2f));
            if (r <= 0)
            {
                path.AddRectangle(rect);
                return path;
            }
            float d = r * 2f;
            path.AddArc(rect.X, rect.Y, d, d, 180, 90);
            path.AddArc(rect.Right - d, rect.Y, d, d, 270, 90);
            path.AddArc(rect.Right - d, rect.Bottom - d, d, d, 0, 90);
            path.AddArc(rect.X, rect.Bottom - d, d, d, 90, 90);
            path.CloseFigure();
            return path;
        }
    }
}
