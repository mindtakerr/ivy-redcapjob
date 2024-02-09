using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;
using Newtonsoft.Json;
using org.rti.cri.REDCapHelper;
using System.Net;

namespace REDCapJob
{
    public class Program
    {
        private static async Task Main(string[] args)
        {
            int WAIT_TIME_MINUTES = 3;

            REDCapHelp.Initialize(Singleton.Instance.API_URL, Singleton.Instance.PROD_IVY_API_KEY);

            // Create full addresses for missing data
            //string jCon = REDCapHelp.GetFormRecordsFromRedcapAPIIncludingMainIdentifier("contact_information", "record_id", "json");
            //var cons = JsonConvert.DeserializeObject<List<IVYContactInfo>>(jCon);

            //for (int i = 0; i < cons.Count; i++)
            //{
            //    if (string.IsNullOrEmpty(cons[i].s01a10_address) && !string.IsNullOrEmpty(cons[i].s01a10a))
            //    {
            //        cons[i].s01a10_address = IVYContactInfo.BuiltAddress(cons[i]);
            //    }
            //}

            //jCon = JsonConvert.SerializeObject(cons);
            //REDCapHelp.SendRecordsToRedcapAPI(jCon);

            while (true)
            {
                // Get Contact Information
                var jIvyContacts = REDCapHelp.GetFormRecordsFromRedcapAPI("contact_information", "json");
                Singleton.Instance.IvyContactPrefs = JsonConvert.DeserializeObject<List<IVYContactPrefs>>(jIvyContacts);
                Singleton.Instance.IvyContactPrefs = Singleton.Instance.IvyContactPrefs.Where(x => x.redcap_event_name == "enrollment_arm_1").ToList();

                try
                {
                    Worker.CheckUSPS();
                    Worker.UpdateFullScreenerFromBriefScreener();
                    await Worker.GeneratePotentialDuplicatesLogAsync();
                    Worker.CreatePaymentLog();
                    Worker.ImportContactInfoFromScreener();

                    // Get all of the currently logged events
                    Singleton.Instance.LoggedEvents = Worker.ReadEvents();
                    Worker.SendConsentForms();
                    await Worker.CheckForVideoCounselingAlertsAsync();

                    if (WAIT_TIME_MINUTES < 1)
                        break;

                    Console.WriteLine("Job done at " + DateTime.Now.AddHours(-3).ToString("M/d/yyyy H:mm:ss") + " PT. Waiting for " + WAIT_TIME_MINUTES + " minutes.");
                    File.WriteAllText("C:\\Users\\mham\\Box\\iVY REDCap\\Job Log\\JobLogUpdated.txt", "Job last ran successfully at " + DateTime.Now.AddHours(-3).ToString());
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Job failed at " + DateTime.Now.AddHours(-3).ToString("M/d/yyyy H:mm:ss") + " PT. Waiting for " + (WAIT_TIME_MINUTES * 2) + " minutes.");
                    Thread.Sleep(3 * 1000 * 60);
                }
                finally
                {
                    Worker.WriteEventsCSV();

                    Thread.Sleep(WAIT_TIME_MINUTES * 1000 * 60);
                }
            }
        }

        private static void SendPDF(byte[] pdfFile, string EmailAddress)
        {
            File.WriteAllBytes("ivy_consent.pdf", pdfFile);
            EmailWithMeetingRequest.SendEmail(new System.Net.Mail.MailAddress(Singleton.Instance.FROM_ADDRESS, "UCSF Team"), new System.Net.Mail.MailAddress[] { new System.Net.Mail.MailAddress(EmailAddress) }, "UCSF iVY Consent", "Please find your signed copy of the iVY consent form attached.", new string[] { "ivy_consent.pdf" });
        }

        private static byte[] GetPDF(string RecordID, string Event, string FormName)
        {
            var rParams = new System.Collections.Specialized.NameValueCollection();
            rParams["content"] = "pdf";
            rParams["record"] = RecordID;
            rParams["event"] = Event;
            rParams["instrument"] = FormName;
            rParams["token"] = Singleton.Instance.PROD_IVY_API_KEY;
            //REDCapHelp.GenericAPICall(rParams);

            using WebClient patientWebClient = new WebClient();
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;

            byte[] bytes = patientWebClient.UploadValues(Singleton.Instance.API_URL, "POST", rParams);
            return bytes;
        }

        private static void SendEmail(EmailInfo Email)
        {
            string result = "";
            using (var client = new WebClient())
            {
                string JSON = JsonConvert.SerializeObject(Email);
                client.Headers[HttpRequestHeader.ContentType] = "application/json";
                result = client.UploadString(@"https://gntransition.rti.org/api/EmailController/SendEmail", "POST", JSON);
            }
        }
    }
}