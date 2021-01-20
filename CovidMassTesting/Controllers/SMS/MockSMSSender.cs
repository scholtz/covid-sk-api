using CovidMassTesting.Model.SMS;
using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;

namespace CovidMassTesting.Controllers.SMS
{
    /// <summary>
    /// If no email sender is defined
    /// </summary>
    public class MockSMSSender : ISMSSender
    {
        /// <summary>
        /// For unit tests
        /// </summary>
        public ConcurrentDictionary<long, (string toPhone, ISMS data)> Data { get; private set; } = new ConcurrentDictionary<long, (string toPhone, ISMS data)>();

        /// <summary>
        /// Act as sms was sent. Log event to console
        /// </summary>
        /// <param name="toPhone"></param>
        /// <param name="data"></param>
        /// <returns></returns>
        public async Task<bool> SendSMS(string toPhone, ISMS data)
        {
            if (string.IsNullOrEmpty(toPhone))
            {
                throw new System.ArgumentException($"'{nameof(toPhone)}' cannot be null or empty", nameof(toPhone));
            }

            if (data is null)
            {
                throw new System.ArgumentNullException(nameof(data));
            }

            var msg = data.GetText();
            System.Console.WriteLine($"SMS: {Newtonsoft.Json.JsonConvert.SerializeObject(data)}");

            await Task.Delay(1);
            Data[DateTimeOffset.Now.Ticks] = (toPhone, data);
            await Task.Delay(1);
            return true;
        }
    }
}
