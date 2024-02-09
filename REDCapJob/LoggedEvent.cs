using CsvHelper.Configuration.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace REDCapJob
{
    public enum EventType
    {
        EVENT_TYPE_CONSENT_SEND,
        EVENT_TYPE_UPCOMING_VISIT_EMAIL_24H,
        EVENT_TYPE_UPCOMING_VISIT_SMS_24H,
        EVENT_TYPE_UPCOMING_VISIT_SMS_15M,
        EVENT_TYPE_UPCOMING_VISIT_EMAIL_15M,
        EVENT_TYPE_DUPLICATE_PHONE
    }

    public class LoggedEvent
    {
        public string RecordID { get; set; }
        public string EmailOrSMS { get; set; }

        [Ignore]
        public EventType EventType { get; set; }

        [JsonIgnore]
        public string EventDescription
        {
            get
            {
                switch (EventType)
                {
                    case EventType.EVENT_TYPE_CONSENT_SEND:
                        return "Welcome E-mail Sent";

                    case EventType.EVENT_TYPE_UPCOMING_VISIT_EMAIL_24H:
                        return "24hr Visit E-mail Sent";

                    case EventType.EVENT_TYPE_UPCOMING_VISIT_SMS_24H:
                        return "24hr Visit SMS Sent";

                    case EventType.EVENT_TYPE_UPCOMING_VISIT_EMAIL_15M:
                        return "15 minute Visit Reminder E-mail Sent";

                    case EventType.EVENT_TYPE_UPCOMING_VISIT_SMS_15M:
                        return "15 minute Visit Reminder SMS Sent";

                    default:
                        return string.Empty;
                }
            }
        }

        public DateTime EventDate { get; set; }
        public DateTime? ScheduledDate { get; set; }
        public string SessionNumber { get; set; }
    }
}