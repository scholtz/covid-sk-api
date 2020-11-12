using CovidMassTesting.Helpers;
using CovidMassTesting.Model;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace NUnitTestCovidApi
{
    public class Tests
    {
        private IConfiguration configuration;

        [SetUp]
        public void Setup()
        {
            configuration = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json")
                .Build();
        }

        private HttpResponseMessage AuthenticateUser(HttpClient client, string email, string password)
        {
            var request = Preauthenticate(client, email);
            var data = JsonConvert.DeserializeObject<AuthData>(request.Content.ReadAsStringAsync().Result);
            var cohash = data.CoHash;
            var rand = data.CoData;
            /// Authenticate
            var pass = password;
            for (int i = 0; i < 99; i++)
            {
                pass = Encoding.ASCII.GetBytes($"{pass}{cohash}").GetSHA256Hash();
            }
            pass = Encoding.ASCII.GetBytes($"{pass}{rand}").GetSHA256Hash();

            return Authenticate(client, email, pass);
        }
        private HttpResponseMessage Authenticate(HttpClient client, string email, string pass)
        {
            return client.PostAsync("User/Authenticate",
                    new System.Net.Http.FormUrlEncodedContent(new List<KeyValuePair<string, string>>() {
                        new KeyValuePair<string, string>("email",email),
                        new KeyValuePair<string, string>("hash",pass)
                    })
                    ).Result;
        }
        private HttpResponseMessage ChangePassword(HttpClient client, string oldHash, string newHash)
        {
            return client.PostAsync("User/ChangePassword",
                    new System.Net.Http.FormUrlEncodedContent(new List<KeyValuePair<string, string>>() {
                        new KeyValuePair<string, string>("oldHash",oldHash),
                        new KeyValuePair<string, string>("newHash",newHash)
                    })
                    ).Result;
        }
        private HttpResponseMessage Preauthenticate(HttpClient client, string email)
        {
            return client.PostAsync("User/Preauthenticate",
                    new System.Net.Http.FormUrlEncodedContent(new List<KeyValuePair<string, string>>() {
                        new KeyValuePair<string, string>("email",email)
                    })
                    ).Result;
        }
        private HttpResponseMessage CheckSlots(HttpClient client)
        {
            return client.PostAsync("Admin/CheckSlots",
                    new System.Net.Http.FormUrlEncodedContent(new List<KeyValuePair<string, string>>() {
                        new KeyValuePair<string, string>("testingDay","2020-10-31T00:00:00+00:00"),
                        new KeyValuePair<string, string>("from","10"),
                        new KeyValuePair<string, string>("until","12"),
                    })
                ).Result;
        }
        private HttpResponseMessage ListPlaces(HttpClient client)
        {
            return client.GetAsync("Place/List").Result;
        }
        private HttpResponseMessage ListDaySlotsByPlace(HttpClient client, string placeId)
        {
            return client.GetAsync($"Slot/ListDaySlotsByPlace?placeId={placeId}").Result;
        }
        private HttpResponseMessage ListHourSlotsByPlaceAndDaySlotId(HttpClient client, string placeId, string daySlotId)
        {
            return client.GetAsync($"Slot/ListHourSlotsByPlaceAndDaySlotId?placeId={placeId}&daySlotId={daySlotId}").Result;
        }
        private HttpResponseMessage ListMinuteSlotsByPlaceAndHourSlotId(HttpClient client, string placeId, string hourSlotId)
        {
            return client.GetAsync($"Slot/ListMinuteSlotsByPlaceAndHourSlotId?placeId={placeId}&hourSlotId={hourSlotId}").Result;
        }

        private HttpResponseMessage Register(HttpClient client, Visitor visitor)
        {
            var body = Newtonsoft.Json.JsonConvert.SerializeObject(visitor);
            return client.PostAsync("Visitor/Register",
                                new System.Net.Http.StringContent(body, Encoding.UTF8, "application/json")
                                ).Result;
        }
        private HttpResponseMessage RegisterByManager(HttpClient client, Visitor visitor)
        {
            var body = Newtonsoft.Json.JsonConvert.SerializeObject(visitor);
            return client.PostAsync("Visitor/RegisterByManager",
                                new System.Net.Http.StringContent(body, Encoding.UTF8, "application/json")
                                ).Result;
        }

        private HttpResponseMessage ConnectVisitorToTest(HttpClient client, string visitorCode, string testCode)
        {
            return client.PostAsync("Result/ConnectVisitorToTest",
                    new System.Net.Http.FormUrlEncodedContent(new List<KeyValuePair<string, string>>() {
                        new KeyValuePair<string, string>("visitorCode",visitorCode),
                        new KeyValuePair<string, string>("testCode",testCode),
                    })
                ).Result;
        }

        private HttpResponseMessage GetVisitor(HttpClient client, string visitorCode)
        {
            return client.PostAsync("Result/GetVisitor",
                    new System.Net.Http.FormUrlEncodedContent(new List<KeyValuePair<string, string>>() {
                        new KeyValuePair<string, string>("visitorCode",visitorCode),
                    })
                ).Result;
        }
        private HttpResponseMessage GetVisitorByRC(HttpClient client, string rc)
        {
            return client.PostAsync("Result/GetVisitorByRC",
                    new System.Net.Http.FormUrlEncodedContent(new List<KeyValuePair<string, string>>() {
                        new KeyValuePair<string, string>("rc",rc),
                    })
                ).Result;
        }
        private HttpResponseMessage GetNextTest(HttpClient client)
        {
            return client.GetAsync("Result/GetNextTest").Result;
        }
        private HttpResponseMessage RemoveFromDocQueue(HttpClient client, string testId)
        {
            return client.PostAsync("Result/RemoveFromDocQueue",
                    new System.Net.Http.FormUrlEncodedContent(new List<KeyValuePair<string, string>>() {
                        new KeyValuePair<string, string>("testId",testId),
                    })
                ).Result;
        }
        private HttpResponseMessage SetResult(HttpClient client, string testCode, string result)
        {
            return client.PostAsync("Result/SetResult",
                    new System.Net.Http.FormUrlEncodedContent(new List<KeyValuePair<string, string>>() {
                        new KeyValuePair<string, string>("testCode",testCode),
                        new KeyValuePair<string, string>("result",result),
                    })
                ).Result;
        }
        private HttpResponseMessage PublicGetTestResult(HttpClient client, string code, string pass)
        {
            return client.PostAsync("Result/Get",
                    new System.Net.Http.FormUrlEncodedContent(new List<KeyValuePair<string, string>>() {
                        new KeyValuePair<string, string>("code",code),
                        new KeyValuePair<string, string>("pass",pass),
                    })
                ).Result;
        }
        private HttpResponseMessage PublicRemoveTest(HttpClient client, string code, string pass)
        {
            return client.PostAsync("Result/RemoveTest",
                    new System.Net.Http.FormUrlEncodedContent(new List<KeyValuePair<string, string>>() {
                        new KeyValuePair<string, string>("code",code),
                        new KeyValuePair<string, string>("pass",pass),
                    })
                ).Result;
        }

        private HttpResponseMessage SetLocation(HttpClient client, string placeId)
        {
            return client.PostAsync("User/SetLocation",
                    new System.Net.Http.FormUrlEncodedContent(new List<KeyValuePair<string, string>>() {
                        new KeyValuePair<string, string>("placeId",placeId),
                    })
                ).Result;
        }



        private List<Visitor> RegisterTestVisitors(HttpClient client, string placeId, long slotId)
        {
            var Registered = new List<Visitor>();
            Visitor visitor1 = new Visitor()
            {
                Address = "addr",
                Email = "email@scholtz.sk",
                ChosenPlaceId = placeId,
                ChosenSlot = slotId,
                FirstName = "F1",
                LastName = "L1",
                Insurance = "25",
                PersonType = "idcard",
                Phone = "+421907000999",
                RC = "0101010008",

            };
            var result = Register(client, visitor1);
            if (result.StatusCode != HttpStatusCode.OK) throw new Exception("Unable to make visitor");
            Registered.Add(Newtonsoft.Json.JsonConvert.DeserializeObject<Visitor>(result.Content.ReadAsStringAsync().Result));
            Visitor visitor2 = new Visitor()
            {
                Address = "addr",
                Email = "email@scholtz.sk",
                ChosenPlaceId = placeId,
                ChosenSlot = slotId,
                FirstName = "F",
                LastName = "L",
                Insurance = "25",
                PersonType = "idcard",
                Phone = "+421",
                RC = "0101010019",
            };
            result = Register(client, visitor2);
            if (result.StatusCode != HttpStatusCode.OK) throw new Exception("Unable to make visitor");
            Registered.Add(Newtonsoft.Json.JsonConvert.DeserializeObject<Visitor>(result.Content.ReadAsStringAsync().Result));

            return Registered;
        }

        [Test]
        public void AuthorizeTest()
        {
            using var web = new MockWebApp();
            var client = web.CreateClient();
            var users = configuration.GetSection("AdminUsers").Get<CovidMassTesting.Model.Settings.User[]>();//.GetValue<List<CovidMassTesting.Model.Settings.User>>("AdminUsers");

            var user = users.First(u => u.Name == "Admin");
            /// Preauthenticate
            var request = Preauthenticate(client, user.Email);
            Assert.AreEqual(HttpStatusCode.OK, request.StatusCode, request.Content.ReadAsStringAsync().Result);
            var authData = JsonConvert.DeserializeObject<AuthData>(request.Content.ReadAsStringAsync().Result);
            var cohash = authData.CoHash;
            var rand = authData.CoData;

            /// Authenticate
            var pass = user.Password;
            for (int i = 0; i < 99; i++)
            {
                pass = Encoding.ASCII.GetBytes($"{pass}{cohash}").GetSHA256Hash();
            }
            pass = Encoding.ASCII.GetBytes($"{pass}{rand}").GetSHA256Hash();

            request = Authenticate(client, user.Email + "1", pass);
            Assert.AreEqual(HttpStatusCode.BadRequest, request.StatusCode, request.Content.ReadAsStringAsync().Result);
            Task.Delay(1000).Wait();
            request = Authenticate(client, user.Email, pass + "1");
            Assert.AreEqual(HttpStatusCode.BadRequest, request.StatusCode, request.Content.ReadAsStringAsync().Result);
            // Test brute force
            request = Authenticate(client, user.Email, pass);
            Assert.AreEqual(HttpStatusCode.BadRequest, request.StatusCode, request.Content.ReadAsStringAsync().Result);

            Task.Delay(1000).Wait();
            request = Authenticate(client, user.Email, pass);
            Assert.AreEqual(HttpStatusCode.OK, request.StatusCode, request.Content.ReadAsStringAsync().Result);

            var token = request.Content.ReadAsStringAsync().Result;
            Assert.IsFalse(string.IsNullOrEmpty(token));
            client.DefaultRequestHeaders.Add("Authorization", $"Bearer {token}");

            request = CheckSlots(client);
            Assert.AreEqual(HttpStatusCode.OK, request.StatusCode, request.Content.ReadAsStringAsync().Result);
        }



        [Test]
        public void ChangePasswordTest()
        {
            using var web = new MockWebApp();
            var client = web.CreateClient();
            var users = configuration.GetSection("AdminUsers").Get<CovidMassTesting.Model.Settings.User[]>();//.GetValue<List<CovidMassTesting.Model.Settings.User>>("AdminUsers");

            var user = users.First(u => u.Name == "Admin");
            /// Preauthenticate
            var request = Preauthenticate(client, user.Email);
            Assert.AreEqual(HttpStatusCode.OK, request.StatusCode, request.Content.ReadAsStringAsync().Result);
            var authData = JsonConvert.DeserializeObject<AuthData>(request.Content.ReadAsStringAsync().Result);

            /// Authenticate
            var pass = user.Password;
            for (int i = 0; i < 99; i++)
            {
                pass = Encoding.ASCII.GetBytes($"{pass}{authData.CoHash}").GetSHA256Hash();
            }
            pass = Encoding.ASCII.GetBytes($"{pass}{authData.CoData}").GetSHA256Hash();

            request = Authenticate(client, user.Email, pass);
            Assert.AreEqual(HttpStatusCode.OK, request.StatusCode, request.Content.ReadAsStringAsync().Result);
            var adminToken = request.Content.ReadAsStringAsync().Result;
            client.DefaultRequestHeaders.Add("Authorization", $"Bearer {adminToken}");

            request = Preauthenticate(client, user.Email);
            Assert.AreEqual(HttpStatusCode.OK, request.StatusCode, request.Content.ReadAsStringAsync().Result);
            authData = JsonConvert.DeserializeObject<AuthData>(request.Content.ReadAsStringAsync().Result);
            /// Authenticate
            pass = user.Password;
            for (int i = 0; i < 99; i++)
            {
                pass = Encoding.ASCII.GetBytes($"{pass}{authData.CoHash}").GetSHA256Hash();
            }

            var newPassword = "New Password";
            var newPasswordHash = newPassword;
            for (int i = 0; i < 99; i++)
            {
                newPasswordHash = Encoding.ASCII.GetBytes($"{newPasswordHash}{authData.CoData}").GetSHA256Hash();
            }
            request = ChangePassword(client, pass, newPasswordHash);

            Assert.AreEqual(HttpStatusCode.OK, request.StatusCode, request.Content.ReadAsStringAsync().Result);

            // test to authenticate with new login
            request = Preauthenticate(client, user.Email);
            Assert.AreEqual(HttpStatusCode.OK, request.StatusCode, request.Content.ReadAsStringAsync().Result);
            authData = JsonConvert.DeserializeObject<AuthData>(request.Content.ReadAsStringAsync().Result);

            /// Authenticate
            pass = newPassword;
            for (int i = 0; i < 99; i++)
            {
                pass = Encoding.ASCII.GetBytes($"{pass}{authData.CoHash}").GetSHA256Hash();
            }
            pass = Encoding.ASCII.GetBytes($"{pass}{authData.CoData}").GetSHA256Hash();

            request = Authenticate(client, user.Email, pass);
            Assert.AreEqual(HttpStatusCode.OK, request.StatusCode, request.Content.ReadAsStringAsync().Result);
        }

        [Test]
        public void VisitorTest()
        {
            using var web = new MockWebApp();
            var client = web.CreateClient();
            var users = configuration.GetSection("AdminUsers").Get<CovidMassTesting.Model.Settings.User[]>();
            var admin = users.First(u => u.Name == "Admin");

            var request = AuthenticateUser(client, admin.Email, admin.Password);
            Assert.AreEqual(HttpStatusCode.OK, request.StatusCode, request.Content.ReadAsStringAsync().Result);
            var adminToken = request.Content.ReadAsStringAsync().Result;
            Assert.IsFalse(string.IsNullOrEmpty(adminToken));
            client.DefaultRequestHeaders.Add("Authorization", $"Bearer {adminToken}");

            request = CheckSlots(client);
            Assert.AreEqual(HttpStatusCode.OK, request.StatusCode, request.Content.ReadAsStringAsync().Result);

            client.DefaultRequestHeaders.Clear();

            request = ListPlaces(client);
            Assert.AreEqual(HttpStatusCode.OK, request.StatusCode, request.Content.ReadAsStringAsync().Result);
            var places = Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, Place>>(request.Content.ReadAsStringAsync().Result);
            Assert.IsTrue(places.Count > 0);
            var place = places.First().Value;
            request = ListDaySlotsByPlace(client, place.Id);
            Assert.AreEqual(HttpStatusCode.OK, request.StatusCode, request.Content.ReadAsStringAsync().Result);
            var days = Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, Slot1Day>>(request.Content.ReadAsStringAsync().Result);
            Assert.IsTrue(days.Count > 0);

            var day = days.First().Value;
            request = ListHourSlotsByPlaceAndDaySlotId(client, place.Id, day.SlotId.ToString());
            Assert.AreEqual(HttpStatusCode.OK, request.StatusCode, request.Content.ReadAsStringAsync().Result);
            var hours = Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, Slot1Hour>>(request.Content.ReadAsStringAsync().Result);
            Assert.IsTrue(hours.Count > 0);

            var hour = hours.First().Value;
            request = ListMinuteSlotsByPlaceAndHourSlotId(client, place.Id, hour.SlotId.ToString());
            Assert.AreEqual(HttpStatusCode.OK, request.StatusCode, request.Content.ReadAsStringAsync().Result);
            var minutes = Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, Slot5Min>>(request.Content.ReadAsStringAsync().Result);
            Assert.IsTrue(minutes.Count > 0);

            var minute = minutes.Values.First();

            Visitor visitor = new Visitor()
            {
                Address = "addr",
                Email = "email@scholtz.sk",
                ChosenPlaceId = place.Id,
                ChosenSlot = minute.SlotId,
                FirstName = "Ľudovít",
                LastName = "Scholtz",
                Insurance = "25",
                PersonType = "idcard",
                Phone = "+420 776082012",
                RC = "0101010008",

            };
            request = Register(client, visitor);
            Assert.AreEqual(HttpStatusCode.OK, request.StatusCode, request.Content.ReadAsStringAsync().Result);
            var responsedVisitor = Newtonsoft.Json.JsonConvert.DeserializeObject<Visitor>(request.Content.ReadAsStringAsync().Result);
            Assert.IsTrue(responsedVisitor.Id > 100000000);
            Assert.AreEqual(visitor.Address, responsedVisitor.Address);
            Assert.AreEqual(visitor.ChosenPlaceId, responsedVisitor.ChosenPlaceId);
            Assert.AreEqual(visitor.ChosenSlot, responsedVisitor.ChosenSlot);
            Assert.AreEqual(visitor.Email, responsedVisitor.Email);
            Assert.AreEqual(visitor.FirstName, responsedVisitor.FirstName);
            Assert.AreEqual(visitor.Insurance, responsedVisitor.Insurance);
            Assert.AreEqual(visitor.RC, responsedVisitor.RC);
            Assert.AreEqual("+421000000000", responsedVisitor.Phone);
            Assert.AreEqual(TestResult.NotTaken, responsedVisitor.Result);

            request = ListDaySlotsByPlace(client, place.Id);
            Assert.AreEqual(HttpStatusCode.OK, request.StatusCode, request.Content.ReadAsStringAsync().Result);
            days = Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, Slot1Day>>(request.Content.ReadAsStringAsync().Result);
            Assert.IsTrue(days.Count > 0);
            day = days[day.SlotId.ToString()];
            Assert.AreEqual(1, day.Registrations);

            request = ListHourSlotsByPlaceAndDaySlotId(client, place.Id, day.SlotId.ToString());
            Assert.AreEqual(HttpStatusCode.OK, request.StatusCode, request.Content.ReadAsStringAsync().Result);
            hours = Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, Slot1Hour>>(request.Content.ReadAsStringAsync().Result);
            Assert.IsTrue(hours.Count > 0);
            hour = hours[hour.SlotId.ToString()];
            Assert.AreEqual(1, hour.Registrations);

            request = ListMinuteSlotsByPlaceAndHourSlotId(client, place.Id, hour.SlotId.ToString());
            Assert.AreEqual(HttpStatusCode.OK, request.StatusCode, request.Content.ReadAsStringAsync().Result);
            minutes = Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, Slot5Min>>(request.Content.ReadAsStringAsync().Result);
            Assert.IsTrue(minutes.Count > 0);
            minute = minutes[minute.SlotId.ToString()];
            Assert.AreEqual(1, hour.Registrations);
        }


        [Test]
        public void RoleRegistrationManagerTest()
        {
            using var web = new MockWebApp();
            var client = web.CreateClient();
            var users = configuration.GetSection("AdminUsers").Get<CovidMassTesting.Model.Settings.User[]>();

            var admin = users.First(u => u.Name == "Admin");
            var request = AuthenticateUser(client, admin.Email, admin.Password);
            Assert.AreEqual(HttpStatusCode.OK, request.StatusCode, request.Content.ReadAsStringAsync().Result);
            var adminToken = request.Content.ReadAsStringAsync().Result;
            Assert.IsFalse(string.IsNullOrEmpty(adminToken));
            client.DefaultRequestHeaders.Add("Authorization", $"Bearer {adminToken}");

            request = CheckSlots(client);
            Assert.AreEqual(HttpStatusCode.OK, request.StatusCode, request.Content.ReadAsStringAsync().Result);

            client.DefaultRequestHeaders.Clear();

            request = ListPlaces(client);
            Assert.AreEqual(HttpStatusCode.OK, request.StatusCode, request.Content.ReadAsStringAsync().Result);
            var places = Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, Place>>(request.Content.ReadAsStringAsync().Result);
            Assert.IsTrue(places.Count > 0);
            var place = places.First().Value;
            request = ListDaySlotsByPlace(client, place.Id);
            Assert.AreEqual(HttpStatusCode.OK, request.StatusCode, request.Content.ReadAsStringAsync().Result);
            var days = Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, Slot1Day>>(request.Content.ReadAsStringAsync().Result);
            Assert.IsTrue(days.Count > 0);

            var day = days.First().Value;
            request = ListHourSlotsByPlaceAndDaySlotId(client, place.Id, day.SlotId.ToString());
            Assert.AreEqual(HttpStatusCode.OK, request.StatusCode, request.Content.ReadAsStringAsync().Result);
            var hours = Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, Slot1Hour>>(request.Content.ReadAsStringAsync().Result);
            Assert.IsTrue(hours.Count > 0);

            var hour = hours.First().Value;
            request = ListMinuteSlotsByPlaceAndHourSlotId(client, place.Id, hour.SlotId.ToString());
            Assert.AreEqual(HttpStatusCode.OK, request.StatusCode, request.Content.ReadAsStringAsync().Result);
            var minutes = Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, Slot5Min>>(request.Content.ReadAsStringAsync().Result);
            Assert.IsTrue(minutes.Count > 0);

            var minute = minutes.Values.First();

            var registered = RegisterTestVisitors(client, place.Id, minute.SlotId);
            Assert.IsTrue(registered.Count >= 2);
            var registrationManager = users.First(u => u.Name == "RegistrationManager");
            request = AuthenticateUser(client, registrationManager.Email, registrationManager.Password);
            Assert.AreEqual(HttpStatusCode.OK, request.StatusCode, request.Content.ReadAsStringAsync().Result);
            var registrationManagerToken = request.Content.ReadAsStringAsync().Result;
            Assert.IsFalse(string.IsNullOrEmpty(registrationManagerToken));
            client.DefaultRequestHeaders.Add("Authorization", $"Bearer {registrationManagerToken}");

            var user1 = registered.First();

            // Set invalid place
            request = SetLocation(client, "undefined");
            Assert.AreEqual(HttpStatusCode.BadRequest, request.StatusCode, request.Content.ReadAsStringAsync().Result);
            // Set place
            request = SetLocation(client, place.Id);
            Assert.AreEqual(HttpStatusCode.OK, request.StatusCode, request.Content.ReadAsStringAsync().Result);


            request = GetVisitor(client, user1.Id.ToString());
            Assert.AreEqual(HttpStatusCode.OK, request.StatusCode, request.Content.ReadAsStringAsync().Result);
            var responseVisitor = Newtonsoft.Json.JsonConvert.DeserializeObject<Visitor>(request.Content.ReadAsStringAsync().Result);
            Assert.AreEqual(user1.RC, responseVisitor.RC);
            Assert.AreEqual(user1.FirstName, responseVisitor.FirstName);
            Assert.AreEqual(user1.LastName, responseVisitor.LastName);
            Assert.AreEqual(user1.Address, responseVisitor.Address);


            request = GetVisitorByRC(client, user1.RC);
            Assert.AreEqual(HttpStatusCode.OK, request.StatusCode, request.Content.ReadAsStringAsync().Result);
            responseVisitor = Newtonsoft.Json.JsonConvert.DeserializeObject<Visitor>(request.Content.ReadAsStringAsync().Result);
            Assert.AreEqual(user1.RC, responseVisitor.RC);
            Assert.AreEqual(user1.FirstName, responseVisitor.FirstName);
            Assert.AreEqual(user1.LastName, responseVisitor.LastName);
            Assert.AreEqual(user1.Address, responseVisitor.Address);

            request = ConnectVisitorToTest(client, user1.Id.ToString(), "111-111-111");
            Assert.AreEqual(HttpStatusCode.OK, request.StatusCode, request.Content.ReadAsStringAsync().Result);


            Visitor visitor = new Visitor()
            {
                Address = "addr",
                Email = "email@scholtz.sk",
                FirstName = "F",
                LastName = "L",
                Insurance = "25",
                PersonType = "idcard",
                Phone = "+421907000000",
                RC = "0101010008",
            };
            request = RegisterByManager(client, visitor);

            Assert.AreEqual(HttpStatusCode.OK, request.StatusCode, request.Content.ReadAsStringAsync().Result);
            var responsedVisitor = Newtonsoft.Json.JsonConvert.DeserializeObject<Visitor>(request.Content.ReadAsStringAsync().Result);
            Assert.IsTrue(responsedVisitor.Id > 100000000);
            Assert.AreEqual(visitor.Address, responsedVisitor.Address);
            Assert.AreEqual(place.Id, responsedVisitor.ChosenPlaceId);
            Assert.IsTrue(responsedVisitor.ChosenSlot > 0);
            Assert.AreEqual(visitor.Email, responsedVisitor.Email);
            Assert.AreEqual(visitor.FirstName, responsedVisitor.FirstName);
            Assert.AreEqual(visitor.Insurance, responsedVisitor.Insurance);
            Assert.AreEqual(visitor.RC, responsedVisitor.RC);
            Assert.AreEqual(visitor.Phone, responsedVisitor.Phone);
            Assert.AreEqual(TestResult.NotTaken, responsedVisitor.Result);
        }


        [Test]
        public void RoleMedicTesterTest()
        {
            using var web = new MockWebApp();
            var client = web.CreateClient();
            var users = configuration.GetSection("AdminUsers").Get<CovidMassTesting.Model.Settings.User[]>();

            var admin = users.First(u => u.Name == "Admin");
            var request = AuthenticateUser(client, admin.Email, admin.Password);
            Assert.AreEqual(HttpStatusCode.OK, request.StatusCode, request.Content.ReadAsStringAsync().Result);
            var adminToken = request.Content.ReadAsStringAsync().Result;
            Assert.IsFalse(string.IsNullOrEmpty(adminToken));
            client.DefaultRequestHeaders.Add("Authorization", $"Bearer {adminToken}");

            request = CheckSlots(client);
            Assert.AreEqual(HttpStatusCode.OK, request.StatusCode, request.Content.ReadAsStringAsync().Result);

            client.DefaultRequestHeaders.Clear();

            request = ListPlaces(client);
            Assert.AreEqual(HttpStatusCode.OK, request.StatusCode, request.Content.ReadAsStringAsync().Result);
            var places = Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, Place>>(request.Content.ReadAsStringAsync().Result);
            Assert.IsTrue(places.Count > 0);
            var place = places.First().Value;
            request = ListDaySlotsByPlace(client, place.Id);
            Assert.AreEqual(HttpStatusCode.OK, request.StatusCode, request.Content.ReadAsStringAsync().Result);
            var days = Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, Slot1Day>>(request.Content.ReadAsStringAsync().Result);
            Assert.IsTrue(days.Count > 0);

            var day = days.First().Value;
            request = ListHourSlotsByPlaceAndDaySlotId(client, place.Id, day.SlotId.ToString());
            Assert.AreEqual(HttpStatusCode.OK, request.StatusCode, request.Content.ReadAsStringAsync().Result);
            var hours = Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, Slot1Hour>>(request.Content.ReadAsStringAsync().Result);
            Assert.IsTrue(hours.Count > 0);

            var hour = hours.First().Value;
            request = ListMinuteSlotsByPlaceAndHourSlotId(client, place.Id, hour.SlotId.ToString());
            Assert.AreEqual(HttpStatusCode.OK, request.StatusCode, request.Content.ReadAsStringAsync().Result);
            var minutes = Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, Slot5Min>>(request.Content.ReadAsStringAsync().Result);
            Assert.IsTrue(minutes.Count > 0);

            var minute = minutes.Values.First();
            var registered = RegisterTestVisitors(client, place.Id, minute.SlotId);
            Assert.IsTrue(registered.Count >= 2);
            var registrationManager = users.First(u => u.Name == "RegistrationManager");
            request = AuthenticateUser(client, registrationManager.Email, registrationManager.Password);
            Assert.AreEqual(HttpStatusCode.OK, request.StatusCode, request.Content.ReadAsStringAsync().Result);
            var registrationManagerToken = request.Content.ReadAsStringAsync().Result;
            Assert.IsFalse(string.IsNullOrEmpty(registrationManagerToken));
            client.DefaultRequestHeaders.Add("Authorization", $"Bearer {registrationManagerToken}");

            string test1 = "111-111-111";
            request = ConnectVisitorToTest(client, registered[0].Id.ToString(), test1);
            Assert.AreEqual(HttpStatusCode.OK, request.StatusCode, request.Content.ReadAsStringAsync().Result);

            string test2 = "222-222-222";
            request = ConnectVisitorToTest(client, registered[1].Id.ToString(), test2);
            Assert.AreEqual(HttpStatusCode.OK, request.StatusCode, request.Content.ReadAsStringAsync().Result);

            // TEST mark as sick
            request = SetResult(client, test1, TestResult.PositiveWaitingForCertificate);
            Assert.AreEqual(HttpStatusCode.OK, request.StatusCode, request.Content.ReadAsStringAsync().Result);
            var result = Newtonsoft.Json.JsonConvert.DeserializeObject<Result>(request.Content.ReadAsStringAsync().Result);
            Assert.AreEqual(TestResult.PositiveWaitingForCertificate, result.State);

            // TEST mark as wrong code
            request = SetResult(client, test1, TestResult.PositiveCertificateTaken);
            Assert.AreEqual(HttpStatusCode.BadRequest, request.StatusCode);

            // TEST mark as sick
            request = SetResult(client, test2, TestResult.NegativeWaitingForCertificate);
            Assert.AreEqual(HttpStatusCode.OK, request.StatusCode, request.Content.ReadAsStringAsync().Result);
            result = Newtonsoft.Json.JsonConvert.DeserializeObject<Result>(request.Content.ReadAsStringAsync().Result);
            Assert.AreEqual(TestResult.NegativeWaitingForCertificate, result.State);
            client.DefaultRequestHeaders.Clear();

            request = PublicGetTestResult(client, registered[0].Id.ToString(), registered[0].RC.Substring(6, 4));
            Assert.AreEqual(HttpStatusCode.OK, request.StatusCode, request.Content.ReadAsStringAsync().Result);
            result = Newtonsoft.Json.JsonConvert.DeserializeObject<Result>(request.Content.ReadAsStringAsync().Result);
            Assert.AreEqual(TestResult.PositiveWaitingForCertificate, result.State);

            request = PublicGetTestResult(client, registered[1].Id.ToString(), registered[1].RC.Substring(6, 4));
            Assert.AreEqual(HttpStatusCode.OK, request.StatusCode, request.Content.ReadAsStringAsync().Result);
            result = Newtonsoft.Json.JsonConvert.DeserializeObject<Result>(request.Content.ReadAsStringAsync().Result);
            Assert.AreEqual(TestResult.NegativeWaitingForCertificate, result.State);
        }


        [Test]
        public void RoleDocumentManagerTest()
        {
            using var web = new MockWebApp();
            var client = web.CreateClient();
            var users = configuration.GetSection("AdminUsers").Get<CovidMassTesting.Model.Settings.User[]>();

            var admin = users.First(u => u.Name == "Admin");
            var request = AuthenticateUser(client, admin.Email, admin.Password);
            Assert.AreEqual(HttpStatusCode.OK, request.StatusCode, request.Content.ReadAsStringAsync().Result);
            var adminToken = request.Content.ReadAsStringAsync().Result;
            Assert.IsFalse(string.IsNullOrEmpty(adminToken));
            client.DefaultRequestHeaders.Add("Authorization", $"Bearer {adminToken}");

            request = CheckSlots(client);
            Assert.AreEqual(HttpStatusCode.OK, request.StatusCode, request.Content.ReadAsStringAsync().Result);

            client.DefaultRequestHeaders.Clear();

            request = ListPlaces(client);
            Assert.AreEqual(HttpStatusCode.OK, request.StatusCode, request.Content.ReadAsStringAsync().Result);
            var places = Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, Place>>(request.Content.ReadAsStringAsync().Result);
            Assert.IsTrue(places.Count > 0);
            var place = places.First().Value;
            request = ListDaySlotsByPlace(client, place.Id);
            Assert.AreEqual(HttpStatusCode.OK, request.StatusCode, request.Content.ReadAsStringAsync().Result);
            var days = Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, Slot1Day>>(request.Content.ReadAsStringAsync().Result);
            Assert.IsTrue(days.Count > 0);

            var day = days.First().Value;
            request = ListHourSlotsByPlaceAndDaySlotId(client, place.Id, day.SlotId.ToString());
            Assert.AreEqual(HttpStatusCode.OK, request.StatusCode, request.Content.ReadAsStringAsync().Result);
            var hours = Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, Slot1Hour>>(request.Content.ReadAsStringAsync().Result);
            Assert.IsTrue(hours.Count > 0);

            var hour = hours.First().Value;
            request = ListMinuteSlotsByPlaceAndHourSlotId(client, place.Id, hour.SlotId.ToString());
            Assert.AreEqual(HttpStatusCode.OK, request.StatusCode, request.Content.ReadAsStringAsync().Result);
            var minutes = Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, Slot5Min>>(request.Content.ReadAsStringAsync().Result);
            Assert.IsTrue(minutes.Count > 0);

            var minute = minutes.Values.First();
            var registered = RegisterTestVisitors(client, place.Id, minute.SlotId);
            Assert.IsTrue(registered.Count >= 2);
            var registrationManager = users.First(u => u.Name == "RegistrationManager");
            request = AuthenticateUser(client, registrationManager.Email, registrationManager.Password);
            Assert.AreEqual(HttpStatusCode.OK, request.StatusCode, request.Content.ReadAsStringAsync().Result);
            var registrationManagerToken = request.Content.ReadAsStringAsync().Result;
            Assert.IsFalse(string.IsNullOrEmpty(registrationManagerToken));
            client.DefaultRequestHeaders.Add("Authorization", $"Bearer {registrationManagerToken}");

            string test1 = "111-111-111";
            request = ConnectVisitorToTest(client, registered[0].Id.ToString(), test1);
            Assert.AreEqual(HttpStatusCode.OK, request.StatusCode, request.Content.ReadAsStringAsync().Result);

            string test2 = "222-222-222";
            request = ConnectVisitorToTest(client, registered[1].Id.ToString(), test2);
            Assert.AreEqual(HttpStatusCode.OK, request.StatusCode, request.Content.ReadAsStringAsync().Result);

            // TEST mark as sick
            request = SetResult(client, test1, TestResult.PositiveWaitingForCertificate);
            Assert.AreEqual(HttpStatusCode.OK, request.StatusCode, request.Content.ReadAsStringAsync().Result);
            var result = Newtonsoft.Json.JsonConvert.DeserializeObject<Result>(request.Content.ReadAsStringAsync().Result);
            Assert.AreEqual(TestResult.PositiveWaitingForCertificate, result.State);

            // TEST mark as sick
            request = SetResult(client, test2, TestResult.NegativeWaitingForCertificate);
            Assert.AreEqual(HttpStatusCode.OK, request.StatusCode, request.Content.ReadAsStringAsync().Result);
            result = Newtonsoft.Json.JsonConvert.DeserializeObject<Result>(request.Content.ReadAsStringAsync().Result);
            Assert.AreEqual(TestResult.NegativeWaitingForCertificate, result.State);
            client.DefaultRequestHeaders.Clear();

            var documentManager = users.First(u => u.Name == "DocumentManager");
            request = AuthenticateUser(client, documentManager.Email, documentManager.Password);
            Assert.AreEqual(HttpStatusCode.OK, request.StatusCode, request.Content.ReadAsStringAsync().Result);
            var documentManagerToken = request.Content.ReadAsStringAsync().Result;
            Assert.IsFalse(string.IsNullOrEmpty(registrationManagerToken));
            client.DefaultRequestHeaders.Add("Authorization", $"Bearer {documentManagerToken}");

            // Test fetch one document to fill in.
            // It must be in queue, so the first one we have marked as result
            request = GetNextTest(client);
            Assert.AreEqual(HttpStatusCode.OK, request.StatusCode, request.Content.ReadAsStringAsync().Result);
            var visitorForDocumenter = Newtonsoft.Json.JsonConvert.DeserializeObject<Visitor>(request.Content.ReadAsStringAsync().Result);
            Assert.AreEqual(test1.Replace("-", ""), visitorForDocumenter.TestingSet);
            Assert.AreEqual(TestResult.PositiveWaitingForCertificate, visitorForDocumenter.Result);
            // Repeated request must return the same thing until we mark it as processed
            request = GetNextTest(client);
            Assert.AreEqual(HttpStatusCode.OK, request.StatusCode, request.Content.ReadAsStringAsync().Result);
            visitorForDocumenter = Newtonsoft.Json.JsonConvert.DeserializeObject<Visitor>(request.Content.ReadAsStringAsync().Result);
            Assert.AreEqual(test1.Replace("-", ""), visitorForDocumenter.TestingSet);
            Assert.AreEqual(TestResult.PositiveWaitingForCertificate, visitorForDocumenter.Result);

            request = RemoveFromDocQueue(client, test1);
            Assert.AreEqual(HttpStatusCode.OK, request.StatusCode, request.Content.ReadAsStringAsync().Result);

            request = GetNextTest(client);
            Assert.AreEqual(HttpStatusCode.OK, request.StatusCode, request.Content.ReadAsStringAsync().Result);
            visitorForDocumenter = Newtonsoft.Json.JsonConvert.DeserializeObject<Visitor>(request.Content.ReadAsStringAsync().Result);
            Assert.AreEqual(test2.Replace("-", ""), visitorForDocumenter.TestingSet);
            Assert.AreEqual(TestResult.NegativeWaitingForCertificate, visitorForDocumenter.Result);

            request = RemoveFromDocQueue(client, test2);
            Assert.AreEqual(HttpStatusCode.OK, request.StatusCode, request.Content.ReadAsStringAsync().Result);

            request = GetNextTest(client);
            Assert.AreEqual(HttpStatusCode.NoContent, request.StatusCode, request.Content.ReadAsStringAsync().Result);
            result = Newtonsoft.Json.JsonConvert.DeserializeObject<Result>(request.Content.ReadAsStringAsync().Result);
            // When no other item is in queue, the server should return NoContent (204), with no data
            Assert.IsNull(result);

            // check public info
            request = PublicGetTestResult(client, registered[0].Id.ToString(), registered[0].RC.Substring(6, 4));
            Assert.AreEqual(HttpStatusCode.OK, request.StatusCode, request.Content.ReadAsStringAsync().Result);
            result = Newtonsoft.Json.JsonConvert.DeserializeObject<Result>(request.Content.ReadAsStringAsync().Result);
            Assert.AreEqual(TestResult.PositiveCertificateTaken, result.State);

            request = PublicGetTestResult(client, registered[1].Id.ToString(), registered[1].RC.Substring(6, 4));
            Assert.AreEqual(HttpStatusCode.OK, request.StatusCode, request.Content.ReadAsStringAsync().Result);
            result = Newtonsoft.Json.JsonConvert.DeserializeObject<Result>(request.Content.ReadAsStringAsync().Result);
            Assert.AreEqual(TestResult.NegativeCertificateTaken, result.State);

            // remove my private information from system
            request = PublicRemoveTest(client, registered[1].Id.ToString(), registered[1].RC.Substring(6, 4));
            Assert.AreEqual(HttpStatusCode.OK, request.StatusCode, request.Content.ReadAsStringAsync().Result);
            bool resultBool = Newtonsoft.Json.JsonConvert.DeserializeObject<bool>(request.Content.ReadAsStringAsync().Result);
            Assert.IsTrue(resultBool);

            // unable to delete removed test
            request = PublicRemoveTest(client, registered[1].Id.ToString(), registered[1].RC.Substring(6, 4));
            Assert.AreEqual(HttpStatusCode.BadRequest, request.StatusCode, request.Content.ReadAsStringAsync().Result);

        }

        public class MockWebApp : WebApplicationFactory<CovidMassTesting.Startup>
        {
            protected override void ConfigureWebHost(IWebHostBuilder builder)
            {

                builder.ConfigureTestServices(ConfigureServices);
                builder.ConfigureLogging((WebHostBuilderContext context, ILoggingBuilder loggingBuilder) =>
                {
                    loggingBuilder.ClearProviders();
                    loggingBuilder.AddConsole(options => options.IncludeScopes = true);
                });
                var configuration = new ConfigurationBuilder()
                    .AddJsonFile("appsettings.json")
                    .Build();
                builder.UseConfiguration(configuration);
            }

            protected virtual void ConfigureServices(IServiceCollection services)
            {
                var configuration = new ConfigurationBuilder()
                    .AddJsonFile("appsettings.json")
                    .Build();
                services.AddSingleton(typeof(IConfiguration), configuration);
            }
        }
    }
}