using System;
using System.Linq;
using System.Web;
using System.Web.SessionState;
using StackExchange.Redis;

namespace Glass.Redis.SessionProvider
{
    public class ExpiredSessionProvider
    {
        private readonly string _applicationName;
        private readonly int _lockTimeoutSeconds;
        private readonly int _sessionLength;
        public IDatabase Database { get; set; }

        public const  int RetryTime = 5;
        private string _expiredIndex = "EXPIRY_INDEX";

        public ExpiredSessionProvider(
            IDatabase database, 
            string applicationName = "",
            //data locks should be short, we assume that 10 seconds should be sufficient for all processing
            int lockTimeoutSeconds = 10)
        {
            _applicationName = applicationName;
            _lockTimeoutSeconds = lockTimeoutSeconds;
            Database = database;
        }

        public void Updated(string id)
        {
            Updated(DateTime.Now, id);
        }
        public void Updated(DateTime date, string id)
        {
            double ticks = date.ToFileTimeUtc();
            Database.SortedSetAdd(_expiredIndex, id, ticks);
        }

        public SessionStateStoreData Expired(DateTime to,out string id)
        {
            //Using FileTimeUtc to avoid any date generation issues
            double fromTicks = DateTime.Now.AddHours(-1).ToFileTimeUtc();
            double toTicks = to.ToFileTimeUtc();

            var values = Database.SortedSetRangeByScore(_expiredIndex, fromTicks, toTicks);
            SessionStateStoreData data = null;
            id = null;

            if (values.Any())
            {
                id = values.FirstOrDefault();

                //push into the future so we can retry encase of failure.
                if (!string.IsNullOrEmpty(id))
                {
                    Updated(DateTime.Now.AddMinutes(RetryTime), id);
                    bool found = GetItem(id, out data);

                    Log.Debug("Get expired session " + id);

                    //if we aquire the lock but don't have any data then we should remove the key.
                    if (found == true && data == null)
                    {
                        Log.Debug("Removing session null data " + id);
                        Remove(id);
                    }
                    else if (found == true && data != null)
                    {
                        Log.Debug("Found data for" + id);
                    }
                    else
                    {
                        Log.Debug("Data not found" + id);
                    }
                }
            }

            return data;
        }

        public bool GetItem(string id, out SessionStateStoreData data)
        {
            //this is not exclusive
            var keys = new KeyGenerator(id, "");
            string expectedLock = Guid.NewGuid().ToString();

            RedisKey[] keyArgs = new RedisKey[] { keys.LockKey, keys.DataKey };
            RedisValue[] valueArgs = new RedisValue[] { expectedLock, _lockTimeoutSeconds };


            RedisResult[] result = (RedisResult[]) Database.ScriptEvaluate(WriteLockAndGetDataScript, keyArgs, valueArgs) ;

            string lockValue = (string) result[0];
          
            bool isLocked = (bool)result[2];

            data = null;

            //islocked means locked to another thread
            if (!isLocked && lockValue == expectedLock)
            {
                RedisResult[] items = null;

                if (result[1] != null)
                {
                   items = (RedisResult[])result[1];
                }
                if (items != null && items.Length > 0)
                {
                    var collection = SessionStateParser.GetSessionDataStatic(items);
                    data = new SessionStateStoreData(collection, new HttpStaticObjectsCollection(), _sessionLength);
                }

                return true;
            }
            return false;
        }


        static readonly string WriteLockAndGetDataScript = (@" 
                local retArray = {} 
                local lockValue = ARGV[1] 
                local locked = redis.call('SETNX',KEYS[1],ARGV[1])        
                local IsLocked = true
                
                if locked == 0 then
                    lockValue = redis.call('GET',KEYS[1])
                else
                    redis.call('EXPIRE',KEYS[1],ARGV[2])
                    IsLocked = false
                end
                
                retArray[1] = lockValue
                if lockValue == ARGV[1] then retArray[2] = redis.call('HGETALL',KEYS[2]) else retArray[2] = '' end              

                retArray[3] = IsLocked
                return retArray
                ");



        public void Remove(string id)
        {
            Log.Debug("Removing session " + id);
            Database.SortedSetRemove(_expiredIndex, id);
        }
    }

    
}
