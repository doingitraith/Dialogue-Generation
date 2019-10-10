using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class DialogueParticipant : MonoBehaviour
{
    public string name;
    public List<IntentId> dialogueGoals;
    //public List<IntentId> initDialogueBacklog;
    public Stack<Reply> currentIntentBacklog;
    public List<Reply> replyOptions;
    public float moodValue;
    public Intent currentIntent;
    private int positiveCounter;

    public override string ToString()
    {
        return name;
    }

    void Awake()
    {
        positiveCounter = 0;
        moodValue = 0.0f;
        replyOptions = new List<Reply>();
        currentIntentBacklog = new Stack<Reply>();
        currentIntentBacklog.Push(new Reply(){Id = dialogueGoals.First()});
        /*
        for(int i = initDialogueBacklog.Count-1; i >= 0; i--)
            currentIntentBacklog.Push(initDialogueBacklog[i]);
        */
    }

    public void UpdateMood(Intent intent)
    {
        if (intent == null)
            return;

        float modifier = .0f;
        float sentiment = intent.SentimentModifier;
        float expectancy = intent.ExpectancyValue;

        if (sentiment == .0f)
            modifier = expectancy;
        else if (sentiment < .0f && expectancy < .0f)
            modifier = -(sentiment * expectancy);
        else
            modifier = sentiment * expectancy;

        if (modifier > .0f)
            positiveCounter++;
        
        moodValue = Mathf.Clamp(moodValue+modifier, -1.0f, 1.0f);
        if (moodValue <= -1.0f)
            AbortDialogue();
    }

    public void CheckGoals(Intent intent)
    {
        if (dialogueGoals.Count == 0)
            FinishDialogue();
        else if (dialogueGoals.Contains(intent.Id))
        {
            dialogueGoals.Remove(intent.Id);
            if(dialogueGoals.Count > 0)
                currentIntentBacklog.Push(new Reply(){Id = dialogueGoals.First()});
        }

    }

    private void FinishDialogue()
    {
        if(positiveCounter >=3)
            currentIntentBacklog.Push(new Reply(){Id = IntentId.Buy});
        else
            currentIntentBacklog.Push(new Reply(){Id = IntentId.NotBuy});
    }
    
    private void AbortDialogue()
    {
        currentIntentBacklog.Push(new Reply(){Id = IntentId.Outrage});
    }
}
