using UnityEngine;
using UnityEngine.UI;

public class RobotKodlayici : MonoBehaviour
{
    // Burada referanslar� tutaca��z
    public InputField kodInput;

    public Button calistirButon;

    void Start()
    {
        // Butona t�klan�nca KoduCalistir fonksiyonunu �al��t�r
        calistirButon.onClick.AddListener(KoduCalistir);
    }

    void KoduCalistir()
    {
        // InputField'daki metni al
        string kod = kodInput.text;

        // Kod sat�rlar�na b�l
        string[] satirlar = kod.Split('\n');

        // Her sat�r� tek tek i�le
        foreach (string satir in satirlar)
        {
            if (!string.IsNullOrWhiteSpace(satir))
            {
                KomutYorumla(satir.Trim());
            }
        }
    }

    void KomutYorumla(string komut)
    {
        // yazd�r("merhaba") �eklindeki komutlar� alg�la
        if (komut.StartsWith("yazdir(") && komut.EndsWith(")"))
        {
            // T�rnak i�aretlerini bul
            int baslangicIndex = komut.IndexOf('"');
            int bitisIndex = komut.LastIndexOf('"');

            if (baslangicIndex != -1 && bitisIndex != -1 && bitisIndex > baslangicIndex)
            {
                // T�rnak i�aretleri aras�ndaki metni al
                string mesaj = komut.Substring(baslangicIndex + 1, bitisIndex - baslangicIndex - 1);

                // Console'a yazd�r
                Debug.Log("Robot: " + mesaj);
            }
            else
            {
                Debug.LogError("Hata: T�rnak i�aretlerini kontrol et!");
            }
        }
        else
        {
            Debug.LogWarning("Bilinmeyen komut: " + komut);
        }
    }
}