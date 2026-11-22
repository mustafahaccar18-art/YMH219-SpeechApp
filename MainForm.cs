using System;
using System.Drawing;
using System.Linq;
using System.Speech.Synthesis;
using System.Speech.Recognition;
using System.Windows.Forms;

namespace SpeechApp
{
    public class MainForm : Form
    {
        // ── Speech engines ────────────────────────────────────────
        private SpeechSynthesizer? _synth;
        private SpeechRecognitionEngine? _recognizer;
        private bool _ttsPaused;
        private bool _sttRunning;

        // ── TTS controls ──────────────────────────────────────────
        private RichTextBox rtbTtsInput   = null!;
        private Button      btnTtsStart   = null!;
        private Button      btnTtsPause   = null!;
        private Button      btnTtsStop    = null!;
        private Button      btnTtsClear   = null!;
        private Label       lblTtsStatus  = null!;
        private TrackBar    tbRate        = null!;
        private TrackBar    tbVolume      = null!;
        private Label       lblRate       = null!;
        private Label       lblVolume     = null!;

        // ── STT controls ──────────────────────────────────────────
        private RichTextBox rtbSttOutput  = null!;
        private Button      btnSttStart   = null!;
        private Button      btnSttStop    = null!;
        private Button      btnSttClear   = null!;
        private Label       lblSttStatus  = null!;

        // ── Status bar ────────────────────────────────────────────
        private Label lblActiveOp = null!;

        // ── Colours ───────────────────────────────────────────────
        private static readonly Color C_Blue   = Color.FromArgb(37,  99, 235);
        private static readonly Color C_Green  = Color.FromArgb(22, 163,  74);
        private static readonly Color C_Red    = Color.FromArgb(220,  38,  38);
        private static readonly Color C_Orange = Color.FromArgb(217, 119,   6);
        private static readonly Color C_Gray   = Color.FromArgb(107, 114, 128);
        private static readonly Color C_BgMain = Color.FromArgb(243, 244, 246);
        private static readonly Color C_Card   = Color.White;

        public MainForm()
        {
            BuildUI();
            InitializeSynth();
        }

        // ══════════════════════════════════════════════════════════
        //  UI BUILD
        // ══════════════════════════════════════════════════════════
        private void BuildUI()
        {
            SuspendLayout();

            Text            = "Speech App — TTS & STT";
            Size            = new Size(1000, 680);
            MinimumSize     = new Size(800,  520);
            StartPosition   = FormStartPosition.CenterScreen;
            BackColor       = C_BgMain;
            Font            = new Font("Segoe UI", 9.5f);

            // ── Root table: two columns (TTS | STT) + status row ──
            var root = new TableLayoutPanel
            {
                Dock        = DockStyle.Fill,
                ColumnCount = 2,
                RowCount    = 2,
                BackColor   = C_BgMain,
                Padding     = new Padding(10)
            };
            root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 36));

            root.Controls.Add(BuildTtsPanel(), 0, 0);
            root.Controls.Add(BuildSttPanel(), 1, 0);
            root.Controls.Add(BuildStatusBar(), 0, 1);
            root.SetColumnSpan(root.GetControlFromPosition(0, 1), 2);

            Controls.Add(root);
            ResumeLayout(true);
        }

        // ── TTS panel ─────────────────────────────────────────────
        private Panel BuildTtsPanel()
        {
            var card = MakeCard();

            // Header
            var header = new Label
            {
                Text      = "📢  Metinden Sese  (Text-to-Speech)",
                Dock      = DockStyle.Top,
                Height    = 36,
                ForeColor = C_Blue,
                Font      = new Font("Segoe UI", 10.5f, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleLeft,
                Padding   = new Padding(4, 0, 0, 0)
            };

            // Text input
            rtbTtsInput = new RichTextBox
            {
                Dock        = DockStyle.Fill,
                ScrollBars  = RichTextBoxScrollBars.Vertical,
                BackColor   = Color.FromArgb(249, 250, 251),
                BorderStyle = BorderStyle.FixedSingle,
                Font        = new Font("Segoe UI", 10.5f)
            };

            // Sliders panel
            var sliderPanel = new TableLayoutPanel
            {
                Dock        = DockStyle.Bottom,
                Height      = 56,
                ColumnCount = 4,
                RowCount    = 2,
                BackColor   = C_Card,
                Padding     = new Padding(4, 2, 4, 0)
            };
            sliderPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 68));
            sliderPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            sliderPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 68));
            sliderPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            sliderPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 50));
            sliderPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 50));

            lblRate   = new Label { Text = "Hız: 0",   Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleRight, ForeColor = C_Gray };
            lblVolume = new Label { Text = "Ses: 100", Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleRight, ForeColor = C_Gray };

            tbRate = new TrackBar
            {
                Dock = DockStyle.Fill, Minimum = -10, Maximum = 10, Value = 0,
                TickFrequency = 5, AutoSize = false
            };
            tbRate.ValueChanged += (s, e) => lblRate.Text = $"Hız: {tbRate.Value}";

            tbVolume = new TrackBar
            {
                Dock = DockStyle.Fill, Minimum = 0, Maximum = 100, Value = 100,
                TickFrequency = 25, AutoSize = false
            };
            tbVolume.ValueChanged += (s, e) => lblVolume.Text = $"Ses: {tbVolume.Value}";

            sliderPanel.Controls.Add(lblRate,   0, 0);
            sliderPanel.Controls.Add(tbRate,    1, 0);
            sliderPanel.Controls.Add(lblVolume, 2, 0);
            sliderPanel.Controls.Add(tbVolume,  3, 0);

            // Buttons row
            var btnPanel = new TableLayoutPanel
            {
                Dock        = DockStyle.Bottom,
                Height      = 44,
                ColumnCount = 4,
                RowCount    = 1,
                BackColor   = C_Card,
                Padding     = new Padding(4, 4, 4, 0)
            };
            for (int i = 0; i < 4; i++)
                btnPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25));

            btnTtsStart  = MakeBtn("▶  Başlat",   C_Blue,   Color.White);
            btnTtsPause  = MakeBtn("⏸  Duraklat", C_Orange, Color.White);
            btnTtsStop   = MakeBtn("⏹  Durdur",   C_Red,    Color.White);
            btnTtsClear  = MakeBtn("🗑  Temizle",  C_Gray,   Color.White);

            btnTtsStart.Click += BtnTtsStart_Click;
            btnTtsPause.Click += BtnTtsPause_Click;
            btnTtsStop .Click += BtnTtsStop_Click;
            btnTtsClear.Click += BtnTtsClear_Click;

            btnPanel.Controls.Add(btnTtsStart, 0, 0);
            btnPanel.Controls.Add(btnTtsPause, 1, 0);
            btnPanel.Controls.Add(btnTtsStop,  2, 0);
            btnPanel.Controls.Add(btnTtsClear, 3, 0);

            // Status
            lblTtsStatus = new Label
            {
                Dock      = DockStyle.Bottom,
                Height    = 22,
                Text      = "Durum: Hazır",
                ForeColor = C_Blue,
                Font      = new Font("Segoe UI", 8.5f, FontStyle.Italic),
                Padding   = new Padding(6, 0, 0, 0)
            };

            // Separator line
            var sep = new Panel { Dock = DockStyle.Bottom, Height = 1, BackColor = Color.FromArgb(229, 231, 235) };

            card.Controls.Add(rtbTtsInput);   // Fill
            card.Controls.Add(sliderPanel);   // Bottom (added after Fill so it stacks below)
            card.Controls.Add(btnPanel);
            card.Controls.Add(sep);
            card.Controls.Add(lblTtsStatus);
            card.Controls.Add(header);        // Top

            return card;
        }

        // ── STT panel ─────────────────────────────────────────────
        private Panel BuildSttPanel()
        {
            var card = MakeCard();
            card.Margin = new Padding(6, 0, 0, 0);

            var header = new Label
            {
                Text      = "🎤  Sesten Metne  (Speech-to-Text)",
                Dock      = DockStyle.Top,
                Height    = 36,
                ForeColor = C_Green,
                Font      = new Font("Segoe UI", 10.5f, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleLeft,
                Padding   = new Padding(4, 0, 0, 0)
            };

            rtbSttOutput = new RichTextBox
            {
                Dock        = DockStyle.Fill,
                ReadOnly    = true,
                ScrollBars  = RichTextBoxScrollBars.Vertical,
                BackColor   = Color.FromArgb(240, 253, 244),
                BorderStyle = BorderStyle.FixedSingle,
                Font        = new Font("Segoe UI", 10.5f)
            };

            var btnPanel = new TableLayoutPanel
            {
                Dock        = DockStyle.Bottom,
                Height      = 44,
                ColumnCount = 3,
                RowCount    = 1,
                BackColor   = C_Card,
                Padding     = new Padding(4, 4, 4, 0)
            };
            for (int i = 0; i < 3; i++)
                btnPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.33f));

            btnSttStart = MakeBtn("🎙  Dinlemeye Başla", C_Green, Color.White);
            btnSttStop  = MakeBtn("⏹  Durdur",          C_Red,   Color.White);
            btnSttClear = MakeBtn("🗑  Temizle",         C_Gray,  Color.White);

            btnSttStart.Click += BtnSttStart_Click;
            btnSttStop .Click += BtnSttStop_Click;
            btnSttClear.Click += BtnSttClear_Click;

            btnPanel.Controls.Add(btnSttStart, 0, 0);
            btnPanel.Controls.Add(btnSttStop,  1, 0);
            btnPanel.Controls.Add(btnSttClear, 2, 0);

            lblSttStatus = new Label
            {
                Dock      = DockStyle.Bottom,
                Height    = 22,
                Text      = "Durum: Hazır",
                ForeColor = C_Green,
                Font      = new Font("Segoe UI", 8.5f, FontStyle.Italic),
                Padding   = new Padding(6, 0, 0, 0)
            };

            var sep = new Panel { Dock = DockStyle.Bottom, Height = 1, BackColor = Color.FromArgb(229, 231, 235) };

            card.Controls.Add(rtbSttOutput);
            card.Controls.Add(btnPanel);
            card.Controls.Add(sep);
            card.Controls.Add(lblSttStatus);
            card.Controls.Add(header);

            return card;
        }

        // ── Status bar ────────────────────────────────────────────
        private Panel BuildStatusBar()
        {
            var bar = new Panel { Dock = DockStyle.Fill, BackColor = Color.FromArgb(30, 41, 59) };
            lblActiveOp = new Label
            {
                Dock      = DockStyle.Fill,
                ForeColor = Color.White,
                TextAlign = ContentAlignment.MiddleCenter,
                Font      = new Font("Segoe UI", 9f, FontStyle.Bold),
                Text      = "Aktif İşlem:  Boşta"
            };
            bar.Controls.Add(lblActiveOp);
            return bar;
        }

        // ══════════════════════════════════════════════════════════
        //  TTS LOGIC
        // ══════════════════════════════════════════════════════════
        private void InitializeSynth()
        {
            try
            {
                _synth = new SpeechSynthesizer();
                _synth.SpeakStarted   += (s, e) => SetTtsStatus("Konuşuyor...", C_Blue);
                _synth.SpeakCompleted += Synth_SpeakCompleted;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"TTS motoru başlatılamadı:\n{ex.Message}",
                    "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BtnTtsStart_Click(object? s, EventArgs e)
        {
            if (_synth == null) return;
            string text = rtbTtsInput.Text.Trim();
            if (string.IsNullOrEmpty(text))
            {
                MessageBox.Show("Lütfen seslendirilecek metni girin.", "Uyarı",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            if (_ttsPaused)
            {
                _synth.Resume(); _ttsPaused = false;
                SetTtsStatus("Devam ediyor...", C_Blue);
                UpdateActive("▶  TTS çalışıyor");
                return;
            }
            try
            {
                _synth.Rate   = tbRate.Value;
                _synth.Volume = tbVolume.Value;
                _synth.SpeakAsyncCancelAll();
                _synth.SpeakAsync(text);
                UpdateActive("▶  TTS çalışıyor");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Konuşma başlatılamadı:\n{ex.Message}", "Hata",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BtnTtsPause_Click(object? s, EventArgs e)
        {
            if (_synth?.State == SynthesizerState.Speaking)
            {
                _synth.Pause(); _ttsPaused = true;
                SetTtsStatus("Duraklatıldı", C_Orange);
                UpdateActive("⏸  TTS duraklatıldı");
            }
        }

        private void BtnTtsStop_Click(object? s, EventArgs e)
        {
            _synth?.SpeakAsyncCancelAll(); _ttsPaused = false;
            SetTtsStatus("Durduruldu", C_Red);
            UpdateActive("Boşta");
        }

        private void BtnTtsClear_Click(object? s, EventArgs e)
        {
            _synth?.SpeakAsyncCancelAll(); _ttsPaused = false;
            rtbTtsInput.Clear();
            SetTtsStatus("Hazır", C_Blue);
            UpdateActive("Boşta");
        }

        private void Synth_SpeakCompleted(object? s, SpeakCompletedEventArgs e)
        {
            Invoke(() =>
            {
                _ttsPaused = false;
                SetTtsStatus(e.Cancelled ? "İptal edildi" : "Tamamlandı ✓", C_Green);
                UpdateActive("Boşta");
            });
        }

        // ══════════════════════════════════════════════════════════
        //  STT LOGIC
        // ══════════════════════════════════════════════════════════
        private void BtnSttStart_Click(object? s, EventArgs e)
        {
            if (_sttRunning) return;
            try
            {
                // Pick the best available recognizer — prefer Turkish, fall back to whatever is installed
                var installed = SpeechRecognitionEngine.InstalledRecognizers();
                if (installed.Count == 0)
                    throw new InvalidOperationException("Sistemde hiçbir konuşma tanıma motoru bulunamadı.");

                RecognizerInfo? chosen =
                    installed.FirstOrDefault(r => r.Culture.Name.StartsWith("tr", StringComparison.OrdinalIgnoreCase))
                    ?? installed.FirstOrDefault(r => r.Culture.Name.StartsWith("en", StringComparison.OrdinalIgnoreCase))
                    ?? installed[0];

                _recognizer = new SpeechRecognitionEngine(chosen);
                _recognizer.LoadGrammar(new DictationGrammar());
                _recognizer.SetInputToDefaultAudioDevice();
                _recognizer.SpeechRecognized         += Recognizer_SpeechRecognized;
                _recognizer.SpeechRecognitionRejected += (_, __) => SetSttStatus("🔴 Dinleniyor...", C_Red);
                _recognizer.RecognizeCompleted        += Recognizer_Completed;
                _recognizer.RecognizeAsync(RecognizeMode.Multiple);
                _sttRunning = true;
                SetSttStatus($"🔴 Dinleniyor... ({chosen.Culture.Name})", C_Red);
                UpdateActive("🎙  STT dinleniyor");
                btnSttStart.Enabled = false;
            }
            catch (Exception ex)
            {
                string msg = ex.Message.Contains("audio", StringComparison.OrdinalIgnoreCase)
                    ? "Mikrofon bulunamadı veya erişilemiyor.\nLütfen mikrofonunuzu bağlayıp tekrar deneyin."
                    : $"Konuşma tanıma başlatılamadı:\n{ex.Message}";
                MessageBox.Show(msg, "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
                SetSttStatus("Hata oluştu", C_Red);
            }
        }

        private void BtnSttStop_Click(object? s, EventArgs e) => StopRecognizer();

        private void BtnSttClear_Click(object? s, EventArgs e)
        {
            StopRecognizer();
            rtbSttOutput.Clear();
            SetSttStatus("Hazır", C_Green);
            UpdateActive("Boşta");
        }

        private void StopRecognizer()
        {
            try { _recognizer?.RecognizeAsyncStop(); } catch { }
            _sttRunning = false;
            btnSttStart.Enabled = true;
            SetSttStatus("Durduruldu", C_Orange);
            UpdateActive("Boşta");
        }

        private void Recognizer_SpeechRecognized(object? s, SpeechRecognizedEventArgs e)
        {
            if (e.Result.Confidence < 0.4f) return;
            Invoke(() => { rtbSttOutput.AppendText(e.Result.Text + " "); rtbSttOutput.ScrollToCaret(); });
        }

        private void Recognizer_Completed(object? s, RecognizeCompletedEventArgs e)
        {
            Invoke(() =>
            {
                _sttRunning = false; btnSttStart.Enabled = true;
                SetSttStatus("Tamamlandı", C_Green);
                if (e.Error != null)
                {
                    string errMsg = e.Error.HResult == unchecked((int)0x80070005)
                        ? "Mikrofon erişimi reddedildi!\n\n" +
                          "Lütfen şu adımları izleyin:\n" +
                          "1. Windows Ayarlar → Gizlilik ve Güvenlik → Mikrofon\n" +
                          "2. 'Mikrofon erişimi' → Açık\n" +
                          "3. 'Masaüstü uygulamalarının mikrofona erişmesine izin ver' → Açık"
                        : $"Tanıma hatası:\n{e.Error.Message}";
                    MessageBox.Show(errMsg, "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            });
        }

        // ══════════════════════════════════════════════════════════
        //  HELPERS
        // ══════════════════════════════════════════════════════════
        private void SetTtsStatus(string t, Color c)
        {
            if (InvokeRequired) { Invoke(() => SetTtsStatus(t, c)); return; }
            lblTtsStatus.Text = $"Durum: {t}"; lblTtsStatus.ForeColor = c;
        }
        private void SetSttStatus(string t, Color c)
        {
            if (InvokeRequired) { Invoke(() => SetSttStatus(t, c)); return; }
            lblSttStatus.Text = $"Durum: {t}"; lblSttStatus.ForeColor = c;
        }
        private void UpdateActive(string op)
        {
            if (InvokeRequired) { Invoke(() => UpdateActive(op)); return; }
            lblActiveOp.Text = $"Aktif İşlem:  {op}";
        }

        private static Panel MakeCard() => new Panel
        {
            Dock      = DockStyle.Fill,
            BackColor = C_Card,
            Padding   = new Padding(8),
            Margin    = new Padding(0, 0, 6, 0)
        };

        private static Button MakeBtn(string text, Color back, Color fore) => new Button
        {
            Text      = text,
            Dock      = DockStyle.Fill,
            BackColor = back,
            ForeColor = fore,
            FlatStyle = FlatStyle.Flat,
            Font      = new Font("Segoe UI", 9f, FontStyle.Bold),
            Cursor    = Cursors.Hand,
            Margin    = new Padding(2),
            FlatAppearance = { BorderSize = 0 }
        };

        // ── Cleanup ───────────────────────────────────────────────
        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            _synth?.SpeakAsyncCancelAll(); _synth?.Dispose();
            try { _recognizer?.RecognizeAsyncStop(); } catch { }
            _recognizer?.Dispose();
            base.OnFormClosing(e);
        }
    }
}
