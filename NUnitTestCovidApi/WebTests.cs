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
using System.IdentityModel.Tokens.Jwt;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace NUnitTestCovidApi
{
    public class Tests
    {
        public virtual string AppSettings { get; set; } = "appsettings.json";
        private IConfiguration configuration;

        [SetUp]
        public virtual void Setup()
        {
            configuration = new ConfigurationBuilder()
                .AddJsonFile(AppSettings)
                .Build();
        }
        protected HttpResponseMessage CheckVersion(HttpClient client)
        {
            return client.GetAsync("Version").Result;
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
        private HttpResponseMessage SetPlaceProvider(HttpClient client, string placeProviderId)
        {
            return client.PostAsync("User/SetPlaceProvider",
                    new System.Net.Http.FormUrlEncodedContent(new List<KeyValuePair<string, string>>() {
                        new KeyValuePair<string, string>("placeProviderId",placeProviderId),
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
        protected HttpResponseMessage DropDatabase(HttpClient client, string hash)
        {
            return client.PostAsync("Admin/DropDatabase",
                    new System.Net.Http.FormUrlEncodedContent(new List<KeyValuePair<string, string>>() {
                        new KeyValuePair<string, string>("hash",hash),
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
        private HttpResponseMessage CheckSlotsDay1(HttpClient client)
        {
            return client.PostAsync("Admin/CheckSlots",
                    new System.Net.Http.FormUrlEncodedContent(new List<KeyValuePair<string, string>>() {
                        new KeyValuePair<string, string>("testingDay","2020-10-31T00:00:00+00:00"),
                        new KeyValuePair<string, string>("from","10"),
                        new KeyValuePair<string, string>("until","12"),
                    })
                ).Result;
        }
        private HttpResponseMessage CheckSlotsDay2(HttpClient client)
        {
            return client.PostAsync("Admin/CheckSlots",
                    new System.Net.Http.FormUrlEncodedContent(new List<KeyValuePair<string, string>>() {
                        new KeyValuePair<string, string>("testingDay","2020-11-29T00:00:00+00:00"),
                        new KeyValuePair<string, string>("from","10"),
                        new KeyValuePair<string, string>("until","12"),
                    })
                ).Result;
        }
        private HttpResponseMessage ListPlaces(HttpClient client)
        {
            return client.GetAsync("Place/List").Result;
        }
        private Place[] SetupDebugPlaces(HttpClient client)
        {
            var places = new Place[] {
                new Place()
                {
                    Id = Guid.NewGuid().ToString(),
                    Name = "Škola AA",

                    Address = "Bratislavská 1, Pezinok",
                    Lat = 48.28524902921143M,
                    Lng = 17.256517410278324M,
                    IsDriveIn = true,
                    IsWalkIn = false,
                    Registrations = 0,
                    OpeningHoursWorkDay = "10:00-12:00",
                    OpeningHoursOther1 = "11:45-11:50",
               },new Place()
                {
                    Id = Guid.NewGuid().ToString(),
                    Name = "Odberné miesto 2",
                    Address = "Pražská 11, Pezinok",
                    Lat = 48.29467191641477M,
                    Lng = 17.26587295532227M,
                    IsDriveIn = false,
                    IsWalkIn = true,
                    Registrations = 0,
                    OpeningHoursWorkDay = "08:30-22:00",
                    OpeningHoursOther1 = "11:00-13:00",
                },new Place()
                {
                    Id = Guid.NewGuid().ToString(),
                    Name = "Odberné miesto 3",
                    Address = "Pražská 10, Pezinok",
                    Lat = 48.289218275462225M,
                    Lng = 17.272996902465824M,
                    IsDriveIn = true,
                    IsWalkIn = true,
                    Registrations = 0,
                    OpeningHoursWorkDay = "08:00-09:00",
                    OpeningHoursOther1 = "09:00-10:00",
                }
            };
            var ret = new List<Place>();
            foreach (var place in places)
            {
                var body = Newtonsoft.Json.JsonConvert.SerializeObject(place);
                var response = client.PostAsync("Place/InsertOrUpdate",
                                    new System.Net.Http.StringContent(body, Encoding.UTF8, "application/json")
                                    ).Result;
                Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
                ret.Add(JsonConvert.DeserializeObject<Place>(response.Content.ReadAsStringAsync().Result));
            }
            return ret.ToArray();
        }
        private HttpResponseMessage ListDaySlotsByPlace(HttpClient client, string placeId)
        {
            return client.GetAsync($"Slot/ListDaySlotsByPlace?placeId={placeId}").Result;
        }
        private HttpResponseMessage PlaceProviderListPublic(HttpClient client)
        {
            return client.GetAsync($"PlaceProvider/ListPublic").Result;
        }
        private HttpResponseMessage ListPPInvites(HttpClient client)
        {
            return client.GetAsync($"User/ListPPInvites").Result;
        }
        private HttpResponseMessage ListUserInvites(HttpClient client)
        {
            return client.GetAsync($"User/ListUserInvites").Result;
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



        private HttpResponseMessage AllocatePersonsToPlace(HttpClient client, PersonAllocation[] allocations, string place)
        {
            var body = Newtonsoft.Json.JsonConvert.SerializeObject(allocations);
            return client.PostAsync($"PlaceProvider/AllocatePersonsToPlace?placeId={place}",
                                new System.Net.Http.StringContent(body, Encoding.UTF8, "application/json")
                                ).Result;
        }
        private HttpResponseMessage RemoveAllocationAtPlace(HttpClient client, string allocationId, string placeId)
        {
            return client.PostAsync("PlaceProvider/RemoveAllocationAtPlace",
                    new System.Net.Http.FormUrlEncodedContent(new List<KeyValuePair<string, string>>() {
                        new KeyValuePair<string, string>("allocationId",allocationId),
                        new KeyValuePair<string, string>("placeId",placeId),
                    })
                ).Result;
        }

        private HttpResponseMessage ListPlaceAllocations(HttpClient client, string place)
        {
            return client.GetAsync($"PlaceProvider/ListPlaceAllocations?placeId={place}").Result;
        }
        private HttpResponseMessage ScheduleOpenningHours(HttpClient client, TimeUpdate[] actions)
        {
            var body = Newtonsoft.Json.JsonConvert.SerializeObject(actions);
            return client.PostAsync("Place/ScheduleOpenningHours",
                                new System.Net.Http.StringContent(body, Encoding.UTF8, "application/json")
                                ).Result;
        }
        private HttpResponseMessage ListScheduledDays(HttpClient client)
        {
            return client.GetAsync("Place/ListScheduledDays").Result;
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
        private HttpResponseMessage ProcessInvitation(HttpClient client, string invitationId, bool accepted)
        {
            return client.PostAsync("User/ProcessInvitation",
                    new System.Net.Http.FormUrlEncodedContent(new List<KeyValuePair<string, string>>() {
                        new KeyValuePair<string, string>("invitationId",invitationId),
                        new KeyValuePair<string, string>("accepted",accepted.ToString()),
                    })
                ).Result;
        }

        private HttpResponseMessage InviteUserToPP(HttpClient client, string email, string name, string phone, string message)
        {
            return client.PostAsync("PlaceProvider/InviteUserToPP",
                    new System.Net.Http.FormUrlEncodedContent(new List<KeyValuePair<string, string>>() {
                        new KeyValuePair<string, string>("email",email),
                        new KeyValuePair<string, string>("name",name),
                        new KeyValuePair<string, string>("phone",phone),
                        new KeyValuePair<string, string>("message",message),
                    })
                ).Result;
        }

        private HttpResponseMessage PlaceProviderRegistration(HttpClient client, PlaceProvider pp)
        {
            var body = Newtonsoft.Json.JsonConvert.SerializeObject(pp);
            return client.PostAsync("PlaceProvider/Register",
                                new System.Net.Http.StringContent(body, Encoding.UTF8, "application/json")
                                ).Result;
        }

        private HttpResponseMessage FinalDataExport(HttpClient client, int from, int count)
        {
            client.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("text/csv"));
            var ret = client.GetAsync($"Result/FinalDataExport?from={from}&count={count}").Result;
            client.DefaultRequestHeaders.Accept.Clear();
            return ret;
        }
        private HttpResponseMessage SetLocation(HttpClient client, string placeId)
        {
            return client.PostAsync("User/SetLocation",
                    new System.Net.Http.FormUrlEncodedContent(new List<KeyValuePair<string, string>>() {
                        new KeyValuePair<string, string>("placeId",placeId),
                    })
                ).Result;
        }

        protected void DropDatabase()
        {
            using var web = new MockWebApp(AppSettings);
            var client = web.CreateClient();
            var users = configuration.GetSection("AdminUsers").Get<CovidMassTesting.Model.Settings.User[]>();
            var admin = users.First(u => u.Name == "Admin");
            var request = AuthenticateUser(client, admin.Email, admin.Password);
            var adminToken = request.Content.ReadAsStringAsync().Result;
            client.DefaultRequestHeaders.Add("Authorization", $"Bearer {adminToken}");

            request = Preauthenticate(client, admin.Email);
            var data = JsonConvert.DeserializeObject<AuthData>(request.Content.ReadAsStringAsync().Result);
            var cohash = data.CoHash;
            var rand = data.CoData;
            /// Authenticate
            var pass = admin.Password;
            for (int i = 0; i < 99; i++)
            {
                pass = Encoding.ASCII.GetBytes($"{pass}{cohash}").GetSHA256Hash();
            }
            pass = Encoding.ASCII.GetBytes($"{pass}{rand}").GetSHA256Hash();
            request = DropDatabase(client, pass);
            var dataDeleted = request.Content.ReadAsStringAsync().Result;
            Assert.AreEqual(HttpStatusCode.OK, request.StatusCode, dataDeleted);
            Console.WriteLine($"cleared {dataDeleted} items");
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
            DropDatabase();
            using var web = new MockWebApp(AppSettings);
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

            request = CheckSlotsDay1(client);
            Assert.AreEqual(HttpStatusCode.OK, request.StatusCode, request.Content.ReadAsStringAsync().Result);
        }



        [Test]
        public void ChangePasswordTest()
        {
            DropDatabase();

            using var web = new MockWebApp(AppSettings);
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
            DropDatabase();

            using var web = new MockWebApp(AppSettings);
            var client = web.CreateClient();
            var users = configuration.GetSection("AdminUsers").Get<CovidMassTesting.Model.Settings.User[]>();
            var admin = users.First(u => u.Name == "Admin");

            var request = AuthenticateUser(client, admin.Email, admin.Password);
            Assert.AreEqual(HttpStatusCode.OK, request.StatusCode, request.Content.ReadAsStringAsync().Result);
            var adminToken = request.Content.ReadAsStringAsync().Result;
            Assert.IsFalse(string.IsNullOrEmpty(adminToken));
            client.DefaultRequestHeaders.Add("Authorization", $"Bearer {adminToken}");

            SetupDebugPlaces(client);

            request = CheckSlotsDay1(client);
            Assert.AreEqual(HttpStatusCode.OK, request.StatusCode, request.Content.ReadAsStringAsync().Result);
            request = CheckSlotsDay2(client);
            Assert.AreEqual(HttpStatusCode.OK, request.StatusCode, request.Content.ReadAsStringAsync().Result);

            client.DefaultRequestHeaders.Clear();

            request = ListPlaces(client);
            Assert.AreEqual(HttpStatusCode.OK, request.StatusCode, request.Content.ReadAsStringAsync().Result);
            var places = Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, Place>>(request.Content.ReadAsStringAsync().Result).Values.ToArray();
            Assert.IsTrue(places.Length > 1);
            var place1 = places[0];
            var place2 = places[1];
            request = ListDaySlotsByPlace(client, place1.Id);
            Assert.AreEqual(HttpStatusCode.OK, request.StatusCode, request.Content.ReadAsStringAsync().Result);
            var days = Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, Slot1Day>>(request.Content.ReadAsStringAsync().Result).Values.ToArray();
            Assert.IsTrue(days.Length > 0);

            var day1 = days[0];
            var day2 = days[1];
            request = ListHourSlotsByPlaceAndDaySlotId(client, place1.Id, day1.SlotId.ToString());
            Assert.AreEqual(HttpStatusCode.OK, request.StatusCode, request.Content.ReadAsStringAsync().Result);
            var hours = Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, Slot1Hour>>(request.Content.ReadAsStringAsync().Result).Values.ToArray();
            Assert.IsTrue(hours.Length > 1);
            var hour1 = hours[0];

            request = ListHourSlotsByPlaceAndDaySlotId(client, place2.Id, day2.SlotId.ToString());
            Assert.AreEqual(HttpStatusCode.OK, request.StatusCode, request.Content.ReadAsStringAsync().Result);
            hours = Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, Slot1Hour>>(request.Content.ReadAsStringAsync().Result).Values.ToArray();
            Assert.IsTrue(hours.Length > 1);

            var hour2 = hours[1];

            request = ListMinuteSlotsByPlaceAndHourSlotId(client, place1.Id, hour1.SlotId.ToString());
            Assert.AreEqual(HttpStatusCode.OK, request.StatusCode, request.Content.ReadAsStringAsync().Result);
            var minutes = Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, Slot5Min>>(request.Content.ReadAsStringAsync().Result).Values.ToArray();
            Assert.IsTrue(minutes.Length > 0);

            var minute = minutes[0];

            request = ListMinuteSlotsByPlaceAndHourSlotId(client, place2.Id, hour2.SlotId.ToString());
            Assert.AreEqual(HttpStatusCode.OK, request.StatusCode, request.Content.ReadAsStringAsync().Result);
            minutes = Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, Slot5Min>>(request.Content.ReadAsStringAsync().Result).Values.ToArray();
            Assert.IsTrue(minutes.Length > 0);

            var minute2 = minutes[1];

            Visitor visitor = new Visitor()
            {
                Address = "addr",
                Email = "email@scholtz.sk",
                ChosenPlaceId = place1.Id,
                ChosenSlot = minute.SlotId,
                FirstName = "Ľudovít",
                LastName = "Scholtz",
                Insurance = "25",
                PersonType = "idcard",
                Phone = "+421",
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
            Assert.AreEqual("", responsedVisitor.Phone);
            Assert.AreEqual(TestResult.NotTaken, responsedVisitor.Result);

            request = ListDaySlotsByPlace(client, place2.Id);
            Assert.AreEqual(HttpStatusCode.OK, request.StatusCode, request.Content.ReadAsStringAsync().Result);
            var daysDictionary = Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, Slot1Day>>(request.Content.ReadAsStringAsync().Result);
            Assert.IsTrue(daysDictionary.Count > 1);
            Assert.AreEqual(0, daysDictionary[day1.SlotId.ToString()].Registrations);
            Assert.AreEqual(0, daysDictionary[day2.SlotId.ToString()].Registrations);

            request = ListHourSlotsByPlaceAndDaySlotId(client, place2.Id, day2.SlotId.ToString());
            Assert.AreEqual(HttpStatusCode.OK, request.StatusCode, request.Content.ReadAsStringAsync().Result);
            var hoursDictionary = Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, Slot1Hour>>(request.Content.ReadAsStringAsync().Result);
            Assert.IsTrue(hoursDictionary.Count > 0);
            Assert.AreEqual(0, hoursDictionary[hour2.SlotId.ToString()].Registrations);

            request = ListMinuteSlotsByPlaceAndHourSlotId(client, place2.Id, hour2.SlotId.ToString());
            Assert.AreEqual(HttpStatusCode.OK, request.StatusCode, request.Content.ReadAsStringAsync().Result);
            var minutesDict = Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, Slot5Min>>(request.Content.ReadAsStringAsync().Result);
            Assert.IsTrue(minutesDict.Count > 0);
            minute2 = minutesDict[minute2.SlotId.ToString()];
            Assert.AreEqual(0, minute2.Registrations);

            // Test new registration of the same user - validate statistics
            visitor.Phone = "+421 000 000 000";
            visitor.ChosenPlaceId = place2.Id;
            visitor.ChosenSlot = minute2.SlotId;
            request = Register(client, visitor);

            Assert.AreEqual(HttpStatusCode.OK, request.StatusCode, request.Content.ReadAsStringAsync().Result);
            responsedVisitor = Newtonsoft.Json.JsonConvert.DeserializeObject<Visitor>(request.Content.ReadAsStringAsync().Result);
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

            request = ListPlaces(client);
            Assert.AreEqual(HttpStatusCode.OK, request.StatusCode, request.Content.ReadAsStringAsync().Result);
            var placesDict = Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, Place>>(request.Content.ReadAsStringAsync().Result);
            Assert.IsTrue(placesDict.Count > 1);

            Assert.AreEqual(1, placesDict[place2.Id].Registrations);
            Assert.AreEqual(0, placesDict[place1.Id].Registrations);


            request = ListDaySlotsByPlace(client, place2.Id);
            Assert.AreEqual(HttpStatusCode.OK, request.StatusCode, request.Content.ReadAsStringAsync().Result);
            daysDictionary = Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, Slot1Day>>(request.Content.ReadAsStringAsync().Result);
            Assert.IsTrue(daysDictionary.Count > 1);

            Assert.AreEqual(1, daysDictionary[day2.SlotId.ToString()].Registrations);
            Assert.AreEqual(0, daysDictionary[day1.SlotId.ToString()].Registrations);
            Assert.AreEqual(place2.Id, daysDictionary[day2.SlotId.ToString()].PlaceId);

            request = ListHourSlotsByPlaceAndDaySlotId(client, place2.Id, day2.SlotId.ToString());
            Assert.AreEqual(HttpStatusCode.OK, request.StatusCode, request.Content.ReadAsStringAsync().Result);
            hoursDictionary = Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, Slot1Hour>>(request.Content.ReadAsStringAsync().Result);
            Assert.IsTrue(hoursDictionary.Count > 0);
            Assert.AreEqual(1, hoursDictionary[hour2.SlotId.ToString()].Registrations);

            request = ListHourSlotsByPlaceAndDaySlotId(client, place1.Id, day1.SlotId.ToString());
            Assert.AreEqual(HttpStatusCode.OK, request.StatusCode, request.Content.ReadAsStringAsync().Result);
            hoursDictionary = Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, Slot1Hour>>(request.Content.ReadAsStringAsync().Result);
            Assert.IsTrue(hoursDictionary.Count > 0);
            Assert.AreEqual(0, hoursDictionary[hour1.SlotId.ToString()].Registrations);

            request = ListHourSlotsByPlaceAndDaySlotId(client, place2.Id, day2.SlotId.ToString());
            Assert.AreEqual(HttpStatusCode.OK, request.StatusCode, request.Content.ReadAsStringAsync().Result);
            hoursDictionary = Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, Slot1Hour>>(request.Content.ReadAsStringAsync().Result);
            Assert.IsTrue(hoursDictionary.Count > 0);
            Assert.AreEqual(1, hoursDictionary[hour2.SlotId.ToString()].Registrations);

            request = ListMinuteSlotsByPlaceAndHourSlotId(client, place1.Id, hour1.SlotId.ToString());
            Assert.AreEqual(HttpStatusCode.OK, request.StatusCode, request.Content.ReadAsStringAsync().Result);
            minutesDict = Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, Slot5Min>>(request.Content.ReadAsStringAsync().Result);
            Assert.IsTrue(minutesDict.Count > 0);
            minute = minutesDict[minute.SlotId.ToString()];
            Assert.AreEqual(0, minute.Registrations);

            request = ListMinuteSlotsByPlaceAndHourSlotId(client, place2.Id, hour2.SlotId.ToString());
            Assert.AreEqual(HttpStatusCode.OK, request.StatusCode, request.Content.ReadAsStringAsync().Result);
            minutesDict = Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, Slot5Min>>(request.Content.ReadAsStringAsync().Result);
            Assert.IsTrue(minutesDict.Count > 0);
            minute2 = minutesDict[minute2.SlotId.ToString()];
            Assert.AreEqual(1, minute2.Registrations);



        }

        [Test]
        public void VisitorTestSamePlace()
        {
            DropDatabase();

            using var web = new MockWebApp(AppSettings);
            var client = web.CreateClient();
            var users = configuration.GetSection("AdminUsers").Get<CovidMassTesting.Model.Settings.User[]>();
            var admin = users.First(u => u.Name == "Admin");

            var request = AuthenticateUser(client, admin.Email, admin.Password);
            Assert.AreEqual(HttpStatusCode.OK, request.StatusCode, request.Content.ReadAsStringAsync().Result);
            var adminToken = request.Content.ReadAsStringAsync().Result;
            Assert.IsFalse(string.IsNullOrEmpty(adminToken));
            client.DefaultRequestHeaders.Add("Authorization", $"Bearer {adminToken}");
            SetupDebugPlaces(client);

            request = CheckSlotsDay1(client);
            Assert.AreEqual(HttpStatusCode.OK, request.StatusCode, request.Content.ReadAsStringAsync().Result);
            request = CheckSlotsDay2(client);
            Assert.AreEqual(HttpStatusCode.OK, request.StatusCode, request.Content.ReadAsStringAsync().Result);

            client.DefaultRequestHeaders.Clear();

            request = ListPlaces(client);
            Assert.AreEqual(HttpStatusCode.OK, request.StatusCode, request.Content.ReadAsStringAsync().Result);
            var places = Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, Place>>(request.Content.ReadAsStringAsync().Result).Values.ToArray();
            Assert.IsTrue(places.Length > 1);
            var place1 = places[0];

            request = ListDaySlotsByPlace(client, place1.Id);
            Assert.AreEqual(HttpStatusCode.OK, request.StatusCode, request.Content.ReadAsStringAsync().Result);
            var days = Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, Slot1Day>>(request.Content.ReadAsStringAsync().Result).Values.ToArray();
            Assert.IsTrue(days.Length > 0);

            var day1 = days[0];
            var day2 = days[1];
            request = ListHourSlotsByPlaceAndDaySlotId(client, place1.Id, day1.SlotId.ToString());
            Assert.AreEqual(HttpStatusCode.OK, request.StatusCode, request.Content.ReadAsStringAsync().Result);
            var hours = Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, Slot1Hour>>(request.Content.ReadAsStringAsync().Result).Values.ToArray();
            Assert.IsTrue(hours.Length > 1);
            var hour1 = hours[0];

            request = ListHourSlotsByPlaceAndDaySlotId(client, place1.Id, day2.SlotId.ToString());
            Assert.AreEqual(HttpStatusCode.OK, request.StatusCode, request.Content.ReadAsStringAsync().Result);
            hours = Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, Slot1Hour>>(request.Content.ReadAsStringAsync().Result).Values.ToArray();
            Assert.IsTrue(hours.Length > 1);

            var hour2 = hours[1];

            request = ListMinuteSlotsByPlaceAndHourSlotId(client, place1.Id, hour1.SlotId.ToString());
            Assert.AreEqual(HttpStatusCode.OK, request.StatusCode, request.Content.ReadAsStringAsync().Result);
            var minutes = Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, Slot5Min>>(request.Content.ReadAsStringAsync().Result).Values.ToArray();
            Assert.IsTrue(minutes.Length > 0);

            var minute = minutes[0];

            request = ListMinuteSlotsByPlaceAndHourSlotId(client, place1.Id, hour2.SlotId.ToString());
            Assert.AreEqual(HttpStatusCode.OK, request.StatusCode, request.Content.ReadAsStringAsync().Result);
            minutes = Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, Slot5Min>>(request.Content.ReadAsStringAsync().Result).Values.ToArray();
            Assert.IsTrue(minutes.Length > 0);

            var minute2 = minutes[1];

            Visitor visitor = new Visitor()
            {
                Address = "addr",
                Email = "email@scholtz.sk",
                ChosenPlaceId = place1.Id,
                ChosenSlot = minute.SlotId,
                FirstName = "Ľudovít",
                LastName = "Scholtz",
                Insurance = "25",
                PersonType = "idcard",
                Phone = "+421",
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
            Assert.AreEqual("", responsedVisitor.Phone);
            Assert.AreEqual(TestResult.NotTaken, responsedVisitor.Result);

            request = ListDaySlotsByPlace(client, place1.Id);
            Assert.AreEqual(HttpStatusCode.OK, request.StatusCode, request.Content.ReadAsStringAsync().Result);
            var daysDictionary = Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, Slot1Day>>(request.Content.ReadAsStringAsync().Result);
            Assert.IsTrue(daysDictionary.Count > 1);
            Assert.AreEqual(1, daysDictionary[day1.SlotId.ToString()].Registrations);
            Assert.AreEqual(0, daysDictionary[day2.SlotId.ToString()].Registrations);

            request = ListHourSlotsByPlaceAndDaySlotId(client, place1.Id, day2.SlotId.ToString());
            Assert.AreEqual(HttpStatusCode.OK, request.StatusCode, request.Content.ReadAsStringAsync().Result);
            var hoursDictionary = Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, Slot1Hour>>(request.Content.ReadAsStringAsync().Result);
            Assert.IsTrue(hoursDictionary.Count > 0);
            Assert.AreEqual(0, hoursDictionary[hour2.SlotId.ToString()].Registrations);

            request = ListMinuteSlotsByPlaceAndHourSlotId(client, place1.Id, hour2.SlotId.ToString());
            Assert.AreEqual(HttpStatusCode.OK, request.StatusCode, request.Content.ReadAsStringAsync().Result);
            var minutesDict = Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, Slot5Min>>(request.Content.ReadAsStringAsync().Result);
            Assert.IsTrue(minutesDict.Count > 0);
            minute2 = minutesDict[minute2.SlotId.ToString()];
            Assert.AreEqual(0, minute2.Registrations);

            // Test new registration of the same user - validate statistics
            visitor.Phone = "+421 000 000 000";
            visitor.ChosenPlaceId = place1.Id;
            visitor.ChosenSlot = minute2.SlotId;
            request = Register(client, visitor);

            Assert.AreEqual(HttpStatusCode.OK, request.StatusCode, request.Content.ReadAsStringAsync().Result);
            responsedVisitor = Newtonsoft.Json.JsonConvert.DeserializeObject<Visitor>(request.Content.ReadAsStringAsync().Result);
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

            request = ListPlaces(client);
            Assert.AreEqual(HttpStatusCode.OK, request.StatusCode, request.Content.ReadAsStringAsync().Result);
            var placesDict = Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, Place>>(request.Content.ReadAsStringAsync().Result);
            Assert.IsTrue(placesDict.Count > 1);

            Assert.AreEqual(1, placesDict[place1.Id].Registrations);


            request = ListDaySlotsByPlace(client, place1.Id);
            Assert.AreEqual(HttpStatusCode.OK, request.StatusCode, request.Content.ReadAsStringAsync().Result);
            daysDictionary = Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, Slot1Day>>(request.Content.ReadAsStringAsync().Result);
            Assert.IsTrue(daysDictionary.Count > 1);

            Assert.AreEqual(1, daysDictionary[day2.SlotId.ToString()].Registrations);
            Assert.AreEqual(0, daysDictionary[day1.SlotId.ToString()].Registrations);
            Assert.AreEqual(place1.Id, daysDictionary[day2.SlotId.ToString()].PlaceId);

            request = ListHourSlotsByPlaceAndDaySlotId(client, place1.Id, day2.SlotId.ToString());
            Assert.AreEqual(HttpStatusCode.OK, request.StatusCode, request.Content.ReadAsStringAsync().Result);
            hoursDictionary = Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, Slot1Hour>>(request.Content.ReadAsStringAsync().Result);
            Assert.IsTrue(hoursDictionary.Count > 0);
            Assert.AreEqual(1, hoursDictionary[hour2.SlotId.ToString()].Registrations);

            request = ListHourSlotsByPlaceAndDaySlotId(client, place1.Id, day1.SlotId.ToString());
            Assert.AreEqual(HttpStatusCode.OK, request.StatusCode, request.Content.ReadAsStringAsync().Result);
            hoursDictionary = Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, Slot1Hour>>(request.Content.ReadAsStringAsync().Result);
            Assert.IsTrue(hoursDictionary.Count > 0);
            Assert.AreEqual(0, hoursDictionary[hour1.SlotId.ToString()].Registrations);

            request = ListHourSlotsByPlaceAndDaySlotId(client, place1.Id, day2.SlotId.ToString());
            Assert.AreEqual(HttpStatusCode.OK, request.StatusCode, request.Content.ReadAsStringAsync().Result);
            hoursDictionary = Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, Slot1Hour>>(request.Content.ReadAsStringAsync().Result);
            Assert.IsTrue(hoursDictionary.Count > 0);
            Assert.AreEqual(1, hoursDictionary[hour2.SlotId.ToString()].Registrations);

            request = ListMinuteSlotsByPlaceAndHourSlotId(client, place1.Id, hour1.SlotId.ToString());
            Assert.AreEqual(HttpStatusCode.OK, request.StatusCode, request.Content.ReadAsStringAsync().Result);
            minutesDict = Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, Slot5Min>>(request.Content.ReadAsStringAsync().Result);
            Assert.IsTrue(minutesDict.Count > 0);
            minute = minutesDict[minute.SlotId.ToString()];
            Assert.AreEqual(0, minute.Registrations);

            request = ListMinuteSlotsByPlaceAndHourSlotId(client, place1.Id, hour2.SlotId.ToString());
            Assert.AreEqual(HttpStatusCode.OK, request.StatusCode, request.Content.ReadAsStringAsync().Result);
            minutesDict = Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, Slot5Min>>(request.Content.ReadAsStringAsync().Result);
            Assert.IsTrue(minutesDict.Count > 0);
            minute2 = minutesDict[minute2.SlotId.ToString()];
            Assert.AreEqual(1, minute2.Registrations);



        }

        [Test]
        public void RoleRegistrationManagerTest()
        {
            DropDatabase();

            using var web = new MockWebApp(AppSettings);
            var client = web.CreateClient();
            var users = configuration.GetSection("AdminUsers").Get<CovidMassTesting.Model.Settings.User[]>();

            var admin = users.First(u => u.Name == "Admin");
            var request = AuthenticateUser(client, admin.Email, admin.Password);
            Assert.AreEqual(HttpStatusCode.OK, request.StatusCode, request.Content.ReadAsStringAsync().Result);
            var adminToken = request.Content.ReadAsStringAsync().Result;
            Assert.IsFalse(string.IsNullOrEmpty(adminToken));
            client.DefaultRequestHeaders.Add("Authorization", $"Bearer {adminToken}");
            SetupDebugPlaces(client);

            request = CheckSlotsDay1(client);
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
            DropDatabase();

            using var web = new MockWebApp(AppSettings);
            var client = web.CreateClient();
            var users = configuration.GetSection("AdminUsers").Get<CovidMassTesting.Model.Settings.User[]>();

            var admin = users.First(u => u.Name == "Admin");
            var request = AuthenticateUser(client, admin.Email, admin.Password);
            Assert.AreEqual(HttpStatusCode.OK, request.StatusCode, request.Content.ReadAsStringAsync().Result);
            var adminToken = request.Content.ReadAsStringAsync().Result;
            Assert.IsFalse(string.IsNullOrEmpty(adminToken));
            client.DefaultRequestHeaders.Add("Authorization", $"Bearer {adminToken}");
            SetupDebugPlaces(client);

            request = CheckSlotsDay1(client);
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
            var registrationManager = users.First(u => u.Name == "MedicTester");
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


            var medicLab = users.First(u => u.Name == "MedicLab");
            request = AuthenticateUser(client, medicLab.Email, medicLab.Password);
            Assert.AreEqual(HttpStatusCode.OK, request.StatusCode, request.Content.ReadAsStringAsync().Result);
            var medicLabToken = request.Content.ReadAsStringAsync().Result;
            Assert.IsFalse(string.IsNullOrEmpty(registrationManagerToken));
            client.DefaultRequestHeaders.Clear();
            client.DefaultRequestHeaders.Add("Authorization", $"Bearer {medicLabToken}");

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
            DropDatabase();

            using var web = new MockWebApp(AppSettings);
            var client = web.CreateClient();
            var users = configuration.GetSection("AdminUsers").Get<CovidMassTesting.Model.Settings.User[]>();

            var admin = users.First(u => u.Name == "Admin");
            var request = AuthenticateUser(client, admin.Email, admin.Password);
            Assert.AreEqual(HttpStatusCode.OK, request.StatusCode, request.Content.ReadAsStringAsync().Result);
            var adminToken = request.Content.ReadAsStringAsync().Result;
            Assert.IsFalse(string.IsNullOrEmpty(adminToken));
            client.DefaultRequestHeaders.Add("Authorization", $"Bearer {adminToken}");
            SetupDebugPlaces(client);

            request = CheckSlotsDay1(client);
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

            var medicLab = users.First(u => u.Name == "MedicLab");
            request = AuthenticateUser(client, medicLab.Email, medicLab.Password);
            Assert.AreEqual(HttpStatusCode.OK, request.StatusCode, request.Content.ReadAsStringAsync().Result);
            var medicLabToken = request.Content.ReadAsStringAsync().Result;
            Assert.IsFalse(string.IsNullOrEmpty(registrationManagerToken));
            client.DefaultRequestHeaders.Clear();
            client.DefaultRequestHeaders.Add("Authorization", $"Bearer {medicLabToken}");

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
        [Test]
        public void RoleDataExporterTest()
        {
            DropDatabase();

            using var web = new MockWebApp(AppSettings);
            var client = web.CreateClient();
            var users = configuration.GetSection("AdminUsers").Get<CovidMassTesting.Model.Settings.User[]>();

            var admin = users.First(u => u.Name == "Admin");
            var request = AuthenticateUser(client, admin.Email, admin.Password);
            Assert.AreEqual(HttpStatusCode.OK, request.StatusCode, request.Content.ReadAsStringAsync().Result);
            var adminToken = request.Content.ReadAsStringAsync().Result;
            Assert.IsFalse(string.IsNullOrEmpty(adminToken));
            client.DefaultRequestHeaders.Add("Authorization", $"Bearer {adminToken}");
            SetupDebugPlaces(client);

            var dataexporter = users.First(u => u.Name == "DataExporter");
            request = AuthenticateUser(client, dataexporter.Email, dataexporter.Password);
            Assert.AreEqual(HttpStatusCode.OK, request.StatusCode, request.Content.ReadAsStringAsync().Result);
            var dataExporterToken = request.Content.ReadAsStringAsync().Result;

            request = CheckSlotsDay1(client);
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

            var medicLab = users.First(u => u.Name == "MedicLab");
            request = AuthenticateUser(client, medicLab.Email, medicLab.Password);
            Assert.AreEqual(HttpStatusCode.OK, request.StatusCode, request.Content.ReadAsStringAsync().Result);
            var medicLabToken = request.Content.ReadAsStringAsync().Result;
            Assert.IsFalse(string.IsNullOrEmpty(registrationManagerToken));
            client.DefaultRequestHeaders.Clear();
            client.DefaultRequestHeaders.Add("Authorization", $"Bearer {medicLabToken}");

            // TEST mark as sick
            request = SetResult(client, test1, TestResult.PositiveWaitingForCertificate);
            Assert.AreEqual(HttpStatusCode.OK, request.StatusCode, request.Content.ReadAsStringAsync().Result);
            var result = Newtonsoft.Json.JsonConvert.DeserializeObject<Result>(request.Content.ReadAsStringAsync().Result);
            Assert.AreEqual(TestResult.PositiveWaitingForCertificate, result.State);

            // TEST mark as healthy
            request = SetResult(client, test2, TestResult.NegativeWaitingForCertificate);
            Assert.AreEqual(HttpStatusCode.OK, request.StatusCode, request.Content.ReadAsStringAsync().Result);
            result = Newtonsoft.Json.JsonConvert.DeserializeObject<Result>(request.Content.ReadAsStringAsync().Result);
            Assert.AreEqual(TestResult.NegativeWaitingForCertificate, result.State);



            client.DefaultRequestHeaders.Clear();
            client.DefaultRequestHeaders.Add("Authorization", $"Bearer {dataExporterToken}");
            request = FinalDataExport(client, 0, 100);
            var stream = new MemoryStream();
            request.Content.CopyToAsync(stream).Wait();
            var resultExport = Encoding.UTF8.GetString(stream.ToArray());
            Assert.IsNotNull(resultExport);
            Assert.IsTrue(resultExport.Contains("111111111"));
            Assert.IsTrue(resultExport.Contains(registered[0].Id.ToString()));
            Assert.IsTrue(resultExport.Contains(registered[0].Phone));
            Assert.IsTrue(resultExport.Contains(registered[0].RC));
            Assert.IsFalse(resultExport.Contains(registered[1].Id.ToString()));
            Assert.IsFalse(resultExport.Contains(registered[1].RC));

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

            // unauthorized request
            request = FinalDataExport(client, 0, 100);
            Assert.AreEqual(HttpStatusCode.BadRequest, request.StatusCode);

            //authorized data export
            client.DefaultRequestHeaders.Clear();
            client.DefaultRequestHeaders.Add("Authorization", $"Bearer {dataExporterToken}");
            request = FinalDataExport(client, 0, 100);
            stream = new MemoryStream();
            request.Content.CopyToAsync(stream).Wait();
            resultExport = Encoding.UTF8.GetString(stream.ToArray());
            Assert.IsNotNull(resultExport);
            Assert.IsTrue(resultExport.Contains("111111111"));
            Assert.IsTrue(resultExport.Contains(registered[0].Id.ToString()));
            Assert.IsTrue(resultExport.Contains(registered[0].Phone));
            Assert.IsTrue(resultExport.Contains(registered[0].RC));
            Assert.IsFalse(resultExport.Contains(registered[1].Id.ToString()));
            Assert.IsFalse(resultExport.Contains(registered[1].RC));
            //Assert.AreEqual(1, resultExport.Count);
        }

        [Test]
        public void PlaceProviderTests()
        {
            DropDatabase();

            using var web = new MockWebApp(AppSettings);
            var client = web.CreateClient();

            var request = PlaceProviderListPublic(client);
            Assert.AreEqual(HttpStatusCode.OK, request.StatusCode, request.Content.ReadAsStringAsync().Result);
            var data = request.Content.ReadAsStringAsync().Result;
            var result = Newtonsoft.Json.JsonConvert.DeserializeObject<List<PlaceProvider>>(data);
            Assert.AreEqual(0, result.Count);
            var users = configuration.GetSection("AdminUsers").Get<CovidMassTesting.Model.Settings.User[]>();
            var admin = users.First(u => u.Name == "Admin");

            var obj = new PlaceProvider()
            {
                VAT = "123",
                Web = "123",
                CompanyId = "123",
                CompanyName = "123",
                Country = "SK",
                MainEmail = admin.Email,
                PrivatePhone = "+421 907 000000",
                MainContact = "Admin Person"
            };

            request = PlaceProviderRegistration(client, obj);
            Assert.AreEqual(HttpStatusCode.OK, request.StatusCode, request.Content.ReadAsStringAsync().Result);

            request = PlaceProviderListPublic(client);
            Assert.AreEqual(HttpStatusCode.OK, request.StatusCode, request.Content.ReadAsStringAsync().Result);
            data = request.Content.ReadAsStringAsync().Result;
            result = Newtonsoft.Json.JsonConvert.DeserializeObject<List<PlaceProvider>>(data);
            Assert.AreEqual(1, result.Count);


            request = AuthenticateUser(client, admin.Email, admin.Password);
            Assert.AreEqual(HttpStatusCode.OK, request.StatusCode, request.Content.ReadAsStringAsync().Result);
            var adminToken = request.Content.ReadAsStringAsync().Result;
            Assert.IsFalse(string.IsNullOrEmpty(adminToken));
            client.DefaultRequestHeaders.Add("Authorization", $"Bearer {adminToken}");

            request = SetPlaceProvider(client, "123");
            Assert.AreEqual(HttpStatusCode.OK, request.StatusCode, request.Content.ReadAsStringAsync().Result);
            adminToken = request.Content.ReadAsStringAsync().Result;

        }

        [Test]
        public void PlaceProviderRegistrationTests()
        {
            DropDatabase();

            using var web = new MockWebApp(AppSettings);
            var client = web.CreateClient();

            var request = PlaceProviderListPublic(client);
            Assert.AreEqual(HttpStatusCode.OK, request.StatusCode, request.Content.ReadAsStringAsync().Result);
            var data = request.Content.ReadAsStringAsync().Result;
            var result = Newtonsoft.Json.JsonConvert.DeserializeObject<List<PlaceProvider>>(data);
            Assert.AreEqual(0, result.Count);
            var email = "place.provider@scholtz.sk";

            var obj = new PlaceProvider()
            {
                VAT = "123",
                Web = "123",
                CompanyId = "123",
                CompanyName = "123",
                Country = "SK",
                MainEmail = email,
                PrivatePhone = "+421 907 000000",
                MainContact = "Admin Person"
            };

            request = PlaceProviderRegistration(client, obj);
            Assert.AreEqual(HttpStatusCode.OK, request.StatusCode, request.Content.ReadAsStringAsync().Result);

            request = PlaceProviderListPublic(client);
            Assert.AreEqual(HttpStatusCode.OK, request.StatusCode, request.Content.ReadAsStringAsync().Result);
            data = request.Content.ReadAsStringAsync().Result;
            result = Newtonsoft.Json.JsonConvert.DeserializeObject<List<PlaceProvider>>(data);
            Assert.AreEqual(1, result.Count);

            var emailSender = web.Server.Services.GetService<CovidMassTesting.Controllers.Email.IEmailSender>();
            var noEmailSender = emailSender as CovidMassTesting.Controllers.Email.NoEmailSender;
            Assert.AreEqual(1, noEmailSender.Data.Count);
            var emailData = noEmailSender.Data.First().Value.data as CovidMassTesting.Model.Email.InvitationEmail;
            request = AuthenticateUser(client, email, emailData.Password);
            Assert.AreEqual(HttpStatusCode.OK, request.StatusCode, request.Content.ReadAsStringAsync().Result);
            var adminToken = request.Content.ReadAsStringAsync().Result;
            Assert.IsFalse(string.IsNullOrEmpty(adminToken));
            client.DefaultRequestHeaders.Add("Authorization", $"Bearer {adminToken}");

            request = SetPlaceProvider(client, "123");
            Assert.AreEqual(HttpStatusCode.OK, request.StatusCode, request.Content.ReadAsStringAsync().Result);
            adminToken = request.Content.ReadAsStringAsync().Result;

        }

        [Test]
        public void PlaceProviderRegisterPlace()
        {
            DropDatabase();

            using var web = new MockWebApp(AppSettings);
            var client = web.CreateClient();

            var request = PlaceProviderListPublic(client);
            Assert.AreEqual(HttpStatusCode.OK, request.StatusCode, request.Content.ReadAsStringAsync().Result);
            var data = request.Content.ReadAsStringAsync().Result;
            var result = Newtonsoft.Json.JsonConvert.DeserializeObject<List<PlaceProvider>>(data);
            Assert.AreEqual(0, result.Count);
            var email = "place.provider@scholtz.sk";

            var obj = new PlaceProvider()
            {
                VAT = "123",
                Web = "123",
                CompanyId = "123",
                CompanyName = "123",
                Country = "SK",
                MainEmail = email,
                PrivatePhone = "+421 907 000000",
                MainContact = "Admin Person"
            };

            request = PlaceProviderRegistration(client, obj);
            Assert.AreEqual(HttpStatusCode.OK, request.StatusCode, request.Content.ReadAsStringAsync().Result);

            request = PlaceProviderListPublic(client);
            Assert.AreEqual(HttpStatusCode.OK, request.StatusCode, request.Content.ReadAsStringAsync().Result);
            data = request.Content.ReadAsStringAsync().Result;
            result = Newtonsoft.Json.JsonConvert.DeserializeObject<List<PlaceProvider>>(data);
            Assert.AreEqual(1, result.Count);

            var emailSender = web.Server.Services.GetService<CovidMassTesting.Controllers.Email.IEmailSender>();
            var noEmailSender = emailSender as CovidMassTesting.Controllers.Email.NoEmailSender;
            Assert.AreEqual(1, noEmailSender.Data.Count);
            var emailData = noEmailSender.Data.First().Value.data as CovidMassTesting.Model.Email.InvitationEmail;
            request = AuthenticateUser(client, email, emailData.Password);
            Assert.AreEqual(HttpStatusCode.OK, request.StatusCode, request.Content.ReadAsStringAsync().Result);
            var adminToken = request.Content.ReadAsStringAsync().Result;
            Assert.IsFalse(string.IsNullOrEmpty(adminToken));
            client.DefaultRequestHeaders.Add("Authorization", $"Bearer {adminToken}");

            var places = SetupDebugPlaces(client);
            var place = places.First();
            var second = places.Skip(1).First();
            var actions = new TimeUpdate[]
            {
                new TimeUpdate()
                {
                    Date = DateTimeOffset.Now,
                    OpeningHoursTemplateId= 1,
                    PlaceId = "__ALL__",
                    Type = "set"
                },

                new TimeUpdate()
                {
                    Date = DateTimeOffset.Now.AddDays(1),
                    OpeningHoursTemplateId= 2,
                    PlaceId = place.Id,
                    Type = "set"
                },

                new TimeUpdate()
                {
                    Date = DateTimeOffset.Now,
                    PlaceId = place.Id,
                    Type = "delete"
                }
            };

            request = ListScheduledDays(client);
            Assert.AreEqual(HttpStatusCode.OK, request.StatusCode, request.Content.ReadAsStringAsync().Result);
            var daysData = Newtonsoft.Json.JsonConvert.DeserializeObject<List<DayTimeManagement>>(request.Content.ReadAsStringAsync().Result);
            Assert.AreEqual(0, daysData.Count);

            request = ScheduleOpenningHours(client, actions);

            Assert.AreEqual(HttpStatusCode.OK, request.StatusCode, request.Content.ReadAsStringAsync().Result);

            request = ListDaySlotsByPlace(client, place.Id);
            Assert.AreEqual(HttpStatusCode.OK, request.StatusCode, request.Content.ReadAsStringAsync().Result);

            var daysDictionary = Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, Slot1Day>>(request.Content.ReadAsStringAsync().Result);
            Assert.AreEqual(1, daysDictionary.Count);
            var daySlot = daysDictionary.Values.First();
            Assert.AreEqual("11:45-11:50", daySlot.OpeningHours);
            Assert.AreEqual(2, daySlot.OpeningHoursTemplate);
            request = ListHourSlotsByPlaceAndDaySlotId(client, place.Id, daySlot.SlotId.ToString());
            Assert.AreEqual(HttpStatusCode.OK, request.StatusCode, request.Content.ReadAsStringAsync().Result);

            var hoursDictionary = Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, Slot1Hour>>(request.Content.ReadAsStringAsync().Result);
            Assert.AreEqual(1, hoursDictionary.Count);
            var hourSlot = hoursDictionary.Values.First();

            request = ListMinuteSlotsByPlaceAndHourSlotId(client, place.Id, hourSlot.SlotId.ToString());
            Assert.AreEqual(HttpStatusCode.OK, request.StatusCode, request.Content.ReadAsStringAsync().Result);

            var minutesDictionary = Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, Slot5Min>>(request.Content.ReadAsStringAsync().Result);
            Assert.AreEqual(1, minutesDictionary.Count);



            request = ListDaySlotsByPlace(client, second.Id);
            Assert.AreEqual(HttpStatusCode.OK, request.StatusCode, request.Content.ReadAsStringAsync().Result);

            daysDictionary = Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, Slot1Day>>(request.Content.ReadAsStringAsync().Result);
            Assert.AreEqual(1, daysDictionary.Count);
            daySlot = daysDictionary.Values.First();
            Assert.AreEqual("08:30-22:00", daySlot.OpeningHours);
            Assert.AreEqual(1, daySlot.OpeningHoursTemplate);
            request = ListHourSlotsByPlaceAndDaySlotId(client, second.Id, daySlot.SlotId.ToString());
            Assert.AreEqual(HttpStatusCode.OK, request.StatusCode, request.Content.ReadAsStringAsync().Result);

            hoursDictionary = Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, Slot1Hour>>(request.Content.ReadAsStringAsync().Result);
            Assert.AreEqual(14, hoursDictionary.Count);
            hourSlot = hoursDictionary.Values.First();

            request = ListMinuteSlotsByPlaceAndHourSlotId(client, second.Id, hourSlot.SlotId.ToString());
            Assert.AreEqual(HttpStatusCode.OK, request.StatusCode, request.Content.ReadAsStringAsync().Result);

            minutesDictionary = Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, Slot5Min>>(request.Content.ReadAsStringAsync().Result);
            Assert.AreEqual(6, minutesDictionary.Count);

            request = ListScheduledDays(client);
            Assert.AreEqual(HttpStatusCode.OK, request.StatusCode, request.Content.ReadAsStringAsync().Result);
            daysData = Newtonsoft.Json.JsonConvert.DeserializeObject<List<DayTimeManagement>>(request.Content.ReadAsStringAsync().Result);
            Assert.AreEqual(2, daysData.Count);

            var keys = daysData.Select(d => d.SlotId).OrderBy(d => d).ToArray();
            var day1 = daysData.First(d => d.SlotId == keys[0]);
            Assert.IsTrue(day1.OpeningHours.Contains("08:30-22:00"));
            Assert.IsTrue(day1.OpeningHours.Contains("08:00-09:00"));
            Assert.IsTrue(day1.OpeningHoursTemplates.Contains(1));
            var day2 = daysData.First(d => d.SlotId == keys[1]);
            Assert.IsTrue(day2.OpeningHours.Contains("11:45-11:50"));
            Assert.IsTrue(day2.OpeningHoursTemplates.Contains(2));
        }

        [Test]
        public void PlaceProviderHumanResourcesManagement()
        {
            DropDatabase();

            using var web = new MockWebApp(AppSettings);
            var client = web.CreateClient();

            var request = PlaceProviderListPublic(client);
            Assert.AreEqual(HttpStatusCode.OK, request.StatusCode, request.Content.ReadAsStringAsync().Result);
            var data = request.Content.ReadAsStringAsync().Result;
            var result = Newtonsoft.Json.JsonConvert.DeserializeObject<List<PlaceProvider>>(data);
            Assert.AreEqual(0, result.Count);
            var email = "place.provider@scholtz.sk";

            var obj = new PlaceProvider()
            {
                VAT = "123",
                Web = "123",
                CompanyId = "123",
                CompanyName = "123, s.r.o.",
                Country = "SK",
                MainEmail = email,
                PrivatePhone = "+421 907 000000",
                MainContact = "Admin Person"

            };

            request = PlaceProviderRegistration(client, obj);
            Assert.AreEqual(HttpStatusCode.OK, request.StatusCode, request.Content.ReadAsStringAsync().Result);

            request = PlaceProviderListPublic(client);
            Assert.AreEqual(HttpStatusCode.OK, request.StatusCode, request.Content.ReadAsStringAsync().Result);
            data = request.Content.ReadAsStringAsync().Result;
            result = Newtonsoft.Json.JsonConvert.DeserializeObject<List<PlaceProvider>>(data);
            Assert.AreEqual(1, result.Count);

            var emailSender = web.Server.Services.GetService<CovidMassTesting.Controllers.Email.IEmailSender>();
            var noEmailSender = emailSender as CovidMassTesting.Controllers.Email.NoEmailSender;
            Assert.AreEqual(1, noEmailSender.Data.Count);
            var emailData = noEmailSender.Data.First().Value.data as CovidMassTesting.Model.Email.InvitationEmail;
            noEmailSender.Data.Clear();
            request = AuthenticateUser(client, email, emailData.Password);
            Assert.AreEqual(HttpStatusCode.OK, request.StatusCode, request.Content.ReadAsStringAsync().Result);
            var adminToken = request.Content.ReadAsStringAsync().Result;
            Assert.IsFalse(string.IsNullOrEmpty(adminToken));

            var handler = new JwtSecurityTokenHandler();
            var tokenS = handler.ReadToken(adminToken) as JwtSecurityToken;
            var jti = tokenS.Claims.FirstOrDefault(claim => claim.Type == "Role" && claim.Value == "PPAdmin");
            Assert.IsNotNull(jti);

            client.DefaultRequestHeaders.Add("Authorization", $"Bearer {adminToken}");

            var places = SetupDebugPlaces(client);
            var firstPlace = places.First();
            var secondPlace = places.Skip(1).First();

            var medicPersonEmail = "person1@scholtz.sk";
            request = InviteUserToPP(client, medicPersonEmail, "Person 1", "+421 907 000 000", "MyMessage");
            Assert.AreEqual(HttpStatusCode.OK, request.StatusCode, request.Content.ReadAsStringAsync().Result);
            Assert.AreEqual(1, noEmailSender.Data.Count);


            request = ListPPInvites(client);
            Assert.AreEqual(HttpStatusCode.OK, request.StatusCode, request.Content.ReadAsStringAsync().Result);
            var invites = JsonConvert.DeserializeObject<List<Invitation>>(request.Content.ReadAsStringAsync().Result);
            Assert.AreEqual(2, invites.Count);

            emailData = noEmailSender.Data.First().Value.data as CovidMassTesting.Model.Email.InvitationEmail;
            noEmailSender.Data.Clear();
            request = AuthenticateUser(client, medicPersonEmail, emailData.Password);
            Assert.AreEqual(HttpStatusCode.OK, request.StatusCode, request.Content.ReadAsStringAsync().Result);
            var medicPersonToken = request.Content.ReadAsStringAsync().Result;
            Assert.IsFalse(string.IsNullOrEmpty(medicPersonToken));

            client.DefaultRequestHeaders.Clear();
            client.DefaultRequestHeaders.Add("Authorization", $"Bearer {medicPersonToken}");


            request = ListUserInvites(client);
            Assert.AreEqual(HttpStatusCode.OK, request.StatusCode, request.Content.ReadAsStringAsync().Result);
            invites = JsonConvert.DeserializeObject<List<Invitation>>(request.Content.ReadAsStringAsync().Result);
            Assert.AreEqual(1, invites.Count);
            Assert.AreEqual("MyMessage", invites[0].InvitationMessage);
            Assert.AreEqual("123, s.r.o.", invites[0].CompanyName);

            request = ListPPInvites(client);
            Assert.AreEqual(HttpStatusCode.BadRequest, request.StatusCode, request.Content.ReadAsStringAsync().Result);

            request = ProcessInvitation(client, invites.First().InvitationId, true);
            Assert.AreEqual(HttpStatusCode.OK, request.StatusCode, request.Content.ReadAsStringAsync().Result);

            client.DefaultRequestHeaders.Clear();
            client.DefaultRequestHeaders.Add("Authorization", $"Bearer {adminToken}");

            var allocations = new PersonAllocation[]
            {
                new PersonAllocation()
                {
                    Start = DateTimeOffset.Now,
                    End = DateTimeOffset.Now.AddDays(1),
                    Role = Groups.MedicLab,
                    User = medicPersonEmail
                }
            };
            request = AllocatePersonsToPlace(client, allocations, firstPlace.Id);
            Assert.AreEqual(HttpStatusCode.OK, request.StatusCode, request.Content.ReadAsStringAsync().Result);

            request = ListPlaceAllocations(client, firstPlace.Id);
            Assert.AreEqual(HttpStatusCode.OK, request.StatusCode, request.Content.ReadAsStringAsync().Result);

            var allocationsParsed = JsonConvert.DeserializeObject<List<PersonAllocation>>(request.Content.ReadAsStringAsync().Result);
            Assert.AreEqual(1, allocationsParsed.Count);


            request = AuthenticateUser(client, medicPersonEmail, emailData.Password);
            Assert.AreEqual(HttpStatusCode.OK, request.StatusCode, request.Content.ReadAsStringAsync().Result);
            medicPersonToken = request.Content.ReadAsStringAsync().Result;
            Assert.IsFalse(string.IsNullOrEmpty(medicPersonToken));


            handler = new JwtSecurityTokenHandler();
            tokenS = handler.ReadToken(medicPersonToken) as JwtSecurityToken;
            jti = tokenS.Claims.FirstOrDefault(claim => claim.Type == "Role" && claim.Value == Groups.PPAdmin);
            Assert.IsNull(jti);
            jti = tokenS.Claims.FirstOrDefault(claim => claim.Type == "Role" && claim.Value == Groups.MedicLab);
            Assert.IsNotNull(jti);

            request = RemoveAllocationAtPlace(client, allocationsParsed[0].Id, firstPlace.Id);
            Assert.AreEqual(HttpStatusCode.OK, request.StatusCode, request.Content.ReadAsStringAsync().Result);

            request = ListPlaceAllocations(client, firstPlace.Id);
            Assert.AreEqual(HttpStatusCode.OK, request.StatusCode, request.Content.ReadAsStringAsync().Result);

            allocationsParsed = JsonConvert.DeserializeObject<List<PersonAllocation>>(request.Content.ReadAsStringAsync().Result);
            Assert.AreEqual(0, allocationsParsed.Count);



        }


        [Test]
        public void RoleMedicTesterPPTest()
        {
            DropDatabase();

            using var web = new MockWebApp(AppSettings);
            var client = web.CreateClient();


            var request = PlaceProviderListPublic(client);
            Assert.AreEqual(HttpStatusCode.OK, request.StatusCode, request.Content.ReadAsStringAsync().Result);
            var data = request.Content.ReadAsStringAsync().Result;
            var result = Newtonsoft.Json.JsonConvert.DeserializeObject<List<PlaceProvider>>(data);
            Assert.AreEqual(0, result.Count);
            var email = "place.provider@scholtz.sk";

            var obj = new PlaceProvider()
            {
                VAT = "123",
                Web = "123",
                CompanyId = "123",
                CompanyName = "123, s.r.o.",
                Country = "SK",
                MainEmail = email,
                PrivatePhone = "+421 907 000000",
                MainContact = "Admin Person"

            };

            request = PlaceProviderRegistration(client, obj);
            Assert.AreEqual(HttpStatusCode.OK, request.StatusCode, request.Content.ReadAsStringAsync().Result);
            data = request.Content.ReadAsStringAsync().Result;

            var pp = JsonConvert.DeserializeObject<PlaceProvider>(data);
            Assert.AreEqual(obj.VAT, pp.VAT);
            Assert.AreEqual(obj.Web, pp.Web);
            Assert.AreEqual(obj.CompanyId, pp.CompanyId);
            Assert.AreEqual(obj.CompanyName, pp.CompanyName);
            Assert.AreEqual(obj.Country, pp.Country);
            Assert.AreEqual(obj.MainEmail, pp.MainEmail);
            Assert.AreEqual("+421907000000", pp.PrivatePhone);
            Assert.AreEqual(obj.MainContact, pp.MainContact);

            request = PlaceProviderListPublic(client);
            Assert.AreEqual(HttpStatusCode.OK, request.StatusCode, request.Content.ReadAsStringAsync().Result);
            data = request.Content.ReadAsStringAsync().Result;
            result = Newtonsoft.Json.JsonConvert.DeserializeObject<List<PlaceProvider>>(data);
            Assert.AreEqual(1, result.Count);

            var emailSender = web.Server.Services.GetService<CovidMassTesting.Controllers.Email.IEmailSender>();
            var noEmailSender = emailSender as CovidMassTesting.Controllers.Email.NoEmailSender;
            Assert.AreEqual(1, noEmailSender.Data.Count);
            var emailData = noEmailSender.Data.First().Value.data as CovidMassTesting.Model.Email.InvitationEmail;
            noEmailSender.Data.Clear();
            request = AuthenticateUser(client, email, emailData.Password);
            Assert.AreEqual(HttpStatusCode.OK, request.StatusCode, request.Content.ReadAsStringAsync().Result);
            var adminToken = request.Content.ReadAsStringAsync().Result;
            Assert.IsFalse(string.IsNullOrEmpty(adminToken));

            var handler = new JwtSecurityTokenHandler();
            var tokenS = handler.ReadToken(adminToken) as JwtSecurityToken;
            var jti = tokenS.Claims.FirstOrDefault(claim => claim.Type == "Role" && claim.Value == "PPAdmin");
            Assert.IsNotNull(jti);

            client.DefaultRequestHeaders.Add("Authorization", $"Bearer {adminToken}");

            // setup places
            var debugPlaces = SetupDebugPlaces(client);
            var firstPlace = debugPlaces.First();
            var secondPlace = debugPlaces.Skip(1).First();

            // invite tester
            var medicPersonEmail = "person1tester@scholtz.sk";
            request = InviteUserToPP(client, medicPersonEmail, "Person 1", "+421 907 000 000", "MyMessage");
            Assert.AreEqual(HttpStatusCode.OK, request.StatusCode, request.Content.ReadAsStringAsync().Result);
            Assert.AreEqual(1, noEmailSender.Data.Count);

            request = ListPPInvites(client);
            Assert.AreEqual(HttpStatusCode.OK, request.StatusCode, request.Content.ReadAsStringAsync().Result);
            var invites = JsonConvert.DeserializeObject<List<Invitation>>(request.Content.ReadAsStringAsync().Result);
            Assert.AreEqual(2, invites.Count);

            emailData = noEmailSender.Data.First().Value.data as CovidMassTesting.Model.Email.InvitationEmail;
            noEmailSender.Data.Clear();
            var medicPersonPass = emailData.Password;
            request = AuthenticateUser(client, medicPersonEmail, medicPersonPass);
            Assert.AreEqual(HttpStatusCode.OK, request.StatusCode, request.Content.ReadAsStringAsync().Result);
            var medicPersonToken = request.Content.ReadAsStringAsync().Result;
            Assert.IsFalse(string.IsNullOrEmpty(medicPersonToken));


            // invite lab user
            var medicLabPersonEmail = "person1lab@scholtz.sk";
            request = InviteUserToPP(client, medicLabPersonEmail, "Person 2", "+421 907 000 000", "MyMessage");
            Assert.AreEqual(HttpStatusCode.OK, request.StatusCode, request.Content.ReadAsStringAsync().Result);
            Assert.AreEqual(1, noEmailSender.Data.Count);

            request = ListPPInvites(client);
            Assert.AreEqual(HttpStatusCode.OK, request.StatusCode, request.Content.ReadAsStringAsync().Result);
            invites = JsonConvert.DeserializeObject<List<Invitation>>(request.Content.ReadAsStringAsync().Result);
            Assert.AreEqual(3, invites.Count);

            emailData = noEmailSender.Data.First().Value.data as CovidMassTesting.Model.Email.InvitationEmail;
            noEmailSender.Data.Clear();
            request = AuthenticateUser(client, medicLabPersonEmail, emailData.Password);
            Assert.AreEqual(HttpStatusCode.OK, request.StatusCode, request.Content.ReadAsStringAsync().Result);
            var medicLabPersonToken = request.Content.ReadAsStringAsync().Result;
            Assert.IsFalse(string.IsNullOrEmpty(medicLabPersonToken));


            // open testing for public

            var actions = new TimeUpdate[]
            {
                new TimeUpdate()
                {
                    Date = DateTimeOffset.Now,
                    OpeningHoursTemplateId= 1,
                    PlaceId = "__ALL__",
                    Type = "set"
                },

                new TimeUpdate()
                {
                    Date = DateTimeOffset.Now.AddDays(1),
                    OpeningHoursTemplateId= 2,
                    PlaceId = firstPlace.Id,
                    Type = "set"
                },

                new TimeUpdate()
                {
                    Date = DateTimeOffset.Now,
                    PlaceId = firstPlace.Id,
                    Type = "delete"
                }
            };

            request = ListScheduledDays(client);
            Assert.AreEqual(HttpStatusCode.OK, request.StatusCode, request.Content.ReadAsStringAsync().Result);
            var daysData = Newtonsoft.Json.JsonConvert.DeserializeObject<List<DayTimeManagement>>(request.Content.ReadAsStringAsync().Result);
            Assert.AreEqual(0, daysData.Count);

            request = ScheduleOpenningHours(client, actions);


            // accept invitations

            client.DefaultRequestHeaders.Clear();
            client.DefaultRequestHeaders.Add("Authorization", $"Bearer {medicPersonToken}");

            request = ListUserInvites(client);
            Assert.AreEqual(HttpStatusCode.OK, request.StatusCode, request.Content.ReadAsStringAsync().Result);
            invites = JsonConvert.DeserializeObject<List<Invitation>>(request.Content.ReadAsStringAsync().Result);
            Assert.AreEqual(1, invites.Count);
            request = ProcessInvitation(client, invites[0].InvitationId, true);
            Assert.AreEqual(HttpStatusCode.OK, request.StatusCode, request.Content.ReadAsStringAsync().Result);


            client.DefaultRequestHeaders.Clear();
            client.DefaultRequestHeaders.Add("Authorization", $"Bearer {medicLabPersonToken}");

            request = ListUserInvites(client);
            Assert.AreEqual(HttpStatusCode.OK, request.StatusCode, request.Content.ReadAsStringAsync().Result);
            invites = JsonConvert.DeserializeObject<List<Invitation>>(request.Content.ReadAsStringAsync().Result);
            Assert.AreEqual(1, invites.Count);
            request = ProcessInvitation(client, invites[0].InvitationId, true);
            Assert.AreEqual(HttpStatusCode.OK, request.StatusCode, request.Content.ReadAsStringAsync().Result);


            client.DefaultRequestHeaders.Clear();
            // check slots
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


            client.DefaultRequestHeaders.Clear();
            client.DefaultRequestHeaders.Add("Authorization", $"Bearer {medicPersonToken}");
            // test authentication when user has not yet been assigned to the place
            var user1 = registered[0];
            request = GetVisitor(client, user1.Id.ToString());
            Assert.AreEqual(HttpStatusCode.BadRequest, request.StatusCode, request.Content.ReadAsStringAsync().Result);
            request = GetVisitorByRC(client, user1.RC);
            Assert.AreEqual(HttpStatusCode.BadRequest, request.StatusCode, request.Content.ReadAsStringAsync().Result);

            client.DefaultRequestHeaders.Clear();
            client.DefaultRequestHeaders.Add("Authorization", $"Bearer {adminToken}");

            // assign person to pp

            var allocations = new PersonAllocation[]
            {
                new PersonAllocation()
                {
                    Start = DateTimeOffset.Now,
                    End = DateTimeOffset.Now.AddDays(1),
                    Role = Groups.MedicTester,
                    User = medicPersonEmail
                },
                new PersonAllocation()
                {
                    Start = DateTimeOffset.Now,
                    End = DateTimeOffset.Now.AddDays(1),
                    Role = Groups.MedicLab,
                    User = medicLabPersonEmail
                }
            };
            request = AllocatePersonsToPlace(client, allocations, firstPlace.Id);
            Assert.AreEqual(HttpStatusCode.OK, request.StatusCode, request.Content.ReadAsStringAsync().Result);

            // login again, check tokens

            client.DefaultRequestHeaders.Clear();
            client.DefaultRequestHeaders.Add("Authorization", $"Bearer {medicPersonToken}");
            request = SetPlaceProvider(client, pp.PlaceProviderId);
            Assert.AreEqual(HttpStatusCode.OK, request.StatusCode, request.Content.ReadAsStringAsync().Result);
            medicPersonToken = request.Content.ReadAsStringAsync().Result;

            handler = new JwtSecurityTokenHandler();
            tokenS = handler.ReadToken(medicPersonToken) as JwtSecurityToken;
            jti = tokenS.Claims.FirstOrDefault(claim => claim.Type == "Role" && claim.Value == Groups.MedicTester);
            Assert.IsNotNull(jti);
            jti = tokenS.Claims.FirstOrDefault(claim => claim.Type == Token.Claims.PlaceProvider);
            Assert.IsNotNull(jti);
            Assert.AreEqual(pp.PlaceProviderId, jti.Value);

            client.DefaultRequestHeaders.Clear();
            client.DefaultRequestHeaders.Add("Authorization", $"Bearer {medicLabPersonToken}");
            request = SetPlaceProvider(client, pp.PlaceProviderId);
            Assert.AreEqual(HttpStatusCode.OK, request.StatusCode, request.Content.ReadAsStringAsync().Result);
            medicLabPersonToken = request.Content.ReadAsStringAsync().Result;

            handler = new JwtSecurityTokenHandler();
            tokenS = handler.ReadToken(medicLabPersonToken) as JwtSecurityToken;
            jti = tokenS.Claims.FirstOrDefault(claim => claim.Type == "Role" && claim.Value == Groups.MedicLab);
            Assert.IsNotNull(jti);
            jti = tokenS.Claims.FirstOrDefault(claim => claim.Type == Token.Claims.PlaceProvider);
            Assert.IsNotNull(jti);
            Assert.AreEqual(pp.PlaceProviderId, jti.Value);

            // perform fetch data by personal number, by reg code, assign the test set

            client.DefaultRequestHeaders.Clear();
            client.DefaultRequestHeaders.Add("Authorization", $"Bearer {medicPersonToken}");
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

            string test1 = "111-111-111";
            request = ConnectVisitorToTest(client, registered[0].Id.ToString(), test1);
            Assert.AreEqual(HttpStatusCode.OK, request.StatusCode, request.Content.ReadAsStringAsync().Result);

            string test2 = "222-222-222";
            request = ConnectVisitorToTest(client, registered[1].Id.ToString(), test2);
            Assert.AreEqual(HttpStatusCode.OK, request.StatusCode, request.Content.ReadAsStringAsync().Result);

            client.DefaultRequestHeaders.Clear();
            client.DefaultRequestHeaders.Add("Authorization", $"Bearer {medicLabPersonToken}");


            // TEST mark as sick
            request = SetResult(client, test1, TestResult.PositiveWaitingForCertificate);
            Assert.AreEqual(HttpStatusCode.OK, request.StatusCode, request.Content.ReadAsStringAsync().Result);
            var testResult = Newtonsoft.Json.JsonConvert.DeserializeObject<Result>(request.Content.ReadAsStringAsync().Result);
            Assert.AreEqual(TestResult.PositiveWaitingForCertificate, testResult.State);

            // TEST mark as wrong code
            request = SetResult(client, test1, TestResult.PositiveCertificateTaken);
            Assert.AreEqual(HttpStatusCode.BadRequest, request.StatusCode);

            // TEST mark as sick
            request = SetResult(client, test2, TestResult.NegativeWaitingForCertificate);
            Assert.AreEqual(HttpStatusCode.OK, request.StatusCode, request.Content.ReadAsStringAsync().Result);
            testResult = Newtonsoft.Json.JsonConvert.DeserializeObject<Result>(request.Content.ReadAsStringAsync().Result);
            Assert.AreEqual(TestResult.NegativeWaitingForCertificate, testResult.State);
            client.DefaultRequestHeaders.Clear();

            request = PublicGetTestResult(client, registered[0].Id.ToString(), registered[0].RC.Substring(6, 4));
            Assert.AreEqual(HttpStatusCode.OK, request.StatusCode, request.Content.ReadAsStringAsync().Result);
            testResult = Newtonsoft.Json.JsonConvert.DeserializeObject<Result>(request.Content.ReadAsStringAsync().Result);
            Assert.AreEqual(TestResult.PositiveWaitingForCertificate, testResult.State);

            request = PublicGetTestResult(client, registered[1].Id.ToString(), registered[1].RC.Substring(6, 4));
            Assert.AreEqual(HttpStatusCode.OK, request.StatusCode, request.Content.ReadAsStringAsync().Result);
            testResult = Newtonsoft.Json.JsonConvert.DeserializeObject<Result>(request.Content.ReadAsStringAsync().Result);
            Assert.AreEqual(TestResult.NegativeWaitingForCertificate, testResult.State);
        }

        [Test]
        public virtual void TestVersion()
        {
            DropDatabase();

            using var web = new MockWebApp(AppSettings);
            var client = web.CreateClient();
            var request = CheckVersion(client);
            Assert.AreEqual(HttpStatusCode.OK, request.StatusCode, request.Content.ReadAsStringAsync().Result);

            var version = Newtonsoft.Json.JsonConvert.DeserializeObject<CovidMassTesting.Model.Version>(request.Content.ReadAsStringAsync().Result);
            Assert.AreEqual(false, version.SMSConfigured);
            Assert.AreEqual(false, version.EmailConfigured);
            Assert.AreEqual(false, version.RedisConfigured);
        }
        public class MockWebApp : WebApplicationFactory<CovidMassTesting.Startup>
        {
            private readonly string appSettings;
            public MockWebApp(string appSettings)
            {
                this.appSettings = appSettings;
            }
            protected override void ConfigureWebHost(IWebHostBuilder builder)
            {

                builder.ConfigureTestServices(ConfigureServices);
                builder.ConfigureLogging((WebHostBuilderContext context, ILoggingBuilder loggingBuilder) =>
                {
                    loggingBuilder.ClearProviders();
                    loggingBuilder.AddConsole(options => options.IncludeScopes = true);
                });
                var configuration = new ConfigurationBuilder()
                    .AddJsonFile(appSettings)
                    .Build();
                builder.UseConfiguration(configuration);
                var configfile = $"{System.Environment.CurrentDirectory}/{appSettings}";
                builder.ConfigureAppConfiguration(c =>
                {
                    c.AddJsonFile(configfile);
                });


            }

            protected virtual void ConfigureServices(IServiceCollection services)
            {
                var configuration = new ConfigurationBuilder()
                    .AddJsonFile(appSettings)
                    .Build();
                services.AddSingleton(typeof(IConfiguration), configuration);
            }
        }
    }
}