namespace mew_agent_con.Models
{
    public class QuantityToSend
    {
        public bool SendAll { get; set; }
        public decimal Value { get; set; }

        public QuantityToSend() { }
        public QuantityToSend(decimal value) { Value = value; }
        public static QuantityToSend All { get { return new QuantityToSend { SendAll = true }; } }

        public static QuantityToSend Some(decimal quantity)
        {
            return new QuantityToSend
            {
                Value = quantity,
                SendAll = false
            };
        }
    }
}
