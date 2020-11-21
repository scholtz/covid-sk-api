using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CovidMassTesting.Model.SMS
{
    /// <summary>
    /// Generic sms 
    /// </summary>
    public class Message : ISMS
    {
        private readonly string text;
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="text"></param>
        public Message(string text)
        {
            this.text = text;
        }
        /// <summary>
        /// Returns sms text
        /// </summary>
        /// <returns></returns>
        public override string GetText()
        {
            return text;
        }
    }
}
