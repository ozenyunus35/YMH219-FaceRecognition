# Kullanılan veri seti

Bu projede **gerçek kişilerden izinsiz fotoğraf kullanılmadı.** Örnekler, yüz tanıma derslerinde standart olarak
kullanılan, kamuya açık bir araştırma veri setinden alındı:

**The ORL Database of Faces / Olivetti Faces**
AT&T Laboratories Cambridge, 1992–1994. 40 gönüllü kişi, kişi başı 10 gri tonlamalı fotoğraf.
Kaynak: F. Samaria and A. Harter, *"Parameterisation of a stochastic model for human face identification"*,
2nd IEEE Workshop on Applications of Computer Vision, 1994.
(Bu çalışmada scikit-learn'ün `fetch_olivetti_faces` sürümü kullanıldı; 64×64 piksel.)

| Klasör | İçerik |
|---|---|
| `FaceRecognition/Ornekler/Gönüllü A…D` | 4 gönüllünün 8'er eğitim fotoğrafı (kimlikler anonim, sadece "Gönüllü A/B/C/D" olarak etiketlendi) |
| `FaceRecognition/DenemeGorselleri` | Aynı gönüllülerin eğitimde **kullanılmayan** fotoğrafları, eğitimde olmayan bir kişi ve bunlardan oluşturulmuş bir "grup fotoğrafı" |
| `FaceRecognition.Tests/DegerlendirmeVerisi` | Eğitimde olmayan diğer 36 kişinin 2'şer fotoğrafı (yabancı yüz testi için) |

Uygulama kişileri kimlikleriyle değil, sadece bu anonim etiketlerle tanır. Kendi örneklerinizi eklemek için uygulamadaki
**Eğitim** penceresini kullanabilirsiniz (yalnızca izin veren gönüllülerin fotoğraflarıyla).

Yüz algılama modeli: `haarcascade_frontalface_default.xml` — OpenCV projesi (Intel License Agreement / BSD).
