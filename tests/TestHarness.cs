using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Text;
using GenOffice.Core;

namespace GenOffice.Tests
{
    public static class SparseMatrixTestExtensions
    {
        public static void SetCellString(this SparseMatrix sm, uint r, uint c, string val)
        {
            if (sm == null) return;
            CellData cd = new CellData();
            cd.SetString(val);
            sm.SetCell(r, c, cd);
        }

        public static void SetCellNumber(this SparseMatrix sm, uint r, uint c, double val)
        {
            if (sm == null) return;
            CellData cd = new CellData();
            cd.SetNumber(val);
            sm.SetCell(r, c, cd);
        }

        public static void SetCellFormula(this SparseMatrix sm, uint r, uint c, string formula)
        {
            if (sm == null) return;
            CellData cd = new CellData();
            cd.SetFormula(formula);
            sm.SetCell(r, c, cd);
        }

        public static CellFormat GetFormat(this SparseMatrix sm, uint r, uint c)
        {
            if (sm == null) return new CellFormat();
            CellData cd = sm.GetCell(r, c);
            if (cd == null)
            {
                cd = new CellData();
                sm.SetCell(r, c, cd);
            }
            if (cd.Format == null) cd.Format = new CellFormat();
            return cd.Format;
        }

        public static void DeleteCell(this SparseMatrix sm, uint r, uint c)
        {
            if (sm == null) return;
            sm.SetCell(r, c, null);
        }

        public static void DeleteRow(this SparseMatrix sm, uint targetRow)
        {
            if (sm == null) return;
            List<KeyValuePair<ulong, CellData>> all = new List<KeyValuePair<ulong, CellData>>(sm.GetAllCells());
            foreach (var kvp in all)
            {
                var pos = SparseMatrix.DecodeKey(kvp.Key);
                uint r = pos.Item1;
                uint c = pos.Item2;
                if (r == targetRow)
                {
                    sm.ClearCell(r, c);
                }
                else if (r > targetRow)
                {
                    sm.ClearCell(r, c);
                    sm.SetCell(r - 1, c, kvp.Value);
                }
            }
        }

        public static int GetCellCount(this SparseMatrix sm)
        {
            if (sm == null) return 0;
            int count = 0;
            foreach (var kvp in sm.GetAllCells()) count++;
            return count;
        }
    }

    public enum Milestone
    {
        M1,
        M2,
        M3,
        M4,
        All
    }

    public enum TestTier
    {
        Tier1_Feature = 1,
        Tier2_Boundary = 2,
        Tier3_CrossFeature = 3,
        Tier4_RealWorld = 4
    }

    public enum TestStatus
    {
        Passed,
        Failed,
        Pending
    }

    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public class TestAttribute : Attribute
    {
    }

    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public class TierAttribute : Attribute
    {
        public TestTier Tier { get; private set; }
        public TierAttribute(TestTier tier)
        {
            Tier = tier;
        }
    }

    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public class FeatureAttribute : Attribute
    {
        public int FeatureId { get; private set; }
        public string FeatureName { get; private set; }
        public Milestone TargetMilestone { get; private set; }

        public FeatureAttribute(int featureId, string featureName, Milestone milestone)
        {
            FeatureId = featureId;
            FeatureName = featureName;
            TargetMilestone = milestone;
        }
    }

    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public class DescriptionAttribute : Attribute
    {
        public string Description { get; private set; }
        public DescriptionAttribute(string description)
        {
            Description = description;
        }
    }

    public class TestFailureException : Exception
    {
        public TestFailureException(string message) : base(message) { }
    }

    public class TestPendingException : Exception
    {
        public string MilestoneReason { get; private set; }
        public TestPendingException(string reason) : base(reason)
        {
            MilestoneReason = reason;
        }
    }

    public static class Assert
    {
        public static void AreEqual(object expected, object actual, string message = null)
        {
            if (expected == null && actual == null) return;
            if (expected != null && expected.Equals(actual)) return;

            string expectedStr = expected != null ? expected.ToString() : "null";
            string actualStr = actual != null ? actual.ToString() : "null";
            string msg = string.Format("Expected: <{0}>, Actual: <{1}>.{2}",
                expectedStr, actualStr, !string.IsNullOrEmpty(message) ? " " + message : "");
            throw new TestFailureException(msg);
        }

        public static void AreEqual(double expected, double actual, double delta, string message = null)
        {
            if (Math.Abs(expected - actual) <= delta) return;
            string msg = string.Format("Expected: <{0}> +/- {1}, Actual: <{2}>.{3}",
                expected, delta, actual, !string.IsNullOrEmpty(message) ? " " + message : "");
            throw new TestFailureException(msg);
        }

        public static void IsTrue(bool condition, string message = null)
        {
            if (!condition)
            {
                string msg = string.Format("Expected: True, Actual: False.{0}",
                    !string.IsNullOrEmpty(message) ? " " + message : "");
                throw new TestFailureException(msg);
            }
        }

        public static void IsFalse(bool condition, string message = null)
        {
            if (condition)
            {
                string msg = string.Format("Expected: False, Actual: True.{0}",
                    !string.IsNullOrEmpty(message) ? " " + message : "");
                throw new TestFailureException(msg);
            }
        }

        public static void IsNotNull(object obj, string message = null)
        {
            if (obj == null)
            {
                string msg = string.Format("Expected non-null object, but was null.{0}",
                    !string.IsNullOrEmpty(message) ? " " + message : "");
                throw new TestFailureException(msg);
            }
        }

        public static void IsNull(object obj, string message = null)
        {
            if (obj != null)
            {
                string msg = string.Format("Expected null object, but was <{0}>.{1}",
                    obj, !string.IsNullOrEmpty(message) ? " " + message : "");
                throw new TestFailureException(msg);
            }
        }

        public static void Contains(string expectedSubstring, string actualString, string message = null)
        {
            if (actualString == null || !actualString.Contains(expectedSubstring))
            {
                string msg = string.Format("Expected string containing <{0}>, but got <{1}>.{2}",
                    expectedSubstring ?? "null", actualString ?? "null", !string.IsNullOrEmpty(message) ? " " + message : "");
                throw new TestFailureException(msg);
            }
        }

        public static void Fail(string message)
        {
            throw new TestFailureException(message ?? "Explicit test failure triggered.");
        }

        public static void Pending(string reason)
        {
            throw new TestPendingException(reason);
        }
    }

    public class TestResult
    {
        public string MethodName { get; set; }
        public string ClassName { get; set; }
        public int FeatureId { get; set; }
        public string FeatureName { get; set; }
        public Milestone TargetMilestone { get; set; }
        public TestTier Tier { get; set; }
        public string Description { get; set; }
        public TestStatus Status { get; set; }
        public string ErrorMessage { get; set; }
        public string StackTrace { get; set; }
        public TimeSpan Duration { get; set; }
    }

    public static class ContractHelper
    {
        public static Assembly GenOfficeAssembly { get; set; }

        static ContractHelper()
        {
            foreach (Assembly asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                string name = asm.GetName().Name;
                if (name.Equals("ZenQOffice", StringComparison.OrdinalIgnoreCase) ||
                    name.Equals("GenOffice", StringComparison.OrdinalIgnoreCase))
                {
                    GenOfficeAssembly = asm;
                    break;
                }
            }
            if (GenOfficeAssembly == null)
            {
                try
                {
                    string baseDir = AppDomain.CurrentDomain.BaseDirectory;
                    string[] candidates = new string[]
                    {
                        Path.Combine(baseDir, "ZenQOffice.dll"),
                        Path.Combine(baseDir, "ZenQOffice.exe"),
                        Path.Combine(baseDir, "GenOffice.dll"),
                        Path.Combine(baseDir, "GenOffice.exe"),
                        Path.Combine(Path.GetFullPath(Path.Combine(baseDir, "..")), "ZenQOffice.dll"),
                        Path.Combine(Path.GetFullPath(Path.Combine(baseDir, "..")), "ZenQOffice.exe"),
                        Path.Combine(Path.GetFullPath(Path.Combine(baseDir, "..")), "GenOffice.dll"),
                        Path.Combine(Path.GetFullPath(Path.Combine(baseDir, "..")), "GenOffice.exe")
                    };
                    foreach (string cand in candidates)
                    {
                        if (File.Exists(cand))
                        {
                            GenOfficeAssembly = Assembly.LoadFrom(cand);
                            break;
                        }
                    }
                }
                catch { }
            }
        }

        public static Type FindType(string typeFullName)
        {
            if (GenOfficeAssembly != null)
            {
                Type t = GenOfficeAssembly.GetType(typeFullName);
                if (t != null) return t;
            }
            return Type.GetType(typeFullName);
        }

        private static readonly HashSet<string> _activeFormulas = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "SUM", "AVERAGE", "SUMPRODUCT", "COUNT", "COUNTA", "MIN", "MAX", "PRODUCT",
            "ROUND", "ROUNDUP", "ROUNDDOWN", "ABS", "SQRT", "POWER", "IF", "AND", "OR",
            "NOT", "CHOOSE", "COUNTIF", "SUMIF", "AVERAGEIF", "MOD", "INT", "TRUNC",
            "MEDIAN", "LARGE", "SMALL", "PROPER", "SUBSTITUTE", "EXACT", "DATE", "TIME",
            "CONCATENATE", "CONCAT", "LEFT", "RIGHT", "MID", "LEN", "UPPER", "LOWER",
            "TRIM", "TODAY", "NOW", "VLOOKUP", "HLOOKUP", "XLOOKUP", "INDEX", "MATCH",
            "OFFSET", "INDIRECT", "IFS", "XOR", "SWITCH", "IFERROR", "TEXTJOIN", "TEXT",
            "DAY", "MONTH", "YEAR", "HOUR", "MINUTE", "SECOND",
            "ROW", "COLUMN", "IFNA", "WEEKDAY", "EDATE", "NETWORKDAYS", "DATEDIF",
            "VALUE", "FIND", "SEARCH", "TEXTBEFORE", "TEXTAFTER", "CHAR", "CODE", "REPT",
            "RANDBETWEEN", "CEILING", "FLOOR", "SIGN", "STDEV", "VAR", "RANK", "MODE",
            "ISNUMBER", "ISTEXT", "ISBLANK", "ISERROR", "ISNA"
        };

        public static bool IsFormulaFunctionImplemented(string funcName)
        {
            if (string.IsNullOrEmpty(funcName)) return false;
            return _activeFormulas.Contains(funcName);
        }

        public static bool MethodExists(Type type, string methodName)
        {
            if (type == null) return false;
            MethodInfo[] methods = type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
            for (int i = 0; i < methods.Length; i++)
            {
                if (methods[i].Name.Equals(methodName, StringComparison.OrdinalIgnoreCase))
                    return true;
            }
            return false;
        }

        public static object InvokeStatic(Type type, string methodName, object[] args)
        {
            if (type == null)
                throw new ArgumentNullException("type");
            MethodInfo m = type.GetMethod(methodName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
            if (m == null)
                throw new MissingMethodException(type.FullName, methodName);
            return m.Invoke(null, args);
        }

        public static object InvokeInstance(object target, string methodName, object[] args)
        {
            if (target == null)
                throw new ArgumentNullException("target");
            Type type = target.GetType();
            MethodInfo m = null;
            MethodInfo[] methods = type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            int argCount = args != null ? args.Length : 0;
            foreach (MethodInfo candidate in methods)
            {
                if (candidate.Name == methodName && candidate.GetParameters().Length == argCount)
                {
                    m = candidate;
                    break;
                }
            }
            if (m == null)
            {
                m = type.GetMethod(methodName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            }
            if (m == null)
                throw new MissingMethodException(type.FullName, methodName);
            return m.Invoke(target, args);
        }

        public static object GetProperty(object target, string propName)
        {
            if (target == null) return null;
            PropertyInfo pi = target.GetType().GetProperty(propName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            return pi != null ? pi.GetValue(target, null) : null;
        }
    }

    public class TestHarness
    {
        private List<TestResult> _results = new List<TestResult>();

        public void AddResult(TestResult res)
        {
            _results.Add(res);
        }

        public List<TestResult> Results
        {
            get { return _results; }
        }

        public void RunAll(Type[] testClasses)
        {
            _results.Clear();
            Console.WriteLine("===============================================================================");
            Console.WriteLine("                ZENQ CALC 4-TIER E2E TEST SUITE RUNNER                         ");
            Console.WriteLine("===============================================================================");
            Console.WriteLine("Target Environment: .NET Framework 4.8 / C# 5.0 Strict Compliance");
            Console.WriteLine("Execution Time:     " + DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ"));
            Console.WriteLine("===============================================================================\n");

            Stopwatch totalSw = Stopwatch.StartNew();

            for (int c = 0; c < testClasses.Length; c++)
            {
                Type cls = testClasses[c];
                object instance = Activator.CreateInstance(cls);
                MethodInfo[] methods = cls.GetMethods(BindingFlags.Public | BindingFlags.Instance);

                for (int m = 0; m < methods.Length; m++)
                {
                    MethodInfo method = methods[m];
                    object[] testAttrs = method.GetCustomAttributes(typeof(TestAttribute), false);
                    if (testAttrs.Length == 0) continue;

                    TestTier tier = TestTier.Tier1_Feature;
                    object[] tierAttrs = method.GetCustomAttributes(typeof(TierAttribute), false);
                    if (tierAttrs.Length > 0) tier = ((TierAttribute)tierAttrs[0]).Tier;

                    int featId = 0;
                    string featName = "General";
                    Milestone ms = Milestone.M1;
                    object[] featAttrs = method.GetCustomAttributes(typeof(FeatureAttribute), false);
                    if (featAttrs.Length > 0)
                    {
                        FeatureAttribute fa = (FeatureAttribute)featAttrs[0];
                        featId = fa.FeatureId;
                        featName = fa.FeatureName;
                        ms = fa.TargetMilestone;
                    }

                    string desc = "";
                    object[] descAttrs = method.GetCustomAttributes(typeof(DescriptionAttribute), false);
                    if (descAttrs.Length > 0) desc = ((DescriptionAttribute)descAttrs[0]).Description;

                    TestResult res = new TestResult();
                    res.MethodName = method.Name;
                    res.ClassName = cls.Name;
                    res.Tier = tier;
                    res.FeatureId = featId;
                    res.FeatureName = featName;
                    res.TargetMilestone = ms;
                    res.Description = desc;

                    Stopwatch sw = Stopwatch.StartNew();
                    try
                    {
                        method.Invoke(instance, null);
                        sw.Stop();
                        res.Status = TestStatus.Passed;
                        res.Duration = sw.Elapsed;
                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.Write("[PASS] ");
                        Console.ResetColor();
                        Console.WriteLine(string.Format("[T{0}|F{1:D2}|{2}] {3}", (int)tier, featId, ms, method.Name));
                    }
                    catch (TargetInvocationException ex)
                    {
                        sw.Stop();
                        res.Duration = sw.Elapsed;
                        if (ex.InnerException is TestPendingException)
                        {
                            TestPendingException tpe = (TestPendingException)ex.InnerException;
                            res.Status = TestStatus.Pending;
                            res.ErrorMessage = tpe.MilestoneReason;
                            Console.ForegroundColor = ConsoleColor.Yellow;
                            Console.Write("[PEND] ");
                            Console.ResetColor();
                            Console.WriteLine(string.Format("[T{0}|F{1:D2}|{2}] {3} (Pending: {4})", (int)tier, featId, ms, method.Name, tpe.MilestoneReason));
                        }
                        else
                        {
                            res.Status = TestStatus.Failed;
                            res.ErrorMessage = ex.InnerException != null ? ex.InnerException.Message : ex.Message;
                            res.StackTrace = ex.InnerException != null ? ex.InnerException.StackTrace : ex.StackTrace;
                            Console.ForegroundColor = ConsoleColor.Red;
                            Console.Write("[FAIL] ");
                            Console.ResetColor();
                            Console.WriteLine(string.Format("[T{0}|F{1:D2}|{2}] {3} (Error: {4})", (int)tier, featId, ms, method.Name, res.ErrorMessage));
                        }
                    }
                    catch (Exception ex)
                    {
                        sw.Stop();
                        res.Duration = sw.Elapsed;
                        res.Status = TestStatus.Failed;
                        res.ErrorMessage = ex.Message;
                        res.StackTrace = ex.StackTrace;
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.Write("[FAIL] ");
                        Console.ResetColor();
                        Console.WriteLine(string.Format("[T{0}|F{1:D2}|{2}] {3} (Error: {4})", (int)tier, featId, ms, method.Name, res.ErrorMessage));
                    }

                    _results.Add(res);
                }
            }

            totalSw.Stop();
            PrintSummary(totalSw.Elapsed);
        }

        private void PrintSummary(TimeSpan totalDuration)
        {
            int total = _results.Count;
            int passed = 0;
            int failed = 0;
            int pending = 0;

            for (int i = 0; i < total; i++)
            {
                if (_results[i].Status == TestStatus.Passed) passed++;
                else if (_results[i].Status == TestStatus.Failed) failed++;
                else if (_results[i].Status == TestStatus.Pending) pending++;
            }

            Console.WriteLine("\n===============================================================================");
            Console.WriteLine("                           TEST EXECUTION SUMMARY                              ");
            Console.WriteLine("===============================================================================");

            // Tier Summary
            Console.WriteLine("\n--- TIER BREAKDOWN ---");
            Console.WriteLine(string.Format("{0,-25} | {1,8} | {2,8} | {3,8} | {4,8}", "Tier", "Total", "Passed", "Failed", "Pending"));
            Console.WriteLine(new string('-', 68));
            for (int t = 1; t <= 4; t++)
            {
                TestTier tier = (TestTier)t;
                int tTotal = 0, tPass = 0, tFail = 0, tPend = 0;
                for (int i = 0; i < total; i++)
                {
                    if (_results[i].Tier == tier)
                    {
                        tTotal++;
                        if (_results[i].Status == TestStatus.Passed) tPass++;
                        else if (_results[i].Status == TestStatus.Failed) tFail++;
                        else if (_results[i].Status == TestStatus.Pending) tPend++;
                    }
                }
                string tierName = string.Format("Tier {0}: {1}", t, GetTierDisplayName(tier));
                Console.WriteLine(string.Format("{0,-25} | {1,8} | {2,8} | {3,8} | {4,8}", tierName, tTotal, tPass, tFail, tPend));
            }

            // Milestone Summary
            Console.WriteLine("\n--- MILESTONE BREAKDOWN ---");
            Console.WriteLine(string.Format("{0,-25} | {1,8} | {2,8} | {3,8} | {4,8}", "Milestone", "Total", "Passed", "Failed", "Pending"));
            Console.WriteLine(new string('-', 68));
            Milestone[] milestones = new Milestone[] { Milestone.M1, Milestone.M2, Milestone.M3, Milestone.M4 };
            for (int m = 0; m < milestones.Length; m++)
            {
                Milestone ms = milestones[m];
                int mTotal = 0, mPass = 0, mFail = 0, mPend = 0;
                for (int i = 0; i < total; i++)
                {
                    if (_results[i].TargetMilestone == ms)
                    {
                        mTotal++;
                        if (_results[i].Status == TestStatus.Passed) mPass++;
                        else if (_results[i].Status == TestStatus.Failed) mFail++;
                        else if (_results[i].Status == TestStatus.Pending) mPend++;
                    }
                }
                string msName = string.Format("Milestone {0}", ms);
                Console.WriteLine(string.Format("{0,-25} | {1,8} | {2,8} | {3,8} | {4,8}", msName, mTotal, mPass, mFail, mPend));
            }

            // Feature Coverage Summary (Features 1 to 32)
            Console.WriteLine("\n--- FEATURE COVERAGE MATRIX (Features 1 to 32) ---");
            Console.WriteLine(string.Format("{0,3} | {0,-32} | {1,4} | {2,4} | {3,4} | {4,4} | {5,5}", "F#", "T1", "T2", "T3", "T4", "Total"));
            Console.WriteLine(new string('-', 64));
            for (int f = 1; f <= 32; f++)
            {
                int t1 = 0, t2 = 0, t3 = 0, t4 = 0;
                string fName = "";
                for (int i = 0; i < total; i++)
                {
                    if (_results[i].FeatureId == f)
                    {
                        fName = _results[i].FeatureName;
                        if (_results[i].Tier == TestTier.Tier1_Feature) t1++;
                        else if (_results[i].Tier == TestTier.Tier2_Boundary) t2++;
                        else if (_results[i].Tier == TestTier.Tier3_CrossFeature) t3++;
                        else if (_results[i].Tier == TestTier.Tier4_RealWorld) t4++;
                    }
                }
                int fTotal = t1 + t2 + t3 + t4;
                string truncatedName = fName.Length > 28 ? fName.Substring(0, 25) + "..." : fName;
                Console.WriteLine(string.Format("{0,2:D2} | {1,-28} | {2,4} | {3,4} | {4,4} | {5,4} | {6,5}",
                    f, truncatedName, t1, t2, t3, t4, fTotal));
            }

            Console.WriteLine(new string('-', 68));
            Console.WriteLine(string.Format("TOTAL TESTS RUN: {0} (Passed: {1}, Failed: {2}, Pending: {3})",
                total, passed, failed, pending));
            Console.WriteLine(string.Format("TOTAL TIME:      {0:F2} seconds", totalDuration.TotalSeconds));

            if (failed > 0)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("\nVERDICT: FAIL - Regressions or assertion failures detected!");
                Console.ResetColor();
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("\nVERDICT: SUCCESS - All active test assertions verified cleanly!");
                Console.ResetColor();
            }
            Console.WriteLine("===============================================================================\n");
        }

        private static string GetTierDisplayName(TestTier tier)
        {
            switch (tier)
            {
                case TestTier.Tier1_Feature: return "Feature / Category-Partition";
                case TestTier.Tier2_Boundary: return "Boundary Value Analysis";
                case TestTier.Tier3_CrossFeature: return "Pairwise Combinatorial";
                case TestTier.Tier4_RealWorld: return "Real-World Workloads";
                default: return "Unknown";
            }
        }
    }

    class Program
    {
        [STAThread]
        public static int Main(string[] args)
        {
            try { System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance); } catch { }
            // Register assembly resolution hook for .NET 4.8
            AppDomain.CurrentDomain.AssemblyResolve += delegate(object sender, ResolveEventArgs resolveArgs)
            {
                try
                {
                    string shortName = new AssemblyName(resolveArgs.Name).Name;
                    string[] searchDirs = new string[]
                    {
                        AppDomain.CurrentDomain.BaseDirectory,
                        Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..")),
                        Directory.GetCurrentDirectory()
                    };

                    foreach (string dir in searchDirs)
                    {
                        string dllPath = Path.Combine(dir, shortName + ".dll");
                        if (File.Exists(dllPath)) return Assembly.LoadFrom(dllPath);

                        string exePath = Path.Combine(dir, shortName + ".exe");
                        if (File.Exists(exePath)) return Assembly.LoadFrom(exePath);
                    }
                }
                catch { }
                return null;
            };

            TestHarness harness = new TestHarness();
            Type[] testSuites = new Type[]
            {
                typeof(Tier1FeatureTests),
                typeof(Tier2BoundaryTests),
                typeof(Tier3CrossFeatureTests),
                typeof(Tier4RealWorldTests)
            };

            harness.RunAll(testSuites);

            int failedCount = 0;
            for (int i = 0; i < harness.Results.Count; i++)
            {
                if (harness.Results[i].Status == TestStatus.Failed) failedCount++;
            }

            return failedCount > 0 ? 1 : 0;
        }
    }
}
