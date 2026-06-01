# Procedural Background Terrain System — Yapım Promptu

> Bu prompt, README.md dosyasında tanımlanan sistemi Unity 2D'de sıfırdan uygulamak için bir yapay zekaya veya geliştiriciye verilmek üzere hazırlanmıştır. Tüm kararlar README.md'deki mimari ve tasarım ilkelerine dayanır.

---

## Genel Talimat

Aşağıda tanımlanan sistemi Unity 2D için C# ile yaz. Sistem, yatay ekran mobil oyunlar için **tamamen arka plan amaçlı** prosedürel arazi üreticisidir. Karakter ön planda çalışır; bu sistemin ürettiği terrain arka planda görsel amaçlıdır — **hiçbir Collider, Rigidbody veya fizik bileşeni olmayacaktır.**

Tüm kararlarını aşağıdaki kısıtlara göre ver:
- Hedef platform: **Android / iOS mobil**
- Unity versiyonu: **2022.3 LTS veya üzeri**
- Render pipeline: **URP (Universal Render Pipeline)**
- Ekran yönü: **Yatay (Landscape)**
- Hedef FPS: **60 FPS (orta segment cihaz)**

---

## Adım 1 — Klasör Yapısını Oluştur

Projeye şu klasör yapısını ekle:

```
Assets/
└── Terrain/
    ├── Scripts/
    ├── Configs/
    │   ├── Modes/
    │   └── Themes/
    ├── Prefabs/
    └── Materials/
```

---

## Adım 2 — ColorThemeConfig ScriptableObject

`Assets/Terrain/Scripts/ColorThemeConfig.cs` dosyasını oluştur.

**Gereksinimler:**
- `ScriptableObject` sınıfından türe
- `[CreateAssetMenu]` attribute ekle: menu adı `"Terrain/Color Theme"`, dosya adı `"ColorThemeConfig"`
- Şu public alanları içersin:
  - `Color skyColorTop` — arka plan üst rengi
  - `Color skyColorBottom` — arka plan alt rengi
  - `Color terrainColorTop` — terrain yüzey rengi
  - `Color terrainColorBottom` — terrain alt dolgu rengi
  - `Color[] accentColors` — opsiyonel aksan renkleri dizisi (varsayılan: boş dizi)

Bu dosyayı tamamladıktan sonra `Assets/Terrain/Configs/Themes/` klasöründe dört adet `.asset` dosyası için Inspector değerlerini belirt:

- **DagTemasi.asset:** soğuk mavi (#C8DCF0), açık mavi (#A0BEDC), gri-yeşil (#6B8C6B), koyu gri (#2D3A2D)
- **ColTemasi.asset:** sıcak turuncu (#F5C87A), sarı-kırmızı (#E87840), koyu sarı (#C8943C), koyu kahve (#4A2C1A)
- **OkyanusTemasi.asset:** açık mavi (#A8D4F0), orta mavi (#5090C8), kumlu bej (#D4C08C), koyu mavi (#1A3C5A)
- **MagaraTemasi.asset:** koyu gri (#2A2A2A), siyah (#0A0A0A), koyu kahve (#3A2A1A), çok koyu (#0F0A05)

---

## Adım 3 — TerrainModeConfig ScriptableObject

`Assets/Terrain/Scripts/TerrainModeConfig.cs` dosyasını oluştur.

**Gereksinimler:**
- `ScriptableObject` sınıfından türe
- `[CreateAssetMenu]` attribute: `"Terrain/Mode Config"`, `"TerrainModeConfig"`
- Şu alanları içersin:

```csharp
[Header("Meta")]
public string modeName;
public Sprite previewSprite; // opsiyonel

[Header("Perlin Noise")]
[Range(0.01f, 1f)]   public float noiseScale = 0.1f;
[Range(0.5f, 10f)]   public float amplitude = 3f;
[Range(1, 6)]        public int octaves = 3;
[Range(0f, 1f)]      public float persistence = 0.5f;
[Range(1f, 4f)]      public float lacunarity = 2f;

[Header("Zemin Profili")]
public float baselineY = 0f;
public float minY = -2f;
public float maxY = 4f;

[Header("Mağara Modu")]
public bool isCaveMode = false;       // true ise hem üst hem alt çizgi üretilir
[Range(0.5f, 5f)] public float caveHeight = 2f;

[Header("Görsel")]
public ColorThemeConfig colorTheme;

[Header("Parallax")]
[Range(1, 5)] public int parallaxLayerCount = 3;
[Range(0f, 1f)] public float[] parallaxSpeedRatios; // her katman için hız oranı
```

- Inspector'da `parallaxSpeedRatios` dizisini `parallaxLayerCount` ile otomatik boyutlandırmak için `OnValidate()` metodu ekle

Dört mod için `.asset` dosyaları oluştur ve şu değerleri ata:

**DagModu.asset:**
- noiseScale: 0.08, amplitude: 4.5, octaves: 4, persistence: 0.5, lacunarity: 2.0
- baselineY: -1, minY: -2, maxY: 5
- isCaveMode: false
- colorTheme: DagTemasi
- parallaxLayerCount: 3, speedRatios: [0.1, 0.4, 1.0]

**ColModu.asset:**
- noiseScale: 0.05, amplitude: 1.5, octaves: 2, persistence: 0.4, lacunarity: 1.8
- baselineY: -1.5, minY: -2, maxY: 1.5
- isCaveMode: false
- colorTheme: ColTemasi
- parallaxLayerCount: 2, speedRatios: [0.15, 1.0]

**OkyansuModu.asset:**
- noiseScale: 0.2, amplitude: 0.8, octaves: 3, persistence: 0.6, lacunarity: 2.2
- baselineY: -0.5, minY: -1, maxY: 1
- isCaveMode: false
- colorTheme: OkyanusTemasi
- parallaxLayerCount: 3, speedRatios: [0.05, 0.3, 1.0]

**MagaraModu.asset:**
- noiseScale: 0.12, amplitude: 1.5, octaves: 3, persistence: 0.5, lacunarity: 2.0
- baselineY: 0, minY: -3, maxY: 3
- isCaveMode: true, caveHeight: 2.5
- colorTheme: MagaraTemasi
- parallaxLayerCount: 2, speedRatios: [0.2, 1.0]

---

## Adım 4 — TerrainGenerator

`Assets/Terrain/Scripts/TerrainGenerator.cs` dosyasını oluştur.

**Gereksinimler:**
- `MonoBehaviour` değil, plain C# sınıfı olsun (bileşen değil, yardımcı sınıf)
- Constructor `TerrainModeConfig config` alır
- Şu public metotları içersin:

### `float[] GenerateHeightMap(float startX, int pointCount, float pointSpacing)`
- `startX`: chunk'ın dünya koordinatındaki başlangıç X'i (seamless için offset olarak kullan)
- `pointCount`: kaç nokta üretileceği
- `pointSpacing`: noktalar arası mesafe (dünya birimi)
- Perlin Noise: her oktav için `Mathf.PerlinNoise` çağır, sonuçları persistence ile ağırlıklandır, topla, normalize et
- Sonucu `config.minY` ve `config.maxY` arasına `Mathf.Lerp` ile eşle
- `config.baselineY` ekle
- `float[]` döndür

### `Vector2[] SmoothHeightMap(float[] heights, float pointSpacing)`
- Catmull-Rom spline uygula: her segment için 4 kontrol noktası kullan, segmentte `subdivisions = 4` ara nokta üret
- `Vector2[]` döndür (x = dünya konumu, y = yükseklik)

### `Mesh BuildMesh(Vector2[] topPoints, float bottomY)`
- `topPoints` dizisindeki her nokta için üst vertex ekle
- Her noktanın altında `bottomY` Y değerinde alt vertex ekle
- Quad'larla triangles oluştur (her iki komşu nokta arasında 2 triangle)
- Vertex renklerini ata: üst = `config.colorTheme.terrainColorTop`, alt = `config.colorTheme.terrainColorBottom`
- UVs ekle (opsiyonel, en azından boş dizi ata)
- `mesh.RecalculateBounds()` ve `mesh.RecalculateNormals()` çağır
- Mesh döndür

**Mağara modu için ek metot:**
### `Mesh BuildCaveMesh(Vector2[] topPoints, Vector2[] bottomPoints)`
- Hem üst hem alt terrain çizgisi alır
- Aralarındaki boşluğu kapatacak şekilde mesh oluşturur (sadece çerçeve, iç boş)
- Aynı vertex rengi mantığını uygula

---

## Adım 5 — TerrainChunk

`Assets/Terrain/Scripts/TerrainChunk.cs` ve `Assets/Terrain/Prefabs/TerrainChunk.prefab` oluştur.

**Script gereksinimleri:**
- `MonoBehaviour` olsun
- Şu bileşenlere referans tut: `MeshFilter`, `MeshRenderer`
- Bileşenleri `RequireComponent` attribute ile zorla
- Şu public metotları içersin:

```csharp
public void Initialize(TerrainModeConfig config, float startX, int pointCount, float pointSpacing, float bottomY)
// TerrainGenerator oluştur, mesh üret, MeshFilter'a ata, renk temasını uygula

public void ReturnToPool()
// gameObject.SetActive(false) çağır, pool'a dönüş için event fırlat veya doğrudan pool referansı kullan

public float ChunkWidth { get; }   // startX + (pointCount * pointSpacing)
public float StartX { get; }
public float EndX { get; }
```

**Prefab kurulumu:**
- `TerrainChunk.prefab` üzerinde `MeshFilter` ve `MeshRenderer` bileşeni olsun
- MeshRenderer'da `TerrainMaterial` atanmış olsun
- Collider **ekleme**
- Layer: `"Background"` (bu layer'ı proje ayarlarında oluştur)
- Sorting Layer: `"Background"`, Order in Layer: `0`

---

## Adım 6 — ChunkPool

`Assets/Terrain/Scripts/ChunkPool.cs` dosyasını oluştur.

**Gereksinimler:**
- Unity'nin `UnityEngine.Pool.ObjectPool<TerrainChunk>` sınıfını kullan
- Constructor: `ChunkPool(TerrainChunk prefab, Transform parent, int initialSize = 6)`
- `initialSize` kadar chunk'ı başlangıçta `Prewarm` et (pool'a ekle ama pasif bırak)
- `Get()` → havuzdan chunk al, aktif et
- `Release(TerrainChunk chunk)` → pasif et, havuza geri koy

---

## Adım 7 — ChunkSystem

`Assets/Terrain/Scripts/ChunkSystem.cs` dosyasını oluştur.

**Gereksinimler:**
- `MonoBehaviour` olsun
- Şu serialize edilmiş alanları içersin:

```csharp
[SerializeField] TerrainChunk chunkPrefab;
[SerializeField] Transform chunksParent;
[SerializeField] int chunksAhead = 3;
[SerializeField] int chunksBehind = 1;
[SerializeField] float chunkWidth = 20f;
[SerializeField] int pointsPerChunk = 50;
[SerializeField] float bottomY = -10f;
```

- `Initialize(TerrainModeConfig config, Camera cam)` metodu: pool oluştur, başlangıç chunk'larını üret
- `Update()` içinde:
  - Kameranın sağ kenarını hesapla: `cam.transform.position.x + cam.orthographicSize * cam.aspect`
  - Sağda yeterli chunk yoksa yeni chunk üret
  - Sol kenarın gerisinde kalan chunk'ları `ChunkPool`'a geri gönder
- Mod değişikliğinde: `SwitchMode(TerrainModeConfig newConfig)` — tüm aktif chunk'ları havuza geri gönder, yeni mod ile yeniden üret

---

## Adım 8 — ParallaxLayer

`Assets/Terrain/Scripts/ParallaxLayer.cs` dosyasını oluştur.

**Gereksinimler:**
- `MonoBehaviour` olsun
- Şu alanları içersin:

```csharp
[SerializeField] float speedRatio = 0.5f;  // 0 = sabit, 1 = kamerayla aynı
[SerializeField] SpriteRenderer layerRenderer; // opsiyonel sprite arka plan katmanı
```

- `UpdatePosition(float cameraDeltaX)` metodu: `transform.position.x -= cameraDeltaX * speedRatio`
- Sonsuz döngü için: katmanın sağ kenarı kameranın soluna geçerse katmanı sağa teleport et (sprite tabanlı katmanlar için)

---

## Adım 9 — ParallaxController

`Assets/Terrain/Scripts/ParallaxController.cs` dosyasını oluştur.

**Gereksinimler:**
- `MonoBehaviour` olsun
- `List<ParallaxLayer> layers` listesi tut
- `Initialize(TerrainModeConfig config)` metodu:
  - Config'deki `parallaxLayerCount` ve `parallaxSpeedRatios` ile katmanları ayarla
- `Update()`:
  - `float delta = cam.transform.position.x - lastCamX` hesapla
  - Tüm katmanlara `UpdatePosition(delta)` çağır
  - `lastCamX` güncelle

---

## Adım 10 — TerrainManager

`Assets/Terrain/Scripts/TerrainManager.cs` dosyasını oluştur.

**Gereksinimler:**
- `MonoBehaviour` olsun, Singleton pattern uygula:

```csharp
public static TerrainManager Instance { get; private set; }
void Awake() {
    if (Instance != null) { Destroy(gameObject); return; }
    Instance = this;
}
```

- Serialize edilmiş alanlar:

```csharp
[SerializeField] TerrainModeConfig initialModeConfig;
[SerializeField] ChunkSystem chunkSystem;
[SerializeField] ParallaxController parallaxController;
[SerializeField] Camera mainCamera;
```

- `Start()`: `chunkSystem.Initialize(initialModeConfig, mainCamera)` ve `parallaxController.Initialize(initialModeConfig)` çağır
- `SwitchMode(TerrainModeConfig newConfig)` public metodu: her iki sistemi de yeni config ile güncelle

---

## Adım 11 — Material ve Shader Kurulumu

**Material:**
- `Assets/Terrain/Materials/TerrainMaterial.mat` oluştur
- Shader: `Universal Render Pipeline/Lit` veya daha hafif olan `Universal Render Pipeline/Unlit`
- **Vertex Color** desteği için Unlit shader kullan (performans açısından tercih edilir)
- `_Color` beyaz olarak ayarla (vertex renkleri kendi rengini taşır)

**Eğer URP custom shader gerekirse:**
- Vertex rengi okuyup `albedo` olarak çıkaran minimal bir Shader Graph oluştur
- Node zinciri: `Vertex Color → Base Color → Unlit Master`

---

## Adım 12 — Sahne Kurulumu

Sahnede şu hiyerarşiyi oluştur:

```
Scene
├── TerrainSystem (Empty GameObject)
│   ├── TerrainManager (script)
│   ├── ChunkSystem (script)
│   ├── ParallaxController (script)
│   └── ChunksParent (Empty, chunk'ların parent'ı)
├── ParallaxLayers (Empty GameObject)
│   ├── Layer_Far (ParallaxLayer script, speedRatio: 0.1)
│   ├── Layer_Mid (ParallaxLayer script, speedRatio: 0.4)
│   └── Layer_Near — terrain chunk'ları bu katmanda (speedRatio: 1.0)
└── Main Camera
```

**Camera ayarları:**
- Projection: Orthographic
- Size: oyun tasarımına göre ayarla (örn. 5)
- Background: `ColorThemeConfig.skyColorTop` (runtime'da TerrainManager set etsin)
- Culling Mask: Background layer dahil olsun

---

## Adım 13 — Test Sahne Script'i

`Assets/Terrain/Scripts/Editor/TerrainTestRunner.cs` (MonoBehaviour, editor dışı da çalışan) oluştur:

```csharp
// Play modunda klavyeden mod geçişi test etmek için:
// 1 tuşu → DagModu
// 2 tuşu → ColModu  
// 3 tuşu → OkyansuModu
// 4 tuşu → MagaraModu
// Sağ ok tuşu → kamerayı sağa kaydır (infinite scroll testi)
```

---

## Teslim Kriterleri

Kod tamamlandığında şunlar doğrulanmış olmalıdır:

- [ ] Tüm `.cs` dosyaları derleniyor, Console'da hata yok
- [ ] 4 mod çalışıyor, her birinde farklı terrain profili görünüyor
- [ ] Kamera sağa hareket ettiğinde yeni chunk'lar üretiliyor, eski chunk'lar havuza dönüyor
- [ ] Parallax efekti görünür (farklı hızlarda kayan katmanlar)
- [ ] Profiler'da Instantiate/Destroy spike'ı yok (Object Pool çalışıyor)
- [ ] Collider yok (Physics Debugger'da terrain görünmüyor)
- [ ] Terrain çizgisi köşeli değil, smooth ve organik görünüyor
- [ ] Mağara modunda hem üst hem alt çizgi üretiliyor
- [ ] Mod geçişi (SwitchMode) çökmesiz çalışıyor

---

## Önemli Notlar

1. **Seamless chunk birleşimi:** Bir chunk biterken sonraki chunk başlarken çizgi kesintisiz olmalıdır. Bunu sağlamak için `GenerateHeightMap`'e verilen `startX` değeri bir önceki chunk'ın son noktasının X koordinatı olmalıdır. Perlin Noise deterministik olduğundan aynı X değeri her zaman aynı Y'yi üretir.

2. **Random seed:** `TerrainModeConfig`'e `float noiseSeed` alanı ekle (varsayılan: 0). Her oyun başlangıcında `Random.Range(0f, 9999f)` ile set et. Bu değeri Perlin Noise X input'una ekle: `Mathf.PerlinNoise(x * noiseScale + noiseSeed, octave * 100f)`.

3. **Mağara modu zemin çizgisi:** Alt çizgi için `baselineY - caveHeight / 2`, üst çizgi için `baselineY + caveHeight / 2` kullan. Her iki çizgiye de ayrı Perlin Noise uygula ama alt noise'u ters çevir (çarpı -1) ki birbirinden uzaklaşsın.

4. **Vertex rengi için shader:** Unity'nin standart URP Unlit shader'ı vertex rengi desteklemez. Shader Graph'ta `Vertex Color` node'u ile custom shader oluşturman gerekecektir. Bu adımı atlama.

5. **Performans önceliği:** Şüphe durumunda her zaman daha az nokta, daha az oktav, daha az katman tercih et. Mobil cihazda güzel ama yavaş > çirkin ama hızlı tercih sırasında her zaman hızlıyı seç.
