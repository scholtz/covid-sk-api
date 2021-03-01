using CovidMassTesting.Model;
using CovidMassTesting.Model.EZdravie.Payload;
using CovidMassTesting.Model.EZdravie.Request;
using CovidMassTesting.Repository.Interface;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace CovidMassTesting.Connectors
{
    public interface IMojeEZdravie
    {
        /// <summary>
        /// Authenticase to eHealth
        /// </summary>
        /// <param name="user">username - You can obtain this from the eHealth support</param>
        /// <param name="pass">password</param>
        /// <returns></returns>
        public Task<Model.EZdravie.LoginResponse> Authenticate(string user, string pass);
        /// <summary>
        /// Check person. Returns null if person is not in eHealth
        /// </summary>
        /// <param name="token"></param>
        /// <param name="vSearch_string"></param>
        /// <returns></returns>
        public Task<Model.EZdravie.Payload.CheckResult> CheckPerson(string token, string vSearch_string);
        /// <summary>
        /// Extends session validity
        /// </summary>
        /// <param name="token"></param>
        /// <param name="request"></param>
        /// <returns></returns>
        public Task<Model.EZdravie.ExtendSessionResponse> Extendsession(string token, ExtendSessionRequest request);
        /// <summary>
        /// Get testing place information
        /// </summary>
        /// <param name="token"></param>
        /// <param name="date"></param>
        /// <returns></returns>
        public Task<Model.EZdravie.DriveInQueueResponse> DriveInQueue(string token, DateTimeOffset date);
        /// <summary>
        /// List registered users at specified place and date
        /// </summary>
        /// <param name="token"></param>
        /// <param name="date"></param>
        /// <param name="driveinId"></param>
        /// <returns></returns>
        public Task<Model.EZdravie.Response.PlaceDetailResponse> PlaceDetail(string token, DateTimeOffset date, string driveinId);
        /// <summary>
        /// Allocate person for testing at the testing place
        /// </summary>
        /// <param name="token"></param>
        /// <param name="cfid"></param>
        /// <param name="request"></param>
        /// <returns></returns>
        public Task<string> AddPersonToTestingPlace(string token, int cfid, DriveInRequest request);
        /// <summary>
        /// Set the test result
        /// </summary>
        /// <param name="token"></param>
        /// <param name="request"></param>
        /// <returns></returns>
        public Task<Model.EZdravie.Payload.HttpStatus[][]> SetTestResultToPerson(string token, SetResultRequest request);
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
        public Task<bool> SendResultToEHealth(
            Visitor visitor,
            string placeProviderId,
            IPlaceProviderRepository placeProviderRepository,
            IConfiguration configuration
            );
        /// <summary>
        /// Registers person to eHealth
        /// </summary>
        /// <param name="token"></param>
        /// <param name="registerPersonRequest"></param>
        /// <returns></returns>
        public Task<HttpStatus[][]> RegisterPerson(string token, RegisterPersonRequest registerPersonRequest);

        /// <summary>
        /// Downloads all visitors from all eHealth user locations to rychlejsie
        /// 
        /// Returns number of new visitors
        /// </summary>
        /// <param name="placeProviderId"></param>
        /// <param name="managerEmail"></param>
        /// <param name="day"></param>
        /// <param name="visitorRepository"></param>
        /// <param name="placeRepository"></param>
        /// <param name="slotRepository"></param>
        /// <returns></returns>
        public Task<int> DownloadEHealthVisitors(
            string placeProviderId,
            string managerEmail,
            DateTimeOffset day,
            IVisitorRepository visitorRepository,
            IPlaceRepository placeRepository,
            IPlaceProviderRepository placeProviderRepository,
            ISlotRepository slotRepository,
            ILoggerFactory loggerFactory
            );
    }
}
