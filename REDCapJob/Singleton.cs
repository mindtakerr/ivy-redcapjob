namespace REDCapJob
{
    public sealed class Singleton
    {
        private static Singleton instance = null;
        private static readonly object padlock = new object();

        private Singleton()
        {
        }

        public static Singleton Instance
        {
            get
            {
                lock (padlock)
                {
                    if (instance == null)
                    {
                        instance = new Singleton();
                    }
                    return instance;
                }
            }
        }

        public List<LoggedEvent> LoggedEvents { get; set; }
        public List<IVYContactPrefs>? IvyContactPrefs;
        public string API_URL = "URL";
        public string PROD_IVY_API_KEY = "API_KEY";
        public string DEV_IVY_API_KEY = "API_KEY";
        public string PROD_SCREENING_API_KEY = "API_KEY";
        public string DEV_SCREENING_API_KEY = "API_KEY";
        public string FROM_ADDRESS = "noreply@ivy-ucsf.edu";
        public bool HOLD_ALERTS = false; // This is used to stop alerts from sending during test purposes, etc.

        public DateTime CurrentPT
        { get { return DateTime.Now.AddHours(-3); } }
    }
}