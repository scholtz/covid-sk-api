using CovidMassTesting.Helpers;
using CovidMassTesting.Model;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;

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

        private HttpResponseMessage Authenticate(HttpClient client, string email, string pass, string rand)
        {
            return client.PostAsync("User/Authenticate",
                    new System.Net.Http.FormUrlEncodedContent(new List<KeyValuePair<string, string>>() {
                        new KeyValuePair<string, string>("email",email),
                        new KeyValuePair<string, string>("hash",pass),
                        new KeyValuePair<string, string>("data",rand)
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

        [Test]
        public void Authorize()
        {
            using (var web = new MockWebApp())
            {
                var client = web.CreateClient();
                var users = configuration.GetSection("AdminUsers").Get<CovidMassTesting.Model.Settings.User[]>();//.GetValue<List<CovidMassTesting.Model.Settings.User>>("AdminUsers");

                var user = users.First(u => u.Name == "Admin");
                /// Preauthenticate
                var request = Preauthenticate(client, user.Email);
                Assert.AreEqual(HttpStatusCode.OK, request.StatusCode);
                var cohash = request.Content.ReadAsStringAsync().Result;

                /// Authenticate
                var rand = "123";
                var pass = user.Password;
                for (int i = 0; i < 99; i++)
                {
                    pass = Encoding.ASCII.GetBytes($"{pass}{cohash}").GetSHA256Hash();
                }
                pass = Encoding.ASCII.GetBytes($"{pass}{rand}").GetSHA256Hash();

                request = Authenticate(client, user.Email, pass, rand);
                Assert.AreEqual(HttpStatusCode.OK, request.StatusCode);
                var token = request.Content.ReadAsStringAsync().Result;
                Assert.IsFalse(string.IsNullOrEmpty(token));
                client.DefaultRequestHeaders.Add("Authorization", $"Bearer {token}");

                request = CheckSlots(client);
                Assert.AreEqual(HttpStatusCode.OK, request.StatusCode);


            }

        }



        [Test]
        public void Visitor()
        {
            using (var web = new MockWebApp())
            {
                var client = web.CreateClient();
                var users = configuration.GetSection("AdminUsers").Get<CovidMassTesting.Model.Settings.User[]>();//.GetValue<List<CovidMassTesting.Model.Settings.User>>("AdminUsers");

                var user = users.First(u => u.Name == "Admin");
                /// Preauthenticate
                var request = Preauthenticate(client, user.Email);
                Assert.AreEqual(HttpStatusCode.OK, request.StatusCode);
                var cohash = request.Content.ReadAsStringAsync().Result;

                /// Authenticate
                var rand = "123";
                var pass = user.Password;
                for (int i = 0; i < 99; i++)
                {
                    pass = Encoding.ASCII.GetBytes($"{pass}{cohash}").GetSHA256Hash();
                }
                pass = Encoding.ASCII.GetBytes($"{pass}{rand}").GetSHA256Hash();

                request = Authenticate(client, user.Email, pass, rand);
                Assert.AreEqual(HttpStatusCode.OK, request.StatusCode);
                var token = request.Content.ReadAsStringAsync().Result;
                Assert.IsFalse(string.IsNullOrEmpty(token));
                client.DefaultRequestHeaders.Add("Authorization", $"Bearer {token}");

                request = CheckSlots(client);
                Assert.AreEqual(HttpStatusCode.OK, request.StatusCode);

                client.DefaultRequestHeaders.Clear();

                request = ListPlaces(client);
                Assert.AreEqual(HttpStatusCode.OK, request.StatusCode);
                var places = Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, Place>>(request.Content.ReadAsStringAsync().Result);
                Assert.IsTrue(places.Count > 0);
                var place = places.First().Value;
                request = ListDaySlotsByPlace(client, place.Id);
                Assert.AreEqual(HttpStatusCode.OK, request.StatusCode);
                var days = Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, Slot1Day>>(request.Content.ReadAsStringAsync().Result);
                Assert.IsTrue(days.Count > 0);

                var day = days.First().Value;
                request = ListHourSlotsByPlaceAndDaySlotId(client, place.Id, day.SlotId.ToString());
                Assert.AreEqual(HttpStatusCode.OK, request.StatusCode);
                var hours = Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, Slot1Hour>>(request.Content.ReadAsStringAsync().Result);
                Assert.IsTrue(hours.Count > 0);

                var hour = hours.First().Value;
                request = ListMinuteSlotsByPlaceAndHourSlotId(client, place.Id, hour.SlotId.ToString());
                Assert.AreEqual(HttpStatusCode.OK, request.StatusCode);
                var minutes = Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, Slot5Min>>(request.Content.ReadAsStringAsync().Result);
                Assert.IsTrue(minutes.Count > 0);

                var minute = minutes.Values.First();

                Visitor visitor = new Visitor()
                {
                    Address = "addr",
                    Email = "email@scholtz.sk",
                    ChosenPlaceId = place.Id,
                    ChosenSlot = minute.SlotId,
                    FirstName = "F",
                    LastName = "L",
                    Insurance = "25",
                    PersonType = "rc",
                    Phone = "+421",
                    RC = "0101010008",

                };
                request = Register(client, visitor);
                Assert.AreEqual(HttpStatusCode.OK, request.StatusCode);
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

            }
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