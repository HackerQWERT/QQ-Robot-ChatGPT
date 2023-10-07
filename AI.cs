partial class AI : WebSocketBehavior
{
    //接受数据类型枚举类
    public static class PostType
    {
        static public string Message { get; } = "message";
        static public string MetaEvent { get; } = "meta_event";
    }
    public static class MessageType
    {
        static public string Private { get; } = "private";
        static public string Group { get; } = "group";
    }
    public static class MetaEventType
    {
        static public string Lifecycle { get; } = "lifecycle";
        static public string Heartbeat { get; } = "heartbeat";
        static public string Notice { get; } = "notice";
        static public string Request { get; } = "request";
    }

    //接受数据JSON类
    public class ReceivedJson
    {
        public string? PostType { get; set; }
        public string? MessageType { get; set; }
        public Int64? GroupId { get; set; }
        public string? Message { get; set; }
        public string? RawMessage { get; set; }
        public Int64? UserId { get; set; }
        public Int64? SelfId { get; set; }

    }
    public class Sender
    {
        public Int64? age { get; set; }
        public string? nickname { get; set; }
        public string? sex { get; set; }
        public Int64? user_id { get; set; }

    }

    //发送数据JSON类
    public class SendingJson
    {
        [JsonIgnore(Condition = JsonIgnoreCondition.Never)]
        public string? Action { get; set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public Params? Params { get; set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? Echo { get; set; }
    }
    //发送数据JSON类内嵌类
    public class Params
    {
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? MessageType { get; set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public Int64? UserId { get; set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public Int64? GroupId { get; set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.Never)]
        public string? Message { get; set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public bool? AutoEscape { get; set; }
    }

    //将来自GO-CQHTTPX的JSON数据切换大写
    string ProcessReceivedJsonString(string receivedJsonString)
    {
        receivedJsonString = receivedJsonString
        .Replace("\"post_type\"", "\"PostType\"")
        .Replace("\"message_type\"", "\"MessageType\"")
        .Replace("\"group_id\"", "\"GroupId\"")
        .Replace("\"raw_message\"", "\"RawMessage\"")
        .Replace("\"user_id\"", "\"UserId\"")
        .Replace("\"self_id\"", "\"SelfId\"");
        return receivedJsonString;

    }
    //将收到的GO-CQHTTPX数据JSON转换为ReceivedJson类
    ReceivedJson? ConvertReceivedJsonToClass(string receivedJsonString)
    {
        receivedJsonString = ProcessReceivedJsonString(receivedJsonString);
        var receivedJson = JsonSerializer.Deserialize<ReceivedJson>(receivedJsonString);
        return receivedJson;
    }

    //将发送到GO-CQHTTPX的JSON数据切换小写
    string ProcessSendingJsonString(string sendingJsonString)
    {
        sendingJsonString = sendingJsonString
        .Replace("\"Action\"", "\"action\"")
        .Replace("\"Params\"", "\"params\"")
        .Replace("\"Echo\"", "\"echo\"")
        .Replace("\"MessageType\"", "\"message_type\"")
        .Replace("\"UserId\"", "\"user_id\"")
        .Replace("\"GroupId\"", "\"group_id\"")
        .Replace("\"Message\"", "\"message\"")
        .Replace("\"AutoEscape\"", "\"auto_escape\"");
        return sendingJsonString;
    }

    //群聊消息JSON
    string GenerateSendingGroupMessageJsonString(string message, Int64? groupId, Int64? userId, string action)
    {
        var aiteMessage = userId != null ? $"[CQ:at,qq={userId}]\n" + message : message;
        SendingJson sendingJson = new SendingJson();
        sendingJson.Action = action;
        sendingJson.Echo = action;
        sendingJson.Params = new Params
        {
            GroupId = groupId,
            Message = aiteMessage,
            AutoEscape = false
        };
        var sendingJsonString = JsonSerializer.Serialize(sendingJson);
        sendingJsonString = ProcessSendingJsonString(sendingJsonString);
        return sendingJsonString;
    }

    //群聊图片JSON
    string GenerateSendingGroupImageJsonString(string prompts, string url, Int64? groupId, Int64? userId, string action)
    {
        Guid guid = Guid.NewGuid();
        var aiteMessage = userId != null ? $"[CQ:at,qq={userId}]\t{prompts} [CQ:image,file={guid}-Image_{DateTime.Now.ToString("yyyy_MM_dd_HH_mm_ss")}.png,url={url},cache=0]" : $" [CQ:image,file={guid}-Image_{DateTime.Now.ToString("yyyy_MM_dd_HH_mm_ss")}.png,url={url}],cache=0]";
        SendingJson sendingJson = new SendingJson();
        sendingJson.Action = action;
        sendingJson.Echo = action;
        sendingJson.Params = new Params
        {
            GroupId = groupId,
            Message = aiteMessage,
            AutoEscape = false
        };
        var sendingJsonString = JsonSerializer.Serialize(sendingJson);
        sendingJsonString = ProcessSendingJsonString(sendingJsonString);
        return sendingJsonString;
    }
    // //私聊JSON
    // string GenerateSendingPrivateMessageJsonString(string message, Int64? userId, string action)
    // {
    //     var aiteMessage = userId != null ? $"[CQ:at,qq={userId}] " + message : message;
    //     SendingJson sendingJson = new SendingJson();
    //     sendingJson.Action = action;
    //     sendingJson.Echo = action;
    //     sendingJson.Params = new Params
    //     {
    //         Message = aiteMessage,
    //         AutoEscape = false
    //     };
    //     var sendingJsonString = JsonSerializer.Serialize(sendingJson);
    //     sendingJsonString = ProcessSendingJsonString(sendingJsonString);
    //     return sendingJsonString;
    // }

    //处理ChatGPT消息
    string FilterChatGptMessage(string message)
    {
        return message.TrimStart().TrimEnd();
    }

    //发送群聊消息
    void SendGroupMessage(string message, Int64? groupId, Int64? userId)
    {
        message = FilterChatGptMessage(message);
        var sendingJsonString = GenerateSendingGroupMessageJsonString(message, groupId, userId, "send_group_msg");
        Send(sendingJsonString);
    }

    //发送群聊图片
    void SendGroupImage(string prompts, string url, Int64? groupId, Int64? userId)
    {
        var sendingJsonString = GenerateSendingGroupImageJsonString(prompts, url, groupId, userId, "send_group_msg");
        Send(sendingJsonString);
    }

    // //发送私聊消息  
    // void SendPrivateMessage(string message, Int64? userId)
    // {
    //     message = FilterChatGptMessage(message);
    //     var sendingJsonString = GenerateSendingPrivateMessageJsonString(message, userId, "send_msg");
    //     Send(sendingJsonString);
    // }

}



