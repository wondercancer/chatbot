[ServiceContract(CallbackContract = typeof(IReceiveChatService))]
    public interface ISendChatService
    {
        [OperationContract(IsOneWay = true)]
        void SendMessage(string msg,string receiver);
        [OperationContract(IsOneWay = true)]
        void Start(string Name);
        [OperationContract(IsOneWay = true)]
        void Stop(string Name);
    } 
    
    public interface IReceiveChatService
{
    [OperationContract(IsOneWay = true)]
    void ReceiveMessage(string msg,string sender);
    [OperationContract(IsOneWay = true)]
    void SendNames(List<string> names);
}

[ServiceBehavior(InstanceContextMode = InstanceContextMode.Single)]
    public class ChatService : ISendChatService
    {
        Dictionary<string, IReceiveChatService> names =
        	new Dictionary<string, IReceiveChatService>();

        public static event ListOfNames ChatListOfNames;

        IReceiveChatService callback = null;

        public ChatService() { }

        public void Close()
        {
            callback = null;
            names.Clear();
        }

        public void Start(string Name)
        {
            try
            {
                if (!names.ContainsKey(Name))
                {
                    callback = OperationContext.Current.
                    GetCallbackChannel<IReceiveChatService>();
                    AddUser(Name, callback);
                    SendNamesToAll();
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public void Stop(string Name)
        {
            try
            {
                if (names.ContainsKey(Name))
                {
                    names.Remove(Name);
                    SendNamesToAll();
                 }
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        void SendNamesToAll()
        {
            foreach (KeyValuePair<string, IReceiveChatService> name in names)
            {
                IReceiveChatService proxy = name.Value;
                proxy.SendNames(names.Keys.ToList());
            }

            if (ChatListOfNames != null)
                ChatListOfNames(names.Keys.ToList(), this);
        }

        void ISendChatService.SendMessage(string msg,string sender, string receiver)
        {
            if (names.ContainsKey(receiver))
            {
                callback = names[receiver];
                callback.ReceiveMessage(msg, sender);
           }
        }

        private void AddUser(string name,IReceiveChatService callback)
        {
            names.Add(name, callback);
            if (ChatListOfNames !=null)
                ChatListOfNames(names.Keys.ToList(), this);
        }
    }
    
    public delegate void ReceviedMessage(string sender, string message);
public delegate void GotNames(object sender, List<string /> names);

class ReceiveClient : ChatService.ISendChatServiceCallback
{
    public event ReceviedMessage ReceiveMsg;
    public event GotNames NewNames;

    InstanceContext inst = null;
    ChatService.SendChatServiceClient chatClient = null;

    public void Start(ReceiveClient rc,string name)
    {
        inst = new InstanceContext(rc);
        chatClient = new ChatService.SendChatServiceClient(inst);
        chatClient.Start(name);
    }

    public void SendMessage(string msg, string sender,string receiver)
    {
        chatClient.SendMessage(msg, sender,receiver);
    }

    public void Stop(string name)
    {
        chatClient.Stop(name);
    }

    void ChatService.ISendChatServiceCallback.ReceiveMessage(string msg, string receiver)
    {
        if (ReceiveMsg != null)
            ReceiveMsg(receiver,msg);
    }

    public void SendNames(string[] names)
    {
        if (NewNames != null)
            NewNames(this, names.ToList());
    }
}

<div>
<configuration>
  <system.serviceModel>
    <services>
      <service name="Server.ChatService"
      behaviorConfiguration="ChatServiceBehavior">
        <host>
          <baseAddresses>
            <add baseAddress="net.tcp://localhost:1234/Chat/ChatService"/>
          </baseAddresses>
        </host>
        <endpoint address="" binding="netTcpBinding"
        bindingConfiguration="Binding1"
        contract="Server.ISendChatService">
        </endpoint>
        <endpoint address="mex" binding="mexTcpBinding"
        contract="IMetadataExchange"/>
      </service>
    </services>

    <bindings>
      <netTcpBinding>
        <binding name="Binding1" closeTimeout="00:01:00"
        openTimeout="00:01:00" receiveTimeout="00:10:00"
        sendTimeout="00:01:00" transactionFlow="false" transferMode="Buffered"
        transactionProtocol="OleTransactions" hostNameComparisonMode="StrongWildcard"
        maxBufferPoolSize="524288" maxBufferSize="65536" maxConnections="10"
        maxReceivedMessageSize="65536">
          <security mode="None">
            <message clientCredentialType ="UserName"/>
          </security>
        </binding>
      </netTcpBinding>
    </bindings>

    <behaviors>
      <serviceBehaviors>
        <behavior name="ChatServiceBehavior">
          <serviceMetadata httpGetEnabled="False"/>
          <serviceDebug includeExceptionDetailInFaults="False"/>
        </behavior>
      </serviceBehaviors>
    </behaviors>

  </system.serviceModel>
</configuration>
</div>

