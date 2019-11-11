using System;
using System.Collections;
using System.Collections.Generic;
using Expressionist;
using UnityEngine;
using UnityEngine.SceneManagement;
using Random = UnityEngine.Random;

/// <summary>
/// An enum of all intent types
/// </summary>
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

/// <summary>
/// A struct for a reply consisting of an intent Id and an expectancy value
/// </summary>
public struct Reply
{ 
    public IntentId Id;
    public float ExpectancyValue;
}

/// <summary>
/// A struct for a Expressionist request for generating text
/// </summary>
public struct ExpressionistRequest
{
    /// <summary>
    /// A list of must have tags
    /// </summary>
    public readonly List<string> mustHaveTags;
    /// <summary>
    /// A list of prohibited tags
    /// </summary>
    public readonly List<string> mustNotHaveTags;
    /// <summary>
    /// A scoring metric as a list of "tag, value" tuples
    /// </summary>
    public readonly List<Tuple<string, int>> scoringMetric;
    /// <summary>
    /// The current state as a list of "variable, value" tuples
    /// </summary>
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

/// <summary>
/// Dialogue system to process a dialogue
/// </summary>
public class DialogueState : MonoBehaviour
{
    /// <summary>
    /// the PythonEndpoint to handle communction with the Python console
    /// </summary>
    public PythonEndpoint pythonEndpoint;
    /// <summary>
    /// Helper flag to determine the current player
    /// </summary>
    public bool isPlayerTurn;
    /// <summary>
    /// A dictionary of intents and their reactions
    /// </summary>
    public Dictionary<IntentId, List<Reply>> reactions;
    /// <summary>
    /// a list of all intents
    /// </summary>
    public static List<Intent> allIntents;
    /// <summary>
    /// the current shown text
    /// </summary>
    public string currentText;
    /// <summary>
    /// The player character
    /// </summary>
    public DialogueParticipant player;
    /// <summary>
    /// The NPC character
    /// </summary>
    public DialogueParticipant npc;
    /// <summary>
    /// The user interface
    /// </summary>
    public UserInterface userInterface;
    /// <summary>
    /// The current Expressionist state
    /// </summary>
    public List<Tuple<string, string>> dialogueState;
    /// <summary>
    /// The camera controller
    /// </summary>
    public CameraContoller cameraContoller;

    /// <summary>
    /// The newly generated text
    /// </summary>
    private string _generatedText;
    /// <summary>
    /// The current calculated sentiment
    /// </summary>
    private Sentiment _sentiment;
    /// <summary>
    /// Helper flag to determine newly updated text
    /// </summary>
    private bool _isUpdated;
    /// <summary>
    /// Helper flag to determine newly generated text
    /// </summary>
    private bool _isTextGenerated;
    /// <summary>
    /// The currently presented/ intent
    /// </summary>
    private Intent _currentIntent;
    /// <summary>
    /// Helper flag to determine if the current text is the last in a dialogue
    /// </summary>
    private bool _isLastText;

    /// <summary>
    /// Init the dialogue
    /// </summary>
    void Start()
    {
        // Get the single PythonEndpoint
        pythonEndpoint = FindObjectOfType<PythonEndpoint>();
        pythonEndpoint.OnTextGenerated += UpdateGeneratedText;
        pythonEndpoint.OnSentimentProcessed += UpdateSentiments;
        isPlayerTurn = true;
        currentText = "";
        InitializeIntents();

        // init the state variables needed by the Expressionist grammars
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
        // init helper variables
        _generatedText = "";
        _isUpdated = false;
        _isTextGenerated = false;
        _isLastText = false;
        userInterface.OnChangeSpeaker += ChangeSpeaker;
        userInterface.OnGenerateDialogue += GenerateDialogue;
    }
    
    void Update()
    {
        // if a new text is generated AND the sentiment is calculated
        if (_isUpdated)
        {
            // set the text and sentiment
            currentText = _generatedText;
            _currentIntent.SentimentModifier = _sentiment.compound;
            
            // set the speakers
            DialogueParticipant speaker = npc;
            DialogueParticipant otherSpeaker = player;
            if (isPlayerTurn)
            {
                speaker = player;
                otherSpeaker = npc;
            }
            
            // update the mood and the slider with the sentiment
            otherSpeaker.UpdateMood(_currentIntent);
            if (isPlayerTurn)
                userInterface.UpdateSlider(otherSpeaker.moodValue);
            
            // present the generated text
            userInterface.PresentText(isPlayerTurn, currentText);
            _isUpdated = false;
        }
    }

    /// <summary>
    /// Start a new dialogue
    /// </summary>
    public void StartDialogue()
    {
        // start the dialogue in the user interface
        userInterface.StartDialogueUI(player.gender, npc.gender);
        // initially perform one player change
        ChangeSpeaker();
    }

    /// <summary>
    /// Switches the current player (Player<>NPC)
    /// </summary>
    private void ChangeSpeaker()
    {
        // if the text is the last text, end the dialogue after
        if (_isLastText)
        {
            userInterface.EndDialogueUI();
            StartCoroutine(EndGame());
        }
        else
        {
            // switch the player
            if (_currentIntent == null || !_currentIntent.IsReaction)
            {
                isPlayerTurn = !isPlayerTurn;
                DialogueParticipant speaker = isPlayerTurn ? player : npc;
                // switch the camera to view the new player
                cameraContoller.SwitchPosition(isPlayerTurn);
                // prepare the UI for the new player
                userInterface.PrepareInterface(isPlayerTurn, speaker);

                // generate NPC dialogue
                if (!isPlayerTurn)
                    GenerateDialogue();
            }
            // generate player dialogue
            else
                GenerateDialogue();
        }
    }

    /// <summary>
    /// Initiates a new text generation
    /// </summary>
    private void GenerateDialogue()
    {
        // Determine the player
        DialogueParticipant speaker = isPlayerTurn ? player : npc;
        // get the next intent for the player
        _currentIntent = GetNextIntent(speaker);
        if (_currentIntent == null)
            return;
        
        // get the tags for the generation request
        List<string> mustHaveTags = GetMustHaveTagsforIntent(_currentIntent.Id);
        List<string> mustNotHaveTags = GetMustNotHaveTagsforIntent(_currentIntent.Id);
        // update the state for the generation request
        UpdateState(_currentIntent.Id);
        // create a new Expressionist request
        ExpressionistRequest request = new ExpressionistRequest(mustHaveTags, mustNotHaveTags,
            null, dialogueState);
        GetTextForIntent(_currentIntent, request);

        // remove the intent from the speaker backlog
        speaker.CheckGoals(_currentIntent);
        // add potential reactions to the backlog
        AddReactionIntents(_currentIntent.Id);

        // Determine if the intent is the last text
        if (_currentIntent.Id.Equals(IntentId.ReplyEnd))
            _isLastText = true;
    }

    /// <summary>
    /// Determine the required tags for given intents
    /// </summary>
    /// <param name="id">the intent Id</param>
    /// <returns>a list of required tags</returns>
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
            
            case IntentId.Buy:{return new List<string>(){"decision:yes"};}
            case IntentId.NotBuy:{return new List<string>(){"decision:no"};}

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

    /// <summary>
    /// Determine the prohibited tags for given intents
    /// </summary>
    /// <param name="id">the intent Id</param>
    /// <returns>a list of prohibited tags</returns>
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
            
            case IntentId.Buy:{return new List<string>(){"decision:no"};}
            case IntentId.NotBuy:{return new List<string>(){"decision:yes"};}
            
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

    /// <summary>
    /// Update the state variables for a given intent
    /// </summary>
    /// <param name="id">The intent Id</param>
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

    /// <summary>
    /// Add reaction intents to the player backlog or add replies to the player backlog
    /// </summary>
    /// <param name="reactTo">The intent Id to react to</param>
    private void AddReactionIntents(IntentId reactTo)
    {
        if (!reactions.ContainsKey(reactTo))
            return;
        
        // find the replies for a given intent
        List<Reply> replies = reactions[reactTo];
        if (replies != null)
        {
            // add replies to player backlog
            if (isPlayerTurn)
                replies.ForEach(i => npc.currentIntentBacklog.Push(i));
            else
            {
                // add reactions to NPC backlog
                player.replyOptions.Clear();
                replies.ForEach(i => player.replyOptions.Add(i));
            }
        }
    }

    /// <summary>
    /// Gets the next intent from the speaker backlog
    /// </summary>
    /// <param name="speaker"></param>
    /// <returns></returns>
    private Intent GetNextIntent(DialogueParticipant speaker)
    {
        if (speaker.currentIntentBacklog.Count == 0)
            return null;
        
        // Get the intent
        Reply r = speaker.currentIntentBacklog.Pop();
        Reply reply = new Reply() {Id = r.Id, ExpectancyValue = r.ExpectancyValue};
        
        // Determine the mood intent based on the current mood
        if (r.Id.Equals(IntentId.Mood))
            reply = GetMoodBasedOnIntent(speaker, reply);
        
        // Create new intent
        Intent intent = allIntents.Find(i => i.Id.Equals(reply.Id));
        intent.ExpectancyValue = r.ExpectancyValue;
        return intent;
    }

    /// <summary>
    /// Adjust the mood based on the speaker mood
    /// </summary>
    /// <param name="speaker">The current player</param>
    /// <param name="reply">The reply to adjust the mood for</param>
    /// <returns></returns>
    private Reply GetMoodBasedOnIntent(DialogueParticipant speaker, Reply reply)
    {
        float mood = speaker.moodValue;
        float expectancy = reply.ExpectancyValue;
        
        // Determine the mood
        if (mood >= .4f) // mood happy
            reply.Id = IntentId.Happy;
        else if (mood >= -.3f && mood < .4f) // mood neutral
            reply.Id = IntentId.Meh;
        else // mood unhappy
            reply.Id = IntentId.Unhappy;
        
        return reply;
    }

    /// <summary>
    /// Initiate new text generation for an intent
    /// </summary>
    /// <param name="intent">The intent to generate text for</param>
    /// <param name="request">The Expressionist request</param>
    private void GetTextForIntent(Intent intent, ExpressionistRequest request)
    {
        // Send request to Python
        pythonEndpoint.ExpressionistRequestCode(intent?.ToString(), request.mustHaveTags,request.mustNotHaveTags, request.scoringMetric, request.state);

        // wait for Python to complete text generation
        while (!_isTextGenerated)
            ;
		
        _isTextGenerated = false;
		
        // Start sentiment analysis for generated text
        pythonEndpoint.ExecuteSentimentAnalysis(_generatedText);
    }
    
    /// <summary>
    /// Update the current text with the newly generated text
    /// </summary>
    private void UpdateGeneratedText()
    {
        _generatedText = pythonEndpoint.currentGeneratedString;
        _isTextGenerated = true;
    }

    /// <summary>
    /// Update the current sentiment anlysis with the newly calculated values
    /// </summary>
    private void UpdateSentiments()
    {
        _sentiment = pythonEndpoint.currentSentiment;
        _isUpdated = true;
    }

    /// <summary>
    /// Zoom out of the scene and reload game
    /// </summary>
    /// <returns></returns>
    public IEnumerator EndGame()
    {
        cameraContoller.MoveOut();
        yield return new WaitForSeconds(2);
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    /// <summary>
    /// Update a state variable with a new value
    /// </summary>
    /// <param name="variable">the name of the variable</param>
    /// <param name="value">the new value of the variable</param>
    private void ChangeStateVariable(string variable, string value)
    {
        dialogueState.Remove(dialogueState.Find(t => t.Item1.Equals(variable)));
        dialogueState.Add(new Tuple<string, string>(variable, value));
    }

    /// <summary>
    /// Initialize all intents with an Id, a grammar file name and a label,
    /// all reactions as replies with a single reaction intent
    /// and all replies with a a list of reply intents
    /// </summary>
    private void InitializeIntents()
    {
        // intent initialization
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

        // reaction and reply initialization
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
