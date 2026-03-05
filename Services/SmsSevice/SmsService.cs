
using Twilio;
using Twilio.Rest.Api.V2010.Account;
using Twilio.Types;

namespace AuthProject.Services.SmsSevice
{
    public class SmsService : ISmsService
    {
        private readonly IConfiguration _config;

        public SmsService(IConfiguration config)
        {
            _config = config;
        }

        public async Task SendSmsAsync(string phoneNumber, string message)
        {
            try
            {
                var accountSid = _config["SmsProvider:TiwilioAccoundSid"];
                var authToken = _config["SmsProvider:TiwilioAuthToken"];
                var twilioNumber = _config["SmsProvider:TwilioPhoneNumber"];
                TwilioClient.Init(accountSid, authToken);

                MessageResource.Create(
                    from: twilioNumber,
                    to: phoneNumber,
                    body: message);
                Console.WriteLine("Mesaj gönderildi.");
            }
            catch(Exception ex)
            {
                Console.WriteLine($"SMS gönderim hatası: {ex.Message}");
            }
        }
    }
}
