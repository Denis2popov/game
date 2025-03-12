using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BombEffectManager : MonoBehaviour
{
    // Синглтон для удобного доступа
    public static BombEffectManager Instance { get; private set; }
    
    // Префабы эффектов
    [Header("Эффекты взрыва")]
    public GameObject horizontalExplosionPrefab;
    public GameObject verticalExplosionPrefab;
    public GameObject explosionFlashPrefab;
    
    [Header("Настройки эффектов")]
    public float explosionDuration = 0.7f;
    public float flashDuration = 0.2f;
    public float cameraShakeDuration = 0.3f;
    public float cameraShakeIntensity = 0.15f;
    
    [Header("Звуки")]
    public AudioClip explosionSound;
    public AudioClip bombMatchSound;
    
    private AudioSource audioSource;
    private Camera mainCamera;
    private Vector3 originalCameraPosition;
    
    private void Awake()
    {
        // Реализация синглтона
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            
            // Инициализация компонентов
            audioSource = gameObject.AddComponent<AudioSource>();
            mainCamera = Camera.main;
            
            // Создаем массивы для рандомизации эффектов, если они не назначены
            if (horizontalExplosionPrefab == null)
                CreateDefaultExplosionPrefab(true);
            
            if (verticalExplosionPrefab == null)
                CreateDefaultExplosionPrefab(false);
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    /// <summary>
    /// Создает взрыв бомбы на указанной позиции
    /// </summary>
    public void CreateBombExplosion(Vector3 position, GamePiece.BombType bombType, Color pieceColor)
    {
        StartCoroutine(ExplosionSequence(position, bombType, pieceColor));
    }
    
    /// <summary>
    /// Полная последовательность эффектов взрыва
    /// </summary>
    private IEnumerator ExplosionSequence(Vector3 position, GamePiece.BombType bombType, Color pieceColor)
    {
        // Сохраняем изначальную позицию камеры
        originalCameraPosition = mainCamera.transform.position;
        
        // Создаем эффект вспышки
        CreateFlashEffect(position);
        
        // Воспроизводим звук
        PlayExplosionSound();
        
        // Создаем основной эффект взрыва
        GameObject explosionObj = null;
        
        if (bombType == GamePiece.BombType.Horizontal)
        {
            explosionObj = Instantiate(horizontalExplosionPrefab, position, Quaternion.identity);
        }
        else if (bombType == GamePiece.BombType.Vertical)
        {
            explosionObj = Instantiate(verticalExplosionPrefab, position, Quaternion.identity);
        }
        
        // Настраиваем цвет частиц
        if (explosionObj != null)
        {
            var particles = explosionObj.GetComponentsInChildren<ParticleSystem>();
            foreach (var ps in particles)
            {
                var main = ps.main;
                main.startColor = new ParticleSystem.MinMaxGradient(pieceColor, Color.white);
            }
        }
        
        // Запускаем эффект тряски камеры
        StartCoroutine(ShakeCamera());
        
        // Ждем завершения эффекта
        yield return new WaitForSeconds(explosionDuration);
        
        // Уничтожаем эффект
        if (explosionObj != null)
            Destroy(explosionObj);
    }
    
    /// <summary>
    /// Создает эффект вспышки при взрыве
    /// </summary>
    private void CreateFlashEffect(Vector3 position)
    {
        if (explosionFlashPrefab != null)
        {
            GameObject flash = Instantiate(explosionFlashPrefab, position, Quaternion.identity);
            Destroy(flash, flashDuration);
        }
        else
        {
            // Создаем простой эффект вспышки, если префаб не назначен
            GameObject flashObj = new GameObject("ExplosionFlash");
            flashObj.transform.position = position;
            
            SpriteRenderer renderer = flashObj.AddComponent<SpriteRenderer>();
            renderer.sprite = CreateCircleSprite(1f, Color.white);
            renderer.sortingOrder = 10;
            
            // Анимация затухания вспышки
            StartCoroutine(FadeOutAndDestroy(renderer, flashDuration));
        }
    }
    
    /// <summary>
    /// Эффект тряски камеры
    /// </summary>
    private IEnumerator ShakeCamera()
    {
        float elapsed = 0f;
        
        while (elapsed < cameraShakeDuration)
        {
            float x = originalCameraPosition.x + Random.Range(-1f, 1f) * cameraShakeIntensity;
            float y = originalCameraPosition.y + Random.Range(-1f, 1f) * cameraShakeIntensity;
            
            mainCamera.transform.position = new Vector3(x, y, originalCameraPosition.z);
            
            elapsed += Time.deltaTime;
            yield return null;
        }
        
        // Возвращаем камеру в исходное положение
        mainCamera.transform.position = originalCameraPosition;
    }
    
    /// <summary>
    /// Воспроизводит звук взрыва
    /// </summary>
    private void PlayExplosionSound()
    {
        if (explosionSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(explosionSound, 0.7f);
        }
    }
    
    /// <summary>
    /// Затухание спрайта
    /// </summary>
    private IEnumerator FadeOutAndDestroy(SpriteRenderer renderer, float duration)
    {
        float elapsed = 0f;
        Color startColor = renderer.color;
        
        while (elapsed < duration)
        {
            float t = elapsed / duration;
            Color newColor = startColor;
            newColor.a = Mathf.Lerp(1f, 0f, EasingFunctions.EaseOutQuad(t));
            renderer.color = newColor;
            
            elapsed += Time.deltaTime;
            yield return null;
        }
        
        Destroy(renderer.gameObject);
    }
    
    /// <summary>
    /// Создает спрайт круга для вспышки
    /// </summary>
    private Sprite CreateCircleSprite(float radius, Color color)
    {
        int textureSize = 128;
        Texture2D texture = new Texture2D(textureSize, textureSize);
        
        float centerX = textureSize / 2f;
        float centerY = textureSize / 2f;
        
        for (int x = 0; x < textureSize; x++)
        {
            for (int y = 0; y < textureSize; y++)
            {
                float distance = Vector2.Distance(new Vector2(x, y), new Vector2(centerX, centerY));
                
                if (distance < radius * (textureSize / 2f))
                {
                    float alpha = 1f - (distance / (radius * textureSize / 2f));
                    texture.SetPixel(x, y, new Color(color.r, color.g, color.b, alpha));
                }
                else
                {
                    texture.SetPixel(x, y, Color.clear);
                }
            }
        }
        
        texture.Apply();
        
        return Sprite.Create(texture, new Rect(0, 0, textureSize, textureSize), new Vector2(0.5f, 0.5f));
    }
    
    /// <summary>
    /// Создает базовые префабы эффектов, если они не назначены
    /// </summary>
    private void CreateDefaultExplosionPrefab(bool isHorizontal)
    {
        GameObject prefab = new GameObject(isHorizontal ? "HorizontalExplosion" : "VerticalExplosion");
        
        // Основная система частиц
        ParticleSystem mainPS = prefab.AddComponent<ParticleSystem>();
        var main = mainPS.main;
        main.startLifetime = 0.6f;
        main.startSpeed = 5f;
        main.startSize = 0.3f;
        main.startColor = Color.white;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        
        var emission = mainPS.emission;
        emission.rateOverTime = 100;
        
        var shape = mainPS.shape;
        shape.shapeType = ParticleSystemShapeType.Box;
        
        if (isHorizontal)
        {
            shape.scale = new Vector3(6f, 0.1f, 0.1f);
            horizontalExplosionPrefab = prefab;
        }
        else
        {
            shape.scale = new Vector3(0.1f, 6f, 0.1f);
            verticalExplosionPrefab = prefab;
        }
        
        // Добавляем дополнительные эффекты частиц
        CreateAdditionalParticleSystem(prefab, isHorizontal);
        
        // Скрываем префаб
        prefab.SetActive(false);
    }
    
    /// <summary>
    /// Создает дополнительную систему частиц для более красивого эффекта
    /// </summary>
    private void CreateAdditionalParticleSystem(GameObject parent, bool isHorizontal)
    {
        GameObject child = new GameObject("ExplosionWave");
        child.transform.parent = parent.transform;
        child.transform.localPosition = Vector3.zero;
        
        ParticleSystem wavePS = child.AddComponent<ParticleSystem>();
        var main = wavePS.main;
        main.startLifetime = 0.5f;
        main.startSpeed = 2f;
        main.startSize = 0.2f;
        main.startColor = new Color(1f, 1f, 1f, 0.3f);
        
        var emission = wavePS.emission;
        emission.rateOverTime = 50;
        
        var shape = wavePS.shape;
        shape.shapeType = ParticleSystemShapeType.Box;
        
        if (isHorizontal)
            shape.scale = new Vector3(6f, 0.5f, 0.1f);
        else
            shape.scale = new Vector3(0.5f, 6f, 0.1f);
        
        // Добавляем цветной модуль
        var colorOverLifetime = wavePS.colorOverLifetime;
        colorOverLifetime.enabled = true;
        
        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new GradientColorKey[] { new GradientColorKey(Color.yellow, 0.0f), new GradientColorKey(Color.red, 1.0f) },
            new GradientAlphaKey[] { new GradientAlphaKey(1.0f, 0.0f), new GradientAlphaKey(0.0f, 1.0f) }
        );
        
        colorOverLifetime.color = gradient;
    }
}
