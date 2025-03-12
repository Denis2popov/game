using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LightningEffect : MonoBehaviour
{
    [Header("Настройки молнии")]
    public Color lightningColor = Color.cyan;
    public float width = 0.2f;
    public float lifetime = 0.5f;
    public float variance = 0.25f; // Насколько сильно молния будет изгибаться
    public int segments = 12; // Количество сегментов в молнии
    public float flashFrequency = 0.05f; // Частота мерцания
    
    [Header("Эффекты частиц")]
    public bool useParticles = true;
    public float particleSize = 0.15f;
    public Color particleColor = new Color(0.5f, 0.8f, 1f, 0.7f);
    
    private LineRenderer lightningLine;
    private ParticleSystem lightningParticles;
    private bool isHorizontal;
    
    // Фактические координаты фигур для молнии
    private Vector3[] piecesPositions;
    
    /// <summary>
    /// Инициализация эффекта молнии по конкретным позициям фигур
    /// </summary>
    public void Initialize(bool horizontal, List<Vector3> positions)
    {
        if (positions == null || positions.Count < 2)
        {
            Debug.LogError("Недостаточно позиций для создания молнии");
            Destroy(gameObject);
            return;
        }
        
        isHorizontal = horizontal;
        
        // Сортируем позиции для правильного порядка молнии
        if (isHorizontal)
        {
            // Сортировка по X (слева направо)
            positions.Sort((a, b) => a.x.CompareTo(b.x));
        }
        else
        {
            // Сортировка по Y (снизу вверх)
            positions.Sort((a, b) => a.y.CompareTo(b.y));
        }
        
        piecesPositions = positions.ToArray();
        
        // Устанавливаем позицию объекта молнии в первую позицию
        transform.position = piecesPositions[0];
        
        // Создаем молнию
        CreateLightning();
        
        // Запускаем анимацию
        StartCoroutine(AnimateLightning());
    }
    
    private void CreateLightning()
    {
        // Создаем LineRenderer для молнии
        lightningLine = gameObject.AddComponent<LineRenderer>();
        lightningLine.positionCount = piecesPositions.Length * 2 - 1; // Больше точек для зигзагов
        lightningLine.startWidth = width;
        lightningLine.endWidth = width;
        lightningLine.material = new Material(Shader.Find("Sprites/Default"));
        lightningLine.startColor = lightningColor;
        lightningLine.endColor = lightningColor;
        
        // Устанавливаем линию в мировых координатах
        lightningLine.useWorldSpace = true;
        
        // Генерируем случайную форму молнии
        GenerateLightningPoints();
        
        // Создаем систему частиц для искр
        if (useParticles)
        {
            GameObject particleObj = new GameObject("LightningParticles");
            particleObj.transform.parent = transform;
            particleObj.transform.localPosition = Vector3.zero;
            
            lightningParticles = particleObj.AddComponent<ParticleSystem>();
            
            // Настраиваем систему частиц
            var main = lightningParticles.main;
            main.startLifetime = 0.3f;
            main.startSpeed = 1.0f;
            main.startSize = particleSize;
            main.startColor = particleColor;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            
            var emission = lightningParticles.emission;
            emission.rateOverTime = 100;
            
            var shape = lightningParticles.shape;
            shape.shapeType = ParticleSystemShapeType.Box; // Используем Box вместо Edge или Line
            
            // Устанавливаем размер прямоугольника в зависимости от длины молнии
            float length = Vector3.Distance(piecesPositions[0], piecesPositions[piecesPositions.Length - 1]);
            if (isHorizontal)
            {
                shape.scale = new Vector3(length, 0.1f, 0.1f);
                shape.position = new Vector3(length/2, 0, 0);
            }
            else
            {
                shape.scale = new Vector3(0.1f, length, 0.1f);
                shape.position = new Vector3(0, length/2, 0);
            }
            
            // Добавляем цветовой градиент
            var colorOverLifetime = lightningParticles.colorOverLifetime;
            colorOverLifetime.enabled = true;
            
            Gradient gradient = new Gradient();
            gradient.SetKeys(
                new GradientColorKey[] { 
                    new GradientColorKey(particleColor, 0.0f),
                    new GradientColorKey(Color.white, 1.0f)
                },
                new GradientAlphaKey[] { 
                    new GradientAlphaKey(1.0f, 0.0f),
                    new GradientAlphaKey(0.0f, 1.0f)
                }
            );
            
            colorOverLifetime.color = gradient;
            
            // Добавляем модуль размера по времени жизни
            var sizeOverLifetime = lightningParticles.sizeOverLifetime;
            sizeOverLifetime.enabled = true;
            sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, 0.1f);
        }
    }
    
    private void GenerateLightningPoints()
    {
        int totalPoints = piecesPositions.Length * 2 - 1;
        Vector3[] points = new Vector3[totalPoints];
        
        // Каждая реальная позиция фигуры будет точкой молнии
        // Между ними будут дополнительные точки для зигзагов
        for (int i = 0; i < piecesPositions.Length; i++)
        {
            int pointIndex = i * 2;
            if (pointIndex < totalPoints)
            {
                points[pointIndex] = piecesPositions[i];
            }
            
            // Добавляем промежуточные точки с отклонениями
            if (i < piecesPositions.Length - 1)
            {
                Vector3 midPoint = (piecesPositions[i] + piecesPositions[i + 1]) / 2;
                
                // Добавляем случайное смещение перпендикулярно основному направлению
                float randomOffset = Random.Range(-variance, variance);
                Vector3 offset;
                
                if (isHorizontal)
                {
                    offset = new Vector3(0, randomOffset, 0);
                }
                else
                {
                    offset = new Vector3(randomOffset, 0, 0);
                }
                
                if (pointIndex + 1 < totalPoints)
                {
                    points[pointIndex + 1] = midPoint + offset;
                }
            }
        }
        
        lightningLine.positionCount = totalPoints;
        lightningLine.SetPositions(points);
    }
    
    private IEnumerator AnimateLightning()
    {
        float elapsed = 0;
        
        // Мерцание молнии
        while (elapsed < lifetime)
        {
            // Генерируем новую форму молнии
            GenerateLightningPoints();
            
            // Случайно меняем цвет для эффекта мерцания
            float intensity = Random.Range(0.7f, 1.0f);
            Color flashColor = new Color(
                lightningColor.r * intensity,
                lightningColor.g * intensity,
                lightningColor.b * intensity,
                lightningColor.a);
            
            lightningLine.startColor = flashColor;
            lightningLine.endColor = flashColor;
            
            // Случайно меняем ширину
            float widthVariation = Random.Range(0.8f, 1.2f);
            lightningLine.startWidth = width * widthVariation;
            lightningLine.endWidth = width * (widthVariation * 0.7f);
            
            yield return new WaitForSeconds(flashFrequency);
            elapsed += flashFrequency;
        }
        
        // Затухание эффекта
        float fadeTime = 0.2f;
        float fadeElapsed = 0;
        Color initialColor = lightningLine.startColor;
        
        while (fadeElapsed < fadeTime)
        {
            fadeElapsed += Time.deltaTime;
            float fade = 1 - (fadeElapsed / fadeTime);
            
            lightningLine.startColor = new Color(
                initialColor.r, initialColor.g, initialColor.b, fade);
            lightningLine.endColor = new Color(
                initialColor.r, initialColor.g, initialColor.b, fade * 0.7f);
            
            yield return null;
        }
        
        // Остановка частиц
        if (lightningParticles != null)
        {
            var emission = lightningParticles.emission;
            emission.rateOverTime = 0;
            yield return new WaitForSeconds(0.3f); // Дождаться исчезновения частиц
        }
        
        // Уничтожаем объект после завершения анимации
        Destroy(gameObject);
    }
    
    /// <summary>
    /// Статический метод для удобного создания эффекта молнии
    /// </summary>
   public static LightningEffect CreateLightningEffect(bool isHorizontal, 
                                                List<GamePiece> piecesToDestroy, 
                                                Color lightningColor)
{
    // Собираем мировые позиции всех фигур, которые будут уничтожены
    List<Vector3> positions = new List<Vector3>();
    foreach (GamePiece piece in piecesToDestroy)
    {
        positions.Add(piece.transform.position);
    }
    
    GameObject lightningObj = new GameObject("LightningEffect");
    
    LightningEffect effect = lightningObj.AddComponent<LightningEffect>();
    effect.lightningColor = lightningColor;
    
    // Отключаем частицы
    effect.useParticles = false;
    
    // Настраиваем цвет в зависимости от типа молнии
    if (isHorizontal)
    {
        effect.particleColor = new Color(1f, 0.7f, 0.1f, 0.8f);
    }
    else
    {
        effect.particleColor = new Color(0.1f, 0.8f, 1f, 0.8f);
    }
    
    effect.Initialize(isHorizontal, positions);
    return effect;
}
}

