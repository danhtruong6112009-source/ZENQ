using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Media;
using System.Windows.Forms;
using ZenQOffice.Core;

namespace ZenQOffice.UI
{
    /// <summary>
    /// Bộ tổng hợp và phát hiệu ứng âm thanh chuẩn Microsoft PowerPoint 365 (Chime, Buzzer, Applause, Coin, Click, Laser, v.v.)
    /// Tự động tạo sóng âm PCM 16-bit 44.1kHz trực tiếp trong bộ nhớ mà không phụ thuộc tệp âm thanh bên ngoài.
    /// </summary>
    public static class SlideSoundPlayer
    {
        private static readonly Dictionary<SlideActionSound, byte[]> _wavCache = new Dictionary<SlideActionSound, byte[]>();
        private static readonly object _syncLock = new object();

        public static string GetSoundDisplayName(SlideActionSound sound)
        {
            switch (sound)
            {
                case SlideActionSound.None: return "(Không âm thanh)";
                case SlideActionSound.Chime: return "🔔 Tiếng chuông Đúng (Chime / Correct)";
                case SlideActionSound.Buzzer: return "🚨 Tiếng còi báo Sai (Buzzer / Wrong)";
                case SlideActionSound.Applause: return "👏 Tiếng vỗ tay (Applause)";
                case SlideActionSound.Coin: return "🪙 Tiếng đồng xu (Coin)";
                case SlideActionSound.Click: return "🖱️ Tiếng bấm máy (Click)";
                case SlideActionSound.DrumRoll: return "🥁 Tiếng trống dồn (Drum Roll)";
                case SlideActionSound.Explosion: return "💥 Tiếng nổ (Explosion)";
                case SlideActionSound.Laser: return "⚡ Tiếng tia Laser (Laser)";
                case SlideActionSound.Camera: return "📷 Tiếng chụp ảnh (Camera)";
                case SlideActionSound.Whoosh: return "💨 Tiếng gió vút (Whoosh)";
                case SlideActionSound.Bell: return "🛎️ Tiếng chuông ngân (Bell)";
                case SlideActionSound.CustomFile: return "📂 Tệp âm thanh khác (.wav)...";
                default: return sound.ToString();
            }
        }

        public static void Play(SlideActionSound sound, string customFilePath = null)
        {
            PlaySound(sound, customFilePath);
        }

        public static void PlaySound(SlideActionSound sound, string customFilePath = null)
        {
            if (sound == SlideActionSound.None) return;
            try
            {
                if (sound == SlideActionSound.CustomFile && !string.IsNullOrEmpty(customFilePath) && File.Exists(customFilePath))
                {
                    using (var sp = new SoundPlayer(customFilePath))
                    {
                        sp.Play();
                    }
                    return;
                }

                byte[] wavBytes = null;
                lock (_syncLock)
                {
                    if (!_wavCache.TryGetValue(sound, out wavBytes))
                    {
                        wavBytes = SynthesizeSoundWav(sound);
                        _wavCache[sound] = wavBytes;
                    }
                }

                if (wavBytes != null && wavBytes.Length > 44)
                {
                    var ms = new MemoryStream(wavBytes, false);
                    var player = new SoundPlayer(ms);
                    player.Play();
                }
            }
            catch
            {
                // Ignore audio device errors on headless machines
            }
        }

        private static byte[] SynthesizeSoundWav(SlideActionSound sound)
        {
            int sampleRate = 44100;
            float durationSec = 0.55f;
            switch (sound)
            {
                case SlideActionSound.Click: durationSec = 0.06f; break;
                case SlideActionSound.Camera: durationSec = 0.16f; break;
                case SlideActionSound.Coin: durationSec = 0.48f; break;
                case SlideActionSound.Chime: durationSec = 0.75f; break;
                case SlideActionSound.Bell: durationSec = 0.85f; break;
                case SlideActionSound.Buzzer: durationSec = 0.55f; break;
                case SlideActionSound.Laser: durationSec = 0.32f; break;
                case SlideActionSound.Whoosh: durationSec = 0.38f; break;
                case SlideActionSound.Applause: durationSec = 1.15f; break;
                case SlideActionSound.DrumRoll: durationSec = 0.90f; break;
                case SlideActionSound.Explosion: durationSec = 0.85f; break;
            }

            int numSamples = (int)(sampleRate * durationSec);
            short[] samples = new short[numSamples];
            Random rng = new Random(1337 + (int)sound);
            double prevSample = 0.0;

            for (int i = 0; i < numSamples; i++)
            {
                double t = (double)i / sampleRate;
                double progress = (double)i / numSamples;
                double val = 0.0;

                switch (sound)
                {
                    case SlideActionSound.Chime:
                    {
                        // Hai nốt hợp âm tươi sáng (C6 -> G6 + E6) chuẩn âm báo Đáp án Đúng
                        double env1 = Math.Exp(-t * 6.0);
                        double s1 = Math.Sin(2.0 * Math.PI * 1046.50 * t) * 0.5 +
                                    Math.Sin(2.0 * Math.PI * 1318.51 * t) * 0.3;
                        double s2 = 0.0;
                        if (t >= 0.11)
                        {
                            double t2 = t - 0.11;
                            double env2 = Math.Exp(-t2 * 4.2);
                            s2 = (Math.Sin(2.0 * Math.PI * 1567.98 * t2) * 0.55 +
                                  Math.Sin(2.0 * Math.PI * 2093.00 * t2) * 0.35) * env2;
                        }
                        val = s1 * env1 + s2;
                        break;
                    }
                    case SlideActionSound.Buzzer:
                    {
                        // Hai nhịp còi trầm (Eb3 -> C3) chuẩn âm báo Đáp án Sai
                        double freq = (t < 0.24) ? 155.56 : 130.81;
                        double env = (t < 0.22 || (t >= 0.25 && t < 0.52)) ? Math.Exp(-(t % 0.25) * 2.5) : 0.0;
                        // Sóng răng cưa pha sóng vuông tạo âm buzzer đặc trưng
                        double saw = 2.0 * ((t * freq) - Math.Floor(0.5 + t * freq));
                        double sq = Math.Sin(2.0 * Math.PI * freq * t) >= 0 ? 0.6 : -0.6;
                        val = (saw * 0.55 + sq * 0.45) * env * 0.65;
                        break;
                    }
                    case SlideActionSound.Coin:
                    {
                        double freq = (t < 0.09) ? 987.77 : 1318.51; // B5 -> E6
                        double env = Math.Exp(-t * 5.0);
                        val = (Math.Sin(2.0 * Math.PI * freq * t) * 0.75 +
                               Math.Sin(2.0 * Math.PI * freq * 2.0 * t) * 0.25) * env;
                        break;
                    }
                    case SlideActionSound.Bell:
                    {
                        double env = Math.Exp(-t * 3.8);
                        val = (Math.Sin(2.0 * Math.PI * 880.0 * t) * 0.6 +
                               Math.Sin(2.0 * Math.PI * 1760.0 * t) * 0.25 +
                               Math.Sin(2.0 * Math.PI * 2640.0 * t) * 0.15) * env;
                        break;
                    }
                    case SlideActionSound.Click:
                    {
                        double env = Math.Exp(-t * 85.0);
                        val = (Math.Sin(2.0 * Math.PI * 1800.0 * t) * 0.7 + (rng.NextDouble() * 2.0 - 1.0) * 0.3) * env;
                        break;
                    }
                    case SlideActionSound.Camera:
                    {
                        double env = (t < 0.05) ? Math.Exp(-t * 70.0) : (t > 0.07 ? Math.Exp(-(t - 0.07) * 65.0) : 0.0);
                        val = ((rng.NextDouble() * 2.0 - 1.0) * 0.6 + Math.Sin(2.0 * Math.PI * 2200.0 * t) * 0.4) * env;
                        break;
                    }
                    case SlideActionSound.Laser:
                    {
                        double freq = 1600.0 * Math.Exp(-t * 8.5) + 120.0;
                        double env = Math.Exp(-t * 5.5);
                        val = Math.Sin(2.0 * Math.PI * freq * t) * env * 0.8;
                        break;
                    }
                    case SlideActionSound.Whoosh:
                    {
                        double env = Math.Sin(Math.PI * progress);
                        double noise = rng.NextDouble() * 2.0 - 1.0;
                        prevSample = prevSample * 0.86 + noise * 0.14;
                        val = prevSample * env * 1.4;
                        break;
                    }
                    case SlideActionSound.Applause:
                    {
                        double env = Math.Sin(Math.PI * Math.Pow(progress, 0.7));
                        // Tổ hợp các xung vỗ tay ngẫu nhiên
                        double burst = 0.4 + 0.6 * Math.Abs(Math.Sin(2.0 * Math.PI * 19.0 * t) * Math.Cos(2.0 * Math.PI * 31.0 * t));
                        double noise = rng.NextDouble() * 2.0 - 1.0;
                        prevSample = prevSample * 0.65 + noise * 0.35;
                        val = prevSample * env * burst * 0.85;
                        break;
                    }
                    case SlideActionSound.DrumRoll:
                    {
                        double hitPhase = (t * 24.0) % 1.0;
                        double hitEnv = Math.Exp(-hitPhase * 7.0);
                        double overallEnv = (progress < 0.85) ? (0.4 + 0.6 * progress) : Math.Exp(-(progress - 0.85) * 14.0);
                        double noise = rng.NextDouble() * 2.0 - 1.0;
                        double drumTone = Math.Sin(2.0 * Math.PI * 165.0 * t);
                        val = (noise * 0.65 + drumTone * 0.35) * hitEnv * overallEnv * 0.8;
                        break;
                    }
                    case SlideActionSound.Explosion:
                    {
                        double env = Math.Exp(-t * 3.8);
                        double noise = rng.NextDouble() * 2.0 - 1.0;
                        prevSample = prevSample * 0.90 + noise * 0.10;
                        double subBass = Math.Sin(2.0 * Math.PI * (75.0 * Math.Exp(-t * 3.0) + 30.0) * t);
                        val = (prevSample * 0.7 + subBass * 0.5) * env;
                        break;
                    }
                }

                if (val > 0.98) val = 0.98;
                if (val < -0.98) val = -0.98;
                samples[i] = (short)(val * 32000);
            }

            using (var ms = new MemoryStream(44 + numSamples * 2))
            using (var bw = new BinaryWriter(ms))
            {
                int byteRate = sampleRate * 2;
                bw.Write(new char[] { 'R', 'I', 'F', 'F' });
                bw.Write(36 + numSamples * 2);
                bw.Write(new char[] { 'W', 'A', 'V', 'E' });
                bw.Write(new char[] { 'f', 'm', 't', ' ' });
                bw.Write(16);
                bw.Write((short)1); // PCM
                bw.Write((short)1); // Mono
                bw.Write(sampleRate);
                bw.Write(byteRate);
                bw.Write((short)2); // BlockAlign
                bw.Write((short)16); // BitsPerSample
                bw.Write(new char[] { 'd', 'a', 't', 'a' });
                bw.Write(numSamples * 2);
                for (int i = 0; i < numSamples; i++)
                {
                    bw.Write(samples[i]);
                }
                return ms.ToArray();
            }
        }
    }

    /// <summary>
    /// Hộp thoại Chèn Liên kết (Insert Hyperlink) chuẩn Microsoft Office 365.
    /// Hỗ trợ đầy đủ:
    /// 1. Vị trí trong Tài liệu này (Link tới Trang chiếu cụ thể, Trang tiếp theo, Trang trước, Trang đầu, Trang cuối, kèm Xem trước Slide).
    /// 2. Trang Web hoặc Tệp hiện có (URL / File).
    /// 3. Phản hồi Trắc nghiệm nhanh (Đáp án Đúng / Đáp án Sai).
    /// 4. Địa chỉ Email.
    /// </summary>
    public class InsertHyperlinkForm : Form
    {
        public string DisplayText { get; private set; }
        public string TargetUrl { get; private set; }
        public string ScreenTip { get; private set; }

        // Thông tin mở rộng cho PowerPoint Slide Link & Action
        public SlideActionType SelectedActionType { get; private set; } = SlideActionType.Url;
        public int SelectedSlideIndex { get; private set; } = -1;
        public int TargetSlideIndex => SelectedSlideIndex;
        public bool ShowAndReturn { get; private set; } = false;
        public SlideActionSound SelectedSound { get; private set; } = SlideActionSound.None;
        public bool HighlightClick { get; private set; } = true;
        public bool IsRemoveLinkRequested { get; private set; } = false;
        public bool RemoveLinkRequested => IsRemoveLinkRequested;

        private readonly PresentationRoot _presentation;
        private readonly int _currentSlideIndex;
        private readonly bool _isSlidesMode;

        private int _activeCategoryTab = 0; // 0: Web/File, 1: Place in Document (Slides), 2: Quiz Action, 3: Email
        private Button[] _navButtons;
        private Panel _contentHost;
        private Panel _panelWeb;
        private Panel _panelSlide;
        private Panel _panelQuiz;
        private Panel _panelEmail;

        private TextBox txtText;
        private TextBox txtScreenTip;
        private TextBox txtUrl;
        private TextBox txtEmail;
        private TextBox txtEmailSubject;
        private ListBox lstSlideTargets;
        private PictureBox picSlidePreview;
        private Label lblSlidePreviewTitle;
        private CheckBox chkShowAndReturn;
        private CheckBox chkHighlightClick;
        private ComboBox cboSound;
        private RadioButton rbQuizCorrect;
        private RadioButton rbQuizWrong;

        private class SlideTargetItem
        {
            public SlideActionType ActionType;
            public int SlideIndex;
            public string Label;
            public override string ToString() { return Label; }
        }

        public InsertHyperlinkForm(string selectedText = "", string existingUrl = "")
            : this(null, -1, null, selectedText, existingUrl)
        {
        }

        public InsertHyperlinkForm(PresentationRoot presentation, int currentSlideIndex, ShapeNode existingShape, string fallbackText = "", string fallbackUrl = "")
        {
            _presentation = presentation;
            _currentSlideIndex = currentSlideIndex;
            _isSlidesMode = (presentation != null && presentation.Slides != null && presentation.Slides.Count > 0);

            string initText = fallbackText;
            string initUrl = fallbackUrl;
            string initTip = "";

            if (existingShape != null)
            {
                if (string.IsNullOrEmpty(initText)) initText = existingShape.Text ?? "";
                initTip = existingShape.HyperlinkScreenTip ?? "";
                SelectedActionType = existingShape.ClickActionType;
                SelectedSlideIndex = existingShape.ClickTargetSlideIndex;
                ShowAndReturn = existingShape.ClickShowAndReturn;
                SelectedSound = existingShape.ClickSound;
                HighlightClick = existingShape.ClickHighlight;
                if (!string.IsNullOrEmpty(existingShape.ClickTargetUrl))
                    initUrl = existingShape.ClickTargetUrl;
            }

            this.Text = _isSlidesMode
                ? "Chèn Liên kết & Chuyển hướng Trang chiếu (Insert Hyperlink - MS365)"
                : "Chèn Liên kết (Insert Hyperlink)";
            this.Size = new Size(720, 520);
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.StartPosition = FormStartPosition.CenterParent;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.BackColor = Color.FromArgb(248, 250, 252);
            this.Font = new Font("Segoe UI", 9.0f);

            // Top Section: Text to display & ScreenTip
            Panel topBar = new Panel
            {
                Dock = DockStyle.Top,
                Height = 76,
                BackColor = Color.White,
                Padding = new Padding(16, 10, 16, 8)
            };
            topBar.Paint += (s, e) =>
            {
                using (Pen p = new Pen(Color.FromArgb(226, 232, 240)))
                    e.Graphics.DrawLine(p, 0, topBar.Height - 1, topBar.Width, topBar.Height - 1);
            };
            this.Controls.Add(topBar);

            Label lblText = new Label { Text = "Văn bản hiển thị:", Location = new Point(16, 14), AutoSize = true, Font = new Font("Segoe UI", 9f, FontStyle.Bold), ForeColor = Color.FromArgb(30, 41, 59) };
            topBar.Controls.Add(lblText);

            txtText = new TextBox
            {
                Text = string.IsNullOrEmpty(initText) ? (_isSlidesMode ? "Đi tới trang chiếu" : "Nhấp vào đây để xem chi tiết") : initText,
                Location = new Point(140, 11),
                Width = 545
            };
            topBar.Controls.Add(txtText);

            Label lblTip = new Label { Text = "Gợi ý màn hình:", Location = new Point(16, 44), AutoSize = true, ForeColor = Color.FromArgb(71, 85, 105) };
            topBar.Controls.Add(lblTip);

            txtScreenTip = new TextBox
            {
                Text = initTip,
                PlaceholderText = "Chú thích hiện ra khi di chuột qua liên kết (tùy chọn)...",
                Location = new Point(140, 41),
                Width = 545
            };
            topBar.Controls.Add(txtScreenTip);

            // Left Navigation Bar ("Liên kết đến / Link to:")
            Panel leftNav = new Panel
            {
                Location = new Point(14, 88),
                Size = new Size(165, 330),
                BackColor = Color.White
            };
            leftNav.Paint += (s, e) =>
            {
                using (Pen p = new Pen(Color.FromArgb(203, 213, 225)))
                    e.Graphics.DrawRectangle(p, 0, 0, leftNav.Width - 1, leftNav.Height - 1);
            };
            this.Controls.Add(leftNav);

            Label lblLinkTo = new Label
            {
                Text = "LIÊN KẾT ĐẾN:",
                Location = new Point(12, 10),
                AutoSize = true,
                Font = new Font("Segoe UI", 8.25f, FontStyle.Bold),
                ForeColor = Color.FromArgb(100, 116, 139)
            };
            leftNav.Controls.Add(lblLinkTo);

            string[] navTitles = new string[]
            {
                "📑 Trang chiếu trong bài",
                "🌐 Trang Web / Tệp",
                "🎯 Đáp án Trắc nghiệm",
                "✉️ Địa chỉ Email"
            };
            _navButtons = new Button[navTitles.Length];
            for (int i = 0; i < navTitles.Length; i++)
            {
                int tabIdx = i;
                Button btn = new Button
                {
                    Text = navTitles[i],
                    Location = new Point(8, 34 + i * 44),
                    Size = new Size(149, 38),
                    FlatStyle = FlatStyle.Flat,
                    TextAlign = ContentAlignment.MiddleLeft,
                    Font = new Font("Segoe UI", 9.0f),
                    Cursor = Cursors.Hand
                };
                btn.FlatAppearance.BorderSize = 0;
                if (!_isSlidesMode && (i == 0 || i == 2))
                {
                    btn.Enabled = false;
                }
                btn.Click += (s, e) => SwitchCategoryTab(tabIdx);
                leftNav.Controls.Add(btn);
                _navButtons[i] = btn;
            }

            // Main Content Host Panel
            _contentHost = new Panel
            {
                Location = new Point(190, 88),
                Size = new Size(498, 330),
                BackColor = Color.White
            };
            _contentHost.Paint += (s, e) =>
            {
                using (Pen p = new Pen(Color.FromArgb(203, 213, 225)))
                    e.Graphics.DrawRectangle(p, 0, 0, _contentHost.Width - 1, _contentHost.Height - 1);
            };
            this.Controls.Add(_contentHost);

            BuildSlidePlacePanel();
            BuildWebFilePanel(initUrl);
            BuildQuizQuickPanel();
            BuildEmailPanel();

            // Sound & Highlight Footer Options (when in Slides mode)
            if (_isSlidesMode)
            {
                Label lblSound = new Label { Text = "🔊 Âm thanh khi bấm:", Location = new Point(14, 425), AutoSize = true, ForeColor = Color.FromArgb(51, 65, 85) };
                this.Controls.Add(lblSound);

                cboSound = new ComboBox
                {
                    Location = new Point(145, 421),
                    Width = 235,
                    DropDownStyle = ComboBoxStyle.DropDownList
                };
                foreach (SlideActionSound snd in Enum.GetValues(typeof(SlideActionSound)))
                {
                    if (snd == SlideActionSound.CustomFile) continue;
                    cboSound.Items.Add(SlideSoundPlayer.GetSoundDisplayName(snd));
                }
                cboSound.SelectedIndex = Math.Max(0, Math.Min(cboSound.Items.Count - 1, (int)SelectedSound));
                this.Controls.Add(cboSound);

                Button btnTestSound = new Button
                {
                    Text = "▶ Nghe",
                    Location = new Point(386, 420),
                    Size = new Size(64, 25),
                    FlatStyle = FlatStyle.Flat,
                    BackColor = Color.White
                };
                btnTestSound.FlatAppearance.BorderColor = Color.FromArgb(203, 213, 225);
                btnTestSound.Click += (s, e) =>
                {
                    SlideActionSound snd = (SlideActionSound)Math.Max(0, cboSound.SelectedIndex);
                    SlideSoundPlayer.PlaySound(snd);
                };
                this.Controls.Add(btnTestSound);

                chkHighlightClick = new CheckBox
                {
                    Text = "Làm sáng nút khi nhấp (Highlight click)",
                    Location = new Point(462, 423),
                    AutoSize = true,
                    Checked = HighlightClick,
                    ForeColor = Color.FromArgb(51, 65, 85)
                };
                this.Controls.Add(chkHighlightClick);
            }

            // Bottom Action Bar
            Panel bottomPanel = new Panel { Dock = DockStyle.Bottom, Height = 50, BackColor = Color.FromArgb(241, 245, 249) };
            this.Controls.Add(bottomPanel);

            if (existingShape != null && existingShape.HasInteractiveAction)
            {
                Button btnRemove = new Button
                {
                    Text = "🗑️ Xóa Liên kết",
                    Location = new Point(14, 11),
                    Size = new Size(125, 29),
                    BackColor = Color.FromArgb(254, 242, 242),
                    ForeColor = Color.FromArgb(185, 28, 28),
                    FlatStyle = FlatStyle.Flat
                };
                btnRemove.FlatAppearance.BorderColor = Color.FromArgb(252, 165, 165);
                btnRemove.Click += (s, e) =>
                {
                    IsRemoveLinkRequested = true;
                    SelectedActionType = SlideActionType.None;
                    SelectedSlideIndex = -1;
                    TargetUrl = "";
                    this.DialogResult = DialogResult.OK;
                    this.Close();
                };
                bottomPanel.Controls.Add(btnRemove);
            }

            Button btnOk = new Button
            {
                Text = "Áp dụng (OK)",
                Location = new Point(492, 11),
                Size = new Size(105, 29),
                BackColor = Color.FromArgb(37, 99, 235),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 9.0f, FontStyle.Bold),
                FlatStyle = FlatStyle.Flat,
                DialogResult = DialogResult.OK
            };
            btnOk.FlatAppearance.BorderSize = 0;
            btnOk.Click += OnOkClicked;
            bottomPanel.Controls.Add(btnOk);

            Button btnCancel = new Button
            {
                Text = "Hủy",
                Location = new Point(605, 11),
                Size = new Size(82, 29),
                BackColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                DialogResult = DialogResult.Cancel
            };
            btnCancel.FlatAppearance.BorderColor = Color.FromArgb(203, 213, 225);
            bottomPanel.Controls.Add(btnCancel);

            this.AcceptButton = btnOk;
            this.CancelButton = btnCancel;

            // Mặc định mở tab nào phù hợp nhất
            if (_isSlidesMode)
            {
                if (SelectedActionType == SlideActionType.QuizCorrect || SelectedActionType == SlideActionType.QuizWrong)
                    SwitchCategoryTab(2);
                else if (SelectedActionType == SlideActionType.Url && !string.IsNullOrEmpty(initUrl) && !initUrl.StartsWith("slide:"))
                    SwitchCategoryTab(1);
                else
                    SwitchCategoryTab(0);
            }
            else
            {
                SwitchCategoryTab(1);
            }
        }

        private void BuildSlidePlacePanel()
        {
            _panelSlide = new Panel { Dock = DockStyle.Fill, Visible = false };

            Label lblHeader = new Label
            {
                Text = "Chọn một vị trí hoặc trang chiếu trong bài thuyết trình này:",
                Location = new Point(14, 12),
                AutoSize = true,
                Font = new Font("Segoe UI", 9.0f, FontStyle.Bold),
                ForeColor = Color.FromArgb(30, 41, 59)
            };
            _panelSlide.Controls.Add(lblHeader);

            lstSlideTargets = new ListBox
            {
                Location = new Point(14, 36),
                Size = new Size(245, 250),
                Font = new Font("Segoe UI", 9.25f),
                IntegralHeight = false
            };

            if (_isSlidesMode)
            {
                lstSlideTargets.Items.Add(new SlideTargetItem { ActionType = SlideActionType.NextSlide, SlideIndex = -1, Label = "⏭ Trang chiếu tiếp theo (Next Slide)" });
                lstSlideTargets.Items.Add(new SlideTargetItem { ActionType = SlideActionType.PreviousSlide, SlideIndex = -1, Label = "⏮ Trang chiếu trước đó (Previous Slide)" });
                lstSlideTargets.Items.Add(new SlideTargetItem { ActionType = SlideActionType.FirstSlide, SlideIndex = 0, Label = "⏪ Trang chiếu đầu tiên (First Slide)" });
                lstSlideTargets.Items.Add(new SlideTargetItem { ActionType = SlideActionType.LastSlide, SlideIndex = _presentation.Slides.Count - 1, Label = "⏩ Trang chiếu cuối cùng (Last Slide)" });
                lstSlideTargets.Items.Add(new SlideTargetItem { ActionType = SlideActionType.LastSlideViewed, SlideIndex = -1, Label = "🔙 Trang chiếu vừa xem (Last Viewed)" });
                lstSlideTargets.Items.Add(new SlideTargetItem { ActionType = SlideActionType.EndShow, SlideIndex = -1, Label = "⏹ Kết thúc trình chiếu (End Show)" });

                for (int i = 0; i < _presentation.Slides.Count; i++)
                {
                    SlideNode s = _presentation.Slides[i];
                    string stitle = GetFriendlySlideTitle(s, i);
                    lstSlideTargets.Items.Add(new SlideTargetItem
                    {
                        ActionType = SlideActionType.SpecificSlide,
                        SlideIndex = i,
                        Label = string.Format("📄 Trang {0}: {1}", i + 1, stitle)
                    });
                }
            }

            lstSlideTargets.SelectedIndexChanged += (s, e) =>
            {
                UpdateSlidePreview();
                SlideTargetItem sel = lstSlideTargets.SelectedItem as SlideTargetItem;
                if (sel != null && (string.IsNullOrEmpty(txtText.Text) || txtText.Text == "Đi tới trang chiếu" || txtText.Text.StartsWith("Trang ") || txtText.Text.StartsWith("Đi tới ")))
                {
                    txtText.Text = sel.Label.Substring(sel.Label.IndexOf(' ') + 1);
                }
            };
            _panelSlide.Controls.Add(lstSlideTargets);

            // Slide Preview Box on the Right
            Label lblPreviewHdr = new Label
            {
                Text = "Xem trước trang chiếu (Slide Preview):",
                Location = new Point(272, 36),
                AutoSize = true,
                Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
                ForeColor = Color.FromArgb(71, 85, 105)
            };
            _panelSlide.Controls.Add(lblPreviewHdr);

            picSlidePreview = new PictureBox
            {
                Location = new Point(272, 58),
                Size = new Size(210, 120),
                BackColor = Color.FromArgb(241, 245, 249),
                BorderStyle = BorderStyle.FixedSingle,
                SizeMode = PictureBoxSizeMode.Zoom
            };
            _panelSlide.Controls.Add(picSlidePreview);

            lblSlidePreviewTitle = new Label
            {
                Text = "",
                Location = new Point(272, 184),
                Size = new Size(210, 44),
                ForeColor = Color.FromArgb(51, 65, 85),
                Font = new Font("Segoe UI", 8.5f)
            };
            _panelSlide.Controls.Add(lblSlidePreviewTitle);

            chkShowAndReturn = new CheckBox
            {
                Text = "Hiển thị và quay lại\n(Show and return)",
                Location = new Point(272, 236),
                Size = new Size(210, 42),
                Checked = ShowAndReturn,
                ForeColor = Color.FromArgb(30, 41, 59)
            };
            _panelSlide.Controls.Add(chkShowAndReturn);

            // Pre-select existing target
            if (_isSlidesMode && lstSlideTargets.Items.Count > 0)
            {
                int foundIdx = 0;
                for (int i = 0; i < lstSlideTargets.Items.Count; i++)
                {
                    SlideTargetItem it = lstSlideTargets.Items[i] as SlideTargetItem;
                    if (it != null)
                    {
                        if (SelectedActionType == SlideActionType.SpecificSlide && it.ActionType == SlideActionType.SpecificSlide && it.SlideIndex == SelectedSlideIndex)
                        {
                            foundIdx = i;
                            break;
                        }
                        else if (SelectedActionType != SlideActionType.SpecificSlide && it.ActionType == SelectedActionType)
                        {
                            foundIdx = i;
                            break;
                        }
                    }
                }
                if (SelectedActionType == SlideActionType.None && _presentation.Slides.Count > 1)
                {
                    // Mặc định chọn Slide kế tiếp hoặc Slide 1 trong danh sách
                    foundIdx = Math.Min(lstSlideTargets.Items.Count - 1, 6 + Math.Min(_presentation.Slides.Count - 1, _currentSlideIndex + 1));
                }
                lstSlideTargets.SelectedIndex = foundIdx;
            }

            _contentHost.Controls.Add(_panelSlide);
        }

        private void BuildWebFilePanel(string initUrl)
        {
            _panelWeb = new Panel { Dock = DockStyle.Fill, Visible = false };

            Label lblUrl = new Label
            {
                Text = "Địa chỉ Trang Web (URL) hoặc Đường dẫn Tệp cục bộ:",
                Location = new Point(16, 18),
                AutoSize = true,
                Font = new Font("Segoe UI", 9.0f, FontStyle.Bold),
                ForeColor = Color.FromArgb(30, 41, 59)
            };
            _panelWeb.Controls.Add(lblUrl);

            txtUrl = new TextBox
            {
                Text = string.IsNullOrEmpty(initUrl) ? "https://" : initUrl,
                Location = new Point(16, 44),
                Width = 365,
                Font = new Font("Segoe UI", 9.5f)
            };
            _panelWeb.Controls.Add(txtUrl);

            Button btnBrowseFile = new Button
            {
                Text = "📂 Chọn Tệp...",
                Location = new Point(388, 42),
                Size = new Size(94, 27),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(241, 245, 249)
            };
            btnBrowseFile.FlatAppearance.BorderColor = Color.FromArgb(203, 213, 225);
            btnBrowseFile.Click += (s, e) =>
            {
                using (OpenFileDialog ofd = new OpenFileDialog())
                {
                    ofd.Title = "Chọn tệp hoặc tài liệu để liên kết";
                    ofd.Filter = "Tất cả các tệp (*.*)|*.*|Bài thuyết trình (*.pptx)|*.pptx|Tài liệu (*.docx;*.pdf)|*.docx;*.pdf";
                    if (ofd.ShowDialog(this) == DialogResult.OK)
                    {
                        txtUrl.Text = ofd.FileName;
                    }
                }
            };
            _panelWeb.Controls.Add(btnBrowseFile);

            Label lblQuick = new Label
            {
                Text = "Mẫu đường dẫn nhanh:",
                Location = new Point(16, 88),
                AutoSize = true,
                Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
                ForeColor = Color.FromArgb(100, 116, 139)
            };
            _panelWeb.Controls.Add(lblQuick);

            ListBox lstQuickUrls = new ListBox
            {
                Location = new Point(16, 110),
                Size = new Size(466, 180),
                Font = new Font("Segoe UI", 9.0f)
            };
            lstQuickUrls.Items.Add("https://www.google.com");
            lstQuickUrls.Items.Add("https://www.youtube.com");
            lstQuickUrls.Items.Add("https://vi.wikipedia.org");
            lstQuickUrls.Items.Add("https://zenqoffice.vn");
            lstQuickUrls.SelectedIndexChanged += (s, e) =>
            {
                if (lstQuickUrls.SelectedItem != null)
                    txtUrl.Text = lstQuickUrls.SelectedItem.ToString();
            };
            _panelWeb.Controls.Add(lstQuickUrls);

            _contentHost.Controls.Add(_panelWeb);
        }

        private void BuildQuizQuickPanel()
        {
            _panelQuiz = new Panel { Dock = DockStyle.Fill, Visible = false };

            Label lblTitle = new Label
            {
                Text = "Thiết lập Tương tác Câu hỏi Trắc nghiệm (Interactive Quiz):",
                Location = new Point(16, 16),
                AutoSize = true,
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                ForeColor = Color.FromArgb(30, 41, 59)
            };
            _panelQuiz.Controls.Add(lblTitle);

            Label lblDesc = new Label
            {
                Text = "Khi trình chiếu, nếu người thuyết trình hoặc học sinh nhấp chuột vào đối tượng này, hệ thống sẽ tự động đổi màu phản hồi, phát hiệu ứng hoạt họa và âm thanh tương ứng:",
                Location = new Point(16, 40),
                Size = new Size(465, 42),
                ForeColor = Color.FromArgb(71, 85, 105)
            };
            _panelQuiz.Controls.Add(lblDesc);

            rbQuizCorrect = new RadioButton
            {
                Text = "✅ ĐÁP ÁN ĐÚNG: Đổi sang màu Xanh lá + Hiệu ứng nảy sáng + Tiếng chuông Đúng (Chime)",
                Location = new Point(20, 95),
                Size = new Size(460, 44),
                Font = new Font("Segoe UI", 9.25f, FontStyle.Bold),
                ForeColor = Color.FromArgb(21, 128, 61),
                Checked = (SelectedActionType != SlideActionType.QuizWrong)
            };
            rbQuizCorrect.CheckedChanged += (s, e) =>
            {
                if (rbQuizCorrect.Checked && cboSound != null)
                    cboSound.SelectedIndex = (int)SlideActionSound.Chime;
            };
            _panelQuiz.Controls.Add(rbQuizCorrect);

            rbQuizWrong = new RadioButton
            {
                Text = "❌ ĐÁP ÁN SAI: Đổi sang màu Đỏ + Hiệu ứng rung lắc cảnh báo + Tiếng còi Sai (Buzzer)",
                Location = new Point(20, 150),
                Size = new Size(460, 44),
                Font = new Font("Segoe UI", 9.25f, FontStyle.Bold),
                ForeColor = Color.FromArgb(185, 28, 28),
                Checked = (SelectedActionType == SlideActionType.QuizWrong)
            };
            rbQuizWrong.CheckedChanged += (s, e) =>
            {
                if (rbQuizWrong.Checked && cboSound != null)
                    cboSound.SelectedIndex = (int)SlideActionSound.Buzzer;
            };
            _panelQuiz.Controls.Add(rbQuizWrong);

            Label lblTipQuiz = new Label
            {
                Text = "💡 Mẹo: Bạn cũng có thể dùng nút '⚡ Kích hoạt (Trigger)' trong tab Hoạt họa (Animations) để kích hoạt bất kỳ hiệu ứng xuất hiện/nhấn mạnh/biến mất nào khi bấm vào một câu trả lời giống hệt PowerPoint 365!",
                Location = new Point(20, 215),
                Size = new Size(455, 65),
                ForeColor = Color.FromArgb(37, 99, 235)
            };
            _panelQuiz.Controls.Add(lblTipQuiz);

            _contentHost.Controls.Add(_panelQuiz);
        }

        private void BuildEmailPanel()
        {
            _panelEmail = new Panel { Dock = DockStyle.Fill, Visible = false };

            Label lblMail = new Label
            {
                Text = "Địa chỉ Email người nhận:",
                Location = new Point(16, 20),
                AutoSize = true,
                Font = new Font("Segoe UI", 9.0f, FontStyle.Bold)
            };
            _panelEmail.Controls.Add(lblMail);

            txtEmail = new TextBox { Location = new Point(16, 44), Width = 450, PlaceholderText = "ví dụ: lienhe@zenqoffice.vn" };
            _panelEmail.Controls.Add(txtEmail);

            Label lblSub = new Label
            {
                Text = "Chủ đề thư (Subject):",
                Location = new Point(16, 86),
                AutoSize = true,
                Font = new Font("Segoe UI", 9.0f, FontStyle.Bold)
            };
            _panelEmail.Controls.Add(lblSub);

            txtEmailSubject = new TextBox { Location = new Point(16, 110), Width = 450 };
            _panelEmail.Controls.Add(txtEmailSubject);

            _contentHost.Controls.Add(_panelEmail);
        }

        private void SwitchCategoryTab(int idx)
        {
            _activeCategoryTab = idx;
            for (int i = 0; i < _navButtons.Length; i++)
            {
                bool active = (i == idx);
                _navButtons[i].BackColor = active ? Color.FromArgb(239, 246, 255) : Color.White;
                _navButtons[i].ForeColor = active ? Color.FromArgb(29, 78, 216) : Color.FromArgb(51, 65, 85);
                _navButtons[i].Font = new Font("Segoe UI", 9.0f, active ? FontStyle.Bold : FontStyle.Regular);
            }

            _panelSlide.Visible = (idx == 0);
            _panelWeb.Visible = (idx == 1);
            _panelQuiz.Visible = (idx == 2);
            _panelEmail.Visible = (idx == 3);

            if (idx == 0) UpdateSlidePreview();
            else if (idx == 2 && cboSound != null && cboSound.SelectedIndex == 0)
            {
                cboSound.SelectedIndex = rbQuizWrong.Checked ? (int)SlideActionSound.Buzzer : (int)SlideActionSound.Chime;
            }
        }

        public static string GetFriendlySlideTitle(SlideNode slide, int index)
        {
            if (slide == null) return "Trang chiếu " + (index + 1);
            if (!string.IsNullOrEmpty(slide.Title) && !slide.Title.StartsWith("Slide "))
                return slide.Title;

            if (slide.Shapes != null)
            {
                foreach (var sh in slide.Shapes)
                {
                    if (!string.IsNullOrEmpty(sh.Text))
                    {
                        string t = sh.Text.Trim().Replace("\r", " ").Replace("\n", " ");
                        if (t.Length > 32) t = t.Substring(0, 30) + "...";
                        if (!string.IsNullOrEmpty(t)) return t;
                    }
                }
            }
            return "Trang chiếu " + (index + 1);
        }

        public static Bitmap RenderMiniSlidePreview(PresentationRoot pres, int slideIdx, int width, int height)
        {
            Bitmap bmp = new Bitmap(width, height);
            using (Graphics g = Graphics.FromImage(bmp))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.Clear(Color.FromArgb(241, 245, 249));

                if (pres == null || pres.Slides == null || slideIdx < 0 || slideIdx >= pres.Slides.Count)
                {
                    using (Font f = new Font("Segoe UI", 9f, FontStyle.Italic))
                    using (StringFormat sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center })
                    {
                        g.DrawString("Điều hướng động khi trình chiếu", f, Brushes.SlateGray, new RectangleF(0, 0, width, height), sf);
                    }
                    return bmp;
                }

                SlideNode slide = pres.Slides[slideIdx];
                if (slide.ThumbnailCache != null)
                {
                    g.DrawImage(slide.ThumbnailCache, new Rectangle(0, 0, width, height));
                    return bmp;
                }

                Color bgCol = Color.FromArgb((int)((uint)slide.BgColor | 0xFF000000));
                using (SolidBrush bgB = new SolidBrush(bgCol))
                {
                    g.FillRectangle(bgB, 0, 0, width, height);
                }

                float scaleX = width / Math.Max(1f, pres.CanvasWidth);
                float scaleY = height / Math.Max(1f, pres.CanvasHeight);

                if (slide.Shapes != null)
                {
                    foreach (var sh in slide.Shapes)
                    {
                        if (!sh.IsVisible || sh.Transform == null) continue;
                        RectangleF r = new RectangleF(
                            sh.Transform.X * scaleX,
                            sh.Transform.Y * scaleY,
                            Math.Max(2f, sh.Transform.Width * scaleX),
                            Math.Max(2f, sh.Transform.Height * scaleY));

                        if (sh.FillOpacity > 0.01f && sh.Type != ShapeType.TextBox)
                        {
                            Color fc = Color.FromArgb((int)(Math.Min(1f, sh.FillOpacity) * 255), Color.FromArgb((int)((uint)sh.FillColor | 0xFF000000)));
                            using (SolidBrush fb = new SolidBrush(fc))
                                g.FillRectangle(fb, r);
                        }
                        if (!string.IsNullOrEmpty(sh.Text))
                        {
                            Color tc = Color.FromArgb((int)((uint)sh.TextColor | 0xFF000000));
                            using (SolidBrush tb = new SolidBrush(tc))
                            using (Font tf = new Font("Segoe UI", Math.Max(5f, Math.Min(11f, sh.FontSize * scaleY * 0.8f))))
                            {
                                g.DrawString(sh.Text, tf, tb, r);
                            }
                        }
                    }
                }
            }
            return bmp;
        }

        private void UpdateSlidePreview()
        {
            if (!_isSlidesMode || lstSlideTargets == null) return;
            SlideTargetItem sel = lstSlideTargets.SelectedItem as SlideTargetItem;
            if (sel == null) return;

            int previewSlideIdx = sel.SlideIndex;
            if (sel.ActionType == SlideActionType.NextSlide)
                previewSlideIdx = Math.Min(_presentation.Slides.Count - 1, _currentSlideIndex + 1);
            else if (sel.ActionType == SlideActionType.PreviousSlide)
                previewSlideIdx = Math.Max(0, _currentSlideIndex - 1);

            if (picSlidePreview.Image != null)
            {
                var old = picSlidePreview.Image;
                picSlidePreview.Image = null;
                old.Dispose();
            }
            picSlidePreview.Image = RenderMiniSlidePreview(_presentation, previewSlideIdx, picSlidePreview.Width, picSlidePreview.Height);
            lblSlidePreviewTitle.Text = sel.Label;
        }

        private void OnOkClicked(object sender, EventArgs e)
        {
            DisplayText = txtText.Text.Trim();
            ScreenTip = txtScreenTip.Text.Trim();
            if (cboSound != null)
                SelectedSound = (SlideActionSound)Math.Max(0, cboSound.SelectedIndex);
            if (chkHighlightClick != null)
                HighlightClick = chkHighlightClick.Checked;

            if (_activeCategoryTab == 0 && _isSlidesMode)
            {
                SlideTargetItem sel = lstSlideTargets.SelectedItem as SlideTargetItem;
                if (sel != null)
                {
                    SelectedActionType = sel.ActionType;
                    SelectedSlideIndex = sel.SlideIndex;
                    ShowAndReturn = chkShowAndReturn.Checked;
                    TargetUrl = (sel.ActionType == SlideActionType.SpecificSlide)
                        ? ("slide:" + (sel.SlideIndex + 1))
                        : ("action:" + sel.ActionType.ToString());
                }
            }
            else if (_activeCategoryTab == 2 && _isSlidesMode)
            {
                SelectedActionType = rbQuizWrong.Checked ? SlideActionType.QuizWrong : SlideActionType.QuizCorrect;
                SelectedSlideIndex = -1;
                TargetUrl = "quiz:" + SelectedActionType.ToString();
            }
            else if (_activeCategoryTab == 3)
            {
                string mail = txtEmail.Text.Trim();
                string sub = txtEmailSubject.Text.Trim();
                TargetUrl = "mailto:" + mail + (string.IsNullOrEmpty(sub) ? "" : "?subject=" + Uri.EscapeDataString(sub));
                SelectedActionType = SlideActionType.Url;
            }
            else
            {
                TargetUrl = txtUrl.Text.Trim();
                if (string.IsNullOrEmpty(TargetUrl)) TargetUrl = "https://zenqoffice.vn";
                SelectedActionType = File.Exists(TargetUrl) ? SlideActionType.OtherFile : SlideActionType.Url;
            }

            if (string.IsNullOrEmpty(DisplayText))
                DisplayText = TargetUrl;
        }
    }

    /// <summary>
    /// Hộp thoại Cài đặt Hành động (Action Settings) chuẩn 100% Microsoft PowerPoint 365.
    /// Bao gồm 2 tab: Nhấp chuột (Mouse Click) và Di chuột qua (Mouse Over),
    /// hỗ trợ Liên kết trang chiếu, Chạy chương trình, Đáp án Trắc nghiệm, Hiện/Ẩn đối tượng giải thích, Phát âm thanh và Làm sáng khi bấm.
    /// </summary>
    public class SlideActionSettingsDialog : Form
    {
        private readonly PresentationRoot _presentation;
        private readonly int _currentSlideIndex;
        private readonly ShapeNode _shape;

        private TabControl _tabControl;
        private ActionTabControls _clickControls;
        private ActionTabControls _hoverControls;

        private class ActionTabControls
        {
            public RadioButton RbNone;
            public RadioButton RbHyperlink;
            public ComboBox CboHyperlinkTarget;
            public ComboBox CboSpecificSlide;
            public PictureBox PicSlidePreview;
            public TextBox TxtUrlOrFile;
            public Button BtnBrowseFile;
            public CheckBox ChkShowAndReturn;

            public RadioButton RbRunProgram;
            public TextBox TxtProgramPath;
            public Button BtnBrowseProgram;

            public RadioButton RbQuizCorrect;
            public RadioButton RbQuizWrong;

            public CheckBox ChkRevealShape;
            public ComboBox CboRevealShape;

            public CheckBox ChkPlaySound;
            public ComboBox CboSound;
            public Button BtnTestSound;
            public CheckBox ChkHighlight;
        }

        private class ShapeChoiceItem
        {
            public string ShapeId;
            public string Label;
            public override string ToString() { return Label; }
        }

        public SlideActionSettingsDialog(PresentationRoot presentation, int currentSlideIndex, ShapeNode shape)
        {
            _presentation = presentation;
            _currentSlideIndex = currentSlideIndex;
            _shape = shape;

            this.Text = "Cài đặt Hành động & Tương tác (Action Settings - PowerPoint 365)";
            this.Size = new Size(580, 565);
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.StartPosition = FormStartPosition.CenterParent;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.BackColor = Color.FromArgb(248, 250, 252);
            this.Font = new Font("Segoe UI", 9.0f);

            _tabControl = new TabControl
            {
                Location = new Point(12, 12),
                Size = new Size(542, 455),
                Font = new Font("Segoe UI", 9.0f)
            };

            TabPage tabClick = new TabPage("🖱️ Nhấp chuột (Mouse Click)") { BackColor = Color.White, Padding = new Padding(12) };
            TabPage tabHover = new TabPage("👆 Di chuột qua (Mouse Over)") { BackColor = Color.White, Padding = new Padding(12) };

            _clickControls = BuildActionTab(tabClick, true);
            _hoverControls = BuildActionTab(tabHover, false);

            _tabControl.TabPages.Add(tabClick);
            _tabControl.TabPages.Add(tabHover);
            this.Controls.Add(_tabControl);

            LoadFromShape();

            Panel bottomPanel = new Panel { Dock = DockStyle.Bottom, Height = 50, BackColor = Color.FromArgb(241, 245, 249) };
            this.Controls.Add(bottomPanel);

            Button btnOk = new Button
            {
                Text = "OK",
                Location = new Point(368, 11),
                Size = new Size(90, 29),
                BackColor = Color.FromArgb(37, 99, 235),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 9.0f, FontStyle.Bold),
                FlatStyle = FlatStyle.Flat,
                DialogResult = DialogResult.OK
            };
            btnOk.FlatAppearance.BorderSize = 0;
            btnOk.Click += (s, e) => SaveToShape();
            bottomPanel.Controls.Add(btnOk);

            Button btnCancel = new Button
            {
                Text = "Hủy",
                Location = new Point(464, 11),
                Size = new Size(88, 29),
                BackColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                DialogResult = DialogResult.Cancel
            };
            btnCancel.FlatAppearance.BorderColor = Color.FromArgb(203, 213, 225);
            bottomPanel.Controls.Add(btnCancel);

            this.AcceptButton = btnOk;
            this.CancelButton = btnCancel;
        }

        private ActionTabControls BuildActionTab(TabPage page, bool isClickTab)
        {
            ActionTabControls c = new ActionTabControls();
            int y = 12;

            GroupBox grpAction = new GroupBox
            {
                Text = isClickTab ? "Hành động khi nhấp chuột (Action on click)" : "Hành động khi di chuột qua (Action on mouse over)",
                Location = new Point(12, y),
                Size = new Size(508, 305),
                Font = new Font("Segoe UI", 9.0f, FontStyle.Bold),
                ForeColor = Color.FromArgb(30, 41, 59)
            };
            page.Controls.Add(grpAction);

            Font regFont = new Font("Segoe UI", 9.0f, FontStyle.Regular);

            c.RbNone = new RadioButton { Text = "Không có (None)", Location = new Point(16, 24), AutoSize = true, Font = regFont, Checked = true };
            grpAction.Controls.Add(c.RbNone);

            c.RbHyperlink = new RadioButton { Text = "Liên kết đến (Hyperlink to):", Location = new Point(16, 50), AutoSize = true, Font = regFont };
            grpAction.Controls.Add(c.RbHyperlink);

            c.CboHyperlinkTarget = new ComboBox
            {
                Location = new Point(36, 74),
                Width = 265,
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = regFont
            };
            c.CboHyperlinkTarget.Items.AddRange(new object[]
            {
                "Trang chiếu tiếp theo (Next Slide)",
                "Trang chiếu trước đó (Previous Slide)",
                "Trang chiếu đầu tiên (First Slide)",
                "Trang chiếu cuối cùng (Last Slide)",
                "Trang chiếu vừa xem (Last Slide Viewed)",
                "Kết thúc trình chiếu (End Show)",
                "Trang chiếu cụ thể... (Slide...)",
                "Địa chỉ Trang Web (URL...)",
                "Tệp khác... (Other File...)"
            });
            c.CboHyperlinkTarget.SelectedIndex = 0;
            grpAction.Controls.Add(c.CboHyperlinkTarget);

            c.CboSpecificSlide = new ComboBox
            {
                Location = new Point(36, 104),
                Width = 265,
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = regFont,
                Visible = false
            };
            if (_presentation != null && _presentation.Slides != null)
            {
                for (int i = 0; i < _presentation.Slides.Count; i++)
                {
                    string st = InsertHyperlinkForm.GetFriendlySlideTitle(_presentation.Slides[i], i);
                    c.CboSpecificSlide.Items.Add(string.Format("Trang {0}: {1}", i + 1, st));
                }
                if (c.CboSpecificSlide.Items.Count > 0) c.CboSpecificSlide.SelectedIndex = 0;
            }
            grpAction.Controls.Add(c.CboSpecificSlide);

            c.TxtUrlOrFile = new TextBox
            {
                Location = new Point(36, 104),
                Width = 200,
                Font = regFont,
                PlaceholderText = "https://...",
                Visible = false
            };
            grpAction.Controls.Add(c.TxtUrlOrFile);

            c.BtnBrowseFile = new Button
            {
                Text = "Duyệt...",
                Location = new Point(240, 103),
                Size = new Size(61, 25),
                Font = regFont,
                Visible = false
            };
            c.BtnBrowseFile.Click += (s, e) =>
            {
                using (OpenFileDialog ofd = new OpenFileDialog())
                {
                    if (ofd.ShowDialog(this) == DialogResult.OK)
                        c.TxtUrlOrFile.Text = ofd.FileName;
                }
            };
            grpAction.Controls.Add(c.BtnBrowseFile);

            c.ChkShowAndReturn = new CheckBox
            {
                Text = "Hiển thị và quay lại (Show and return)",
                Location = new Point(36, 133),
                AutoSize = true,
                Font = regFont
            };
            grpAction.Controls.Add(c.ChkShowAndReturn);

            // Mini Slide Preview inside Action Settings
            c.PicSlidePreview = new PictureBox
            {
                Location = new Point(316, 48),
                Size = new Size(176, 102),
                BackColor = Color.FromArgb(241, 245, 249),
                BorderStyle = BorderStyle.FixedSingle,
                SizeMode = PictureBoxSizeMode.Zoom
            };
            grpAction.Controls.Add(c.PicSlidePreview);

            // Run Program
            c.RbRunProgram = new RadioButton { Text = "Chạy chương trình (Run program):", Location = new Point(16, 160), AutoSize = true, Font = regFont };
            grpAction.Controls.Add(c.RbRunProgram);

            c.TxtProgramPath = new TextBox { Location = new Point(36, 184), Width = 200, Font = regFont, Enabled = false };
            grpAction.Controls.Add(c.TxtProgramPath);

            c.BtnBrowseProgram = new Button { Text = "Duyệt...", Location = new Point(240, 183), Size = new Size(61, 25), Font = regFont, Enabled = false };
            c.BtnBrowseProgram.Click += (s, e) =>
            {
                using (OpenFileDialog ofd = new OpenFileDialog { Filter = "Chương trình (*.exe;*.bat)|*.exe;*.bat|Tất cả (*.*)|*.*" })
                {
                    if (ofd.ShowDialog(this) == DialogResult.OK)
                        c.TxtProgramPath.Text = ofd.FileName;
                }
            };
            grpAction.Controls.Add(c.BtnBrowseProgram);

            // Quiz Correct / Wrong Radio Options
            c.RbQuizCorrect = new RadioButton
            {
                Text = "✅ Trắc nghiệm: Đáp án ĐÚNG (Đổi màu Xanh lá + Hiệu ứng chúc mừng)",
                Location = new Point(16, 216),
                AutoSize = true,
                Font = new Font("Segoe UI", 9.0f, FontStyle.Bold),
                ForeColor = Color.FromArgb(21, 128, 61)
            };
            grpAction.Controls.Add(c.RbQuizCorrect);

            c.RbQuizWrong = new RadioButton
            {
                Text = "❌ Trắc nghiệm: Đáp án SAI (Đổi màu Đỏ + Hiệu ứng rung lắc cảnh báo)",
                Location = new Point(16, 242),
                AutoSize = true,
                Font = new Font("Segoe UI", 9.0f, FontStyle.Bold),
                ForeColor = Color.FromArgb(185, 28, 28)
            };
            grpAction.Controls.Add(c.RbQuizWrong);

            // Reveal / Toggle Explanation Shape on Click
            if (isClickTab)
            {
                c.ChkRevealShape = new CheckBox
                {
                    Text = "💡 Hiện / Ẩn hình giải thích khi bấm:",
                    Location = new Point(16, 271),
                    AutoSize = true,
                    Font = regFont
                };
                grpAction.Controls.Add(c.ChkRevealShape);

                c.CboRevealShape = new ComboBox
                {
                    Location = new Point(248, 268),
                    Width = 244,
                    DropDownStyle = ComboBoxStyle.DropDownList,
                    Font = regFont,
                    Enabled = false
                };
                if (_presentation != null && _currentSlideIndex >= 0 && _currentSlideIndex < _presentation.Slides.Count)
                {
                    SlideNode curSlide = _presentation.Slides[_currentSlideIndex];
                    for (int i = 0; i < curSlide.Shapes.Count; i++)
                    {
                        ShapeNode sh = curSlide.Shapes[i];
                        if (_shape != null && sh.Id == _shape.Id) continue;
                        c.CboRevealShape.Items.Add(new ShapeChoiceItem
                        {
                            ShapeId = sh.Id,
                            Label = string.Format("{0}. {1}", i + 1, sh.GetDisplayName(i))
                        });
                    }
                    if (c.CboRevealShape.Items.Count > 0) c.CboRevealShape.SelectedIndex = 0;
                }
                c.ChkRevealShape.CheckedChanged += (s, e) => c.CboRevealShape.Enabled = c.ChkRevealShape.Checked;
                grpAction.Controls.Add(c.CboRevealShape);
            }

            // Sound & Highlight GroupBox
            GroupBox grpFeedback = new GroupBox
            {
                Text = "Âm thanh & Phản hồi Trực quan",
                Location = new Point(12, 325),
                Size = new Size(508, 92),
                Font = new Font("Segoe UI", 9.0f, FontStyle.Bold),
                ForeColor = Color.FromArgb(30, 41, 59)
            };
            page.Controls.Add(grpFeedback);

            c.ChkPlaySound = new CheckBox { Text = "Phát âm thanh (Play sound):", Location = new Point(16, 26), AutoSize = true, Font = regFont };
            grpFeedback.Controls.Add(c.ChkPlaySound);

            c.CboSound = new ComboBox
            {
                Location = new Point(205, 23),
                Width = 210,
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = regFont,
                Enabled = false
            };
            foreach (SlideActionSound snd in Enum.GetValues(typeof(SlideActionSound)))
            {
                if (snd == SlideActionSound.CustomFile) continue;
                c.CboSound.Items.Add(SlideSoundPlayer.GetSoundDisplayName(snd));
            }
            c.CboSound.SelectedIndex = 0;
            grpFeedback.Controls.Add(c.CboSound);

            c.BtnTestSound = new Button { Text = "🔊 Nghe", Location = new Point(422, 22), Size = new Size(70, 25), Font = regFont, Enabled = false };
            c.BtnTestSound.Click += (s, e) =>
            {
                SlideActionSound snd = (SlideActionSound)Math.Max(0, c.CboSound.SelectedIndex);
                SlideSoundPlayer.PlaySound(snd);
            };
            grpFeedback.Controls.Add(c.BtnTestSound);

            c.ChkPlaySound.CheckedChanged += (s, e) =>
            {
                c.CboSound.Enabled = c.ChkPlaySound.Checked;
                c.BtnTestSound.Enabled = c.ChkPlaySound.Checked;
                if (c.ChkPlaySound.Checked && c.CboSound.SelectedIndex == 0)
                    c.CboSound.SelectedIndex = 4; // Click
            };

            c.ChkHighlight = new CheckBox
            {
                Text = isClickTab ? "Làm sáng nổi bật khi nhấp chuột (Highlight click)" : "Làm sáng nổi bật khi di chuột qua (Highlight when mouse over)",
                Location = new Point(16, 58),
                AutoSize = true,
                Font = regFont
            };
            grpFeedback.Controls.Add(c.ChkHighlight);

            // Wire UI state updates
            EventHandler updateUi = (s, e) => UpdateTabUiState(c);
            c.RbNone.CheckedChanged += updateUi;
            c.RbHyperlink.CheckedChanged += updateUi;
            c.RbRunProgram.CheckedChanged += updateUi;
            c.RbQuizCorrect.CheckedChanged += (s, e) =>
            {
                if (c.RbQuizCorrect.Checked)
                {
                    c.ChkPlaySound.Checked = true;
                    c.CboSound.SelectedIndex = (int)SlideActionSound.Chime;
                    c.ChkHighlight.Checked = true;
                }
                UpdateTabUiState(c);
            };
            c.RbQuizWrong.CheckedChanged += (s, e) =>
            {
                if (c.RbQuizWrong.Checked)
                {
                    c.ChkPlaySound.Checked = true;
                    c.CboSound.SelectedIndex = (int)SlideActionSound.Buzzer;
                    c.ChkHighlight.Checked = true;
                }
                UpdateTabUiState(c);
            };
            c.CboHyperlinkTarget.SelectedIndexChanged += updateUi;
            c.CboSpecificSlide.SelectedIndexChanged += updateUi;

            UpdateTabUiState(c);
            return c;
        }

        private void UpdateTabUiState(ActionTabControls c)
        {
            bool isHlink = c.RbHyperlink.Checked;
            c.CboHyperlinkTarget.Enabled = isHlink;
            int hIdx = c.CboHyperlinkTarget.SelectedIndex;

            c.CboSpecificSlide.Visible = isHlink && (hIdx == 6);
            c.TxtUrlOrFile.Visible = isHlink && (hIdx == 7 || hIdx == 8);
            c.BtnBrowseFile.Visible = isHlink && (hIdx == 8);
            c.ChkShowAndReturn.Enabled = isHlink && (hIdx == 6);

            c.TxtProgramPath.Enabled = c.RbRunProgram.Checked;
            c.BtnBrowseProgram.Enabled = c.RbRunProgram.Checked;

            // Update mini preview
            int previewIdx = _currentSlideIndex;
            if (isHlink && _presentation != null && _presentation.Slides != null)
            {
                if (hIdx == 0) previewIdx = Math.Min(_presentation.Slides.Count - 1, _currentSlideIndex + 1);
                else if (hIdx == 1) previewIdx = Math.Max(0, _currentSlideIndex - 1);
                else if (hIdx == 2) previewIdx = 0;
                else if (hIdx == 3) previewIdx = _presentation.Slides.Count - 1;
                else if (hIdx == 6 && c.CboSpecificSlide.SelectedIndex >= 0) previewIdx = c.CboSpecificSlide.SelectedIndex;
                else previewIdx = -1;
            }
            if (c.PicSlidePreview.Image != null)
            {
                var old = c.PicSlidePreview.Image;
                c.PicSlidePreview.Image = null;
                old.Dispose();
            }
            c.PicSlidePreview.Image = InsertHyperlinkForm.RenderMiniSlidePreview(_presentation, previewIdx, c.PicSlidePreview.Width, c.PicSlidePreview.Height);
        }

        private void LoadFromShape()
        {
            if (_shape == null) return;
            PopulateControlsFromAction(_clickControls, _shape.ClickActionType, _shape.ClickTargetSlideIndex, _shape.ClickTargetUrl, _shape.ClickShowAndReturn, _shape.ClickSound, _shape.ClickHighlight, _shape.ClickRevealShapeId);
            PopulateControlsFromAction(_hoverControls, _shape.HoverActionType, _shape.HoverTargetSlideIndex, _shape.HoverTargetUrl, _shape.HoverShowAndReturn, _shape.HoverSound, _shape.HoverHighlight, "");
        }

        private void PopulateControlsFromAction(ActionTabControls c, SlideActionType type, int slideIdx, string url, bool showAndReturn, SlideActionSound sound, bool highlight, string revealShapeId)
        {
            c.ChkShowAndReturn.Checked = showAndReturn;
            c.ChkHighlight.Checked = highlight;
            c.ChkPlaySound.Checked = (sound != SlideActionSound.None);
            if (sound != SlideActionSound.None && (int)sound < c.CboSound.Items.Count)
                c.CboSound.SelectedIndex = (int)sound;

            if (c.ChkRevealShape != null && !string.IsNullOrEmpty(revealShapeId))
            {
                for (int i = 0; i < c.CboRevealShape.Items.Count; i++)
                {
                    ShapeChoiceItem sci = c.CboRevealShape.Items[i] as ShapeChoiceItem;
                    if (sci != null && sci.ShapeId == revealShapeId)
                    {
                        c.ChkRevealShape.Checked = true;
                        c.CboRevealShape.SelectedIndex = i;
                        break;
                    }
                }
            }

            switch (type)
            {
                case SlideActionType.None:
                    c.RbNone.Checked = true;
                    break;
                case SlideActionType.NextSlide:
                    c.RbHyperlink.Checked = true;
                    c.CboHyperlinkTarget.SelectedIndex = 0;
                    break;
                case SlideActionType.PreviousSlide:
                    c.RbHyperlink.Checked = true;
                    c.CboHyperlinkTarget.SelectedIndex = 1;
                    break;
                case SlideActionType.FirstSlide:
                    c.RbHyperlink.Checked = true;
                    c.CboHyperlinkTarget.SelectedIndex = 2;
                    break;
                case SlideActionType.LastSlide:
                    c.RbHyperlink.Checked = true;
                    c.CboHyperlinkTarget.SelectedIndex = 3;
                    break;
                case SlideActionType.LastSlideViewed:
                    c.RbHyperlink.Checked = true;
                    c.CboHyperlinkTarget.SelectedIndex = 4;
                    break;
                case SlideActionType.EndShow:
                    c.RbHyperlink.Checked = true;
                    c.CboHyperlinkTarget.SelectedIndex = 5;
                    break;
                case SlideActionType.SpecificSlide:
                    c.RbHyperlink.Checked = true;
                    c.CboHyperlinkTarget.SelectedIndex = 6;
                    if (slideIdx >= 0 && slideIdx < c.CboSpecificSlide.Items.Count)
                        c.CboSpecificSlide.SelectedIndex = slideIdx;
                    break;
                case SlideActionType.Url:
                    c.RbHyperlink.Checked = true;
                    c.CboHyperlinkTarget.SelectedIndex = 7;
                    c.TxtUrlOrFile.Text = url;
                    break;
                case SlideActionType.OtherFile:
                    c.RbHyperlink.Checked = true;
                    c.CboHyperlinkTarget.SelectedIndex = 8;
                    c.TxtUrlOrFile.Text = url;
                    break;
                case SlideActionType.RunProgram:
                    c.RbRunProgram.Checked = true;
                    c.TxtProgramPath.Text = url;
                    break;
                case SlideActionType.QuizCorrect:
                    c.RbQuizCorrect.Checked = true;
                    break;
                case SlideActionType.QuizWrong:
                    c.RbQuizWrong.Checked = true;
                    break;
            }
            UpdateTabUiState(c);
        }

        private void ReadActionFromControls(ActionTabControls c, out SlideActionType type, out int slideIdx, out string url, out bool showAndReturn, out SlideActionSound sound, out bool highlight, out string revealShapeId)
        {
            type = SlideActionType.None;
            slideIdx = -1;
            url = "";
            showAndReturn = c.ChkShowAndReturn.Checked;
            highlight = c.ChkHighlight.Checked;
            sound = c.ChkPlaySound.Checked ? (SlideActionSound)Math.Max(0, c.CboSound.SelectedIndex) : SlideActionSound.None;
            revealShapeId = "";

            if (c.ChkRevealShape != null && c.ChkRevealShape.Checked && c.CboRevealShape.SelectedItem is ShapeChoiceItem sci)
            {
                revealShapeId = sci.ShapeId;
            }

            if (c.RbHyperlink.Checked)
            {
                switch (c.CboHyperlinkTarget.SelectedIndex)
                {
                    case 0: type = SlideActionType.NextSlide; break;
                    case 1: type = SlideActionType.PreviousSlide; break;
                    case 2: type = SlideActionType.FirstSlide; break;
                    case 3: type = SlideActionType.LastSlide; break;
                    case 4: type = SlideActionType.LastSlideViewed; break;
                    case 5: type = SlideActionType.EndShow; break;
                    case 6:
                        type = SlideActionType.SpecificSlide;
                        slideIdx = Math.Max(0, c.CboSpecificSlide.SelectedIndex);
                        url = "slide:" + (slideIdx + 1);
                        break;
                    case 7:
                        type = SlideActionType.Url;
                        url = c.TxtUrlOrFile.Text.Trim();
                        break;
                    case 8:
                        type = SlideActionType.OtherFile;
                        url = c.TxtUrlOrFile.Text.Trim();
                        break;
                }
            }
            else if (c.RbRunProgram.Checked)
            {
                type = SlideActionType.RunProgram;
                url = c.TxtProgramPath.Text.Trim();
            }
            else if (c.RbQuizCorrect.Checked)
            {
                type = SlideActionType.QuizCorrect;
            }
            else if (c.RbQuizWrong.Checked)
            {
                type = SlideActionType.QuizWrong;
            }
        }

        public void ApplyToShape(ShapeNode shape)
        {
            SaveToShape();
        }

        private void SaveToShape()
        {
            if (_shape == null) return;

            SlideActionType cType; int cSlide; string cUrl; bool cRet; SlideActionSound cSnd; bool cHi; string cRev;
            ReadActionFromControls(_clickControls, out cType, out cSlide, out cUrl, out cRet, out cSnd, out cHi, out cRev);
            _shape.ClickActionType = cType;
            _shape.ClickTargetSlideIndex = cSlide;
            _shape.ClickTargetUrl = cUrl;
            _shape.ClickShowAndReturn = cRet;
            _shape.ClickSound = cSnd;
            _shape.ClickHighlight = cHi;
            _shape.ClickRevealShapeId = cRev;

            SlideActionType hType; int hSlide; string hUrl; bool hRet; SlideActionSound hSnd; bool hHi; string hRev;
            ReadActionFromControls(_hoverControls, out hType, out hSlide, out hUrl, out hRet, out hSnd, out hHi, out hRev);
            _shape.HoverActionType = hType;
            _shape.HoverTargetSlideIndex = hSlide;
            _shape.HoverTargetUrl = hUrl;
            _shape.HoverShowAndReturn = hRet;
            _shape.HoverSound = hSnd;
            _shape.HoverHighlight = hHi;
        }
    }
}
