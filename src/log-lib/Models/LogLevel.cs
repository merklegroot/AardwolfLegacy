namespace log_lib.Models
{
    public class LogLevel
    {
        private string Name { get; set; }

        public LogLevel() { }
        public LogLevel(string name) { Name = name; }

        public static implicit operator string(LogLevel item)
        {
            return item?.Name;
        }

        public override string ToString()
        {
            return Name;
        }

        public static LogLevel Debug { get { return new LogLevel("Debug"); } }
        public static LogLevel Verbose { get { return new LogLevel("Verbose"); } }
        public static LogLevel Error { get { return new LogLevel("Error"); } }
        public static LogLevel Warning { get { return new LogLevel("Warning"); } }
        public static LogLevel Info { get { return new LogLevel("Info"); } }        
    }
}
