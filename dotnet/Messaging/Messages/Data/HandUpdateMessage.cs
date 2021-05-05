using Geste.Messaging.Models;
using Newtonsoft.Json.Linq;

namespace Geste.Messaging
{
    public sealed class HandUpdateMessage : Message
    {
        public HandUpdateMessage(int fingers, bool contourAvailable, bool indicesAvailable)
        {
            OpCode = OpCode.HandUpdate;
            Data = JToken.FromObject(new HandUpdateMessageModel
            {
                Fingers = fingers,
                ContourAvailable = contourAvailable,
                IndicesAvailable = indicesAvailable
            });
        }
    }
}