using CovidMassTesting.Model.EZdravie.Payload;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CovidMassTesting.Model.EZdravie.Response
{
    public class PlaceDetailResponse : BaseResponse
    {
        public VisitorPayload[] Payload { get; set; }
    }
}
