using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System;

public class BasitRobotKodlayici : MonoBehaviour
{
    public TMP_InputField kodInput;
    public Button calistirButon;
    
    void Start()
    {
        // Butona tıklanınca KoduCalistir fonksiyonunu çalıştır
        calistirButon.onClick.AddListener(runner);
    }

        public bool ParantezKontrol(string kod)
    {
        int sayac = 0;

        foreach (char c in kod)
        {
            if (c == '(')
            {
                sayac++;
            }
            else if (c == ')')
            {
                sayac--;

                // Kapanış fazla ise hata
                if (sayac < 0)
                    return false;
            }
        }

        // Açık parantez kaldıysa hata
        return sayac == 0;
    }
    public bool SusluParantezKontrol(string kod)
    {
        int sayac = 0;

        foreach (char c in kod)
        {
            if (c == '{')
            {
                sayac++;
            }
            else if (c == '}')
            {
                sayac--;

                // Kapanış fazla ise hata
                if (sayac < 0)
                    return false;
            }
        }

        return sayac == 0;
    }
    void runner()
    {
        string kod = kodInput.text;
        if (KodControl(kod))
        {
            KoduCalistir(kod);
        };
    }

    public bool KodControl(string kod)
    {
        bool normalDogru = ParantezKontrol(kod);
        bool susluDogru = SusluParantezKontrol(kod);

        if (!normalDogru)
        {
            Debug.LogError("() parantez hatası var.");
            return false;
        }

        if (!susluDogru)
        {
            Debug.LogError("{} parantez hatası var.");
            return false;
        }

        return true;
    }
    
    void KoduCalistir(string kod)
    {      
        // Kod satırlarına böl
        string[] satirlar = kod.Split('\n');
        
        // Her satırı tek tek işle
        foreach (string satir in satirlar)
        {
            if (!string.IsNullOrWhiteSpace(satir))
            {
                KomutOkuma(satir.Trim());
            }
        }
    }
    public HashSet<string> komutlar = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
    "var", "if", "for", "find", "go", "yazdır"
    };
    
    void KomutOkuma(string komut)
    {
        
    }
}