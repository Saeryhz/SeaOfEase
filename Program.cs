using SeaOfEase.SeaOfThieves.Util;
using System;

namespace SeaOfEase
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.Title = "Sea of Ease";

            Loader loader = new Loader();
            loader.DefineSOT();
            loader.GetEthernetDevice();

            while (true)
            {
                loader.NetstatListener();
                loader.InterceptListener();
                Console.ReadLine();
            }
        }
    }
}
