using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Player;


public enum GameState
{
    Running, 
    Paused,
    Menu
}

public class GameManager : MonoBehaviour
{
    public static GameManager instance;
    private void Awake()
    {
        instance = this;
    }


    public GameState currentState;
    public CameraController cameraController;
    public PlayerController playerController;

    // Start is called before the first frame update
    void Start()
    {
        currentState = GameState.Running;  




    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
