/// https://github.com/Fraljimetry/Fraljiculator/blob/main/Form1.cs

using System.Buffers;                 // ArrayPool
using System.Drawing.Imaging;         // BitmapData
using System.Runtime.CompilerServices;// RuntimeHelpers, Unsafe
using System.Runtime.InteropServices; // DllImport, StructLayout
using System.Text;                    // StringBuilder

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
    private static System.Windows.Forms.Timer GraphTimer, WaitTimer, DisplayTimer;
    private static Graphics graphics;
    private static Rectangle rectangle, rect_mac, rect_mic; // rect_: slightly larger than the display regions
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
    private static int display_elapsed, x_left, x_right, y_up, y_down, color_mode, contour_mode,
        loop_number, chosen_number, export_number, pixel_number, segment_number;
    private static readonly int X_LEFT_MAC = 620, X_RIGHT_MAC = 1520, Y_UP_MAC = 45, Y_DOWN_MAC = 945,
        X_LEFT_MIC = 1565, X_RIGHT_MIC = 1765, Y_UP_MIC = 745, Y_DOWN_MIC = 945, X_LEFT_CHECK = 1921, X_RIGHT_CHECK = 1922,
        Y_UP_CHECK = 1081, Y_DOWN_CHECK = 1082, REF_POS_1 = 9, REF_POS_2 = 27, WIDTH_IND = 22, HEIGHT_IND = 55,
        LEFT_SUPP = 11, TOP_SUPP = 45, GRID = 5, UPDATE = 5, REFRESH = 100, SLEEP = 200, THRESHOLD = 1000;
    private static Real[] scopes; // Corresponds to tbxDetails = [X_Left, X_Right, Y_Left, Y_Right]
    private static int[] borders; // = [x_left, x_right, y_up, y_down]
    private static Matrix<Complex> output_complex;
    private static Matrix<Real> output_real;
    //
    private static bool is_flashing, is_complex = true, delete_point = true, delete_coor, swap_colors, is_auto, freeze_graph,
        clicked, shade, axes_drawn_mac, axes_drawn_mic, is_main, activate_mouse, is_checking, error_input, error_address, is_resized,
        ctrl_pressed, sft_pressed, suppress_key_up, bdp_painted;
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

    #region Initilizations
    public Graph()
    {
        InitializeComponent(); SetTitleBarColor(); ReduceFontSizeByScale(this, ref scale_factor); BanMouseWheel();
        InitializeTimers(); InitializeGraphics(); InitializeCombo(); InitializeData(); SetThicknessDensenessScopesBorders();
    }
    private void Graph_Load(object sender, EventArgs e) => TextBoxFocus(sender, e);
    private void Graph_Paint(object sender, PaintEventArgs e) { if (!bdp_painted && !clicked) SubtitleBox_DoubleClick(sender, e); }
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
    private void BanMouseWheel()
    {
        ComboBox[] comboBoxes = [ComboExamples, ComboFunctions, ComboSpecial, ComboColoring, ComboContour];
        foreach (var cbx in comboBoxes) cbx.MouseWheel += (sender, e) => ((HandledMouseEventArgs)e).Handled = true;
    } // Default mouse-wheel behavior conflicts with the custom combo boxes
    private void InitializeTimers()
    {
        static System.Windows.Forms.Timer setT(int interval) => new() { Interval = interval };
        GraphTimer = setT(1000); WaitTimer = setT(500); DisplayTimer = setT(1000 / UPDATE);
        WaitTimer.Tick += (sender, e) =>
        {
            ReverseBool(ref is_flashing); // Properties cannot be passed by reference
            PictureWait.Visible = is_flashing;
        };
        DisplayTimer.Tick += (sender, e) =>
        {
            if (++display_elapsed % UPDATE == 0) SetText(TimeDisplay, (display_elapsed / UPDATE).ToString() + "s");
            SetText(PointNumDisplay, (pixel_number + segment_number).ToString()); // Refreshes $"{RATE}" times per second
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
        coloringContour_AddItem(ComboColoring, 4, COLOR_MODES); coloringContour_AddItem(ComboContour, 1, CONTOUR_MODES);

        void addExamples(string[] items) { foreach (string item in items) ComboExamples.Items.Add(item); }
        addExamples(ReplaceTags.EX_COMPLEX); ComboExamples.Items.Add(String.Empty);
        addExamples(ReplaceTags.EX_REAL); ComboExamples.Items.Add(String.Empty);
        addExamples(ReplaceTags.EX_CURVES); ComboExamples.Items.Add(String.Empty);

        void functionsSpecial_AddItem(string[] options, bool isFunc)
        {
            string[] modifiedOptions = new string[options.Length]; int index = 0;
            foreach (string option in options) modifiedOptions[index++] = option;
            Action<string[]> addOptions = isFunc ? ComboFunctions.Items.AddRange : ComboSpecial.Items.AddRange;
            addOptions(modifiedOptions);
        }
        functionsSpecial_AddItem(ReplaceTags.FUNCTIONS, true); functionsSpecial_AddItem(ReplaceTags.SPECIALS, false);
    }
    private void RecoverInput()
    {
        SetText(InputString, INPUT_DEFAULT); SetText(AddressInput, ADDRESS_DEFAULT);
        SetText(GeneralInput, GENERAL_DEFAULT); SetText(ThickInput, THICK_DEFAULT); SetText(DenseInput, DENSE_DEFAULT);
        InputString_Focus();
    }
    private void InitializeData() { RecoverInput(); SetText(DraftBox, DRAFT_DEFAULT); SetText(CaptionBox, CAPTION_DEFAULT); }
    private void SetThicknessDensenessScopesBorders(bool autoFill = true)
    {
        FillEmpty(GeneralInput, GENERAL_DEFAULT); FillEmpty(ThickInput, THICK_DEFAULT); FillEmpty(DenseInput, DENSE_DEFAULT);
        TextBox[] tbxDetails = [X_Left, X_Right, Y_Left, Y_Right]; // Crucial ordering
        if (autoFill) foreach (var tbx in tbxDetails) FillEmpty(tbx, ZERO);

        Real _dense = Obtain(DenseInput), _thick = Obtain(ThickInput);
        stride_real = STRIDE_REAL / _dense; stride = STRIDE / _dense; mod_stride = MOD / _dense; arg_stride = ARG / _dense;
        epsilon = (is_complex ? EPS_COMPLEX : EPS_REAL) * _thick; size_real = SIZE_REAL * _thick / (1 + _thick); decay = DECAY * _thick;

        if (!GeneralInput_Undo())
        {
            Real _scope = Obtain(GeneralInput);
            scopes = [-_scope, _scope, -_scope, _scope]; // Note the signs
            for (int i = 0; i < tbxDetails.Length; i++) SetText(tbxDetails[i], scopes[i].ToString("0.################"));
        } // Never use scientific notation
        else for (int i = 0; i < tbxDetails.Length; i++) scopes[i] = Obtain(tbxDetails[i]);
        MyString.ThrowException(InvalidScopesX() || InvalidScopesY()); // A more specific exception is determined later
        borders = [x_left, x_right, y_up, y_down];
    }
    private void TextBoxFocus(object sender, EventArgs e)
    {
        foreach (var ctrl in Controls.OfType<TextBox>())
            ctrl.GotFocus += (sender, e) => { ((TextBox)sender).SelectionStart = ((TextBox)sender).Text.Length; };
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
    public static Real ArgRGB(Real x, Real y) => Real.IsNaN(x) && Real.IsNaN(y) ? -1 : y == 0 ?
        (x == 0 ? -1 : x > 0 ? 0 : MathR.PI) : (y > 0 ? MathR.Atan2(y, x) : MathR.Atan2(y, x) + MathR.Tau); // Numerically sensitive check
    private static int Frac(int input, Real alpha) => (int)(input * alpha);
    private static bool IllegalRatio(Real ratio) => ratio < 0 || ratio > 1;
    private static int RowBorders(int[] borders) => borders[1] - borders[0];
    private static int ColumnBorders(int[] borders) => borders[3] - borders[2];
    private static Real RowScopes() => scopes[1] - scopes[0];
    private static Real ColumnScopes() => scopes[3] - scopes[2]; // Sign conventions vary across the codebase
    private static bool InvalidScopesX() => scopes[0] >= scopes[1];
    private static bool InvalidScopesY() => scopes[2] >= scopes[3];
    private static int[] GetBorders(int mode) => mode switch
    {
        1 => [X_LEFT_MAC, X_RIGHT_MAC, Y_UP_MAC, Y_DOWN_MAC],
        2 => [X_LEFT_MIC, X_RIGHT_MIC, Y_UP_MIC, Y_DOWN_MIC],
        3 => [X_LEFT_CHECK, X_RIGHT_CHECK, Y_UP_CHECK, Y_DOWN_CHECK]
    };
    private static Matrix<Real> GetMatrix(int rows, int columns) => new(RealComplex.GetArithProg(rows, columns), columns);
    private static Rectangle GetRect(int[] borders, int margin = 0)
        => new(borders[0] + margin, borders[2] + margin, RowBorders(borders) - margin, ColumnBorders(borders) - margin);
    private static Bitmap GetBitmap(bool isMain) => isMain ? bmp_mac : bmp_mic;
    private static ref bool ReturnAxesDrawn(bool isMain) => ref (isMain ? ref axes_drawn_mac : ref axes_drawn_mic);
    private static void SetAxesDrawn(bool isMain, bool drawn = false) { ReturnAxesDrawn(isMain) = drawn; }
    private static void ReverseBool(ref bool isChecked) => isChecked = !isChecked;
    private static Real Obtain(string text) => RealSub.Obtain(RecoverMultiply.Simplify(text));
    private static Real Obtain(TextBox tbx) => Obtain(tbx.Text);
    private static void SetText(TextBox tbx, string text) => tbx.Text = text;
    private static void FillEmpty(TextBox tbx, string text) { if (String.IsNullOrEmpty(tbx.Text)) SetText(tbx, text); }
    private static bool ContainsTag(string input, string tag)
        => input.Contains(String.Concat(ReplaceTags.FUNC_HEAD, tag, ReplaceTags.SERIES_TAIL, '('));
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
        bdp_painted = true; // Prevents Graph_Paint from being called afterward
        static Real calculateGrid(Real range) => MathR.Pow(GRID, MathR.Floor(MathR.Log(range / 2, GRID)));
        var (xGrid, yGrid) = (calculateGrid(RowScopes()), calculateGrid(ColumnScopes()));
        var (ratioRow, ratioColumn) = (GetRatioRow(borders), GetRatioColumn(borders));
        var (xInit, yInit, xEnd, yEnd) = (AddOne(borders[0]), AddOne(borders[2]), borders[1], borders[3]);

        void drawGrids(Real xGrid, Real yGrid, Real penWidth)
        {
            Pen gridPen = new(GRID_GRAY, (float)penWidth);
            RealComplex.CheckFor((int)MathR.Floor(scopes[2] / yGrid), (int)MathR.Ceiling(scopes[3] / yGrid), i =>
            {
                int pos = LinearTransformY(i * yGrid, borders, ratioColumn);
                if (pos >= yInit && pos < yEnd) graphics.DrawLine(gridPen, xInit, pos, xEnd, pos);
            });
            RealComplex.CheckFor((int)MathR.Floor(scopes[0] / xGrid), (int)MathR.Ceiling(scopes[1] / xGrid), i =>
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
        if (!delete_coor && !ReturnAxesDrawn(isMain)) { DrawAxesGrids(borders); SetAxesDrawn(isMain, true); }
    } // Sensitive
    private void DrawReferenceRectangles(Color color) => graphics.FillRectangle(new SolidBrush(color), VScrollBarX.Location.X - REF_POS_1,
        Y_UP_MIC + REF_POS_2, 2 * (VScrollBarX.Width + REF_POS_1), VScrollBarX.Height - 2 * REF_POS_2);
    private void DrawScrollBar((Real x, Real y) xyCoor)
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
    private static Real LowRatio(Real a, Real m) => a == -0 ? 1 : LowDist(a, m) / m; // -0 is necessary
    private static Real GetShade(Real alpha) => (alpha - 1) / DEPTH + 1;
    private unsafe static (Real, Real) FiniteExtremities(Matrix<Real> output, int rows, int columns)
    {
        static Real seekM(Func<Real, Real, Real> function, Real* ptr, int length)
        {
            Real _value = Real.NaN;
            for (int i = 0; i < length; i++, ptr++)
            { if (Real.IsNaN(*ptr)) continue; if (Real.IsNaN(_value)) _value = *ptr; else _value = function(*ptr, _value); }
            return _value;
        }
        Matrix<Real> outputAtan = Matrix<Real>.Rent(RealComplex.GetArithProg(rows, columns), columns), minMax = GetMatrix(2, rows);
        try
        {
            Parallel.For(0, rows, p =>
            {
                Real* destPtr = outputAtan.RowPtr(p), _destPtr = destPtr, srcPtr = output.RowPtr(p);
                for (int q = 0; q < columns; q++, destPtr++, srcPtr++) *destPtr = MathR.Atan(*srcPtr);
                minMax[0, p] = seekM(MathR.Min, _destPtr, columns); minMax[1, p] = seekM(MathR.Max, _destPtr, columns);
            });
            return (seekM(MathR.Min, minMax.RowPtr(0), rows), seekM(MathR.Max, minMax.RowPtr(1), rows));
        }
        finally { outputAtan.Return(); }
    } // Finds the minimum and maximum after applying atan, which bounds infinite values
    private unsafe static (int, int, Matrix<Real>, Matrix<Real>) GetRowColumnCoor()
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
    private static (int, BitmapData) GetBppBmpData(Bitmap bmp) // bpp: bytes per pixel
        => (Image.GetPixelFormatSize(bmp.PixelFormat) / 8, bmp.LockBits(rectangle, ImageLockMode.ReadWrite, bmp.PixelFormat));
    private unsafe static void ClearBitmap(Bitmap bmp)
    {
        var (bpp, bmpData) = GetBppBmpData(bmp);
        try
        {
            var bmpInit = (byte*)bmpData.Scan0 + bpp - 1;
            Parallel.For(0, rectangle.Height, y =>
            {
                byte* pixelPtr = bmpInit + y * bmpData.Stride;
                for (int x = 0; x < rectangle.Width; x++, pixelPtr += bpp) *pixelPtr = 0; // It suffices to set color.A to zero
            }); // Deliberate loop order
        }
        finally { bmp.UnlockBits(bmpData); }
    }
    private unsafe void SetPixel(byte* _ptr, Color color, ref int pixNum)
    { pixNum++; *_ptr = color.B; _ptr++; *_ptr = color.G; _ptr++; *_ptr = color.R; _ptr++; *_ptr = color.A; }
    private unsafe void RealSpecial(byte* _ptr, Color _zero, Color _pole, Real _value, (Real min, Real max) mM, ref int pixNum)
    {
        if (_value < Real.Lerp(mM.min, mM.max, size_real)) SetPixel(_ptr, _zero, ref pixNum);
        if (_value > Real.Lerp(mM.max, mM.min, size_real)) SetPixel(_ptr, _pole, ref pixNum);
    }
    private unsafe void ComplexSpecial(byte* _ptr, Color _zero, Color _pole, Real _value, ref int pixNum)
    {
        if (_value < epsilon) SetPixel(_ptr, _zero, ref pixNum);
        if (_value > 1 / epsilon) SetPixel(_ptr, _pole, ref pixNum);
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
    private unsafe void RealLoop(Matrix<Real> output, Color _zero, Color _pole, Func<Real, Color> extractor, (Real, Real) mM)
        => LoopBase((x, y, pixelPtr, ref pixNum) =>
        {
            Real _value = output[x, y]; var _pixelPtr = (byte*)pixelPtr;
            if (Real.IsNaN(_value)) return;
            SetPixel(_pixelPtr, extractor(_value), ref pixNum);
            if (!delete_point) RealSpecial(_pixelPtr, _zero, _pole, MathR.Atan(_value), mM, ref pixNum);
        });
    private unsafe void ComplexLoop(Matrix<Complex> output, Color _zero, Color _pole, Func<Complex, Color> extractor)
        => LoopBase((x, y, pixelPtr, ref pixNum) =>
        {
            Complex _value = output[x, y]; var _pixelPtr = (byte*)pixelPtr;
            if (Real.IsNaN(_value.real) || Real.IsNaN(_value.imaginary)) return;
            SetPixel(_pixelPtr, extractor(_value), ref pixNum);
            if (!delete_point) ComplexSpecial(_pixelPtr, _zero, _pole, Complex.Modulus(_value), ref pixNum);
        });
    private static Func<Real, Color> GetColorReal123(int mode) => _value =>
    {
        Color func23(Color c1, Color c2) => _value < 0 ? Swap(c1, c2) : _value > 0 ? Swap(c2, c1) : Color.Empty;
        return mode switch
        {
            1 => MathR.Abs(_value) < epsilon ? Swap(Color.Black, Color.White) : Color.Empty,
            2 => func23(Color.White, Color.Black),
            3 => func23(UPPER_GOLD, LOWER_BLUE)
        };
    };
    private static Func<Real, Color> GetColorReal45(bool mode, (Real min, Real max) mM) => _value => mode ?
        ObtainColorStrip(_value, mM.min, mM.max) : ObtainColorStrip(_value, mM.min, mM.max, GetShade(LowRatio(_value, stride_real)));
    private static Func<Complex, Color> GetColorComplex123(int mode, bool isReIm) => input =>
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
            : MathR.Min(MathR.Min(LowDist(v1, s1), LowDist(v2, s2)), MathR.Min(-LowDist(v1, -s1), -LowDist(v2, -s2))) < epsilon;
        return mode == 1 ? (draw ? Swap(c2, c1) : Color.Empty) : (draw ? Swap(c1, c2) : Swap(c2, c1));
    };
    private static Func<Complex, Color> GetColorComplex45(bool mode) => mode ? (c => ObtainColorWheel(c, alpha: 1)) : (_value =>
    {
        Complex _valueLog = Complex.Log(_value);
        Real alpha = (LowRatio(_valueLog.real, mod_stride) + LowRatio(_valueLog.imaginary, arg_stride)) / 2;
        return ObtainColorWheel(_value, GetShade(alpha));
    });
    private void RealLoop123(Matrix<Real> output, Color _zero, Color _pole, int mode, (Real, Real) mM)
        => RealLoop(output, _zero, _pole, GetColorReal123(mode), mM);
    private void RealLoop45(Matrix<Real> output, bool mode, (Real, Real) mM)
        => RealLoop(output, Color.Black, Color.White, GetColorReal45(mode, mM), mM);
    private void ComplexLoop123(Matrix<Complex> output, Color _zero, Color _pole, int mode, bool isReIm)
        => ComplexLoop(output, _zero, _pole, GetColorComplex123(mode, isReIm));
    private void ComplexLoop45(Matrix<Complex> output, bool mode)
        => ComplexLoop(output, Color.Black, Color.White, GetColorComplex45(mode));
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
        realOperation(output_real, FiniteExtremities(output_real, RowBorders(borders), ColumnBorders(borders)));
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
    private (Real, Real, Real) SetStartEndIncrement(string[] split, bool isPolar, bool isParam)
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
            MyString.ThrowInvalidLengths(split, [0, 1, 2, 3, 4]); Real range = Obtain(GeneralInput);
            Real getRange(TextBox tbx, bool minus) => GeneralInput_Undo() ? Obtain(tbx) : (minus ? -range : range);
            return (split.Length < 3 ? getRange(X_Left, true) : obtain(1),
                split.Length < 3 ? getRange(X_Right, false) : obtain(2),
                split.Length == 2 ? obtain(1) : split.Length == 4 ? obtain(3) : INCREMENT);
        }
    }
    private unsafe static (Matrix<Real>, Matrix<Real>, int, bool) SetCurveValues(string[] split, bool isPolar, bool isParam,
        Real start, Real end, Real increment)
    {
        string replace(string s, int index) => s.Replace(split[index], "x");
        string tag1 = ReplaceTags.FUNC_HEAD + ReplaceTags.COS, tag2 = ReplaceTags.FUNC_HEAD + ReplaceTags.SIN,
            input1 = isParam ? replace(split[0], 2) : isPolar ? replace($"({split[0]})*{tag1}({split[1]})", 1) : "x",
            input2 = isParam ? replace(split[1], 2) : isPolar ? replace($"({split[0]})*{tag2}({split[1]})", 1) : split[0];

        int length = (int)((end - start) / increment), _length = length + 1; // For safety
        Matrix<Real> partition = GetMatrix(1, _length); Real steps = start;
        Real obtainCheck(string input) => RealSub.Obtain(input, steps); // Already simplified
        if (is_checking) { obtainCheck(input1); obtainCheck(input2); return (partition, partition, length, true); }

        Real* partPtr = partition.RowPtr(); for (int i = 0; i < _length; i++, partPtr++, steps += increment) *partPtr = steps;
        Matrix<Real> obtain(string input) => new RealSub(input, partition, null, null, null, null, 1, _length).Obtain();
        return (obtain(input1), obtain(input2), length, false);
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
                DrawScrollBar(LinearTransform(pos.X, pos.Y, ratioRow, ratioColumn, borders));
                _ratio = Frac(REFRESH, ratio);
                if (_ratioBuffer != _ratio) DrawReferenceRectangles(selectedPen.Color);
                _ratioBuffer = _ratio;
            }
            inRangeBuffer = inRange; posBuffer = pos;
        }
    }
    private void DisplayFPPBase(string[] split, bool isPolar = false, bool isParam = false)
    {
        var (start, end, increment) = SetStartEndIncrement(split, isPolar, isParam);
        MyString.ThrowException(start >= end);
        var (value1, value2, length, isChecking) = SetCurveValues(split, isPolar, isParam, start, end, increment);
        if (isChecking) return;
        DisplayBase(() => { DrawCurve(value1, value2, length); pixel_number += segment_number; segment_number = 0; });
    }
    private void DisplayFunction(string[] split) => DisplayFPPBase(split); // Necessary
    private void DisplayPolar(string[] split) => DisplayFPPBase(split, isPolar: true);
    private void DisplayParametric(string[] split) => DisplayFPPBase(split, isParam: true);
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
        ClearBitmap(bmp_mac); ClearBitmap(bmp_mic); // Required thanks to ZAL
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
        string replaceLoop(int loops, int origIdx, int subIdx) => MyString.ReplaceLoop(split, origIdx, subIdx, loops.ToString(), true);
        string obtainDisplay(int loops, string defaultInput) => split.Length == 6 ? replaceLoop(loops, 5, 2) : defaultInput;
        if (is_complex)
        {
            MyString.ThrowInvalidLengths(split, [5, 6, 8]);
            if (split.Length != 8)
            {
                Matrix<Complex> z = ComplexSub.InitilizeZ(xCoor, yCoor, rows, columns); // Complex-specific
                Matrix<Complex> Z = new ComplexSub(split[1], z, null, null, rows, columns).Obtain();
                RealComplex.CheckFor(RealSub.ToInt(split[3]), RealSub.ToInt(split[4]), loops =>
                {
                    Z = new ComplexSub(replaceLoop(loops, 0, 2), z, Z, null, rows, columns).Obtain();
                    output_complex = new ComplexSub(obtainDisplay(loops, "Z"), z, Z, null, rows, columns).Obtain();
                    RunDisplayBase(ComplexComputation);
                });
            }
            else
            {
                Matrix<Real> X = new RealSub(split[2], xCoor, yCoor, null, null, null, rows, columns).Obtain(), temp1;
                Matrix<Real> Y = new RealSub(split[3], xCoor, yCoor, null, null, null, rows, columns).Obtain(), temp2;
                RealComplex.CheckFor(RealSub.ToInt(split[5]), RealSub.ToInt(split[6]), loops =>
                {
                    temp1 = new RealSub(replaceLoop(loops, 0, 4), xCoor, yCoor, X, Y, null, rows, columns).Obtain();
                    temp2 = new RealSub(replaceLoop(loops, 1, 4), xCoor, yCoor, X, Y, null, rows, columns).Obtain();
                    X = temp1; Y = temp2;
                    output_complex = new ComplexSub(replaceLoop(loops, 7, 4), X, Y, rows, columns).Obtain();
                    RunDisplayBase(ComplexComputation);
                });
            }
        }
        else
        {
            MyString.ThrowInvalidLengths(split, [5, 6]);
            Matrix<Real> X = new RealSub(split[1], xCoor, yCoor, null, null, null, rows, columns).Obtain();
            RealComplex.CheckFor(RealSub.ToInt(split[3]), RealSub.ToInt(split[4]), loops =>
            {
                X = new RealSub(replaceLoop(loops, 0, 2), xCoor, yCoor, X, null, null, rows, columns).Obtain();
                output_real = new RealSub(obtainDisplay(loops, "y-X"), xCoor, yCoor, X, null, null, rows, columns).Obtain();
                RunDisplayBase(RealComputation);
            });
        }
    } // Intentionally buffer-free; rendering provides its own delay [see ComplexSub.ProcessSPI and RealSub.ProcessSPI]
    private void DisplayLoop(string[] split)
    {
        MyString.ThrowInvalidLengths(split, [4]);
        RealComplex.CheckFor(RealSub.ToInt(split[2]), RealSub.ToInt(split[3]), loops =>
        { DisplayLevel3(MyString.ReplaceLoop(split, 0, 1, loops.ToString(), true)); });
    }
    private void DisplaySubs(string[] split)
    {
        MyString.ThrowException(Int32.IsEvenInteger(split.Length));
        for (int i = 0, j = 1; i < split.Length / 2; i++) split[0] = MyString.ReplaceLoop(split, 0, j++, split[j++]);
        DisplayLevel2(split[0]);
    }
    private void DisplayLevel3(string input)
    {
        Action<string[]>? displayMethod = ContainsTag(input, ReplaceTags.ITLOOP) ? DisplayIterateLoop :
            ContainsTag(input, ReplaceTags._FUNC) ? DisplayFunction :
            ContainsTag(input, ReplaceTags._POLAR) ? DisplayPolar :
            ContainsTag(input, ReplaceTags._PARAM) ? DisplayParametric : null;
        if (displayMethod != null) displayMethod(MyString.SplitString(input));
        else DisplayRendering(input);
    }
    private void DisplayLevel2(string input)
    {
        if (ContainsTag(input, ReplaceTags.LOOP)) DisplayLoop(MyString.SplitString(input));
        else DisplayLevel3(input);
    }
    private void DisplayLevel1(string input)
    {
        if (ContainsTag(input, ReplaceTags.SUBS)) DisplaySubs(MyString.SplitString(input));
        else DisplayLevel2(input);
    }
    private void DisplayOnScreen()
    {
        if (NoInput()) return; // Necessary
        string[] split = MyString.SplitByChars(InputString.Text, "|");
        for (int loops = 0; loops < split.Length; loops++) DisplayLevel1(RecoverMultiply.Simplify(split[loops]));
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
    private static Color ObtainColorWheel(Complex c, Real alpha = 1)
        => ObtainColorBase(ArgRGB(c.real, c.imaginary), alpha, (int)(255 / (1 + decay * Complex.Modulus(shade ? c : Complex.ZERO))));
    private static Color ObtainColorWheelCurve(Real alpha) => ObtainColorBase(alpha * MathR.Tau, 1, 255);
    private static Color ObtainColorStrip(Real _value, Real min, Real max, Real alpha = 1) // alpha: brightness
    {
        if (min == max) return Color.Empty; // Necessary
        Real beta = (MathR.Atan(_value) - min) / (max - min);
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
        HandleMouseAction(e, borders, v => { DrawScrollBar(v); DisplayMouseMove(e, v.Item1, v.Item2); });
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
        static string trimMove(Real input) => MyString.TrimExtremeNum(input, THRESHOLD);
        SetText(X_CoorDisplay, trimMove(xCoor)); SetText(Y_CoorDisplay, trimMove(yCoor));
        SetText(ModulusDisplay, trimMove(Real.Hypot(xCoor, yCoor))); SetText(AngleDisplay, MyString.GetAngle(xCoor, yCoor));
        DisplayMouseMoveCore(e.X - AddOne(borders[0]), e.Y - AddOne(borders[2]));
    }
    private void DisplayMouseDown(MouseEventArgs e, Real xCoor, Real yCoor)
    {
        static string trimDown(Real input) => MyString.TrimExtremeNum(input, THRESHOLD);
        string _xCoor = trimDown(xCoor), _yCoor = trimDown(yCoor), modulus = trimDown(Real.Hypot(xCoor, yCoor)),
            angle = MyString.GetAngle(xCoor, yCoor), message = String.Empty;
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
        Invoke(() => { StopTimers(); Thread.Sleep(SLEEP); StartTimers(); }); // Executed on the UI thread
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
        if (NoInput()) return;
        Clipboard.SetText(MyString.BeautifyInput(InputString.Text)); // Problematic on JSX's PC
        BlockInput(true);
        StartTimers();
        await Task.Run(() => { Thread.CurrentThread.Priority = ThreadPriority.Highest; runClick(); });
        BlockInput(false);
    }
    private void StartTimers()
    {
        display_elapsed = 0;
        SetText(TimeDisplay, "0s");
        is_flashing = false; // Delays the hourglass
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
        TextBox[] textBoxes = [InputString, GeneralInput, X_Left, X_Right, Y_Left, Y_Right, ThickInput, DenseInput, AddressInput];
        foreach (var tbx in textBoxes) tbx.ReadOnly = readOnly;
        Button[] buttons = [ConfirmButton, PreviewButton, AllButton];
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
        if (is_main) SetText(CaptionBox, $"{MyString.BeautifyInput(InputString.Text)}\r\n" + CaptionBox.Text);
        SetText(TimeDisplay, $"{TimeCount:hh\\:mm\\:ss\\.fff}");
        AddDraft($"\r\n{SEP} No.{loop_number} [{mode}] {SEP}\r\n" + $"\r\n{MyString.BeautifyInput(InputString.Text)}\r\n" +
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
        bool temp = ProcessingGraphics();
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
    private void CheckAll(object sender, EventArgs e)
    {
        Action<object, EventArgs>[] checkActions =
        [
            GeneralInput_DoubleClick,
            Details_TextChanged, // Order-sensitive position
            InputString_DoubleClick,
            ThickInput_DoubleClick,
            DenseInput_DoubleClick,
            AddressInput_DoubleClick
        ];
        foreach (var action in checkActions) action(sender, e);
    }
    private void Graph_KeyUp(object sender, KeyEventArgs e)
    {
        HandleModifierKeys(e, false);
        if (suppress_key_up) return; // Do not merge with the next line
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
        if (!ctrl_pressed) return;
        void restoreDefault(object sender, KeyEventArgs e)
        {
            RecoverInput(); ComboColoring.SelectedIndex = 4; ComboContour.SelectedIndex = 1;
            CheckBox[] checkFalse = [CheckAuto, CheckSwap, CheckPoints, CheckShade, CheckRetain];
            foreach (var cbx in checkFalse) cbx.Checked = false;
            CheckBox[] checkTrue = [CheckEdit, CheckComplex, CheckCoor];
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
            $"\r\n\r\n{TAB}Real(...)" +
            $"{TAB}{GetComment("Variable-free real blocks in complex expressions.")}";
        content += subtitleContent("COMBINATORICS",
            $"\r\n\r\n{TAB}Floor, Ceiling & Ceil, Round, Sign & Sgn, Factorial & Fact(Real a)" +
            $"\r\n\r\n{TAB}Mod(Real a, Real n), nCr, nPr(int n, int r)" +
            $"\r\n\r\n{TAB}Max, Min, Dist(Real a, Real b, ...)");
        content += subtitleContent("SPECIALTIES",
            $"\r\n\r\n{GetComment("R&C := Real & Complex.")}" +
            $"\r\n\r\n{TAB}Hypergeometric & Hypgeo(R&C a, R&C b, R&C c, f(x,y) & f(z)) & " +
            $"\r\n{TAB}Hypergeometric & Hypgeo(R&C a, R&C b, R&C c, f(x,y) & f(z), int n)" +
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
        content += getShortcuts("Control + D2", 3, "View Fraljimetry's profile");
        content += getShortcuts("Control + D3", 3, "Clear all read-only displays");
        content += getShortcuts("Control + OemQuestion", 2, "View the user manual");
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
        else { ComboExamples_Undo(); InputString.ReadOnly = false; return; }
        InputString.ReadOnly = false;

        ComboColoring.SelectedIndex = set.iC;
        SetText(GeneralInput, set.sG); SetText(ThickInput, set.sO.T); SetText(DenseInput, set.sO.D);
        SetText(X_Left, set.sD.xL); SetText(X_Right, set.sD.xR); SetText(Y_Left, set.sD.yL); SetText(Y_Right, set.sD.yR);
        (CheckCoor.Checked, CheckPoints.Checked, CheckShade.Checked, CheckRetain.Checked) = set.bC;
    }
    private void ComboFS_SelectionChanged(ComboBox cbx)
    {
        string selectedItem = cbx.SelectedItem.ToString();
        if (ProcessingGraphics()) return; int pos = InputString.SelectionStart;
        SetText(InputString, MyString.Replace(InputString.Text, String.Concat(selectedItem, RecoverMultiply.LR_BRA),
            pos, pos + InputString.SelectionLength - 1));
        InputString.Focus(); InputString.SelectionStart = pos + selectedItem.Length + 1; // Must remain after .Focus()
    }
    private void ComboExamples_SelectedIndexChanged(object sender, EventArgs e)
    {
        string? selection = ComboExamples.SelectedItem?.ToString();
        if (ProcessingGraphics() || String.IsNullOrEmpty(selection) || ComboExamples.SelectedIndex == -1) return;
        SetText(InputString, selection);
        SetValuesForSelectedIndex(ComboExamples.SelectedIndex);
        ComboExamples_Undo(); // Prevents repeated calls
        Delete_Click(e);
        InputString_Focus();
    }
    private void ComboFunctions_SelectedIndexChanged(object sender, EventArgs e) => ComboFS_SelectionChanged(ComboFunctions);
    private void ComboSpecial_SelectedIndexChanged(object sender, EventArgs e) => ComboFS_SelectionChanged(ComboSpecial);
    private static int ComboCC_SelectionChanged(ComboBox cbx) => AddOne(cbx.SelectedIndex);
    private void ComboColoring_SelectedIndexChanged(object sender, EventArgs e) => color_mode = ComboCC_SelectionChanged(ComboColoring);
    private void ComboContour_SelectedIndexChanged(object sender, EventArgs e) => contour_mode = ComboCC_SelectionChanged(ComboContour);
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
        TextBox[] textBoxes = [DraftBox, PointNumDisplay, TimeDisplay, X_CoorDisplay, Y_CoorDisplay,
                ModulusDisplay, AngleDisplay, FunctionDisplay, CaptionBox];
        foreach (var tbx in textBoxes) SetText(tbx, String.Empty);
        InputString_Focus();
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
        Label[] labels = [InputLabel, AtLabel, GeneralLabel, DetailLabel, X_Scope, Y_Scope, ThickLabel, DenseLabel,
                ExampleLabel, FunctionLabel, ModeLabel, ContourLabel];
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
                if (!noInput) Obtain(tbx); // For checking
            }
            lbl.ForeColor = noSomeInput ? Color.White : CORRECT_GREEN; // White if any input is null or empty
        }
        catch (Exception) { lbl.ForeColor = ERROR_RED; }
    }
    private void MiniChecks(TextBox tbx, Label lbl) => MiniChecks([tbx], lbl);
    private void Details_TextChanged(object sender, EventArgs e)
    {
        if (ProcessingGraphics()) return;
        MiniChecks([X_Left, X_Right, Y_Left, Y_Right], DetailLabel);
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
        if (ProcessingGraphics()) return;
        static int removeSomeKeys(TextBox tbx)
        {
            int caretPosition = tbx.Text.Length - tbx.SelectionStart - tbx.SelectionLength; // Necessary
            foreach (char c in RecoverMultiply.BARRED_CHARS) SetText(tbx, tbx.Text.Replace(c, ' '));
            return tbx.Text.Length - caretPosition;
        }
        int pos = removeSomeKeys(InputString); // Necessary
        CheckValidityCore(() =>
        {
            InputString.BackColor = InputLabel.ForeColor = ERROR_RED;
            PictureIncorrect.Visible = true; PictureCorrect.Visible = false;
        });
        InputString.SelectionStart = pos;
        if (PictureCorrect.Visible) DisplayMouseMoveCore(); // Displays the value in the lower-right corner
    }
    private void AddressInput_TextChanged(object sender, EventArgs e)
    {
        if (ProcessingGraphics()) return;
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
        if (tbx.ReadOnly) return;
        int caretPosition = tbx.SelectionStart; // Necessary
        void selectSuppress(int pos) { tbx.SelectionStart = caretPosition + pos; e.SuppressKeyPress = true; }
        void insertSelectSuppress(string insertion, int pos)
        { SetText(tbx, tbx.Text.Insert(caretPosition, insertion)); selectSuppress(pos); }
        char obtainLeft() => e.KeyCode switch { Keys.D9 => '(', Keys.OemOpenBrackets => '{' };
        char obtainRight(char left) => left switch { '(' => ')', '{' => '}' };

        if (!MyString.CheckParenthesis(tbx.Text.AsSpan(caretPosition, tbx.SelectionLength))) selectSuppress(0);
        else if ((e.KeyCode == Keys.D9 || e.KeyCode == Keys.OemOpenBrackets) && (ModifierKeys & Keys.Shift) != 0)
        {
            char left = obtainLeft(), right = obtainRight(left);
            if (tbx.SelectionLength == 0) insertSelectSuppress($"{left}{right}", 1);
            else
            {
                string selectedText = tbx.Text.Substring(caretPosition, tbx.SelectionLength);
                SetText(tbx, tbx.Text.Remove(caretPosition, tbx.SelectionLength));
                insertSelectSuppress($"{left}{selectedText}{right}", selectedText.Length + 2);
            }
        }
        else if ((e.KeyCode == Keys.D0 || e.KeyCode == Keys.OemCloseBrackets) && (ModifierKeys & Keys.Shift) != 0)
        {
            if (tbx.SelectionLength > 0) selectSuppress(0);
            else if (caretPosition == 0) selectSuppress(0);
            else if (RecoverMultiply.IsBraL(tbx.Text[caretPosition - 1])) selectSuppress(1);
        }
        else if (e.KeyCode == Keys.Oemcomma) insertSelectSuppress(", ", 2);
        else if (e.KeyCode == Keys.OemPipe) insertSelectSuppress(" | ", 3);
        else if (e.KeyCode == Keys.Back)
        {
            if (caretPosition == 0 || !MyString.CheckParenthesis(tbx.Text) || tbx.SelectionLength > 0) return;
            char c = tbx.Text[caretPosition - 1];
            if (RecoverMultiply.IsBraL(c))
            {
                if (tbx.Text[caretPosition] == obtainRight(c)) SetText(tbx, tbx.Text.Remove(caretPosition - 1, 2));
                selectSuppress(-1);
            }
            else if (RecoverMultiply.IsBraR(c)) selectSuppress(-1);
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

/// <summary>
/// TOOLKIT SECTION
/// </summary>
public class MyString
{
    private static string[] AddSuffix(string[] str) { for (int i = 0; i < str.Length; i++) str[i] += "("; return str; }
    public static readonly string[] FUNC = AddSuffix(["function", "Function", "func", "Func"]),
        POLAR = AddSuffix(["polar", "Polar"]), PARAM = AddSuffix(["parametric", "Parametric", "param", "Param"]);
    public static readonly string[] FPP_NAMES = [.. FUNC, .. POLAR, .. PARAM];
    protected static readonly char SUB_CHAR = ';'; // Replaces ",^"

    #region Parentheses
    private static int PairedParenthesis(ReadOnlySpan<char> input, int start)
    {
        for (int i = start + 1, count = 1; ; i++)
        { if (input[i] == '(') count++; else if (input[i] == ')') count--; if (count == 0) return i; }
    }
    protected static (int, int, string[]) PrepareSeriesSub(ReadOnlySpan<char> input)
    {
        int i = input.IndexOf(ReplaceTags.SERIES_TAIL), end = PairedParenthesis(input, i + 1);
        return (i, end, ReplaceRecover(BraFreePart(input, i + 1, end)));
    }
    protected static void ResetStartEnd(ReadOnlySpan<char> input, ref int start, ref int end)
    {
        static (int, int) innerBra(ReadOnlySpan<char> input, int start)
        { for (int i = start, j = -1; ; i--) { if (input[i] == ')') j = i; else if (input[i] == '(') return (i, j); } }
        static int pairedInnerBra(ReadOnlySpan<char> input, int start)
        { for (int i = start + 1; ; i++) if (input[i] == ')') return i; }
        int _start = start; (start, end) = innerBra(input, start); if (end == -1) end = pairedInnerBra(input, _start);
    } // Performs a backward lookup for matching parentheses; highly sensitive
    public static bool CheckParenthesis(ReadOnlySpan<char> input)
    {
        int sum = 0;
        foreach (char c in input) { if (c == '(') sum++; else if (c == ')') sum--; if (sum < 0) return false; }
        return sum == 0;
    }
    #endregion

    #region Replacement
    protected static ReadOnlySpan<char> BraFreePart(ReadOnlySpan<char> input, int start, int end) => input.Slice(start + 1, end - start - 1);
    protected static ReadOnlySpan<char> TryBraNum(ReadOnlySpan<char> input, char c1, char c2)
    { ThrowException(input[0] != c1 || input[^1] != c2); return BraFreePart(input, 0, input.Length - 1); }
    private static string ReplaceCore(string orig, string sub, int start, int end)
        => String.Create(start + sub.Length + orig.Length - end - 1, (start, end, sub.Length), (span, state) =>
        {
            var (_start, _end, _subLen) = state;
            orig[.._start].CopyTo(span); sub.CopyTo(span[_start..]); orig[(_end + 1)..].CopyTo(span[(_start + _subLen)..]);
        });
    public static string Replace(ReadOnlySpan<char> orig, ReadOnlySpan<char> sub, int start, int end)
        => ReplaceCore(orig.ToString(), sub.ToString(), start, end);
    public static string ReplaceLoop(ReadOnlySpan<string> split, int origIdx, int subIdx, string idxStr, bool wrapBra = false)
        => split[origIdx].Replace(split[subIdx], wrapBra ? String.Concat('(', idxStr, ')') : idxStr);
    protected static string ReplaceInput(ReadOnlySpan<char> input, int countBra, int start, int end)
        => Replace(input, String.Concat('[', countBra.ToString(), ']'), start, end);
    protected static string ReplaceInput(ReadOnlySpan<char> input, int countBra, ref int start, int end, ref int tagL)
    { start -= tagL; tagL = 0; return ReplaceInput(input, countBra, start--, end); }
    private static string ReplaceInterior(ReadOnlySpan<char> input, char origChar, char subChar)
    {
        if (!input.Contains(ReplaceTags.SERIES_TAIL)) return input.ToString();
        StringBuilder buffer = new(input.ToString());
        for (int i = 0; i < buffer.Length; i++)
        {
            if (buffer[i] != ReplaceTags.SERIES_TAIL) continue;
            int endIndex = PairedParenthesis(input, i + 1);
            for (int j = i + 1; j < endIndex; j++) if (buffer[j] == origChar) buffer[j] = subChar;
            i = endIndex;
        } // Sensitive
        return buffer.ToString();
    } // Prevents commas inside parentheses from interfering with outer splitting
    private static string[] ReplaceRecover(ReadOnlySpan<char> input)
        => [.. SplitByChars(ReplaceInterior(input, ',', SUB_CHAR), ",").Select(part => part.Replace(SUB_CHAR, ','))];
    public static string ReplaceSubstrings(string input, ReadOnlySpan<string> substrings, string substitution)
    { foreach (string s in substrings) input = input.Replace(s, substitution); return input; }
    protected static string RemoveEnterBlank(string input) => ReplaceSubstrings(input, RecoverMultiply.ENTER_BLANK, String.Empty);
    public static string BeautifyInput(string input) => RemoveEnterBlank(input).Replace(",", ", ").Replace("|", " | ");
    #endregion

    #region Miscellaneous
    public static string[] SplitString(ReadOnlySpan<char> input)
        => ReplaceRecover(BraFreePart(input, input.IndexOf('('), PairedParenthesis(input, input.IndexOf('('))));
    public static string[] SplitByChars(ReadOnlySpan<char> input, ReadOnlySpan<char> delimiters)
    {
        Span<bool> lookup = stackalloc bool[1024]; foreach (char d in delimiters) lookup[d] = true; // ASCII + Greek
        List<string> segments = []; StringBuilder segmentBuilder = new(input.Length);
        foreach (char c in input)
        {
            if (lookup[c]) { segments.Add(segmentBuilder.ToString()); segmentBuilder.Clear(); }
            else segmentBuilder.Append(c);
        }
        segments.Add(segmentBuilder.ToString());
        return [.. segments]; // Collection expression (.NET 8.0)
    }
    protected static string TrimStartChar(ReadOnlySpan<char> input, char startChar)
    {
        int startIndex = 0;
        while (startIndex < input.Length && input[startIndex] == startChar) startIndex++;
        if (startIndex == input.Length) return String.Empty;
        return input[startIndex..].ToString();
    }
    public static string TrimExtremeNum(Real input, Real threshold)
        => (MathR.Abs(input) < threshold && MathR.Abs(input) > 1 / threshold) ? input.ToString("#0.0000000") : input.ToString("E3");
    public static string GetAngle(Real x, Real y) => (Graph.ArgRGB(x, y) / MathR.PI).ToString("#0.000000") + " π";
    public static void ThrowException(bool error = true) { if (error) throw new Exception(); }
    public static void ThrowInvalidLengths(ReadOnlySpan<string> split, int[] lengths) => ThrowException(!lengths.Contains(split.Length));
    protected static (int, int) ObtainStartEnd(ReadOnlySpan<string> split, int length, int start, int iteration)
    {
        ThrowInvalidLengths(split, [length, length + 1]);
        int end = split.Length == length ? iteration : RealSub.ToInt(split[^1]);
        ThrowException(start > end); return (start, end);
    }
    public static bool ContainsAny(string input, ReadOnlySpan<string> stringsToCheck)
    {
        foreach (string s in stringsToCheck) if (input.Contains(s)) return true;
        return false;
    }
    #endregion
} /// Provides string-manipulation utilities
public class RealComplex : MyString
{
    protected static readonly Real GAMMA = (Real)0.5772156649015329, LOG2 = MathR.Log(2);
    protected static readonly int THRESHOLD = 10, BRKCHK = 5 * THRESHOLD, STEP = 1; // STEP: a tunable chunk size
    protected static readonly string SUB_CHAR_STR = SUB_CHAR.ToString(), SUB_CHARS = ":;"; // Replaces "+-*/"
    protected const char _A = 'a', A_ = 'A', B_ = 'B', _C = 'c', C_ = 'C', D_ = 'D', _D_ = '$', E = 'e', E_ = 'E',
        _F = 'f', F_ = 'F', _F_ = '!', G = 'γ', G_ = 'G', _H = 'h', H_ = 'H', I = 'i', I_ = 'I', J_ = 'J', K_ = 'K', _L = 'l',
        M_ = 'M', MAX = '>', MIN = '<', MODE_1 = '1', MODE_2 = '2', P = 'π', P_ = 'P', _Q = 'q', _R = 'r', R_ = 'R',
        _S = 's', S_ = 'S', SP = '#', _T = 't', TILDE = '~', _X = 'x', X_ = 'X', _Y = 'y', Y_ = 'Y', _Z = 'z', Z_ = 'Z', _Z_ = 'ζ';
    public static readonly string SUBS = "σ", ITLOOP = "ι", LOOP = "λ", _FUNC = "φ", _POLAR = "ψ", _PARAM = "ρ";

    protected static int CountChars(ReadOnlySpan<char> input, ReadOnlySpan<char> charsToCheck)
    {
        int count = 0, offset = 0;
        do
        {
            int idx = input[offset..].IndexOfAny(charsToCheck); if (idx < 0) break;
            count++; offset += idx + 1;
        } while (offset < input.Length); // To avoid slicing past the end
        return count;
    }
    public unsafe static int[] GetArithProg(int length, int diff)
    {
        if (length == 0) return []; int[] arithProg = new int[length];
        fixed (int* ptr = arithProg) { int* _ptr = ptr; for (int i = 0, j = 0; i < length; i++, _ptr++, j += diff) *_ptr = j; }
        return arithProg;
    }
    protected static void Initialize<TEntry>(int rows, int columns, ref int rowChk, ref int[]? rowOffs, ref uint colBytes,
        ref int strd, ref int[]? strdInit, ref uint strdBytes, ref int res, ref int resInit, ref uint resBytes)
    {
        int step = Int32.Min(rows, STEP); // Necessary to ensure rowChk > 0
        rowChk = rows / step; rowOffs = GetArithProg(rows, columns);
        strd = columns * step; strdInit = GetArithProg(rowChk, step);
        resInit = rowChk * step; res = rows - resInit;
        int _colBytes = columns * Unsafe.SizeOf<TEntry>(); uint getBytes(int times) => (uint)(_colBytes * times);
        colBytes = getBytes(1); strdBytes = getBytes(step); resBytes = getBytes(res);
    } // Fields for optimization
    protected unsafe static (Real[], Real[], Real[]) GetSeqsForZeta(int start, int end)
    {
        int length = end - start + 1;
        Real[] coeffSeq = new Real[length], _coeffSeq = new Real[length * (length + 1) / 2], logSeq = new Real[length];
        fixed (Real* coeffSeqPtr = coeffSeq, _coeffSeqPtr = _coeffSeq, logSeqPtr = logSeq)
        {
            Real* ptr = coeffSeqPtr, _ptr = _coeffSeqPtr, ptrL = logSeqPtr; Real coeff = 1, _coeff;
            for (int i = start; i <= end; i++, ptr++, ptrL++)
            {
                coeff /= 2; *ptr = coeff; *ptrL = MathR.Log(i + 1); _coeff = 1;
                for (int j = start; j <= i; j++, _ptr++) { *_ptr = _coeff; _coeff *= (Real)(j - i) / (Real)(j + 1); }
            }
        }
        return (coeffSeq, _coeffSeq, logSeq);
    }
    public static void CheckFor(int start, int end, Action<int> action)
    { ThrowException(start > end); for (int i = start; i <= end; i++) action(i); }
    protected static Matrix<Real> ChooseMode((string mode, Matrix<Real> m1, Matrix<Real> m2) mode12)
        => Char.Parse(mode12.mode) switch { MODE_1 => mode12.m1, MODE_2 => mode12.m2 };
    protected static Matrix<TEntry> HandleMtx<TEntry>(Matrix<TEntry> mtx, Action<Matrix<TEntry>> action) { action(mtx); return mtx; }
    protected static MatrixCopy<TEntry> HandleSolo<TEntry>(ReadOnlySpan<char> input, MatrixCopy<TEntry> mc)
    { ThrowException(input.Length != 1); return mc; }
    protected static string[] PrepareBreakPower(string input, int THRESHOLD)
    {
        StringBuilder result = new(input);
        for (int i = 0, flag = 0; i < result.Length; i++)
        {
            if (result[i] != '^') continue;
            if (++flag == THRESHOLD) { result.Remove(i, 1).Insert(i, SUB_CHAR); flag = 0; }
        }
        return SplitByChars(result.ToString(), SUB_CHAR_STR);
    }
    protected static (string[], StringBuilder) PrepareBreakPSMD(string input, ReadOnlySpan<char> signs, int THRESHOLD)
    {
        Span<bool> lookup = stackalloc bool[1024]; foreach (char c in signs) lookup[c] = true; // ASCII + Greek
        StringBuilder signsBuilder = new(input), result = new(input);
        for (int i = 0, flag = 0; i < result.Length; i++)
        {
            if (!lookup[result[i]]) continue;
            if (++flag == THRESHOLD)
            {
                char subChar = result[i] == signs[0] ? SUB_CHARS[0] : result[i] == signs[1] ? SUB_CHARS[1] : '\0'; // Necessary
                result.Remove(i, 1).Insert(i, subChar); signsBuilder.Append(subChar);
                flag = 0;
            }
        }
        return (SplitByChars(result.ToString(), SUB_CHARS), signsBuilder);
    }
    private static StringBuilder GetSignsBuilder(ReadOnlySpan<char> input, ReadOnlySpan<char> signs)
    {
        Span<bool> lookup = stackalloc bool[1024]; foreach (char c in signs) lookup[c] = true; // ASCII + Greek
        StringBuilder signsBuilder = new(input.Length);
        foreach (char c in input) if (lookup[c]) signsBuilder.Append(c);
        return signsBuilder;
    }
    protected static (string[], StringBuilder) GetPSMDComponents(ReadOnlySpan<char> input, ReadOnlySpan<char> signs)
    {
        bool signHead = input[0] == signs[1]; string _input = TrimStartChar(input, signs[1]);
        return (SplitByChars(_input, signs), GetSignsBuilder(String.Concat(signHead ? signs[1] : signs[0], _input), signs));
    } // Sensitive
    protected static (bool trig, bool hyper) IsInverseFunc(ReadOnlySpan<char> input, int start)
        => (start > 1 ? input[start - 2] == _A : true, start > 2 ? input[start - 3] == _A : true); // Do not simplify
    protected static (int, int, int, int) PrepareLoop(ReadOnlySpan<char> input) => (CountChars(input, "("), input.Length - 1, -1, 0);
} /// Provides shared functionality for RealSub and ComplexSub
public class ReplaceTags : RealComplex
{
    public static readonly string[] FUNCTIONS =
        [ "floor", "ceiling", "round", "sign", "factorial", "mod", "nCr", "nPr", "max", "min", "distance", "conjugate", "ei", "real",
            "abs", "log", "exp", "sqrt", "arsinh", "arcosh", "artanh", "arcsin", "arccos", "arctan",
            "sinh", "cosh", "tanh", "sin", "cos", "tan" ];
    public static readonly string[] SPECIALS =
        [ "hypergeometric", "gamma", "beta", "zeta", "stereographic", "homothety", "sum", "product", "iterate", "iterate1", "iterate2",
            "compose", "compose1", "compose2", "cocoon", "substitute", "iterateLoop", "loop", "function", "polar", "parametric" ];
    public static readonly string[] EX_COMPLEX =
        [
            "stereo(3, 1, 1, z)",
            "z^coc(1+10i)cos((z-1)/(z^13+z+1))",
            "subs(coc(sum(/(1-exp(k{0})), k, 1, j), log(z))-j, j, 100)",
            "prod(exp(2/(coc(ei(-k/5))z-1)+1), k, 1, 5)",
            "iterate(/sin(Z), z, 100)",
            "conj(coc(iterate((/(ZZZZ)+Z){0}, z, 1000), .9ei(/60)))",
            "subs(itLoop(ZZ+z, 0, k, 1, j, abs(Z)coc(ei(-k/j/3))), j, 100)",
            "comp(sin(zzz), cos(z/Z), log(Z), y-pi sgn(y))"
        ];
    public static readonly string[] EX_REAL =
        [
            "nCr(x, y)",
            "min(sin(xy), tan(x), tan(y))",
            "ceil(x)round(y)-floor(y)round(x)",
            "loop(dist(x, y)-dist(x+1, y-coc(.2k))-1, k, -50, 50)",
            "comp1(iterate1(abs(/X-1), abs(x)+abs(y), 10), X-1)",
            "iterate2(X-tan(Y), Y-/cos(X), x, y, 3, z)",
            "itLoop(x^X, 1, k, 1, 100)",
            "comp2(xx-yy, 2xy, sin(3X)+cos(2Y), cos(3Y)-sin(2X), z)"
        ];
    public static readonly string[] EX_CURVES =
        [
            "func(zeta(x))",
            "func(exp(/(xx-1)), -1, 1)",
            "subs(func(sum(abs(jx-round(jx))/j, k, 0, 10)), j, coc(2^k))",
            "polar(sqrt(cos(2u)), u, 0, 2pi, .0001)",
            "polar(sin(5u)sin(7u), u, 0, 2pi)",
            "loop(polar(coc(.1k)cos(6u+coc(.7kpi)), u, 0, 2pi), k, 1, 10)",
            "param(sin(7u), cos(9u), u, 0, 2pi)",
            "loop(param(cos(u)^k, sin(u)^k, u, 0, pi/2, .01), k, 1, 10)"
        ];
    public static readonly char FUNC_HEAD = TILDE, SERIES_TAIL = '_', REAL_TAIL = _D_, COMPLEX_TAIL = SP;
    private static string ToS(char c) => c.ToString();
    public static readonly string FLOOR = ToS(_F), CEIL = ToS(_C), ROUND = ToS(_R), SGN = ToS(_S), FACT = ToS(_F_),
        MOD = ToS(M_), NCR = ToS(C_), NPR = ToS(A_), _MAX = ToS(MAX), _MIN = ToS(MIN), DIST = ToS(D_);
    public static readonly string CONJ = ToS(J_), EI = EXP = ToS(E_), _REAL = ToS(R_);
    public static readonly string ABS = ToS(_A), LOG = ToS(_L), EXP = ToS(E_), SQRT = ToS(_Q);
    public static readonly string SIN = ToS(_S), COS = ToS(_C), TAN = ToS(_T),
        AS = String.Concat(_A, SIN), AC = String.Concat(_A, COS), AT = String.Concat(_A, TAN),
        SH = String.Concat(SIN, _H), CH = String.Concat(COS, _H), TH = String.Concat(TAN, _H),
        ASH = String.Concat(AS, _H), ACH = String.Concat(AC, _H), ATH = String.Concat(AT, _H);
    public static readonly string HYPGEO = ToS(F_), GA = ToS(G_), BETA = ToS(B_), ZETA = ToS(_Z_),
        STEREO = ToS(R_), HOMOTH = ToS(H_), SUM = ToS(S_), PROD = ToS(P_), COC = ToS(K_), PI = ToS(P), _GA = ToS(G);
    public static readonly string IT = ToS(I_), IT1 = String.Concat(MODE_1, IT), IT2 = String.Concat(MODE_2, IT),
        COMP = ToS(J_), COMP1 = String.Concat(MODE_1, COMP), COMP2 = String.Concat(MODE_2, COMP);

    private static Dictionary<string, string> Concat(Dictionary<string, string> dic1, Dictionary<string, string> dic2)
        => dic1.Concat(dic2).ToDictionary(pair => pair.Key, pair => pair.Value); // Series functions first, then standard functions
    private static readonly Dictionary<string, string> COMMON_STANDARD = new()
        {
            { "abs", ABS }, { "Abs", ABS },
            { "log", LOG }, { "Log", LOG }, { "ln", LOG }, { "Ln", LOG },
            { "exp", EXP }, { "Exp", EXP },
            { "sqrt", SQRT }, { "Sqrt", SQRT },
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
            { "hypergeometric", HYPGEO }, { "Hypergeometric", HYPGEO }, { "hypgeo", HYPGEO }, { "Hypgeo", HYPGEO },
            { "gamma", GA }, { "Gamma", GA }, { "ga", GA }, { "Ga", GA },
            { "beta", BETA }, { "Beta", BETA },
            { "zeta", ZETA }, { "Zeta", ZETA },
            { "stereographic", STEREO }, { "Stereographic", STEREO}, { "stereo", STEREO}, { "Stereo", STEREO},
            { "homothety", HOMOTH }, { "Homothety", HOMOTH }, { "homoth", HOMOTH }, { "Homoth", HOMOTH },
            { "sum", SUM }, { "Sum", SUM },
            { "product", PROD }, { "Product", PROD }, { "prod", PROD }, { "Prod", PROD },
            { "iterate", IT }, { "Iterate", IT },
            { "compose", COMP }, { "Compose", COMP }, { "comp", COMP }, { "Comp", COMP },
            { "iterate2", IT2 }, { "Iterate2", IT2 },
            { "compose2", COMP2 }, { "Compose2", COMP2 }, { "comp2", COMP2 }, { "Comp2", COMP2 },
            { "cocoon", COC}, { "Cocoon", COC}, { "coc", COC}, { "Coc", COC}
        }, SERIES_TAIL);
    private static readonly Dictionary<string, string> COMMON = Concat(COMMON_SERIES, COMMON_STANDARD);
    private static readonly Dictionary<string, string> REAL_STANDARD = AddSuffix(new()
        {
            { "floor", FLOOR }, { "Floor", FLOOR },
            { "ceiling", CEIL }, { "Ceiling", CEIL }, { "ceil", CEIL }, { "Ceil", CEIL },
            { "round", ROUND }, { "Round", ROUND },
            { "sign", SGN }, { "Sign", SGN }, { "sgn", SGN }, { "Sgn", SGN },
            { "factorial", FACT }, { "Factorial", FACT }, { "fact", FACT }, { "Fact", FACT }
        }, REAL_TAIL);
    private static readonly Dictionary<string, string> REAL_SERIES = AddSuffix(AddSuffix(new()
        {
            { "mod", MOD }, { "Mod", MOD }, { "nCr", NCR }, { "nPr", NPR },
            { "max", _MAX }, { "Max", _MAX }, { "min", _MIN }, { "Min", _MIN },
            { "distance", DIST}, { "Distance", DIST}, { "dist", DIST}, { "Dist", DIST},
            { "iterate1", IT1 }, { "Iterate1", IT1 },
            { "compose1", COMP1 }, { "Compose1", COMP1 }, { "comp1", COMP1 }, { "Comp1", COMP1 }
        }, REAL_TAIL), SERIES_TAIL);
    private static readonly Dictionary<string, string> REAL = Concat(REAL_SERIES, REAL_STANDARD);
    private static readonly Dictionary<string, string> COMPLEX_STANDARD = AddSuffix(new()
        { { "conjugate", CONJ }, { "Conjugate", CONJ }, { "conj", CONJ }, { "Conj", CONJ }, { "ei", EI }, { "Ei", EI } }, COMPLEX_TAIL);
    private static readonly Dictionary<string, string> COMPLEX_SERIES = AddSuffix(AddSuffix(new()
        { { "real", _REAL }, { "Real", _REAL } }, COMPLEX_TAIL), SERIES_TAIL);
    private static readonly Dictionary<string, string> COMPLEX = Concat(COMPLEX_SERIES, COMPLEX_STANDARD);
    private static readonly Dictionary<string, string> CONSTANTS = new()
        { { "pi", PI }, { "Pi", PI }, { "gamma", _GA }, { "Gamma", _GA }, { "ga", _GA }, { "Ga", _GA } };
    private static readonly Dictionary<string, string> TAGS = AddSuffix(new()
        {
            { "substitute", SUBS}, { "Substitute", SUBS}, { "subs", SUBS}, { "Subs", SUBS},
            { "iterateLoop", ITLOOP }, { "IterateLoop", ITLOOP }, { "itLoop", ITLOOP }, { "ItLoop", ITLOOP }, // Must precede "loop"
            { "loop", LOOP }, { "Loop", LOOP },
            { "function", _FUNC }, { "Function", _FUNC }, { "func", _FUNC }, { "Func", _FUNC },
            { "polar", _POLAR }, { "Polar", _POLAR },
            { "parametric", _PARAM }, { "Parametric", _PARAM }, { "param", _PARAM }, { "Param", _PARAM }
        }, SERIES_TAIL);
    private static readonly Dictionary<string, string> REAL_COMPLEX = Concat(REAL, COMPLEX);

    private static Dictionary<string, string> AddBase(Action<Dictionary<string, string>> action)
    { Dictionary<string, string> _dictionary = []; action(_dictionary); return _dictionary; }
    private static Dictionary<string, string> AddPrefixSuffix(Dictionary<string, string> dictionary) => AddBase(_dictionary =>
    { foreach (var kvp in dictionary) _dictionary[String.Concat(kvp.Key, '(')] = String.Concat(FUNC_HEAD, kvp.Value, '('); });
    private static Dictionary<string, string> AddSuffix(Dictionary<string, string> dictionary, char suffix) => AddBase(_dictionary =>
    { foreach (var kvp in dictionary) _dictionary[kvp.Key] = String.Concat(kvp.Value, suffix); });
    private static string ReplaceBase(string input, Dictionary<string, string> dictionary)
    { foreach (var kvp in dictionary) input = input.Replace(kvp.Key, kvp.Value); return input; }
    private static string ReplaceConstant(string input) => ReplaceBase(input, CONSTANTS);
    private static string ReplaceCommon(string input) => ReplaceConstant(ReplaceBase(input, AddPrefixSuffix(COMMON)));
    protected static string ReplaceRealComplex(string input) => ReplaceCommon(ReplaceBase(input, AddPrefixSuffix(REAL_COMPLEX)));
    protected static string ReplaceCurves(string input) => ReplaceBase(input, AddPrefixSuffix(TAGS));
} /// Interprets function names
public class RecoverMultiply : ReplaceTags
{
    public static readonly string LR_BRA = "()", LR_CBRA = "{}", _ZZ_ = String.Concat(_Z, Z_), _XX__YY_ = String.Concat(_X, X_, _Y, Y_),
        _ZZ_BRA = String.Concat(_ZZ_, LR_CBRA), _XX__YY_BRA = String.Concat(_XX__YY_, LR_CBRA),
        BARRED_CHARS = String.Concat("\t!\"#$%&\':;<=>?@[\\]_`~", SUBS, ITLOOP, LOOP, _FUNC, _POLAR, _PARAM);
    private static readonly string VAR_REAL = _XX__YY_, VAR_COMPLEX = String.Concat(_ZZ_, I), CONST = String.Concat(E, P, G),
        ARITH = "+-*/^(,|", BRA_L = "({", BRA_R = ")}";
    public static readonly string[] ENTER_BLANK = ["\n", "\r", " "];

    public static string Simplify(string input)
    {
        ThrowException(!CheckParenthesis(input) || input.Contains(LR_BRA) || input.AsSpan().ContainsAny(BARRED_CHARS));
        return ReplaceRealComplex(ReplaceCurves(RemoveEnterBlank(input))); // Sensitive
    } // Used only once at the beginning
    protected static string Recover(ReadOnlySpan<char> input, bool isComplex)
    {
        if (input.Length == 1) return input.ToString();
        Func<char, bool> isVar = isComplex ? IsVarComplex : IsVarReal;
        StringBuilder recoveredInput = new(input.Length * 2); // Maximum possible length
        recoveredInput.Append(input[0]);
        for (int i = 1; i < input.Length; i++) // Do not parallelize this loop
        {
            if (DecideRecovery(input[i - 1], input[i], isVar)) recoveredInput.Append('*');
            recoveredInput.Append(input[i]);
        }
        return recoveredInput.ToString();
    } // Moved outside the loops
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
    private static bool IsArithmetic(char c) => ARITH.Contains(c); // Function heads after these operators do not require recovery
    private static bool IsFunctionHead(char c) => c == FUNC_HEAD;
    public static bool IsBraL(char c) => BRA_L.Contains(c);
    public static bool IsBraR(char c) => BRA_R.Contains(c);
} /// Restores omitted multiplication operators ("*")

/// <summary>
/// COMPUTATION SECTION
/// </summary>
public sealed class ComplexSub : RecoverMultiply
{
    #region Fields & Constructors
    private readonly uint colBytes, strdBytes, resBytes; // Chunk sizes in bytes
    private readonly int rows, columns, rowChk, strd, res, resInit; // Chunk lengths
    private readonly int[] rowOffs, strdInit; // For row extraction
    private readonly bool useList, brkChk; // useList: whether to use cstMtcs; brkChk: whether to split processing into chunks
    private readonly Matrix<Complex> z;
    private readonly Matrix<Complex>[] buffCocs; // Precomputes repeatedly used blocks
    private readonly MatrixCopy<Complex>[] braValues; // Stores values for matching pairs of parentheses
    private readonly List<ConstMatrix<Complex>> cstMtcs = []; // Stores reusable constant matrices

    private int countBra, countCst; // countBra: parentheses, countCst: constants
    private bool readList; // Indicates whether cstMtcs is being read or written
    private string input;
    private Matrix<Complex> Z; // For substitution

    public ComplexSub(ReadOnlySpan<char> input, Matrix<Complex>? z, Matrix<Complex>? Z, Matrix<Complex>[]? buffCocs,
        int rows, int columns, bool useList = false)
    {
        this.input = Recover(input, true); brkChk = CountChars(this.input, "+-*/^") > BRKCHK;
        braValues = new MatrixCopy<Complex>[CountChars(this.input, "(")];
        if (z != null) this.z = (Matrix<Complex>)z; if (Z != null) this.Z = (Matrix<Complex>)Z;
        this.rows = rows; this.columns = columns; this.useList = useList; this.buffCocs = buffCocs;
        Initialize<Complex>(rows, columns, ref rowChk, ref rowOffs, ref colBytes,
            ref strd, ref strdInit, ref strdBytes, ref res, ref resInit, ref resBytes);
    }
    public ComplexSub(ReadOnlySpan<char> input, Matrix<Real> xCoor, Matrix<Real> yCoor, int rows, int columns)
        : this(input, InitilizeZ(xCoor, yCoor, rows, columns), null, null, rows, columns) { }
    private ComplexSub ObtainSub(ReadOnlySpan<char> input, Matrix<Complex>? Z, Matrix<Complex>[]? buffCocs, bool useList = false)
        => new(input, z, Z, buffCocs, rows, columns, useList);
    private Matrix<Complex> ObtainValue(ReadOnlySpan<char> input) => new ComplexSub(input, z, Z, buffCocs, rows, columns).Obtain();
    private static Complex Obtain(ReadOnlySpan<char> input) => new ComplexSub(input, null, null, null, 1, 1).Obtain(false)[0, 0];
    #endregion

    #region Calculations
    private unsafe Matrix<Complex> Hypergeometric(string[] split) // Reference: https://en.wikipedia.org/wiki/Hypergeometric_function
        => HandleMtx(Const(Complex.ZERO, true), sum =>
        {
            var (start, end) = ObtainStartEnd(split, 4, 0, 100);
            Matrix<Complex> obtain(int index) => ObtainValue(split[index]);
            Matrix<Complex> a = obtain(0), b = obtain(1), c = obtain(2), initial = obtain(3);
            void hypergeometric(int p, int col)
            {
                Complex* sumPtr = sum.RowPtr(p), aPtr = a.RowPtr(p), bPtr = b.RowPtr(p), cPtr = c.RowPtr(p), initialPtr = initial.RowPtr(p);
                for (int q = 0; q < col; q++, sumPtr++, aPtr++, bPtr++, cPtr++, initialPtr++)
                {
                    Complex product = Complex.ONE; Real temp;
                    for (int i = start; i <= end; i++)
                    {
                        if (i != start) { temp = i - 1; product *= *initialPtr * (temp + *aPtr) * (temp + *bPtr) / (temp + *cPtr) / i; }
                        *sumPtr += product;
                    }
                }
            }
            if (rows == 1) { hypergeometric(0, columns); return; }
            Parallel.For(0, rowChk, p => { hypergeometric(strdInit[p], strd); }); if (res != 0) hypergeometric(resInit, res);
        });
    private unsafe Matrix<Complex> Gamma(string[] split) // Reference: https://en.wikipedia.org/wiki/Gamma_function
        => HandleMtx(UninitMtx(true), output =>
        {
            var (start, end) = ObtainStartEnd(split, 1, 1, 100);
            Matrix<Complex> initial = ObtainValue(split[0]);
            void gamma(int p, int col)
            {
                Complex* initialPtr = initial.RowPtr(p), outputPtr = output.RowPtr(p);
                for (int q = 0; q < col; q++, initialPtr++, outputPtr++)
                {
                    Complex product = Complex.ONE, temp;
                    for (int i = start; i <= end; i++) { temp = *initialPtr / i; product *= Complex.Exp(temp) / (1 + temp); }
                    *outputPtr = product * Complex.Exp(-*initialPtr * GAMMA) / *initialPtr;
                }
            }
            if (rows == 1) { gamma(0, columns); return; }
            Parallel.For(0, rowChk, p => { gamma(strdInit[p], strd); }); if (res != 0) gamma(resInit, res);
        });
    private unsafe Matrix<Complex> Beta(string[] split) // Reference: https://en.wikipedia.org/wiki/Beta_function
        => HandleMtx(UninitMtx(true), output =>
        {
            var (start, end) = ObtainStartEnd(split, 2, 1, 100);
            Matrix<Complex> initial1 = ObtainValue(split[0]), initial2 = ObtainValue(split[1]);
            void beta(int p, int col)
            {
                Complex* initial1Ptr = initial1.RowPtr(p), initial2Ptr = initial2.RowPtr(p), outputPtr = output.RowPtr(p);
                for (int q = 0; q < col; q++, initial1Ptr++, initial2Ptr++, outputPtr++)
                {
                    Complex product = Complex.ONE, initSum = *initial1Ptr + *initial2Ptr, initProd = *initial1Ptr * *initial2Ptr;
                    for (int i = start; i <= end; i++) product *= 1 + initProd / (i + initSum) / i;
                    *outputPtr = initSum / initProd / product;
                }
            }
            if (rows == 1) { beta(0, columns); return; }
            Parallel.For(0, rowChk, p => { beta(strdInit[p], strd); }); if (res != 0) beta(resInit, res);
        });
    private unsafe Matrix<Complex> Zeta(string[] split) // Reference: https://en.wikipedia.org/wiki/Riemann_zeta_function
        => HandleMtx(Const(Complex.ZERO, true), sum =>
        {
            var (start, end) = ObtainStartEnd(split, 1, 0, 50);
            Matrix<Complex> initial = ObtainValue(split[0]); var (coeffSeq, _coeffSeq, logSeq) = GetSeqsForZeta(start, end);
            void zeta(int p, int col)
            {
                Complex* sumPtr = sum.RowPtr(p), initialPtr = initial.RowPtr(p);
                for (int q = 0; q < col; q++, sumPtr++, initialPtr++)
                {
                    Complex _sum = Complex.ZERO, initNeg = -*initialPtr;
                    for (int i = start, k = start; i <= end; i++)
                    {
                        for (int j = start; j <= i; j++, k++) _sum += Complex.Exp(initNeg * logSeq[j]) * _coeffSeq[k];
                        *sumPtr += _sum * coeffSeq[i]; _sum = Complex.ZERO;
                    }
                    *sumPtr /= 1 - Complex.Exp((1 + initNeg) * LOG2);
                }
            }
            if (rows == 1) { zeta(0, columns); return; }
            Parallel.For(0, rowChk, p => { zeta(strdInit[p], strd); }); if (res != 0) zeta(resInit, res);
        });
    private unsafe Matrix<Complex> ProcessSH(string[] split, Func<Complex, Real, Complex, Complex> function)
    {
        ThrowInvalidLengths(split, [4]); Matrix<Complex> _z = UninitMtx();
        Real obtain(int i) => RealSub.Obtain(split[i]); Real r = obtain(0); Complex ctr = new(obtain(1), obtain(2));
        void processSH(int p, int col)
        {
            Complex* zPtr = z.RowPtr(p), _zPtr = _z.RowPtr(p);
            for (int q = 0; q < col; q++, zPtr++, _zPtr++) *_zPtr = function(*zPtr, r, ctr);
        }
        if (rows == 1) processSH(0, columns);
        else { Parallel.For(0, rowChk, p => { processSH(strdInit[p], strd); }); if (res != 0) processSH(resInit, res); }
        return new ComplexSub(split[3], _z, Z, buffCocs, rows, columns).Obtain();
    }
    private Matrix<Complex> ProcessSPI(string[] split, int validLength, Matrix<Complex> initMtx, Action<ComplexSub> action)
    {
        ThrowInvalidLengths(split, [validLength, validLength - 2]); bool sub = split.Length == validLength;
        int subIdx = validLength - 3; if (sub) split[0] = Recover(ReplaceLoop(split, 0, subIdx, split[subIdx], true), true);
        ComplexSub buffer = ObtainSub(sub ? ReplaceLoop(split, 0, subIdx, "0") : split[0], initMtx, buffCocs, true);

        CheckFor(sub ? RealSub.ToInt(split[subIdx + 1]) : 1, RealSub.ToInt(split[sub ? subIdx + 2 : subIdx]), i =>
        {
            if (sub) buffer.input = ReplaceLoop(split, 0, subIdx, i.ToString()); buffer.countBra = buffer.countCst = 0;
            action(buffer); if (!buffer.readList) buffer.readList = true; // Precomputes cstMtcs
        });
        return buffer.Z;
    } // Meticulously optimized
    private Matrix<Complex> ProcessI2C2(string[] split, Func<string[], (string, Matrix<Real>, Matrix<Real>)> function)
    {
        var (input, xCoor, yCoor) = function(split);
        return new ComplexSub(input, xCoor, yCoor, rows, columns).Obtain();
    }
    private Matrix<Complex> Stereographic(string[] split) => ProcessSH(split, Complex.Stereographic);
    private Matrix<Complex> Homothety(string[] split) => ProcessSH(split, Complex.Homothety);
    private Matrix<Complex> Sum(string[] split) => ProcessSPI(split, 4, Const(Complex.ZERO), b => { Plus(b.Obtain(), b.Z); });
    private Matrix<Complex> Product(string[] split) => ProcessSPI(split, 4, Const(Complex.ONE), b => { Multiply(b.Obtain(), b.Z); });
    public Matrix<Complex> Iterate(string[] split) => ProcessSPI(split, 5, ObtainValue(split[1]), b => { b.Z = b.Obtain(); });
    private Matrix<Complex> Iterate2(string[] split) => ProcessI2C2(split, new RealSub("0", z, rows, columns).ProcessIterate2);
    private Matrix<Complex> Compose2(string[] split) => ProcessI2C2(split, new RealSub("0", z, rows, columns).ProcessCompose2);
    public Matrix<Complex> Compose(string[] split)
    {
        Matrix<Complex> _value = ObtainValue(split[0]);
        for (int i = 1; i < split.Length; i++) _value = ObtainSub(split[i], _value, buffCocs).Obtain();
        return _value;
    } // Do not use HandleMtx
    private Matrix<Complex> Cocoon(string[] split)
    {
        ComplexSub body = ObtainSub(split[0], Z, new Matrix<Complex>[split.Length - 1]);
        for (int i = 1; i < split.Length; i++) body.buffCocs[i - 1] = ObtainValue(split[i]);
        return body.Obtain();
    } // Used for shallow but complicated compositions
    private Matrix<Complex> RealBlock(string[] split) { ThrowInvalidLengths(split, [1]); return Const(new(RealSub.Obtain(split[0])), true); }
    #endregion

    #region Elements
    public unsafe static Matrix<Complex> InitilizeZ(Matrix<Real> xCoor, Matrix<Real> yCoor, int rows, int columns)
    {
        Matrix<Complex> zCoor = new(GetArithProg(rows, columns), columns);
        Parallel.For(0, rows, p =>
        {
            Complex* zCoorPtr = zCoor.RowPtr(p); Real* xCoorPtr = xCoor.RowPtr(p), yCoorPtr = yCoor.RowPtr(p);
            for (int q = 0; q < columns; q++, zCoorPtr++, xCoorPtr++, yCoorPtr++) *zCoorPtr = new(*xCoorPtr, *yCoorPtr);
        });
        return zCoor;
    } // Cannot use HandleMtx in a static method
    private unsafe Matrix<Complex> Copy(Matrix<Complex> src, bool pooled = false) => HandleMtx(UninitMtx(pooled), dest =>
    {
        void copy(int p, uint colBytes) => Unsafe.CopyBlock(dest.RowPtr(p), src.RowPtr(p), colBytes);
        if (rows == 1) { copy(0, colBytes); return; }
        Parallel.For(0, rowChk, p => { copy(strdInit[p], strdBytes); }); if (res != 0) copy(resInit, resBytes);
    });
    private unsafe Matrix<Complex> Const(Complex _const, bool pooled = false) => HandleMtx(UninitMtx(pooled), output =>
    {
        Complex* outputPtr = output.RowPtr(), _outputPtr = outputPtr;
        for (int q = 0; q < strd; q++, outputPtr++) *outputPtr = _const; if (rows == 1) return;
        void copy(int p, uint colBytes) => Unsafe.CopyBlock(output.RowPtr(p), _outputPtr, colBytes);
        Parallel.For(1, rowChk, p => { copy(strdInit[p], strdBytes); }); if (res != 0) copy(resInit, resBytes);
    }); // Sensitive
    private unsafe void Negate(Matrix<Complex> _value)
    {
        void negate(int p, int col)
        {
            Complex* _valuePtr = _value.RowPtr(p);
            for (int q = 0; q < col; q++, _valuePtr++) *_valuePtr = -*_valuePtr;
        }
        if (rows == 1) { negate(0, columns); return; }
        Parallel.For(0, rowChk, p => { negate(strdInit[p], strd); }); if (res != 0) negate(resInit, res);
    }
    private unsafe void Invert(Matrix<Complex> _value)
    {
        void invert(int p, int col)
        {
            Complex* _valuePtr = _value.RowPtr(p);
            for (int q = 0; q < col; q++, _valuePtr++) *_valuePtr = ~*_valuePtr;
        }
        if (rows == 1) { invert(0, columns); return; }
        Parallel.For(0, rowChk, p => { invert(strdInit[p], strd); }); if (res != 0) invert(resInit, res);
    }
    private unsafe void Plus(Matrix<Complex> src, Matrix<Complex> dest)
    {
        void plus(int p, int col)
        {
            Complex* destPtr = dest.RowPtr(p), srcPtr = src.RowPtr(p);
            for (int q = 0; q < col; q++, destPtr++, srcPtr++) *destPtr += *srcPtr;
        }
        if (rows == 1) { plus(0, columns); return; }
        Parallel.For(0, rowChk, p => { plus(strdInit[p], strd); }); if (res != 0) plus(resInit, res);
    }
    private unsafe void Subtract(Matrix<Complex> src, Matrix<Complex> dest)
    {
        void subtract(int p, int col)
        {
            Complex* destPtr = dest.RowPtr(p), srcPtr = src.RowPtr(p);
            for (int q = 0; q < col; q++, destPtr++, srcPtr++) *destPtr -= *srcPtr;
        }
        if (rows == 1) { subtract(0, columns); return; }
        Parallel.For(0, rowChk, p => { subtract(strdInit[p], strd); }); if (res != 0) subtract(resInit, res);
    }
    private unsafe void Multiply(Matrix<Complex> src, Matrix<Complex> dest)
    {
        void multiply(int p, int col)
        {
            Complex* destPtr = dest.RowPtr(p), srcPtr = src.RowPtr(p);
            for (int q = 0; q < col; q++, destPtr++, srcPtr++) *destPtr *= *srcPtr;
        }
        if (rows == 1) { multiply(0, columns); return; }
        Parallel.For(0, rowChk, p => { multiply(strdInit[p], strd); }); if (res != 0) multiply(resInit, res);
    }
    private unsafe void Divide(Matrix<Complex> src, Matrix<Complex> dest)
    {
        void divide(int p, int col)
        {
            Complex* destPtr = dest.RowPtr(p), srcPtr = src.RowPtr(p);
            for (int q = 0; q < col; q++, destPtr++, srcPtr++) *destPtr /= *srcPtr;
        }
        if (rows == 1) { divide(0, columns); return; }
        Parallel.For(0, rowChk, p => { divide(strdInit[p], strd); }); if (res != 0) divide(resInit, res);
    }
    private unsafe void Power(Matrix<Complex> src, Matrix<Complex> dest)
    {
        void power(int p, int col)
        {
            Complex* destPtr = dest.RowPtr(p), srcPtr = src.RowPtr(p);
            for (int q = 0; q < col; q++, destPtr++, srcPtr++) *destPtr = Complex.Pow(*srcPtr, *destPtr);
        }
        if (rows == 1) { power(0, columns); return; }
        Parallel.For(0, rowChk, p => { power(strdInit[p], strd); }); if (res != 0) power(resInit, res);
    }
    private unsafe void FuncSub(Matrix<Complex> _value, Func<Complex, Complex> function)
    {
        void funcSub(int p, int col)
        {
            Complex* _valuePtr = _value.RowPtr(p);
            for (int q = 0; q < col; q++, _valuePtr++) *_valuePtr = function(*_valuePtr);
        }
        if (rows == 1) { funcSub(0, columns); return; }
        Parallel.For(0, rowChk, p => { funcSub(strdInit[p], strd); }); if (res != 0) funcSub(resInit, res);
    }
    #endregion

    #region Assembly
    private Matrix<Complex> UninitMtx(bool pooled = false) => pooled ? Matrix<Complex>.Rent(rowOffs, columns) : new(rowOffs, columns);
    private Matrix<Complex> CopyMtx(MatrixCopy<Complex> mc, bool pooled = false) => mc.copy ? Copy(mc.matrix, pooled) : mc.matrix;
    private static void ReleaseMtx(MatrixCopy<Complex> mc) { if (!mc.copy && mc.matrix.IsPooled()) mc.matrix.Return(); }
    private Matrix<Complex> FinalizeMtx(MatrixCopy<Complex> mc)
    { if (!mc.matrix.IsPooled()) return mc.matrix; Matrix<Complex> output = Copy(mc.matrix); mc.matrix.Return(); return output; }
    private MatrixCopy<Complex> ConstMtx(Complex _const, bool pooled = false)
    {
        if (!useList) return new(Const(_const, pooled));
        if (!readList) { cstMtcs.Add(new(_const, Const(_const))); return new(cstMtcs[^1].matrix, true); }
        ConstMatrix<Complex> cm = cstMtcs[countCst];
        bool equal = _const.Equals(cm._const); Matrix<Complex> mtx = equal ? cm.matrix : Const(_const, pooled); countCst++;
        return new(mtx, equal);
    } // Cached constants must remain ordinary matrices
    private MatrixCopy<Complex> Transform(ReadOnlySpan<char> input, bool pooled = false) => input[0] switch
    {
        _Z => HandleSolo<Complex>(input, new(z, true)),
        Z_ => HandleSolo<Complex>(input, new(Z, true)),
        '{' => new(buffCocs[Int32.Parse(TryBraNum(input, '{', '}'))], true),
        '[' => braValues[Int32.Parse(TryBraNum(input, '[', ']'))],
        I => HandleSolo(input, ConstMtx(Complex.I, pooled)),
        E => HandleSolo(input, ConstMtx(new(MathR.E), pooled)),
        P => HandleSolo(input, ConstMtx(new(MathR.PI), pooled)),
        G => HandleSolo(input, ConstMtx(new(GAMMA), pooled)),
        _ => ConstMtx(new(Real.Parse(input)), pooled)
    };
    private MatrixCopy<Complex> BreakPower(string input, bool pooled)
    {
        string[] chunks = PrepareBreakPower(input, THRESHOLD);
        Matrix<Complex> tower = CopyMtx(PowerCore(chunks[^1], pooled), pooled);
        for (int k = chunks.Length - 2; k >= 0; k--)
        {
            string[] split = SplitByChars(chunks[k], "^"); // Special handling for "^"
            for (int m = split.Length - 1; m >= 0; m--)
            { MatrixCopy<Complex> src = Transform(split[m], true); Power(src.matrix, tower); ReleaseMtx(src); }
        }
        return new(tower);
    }
    private MatrixCopy<Complex> PowerCore(ReadOnlySpan<char> input, bool pooled = false)
    {
        if (!input.Contains('^')) return Transform(input, pooled);
        if (brkChk) if (CountChars(input, "^") > THRESHOLD) return BreakPower(input.ToString(), pooled);
        string[] split = SplitByChars(input, "^");
        Matrix<Complex> tower = CopyMtx(Transform(split[^1], pooled), pooled);
        for (int k = split.Length - 2; k >= 0; k--)
        { MatrixCopy<Complex> src = Transform(split[k], true); Power(src.matrix, tower); ReleaseMtx(src); }
        return new(tower);
    }
    private MatrixCopy<Complex> BreakMultiplyDivide(string input, bool pooled)
    {
        var (chunks, signs) = PrepareBreakPSMD(input[0] == '/' ? input : String.Concat('*', input), "*/", THRESHOLD);
        Matrix<Complex> product = CopyMtx(MultiplyDivideCore(TrimStartChar(chunks[0], '*'), pooled), pooled);
        for (int j = 1; j < chunks.Length; j++)
        {
            MatrixCopy<Complex> src = MultiplyDivideCore(signs[j - 1] == SUB_CHARS[0] ? chunks[j] : String.Concat('/', chunks[j]), true);
            Multiply(src.matrix, product); ReleaseMtx(src);
        }
        return new(product);
    }
    private MatrixCopy<Complex> MultiplyDivideCore(ReadOnlySpan<char> input, bool pooled = false)
    {
        if (!input.ContainsAny("*/")) return PowerCore(input, pooled);
        if (brkChk) if (CountChars(input, "*/") > THRESHOLD) return BreakMultiplyDivide(input.ToString(), pooled);
        var (split, signs) = GetPSMDComponents(input, "*/");
        Matrix<Complex> product = CopyMtx(PowerCore(split[0], pooled), pooled); if (signs[0] == '/') Invert(product);
        for (int j = 1; j < split.Length; j++)
        {
            MatrixCopy<Complex> src = PowerCore(split[j], true);
            Action<Matrix<Complex>, Matrix<Complex>> operation = signs[j] switch { '*' => Multiply, '/' => Divide };
            operation(src.matrix, product); ReleaseMtx(src);
        }
        return new(product);
    }
    private MatrixCopy<Complex> BreakPlusSubtract(string input, bool pooled)
    {
        var (chunks, signs) = PrepareBreakPSMD(input[0] == '-' ? input : String.Concat('+', input), "+-", THRESHOLD);
        Matrix<Complex> sum = CopyMtx(PlusSubtractCore(TrimStartChar(chunks[0], '+'), pooled), pooled);
        for (int i = 1; i < chunks.Length; i++)
        {
            MatrixCopy<Complex> src = PlusSubtractCore(signs[i - 1] == SUB_CHARS[0] ? chunks[i] : String.Concat('-', chunks[i]), true);
            Plus(src.matrix, sum); ReleaseMtx(src);
        }
        return new(sum);
    }
    private MatrixCopy<Complex> PlusSubtractCore(ReadOnlySpan<char> input, bool pooled = false)
    {
        if (!input.ContainsAny("+-")) return MultiplyDivideCore(input, pooled);
        if (brkChk) if (CountChars(input, "+-") > THRESHOLD) return BreakPlusSubtract(input.ToString(), pooled);
        var (split, signs) = GetPSMDComponents(input, "+-");
        Matrix<Complex> sum = CopyMtx(MultiplyDivideCore(split[0], pooled), pooled); if (signs[0] == '-') Negate(sum);
        for (int i = 1; i < split.Length; i++)
        {
            MatrixCopy<Complex> src = MultiplyDivideCore(split[i], true);
            Action<Matrix<Complex>, Matrix<Complex>> operation = signs[i] switch { '+' => Plus, '-' => Subtract };
            operation(src.matrix, sum); ReleaseMtx(src);
        }
        return new(sum);
    }
    private MatrixCopy<Complex> ComputeBraFreePart(ReadOnlySpan<char> input, bool pooled = false)
        => Int32.TryParse(input, out int result) ? ConstMtx(new(result), pooled) : PlusSubtractCore(input, pooled);
    private MatrixCopy<Complex> SubCore(string input, int start, MatrixCopy<Complex> bFValue, ref int tagL, bool pooled = false)
    {
        if (start == 0) return bFValue;
        var (isInverse, mtx, copy) = (IsInverseFunc(input, start), bFValue.matrix, bFValue.copy);
        int handleSub(Func<Complex, Complex> func, int tagL)
        {
            ThrowException(input[start - tagL] != FUNC_HEAD);
            mtx = CopyMtx(bFValue, pooled); FuncSub(mtx, func); copy = false; return tagL;
        }
        tagL = input[start - 1] switch
        {
            _A => handleSub(c => new(Complex.Modulus(c)), 2),
            _L => handleSub(Complex.Log, 2),
            E_ => handleSub(Complex.Exp, 2),
            _Q => handleSub(Complex.Sqrt, 2),
            _S => isInverse.trig ? handleSub(Complex.Asin, 3) : handleSub(Complex.Sin, 2),
            _C => isInverse.trig ? handleSub(Complex.Acos, 3) : handleSub(Complex.Cos, 2),
            _T => isInverse.trig ? handleSub(Complex.Atan, 3) : handleSub(Complex.Tan, 2),
            _H => input[start - 2] switch
            {
                _S => isInverse.hyper ? handleSub(Complex.Asinh, 4) : handleSub(Complex.Sinh, 3),
                _C => isInverse.hyper ? handleSub(Complex.Acosh, 4) : handleSub(Complex.Cosh, 3),
                _T => isInverse.hyper ? handleSub(Complex.Atanh, 4) : handleSub(Complex.Tanh, 3)
            },
            SP => input[start - 2] switch
            {
                J_ => handleSub(Complex.Conjugate, 3),
                E_ => handleSub(Complex.Ei, 3)
            }, // Complex-specific
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
            R_ => handleSub(Stereographic, 2),
            H_ => handleSub(Homothety, 2),
            S_ => handleSub(Sum, 2),
            P_ => handleSub(Product, 2),
            I_ => input[idx - 2] switch { TILDE => handleSub(Iterate, 2), MODE_2 => handleSub(Iterate2, 3) },
            J_ => input[idx - 2] switch { TILDE => handleSub(Compose, 2), MODE_2 => handleSub(Compose2, 3) },
            K_ => handleSub(Cocoon, 2),
            SP => handleSub(RealBlock, 3) // Complex-specific
        };
        braValues[countBra] = new(braFunc(split)); // No need to copy
        return ReplaceInput(input, countBra++, idx - tagL, end);
    }
    private Matrix<Complex> ObtainCore(string input)
    {
        while (input.Contains(SERIES_TAIL)) input = SeriesSub(input); // The number of substitutions is not known in advance
        var (length, start, end, tagL) = PrepareLoop(input);
        for (int i = 0; i < length; i++)
        {
            ResetStartEnd(input, ref start, ref end);
            braValues[countBra] = SubCore(input, start, ComputeBraFreePart(BraFreePart(input, start, end), true), ref tagL, true);
            input = ReplaceInput(input, countBra++, ref start, end, ref tagL);
        }
        return FinalizeMtx(ComputeBraFreePart(input));
    }
    public Matrix<Complex> Obtain(bool checkVar = true)
        => checkVar && !input.AsSpan().ContainsAny(_ZZ_BRA) ? Const(Obtain(input)) : ObtainCore(input);
    #endregion
} /// Computes complex-variable expressions
public sealed class RealSub : RecoverMultiply
{
    #region Fields & Constructors
    private readonly uint colBytes, strdBytes, resBytes; // Chunk sizes in bytes
    private readonly int rows, columns, rowChk, strd, res, resInit; // Chunk lengths
    private readonly int[] rowOffs, strdInit; // For row extraction
    private readonly bool useList, brkChk; // useList: whether to use cstMtcs; brkChk: whether to split processing into chunks
    private readonly Matrix<Real> x, y;
    private readonly Matrix<Real>[] buffCocs; // Precomputes repeatedly used blocks
    private readonly MatrixCopy<Real>[] braValues; // Stores values for matching pairs of parentheses
    private readonly List<ConstMatrix<Real>> cstMtcs = []; // Stores reusable constant matrices

    private int countBra, countCst; // countBra: parentheses, countCst: constants
    private bool readList; // Indicates whether cstMtcs is being read or written
    private string input;
    private Matrix<Real> X, Y; // For substitution

    public RealSub(ReadOnlySpan<char> input, Matrix<Real>? x, Matrix<Real>? y, Matrix<Real>? X, Matrix<Real>? Y, Matrix<Real>[]? buffCocs,
        int rows, int columns, bool useList = false)
    {
        this.input = Recover(input, false); brkChk = CountChars(this.input, "+-*/^") > BRKCHK;
        braValues = new MatrixCopy<Real>[CountChars(this.input, "(")];
        if (x != null) this.x = (Matrix<Real>)x; if (y != null) this.y = (Matrix<Real>)y;
        if (X != null) this.X = (Matrix<Real>)X; if (Y != null) this.Y = (Matrix<Real>)Y;
        this.rows = rows; this.columns = columns; this.useList = useList; this.buffCocs = buffCocs;
        Initialize<Real>(rows, columns, ref rowChk, ref rowOffs, ref colBytes,
            ref strd, ref strdInit, ref strdBytes, ref res, ref resInit, ref resBytes);
    }
    private RealSub(ReadOnlySpan<char> input, (Matrix<Real> X, Matrix<Real> Y) xyCoor, int rows, int columns)
        : this(input, xyCoor.X, xyCoor.Y, null, null, null, rows, columns) { } // A helper constructor
    public RealSub(ReadOnlySpan<char> input, Matrix<Complex> zCoor, int rows, int columns)
        : this(input, InitializeXY(zCoor, rows, columns), rows, columns) { }
    private RealSub ObtainSub(ReadOnlySpan<char> input, Matrix<Real>? X, Matrix<Real>? Y, Matrix<Real>[]? buffCocs, bool useList = false)
        => new(input, x, y, X, Y, buffCocs, rows, columns, useList);
    private Matrix<Real> ObtainValue(ReadOnlySpan<char> input) => new RealSub(input, x, y, X, Y, buffCocs, rows, columns).Obtain();
    public static Real Obtain(ReadOnlySpan<char> input, Real? x = null)
        => new RealSub(input, x != null ? new((Real)x) : null, null, null, null, null, 1, 1).Obtain(false)[0, 0];
    public static int ToInt(ReadOnlySpan<char> input) => (int)Obtain(input); // Often used with RealComplex.CheckFor
    #endregion

    #region Basic Calculations
    private static Real SafeSign(Real r) => Real.IsNaN(r) ? Real.NaN : MathR.Sign(r); // MathR.Sign does not accept Real.NaN
    private static Real FactorialBase(int n) { if (n < 0) return Real.NaN; Real f = 1; for (; n > 1; n--) f *= n; return f; }
    private static Real Factorial(Real r) => MathR.Round(FactorialBase((int)MathR.Floor(r)));
    private static Real Mod(Real n, Real r) => r != 0 ? n % MathR.Abs(r) : Real.NaN;
    private static Real ParitySign(int a) => Int32.IsEvenInteger(a) ? 1 : -1;
    private static Real CombinationCore(int n, int r)
    { r = MathR.Min(r, n - r); Real c = 1; for (int i = 1; i <= r; i++, n--) c *= (Real)n / (Real)i; return c; }
    private static Real CombinationBase(int n, int r) // Generalized Pascal's triangle
        => (n == r || r == 0) ? 1 : (r > n && n >= 0 || 0 > r && r > n || n >= 0 && 0 > r) ? 0 : n >= 0 ? CombinationCore(n, r) :
        r >= 0 ? (ParitySign(r) * CombinationCore(r - n - 1, r)) : (ParitySign(n - r) * CombinationCore(-r - 1, -n - 1));
    private static Real Combination(Real n, Real r) => MathR.Round(CombinationBase((int)MathR.Floor(n), (int)MathR.Floor(r)));
    private static Real PermutationBase(int n, int r) { if (r < 0) return 0; Real p = 1; for (; r > 0; r--, n--) p *= n; return p; }
    private static Real Permutation(Real n, Real r) => MathR.Round(PermutationBase((int)MathR.Floor(n), (int)MathR.Floor(r)));
    private static Real Distance(Real[] array) { Real sum = 0; foreach (Real a in array) sum += a * a; return Real.Sqrt(sum); }
    private unsafe Matrix<Real> ProcessMCP(string[] split, Func<Real, Real, Real> function)
        => HandleMtx(UninitMtx(true), output =>
        {
            ThrowInvalidLengths(split, [2]);
            Matrix<Real> input1 = ObtainValue(split[0]), input2 = ObtainValue(split[1]);
            void processMCP(int p, int col)
            {
                Real* input1Ptr = input1.RowPtr(p), input2Ptr = input2.RowPtr(p), outputPtr = output.RowPtr(p);
                for (int q = 0; q < col; q++, outputPtr++, input1Ptr++, input2Ptr++) *outputPtr = function(*input1Ptr, *input2Ptr);
            }
            if (rows == 1) { processMCP(0, columns); return; }
            Parallel.For(0, rowChk, p => { processMCP(strdInit[p], strd); }); if (res != 0) processMCP(resInit, res);
        });
    private unsafe Matrix<Real> ProcessMMD(string[] split, Func<Real[], Real> function)
        => HandleMtx(UninitMtx(true), output =>
        {
            Matrix<Real>[] _value = new Matrix<Real>[split.Length];
            for (int i = 0; i < split.Length; i++) _value[i] = ObtainValue(split[i]);
            void processMMD(int p, int col)
            {
                Span<Real> array = stackalloc Real[split.Length]; Real* outputPtr = output.RowPtr(p);
                for (int q = 0; q < col; q++, outputPtr++)
                {
                    for (int i = 0; i < split.Length; i++) array[i] = _value[i][p, q];
                    *outputPtr = function(array.ToArray());
                }
            }
            if (rows == 1) { processMMD(0, columns); return; }
            Parallel.For(0, rowChk, p => { processMMD(strdInit[p], strd); }); if (res != 0) processMMD(resInit, res);
        });
    private Matrix<Real> Mod(string[] split) => ProcessMCP(split, Mod);
    private Matrix<Real> Combination(string[] split) => ProcessMCP(split, Combination);
    private Matrix<Real> Permutation(string[] split) => ProcessMCP(split, Permutation);
    private Matrix<Real> Max(string[] split) => ProcessMMD(split, _value => _value.Max());
    private Matrix<Real> Min(string[] split) => ProcessMMD(split, _value => _value.Min());
    private Matrix<Real> Distance(string[] split) => ProcessMMD(split, Distance);
    #endregion // Real-specific

    #region Additional Calculations
    private unsafe Matrix<Real> Hypergeometric(string[] split) // Reference: https://en.wikipedia.org/wiki/Hypergeometric_function
        => HandleMtx(Const(0, true), sum =>
        {
            var (start, end) = ObtainStartEnd(split, 4, 0, 100);
            Matrix<Real> obtain(int index) => ObtainValue(split[index]);
            Matrix<Real> a = obtain(0), b = obtain(1), c = obtain(2), initial = obtain(3);
            void hypergeometric(int p, int col)
            {
                Real* sumPtr = sum.RowPtr(p), aPtr = a.RowPtr(p), bPtr = b.RowPtr(p), cPtr = c.RowPtr(p), initialPtr = initial.RowPtr(p);
                for (int q = 0; q < col; q++, sumPtr++, aPtr++, bPtr++, cPtr++, initialPtr++)
                {
                    Real product = 1, temp;
                    for (int i = start; i <= end; i++)
                    {
                        if (i != start) { temp = i - 1; product *= *initialPtr * (temp + *aPtr) * (temp + *bPtr) / (temp + *cPtr) / i; }
                        *sumPtr += product;
                    }
                }
            }
            if (rows == 1) { hypergeometric(0, columns); return; }
            Parallel.For(0, rowChk, p => { hypergeometric(strdInit[p], strd); }); if (res != 0) hypergeometric(resInit, res);
        });
    private unsafe Matrix<Real> Gamma(string[] split) // Reference: https://en.wikipedia.org/wiki/Gamma_function
        => HandleMtx(UninitMtx(true), output =>
        {
            var (start, end) = ObtainStartEnd(split, 1, 1, 100);
            Matrix<Real> initial = ObtainValue(split[0]);
            void gamma(int p, int col)
            {
                Real* initialPtr = initial.RowPtr(p), outputPtr = output.RowPtr(p);
                for (int q = 0; q < col; q++, initialPtr++, outputPtr++)
                {
                    Real product = 1, temp;
                    for (int i = start; i <= end; i++) { temp = *initialPtr / i; product *= MathR.Exp(temp) / (1 + temp); }
                    *outputPtr = product * MathR.Exp(-*initialPtr * GAMMA) / *initialPtr;
                }
            }
            if (rows == 1) { gamma(0, columns); return; }
            Parallel.For(0, rowChk, p => { gamma(strdInit[p], strd); }); if (res != 0) gamma(resInit, res);
        });
    private unsafe Matrix<Real> Beta(string[] split) // Reference: https://en.wikipedia.org/wiki/Beta_function
        => HandleMtx(UninitMtx(true), output =>
        {
            var (start, end) = ObtainStartEnd(split, 2, 1, 100);
            Matrix<Real> initial1 = ObtainValue(split[0]), initial2 = ObtainValue(split[1]);
            void beta(int p, int col)
            {
                Real* initial1Ptr = initial1.RowPtr(p), initial2Ptr = initial2.RowPtr(p), outputPtr = output.RowPtr(p);
                for (int q = 0; q < col; q++, initial1Ptr++, initial2Ptr++, outputPtr++)
                {
                    Real product = 1, initSum = *initial1Ptr + *initial2Ptr, initProd = *initial1Ptr * *initial2Ptr;
                    for (int i = start; i <= end; i++) product *= 1 + initProd / (i + initSum) / i;
                    *outputPtr = initSum / initProd / product;
                }
            }
            if (rows == 1) { beta(0, columns); return; }
            Parallel.For(0, rowChk, p => { beta(strdInit[p], strd); }); if (res != 0) beta(resInit, res);
        });
    private unsafe Matrix<Real> Zeta(string[] split) // Reference: https://en.wikipedia.org/wiki/Riemann_zeta_function
        => HandleMtx(Const(0, true), sum =>
        {
            var (start, end) = ObtainStartEnd(split, 1, 0, 50);
            Matrix<Real> initial = ObtainValue(split[0]); var (coeffSeq, _coeffSeq, logSeq) = GetSeqsForZeta(start, end);
            void zeta(int p, int col)
            {
                Real* sumPtr = sum.RowPtr(p), initialPtr = initial.RowPtr(p);
                for (int q = 0; q < col; q++, sumPtr++, initialPtr++)
                {
                    Real _sum = 0, initNeg = -*initialPtr;
                    for (int i = start, k = start; i <= end; i++)
                    {
                        for (int j = start; j <= i; j++, k++) _sum += MathR.Exp(initNeg * logSeq[j]) * _coeffSeq[k];
                        *sumPtr += _sum * coeffSeq[i]; _sum = 0;
                    }
                    *sumPtr /= 1 - MathR.Exp((1 + initNeg) * LOG2);
                }
            }
            if (rows == 1) { zeta(0, columns); return; }
            Parallel.For(0, rowChk, p => { zeta(strdInit[p], strd); }); if (res != 0) zeta(resInit, res);
        });
    private unsafe Matrix<Real> ProcessSH(string[] split, Func<Complex, Real, Complex, Complex> function)
    {
        ThrowInvalidLengths(split, [4]); Matrix<Real> _x = UninitMtx(), _y = UninitMtx();
        Real obtain(int i) => Obtain(split[i]); Real r = obtain(0); Complex ctr = new(obtain(1), obtain(2));
        void processSH(int p, int col)
        {
            Real* xPtr = x.RowPtr(p), yPtr = y.RowPtr(p), _xPtr = _x.RowPtr(p), _yPtr = _y.RowPtr(p);
            for (int q = 0; q < col; q++, xPtr++, yPtr++, _xPtr++, _yPtr++)
                (*_xPtr, *_yPtr) = Complex.ReIm(function(new(*xPtr, *yPtr), r, ctr));
        }
        if (rows == 1) processSH(0, columns);
        else { Parallel.For(0, rowChk, p => { processSH(strdInit[p], strd); }); if (res != 0) processSH(resInit, res); }
        return new RealSub(split[3], _x, _y, X, Y, buffCocs, rows, columns).Obtain();
    }
    private Matrix<Real> ProcessSPI(string[] split, int validLength, Matrix<Real> initMtx, Action<RealSub> action)
    {
        ThrowInvalidLengths(split, [validLength, validLength - 2]); bool sub = split.Length == validLength;
        int subIdx = validLength - 3; if (sub) split[0] = Recover(ReplaceLoop(split, 0, subIdx, split[subIdx], true), false);
        RealSub buffer = ObtainSub(sub ? ReplaceLoop(split, 0, subIdx, "0") : split[0], initMtx, null, buffCocs, true);

        CheckFor(sub ? ToInt(split[subIdx + 1]) : 1, ToInt(split[sub ? subIdx + 2 : subIdx]), i =>
        {
            if (sub) buffer.input = ReplaceLoop(split, 0, subIdx, i.ToString()); buffer.countBra = buffer.countCst = 0;
            action(buffer); if (!buffer.readList) buffer.readList = true; // Precomputes cstMtcs
        });
        return buffer.X;
    } // Meticulously optimized
    private Matrix<Real> ProcessIC(string[] split, Func<string[], Matrix<Complex>> function)
        => new RealSub(split[^1], function(split[..^1]), rows, columns).Obtain();
    public (string, Matrix<Real>, Matrix<Real>) ProcessIterate2(string[] split)
    {
        ThrowInvalidLengths(split, [8, 6]); bool sub = split.Length == 8;
        string replaceLoop(int i) => Recover(ReplaceLoop(split, i, 4, split[4], true), false);
        RealSub obtain(int i) => ObtainSub(sub ? ReplaceLoop(split, i, 4, "0") : split[i],
            ObtainValue(split[2]), ObtainValue(split[3]), buffCocs, true);
        if (sub) (split[0], split[1]) = (replaceLoop(0), replaceLoop(1)); var (buffer1, buffer2) = (obtain(0), obtain(1));

        CheckFor(sub ? ToInt(split[5]) : 1, ToInt(split[sub ? 6 : 4]), i =>
        {
            if (sub) (buffer1.input, buffer2.input) = (ReplaceLoop(split, 1, 4, i.ToString()), ReplaceLoop(split, 0, 4, i.ToString()));
            buffer1.countBra = buffer1.countCst = buffer2.countBra = buffer2.countCst = 0;
            var (temp1, temp2) = (buffer1.Obtain(), buffer2.Obtain()); // Necessary
            buffer1.X = buffer2.X = temp1; buffer1.Y = buffer2.Y = temp2;
            if (!buffer1.readList) buffer1.readList = buffer2.readList = true; // Precomputes cstMtcs
        });
        return (split[^1], buffer1.X, buffer1.Y); // buffer2 would work as well
    }
    public (string, Matrix<Real>, Matrix<Real>) ProcessCompose2(string[] split)
    {
        ThrowException(Int32.IsEvenInteger(split.Length));
        var (value1, value2) = (ObtainValue(split[0]), ObtainValue(split[1]));
        for (int i = 0, j = 2; i < split.Length / 2 - 1; i++)
        {
            var (temp1, temp2) = (value1, value2); // Necessary
            Matrix<Real> obtainValue() => ObtainSub(split[j++], temp1, temp2, buffCocs).Obtain();
            value1 = obtainValue(); value2 = obtainValue(); // Even and odd terms, respectively
        }
        return (split[^1], value1, value2);
    }
    private Matrix<Real> Stereographic(string[] split) => ProcessSH(split, Complex.Stereographic);
    private Matrix<Real> Homothety(string[] split) => ProcessSH(split, Complex.Homothety);
    private Matrix<Real> Sum(string[] split) => ProcessSPI(split, 4, Const(0), b => { Plus(b.Obtain(), b.X); });
    private Matrix<Real> Product(string[] split) => ProcessSPI(split, 4, Const(1), b => { Multiply(b.Obtain(), b.X); });
    private Matrix<Real> Iterate1(string[] split) => ProcessSPI(split, 5, ObtainValue(split[1]), b => { b.X = b.Obtain(); });
    private Matrix<Real> Iterate(string[] split) => ProcessIC(split, new ComplexSub("0", x, y, rows, columns).Iterate);
    private Matrix<Real> Compose(string[] split) => ProcessIC(split, new ComplexSub("0", x, y, rows, columns).Compose);
    private Matrix<Real> Iterate2(string[] split) => ChooseMode(ProcessIterate2(split));
    private Matrix<Real> Compose2(string[] split) => ChooseMode(ProcessCompose2(split));
    private Matrix<Real> Compose1(string[] split)
    {
        Matrix<Real> _value = ObtainValue(split[0]);
        for (int i = 1; i < split.Length; i++) _value = ObtainSub(split[i], _value, null, buffCocs).Obtain();
        return _value;
    } // Do not use HandleMtx
    private Matrix<Real> Cocoon(string[] split)
    {
        RealSub body = ObtainSub(split[0], X, Y, new Matrix<Real>[split.Length - 1]);
        for (int i = 1; i < split.Length; i++) body.buffCocs[i - 1] = ObtainValue(split[i]);
        return body.Obtain();
    } // Used for shallow but complicated compositions
    #endregion

    #region Elements
    public unsafe static (Matrix<Real>, Matrix<Real>) InitializeXY(Matrix<Complex> zCoor, int rows, int columns)
    {
        Matrix<Real> xCoor = new(GetArithProg(rows, columns), columns), yCoor = new(GetArithProg(rows, columns), columns);
        Parallel.For(0, rows, p =>
        {
            Real* xCoorPtr = xCoor.RowPtr(p), yCoorPtr = yCoor.RowPtr(p); Complex* zCoorPtr = zCoor.RowPtr(p);
            for (int q = 0; q < columns; q++, xCoorPtr++, yCoorPtr++, zCoorPtr++) (*xCoorPtr, *yCoorPtr) = Complex.ReIm(*zCoorPtr);
        });
        return (xCoor, yCoor);
    } // Cannot use HandleMtx in a static method
    private unsafe Matrix<Real> Copy(Matrix<Real> src, bool pooled = false) => HandleMtx(UninitMtx(pooled), dest =>
    {
        void copy(int p, uint colBytes) => Unsafe.CopyBlock(dest.RowPtr(p), src.RowPtr(p), colBytes);
        if (rows == 1) { copy(0, colBytes); return; }
        Parallel.For(0, rowChk, p => { copy(strdInit[p], strdBytes); }); if (res != 0) copy(resInit, resBytes);
    });
    private unsafe Matrix<Real> Const(Real _const, bool pooled = false) => HandleMtx(UninitMtx(pooled), output =>
    {
        Real* outputPtr = output.RowPtr(), _outputPtr = outputPtr;
        for (int q = 0; q < strd; q++, outputPtr++) *outputPtr = _const; if (rows == 1) return;
        void copy(int p, uint colBytes) => Unsafe.CopyBlock(output.RowPtr(p), _outputPtr, colBytes);
        Parallel.For(1, rowChk, p => { copy(strdInit[p], strdBytes); }); if (res != 0) copy(resInit, resBytes);
    }); // Sensitive
    private unsafe void Negate(Matrix<Real> _value)
    {
        void negate(int p, int col)
        {
            Real* _valuePtr = _value.RowPtr(p);
            for (int q = 0; q < col; q++, _valuePtr++) *_valuePtr = -*_valuePtr;
        }
        if (rows == 1) { negate(0, columns); return; }
        Parallel.For(0, rowChk, p => { negate(strdInit[p], strd); }); if (res != 0) negate(resInit, res);
    }
    private unsafe void Invert(Matrix<Real> _value)
    {
        void invert(int p, int col)
        {
            Real* _valuePtr = _value.RowPtr(p);
            for (int q = 0; q < col; q++, _valuePtr++) *_valuePtr = 1 / *_valuePtr;
        }
        if (rows == 1) { invert(0, columns); return; }
        Parallel.For(0, rowChk, p => { invert(strdInit[p], strd); }); if (res != 0) invert(resInit, res);
    }
    private unsafe void Plus(Matrix<Real> src, Matrix<Real> dest)
    {
        void plus(int p, int col)
        {
            Real* destPtr = dest.RowPtr(p), srcPtr = src.RowPtr(p);
            for (int q = 0; q < col; q++, destPtr++, srcPtr++) *destPtr += *srcPtr;
        }
        if (rows == 1) { plus(0, columns); return; }
        Parallel.For(0, rowChk, p => { plus(strdInit[p], strd); }); if (res != 0) plus(resInit, res);
    }
    private unsafe void Subtract(Matrix<Real> src, Matrix<Real> dest)
    {
        void subtract(int p, int col)
        {
            Real* destPtr = dest.RowPtr(p), srcPtr = src.RowPtr(p);
            for (int q = 0; q < col; q++, destPtr++, srcPtr++) *destPtr -= *srcPtr;
        }
        if (rows == 1) { subtract(0, columns); return; }
        Parallel.For(0, rowChk, p => { subtract(strdInit[p], strd); }); if (res != 0) subtract(resInit, res);
    }
    private unsafe void Multiply(Matrix<Real> src, Matrix<Real> dest)
    {
        void multiply(int p, int col)
        {
            Real* destPtr = dest.RowPtr(p), srcPtr = src.RowPtr(p);
            for (int q = 0; q < col; q++, destPtr++, srcPtr++) *destPtr *= *srcPtr;
        }
        if (rows == 1) { multiply(0, columns); return; }
        Parallel.For(0, rowChk, p => { multiply(strdInit[p], strd); }); if (res != 0) multiply(resInit, res);
    }
    private unsafe void Divide(Matrix<Real> src, Matrix<Real> dest)
    {
        void divide(int p, int col)
        {
            Real* destPtr = dest.RowPtr(p), srcPtr = src.RowPtr(p);
            for (int q = 0; q < col; q++, destPtr++, srcPtr++) *destPtr /= *srcPtr;
        }
        if (rows == 1) { divide(0, columns); return; }
        Parallel.For(0, rowChk, p => { divide(strdInit[p], strd); }); if (res != 0) divide(resInit, res);
    }
    private unsafe void Power(Matrix<Real> src, Matrix<Real> dest)
    {
        void power(int p, int col)
        {
            Real* destPtr = dest.RowPtr(p), srcPtr = src.RowPtr(p);
            for (int q = 0; q < col; q++, destPtr++, srcPtr++) *destPtr = MathR.Pow(*srcPtr, *destPtr);
        }
        if (rows == 1) { power(0, columns); return; }
        Parallel.For(0, rowChk, p => { power(strdInit[p], strd); }); if (res != 0) power(resInit, res);
    }
    private unsafe void FuncSub(Matrix<Real> _value, Func<Real, Real> function)
    {
        void funcSub(int p, int col)
        {
            Real* _valuePtr = _value.RowPtr(p);
            for (int q = 0; q < col; q++, _valuePtr++) *_valuePtr = function(*_valuePtr);
        }
        if (rows == 1) { funcSub(0, columns); return; }
        Parallel.For(0, rowChk, p => { funcSub(strdInit[p], strd); }); if (res != 0) funcSub(resInit, res);
    }
    #endregion

    #region Assembly
    private Matrix<Real> UninitMtx(bool pooled = false) => pooled ? Matrix<Real>.Rent(rowOffs, columns) : new(rowOffs, columns);
    private Matrix<Real> CopyMtx(MatrixCopy<Real> mc, bool pooled = false) => mc.copy ? Copy(mc.matrix, pooled) : mc.matrix;
    private static void ReleaseMtx(MatrixCopy<Real> mc) { if (!mc.copy && mc.matrix.IsPooled()) mc.matrix.Return(); }
    private Matrix<Real> FinalizeMtx(MatrixCopy<Real> mc)
    { if (!mc.matrix.IsPooled()) return mc.matrix; Matrix<Real> output = Copy(mc.matrix); mc.matrix.Return(); return output; }
    private MatrixCopy<Real> ConstMtx(Real _const, bool pooled = false)
    {
        if (!useList) return new(Const(_const, pooled));
        if (!readList) { cstMtcs.Add(new(_const, Const(_const))); return new(cstMtcs[^1].matrix, true); }
        ConstMatrix<Real> cm = cstMtcs[countCst];
        bool equal = _const.Equals(cm._const); Matrix<Real> mtx = equal ? cm.matrix : Const(_const, pooled); countCst++;
        return new(mtx, equal);
    } // Cached constants must remain ordinary matrices
    private MatrixCopy<Real> Transform(ReadOnlySpan<char> input, bool pooled = false) => input[0] switch
    {
        _X => HandleSolo<Real>(input, new(x, true)),
        _Y => HandleSolo<Real>(input, new(y, true)),
        X_ => HandleSolo<Real>(input, new(X, true)),
        Y_ => HandleSolo<Real>(input, new(Y, true)),
        '{' => new(buffCocs[Int32.Parse(TryBraNum(input, '{', '}'))], true),
        '[' => braValues[Int32.Parse(TryBraNum(input, '[', ']'))],
        E => HandleSolo(input, ConstMtx(MathR.E, pooled)),
        P => HandleSolo(input, ConstMtx(MathR.PI, pooled)),
        G => HandleSolo(input, ConstMtx(GAMMA, pooled)),
        _ => ConstMtx(Real.Parse(input), pooled)
    };
    private MatrixCopy<Real> BreakPower(string input, bool pooled)
    {
        string[] chunks = PrepareBreakPower(input, THRESHOLD);
        Matrix<Real> tower = CopyMtx(PowerCore(chunks[^1], pooled), pooled);
        for (int k = chunks.Length - 2; k >= 0; k--)
        {
            string[] split = SplitByChars(chunks[k], "^"); // Special handling for "^"
            for (int m = split.Length - 1; m >= 0; m--)
            { MatrixCopy<Real> src = Transform(split[m], true); Power(src.matrix, tower); ReleaseMtx(src); }
        }
        return new(tower);
    }
    private MatrixCopy<Real> PowerCore(ReadOnlySpan<char> input, bool pooled = false)
    {
        if (!input.Contains('^')) return Transform(input, pooled);
        if (brkChk) if (CountChars(input, "^") > THRESHOLD) return BreakPower(input.ToString(), pooled);
        string[] split = SplitByChars(input, "^");
        Matrix<Real> tower = CopyMtx(Transform(split[^1], pooled), pooled);
        for (int k = split.Length - 2; k >= 0; k--)
        { MatrixCopy<Real> src = Transform(split[k], true); Power(src.matrix, tower); ReleaseMtx(src); }
        return new(tower);
    }
    private MatrixCopy<Real> BreakMultiplyDivide(string input, bool pooled)
    {
        var (chunks, signs) = PrepareBreakPSMD(input[0] == '/' ? input : String.Concat('*', input), "*/", THRESHOLD);
        Matrix<Real> product = CopyMtx(MultiplyDivideCore(TrimStartChar(chunks[0], '*'), pooled), pooled);
        for (int j = 1; j < chunks.Length; j++)
        {
            MatrixCopy<Real> src = MultiplyDivideCore(signs[j - 1] == SUB_CHARS[0] ? chunks[j] : String.Concat('/', chunks[j]), true);
            Multiply(src.matrix, product); ReleaseMtx(src);
        }
        return new(product);
    }
    private MatrixCopy<Real> MultiplyDivideCore(ReadOnlySpan<char> input, bool pooled = false)
    {
        if (!input.ContainsAny("*/")) return PowerCore(input, pooled);
        if (brkChk) if (CountChars(input, "*/") > THRESHOLD) return BreakMultiplyDivide(input.ToString(), pooled);
        var (split, signs) = GetPSMDComponents(input, "*/");
        Matrix<Real> product = CopyMtx(PowerCore(split[0], pooled), pooled); if (signs[0] == '/') Invert(product);
        for (int j = 1; j < split.Length; j++)
        {
            MatrixCopy<Real> src = PowerCore(split[j], true);
            Action<Matrix<Real>, Matrix<Real>> operation = signs[j] switch { '*' => Multiply, '/' => Divide };
            operation(src.matrix, product); ReleaseMtx(src);
        }
        return new(product);
    }
    private MatrixCopy<Real> BreakPlusSubtract(string input, bool pooled)
    {
        var (chunks, signs) = PrepareBreakPSMD(input[0] == '-' ? input : String.Concat('+', input), "+-", THRESHOLD);
        Matrix<Real> sum = CopyMtx(PlusSubtractCore(TrimStartChar(chunks[0], '+'), pooled), pooled);
        for (int i = 1; i < chunks.Length; i++)
        {
            MatrixCopy<Real> src = PlusSubtractCore(signs[i - 1] == SUB_CHARS[0] ? chunks[i] : String.Concat('-', chunks[i]), true);
            Plus(src.matrix, sum); ReleaseMtx(src);
        }
        return new(sum);
    }
    private MatrixCopy<Real> PlusSubtractCore(ReadOnlySpan<char> input, bool pooled = false)
    {
        if (!input.ContainsAny("+-")) return MultiplyDivideCore(input, pooled);
        if (brkChk) if (CountChars(input, "+-") > THRESHOLD) return BreakPlusSubtract(input.ToString(), pooled);
        var (split, signs) = GetPSMDComponents(input, "+-");
        Matrix<Real> sum = CopyMtx(MultiplyDivideCore(split[0], pooled), pooled); if (signs[0] == '-') Negate(sum);
        for (int i = 1; i < split.Length; i++)
        {
            MatrixCopy<Real> src = MultiplyDivideCore(split[i], true);
            Action<Matrix<Real>, Matrix<Real>> operation = signs[i] switch { '+' => Plus, '-' => Subtract };
            operation(src.matrix, sum); ReleaseMtx(src);
        }
        return new(sum);
    }
    private MatrixCopy<Real> ComputeBraFreePart(ReadOnlySpan<char> input, bool pooled = false)
        => Int32.TryParse(input, out int result) ? ConstMtx(result, pooled) : PlusSubtractCore(input, pooled);
    private MatrixCopy<Real> SubCore(string input, int start, MatrixCopy<Real> bFValue, ref int tagL, bool pooled = false)
    {
        if (start == 0) return bFValue;
        var (isInverse, mtx, copy) = (IsInverseFunc(input, start), bFValue.matrix, bFValue.copy);
        int handleSub(Func<Real, Real> func, int tagL)
        {
            ThrowException(input[start - tagL] != FUNC_HEAD);
            mtx = CopyMtx(bFValue, pooled); FuncSub(mtx, func); copy = false; return tagL;
        }
        tagL = input[start - 1] switch
        {
            _A => handleSub(MathR.Abs, 2),
            _L => handleSub(MathR.Log, 2),
            E_ => handleSub(MathR.Exp, 2),
            _Q => handleSub(MathR.Sqrt, 2),
            _S => isInverse.trig ? handleSub(MathR.Asin, 3) : handleSub(MathR.Sin, 2),
            _C => isInverse.trig ? handleSub(MathR.Acos, 3) : handleSub(MathR.Cos, 2),
            _T => isInverse.trig ? handleSub(MathR.Atan, 3) : handleSub(MathR.Tan, 2),
            _H => input[start - 2] switch
            {
                _S => isInverse.hyper ? handleSub(MathR.Asinh, 4) : handleSub(MathR.Sinh, 3),
                _C => isInverse.hyper ? handleSub(MathR.Acosh, 4) : handleSub(MathR.Cosh, 3),
                _T => isInverse.hyper ? handleSub(MathR.Atanh, 4) : handleSub(MathR.Tanh, 3)
            },
            _D_ => input[start - 2] switch
            {
                _F => handleSub(MathR.Floor, 3),
                _C => handleSub(MathR.Ceiling, 3),
                _R => handleSub(MathR.Round, 3),
                _S => handleSub(SafeSign, 3),
                _F_ => handleSub(Factorial, 3)
            }, // Real-specific
            _ => tagL
        };
        return new(mtx, copy);
    }
    private string SeriesSub(string input)
    {
        var (idx, end, split) = PrepareSeriesSub(input);
        (Func<string[], Matrix<Real>>, int) handleSub(Func<string[], Matrix<Real>> func, int tagL)
        { ThrowException(input[idx - tagL] != FUNC_HEAD); return (func, tagL); }
        var (braFunc, tagL) = input[idx - 1] switch
        {
            F_ => handleSub(Hypergeometric, 2),
            G_ => handleSub(Gamma, 2),
            B_ => handleSub(Beta, 2),
            _Z_ => handleSub(Zeta, 2),
            R_ => handleSub(Stereographic, 2),
            H_ => handleSub(Homothety, 2),
            S_ => handleSub(Sum, 2),
            P_ => handleSub(Product, 2),
            I_ => input[idx - 2] switch { TILDE => handleSub(Iterate, 2), MODE_2 => handleSub(Iterate2, 3) },
            J_ => input[idx - 2] switch { TILDE => handleSub(Compose, 2), MODE_2 => handleSub(Compose2, 3) },
            K_ => handleSub(Cocoon, 2),
            _D_ => input[idx - 2] switch
            {
                M_ => handleSub(Mod, 3),
                C_ => handleSub(Combination, 3),
                A_ => handleSub(Permutation, 3),
                MAX => handleSub(Max, 3),
                MIN => handleSub(Min, 3),
                D_ => handleSub(Distance, 3),
                I_ => handleSub(Iterate1, 4),
                J_ => handleSub(Compose1, 4)
            } // Real-specific
        };
        braValues[countBra] = new(braFunc(split)); // No need to copy
        return ReplaceInput(input, countBra++, idx - tagL, end);
    }
    private Matrix<Real> ObtainCore(string input)
    {
        while (input.Contains(SERIES_TAIL)) input = SeriesSub(input); // The number of substitutions is not known in advance
        var (length, start, end, tagL) = PrepareLoop(input);
        for (int i = 0; i < length; i++)
        {
            ResetStartEnd(input, ref start, ref end);
            braValues[countBra] = SubCore(input, start, ComputeBraFreePart(BraFreePart(input, start, end), true), ref tagL, true);
            input = ReplaceInput(input, countBra++, ref start, end, ref tagL);
        }
        return FinalizeMtx(ComputeBraFreePart(input));
    }
    public Matrix<Real> Obtain(bool checkVar = true)
        => checkVar && !input.AsSpan().ContainsAny(_XX__YY_BRA) ? Const(Obtain(input)) : ObtainCore(input);
    #endregion
} /// Computes real-variable expressions

/// <summary>
/// STRUCTURE SECTION
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public readonly struct Complex(Real real, Real imaginary = 0) // Manually inlined to reduce overhead
{
    public readonly Real real = real, imaginary = imaginary;
    public static readonly Real QUARTER = (Real)0.25, PI_HALF = MathR.PI / 2, PI_THIRD = MathR.PI / 3;
    public static readonly Complex ZERO = new(0), ONE = new(1), I = new(0, 1);

    #region Operators
    public static Complex operator +(Real r, Complex c) => new(r + c.real, c.imaginary);
    public static Complex operator +(Complex c1, Complex c2) => new(c1.real + c2.real, c1.imaginary + c2.imaginary);
    public static Complex operator -(Complex c) => new(-c.real, -c.imaginary);
    public static Complex operator -(Real r, Complex c) => new(r - c.real, -c.imaginary);
    public static Complex operator -(Complex c1, Complex c2) => new(c1.real - c2.real, c1.imaginary - c2.imaginary);
    public static Complex operator *(Complex c, Real r) => new(c.real * r, c.imaginary * r);
    public static Complex operator *(Complex c1, Complex c2)
    {
        Real re1 = c1.real, im1 = c1.imaginary, re2 = c2.real, im2 = c2.imaginary;
        return new(re1 * re2 - im1 * im2, re1 * im2 + im1 * re2);
    }
    public static Complex operator ~(Complex c)
    { Real re = c.real, im = c.imaginary, denom = re * re + im * im; return new(re / denom, -im / denom); } // Inverse
    public static Complex operator /(Complex c, Real r) => new(c.real / r, c.imaginary / r);
    public static Complex operator /(Complex c1, Complex c2)
    {
        Real re1 = c1.real, im1 = c1.imaginary, re2 = c2.real, im2 = c2.imaginary, denom = re2 * re2 + im2 * im2;
        return new((re1 * re2 + im1 * im2) / denom, (im1 * re2 - re1 * im2) / denom);
    }
    #endregion

    #region Elementary Functions
    public static (Real, Real) ReIm(Complex c) => (c.real, c.imaginary);
    public static Complex Conjugate(Complex c) => new(c.real, -c.imaginary);
    public static Real Modulus(Complex c) => Real.Hypot(c.real, c.imaginary);
    public static Complex Log(Complex c)
    {
        Real re = c.real, im = c.imaginary;
        return new(MathR.Log(re * re + im * im) / 2, MathR.Atan2(im, re));
    }
    public static Complex Exp(Complex c)
    {
        var (mod, unit) = (MathR.Exp(c.real), MathR.SinCos(c.imaginary));
        return new(mod * unit.Cos, mod * unit.Sin);
    }
    public static Complex Ei(Complex c)
    {
        var (mod, unit) = (MathR.Exp(-MathR.Tau * c.imaginary), MathR.SinCos(MathR.Tau * c.real));
        return new(mod * unit.Cos, mod * unit.Sin);
    } // Often represented by "q" in analytic number theory
    public static Complex Pow(Complex c1, Complex c2)
    {
        Real re1 = c1.real, im1 = c1.imaginary; if (re1 == 0 && im1 == 0) return ZERO; // Required a priori check
        Real re2 = c2.real, im2 = c2.imaginary, re3 = MathR.Log(re1 * re1 + im1 * im1) / 2, im3 = MathR.Atan2(im1, re1);
        var (mod, unit) = (MathR.Exp(re2 * re3 - im2 * im3), MathR.SinCos(re2 * im3 + im2 * re3));
        return new(mod * unit.Cos, mod * unit.Sin);
    }
    public static Complex Sqrt(Complex c)
    {
        Real re = c.real, im = c.imaginary;
        var (mod, unit) = (MathR.Pow(re * re + im * im, QUARTER), MathR.SinCos(MathR.Atan2(im, re) / 2));
        return new(mod * unit.Cos, mod * unit.Sin);
    }
    public static Complex Sin(Complex c)
    {
        var (mod, unit) = (MathR.Exp(-c.imaginary) / 2, MathR.SinCos(c.real));
        Real _mod = QUARTER / mod; return new((_mod + mod) * unit.Sin, (_mod - mod) * unit.Cos);
    }
    public static Complex Cos(Complex c)
    {
        var (mod, unit) = (MathR.Exp(-c.imaginary) / 2, MathR.SinCos(c.real));
        Real _mod = QUARTER / mod; return new((mod + _mod) * unit.Cos, (mod - _mod) * unit.Sin);
    }
    public static Complex Tan(Complex c)
    {
        var (mod, unit) = (MathR.Exp(-c.imaginary - c.imaginary) / 2, MathR.SinCos(c.real + c.real));
        Real _mod = QUARTER / mod, denom = (_mod + mod) + unit.Cos; return new(unit.Sin / denom, (_mod - mod) / denom);
    }
    public static Complex Asin(Complex c)
    {
        Real re = c.real, im = c.imaginary, re_ = 1 - re * re + im * im, im_ = -2 * re * im;
        var (mod, unit) = (MathR.Pow(re_ * re_ + im_ * im_, QUARTER), MathR.SinCos(MathR.Atan2(im_, re_) / 2));
        Real _re = -im + mod * unit.Cos, _im = re + mod * unit.Sin;
        return new(MathR.Atan2(_im, _re), -MathR.Log(_re * _re + _im * _im) / 2);
    }
    public static Complex Acos(Complex c) // Wolfram convention: https://mathworld.wolfram.com/InverseCosine.html
    {
        Real re = c.real, im = c.imaginary, re_ = 1 - re * re + im * im, im_ = -2 * re * im;
        var (mod, unit) = (MathR.Pow(re_ * re_ + im_ * im_, QUARTER), MathR.SinCos(MathR.Atan2(im_, re_) / 2));
        Real _re = -im + mod * unit.Cos, _im = re + mod * unit.Sin;
        return new(PI_HALF - MathR.Atan2(_im, _re), MathR.Log(_re * _re + _im * _im) / 2);
    }
    public static Complex Atan(Complex c)
    {
        Real re = c.real, im = c.imaginary, modSquare = re * re + im * im, denom = (1 + modSquare) + 2 * im,
            _re = (1 - modSquare) / denom, _im = 2 * re / denom;
        return new(MathR.Atan2(_im, _re) / 2, -MathR.Log(_re * _re + _im * _im) / 4);
    }
    public static Complex Sinh(Complex c)
    {
        var (mod, unit) = (MathR.Exp(c.real) / 2, MathR.SinCos(c.imaginary));
        Real _mod = QUARTER / mod; return new((mod - _mod) * unit.Cos, (mod + _mod) * unit.Sin);
    }
    public static Complex Cosh(Complex c)
    {
        var (mod, unit) = (MathR.Exp(c.real) / 2, MathR.SinCos(c.imaginary));
        Real _mod = QUARTER / mod; return new((mod + _mod) * unit.Cos, (mod - _mod) * unit.Sin);
    }
    public static Complex Tanh(Complex c)
    {
        var (mod, unit) = (MathR.Exp(c.real + c.real) / 2, MathR.SinCos(c.imaginary + c.imaginary));
        Real _mod = QUARTER / mod, denom = (mod + _mod) + unit.Cos; return new((mod - _mod) / denom, unit.Sin / denom);
    }
    public static Complex Asinh(Complex c)
    {
        Real re = c.real, im = c.imaginary, re_ = 1 + re * re - im * im, im_ = 2 * re * im;
        var (mod, unit) = (MathR.Pow(re_ * re_ + im_ * im_, QUARTER), MathR.SinCos(MathR.Atan2(im_, re_) / 2));
        Real _re = re + mod * unit.Cos, _im = im + mod * unit.Sin;
        return new(MathR.Log(_re * _re + _im * _im) / 2, MathR.Atan2(_im, _re));
    }
    public static Complex Acosh(Complex c) // Wolfram convention: https://mathworld.wolfram.com/InverseHyperbolicCosine.html
    {
        Real re = c.real, im = c.imaginary, re1 = 1 + re, re2 = -1 + re, imSquare = im * im;
        var (mod, unit) = (MathR.Pow((re1 * re1 + imSquare) * (re2 * re2 + imSquare), QUARTER),
            MathR.SinCos((MathR.Atan2(im, re1) + MathR.Atan2(im, re2)) / 2));
        Real _re = re + mod * unit.Cos, _im = im + mod * unit.Sin;
        return new(MathR.Log(_re * _re + _im * _im) / 2, MathR.Atan2(_im, _re));
    }
    public static Complex Atanh(Complex c)
    {
        Real re = c.real, im = c.imaginary, modSquare = re * re + im * im, denom = (1 + modSquare) - 2 * re,
            _re = (1 - modSquare) / denom, _im = 2 * im / denom;
        return new(MathR.Log(_re * _re + _im * _im) / 4, MathR.Atan2(_im, _re) / 2);
    }
    public static Complex Stereographic(Complex pt, Real r, Complex ctr)
    { var (x, y) = ReIm(pt); return pt * (r / (1 + MathR.Sqrt(1 - x * x - y * y))) + ctr; }
    public static Complex Homothety(Complex pt, Real r, Complex ctr) => (pt - ctr) / r + ctr;
    #endregion
} /// Represents optimized complex numbers with Real components
internal sealed class MatrixPoolLease<TEntry>(int length)
{
    public readonly TEntry[] array = ArrayPool<TEntry>.Shared.Rent(length);
    private int returned; // To make the lease double-return safe
    public void Return()
    {
        if (Interlocked.Exchange(ref returned, 1) != 0) return;
        ArrayPool<TEntry>.Shared.Return(array, RuntimeHelpers.IsReferenceOrContainsReferences<TEntry>());
    }
}
public readonly struct Matrix<TEntry>
{
    private readonly TEntry[] matrix;
    private readonly int[] rowOffs; // For row extraction
    private readonly MatrixPoolLease<TEntry>? lease;
    public Matrix(int[] rowOffs, int col)
    { this.rowOffs = rowOffs; matrix = GC.AllocateUninitializedArray<TEntry>(rowOffs[^1] + col); lease = null; }
    private Matrix(int[] rowOffs, MatrixPoolLease<TEntry> lease) { this.rowOffs = rowOffs; matrix = lease.array; this.lease = lease; }
    public static Matrix<TEntry> Rent(int[] rowOffs, int col)
        => rowOffs.Length == 1 ? new(rowOffs, col) : new(rowOffs, new MatrixPoolLease<TEntry>(rowOffs[^1] + col));
    public Matrix(TEntry x) { matrix = [x]; rowOffs = [0]; lease = null; } // Real-specific
    public bool IsPooled() => lease != null;
    public void Return() => lease?.Return();
    public TEntry this[int row, int column] { get => matrix[rowOffs[row] + column]; set => matrix[rowOffs[row] + column] = value; }
    public readonly unsafe TEntry* RowPtr(int row = 0) { fixed (TEntry* ptr = &matrix[rowOffs[row]]) { return ptr; } }
} /// Represents optimized matrices with real or complex entries
public readonly struct MatrixCopy<TEntry>(Matrix<TEntry> matrix, bool copy = false)
{
    public readonly Matrix<TEntry> matrix = matrix;
    public readonly bool copy = copy;
} /// Controls whether matrices are copied
public readonly struct ConstMatrix<TEntry>(TEntry _const, Matrix<TEntry> matrix)
{
    public readonly TEntry _const = _const;
    public readonly Matrix<TEntry> matrix = matrix;
} /// Represents reusable constant matrices
