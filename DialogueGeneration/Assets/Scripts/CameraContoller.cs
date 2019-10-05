using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraContoller : MonoBehaviour
{
    public Transform npcPosition;
    public Transform playerPosition;
    
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void SwitchPosition(bool isPlayerTurn)
    {
        Transform t = gameObject.transform;
        
        if (isPlayerTurn)
            StartCoroutine(MoveCamera(t.position, playerPosition.position,
                t.rotation, playerPosition.rotation, 1.0f));
        else
        StartCoroutine(MoveCamera(t.position, npcPosition.position,
            t.rotation, npcPosition.rotation, 1.0f));
    }

    public IEnumerator MoveCamera(Vector3 startPos, Vector3 targetPos,
        Quaternion startRotation, Quaternion targetRotation, float duration)
    {
        for (float t = .0f; t < duration; t += Time.deltaTime)
        {
            gameObject.transform.position = Vector3.Lerp(startPos, targetPos, t / duration);
            gameObject.transform.rotation = Quaternion.Lerp(startRotation, targetRotation, t / duration);
            yield return null;
        }
        gameObject.transform.position = targetPos;
    }
}
