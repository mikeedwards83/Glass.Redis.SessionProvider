
using System;
using System.Web.SessionState;
using Microsoft.Web.Redis;
using StackExchange.Redis;

namespace Glass.Redis.SessionProvider
{
    public class SessionStateParser
    {
        static BinarySerializer _serializer = new BinarySerializer();

        public static ISessionStateItemCollection GetSessionDataStatic(RedisResult[] rowDataFromRedis)
        {
            ISessionStateItemCollection sessionData = null;
            sessionData = new SessionStateItemCollection();
            for (int i = 0; i < rowDataFromRedis.Length; i+=2)
            {
                string key = (string) rowDataFromRedis[i];
                Console.WriteLine(key);
                object val = Deserialize((byte[])rowDataFromRedis[i+1]);

                if (key != null)
                {
                    sessionData[key] = val;
                }
            }

            return sessionData;
        }

        public static object Deserialize(byte[] dataAsBytes)
        {

            return _serializer.Deserialize(dataAsBytes);
        }


        public static byte[] Serialize(object obj)
        {
            return _serializer.Serialize(obj);
        }
    }
}
