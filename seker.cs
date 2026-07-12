using UnityEngine;

public class Seker : MonoBehaviour
{
    [Header("Şeker Tipi")]
    public int tip;
    
    [Header("Patlama Efekti")]
    public GameObject patlamaEfekti;
    public AudioClip patlamaSesi;
    
    private AudioSource sesKaynagi;
    private SpriteRenderer spriteRenderer;
    
    void Start()
    {
        sesKaynagi = GetComponent<AudioSource>();
        spriteRenderer = GetComponent<SpriteRenderer>();
    }
    
    public void Patla()
    {
        // Patlama efekti
        if (patlamaEfekti != null)
        {
            GameObject efekt = Instantiate(patlamaEfekti, transform.position, Quaternion.identity);
            Destroy(efekt, 0.5f);
        }
        
        // Ses
        if (sesKaynagi != null && patlamaSesi != null)
        {
            sesKaynagi.PlayOneShot(patlamaSesi);
        }
        
        // Şekeri yok et
        Destroy(gameObject, 0.1f);
    }
}