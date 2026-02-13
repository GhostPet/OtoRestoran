using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class RobotKodlayici : MonoBehaviour
{
    public TMP_InputField numaraInput;   // Sol taraftaki
    public TMP_InputField kodInput;       // Sağ taraftaki
    public Button calistirButon;
    public ScrollRect scrollRect;         // Scroll View bileşeni
    
    private bool senkronizeEdiliyor = false;
    
    void Start()
    {
        calistirButon.onClick.AddListener(KoduCalistir);
        kodInput.onValueChanged.AddListener(NumaralariGuncelle);
        
        // Scroll olaylarını dinle
        scrollRect.onValueChanged.AddListener(ScrollDegisti);
        
        // Başlangıçta numaraları göster
        NumaralariGuncelle("");
    }
    
    void NumaralariGuncelle(string metin)
    {
        if (senkronizeEdiliyor) return;
        
        string[] satirlar = kodInput.text.Split('\n');
        string numaralar = "";
        
        for (int i = 0; i < satirlar.Length; i++)
        {
            numaralar += i + "\n";
        }
        
        numaraInput.text = numaralar;
    }
    
    void ScrollDegisti(Vector2 pozisyon)
    {
        // İki InputField'ın scroll pozisyonlarını senkronize et
        if (senkronizeEdiliyor) return;
        
        senkronizeEdiliyor = true;
        
        // Not: TMP_InputField'ın direkt scroll pozisyonu yok
        // Bu nedenle farklı bir yöntem kullanacağız
        
        senkronizeEdiliyor = false;
    }
    
    void Update()
    {
        // Her frame'de scroll pozisyonlarını kontrol et ve senkronize et
        ScrollSenkronize();
    }
    
    void ScrollSenkronize()
    {
        // Bu kısım biraz karmaşık, alternatif bir çözüm sunacağım
    }
    
    void KoduCalistir()
    {
        string kod = kodInput.text;
        string[] satirlar = kod.Split('\n');
        
        for (int i = 0; i < satirlar.Length; i++)
        {
            string satir = satirlar[i];
            
            if (!string.IsNullOrWhiteSpace(satir))
            {
                Debug.Log("Satır " + i + ": " + satir);
                KomutYorumla(satir.Trim());
            }
        }
    }
    
    void KomutYorumla(string komut)
    {
        if (komut.StartsWith("yazdır(\"") && komut.EndsWith("\")"))
        {
            int basla = komut.IndexOf('"') + 1;
            int bitir = komut.LastIndexOf('"');
            
            if (basla < bitir)
            {
                string mesaj = komut.Substring(basla, bitir - basla);
                Debug.Log("Robot: " + mesaj);
            }
        }
        else if (!string.IsNullOrEmpty(komut))
        {
            Debug.LogWarning("Bilinmeyen komut: " + komut);
        }
    }
}