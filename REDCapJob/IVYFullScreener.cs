using MimeKit.Encodings;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace REDCapJob
{
    // Root myDeserializedClass = JsonConvert.DeserializeObject<List<Root>>(myJsonResponse);
    public class IVYFullScreener
    {
        public string record_id { get; set; }
        public string fs01 { get; set; }
        public string fs01b { get; set; }
        public string fs01_legalfirst { get; set; }
        public string fs01_legallast { get; set; }

        public string fs02___1 { get; set; }
        public string fs02___2 { get; set; }
        public string fs02___3 { get; set; }
        public string fs02___4 { get; set; }
        public string fs02___5 { get; set; }
        public string fs02___6 { get; set; }
        public string fs02___7 { get; set; }
        public string fs2a { get; set; }
        public string fs03 { get; set; }
        public string fs04 { get; set; }
        public string fs05 { get; set; }
        public string fs06 { get; set; }
        public string fs07 { get; set; }
        public string fs08m { get; set; }
        public string fs08y { get; set; }
        public string fs08a { get; set; }
        public string fs09 { get; set; }
        public string fs10 { get; set; }
        public string fs10a { get; set; }
        public string fs11 { get; set; }
        public string fs12m { get; set; }
        public string fs12y { get; set; }
        public string fs12a { get; set; }
        public string fs13 { get; set; }
        public string fs13a { get; set; }
        public string fs14___1 { get; set; }
        public string fs14___2 { get; set; }
        public string fs14___3 { get; set; }
        public string fs14___4 { get; set; }
        public string fs14___5 { get; set; }
        public string fs14___6 { get; set; }
        public string fs14___7 { get; set; }
        public string fs14___8 { get; set; }
        public string fs14___9 { get; set; }
        public string fs14___10 { get; set; }
        public string fs14a { get; set; }
        public string fs15 { get; set; }
        public string fs15a { get; set; }
        public string fs15b { get; set; }
        public string fs16___1 { get; set; }
        public string fs16___2 { get; set; }
        public string fs16___3 { get; set; }
        public string fs16___4 { get; set; }
        public string fs16___5 { get; set; }
        public string fs16a { get; set; }
        public string fs17 { get; set; }
        public string fs18 { get; set; }
        public string fs18a { get; set; }
        public string fs19 { get; set; }
        public string fs20a { get; set; }
        public string fs20b { get; set; }
        public string fs20c { get; set; }
        public string fs20d { get; set; }
        public string fs21 { get; set; }
        public string fs22 { get; set; }
        public string fs22a { get; set; }
        public string fs23 { get; set; }
        public string fs24 { get; set; }
        public string full_screener_complete { get; set; }

        public bool IsFullyEligible
        {
            get
            {
                return fs13 == "1" && fs15 == "1";
            }
        }
    }
}