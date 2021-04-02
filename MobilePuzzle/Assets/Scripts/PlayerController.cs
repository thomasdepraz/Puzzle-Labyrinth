using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Player
{   
    public enum WorldVector {X, Y, Z, mX, mY, mZ }

    public class PlayerController : MonoBehaviour
    {
        private Transform _transform;

        [Header("Setup")]
        public WorldVector startingDownVector;
        public WorldVector startingEastVector;
        public WorldVector startingNorthVector;
        public CameraController camController;

        [Header("Movement")]
        public float speed;
        public float acceleration;
        private float currentSpeed = 0;
        private Vector3 movementDirection;
        private bool moving;
        private bool check = true;

       [HideInInspector]public Vector3 eastVector;
       [HideInInspector]public Vector3 westVector;
       [HideInInspector]public Vector3 northVector;
       [HideInInspector]public Vector3 southVector;
       [HideInInspector]public Vector3 downVector;

        [Header("Raycasting")]
        public float rayLength;
        public LayerMask wallLayer;
        public LayerMask floorLayer;


        // Start is called before the first frame update
        void Start()
        {
            _transform = transform;

            #region switches
            switch (startingDownVector)
            {
                case WorldVector.X:
                    downVector = new Vector3(1, 0, 0);
                    break;
                case WorldVector.Y:
                    downVector = new Vector3(0, 1, 0);
                    break;
                case WorldVector.Z:
                    downVector = new Vector3(0, 0, 1);
                    break;
                case WorldVector.mX:
                    downVector = new Vector3(-1, 0, 0);
                    break;
                case WorldVector.mY:
                    downVector = new Vector3(0, -1, 0);
                    break;
                case WorldVector.mZ:
                    downVector = new Vector3(0, 0, -1);
                    break;
                default:
                    break;
            }

            switch (startingEastVector)
            {
                case WorldVector.X:
                    eastVector = new Vector3(1, 0, 0);
                    break;
                case WorldVector.Y:
                    eastVector = new Vector3(0, 1, 0);
                    break;
                case WorldVector.Z:
                    eastVector = new Vector3(0, 0, 1);
                    break;
                case WorldVector.mX:
                    eastVector = new Vector3(-1, 0, 0);
                    break;
                case WorldVector.mY:
                    eastVector = new Vector3(0, -1, 0);
                    break;
                case WorldVector.mZ:
                    eastVector = new Vector3(0, 0, -1);
                    break;
                default:
                    break;
            }

            switch (startingNorthVector)
            {
                case WorldVector.X:
                    northVector = new Vector3(1, 0, 0);
                    break;
                case WorldVector.Y:
                    northVector = new Vector3(0, 1, 0);
                    break;
                case WorldVector.Z:
                    northVector = new Vector3(0, 0, 1);
                    break;
                case WorldVector.mX:
                    northVector = new Vector3(-1, 0, 0);
                    break;
                case WorldVector.mY:
                    northVector = new Vector3(0, -1, 0);
                    break;
                case WorldVector.mZ:
                    northVector = new Vector3(0, 0, -1);
                    break;
                default:
                    break;
            }

            westVector = -eastVector;
            southVector = -northVector;

            #endregion

            camController = GameManager.instance.cameraController;
            camController.InitCamera(this);
        }

        // Update is called once per frame
        void Update()
        {        
            if(!camController.rotating)
            {
                if(Input.GetKeyDown(KeyCode.RightArrow) && !moving)
                {
                    StartCoroutine(Move(eastVector));
                }
                if (Input.GetKeyDown(KeyCode.UpArrow) && !moving)
                {
                    StartCoroutine(Move(northVector));
                }
                if (Input.GetKeyDown(KeyCode.DownArrow) && !moving)
                {
                    StartCoroutine(Move(southVector));
                }
                if (Input.GetKeyDown(KeyCode.LeftArrow) && !moving)
                {
                    StartCoroutine(Move(westVector));
                }
            }


            if(Input.touchCount > 0)
            {
                Touch touch = Input.GetTouch(0);
                if(!moving && !camController.rotating)
                {
                    if(touch.deltaPosition.magnitude > 0.5 && check)
                    {
                        check = false;
                        float scalarUp = Vector3.Dot(Vector3.up, touch.deltaPosition.normalized);
                        float scalarRight = Vector3.Dot(Vector3.right, touch.deltaPosition.normalized);
                        if (scalarUp > 0.8f)
                        {
                            StartCoroutine(Move(northVector));
                        } 
                        else if(scalarUp < -0.8f)
                        {
                            StartCoroutine(Move(southVector));
                        }

                        if(scalarRight > 0.8f)
                        {
                            StartCoroutine(Move(eastVector));
                        

                        }
                        else if(scalarRight < -0.8f)
                        {
                            StartCoroutine(Move(westVector));
                        }
                    }
                }
            }
            if(Input.touchCount == 0 && !check)
            {
                check = true;
            }


            /*
            Debug.DrawRay(_transform.position, eastVector, Color.blue);
            Debug.DrawRay(_transform.position, northVector, Color.red);
            Debug.DrawRay(_transform.position, southVector, Color.green);
            Debug.DrawRay(_transform.position, westVector, Color.yellow);
            Debug.DrawRay(_transform.position, downVector, Color.cyan);
            */
        }

        public bool FloorDetection()
        {
            if (Physics.Raycast(_transform.position, downVector, rayLength, floorLayer))
            {
                return true;
            }
            else
            {
                Vector3 previousMovementDirection = movementDirection;

                //Change Direction vector to a copy of down vector
                movementDirection = downVector;

                #region ChangeDirections
                if (previousMovementDirection == eastVector)
                {
                    eastVector = downVector;
                    westVector = -eastVector;
                }
                else if(previousMovementDirection == westVector)
                {
                    westVector = downVector;
                    eastVector = -westVector;
                }
                else if(previousMovementDirection == northVector)
                {
                    northVector = downVector;
                    southVector = -northVector;
                }
                else if(previousMovementDirection ==southVector)
                {
                    southVector = downVector;
                    northVector = -southVector;
                }
                downVector = -previousMovementDirection;
                #endregion

                return false;
            }
        }

        RaycastHit hit;
        public RaycastHit CollisionDetection(Vector3 movingDirection)
        {
            Physics.Raycast(_transform.position, movingDirection, out hit, 15, wallLayer);
            return hit; 
        }

        public IEnumerator Move(Vector3 movingDirection)
        {
            print(movingDirection);
            movementDirection = movingDirection;
            moving = true;
            CollisionDetection(movingDirection);

            Vector3 targetPosition = hit.point - movingDirection/2;

            currentSpeed = 0;

            while(_transform.position != targetPosition)
            { 

                if(currentSpeed < speed)
                {
                    currentSpeed += acceleration * Time.deltaTime;
                }
                else
                {
                    currentSpeed = speed;
                }
                _transform.position = Vector3.MoveTowards(_transform.position, targetPosition, 0.1f * currentSpeed);
                yield return null;
            }

            yield return null;

            if(FloorDetection())
            {
                moving = false;
                movementDirection = Vector3.zero;
            }
            else
            {
                StartCoroutine(camController.RotateCamera(this));
                StartCoroutine(Move(movementDirection));
            }
        }
    }
}
