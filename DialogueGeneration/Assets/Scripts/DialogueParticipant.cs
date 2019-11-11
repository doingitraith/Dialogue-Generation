using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Random = UnityEngine.Random;

/// <summary>
/// An enum for character gender types
/// </summary>
public enum Gender
{
    Male,
    Female
}

/// <summary>
/// Represents a character in the game, player or NPC
/// </summary>
public class DialogueParticipant : MonoBehaviour
{
    /// <summary>
    /// the name of the character
    /// </summary>
    public string name;
    /// <summary>
    /// the gender of the character for visual in game representation
    /// </summary>
    public Gender gender;
    /// <summary>
    /// A list of intents that the character wants to state
    /// </summary>
    public List<IntentId> dialogueGoals;
    /// <summary>
    /// The list of currently planned statements for the character
    /// </summary>
    public Stack<Reply> currentIntentBacklog;
    /// <summary>
    /// The current reply options for a character
    /// </summary>
    public List<Reply> replyOptions;
    /// <summary>
    /// The current mood of a character
    /// </summary>
    public float moodValue;
    /// <summary>
    /// The surrent intent of the character
    /// </summary>
    public Intent currentIntent;
    /// <summary>
    /// The male character avatar
    /// </summary>
    public Avatar maleAvatar;
    /// <summary>
    /// The female character avatar
    /// </summary>
    public Avatar femaleAvatar;
    /// <summary>
    /// The male character model
    /// </summary>
    public Mesh maleMesh;
    /// <summary>
    /// The female character model
    /// </summary>
    public Mesh femaleMesh;

    /// <summary>
    /// Helper flag to indicate if the list of goals has already been randomized
    /// </summary>
    private bool _isGoalsRandomized;
    /// <summary>
    /// Helper int to count the number of expectied replies
    /// </summary>
    private int _positiveCounter;

    /// <summary>
    /// Returns the name of the character
    /// </summary>
    /// <returns>string representation of the name</returns>
    public override string ToString()
    {
        return name;
    }

    /// <summary>
    /// Initializing the character to start a new dialogue
    /// </summary>
    void Awake()
    {
        _positiveCounter = 0;
        _isGoalsRandomized = false;
        moodValue = 0.0f;
        replyOptions = new List<Reply>();
        currentIntentBacklog = new Stack<Reply>();
        currentIntentBacklog.Push(new Reply(){Id = dialogueGoals.First()});
        InitCharacter();
    }

    /// <summary>
    /// Set the visual representation of the character based on the gender
    /// </summary>
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

    /// <summary>
    /// Update the mood of a character
    /// </summary>
    /// <param name="intent"></param>
    public void UpdateMood(Intent intent)
    {
        if (intent == null)
            return;

        // Get the values
        float modifier = .0f;
        float sentiment = intent.SentimentModifier;
        float expectancy = intent.ExpectancyValue;

        // if the sentiment is not specified, just use the expectancy
        if (sentiment == .0f)
            modifier = expectancy;
        // make sure that the modifier stays negative if both expectancy and sentiment are negative
        else if (sentiment < .0f && expectancy < .0f)
            modifier = -(sentiment * expectancy);
        // multiply sentiment with the expectancy
        else
            modifier = sentiment * expectancy;

        // if the modifier is positive, update the counter
        if (modifier > .0f)
            _positiveCounter++;

        // make sure the mood stays in bounds
        moodValue = Mathf.Clamp(moodValue+modifier, -1.0f, 1.0f);
        // exit if the mood drops below -1.0
        if (moodValue <= -1.0f)
            AbortDialogue();
    }

    /// <summary>
    /// Check if all goals have been satisfied
    /// </summary>
    /// <param name="intent">the intrent that has been stated</param>
    public void CheckGoals(Intent intent)
    {
        if (dialogueGoals.Count == 0)
            FinishDialogue();
        // Remove the statet intent from the goals
        else if (dialogueGoals.Contains(intent.Id))
        {
            dialogueGoals.Remove(intent.Id);
            // Randomize the order of the goals in the beginning
            if (!_isGoalsRandomized)
            {
                _isGoalsRandomized = true;
                dialogueGoals = dialogueGoals.OrderBy(x => Random.value).ToList();
            }
            // Push the next goal on the backlog of intents
            if(dialogueGoals.Count > 0)
                currentIntentBacklog.Push(new Reply(){Id = dialogueGoals.First()});
        }

    }

    /// <summary>
    /// End the dialogue with a buying decision
    /// </summary>
    private void FinishDialogue()
    {
        // if three or more goals have been satisifed, buy the product
        if(_positiveCounter >=3)
            currentIntentBacklog.Push(new Reply(){Id = IntentId.Buy});
        else
            currentIntentBacklog.Push(new Reply(){Id = IntentId.NotBuy});
    }
    
    /// <summary>
    /// End the dialogue on a negative note
    /// </summary>
    private void AbortDialogue()
    {
        currentIntentBacklog.Push(new Reply(){Id = IntentId.Outrage});
        _positiveCounter = -100;
    }
}
