using System.Buffers;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

using MathR = System.Math;
using Real = System.Double;

namespace Fraljiculator;

/// <summary>
/// TOOLKIT SECTION
/// </summary>
public class MyString
{
    private static string[] AddSuffix(string[] str) { for (int i = 0; i < str.Length; i++) str[i] += "("; return str; }
    public static readonly string[] FUNC = AddSuffix(["function", "Function", "func", "Func"]),
        POLAR = AddSuffix(["polar", "Polar"]), PARAM = AddSuffix(["parametric", "Parametric", "param", "Param"]);
    public static readonly string[] FPP_NAMES = [.. FUNC, .. POLAR, .. PARAM];

    #region Parentheses
    public static int FindMatchingParen(ReadOnlySpan<char> input, int start)
    {
        for (int i = start + 1, count = 1; ; i++)
        { if (input[i] == '(') count++; else if (input[i] == ')') count--; if (count == 0) return i; }
    }
    public static int FindMatchingParenBack(ReadOnlySpan<char> input, int start)
    {
        for (int i = start - 1, count = 1; ; i--)
        { if (input[i] == ')') count++; else if (input[i] == '(') count--; if (count == 0) return i; }
    }
    protected static bool ContainsAnyTopLevel(ReadOnlySpan<char> input, ReadOnlySpan<char> chars)
    {
        for (int i = 0; i < input.Length; i++)
        { if (input[i] == '(') i = FindMatchingParen(input, i); else if (chars.Contains(input[i])) return true; }
        return false;
    }
    public static string[] SplitTopLevel(ReadOnlySpan<char> input, ReadOnlySpan<char> delimiters)
    {
        List<string> split = []; int start = 0;
        for (int i = 0; i < input.Length; i++)
        {
            if (input[i] == '(') i = FindMatchingParen(input, i);
            else if (delimiters.Contains(input[i])) { split.Add(input[start..i].ToString()); start = i + 1; }
        }
        split.Add(input[start..].ToString()); return [.. split];
    }
    protected static (int, string[]) ParseSeriesCall(ReadOnlySpan<char> input)
    {
        int i = input.IndexOf(ReplaceTags.SERIES_TAIL), end = FindMatchingParen(input, i + 1);
        return (i, SplitTopLevel(ParenContent(input, i + 1, end), ","));
    }
    public static bool HasBalancedParen(ReadOnlySpan<char> input)
    { int sum = 0; foreach (char c in input) { if (c == '(') sum++; else if (c == ')') sum--; if (sum < 0) return false; } return sum == 0; }
    #endregion

    #region Replacement
    protected static ReadOnlySpan<char> ParenContent(ReadOnlySpan<char> input, int start, int end) => input[(start + 1)..end];
    protected static ReadOnlySpan<char> BraceContent(ReadOnlySpan<char> input)
    { ThrowException(input[0] != '{' || input[^1] != '}'); return input[1..^1]; }
    public static string Replace(string orig, string sub, int start, int end)
        => String.Create(start + sub.Length + orig.Length - end - 1, (start, end, sub.Length), (span, state) =>
        {
            var (_start, _end, _subLen) = state;
            orig[.._start].CopyTo(span);
            sub.CopyTo(span[_start..]);
            orig[(_end + 1)..].CopyTo(span[(_start + _subLen)..]);
        });
    public static string ReplaceLoop(ReadOnlySpan<string> split, int origIdx, int subIdx, string idxStr, bool wrapParen = false)
        => split[origIdx].Replace(split[subIdx], wrapParen ? String.Concat('(', idxStr, ')') : idxStr);
    protected static string RemoveWhitespace(string input)
    { foreach (string s in ImplicitMultiply.ENTER_BLANK) input = input.Replace(s, String.Empty); return input; }
    public static string BeautifyInput(string input) => RemoveWhitespace(input).Replace(",", ", ").Replace("|", " | ");
    #endregion

    #region Miscellaneous
    public static string[] SplitArguments(ReadOnlySpan<char> input)
        => SplitTopLevel(ParenContent(input, input.IndexOf('('), input.Length - 1), ",");
    public static string FormatNumber(Real input, Real threshold)
        => (MathR.Abs(input) < threshold && MathR.Abs(input) > 1 / threshold) ? input.ToString("#0.0000000") : input.ToString("E3");
    public static string FormatAngle(Real x, Real y) => (RealComplex.ArgRGB(x, y) / MathR.PI).ToString("#0.000000") + " π";
    public static void ThrowException(bool error = true) { if (error) throw new Exception(); }
    public static void ThrowInvalidLens(ReadOnlySpan<string> split, ReadOnlySpan<int> lengths)
        => ThrowException(!lengths.Contains(split.Length));
    protected static (int, int) GetIterationBounds(ReadOnlySpan<string> split, int length, int start, int iteration)
    {
        ThrowInvalidLens(split, [length, length + 1]);
        int end = split.Length == length ? iteration : RealSub.ToInt(split[^1]);
        ThrowException(start > end); return (start, end);
    }
    public static bool ContainsAny(ReadOnlySpan<char> input, ReadOnlySpan<string> stringsToCheck)
    {
        foreach (string s in stringsToCheck) if (input.IndexOf(s.AsSpan()) >= 0) return true;
        return false;
    }
    public static bool StartsWithTag(ReadOnlySpan<char> input, ReadOnlySpan<char> tag)
    {
        int end = input.IndexOf('(');
        return end == tag.Length + 2 && input[0] == ReplaceTags.FUNC_HEAD && input[end - 1] == ReplaceTags.SERIES_TAIL &&
            input[1..(1 + tag.Length)].SequenceEqual(tag);
    }
    #endregion
} /// Provides string-manipulation utilities
public class RealComplex : MyString
{
    protected static readonly Real GAMMA = (Real)0.5772156649015329, LOG2 = MathR.Log(2);
    protected static readonly int STEP = 1; // A tunable chunk size
    protected const char _A = 'a', A_ = 'A', B_ = 'B', _C = 'c', C_ = 'C', D_ = 'D', _D_ = '$', E = 'e', E_ = 'E',
        _F = 'f', F_ = 'F', _F_ = '!', G = 'γ', G_ = 'G', _H = 'h', H_ = 'H', I = 'i', I_ = 'I', J_ = 'J', K_ = 'K', _L = 'l',
        M_ = 'M', MAX = '>', MIN = '<', MODE_1 = '1', MODE_2 = '2', P = 'π', P_ = 'P', _Q = 'q', _R = 'r', R_ = 'R',
        _S = 's', S_ = 'S', SP = '#', _T = 't', TILDE = '~', _X = 'x', X_ = 'X', _Y = 'y', Y_ = 'Y', _Z = 'z', Z_ = 'Z', _Z_ = 'ζ';
    public static readonly string SUBS = "σ", ITLOOP = "ι", LOOP = "λ", _FUNC = "φ", _POLAR = "ψ", _PARAM = "ρ";

    protected uint colBytes, strdBytes, resBytes; // Chunk sizes in bytes
    protected int rows, columns, rowChk, strd, res, resInit; // Chunk lengths
    protected int[] rowOffs, strdInit; // For row extraction
    protected bool useList; // useList: whether to use cstMtcs
    protected int countCst; // countCst: counts cstMtcs
    protected bool readList; // Indicates whether cstMtcs is being read or written
    protected string input;

    public static Real ArgRGB(Real x, Real y) => Real.IsNaN(x) && Real.IsNaN(y) ? -1 : y == 0 ?
        (x == 0 ? -1 : x > 0 ? 0 : MathR.PI) : (y > 0 ? MathR.Atan2(y, x) : MathR.Atan2(y, x) + MathR.Tau);
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
    protected void ProcessChunks(Action<int, int> action)
    {
        if (rows == 1) { action(0, columns); return; }
        else { Parallel.For(0, rowChk, p => action(strdInit[p], strd)); if (res != 0) action(resInit, res); }
    }
    protected void ProcessCopyConst(Action<int, uint> action, bool isCopy)
    {
        if (rows == 1) { if (isCopy) action(0, colBytes); return; }
        else { Parallel.For(isCopy ? 0 : 1, rowChk, p => { action(strdInit[p], strdBytes); }); if (res != 0) action(resInit, resBytes); }
    }
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
    public static void ForEachInclusive(int start, int end, Action<int> action)
    { ThrowException(start > end); for (int i = start; i <= end; i++) action(i); }
    protected static Matrix<Real> ChooseMode((string mode, Matrix<Real> m1, Matrix<Real> m2) m)
    {
        if (!(m.mode.Length == 1 && (m.mode[0] == MODE_1 || m.mode[0] == MODE_2))) { m.m1.Return(); m.m2.Return(); ThrowException(); }
        var (selected, unused) = m.mode[0] == MODE_1 ? (m.m1, m.m2) : (m.m2, m.m1); unused.Return(); return selected;
    }
    protected static Matrix<TEntry> HandleMtx<TEntry>(Matrix<TEntry> mtx, Action<Matrix<TEntry>> action) { action(mtx); return mtx; }
    protected static MatrixCopy<TEntry> RequireSingleChar<TEntry>(ReadOnlySpan<char> input, MatrixCopy<TEntry> mc)
    { ThrowException(input.Length != 1); return mc; }
    protected static (string[], StringBuilder) SplitOperatorLevel(ReadOnlySpan<char> input, ReadOnlySpan<char> signs)
    {
        bool signHead = input[0] == signs[1];
        ThrowException(signHead && input.Length > 1 && input[1] == signs[1]);
        ReadOnlySpan<char> core = signHead ? input[1..] : input;
        StringBuilder result = new(core.Length + 1); result.Append(signHead ? signs[1] : signs[0]);
        for (int i = 0; i < core.Length; i++)
            if (core[i] == '(') i = FindMatchingParen(core, i); else if (signs.Contains(core[i])) result.Append(core[i]);
        return (SplitTopLevel(core, signs), result);
    }
    protected static (bool trig, bool hyper) GetInverseFlags(ReadOnlySpan<char> input, int start)
        => (start <= 1 || input[start - 2] == _A, start <= 2 || input[start - 3] == _A);
} /// Provides shared functionality for RealSub and ComplexSub
public class ReplaceTags : RealComplex
{
    public static readonly string[] FUNCTIONS =
        [ "floor", "ceiling", "round", "sign", "factorial", "mod", "nCr", "nPr", "max", "min", "distance", "conjugate", "ei",
            "blaschke", "real", "abs", "log", "exp", "sqrt", "arsinh", "arcosh", "artanh", "arcsin", "arccos", "arctan",
            "sinh", "cosh", "tanh", "sin", "cos", "tan", "hypergeo", "gamma", "beta", "zeta" ];
    public static readonly string[] SPECIALS =
        [ "stereographic", "homothety", "sum", "product", "iterate", "iterate1", "iterate2", "compose", "compose1", "compose2",
            "cocoon", "substitute", "iterateLoop", "loop", "function", "polar", "parametric" ];
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
    public static readonly string CONJ = ToS(J_), EI = EXP = ToS(E_), BLA = ToS(B_), _REAL = ToS(R_);
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
    private static readonly Dictionary<string, string> COMMON_SERIES = AddSuffix(SERIES_TAIL, new()
        {
            { "hypergeo", HYPGEO }, { "Hypergeo", HYPGEO }, { "hypgeo", HYPGEO }, { "Hypgeo", HYPGEO },
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
        });
    private static readonly Dictionary<string, string> COMMON = Concat(COMMON_SERIES, COMMON_STANDARD);
    private static readonly Dictionary<string, string> REAL_STANDARD = AddSuffix(REAL_TAIL, new()
        {
            { "floor", FLOOR }, { "Floor", FLOOR },
            { "ceiling", CEIL }, { "Ceiling", CEIL }, { "ceil", CEIL }, { "Ceil", CEIL },
            { "round", ROUND }, { "Round", ROUND },
            { "sign", SGN }, { "Sign", SGN }, { "sgn", SGN }, { "Sgn", SGN },
            { "factorial", FACT }, { "Factorial", FACT }, { "fact", FACT }, { "Fact", FACT }
        });
    private static readonly Dictionary<string, string> REAL_SERIES = AddSuffix(SERIES_TAIL, AddSuffix(REAL_TAIL, new()
        {
            { "mod", MOD }, { "Mod", MOD }, { "nCr", NCR }, { "nPr", NPR },
            { "max", _MAX }, { "Max", _MAX }, { "min", _MIN }, { "Min", _MIN },
            { "distance", DIST}, { "Distance", DIST}, { "dist", DIST}, { "Dist", DIST},
            { "iterate1", IT1 }, { "Iterate1", IT1 },
            { "compose1", COMP1 }, { "Compose1", COMP1 }, { "comp1", COMP1 }, { "Comp1", COMP1 }
        }));
    private static readonly Dictionary<string, string> REAL = Concat(REAL_SERIES, REAL_STANDARD);
    private static readonly Dictionary<string, string> COMPLEX_STANDARD = AddSuffix(COMPLEX_TAIL, new()
        {
            { "conjugate", CONJ }, { "Conjugate", CONJ }, { "conj", CONJ }, { "Conj", CONJ },
            { "ei", EI }, { "Ei", EI }
        });
    private static readonly Dictionary<string, string> COMPLEX_SERIES = AddSuffix(SERIES_TAIL, AddSuffix(COMPLEX_TAIL, new()
        {
            { "blaschke", BLA}, { "Blaschke", BLA}, { "bla", BLA}, { "Bla", BLA},
            { "real", _REAL }, { "Real", _REAL }
        }));
    private static readonly Dictionary<string, string> COMPLEX = Concat(COMPLEX_SERIES, COMPLEX_STANDARD);
    private static readonly Dictionary<string, string> CONSTANTS = new()
        {
            { "pi", PI }, { "Pi", PI },
            { "gamma", _GA }, { "Gamma", _GA }, { "ga", _GA }, { "Ga", _GA }
        };
    private static readonly Dictionary<string, string> TAGS = AddSuffix(SERIES_TAIL, new()
        {
            { "substitute", SUBS}, { "Substitute", SUBS}, { "subs", SUBS}, { "Subs", SUBS},
            { "iterateLoop", ITLOOP }, { "IterateLoop", ITLOOP }, { "itLoop", ITLOOP }, { "ItLoop", ITLOOP }, // Must precede "loop"
            { "loop", LOOP }, { "Loop", LOOP },
            { "function", _FUNC }, { "Function", _FUNC }, { "func", _FUNC }, { "Func", _FUNC },
            { "polar", _POLAR }, { "Polar", _POLAR },
            { "parametric", _PARAM }, { "Parametric", _PARAM }, { "param", _PARAM }, { "Param", _PARAM }
        });
    private static readonly Dictionary<string, string> REAL_COMPLEX = Concat(REAL, COMPLEX);
    private static Dictionary<string, string> AddBase(Action<Dictionary<string, string>> action)
    { Dictionary<string, string> _dictionary = []; action(_dictionary); return _dictionary; }
    private static Dictionary<string, string> AddPrefixSuffix(Dictionary<string, string> dictionary) => AddBase(_dictionary =>
    { foreach (var kvp in dictionary) _dictionary[String.Concat(kvp.Key, '(')] = String.Concat(FUNC_HEAD, kvp.Value, '('); });
    private static Dictionary<string, string> AddSuffix(char suffix, Dictionary<string, string> dictionary) => AddBase(_dictionary =>
    { foreach (var kvp in dictionary) _dictionary[kvp.Key] = String.Concat(kvp.Value, suffix); });
    private static string ReplaceBase(string input, Dictionary<string, string> dictionary)
    { foreach (var kvp in dictionary) input = input.Replace(kvp.Key, kvp.Value); return input; }
    private static string ReplaceConstant(string input) => ReplaceBase(input, CONSTANTS);
    private static string ReplaceCommon(string input) => ReplaceConstant(ReplaceBase(input, AddPrefixSuffix(COMMON)));
    protected static string NormalizeMathNames(string input) => ReplaceCommon(ReplaceBase(input, AddPrefixSuffix(REAL_COMPLEX)));
    protected static string NormalizeSpecialTags(string input) => ReplaceBase(input, AddPrefixSuffix(TAGS));
} /// Interprets function names
public class ImplicitMultiply : ReplaceTags
{
    public static readonly string EMPTY_PARENS = "()", EMPTY_BRACES = "{}",
        _ZZ_ = String.Concat(_Z, Z_), _XX__YY_ = String.Concat(_X, X_, _Y, Y_),
        _ZZ_BRACES = String.Concat(_ZZ_, EMPTY_BRACES), _XX__YY_BRACES = String.Concat(_XX__YY_, EMPTY_BRACES),
        BARRED_CHARS = String.Concat("\t!\"#$%&\':;<=>?@[\\]_`~", SUBS, ITLOOP, LOOP, _FUNC, _POLAR, _PARAM);
    private static readonly string VAR_REAL = _XX__YY_, VAR_COMPLEX = String.Concat(_ZZ_, I), CONST = String.Concat(E, P, G),
        ARITH = "+-*/^(,|", OPEN_BRACKETS = "({", CLOSE_BRACKETS = ")}";
    public static readonly string[] ENTER_BLANK = ["\n", "\r", " "];

    public static string NormalizeInput(string input)
    {
        ThrowException(!HasBalancedParen(input) || input.Contains(EMPTY_PARENS) || input.AsSpan().ContainsAny(BARRED_CHARS));
        return NormalizeMathNames(NormalizeSpecialTags(RemoveWhitespace(input))); // Sensitive
    }
    protected static string InsertImpMultiply(ReadOnlySpan<char> input, bool isComplex)
    {
        if (input.Length == 1) return input.ToString();
        Func<char, bool> isVar = isComplex ? IsVarComplex : IsVarReal;
        StringBuilder recoveredInput = new(input.Length * 2); // Maximum possible length
        recoveredInput.Append(input[0]);
        for (int i = 1; i < input.Length; i++) // Do not parallelize this loop
        {
            if (NeedsImpMultiply(input[i - 1], input[i], isVar)) recoveredInput.Append('*');
            recoveredInput.Append(input[i]);
        }
        return recoveredInput.ToString();
    } // Moved outside the loops
    private static bool NeedsImpMultiply(char c1, char c2, Func<char, bool> isVar)
    {
        bool isConstNum(char c) => IsConst(c) || Char.IsNumber(c);
        bool isConstVar(char c) => IsConst(c) || isVar(c);
        bool isConstNumVar(char c) => IsConst(c) || Char.IsNumber(c) || isVar(c);
        bool bNV = isConstNum(c1) && isConstVar(c2), bVN = isConstVar(c1) && isConstNum(c2), bVV = isVar(c1) && isVar(c2),
            bNVL = isConstNumVar(c1) && IsOpeningBracket(c2), bRNV = IsClosingBracket(c1) && isConstNumVar(c2), bRL = IsClosingBracket(c1) && IsOpeningBracket(c2),
            bAF = !IsArithmetic(c1) && IsFunctionHead(c2);
        return bNV || bVN || bVV || bNVL || bRNV || bRL || bAF;
    } // Sensitive
    private static bool IsVarReal(char c) => VAR_REAL.Contains(c);
    private static bool IsVarComplex(char c) => VAR_COMPLEX.Contains(c);
    private static bool IsConst(char c) => CONST.Contains(c);
    private static bool IsArithmetic(char c) => ARITH.Contains(c); // Functions after these operators are not multiplied
    private static bool IsFunctionHead(char c) => c == FUNC_HEAD;
    public static bool IsOpeningBracket(char c) => OPEN_BRACKETS.Contains(c);
    public static bool IsClosingBracket(char c) => CLOSE_BRACKETS.Contains(c);
} /// Restores omitted multiplication operators ("*")

/// <summary>
/// COMPUTATION SECTION
/// </summary>
public sealed class ComplexSub : ImplicitMultiply
{
    #region Fields & Constructors
    private readonly Matrix<Complex> z;
    private readonly Matrix<Complex>[] buffCocs; // Precomputes repeatedly used blocks
    private readonly List<ConstMatrix<Complex>> cstMtcs = []; // Stores reusable constant matrices
    private Matrix<Complex> Z; // For substitution

    public ComplexSub(ReadOnlySpan<char> input, Matrix<Complex>? z, Matrix<Complex>? Z, Matrix<Complex>[]? buffCocs,
        int rows, int columns, bool useList = false)
    {
        this.input = InsertImpMultiply(input, true);
        if (z != null) this.z = (Matrix<Complex>)z; if (Z != null) this.Z = (Matrix<Complex>)Z;
        this.rows = rows; this.columns = columns; this.useList = useList; this.buffCocs = buffCocs;
        Initialize<Complex>(rows, columns, ref rowChk, ref rowOffs, ref colBytes,
            ref strd, ref strdInit, ref strdBytes, ref res, ref resInit, ref resBytes);
    }
    public ComplexSub(ReadOnlySpan<char> input, Matrix<Real> xCoor, Matrix<Real> yCoor, int rows, int columns)
        : this(input, InitializeZ(xCoor, yCoor, rows, columns), null, null, rows, columns) { }
    private ComplexSub ObtainSub(ReadOnlySpan<char> input, Matrix<Complex>? Z, Matrix<Complex>[]? buffCocs, bool useList = false)
        => new(input, z, Z, buffCocs, rows, columns, useList);
    private Matrix<Complex> ObtainValue(ReadOnlySpan<char> input) => ObtainSub(input, Z, buffCocs).ObtainOwnScratch();
    private static Complex Obtain(ReadOnlySpan<char> input) => new ComplexSub(input, null, null, null, 1, 1).Obtain(false)[0, 0];
    #endregion

    #region Calculations
    private unsafe Matrix<Complex> Blaschke(string[] split) // Reference: https://en.wikipedia.org/wiki/Blaschke_product
        => HandleMtx(UninitMtx(true), output =>
        {
            ThrowInvalidLens(split, [2]);
            Matrix<Complex> initial1 = ObtainValue(split[0]), initial2 = ObtainValue(split[1]);
            ProcessChunks((p, col) =>
            {
                Complex* outputPtr = output.RowPtr(p), init1Ptr = initial1.RowPtr(p), init2Ptr = initial2.RowPtr(p);
                for (int q = 0; q < col; q++, outputPtr++, init1Ptr++, init2Ptr++)
                    *outputPtr = (*init1Ptr - *init2Ptr) / (1 - Complex.Conjugate(*init2Ptr) * *init1Ptr);
            });
            initial1.Return(); initial2.Return();
        });
    private unsafe Matrix<Complex> Hypergeometric(string[] split) // Reference: https://en.wikipedia.org/wiki/Hypergeometric_function
        => HandleMtx(Const(Complex.ZERO, true), sum =>
        {
            var (start, end) = GetIterationBounds(split, 4, 0, 100);
            Matrix<Complex> obtain(int index) => ObtainValue(split[index]);
            Matrix<Complex> a = obtain(0), b = obtain(1), c = obtain(2), initial = obtain(3);
            ProcessChunks((p, col) =>
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
            });
            a.Return(); b.Return(); c.Return(); initial.Return();
        });
    private unsafe Matrix<Complex> Gamma(string[] split) // Reference: https://en.wikipedia.org/wiki/Gamma_function
        => HandleMtx(UninitMtx(true), output =>
        {
            var (start, end) = GetIterationBounds(split, 1, 1, 100);
            Matrix<Complex> initial = ObtainValue(split[0]);
            ProcessChunks((p, col) =>
            {
                Complex* outputPtr = output.RowPtr(p), initialPtr = initial.RowPtr(p);
                for (int q = 0; q < col; q++, outputPtr++, initialPtr++)
                {
                    Complex product = Complex.ONE, temp;
                    for (int i = start; i <= end; i++) { temp = *initialPtr / i; product *= Complex.Exp(temp) / (1 + temp); }
                    *outputPtr = product * Complex.Exp(-*initialPtr * GAMMA) / *initialPtr;
                }
            });
            initial.Return();
        });
    private unsafe Matrix<Complex> Beta(string[] split) // Reference: https://en.wikipedia.org/wiki/Beta_function
        => HandleMtx(UninitMtx(true), output =>
        {
            var (start, end) = GetIterationBounds(split, 2, 1, 100);
            Matrix<Complex> initial1 = ObtainValue(split[0]), initial2 = ObtainValue(split[1]);
            ProcessChunks((p, col) =>
            {
                Complex* outputPtr = output.RowPtr(p), init1Ptr = initial1.RowPtr(p), init2Ptr = initial2.RowPtr(p);
                for (int q = 0; q < col; q++, outputPtr++, init1Ptr++, init2Ptr++)
                {
                    Complex product = Complex.ONE, initSum = *init1Ptr + *init2Ptr, initProd = *init1Ptr * *init2Ptr;
                    for (int i = start; i <= end; i++) product *= 1 + initProd / (i + initSum) / i;
                    *outputPtr = initSum / initProd / product;
                }
            });
            initial1.Return(); initial2.Return();
        });
    private unsafe Matrix<Complex> Zeta(string[] split) // Reference: https://en.wikipedia.org/wiki/Riemann_zeta_function
        => HandleMtx(Const(Complex.ZERO, true), sum =>
        {
            var (start, end) = GetIterationBounds(split, 1, 0, 50);
            Matrix<Complex> initial = ObtainValue(split[0]); var (coeffSeq, _coeffSeq, logSeq) = GetSeqsForZeta(start, end);
            ProcessChunks((p, col) =>
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
            });
            initial.Return();
        });
    private unsafe Matrix<Complex> ProcessSH(string[] split, Func<Complex, Real, Complex, Complex> function)
    {
        ThrowInvalidLens(split, [4]); Matrix<Complex> _z = UninitMtx(true);
        Real obtain(int i) => RealSub.Obtain(split[i]); Real r = obtain(0); Complex ctr = new(obtain(1), obtain(2));
        ProcessChunks((p, col) =>
        {
            Complex* zPtr = z.RowPtr(p), _zPtr = _z.RowPtr(p);
            for (int q = 0; q < col; q++, zPtr++, _zPtr++) *_zPtr = function(*zPtr, r, ctr);
        });
        Matrix<Complex> output = new ComplexSub(split[3], _z, Z, buffCocs, rows, columns).ObtainOwnScratch();
        _z.Return(); return output;
    }
    private Matrix<Complex> ProcessSPI(string[] split, int validLen, Matrix<Complex> initMtx, Action<ComplexSub> action,
        Action<int, Matrix<Complex>>? iterateAction = null)
    {
        ThrowInvalidLens(split, [validLen, validLen - 2]); bool sub = split.Length == validLen;
        int subIdx = validLen - 3; if (sub) split[0] = InsertImpMultiply(ReplaceLoop(split, 0, subIdx, split[subIdx], true), true);
        ComplexSub buffer = ObtainSub(sub ? ReplaceLoop(split, 0, subIdx, "0") : split[0], initMtx, buffCocs, true);

        ForEachInclusive(sub ? RealSub.ToInt(split[subIdx + 1]) : 1, RealSub.ToInt(split[sub ? subIdx + 2 : subIdx]), i =>
        {
            if (sub) buffer.input = ReplaceLoop(split, 0, subIdx, i.ToString()); buffer.countCst = 0;
            action(buffer); if (!buffer.readList) buffer.readList = true; iterateAction?.Invoke(i, buffer.Z);
        });
        return buffer.Z;
    } // Meticulously optimized
    private Matrix<Complex> ProcessI2C2(string[] split, Func<string[], (string, Matrix<Real>, Matrix<Real>)> function)
    {
        var (input, xCoor, yCoor) = function(split);
        Matrix<Complex> zCoor = InitializeZ(xCoor, yCoor, rows, columns, true); xCoor.Return(); yCoor.Return();
        Matrix<Complex> output = new ComplexSub(input, zCoor, null, null, rows, columns).ObtainOwnScratch();
        zCoor.Return(); return output;
    }
    private Matrix<Complex> Stereographic(string[] split) => ProcessSH(split, Complex.Stereographic);
    private Matrix<Complex> Homothety(string[] split) => ProcessSH(split, Complex.Homothety);
    private Matrix<Complex> ProcessSP(string[] split, Complex initial, Action<Matrix<Complex>, Matrix<Complex>> operation)
        => ProcessSPI(split, 4, Const(initial), b => { PoolOp(b.ObtainScratch(), b.Z, operation); });
    private Matrix<Complex> Sum(string[] split) => ProcessSP(split, Complex.ZERO, Plus);
    private Matrix<Complex> Product(string[] split) => ProcessSP(split, Complex.ONE, Multiply);
    public Matrix<Complex> Iterate(string[] split) => Iterate(split, null);
    public Matrix<Complex> Iterate(string[] split, Action<int, Matrix<Complex>>? iterateAction)
        => ProcessSPI(split, 5, ObtainValue(split[1]), b => { PoolSub(b, ref b.Z); }, iterateAction);
    private Matrix<Complex> Iterate2(string[] split) => ProcessI2C2(split, new RealSub("0", z, rows, columns).ProcessIterate2);
    private Matrix<Complex> Compose2(string[] split) => ProcessI2C2(split, new RealSub("0", z, rows, columns).ProcessCompose2);
    public Matrix<Complex> Compose(string[] split)
    {
        Matrix<Complex> value = ObtainValue(split[0]);
        for (int i = 1; i < split.Length; i++) PoolSub(ObtainSub(split[i], value, buffCocs), ref value);
        return value;
    } // Do not use HandleMtx
    private Matrix<Complex> Cocoon(string[] split)
    {
        ComplexSub body = ObtainSub(split[0], Z, new Matrix<Complex>[split.Length - 1]);
        for (int i = 1; i < split.Length; i++) body.buffCocs[i - 1] = ObtainValue(split[i]);
        Matrix<Complex> output = body.ObtainOwnScratch();
        foreach (var buffCoc in body.buffCocs) buffCoc.Return();
        return output;
    } // Used for shallow but complicated compositions
    private Matrix<Complex> RealBlock(string[] split) { ThrowInvalidLens(split, [1]); return Const(new(RealSub.Obtain(split[0])), true); }
    #endregion

    #region Elements
    public unsafe static Matrix<Complex> InitializeZ(Matrix<Real> xCoor, Matrix<Real> yCoor, int rows, int columns, bool pooled = false)
    {
        int[] rowOffs = GetArithProg(rows, columns);
        Matrix<Complex> zCoor = pooled ? Matrix<Complex>.Rent(rowOffs, columns) : new(rowOffs, columns);
        Parallel.For(0, rows, p =>
        {
            Complex* zCoorPtr = zCoor.RowPtr(p); Real* xCoorPtr = xCoor.RowPtr(p), yCoorPtr = yCoor.RowPtr(p);
            for (int q = 0; q < columns; q++, zCoorPtr++, xCoorPtr++, yCoorPtr++) *zCoorPtr = new(*xCoorPtr, *yCoorPtr);
        });
        return zCoor;
    }
    private unsafe Matrix<Complex> Copy(Matrix<Complex> src, bool pooled = false) => HandleMtx(UninitMtx(pooled), dest =>
        ProcessCopyConst((p, colBytes) => { Unsafe.CopyBlock(dest.RowPtr(p), src.RowPtr(p), colBytes); }, true));
    private unsafe Matrix<Complex> Const(Complex _const, bool pooled = false) => HandleMtx(UninitMtx(pooled), output =>
    {
        Complex* outputPtr = output.RowPtr(), _outputPtr = outputPtr;
        for (int q = 0; q < strd; q++, outputPtr++) *outputPtr = _const;
        ProcessCopyConst((p, colBytes) => { Unsafe.CopyBlock(output.RowPtr(p), _outputPtr, colBytes); }, false);
    }); // Sensitive
    private unsafe void Negate(Matrix<Complex> value) => ProcessChunks((p, col) =>
    {
        Complex* valuePtr = value.RowPtr(p);
        for (int q = 0; q < col; q++, valuePtr++) *valuePtr = -*valuePtr;
    });
    private unsafe void Invert(Matrix<Complex> value) => ProcessChunks((p, col) =>
    {
        Complex* valuePtr = value.RowPtr(p);
        for (int q = 0; q < col; q++, valuePtr++) *valuePtr = ~*valuePtr;
    });
    private unsafe void Plus(Matrix<Complex> src, Matrix<Complex> dest) => ProcessChunks((p, col) =>
    {
        Complex* destPtr = dest.RowPtr(p), srcPtr = src.RowPtr(p);
        for (int q = 0; q < col; q++, destPtr++, srcPtr++) *destPtr += *srcPtr;
    });
    private unsafe void Subtract(Matrix<Complex> src, Matrix<Complex> dest) => ProcessChunks((p, col) =>
    {
        Complex* destPtr = dest.RowPtr(p), srcPtr = src.RowPtr(p);
        for (int q = 0; q < col; q++, destPtr++, srcPtr++) *destPtr -= *srcPtr;
    });
    private unsafe void Multiply(Matrix<Complex> src, Matrix<Complex> dest) => ProcessChunks((p, col) =>
    {
        Complex* destPtr = dest.RowPtr(p), srcPtr = src.RowPtr(p);
        for (int q = 0; q < col; q++, destPtr++, srcPtr++) *destPtr *= *srcPtr;
    });
    private unsafe void Divide(Matrix<Complex> src, Matrix<Complex> dest) => ProcessChunks((p, col) =>
    {
        Complex* destPtr = dest.RowPtr(p), srcPtr = src.RowPtr(p);
        for (int q = 0; q < col; q++, destPtr++, srcPtr++) *destPtr /= *srcPtr;
    });
    private unsafe void Power(Matrix<Complex> src, Matrix<Complex> dest) => ProcessChunks((p, col) =>
    {
        Complex* destPtr = dest.RowPtr(p), srcPtr = src.RowPtr(p);
        for (int q = 0; q < col; q++, destPtr++, srcPtr++) *destPtr = Complex.Pow(*srcPtr, *destPtr);
    });
    private unsafe void FuncSub(Matrix<Complex> value, Func<Complex, Complex> function) => ProcessChunks((p, col) =>
    {
        Complex* valuePtr = value.RowPtr(p);
        for (int q = 0; q < col; q++, valuePtr++) *valuePtr = function(*valuePtr);
    });
    #endregion

    #region Assembly
    private Matrix<Complex> UninitMtx(bool pooled = false) => pooled ? Matrix<Complex>.Rent(rowOffs, columns) : new(rowOffs, columns);
    private Matrix<Complex> CopyMtx(MatrixCopy<Complex> mc, bool pooled = false) => mc.copy ? Copy(mc.matrix, pooled) : mc.matrix;
    private Matrix<Complex> FinalizeMtx(MatrixCopy<Complex> mc)
    {
        if (!mc.matrix.IsPooled()) return mc.matrix;
        Matrix<Complex> output = Copy(mc.matrix); if (!mc.copy) mc.matrix.Return(); return output;
    }
    private static void PoolSub(ComplexSub buffer, ref Matrix<Complex> mtx)
    { Matrix<Complex> _mtx = mtx; mtx = buffer.ObtainOwnScratch(); if (_mtx.IsPooled()) _mtx.Return(); }
    private static void PoolOp(MatrixCopy<Complex> mc, Matrix<Complex> dest, Action<Matrix<Complex>, Matrix<Complex>> operation)
    { operation(mc.matrix, dest); if (!mc.copy && mc.matrix.IsPooled()) mc.matrix.Return(); }
    private MatrixCopy<Complex> ConstMtx(Complex _const, bool pooled = false)
    {
        if (!useList) return new(Const(_const, pooled));
        if (!readList) { cstMtcs.Add(new(_const, Const(_const))); return new(cstMtcs[^1].matrix, true); }
        ConstMatrix<Complex> cm = cstMtcs[countCst];
        bool equal = _const.Equals(cm._const); Matrix<Complex> mtx = equal ? cm.matrix : Const(_const, pooled); countCst++;
        return new(mtx, equal);
    } // Cached constants must remain ordinary matrices
    private MatrixCopy<Complex> Evaluate(ReadOnlySpan<char> input, bool pooled = false) => input[0] switch
    {
        _Z => RequireSingleChar<Complex>(input, new(z, true)),
        Z_ => RequireSingleChar<Complex>(input, new(Z, true)),
        '{' => new(buffCocs[Int32.Parse(BraceContent(input))], true),
        I => RequireSingleChar(input, ConstMtx(Complex.I, pooled)),
        E => RequireSingleChar(input, ConstMtx(new(MathR.E), pooled)),
        P => RequireSingleChar(input, ConstMtx(new(MathR.PI), pooled)),
        G => RequireSingleChar(input, ConstMtx(new(GAMMA), pooled)),
        _ => ConstMtx(new(Real.Parse(input)), pooled)
    };
    private MatrixCopy<Complex> SeriesSub(ReadOnlySpan<char> input)
    {
        var (idx, split) = ParseSeriesCall(input);
        Func<string[], Matrix<Complex>> handleSub(Func<string[], Matrix<Complex>> func, int tagL, ReadOnlySpan<char> source)
        { ThrowException(source[idx - tagL] != FUNC_HEAD); return func; }
        Func<string[], Matrix<Complex>> braFunc = input[idx - 1] switch
        {
            F_ => handleSub(Hypergeometric, 2, input),
            G_ => handleSub(Gamma, 2, input),
            B_ => handleSub(Beta, 2, input),
            _Z_ => handleSub(Zeta, 2, input),
            R_ => handleSub(Stereographic, 2, input),
            H_ => handleSub(Homothety, 2, input),
            S_ => handleSub(Sum, 2, input),
            P_ => handleSub(Product, 2, input),
            I_ => input[idx - 2] switch { TILDE => handleSub(Iterate, 2, input), MODE_2 => handleSub(Iterate2, 3, input) },
            J_ => input[idx - 2] switch { TILDE => handleSub(Compose, 2, input), MODE_2 => handleSub(Compose2, 3, input) },
            K_ => handleSub(Cocoon, 2, input),
            SP => input[idx - 2] switch
            {
                B_ => handleSub(Blaschke, 3, input),
                R_ => handleSub(RealBlock, 3, input)
            } // Complex-specific
        };
        return new(braFunc(split));
    }
    private MatrixCopy<Complex> SubCore(ReadOnlySpan<char> input, int start, MatrixCopy<Complex> bFValue, bool pooled = false)
    {
        var (trig, hyper) = GetInverseFlags(input, start);
        MatrixCopy<Complex> handleSub(Func<Complex, Complex> func, int tagL, ReadOnlySpan<char> source)
        {
            ThrowException(source[start - tagL] != FUNC_HEAD);
            Matrix<Complex> mtx = CopyMtx(bFValue, pooled); FuncSub(mtx, func); return new(mtx);
        }
        return input[start - 1] switch
        {
            _A => handleSub(c => new(Complex.Modulus(c)), 2, input),
            _L => handleSub(Complex.Log, 2, input),
            E_ => handleSub(Complex.Exp, 2, input),
            _Q => handleSub(Complex.Sqrt, 2, input),
            _S => trig ? handleSub(Complex.Asin, 3, input) : handleSub(Complex.Sin, 2, input),
            _C => trig ? handleSub(Complex.Acos, 3, input) : handleSub(Complex.Cos, 2, input),
            _T => trig ? handleSub(Complex.Atan, 3, input) : handleSub(Complex.Tan, 2, input),
            _H => input[start - 2] switch
            {
                _S => hyper ? handleSub(Complex.Asinh, 4, input) : handleSub(Complex.Sinh, 3, input),
                _C => hyper ? handleSub(Complex.Acosh, 4, input) : handleSub(Complex.Cosh, 3, input),
                _T => hyper ? handleSub(Complex.Atanh, 4, input) : handleSub(Complex.Tanh, 3, input)
            },
            SP => input[start - 2] switch
            {
                J_ => handleSub(Complex.Conjugate, 3, input),
                E_ => handleSub(Complex.Ei, 3, input)
            } // Complex-specific
        };
    }
    private MatrixCopy<Complex> Transform(ReadOnlySpan<char> input, bool pooled = false)
    {
        int start = input.IndexOf('('); if (start < 0) return Evaluate(input, pooled);
        int end = FindMatchingParen(input, start); ThrowException(end != input.Length - 1);
        if (start > 0 && input[start - 1] == SERIES_TAIL) return SeriesSub(input);
        MatrixCopy<Complex> value = ObtainCore(ParenContent(input, start, end), true);
        return start == 0 ? value : SubCore(input, start, value, pooled);
    }
    private MatrixCopy<Complex> PowerCore(ReadOnlySpan<char> input, bool pooled = false)
    {
        if (!ContainsAnyTopLevel(input, "^")) return Transform(input, pooled);
        string[] split = SplitTopLevel(input, "^");
        Matrix<Complex> tower = CopyMtx(Transform(split[^1], pooled), pooled);
        for (int k = split.Length - 2; k >= 0; k--) PoolOp(Transform(split[k], true), tower, Power);
        return new(tower);
    }
    private MatrixCopy<Complex> MultiplyDivideCore(ReadOnlySpan<char> input, bool pooled = false)
    {
        if (!ContainsAnyTopLevel(input, "*/")) return PowerCore(input, pooled);
        var (split, signs) = SplitOperatorLevel(input, "*/");
        Matrix<Complex> product = CopyMtx(PowerCore(split[0], pooled), pooled); if (signs[0] == '/') Invert(product);
        for (int j = 1; j < split.Length; j++)
            PoolOp(PowerCore(split[j], true), product, signs[j] switch { '*' => Multiply, '/' => Divide });
        return new(product);
    }
    private MatrixCopy<Complex> PlusSubtractCore(ReadOnlySpan<char> input, bool pooled = false)
    {
        if (!ContainsAnyTopLevel(input, "+-")) return MultiplyDivideCore(input, pooled);
        var (split, signs) = SplitOperatorLevel(input, "+-");
        Matrix<Complex> sum = CopyMtx(MultiplyDivideCore(split[0], pooled), pooled); if (signs[0] == '-') Negate(sum);
        for (int i = 1; i < split.Length; i++)
            PoolOp(MultiplyDivideCore(split[i], true), sum, signs[i] switch { '+' => Plus, '-' => Subtract });
        return new(sum);
    }
    private MatrixCopy<Complex> ObtainCore(ReadOnlySpan<char> input, bool pooled = false)
        => Int32.TryParse(input, out int result) ? ConstMtx(new(result), pooled) : PlusSubtractCore(input, pooled);
    private MatrixCopy<Complex> ObtainScratch()
        => !input.AsSpan().ContainsAny(_ZZ_BRACES) ? new(Const(Obtain(input), true)) : ObtainCore(input, true);
    private Matrix<Complex> ObtainOwnScratch()
    { MatrixCopy<Complex> mc = ObtainScratch(); return !mc.copy && mc.matrix.IsPooled() ? mc.matrix : Copy(mc.matrix, true); }
    public Matrix<Complex> Obtain(bool checkVar = true)
        => checkVar && !input.AsSpan().ContainsAny(_ZZ_BRACES) ? Const(Obtain(input)) : FinalizeMtx(ObtainCore(input));
    #endregion
} /// Computes complex-variable expressions
public sealed class RealSub : ImplicitMultiply
{
    #region Fields & Constructors
    private readonly Matrix<Real> x, y;
    private readonly Matrix<Real>[] buffCocs; // Precomputes repeatedly used blocks
    private readonly List<ConstMatrix<Real>> cstMtcs = []; // Stores reusable constant matrices
    private Matrix<Real> X, Y; // For substitution

    public RealSub(ReadOnlySpan<char> input, Matrix<Real>? x, Matrix<Real>? y, Matrix<Real>? X, Matrix<Real>? Y, Matrix<Real>[]? buffCocs,
        int rows, int columns, bool useList = false)
    {
        this.input = InsertImpMultiply(input, false);
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
    private Matrix<Real> ObtainValue(ReadOnlySpan<char> input) => ObtainSub(input, X, Y, buffCocs).ObtainOwnScratch();
    public static Real Obtain(ReadOnlySpan<char> input, Real? x = null)
        => new RealSub(input, x != null ? new((Real)x) : null, null, null, null, null, 1, 1).Obtain(false)[0, 0];
    public static int ToInt(ReadOnlySpan<char> input) => (int)Obtain(input); // Often used with RealComplex.ForEachInclusive
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
            ThrowInvalidLens(split, [2]);
            Matrix<Real> initial1 = ObtainValue(split[0]), initial2 = ObtainValue(split[1]);
            ProcessChunks((p, col) =>
            {
                Real* outputPtr = output.RowPtr(p), init1Ptr = initial1.RowPtr(p), init2Ptr = initial2.RowPtr(p);
                for (int q = 0; q < col; q++, outputPtr++, init1Ptr++, init2Ptr++) *outputPtr = function(*init1Ptr, *init2Ptr);
            });
            initial1.Return(); initial2.Return();
        });
    private unsafe Matrix<Real> ProcessMMD(string[] split, Func<Real[], Real> function)
        => HandleMtx(UninitMtx(true), output =>
        {
            Matrix<Real>[] initials = new Matrix<Real>[split.Length];
            for (int i = 0; i < split.Length; i++) initials[i] = ObtainValue(split[i]);
            ProcessChunks((p, col) =>
            {
                Real[] array = new Real[split.Length]; Real* outputPtr = output.RowPtr(p);
                for (int q = 0; q < col; q++, outputPtr++)
                {
                    for (int i = 0; i < split.Length; i++) array[i] = initials[i][p, q];
                    *outputPtr = function(array);
                }
            });
            foreach (var initial in initials) initial.Return();
        });
    private Matrix<Real> Mod(string[] split) => ProcessMCP(split, Mod);
    private Matrix<Real> Combination(string[] split) => ProcessMCP(split, Combination);
    private Matrix<Real> Permutation(string[] split) => ProcessMCP(split, Permutation);
    private Matrix<Real> Max(string[] split) => ProcessMMD(split, array => array.Max());
    private Matrix<Real> Min(string[] split) => ProcessMMD(split, array => array.Min());
    private Matrix<Real> Distance(string[] split) => ProcessMMD(split, Distance);
    #endregion // Real-specific

    #region Additional Calculations
    private unsafe Matrix<Real> Hypergeometric(string[] split) // Reference: https://en.wikipedia.org/wiki/Hypergeometric_function
        => HandleMtx(Const(0, true), sum =>
        {
            var (start, end) = GetIterationBounds(split, 4, 0, 100);
            Matrix<Real> obtain(int index) => ObtainValue(split[index]);
            Matrix<Real> a = obtain(0), b = obtain(1), c = obtain(2), initial = obtain(3);
            ProcessChunks((p, col) =>
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
            });
            a.Return(); b.Return(); c.Return(); initial.Return();
        });
    private unsafe Matrix<Real> Gamma(string[] split) // Reference: https://en.wikipedia.org/wiki/Gamma_function
        => HandleMtx(UninitMtx(true), output =>
        {
            var (start, end) = GetIterationBounds(split, 1, 1, 100);
            Matrix<Real> initial = ObtainValue(split[0]);
            ProcessChunks((p, col) =>
            {
                Real* outputPtr = output.RowPtr(p), initialPtr = initial.RowPtr(p);
                for (int q = 0; q < col; q++, outputPtr++, initialPtr++)
                {
                    Real product = 1, temp;
                    for (int i = start; i <= end; i++) { temp = *initialPtr / i; product *= MathR.Exp(temp) / (1 + temp); }
                    *outputPtr = product * MathR.Exp(-*initialPtr * GAMMA) / *initialPtr;
                }
            });
            initial.Return();
        });
    private unsafe Matrix<Real> Beta(string[] split) // Reference: https://en.wikipedia.org/wiki/Beta_function
        => HandleMtx(UninitMtx(true), output =>
        {
            var (start, end) = GetIterationBounds(split, 2, 1, 100);
            Matrix<Real> initial1 = ObtainValue(split[0]), initial2 = ObtainValue(split[1]);
            ProcessChunks((p, col) =>
            {
                Real* outputPtr = output.RowPtr(p), init1Ptr = initial1.RowPtr(p), init2Ptr = initial2.RowPtr(p);
                for (int q = 0; q < col; q++, outputPtr++, init1Ptr++, init2Ptr++)
                {
                    Real product = 1, initSum = *init1Ptr + *init2Ptr, initProd = *init1Ptr * *init2Ptr;
                    for (int i = start; i <= end; i++) product *= 1 + initProd / (i + initSum) / i;
                    *outputPtr = initSum / initProd / product;
                }
            });
            initial1.Return(); initial2.Return();
        });
    private unsafe Matrix<Real> Zeta(string[] split) // Reference: https://en.wikipedia.org/wiki/Riemann_zeta_function
        => HandleMtx(Const(0, true), sum =>
        {
            var (start, end) = GetIterationBounds(split, 1, 0, 50);
            Matrix<Real> initial = ObtainValue(split[0]); var (coeffSeq, _coeffSeq, logSeq) = GetSeqsForZeta(start, end);
            ProcessChunks((p, col) =>
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
            });
            initial.Return();
        });
    private unsafe Matrix<Real> ProcessSH(string[] split, Func<Complex, Real, Complex, Complex> function)
    {
        ThrowInvalidLens(split, [4]); Matrix<Real> _x = UninitMtx(true), _y = UninitMtx(true);
        Real obtain(int i) => Obtain(split[i]); Real r = obtain(0); Complex ctr = new(obtain(1), obtain(2));
        ProcessChunks((p, col) =>
        {
            Real* xPtr = x.RowPtr(p), yPtr = y.RowPtr(p), _xPtr = _x.RowPtr(p), _yPtr = _y.RowPtr(p);
            for (int q = 0; q < col; q++, xPtr++, yPtr++, _xPtr++, _yPtr++)
                (*_xPtr, *_yPtr) = Complex.ReIm(function(new(*xPtr, *yPtr), r, ctr));
        });
        Matrix<Real> output = new RealSub(split[3], _x, _y, X, Y, buffCocs, rows, columns).ObtainOwnScratch();
        _x.Return(); _y.Return(); return output;
    }
    private Matrix<Real> ProcessSPI(string[] split, int validLen, Matrix<Real> initMtx, Action<RealSub> action,
        Action<int, Matrix<Real>>? iterateAction = null)
    {
        ThrowInvalidLens(split, [validLen, validLen - 2]); bool sub = split.Length == validLen;
        int subIdx = validLen - 3; if (sub) split[0] = InsertImpMultiply(ReplaceLoop(split, 0, subIdx, split[subIdx], true), false);
        RealSub buffer = ObtainSub(sub ? ReplaceLoop(split, 0, subIdx, "0") : split[0], initMtx, null, buffCocs, true);

        ForEachInclusive(sub ? ToInt(split[subIdx + 1]) : 1, ToInt(split[sub ? subIdx + 2 : subIdx]), i =>
        {
            if (sub) buffer.input = ReplaceLoop(split, 0, subIdx, i.ToString()); buffer.countCst = 0;
            action(buffer); if (!buffer.readList) buffer.readList = true; iterateAction?.Invoke(i, buffer.X);
        });
        return buffer.X;
    } // Meticulously optimized
    private Matrix<Real> ProcessIC(string[] split, Func<string[], Matrix<Complex>> function)
    {
        Matrix<Complex> zCoor = function(split[..^1]); var (xCoor, yCoor) = InitializeXY(zCoor, rows, columns, true); zCoor.Return();
        Matrix<Real> output = new RealSub(split[^1], xCoor, yCoor, null, null, null, rows, columns).ObtainOwnScratch();
        xCoor.Return(); yCoor.Return(); return output;
    }
    public (string, Matrix<Real>, Matrix<Real>) ProcessIterate2(string[] split) => ProcessIterate2(split, null);
    public (string, Matrix<Real>, Matrix<Real>) ProcessIterate2(string[] split, Action<int, Matrix<Real>, Matrix<Real>>? iterateAction)
    {
        ThrowInvalidLens(split, [8, 6]); bool sub = split.Length == 8;
        string replaceLoop(int i) => InsertImpMultiply(ReplaceLoop(split, i, 4, split[4], true), false);
        Matrix<Real> initialX = ObtainValue(split[2]), initialY = ObtainValue(split[3]);
        RealSub obtainSub(int i) => ObtainSub(sub ? ReplaceLoop(split, i, 4, "0") : split[i], initialX, initialY, buffCocs, true);
        if (sub) (split[0], split[1]) = (replaceLoop(0), replaceLoop(1)); var (buffer1, buffer2) = (obtainSub(0), obtainSub(1));

        ForEachInclusive(sub ? ToInt(split[5]) : 1, ToInt(split[sub ? 6 : 4]), i =>
        {
            if (sub) (buffer1.input, buffer2.input) = (ReplaceLoop(split, 0, 4, i.ToString()), ReplaceLoop(split, 1, 4, i.ToString()));
            buffer1.countCst = buffer2.countCst = 0;
            var (oldX, oldY) = (buffer1.X, buffer1.Y);
            var (newX, newY) = (buffer1.ObtainOwnScratch(), buffer2.ObtainOwnScratch()); // Necessary
            buffer1.X = buffer2.X = newX; buffer1.Y = buffer2.Y = newY;
            if (oldX.IsPooled()) oldX.Return(); if (oldY.IsPooled()) oldY.Return();
            if (!buffer1.readList) buffer1.readList = buffer2.readList = true; // Precomputes cstMtcs
            iterateAction?.Invoke(i, newX, newY);
        });
        return (split[^1], buffer1.X, buffer1.Y); // buffer2 would work as well
    }
    public (string, Matrix<Real>, Matrix<Real>) ProcessCompose2(string[] split)
    {
        ThrowException(Int32.IsEvenInteger(split.Length));
        var (value1, value2) = (ObtainValue(split[0]), ObtainValue(split[1]));
        for (int i = 0, j = 2; i < split.Length / 2 - 1; i++)
        {
            var (old1, old2) = (value1, value2);
            value1 = ObtainSub(split[j++], old1, old2, buffCocs).ObtainOwnScratch();
            value2 = ObtainSub(split[j++], old1, old2, buffCocs).ObtainOwnScratch();
            if (old1.IsPooled()) old1.Return(); if (old2.IsPooled()) old2.Return();
        }
        return (split[^1], value1, value2);
    }
    private Matrix<Real> Stereographic(string[] split) => ProcessSH(split, Complex.Stereographic);
    private Matrix<Real> Homothety(string[] split) => ProcessSH(split, Complex.Homothety);
    private Matrix<Real> ProcessSP(string[] split, Real initial, Action<Matrix<Real>, Matrix<Real>> operation)
        => ProcessSPI(split, 4, Const(initial), b => { PoolOp(b.ObtainScratch(), b.X, operation); });
    private Matrix<Real> Sum(string[] split) => ProcessSP(split, 0, Plus);
    private Matrix<Real> Product(string[] split) => ProcessSP(split, 1, Multiply);
    public Matrix<Real> Iterate1(string[] split) => Iterate1(split, null);
    public Matrix<Real> Iterate1(string[] split, Action<int, Matrix<Real>>? iterateAction)
        => ProcessSPI(split, 5, ObtainValue(split[1]), b => { PoolSub(b, ref b.X); }, iterateAction);
    private Matrix<Real> Iterate(string[] split) => ProcessIC(split, new ComplexSub("0", x, y, rows, columns).Iterate);
    private Matrix<Real> Compose(string[] split) => ProcessIC(split, new ComplexSub("0", x, y, rows, columns).Compose);
    private Matrix<Real> Iterate2(string[] split) => ChooseMode(ProcessIterate2(split));
    private Matrix<Real> Compose2(string[] split) => ChooseMode(ProcessCompose2(split));
    private Matrix<Real> Compose1(string[] split)
    {
        Matrix<Real> value = ObtainValue(split[0]);
        for (int i = 1; i < split.Length; i++) PoolSub(ObtainSub(split[i], value, null, buffCocs), ref value);
        return value;
    } // Do not use HandleMtx
    private Matrix<Real> Cocoon(string[] split)
    {
        RealSub body = ObtainSub(split[0], X, Y, new Matrix<Real>[split.Length - 1]);
        for (int i = 1; i < split.Length; i++) body.buffCocs[i - 1] = ObtainValue(split[i]);
        Matrix<Real> output = body.ObtainOwnScratch();
        foreach (var buffCoc in body.buffCocs) buffCoc.Return();
        return output;
    } // Used for shallow but complicated compositions
    #endregion

    #region Elements
    public unsafe static (Matrix<Real>, Matrix<Real>) InitializeXY(Matrix<Complex> zCoor, int rows, int columns, bool pooled = false)
    {
        int[] rowOffs = GetArithProg(rows, columns);
        Matrix<Real> xCoor = pooled ? Matrix<Real>.Rent(rowOffs, columns) : new(rowOffs, columns);
        Matrix<Real> yCoor = pooled ? Matrix<Real>.Rent(rowOffs, columns) : new(rowOffs, columns);
        Parallel.For(0, rows, p =>
        {
            Real* xCoorPtr = xCoor.RowPtr(p), yCoorPtr = yCoor.RowPtr(p); Complex* zCoorPtr = zCoor.RowPtr(p);
            for (int q = 0; q < columns; q++, xCoorPtr++, yCoorPtr++, zCoorPtr++) (*xCoorPtr, *yCoorPtr) = Complex.ReIm(*zCoorPtr);
        });
        return (xCoor, yCoor);
    }
    private unsafe Matrix<Real> Copy(Matrix<Real> src, bool pooled = false) => HandleMtx(UninitMtx(pooled), dest =>
        ProcessCopyConst((p, colBytes) => { Unsafe.CopyBlock(dest.RowPtr(p), src.RowPtr(p), colBytes); }, true));
    private unsafe Matrix<Real> Const(Real _const, bool pooled = false) => HandleMtx(UninitMtx(pooled), output =>
    {
        Real* outputPtr = output.RowPtr(), _outputPtr = outputPtr;
        for (int q = 0; q < strd; q++, outputPtr++) *outputPtr = _const;
        ProcessCopyConst((p, colBytes) => { Unsafe.CopyBlock(output.RowPtr(p), _outputPtr, colBytes); }, false);
    }); // Sensitive
    private unsafe void Negate(Matrix<Real> value) => ProcessChunks((p, col) =>
    {
        Real* valuePtr = value.RowPtr(p);
        for (int q = 0; q < col; q++, valuePtr++) *valuePtr = -*valuePtr;
    });
    private unsafe void Invert(Matrix<Real> value) => ProcessChunks((p, col) =>
    {
        Real* valuePtr = value.RowPtr(p);
        for (int q = 0; q < col; q++, valuePtr++) *valuePtr = 1 / *valuePtr;
    });
    private unsafe void Plus(Matrix<Real> src, Matrix<Real> dest) => ProcessChunks((p, col) =>
    {
        Real* destPtr = dest.RowPtr(p), srcPtr = src.RowPtr(p);
        for (int q = 0; q < col; q++, destPtr++, srcPtr++) *destPtr += *srcPtr;
    });
    private unsafe void Subtract(Matrix<Real> src, Matrix<Real> dest) => ProcessChunks((p, col) =>
    {
        Real* destPtr = dest.RowPtr(p), srcPtr = src.RowPtr(p);
        for (int q = 0; q < col; q++, destPtr++, srcPtr++) *destPtr -= *srcPtr;
    });
    private unsafe void Multiply(Matrix<Real> src, Matrix<Real> dest) => ProcessChunks((p, col) =>
    {
        Real* destPtr = dest.RowPtr(p), srcPtr = src.RowPtr(p);
        for (int q = 0; q < col; q++, destPtr++, srcPtr++) *destPtr *= *srcPtr;
    });
    private unsafe void Divide(Matrix<Real> src, Matrix<Real> dest) => ProcessChunks((p, col) =>
    {
        Real* destPtr = dest.RowPtr(p), srcPtr = src.RowPtr(p);
        for (int q = 0; q < col; q++, destPtr++, srcPtr++) *destPtr /= *srcPtr;
    });
    private unsafe void Power(Matrix<Real> src, Matrix<Real> dest) => ProcessChunks((p, col) =>
    {
        Real* destPtr = dest.RowPtr(p), srcPtr = src.RowPtr(p);
        for (int q = 0; q < col; q++, destPtr++, srcPtr++) *destPtr = MathR.Pow(*srcPtr, *destPtr);
    });
    private unsafe void FuncSub(Matrix<Real> value, Func<Real, Real> function) => ProcessChunks((p, col) =>
    {
        Real* valuePtr = value.RowPtr(p);
        for (int q = 0; q < col; q++, valuePtr++) *valuePtr = function(*valuePtr);
    });
    #endregion

    #region Assembly
    private Matrix<Real> UninitMtx(bool pooled = false) => pooled ? Matrix<Real>.Rent(rowOffs, columns) : new(rowOffs, columns);
    private Matrix<Real> CopyMtx(MatrixCopy<Real> mc, bool pooled = false) => mc.copy ? Copy(mc.matrix, pooled) : mc.matrix;
    private Matrix<Real> FinalizeMtx(MatrixCopy<Real> mc)
    {
        if (!mc.matrix.IsPooled()) return mc.matrix;
        Matrix<Real> output = Copy(mc.matrix); if (!mc.copy) mc.matrix.Return(); return output;
    }
    private static void PoolSub(RealSub buffer, ref Matrix<Real> mtx)
    { Matrix<Real> _mtx = mtx; mtx = buffer.ObtainOwnScratch(); if (_mtx.IsPooled()) _mtx.Return(); }
    private static void PoolOp(MatrixCopy<Real> mc, Matrix<Real> dest, Action<Matrix<Real>, Matrix<Real>> operation)
    { operation(mc.matrix, dest); if (!mc.copy && mc.matrix.IsPooled()) mc.matrix.Return(); }
    private MatrixCopy<Real> ConstMtx(Real _const, bool pooled = false)
    {
        if (!useList) return new(Const(_const, pooled));
        if (!readList) { cstMtcs.Add(new(_const, Const(_const))); return new(cstMtcs[^1].matrix, true); }
        ConstMatrix<Real> cm = cstMtcs[countCst];
        bool equal = _const.Equals(cm._const); Matrix<Real> mtx = equal ? cm.matrix : Const(_const, pooled); countCst++;
        return new(mtx, equal);
    } // Cached constants must remain ordinary matrices
    private MatrixCopy<Real> Evaluate(ReadOnlySpan<char> input, bool pooled = false) => input[0] switch
    {
        _X => RequireSingleChar<Real>(input, new(x, true)),
        _Y => RequireSingleChar<Real>(input, new(y, true)),
        X_ => RequireSingleChar<Real>(input, new(X, true)),
        Y_ => RequireSingleChar<Real>(input, new(Y, true)),
        '{' => new(buffCocs[Int32.Parse(BraceContent(input))], true),
        E => RequireSingleChar(input, ConstMtx(MathR.E, pooled)),
        P => RequireSingleChar(input, ConstMtx(MathR.PI, pooled)),
        G => RequireSingleChar(input, ConstMtx(GAMMA, pooled)),
        _ => ConstMtx(Real.Parse(input), pooled)
    };
    private MatrixCopy<Real> SeriesSub(ReadOnlySpan<char> input)
    {
        var (idx, split) = ParseSeriesCall(input);
        Func<string[], Matrix<Real>> handleSub(Func<string[], Matrix<Real>> func, int tagL, ReadOnlySpan<char> source)
        { ThrowException(source[idx - tagL] != FUNC_HEAD); return func; }
        Func<string[], Matrix<Real>> braFunc = input[idx - 1] switch
        {
            F_ => handleSub(Hypergeometric, 2, input),
            G_ => handleSub(Gamma, 2, input),
            B_ => handleSub(Beta, 2, input),
            _Z_ => handleSub(Zeta, 2, input),
            R_ => handleSub(Stereographic, 2, input),
            H_ => handleSub(Homothety, 2, input),
            S_ => handleSub(Sum, 2, input),
            P_ => handleSub(Product, 2, input),
            I_ => input[idx - 2] switch { TILDE => handleSub(Iterate, 2, input), MODE_2 => handleSub(Iterate2, 3, input) },
            J_ => input[idx - 2] switch { TILDE => handleSub(Compose, 2, input), MODE_2 => handleSub(Compose2, 3, input) },
            K_ => handleSub(Cocoon, 2, input),
            _D_ => input[idx - 2] switch
            {
                M_ => handleSub(Mod, 3, input),
                C_ => handleSub(Combination, 3, input),
                A_ => handleSub(Permutation, 3, input),
                MAX => handleSub(Max, 3, input),
                MIN => handleSub(Min, 3, input),
                D_ => handleSub(Distance, 3, input),
                I_ => handleSub(Iterate1, 4, input),
                J_ => handleSub(Compose1, 4, input)
            } // Real-specific
        };
        return new(braFunc(split));
    }
    private MatrixCopy<Real> SubCore(ReadOnlySpan<char> input, int start, MatrixCopy<Real> bFValue, bool pooled = false)
    {
        var (trig, hyper) = GetInverseFlags(input, start);
        MatrixCopy<Real> handleSub(Func<Real, Real> func, int tagL, ReadOnlySpan<char> source)
        {
            ThrowException(source[start - tagL] != FUNC_HEAD);
            Matrix<Real> mtx = CopyMtx(bFValue, pooled); FuncSub(mtx, func); return new(mtx);
        }
        return input[start - 1] switch
        {
            _A => handleSub(MathR.Abs, 2, input),
            _L => handleSub(MathR.Log, 2, input),
            E_ => handleSub(MathR.Exp, 2, input),
            _Q => handleSub(MathR.Sqrt, 2, input),
            _S => trig ? handleSub(MathR.Asin, 3, input) : handleSub(MathR.Sin, 2, input),
            _C => trig ? handleSub(MathR.Acos, 3, input) : handleSub(MathR.Cos, 2, input),
            _T => trig ? handleSub(MathR.Atan, 3, input) : handleSub(MathR.Tan, 2, input),
            _H => input[start - 2] switch
            {
                _S => hyper ? handleSub(MathR.Asinh, 4, input) : handleSub(MathR.Sinh, 3, input),
                _C => hyper ? handleSub(MathR.Acosh, 4, input) : handleSub(MathR.Cosh, 3, input),
                _T => hyper ? handleSub(MathR.Atanh, 4, input) : handleSub(MathR.Tanh, 3, input)
            },
            _D_ => input[start - 2] switch
            {
                _F => handleSub(MathR.Floor, 3, input),
                _C => handleSub(MathR.Ceiling, 3, input),
                _R => handleSub(MathR.Round, 3, input),
                _S => handleSub(SafeSign, 3, input),
                _F_ => handleSub(Factorial, 3, input)
            } // Real-specific
        };
    }
    private MatrixCopy<Real> Transform(ReadOnlySpan<char> input, bool pooled = false)
    {
        int start = input.IndexOf('('); if (start < 0) return Evaluate(input, pooled);
        int end = FindMatchingParen(input, start); ThrowException(end != input.Length - 1);
        if (start > 0 && input[start - 1] == SERIES_TAIL) return SeriesSub(input);
        MatrixCopy<Real> value = ObtainCore(ParenContent(input, start, end), true);
        return start == 0 ? value : SubCore(input, start, value, pooled);
    }
    private MatrixCopy<Real> PowerCore(ReadOnlySpan<char> input, bool pooled = false)
    {
        if (!ContainsAnyTopLevel(input, "^")) return Transform(input, pooled);
        string[] split = SplitTopLevel(input, "^");
        Matrix<Real> tower = CopyMtx(Transform(split[^1], pooled), pooled);
        for (int k = split.Length - 2; k >= 0; k--) PoolOp(Transform(split[k], true), tower, Power);
        return new(tower);
    }
    private MatrixCopy<Real> MultiplyDivideCore(ReadOnlySpan<char> input, bool pooled = false)
    {
        if (!ContainsAnyTopLevel(input, "*/")) return PowerCore(input, pooled);
        var (split, signs) = SplitOperatorLevel(input, "*/");
        Matrix<Real> product = CopyMtx(PowerCore(split[0], pooled), pooled); if (signs[0] == '/') Invert(product);
        for (int j = 1; j < split.Length; j++)
            PoolOp(PowerCore(split[j], true), product, signs[j] switch { '*' => Multiply, '/' => Divide });
        return new(product);
    }
    private MatrixCopy<Real> PlusSubtractCore(ReadOnlySpan<char> input, bool pooled = false)
    {
        if (!ContainsAnyTopLevel(input, "+-")) return MultiplyDivideCore(input, pooled);
        var (split, signs) = SplitOperatorLevel(input, "+-");
        Matrix<Real> sum = CopyMtx(MultiplyDivideCore(split[0], pooled), pooled); if (signs[0] == '-') Negate(sum);
        for (int i = 1; i < split.Length; i++)
            PoolOp(MultiplyDivideCore(split[i], true), sum, signs[i] switch { '+' => Plus, '-' => Subtract });
        return new(sum);
    }
    private MatrixCopy<Real> ObtainCore(ReadOnlySpan<char> input, bool pooled = false)
        => Int32.TryParse(input, out int result) ? ConstMtx(result, pooled) : PlusSubtractCore(input, pooled);
    private MatrixCopy<Real> ObtainScratch()
        => !input.AsSpan().ContainsAny(_XX__YY_BRACES) ? new(Const(Obtain(input), true)) : ObtainCore(input, true);
    private Matrix<Real> ObtainOwnScratch()
    { MatrixCopy<Real> mc = ObtainScratch(); return !mc.copy && mc.matrix.IsPooled() ? mc.matrix : Copy(mc.matrix, true); }
    public Matrix<Real> Obtain(bool checkVar = true)
        => checkVar && !input.AsSpan().ContainsAny(_XX__YY_BRACES) ? Const(Obtain(input)) : FinalizeMtx(ObtainCore(input));
    #endregion
} /// Computes real-variable expressions
public sealed class MatrixPoolLease<TEntry>(int length)
{
    public readonly TEntry[] array = ArrayPool<TEntry>.Shared.Rent(length);
    private int returned; // To make the lease double-return safe
    public void Return()
    {
        if (Interlocked.Exchange(ref returned, 1) != 0) return;
        ArrayPool<TEntry>.Shared.Return(array, RuntimeHelpers.IsReferenceOrContainsReferences<TEntry>());
    }
} /// Owns a rented ArrayPool buffer

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
