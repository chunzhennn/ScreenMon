using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ScreenMonAPI.Messages
{
    public class AesKeyMessage : IAppMessage
    {
        public required byte[] AesKeyBytes { get; set; }
        public required byte[] AesIvBytes { get; set; }
    }
}
