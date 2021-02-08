using System;
using System.Linq;
using CmdCore.Splice;

namespace CmdCore
{
    class Program
    {
        static void Main(string[] args)
        {
            try
            {
                bool okArgs = false;
                //okArgs |= (new CyclizeAssemblyFlags()).ParseArgs(args);
                //okArgs |= (new AsymmetricFusionFlags()).ParseArgs(args);
                okArgs |= (new CxrcxFusionFlags()).ParseArgs(args);


                if (!okArgs)
                    throw new ArgumentException();
            }
            catch
            {
                Console.WriteLine();
                Console.WriteLine("Options:");
                //Console.WriteLine(AsymmetricFusionFlags.BaseFlag + AsymmetricFusionFlags.GetUsageOptions().Aggregate("\t", (a, b) => a + "\n\t" + b));
                //Console.WriteLine();
                Console.WriteLine(CxrcxFusionFlags.BaseFlag + CxrcxFusionFlags.GetUsageOptions().Aggregate("\t", (a, b) => a + "\n\t" + b));
                //Console.WriteLine();
                //Console.WriteLine("-splice_cyclize" + CyclizeAssemblyFlags.GetUsageOptions().Aggregate("\t", (a, b) => a + "\n\t" + b));
                return;
            }

            try
            {
                FusionJobRunner.Run(args);
            }
            catch(Exception e)
            {
                Console.Write(e.StackTrace);
            }
        }
    }
}
