using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CovidMassTesting.Model.EZdravie
{
    public class UserPayload
    {
        public string AttId { get; set; }
        public string AvatarUrl { get; set; }
        public int EmployeeId { get; set; }
        public string FirstName { get; set; }
        public string FullName { get; set; }
        public string FullNameNoTitles { get; set; }
        public int Id { get; set; }
        public string LastName { get; set; }
        public string Login { get; set; }
        public string OrgUnitId { get; set; }
        public string PersonId { get; set; }
        public string PrimaryEmail { get; set; }
        public string PrimaryPhone { get; set; }
        public string TitleAfterNameUi { get; set; }
        public string TitleBeforeNameUi { get; set; }
    }
}
