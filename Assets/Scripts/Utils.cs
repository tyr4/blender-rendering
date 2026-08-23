using System.Collections.Generic;
using JetBrains.Annotations;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;

public static class Utils
{
    private static string StripResponse(string response)
    {
        const string marker = "RESPONSE_END_STDOUT";

        if (!response.StartsWith(marker)) return null;
        
        return response.Substring(marker.Length);
    }

    public static JObject DeserializeJson(string input)
    {
        var stripped = StripResponse(input) ?? input;
        
        return JObject.Parse(stripped);
    }

    public static JToken GetJsonValue(JObject json, string key)
    {
        return json?[key];
    }

    public static JToken GetJsonValue(string input, string key)
    {
        var json = DeserializeJson(input);
        
        return GetJsonValue(json, key);
    }

    public static JToken GetJsonMessageResponse(string input)
    {
        var json = DeserializeJson(input);
        
        return GetJsonValue(json, "message");
    }
    
    public static string GetJsonStatusResponse(string input)
    {
        var json = DeserializeJson(input);
        
        return GetJsonValue(json, "status")?.ToString();
    }
}
