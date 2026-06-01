# Procedural Background Terrain System
### Unity 2D — Mobile Optimized

---

## İçindekiler

1. [Genel Bakış](#genel-bakış)
2. [Özellikler](#özellikler)
3. [Mimari](#mimari)
4. [Sınıf Yapısı](#sınıf-yapısı)
5. [Mod Sistemi](#mod-sistemi)
6. [Chunk Sistemi](#chunk-sistemi)
7. [Parallax Sistemi](#parallax-sistemi)
8. [Renk Tema Sistemi](#renk-tema-sistemi)
9. [Görsel — Soft Çizgi](#görsel--soft-çizgi)
10. [Performans Stratejisi](#performans-stratejisi)
11. [Genişletilebilirlik](#genişletilebilirlik)
12. [Klasör Yapısı](#klasör-yapısı)

---

## Genel Bakış

Bu sistem, Unity 2D yatay ekran mobil oyunlar için tasarlanmış, **tamamen arka plan amaçlı** prosedürel arazi üreticisidir. Asset Store'daki "2D Terrain Generator Based On Pixel (Dead Bit)" asetinden ilham alınmış; ancak şu temel farklar gözetilerek sıfırdan tasarlanmıştır:

| Özellik | Dead Bit Asset | Bu Sistem |
|---|---|---|
| Amaç | Gameplay zemini | Sadece arka plan görseli |
| Collider | Var | **Yok** |
| Görsel stil | Pixel tabanlı | **Soft / organik çizgi** |
| Mod sistemi | Yok | **ScriptableObject modlar** |
| Parallax | Yok | **Çok katmanlı** |
| Performans | Chunk tabanlı | **Chunk + Object Pool** |

Karakter ön planda, terrain arka planda çalışır. Hiçbir fizik ya da çarpışma hesabı yapılmaz.

---

## Özellikler

- **Sonsuz yatay kaydırma** — kamera ilerledikçe chunk'lar önde üretilir, arkada havuza geri döner
- **4 hazır mod** — Dağ, Çöl, Okyanus/Kıyı, Mağara; her biri ayrı bir ScriptableObject dosyası
- **Kolay mod ekleme** — yeni bir ScriptableObject oluşturmak yeni bir mod eklemek için yeterli, tek satır kod gerekmez
- **Mod bazlı renk temaları** — her modun kendine özgü arka plan renk paleti
- **Çok katmanlı parallax** — her katman bağımsız hız oranıyla kayar
- **Soft organik çizgi** — Perlin Noise + Catmull-Rom smoothing ile elle çizilmiş görünüm
- **Object Pool** — mobil için Instantiate/Destroy çağrısı minimumdur
- **Kamera bazlı üretim** — sadece ekranda görünen bölge aktiftir

---

## Mimari

```
TerrainManager
├── TerrainModeConfig (ScriptableObject)
│   ├── DagModu.asset
│   ├── ColModu.asset
│   ├── OkyansuModu.asset
│   └── MagaraModu.asset
├── ChunkSystem
│   ├── TerrainGenerator
│   └── ChunkPool (Object Pool)
├── ParallaxController
│   ├── ParallaxLayer (Katman 1 — en yavaş)
│   ├── ParallaxLayer (Katman 2)
│   └── ParallaxLayer (Katman N — en hızlı)
└── ColorThemeConfig (ScriptableObject)
```

**Veri akışı:**

```
[Kamera hareketi]
       ↓
[ChunkSystem] — kameranın önüne yeni chunk üret
       ↓
[TerrainGenerator] — aktif moda ait config'i oku, Perlin Noise üret, smooth et
       ↓
[MeshRenderer / LineRenderer] — soft çizgiyi çiz, renk temasını uygula
       ↓
[ParallaxController] — kamera hızına göre her katmanı ayrı kaydır
       ↓
[ChunkPool] — ekran dışına çıkan chunk'ı havuza geri al
```

---

## Sınıf Yapısı

### `TerrainManager.cs`
Ana MonoBehaviour. Sahnede tek örneği bulunur (Singleton). Görevleri:
- Aktif modu tutar ve mod geçişlerini yönetir
- ChunkSystem, ParallaxController ve ColorThemeConfig'i başlatır
- Oyun döngüsünde kamera pozisyonunu takip eder

**Önemli alanlar:**
```
TerrainModeConfig activeModeConfig   // Aktif mod ayarları
ChunkSystem chunkSystem
ParallaxController parallaxController
Camera mainCamera
```

---

### `TerrainModeConfig.cs` — ScriptableObject
Her mod için ayrı bir `.asset` dosyası oluşturulur. İçerik:

```
// Perlin Noise parametreleri
float noiseScale          // Dalga frekansı (küçük = geniş dalgalar)
float amplitude           // Tepe yüksekliği (büyük = daha dramatik)
int octaves               // Oktav sayısı (fazla = daha detaylı ama ağır)
float persistence         // Her oktavın etkisi (0-1 arası)
float lacunarity          // Frekans artış katsayısı

// Zemin profili
float baselineY           // Zeminin ekrandaki dikey merkezi
float minY                // Minimum yükseklik sınırı
float maxY                // Maksimum yükseklik sınırı

// Görsel
ColorThemeConfig colorTheme
int parallaxLayerCount
float[] parallaxSpeedRatios

// Meta
string modeName           // Inspector'da görünen isim
Sprite previewSprite      // Editor önizleme görseli (opsiyonel)
```

---

### `TerrainGenerator.cs`
Bir chunk'ın nokta dizisini üretir ve smooth eder.

**Üretim akışı:**
1. `GenerateHeightMap(chunkOffsetX)` — Perlin Noise ile ham yükseklik dizisi üret
2. `SmoothHeightMap(points)` — Catmull-Rom smoothing uygula
3. `BuildMesh(smoothedPoints)` — Alt dolgu dahil mesh oluştur
4. `ApplyColorTheme()` — Renk temasını uygula

**Smoothing notu:** Ham Perlin Noise noktaları köşelidir. Catmull-Rom algoritması bu noktaları geçen sürekli eğri üretir, bu da elle çizilmiş organik görünümü sağlar.

---

### `ChunkSystem.cs`
Kamera takibini ve chunk yaşam döngüsünü yönetir.

**Parametreler:**
```
int chunksAhead       // Kameranın önünde kaç chunk üretilsin (önerilen: 3)
int chunksBehind      // Kameranın arkasında kaç chunk tutulsun (önerilen: 1)
float chunkWidth      // Bir chunk'ın dünya birimi cinsinden genişliği
```

**Döngü mantığı:**
- Her frame kamera X pozisyonunu kontrol et
- Eşik aşılırsa yeni chunk üret (ChunkPool'dan al)
- Ekran dışına çıkan chunk'ı havuza geri gönder
- Chunk'lar pool'dan alındığı için `Instantiate` maliyeti sadece ilk seferdir

---

### `ChunkPool.cs`
Unity'nin `ObjectPool<T>` sınıfı üzerine kurulu basit havuz.

```
int initialPoolSize       // Başlangıçta kaç chunk pre-warm edilsin (önerilen: 6)
```

---

### `ParallaxController.cs`
Birden fazla `ParallaxLayer`'ı yönetir.

Her `ParallaxLayer`:
```
float speedRatio          // 0 = sabit, 1 = kamerayla aynı hız
SpriteRenderer renderer   // veya ikinci bir terrain katmanı
```

Katman hız örneği (3 katman):
```
Katman 1 (en uzak arka plan) : speedRatio = 0.1
Katman 2 (orta plan)         : speedRatio = 0.4
Katman 3 (terrain — ön plan) : speedRatio = 1.0
```

---

### `ColorThemeConfig.cs` — ScriptableObject
Her `TerrainModeConfig` bir `ColorThemeConfig` referansı tutar.

```
Color skyColorTop         // Arka plan gradient üst rengi
Color skyColorBottom      // Arka plan gradient alt rengi
Color terrainColorTop     // Terrain yüzey rengi
Color terrainColorBottom  // Terrain dolgu alt rengi
Color[] accentColors      // Ek detay renkleri (taş, bitki vs. — opsiyonel)
```

---

## Mod Sistemi

### Hazır Modlar

#### Dağ Modu
- Yüksek amplitüd, düşük frekans — geniş ve dramatik tepeler
- Renk paleti: soğuk mavi-gri tonlar, kar beyazı zirveler
- Parallax: 3 katman (uzak dağlar, orta plan, ön plan kayalar)

#### Çöl Modu
- Düşük amplitüd, çok düşük frekans — yavaş yükselen kum tepeleri
- Renk paleti: sıcak sarı-turuncu tonlar, ufukta pembe
- Parallax: 2 katman (ufuk, ön plan kumullar)

#### Okyanus / Kıyı Modu
- Orta amplitüd, yüksek frekans — ince dalgalı kıyı çizgisi
- Renk paleti: mavi-turkuaz su, açık kum rengi kıyı
- Parallax: 3 katman (açık deniz, kıyı, ön plan dalgalar)

#### Mağara Modu
- Hem üst hem alt terrain çizgisi — tünel/mağara efekti
- Renk paleti: koyu kahve-gri taş, sarkıt efekti için açık noktalar
- Parallax: 2 katman (arka duvar, ön plan taşlar)

### Yeni Mod Ekleme
1. `Assets/Terrain/Configs/` klasöründe sağ tık → Create → Terrain → Mode Config
2. Inspector'da tüm alanları doldur
3. `TerrainManager.activeModeConfig` alanına sürükle
4. Kod değişikliği gerekmez

---

## Chunk Sistemi

```
Kamera görüş alanı
←————————————————————————————→
  [chunk-1] [chunk0] [chunk1] [chunk2] [chunk3]
                ↑
           Aktif bölge
  
Sola kayınca:
  chunk-1 → havuza geri    |    chunk4 yeni üretilir
```

Her chunk bağımsız bir GameObject'tir. Chunk genişliği `chunkWidth` ile ayarlanır ve ekran genişliğinin katı olması önerilir (örn. ekran genişliği × 1.5).

---

## Parallax Sistemi

Kamera X ekseninde `delta` kadar hareket ettiğinde:

```
Her katman için:
layer.x -= delta * layer.speedRatio
```

Katmanlar bağımsız hareket ettiğinden derinlik illüzyonu oluşur. En uzak katman en yavaş, terrain katmanı tam kamera hızında hareket eder.

---

## Renk Tema Sistemi

Terrain mesh'ine vertex rengi atanır:
- Üst noktalar `terrainColorTop` alır
- Alt noktalar (zemin dolgusu) `terrainColorBottom` alır
- Unity shader'ı iki renk arasında linear interpolation yapar

Arka plan sky gradient'i için ayrı bir `SpriteRenderer` veya `Camera.backgroundColor` kullanılır.

---

## Görsel — Soft Çizgi

### Neden Soft?
Raw Perlin Noise discrete adımlar üretir — köşeli görünür. Catmull-Rom smoothing algoritması ham noktaları geçen ve aralarında yumuşak eğri üreten bir spline algoritmasıdır.

### Adımlar
1. Perlin Noise ile her X için bir Y noktası üret (çözünürlük: chunk başına ~40-80 nokta)
2. Catmull-Rom kontrolörlerini hesapla
3. Her iki nokta arasına ara noktalar ekle (subdivide × 3-4)
4. Sonuç noktalarından `Mesh` oluştur (alt kenar düz)

### Çözünürlük vs Performans
| Nokta sayısı | Görünüm | Performans |
|---|---|---|
| 20-30 | Biraz köşeli | Çok hızlı |
| 40-60 | Organik, yumuşak | İyi (önerilen) |
| 80+ | Çok pürüzsüz | Mobilde dikkatli ol |

---

## Performans Stratejisi

| Teknik | Açıklama |
|---|---|
| Object Pool | Chunk'lar `Instantiate/Destroy` yerine havuzdan alınır |
| Kamera bazlı üretim | Sadece görünen + yakın chunk'lar aktiftir |
| Collider yok | Fizik hesabı sıfırdır |
| Vertex rengi | Texture yerine vertex rengi → draw call azalır |
| Static batching | Uzak (hareketsiz) arka plan chunk'ları için opsiyonel |

**Hedef:** Orta segment Android cihazda sabit 60 FPS.

---

## Genişletilebilirlik

Sisteme yeni özellik eklemek için değiştirilmesi gereken tek dosya ilgili `ScriptableObject`'tir. Örnek senaryolar:

- **Yeni mod:** Yeni bir `TerrainModeConfig.asset` oluştur
- **Yeni renk teması:** Yeni bir `ColorThemeConfig.asset` oluştur
- **Yeni parallax katmanı:** `TerrainModeConfig`'deki `parallaxLayerCount` değerini artır
- **Gece/gündüz geçişi:** `ColorThemeConfig`'e ikinci bir renk seti ekle, runtime'da lerp yap

---

## Klasör Yapısı

```
Assets/
└── Terrain/
    ├── Scripts/
    │   ├── TerrainManager.cs
    │   ├── TerrainModeConfig.cs
    │   ├── TerrainGenerator.cs
    │   ├── ChunkSystem.cs
    │   ├── ChunkPool.cs
    │   ├── ParallaxController.cs
    │   ├── ParallaxLayer.cs
    │   └── ColorThemeConfig.cs
    ├── Configs/
    │   ├── Modes/
    │   │   ├── DagModu.asset
    │   │   ├── ColModu.asset
    │   │   ├── OkyansuModu.asset
    │   │   └── MagaraModu.asset
    │   └── Themes/
    │       ├── DagTemasi.asset
    │       ├── ColTemasi.asset
    │       ├── OkyanusTemasi.asset
    │       └── MagaraTemasi.asset
    ├── Prefabs/
    │   └── TerrainChunk.prefab
    └── Materials/
        └── TerrainMaterial.mat
```

---

*Bu belge, sistemin tüm bileşenlerini kapsar. Yapım promptu için `PROMPT.md` dosyasına bakınız.*
