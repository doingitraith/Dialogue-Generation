using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Intent
{
    public IntentId Id;
    public float SentimentModifier;
    public float ExpectancyValue;
    public string Label;
    public string GrammarName;
    public bool IsReaction;

    public bool Equals(Intent other)
    {
        return this.Id.Equals(other.Id);
    }

    public override string ToString()
    {
        return GrammarName;
    }

    public static string GetRandomDetail(int idx)
    {
        List<string> s = new List<string>(){"camera", "display", "battery", "resolution"};
        return s.ElementAt(idx);
    }
    public static string GetRandomDetails(int idx)
    {
        List<string> s = new List<string>(){"cameras", "displays", "batteries", "resolutions"};
        return s.ElementAt(idx);
    }
    
    public static string GetRandomFeature(int idx)
    {
        List<string> s = new List<string>(){"processor core", "camera", "megapixel", "color variation"};
        return s.ElementAt(idx);
    }
    public static string GetRandomFeatures(int idx)
    {
        List<string> s = new List<string>(){"processor cores", "cameras", "megapixels", "color variations"};
        return s.ElementAt(idx);
    }

    public static string GetRandomPrice()
    {
        return Random.Range(200, 500).ToString();
    }
    
    public static string GetRandomDeliveryTime()
    {
        return Random.Range(3, 15).ToString();
    }
    public static string GetRandomNumber()
    {
        return Random.Range(2, 4).ToString();
    }
}
