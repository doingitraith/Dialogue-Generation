using System.Collections;
using System.Collections.Generic;
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
}
