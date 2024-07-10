using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ScreenMonAPI.Messages
{
    public class RsaKeyMessage : IAppMessage
    {
        public required byte[] RsaPublicKeyBytes { get; set; }
    }
}
