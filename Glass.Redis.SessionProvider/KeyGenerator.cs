using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Glass.Redis.SessionProvider
{
    public  class KeyGenerator
    {
        private readonly string _id;
        private readonly string _application;

        public KeyGenerator(string id, string application)
        {
            _id = id;
            _application = application;
        }

        public string DataKey
        {
            get { return "{/" + _application + "_" + _id + "}_Data"; }
        }
        public string LockKey
        {
            get { return "{/" + _application + "_" + _id + "}_GlassLock"; }
        }
    }
}
