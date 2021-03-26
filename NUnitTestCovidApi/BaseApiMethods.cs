using CovidMassTesting.Helpers;
using CovidMassTesting.Model;
using Newtonsoft.Json;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text;

namespace NUnitTestCovidApi
{
    public class BaseApiMethods
    {

        protected HttpResponseMessage CheckVersion(HttpClient client)
        {
            return client.GetAsync("Version").Result;
        }
        protected HttpResponseMessage AuthenticateUser(HttpClient client, string email, string password)
        {
            var request = Preauthenticate(client, email);
            var content = request.Content.ReadAsStringAsync().Result;
            var data = JsonConvert.DeserializeObject<AuthData>(content);
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
        protected HttpResponseMessage Authenticate(HttpClient client, string email, string pass)
        {
            return client.PostAsync("User/Authenticate",
                    new System.Net.Http.FormUrlEncodedContent(new List<KeyValuePair<string, string>>() {
                        new KeyValuePair<string, string>("email",email),
                        new KeyValuePair<string, string>("hash",pass)
                    })
                    ).Result;
        }
        protected HttpResponseMessage SetPlaceProvider(HttpClient client, string placeProviderId)
        {
            return client.PostAsync("User/SetPlaceProvider",
                    new System.Net.Http.FormUrlEncodedContent(new List<KeyValuePair<string, string>>() {
                        new KeyValuePair<string, string>("placeProviderId",placeProviderId),
                    })
                    ).Result;
        }
        protected HttpResponseMessage ChangePassword(HttpClient client, string oldHash, string newHash)
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
        protected HttpResponseMessage FixAdvancedStatsSlots(HttpClient client)
        {
            return client.PostAsync("Admin/FixAdvancedStatsSlots",
                    new System.Net.Http.FormUrlEncodedContent(new List<KeyValuePair<string, string>>()
                    {

                    })
                    ).Result;
        }
        protected HttpResponseMessage FixSlotIssues(HttpClient client)
        {
            return client.PostAsync("Admin/FixSlotIssues",
                    new System.Net.Http.FormUrlEncodedContent(new List<KeyValuePair<string, string>>()
                    {

                    })
                    ).Result;
        }
        
        protected HttpResponseMessage Preauthenticate(HttpClient client, string email)
        {
            return client.PostAsync("User/Preauthenticate",
                    new System.Net.Http.FormUrlEncodedContent(new List<KeyValuePair<string, string>>() {
                        new KeyValuePair<string, string>("email",email)
                    })
                    ).Result;
        }
        protected HttpResponseMessage CheckSlotsDayToday(HttpClient client)
        {
            return client.PostAsync("Admin/CheckSlots",
                    new System.Net.Http.FormUrlEncodedContent(new List<KeyValuePair<string, string>>() {
                        new KeyValuePair<string, string>("testingDay",$"{DateTimeOffset.Now.ToString("yyyy-MM-dd")}T00:00:00+00:00"),
                        new KeyValuePair<string, string>("from","22"),
                        new KeyValuePair<string, string>("until","24"),
                    })
                ).Result;
        }
        protected HttpResponseMessage CheckSlotsDay1(HttpClient client)
        {

            return client.PostAsync("Admin/CheckSlots",
                    new System.Net.Http.FormUrlEncodedContent(new List<KeyValuePair<string, string>>() {
                        new KeyValuePair<string, string>("testingDay",$"{DateTimeOffset.Now.AddDays(1).ToString("yyyy-MM-dd")}T00:00:00+00:00"),
                        new KeyValuePair<string, string>("from","10"),
                        new KeyValuePair<string, string>("until","12"),
                    })
                ).Result;/**/
        }
        protected HttpResponseMessage CheckSlotsDay2(HttpClient client)
        {
            return client.PostAsync("Admin/CheckSlots",
                    new System.Net.Http.FormUrlEncodedContent(new List<KeyValuePair<string, string>>() {
                        new KeyValuePair<string, string>("testingDay",$"{DateTimeOffset.Now.AddDays(2).ToString("yyyy-MM-dd")}T00:00:00+00:00"),
                        new KeyValuePair<string, string>("from","10"),
                        new KeyValuePair<string, string>("until","12"),
                    })
                ).Result;
        }
        protected HttpResponseMessage ListPlaces(HttpClient client)
        {
            return client.GetAsync("Place/List").Result;
        }
        protected Place[] SetupDebugPlaces(HttpClient client)
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
                    OpeningHoursWorkDay = "20:00-23:55",
                    OpeningHoursOther1 = "23:45-23:55",
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
                    OpeningHoursWorkDay = "21:00-23:55",
                    OpeningHoursOther1 = "23:25-23:55",
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
                    OpeningHoursWorkDay = "23:00-23:55",
                    OpeningHoursOther1 = "23:05-23:55",
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
        protected Product SetupDebugProduct(HttpClient client)
        {
            // Test product management
            var request = CreateProduct(client, new Product()
            {
                Name = "Antigenovy test",
                Description = "Statny antigenovy test",
                DefaultPrice = 0,
                DefaultPriceCurrency = "EUR",
                Category = "ant",
                All = true,
                InsuranceOnly = false,
            });
            Assert.AreEqual(HttpStatusCode.OK, request.StatusCode, request.Content.ReadAsStringAsync().Result);
            return JsonConvert.DeserializeObject<Product>(request.Content.ReadAsStringAsync().Result);
        }
        protected HttpResponseMessage RegisterPlace(HttpClient client, Place place)
        {
            var body = Newtonsoft.Json.JsonConvert.SerializeObject(place);
            return client.PostAsync("Place/InsertOrUpdate",
                                new System.Net.Http.StringContent(body, Encoding.UTF8, "application/json")
                                ).Result;
        }

        protected HttpResponseMessage ListDaySlotsByPlace(HttpClient client, string placeId)
        {
            return client.GetAsync($"Slot/ListDaySlotsByPlace?placeId={placeId}").Result;
        }
        protected HttpResponseMessage PlaceProviderListPublic(HttpClient client)
        {
            return client.GetAsync($"PlaceProvider/ListPublic").Result;
        }
        protected HttpResponseMessage ListPPInvites(HttpClient client)
        {
            return client.GetAsync($"User/ListPPInvites").Result;
        }
        protected HttpResponseMessage ListUserInvites(HttpClient client)
        {
            return client.GetAsync($"User/ListUserInvites").Result;
        }

        protected HttpResponseMessage ListHourSlotsByPlaceAndDaySlotId(HttpClient client, string placeId, string daySlotId)
        {
            return client.GetAsync($"Slot/ListHourSlotsByPlaceAndDaySlotId?placeId={placeId}&daySlotId={daySlotId}").Result;
        }
        protected HttpResponseMessage ListMinuteSlotsByPlaceAndHourSlotId(HttpClient client, string placeId, string hourSlotId)
        {
            return client.GetAsync($"Slot/ListMinuteSlotsByPlaceAndHourSlotId?placeId={placeId}&hourSlotId={hourSlotId}").Result;
        }

        protected HttpResponseMessage Register(HttpClient client, Visitor visitor)
        {
            var body = Newtonsoft.Json.JsonConvert.SerializeObject(visitor);
            return client.PostAsync("Visitor/Register",
                                new System.Net.Http.StringContent(body, Encoding.UTF8, "application/json")
                                ).Result;
        }
        protected HttpResponseMessage RegisterByManager(HttpClient client, Visitor visitor)
        {
            var body = Newtonsoft.Json.JsonConvert.SerializeObject(visitor);
            return client.PostAsync("Visitor/RegisterByManager",
                                new System.Net.Http.StringContent(body, Encoding.UTF8, "application/json")
                                ).Result;
        }



        protected HttpResponseMessage StatsTestedVisitors(HttpClient client, string statsType, string placeProviderId)
        {
            return client.GetAsync($"PlaceProvider/GetStats?statsType={statsType}&placeProviderId={placeProviderId}").Result;
        }
        protected HttpResponseMessage AllocatePersonsToPlace(HttpClient client, PersonAllocation[] allocations, string place)
        {
            var body = Newtonsoft.Json.JsonConvert.SerializeObject(allocations);
            return client.PostAsync($"PlaceProvider/AllocatePersonsToPlace?placeId={place}",
                                new System.Net.Http.StringContent(body, Encoding.UTF8, "application/json")
                                ).Result;
        }
        protected HttpResponseMessage RemoveAllocationAtPlace(HttpClient client, string allocationId, string placeId)
        {
            return client.PostAsync("PlaceProvider/RemoveAllocationAtPlace",
                    new System.Net.Http.FormUrlEncodedContent(new List<KeyValuePair<string, string>>() {
                        new KeyValuePair<string, string>("allocationId",allocationId),
                        new KeyValuePair<string, string>("placeId",placeId),
                    })
                ).Result;
        }

        protected HttpResponseMessage ListPlaceAllocations(HttpClient client, string place)
        {
            return client.GetAsync($"PlaceProvider/ListPlaceAllocations?placeId={place}").Result;
        }
        protected HttpResponseMessage ScheduleOpenningHours(HttpClient client, TimeUpdate[] actions)
        {
            var body = Newtonsoft.Json.JsonConvert.SerializeObject(actions);
            return client.PostAsync("Place/ScheduleOpenningHours",
                                new System.Net.Http.StringContent(body, Encoding.UTF8, "application/json")
                                ).Result;
        }
        protected HttpResponseMessage ListScheduledDays(HttpClient client)
        {
            return client.GetAsync("Place/ListScheduledDays").Result;
        }

        protected HttpResponseMessage ConnectVisitorToTest(HttpClient client, string visitorCode, string testCode)
        {
            return client.PostAsync("Result/ConnectVisitorToTest",
                    new System.Net.Http.FormUrlEncodedContent(new List<KeyValuePair<string, string>>() {
                        new KeyValuePair<string, string>("visitorCode",visitorCode),
                        new KeyValuePair<string, string>("testCode",testCode),
                    })
                ).Result;
        }

        protected HttpResponseMessage GetVisitor(HttpClient client, string visitorCode)
        {
            return client.PostAsync("Result/GetVisitor",
                    new System.Net.Http.FormUrlEncodedContent(new List<KeyValuePair<string, string>>() {
                        new KeyValuePair<string, string>("visitorCode",visitorCode),
                    })
                ).Result;
        }
        protected HttpResponseMessage GetVisitorByRC(HttpClient client, string rc)
        {
            return client.PostAsync("Result/GetVisitorByRC",
                    new System.Net.Http.FormUrlEncodedContent(new List<KeyValuePair<string, string>>() {
                        new KeyValuePair<string, string>("rc",rc),
                    })
                ).Result;
        }
        protected HttpResponseMessage GetNextTest(HttpClient client)
        {
            return client.GetAsync("Result/GetNextTest").Result;
        }
        protected HttpResponseMessage RemoveFromDocQueue(HttpClient client, string testId)
        {
            return client.PostAsync("Result/RemoveFromDocQueue",
                    new System.Net.Http.FormUrlEncodedContent(new List<KeyValuePair<string, string>>() {
                        new KeyValuePair<string, string>("testId",testId),
                    })
                ).Result;
        }
        protected HttpResponseMessage SetResult(HttpClient client, string testCode, string result)
        {
            return client.PostAsync("Result/SetResult",
                    new System.Net.Http.FormUrlEncodedContent(new List<KeyValuePair<string, string>>() {
                        new KeyValuePair<string, string>("testCode",testCode),
                        new KeyValuePair<string, string>("result",result),
                    })
                ).Result;
        }
        protected HttpResponseMessage PublicGetTestResult(HttpClient client, string code, string pass)
        {
            return client.PostAsync("Result/Get",
                    new System.Net.Http.FormUrlEncodedContent(new List<KeyValuePair<string, string>>() {
                        new KeyValuePair<string, string>("code",code),
                        new KeyValuePair<string, string>("pass",pass),
                    })
                ).Result;
        }
        protected HttpResponseMessage DownloadPDF(HttpClient client, string code, string pass)
        {
            return client.PostAsync("Result/DownloadPDF",
                    new System.Net.Http.FormUrlEncodedContent(new List<KeyValuePair<string, string>>() {
                        new KeyValuePair<string, string>("code",code),
                        new KeyValuePair<string, string>("pass",pass),
                    })
                ).Result;
        }
        protected HttpResponseMessage VerifyResult(HttpClient client, string id)
        {
            return client.PostAsync($"Result/VerifyResult",
                    new System.Net.Http.FormUrlEncodedContent(new List<KeyValuePair<string, string>>() {
                        new KeyValuePair<string, string>("id",id),
                    })).Result;
        }


        protected HttpResponseMessage PublicRemoveTest(HttpClient client, string code, string pass)
        {
            return client.PostAsync("Result/RemoveTest",
                    new System.Net.Http.FormUrlEncodedContent(new List<KeyValuePair<string, string>>() {
                        new KeyValuePair<string, string>("code",code),
                        new KeyValuePair<string, string>("pass",pass),
                    })
                ).Result;
        }
        protected HttpResponseMessage ProcessInvitation(HttpClient client, string invitationId, bool accepted)
        {
            return client.PostAsync("User/ProcessInvitation",
                    new System.Net.Http.FormUrlEncodedContent(new List<KeyValuePair<string, string>>() {
                        new KeyValuePair<string, string>("invitationId",invitationId),
                        new KeyValuePair<string, string>("accepted",accepted.ToString()),
                    })
                ).Result;
        }

        protected HttpResponseMessage InviteUserToPP(HttpClient client, string email, string name, string phone, string message)
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

        protected HttpResponseMessage PlaceProviderRegistration(HttpClient client, PlaceProvider pp)
        {
            var body = Newtonsoft.Json.JsonConvert.SerializeObject(pp);
            return client.PostAsync("PlaceProvider/Register",
                                new System.Net.Http.StringContent(body, Encoding.UTF8, "application/json")
                                ).Result;
        }
        protected HttpResponseMessage CreateProduct(HttpClient client, Product product)
        {
            var body = Newtonsoft.Json.JsonConvert.SerializeObject(product);
            return client.PostAsync("PlaceProvider/CreateProduct",
                                new System.Net.Http.StringContent(body, Encoding.UTF8, "application/json")
                                ).Result;
        }
        protected HttpResponseMessage InsertOrUpdatePlaceProduct(HttpClient client, PlaceProduct placeProduct)
        {
            var body = Newtonsoft.Json.JsonConvert.SerializeObject(placeProduct);
            return client.PostAsync("Place/InsertOrUpdatePlaceProduct",
                                new System.Net.Http.StringContent(body, Encoding.UTF8, "application/json")
                                ).Result;
        }

        protected HttpResponseMessage UpdateProduct(HttpClient client, Product product)
        {
            var body = Newtonsoft.Json.JsonConvert.SerializeObject(product);
            return client.PostAsync("PlaceProvider/UpdateProduct",
                                new System.Net.Http.StringContent(body, Encoding.UTF8, "application/json")
                                ).Result;
        }
        protected HttpResponseMessage DeleteProduct(HttpClient client, Product product)
        {
            var body = Newtonsoft.Json.JsonConvert.SerializeObject(product);
            return client.PostAsync("PlaceProvider/DeleteProduct",
                                new System.Net.Http.StringContent(body, Encoding.UTF8, "application/json")
                                ).Result;
        }

        protected HttpResponseMessage ListPlaceProductByCategory(HttpClient client, string category)
        {
            return client.GetAsync($"PlaceProvider/ListPlaceProductByCategory?category={category}").Result;
        }
        protected HttpResponseMessage ListPlaceProductByPlaceProvider(HttpClient client, string placeProviderId)
        {
            return client.GetAsync($"PlaceProvider/ListPlaceProductByPlaceProvider?placeProviderId={placeProviderId}").Result;
        }
        protected HttpResponseMessage ListPlaceProductByPlace(HttpClient client, string placeId)
        {
            return client.GetAsync($"PlaceProvider/ListPlaceProductByPlace?placeId={placeId}").Result;
        }
        protected HttpResponseMessage ListPlaceProduct(HttpClient client)
        {
            return client.GetAsync($"PlaceProvider/ListPlaceProduct").Result;
        }

        protected HttpResponseMessage DeletePlaceProduct(HttpClient client, string placeProductid)
        {
            return client.PostAsync("Place/DeletePlaceProduct",
                    new System.Net.Http.FormUrlEncodedContent(new List<KeyValuePair<string, string>>() {
                        new KeyValuePair<string, string>("placeProductid",placeProductid),
                    })
                ).Result;
        }

        protected HttpResponseMessage ListProducts(HttpClient client)
        {
            return client.GetAsync("PlaceProvider/ListProducts").Result;
        }

        protected HttpResponseMessage ListFiltered(HttpClient client, string category, string availability)
        {
            return client.GetAsync($"Place/ListFiltered?category={category}&availability={availability}").Result;
        }
        protected HttpResponseMessage FinalDataExport(HttpClient client, int from, int count)
        {
            client.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("text/csv"));
            var ret = client.GetAsync($"Result/FinalDataExport?from={from}&count={count}").Result;
            client.DefaultRequestHeaders.Accept.Clear();
            return ret;
        }
        protected HttpResponseMessage CompanyRegistrationsExport(HttpClient client, int from, int count)
        {
            client.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("text/csv"));
            var ret = client.GetAsync($"User/CompanyRegistrationsExport?from={from}&count={count}").Result;
            client.DefaultRequestHeaders.Accept.Clear();
            return ret;
        }
        protected HttpResponseMessage ProofOfWorkExport(HttpClient client, int from, int count)
        {
            client.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("text/csv"));
            var ret = client.GetAsync($"Result/ProofOfWorkExport?from={from}&count={count}").Result;
            client.DefaultRequestHeaders.Accept.Clear();
            return ret;
        }
        protected HttpResponseMessage ListTestedVisitors(HttpClient client, int from, int count)
        {
            client.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("text/csv"));
            var ret = client.GetAsync($"Result/ListTestedVisitors?from={from}&count={count}").Result;
            client.DefaultRequestHeaders.Accept.Clear();
            return ret;
        }

        protected HttpResponseMessage SetLocation(HttpClient client, string placeId)
        {
            return client.PostAsync("User/SetLocation",
                    new System.Net.Http.FormUrlEncodedContent(new List<KeyValuePair<string, string>>() {
                        new KeyValuePair<string, string>("placeId",placeId),
                    })
                ).Result;
        }
    }
}
