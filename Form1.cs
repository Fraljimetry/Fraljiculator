/// DATE: 2023.4~5, 2024.9~11, 2025.1, 2026.1
/// DESIGNER: Fraljimetry
/// PRECISION: System.Single (float)

using System.Runtime.CompilerServices; // MethodImpl (AggressiveInlining = 256, AggressiveOptimization = 512)
using System.Runtime.InteropServices; // DllImport, StructLayout
using System.Drawing.Imaging; // BitmapData
using System.Text; // StringBuilder

namespace Fraljiculator
{
    /// <summary>
    /// DISPLAY SECTION
    /// </summary>
    public partial class Graph : Form
    {
        // 1.PREPARATIONS
        #region Fields
        private static System.Media.SoundPlayer ClickPlayer;
        private static WMPLib.WindowsMediaPlayer MediaPlayer;
        private static DateTime TimeNow = new();
        private static TimeSpan TimeCount = new();
        private static System.Windows.Forms.Timer GraphTimer, ColorTimer, WaitTimer, DisplayTimer;
        private static Graphics graphics;
        private static Rectangle rectangle, rect_mac, rect_mic; // rect_: slightly larger than the display regions
        private static Bitmap bmp_mac, bmp_mic, bmp_screen; // bmp_screen: snapshots
        private static readonly Bitmap BMP_PIXEL = new(1, 1);
        private static readonly Size SIZE_PIXEL = new(1, 1);
        private static readonly SolidBrush BACK_BRUSH = new(Color.Black);
        private static readonly Pen BDR_PEN = new(Color.Gray), _BDR_PEN = new(Color.White), AXES_PEN = new(Color.DarkGray, 4f);
        private static readonly Color CORRECT_GREEN = Argb(192, 255, 192), ERROR_RED = Argb(255, 192, 192),
            UNCHECK_YELLOW = Argb(255, 255, 128), READONLY_PURPLE = Argb(255, 192, 255),
            COMBO_BLUE = Argb(192, 255, 255), FOCUS_GRAY = Color.LightGray, BACKDROP_GRAY = Argb(64, 64, 64),
            CONTROL_GRAY = Argb(105, 105, 105), GRID_GRAY = Argb(75, 255, 255, 255), READONLY_GRAY = Color.Gainsboro,
            UPPER_GOLD = Color.Gold, LOWER_BLUE = Color.RoyalBlue, ZERO_BLUE = Color.Lime, POLE_PURPLE = Color.Magenta;

        private static float scale_factor, title_elapsed, pause_pos, epsilon, stride, mod_stride, arg_stride, stride_real, size_real, decay;
        private static readonly float GRID_WIDTH_1 = 3f, GRID_WIDTH_2 = 2f, CURVE_WIDTH_LIMIT = 20f, STRIDE = 0.25f, MOD = 0.25f,
            ARG = MathF.PI / 12, STRIDE_REAL = 1f, EPS_REAL = 0.015f, EPS_COMPLEX = 0.015f, SIZE_REAL = 0.5f, DECAY = 0.2f, DEPTH = 2f,
            CURVE_WIDTH = 5f, INCREMENT = 0.01f, TITLE = 0.01f;
        private static int display_elapsed, x_left, x_right, y_up, y_down, color_mode, contour_mode,
            loop_number, chosen_number, export_number, pixel_number, segment_number;
        private static readonly int X_LEFT_MAC = 620, X_RIGHT_MAC = 1520, Y_UP_MAC = 45, Y_DOWN_MAC = 945,
            X_LEFT_MIC = 1565, X_RIGHT_MIC = 1765, Y_UP_MIC = 745, Y_DOWN_MIC = 945, X_LEFT_CHECK = 1921,
            X_RIGHT_CHECK = 1922, Y_UP_CHECK = 1081, Y_DOWN_CHECK = 1082, REF_POS_1 = 9, REF_POS_2 = 27,
            WIDTH_IND = 22, HEIGHT_IND = 55, LEFT_SUPP = 11, TOP_SUPP = 45, GRID = 5, UPDATE = 5, REFRESH = 100, SLEEP = 200;
        private static float[] scopes; // WARNING: scopes[3] - scopes[2] < 0 < borders[3] - borders[2]
        private static int[] borders; // = [ x_left, x_right, y_up, y_down ];
        private static Matrix<Complex> output_complex;
        private static Matrix<float> output_real;

        private static bool is_flashing, is_paused = true, is_complex = true, delete_point = true, delete_coor, swap_colors,
            is_auto, freeze_graph, clicked, shade, axes_drawn_mac, axes_drawn_mic, is_main, activate_mouse, is_checking,
            error_input, error_address, is_resized, ctrl_pressed, sft_pressed, suppress_key_up, bdp_painted, music_sound;
        private static readonly string ADDRESS_DEFAULT = @"C:\Users\Public", DATE = "Oct, 2024", STOCKPILE = "stockpile",
            INPUT_DEFAULT = "z", GENERAL_DEFAULT = "e", THICK_DEFAULT = "1", DENSE_DEFAULT = "1", MACRO = "MACRO",
            MICRO = "MICRO", ZERO = "0", REMIND_EXPORT = "Snapshot saved at", REMIND_STORE = "History stored at",
            CAPTION_DEFAULT = "Yours inputs will be shown here.", MISTAKES_HEAD = "\r\nCommon mistakes include:",
            WRONG_FORMAT = "THE INPUT IS IN A WRONG FORMAT.", WRONG_ADDRESS = "THE ADDRESS DOES NOT EXIST.",
            DISPLAY_ERROR = "UNAVAILABLE.", DRAFT_DEFAULT = "\r\nDetailed historical info is documented here.",
            TEMP_BGM_NAME = "background_music", MUSIC = "music", SOUND = "click sound", TIP = "ReadOnly",
            SEP_1 = new('>', 3), SEP_2 = new('<', 3), SEP = new('~', 3), _SEP = new('*', 20), TAB = new(' ', 4);
        private static readonly string[] CONTOUR_MODES = ["Cartesian (x,y)", "Polar (r,θ)"], COLOR_MODES =
            ["Commonplace", "Monochromatic", "Bichromatic", "Kaleidoscopic", "Miscellaneous"];
        #endregion

        #region Initilizations
        public Graph()
        {
            InitializeComponent();
            SetTitleBarColor(); ReduceFontSizeByScale(this, ref scale_factor);
            if (music_sound) { InitializeMusicPlayer(); InitializeClickSound(); AttachClickEvents(this); }
            InitializeTimers(); InitializeGraphics();
            InitializeCombo(); InitializeData();
            SetThicknessDensenessScopesBorders();
            BanMouseWheel();
        }
        private void Graph_Load(object sender, EventArgs e) => TextBoxFocus(sender, e);
        private void Graph_Paint(object sender, PaintEventArgs e) { if (!bdp_painted && !clicked) SubtitleBox_DoubleClick(sender, e); }
        //
        private int SetTitleBarColor()
        {
            int mode = 1;  // Set to 1 to apply immersive color mode
            const int DWMWA_USE_IMMERSIVE_DARK_MODE = 20; // Desktop Window Manager (DWM)
            return DwmSetWindowAttribute(Handle, DWMWA_USE_IMMERSIVE_DARK_MODE, ref mode, Unsafe.SizeOf<Int32>());
        }
        public static void ReduceFontSizeByScale(Control parentCtrl, ref float scalingFactor)
        {
            scalingFactor = Graphics.FromHwnd(IntPtr.Zero).DpiX / 96f / 1.5f; // Originally my PC was scaled to 150%
            foreach (Control ctrl in parentCtrl.Controls)
            {
                ctrl.Font = new(ctrl.Font.FontFamily, ctrl.Font.Size / scalingFactor, ctrl.Font.Style);
                if (ctrl.Controls.Count > 0) ReduceFontSizeByScale(ctrl, ref scalingFactor);
            }
        } // Also used for Message Boxes, so scalingFactor should not be passed as a field
        private static Stream? GetStream(string file)
            => System.Reflection.Assembly.GetExecutingAssembly().GetManifestResourceStream($"{typeof(Program).Namespace}.{file}.wav");
        private static void InitializeMusicClick(Stream? stream, string message, Action<Stream> setUpStream)
        { if (stream != null) setUpStream(stream); else GetMusicClickErrorBox(message); }
        private void InitializeMusicPlayer() => InitializeMusicClick(GetStream("bgm"), MUSIC, soundStream =>
        {
            MediaPlayer = new(); // Saving the stream to a temp file, since WMP cannot play directly from the stream
            string tempFile = Path.Combine(Path.GetTempPath(), $"{TEMP_BGM_NAME}.wav");
            using FileStream fileStream = new(tempFile, FileMode.Create, FileAccess.Write); // "using" should not be removed
            soundStream.CopyTo(fileStream);
            MediaPlayer.URL = tempFile; // Setting the media file
            MediaPlayer.settings.setMode("loop", true); // Looping the music
            MediaPlayer.controls.stop();
        });
        private static void InitializeClickSound() => InitializeMusicClick(GetStream("click"), SOUND, soundStream =>
        { ClickPlayer = new(soundStream); });
        private static void AttachClickEvents(Control ctrl)
        {
            ctrl.Click += (sender, e) => ClickPlayer?.Play();
            foreach (Control childCtrl in ctrl.Controls) AttachClickEvents(childCtrl);
        }
        private void InitializeTimers()
        {
            static System.Windows.Forms.Timer setT(int interval) => new() { Interval = interval };
            GraphTimer = setT(1000); ColorTimer = setT(50); WaitTimer = setT(500); DisplayTimer = setT(1000 / UPDATE);

            ColorTimer.Tick += (sender, e) =>
            {
                title_elapsed += TITLE;
                TitleLabel.ForeColor = ObtainColorWheelCurve(title_elapsed % 1);
            };
            WaitTimer.Tick += (sender, e) =>
            {
                ReverseBool(ref is_flashing); // Cannnot pass properties as reference
                PictureWait.Visible = is_flashing;
            };
            DisplayTimer.Tick += (sender, e) =>
            {
                if (++display_elapsed % UPDATE == 0) SetText(TimeDisplay, (display_elapsed / UPDATE).ToString() + "s");
                SetText(PointNumDisplay, (pixel_number + segment_number).ToString()); // Refreshing $"{RATE}" times per second
            };
        }
        private void InitializeGraphics()
        {
            graphics = CreateGraphics();
            bmp_mac = bmp_mic = new(Width, Height, PixelFormat.Format32bppArgb);
            bmp_screen = new(Width - WIDTH_IND, Height - HEIGHT_IND);

            rectangle = new(0, 0, Width, Height);
            int indent = (int)(CURVE_WIDTH_LIMIT / 2), _indent = indent * 2,
                widthMac = X_RIGHT_MAC - X_LEFT_MAC, heightMac = Y_DOWN_MAC - Y_UP_MAC,
                widthMic = X_RIGHT_MIC - X_LEFT_MIC, heightMic = Y_DOWN_MIC - Y_UP_MIC;
            rect_mac = new(X_LEFT_MAC - indent, Y_UP_MAC - indent, widthMac + _indent, heightMac + _indent);
            rect_mic = new(X_LEFT_MIC - indent, Y_UP_MIC - indent, widthMic + _indent, heightMic + _indent);

            DoubleBuffered = KeyPreview = true; // Essential for shortcuts
        }
        private void InitializeCombo()
        {
            static void coloringContour_AddItem(ComboBox cbx, int index, string[] options)
            { cbx.Items.AddRange(options); cbx.SelectedIndex = index; }
            coloringContour_AddItem(ComboColoring, 4, COLOR_MODES);
            coloringContour_AddItem(ComboContour, 1, CONTOUR_MODES);

            void addExamples(string[] items) { foreach (string item in items) ComboExamples.Items.Add(item); }
            addExamples(ReplaceTags.EX_COMPLEX);
            ComboExamples.Items.Add(String.Empty);
            addExamples(ReplaceTags.EX_REAL);
            ComboExamples.Items.Add(String.Empty);
            addExamples(ReplaceTags.EX_CURVES);

            void functionsSpecial_AddItem(string[] options, bool isFunc)
            {
                string[] modifiedOptions = new string[options.Length]; int index = 0;
                foreach (string option in options) modifiedOptions[index++] = String.Concat(option, RecoverMultiply.LR_BRA);
                Action<string[]> addOptions = isFunc ? ComboFunctions.Items.AddRange : ComboSpecial.Items.AddRange;
                addOptions(modifiedOptions);
            }
            functionsSpecial_AddItem(ReplaceTags.FUNCTIONS, true);
            functionsSpecial_AddItem(ReplaceTags.SPECIALS, false);
        }
        private void RecoverInput()
        {
            SetText(InputString, INPUT_DEFAULT); SetText(AddressInput, ADDRESS_DEFAULT);
            SetText(GeneralInput, GENERAL_DEFAULT);
            SetText(ThickInput, THICK_DEFAULT); SetText(DenseInput, DENSE_DEFAULT);
            InputString_Focus();
        }
        private void InitializeData() { RecoverInput(); SetText(DraftBox, DRAFT_DEFAULT); SetText(CaptionBox, CAPTION_DEFAULT); }
        private void SetThicknessDensenessScopesBorders(bool autoFill = true)
        {
            FillEmpty(GeneralInput, GENERAL_DEFAULT);
            FillEmpty(ThickInput, THICK_DEFAULT); FillEmpty(DenseInput, DENSE_DEFAULT);

            TextBox[] tbxDetails = { X_Left, X_Right, Y_Right, Y_Left }; // Crucial ordering
            if (autoFill) foreach (var tbx in tbxDetails) FillEmpty(tbx, ZERO);

            float _dense = Obtain(DenseInput), _thick = Obtain(ThickInput);
            stride_real = STRIDE_REAL / _dense; stride = STRIDE / _dense;
            mod_stride = MOD / _dense; arg_stride = ARG / _dense;
            epsilon = (is_complex ? EPS_COMPLEX : EPS_REAL) * _thick; // For lines and complex extremities
            size_real = SIZE_REAL * _thick / (1 + _thick); // For real extremities
            decay = DECAY * _thick;

            if (!GeneralInput_Undo())
            {
                float _scope = Obtain(GeneralInput);
                scopes = [-_scope, _scope, _scope, -_scope]; // Remind the signs
                for (int i = 0; i < tbxDetails.Length; i++) SetText(tbxDetails[i], scopes[i].ToString("#0.0000"));
            }
            else for (int i = 0; i < tbxDetails.Length; i++) scopes[i] = Obtain(tbxDetails[i]);
            MyString.ThrowException(InvalidScopesX() || InvalidScopesY()); // The detailed exception is determined later

            borders = [x_left, x_right, y_up, y_down];
        }
        private void BanMouseWheel()
        {
            ComboBox[] comboBoxes = { ComboExamples, ComboFunctions, ComboSpecial, ComboColoring, ComboContour };
            foreach (var cbx in comboBoxes) cbx.MouseWheel += (sender, e) => ((HandledMouseEventArgs)e).Handled = true;
        } // The default wheeling clashes with the personalized combo boxes
        private void TextBoxFocus(object sender, EventArgs e)
        {
            foreach (var ctrl in Controls.OfType<TextBox>())
                ctrl.GotFocus += (sender, e) => { ((TextBox)sender).SelectionStart = ((TextBox)sender).Text.Length; };
        } // Forcing the caret to appear at the end of each textbox
        #endregion

        #region External Methods
        [DllImport("dwmapi.dll")]
        static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int attrValue, int attrSize);
        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool BlockInput([MarshalAs(UnmanagedType.Bool)] bool fBlockIt);
        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        public static extern bool HideCaret(IntPtr hWnd); // Also used for Message Boxes
        protected override void WndProc(ref Message m) // Window Procedure
        {
            const int WM_NCLBTNDOWN = 0x00A1, HTCAPTION = 0x0002; //Window Message, Non-Client Left Button Down, Hit Test Caption
            if (m.Msg == WM_NCLBTNDOWN && m.WParam.ToInt32() == HTCAPTION) return; // Preventing dragging the title bar
            base.WndProc(ref m);
        } // Overriding WndProc to customize window behavior
        #endregion

        #region Shorthands
        private static int AddOne(int input) => input + 1;
        private static Color Swap(Color c1, Color c2) => swap_colors ? c1 : c2;
        private static Color Argb(int a, int r, int g, int b) => Color.FromArgb(a, r, g, b);
        public static Color Argb(int r, int g, int b) => Color.FromArgb(r, g, b); // Also used for Message Boxes
        public static float ArgRGB(float x, float y) => Single.IsNaN(x) && Single.IsNaN(y) ? -1 : y == 0 ?
            (x == 0 ? -1 : x > 0 ? 0 : MathF.PI) : (y > 0 ? MathF.Atan2(y, x) : MathF.Atan2(y, x) + MathF.Tau); // Sensitive checking
        private static int Frac(int input, float alpha) => (int)(input * alpha);
        private static bool IllegalRatio(float ratio) => ratio < 0 || ratio > 1;
        private static int GetRow(int[] borders) => borders[1] - borders[0];
        private static int GetColumn(int[] borders) => borders[3] - borders[2];
        private static float Get_Row() => scopes[1] - scopes[0];
        private static float Get_Column() => scopes[3] - scopes[2]; // The sign convention varies from place to place
        private static bool InvalidScopesX() => scopes[0] >= scopes[1];
        private static bool InvalidScopesY() => scopes[3] >= scopes[2];
        private static int[] GetBorders(int mode) => mode switch
        {
            1 => [X_LEFT_MAC, X_RIGHT_MAC, Y_UP_MAC, Y_DOWN_MAC],
            2 => [X_LEFT_MIC, X_RIGHT_MIC, Y_UP_MIC, Y_DOWN_MIC],
            3 => [X_LEFT_CHECK, X_RIGHT_CHECK, Y_UP_CHECK, Y_DOWN_CHECK]
        };
        private static Matrix<float> GetMatrix(int rows, int columns) => new(RealComplex.GetArithProg(rows, columns), columns);
        private static Rectangle GetRect(int[] borders, int margin = 0)
            => new(borders[0] + margin, borders[2] + margin, GetRow(borders) - margin, GetColumn(borders) - margin);
        private static Bitmap GetBitmap(bool isMain) => isMain ? bmp_mac : bmp_mic;
        private static ref bool ReturnAxesDrawn(bool isMain) => ref (isMain ? ref axes_drawn_mac : ref axes_drawn_mic);
        private static void SetAxesDrawn(bool isMain, bool drawn = false) { ReturnAxesDrawn(isMain) = drawn; }
        private static void ReverseBool(ref bool isChecked) => isChecked = !isChecked;
        private static float Obtain(string text) => RealSub.Obtain(RecoverMultiply.Simplify(text));
        private static float Obtain(TextBox tbx) => Obtain(tbx.Text);
        private static void SetText(TextBox tbx, string text) => tbx.Text = text;
        private static void FillEmpty(TextBox tbx, string text) { if (String.IsNullOrEmpty(tbx.Text)) SetText(tbx, text); }
        private void AddDraft(string text) => SetText(DraftBox, text + DraftBox.Text);
        private void SetScrollBars(bool enabled) => VScrollBarX.Enabled = VScrollBarY.Enabled = enabled;
        private bool GeneralInput_Undo() => GeneralInput.Text == ZERO;
        private void ComboExamples_Undo() => ComboExamples.SelectedIndex = -1;
        private void InputString_Focus() { InputString.Focus(); InputString.SelectionStart = InputString.Text.Length; }
        private bool NoInput() => String.IsNullOrEmpty(InputString.Text);
        private bool ProcessingGraphics() => InputString.ReadOnly;
        #endregion

        #region Auxiliary Drawings
        private static void DrawBackdrop(int[] borders)
        { graphics.DrawRectangle(BDR_PEN, GetRect(borders)); graphics.FillRectangle(BACK_BRUSH, GetRect(borders, 1)); }
        private static void DrawAxesGrids(int[] borders)
        {
            bdp_painted = true; // To prevent calling Graph_Paint afterwards
            var (xInit, yInit, xEnd, yEnd) = (AddOne(borders[0]), AddOne(borders[2]), borders[1], borders[3]);
            static float calculateGrid(float range) => MathF.Pow(GRID, MathF.Floor(MathF.Log(range / 2, GRID)));
            var (xGrid, yGrid) = (calculateGrid(Get_Row()), calculateGrid(-Get_Column())); // Remind the minus sign

            void drawGrids(float xGrid, float yGrid, float penWidth)
            {
                Pen gridPen = new(GRID_GRAY, penWidth);
                RealComplex.For((int)MathF.Floor(scopes[3] / yGrid), (int)MathF.Ceiling(scopes[2] / yGrid), i =>
                {
                    int pos = LinearTransformY(i * yGrid, borders);
                    if (pos >= yInit && pos < yEnd) graphics.DrawLine(gridPen, xInit, pos, xEnd, pos);
                });
                RealComplex.For((int)MathF.Floor(scopes[0] / xGrid), (int)MathF.Ceiling(scopes[1] / xGrid), i =>
                {
                    int pos = LinearTransformX(i * xGrid, borders);
                    if (pos >= xInit && pos < xEnd) graphics.DrawLine(gridPen, pos, yEnd, pos, yInit);
                });
            }
            drawGrids(xGrid, yGrid, GRID_WIDTH_1);
            drawGrids(xGrid / GRID, yGrid / GRID, GRID_WIDTH_2);

            var (x, y) = (LinearTransformX(0f, borders), LinearTransformY(0f, borders)); // Should not write "0.0" or "0"
            if (y >= yInit && y < yEnd) graphics.DrawLine(AXES_PEN, xInit, y, xEnd, y);
            if (x >= xInit && x < xEnd) graphics.DrawLine(AXES_PEN, x, yEnd, x, yInit);
        }
        private static void DrawBackdropAxesGrids(int[] borders, bool isMain, bool IsFrozen = false)
        {
            if (!IsFrozen) { DrawBackdrop(borders); SetAxesDrawn(isMain); }
            if (!delete_coor && !ReturnAxesDrawn(isMain)) { DrawAxesGrids(borders); SetAxesDrawn(isMain, true); }
        } // Sensitive
        private void DrawReferenceRectangles(Color color)
            => graphics.FillRectangle(new SolidBrush(color), VScrollBarX.Location.X - REF_POS_1, Y_UP_MIC + REF_POS_2,
                2 * (VScrollBarX.Width + REF_POS_1), VScrollBarX.Height - 2 * REF_POS_2);
        private void DrawScrollBar((float x, float y) xyCoor)
        {
            int range = VScrollBarX.Maximum - VScrollBarX.Minimum;
            VScrollBarX.Value = Frac(range, (xyCoor.x - scopes[0]) / Get_Row());
            VScrollBarY.Value = Frac(range, (xyCoor.y - scopes[3]) / -Get_Column()); // Remind the minus sign
        }
        #endregion

        // 2.GRAPHING
        #region Numerics
        private static float GetRatioRow(int[] borders) => Get_Row() / GetRow(borders);
        private static float GetRatioColumn(int[] borders) => Get_Column() / GetColumn(borders);
        private static int LinearTransformX(float x, int[] borders) => (int)(borders[0] + (x - scopes[0]) / GetRatioRow(borders));
        private static int LinearTransformY(float y, int[] borders) => (int)(borders[2] + (y - scopes[2]) / GetRatioColumn(borders));
        private static (float, float) LinearTransform(int x, int y, float xCoor, float yCoor, int[] borders)
            => (scopes[0] + (x - borders[0]) * xCoor, scopes[2] + (y - borders[2]) * yCoor); // For optimization
        private static int LowIdx(float a, float m) => (int)MathF.Floor(a / m);
        private static float LowDist(float a, float m) => a - m * LowIdx(a, m);
        private static float LowRatio(float a, float m) => a == -0 ? 1 : LowDist(a, m) / m; // -0 is necessary
        private static float GetShade(float alpha) => (alpha - 1) / DEPTH + 1;
        private unsafe static (float, float) FiniteExtremities(Matrix<float> output, int rows, int columns)
        {
            static float seekM(Func<float, float, float> function, float* ptr, int length)
            {
                float _value = Single.NaN;
                for (int i = 0; i < length; i++, ptr++)
                { if (Single.IsNaN(*ptr)) continue; if (Single.IsNaN(_value)) _value = *ptr; else _value = function(*ptr, _value); }
                return _value;
            }
            Matrix<float> outputAtan = GetMatrix(rows, columns), minMax = GetMatrix(2, rows); // Necessary
            Parallel.For(0, rows, p =>
            {
                float* destPtr = outputAtan.RowPtr(p), _destPtr = destPtr, srcPtr = output.RowPtr(p);
                for (int q = 0; q < columns; q++, destPtr++, srcPtr++) *destPtr = MathF.Atan(*srcPtr);
                minMax[0, p] = seekM(MathF.Min, _destPtr, columns);
                minMax[1, p] = seekM(MathF.Max, _destPtr, columns);
            });
            return (seekM(MathF.Min, minMax.RowPtr(0), rows), seekM(MathF.Max, minMax.RowPtr(1), rows));
        } // To find the min and max of the atan'ed matrix to prevent infinitude
        private unsafe static (int, int, Matrix<float>, Matrix<float>) GetRowColumnCoor()
        {
            var (rows, columns) = (GetRow(borders), GetColumn(borders));
            var (xCoor, yCoor) = (GetMatrix(rows, columns), GetMatrix(rows, columns));
            var (xInit, yInit, _x, _y) = (AddOne(borders[0]), AddOne(borders[2]), GetRatioRow(borders), GetRatioColumn(borders));
            Parallel.For(0, rows, p =>
            {
                float* xPtr = xCoor.RowPtr(p), yPtr = yCoor.RowPtr(p); int _xInit = xInit + p, _yInit = yInit; // Must NOT be pre-defined
                for (int q = 0; q < columns; q++, xPtr++, yPtr++, _yInit++) (*xPtr, *yPtr) = LinearTransform(_xInit, _yInit, _x, _y, borders);
            });
            return (rows, columns, xCoor, yCoor);
        }
        #endregion

        #region Rendering Core
        private static (int, BitmapData) GetBppBmpData(Bitmap bmp) // bpp: bytesPerPixel
            => (Image.GetPixelFormatSize(bmp.PixelFormat) / 8, bmp.LockBits(rectangle, ImageLockMode.ReadWrite, bmp.PixelFormat));
        private unsafe static void ClearBitmap(Bitmap bmp)
        {
            var (bpp, bmpData) = GetBppBmpData(bmp); var bmpInit = (byte*)bmpData.Scan0 + bpp - 1;
            Parallel.For(0, rectangle.Height, y =>
            {
                byte* pixelPtr = bmpInit + y * bmpData.Stride;
                for (int x = 0; x < rectangle.Width; x++, pixelPtr += bpp) *pixelPtr = 0; // It suffices to set color.A to zero
            }); // Deliberate loop order
            bmp.UnlockBits(bmpData);
        }
        private unsafe void SetPixelFast(byte* _ptr, Color color, ref int pixNum)
        { pixNum++; *_ptr = color.B; _ptr++; *_ptr = color.G; _ptr++; *_ptr = color.R; _ptr++; *_ptr = color.A; }
        private unsafe void RealSpecial(byte* _ptr, Color _zero, Color _pole, float _value, (float min, float max) mM, ref int pixNum)
        {
            if (_value < Single.Lerp(mM.min, mM.max, size_real)) SetPixelFast(_ptr, _zero, ref pixNum);
            if (_value > Single.Lerp(mM.max, mM.min, size_real)) SetPixelFast(_ptr, _pole, ref pixNum);
        }
        private unsafe void ComplexSpecial(byte* _ptr, Color _zero, Color _pole, float _value, ref int pixNum)
        {
            if (_value < epsilon) SetPixelFast(_ptr, _zero, ref pixNum);
            if (_value > 1 / epsilon) SetPixelFast(_ptr, _pole, ref pixNum);
        }
        private delegate void PixelLoop(int x, int y, IntPtr pixelPtr, ref int pixNum); // Instead of Action<int, int, IntPtr, ref int>
        private unsafe static void LoopBase(PixelLoop pixelLoop)
        {
            Bitmap bmp = GetBitmap(is_main); var (bpp, bmpData) = GetBppBmpData(bmp);
            var (xInit, yInit) = (AddOne(borders[0]), AddOne(borders[2])); var (xLen, yLen) = (borders[1] - xInit, borders[3] - yInit);
            try
            {
                int[] pixNums = new int[yLen]; var bmpInit = (byte*)bmpData.Scan0 + yInit * bmpData.Stride + xInit * bpp;
                Parallel.For(0, yLen, y =>
                {
                    int pixNum = 0; var pixelPtr = (IntPtr)(bmpInit + y * bmpData.Stride);
                    for (int x = 0; x < xLen; x++, pixelPtr += bpp) pixelLoop(x, y, pixelPtr, ref pixNum);
                    pixNums[y] = pixNum;
                }); // Deliberate loop order
                fixed (int* ptr = pixNums) { int* _ptr = ptr; for (int y = 0; y < yLen; y++, _ptr++) pixel_number += *_ptr; }
            }
            finally { bmp.UnlockBits(bmpData); }
        }
        //
        private unsafe void RealLoop(Matrix<float> output, Color _zero, Color _pole, Func<float, Color> extractor, (float, float) mM)
            => LoopBase((x, y, pixelPtr, ref pixNum) =>
            {
                float _value = output[x, y];
                if (Single.IsNaN(_value)) return;
                var _pixelPtr = (byte*)pixelPtr;
                SetPixelFast(_pixelPtr, extractor(_value), ref pixNum);
                if (!delete_point) RealSpecial(_pixelPtr, _zero, _pole, MathF.Atan(_value), mM, ref pixNum);
            });
        private unsafe void ComplexLoop(Matrix<Complex> output, Color _zero, Color _pole, Func<Complex, Color> extractor)
            => LoopBase((x, y, pixelPtr, ref pixNum) =>
            {
                Complex _value = output[x, y];
                if (Single.IsNaN(_value.real) || Single.IsNaN(_value.imaginary)) return;
                var _pixelPtr = (byte*)pixelPtr;
                SetPixelFast(_pixelPtr, extractor(_value), ref pixNum);
                if (!delete_point) ComplexSpecial(_pixelPtr, _zero, _pole, Complex.Modulus(_value), ref pixNum);
            });
        private static Func<float, Color> GetColorReal123(int mode) => _value =>
        {
            Color func23(Color c1, Color c2) => _value < 0 ? Swap(c1, c2) : _value > 0 ? Swap(c2, c1) : Color.Empty;
            return mode switch
            {
                1 => MathF.Abs(_value) < epsilon ? Swap(Color.Black, Color.White) : Color.Empty,
                2 => func23(Color.White, Color.Black),
                3 => func23(UPPER_GOLD, LOWER_BLUE)
            };
        };
        private static Func<float, Color> GetColorReal45(bool mode, (float min, float max) mM) => _value => mode ?
            ObtainColorStrip(_value, mM.min, mM.max) :
            ObtainColorStrip(_value, mM.min, mM.max, GetShade(LowRatio(_value, stride_real)));
        private static Func<Complex, Color> GetColorComplex123(int mode, bool isReIm) => input =>
        {
            Complex _value = isReIm ? input : Complex.Log(input); var (v1, v2) = (_value.real, _value.imaginary);
            var (s1, s2) = isReIm ? (stride, stride) : (mod_stride, arg_stride);
            var (c1, c2) = mode switch
            {
                1 => (Color.White, Color.Black),
                2 => (Color.Black, Color.White),
                3 => (LOWER_BLUE, UPPER_GOLD)
            };
            bool draw = mode != 1 ? Single.IsEvenInteger(LowIdx(v1, s1) + LowIdx(v2, s2))
                : MathF.Min(MathF.Min(LowDist(v1, s1), LowDist(v2, s2)), MathF.Min(-LowDist(v1, -s1), -LowDist(v2, -s2))) < epsilon;
            return mode == 1 ? (draw ? Swap(c2, c1) : Color.Empty) : (draw ? Swap(c1, c2) : Swap(c2, c1));
        };
        private static Func<Complex, Color> GetColorComplex45(bool mode) => mode ? (c => ObtainColorWheel(c, alpha: 1)) : (_value =>
        {
            Complex _valueLog = Complex.Log(_value);
            float alpha = (LowRatio(_valueLog.real, mod_stride) + LowRatio(_valueLog.imaginary, arg_stride)) / 2;
            return ObtainColorWheel(_value, GetShade(alpha));
        });
        private void RealLoop123(Matrix<float> output, Color _zero, Color _pole, int mode, (float, float) mM)
            => RealLoop(output, _zero, _pole, GetColorReal123(mode), mM);
        private void RealLoop45(Matrix<float> output, bool mode, (float, float) mM)
            => RealLoop(output, Color.Black, Color.White, GetColorReal45(mode, mM), mM);
        private void ComplexLoop123(Matrix<Complex> output, Color _zero, Color _pole, int mode, bool isReIm)
            => ComplexLoop(output, _zero, _pole, GetColorComplex123(mode, isReIm));
        private void ComplexLoop45(Matrix<Complex> output, bool mode)
            => ComplexLoop(output, Color.Black, Color.White, GetColorComplex45(mode));
        #endregion

        #region Rendering
        private void RealComputation()
        {
            Action<Matrix<float>, (float, float)> realOperation = color_mode switch
            {
                1 => Real1,
                2 => Real2,
                3 => Real3,
                4 => Real4,
                5 => Real5
            };
            realOperation(output_real, FiniteExtremities(output_real, GetRow(borders), GetColumn(borders)));
        }
        private void Real1(Matrix<float> output, (float, float) mM) => RealLoop123(output, ZERO_BLUE, POLE_PURPLE, 1, mM);
        private void Real2(Matrix<float> output, (float, float) mM) => RealLoop123(output, ZERO_BLUE, POLE_PURPLE, 2, mM);
        private void Real3(Matrix<float> output, (float, float) mM) => RealLoop123(output, Color.Black, Color.White, 3, mM);
        private void Real4(Matrix<float> output, (float, float) mM) => RealLoop45(output, true, mM);
        private void Real5(Matrix<float> output, (float, float) mM) => RealLoop45(output, false, mM);
        private void ComplexComputation()
        {
            bool isReIm = contour_mode == 1;
            Action<Matrix<Complex>> complexOperation = color_mode switch
            {
                1 => isReIm ? Complex1_ReIm : Complex1_ModArg,
                2 => isReIm ? Complex2_ReIm : Complex2_ModArg,
                3 => isReIm ? Complex3_ReIm : Complex3_ModArg,
                4 => Complex4,
                5 => Complex5
            };
            complexOperation(output_complex);
        }
        private void Complex1_ReIm(Matrix<Complex> output) => ComplexLoop123(output, ZERO_BLUE, POLE_PURPLE, 1, true);
        private void Complex2_ReIm(Matrix<Complex> output) => ComplexLoop123(output, ZERO_BLUE, POLE_PURPLE, 2, true);
        private void Complex3_ReIm(Matrix<Complex> output) => ComplexLoop123(output, Color.Black, Color.White, 3, true);
        private void Complex1_ModArg(Matrix<Complex> output) => ComplexLoop123(output, ZERO_BLUE, POLE_PURPLE, 1, false);
        private void Complex2_ModArg(Matrix<Complex> output) => ComplexLoop123(output, ZERO_BLUE, POLE_PURPLE, 2, false);
        private void Complex3_ModArg(Matrix<Complex> output) => ComplexLoop123(output, Color.Black, Color.White, 3, false);
        private void Complex4(Matrix<Complex> output) => ComplexLoop45(output, true);
        private void Complex5(Matrix<Complex> output) => ComplexLoop45(output, false);
        #endregion

        #region Curves
        private (float, float, float) SetStartEndIncrement(string[] split, bool isPolar, bool isParam)
        {
            float obtain(int index) => Obtain(split[index]);
            (float, float, float) initializeParamPolar(int relPos)
            {
                MyString.ThrowInvalidLengths(split, [relPos + 2, relPos + 3]);
                return (obtain(relPos), obtain(relPos + 1), split.Length == relPos + 3 ? obtain(relPos + 2) : INCREMENT);
            }
            if (isParam) return initializeParamPolar(3);
            else if (isPolar) return initializeParamPolar(2);
            else
            {
                MyString.ThrowInvalidLengths(split, [0, 1, 2, 3, 4]); float range = Obtain(GeneralInput);
                float getRange(TextBox tbx, bool minus) => GeneralInput_Undo() ? Obtain(tbx) : (minus ? -range : range);
                return (split.Length < 3 ? getRange(X_Left, true) : obtain(1),
                    split.Length < 3 ? getRange(X_Right, false) : obtain(2),
                    split.Length == 2 ? obtain(1) : split.Length == 4 ? obtain(3) : INCREMENT);
            }
        }
        private unsafe static (Matrix<float>, Matrix<float>, int, bool) SetCurveValues(string[] split, bool isPolar, bool isParam,
            float start, float end, float increment)
        {
            string replace(string s, int index) => s.Replace(split[index], "x");
            string tag1 = ReplaceTags.FUNC_HEAD + ReplaceTags.COS, tag2 = ReplaceTags.FUNC_HEAD + ReplaceTags.SIN,
                input1 = isParam ? replace(split[0], 2) : isPolar ? replace($"({split[0]})*{tag1}({split[1]})", 1) : "x",
                input2 = isParam ? replace(split[1], 2) : isPolar ? replace($"({split[0]})*{tag2}({split[1]})", 1) : split[0];

            int length = (int)((end - start) / increment), _length = length + 2; // For safety
            Matrix<float> partition = GetMatrix(1, _length); float steps = start;
            float obtainCheck(string input) => RealSub.Obtain(input, steps); // Already simplified
            if (is_checking) { _ = obtainCheck(input1); _ = obtainCheck(input2); return (partition, partition, length, true); }

            float* partPtr = partition.RowPtr();
            for (int i = 0; i < _length; i++, partPtr++, steps += increment) *partPtr = steps;
            Matrix<float> obtain(string input) => new RealSub(input, partition, null, null, null, null, 1, _length).Obtain();
            return (obtain(input1), obtain(input2), length, false);
        }
        private unsafe void DrawCurve(Matrix<float> value1, Matrix<float> value2, int length)
        {
            float curveWidth = MathF.Min(CURVE_WIDTH * Obtain(ThickInput), CURVE_WIDTH_LIMIT);
            Pen dichoPen(Color c1, Color c2) => new(Swap(c1, c2), curveWidth);
            Pen vividPen = dichoPen(Color.Empty, Color.Empty), defaultPen = dichoPen(Color.Black, Color.White),
                blackPen = dichoPen(Color.White, Color.Black), whitePen = dichoPen(Color.Black, Color.White),
                bluePen = dichoPen(LOWER_BLUE, UPPER_GOLD), yellowPen = dichoPen(UPPER_GOLD, LOWER_BLUE),
                selectedPen = color_mode == 1 ? defaultPen : vividPen;

            Point pos = new(), posBuffer = new(); bool inRange, inRangeBuffer = false; int _ratio, _ratioBuffer = 0;
            float relativeSpeed = Obtain(DenseInput) / length, ratio; float* v1Ptr = value1.RowPtr(), v2Ptr = value2.RowPtr();

            for (int steps = 0; steps <= length; steps++, v1Ptr++, v2Ptr++, segment_number++)
            {
                (pos.X, pos.Y) = (LinearTransformX(*v1Ptr, borders), LinearTransformY(*v2Ptr, borders));
                inRange = *v1Ptr > scopes[0] && *v1Ptr < scopes[1] && *v2Ptr > scopes[3] && *v2Ptr < scopes[2];
                if (inRangeBuffer && inRange && posBuffer != pos)
                {
                    ratio = relativeSpeed * steps % 1;
                    selectedPen = color_mode switch
                    {
                        2 => ratio < 0.5f ? whitePen : blackPen,
                        3 => ratio < 0.5f ? bluePen : yellowPen,
                        _ => selectedPen
                    };
                    if (color_mode > 3) vividPen.Color = ObtainColorWheelCurve(ratio);
                    graphics.DrawLine(selectedPen, posBuffer, pos);

                    SetScrollBars(true); // Necessary for each loop
                    DrawScrollBar(LinearTransform(pos.X, pos.Y, GetRatioRow(borders), GetRatioColumn(borders), borders));
                    _ratio = Frac(REFRESH, ratio);
                    if (_ratioBuffer != _ratio) DrawReferenceRectangles(selectedPen.Color);
                    _ratioBuffer = _ratio;
                }
                inRangeBuffer = inRange;
                posBuffer = pos;
            }
        }
        //
        private void DisplayFPPBase(string input, bool isPolar = false, bool isParam = false)
        {
            string[] split = MyString.SplitString(input);
            var (start, end, increment) = SetStartEndIncrement(split, isPolar, isParam);
            MyString.ThrowException(start >= end);
            var (value1, value2, length, isChecking) = SetCurveValues(split, isPolar, isParam, start, end, increment);
            if (isChecking) return;
            DisplayBase(() => { DrawCurve(value1, value2, length); pixel_number += segment_number; segment_number = 0; });
        }
        private void DisplayFunction(string input) => DisplayFPPBase(input); // Necessary
        private void DisplayPolar(string input) => DisplayFPPBase(input, isPolar: true);
        private void DisplayParam(string input) => DisplayFPPBase(input, isParam: true);
        #endregion

        #region Graph Display
        private void DisplayBase(Action drawAction)
        {
            DrawBackdropAxesGrids(borders, is_main, freeze_graph);
            graphics.DrawRectangle(_BDR_PEN, GetRect(borders));
            drawAction();
            SetText(PointNumDisplay, pixel_number.ToString());
            if (is_auto) RunExport();
        }
        private void RunDisplayBase(Action computeAction)
        {
            if (is_checking) return; // Necessary
            ClearBitmap(bmp_mac); ClearBitmap(bmp_mic); // Necessary courtesy of ZAL
            computeAction();
            DisplayBase(() => { graphics.DrawImage(GetBitmap(is_main), 0, 0); });
        }
        private void DisplayRendering(string input)
        {
            var (rows, columns, xCoor, yCoor) = GetRowColumnCoor();
            if (is_complex) output_complex = new ComplexSub(input, xCoor, yCoor, rows, columns).Obtain();
            else output_real = new RealSub(input, xCoor, yCoor, null, null, null, rows, columns).Obtain();
            RunDisplayBase(is_complex ? ComplexComputation : RealComputation);
        }
        private void DisplayIterateLoop(string[] split)
        {
            var (rows, columns, xCoor, yCoor) = GetRowColumnCoor();
            int toInt(int index) => RealSub.ToInt(split[index]);
            int int3 = toInt(3), int4 = is_checking ? int3 : (int)MathF.Max(int3, toInt(4)); // Necessary
            string replaceLoop(int pos, int loops) => MyString.ReplaceLoop(split, pos, 2, loops);
            string obtainDisplayInput(int loops, string defaultInput) => split.Length == 6 ? replaceLoop(5, loops) : defaultInput;

            MyString.ThrowInvalidLengths(split, [5, 6]);
            if (is_complex)
            {
                Matrix<Complex> z = ComplexSub.InitilizeZ(xCoor, yCoor, rows, columns); // Special for complex
                Matrix<Complex> Z = new ComplexSub(split[1], z, null, null, rows, columns).Obtain();
                for (int loops = int3; loops <= int4; loops++)
                {
                    Z = new ComplexSub(replaceLoop(0, loops), z, Z, null, rows, columns).Obtain();
                    output_complex = new ComplexSub(obtainDisplayInput(loops, "Z"), z, Z, null, rows, columns).Obtain();
                    RunDisplayBase(ComplexComputation);
                }
            }
            else
            {
                Matrix<float> X = new RealSub(split[1], xCoor, yCoor, null, null, null, rows, columns).Obtain();
                for (int loops = int3; loops <= int4; loops++)
                {
                    X = new RealSub(replaceLoop(0, loops), xCoor, yCoor, X, null, null, rows, columns).Obtain();
                    output_real = new RealSub(obtainDisplayInput(loops, "y-X"), xCoor, yCoor, X, null, null, rows, columns).Obtain();
                    RunDisplayBase(RealComputation);
                }
            }
        } // Deliberate buffer-free zone, rendering self-contained delay [See: ComplexSub.ProcessSPI, RealSub.ProcessSPI]
        private void DisplayLoop(string input)
        {
            input = ReplaceTags.ReplaceCurves(input); string[] split = MyString.SplitString(input); // Do not merge into one
            bool containsTag(string s) => input.Contains(String.Concat(ReplaceTags.FUNC_HEAD, s, ReplaceTags.UNDERLINE, '('));
            if (containsTag(ReplaceTags.ITLOOP)) { DisplayIterateLoop(split); return; }
            Action<string> displayMethod =
                containsTag(ReplaceTags.FUNC) ? DisplayFunction :
                containsTag(ReplaceTags.POLAR) ? DisplayPolar :
                containsTag(ReplaceTags.PARAM) ? DisplayParam : DisplayRendering;
            int toInt(int index) => RealSub.ToInt(split[index]);
            int int2 = toInt(2), int3 = is_checking ? int2 : (int)MathF.Max(int2, toInt(3)); // Necessary
            for (int loops = int2; loops <= int3; loops++) displayMethod(MyString.ReplaceLoop(split, 0, 1, loops));
        }
        private void DisplayOnScreen()
        {
            if (NoInput()) return; // Necessary
            string[] split = MyString.SplitByChars(RecoverMultiply.Simplify(InputString.Text, is_complex), "|");
            for (int loops = 0; loops < split.Length; loops++)
            {
                bool containsTags(string s1, string s2) => MyString.ContainsAny(split[loops], [s1, s2]);
                Action<string> displayMethod =    // Should not pull outside of the loop
                    containsTags(MyString.LOOP_NAMES[0], MyString.LOOP_NAMES[1]) ? DisplayLoop :
                    containsTags(MyString.FUNC_NAMES[0], MyString.FUNC_NAMES[1]) ? DisplayFunction :
                    containsTags(MyString.FUNC_NAMES[2], MyString.FUNC_NAMES[3]) ? DisplayPolar :
                    containsTags(MyString.FUNC_NAMES[4], MyString.FUNC_NAMES[5]) ? DisplayParam : DisplayRendering;
                displayMethod(split[loops]);
            }
        }
        #endregion

        #region Color Extractors
        private static Color ObtainColorBase(float argument, float alpha, int decay) // alpha: brightness
        {
            if (IllegalRatio(alpha)) return Color.Empty; // Necessary
            float _argument = argument * 3 / MathF.PI; int proportion, region = argument < 0 ? -1 : (int)_argument;
            if (region == 6) region = proportion = 0; else proportion = Frac(255, _argument - region);

            Color getArgb(int r, int g, int b) => Argb(decay, r, g, b);
            return region switch
            {
                0 => getArgb(Frac(255, alpha), Frac(proportion, alpha), 0),
                1 => getArgb(Frac(255 - proportion, alpha), Frac(255, alpha), 0),
                2 => getArgb(0, Frac(255, alpha), Frac(proportion, alpha)),
                3 => getArgb(0, Frac(255 - proportion, alpha), Frac(255, alpha)),
                4 => getArgb(Frac(proportion, alpha), 0, Frac(255, alpha)),
                5 => getArgb(Frac(255, alpha), 0, Frac(255 - proportion, alpha)),
                _ => Color.Empty
            }; // The ARGB hexagon for standard domain coloring
        } // Reference: https://en.wikipedia.org/wiki/Domain_coloring & https://complex-analysis.com/content/domain_coloring.html
        private static Color ObtainColorWheel(Complex c, float alpha = 1)
            => ObtainColorBase(ArgRGB(c.real, c.imaginary), alpha, (int)(255 / (1 + decay * Complex.Modulus(shade ? c : Complex.ZERO))));
        private static Color ObtainColorWheelCurve(float alpha) => ObtainColorBase(alpha * MathF.Tau, 1, 255);
        private static Color ObtainColorStrip(float _value, float min, float max, float alpha = 1) // alpha: brightness
        {
            if (min == max) return Color.Empty; // Necessary
            float beta = (MathF.Atan(_value) - min) / (max - min);
            if (IllegalRatio(alpha) || IllegalRatio(beta)) return Color.Empty; // Necessary
            return beta < 0.5f ? Argb(Frac(Frac(510, beta), alpha), 0, Frac(255, alpha))
                : Argb(Frac(255, alpha), 0, Frac(255 - Frac(510, beta - 0.5f), alpha));
        }
        #endregion

        // 3.INTERACTIONS
        #region Mouse Move & Mouse Down
        private void Graph_MouseMove(object sender, MouseEventArgs e)
        {
            if (!ActivateMoveDown()) return;
            CheckMoveDown(b => RunMouse(e, b, RunMouseMove, () =>
            { Cursor = Cursors.Default; DrawReferenceRectangles(SystemColors.ControlDark); SetScrollBars(false); }));
        }
        private void Graph_MouseDown(object sender, MouseEventArgs e)
        {
            if (!ActivateMoveDown()) return;
            CheckMoveDown(b => RunMouse(e, b, RunMouseDown, null));
        }
        private void RunMouseMove(MouseEventArgs e, int[] borders)
        {
            Cursor = Cursors.Cross;
            Graphics.FromImage(BMP_PIXEL).CopyFromScreen(Cursor.Position, Point.Empty, SIZE_PIXEL);
            DrawReferenceRectangles(BMP_PIXEL.GetPixel(0, 0));
            SetScrollBars(true);
            HandleMouseAction(e, borders, v => { DrawScrollBar(v); DisplayMouseMove(e, v.Item1, v.Item2); });
        }
        private void RunMouseDown(MouseEventArgs e, int[] borders)
        {
            chosen_number++;
            HandleMouseAction(e, borders, v => { DisplayMouseDown(e, v.Item1, v.Item2); });
        }
        //
        private bool ActivateMoveDown() => activate_mouse && !error_input && !is_checking && !NoInput();
        private static void RunMouse(MouseEventArgs e, int[] b, Action<MouseEventArgs, int[]> action, Action? _action)
        { if (e.X > b[0] && e.X < b[1] && e.Y > b[2] && e.Y < b[3]) action(e, b); else _action?.Invoke(); }
        private static void CheckMoveDown(Action<int[]> checkMouse) => checkMouse(GetBorders(is_main ? 1 : 2));
        private static void HandleMouseAction(MouseEventArgs e, int[] borders, Action<(float, float)> actionHandler)
            => actionHandler(LinearTransform(e.X, e.Y, GetRatioRow(borders), GetRatioColumn(borders), borders));
        private void DisplayMouseMove(MouseEventArgs e, float xCoor, float yCoor)
        {
            static string trimForMove(float input) => MyString.TrimLargeNum(input, 1000000);
            SetText(X_CoorDisplay, trimForMove(xCoor)); SetText(Y_CoorDisplay, trimForMove(yCoor));
            SetText(ModulusDisplay, trimForMove(Complex.Modulus(xCoor, yCoor)));
            SetText(AngleDisplay, MyString.GetAngle(xCoor, yCoor));

            if (!MyString.ContainsAny(InputString.Text, MyString.FUNC_NAMES))
            {
                if (is_complex)
                {
                    Complex c = output_complex[e.X - AddOne(borders[0]), e.Y - AddOne(borders[2])];
                    SetText(FunctionDisplay, $"[Re] {c.real}\r\n[Im] {c.imaginary}");
                }
                else SetText(FunctionDisplay, output_real[e.X - AddOne(borders[0]), e.Y - AddOne(borders[2])].ToString());
            }
            else SetText(FunctionDisplay, DISPLAY_ERROR);
        }
        private void DisplayMouseDown(MouseEventArgs e, float xCoor, float yCoor)
        {
            static string trimForDown(float input) => MyString.TrimLargeNum(input, 100);
            string _xCoor = trimForDown(xCoor), _yCoor = trimForDown(yCoor),
                Modulus = trimForDown(Complex.Modulus(xCoor, yCoor)), Angle = MyString.GetAngle(xCoor, yCoor);

            string message = String.Empty;
            if (!MyString.ContainsAny(InputString.Text, MyString.FUNC_NAMES))
            {
                message += "\r\n\r\n";
                var (x, y) = (e.X - AddOne(borders[0]), e.Y - AddOne(borders[2]));
                if (is_complex)
                {
                    Complex c = output_complex[x, y];
                    message += $"Re = {trimForDown(c.real)}\r\nIm = {trimForDown(c.imaginary)}";
                }
                else message += $"f(x, y) = {trimForDown(output_real[x, y])}";
            }
            AddDraft($"\r\n{SEP_1} Point {chosen_number} of No.{loop_number} {SEP_2}\r\n" +
                $"\r\nx = {_xCoor}\r\ny = {_yCoor}\r\n" + $"\r\nmod = {Modulus}\r\narg = {Angle}{message}\r\n");
        }
        #endregion

        #region Graphing Buttons
        private async void ConfirmButton_Click(object sender, EventArgs e) => await Async(() => RunConfirm_Click(sender, e));
        private async void PreviewButton_Click(object sender, EventArgs e) => await Async(() => RunPreview_Click(sender, e));
        private async void AllButton_Click(object sender, EventArgs e) => await Async(() =>
        {
            RunPreview_Click(sender, e);
            if (error_input) return; // To prevent the emergence of the second error box
            Invoke(() => { StopTimers(); Thread.Sleep(SLEEP); StartTimers(); }); // Executed on the UI thread
            RunConfirm_Click(sender, e);
        });
        private void RunConfirm_Click(object sender, EventArgs e) => RunClick(sender, e, GetBorders(1), true, () => Ending(MACRO));
        private void RunPreview_Click(object sender, EventArgs e) => RunClick(sender, e, GetBorders(2), false, () => Ending(MICRO));
        //
        private void RunClick(object sender, EventArgs e, int[] borders, bool isMain, Action endAction)
        {
            try
            {
                Graph_DoubleClick(sender, e);
                SetTextboxButtonReadOnly(true);

                pixel_number = segment_number = export_number = 0;
                error_input = error_address = is_checking = false;
                clicked = true; loop_number++;

                PrepareSetDisplay(borders, isMain);
                endAction();
            }
            catch (Exception) { Invoke(() => InputErrorBox(sender, e, WRONG_FORMAT)); } // Executed on the UI thread
            finally
            {
                if (error_input) StopTimers();
                SetTextboxButtonReadOnly(false); // Necessary to revive the controls after errors
                SetScrollBars(false);
                PictureWait.Visible = false;
            }
        }
        private async Task Async(Action runClick)
        {
            if (NoInput()) return;
            Clipboard.SetText(InputString.Text); // Problematic on JSX's PC
            BlockInput(true); // Cursor.Hide();
            StartTimers();
            await Task.Run(() => { Thread.CurrentThread.Priority = ThreadPriority.Highest; runClick(); });
            BlockInput(false); // Cursor.Show();
        }
        private void StartTimers()
        {
            display_elapsed = 0;
            SetText(TimeDisplay, "0s");
            is_flashing = false; // Ensuring deferred emergence of the hourglass

            DisplayTimer.Start(); WaitTimer.Start(); GraphTimer.Start();
            TimeNow = DateTime.Now;
        }
        private static void StopTimers()
        {
            DisplayTimer.Stop(); WaitTimer.Stop(); GraphTimer.Stop();
            TimeCount = DateTime.Now - TimeNow;
        }
        private void SetTextboxButtonReadOnly(bool readOnly)
        {
            TextBox[] textBoxes = { InputString, GeneralInput, X_Left, X_Right, Y_Left, Y_Right, ThickInput, DenseInput, AddressInput };
            foreach (var tbx in textBoxes) tbx.ReadOnly = readOnly;
            Button[] buttons = { ConfirmButton, PreviewButton, AllButton };
            foreach (var btn in buttons) btn.Enabled = !readOnly;
            activate_mouse = !readOnly;
        }
        private void PrepareSetDisplay(int[] borders, bool isMain)
        {
            (x_left, x_right, y_up, y_down, is_main) = (borders[0], borders[1], borders[2], borders[3], isMain);
            SetThicknessDensenessScopesBorders();
            DisplayOnScreen();
        }
        private void Ending(string mode)
        {
            StopTimers();
            if (is_main) SetText(CaptionBox, $"{InputString.Text}\r\n" + CaptionBox.Text);

            SetText(TimeDisplay, $"{TimeCount:hh\\:mm\\:ss\\.fff}");
            AddDraft($"\r\n{SEP} No.{loop_number} [{mode}] {SEP}\r\n" + $"\r\n{InputString.Text}\r\n" +
                $"\r\nPixels: {PointNumDisplay.Text}\r\nDuration: {TimeDisplay.Text}\r\n");

            if (is_auto && !error_address) RunStore();
            InputString_Focus();
        }
        #endregion

        #region Export & Storage Buttons
        private void ExportButton_Click(object sender, EventArgs e) { Graph_DoubleClick(sender, e); RunExport(); }
        private void StoreButton_Click(object sender, EventArgs e) { Graph_DoubleClick(sender, e); RunStore(); }
        private void RunExport() => HandleExportStore(ExportGraph, REMIND_EXPORT);
        private void RunStore() => HandleExportStore(StoreHistory, REMIND_STORE);
        //
        private void HandleExportStore(Action exportStoreHandler, string prefix)
        {
            try
            {
                FillEmpty(AddressInput, ADDRESS_DEFAULT);
                exportStoreHandler();
                AddDraft($"\r\n{prefix}\r\n{DateTime.Now:HH_mm_ss}\r\n");
            }
            catch (Exception) { error_address = true; Invoke(() => GetExportStoreErrorBox()); } // Executed on the UI thread
        }
        private string GetFileName(string suffix)
        {
            DateTime currentTime = DateTime.Now;
            return $@"{AddressInput.Text}\{currentTime:yyyy}_{currentTime.DayOfYear}_{currentTime:HH_mm_ss}_{suffix}";
        } // The address must be written in a single line
        private void ExportGraph()
        {
            export_number++;
            Graphics.FromImage(bmp_screen).CopyFromScreen(Left + LEFT_SUPP, Top + TOP_SUPP, 0, 0, bmp_screen.Size);
            bmp_screen.Save(GetFileName($"No.{export_number}.png"));
        }
        private void StoreHistory()
        {
            using StreamWriter streamWriter = new(GetFileName($"{STOCKPILE}.txt")); // "using" should not be removed
            streamWriter.Write(DraftBox.Text);
        }
        #endregion

        #region Checking Core & Shortcuts
        private void InputErrorBox(object sender, EventArgs e, string message)
        {
            error_input = true;
            bool temp = ProcessingGraphics();
            InputString.ReadOnly = false; CheckAll(sender, e); InputString.ReadOnly = temp; // Sensitive
            GetInputErrorBox(message);
        }
        private void CheckValidityCore(Action errorHandler)
        {
            try
            {
                is_checking = true;
                PrepareSetDisplay(GetBorders(3), false);

                bool noInput = NoInput(); // Should not return immediately if NoInput()
                InputLabel.ForeColor = noInput ? Color.White : CORRECT_GREEN;
                InputString.BackColor = noInput ? FOCUS_GRAY : CORRECT_GREEN;
                PictureCorrect.Visible = !noInput; PictureIncorrect.Visible = false;
            }
            catch (Exception) { errorHandler(); }
        }
        private void CheckAll(object sender, EventArgs e)
        {
            Action<object, EventArgs>[] checkActions =
            {
                GeneralInput_DoubleClick,
                Details_TextChanged, // Sensitive position
                InputString_DoubleClick,
                ThickInput_DoubleClick,
                DenseInput_DoubleClick,
                AddressInput_DoubleClick
            };
            foreach (var action in checkActions) action(sender, e);
        }
        //
        private void Graph_KeyUp(object sender, KeyEventArgs e)
        {
            HandleModifierKeys(e, false);
            if (suppress_key_up) return; // Should not merge with the next line
            if (HandleSpecialKeys(e)) return;
            HandleCtrlCombination(sender, e);
        }
        private void Graph_KeyDown(object sender, KeyEventArgs e)
        {
            HandleModifierKeys(e, true);
            if (!NoInput() && !ProcessingGraphics() && sft_pressed && e.KeyCode == Keys.Back)
                ExecuteSuppress(() =>
                {
                    AddDraft("\r\nDeleted: " + InputString.Text + "\r\n");
                    SetText(InputString, String.Empty);
                    InputString.Focus();
                }, e);
            else if (e.KeyCode == Keys.Delete) ExecuteSuppress(null, e); // Banning the original deletion
        }

        private static void HandleModifierKeys(KeyEventArgs e, bool isKeyDown)
        {
            if (e.KeyCode == Keys.ControlKey) { ctrl_pressed = isKeyDown; e.Handled = true; }
            else if (e.KeyCode == Keys.ShiftKey) { sft_pressed = isKeyDown; e.Handled = true; }
        }
        private bool HandleSpecialKeys(KeyEventArgs e)
        {
            bool handleReturn(Action<KeyEventArgs> action, bool handled = true) { action(e); return handled; }
            return e.KeyCode switch
            {
                Keys.Escape => handleReturn(e => { ExecuteSuppress(Close, e); }),
                Keys.Oemtilde => handleReturn(e => { ExecuteSuppress(() => PicturePlay_Click(null, e), e); }), // Problematic on other PCs
                Keys.Delete => handleReturn(e => { ExecuteSuppress(() => { Graph_DoubleClick(null, e); Delete_Click(e); }, e); }),
                _ => false
            };
        }
        private void HandleCtrlCombination(object sender, KeyEventArgs e)
        {
            if (!ctrl_pressed) return;
            void restoreDefault(object sender, KeyEventArgs e)
            {
                RecoverInput(); ComboColoring.SelectedIndex = 4; ComboContour.SelectedIndex = 1;

                CheckBox[] checkFalse = { CheckAuto, CheckSwap, CheckPoints, CheckShade, CheckRetain };
                foreach (var cbx in checkFalse) cbx.Checked = false;
                CheckBox[] checkTrue = { CheckEdit, CheckComplex, CheckCoor };
                foreach (var cbx in checkTrue) cbx.Checked = true;
            }
            Action? shortcutHandler = e.KeyCode switch
            {
                Keys.K => () => StoreButton_Click(sender, e),
                Keys.S => () => ExportButton_Click(sender, e),
                Keys.R => () => Graph_DoubleClick(sender, e),
                Keys.D3 => () => ClearButton_Click(sender, e),
                Keys.D2 => () => PictureLogo_DoubleClick(sender, e),
                Keys.OemQuestion => () => TitleLabel_DoubleClick(sender, e),
                Keys.D when !ProcessingGraphics() => () => restoreDefault(sender, e),
                Keys.B when !ProcessingGraphics() => () => AllButton_Click(sender, e),
                Keys.P when !ProcessingGraphics() => () => PreviewButton_Click(sender, e),
                Keys.G when !ProcessingGraphics() => () => ConfirmButton_Click(sender, e),
                Keys.C when !ProcessingGraphics() && sft_pressed => () => CheckAll(sender, e),
                _ => null
            };
            if (shortcutHandler != null) ExecuteSuppress(shortcutHandler, e);
        }
        private static void ExecuteSuppress(Action? action, KeyEventArgs e)
        {
            suppress_key_up = true;
            action?.Invoke();
            e.Handled = e.SuppressKeyPress = true;
            suppress_key_up = false;
        }
        #endregion

        #region Dialogs
        private static void GetMusicClickErrorBox(string message)
            => MessageBox.Show($"Error: Could not find embedded resource for {message}.");
        private static void ShowBoxBase(Action<string, int, int> showMessage, string heading, string[] contents, int feed)
        {
            string content = heading, feeder = String.Concat(Enumerable.Repeat("\r\n", feed));
            for (int i = 0; i < contents.Length; i++) content += $"{feeder}{i + 1}. {contents[i]}";
            showMessage(content + "\r\n", 450, 300);
        }
        private static void ShowErrorBox(string message, string[] contents)
            => ShowBoxBase(MyMessageBox.ShowException, message + MISTAKES_HEAD + "\r\n", contents, 1);
        private static void GetInputErrorBox(string message) => ShowErrorBox(message,
        [
            "Misspelling of function/variable names.",
            "Incorrect grammar of special functions.",
            "Excess or deficiency of characters.",
            "Real/Complex mode confusion.",
            "Invalid other parameters."
        ]);
        private static void GetExportStoreErrorBox() => ShowErrorBox(WRONG_ADDRESS,
        [
            "Files not created beforehand.",
            "The address ending with \\.",
            "The address quoted automatically.",
            "The file storage being full."
        ]);
        //
        private static string GetComment(string input) => $"# {input}";
        private static string GetManual()
        {
            string content = $"DESIGNER: Fraljimetry\r\nDATE: {DATE}\r\nLOCATION: Xi'an, China";
            content += "\r\n\r\nThis software was developed in Visual Studio 2022, written in C#, " +
                "to visualize real/complex functions and equations with no more than two variables." +
                "\r\n\r\nTo bolster artistry and practicality, numerous modes are rendered, " +
                "making it possible to generate images tailored for users of various ends." +
                "\r\n\r\nNote: I wish the definitions of these operations are self-evident if you try yourself or refer to the examples.";

            static string subTitleContent(string subtitle, string content) => $"\r\n\r\n{_SEP} {subtitle} {_SEP}" + content;
            content += subTitleContent("ELEMENTS",
                "\r\n\r\n+ - * / ^ ( )" +
                "\r\n\r\nSin, Cos, Tan, Sinh, Cosh, Tanh," +
                "\r\nArcsin & Asin, Arccos & Acos, Arctan & Atan," +
                "\r\nArsinh & Asinh, Arcosh & Acosh, Artanh & Atanh," +
                "\r\n\r\nLog & Ln, Exp, Sqrt, Abs (f(x,y) & f(z))" +
                $"\r\n\r\nConjugate & Conj (f(z)), e(f(z)){TAB}{GetComment("e(z) := exp (2*pi*i*z).")}");
            content += subTitleContent("COMBINATORICS",
                "\r\n\r\nFloor, Ceil, Round, Sign & Sgn (float a)" +
                "\r\n\r\nMod (float a, float n), nCr, nPr (int n, int r)" +
                "\r\n\r\nMax, Min (float a, float b, ...), Factorial & Fact (int n)");
            content += subTitleContent("SPECIALTIES",
                $"\r\n\r\n{GetComment("F&C := float & Complex.")}" +
                "\r\n\r\nF (F&C a, F&C b, F&C c, f(x,y) & f(z)) & " +
                "\r\nF (F&C a, F&C b, F&C c, f(x,y) & f(z), int n)" +
                $"\r\n{GetComment("HyperGeometric Series (case-sensitive).")}" +
                "\r\n\r\nGamma & Ga (f(x,y) & f(z)) & " +
                "\r\nGamma & Ga (f(x,y) & f(z), int n)" +
                "\r\n\r\nBeta (f(x,y) & f(z), g(x,y) & g(z)) & " +
                "\r\nBeta (f(x,y) & f(z), g(x,y) & g(z), int n)" +
                "\r\n\r\nZeta (f(x,y) & f(z)) & " +
                $"\r\nZeta (f(x,y) & f(z), int n){TAB}{GetComment("This is a mess for n too large.")}");
            content += subTitleContent("REPETITIONS",
                $"\r\n\r\n{GetComment("Capitalizations represent substitutions of variables.")}" +
                "\r\n\r\nSum (f(x,y,k) & f(z,k), k, int a, int b)" +
                "\r\nProduct & Prod (f(x,y,k) & f(z,k), k, int a, int b)" +
                "\r\n\r\nIterate1 (f(x,y,X,k), g(x,y), k, int a, int b)" +
                "\r\nIterate2 (f1(x,y,X,Y,k), f2(...), g1(x,y), g2(...), k, int a, int b, 1&2)" +
                "\r\nIterate (f(z,Z,k), g(z), k, int a, int b)" +
                $"\r\n{GetComment("g: initial values; f: iterations.")}" +
                "\r\n\r\nComposite1 & Comp1(f(x,y), g1(x,y,X), ... , gn(x,y,X))" +
                $"\r\nComposite2 & Comp2\r\n{TAB}(f1(x,y), f2(...), g1(x,y,X,Y), h1(...), ... , gn(...), hn(...), 1&2)" +
                "\r\nComposite & Comp (f(z), g1(z,Z), ... , gn(z,Z))" +
                $"\r\n{GetComment("f: initial values; g: compositions.")}" +
                $"\r\n\r\nCocoon & Coc\r\n{TAB}" + "(f(z,{0},...,{n})&f(x,y,...), g1(z)&g1(x,y), ... , gn(z)&gn(x,y))" +
                $"\r\n{GetComment("f: body; {*}: the *-th tag; g: values of tags.")}");
            content += subTitleContent("PLANAR CURVES",
                "\r\n\r\nFunc (f(x)) & " +
                "\r\nFunc (f(x), float increment) & " +
                "\r\nFunc (f(x), float a, float b) & " +
                "\r\nFunc (f(x), float a, float b, float increment)" +
                "\r\n\r\nPolar (f(θ), θ, float a, float b) & " +
                "\r\nPolar (f(θ), θ, float a, float b, float increment)" +
                "\r\n\r\nParam (f(u), g(u), u, float a, float b) & " +
                "\r\nParam (f(u), g(u), u, float a, float b, float increment)");
            content += subTitleContent("RECURSIONS",
                $"\r\n\r\n{GetComment("These methods should be combined with all above.")}" +
                "\r\n\r\nLoop (Input(k), k, int a, int b)" +
                "\r\n\r\nIterateLoop (f(x,y,X,k), g(x,y), k, int a, int b) & " +
                "\r\nIterateLoop (f(x,y,X,k), g(x,y), k, int a, int b, h(x,y,X,k))" +
                "\r\n\r\nIterateLoop (f(z,Z,k), g(z), k, int a, int b) & " +
                "\r\nIterateLoop (f(z,Z,k), g(z), k, int a, int b, h(z,Z,k))" +
                $"\r\n\r\n{GetComment("Displaying each loop of iteration.")}" +
                $"\r\n\r\n... | ... | ...{TAB}{GetComment("Displaying one by one.")}");
            content += subTitleContent("CONSTANTS", "\r\n\r\npi, e, gamma & ga, i");
            content += subTitleContent("SHORTCUTS", "\r\n");

            static string getShortcuts(string key, int blank, string meaning) => $"\r\n[{key}]" + new string('\t', blank) + meaning + ";";
            content += getShortcuts("Control + P", 2, "Graph in the MicroBox");
            content += getShortcuts("Control + G", 2, "Graph in the MacroBox");
            content += getShortcuts("Control + B", 2, "Graph in both regions");
            content += getShortcuts("Control + S", 2, "Save as a snapshot");
            content += getShortcuts("Control + K", 2, "Save the history as a .txt");
            content += getShortcuts("Control + Shift + C", 1, "Check all inputs");
            content += getShortcuts("Control + R", 2, "Erase all checks");
            content += getShortcuts("Control + D", 2, "Restore to default");
            content += getShortcuts("Shift + Back", 2, "Clear the InputBox");
            content += getShortcuts("Control + D2", 2, "View Fralji's profile");
            content += getShortcuts("Control + D3", 2, "Clear all ReadOnly controls");
            content += getShortcuts("Control + OemQuestion", 1, "See the manual");
            content += getShortcuts("Oemtilde", 2, "Play/pause the music");
            content += getShortcuts("Delete", 3, "Clear both regions");
            content += getShortcuts("Escape", 3, "Close Fraljiculator");
            return content + "\r\n\r\nClick [Tab] to witness the process of control design.";
        }
        private static string AddContact(string platform, string account, string note)
            => $"\r\n\r\n{platform}: {account}" + (note != String.Empty ? (new string(' ', 4) + GetComment(note)) : note);
        private static string GetProfile()
        {
            string content = "Dear math lovers & mathematicians:" +
                "\r\n\r\nHi! I'm Fralji, a content creator on Bilibili since July, 2021, when I was a pre-freshman before entering college." +
                "\r\n\r\nI aim to deliver unique lectures on many branches of mathematics. " +
                "If you have any problem on the usage of this application, or anything concerning math, please reach to me via:";
            content += AddContact("Bilibili", "355884223", String.Empty);
            content += AddContact("Email", "frankjiiiiiiii@gmail.com", String.Empty);
            content += AddContact("Wechat", "F1r4a2n8k5y7", "recommended");
            content += AddContact("QQ", "472955101", String.Empty);
            content += AddContact("Facebook", "Fraljimetry", String.Empty);
            content += AddContact("Instagram", "shaodaji", "NOT recommended");
            return content + "\r\n\r\n" + new string(' ', 72) + $"{DATE}";
        }
        private void TitleLabel_DoubleClick(object sender, EventArgs e) => MyMessageBox.ShowFormal(GetManual(), 640, 480);
        private void PictureLogo_DoubleClick(object sender, EventArgs e) => MyMessageBox.ShowFormal(GetProfile(), 600, 450);
        private static void ShowCustomBox(string title, string[] contents)
            => ShowBoxBase(MyMessageBox.ShowCustom, $"[{title}]" + new string(' ', 12) + $"{DATE}", contents, 2);
        //
        private void InputLabel_DoubleClick(object sender, EventArgs e) => ShowCustomBox("FORMULA INPUT",
        [
            "Space and Enter keys are both acceptable. Unsupported keys are blocked, and removed if pasted from the clipboard.",
            "Excessive omission of multiplication may cause misinterpretation. For example, \"gammax\" will be parsed as \"max\"."
        ]);
        private void AtLabel_DoubleClick(object sender, EventArgs e) => ShowCustomBox("SAVING ADDRESS",
        [
            "Create a file for snapshot storage and paste its address here. It will be validated.",
            "PNG snapshots and history lists will be named in the following formats: " +
            "\"yyyy_ddd_hh_mm_ss_No.#\" and \"yyyy_ddd_hh_mm_ss_stockpile\"."
        ]);
        private void GeneralLabel_DoubleClick(object sender, EventArgs e) => ShowCustomBox("GENERAL SCOPE",
        [
            "The detailed scope takes effect only when the general scope is set to \"0\".",
            "Any legitimate variable-free algebraic expression is acceptable, checked in the same way as in the input box."
        ]);
        private void DetailLabel_DoubleClick(object sender, EventArgs e) => ShowCustomBox("DETAILED SCOPE",
        [
            "Creating a mirror effect by reversing endpoints is NOT supported.",
            "Any legitimate variable-free algebraic expression is acceptable, checked in the same way as in the input box."
        ]);
        private void ThickLabel_DoubleClick(object sender, EventArgs e) => ShowCustomBox("MAGNITUDE",
        [
            "Represents: (i) Width of planar curves, (ii) Size of special points, (iii) Decay rates of translucence.",
            "It should be chosen appropriately according to the scale. The examples have been carefully tuned."
        ]);
        private void DenseLabel_DoubleClick(object sender, EventArgs e) => ShowCustomBox("DENSITY",
        [
            "Represents:\r\n(i) Density of contours (real and complex),\r\n(ii) Relative speed of planar curves.",
            "It should be chosen appropriately according to the scale. The examples have been carefully tuned."
        ]);
        private void DraftLabel_DoubleClick(object sender, EventArgs e) => ShowCustomBox("HISTORY LIST",
        [
            "The input will be saved both in this box and to the clipboard.",
            "Clicked points, along with the timestamps of snapshots and history entries, will also be recorded in detail."
        ]);

        private void ExampleLabel_DoubleClick(object sender, EventArgs e) => ShowCustomBox("EXAMPLES",
        [
            "These examples illustrate the wide variety of supported input formats.",
            "Some renderings are elegant while others are chaotic. Elegance takes time to explore and appreciate. Enjoy!"
        ]);
        private void FunctionLabel_DoubleClick(object sender, EventArgs e) => ShowCustomBox("FUNCTIONS",
        [
            "The two combo boxes contain regular and special operations respectively, the latter having more complex grammar.",
            "Select something in the input box and choose an item here to substitute your selection."
        ]);
        private void ModeLabel_DoubleClick(object sender, EventArgs e) => ShowCustomBox("COLORING MODES",
        [
            "The color spectrum represents:\r\n(i) Arguments of meromorphic functions," +
            "\r\n(ii) Values of two-variable functions,\r\n(iii) Parameterizations of planar curves.",
            "The first three modes support swappable color schemes, while the last two do not."
        ]);
        private void ContourLabel_DoubleClick(object sender, EventArgs e) => ShowCustomBox("CONTOUR MODES",
        [
            "Both options apply ONLY to the complex version, for contouring meromorphic functions.",
            "Only the Polar option supports translucent display, representing the decay rate of the modulus."
        ]);

        private void PointNumLabel_DoubleClick(object sender, EventArgs e) => ShowCustomBox("PIXELS",
        [
            "Logs the number of points or line segments in the previous loop, roughly proportional to time and iteration count.",
            "A value of zero often results from constancy, divergence, or undefined behavior."
        ]);
        private void TimeLabel_DoubleClick(object sender, EventArgs e) => ShowCustomBox("DURATION",
        [
            "The auto-snapshot cannot capture updates here in real time, but the value will be saved in the history list.",
            "This value is a valuable indicator of optimization, referenced for choosing appropriate iterations and other settings."
        ]);
        private void PreviewLabel_DoubleClick(object sender, EventArgs e) => ShowCustomBox("MICROCOSM",
        [
            "Since graphing cannot be paused manually during execution, a preview is necessary for estimating computation time.",
            "It differs from the main graph only in sharpness. Graphing here is roughly 20 times faster (less after optimization)."
        ]);
        #endregion

        #region Index Change & Check Change
        private void SetValuesForSelectedIndex(int index)
        {
            int _color = 3; string _general = "1.1", _thick = THICK_DEFAULT, _dense = DENSE_DEFAULT;
            bool _points = false, _retain = false, _shade = false, _axes = true;
            int complexL = ReplaceTags.EX_COMPLEX.Length, realL = ReplaceTags.EX_REAL.Length,
                curveL = ReplaceTags.EX_CURVES.Length;

            InputString.ReadOnly = true; // Necessary
            void setDetails(string xLeft, string xRight, string yLeft, string yRight)
            { SetText(X_Left, xLeft); SetText(X_Right, xRight); SetText(Y_Left, yLeft); SetText(Y_Right, yRight); }
            if (index < complexL)
                switch (index)
                {
                    case 0: _general = "2"; _color = 4; _points = true; break;
                    case 1: _general = "1.2"; break;
                    case 2: _color = 2; _points = true; break;
                    case 3: _general = "pi/2"; _color = 4; break;
                    case 4: _general = "4"; _thick = "0.1"; _shade = true; break;
                    case 5: _general = "3"; break;
                    case 6: _general = "0"; setDetails("-1.6", "0.6", "-1.1", "1.1"); _axes = false; break;
                    case 7: _general = "2"; _color = 0; _shade = true; break;
                }
            else if (index > complexL && index < complexL + realL + 1)
                switch (index - complexL - 1)
                {
                    case 0: _general = "10"; _color = 3; break;
                    case 1: _general = "2pi"; _color = 4; break;
                    case 2: _general = "4"; _color = 2; break;
                    case 3: _general = "0"; setDetails("0", "1", "0", "1"); _thick = "0.2"; _color = 0; _retain = true; break;
                    case 4: _general = "0"; setDetails("-10", "0", "0", "10"); _thick = "0.1"; _color = 1; _points = true; break;
                    case 5: _general = "5"; _color = 1; break;
                    case 6: _general = "3"; break;
                    case 7: _general = "3"; _thick = "0.05"; _color = 2; _points = true; break;
                }
            else if (index > complexL + realL + 1 && index < complexL + realL + curveL + 2)
                switch (index - complexL - realL - 2)
                {
                    case 0: _general = "5"; break;
                    case 1: _general = "pi"; _thick = "0.5"; _dense = "2"; _color = 2; break;
                    case 2: _general = "3"; _color = 0; break;
                    case 3: _dense = "100"; _color = 1; break;
                    case 4: _thick = "0.5"; break;
                    case 5: _thick = "0.5"; _dense = "10"; _retain = true; break;
                    case 6: _thick = "0.5"; break;
                    case 7: _general = "0"; setDetails("-0.2", "1.2", "-0.2", "1.2"); _thick = "0.5"; _color = 0; _retain = true; break;
                }
            else ComboExamples_Undo();
            InputString.ReadOnly = false; // Necessary

            SetText(GeneralInput, _general); SetText(ThickInput, _thick); SetText(DenseInput, _dense);
            CheckPoints.Checked = _points; CheckRetain.Checked = _retain; CheckShade.Checked = _shade; CheckCoor.Checked = _axes;
            CheckComplex.Checked = ComboExamples.SelectedIndex < complexL;
            ComboColoring.SelectedIndex = _color;
        }
        private void Combo_SelectionChanged(string selectedItem)
        {
            if (ProcessingGraphics()) return;
            SetText(InputString, MyString.Replace(InputString.Text, selectedItem, InputString.SelectionStart,
                InputString.SelectionStart + InputString.SelectionLength - 1));
            InputString.Focus(); InputString.SelectionStart--; // Should not place before .Focus()
        }

        private void ComboExamples_SelectedIndexChanged(object sender, EventArgs e)
        {
            string? selection = ComboExamples.SelectedItem?.ToString();
            if (ProcessingGraphics() || String.IsNullOrEmpty(selection) || ComboExamples.SelectedIndex == -1) return;
            SetText(InputString, selection);
            SetValuesForSelectedIndex(ComboExamples.SelectedIndex);
            ComboExamples_Undo(); // To prevent repetitive call
            Delete_Click(e);
            InputString_Focus();
        }
        private void ComboFunctions_SelectedIndexChanged(object sender, EventArgs e)
            => Combo_SelectionChanged(ComboFunctions.SelectedItem.ToString());
        private void ComboSpecial_SelectedIndexChanged(object sender, EventArgs e)
            => Combo_SelectionChanged(ComboSpecial.SelectedItem.ToString());
        private void ComboColoring_SelectedIndexChanged(object sender, EventArgs e)
            => color_mode = AddOne(ComboColoring.SelectedIndex);
        private void ComboContour_SelectedIndexChanged(object sender, EventArgs e)
            => contour_mode = AddOne(ComboContour.SelectedIndex);
        //
        private void CheckComplex_CheckedChanged(object sender, EventArgs e) => ReverseBool(ref is_complex);
        private void CheckSwap_CheckedChanged(object sender, EventArgs e) => ReverseBool(ref swap_colors);
        private void CheckCoor_CheckedChanged(object sender, EventArgs e) => ReverseBool(ref delete_coor);
        private void CheckPoints_CheckedChanged(object sender, EventArgs e) => ReverseBool(ref delete_point);
        private void CheckShade_CheckedChanged(object sender, EventArgs e) => ReverseBool(ref shade);
        private void CheckRetain_CheckedChanged(object sender, EventArgs e) => ReverseBool(ref freeze_graph);
        private void CheckAuto_CheckedChanged(object sender, EventArgs e) => ReverseBool(ref is_auto);
        private void CheckEdit_CheckedChanged(object sender, EventArgs e)
        {
            DraftBox.ReadOnly = !DraftBox.ReadOnly; // Cannnot pass properties as reference
            DraftBox.BackColor = DraftBox.ReadOnly ? Color.Black : SystemColors.ControlDarkDark;
            DraftBox.ForeColor = DraftBox.ReadOnly ? READONLY_GRAY : Color.White;
            DraftBox.ScrollBars = DraftBox.ReadOnly ? ScrollBars.None : ScrollBars.Vertical;
        }
        #endregion

        // 4.SPECIAL EFFECTS
        #region Click & Mouse Down & Text Changed
        private void Delete_Click(int[] borders, bool isMain)
        {
            Details_TextChanged(null, EventArgs.Empty); // So that axes and grids are drawn correctly
            ClearBitmap(GetBitmap(isMain));
            Invalidate(isMain ? rect_mac : rect_mic); Update(); // To clear the overflowed curves
            DrawBackdropAxesGrids(borders, isMain);
        } // Sensitive
        private void Delete_Click(EventArgs e) { DeleteMain_Click(this, e); DeletePreview_Click(this, e); }
        private void DeleteMain_Click(object sender, EventArgs e) => Delete_Click(GetBorders(1), true);
        private void DeletePreview_Click(object sender, EventArgs e) => Delete_Click(GetBorders(2), false);
        private void ClearButton_Click(object sender, EventArgs e)
        {
            loop_number = chosen_number = 0;
            TextBox[] textBoxes = { DraftBox, PointNumDisplay, TimeDisplay, X_CoorDisplay, Y_CoorDisplay,
                ModulusDisplay, AngleDisplay, FunctionDisplay, CaptionBox };
            foreach (var tbx in textBoxes) SetText(tbx, String.Empty);
            InputString_Focus();
        }
        private void PicturePlay_Click(object sender, EventArgs e)
        {
            if (!music_sound) return;
            if (is_paused)
            {
                MediaPlayer.controls.currentPosition = pause_pos;
                MediaPlayer.controls.play();
                ColorTimer.Start();
            }
            else
            {
                pause_pos = (float)MediaPlayer.controls.currentPosition; // Conversion from double to float
                MediaPlayer.controls.pause();
                ColorTimer.Stop();
                TitleLabel.ForeColor = Color.White;
            }
            ReverseBool(ref is_paused);
        }
        private void PictureIncorrect_Click(object sender, EventArgs e)
        { if (!ProcessingGraphics()) CheckValidityCore(() => InputErrorBox(sender, e, WRONG_FORMAT)); }
        //
        private void PointNumDisplay_MouseDown(object sender, MouseEventArgs e) => HideCaret(PointNumDisplay.Handle);
        private void TimeDisplay_MouseDown(object sender, MouseEventArgs e) => HideCaret(TimeDisplay.Handle);
        private void X_CoorDisplay_MouseDown(object sender, MouseEventArgs e) => HideCaret(X_CoorDisplay.Handle);
        private void Y_CoorDisplay_MouseDown(object sender, MouseEventArgs e) => HideCaret(Y_CoorDisplay.Handle);
        private void ModulusDisplay_MouseDown(object sender, MouseEventArgs e) => HideCaret(ModulusDisplay.Handle);
        private void AngleDisplay_MouseDown(object sender, MouseEventArgs e) => HideCaret(AngleDisplay.Handle);
        private void FunctionDisplay_MouseDown(object sender, MouseEventArgs e) => HideCaret(FunctionDisplay.Handle);
        private void CaptionBox_MouseDown(object sender, MouseEventArgs e) => HideCaret(CaptionBox.Handle);
        private void DraftBox_MouseDown(object sender, MouseEventArgs e) { if (DraftBox.ReadOnly) HideCaret(DraftBox.Handle); }
        //
        private void Graph_DoubleClick(object sender, EventArgs e)
        {
            InputString.BackColor = FOCUS_GRAY;
            Label[] labels = { InputLabel, AtLabel, GeneralLabel, DetailLabel, X_Scope, Y_Scope, ThickLabel, DenseLabel,
                ExampleLabel, FunctionLabel, ModeLabel, ContourLabel };
            foreach (var lbl in labels) lbl.ForeColor = Color.White;
            PictureIncorrect.Visible = PictureCorrect.Visible = is_checking = false;
        }
        private void SubtitleBox_DoubleClick(object sender, EventArgs e)
        {
            DrawBackdrop(GetBorders(1)); DrawBackdrop(GetBorders(2));
            SetAxesDrawn(true); SetAxesDrawn(false);
            DrawReferenceRectangles(SystemColors.ControlDark);
        }
        private void InputString_DoubleClick(object sender, EventArgs e) => InputString_TextChanged(sender, e);
        private void AddressInput_DoubleClick(object sender, EventArgs e) => AddressInput_TextChanged(sender, e);
        private void GeneralInput_DoubleClick(object sender, EventArgs e) => GeneralInput_TextChanged(sender, e);
        private void X_Left_DoubleClick(object sender, EventArgs e) => X_Left_TextChanged(sender, e);
        private void X_Right_DoubleClick(object sender, EventArgs e) => X_Right_TextChanged(sender, e);
        private void Y_Left_DoubleClick(object sender, EventArgs e) => Y_Left_TextChanged(sender, e);
        private void Y_Right_DoubleClick(object sender, EventArgs e) => Y_Right_TextChanged(sender, e);
        private void ThickInput_DoubleClick(object sender, EventArgs e) => ThickInput_TextChanged(sender, e);
        private void DenseInput_DoubleClick(object sender, EventArgs e) => DenseInput_TextChanged(sender, e);
        //
        private void MiniChecks(TextBox[] textBoxes, Label lbl)
        {
            try
            {
                if (ProcessingGraphics()) return; bool noSomeInput = false;
                foreach (var tbx in textBoxes)
                {
                    bool noInput = String.IsNullOrEmpty(tbx.Text); noSomeInput = noSomeInput || noInput;
                    if (!noInput) _ = Obtain(tbx); // For checking
                }
                lbl.ForeColor = noSomeInput ? Color.White : CORRECT_GREEN; // White if any being null or empty
            }
            catch (Exception) { lbl.ForeColor = ERROR_RED; }
        }
        private void MiniChecks(TextBox tbx, Label lbl) => MiniChecks([tbx], lbl);
        private void Details_TextChanged(object sender, EventArgs e)
        {
            if (ProcessingGraphics()) return;
            MiniChecks([X_Left, X_Right, Y_Left, Y_Right], DetailLabel);
            if (scopes == null) return; // Necessary for the initialization

            void checkScopes(bool b1, bool b2, Color c) { if (b1) X_Scope.ForeColor = c; if (b2) Y_Scope.ForeColor = c; }
            try { SetThicknessDensenessScopesBorders(false); }
            catch (Exception) { checkScopes(InvalidScopesX(), InvalidScopesY(), ERROR_RED); }
            finally { checkScopes(!InvalidScopesX(), !InvalidScopesY(), CORRECT_GREEN); } // Should not declare the bools ahead
        } // Sensitive

        private void X_Left_TextChanged(object sender, EventArgs e) => Details_TextChanged(sender, e);
        private void X_Right_TextChanged(object sender, EventArgs e) => Details_TextChanged(sender, e);
        private void Y_Left_TextChanged(object sender, EventArgs e) => Details_TextChanged(sender, e);
        private void Y_Right_TextChanged(object sender, EventArgs e) => Details_TextChanged(sender, e);
        private void GeneralInput_TextChanged(object sender, EventArgs e) => MiniChecks(GeneralInput, GeneralLabel);
        private void ThickInput_TextChanged(object sender, EventArgs e) => MiniChecks(ThickInput, ThickLabel);
        private void DenseInput_TextChanged(object sender, EventArgs e) => MiniChecks(DenseInput, DenseLabel);
        private void InputString_TextChanged(object sender, EventArgs e)
        {
            if (ProcessingGraphics()) return;
            static int removeSomeKeys(TextBox tbx)
            {
                int caretPosition = tbx.Text.Length - tbx.SelectionStart - tbx.SelectionLength; // Necessary
                foreach (char c in RecoverMultiply.BARRED_CHARS) SetText(tbx, tbx.Text.Replace(c, ' '));
                return tbx.Text.Length - caretPosition;
            }
            int pos = removeSomeKeys(InputString); // Necessary
            CheckComplex.Checked = MyString.ContainsAny(MyString.ReplaceConfusion(InputString.Text), RecoverMultiply._ZZ_);
            CheckValidityCore(() =>
            {
                InputString.BackColor = InputLabel.ForeColor = ERROR_RED;
                PictureIncorrect.Visible = true; PictureCorrect.Visible = false;
            });
            InputString.SelectionStart = pos;
        }
        private void AddressInput_TextChanged(object sender, EventArgs e)
        {
            if (ProcessingGraphics()) return;
            if (String.IsNullOrEmpty(AddressInput.Text)) AtLabel.ForeColor = Color.White;
            else AtLabel.ForeColor = Directory.Exists(AddressInput.Text) ? CORRECT_GREEN : ERROR_RED;
        }
        //
        private static void BanDoubleClick(TextBox tbx, MouseEventArgs e)
        { tbx.SelectionStart = tbx.GetCharIndexFromPosition(e.Location); tbx.SelectionLength = 0; } // To ban the default selection
        private void InputString_MouseDoubleClick(object sender, MouseEventArgs e) => BanDoubleClick(InputString, e);
        private void AddressInput_MouseDoubleClick(object sender, MouseEventArgs e) => BanDoubleClick(AddressInput, e);
        private void GeneralInput_MouseDoubleClick(object sender, MouseEventArgs e) => BanDoubleClick(GeneralInput, e);
        private void X_Left_MouseDoubleClick(object sender, MouseEventArgs e) => BanDoubleClick(X_Left, e);
        private void X_Right_MouseDoubleClick(object sender, MouseEventArgs e) => BanDoubleClick(X_Right, e);
        private void Y_Left_MouseDoubleClick(object sender, MouseEventArgs e) => BanDoubleClick(Y_Left, e);
        private void Y_Right_MouseDoubleClick(object sender, MouseEventArgs e) => BanDoubleClick(Y_Right, e);
        private void ThickInput_MouseDoubleClick(object sender, MouseEventArgs e) => BanDoubleClick(ThickInput, e);
        private void DenseInput_MouseDoubleClick(object sender, MouseEventArgs e) => BanDoubleClick(DenseInput, e);
        #endregion

        #region Key Press & Key Down
        private void BarSomeKeys(object sender, KeyPressEventArgs e)
        { if (RecoverMultiply.BARRED_CHARS.Contains(e.KeyChar)) e.Handled = true; }
        private void InputString_KeyPress(object sender, KeyPressEventArgs e) => BarSomeKeys(sender, e);
        private void GeneralInput_KeyPress(object sender, KeyPressEventArgs e) => BarSomeKeys(sender, e);
        private void X_Left_KeyPress(object sender, KeyPressEventArgs e) => BarSomeKeys(sender, e);
        private void X_Right_KeyPress(object sender, KeyPressEventArgs e) => BarSomeKeys(sender, e);
        private void Y_Left_KeyPress(object sender, KeyPressEventArgs e) => BarSomeKeys(sender, e);
        private void Y_Right_KeyPress(object sender, KeyPressEventArgs e) => BarSomeKeys(sender, e);
        private void ThickInput_KeyPress(object sender, KeyPressEventArgs e) => BarSomeKeys(sender, e);
        private void DenseInput_KeyPress(object sender, KeyPressEventArgs e) => BarSomeKeys(sender, e);
        //
        private static void AutoKeyDown(TextBox tbx, KeyEventArgs e)
        {
            if (tbx.ReadOnly) return; int caretPosition = tbx.SelectionStart; // Necessary
            void selectSuppress(int pos) { tbx.SelectionStart = caretPosition + pos; e.SuppressKeyPress = true; }
            void insertSelectSuppress(string insertion, int pos)
            { SetText(tbx, tbx.Text.Insert(caretPosition, insertion)); selectSuppress(pos); }

            if (!MyString.CheckParenthesis(tbx.Text.AsSpan(caretPosition, tbx.SelectionLength))) selectSuppress(0);
            else if (e.KeyCode == Keys.D9 && (ModifierKeys & Keys.Shift) != 0)
            {
                if (tbx.SelectionLength == 0) insertSelectSuppress(RecoverMultiply.LR_BRA, 1);
                else
                {
                    string selectedText = tbx.Text.Substring(caretPosition, tbx.SelectionLength);
                    SetText(tbx, tbx.Text.Remove(caretPosition, tbx.SelectionLength));
                    insertSelectSuppress("(" + selectedText + ")", selectedText.Length + 2);
                }
            } // '{' does not share this privilege
            else if (e.KeyCode == Keys.D0 && (ModifierKeys & Keys.Shift) != 0)
            {
                if (tbx.SelectionLength > 0) selectSuppress(0);
                else if (caretPosition == 0) selectSuppress(0);
                else if (tbx.Text[caretPosition - 1] == '(') selectSuppress(1);
            } // '}' does not share this privilege
            else if (e.KeyCode == Keys.Oemcomma) insertSelectSuppress(", ", 2);
            else if (e.KeyCode == Keys.OemPipe) insertSelectSuppress(" | ", 3);
            else if (e.KeyCode == Keys.Back)
            {
                if (caretPosition == 0 || !MyString.CheckParenthesis(tbx.Text) || tbx.SelectionLength > 0) return;
                else if (tbx.Text[caretPosition - 1] == '(')
                {
                    if (tbx.Text[caretPosition] == ')') SetText(tbx, tbx.Text.Remove(caretPosition - 1, 2));
                    selectSuppress(-1);
                }
                else if (tbx.Text[caretPosition - 1] == ')') selectSuppress(-1);
            }
        } // Sensitive
        private void InputString_KeyDown(object sender, KeyEventArgs e) => AutoKeyDown(InputString, e);
        private void GeneralInput_KeyDown(object sender, KeyEventArgs e) => AutoKeyDown(GeneralInput, e);
        private void X_Left_KeyDown(object sender, KeyEventArgs e) => AutoKeyDown(X_Left, e);
        private void X_Right_KeyDown(object sender, KeyEventArgs e) => AutoKeyDown(X_Right, e);
        private void Y_Left_KeyDown(object sender, KeyEventArgs e) => AutoKeyDown(Y_Left, e);
        private void Y_Right_KeyDown(object sender, KeyEventArgs e) => AutoKeyDown(Y_Right, e);
        private void ThickInput_KeyDown(object sender, KeyEventArgs e) => AutoKeyDown(ThickInput, e);
        private void DenseInput_KeyDown(object sender, KeyEventArgs e) => AutoKeyDown(DenseInput, e);

        private static void Combo_KeyDown(KeyEventArgs e) // To ban the default keyboard search
            => e.SuppressKeyPress = e.KeyCode >= Keys.A && e.KeyCode <= Keys.Z;
        private void ComboExamples_KeyDown(object sender, KeyEventArgs e) => Combo_KeyDown(e);
        private void ComboFunctions_KeyDown(object sender, KeyEventArgs e) => Combo_KeyDown(e);
        private void ComboSpecial_KeyDown(object sender, KeyEventArgs e) => Combo_KeyDown(e);
        private void ComboColoring_KeyDown(object sender, KeyEventArgs e) => Combo_KeyDown(e);
        private void ComboContour_KeyDown(object sender, KeyEventArgs e) => Combo_KeyDown(e);
        #endregion

        #region Mouse Hover & Mouse Leave
        private static void SetFont(Label lbl) => lbl.ForeColor = lbl.ForeColor == Color.White ? UNCHECK_YELLOW : lbl.ForeColor;
        private static void RecoverFont(Label lbl) => lbl.ForeColor = lbl.ForeColor == UNCHECK_YELLOW ? Color.White : lbl.ForeColor;
        private static void HoverEffect(TextBox tbx, Label lbl)
        { tbx.BackColor = lbl.ForeColor == Color.White ? FOCUS_GRAY : lbl.ForeColor; tbx.ForeColor = Color.Black; SetFont(lbl); }
        private static void LeaveEffect(TextBox tbx, Label lbl)
        { tbx.BackColor = CONTROL_GRAY; tbx.ForeColor = Color.White; RecoverFont(lbl); }

        private void InputString_MouseHover(object sender, EventArgs e) => SetFont(InputLabel);
        private void InputString_MouseLeave(object sender, EventArgs e) => RecoverFont(InputLabel);
        private void AddressInput_MouseHover(object sender, EventArgs e) => HoverEffect(AddressInput, AtLabel);
        private void AddressInput_MouseLeave(object sender, EventArgs e) => LeaveEffect(AddressInput, AtLabel);
        private void GeneralInput_MouseHover(object sender, EventArgs e) => HoverEffect(GeneralInput, GeneralLabel);
        private void GeneralInput_MouseLeave(object sender, EventArgs e) => LeaveEffect(GeneralInput, GeneralLabel);
        private void X_Left_MouseHover(object sender, EventArgs e) => HoverEffect(X_Left, DetailLabel);
        private void X_Left_MouseLeave(object sender, EventArgs e) => LeaveEffect(X_Left, DetailLabel);
        private void X_Right_MouseHover(object sender, EventArgs e) => HoverEffect(X_Right, DetailLabel);
        private void X_Right_MouseLeave(object sender, EventArgs e) => LeaveEffect(X_Right, DetailLabel);
        private void Y_Left_MouseHover(object sender, EventArgs e) => HoverEffect(Y_Left, DetailLabel);
        private void Y_Left_MouseLeave(object sender, EventArgs e) => LeaveEffect(Y_Left, DetailLabel);
        private void Y_Right_MouseHover(object sender, EventArgs e) => HoverEffect(Y_Right, DetailLabel);
        private void Y_Right_MouseLeave(object sender, EventArgs e) => LeaveEffect(Y_Right, DetailLabel);
        private void ThickInput_MouseHover(object sender, EventArgs e) => HoverEffect(ThickInput, ThickLabel);
        private void ThickInput_MouseLeave(object sender, EventArgs e) => LeaveEffect(ThickInput, ThickLabel);
        private void DenseInput_MouseHover(object sender, EventArgs e) => HoverEffect(DenseInput, DenseLabel);
        private void DenseInput_MouseLeave(object sender, EventArgs e) => LeaveEffect(DenseInput, DenseLabel);
        private void DraftBox_MouseHover(object sender, EventArgs e)
        {
            if (DraftBox.ReadOnly)
            {
                DraftLabel.ForeColor = READONLY_PURPLE;
                toolTip_ReadOnly.SetToolTip(DraftBox, TIP);
            }
            else
            {
                DraftBox.BackColor = FOCUS_GRAY;
                toolTip_ReadOnly.SetToolTip(DraftBox, String.Empty);
                SetFont(DraftLabel);
            }
            DraftBox.ForeColor = DraftBox.ReadOnly ? Color.White : Color.Black;
        }
        private void DraftBox_MouseLeave(object sender, EventArgs e)
        {
            if (!DraftBox.ReadOnly) DraftBox.BackColor = CONTROL_GRAY;
            DraftBox.ForeColor = DraftBox.ReadOnly ? READONLY_GRAY : Color.White;
            DraftLabel.ForeColor = Color.White;
        }

        private void ComboExamples_MouseHover(object sender, EventArgs e) => ExampleLabel.ForeColor = COMBO_BLUE;
        private void ComboExamples_MouseLeave(object sender, EventArgs e) => ExampleLabel.ForeColor = Color.White;
        private void ComboFunctions_MouseHover(object sender, EventArgs e) => FunctionLabel.ForeColor = COMBO_BLUE;
        private void ComboFunctions_MouseLeave(object sender, EventArgs e) => FunctionLabel.ForeColor = Color.White;
        private void ComboSpecial_MouseHover(object sender, EventArgs e) => FunctionLabel.ForeColor = COMBO_BLUE;
        private void ComboSpecial_MouseLeave(object sender, EventArgs e) => FunctionLabel.ForeColor = Color.White;
        private void ComboColoring_MouseHover(object sender, EventArgs e) => ModeLabel.ForeColor = COMBO_BLUE;
        private void ComboColoring_MouseLeave(object sender, EventArgs e) => ModeLabel.ForeColor = Color.White;
        private void ComboContour_MouseHover(object sender, EventArgs e) => ContourLabel.ForeColor = COMBO_BLUE;
        private void ComboContour_MouseLeave(object sender, EventArgs e) => ContourLabel.ForeColor = Color.White;

        private void CheckComplex_MouseHover(object sender, EventArgs e) => CheckComplex.ForeColor = COMBO_BLUE;
        private void CheckComplex_MouseLeave(object sender, EventArgs e) => CheckComplex.ForeColor = Color.White;
        private void CheckSwap_MouseHover(object sender, EventArgs e) => CheckSwap.ForeColor = COMBO_BLUE;
        private void CheckSwap_MouseLeave(object sender, EventArgs e) => CheckSwap.ForeColor = Color.White;
        private void CheckCoor_MouseHover(object sender, EventArgs e) => CheckCoor.ForeColor = COMBO_BLUE;
        private void CheckCoor_MouseLeave(object sender, EventArgs e) => CheckCoor.ForeColor = Color.White;
        private void CheckPoints_MouseHover(object sender, EventArgs e) => CheckPoints.ForeColor = COMBO_BLUE;
        private void CheckPoints_MouseLeave(object sender, EventArgs e) => CheckPoints.ForeColor = Color.White;
        private void CheckShade_MouseHover(object sender, EventArgs e) => CheckShade.ForeColor = COMBO_BLUE;
        private void CheckShade_MouseLeave(object sender, EventArgs e) => CheckShade.ForeColor = Color.White;
        private void CheckRetain_MouseHover(object sender, EventArgs e) => CheckRetain.ForeColor = COMBO_BLUE;
        private void CheckRetain_MouseLeave(object sender, EventArgs e) => CheckRetain.ForeColor = Color.White;
        private void CheckAuto_MouseHover(object sender, EventArgs e) => CheckAuto.ForeColor = COMBO_BLUE;
        private void CheckAuto_MouseLeave(object sender, EventArgs e) => CheckAuto.ForeColor = Color.White;
        private void CheckEdit_MouseHover(object sender, EventArgs e) => CheckEdit.ForeColor = COMBO_BLUE;
        private void CheckEdit_MouseLeave(object sender, EventArgs e) => CheckEdit.ForeColor = Color.White;

        private void PointNumDisplay_MouseHover(object sender, EventArgs e)
        { PointNumLabel.ForeColor = READONLY_PURPLE; PointNumDisplay.ForeColor = Color.White; }
        private void PointNumDisplay_MouseLeave(object sender, EventArgs e)
        { PointNumLabel.ForeColor = Color.White; PointNumDisplay.ForeColor = READONLY_GRAY; }
        private void TimeDisplay_MouseHover(object sender, EventArgs e)
        { TimeLabel.ForeColor = READONLY_PURPLE; TimeDisplay.ForeColor = Color.White; }
        private void TimeDisplay_MouseLeave(object sender, EventArgs e)
        { TimeLabel.ForeColor = Color.White; TimeDisplay.ForeColor = READONLY_GRAY; }
        private void X_CoorDisplay_MouseHover(object sender, EventArgs e)
        { X_Coor.ForeColor = READONLY_PURPLE; X_CoorDisplay.ForeColor = Color.White; }
        private void X_CoorDisplay_MouseLeave(object sender, EventArgs e)
        { X_Coor.ForeColor = Color.White; X_CoorDisplay.ForeColor = READONLY_GRAY; }
        private void Y_CoorDisplay_MouseHover(object sender, EventArgs e)
        { Y_Coor.ForeColor = READONLY_PURPLE; Y_CoorDisplay.ForeColor = Color.White; }
        private void Y_CoorDisplay_MouseLeave(object sender, EventArgs e)
        { Y_Coor.ForeColor = Color.White; Y_CoorDisplay.ForeColor = READONLY_GRAY; }
        private void ModulusDisplay_MouseHover(object sender, EventArgs e)
        { Modulus.ForeColor = READONLY_PURPLE; ModulusDisplay.ForeColor = Color.White; }
        private void ModulusDisplay_MouseLeave(object sender, EventArgs e)
        { Modulus.ForeColor = Color.White; ModulusDisplay.ForeColor = READONLY_GRAY; }
        private void AngleDisplay_MouseHover(object sender, EventArgs e)
        { Angle.ForeColor = READONLY_PURPLE; AngleDisplay.ForeColor = Color.White; }
        private void AngleDisplay_MouseLeave(object sender, EventArgs e)
        { Angle.ForeColor = Color.White; AngleDisplay.ForeColor = READONLY_GRAY; }
        private void FunctionDisplay_MouseHover(object sender, EventArgs e)
        { ValueLabel.ForeColor = READONLY_PURPLE; FunctionDisplay.ForeColor = Color.White; }
        private void FunctionDisplay_MouseLeave(object sender, EventArgs e)
        { ValueLabel.ForeColor = Color.White; FunctionDisplay.ForeColor = READONLY_GRAY; }

        private void SubtitleBox_MouseHover(object sender, EventArgs e) => SubtitleBox.ForeColor = ERROR_RED;
        private void SubtitleBox_MouseLeave(object sender, EventArgs e) => SubtitleBox.ForeColor = Color.White;
        private void CaptionBox_MouseHover(object sender, EventArgs e) => CaptionBox.ForeColor = Color.White;
        private void CaptionBox_MouseLeave(object sender, EventArgs e) => CaptionBox.ForeColor = READONLY_GRAY;
        private void PreviewLabel_MouseHover(object sender, EventArgs e) => PreviewLabel.ForeColor = READONLY_PURPLE;
        private void PreviewLabel_MouseLeave(object sender, EventArgs e) => PreviewLabel.ForeColor = Color.White;
        private void X_Bar_MouseHover(object sender, EventArgs e) => X_Bar.ForeColor = READONLY_PURPLE;
        private void X_Bar_MouseLeave(object sender, EventArgs e) => X_Bar.ForeColor = Color.White;
        private void Y_Bar_MouseHover(object sender, EventArgs e) => Y_Bar.ForeColor = READONLY_PURPLE;
        private void Y_Bar_MouseLeave(object sender, EventArgs e) => Y_Bar.ForeColor = Color.White;

        private static void ResizeControl(PictureBox pbx, int delta, bool isLarge)
        {
            if (isLarge ? is_resized : !is_resized) return; // To prevent repetitive call
            var (_location, _size) = isLarge ? (-delta, 2 * delta) : (delta, -2 * delta);
            pbx.Location = new(pbx.Location.X + _location, pbx.Location.Y + _location);
            pbx.Size = new(pbx.Width + _size, pbx.Height + _size);
            is_resized = isLarge;
        }
        private static void EnlargePicture(PictureBox pbx, int increment) => ResizeControl(pbx, increment, true);
        private static void ShrinkPicture(PictureBox pbx, int decrement) => ResizeControl(pbx, decrement, false);

        private void PictureLogo_MouseHover(object sender, EventArgs e) => EnlargePicture(PictureLogo, 5);
        private void PictureLogo_MouseLeave(object sender, EventArgs e) => ShrinkPicture(PictureLogo, 5);
        private void PicturePlay_MouseHover(object sender, EventArgs e) => EnlargePicture(PicturePlay, 2);
        private void PicturePlay_MouseLeave(object sender, EventArgs e) => ShrinkPicture(PicturePlay, 2);
        private void PictureIncorrect_MouseHover(object sender, EventArgs e) => EnlargePicture(PictureIncorrect, 2);
        private void PictureIncorrect_MouseLeave(object sender, EventArgs e) => ShrinkPicture(PictureIncorrect, 2);

        private void ExportButton_MouseHover(object sender, EventArgs e) => AddressInput_DoubleClick(sender, e);
        private void StoreButton_MouseHover(object sender, EventArgs e) => AddressInput_DoubleClick(sender, e);
        #endregion
    } /// The visualization interface
    public class MyMessageBox : Form
    {
        private static Button btnOk;
        private static TextBox txtMessage;
        private static readonly Color BACKDROP_GRAY = Graph.Argb(64, 64, 64),
            FORMAL_FONT = Graph.Argb(224, 224, 224), CUSTOM_FONT = Color.Turquoise, EXCEPTION_FONT = Color.LightPink,
            FORMAL_BUTTON = Color.Black, CUSTOM_BUTTON = Color.DarkBlue, EXCEPTION_BUTTON = Color.DarkRed;
        private static float scale_factor;
        private static readonly float MSG_TXT_SIZE = 10f, BTN_TXT_SIZE = 7f;
        private static readonly int DIST = 10, BTN_SIZE = 25, BORDER = 10; // DIST = dist(btnOk, txtMessage)
        private static bool is_resized;
        private static readonly string MSG_FONT = "Microsoft YaHei UI", BTN_FONT = "Microsoft YaHei UI", BTN_TXT = "OK";

        private static void BtnOk_MouseEnterLeave(bool isEnter)
        {
            if (isEnter ? is_resized : !is_resized) return; // To prevent repetitive call
            var (_size, _location, _font) = isEnter ? (2, -1, 1f) : (-2, 1, -1f);
            btnOk.Size = new(btnOk.Width + _size, btnOk.Height + _size);
            btnOk.Location = new(btnOk.Location.X + _location, btnOk.Location.Y + _location);
            btnOk.Font = new(btnOk.Font.FontFamily, btnOk.Font.Size + _font, btnOk.Font.Style);
            is_resized = isEnter;
        } // Analogous to Graph.ResizeControl
        private void BtnOk_MouseEnter(object sender, EventArgs e) => BtnOk_MouseEnterLeave(true);
        private void BtnOk_MouseLeave(object sender, EventArgs e) => BtnOk_MouseEnterLeave(false);
        private void Form_KeyDown(object sender, KeyEventArgs e) { if (e.KeyCode == Keys.Enter) Close(); }

        private void SetUpForm(int width, int height)
        {
            FormBorderStyle = FormBorderStyle.None; TopMost = true; Size = new(width, height);
            StartPosition = FormStartPosition.CenterScreen; BackColor = SystemColors.ControlDark;
        }
        private static void SetUpTextBox(string message, int width, int height, Color txtColor)
        {
            txtMessage = new()
            {
                Text = message,
                Font = new(MSG_FONT, MSG_TXT_SIZE, FontStyle.Regular),
                ForeColor = txtColor,
                Multiline = true,
                ReadOnly = true,
                BorderStyle = BorderStyle.None,
                BackColor = BACKDROP_GRAY,
                ScrollBars = ScrollBars.Vertical
            };
            txtMessage.SetBounds(BORDER, BORDER, width - BORDER * 2, height - BORDER - 2 * DIST - BTN_SIZE);
            txtMessage.SelectionStart = message.Length; txtMessage.SelectionLength = 0;
            txtMessage.GotFocus += (sender, e) => { Graph.HideCaret(txtMessage.Handle); }; // This works well!
        }
        private void SetUpButton(int width, int height, Color btnColor, Color btnTxtColor)
        {
            btnOk = new()
            {
                Size = new(BTN_SIZE * 2, BTN_SIZE),
                Location = new(width / 2 - BTN_SIZE, height - DIST - BTN_SIZE),
                BackColor = btnColor,
                ForeColor = btnTxtColor,
                Font = new(BTN_FONT, BTN_TXT_SIZE, FontStyle.Bold),
                FlatStyle = FlatStyle.Flat,
                Text = BTN_TXT,
            };
            btnOk.FlatAppearance.BorderSize = 0; btnOk.Click += (sender, e) => { Close(); };
            btnOk.MouseEnter += BtnOk_MouseEnter; btnOk.MouseLeave += BtnOk_MouseLeave;
        }
        private void Setup(string message, int width, int height, Color txtColor, Color btnColor, Color btnTxtColor)
        {
            SetUpForm(width, height);
            SetUpTextBox(message, width, height, txtColor);
            SetUpButton(width, height, btnColor, btnTxtColor);
            Controls.Add(txtMessage); Controls.Add(btnOk);

            Graph.ReduceFontSizeByScale(this, ref scale_factor);
            KeyPreview = true; KeyDown += new(Form_KeyDown);
        }
        private static void Display(string message, int width, int height, Color txtColor, Color btnColor, Color btnTxtColor)
        {
            MyMessageBox msgBox = new();
            msgBox.Setup(message, width, height, txtColor, btnColor, btnTxtColor);
            msgBox.ShowDialog();
        }

        public static void ShowFormal(string message, int width, int height)
            => Display(message, width, height, FORMAL_FONT, FORMAL_BUTTON, Color.White);
        public static void ShowCustom(string message, int width, int height)
            => Display(message, width, height, CUSTOM_FONT, CUSTOM_BUTTON, Color.White);
        public static void ShowException(string message, int width, int height)
            => Display(message, width, height, EXCEPTION_FONT, EXCEPTION_BUTTON, Color.White);
    } /// Customized box construction

    /// <summary>
    /// TOOLKIT SECTION
    /// </summary>
    public class MyString
    {
        private static string[] AddSuffix(string[] str) { for (int i = 0; i < str.Length; i++) str[i] += "("; return str; }
        public static readonly string[] FUNC_NAMES = AddSuffix(["func", "Func", "polar", "Polar", "param", "Param"]);
        public static readonly string[] LOOP_NAMES = AddSuffix(["loop", "Loop"]);
        private static readonly List<string> CONFUSION = ["zeta", "Zeta"];
        protected static readonly string SUB_CHARS = ":;";

        #region Reckoning
        protected static int CountChars(ReadOnlySpan<char> input, string charsToCheck)
        {
            HashSet<char> charSet = [.. charsToCheck]; // Fast lookup
            int count = 0; foreach (char c in input) if (charSet.Contains(c)) count++;
            return count;
        }
        protected static (int, int, int, int) PrepareLoop(ReadOnlySpan<char> input) => (CountChars(input, "("), input.Length - 1, -1, 0);
        public static bool ContainsAny(ReadOnlySpan<char> input, string charsToCheck)
        {
            HashSet<char> charSet = [.. charsToCheck]; // Fast lookup
            foreach (char c in input) if (charSet.Contains(c)) return true;
            return false;
        }
        public static bool ContainsAny(string input, ReadOnlySpan<string> stringsToCheck)
        {
            foreach (string s in stringsToCheck) if (input.Contains(s)) return true;
            return false;
        }
        #endregion // Automatic conversion from string to ReadOnlySpan<char>, from string[] to ReadOnlySpan<string>

        #region Parentheses
        private static int PairedParenthesis(ReadOnlySpan<char> input, int start)
        {
            for (int i = start + 1, count = 1; ; i++)
            { if (input[i] == '(') count++; else if (input[i] == ')') count--; if (count == 0) return i; }
        }
        protected static (int, int, string[]) PrepareSeriesSub(string input)
        {
            int i = input.IndexOf(ReplaceTags.UNDERLINE), end = PairedParenthesis(input, i + 1);
            return (i, end, ReplaceRecover(BraFreePart(input, i + 1, end)));
        }
        protected static void ResetStartEnd(ReadOnlySpan<char> input, ref int start, ref int end)
        {
            static (int, int) innerBra(ReadOnlySpan<char> input, int start)
            { for (int i = start, j = -1; ; i--) { if (input[i] == ')') j = i; else if (input[i] == '(') return (i, j); } }
            static int pairedInnerBra(ReadOnlySpan<char> input, int start)
            { for (int i = start + 1; ; i++) if (input[i] == ')') return i; }

            int _start = start; (start, end) = innerBra(input, start); if (end == -1) end = pairedInnerBra(input, _start);
        } // Backward lookup for parenthesis pairs, extremely sensitive
        public static bool CheckParenthesis(ReadOnlySpan<char> input)
        {
            int sum = 0;
            foreach (char c in input) { if (c == '(') sum++; else if (c == ')') sum--; if (sum < 0) return false; }
            return sum == 0;
        }
        private static string WrapBase(int n, char c1, char c2) => String.Concat(c1, n.ToString(), c2);
        #endregion

        #region Replacement
        private static string Extract(string input, int start, int end) => input.AsSpan(start, end - start + 1).ToString();
        protected static string BraFreePart(string input, int start, int end) => Extract(input, start + 1, end - 1);
        protected static string TryBraNum(string input, char c1, char c2)
        { ThrowException(input[0] != c1 || input[^1] != c2); return BraFreePart(input, 0, input.Length - 1); }
        public static string Replace(string origStr, string subStr, int start, int end)
            => String.Create(start + subStr.Length + origStr.Length - end - 1, (origStr, subStr, start, end), (span, state) =>
            {
                var (_origStr, _subStr, _start, _end) = state;
                _origStr.AsSpan(0, _start).CopyTo(span); // Copying the beginning
                _subStr.AsSpan().CopyTo(span[_start..]); // Copying the substitution
                _origStr.AsSpan(_end + 1).CopyTo(span[(_start + _subStr.Length)..]); // Copying the remaining
            });
        public static string ReplaceLoop(string[] split, int origIdx, int subIdx, int i)
            => split[origIdx].Replace(split[subIdx], WrapBase(i, '(', ')'));
        protected static string ReplaceInput(string input, ref int countBra, int start, int end)
            => Replace(input, WrapBase(countBra++, '[', ']'), start, end);
        protected static void ReplaceInput(ref string input, ref int countBra, ref int start, int end, ref int tagL)
        { start -= tagL; tagL = 0; input = ReplaceInput(input, ref countBra, start--, end); }
        private static string ReplaceInterior(string input, char origChar, char subChar)
        {
            if (!input.Contains(ReplaceTags.UNDERLINE)) return input;
            StringBuilder result = new(input);
            for (int i = 0; i < result.Length; i++)
            {
                if (result[i] != ReplaceTags.UNDERLINE) continue;
                int endIndex = PairedParenthesis(input, i + 1);
                for (int j = i + 1; j < endIndex; j++)
                {
                    if (result[j] != origChar) continue;
                    result.Remove(j, 1).Insert(j, subChar);
                }
                i = endIndex;
            } // Sensitive
            return result.ToString();
        } // To prevent the interior ',' from interfering with exterior splitting
        private static string[] ReplaceRecover(string input)
            => [.. SplitByChars(ReplaceInterior(input, ',', SUB_CHARS[0]), ",").Select(part => part.Replace(SUB_CHARS[0], ','))];
        protected static string ReplaceSubstrings(string input, List<string> substrings, string substitution)
           => System.Text.RegularExpressions.Regex.Replace(input, String.Join("|", substrings), substitution);
        public static string ReplaceConfusion(string input) => ReplaceSubstrings(input, CONFUSION, String.Empty);
        #endregion

        #region Miscellaneous
        public static string[] SplitString(string input)
            => ReplaceRecover(BraFreePart(input, input.IndexOf('('), PairedParenthesis(input, input.IndexOf('('))));
        public static string[] SplitByChars(string input, string delimiters)
        {
            HashSet<char> delimiterSet = [.. delimiters];
            List<string> segments = [];
            StringBuilder segmentBuilder = new();
            for (int i = 0; i < input.Length; i++)
            {
                if (delimiterSet.Contains(input[i]))
                {
                    segments.Add(segmentBuilder.ToString());
                    segmentBuilder.Clear();
                }
                else segmentBuilder.Append(input[i]);
            }
            segments.Add(segmentBuilder.ToString());
            return [.. segments]; // Collection expression (.NET 8.0)
        }
        protected static string TrimStartChar(string input, char startChar)
        {
            int startIndex = 0, length = input.Length;
            while (startIndex < length && input[startIndex] == startChar) startIndex++;
            if (startIndex == length) return String.Empty;
            StringBuilder result = new(length - startIndex);
            return result.Append(input, startIndex, length - startIndex).ToString();
        }
        public static string TrimLargeNum(float input, float threshold)
            => MathF.Abs(input) < threshold ? input.ToString("#0.000000") : input.ToString("E3");
        public static string GetAngle(float x, float y) => (Graph.ArgRGB(x, y) / MathF.PI).ToString("#0.00000") + " * PI";
        public static void ThrowException(bool error = true) { if (error) throw new Exception(); }
        public static void ThrowInvalidLengths(string[] split, int[] length) => ThrowException(!length.Contains(split.Length));
        protected static int ThrowReturnLengths(string[] split, int length, int iteration)
        { ThrowInvalidLengths(split, [length, length + 1]); return split.Length == length ? iteration : RealSub.ToInt(split[^1]); }
        #endregion
    } /// String manipulations
    public class RealComplex : MyString
    {
        protected static readonly float GAMMA = 0.57721566f;
        protected static readonly int THRESHOLD = 10, STEP = 1; // THRESHOLD: breaking long expressions; STEP: referring to chunks
        protected const char _A = 'a', A_ = 'A', B_ = 'B', _C = 'c', C_ = 'C', _D_ = '$', E = 'e', E_ = 'E',
            _F = 'f', F_ = 'F', _F_ = '!', G = 'γ', G_ = 'G', _H = 'h', I = 'i', I_ = 'I', J_ = 'J', K_ = 'K', _L = 'l',
            M_ = 'M', MAX = '>', MIN = '<', MODE_1 = '1', MODE_2 = '2', P = 'π', P_ = 'P', _Q = 'q', _R = 'r',
            _S = 's', S_ = 'S', SP = '#', _T = 't', _X = 'x', X_ = 'X', _Y = 'y', Y_ = 'Y', _Z = 'z', Z_ = 'Z', _Z_ = 'ζ';

        public unsafe static int[] GetArithProg(int length, int diff)
        {
            if (length == 0) return []; int[] progression = new int[length];
            fixed (int* ptr = progression)
            { int* _ptr = ptr; for (int i = 0, j = 0; i < length; i++, _ptr++, j += diff) *_ptr = j; } // _ptr is necessary
            return progression;
        }
        protected static void Initialize<TEntry>(int rows, int columns, ref int rowChk, ref int[]? rowOffs, ref uint colBytes,
            ref int strd, ref int[]? strdInit, ref uint strdBytes, ref int res, ref int resInit, ref uint resBytes)
        {
            rowChk = rows / STEP; rowOffs = GetArithProg(rows, columns);
            strd = columns * STEP; strdInit = GetArithProg(rowChk, STEP);
            resInit = rowChk * STEP; res = rows - resInit;

            int _colBytes = columns * Unsafe.SizeOf<TEntry>(); uint getBytes(int times) => (uint)(_colBytes * times);
            colBytes = getBytes(1); strdBytes = getBytes(STEP); resBytes = getBytes(res);
        } // Fields for optimization
        public static void For(int start, int end, Action<int> action) { for (int i = start; i <= end; i++) action(i); }
        protected static Matrix<float> ChooseMode(string mode, Matrix<float> m1, Matrix<float> m2, int[] rowOffs, int columns)
        {
            switch (Char.Parse(mode))
            {
                case MODE_1: return m1;
                case MODE_2: return m2;
                default: ThrowException(); return new(rowOffs, columns); // Should not have happened
            }
        }
        protected static MatrixCopy<TEntry> HandleSolo<TEntry>(string input, MatrixCopy<TEntry> mc)
        { ThrowException(input.Length != 1); return mc; }
        protected static string[] PrepareBreakPower(string input, int THRESHOLD)
        {
            StringBuilder result = new(input);
            for (int i = 0, flag = 0; i < result.Length; i++)
            {
                if (result[i] != '^') continue;
                if (++flag % THRESHOLD == 0) result.Remove(i, 1).Insert(i, SUB_CHARS[0]);
            }
            return SplitByChars(result.ToString(), SUB_CHARS[0].ToString());
        }
        protected static (string[], StringBuilder) PrepareBreakPSMD(string input, string signs, int THRESHOLD)
        {
            StringBuilder signsBuilder = new(), result = new(input);
            for (int i = 0, flag = 0; i < result.Length; i++)
            {
                if (!signs.Contains(result[i])) continue;
                if (++flag % THRESHOLD == 0)
                {
                    char subChar = result[i] == signs[0] ? SUB_CHARS[0] : SUB_CHARS[1]; // Necessary
                    result.Remove(i, 1).Insert(i, subChar);
                    signsBuilder.Append(result[i]);
                }
            }
            return (SplitByChars(result.ToString(), SUB_CHARS), signsBuilder);
        }
        private static StringBuilder GetSignsBuilder(string input, string signs)
        {
            StringBuilder signsBuilder = new();
            for (int i = 0; i < input.Length; i++) if (signs.Contains(input[i])) signsBuilder.Append(input[i]);
            return signsBuilder;
        }
        protected static (string[], StringBuilder) GetPlusSubtractComponents(string input)
        {
            bool minusHead = input[0] == '-'; input = TrimStartChar(input, '-');
            return (SplitByChars(input, "+-"), GetSignsBuilder(String.Concat(minusHead ? '-' : '+', input), "+-"));
        } // Sensitive
        protected static (string[], StringBuilder) GetMultiplyDivideComponents(string input)
            => (SplitByChars(input, "*/"), GetSignsBuilder(input, "*/"));
        protected static (bool trig, bool hyper) IsInverseFunc(int start, string input)
            => (start > 1 ? input[start - 2] == _A : true, start > 2 ? input[start - 3] == _A : true); // Should not simplify
    } /// Commonalities for RealSub & ComplexSub
    public class ReplaceTags : RealComplex
    {
        public static readonly string[] FUNCTIONS =
            [ "floor", "ceil", "round", "sgn", "F", "gamma", "beta", "zeta", "mod", "nCr", "nPr",
                "max", "min", "log", "exp", "sqrt", "abs", "factorial", "arsinh", "arcosh", "artanh",
                "arcsin", "arccos", "arctan", "sinh", "cosh", "tanh", "sin", "cos", "tan", "conjugate", "e" ];
        public static readonly string[] SPECIALS =
            [ "sum", "product", "iterate", "iterate1", "iterate2", "composite", "composite1", "composite2", "cocoon",
                "iterateLoop", "loop", "func", "polar", "param" ];
        public static readonly string[] EX_COMPLEX =
            [
                "(z-1)/(z+1)",
                "z^(1+10i)cos((z-1)/(z^13+z+1))",
                "sum(1/(1-z^n),n,1,100)-100",
                "prod(exp(2/(e(-k/5)z-1)+1),k,1,5)",
                "coc(iterate((1/Z+Z){0},z,k,1,1000),e(0.02))",
                "iterate(exp(zZ),z,k,1,100)",
                "iterateLoop(ZZ+z,0,k,1,30)",
                "comp(zz,sin(zZ),cos(z/Z))"
            ];
        public static readonly string[] EX_REAL =
            [
                "nCr(x,y)",
                "min(sin(xy),tan(x),tan(y))",
                "ceil(x)round(y)-floor(y)round(x)",
                "IterateLoop(x^X,1,k,1,30,y-X)",
                "iterate1(x/X+X/y,xy,k,1,5)",
                "iterate2(1/X+1/Y,XY,sin(x+y),cos(x-y),k,1,15,2)",
                "comp1(xy,tan(Xx),Artanh(X-y))",
                "comp2(xy,xx+yy,sin(X+Y),cos(X-Y),2)"
            ];
        public static readonly string[] EX_CURVES =
            [
                "func(zeta(x,50))",
                "func(sum(sin(x2^k)/2^k,k,0,100),-pi,pi,0.001)",
                "func(beta(sinh(x),cosh(x)),-2,2,0.0001)",
                "polar(sqrt(cos(2theta)),theta,0,2pi,0.0001)",
                "polar(cos(5k)cos(7k),k,0,2pi,0.001)",
                "loop(polar(0.1jcos(5k+0.7jpi),k,0,pi),j,1,10)",
                "param(sin(7k),cos(9k),k,0,2pi,0.001)",
                "loop(param(cos(m)^k,sin(m)^k,m,0,pi/2),k,1,10)"
            ];
        public static readonly char FUNC_HEAD = '~', UNDERLINE = '_', DOLLAR = _D_;
        public static readonly string FUNC = "φ", POLAR = "ψ", PARAM = "ρ", ITLOOP = "ι",
            LOG = _L.ToString(), EXP = E_.ToString(), SQRT = _Q.ToString(), ABS = _A.ToString(), FACT = _F_.ToString(),
            SIN = _S.ToString(), COS = _C.ToString(), TAN = _T.ToString(), // This should come first
            AS = String.Concat(_A, SIN), AC = String.Concat(_A, COS), AT = String.Concat(_A, TAN),
            SH = String.Concat(SIN, _H), CH = String.Concat(COS, _H), TH = String.Concat(TAN, _H),
            ASH = String.Concat(AS, _H), ACH = String.Concat(AC, _H), ATH = String.Concat(AT, _H),
            PROD = P_.ToString(), SUM = S_.ToString(), COC = K_.ToString(),
            F = F_.ToString(), GA = G_.ToString(), BETA = B_.ToString(), ZETA = _Z_.ToString(),
            FLOOR = _F.ToString(), CEIL = _C.ToString(), ROUND = _R.ToString(), SIGN = _S.ToString(),
            MOD = M_.ToString(), NCR = C_.ToString(), NPR = A_.ToString(), _MAX = MAX.ToString(), _MIN = MIN.ToString(),
            IT = I_.ToString(), IT1 = String.Concat(MODE_1, IT), IT2 = String.Concat(MODE_2, IT),
            COMP = J_.ToString(), COMP1 = String.Concat(MODE_1, COMP), COMP2 = String.Concat(MODE_2, COMP),
            CONJ = J_.ToString(), E_SP = String.Concat(EXP, SP),
            PI = P.ToString(), _GA = G.ToString();
        private static Dictionary<string, string> Concat(Dictionary<string, string> dic1, Dictionary<string, string> dic2)
            => dic1.Concat(dic2).ToDictionary(pair => pair.Key, pair => pair.Value); // Series first, Standard next
        private static readonly Dictionary<string, string> COMMON_STANDARD = new()
        {
            { "log", LOG }, { "Log", LOG }, { "ln", LOG }, { "Ln", LOG },
            { "exp", EXP }, { "Exp", EXP },
            { "sqrt", SQRT }, { "Sqrt", SQRT },
            { "abs", ABS }, { "Abs", ABS },
            { "factorial", FACT }, { "Factorial", FACT }, { "fact", FACT }, { "Fact", FACT },
            { "arsinh", ASH }, { "Arsinh", ASH }, { "asinh", ASH }, { "Asinh", ASH },
            { "arcosh", ACH }, { "Arcosh", ACH }, { "acosh", ACH }, { "Acosh", ACH },
            { "artanh", ATH }, { "Artanh", ATH }, { "atanh", ATH }, { "Atanh", ATH },
            { "arcsin", AS }, { "Arcsin", AS }, { "asin", AS }, { "Asin", AS },
            { "arccos", AC }, { "Arccos", AC }, { "acos", AC }, { "Acos", AC },
            { "arctan", AT }, { "Arctan", AT }, { "atan", AT }, { "Atan", AT },
            { "sinh", SH }, { "Sinh", SH },
            { "cosh", CH }, { "Cosh", CH },
            { "tanh", TH }, { "Tanh", TH },
            { "sin", SIN }, { "Sin", SIN },
            { "cos", COS }, { "Cos", COS },
            { "tan", TAN }, { "Tan", TAN }
        };
        private static readonly Dictionary<string, string> COMMON_SERIES = AddSuffix(new()
        {
            { "product", PROD }, { "Product", PROD }, { "prod", PROD }, { "Prod", PROD },
            { "sum", SUM }, { "Sum", SUM },
            { "F", F },
            { "gamma", GA }, { "Gamma", GA }, { "ga", GA }, { "Ga", GA },
            { "beta", BETA }, { "Beta", BETA },
            { "zeta", ZETA }, { "Zeta", ZETA },
            { "cocoon", COC}, { "Cocoon", COC}, { "coc", COC}, { "Coc", COC}
        }, UNDERLINE);
        private static readonly Dictionary<string, string> COMMON = Concat(COMMON_SERIES, COMMON_STANDARD);
        private static readonly Dictionary<string, string> REAL_STANDARD = AddSuffix(new()
        {
            { "floor", FLOOR }, { "Floor", FLOOR },
            { "ceil", CEIL }, { "Ceil", CEIL },
            { "round", ROUND }, { "Round", ROUND },
            { "sign", SIGN }, { "Sign", SIGN }, { "sgn", SIGN }, { "Sgn", SIGN }
        }, DOLLAR);
        private static readonly Dictionary<string, string> REAL_SERIES = AddSuffix(new()
        {
            { "mod", MOD }, { "Mod", MOD },
            { "nCr", NCR }, { "nPr", NPR },
            { "max", _MAX }, { "Max", _MAX }, { "min", _MIN }, { "Min", _MIN },
            { "iterate1", IT1 }, { "Iterate1", IT1 }, { "iterate2", IT2 }, { "Iterate2", IT2 },
            { "composite1", COMP1 }, { "Composite1", COMP1 }, { "comp1", COMP1 }, { "Comp1", COMP1 },
            { "composite2", COMP2 }, { "Composite2", COMP2 }, { "comp2", COMP2 }, { "Comp2", COMP2 }
        }, UNDERLINE);
        private static readonly Dictionary<string, string> REAL = Concat(REAL_SERIES, REAL_STANDARD);
        private static readonly Dictionary<string, string> COMPLEX_STANDARD = new()
        { { "conjugate", CONJ }, { "Conjugate", CONJ }, { "conj", CONJ }, { "Conj", CONJ }, { "e", E_SP } };
        private static readonly Dictionary<string, string> COMPLEX_SERIES = AddSuffix(new()
        {
            { "iterate", IT }, { "Iterate", IT },
            { "composite", COMP }, { "Composite", COMP }, { "comp", COMP }, { "Comp", COMP }
        }, UNDERLINE);
        private static readonly Dictionary<string, string> COMPLEX = Concat(COMPLEX_SERIES, COMPLEX_STANDARD);
        private static readonly Dictionary<string, string> CONSTANTS = new()
        { { "pi", PI }, { "Pi", PI }, { "gamma", _GA }, { "Gamma", _GA }, { "ga", _GA }, { "Ga", _GA } };
        private static readonly Dictionary<string, string> TAGS = AddSuffix(new()
        {
            { "func", FUNC }, { "Func", FUNC },
            { "polar", POLAR }, { "Polar", POLAR },
            { "param", PARAM }, { "Param", PARAM },
            { "iterateLoop", ITLOOP }, { "IterateLoop", ITLOOP }
        }, UNDERLINE);
        private static Dictionary<string, string> AddPrefixSuffix(Dictionary<string, string> dictionary)
        {
            Dictionary<string, string> _dictionary = [];
            foreach (var kvp in dictionary) _dictionary[String.Concat(kvp.Key, '(')] = String.Concat(FUNC_HEAD, kvp.Value, '(');
            return _dictionary;
        }
        private static Dictionary<string, string> AddSuffix(Dictionary<string, string> dictionary, char suffix)
        {
            Dictionary<string, string> _dictionary = [];
            foreach (var kvp in dictionary) _dictionary[kvp.Key] = String.Concat(kvp.Value, suffix);
            return _dictionary;
        }
        private static string ReplaceBase(string input, Dictionary<string, string> dictionary)
        {
            foreach (var kvp in dictionary) input = input.Replace(kvp.Key, kvp.Value);
            return input;
        }
        private static string ReplaceCommon(string input) => ReplaceConstant(ReplaceBase(input, AddPrefixSuffix(COMMON)));
        private static string ReplaceConstant(string input) => ReplaceBase(input, CONSTANTS);
        protected static string ReplaceReal(string input) => ReplaceCommon(ReplaceBase(input, AddPrefixSuffix(REAL)));
        protected static string ReplaceComplex(string input) => ReplaceCommon(ReplaceBase(input, AddPrefixSuffix(COMPLEX)));
        public static string ReplaceCurves(string input) => ReplaceBase(input, AddPrefixSuffix(TAGS));
    } /// Function name interpretors
    public class RecoverMultiply : ReplaceTags
    {
        public static readonly string _ZZ_ = String.Concat(_Z, Z_), LR_BRA = "()",
            BARRED_CHARS = String.Concat("\t!\"#$%&\':;<=>?@[\\]_`~", FUNC, POLAR, PARAM, ITLOOP);
        private static readonly string VAR_REAL = String.Concat(_X, _Y, X_, Y_), VAR_COMPLEX = String.Concat(_Z, Z_, I),
            CONST = String.Concat(E, P, G), ARITH = "+-*/^(,|", BRA_L = "({", BRA_R = ")}";
        private static readonly List<string> ENTER_BLANK = ["\n", "\r", " "];
        public static string Simplify(string input, bool isComplex = false)
        {
            ThrowException(!CheckParenthesis(input) || input.Contains(LR_BRA) || ContainsAny(input, BARRED_CHARS));
            Func<string, string> replaceTags = isComplex ? ReplaceComplex : ReplaceReal;
            return replaceTags(ReplaceSubstrings(input, ENTER_BLANK, String.Empty));
        } // Used only once at the beginning
        protected static string Recover(string input, bool isComplex)
        {
            int length = input.Length; if (length == 1) return input;
            StringBuilder recoveredInput = new(length * 2); // The longest possible length
            recoveredInput.Append(input[0]);
            for (int i = 1; i < length; i++) // Should not use parallel
            {
                if (DecideRecovery(input[i - 1], input[i], isComplex ? IsVarComplex : IsVarReal)) recoveredInput.Append('*');
                recoveredInput.Append(input[i]);
            }
            return recoveredInput.ToString();
        }
        private static bool DecideRecovery(char c1, char c2, Func<char, bool> isVar)
        {
            bool isConstNum(char c) => IsConst(c) || Char.IsNumber(c);
            bool isConstVar(char c) => IsConst(c) || isVar(c);
            bool isConstNumVar(char c) => IsConst(c) || Char.IsNumber(c) || isVar(c);
            bool bNV = isConstNum(c1) && isConstVar(c2), bVN = isConstVar(c1) && isConstNum(c2), bVV = isVar(c1) && isVar(c2),
                bNVL = isConstNumVar(c1) && IsBraL(c2), bRNV = IsBraR(c1) && isConstNumVar(c2), bRL = IsBraR(c1) && IsBraL(c2),
                bAF = !IsArithmetic(c1) && IsFunctionHead(c2);
            return bNV || bVN || bVV || bNVL || bRNV || bRL || bAF;
        } // Sensitive
        private static bool IsVarReal(char c) => VAR_REAL.Contains(c);
        private static bool IsVarComplex(char c) => VAR_COMPLEX.Contains(c);
        private static bool IsConst(char c) => CONST.Contains(c);
        private static bool IsArithmetic(char c) => ARITH.Contains(c); // Function heads preceded by these require no anterior recovery
        private static bool IsFunctionHead(char c) => c == FUNC_HEAD;
        private static bool IsBraL(char c) => BRA_L.Contains(c);
        private static bool IsBraR(char c) => BRA_R.Contains(c);
    } /// Recovery of omitted '*'

    /// <summary>
    /// COMPUTATION SECTION
    /// </summary>
    public sealed class ComplexSub : RecoverMultiply
    {
        #region Fields & Constructors
        private readonly uint colBytes, strdBytes, resBytes; // Sizes of chunks
        private readonly int rows, columns, rowChk, strd, res, resInit; // Lengths of chunks
        private readonly int[] rowOffs, strdInit; // rowOffs: for row extraction
        private readonly bool useList; // Whether to use constMtx or not
        private readonly Matrix<Complex> z;
        private readonly Matrix<Complex>[] buffCocs; // To precompute repetitively used blocks
        private readonly MatrixCopy<Complex>[] braValues; // To store values between parenthesis pairs
        private readonly List<ConstMatrix<Complex>> constMtx = [];

        private int countBra, countCst; // countBra: logging parentheses; countCst: logging constants
        private bool readList; // Reading or writing constMtx
        private string input;
        private Matrix<Complex> Z; // For substitution

        public ComplexSub(string input, Matrix<Complex>? z, Matrix<Complex>? Z, Matrix<Complex>[]? buffCocs,
            int rows, int columns, bool useList = false)
        {
            ThrowException(String.IsNullOrEmpty(input));
            this.input = Recover(input, true); braValues = new MatrixCopy<Complex>[CountChars(this.input, "(")];
            if (z != null) this.z = (Matrix<Complex>)z; if (Z != null) this.Z = (Matrix<Complex>)Z;
            this.rows = rows; this.columns = columns; this.useList = useList; this.buffCocs = buffCocs;
            Initialize<Complex>(rows, columns, ref rowChk, ref rowOffs, ref colBytes,
                ref strd, ref strdInit, ref strdBytes, ref res, ref resInit, ref resBytes);
        }
        public ComplexSub(string input, Matrix<float> xCoor, Matrix<float> yCoor, int rows, int columns)
            : this(input, InitilizeZ(xCoor, yCoor, rows, columns), null, null, rows, columns) { } // Special for complex
        private ComplexSub ObtainSub(string input, Matrix<Complex>? Z, Matrix<Complex>[]? buffCocs, bool useList = false)
            => new(input, z, Z, buffCocs, rows, columns, useList);
        private Matrix<Complex> ObtainValue(string input) => new ComplexSub(input, z, Z, buffCocs, rows, columns).Obtain();
        #endregion

        #region Calculations
        private unsafe Matrix<Complex> Hypergeometric(string[] split)
        {
            int n = ThrowReturnLengths(split, 4, 100);
            return HandleMatrix(sum =>
            {
                Matrix<Complex> obtain(int index) => ObtainValue(split[index]);
                Matrix<Complex> product = MtxOne(), _a = obtain(0), _b = obtain(1), _c = obtain(2), initial = obtain(3);
                void hypergeometric(int p, int col)
                {
                    Complex* productPtr = product.RowPtr(p), sumPtr = sum.RowPtr(p),
                        _aPtr = _a.RowPtr(p), _bPtr = _b.RowPtr(p), _cPtr = _c.RowPtr(p), initialPtr = initial.RowPtr(p);
                    for (int q = 0; q < col; q++, productPtr++, sumPtr++, _aPtr++, _bPtr++, _cPtr++, initialPtr++)
                    {
                        for (int i = 0; i <= n; i++)
                        {
                            if (i != 0) *productPtr *= *initialPtr * (i - 1 + *_aPtr) * (i - 1 + *_bPtr) / (i - 1 + *_cPtr) / i;
                            *sumPtr += *productPtr;
                        }
                    }
                }
                Parallel.For(0, rowChk, p => { hypergeometric(strdInit[p], strd); }); if (res != 0) hypergeometric(resInit, res);
            });
        } // Reference: https://en.wikipedia.org/wiki/Hypergeometric_function
        private unsafe Matrix<Complex> Gamma(string[] split)
        {
            int n = ThrowReturnLengths(split, 1, 100);
            return HandleMatrix(output =>
            {
                Matrix<Complex> product = MtxOne(), initial = ObtainValue(split[0]);
                void gamma(int p, int col)
                {
                    Complex* productPtr = product.RowPtr(p), initialPtr = initial.RowPtr(p), outputPtr = output.RowPtr(p);
                    for (int q = 0; q < col; q++, productPtr++, initialPtr++, outputPtr++)
                    {
                        for (int i = 1; i <= n; i++) { Complex temp = *initialPtr / i; *productPtr *= Complex.Exp(temp) / (1 + temp); }
                        *outputPtr = *productPtr * Complex.Exp(-*initialPtr * GAMMA) / *initialPtr;
                    }
                }
                Parallel.For(0, rowChk, p => { gamma(strdInit[p], strd); }); if (res != 0) gamma(resInit, res);
            });
        } // Reference: https://en.wikipedia.org/wiki/Gamma_function
        private unsafe Matrix<Complex> Beta(string[] split)
        {
            int n = ThrowReturnLengths(split, 2, 100);
            return HandleMatrix(output =>
            {
                Matrix<Complex> obtain(int index) => ObtainValue(split[index]);
                Matrix<Complex> product = MtxOne(), input1 = obtain(0), input2 = obtain(1);
                void beta(int p, int col)
                {
                    Complex* productPtr = product.RowPtr(p), input1Ptr = input1.RowPtr(p),
                        input2Ptr = input2.RowPtr(p), outputPtr = output.RowPtr(p);
                    for (int q = 0; q < col; q++, productPtr++, input1Ptr++, input2Ptr++, outputPtr++)
                    {
                        for (int i = 1; i <= n; i++) *productPtr *= 1 + *input1Ptr * *input2Ptr / ((i + *input1Ptr + *input2Ptr) * i);
                        *outputPtr = (*input1Ptr + *input2Ptr) / (*input1Ptr * *input2Ptr) / *productPtr;
                    }
                }
                Parallel.For(0, rowChk, p => { beta(strdInit[p], strd); }); if (res != 0) beta(resInit, res);
            });
        } // Reference: https://en.wikipedia.org/wiki/Beta_function
        private unsafe Matrix<Complex> Zeta(string[] split)
        {
            int n = ThrowReturnLengths(split, 1, 50);
            return HandleMatrix(_sum =>
            {
                Matrix<Complex> sum = MtxZero(), coeff = MtxOne(), _coeff = MtxOne(), initial = ObtainValue(split[0]);
                void zeta(int p, int col)
                {
                    Complex* sumPtr = sum.RowPtr(p), _sumPtr = _sum.RowPtr(p),
                        coeffPtr = coeff.RowPtr(p), _coeffPtr = _coeff.RowPtr(p), initialPtr = initial.RowPtr(p);
                    for (int q = 0; q < col; q++, sumPtr++, _sumPtr++, coeffPtr++, _coeffPtr++, initialPtr++)
                    {
                        for (int i = 0; i <= n; i++)
                        {
                            *coeffPtr *= 0.5f; *_coeffPtr = Complex.ONE; *sumPtr = Complex.ZERO;
                            for (int j = 0; j <= i; j++)
                            {
                                *sumPtr += *_coeffPtr * Complex.Pow(j + 1, -*initialPtr);
                                *_coeffPtr *= (float)(j - i) / (float)(j + 1); // (float) is not redundant
                            }
                            *sumPtr *= *coeffPtr; *_sumPtr += *sumPtr;
                        }
                        *_sumPtr /= 1 - Complex.Pow(2, 1 - *initialPtr);
                    }
                }
                Parallel.For(0, rowChk, p => { zeta(strdInit[p], strd); }); if (res != 0) zeta(resInit, res);
            });
        } // Reference: https://en.wikipedia.org/wiki/Riemann_zeta_function
        //
        private Matrix<Complex> ProcessSPI(string[] split, int validLength, Matrix<Complex> initMtx, Action<ComplexSub> action)
        {
            ThrowInvalidLengths(split, [validLength]);
            ComplexSub buffer = ObtainSub(ReplaceLoop(split, 0, validLength - 3, 0), initMtx, buffCocs, true);

            void resetCount() => buffer.countBra = buffer.countCst = 0;
            _ = buffer.Obtain(); resetCount(); buffer.readList = true; // To precompute constMtx

            For(RealSub.ToInt(split[validLength - 2]), RealSub.ToInt(split[validLength - 1]), i =>
            { buffer.input = Recover(ReplaceLoop(split, 0, validLength - 3, i), true); resetCount(); action(buffer); });
            return buffer.Z;
        } // Meticulously optimized
        private Matrix<Complex> Sum(string[] split) => ProcessSPI(split, 4, MtxZero(), b => { Plus(b.Obtain(), b.Z); });
        private Matrix<Complex> Product(string[] split) => ProcessSPI(split, 4, MtxOne(), b => { Multiply(b.Obtain(), b.Z); });
        private Matrix<Complex> Iterate(string[] split) => ProcessSPI(split, 5, ObtainValue(split[1]), b => { b.Z = b.Obtain(); });
        //
        private Matrix<Complex> Composite(string[] split)
        {
            Matrix<Complex> _value = ObtainValue(split[0]);
            for (int i = 1; i < split.Length; i++) _value = ObtainSub(split[i], _value, buffCocs).Obtain();
            return _value;
        }
        private Matrix<Complex> Cocoon(string[] split)
        {
            ComplexSub body = ObtainSub(split[0], null, new Matrix<Complex>[split.Length - 1]);
            for (int i = 1; i < split.Length; i++) body.buffCocs[i - 1] = ObtainValue(split[i]);
            return body.Obtain();
        } // For shallow and complicated composites
        #endregion

        #region Elements
        private Matrix<Complex> MtxZero() => new(rowOffs, columns);
        private Matrix<Complex> MtxOne() => Const(Complex.ONE).matrix;
        private Matrix<Complex> HandleMatrix(Action<Matrix<Complex>> action) { Matrix<Complex> mtx = MtxZero(); action(mtx); return mtx; }
        [MethodImpl(512)] // AggressiveOptimization
        public unsafe static Matrix<Complex> InitilizeZ(Matrix<float> xCoor, Matrix<float> yCoor, int rows, int columns)
        {
            Matrix<Complex> z = new(GetArithProg(rows, columns), columns);
            Parallel.For(0, rows, p =>
            {
                Complex* zPtr = z.RowPtr(p); float* xCoorPtr = xCoor.RowPtr(p), yCoorPtr = yCoor.RowPtr(p);
                for (int q = 0; q < columns; q++, zPtr++, xCoorPtr++, yCoorPtr++) *zPtr = new(*xCoorPtr, *yCoorPtr);
            });
            return z;
        } // Special for complex
        [MethodImpl(512)] // AggressiveOptimization
        private unsafe MatrixCopy<Complex> Const(Complex _const, int mode = 1)
        {
            switch (mode) // 1: Custom; 2: Writing; 3: Reading
            {
                case 1:
                    return new(HandleMatrix(output =>
                    {
                        Complex* outputPtr = output.RowPtr(), _outputPtr = outputPtr;
                        for (int q = 0; q < columns; q++, outputPtr++) *outputPtr = _const;
                        Parallel.For(1, rows, p => { Unsafe.CopyBlock(output.RowPtr(p), _outputPtr, colBytes); });
                    }));
                case 2: constMtx.Add(new(_const, Const(_const).matrix)); return new(constMtx[^1].matrix, true);
                case 3: return _const.Equals(constMtx[countCst]._const) ? new(constMtx[countCst++].matrix, true) : Const(_const);
                default: ThrowException(); return new(MtxZero()); // Should not have happened
            }
        } // Sensitive
        [MethodImpl(512)] // AggressiveOptimization
        private unsafe Matrix<Complex> Copy(Matrix<Complex> src) => HandleMatrix(dest =>
        {
            void copy(int p, uint colBytes) => Unsafe.CopyBlock(dest.RowPtr(p), src.RowPtr(p), colBytes);
            Parallel.For(0, rowChk, p => { copy(strdInit[p], strdBytes); }); if (res != 0) copy(resInit, resBytes);
        }); // Passing matrices to mutable variables
        [MethodImpl(512)] // AggressiveOptimization
        private unsafe void Negate(Matrix<Complex> _value)
        {
            void negate(int p, int col)
            {
                Complex* _valuePtr = _value.RowPtr(p);
                for (int q = 0; q < col; q++, _valuePtr++) *_valuePtr = -*_valuePtr;
            }
            Parallel.For(0, rowChk, p => { negate(strdInit[p], strd); }); if (res != 0) negate(resInit, res);
        }
        [MethodImpl(512)] // AggressiveOptimization
        private unsafe void Plus(Matrix<Complex> src, Matrix<Complex> dest)
        {
            void plus(int p, int col)
            {
                Complex* destPtr = dest.RowPtr(p), srcPtr = src.RowPtr(p);
                for (int q = 0; q < col; q++, destPtr++, srcPtr++) *destPtr += *srcPtr;
            }
            Parallel.For(0, rowChk, p => { plus(strdInit[p], strd); }); if (res != 0) plus(resInit, res);
        }
        [MethodImpl(512)] // AggressiveOptimization
        private unsafe void Subtract(Matrix<Complex> src, Matrix<Complex> dest)
        {
            void subtract(int p, int col)
            {
                Complex* destPtr = dest.RowPtr(p), srcPtr = src.RowPtr(p);
                for (int q = 0; q < col; q++, destPtr++, srcPtr++) *destPtr -= *srcPtr;
            }
            Parallel.For(0, rowChk, p => { subtract(strdInit[p], strd); }); if (res != 0) subtract(resInit, res);
        }
        [MethodImpl(512)] // AggressiveOptimization
        private unsafe void Multiply(Matrix<Complex> src, Matrix<Complex> dest)
        {
            void multiply(int p, int col)
            {
                Complex* destPtr = dest.RowPtr(p), srcPtr = src.RowPtr(p);
                for (int q = 0; q < col; q++, destPtr++, srcPtr++) *destPtr *= *srcPtr;
            }
            Parallel.For(0, rowChk, p => { multiply(strdInit[p], strd); }); if (res != 0) multiply(resInit, res);
        }
        [MethodImpl(512)] // AggressiveOptimization
        private unsafe void Divide(Matrix<Complex> src, Matrix<Complex> dest)
        {
            void divide(int p, int col)
            {
                Complex* destPtr = dest.RowPtr(p), srcPtr = src.RowPtr(p);
                for (int q = 0; q < col; q++, destPtr++, srcPtr++) *destPtr /= *srcPtr;
            }
            Parallel.For(0, rowChk, p => { divide(strdInit[p], strd); }); if (res != 0) divide(resInit, res);
        }
        [MethodImpl(512)] // AggressiveOptimization
        private unsafe void Power(Matrix<Complex> src, Matrix<Complex> dest)
        {
            void power(int p, int col)
            {
                Complex* destPtr = dest.RowPtr(p), srcPtr = src.RowPtr(p);
                for (int q = 0; q < col; q++, destPtr++, srcPtr++) *destPtr = Complex.Pow(*srcPtr, *destPtr);
            }
            Parallel.For(0, rowChk, p => { power(strdInit[p], strd); }); if (res != 0) power(resInit, res);
        }
        [MethodImpl(512)] // AggressiveOptimization
        private unsafe void FuncSub(Matrix<Complex> _value, Func<Complex, Complex> function)
        {
            void funcSub(int p, int col)
            {
                Complex* _valuePtr = _value.RowPtr(p);
                for (int q = 0; q < col; q++, _valuePtr++) *_valuePtr = function(*_valuePtr);
            }
            Parallel.For(0, rowChk, p => { funcSub(strdInit[p], strd); }); if (res != 0) funcSub(resInit, res);
        }
        #endregion

        #region Assembly
        private Matrix<Complex> CopyMtx(MatrixCopy<Complex> mc) => mc.copy ? Copy(mc.matrix) : mc.matrix;
        private MatrixCopy<Complex> ReturnConst(Complex _const) => Const(_const, !useList ? 1 : !readList ? 2 : 3);
        private MatrixCopy<Complex> Transform(string input) => input[0] switch
        {
            _Z => HandleSolo<Complex>(input, new(z, true)),
            Z_ => HandleSolo<Complex>(input, new(Z, true)),
            '{' => new(buffCocs[Int32.Parse(TryBraNum(input, '{', '}'))], true),
            '[' => braValues[Int32.Parse(TryBraNum(input, '[', ']'))],
            I => HandleSolo<Complex>(input, ReturnConst(Complex.I)), // Special for complex
            E => HandleSolo<Complex>(input, ReturnConst(new(MathF.E))),
            P => HandleSolo<Complex>(input, ReturnConst(new(MathF.PI))),
            G => HandleSolo<Complex>(input, ReturnConst(new(GAMMA))),
            _ => ReturnConst(new(Single.Parse(input)))
        };
        private MatrixCopy<Complex> BreakPower(string input)
        {
            string[] chunks = PrepareBreakPower(input, THRESHOLD);
            Matrix<Complex> tower = CopyMtx(PowerCore(chunks[^1]));
            for (int k = chunks.Length - 2; k >= 0; k--)
            {
                string[] split = SplitByChars(chunks[k], "^"); // Special for "^"
                for (int m = split.Length - 1; m >= 0; m--) Power(Transform(split[m]).matrix, tower);
            }
            return new(tower);
        }
        private MatrixCopy<Complex> PowerCore(string input)
        {
            if (!input.Contains('^')) return Transform(input);
            if (CountChars(input, "^") > THRESHOLD) return BreakPower(input);
            string[] split = SplitByChars(input, "^");
            Matrix<Complex> tower = CopyMtx(Transform(split[^1]));
            for (int k = split.Length - 2; k >= 0; k--) Power(Transform(split[k]).matrix, tower);
            return new(tower);
        }
        private MatrixCopy<Complex> BreakMultiplyDivide(string input)
        {
            var (chunks, signs) = PrepareBreakPSMD(String.Concat('*', input), "*/", THRESHOLD);
            Matrix<Complex> product = CopyMtx(MultiplyDivideCore(TrimStartChar(chunks[0], '*')));
            for (int j = 1; j < chunks.Length; j++)
                Multiply(MultiplyDivideCore(signs[j - 1] == SUB_CHARS[0] ? chunks[j] : String.Concat("1/", chunks[j])).matrix, product);
            return new(product);
        }
        private MatrixCopy<Complex> MultiplyDivideCore(string input)
        {
            if (!ContainsAny(input, "*/")) return PowerCore(input);
            if (CountChars(input, "*/") > THRESHOLD) return BreakMultiplyDivide(input);
            var (split, signs) = GetMultiplyDivideComponents(input);
            Matrix<Complex> product = CopyMtx(PowerCore(split[0]));
            for (int j = 1; j < split.Length; j++)
            {
                Action<Matrix<Complex>, Matrix<Complex>>? op = signs[j - 1] == '*' ? Multiply : signs[j - 1] == '/' ? Divide : null;
                ThrowException(op == null); op(PowerCore(split[j]).matrix, product);
            }
            return new(product);
        }
        private MatrixCopy<Complex> BreakPlusSubtract(string input)
        {
            var (chunks, signs) = PrepareBreakPSMD(input[0] == '-' ? input : String.Concat('+', input), "+-", THRESHOLD);
            Matrix<Complex> sum = CopyMtx(PlusSubtractCore(TrimStartChar(chunks[0], '+')));
            for (int i = 1; i < chunks.Length; i++)
                Plus(PlusSubtractCore(signs[i - 1] == SUB_CHARS[0] ? chunks[i] : String.Concat('-', chunks[i])).matrix, sum);
            return new(sum);
        }
        private MatrixCopy<Complex> PlusSubtractCore(string input)
        {
            if (!ContainsAny(input, "+-")) return MultiplyDivideCore(input);
            if (CountChars(input, "+-") > THRESHOLD) return BreakPlusSubtract(input);
            var (split, signs) = GetPlusSubtractComponents(input);
            Matrix<Complex> sum = CopyMtx(MultiplyDivideCore(split[0])); if (signs[0] == '-') Negate(sum); // Special for "+-"
            for (int i = 1; i < split.Length; i++)
            {
                Action<Matrix<Complex>, Matrix<Complex>>? op = signs[i] == '+' ? Plus : signs[i] == '-' ? Subtract : null;
                ThrowException(op == null); op(MultiplyDivideCore(split[i]).matrix, sum);
            }
            return new(sum);
        }
        private MatrixCopy<Complex> ComputeBraFreePart(string input)
            => Int32.TryParse(input, out int result) ? ReturnConst(new(result)) : PlusSubtractCore(input); // Single.Parse is slower
        private MatrixCopy<Complex> SubCore(string input, int start, MatrixCopy<Complex> bFValue, ref int tagL)
        {
            if (start == 0) return bFValue;
            var (isInverse, mtx, copy) = (IsInverseFunc(start, input), bFValue.matrix, bFValue.copy);
            int handleSub(Func<Complex, Complex> func, int tagL)
            {
                ThrowException(input[start - tagL] != FUNC_HEAD);
                mtx = CopyMtx(bFValue); FuncSub(mtx, func); copy = false; return tagL;
            }
            tagL = input[start - 1] switch
            {
                _S => isInverse.trig ? handleSub(Complex.Asin, 3) : handleSub(Complex.Sin, 2),
                _C => isInverse.trig ? handleSub(Complex.Acos, 3) : handleSub(Complex.Cos, 2),
                _T => isInverse.trig ? handleSub(Complex.Atan, 3) : handleSub(Complex.Tan, 2),
                _H => input[start - 2] switch
                {
                    _S => isInverse.hyper ? handleSub(Complex.Asinh, 4) : handleSub(Complex.Sinh, 3),
                    _C => isInverse.hyper ? handleSub(Complex.Acosh, 4) : handleSub(Complex.Cosh, 3),
                    _T => isInverse.hyper ? handleSub(Complex.Atanh, 4) : handleSub(Complex.Tanh, 3)
                },
                _A => handleSub(c => new(Complex.Modulus(c)), 2), // Converting from float to Complex
                J_ => handleSub(Complex.Conjugate, 2), // Special for complex
                _L => handleSub(Complex.Log, 2),
                E_ => handleSub(Complex.Exp, 2),
                SP => handleSub(Complex.Ei, 3), // Special for complex
                _Q => handleSub(Complex.Sqrt, 2),
                _F_ => handleSub(Complex.Factorial, 2),
                _ => tagL
            };
            return new(mtx, copy);
        }
        private string SeriesSub(string input)
        {
            var (idx, end, split) = PrepareSeriesSub(input);
            (Func<string[], Matrix<Complex>>, int) handleSub(Func<string[], Matrix<Complex>> func, int tagL)
            { ThrowException(input[idx - tagL] != FUNC_HEAD); return (func, tagL); }
            var (braFunc, tagL) = input[idx - 1] switch
            {
                F_ => handleSub(Hypergeometric, 2),
                G_ => handleSub(Gamma, 2),
                B_ => handleSub(Beta, 2),
                _Z_ => handleSub(Zeta, 2),
                S_ => handleSub(Sum, 2),
                P_ => handleSub(Product, 2),
                I_ => handleSub(Iterate, 2),
                J_ => handleSub(Composite, 2),
                K_ => handleSub(Cocoon, 2)
            };
            braValues[countBra] = new(braFunc(split)); // No need to copy
            return ReplaceInput(input, ref countBra, idx - tagL, end);
        }
        public Matrix<Complex> Obtain()
        {
            string input = this.input; // Preserving the original input
            if (input.Contains('('))
            {
                while (input.Contains(UNDERLINE)) input = SeriesSub(input); // Hard to count
                var (length, start, end, tagL) = PrepareLoop(input);
                for (int i = 0; i < length; i++)
                {
                    ResetStartEnd(input, ref start, ref end);
                    braValues[countBra] = SubCore(input, start, ComputeBraFreePart(BraFreePart(input, start, end)), ref tagL);
                    ReplaceInput(ref input, ref countBra, ref start, end, ref tagL);
                }
            }
            return ComputeBraFreePart(input).matrix; // No need to copy
        }
        #endregion
    } /// Computing complex-variable expressions
    public sealed class RealSub : RecoverMultiply
    {
        #region Fields & Constructors
        private readonly uint colBytes, strdBytes, resBytes; // Sizes of chunks
        private readonly int rows, columns, rowChk, strd, res, resInit; // Lengths of chunks
        private readonly int[] rowOffs, strdInit; // rowOffs: for row extraction
        private readonly bool useList; // Whether to use constMtx or not
        private readonly Matrix<float> x, y;
        private readonly Matrix<float>[] buffCocs; // To precompute repetitively used blocks
        private readonly MatrixCopy<float>[] braValues; // To store values between parenthesis pairs
        private readonly List<ConstMatrix<float>> constMtx = [];

        private int countBra, countCst; // countBra: logging parentheses; countCst: logging constants
        private bool readList; // Reading or writing constMtx
        private string input;
        private Matrix<float> X, Y; // For substitution

        public RealSub(string input, Matrix<float>? x, Matrix<float>? y, Matrix<float>? X, Matrix<float>? Y, Matrix<float>[]? buffCocs,
            int rows, int columns, bool useList = false)
        {
            ThrowException(String.IsNullOrEmpty(input));
            this.input = Recover(input, false); braValues = new MatrixCopy<float>[CountChars(this.input, "(")];
            if (x != null) this.x = (Matrix<float>)x; if (y != null) this.y = (Matrix<float>)y;
            if (X != null) this.X = (Matrix<float>)X; if (Y != null) this.Y = (Matrix<float>)Y;
            this.rows = rows; this.columns = columns; this.useList = useList; this.buffCocs = buffCocs;
            Initialize<float>(rows, columns, ref rowChk, ref rowOffs, ref colBytes,
                ref strd, ref strdInit, ref strdBytes, ref res, ref resInit, ref resBytes);
        }
        private RealSub ObtainSub(string input, Matrix<float>? X, Matrix<float>? Y, Matrix<float>[]? buffCocs, bool useList = false)
            => new(input, x, y, X, Y, buffCocs, rows, columns, useList);
        private Matrix<float> ObtainValue(string input) => new RealSub(input, x, y, X, Y, buffCocs, rows, columns).Obtain();
        public static float Obtain(string input, float x = 0) => new RealSub(input, new(x), null, null, null, null, 1, 1).Obtain()[0, 0];
        public static int ToInt(string input) => (int)Obtain(input); // Often bound to MyString.For
        #endregion

        #region Basic Calculations
        public static float Factorial(float n) => n < 0 ? Single.NaN : n == 0 ? 1 : n * Factorial(n - 1);
        private static float Mod(float a, float n) => n != 0 ? a % MathF.Abs(n) : Single.NaN;
        private static float Combination(float n, float r)
            => (n == r || r == 0) ? 1 : (r > n && n >= 0 || 0 > r && r > n || n >= 0 && 0 > r) ? 0 : n > 0 ?
            Combination(n - 1, r - 1) + Combination(n - 1, r) : r > 0 ?
            Combination(n + 1, r) - Combination(n, r - 1) :
            Combination(n + 1, r + 1) - Combination(n, r + 1); // Generalized Pascal's triangle
        private static float Permutation(float n, float r) => r < 0 ? 0 : r == 0 ? 1 : (n - r + 1) * Permutation(n, r - 1);
        //
        private unsafe Matrix<float> ProcessMCP(string[] split, Func<float, float, float> function)
        {
            ThrowInvalidLengths(split, [2]);
            return HandleMatrix(output =>
            {
                Matrix<float> input1 = ObtainValue(split[0]), input2 = ObtainValue(split[1]);
                void processMCP(int p, int col)
                {
                    float* input1Ptr = input1.RowPtr(p), input2Ptr = input2.RowPtr(p), outputPtr = output.RowPtr(p);
                    for (int q = 0; q < col; q++, outputPtr++, input1Ptr++, input2Ptr++) *outputPtr = function(*input1Ptr, *input2Ptr);
                }
                Parallel.For(0, rowChk, p => { processMCP(strdInit[p], strd); }); if (res != 0) processMCP(resInit, res);
            });
        }
        private unsafe Matrix<float> ProcessMinMax(string[] split, Func<float[], float> function)
        {
            Matrix<float>[] _value = new Matrix<float>[split.Length];
            for (int i = 0; i < split.Length; i++) _value[i] = ObtainValue(split[i]);
            return HandleMatrix(output =>
            {
                void processMinMax(int p, int col)
                {
                    Span<float> minMax = stackalloc float[split.Length];
                    float* outputPtr = output.RowPtr(p);
                    for (int q = 0; q < col; q++, outputPtr++)
                    {
                        for (int i = 0; i < split.Length; i++) minMax[i] = _value[i][p, q];
                        *outputPtr = function(minMax.ToArray());
                    }
                }
                Parallel.For(0, rowChk, p => { processMinMax(strdInit[p], strd); }); if (res != 0) processMinMax(resInit, res);
            });
        }
        //
        private Matrix<float> Mod(string[] split) => ProcessMCP(split, Mod);
        private Matrix<float> Combination(string[] split) => ProcessMCP(split, (n, r) => Combination(MathF.Floor(n), MathF.Floor(r)));
        private Matrix<float> Permutation(string[] split) => ProcessMCP(split, (n, r) => Permutation(MathF.Floor(n), MathF.Floor(r)));
        private Matrix<float> Max(string[] split) => ProcessMinMax(split, _value => _value.Max());
        private Matrix<float> Min(string[] split) => ProcessMinMax(split, _value => _value.Min());
        #endregion // Special for real

        #region Additional Calculations
        private unsafe Matrix<float> Hypergeometric(string[] split)
        {
            int n = ThrowReturnLengths(split, 4, 100);
            return HandleMatrix(sum =>
            {
                Matrix<float> obtain(int index) => ObtainValue(split[index]);
                Matrix<float> product = MtxOne(), _a = obtain(0), _b = obtain(1), _c = obtain(2), initial = obtain(3);
                void hypergeometric(int p, int col)
                {
                    float* productPtr = product.RowPtr(p), sumPtr = sum.RowPtr(p),
                        _aPtr = _a.RowPtr(p), _bPtr = _b.RowPtr(p), _cPtr = _c.RowPtr(p), initialPtr = initial.RowPtr(p);
                    for (int q = 0; q < col; q++, productPtr++, sumPtr++, _aPtr++, _bPtr++, _cPtr++, initialPtr++)
                    {
                        for (int i = 0; i <= n; i++)
                        {
                            if (i != 0) *productPtr *= *initialPtr * (i - 1 + *_aPtr) * (i - 1 + *_bPtr) / (i - 1 + *_cPtr) / i;
                            *sumPtr += *productPtr;
                        }
                    }
                }
                Parallel.For(0, rowChk, p => { hypergeometric(strdInit[p], strd); }); if (res != 0) hypergeometric(resInit, res);
            });
        } // Reference: https://en.wikipedia.org/wiki/Hypergeometric_function
        private unsafe Matrix<float> Gamma(string[] split)
        {
            int n = ThrowReturnLengths(split, 1, 100);
            return HandleMatrix(output =>
            {
                Matrix<float> product = MtxOne(), initial = ObtainValue(split[0]);
                void gamma(int p, int col)
                {
                    float* productPtr = product.RowPtr(p), initialPtr = initial.RowPtr(p), outputPtr = output.RowPtr(p);
                    for (int q = 0; q < col; q++, productPtr++, initialPtr++, outputPtr++)
                    {
                        for (int i = 1; i <= n; i++) { float temp = *initialPtr / i; *productPtr *= MathF.Exp(temp) / (1 + temp); }
                        *outputPtr = *productPtr * MathF.Exp(-*initialPtr * GAMMA) / *initialPtr;
                    }
                }
                Parallel.For(0, rowChk, p => { gamma(strdInit[p], strd); }); if (res != 0) gamma(resInit, res);
            });
        } // Reference: https://en.wikipedia.org/wiki/Gamma_function
        private unsafe Matrix<float> Beta(string[] split)
        {
            int n = ThrowReturnLengths(split, 2, 100);
            return HandleMatrix(output =>
            {
                Matrix<float> obtain(int index) => ObtainValue(split[index]);
                Matrix<float> product = MtxOne(), input1 = obtain(0), input2 = obtain(1);
                void beta(int p, int col)
                {
                    float* productPtr = product.RowPtr(p), input1Ptr = input1.RowPtr(p),
                        input2Ptr = input2.RowPtr(p), outputPtr = output.RowPtr(p);
                    for (int q = 0; q < col; q++, productPtr++, input1Ptr++, input2Ptr++, outputPtr++)
                    {
                        for (int i = 1; i <= n; i++) *productPtr *= 1 + *input1Ptr * *input2Ptr / ((i + *input1Ptr + *input2Ptr) * i);
                        *outputPtr = (*input1Ptr + *input2Ptr) / (*input1Ptr * *input2Ptr) / *productPtr;
                    }
                }
                Parallel.For(0, rowChk, p => { beta(strdInit[p], strd); }); if (res != 0) beta(resInit, res);
            });
        } // Reference: https://en.wikipedia.org/wiki/Beta_function
        private unsafe Matrix<float> Zeta(string[] split)
        {
            int n = ThrowReturnLengths(split, 1, 50);
            return HandleMatrix(_sum =>
            {
                Matrix<float> sum = MtxZero(), coeff = MtxOne(), _coeff = MtxOne(), initial = ObtainValue(split[0]);
                void zeta(int p, int col)
                {
                    float* sumPtr = sum.RowPtr(p), _sumPtr = _sum.RowPtr(p),
                        coeffPtr = coeff.RowPtr(p), _coeffPtr = _coeff.RowPtr(p), initialPtr = initial.RowPtr(p);
                    for (int q = 0; q < col; q++, sumPtr++, _sumPtr++, coeffPtr++, _coeffPtr++, initialPtr++)
                    {
                        for (int i = 0; i <= n; i++)
                        {
                            *coeffPtr *= 0.5f; *_coeffPtr = 1; *sumPtr = 0;
                            for (int j = 0; j <= i; j++)
                            {
                                *sumPtr += *_coeffPtr * MathF.Pow(j + 1, -*initialPtr);
                                *_coeffPtr *= (float)(j - i) / (float)(j + 1); // (float) is not redundant
                            }
                            *sumPtr *= *coeffPtr; *_sumPtr += *sumPtr;
                        }
                        *_sumPtr /= 1 - MathF.Pow(2, 1 - *initialPtr);
                    }
                }
                Parallel.For(0, rowChk, p => { zeta(strdInit[p], strd); }); if (res != 0) zeta(resInit, res);
            });
        } // Reference: https://en.wikipedia.org/wiki/Riemann_zeta_function
        //
        private Matrix<float> ProcessSPI(string[] split, int validLength, Matrix<float> initMtx, Action<RealSub> action)
        {
            ThrowInvalidLengths(split, [validLength]);
            RealSub buffer = ObtainSub(ReplaceLoop(split, 0, validLength - 3, 0), initMtx, null, buffCocs, true);

            void resetCount() => buffer.countBra = buffer.countCst = 0;
            _ = buffer.Obtain(); resetCount(); buffer.readList = true; // To precompute constMtx

            For(ToInt(split[validLength - 2]), ToInt(split[validLength - 1]), i =>
            { buffer.input = Recover(ReplaceLoop(split, 0, validLength - 3, i), false); resetCount(); action(buffer); });
            return buffer.X;
        } // Meticulously optimized
        private Matrix<float> Sum(string[] split) => ProcessSPI(split, 4, MtxZero(), b => { Plus(b.Obtain(), b.X); });
        private Matrix<float> Product(string[] split) => ProcessSPI(split, 4, MtxOne(), b => { Multiply(b.Obtain(), b.X); });
        private Matrix<float> Iterate1(string[] split) => ProcessSPI(split, 5, ObtainValue(split[1]), b => { b.X = b.Obtain(); });
        private Matrix<float> Iterate2(string[] split)
        {
            ThrowInvalidLengths(split, [8]);
            RealSub obtain(int idx) => ObtainSub(ReplaceLoop(split, idx, 4, 0), ObtainValue(split[2]), ObtainValue(split[3]), buffCocs, true);
            RealSub buffer1 = obtain(0), buffer2 = obtain(1); Matrix<float> temp1, temp2;

            void resetCount() => buffer1.countBra = buffer1.countCst = buffer2.countBra = buffer2.countCst = 0;
            _ = buffer1.Obtain(); _ = buffer2.Obtain(); resetCount(); buffer1.readList = buffer2.readList = true; // To precompute constMtx

            string obtainInput(int s, int i) => Recover(ReplaceLoop(split, s, 4, i), false);
            For(ToInt(split[5]), ToInt(split[6]), i =>
            {
                buffer1.input = obtainInput(0, i); buffer2.input = obtainInput(1, i);
                resetCount(); temp1 = buffer1.Obtain(); temp2 = buffer2.Obtain(); // Necessary
                buffer1.X = buffer2.X = temp1; buffer1.Y = buffer2.Y = temp2;
            });
            return ChooseMode(split[^1], buffer1.X, buffer1.Y, rowOffs, columns); // Or, alternatively, buffer2
        } // Special for real
        //
        private Matrix<float> Composite1(string[] split)
        {
            Matrix<float> _value = ObtainValue(split[0]);
            for (int i = 1; i < split.Length; i++) _value = ObtainSub(split[i], _value, null, buffCocs).Obtain();
            return _value;
        }
        private Matrix<float> Composite2(string[] split)
        {
            ThrowException(Int32.IsEvenInteger(split.Length));
            Matrix<float> value1 = ObtainValue(split[0]), value2 = ObtainValue(split[1]), temp1, temp2;
            for (int i = 0, j = 2; i < split.Length / 2 - 1; i++)
            {
                temp1 = value1; temp2 = value2; // Necessary
                Matrix<float> obtainValue() => ObtainSub(split[j++], temp1, temp2, buffCocs).Obtain();
                value1 = obtainValue(); value2 = obtainValue(); // Even and odd terms respectively
            }
            return ChooseMode(split[^1], value1, value2, rowOffs, columns);
        } // Special for real
        private Matrix<float> Cocoon(string[] split)
        {
            RealSub body = ObtainSub(split[0], null, null, new Matrix<float>[split.Length - 1]);
            for (int i = 1; i < split.Length; i++) body.buffCocs[i - 1] = ObtainValue(split[i]);
            return body.Obtain();
        } // For shallow and complicated composites
        #endregion

        #region Elements
        private Matrix<float> MtxZero() => new(rowOffs, columns);
        private Matrix<float> MtxOne() => Const(1).matrix;
        private Matrix<float> HandleMatrix(Action<Matrix<float>> action) { Matrix<float> mtx = MtxZero(); action(mtx); return mtx; }
        [MethodImpl(512)] // AggressiveOptimization
        private unsafe MatrixCopy<float> Const(float _const, int mode = 1)
        {
            switch (mode) // 1: Custom; 2: Writing; 3: Reading
            {
                case 1:
                    return new(HandleMatrix(output =>
                    {
                        float* outputPtr = output.RowPtr(), _outputPtr = outputPtr;
                        for (int q = 0; q < columns; q++, outputPtr++) *outputPtr = _const;
                        Parallel.For(1, rows, p => { Unsafe.CopyBlock(output.RowPtr(p), _outputPtr, colBytes); });
                    }));
                case 2: constMtx.Add(new(_const, Const(_const).matrix)); return new(constMtx[^1].matrix, true);
                case 3: return _const.Equals(constMtx[countCst]._const) ? new(constMtx[countCst++].matrix, true) : Const(_const);
                default: ThrowException(); return new(MtxZero()); // Should not have happened
            }
        } // Sensitive
        [MethodImpl(512)] // AggressiveOptimization
        private unsafe Matrix<float> Copy(Matrix<float> src) => HandleMatrix(dest =>
        {
            void copy(int p, uint colBytes) => Unsafe.CopyBlock(dest.RowPtr(p), src.RowPtr(p), colBytes);
            Parallel.For(0, rowChk, p => { copy(strdInit[p], strdBytes); }); if (res != 0) copy(resInit, resBytes);
        }); // Passing matrices to mutable variables
        [MethodImpl(512)] // AggressiveOptimization
        private unsafe void Negate(Matrix<float> _value)
        {
            void negate(int p, int col)
            {
                float* _valuePtr = _value.RowPtr(p);
                for (int q = 0; q < col; q++, _valuePtr++) *_valuePtr = -*_valuePtr;
            }
            Parallel.For(0, rowChk, p => { negate(strdInit[p], strd); }); if (res != 0) negate(resInit, res);
        }
        [MethodImpl(512)] // AggressiveOptimization
        private unsafe void Plus(Matrix<float> src, Matrix<float> dest)
        {
            void plus(int p, int col)
            {
                float* destPtr = dest.RowPtr(p), srcPtr = src.RowPtr(p);
                for (int q = 0; q < col; q++, destPtr++, srcPtr++) *destPtr += *srcPtr;
            }
            Parallel.For(0, rowChk, p => { plus(strdInit[p], strd); }); if (res != 0) plus(resInit, res);
        }
        [MethodImpl(512)] // AggressiveOptimization
        private unsafe void Subtract(Matrix<float> src, Matrix<float> dest)
        {
            void subtract(int p, int col)
            {
                float* destPtr = dest.RowPtr(p), srcPtr = src.RowPtr(p);
                for (int q = 0; q < col; q++, destPtr++, srcPtr++) *destPtr -= *srcPtr;
            }
            Parallel.For(0, rowChk, p => { subtract(strdInit[p], strd); }); if (res != 0) subtract(resInit, res);
        }
        [MethodImpl(512)] // AggressiveOptimization
        private unsafe void Multiply(Matrix<float> src, Matrix<float> dest)
        {
            void multiply(int p, int col)
            {
                float* destPtr = dest.RowPtr(p), srcPtr = src.RowPtr(p);
                for (int q = 0; q < col; q++, destPtr++, srcPtr++) *destPtr *= *srcPtr;
            }
            Parallel.For(0, rowChk, p => { multiply(strdInit[p], strd); }); if (res != 0) multiply(resInit, res);
        }
        [MethodImpl(512)] // AggressiveOptimization
        private unsafe void Divide(Matrix<float> src, Matrix<float> dest)
        {
            void divide(int p, int col)
            {
                float* destPtr = dest.RowPtr(p), srcPtr = src.RowPtr(p);
                for (int q = 0; q < col; q++, destPtr++, srcPtr++) *destPtr /= *srcPtr;
            }
            Parallel.For(0, rowChk, p => { divide(strdInit[p], strd); }); if (res != 0) divide(resInit, res);
        }
        [MethodImpl(512)] // AggressiveOptimization
        private unsafe void Power(Matrix<float> src, Matrix<float> dest)
        {
            void power(int p, int col)
            {
                float* destPtr = dest.RowPtr(p), srcPtr = src.RowPtr(p);
                for (int q = 0; q < col; q++, destPtr++, srcPtr++) *destPtr = MathF.Pow(*srcPtr, *destPtr);
            }
            Parallel.For(0, rowChk, p => { power(strdInit[p], strd); }); if (res != 0) power(resInit, res);
        }
        [MethodImpl(512)] // AggressiveOptimization
        private unsafe void FuncSub(Matrix<float> _value, Func<float, float> function)
        {
            void funcSub(int p, int col)
            {
                float* _valuePtr = _value.RowPtr(p);
                for (int q = 0; q < col; q++, _valuePtr++) *_valuePtr = function(*_valuePtr);
            }
            Parallel.For(0, rowChk, p => { funcSub(strdInit[p], strd); }); if (res != 0) funcSub(resInit, res);
        }
        #endregion

        #region Assembly
        private Matrix<float> CopyMtx(MatrixCopy<float> mc) => mc.copy ? Copy(mc.matrix) : mc.matrix;
        private MatrixCopy<float> ReturnConst(float _const) => Const(_const, !useList ? 1 : !readList ? 2 : 3);
        private MatrixCopy<float> Transform(string input) => input[0] switch
        {
            _X => HandleSolo<float>(input, new(x, true)),
            _Y => HandleSolo<float>(input, new(y, true)),
            X_ => HandleSolo<float>(input, new(X, true)),
            Y_ => HandleSolo<float>(input, new(Y, true)),
            '{' => new(buffCocs[Int32.Parse(TryBraNum(input, '{', '}'))], true),
            '[' => braValues[Int32.Parse(TryBraNum(input, '[', ']'))],
            E => HandleSolo<float>(input, ReturnConst(MathF.E)),
            P => HandleSolo<float>(input, ReturnConst(MathF.PI)),
            G => HandleSolo<float>(input, ReturnConst(GAMMA)),
            _ => ReturnConst(Single.Parse(input))
        };
        private MatrixCopy<float> BreakPower(string input)
        {
            string[] chunks = PrepareBreakPower(input, THRESHOLD);
            Matrix<float> tower = CopyMtx(PowerCore(chunks[^1]));
            for (int k = chunks.Length - 2; k >= 0; k--)
            {
                string[] split = SplitByChars(chunks[k], "^"); // Special for "^"
                for (int m = split.Length - 1; m >= 0; m--) Power(Transform(split[m]).matrix, tower);
            }
            return new(tower);
        }
        private MatrixCopy<float> PowerCore(string input)
        {
            if (!input.Contains('^')) return Transform(input);
            if (CountChars(input, "^") > THRESHOLD) return BreakPower(input);
            string[] split = SplitByChars(input, "^");
            Matrix<float> tower = CopyMtx(Transform(split[^1]));
            for (int k = split.Length - 2; k >= 0; k--) Power(Transform(split[k]).matrix, tower);
            return new(tower);
        }
        private MatrixCopy<float> BreakMultiplyDivide(string input)
        {
            var (chunks, signs) = PrepareBreakPSMD(String.Concat('*', input), "*/", THRESHOLD);
            Matrix<float> product = CopyMtx(MultiplyDivideCore(TrimStartChar(chunks[0], '*')));
            for (int j = 1; j < chunks.Length; j++)
                Multiply(MultiplyDivideCore(signs[j - 1] == SUB_CHARS[0] ? chunks[j] : String.Concat("1/", chunks[j])).matrix, product);
            return new(product);
        }
        private MatrixCopy<float> MultiplyDivideCore(string input)
        {
            if (!ContainsAny(input, "*/")) return PowerCore(input);
            if (CountChars(input, "*/") > THRESHOLD) return BreakMultiplyDivide(input);
            var (split, signs) = GetMultiplyDivideComponents(input);
            Matrix<float> product = CopyMtx(PowerCore(split[0]));
            for (int j = 1; j < split.Length; j++)
            {
                Action<Matrix<float>, Matrix<float>>? op = signs[j - 1] == '*' ? Multiply : signs[j - 1] == '/' ? Divide : null;
                ThrowException(op == null); op(PowerCore(split[j]).matrix, product);
            }
            return new(product);
        }
        private MatrixCopy<float> BreakPlusSubtract(string input)
        {
            var (chunks, signs) = PrepareBreakPSMD(input[0] == '-' ? input : String.Concat('+', input), "+-", THRESHOLD);
            Matrix<float> sum = CopyMtx(PlusSubtractCore(TrimStartChar(chunks[0], '+')));
            for (int i = 1; i < chunks.Length; i++)
                Plus(PlusSubtractCore(signs[i - 1] == SUB_CHARS[0] ? chunks[i] : String.Concat('-', chunks[i])).matrix, sum);
            return new(sum);
        }
        private MatrixCopy<float> PlusSubtractCore(string input)
        {
            if (!ContainsAny(input, "+-")) return MultiplyDivideCore(input);
            if (CountChars(input, "+-") > THRESHOLD) return BreakPlusSubtract(input);
            var (split, signs) = GetPlusSubtractComponents(input);
            Matrix<float> sum = CopyMtx(MultiplyDivideCore(split[0])); if (signs[0] == '-') Negate(sum); // Special for "+-"
            for (int i = 1; i < split.Length; i++)
            {
                Action<Matrix<float>, Matrix<float>>? op = signs[i] == '+' ? Plus : signs[i] == '-' ? Subtract : null;
                ThrowException(op == null); op(MultiplyDivideCore(split[i]).matrix, sum);
            }
            return new(sum);
        }
        private MatrixCopy<float> ComputeBraFreePart(string input)
           => Int32.TryParse(input, out int result) ? ReturnConst(result) : PlusSubtractCore(input); // Single.Parse is slower
        private MatrixCopy<float> SubCore(string input, int start, MatrixCopy<float> bFValue, ref int tagL)
        {
            if (start == 0) return bFValue;
            var (isInverse, mtx, copy) = (IsInverseFunc(start, input), bFValue.matrix, bFValue.copy);
            int handleSub(Func<float, float> func, int tagL)
            {
                ThrowException(input[start - tagL] != FUNC_HEAD);
                mtx = CopyMtx(bFValue); FuncSub(mtx, func); copy = false; return tagL;
            }
            tagL = input[start - 1] switch
            {
                _S => isInverse.trig ? handleSub(MathF.Asin, 3) : handleSub(MathF.Sin, 2),
                _C => isInverse.trig ? handleSub(MathF.Acos, 3) : handleSub(MathF.Cos, 2),
                _T => isInverse.trig ? handleSub(MathF.Atan, 3) : handleSub(MathF.Tan, 2),
                _H => input[start - 2] switch
                {
                    _S => isInverse.hyper ? handleSub(MathF.Asinh, 4) : handleSub(MathF.Sinh, 3),
                    _C => isInverse.hyper ? handleSub(MathF.Acosh, 4) : handleSub(MathF.Cosh, 3),
                    _T => isInverse.hyper ? handleSub(MathF.Atanh, 4) : handleSub(MathF.Tanh, 3)
                },
                _A => handleSub(MathF.Abs, 2),
                _L => handleSub(MathF.Log, 2),
                E_ => handleSub(MathF.Exp, 2),
                _Q => handleSub(MathF.Sqrt, 2),
                _F_ => handleSub(r => Factorial(MathF.Floor(r)), 2),
                _D_ => input[start - 2] switch
                {
                    _F => handleSub(MathF.Floor, 3),
                    _C => handleSub(MathF.Ceiling, 3),
                    _R => handleSub(MathF.Round, 3),
                    _S => handleSub(r => MathF.Sign(r), 3) // Automatic conversion from int to float
                }, // Special for real
                _ => tagL
            };
            return new(mtx, copy);
        }
        private string SeriesSub(string input)
        {
            var (idx, end, split) = PrepareSeriesSub(input);
            (Func<string[], Matrix<float>>, int) handleSub(Func<string[], Matrix<float>> func, int tagL)
            { ThrowException(input[idx - tagL] != FUNC_HEAD); return (func, tagL); }
            var (braFunc, tagL) = input[idx - 1] switch
            {
                M_ => handleSub(Mod, 2),
                C_ => handleSub(Combination, 2),
                A_ => handleSub(Permutation, 2),
                MAX => handleSub(Max, 2),
                MIN => handleSub(Min, 2),
                F_ => handleSub(Hypergeometric, 2),
                G_ => handleSub(Gamma, 2),
                B_ => handleSub(Beta, 2),
                _Z_ => handleSub(Zeta, 2),
                S_ => handleSub(Sum, 2),
                P_ => handleSub(Product, 2),
                I_ => input[idx - 2] switch { MODE_1 => handleSub(Iterate1, 3), MODE_2 => handleSub(Iterate2, 3) },
                J_ => input[idx - 2] switch { MODE_1 => handleSub(Composite1, 3), MODE_2 => handleSub(Composite2, 3) },
                K_ => handleSub(Cocoon, 2)
            };
            braValues[countBra] = new(braFunc(split)); // No need to copy
            return ReplaceInput(input, ref countBra, idx - tagL, end);
        }
        public Matrix<float> Obtain()
        {
            string input = this.input; // Preserving the original input
            if (input.Contains('('))
            {
                while (input.Contains(UNDERLINE)) input = SeriesSub(input); // Hard to count
                var (length, start, end, tagL) = PrepareLoop(input);
                for (int i = 0; i < length; i++)
                {
                    ResetStartEnd(input, ref start, ref end);
                    braValues[countBra] = SubCore(input, start, ComputeBraFreePart(BraFreePart(input, start, end)), ref tagL);
                    ReplaceInput(ref input, ref countBra, ref start, end, ref tagL);
                }
            }
            return ComputeBraFreePart(input).matrix; // No need to copy
        }
        #endregion
    } /// Computing real-variable expressions

    /// <summary>
    /// STRUCTURE SECTION
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public readonly struct Complex // Manually inlined to reduce overhead
    {
        public readonly float real, imaginary;
        public static readonly Complex ZERO = new(0), ONE = new(1), I = new(0, 1);
        [MethodImpl(256)] // AggressiveInlining
        public Complex(float real, float imaginary = 0) { this.real = real; this.imaginary = imaginary; } // Do not use primary constructor

        #region Operators
        [MethodImpl(256)] // AggressiveInlining
        public static Complex operator -(Complex c) => new(-c.real, -c.imaginary);
        [MethodImpl(256)] // AggressiveInlining
        public static Complex operator +(float f, Complex c) => new(f + c.real, c.imaginary);
        [MethodImpl(256)] // AggressiveInlining
        public static Complex operator +(Complex c1, Complex c2) => new(c1.real + c2.real, c1.imaginary + c2.imaginary);
        [MethodImpl(256)] // AggressiveInlining
        public static Complex operator -(float f, Complex c) => new(f - c.real, -c.imaginary);
        [MethodImpl(256)] // AggressiveInlining
        public static Complex operator -(Complex c1, Complex c2) => new(c1.real - c2.real, c1.imaginary - c2.imaginary);
        [MethodImpl(256)] // AggressiveInlining
        public static Complex operator *(Complex c, float f) => new(f * c.real, f * c.imaginary);
        [MethodImpl(256)] // AggressiveInlining
        public static Complex operator *(Complex c1, Complex c2)
        {
            float re1 = c1.real, im1 = c1.imaginary, re2 = c2.real, im2 = c2.imaginary;
            return new(re1 * re2 - im1 * im2, re1 * im2 + im1 * re2);
        }
        [MethodImpl(256)] // AggressiveInlining
        public static Complex operator /(float f, Complex c)
        { float re = c.real, im = c.imaginary, mod = f / (re * re + im * im); return new(mod * re, -mod * im); }
        [MethodImpl(256)] // AggressiveInlining
        public static Complex operator /(Complex c, float f) => c * (1 / f);
        [MethodImpl(256)] // AggressiveInlining
        public static Complex operator /(Complex c1, Complex c2)
        {
            float re1 = c1.real, im1 = c1.imaginary, re2 = c2.real, im2 = c2.imaginary, modSquare = re2 * re2 + im2 * im2;
            return new((re1 * re2 + im1 * im2) / modSquare, (im1 * re2 - re1 * im2) / modSquare);
        }
        #endregion

        #region Elementary Functions
        public static Complex Pow(float f, Complex c)
        {
            if (f == 0) return ZERO; // Necessary apriori checking
            var (mod, unit) = (MathF.Pow(f, c.real), MathF.SinCos(MathF.Log(f) * c.imaginary));
            return new(mod * unit.Cos, mod * unit.Sin);
        }
        public static Complex Pow(Complex c, float f)
        {
            float re = c.real, im = c.imaginary; if (re == 0 && im == 0) return ZERO; // Necessary apriori checking
            var (mod, unit) = (MathF.Pow(re * re + im * im, f / 2), MathF.SinCos(f * MathF.Atan2(im, re)));
            return new(mod * unit.Cos, mod * unit.Sin);
        }
        public static Complex Pow(Complex c1, Complex c2)
        {
            float re = c1.real, im = c1.imaginary; if (re == 0 && im == 0) return ZERO; // Necessary apriori checking
            Complex c = c2 * new Complex(MathF.Log(re * re + im * im) / 2, MathF.Atan2(im, re));
            var (mod, unit) = (MathF.Exp(c.real), MathF.SinCos(c.imaginary));
            return new(mod * unit.Cos, mod * unit.Sin);
        }
        public static Complex Log(Complex c)
        {
            float re = c.real, im = c.imaginary;
            return new(MathF.Log(re * re + im * im) / 2, MathF.Atan2(im, re));
        }
        public static Complex Exp(Complex c)
        {
            var (mod, unit) = (MathF.Exp(c.real), MathF.SinCos(c.imaginary));
            return new(mod * unit.Cos, mod * unit.Sin);
        }
        public static Complex Ei(Complex c)
        {
            var (mod, unit) = (MathF.Exp(-MathF.Tau * c.imaginary), MathF.SinCos(MathF.Tau * c.real));
            return new(mod * unit.Cos, mod * unit.Sin);
        } // Often used in analytic number theory, represented by 'q'
        //
        public static Complex Sin(Complex c)
        {
            var (mod, unit) = (MathF.Exp(-c.imaginary), MathF.SinCos(c.real));
            Complex _c = new(mod * unit.Cos, mod * unit.Sin); _c -= 1 / _c; return new(_c.imaginary / 2, -_c.real / 2);
        }
        public static Complex Cos(Complex c)
        {
            var (mod, unit) = (MathF.Exp(-c.imaginary), MathF.SinCos(c.real));
            Complex _c = new(mod * unit.Cos, mod * unit.Sin); _c += 1 / _c; return new(_c.real / 2, _c.imaginary / 2);
        }
        public static Complex Tan(Complex c)
        {
            var (mod, unit) = (MathF.Exp(-c.imaginary - c.imaginary), MathF.SinCos(c.real + c.real));
            Complex _c = 2 / new Complex(1 + mod * unit.Cos, mod * unit.Sin); return new(-_c.imaginary, _c.real - 1);
        }
        public static Complex Asin(Complex c)
        {
            Complex _c = new Complex(-c.imaginary, c.real) + Pow(1 - c * c, 0.5f); float re = _c.real, im = _c.imaginary;
            return new(MathF.Atan2(im, re), -MathF.Log(re * re + im * im) / 2);
        }
        public static Complex Acos(Complex c)
        {
            Complex _c = new Complex(-c.imaginary, c.real) + Pow(1 - c * c, 0.5f); float re = _c.real, im = _c.imaginary;
            return new(MathF.PI / 2 - MathF.Atan2(im, re), MathF.Log(re * re + im * im) / 2);
        } // Wolfram convention: https://mathworld.wolfram.com/InverseCosine.html
        public static Complex Atan(Complex c)
        {
            Complex _c = 2 / new Complex(1 + c.imaginary, -c.real); float re = _c.real - 1, im = _c.imaginary;
            return new(MathF.Atan2(im, re) / 2, -MathF.Log(re * re + im * im) / 4);
        }
        //
        public static Complex Sinh(Complex c)
        {
            var (mod, unit) = (MathF.Exp(c.real), MathF.SinCos(c.imaginary));
            Complex _c = new(mod * unit.Cos, mod * unit.Sin); _c -= 1 / _c; return new(_c.real / 2, _c.imaginary / 2);
        }
        public static Complex Cosh(Complex c)
        {
            var (mod, unit) = (MathF.Exp(c.real), MathF.SinCos(c.imaginary));
            Complex _c = new(mod * unit.Cos, mod * unit.Sin); _c += 1 / _c; return new(_c.real / 2, _c.imaginary / 2);
        }
        public static Complex Tanh(Complex c)
        {
            var (mod, unit) = (MathF.Exp(c.real + c.real), MathF.SinCos(c.imaginary + c.imaginary));
            Complex _c = new(1 + mod * unit.Cos, mod * unit.Sin); return 1 - 2 / _c;
        }
        public static Complex Asinh(Complex c)
        {
            Complex _c = c + Pow(1 + c * c, 0.5f); float re = _c.real, im = _c.imaginary;
            return new(MathF.Log(re * re + im * im) / 2, MathF.Atan2(im, re));
        }
        public static Complex Acosh(Complex c)
        {
            Complex _c = c + Pow(1 + c, 0.5f) * Pow(-1 + c, 0.5f); float re = _c.real, im = _c.imaginary;
            return new(MathF.Log(re * re + im * im) / 2, MathF.Atan2(im, re));
        } // Wolfram convention: https://mathworld.wolfram.com/InverseHyperbolicCosine.html
        public static Complex Atanh(Complex c)
        {
            Complex _c = 2 / new Complex(1 - c.real, -c.imaginary); float re = _c.real - 1, im = _c.imaginary;
            return new(MathF.Log(re * re + im * im) / 4, MathF.Atan2(im, re) / 2);
        }
        //
        public static Complex Sqrt(Complex c) => Pow(c, 0.5f);
        public static float Modulus(float x, float y) => Modulus(new(x, y));
        public static float Modulus(Complex c) => MathF.Sqrt(c.real * c.real + c.imaginary * c.imaginary);
        public static Complex Conjugate(Complex c) => new(c.real, -c.imaginary);
        public static Complex Factorial(Complex c) => new(RealSub.Factorial(c.real));
        #endregion
    } /// Optimized float-entried complex numbers
    public readonly struct Matrix<TEntry>
    {
        private readonly TEntry[] matrix;
        private readonly int[] rowOffs; // For row extraction

        [MethodImpl(256)] // AggressiveInlining
        public Matrix(int[] rowOffs, int columns) { this.rowOffs = rowOffs; matrix = new TEntry[rowOffs[^1] + columns]; }
        [MethodImpl(256)] // AggressiveInlining
        public Matrix(TEntry x) { matrix = [x]; rowOffs = [0]; } // Special for real
        public TEntry this[int row, int column]
        {
            [MethodImpl(256)] // AggressiveInlining
            get => Access(matrix, Access(rowOffs, row) + column);
            [MethodImpl(256)] // AggressiveInlining
            set => Access(matrix, Access(rowOffs, row) + column) = value;
        }
        [MethodImpl(256)] // AggressiveInlining
        public readonly unsafe TEntry* RowPtr(int row = 0) { fixed (TEntry* ptr = &Access(matrix, Access(rowOffs, row))) { return ptr; } }
        [MethodImpl(256)] // AggressiveInlining
        private static ref T Access<T>(T[] array, int index) => ref Unsafe.Add(ref MemoryMarshal.GetArrayDataReference(array), index);
    } /// Optimized real or complex matrices
    public readonly struct MatrixCopy<TEntry>(Matrix<TEntry> matrix, bool copy = false)
    {
        public readonly Matrix<TEntry> matrix = matrix;
        public readonly bool copy = copy;
    } /// Matrices to be copied or not
    public readonly struct ConstMatrix<TEntry>(TEntry _const, Matrix<TEntry> matrix)
    {
        public readonly TEntry _const = _const;
        public readonly Matrix<TEntry> matrix = matrix;
    } /// Constant matrices for recycling
}
