using CovidMassTesting.Helpers;
using CovidMassTesting.Model;
using CovidMassTesting.Model.EZdravie;
using CovidMassTesting.Model.EZdravie.Payload;
using CovidMassTesting.Model.EZdravie.Request;
using CovidMassTesting.Model.EZdravie.Response;
using CovidMassTesting.Repository.Interface;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CovidMassTesting.Connectors
{
    public abstract class MojeEZdravieAbstract : IMojeEZdravie
    {
        /// <summary>
        /// Downloads the visitors from eHealth
        /// </summary>
        /// <param name="placeProviderId"></param>
        /// <param name="managerEmail"></param>
        /// <param name="day"></param>
        /// <param name="visitorRepository"></param>
        /// <param name="placeRepository"></param>
        /// <param name="placeProviderRepository"></param>
        /// <param name="slotRepository"></param>
        /// <param name="loggerFactory"></param>
        /// <returns></returns>
        public async Task<int> DownloadEHealthVisitors(
            string placeProviderId,
            string managerEmail,
            DateTimeOffset day,
            IVisitorRepository visitorRepository,
            IPlaceRepository placeRepository,
            IPlaceProviderRepository placeProviderRepository,
            ISlotRepository slotRepository,
            ILoggerFactory loggerFactory
            )
        {
            var logger = loggerFactory.CreateLogger<MojeEZdravieAbstract>();
            logger.LogInformation($"eHealth: Downloading {placeProviderId} {managerEmail} {day}");
            var rPlaces = (await placeRepository.ListAll())?.Where(p => p.PlaceProviderId == placeProviderId);
            if (rPlaces == null || !rPlaces.Any()) throw new Exception("This place provider does not have any place defined");
            var pp = await placeProviderRepository.GetPlaceProvider(placeProviderId);
            var product = pp.Products.FirstOrDefault(pr => pr.EHealthDefault == true);
            if (product == null) product = pp.Products.FirstOrDefault();

            int ret = 0;
            var data = await MakeSurePlaceProviderIsAuthenticated(placeProviderId, placeProviderRepository);
            var token = data.LoginPayload.Session.Token;
            var places = await DriveInQueue(token, day);
            if (places.Payload == null) throw new Exception("No places found");

            foreach (var place in places.Payload)
            {
                var rPlace = rPlaces.FirstOrDefault(p => p.EHealthId == place.Id);
                if (rPlace == null) rPlace = rPlaces.FirstOrDefault();

                var list = await PlaceDetail(token, day, place.Id);
                foreach (var person in list.Payload)
                {
                    if (!person.DesignatedDriveinScheduledAt.HasValue) continue;
                    if (string.IsNullOrEmpty(person.BirthNumber?.FormatDocument())) continue;
                    var slot = await slotRepository.GetCurrentSlot(rPlace.Id, person.DesignatedDriveinScheduledAt.Value);

                    var documentClear = person.BirthNumber.FormatDocument();
                    var existing = await visitorRepository.GetVisitorByPersonalNumber(documentClear, true);
                    if (existing != null && existing.ChosenPlaceId == rPlace.Id && existing.ChosenSlot == slot.SlotId)
                    {
                        continue; // already exists
                    }

                    var visitor = new Visitor()
                    {
                        ChosenSlot = slot.SlotId,
                        ChosenPlaceId = rPlace.Id,
                        Product = product.Id,
                        FirstName = person.FirstName,
                        LastName = person.LastName,
                        RC = person.BirthNumber,
                        Insurance = person.HealthInsuranceCompany,
                        PersonTrackingNumber = person.PersonTrackingNumber,
                        Gender = person.Gender,
                        Street = person.Street,
                        StreetNo = person.StreetNumber,
                        City = person.City,
                        ZIP = person.PostalCode,
                        Phone = person.PrimaryPhone,
                        Language = "sk",
                        Result = TestResult.NotTaken,
                        DownloadedAt = DateTimeOffset.UtcNow
                    };
                    var newRegistration = await visitorRepository.Register(visitor, managerEmail, false);
                    logger.LogInformation($"eHealth: Visitor downloaded {visitor.Id} {visitor.RC.GetSHA256Hash()}");
                    ret++;
                }

            }
            return ret;
        }
        public abstract Task SendResultToEHealth(Visitor visitor);
        /// <summary>
        /// Send visitor registered in rychlejsie or downloaded from eHealth to eHealth system
        /// 
        /// Returns true if successful, False or Exception if not successful
        /// </summary>
        /// <param name="visitor"></param>
        /// <param name="placeProviderId"></param>
        /// <param name="placeProviderRepository"></param>
        /// <param name="configuration"></param>
        /// <returns></returns>
        public async Task<bool> SendResultToEHealth(
            Visitor visitor,
            string placeProviderId,
            IPlaceProviderRepository placeProviderRepository,
            IConfiguration configuration
            )
        {
            await SendResultToEHealth(visitor);
            var data = await MakeSurePlaceProviderIsAuthenticated(placeProviderId, placeProviderRepository);
            if (visitor.PersonType == "foreign")
            {
                throw new Exception("Only residents supported right now");
            }
            // session is valid

            if (string.IsNullOrEmpty(visitor.RC))
            {
                throw new Exception("Error - invalid personal number");
            }

            var check = await this.CheckPerson(data.LoginPayload.Session.Token, visitor.RC);
            if (check?.CfdId > 0)
            {
                // ok
            }
            else
            {
                if (configuration["AllowEHealthRegistration"] != "1")
                {
                    return false;
                }
                var personData = await RegisterPerson(data.LoginPayload.Session.Token, RegisterPersonRequest.FromVisitor(visitor, data.LoginPayload));
                check = await this.CheckPerson(data.LoginPayload.Session.Token, visitor.RC);
                if (check?.CfdId > 0)
                {
                    // ok
                }
                else
                {
                    throw new Exception("Unable to process visitor in ehealth - not found in search");
                }
            }

            var driveIn = await DriveInQueue(data.LoginPayload.Session.Token, DateTimeOffset.Now);
            var place = driveIn.Payload.OrderByDescending(p => p.DailyCapacity).FirstOrDefault();

            var t = DateTimeOffset.Now.ToString("yyyy-MM-dd HH:mm:ss");
            var input = new DriveInRequest()
            {
                DesignatedDriveinCity = place.City,
                DesignatedDriveinId = place.Id,
                DesignatedDriveinLatitude = place.Latitude,
                DesignatedDriveinLongitude = place.Longitude,
                DesignatedDriveinScheduledAt = t,
                DesignatedDriveinStreetName = place.StreetName,
                DesignatedDriveinStreetNumber = place.StreetNumber,
                DesignatedDriveinTitle = place.Title,
                DesignatedDriveinZipCode = place.ZipCode,
                MedicalAssessedAt = t,
                State = "SD",
                Triage = "2",
            };
            var addPersonToPlace = await AddPersonToTestingPlace(data.LoginPayload.Session.Token, check.CfdId, input);
            if (addPersonToPlace != "1") throw new Exception("Unexpected error returned while adding person to place");

            string result;
            switch (visitor.Result)
            {
                case TestResult.PositiveWaitingForCertificate:
                case TestResult.PositiveCertificateTaken:
                    result = "POSITIVE";
                    break;

                case TestResult.NegativeWaitingForCertificate:
                case TestResult.NegativeCertificateTaken:
                case TestResult.NegativeCertificateTakenTypo:
                    result = "NEGATIVE";
                    break;
                default:
                    throw new Exception($"Unable to determine state: {visitor.Result}");
            }

            var setResultRequest = new CovidMassTesting.Model.EZdravie.Request.SetResultRequest()
            {
                Id = 0,
                UserId = data.LoginPayload.User.Id,
                CovidFormDataId = check.CfdId,
                FinalResult = result,
                ScreeningFinalResult = result,
                SpecimenId = visitor.TestingSet,
                SpecimenCollectedAt = visitor.TestingTime.Value.ToString("yyyy-MM-dd HH:mm:ss"),
                ScreeningEndedAt = visitor.TestResultTime?.ToString("yyyy-MM-dd HH:mm:ss") ?? visitor.TestingTime.Value.ToString("yyyy-MM-dd HH:mm:ss"),
            };
            var setResult = await SetTestResultToPerson(data.LoginPayload.Session.Token, setResultRequest);
            return setResult[0][0].HttpStatusCode == 200;
        }
        private async Task<PlaceProviderSensitiveData> MakeSurePlaceProviderIsAuthenticated(string placeProviderId, IPlaceProviderRepository placeProviderRepository)
        {
            var data = await placeProviderRepository.GetPlaceProviderSensitiveData(placeProviderId);
            if (data.SessionValidity == null || data.SessionValidity.ValidThru.AddMinutes(10) < DateTimeOffset.Now)
            {
                // session is going to expire
                if (data.SessionValidity == null || data.SessionValidity.ValidThru.AddMinutes(10) < DateTimeOffset.Now)
                {
                    if (data.SessionValidity == null || data.SessionValidity.ValidThru.AddMinutes(1) < DateTimeOffset.Now)
                    {
                        // expired .. login again
                        data.LoginPayload = (await Authenticate(data.EZdravieUser, data.EZdraviePass))?.Payload;
                        if (string.IsNullOrEmpty(data.LoginPayload.User.Login))
                        {
                            throw new Exception("Unable to authenticate to ehealth");
                        }
                    }

                    // extend session
                    var extendSessionRequest = new ExtendSessionRequest()
                    {
                        AccessId = data.LoginPayload.Session.SessionId,
                        UserId = data.LoginPayload.User.Id
                    };
                    data.SessionValidity = await Extendsession(data.LoginPayload.Session.Token, extendSessionRequest);
                    if (data.SessionValidity == null)
                    {
                        data.SessionValidity = new ExtendSessionResponse()
                        {
                            ValidThru = data.LoginPayload.Session.ValidThru
                        };
                    }
                    if (data.SessionValidity.ValidThru.AddMinutes(1) < DateTimeOffset.Now)
                    {
                        throw new Exception("Unable to prolong the session");
                    }

                    await placeProviderRepository.SetPlaceProviderSensitiveData(data, false);
                }

            }
            return data;
        }
        public abstract Task<string> AddPersonToTestingPlace(string token, int cfid, DriveInRequest request);
        public abstract Task<LoginResponse> Authenticate(string user, string pass);
        public abstract Task<CheckResult> CheckPerson(string token, string vSearch_string);
        public abstract Task<DriveInQueueResponse> DriveInQueue(string token, DateTimeOffset date);
        public abstract Task<ExtendSessionResponse> Extendsession(string token, ExtendSessionRequest request);
        public abstract Task<PlaceDetailResponse> PlaceDetail(string token, DateTimeOffset date, string driveinId);
        public abstract Task<HttpStatus[][]> SetTestResultToPerson(string token, SetResultRequest request);
        public abstract Task<HttpStatus[][]> RegisterPerson(string token, RegisterPersonRequest registerPersonRequest);
    }
}
