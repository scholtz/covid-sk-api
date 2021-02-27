using CovidMassTesting.Model;
using CovidMassTesting.Model.EZdravie;
using CovidMassTesting.Model.EZdravie.Payload;
using CovidMassTesting.Model.EZdravie.Request;
using CovidMassTesting.Model.EZdravie.Response;
using CovidMassTesting.Repository.Interface;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using RestSharp;
using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CovidMassTesting.Connectors
{
    public class MojeEZdravieConnector : IMojeEZdravie
    {
        private RestSharp.RestClient client;
        private JsonSerializerSettings deserializeSettings;
        private readonly IPlaceProviderRepository placeProviderRepository;
        public MojeEZdravieConnector(IPlaceProviderRepository placeProviderRepository)
        {
            this.placeProviderRepository = placeProviderRepository;

            client = new RestSharp.RestClient("https://mojeezdravie.nczisk.sk/");
            deserializeSettings = new JsonSerializerSettings
            {
                ContractResolver = new DefaultContractResolver
                {
                    NamingStrategy = new SnakeCaseNamingStrategy(),
                },
            };
        }

        public async Task<bool> SendResultToEHealth(Visitor visitor, string placeProviderId)
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

                if (visitor.PersonType == "foreign")
                {
                    throw new Exception("Only residents supported right now");
                }
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
                SpecimenId = visitor.TestingSet,
                SpecimenCollectedAt = visitor.TestingTime.Value.ToString("yyyy-MM-dd HH:mm:ss"),
                ScreeningEndedAt = visitor.TestResultTime?.ToString("yyyy-MM-dd HH:mm:ss") ?? visitor.TestingTime.Value.ToString("yyyy-MM-dd HH:mm:ss"),
            };
            var setResult = await SetTestResultToPerson(data.LoginPayload.Session.Token, setResultRequest);
            return setResult[0][0].HttpStatusCode == 200;
        }


        public async Task<Model.EZdravie.LoginResponse> Authenticate(string user, string pass)
        {
            var request = new RestSharp.RestRequest("api/v1/login", RestSharp.Method.POST, RestSharp.DataFormat.Json);
            request.AddHeader("Authorization", $"Basic {Convert.ToBase64String(Encoding.UTF8.GetBytes($"{user}:{pass}"))}");
            string ret = "";
            await Task.Delay(500);
            var response = await client.ExecuteAsync(request);
            ret = response.Content;
            if (!response.IsSuccessful) throw new Exception(response.Content);
            return JsonConvert.DeserializeObject<Model.EZdravie.LoginResponse>(ret, deserializeSettings);
        }

        public async Task<ExtendSessionResponse> Extendsession(string token, ExtendSessionRequest extendSessionRequest)
        {
            var request = new RestSharp.RestRequest($"api/v1/extendsession", RestSharp.Method.POST, RestSharp.DataFormat.Json);
            request.AddHeader("Authorization", $"Bearer {token}");
            //request.AddJsonBody(extendSessionRequest);
            var body = Serialize(extendSessionRequest);
            request.AddParameter("application/json", body, ParameterType.RequestBody);
            await Task.Delay(500);
            var response = await client.ExecuteAsync(request);
            if (!response.IsSuccessful) throw new Exception(response.Content);
            return JsonConvert.DeserializeObject<Model.EZdravie.ExtendSessionResponse>(response.Content, deserializeSettings);
        }
        public async Task<Model.EZdravie.DriveInQueueResponse> DriveInQueue(string token, DateTimeOffset date)
        {
            var request = new RestSharp.RestRequest($"api/v1/ema/whiteboard/drivein-queue?date={date.ToString("yyyy-MM-dd")}", RestSharp.Method.GET, RestSharp.DataFormat.Json);
            request.AddHeader("Authorization", $"Bearer {token}");
            await Task.Delay(500);
            var response = await client.ExecuteAsync(request);
            if (!response.IsSuccessful) throw new Exception(response.Content);
            return JsonConvert.DeserializeObject<Model.EZdravie.DriveInQueueResponse>(response.Content, deserializeSettings);
        }

        public async Task<PlaceDetailResponse> PlaceDetail(string token, DateTimeOffset date, string driveinId)
        {
            var request = new RestSharp.RestRequest($"api/v1/ema/whiteboard/load_detail?date={date.ToString("yyyy-MM-dd")}", RestSharp.Method.GET, RestSharp.DataFormat.Json);
            request.AddHeader("Authorization", $"Bearer {token}");
            var body = Serialize(new DetailRequest() { DriveinId = driveinId });
            request.AddParameter("application/json", body, ParameterType.RequestBody);
            //request.AddJsonBody(new DetailRequest() { DriveinId = driveinId });
            await Task.Delay(500);
            var response = await client.ExecuteAsync(request);
            if (!response.IsSuccessful) throw new Exception(response.Content);
            return JsonConvert.DeserializeObject<Model.EZdravie.Response.PlaceDetailResponse>(response.Content, deserializeSettings);
        }

        public async Task<CheckResult> CheckPerson(string token, string vSearch_string)
        {
            var request = new RestSharp.RestRequest($"api/sp_v0/sp_covid_gp_check_person", RestSharp.Method.POST, RestSharp.DataFormat.Json);
            request.AddHeader("Authorization", $"Bearer {token}");
            request.AddParameter("vSearch_string", vSearch_string);
            await Task.Delay(500);
            var response = await client.ExecuteAsync(request);
            if (!response.IsSuccessful) return null;
            var jArr = JArray.Parse(response.Content);
            if (jArr.Count == 0) return null;
            var json = JsonConvert.SerializeObject(jArr[0]);

            var ret = JsonConvert.DeserializeObject<Model.EZdravie.Payload.CheckResult[]>(json, deserializeSettings);
            if (ret.Length == 0) return null;
            return ret[0];

        }


        public async Task<string> AddPersonToTestingPlace(string token, int cfid, DriveInRequest driveInRequest)
        {
            var request = new RestSharp.RestRequest($"api/covid_form_data/{cfid}", RestSharp.Method.PUT, RestSharp.DataFormat.Json);
            request.AddHeader("Authorization", $"Bearer {token}");
            //request.AddJsonBody(driveInRequest);
            var body = Serialize(driveInRequest);
            request.AddParameter("application/json", body, ParameterType.RequestBody);
            await Task.Delay(500);
            var response = await client.ExecuteAsync(request);
            if (!response.IsSuccessful) throw new Exception(response.Content);
            return response.Content;
        }

        public async Task<object> RegisterPerson(string token, RegisterPersonRequest registerPersonRequest)
        {
            var request = new RestSharp.RestRequest($"api/sp_v0/sp_covid_form_cc_iu", RestSharp.Method.POST, RestSharp.DataFormat.Json);
            request.AddHeader("Authorization", $"Bearer {token}");
            //request.AddJsonBody(setResultRequest);
            //var body = Serialize(setResultRequest);
            //request.AddParameter("application/json", body, ParameterType.RequestBody);
            request.AddParameter("nUser_id", registerPersonRequest.nUser_id);
            request.AddParameter("vPass", registerPersonRequest.vPass);
            request.AddParameter("vState", registerPersonRequest.vState);
            request.AddParameter("nTriage", registerPersonRequest.nTriage);
            request.AddParameter("vFirst_name", registerPersonRequest.vFirst_name);
            request.AddParameter("vLast_name", registerPersonRequest.vLast_name);
            request.AddParameter("vBirth_number", registerPersonRequest.vBirth_number);
            request.AddParameter("vNationality", registerPersonRequest.vNationality);
            request.AddParameter("vPatient_localization", registerPersonRequest.vPatient_localization);
            request.AddParameter("vEmail", registerPersonRequest.vEmail);
            request.AddParameter("vPhone_number", registerPersonRequest.vPhone_number);
            request.AddParameter("vEmail_secondary", registerPersonRequest.vEmail_secondary);
            request.AddParameter("vPhone_number_secondary", registerPersonRequest.vPhone_number_secondary);
            request.AddParameter("vMunicipality", registerPersonRequest.vMunicipality);
            request.AddParameter("vStreet", registerPersonRequest.vStreet);
            request.AddParameter("vStreet_number", registerPersonRequest.vStreet_number);
            request.AddParameter("vPostal_code", registerPersonRequest.vPostal_code);
            request.AddParameter("vCountry", registerPersonRequest.vCountry);
            request.AddParameter("vAddress_note", registerPersonRequest.vAddress_note);
            request.AddParameter("vTemporary_municipality", registerPersonRequest.vTemporary_municipality);
            request.AddParameter("vTemporary_street", registerPersonRequest.vTemporary_street);
            request.AddParameter("vTemporary_street_number", registerPersonRequest.vTemporary_street_number);
            request.AddParameter("vTemporary_postal_code", registerPersonRequest.vTemporary_postal_code);
            request.AddParameter("vTemporary_country", registerPersonRequest.vTemporary_country);
            request.AddParameter("vTemporary_address_note", registerPersonRequest.vTemporary_address_note);
            request.AddParameter("nHas_high_temperature", registerPersonRequest.nHas_high_temperature);
            request.AddParameter("nHas_continuous_cough", registerPersonRequest.nHas_continuous_cough);
            request.AddParameter("nHas_fatigue", registerPersonRequest.nHas_fatigue);
            request.AddParameter("nHas_head_aches", registerPersonRequest.nHas_head_aches);
            request.AddParameter("nHas_hinge_ache", registerPersonRequest.nHas_hinge_ache);
            request.AddParameter("nHas_cold", registerPersonRequest.nHas_cold);
            request.AddParameter("nHas_thrown_up", registerPersonRequest.nHas_thrown_up);
            request.AddParameter("nHas_heart_palpitation", registerPersonRequest.nHas_heart_palpitation);
            request.AddParameter("nHas_diarrhea", registerPersonRequest.nHas_diarrhea);
            request.AddParameter("nHas_ague", registerPersonRequest.nHas_ague);
            request.AddParameter("nHas_mucus_cough", registerPersonRequest.nHas_mucus_cough);
            request.AddParameter("nHas_dyspnoea", registerPersonRequest.nHas_dyspnoea);
            request.AddParameter("nHas_come_from_abroad", registerPersonRequest.nHas_come_from_abroad);
            request.AddParameter("nHas_influenza_vaccination", registerPersonRequest.nHas_influenza_vaccination);
            request.AddParameter("nHas_clinical_symptoms", registerPersonRequest.nHas_clinical_symptoms);
            request.AddParameter("vInfluenza_vaccine_name", registerPersonRequest.vInfluenza_vaccine_name);
            request.AddParameter("dInfluenza_vaccine_date", registerPersonRequest.dInfluenza_vaccine_date);
            request.AddParameter("vOther_symptoms", registerPersonRequest.vOther_symptoms);
            request.AddParameter("dInfection_start", registerPersonRequest.dInfection_start);
            request.AddParameter("vDrugs_used", registerPersonRequest.vDrugs_used);
            request.AddParameter("vSimilar_disease_occurence", registerPersonRequest.vSimilar_disease_occurence);
            request.AddParameter("vTravel_history", registerPersonRequest.vTravel_history);
            request.AddParameter("nPersons_count_at_home", registerPersonRequest.nPersons_count_at_home);
            request.AddParameter("nHome_rooms_number", registerPersonRequest.nHome_rooms_number);
            request.AddParameter("nContagion_concern_reasons", registerPersonRequest.nContagion_concern_reasons);
            request.AddParameter("vQuarantine_reason", registerPersonRequest.vQuarantine_reason);
            request.AddParameter("dQuarantine_start", registerPersonRequest.dQuarantine_start);
            request.AddParameter("dQuarantine_end", registerPersonRequest.dQuarantine_end);
            request.AddParameter("vQuarantine_country", registerPersonRequest.vQuarantine_country);
            request.AddParameter("vQuarantine_address_note", registerPersonRequest.vQuarantine_address_note);
            request.AddParameter("vHealth_insurance_company", registerPersonRequest.vHealth_insurance_company);
            request.AddParameter("nLongitude", registerPersonRequest.nLongitude);
            request.AddParameter("nLatitude", registerPersonRequest.nLatitude);
            request.AddParameter("nShare_my_location", registerPersonRequest.nShare_my_location);
            request.AddParameter("nSend_data_to_my_gp", registerPersonRequest.nSend_data_to_my_gp);
            request.AddParameter("vGp_name", registerPersonRequest.vGp_name);
            request.AddParameter("vGp_email", registerPersonRequest.vGp_email);
            request.AddParameter("vCar_plate_number", registerPersonRequest.vCar_plate_number);
            request.AddParameter("nDesignated_drivein_id", registerPersonRequest.nDesignated_drivein_id);
            request.AddParameter("vDesignated_drivein_city", registerPersonRequest.vDesignated_drivein_city);
            request.AddParameter("vDesignated_drivein_street_name", registerPersonRequest.vDesignated_drivein_street_name);
            request.AddParameter("vDesignated_drivein_street_number", registerPersonRequest.vDesignated_drivein_street_number);
            request.AddParameter("vDesignated_drivein_zip_code", registerPersonRequest.vDesignated_drivein_zip_code);
            request.AddParameter("nDesignated_drivein_longitude", registerPersonRequest.nDesignated_drivein_longitude);
            request.AddParameter("nDesignated_drivein_latitude", registerPersonRequest.nDesignated_drivein_latitude);
            request.AddParameter("dDesignated_drivein_scheduled_at", registerPersonRequest.dDesignated_drivein_scheduled_at);
            request.AddParameter("dDrivein_entered_at", registerPersonRequest.dDrivein_entered_at);
            request.AddParameter("dDrivein_left_at", registerPersonRequest.dDrivein_left_at);
            request.AddParameter("nAssesor_person_id", registerPersonRequest.nAssesor_person_id);
            request.AddParameter("dOperator_assessed_at", registerPersonRequest.dOperator_assessed_at);
            request.AddParameter("dMedical_assessed_at", registerPersonRequest.dMedical_assessed_at);
            request.AddParameter("nIs_smoking", registerPersonRequest.nIs_smoking);
            request.AddParameter("dFever_started_at", registerPersonRequest.dFever_started_at);
            request.AddParameter("dFever_ended_at", registerPersonRequest.dFever_ended_at);
            request.AddParameter("nHas_lost_sense_of_smell", registerPersonRequest.nHas_lost_sense_of_smell);
            request.AddParameter("nHas_lost_sense_of_taste", registerPersonRequest.nHas_lost_sense_of_taste);
            request.AddParameter("nHas_pneumonia", registerPersonRequest.nHas_pneumonia);
            request.AddParameter("nHas_hypertension", registerPersonRequest.nHas_hypertension);
            request.AddParameter("nHas_cardiovascular_disease", registerPersonRequest.nHas_cardiovascular_disease);
            request.AddParameter("nHas_diabetes", registerPersonRequest.nHas_diabetes);
            request.AddParameter("nHas_oncological_disease", registerPersonRequest.nHas_oncological_disease);
            request.AddParameter("vPerson_identification_number", registerPersonRequest.vPerson_identification_number);
            request.AddParameter("nHas_severe_health_disability", registerPersonRequest.nHas_severe_health_disability);
            request.AddParameter("vHealth_disability_id_card", registerPersonRequest.vHealth_disability_id_card);
            request.AddParameter("nHas_disability_pension", registerPersonRequest.nHas_disability_pension);
            request.AddParameter("vIce_first_name", registerPersonRequest.vIce_first_name);
            request.AddParameter("vIce_last_name", registerPersonRequest.vIce_last_name);
            request.AddParameter("vIce_email", registerPersonRequest.vIce_email);
            request.AddParameter("vIce_phone", registerPersonRequest.vIce_phone);
            request.AddParameter("vEntered_by_first_name", registerPersonRequest.vEntered_by_first_name);
            request.AddParameter("vEntered_by_last_name", registerPersonRequest.vEntered_by_last_name);
            request.AddParameter("vEntered_by_email", registerPersonRequest.vEntered_by_email);
            request.AddParameter("vEntered_by_phone", registerPersonRequest.vEntered_by_phone);
            request.AddParameter("nQuarantine_center_id", registerPersonRequest.nQuarantine_center_id);
            request.AddParameter("dBirth_date", registerPersonRequest.dBirth_date);
            request.AddParameter("vGender", registerPersonRequest.vGender);
            request.AddParameter("nHas_planned_operation", registerPersonRequest.nHas_planned_operation);
            request.AddParameter("dOperation_planned_at", registerPersonRequest.dOperation_planned_at);
            request.AddParameter("nOperation_planned_in_subject_id", registerPersonRequest.nOperation_planned_in_subject_id);
            request.AddParameter("vOperation_planned_in_name", registerPersonRequest.vOperation_planned_in_name);
            request.AddParameter("vHas_come_from_country", registerPersonRequest.vHas_come_from_country);
            request.AddParameter("dEntry_from_abroad_planned_at", registerPersonRequest.dEntry_from_abroad_planned_at);
            request.AddParameter("vPersonal_id", registerPersonRequest.vPersonal_id);
            request.AddParameter("vQuarantine_municipality", registerPersonRequest.vQuarantine_municipality);
            request.AddParameter("vQuarantine_street", registerPersonRequest.vQuarantine_street);
            request.AddParameter("vQuarantine_street_number", registerPersonRequest.vQuarantine_street_number);
            request.AddParameter("vQuarantine_postal_code", registerPersonRequest.vQuarantine_postal_code);
            request.AddParameter("nHas_been_exposed", registerPersonRequest.nHas_been_exposed);
            request.AddParameter("nIs_mom_user", registerPersonRequest.nIs_mom_user);

            await Task.Delay(500);
            var response = await client.ExecuteAsync(request);
            if (!response.IsSuccessful) throw new Exception(response.Content);
            return JsonConvert.DeserializeObject<Model.EZdravie.Payload.HttpStatus[][]>(response.Content, deserializeSettings);
        }

        public async Task<HttpStatus[][]> SetTestResultToPerson(string token, SetResultRequest setResultRequest)
        {
            var request = new RestSharp.RestRequest($"api/sp_v0/sp_covid_form_lab_order_and_result_iu", RestSharp.Method.POST, RestSharp.DataFormat.Json);
            request.AddHeader("Authorization", $"Bearer {token}");
            //request.AddJsonBody(setResultRequest);
            //var body = Serialize(setResultRequest);
            //request.AddParameter("application/json", body, ParameterType.RequestBody);

            request.AddParameter("nUser_id", setResultRequest.UserId);
            request.AddParameter("nId", setResultRequest.Id);
            request.AddParameter("nCovid_form_data_id", setResultRequest.CovidFormDataId);
            request.AddParameter("nLaboratory_order_number", setResultRequest.nLaboratoryOrderNumber);
            request.AddParameter("vDiagnosis", setResultRequest.Diagnosis);
            request.AddParameter("vFinal_result", setResultRequest.FinalResult);
            request.AddParameter("vOrdered_by_physician_code", setResultRequest.OrderedByPhysicianCode);
            request.AddParameter("vOrdered_by_physician_name", setResultRequest.OrderedByPhysicianName);
            request.AddParameter("vOrdered_by_address", setResultRequest.OrderedByAddress);
            request.AddParameter("vOrdered_by_phone", setResultRequest.OrderedByPhone);
            request.AddParameter("vOrdered_by_email", setResultRequest.OrderedByEmail);
            request.AddParameter("dOrdered_at", setResultRequest.OrderedAt);
            request.AddParameter("vOrdered_by_care_provider_code", setResultRequest.OrderedByCareProviderCode);
            request.AddParameter("vOrdered_by_care_provider_name", setResultRequest.OrderedByCareProviderName);
            request.AddParameter("vOrdered_by_care_provider_speciality", setResultRequest.OrderedByCareProviderSpeciality);
            request.AddParameter("vSpecimen_id", setResultRequest.SpecimenId);
            request.AddParameter("vSpecimen_type", setResultRequest.SpecimenType);
            request.AddParameter("vRequired_screening", setResultRequest.RequiredScreening);
            request.AddParameter("dSpecimen_collected_at", setResultRequest.SpecimenCollectedAt);
            request.AddParameter("dSpecimen_sent_at", setResultRequest.SpecimenSentAt);
            request.AddParameter("dSpecimen_received_at", setResultRequest.SpecimenReceivedAt);
            request.AddParameter("vSpecimen_number", setResultRequest.SpecimenNumber);
            request.AddParameter("dScreening_ended_at", setResultRequest.ScreeningEndedAt);
            request.AddParameter("vTested_by_care_provider_name", setResultRequest.TestedByCareProviderName);
            request.AddParameter("nQuantity_to", setResultRequest.QuantityTo);
            request.AddParameter("vMicrobiology_screening_type", setResultRequest.MicrobiologyScreeningType);
            request.AddParameter("vScreening_final_result", setResultRequest.ScreeningFinalResult);
            request.AddParameter("vScreening_final_comment", setResultRequest.ScreeningFinalComment);
            request.AddParameter("vTest_nclp", setResultRequest.TestNclp);
            request.AddParameter("vTest_loinc", setResultRequest.TestLoinc);
            request.AddParameter("vTest_title", setResultRequest.TestTitle);
            request.AddParameter("nIs_rapid_test", setResultRequest.IsRapidTest);

            await Task.Delay(500);
            var response = await client.ExecuteAsync(request);
            if (!response.IsSuccessful) throw new Exception(response.Content);
            return JsonConvert.DeserializeObject<Model.EZdravie.Payload.HttpStatus[][]>(response.Content, deserializeSettings);
        }
        private object Serialize<T>(T item)
        {
            return JsonConvert.SerializeObject(item);
        }
    }
}
