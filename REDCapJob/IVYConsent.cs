namespace REDCapJob
{
    public class IVYConsent
    {
        public string record_id { get; set; }
        public string s01a07 { get; set; } // EmailAddy

        public string consent_p_date { get; set; }
        public string consent_part_sig { get; set; }
        public string consent_o_date { get; set; }
        public string consent_obtain_sig { get; set; }
        public string hippa_hiv { get; set; }
        public string hippa_pname { get; set; }
        public string hippa_psig { get; set; }
        public string hippa_pdate { get; set; }

        public string consent_complete
        {
            get; set;
        }
    }
}