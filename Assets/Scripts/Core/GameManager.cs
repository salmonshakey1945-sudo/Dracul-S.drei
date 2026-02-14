using UnityEngine;
using Dracul.Player;
using Dracul.Core; // For TimeManager if needed

namespace Dracul.Core
{
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        [Header("References")]
        public PlayerStats Player;
        public TimeManager TimeManager;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void Start()
        {
            // Find references if not assigned
            if (Player == null) 
                Player = FindObjectOfType<PlayerStats>();
            
            if (TimeManager == null) 
                TimeManager = FindObjectOfType<TimeManager>();
        }

        // Global Game State Logic
    }
}
