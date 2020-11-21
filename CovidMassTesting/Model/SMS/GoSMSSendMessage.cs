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
        /// <summary>
        /// Message
        /// </summary>
#pragma warning disable IDE1006 // Naming Styles
        public string message { get; set; }
#pragma warning restore IDE1006 // Naming Styles
        /// <summary>
        /// recipients
        /// </summary>
#pragma warning disable IDE1006 // Naming Styles
        public string recipients { get; set; }
#pragma warning restore IDE1006 // Naming Styles
        /// <summary>
        /// Channel
        /// </summary>
#pragma warning disable IDE1006 // Naming Styles
        public int channel { get; set; }
#pragma warning restore IDE1006 // Naming Styles
    }
}
