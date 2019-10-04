using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;

public enum IntentId
{
    Greeting,
    ReplyGreeting
}

public struct Intent
{
    public IntentId Id;
    public float SentimentModifier;
    public string Label;
    public string GrammarName;

    public bool Equals(Intent other)
    {
        return this.Id.Equals(other.Id);
    }

    public override string ToString()
    {
        return GrammarName;
    }
}

public struct ExpressionistRequest
{
    public readonly List<string> mustHaveTags;
    public readonly List<string> mustNotHaveTags;
    public readonly List<Tuple<string, int>> scoringMetric;
    public readonly List<Tuple<string, string>> state;

    public ExpressionistRequest(List<string> mustHaveTags, List<string> mustNotHaveTags, 
        List<Tuple<string, int>> scoringMetric, List<Tuple<string, string>> state)
    {
        this.mustHaveTags = mustHaveTags;
        this.mustNotHaveTags = mustNotHaveTags;
        this.scoringMetric = scoringMetric;
        this.state = state;
    }
}

public class DialogueState : MonoBehaviour
{
    public PythonEndpoint pythonEndpoint;
    public bool isPlayerTurn;
    public Dictionary<IntentId, List<IntentId>> reactions;
    public List<Intent> allIntents;
    public string currentText;
    public DialogueParticipant player;
    public DialogueParticipant npc;
    public UserInterface userInterface;

    private string _generatedText;
    private List<Sentiment> _sentiments;
    private string _selectedGrammar;
    private bool _isUpdated;
    private bool _isTextGenerated;


    void Start()
    {
        pythonEndpoint.OnTextGenerated += UpdateGeneratedText;
        pythonEndpoint.OnSentimentProcessed += UpdateSentiments;
        isPlayerTurn = false;
        currentText = "";
        InitializeIntents();
        
        _generatedText = "";
        _sentiments = new List<Sentiment>();
        _selectedGrammar = "introduction";
        _isUpdated = false;
        _isTextGenerated = false;

        WriteDialogue();
    }

    void Update()
    {
        if (_isUpdated)
        {
            currentText = _generatedText;
            userInterface.PresentText(isPlayerTurn, currentText);
            _isUpdated = false;
        }
    }
    
    private void WriteDialogue()
    {
        Intent intent = GetNextIntent();
        ExpressionistRequest request = new ExpressionistRequest(null, null, null, null);
        GetTextForIntent(intent, request);
        AddReactionIntents(intent.Id);
    }

    public void AddReactionIntents(IntentId reactTo)
    {
        List<IntentId> replies = reactions[reactTo];
        if (reactions != null && isPlayerTurn)
            replies.ForEach(i => npc.currentIntentBacklog.Push(i));
    }

    public Intent GetNextIntent()
    {
        IntentId id = isPlayerTurn ? player.currentIntentBacklog.Pop() : npc.currentIntentBacklog.Pop();
        return allIntents.Find(i => i.Id.Equals(id));
    }

    public void GetTextForIntent(Intent intent, ExpressionistRequest request)
    {
        pythonEndpoint.ExpressionistRequestCode(intent.ToString(), request.mustHaveTags,request.mustNotHaveTags, request.scoringMetric, request.state);
        
        while (!_isTextGenerated)
            _generatedText = pythonEndpoint.currentGeneratedString;
		
        _isTextGenerated = false;
		
        pythonEndpoint.ExecuteSentimentAnalysis(_generatedText);
    }
    
    private void UpdateGeneratedText()
    {
        _generatedText = pythonEndpoint.currentGeneratedString;
        _isTextGenerated = true;
        //isUpdated = true;
    }

    private void UpdateSentiments()
    {
        _sentiments = pythonEndpoint.currentSentiments;
        _isUpdated = true;
    }
    
    private void InitializeIntents()
    {
        allIntents = new List<Intent>
        {
            new Intent()
            {
                Id = IntentId.Greeting, GrammarName = "introduction", SentimentModifier = .01f, Label = "Greet"
            },
            new Intent()
            {
                Id = IntentId.ReplyGreeting, GrammarName = "replyIntroduction", SentimentModifier = .01f, Label = "Greet back"
            }
        };
        // TODO: Add all Intents

        reactions = new Dictionary<IntentId, List<IntentId>>
        {
            {IntentId.Greeting, new List<IntentId>() {IntentId.ReplyGreeting}}
        };
        // TODO: Add all reactions
    }
}
