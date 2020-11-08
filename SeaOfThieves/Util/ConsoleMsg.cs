using System;
using System.Drawing;
using Console = Colorful.Console;

namespace SeaOfEase.SeaOfThieves.Util
{
    public class ConsoleMsg
    {
        public static void Write(string message, Type messageType, bool refresh = false)
        {
            Console.Write($"[{DateTime.Now.ToString("H:mm:ss")}] ", Color.LightYellow);
            if (messageType == Type.Error)
                Console.WriteLine(message, Color.PaleVioletRed);
            else if (messageType == Type.Info)
                Console.WriteLine(message, Color.LightCyan);


            if (refresh)
            {
                Console.Write($"[{DateTime.Now.ToString("H:mm:ss")}] ", Color.LightYellow);
                Console.WriteLine("Press enter to refresh...", Color.GreenYellow);
            }
        }

        public enum Type
        {
            Info,
            Error
        }
    }
}
