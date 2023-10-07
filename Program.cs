string url = "ws://127.0.0.1:15999";


WebSocketServer webSocketServer = new WebSocketServer(url);

webSocketServer.AddWebSocketService<AI>("/AI");
webSocketServer.Start();
Console.WriteLine($"WebsocketServer Start On {url}");
Console.WriteLine($"WebsocketServer AddWebSocketService {nameof(AI)}");

A:
await Task.Delay(1000000000);
goto A;



//注册WebsocketServer路由CQAPI
public partial class AI : WebSocketBehavior
{
    string apiKey = "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";
    string chatApiUrl = "https://api.openai.com/v1/chat/completions";
    string generationApiUrl = "https://api.openai.com/v1/images/generations";

    string varyApiUrl = "https://api.openai.com/v1/images/variations";

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

    public async Task<String> GetDALLE2CreateImagesAsync(string prompts, string resolution = "1024x1024", int n = 1)
    {
        var data = new
        {
            n = n,
            prompt = prompts,
            size = resolution,
            // user = user
        };
        var json = JsonSerializer.Serialize(data);
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        List<Url> dalle2ResponseData = null;
        string? responseContent = null;
        try
        {
            var httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiKey}");
            // httpClient.DefaultRequestHeaders.Add("Organization", "org-jzBeBdUq4xCadFXGMYwiljWI");


            var dalle2HttpResponse = await httpClient.PostAsync(generationApiUrl, content);
            responseContent = await dalle2HttpResponse.Content.ReadAsStringAsync();
            Console.WriteLine("HttpResponseContent:\n" + responseContent);

            var dalle2ResponseClass = JsonSerializer.Deserialize<DALLE2ResponseJson>(responseContent);
            dalle2ResponseData = dalle2ResponseClass?.data ?? null;

        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
        }
        if (dalle2ResponseData == null)
        {
            var imageErrorJson = JsonSerializer.Deserialize<ImageErrorJson>(responseContent);
            return "0" + imageErrorJson?.error.message;
        }
        else
        {
            List<string> imageUrlList = new List<string>();
            dalle2ResponseData.ForEach((v) => imageUrlList.Add(v.url!));
            return imageUrlList[0];
        }
    }

    public async Task<string> GetDALLE2VaryImagesAsync(byte[] imageBytes, Int16 n, string size, string? user = null)
    {
        List<Url>? data = null;
        string? responseContent = null;
        try
        {
            var httpClient = new HttpClient();
            httpClient.BaseAddress = new Uri(varyApiUrl);
            httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiKey}");
            httpClient.DefaultRequestHeaders.Add("Organization", "org-jzBeBdUq4xCadFXGMYwiljWI");

            var content = new MultipartFormDataContent();
            content.Add(new ByteArrayContent(imageBytes), "image", "A.png");
            content.Add(new StringContent(n.ToString()), "n");
            content.Add(new StringContent(size), "size");

            var dalle2HttpResponse = await httpClient.PostAsync(varyApiUrl, content);

            responseContent = await dalle2HttpResponse.Content.ReadAsStringAsync();
            Console.WriteLine("HttpResponseContent:\n" + responseContent);

            var dalle2ResponseClass = JsonSerializer.Deserialize<DALLE2ResponseJson>(responseContent);
            data = dalle2ResponseClass?.data ?? null;

        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
        }
        if (data is null)
        {
            var imageErrorJson = JsonSerializer.Deserialize<ImageErrorJson>(responseContent);
            return "0" + imageErrorJson?.error.message;
        }
        else
        {
            List<string> imageUrlList = new List<string>();
            data.ForEach((v) => imageUrlList.Add(v.url!));
            return imageUrlList[0];
        }
    }

    protected override async void OnMessage(MessageEventArgs e)
    {
        //Handle Reverse Message
        var receivedJson = ConvertReceivedJsonToClass(e.Data);        //转换GO-CQHTTPX为ReceivedJson类
        await Task.Run(async () =>
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
                     var index = receivedRawMessage.IndexOf($"[CQ:at,qq={receivedJson.SelfId}]") + $"[CQ:at,qq={receivedJson.SelfId}]".Length - 1;
                     try
                     {
                         receivedRawMessage = receivedRawMessage[(index + 1)..].TrimStart().TrimEnd();  //截取有效信息段
                         if (string.IsNullOrWhiteSpace(receivedRawMessage))
                         {
                             var message = "What to do ?";
                             try
                             {
                                 SendGroupMessage(message, receivedJson.GroupId, receivedJson.UserId);        //将ChatGTP响应 数据发送到GO-CQHTTPX
                             }
                             catch (Exception exception)
                             {
                                 SendGroupMessage(exception.Message, receivedJson.GroupId, receivedJson.UserId);
                             }
                         }
                         else
                         {
                             var spaceIndex = receivedRawMessage.IndexOf(" ");
                             bool isCreateImage = false;
                             bool isVaryImage = false;
                             try
                             {
                                 isCreateImage = receivedRawMessage.Substring(0, spaceIndex).Trim().ToUpper() == "CREATE";
                                 isVaryImage = receivedRawMessage.Substring(0, spaceIndex).Trim().ToUpper() == "VARY";
                             }
                             catch
                             {
                                 isCreateImage = false;
                                 isVaryImage = false;
                             }
                             //处理Create图片
                             if (isCreateImage)
                             {
                                 SendGroupMessage("Creating..........", receivedJson.GroupId, receivedJson.UserId);

                                 var prompts = receivedRawMessage[(spaceIndex + 1)..].TrimStart();
                                 var message = await GetDALLE2CreateImagesAsync(prompts, "1024x1024", 1);
                                 //    HttpClient httpClient = new HttpClient();
                                 //    Guid guid = Guid.NewGuid();
                                 //    ImageModel imageModel = new ImageModel { Name = $"{guid}-Image_{DateTime.Now.ToString("yyyy_MM_dd_HH_mm_ss")}.png", Url = url };
                                 //    imageModel.ImageBytes = await httpClient.GetByteArrayAsync(imageModel.Url);
                                 try
                                 {

                                     if (message[0] is '0')
                                         if (message is "0" + "Your request was rejected as a result of our safety system. Your prompt may contain text that is not allowed by our safety system.")
                                         {
                                             SendGroupMessage("虎狼之辞[CQ:face,id=14]", receivedJson.GroupId, receivedJson.UserId);        //将ChatGTP响应 数据发送到GO-CQHTTPX
                                             return;
                                         }
                                         else if (message.Contains("is too long - 'prompt'"))
                                         {
                                             SendGroupMessage("提示词过长，请简短提示[CQ:face,id=14]", receivedJson.GroupId, receivedJson.UserId);        //将ChatGTP响应 数据发送到GO-CQHTTPX
                                             return;
                                         }
                                         else
                                         {
                                             SendGroupMessage("服务器过载，稍后再试[CQ:face,id=14]", receivedJson.GroupId, receivedJson.UserId);        //将ChatGTP响应 数据发送到GO-CQHTTPX
                                             return;
                                         }


                                     else
                                     {
                                         var url = message;
                                         SendGroupImage(prompts, url, receivedJson.GroupId, receivedJson.UserId);        //将ChatGTP响应 数据发送到GO-CQHTTPX
                                     }

                                 }
                                 catch (Exception exception)
                                 {
                                     SendGroupMessage(exception.Message, receivedJson.GroupId, receivedJson.UserId);        //将ChatGTP响应 数据发送到GO-CQHTTPX
                                 }
                                 finally
                                 {
                                 }
                             }
                             //处理Vary图片

                             else if (isVaryImage)
                             {

                                 SendGroupMessage("Varying..........", receivedJson.GroupId, receivedJson.UserId);

                                 string pattern = @"\[CQ:image,file=\w+\.image,subType=\d+,url=(?<url>.*?);is_origin=\d+.*?\]";
                                 var imageString = receivedRawMessage[(spaceIndex + 1)..].TrimStart();

                                 Match match = Regex.Match(imageString, pattern);
                                 if (match.Success)
                                 {

                                     try
                                     {
                                         string inputUrl = match.Groups["url"].Value;
                                         Console.WriteLine(inputUrl);
                                         HttpClient httpClient = new HttpClient();
                                         Guid guid = Guid.NewGuid();
                                         ImageModel imageModel = new ImageModel { Name = $"{guid}-Image_{DateTime.Now.ToString("yyyy_MM_dd_HH_mm_ss")}.png", Url = inputUrl };
                                         imageModel.ImageBytes = await httpClient.GetByteArrayAsync(imageModel.Url);

                                         // 将字节数组转换为 ImageSharp 的 Image 对象
                                         using var temporaryImage = Image.Load<Rgba32>(imageModel.ImageBytes);

                                         // 转换为 PNG 格式
                                         using var outputStream = new MemoryStream();
                                         temporaryImage.Save(outputStream, new PngEncoder());

                                         // 获取字节数组
                                         var pngBytes = outputStream.ToArray();
                                         long sizeInBytes = pngBytes.Length;

                                         if (sizeInBytes > 1024 * 1024 * 4)
                                         {
                                             SendGroupMessage("图片必须小于4MB[CQ:face,id=14]", receivedJson.GroupId, receivedJson.UserId);        //将ChatGTP响应 数据发送到GO-CQHTTPX
                                             return;
                                         }
                                         if (temporaryImage.Width != temporaryImage.Height)
                                         {
                                             SendGroupMessage("图片长宽不一致[CQ:face,id=14]", receivedJson.GroupId, receivedJson.UserId);        //将ChatGTP响应 数据发送到GO-CQHTTPX
                                             return;
                                         }
                                         string message = await GetDALLE2VaryImagesAsync(pngBytes, 1, "1024x1024");

                                         if (message[0] is '0')

                                             if (message.Contains("0" + "Invalid input image - expected an image with equal width and height"))
                                             {
                                                 SendGroupMessage("请发送长宽一致的图片[CQ:face,id=14]", receivedJson.GroupId, receivedJson.UserId);        //将ChatGTP响应 数据发送到GO-CQHTTPX
                                                 return;
                                             }
                                             else if (message.Contains("Uploaded image must be a PNG and less than 4 MB."))
                                             {
                                                 SendGroupMessage("转换为PNG失败,最好传送PNG图片[CQ:face,id=14]", receivedJson.GroupId, receivedJson.UserId);        //将ChatGTP响应 数据发送到GO-CQHTTPX
                                                 return;
                                             }
                                             else if (message.Contains("Invalid input image - format must be in ['RGB', 'RGBA']"))
                                             {
                                                 SendGroupMessage("图片要为RGBA或RGB格式[CQ:face,id=14]", receivedJson.GroupId, receivedJson.UserId);        //将ChatGTP响应 数据发送到GO-CQHTTPX
                                                 return;
                                             }
                                             else
                                             {
                                                 SendGroupMessage("服务器过载，稍后再试[CQ:face,id=14]", receivedJson.GroupId, receivedJson.UserId);        //将ChatGTP响应 数据发送到GO-CQHTTPX
                                                 return;
                                             }

                                         else
                                         {
                                             var url = message;
                                             SendGroupImage("", url, receivedJson.GroupId, receivedJson.UserId);        //将ChatGTP响应 数据发送到GO-CQHTTPX
                                         }

                                     }
                                     catch (Exception exception)
                                     {
                                         SendGroupMessage(exception.Message, receivedJson.GroupId, receivedJson.UserId);        //将ChatGTP响应 数据发送到GO-CQHTTPX
                                     }
                                     finally
                                     {
                                     }
                                 }
                                 else
                                 {
                                     SendGroupMessage("你发的是图片吗[CQ:face,id=14]", receivedJson.GroupId, receivedJson.UserId);
                                     return;
                                 }
                             }
                             //处理消息
                             else
                             {
                                 SendGroupMessage("Processing..........", receivedJson.GroupId, receivedJson.UserId);

                                 String chatGptResponseMessageString;

                                 try                                                                                                                 //TODO:Handle Positive Message
                                 {
                                     // gpt-3.5-turbo gpt-3.5-turbo-0301
                                     chatGptResponseMessageString = await GetChatGptResponseAsync(model: "gpt-3.5-turbo", role: "user", content: receivedRawMessage);  //向ChatGPT发送信息，并获取响应

                                     SendGroupMessage(chatGptResponseMessageString, receivedJson.GroupId, receivedJson.UserId);        //将ChatGTP响应 数据发送到GO-CQHTTPX
                                     return;


                                 }
                                 catch (TimeoutException)
                                 {
                                     SendGroupMessage("服务器超时", receivedJson.GroupId, receivedJson.UserId);
                                 }
                                 catch (Exception exception)
                                 {
                                     SendGroupMessage(exception.Message, receivedJson.GroupId, receivedJson.UserId);
                                 }
                             }
                         }
                     }
                     catch (Exception exception)
                     {
                         Console.WriteLine(exception.Message);
                     }
                 }

                 //  else if (receivedJson.MessageType == MessageType.Private)
                 //  {
                 //      if (!privateAccountList.Contains(receivedJson.UserId.ToString()))
                 //      {
                 //          SendPrivateMessage("人家不认识你呀,先去找雾都杀手添加身份认证吧", receivedJson.UserId);        //将ChatGTP响应 数据发送到GO-CQHTTPX
                 //          return;
                 //      }
                 //      Console.WriteLine(e.Data);
                 //      String chatGptResponseMessageString;
                 //      try
                 //      {
                 //          receivedRawMessage = receivedRawMessage.TrimStart();  //截取有效信息段
                 //          if (string.IsNullOrWhiteSpace(receivedRawMessage))
                 //              chatGptResponseMessageString = "What to do ?";
                 //          else
                 //              // gpt-3.5-turbo gpt-3.5-turbo-0301

                 //              chatGptResponseMessageString = await GetChatGptResponseAsync(model: "gpt-3.5-turbo", role: "user", content: receivedRawMessage);  //向ChatGPT发送信息，并获取响应
                 //      }
                 //      catch (TimeoutException)
                 //      {
                 //          chatGptResponseMessageString = "Time Out !";
                 //      }
                 //      try
                 //      {
                 //          SendPrivateMessage(chatGptResponseMessageString, receivedJson.UserId);        //将ChatGTP响应 数据发送到GO-CQHTTPX

                 //      }
                 //      catch (Exception exception)
                 //      {
                 //          SendPrivateMessage(exception.Message, receivedJson.UserId);
                 //      }
                 //  }

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


public class DALLE2ResponseJson
{
    public Int64? created { get; set; }
    public List<Url>? data { get; set; }
}

//反序列化类
public class Url
{
    public string? url { get; set; }
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