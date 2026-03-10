
using AuthProject.Settings;
using Microsoft.Extensions.Options;
using Twilio;
using Twilio.Rest.Api.V2010.Account;
using Twilio.Types;

namespace AuthProject.Services.SmsSevice
{
    public class SmsService : ISmsService
    {
        private readonly SmsSettings _smsSettings;

        public SmsService(IOptions<SmsSettings> options)
        {
            _smsSettings = options.Value;
        }

        public async Task SendSmsAsync(string phoneNumber, string message)
        {
            try
            {
                var accountSid = _smsSettings.TiwilioAccoundSid;
                var authToken = _smsSettings.TiwilioAuthToken;
                var twilioNumber = _smsSettings.TwilioPhoneNumber;
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
