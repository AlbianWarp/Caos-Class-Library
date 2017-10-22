using System;
using CAOS;
using System.Diagnostics;

namespace ConsoleApp1
{
    class Demo
    {
        static void Main(string[] args)
        {
            CaosInjector injector = new CaosInjector("Docking Station");

            if (injector.CanConnectToGame())
            {
                TryCatchStrategy(injector);
                TryReturnBoolStrategy(injector);
            }
            else
            {
                Console.WriteLine("Couldn't connect to game.");
            }
            Console.ReadKey();
        }

        private static void TryCatchStrategy(CaosInjector injector)
        {
            try
            {
                CaosResult result = injector.ExecuteCaos("outs \"hi\"");
                if (result.Succeded)
                {
                    Console.WriteLine(result.Content);
                }
                else
                {
                    Console.WriteLine($"Error Code: {result.ResultCode}");
                }
            }
            catch (NoGameCaosException e)
            {
                Console.WriteLine($"Game exited unexpectedly. Error message: {e.Message}");
            }
        }

        private static void TryReturnBoolStrategy(CaosInjector injector)
        {
            CaosResult result;
            if (injector.TryExecuteCaos("outs \"hi\"", out result))
            {
                if (result.Succeded)
                {
                    Console.WriteLine(result.Content);
                    //Just try to do it, we don't care about the results
                    injector.TryExecuteCaos("targ norn doif targ <> null sezz \"Yo yo! What up?\" endi");
                }
                else
                {
                    Debug.Assert(result.ResultCode != 0);
                    Console.WriteLine($"Error Code: {result.ResultCode}");
                }
            }
            else
            {
                Console.WriteLine("Execution failed. Game may have exited.");
            }
        }
    }
}
