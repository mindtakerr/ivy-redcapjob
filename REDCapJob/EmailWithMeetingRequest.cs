namespace REDCapJob
{
    using System;
    using System.Net;
    using System.Net.Mail;
    using System.Text;

    public class EmailWithMeetingRequest
    {
        public enum MeetingAction
        {
            Create,
            Cancel
        };

        public static string SendEmail(string FromAddress, string ToAddress, string Subject, string Body)
        {
            MailAddress From = new MailAddress(FromAddress);

            MailAddress[] To = new MailAddress[1];
            To[0] = new MailAddress(ToAddress);

            return SendEmail(From, To, Subject, Body);
        }

        public static string SendEmail(MailAddress from, MailAddress[] to, string subject, string body)
        {
            return SendEmail(from, to, subject, body, null);
        }

        public static string SendEmail(MailAddress from, MailAddress[] to, string subject, string body, string[] attachmentFilenames)
        {
            try
            {
                MailMessage message = new MailMessage();
                //SmtpClient sc = new SmtpClient("smtp.gmail.com", 587)
                //{
                //    Credentials = new NetworkCredential("mhamrti@gmail.com", "wzxswgxerglihnyv"),
                //    EnableSsl = true
                //};

                SmtpClient sc = new SmtpClient("smtp.sendgrid.net", 587)
                {
                    Credentials = new NetworkCredential("apikey", "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"),
                    EnableSsl = true
                };

                foreach (MailAddress Address in to)
                {
                    message.To.Add(Address);
                }

                message.Subject = subject;
                message.From = from;
                message.Body = body;
                message.IsBodyHtml = true;
                message.SubjectEncoding = System.Text.Encoding.UTF8;
                message.BodyEncoding = System.Text.Encoding.UTF8;

                if (attachmentFilenames != null)
                {
                    foreach (string AttachmentFilename in attachmentFilenames)
                    {
                        Attachment a = new Attachment(AttachmentFilename);
                        if (AttachmentFilename.Contains("png"))
                            a.ContentId = "logo";
                        message.Attachments.Add(a);
                    }
                }

                sc.Send(message);

                return "Mail successfully sent <br/>";
            }
            catch (Exception ex)
            {
                return "Mail failed to send because of error: " + ex.Message + "<br/>";
            }
        }

        public static Guid SendMeetingRequest(DateTime start, DateTime end, MailAddress from, MailAddress[] to, string subject, string body, string location)
        {
            return SendMeetingRequest(start, end, from, to, subject, body, location, null, MeetingAction.Create);
        }

        public static Guid SendMeetingRequest(DateTime start, DateTime end, MailAddress from, MailAddress[] to, string subject, string body, string location, MeetingAction action)
        {
            return SendMeetingRequest(start, end, from, to, subject, body, location, null, action);
        }

        public static Guid SendMeetingRequest(DateTime start, DateTime end, MailAddress from, MailAddress[] to, string subject, string body, string location, string[] attachmentFilenames, MeetingAction action)
        {
            Guid mid = Guid.NewGuid();
            return SendMeetingRequest(start, end, from, to, subject, body, location, attachmentFilenames, action, mid);
        }

        public static Guid SendMeetingRequest(DateTime start, DateTime end, MailAddress from, MailAddress[] to, string subject, string body, string location, string[] attachmentFilenames, MeetingAction action, Guid GUID)
        {
            string action_str = "REQUEST";
            if (action == MeetingAction.Cancel)
                action_str = "CANCEL";

            SmtpClient sc = new SmtpClient("smtp.gmail.com", 587)
            {
                Credentials = new NetworkCredential("mhamrti@gmail.com", "wzxswgxerglihnyv"),
                EnableSsl = true
            };
            MailMessage msg = new MailMessage();

            msg.From = from;

            foreach (MailAddress m in to)
            {
                msg.To.Add(m);
            }

            msg.Subject = subject;

            string NewBody = "<!DOCTYPE HTML PUBLIC \"-//W3C//DTD HTML 4.0 Transitional//EN\">";
            NewBody += "<html><head><META http-equiv=Content-Type content=\"text/html; charset=iso-8859-1\"></head><body>";
            NewBody += body;
            NewBody += "</body></html>";

            msg.Body = NewBody;
            msg.IsBodyHtml = true;

            StringBuilder str = new StringBuilder();
            str.AppendLine("BEGIN:VCALENDAR");
            str.AppendLine("PRODID:-//RTI International");
            str.AppendLine("VERSION:2.0");
            str.AppendLine("METHOD:" + action_str);
            str.AppendLine("BEGIN:VEVENT");
            str.AppendLine(string.Format("DTSTART:{0:yyyyMMddTHHmmssZ}", start.ToUniversalTime()));
            str.AppendLine(string.Format("DTSTAMP:{0:yyyyMMddTHHmmssZ}", DateTime.UtcNow));
            str.AppendLine(string.Format("DTEND:{0:yyyyMMddTHHmmssZ}", end.ToUniversalTime()));
            str.AppendLine("LOCATION: " + location);
            str.AppendLine(string.Format("UID:{0}", GUID));
            str.AppendLine(string.Format("DESCRIPTION:{0}", msg.Body));
            str.AppendLine(string.Format("X-ALT-DESC;FMTTYPE=text/html:{0}", msg.Body));
            str.AppendLine(string.Format("SUMMARY:{0}", msg.Subject));
            str.AppendLine(string.Format("ORGANIZER:MAILTO:{0}", msg.From.Address));

            str.AppendLine(string.Format("ATTENDEE;CN=\"{0}\";RSVP=TRUE:mailto:{1}", msg.To[0].DisplayName, msg.To[0].Address));

            str.AppendLine("BEGIN:VALARM");
            str.AppendLine("TRIGGER:-PT15M");
            str.AppendLine("ACTION:DISPLAY");
            str.AppendLine("DESCRIPTION:Reminder");
            str.AppendLine("END:VALARM");
            str.AppendLine("END:VEVENT");
            str.AppendLine("END:VCALENDAR");
            System.Net.Mime.ContentType ct = new System.Net.Mime.ContentType("text/calendar");
            ct.Parameters.Add("method", action_str);

            AlternateView avCalhtml = AlternateView.CreateAlternateViewFromString(body, new System.Net.Mime.ContentType("text/html"));
            AlternateView avCal = AlternateView.CreateAlternateViewFromString(str.ToString(), ct);
            msg.AlternateViews.Add(avCalhtml);
            msg.AlternateViews.Add(avCal);

            if (attachmentFilenames != null)
            {
                foreach (string AttachmentFilename in attachmentFilenames)
                {
                    msg.Attachments.Add(new Attachment(AttachmentFilename));
                }
            }

            sc.Send(msg);

            return GUID;
        }

        public static string StripHtml(string html, bool allowHarmlessTags)
        {
            if (html == null || html == string.Empty)
                return string.Empty;
            if (allowHarmlessTags)
                return System.Text.RegularExpressions.Regex.Replace(html, "", "");
            return System.Text.RegularExpressions.Regex.Replace(html, "<[^>]*>", "");
        }
    }
}