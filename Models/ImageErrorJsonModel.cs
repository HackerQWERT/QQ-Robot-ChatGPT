public class ImageErrorJson
{
    [JsonIgnore(Condition = JsonIgnoreCondition.Never)] public Error error { get; set; }
}
public class Error
{
    [JsonIgnore(Condition = JsonIgnoreCondition.Never)] public string code { get; set; }
    [JsonIgnore(Condition = JsonIgnoreCondition.Never)] public string message { get; set; }
    [JsonIgnore(Condition = JsonIgnoreCondition.Never)] public string param { get; set; }
    [JsonIgnore(Condition = JsonIgnoreCondition.Never)] public string type { get; set; }
}