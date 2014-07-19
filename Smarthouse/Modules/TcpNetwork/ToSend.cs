namespace Smarthouse {
    internal class ToSend
    {
        public ToSend(string partner, byte[] data)
        {
            this.partner = partner;
            this.data = data;
        }

        public string partner { get; set; }
        public byte[] data { get; set; }
    }
}