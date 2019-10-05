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
    
    public event DialogueAction OnGenerateDialogue;
    public event DialogueAction OnChangeSpeaker;
    private Text _dialogueText;
    private Coroutine _typingCoroutine;
    private string _currentText;
    private bool _isTyping;
    private bool _isPlayerTurn;
    private DialogueParticipant _speaker;
    
    // Start is called before the first frame update
    void Start()
    {
        _dialogueText = GameObject.FindWithTag("Text").GetComponent<Text>();
        _isTyping = false;
        _isPlayerTurn = true;
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    
    public void PrepareInterface(bool isPlayerTurn, DialogueParticipant speaker)
    {
        _isPlayerTurn = isPlayerTurn;
        _speaker = speaker;
        npcDialogueArea.SetActive(!isPlayerTurn);
        playerDialogueArea.SetActive(isPlayerTurn);
        _dialogueText = GameObject.FindWithTag("Text").GetComponent<Text>();

        if (_isPlayerTurn)
        {
            Button skipButton = GameObject.Find("SkipButton").GetComponent<Button>();
            int numOfReplies = speaker.replyOptions.Count;

            if (numOfReplies > 0)
            {
                skipButton.interactable = false;
                ActivateReplyButtons(numOfReplies);
            }
        }
    }

    private void ActivateReplyButtons(int numOfReplies)
    {
        GameObject buttonArea = GameObject.Find("Replies");
        Button[] buttons = buttonArea.GetComponentsInChildren<Button>();
        for (int i = 0; i < buttons.Length; i++)
        {
            buttons[i].gameObject.SetActive(false);
            if (i <= numOfReplies - 1)
            {
                buttons[i].gameObject.SetActive(true);
                buttons[i].interactable = true;
                buttons[i].GetComponentInChildren<Text>().text = _speaker.replyOptions[i].Label;
            }
        }
    }

    public void OnReplyClick(int idx)
    {
        GameObject buttonArea = GameObject.Find("Replies");
        Button[] buttons = buttonArea.GetComponentsInChildren<Button>();
        foreach (Button button in buttons)
            button.interactable = false;
        
        _speaker.currentIntentBacklog.Push(_speaker.replyOptions[idx].Id);
        OnGenerateDialogue?.Invoke();
    }

    public void PresentText(bool isPlayerTurn, string currentText)
    {
        _currentText = currentText;
        GameObject.Find("SkipButton").GetComponent<Button>().interactable = true;
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
            OnChangeSpeaker?.Invoke();
    }

    public void MakeTextBigger()
    {
        _dialogueText.fontSize++;
    }
    
    public void MakeTextSmaller()
    {
        _dialogueText.fontSize--;
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

        _isTyping = false;
    }
}
