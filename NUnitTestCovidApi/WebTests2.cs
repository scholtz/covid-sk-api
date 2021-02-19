using CovidMassTesting.Helpers;
using CovidMassTesting.Model;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
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
    public class WebTests2 : BaseApiMethods
    {
        public virtual string AppSettings { get; set; } = "appsettings2.json";
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
            using var web = new Tests.MockWebApp(AppSettings);
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
            for (var i = 0; i < 99; i++)
            {
                pass = Encoding.ASCII.GetBytes($"{pass}{cohash}").GetSHA256Hash();
            }
            pass = Encoding.ASCII.GetBytes($"{pass}{rand}").GetSHA256Hash();
            request = DropDatabase(client, pass);
            var dataDeleted = request.Content.ReadAsStringAsync().Result;
            Assert.AreEqual(HttpStatusCode.OK, request.StatusCode, dataDeleted);
            Console.WriteLine($"cleared {dataDeleted} items");
        }

        protected List<Visitor> RegisterTestVisitors(HttpClient client, string placeId, long slotId, string productId)
        {
            var Registered = new List<Visitor>();
            var visitor1 = new Visitor()
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
            if (result.StatusCode != HttpStatusCode.OK)
            {
                throw new Exception("Unable to make visitor " + result.Content.ReadAsStringAsync().Result);
            }

            Registered.Add(Newtonsoft.Json.JsonConvert.DeserializeObject<Visitor>(result.Content.ReadAsStringAsync().Result));
            var visitor2 = new Visitor()
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
            if (result.StatusCode != HttpStatusCode.OK)
            {
                throw new Exception("Unable to make visitor");
            }

            Registered.Add(Newtonsoft.Json.JsonConvert.DeserializeObject<Visitor>(result.Content.ReadAsStringAsync().Result));

            return Registered;
        }
        protected List<Visitor> RegisterTestVisitors2(HttpClient client, string placeId, long slotId, string productId)
        {
            var Registered = new List<Visitor>();
            var visitor1 = new Visitor()
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
            if (result.StatusCode != HttpStatusCode.OK)
            {
                throw new Exception("Unable to make visitor " + result.Content.ReadAsStringAsync().Result);
            }

            Registered.Add(Newtonsoft.Json.JsonConvert.DeserializeObject<Visitor>(result.Content.ReadAsStringAsync().Result));
            var visitor2 = new Visitor()
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
            if (result.StatusCode != HttpStatusCode.OK)
            {
                throw new Exception("Unable to make visitor");
            }

            Registered.Add(Newtonsoft.Json.JsonConvert.DeserializeObject<Visitor>(result.Content.ReadAsStringAsync().Result));

            return Registered;
        }

        [Test]
        public async Task TwoPlaceProvidersInOneSystemTest()
        {
            DropDatabase();

            using var web = new Tests.MockWebApp(AppSettings);
            var client = web.CreateClient();
            var emailSender = web.Server.Services.GetService<CovidMassTesting.Controllers.Email.IEmailSender>();
            var noEmailSender = emailSender as CovidMassTesting.Controllers.Email.NoEmailSender;


            var response = PlaceProviderListPublic(client);
            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode, response.Content.ReadAsStringAsync().Result);
            var data = response.Content.ReadAsStringAsync().Result;
            var result = Newtonsoft.Json.JsonConvert.DeserializeObject<List<PlaceProvider>>(data);
            Assert.AreEqual(0, result.Count);
            var email1 = "place.provider1@scholtz.sk";

            var obj = new PlaceProvider()
            {
                VAT = "123",
                Web = "123",
                CompanyId = "123",
                CompanyName = "123, s.r.o.",
                Country = "SK",
                MainEmail = email1,
                PrivatePhone = "+421 907 000000",
                MainContact = "Admin Person"
            };

            response = PlaceProviderRegistration(client, obj);
            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode, response.Content.ReadAsStringAsync().Result);
            data = response.Content.ReadAsStringAsync().Result;
            var pp1 = JsonConvert.DeserializeObject<PlaceProvider>(data);
            Assert.AreEqual(obj.VAT, pp1.VAT);
            Assert.AreEqual(obj.Web, pp1.Web);
            Assert.AreEqual(obj.CompanyId, pp1.CompanyId);
            Assert.AreEqual(obj.CompanyName, pp1.CompanyName);
            Assert.AreEqual(obj.Country, pp1.Country);
            Assert.AreEqual(obj.MainEmail, pp1.MainEmail);
            Assert.AreEqual("+421907000000", pp1.PrivatePhone);
            Assert.AreEqual(obj.MainContact, pp1.MainContact);


            response = PlaceProviderListPublic(client);
            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode, response.Content.ReadAsStringAsync().Result);
            data = response.Content.ReadAsStringAsync().Result;
            result = Newtonsoft.Json.JsonConvert.DeserializeObject<List<PlaceProvider>>(data);
            Assert.AreEqual(1, result.Count);

            Assert.AreEqual(1, noEmailSender.Data.Count);
            var invitationEmail1 = noEmailSender.Data.First().Value.data as CovidMassTesting.Model.Email.InvitationEmail;
            noEmailSender.Data.Clear();


            var email2 = "place.provider2@scholtz.sk";

            var obj2 = new PlaceProvider()
            {
                VAT = "124",
                Web = "124",
                CompanyId = "124",
                CompanyName = "124, s.r.o.",
                Country = "SK",
                MainEmail = email2,
                PrivatePhone = "+421 907 000001",
                MainContact = "Admin Person 2"
            };

            response = PlaceProviderRegistration(client, obj2);
            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode, response.Content.ReadAsStringAsync().Result);
            data = response.Content.ReadAsStringAsync().Result;
            var pp2 = JsonConvert.DeserializeObject<PlaceProvider>(data);
            Assert.AreEqual(obj2.VAT, pp2.VAT);
            Assert.AreEqual(obj2.Web, pp2.Web);
            Assert.AreEqual(obj2.CompanyId, pp2.CompanyId);
            Assert.AreEqual(obj2.CompanyName, pp2.CompanyName);
            Assert.AreEqual(obj2.Country, pp2.Country);
            Assert.AreEqual(obj2.MainEmail, pp2.MainEmail);
            Assert.AreEqual("+421907000001", pp2.PrivatePhone);
            Assert.AreEqual(obj2.MainContact, pp2.MainContact);

            Assert.AreEqual(1, noEmailSender.Data.Count);
            var invitationEmail2 = noEmailSender.Data.First().Value.data as CovidMassTesting.Model.Email.InvitationEmail;
            noEmailSender.Data.Clear();


            // test limit 2 place provider registration per api configuration
            response = PlaceProviderRegistration(client, obj);
            Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode, response.Content.ReadAsStringAsync().Result);

            response = PlaceProviderListPublic(client);
            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode, response.Content.ReadAsStringAsync().Result);
            data = response.Content.ReadAsStringAsync().Result;
            result = Newtonsoft.Json.JsonConvert.DeserializeObject<List<PlaceProvider>>(data);
            Assert.AreEqual(2, result.Count);




            response = AuthenticateUser(client, email1, invitationEmail1.Password);
            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode, response.Content.ReadAsStringAsync().Result);
            var adminToken1 = response.Content.ReadAsStringAsync().Result;
            Assert.IsFalse(string.IsNullOrEmpty(adminToken1));

            var handler = new JwtSecurityTokenHandler();
            var tokenS = handler.ReadToken(adminToken1) as JwtSecurityToken;
            var jti = tokenS.Claims.FirstOrDefault(claim => claim.Type == "Role" && claim.Value == "PPAdmin");
            Assert.IsNotNull(jti);

            response = AuthenticateUser(client, email2, invitationEmail2.Password);
            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode, response.Content.ReadAsStringAsync().Result);
            var adminToken2 = response.Content.ReadAsStringAsync().Result;
            Assert.IsFalse(string.IsNullOrEmpty(adminToken1));

            handler = new JwtSecurityTokenHandler();
            tokenS = handler.ReadToken(adminToken1) as JwtSecurityToken;
            jti = tokenS.Claims.FirstOrDefault(claim => claim.Type == "Role" && claim.Value == "PPAdmin");
            Assert.IsNotNull(jti);

            client.DefaultRequestHeaders.Add("Authorization", $"Bearer {adminToken1}");

            response = FinalDataExport(client, 0, 100);
            Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode);

            // Test product management
            response = CreateProduct(client, new Product()
            {
                Name = "Antigen 1",
                Description = "Antigen 1",
                DefaultPrice = 0,
                DefaultPriceCurrency = "EUR",
                Category = "ant",
                All = true,
                InsuranceOnly = false,
            });
            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode, response.Content.ReadAsStringAsync().Result);
            var pr1 = JsonConvert.DeserializeObject<Product>(response.Content.ReadAsStringAsync().Result);
            Assert.AreEqual("Antigen 1", pr1.Name);
            Assert.AreEqual("Antigen 1", pr1.Description);

            client.DefaultRequestHeaders.Clear();
            client.DefaultRequestHeaders.Add("Authorization", $"Bearer {adminToken2}");

            response = CreateProduct(client, new Product()
            {
                Name = "Antigen 2",
                Description = "Antigen 2",
                DefaultPrice = 0,
                DefaultPriceCurrency = "EUR",
                Category = "ant",
                All = true,
                InsuranceOnly = false,
            });
            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode, response.Content.ReadAsStringAsync().Result);
            var pr2 = JsonConvert.DeserializeObject<Product>(response.Content.ReadAsStringAsync().Result);
            Assert.AreEqual("Antigen 2", pr2.Name);
            Assert.AreEqual("Antigen 2", pr2.Description);


            client.DefaultRequestHeaders.Clear();
            client.DefaultRequestHeaders.Add("Authorization", $"Bearer {adminToken1}");

            response = ListProducts(client);
            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode, response.Content.ReadAsStringAsync().Result);
            var prList = JsonConvert.DeserializeObject<List<Product>>(response.Content.ReadAsStringAsync().Result);
            Assert.AreEqual(1, prList.Count);

            Assert.AreEqual("Antigen 1", prList.First().Name);
            Assert.AreEqual("Antigen 1", prList.First().Description);

            // setup places
            var plObj1 = new Place()
            {
                Id = "myId1",
                Name = "Škola AA",
                Address = "Bratislavská 1, Pezinok",
                Lat = 48.28524902921143M,
                Lng = 17.256517410278324M,
                IsDriveIn = true,
                IsWalkIn = false,
                Registrations = 0,
                OpeningHoursWorkDay = "20:00-23:59",
                OpeningHoursOther1 = "23:45-23:59",
            };
            response = RegisterPlace(client, plObj1);
            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode, response.Content.ReadAsStringAsync().Result);
            var place1 = JsonConvert.DeserializeObject<Place>(response.Content.ReadAsStringAsync().Result);
            Assert.AreEqual(plObj1.Id, place1.Id);
            Assert.AreEqual(plObj1.Name, place1.Name);
            Assert.AreEqual(plObj1.Address, place1.Address);
            Assert.AreEqual(plObj1.IsDriveIn, place1.IsDriveIn);
            Assert.AreEqual(plObj1.IsWalkIn, place1.IsWalkIn);
            Assert.AreEqual(plObj1.OpeningHoursWorkDay, place1.OpeningHoursWorkDay);
            Assert.AreEqual(plObj1.OpeningHoursOther1, place1.OpeningHoursOther1);

            client.DefaultRequestHeaders.Clear();
            client.DefaultRequestHeaders.Add("Authorization", $"Bearer {adminToken2}");
            response = RegisterPlace(client, plObj1);
            // registration for already registered place id with diffrent place provider should show the error
            Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode, response.Content.ReadAsStringAsync().Result);


            var plObj2 = new Place()
            {
                Id = "myId2",
                Name = "Škola 2",
                Address = "Bratislavská 2, Pezinok",
                Lat = 48.2M,
                Lng = 17.2M,
                IsDriveIn = false,
                IsWalkIn = true,
                Registrations = 0,
                OpeningHoursWorkDay = "20:00-23:59",
                OpeningHoursOther1 = "23:45-23:59",
            };
            response = RegisterPlace(client, plObj2);
            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode, response.Content.ReadAsStringAsync().Result);
            var place2 = JsonConvert.DeserializeObject<Place>(response.Content.ReadAsStringAsync().Result);
            Assert.AreEqual(plObj2.Id, place2.Id);
            Assert.AreEqual(plObj2.Name, place2.Name);
            Assert.AreEqual(plObj2.Address, place2.Address);
            Assert.AreEqual(plObj2.IsDriveIn, place2.IsDriveIn);
            Assert.AreEqual(plObj2.IsWalkIn, place2.IsWalkIn);
            Assert.AreEqual(plObj2.OpeningHoursWorkDay, place2.OpeningHoursWorkDay);
            Assert.AreEqual(plObj2.OpeningHoursOther1, place2.OpeningHoursOther1);

            // ListFiltered

            response = ListFiltered(client, "all", "all");
            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode, response.Content.ReadAsStringAsync().Result);
            var filteredPlaces = JsonConvert.DeserializeObject<Dictionary<string, Place>>(response.Content.ReadAsStringAsync().Result);
            Assert.AreEqual(2, filteredPlaces.Count);

            // test productplace
            response = ListPlaceProductByPlaceProvider(client, pp1.PlaceProviderId);
            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode, response.Content.ReadAsStringAsync().Result);
            var placeProducts = JsonConvert.DeserializeObject<List<PlaceProduct>>(response.Content.ReadAsStringAsync().Result);
            Assert.AreEqual(1, placeProducts.Count);

            // invite tester pp1

            client.DefaultRequestHeaders.Clear();
            client.DefaultRequestHeaders.Add("Authorization", $"Bearer {adminToken1}");

            var medicPersonEmail1 = "person1tester@scholtz.sk";
            response = InviteUserToPP(client, medicPersonEmail1, "Person 1", "+421 907 000 001", "MyMessage");
            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode, response.Content.ReadAsStringAsync().Result);
            Assert.AreEqual(1, noEmailSender.Data.Count);
            invitationEmail1 = noEmailSender.Data.First().Value.data as CovidMassTesting.Model.Email.InvitationEmail;
            noEmailSender.Data.Clear();

            response = ListPPInvites(client);
            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode, response.Content.ReadAsStringAsync().Result);
            var invites = JsonConvert.DeserializeObject<List<Invitation>>(response.Content.ReadAsStringAsync().Result);
            Assert.AreEqual(2, invites.Count);

            var medicPersonPass = invitationEmail1.Password;
            response = AuthenticateUser(client, medicPersonEmail1, medicPersonPass);
            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode, response.Content.ReadAsStringAsync().Result);
            var medicPersonToken1 = response.Content.ReadAsStringAsync().Result;
            Assert.IsFalse(string.IsNullOrEmpty(medicPersonToken1));

            // invite lab user
            var medicLabPersonEmail1 = "person1lab@scholtz.sk";
            response = InviteUserToPP(client, medicLabPersonEmail1, "Person 2", "+421 907 000 000", "MyMessage");
            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode, response.Content.ReadAsStringAsync().Result);
            Assert.AreEqual(1, noEmailSender.Data.Count);
            invitationEmail1 = noEmailSender.Data.First().Value.data as CovidMassTesting.Model.Email.InvitationEmail;
            noEmailSender.Data.Clear();

            response = ListPPInvites(client);
            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode, response.Content.ReadAsStringAsync().Result);
            invites = JsonConvert.DeserializeObject<List<Invitation>>(response.Content.ReadAsStringAsync().Result);
            Assert.AreEqual(3, invites.Count);

            response = AuthenticateUser(client, medicLabPersonEmail1, invitationEmail1.Password);
            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode, response.Content.ReadAsStringAsync().Result);
            var medicLabPersonToken1 = response.Content.ReadAsStringAsync().Result;
            Assert.IsFalse(string.IsNullOrEmpty(medicLabPersonToken1));



            // invite tester pp2

            client.DefaultRequestHeaders.Clear();
            client.DefaultRequestHeaders.Add("Authorization", $"Bearer {adminToken2}");

            var medicPersonEmail2 = "person2tester@scholtz.sk";
            response = InviteUserToPP(client, medicPersonEmail2, "Person 1", "+421 907 000 001", "MyMessage");
            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode, response.Content.ReadAsStringAsync().Result);
            Assert.AreEqual(1, noEmailSender.Data.Count);
            invitationEmail2 = noEmailSender.Data.First().Value.data as CovidMassTesting.Model.Email.InvitationEmail;
            noEmailSender.Data.Clear();

            response = ListPPInvites(client);
            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode, response.Content.ReadAsStringAsync().Result);
            invites = JsonConvert.DeserializeObject<List<Invitation>>(response.Content.ReadAsStringAsync().Result);
            Assert.AreEqual(2, invites.Count);

            medicPersonPass = invitationEmail2.Password;
            response = AuthenticateUser(client, medicPersonEmail2, medicPersonPass);
            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode, response.Content.ReadAsStringAsync().Result);
            var medicPersonToken2 = response.Content.ReadAsStringAsync().Result;
            Assert.IsFalse(string.IsNullOrEmpty(medicPersonToken2));

            // invite lab user
            var medicLabPersonEmail2 = "person2lab@scholtz.sk";
            response = InviteUserToPP(client, medicLabPersonEmail2, "Person 2", "+421 907 000 000", "MyMessage");
            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode, response.Content.ReadAsStringAsync().Result);
            Assert.AreEqual(1, noEmailSender.Data.Count);
            invitationEmail2 = noEmailSender.Data.First().Value.data as CovidMassTesting.Model.Email.InvitationEmail;
            noEmailSender.Data.Clear();

            response = ListPPInvites(client);
            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode, response.Content.ReadAsStringAsync().Result);
            invites = JsonConvert.DeserializeObject<List<Invitation>>(response.Content.ReadAsStringAsync().Result);
            Assert.AreEqual(3, invites.Count);

            response = AuthenticateUser(client, medicLabPersonEmail2, invitationEmail2.Password);
            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode, response.Content.ReadAsStringAsync().Result);
            var medicLabPersonToken2 = response.Content.ReadAsStringAsync().Result;
            Assert.IsFalse(string.IsNullOrEmpty(medicLabPersonToken2));

            // open testing for public
            client.DefaultRequestHeaders.Clear();
            client.DefaultRequestHeaders.Add("Authorization", $"Bearer {adminToken1}");

            var actions = new TimeUpdate[]
            {
                new TimeUpdate()
                {
                    Date = DateTimeOffset.Now,
                    OpeningHoursTemplateId= 1,
                    PlaceId = "__ALL__",
                    Type = "set"
                },
            };

            response = ListScheduledDays(client);
            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode, response.Content.ReadAsStringAsync().Result);
            var daysData = Newtonsoft.Json.JsonConvert.DeserializeObject<List<DayTimeManagement>>(response.Content.ReadAsStringAsync().Result);
            Assert.AreEqual(0, daysData.Count);

            response = ScheduleOpenningHours(client, actions);
            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode, response.Content.ReadAsStringAsync().Result);

            response = ListScheduledDays(client);
            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode, response.Content.ReadAsStringAsync().Result);
            daysData = Newtonsoft.Json.JsonConvert.DeserializeObject<List<DayTimeManagement>>(response.Content.ReadAsStringAsync().Result);
            Assert.AreEqual(1, daysData.Count);

            client.DefaultRequestHeaders.Clear();
            client.DefaultRequestHeaders.Add("Authorization", $"Bearer {adminToken2}");

            actions = new TimeUpdate[]
            {
                new TimeUpdate()
                {
                    Date = DateTimeOffset.Now,
                    OpeningHoursTemplateId= 1,
                    PlaceId = "__ALL__",
                    Type = "set"
                },
            };

            response = ListScheduledDays(client);
            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode, response.Content.ReadAsStringAsync().Result);
            daysData = Newtonsoft.Json.JsonConvert.DeserializeObject<List<DayTimeManagement>>(response.Content.ReadAsStringAsync().Result);
            Assert.AreEqual(0, daysData.Count);

            response = ScheduleOpenningHours(client, actions);
            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode, response.Content.ReadAsStringAsync().Result);

            response = ListScheduledDays(client);
            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode, response.Content.ReadAsStringAsync().Result);
            daysData = Newtonsoft.Json.JsonConvert.DeserializeObject<List<DayTimeManagement>>(response.Content.ReadAsStringAsync().Result);
            Assert.AreEqual(1, daysData.Count);

            // accept invitations

            client.DefaultRequestHeaders.Clear();
            client.DefaultRequestHeaders.Add("Authorization", $"Bearer {medicPersonToken1}");

            response = ListUserInvites(client);
            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode, response.Content.ReadAsStringAsync().Result);
            invites = JsonConvert.DeserializeObject<List<Invitation>>(response.Content.ReadAsStringAsync().Result);
            Assert.AreEqual(1, invites.Count);
            response = ProcessInvitation(client, invites[0].InvitationId, true);
            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode, response.Content.ReadAsStringAsync().Result);


            client.DefaultRequestHeaders.Clear();
            client.DefaultRequestHeaders.Add("Authorization", $"Bearer {medicLabPersonToken1}");

            response = FinalDataExport(client, 0, 100);
            Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode);

            response = ListUserInvites(client);
            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode, response.Content.ReadAsStringAsync().Result);
            invites = JsonConvert.DeserializeObject<List<Invitation>>(response.Content.ReadAsStringAsync().Result);
            Assert.AreEqual(1, invites.Count);
            response = ProcessInvitation(client, invites[0].InvitationId, true);
            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode, response.Content.ReadAsStringAsync().Result);


            client.DefaultRequestHeaders.Clear();
            client.DefaultRequestHeaders.Add("Authorization", $"Bearer {medicPersonToken2}");

            response = ListUserInvites(client);
            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode, response.Content.ReadAsStringAsync().Result);
            invites = JsonConvert.DeserializeObject<List<Invitation>>(response.Content.ReadAsStringAsync().Result);
            Assert.AreEqual(1, invites.Count);
            response = ProcessInvitation(client, invites[0].InvitationId, true);
            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode, response.Content.ReadAsStringAsync().Result);


            client.DefaultRequestHeaders.Clear();
            client.DefaultRequestHeaders.Add("Authorization", $"Bearer {medicLabPersonToken2}");

            response = ListUserInvites(client);
            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode, response.Content.ReadAsStringAsync().Result);
            invites = JsonConvert.DeserializeObject<List<Invitation>>(response.Content.ReadAsStringAsync().Result);
            Assert.AreEqual(1, invites.Count);
            response = ProcessInvitation(client, invites[0].InvitationId, true);
            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode, response.Content.ReadAsStringAsync().Result);

            client.DefaultRequestHeaders.Clear();
            // check slots
            response = ListPlaces(client);
            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode, response.Content.ReadAsStringAsync().Result);
            var places = Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, Place>>(response.Content.ReadAsStringAsync().Result);
            Assert.AreEqual(2, places.Count);
            response = ListDaySlotsByPlace(client, place1.Id);
            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode, response.Content.ReadAsStringAsync().Result);
            var days = Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, Slot1Day>>(response.Content.ReadAsStringAsync().Result);
            Assert.IsTrue(days.Count > 0);
            var day = days.Last().Value;
            response = ListHourSlotsByPlaceAndDaySlotId(client, place1.Id, day.SlotId.ToString());
            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode, response.Content.ReadAsStringAsync().Result);
            var hours = Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, Slot1Hour>>(response.Content.ReadAsStringAsync().Result);
            Assert.IsTrue(hours.Count > 0);

            var hour = hours.Last().Value;
            response = ListMinuteSlotsByPlaceAndHourSlotId(client, place1.Id, hour.SlotId.ToString());
            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode, response.Content.ReadAsStringAsync().Result);
            var minutes = Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, Slot5Min>>(response.Content.ReadAsStringAsync().Result);
            Assert.IsTrue(minutes.Count > 0);

            // test free places

            response = ListFiltered(client, "ant-self", "all");
            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode, response.Content.ReadAsStringAsync().Result);
            filteredPlaces = JsonConvert.DeserializeObject<Dictionary<string, Place>>(response.Content.ReadAsStringAsync().Result);
            Assert.AreEqual(2, filteredPlaces.Count);
            //Assert.AreEqual(120, filteredPlaces.Values.First().AvailableSlotsToday);
            var minute = minutes.Values.Last();
            var registered1 = RegisterTestVisitors(client, place1.Id, minute.SlotId, pr1.Id);
            Assert.IsTrue(registered1.Count >= 2);

            var registered2 = RegisterTestVisitors2(client, place2.Id, minute.SlotId, pr2.Id);
            Assert.IsTrue(registered2.Count >= 2);
            var user3 = registered2[0];
            var user4 = registered2[1];
            response = ListFiltered(client, "ant-self", "all");
            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode, response.Content.ReadAsStringAsync().Result);
            filteredPlaces = JsonConvert.DeserializeObject<Dictionary<string, Place>>(response.Content.ReadAsStringAsync().Result);
            Assert.AreEqual(2, filteredPlaces.Count);
            Assert.AreEqual(2, filteredPlaces.Values.FirstOrDefault(p => p.Id == place1.Id).Registrations);
            if (DateTime.Now.Hour < 20 && DateTime.Now.Hour > 0)
            {
                Assert.AreEqual(158, filteredPlaces.Values.First().AvailableSlotsToday);
            }
            client.DefaultRequestHeaders.Clear();
            client.DefaultRequestHeaders.Add("Authorization", $"Bearer {medicPersonToken1}");

            response = FinalDataExport(client, 0, 100);
            Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode);

            // test authentication when user has not yet been assigned to the place
            var user1 = registered1[0];
            response = GetVisitor(client, user1.Id.ToString());
            Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode, response.Content.ReadAsStringAsync().Result);
            response = GetVisitorByRC(client, user1.RC);
            Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode, response.Content.ReadAsStringAsync().Result);

            client.DefaultRequestHeaders.Clear();
            client.DefaultRequestHeaders.Add("Authorization", $"Bearer {adminToken1}");

            response = FinalDataExport(client, 0, 100);
            Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode);

            // assign person to pp

            var allocations = new PersonAllocation[]
            {
                new PersonAllocation()
                {
                    Start = DateTimeOffset.Now,
                    End = DateTimeOffset.Now.AddDays(1),
                    Role = Groups.MedicTester,
                    User = medicPersonEmail1
                },
                new PersonAllocation()
                {
                    Start = DateTimeOffset.Now,
                    End = DateTimeOffset.Now.AddDays(1),
                    Role = Groups.MedicLab,
                    User = medicLabPersonEmail1
                },
                new PersonAllocation()
                {
                    Start = DateTimeOffset.Now,
                    End = DateTimeOffset.Now.AddDays(1),
                    Role = Groups.DataExporter,
                    User = medicLabPersonEmail1
                }
            };
            response = AllocatePersonsToPlace(client, allocations, place1.Id);
            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode, response.Content.ReadAsStringAsync().Result);


            client.DefaultRequestHeaders.Clear();
            client.DefaultRequestHeaders.Add("Authorization", $"Bearer {adminToken2}");

            response = FinalDataExport(client, 0, 100);
            Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode);

            // assign person to pp

            allocations = new PersonAllocation[]
            {
                new PersonAllocation()
                {
                    Start = DateTimeOffset.Now,
                    End = DateTimeOffset.Now.AddDays(1),
                    Role = Groups.MedicTester,
                    User = medicPersonEmail2
                },
                new PersonAllocation()
                {
                    Start = DateTimeOffset.Now,
                    End = DateTimeOffset.Now.AddDays(1),
                    Role = Groups.MedicLab,
                    User = medicLabPersonEmail2
                },
                new PersonAllocation()
                {
                    Start = DateTimeOffset.Now,
                    End = DateTimeOffset.Now.AddDays(1),
                    Role = Groups.DataExporter,
                    User = medicLabPersonEmail2
                }
            };
            response = AllocatePersonsToPlace(client, allocations, place2.Id);
            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode, response.Content.ReadAsStringAsync().Result);


            // login again, check tokens

            client.DefaultRequestHeaders.Clear();
            client.DefaultRequestHeaders.Add("Authorization", $"Bearer {medicPersonToken1}");

            response = FinalDataExport(client, 0, 100);
            Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode);

            response = SetPlaceProvider(client, pp1.PlaceProviderId);
            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode, response.Content.ReadAsStringAsync().Result);
            medicPersonToken1 = response.Content.ReadAsStringAsync().Result;

            response = SetPlaceProvider(client, pp2.PlaceProviderId);
            Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode, response.Content.ReadAsStringAsync().Result);


            handler = new JwtSecurityTokenHandler();
            tokenS = handler.ReadToken(medicPersonToken1) as JwtSecurityToken;
            jti = tokenS.Claims.FirstOrDefault(claim => claim.Type == "Role" && claim.Value == Groups.MedicTester);
            Assert.IsNotNull(jti);
            jti = tokenS.Claims.FirstOrDefault(claim => claim.Type == Token.Claims.PlaceProvider);
            Assert.IsNotNull(jti);
            Assert.AreEqual(pp1.PlaceProviderId, jti.Value);

            client.DefaultRequestHeaders.Clear();
            client.DefaultRequestHeaders.Add("Authorization", $"Bearer {medicLabPersonToken1}");

            response = FinalDataExport(client, 0, 100);
            Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode);

            response = SetPlaceProvider(client, pp1.PlaceProviderId);
            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode, response.Content.ReadAsStringAsync().Result);
            medicLabPersonToken1 = response.Content.ReadAsStringAsync().Result;
            client.DefaultRequestHeaders.Clear();
            client.DefaultRequestHeaders.Add("Authorization", $"Bearer {medicLabPersonToken1}");

            response = FinalDataExport(client, 0, 100);
            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);

            handler = new JwtSecurityTokenHandler();
            tokenS = handler.ReadToken(medicLabPersonToken1) as JwtSecurityToken;
            jti = tokenS.Claims.FirstOrDefault(claim => claim.Type == "Role" && claim.Value == Groups.MedicLab);
            Assert.IsNotNull(jti);
            jti = tokenS.Claims.FirstOrDefault(claim => claim.Type == "Role" && claim.Value == Groups.DataExporter);
            Assert.IsNotNull(jti);
            var jtis = tokenS.Claims.Where(claim => claim.Type == "Role").ToArray();
            Assert.AreEqual(2, jtis.Length);
            jti = tokenS.Claims.FirstOrDefault(claim => claim.Type == Token.Claims.PlaceProvider);
            Assert.IsNotNull(jti);
            Assert.AreEqual(pp1.PlaceProviderId, jti.Value);


            // login again, check tokens, pp2

            client.DefaultRequestHeaders.Clear();
            client.DefaultRequestHeaders.Add("Authorization", $"Bearer {medicPersonToken2}");

            response = FinalDataExport(client, 0, 100);
            Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode);

            response = SetPlaceProvider(client, pp2.PlaceProviderId);
            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode, response.Content.ReadAsStringAsync().Result);
            medicPersonToken2 = response.Content.ReadAsStringAsync().Result;

            response = SetPlaceProvider(client, pp1.PlaceProviderId);
            Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode, response.Content.ReadAsStringAsync().Result);


            handler = new JwtSecurityTokenHandler();
            tokenS = handler.ReadToken(medicPersonToken2) as JwtSecurityToken;
            jti = tokenS.Claims.FirstOrDefault(claim => claim.Type == "Role" && claim.Value == Groups.MedicTester);
            Assert.IsNotNull(jti);
            jti = tokenS.Claims.FirstOrDefault(claim => claim.Type == Token.Claims.PlaceProvider);
            Assert.IsNotNull(jti);
            Assert.AreEqual(pp2.PlaceProviderId, jti.Value);

            client.DefaultRequestHeaders.Clear();
            client.DefaultRequestHeaders.Add("Authorization", $"Bearer {medicLabPersonToken2}");

            response = FinalDataExport(client, 0, 100);
            Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode);

            response = SetPlaceProvider(client, pp2.PlaceProviderId);
            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode, response.Content.ReadAsStringAsync().Result);
            medicLabPersonToken2 = response.Content.ReadAsStringAsync().Result;
            client.DefaultRequestHeaders.Clear();
            client.DefaultRequestHeaders.Add("Authorization", $"Bearer {medicLabPersonToken2}");

            response = FinalDataExport(client, 0, 100);
            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);

            handler = new JwtSecurityTokenHandler();
            tokenS = handler.ReadToken(medicLabPersonToken2) as JwtSecurityToken;
            jti = tokenS.Claims.FirstOrDefault(claim => claim.Type == "Role" && claim.Value == Groups.MedicLab);
            Assert.IsNotNull(jti);
            jti = tokenS.Claims.FirstOrDefault(claim => claim.Type == "Role" && claim.Value == Groups.DataExporter);
            Assert.IsNotNull(jti);
            jtis = tokenS.Claims.Where(claim => claim.Type == "Role").ToArray();
            Assert.AreEqual(2, jtis.Length);
            jti = tokenS.Claims.FirstOrDefault(claim => claim.Type == Token.Claims.PlaceProvider);
            Assert.IsNotNull(jti);
            Assert.AreEqual(pp2.PlaceProviderId, jti.Value);

            // perform fetch data by personal number, by reg code, assign the test set

            client.DefaultRequestHeaders.Clear();
            client.DefaultRequestHeaders.Add("Authorization", $"Bearer {medicPersonToken1}");

            response = FinalDataExport(client, 0, 100);
            Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode);

            response = GetVisitor(client, user1.Id.ToString());
            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode, response.Content.ReadAsStringAsync().Result);
            var responseVisitor = Newtonsoft.Json.JsonConvert.DeserializeObject<Visitor>(response.Content.ReadAsStringAsync().Result);
            Assert.AreEqual(user1.RC, responseVisitor.RC);
            Assert.AreEqual(user1.FirstName, responseVisitor.FirstName);
            Assert.AreEqual(user1.LastName, responseVisitor.LastName);
            Assert.AreEqual(user1.Address, responseVisitor.Address);


            response = GetVisitorByRC(client, user1.RC);
            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode, response.Content.ReadAsStringAsync().Result);
            responseVisitor = Newtonsoft.Json.JsonConvert.DeserializeObject<Visitor>(response.Content.ReadAsStringAsync().Result);
            Assert.AreEqual(user1.RC, responseVisitor.RC);
            Assert.AreEqual(user1.FirstName, responseVisitor.FirstName);
            Assert.AreEqual(user1.LastName, responseVisitor.LastName);
            Assert.AreEqual(user1.Address, responseVisitor.Address);

            var test1 = "111-111-111";
            response = ConnectVisitorToTest(client, registered1[0].Id.ToString(), test1);
            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode, response.Content.ReadAsStringAsync().Result);

            var test2 = "222-222-222";
            response = ConnectVisitorToTest(client, registered1[1].Id.ToString(), test2);
            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode, response.Content.ReadAsStringAsync().Result);

            client.DefaultRequestHeaders.Clear();
            client.DefaultRequestHeaders.Add("Authorization", $"Bearer {medicLabPersonToken1}");

            response = FinalDataExport(client, 0, 100);
            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
            noEmailSender.Data.Clear();

            var iVisitor = web.Server.Services.GetService<CovidMassTesting.Repository.Interface.IVisitorRepository>();
            var visitor1 = iVisitor.GetVisitor(registered1[0].Id).Result;
            visitor1.TestingTime = DateTimeOffset.Now.AddMinutes(-16);
            await iVisitor.SetVisitor(visitor1, false);

            // TEST mark as sick
            response = SetResult(client, test1, TestResult.PositiveWaitingForCertificate);
            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode, response.Content.ReadAsStringAsync().Result);
            var testResult = Newtonsoft.Json.JsonConvert.DeserializeObject<Result>(response.Content.ReadAsStringAsync().Result);
            Assert.AreEqual(TestResult.PositiveWaitingForCertificate, testResult.State);


            if (configuration["SendResultsThroughQueue"] == "1")
            {
                Assert.AreEqual(0, noEmailSender.Data.Count);
            }

            testResult.Time = DateTimeOffset.Now.AddMinutes(-15);
            await iVisitor.SetResultObject(testResult, false);
            iVisitor.ProcessSingle().Wait();
            Assert.AreEqual(1, noEmailSender.Data.Count);

            var tuple = noEmailSender.Data.Values.First();
            Assert.AreEqual(1, tuple.attachments.Count());

#if DEBUG
            var file = tuple.attachments.First();
            File.WriteAllBytes($"d:/covid/{file.Filename}", Convert.FromBase64String(file.Content));
#endif

            // TEST mark as wrong code
            response = SetResult(client, test1, TestResult.PositiveCertificateTaken);
            Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode);

            var visitor2 = await iVisitor.GetVisitor(registered1[1].Id);
            visitor2.TestingTime = DateTimeOffset.Now.AddMinutes(-16);
            await iVisitor.SetVisitor(visitor2, false);


            // TEST mark as sick
            response = SetResult(client, test2, TestResult.NegativeWaitingForCertificate);
            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode, response.Content.ReadAsStringAsync().Result);
            testResult = Newtonsoft.Json.JsonConvert.DeserializeObject<Result>(response.Content.ReadAsStringAsync().Result);
            Assert.AreEqual(TestResult.NegativeWaitingForCertificate, testResult.State);
            client.DefaultRequestHeaders.Clear();
            var idWithSlashes = registered1[0].Id.ToString();

            response = PublicGetTestResult(client, idWithSlashes.Substring(0, 3) + "‐" + idWithSlashes.Substring(3, 3) + " " + idWithSlashes.Substring(6), registered1[0].RC.Substring(6, 4));
            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode, response.Content.ReadAsStringAsync().Result);
            testResult = Newtonsoft.Json.JsonConvert.DeserializeObject<Result>(response.Content.ReadAsStringAsync().Result);
            Assert.AreEqual(TestResult.PositiveCertificateTaken, testResult.State);
            Assert.IsNotNull(testResult.VerificationId);

            response = PublicGetTestResult(client, registered1[1].Id.ToString(), registered1[1].RC.Substring(6, 4));
            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode, response.Content.ReadAsStringAsync().Result);
            testResult = Newtonsoft.Json.JsonConvert.DeserializeObject<Result>(response.Content.ReadAsStringAsync().Result);
            if (configuration["SendResultsThroughQueue"] == "1")
            {
                Assert.AreEqual(TestResult.TestIsBeingProcessing, testResult.State);
            }
            else
            {
                Assert.AreEqual(TestResult.NegativeCertificateTaken, testResult.State);
            }
            visitor1 = iVisitor.GetVisitor(registered1[0].Id).Result;

            response = VerifyResult(client, visitor1.VerificationId);
            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode, response.Content.ReadAsStringAsync().Result);
            var resultData = Newtonsoft.Json.JsonConvert.DeserializeObject<VerificationData>(response.Content.ReadAsStringAsync().Result);
            Assert.AreEqual(TestResult.PositiveWaitingForCertificate, resultData.Result);
            Assert.AreEqual("Antigen 1", resultData.Product);


            iVisitor.ProcessSingle().Wait(); // send email, mark with verification id

            visitor2 = iVisitor.GetVisitor(registered1[1].Id).Result;
            response = VerifyResult(client, visitor2.VerificationId);
            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode, response.Content.ReadAsStringAsync().Result);
            resultData = Newtonsoft.Json.JsonConvert.DeserializeObject<VerificationData>(response.Content.ReadAsStringAsync().Result);
            Assert.AreEqual(TestResult.NegativeWaitingForCertificate, resultData.Result);
            Assert.AreEqual("Antigen 1", resultData.Product);



            // perform fetch data by personal number, by reg code, assign the test set

            client.DefaultRequestHeaders.Clear();
            client.DefaultRequestHeaders.Add("Authorization", $"Bearer {medicPersonToken2}");

            response = FinalDataExport(client, 0, 100);
            Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode);

            response = GetVisitor(client, user3.Id.ToString());
            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode, response.Content.ReadAsStringAsync().Result);
            responseVisitor = Newtonsoft.Json.JsonConvert.DeserializeObject<Visitor>(response.Content.ReadAsStringAsync().Result);
            Assert.AreEqual(user3.RC, responseVisitor.RC);
            Assert.AreEqual(user3.FirstName, responseVisitor.FirstName);
            Assert.AreEqual(user3.LastName, responseVisitor.LastName);
            Assert.AreEqual(user3.Address, responseVisitor.Address);


            response = GetVisitorByRC(client, user4.RC);
            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode, response.Content.ReadAsStringAsync().Result);
            responseVisitor = Newtonsoft.Json.JsonConvert.DeserializeObject<Visitor>(response.Content.ReadAsStringAsync().Result);
            Assert.AreEqual(user4.RC, responseVisitor.RC);
            Assert.AreEqual(user4.FirstName, responseVisitor.FirstName);
            Assert.AreEqual(user4.LastName, responseVisitor.LastName);
            Assert.AreEqual(user4.Address, responseVisitor.Address);

            test1 = "911-111-111";
            response = ConnectVisitorToTest(client, registered2[0].Id.ToString(), test1);
            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode, response.Content.ReadAsStringAsync().Result);

            test2 = "922-222-222";
            response = ConnectVisitorToTest(client, registered2[1].Id.ToString(), test2);
            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode, response.Content.ReadAsStringAsync().Result);

            client.DefaultRequestHeaders.Clear();
            client.DefaultRequestHeaders.Add("Authorization", $"Bearer {medicLabPersonToken2}");

            response = FinalDataExport(client, 0, 100);
            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
            noEmailSender.Data.Clear();

            var visitor3 = iVisitor.GetVisitor(registered2[0].Id).Result;
            visitor3.TestingTime = DateTimeOffset.Now.AddMinutes(-16);
            await iVisitor.SetVisitor(visitor3, false);

            // TEST mark as sick
            response = SetResult(client, test1, TestResult.PositiveWaitingForCertificate);
            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode, response.Content.ReadAsStringAsync().Result);
            testResult = Newtonsoft.Json.JsonConvert.DeserializeObject<Result>(response.Content.ReadAsStringAsync().Result);
            Assert.AreEqual(TestResult.PositiveWaitingForCertificate, testResult.State);


            if (configuration["SendResultsThroughQueue"] == "1")
            {
                Assert.AreEqual(0, noEmailSender.Data.Count);
            }

            testResult.Time = DateTimeOffset.Now.AddMinutes(-15);
            await iVisitor.SetResultObject(testResult, false);
            iVisitor.ProcessSingle().Wait();
            Assert.AreEqual(1, noEmailSender.Data.Count);

            tuple = noEmailSender.Data.Values.First();
            Assert.AreEqual(1, tuple.attachments.Count());

#if DEBUG
            file = tuple.attachments.First();
            File.WriteAllBytes($"d:/covid/{file.Filename}", Convert.FromBase64String(file.Content));
#endif

            // TEST mark as wrong code
            response = SetResult(client, test1, TestResult.PositiveCertificateTaken);
            Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode);

            visitor2 = await iVisitor.GetVisitor(registered2[1].Id);
            visitor2.TestingTime = DateTimeOffset.Now.AddMinutes(-16);
            await iVisitor.SetVisitor(visitor2, false);


            // TEST mark as sick
            response = SetResult(client, test2, TestResult.NegativeWaitingForCertificate);
            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode, response.Content.ReadAsStringAsync().Result);
            testResult = Newtonsoft.Json.JsonConvert.DeserializeObject<Result>(response.Content.ReadAsStringAsync().Result);
            Assert.AreEqual(TestResult.NegativeWaitingForCertificate, testResult.State);
            client.DefaultRequestHeaders.Clear();
            idWithSlashes = registered2[0].Id.ToString();

            response = PublicGetTestResult(client, idWithSlashes.Substring(0, 3) + "‐" + idWithSlashes.Substring(3, 3) + " " + idWithSlashes.Substring(6), registered2[0].RC.Substring(6, 4));
            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode, response.Content.ReadAsStringAsync().Result);
            testResult = Newtonsoft.Json.JsonConvert.DeserializeObject<Result>(response.Content.ReadAsStringAsync().Result);
            Assert.AreEqual(TestResult.PositiveCertificateTaken, testResult.State);
            Assert.IsNotNull(testResult.VerificationId);

            response = PublicGetTestResult(client, registered2[1].Id.ToString(), registered2[1].RC.Substring(6, 4));
            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode, response.Content.ReadAsStringAsync().Result);
            testResult = Newtonsoft.Json.JsonConvert.DeserializeObject<Result>(response.Content.ReadAsStringAsync().Result);
            if (configuration["SendResultsThroughQueue"] == "1")
            {
                Assert.AreEqual(TestResult.TestIsBeingProcessing, testResult.State);
            }
            else
            {
                Assert.AreEqual(TestResult.NegativeCertificateTaken, testResult.State);
            }
            visitor1 = iVisitor.GetVisitor(registered2[0].Id).Result;

            response = VerifyResult(client, visitor1.VerificationId);
            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode, response.Content.ReadAsStringAsync().Result);
            resultData = Newtonsoft.Json.JsonConvert.DeserializeObject<VerificationData>(response.Content.ReadAsStringAsync().Result);
            Assert.AreEqual(TestResult.PositiveWaitingForCertificate, resultData.Result);
            Assert.AreEqual("Antigen 2", resultData.Product);


            iVisitor.ProcessSingle().Wait(); // send email, mark with verification id

            visitor2 = iVisitor.GetVisitor(registered2[1].Id).Result;
            response = VerifyResult(client, visitor2.VerificationId);
            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode, response.Content.ReadAsStringAsync().Result);
            resultData = Newtonsoft.Json.JsonConvert.DeserializeObject<VerificationData>(response.Content.ReadAsStringAsync().Result);
            Assert.AreEqual(TestResult.NegativeWaitingForCertificate, resultData.Result);
            Assert.AreEqual("Antigen 2", resultData.Product);

            client.DefaultRequestHeaders.Clear();
            client.DefaultRequestHeaders.Add("Authorization", $"Bearer {medicLabPersonToken1}");

            response = ListTestedVisitors(client, 0, 100);
            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode, response.Content.ReadAsStringAsync().Result);
            var stream = new MemoryStream();
            response.Content.CopyToAsync(stream).Wait();
            var resultExport = Encoding.UTF8.GetString(stream.ToArray());
            Assert.IsNotNull(resultExport);
            Assert.IsTrue(resultExport.Contains("0101010019"));
            Assert.IsTrue(resultExport.Contains("0101010008"));
            Assert.IsFalse(resultExport.Contains("0151020018"));
            Assert.IsFalse(resultExport.Contains("0101020007"));

            response = ProofOfWorkExport(client, 0, 100);
            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode, response.Content.ReadAsStringAsync().Result);
            stream = new MemoryStream();
            response.Content.CopyToAsync(stream).Wait();
            resultExport = Encoding.UTF8.GetString(stream.ToArray());
            Assert.IsNotNull(resultExport);
            Assert.IsTrue(resultExport.Contains("0101010019"));
            Assert.IsTrue(resultExport.Contains("0101010008"));
            Assert.IsFalse(resultExport.Contains("0151020018"));
            Assert.IsFalse(resultExport.Contains("0101020007"));

            response = FinalDataExport(client, 0, 100);
            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode, response.Content.ReadAsStringAsync().Result);
            stream = new MemoryStream();
            response.Content.CopyToAsync(stream).Wait();
            resultExport = Encoding.UTF8.GetString(stream.ToArray());
            Assert.IsNotNull(resultExport);
            Assert.IsFalse(resultExport.Contains("0101010019"));
            Assert.IsTrue(resultExport.Contains("0101010008"));
            Assert.IsFalse(resultExport.Contains("0151020018"));
            Assert.IsFalse(resultExport.Contains("0101020007"));



            client.DefaultRequestHeaders.Clear();
            client.DefaultRequestHeaders.Add("Authorization", $"Bearer {medicLabPersonToken2}");

            response = ListTestedVisitors(client, 0, 100);
            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode, response.Content.ReadAsStringAsync().Result);
            stream = new MemoryStream();
            response.Content.CopyToAsync(stream).Wait();
            resultExport = Encoding.UTF8.GetString(stream.ToArray());
            Assert.IsNotNull(resultExport);
            Assert.IsFalse(resultExport.Contains("0101010019"));
            Assert.IsFalse(resultExport.Contains("0101010008"));
            Assert.IsTrue(resultExport.Contains("0151020018"));
            Assert.IsTrue(resultExport.Contains("0101020007"));

            response = ProofOfWorkExport(client, 0, 100);
            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode, response.Content.ReadAsStringAsync().Result);
            stream = new MemoryStream();
            response.Content.CopyToAsync(stream).Wait();
            resultExport = Encoding.UTF8.GetString(stream.ToArray());
            Assert.IsNotNull(resultExport);
            Assert.IsFalse(resultExport.Contains("0101010019"));
            Assert.IsFalse(resultExport.Contains("0101010008"));
            Assert.IsTrue(resultExport.Contains("0151020018"));
            Assert.IsTrue(resultExport.Contains("0101020007"));

            response = FinalDataExport(client, 0, 100);
            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode, response.Content.ReadAsStringAsync().Result);
            stream = new MemoryStream();
            response.Content.CopyToAsync(stream).Wait();
            resultExport = Encoding.UTF8.GetString(stream.ToArray());
            Assert.IsNotNull(resultExport);
            Assert.IsFalse(resultExport.Contains("0101010019"));
            Assert.IsFalse(resultExport.Contains("0101010008"));
            Assert.IsFalse(resultExport.Contains("0151020018"));
            Assert.IsTrue(resultExport.Contains("0101020007"));


        }

    }
}
