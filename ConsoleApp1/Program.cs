using System;
using CAOS;
using System.Diagnostics;

namespace ConsoleApp1
{
    class Demo
    {
        static void Main(string[] args)
        {
            CaosInjector.

            CaosResult result = null;
            try
            {
                result = (new CaosInjector("Docking Station"))
                    .ExecuteCaosGetResult("outs \"hi\"");
            }catch (NoGameCaosException e)
            {
                Console.WriteLine(e.Message);
            }
            
            if (result.Succeded)
            {
                Console.WriteLine(result.Content);
            }
            else
            {
                Debug.Assert(result.ResultCode != 0);
                Console.WriteLine($"Error Code: {result.ResultCode}");
            }

            //try return bool strategy
            if()


            Console.ReadKey();
        }
    }
}
