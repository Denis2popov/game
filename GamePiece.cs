using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GamePiece : MonoBehaviour
{
    public int xIndex;
    public int yIndex;
    
    [Tooltip("Тип фигуры (цвет или форма)")]
    public int type;
    
    // Константный масштаб для всех фигур
    public readonly Vector3 fixedScale = new Vector3(0.430229515f, 0.430229515f, 0.430229515f);
    
    // Добавляем перечисление для типов бомб
   public enum BombType
{
    None,
    Horizontal,
    Vertical,
    Color    // Добавляем тип для цветной бомбы
}
    
    // Свойство для хранения типа бомбы
    public BombType bombType = BombType.None;
    
    // Визуальные элементы для бомб
    private GameObject horizontalBombIndicator;
    private GameObject verticalBombIndicator;
    
    public void Init(int x, int y)
    {
        xIndex = x;
        yIndex = y;
        
        // Установка фиксированного масштаба
        transform.localScale = fixedScale;
    }
    
    // Метод для превращения в бомбу
    public void SetBomb(BombType type)
    {
        bombType = type;
        UpdateBombVisual();
    }
    
    // Обновление визуала бомбы
    private void UpdateBombVisual()
    {
        // Удаляем предыдущие индикаторы если они есть
        if (horizontalBombIndicator != null)
            Destroy(horizontalBombIndicator);
        if (verticalBombIndicator != null)
            Destroy(verticalBombIndicator);
        if (colorBombIndicator != null)
            Destroy(colorBombIndicator);
            
        // Создаем новые визуальные индикаторы в зависимости от типа бомбы
        if (bombType == BombType.Horizontal)
        {
            // Создаем горизонтальную полоску или другой индикатор
            horizontalBombIndicator = CreateBombIndicator(new Vector3(0.8f, 0.2f, 1f), Color.yellow);
        }
        else if (bombType == BombType.Vertical)
        {
            // Создаем вертикальную полоску
            verticalBombIndicator = CreateBombIndicator(new Vector3(0.2f, 0.8f, 1f), Color.cyan);
        }
        else if (bombType == BombType.Color)
        {
            // Создаем индикатор цветной бомбы (круглый)
            colorBombIndicator = CreateCircularBombIndicator(Color.magenta);
        }
    }
    
    // Добавляем переменную для индикатора цветной бомбы
    private GameObject colorBombIndicator;

    // Добавляем метод для создания круглого индикатора
    private GameObject CreateCircularBombIndicator(Color color)
    {
        GameObject indicator = new GameObject("ColorBombIndicator");
        indicator.transform.parent = transform;
        indicator.transform.localPosition = Vector3.zero;
        
        SpriteRenderer renderer = indicator.AddComponent<SpriteRenderer>();
        renderer.sprite = Resources.Load<Sprite>("ColorBombIndicator");
        
        // Если sprite не найден, создаем простой белый круг
        if (renderer.sprite == null)
        {
            Texture2D texture = new Texture2D(64, 64);
            Color[] colors = new Color[64 * 64];
            
            // Создаем круг
            for (int x = 0; x < 64; x++)
            {
                for (int y = 0; y < 64; y++)
                {
                    float distanceFromCenter = Vector2.Distance(new Vector2(x, y), new Vector2(32, 32));
                    if (distanceFromCenter < 28)
                    {
                        colors[y * 64 + x] = Color.white;
                    }
                    else
                    {
                        colors[y * 64 + x] = new Color(1, 1, 1, 0);
                    }
                }
            }
            
            texture.SetPixels(colors);
            texture.Apply();
            
            Sprite sprite = Sprite.Create(texture, new Rect(0, 0, 64, 64), new Vector2(0.5f, 0.5f));
            renderer.sprite = sprite;
        }
        
        // Устанавливаем цвет и размер
        renderer.color = color;
        indicator.transform.localScale = new Vector3(0.4f, 0.4f, 1f);
        
        // Ставим сортировку выше, чтобы было видно поверх фигуры
        renderer.sortingOrder = GetComponent<SpriteRenderer>().sortingOrder + 1;
        
        return indicator;
    }
    // Вспомогательный метод для создания индикатора бомбы
    private GameObject CreateBombIndicator(Vector3 scale, Color color)
    {
        GameObject indicator = new GameObject("BombIndicator");
        indicator.transform.parent = transform;
        indicator.transform.localPosition = Vector3.zero;
        
        SpriteRenderer renderer = indicator.AddComponent<SpriteRenderer>();
        renderer.sprite = Resources.Load<Sprite>("BombIndicator");
        
        // Если sprite не найден, создаем простой белый квадрат
        if (renderer.sprite == null)
        {
            Texture2D texture = new Texture2D(64, 64);
            Color[] colors = new Color[64 * 64];
            for (int i = 0; i < colors.Length; i++)
                colors[i] = Color.white;
            texture.SetPixels(colors);
            texture.Apply();
            
            Sprite sprite = Sprite.Create(texture, new Rect(0, 0, 64, 64), new Vector2(0.5f, 0.5f));
            renderer.sprite = sprite;
        }
        
        // Устанавливаем цвет и размер
        renderer.color = color;
        indicator.transform.localScale = scale;
        
        // Ставим сортировку выше, чтобы было видно поверх фигуры
        renderer.sortingOrder = GetComponent<SpriteRenderer>().sortingOrder + 1;
        
        return indicator;
    }
    
    void Start()
    {
        // Устанавливаем масштаб при создании объекта
        transform.localScale = fixedScale;
    }
    
    void OnValidate()
    {
        // Убедимся, что тип не отрицательный
        if (type < 0) type = 0;
        
        // Устанавливаем масштаб также в редакторе
        transform.localScale = fixedScale;
    }
    
    // Публичный метод для сброса масштаба, если где-то он изменится
    public void ResetScale()
    {
        transform.localScale = fixedScale;
    }
}
