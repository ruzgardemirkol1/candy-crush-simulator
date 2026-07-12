using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

public class GameManager : MonoBehaviour
{
    [Header("Grid Ayarları")]
    public int satirSayisi = 8;
    public int sutunSayisi = 8;
    public float hucreBoyutu = 1f;
    public Vector2 baslangicPozisyonu = new Vector2(-3.5f, 3.5f);
    
    [Header("Prefablar")]
    public GameObject[] sekerPrefablari;
    public GameObject secimGostergesi;
    
    [Header("UI")]
    public Text puanText;
    public Text hamleText;
    public Text hedefText;
    public GameObject oyunBittiPaneli;
    public Text oyunBittiText;
    
    [Header("Oyun Ayarları")]
    public int hedefPuan = 1000;
    public int maxHamle = 30;
    
    private GameObject[,] grid;
    private int puan = 0;
    private int hamle = 0;
    private bool oyunAktif = true;
    private bool islemYapiliyor = false;
    
    private GameObject ilkSecilen;
    private GameObject ikinciSecilen;
    private Vector2Int ilkIndex;
    private Vector2Int ikinciIndex;
    
    void Start()
    {
        grid = new GameObject[satirSayisi, sutunSayisi];
        GridOlustur();
        OlasiliklariKontrolEt();
        
        if (puanText != null) puanText.text = "Puan: " + puan;
        if (hamleText != null) hamleText.text = "Hamle: " + hamle + "/" + maxHamle;
        if (hedefText != null) hedefText.text = "Hedef: " + hedefPuan;
        
        if (oyunBittiPaneli != null)
            oyunBittiPaneli.SetActive(false);
    }
    
    void Update()
    {
        if (!oyunAktif || islemYapiliyor) return;
        
        if (Input.GetMouseButtonDown(0))
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit2D hit = Physics2D.Raycast(ray.origin, Vector2.zero);
            
            if (hit.collider != null)
            {
                GameObject tiklanan = hit.collider.gameObject;
                Vector2Int index = GridIndexBul(tiklanan);
                
                if (index.x >= 0 && index.y >= 0)
                {
                    SekerTiklandi(index, tiklanan);
                }
            }
        }
    }
    
    #region Grid Oluşturma
    
    void GridOlustur()
    {
        for (int satir = 0; satir < satirSayisi; satir++)
        {
            for (int sutun = 0; sutun < sutunSayisi; sutun++)
            {
                Vector3 pozisyon = HucrePozisyonu(satir, sutun);
                GameObject seker = Instantiate(
                    sekerPrefablari[Random.Range(0, sekerPrefablari.Length)],
                    pozisyon,
                    Quaternion.identity
                );
                seker.transform.parent = transform;
                grid[satir, sutun] = seker;
            }
        }
    }
    
    Vector3 HucrePozisyonu(int satir, int sutun)
    {
        float x = baslangicPozisyonu.x + sutun * hucreBoyutu;
        float y = baslangicPozisyonu.y - satir * hucreBoyutu;
        return new Vector3(x, y, 0);
    }
    
    Vector2Int GridIndexBul(GameObject seker)
    {
        for (int satir = 0; satir < satirSayisi; satir++)
        {
            for (int sutun = 0; sutun < sutunSayisi; sutun++)
            {
                if (grid[satir, sutun] == seker)
                    return new Vector2Int(satir, sutun);
            }
        }
        return new Vector2Int(-1, -1);
    }
    
    #endregion
    
    #region Seker Seçme ve Değiştirme
    
    void SekerTiklandi(Vector2Int index, GameObject seker)
    {
        if (ilkSecilen == null)
        {
            ilkSecilen = seker;
            ilkIndex = index;
            SecimGoster(seker, true);
        }
        else if (ikinciSecilen == null && seker != ilkSecilen)
        {
            ikinciSecilen = seker;
            ikinciIndex = index;
            SecimGoster(seker, true);
            
            // Komşu mu kontrol et
            if (KomsuMu(ilkIndex, ikinciIndex))
            {
                StartCoroutine(SekerDegistir());
            }
            else
            {
                // Komşu değilse seçimi temizle
                SecimTemizle();
            }
        }
        else
        {
            SecimTemizle();
            SekerTiklandi(index, seker);
        }
    }
    
    bool KomsuMu(Vector2Int a, Vector2Int b)
    {
        return (Mathf.Abs(a.x - b.x) == 1 && a.y == b.y) ||
               (Mathf.Abs(a.y - b.y) == 1 && a.x == b.x);
    }
    
    IEnumerator SekerDegistir()
    {
        islemYapiliyor = true;
        
        // Değiştir
        grid[ilkIndex.x, ilkIndex.y] = ikinciSecilen;
        grid[ikinciIndex.x, ikinciIndex.y] = ilkSecilen;
        
        Vector3 ilkPoz = ilkSecilen.transform.position;
        Vector3 ikinciPoz = ikinciSecilen.transform.position;
        
        ilkSecilen.transform.position = ikinciPoz;
        ikinciSecilen.transform.position = ilkPoz;
        
        yield return new WaitForSeconds(0.1f);
        
        // Eşleşme kontrolü
        List<Vector2Int> eslesenler = EslesenleriBul();
        
        if (eslesenler.Count > 0)
        {
            hamle++;
            HamleGuncelle();
            
            // Eşleşenleri yok et
            yield return StartCoroutine(EslesenleriYokEt(eslesenler));
            
            // Boşlukları doldur
            yield return StartCoroutine(BosluklariDoldur());
            
            // Yeni eşleşmeleri kontrol et
            while (true)
            {
                eslesenler = EslesenleriBul();
                if (eslesenler.Count == 0) break;
                
                yield return StartCoroutine(EslesenleriYokEt(eslesenler));
                yield return StartCoroutine(BosluklariDoldur());
            }
            
            // Olası hamle var mı kontrol et
            if (!OlasilikVarMi())
            {
                Karistir();
            }
        }
        else
        {
            // Geri değiştir
            grid[ilkIndex.x, ilkIndex.y] = ilkSecilen;
            grid[ikinciIndex.x, ikinciIndex.y] = ikinciSecilen;
            
            ilkSecilen.transform.position = ilkPoz;
            ikinciSecilen.transform.position = ikinciPoz;
        }
        
        SecimTemizle();
        islemYapiliyor = false;
        
        // Oyun bitti mi?
        if (puan >= hedefPuan)
        {
            OyunKazandi();
        }
        else if (hamle >= maxHamle)
        {
            OyunKaybetti();
        }
    }
    
    void SecimGoster(GameObject seker, bool goster)
    {
        if (secimGostergesi != null)
        {
            secimGostergesi.transform.position = seker.transform.position;
            secimGostergesi.SetActive(goster);
        }
    }
    
    void SecimTemizle()
    {
        if (secimGostergesi != null)
            secimGostergesi.SetActive(false);
        
        ilkSecilen = null;
        ikinciSecilen = null;
    }
    
    #endregion
    
    #region Eşleşme Kontrolü
    
    List<Vector2Int> EslesenleriBul()
    {
        List<Vector2Int> eslesenler = new List<Vector2Int>();
        
        // Yatay kontrol
        for (int satir = 0; satir < satirSayisi; satir++)
        {
            for (int sutun = 0; sutun < sutunSayisi - 2; sutun++)
            {
                int tip = SekerTipi(grid[satir, sutun]);
                if (tip == -1) continue;
                
                if (tip == SekerTipi(grid[satir, sutun + 1]) &&
                    tip == SekerTipi(grid[satir, sutun + 2]))
                {
                    eslesenler.Add(new Vector2Int(satir, sutun));
                    eslesenler.Add(new Vector2Int(satir, sutun + 1));
                    eslesenler.Add(new Vector2Int(satir, sutun + 2));
                }
            }
        }
        
        // Dikey kontrol
        for (int sutun = 0; sutun < sutunSayisi; sutun++)
        {
            for (int satir = 0; satir < satirSayisi - 2; satir++)
            {
                int tip = SekerTipi(grid[satir, sutun]);
                if (tip == -1) continue;
                
                if (tip == SekerTipi(grid[satir + 1, sutun]) &&
                    tip == SekerTipi(grid[satir + 2, sutun]))
                {
                    eslesenler.Add(new Vector2Int(satir, sutun));
                    eslesenler.Add(new Vector2Int(satir + 1, sutun));
                    eslesenler.Add(new Vector2Int(satir + 2, sutun));
                }
            }
        }
        
        return eslesenler;
    }
    
    int SekerTipi(GameObject seker)
    {
        if (seker == null) return -1;
        return seker.GetComponent<Seker>().tip;
    }
    
    #endregion
    
    #region Eşleşenleri Yok Etme
    
    IEnumerator EslesenleriYokEt(List<Vector2Int> eslesenler)
    {
        foreach (Vector2Int index in eslesenler)
        {
            if (grid[index.x, index.y] != null)
            {
                Seker seker = grid[index.x, index.y].GetComponent<Seker>();
                seker.Patla();
                puan += 10;
                PuanGuncelle();
            }
        }
        
        yield return new WaitForSeconds(0.3f);
        
        // Grid'den temizle
        foreach (Vector2Int index in eslesenler)
        {
            grid[index.x, index.y] = null;
        }
    }
    
    #endregion
    
    #region Boşlukları Doldur
    
    IEnumerator BosluklariDoldur()
    {
        for (int sutun = 0; sutun < sutunSayisi; sutun++)
        {
            for (int satir = satirSayisi - 1; satir >= 0; satir--)
            {
                if (grid[satir, sutun] == null)
                {
                    // Üstteki dolu olanı bul
                    for (int y = satir - 1; y >= 0; y--)
                    {
                        if (grid[y, sutun] != null)
                        {
                            // Aşağı taşı
                            grid[satir, sutun] = grid[y, sutun];
                            grid[y, sutun] = null;
                            
                            Vector3 hedefPoz = HucrePozisyonu(satir, sutun);
                            grid[satir, sutun].transform.position = hedefPoz;
                            break;
                        }
                    }
                    
                    // Eğer hala boşsa yeni şeker oluştur
                    if (grid[satir, sutun] == null)
                    {
                        Vector3 poz = HucrePozisyonu(satir, sutun);
                        GameObject yeniSeker = Instantiate(
                            sekerPrefablari[Random.Range(0, sekerPrefablari.Length)],
                            HucrePozisyonu(-1, sutun),
                            Quaternion.identity
                        );
                        yeniSeker.transform.parent = transform;
                        grid[satir, sutun] = yeniSeker;
                        yeniSeker.transform.position = poz;
                    }
                }
            }
        }
        
        yield return new WaitForSeconds(0.1f);
    }
    
    #endregion
    
    #region Olasılık Kontrolü
    
    void OlasiliklariKontrolEt()
    {
        while (!OlasilikVarMi())
        {
            Karistir();
        }
    }
    
    bool OlasilikVarMi()
    {
        for (int satir = 0; satir < satirSayisi; satir++)
        {
            for (int sutun = 0; sutun < sutunSayisi; sutun++)
            {
                Vector2Int mevcut = new Vector2Int(satir, sutun);
                
                // Sağa bak
                if (sutun < sutunSayisi - 1)
                {
                    Vector2Int sag = new Vector2Int(satir, sutun + 1);
                    if (OlasilikKontrol(mevcut, sag))
                        return true;
                }
                
                // Aşağı bak
                if (satir < satirSayisi - 1)
                {
                    Vector2Int asagi = new Vector2Int(satir + 1, sutun);
                    if (OlasilikKontrol(mevcut, asagi))
                        return true;
                }
            }
        }
        return false;
    }
    
    bool OlasilikKontrol(Vector2Int a, Vector2Int b)
    {
        // Yer değiştir
        GameObject temp = grid[a.x, a.y];
        grid[a.x, a.y] = grid[b.x, b.y];
        grid[b.x, b.y] = temp;
        
        bool sonuc = EslesenleriBul().Count > 0;
        
        // Geri al
        temp = grid[a.x, a.y];
        grid[a.x, a.y] = grid[b.x, b.y];
        grid[b.x, b.y] = temp;
        
        return sonuc;
    }
    
    void Karistir()
    {
        for (int satir = 0; satir < satirSayisi; satir++)
        {
            for (int sutun = 0; sutun < sutunSayisi; sutun++)
            {
                if (grid[satir, sutun] != null)
                {
                    int tip = Random.Range(0, sekerPrefablari.Length);
                    Destroy(grid[satir, sutun]);
                    Vector3 poz = HucrePozisyonu(satir, sutun);
                    grid[satir, sutun] = Instantiate(sekerPrefablari[tip], poz, Quaternion.identity);
                    grid[satir, sutun].transform.parent = transform;
                }
            }
        }
    }
    
    #endregion
    
    #region UI Güncelleme
    
    void PuanGuncelle()
    {
        if (puanText != null)
            puanText.text = "Puan: " + puan;
    }
    
    void HamleGuncelle()
    {
        if (hamleText != null)
            hamleText.text = "Hamle: " + hamle + "/" + maxHamle;
    }
    
    void OyunKazandi()
    {
        oyunAktif = false;
        if (oyunBittiPaneli != null)
        {
            oyunBittiPaneli.SetActive(true);
            if (oyunBittiText != null)
                oyunBittiText.text = "🎉 Tebrikler Kazandın! 🎉\nPuan: " + puan;
        }
    }
    
    void OyunKaybetti()
    {
        oyunAktif = false;
        if (oyunBittiPaneli != null)
        {
            oyunBittiPaneli.SetActive(true);
            if (oyunBittiText != null)
                oyunBittiText.text = "😢 Hamlen Bitti!\nPuan: " + puan;
        }
    }
    
    #endregion
}