using System;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace NewEggStockMonitor
{
    public class Program
    {
        private const int MegabyteCap = 15000000; //arbitrary

        private static Timer Timer = null;
        private static int ConnFailCount = 0;
        private static int PingDelay = 0;

        //External DLLS
        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImport("user32.dll", EntryPoint = "FindWindow", SetLastError = true)]
        static extern IntPtr FindWindowByCaption(IntPtr zeroOnly, string lpWindowName);

        public static void Main(string[] args)
        {
            Console.Title = "NewEgg Stock Monitor";

            PingDelay = GetDelay();

            RunService(MemUsageUpdate);

            static void MemUsageUpdate(long usageQty)
            {
                if (usageQty > MegabyteCap)
                {
                    //Empty console window text to see if it will bring memory usage back below arbitrary 15Mb cap.
                    Console.Clear();
                    if (GC.GetTotalMemory(true) < MegabyteCap)
                    {
                        Console.WriteLine("Press any key to stop polling.\n");
                        return;
                    }
                    Timer.Change(Timeout.Infinite, Timeout.Infinite);
                    Timer.Dispose();
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("\nApplication exceeded memory consumption limit of 15Mb. Exiting application.\n");
                    Console.ResetColor();
                    Console.WriteLine("Polling has been stopped.\n\nPress any key to close this window...");
                }
            }
        }

        private static int GetDelay()
        {
            int pingDelay;
            Console.Write("How many seconds should I wait between requests? : ");
            if (!int.TryParse(Console.ReadLine(), out pingDelay))
            {
                pingDelay = TryAgain(true);
            }
            else if (pingDelay < 1)
            {
                pingDelay = TryAgain(false);
            }

            Console.Clear();
            Console.WriteLine($"Alright I'll check every {pingDelay} {(pingDelay == 1 ? "second" : "seconds")}\n");

            return pingDelay;

            static int TryAgain(bool invalidChar)
            {
                Console.Clear();
                string msg = invalidChar ? "Let's try that again..." : "Request must be at least 1 second apart";
                Console.WriteLine($"{msg}\n");
                return GetDelay();
            }
        }

        private static void RunService(Action<long> MemUsageUpdate)
        {
            Progress<long> memUsage = new Progress<long>(MemUsageUpdate);

            DateTime nextWholeMin = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, DateTime.Now.Hour, DateTime.Now.Minute + 1, 0, 0);
            TimeSpan timeDiff = nextWholeMin.TimeOfDay - DateTime.Now.TimeOfDay;
            Console.WriteLine($"Polling will begin in {timeDiff.TotalSeconds:N3} seconds");
            Thread.Sleep(timeDiff);

            Console.Clear();
            Console.WriteLine("Press any key to stop polling.\n");

            Timer = new Timer(state => CheckStock(memUsage), null, TimeSpan.Zero, TimeSpan.FromSeconds(PingDelay));

            Console.ReadKey();

            try
            {
                Timer.Change(Timeout.Infinite, Timeout.Infinite);
                Timer.Dispose();

                Console.WriteLine("\nPolling has been stopped.\n\nPress any key to close this window...");
                Console.ReadKey();
            }
            catch
            {
                //Already disposed. Error out silently and continue to exit main thread.
            }
        }

        private static void CheckStock(IProgress<long> memUsage)
        {
            string httpContent;
            try
            {
                httpContent = QueryNewEgg().GetAwaiter().GetResult();
            }
            catch
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"[{DateTime.Now:hh:mm:ss tt}] - Error connecting to NewEgg.");
                Console.ResetColor();
                if (++ConnFailCount == 10)
                {
                    Timer.Change(Timeout.Infinite, Timeout.Infinite);
                    Timer.Dispose();
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("\nUnable to connect to NewEgg for past 10 requests\n");
                    Console.ResetColor();
                    Console.WriteLine("Polling has been stopped.\n\nPress any key to close this window...");
                }
                else
                {
                    memUsage.Report(GC.GetTotalMemory(true));
                }
                return;
            }
            ConnFailCount = 0;

            Regex inStockPattern = new Regex(@"add to cart", RegexOptions.IgnoreCase);
            if (inStockPattern.IsMatch(httpContent))
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine($"[{DateTime.Now:hh:mm:ss tt}] - Item in stock!");
                Console.ResetColor();
                BringToFront();
            }
            else
            {
                Console.WriteLine($"[{DateTime.Now:hh:mm:ss tt}] - Waiting for restock");
            }

            memUsage.Report(GC.GetTotalMemory(true));
        }

        private static async Task<string> QueryNewEgg()
        {
            Console.WriteLine($"[{DateTime.Now:hh:mm:ss tt}] - Loading page");
            string result = null;
            using HttpClient client = new HttpClient
            {
                BaseAddress = new Uri(@"https://newegg.com")
            };
            string endpoint = @"amd-ryzen-9-5900x/p/N82E16819113664";
            using HttpResponseMessage response = await client.GetAsync(endpoint);
            if (response.IsSuccessStatusCode)
            {
                result = await response.Content?.ReadAsStringAsync();
            }
            return result;
        }

        private static void BringToFront()
        {
            string originalTitle = Console.Title;
            string uniqueTitle = Guid.NewGuid().ToString();
            Console.Title = uniqueTitle;
            Thread.Sleep(50);
            IntPtr handle = FindWindowByCaption(IntPtr.Zero, uniqueTitle);

            if (handle == IntPtr.Zero)
            {
                Console.WriteLine($"[{DateTime.Now:hh:mm:ss tt}] - Tried to notify you of stock, but couldn't bring this window to the front.");
                return;
            }

            Console.Title = originalTitle;

            SetForegroundWindow(handle);
        }
    }
}
