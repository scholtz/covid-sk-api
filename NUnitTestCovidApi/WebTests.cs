#define DoTests

#if DoTests
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
using System.Diagnostics;
using System.IdentityModel.Tokens.Jwt;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
namespace NUnitTestCovidApi
{
    public class Tests : BaseApiMethods
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

        private HttpResponseMessage RegisterWithCompanyRegistration(
            HttpClient client,
            long chosenSlotId,
            string chosenPlaceId,
            string personCompanyId,
             string pass,
            string product,
            string token)
        {

            return client.PostAsync("Visitor/RegisterWithCompanyRegistration",
                    new System.Net.Http.FormUrlEncodedContent(new List<KeyValuePair<string, string>>() {
                        new KeyValuePair<string, string>("chosenSlot",chosenSlotId.ToString()),
                        new KeyValuePair<string, string>("chosenPlaceId",chosenPlaceId),
                        new KeyValuePair<string, string>("employeeNumber",personCompanyId),
                        new KeyValuePair<string, string>("pass",pass),
                        new KeyValuePair<string, string>("product",product),
                        new KeyValuePair<string, string>("token",token),
                    })
                ).Result;

        }
        private HttpResponseMessage RegisterEmployeeByDocumenter(
            HttpClient client,
            string employeeId,
            string email,
            string phone,
            DateTimeOffset time,
            string productId,
            string result)
        {

            var timeFormatted = time.ToString("o");

            return client.PostAsync("Visitor/RegisterEmployeeByDocumenter",
                    new System.Net.Http.FormUrlEncodedContent(new List<KeyValuePair<string, string>>() {
                        new KeyValuePair<string, string>("employeeId",employeeId),
                        new KeyValuePair<string, string>("email",email),
                        new KeyValuePair<string, string>("phone",phone),
                        new KeyValuePair<string, string>("time",timeFormatted),
                        new KeyValuePair<string, string>("productId",productId),
                        new KeyValuePair<string, string>("result",result),
                    })
                ).Result;
        }



        protected List<Visitor> RegisterTestVisitors(HttpClient client, string placeId, long slotId, string productId)
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
                RC = " 010101 /0008 ",
                Product = productId,
                BirthDayDay = 1,
                BirthDayMonth = 1,
                BirthDayYear = 2001,
                City = "City",
                Street = "Street",
                StreetNo = "10",
                ZIP = "10000",
            };
            var result = Register(client, visitor1);
            if (result.StatusCode != HttpStatusCode.OK) throw new Exception("Unable to make visitor " + result.Content.ReadAsStringAsync().Result);
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
                Product = productId,
                BirthDayDay = 1,
                BirthDayMonth = 1,
                BirthDayYear = 2001,
                City = "City2",
                Street = "Street2",
                StreetNo = "11",
                ZIP = "10001",
            };
            result = Register(client, visitor2);
            if (result.StatusCode != HttpStatusCode.OK) throw new Exception("Unable to make visitor");
            Registered.Add(Newtonsoft.Json.JsonConvert.DeserializeObject<Visitor>(result.Content.ReadAsStringAsync().Result));

            return Registered;
        }
        protected List<Visitor> RegisterTestVisitors2(HttpClient client, string placeId, long slotId, string productId)
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
                RC = " 010102 /0007 ",
                Product = productId,
                BirthDayDay = 2,
                BirthDayMonth = 1,
                BirthDayYear = 2001,
                City = "City",
                Street = "Street",
                StreetNo = "10",
                ZIP = "10000",
            };
            var result = Register(client, visitor1);
            if (result.StatusCode != HttpStatusCode.OK) throw new Exception("Unable to make visitor " + result.Content.ReadAsStringAsync().Result);
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
                RC = "0151020018",
                Product = productId,
                BirthDayDay = 2,
                BirthDayMonth = 1,
                BirthDayYear = 2001,
                City = "City2",
                Street = "Street2",
                StreetNo = "11",
                ZIP = "10001",
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

            var request = Preauthenticate(client, "@");
            Assert.AreEqual(HttpStatusCode.OK, request.StatusCode, request.Content.ReadAsStringAsync().Result);
            request = Authenticate(client, "@", "1");
            Assert.AreEqual(HttpStatusCode.BadRequest, request.StatusCode, request.Content.ReadAsStringAsync().Result);


            var users = configuration.GetSection("AdminUsers").Get<CovidMassTesting.Model.Settings.User[]>();//.GetValue<List<CovidMassTesting.Model.Settings.User>>("AdminUsers");

            var user = users.First(u => u.Name == "Admin");
            /// Preauthenticate
            request = Preauthenticate(client, user.Email);
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

            Task.Delay(2000).Wait();
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
                MainContact = "Admin Person",
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

            request = CreateProduct(client, new Product()
            {
                Name = "Antigenovy test",
                Description = "Štátny Antigenovy test",
                DefaultPrice = 0,
                DefaultPriceCurrency = "EUR",
                Category = "ant",
                All = true,
                InsuranceOnly = false,
            });
            Assert.AreEqual(HttpStatusCode.OK, request.StatusCode, request.Content.ReadAsStringAsync().Result);
            var pr1 = JsonConvert.DeserializeObject<Product>(request.Content.ReadAsStringAsync().Result);

            Assert.IsFalse(string.IsNullOrEmpty(adminToken));
            client.DefaultRequestHeaders.Clear();
            client.DefaultRequestHeaders.Add("Authorization", $"Bearer {adminToken}");
            var places = SetupDebugPlaces(client);
            var firstPlace = places[0];
            var secondPlace = places[1];


            // open testing for public

            var actions = new TimeUpdate[]
            {
                new TimeUpdate()
                {
                    Date = DateTimeOffset.Now.AddDays(1),
                    OpeningHoursTemplateId= 1,
                    PlaceId = firstPlace.Id,
                    Type = "set"
                },

                new TimeUpdate()
                {
                    Date = DateTimeOffset.Now.AddDays(1),
                    OpeningHoursTemplateId= 2,
                    PlaceId = secondPlace.Id,
                    Type = "set"
                },
                new TimeUpdate()
                {
                    Date = DateTimeOffset.Now.AddDays(2),
                    OpeningHoursTemplateId= 1,
                    PlaceId = secondPlace.Id,
                    Type = "set"
                },

                new TimeUpdate()
                {
                    Date = DateTimeOffset.Now.AddDays(2),
                    OpeningHoursTemplateId= 2,
                    PlaceId = firstPlace.Id,
                    Type = "set"
                },
            };

            request = ListScheduledDays(client);
            Assert.AreEqual(HttpStatusCode.OK, request.StatusCode, request.Content.ReadAsStringAsync().Result);
            var daysData = Newtonsoft.Json.JsonConvert.DeserializeObject<List<DayTimeManagement>>(request.Content.ReadAsStringAsync().Result);
            Assert.AreEqual(0, daysData.Count);

            request = ScheduleOpenningHours(client, actions);
            Assert.AreEqual(HttpStatusCode.OK, request.StatusCode, request.Content.ReadAsStringAsync().Result);

            client.DefaultRequestHeaders.Clear();

            request = ListPlaces(client);
            Assert.AreEqual(HttpStatusCode.OK, request.StatusCode, request.Content.ReadAsStringAsync().Result);
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
            var hour1 = hours[hours.Length - 2];

            request = ListHourSlotsByPlaceAndDaySlotId(client, place2.Id, day2.SlotId.ToString());
            Assert.AreEqual(HttpStatusCode.OK, request.StatusCode, request.Content.ReadAsStringAsync().Result);
            hours = Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, Slot1Hour>>(request.Content.ReadAsStringAsync().Result).Values.ToArray();
            Assert.IsTrue(hours.Length > 1);

            var hour2 = hours[hours.Length - 1];

            request = ListMinuteSlotsByPlaceAndHourSlotId(client, place1.Id, hour1.SlotId.ToString());
            Assert.AreEqual(HttpStatusCode.OK, request.StatusCode, request.Content.ReadAsStringAsync().Result);
            var minutes = Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, Slot5Min>>(request.Content.ReadAsStringAsync().Result).Values.ToArray();
            Assert.IsTrue(minutes.Length > 0);

            var minute = minutes[minutes.Length - 2];

            request = ListMinuteSlotsByPlaceAndHourSlotId(client, place2.Id, hour2.SlotId.ToString());
            Assert.AreEqual(HttpStatusCode.OK, request.StatusCode, request.Content.ReadAsStringAsync().Result);
            minutes = Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, Slot5Min>>(request.Content.ReadAsStringAsync().Result).Values.ToArray();
            Assert.IsTrue(minutes.Length > 0);

            var minute2 = minutes[minutes.Length - 1];

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

                BirthDayDay = 1,
                BirthDayMonth = 1,
                BirthDayYear = 2001,
                City = "City",
                Street = "Street",
                StreetNo = "10",
                ZIP = "10000",
                Product = pr1.Id
            };

            emailSender = web.Server.Services.GetService<CovidMassTesting.Controllers.Email.IEmailSender>();
            noEmailSender = emailSender as CovidMassTesting.Controllers.Email.NoEmailSender;
            noEmailSender.Data.Clear();


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

            Assert.AreEqual(1, noEmailSender.Data.Count);

            var tuple = noEmailSender.Data.Values.First();
            Assert.AreEqual(1, tuple.attachments.Count());

#if DEBUG
            var file = tuple.attachments.First();
            File.WriteAllBytes($"d:/covid/{file.Filename}", Convert.FromBase64String(file.Content));
#endif

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
        /*
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

            request = CreateProduct(client, new Product()
            {
                Name = "Antigenovy test",
                Description = "Štátny Antigenovy test",
                DefaultPrice = 0,
                DefaultPriceCurrency = "EUR",
                Category = "ant",
                All = true,
                InsuranceOnly = false,
            });
            Assert.AreEqual(HttpStatusCode.OK, request.StatusCode, request.Content.ReadAsStringAsync().Result);
            var pr1 = JsonConvert.DeserializeObject<Product>(request.Content.ReadAsStringAsync().Result);

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

                BirthDayDay = 1,
                BirthDayMonth = 1,
                BirthDayYear = 2001,
                City = "City",
                Street = "Street",
                StreetNo = "10",
                ZIP = "10000",
                Product = pr1.Id
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
        /**/

        [Test]
        public void RoleRegistrationManagerTest()
        {
            DropDatabase();

            using var web = new MockWebApp(AppSettings);
            var client = web.CreateClient();
            var users = configuration.GetSection("AdminUsers").Get<CovidMassTesting.Model.Settings.User[]>();
            var admin = users.First(u => u.Name == "Admin");

            var obj = new PlaceProvider()
            {
                VAT = "123",
                Web = "123",
                CompanyId = "123",
                CompanyName = "123, s.r.o.",
                Country = "SK",
                MainEmail = admin.Email,
                PrivatePhone = "+421 907 000000",
                MainContact = "Admin Person"
            };

            var request = PlaceProviderRegistration(client, obj);
            Assert.AreEqual(HttpStatusCode.OK, request.StatusCode, request.Content.ReadAsStringAsync().Result);
            var data = request.Content.ReadAsStringAsync().Result;
            var pp = JsonConvert.DeserializeObject<PlaceProvider>(data);
            request = AuthenticateUser(client, admin.Email, admin.Password);
            Assert.AreEqual(HttpStatusCode.OK, request.StatusCode, request.Content.ReadAsStringAsync().Result);
            var adminToken = request.Content.ReadAsStringAsync().Result;
            Assert.IsFalse(string.IsNullOrEmpty(adminToken));
            client.DefaultRequestHeaders.Add("Authorization", $"Bearer {adminToken}");
            SetupDebugPlaces(client);
            var pr1 = SetupDebugProduct(client);

            request = CheckSlotsDayToday(client);
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
            if (DateTime.Now.Hour >= 23) return;
            var day = days.First().Value;
            request = ListHourSlotsByPlaceAndDaySlotId(client, place.Id, day.SlotId.ToString());
            Assert.AreEqual(HttpStatusCode.OK, request.StatusCode, request.Content.ReadAsStringAsync().Result);
            var hours = Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, Slot1Hour>>(request.Content.ReadAsStringAsync().Result);
            Assert.IsTrue(hours.Count > 0);

            var hour = hours.Last().Value;
            request = ListMinuteSlotsByPlaceAndHourSlotId(client, place.Id, hour.SlotId.ToString());
            Assert.AreEqual(HttpStatusCode.OK, request.StatusCode, request.Content.ReadAsStringAsync().Result);
            var minutes = Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, Slot5Min>>(request.Content.ReadAsStringAsync().Result);
            Assert.IsTrue(minutes.Count > 0);

            var minute = minutes.Values.Last();

            var registered = RegisterTestVisitors(client, place.Id, minute.SlotId, pr1.Id);
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


            request = GetVisitorByRC(client, "0101010008");
            Assert.AreEqual(HttpStatusCode.OK, request.StatusCode, request.Content.ReadAsStringAsync().Result);
            responseVisitor = Newtonsoft.Json.JsonConvert.DeserializeObject<Visitor>(request.Content.ReadAsStringAsync().Result);
            Assert.AreEqual("0101010008", responseVisitor.RC);
            Assert.AreEqual(user1.FirstName, responseVisitor.FirstName);
            Assert.AreEqual(user1.LastName, responseVisitor.LastName);
            Assert.AreEqual(user1.Address, responseVisitor.Address);

            request = GetVisitorByRC(client, "  010101/0008 ");
            Assert.AreEqual(HttpStatusCode.OK, request.StatusCode, request.Content.ReadAsStringAsync().Result);
            responseVisitor = Newtonsoft.Json.JsonConvert.DeserializeObject<Visitor>(request.Content.ReadAsStringAsync().Result);
            Assert.AreEqual("0101010008", responseVisitor.RC);


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

            if (DateTimeOffset.Now.Hour <= 2 || DateTimeOffset.Now.Hour >= 23) return;

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

            var obj = new PlaceProvider()
            {
                VAT = "123",
                Web = "123",
                CompanyId = "123",
                CompanyName = "123, s.r.o.",
                Country = "SK",
                MainEmail = admin.Email,
                PrivatePhone = "+421907000000",
                MainContact = "Admin Person"
            };

            var request = PlaceProviderRegistration(client, obj);
            Assert.AreEqual(HttpStatusCode.OK, request.StatusCode, request.Content.ReadAsStringAsync().Result);
            var data = request.Content.ReadAsStringAsync().Result;
            var pp = JsonConvert.DeserializeObject<PlaceProvider>(data);
            request = AuthenticateUser(client, admin.Email, admin.Password);
            Assert.AreEqual(HttpStatusCode.OK, request.StatusCode, request.Content.ReadAsStringAsync().Result);
            var adminToken = request.Content.ReadAsStringAsync().Result;
            Assert.IsFalse(string.IsNullOrEmpty(adminToken));
            client.DefaultRequestHeaders.Add("Authorization", $"Bearer {adminToken}");
            SetupDebugPlaces(client);
            var pr1 = SetupDebugProduct(client);

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


            var smsSender = web.Server.Services.GetService<CovidMassTesting.Controllers.SMS.ISMSSender>();
            var noSMSSender = smsSender as CovidMassTesting.Controllers.SMS.MockSMSSender;
            noSMSSender?.Data.Clear();
            var minute = minutes.Values.First();
            var registered = RegisterTestVisitors(client, place.Id, minute.SlotId, pr1.Id);

            Assert.IsTrue(registered.Count >= 2);
            var sms = noSMSSender.Data.Values.First();
            var text = sms.data.GetText();
            Assert.IsTrue(text.Contains(minute.Description), $"{text} does not contain {minute.Description}");

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


            var iVisitor = web.Server.Services.GetService<CovidMassTesting.Repository.Interface.IVisitorRepository>();
            var visitor1 = iVisitor.GetVisitor(registered[0].Id).Result;
            visitor1.TestingTime = DateTimeOffset.Now.AddMinutes(-16);
            iVisitor.SetVisitor(visitor1, false);
            noSMSSender?.Data.Clear();

            // TEST mark as sick
            request = SetResult(client, test1, TestResult.PositiveWaitingForCertificate);
            Assert.AreEqual(HttpStatusCode.OK, request.StatusCode, request.Content.ReadAsStringAsync().Result);
            var result = Newtonsoft.Json.JsonConvert.DeserializeObject<Result>(request.Content.ReadAsStringAsync().Result);
            Assert.AreEqual(TestResult.PositiveWaitingForCertificate, result.State);
            if (configuration["SendResultsThroughQueue"] == "1")
            {
                Assert.AreEqual(0, noSMSSender?.Data.Count);
            }
            else
            {
                Assert.AreEqual(1, noSMSSender?.Data.Count);
            }
            iVisitor.ProcessSingle().Wait();
            Assert.AreEqual(1, noSMSSender?.Data.Count);
            sms = noSMSSender.Data.Values.First();
            Assert.AreEqual(registered[0].Phone, sms.toPhone);
            Assert.IsTrue(sms.data.GetText().Contains("POSITIVE"));
            //Assert.IsTrue(sms.data.GetText().Contains(DateTime.Now.ToString("dd.MM.yyyy")));
            Assert.IsTrue(sms.data.GetText().Contains("F1 L1"));
            Assert.IsTrue(sms.data.GetText().Contains("2001"));

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
            Assert.AreEqual(TestResult.PositiveCertificateTaken, result.State);


            request = DownloadPDF(client, registered[0].Id.ToString(), registered[0].RC.Substring(6, 4));
            Assert.AreEqual(HttpStatusCode.OK, request.StatusCode, request.Content.ReadAsStringAsync().Result);
            var file = request.Content.ReadAsByteArrayAsync().Result;
#if DEBUG
            File.WriteAllBytes("d:/covid/test-111-111-111.pdf", file);

            var emailSender = web.Server.Services.GetService<CovidMassTesting.Controllers.Email.IEmailSender>();
            var noEmailSender = emailSender as CovidMassTesting.Controllers.Email.NoEmailSender;

            foreach (var email in noEmailSender.Data.Values)
            {
                foreach (var att in email.attachments)
                {
                    File.WriteAllBytes($"d:/covid/{att.Filename}", Convert.FromBase64String(att.Content));
                }
            }

#endif

            request = PublicGetTestResult(client, registered[1].Id.ToString(), registered[1].RC.Substring(6, 4));
            Assert.AreEqual(HttpStatusCode.OK, request.StatusCode, request.Content.ReadAsStringAsync().Result);
            result = Newtonsoft.Json.JsonConvert.DeserializeObject<Result>(request.Content.ReadAsStringAsync().Result);
            Assert.AreEqual(TestResult.TestIsBeingProcessing, result.State);


            client.DefaultRequestHeaders.Clear();
            client.DefaultRequestHeaders.Add("Authorization", $"Bearer {adminToken}");
            request = StatsTestedVisitors(client, StatsType.Tested, pp.PlaceProviderId);
            Assert.AreEqual(HttpStatusCode.OK, request.StatusCode, request.Content.ReadAsStringAsync().Result);
            var stats = Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<DateTimeOffset, long>>(request.Content.ReadAsStringAsync().Result);
            Assert.AreEqual(1, stats.Count);
            var time = new DateTimeOffset(DateTimeOffset.Now.RoundDay(), TimeSpan.Zero);
            if (DateTime.Now.Hour > 1)
            {
                Assert.AreEqual(1, stats[time]);
            }
        }


        [Test]
        public void RoleDocumentManagerTest()
        {
            DropDatabase();

            using var web = new MockWebApp(AppSettings);
            var client = web.CreateClient();
            var users = configuration.GetSection("AdminUsers").Get<CovidMassTesting.Model.Settings.User[]>();
            var admin = users.First(u => u.Name == "Admin");

            var obj = new PlaceProvider()
            {
                VAT = "123",
                Web = "123",
                CompanyId = "123",
                CompanyName = "123, s.r.o.",
                Country = "SK",
                MainEmail = admin.Email,
                PrivatePhone = "+421 907 000000",
                MainContact = "Admin Person"
            };

            var request = PlaceProviderRegistration(client, obj);
            Assert.AreEqual(HttpStatusCode.OK, request.StatusCode, request.Content.ReadAsStringAsync().Result);

            request = AuthenticateUser(client, admin.Email, admin.Password);
            Assert.AreEqual(HttpStatusCode.OK, request.StatusCode, request.Content.ReadAsStringAsync().Result);
            var adminToken = request.Content.ReadAsStringAsync().Result;
            Assert.IsFalse(string.IsNullOrEmpty(adminToken));
            client.DefaultRequestHeaders.Add("Authorization", $"Bearer {adminToken}");
            SetupDebugPlaces(client);
            var pr1 = SetupDebugProduct(client);

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
            var registered = RegisterTestVisitors(client, place.Id, minute.SlotId, pr1.Id);
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

            var iVisitor = web.Server.Services.GetService<CovidMassTesting.Repository.Interface.IVisitorRepository>();
            var visitor1 = iVisitor.GetVisitor(registered[0].Id).Result;
            visitor1.TestingTime = DateTimeOffset.Now.AddMinutes(-16);
            iVisitor.SetVisitor(visitor1, false);
            var visitor2 = iVisitor.GetVisitor(registered[1].Id).Result;
            visitor2.TestingTime = DateTimeOffset.Now.AddMinutes(-16);
            iVisitor.SetVisitor(visitor2, false);

            // TEST mark as sick
            request = SetResult(client, test1, TestResult.PositiveWaitingForCertificate);
            Assert.AreEqual(HttpStatusCode.OK, request.StatusCode, request.Content.ReadAsStringAsync().Result);
            var result1 = Newtonsoft.Json.JsonConvert.DeserializeObject<Result>(request.Content.ReadAsStringAsync().Result);
            Assert.AreEqual(TestResult.PositiveWaitingForCertificate, result1.State);

            // TEST mark as sick
            request = SetResult(client, test2, TestResult.NegativeWaitingForCertificate);
            Assert.AreEqual(HttpStatusCode.OK, request.StatusCode, request.Content.ReadAsStringAsync().Result);
            var result2 = Newtonsoft.Json.JsonConvert.DeserializeObject<Result>(request.Content.ReadAsStringAsync().Result);
            Assert.AreEqual(TestResult.NegativeWaitingForCertificate, result2.State);

            result1.Time = DateTimeOffset.Now.AddMinutes(-15);
            iVisitor.SetResultObject(result1, false);
            result2.Time = DateTimeOffset.Now.AddMinutes(-15);
            iVisitor.SetResultObject(result2, false);
            iVisitor.ProcessSingle().Wait();
            iVisitor.ProcessSingle().Wait();
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
            var result = Newtonsoft.Json.JsonConvert.DeserializeObject<Result>(request.Content.ReadAsStringAsync().Result);
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
            /*
            // remove my private information from system
            request = PublicRemoveTest(client, registered[1].Id.ToString(), registered[1].RC.Substring(6, 4));
            Assert.AreEqual(HttpStatusCode.OK, request.StatusCode, request.Content.ReadAsStringAsync().Result);
            bool resultBool = Newtonsoft.Json.JsonConvert.DeserializeObject<bool>(request.Content.ReadAsStringAsync().Result);
            Assert.IsTrue(resultBool);

            // unable to delete removed test
            request = PublicRemoveTest(client, registered[1].Id.ToString(), registered[1].RC.Substring(6, 4));
            Assert.AreEqual(HttpStatusCode.BadRequest, request.StatusCode, request.Content.ReadAsStringAsync().Result);
            /**/
        }
        [Test]
        public void RoleDataExporterTest()
        {
            DropDatabase();

            using var web = new MockWebApp(AppSettings);
            var client = web.CreateClient();
            var users = configuration.GetSection("AdminUsers").Get<CovidMassTesting.Model.Settings.User[]>();
            var admin = users.First(u => u.Name == "Admin");

            var obj = new PlaceProvider()
            {
                VAT = "123",
                Web = "123",
                CompanyId = "123",
                CompanyName = "123, s.r.o.",
                Country = "SK",
                MainEmail = admin.Email,
                PrivatePhone = "+421 907 000000",
                MainContact = "Admin Person"
            };

            var response = PlaceProviderRegistration(client, obj);
            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode, response.Content.ReadAsStringAsync().Result);
            var data = response.Content.ReadAsStringAsync().Result;
            var pp1 = JsonConvert.DeserializeObject<PlaceProvider>(data);

            response = AuthenticateUser(client, admin.Email, admin.Password);
            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode, response.Content.ReadAsStringAsync().Result);
            var adminToken = response.Content.ReadAsStringAsync().Result;
            Assert.IsFalse(string.IsNullOrEmpty(adminToken));
            client.DefaultRequestHeaders.Add("Authorization", $"Bearer {adminToken}");
            SetupDebugPlaces(client);
            var pr1 = SetupDebugProduct(client);

            var dataexporter = users.First(u => u.Name == "DataExporter");
            response = InviteUserToPP(client, dataexporter.Email, "Person 2", "+421 907 000 000", "MyMessage");
            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode, response.Content.ReadAsStringAsync().Result);

            response = AuthenticateUser(client, dataexporter.Email, dataexporter.Password);
            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode, response.Content.ReadAsStringAsync().Result);
            var dataExporterToken = response.Content.ReadAsStringAsync().Result;

            response = CheckSlotsDay1(client);
            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode, response.Content.ReadAsStringAsync().Result);

            client.DefaultRequestHeaders.Clear();
            client.DefaultRequestHeaders.Add("Authorization", $"Bearer {dataExporterToken}");

            response = ListUserInvites(client);
            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode, response.Content.ReadAsStringAsync().Result);
            var invites = JsonConvert.DeserializeObject<List<Invitation>>(response.Content.ReadAsStringAsync().Result);
            Assert.AreEqual(1, invites.Count);
            response = ProcessInvitation(client, invites[0].InvitationId, true);
            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode, response.Content.ReadAsStringAsync().Result);


            client.DefaultRequestHeaders.Clear();


            response = ListPlaces(client);
            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode, response.Content.ReadAsStringAsync().Result);
            var places = Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, Place>>(response.Content.ReadAsStringAsync().Result);
            Assert.IsTrue(places.Count > 0);
            var place = places.First().Value;
            response = ListDaySlotsByPlace(client, place.Id);
            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode, response.Content.ReadAsStringAsync().Result);
            var days = Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, Slot1Day>>(response.Content.ReadAsStringAsync().Result);
            Assert.IsTrue(days.Count > 0);

            var day = days.First().Value;
            response = ListHourSlotsByPlaceAndDaySlotId(client, place.Id, day.SlotId.ToString());
            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode, response.Content.ReadAsStringAsync().Result);
            var hours = Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, Slot1Hour>>(response.Content.ReadAsStringAsync().Result);
            Assert.IsTrue(hours.Count > 0);

            var hour = hours.First().Value;
            response = ListMinuteSlotsByPlaceAndHourSlotId(client, place.Id, hour.SlotId.ToString());
            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode, response.Content.ReadAsStringAsync().Result);
            var minutes = Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, Slot5Min>>(response.Content.ReadAsStringAsync().Result);
            Assert.IsTrue(minutes.Count > 0);

            var minute = minutes.Values.First();
            var registered = RegisterTestVisitors(client, place.Id, minute.SlotId, pr1.Id);
            Assert.IsTrue(registered.Count >= 2);

            client.DefaultRequestHeaders.Clear();
            client.DefaultRequestHeaders.Add("Authorization", $"Bearer {adminToken}");

            response = ListDaySlotsByPlace(client, place.Id);
            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode, response.Content.ReadAsStringAsync().Result);
            days = Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, Slot1Day>>(response.Content.ReadAsStringAsync().Result);
            Assert.IsTrue(days.Count > 0);

            response = FixAdvancedStatsSlots(client);
            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode, response.Content.ReadAsStringAsync().Result);

            response = ListDaySlotsByPlace(client, place.Id);

            var days2 = Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, Slot1Day>>(response.Content.ReadAsStringAsync().Result);
            Assert.IsTrue(days2.Count > 0);

            foreach (var dayI in days)
            {
                Assert.AreEqual(days2[dayI.Key].Registrations, dayI.Value.Registrations);
            }

            response = ListHourSlotsByPlaceAndDaySlotId(client, place.Id, day.SlotId.ToString());
            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode, response.Content.ReadAsStringAsync().Result);
            var hours2 = Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, Slot1Hour>>(response.Content.ReadAsStringAsync().Result);
            Assert.IsTrue(hours2.Count > 0);

            var hour2 = hours.First().Value;
            response = ListMinuteSlotsByPlaceAndHourSlotId(client, place.Id, hour.SlotId.ToString());
            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode, response.Content.ReadAsStringAsync().Result);
            var minutes2 = Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, Slot5Min>>(response.Content.ReadAsStringAsync().Result);
            Assert.IsTrue(minutes2.Count > 0);


            var registrationManager = users.First(u => u.Name == "RegistrationManager");
            response = AuthenticateUser(client, registrationManager.Email, registrationManager.Password);
            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode, response.Content.ReadAsStringAsync().Result);
            var registrationManagerToken = response.Content.ReadAsStringAsync().Result;
            Assert.IsFalse(string.IsNullOrEmpty(registrationManagerToken));

            client.DefaultRequestHeaders.Clear();
            client.DefaultRequestHeaders.Add("Authorization", $"Bearer {registrationManagerToken}");

            string test1 = "111-111-111";
            response = ConnectVisitorToTest(client, registered[0].Id.ToString(), test1);
            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode, response.Content.ReadAsStringAsync().Result);

            string test2 = "222-222-222";
            response = ConnectVisitorToTest(client, registered[1].Id.ToString(), test2);
            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode, response.Content.ReadAsStringAsync().Result);

            var medicLab = users.First(u => u.Name == "MedicLab");
            response = AuthenticateUser(client, medicLab.Email, medicLab.Password);
            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode, response.Content.ReadAsStringAsync().Result);
            var medicLabToken = response.Content.ReadAsStringAsync().Result;
            Assert.IsFalse(string.IsNullOrEmpty(registrationManagerToken));
            client.DefaultRequestHeaders.Clear();
            client.DefaultRequestHeaders.Add("Authorization", $"Bearer {medicLabToken}");

            // TEST mark as sick
            response = SetResult(client, test1, TestResult.PositiveWaitingForCertificate);
            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode, response.Content.ReadAsStringAsync().Result);
            var result = Newtonsoft.Json.JsonConvert.DeserializeObject<Result>(response.Content.ReadAsStringAsync().Result);
            Assert.AreEqual(Result.Values.Positive, result.State);
            Assert.AreEqual(false, result.TimeIsValid);
            Assert.AreEqual(true, result.Matched);

            var iVisitor = web.Server.Services.GetService<CovidMassTesting.Repository.Interface.IVisitorRepository>();
            var visitor1 = iVisitor.GetVisitor(registered[0].Id).Result;
            visitor1.TestingTime = DateTimeOffset.Now.AddMinutes(-16);
            iVisitor.SetVisitor(visitor1, false);

            response = SetResult(client, test1, TestResult.PositiveWaitingForCertificate);
            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode, response.Content.ReadAsStringAsync().Result);
            var result1 = Newtonsoft.Json.JsonConvert.DeserializeObject<Result>(response.Content.ReadAsStringAsync().Result);
            Assert.AreEqual(TestResult.PositiveWaitingForCertificate, result1.State);
            Assert.AreEqual(true, result1.TimeIsValid);
            Assert.AreEqual(true, result1.Matched);

            // TEST mark as healthy
            response = SetResult(client, test2, TestResult.NegativeWaitingForCertificate);
            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode, response.Content.ReadAsStringAsync().Result);
            result = Newtonsoft.Json.JsonConvert.DeserializeObject<Result>(response.Content.ReadAsStringAsync().Result);
            Assert.AreEqual(Result.Values.Negative, result.State);
            Assert.AreEqual(false, result.TimeIsValid);
            Assert.AreEqual(true, result.Matched);

            var visitor2 = iVisitor.GetVisitor(registered[1].Id).Result;
            visitor2.TestingTime = DateTimeOffset.Now.AddMinutes(-16);
            iVisitor.SetVisitor(visitor2, false);

            response = SetResult(client, test2, TestResult.NegativeWaitingForCertificate);
            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode, response.Content.ReadAsStringAsync().Result);
            var result2 = Newtonsoft.Json.JsonConvert.DeserializeObject<Result>(response.Content.ReadAsStringAsync().Result);
            Assert.AreEqual(Result.Values.Negative, result2.State);
            Assert.AreEqual(true, result2.TimeIsValid);
            Assert.AreEqual(true, result2.Matched);



            client.DefaultRequestHeaders.Clear();
            client.DefaultRequestHeaders.Add("Authorization", $"Bearer {dataExporterToken}");

            response = SetPlaceProvider(client, pp1.PlaceProviderId);
            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode, response.Content.ReadAsStringAsync().Result);
            dataExporterToken = response.Content.ReadAsStringAsync().Result;

            client.DefaultRequestHeaders.Clear();
            client.DefaultRequestHeaders.Add("Authorization", $"Bearer {dataExporterToken}");


            response = FinalDataExport(client, 0, 100);
            var stream = new MemoryStream();
            response.Content.CopyToAsync(stream).Wait();
            var resultExport = Encoding.UTF8.GetString(stream.ToArray());
            Assert.IsNotNull(resultExport);
            Assert.IsTrue(resultExport.Contains("111111111"));
            Assert.IsTrue(resultExport.Contains(registered[0].Id.ToString()));
            Assert.IsTrue(resultExport.Contains(registered[0].Phone));
            Assert.IsTrue(resultExport.Contains(registered[0].RC));
            Assert.IsFalse(resultExport.Contains(registered[1].Id.ToString()));
            Assert.IsFalse(resultExport.Contains(registered[1].RC));

            response = ListTestedVisitors(client, 0, 100);
            stream = new MemoryStream();
            response.Content.CopyToAsync(stream).Wait();
            resultExport = Encoding.UTF8.GetString(stream.ToArray());
            Assert.IsNotNull(resultExport);
            Assert.IsTrue(resultExport.Contains("111111111"));
            Assert.IsTrue(resultExport.Contains(registered[0].Id.ToString()));
            Assert.IsTrue(resultExport.Contains(registered[0].Phone));
            Assert.IsTrue(resultExport.Contains(registered[0].RC));
            Assert.IsTrue(resultExport.Contains(registered[1].Id.ToString()));
            Assert.IsTrue(resultExport.Contains(registered[1].RC));

            client.DefaultRequestHeaders.Clear();

            var documentManager = users.First(u => u.Name == "DocumentManager");
            response = AuthenticateUser(client, documentManager.Email, documentManager.Password);
            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode, response.Content.ReadAsStringAsync().Result);
            var documentManagerToken = response.Content.ReadAsStringAsync().Result;
            Assert.IsFalse(string.IsNullOrEmpty(registrationManagerToken));
            client.DefaultRequestHeaders.Add("Authorization", $"Bearer {documentManagerToken}");

            result1.Time = DateTimeOffset.Now.AddMinutes(-15).AddSeconds(1);
            iVisitor.SetResultObject(result1, false);

            iVisitor.ProcessSingle().Wait();

            // Test fetch one document to fill in.
            // It must be in queue, so the first one we have marked as result
            response = GetNextTest(client);
            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode, response.Content.ReadAsStringAsync().Result);
            var visitorForDocumenter = Newtonsoft.Json.JsonConvert.DeserializeObject<Visitor>(response.Content.ReadAsStringAsync().Result);
            Assert.AreEqual(test1.Replace("-", ""), visitorForDocumenter.TestingSet);
            Assert.AreEqual(TestResult.PositiveWaitingForCertificate, visitorForDocumenter.Result);
            // Repeated request must return the same thing until we mark it as processed
            response = GetNextTest(client);
            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode, response.Content.ReadAsStringAsync().Result);
            visitorForDocumenter = Newtonsoft.Json.JsonConvert.DeserializeObject<Visitor>(response.Content.ReadAsStringAsync().Result);
            Assert.AreEqual(test1.Replace("-", ""), visitorForDocumenter.TestingSet);
            Assert.AreEqual(TestResult.PositiveWaitingForCertificate, visitorForDocumenter.Result);

            response = RemoveFromDocQueue(client, test1);
            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode, response.Content.ReadAsStringAsync().Result);

            result1.Time = DateTimeOffset.Now.AddMinutes(-15).AddSeconds(2);
            iVisitor.SetResultObject(result1, false);

            iVisitor.ProcessSingle().Wait();

            response = GetNextTest(client);
            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode, response.Content.ReadAsStringAsync().Result);
            visitorForDocumenter = Newtonsoft.Json.JsonConvert.DeserializeObject<Visitor>(response.Content.ReadAsStringAsync().Result);
            Assert.AreEqual(test2.Replace("-", ""), visitorForDocumenter.TestingSet);
            Assert.AreEqual(TestResult.NegativeWaitingForCertificate, visitorForDocumenter.Result);

            response = RemoveFromDocQueue(client, test2);
            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode, response.Content.ReadAsStringAsync().Result);

            response = GetNextTest(client);
            Assert.AreEqual(HttpStatusCode.NoContent, response.StatusCode, response.Content.ReadAsStringAsync().Result);
            result = Newtonsoft.Json.JsonConvert.DeserializeObject<Result>(response.Content.ReadAsStringAsync().Result);
            // When no other item is in queue, the server should return NoContent (204), with no data
            Assert.IsNull(result);

            // check public info
            response = PublicGetTestResult(client, registered[0].Id.ToString(), registered[0].RC.Substring(6, 4));
            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode, response.Content.ReadAsStringAsync().Result);
            result = Newtonsoft.Json.JsonConvert.DeserializeObject<Result>(response.Content.ReadAsStringAsync().Result);
            Assert.AreEqual(TestResult.PositiveCertificateTaken, result.State);

            response = PublicGetTestResult(client, registered[1].Id.ToString(), registered[1].RC.Substring(6, 4));
            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode, response.Content.ReadAsStringAsync().Result);
            result = Newtonsoft.Json.JsonConvert.DeserializeObject<Result>(response.Content.ReadAsStringAsync().Result);
            Assert.AreEqual(TestResult.NegativeCertificateTaken, result.State);

            // remove my private information from system
            /*
            request = PublicRemoveTest(client, registered[1].Id.ToString(), registered[1].RC.Substring(6, 4));
            Assert.AreEqual(HttpStatusCode.OK, request.StatusCode, request.Content.ReadAsStringAsync().Result);
            bool resultBool = Newtonsoft.Json.JsonConvert.DeserializeObject<bool>(request.Content.ReadAsStringAsync().Result);
            Assert.IsTrue(resultBool);

            // unable to delete removed test
            request = PublicRemoveTest(client, registered[1].Id.ToString(), registered[1].RC.Substring(6, 4));
            Assert.AreEqual(HttpStatusCode.BadRequest, request.StatusCode, request.Content.ReadAsStringAsync().Result);
            /**/
            // unauthorized request
            response = FinalDataExport(client, 0, 100);
            Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode);

            //authorized data export
            client.DefaultRequestHeaders.Clear();
            client.DefaultRequestHeaders.Add("Authorization", $"Bearer {dataExporterToken}");
            response = FinalDataExport(client, 0, 100);
            stream = new MemoryStream();
            response.Content.CopyToAsync(stream).Wait();
            resultExport = Encoding.UTF8.GetString(stream.ToArray());
            Assert.IsNotNull(resultExport);
            Assert.IsTrue(resultExport.Contains("111111111"));
            Assert.IsTrue(resultExport.Contains(registered[0].Id.ToString()));
            Assert.IsTrue(resultExport.Contains(registered[0].Phone));
            Assert.IsTrue(resultExport.Contains(registered[0].RC));
            Assert.IsFalse(resultExport.Contains(registered[1].Id.ToString()));
            Assert.IsFalse(resultExport.Contains(registered[1].RC));

            response = ListTestedVisitors(client, 0, 100);
            stream = new MemoryStream();
            response.Content.CopyToAsync(stream).Wait();
            resultExport = Encoding.UTF8.GetString(stream.ToArray());
            Assert.IsNotNull(resultExport);
            Assert.IsTrue(resultExport.Contains("111111111"));
            Assert.IsTrue(resultExport.Contains(registered[0].Id.ToString()));
            Assert.IsTrue(resultExport.Contains(registered[0].Phone));
            Assert.IsTrue(resultExport.Contains(registered[0].RC));
            Assert.IsTrue(resultExport.Contains(registered[1].Id.ToString()));
            Assert.IsTrue(resultExport.Contains(registered[1].RC));
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
            Assert.AreEqual("23:45-23:55", daySlot.OpeningHours);
            Assert.AreEqual(2, daySlot.OpeningHoursTemplate);
            request = ListHourSlotsByPlaceAndDaySlotId(client, place.Id, daySlot.SlotId.ToString());
            Assert.AreEqual(HttpStatusCode.OK, request.StatusCode, request.Content.ReadAsStringAsync().Result);

            if (DateTimeOffset.Now.Hour == 0) return;

            var hoursDictionary = Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, Slot1Hour>>(request.Content.ReadAsStringAsync().Result);
            Assert.AreEqual(1, hoursDictionary.Count);
            var hourSlot = hoursDictionary.Values.First();

            request = ListMinuteSlotsByPlaceAndHourSlotId(client, place.Id, hourSlot.SlotId.ToString());
            Assert.AreEqual(HttpStatusCode.OK, request.StatusCode, request.Content.ReadAsStringAsync().Result);

            var minutesDictionary = Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, Slot5Min>>(request.Content.ReadAsStringAsync().Result);
            Assert.AreEqual(2, minutesDictionary.Count);



            request = ListDaySlotsByPlace(client, second.Id);
            Assert.AreEqual(HttpStatusCode.OK, request.StatusCode, request.Content.ReadAsStringAsync().Result);

            daysDictionary = Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, Slot1Day>>(request.Content.ReadAsStringAsync().Result);
            Assert.AreEqual(1, daysDictionary.Count);
            daySlot = daysDictionary.Values.First();
            Assert.AreEqual("21:00-23:55", daySlot.OpeningHours);
            Assert.AreEqual(1, daySlot.OpeningHoursTemplate);
            request = ListHourSlotsByPlaceAndDaySlotId(client, second.Id, daySlot.SlotId.ToString());
            Assert.AreEqual(HttpStatusCode.OK, request.StatusCode, request.Content.ReadAsStringAsync().Result);
            if (DateTime.Now.Hour >= 23) return;
            hoursDictionary = Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, Slot1Hour>>(request.Content.ReadAsStringAsync().Result);
            Assert.IsTrue(hoursDictionary.Count >= 1);
            hourSlot = hoursDictionary.Values.First();

            request = ListMinuteSlotsByPlaceAndHourSlotId(client, second.Id, hourSlot.SlotId.ToString());
            Assert.AreEqual(HttpStatusCode.OK, request.StatusCode, request.Content.ReadAsStringAsync().Result);

            minutesDictionary = Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, Slot5Min>>(request.Content.ReadAsStringAsync().Result);
            Assert.IsTrue(minutesDictionary.Count >= 1);

            request = ListScheduledDays(client);
            Assert.AreEqual(HttpStatusCode.OK, request.StatusCode, request.Content.ReadAsStringAsync().Result);
            daysData = Newtonsoft.Json.JsonConvert.DeserializeObject<List<DayTimeManagement>>(request.Content.ReadAsStringAsync().Result);
            Assert.AreEqual(2, daysData.Count);

            var keys = daysData.Select(d => d.SlotId).OrderBy(d => d).ToArray();
            var day1 = daysData.First(d => d.SlotId == keys[0]);
            Assert.IsTrue(day1.OpeningHours.Contains("23:00-23:55"));
            Assert.IsTrue(day1.OpeningHours.Contains("21:00-23:55"));
            Assert.IsTrue(day1.OpeningHoursTemplates.Contains(1));
            var day2 = daysData.First(d => d.SlotId == keys[1]);
            Assert.IsTrue(day2.OpeningHours.Contains("23:45-23:55"));
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

            request = FinalDataExport(client, 0, 100);
            Assert.AreEqual(HttpStatusCode.BadRequest, request.StatusCode);

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

            request = FinalDataExport(client, 0, 100);
            Assert.AreEqual(HttpStatusCode.BadRequest, request.StatusCode);

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
        public async Task RoleMedicTesterPPTest()
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

            // test limit 1 place provider registration per api configuration
            request = PlaceProviderRegistration(client, obj);
            Assert.AreEqual(HttpStatusCode.BadRequest, request.StatusCode, request.Content.ReadAsStringAsync().Result);



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

            request = FinalDataExport(client, 0, 100);
            Assert.AreEqual(HttpStatusCode.BadRequest, request.StatusCode);

            // Test product management
            request = CreateProduct(client, new Product()
            {
                Name = "Drahá vakcína",
                Description = "Vakcína ktorá nie je hradená poisťovňou",
                DefaultPrice = 100M,
                DefaultPriceCurrency = "EUR",
                Category = "vac",
                All = false,
                InsuranceOnly = false,
            });
            Assert.AreEqual(HttpStatusCode.OK, request.StatusCode, request.Content.ReadAsStringAsync().Result);
            var pr1 = JsonConvert.DeserializeObject<Product>(request.Content.ReadAsStringAsync().Result);
            Assert.AreEqual(100, pr1.DefaultPrice);
            request = CreateProduct(client, new Product()
            {
                Name = "Vakcína zadarmo",
                Description = "Vakcína ktorá je hradená poisťovňou",
                DefaultPrice = 0,
                DefaultPriceCurrency = "EUR",
                Category = "vac",
                All = true,
                InsuranceOnly = true,
            });
            Assert.AreEqual(HttpStatusCode.OK, request.StatusCode, request.Content.ReadAsStringAsync().Result);
            var pr2 = JsonConvert.DeserializeObject<Product>(request.Content.ReadAsStringAsync().Result);

            pr1.DefaultPrice = 99;
            request = UpdateProduct(client, pr1);
            Assert.AreEqual(HttpStatusCode.OK, request.StatusCode, request.Content.ReadAsStringAsync().Result);

            request = ListProducts(client);
            Assert.AreEqual(HttpStatusCode.OK, request.StatusCode, request.Content.ReadAsStringAsync().Result);
            var prList = JsonConvert.DeserializeObject<List<Product>>(request.Content.ReadAsStringAsync().Result);
            Assert.AreEqual(2, prList.Count);

            request = DeleteProduct(client, pr2);
            Assert.AreEqual(HttpStatusCode.OK, request.StatusCode, request.Content.ReadAsStringAsync().Result);
            request = ListProducts(client);
            Assert.AreEqual(HttpStatusCode.OK, request.StatusCode, request.Content.ReadAsStringAsync().Result);
            prList = JsonConvert.DeserializeObject<List<Product>>(request.Content.ReadAsStringAsync().Result);
            Assert.AreEqual(1, prList.Count);

            request = CreateProduct(client, new Product()
            {
                Name = "Vakcína zadarmo",
                Description = "Vakcína ktorá je hradená poisťovňou",
                DefaultPrice = 0,
                DefaultPriceCurrency = "EUR",
                Category = "vac",
                All = true,
                InsuranceOnly = true,
            });
            Assert.AreEqual(HttpStatusCode.OK, request.StatusCode, request.Content.ReadAsStringAsync().Result);
            pr2 = JsonConvert.DeserializeObject<Product>(request.Content.ReadAsStringAsync().Result);

            // setup places
            var debugPlaces = SetupDebugPlaces(client);
            var firstPlace = debugPlaces.First();
            var secondPlace = debugPlaces.Skip(1).First();

            // ListFiltered

            request = ListFiltered(client, "all", "all");
            Assert.AreEqual(HttpStatusCode.OK, request.StatusCode, request.Content.ReadAsStringAsync().Result);
            var filteredPlaces = JsonConvert.DeserializeObject<Dictionary<string, Place>>(request.Content.ReadAsStringAsync().Result);
            Assert.AreEqual(3, filteredPlaces.Count);

            request = ListFiltered(client, "vac-doctor", "all");
            Assert.AreEqual(HttpStatusCode.OK, request.StatusCode, request.Content.ReadAsStringAsync().Result);
            filteredPlaces = JsonConvert.DeserializeObject<Dictionary<string, Place>>(request.Content.ReadAsStringAsync().Result);
            Assert.AreEqual(3, filteredPlaces.Count);

            request = ListFiltered(client, "vac-self", "all");
            Assert.AreEqual(HttpStatusCode.OK, request.StatusCode, request.Content.ReadAsStringAsync().Result);
            filteredPlaces = JsonConvert.DeserializeObject<Dictionary<string, Place>>(request.Content.ReadAsStringAsync().Result);
            Assert.AreEqual(3, filteredPlaces.Count);

            request = ListFiltered(client, "ant-self", "all");
            Assert.AreEqual(HttpStatusCode.OK, request.StatusCode, request.Content.ReadAsStringAsync().Result);
            filteredPlaces = JsonConvert.DeserializeObject<Dictionary<string, Place>>(request.Content.ReadAsStringAsync().Result);
            Assert.AreEqual(0, filteredPlaces.Count);

            // test productplace
            request = ListPlaceProductByPlaceProvider(client, pp.PlaceProviderId);
            Assert.AreEqual(HttpStatusCode.OK, request.StatusCode, request.Content.ReadAsStringAsync().Result);
            var placeProducts = JsonConvert.DeserializeObject<List<PlaceProduct>>(request.Content.ReadAsStringAsync().Result);
            Assert.AreEqual(3, placeProducts.Count);

            request = ListPlaceProductByPlace(client, firstPlace.Id);
            Assert.AreEqual(HttpStatusCode.OK, request.StatusCode, request.Content.ReadAsStringAsync().Result);
            placeProducts = JsonConvert.DeserializeObject<List<PlaceProduct>>(request.Content.ReadAsStringAsync().Result);
            Assert.AreEqual(1, placeProducts.Count);

            request = ListPlaceProductByCategory(client, "vac");
            Assert.AreEqual(HttpStatusCode.OK, request.StatusCode, request.Content.ReadAsStringAsync().Result);
            placeProducts = JsonConvert.DeserializeObject<List<PlaceProduct>>(request.Content.ReadAsStringAsync().Result);
            Assert.AreEqual(3, placeProducts.Count);

            request = InsertOrUpdatePlaceProduct(client, new PlaceProduct()
            {
                PlaceId = firstPlace.Id,
                PlaceProviderId = pp.PlaceProviderId,
                CustomPrice = true,
                Price = 100M,
                PriceCurrency = "EUR",
                From = DateTimeOffset.Now.AddDays(1),
                Until = DateTimeOffset.Now.AddDays(8),
                ProductId = pr1.Id,
            });
            Assert.AreEqual(HttpStatusCode.OK, request.StatusCode, request.Content.ReadAsStringAsync().Result);
            var placeProduct1 = JsonConvert.DeserializeObject<PlaceProduct>(request.Content.ReadAsStringAsync().Result);
            request = ListPlaceProductByPlace(client, firstPlace.Id);
            Assert.AreEqual(HttpStatusCode.OK, request.StatusCode, request.Content.ReadAsStringAsync().Result);
            var placeProducts2 = JsonConvert.DeserializeObject<List<PlaceProductWithPlace>>(request.Content.ReadAsStringAsync().Result);
            Assert.AreEqual(2, placeProducts2.Count);
            foreach (var pr in placeProducts2)
            {
                Assert.IsNotNull(pr.Product);
            }


            request = ListPlaceProduct(client);
            Assert.AreEqual(HttpStatusCode.OK, request.StatusCode, request.Content.ReadAsStringAsync().Result);
            placeProducts = JsonConvert.DeserializeObject<List<PlaceProduct>>(request.Content.ReadAsStringAsync().Result);
            Assert.AreEqual(4, placeProducts.Count);

            request = DeletePlaceProduct(client, placeProduct1.Id);
            Assert.AreEqual(HttpStatusCode.OK, request.StatusCode, request.Content.ReadAsStringAsync().Result);

            request = ListPlaceProductByPlace(client, firstPlace.Id);
            Assert.AreEqual(HttpStatusCode.OK, request.StatusCode, request.Content.ReadAsStringAsync().Result);
            placeProducts = JsonConvert.DeserializeObject<List<PlaceProduct>>(request.Content.ReadAsStringAsync().Result);
            Assert.AreEqual(1, placeProducts.Count);

            // vac-doctor, specific place
            pr2.All = false;
            request = UpdateProduct(client, pr2);
            Assert.AreEqual(HttpStatusCode.OK, request.StatusCode, request.Content.ReadAsStringAsync().Result);

            request = ListFiltered(client, "vac-doctor", "all");
            Assert.AreEqual(HttpStatusCode.OK, request.StatusCode, request.Content.ReadAsStringAsync().Result);
            filteredPlaces = JsonConvert.DeserializeObject<Dictionary<string, Place>>(request.Content.ReadAsStringAsync().Result);
            Assert.AreEqual(0, filteredPlaces.Count);

            request = InsertOrUpdatePlaceProduct(client, new PlaceProduct()
            {
                PlaceId = secondPlace.Id,
                PlaceProviderId = pp.PlaceProviderId,
                CustomPrice = true,
                Price = 99M,
                PriceCurrency = "EUR",
                From = DateTimeOffset.Now.AddDays(1),
                Until = DateTimeOffset.Now.AddDays(8),
                ProductId = pr2.Id,
            });


            request = ListFiltered(client, "vac-doctor", "all");
            Assert.AreEqual(HttpStatusCode.OK, request.StatusCode, request.Content.ReadAsStringAsync().Result);
            filteredPlaces = JsonConvert.DeserializeObject<Dictionary<string, Place>>(request.Content.ReadAsStringAsync().Result);
            Assert.AreEqual(1, filteredPlaces.Count);
            var placeTmp = filteredPlaces.First().Value;
            Assert.AreEqual(secondPlace.Id, placeTmp.Id);

            request = ListPlaceProductByPlace(client, secondPlace.Id);
            Assert.AreEqual(HttpStatusCode.OK, request.StatusCode, request.Content.ReadAsStringAsync().Result);
            placeProducts2 = JsonConvert.DeserializeObject<List<PlaceProductWithPlace>>(request.Content.ReadAsStringAsync().Result);
            Assert.AreEqual(1, placeProducts2.Count);
            var prod = placeProducts2.FirstOrDefault(pp => pp.Product.Category == "vac");
            Assert.IsNotNull(prod);
            Assert.AreEqual(99M, prod.Price);


            request = CreateProduct(client, new Product()
            {
                Name = "Vakcína 3",
                Description = "Vakcína 3",
                DefaultPrice = 98M,
                DefaultPriceCurrency = "EUR",
                Category = "vac",
                All = false,
                InsuranceOnly = false,
            });
            Assert.AreEqual(HttpStatusCode.OK, request.StatusCode, request.Content.ReadAsStringAsync().Result);
            var pr3 = JsonConvert.DeserializeObject<Product>(request.Content.ReadAsStringAsync().Result);

            request = InsertOrUpdatePlaceProduct(client, new PlaceProduct()
            {
                PlaceId = secondPlace.Id,
                PlaceProviderId = pp.PlaceProviderId,
                CustomPrice = false,
                From = DateTimeOffset.Now.AddDays(1),
                Until = DateTimeOffset.Now.AddDays(8),
                ProductId = pr3.Id,
            });
            request = ListFiltered(client, "vac-doctor", "all");
            Assert.AreEqual(HttpStatusCode.OK, request.StatusCode, request.Content.ReadAsStringAsync().Result);
            filteredPlaces = JsonConvert.DeserializeObject<Dictionary<string, Place>>(request.Content.ReadAsStringAsync().Result);
            Assert.AreEqual(1, filteredPlaces.Count);
            placeTmp = filteredPlaces.First().Value;
            Assert.AreEqual(secondPlace.Id, placeTmp.Id);

            request = ListFiltered(client, "vac-self", "all");
            Assert.AreEqual(HttpStatusCode.OK, request.StatusCode, request.Content.ReadAsStringAsync().Result);
            filteredPlaces = JsonConvert.DeserializeObject<Dictionary<string, Place>>(request.Content.ReadAsStringAsync().Result);
            Assert.AreEqual(1, filteredPlaces.Count);
            placeTmp = filteredPlaces.First().Value;
            Assert.AreEqual(secondPlace.Id, placeTmp.Id);


            request = ListPlaceProductByPlace(client, secondPlace.Id);
            Assert.AreEqual(HttpStatusCode.OK, request.StatusCode, request.Content.ReadAsStringAsync().Result);
            placeProducts2 = JsonConvert.DeserializeObject<List<PlaceProductWithPlace>>(request.Content.ReadAsStringAsync().Result);
            Assert.AreEqual(2, placeProducts2.Count);

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

            request = FinalDataExport(client, 0, 100);
            Assert.AreEqual(HttpStatusCode.BadRequest, request.StatusCode);

            request = ListUserInvites(client);
            Assert.AreEqual(HttpStatusCode.OK, request.StatusCode, request.Content.ReadAsStringAsync().Result);
            invites = JsonConvert.DeserializeObject<List<Invitation>>(request.Content.ReadAsStringAsync().Result);
            Assert.AreEqual(1, invites.Count);
            request = ProcessInvitation(client, invites[0].InvitationId, true);
            Assert.AreEqual(HttpStatusCode.OK, request.StatusCode, request.Content.ReadAsStringAsync().Result);


            client.DefaultRequestHeaders.Clear();
            client.DefaultRequestHeaders.Add("Authorization", $"Bearer {medicLabPersonToken}");

            request = FinalDataExport(client, 0, 100);
            Assert.AreEqual(HttpStatusCode.BadRequest, request.StatusCode);

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
            var place = places.Values.FirstOrDefault(p => p.Name == "Odberné miesto 2");
            request = ListDaySlotsByPlace(client, place.Id);
            Assert.AreEqual(HttpStatusCode.OK, request.StatusCode, request.Content.ReadAsStringAsync().Result);
            var days = Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, Slot1Day>>(request.Content.ReadAsStringAsync().Result);
            Assert.IsTrue(days.Count > 0);
            if (DateTime.Now.Hour >= 23) return;
            var day = days.Last().Value;
            request = ListHourSlotsByPlaceAndDaySlotId(client, place.Id, day.SlotId.ToString());
            Assert.AreEqual(HttpStatusCode.OK, request.StatusCode, request.Content.ReadAsStringAsync().Result);
            var hours = Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, Slot1Hour>>(request.Content.ReadAsStringAsync().Result);
            Assert.IsTrue(hours.Count > 0);

            var hour = hours.Last().Value;
            request = ListMinuteSlotsByPlaceAndHourSlotId(client, place.Id, hour.SlotId.ToString());
            Assert.AreEqual(HttpStatusCode.OK, request.StatusCode, request.Content.ReadAsStringAsync().Result);
            var minutes = Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, Slot5Min>>(request.Content.ReadAsStringAsync().Result);
            Assert.IsTrue(minutes.Count > 0);

            // test free places

            request = ListFiltered(client, "vac-doctor", "all");
            Assert.AreEqual(HttpStatusCode.OK, request.StatusCode, request.Content.ReadAsStringAsync().Result);
            filteredPlaces = JsonConvert.DeserializeObject<Dictionary<string, Place>>(request.Content.ReadAsStringAsync().Result);
            Assert.AreEqual(1, filteredPlaces.Count);
            //Assert.AreEqual(120, filteredPlaces.Values.First().AvailableSlotsToday);
            var placeId = filteredPlaces.First().Value.Id;
            Assert.AreEqual(placeId, place.Id);
            var minute = minutes.Values.Last();
            var registered = RegisterTestVisitors(client, placeId, minute.SlotId, pr1.Id);
            Assert.IsTrue(registered.Count >= 2);
            request = ListFiltered(client, "vac-doctor", "all");
            Assert.AreEqual(HttpStatusCode.OK, request.StatusCode, request.Content.ReadAsStringAsync().Result);
            filteredPlaces = JsonConvert.DeserializeObject<Dictionary<string, Place>>(request.Content.ReadAsStringAsync().Result);
            Assert.AreEqual(1, filteredPlaces.Count);
            Assert.AreEqual(2, filteredPlaces.Values.First().Registrations);
            if (DateTime.Now.Hour < 20 && DateTime.Now.Hour > 1)
            {
                Assert.AreEqual(118, filteredPlaces.Values.First().AvailableSlotsToday);
            }
            client.DefaultRequestHeaders.Clear();
            client.DefaultRequestHeaders.Add("Authorization", $"Bearer {medicPersonToken}");

            request = FinalDataExport(client, 0, 100);
            Assert.AreEqual(HttpStatusCode.BadRequest, request.StatusCode);

            // test authentication when user has not yet been assigned to the place
            var user1 = registered[0];
            request = GetVisitor(client, user1.Id.ToString());
            Assert.AreEqual(HttpStatusCode.BadRequest, request.StatusCode, request.Content.ReadAsStringAsync().Result);
            request = GetVisitorByRC(client, user1.RC);
            Assert.AreEqual(HttpStatusCode.BadRequest, request.StatusCode, request.Content.ReadAsStringAsync().Result);

            client.DefaultRequestHeaders.Clear();
            client.DefaultRequestHeaders.Add("Authorization", $"Bearer {adminToken}");

            request = FinalDataExport(client, 0, 100);
            Assert.AreEqual(HttpStatusCode.BadRequest, request.StatusCode);

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
                },
                new PersonAllocation()
                {
                    Start = DateTimeOffset.Now,
                    End = DateTimeOffset.Now.AddDays(1),
                    Role = Groups.DataExporter,
                    User = medicLabPersonEmail
                }
            };
            request = AllocatePersonsToPlace(client, allocations, firstPlace.Id);
            Assert.AreEqual(HttpStatusCode.OK, request.StatusCode, request.Content.ReadAsStringAsync().Result);

            // login again, check tokens

            client.DefaultRequestHeaders.Clear();
            client.DefaultRequestHeaders.Add("Authorization", $"Bearer {medicPersonToken}");

            request = FinalDataExport(client, 0, 100);
            Assert.AreEqual(HttpStatusCode.BadRequest, request.StatusCode);

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

            request = FinalDataExport(client, 0, 100);
            Assert.AreEqual(HttpStatusCode.BadRequest, request.StatusCode);

            request = SetPlaceProvider(client, pp.PlaceProviderId);
            Assert.AreEqual(HttpStatusCode.OK, request.StatusCode, request.Content.ReadAsStringAsync().Result);
            medicLabPersonToken = request.Content.ReadAsStringAsync().Result;
            client.DefaultRequestHeaders.Clear();
            client.DefaultRequestHeaders.Add("Authorization", $"Bearer {medicLabPersonToken}");

            request = FinalDataExport(client, 0, 100);
            Assert.AreEqual(HttpStatusCode.OK, request.StatusCode, request.Content.ReadAsStringAsync().Result);

            handler = new JwtSecurityTokenHandler();
            tokenS = handler.ReadToken(medicLabPersonToken) as JwtSecurityToken;
            jti = tokenS.Claims.FirstOrDefault(claim => claim.Type == "Role" && claim.Value == Groups.MedicLab);
            Assert.IsNotNull(jti);
            jti = tokenS.Claims.FirstOrDefault(claim => claim.Type == "Role" && claim.Value == Groups.DataExporter);
            Assert.IsNotNull(jti);
            var jtis = tokenS.Claims.Where(claim => claim.Type == "Role").ToArray();
            Assert.AreEqual(2, jtis.Length);
            jti = tokenS.Claims.FirstOrDefault(claim => claim.Type == Token.Claims.PlaceProvider);
            Assert.IsNotNull(jti);
            Assert.AreEqual(pp.PlaceProviderId, jti.Value);

            // perform fetch data by personal number, by reg code, assign the test set

            client.DefaultRequestHeaders.Clear();
            client.DefaultRequestHeaders.Add("Authorization", $"Bearer {medicPersonToken}");

            request = FinalDataExport(client, 0, 100);
            Assert.AreEqual(HttpStatusCode.BadRequest, request.StatusCode);

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

            request = FinalDataExport(client, 0, 100);
            Assert.AreEqual(HttpStatusCode.OK, request.StatusCode, request.Content.ReadAsStringAsync().Result);
            noEmailSender.Data.Clear();

            var iVisitor = web.Server.Services.GetService<CovidMassTesting.Repository.Interface.IVisitorRepository>();
            var visitor1 = iVisitor.GetVisitor(registered[0].Id).Result;
            visitor1.TestingTime = DateTimeOffset.Now.AddMinutes(-16);
            await iVisitor.SetVisitor(visitor1, false);

            // TEST mark as sick
            request = SetResult(client, test1, TestResult.PositiveWaitingForCertificate);
            Assert.AreEqual(HttpStatusCode.OK, request.StatusCode, request.Content.ReadAsStringAsync().Result);
            var testResult = Newtonsoft.Json.JsonConvert.DeserializeObject<Result>(request.Content.ReadAsStringAsync().Result);
            Assert.AreEqual(TestResult.PositiveWaitingForCertificate, testResult.State);


            if (configuration["SendResultsThroughQueue"] == "1")
            {
                Assert.AreEqual(0, noEmailSender.Data.Count);
            }

            testResult.Time = DateTimeOffset.Now.AddMinutes(-15);
            await iVisitor.SetResultObject(testResult, false);
            await iVisitor.ProcessSingle();
            Assert.AreEqual(2, noEmailSender.Data.Count);
            await Task.Delay(1000);
            var tuple = noEmailSender.Data.Values.First();
            Assert.AreEqual(1, noEmailSender.Data.Values.SelectMany(v => v.attachments).Count());

#if DEBUG
            var file = noEmailSender.Data.Values.SelectMany(v => v.attachments).First();
            File.WriteAllBytes($"d:/covid/{file.Filename}", Convert.FromBase64String(file.Content));
#endif

            // TEST mark as wrong code
            request = SetResult(client, test1, TestResult.PositiveCertificateTaken);
            Assert.AreEqual(HttpStatusCode.BadRequest, request.StatusCode);

            var visitor2 = iVisitor.GetVisitor(registered[1].Id).Result;
            visitor2.TestingTime = DateTimeOffset.Now.AddMinutes(-16);
            await iVisitor.SetVisitor(visitor2, false);


            // TEST mark as sick

            noEmailSender.Data.Clear();

            request = SetResult(client, test2, TestResult.NegativeWaitingForCertificate);

            await Task.Delay(1000);
            if (configuration["SendResultsThroughQueue"] != "1")
            {
                Assert.AreEqual(1, noEmailSender.Data.Count);
            }

            Assert.AreEqual(HttpStatusCode.OK, request.StatusCode, request.Content.ReadAsStringAsync().Result);
            testResult = Newtonsoft.Json.JsonConvert.DeserializeObject<Result>(request.Content.ReadAsStringAsync().Result);
            Assert.AreEqual(TestResult.NegativeWaitingForCertificate, testResult.State);
            client.DefaultRequestHeaders.Clear();
            var idWithSlashes = registered[0].Id.ToString();
            request = PublicGetTestResult(client, idWithSlashes.Substring(0, 3) + "‐" + idWithSlashes.Substring(3, 3) + " " + idWithSlashes.Substring(6), registered[0].RC.Substring(6, 4));
            Assert.AreEqual(HttpStatusCode.OK, request.StatusCode, request.Content.ReadAsStringAsync().Result);
            testResult = Newtonsoft.Json.JsonConvert.DeserializeObject<Result>(request.Content.ReadAsStringAsync().Result);
            Assert.AreEqual(TestResult.PositiveCertificateTaken, testResult.State);
            Assert.IsNotNull(testResult.VerificationId);

            request = PublicGetTestResult(client, registered[1].Id.ToString(), registered[1].RC.Substring(6, 4));
            Assert.AreEqual(HttpStatusCode.OK, request.StatusCode, request.Content.ReadAsStringAsync().Result);
            testResult = Newtonsoft.Json.JsonConvert.DeserializeObject<Result>(request.Content.ReadAsStringAsync().Result);
            if (configuration["SendResultsThroughQueue"] == "1")
            {
                Assert.AreEqual(TestResult.TestIsBeingProcessing, testResult.State);
            }
            else
            {
                Assert.AreEqual(TestResult.NegativeCertificateTaken, testResult.State);
            }
            visitor1 = iVisitor.GetVisitor(registered[0].Id).Result;

            request = VerifyResult(client, visitor1.VerificationId);
            Assert.AreEqual(HttpStatusCode.OK, request.StatusCode, request.Content.ReadAsStringAsync().Result);
            var resultData = Newtonsoft.Json.JsonConvert.DeserializeObject<VerificationData>(request.Content.ReadAsStringAsync().Result);
            Assert.AreEqual(TestResult.PositiveWaitingForCertificate, resultData.Result);
            Assert.AreEqual("Drahá vakcína", resultData.Product);


            iVisitor.ProcessSingle().Wait(); // send email, mark with verification id

            visitor2 = iVisitor.GetVisitor(registered[1].Id).Result;
            request = VerifyResult(client, visitor2.VerificationId);
            Assert.AreEqual(HttpStatusCode.OK, request.StatusCode, request.Content.ReadAsStringAsync().Result);
            resultData = Newtonsoft.Json.JsonConvert.DeserializeObject<VerificationData>(request.Content.ReadAsStringAsync().Result);
            Assert.AreEqual(TestResult.NegativeWaitingForCertificate, resultData.Result);
            Assert.AreEqual("Drahá vakcína", resultData.Product);


        }


        [Test]
        public void TestPDF()
        {
            DropDatabase();

            using var web = new MockWebApp(AppSettings);
            var client = web.CreateClient();


            var visitorRepository = web.Server.Services.GetService<CovidMassTesting.Repository.Interface.IVisitorRepository>();

            var t = new DateTimeOffset(2021, 1, 17, 13, 14, 20, TimeSpan.Zero);

            var visitor = new Visitor()
            {
                FirstName = "X",
                LastName = "Y",
                RC = "1234567890",
                Language = "en-US",
                TestingTime = t,
                Result = TestResult.PositiveWaitingForCertificate,
            };
            var html = visitorRepository.GenerateResultHTML(visitor, "Nitra", "Bratislavská 1, Nitra", "Antigénový test", "SD BIOSENSOR, Inc.; Roche, STANDARD Q COVID-19 Ag Test", Guid.NewGuid().ToString());
            Assert.IsTrue(html.Contains("X Y"));
            Assert.IsTrue(html.Contains("Sunday, January 17, 2021 2:14 PM"));

            var visitor2 = new Visitor()
            {
                FirstName = "X",
                LastName = "Y",
                RC = "1234567890",
                Language = "sk-SK",
                TestingTime = t,
                Result = TestResult.PositiveWaitingForCertificate,
            };
            var html2 = visitorRepository.GenerateResultHTML(visitor2, "Nitra", "Bratislavská 1, Nitra", "Antigénový test", "SD BIOSENSOR, Inc.; Roche, STANDARD Q COVID-19 Ag Test", Guid.NewGuid().ToString());
            Assert.IsTrue(html2.Contains("X Y"));
            Assert.IsTrue(html2.Contains("nedeľa 17. janu&#225;ra 2021 14:14"));

            var pdf = visitorRepository.GenerateResultPDF(visitor, "Nitra", "Bratislavská 1, Nitra", "Antigénový test", "SD BIOSENSOR, Inc.; Roche, STANDARD Q COVID-19 Ag Test", Guid.NewGuid().ToString(), true, "Oversight");
            Assert.IsTrue(pdf.Length > 100);
#if DEBUG
            File.WriteAllBytes("d:/covid/test-pdf.pdf", pdf);
#endif

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
        [Test]
        public virtual void TestText()
        {
            Assert.AreEqual("Ludovit", CovidMassTesting.Helpers.Text.RemoveDiacritism("Ľudovít"));
            Assert.AreEqual("+lsctzyaie LCZI", CovidMassTesting.Helpers.Text.RemoveDiacritism("+ľščťžýáíé ĽČŽÍ"));
        }


        [Test]
        public void SendEmailTest()
        {
            using var web = new MockWebApp(AppSettings);
            var client = web.CreateClient();

            var emailSender = web.Server.Services.GetService<CovidMassTesting.Controllers.Email.IEmailSender>();
            var noEmailSender = emailSender as CovidMassTesting.Controllers.Email.NoEmailSender;
            noEmailSender?.Data.Clear();
            var attachment = new SendGrid.Helpers.Mail.Attachment();
            attachment.Content = Convert.ToBase64String(Encoding.UTF8.GetBytes("Hello"));
            attachment.Filename = "test.pdf";
            attachment.Type = "application/pdf";

            emailSender.SendEmail(
                $"Test {DateTimeOffset.Now.ToString("f")}",
                "ludkosk@gmail.com",
                "CovidL",
                new CovidMassTesting.Model.Email.InvitationEmail("sk", configuration["FrontedURL"], configuration["EmailSupport"], configuration["PhoneSupport"])
                {
                    Name = "Ludo",
                    IsSK = true,
                    CompanyName = "company",
                    Password = "test",
                    InviterName = "Inviter name",
                    Roles = new string[] { "Test", "test2" },
                    WebPath = configuration["FrontedURL"]
                },
                new List<SendGrid.Helpers.Mail.Attachment>() { attachment }
            ).Wait();
            Task.Delay(1000).Wait();
            Assert.AreEqual(1, noEmailSender?.Data.Count);
        }
        [Test]
        public void SendSMSTest()
        {
            using var web = new MockWebApp(AppSettings);
            var client = web.CreateClient();

            var smsSender = web.Server.Services.GetService<CovidMassTesting.Controllers.SMS.ISMSSender>();
            var noSMSSender = smsSender as CovidMassTesting.Controllers.SMS.MockSMSSender;
            noSMSSender?.Data.Clear();

            smsSender.SendSMS(
                $"+420776082012",
                new CovidMassTesting.Model.SMS.Message($"SMS Test {DateTimeOffset.Now.ToString("f")}")
            ).Wait();
            Assert.AreEqual(1, noSMSSender?.Data.Count);
        }

        [Test]
        public void TestLimits()
        {
            using var web = new MockWebApp(AppSettings);
            var client = web.CreateClient();
            var iVisitor = web.Server.Services.GetService<CovidMassTesting.Repository.Interface.IVisitorRepository>();
            var iPlace = web.Server.Services.GetService<CovidMassTesting.Repository.Interface.IPlaceRepository>();
            var iSlot = web.Server.Services.GetService<CovidMassTesting.Repository.Interface.ISlotRepository>();

            var tick = DateTimeOffset.Parse("2020-01-01T00:00:00+00:00");
            iPlace.SetPlace(new Place()
            {
                Id = "123",
                OtherLimitations = new List<PlaceLimitation>()
                {
                    new PlaceLimitation()
                    {
                        From = tick,
                        HourLimit = 0,
                        PlaceId = "123",
                        Until = tick.AddHours(1),
                    }
                }
            });

            iSlot.Add(new Slot1Day() { PlaceId = "123", Time = tick });
            iSlot.Add(new Slot1Hour() { PlaceId = "123", Time = tick, DaySlotId = tick.UtcTicks });
            iSlot.Add(new Slot5Min() { PlaceId = "123", Time = tick, HourSlotId = tick.UtcTicks });

            var smsSender = web.Server.Services.GetService<CovidMassTesting.Controllers.SMS.ISMSSender>();
            var noSMSSender = smsSender as CovidMassTesting.Controllers.SMS.MockSMSSender;
            Visitor visitor;
            try
            {
                var vis = iVisitor.Register(visitor = new Visitor()
                {
                    FirstName = "L",
                    LastName = "S",
                    Language = "en",
                    ChosenPlaceId = "123",
                    ChosenSlot = tick.UtcTicks,
                    RC = " 845123/0007",
                    BirthDayDay = 23,
                    BirthDayMonth = 01,
                    BirthDayYear = 1985,
                    PersonType = "child",
                    Result = TestResult.NegativeWaitingForCertificate,
                    Email = "test@test.com",
                    Phone = "+421907723428"
                }, "", true).Result;
                Assert.Fail("Registration limit should be triggered");
            }
            catch
            {

            }


        }

        /*
        [Test]
        public void TestFixYear()
        {
            using var web = new MockWebApp(AppSettings);
            var client = web.CreateClient();
            var iVisitor = web.Server.Services.GetService<CovidMassTesting.Repository.Interface.IVisitorRepository>();
            var iPlace = web.Server.Services.GetService<CovidMassTesting.Repository.Interface.IPlaceRepository>();
            var iSlot = web.Server.Services.GetService<CovidMassTesting.Repository.Interface.ISlotRepository>();
            iPlace.SetPlace(new Place()
            {
                Id = "123",
            });

            var tick = DateTimeOffset.Parse("2020-01-01");
            iSlot.Add(new Slot1Day() { PlaceId = "123", Time = tick });
            iSlot.Add(new Slot1Hour() { PlaceId = "123", Time = tick });
            iSlot.Add(new Slot5Min() { PlaceId = "123", Time = tick });

            var smsSender = web.Server.Services.GetService<CovidMassTesting.Controllers.SMS.ISMSSender>();
            var noSMSSender = smsSender as CovidMassTesting.Controllers.SMS.MockSMSSender;
            Visitor visitor;
            var vis = iVisitor.Add(visitor = new Visitor()
            {
                FirstName = "L",
                LastName = "S",
                Language = "en",
                ChosenPlaceId = "123",
                ChosenSlot = tick.UtcTicks,
                RC = " 845123/0007",
                BirthDayDay = 23,
                BirthDayMonth = 01,
                BirthDayYear = 1985,
                PersonType = "child",
                Result = TestResult.NegativeWaitingForCertificate,
                Email = "test@test.com",
                Phone = "+421907723428"
            }).Result;

            iVisitor.ConnectVisitorToTest(vis.Id, "12345");
            iVisitor.SetTestResult("12345", TestResult.NegativeWaitingForCertificate);

            Assert.AreEqual(1984, visitor.BirthDayYear);// check if fix with insert is done

            visitor.BirthDayYear = 1985;//corrupt
            Assert.AreEqual(TestResult.NegativeWaitingForCertificate, visitor.Result);
            Task.Delay(100).Wait();
            noSMSSender?.Data.Clear();
            Assert.AreEqual(1, iVisitor.FixBirthYear().Result);
            Task.Delay(100).Wait();

            //Assert.AreEqual("L S, 1984, AG test from 24.01.2021 is NEGATIVE. Download PDF certificate from: https://www.rychlejsie.sk/", noSMSSender.Data.Values.First().data.GetText());
            Assert.AreEqual(1, noSMSSender?.Data.Count);
            var sms = noSMSSender.Data.Values.First();
            Assert.IsTrue(sms.data.GetText().Contains("NEGATIVE"));
            Assert.IsTrue(sms.data.GetText().Contains(DateTime.Now.ToString("dd.MM.yyyy")));
            Assert.IsTrue(sms.data.GetText().Contains("L S"));
            Assert.IsTrue(sms.data.GetText().Contains("1984"));
        }/**/

        /*
        [Test]
        public void TestFixTestingTime()
        {
            using var web = new MockWebApp(AppSettings);
            var client = web.CreateClient();
            var iVisitor = web.Server.Services.GetService<CovidMassTesting.Repository.Interface.IVisitorRepository>();
            var iPlace = web.Server.Services.GetService<CovidMassTesting.Repository.Interface.IPlaceRepository>();
            var iSlot = web.Server.Services.GetService<CovidMassTesting.Repository.Interface.ISlotRepository>();
            iPlace.SetPlace(new Place()
            {
                Id = "123",
            });

            var tick = DateTimeOffset.Parse("2020-01-01");
            iSlot.Add(new Slot1Day() { PlaceId = "123", Time = tick });
            iSlot.Add(new Slot1Hour() { PlaceId = "123", Time = tick });
            iSlot.Add(new Slot5Min() { PlaceId = "123", Time = tick });

            var smsSender = web.Server.Services.GetService<CovidMassTesting.Controllers.SMS.ISMSSender>();
            var noSMSSender = smsSender as CovidMassTesting.Controllers.SMS.MockSMSSender;
            Visitor visitor;
            var vis = iVisitor.Add(visitor = new Visitor()
            {
                FirstName = "L",
                LastName = "S",
                Language = "en",
                ChosenPlaceId = "123",
                ChosenSlot = tick.UtcTicks,
                RC = " 845123/0007",
                BirthDayDay = 23,
                BirthDayMonth = 01,
                BirthDayYear = 1984,
                PersonType = "child",
                Result = TestResult.NegativeWaitingForCertificate,
                Email = "test@test.com",
                Phone = "+421907723428"
            }).Result;

            iVisitor.ConnectVisitorToTest(vis.Id, "12345");
            var vis1 = iVisitor.GetVisitor(vis.Id).Result;
            vis1.TestingTime = DateTimeOffset.Parse("2021-01-28");
            var new1 = iVisitor.SetVisitor(vis1, false).Result;
            iVisitor.SetTestResult("12345", TestResult.NegativeWaitingForCertificate);

            Assert.AreEqual(TestResult.NegativeWaitingForCertificate, visitor.Result);
            Task.Delay(100).Wait();
            noSMSSender?.Data.Clear();
            Assert.AreEqual(1, iVisitor.FixTestingTime().Result);
            Task.Delay(100).Wait();

            //Assert.AreEqual("L S, 1984, AG test from 24.01.2021 is NEGATIVE. Download PDF certificate from: https://www.rychlejsie.sk/", noSMSSender.Data.Values.First().data.GetText());
            Assert.AreEqual(1, noSMSSender?.Data.Count);
            var sms = noSMSSender.Data.Values.First();
            Assert.IsTrue(sms.data.GetText().Contains("NEGATIVE"));
            Assert.IsTrue(sms.data.GetText().Contains(DateTime.Now.ToString("dd.MM.yyyy")));
            Assert.IsTrue(sms.data.GetText().Contains("L S"));
            Assert.IsTrue(sms.data.GetText().Contains("1984"));
        }/**/
        [Test]
        public void TestDoubleTestInput()
        {
            using var web = new MockWebApp(AppSettings);
            var client = web.CreateClient();
            var iVisitor = web.Server.Services.GetService<CovidMassTesting.Repository.Interface.IVisitorRepository>();
            var iPlace = web.Server.Services.GetService<CovidMassTesting.Repository.Interface.IPlaceRepository>();
            var iSlot = web.Server.Services.GetService<CovidMassTesting.Repository.Interface.ISlotRepository>();
            iPlace.SetPlace(new Place()
            {
                Id = "123",
            });

            var tick = DateTimeOffset.Parse("2020-01-01");
            iSlot.Add(new Slot1Day() { PlaceId = "123", Time = tick });
            iSlot.Add(new Slot1Hour() { PlaceId = "123", Time = tick });
            iSlot.Add(new Slot5Min() { PlaceId = "123", Time = tick });

            var smsSender = web.Server.Services.GetService<CovidMassTesting.Controllers.SMS.ISMSSender>();
            var noSMSSender = smsSender as CovidMassTesting.Controllers.SMS.MockSMSSender;
            Visitor visitor;
            var vis1 = iVisitor.Add(visitor = new Visitor()
            {
                FirstName = "L",
                LastName = "S",
                Language = "en",
                ChosenPlaceId = "123",
                ChosenSlot = tick.UtcTicks,
                RC = " 845123/0007",
                BirthDayDay = 23,
                BirthDayMonth = 01,
                BirthDayYear = 1985,
                PersonType = "child",
                Result = TestResult.NegativeWaitingForCertificate,
                Email = "test@test.com",
                Phone = "+421907723428"
            }, true).Result;

            iVisitor.ConnectVisitorToTest(vis1.Id, "12345", "aa@bb.cc", "", "");
            iVisitor.SetTestResult("12345", TestResult.NegativeWaitingForCertificate, true);

            var vis2 = iVisitor.Add(visitor = new Visitor()
            {
                FirstName = "L",
                LastName = "S",
                Language = "en",
                ChosenPlaceId = "123",
                ChosenSlot = tick.UtcTicks,
                RC = " 845123/0018",
                BirthDayDay = 23,
                BirthDayMonth = 01,
                BirthDayYear = 1985,
                PersonType = "child",
                Result = TestResult.NegativeWaitingForCertificate,
                Email = "test@test.com",
                Phone = "+421907723428"
            }, true).Result;

            try
            {
                var result = iVisitor.ConnectVisitorToTest(vis2.Id, "12345", "aa@bb.cc", "", "").Result;
                Assert.Fail("ConnectVisitorToTest with duplicit testset id should throw exception");
            }
            catch (Exception exc)
            {
                Console.WriteLine(exc.Message);
            }

        }
        [Test]
        public async Task TestUploadEmployees()
        {
            using var web = new MockWebApp(AppSettings);
            var client = web.CreateClient();
            var users = configuration.GetSection("AdminUsers").Get<CovidMassTesting.Model.Settings.User[]>();
            var admin = users.First(u => u.Name == "Admin");

            var obj = new PlaceProvider()
            {
                VAT = "123",
                Web = "123",
                CompanyId = "123",
                CompanyName = "123, s.r.o.",
                Country = "SK",
                MainEmail = admin.Email,
                PrivatePhone = "+421 907 000000",
                MainContact = "Admin Person"
            };

            var request = PlaceProviderRegistration(client, obj);
            Assert.AreEqual(HttpStatusCode.OK, request.StatusCode, request.Content.ReadAsStringAsync().Result);
            var data = request.Content.ReadAsStringAsync().Result;
            var pp = JsonConvert.DeserializeObject<PlaceProvider>(data);


            request = AuthenticateUser(client, admin.Email, admin.Password);
            Assert.AreEqual(HttpStatusCode.OK, request.StatusCode, request.Content.ReadAsStringAsync().Result);
            var adminToken = request.Content.ReadAsStringAsync().Result;
            Assert.IsFalse(string.IsNullOrEmpty(adminToken));
            client.DefaultRequestHeaders.Add("Authorization", $"Bearer {adminToken}");

            var stream1 = new MemoryStream(Encoding.UTF8.GetBytes("Meno;Priezvisko;Dátum narodenia;Rodné číslo;Ulica a číslo domu;Súpisné číslo;Orientačné číslo;Miesto;Pošt.smer.č./miesto;e-mail;Telefónne číslo;Osobné číslo\n" +
                "Meno;Priezvisko;01/01/2000;0001010009;Ulica;1;2;Poprad;058 01;test@rychlejsie.sk;0903000000;100"
            ));
            using var formData = new MultipartFormDataContent();
            using var content = new StreamContent(stream1);
            formData.Add(content, "files", "upload.csv");
            var response = await client.PostAsync("Visitor/UploadEmployees", formData);
            response.EnsureSuccessStatusCode();
            var responseString = await response.Content.ReadAsStringAsync();

            Assert.AreEqual(responseString, "1");


            SetupDebugPlaces(client);
            var pr1 = SetupDebugProduct(client);

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

            request = RegisterWithCompanyRegistration(client, minute.SlotId, place.Id, "101", "0009", pr1.Id, "");
            // bad company id
            Assert.AreEqual(HttpStatusCode.BadRequest, request.StatusCode);

            request = RegisterWithCompanyRegistration(client, minute.SlotId, place.Id, "100", "0009", pr1.Id, "");

            Assert.AreEqual(HttpStatusCode.OK, request.StatusCode, request.Content.ReadAsStringAsync().Result);

            var visitor = JsonConvert.DeserializeObject<Visitor>(request.Content.ReadAsStringAsync().Result);
            Assert.IsNotNull(visitor);
            Assert.AreEqual(place.Id, visitor.ChosenPlaceId);
            Assert.AreEqual(minute.SlotId, visitor.ChosenSlot);
            Assert.AreEqual("0001010009", visitor.RC);

            var smsSender = web.Server.Services.GetService<CovidMassTesting.Controllers.SMS.ISMSSender>();
            var noSMSSender = smsSender as CovidMassTesting.Controllers.SMS.MockSMSSender;
            noSMSSender?.Data.Clear();

            var placeProviderRepository = web.Server.Services.GetService<CovidMassTesting.Repository.Interface.IPlaceProviderRepository>();
            var product = await placeProviderRepository.AddProduct(pp.PlaceProviderId, new Product()
            {
                Id = Guid.NewGuid().ToString(),
                All = false,
                Category = "ant",
                CollectInsurance = false,
                Name = "Product - selftest"
            });


            client.DefaultRequestHeaders.Clear();
            client.DefaultRequestHeaders.Add("Authorization", $"Bearer {adminToken}");

            request = RegisterEmployeeByDocumenter(client, "100", "ludovit@scholtz.sk", "+421907000000", DateTimeOffset.Now.AddDays(-1), product.Id, TestResult.PositiveWaitingForCertificate);
            Assert.AreEqual(HttpStatusCode.OK, request.StatusCode, request.Content.ReadAsStringAsync().Result);
            Assert.AreEqual(0, noSMSSender.Data.Count);


            stream1 = new MemoryStream(Encoding.UTF8.GetBytes("Meno;Priezvisko;Dátum narodenia;Rodné číslo;Ulica a číslo domu;Súpisné číslo;Orientačné číslo;Miesto;Pošt.smer.č./miesto;e-mail;Telefónne číslo;Osobné číslo\n" +
                "Meno2;Priezvisko2;01/01/2000;0001010009;Ulica;1;2;Poprad;058 01;test@rychlejsie.sk;0903000000;100"
            ));
            using var formData2 = new MultipartFormDataContent();
            using var content2 = new StreamContent(stream1);
            formData2.Add(content2, "files", "upload.csv");
            response = await client.PostAsync("Visitor/UploadEmployees", formData2);
            response.EnsureSuccessStatusCode();
            responseString = await response.Content.ReadAsStringAsync();

            Assert.AreEqual(responseString, "1");




            var dataexporter = users.First(u => u.Name == "DataExporter");
            response = InviteUserToPP(client, dataexporter.Email, "Person 2", "+421 907 000 000", "MyMessage");
            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode, response.Content.ReadAsStringAsync().Result);

            response = AuthenticateUser(client, dataexporter.Email, dataexporter.Password);
            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode, response.Content.ReadAsStringAsync().Result);
            var dataExporterToken = response.Content.ReadAsStringAsync().Result;

            client.DefaultRequestHeaders.Clear();
            client.DefaultRequestHeaders.Add("Authorization", $"Bearer {dataExporterToken}");

            request = CompanyRegistrationsExport(client, 0, 100);
            var stream = new MemoryStream();
            request.Content.CopyToAsync(stream).Wait();
            var resultExport = Encoding.UTF8.GetString(stream.ToArray());
            Assert.IsNotNull(resultExport);
            Assert.IsTrue(resultExport.Contains("Meno2"));
            Assert.IsTrue(resultExport.Contains("Priezvisko2"));
            Assert.IsTrue(resultExport.Contains("100"));

        }

        [Test]
        public async Task TestDeleteVisitors()
        {
            using var web = new MockWebApp(AppSettings);
            var client = web.CreateClient();
            var visitorRepository = web.Server.Services.GetService<CovidMassTesting.Repository.Interface.IVisitorRepository>();
            await visitorRepository.SetVisitor(new Visitor()
            {
                Id = 123,
                FirstName = "A",
                TestingTime = DateTimeOffset.Now.AddDays(-15),
                PlaceProviderId = "11",
            }, false);

            await visitorRepository.SetVisitor(new Visitor()
            {
                Id = 124,
                FirstName = "B",
                TestingTime = DateTimeOffset.Now.AddDays(-13),
                PlaceProviderId = "11",
            }, false);

            var ret = await visitorRepository.DeleteOldVisitors(14);
            Assert.AreEqual(1, ret);

            var all = await visitorRepository.ListAllVisitors("11");
            Assert.AreEqual(1, all.Count());
            Assert.AreEqual(124, all.FirstOrDefault().Id);
            Assert.AreEqual("B", all.FirstOrDefault().FirstName);
        }


        [Test]
        public async Task TestOffset()
        {
            Assert.AreEqual(new TimeSpan(1, 0, 0), DateTimeOffset.Parse("2021-03-24T00:00:00+00:00").GetLocalOffset());
            Assert.AreEqual(new TimeSpan(2, 0, 0), DateTimeOffset.Parse("2021-03-31T00:00:00+00:00").GetLocalOffset());

            Assert.AreEqual(new TimeSpan(1, 0, 0), DateTimeOffset.Parse("2021-03-24T00:00:00+01:00").GetLocalOffset());
            Assert.AreEqual(new TimeSpan(2, 0, 0), DateTimeOffset.Parse("2021-03-31T00:00:00+02:00").GetLocalOffset());

            Assert.AreEqual(637527456000000000, DateTimeOffset.Parse("2021-03-31T00:00:00+02:00").RoundDay());
            Assert.AreEqual(637527456000000000, DateTimeOffset.Parse("2021-03-31T00:00:00+02:00").RoundDay());
            Assert.AreEqual(DateTimeOffset.Parse("2021-03-31T00:00:00+00:00").UtcTicks, DateTimeOffset.Parse("2021-03-31T00:00:00+02:00").RoundDay());

            Assert.AreEqual(637527384000000000, DateTimeOffset.Parse("2021-03-31T00:00:00+00:00").RoundHour());
            Assert.AreEqual(637527384000000000, DateTimeOffset.Parse("2021-03-31T02:00:00+02:00").RoundHour());

            Assert.AreEqual(637527672000000000, DateTimeOffset.Parse("2021-03-31T10:00:00+02:00").RoundHour());
            Assert.AreEqual(637527672000000000, DateTimeOffset.Parse("2021-03-31T10:00:00+02:00").RoundMinute());

            Assert.AreEqual("2021-03-27T11:00:00.0000000+01:00", DateTimeOffset.Parse("2021-03-27T10:00:00+00:00").ToLocalTime().ToString("o"));
            Assert.AreEqual("2021-03-28T12:00:00.0000000+02:00", DateTimeOffset.Parse("2021-03-28T10:00:00+00:00").ToLocalTime().ToString("o"));

        }

        [Test]
        public async Task SlotRepositoryTest()
        {
            DropDatabase();

            using var web = new MockWebApp(AppSettings);
            var client = web.CreateClient();
            var slotRepository = web.Server.Services.GetService<CovidMassTesting.Repository.Interface.ISlotRepository>();
            var placeRepository = web.Server.Services.GetService<CovidMassTesting.Repository.Interface.IPlaceRepository>();

            await placeRepository.SetPlace(new Place()
            {
                Id = "123",
                Name = "123"
            });

            await slotRepository.Add(new Slot1Day()
            {
                Time = DateTimeOffset.Parse("2021-03-28T00:00:00+00:00"),
                Description = "28.3.",
                PlaceId = "123",
                Registrations = 5,
            });

            await slotRepository.Add(new Slot1Hour()
            {
                Time = DateTimeOffset.Parse("2021-03-28T14:00:00+00:00"),
                Description = "15:00 - 16:00",
                PlaceId = "123",
                Registrations = 5,
                TestingDayId = 637524864000000000,
                DaySlotId = 637524864000000000
            });
            await slotRepository.Add(new Slot1Hour()
            {
                Time = DateTimeOffset.Parse("2021-03-28T13:00:00+00:00"),
                Description = "14:00 - 15:00",
                PlaceId = "123",
                Registrations = 5,
                TestingDayId = 637524864000000000,
                DaySlotId = 637524864000000000
            });
            await slotRepository.Add(new Slot5Min()
            {
                Time = DateTimeOffset.Parse("2021-03-28T13:00:00+00:00"),
                Description = "14:00 - 14:05",
                PlaceId = "123",
                Registrations = 5,
                TestingDayId = 637524864000000000,
                HourSlotId = DateTimeOffset.Parse("2021-03-28T13:00:00+00:00").UtcTicks
            });
            await slotRepository.Add(new Slot5Min()
            {
                Time = DateTimeOffset.Parse("2021-03-28T13:05:00+00:00"),
                Description = "14:05 - 14:10",
                PlaceId = "123",
                Registrations = 5,
                TestingDayId = 637524864000000000,
                HourSlotId = DateTimeOffset.Parse("2021-03-28T13:00:00+00:00").UtcTicks
            });

            var users = configuration.GetSection("AdminUsers").Get<CovidMassTesting.Model.Settings.User[]>();
            var admin = users.First(u => u.Name == "Admin");

            var obj = new PlaceProvider()
            {
                VAT = "123",
                Web = "123",
                CompanyId = "123",
                CompanyName = "123, s.r.o.",
                Country = "SK",
                MainEmail = admin.Email,
                PrivatePhone = "+421907000000",
                MainContact = "Admin Person"
            };

            var request = PlaceProviderRegistration(client, obj);
            Assert.AreEqual(HttpStatusCode.OK, request.StatusCode, request.Content.ReadAsStringAsync().Result);
            var data = request.Content.ReadAsStringAsync().Result;
            var pp = JsonConvert.DeserializeObject<PlaceProvider>(data);
            request = AuthenticateUser(client, admin.Email, admin.Password);
            Assert.AreEqual(HttpStatusCode.OK, request.StatusCode, request.Content.ReadAsStringAsync().Result);
            var adminToken = request.Content.ReadAsStringAsync().Result;
            Assert.IsFalse(string.IsNullOrEmpty(adminToken));
            client.DefaultRequestHeaders.Add("Authorization", $"Bearer {adminToken}");

            var response = FixSlotIssues(client);
            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode, response.Content.ReadAsStringAsync().Result);
            var fixSlotIssuesData = JsonConvert.DeserializeObject<List<object>>(response.Content.ReadAsStringAsync().Result);
            Assert.AreEqual(4, fixSlotIssuesData.Count);

            Assert.AreEqual(1, (await slotRepository.ListDaySlotsByPlace("123")).Count());
            var hours = await slotRepository.ListHourSlotsByPlaceAndDaySlotId("123", 637524864000000000);
            Assert.AreEqual(2, hours.Count());

            foreach (var hour in hours)
            {
                Trace.WriteLine(hour.TimeInCET.ToString("o"));
            }
            //Assert.AreEqual(DateTimeOffset.Parse("2021-03-28T14:00:00+02:00").UtcTicks, 637525296000000000);
            Assert.AreEqual(DateTimeOffset.Parse("2021-03-28T14:00:00+02:00").UtcTicks, DateTimeOffset.Parse("2021-03-28T12:00:00+00:00").UtcTicks);
            var minutes = await slotRepository.ListMinuteSlotsByPlaceAndHourSlotId("123", DateTimeOffset.Parse("2021-03-28T14:00:00+02:00").UtcTicks);
            Assert.AreEqual(2, minutes.Count());

        }


        [Test]
        public async Task SlotRegisterTest()
        {
            DropDatabase();

            using var web = new MockWebApp(AppSettings);
            var client = web.CreateClient();
            var slotRepository = web.Server.Services.GetService<CovidMassTesting.Repository.Interface.ISlotRepository>();
            var placeRepository = web.Server.Services.GetService<CovidMassTesting.Repository.Interface.IPlaceRepository>();

            var users = configuration.GetSection("AdminUsers").Get<CovidMassTesting.Model.Settings.User[]>();
            var admin = users.First(u => u.Name == "Admin");

            var obj = new PlaceProvider()
            {
                VAT = "123",
                Web = "123",
                CompanyId = "123",
                CompanyName = "123, s.r.o.",
                Country = "SK",
                MainEmail = admin.Email,
                PrivatePhone = "+421907000000",
                MainContact = "Admin Person"
            };

            var request = PlaceProviderRegistration(client, obj);
            Assert.AreEqual(HttpStatusCode.OK, request.StatusCode, request.Content.ReadAsStringAsync().Result);
            var data = request.Content.ReadAsStringAsync().Result;
            var pp = JsonConvert.DeserializeObject<PlaceProvider>(data);
            request = AuthenticateUser(client, admin.Email, admin.Password);
            Assert.AreEqual(HttpStatusCode.OK, request.StatusCode, request.Content.ReadAsStringAsync().Result);
            var adminToken = request.Content.ReadAsStringAsync().Result;
            Assert.IsFalse(string.IsNullOrEmpty(adminToken));
            client.DefaultRequestHeaders.Add("Authorization", $"Bearer {adminToken}");
            var places = SetupDebugPlaces(client);
            var pr1 = SetupDebugProduct(client);


            // CheckSlotsDay1

            var actions = new TimeUpdate[]
            {

                new TimeUpdate()
                {
                    Date = DateTimeOffset.Now,
                    OpeningHoursTemplateId= 1,
                    PlaceId = "__ALL__",
                    Type = "set"
                },/**/
                new TimeUpdate()
                {
                    Date = DateTimeOffset.Now.AddDays(1),
                    OpeningHoursTemplateId= 1,
                    PlaceId = "__ALL__",
                    Type = "set"
                },
            };

            request = ListScheduledDays(client);
            Assert.AreEqual(HttpStatusCode.OK, request.StatusCode, request.Content.ReadAsStringAsync().Result);
            var daysData = Newtonsoft.Json.JsonConvert.DeserializeObject<List<DayTimeManagement>>(request.Content.ReadAsStringAsync().Result);
            Assert.AreEqual(0, daysData.Count);

            request = ScheduleOpenningHours(client, actions);

            var day = DateTimeOffset.Now.RoundDay();

            foreach (var place in places)
            {
                var hours = await slotRepository.ListHourSlotsByPlaceAndDaySlotId(place.Id, day);

                switch (place.OpeningHoursWorkDay)
                {
                    case "20:00-23:55":
                        Assert.AreEqual(4, hours.Count());
                        break;
                    case "21:00-23:55":
                        Assert.AreEqual(3, hours.Count());
                        break;
                    case "23:00-23:55":
                        Assert.AreEqual(1, hours.Count());
                        break;
                }
                foreach (var hour in hours)
                {
                    Assert.AreEqual(day, hour.DaySlotId);
                }
            }/**/

            day = DateTimeOffset.Now.AddDays(1).RoundDay();
            foreach (var place in places)
            {
                var hours = await slotRepository.ListHourSlotsByPlaceAndDaySlotId(place.Id, day);

                switch (place.OpeningHoursWorkDay)
                {
                    case "20:00-23:55":
                        Assert.AreEqual(4, hours.Count());
                        break;
                    case "21:00-23:55":
                        Assert.AreEqual(3, hours.Count());
                        break;
                    case "23:00-23:55":
                        Assert.AreEqual(1, hours.Count());
                        break;
                }
                foreach (var hour in hours)
                {
                    Assert.AreEqual(day, hour.DaySlotId);
                }
            }

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

#endif