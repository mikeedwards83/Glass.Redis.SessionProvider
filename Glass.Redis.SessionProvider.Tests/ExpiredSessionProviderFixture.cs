using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using StackExchange.Redis;

namespace Glass.Redis.SessionProvider.Tests
{
    [TestFixture]
    public class ExpiredSessionProviderFixture
    {
        [Test]
        public void Updated_AddsNewKeyWithoutExcpetion()
        {
            //Arrange
            string hostname = "localhost";
            Utilities.ClearDatabases(hostname);
            ConnectionMultiplexer connectionMultiplexer = ConnectionMultiplexer.Connect(hostname);

            var database = connectionMultiplexer.GetDatabase();

            ExpiredSessionProvider provider = new ExpiredSessionProvider(database);
            DateTime date = DateTime.Now;
            var id = Guid.NewGuid().ToString();

            //Act
            provider.Updated(date, id);
        }

        [Test]
        public void Expired_GetsOldKey()
        {
            //Arrange
            string hostname = "localhost";
            Utilities.ClearDatabases(hostname);
            ConnectionMultiplexer connectionMultiplexer = ConnectionMultiplexer.Connect(hostname);

            var database = connectionMultiplexer.GetDatabase();
            var id = Guid.NewGuid().ToString();

            ExpiredSessionProvider provider = new ExpiredSessionProvider(database);
            DateTime date = DateTime.Now.AddMinutes(-30);
            provider.Updated(date, id);

            DateTime to = DateTime.Now;
            string returnedId; 

            //Act
            var expired = provider.Expired(to, out returnedId);

            //Assert
            Assert.AreEqual(returnedId, id);
        }
        [Test]
        public void Expired_IgnoresNewKey()
        {
            //Arrange

            string hostname = "localhost";
            Utilities.ClearDatabases(hostname);
            ConnectionMultiplexer connectionMultiplexer = ConnectionMultiplexer.Connect(hostname);


            var database = connectionMultiplexer.GetDatabase();
            var id = Guid.NewGuid().ToString();

            ExpiredSessionProvider provider = new ExpiredSessionProvider(database);
            DateTime date = DateTime.Now.AddMinutes(-10);
            provider.Updated(date, id);

            DateTime to = DateTime.Now.AddMinutes(-20);

            string returnedId;


            //Act
            var expired = provider.Expired( to, out returnedId);


            //Assert
            Assert.AreEqual(null, expired);
        }
        [Test]
        public void Expired_DoubleGetNotPossible()
        {
            //Arrange

            string hostname = "localhost";
            Utilities.ClearDatabases(hostname);
            ConnectionMultiplexer connectionMultiplexer = ConnectionMultiplexer.Connect(hostname);


            var database = connectionMultiplexer.GetDatabase();
            var id = Guid.NewGuid().ToString();

            ExpiredSessionProvider provider = new ExpiredSessionProvider(database);
            DateTime date = DateTime.Now.AddMinutes(-30);
            provider.Updated(date, id);

            DateTime to = DateTime.Now.AddMinutes(-20);
            string id1;
            string id2;

            //Act
            var expired1 =  provider.Expired( to, out id1);
            var expired2 = provider.Expired( to, out id2);
            
            //Assert
            Assert.AreEqual(id, id1);
            Assert.AreEqual(null, id2);
        }

        [Test]
        public void Expired_GetSessionData()
        {
            //Arrange

            string hostname = "localhost";
            Utilities.ClearDatabases(hostname);
            ConnectionMultiplexer connectionMultiplexer = ConnectionMultiplexer.Connect(hostname);



            var database = connectionMultiplexer.GetDatabase();
            var id = Guid.NewGuid().ToString();

            ExpiredSessionProvider provider = new ExpiredSessionProvider(database);
            DateTime date = DateTime.Now.AddMinutes(-30);
            provider.Updated(date, id);

            DateTime to = DateTime.Now.AddMinutes(-20);
            string id1;

            var keyGenerator = new KeyGenerator(id, "");

             var values = new Dictionary<string , object>();
            values.Add("key1","value1");
            Utilities.SetKeys(provider, keyGenerator, values);

              //Act
            var expired1 = provider.Expired(to, out id1);

            //Assert
            Assert.AreEqual(id, id1);
            Assert.IsNotNull(expired1);
            Assert.AreEqual("value1", expired1.Items["key1"]);
        }


        [Test]
        public void Expired_GetSessionDataUpdatedTimeoutLockTimeout()
        {
            //Arrange
            string hostname = "localhost";
            Utilities.ClearDatabases(hostname);
            ConnectionMultiplexer connectionMultiplexer = ConnectionMultiplexer.Connect(hostname);


            var database = connectionMultiplexer.GetDatabase();
            var id = Guid.NewGuid().ToString();

            int timeout = 3;
            ExpiredSessionProvider provider = new ExpiredSessionProvider(database, lockTimeoutSeconds:timeout);

            DateTime date = DateTime.Now.AddMinutes(-30);
            provider.Updated(date, id);

            DateTime to = DateTime.Now.AddMinutes(-20);
            string id1;
            string id2;
            string id3;

            var keyGenerator = new KeyGenerator(id, "");

            var values = new Dictionary<string, object>();
            values.Add("key1", "value1");
            Utilities.SetKeys(provider, keyGenerator, values);

            //Act
            var expired1 = provider.Expired(to, out id1);
            var expired2 = provider.Expired(to, out id2);

            to = DateTime.Now.AddMinutes(ExpiredSessionProvider.RetryTime + 1);

            Thread.Sleep((timeout+1)*1000);
            var expired3 = provider.Expired(to, out id3);

            //Assert
            Assert.AreEqual(id, id1);
            Assert.IsNotNull(expired1);
            Assert.AreEqual("value1", expired1.Items["key1"]);
            Assert.AreEqual(null, id2);
            Assert.IsNull(expired2);

            Assert.AreEqual(id, id3);
            Assert.IsNotNull(expired3);
            Assert.AreEqual("value1", expired1.Items["key1"]);
        }

       

        [Test]
        public void Remove_DeletesKey()
        {
            //Arrange

            ConnectionMultiplexer connectionMultiplexer = ConnectionMultiplexer.Connect("localhost");
            var database = connectionMultiplexer.GetDatabase();

            ExpiredSessionProvider provider = new ExpiredSessionProvider(database);
            DateTime date = DateTime.Now.AddMinutes(-30);
            provider.Updated(date, "test");

            DateTime to = DateTime.Now.AddMinutes(-20);
            string id;

            //Act
            provider.Remove("test");
            var expired1 = provider.Expired( to, out id);

            //Assert
            Assert.AreEqual(null, expired1);
        }
    }
}
