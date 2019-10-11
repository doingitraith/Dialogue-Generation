using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Random = UnityEngine.Random;

public enum Gender
{
    Male,
    Female
}

public class DialogueParticipant : MonoBehaviour
{
    public string name;
    public Gender gender;
    public List<IntentId> dialogueGoals;
    //public List<IntentId> initDialogueBacklog;
    public Stack<Reply> currentIntentBacklog;
    public List<Reply> replyOptions;
    public float moodValue;
    public Intent currentIntent;
    public Avatar maleAvatar;
    public Avatar femaleAvatar;
    public Mesh maleMesh;
    public Mesh femaleMesh;

    private bool _isGoalsRandomized;
    private int _positiveCounter;

    public override string ToString()
    {
        return name;
    }

    void Awake()
    {
        _positiveCounter = 0;
        _isGoalsRandomized = false;
        moodValue = 0.0f;
        replyOptions = new List<Reply>();
        currentIntentBacklog = new Stack<Reply>();
        currentIntentBacklog.Push(new Reply(){Id = dialogueGoals.First()});
        /*
        for(int i = initDialogueBacklog.Count-1; i >= 0; i--)
            currentIntentBacklog.Push(initDialogueBacklog[i]);
        */
        InitCharacter();
    }

    public void InitCharacter()
    {
        int g = Random.Range(0, 2);
        if (g == 0)
        {
            gender = Gender.Male;
            GetComponent<Animator>().avatar = maleAvatar;
            SkinnedMeshRenderer r = GetComponentInChildren<SkinnedMeshRenderer>();
            r.sharedMesh = maleMesh;
        }
        else
        {
            gender = Gender.Female;
            GetComponent<Animator>().avatar = femaleAvatar;
            SkinnedMeshRenderer r = GetComponentInChildren<SkinnedMeshRenderer>();
            r.sharedMesh = femaleMesh;
        }
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
            _positiveCounter++;
        
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
            if (!_isGoalsRandomized)
            {
                _isGoalsRandomized = true;
                dialogueGoals = dialogueGoals.OrderBy(x => Random.value).ToList();
            }
            if(dialogueGoals.Count > 0)
                currentIntentBacklog.Push(new Reply(){Id = dialogueGoals.First()});
        }

    }

    private void FinishDialogue()
    {
        if(_positiveCounter >=3)
            currentIntentBacklog.Push(new Reply(){Id = IntentId.Buy});
        else
            currentIntentBacklog.Push(new Reply(){Id = IntentId.NotBuy});
    }
    
    private void AbortDialogue()
    {
        currentIntentBacklog.Push(new Reply(){Id = IntentId.Outrage});
        _positiveCounter = -100;
    }
}
