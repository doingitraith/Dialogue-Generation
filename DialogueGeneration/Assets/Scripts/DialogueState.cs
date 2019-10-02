using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;

public enum IntentID
{
    GREETING,
    REPLY_GREETING
}

public struct Intent
{
    public IntentID Id;
    public float SentimentModifier;
    public string Label;
    public string GrammarName;

    public bool Equals(Intent other)
    {
        return this.Id.Equals(other.Id);
    }

    public string ToString()
    {
        return GrammarName;
    }
}

public struct ExpressionistRequest
{
    public List<string> mustHaveTags;
    public List<string> mustNotHaveTags;
    public List<Tuple<string, int>> scoringMetric;
    public List<Tuple<string, string>> state;
}

public class DialogueState : MonoBehaviour
{
    public PythonEndpoint pythonEndpoint;
    public bool isPlayerTurn;
    public Dictionary<IntentID, List<IntentID>> reactions;
    public List<Intent> allIntents;
    public string currentText;
    public DialogueParticipant player;
    public DialogueParticipant npc;

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
        
    }

    void Update()
    {
        if (_isUpdated)
        {
            currentText = _generatedText;
            _isUpdated = false;
        }
    }

    public void AddReactionIntents(IntentID reactTo)
    {
        List<IntentID> replies = reactions[reactTo];
        if (reactions != null)
        {
            if(isPlayerTurn)
                replies.ForEach(i => player.CurrentIntentBacklog.Push(i));
            else
                replies.ForEach(i => npc.CurrentIntentBacklog.Push(i));
        }
    }

    public Intent getNextIntent()
    {
        IntentID id = isPlayerTurn ? player.CurrentIntentBacklog.Pop() : npc.CurrentIntentBacklog.Pop();
        return allIntents.Find(i => i.Id.Equals(id));
    }

    public void getTextForIntent(Intent intent, ExpressionistRequest request)
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
        allIntents = new List<Intent>();
        // TODO: Add all Intents
        allIntents.Add(new Intent()
        {
            Id = IntentID.GREETING,
            GrammarName = "greeting",
            SentimentModifier = .01f,
            Label = "Greet"
        });
        
        reactions = new Dictionary<IntentID, List<IntentID>>();
        // TODO: Add all reactions
        reactions.Add(IntentID.GREETING, new List<IntentID>(){IntentID.REPLY_GREETING});
    }
}
