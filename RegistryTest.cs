using System;
using System.Threading;
using Microsoft.Win32;

namespace B2SRegistryTest
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("B2S Registry Test Utility");
            Console.WriteLine("=========================");
            Console.WriteLine("This will write test data to HKCU\\Software\\B2S");
            Console.WriteLine("Make sure B2SBackglassServerEXE.exe is running!");
            Console.WriteLine();
            Console.WriteLine("Commands:");
            Console.WriteLine("  1 = Test lamp changes");
            Console.WriteLine("  2 = Blink test");
            Console.WriteLine("  q = Quit");
            Console.WriteLine();

            while (true)
            {
                Console.Write("> ");
                var cmd = Console.ReadLine();
                
                if (cmd == "q") break;
                
                try
                {
                    using (var key = Registry.CurrentUser.CreateSubKey("Software\\B2S"))
                    {
                        if (cmd == "1")
                        {
                            Console.WriteLine("Turning lamps 0-9 ON...");
                            var lamps = new string('0', 401).ToCharArray();
                            for (int i = 0; i < 10; i++)
                                lamps[i] = '1';
                            key.SetValue("B2SLamps", new string(lamps));
                            Console.WriteLine("Done! Check the backglass.");
                        }
                        else if (cmd == "2")
                        {
                            Console.WriteLine("Blinking for 5 seconds...");
                            var lamps = new string('0', 401).ToCharArray();
                            for (int i = 0; i < 10; i++)
                            {
                                for (int j = 0; j < 20; j++)
                                    lamps[j] = (i % 2 == 0) ? '1' : '0';
                                key.SetValue("B2SLamps", new string(lamps));
                                Thread.Sleep(500);
                            }
                            Console.WriteLine("Done!");
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"ERROR: {ex.Message}");
                }
            }
        }
    }
}
