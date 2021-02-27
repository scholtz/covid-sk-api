using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CovidMassTesting.Model.EZdravie.Request
{
    public class RegisterPersonRequest
    {
        public static RegisterPersonRequest FromVisitor(Visitor visitor, LoginPayload loginPayload)
        {
            return new RegisterPersonRequest()
            {
                vFirst_name = visitor.FirstName,
                vLast_name = visitor.LastName,
                vBirth_number = visitor.RC,
                vEmail = visitor.Email,
                vPhone_number = visitor.Phone,
                vMunicipality = visitor.City,
                vStreet = visitor.Street,
                vStreet_number = visitor.StreetNo,
                vPostal_code = visitor.ZIP,
                vHealth_insurance_company = visitor.Insurance,
                dBirth_date = $"{visitor.BirthDayYear}-{visitor.BirthDayMonth:D2}-{visitor.BirthDayDay:D2}",
                vEntered_by_first_name = loginPayload.User.FirstName,
                vEntered_by_last_name = loginPayload.User.LastName,
                vEntered_by_email = loginPayload.User.PrimaryEmail
            };
        }

        public int nUser_id { get; set; } = 0;
        public string vPass { get; set; }
        public string vState { get; set; }
        public string nTriage { get; set; }
        /// <summary>
        /// First name
        /// </summary>
        public string vFirst_name { get; set; }
        /// <summary>
        /// Last name
        /// </summary>
        public string vLast_name { get; set; }
        /// <summary>
        /// RC
        /// </summary>
        public string vBirth_number { get; set; }
        public string vNationality { get; set; }
        public string vPatient_localization { get; set; }
        /// <summary>
        /// Email
        /// </summary>
        public string vEmail { get; set; }
        /// <summary>
        /// Phone
        /// </summary>
        public string vPhone_number { get; set; }
        public string vEmail_secondary { get; set; }
        public string vPhone_number_secondary { get; set; }
        /// <summary>
        /// City
        /// </summary>
        public string vMunicipality { get; set; }
        /// <summary>
        /// Street
        /// </summary>
        public string vStreet { get; set; }
        /// <summary>
        /// Street no
        /// </summary>
        public string vStreet_number { get; set; }
        /// <summary>
        /// zip
        /// </summary>
        public string vPostal_code { get; set; }
        public string vCountry { get; set; }
        public string vAddress_note { get; set; }
        public string vTemporary_municipality { get; set; }
        public string vTemporary_street { get; set; }
        public string vTemporary_street_number { get; set; }
        public string vTemporary_postal_code { get; set; }
        public string vTemporary_country { get; set; }
        public string vTemporary_address_note { get; set; }
        public int nHas_high_temperature { get; set; } = 0;
        public int nHas_continuous_cough { get; set; } = 0;
        public int nHas_fatigue { get; set; } = 0;
        public int nHas_head_aches { get; set; } = 0;
        public int nHas_hinge_ache { get; set; } = 0;
        public int nHas_cold { get; set; } = 0;
        public int nHas_thrown_up { get; set; } = 0;
        public int nHas_heart_palpitation { get; set; } = 0;
        public int nHas_diarrhea { get; set; } = 0;
        public int nHas_ague { get; set; } = 0;
        public int nHas_mucus_cough { get; set; } = 0;
        public int nHas_dyspnoea { get; set; } = 0;
        public int nHas_come_from_abroad { get; set; } = 0;
        public int nHas_influenza_vaccination { get; set; } = 0;
        public int nHas_clinical_symptoms { get; set; } = 0;
        public string vInfluenza_vaccine_name { get; set; }
        public int dInfluenza_vaccine_date { get; set; } = 0;
        public string vOther_symptoms { get; set; }
        public string dInfection_start { get; set; }
        public string vDrugs_used { get; set; }
        public string vSimilar_disease_occurence { get; set; }
        public string vTravel_history { get; set; }
        public int? nPersons_count_at_home { get; set; }
        public int? nHome_rooms_number { get; set; }
        public int? nContagion_concern_reasons { get; set; }
        public string vQuarantine_reason { get; set; }
        public string dQuarantine_start { get; set; }
        public string dQuarantine_end { get; set; }
        public string vQuarantine_country { get; set; }
        public string vQuarantine_address_note { get; set; }
        /// <summary>
        /// Insurance company
        /// </summary>
        public string vHealth_insurance_company { get; set; }
        public string nLongitude { get; set; }
        public string nLatitude { get; set; }
        public string nShare_my_location { get; set; }
        public string nSend_data_to_my_gp { get; set; }
        public string vGp_name { get; set; }
        public string vGp_email { get; set; }
        public string vCar_plate_number { get; set; }
        public string nDesignated_drivein_id { get; set; }
        public string vDesignated_drivein_city { get; set; }
        public string vDesignated_drivein_street_name { get; set; }
        public string vDesignated_drivein_street_number { get; set; }
        public string vDesignated_drivein_zip_code { get; set; }
        public string nDesignated_drivein_longitude { get; set; }
        public string nDesignated_drivein_latitude { get; set; }
        public string dDesignated_drivein_scheduled_at { get; set; }
        public string dDrivein_entered_at { get; set; }
        public string dDrivein_left_at { get; set; }
        public string nAssesor_person_id { get; set; }
        public string dOperator_assessed_at { get; set; }
        public string dMedical_assessed_at { get; set; }
        public int nIs_smoking { get; set; } = 0;
        public string dFever_started_at { get; set; }
        public string dFever_ended_at { get; set; }
        public int nHas_lost_sense_of_smell { get; set; } = 0;
        public int nHas_lost_sense_of_taste { get; set; } = 0;
        public string nHas_pneumonia { get; set; }
        public string nHas_hypertension { get; set; }
        public string nHas_cardiovascular_disease { get; set; }
        public string nHas_diabetes { get; set; }
        public string nHas_oncological_disease { get; set; }
        public string vPerson_identification_number { get; set; }
        public string nHas_severe_health_disability { get; set; }
        public string vHealth_disability_id_card { get; set; }
        public string nHas_disability_pension { get; set; }
        public string vIce_first_name { get; set; }
        public string vIce_last_name { get; set; }
        public string vIce_email { get; set; }
        public string vIce_phone { get; set; }
        /// <summary>
        /// User name
        /// </summary>
        public string vEntered_by_first_name { get; set; }
        /// <summary>
        /// User last name
        /// </summary>
        public string vEntered_by_last_name { get; set; }
        /// <summary>
        /// user email
        /// </summary>
        public string vEntered_by_email { get; set; }
        public string vEntered_by_phone { get; set; }
        public string nQuarantine_center_id { get; set; }
        /// <summary>
        /// Birthday yyyy-MM-dd
        /// </summary>
        public string dBirth_date { get; set; }
        /// <summary>
        /// Gender - M | F
        /// </summary>
        public string vGender { get; set; }
        public string nHas_planned_operation { get; set; }
        public string dOperation_planned_at { get; set; }
        public string nOperation_planned_in_subject_id { get; set; }
        public string vOperation_planned_in_name { get; set; }
        public string vHas_come_from_country { get; set; }
        public string dEntry_from_abroad_planned_at { get; set; }
        public string vPersonal_id { get; set; }
        public string vQuarantine_municipality { get; set; }
        public string vQuarantine_street { get; set; }
        public string vQuarantine_street_number { get; set; }
        public string vQuarantine_postal_code { get; set; }
        public string nHas_been_exposed { get; set; }
        public string nIs_mom_user { get; set; }
    }
}
