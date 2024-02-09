using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace REDCapJob
{
    public class IVYPaymentInfo
    {
        public string record_id { get; set; }
        public string redcap_event_name { get; set; }

        public string s05a03 { get; set; }
        public string paid_date_1 { get; set; }
        public string session_number { get; set; }

        public int Session
        {
            get
            {
                try
                {
                    return int.Parse(session_number);
                }
                catch
                {
                    return -1;
                }
            }
        }

        public DateTime? CompletionDate
        {
            get
            {
                try
                {
                    return DateTime.Parse(s05a03);
                }
                catch
                {
                    return null;
                }
            }
        }

        public DateTime? PaidDate
        {
            get
            {
                try
                {
                    return DateTime.Parse(paid_date_1);
                }
                catch
                {
                    return null;
                }
            }
        }

        public override string ToString()
        {
            return record_id + "," + session_number + "," + (CompletionDate.HasValue ? CompletionDate.Value.ToShortDateString() : string.Empty) + "," + (PaidDate.HasValue ? PaidDate.Value.ToShortDateString() : string.Empty);
        }
    }
}