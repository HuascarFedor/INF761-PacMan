using UnityEngine;

public class Pellet : MonoBehaviour
{
    [SerializeField] protected int points = 10;
    [SerializeField] protected bool isPower = false;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        GameManager.Instance.OnPelletEaten(points, isPower);
        Destroy(gameObject);
    }
}
