using CovidMassTesting.Model.EZdravie.Payload;

namespace CovidMassTesting.Model.EZdravie
{
    public class DriveInQueueResponse : BaseResponse
    {
        public PlacePayload[] Payload { get; set; }
    }
}
