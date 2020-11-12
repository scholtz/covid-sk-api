using CovidMassTesting.Model.SMS;
using System.Threading.Tasks;

namespace CovidMassTesting.Controllers.SMS
{
    /// <summary>
    /// If no email sender is defined
    /// </summary>
    public class MockSMSSender : ISMSSender
    {

        /// <summary>
        /// Act as sms was sent. Log event to console
        /// </summary>
        /// <param name="toPhone"></param>
        /// <param name="data"></param>
        /// <returns></returns>
        public async Task<bool> SendSMS(string toPhone, ISMS data)
        {
            var msg = data.GetText();
            System.Console.WriteLine($"SMS: {msg}");
            await Task.Delay(1);
            return true;
        }
    }
}
