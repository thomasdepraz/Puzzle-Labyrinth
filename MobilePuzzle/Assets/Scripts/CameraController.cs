using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Player;
public class CameraController : MonoBehaviour
{
    public Transform target;
    public float cameraHeight;
    public float rotateSpeed;
    private Transform _transform;
    public bool rotating = false;

    // Start is called before the first frame update
    void Awake()
    {
        _transform = transform;
    }

    public void InitCamera(PlayerController playerController)
    {
        print(target);
        print(playerController.downVector);


        _transform.position = target.position - playerController.downVector * cameraHeight;
        _transform.forward = target.position - _transform.position;
    }


    public IEnumerator RotateCamera(PlayerController playerController)
    {
        rotating = true;
        Vector3 targetToCam = _transform.position - target.position;
        while (targetToCam.normalized != -playerController.downVector.normalized)
        {
            float step = Time.deltaTime * rotateSpeed; 
            targetToCam = _transform.position - target.position;
            Vector3 temp = Vector3.RotateTowards(targetToCam, -playerController.downVector * cameraHeight, step, 0.1f);

            _transform.position = target.position + temp;
            _transform.forward = -targetToCam;

            yield return null;
        }
        rotating = false;
    }



    // Update is called once per frame
    void Update()
    {
        
    }
}
