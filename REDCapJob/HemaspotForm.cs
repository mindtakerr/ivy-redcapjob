using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace REDCapJob
{
    internal class HemaspotForm
    {
        public string record_id { get; set; }
        public string redcap_event_name { get; set; }
        public string redcap_repeat_instrument { get; set; }
        public int redcap_repeat_instance { get; set; }
        public string hem_a06 { get; set; }
        public string hem_a06_eb { get; set; }
        public string hem1_a12 { get; set; }
        public string hem_a07 { get; set; }
        public string hem1_a08 { get; set; }
        public string hem1_a23 { get; set; }
        public string hem_a08 { get; set; }
        public string hem1_a05 { get; set; }
        public string hem1_a01 { get; set; }

        public string OutboundTracking
        {
            get
            {
                return hem_a07.Replace(" ", string.Empty);
            }
        }

        public string Tracking1
        {
            get
            {
                return hem_a08.Replace(" ", string.Empty);
            }
        }

        public string Tracking2
        {
            get
            {
                return hem1_a05.Replace(" ", string.Empty);
            }
        }

        public int InstanceF1 { get; set; }
        public int InstanceF2 { get; set; }

        public string LinkF1
        {
            get
            {
                // F1: https://redcap.ucsf.edu/redcap_v13.7.31/DataEntry/index.php?pid=45158&id=2&page=hemaspot_tracking_form&event_id=229473&instance=1
                string l = "https://redcap.ucsf.edu/redcap_v13.7.31/DataEntry/index.php?pid=45158&id=" + record_id + "&page=hemaspot_tracking_form&event_id=229473&instance=" + InstanceF1;
                return "<a href='" + l + "'>Hemaspot Form</a>";
            }
        }

        public string LinkF2
        {
            get
            {
                // F2: https://redcap.ucsf.edu/redcap_v13.7.31/DataEntry/index.php?pid=45158&id=2&page=hemaspot_tracking_form_2&event_id=229473&instance=1
                string l = "https://redcap.ucsf.edu/redcap_v13.7.31/DataEntry/index.php?pid=45158&id=" + record_id + "&page=hemaspot_tracking_form_2&event_id=229473&instance=" + InstanceF2;
                return "<a href='" + l + "'>Hemaspot Form 2</a>";
            }
        }

        public string LinkOutboundTracking
        {
            get
            {
                string l = "https://tools.usps.com/go/TrackConfirmAction?qtc_tLabels1=" + OutboundTracking;
                return "<a target='_blank' href='" + l + "'>" + OutboundTracking + "</a>";
            }
        }

        public string LinkInboundTracking
        {
            get
            {
                string l = "https://tools.usps.com/go/TrackConfirmAction?qtc_tLabels1=" + Tracking1;
                return "<a target='_blank' href='" + l + "'>" + Tracking1 + "</a>";
            }
        }
    }
}