using CsvHelper;
using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;
using Newtonsoft.Json;
using org.rti.cri.REDCapHelper;
using Renci.SshNet;
using System.Collections.Specialized;
using System.Data;
using System.Diagnostics;
using System.Globalization;
using System.Net;
using System.Net.Mail;
using System.Security.Cryptography;
using System.Text;
using System.Xml.Linq;
using Twilio;
using Twilio.Rest.Api.V2010.Account;
using Twilio.TwiML.Voice;
using static Org.BouncyCastle.Bcpg.Attr.ImageAttrib;

namespace REDCapJob
{
    public class Worker
    {
        private static async System.Threading.Tasks.Task SendAlertAsync(EmailInfo alert)
        {
            //Console.WriteLine(EmailWithMeetingRequest.SendEmail(alert.From, alert.To, alert.Subject, alert.Body));
            if (Singleton.Instance.HOLD_ALERTS)
                return;

            using var message = new MimeMessage();
            message.From.Add(new MailboxAddress("iVY Team", Singleton.Instance.FROM_ADDRESS));
            message.To.Add(new MailboxAddress(alert.To, alert.To));
            message.Subject = alert.Subject;
            var bodyBuilder = new BodyBuilder
            {
                HtmlBody = alert.Body
            };
            message.Body = bodyBuilder.ToMessageBody();

            using var client = new MailKit.Net.Smtp.SmtpClient();
            // SecureSocketOptions.StartTls force a secure connection over TLS
            await client.ConnectAsync("smtp.sendgrid.net", 587, SecureSocketOptions.StartTls);
            await client.AuthenticateAsync(
                userName: "apikey", // the userName is the exact string "apikey" and not the API key itself.
                password: "apikey" // password is the API key
            );

            await client.SendAsync(message);
            await client.DisconnectAsync(true);
        }

        public static List<LoggedEvent> ReadEvents()
        {
            string Filename = "logged-events.json";
            if (Directory.Exists(@"C:\Users\mham\Box\iVY REDCap\Job Log"))
            {
                Filename = @"C:\Users\mham\Box\iVY REDCap\Job Log\JobBackup\logged-events.json";
            }

            try
            {
                return JsonConvert.DeserializeObject<List<LoggedEvent>>(File.ReadAllText(Filename, Encoding.UTF8));
            }
            catch
            {
                return new List<LoggedEvent>();
            }
        }

        public static void SendConsentForms()
        {
            // Check for need to send PDF of consent
            //var consents = REDCapHelp.GetFormRecordsFromRedcapAPIIncludingMainIdentifier("consent", "record_id");

            // Possibly switch to people who have randomized?
            // et_a12rand != ""
            // from form randomization

            var randomized = GetRandomizationsFromREDCap();
            foreach (var p in randomized)
            {
                // Does this participant allow email?
                var contact = Singleton.Instance.IvyContactPrefs.SingleOrDefault(x => x.record_id == p.Key);
                if (contact.AcceptsEmails)
                {
                    LoggedEvent e = new LoggedEvent
                    {
                        RecordID = p.Key,
                        EmailOrSMS = contact.EmailAddress,
                        EventDate = DateTime.Now,
                        EventType = EventType.EVENT_TYPE_CONSENT_SEND
                    };

                    // Check to see if an event already exists with this record and this type
                    var ExistingEvent = Singleton.Instance.LoggedEvents.SingleOrDefault(x => x.RecordID == e.RecordID && x.EventType == EventType.EVENT_TYPE_CONSENT_SEND);
                    if (ExistingEvent == null)
                    {
                        // It's a new event
                        Singleton.Instance.LoggedEvents.Add(e);
                        Console.WriteLine("Sending welcome email to Record #" + e.RecordID);
                        //SendConsent(e.EmailOrSMS);
                        SendWelcomeEmail(e.EmailOrSMS, p.Value);
                        WriteEvents();
                    }
                }
            }

            /*
            var consents = GetConsentsFromREDCap();
            consents = consents.Where(x => x.consent_complete == "2").ToList();

            // Send consent forms to those that are completed
            foreach (var c in consents)
            {
                LoggedEvent e = new LoggedEvent
                {
                    RecordID = c.record_id.ToString(),
                    EmailOrSMS = c.s01a07.ToString(),
                    EventDate = DateTime.Now,
                    EventType = EventType.EVENT_TYPE_CONSENT_SEND
                };

                if (string.IsNullOrEmpty(e.EmailOrSMS))
                {
                    // We don't have permission to send email, so we have to ignore it
                    continue;
                }

                // Check to see if an event already exists with this record and this type
                var ExistingEvent = Singleton.Instance.LoggedEvents.SingleOrDefault(x => x.RecordID == e.RecordID && x.EventType == EventType.EVENT_TYPE_CONSENT_SEND);
                if (ExistingEvent == null)
                {
                    // It's a new event
                    Singleton.Instance.LoggedEvents.Add(e);
                    Console.WriteLine("Sending consent email to Record #" + e.RecordID);
                    SendConsent(e.EmailOrSMS);
                    WriteEvents();
                }
            }
            */
        }

        private static void SendWelcomeEmail(string EmailAddress, string RandomizationValue)
        {
            string interventionTable = "<table border=1 cellpadding=0 cellspacing=0 width=647><thead><tr><th style=font-size:larger>Timeline<th style=font-size:larger>Study Activities<th style=font-size:larger>Payment<tbody><tr><td style=background-color:#ee8aa8;text-align:center rowspan=5>Month 1-4<td style=background-color:#fad8e2;text-align:center>Survey 1<td style=background-color:#fad8e2;text-align:center>$40<tr><td style=background-color:#fad8e2;text-align:center>Home Test 1<td style=background-color:#fad8e2;text-align:center>$40 + $10 <em>if on time</em><tr><td style=background-color:#fad8e2;text-align:center>12 Weekly Counseling Sessions<td style=background-color:#fad8e2;text-align:center>$5 each<tr><td style=background-color:#fad8e2;text-align:center>Survey 2<td style=background-color:#fad8e2;text-align:center>$50<tr><td style=background-color:#fad8e2;text-align:center>Home Test 2<td style=background-color:#fad8e2;text-align:center>$40 + $10 <em>if on time</em><tr><td colspan=3> <tr><td style=background-color:#e68ae3;text-align:center rowspan=4>Month 5-8<td style=background-color:#f7d7f5;text-align:center>12 Weekly Counseling Sessions<td style=background-color:#f7d7f5;text-align:center>$5 each<tr><td style=background-color:#f7d7f5;text-align:center><em><strong>OR</strong></em> 3 Monthly Check ins<td style=background-color:#f7d7f5;text-align:center>$10 each<tr><td style=background-color:#f7d7f5;text-align:center>Survey 3<td style=background-color:#f7d7f5;text-align:center>$50<tr><td style=background-color:#f7d7f5;text-align:center>Home Test 3<td style=background-color:#f7d7f5;text-align:center>$40 + $10 <em>if on time</em><tr><td colspan=3> <tr><td style=background-color:#c1a5f8;text-align:center rowspan=3>Month 9-12<td style=background-color:#eae0fc;text-align:center>3 Monthly Check Ins<td style=background-color:#eae0fc;text-align:center>$10 each<tr><td style=background-color:#eae0fc;text-align:center>Survey 4<td style=background-color:#eae0fc;text-align:center>$40<tr><td style=background-color:#eae0fc;text-align:center>Home Test 4<td style=background-color:#eae0fc;text-align:center>$40 + $10 <em>if on time</em><tr><td style=background-color:#e9943a;text-align:center;color:#fff;font-weight:700 colspan=3>Finish Line!!</table>";
            string socTable = "<table border=1 cellpadding=0 cellspacing=0 width=647><thead><tr><th style=font-size:larger>Timeline<th style=font-size:larger>Study Activities<th style=font-size:larger>Payment<tbody><tr><td style=background-color:#ee8aa8;text-align:center rowspan=5>Month 1-4<td style=background-color:#fad8e2;text-align:center>Survey 1<td style=background-color:#fad8e2;text-align:center>$40<tr><td style=background-color:#fad8e2;text-align:center>Home Test 1<td style=background-color:#fad8e2;text-align:center>$40 + $10 <em>if on time</em><tr><td style=background-color:#fad8e2;text-align:center>3 Monthly Check ins<td style=background-color:#fad8e2;text-align:center>$10 each<tr><td style=background-color:#fad8e2;text-align:center>Survey 2<td style=background-color:#fad8e2;text-align:center>$50<tr><td style=background-color:#fad8e2;text-align:center>Home Test 2<td style=background-color:#fad8e2;text-align:center>$40 + $10 <em>if on time</em><tr><td colspan=3> <tr><td style=background-color:#e68ae3;text-align:center rowspan=3>Month 5-8<td style=background-color:#f7d7f5;text-align:center>3 Monthly Check ins<td style=background-color:#f7d7f5;text-align:center>$10 each<tr><td style=background-color:#f7d7f5;text-align:center>Survey 3<td style=background-color:#f7d7f5;text-align:center>$50<tr><td style=background-color:#f7d7f5;text-align:center>Home Test 3<td style=background-color:#f7d7f5;text-align:center>$40 + $10 <em>if on time</em><tr><td colspan=3> <tr><td style=background-color:#c1a5f8;text-align:center rowspan=3>Month 9-12<td style=background-color:#eae0fc;text-align:center>3 Monthly Check ins<td style=background-color:#eae0fc;text-align:center>$10 each<tr><td style=background-color:#eae0fc;text-align:center>Survey 4<td style=background-color:#eae0fc;text-align:center>$40<tr><td style=background-color:#eae0fc;text-align:center>Home Test 4<td style=background-color:#eae0fc;text-align:center>$40 + $10 <em>if on time</em><tr><td style=background-color:#e9943a;text-align:center;color:#fff;font-weight:700 colspan=3>Finish Line!!</table>";

            //          0   Control
            //          1   Intervention

            if (Singleton.Instance.HOLD_ALERTS)
                return;

            // TODO: If this fails, then hold on to the event
            //string IVYLogo = "https://ivy.ucsf.edu/sites/g/files/tkssra8636/f/IVY%20LOGO%20COLOUR%20BG%20TRANSP.png";
            //string Body = "<p>Thank you for participating in our study!<br/>Please find a copy of the consent form attached.<br/>If you have any questions, you may contact us at 415-735-1507 or ivy@ucsf.edu.</p>";
            //Body += "<p>Louis and Kristin<br/>Call/text: (415) 735-1507</p>";
            //Body += "<p><img src='" + IVYLogo + "' alt='iVY Logo' style='height:100px;' /></p>";

            string IVYLogo = "cid:logo";

            string Body = "<p>Welcome to the iVY Study!</p>";
            Body += "<p>A copy of your consent form is attached, and an overview of study activities is below.<br/>For feedback or assistance, please call/text 415-735-1507 or email ivy@ucsf.edu. Do not reply to this message.";
            Body += "<p>&nbsp;</p>";

            if (RandomizationValue == "0")
                Body += socTable;
            else if (RandomizationValue == "1")
                Body += interventionTable;

            Body += "<p>We look forward to working with you!</p>";
            Body += "<p style='color: #2F5597;'>iVY Team (Kristin, Louis, Celeste & Parya)<br/>Division of Prevention Science, University of California, San Francisco<br/>tel: 415-735-1507 (call/text)<br/>email: ivy@ucsf.edu</p>";
            Body += "<p><img src='" + IVYLogo + "' alt='iVY Logo'  /></p>";
            string msg = EmailWithMeetingRequest.SendEmail(new System.Net.Mail.MailAddress(Singleton.Instance.FROM_ADDRESS, "UCSF Team"), new System.Net.Mail.MailAddress[] { new System.Net.Mail.MailAddress(EmailAddress) }, "Welcome to the iVY Study", Body, new string[] { "iVY Consent Form.pdf", "ivy-logo.png" });
        }

        private static Dictionary<string, string> GetRandomizationsFromREDCap()
        {
            NameValueCollection parameters = new NameValueCollection
            {
                ["format"] = "json",
                ["content"] = "record",
                ["fields[0]"] = "record_id",
                ["fields[1]"] = "et_a12rand",
                ["exportDataAccessGroups"] = "true"
            };
            string v = REDCapHelp.GenericAPICall(parameters);
            var records = JsonConvert.DeserializeObject<List<dynamic>>(v);
            var randomized = records.Where(x => !string.IsNullOrEmpty(x.et_a12rand.ToString())).ToList();

            Dictionary<string, string> ToReturn = new Dictionary<string, string>();
            foreach (var record in randomized)
            {
                ToReturn[record.record_id.ToString()] = record.et_a12rand.ToString();
            }
            return ToReturn;
            // return randomized.Select(x => (string)x.record_id.ToString()).ToList();
        }

        private static void SendConsent(string EmailAddress)
        {
            if (Singleton.Instance.HOLD_ALERTS)
                return;

            // TODO: If this fails, then hold on to the event
            string IVYLogo = "https://ivy.ucsf.edu/sites/g/files/tkssra8636/f/IVY%20LOGO%20COLOUR%20BG%20TRANSP.png";
            string Body = "<p>Thank you for participating in our study!<br/>Please find a copy of the consent form attached.<br/>If you have any questions, you may contact us at 415-735-1507 or ivy@ucsf.edu.</p>";
            Body += "<p>Louis and Kristin<br/>Call/text: (415) 735-1507</p>";
            Body += "<p><img src='" + IVYLogo + "' alt='iVY Logo' style='height:100px;' /></p>";

            string msg = EmailWithMeetingRequest.SendEmail(new System.Net.Mail.MailAddress(Singleton.Instance.FROM_ADDRESS, "UCSF Team"), new System.Net.Mail.MailAddress[] { new System.Net.Mail.MailAddress(EmailAddress) }, "UCSF iVY Consent", Body, new string[] { "iVY Consent Form.pdf" });
        }

        private static List<IVYConsent> GetConsentsFromREDCap()
        {
            using WebClient patientWebClient = new WebClient();
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
            NameValueCollection data = new NameValueCollection
            {
                ["token"] = Singleton.Instance.PROD_IVY_API_KEY,
                ["content"] = "record",
                ["format"] = "json",
                ["forms[0]"] = "consent",
                ["fields[0]"] = "record_id",
                ["fields[1]"] = "s01a07",
                ["exportDataAccessGroups"] = "true"
            };
            byte[] array = patientWebClient.UploadValues(Singleton.Instance.API_URL, "POST", data);
            string json = Encoding.UTF8.GetString(array, 0, array.Length);
            return JsonConvert.DeserializeObject<List<IVYConsent>>(json);
        }

        private static void WriteEvents()
        {
            string Filename = "logged-events.json";
            if (Directory.Exists(@"C:\Users\mham\Box\iVY REDCap\Job Log"))
            {
                Filename = @"C:\Users\mham\Box\iVY REDCap\Job Log\JobBackup\logged-events.json";
            }

            Singleton.Instance.LoggedEvents = Singleton.Instance.LoggedEvents.OrderByDescending(x => x.EventDate).ToList();
            string json = JsonConvert.SerializeObject(Singleton.Instance.LoggedEvents, Formatting.Indented);
            File.WriteAllText(Filename, json, System.Text.Encoding.UTF8);
        }

        public static async System.Threading.Tasks.Task CheckForVideoCounselingAlertsAsync()
        {
            // Get all of the Scheduled Visits in the session_scheduling_and_payment form
            var jSchedForms = REDCapHelp.GetFormRecordsFromRedcapAPIIncludingMainIdentifier("session_scheduling_and_payment", "record_id", "json");
            var SchedForms = JsonConvert.DeserializeObject<List<IVYScheduledVisit>>(jSchedForms);
            var Scheduled = SchedForms.Where(x => !string.IsNullOrEmpty(x.sch_01_date)).ToList();
            var Upcoming = Scheduled.Where(x => x.SessionDate > Singleton.Instance.CurrentPT).ToList();

            foreach (var Visit in Upcoming)
            {
                // Check to see if it's within 24 hours of the appointment time?
                if (Visit.SessionDate.Value.AddDays(-1) < Singleton.Instance.CurrentPT)
                {
                    //The visit is upcoming(within 1 day)
                    //Check to see if we have already sent an alert and also see if the scheduled time has changed

                    //Emails will not work with a ucsf address :(
                    await Check24HrEmailAlertAsync(Visit);

                    Check24HrSMSAlert(Visit);
                }

                if (Visit.SessionDate.Value.AddMinutes(-15) < Singleton.Instance.CurrentPT)
                {
                    Check15MinSMSAlert(Visit);

                    await Check15MinEmailAlertAsync(Visit);
                }
            }
        }

        private static async System.Threading.Tasks.Task Check15MinEmailAlertAsync(IVYScheduledVisit Visit)
        {
            var EmailAlert = Singleton.Instance.LoggedEvents.SingleOrDefault(x => x.RecordID == Visit.record_id && x.SessionNumber == Visit.redcap_repeat_instance && x.EventType == EventType.EVENT_TYPE_UPCOMING_VISIT_EMAIL_15M);
            if (EmailAlert == null)
            {
                // Does this person have email alerts enabled?
                var Contact = Singleton.Instance.IvyContactPrefs.SingleOrDefault(x => x.record_id == Visit.record_id);
                if (Contact.AcceptsEmails)
                {
                    // We need to send the alert and log it
                    LoggedEvent e = new LoggedEvent
                    {
                        RecordID = Contact.record_id,
                        EmailOrSMS = Contact.EmailAddress,
                        EventType = EventType.EVENT_TYPE_UPCOMING_VISIT_EMAIL_15M,
                        EventDate = DateTime.Now,
                        ScheduledDate = Visit.SessionDate,
                        SessionNumber = Visit.redcap_repeat_instance
                    };
                    Singleton.Instance.LoggedEvents.Add(e);
                    Console.WriteLine("Sending 15 minute visit email for Session #" + e.SessionNumber + " to Record #" + e.RecordID);

                    EmailInfo Alert = new EmailInfo
                    {
                        From = "noreply@ivy-ucsf.edu",
                        To = Contact.EmailAddress,
                        Subject = "UCSF Appt Reminder",
                        Body = "<p>Hi,<p>This is a reminder that your USCF " + Visit.SessionName + " is starting soon at " + e.ScheduledDate.Value.ToShortTimeString() + ". <p>Join using this link: " + Visit.ZoomLink + "<p>To reschedule please call/text 415-735-1507 or email ivy@ucsf.edu. Do not reply to this message.<p>See you soon!<br>-UCSF Team"
                    };
                    await SendAlertAsync(Alert);

                    WriteEvents();
                }
            }
            else
            {
                // The Alert already has been sent. Check to see if the appointment time has changed?
                if (Visit.SessionDate != EmailAlert.ScheduledDate)
                {
                    // Make sure they *still* accept text messages
                    var Contact = Singleton.Instance.IvyContactPrefs.SingleOrDefault(x => x.record_id == Visit.record_id);

                    if (Contact.AcceptsEmails)
                    {
                        // Update the saved alert?
                        EmailAlert.ScheduledDate = Visit.SessionDate;
                        WriteEvents();

                        // Send it again?
                        Console.WriteLine("Sending a new 15 minute visit email for Session #" + EmailAlert.SessionNumber + " to Record #" + EmailAlert.RecordID);
                        EmailInfo Alert = new EmailInfo
                        {
                            From = "noreply@ivy-ucsf.edu",
                            To = Contact.EmailAddress,
                            Subject = "UCSF Appt Reminder",
                            Body = "<p>Hi,<p>This is a reminder that your USCF " + Visit.SessionName + " is starting soon at " + EmailAlert.ScheduledDate.Value.ToShortTimeString() + ". <p>Join using this link: " + Visit.ZoomLink + "<p>To reschedule please call/text 415-735-1507 or email ivy@ucsf.edu. Do not reply to this message.<p>See you soon!<br>-UCSF Team"
                        };
                        await SendAlertAsync(Alert);
                    }
                }
            }
        }

        private static void Check15MinSMSAlert(IVYScheduledVisit Visit)
        {
            var SMSAlert = Singleton.Instance.LoggedEvents.SingleOrDefault(x => x.RecordID == Visit.record_id && x.SessionNumber == Visit.redcap_repeat_instance && x.EventType == EventType.EVENT_TYPE_UPCOMING_VISIT_SMS_15M);
            if (SMSAlert == null)
            {
                // Does this person have SMS alerts enabled?
                var Contact = Singleton.Instance.IvyContactPrefs.SingleOrDefault(x => x.record_id == Visit.record_id);
                if (Contact.AcceptsTexts)
                {
                    // We need to send the alert and log it
                    LoggedEvent e = new LoggedEvent
                    {
                        RecordID = Contact.record_id,
                        EmailOrSMS = Contact.PhoneNumber,
                        EventType = EventType.EVENT_TYPE_UPCOMING_VISIT_SMS_15M,
                        EventDate = DateTime.Now,
                        ScheduledDate = Visit.SessionDate,
                        SessionNumber = Visit.redcap_repeat_instance
                    };
                    Singleton.Instance.LoggedEvents.Add(e);
                    Console.WriteLine("Sending 15 minute visit alert for Session #" + e.SessionNumber + " to Record #" + e.RecordID);

                    string Body = "USCF Team: Appointment Reminder: See you soon at " + e.ScheduledDate.Value.ToShortTimeString() + ".\n\nJoin using this link: " + Visit.zoom + "\n\nPlease call/text 415-735-1507 if you need to reschedule.";
                    SendSMS(Contact.PhoneNumber, Body);
                    WriteEvents();
                }
            }
            else
            {
                if (Visit.SessionDate != SMSAlert.ScheduledDate)
                {
                    // Make sure they *still* accept text messages
                    var Contact = Singleton.Instance.IvyContactPrefs.SingleOrDefault(x => x.record_id == Visit.record_id);

                    if (Contact.AcceptsTexts)
                    {
                        // Update the saved alert?
                        SMSAlert.ScheduledDate = Visit.SessionDate;
                        WriteEvents();

                        // Send it again?
                        Console.WriteLine("Sending a new 15 minute visit alert for Session #" + SMSAlert.SessionNumber + " to Record #" + SMSAlert.RecordID);
                        string Body = "USCF Team: Appointment Reminder: See you soon at " + SMSAlert.ScheduledDate.Value.ToShortTimeString() + ".\n\nJoin using this link: " + Visit.zoom + "\n\nPlease call/text 415-735-1507 if you need to reschedule.";
                        SendSMS(Contact.PhoneNumber, Body);
                    }
                }
            }
        }

        private static void Check24HrSMSAlert(IVYScheduledVisit Visit)
        {
            var SMSAlert = Singleton.Instance.LoggedEvents.SingleOrDefault(x => x.RecordID == Visit.record_id && x.SessionNumber == Visit.redcap_repeat_instance && x.EventType == EventType.EVENT_TYPE_UPCOMING_VISIT_SMS_24H);
            if (SMSAlert == null)
            {
                // Does this person have SMS alerts enabled?
                var Contact = Singleton.Instance.IvyContactPrefs.SingleOrDefault(x => x.record_id == Visit.record_id);
                if (Contact.AcceptsTexts)
                {
                    // We need to send the alert and log it
                    LoggedEvent e = new LoggedEvent
                    {
                        RecordID = Contact.record_id,
                        EmailOrSMS = Contact.PhoneNumber,
                        EventType = EventType.EVENT_TYPE_UPCOMING_VISIT_SMS_24H,
                        EventDate = DateTime.Now,
                        ScheduledDate = Visit.SessionDate,
                        SessionNumber = Visit.redcap_repeat_instance
                    };
                    Singleton.Instance.LoggedEvents.Add(e);
                    Console.WriteLine("Sending 24 hour visit alert for Session #" + e.SessionNumber + " to Record #" + e.RecordID);

                    string Body = "USCF Team: Appointment Reminder: Your " + Visit.SessionName + " is scheduled for " + e.ScheduledDate.Value.ToLongDateString() + " at " + e.ScheduledDate.Value.ToShortTimeString() + ". \n\nPlease call/text 415-735-1507 if you need to reschedule.";
                    SendSMS(Contact.PhoneNumber, Body);
                    WriteEvents();
                }
            }
            else
            {
                // The Alert already has been sent. Check to see if the appointment time has changed?
                if (Visit.SessionDate != SMSAlert.ScheduledDate)
                {
                    // Make sure they *still* accept text messages
                    var Contact = Singleton.Instance.IvyContactPrefs.SingleOrDefault(x => x.record_id == Visit.record_id);

                    if (Contact.AcceptsTexts)
                    {
                        // Update the saved alert?
                        SMSAlert.ScheduledDate = Visit.SessionDate;
                        WriteEvents();

                        // Send it again?
                        Console.WriteLine("Sending a new 24 hour visit alert for Session #" + SMSAlert.SessionNumber + " to Record #" + SMSAlert.RecordID);
                        string Body = "USCF Team: Appointment Reminder: Your " + Visit.SessionName + " is scheduled for " + SMSAlert.ScheduledDate.Value.ToLongDateString() + " at " + SMSAlert.ScheduledDate.Value.ToShortTimeString() + ". \n\nPlease call/text 415-735-1507 if you need to reschedule.";
                        SendSMS(Contact.PhoneNumber, Body);
                    }
                }
            }
        }

        private static async System.Threading.Tasks.Task Check24HrEmailAlertAsync(IVYScheduledVisit Visit)
        {
            var EmailAlert = Singleton.Instance.LoggedEvents.SingleOrDefault(x => x.RecordID == Visit.record_id && x.SessionNumber == Visit.redcap_repeat_instance && x.EventType == EventType.EVENT_TYPE_UPCOMING_VISIT_EMAIL_24H);
            if (EmailAlert == null)
            {
                // Does this person have email alerts enabled?
                var Contact = Singleton.Instance.IvyContactPrefs.SingleOrDefault(x => x.record_id == Visit.record_id);
                if (Contact.AcceptsEmails)
                {
                    // We need to send the alert and log it
                    LoggedEvent e = new LoggedEvent
                    {
                        RecordID = Contact.record_id,
                        EmailOrSMS = Contact.EmailAddress,
                        EventType = EventType.EVENT_TYPE_UPCOMING_VISIT_EMAIL_24H,
                        EventDate = DateTime.Now,
                        ScheduledDate = Visit.SessionDate,
                        SessionNumber = Visit.redcap_repeat_instance
                    };
                    Singleton.Instance.LoggedEvents.Add(e);
                    Console.WriteLine("Sending visit email for Session #" + e.SessionNumber + " to Record #" + e.RecordID);

                    string body = "<p>Hi,</p><p>This is a reminder that your USCF " + Visit.SessionName + " is scheduled for " + e.ScheduledDate.Value.ToLongDateString() + " at " + e.ScheduledDate.Value.ToShortTimeString() + ".</p><p> Join using this link: " + Visit.ZoomLink + "</p><p>To reschedule please call/text 415-735-1507 or email ivy@ucsf.edu. Do not reply to this message.</p><p>See you soon!<br>-UCSF Team</p>";
                    EmailInfo Alert = new EmailInfo
                    {
                        From = "noreply@ivy-ucsf.edu",
                        To = Contact.EmailAddress,
                        Subject = "UCSF Appt Reminder",
                        Body = body
                    };
                    await SendAlertAsync(Alert);

                    WriteEvents();
                }
            }
            else
            {
                // The Alert already has been sent. Check to see if the appointment time has changed?
                if (Visit.SessionDate != EmailAlert.ScheduledDate)
                {
                    // Make sure they *still* accept text messages
                    var Contact = Singleton.Instance.IvyContactPrefs.SingleOrDefault(x => x.record_id == Visit.record_id);

                    if (Contact.AcceptsEmails)
                    {
                        // Update the saved alert?
                        EmailAlert.ScheduledDate = Visit.SessionDate;
                        WriteEvents();

                        // Send it again?
                        Console.WriteLine("Sending a new 24 hour visit email for Session #" + EmailAlert.SessionNumber + " to Record #" + EmailAlert.RecordID);
                        string body = "<p>Hi,</p><p>This is a reminder that your USCF " + Visit.SessionName + " is scheduled for " + EmailAlert.ScheduledDate.Value.ToLongDateString() + " at " + EmailAlert.ScheduledDate.Value.ToShortTimeString() + ".</p><p>Join using this link: " + Visit.ZoomLink + "</p><p>To reschedule please call/text 415-735-1507 or email ivy@ucsf.edu. Do not reply to this message.</p><p>See you soon!<br>-UCSF Team</p>";

                        EmailInfo Alert = new EmailInfo
                        {
                            From = "noreply@ivy-ucsf.edu",
                            To = Contact.EmailAddress,
                            Subject = "UCSF Appt Reminder",
                            Body = body
                        };
                        await SendAlertAsync(Alert);
                    }
                }
            }
        }

        private static void SendSMS(string ToNumber, string Body)
        {
            if (Singleton.Instance.HOLD_ALERTS)
                return;
            SMSInfo SMS = new SMSInfo
            {
                From = "+14157353665",
                To = ToNumber,
                Body = Body
            };
            SendSMS(SMS);
        }

        private static void SendSMS(SMSInfo SMS)
        {
            string accountSid = "TwilioSid";
            string authToken = "TwilioToken";

            TwilioClient.Init(accountSid, authToken);

            var message = MessageResource.Create(body: SMS.Body, from: new Twilio.Types.PhoneNumber(SMS.From), to: new Twilio.Types.PhoneNumber(SMS.To));
        }

        public static void ImportContactInfoFromScreener()
        {
            // Get a list of screener IDs
            REDCapHelp.Initialize(Singleton.Instance.API_URL, Singleton.Instance.PROD_SCREENING_API_KEY);
            var ScreeningIDs = REDCapHelp.GetListOfIDs("record_id");

            REDCapHelp.Initialize(Singleton.Instance.API_URL, Singleton.Instance.PROD_IVY_API_KEY);
            var IvyScreeningIDs = REDCapHelp.GetListOfIDs("et_a01");

            var NewScreeningIDs = ScreeningIDs.Except(IvyScreeningIDs).ToList();

            REDCapHelp.Initialize(Singleton.Instance.API_URL, Singleton.Instance.PROD_SCREENING_API_KEY);

            var sFullScreeningForms = REDCapHelp.GetFormRecordsFromRedcapAPIIncludingMainIdentifier("full_screener", "record_id", "json");
            var FullScreeningForms = JsonConvert.DeserializeObject<List<IVYFullScreener>>(sFullScreeningForms);

            foreach (var ScreeningID in NewScreeningIDs)
            {
                // Get the Full Screener for each new screening ID
                var FSForm = FullScreeningForms.SingleOrDefault(x => x.record_id == ScreeningID);

                if (FSForm.IsFullyEligible)
                {
                    Console.WriteLine("Attempting to push over screening ID #" + ScreeningID);
                    IVYContactInfo info = new IVYContactInfo(FSForm);
                    CreateNewIVYRecord(info);
                }
            }

            REDCapHelp.Initialize(Singleton.Instance.API_URL, Singleton.Instance.PROD_IVY_API_KEY);
        }

        private static void CreateNewIVYRecord(IVYContactInfo info)
        {
            string RecordsAsJSON = "[" + JsonConvert.SerializeObject(info) + "]";

            // Convert nulls to blanks?
            RecordsAsJSON = RecordsAsJSON.Replace("null", "\"\"");

            using (WebClient patientWebClient = new WebClient())
            {
                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;

                NameValueCollection data = new NameValueCollection
                {
                    ["token"] = Singleton.Instance.PROD_IVY_API_KEY,
                    ["content"] = "record",
                    ["format"] = "json",
                    ["forceAutoNumber"] = "true",
                    ["data"] = RecordsAsJSON
                };
                byte[] array = patientWebClient.UploadValues(Singleton.Instance.API_URL, "POST", data);
                string results = Encoding.UTF8.GetString(array, 0, array.Length);
            }
        }

        internal static void WriteEventsCSV()
        {
            Singleton.Instance.LoggedEvents = Singleton.Instance.LoggedEvents.OrderByDescending(x => x.EventDate).ToList();

            var TempEvents = new List<LoggedEvent>();
            TempEvents.AddRange(Singleton.Instance.LoggedEvents);

            for (int i = 0; i < TempEvents.Count; i++)
            {
                // Adjust for Pacific Time
                TempEvents[i].EventDate = TempEvents[i].EventDate.AddHours(-3);
            }

            using (var writer = new StreamWriter("C:\\Users\\mham\\Box\\iVY REDCap\\Job Log\\JobLog.csv"))
            using (var csv = new CsvWriter(writer, CultureInfo.InvariantCulture))
            {
                csv.WriteRecords(TempEvents);
            }
        }

        internal static void CreatePaymentLog()
        {
            REDCapHelp.Initialize(Singleton.Instance.API_URL, Singleton.Instance.PROD_IVY_API_KEY);

            NameValueCollection parameters = new NameValueCollection
            {
                ["token"] = Singleton.Instance.PROD_IVY_API_KEY,
                ["content"] = "record",
                ["format"] = "json",
                ["fields[0]"] = "record_id",
                ["fields[1]"] = "s05a03",
                ["fields[2]"] = "paid_date_1",
                ["fields[3]"] = "session_number"
            };

            string sResults = REDCapHelp.GenericAPICall(parameters);
            List<IVYPaymentInfo> Results = JsonConvert.DeserializeObject<List<IVYPaymentInfo>>(sResults);
            Results.RemoveAll(x => x.record_id == "1"); // Remove the test case. We don't care about it.

            // Now we need to match up the Payment info across the different events
            var ToBeCombined = Results.Where(x => x.redcap_event_name == "enrollment_arm_1" && !string.IsNullOrEmpty(x.session_number)).ToList();

            foreach (var item in ToBeCombined)
            {
                // Find the matching record and fill in the missing date of completion if it exists

                string MatchingEvent = "farty";

                if (item.Session > 0 && item.Session < 12)
                {
                    MatchingEvent = "week_" + item.Session + "_arm_1";
                }
                else if (item.Session >= 25 && item.Session <= 36)
                {
                    MatchingEvent = "week_" + (item.Session - 8).ToString() + "_arm_1";
                }
                else if (item.Session >= 13 && item.Session <= 24)
                {
                    int week = item.Session - 12;
                    week *= 4;
                    MatchingEvent = "week_" + (week).ToString() + "_arm_1";
                }

                var MissingItem = Results.SingleOrDefault(x => x.record_id == item.record_id && x.redcap_event_name == MatchingEvent);
                if (MissingItem != null)
                {
                    item.s05a03 = MissingItem.s05a03;
                }
            }

            var CombinedData = new List<IVYPaymentInfo>();
            CombinedData.AddRange(ToBeCombined);
            CombinedData = CombinedData.OrderBy(x => x.paid_date_1).ToList();
            CombinedData = CombinedData.Where(x => x.CompletionDate.HasValue).ToList();

            var Missing = CombinedData.Where(x => x.CompletionDate.HasValue && !x.PaidDate.HasValue).ToList();

            StringBuilder sb = new StringBuilder("Record ID,Session Number,Completion Date,Paid Date" + Environment.NewLine);
            foreach (var item in CombinedData)
            {
                sb.AppendLine(item.ToString());
            }

            // Enrollment tracking form - check for paid date
            var EnrollmentTracking = REDCapHelp.GetFormRecordsFromRedcapAPIIncludingMainIdentifier("enrollment_tracking", "record_id");
            EnrollmentTracking = EnrollmentTracking.Where(x => x.redcap_event_name.ToString() == "enrollment_arm_1").ToList();
            // Baseline Surveys done (intersectional_discrimination_index_complete)
            NameValueCollection sIDIParams = new NameValueCollection
            {
                ["token"] = Singleton.Instance.PROD_IVY_API_KEY,
                ["content"] = "record",
                ["format"] = "json",
                ["forms[0]"] = "intersectional_discrimination_index",
                ["fields[0]"] = "record_id",
                ["exportSurveyFields"] = "true"
            };
            var sIDI = REDCapHelp.GenericAPICall(sIDIParams);
            var IDI = JsonConvert.DeserializeObject<List<dynamic>>(sIDI);
            IDI = IDI.Where(x => x.redcap_event_name.ToString() == "enrollment_arm_1").ToList();
            IDI = IDI.Where(x => !string.IsNullOrEmpty(x.intersectional_discrimination_index_timestamp.ToString())).ToList();

            //EnrollmentTracking = EnrollmentTracking.Where(x => x.enrollment_tracking_complete.ToString() == "2").ToList();
            foreach (dynamic item in EnrollmentTracking)
            {
                // Date paid: et_a29
                var IDIRecord = IDI.SingleOrDefault(x => x.record_id.ToString() == item.record_id.ToString());
                if (IDIRecord != null)
                    sb.AppendLine(item.record_id.ToString() + "," + "Enrollment Tracking" + "," + IDIRecord.intersectional_discrimination_index_timestamp.ToString() + "," + item.et_a29.ToString());
            }

            // Monthly Check-In Form - Check for paid date
            parameters = new NameValueCollection
            {
                ["token"] = Singleton.Instance.PROD_IVY_API_KEY,
                ["content"] = "record",
                ["format"] = "json",
                ["fields[0]"] = "record_id",
                ["fields[1]"] = "mchk_a0",// Monthly Checkin Date
                ["fields[2]"] = "paid_date_1",
                ["fields[3]"] = "session_number"
            };
            sResults = REDCapHelp.GenericAPICall(parameters);
            var aresults = JsonConvert.DeserializeObject<List<dynamic>>(sResults);
            var mcresults = aresults.Where(x => !string.IsNullOrEmpty(x.mchk_a0.ToString())).ToList();

            // Match the session date with the monthly check-in week
            foreach (dynamic item in mcresults)
            {
                int week = int.Parse(item.redcap_event_name.ToString().Split('_')[1]);
                int session = week / 4;
                session += 12;
                var sessionRec = aresults.SingleOrDefault(x => x.record_id.ToString() == item.record_id.ToString() && x.session_number == session);
                if (sessionRec != null)
                {
                    sb.AppendLine(item.record_id.ToString() + "," + "Monthly Check-In Week " + week + "," + item.mchk_a0.ToString() + "," + sessionRec.paid_date_1.ToString());
                }
            }

            // TODO: Finish this. Hard to test now given the fact that no one has reached week 16 yet.
            // 16/32/48 Week Assessment Form (16: unmet sub, 32: mobile tech, 48: sleep)
            parameters = new NameValueCollection
            {
                ["token"] = Singleton.Instance.PROD_IVY_API_KEY,
                ["content"] = "record",
                ["format"] = "json",
                ["fields[0]"] = "record_id",
                ["forms[0]"] = "unmet_subsistence_needs_and_instrumental_support",
                ["forms[1]"] = "mobile_technology_vulnerability_scale",
                ["forms[2]"] = "sleep",
                ["exportSurveyFields"] = "true"
            };
            sResults = REDCapHelp.GenericAPICall(parameters);
            aresults = JsonConvert.DeserializeObject<List<dynamic>>(sResults);
            var w16 = aresults.Where(x => x.redcap_event_name.ToString() == "week_16_arm_1").ToList();

            File.WriteAllText("C:\\Users\\mham\\Box\\iVY REDCap\\Job Log\\PaymentLog.csv", sb.ToString(), new UTF8Encoding(false));
        }

        internal static async System.Threading.Tasks.Task GeneratePotentialDuplicatesLogAsync()
        {
            REDCapHelp.Initialize(Singleton.Instance.API_URL, Singleton.Instance.PROD_SCREENING_API_KEY);
            NameValueCollection parameters = new NameValueCollection
            {
                ["format"] = "json",
                ["content"] = "record",
                ["fields[0]"] = "record_id",
                ["fields[1]"] = "bsa09b",
                ["fields[2]"] = "fs01b",
                ["fields[3]"] = "fs01_legallast",
                ["fields[4]"] = "fs01_legalfirst",
                ["fields[5]"] = "bsa09a",
                ["fields[6]"] = "fs01",
                ["fields[7]"] = "bsa11",
                ["fields[8]"] = "fs03",
                ["exportDataAccessGroups"] = "true"
            };
            string r = REDCapHelp.GenericAPICall(parameters);
            var records = JsonConvert.DeserializeObject<List<IVYScreenerNames>>(r);
            var NameRecords = new List<IVYScreenerNames>();
            NameRecords.AddRange(records);
            var PhoneRecords = new List<IVYScreenerNames>();
            PhoneRecords.AddRange(records);
            NameRecords.RemoveAll(x => x.LastName == null); // Remove any where there is no name, because we can't test it anyway.
            PhoneRecords.RemoveAll(x => string.IsNullOrEmpty(x.bsa11) && string.IsNullOrEmpty(x.fs03));
            List<string> NameMatches = new List<string>();
            List<string> PhoneMatches = new List<string>();

            foreach (var ThisRecord in NameRecords)
            {
                var PossibleMatches = NameRecords.Where(x => x.LastName.Trim().ToLower() == ThisRecord.LastName.Trim().ToLower()).ToList();
                PossibleMatches.AddRange(NameRecords.Where(x => x.LastName.Trim().ToLower() == ThisRecord.FirstName.Trim().ToLower()).ToList());
                PossibleMatches.RemoveAll(x => x.record_id == ThisRecord.record_id);

                if (PossibleMatches.Count > 0)
                {
                    List<string> Match = new List<string>
                    {
                        ThisRecord.record_id
                    };
                    Match.AddRange(PossibleMatches.Select(x => x.record_id));
                    Match.Sort();

                    string joined = string.Join(',', Match);

                    if (!NameMatches.Contains(joined))
                        NameMatches.Add(joined);
                }
            }

            foreach (var ThisRecord in PhoneRecords)
            {
                // No need to check records with no phone numbers
                if (string.IsNullOrEmpty(ThisRecord.bsa11) && string.IsNullOrEmpty(ThisRecord.fs03))
                    continue;

                if (!string.IsNullOrEmpty(ThisRecord.bsa11))
                {
                    var Dupes = PhoneRecords.Where(x => (x.record_id != ThisRecord.record_id)).ToList();
                    Dupes = Dupes.Where(x => x.bsa11 == ThisRecord.bsa11 || x.fs03 == ThisRecord.bsa11).ToList();

                    if (Dupes.Count > 0)
                    {
                        List<string> Match = new List<string> { ThisRecord.record_id };
                        Match.AddRange(Dupes.Select(x => x.record_id));
                        Match.Sort();

                        string joined = string.Join(',', Match);

                        if (!PhoneMatches.Contains(joined))
                            PhoneMatches.Add(joined);
                    }
                }

                if (!string.IsNullOrEmpty(ThisRecord.fs03))
                {
                    var Dupes = PhoneRecords.Where(x => (x.record_id != ThisRecord.record_id)).ToList();
                    Dupes = Dupes.Where(x => x.fs03 == ThisRecord.fs03 || x.bsa11 == ThisRecord.fs03).ToList();

                    if (Dupes.Count > 0)
                    {
                        List<string> Match = new List<string> { ThisRecord.record_id };
                        Match.AddRange(Dupes.Select(x => x.record_id));
                        Match.Sort();

                        string joined = string.Join(',', Match);

                        if (!PhoneMatches.Contains(joined))
                            PhoneMatches.Add(joined);
                    }
                }
            }

            // Add any potential matches to a spreadsheet I guess?
            StringBuilder sb = new StringBuilder();

            if (NameMatches.Count == 0)
            {
                sb.Append("No Potential Name Duplicates Found!");
                sb.AppendLine();
            }
            else
            {
                sb = new StringBuilder("Potential Name Duplicates" + Environment.NewLine + "IDs,Names" + Environment.NewLine);
                foreach (var m in NameMatches)
                {
                    var rec = m.Split(',');
                    var recs = NameRecords.Where(x => rec.Contains(x.record_id)).ToList();

                    sb.Append("\"" + m.Replace(",", ", ") + "\"");
                    sb.Append(",");
                    sb.Append("\"");

                    foreach (var reco in recs)
                    {
                        sb.Append(reco.FullName + ", ");
                    }

                    sb.Remove(sb.Length - 2, 2);
                    sb.Append("\"");
                    sb.AppendLine();
                }
            }

            sb.AppendLine();

            if (PhoneMatches.Count == 0)
            {
                sb.Append("No Potential Phone Number Duplicates Found!");
                sb.AppendLine();
            }
            else
            {
                sb.Append("Potential Phone Number Duplicates" + Environment.NewLine + "IDs,Numbers" + Environment.NewLine);
                foreach (var m in PhoneMatches)
                {
                    LoggedEvent e = new LoggedEvent
                    {
                        RecordID = m.Replace(",", " & "),
                        EventDate = DateTime.Now,
                        EventType = EventType.EVENT_TYPE_DUPLICATE_PHONE
                    };
                    // Check to see if an event already exists with this record and this type
                    var ExistingEvent = Singleton.Instance.LoggedEvents.SingleOrDefault(x => x.RecordID == m.Replace(",", " & ") && x.EventType == EventType.EVENT_TYPE_DUPLICATE_PHONE);
                    if (ExistingEvent == null)
                    {
                        Singleton.Instance.LoggedEvents.Add(e);
                        Console.WriteLine("Sending alert about duplicate phone number for records: " + e.RecordID);

                        EmailInfo Alert = new EmailInfo
                        {
                            From = "noreply@ivy-ucsf.edu",
                            To = "ivy@ucsf.org",
                            Subject = "Duplicate Phone Number found!",
                            Body = "The REDCap Job has found duplicate phone numbers in the records: " + e.RecordID
                        };

                        await SendAlertAsync(Alert);
                        WriteEvents();
                    }

                    var rec = m.Split(',');
                    var recs = PhoneRecords.Where(x => rec.Contains(x.record_id)).ToList();

                    sb.Append("\"" + m.Replace(",", ", ") + "\"");
                    sb.Append(",");
                    sb.Append("\"");

                    foreach (var reco in recs)
                    {
                        sb.Append(reco.PhoneNumber + ", ");
                    }

                    sb.Remove(sb.Length - 2, 2);
                    sb.Append("\"");
                    sb.AppendLine();
                }
            }
            File.WriteAllText("C:\\Users\\mham\\Box\\iVY REDCap\\Job Log\\PossibleDuplicates.csv", sb.ToString(), new UTF8Encoding(false));
        }

        internal static void UpdateFullScreenerFromBriefScreener()
        {
            // Get brief screener availablity and push into full notes
            // bsa15 != "" and fs24
            REDCapHelp.Initialize(Singleton.Instance.API_URL, Singleton.Instance.PROD_SCREENING_API_KEY);
            NameValueCollection parameters = new NameValueCollection
            {
                ["format"] = "json",
                ["content"] = "record",
                ["fields[0]"] = "record_id",
                ["fields[1]"] = "bsa15",
                ["fields[2]"] = "fs24",
            };
            string json = REDCapHelp.GenericAPICall(parameters);
            var Records = JsonConvert.DeserializeObject<List<dynamic>>(json);
            var ToChange = Records.Where(x => string.IsNullOrEmpty(x.fs24.ToString()) && !string.IsNullOrEmpty(x.bsa15.ToString())).ToList();
            foreach (var record in ToChange)
            {
                record.fs24 = record.bsa15.ToString();
            }
            json = JsonConvert.SerializeObject(ToChange);
            REDCapHelp.SendRecordsToRedcapAPI(json);

            // Get brief screener "How did you hear about this study?" and push into full screener
            // bsa01 = 7 and fs02___7 = 0 => Push bsa01a into fs2a
            parameters = new NameValueCollection
            {
                ["format"] = "json",
                ["content"] = "record",
                ["fields[0]"] = "record_id",
                ["fields[1]"] = "bsa01",
                ["fields[2]"] = "fs02",
                ["fields[3]"] = "bsa01a",
                ["fields[4]"] = "fs2a",
            };
            json = REDCapHelp.GenericAPICall(parameters);
            Records = JsonConvert.DeserializeObject<List<dynamic>>(json);
            ToChange = Records.Where(x => x.bsa01.ToString() == "7" && x.fs02___7.ToString() == "0").ToList();
            foreach (var record in ToChange)
            {
                record.fs02___7 = "1";
                record.fs2a = record.bsa01a.ToString();
            }
            json = JsonConvert.SerializeObject(ToChange);
            REDCapHelp.SendRecordsToRedcapAPI(json);
        }

        internal static void CheckUSPS()
        {
            //•	The outbound tracking number tracks the package sent from UCSF to the Participant.
            //•	The Inbound tracking number tracks the package sent from the participant to the lab.

            // We're looking for missing data in REDCap
            // hem1_a01 -> Date delivered to PT
            // hem1_a12 -> Date received by Kantor
            // hem_a08 -> Inbound tracking #
            // hem_a07 -> Outbound tracking #
            var AllHemaspots = Hemaspots();
            var MissingPTDate = AllHemaspots.Where(x => string.IsNullOrEmpty(x.hem1_a01) && !string.IsNullOrEmpty(x.hem_a07)).ToList();
            var MissingKantorDate = AllHemaspots.Where(x => string.IsNullOrEmpty(x.hem1_a12) && !string.IsNullOrEmpty(x.hem_a08)).ToList();

            foreach (var md in MissingPTDate)
            {
                // Try and use the Tracking Number to see if it's been delivered
                string status = GetPackageStatus(md.OutboundTracking);
                if (status.StartsWith("Delivered"))
                {
                    // Add the delivery date to this record
                }
            }

            foreach (var md in MissingKantorDate)
            {
                // Try and use the Tracking Number to see if it's been delivered
                string status = GetPackageStatus(md.Tracking1);
                if (status.StartsWith("Delivered"))
                {
                    // Add the delivery date to this record
                }
            }
        }

        private static string GetPackageStatus(string USPSTrackingNumber)
        {
            string url = "https://www.bing.com/packagetrackingv2?packNum=" + USPSTrackingNumber + "&carrier=usps";
            string html = new WebClient().DownloadString(url);
            html = html.Replace("b_focusTextSmall\">", "☯");
            html = html.Split('☯')[1];
            html = html.Split('<')[0];
            return html;
        }

        private static List<HemaspotForm> Hemaspots()
        {
            Dictionary<int, string> APITokens = new Dictionary<int, string>();

            REDCapHelp.Initialize(Singleton.Instance.API_URL, Singleton.Instance.PROD_IVY_API_KEY);
            var report = REDCapHelp.GetReportFromRedcapAPI(182586, "json");
            var Hemaspots = JsonConvert.DeserializeObject<List<HemaspotForm>>(report);

            foreach (var h in Hemaspots.Where(x => !string.IsNullOrEmpty(x.Tracking1)))
            {
                var h2 = Hemaspots.SingleOrDefault(x => x.Tracking2 == h.Tracking1);
                if (h2 != null)
                {
                    h.hem1_a08 = h2.hem1_a08;
                    h.hem1_a12 = h2.hem1_a12;
                    h.hem1_a05 = h2.hem1_a05;
                    h.hem1_a23 = h2.hem1_a23;
                    h.hem1_a01 = h2.hem1_a01;
                    h.InstanceF1 = h.redcap_repeat_instance;
                    h.InstanceF2 = h2.redcap_repeat_instance;
                }
            }

            Hemaspots = Hemaspots.Where(x => x.InstanceF1 > 0).ToList();

            return Hemaspots;
        }
    } // Worker
} // Namespace