# TDLibSharp
Telegram TDLib via C-Sharp

### Installation

Install via NuGet: ```TDLib```

[![NuGet](https://img.shields.io/nuget/v/TDLib.svg)](https://www.nuget.org/packages/TDLib/)

### Dependencies

[Build TDLib](https://core.telegram.org/tdlib/docs/index.html#building) and put the compiled library into your project's output directory
* tdjson.dll (Windows)
* LIBEAY32.dll (Windows)
* SSLEAY32.dll (Windows)
* Telegram.Td.dll (Windows)

### Simple example

```csharp
using System;
using System.Windows.Forms;
using TdApi = Telegram.Td.Api;
using client = NouSoft.TDLibSharp.TDLibClient.CustomClient;
using NouSoft.TDLibSharp;

namespace Example
{
    public partial class Form1 : Form
    {
        private static TDLibClient tDLibClient;

        public Form1()
        {
            InitializeComponent();

            tDLibClient = new TDLibClient(94575, "a3406de8d171bb422bb6ddf3bbd800e2");
            tDLibClient.OnAuthorizationStateUpdatedEvent += TDLibClient_OnAuthorizationStateUpdatedEvent;
            TDLibClient.UpdatesHandlerEventHandlerEvent += TDLibClient_UpdatesHandlerEventHandlerEvent; 
        }

        private void TDLibClient_UpdatesHandlerEventHandlerEvent(TdApi.BaseObject @object)
        {
            var obj = @object;
        }

        private void TDLibClient_OnAuthorizationStateUpdatedEvent(EnumAuthorization enumAuthorization)
        {
            if (enumAuthorization == EnumAuthorization.WaitPhoneNumber)
            {
                string phone = "<phone_number>";
                var result = client.Send(new TdApi.SetAuthenticationPhoneNumber(phone, false, false));

            }
            else if (enumAuthorization == EnumAuthorization.WaitCode)
            {
                string code = "<code_from_telegram>";// you can change code in debugger
                var result = client.Send(new TdApi.CheckAuthenticationCode(code, "", ""));
            }
            else
            {
                
            }
        }

        private void btnSendTextMessage_Click(object sender, EventArgs e)
        {
            var me =(TdApi.User) client.Send(new TdApi.GetMe());
            var chat =(TdApi.Chat) client.Send(new TdApi.SearchPublicChat(me.Username));
            TdApi.InputMessageContent content = new TdApi.InputMessageText(new TdApi.FormattedText("Hello!", null), false, true);
            var result =  client.Send(new TdApi.SendMessage(chat.Id, Int64.MaxValue, false, false, null, content));
        }
    }
}
```

