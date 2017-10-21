using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CAOS;
using System.Diagnostics;

namespace ConsoleApp1
{
    class Demo
    {
        static void Main(string[] args)
        {
            CaosInjector injector = new CaosInjector("Docking Station");
            CaosResult result = injector.ExecuteCaosGetResult("outs \"hi\"");
            if (result.Succeded)
            {
                Console.WriteLine(result.Content);
            }
            else
            {
                Debug.Assert(result.ResultCode != 0);
                Console.WriteLine($"Error Code: {result.ResultCode}");
            }
            Console.ReadKey();
        }
    }
}
