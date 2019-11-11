using System.Collections;
using Expressionist;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Unity dialogue user interface
/// </summary>
public class UserInterface : MonoBehaviour
{
    /// <summary>
    /// Typing speed for the text typing effect
    /// </summary>
    public float typeDelay;
    /// <summary>
    /// The player dialogue area with reply buttons
    /// </summary>
    public GameObject playerDialogueArea;
    /// <summary>
    /// The NPC dialogue area
    /// </summary>
    public GameObject npcDialogueArea;
    /// <summary>
    /// A panel with information about the project and the survey
    /// </summary>
    public GameObject titleScreen;
    /// <summary>
    /// Array with the palyer reply buttons
    /// </summary>
    public GameObject[] buttons;
    /// <summary>
    /// Mood slider for the NPC sentiment
    /// </summary>
    public Slider slider;
    /// <summary>
    /// Player avatar image
    /// </summary>
    public Image playerImage;
    /// <summary>
    /// NPC avatar image
    /// </summary>
    public Image npcImage;
    /// <summary>
    /// Male Player image
    /// </summary>
    public Sprite playerMale;
    /// <summary>
    /// Female Player image
    /// </summary>
    public Sprite playerFemale;
    /// <summary>
    /// Male NPC image
    /// </summary>
    public Sprite npcMale;
    /// <summary>
    /// Female NPC image
    /// </summary>
    public Sprite npcFemale;
    
    /// <summary>
    /// Event for when a new dialogue line is generated
    /// </summary>
    public event DialogueAction OnGenerateDialogue;
    /// <summary>
    /// Event for when the active player changes (Player<>NPC)
    /// </summary>
    public event DialogueAction OnChangeSpeaker;
    /// <summary>
    /// The text element for the current dialogue line
    /// </summary>
    private Text _dialogueText;
    /// <summary>
    /// Button to skip the typing and to continue the dialogue
    /// </summary>
    private Button _skipButton;
    /// <summary>
    /// Coroutine for the typing effect when presenting the text
    /// </summary>
    private Coroutine _typingCoroutine;
    /// <summary>
    /// Coroutine for moving the mood indicator on the slider
    /// </summary>
    private Coroutine _slidingCoroutine;
    /// <summary>
    /// The current dialogue line
    /// </summary>
    private string _currentText;
    /// <summary>
    /// Helper flag for the typing effect
    /// </summary>
    private bool _isTyping;
    /// <summary>
    /// Helper flag for the sliding effect
    /// </summary>
    private bool _isSliding;
    /// <summary>
    /// Helper flag for indicating the current player
    /// </summary>
    private bool _isPlayerTurn;
    /// <summary>
    /// Current player
    /// </summary>
    private DialogueParticipant _speaker;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    /// <summary>
    /// Sets up and starts the dialogue for a new game
    /// </summary>
    /// <param name="playerGender">The gender of the player</param>
    /// <param name="npcGender">The gender of the NPC</param>
    public void StartDialogueUI(Gender playerGender, Gender npcGender)
    {
        titleScreen.SetActive(false);
        npcDialogueArea.SetActive(true);
        npcImage.sprite = npcGender.Equals(Gender.Male) ? npcMale : npcFemale;
        playerImage.sprite = playerGender.Equals(Gender.Male) ? playerMale : playerFemale;
        slider.gameObject.SetActive(true);
        _dialogueText = GameObject.FindWithTag("Text").GetComponent<Text>();
        _isTyping = false;
        _isSliding = false;
        _isPlayerTurn = true;   
    }

    /// <summary>
    /// Disables the dialogue areas
    /// </summary>
    public void EndDialogueUI()
    {
        npcDialogueArea.SetActive(false);
        playerDialogueArea.SetActive(false);
    }
    
    /// <summary>
    /// Sets up the correct interface after a player change
    /// </summary>
    /// <param name="isPlayerTurn">Indicates whether it is the player or NPC turn</param>
    /// <param name="speaker">The current player</param>
    public void PrepareInterface(bool isPlayerTurn, DialogueParticipant speaker)
    {
        _isPlayerTurn = isPlayerTurn;
        _speaker = speaker;
        npcDialogueArea.SetActive(!isPlayerTurn);
        playerDialogueArea.SetActive(isPlayerTurn);
        _dialogueText = GameObject.FindWithTag("Text").GetComponent<Text>();
        _skipButton = GameObject.FindWithTag("Skip").GetComponent<Button>();
        //_skipButton.interactable = false;

        if (_isPlayerTurn)
        {
            _dialogueText.text = _currentText;
            int numOfReplies = speaker.replyOptions.Count;

            if (numOfReplies > 0)
            {
                _skipButton.interactable = false;
                ActivateReplyButtons(numOfReplies);
            }
        }
    }

    /// <summary>
    /// Sets up the reply buttons for a given statement
    /// </summary>
    /// <param name="numOfReplies">Indicates how many replies are available</param>
    private void ActivateReplyButtons(int numOfReplies)
    {
        //GameObject buttonArea = GameObject.Find("Replies");
        //Button[] buttons = buttonArea.GetComponentsInChildren<Button>();
        for (int i = 0; i < buttons.Length; i++)
        {
            buttons[i].gameObject.SetActive(false);
            if (i <= numOfReplies - 1)
            {
                buttons[i].gameObject.SetActive(true);
                buttons[i].GetComponentInChildren<Button>().interactable = true;
                buttons[i].GetComponentInChildren<Text>().text = 
                    DialogueState.allIntents.Find(j => j.Id.Equals(_speaker.replyOptions[i].Id)).Label;
            }
        }
    }

    /// <summary>
    /// Update the value of the mood slider
    /// </summary>
    /// <param name="value">The new value</param>
    public void UpdateSlider(float value)
    {
        if (_isSliding)
            StopCoroutine(_slidingCoroutine);
        else
            _slidingCoroutine = StartCoroutine(MoveSlider(slider.value, value, .5f));
    }

    /// <summary>
    /// Handles the click on one of the reply buttons
    /// </summary>
    /// <param name="idx">The index of the clicked reply</param>
    public void OnReplyClick(int idx)
    {
        GameObject buttonArea = GameObject.Find("Replies");
        Button[] buttons = buttonArea.GetComponentsInChildren<Button>();
        foreach (Button button in buttons)
            button.interactable = false;
        
        _speaker.currentIntentBacklog.Push(_speaker.replyOptions[idx]);
        OnGenerateDialogue?.Invoke();
    }

    /// <summary>
    /// Starts the typing effect for presenting a dialogue line
    /// </summary>
    /// <param name="isPlayerTurn">Indicates if it is the turn of the player or the NPC</param>
    /// <param name="currentText">The text to present</param>
    public void PresentText(bool isPlayerTurn, string currentText)
    {
        _currentText = currentText;
        _skipButton.interactable = true;
        _typingCoroutine = StartCoroutine(TypeText(_currentText));
    }

    /// <summary>
    /// Handles the click on the skip button. Either the typing effect is skipped or the dialogue is progressed
    /// </summary>
    public void SkipText()
    {
        
        if (_isTyping)
        {
            StopCoroutine(_typingCoroutine);
            _dialogueText.text = _currentText;
            _isTyping = false;
        }
        else
        {
            OnChangeSpeaker?.Invoke();
        }
        
        //OnChangeSpeaker?.Invoke();
    }

    /// <summary>
    /// Makes the font of the dialogue text larger
    /// </summary>
    public void MakeTextBigger()
    {
        _dialogueText.fontSize++;
    }

    /// <summary>
    /// Makes the font of the dialogue text smaller
    /// </summary>
    public void MakeTextSmaller()
    {
        _dialogueText.fontSize--;
    }

    /// <summary>
    /// Exit the application
    /// </summary>
    public void OnExit()
    {
        Application.Quit();
    }

    /// <summary>
    /// Coroutine for typing the text letter by letter
    /// </summary>
    /// <param name="fullText"></param>
    /// <returns></returns>
    IEnumerator TypeText(string fullText)
    {
        _isTyping = true;
        for (int i = 0; i <= fullText.Length; i++)
        {
            string currentText = fullText.Substring(0, i);
            _dialogueText.text = currentText;
            yield return new WaitForSeconds(typeDelay);
        }

        _skipButton.interactable = true;
        _isTyping = false;
    }
    
    /// <summary>
    /// Couroutine for moving the mood indicator on the slider
    /// </summary>
    /// <param name="startValue"></param>
    /// <param name="targetValue"></param>
    /// <param name="duration"></param>
    /// <returns></returns>
    public IEnumerator MoveSlider(float startValue, float targetValue, float duration)
    {
        _isSliding = true;
        for (float t = .0f; t < duration; t += Time.deltaTime)
        {
            slider.value = Mathf.Lerp(startValue, targetValue, t / duration);
            yield return null;
        }
        slider.value = targetValue;
        _isSliding = false;
    }

    /// <summary>
    /// Open the survey form in a browser
    /// </summary>
    /// <param name="url"></param>
    public void OpenURL(string url)
    {
        Application.OpenURL(url);
    }
}
