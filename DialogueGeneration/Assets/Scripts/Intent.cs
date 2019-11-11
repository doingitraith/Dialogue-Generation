using System.Collections.Generic;
using System.Linq;
using UnityEngine;

// information holding class for a dialogue intent
public class Intent
{
    /// <summary>
    /// Id to identify the intent
    /// </summary>
    public IntentId Id;
    /// <summary>
    /// A modifier on how the intent influences the mood of a character
    /// </summary>
    public float SentimentModifier;
    /// <summary>
    /// A value indicating how much a certain intent is expected if it is a reply
    /// </summary>
    public float ExpectancyValue;
    /// <summary>
    /// The caption for the reply button
    /// </summary>
    public string Label;
    /// <summary>
    /// The filename of the Expressionist grammar file
    /// </summary>
    public string GrammarName;
    /// <summary>
    /// Indicates if an intent is a reaction to a reply
    /// </summary>
    public bool IsReaction;

    /// <summary>
    /// Compares two intents based on their Id
    /// </summary>
    /// <param name="other">The Intent to compare to</param>
    /// <returns>true if the inttents are equal, false otherwise</returns>
    public bool Equals(Intent other)
    {
        return this.Id.Equals(other.Id);
    }

    /// <summary>
    /// Return the name of the gramar file
    /// </summary>
    /// <returns>the string represantation of the intent</returns>
    public override string ToString()
    {
        return GrammarName;
    }

    /// <summary>
    /// Gets an element from a list of details (singular)
    /// </summary>
    /// <param name="idx">the index at which to return the detail</param>
    /// <returns>string represnting the detail (singular)</returns>
    public static string GetRandomDetail(int idx)
    {
        List<string> s = new List<string>(){"camera", "display", "battery", "resolution"};
        return s.ElementAt(idx);
    }

    /// <summary>
    /// Gets an element from a list of details (plural)
    /// </summary>
    /// <param name="idx">the index at which to return the detail</param>
    /// <returns>string represnting the detail (plural)</returns>
    public static string GetRandomDetails(int idx)
    {
        List<string> s = new List<string>(){"cameras", "displays", "batteries", "resolutions"};
        return s.ElementAt(idx);
    }

    /// <summary>
    /// Gets an element from a list of features (singular)
    /// </summary>
    /// <param name="idx">the index at which to return the feature</param>
    /// <returns>string represnting the feature (singular)</returns>
    public static string GetRandomFeature(int idx)
    {
        List<string> s = new List<string>(){"processor core", "camera", "megapixel", "color variation"};
        return s.ElementAt(idx);
    }
    /// <summary>
    /// Gets an element from a list of features (plural)
    /// </summary>
    /// <param name="idx">the index at which to return the feature</param>
    /// <returns>string represnting the feature (plural)</returns>
    public static string GetRandomFeatures(int idx)
    {
        List<string> s = new List<string>(){"processor cores", "cameras", "megapixels", "color variations"};
        return s.ElementAt(idx);
    }

    /// <summary>
    /// Gets a string from a random range representing a price
    /// </summary>
    /// <returns>string represnting the price</returns>
    public static string GetRandomPrice()
    {
        return Random.Range(200, 500).ToString();
    }

    /// <summary>
    /// Gets a string from a random range representing a delivery time
    /// </summary>
    /// <returns>string represnting the delivery time</returns>
    public static string GetRandomDeliveryTime()
    {
        return Random.Range(3, 15).ToString();
    }
    /// <summary>
    /// Gets a string from a random range representing a number of items
    /// </summary>
    /// <returns>string represnting the number</returns>
    public static string GetRandomNumber()
    {
        return Random.Range(2, 4).ToString();
    }
}
