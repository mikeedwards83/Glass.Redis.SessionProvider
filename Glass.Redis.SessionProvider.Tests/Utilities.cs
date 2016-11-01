using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using StackExchange.Redis;

namespace Glass.Redis.SessionProvider.Tests
{
    public  class Utilities
    {

        public static void ClearDatabases(string hostname)
        {
            ConnectionMultiplexer connectionMultiplexer = ConnectionMultiplexer.Connect(hostname + ", allowAdmin=true");
            connectionMultiplexer.GetServer(hostname, 6379).FlushAllDatabases();
        }


        public static void SetKeys(ExpiredSessionProvider provider, KeyGenerator keysGenerator, Dictionary<string, object> dictionary)
        {
            var script = "redis.call('HMSET', KEYS[1], unpack(ARGV, 2, ARGV[1]))";
            var values = CreateList(dictionary);
            var keys = new RedisKey[] { keysGenerator.DataKey };

            provider.Database.ScriptEvaluate(script, keys, values);
        }


        public static RedisValue[] CreateList(Dictionary<string, object> dictionary)
        {

            List<RedisValue> list = new List<RedisValue>();
            list.Add((dictionary.Keys.Count * 2) + 1);

            foreach (var key in dictionary.Keys)
            {
                list.Add(key);
                list.Add(SessionStateParser.Serialize(dictionary[key]));
            }
            return list.ToArray();
        }
    }
}
