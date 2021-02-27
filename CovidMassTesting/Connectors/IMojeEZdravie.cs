using CovidMassTesting.Model;
using CovidMassTesting.Model.EZdravie.Request;
using System;
using System.Threading.Tasks;

namespace CovidMassTesting.Connectors
{
    public interface IMojeEZdravie
    {
        public Task<Model.EZdravie.LoginResponse> Authenticate(string user, string pass);
        public Task<Model.EZdravie.Payload.CheckResult> CheckPerson(string token, string vSearch_string);
        public Task<Model.EZdravie.ExtendSessionResponse> Extendsession(string token, ExtendSessionRequest request);
        public Task<Model.EZdravie.DriveInQueueResponse> DriveInQueue(string token, DateTimeOffset date);
        public Task<Model.EZdravie.Response.PlaceDetailResponse> PlaceDetail(string token, DateTimeOffset date, string driveinId);
        public Task<string> AddPersonToTestingPlace(string token, int cfid, DriveInRequest request);
        public Task<Model.EZdravie.Payload.HttpStatus[][]> SetTestResultToPerson(string token, SetResultRequest request);
        public Task<bool> SendResultToEHealth(Visitor visitor, string placeProviderId);
    }
}
