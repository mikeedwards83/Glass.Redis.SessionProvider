using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Glass.Redis.SessionProvider
{
    public class Log
    {
        private  static Log Instance { get; set; }
        public const string Prefix = "Glass Redis Session: ";


        static Log()
        {
            Instance = new Log();
        }

        private static string Format(string message)
        {
            return Prefix + message;
        }

        
        public static void Debug(string message)
        {
            Sitecore.Diagnostics.Log.Debug(Format(message), Instance);
        }
        public static void Error(string message)
        {
            Sitecore.Diagnostics.Log.Error(Format(message), Instance);
        }
        public static void Info(string message)
        {
            Sitecore.Diagnostics.Log.Info(Format(message), Instance);
        }
    }
}
