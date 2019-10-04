using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DialogueParticipant : MonoBehaviour
{
    public List<IntentId> dialogueGoals;
    public List<IntentId> initDialogueBacklog;
    public Stack<IntentId> currentIntentBacklog;
    public float sentimentValue;
    public Intent currentIntent;


    private void Awake()
    {
        currentIntentBacklog = new Stack<IntentId>();
        for(int i = initDialogueBacklog.Count-1; i >= 0; i--)
            currentIntentBacklog.Push(initDialogueBacklog[i]);
    }

    void Start()
    {
        sentimentValue = 0.0f;
    }
}
