using UnityEngine;

public class Goal : MonoBehaviour
{
    void OnTriggerEnter(Collider other)
    {
        // Check if the object entering is the Player
        if (other.CompareTag("Player"))
        {
            Debug.Log("Goal Reached!");

            // Notify Game Manager
            if (GameManager.Instance != null)
            {
                GameManager.Instance.GameClear();
            }

        }
    }
}
