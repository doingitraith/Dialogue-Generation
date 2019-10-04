using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UserInterface : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void PresentText(bool isPlayerTurn, string currentText)
    {
        Debug.Log($"{(isPlayerTurn ? "Player" : "NPC")}: {currentText}");
    }
}
