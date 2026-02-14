using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System;
using System.Text.RegularExpressions;

public class BasitRobotKodlayici : MonoBehaviour
{
    public TMP_InputField kodInput;
    public Button calistirButon;
    private Dictionary<string, Action<string>> komutFonksiyonlari;
    
    void Start()
    {
        // Butona tıklanınca KoduCalistir fonksiyonunu çalıştır
        calistirButon.onClick.AddListener(runner);

        komutFonksiyonlari = new Dictionary<string, Action<string>>(StringComparer.OrdinalIgnoreCase)
    {
        { "yazdır", Qyazdir },
        { "if", Qif },
        { "foreach", Qforeach },
        { "find", Qfind },
        { "move", Qmove }
    };
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
        string[] satirlar = kod.Split('\n');

        for (int i = 0; i < satirlar.Length; i++)
        {
            string satir = satirlar[i].Trim();
            if (string.IsNullOrWhiteSpace(satir)) continue;

            // Eğer blok başlatıyorsa
            if (satir.Contains("{"))
            {
                string blok = satir;
                int susluSayac = 0;

                do
                {
                    foreach (char c in satir)
                    {
                        if (c == '{') susluSayac++;
                        if (c == '}') susluSayac--;
                    }

                    if (susluSayac == 0) break;

                    i++;
                    satir = satirlar[i];
                    blok += "\n" + satir;

                } while (i < satirlar.Length);

                SatırOkuma(blok.Trim());
            }
            else
            {
                SatırOkuma(satir);
            }
        }
    }
    public HashSet<string> komutlar = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
    "if", "for", "find", "move", "yazdır"
    };
    
    void SatırOkuma(string satir)
    {
        Match komutMatch = Regex.Match(satir, @"^([\p{L}_][\p{L}\p{N}_]*)");
        if (!komutMatch.Success) return;

        string komut = komutMatch.Groups[1].Value;

        if (!komutFonksiyonlari.ContainsKey(komut))
        {
            Debug.LogError("Geçersiz komut: " + komut);
            return;
        }

        komutFonksiyonlari[komut].Invoke(satir);
    }

    void Qyazdir(string satir)
    {
        Match m = Regex.Match(satir, @"yazdır\s*\(\s*""(.*?)""\s*\)");
        if (m.Success)
        {
            Debug.Log(m.Groups[1].Value);
        }
    }

    void Qif(string satir)
    {
        // Koşulu al
        Match kosulMatch = Regex.Match(satir, @"if\s*\((.*?)\)");
        if (!kosulMatch.Success)
        {
            Debug.LogError("if koşulu hatalı");
            return;
        }

        string kosul = kosulMatch.Groups[1].Value.Trim();

        // Şimdilik basit bool kontrolü
        bool sonuc = kosul == "true";

        if (!sonuc) return;

        // Blok içeriğini al
        Match blokMatch = Regex.Match(satir, @"\{([\s\S]*)\}");
        if (!blokMatch.Success) return;

        string blokIcerik = blokMatch.Groups[1].Value.Trim();

        // Blok içindeki kodu tekrar çalıştır
        KoduCalistir(blokIcerik);
    }

    void Qforeach(string obj)
    {
        
    }
    void Qfind(string obj)
    {
        
    }
    void Qmove(string obj)
    {
        
    }
}