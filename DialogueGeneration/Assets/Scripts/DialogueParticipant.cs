using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DialogueParticipant : MonoBehaviour
{
    public List<IntentID> DialogueGoals;
    public Stack<IntentID> CurrentIntentBacklog;
    public float SentimentValue;
    public Intent CurrentIntent;

    void Start()
    {
        DialogueGoals = new List<IntentID>();
        CurrentIntentBacklog = new Stack<IntentID>();
        SentimentValue = 0.0f;
    }
}
