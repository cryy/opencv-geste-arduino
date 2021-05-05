namespace Geste.Messaging
{
    public sealed class HelloMessage : Message
    {
        public HelloMessage()
        {
            OpCode = OpCode.Hello;
            Data = null;
        }
    }
}