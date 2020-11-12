using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CovidMassTesting.Model.SMS
{
    /// <summary>
    /// https://doc.gosms.cz/#jak-poslat-zpravu
    /// 
    /// curl -X POST "https://app.gosms.cz/api/v1/messages/" \
    ///  -H "Content-Type: application/json" \
    ///  -H "Authorization: Bearer {token}" \
    ///  -d '{"message":"Hello","recipients":"111222333","channel":1}'
    /// </summary>
    public class GoSMSSendMessage
    {
        public string message { get; set; }
        public string recipients { get; set; }
        public int channel { get; set; }
    }
}
