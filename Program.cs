string url = "ws://127.0.0.1:15999";


WebSocketServer webSocketServer = new WebSocketServer(url);

webSocketServer.AddWebSocketService<ChatGPT>("/ChatGpt");
webSocketServer.Start();
Console.WriteLine($"WebsocketServer Start On {url}");
Console.WriteLine($"WebsocketServer AddWebSocketService {nameof(ChatGPT)}");

A:
await Task.Delay(1000000000);
goto A;



//注册WebsocketServer路由CQAPI
public partial class ChatGPT : WebSocketBehavior
{
    string apiKey = "Your API KEY";
    string chatApiUrl = "https://api.openai.com/v1/chat/completions";

    List<string> privateAccountList = new List<string>()
    {
        "2658326876",
    };

    public async Task<string>? GetChatGptResponseAsync(string model, string role, string content)
    {

        HttpClient httpClient = new HttpClient();
        httpClient.BaseAddress = new Uri(chatApiUrl);
        httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiKey}");
        httpClient.DefaultRequestHeaders.Add("Organization", "org-jzBeBdUq4xCadFXGMYwiljWI");
        ChatRequestJson chatRequestJson = new ChatRequestJson()
        {
            model = model,
            messages = new List<Message>()
            {
                new Message()
                {
                    role=role,
                    content=content
                }
            }
        };

        var requestJson = JsonSerializer.Serialize(chatRequestJson);

        var requestBody = new StringContent(requestJson, Encoding.UTF8, "application/json");
        try
        {
            var response = await httpClient.PostAsync(chatApiUrl, requestBody);
            var responseJson = await response.Content.ReadAsStringAsync();
            Console.WriteLine(responseJson);

            ChatGPTResponse? chatGPTResponse = JsonSerializer.Deserialize<ChatGPTResponse>(responseJson);
            content = chatGPTResponse?.choices?[0].message?.content;
            Console.WriteLine("Content:" + content);
            return content is null ? "-1" : content;

        }
        catch (Exception exception)
        {
            Console.WriteLine(exception.Message);
            return "-1";
        }
    }

    protected override async void OnMessage(MessageEventArgs e)
    {
        //Handle Reverse Message
        var receivedJson = ConvertReceivedJsonToClass(e.Data);        //转换GO-CQHTTPX为ReceivedJson类
        Task.Run(async () =>
               {

                   if (receivedJson!.PostType is null)   //检测GO-CQHTTPX数据的PostType是否转换为空
                       return;
                   if (receivedJson.PostType == PostType.MetaEvent)    //判断GO-CQHTTPX数据是否为心跳数据
                       return;
                   if (receivedJson.PostType != PostType.Message)      //判断GO-CQHTTPX数据是否为信息数据
                       return;
                   var receivedRawMessage = receivedJson.RawMessage;

                   if (receivedJson.MessageType == MessageType.Group)
                   {
                       if (!receivedRawMessage!.Contains($"[CQ:at,qq={receivedJson.SelfId}]"))   //判断GO-CQHTTPX信息数据是否是@机器人
                           return;
                       Console.WriteLine(e.Data);
                       var index = receivedRawMessage.IndexOf("]");
                       String chatGptResponseMessageString;
                       try
                       {
                           receivedRawMessage = receivedRawMessage[(index + 1)..].TrimStart();  //截取有效信息段
                           if (string.IsNullOrWhiteSpace(receivedRawMessage))
                               chatGptResponseMessageString = "What to do ?";
                           else
                               // gpt-3.5-turbo gpt-3.5-turbo-0301
                               chatGptResponseMessageString = await GetChatGptResponseAsync(model: "gpt-3.5-turbo", role: "user", content: receivedRawMessage);  //向ChatGPT发送信息，并获取响应

                       }
                       catch (TimeoutException)
                       {
                           chatGptResponseMessageString = "Time Out !";
                       }
                       //text-ada-001 text-babbage-001 text-curie-001 text-davinci-003

                       //TODO:Handle Positive Message
                       try
                       {
                           SendGroupMessage(chatGptResponseMessageString, receivedJson.GroupId, receivedJson.UserId);        //将ChatGTP响应 数据发送到GO-CQHTTPX
                       }
                       catch (Exception exception)
                       {
                           SendGroupMessage(exception.Message, receivedJson.GroupId, receivedJson.UserId);
                       }

                   }
                   //    else if (receivedJson.MessageType == MessageType.Private)
                   //    {
                   //        if (!privateAccountList.Contains(receivedJson.UserId.ToString()))
                   //        {
                   //            SendPrivateMessage("人家不认识你呀,先去找雾都杀手添加身份认证吧", receivedJson.UserId);        //将ChatGTP响应 数据发送到GO-CQHTTPX
                   //            return;
                   //        }
                   //        Console.WriteLine(e.Data);
                   //        String chatGptResponseMessageString;
                   //        try
                   //        {
                   //            receivedRawMessage = receivedRawMessage.TrimStart();  //截取有效信息段
                   //            if (string.IsNullOrWhiteSpace(receivedRawMessage))
                   //                chatGptResponseMessageString = "What to do ?";
                   //            else
                   //                // gpt-3.5-turbo gpt-3.5-turbo-0301

                   //                chatGptResponseMessageString = await GetChatGptResponseAsync(model: "gpt-3.5-turbo", role: "user", content: receivedRawMessage);  //向ChatGPT发送信息，并获取响应
                   //        }
                   //        catch (TimeoutException)
                   //        {
                   //            chatGptResponseMessageString = "Time Out !";
                   //        }
                   //        try
                   //        {
                   //            SendPrivateMessage(chatGptResponseMessageString, receivedJson.UserId);        //将ChatGTP响应 数据发送到GO-CQHTTPX

                   //        }
                   //        catch (Exception exception)
                   //        {
                   //            SendPrivateMessage(exception.Message, receivedJson.UserId);
                   //        }
                   //    }

               });
    }
    protected override void OnClose(CloseEventArgs e)
    {
        Console.WriteLine("One Client Disconnected");
    }
    protected override void OnError(WebSocketSharp.ErrorEventArgs e)
    {
        Console.WriteLine(e.Message);
    }
    protected override void OnOpen()
    {
        Console.WriteLine("One Client Connected");
    }

}
class ChatGPTResponse
{
    public List<Choice>? choices { get; set; }

}
class Choice
{
    public int? index { get; set; }
    public Message? message { get; set; }
    public string? finish_reason { get; set; }
}
class Message
{
    [JsonIgnore(Condition = JsonIgnoreCondition.Never)] public string role { get; set; }
    [JsonIgnore(Condition = JsonIgnoreCondition.Never)] public string content { get; set; }
}
class ChatRequestJson
{
    [JsonIgnore(Condition = JsonIgnoreCondition.Never)] public string model { get; set; }
    [JsonIgnore(Condition = JsonIgnoreCondition.Never)] public List<Message> messages { get; set; }


}