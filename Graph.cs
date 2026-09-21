using System.Drawing.Imaging;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

using MathR = System.Math;
using Real = System.Double;

namespace Fraljiculator;

/// <summary>
/// DISPLAY SECTION
/// </summary>
public partial class Graph : Form
{
    // 1. PREPARATIONS
    #region Fields
    private static DateTime TimeNow = new();
    private static TimeSpan TimeCount = new();
    private static System.Windows.Forms.Timer WaitTimer, DisplayTimer;
    private static TextBox? paren_tbx; // The actual on-screen textbox being temporarily modified
    private TextBox[] math_inputs, detail_inputs, read_only_inputs, clear_outputs;
    private (TextBox tbx, string value)[] parameter_defaults, reset_inputs;
    private Button[] graph_buttons;
    private (CheckBox cbx, bool value)[] default_checks;
    private Label[] reset_labels;
    private ComboBox[] combo_boxes;
    private Action<object, EventArgs>[] check_actions;
    private static Graphics graphics;
    private static Rectangle rect_mac, rect_mic; // rect_: slightly larger than the display regions
    private static Bitmap bmp_mac, bmp_mic, bmp_screen; // bmp_screen: snapshots
    private static readonly Bitmap BMP_PIXEL = new(1, 1);
    private static readonly Size SIZE_PIXEL = new(1, 1);
    private static readonly SolidBrush BACK_BRUSH = new(Color.Black);
    private static readonly Pen BDR_PEN = new(Color.Gray), _BDR_PEN = new(Color.White), AXES_PEN = new(Color.DarkGray, 4);
    private static readonly Color CORRECT_GREEN = Argb(192, 255, 192), ERROR_RED = Argb(255, 192, 192),
        UNCHECK_YELLOW = Argb(255, 255, 128), READONLY_PURPLE = Argb(255, 192, 255), COMBO_BLUE = Argb(192, 255, 255),
        FOCUS_GRAY = Color.LightGray, CTRL_GRAY = Argb(105, 105, 105), GRID_GRAY = Argb(75, 255, 255, 255), READONLY_GRAY = Color.Gainsboro,
        UPPER_GOLD = Color.Gold, LOWER_BLUE = Color.RoyalBlue, ZERO_BLUE = Color.Lime, POLE_PURPLE = Color.Magenta;
    //
    private static Real scale_factor, epsilon, stride, mod_stride, arg_stride, stride_real, size_real, decay;
    private static readonly Real GRID_WIDTH_1 = 3, GRID_WIDTH_2 = 2, CURVE_WIDTH_LIMIT = 20, STRIDE = (Real)0.25, MOD = (Real)0.25,
        ARG = MathR.PI / 12, STRIDE_REAL = 1, EPS_REAL = (Real)0.015, EPS_COMPLEX = (Real)0.015, SIZE_REAL = (Real)0.5,
        DECAY = (Real)0.2, DEPTH = 2, CURVE_WIDTH = 5, INCREMENT = (Real)0.001;
    private static int display_elapsed, x_left, x_right, y_up, y_down, color_mode, contour_mode, paren_left = -1, paren_right = -1,
        loop_number, chosen_number, export_number, pixel_number, segment_number;
    private static readonly int X_LEFT_MAC = 620, X_RIGHT_MAC = 1520, Y_UP_MAC = 45, Y_DOWN_MAC = 945,
        X_LEFT_MIC = 1565, X_RIGHT_MIC = 1765, Y_UP_MIC = 745, Y_DOWN_MIC = 945, X_LEFT_CHECK = 1921, X_RIGHT_CHECK = 1922,
        Y_UP_CHECK = 1081, Y_DOWN_CHECK = 1082, REF_POS_1 = 9, REF_POS_2 = 27, WIDTH_IND = 22, HEIGHT_IND = 55,
        LEFT_SUPP = 11, TOP_SUPP = 45, GRID = 5, UPDATE = 5, REFRESH = 100, SLEEP = 100, THRESHOLD = 1000;
    private static readonly int[] BORDERS_MAC = [X_LEFT_MAC, X_RIGHT_MAC, Y_UP_MAC, Y_DOWN_MAC], BORDERS_MIC = [X_LEFT_MIC,
        X_RIGHT_MIC, Y_UP_MIC, Y_DOWN_MIC], BORDERS_CHECK = [X_LEFT_CHECK, X_RIGHT_CHECK, Y_UP_CHECK, Y_DOWN_CHECK];
    private static Real[] scopes; // Corresponds to detail_inputs = [X_Left, X_Right, Y_Left, Y_Right]
    private static int[] borders; // = [x_left, x_right, y_up, y_down]
    private static Matrix<Complex> output_complex;
    private static Matrix<Real> output_real;
    //
    private static bool is_flashing, is_complex = true, delete_point = true, delete_coor, swap_colors, is_auto, freeze_graph,
        clicked, shade, axes_drawn_mac, axes_drawn_mic, is_main, activate_mouse, is_checking, error_input, error_address, is_resized,
        ctrl_pressed, sft_pressed, bdp_painted, paren_change;
    private static readonly string ADDRESS_DEFAULT = @"C:\Users\Public", DATE = "Oct, 2024", STOCKPILE = "stockpile", INPUT_DEFAULT = "z",
        GENERAL_DEFAULT = "e", THICK_DEFAULT = "1", DENSE_DEFAULT = "1", MACRO = "MACRO", MICRO = "MICRO", ZERO = "0",
        REMIND_EXPORT = "Snapshot saved at", REMIND_STORE = "History saved at", CAPTION_DEFAULT = "Your inputs will be shown here.",
        MISTAKES_HEAD = "\r\nCommon mistakes include:", WRONG_FORMAT = "THE INPUT FORMAT IS INVALID.",
        WRONG_ADDRESS = "THE ADDRESS DOES NOT EXIST.", DISPLAY_ERROR = "UNAVAILABLE.",
        DRAFT_DEFAULT = $"\r\nReal-number precision: \r\n{typeof(Real)}.", TIP = "Read-only",
        SEP_1 = new('>', 3), SEP_2 = new('<', 3), SEP = new('-', 6), _SEP = new('-', 80), TAB = new(' ', 4);
    private static readonly string[] CONTOUR_MODES = ["Cartesian (x, y)", "Polar (r, θ)"], COLOR_MODES =
        ["Commonplace", "Monochromatic", "Bichromatic", "Kaleidoscopic", "Miscellaneous"];
    #endregion

    #region Initializations
    public Graph()
    {
        InitializeComponent(); InitializeArrays();
        SetTitleBarColor(); ReduceFontSizeByScale(this, ref scale_factor); BanMouseWheel();
        InitializeTimers(); InitializeGraphics(); InitializeCombo(); InitializeData(); SetThicknessDensenessScopesBorders();
    }
    private void Graph_Load(object sender, EventArgs e) => TextBoxFocus(sender, e);
    private void Graph_Paint(object sender, PaintEventArgs e) { if (!bdp_painted && !clicked) SubtitleBox_DoubleClick(sender, e); }
    private void InitializeArrays()
    {
        detail_inputs = [X_Left, X_Right, Y_Left, Y_Right];
        math_inputs = [InputString, GeneralInput, .. detail_inputs, ThickInput, DenseInput];
        read_only_inputs = [.. math_inputs, AddressInput];

        parameter_defaults = [(GeneralInput, GENERAL_DEFAULT), (ThickInput, THICK_DEFAULT), (DenseInput, DENSE_DEFAULT)];
        reset_inputs = [(InputString, INPUT_DEFAULT), (AddressInput, ADDRESS_DEFAULT), .. parameter_defaults];

        graph_buttons = [ConfirmButton, PreviewButton, AllButton];
        clear_outputs = [DraftBox, PointNumDisplay, TimeDisplay, X_CoorDisplay, Y_CoorDisplay, ModulusDisplay, AngleDisplay,
            FunctionDisplay, CaptionBox];

        default_checks = [(CheckAuto, false), (CheckSwap, false), (CheckPoints, false), (CheckShade, false), (CheckRetain, false),
            (CheckEdit, false), (CheckComplex, true), (CheckCoor, true)];

        reset_labels = [InputLabel, AtLabel, GeneralLabel, DetailLabel, X_Scope, Y_Scope, ThickLabel, DenseLabel, ExampleLabel,
            FunctionLabel, ModeLabel, ContourLabel];

        combo_boxes = [ComboExamples, ComboFunctions, ComboSpecial, ComboColoring, ComboContour];

        check_actions =
        [
            GeneralInput_DoubleClick,
            Details_TextChanged, // Order-sensitive position
            InputString_DoubleClick,
            ThickInput_DoubleClick,
            DenseInput_DoubleClick,
            AddressInput_DoubleClick
        ];
    }
    private int SetTitleBarColor()
    {
        int mode = 1;  // Set to 1 to apply immersive color mode
        const int DWMWA_USE_IMMERSIVE_DARK_MODE = 20; // Desktop Window Manager (DWM)
        return DwmSetWindowAttribute(Handle, DWMWA_USE_IMMERSIVE_DARK_MODE, ref mode, Unsafe.SizeOf<Int32>());
    }
    public static void ReduceFontSizeByScale(Control parentCtrl, ref Real scalingFactor)
    {
        scalingFactor = Graphics.FromHwnd(IntPtr.Zero).DpiX / 96 / (Real)1.5; // Originally designed for 150% display scaling
        foreach (Control ctrl in parentCtrl.Controls)
        {
            ctrl.Font = new(ctrl.Font.FontFamily, ctrl.Font.Size / (float)scalingFactor, ctrl.Font.Style);
            if (ctrl.Controls.Count > 0) ReduceFontSizeByScale(ctrl, ref scalingFactor);
        }
    } // Also used for message boxes, so scalingFactor should remain a parameter rather than a field
    private void BanMouseWheel() // Default mouse-wheel behavior conflicts with the custom combo boxes
    { foreach (var cbx in combo_boxes) cbx.MouseWheel += (sender, e) => ((HandledMouseEventArgs)e).Handled = true; }
    private void InitializeTimers()
    {
        static System.Windows.Forms.Timer setT(int interval) => new() { Interval = interval };
        WaitTimer = setT(500); DisplayTimer = setT(1000 / UPDATE);
        WaitTimer.Tick += (sender, e) =>
        {
            ToggleBool(ref is_flashing); // Properties cannot be passed by reference
            PictureWait.Visible = is_flashing;
        };
        DisplayTimer.Tick += (sender, e) =>
        {
            if (++display_elapsed % UPDATE == 0) SetText(TimeDisplay, (display_elapsed / UPDATE).ToString() + "s");
            SetText(PointNumDisplay, (pixel_number + segment_number).ToString()); // Refreshes $"{UPDATE}" times per second
        };
    }
    private void InitializeGraphics()
    {
        graphics = CreateGraphics();

        int indent = (int)(CURVE_WIDTH_LIMIT / 2), _indent = indent * 2,
            widthMac = X_RIGHT_MAC - X_LEFT_MAC, heightMac = Y_DOWN_MAC - Y_UP_MAC,
            widthMic = X_RIGHT_MIC - X_LEFT_MIC, heightMic = Y_DOWN_MIC - Y_UP_MIC;

        bmp_mac = new(widthMac, heightMac, PixelFormat.Format32bppArgb);
        bmp_mic = new(widthMic, heightMic, PixelFormat.Format32bppArgb);
        bmp_screen = new(Width - WIDTH_IND, Height - HEIGHT_IND);

        rect_mac = new(X_LEFT_MAC - indent, Y_UP_MAC - indent, widthMac + _indent, heightMac + _indent);
        rect_mic = new(X_LEFT_MIC - indent, Y_UP_MIC - indent, widthMic + _indent, heightMic + _indent);

        DoubleBuffered = KeyPreview = true; // Essential for shortcuts
    }
    private void InitializeCombo()
    {
        ComboColoring.Items.AddRange(COLOR_MODES); ComboColoring.SelectedIndex = 4;
        ComboContour.Items.AddRange(CONTOUR_MODES); ComboContour.SelectedIndex = 1;

        ComboExamples.Items.AddRange(ReplaceTags.EX_COMPLEX); ComboExamples.Items.Add(String.Empty);
        ComboExamples.Items.AddRange(ReplaceTags.EX_REAL); ComboExamples.Items.Add(String.Empty);
        ComboExamples.Items.AddRange(ReplaceTags.EX_CURVES); ComboExamples.Items.Add(String.Empty);

        ComboFunctions.Items.AddRange(ReplaceTags.FUNCTIONS); ComboSpecial.Items.AddRange(ReplaceTags.SPECIALS);
    }
    private void ResetInputs() { foreach (var (tbx, value) in reset_inputs) SetText(tbx, value); FocusInput(); }
    private void InitializeData() { ResetInputs(); SetText(DraftBox, DRAFT_DEFAULT); SetText(CaptionBox, CAPTION_DEFAULT); }
    private void SetThicknessDensenessScopesBorders(bool autoFill = true)
    {
        foreach (var (tbx, value) in parameter_defaults) FillEmpty(tbx, value);
        if (autoFill) foreach (var tbx in detail_inputs) FillEmpty(tbx, ZERO);

        Real _dense = Obtain(DenseInput), _thick = Obtain(ThickInput);
        stride_real = STRIDE_REAL / _dense; stride = STRIDE / _dense; mod_stride = MOD / _dense; arg_stride = ARG / _dense;
        epsilon = (is_complex ? EPS_COMPLEX : EPS_REAL) * _thick; size_real = SIZE_REAL * _thick / (1 + _thick); decay = DECAY * _thick;

        if (!DetailedScopeEnabled())
        {
            Real _scope = Obtain(GeneralInput);
            scopes = [-_scope, _scope, -_scope, _scope]; // Note the signs
            for (int i = 0; i < detail_inputs.Length; i++) SetText(detail_inputs[i], scopes[i].ToString("0.################"));
        } // Never use scientific notation
        else for (int i = 0; i < detail_inputs.Length; i++) scopes[i] = Obtain(detail_inputs[i]);
        MyString.ThrowException(InvalidScopesX() || InvalidScopesY()); // A more specific exception is determined later
        borders = [x_left, x_right, y_up, y_down];
    }
    private void TextBoxFocus(object sender, EventArgs e)
    {
        foreach (var ctrl in Controls.OfType<TextBox>())
            ctrl.GotFocus += (sender, e) => { ((TextBox)sender).SelectionStart = ((TextBox)sender).Text.Length; };
        foreach (var tbx in math_inputs)
        {
            tbx.MouseDown += (sender, e) => RecoverParen();
            tbx.MouseUp += (sender, e) => { SnapSeparatorCaret(tbx); ShowParen(tbx); };
            tbx.Leave += (sender, e) => RecoverParen();
        }
    } // Forces the caret to the end of each text box
    #endregion

    #region External Methods
    [DllImport("dwmapi.dll")]
    static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int attrValue, int attrSize);
    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    static extern bool BlockInput([MarshalAs(UnmanagedType.Bool)] bool fBlockIt);
    [DllImport("user32.dll", CharSet = CharSet.Auto)]
    public static extern bool HideCaret(IntPtr hWnd); // Also used for message boxes
    protected override void WndProc(ref Message m) // Window Procedure
    {
        const int WM_NCLBTNDOWN = 0x00A1, HTCAPTION = 0x0002; // Window Message, Non-Client Left Button Down, Hit Test Caption
        if (m.Msg == WM_NCLBTNDOWN && m.WParam.ToInt32() == HTCAPTION) return; // Prevents the title bar from being dragged
        base.WndProc(ref m);
    } // Overrides WndProc to customize window behavior
    #endregion

    #region Shorthands
    private static int AddOne(int input) => input + 1;
    private static Color Swap(Color c1, Color c2) => swap_colors ? c1 : c2;
    private static Color Argb(int a, int r, int g, int b) => Color.FromArgb(a, r, g, b);
    public static Color Argb(int r, int g, int b) => Color.FromArgb(r, g, b); // Also used for message boxes
    private static int Frac(int input, Real alpha) => (int)(input * alpha);
    private static bool IllegalRatio(Real ratio) => ratio < 0 || ratio > 1;
    private static int RowBorders(int[] borders) => borders[1] - borders[0];
    private static int ColumnBorders(int[] borders) => borders[3] - borders[2];
    private static Real RowScopes() => scopes[1] - scopes[0];
    private static Real ColumnScopes() => scopes[3] - scopes[2]; // Sign conventions vary across the codebase
    private static bool InvalidScopesX() => scopes[0] >= scopes[1];
    private static bool InvalidScopesY() => scopes[2] >= scopes[3];
    private static int[] GetBorders(int mode) => mode switch { 1 => BORDERS_MAC, 2 => BORDERS_MIC, 3 => BORDERS_CHECK };
    private static Matrix<Real> GetMatrix(int rows, int columns) => new(RealComplex.GetArithProg(rows, columns), columns);
    private static Rectangle GetRect(int[] borders, int margin = 0)
        => new(borders[0] + margin, borders[2] + margin, RowBorders(borders) - margin, ColumnBorders(borders) - margin);
    private static Bitmap GetBitmap(bool isMain) => isMain ? bmp_mac : bmp_mic;
    private static ref bool GetAxesDrawnRef(bool isMain) => ref (isMain ? ref axes_drawn_mac : ref axes_drawn_mic);
    private static void SetAxesDrawn(bool isMain, bool drawn = false) { GetAxesDrawnRef(isMain) = drawn; }
    private static void ToggleBool(ref bool isChecked) => isChecked = !isChecked;
    private static Real Obtain(string text) => RealSub.Obtain(ImplicitMultiply.NormalizeInput(text));
    private static Real Obtain(TextBox tbx) => Obtain(tbx.Text);
    private static void SetText(TextBox tbx, string text) => tbx.Text = text;
    private static void SetCaret(TextBox tbx, int pos) { tbx.SelectionStart = MathR.Clamp(pos, 0, tbx.Text.Length); tbx.ScrollToCaret(); }
    private static void FillEmpty(TextBox tbx, string text) { if (String.IsNullOrEmpty(tbx.Text)) SetText(tbx, text); }
    private void AddDraft(string text) => SetText(DraftBox, text + DraftBox.Text);
    private void SetScrollBars(bool enabled) => VScrollBarX.Enabled = VScrollBarY.Enabled = enabled;
    private bool DetailedScopeEnabled() => GeneralInput.Text == ZERO;
    private void ClearExampleSelection() => ComboExamples.SelectedIndex = -1;
    private void FocusInput() { InputString.Focus(); SetCaret(InputString, InputString.Text.Length); }
    private bool NoInput() => String.IsNullOrEmpty(InputString.Text);
    private bool InputLocked() => InputString.ReadOnly || paren_change;
    #endregion

    #region Auxiliary Drawings
    private static void DrawBackdrop(int[] borders)
    { graphics.DrawRectangle(BDR_PEN, GetRect(borders)); graphics.FillRectangle(BACK_BRUSH, GetRect(borders, 1)); }
    private static void DrawAxesGrids(int[] borders)
    {
        bdp_painted = true; // Prevents Graph_Paint from being called afterward
        static Real calculateGrid(Real range) => MathR.Pow(GRID, MathR.Floor(MathR.Log(range / 2, GRID)));
        var (xGrid, yGrid) = (calculateGrid(RowScopes()), calculateGrid(ColumnScopes()));
        var (ratioRow, ratioColumn) = (GetRatioRow(borders), GetRatioColumn(borders));
        var (xInit, yInit, xEnd, yEnd) = (AddOne(borders[0]), AddOne(borders[2]), borders[1], borders[3]);

        void drawGrids(Real xGrid, Real yGrid, Real penWidth)
        {
            Pen gridPen = new(GRID_GRAY, (float)penWidth);
            RealComplex.ForEachInclusive((int)MathR.Floor(scopes[2] / yGrid), (int)MathR.Ceiling(scopes[3] / yGrid), i =>
            {
                int pos = LinearTransformY(i * yGrid, borders, ratioColumn);
                if (pos >= yInit && pos < yEnd) graphics.DrawLine(gridPen, xInit, pos, xEnd, pos);
            });
            RealComplex.ForEachInclusive((int)MathR.Floor(scopes[0] / xGrid), (int)MathR.Ceiling(scopes[1] / xGrid), i =>
            {
                int pos = LinearTransformX(i * xGrid, borders, ratioRow);
                if (pos >= xInit && pos < xEnd) graphics.DrawLine(gridPen, pos, yEnd, pos, yInit);
            });
        }
        drawGrids(xGrid, yGrid, GRID_WIDTH_1); drawGrids(xGrid / GRID, yGrid / GRID, GRID_WIDTH_2);

        var (x, y) = (LinearTransformX(0, borders, ratioRow), LinearTransformY(0, borders, ratioColumn));
        if (y >= yInit && y < yEnd) graphics.DrawLine(AXES_PEN, xInit, y, xEnd, y);
        if (x >= xInit && x < xEnd) graphics.DrawLine(AXES_PEN, x, yEnd, x, yInit);
    }
    private static void DrawBackdropAxesGrids(int[] borders, bool isMain, bool isFrozen = false)
    {
        if (!isFrozen) { DrawBackdrop(borders); SetAxesDrawn(isMain); }
        if (!delete_coor && !GetAxesDrawnRef(isMain)) { DrawAxesGrids(borders); SetAxesDrawn(isMain, true); }
    } // Sensitive
    private void DrawReferenceRectangles(Color color) => graphics.FillRectangle(new SolidBrush(color), VScrollBarX.Location.X - REF_POS_1,
        Y_UP_MIC + REF_POS_2, 2 * (VScrollBarX.Width + REF_POS_1), VScrollBarX.Height - 2 * REF_POS_2);
    private void UpdateScrollBars((Real x, Real y) xyCoor)
    {
        int range = VScrollBarX.Maximum - VScrollBarX.Minimum;
        VScrollBarX.Value = Frac(range, (xyCoor.x - scopes[0]) / RowScopes());
        VScrollBarY.Value = Frac(range, (xyCoor.y - scopes[2]) / ColumnScopes());
    }
    #endregion

    // 2. GRAPHING
    #region Numerics
    private static Real GetRatioRow(int[] borders) => RowScopes() / RowBorders(borders);
    private static Real GetRatioColumn(int[] borders) => -ColumnScopes() / ColumnBorders(borders); // Note the minus sign
    private static int LinearTransformX(Real x, int[] borders, Real ratioRow) => (int)(borders[0] + (x - scopes[0]) / ratioRow);
    private static int LinearTransformY(Real y, int[] borders, Real ratioColumn) => (int)(borders[2] + (y - scopes[3]) / ratioColumn);
    private static (Real, Real) LinearTransform(int x, int y, Real xCoor, Real yCoor, int[] borders)
        => (scopes[0] + (x - borders[0]) * xCoor, scopes[3] + (y - borders[2]) * yCoor);
    private static int LowIdx(Real a, Real m) => (int)MathR.Floor(a / m);
    private static Real LowDist(Real a, Real m) => a - m * LowIdx(a, m);
    private static Real LowRatio(Real a, Real m) => a == 0 && BitConverter.DoubleToInt64Bits(a) < 0 ? 1 : LowDist(a, m) / m;
    private static Real LowRatioFast(Real a, Real m)
    {
        if (a == 0 && BitConverter.DoubleToInt64Bits(a) < 0) return 1;
        Real q = a / m; return q - MathR.Floor(q);
    }
    private static Real LowNearDist(Real a, Real m) { Real d = LowDist(a, m); return MathR.Min(d, m - d); }
    private static Real GetShade(Real alpha) => (alpha - 1) / DEPTH + 1;
    private unsafe static (Real, Real) GetAtanExtrema(Matrix<Real> output, int rows, int columns)
    {
        static Real seekM(Func<Real, Real, Real> function, Real* ptr, int length)
        {
            Real value = Real.NaN;
            for (int i = 0; i < length; i++, ptr++)
            { if (Real.IsNaN(*ptr)) continue; if (Real.IsNaN(value)) value = *ptr; else value = function(*ptr, value); }
            return value;
        }
        Matrix<Real> minMax = GetMatrix(2, rows);
        Parallel.For(0, rows, p =>
        {
            Real* srcPtr = output.RowPtr(p);
            minMax[0, p] = seekM(MathR.Min, srcPtr, columns); minMax[1, p] = seekM(MathR.Max, srcPtr, columns);
        });
        return (MathR.Atan(seekM(MathR.Min, minMax.RowPtr(0), rows)), MathR.Atan(seekM(MathR.Max, minMax.RowPtr(1), rows)));
    } // Finds the minimum and maximum after applying atan, which bounds infinite values
    private unsafe static (int, int, Matrix<Real>, Matrix<Real>) CreateCoorMatrices()
    {
        var (rows, columns, _x, _y) = (RowBorders(borders), ColumnBorders(borders), GetRatioRow(borders), GetRatioColumn(borders));
        var (xCoor, yCoor) = (GetMatrix(rows, columns), GetMatrix(rows, columns));
        Parallel.For(0, rows, p =>
        {
            Real* xPtr = xCoor.RowPtr(p), yPtr = yCoor.RowPtr(p); var (x, y) = (scopes[0] + (1 + p) * _x, scopes[3] + _y);
            for (int q = 0; q < columns; q++, xPtr++, yPtr++, y += _y) (*xPtr, *yPtr) = (x, y);
        }); // Optimized linear transforms
        return (rows, columns, xCoor, yCoor);
    }
    #endregion

    #region Rendering Core
    private static (int, BitmapData) LockBitmapData(Bitmap bmp) // bpp: bytes per pixel
        => (Image.GetPixelFormatSize(bmp.PixelFormat) / 8,
            bmp.LockBits(new(0, 0, bmp.Width, bmp.Height), ImageLockMode.ReadWrite, bmp.PixelFormat));
    private unsafe static void ClearBitmap(Bitmap bmp)
    {
        var (width, height) = (bmp.Width, bmp.Height); var (bpp, bmpData) = LockBitmapData(bmp);
        try
        {
            var bmpInit = (byte*)bmpData.Scan0 + bpp - 1;
            Parallel.For(0, height, y =>
            {
                byte* pixelPtr = bmpInit + y * bmpData.Stride;
                for (int x = 0; x < width; x++, pixelPtr += bpp) *pixelPtr = 0;
            });
        }
        finally { bmp.UnlockBits(bmpData); }
    }
    private unsafe void SetPixel(byte* _ptr, Color color, ref int pixNum)
    { pixNum++; *_ptr = color.B; _ptr++; *_ptr = color.G; _ptr++; *_ptr = color.R; _ptr++; *_ptr = color.A; }
    private unsafe void RealSpecial(byte* _ptr, Color _zero, Color _pole, Real value, (Real min, Real max) mM, ref int pixNum)
    {
        if (value < Real.Lerp(mM.min, mM.max, size_real)) SetPixel(_ptr, _zero, ref pixNum);
        if (value > Real.Lerp(mM.max, mM.min, size_real)) SetPixel(_ptr, _pole, ref pixNum);
    }
    private unsafe void ComplexSpecial(byte* _ptr, Color _zero, Color _pole, Real value, ref int pixNum)
    {
        if (value < epsilon) SetPixel(_ptr, _zero, ref pixNum);
        if (value > 1 / epsilon) SetPixel(_ptr, _pole, ref pixNum);
    }
    private delegate void PixelLoop(int x, int y, IntPtr pixelPtr, ref int pixNum); // Instead of Action<int, int, IntPtr, ref int>
    private unsafe static void LoopBase(PixelLoop pixelLoop)
    {
        Bitmap bmp = GetBitmap(is_main); var (width, height) = (bmp.Width, bmp.Height); var (bpp, bmpData) = LockBitmapData(bmp);
        var (xInit, yInit) = (1, 1); var (xLen, yLen) = (width - xInit, height - yInit);
        try
        {
            int[] pixNums = new int[yLen]; var bmpInit = (byte*)bmpData.Scan0 + yInit * bmpData.Stride + xInit * bpp;
            Parallel.For(0, yLen, y =>
            {
                int pixNum = 0; var pixelPtr = (IntPtr)(bmpInit + y * bmpData.Stride);
                for (int x = 0; x < xLen; x++, pixelPtr += bpp) pixelLoop(x, y, pixelPtr, ref pixNum);
                pixNums[y] = pixNum;
            });
            fixed (int* ptr = pixNums) { int* _ptr = ptr; for (int y = 0; y < yLen; y++, _ptr++) pixel_number += *_ptr; }
        }
        finally { bmp.UnlockBits(bmpData); }
    }
    private static Color GetColorReal123(Real value, int mode)
    {
        Color func23(Color c1, Color c2) => value < 0 ? Swap(c1, c2) : value > 0 ? Swap(c2, c1) : Color.Empty;
        return mode switch
        {
            1 => MathR.Abs(value) < epsilon ? Swap(Color.Black, Color.White) : Color.Empty,
            2 => func23(Color.White, Color.Black),
            3 => func23(UPPER_GOLD, LOWER_BLUE)
        };
    }
    private static Color GetColorReal45(Real value, bool mode, (Real min, Real max) mM) => mode ?
        ObtainColorStrip(value, mM.min, mM.max) : ObtainColorStrip(value, mM.min, mM.max, GetShade(LowRatio(value, stride_real)));
    private static Color GetColorComplex123(Complex input, int mode, bool isReIm)
    {
        var (v1, v2) = Complex.ReIm(isReIm ? input : Complex.Log(input));
        var (s1, s2) = isReIm ? (stride, stride) : (mod_stride, arg_stride);
        var (c1, c2) = mode switch
        {
            1 => (Color.White, Color.Black),
            2 => (Color.Black, Color.White),
            3 => (LOWER_BLUE, UPPER_GOLD)
        };
        bool draw = mode != 1 ? Int32.IsEvenInteger(LowIdx(v1, s1) + LowIdx(v2, s2))
            : MathR.Min(LowNearDist(v1, s1), LowNearDist(v2, s2)) < epsilon;
        return mode == 1 ? (draw ? Swap(c2, c1) : Color.Empty) : (draw ? Swap(c1, c2) : Swap(c2, c1));
    }
    private static Color GetColorComplex45(Complex value, bool mode)
    {
        if (mode) return ObtainColorWheel(value, 1);
        Complex z = Complex.Log(value);
        Real alpha = GetShade((LowRatioFast(z.real, mod_stride) + LowRatioFast(z.imaginary, arg_stride)) / 2);
        if (shade) return ObtainColorWheel(value, alpha);
        Real argument = z.imaginary < 0 ? z.imaginary + MathR.Tau : z.imaginary;
        return ObtainColorBase(argument, alpha, 255);
    }
    private unsafe void RealLoop123(Matrix<Real> output, Color zero, Color pole, int mode, (Real, Real) mM)
        => LoopBase((x, y, pixelPtr, ref pixNum) =>
        {
            Real value = output[x, y]; byte* ptr = (byte*)pixelPtr;
            if (Real.IsNaN(value)) return;
            SetPixel(ptr, GetColorReal123(value, mode), ref pixNum);
            if (!delete_point) RealSpecial(ptr, zero, pole, MathR.Atan(value), mM, ref pixNum);
        });
    private unsafe void RealLoop45(Matrix<Real> output, bool mode, (Real, Real) mM)
        => LoopBase((x, y, pixelPtr, ref pixNum) =>
        {
            Real value = output[x, y]; byte* ptr = (byte*)pixelPtr;
            if (Real.IsNaN(value)) return;
            SetPixel(ptr, GetColorReal45(value, mode, mM), ref pixNum);
            if (!delete_point) RealSpecial(ptr, Color.Black, Color.White, MathR.Atan(value), mM, ref pixNum);
        });
    private unsafe void ComplexLoop123(Matrix<Complex> output, Color zero, Color pole, int mode, bool isReIm)
        => LoopBase((x, y, pixelPtr, ref pixNum) =>
        {
            Complex value = output[x, y]; byte* ptr = (byte*)pixelPtr;
            if (Real.IsNaN(value.real) || Real.IsNaN(value.imaginary)) return;
            SetPixel(ptr, GetColorComplex123(value, mode, isReIm), ref pixNum);
            if (!delete_point) ComplexSpecial(ptr, zero, pole, Complex.Modulus(value), ref pixNum);
        });
    private unsafe void ComplexLoop45(Matrix<Complex> output, bool mode)
        => LoopBase((x, y, pixelPtr, ref pixNum) =>
        {
            Complex value = output[x, y]; byte* ptr = (byte*)pixelPtr;
            if (Real.IsNaN(value.real) || Real.IsNaN(value.imaginary)) return;
            SetPixel(ptr, GetColorComplex45(value, mode), ref pixNum);
            if (!delete_point) ComplexSpecial(ptr, Color.Black, Color.White, Complex.Modulus(value), ref pixNum);
        });
    #endregion

    #region Rendering
    private void RealComputation()
    {
        Action<Matrix<Real>, (Real, Real)> realOperation = color_mode switch
        {
            1 => Real1,
            2 => Real2,
            3 => Real3,
            4 => Real4,
            5 => Real5
        };
        realOperation(output_real, GetAtanExtrema(output_real, RowBorders(borders), ColumnBorders(borders)));
    }
    private void Real1(Matrix<Real> output, (Real, Real) mM) => RealLoop123(output, ZERO_BLUE, POLE_PURPLE, 1, mM);
    private void Real2(Matrix<Real> output, (Real, Real) mM) => RealLoop123(output, ZERO_BLUE, POLE_PURPLE, 2, mM);
    private void Real3(Matrix<Real> output, (Real, Real) mM) => RealLoop123(output, Color.Black, Color.White, 3, mM);
    private void Real4(Matrix<Real> output, (Real, Real) mM) => RealLoop45(output, true, mM);
    private void Real5(Matrix<Real> output, (Real, Real) mM) => RealLoop45(output, false, mM);
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
    private (Real, Real, Real) GetCurveBounds(string[] split, bool isPolar, bool isParam)
    {
        Real obtain(int index) => Obtain(split[index]);
        (Real, Real, Real) initializeParamPolar(int relPos)
        {
            MyString.ThrowInvalidLengths(split, [relPos + 2, relPos + 3]);
            return (obtain(relPos), obtain(relPos + 1), split.Length == relPos + 3 ? obtain(relPos + 2) : INCREMENT);
        }
        if (isParam) return initializeParamPolar(3);
        else if (isPolar) return initializeParamPolar(2);
        else
        {
            MyString.ThrowInvalidLengths(split, [1, 2, 3, 4]); Real range = Obtain(GeneralInput);
            Real getRange(TextBox tbx, bool minus) => DetailedScopeEnabled() ? Obtain(tbx) : (minus ? -range : range);
            return (split.Length < 3 ? getRange(X_Left, true) : obtain(1),
                split.Length < 3 ? getRange(X_Right, false) : obtain(2),
                split.Length == 2 ? obtain(1) : split.Length == 4 ? obtain(3) : INCREMENT);
        }
    }
    private static (string, string) GetCurveInputs(string[] split, bool isPolar, bool isParam)
    {
        string replace(string s, int index) => s.Replace(split[index], "x");
        string tag1 = ReplaceTags.FUNC_HEAD + ReplaceTags.COS, tag2 = ReplaceTags.FUNC_HEAD + ReplaceTags.SIN;
        return (isParam ? replace(split[0], 2) : isPolar ? replace($"({split[0]})*{tag1}({split[1]})", 1) : "x",
            isParam ? replace(split[1], 2) : isPolar ? replace($"({split[0]})*{tag2}({split[1]})", 1) : split[0]);
    }
    private unsafe static (Matrix<Real>, Matrix<Real>, int) EvaluateCurveValues(
        string input1, string input2, Real start, Real end, Real increment)
    {
        int length = (int)((end - start) / increment), _length = length + 1;
        Matrix<Real> partition = GetMatrix(1, _length); Real steps = start;

        Real* partPtr = partition.RowPtr();
        for (int i = 0; i < _length; i++, partPtr++, steps += increment) *partPtr = steps;

        Matrix<Real> obtain(string input) => new RealSub(input, partition, null, null, null, null, 1, _length).Obtain();
        return (obtain(input1), obtain(input2), length);
    }
    private unsafe void DrawCurve(Matrix<Real> value1, Matrix<Real> value2, int length)
    {
        Real curveWidth = MathR.Min(CURVE_WIDTH * Obtain(ThickInput), CURVE_WIDTH_LIMIT);
        Pen dichoPen(Color c1, Color c2) => new(Swap(c1, c2), (float)curveWidth);
        Pen vividPen = dichoPen(Color.Empty, Color.Empty), defaultPen = dichoPen(Color.Black, Color.White),
            blackPen = dichoPen(Color.White, Color.Black), whitePen = dichoPen(Color.Black, Color.White),
            bluePen = dichoPen(LOWER_BLUE, UPPER_GOLD), yellowPen = dichoPen(UPPER_GOLD, LOWER_BLUE),
            selectedPen = color_mode == 1 ? defaultPen : vividPen;

        Point pos = new(), posBuffer = new(); bool inRange, inRangeBuffer = false; int _ratio, _ratioBuffer = 0;
        Real relativeSpeed = Obtain(DenseInput) / length, ratio; Real* v1Ptr = value1.RowPtr(), v2Ptr = value2.RowPtr();
        var (ratioRow, ratioColumn) = (GetRatioRow(borders), GetRatioColumn(borders));

        for (int steps = 0; steps <= length; steps++, v1Ptr++, v2Ptr++, segment_number++)
        {
            (pos.X, pos.Y) = (LinearTransformX(*v1Ptr, borders, ratioRow), LinearTransformY(*v2Ptr, borders, ratioColumn));
            inRange = *v1Ptr > scopes[0] && *v1Ptr < scopes[1] && *v2Ptr > scopes[2] && *v2Ptr < scopes[3];
            if (inRangeBuffer && inRange && posBuffer != pos)
            {
                ratio = relativeSpeed * steps % 1;
                selectedPen = color_mode switch
                {
                    2 => ratio < (Real)0.5 ? whitePen : blackPen,
                    3 => ratio < (Real)0.5 ? bluePen : yellowPen,
                    _ => selectedPen
                };
                if (color_mode > 3) vividPen.Color = ObtainColorWheelCurve(ratio);
                graphics.DrawLine(selectedPen, posBuffer, pos);
                SetScrollBars(true); // Necessary for each loop
                UpdateScrollBars(LinearTransform(pos.X, pos.Y, ratioRow, ratioColumn, borders));
                _ratio = Frac(REFRESH, ratio);
                if (_ratioBuffer != _ratio) DrawReferenceRectangles(selectedPen.Color);
                _ratioBuffer = _ratio;
            }
            inRangeBuffer = inRange; posBuffer = pos;
        }
    }
    private void DisplayCurve(string[] split, bool isPolar = false, bool isParam = false)
    {
        var (start, end, increment) = GetCurveBounds(split, isPolar, isParam);
        MyString.ThrowException(start >= end || increment <= 0);
        var (input1, input2) = GetCurveInputs(split, isPolar, isParam);
        if (is_checking) { RealSub.Obtain(input1, start); RealSub.Obtain(input2, start); return; }
        var (value1, value2, length) = EvaluateCurveValues(input1, input2, start, end, increment);
        DisplayBase(() => { DrawCurve(value1, value2, length); pixel_number += segment_number; segment_number = 0; });
    }
    private void DisplayFunction(string[] split) => DisplayCurve(split); // Necessary
    private void DisplayPolar(string[] split) => DisplayCurve(split, isPolar: true);
    private void DisplayParametric(string[] split) => DisplayCurve(split, isParam: true);
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
        ClearBitmap(GetBitmap(is_main)); // Required thanks to ZAL
        computeAction();
        DisplayBase(() => { graphics.DrawImageUnscaled(GetBitmap(is_main), borders[0], borders[2]); });
    }
    private void DisplayExpression(string input)
    {
        var (rows, columns, xCoor, yCoor) = CreateCoorMatrices();
        if (is_complex) output_complex = new ComplexSub(input, xCoor, yCoor, rows, columns).Obtain();
        else output_real = new RealSub(input, xCoor, yCoor, null, null, null, rows, columns).Obtain();
        RunDisplayBase(is_complex ? ComplexComputation : RealComputation);
    }
    private void DisplayIterateLoop(string[] split)
    {
        if (is_complex && split.Length == 7) split = [.. split, "z"];
        var (rows, columns, xCoor, yCoor) = CreateCoorMatrices();
        string replaceLoop(int loops, int origIdx, int subIdx) => MyString.ReplaceLoop(split, origIdx, subIdx, loops.ToString(), true);
        string obtainDisplay(int loops, string defaultInput) => split.Length == 6 ? replaceLoop(loops, 5, 2) : defaultInput;

        if (is_complex)
        {
            MyString.ThrowInvalidLengths(split, [5, 6, 8]);
            if (split.Length != 8)
            {
                Matrix<Complex> z = ComplexSub.InitializeZ(xCoor, yCoor, rows, columns); // Complex-specific
                ComplexSub iterator = new("0", z, null, null, rows, columns);
                iterator.Iterate(split[..5], (loops, Z) =>
                {
                    output_complex = new ComplexSub(obtainDisplay(loops, "Z"), z, Z, null, rows, columns).Obtain();
                    RunDisplayBase(ComplexComputation);
                }).Return();
            }
            else
            {
                RealSub iterator = new("0", xCoor, yCoor, null, null, null, rows, columns);
                var (_, finalX, finalY) = iterator.ProcessIterate2([.. split], (loops, X, Y) =>
                {
                    output_complex = new ComplexSub(replaceLoop(loops, 7, 4), X, Y, rows, columns).Obtain();
                    RunDisplayBase(ComplexComputation);
                });
                finalX.Return(); finalY.Return();
            }
        }
        else
        {
            MyString.ThrowInvalidLengths(split, [5, 6]);
            RealSub iterator = new("0", xCoor, yCoor, null, null, null, rows, columns);
            iterator.Iterate1(split[..5], (loops, X) =>
            {
                output_real = new RealSub(obtainDisplay(loops, "y-X"), xCoor, yCoor, X, null, null, rows, columns).Obtain();
                RunDisplayBase(RealComputation);
            }).Return();
        }
    }
    private void DisplayLoop(string[] split)
    {
        MyString.ThrowInvalidLengths(split, [4]);
        RealComplex.ForEachInclusive(RealSub.ToInt(split[2]), RealSub.ToInt(split[3]), loops =>
        { DispatchGraphCall(MyString.ReplaceLoop(split, 0, 1, loops.ToString(), true)); });
    }
    private void DisplaySubs(string[] split)
    {
        MyString.ThrowException(Int32.IsEvenInteger(split.Length));
        for (int i = 0, j = 1; i < split.Length / 2; i++) split[0] = MyString.ReplaceLoop(split, 0, j++, split[j++]);
        DispatchLoop(split[0]);
    }
    private void DispatchGraphCall(string input)
    {
        Action<string[]>? displayMethod = MyString.StartsWithTag(input, ReplaceTags.ITLOOP) ? DisplayIterateLoop :
            MyString.StartsWithTag(input, ReplaceTags._FUNC) ? DisplayFunction :
            MyString.StartsWithTag(input, ReplaceTags._POLAR) ? DisplayPolar :
            MyString.StartsWithTag(input, ReplaceTags._PARAM) ? DisplayParametric : null;
        if (displayMethod != null) displayMethod(MyString.SplitArguments(input));
        else DisplayExpression(input);
    }
    private void DispatchLoop(string input)
    {
        if (MyString.StartsWithTag(input, ReplaceTags.LOOP)) DisplayLoop(MyString.SplitArguments(input));
        else DispatchGraphCall(input);
    }
    private void DispatchSubstitute(string input)
    {
        if (MyString.StartsWithTag(input, ReplaceTags.SUBS)) DisplaySubs(MyString.SplitArguments(input));
        else DispatchLoop(input);
    }
    private void DisplayInput()
    {
        if (NoInput()) return; // Necessary
        string[] split = MyString.SplitTopLevel(InputString.Text, "|");
        for (int loops = 0; loops < split.Length; loops++) DispatchSubstitute(ImplicitMultiply.NormalizeInput(split[loops]));
    }
    #endregion

    #region Color Extractors
    private static Color ObtainColorBase(Real argument, Real alpha, int decay) // alpha: brightness
    {
        if (IllegalRatio(alpha)) return Color.Empty; // Necessary
        argument /= Complex.PI_THIRD; int proportion, region = argument < 0 ? -1 : (int)argument;
        if (region == 6) region = proportion = 0; else proportion = Frac(255, argument - region);
        return region switch
        {
            0 => Argb(decay, Frac(255, alpha), Frac(proportion, alpha), 0),
            1 => Argb(decay, Frac(255 - proportion, alpha), Frac(255, alpha), 0),
            2 => Argb(decay, 0, Frac(255, alpha), Frac(proportion, alpha)),
            3 => Argb(decay, 0, Frac(255 - proportion, alpha), Frac(255, alpha)),
            4 => Argb(decay, Frac(proportion, alpha), 0, Frac(255, alpha)),
            5 => Argb(decay, Frac(255, alpha), 0, Frac(255 - proportion, alpha)),
            _ => Color.Empty
        }; // ARGB color hexagon used for standard domain coloring
    } // Reference: https://en.wikipedia.org/wiki/Domain_coloring & https://complex-analysis.com/content/domain_coloring.html
    private static Color ObtainColorWheel(Complex c, Real alpha = 1) => ObtainColorBase(RealComplex.ArgRGB(c.real, c.imaginary),
        alpha, shade ? (int)(255 / (1 + decay * Complex.Modulus(c))) : 255);
    private static Color ObtainColorWheelCurve(Real alpha) => ObtainColorBase(alpha * MathR.Tau, 1, 255);
    private static Color ObtainColorStrip(Real value, Real min, Real max, Real alpha = 1) // alpha: brightness
    {
        if (min == max) return Color.Empty; // Necessary
        Real beta = (MathR.Atan(value) - min) / (max - min);
        if (IllegalRatio(alpha) || IllegalRatio(beta)) return Color.Empty; // Necessary
        return beta < (Real)0.5 ? Argb(Frac(Frac(510, beta), alpha), 0, Frac(255, alpha))
            : Argb(Frac(255, alpha), 0, Frac(255 - Frac(510, beta - (Real)0.5), alpha));
    }
    #endregion

    // 3. INTERACTIONS
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
        HandleMouseAction(e, borders, v => { UpdateScrollBars(v); DisplayMouseMove(e, v.Item1, v.Item2); });
    }
    private void RunMouseDown(MouseEventArgs e, int[] borders)
    { chosen_number++; HandleMouseAction(e, borders, v => { DisplayMouseDown(e, v.Item1, v.Item2); }); }
    private bool ActivateMoveDown() => activate_mouse && !error_input && !is_checking && !NoInput();
    private static void RunMouse(MouseEventArgs e, int[] b, Action<MouseEventArgs, int[]> action, Action? _action)
    { if (e.X > b[0] && e.X < b[1] && e.Y > b[2] && e.Y < b[3]) action(e, b); else _action?.Invoke(); }
    private static void CheckMoveDown(Action<int[]> checkMouse) => checkMouse(GetBorders(is_main ? 1 : 2));
    private static void HandleMouseAction(MouseEventArgs e, int[] borders, Action<(Real, Real)> actionHandler)
        => actionHandler(LinearTransform(e.X, e.Y, GetRatioRow(borders), GetRatioColumn(borders), borders));
    private void DisplayMouseMoveCore(int x = 0, int y = 0)
    {
        if (!MyString.ContainsAny(InputString.Text, MyString.FPP_NAMES))
        {
            if (is_complex) SetText(FunctionDisplay, $"[Re] {output_complex[x, y].real}\r\n[Im] {output_complex[x, y].imaginary}");
            else SetText(FunctionDisplay, output_real[x, y].ToString());
        }
        else SetText(FunctionDisplay, DISPLAY_ERROR);
    }
    private void DisplayMouseMove(MouseEventArgs e, Real xCoor, Real yCoor)
    {
        static string trimMove(Real input) => MyString.FormatNumber(input, THRESHOLD);
        SetText(X_CoorDisplay, trimMove(xCoor)); SetText(Y_CoorDisplay, trimMove(yCoor));
        SetText(ModulusDisplay, trimMove(Real.Hypot(xCoor, yCoor))); SetText(AngleDisplay, MyString.FormatAngle(xCoor, yCoor));
        DisplayMouseMoveCore(e.X - AddOne(borders[0]), e.Y - AddOne(borders[2]));
    }
    private void DisplayMouseDown(MouseEventArgs e, Real xCoor, Real yCoor)
    {
        static string trimDown(Real input) => MyString.FormatNumber(input, THRESHOLD);
        string _xCoor = trimDown(xCoor), _yCoor = trimDown(yCoor), modulus = trimDown(Real.Hypot(xCoor, yCoor)),
            angle = MyString.FormatAngle(xCoor, yCoor), message = String.Empty;
        if (!MyString.ContainsAny(InputString.Text, MyString.FPP_NAMES))
        {
            message += "\r\n\r\n";
            var (x, y) = (e.X - AddOne(borders[0]), e.Y - AddOne(borders[2]));
            if (is_complex) message += $"Re = {trimDown(output_complex[x, y].real)}\r\nIm = {trimDown(output_complex[x, y].imaginary)}";
            else message += $"f(x, y) = {trimDown(output_real[x, y])}";
        }
        AddDraft($"\r\n{SEP_1} Point {chosen_number} of No.{loop_number} {SEP_2}\r\n" +
            $"\r\nx = {_xCoor}\r\ny = {_yCoor}\r\n" + $"\r\nmod = {modulus}\r\narg = {angle}{message}\r\n");
    }
    #endregion

    #region Graphing Buttons
    private async void ConfirmButton_Click(object sender, EventArgs e) => await Async(() => RunConfirm_Click(sender, e));
    private async void PreviewButton_Click(object sender, EventArgs e) => await Async(() => RunPreview_Click(sender, e));
    private async void AllButton_Click(object sender, EventArgs e) => await Async(() =>
    {
        RunPreview_Click(sender, e);
        if (error_input) return; // Prevents a second error box from appearing
        Thread.Sleep(SLEEP); Invoke(StartTimers);
        RunConfirm_Click(sender, e);
    });
    private void RunConfirm_Click(object sender, EventArgs e) => RunClick(sender, e, GetBorders(1), true, () => Ending(MACRO));
    private void RunPreview_Click(object sender, EventArgs e) => RunClick(sender, e, GetBorders(2), false, () => Ending(MICRO));
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
            SetTextboxButtonReadOnly(false); // Required to re-enable the controls after an error
            SetScrollBars(false);
            PictureWait.Visible = false;
        }
    }
    private async Task Async(Action runClick)
    {
        RecoverParen(); if (NoInput()) return; FocusInput();
        Clipboard.SetText(MyString.BeautifyInput(InputString.Text)); // Problematic on JSX's PC
        BlockInput(true);
        try
        {
            StartTimers();
            await Task.Run(() => { Thread.CurrentThread.Priority = ThreadPriority.Highest; runClick(); });
        }
        finally { BlockInput(false); }
    }
    private void StartTimers()
    {
        display_elapsed = 0;
        SetText(TimeDisplay, "0s");
        is_flashing = false; // Delays the hourglass
        DisplayTimer.Start(); WaitTimer.Start();
        TimeNow = DateTime.Now;
    }
    private static void StopTimers()
    {
        DisplayTimer.Stop(); WaitTimer.Stop();
        TimeCount = DateTime.Now - TimeNow;
    }
    private void SetTextboxButtonReadOnly(bool readOnly)
    {
        foreach (var tbx in read_only_inputs) tbx.ReadOnly = readOnly;
        foreach (var btn in graph_buttons) btn.Enabled = !readOnly;
        activate_mouse = !readOnly;
    }
    private void PrepareSetDisplay(int[] borders, bool isMain)
    {
        (x_left, x_right, y_up, y_down, is_main) = (borders[0], borders[1], borders[2], borders[3], isMain);
        SetThicknessDensenessScopesBorders();
        DisplayInput();
    }
    private void Ending(string mode)
    {
        StopTimers();
        if (is_main) SetText(CaptionBox, $"{MyString.BeautifyInput(InputString.Text)}\r\n" + CaptionBox.Text);
        SetText(TimeDisplay, $"{TimeCount:hh\\:mm\\:ss\\.fff}");
        AddDraft($"\r\n{SEP} No.{loop_number} [{mode}] {SEP}\r\n" + $"\r\n{MyString.BeautifyInput(InputString.Text)}\r\n" +
            $"\r\nPixels: {PointNumDisplay.Text}\r\nDuration: {TimeDisplay.Text}\r\n");
        if (is_auto && !error_address) RunStore();
        FocusInput();
    }
    #endregion

    #region Export & Storage Buttons
    private void ExportButton_Click(object sender, EventArgs e) { Graph_DoubleClick(sender, e); RunExport(); }
    private void StoreButton_Click(object sender, EventArgs e) { Graph_DoubleClick(sender, e); RunStore(); }
    private void RunExport() => HandleExportStore(ExportGraph, REMIND_EXPORT);
    private void RunStore() => HandleExportStore(StoreHistory, REMIND_STORE);
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
    } // The address must fit on a single line
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
        bool temp = InputString.ReadOnly;
        InputString.ReadOnly = false; CheckAll(sender, e); InputString.ReadOnly = temp; // Sensitive
        GetInputErrorBox(message);
    }
    private void CheckValidityCore(Action errorHandler)
    {
        void correctHandler()
        {
            bool noInput = NoInput(); // Do not return immediately when NoInput() is true
            InputLabel.ForeColor = noInput ? Color.White : CORRECT_GREEN;
            InputString.BackColor = noInput ? FOCUS_GRAY : CORRECT_GREEN;
            PictureCorrect.Visible = !noInput; PictureIncorrect.Visible = false;
        }
        is_checking = true;
        try { PrepareSetDisplay(GetBorders(3), false); correctHandler(); }
        catch
        {
            CheckComplex.Checked = !CheckComplex.Checked;
            try { PrepareSetDisplay(GetBorders(3), false); correctHandler(); }
            catch { CheckComplex.Checked = !CheckComplex.Checked; errorHandler(); }
        }
    } // Sensitive
    private void CheckAll(object sender, EventArgs e) { foreach (var action in check_actions) action(sender, e); }
    private void Graph_KeyUp(object sender, KeyEventArgs e)
    {
        HandleModifierKeys(e, false);
        if (HandleSpecialKeys(e)) return;
        HandleCtrlCombination(sender, e);
    }
    private void Graph_KeyDown(object sender, KeyEventArgs e)
    {
        HandleModifierKeys(e, true);
        if (ActiveControl is TextBox tbx && math_inputs.Contains(tbx) && !InputLocked() && !String.IsNullOrEmpty(tbx.Text) &&
            sft_pressed && e.KeyCode == Keys.Back)
            ExecuteSuppress(() =>
            {
                RecoverParen();
                AddDraft("\r\nDeleted: " + tbx.Text + "\r\n");
                SetText(tbx, String.Empty);
                tbx.Focus();
            }, e);
        else if (e.KeyCode == Keys.Delete) ExecuteSuppress(null, e); // Suppresses the default Delete action
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
            Keys.Delete => handleReturn(e => { ExecuteSuppress(() => { Graph_DoubleClick(null, e); Delete_Click(e); }, e); }),
            _ => false
        };
    }
    private void HandleCtrlCombination(object sender, KeyEventArgs e)
    {
        if (!ctrl_pressed) return; RecoverParen();
        void restoreDefault(object sender, KeyEventArgs e)
        {
            ResetInputs(); ComboColoring.SelectedIndex = 4; ComboContour.SelectedIndex = 1;
            foreach (var (cbx, value) in default_checks) cbx.Checked = value;
        }
        Action? shortcutHandler = e.KeyCode switch
        {
            Keys.K => () => StoreButton_Click(sender, e),
            Keys.S => () => ExportButton_Click(sender, e),
            Keys.R => () => Graph_DoubleClick(sender, e),
            Keys.D3 => () => ClearButton_Click(sender, e),
            Keys.D2 => () => PictureLogo_DoubleClick(sender, e),
            Keys.OemQuestion => () => TitleLabel_DoubleClick(sender, e),
            Keys.D when !InputLocked() => () => restoreDefault(sender, e),
            Keys.B when !InputLocked() => () => AllButton_Click(sender, e),
            Keys.P when !InputLocked() => () => PreviewButton_Click(sender, e),
            Keys.G when !InputLocked() => () => ConfirmButton_Click(sender, e),
            Keys.C when !InputLocked() && sft_pressed => () => CheckAll(sender, e),
            _ => null
        };
        if (shortcutHandler != null) ExecuteSuppress(shortcutHandler, e);
    }
    private static void ExecuteSuppress(Action? action, KeyEventArgs e) { action?.Invoke(); e.Handled = e.SuppressKeyPress = true; }
    #endregion

    #region Dialogs
    private static void ShowBoxBase(Action<string, int, int> showMessage, string heading, string[] contents, int feed)
    {
        string content = heading, feeder = String.Concat(Enumerable.Repeat("\r\n", feed));
        for (int i = 0; i < contents.Length; i++) content += $"{feeder}{i + 1}. {contents[i]}";
        showMessage(content + "\r\n", 450, 285);
    }
    private static void ShowErrorBox(string message, string[] contents)
        => ShowBoxBase(MyMessageBox.ShowException, message + MISTAKES_HEAD + "\r\n", contents, 1);
    private static void GetInputErrorBox(string message) => ShowErrorBox(message,
    [
        "Misspelled function or variable names.",
        "Invalid special-function syntax.",
        "Extra or missing characters.",
        "Confusion between real & complex modes.",
        "Other invalid parameters."
    ]);
    private static void GetExportStoreErrorBox() => ShowErrorBox(WRONG_ADDRESS,
    [
        "The destination folder does not exist.",
        "A path ending with a backslash.",
        "A path enclosed in quotation marks.",
        "A full destination drive."
    ]);
    private static string GetComment(string input) => TAB + $"# {input}";
    private static string GetManual()
    {
        string content = $" DESIGNER:\tFraljimetry\r\n DATE:\t\t{DATE}\r\n LOCATION:\tXi'an, China";
        content += $"\r\n\r\n{TAB}This software was developed with Visual Studio 2022 and written in C# " +
            $"to visualize real and complex functions and equations involving no more than two variables." +
            $"\r\n\r\n{TAB}To enhance both visual appeal and practicality, numerous parameters can be adjusted " +
            $"to generate images for a variety of purposes." +
            $"\r\n\r\n{TAB}Note: Default variable names are case-sensitive, whereas function names are not, unless otherwise stated.";
        static string subtitleContent(string subtitle, string content) => $"\r\n\r\n{_SEP}\r\n{TAB}{subtitle}\r\n{_SEP}" + content;
        content += subtitleContent("ELEMENTS",
            $"\r\n\r\n{TAB}+ - * / ^ ( )" +
            $"\r\n\r\n{TAB}Sin, Cos, Tan, Sinh, Cosh, Tanh," +
            $"\r\n{TAB}Arcsin & Asin, Arccos & Acos, Arctan & Atan," +
            $"\r\n{TAB}Arsinh & Asinh, Arcosh & Acosh, Artanh & Atanh," +
            $"\r\n\r\n{TAB}Abs, Log & Ln, Exp, Sqrt{TAB}(f(x,y) & f(z))" +
            $"\r\n\r\n{TAB}Conjugate & Conj(f(z)), Ei(f(z)){GetComment("Ei(z) := Exp(2πiz).")}") +
            $"\r\n\r\n{TAB}Blaschke & Bla(f(z), g(z)){GetComment("Blaschke(z, w) := (z-w)/(1-Conj(w)z).")}" +
            $"\r\n\r\n{TAB}Real(...)" +
            $"{TAB}{GetComment("Variable-free real blocks in complex expressions.")}";
        content += subtitleContent("COMBINATORICS",
            $"\r\n\r\n{TAB}Floor, Ceiling & Ceil, Round, Sign & Sgn, Factorial & Fact(Real a)" +
            $"\r\n\r\n{TAB}Mod(Real a, Real n), nCr, nPr(int n, int r)" +
            $"\r\n\r\n{TAB}Max, Min, Dist(Real a, Real b, ...)");
        content += subtitleContent("SPECIALTIES",
            $"\r\n\r\n{GetComment("R&C := Real & Complex.")}" +
            $"\r\n\r\n{TAB}Hypergeo & Hypgeo(R&C a, R&C b, R&C c, f(x,y) & f(z)) & " +
            $"\r\n{TAB}Hypergeo & Hypgeo(R&C a, R&C b, R&C c, f(x,y) & f(z), int n)" +
            $"\r\n\r\n{TAB}Gamma & Ga(f(x,y) & f(z)) & " +
            $"\r\n{TAB}Gamma & Ga(f(x,y) & f(z), int n)" +
            $"\r\n\r\n{TAB}Beta(f(x,y) & f(z), g(x,y) & g(z)) & " +
            $"\r\n{TAB}Beta(f(x,y) & f(z), g(x,y) & g(z), int n)" +
            $"\r\n\r\n{TAB}Zeta(f(x,y) & f(z)) & " +
            $"\r\n{TAB}Zeta(f(x,y) & f(z), int n){GetComment("Reduced accuracy if n is too large.")}") +
            $"\r\n\r\n{TAB}Stereographic & Stereo(Real r, Real ctrX, Real ctrY, f(x,y) & f(z))" +
            $"\r\n\r\n{TAB}Homothety & Homoth(Real r, Real ctrX, Real ctrY, f(x,y) & f(z))";
        content += subtitleContent("REPETITIONS",
            $"\r\n\r\n{GetComment("Capital letters denote variable substitutions.")}" +
            $"\r\n{GetComment("\"k, int a, int b\" can be replaced by a single length \"int n\".")}" +
            $"\r\n\r\n{TAB}Sum(f(x,y,k) & f(z,k), k, int a, int b)" +
            $"\r\n{TAB}Product & Prod(f(x,y,k) & f(z,k), k, int a, int b)" +
            $"\r\n\r\n{TAB}Iterate1(f(x,y,X,k), g(x,y), k, int a, int b)" +
            $"\r\n{TAB}Iterate2(f1(x,y,X,Y,k), f2(x,y,X,Y,k), g1(x,y), g2(x,y), k, int a, int b, 1&2&F(z))" +
            $"\r\n{TAB}Iterate(f(z,Z,k), g(z), k, int a, int b, F(x,y))" +
            $"\r\n{TAB}Iterate(f(z,Z,k), g(z), k, int a, int b)" +
            $"\r\n{TAB}{GetComment("g: initial values; f: iteration rules.")}" +
            $"\r\n\r\n{TAB}Compose1 & Comp1(f(x,y), g1(x,y,X), ... , gn(x,y,X))" +
            $"\r\n{TAB}Compose2 & Comp2" +
            $"\r\n{TAB}{TAB}(f1(x,y), f2(x,y), g1(x,y,X,Y), h1(x,y,X,Y), ... , gn(...), hn(...), 1&2&F(z))" +
            $"\r\n{TAB}Compose & Comp(f(z), g1(z,Z), ... , gn(z,Z), F(x,y))" +
            $"\r\n{TAB}Compose & Comp(f(z), g1(z,Z), ... , gn(z,Z))" +
            $"\r\n{TAB}{GetComment("f: initial values; g: composition functions.")}" +
            $"\r\n\r\n{TAB}Cocoon & Coc" + "(f(x,y,{0},...,{n})&f(z,...), g0(x,y)&g0(z), ... , gn(x,y)&gn(z))" +
            $"\r\n{TAB}{GetComment("f: body; {*}: tag number *; g: tag values.")}");
        content += subtitleContent("PLANAR CURVES",
            $"\r\n\r\n{TAB}Function & Func(f(x)) & " +
            $"\r\n{TAB}Function & Func(f(x), Real increment) & " +
            $"\r\n{TAB}Function & Func(f(x), Real a, Real b) & " +
            $"\r\n{TAB}Function & Func(f(x), Real a, Real b, Real increment)" +
            $"\r\n\r\n{TAB}Polar(f(θ), θ, Real a, Real b) & " +
            $"\r\n{TAB}Polar(f(θ), θ, Real a, Real b, Real increment)" +
            $"\r\n\r\n{TAB}Parametric & Param(f(u), g(u), u, Real a, Real b) & " +
            $"\r\n{TAB}Parametric & Param(f(u), g(u), u, Real a, Real b, Real increment)");
        content += subtitleContent("RECURSIONS",
            $"\r\n\r\n{GetComment("Listed from higher to lower priorities.")}" +
            $"\r\n\r\n{TAB}... | ...{GetComment("Consecutive displays.")}") +
            $"\r\n\r\n{TAB}Substitute & Subs(Input(a,b,c,...), a, aNew, b, bNew, c, cNew, ...)" +
            $"\r\n{TAB}{GetComment("Verbatim substitutions without precomputation.")}" +
            $"\r\n\r\n{TAB}Loop(Input(k), k, int a, int b)" +
            $"\r\n\r\n{TAB}IterateLoop & ItLoop(f(x,y,X,k), g(x,y), k, int a, int b) & " +
            $"\r\n{TAB}IterateLoop & ItLoop(f(x,y,X,k), g(x,y), k, int a, int b, F(x,y,X,k)) & " +
            $"\r\n{TAB}IterateLoop & ItLoop(f1(x,y,X,Y,k), f2(...), g1(x,y), g2(x,y), k, int a, int b)" +
            $"\r\n{TAB}IterateLoop & ItLoop(f1(x,y,X,Y,k), f2(...), g1(x,y), g2(x,y), k, int a, int b, F(z,k))" +
            $"\r\n\r\n{TAB}IterateLoop & ItLoop(f(z,Z,k), g(z), k, int a, int b) & " +
            $"\r\n{TAB}IterateLoop & ItLoop(f(z,Z,k), g(z), k, int a, int b, F(z,Z,k))" +
            $"\r\n{TAB}{GetComment("Displays iterations one loop at a time.")}";
        content += subtitleContent("CONSTANTS", $"\r\n\r\n{TAB}pi, e, gamma & ga, i{GetComment("e and i are case-sensitive.")}");
        content += subtitleContent("SHORTCUTS", "\r\n");
        static string getShortcuts(string key, int blank, string meaning) => $"\r\n{TAB}[{key}]" + new string('\t', blank) + meaning;
        content += getShortcuts("Control + P", 3, "Graph in Microbox");
        content += getShortcuts("Control + G", 3, "Graph in Macrobox");
        content += getShortcuts("Control + B", 3, "Graph in Microbox & Macrobox");
        content += getShortcuts("Control + S", 3, "Save a screenshot as a PNG file");
        content += getShortcuts("Control + K", 3, "Save the history as a TXT file");
        content += getShortcuts("Control + Shift + C", 2, "Check all inputs");
        content += getShortcuts("Control + R", 3, "Clear all validation results");
        content += getShortcuts("Control + D", 3, "Restore default settings");
        content += getShortcuts("Shift + Back", 3, "Clear the input box");
        content += getShortcuts("Control + 2", 3, "View Fraljimetry's profile");
        content += getShortcuts("Control + 3", 3, "Clear all read-only displays");
        content += getShortcuts("Control + /", 3, "View the user manual");
        content += getShortcuts("Delete", 3, "Clear the Microbox & Macrobox");
        content += getShortcuts("Escape", 3, "Close Fraljiculator");
        return content + $"\r\n\r\n{GetComment("Double-click the subtitle to repaint the backdrop.")}";
    }
    private static string AddContact(string platform, int blank, string account, string note)
        => $"\r\n\r\n{TAB}{platform}:" + new string('\t', blank) + account + (note != String.Empty ? (TAB + GetComment(note)) : note);
    private static string GetProfile()
    {
        string content = "Dear math lovers and mathematicians," +
            $"\r\n\r\n{TAB}Hi! I'm Fralji, a content creator on Bilibili since July 2021, shortly before I began my first year of college." +
            $"\r\n\r\n{TAB}I aim to deliver lectures on many branches of mathematics. To learn more about my work, visit shaodaji.cc." +
            $"\r\n\r\n{TAB}If you have any questions about using this application or about mathematics, please contact me via:";
        content += AddContact("Bilibili", 2, "355884223", String.Empty);
        content += AddContact("Email", 2, "frankjiiiiiiii@gmail.com", String.Empty);
        content += AddContact("WeChat", 1, "F1r4a2n8k5y7", "Recommended");
        content += AddContact("QQ", 2, "472955101", String.Empty);
        content += AddContact("Facebook", 1, "Fraljimetry", String.Empty);
        content += AddContact("Instagram", 1, "shaodaji", "Not recommended");
        return content + "\r\n\r\n" + new string(' ', 85) + $"{DATE}";
    }
    private void TitleLabel_DoubleClick(object sender, EventArgs e) => MyMessageBox.ShowFormal(GetManual(), 720, 540);
    private void PictureLogo_DoubleClick(object sender, EventArgs e) => MyMessageBox.ShowFormal(GetProfile(), 600, 450);
    private static void ShowCustomBox(string title, string[] contents)
        => ShowBoxBase(MyMessageBox.ShowCustom, $"[{title}]" + new string(' ', 20) + $"{DATE}", contents, 2);
    private void InputLabel_DoubleClick(object sender, EventArgs e) => ShowCustomBox("INPUTBOX",
    [
        "The Space and Enter keys are both accepted. Unsupported keys are blocked during typing and removed when pasted from the clipboard.",
        "Omitting too many multiplication signs may cause misinterpretation. For example, \"gammax\" could be parsed as \"max\"."
    ]);
    private void AtLabel_DoubleClick(object sender, EventArgs e) => ShowCustomBox("ADDRESS",
    [
        "Create or select a folder for snapshot storage and paste its path here. The path will be validated immediately.",
        "PNG snapshots and history files are named using the following formats, respectively: " +
        "\"yyyy_ddd_hh_mm_ss_No.#\" and \"yyyy_ddd_hh_mm_ss_stockpile\"."
    ]);
    private void GeneralLabel_DoubleClick(object sender, EventArgs e) => ShowCustomBox("GENERAL SCOPE",
    [
        "The detailed scope takes effect only when the general scope is set to \"0\".",
        "Any valid variable-free algebraic expression is accepted and checked in the same way as the main input."
    ]);
    private void DetailLabel_DoubleClick(object sender, EventArgs e) => ShowCustomBox("DETAILED SCOPE",
    [
        "Creating a mirror effect by reversing the endpoints is not supported.",
        "Any valid variable-free algebraic expression is accepted and checked in the same way as the main input."
    ]);
    private void ThickLabel_DoubleClick(object sender, EventArgs e) => ShowCustomBox("MAGNITUDE",
    [
        "Controls\r\n (i) the widths of planar curves,\r\n (ii) the sizes of special points, and\r\n (iii) translucency decay rates.",
        "Choose a value appropriate for the current scale. The examples have been carefully tuned."
    ]);
    private void DenseLabel_DoubleClick(object sender, EventArgs e) => ShowCustomBox("DENSITY",
    [
        "Controls\r\n (i) contour density for real and complex plots\r\n (ii) the coloring periods of planar curves.",
        "Choose a value appropriate for the current scale. The examples have been carefully tuned."
    ]);
    private void DraftLabel_DoubleClick(object sender, EventArgs e) => ShowCustomBox("HISTORY LIST",
    [
        "The input is saved in this box and copied to the clipboard.",
        "Clicked points, snapshot timestamps, and other details are also recorded."
    ]);
    private void ExampleLabel_DoubleClick(object sender, EventArgs e) => ShowCustomBox("EXAMPLES",
    [
        "These examples illustrate the wide variety of supported input formats.",
        "Some renderings are elegant while others are chaotic. Elegance takes time to explore and appreciate. Enjoy!"
    ]);
    private void FunctionLabel_DoubleClick(object sender, EventArgs e) => ShowCustomBox("FUNCTIONS",
    [
        "The two combo boxes contain regular and special operations, respectively; special operations use more complex syntax.",
        "Select text in the input box, then choose an item here to replace the selection."
    ]);
    private void ModeLabel_DoubleClick(object sender, EventArgs e) => ShowCustomBox("COLORING MODES",
    [
        "These modes represent:\r\n (i) arguments of meromorphic functions,\r\n (ii) values of two-variable functions, " +
        "and\r\n (iii) parameterizations of planar curves.",
        "The first three modes support swappable color schemes, while the last two do not."
    ]);
    private void ContourLabel_DoubleClick(object sender, EventArgs e) => ShowCustomBox("CONTOUR MODES",
    [
        "These options apply only in complex mode and control the contours of meromorphic functions.",
        "Only the Polar option supports a translucent display that represents the decay of the modulus."
    ]);
    private void PointNumLabel_DoubleClick(object sender, EventArgs e) => ShowCustomBox("PIXELS",
    [
        "Shows the number of points or line segments rendered in the previous loop.",
        "This count is roughly proportional to execution time and iteration count.",
        "A value of zero often indicates numeric overflow or unsuitable settings."
    ]);
    private void TimeLabel_DoubleClick(object sender, EventArgs e) => ShowCustomBox("DURATION",
    [
        "Automatic snapshots may not capture updates here in real time, but the elapsed time is saved in the history list.",
        "This value helps evaluate optimization and choose suitable iteration counts and other settings."
    ]);
    private void PreviewLabel_DoubleClick(object sender, EventArgs e) => ShowCustomBox("MICROCOSM",
    [
        "Because graphing cannot be paused manually during execution, the preview helps estimate computation time.",
        "The preview differs from the main graph only in resolution.",
        "It renders roughly 20 times faster, although the speedup may be smaller after optimization."
    ]);
    #endregion

    #region Index Change & Check Change
    private void SetValuesForSelectedIndex(int index)
    {
        (int iC, string sG, (string xL, string xR, string yL, string yR) sD, (string T, string D) sO, (bool, bool, bool, bool) bC) set;
        var (L1, L2, L3) = (ReplaceTags.EX_COMPLEX.Length, ReplaceTags.EX_REAL.Length, ReplaceTags.EX_CURVES.Length);

        InputString.ReadOnly = true;
        if (index < L1) set = index switch
        {
            0 => (1, "1.1", ("", "", "", ""), (THICK_DEFAULT, DENSE_DEFAULT), (false, true, false, false)),
            1 => (3, "1.2", ("", "", "", ""), (THICK_DEFAULT, DENSE_DEFAULT), (true, false, false, false)),
            2 => (3, "1.1", ("", "", "", ""), (THICK_DEFAULT, DENSE_DEFAULT), (true, false, false, false)),
            3 => (4, "pi/2", ("", "", "", ""), (THICK_DEFAULT, DENSE_DEFAULT), (true, false, false, false)),
            4 => (3, "pi", ("", "", "", ""), (THICK_DEFAULT, DENSE_DEFAULT), (true, false, false, false)),
            5 => (3, "1.5", ("", "", "", ""), (THICK_DEFAULT, DENSE_DEFAULT), (true, false, false, false)),
            6 => (3, "0", ("-1.6", "0.6", "-1.1", "1.1"), ("100", DENSE_DEFAULT), (false, false, true, true)),
            7 => (4, "2", ("", "", "", ""), (THICK_DEFAULT, "pi/2"), (true, false, false, false))
        };
        else if (index > L1 && index < L1 + L2 + 1) set = (index - L1 - 1) switch
        {
            0 => (2, "10", ("", "", "", ""), (THICK_DEFAULT, DENSE_DEFAULT), (true, false, false, false)),
            1 => (4, "2pi", ("", "", "", ""), (THICK_DEFAULT, DENSE_DEFAULT), (true, false, false, false)),
            2 => (3, "5", ("", "", "", ""), (THICK_DEFAULT, DENSE_DEFAULT), (true, false, false, false)),
            3 => (0, "5", ("", "", "", ""), (THICK_DEFAULT, DENSE_DEFAULT), (false, false, false, true)),
            4 => (1, "e", ("", "", "", ""), ("0.1", DENSE_DEFAULT), (true, true, false, false)),
            5 => (3, "3pi/2", ("", "", "", ""), ("0.5", DENSE_DEFAULT), (false, false, true, false)),
            6 => (0, "0", ("0", "1", "0", "1"), ("0.2", DENSE_DEFAULT), (true, false, false, true)),
            7 => (4, "2", ("", "", "", ""), ("5", DENSE_DEFAULT), (false, false, true, false))
        };
        else if (index > L1 + L2 + 1 && index < L1 + L2 + L3 + 2) set = (index - L1 - L2 - 2) switch
        {
            0 => (0, "5", ("", "", "", ""), ("0.5", DENSE_DEFAULT), (true, false, false, false)),
            1 => (0, "1.5", ("", "", "", ""), ("0.5", DENSE_DEFAULT), (true, false, false, false)),
            2 => (2, "0", ("0", "1", "0", "1"), ("0.5", "8"), (true, false, false, false)),
            3 => (0, "1.1", ("", "", "", ""), ("0.5", DENSE_DEFAULT), (true, false, false, false)),
            4 => (3, "1.1", ("", "", "", ""), ("0.5", DENSE_DEFAULT), (true, false, false, false)),
            5 => (3, "1.1", ("", "", "", ""), ("0.5", DENSE_DEFAULT), (true, false, false, true)),
            6 => (3, "1.1", ("", "", "", ""), ("0.5", DENSE_DEFAULT), (true, false, false, false)),
            7 => (3, "0", ("-0.2", "1.2", "-0.2", "1.2"), ("0.5", DENSE_DEFAULT), (true, false, false, true))
        };
        else { ClearExampleSelection(); InputString.ReadOnly = false; return; }

        ComboColoring.SelectedIndex = set.iC;
        SetText(GeneralInput, set.sG); SetText(ThickInput, set.sO.T); SetText(DenseInput, set.sO.D);
        SetText(X_Left, set.sD.xL); SetText(X_Right, set.sD.xR); SetText(Y_Left, set.sD.yL); SetText(Y_Right, set.sD.yR);
        (CheckCoor.Checked, CheckPoints.Checked, CheckShade.Checked, CheckRetain.Checked) = set.bC;
        InputString.ReadOnly = false;
    }
    private void ComboFS_SelectionChanged(ComboBox cbx)
    {
        string selectedItem = cbx.SelectedItem.ToString();
        if (InputLocked()) return; int pos = InputString.SelectionStart;
        SetText(InputString, MyString.Replace(InputString.Text, String.Concat(selectedItem, ImplicitMultiply.EMPTY_PARENS),
            pos, pos + InputString.SelectionLength - 1));
        InputString.Focus(); SetCaret(InputString, pos + selectedItem.Length + 1); // Must remain after .Focus()
    }
    private void ComboExamples_SelectedIndexChanged(object sender, EventArgs e)
    {
        string? selection = ComboExamples.SelectedItem?.ToString();
        if (InputLocked() || String.IsNullOrEmpty(selection) || ComboExamples.SelectedIndex == -1) return;
        SetValuesForSelectedIndex(ComboExamples.SelectedIndex);
        SetText(InputString, selection);
        ClearExampleSelection(); // Prevents repeated calls
        Delete_Click(e);
        FocusInput();
    }
    private void ComboFunctions_SelectedIndexChanged(object sender, EventArgs e) => ComboFS_SelectionChanged(ComboFunctions);
    private void ComboSpecial_SelectedIndexChanged(object sender, EventArgs e) => ComboFS_SelectionChanged(ComboSpecial);
    private static int ComboCC_SelectionChanged(ComboBox cbx) => AddOne(cbx.SelectedIndex);
    private void ComboColoring_SelectedIndexChanged(object sender, EventArgs e) => color_mode = ComboCC_SelectionChanged(ComboColoring);
    private void ComboContour_SelectedIndexChanged(object sender, EventArgs e) => contour_mode = ComboCC_SelectionChanged(ComboContour);
    //
    private void CheckComplex_CheckedChanged(object sender, EventArgs e) => ToggleBool(ref is_complex);
    private void CheckSwap_CheckedChanged(object sender, EventArgs e) => ToggleBool(ref swap_colors);
    private void CheckCoor_CheckedChanged(object sender, EventArgs e) => ToggleBool(ref delete_coor);
    private void CheckPoints_CheckedChanged(object sender, EventArgs e) => ToggleBool(ref delete_point);
    private void CheckShade_CheckedChanged(object sender, EventArgs e) => ToggleBool(ref shade);
    private void CheckRetain_CheckedChanged(object sender, EventArgs e) => ToggleBool(ref freeze_graph);
    private void CheckAuto_CheckedChanged(object sender, EventArgs e) => ToggleBool(ref is_auto);
    private void CheckEdit_CheckedChanged(object sender, EventArgs e)
    {
        DraftBox.ReadOnly = !DraftBox.ReadOnly; // Properties cannot be passed by reference
        DraftBox.BackColor = DraftBox.ReadOnly ? Color.Black : SystemColors.ControlDarkDark;
        DraftBox.ForeColor = DraftBox.ReadOnly ? READONLY_GRAY : Color.White;
        DraftBox.ScrollBars = DraftBox.ReadOnly ? ScrollBars.None : ScrollBars.Vertical;
    }
    #endregion

    // 4. SPECIAL EFFECTS
    #region Click & Mouse Down & Text Changed
    private void Delete_Click(int[] borders, bool isMain)
    {
        if (InputLocked()) return;
        Details_TextChanged(null, EventArgs.Empty); // Ensures that the axes and grids are drawn correctly
        ClearBitmap(GetBitmap(isMain));
        Invalidate(isMain ? rect_mac : rect_mic); Update(); // Clears curves that extend beyond the display bounds
        DrawBackdropAxesGrids(borders, isMain);
    } // Sensitive
    private void Delete_Click(EventArgs e) { DeleteMain_Click(this, e); DeletePreview_Click(this, e); }
    private void DeleteMain_Click(object sender, EventArgs e) => Delete_Click(GetBorders(1), true);
    private void DeletePreview_Click(object sender, EventArgs e) => Delete_Click(GetBorders(2), false);
    private void ClearButton_Click(object sender, EventArgs e)
    {
        loop_number = chosen_number = 0;
        foreach (var tbx in clear_outputs) SetText(tbx, String.Empty);
        FocusInput();
    }
    private void PictureIncorrect_Click(object sender, EventArgs e)
    { if (!InputLocked()) CheckValidityCore(() => InputErrorBox(sender, e, WRONG_FORMAT)); }
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
        foreach (var lbl in reset_labels) lbl.ForeColor = Color.White;
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
    private static bool RemoveSomeKeys(TextBox tbx)
    {
        int caretPosition = tbx.Text.Length - tbx.SelectionStart - tbx.SelectionLength; // Necessary
        string text = tbx.Text;
        foreach (char c in ImplicitMultiply.BARRED_CHARS) text = text.Replace(c, ' ');
        if (text == tbx.Text) return false;
        SetText(tbx, text); SetCaret(tbx, tbx.Text.Length - caretPosition);
        return true;
    }
    private void MiniChecks(TextBox[] textBoxes, Label lbl)
    {
        try
        {
            if (InputLocked()) return; bool noSomeInput = false;
            foreach (var tbx in textBoxes)
            {
                bool noInput = String.IsNullOrEmpty(tbx.Text); noSomeInput = noSomeInput || noInput;
                if (!noInput) Obtain(tbx); // For checking
            }
            lbl.ForeColor = noSomeInput ? Color.White : CORRECT_GREEN; // White if any input is null or empty
        }
        catch (Exception) { lbl.ForeColor = ERROR_RED; }
    }
    private void MiniChecks(TextBox tbx, Label lbl) { if (InputLocked() || RemoveSomeKeys(tbx)) return; MiniChecks([tbx], lbl); }
    private void Details_TextChanged(object sender, EventArgs e)
    {
        if (InputLocked()) return;
        if (sender is TextBox tbx && RemoveSomeKeys(tbx)) return;
        MiniChecks(detail_inputs, DetailLabel);
        if (scopes == null) return; // Required during initialization
        void checkScopes(bool b1, bool b2, Color c) { if (b1) X_Scope.ForeColor = c; if (b2) Y_Scope.ForeColor = c; }

        try { SetThicknessDensenessScopesBorders(false); }
        catch (Exception) { checkScopes(InvalidScopesX(), InvalidScopesY(), ERROR_RED); }
        finally { checkScopes(!InvalidScopesX(), !InvalidScopesY(), CORRECT_GREEN); } // Keep the Boolean expressions inline
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
        if (InputLocked() || RemoveSomeKeys(InputString)) return;
        CheckValidityCore(() =>
        {
            InputString.BackColor = InputLabel.ForeColor = ERROR_RED;
            PictureIncorrect.Visible = true; PictureCorrect.Visible = false;
        });
        if (PictureCorrect.Visible) DisplayMouseMoveCore(); // Displays the value in the lower-right corner
    }
    private void AddressInput_TextChanged(object sender, EventArgs e)
    {
        if (InputLocked()) return;
        if (String.IsNullOrEmpty(AddressInput.Text)) AtLabel.ForeColor = Color.White;
        else AtLabel.ForeColor = Directory.Exists(AddressInput.Text) ? CORRECT_GREEN : ERROR_RED;
    }
    //
    private static void BanDoubleClick(TextBox tbx, MouseEventArgs e) // Suppresses the default selection behavior
    { tbx.SelectionStart = tbx.GetCharIndexFromPosition(e.Location); tbx.SelectionLength = 0; }
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
    { if (ImplicitMultiply.BARRED_CHARS.Contains(e.KeyChar)) e.Handled = true; }
    private void InputString_KeyPress(object sender, KeyPressEventArgs e) => BarSomeKeys(sender, e);
    private void GeneralInput_KeyPress(object sender, KeyPressEventArgs e) => BarSomeKeys(sender, e);
    private void X_Left_KeyPress(object sender, KeyPressEventArgs e) => BarSomeKeys(sender, e);
    private void X_Right_KeyPress(object sender, KeyPressEventArgs e) => BarSomeKeys(sender, e);
    private void Y_Left_KeyPress(object sender, KeyPressEventArgs e) => BarSomeKeys(sender, e);
    private void Y_Right_KeyPress(object sender, KeyPressEventArgs e) => BarSomeKeys(sender, e);
    private void ThickInput_KeyPress(object sender, KeyPressEventArgs e) => BarSomeKeys(sender, e);
    private void DenseInput_KeyPress(object sender, KeyPressEventArgs e) => BarSomeKeys(sender, e);
    //
    private static void SetParen(char left, char right)
    {
        char[] text = paren_tbx!.Text.ToCharArray(); text[paren_left] = left; text[paren_right] = right;
        paren_change = true; try { SetText(paren_tbx, new(text)); } finally { paren_change = false; }
    } // Strings are immutable
    private static void RecoverParen()
    {
        if (paren_tbx == null) return;
        int pos = paren_tbx.SelectionStart;

        bool recovered = paren_left >= 0 && paren_right >= 0 &&
            paren_left < paren_tbx.Text.Length && paren_right < paren_tbx.Text.Length &&
            paren_tbx.Text[paren_left] == '[' && paren_tbx.Text[paren_right] == ']';

        if (recovered) SetParen('(', ')');
        SetCaret(paren_tbx, pos);
        if (recovered) paren_tbx.Refresh();

        paren_tbx = null; paren_left = paren_right = -1;
    }
    private static void ShowParen(TextBox tbx)
    {
        if (!tbx.Focused || tbx.ReadOnly) return;
        RecoverParen(); int pos = tbx.SelectionStart - 1;
        if (pos < 0 || tbx.SelectionLength > 0 || !MyString.HasBalancedParen(tbx.Text)) return;

        char c = tbx.Text[pos]; if (c != '(' && c != ')') return;
        int match = c == '(' ? MyString.FindMatchingParen(tbx.Text, pos) : MyString.FindMatchingParenBack(tbx.Text, pos);

        paren_tbx = tbx;
        (paren_left, paren_right) = c == '(' ? (pos, match) : (match, pos);
        SetParen('[', ']'); SetCaret(tbx, pos + 1);
    }
    private static void SnapSeparatorCaret(TextBox tbx)
    {
        if (tbx.SelectionLength > 0) return;
        int pos = tbx.SelectionStart; if (pos <= 0 || pos >= tbx.Text.Length) return;
        if (tbx.Text[pos - 1] == ',' && tbx.Text[pos] == ' ') tbx.SelectionStart = pos + 1;
        else if (pos + 1 < tbx.Text.Length && tbx.Text[pos - 1] == ' ' && tbx.Text[pos] == '|' && tbx.Text[pos + 1] == ' ')
            tbx.SelectionStart = pos - 1;
        else if (pos >= 2 && tbx.Text[pos - 2] == ' ' && tbx.Text[pos - 1] == '|' && tbx.Text[pos] == ' ')
            tbx.SelectionStart = pos + 1;
    }
    private static void AutoKeyDown(TextBox tbx, KeyEventArgs e)
    {
        if (tbx.ReadOnly) return;
        RecoverParen(); tbx.BeginInvoke(() => ShowParen(tbx));

        int caretPos = tbx.SelectionStart, selectionLength = tbx.SelectionLength;
        bool shift = (ModifierKeys & Keys.Shift) != 0;

        void selectSuppress(int pos)
        {
            SetCaret(tbx, caretPos + pos);
            e.SuppressKeyPress = true;
        }

        void insertSelectSuppress(string insertion, int pos)
        {
            SetText(tbx, tbx.Text.Insert(caretPos, insertion));
            selectSuppress(pos);
        }

        char obtainLeft() => e.KeyCode switch { Keys.D9 => '(', Keys.OemOpenBrackets => '{' };
        char obtainRight(char left) => left switch { '(' => ')', '{' => '}' };

        if (!MyString.HasBalancedParen(tbx.Text.AsSpan(caretPos, selectionLength))) selectSuppress(0);

        else if (shift && (e.KeyCode == Keys.D9 || e.KeyCode == Keys.OemOpenBrackets))
        {
            char left = obtainLeft(), right = obtainRight(left);

            if (selectionLength == 0) insertSelectSuppress($"{left}{right}", 1);
            else
            {
                string selectedText = tbx.Text.Substring(caretPos, selectionLength),
                    insertion = $"{left}{selectedText}{right}";

                SetText(tbx, MyString.Replace(tbx.Text, insertion,
                    caretPos, caretPos + selectionLength - 1));

                selectSuppress(insertion.Length);
            }
        }

        else if (shift && (e.KeyCode == Keys.D0 || e.KeyCode == Keys.OemCloseBrackets))
        {
            if (selectionLength > 0) selectSuppress(0);
            else if (caretPos == 0) selectSuppress(0);
            else if (caretPos < tbx.Text.Length &&
                ImplicitMultiply.IsOpeningBracket(tbx.Text[caretPos - 1]) &&
                tbx.Text[caretPos] == obtainRight(tbx.Text[caretPos - 1]))
                selectSuppress(1);
        }

        else if (e.KeyCode == Keys.Oemcomma && !shift)
            insertSelectSuppress(", ", 2);

        else if (e.KeyCode == Keys.OemPipe && shift)
            insertSelectSuppress(" | ", 3);

        else if (e.KeyCode == Keys.Back)
        {
            if (caretPos == 0 || selectionLength > 0) return;

            bool pipe = caretPos >= 3 &&
                tbx.Text[caretPos - 3] == ' ' &&
                tbx.Text[caretPos - 2] == '|' &&
                tbx.Text[caretPos - 1] == ' ',

                comma = caretPos >= 2 &&
                tbx.Text[caretPos - 2] == ',' &&
                tbx.Text[caretPos - 1] == ' ';

            if (pipe || comma)
            {
                int length = pipe ? 3 : 2;
                SetText(tbx, tbx.Text.Remove(caretPos - length, length));
                selectSuppress(-length);
            }
            else
            {
                if (!MyString.HasBalancedParen(tbx.Text)) return;
                char c = tbx.Text[caretPos - 1];

                if (ImplicitMultiply.IsOpeningBracket(c))
                {
                    if (caretPos < tbx.Text.Length && tbx.Text[caretPos] == obtainRight(c))
                        SetText(tbx, tbx.Text.Remove(caretPos - 1, 2));

                    selectSuppress(-1);
                }
                else if (ImplicitMultiply.IsClosingBracket(c))
                    selectSuppress(-1);
            }
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
    private static void Combo_KeyDown(KeyEventArgs e) // Suppresses the default keyboard search
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
    { tbx.BackColor = CTRL_GRAY; tbx.ForeColor = Color.White; RecoverFont(lbl); }
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
        if (!DraftBox.ReadOnly) DraftBox.BackColor = CTRL_GRAY;
        DraftBox.ForeColor = DraftBox.ReadOnly ? READONLY_GRAY : Color.White;
        DraftLabel.ForeColor = Color.White;
    }
    //
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
    //
    private static void ReadOnlyHover(Label lbl, TextBox tbx) { lbl.ForeColor = READONLY_PURPLE; tbx.ForeColor = Color.White; }
    private static void ReadOnlyLeave(Label lbl, TextBox tbx) { lbl.ForeColor = Color.White; tbx.ForeColor = READONLY_GRAY; }
    private void PointNumDisplay_MouseHover(object sender, EventArgs e) => ReadOnlyHover(PointNumLabel, PointNumDisplay);
    private void PointNumDisplay_MouseLeave(object sender, EventArgs e) => ReadOnlyLeave(PointNumLabel, PointNumDisplay);
    private void TimeDisplay_MouseHover(object sender, EventArgs e) => ReadOnlyHover(TimeLabel, TimeDisplay);
    private void TimeDisplay_MouseLeave(object sender, EventArgs e) => ReadOnlyLeave(TimeLabel, TimeDisplay);
    private void X_CoorDisplay_MouseHover(object sender, EventArgs e) => ReadOnlyHover(X_Coor, X_CoorDisplay);
    private void X_CoorDisplay_MouseLeave(object sender, EventArgs e) => ReadOnlyLeave(X_Coor, X_CoorDisplay);
    private void Y_CoorDisplay_MouseHover(object sender, EventArgs e) => ReadOnlyHover(Y_Coor, Y_CoorDisplay);
    private void Y_CoorDisplay_MouseLeave(object sender, EventArgs e) => ReadOnlyLeave(Y_Coor, Y_CoorDisplay);
    private void ModulusDisplay_MouseHover(object sender, EventArgs e) => ReadOnlyHover(Modulus, ModulusDisplay);
    private void ModulusDisplay_MouseLeave(object sender, EventArgs e) => ReadOnlyLeave(Modulus, ModulusDisplay);
    private void AngleDisplay_MouseHover(object sender, EventArgs e) => ReadOnlyHover(Angle, AngleDisplay);
    private void AngleDisplay_MouseLeave(object sender, EventArgs e) => ReadOnlyLeave(Angle, AngleDisplay);
    private void FunctionDisplay_MouseHover(object sender, EventArgs e) => ReadOnlyHover(ValueLabel, FunctionDisplay);
    private void FunctionDisplay_MouseLeave(object sender, EventArgs e) => ReadOnlyLeave(ValueLabel, FunctionDisplay);
    //
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
    //
    private static void ResizeControl(PictureBox pbx, int delta, bool isLarge)
    {
        if (isLarge ? is_resized : !is_resized) return; // Prevents repeated calls
        var (_location, _size) = isLarge ? (-delta, 2 * delta) : (delta, -2 * delta);
        pbx.Location = new(pbx.Location.X + _location, pbx.Location.Y + _location);
        pbx.Size = new(pbx.Width + _size, pbx.Height + _size);
        is_resized = isLarge;
    }
    private static void EnlargePicture(PictureBox pbx, int increment) => ResizeControl(pbx, increment, true);
    private static void ShrinkPicture(PictureBox pbx, int decrement) => ResizeControl(pbx, decrement, false);
    private void PictureLogo_MouseHover(object sender, EventArgs e) => EnlargePicture(PictureLogo, 5);
    private void PictureLogo_MouseLeave(object sender, EventArgs e) => ShrinkPicture(PictureLogo, 5);
    private void PictureIncorrect_MouseHover(object sender, EventArgs e) => EnlargePicture(PictureIncorrect, 2);
    private void PictureIncorrect_MouseLeave(object sender, EventArgs e) => ShrinkPicture(PictureIncorrect, 2);
    //
    private void ExportButton_MouseHover(object sender, EventArgs e) => AddressInput_DoubleClick(sender, e);
    private void StoreButton_MouseHover(object sender, EventArgs e) => AddressInput_DoubleClick(sender, e);
    #endregion
} /// Provides the visualization interface
public class MyMessageBox : Form
{
    #region Fields
    private static Button btnOk;
    private static TextBox txtMessage;
    private static readonly Color BACKDROP_GRAY = Graph.Argb(64, 64, 64),
        FORMAL_FONT = Graph.Argb(224, 224, 224), CUSTOM_FONT = Color.Turquoise, EXCEPTION_FONT = Color.LightPink,
        FORMAL_BUTTON = Color.Black, CUSTOM_BUTTON = Color.DarkBlue, EXCEPTION_BUTTON = Color.DarkRed;

    private static Real scale_factor;
    private static readonly Real MSG_TXT_SIZE = 10, BTN_TXT_SIZE = 7;
    private static readonly int DIST = 10, BTN_SIZE = 25, BORDER = 10; // DIST = dist(btnOk, txtMessage)
    private static bool is_resized;
    private static readonly string MSG_FONT = "Segoe UI", BTN_FONT = "Microsoft YaHei UI", BTN_TXT = "OK";
    #endregion

    #region Methods
    private static void BtnOk_MouseEnterLeave(bool isEnter)
    {
        if (isEnter ? is_resized : !is_resized) return; // Prevents repeated calls
        var (_size, _location, _font) = isEnter ? (2, -1, 1) : (-2, 1, -1);
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
        FormBorderStyle = FormBorderStyle.None; Size = new(width, height);
        StartPosition = FormStartPosition.CenterScreen; BackColor = SystemColors.ControlDark;
    }
    private static void SetUpTextBox(string message, int width, int height, Color txtColor)
    {
        txtMessage = new()
        {
            Text = message,
            Font = new(MSG_FONT, (float)MSG_TXT_SIZE, FontStyle.Regular),
            ForeColor = txtColor,
            Multiline = true,
            ReadOnly = true,
            BorderStyle = BorderStyle.None,
            BackColor = BACKDROP_GRAY,
            ScrollBars = ScrollBars.Vertical
        };
        txtMessage.SetBounds(BORDER, BORDER, width - BORDER * 2, height - BORDER - 2 * DIST - BTN_SIZE);
        txtMessage.SelectionStart = message.Length; txtMessage.SelectionLength = 0;
        txtMessage.GotFocus += (sender, e) => { Graph.HideCaret(txtMessage.Handle); };
    }
    private void SetUpButton(int width, int height, Color btnColor, Color btnTxtColor)
    {
        btnOk = new()
        {
            Size = new(BTN_SIZE * 2, BTN_SIZE),
            Location = new(width / 2 - BTN_SIZE, height - DIST - BTN_SIZE),
            BackColor = btnColor,
            ForeColor = btnTxtColor,
            Font = new(BTN_FONT, (float)BTN_TXT_SIZE, FontStyle.Bold),
            FlatStyle = FlatStyle.Flat,
            Text = BTN_TXT,
        };
        btnOk.FlatAppearance.BorderSize = 0; btnOk.Click += (sender, e) => { Close(); };
        btnOk.MouseEnter += BtnOk_MouseEnter; btnOk.MouseLeave += BtnOk_MouseLeave;
    }
    private void Setup(string message, int width, int height, Color txtColor, Color btnColor, Color btnTxtColor)
    {
        SetUpForm(width, height); SetUpTextBox(message, width, height, txtColor); SetUpButton(width, height, btnColor, btnTxtColor);
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
    #endregion
} /// Constructs custom message boxes
