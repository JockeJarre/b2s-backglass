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
            Console.WriteLine("Press any key to start...");
            Console.ReadKey();

            try
            {
                // Create registry key if it doesn't exist
                using (var key = Registry.CurrentUser.CreateSubKey("Software\\B2S"))
                {
                    if (key == null)
                    {
                        Console.WriteLine("ERROR: Could not create registry key!");
                        return;
                    }

                    Console.WriteLine();
                    Console.WriteLine("Starting test sequence...");
                    Console.WriteLine();

                    // Test 1: All lamps off
                    Console.WriteLine("Test 1: All lamps OFF");
                    key.SetValue("B2SLamps", new string('0', 401));
                    Thread.Sleep(1000);

                    // Test 2: Turn on lamp 0
                    Console.WriteLine("Test 2: Lamp 0 ON");
                    var lamps = new string('0', 401).ToCharArray();
                    lamps[0] = '1';
                    key.SetValue("B2SLamps", new string(lamps));
                    Thread.Sleep(1000);

                    // Test 3: Turn on lamps 0-9
                    Console.WriteLine("Test 3: Lamps 0-9 ON");
                    for (int i = 0; i < 10; i++)
                        lamps[i] = '1';
                    key.SetValue("B2SLamps", new string(lamps));
                    Thread.Sleep(1000);

                    // Test 4: Blink pattern
                    Console.WriteLine("Test 4: Blinking pattern (5 seconds)...");
                    for (int cycle = 0; cycle < 10; cycle++)
                    {
                        for (int i = 0; i < 50; i++)
                            lamps[i] = (cycle % 2 == 0) ? '1' : '0';
                        
                        key.SetValue("B2SLamps", new string(lamps));
                        Thread.Sleep(500);
                    }

                    // Test 5: Wave pattern
                    Console.WriteLine("Test 5: Wave pattern (5 seconds)...");
                    for (int wave = 0; wave < 20; wave++)
                    {
                        for (int i = 0; i < 401; i++)
                            lamps[i] = '0';
                        
                        for (int i = wave * 20; i < Math.Min((wave + 1) * 20, 401); i++)
                            lamps[i] = '1';
                        
                        key.SetValue("B2SLamps", new string(lamps));
                        Thread.Sleep(250);
                    }

                    // Test 6: All on
                    Console.WriteLine("Test 6: All lamps ON");
                    key.SetValue("B2SLamps", new string('1', 401));
                    Thread.Sleep(2000);

                    // Test 7: All off
                    Console.WriteLine("Test 7: All lamps OFF");
                    key.SetValue("B2SLamps", new string('0', 401));
                    Thread.Sleep(1000);

                    Console.WriteLine();
                    Console.WriteLine("Test complete!");
                    Console.WriteLine();
                    Console.WriteLine("Did you see the lamps change in B2SBackglassServerEXE?");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ERROR: {ex.Message}");
            }

            Console.WriteLine();
            Console.WriteLine("Press any key to exit...");
            Console.ReadKey();
        }
    }
}
