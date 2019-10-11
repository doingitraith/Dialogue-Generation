using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Expressionist;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Random = UnityEngine.Random;

public enum IntentId
{
    Agree,
    Buy,
    CompareInfo,
    DetailInfo,
    Disagree,
    DurationInfo,
    End,
    Explain,
    ExplainMistake,
    FeatureInfo,
    Happy,
    Introduction,
    Little,
    Meh,
    Mood,
    Much,
    No,
    NotBuy,
    OutOfStock,
    Outrage,
    PriceInfo,
    PriceMuch,
    PriceLittle,
    Quality,
    ReplyEnd,
    ReplyIntroduction,
    Sorry,
    Unhappy,
    UsageInfo,
    UsageHow,
    Water,
    Yes
}

public struct Reply
{ 
    public IntentId Id;
    public float ExpectancyValue;
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
    public Dictionary<IntentId, List<Reply>> reactions;
    public static List<Intent> allIntents;
    public string currentText;
    public DialogueParticipant player;
    public DialogueParticipant npc;
    public UserInterface userInterface;
    public List<Tuple<string, string>> dialogueState;
    public CameraContoller cameraContoller;

    private string _generatedText;
    private Sentiment _sentiment;
    //private string _selectedGrammar;
    private bool _isUpdated;
    private bool _isTextGenerated;
    private Intent _currentIntent;
    private bool _isLastText;


    void Start()
    {
        pythonEndpoint = FindObjectOfType<PythonEndpoint>();
        pythonEndpoint.OnTextGenerated += UpdateGeneratedText;
        pythonEndpoint.OnSentimentProcessed += UpdateSentiments;
        isPlayerTurn = true;
        currentText = "";
        InitializeIntents();

        int randIdx = Random.Range(0, 4);
        dialogueState = new List<Tuple<string, string>>()
        {
            new Tuple<string, string>("dayTime", "day"),
            new Tuple<string, string>("feature", Intent.GetRandomFeature(randIdx)),
            new Tuple<string, string>("features", Intent.GetRandomFeatures(randIdx)),
            new Tuple<string, string>("product1", "phone"),
            new Tuple<string, string>("products1", "phones"),
            new Tuple<string, string>("detail1", Intent.GetRandomDetail(randIdx)),
            new Tuple<string, string>("details1", Intent.GetRandomDetails(randIdx)),
            new Tuple<string, string>("duration", "days"),
            new Tuple<string, string>("time", "day"),
            new Tuple<string, string>("amount", Intent.GetRandomDeliveryTime())
        };
        _generatedText = "";
        //_selectedGrammar = "introduction";
        _isUpdated = false;
        _isTextGenerated = false;
        _isLastText = false;
        userInterface.OnChangeSpeaker += ChangeSpeaker;
        userInterface.OnGenerateDialogue += GenerateDialogue;
    }
    
    void Update()
    {
        if (_isUpdated)
        {
            currentText = _generatedText;
            _currentIntent.SentimentModifier = _sentiment.compound;
            
            DialogueParticipant speaker = npc;
            DialogueParticipant otherSpeaker = player;
            if (isPlayerTurn)
            {
                speaker = player;
                otherSpeaker = npc;
            }
            
            otherSpeaker.UpdateMood(_currentIntent);
            if (isPlayerTurn)
                userInterface.UpdateSlider(otherSpeaker.moodValue);
            
            userInterface.PresentText(isPlayerTurn, currentText);
            _isUpdated = false;
        }
    }

    public void StartDialogue()
    {
        userInterface.StartDialoueUI(player.gender, npc.gender);
        ChangeSpeaker();
    }

    private void ChangeSpeaker()
    {
        if (_isLastText)
        {
            userInterface.EndDialogueUI();
            StartCoroutine(EndGame());
        }
        else
        {
            if (_currentIntent == null || !_currentIntent.IsReaction)
            {
                isPlayerTurn = !isPlayerTurn;
                DialogueParticipant speaker = isPlayerTurn ? player : npc;
                cameraContoller.SwitchPosition(isPlayerTurn);
                userInterface.PrepareInterface(isPlayerTurn, speaker);

                if (!isPlayerTurn)
                    GenerateDialogue();
            }
            else
                GenerateDialogue();
        }
    }

    private void GenerateDialogue()
    {
        DialogueParticipant speaker = isPlayerTurn ? player : npc;
        _currentIntent = GetNextIntent(speaker);
        if (_currentIntent == null)
            return;
        
        List<string> mustHaveTags = GetMustHaveTagsforIntent(_currentIntent.Id);
        List<string> mustNotHaveTags = GetMustNotHaveTagsforIntent(_currentIntent.Id);
        UpdateState(_currentIntent.Id);
        ExpressionistRequest request = new ExpressionistRequest(mustHaveTags, mustNotHaveTags,
            null, dialogueState);
        GetTextForIntent(_currentIntent, request);

        speaker.CheckGoals(_currentIntent);
        AddReactionIntents(_currentIntent.Id);

        if (_currentIntent.Id.Equals(IntentId.ReplyEnd))
            _isLastText = true;
    }

    private List<string> GetMustHaveTagsforIntent(IntentId id)
    {
        switch (id)
        {
            case IntentId.Happy:{return new List<string>() {"mood:happy"};}
            case IntentId.Unhappy:{return new List<string>() {"mood:unhappy"};}
            case IntentId.Meh:{return new List<string>() {"mood:meh"};}
            
            case IntentId.Much:
            case IntentId.PriceMuch:{return new List<string>(){"amount:much"};}
            
            case IntentId.Little:
            case IntentId.PriceLittle:{return new List<string>(){"amount:little"};}

            case IntentId.Introduction:
            {
                if(player.gender.Equals(Gender.Male))
                    return new List<string>(){"gender:male"};
                
                return new List<string>(){"gender:female"};
            }
            case IntentId.ReplyIntroduction:
            {
                if(npc.gender.Equals(Gender.Male))
                    return new List<string>(){"gender:male"};
                
                return new List<string>(){"gender:female"};
            }
            default:{return null;}
        }
    }

    private List<string> GetMustNotHaveTagsforIntent(IntentId id)
    {
        switch (id)
        {
            case IntentId.Happy:{return new List<string>() {"mood:unhappy","mood:meh"};}
            case IntentId.Unhappy:{return new List<string>() {"mood:happy","mood:meh"};}
            case IntentId.Meh:{return new List<string>() {"mood:happy","mood:unhappy"};}
            
            case IntentId.Much:
            case IntentId.PriceMuch:{return new List<string>(){"amount:little"};}
            
            
            case IntentId.Little:
            case IntentId.PriceLittle:{return new List<string>(){"amount:much"};}
            
            case IntentId.Introduction:
            {
                if(player.gender.Equals(Gender.Male))
                    return new List<string>(){"gender:female"};
                
                return new List<string>(){"gender:male"};
            }
            case IntentId.ReplyIntroduction:
            {
                if(npc.gender.Equals(Gender.Male))
                    return new List<string>(){"gender:female"};
                
                return new List<string>(){"gender:male"};
            }
            default:{return null;}
        }
    }

    private void UpdateState(IntentId id)
    {
        switch (id)
        {
            case IntentId.PriceMuch:
            case IntentId.PriceLittle:
            case IntentId.PriceInfo:
            {
                ChangeStateVariable("amount", Intent.GetRandomPrice());
            }break;
            case IntentId.Explain:
            {
                ChangeStateVariable("amount", Intent.GetRandomNumber());
            }break;
            case IntentId.Little:
            case IntentId.Much:
            {
                ChangeStateVariable("amount", Intent.GetRandomDeliveryTime());
            }break;
            default:
            break;
        }
    }

    private void AddReactionIntents(IntentId reactTo)
    {
        if (!reactions.ContainsKey(reactTo))
            return;
        
        List<Reply> replies = reactions[reactTo];
        if (replies != null)
        {
            if (isPlayerTurn)
                replies.ForEach(i => npc.currentIntentBacklog.Push(i));
            else
            {
                player.replyOptions.Clear();
                replies.ForEach(i => player.replyOptions.Add(i));
            }
        }
    }

    private Intent GetNextIntent(DialogueParticipant speaker)
    {
        if (speaker.currentIntentBacklog.Count == 0)
            return null;
        
        Reply r = speaker.currentIntentBacklog.Pop();
        Reply reply = new Reply() {Id = r.Id, ExpectancyValue = r.ExpectancyValue};
        
        if (r.Id.Equals(IntentId.Mood))
            reply = GetMoodBasedOnIntent(speaker, reply);
        
        Intent intent = allIntents.Find(i => i.Id.Equals(reply.Id));
        intent.ExpectancyValue = r.ExpectancyValue;
        return intent;
    }

    private Reply GetMoodBasedOnIntent(DialogueParticipant speaker, Reply reply)
    {
        float mood = speaker.moodValue;
        float expectancy = reply.ExpectancyValue;
        
        if (mood >= .4f) // mood happy
            reply.Id = IntentId.Happy;
        else if (mood >= -.3f && mood < .4f) // mood neutral
            reply.Id = IntentId.Meh;
        else // mood unhappy
            reply.Id = IntentId.Unhappy;
        
        return reply;
    }

    private void GetTextForIntent(Intent intent, ExpressionistRequest request)
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
        _sentiment = pythonEndpoint.currentSentiment;
        _isUpdated = true;
    }

    public IEnumerator EndGame()
    {
        cameraContoller.MoveOut();
        yield return new WaitForSeconds(2);
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    private void ChangeStateVariable(string variable, string value)
    {
        dialogueState.Remove(dialogueState.Find(t => t.Item1.Equals(variable)));
        dialogueState.Add(new Tuple<string, string>(variable, value));
    }

    private void InitializeIntents()
    {
        allIntents = new List<Intent>
        {
            new Intent()
            {
                Id=IntentId.Agree, GrammarName = "yes", Label = "Agree"
            },
            new Intent()
            {
                Id=IntentId.Buy, GrammarName = "buyInfo", Label = "Confirm purchase"
            },
            /*
            new Intent()
            {
                Id=IntentId.Disagree, GrammarName = "agree", Label = "Disagree"
            },
            */
            new Intent()
            {
                Id=IntentId.End, GrammarName = "end", Label = "Say goodbye"
            },
            new Intent()
            {
                Id=IntentId.Explain, GrammarName = "explain", Label = "Explain feature"
            },
            new Intent()
            {
                Id=IntentId.ExplainMistake, GrammarName = "mistake", Label = "Admit mistake"
            },
            new Intent()
            {
                Id=IntentId.FeatureInfo, GrammarName = "requestInfo", Label = "Ask for specific features"
            },
            new Intent()
            {
                Id=IntentId.Happy, GrammarName = "mood", Label = "React happy", IsReaction = true
            },
            new Intent()
            {
                Id = IntentId.ReplyIntroduction, GrammarName = "introductionPlayer", SentimentModifier = .01f, Label = "Greet back"
            },
            new Intent()
            {
                Id=IntentId.Little, GrammarName = "amount", Label = "Short delivery time"
            },
            new Intent()
            {
                Id=IntentId.Meh, GrammarName = "mood", Label = "React indifferent", IsReaction = true
            },
            new Intent()
            {
                Id=IntentId.Mood, GrammarName = "", Label = "choose reply based on mood"
            },
            new Intent()
            {
                Id=IntentId.Much, GrammarName = "amount", Label = "Long delivery time"
            },
            new Intent()
            {
                Id=IntentId.No, GrammarName = "no", Label = "Deny"
            },
            new Intent()
            {
                Id=IntentId.Outrage, GrammarName = "outrage", Label = "React angry"
            },
            new Intent()
            {
                Id=IntentId.Sorry, GrammarName = "sorry", Label = "Apologize"
            },
            new Intent()
            {
                Id=IntentId.Unhappy, GrammarName = "mood", Label = "react unhappy", IsReaction = true
            },
            new Intent()
            {
                Id=IntentId.Water, GrammarName = "water", Label = "Offer drink"
            },
            new Intent()
            {
                Id=IntentId.Yes, GrammarName = "agree", Label = "Affirm"
            },
            new Intent()
            {
                Id=IntentId.CompareInfo, GrammarName = "compareInfo", Label = "Compare to other products"
            },
            new Intent()
            {
                Id=IntentId.DetailInfo, GrammarName = "detailInfo", Label = "Ask for number of features"
            },
            new Intent()
            {
                Id=IntentId.DurationInfo, GrammarName = "durationInfo", Label = "Ask for delivery time"
            },
            new Intent()
            {
                Id=IntentId.NotBuy, GrammarName = "buyInfo", Label = "Decline purchase"
            },
            new Intent()
            {
                Id=IntentId.ReplyEnd, GrammarName = "replyEnd", Label = "Say goodbye", IsReaction = true
            },
            new Intent()
            {
                Id = IntentId.Introduction, GrammarName = "introductionNPC", SentimentModifier = .01f, Label = "Greet"//, IsReaction = true
            },
            new Intent()
            {
                Id=IntentId.UsageInfo, GrammarName = "usageInfo", Label = "Ask for usage details"
            },
            new Intent()
            {
                Id=IntentId.OutOfStock, GrammarName = "outOfStock", Label = "Product is out of stock"
            },
            new Intent()
            {
                Id=IntentId.Quality, GrammarName = "quality", Label = "Speak about quality"
            },
            new Intent()
            {
                Id=IntentId.PriceInfo, GrammarName = "priceInfo", Label = "Ask for price"
            },
            new Intent()
            {
                Id=IntentId.PriceMuch, GrammarName = "amountPrice", Label = "Mention high price"
            },
            new Intent()
            {
                Id=IntentId.PriceLittle, GrammarName = "amountPrice", Label = "Mention low price"
            },
            new Intent()
            {
                Id=IntentId.UsageHow, GrammarName = "howUse", Label = "Elaborate on usage"
            }
        };

        reactions = new Dictionary<IntentId, List<Reply>>
        {
            // Reactions
            {IntentId.Introduction, new List<Reply>() {new Reply(){Id = IntentId.ReplyIntroduction, ExpectancyValue = .0f}}},
            {IntentId.End, new List<Reply>() {new Reply(){Id = IntentId.ReplyEnd, ExpectancyValue = .0f}}},
            
            {IntentId.Agree, new List<Reply>() {new Reply(){Id = IntentId.Mood, ExpectancyValue = .0f}}},
            {IntentId.Explain, new List<Reply>() {new Reply(){Id = IntentId.Mood, ExpectancyValue = .0f}}},
            {IntentId.Little, new List<Reply>() {new Reply(){Id = IntentId.Mood, ExpectancyValue = .0f}}},
            {IntentId.Much, new List<Reply>() {new Reply(){Id = IntentId.Mood, ExpectancyValue = .0f}}},
            {IntentId.No, new List<Reply>() {new Reply(){Id = IntentId.Mood, ExpectancyValue = .0f}}},
            {IntentId.Sorry, new List<Reply>() {new Reply(){Id = IntentId.Mood, ExpectancyValue = .0f}}},
            {IntentId.Water, new List<Reply>() {new Reply(){Id = IntentId.Mood, ExpectancyValue = .0f}}},
            {IntentId.Yes, new List<Reply>() {new Reply(){Id = IntentId.Mood, ExpectancyValue = .0f}}},
            {IntentId.OutOfStock, new List<Reply>() {new Reply(){Id = IntentId.Mood, ExpectancyValue = .0f}}},
            {IntentId.PriceMuch, new List<Reply>() {new Reply(){Id = IntentId.Mood, ExpectancyValue = .0f}}},
            {IntentId.PriceLittle, new List<Reply>() {new Reply(){Id = IntentId.Mood, ExpectancyValue = .0f}}},
            
            // Replies
            {IntentId.Buy, new List<Reply>()
            {
                new Reply(){Id = IntentId.End, ExpectancyValue = 1.0f}
            }},
            {IntentId.NotBuy, new List<Reply>()
            {
                new Reply(){Id=IntentId.Sorry, ExpectancyValue = .5f},
                new Reply(){Id = IntentId.End, ExpectancyValue = .5f}
            }},
            {IntentId.Outrage, new List<Reply>()
            {
                new Reply(){Id = IntentId.Sorry, ExpectancyValue = .4f},
                new Reply(){Id = IntentId.Water, ExpectancyValue = .4f},
                new Reply(){Id=IntentId.Quality, ExpectancyValue = .3f},
                new Reply(){Id=IntentId.ExplainMistake, ExpectancyValue = .5f}
            }},
            {IntentId.FeatureInfo, new List<Reply>()
            {
                new Reply(){Id = IntentId.Explain, ExpectancyValue = .3f},
                new Reply(){Id = IntentId.Quality, ExpectancyValue = .7f},
                new Reply(){Id=IntentId.OutOfStock, ExpectancyValue = -.8f}
            }},
            {IntentId.DetailInfo, new List<Reply>()
            {
                new Reply(){Id = IntentId.Yes, ExpectancyValue = .7f},
                new Reply(){Id = IntentId.No, ExpectancyValue = -.5f},
                new Reply(){Id=IntentId.Explain, ExpectancyValue = .3f},
                new Reply(){Id=IntentId.OutOfStock, ExpectancyValue = -.5f}
            }},
            {IntentId.CompareInfo, new List<Reply>()
            {
                new Reply(){Id = IntentId.Yes, ExpectancyValue = .5f},
                new Reply(){Id = IntentId.No, ExpectancyValue = -1.0f},
                new Reply(){Id=IntentId.Quality, ExpectancyValue = -.5f}
                //new Reply(){Id=IntentId.Explain, ExpectancyValue = -.2f}
            }},
            {IntentId.UsageInfo, new List<Reply>()
            {
                new Reply(){Id=IntentId.UsageHow, ExpectancyValue = .6f},
                new Reply(){Id = IntentId.Yes, ExpectancyValue = .4f},
                new Reply(){Id = IntentId.No, ExpectancyValue = -.8f},
                new Reply(){Id=IntentId.Agree, ExpectancyValue = .4f}
            }},
            {IntentId.DurationInfo, new List<Reply>()
            {
                new Reply(){Id = IntentId.Much, ExpectancyValue = -.5f},
                new Reply(){Id = IntentId.Little, ExpectancyValue = .8f},
                new Reply(){Id=IntentId.OutOfStock, ExpectancyValue = -.8f}
            }},
            {IntentId.PriceInfo, new List<Reply>()
            {
                new Reply(){Id = IntentId.PriceMuch, ExpectancyValue = -.5f},
                new Reply(){Id = IntentId.PriceLittle, ExpectancyValue = .8f},
                new Reply(){Id=IntentId.OutOfStock, ExpectancyValue = -.8f}
            }},
        };
    }
}
