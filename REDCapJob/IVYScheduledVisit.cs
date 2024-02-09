namespace REDCapJob
{
    public class IVYScheduledVisit
    {
        public string record_id { get; set; }
        public string redcap_event_name { get; set; }
        public string redcap_repeat_instrument { get; set; }
        public string redcap_repeat_instance { get; set; }
        public string session_number { get; set; }
        public string sch_01_date { get; set; }
        public string session_facilitator { get; set; }
        public string zoom { get; set; }
        public string paid_date_1 { get; set; }
        public string paid_amount_1 { get; set; }
        public string payment_details { get; set; }
        public string sched_notes_1 { get; set; }
        public string session_scheduling_and_payment_complete { get; set; }

        public DateTime? SessionDate
        {
            get { try { return DateTime.Parse(sch_01_date); } catch { return null; } }
        }

        public string ZoomLink
        {
            get
            {
                return "<a href='" + zoom + "'>Zoom Link</a>";
            }
        }

        public string SessionName
        {
            get
            {
                int sNum = int.Parse(session_number);
                if (sNum > 0 && sNum < 13)
                    return "Video Session " + sNum;
                else if (sNum == 0)
                    return "Team Visit";
                return session_number.ToString();
            }
        }
    }
}