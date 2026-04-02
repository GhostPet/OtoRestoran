using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;

public class TerminalKontrol : MonoBehaviour
{
    [Header("UI Elementleri")]
    public InputField girisAlani;
    public Text ciktiYazisi;

    // Gözler ve Partiküller artık kullanılmayabilir ama kodu bozmuyoruz, 
    // sadece startta gizliyoruz. Scriptte boş bırakabilirsin.
    [Header("Robot Gözleri & Glow (Opsiyonel/Silebilirsin)")]
    public Image solGozCore;
    public Image sagGozCore;
    public ParticleSystem solGozParticles;
    public ParticleSystem sagGozParticles;
    public Color gozRengi = Color.white; // Temaya göre tebeşir rengi yapabilirsin

    [Header("Sesler")]
    public AudioSource sesKaynagi; 
    public AudioClip yazmaSesi; // Daktilo sesi yerine tahtaya tebeşir yazma sesi bulursan harika olur
    public AudioClip uyanisSesi; // Bunu "Açılış Zil Sesi" (Shop Bell) ile değiştir

    [Header("Ayarlar")]
    public float yaziHizi = 0.08f; // Tebeşir daha yavaş yazılır
    public float silmeHizi = 0.03f; // Tahta silme hızı
    public float uyanmaSuresi = 1.0f; // Zil çaldıktan sonra açılma süresi

    private string mevcutMesaj = ""; 
    private bool imlecGorunsun = true;
    private bool ayarlarAcik = false;

    void Start()
    {
        // Gözleri Startta kapat (Garanti olsun)
        GozSeffafligiSet(0f);
        if (solGozParticles != null) solGozParticles.Stop();
        if (sagGozParticles != null) sagGozParticles.Stop();

        girisAlani.ActivateInputField();
        StartCoroutine(ImlecDongusu());
        
        // --- YENİ KARŞILAMA MESAJI ---
        StartCoroutine(YaziYazdir("Welcome to Auto-Restaurant Terminal v1.0 sir.\nCommands: /play, /settings, /exit"));
    }

    public void KomutuKontrolEt(string gelenYazi)
    {
        if (string.IsNullOrEmpty(gelenYazi)) return;

        string komut = gelenYazi.Trim().ToLower();

        if (!ayarlarAcik)
        {
            if (komut == "/play")
            {
                StopAllCoroutines();
                StartCoroutine(ImlecDongusu()); 
                StartCoroutine(AcilisSekansi()); // Uyanış değil, Açılış
            }
            else if (komut == "/settings")
            {
                StopAllCoroutines();
                StartCoroutine(ImlecDongusu());
                // Ayarlar menüsü mesajı
                StartCoroutine(MenuyeGecis("-- KITCHEN PREFERENCES --\n/volume [0-100]\n/speed [fast/slow]\n/back", true));
            }
            else if (komut == "/exit") { Application.Quit(); }
            else if (komut != "")
            {
                StartCoroutine(YaziYazdir("\nUnknown command. Please check the terminal log."));
            }
        }
        else 
        {
            if (komut == "/back")
            {
                StopAllCoroutines();
                StartCoroutine(ImlecDongusu());
                // Ayarlardan geri dönme mesajı
                StartCoroutine(MenuyeGecis("Restoring main terminal... What are the next orders, sir?\nCommands: /play, /settings, /exit", false));
            }
            else if (komut.StartsWith("/volume"))
            {
                string[] parcalar = komut.Split(' ');
                if (parcalar.Length > 1 && float.TryParse(parcalar[1], out float deger))
                {
                    AudioListener.volume = Mathf.Clamp(deger / 100f, 0f, 1f);
                }
            }
            else if (komut == "/speed fast") { yaziHizi = 0.04f; }
            else if (komut == "/speed slow") { yaziHizi = 0.12f; }
        }

        girisAlani.text = "";
        girisAlani.ActivateInputField();
    }

    IEnumerator MenuyeGecis(string yeniMesaj, bool ayarlarModu)
    {
        ayarlarAcik = ayarlarModu;
        int sesSayaci = 0;
        while (mevcutMesaj.Length > 0)
        {
            mevcutMesaj = mevcutMesaj.Substring(0, mevcutMesaj.Length - 1);
            EkraniGuncelle();
            if (sesKaynagi != null && yazmaSesi != null)
            {
                sesSayaci++;
                if (sesSayaci % 3 == 0) { sesKaynagi.Stop(); sesKaynagi.PlayOneShot(yazmaSesi, 0.4f); }
            }
            yield return new WaitForSeconds(silmeHizi);
        }
        yield return new WaitForSeconds(0.4f); // Kısa bekleme
        yield return StartCoroutine(YaziYazdir(yeniMesaj));
    }

    IEnumerator YaziYazdir(string mesaj)
    {
        mevcutMesaj = ""; 
        foreach (char harf in mesaj.ToCharArray())
        {
            if (harf == '|') { yield return new WaitForSeconds(1.0f); continue; }
            mevcutMesaj += harf; 
            EkraniGuncelle(); 
            
            if (sesKaynagi != null && yazmaSesi != null && harf != ' ')
            {
                sesKaynagi.Stop();
                sesKaynagi.PlayOneShot(yazmaSesi);
            }
            yield return new WaitForSeconds(yaziHizi);
        }
    }

    // --- YENİ AÇILIŞ SEKANSI ---
    IEnumerator AcilisSekansi()
    {
        girisAlani.gameObject.SetActive(false);
        yield return StartCoroutine(YaziYazdir("Opening kitchen doors...| Preparing ingredients. Please wait."));
        
        // Gözleri kapattık ama uyanma süresini bekliyoruz. 
        // Burada bir dükkan zil sesi (Shop Bell) çalabilirsin.
        if (sesKaynagi != null && uyanisSesi != null) sesKaynagi.PlayOneShot(uyanisSesi);

        yield return new WaitForSeconds(uyanmaSuresi + 0.5f);
        SceneManager.LoadScene("Restaurant");
    }

    // Bu fonksiyonu script bileşeninde göz core resimleri bağlı değilse 
    // hata vermemesi için boş bıraktık.
    void GozSeffafligiSet(float a)
    {
        if (solGozCore != null && sagGozCore != null)
        {
            Color c = gozRengi; c.a = a;
            solGozCore.color = c;
            sagGozCore.color = c;
        }
    }

    IEnumerator ImlecDongusu() { while (true) { imlecGorunsun = !imlecGorunsun; EkraniGuncelle(); yield return new WaitForSeconds(0.5f); } }
    
    void EkraniGuncelle() { ciktiYazisi.text = mevcutMesaj + (imlecGorunsun ? "_" : " "); }
}