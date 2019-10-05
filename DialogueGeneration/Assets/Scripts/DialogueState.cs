using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Expressionist;
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
    public List<Tuple<string, string>> dialogueState;
    public CameraContoller cameraContoller;

    private string _generatedText;
    private List<Sentiment> _sentiments;
    private string _selectedGrammar;
    private bool _isUpdated;
    private bool _isTextGenerated;


    void Start()
    {
        pythonEndpoint.OnTextGenerated += UpdateGeneratedText;
        pythonEndpoint.OnSentimentProcessed += UpdateSentiments;
        isPlayerTurn = true;
        currentText = "";
        InitializeIntents();

        dialogueState = new List<Tuple<string, string>>() {new Tuple<string, string>("dayTime", "day")};
        _generatedText = "";
        _sentiments = new List<Sentiment>();
        _selectedGrammar = "introduction";
        _isUpdated = false;
        _isTextGenerated = false;
        userInterface.OnChangeSpeaker += ChangeSpeaker;
        userInterface.OnGenerateDialogue += GenerateDialogue;
        ChangeSpeaker();
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

    private void ChangeSpeaker()
    {
        isPlayerTurn = !isPlayerTurn;
        DialogueParticipant speaker = isPlayerTurn ? player : npc;
        cameraContoller.SwitchPosition(isPlayerTurn);
        userInterface.PrepareInterface(isPlayerTurn, speaker);
        
        if(!isPlayerTurn)
            GenerateDialogue();
    }

    private void GenerateDialogue()
    {
        DialogueParticipant speaker = isPlayerTurn ? player : npc;
        Intent? intent = GetNextIntent(speaker);
        if (intent == null)
            return;
        
        ExpressionistRequest request = new ExpressionistRequest(null, null,
            null, dialogueState);
        GetTextForIntent(intent, request);
        speaker.UpdateMood(intent);
        AddReactionIntents(intent.Value.Id);
    }

    private void AddReactionIntents(IntentId reactTo)
    {
        if (!reactions.ContainsKey(reactTo))
            return;
        
        List<IntentId> replies = reactions[reactTo];
        if (replies != null)
        {
            if (isPlayerTurn)
                replies.ForEach(i => npc.currentIntentBacklog.Push(i));
            else
                replies.ForEach(i => player.replyOptions.Add(allIntents.Find(j => j.Id.Equals(i))));    
        }
    }

    private Intent? GetNextIntent(DialogueParticipant speaker)
    {
        if (speaker.currentIntentBacklog.Count == 0)
            return null;
        
        IntentId id = speaker.currentIntentBacklog.Pop();
        return allIntents.Find(i => i.Id.Equals(id));
    }

    private void GetTextForIntent(Intent? intent, ExpressionistRequest request)
    {
        pythonEndpoint.ExpressionistRequestCode(intent?.ToString(), request.mustHaveTags,request.mustNotHaveTags, request.scoringMetric, request.state);
        
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
