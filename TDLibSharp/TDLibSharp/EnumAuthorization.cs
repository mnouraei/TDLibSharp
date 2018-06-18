using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NouSoft.TDLibSharp
{
    public enum EnumAuthorization
    {
        TdlibParameters,
        WaitPhoneNumber,
        WaitCode,
        WaitPassword,
        Ready,
        LoggingOut,
        Closing,
        Closed,
        None,
        EncryptionKey

    }
}
