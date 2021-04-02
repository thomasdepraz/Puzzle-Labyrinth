using UnityEngine;

namespace Player
{
    public class PlayerSpawner : MonoBehaviour
    {
        [Header("Setup")]
        public WorldVector startingDownVector;
        public WorldVector startingEastVector;
        public WorldVector startingNorthVector;
        public GameObject player;
        private PlayerController playerController;

        void Awake()
        {
            GameObject go = Instantiate(player, transform.position, Quaternion.identity);
            playerController = go.GetComponent<PlayerController>();

            playerController.startingDownVector = startingDownVector;
            playerController.startingNorthVector = startingNorthVector;
            playerController.startingEastVector = startingEastVector;
        }

    }
}
