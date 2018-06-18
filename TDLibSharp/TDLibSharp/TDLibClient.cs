using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Td = Telegram.Td;
using TdApi = Telegram.Td.Api;
using System.Threading;

namespace NouSoft.TDLibSharp
{
    public class TDLibClient
    {
        private static TDLibClient _tDLibClient;
        private static Td.Client _client = null;
        private readonly static Td.ClientResultHandler _defaultHandler = new DefaultHandler();
        private static TdApi.AuthorizationState _authorizationState = null;
        private static volatile bool _haveAuthorization = false;
        private static volatile bool _quiting = false;
        private static readonly string _newLine = Environment.NewLine;
        private static volatile AutoResetEvent _gotAuthorization = new AutoResetEvent(false);
        private static volatile string _currentPrompt = null;
        private string _databaseDirectory;
        private bool _useMessageDatabase;
        private bool _useSecretChats;
        private int _apiId;
        private string _apiHash;
        private string _systemLanguageCode;
        private string _deviceModel;
        private string _systemVersion;
        private string _applicationVersion;
        private bool _enableStorageOptimizer;

        public delegate void OnAuthorizationStateUpdatedEventHandler(EnumAuthorization enumAuthorization);
        public event OnAuthorizationStateUpdatedEventHandler OnAuthorizationStateUpdatedEvent;

        public delegate void UpdatesHandlerEventHandler(TdApi.BaseObject @object);
        public static event UpdatesHandlerEventHandler UpdatesHandlerEventHandlerEvent;

        static TDLibClient()
        {
            _client = CreateTdClient();
        }

        public TDLibClient(int apiId, string apiHash,
            string databaseDirectory = "tdlib",
            bool enableStorageOptimizer = true,
            bool useMessageDatabase = true, bool useSecretChats = true, string systemLanguageCode = "en",
            string deviceModel = "Desktop", string systemVersion = "NouSoft TDLib", string applicationVersion = "1.0")
        {
            _databaseDirectory = databaseDirectory;
            _useMessageDatabase = useMessageDatabase;
            _useSecretChats = useSecretChats;
            _apiId = apiId;
            _apiHash = apiHash;
            _systemLanguageCode = systemLanguageCode;
            _deviceModel = deviceModel;
            _systemVersion = systemVersion;
            _enableStorageOptimizer = enableStorageOptimizer;
            _applicationVersion = applicationVersion;

            _tDLibClient = this;
        }

        public static Td.Client CreateTdClient()
        {
            Td.Client result = Td.Client.Create(new UpdatesHandler());
            new Thread(() =>
            {
                Thread.CurrentThread.IsBackground = true;
                result.Run();
            }).Start();
            return result;
        }

        private static void Print(string str)
        {
            if (_currentPrompt != null)
            {
                Console.WriteLine();
            }
            Console.WriteLine(str);
            if (_currentPrompt != null)
            {
                Console.Write(_currentPrompt);
            }
        }
        

        public void OnAuthorizationStateUpdated(TdApi.AuthorizationState authorizationState)
        {            
            if (authorizationState != null)
            {
                _authorizationState = authorizationState;
            }
            if (_authorizationState is TdApi.AuthorizationStateWaitTdlibParameters)
            {
                TdApi.TdlibParameters parameters = new TdApi.TdlibParameters();
                parameters.DatabaseDirectory = _databaseDirectory;
                parameters.UseMessageDatabase = _useMessageDatabase;
                parameters.UseSecretChats = _useSecretChats;
                parameters.ApiId = _apiId;
                parameters.ApiHash = _apiHash; 
                parameters.SystemLanguageCode = _systemLanguageCode;
                parameters.DeviceModel = _deviceModel;
                parameters.SystemVersion = _systemVersion;
                parameters.ApplicationVersion = _applicationVersion;
                parameters.EnableStorageOptimizer = _enableStorageOptimizer;

                _client.Send(new TdApi.SetTdlibParameters(parameters), new AuthorizationRequestHandler());

                OnAuthorizationStateUpdatedEvent(EnumAuthorization.TdlibParameters);
            }
            else if (_authorizationState is TdApi.AuthorizationStateWaitEncryptionKey)
            {
                _client.Send(new TdApi.CheckDatabaseEncryptionKey(), new AuthorizationRequestHandler());
                OnAuthorizationStateUpdatedEvent(EnumAuthorization.EncryptionKey);

            }
            else if (_authorizationState is TdApi.AuthorizationStateWaitPhoneNumber)
            {
                OnAuthorizationStateUpdatedEvent(EnumAuthorization.WaitPhoneNumber);
            }
            else if (_authorizationState is TdApi.AuthorizationStateWaitCode)
            {
                OnAuthorizationStateUpdatedEvent(EnumAuthorization.WaitCode);
            }
            else if (_authorizationState is TdApi.AuthorizationStateWaitPassword)
            {
                OnAuthorizationStateUpdatedEvent(EnumAuthorization.WaitPassword);
            }
            else if (_authorizationState is TdApi.AuthorizationStateReady)
            {
                _haveAuthorization = true;
                _gotAuthorization.Set();
                OnAuthorizationStateUpdatedEvent(EnumAuthorization.Ready);
            }
            else if (_authorizationState is TdApi.AuthorizationStateLoggingOut)
            {
                _haveAuthorization = false;
                Print("Logging out");
                OnAuthorizationStateUpdatedEvent(EnumAuthorization.LoggingOut);
            }
            else if (_authorizationState is TdApi.AuthorizationStateClosing)
            {
                _haveAuthorization = false;
                Print("Closing");
                OnAuthorizationStateUpdatedEvent(EnumAuthorization.Closing);
            }
            else if (_authorizationState is TdApi.AuthorizationStateClosed)
            {
                Print("Closed");
                if (!_quiting)
                {
                    _client = CreateTdClient(); // recreate _client after previous has closed
                }
                OnAuthorizationStateUpdatedEvent(EnumAuthorization.Closed);
            }
            else
            {
                Print("Unsupported authorization state:" + _newLine + _authorizationState);
                OnAuthorizationStateUpdatedEvent(EnumAuthorization.None);
            }
            OnAuthorizationStateUpdatedEvent(EnumAuthorization.None);
        }

        private static long GetChatId(string arg)
        {
            long chatId = 0;
            try
            {
                chatId = Convert.ToInt64(arg);
            }
            catch (FormatException)
            {
            }
            catch (OverflowException)
            {
            }
            return chatId;
        }

        private class DefaultHandler : Td.ClientResultHandler
        {
            void Td.ClientResultHandler.OnResult(TdApi.BaseObject @object)
            {
                Print(@object.ToString());
            }
        }

        public class UpdatesHandler : Td.ClientResultHandler
        {
            void Td.ClientResultHandler.OnResult(TdApi.BaseObject @object)
            {
                UpdatesHandlerEventHandlerEvent(@object);
                if (@object is TdApi.UpdateAuthorizationState)
                {
                    _tDLibClient.OnAuthorizationStateUpdated((@object as TdApi.UpdateAuthorizationState).AuthorizationState);
                }
                else
                {
                    // Print("Unsupported update: " + @object);
                }
            }
        }

        public class AuthorizationRequestHandler : Td.ClientResultHandler
        {
            void Td.ClientResultHandler.OnResult(TdApi.BaseObject @object)
            {
                if (@object is TdApi.Error)
                {
                    Print("Receive an error:" + _newLine + @object);
                    _tDLibClient.OnAuthorizationStateUpdated(null); // repeat last action
                }
                else
                {
                    // result is already received through UpdateAuthorizationState, nothing to do
                }
            }
        }

        public class CustomClient
        {
            public static TdApi.BaseObject Send(TdApi.Function function)
            {
                var handler = new CustomDefaultHandler();
                _client.Send(function, handler);
                return handler.GetBaseObject();
            }
        }

        public class CustomDefaultHandler : Td.ClientResultHandler
        {
            private TdApi.BaseObject baseObject;
            private bool flag = false;
            public void OnResult(TdApi.BaseObject @object)
            {
                flag = false;
                baseObject = @object;
                flag = true;
            }

            public TdApi.BaseObject GetBaseObject()
            {
                while (true)
                {
                    if (flag == true)
                    {
                        flag = false;
                        return baseObject;
                    }
                    continue;
                }
            }

        }


    }
}
