// Date: 20241021, Designer: Fraljimetry

using System.Runtime.InteropServices;
using System.Drawing.Imaging;
using System.Media;
using System.Reflection;
using WMPLib;
using System.Text;
using System.Text.RegularExpressions;

namespace FunctionGrapher2._0
{
    public partial class Graph : Form
    {
        private static SoundPlayer _clickSoundPlayer;
        private static WindowsMediaPlayer _player;
        private static Bitmap bitmap;
        private static Rectangle rect;
        private static DateTime TimeNow = new();
        private static TimeSpan TimeCount = new();
        private static System.Windows.Forms.Timer GraphTimer, ColorTimer, WaitTimer, DisplayTimer;
        private static float ScalingFactor;
        private static int elapsedSeconds, x_left, x_right, y_up, y_down;
        private static readonly int X_LEFT_MAC = 620, X_RIGHT_MAC = 1520, Y_UP_MAC = 45, Y_DOWN_MAC = 945;
        private static readonly int X_LEFT_MIC = 1565, X_RIGHT_MIC = 1765, Y_UP_MIC = 745, Y_DOWN_MIC = 945;
        private static readonly int X_LEFT_CHECK = 1921, X_RIGHT_CHECK = 1922, Y_UP_CHECK = 1081, Y_DOWN_CHECK = 1082;
        private static readonly int REF_POS_1 = 9, REF_POS_2 = 27;
        private static int plot_loop, points_chosen, color_mode, contour_mode, point_number, times, export_number;
        private static double timeElapsed, _currentPosition;
        private static readonly double EPSILON = 0.03, STEPS = 0.25, DEVIATION = Math.PI / 12, EPS_DIFF_REAL = 0.5, EPS_DIFF_COMPLEX = 0.5, STEP_DIFF = 1, SIZE_DIFF = 1, PARAM_WIDTH = 5, INCREMENT_DEFAULT = 0.01, SHADE_DENSITY = 2;
        private static double epsilon, steps, deviation, raw_thickness, size_for_extremities, decay_rate;
        private static double[] scopes;
        private static int[] borders;
        private static bool waiting, commence_waiting, _isPlaying = true, _isPaused, delete_coordinate, delete_point = true, swap_colors, complex_mode = true, auto_export, retain_graph, clicked, shade_rainbow, axes_drawn, Axes_drawn, is_main, drafted, main_drawn, text_changed, activate_mousemove, is_checking, address_error, is_resized, ctrlPressed, sftPressed, suppressKeyUp;
        private static readonly Color CORRECT_GREEN = Color.FromArgb(192, 255, 192), ERROR_RED = Color.FromArgb(255, 192, 192), UNCHECKED_YELLOW = Color.FromArgb(255, 255, 128), READONLY_PURPLE = Color.FromArgb(255, 192, 255), COMBO_BLUE = Color.FromArgb(192, 255, 255), FOCUS_GRAY = Color.LightGray, BACKDROP_GRAY = Color.FromArgb(64, 64, 64), CONTROL_GRAY = Color.FromArgb(105, 105, 105), GRID_GRAY = Color.FromArgb(75, 255, 255, 255), UPPER_GOLD = Color.Gold, LOWER_BLUE = Color.RoyalBlue, ZERO_BLUE = Color.Lime, POLE_PURPLE = Color.Magenta, READONLY_GRAY = Color.Gainsboro;
        private static readonly string ADDRESS_DEFAULT = @"C:\Users\Public", INPUT_DEFAULT = "z", GENERAL_DEFAULT = "e", THICKNESS_DEFAULT = "1", DENSENESS_DEFAULT = "1", DRAFT_DEFAULT = "Detailed historical info is documented here.\r\n\r\n", CAPTION_DEFAULT = "Yours inputs will be shown here.", BARREDCHARS = "_#!<>$%&@~:\'\"\\?=`[]{}\t";
        private static string[] ExampleString;
        private static Complex[,] output_table;
        private static double[,] Output_table;
        #region Initializations
        [DllImport("dwmapi.dll")]
        static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int attrValue, int attrSize);
        const int DWMWA_USE_IMMERSIVE_DARK_MODE = 20;
        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool BlockInput([MarshalAs(UnmanagedType.Bool)] bool fBlockIt);
        [DllImport("user32.dll")]
        static extern bool HideCaret(IntPtr hWnd);
        protected override void WndProc(ref Message m)
        {
            const int WM_NCLBUTTONDOWN = 0x00A1;
            const int HTCAPTION = 0x0002;
            if (m.Msg == WM_NCLBUTTONDOWN && m.WParam.ToInt32() == HTCAPTION) return;
            base.WndProc(ref m);
        } // Prevents dragging the titlebar
        public Graph()
        {
            InitializeComponent();
            SetTitleBarColor();
            InitializeMusicPlayer();
            LoadClickSound();
            AttachClickEvents(this);
            InitializeTimers();
            InitializeBitmap();
            BanMouseWheel();
        }
        private void Graph_Load(object sender, EventArgs e)
        {
            InitializeCombo();
            InitializeData();
            SetTDSB();
            ReduceFontSizeByScale(this);
            TextBoxFocus(sender, e);
        }
        private void Graph_Paint(object sender, PaintEventArgs e)
        {
            if (clicked) return;
            SetBackdrop(e);
            DefaultReference(true);
        }
        private int SetTitleBarColor()
        {
            // Set window attribute for title bar color
            int attribute = DWMWA_USE_IMMERSIVE_DARK_MODE;
            int value = 1;  // Set to 1 to apply immersive color mode
            return DwmSetWindowAttribute(Handle, attribute, ref value, sizeof(int));
        }
        private void InitializeMusicPlayer()
        {
            _player = new WindowsMediaPlayer();
            Assembly assembly = Assembly.GetExecutingAssembly();
            Stream soundStream = assembly.GetManifestResourceStream("FunctionGrapher2._0.calm-zimpzon-main-version-07-55-10844.wav");
            if (soundStream != null)
            {
                // Save the stream to a temporary file, since Windows Media Player cannot play directly from the stream
                string tempFile = Path.Combine(Path.GetTempPath(), "background_music.wav");
                using (FileStream fileStream = new(tempFile, FileMode.Create, FileAccess.Write)) // This is extremely sensitive
                    soundStream.CopyTo(fileStream);
                // Set the media file
                _player.URL = tempFile;
                _player.settings.setMode("loop", true); // Loop the music
            }
            else MessageBox.Show("Error: Could not find embedded resource for music.");
        }
        private void PlayOrPause()
        {
            if (_isPlaying && !_isPaused) // When resumed
            {
                _currentPosition = _player.controls.currentPosition;
                _player.controls.pause();
                _isPaused = true;
                ColorTimer.Stop();
                TitleLabel.ForeColor = Color.White;
            }
            else if (_isPaused) // When paused
            {
                _player.controls.currentPosition = _currentPosition;
                _player.controls.play();
                _isPaused = false;
                timeElapsed = 0;
                ColorTimer.Start();
            }
            else // When initialized
            {
                _player.controls.play();
                _isPlaying = true;
                timeElapsed = 0;
                ColorTimer.Start();
            }
        }
        private static void LoadClickSound()
        {
            Assembly assembly = Assembly.GetExecutingAssembly();
            Stream soundStream = assembly.GetManifestResourceStream("FunctionGrapher2._0.bubble-sound-43207_[cut_0sec].wav");
            if (soundStream != null) _clickSoundPlayer = new SoundPlayer(soundStream);
            else MessageBox.Show("Error: Could not find embedded resource for click sound.");
        }
        private void AttachClickEvents(Control control)
        {
            control.Click += Control_Click;
            foreach (Control childControl in control.Controls) AttachClickEvents(childControl);
        }
        private void Control_Click(object sender, EventArgs e) => _clickSoundPlayer?.Play();
        private void InitializeTimers()
        {
            GraphTimer = new System.Windows.Forms.Timer { Interval = 1000 };
            ColorTimer = new System.Windows.Forms.Timer { Interval = 50 };
            ColorTimer.Tick += ColorTimer_Tick;
            ColorTimer.Start(); // This is necessary
            WaitTimer = new System.Windows.Forms.Timer { Interval = 500 };
            WaitTimer.Tick += WaitTimer_Tick;
            DisplayTimer = new System.Windows.Forms.Timer { Interval = 500 };
            DisplayTimer.Tick += DisplayTimer_Tick;
            timeElapsed = 0;
        }
        private void DisplayTimer_Tick(object sender, EventArgs e)
        {
            elapsedSeconds++;
            if (elapsedSeconds % 2 == 0) TimeDisplay.Text = (elapsedSeconds / 2).ToString() + "s";
            PointNumDisplay.Text = Convert.ToString(point_number + times);
        }
        private void WaitTimer_Tick(object sender, EventArgs e)
        {
            waiting = !waiting;
            if (!commence_waiting) return;
            PictureWait.Visible = waiting;
        }
        private void ColorTimer_Tick(object sender, EventArgs e)
        {
            timeElapsed += 0.01;
            TitleLabel.ForeColor = ObtainWheelCurve(timeElapsed % 1);
        }
        private void InitializeBitmap()
        {
            bitmap = new(Width, Height, PixelFormat.Format32bppArgb);
            rect = new(0, 0, Width, Height);
            DoubleBuffered = true;
            KeyPreview = true; // This is essential for shortcuts
        }
        private void BanMouseWheel()
        {
            ComboExamples.MouseWheel += ComboBox_MouseWheel;
            ComboFunctions.MouseWheel += ComboBox_MouseWheel;
            ComboSpecial.MouseWheel += ComboBox_MouseWheel;
            ComboColoring.MouseWheel += ComboBox_MouseWheel;
            ComboContour.MouseWheel += ComboBox_MouseWheel;
        }
        private void ComboBox_MouseWheel(object sender, MouseEventArgs e) => ((HandledMouseEventArgs)e).Handled = true;
        private void InitializeCombo()
        {
            Construct_Examples();
            ComboColoring_AddItem();
            ComboContour_AddItem();
            ComboExamples_AddItem();
            ComboFunctions_AddItem();
            ComboSpecial_AddItem();
        }
        private static void Construct_Examples()
        {
            ExampleString = new string[]
            {
                "F(1-10i,0.5i,i,z^5,100)",
                "z^(1+10i)cos((z-1)/(z^13+z+1))",
                "sum(z^n/(1-z^n),n,1,100)",
                "prod(e^((z+e(k/5))/(z-e(k/5))),k,1,5)",
                "iterate((Z+1/Z)e(0.02),z,k,1,100)",
                "iterate(exp(z^Z),z,k,1,100)",
                "iterateLoop(Z^2+z,0,k,1,30)",
                "comp(z^2,sin(zZ),cos(z/Z))",
                "cos(xy)-cos(x)-cos(y)",
                "min(sin(xy),tan(x),tan(y))",
                "xround(y)-yround(x)",
                "y-x|IterateLoop(x^X,x,k,1,30,y-X)",
                "iterate1((k*x)/X+X/(y+k),sin(x+y),k,1,3)",
                "iterate2(k/X+k/Y,XY,sin(x+y),cos(x-y),k,1,10,2)",
                "comp1(xy,tan(X+x),Arth(X-y))",
                "comp2(xy,x^2+y^2,sin(X+Y),cos(X-Y),2)",
                "func(ga(x,100),0.0001)",
                "func(sum(sin(2^kx)/2^k,k,0,100),-pi,pi,0.001)",
                "func(beta(sinh(x),cosh(x),100),-2,2,0.00001)",
                "polar(sqrt(cos(2theta)),theta,0,2pi,0.0001)",
                "polar(cos(5k)cos(7k),k,0,2pi,0.001)",
                "loop(polar(0.1jcos(5k+0.7jpi),k,0,pi),j,1,10)",
                "param(cos(17k),cos(19k),k,0,pi,0.0001)",
                "loop(param(cos(m)^k,sin(m)^k,m,0,p/2),k,1,10)"
            };
        }
        private void ComboColoring_AddItem()
        {
            string[] coloringOptions = { "Commonplace", "Monochromatic", "Bichromatic", "Kaleidoscopic", "Miscellaneous" };
            ComboColoring.Items.AddRange(coloringOptions);
            ComboColoring.SelectedIndex = 4;
        }
        private void ComboContour_AddItem()
        {
            string[] contourOptions = { "Cartesian (x,y)", "Polar (r,θ)" };
            ComboContour.Items.AddRange(contourOptions);
            ComboContour.SelectedIndex = 1;
        }
        private void ComboExamples_AddItem()
        {
            for (int i = 0; i < 8; i++) ComboExamples.Items.Add(ExampleString[i]);
            ComboExamples.Items.Add(String.Empty);
            for (int i = 8; i < 16; i++) ComboExamples.Items.Add(ExampleString[i]);
            ComboExamples.Items.Add(String.Empty);
            for (int i = 16; i < 24; i++) ComboExamples.Items.Add(ExampleString[i]);
        }
        private void ComboFunctions_AddItem()
        {
            string[] functionOptions = { "floor()", "ceil()", "round()", "sgn()", "F()", "gamma()", "beta()", "zeta()", "mod()", "nCr()", "nPr()", "max()", "min()", "log()", "exp()", "sqrt()", "abs()", "factorial()", "arsinh()", "arcosh()", "artanh()", "arcsin()", "arccos()", "arctan()", "sinh()", "cosh()", "tanh()", "sin()", "cos()", "tan()", "conjugate()", "e()" };
            ComboFunctions.Items.AddRange(functionOptions);
        }
        private void ComboSpecial_AddItem()
        {
            string[] specialOptions = { "product()", "sum()", "iterate1()", "iterate2()", "composite1()", "composite2()", "iterateLoop()", "loop()", "iterate()", "composite()", "func()", "polar()", "param()" };
            ComboSpecial.Items.AddRange(specialOptions);
        }
        private void InitializeData()
        {
            InputString.Text = INPUT_DEFAULT;
            InputString.SelectionStart = InputString.Text.Length;
            DraftBox.Text = DRAFT_DEFAULT;
            CaptionBox.Text = CAPTION_DEFAULT;
            GeneralInput.Text = GENERAL_DEFAULT;
            ThickInput.Text = THICKNESS_DEFAULT;
            DenseInput.Text = DENSENESS_DEFAULT;
            AddressInput.Text = ADDRESS_DEFAULT;
            PictureIncorrect.Visible = false;
            PictureCorrect.Visible = true;
        }
        private void SetThicknessDenseness()
        {
            epsilon = EPSILON; steps = STEPS; deviation = DEVIATION;
            if (ThickInput.Text == String.Empty) ThickInput.Text = THICKNESS_DEFAULT;
            if (DenseInput.Text == String.Empty) DenseInput.Text = DENSENESS_DEFAULT;
            raw_thickness = RealSubstitution.ObtainValue(ThickInput.Text);
            double temp = RealSubstitution.ObtainValue(DenseInput.Text);
            epsilon = complex_mode ? EPSILON * EPS_DIFF_COMPLEX * raw_thickness : EPSILON * EPS_DIFF_REAL * raw_thickness;
            steps = STEPS / temp; deviation = DEVIATION / temp;
            decay_rate = 0.2 * raw_thickness;
            size_for_extremities = 0.5 * raw_thickness / (1 + raw_thickness);
        }
        private void SetScopesBorders()
        {
            if (GeneralInput.Text == String.Empty) GeneralInput.Text = GENERAL_DEFAULT;
            if (GeneralInput.Text != "0")
            {
                double temp_scope = RealSubstitution.ObtainValue(GeneralInput.Text);
                scopes = new double[] { -temp_scope, temp_scope, temp_scope, -temp_scope };
                X_Left.Text = Y_Left.Text = (-temp_scope).ToString("#0.0000");
                X_Right.Text = Y_Right.Text = temp_scope.ToString("#0.0000");
            }
            else
            {
                if (X_Left.Text == String.Empty) X_Left.Text = "0";
                if (X_Right.Text == String.Empty) X_Right.Text = "0";
                if (Y_Left.Text == String.Empty) Y_Left.Text = "0";
                if (Y_Right.Text == String.Empty) Y_Right.Text = "0";
                scopes[0] = RealSubstitution.ObtainValue(X_Left.Text);
                scopes[1] = RealSubstitution.ObtainValue(X_Right.Text);
                scopes[2] = RealSubstitution.ObtainValue(Y_Right.Text);
                scopes[3] = RealSubstitution.ObtainValue(Y_Left.Text);
            }
            borders = new int[] { x_left, x_right, y_up, y_down };
        }
        private void SetTDSB() { SetThicknessDenseness(); SetScopesBorders(); }
        private static void ReduceFontSizeByScale(Control parent)
        {
            Graphics g = Graphics.FromHwnd(IntPtr.Zero);
            ScalingFactor = g.DpiX / 96f / 1.5f;
            foreach (Control ctrl in parent.Controls)
            {
                ctrl.Font = new Font(ctrl.Font.FontFamily, ctrl.Font.Size / ScalingFactor, ctrl.Font.Style);
                if (ctrl.Controls.Count > 0) ReduceFontSizeByScale(ctrl);
            }
        }
        private void TextBoxFocus(object sender, EventArgs e)
        {
            foreach (Control control in Controls.OfType<TextBox>())
                control.GotFocus += (sender, e) => { ((TextBox)sender).SelectionStart = ((TextBox)sender).Text.Length; };
        }
        private static void SetBackdrop(PaintEventArgs e)
        {
            DrawBackdrop(e.Graphics, new(Color.Gray, 1), X_LEFT_MIC, Y_UP_MIC, X_RIGHT_MIC, Y_DOWN_MIC, new SolidBrush(Color.Black));
            DrawBackdrop(e.Graphics, new(Color.Gray, 1), X_LEFT_MAC, Y_UP_MAC, X_RIGHT_MAC, Y_DOWN_MAC, new SolidBrush(Color.Black));
        }
        private static void DrawBackdrop(Graphics g, Pen pen, int xLeft, int yUp, int xRight, int yDown, Brush backBrush)
        {
            g.DrawLine(pen, xLeft, yUp, xRight, yUp);
            g.DrawLine(pen, xRight, yUp, xRight, yDown);
            g.DrawLine(pen, xRight, yDown, xLeft, yDown);
            g.DrawLine(pen, xLeft, yDown, xLeft, yUp);
            g.FillRectangle(backBrush, xLeft + 1, yUp + 1, xRight - xLeft - 1, yDown - yUp - 1);
        }
        #endregion
        #region Pre-Graphing
        private static int[] Transform(double x, double y, double[] scopes, int[] borders)
        {
            double _x = (borders[1] - borders[0]) / (scopes[1] - scopes[0]);
            double _y = (borders[3] - borders[2]) / (scopes[3] - scopes[2]);
            return new int[] { (int)(borders[0] + (x - scopes[0]) * _x), (int)(borders[2] + (y - scopes[2]) * _y) };
        }
        private static double[] InverseTransform(int a, int b, double[] scopes, int[] borders)
        {
            double _x = (scopes[1] - scopes[0]) / (borders[1] - borders[0]);
            double _y = (scopes[3] - scopes[2]) / (borders[3] - borders[2]);
            return new double[] { scopes[0] + (a - borders[0]) * _x, scopes[2] + (b - borders[2]) * _y };
        }
        private static double LowerDistance(double a, double m) => Math.Abs(a - m * LowerIndex(a, m));
        private static int LowerIndex(double a, double m) => a >= 0 ? (int)(a / m) : (int)((a / m) + (int)(-a / m) + 1) - (int)(-a / m) - 1;
        private static double LowerRatio(double a, double m) => a == -0 ? 1 : (a - m * LowerIndex(a, m)) / m;
        private unsafe static double[] FiniteExtremities(double[,] output, int row, int column)
        {
            double min = Double.NaN, max = Double.NaN;
            fixed (double* outPtr = &output[0, 0])
            {
                for (int i = 0; i < row; i++) // Should not use parallel
                {
                    for (int j = 0; j < column; j++)
                    {
                        if (Double.IsNaN(outPtr[i * column + j])) continue;
                        double atanValue = Math.Atan(outPtr[i * column + j]);
                        if (Double.IsNaN(min)) min = max = atanValue; // This is sensitive
                        else
                        {
                            max = Math.Max(atanValue, max);
                            min = Math.Min(atanValue, min);
                        }
                    }
                }
            }
            return new double[] { min, max };
        }
        private void RealComputation(int row, int column)
        {
            switch (color_mode)
            {
                case 1: Real1(Output_table, row, column); break;
                case 2: Real2(Output_table, row, column); break;
                case 3: Real3(Output_table, row, column); break;
                case 4: Real4(Output_table, row, column); break;
                case 5: Real5(Output_table, row, column); break;
            }
        }
        private unsafe void RealSpecial(int i, int j, double[,] output, int _row, int _column, double[] MinMax, byte* ptr, int stride, Color zeroColor, Color poleColor)
        {
            if (delete_point) return;
            double value = output[_row, _column];
            if (Math.Atan(value) < (1 - size_for_extremities) * MinMax[0] + size_for_extremities * MinMax[1])
                SetPixelFast(i, j, ptr, stride, zeroColor);
            if (Math.Atan(value) > (1 - size_for_extremities) * MinMax[1] + size_for_extremities * MinMax[0])
                SetPixelFast(i, j, ptr, stride, poleColor);
        }
        private unsafe void ProcessReal(int i, int j, double[,] output, double[] MinMax, byte* ptr, int stride, Color zeroColor, Color poleColor, Func<double, Color> colorSelector)
        {
            int _row = i - (x_left + 1), _column = j - (y_up + 1);
            double value = output[_row, _column];
            if (Double.IsNaN(value)) return;
            Color pixelColor = colorSelector(value);
            if (pixelColor != Color.Empty) SetPixelFast(i, j, ptr, stride, pixelColor);
            RealSpecial(i, j, output, _row, _column, MinMax, ptr, stride, zeroColor, poleColor);
        }
        private unsafe void RealLoop(double[,] output, int row, int column, Color zeroColor, Color poleColor, Func<double, Color> colorSelector)
        {
            int xStart = x_left + 1, yStart = y_up + 1, xEnd = x_right, yEnd = y_down;
            BitmapData bmpData = bitmap.LockBits(rect, ImageLockMode.ReadWrite, bitmap.PixelFormat);
            byte* ptr = (byte*)bmpData.Scan0;
            try
            {
                double[] MinMax = FiniteExtremities(output, row, column);
                for (int i = xStart; i < xEnd; i++) for (int j = yStart; j < yEnd; j++)
                        ProcessReal(i, j, output, MinMax, ptr, bmpData.Stride, zeroColor, poleColor, colorSelector);
            }
            finally { bitmap.UnlockBits(bmpData); }
        }
        private void Real1(double[,] output, int row, int column)
        => RealLoop(output, row, column, ZERO_BLUE, POLE_PURPLE, value =>
        (Math.Abs(value) < epsilon) ? (swap_colors ? Color.Black : Color.White) : Color.Empty);
        private void Real2(double[,] output, int row, int column)
        {
            Color trueColor = swap_colors ? Color.Black : Color.White;
            Color falseColor = swap_colors ? Color.White : Color.Black;
            RealLoop(output, row, column, ZERO_BLUE, POLE_PURPLE, value =>
            value < 0 ? falseColor : (value > 0 ? trueColor : Color.Empty));
        }
        private void Real3(double[,] output, int row, int column)
        {
            Color trueColor = swap_colors ? LOWER_BLUE : UPPER_GOLD;
            Color falseColor = swap_colors ? UPPER_GOLD : LOWER_BLUE;
            RealLoop(output, row, column, Color.Black, Color.White, value =>
            value < 0 ? falseColor : (value > 0 ? trueColor : Color.Empty));
        }
        private unsafe void Real4(double[,] output, int row, int column)
        {
            int xStart = x_left + 1, yStart = y_up + 1, xEnd = x_right, yEnd = y_down;
            BitmapData bmpData = bitmap.LockBits(rect, ImageLockMode.ReadWrite, bitmap.PixelFormat);
            byte* ptr = (byte*)bmpData.Scan0;
            try
            {
                double[] MinMax = FiniteExtremities(output, row, column);
                for (int i = xStart; i < xEnd; i++)
                {
                    int _row = i - xStart;
                    for (int j = yStart; j < yEnd; j++)
                    {
                        int _column = j - yStart;
                        double value = output[_row, _column];
                        if (Double.IsNaN(value)) continue;
                        Color pixelColor = ObtainStrip(Math.Atan(value), MinMax[0], MinMax[1]);
                        SetPixelFast(i, j, ptr, bmpData.Stride, pixelColor);
                        RealSpecial(i, j, output, _row, _column, MinMax, ptr, bmpData.Stride, Color.Black, Color.White);
                    }
                }
            }
            finally { bitmap.UnlockBits(bmpData); }
        }
        private unsafe void Real5(double[,] output, int row, int column)
        {
            int xStart = x_left + 1, yStart = y_up + 1, xEnd = x_right, yEnd = y_down;
            BitmapData bmpData = bitmap.LockBits(rect, ImageLockMode.ReadWrite, bitmap.PixelFormat);
            byte* ptr = (byte*)bmpData.Scan0;
            try
            {
                double[] MinMax = FiniteExtremities(output, row, column);
                for (int i = xStart; i < xEnd; i++)
                {
                    int _row = i - xStart;
                    for (int j = yStart; j < yEnd; j++)
                    {
                        int _column = j - yStart;
                        double value = output[_row, _column];
                        if (Double.IsNaN(value)) continue;
                        double alpha = Math.Clamp(LowerRatio(value, raw_thickness), 0, 1);
                        Color pixelColor = ObtainStripAlpha(Math.Atan(value), (alpha - 1) / SHADE_DENSITY + 1, MinMax[0], MinMax[1]);
                        SetPixelFast(i, j, ptr, bmpData.Stride, pixelColor);
                        RealSpecial(i, j, output, _row, _column, MinMax, ptr, bmpData.Stride, Color.Black, Color.White);
                    }
                }
            }
            finally { bitmap.UnlockBits(bmpData); }
        }
        private void ComplexComputation()
        {
            switch (color_mode, contour_mode)
            {
                case (1, 1): Complex1_ReIm(output_table); break;
                case (2, 1): Complex2_ReIm(output_table); break;
                case (3, 1): Complex3_ReIm(output_table); break;
                case (1, _): Complex1_ModArg(output_table); break;
                case (2, _): Complex2_ModArg(output_table); break;
                case (3, _): Complex3_ModArg(output_table); break;
                case (4, _): Complex4(output_table); break;
                case (5, _): Complex5(output_table); break;
            }
        }
        private unsafe void ComplexSpecial(int i, int j, Complex[,] output, int _row, int _column, byte* ptr, int stride, Color zeroColor, Color poleColor)
        {
            if (delete_point) return;
            double modulus = Complex.Modulus(output[_row, _column]);
            if (modulus < epsilon * SIZE_DIFF) SetPixelFast(i, j, ptr, stride, zeroColor);
            else if (modulus > 1 / (epsilon * SIZE_DIFF)) SetPixelFast(i, j, ptr, stride, poleColor);
        }
        private unsafe void ProcessComplexData(int i, int j, Complex[,] output, byte* ptr, int stride, Color trueColor, Color falseColor, Color zeroColor, Color poleColor, bool mode1, Func<Complex, (double, double)> valueExtractor, double stepFactor1, double stepFactor2)
        {
            int _row = i - (x_left + 1), _column = j - (y_up + 1);
            Complex value = output[_row, _column];
            if (Double.IsNaN(value.real) || Double.IsNaN(value.imaginary)) return;
            (double value1, double value2) = valueExtractor(value);
            if (mode1)
            {
                if (LowerDistance(value1, stepFactor1) < epsilon || LowerDistance(value2, stepFactor2) < epsilon)
                    SetPixelFast(i, j, ptr, stride, swap_colors ? falseColor : trueColor);
            }
            else
                SetPixelFast(i, j, ptr, stride, (LowerIndex(value1, stepFactor1) + LowerIndex(value2, stepFactor2)) % 2 == 0 ? trueColor : falseColor);
            ComplexSpecial(i, j, output, _row, _column, ptr, stride, zeroColor, poleColor);
        }
        private unsafe void ProcessComplexLoop(Complex[,] output, Color trueColor, Color falseColor, Color zeroColor, Color poleColor, bool mode1, Func<Complex, (double, double)> valueExtractor, double stepFactor1, double stepFactor2)
        {
            int xStart = x_left + 1, yStart = y_up + 1, xEnd = x_right, yEnd = y_down;
            BitmapData bmpData = bitmap.LockBits(rect, ImageLockMode.ReadWrite, bitmap.PixelFormat);
            byte* ptr = (byte*)bmpData.Scan0;
            try
            {
                for (int i = xStart; i < xEnd; i++) for (int j = yStart; j < yEnd; j++)
                        ProcessComplexData(i, j, output, ptr, bmpData.Stride, trueColor, falseColor, zeroColor, poleColor, mode1, valueExtractor, stepFactor1, stepFactor2);
            }
            finally { bitmap.UnlockBits(bmpData); }
        }
        private unsafe void ComplexReImLoop(Complex[,] output, Color trueColor, Color falseColor, Color zeroColor, Color poleColor, bool mode1)
            => ProcessComplexLoop(output, trueColor, falseColor, zeroColor, poleColor, mode1, c => (c.real, c.imaginary), steps, steps);
        private unsafe void ComplexModArgLoop(Complex[,] output, Color trueColor, Color falseColor, Color zeroColor, Color poleColor, bool mode1)
            => ProcessComplexLoop(output, trueColor, falseColor, zeroColor, poleColor, mode1, c =>
            (Complex.Log(c).real, Math.Atan2(c.imaginary, c.real)), steps * STEP_DIFF, deviation);
        private void Complex1_ReIm(Complex[,] output)
            => ComplexReImLoop(output, Color.White, Color.Black, ZERO_BLUE, POLE_PURPLE, true);
        private void Complex2_ReIm(Complex[,] output)
        {
            Color trueColor = swap_colors ? Color.Black : Color.White;
            Color falseColor = swap_colors ? Color.White : Color.Black;
            ComplexReImLoop(output, trueColor, falseColor, ZERO_BLUE, POLE_PURPLE, false);
        }
        private void Complex3_ReIm(Complex[,] output)
        {
            Color trueColor = swap_colors ? LOWER_BLUE : UPPER_GOLD;
            Color falseColor = swap_colors ? UPPER_GOLD : LOWER_BLUE;
            ComplexReImLoop(output, trueColor, falseColor, Color.Black, Color.White, false);
        }
        private void Complex1_ModArg(Complex[,] output)
            => ComplexModArgLoop(output, Color.White, Color.Black, ZERO_BLUE, POLE_PURPLE, true);
        private void Complex2_ModArg(Complex[,] output)
        {
            Color trueColor = swap_colors ? Color.Black : Color.White;
            Color falseColor = swap_colors ? Color.White : Color.Black;
            ComplexModArgLoop(output, trueColor, falseColor, ZERO_BLUE, POLE_PURPLE, false);
        }
        private void Complex3_ModArg(Complex[,] output)
        {
            Color trueColor = swap_colors ? LOWER_BLUE : UPPER_GOLD;
            Color falseColor = swap_colors ? UPPER_GOLD : LOWER_BLUE;
            ComplexModArgLoop(output, trueColor, falseColor, Color.Black, Color.White, false);
        }
        private unsafe void Complex4(Complex[,] output)
        {
            int xStart = x_left + 1, yStart = y_up + 1, xEnd = x_right, yEnd = y_down;
            BitmapData bmpData = bitmap.LockBits(rect, ImageLockMode.ReadWrite, bitmap.PixelFormat);
            byte* ptr = (byte*)bmpData.Scan0;
            try
            {
                for (int i = xStart; i < xEnd; i++)
                {
                    int _row = i - xStart;
                    for (int j = yStart; j < yEnd; j++)
                    {
                        int _column = j - yStart;
                        Complex value = output[_row, _column];
                        if (Double.IsNaN(value.real) || Double.IsNaN(value.imaginary)) continue;
                        Color pixelColor = shade_rainbow ? ObtainWheelAlpha(value) : ObtainWheel(value);
                        SetPixelFast(i, j, ptr, bmpData.Stride, pixelColor);
                        ComplexSpecial(i, j, output, _row, _column, ptr, bmpData.Stride, Color.Black, Color.White);
                    }
                }
            }
            finally { bitmap.UnlockBits(bmpData); }
        }
        private unsafe void Complex5(Complex[,] output)
        {
            int xStart = x_left + 1, yStart = y_up + 1, xEnd = x_right, yEnd = y_down;
            BitmapData bmpData = bitmap.LockBits(rect, ImageLockMode.ReadWrite, bitmap.PixelFormat);
            byte* ptr = (byte*)bmpData.Scan0;
            try
            {
                for (int i = xStart; i < xEnd; i++)
                {
                    int _row = i - xStart;
                    for (int j = yStart; j < yEnd; j++)
                    {
                        int _column = j - yStart;
                        Complex value = output[_row, _column];
                        if (Double.IsNaN(value.real) || Double.IsNaN(value.imaginary)) continue;
                        double _modulus = Complex.Log(value).real;
                        double _argument = Math.Atan2(value.imaginary, value.real);
                        double alpha = (LowerRatio(_modulus, steps * STEP_DIFF) + LowerRatio(_argument, deviation)) / 2;
                        double normalAlpha = (alpha - 1) / SHADE_DENSITY + 1;
                        Color pixelColor = shade_rainbow ? ObtainWheelAlpha(value, normalAlpha) : ObtainWheel(value, normalAlpha);
                        SetPixelFast(i, j, ptr, bmpData.Stride, pixelColor);
                        ComplexSpecial(i, j, output, _row, _column, ptr, bmpData.Stride, Color.Black, Color.White);
                    }
                }
            }
            finally { bitmap.UnlockBits(bmpData); }
        }
        public unsafe void SetPixelFast(int x, int y, byte* ptr, int stride, Color color)
        {
            point_number++;
            int pixelIndex = y * stride + x * 4; // Assuming 32bpp (ARGB format)
            ptr[pixelIndex] = color.B;
            ptr[pixelIndex + 1] = color.G;
            ptr[pixelIndex + 2] = color.R;
            ptr[pixelIndex + 3] = color.A;
        }
        private unsafe static void ClearBitmap(Bitmap bitmap, Rectangle rect)
        {
            BitmapData bmpData = bitmap.LockBits(rect, ImageLockMode.WriteOnly, bitmap.PixelFormat);
            int bytesPerPixel = Image.GetPixelFormatSize(bitmap.PixelFormat) / 8;
            int height = rect.Height, widthInBytes = rect.Width * bytesPerPixel, stride = bmpData.Stride;
            byte* ptr = (byte*)bmpData.Scan0;
            Parallel.For(0, height, y => {
                byte* row = ptr + y * stride;
                for (int x = 0; x < widthInBytes; x += bytesPerPixel) row[x + 3] = 0;
            });
            bitmap.UnlockBits(bmpData);
        }
        private static void BeginRendering() { if (!retain_graph) ClearBitmap(bitmap, rect); }
        private void EndRendering()
        {
            if (is_checking) return;
            PointNumDisplay.Text = point_number.ToString();
            if (auto_export) RunExportButton_Click();
        }
        private unsafe static (double[,] xCoor, double[,] yCoor) SetCoor(int row, int column)
        {
            double[,] xCoor = new double[row, column], yCoor = new double[row, column];
            Parallel.For(0, row, i =>
            {
                fixed (double* xCoorPtr = &xCoor[0, 0], yCoorPtr = &yCoor[0, 0])
                {
                    for (int j = 0; j < column; j++)
                    {
                        double[] coor = InverseTransform(i + x_left + 1, j + y_up + 1, scopes, borders);
                        xCoorPtr[i * column + j] = coor[0]; yCoorPtr[i * column + j] = coor[1];
                    }
                }
            });
            return (xCoor, yCoor);
        }
        private static void Computation(string input, int row, int column)
        {
            (double[,] xCoor, double[,] yCoor) = SetCoor(row, column);
            if (!complex_mode) Output_table = new RealSubstitution(input, xCoor, yCoor, row, column).ObtainValues();
            else output_table = new ComplexSubstitution(input, xCoor, yCoor, row, column).ObtainValues();
        }
        private static void PrepareAxes(Graphics g)
        {
            if (is_checking) return;
            DrawBorders(g);
            if (!retain_graph)
            {
                g.FillRectangle(new SolidBrush(Color.Black), x_left + 1, y_up + 1, x_right - x_left - 1, y_down - y_up - 1);
                SetAxesDrawn(false);
            }
            if (!delete_coordinate && !(is_main ? Axes_drawn : axes_drawn))
            {
                DrawAxesGrid(g, scopes, borders);
                SetAxesDrawn(true);
            }
        }
        private static void DrawBorders(Graphics g)
        {
            Pen BlackPen = new(Color.White, 1);
            g.DrawLine(BlackPen, x_left, y_up, x_right, y_up);
            g.DrawLine(BlackPen, x_right, y_up, x_right, y_down);
            g.DrawLine(BlackPen, x_right, y_down, x_left, y_down);
            g.DrawLine(BlackPen, x_left, y_down, x_left, y_up);
        }
        private static void DrawAxesGrid(Graphics g, double[] scopes, int[] borders)
        {
            double xGrid = Math.Pow(5, Math.Floor(Math.Log((scopes[1] - scopes[0]) / 2) / Math.Log(5)));
            double yGrid = Math.Pow(5, Math.Floor(Math.Log((scopes[2] - scopes[3]) / 2) / Math.Log(5)));
            DrawGrid(g, scopes, borders, xGrid, yGrid, 3);
            DrawGrid(g, scopes, borders, xGrid / 5, yGrid / 5, 2);
            DrawAxes(g, scopes, borders);
        }
        private static void DrawGrid(Graphics g, double[] scopes, int[] borders, double xGrid, double yGrid, float penWidth)
        {
            Pen gridPen = new(GRID_GRAY, penWidth);
            for (int i = (int)Math.Floor(scopes[3] / yGrid); i <= (int)Math.Ceiling(scopes[2] / yGrid); i++)
            {
                int gridPosition = Transform(0, i * yGrid, scopes, borders)[1];
                if (gridPosition > borders[2] && gridPosition < borders[3])
                    g.DrawLine(gridPen, borders[0] + 1, gridPosition, borders[1], gridPosition);
            }
            for (int i = (int)Math.Floor(scopes[0] / xGrid); i <= (int)Math.Ceiling(scopes[1] / xGrid); i++)
            {
                int gridPosition = Transform(i * xGrid, 0, scopes, borders)[0];
                if (gridPosition > borders[0] && gridPosition < borders[1])
                    g.DrawLine(gridPen, gridPosition, borders[3], gridPosition, borders[2] + 1);
            }
        }
        private static void DrawAxes(Graphics g, double[] scopes, int[] borders)
        {
            Pen axisPen = new(Color.DarkGray, 4);
            int[] axisPositions = Transform(0, 0, scopes, borders);
            if (axisPositions[1] > borders[2] && axisPositions[1] < borders[3])
                g.DrawLine(axisPen, borders[0] + 1, axisPositions[1], borders[1], axisPositions[1]);
            if (axisPositions[0] > borders[0] && axisPositions[0] < borders[1])
                g.DrawLine(axisPen, axisPositions[0], borders[3], axisPositions[0], borders[2] + 1);
        }
        private static void SetAxesDrawn(bool drawn)
        {
            if (is_main) Axes_drawn = drawn;
            else axes_drawn = drawn;
        }
        #endregion
        #region Graphing
        private void DisplayBase(Action renderModes)
        {
            if (is_checking) return;
            Graphics g = CreateGraphics();
            BeginRendering();
            renderModes();  // Dynamically render complex or real modes
            PrepareAxes(g);
            g.DrawImage(bitmap, 0, 0);
            EndRendering();
        }
        private Complex[,] DisplayMini(string input, Complex[,] z, Complex[,] Z)
        {
            int row = x_right - x_left, column = y_down - y_up;
            output_table = new ComplexSubstitution(input, z, Z, row, column).ObtainValues();
            DisplayBase(ComplexComputation);
            return output_table;
        }
        private double[,] DisplayMini(string input, double[,] x, double[,] y, double[,] X)
        {
            int row = x_right - x_left, column = y_down - y_up;
            Output_table = new RealSubstitution(input, x, y, X, new double[row, column], row, column).ObtainValues();
            DisplayBase(() => RealComputation(row, column));
            return Output_table;
        }
        private void DisplayPro(string input)
        {
            int row = x_right - x_left, column = y_down - y_up;
            Computation(input, row, column);
            DisplayBase(() =>
            {
                if (!complex_mode) RealComputation(row, column);
                else ComplexComputation();
            });
        }
        private void DisplayBase(string input, bool isPolar = false, bool isParam = false)
        {
            times = 0;
            string[] split = MyString.SplitString(input);
            Graphics g = CreateGraphics();
            PrepareAxes(g);
            int penWidth = (int)(PARAM_WIDTH * RealSubstitution.ObtainValue(ThickInput.Text));
            Pen curve_pen = new(swap_colors ? Color.Black : Color.White, penWidth);
            Pen colorful_pen = new(Color.Black, penWidth);
            Pen black_pen = new(swap_colors ? Color.White : Color.Black, penWidth);
            Pen white_pen = new(swap_colors ? Color.Black : Color.White, penWidth);
            Pen blue_pen = new(swap_colors ? LOWER_BLUE : UPPER_GOLD, penWidth);
            Pen yellow_pen = new(swap_colors ? UPPER_GOLD : LOWER_BLUE, penWidth);
            double relative_speed = RealSubstitution.ObtainValue(DenseInput.Text);
            double start = 0, ending = 0, increment = INCREMENT_DEFAULT;
            if (isParam)
            {
                start = RealSubstitution.ObtainValue(split[3]);
                ending = RealSubstitution.ObtainValue(split[4]);
                if (split.Length > 5) increment = RealSubstitution.ObtainValue(split[5]);
            }
            else if (isPolar)
            {
                start = RealSubstitution.ObtainValue(split[2]);
                ending = RealSubstitution.ObtainValue(split[3]);
                if (split.Length > 4) increment = RealSubstitution.ObtainValue(split[4]);
            }
            else
            {
                double range = RealSubstitution.ObtainValue(GeneralInput.Text);
                double range_1 = (GeneralInput.Text == "0") ? RealSubstitution.ObtainValue(X_Left.Text) : -range;
                double range_2 = (GeneralInput.Text == "0") ? RealSubstitution.ObtainValue(X_Right.Text) : range;
                switch (split.Length)
                {
                    case 1:
                        start = range_1;
                        ending = range_2;
                        break;
                    case 2:
                        increment = RealSubstitution.ObtainValue(split[1]);
                        start = range_1;
                        ending = range_2;
                        break;
                    case 3:
                        start = RealSubstitution.ObtainValue(split[1]);
                        ending = RealSubstitution.ObtainValue(split[2]);
                        break;
                    case 4:
                        start = RealSubstitution.ObtainValue(split[1]);
                        ending = RealSubstitution.ObtainValue(split[2]);
                        increment = RealSubstitution.ObtainValue(split[3]);
                        break;
                }
            }
            int Length = (int)Math.Abs((start - ending) / increment) + 2;
            double[,] coor = new double[2, Length]; // Efficient memory access
            bool[] in_range = new bool[Length];
            int[,] pos = new int[2, Length]; // Efficient memory access
            double xCoor_left = InverseTransform(x_left, 0, scopes, borders)[0];
            double xCoor_right = InverseTransform(x_right, 0, scopes, borders)[0];
            double yCoor_up = InverseTransform(0, y_up, scopes, borders)[1];
            double yCoor_down = InverseTransform(0, y_down, scopes, borders)[1];
            string input_1 = "x";
            string input_2 = split[0];
            if (isPolar || isParam)
            {
                input_1 = isPolar ? $"({split[0]})*~c({split[1]})".Replace(split[1], "x") : split[0].Replace(split[2], "x");
                input_2 = isPolar ? $"({split[0]})*~s({split[1]})".Replace(split[1], "x") : split[1].Replace(split[2], "x");
            }
            double[,] Steps = new double[1, Length]; // Efficient memory access
            double temp = start;
            if (is_checking)
            {
                coor[0, 0] = RealSubstitution.ObtainValue(input_1, temp);
                coor[1, 0] = RealSubstitution.ObtainValue(input_2, temp);
                return;
            }
            else
            {
                for (int i = 0; i < Length; i++)
                {
                    Steps[0, i] = temp;
                    temp += increment;
                }
                double[,] value_1 = new RealSubstitution(input_1, Steps, 1, Length).ObtainValues();
                double[,] value_2 = new RealSubstitution(input_2, Steps, 1, Length).ObtainValues();
                for (int i = 0; i < Length; i++)
                {
                    coor[0, i] = value_1[0, i];
                    coor[1, i] = value_2[0, i];
                }
            }
            int reference = 0;
            for (double steps = start; steps <= ending; steps += increment)
            {
                pos[0, times] = Transform(coor[0, times], 0, scopes, borders)[0];
                pos[1, times] = Transform(0, coor[1, times], scopes, borders)[1];
                in_range[times] = coor[0, times] > xCoor_left && coor[0, times] < xCoor_right &&
                                                 coor[1, times] > yCoor_down && coor[1, times] < yCoor_up;
                if (times > 0 && in_range[times - 1] && in_range[times])
                {
                    double ratio = relative_speed * (steps - start) / (ending - start) % 1;
                    Pen selectedPen;
                    switch (color_mode)
                    {
                        case 1: selectedPen = curve_pen; break;
                        case 2: selectedPen = ratio < 0.5 ? white_pen : black_pen; break;
                        case 3: selectedPen = ratio < 0.5 ? blue_pen : yellow_pen; break;
                        default: selectedPen = colorful_pen; colorful_pen.Color = ObtainWheelCurve(ratio); break;
                    }
                    g.DrawLine(selectedPen, pos[0, times - 1], pos[1, times - 1], pos[0, times], pos[1, times]);
                    VScrollBarX.Enabled = VScrollBarY.Enabled = true; // This is necessary for each loop
                    ScrollMoving(pos[0, times], pos[1, times]);
                    if (reference != (int)(ratio * 100)) DrawReferenceRectangles(selectedPen.Color);
                    reference = (int)(ratio * 100);
                }
                times++; // This is a sensitive position
            }
            point_number += times; times = 0;
            EndRendering();
        }
        private void DisplayFunction(string input) => DisplayBase(input);
        private void DisplayPolar(string input) => DisplayBase(input, isPolar: true);
        private void DisplayParam(string input) => DisplayBase(input, isParam: true);
        private void DisplayLoop(string input)
        {
            input = MyString.ReplaceTagCurves(input); // This is necessary
            string[] split = MyString.SplitString(input);
            if (input.Contains('δ')) { DisplayIterateLoop(split); return; }
            Action<string> displayMethod = input.Contains('α') ? DisplayFunction :
                                           input.Contains('β') ? DisplayPolar :
                                           input.Contains('γ') ? DisplayParam : DisplayPro;
            for (int times = MyString.ToInt(split[2]); times <= MyString.ToInt(split[3]); times++)
                displayMethod(split[0].Replace(split[1], $"({times})"));
        }
        private unsafe void DisplayIterateLoop(string[] split)
        {
            int row = x_right - x_left, column = y_down - y_up;
            (double[,] xCoor, double[,] yCoor) = SetCoor(row, column);
            if (complex_mode)
            {
                Complex[,] table_initial = new Complex[row, column];
                Parallel.For(0, row, i => {
                    fixed (double* xCoorPtr = &xCoor[0, 0], yCoorPtr = &yCoor[0, 0])
                    { for (int j = 0; j < column; j++) table_initial[i, j] = new(xCoorPtr[i * column + j], yCoorPtr[i * column + j]); }
                });
                Complex[,] table_inherit = new ComplexSubstitution(split[1], table_initial, row, column).ObtainValues();
                if (split.Length != 5) throw new FormatException();
                for (int times = MyString.ToInt(split[3]); times <= MyString.ToInt(split[4]); times++)
                {
                    string temp_string = split[0].Replace(split[2], $"({times})");
                    table_inherit = DisplayMini(temp_string, table_initial, table_inherit);
                    if (is_checking) break;
                }
            }
            else
            {
                double[,] table_inherit = new RealSubstitution(split[1], xCoor, yCoor, row, column).ObtainValues();
                if (split.Length != 6) throw new FormatException();
                for (int times = MyString.ToInt(split[3]); times <= MyString.ToInt(split[4]); times++)
                {
                    string temp = split[0].Replace(split[2], $"({times})");
                    table_inherit = new RealSubstitution(temp, xCoor, yCoor, table_inherit, table_inherit, row, column).ObtainValues();
                    DisplayMini(split[5], xCoor, yCoor, table_inherit);
                    if (is_checking) break;
                }
            }
        }
        private static int CalculateAlpha(Complex input) => (int)(255 / (1 + decay_rate * Complex.Modulus(input)));
        private static (int region, int proportion) CalculatePhase(double argument)
        {
            int proportion, region = argument < 0 ? -1 : (int)(3 * argument / Math.PI);
            if (region > 5) { region = proportion = 0; }
            else proportion = Math.Clamp((int)(255 * (argument - region * (Math.PI / 3)) / (Math.PI / 3)), 0, 255);
            return (region, proportion);
        }
        private static Color Obtain(int region, int proportion, int alpha) => ObtainAlpha(region, proportion, 1, alpha);
        private static Color ObtainAlpha(int region, int proportion, double alpha, int beta) => region switch
        {
            0 => Color.FromArgb(beta, (int)(255 * alpha), (int)(proportion * alpha), 0),
            1 => Color.FromArgb(beta, (int)((255 - proportion) * alpha), (int)(255 * alpha), 0),
            2 => Color.FromArgb(beta, 0, (int)(255 * alpha), (int)(proportion * alpha)),
            3 => Color.FromArgb(beta, 0, (int)((255 - proportion) * alpha), (int)(255 * alpha)),
            4 => Color.FromArgb(beta, (int)(proportion * alpha), 0, (int)(255 * alpha)),
            5 => Color.FromArgb(beta, (int)(255 * alpha), 0, (int)((255 - proportion) * alpha)),
            _ => Color.Empty
        };
        private static Color ObtainWheel(Complex input) => ObtainWheelInternal(false, input, 255);
        private static Color ObtainWheel(Complex input, double alpha)
            => ObtainWheelInternal(true, input, 255, Math.Clamp(alpha, 0, 1));
        private static Color ObtainWheelAlpha(Complex input) => ObtainWheelInternal(false, input, CalculateAlpha(input));
        private static Color ObtainWheelAlpha(Complex input, double alpha)
            => ObtainWheelInternal(true, input, CalculateAlpha(input), Math.Clamp(alpha, 0, 1));
        private static Color ObtainWheelInternal(bool isAlpha, Complex input, int calculatedAlpha, double alpha = 0)
        {
            (int color_region, int temp_proportion) = CalculatePhase(ComplexSubstitution.ArgumentForRGB(input));
            return !isAlpha ? Obtain(color_region, temp_proportion, calculatedAlpha) : ObtainAlpha(color_region, temp_proportion, alpha, calculatedAlpha);
        }
        private static Color ObtainWheelCurve(double alpha)
        {
            (int color_region, int temp_proportion) = CalculatePhase(Math.Clamp(alpha, 0, 1) * Math.Tau);
            return Obtain(color_region, temp_proportion, 255);
        }
        private static Color GetColorFromAlpha(double alpha, double beta, bool useBeta)
        {
            if (alpha <= 0.5) // From blue to purple
            {
                int red = (int)(510 * alpha), green = 0, blue = 255;
                return Color.FromArgb(useBeta ? (int)(red * beta) : red, green, (int)(blue * (useBeta ? beta : 1)));
            }
            else // From purple to red
            {
                int temp = 255 - (int)(510 * (alpha - 0.5));
                return Color.FromArgb((int)(255 * (useBeta ? beta : 1)), 0, (int)(temp * (useBeta ? beta : 1)));
            }
        }
        private static Color ObtainBase(double input, double beta, double min, double max, bool useBeta)
        {
            double alpha = (input - min) / (max - min);
            if (alpha >= 0 && alpha <= 1) return GetColorFromAlpha(alpha, useBeta ? beta : 1, useBeta);
            return Color.Empty;
        }
        private static Color ObtainStrip(double input, double min, double max) => ObtainBase(input, 1, min, max, false);
        private static Color ObtainStripAlpha(double input, double beta, double min, double max)
            => ObtainBase(input, beta, min, max, true);
        #endregion
        #region Mouse
        private void Graph_MouseMove(object sender, MouseEventArgs e)
        {
            if (!(activate_mousemove && clicked && !text_changed && InputString.Text != String.Empty)) return;
            if (!is_main && e.X > X_LEFT_MIC && e.X < X_RIGHT_MIC && e.Y > Y_UP_MIC && e.Y < Y_DOWN_MIC)
                RunMouseMove(sender, e, X_LEFT_MIC, X_RIGHT_MIC, Y_UP_MIC, Y_DOWN_MIC);
            else if (is_main && e.X > X_LEFT_MAC && e.X < X_RIGHT_MAC && e.Y > Y_UP_MAC && e.Y < Y_DOWN_MAC)
                RunMouseMove(sender, e, X_LEFT_MAC, X_RIGHT_MAC, Y_UP_MAC, Y_DOWN_MAC);
            else DefaultReference(false);
        }
        private void Graph_MouseDown(object sender, MouseEventArgs e)
        {
            if (!(activate_mousemove && clicked && !text_changed && InputString.Text != String.Empty)) return;
            if (!is_main && e.X > X_LEFT_MIC && e.X < X_RIGHT_MIC && e.Y > Y_UP_MIC && e.Y < Y_DOWN_MIC)
                RunMouseDown(e, X_LEFT_MIC, X_RIGHT_MIC, Y_UP_MIC, Y_DOWN_MIC);
            else if (is_main && e.X > X_LEFT_MAC && e.X < X_RIGHT_MAC && e.Y > Y_UP_MAC && e.Y < Y_DOWN_MAC)
                RunMouseDown(e, X_LEFT_MAC, X_RIGHT_MAC, Y_UP_MAC, Y_DOWN_MAC);
        }
        private static void HandleMouseAction(MouseEventArgs e, int x_left, int x_right, int y_up, int y_down, Action<double, double> action)
        {
            int[] borders = new int[] { x_left, x_right, y_up, y_down };
            double xCoor = InverseTransform(e.X, e.Y, scopes, borders)[0];
            double yCoor = InverseTransform(e.X, e.Y, scopes, borders)[1];
            action(xCoor, yCoor);
        }
        private void RunMouseMove(object sender, MouseEventArgs e, int x_left, int x_right, int y_up, int y_down)
        {
            SetReference(sender, e);
            HandleMouseAction(e, x_left, x_right, y_up, y_down, (xCoor, yCoor)
                => { ScrollMoving(xCoor, yCoor); DisplayMouseMove(e, xCoor, yCoor); });
        }
        private void RunMouseDown(MouseEventArgs e, int x_left, int x_right, int y_up, int y_down)
        {
            points_chosen++;
            HandleMouseAction(e, x_left, x_right, y_up, y_down, (xCoor, yCoor) => { DisplayMouseDown(e, xCoor, yCoor); });
        }
        private void DisplayMouseMove(MouseEventArgs e, double xCoor, double yCoor)
        {
            X_CoorDisplay.Text = MyString.TrimLargeDouble(xCoor, 1000000);
            Y_CoorDisplay.Text = MyString.TrimLargeDouble(yCoor, 1000000);
            ModulusDisplay.Text = MyString.TrimLargeDouble(Math.Sqrt(xCoor * xCoor + yCoor * yCoor), 1000000);
            AngleDisplay.Text = (ComplexSubstitution.ArgumentForRGB(xCoor, yCoor) / Math.PI).ToString("#0.00000") + " * PI";
            if (!MyString.ContainFunctionName(InputString.Text))
            {
                if (!complex_mode) DisplayValuesReal(e.X, e.Y);
                else DisplayValuesComplex(e.X, e.Y);
            }
            else FunctionDisplay.Text = "Unavailable in this mode.";
        }
        private void DisplayMouseDown(MouseEventArgs e, double xCoor, double yCoor)
        {
            string X_Coor = MyString.TrimLargeDouble(xCoor, 100);
            string Y_Coor = MyString.TrimLargeDouble(yCoor, 100);
            string Modulus = MyString.TrimLargeDouble(Math.Sqrt(xCoor * xCoor + yCoor * yCoor), 100);
            string Angle = (ComplexSubstitution.ArgumentForRGB(xCoor, yCoor) / Math.PI).ToString("#0.000000") + " * PI";
            if (!MyString.ContainFunctionName(InputString.Text))
            {
                string ValueDisplay;
                if (complex_mode)
                {
                    Complex clicked_value = output_table[e.X - 1 - x_left, e.Y - 1 - y_up];
                    ValueDisplay = $"Re = {MyString.TrimLargeDouble(clicked_value.real, 100)}\r\nIm = {MyString.TrimLargeDouble(clicked_value.imaginary, 100)}";
                }
                else
                {
                    double clicked_value = Output_table[e.X - 1 - x_left, e.Y - 1 - y_up];
                    ValueDisplay = $"f(x, y) = {MyString.TrimLargeDouble(clicked_value, 100)}";
                }
                DraftBox.Text = $"\r\n>>>>> POINT_{points_chosen} <<<<<\r\n\r\nx = {X_Coor}\r\ny = {Y_Coor}" +
                    $"\r\n\r\nmod = {Modulus}\r\narg = {Angle}\r\n\r\n{ValueDisplay}\r\n" + DraftBox.Text;
            }
            else DraftBox.Text = $"\r\n>>>>> POINT_{points_chosen} <<<<<\r\n\r\nx = {X_Coor}\r\ny = {Y_Coor}" +
                    $"\r\n\r\nmod = {Modulus}\r\narg = {Angle}\r\n" + DraftBox.Text;
        }
        private void DisplayValuesComplex(int xCoor, int yCoor)
        {
            Complex clicked_value = output_table[xCoor - 1 - x_left, yCoor - 1 - y_up];
            FunctionDisplay.Text = $"[Re] {clicked_value.real}\r\n[Im] {clicked_value.imaginary}";
        }
        private void DisplayValuesReal(int xCoor, int yCoor)
            => FunctionDisplay.Text = Convert.ToString(Output_table[xCoor - 1 - x_left, yCoor - 1 - y_up]);
        private void DrawReferenceRectangles(Color color)
            => CreateGraphics().FillRectangle(new SolidBrush(color), VScrollBarX.Location.X - REF_POS_1, Y_UP_MIC + REF_POS_2,
                2 * (VScrollBarX.Width + REF_POS_1), VScrollBarX.Height - 2 * REF_POS_2);
        private void DefaultReference(bool isInitial)
        {
            if (!isInitial) Cursor = Cursors.Default;
            DrawReferenceRectangles(SystemColors.ControlDark);
            if (!isInitial) VScrollBarX.Enabled = VScrollBarY.Enabled = false;
        }
        private void SetReference(object sender, MouseEventArgs e)
        {
            Cursor = Cursors.Cross;
            DrawReferenceRectangles(GetMouseColor(sender, e));
            VScrollBarX.Enabled = VScrollBarY.Enabled = true;
        }
        private static Color GetMouseColor(object sender, EventArgs e)
        {
            Bitmap bmp = new(1, 1);
            Graphics.FromImage(bmp).CopyFromScreen(Cursor.Position, Point.Empty, new Size(1, 1));
            return bmp.GetPixel(0, 0);
        }
        private void ScrollMoving(double xCoor, double yCoor)
        {
            VScrollBarX.Value = (int)((VScrollBarX.Maximum - VScrollBarX.Minimum) * (xCoor - scopes[0]) / (scopes[1] - scopes[0]));
            VScrollBarY.Value = (int)((VScrollBarY.Maximum - VScrollBarY.Minimum) * (yCoor - scopes[3]) / (scopes[2] - scopes[3]));
        }
        private void ScrollMoving(int xPos, int yPos)
            => ScrollMoving(InverseTransform(xPos, yPos, scopes, borders)[0], InverseTransform(xPos, yPos, scopes, borders)[1]);
        private async void ConfirmButton_Click(object sender, EventArgs e)
            => await ExecuteAsync(() => RunConfirmButton_Click(sender, e));
        private async void PreviewButton_Click(object sender, EventArgs e)
            => await ExecuteAsync(() => RunPreviewButton_Click(sender, e));
        private async void AllButton_Click(object sender, EventArgs e) => await ExecuteAsync(() => RunAllButton_Click(sender, e));
        private void RunButtonClick(Action endAction, int xLeft, int xRight, int yUp, int yDown, bool isMain)
        {
            try
            {
                PrepareGraphing();
                PrepareScopes(xLeft, xRight, yUp, yDown, isMain);
                SetTDSB();
                DisplayOnScreen();
                endAction();
            }
            catch (Exception) { ErrorBox("THE INPUT IS IN A WRONG FORMAT."); }
        }
        private void RunConfirmButton_Click(object sender, EventArgs e)
            => RunButtonClick(EndMacro, X_LEFT_MAC, X_RIGHT_MAC, Y_UP_MAC, Y_DOWN_MAC, true);
        private void RunPreviewButton_Click(object sender, EventArgs e)
            => RunButtonClick(EndMicro, X_LEFT_MIC, X_RIGHT_MIC, Y_UP_MIC, Y_DOWN_MIC, false);
        private void RunAllButton_Click(object sender, EventArgs e)
        {
            try
            {
                PrepareGraphing();
                PrepareScopes(X_LEFT_MIC, X_RIGHT_MIC, Y_UP_MIC, Y_DOWN_MIC, false);
                SetTDSB();
                DisplayOnScreen();
                MiddleAll();
                PrepareScopes(X_LEFT_MAC, X_RIGHT_MAC, Y_UP_MAC, Y_DOWN_MAC, true);
                SetTDSB();
                DisplayOnScreen();
                EndMacro();
            }
            catch (Exception) { ErrorBox("THE INPUT IS IN A WRONG FORMAT."); }
        }
        private async Task ExecuteAsync(Action action)
        {
            BlockInput(true);
            PrepareAsync();
            await Task.Run(() =>
            {
                Thread.CurrentThread.Priority = ThreadPriority.Highest;
                action();
            });
            RunCopyToClipboard();
            BlockInput(false);
        }
        private void PrepareAsync()
        {
            elapsedSeconds = 0;
            TimeDisplay.Text = "0s";
            if (InputString.Text != String.Empty)
            {
                DisplayTimer.Start(); WaitTimer.Start(); GraphTimer.Start();
                TimeNow = DateTime.Now;
                waiting = false;
            }
            else PointNumDisplay.Text = "0";
        }
        private void CopyToClipboard()
        {
            if (InputString.Text == String.Empty) return;
            Clipboard.SetText(InputString.Text);
        }
        private void RunCopyToClipboard()
        {
            // Ensure clipboard operation is done on the UI thread
            if (InvokeRequired) Invoke((MethodInvoker)delegate { CopyToClipboard(); });
            else CopyToClipboard();
        }
        private void PrepareGraphing()
        {
            if (InputString.Text == String.Empty) return;
            RestoreMelancholy();
            DisableControls();
            point_number = times = export_number = 0;
            address_error = is_checking = text_changed = false;
            clicked = true; plot_loop++;
        }
        private static void PrepareScopes(int xLeft, int xRight, int yUp, int yDown, bool isMain)
        { is_main = isMain; x_left = xLeft; x_right = xRight; y_up = yUp; y_down = yDown; }
        private void DisplayOnScreen()
        {
            if (InputString.Text == String.Empty) return;
            string[] split = MyString.SplitByChars(RecoverMultiplication.BeautifyInput(InputString.Text, complex_mode), new char[] { '|' });
            for (int loops = 0; loops < split.Length; loops++)
            {
                bool temp = !(split[loops].Contains("Loop") || split[loops].Contains("loop")) && !complex_mode;
                if ((split[loops].Contains("Func") || split[loops].Contains("func")) && temp)
                    DisplayFunction(split[loops]);
                else if ((split[loops].Contains("Polar") || split[loops].Contains("polar")) && temp)
                    DisplayPolar(split[loops]);
                else if ((split[loops].Contains("Param") || split[loops].Contains("param")) && temp)
                    DisplayParam(split[loops]);
                else if (split[loops].Contains("Loop") || split[loops].Contains("loop"))
                    DisplayLoop(split[loops]);
                else
                    DisplayPro(split[loops]);
            }
        }
        private void TimeDraft(string mode, int plotLoop)
        {
            TimeDisplay.Text = $"{TimeCount:hh\\:mm\\:ss\\.fff}";
            if (!drafted) DraftBox.Text = String.Empty; drafted = true;
            DraftBox.Text = $"\r\n>>>> {plotLoop}_{mode} <<<<\r\n\r\n{InputString.Text}\r\n\r\n" +
                $"Pixels: {PointNumDisplay.Text}\r\nDuration: {TimeDisplay.Text}\r\n" + DraftBox.Text;
        }
        private void MiddleAll()
        {
            GraphTimer.Stop();
            TimeCount = DateTime.Now - TimeNow;
            TimeDraft("MICRO", plot_loop);
            plot_loop++;
            GraphTimer.Start();
            TimeNow = DateTime.Now;
        }
        private void EndProcess(string mode, int plotLoop, bool updateCaption)
        {
            ActivateControls();
            main_drawn = updateCaption;
            if (main_drawn) CaptionBox.Text = $"{InputString.Text}\r\n" + CaptionBox.Text;
            TimeDraft(mode, plotLoop);
            InputString.Focus(); InputString.SelectionStart = InputString.Text.Length;
            if (auto_export && !address_error) RunStoreButton_Click();
        }
        private void EndMicro() => EndProcess("MICRO", plot_loop, false);
        private void EndMacro() => EndProcess("MACRO", plot_loop, true);
        private void SetTextboxButton(bool readOnly)
        {
            TextBox[] textBoxes = new[] { InputString, GeneralInput, X_Left, X_Right, Y_Left, Y_Right, ThickInput, DenseInput, AddressInput };
            foreach (TextBox textBox in textBoxes) textBox.ReadOnly = readOnly;
            ConfirmButton.Enabled = PreviewButton.Enabled = AllButton.Enabled = !readOnly;
            activate_mousemove = !readOnly; commence_waiting = readOnly;
        }
        private void DisableControls() => SetTextboxButton(true);
        private void ActivateControls()
        {
            DisplayTimer.Stop(); WaitTimer.Stop(); GraphTimer.Stop(); // These shall be here
            TimeCount = DateTime.Now - TimeNow;
            SetTextboxButton(false);
            PictureWait.Visible = VScrollBarX.Enabled = VScrollBarY.Enabled = false;
            GC.Collect();
        }
        private void ErrorBox(string message)
        {
            if (!is_checking) clicked = false;
            ExceptionMessageBox.Show(message +
                "\r\nCommon mistakes include:" +
                "\r\n\r\n1. Misspelling of function/variable names;" +
                "\r\n2. Incorrect grammar of special functions;" +
                "\r\n3. Excess or deficiency of characters;" +
                "\r\n4. Real/Complex mode confusion;", 450, 300);
            if (!is_checking) ActivateControls();
        }
        private void ExportButton_Click(object sender, EventArgs e)
        { RestoreMelancholy(); RunExportButton_Click(); }
        private void StoreButton_Click(object sender, EventArgs e)
        { RestoreMelancholy(); RunStoreButton_Click(); }
        private void RunExportButton_Click() => HandleExportOrStore(ExportGraph, "Snapshot saved at");
        private void RunStoreButton_Click() => HandleExportOrStore(ExportHistory, "History stored at");
        private void HandleExportOrStore(Action exportAction, string messagePrefix)
        {
            try
            {
                if (AddressInput.Text == String.Empty) AddressInput.Text = ADDRESS_DEFAULT;
                exportAction();
                if (!drafted) DraftBox.Text = String.Empty;
                DraftBox.Text = $"\r\n{messagePrefix} \r\n{DateTime.Now:HH_mm_ss}\r\n" + DraftBox.Text;
                drafted = true;
            }
            catch (Exception)
            {
                clicked = false; address_error = true;
                ExceptionMessageBox.Show("THE ADDRESS DOES NOT EXIST." +
                    "\r\nCommon mistakes include:" +
                    "\r\n\r\n1. Files not created beforehand;" +
                    "\r\n2. The address ending with \\;" +
                    "\r\n3. The address quoted automatically;" +
                    "\r\n4. The file storage being full;", 450, 300);
            }
        }
        private void ExportGraph()
        {
            export_number++;
            Bitmap bmp = new(Width - 22, Height - 55);
            Graphics g = Graphics.FromImage(bmp);
            g.CopyFromScreen(Left + 11, Top + 45, 0, 0, bmp.Size);
            bmp.Save($@"{AddressInput.Text}\{DateTime.Now:yyyy}_{DateTime.Now.DayOfYear}_{DateTime.Now:HH}_{DateTime.Now:mm}_{DateTime.Now:ss}_No.{export_number}.png");
        }
        private void ExportHistory()
        {
            StreamWriter writer = new($@"{AddressInput.Text}\{DateTime.Now:yyyy}_{DateTime.Now.DayOfYear}_{DateTime.Now:HH}_{DateTime.Now:mm}_{DateTime.Now:ss}_stockpile.txt");
            writer.Write(DraftBox.Text);
        }
        #endregion
        #region Keys
        private void FalseColor()
        {
            InputString.BackColor = InputLabel.ForeColor = ERROR_RED;
            PictureIncorrect.Visible = true; PictureCorrect.Visible = false;
        }
        private void CheckValidity() => CheckValidityCore(FalseColor);
        private void CheckValidityDetailed() => CheckValidityCore(() => ErrorBox("THE INPUT IS IN A WRONG FORMAT."));
        private void CheckValidityCore(Action errorHandler)
        {
            try
            {
                is_checking = true;
                PrepareScopes(X_LEFT_CHECK, X_RIGHT_CHECK, Y_UP_CHECK, Y_DOWN_CHECK, false);
                SetTDSB();
                if (InputString.Text == String.Empty)
                {
                    InputString.BackColor = FOCUS_GRAY;
                    InputLabel.ForeColor = Color.White;
                    PictureIncorrect.Visible = PictureCorrect.Visible = false;
                }
                else
                {
                    DisplayOnScreen();
                    InputString.BackColor = InputLabel.ForeColor = CORRECT_GREEN;
                    PictureIncorrect.Visible = false; PictureCorrect.Visible = true;
                }
                activate_mousemove = false;
                VScrollBarX.Enabled = VScrollBarY.Enabled = false;
            }
            catch (Exception) { errorHandler(); }
        }
        private void MiniChecks(Control Ctrl, Control ctrl)
        {
            try
            {
                if (!AllButton.Enabled) return;
                if (Ctrl.Text == String.Empty) ctrl.ForeColor = Color.White;
                else
                {
                    double temp = RealSubstitution.ObtainValue(Ctrl.Text); // For checking
                    ctrl.ForeColor = CORRECT_GREEN;
                }
            }
            catch (Exception) { ctrl.ForeColor = ERROR_RED; }
        }
        private void CheckAll(object sender, EventArgs e)
        {
            InputString_DoubleClick(sender, e);
            GeneralInput_DoubleClick(sender, e);
            X_Left_DoubleClick(sender, e);
            X_Right_DoubleClick(sender, e);
            Y_Left_DoubleClick(sender, e);
            Y_Right_DoubleClick(sender, e);
            ThickInput_DoubleClick(sender, e);
            DenseInput_DoubleClick(sender, e);
            AddressInput_DoubleClick(sender, e);
        }
        private void AutoCheckComplex()
        {
            string input = InputString.Text;
            input = input.Replace("zeta", String.Empty).Replace("Zeta", String.Empty);
            CheckComplex.Checked = MyString.ContainsAny(input, new char[] { 'z', 'Z' });
        }
        private void BarSomeKeys(object sender, KeyPressEventArgs e)
        { if (BARREDCHARS.Contains(e.KeyChar)) e.Handled = true; }
        private static void AutoKeyDown(TextBox ctrl, KeyEventArgs e)
        {
            if (ctrl.ReadOnly) return;
            int caretPosition = ctrl.SelectionStart; // This is a necessary intermediate variable
            if (e.KeyCode == Keys.D9 && (ModifierKeys & Keys.Shift) != 0)
            {
                if (ctrl.SelectionLength == 0)
                {
                    ctrl.Text = ctrl.Text.Insert(caretPosition, "()");
                    SelectSuppress(ctrl, e, caretPosition, 1);
                }
                else
                {
                    string selectedText = ctrl.Text.Substring(caretPosition, ctrl.SelectionLength);
                    ctrl.Text = ctrl.Text.Remove(caretPosition, ctrl.SelectionLength).Insert(caretPosition, "(" + selectedText + ")");
                    SelectSuppress(ctrl, e, caretPosition, selectedText.Length + 2);
                }
            }
            else if (e.KeyCode == Keys.D0 && (ModifierKeys & Keys.Shift) != 0)
            {
                if (ctrl.Text[caretPosition - 1] == '(') SelectSuppress(ctrl, e, caretPosition, 1);
                else SelectSuppress(ctrl, e, caretPosition, 0);
            }
            else if (e.KeyCode == Keys.Oemcomma)
            {
                ctrl.Text = ctrl.Text.Insert(caretPosition, ", ");
                SelectSuppress(ctrl, e, caretPosition, 2);
            }
            else if (e.KeyCode == Keys.OemPipe)
            {
                ctrl.Text = ctrl.Text.Insert(caretPosition, " | ");
                SelectSuppress(ctrl, e, caretPosition, 3);
            }
            else if (e.KeyCode == Keys.Back)
            {
                if (ctrl.SelectionLength > 0)
                {
                    if (MyString.ParenthesisCheck(ctrl.Text.AsSpan(ctrl.SelectionStart, ctrl.SelectionLength), '(', ')'))
                        ctrl.Text = ctrl.Text.Remove(ctrl.SelectionStart, ctrl.SelectionLength);
                    SelectSuppress(ctrl, e, caretPosition, 0);
                }
                else if (caretPosition == 0) return; // This is necessary
                else if (ctrl.Text[caretPosition - 1] == '(')
                {
                    if (ctrl.Text[caretPosition] == ')') ctrl.Text = ctrl.Text.Remove(caretPosition - 1, 2);
                    SelectSuppress(ctrl, e, caretPosition, -1);
                }
                else if (caretPosition > 1 && ctrl.Text[caretPosition - 1] == ')')
                    SelectSuppress(ctrl, e, caretPosition, -1);
            }
        }
        private static void SelectSuppress(TextBox ctrl, KeyEventArgs e, int caretPosition, int caretMove)
        {
            ctrl.SelectionStart = caretPosition + caretMove;
            e.SuppressKeyPress = true;
        }
        private void Graph_KeyUp(object sender, KeyEventArgs e)
        {
            HandleModifierKeys(e, false);
            if (suppressKeyUp) return;
            if (HandleSpecialKeys(e)) return;
            HandleCtrlCombination(sender, e);
        }
        private void Graph_KeyDown(object sender, KeyEventArgs e)
        {
            HandleModifierKeys(e, true);
            if (sftPressed && e.KeyCode == Keys.Back && AllButton.Enabled)
                ExecuteWithSuppression(() => SubtitleBox_DoubleClick(sender, e), e);
            else if (e.KeyCode == Keys.Delete) ExecuteWithSuppression(null, e);
        }
        private static void HandleModifierKeys(KeyEventArgs e, bool isKeyDown)
        {
            if (e.KeyCode == Keys.ControlKey) { ctrlPressed = isKeyDown; e.Handled = true; }
            else if (e.KeyCode == Keys.ShiftKey) { sftPressed = isKeyDown; e.Handled = true; }
        }
        private bool HandleSpecialKeys(KeyEventArgs e)
        {
            switch (e.KeyCode)
            {
                case Keys.Escape: ExecuteWithSuppression(() => Close(), e); return true;
                case Keys.Oemtilde: ExecuteWithSuppression(() => PlayOrPause(), e); return true;
                case Keys.Delete: ExecuteWithSuppression(() => PressDelete(e), e); return true;
                default: return false;
            }
        }
        private void HandleCtrlCombination(object sender, KeyEventArgs e)
        {
            if (!ctrlPressed) return;
#pragma warning disable CS8600 // Converting null literal or possible null value to non-nullable type.
            Action action = e.KeyCode switch
            {
                Keys.D3 => () => ClearButton_Click(sender, e),
                Keys.D2 => () => PictureLogo_DoubleClick(sender, e),
                Keys.OemQuestion => () => TitleLabel_DoubleClick(sender, e),
                Keys.P when PreviewButton.Enabled => () => PreviewButton_Click(sender, e),
                Keys.G when ConfirmButton.Enabled => () => ConfirmButton_Click(sender, e),
                Keys.B when AllButton.Enabled => () => AllButton_Click(sender, e),
                Keys.S when ExportButton.Enabled => () => ExportButton_Click(sender, e),
                Keys.K => () => StoreButton_Click(sender, e),
                Keys.R => () => Graph_DoubleClick(sender, e),
                Keys.D => () => RestoreDefault(),
                Keys.C when sftPressed && AllButton.Enabled => () => CheckAll(sender, e),
                _ => null
            };
#pragma warning restore CS8600 // Converting null literal or possible null value to non-nullable type.
            if (action != null) ExecuteWithSuppression(action, e);
        }
        private static void ExecuteWithSuppression(Action action, KeyEventArgs e)
        {
            suppressKeyUp = true;
            action?.Invoke();
            e.Handled = true;
            e.SuppressKeyPress = true;
            suppressKeyUp = false;
        }
        private void PressDelete(KeyEventArgs e)
        {
            RestoreMelancholy();
            DeleteMain_Click(this, e);
            DeletePreview_Click(this, e);
        }
        private void Graph_DoubleClick(object sender, EventArgs e) => RestoreMelancholy();
        private void RestoreMelancholy()
        {
            InputString.BackColor = FOCUS_GRAY;
            Label[] labels = new[] { InputLabel, AtLabel, GeneralLabel, DetailLabel, X_Scope, Y_Scope, ThickLabel, DenseLabel, ExampleLabel, FunctionLabel, ModeLabel, ContourLabel };
            foreach (Label label in labels) label.ForeColor = Color.White;
            PictureIncorrect.Visible = PictureCorrect.Visible = false;
            is_checking = false;
        }
        private void RestoreDefault()
        {
            InputString.Text = INPUT_DEFAULT;
            InputString.SelectionStart = InputString.Text.Length;
            GeneralInput.Text = GENERAL_DEFAULT;
            ThickInput.Text = THICKNESS_DEFAULT;
            DenseInput.Text = DENSENESS_DEFAULT;
            AddressInput.Text = ADDRESS_DEFAULT;
            ComboColoring.SelectedIndex = 4;
            ComboContour.SelectedIndex = 1;
            CheckAuto.Checked = CheckSwap.Checked = CheckPoints.Checked = CheckShade.Checked = CheckRetain.Checked = false;
            CheckEdit.Checked = CheckComplex.Checked = CheckCoor.Checked = true;
        }
        #endregion
        #region Micellaneous
        private void ComboColoring_SelectedIndexChanged(object sender, EventArgs e)
            => color_mode = ComboColoring.SelectedIndex + 1;
        private void ComboContour_SelectedIndexChanged(object sender, EventArgs e)
            => contour_mode = ComboContour.SelectedIndex + 1;
        private void ComboExamples_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (ComboExamples.SelectedIndex == -1 || InputString.ReadOnly || ComboExamples.SelectedItem.ToString() == String.Empty) return;
            InputString.Text = ComboExamples.SelectedItem.ToString();
            CheckComplex.Checked = ComboExamples.SelectedIndex < 8;
            SetValuesForSelectedIndex(ComboExamples.SelectedIndex);
            DeleteMain_Click(this, e);
            DeletePreview_Click(this, e);
            ComboExamples.SelectedIndex = -1;
            InputString.Focus();
            InputString.SelectionStart = InputString.Text.Length;
        }
        private void SetValuesForSelectedIndex(int index)
        {
            string generalScope;
            string thickness = "1";
            string denseness = "1";
            int colorIndex = 3;
            bool pointsChecked = false, retainChecked = false, shadeChecked = false;
            switch (index)
            {
                case 0: generalScope = "1.1"; colorIndex = 4; pointsChecked = true; break;
                case 1: generalScope = "1.2"; colorIndex = 3; break;
                case 2: generalScope = "1.1"; colorIndex = 2; pointsChecked = true; break;
                case 3: generalScope = "pi/2"; colorIndex = 4; break;
                case 4: generalScope = "4"; thickness = "0.1"; colorIndex = 3; shadeChecked = true; break;
                case 5: generalScope = "3"; break;
                case 6: generalScope = "0"; X_Left.Text = "-1.6"; X_Right.Text = "0.6"; Y_Left.Text = "-1.1"; Y_Right.Text = "1.1"; break;
                case 7: generalScope = "2"; colorIndex = 4; shadeChecked = true; break;
                case 9: generalScope = "4pi"; thickness = "0.1"; colorIndex = 2; pointsChecked = true; break;
                case 10: generalScope = "2pi"; colorIndex = 4; break;
                case 11: generalScope = "2.5"; thickness = "0.3"; colorIndex = 1; pointsChecked = true; break;
                case 12: generalScope = "0"; X_Left.Text = "0"; X_Right.Text = "1"; Y_Left.Text = "0"; Y_Right.Text = "1"; thickness = "0.2"; colorIndex = 0; retainChecked = true; break;
                case 13: generalScope = "10"; thickness = "0.1"; colorIndex = 1; pointsChecked = true; break;
                case 14: generalScope = "5"; colorIndex = 1; break;
                case 15: generalScope = "3"; break;
                case 16: generalScope = "4"; thickness = "0.05"; colorIndex = 2; pointsChecked = true; break;
                case 18: generalScope = "5.5"; break;
                case 19: generalScope = "pi"; thickness = "0.5"; denseness = "10"; colorIndex = 2; break;
                case 20: generalScope = "3"; colorIndex = 0; break;
                case 21: generalScope = "1.1"; denseness = "100"; colorIndex = 1; break;
                case 22: generalScope = "1.1"; thickness = "0.5"; colorIndex = 3; break;
                case 23: generalScope = "1.1"; thickness = "0.5"; denseness = "10"; retainChecked = true; break;
                case 24: generalScope = "1.1"; thickness = "0.5"; colorIndex = 3; break;
                case 25: generalScope = "0"; X_Left.Text = "-0.2"; X_Right.Text = "1.2"; Y_Left.Text = "-0.2"; Y_Right.Text = "1.2"; thickness = "0.5"; colorIndex = 0; retainChecked = true; break;
                default: ComboExamples.SelectedIndex = -1; return;
            }
            GeneralInput.Text = generalScope;
            ThickInput.Text = thickness;
            DenseInput.Text = denseness;
            ComboColoring.SelectedIndex = colorIndex;
            CheckPoints.Checked = pointsChecked;
            CheckRetain.Checked = retainChecked;
            CheckShade.Checked = shadeChecked;
        }
        private void ComboSelectionChanged(string selectedItem)
        {
            if (InputString.ReadOnly) return;
            InputString.Text = MyString.Replace(InputString.Text, selectedItem, InputString.SelectionStart, InputString.SelectionStart + InputString.SelectionLength - 1);
            InputString.SelectionStart += selectedItem.Length - 1;
            InputString.Focus();
        }
        private void ComboFunctions_SelectedIndexChanged(object sender, EventArgs e)
            => ComboSelectionChanged(ComboFunctions.SelectedItem.ToString());
        private void ComboSpecial_SelectedIndexChanged(object sender, EventArgs e)
            => ComboSelectionChanged(ComboSpecial.SelectedItem.ToString());
        private void CheckCoor_CheckedChanged(object sender, EventArgs e) => delete_coordinate = !delete_coordinate;
        private void CheckSwap_CheckedChanged(object sender, EventArgs e) => swap_colors = !swap_colors;
        private void CheckComplex_CheckedChanged(object sender, EventArgs e) => complex_mode = !complex_mode;
        private void CheckPoints_CheckedChanged(object sender, EventArgs e) => delete_point = !delete_point;
        private void CheckRetain_CheckedChanged(object sender, EventArgs e) => retain_graph = !retain_graph;
        private void CheckShade_CheckedChanged(object sender, EventArgs e) => shade_rainbow = !shade_rainbow;
        private void CheckAuto_CheckedChanged(object sender, EventArgs e)
        {
            auto_export = !auto_export;
            if (CheckAuto.Checked) AddressInput_DoubleClick(sender, e);
        }
        private void CheckEdit_CheckedChanged(object sender, EventArgs e)
        {
            DraftBox.ReadOnly = !DraftBox.ReadOnly;
            if (DraftBox.ReadOnly)
            {
                DraftBox.BackColor = Color.Black;
                DraftBox.ForeColor = READONLY_GRAY;
                DraftBox.ScrollBars = ScrollBars.None;
            }
            else
            {
                DraftBox.BackColor = SystemColors.ControlDarkDark;
                DraftBox.ForeColor = Color.White;
                DraftBox.ScrollBars = ScrollBars.Vertical;
            }
        }
        private void TitleLabel_DoubleClick(object sender, EventArgs e)
        {
            FormalMessageBox.Show("DESIGNER: Fraljimetry\r\nDATE: Oct, 2024\r\nLOCATION: Xi'an, China" +
                "\r\n\r\nThis software was developed in Visual Studio 2022, written in C# Winform, to visualize real or complex functions or equations with no more than two variables. To bolster artistry and practicality, numerous modes are rendered, making it possible to generate images that fit users' needs perfectly." +
                "\r\n\r\n(I wish the definitions of these operations are self-evident if you try some inputs yourself or refer to the examples.)" +
                "\r\n\r\n********** ELEMENTARY **********" +
                "\r\n\r\n+ - * / ^ ( )" +
                "\r\n\r\nLog/Ln, Exp, Sqrt, Abs, Sin, Cos, Tan, Sinh/Sh, Cosh/Ch, Tanh/Th, Arcsin/Asin, Arccos/Acos, Arctan/Atan, Arsinh/Arsh, Arccosh/Arch, Arctanh/Arth (f(x,y)/f(z))" +
                "\r\n\r\nConjugate/Conj(f(z)), e(f(z))    // e(z)=exp(2*pi*i*z)" +
                "\r\n\r\n********** COMBINATORICS **********" +
                "\r\n\r\nFloor(double a), Ceil(double a), Round(double a), " +
                "\r\nSign/Sgn(double a)" +
                "\r\n\r\nMod(double a, double n), nCr(int n, int r), nPr(int n, int r)" +
                "\r\n\r\nMax(double a, double b, ...), Min(double a, double b, ...), Factorial/Fact(int n)" +
                "\r\n\r\n********** SPECIAL FUNCTIONS **********" +
                "\r\n\r\nF(double/Complex a, double/Complex b, double/Complex c, f(x,y)/f(z)) / " +
                "\r\nF(double/Complex a, double/Complex b, double/Complex c, f(x,y)/f(z), int n)" +
                "\r\n// HyperGeometric Series." +
                "\r\n\r\nGamma/Ga(f(x,y)/f(z)) / Gamma/Ga(f(x,y)/f(z), int n)" +
                "\r\n\r\nBeta(f(x,y)/f(z), g(x,y)/g(z)) / " +
                "\r\nBeta(f(x,y)/f(z), g(x,y)/g(z), int n)" +
                "\r\n\r\nZeta(f(x,y)/f(z)) / Zeta(f(x,y)/f(z), int n)" +
                "\r\n// This is a mess for n too large." +
                "\r\n\r\n********** REPETITIOUS OPERATIONS **********" +
                "\r\n// Capitalizations represent substitutions of variables." +
                "\r\n\r\nSum(f(x,y,k)/f(z,k), k , int a, int b)" +
                "\r\nProduct/Prod(f(x,y,k)/f(z,k), k , int a, int b)" +
                "\r\n\r\nIterate1(f(x,y,X,k), g(x,y), k , int a, int b)" +
                "\r\nIterate2(f_1(x,y,X,Y,k), f_2(x,y,X,Y,k), g_1(x,y), g_2(x,y), k , int a, int b, int choice)" +
                "\r\nIterate(f(z,Z,k), g(z), k , int a, int b)" +
                "\r\n// g's are the initial values and f's are the iterations." +
                "\r\n\r\nComposite1/Comp1(f(x,y), g_1(x,y,X), ...,g_n(x,y,X))" +
                "\r\nComposite2/Comp2(f_1(x,y), f_2(x,y), g_1(x,y,X,Y), h_1(x,y,X,Y), ..., g_n(x,y,X,Y), h_n(x,y,X,Y), int choice)" +
                "\r\nComposite/Comp(f(z), g_1(z,Z), ..., g_n(z,Z))" +
                "\r\n// f's are the initial values and g's are the compositions." +
                "\r\n\r\n********** PLANAR CURVES **********" +
                "\r\n\r\nFunc(f(x)) / Func(f(x), double increment) / Func(f(x), double a, double b) / Func(f(x), double a, double b, double increment)" +
                "\r\n\r\nPolar(f(θ), θ, double a, double b) / Polar(f(θ), θ, double a, double b, double increment)" +
                "\r\n\r\nParam(f(u), g(u), u, double a, double b) / Param(f(u), g(u), u, double a, double b, double increment)" +
                "\r\n\r\n********** RECURSIONS **********" +
                "\r\n// These methods should be combined with the former." +
                "\r\n\r\nLoop(Input(k), k , int a, int b)" +
                "\r\n\r\nIterateLoop(f(x,y,X,k), g(x,y), k, int a, int b) / " +
                "\r\nIterateLoop(f(z,Z,k), g(z), k, int a, int b, h(x,y,X))" +
                "\r\n// Displaying each step of iteration." +
                "\r\n\r\n...|...|...    // Displaying one by one." +
                "\r\n\r\n********** CONSTANTS **********" +
                "\r\n\r\nPi, e, Gamma/Ga" +
                "\r\n\r\n(The following are shortcuts for instant operations.)" +
                "\r\n\r\n********** SHORTCUTS **********" +
                "\r\n" +
                "\r\n[Control + P] Graph in the MicroBox;" +
                "\r\n[Control + G] Graph in the MacroBox;" +
                "\r\n[Control + B] Graph in both regions;" +
                "\r\n[Control + S] Save as a snapshot;" +
                "\r\n[Control + K] Save the history as a txt file;" +
                "\r\n[Control + Shift + C] Check all inputs;" +
                "\r\n[Control + R] Erase all checks;" +
                "\r\n[Control + D] Restore to the default state;" +
                "\r\n[Shift + Back] Clear the InputBox;" +
                "\r\n[Control + D2] See the profile of Fraljimetry;" +
                "\r\n[Contorl + D3] Clear all ReadOnly displays;" +
                "\r\n[Control + OemQuestion] See the manual;" +
                "\r\n[Oemtilde] Play or pause the ambient music;" +
                "\r\n[Delete] Clear both graphing regions;" +
                "\r\n[Escape] Close Fraljiculator;" +
                "\r\n\r\nClick [Tab] to witness the process of control design.", 600, 450);
        }
        private void PictureLogo_DoubleClick(object sender, EventArgs e)
        {
            FormalMessageBox.Show("Dear math lovers and mathematicians:" +
                "\r\n    Hi! I'm Fralji, a video uploader on Bilibili since July, 2021, right before entering college." +
                "\r\n    I aim to create unique lectures on many branches of mathematics. If you have any problem on the usage of this application, please contact me via one the following:" +
                "\r\n\r\nBilibili: 355884223" +
                "\r\n\r\nEmail: frankjiiiiiiii@gmail.com" +
                "\r\n\r\nWechat: F1r4a2n8k5y7 (recommended)" +
                "\r\n\r\nQQ: 472955101" +
                "\r\n\r\nFacebook: Fraljimetry" +
                "\r\n\r\nInstagram: shaodaji (NOT recommended)", 600, 450);
        }
        private static void ShowCustomMessageBox(string title, string message)
            => CustomMessageBox.Show(title + "\r\n\r\n" + message, 450, 300);
        private void GeneralLabel_DoubleClick(object sender, EventArgs e)
        {
            ShowCustomMessageBox("[GENERAL SCOPE]", "The detailed scope effectuates only if the general scope is set to zero." + "\r\n\r\n" + "Any legitimate variable-free algebraic expressions are acceptable in this box, and will be checked as in the input box.");
        }
        private void AtLabel_DoubleClick(object sender, EventArgs e)
        {
            ShowCustomMessageBox("[SAVING ADDRESS]", "You can create a file for snapshot storage and paste the address here." + "\r\n\r\n" + "The png snapshot will be named according to the current time.");
        }
        private void ModeLabel_DoubleClick(object sender, EventArgs e)
        {
            ShowCustomMessageBox("[COLORING MODES]", "The spectrum of colors represents the argument of meromorphic functions, the value of two-variable functions, or the parameterization of planar curves." + "\r\n\r\n" + "The first three modes have swappable colorations, while the last two do not.");
        }
        private void ContourLabel_DoubleClick(object sender, EventArgs e)
        {
            ShowCustomMessageBox("[CONTOUR MODES]", "Both options apply to the complex version only, for the contouring of meromorphic functions." + "\r\n\r\n" + "Only the Polar option admits translucent display, which represents the decay rate of modulus.");
        }
        private void ThickLabel_DoubleClick(object sender, EventArgs e)
        {
            ShowCustomMessageBox("[MAGNITUDE]", "This represents the width of curves, the size of special points, or the decay rate of translucence." + "\r\n\r\n" + "It should be appropriate according to the scale. The examples have been tweaked with much effort.");
        }
        private void InputLabel_DoubleClick(object sender, EventArgs e)
        {
            ShowCustomMessageBox("[FORMULA INPUT]", "Space and enter keys are both accepted. Unaccepted keys have been banned." + "\r\n\r\n" + "Try to use longer names for temporary parameters to avoid collapse of the interpreter.");
        }
        private void DraftLabel_DoubleClick(object sender, EventArgs e)
        {
            ShowCustomMessageBox("[HISTORY LIST]", "The input will be saved both here and in the clipboard." + "\r\n\r\n" + "The clicked points will also be recorded with detailed information, along with the time of snapshots and history storage.");
        }
        private void PreviewLabel_DoubleClick(object sender, EventArgs e)
        {
            ShowCustomMessageBox("[MICROCOSM]", "Since graphing cannot pause manually during the process, you may glimpse the result here." + "\r\n\r\n" + "It differs from the main graph only in size. It is estimated that graphing here is over 20 times faster.");
        }
        private void TimeLabel_DoubleClick(object sender, EventArgs e)
        {
            ShowCustomMessageBox("[DURATION]", "The auto snapshot cannot capture updates here on time, but it will be saved in the history list along with the pixels." + "\r\n\r\n" + "This value is precious as an embodiment of the input structure for future reference.");
        }
        private void ExampleLabel_DoubleClick(object sender, EventArgs e)
        {
            ShowCustomMessageBox("[EXAMPLES]", "These examples mainly inform you of the various types of legitimate grammar." + "\r\n\r\n" + "Some renderings are elegant while others are chaotic. Elegant graphs take time to explore: the essence of this app.");
        }
        private void FunctionLabel_DoubleClick(object sender, EventArgs e)
        {
            ShowCustomMessageBox("[FUNCTIONS]", "The two combo boxes contain regular and special operations respectively. The latter tends to have complicated grammar." + "\r\n\r\n" + "Select the content in the input box and choose here for substitution.");
        }
        private void PointNumLabel_DoubleClick(object sender, EventArgs e)
        {
            ShowCustomMessageBox("[PIXELS]", "This box logs the number of points or line segments throughout the previous loop, which is almost proportional to time and iteration." + "\r\n\r\n" + "Nullity often results from divergence or undefinedness.");
        }
        private void DenseLabel_DoubleClick(object sender, EventArgs e)
        {
            ShowCustomMessageBox("[DENSITY]", "It refers to the density of contours or the relative speed of planar curves with respect to parameterizations." + "\r\n\r\n" + "It should be appropriate according to the scale. The examples have been tweaked with much effort.");
        }
        private void DetailLabel_DoubleClick(object sender, EventArgs e)
        {
            ShowCustomMessageBox("[DETAILED SCOPE]", "You can even reverse the endpoints to create the mirror effect." + "\r\n\r\n" + "Any legitimate variable-free algebraic expressions are acceptable in this box, and will be checked as in the input box.");
        }
        private void ClearButton_Click(object sender, EventArgs e)
        {
            foreach (TextBox control in new[] { DraftBox, PointNumDisplay, TimeDisplay, X_CoorDisplay, Y_CoorDisplay, ModulusDisplay, AngleDisplay, FunctionDisplay, CaptionBox })
                control.Text = String.Empty;
            plot_loop = points_chosen = 0;
            drafted = false;
        }
        private void Delete_Click(int xLeft, int yUp, int xRight, int yDown, bool isMain)
        {
            SetTDSB(); // this is necessary
            ClearBitmap(bitmap, new Rectangle(xLeft, yUp, xRight - xLeft, yDown - yUp));
            Graphics g = CreateGraphics();
            DrawBackdrop(g, new(Color.Gray, 1), xLeft, yUp, xRight, yDown, new SolidBrush(Color.Black));
            if (CheckCoor.Checked) DrawAxesGrid(g, scopes, new int[] { xLeft, xRight, yUp, yDown });
            axes_drawn = isMain ? axes_drawn : CheckCoor.Checked;
            Axes_drawn = isMain ? CheckCoor.Checked : Axes_drawn;
        }
        private void DeleteMain_Click(object sender, EventArgs e)
            => Delete_Click(X_LEFT_MAC, Y_UP_MAC, X_RIGHT_MAC, Y_DOWN_MAC, true);
        private void DeletePreview_Click(object sender, EventArgs e)
            => Delete_Click(X_LEFT_MIC, Y_UP_MIC, X_RIGHT_MIC, Y_DOWN_MIC, false);
        private static void SetFontStyle(Label ctrl)
            => ctrl.ForeColor = ctrl.ForeColor == Color.White ? UNCHECKED_YELLOW : ctrl.ForeColor;
        private static void RecoverFontStyle(Label ctrl)
            => ctrl.ForeColor = ctrl.ForeColor == UNCHECKED_YELLOW ? Color.White : ctrl.ForeColor;
        private static void Input_MouseHover(Control input, Label label)
        {
            input.BackColor = label.ForeColor == Color.White ? FOCUS_GRAY : label.ForeColor;
            input.ForeColor = Color.Black;
            SetFontStyle(label);
        }
        private static void Input_MouseLeave(Control input, Label label)
        {
            input.BackColor = CONTROL_GRAY;
            input.ForeColor = Color.White;
            RecoverFontStyle(label);
        }
        private void GeneralInput_MouseHover(object sender, EventArgs e) => Input_MouseHover(GeneralInput, GeneralLabel);
        private void GeneralInput_MouseLeave(object sender, EventArgs e) => Input_MouseLeave(GeneralInput, GeneralLabel);
        private void X_Left_MouseHover(object sender, EventArgs e) => Input_MouseHover(X_Left, DetailLabel);
        private void X_Left_MouseLeave(object sender, EventArgs e) => Input_MouseLeave(X_Left, DetailLabel);
        private void X_Right_MouseHover(object sender, EventArgs e) => Input_MouseHover(X_Right, DetailLabel);
        private void X_Right_MouseLeave(object sender, EventArgs e) => Input_MouseLeave(X_Right, DetailLabel);
        private void Y_Left_MouseHover(object sender, EventArgs e) => Input_MouseHover(Y_Left, DetailLabel);
        private void Y_Left_MouseLeave(object sender, EventArgs e) => Input_MouseLeave(Y_Left, DetailLabel);
        private void Y_Right_MouseHover(object sender, EventArgs e) => Input_MouseHover(Y_Right, DetailLabel);
        private void Y_Right_MouseLeave(object sender, EventArgs e) => Input_MouseLeave(Y_Right, DetailLabel);
        private void ThickInput_MouseHover(object sender, EventArgs e) => Input_MouseHover(ThickInput, ThickLabel);
        private void ThickInput_MouseLeave(object sender, EventArgs e) => Input_MouseLeave(ThickInput, ThickLabel);
        private void DenseInput_MouseHover(object sender, EventArgs e) => Input_MouseHover(DenseInput, DenseLabel);
        private void DenseInput_MouseLeave(object sender, EventArgs e) => Input_MouseLeave(DenseInput, DenseLabel);
        private void AddressInput_MouseHover(object sender, EventArgs e) => Input_MouseHover(AddressInput, AtLabel);
        private void AddressInput_MouseLeave(object sender, EventArgs e) => Input_MouseLeave(AddressInput, AtLabel);
        private void DraftBox_MouseHover(object sender, EventArgs e)
        {
            if (!DraftBox.ReadOnly)
            {
                DraftBox.BackColor = FOCUS_GRAY;
                DraftBox.ForeColor = Color.Black;
                toolTip_ReadOnly.SetToolTip(DraftBox, String.Empty);
                SetFontStyle(DraftLabel);
            }
            else
            {
                toolTip_ReadOnly.SetToolTip(DraftBox, "ReadOnly");
                DraftLabel.ForeColor = READONLY_PURPLE;
                DraftBox.ForeColor = Color.White;
            }
        }
        private void DraftBox_MouseLeave(object sender, EventArgs e)
        {
            if (!DraftBox.ReadOnly)
            {
                DraftBox.BackColor = CONTROL_GRAY;
                DraftBox.ForeColor = Color.White;
            }
            else DraftBox.ForeColor = READONLY_GRAY;
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
        private void InputString_TextChanged(object sender, EventArgs e)
        {
            if (!AllButton.Enabled) return;
            is_checking = text_changed = true;
            AutoCheckComplex();
            CheckValidity();
        }
        private void InputString_DoubleClick(object sender, EventArgs e) => InputString_TextChanged(sender, e);
        private void AddressInput_TextChanged(object sender, EventArgs e)
        {
            if (!AllButton.Enabled) return;
            if (AddressInput.Text == String.Empty) AtLabel.ForeColor = Color.White;
            else AtLabel.ForeColor = Directory.Exists(AddressInput.Text) ? CORRECT_GREEN : ERROR_RED;
        }
        private void AddressInput_DoubleClick(object sender, EventArgs e) => AddressInput_TextChanged(sender, e);
        private void GeneralInput_TextChanged(object sender, EventArgs e) => MiniChecks(GeneralInput, GeneralLabel);
        private void GeneralInput_DoubleClick(object sender, EventArgs e) => GeneralInput_TextChanged(sender, e);
        private void ColorOfDetails()
        {
            if (X_Scope.ForeColor == CORRECT_GREEN && Y_Scope.ForeColor == CORRECT_GREEN)
                DetailLabel.ForeColor = CORRECT_GREEN;
            if (X_Scope.ForeColor == ERROR_RED || Y_Scope.ForeColor == ERROR_RED)
                DetailLabel.ForeColor = ERROR_RED;
        }
        private void X_Left_TextChanged(object sender, EventArgs e) { MiniChecks(X_Left, X_Scope); ColorOfDetails(); }
        private void X_Left_DoubleClick(object sender, EventArgs e) => X_Left_TextChanged(sender, e);
        private void X_Right_TextChanged(object sender, EventArgs e) { MiniChecks(X_Right, X_Scope); ColorOfDetails(); }
        private void X_Right_DoubleClick(object sender, EventArgs e) => X_Right_TextChanged(sender, e);
        private void Y_Left_TextChanged(object sender, EventArgs e) { MiniChecks(Y_Left, Y_Scope); ColorOfDetails(); }
        private void Y_Left_DoubleClick(object sender, EventArgs e) => Y_Left_TextChanged(sender, e);
        private void Y_Right_TextChanged(object sender, EventArgs e) { MiniChecks(Y_Right, Y_Scope); ColorOfDetails(); }
        private void Y_Right_DoubleClick(object sender, EventArgs e) => Y_Right_TextChanged(sender, e);
        private void ThickInput_TextChanged(object sender, EventArgs e) => MiniChecks(ThickInput, ThickLabel);
        private void ThickInput_DoubleClick(object sender, EventArgs e) => ThickInput_TextChanged(sender, e);
        private void DenseInput_TextChanged(object sender, EventArgs e) => MiniChecks(DenseInput, DenseLabel);
        private void DenseInput_DoubleClick(object sender, EventArgs e) => DenseInput_TextChanged(sender, e);
        private static void BanDoubleClick(TextBox ctrl, MouseEventArgs e)
        { ctrl.SelectionStart = ctrl.GetCharIndexFromPosition(e.Location); ctrl.SelectionLength = 0; }
        private void InputString_MouseDoubleClick(object sender, MouseEventArgs e) => BanDoubleClick(InputString, e);
        private void GeneralInput_MouseDoubleClick(object sender, MouseEventArgs e) => BanDoubleClick(GeneralInput, e);
        private void X_Left_MouseDoubleClick(object sender, MouseEventArgs e) => BanDoubleClick(X_Left, e);
        private void X_Right_MouseDoubleClick(object sender, MouseEventArgs e) => BanDoubleClick(X_Right, e);
        private void Y_Left_MouseDoubleClick(object sender, MouseEventArgs e) => BanDoubleClick(Y_Left, e);
        private void Y_Right_MouseDoubleClick(object sender, MouseEventArgs e) => BanDoubleClick(Y_Right, e);
        private void ThickInput_MouseDoubleClick(object sender, MouseEventArgs e) => BanDoubleClick(ThickInput, e);
        private void DenseInput_MouseDoubleClick(object sender, MouseEventArgs e) => BanDoubleClick(DenseInput, e);
        private void AddressInput_MouseDoubleClick(object sender, MouseEventArgs e) => BanDoubleClick(AddressInput, e);
        private void InputString_KeyDown(object sender, KeyEventArgs e) => AutoKeyDown(InputString, e);
        private void InputString_KeyPress(object sender, KeyPressEventArgs e) => BarSomeKeys(sender, e);
        private void GeneralInput_KeyDown(object sender, KeyEventArgs e) => AutoKeyDown(GeneralInput, e);
        private void GeneralInput_KeyPress(object sender, KeyPressEventArgs e) => BarSomeKeys(sender, e);
        private void X_Left_KeyDown(object sender, KeyEventArgs e) => AutoKeyDown(X_Left, e);
        private void X_Left_KeyPress(object sender, KeyPressEventArgs e) => BarSomeKeys(sender, e);
        private void X_Right_KeyDown(object sender, KeyEventArgs e) => AutoKeyDown(X_Right, e);
        private void X_Right_KeyPress(object sender, KeyPressEventArgs e) => BarSomeKeys(sender, e);
        private void Y_Left_KeyDown(object sender, KeyEventArgs e) => AutoKeyDown(Y_Left, e);
        private void Y_Left_KeyPress(object sender, KeyPressEventArgs e) => BarSomeKeys(sender, e);
        private void Y_Right_KeyDown(object sender, KeyEventArgs e) => AutoKeyDown(Y_Right, e);
        private void Y_Right_KeyPress(object sender, KeyPressEventArgs e) => BarSomeKeys(sender, e);
        private void ThickInput_KeyDown(object sender, KeyEventArgs e) => AutoKeyDown(ThickInput, e);
        private void ThickInput_KeyPress(object sender, KeyPressEventArgs e) => BarSomeKeys(sender, e);
        private void DenseInput_KeyDown(object sender, KeyEventArgs e) => AutoKeyDown(DenseInput, e);
        private void DenseInput_KeyPress(object sender, KeyPressEventArgs e) => BarSomeKeys(sender, e);
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
        private void CheckEdit_MouseHover(object sender, EventArgs e) => CheckEdit.ForeColor = COMBO_BLUE;
        private void CheckEdit_MouseLeave(object sender, EventArgs e) => CheckEdit.ForeColor = Color.White;
        private void CheckAuto_MouseHover(object sender, EventArgs e) => CheckAuto.ForeColor = COMBO_BLUE;
        private void CheckAuto_MouseLeave(object sender, EventArgs e) => CheckAuto.ForeColor = Color.White;
        private void SubtitleBox_DoubleClick(object sender, EventArgs e)
        { if (!AllButton.Enabled) return; Clipboard.SetText(InputString.Text); InputString.Text = String.Empty; }
        private void SubtitleBox_MouseHover(object sender, EventArgs e) => SubtitleBox.ForeColor = ERROR_RED;
        private void SubtitleBox_MouseLeave(object sender, EventArgs e) => SubtitleBox.ForeColor = Color.White;
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
        private void InputString_MouseHover(object sender, EventArgs e) => SetFontStyle(InputLabel);
        private void InputString_MouseLeave(object sender, EventArgs e) => RecoverFontStyle(InputLabel);
        private void VScrollBarX_MouseHover(object sender, EventArgs e) => X_Bar.ForeColor = READONLY_PURPLE;
        private void VScrollBarX_MouseLeave(object sender, EventArgs e) => X_Bar.ForeColor = Color.White;
        private void VScrollBarY_MouseHover(object sender, EventArgs e) => Y_Bar.ForeColor = READONLY_PURPLE;
        private void VScrollBarY_MouseLeave(object sender, EventArgs e) => Y_Bar.ForeColor = Color.White;
        private void CaptionBox_MouseHover(object sender, EventArgs e) => CaptionBox.ForeColor = Color.White;
        private void CaptionBox_MouseLeave(object sender, EventArgs e) => CaptionBox.ForeColor = READONLY_GRAY;
        private void PictureLogo_MouseHover(object sender, EventArgs e) => EnlargePicture(PictureLogo, 5);
        private void PictureLogo_MouseLeave(object sender, EventArgs e) => ShrinkPicture(PictureLogo, 5);
        private void PreviewLabel_MouseHover(object sender, EventArgs e) => PreviewLabel.ForeColor = READONLY_PURPLE;
        private void PreviewLabel_MouseLeave(object sender, EventArgs e) => PreviewLabel.ForeColor = Color.White;
        private void X_Bar_MouseHover(object sender, EventArgs e) => X_Bar.ForeColor = READONLY_PURPLE;
        private void X_Bar_MouseLeave(object sender, EventArgs e) => X_Bar.ForeColor = Color.White;
        private void Y_Bar_MouseHover(object sender, EventArgs e) => Y_Bar.ForeColor = READONLY_PURPLE;
        private void Y_Bar_MouseLeave(object sender, EventArgs e) => Y_Bar.ForeColor = Color.White;
        private void CaptionBox_MouseDown(object sender, MouseEventArgs e) => HideCaret(CaptionBox.Handle);
        private void PointNumDisplay_MouseDown(object sender, MouseEventArgs e) => HideCaret(PointNumDisplay.Handle);
        private void TimeDisplay_MouseDown(object sender, MouseEventArgs e) => HideCaret(TimeDisplay.Handle);
        private void X_CoorDisplay_MouseDown(object sender, MouseEventArgs e) => HideCaret(X_CoorDisplay.Handle);
        private void Y_CoorDisplay_MouseDown(object sender, MouseEventArgs e) => HideCaret(Y_CoorDisplay.Handle);
        private void ModulusDisplay_MouseDown(object sender, MouseEventArgs e) => HideCaret(ModulusDisplay.Handle);
        private void AngleDisplay_MouseDown(object sender, MouseEventArgs e) => HideCaret(AngleDisplay.Handle);
        private void FunctionDisplay_MouseDown(object sender, MouseEventArgs e) => HideCaret(FunctionDisplay.Handle);
        private void DraftBox_MouseDown(object sender, MouseEventArgs e) { if (DraftBox.ReadOnly) HideCaret(DraftBox.Handle); }
        private void PicturePlay_Click(object sender, EventArgs e) => PlayOrPause();
        private void PicturePlay_MouseHover(object sender, EventArgs e) => EnlargePicture(PicturePlay, 2);
        private void PicturePlay_MouseLeave(object sender, EventArgs e) => ShrinkPicture(PicturePlay, 2);
        private void PictureIncorrect_Click(object sender, EventArgs e) { if (AllButton.Enabled) CheckValidityDetailed(); }
        private void PictureIncorrect_MouseHover(object sender, EventArgs e) => EnlargePicture(PictureIncorrect, 2);
        private void PictureIncorrect_MouseLeave(object sender, EventArgs e) => ShrinkPicture(PictureIncorrect, 2);
        private void ExportButton_MouseHover(object sender, EventArgs e) => AddressInput_DoubleClick(sender, e);
        private void StoreButton_MouseHover(object sender, EventArgs e) => AddressInput_DoubleClick(sender, e);
        private static void EnlargePicture(Control ctrl, int increment)
        {
            if (is_resized) return;
            ctrl.Location = new Point(ctrl.Location.X - increment, ctrl.Location.Y - increment);
            ctrl.Size = new Size(ctrl.Width + 2 * increment, ctrl.Height + 2 * increment);
            is_resized = true;
        }
        private static void ShrinkPicture(Control ctrl, int decrement)
        {
            if (!is_resized) return;
            ctrl.Location = new Point(ctrl.Location.X + decrement, ctrl.Location.Y + decrement);
            ctrl.Size = new Size(ctrl.Width - 2 * decrement, ctrl.Height - 2 * decrement);
            is_resized = false;
        }
        private void Combo_KeyDown(object sender, KeyEventArgs e)
            => e.SuppressKeyPress = e.Control && e.KeyCode >= Keys.A && e.KeyCode <= Keys.Z;
        private void ComboColoring_KeyDown(object sender, KeyEventArgs e) => Combo_KeyDown(sender, e);
        private void ComboContour_KeyDown(object sender, KeyEventArgs e) => Combo_KeyDown(sender, e);
        private void ComboExamples_KeyDown(object sender, KeyEventArgs e) => Combo_KeyDown(sender, e);
        private void ComboFunctions_KeyDown(object sender, KeyEventArgs e) => Combo_KeyDown(sender, e);
        private void ComboSpecial_KeyDown(object sender, KeyEventArgs e) => Combo_KeyDown(sender, e);
        #endregion
    }
    public partial class BaseMessageBox : Form
    {
        protected static readonly Color BACKDROP_GRAY = Color.FromArgb(64, 64, 64);
        public static TextBox txtMessage;
        public static Button btnOk;
        [DllImport("user32.dll")]
        static extern bool HideCaret(IntPtr hWnd);
        private static bool isBtnOkResized = false;
        protected float originalFontSize, ScalingFactor;
        protected void ReduceFontSizeByScale(Control parent)
        {
            foreach (Control ctrl in parent.Controls)
            {
                Font currentFont = ctrl.Font;
                float newFontSize = currentFont.Size / ScalingFactor;
                ctrl.Font = new Font(currentFont.FontFamily, newFontSize, currentFont.Style);
                if (ctrl.Controls.Count > 0) ReduceFontSizeByScale(ctrl);
            }
        }
        protected void BtnOk_MouseEnter(object sender, EventArgs e)
        {
            if (sender is Button btn && !isBtnOkResized)
            {
                btn.Size = new Size(btn.Width + 2, btn.Height + 2);
                btn.Location = new Point(btn.Location.X - 1, btn.Location.Y - 1);
                originalFontSize = btn.Font.Size;
                btn.Font = new Font(btn.Font.FontFamily, originalFontSize + 1f, btn.Font.Style);
                isBtnOkResized = true;
            }
        }
        protected void BtnOk_MouseLeave(object sender, EventArgs e)
        {
            if (sender is Button btn && isBtnOkResized)
            {
                btn.Size = new Size(btn.Width - 2, btn.Height - 2);
                btn.Location = new Point(btn.Location.X + 1, btn.Location.Y + 1);
                btn.Font = new Font(btn.Font.FontFamily, originalFontSize, btn.Font.Style);
                isBtnOkResized = false;
            }
        }
        protected static void TxtMessage_MouseDown(object sender, MouseEventArgs e)
        { if (txtMessage != null && txtMessage.Handle != IntPtr.Zero) HideCaret(txtMessage.Handle); }
        protected void Form_KeyDown(object sender, KeyEventArgs e) { if (e.KeyCode == Keys.Enter) Close(); }
        protected void SetUpForm(int width, int height)
        {
            FormBorderStyle = FormBorderStyle.None; TopMost = true; Size = new Size(width, height);
            StartPosition = FormStartPosition.CenterScreen; BackColor = SystemColors.ControlDark;
        }
        protected static void SetUpTextBox(int border, string message, int width, int height, Color textColor)
        {
            txtMessage = new TextBox
            {
                Text = message,
                Font = new Font("Microsoft YaHei UI", 10, FontStyle.Regular),
                ForeColor = textColor,
                Multiline = true,
                ReadOnly = true,
                BorderStyle = BorderStyle.None,
                BackColor = BACKDROP_GRAY,
                ScrollBars = ScrollBars.Vertical
            };
            txtMessage.SetBounds(10, 10, width - 20, height - border * 2 - 10);
            txtMessage.SelectionStart = message.Length; txtMessage.SelectionLength = 0;
            txtMessage.MouseDown += TxtMessage_MouseDown;
        }
        protected void SetUpButton(int border, int width, int height, Color buttonColor, Color buttonTextColor)
        {
            btnOk = new()
            {
                Size = new Size(50, 25),
                Location = new Point(width / 2 - 25, height - border / 2 - 25),
                BackColor = buttonColor,
                ForeColor = buttonTextColor,
                Font = new Font("Microsoft YaHei UI", 7, FontStyle.Bold),
                FlatStyle = FlatStyle.Flat,
                Text = "OK",
            };
            btnOk.FlatAppearance.BorderSize = 0; btnOk.Click += (sender, e) => { Close(); };
            btnOk.MouseEnter += BtnOk_MouseEnter; btnOk.MouseLeave += BtnOk_MouseLeave;
        }
        protected void Setup(string message, int width, int height, Color textColor, Color buttonColor, Color buttonTextColor)
        {
            int border = 23;
            SetUpForm(width, height);
            SetUpTextBox(border, message, width, height, textColor);
            SetUpButton(border, width, height, buttonColor, buttonTextColor);
            Controls.Add(txtMessage); Controls.Add(btnOk);
            ScalingFactor = Graphics.FromHwnd(IntPtr.Zero).DpiX / 96f / 1.5f;
            ReduceFontSizeByScale(this);
            KeyPreview = true; KeyDown += new KeyEventHandler(Form_KeyDown);
            Load += (sender, e) => { HideCaret(txtMessage.Handle); };
        }
        public static void Display(string message, int width, int height, Color textColor, Color buttonColor, Color buttonTextColor)
        {
            BaseMessageBox box = new();
            box.Setup(message, width, height, textColor, buttonColor, buttonTextColor);
            box.ShowDialog();
        }
    }
    public class FormalMessageBox : BaseMessageBox
    {
        public static void Show(string message, int width, int height)
            => Display(message, width, height, Color.FromArgb(224, 224, 224), Color.Black, Color.White);
    } // Title & Profile
    public class CustomMessageBox : BaseMessageBox
    {
        public static void Show(string message, int width, int height)
            => Display(message, width, height, Color.Turquoise, Color.DarkBlue, Color.White);
    } // Instructions
    public class ExceptionMessageBox : BaseMessageBox
    {
        public static void Show(string message, int width, int height)
            => Display(message, width, height, Color.LightPink, Color.DarkRed, Color.White);
    } // Exceptions
    public class MyString
    {
        public static int CountChar(ReadOnlySpan<char> input, char c)
        {
            int count = 0;
            foreach (char ch in input) if (ch == c) count++;
            return count;
        }
        public static int PairedParenthesis(ReadOnlySpan<char> input, int n)
        {
            if (input[n] != '(') throw new FormatException();
            int countBracket = 1;
            for (int i = n + 1; i < input.Length; i++)
            {
                char currentChar = input[i];
                if (currentChar == '(') countBracket++; else if (currentChar == ')') countBracket--;
                if (countBracket == 0) return i;
            }
            throw new FormatException();
        }
        public static bool ParenthesisCheck(ReadOnlySpan<char> input, char c1, char c2)
        {
            int sum = 0;
            foreach (char c in input)
            {
                if (c == c1) sum++; else if (c == c2) sum--;
                if (sum < 0) return false;
            }
            return sum == 0;
        }
        public static int FindOpeningParenthesis(ReadOnlySpan<char> input, int n)
        {
            int count = 0;
            for (int i = 0; i < input.Length; i++) if (input[i] == '(' && ++count == n) return i;
            throw new FormatException();
        }
        public static string ConstructSubstitution(int n) => String.Concat("[", n.ToString(), "]");
        public static string Extract(string input, int begin, int end) => input.AsSpan(begin, end - begin + 1).ToString();
        public static string Replace(string original, string replacement, int begin, int end)
        {
            int resultLength = begin + replacement.Length + (original.Length - end - 1);
            return String.Create(resultLength, (original, replacement, begin, end), (span, state) =>
            {
                (string orig, string repl, int b, int e) = state;
                orig.AsSpan(0, b).CopyTo(span); // Copy the beginning
                repl.AsSpan().CopyTo(span[b..]); // Copy the replacement
                orig.AsSpan(e + 1).CopyTo(span[(b + repl.Length)..]); // Copy the remaining
            });
        }
        private static string ReplaceInterior(string input, char c, char replacement)
        {
            if (!input.Contains('_')) return input;
            StringBuilder result = new(input);
            for (int i = 0; i < result.Length; i++)
            {
                if (result[i] != '_') continue;
                int endIndex = PairedParenthesis(input, i + 1);
                for (int j = i + 1; j < endIndex; j++)
                {
                    if (result[j] != c) continue;
                    result.Remove(j, 1).Insert(j, replacement);
                }
                i = endIndex;
            }
            return result.ToString();
        }
        public static string[] ReplaceRecover(string input)
            => SplitByChars(ReplaceInterior(input, ',', ';'), new char[] { ',' }).Select(part => part.Replace(';', ',')).ToArray();
        public static string[] SplitString(string input)
            => ReplaceRecover(Extract(input, input.IndexOf('(') + 1, PairedParenthesis(input, input.IndexOf('(')) - 1));
        public static string[] SplitByChars(string input, char[] delimiters)
        {
            List<string> segments = new();
            StringBuilder currentSegment = new();
            HashSet<char> delimiterSet = new(delimiters);
            for (int i = 0; i < input.Length; i++)
            {
                if (delimiterSet.Contains(input[i]))
                {
                    segments.Add(currentSegment.ToString());
                    currentSegment.Clear();
                }
                else currentSegment.Append(input[i]);
            }
            segments.Add(currentSegment.ToString());
            return segments.ToArray();
        }
        public static string TrimStartChars(string input, char[] charsToTrim)
        {
            int startIndex = 0;
            HashSet<char> trimSet = new(charsToTrim);
            while (startIndex < input.Length && trimSet.Contains(input[startIndex]))
                startIndex++;
            if (startIndex == input.Length) return String.Empty;
            StringBuilder result = new(input.Length - startIndex);
            result.Append(input, startIndex, input.Length - startIndex);
            return result.ToString();
        }
        public static bool ContainsAny(string input, char[] charsToCheck)
        {
            HashSet<char> charSet = new(charsToCheck);
            foreach (char c in input) if (charSet.Contains(c)) return true;
            return false;
        }
        public static bool ContainsAny(string input, string[] stringsToCheck)
        {
            foreach (string str in stringsToCheck) if (input.Contains(str)) return true;
            return false;
        }
        public static string ReplaceSubstrings(string input, List<string> substrings, string replacement)
            => Regex.Replace(input, String.Join("|", substrings), replacement);
        public static string ReplaceTagReal(string input)
        {
            Dictionary<string, string> replacements = new()
            {
                { "Floor", "~f$" }, { "floor", "~f$" },
                { "Ceil", "~c$" }, { "ceil", "~c$" },
                { "Round", "~r$" }, { "round", "~r$" },
                { "Sign", "~s$" }, { "sign", "~s$" }, { "Sgn", "~s$" }, { "sgn", "~s$" },
                { "Mod", "~M_" }, { "mod", "~M_" },
                { "nCr", "~C_" }, { "nPr", "~A_" },
                { "Max", "~>_" }, { "max", "~>_" }, { "Min", "~<_" }, { "min", "~<_" },
                { "Iterate1", "~1I_" }, { "iterate1", "~1I_" }, { "Iterate2", "~2I_" }, { "iterate2", "~2I_" },
                { "Composite1", "~1J_" }, { "composite1", "~1J_" }, { "Composite2", "~2J_" }, { "composite2", "~2J_" },
                { "Comp1", "~1J_" }, { "comp1", "~1J_" }, { "Comp2", "~2J_" }, { "comp2", "~2J_" }
            };
            foreach (var pair in replacements) input = input.Replace(pair.Key, pair.Value);
            return ReplaceTagCommon(input);
        }
        public static string ReplaceTagComplex(string input)
        {
            Dictionary<string, string> replacements = new()
            {
                { "Iterate(", "~I_(" }, { "iterate(", "~I_(" },
                { "Composite", "~J_" }, { "composite", "~J_" }, { "Comp", "~J_" }, { "comp", "~J_" },
                { "conjugate", "~J" }, { "Conjugate", "~J" }, { "conj", "~J" }, { "Conj", "~J" },
                { "e(", "~E#(" }
            };
            foreach (var replacement in replacements) input = input.Replace(replacement.Key, replacement.Value);
            return ReplaceTagCommon(input);
        }
        public static string ReplaceTagCommon(string input)
        {
            Dictionary<string, string> replacements = new()
            {
                { "Product", "~P_" }, { "product", "~P_" }, { "Prod", "~P_" }, { "prod", "~P_" },
                { "Sum", "~S_" }, { "sum", "~S_" },
                { "F(", "~F_(" },
                { "Gamma(", "~G_(" }, { "gamma(", "~G_(" }, { "Ga(", "~G_(" }, { "ga(", "~G_(" },
                { "Beta", "~B_" }, { "beta", "~B_" },
                { "Zeta", "~Z_" }, { "zeta", "~Z_" },
                { "log", "~l" }, { "Log", "~l" }, { "ln", "~l" }, { "Ln", "~l" },
                { "exp", "~E" }, { "Exp", "~E" },
                { "sqrt", "~q" }, { "Sqrt", "~q" },
                { "abs", "~a" }, { "Abs", "~a" },
                { "factorial", "~!" }, { "Factorial", "~!" }, { "fact", "~!" }, { "Fact", "~!" },
                { "pi", "p" }, { "Pi", "p" },
                { "gamma", "g" }, { "Gamma", "g" }, { "ga", "g" }, { "Ga", "g" },
                { "arcsinh", "~ash" }, { "Arcsinh", "~ash" }, { "arcsh", "~ash" }, { "Arcsh", "~ash" }, { "arsinh", "~ash" }, { "Arsinh", "~ash" }, { "arsh", "~ash" }, { "Arsh", "~ash" },
                { "arccosh", "~ach" }, { "Arccosh", "~ach" }, { "arcch", "~ach" }, { "Arcch", "~ach" }, { "arcosh", "~ach" }, { "Arcosh", "~ach" }, { "arch", "~ach" }, { "Arch", "~ach" },
                { "arctanh", "~ath" }, { "Arctanh", "~ath" }, { "arcth", "~ath" }, { "Arcth", "~ath" }, { "artanh", "~ath" }, { "Artanh", "~ath" }, { "arth", "~ath" }, { "Arth", "~ath" },
                { "arcsin", "~as" }, { "Arcsin", "~as" }, { "asin", "~as" }, { "Asin", "~as" },
                { "arccos", "~ac" }, { "Arccos", "~ac" }, { "acos", "~ac" }, { "Acos", "~ac" },
                { "arctan", "~at" }, { "Arctan", "~at" }, { "atan", "~at" }, { "Atan", "~at" },
                { "sinh", "~sh" }, { "Sinh", "~sh" },
                { "cosh", "~ch" }, { "Cosh", "~ch" },
                { "tanh", "~th" }, { "Tanh", "~th" },
                { "sin", "~s" }, { "Sin", "~s" },
                { "cos", "~c" }, { "Cos", "~c" },
                { "tan", "~t" }, { "Tan", "~t" }
            };
            foreach (var replacement in replacements) input = input.Replace(replacement.Key, replacement.Value);
            return input;
        }
        public static string ReplaceTagCurves(string input)
        {
            Dictionary<string, string> replacements = new()
            {
                { "func", "α_" }, { "Func", "α_" },
                { "polar", "β_" }, { "Polar", "β_" },
                { "param", "γ_" }, { "Param", "γ_" },
                { "iterateLoop", "δ_" }, { "IterateLoop", "δ_" }
            };
            foreach (var replacement in replacements) input = input.Replace(replacement.Key, replacement.Value);
            return input;
        }
        public static bool ContainFunctionName(string input)
            => ContainsAny(input, new string[] { "Func", "func", "Polar", "polar", "Param", "param" });
        public static string TrimLargeDouble(double input, double threshold)
            => Math.Abs(input) < threshold ? input.ToString("#0.000000") : input.ToString("E3");
        public static int ToInt(string input) => (int)RealSubstitution.ObtainValue(input);
    }
    public class RecoverMultiplication
    {
        public static string BeautifyInput(string input, bool isComplex)
        {
            if (MyString.CountChar(input, '(') != MyString.CountChar(input, ')') || input.Contains("()")
                || MyString.ContainsAny(input, "_#!<>$%&@~:\'\"\\?=`[]{}\t".ToCharArray())) throw new FormatException();
            input = MyString.ReplaceSubstrings(input, new List<string> { "\n", "\r", " " }, String.Empty);
            return isComplex ? MyString.ReplaceTagComplex(input) : MyString.ReplaceTagReal(input);
        }
        public static string Recover(string input, bool isComplex)
        {
            if (input.Length == 1) return input;
            int j = 0; string temp = input;
            for (int i = 0; i < input.Length - 1; i++)
                if (AddOrNot(input[i], input[i + 1], isComplex)) { j++; temp = temp.Insert(i + j, "*"); }
            return temp;
        }
        private static bool AddOrNot(char c1, char c2, bool isComplex)
        {
            bool b1 = (IsConst(c1) || Char.IsNumber(c1)) && (IsConst(c2) || IsVar(c2, isComplex));
            bool b2 = (IsConst(c1) || IsVar(c1, isComplex)) && (IsConst(c2) || Char.IsNumber(c2));
            bool b3 = IsVar(c1, isComplex) && IsVar(c2, isComplex);
            bool b4 = (Char.IsNumber(c1) || IsConst(c1) || IsVar(c1, isComplex)) && IsOpen(c2);
            bool b5 = IsClose(c1) && (Char.IsNumber(c2) || IsConst(c2) || IsVar(c2, isComplex));
            bool b6 = IsClose(c1) && IsOpen(c2);
            bool b7 = !IsArithmetic(c1) && IsFunctionHead(c2);
            return b1 || b2 || b3 || b4 || b5 || b6 || b7;
        }
        private static bool IsVar(char c, bool isComplex) => isComplex ? IsVarComplex(c) : IsVarReal(c);
        private static bool IsVarReal(char c) => "xyXY".Contains(c);
        private static bool IsVarComplex(char c) => "zZi".Contains(c);
        private static bool IsConst(char c) => "epg".Contains(c);
        private static bool IsArithmetic(char c) => "+-*/^(,|".Contains(c);
        private static bool IsOpen(char c) => c == '(';
        private static bool IsClose(char c) => c == ')';
        private static bool IsFunctionHead(char c) => c == '~';
    }
    public class RealSubstitution
    {
        private const double GAMMA = 0.57721566490153286060651209008240243;
        private const int THRESHOLD = 10;
        private const int STRUCTSIZE = 8; // Size of double
        private string input;
        private int row, column, columnSIZE, count = 0;
        private double[,] x, y, X, Y;
        private double[,,] bracketed_values;
        #region Constructors
        private void Initialize(string input, int row, int column)
        {
            if (String.IsNullOrEmpty(input)) throw new FormatException();
            this.input = RecoverMultiplication.Recover(input, false);
            bracketed_values = new double[MyString.CountChar(input, '(') + 1, row, column];
            x = y = X = Y = new double[row, column];
            this.row = row; this.column = column; columnSIZE = column * STRUCTSIZE;
        }
        private void PopulateX(double[,] x, double[,] y) { this.x = x; if (y != null) this.y = y; }
        private void PopulateXNew(double[,] X, double[,] Y) { this.X = X; this.Y = Y; }
        public RealSubstitution(string input, double x = 0, double y = 0, double X = 0, double Y = 0)
        {
            Initialize(input, 1, 1);
            PopulateX(new double[,] { { x } }, new double[,] { { y } });
            PopulateXNew(new double[,] { { X } }, new double[,] { { Y } });
        }
        public RealSubstitution(string input, int row, int column) => Initialize(input, row, column);
        public RealSubstitution(string input, double[,] x, int row, int column) : this(input, row, column) => PopulateX(x, null);
        public RealSubstitution(string input, double[,] x, double[,] y, int row, int column) : this(input, row, column) => PopulateX(x, y);
        public RealSubstitution(string input, double[,] x, double[,] y, double[,] X, double[,] Y, int row, int column)
            : this(input, row, column) { PopulateX(x, y); PopulateXNew(X, Y); }
        #endregion
        #region Basic Calculations
        private static double MySign(double value) => value == 0 ? 0 : (value > 0 ? 1 : -1);
        public static double Factorial(double n)
            => n < 0 ? Double.NaN : Math.Floor(n) == 0 ? 1 : Math.Floor(n) * Factorial(n - 1);
        private static double Mod(double a, double n) => (n != 0) ? a % Math.Abs(n) : Double.NaN;
        private static double Combination(double n, double r)
        {
            if (n == r || r == 0) return 1;
            else if (r > n && n >= 0 || 0 > r && r > n || n >= 0 && 0 > r) return 0;
            else if (n > 0) return Combination(n - 1, r - 1) + Combination(n - 1, r);
            else if (r > 0) return Combination(n + 1, r) - Combination(n, r - 1);
            else return Combination(n + 1, r + 1) - Combination(n, r + 1);
        }
        private static double Permutation(double n, double r)
        {
            if (r < 0) return 0;
            else if (r == 0) return 1;
            else return (n - r + 1) * Permutation(n, r - 1);
        }
        private unsafe double[,] ProcessMCP(string input, Func<double, double, double> operation)
        {
            string[] split = MyString.ReplaceRecover(input);
            if (split.Length != 2) throw new FormatException();
            double[,] temp_1 = new RealSubstitution(split[0], x, y, X, Y, row, column).ObtainValues();
            double[,] temp_2 = new RealSubstitution(split[1], x, y, X, Y, row, column).ObtainValues();
            double[,] output = new double[row, column];
            Parallel.For(0, row, r => {
                fixed (double* temp1Ptr = &temp_1[r, 0], temp2Ptr = &temp_2[r, 0], outPtr = &output[r, 0])
                { for (int c = 0; c < column; c++) outPtr[c] = operation(temp1Ptr[c], temp2Ptr[c]); }
            });
            return output;
        }
        private double[,] Mod(string input) => ProcessMCP(input, (a, b) => Mod(a, b));
        private double[,] Combination(string input)
            => ProcessMCP(input, (a, b) => Combination(Math.Floor(a), Math.Floor(b)));
        private double[,] Permutation(string input)
            => ProcessMCP(input, (a, b) => Permutation(Math.Floor(a), Math.Floor(b)));
        private static double Max(double[] input)
        {
            if (input.Length == 1) return input[0];
            return Math.Max(input[0], Max(input.Skip(1).ToArray()));
        }
        private static double Min(double[] input)
        {
            if (input.Length == 1) return input[0];
            return Math.Min(input[0], Min(input.Skip(1).ToArray()));
        }
        private unsafe double[,] ProcessMinMax(string input, Func<double[], double> operation)
        {
            string[] split = MyString.ReplaceRecover(input);
            double[,,] output = new double[split.Length, row, column];
            double[,] Output = new double[row, column];
            for (int i = 0; i < split.Length; i++)
            {
                double[,] temp = new RealSubstitution(split[i], x, y, X, Y, row, column).ObtainValues();
                Parallel.For(0, row, r => {
                    fixed (double* outPtr = &output[i, r, 0], tempPtr = &temp[r, 0])
                    { Buffer.MemoryCopy(tempPtr, outPtr, columnSIZE, columnSIZE); }
                });
            }
            Parallel.For(0, row, () => new double[split.Length], (r, state, values) =>
            {
                for (int c = 0; c < column; c++)
                {
                    for (int i = 0; i < split.Length; i++) values[i] = output[i, r, c];
                    Output[r, c] = operation(values);
                }
                return values; // Return the local array for reuse by the thread
            }, _ => { });
            return Output;
        }
        private double[,] Max(string input) => ProcessMinMax(input, values => values.Max());
        private double[,] Min(string input) => ProcessMinMax(input, values => values.Min());
        #endregion
        #region Additional Calculations
        private unsafe double[,] Hypergeometric(string input)
        {
            string[] split = MyString.ReplaceRecover(input);
            if (split.Length > 5 || split.Length < 4) throw new FormatException();
            int n = split.Length == 5 ? MyString.ToInt(split[4]) : 100;
            double[,] sum = new double[row, column];
            double[,] product = ConstructConstant(1);
            double[,] input_new = new RealSubstitution(split[3], x, y, X, Y, row, column).ObtainValues();
            double[,] _a = new RealSubstitution(split[0], row, column).ObtainValues();
            double[,] _b = new RealSubstitution(split[1], row, column).ObtainValues();
            double[,] _c = new RealSubstitution(split[2], row, column).ObtainValues();
            for (int i = 0; i <= n; i++)
            {
                if (i > 0)
                {
                    Parallel.For(0, row, r =>
                    {
                        fixed (double* prodPtr = &product[r, 0], inputPtr = &input_new[r, 0], aPtr = &_a[r, 0], bPtr = &_b[r, 0], cPtr = &_c[r, 0])
                        {
                            for (int c = 0; c < column; c++)
                            {
                                prodPtr[c] *= inputPtr[c] * (aPtr[c] + i - 1) * (bPtr[c] + i - 1);
                                prodPtr[c] /= (cPtr[c] + i - 1) * i;
                            }
                        }
                    });
                }
                Parallel.For(0, row, r => {
                    fixed (double* sumPtr = &sum[r, 0], prodPtr = &product[r, 0])
                    { for (int c = 0; c < column; c++) sumPtr[c] += prodPtr[c]; }
                });
            }
            return sum;
        }
        private unsafe double[,] Gamma(string input)
        {
            string[] split = MyString.ReplaceRecover(input);
            if (split.Length > 2) throw new FormatException();
            int n = split.Length == 2 ? MyString.ToInt(split[1]) : 100;
            double[,] product = ConstructConstant(1);
            double[,] temp_value = new RealSubstitution(split[0], x, y, X, Y, row, column).ObtainValues();
            for (int i = 1; i <= n; i++)
            {
                double[,] term = new double[row, column];
                Parallel.For(0, row, r =>
                {
                    fixed (double* tempPtr = &temp_value[r, 0], prodPtr = &product[r, 0])
                    {
                        for (int c = 0; c < column; c++) prodPtr[c] *= Math.Exp(tempPtr[c] / i) / (1 + tempPtr[c] / i);
                    }
                });
            }
            double[,] output = new double[row, column];
            Parallel.For(0, row, r =>
            {
                fixed (double* outPtr = &output[r, 0], prodPtr = &product[r, 0], tempPtr = &temp_value[r, 0])
                {
                    for (int c = 0; c < column; c++) outPtr[c] = prodPtr[c] * Math.Exp(-GAMMA * tempPtr[c]) / tempPtr[c];
                }
            });
            return output;
        }
        private unsafe double[,] Beta(string input)
        {
            string[] split = MyString.ReplaceRecover(input);
            if (split.Length > 3 || split.Length < 2) throw new FormatException();
            int n = split.Length == 3 ? MyString.ToInt(split[2]) : 100;
            double[,] product = ConstructConstant(1);
            double[,] temp_1 = new RealSubstitution(split[0], x, y, X, Y, row, column).ObtainValues();
            double[,] temp_2 = new RealSubstitution(split[1], x, y, X, Y, row, column).ObtainValues();
            for (int i = 1; i <= n; i++)
            {
                double[,] term = new double[row, column];
                Parallel.For(0, row, r =>
                {
                    fixed (double* temp1Ptr = &temp_1[r, 0], temp2Ptr = &temp_2[r, 0], prodPtr = &product[r, 0])
                    {
                        for (int c = 0; c < column; c++) prodPtr[c] *= 1 + temp1Ptr[c] * temp2Ptr[c] / (i * (i + temp1Ptr[c] + temp2Ptr[c]));
                    }
                });
            }
            double[,] output = new double[row, column];
            Parallel.For(0, row, r =>
            {
                fixed (double* outPtr = &output[r, 0], temp1Ptr = &temp_1[r, 0], temp2Ptr = &temp_2[r, 0], prodPtr = &product[r, 0])
                {
                    for (int c = 0; c < column; c++) outPtr[c] = (temp1Ptr[c] + temp2Ptr[c]) / (temp1Ptr[c] * temp2Ptr[c]) / prodPtr[c];
                }
            });
            return output;
        }
        private unsafe double[,] Zeta(string input)
        {
            string[] split = MyString.ReplaceRecover(input);
            if (split.Length > 2) throw new FormatException();
            int n = split.Length == 2 ? MyString.ToInt(split[1]) : 50;
            double[,] sum = new double[row, column], Sum = new double[row, column];
            double[,] Coefficient = ConstructConstant(1), coefficient = ConstructConstant(1);
            double[,] temp_value = new RealSubstitution(split[0], x, y, X, Y, row, column).ObtainValues();
            Parallel.For(0, row, r =>
            {
                fixed (double* coeffPtr = &coefficient[r, 0], CoeffPtr = &Coefficient[r, 0], SumPtr = &Sum[r, 0], tempPtr = &temp_value[r, 0], sumPtr = &sum[r, 0])
                {
                    for (int c = 0; c < column; c++)
                    {
                        for (int i = 0; i <= n; i++)
                        {
                            CoeffPtr[c] /= 2;
                            coeffPtr[c] = 1; SumPtr[c] = 0;
                            for (int j = 0; j <= i; j++)
                            {
                                SumPtr[c] += coeffPtr[c] / Math.Pow(j + 1, tempPtr[c]);
                                coeffPtr[c] *= (double)(j - i) / (double)(j + 1); // (double) is necessary
                            }
                            SumPtr[c] *= CoeffPtr[c];
                            sumPtr[c] += SumPtr[c];
                        }
                        sumPtr[c] /= 1 - Math.Pow(2, 1 - tempPtr[c]);
                    }
                }
            });
            return sum;
        }
        private unsafe double[,] Sum(string input)
        {
            string[] split = MyString.ReplaceRecover(input);
            if (split.Length != 4) throw new FormatException();
            double[,] sum = new double[row, column];
            for (int i = MyString.ToInt(split[2]); i <= MyString.ToInt(split[3]); i++)
            {
                string term = split[0].Replace(split[1], $"({i})");
                double[,] temp_value = new RealSubstitution(term, x, y, X, Y, row, column).ObtainValues();
                Parallel.For(0, row, r => {
                    fixed (double* sumPtr = &sum[r, 0], tempPtr = &temp_value[r, 0])
                    { for (int c = 0; c < column; c++) sumPtr[c] += tempPtr[c]; }
                });
            }
            return sum;
        }
        private unsafe double[,] Product(string input)
        {
            string[] split = MyString.ReplaceRecover(input);
            if (split.Length != 4) throw new FormatException();
            double[,] product = ConstructConstant(1);
            for (int i = MyString.ToInt(split[2]); i <= MyString.ToInt(split[3]); i++)
            {
                string term = split[0].Replace(split[1], $"({i})");
                double[,] temp_value = new RealSubstitution(term, x, y, X, Y, row, column).ObtainValues();
                Parallel.For(0, row, r => {
                    fixed (double* prodPtr = &product[r, 0], tempPtr = &temp_value[r, 0])
                    { for (int c = 0; c < column; c++) prodPtr[c] *= tempPtr[c]; }
                });
            }
            return product;
        }
        private double[,] Iterate1(string input)
        {
            string[] split = MyString.ReplaceRecover(input);
            if (split.Length != 5) throw new FormatException();
            double[,] value = new RealSubstitution(split[1], x, y, row, column).ObtainValues(), temp = new double[row, column];
            for (int i = MyString.ToInt(split[3]); i <= MyString.ToInt(split[4]); i++)
                value = new RealSubstitution(split[0].Replace(split[2], $"({i})"), x, y, value, temp, row, column).ObtainValues();
            return value;
        }
        private double[,] Iterate2(string input)
        {
            string[] split = MyString.ReplaceRecover(input);
            if (split.Length != 8) throw new FormatException();
            double[,] value1 = new RealSubstitution(split[2], x, y, row, column).ObtainValues();
            double[,] value2 = new RealSubstitution(split[3], x, y, row, column).ObtainValues();
            for (int i = MyString.ToInt(split[5]); i <= MyString.ToInt(split[6]); i++)
            {
                double[,] temp1 = value1, temp2 = value2;
                value1 = new RealSubstitution(split[0].Replace(split[4], $"({i})"), x, y, temp1, temp2, row, column).ObtainValues();
                value2 = new RealSubstitution(split[1].Replace(split[4], $"({i})"), x, y, temp1, temp2, row, column).ObtainValues();
            }
            return split[7] == "1" ? value1 : value2;
        }
        private double[,] Composite1(string input)
        {
            string[] split = MyString.ReplaceRecover(input);
            double[,] value = new RealSubstitution(split[0], x, y, row, column).ObtainValues();
            double[,] temp = new double[row, column];
            for (int i = 0; i < split.Length - 1; i++) value = new RealSubstitution(split[i + 1], x, y, value, temp, row, column).ObtainValues();
            return value;
        }
        private double[,] Composite2(string input)
        {
            string[] split = MyString.ReplaceRecover(input);
            if (split.Length % 2 == 0) throw new FormatException();
            string[] comp_1 = new string[(split.Length / 2) - 1], comp_2 = new string[(split.Length / 2) - 1];
            for (int i = 0; i < comp_1.Length; i++) { comp_1[i] = split[2 * i + 2]; comp_2[i] = split[2 * i + 3]; }
            double[][,] value = new double[2][,];
            value[0] = new RealSubstitution(split[0], x, y, row, column).ObtainValues();
            value[1] = new RealSubstitution(split[1], x, y, row, column).ObtainValues();
            for (int i = 0; i < comp_1.Length; i++)
            {
                double[,] temp_1 = value[0], temp_2 = value[1]; // This is necessary as value[0] may affect value[1]
                value[0] = new RealSubstitution(comp_1[i], x, y, temp_1, temp_2, row, column).ObtainValues();
                value[1] = new RealSubstitution(comp_2[i], x, y, temp_1, temp_2, row, column).ObtainValues();
            }
            return split[^1] == "1" ? value[0] : value[1];
        }
        #endregion
        #region Assembly
        private unsafe double[,] ConstructConstant(double c)
        {
            double[,] output = new double[row, column];
            for (int q = 0; q < column; q++) output[0, q] = c;
            Parallel.For(1, row, p => {
                fixed (double* destPtr = &output[p, 0], srcPtr = &output[0, 0])
                { Buffer.MemoryCopy(srcPtr, destPtr, columnSIZE, columnSIZE); }
            });
            return output;
        }
        private unsafe double[,] Transform(string input, int temp)
        {
            if (input[0] == '[')
            {
                double[,] output = new double[row, column];
                Parallel.For(0, row, p => {
                    fixed (double* outPtr = &output[p, 0], bracketedPtr = &bracketed_values[temp, p, 0])
                    { Buffer.MemoryCopy(bracketedPtr, outPtr, columnSIZE, columnSIZE); }
                });
                return output;
            }
            return input[0] switch
            {
                'x' => x,
                'y' => y,
                'X' => X,
                'Y' => Y,
                'e' => ConstructConstant(Math.E),
                'p' => ConstructConstant(Math.PI),
                'g' => ConstructConstant(GAMMA),
                _ => ConstructConstant(Convert.ToDouble(input))
            };
        }
        private unsafe double[,] BreakLongPlus(string input, int count_plus)
        {
            input = input[0] == '-' ? input : String.Concat('+', input);
            int flag = 0, _flag = 0;
            char[] signs = new char[(count_plus + 1) / THRESHOLD];
            StringBuilder result = new(input);
            for (int i = 0; i < result.Length; i++)
            {
                if (result[i] != '+' && result[i] != '-') continue;
                flag++;
                if (flag % THRESHOLD == 0)
                {
                    string replacement = result[i] == '+' ? ":" : ";"; // This is necessary as the index will alter after the removal
                    result.Remove(i, 1).Insert(i, replacement);
                    signs[_flag++] = result[i];
                }
            }
            string[] chunks = MyString.SplitByChars(result.ToString(), new char[] { ':', ';' });
            double[,] sum = CalculateBracketFreeString(MyString.TrimStartChars(chunks[0], new char[] { '+' }));
            for (int i = 1; i < chunks.Length; i++)
            {
                double[,] temp = CalculateBracketFreeString(signs[i - 1] == ':' ? chunks[i] : String.Concat('-', chunks[i]));
                Parallel.For(0, row, p => {
                    fixed (double* sumPtr = &sum[p, 0], tempPtr = &temp[p, 0])
                    { for (int q = 0; q < column; q++) sumPtr[q] += tempPtr[q]; }
                });
            }
            return sum;
        }
        private unsafe double[,] BreakLongMultiply(string input, int count_multiply)
        {
            input = String.Concat('*', input);
            int flag = 0, _flag = 0;
            char[] signs = new char[(count_multiply + 1) / THRESHOLD];
            StringBuilder result = new(input);
            for (int i = 0; i < result.Length; i++)
            {
                if (result[i] != '*' && result[i] != '/') continue;
                flag++;
                if (flag % THRESHOLD == 0)
                {
                    string replacement = result[i] == '*' ? ":" : ";"; // This is necessary as the index will alter after the removal
                    result.Remove(i, 1).Insert(i, replacement);
                    signs[_flag++] = result[i];
                }
            }
            string[] chunks = MyString.SplitByChars(result.ToString(), new char[] { ':', ';' });
            double[,] product = CalculateBracketFreeString(MyString.TrimStartChars(chunks[0], new char[] { '*' }));
            for (int i = 1; i < chunks.Length; i++)
            {
                double[,] temp = CalculateBracketFreeString(signs[i - 1] == ':' ? chunks[i] : String.Concat("1/", chunks[i]));
                Parallel.For(0, row, p => {
                    fixed (double* prodPtr = &product[p, 0], tempPtr = &temp[p, 0])
                    { for (int q = 0; q < column; q++) prodPtr[q] *= tempPtr[q]; }
                });
            }
            return product;
        }
        private unsafe double[,] BreakLongPower(string input, int count_power)
        {
            int flag = 0;
            char[] signs = new char[(count_power + 1) / THRESHOLD];
            StringBuilder result = new(input);
            for (int i = 0; i < result.Length; i++)
            {
                if (result[i] != '^') continue;
                flag++;
                if (flag % THRESHOLD == 0) result.Remove(i, 1).Insert(i, ":");
            }
            string[] chunks = MyString.SplitByChars(result.ToString(), new char[] { ':' });
            double[,] term = new double[row, column];
            double[,] tmp = InnerValues(chunks[^1]);
            Parallel.For(0, row, p => {
                fixed (double* tmpPtr = &tmp[p, 0], termPtr = &term[p, 0])
                { Buffer.MemoryCopy(tmpPtr, termPtr, columnSIZE, columnSIZE); }
            });
            for (int m = chunks.Length - 2; m >= 0; m--)
            {
                string[] split = MyString.SplitByChars(chunks[m], new char[] { '^' });
                for (int t = split.Length - 1; t >= 0; t--)
                {
                    double[,] temp = InnerElement(split[t]);
                    Parallel.For(0, row, p => {
                        fixed (double* termPtr = &term[p, 0], tempPtr = &temp[p, 0])
                        { for (int q = 0; q < column; q++) termPtr[q] = Math.Pow(tempPtr[q], termPtr[q]); }
                    });
                }
            }
            return term;
        }
        private double[,] InnerElement(string input)
            => Transform(input, input[0] == '[' ? Int32.Parse(MyString.Extract(input, 1, input.IndexOf(']') - 1)) : 0);
        private unsafe double[,] InnerValues(string split)
        {
            if (!split.Contains('^')) return InnerElement(split); // Be careful here
            else if (MyString.CountChar(split, '^') > THRESHOLD) return BreakLongPower(split, split.Length);
            else
            {
                string[] inner_string = MyString.SplitByChars(split, new char[] { '^' });
                double[,] tower = new double[row, column];
                double[,] tmp = InnerElement(inner_string[^1]);
                Parallel.For(0, row, p => {
                    fixed (double* tmpPtr = &tmp[p, 0], towerPtr = &tower[p, 0])
                    { Buffer.MemoryCopy(tmpPtr, towerPtr, columnSIZE, columnSIZE); }
                });
                for (int m = inner_string.Length - 2; m >= 0; m--)
                {
                    double[,] temp = InnerElement(inner_string[m]);
                    Parallel.For(0, row, p => {
                        fixed (double* towerPtr = &tower[p, 0], tempPtr = &temp[p, 0])
                        { for (int q = 0; q < column; q++) towerPtr[q] = Math.Pow(tempPtr[q], towerPtr[q]); }
                    });
                }
                return tower;
            }
        }
        private unsafe double[,] CalculateBracketFreeString(string input)
        {
            if (input.Contains('(')) throw new FormatException();
            if (Int32.TryParse(input, out int result)) return ConstructConstant(result);
            if (input[0] == '[')
            {
                string extracted = MyString.Extract(input, 1, input.Length - 2);
                if (Int32.TryParse(extracted, out int newResult)) // This is necessary
                {
                    double[,] temp = new double[row, column];
                    Parallel.For(0, row, p => {
                        fixed (double* tempPtr = &temp[p, 0], bracketedPtr = &bracketed_values[newResult, p, 0])
                        { Buffer.MemoryCopy(bracketedPtr, tempPtr, columnSIZE, columnSIZE); }
                    });
                    return temp;
                }
            }
            int count_plus = MyString.CountChar(input, '+') + MyString.CountChar(input, '-');
            if (count_plus > THRESHOLD) return BreakLongPlus(input, count_plus);
            bool begins_minus = input[0] == '-'; // This is necessary in case splitting the first minus sign
            input = MyString.TrimStartChars(input, new char[] { '-' });
            string[] temp_split = MyString.SplitByChars(input, new char[] { '+', '-' });
            input = String.Concat(begins_minus ? '-' : '+', input);
            char[] plus_type = new char[count_plus + 1];
            for (int i = 0, j = 0; i < input.Length; i++) { if (input[i] == '+' || input[i] == '-') plus_type[j++] = input[i]; }
            double[,] sum = new double[row, column], term = new double[row, column];
            for (int i = 0; i < temp_split.Length; i++)
            {
                string tmpSplit = temp_split[i];
                if (!MyString.ContainsAny(tmpSplit, new char[] { '*', '/' }))
                {
                    double[,] tp = InnerValues(tmpSplit);
                    Parallel.For(0, row, p => {
                        fixed (double* tpPtr = &tp[p, 0], termPtr = &term[p, 0])
                        { Buffer.MemoryCopy(tpPtr, termPtr, columnSIZE, columnSIZE); }
                    });
                }
                else
                {
                    string[] split = MyString.SplitByChars(tmpSplit, new char[] { '*', '/' });
                    char[] multiply_type = new char[split.Length];
                    for (int k = 0, j = 0; k < tmpSplit.Length; k++)
                        if (tmpSplit[k] == '*' || tmpSplit[k] == '/') multiply_type[j++] = tmpSplit[k];
                    double[,] tmp = InnerValues(split[0]); // Cannot write var term = InnerValue(split[0]);
                    Parallel.For(0, row, p => {
                        fixed (double* tmpPtr = &tmp[p, 0], termPtr = &term[p, 0])
                        { Buffer.MemoryCopy(tmpPtr, termPtr, columnSIZE, columnSIZE); }
                    });
                    if (split.Length > THRESHOLD) term = BreakLongMultiply(tmpSplit, split.Length);
                    else
                        for (int k = 1; k < split.Length; k++)
                        {
                            double[,] temp = InnerValues(split[k]);
                            if (multiply_type[k - 1] == '*')
                                Parallel.For(0, row, p => {
                                    fixed (double* termPtr = &term[p, 0], tempPtr = &temp[p, 0])
                                    { for (int q = 0; q < column; q++) termPtr[q] *= tempPtr[q]; }
                                });
                            else
                                Parallel.For(0, row, p => {
                                    fixed (double* termPtr = &term[p, 0], tempPtr = &temp[p, 0])
                                    { for (int q = 0; q < column; q++) termPtr[q] /= tempPtr[q]; }
                                });
                        }
                }
                if (i == 0)
                {
                    if (plus_type[i] == '+')
                        Parallel.For(0, row, p => {
                            fixed (double* sumPtr = &sum[p, 0], termPtr = &term[p, 0])
                            { Buffer.MemoryCopy(termPtr, sumPtr, columnSIZE, columnSIZE); }
                        });
                    else
                        Parallel.For(0, row, p => {
                            fixed (double* sumPtr = &sum[p, 0], termPtr = &term[p, 0])
                            { for (int q = 0; q < column; q++) sumPtr[q] = -termPtr[q]; }
                        });
                }
                else
                {
                    if (plus_type[i] == '+')
                        Parallel.For(0, row, p => {
                            fixed (double* sumPtr = &sum[p, 0], termPtr = &term[p, 0])
                            { for (int q = 0; q < column; q++) sumPtr[q] += termPtr[q]; }
                        });
                    else
                        Parallel.For(0, row, p => {
                            fixed (double* sumPtr = &sum[p, 0], termPtr = &term[p, 0])
                            { for (int q = 0; q < column; q++) sumPtr[q] -= termPtr[q]; }
                        });
                }
            }
            return sum;
        }
        private unsafe string SubstituteSeries(string input)
        {
            if (!input.Contains('_')) return input;
            int i = input.IndexOf('_');
            if (input[i + 1] != '(') throw new FormatException();
            int begin = i + 2;
            int end = MyString.PairedParenthesis(input, i + 1) - 1;
            string temp = MyString.Extract(input, begin, end);
            double[,] temp_values = new double[row, column];
            switch (input[i - 1])
            {
                case 'S': temp_values = Sum(temp); break;
                case 'P': temp_values = Product(temp); break;
                case 'F': temp_values = Hypergeometric(temp); break;
                case 'G': temp_values = Gamma(temp); break;
                case 'B': temp_values = Beta(temp); break;
                case 'Z': temp_values = Zeta(temp); break;
                case 'M': temp_values = Mod(temp); break;
                case 'C': temp_values = Combination(temp); break;
                case 'A': temp_values = Permutation(temp); break;
                case '>': temp_values = Max(temp); break;
                case '<': temp_values = Min(temp); break;
                case 'I':
                    if (input[i - 2] == '1') temp_values = Iterate1(temp);
                    if (input[i - 2] == '2') temp_values = Iterate2(temp);
                    break;
                case 'J':
                    if (input[i - 2] == '1') temp_values = Composite1(temp);
                    if (input[i - 2] == '2') temp_values = Composite2(temp);
                    break;
            }
            Parallel.For(0, row, r => {
                fixed (double* bracketedPtr = &bracketed_values[count, r, 0], tempPtr = &temp_values[r, 0])
                { Buffer.MemoryCopy(tempPtr, bracketedPtr, columnSIZE, columnSIZE); }
            });
            string substitute_value = MyString.ConstructSubstitution(count);
            int temp_int = (input[i - 1] == 'I' || input[i - 1] == 'J') ? 4 : 3;
            count++;
            return MyString.Replace(input, substitute_value, begin - temp_int - 1, end + 1);
        }
        private unsafe void FuncSub(double[,] subValue, Func<double, double> function)
        {
            Parallel.For(0, row, r => {
                fixed (double* subValuePtr = &subValue[r, 0])
                { for (int c = 0; c < column; c++) subValuePtr[c] = function(subValuePtr[c]); }
            });
        }
        public unsafe double[,] ObtainValues()
        {
            double[,] subValue; string temp = input;
            do { temp = SubstituteSeries(temp); } while (temp.Contains('_'));
            int length = MyString.CountChar(temp, '(');
            for (int loop = 0; loop < length; loop++)
            {
                int begin = MyString.FindOpeningParenthesis(temp, length - loop);
                int end = MyString.PairedParenthesis(temp, begin);
                subValue = CalculateBracketFreeString(MyString.Extract(temp, begin + 1, end - 1));
                int tagL = -1; // Because of ~ as the head of each tag
                if (begin > 0)
                {
                    bool isA = begin > 1 ? temp[begin - 2] != 'a' : false; // The check is not redundant
                    switch (temp[begin - 1])
                    {
                        case 's': FuncSub(subValue, isA ? Math.Sin : Math.Asin); tagL = isA ? 1 : 2; break;
                        case 'c': FuncSub(subValue, isA ? Math.Cos : Math.Acos); tagL = isA ? 1 : 2; break;
                        case 't': FuncSub(subValue, isA ? Math.Tan : Math.Atan); tagL = isA ? 1 : 2; break;
                        case 'h':
                            bool IsA = temp[begin - 3] != 'a'; // Don't need check because of ~
                            switch (temp[begin - 2])
                            {
                                case 's': FuncSub(subValue, IsA ? Math.Sinh : Math.Asinh); tagL = IsA ? 2 : 3; break;
                                case 'c': FuncSub(subValue, IsA ? Math.Cosh : Math.Acosh); tagL = IsA ? 2 : 3; break;
                                case 't': FuncSub(subValue, IsA ? Math.Tanh : Math.Atanh); tagL = IsA ? 2 : 3; break;
                            }
                            break;
                        case 'a': FuncSub(subValue, Math.Abs); tagL = 1; break;
                        case 'l': FuncSub(subValue, Math.Log); tagL = 1; break;
                        case 'E': FuncSub(subValue, Math.Exp); tagL = 1; break;
                        case 'q': FuncSub(subValue, Math.Sqrt); tagL = 1; break;
                        case '!': FuncSub(subValue, Factorial); tagL = 1; break;
                        case '$': // Special for real
                            switch (temp[begin - 2])
                            {
                                case 'f': FuncSub(subValue, Math.Floor); tagL = 2; break;
                                case 'c': FuncSub(subValue, Math.Ceiling); tagL = 2; break;
                                case 'r': FuncSub(subValue, Math.Round); tagL = 2; break;
                                case 's': FuncSub(subValue, MySign); tagL = 2; break;
                            }
                            break;
                        default: break;
                    }
                }
                temp = MyString.Replace(temp, MyString.ConstructSubstitution(count), begin - tagL - 1, end);
                Parallel.For(0, row, r => {
                    fixed (double* bracketedPtr = &bracketed_values[count, r, 0], subPtr = &subValue[r, 0])
                    { Buffer.MemoryCopy(subPtr, bracketedPtr, columnSIZE, columnSIZE); }
                });
                count++;
            }
            return CalculateBracketFreeString(temp);
        }
        public static double ObtainValue(string input, double x = 0) => new RealSubstitution(input, x).ObtainValues()[0, 0];
        #endregion
    }
    public class ComplexSubstitution
    {
        private const double GAMMA = 0.57721566490153286060651209008240243;
        private const int THRESHOLD = 10;
        private const int STRUCTSIZE = 16; // Size of Complex
        private string input;
        private int row, column, columnSIZE, count = 0;
        private Complex[,] z, Z;
        private Complex[,,] bracketed_values;
        #region Constructors
        private void Initialize(string input, int row, int column)
        {
            if (String.IsNullOrEmpty(input)) throw new FormatException();
            this.input = RecoverMultiplication.Recover(input, true);
            bracketed_values = new Complex[MyString.CountChar(this.input, '(') + 1, row, column];
            z = Z = new Complex[row, column];
            this.row = row; this.column = column; columnSIZE = column * STRUCTSIZE;
        }
        public ComplexSubstitution(string input, int row, int column) => Initialize(input, row, column);
        public ComplexSubstitution(string input, Complex[,] z, int row, int column) : this(input, row, column) => this.z = z;
        public unsafe ComplexSubstitution(string input, double[,] real, double[,] imaginary, int row, int column) : this(input, row, column)
        {
            Parallel.For(0, row, i => {
                fixed (double* realPtr = &real[0, 0], imaginaryPtr = &imaginary[0, 0])
                { for (int j = 0; j < column; j++) z[i, j] = new Complex(realPtr[i * column + j], imaginaryPtr[i * column + j]); }
            });
        }
        public ComplexSubstitution(string input, Complex[,] z, Complex[,] Z, int row, int column) : this(input, row, column)
        { this.z = z; this.Z = Z; }
        #endregion
        #region Calculations
        public static double ArgumentForRGB(Complex input) => ArgumentForRGB(input.real, input.imaginary);
        public static double ArgumentForRGB(double x, double y)
        {
            if (Double.IsNaN(x) && Double.IsNaN(y)) return -1;
            if (y == 0)
            {
                if (x == 0) return -1;
                return x > 0 ? 0 : Math.PI;
            }
            return y > 0 ? Math.Atan2(y, x) : Math.Atan2(y, x) + Math.Tau;
        } // Extremely sensitive, mustn't move a hair.
        private unsafe Complex[,] Hypergeometric(string input)
        {
            string[] split = MyString.ReplaceRecover(input);
            if (split.Length > 5 || split.Length < 4) throw new FormatException();
            int n = split.Length == 5 ? MyString.ToInt(split[4]) : 100;
            Complex[,] sum = new Complex[row, column];
            Complex[,] product = ConstructConstant(new(1));
            Complex[,] input_new = new ComplexSubstitution(split[3], z, Z, row, column).ObtainValues();
            Complex[,] _a = new ComplexSubstitution(split[0], row, column).ObtainValues();
            Complex[,] _b = new ComplexSubstitution(split[1], row, column).ObtainValues();
            Complex[,] _c = new ComplexSubstitution(split[2], row, column).ObtainValues();
            for (int i = 0; i <= n; i++)
            {
                if (i > 0)
                {
                    Parallel.For(0, row, r =>
                    {
                        fixed (Complex* prodPtr = &product[r, 0], inputPtr = &input_new[r, 0], aPtr = &_a[r, 0], bPtr = &_b[r, 0], cPtr = &_c[r, 0])
                        {
                            for (int c = 0; c < column; c++)
                            {
                                prodPtr[c] *= inputPtr[c] * (aPtr[c] + new Complex(i - 1)) * (bPtr[c] + new Complex(i - 1));
                                prodPtr[c] /= (cPtr[c] + new Complex(i - 1)) * new Complex(i);
                            }
                        }
                    });
                }
                Parallel.For(0, row, r => {
                    fixed (Complex* sumPtr = &sum[r, 0], prodPtr = &product[r, 0])
                    { for (int c = 0; c < column; c++) sumPtr[c] += prodPtr[c]; }
                });
            }
            return sum;
        }
        private unsafe Complex[,] Gamma(string input)
        {
            string[] split = MyString.ReplaceRecover(input);
            if (split.Length > 2) throw new FormatException();
            int n = split.Length == 2 ? MyString.ToInt(split[1]) : 100;
            Complex[,] product = ConstructConstant(new(1));
            Complex[,] temp_value = new ComplexSubstitution(split[0], z, Z, row, column).ObtainValues();
            for (int i = 1; i <= n; i++)
            {
                Complex[,] term = new Complex[row, column];
                Parallel.For(0, row, r =>
                {
                    fixed (Complex* prodPtr = &product[r, 0], tempPtr = &temp_value[r, 0])
                    {
                        for (int c = 0; c < column; c++)
                            prodPtr[c] *= Complex.Exp(tempPtr[c] / new Complex(i)) / (new Complex(1) + tempPtr[c] / new Complex(i));
                    }
                });
            }
            Complex[,] output = new Complex[row, column];
            Parallel.For(0, row, r =>
            {
                fixed (Complex* outPtr = &output[r, 0], prodPtr = &product[r, 0], tempPtr = &temp_value[r, 0])
                {
                    for (int c = 0; c < column; c++) outPtr[c] = prodPtr[c] * Complex.Exp(-new Complex(GAMMA) * tempPtr[c]) / tempPtr[c];
                }
            });
            return output;
        }
        private unsafe Complex[,] Beta(string input)
        {
            string[] split = MyString.ReplaceRecover(input);
            if (split.Length > 3 || split.Length < 2) throw new FormatException();
            int n = split.Length == 3 ? MyString.ToInt(split[2]) : 100;
            Complex[,] product = ConstructConstant(new(1));
            Complex[,] temp_1 = new ComplexSubstitution(split[0], z, Z, row, column).ObtainValues();
            Complex[,] temp_2 = new ComplexSubstitution(split[1], z, Z, row, column).ObtainValues();
            for (int i = 1; i <= n; i++)
            {
                Complex[,] term = new Complex[row, column];
                Parallel.For(0, row, r =>
                {
                    fixed (Complex* prodPtr = &product[r, 0], temp1Ptr = &temp_1[r, 0], temp2Ptr = &temp_2[r, 0])
                    {
                        for (int c = 0; c < column; c++)
                            prodPtr[c] *= new Complex(1) + temp1Ptr[c] * temp2Ptr[c] / (new Complex(i) * (new Complex(i) + temp1Ptr[c] + temp2Ptr[c]));
                    }
                });
            }
            Complex[,] output = new Complex[row, column];
            Parallel.For(0, row, r => {
                fixed (Complex* outPtr = &output[r, 0], temp1Ptr = &temp_1[r, 0], temp2Ptr = &temp_2[r, 0], prodPtr = &product[r, 0])
                {
                    for (int c = 0; c < column; c++) outPtr[c] = (temp1Ptr[c] + temp2Ptr[c]) / (temp1Ptr[c] * temp2Ptr[c]) / prodPtr[c];
                }
            });
            return output;
        }
        private unsafe Complex[,] Zeta(string input)
        {
            string[] split = MyString.ReplaceRecover(input);
            if (split.Length > 2) throw new FormatException();
            int n = split.Length == 2 ? MyString.ToInt(split[1]) : 50;
            Complex[,] sum = new Complex[row, column], Sum = new Complex[row, column];
            Complex[,] Coefficient = ConstructConstant(new(1)), coefficient = ConstructConstant(new(1));
            Complex[,] temp_value = new ComplexSubstitution(split[0], z, Z, row, column).ObtainValues();
            Parallel.For(0, row, r =>
            {
                fixed (Complex* coeffPtr = &coefficient[r, 0], CoeffPtr = &Coefficient[r, 0], SumPtr = &Sum[r, 0], tempPtr = &temp_value[r, 0], sumPtr = &sum[r, 0])
                {
                    for (int c = 0; c < column; c++)
                    {
                        for (int i = 0; i <= n; i++)
                        {
                            CoeffPtr[c] /= new Complex(2);
                            coeffPtr[c] = new Complex(1); SumPtr[c] = new Complex(0);
                            for (int j = 0; j <= i; j++)
                            {
                                SumPtr[c] += coeffPtr[c] / ((new Complex(j + 1)) ^ tempPtr[c]);
                                coeffPtr[c] *= new Complex((double)(j - i) / (double)(j + 1)); // (double) is necessary
                            }
                            SumPtr[c] *= CoeffPtr[c];
                            sumPtr[c] += SumPtr[c];
                        }
                        sumPtr[c] /= (new Complex(1) - (new Complex(2) ^ (new Complex(1) - tempPtr[c])));
                    }
                }
            });
            return sum;
        }
        private unsafe Complex[,] Sum(string input)
        {
            string[] split = MyString.ReplaceRecover(input);
            if (split.Length != 4) throw new FormatException();
            Complex[,] sum = new Complex[row, column];
            for (int i = MyString.ToInt(split[2]); i <= MyString.ToInt(split[3]); i++)
            {
                string term = split[0].Replace(split[1], $"({i})");
                Complex[,] temp_value = new ComplexSubstitution(term, z, Z, row, column).ObtainValues();
                Parallel.For(0, row, r => {
                    fixed (Complex* sumPtr = &sum[r, 0], tempPtr = &temp_value[r, 0])
                    { for (int c = 0; c < column; c++) sumPtr[c] += tempPtr[c]; }
                });
            }
            return sum;
        }
        private unsafe Complex[,] Product(string input)
        {
            string[] split = MyString.ReplaceRecover(input);
            if (split.Length != 4) throw new FormatException();
            Complex[,] product = ConstructConstant(new(1));
            for (int i = MyString.ToInt(split[2]); i <= MyString.ToInt(split[3]); i++)
            {
                string term = split[0].Replace(split[1], $"({i})");
                Complex[,] temp_value = new ComplexSubstitution(term, z, Z, row, column).ObtainValues();
                Parallel.For(0, row, r => {
                    fixed (Complex* prodPtr = &product[r, 0], tempPtr = &temp_value[r, 0])
                    { for (int c = 0; c < column; c++) prodPtr[c] *= tempPtr[c]; }
                });
            }
            return product;
        }
        private Complex[,] Iterate(string input)
        {
            string[] split = MyString.ReplaceRecover(input);
            if (split.Length != 5) throw new FormatException();
            Complex[,] value = new ComplexSubstitution(split[1], z, row, column).ObtainValues();
            for (int i = MyString.ToInt(split[3]); i <= MyString.ToInt(split[4]); i++)
                value = new ComplexSubstitution(split[0].Replace(split[2], $"({i})"), z, value, row, column).ObtainValues();
            return value;
        }
        private Complex[,] Composite(string input)
        {
            string[] split = MyString.ReplaceRecover(input);
            Complex[,] value = new ComplexSubstitution(split[0], z, row, column).ObtainValues();
            for (int i = 1; i < split.Length; i++) value = new ComplexSubstitution(split[i], z, value, row, column).ObtainValues();
            return value;
        }
        #endregion
        #region Assembly
        private unsafe Complex[,] ConstructConstant(Complex c)
        {
            Complex[,] output = new Complex[row, column];
            for (int q = 0; q < column; q++) output[0, q] = c;
            Parallel.For(1, row, p => {
                fixed (Complex* destPtr = &output[p, 0], srcPtr = &output[0, 0])
                { Buffer.MemoryCopy(srcPtr, destPtr, columnSIZE, columnSIZE); }
            });
            return output;
        }
        private unsafe Complex[,] Transform(string input, int temp)
        {
            if (input[0] == '[')
            {
                Complex[,] output = new Complex[row, column];
                Parallel.For(0, row, p => {
                    fixed (Complex* outPtr = &output[p, 0], bracketedPtr = &bracketed_values[temp, p, 0])
                    { Buffer.MemoryCopy(bracketedPtr, outPtr, columnSIZE, columnSIZE); }
                });
                return output;
            }
            return input[0] switch
            {
                'z' => z,
                'Z' => Z,
                'i' => ConstructConstant(new Complex(0, 1)),
                'e' => ConstructConstant(new Complex(Math.E)),
                'p' => ConstructConstant(new Complex(Math.PI)),
                'g' => ConstructConstant(new Complex(GAMMA)),
                _ => ConstructConstant(new Complex(Convert.ToDouble(input)))
            };
        }
        private unsafe Complex[,] BreakLongPlus(string input, int count_plus)
        {
            input = input[0] == '-' ? input : String.Concat('+', input);
            int flag = 0, _flag = 0;
            char[] signs = new char[(count_plus + 1) / THRESHOLD];
            StringBuilder result = new(input);
            for (int i = 0; i < result.Length; i++)
            {
                if (result[i] != '+' && result[i] != '-') continue;
                flag++;
                if (flag % THRESHOLD == 0)
                {
                    string replacement = result[i] == '+' ? ":" : ";"; // This is necessary as the index will alter after the removal
                    result.Remove(i, 1).Insert(i, replacement);
                    signs[_flag++] = result[i];
                }
            }
            string[] chunks = MyString.SplitByChars(result.ToString(), new char[] { ':', ';' });
            Complex[,] sum = CalculateBracketFreeString(MyString.TrimStartChars(chunks[0], new char[] { '+' }));
            for (int i = 1; i < chunks.Length; i++)
            {
                Complex[,] temp = CalculateBracketFreeString(signs[i - 1] == ':' ? chunks[i] : String.Concat('-', chunks[i]));
                Parallel.For(0, row, p => {
                    fixed (Complex* sumPtr = &sum[p, 0], tempPtr = &temp[p, 0])
                    { for (int q = 0; q < column; q++) sumPtr[q] += tempPtr[q]; }
                });
            }
            return sum;
        }
        private unsafe Complex[,] BreakLongMultiply(string input, int count_multiply)
        {
            input = String.Concat('*', input);
            int flag = 0, _flag = 0;
            char[] signs = new char[(count_multiply + 1) / THRESHOLD];
            StringBuilder result = new(input);
            for (int i = 0; i < result.Length; i++)
            {
                if (result[i] != '*' && result[i] != '/') continue;
                flag++;
                if (flag % THRESHOLD == 0)
                {
                    string replacement = result[i] == '*' ? ":" : ";"; // This is necessary as the index will alter after the removal
                    result.Remove(i, 1).Insert(i, replacement);
                    signs[_flag++] = result[i];
                }
            }
            string[] chunks = MyString.SplitByChars(result.ToString(), new char[] { ':', ';' });
            Complex[,] product = CalculateBracketFreeString(MyString.TrimStartChars(chunks[0], new char[] { '*' }));
            for (int i = 1; i < chunks.Length; i++)
            {
                Complex[,] temp = CalculateBracketFreeString(signs[i - 1] == ':' ? chunks[i] : String.Concat("1/", chunks[i]));
                Parallel.For(0, row, p => {
                    fixed (Complex* prodPtr = &product[p, 0], tempPtr = &temp[p, 0])
                    { for (int q = 0; q < column; q++) prodPtr[q] *= tempPtr[q]; }
                });
            }
            return product;
        }
        private unsafe Complex[,] BreakLongPower(string input, int count_power)
        {
            int flag = 0;
            char[] signs = new char[(count_power + 1) / THRESHOLD];
            StringBuilder result = new(input);
            for (int i = 0; i < result.Length; i++)
            {
                if (result[i] != '^') continue;
                flag++;
                if (flag % THRESHOLD == 0) result.Remove(i, 1).Insert(i, ":");
            }
            string[] chunks = MyString.SplitByChars(result.ToString(), new char[] { ':' });
            Complex[,] term = new Complex[row, column];
            Complex[,] tmp = InnerValues(chunks[^1]);
            Parallel.For(0, row, p => {
                fixed (Complex* tmpPtr = &tmp[p, 0], termPtr = &term[p, 0])
                { Buffer.MemoryCopy(tmpPtr, termPtr, columnSIZE, columnSIZE); }
            });
            for (int m = chunks.Length - 2; m >= 0; m--)
            {
                string[] split = MyString.SplitByChars(chunks[m], new char[] { '^' });
                for (int t = split.Length - 1; t >= 0; t--)
                {
                    Complex[,] temp = InnerElement(split[t]);
                    Parallel.For(0, row, p => {
                        fixed (Complex* termPtr = &term[p, 0], tempPtr = &temp[p, 0])
                        { for (int q = 0; q < column; q++) termPtr[q] = tempPtr[q] ^ termPtr[q]; }
                    });
                }
            }
            return term;
        }
        private Complex[,] InnerElement(string input)
            => Transform(input, input[0] == '[' ? Int32.Parse(MyString.Extract(input, 1, input.IndexOf(']') - 1)) : 0);
        private unsafe Complex[,] InnerValues(string split)
        {
            if (!split.Contains('^')) return InnerElement(split); // Be careful here
            else if (MyString.CountChar(split, '^') > THRESHOLD) return BreakLongPower(split, split.Length);
            else
            {
                string[] inner_string = MyString.SplitByChars(split, new char[] { '^' });
                Complex[,] tower = new Complex[row, column];
                Complex[,] tmp = InnerElement(inner_string[^1]);
                Parallel.For(0, row, p => {
                    fixed (Complex* tmpPtr = &tmp[p, 0], towerPtr = &tower[p, 0])
                    { Buffer.MemoryCopy(tmpPtr, towerPtr, columnSIZE, columnSIZE); }
                });
                for (int m = inner_string.Length - 2; m >= 0; m--)
                {
                    Complex[,] temp = InnerElement(inner_string[m]);
                    Parallel.For(0, row, p => {
                        fixed (Complex* towerPtr = &tower[p, 0], tempPtr = &temp[p, 0])
                        { for (int q = 0; q < column; q++) towerPtr[q] = tempPtr[q] ^ towerPtr[q]; }
                    });
                }
                return tower;
            }
        }
        private unsafe Complex[,] CalculateBracketFreeString(string input)
        {
            if (input.Contains('(')) throw new FormatException();
            if (Int32.TryParse(input, out int result)) return ConstructConstant(new(result));
            if (input[0] == '[')
            {
                string extracted = MyString.Extract(input, 1, input.Length - 2);
                if (Int32.TryParse(extracted, out int newResult)) // This is necessary
                {
                    Complex[,] temp = new Complex[row, column];
                    Parallel.For(0, row, p => {
                        fixed (Complex* tempPtr = &temp[p, 0], bracketedPtr = &bracketed_values[newResult, p, 0])
                        { Buffer.MemoryCopy(bracketedPtr, tempPtr, columnSIZE, columnSIZE); }
                    });
                    return temp;
                }
            }
            int count_plus = MyString.CountChar(input, '+') + MyString.CountChar(input, '-');
            if (count_plus > THRESHOLD) return BreakLongPlus(input, count_plus);
            bool begins_minus = input[0] == '-'; // This is necessary in case splitting the first minus sign
            input = MyString.TrimStartChars(input, new char[] { '-' });
            string[] temp_split = MyString.SplitByChars(input, new char[] { '+', '-' });
            input = String.Concat(begins_minus ? '-' : '+', input);
            char[] plus_type = new char[count_plus + 1];
            for (int i = 0, j = 0; i < input.Length; i++) { if (input[i] == '+' || input[i] == '-') plus_type[j++] = input[i]; }
            Complex[,] sum = new Complex[row, column], term = new Complex[row, column];
            for (int i = 0; i < temp_split.Length; i++)
            {
                string tmpSplit = temp_split[i];
                if (!MyString.ContainsAny(tmpSplit, new char[] { '*', '/' }))
                {
                    Complex[,] tp = InnerValues(tmpSplit);
                    Parallel.For(0, row, p => {
                        fixed (Complex* tpPtr = &tp[p, 0], termPtr = &term[p, 0])
                        { Buffer.MemoryCopy(tpPtr, termPtr, columnSIZE, columnSIZE); }
                    });
                }
                else
                {
                    string[] split = MyString.SplitByChars(tmpSplit, new char[] { '*', '/' });
                    char[] multiply_type = new char[split.Length];
                    for (int k = 0, j = 0; k < tmpSplit.Length; k++)
                        if (tmpSplit[k] == '*' || tmpSplit[k] == '/') multiply_type[j++] = tmpSplit[k];
                    Complex[,] tmp = InnerValues(split[0]); // Cannot write var term = InnerValue(split[0]);
                    Parallel.For(0, row, p => {
                        fixed (Complex* tmpPtr = &tmp[p, 0], termPtr = &term[p, 0])
                        { Buffer.MemoryCopy(tmpPtr, termPtr, columnSIZE, columnSIZE); }
                    });
                    if (split.Length > THRESHOLD) term = BreakLongMultiply(tmpSplit, split.Length);
                    else
                        for (int k = 1; k < split.Length; k++)
                        {
                            Complex[,] temp = InnerValues(split[k]);
                            if (multiply_type[k - 1] == '*')
                                Parallel.For(0, row, p => {
                                    fixed (Complex* termPtr = &term[p, 0], tempPtr = &temp[p, 0])
                                    { for (int q = 0; q < column; q++) termPtr[q] *= tempPtr[q]; }
                                });
                            else
                                Parallel.For(0, row, p => {
                                    fixed (Complex* termPtr = &term[p, 0], tempPtr = &temp[p, 0])
                                    { for (int q = 0; q < column; q++) termPtr[q] /= tempPtr[q]; }
                                });
                        }
                }
                if (i == 0)
                {
                    if (plus_type[i] == '+')
                        Parallel.For(0, row, p => {
                            fixed (Complex* sumPtr = &sum[p, 0], termPtr = &term[p, 0])
                            { Buffer.MemoryCopy(termPtr, sumPtr, columnSIZE, columnSIZE); }
                        });
                    else
                        Parallel.For(0, row, p => {
                            fixed (Complex* sumPtr = &sum[p, 0], termPtr = &term[p, 0])
                            { for (int q = 0; q < column; q++) sumPtr[q] = -termPtr[q]; }
                        });
                }
                else
                {
                    if (plus_type[i] == '+')
                        Parallel.For(0, row, p => {
                            fixed (Complex* sumPtr = &sum[p, 0], termPtr = &term[p, 0])
                            { for (int q = 0; q < column; q++) sumPtr[q] += termPtr[q]; }
                        });
                    else
                        Parallel.For(0, row, p => {
                            fixed (Complex* sumPtr = &sum[p, 0], termPtr = &term[p, 0])
                            { for (int q = 0; q < column; q++) sumPtr[q] -= termPtr[q]; }
                        });
                }
            }
            return sum;
        }
        private unsafe string SubstituteSeries(string input)
        {
            if (!input.Contains('_')) return input;
            int i = input.IndexOf('_');
            if (input[i + 1] != '(') throw new FormatException();
            int begin = i + 2;
            int end = MyString.PairedParenthesis(input, i + 1) - 1;
            string temp = MyString.Extract(input, begin, end);
            Complex[,] temp_values = new Complex[row, column];
            switch (input[i - 1])
            {
                case 'S': temp_values = Sum(temp); break;
                case 'P': temp_values = Product(temp); break;
                case 'F': temp_values = Hypergeometric(temp); break;
                case 'G': temp_values = Gamma(temp); break;
                case 'B': temp_values = Beta(temp); break;
                case 'Z': temp_values = Zeta(temp); break;
                case 'I': temp_values = Iterate(temp); break;
                case 'J': temp_values = Composite(temp); break;
            }
            Parallel.For(0, row, r => {
                fixed (Complex* bracketedPtr = &bracketed_values[count, r, 0], tempPtr = &temp_values[r, 0])
                { Buffer.MemoryCopy(tempPtr, bracketedPtr, columnSIZE, columnSIZE); }
            });
            string substitute_value = MyString.ConstructSubstitution(count);
            count++;
            return MyString.Replace(input, substitute_value, begin - 4, end + 1);
        }
        private unsafe void FuncSub(Complex[,] values, Func<Complex, Complex> function)
        {
            Parallel.For(0, row, r => {
                fixed (Complex* valuesPtr = &values[r, 0])
                { for (int c = 0; c < column; c++) valuesPtr[c] = function(valuesPtr[c]); }
            });
        }
        public unsafe Complex[,] ObtainValues()
        {
            Complex[,] subValue; string temp = input;
            do { temp = SubstituteSeries(temp); } while (temp.Contains('_'));
            int length = MyString.CountChar(temp, '(');
            for (int loop = 0; loop < length; loop++)
            {
                int begin = MyString.FindOpeningParenthesis(temp, length - loop);
                int end = MyString.PairedParenthesis(temp, begin);
                subValue = CalculateBracketFreeString(MyString.Extract(temp, begin + 1, end - 1));
                int tagL = -1; // Because of ~ as the head of each tag
                if (begin > 0)
                {
                    bool isA = begin > 1 ? temp[begin - 2] != 'a' : false; // The check is not redundant
                    switch (temp[begin - 1])
                    {
                        case 's': FuncSub(subValue, isA ? Complex.Sin : Complex.Asin); tagL = isA ? 1 : 2; break;
                        case 'c': FuncSub(subValue, isA ? Complex.Cos : Complex.Acos); tagL = isA ? 1 : 2; break;
                        case 't': FuncSub(subValue, isA ? Complex.Tan : Complex.Atan); tagL = isA ? 1 : 2; break;
                        case 'h':
                            bool IsA = temp[begin - 3] != 'a'; // Don't need check because of ~
                            switch (temp[begin - 2])
                            {
                                case 's': FuncSub(subValue, IsA ? Complex.Sinh : Complex.Arsh); tagL = IsA ? 2 : 3; break;
                                case 'c': FuncSub(subValue, IsA ? Complex.Cosh : Complex.Arch); tagL = IsA ? 2 : 3; break;
                                case 't': FuncSub(subValue, IsA ? Complex.Tanh : Complex.Arth); tagL = IsA ? 2 : 3; break;
                            }
                            break;
                        case 'a': FuncSub(subValue, Complex.ModulusComplex); tagL = 1; break;
                        case 'J': FuncSub(subValue, Complex.Conjugate); tagL = 1; break;
                        case 'l': FuncSub(subValue, Complex.Log); tagL = 1; break;
                        case 'E': FuncSub(subValue, Complex.Exp); tagL = 1; break;
                        case '#': FuncSub(subValue, Complex.Ei); tagL = 2; break; // Special for complex
                        case 'q': FuncSub(subValue, Complex.Sqrt); tagL = 1; break;
                        case '!': FuncSub(subValue, Complex.Factorial); tagL = 1; break;
                        default: break;
                    }
                }
                temp = MyString.Replace(temp, MyString.ConstructSubstitution(count), begin - tagL - 1, end);
                Parallel.For(0, row, r => {
                    fixed (Complex* bracketedPtr = &bracketed_values[count, r, 0], subPtr = &subValue[r, 0])
                    { Buffer.MemoryCopy(subPtr, bracketedPtr, columnSIZE, columnSIZE); }
                });
                count++;
            }
            return CalculateBracketFreeString(temp);
        }
        #endregion
    }
    public readonly struct Complex
    {
        public readonly double real = 0, imaginary = 0;
        public Complex(double real, double imaginary = 0.0) { this.real = real; this.imaginary = imaginary; }
        public static Complex operator -(Complex input)
            => new(-input.real, -input.imaginary);
        public static Complex operator +(Complex input_1, Complex input_2)
            => new(input_1.real + input_2.real, input_1.imaginary + input_2.imaginary);
        public static Complex operator -(Complex input_1, Complex input_2)
            => new(input_1.real - input_2.real, input_1.imaginary - input_2.imaginary);
        public static Complex operator *(Complex input_1, Complex input_2)
        {
            double real = input_1.real * input_2.real - input_1.imaginary * input_2.imaginary;
            double imaginary = input_1.real * input_2.imaginary + input_1.imaginary * input_2.real;
            return new(real, imaginary);
        }
        public static Complex operator /(Complex input_1, Complex input_2)
        {
            double modSquare = input_2.real * input_2.real + input_2.imaginary * input_2.imaginary;
            double real = (input_1.real * input_2.real + input_1.imaginary * input_2.imaginary) / modSquare;
            double imaginary = (input_1.imaginary * input_2.real - input_1.real * input_2.imaginary) / modSquare;
            return new(real, imaginary);
        }
        public static Complex operator ^(Complex input_1, Complex input_2) => Pow(input_1, input_2);
        public static Complex Pow(Complex input_1, Complex input_2)
        {
            if (input_1.real == 0 && input_1.imaginary == 0) return new(0);
            return Exp(input_2 * Log(input_1));
        } // Extremely sensitive, mustn't move a hair.
        public static Complex Log(Complex input)
            => new(Math.Log(Modulus(input)), Math.Atan2(input.imaginary, input.real));
        public static Complex Exp(Complex input)
        {
            double expReal = Math.Exp(input.real);
            return new(expReal * Math.Cos(input.imaginary), expReal * Math.Sin(input.imaginary));
        }
        public static Complex Ei(Complex input)
            => Exp(new Complex(0, Math.Tau) * input);
        public static Complex Sin(Complex input)
            => (Exp(new Complex(0, 1) * input) - Exp(-new Complex(0, 1) * input)) / new Complex(0, 2);
        public static Complex Cos(Complex input)
            => (Exp(new Complex(0, 1) * input) + Exp(-new Complex(0, 1) * input)) / new Complex(2);
        public static Complex Tan(Complex input)
            => Sin(input) / Cos(input);
        public static Complex Asin(Complex input)
            => new Complex(0, -1) * Log(new Complex(0, 1) * input + Sqrt(new Complex(1) - input * input));
        public static Complex Acos(Complex input)
            => new Complex(0, -1) * Log(input + new Complex(0, 1) * Sqrt(new Complex(1) - input * input));
        public static Complex Atan(Complex input)
            => Log((new Complex(0, 1) - input) / (new Complex(0, 1) + input)) / new Complex(0, 2);
        public static Complex Sinh(Complex input) => (Exp(input) - Exp(-input)) / new Complex(2);
        public static Complex Cosh(Complex input) => (Exp(input) + Exp(-input)) / new Complex(2);
        public static Complex Tanh(Complex input) => Sinh(input) / Cosh(input);
        public static Complex Arsh(Complex input) => Log(input + Sqrt(new Complex(1) + input * input));
        public static Complex Arch(Complex input) => Log(input + Sqrt(new Complex(-1) + input * input));
        public static Complex Arth(Complex input) => Log((new Complex(1) + input) / (new Complex(1) - input)) / new Complex(2);
        public static Complex Sqrt(Complex input) => input ^ new Complex(0.5);
        public static Complex ModulusComplex(Complex input) => new(Modulus(input));
        public static double Modulus(Complex input) => Math.Sqrt(input.real * input.real + input.imaginary * input.imaginary);
        public static Complex Conjugate(Complex input) => new(input.real, -input.imaginary);
        public static Complex Factorial(Complex input) => new(RealSubstitution.Factorial(input.real));
    }
}