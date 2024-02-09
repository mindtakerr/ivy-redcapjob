namespace REDCapJob
{
    public class IVYContactPrefs
    {
        public string record_id { get; set; }
        public string redcap_event_name { get; set; }

        public string s01a05___1 { get; set; }
        public string s01a05___2 { get; set; }
        public string s01a05___3 { get; set; }
        public string s01a05___4 { get; set; }

        public bool AcceptsTexts
        {
            get
            {
                return s01a05___2 == "1";
            }
        }

        public bool AcceptsEmails
        {
            get
            {
                return s01a05___4 == "1";
            }
        }

        public string s01a06 { get; set; }
        public string s01a07 { get; set; }

        public string EmailAddress
        {
            get
            {
                return s01a07;
            }
        }

        public string PhoneNumber
        {
            get
            {
                return s01a06;
            }
        }
    }
}