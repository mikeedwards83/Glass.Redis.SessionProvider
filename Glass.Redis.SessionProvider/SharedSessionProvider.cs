using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using System.Web.SessionState;
using Microsoft.Web.Redis;
using Sitecore.SessionProvider;
using Sitecore.SessionProvider.Helpers;
using StackExchange.Redis;

namespace Glass.Redis.SessionProvider
{
    public class SharedSessionProvider : SitecoreSessionStateStoreProvider
    {
        private readonly Microsoft.Web.Redis.RedisSessionStateProvider _redisProvider;

        private ExpiredSessionProvider _expiredSessionProvider;

        public int Timeout { get; set; }
        public string SessionType { get; set; }
        public string Host { get; set; }
        private int _skewTime = 2;

        public SharedSessionProvider()
        {
            _redisProvider = new RedisSessionStateProvider();
        }


        public override void Initialize(string name, NameValueCollection config)
        {

            ConfigReader configReader = new ConfigReader(config, name);
            Timeout = configReader.GetInt32("timeout", 20);
            SessionType = configReader.GetString("sessionType", true);
            SessionType = SessionType ?? string.Empty;

            Host = configReader.GetString("host", false);

            ConnectionMultiplexer connectionMultiplexer = ConnectionMultiplexer.Connect(Host);
            var database  = connectionMultiplexer.GetDatabase();
            _expiredSessionProvider = new ExpiredSessionProvider(database, SessionType);

            _redisProvider.Initialize(name, config);
            base.Initialize(name, config);
        }

        private string GetId(string id)
        {
            return SessionType + id;
        }

        private string CleanId(string id)
        {
            if (id == null)
                return id;

            return id.Replace(SessionType, "");
        }

        public override void InitializeRequest(HttpContext context)
        {
            _redisProvider.InitializeRequest(context);

            base.InitializeRequest(context);
        }
        public override SessionStateStoreData CreateNewStoreData(HttpContext context, int timeout)
        {
            //stay in session 2 minute longer than expected so that we have a chance to manually remove it.

            return _redisProvider.CreateNewStoreData(context, timeout+ _skewTime);
        }


        public override SessionStateStoreData GetItem(HttpContext context, string id, out bool locked, out TimeSpan lockAge, out object lockId,
            out SessionStateActions actions)
        {
            id = GetId(id);

           var result = _redisProvider.GetItem(context, id, out locked, out lockAge, out lockId, out actions);
            return result;
        }

        public override SessionStateStoreData GetItemExclusive(HttpContext context, string id, out bool locked, out TimeSpan lockAge,
            out object lockId, out SessionStateActions actions)
        {
            id = GetId(id);
            var result = _redisProvider.GetItemExclusive(context, id, out locked, out lockAge, out lockId, out actions);
            return result;
        }

        public override void ReleaseItemExclusive(HttpContext context, string id, object lockId)
        {
            id = GetId(id);
            _redisProvider.ReleaseItemExclusive(context, id, lockId);
        }

        public override void SetAndReleaseItemExclusive(HttpContext context, string id, SessionStateStoreData item, object lockId, bool newItem)
        {
            id = GetId(id);

            //We have to force in a fake item otherwise the server goes into a loop
            item.Items["6247030A-D2EF-46C6-A62C-AD315F9D0E06"] = "";

            //stay in session 2 minute longer than expected so that we have a chance to manually remove it.
            item.Timeout  =  item.Timeout+ _skewTime;
            _expiredSessionProvider.Updated(id);
            _redisProvider.SetAndReleaseItemExclusive(context, id,item, lockId, newItem);
            Log.Debug("Saved id " + id);

        }

        public override void RemoveItem(HttpContext context, string id, object lockId, SessionStateStoreData item)
        {
            id = GetId(id);
            _redisProvider.RemoveItem(context, id, lockId, item);
        }

        public override void ResetItemTimeout(HttpContext context, string id)
        {
            id = GetId(id);
            _redisProvider.ResetItemTimeout(context, id);
        }

        public override void CreateUninitializedItem(HttpContext context, string id, int timeout)
        {
            id = GetId(id);
          _redisProvider.CreateUninitializedItem(context, id, timeout);
        }

        protected override SessionStateStoreData GetExpiredItemExclusive(DateTime signalTime, SessionStateLockCookie lockCookie, out string id)
        {

            var result =  _expiredSessionProvider.Expired(signalTime.AddMinutes(-Timeout), out id);

            if (id != null)
            {
                id = CleanId(id);

                Log.Debug(string.Format("Item Expired {0} with data {1}", id, result != null));
            }
            return result;
        }

        protected override void RemoveItem(string id, string lockCookie)
        {
            id = GetId(id);
            //we should need to expire the standard redis provider, this should expire on its own in the future.
            Log.Debug("Item Removed " + id);

            _expiredSessionProvider.Remove(id);

        }

        public override void Dispose()
        {
            _redisProvider.Dispose();

            base.Dispose();
        }


    }
}
