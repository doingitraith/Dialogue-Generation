using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraContoller : MonoBehaviour
{
    public Camera camera;
    public Transform npcPosition;
    public Transform playerPosition;

    private bool isPlayer = true;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void SwitchPosition()
    {
        Transform t = camera.transform;
        
        if (isPlayer)
            StartCoroutine(MoveCamera(t.position, npcPosition.position,
                t.rotation, npcPosition.rotation, 1.0f));
        else
            StartCoroutine(MoveCamera(t.position, playerPosition.position,
                t.rotation, playerPosition.rotation, 1.0f));
        

        isPlayer = !isPlayer;
    }

    public IEnumerator MoveCamera(Vector3 startPos, Vector3 targetPos,
        Quaternion startRotation, Quaternion targetRotation, float duration)
    {
        for (float t = .0f; t < duration; t += Time.deltaTime)
        {
            camera.transform.position = Vector3.Lerp(startPos, targetPos, t / duration);
            camera.transform.rotation = Quaternion.Lerp(startRotation, targetRotation, t / duration);
            yield return null;
        }
        camera.transform.position = targetPos;
    }
}
