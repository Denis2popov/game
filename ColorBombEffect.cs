using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ColorBombEffect : MonoBehaviour
{
    [Header("Настройки эффекта")]
    public Color particleColor = Color.magenta;
    public float particleSize = 0.2f;
    public float particleSpeed = 5f;
    public float effectDuration = 1.0f;
    
    private ParticleSystem particleSystem;
    private Vector3 targetPosition;
    
    public void Initialize(Vector3 startPosition, Vector3 targetPos, Color color)
    {
        transform.position = startPosition;
        targetPosition = targetPos;
        particleColor = color;
        
        // Создаем систему частиц
        CreateParticleSystem();
        
        // Автоматически уничтожаем объект после окончания эффекта
        Destroy(gameObject, effectDuration + 0.5f);
    }
    
    private void CreateParticleSystem()
    {
        // Создаем систему частиц
        particleSystem = gameObject.AddComponent<ParticleSystem>();
        
        // Настраиваем основные параметры
        var main = particleSystem.main;
        main.startLifetime = 1.0f;
        main.startSpeed = particleSpeed;
        main.startSize = particleSize;
        main.startColor = particleColor;
        
        // Настраиваем эмиссию
        var emission = particleSystem.emission;
        emission.rateOverTime = 30;
        
        // Направляем частицы к цели
        Vector3 direction = (targetPosition - transform.position).normalized;
        var velocity = particleSystem.velocityOverLifetime;
        velocity.enabled = true;
        velocity.x = direction.x * particleSpeed;
        velocity.y = direction.y * particleSpeed;
        
        // Добавляем модуль цвета по времени жизни
        var colorOverLifetime = particleSystem.colorOverLifetime;
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
        
        // Запускаем систему частиц
        particleSystem.Play();
    }
    
    // Статический метод для удобного создания эффекта
    public static ColorBombEffect CreateEffect(Vector3 startPosition, Vector3 targetPosition, Color color)
    {
        GameObject effectObj = new GameObject("ColorBombEffect");
        ColorBombEffect effect = effectObj.AddComponent<ColorBombEffect>();
        effect.Initialize(startPosition, targetPosition, color);
        return effect;
    }
}
