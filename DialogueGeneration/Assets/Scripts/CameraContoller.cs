using System.Collections;
using UnityEngine;

public class CameraContoller : MonoBehaviour
{
    /// <summary>
    /// NPC camera position
    /// </summary>
    public Transform npcPosition;
    /// <summary>
    /// Player camera position
    /// </summary>
    public Transform playerPosition;
    /// <summary>
    /// Scene overview position
    /// </summary>
    public Transform officePosition;
    
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    /// <summary>
    /// Turns the camera from one character to the other
    /// </summary>
    /// <param name="isPlayerTurn">indicates if it is the player's turn or the NPC's turn</param>
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

    /// <summary>
    /// Zooms out of the scene
    /// </summary>
    public void MoveOut()
    {
        Transform t = gameObject.transform;
        StartCoroutine(MoveCamera(t.position, officePosition.position,
            t.rotation, officePosition.rotation, 1.5f));
    }

    /// <summary>
    /// Coroutine for moving and rotating the camera the camera to face a certain point
    /// </summary>
    /// <param name="startPos">Start position</param>
    /// <param name="targetPos">Target position</param>
    /// <param name="startRotation">Start rotation</param>
    /// <param name="targetRotation">Target rotation</param>
    /// <param name="duration">Duration in seconds</param>
    /// <returns></returns>
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
