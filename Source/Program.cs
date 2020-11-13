using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Mail;
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
        private static Timer AmAliveTimer = null;
        private static int ConnFailCount = 0;
        private static int PingDelay = 0;

        private static string
            GmailAcctAddress,
            GmailAcctPassword,
            PhoneNumber,
            PhoneCarrier;
        private static bool SendAmAliveSMS = false;

        private static readonly Dictionary<string, string> Carriers = new Dictionary<string, string>()
        {
            { "AT&T", "[PHONE_NUMBER]@txt.att.net" },
            { "Boost Mobile", "[PHONE_NUMBER]@sms.myboostmobile.com" },
            { "Cricket Wireless", "[PHONE_NUMBER]@mms.cricketwireless.net" },
            { "Google Project Fi", "[PHONE_NUMBER]@msg.fi.google.com" },
            { "Republic Wireless", "[PHONE_NUMBER]@text.republicwireless.com" },
            { "Sprint", "[PHONE_NUMBER]@messaging.sprintpcs.com" },
            { "Straight Talk", "[PHONE_NUMBER]@vtext.com" },
            { "T-Mobile", "[PHONE_NUMBER]@tmomail.net" },
            { "Ting", "[PHONE_NUMBER]@message.ting.com" },
            { "Tracfone", "[PHONE_NUMBER]@mmst5.tracfone.com" },
            { "U.S. Cellular", "[PHONE_NUMBER]@email.uscc.net" },
            { "Verizon", "[PHONE_NUMBER]@vtext.com" },
            { "Virgin Mobile", "[PHONE_NUMBER]@vmobl.com" }
        };

        //External DLLS
        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImport("user32.dll", EntryPoint = "FindWindow", SetLastError = true)]
        static extern IntPtr FindWindowByCaption(IntPtr zeroOnly, string lpWindowName);

        public static void Main(string[] args)
        {
            Console.Title = "NewEgg Stock Monitor";

            SetSMTPPreferences();

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
                    StopTimers("\nApplication exceeded memory consumption limit of 15Mb. Exiting application.\n", ConsoleColor.Red);
                }
            }
        }

        private static void SetSMTPPreferences()
        {
            Console.Write("Do you want to be texted if the item becomes available? (Y/N) - ");
            if (Console.ReadKey().Key != ConsoleKey.Y)
            {
                return;
            }

            //PHONE CARRIER
            WriteDefaultHeader();
            Console.Write("\nSelect your phone carrier from the list below by entering the number that corresponds to your carrier\n\n");
            Console.WriteLine(" 0 - None of the below");
            List<string> carriers = Carriers.Select(kvp => kvp.Key).ToList();
            foreach ((string carrier, int index) in carriers.Select((v, i) => (v, i + 1)))
            {
                Console.WriteLine($"{(index.ToString().Length == 1 ? $" {index}" : $"{index}")} - {carrier}");
            }
            Console.Write("\nCarrier's Number: ");
            PhoneCarrier = Console.ReadLine();
            if (PhoneCarrier == "0")
            {
                Console.Clear();
                CConsole.ColorWriteLine(ConsoleColor.Red, "Sorry I got no free way to get a text out to you via your carrier.\n");
                CConsole.ColorWriteLine(ConsoleColor.Yellow, "Ain't nobody got time to be spendin no money on Twilio up in here\n");
                return;
            }
            PhoneCarrier = Carriers[carriers[int.Parse(PhoneCarrier) - 1]]; //Ngl, it amuses me how convoluted this line is so I'm leaving it as is

            //COURTESY TEXT
            WriteDefaultHeader();
            Console.WriteLine("\nDo you want me to send you a text at the top of every hour to let you know I'm still running? Y/N\n");
            SendAmAliveSMS = Console.ReadKey().Key == ConsoleKey.Y;

            //EMAIL ADDR
            WriteDefaultHeader();
            Console.Write("\nEnter your gmail account email address: ");
            GmailAcctAddress = Console.ReadLine();

            //EMAIL PW
            WriteDefaultHeader();
            CConsole.ColorWriteLine(ConsoleColor.Yellow, "This password is only retained in memory while this application is running.\n");
            CConsole.ColorWriteLine(ConsoleColor.Red, "This password is NOT retained in any permanent way in any place.\nWhen the app is closed it is lost forever or until entered here again\n");
            CConsole.ColorWriteLine(ConsoleColor.Yellow, "Think of it like this, you can't send an email until you sign in to your acct");
            Console.Write("\nEnter your gmail account password: ");
            GmailAcctPassword = Console.ReadLine();

            //PHONE NUMBER
            WriteDefaultHeader();
            CConsole.ColorWriteLine(ConsoleColor.Yellow, "Do not apply any symbols/spacing/formatting etc\ni.e. if your number is (123) 456-7890 enter 1234567890");
            Console.Write("\nEnter your phone number: ");
            PhoneNumber = Console.ReadLine();
            PhoneCarrier = PhoneCarrier.Replace("[PHONE_NUMBER]", PhoneNumber);

            Console.Clear();

            static void WriteDefaultHeader()
            {
                Console.Clear();
                Console.WriteLine("I'm ngl, I'm not gonna put any validation in here.");
                Console.WriteLine("So if you go and put some silly-ass answers down here you're only hurtin' yourself - Chris (The Dev)\n");
            }
        }

        private static int GetDelay()
        {
            int pingDelay;

            Console.Write("How many seconds should I wait between each request made to NewEgg to check their stock? : ");
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
            if (SendAmAliveSMS)
            {
                DateTime nextWholeHour = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, DateTime.Now.Hour + 1, 0, 0, 0);
                TimeSpan hourDiff = nextWholeHour.TimeOfDay - DateTime.Now.TimeOfDay;
                AmAliveTimer = new Timer(state => SendText("NewEgg Stock Monitor Status", "I'm still running! Haven't found anything tho... ):").GetAwaiter().GetResult(), null, hourDiff, TimeSpan.FromHours(1));
            }

            Console.ReadKey();

            StopTimers();

            Console.ReadKey();
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
                CConsole.ColorWriteLine(ConsoleColor.Red, $"[{DateTime.Now:hh:mm:ss tt}] - Error connecting to NewEgg.");
                if (++ConnFailCount == 10)
                    StopTimers("\nUnable to connect to NewEgg for past 10 requests\n", ConsoleColor.Red);
                else
                    memUsage.Report(GC.GetTotalMemory(true));
                return;
            }
            ConnFailCount = 0;

            Regex inStockPattern = new Regex(@"add to cart", RegexOptions.IgnoreCase);
            if (inStockPattern.IsMatch(httpContent))
            {
                CConsole.ColorWriteLine(ConsoleColor.Green, $"[{DateTime.Now:hh:mm:ss tt}] - Item in stock!");
                if (!string.IsNullOrWhiteSpace(PhoneCarrier))
                {
                    SendText("NewEgg Stock Monitor Alert!", "Item in stock! Gogogogogogo!").GetAwaiter().GetResult();
                }
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

        private static async Task SendText(string subject, string message)
        {
            using MailMessage mail = new MailMessage
            {
                From = new MailAddress(GmailAcctAddress),
                Subject = subject,
                Body = message,
                IsBodyHtml = false
            };
            mail.To.Add(PhoneCarrier);
            using SmtpClient smtp = new SmtpClient("smtp.gmail.com", 587)
            {
                Credentials = new NetworkCredential(GmailAcctAddress, GmailAcctPassword),
                EnableSsl = true
            };
            await smtp.SendMailAsync(mail);
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

        private static void StopTimers(string additionalInfo = null, ConsoleColor? addtlInfoColor = null)
        {
            try
            {
                Timer.Change(Timeout.Infinite, Timeout.Infinite);
                Timer.Dispose();

                AmAliveTimer.Change(Timeout.Infinite, Timeout.Infinite);
                AmAliveTimer.Dispose();

                if (!string.IsNullOrWhiteSpace(additionalInfo))
                    if (addtlInfoColor != null)
                        CConsole.ColorWriteLine(addtlInfoColor.Value, additionalInfo);
                    else
                        Console.ResetColor();

                Console.WriteLine("\nPolling has been stopped.\n\nPress any key to close this window...");
                Console.ReadKey();
            }
            catch
            {
                //Already disposed. Error out silently and continue to exit main thread.
            }
        }
    }

    internal static class CConsole //ColorConsole
    {
        public static void ColorWriteLine(ConsoleColor color, string message)
        {
            Console.ForegroundColor = color;
            Console.WriteLine(message);
            Console.ResetColor();
        }
    }
}
