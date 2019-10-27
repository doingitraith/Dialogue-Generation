using System.Collections;
using System.Collections.Generic;
using Expressionist;
using UnityEngine;
using UnityEngine.UI;

public class UserInterface : MonoBehaviour
{
    public float typeDelay;
    public GameObject playerDialogueArea;
    public GameObject npcDialogueArea;
    public GameObject titleScreen;
    public GameObject[] buttons;
    public Slider slider;
    public Image playerImage;
    public Image npcImage;
    public Sprite playerMale;
    public Sprite playerFemale;
    public Sprite npcMale;
    public Sprite npcFemale;
    
    public event DialogueAction OnGenerateDialogue;
    public event DialogueAction OnChangeSpeaker;
    private Text _dialogueText;
    private Button _skipButton;
    private Coroutine _typingCoroutine;
    private Coroutine _slidingCoroutine;
    private string _currentText;
    private bool _isTyping;
    private bool _isSliding;
    private bool _isPlayerTurn;
    private DialogueParticipant _speaker;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void StartDialoueUI(Gender playerGender, Gender npcGender)
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

    public void EndDialogueUI()
    {
        npcDialogueArea.SetActive(false);
        playerDialogueArea.SetActive(false);
    }
    
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

    public void UpdateSlider(float value)
    {
        if (_isSliding)
            StopCoroutine(_slidingCoroutine);
        else
            _slidingCoroutine = StartCoroutine(MoveSlider(slider.value, value, .5f));
    }

    public void OnReplyClick(int idx)
    {
        GameObject buttonArea = GameObject.Find("Replies");
        Button[] buttons = buttonArea.GetComponentsInChildren<Button>();
        foreach (Button button in buttons)
            button.interactable = false;
        
        _speaker.currentIntentBacklog.Push(_speaker.replyOptions[idx]);
        OnGenerateDialogue?.Invoke();
    }

    public void PresentText(bool isPlayerTurn, string currentText)
    {
        _currentText = currentText;
        _skipButton.interactable = true;
        _typingCoroutine = StartCoroutine(TypeText(_currentText));
    }

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

    public void MakeTextBigger()
    {
        _dialogueText.fontSize++;
    }
    
    public void MakeTextSmaller()
    {
        _dialogueText.fontSize--;
    }

    public void OnExit()
    {
        Application.Quit();
    }

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

    public void OpenURL(string url)
    {
        Application.OpenURL(url);
    }
}
