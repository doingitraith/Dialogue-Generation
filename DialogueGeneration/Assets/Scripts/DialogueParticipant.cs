using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DialogueParticipant : MonoBehaviour
{
    public List<IntentId> dialogueGoals;
    public List<IntentId> initDialogueBacklog;
    public Stack<IntentId> currentIntentBacklog;
    public List<Intent> replyOptions;
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
        replyOptions = new List<Intent>();
    }

    public void UpdateMood(Intent? intent)
    {
        if (intent == null)
            return;
        
        sentimentValue = Mathf.Clamp(sentimentValue+intent.Value.SentimentModifier, -1.0f, 1.0f);
        if (sentimentValue <= -1.0f)
            AbortDialogue();
    }

    public void CheckGoals(Intent intent)
    {
        if (dialogueGoals.Contains(intent.Id))
        {
            dialogueGoals.Remove(intent.Id);
            if (dialogueGoals.Count == 0)
                FinishDialogue();
        }

    }

    private void FinishDialogue()
    {
        // TODO: Positive End
        throw new NotImplementedException();
    }
    
    private void AbortDialogue()
    {
        // TODO: Negative End
        throw new NotImplementedException();
    }
}
