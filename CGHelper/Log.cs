using System;

namespace CommonLibrary


{
    public class Log
    {
        public static void WriteLine(string tag, string log)
        {
            if ("Battle".Equals(tag))
            {
                return;
            }

            WriteLine(log);
        }

        public static void WriteLine(string log)
        {
            Console.WriteLine(DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss.ffff ") + log);
        }
    }
}
