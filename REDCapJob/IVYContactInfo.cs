using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace REDCapJob
{
    public class IVYContactInfo
    {
        public string record_id { get; set; }
        public string redcap_event_name { get; set; }
        public string et_a01 { get; set; }
        public string et_a05 { get; set; }
        public string s01a01a { get; set; }
        public string s01a01b { get; set; }
        public string s01a02 { get; set; }
        public string s01a02p { get; set; }
        public string s01a03___1 { get; set; }
        public string s01a03___2 { get; set; }
        public string s01a03___3 { get; set; }
        public string s01a03___4 { get; set; }
        public string s01a03___5 { get; set; }
        public string s01a03a { get; set; }
        public string s01a04 { get; set; }
        public string s01a05___1 { get; set; }
        public string s01a05___2 { get; set; }
        public string s01a05___3 { get; set; }
        public string s01a05___4 { get; set; }
        public string s01a06 { get; set; }
        public string s01a07 { get; set; }
        public string s01a08 { get; set; }
        public string s01a09___1 { get; set; }
        public string s01a09___2 { get; set; }
        public string s01a09___3 { get; set; }
        public string s01a09___4 { get; set; }
        public string s01a09a { get; set; }
        public string s01a09b { get; set; }
        public string s01a09c { get; set; }
        public string s01a09d { get; set; }
        public string s01a10a { get; set; }
        public string s01a10b { get; set; }
        public string s01a10c { get; set; }
        public string s01a10d { get; set; }
        public string s01a10e { get; set; }
        public string s01a11a { get; set; }
        public string s01a11b { get; set; }
        public string s01a11c { get; set; }
        public string s01a11d { get; set; }
        public string s01a11e { get; set; }
        public string s01a11f { get; set; }
        public string s01a11g { get; set; }
        public string s01a11h { get; set; }
        public string s01a12a { get; set; }
        public string s01a12b { get; set; }
        public string s01a12c { get; set; }
        public string s01a12d { get; set; }
        public string s01a13a { get; set; }
        public string s01a13b { get; set; }
        public string s01a13c { get; set; }
        public string s01a13d { get; set; }
        public string s01a13e { get; set; }
        public string s01a13f { get; set; }
        public string s01a13g { get; set; }
        public string s01a13h { get; set; }
        public string s01a13i { get; set; }
        public string s01a13j { get; set; }
        public string s01a13k { get; set; }
        public string s01a13l { get; set; }
        public string s01a13m { get; set; }
        public string s01a14 { get; set; }
        public string s01a15 { get; set; }
        public string s01a16 { get; set; }
        public string s01a17 { get; set; }
        public string s01a17a { get; set; }
        public string s01a17b { get; set; }
        public string s01a18 { get; set; }
        public string contact_information_complete { get; set; }
        public string s01a10_address { get; set; }

        public static string BuiltAddress(IVYContactInfo info)
        {
            string a = info.s01a10a + "\n";
            if (info.s01a10b.Length > 0)
                a += info.s01a10b + "\n";
            a += info.s01a10c;
            if (info.s01a10d == "5")
                a += ", CA";
            a += " " + info.s01a10e;
            return a;
        }

        public IVYContactInfo()
        {
        }

        public IVYContactInfo(IVYFullScreener FSForm)
        {
            et_a01 = FSForm.record_id; // Screening ID
            et_a05 = FSForm.fs24; // Screening notes
            s01a02 = FSForm.fs01 + " " + FSForm.fs01b;// Name (Pref)
            s01a01a = FSForm.fs01_legalfirst;// First Name (Legal)
            s01a01b = FSForm.fs01_legallast;// Last Name (Legal)

            // Pronouns
            s01a03___1 = FSForm.fs16___1;
            s01a03___2 = FSForm.fs16___2;
            s01a03___3 = FSForm.fs16___3;
            s01a03___4 = FSForm.fs16___4;
            s01a03___5 = FSForm.fs16___5;
            s01a03a = FSForm.fs16a;

            s01a04 = FSForm.fs04; // DOB
            if (FSForm.fs20c == "1")
                s01a05___1 = "1"; // Contact by Phone?
            if (FSForm.fs20b == "1")
                s01a05___2 = "1"; // Contact by Text?
            if (FSForm.fs20d == "1")
                s01a05___3 = "1"; // Contact by Voicemail?
            if (FSForm.fs20a == "1")
                s01a05___4 = "1"; // Contact by Email?

            s01a06 = FSForm.fs03; // Cell Number
            s01a07 = FSForm.fs19; // Email Address
            // s01a10a = FSForm.fs21; // Home Address (might need to parse this out because there are multiple fields on Contact vs. 1 field on Screener
            s01a10_address = FSForm.fs21;
            s01a18 = FSForm.fs17; // Phone make and model

            s01a12a = FSForm.fs10a; // PCP Info

            if (FSForm.fs22 == "1")
            {
                s01a17 = "1";
                s01a17a = FSForm.fs22a;
            }

            redcap_event_name = "enrollment_arm_1";
            record_id = "0"; // This should be changed to auto-number
        }
    }
}