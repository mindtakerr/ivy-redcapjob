namespace REDCapJob
{
    internal class IVYScreenerNames
    {
        public string record_id { get; set; }
        public string bsa09b { get; set; }
        public string fs01b { get; set; }
        public string fs01_legallast { get; set; }
        public string fs01_legalfirst { get; set; }
        public string bsa09a { get; set; }
        public string fs01 { get; set; }
        public string bsa11 { get; set; } // Phone Number
        public string fs03 { get; set; } // Phone Number

        public string LastName
        {
            // Generate the name based on starting with legal if available and then going to preferred and then brief
            get
            {
                if (!string.IsNullOrEmpty(fs01_legallast))
                    return fs01_legallast;
                if (!string.IsNullOrEmpty(fs01b))
                    return fs01b;
                if (!string.IsNullOrEmpty(bsa09b))
                    return bsa09b;
                return null;
            }
        }

        public string FirstName
        {
            // Generate the name based on starting with legal if available and then going to preferred and then brief
            get
            {
                if (!string.IsNullOrEmpty(fs01_legalfirst))
                    return fs01_legalfirst;
                if (!string.IsNullOrEmpty(fs01))
                    return fs01;
                if (!string.IsNullOrEmpty(bsa09a))
                    return bsa09a;
                return null;
            }
        }

        public string FullName
        {
            get
            {
                return FirstName + " " + LastName;
            }
        }

        public string BriefScreenerPhoneNumber
        {
            get
            {
                string numbers = new string(bsa11.Where(c => char.IsDigit(c)).ToArray());
                long pNumber = long.Parse(numbers);
                return string.Format("{0:(###) ###-####}", pNumber);
            }
        }

        public string FullScreenerPhoneNumber
        {
            get
            {
                string numbers = new string(fs03.Where(c => char.IsDigit(c)).ToArray());
                long pNumber = long.Parse(numbers);
                return string.Format("{0:(###) ###-####}", pNumber);
            }
        }

        public string PhoneNumber
        {
            get
            {
                if (!string.IsNullOrEmpty(fs03))
                    return FullScreenerPhoneNumber;
                if (!string.IsNullOrEmpty(bsa11))
                    return BriefScreenerPhoneNumber;
                return string.Empty;
            }
        }
    }
}