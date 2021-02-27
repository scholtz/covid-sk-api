using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CovidMassTesting.Model.EZdravie.Request
{
    public class SetResultRequest
    {
        [JsonProperty("nUser_id")]
        public int UserId { get; set; } = 0;
        [JsonProperty("nId")]
        public int Id { get; set; } = 0;
        [JsonProperty("nCovid_form_data_id")]
        public int CovidFormDataId { get; set; }
        [JsonProperty("nLaboratory_order_number")]
        public string nLaboratoryOrderNumber { get; set; }
        [JsonProperty("vDiagnosis")]
        public string Diagnosis { get; set; }
        [JsonProperty("vFinal_result")]
        public string FinalResult { get; set; }
        [JsonProperty("vOrdered_by_physician_code")]
        public string OrderedByPhysicianCode { get; set; }
        [JsonProperty("vOrdered_by_physician_name")]
        public string OrderedByPhysicianName { get; set; }
        [JsonProperty("vOrdered_by_address")]
        public string OrderedByAddress { get; set; }
        [JsonProperty("vOrdered_by_phone")]
        public string OrderedByPhone { get; set; }
        [JsonProperty("vOrdered_by_email")]
        public string OrderedByEmail { get; set; }
        [JsonProperty("dOrdered_at")]
        public string OrderedAt { get; set; }
        [JsonProperty("vOrdered_by_care_provider_code")]
        public string OrderedByCareProviderCode { get; set; }
        [JsonProperty("vOrdered_by_care_provider_name")]
        public string OrderedByCareProviderName { get; set; }
        [JsonProperty("vOrdered_by_care_provider_speciality")]
        public string OrderedByCareProviderSpeciality { get; set; }
        [JsonProperty("vSpecimen_id")]
        public string SpecimenId { get; set; }
        [JsonProperty("vSpecimen_type")]
        public string SpecimenType { get; set; }
        [JsonProperty("vRequired_screening")]
        public string RequiredScreening { get; set; }
        [JsonProperty("dSpecimen_collected_at")]
        public string SpecimenCollectedAt { get; set; }
        [JsonProperty("dSpecimen_sent_at")]
        public string SpecimenSentAt { get; set; }
        [JsonProperty("dSpecimen_received_at")]
        public string SpecimenReceivedAt { get; set; }
        [JsonProperty("vSpecimen_number")]
        public string SpecimenNumber { get; set; }
        [JsonProperty("dScreening_ended_at")]
        public string ScreeningEndedAt { get; set; }
        [JsonProperty("vTested_by_care_provider_name")]
        public string TestedByCareProviderName { get; set; }
        [JsonProperty("nQuantity_to")]
        public string QuantityTo { get; set; }
        [JsonProperty("vMicrobiology_screening_type")]
        public string MicrobiologyScreeningType { get; set; } = "AG";
        [JsonProperty("vScreening_final_result")]
        public string ScreeningFinalResult { get; set; } = "NEGATIVE";
        [JsonProperty("vScreening_final_comment")]
        public string ScreeningFinalComment { get; set; } = "";
        [JsonProperty("vTest_nclp")]
        public string TestNclp { get; set; } = "";
        [JsonProperty("vTest_loinc")]
        public string TestLoinc { get; set; } = "";
        [JsonProperty("vTest_title")]
        public string TestTitle { get; set; } = "";
        [JsonProperty("nIs_rapid_test")]
        public string IsRapidTest { get; set; } = "0";
    }
}
