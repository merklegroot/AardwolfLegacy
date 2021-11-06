namespace log_lib.Models
{
    public class EventType
    {
        private string Name { get; set; }

        public EventType() { }
        public EventType(string name) { Name = name; }

        public static implicit operator string(EventType item)
        {
            return item?.Name;
        }

        public static EventType Exception { get { return new EventType("Exception"); } }
    }
}
