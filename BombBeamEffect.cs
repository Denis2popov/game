using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BombBeamEffect : MonoBehaviour
{
    // Визуальные настройки луча
    [Header("Основные настройки")]
    public Color beamColor = Color.yellow;
    public float beamWidth = 0.7f;
    public float expansionSpeed = 10f;
    public float beamDuration = 0.5f;
    public float fadeOutTime = 0.3f;
    
    [Header("Эффекты частиц")]
    public bool useParticles = true;
    public int particleDensity = 100;
    public float particleSize = 0.15f;
    public Color particleColor = new Color(1f, 0.8f, 0.2f, 0.7f);
    
    // Внутренние переменные
    private LineRenderer beamLine;
    private ParticleSystem beamParticles;
    private bool isHorizontal;
    private int boardWidth;
    private int boardHeight;
    private float currentLength = 0f;
    private float targetLength;
    
    /// <summary>
    /// Инициализация эффекта луча от бомбы
    /// </summary>
    /// <param name="position">Позиция бомбы</param>
    /// <param name="horizontal">Горизонтальный или вертикальный луч</param>
    /// <param name="width">Ширина игрового поля</param>
    /// <param name="height">Высота игрового поля</param>
    public void Initialize(Vector3 position, bool horizontal, int width, int height)
    {
        transform.position = position;
        isHorizontal = horizontal;
        boardWidth = width;
        boardHeight = height;
        
        // Настраиваем целевую длину луча в зависимости от типа и позиции
        if (isHorizontal)
        {
            targetLength = boardWidth;
            transform.rotation = Quaternion.identity;
        }
        else
        {
            targetLength = boardHeight;
            transform.rotation = Quaternion.Euler(0, 0, 90);
        }
        
        // Создаем луч
        CreateBeam();
        
        // Запускаем анимацию
        StartCoroutine(AnimateBeam());
    }
    
    private void CreateBeam()
    {
        // Создаем LineRenderer для основного луча
        beamLine = gameObject.AddComponent<LineRenderer>();
        beamLine.positionCount = 2;
        beamLine.startWidth = beamWidth;
        beamLine.endWidth = beamWidth;
        beamLine.material = new Material(Shader.Find("Sprites/Default"));
        beamLine.startColor = beamColor;
        beamLine.endColor = new Color(beamColor.r, beamColor.g, beamColor.b, 0.4f);
        
        // Начальные позиции (нулевая длина)
        beamLine.SetPosition(0, Vector3.zero);
        beamLine.SetPosition(1, Vector3.zero);
        
        // Создаем систему частиц вдоль луча
        if (useParticles)
        {
            GameObject particleObj = new GameObject("BeamParticles");
            particleObj.transform.parent = transform;
            particleObj.transform.localPosition = Vector3.zero;
            particleObj.transform.localRotation = Quaternion.identity;
            
            beamParticles = particleObj.AddComponent<ParticleSystem>();
            
            // Настраиваем систему частиц
            var main = beamParticles.main;
            main.startLifetime = 0.5f;
            main.startSpeed = 0.2f;
            main.startSize = particleSize;
            main.startColor = particleColor;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            
            var emission = beamParticles.emission;
            emission.rateOverTime = particleDensity;
            
            var shape = beamParticles.shape;
            shape.shapeType = ParticleSystemShapeType.Box;
            
            // Настраиваем форму частиц в зависимости от типа луча
            if (isHorizontal)
            {
                shape.scale = new Vector3(1f, beamWidth, 0.1f);
            }
            else
            {
                shape.scale = new Vector3(beamWidth, 1f, 0.1f);
            }
            
            // Добавляем цветовой градиент
            var colorOverLifetime = beamParticles.colorOverLifetime;
            colorOverLifetime.enabled = true;
            
            Gradient gradient = new Gradient();
            gradient.SetKeys(
                new GradientColorKey[] { 
                    new GradientColorKey(particleColor, 0.0f),
                    new GradientColorKey(particleColor, 0.6f),
                    new GradientColorKey(new Color(1f, 1f, 1f), 1.0f)
                },
                new GradientAlphaKey[] { 
                    new GradientAlphaKey(0.7f, 0.0f),
                    new GradientAlphaKey(0.5f, 0.6f),
                    new GradientAlphaKey(0.0f, 1.0f)
                }
            );
            
            colorOverLifetime.color = gradient;
            
            // Добавляем модуль размера по времени жизни
            var sizeOverLifetime = beamParticles.sizeOverLifetime;
            sizeOverLifetime.enabled = true;
            sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, 0.3f);
        }
    }
    
    private IEnumerator AnimateBeam()
    {
        // Фаза расширения луча
        while (currentLength < targetLength)
        {
            currentLength += expansionSpeed * Time.deltaTime;
            currentLength = Mathf.Min(currentLength, targetLength);
            
            // Обновляем длину LineRenderer
            UpdateBeamLength(currentLength);
            
            // Обновляем размер и форму системы частиц
            if (useParticles && beamParticles != null)
            {
                var shape = beamParticles.shape;
                if (isHorizontal)
                {
                    shape.scale = new Vector3(currentLength, beamWidth, 0.1f);
                    shape.position = new Vector3(currentLength / 2, 0, 0);
                }
                else
                {
                    shape.scale = new Vector3(beamWidth, currentLength, 0.1f);
                    shape.position = new Vector3(0, currentLength / 2, 0);
                }
            }
            
            yield return null;
        }
        
        // Ждем пока луч полностью показывается
        yield return new WaitForSeconds(beamDuration);
        
        // Фаза затухания луча
        float fadeElapsed = 0f;
        Color initialStartColor = beamLine.startColor;
        Color initialEndColor = beamLine.endColor;
        
        while (fadeElapsed < fadeOutTime)
        {
            fadeElapsed += Time.deltaTime;
            float t = fadeElapsed / fadeOutTime;
            
            // Постепенно делаем луч прозрачным
            beamLine.startColor = new Color(
                initialStartColor.r,
                initialStartColor.g,
                initialStartColor.b,
                initialStartColor.a * (1 - t)
            );
            
            beamLine.endColor = new Color(
                initialEndColor.r,
                initialEndColor.g,
                initialEndColor.b,
                initialEndColor.a * (1 - t)
            );
            
            yield return null;
        }
        
        // Уничтожаем объект после завершения анимации
        Destroy(gameObject);
    }
    
    private void UpdateBeamLength(float length)
    {
        if (beamLine != null)
        {
            Vector3 endPoint;
            
            if (isHorizontal)
            {
                // Расширяется в обе стороны от центра для горизонтального луча
                beamLine.SetPosition(0, new Vector3(-length/2, 0, 0));
                beamLine.SetPosition(1, new Vector3(length/2, 0, 0));
            }
            else
            {
                // Расширяется в обе стороны от центра для вертикального луча
                beamLine.SetPosition(0, new Vector3(0, -length/2, 0));
                beamLine.SetPosition(1, new Vector3(0, length/2, 0));
            }
        }
    }
    
    /// <summary>
    /// Статический метод для удобного создания эффекта
    /// </summary>
    public static BombBeamEffect CreateBeamEffect(Vector3 position, bool isHorizontal, 
                                                 int boardWidth, int boardHeight, Color beamColor)
    {
        GameObject beamObj = new GameObject("BombBeam");
        beamObj.transform.position = position;
        
        BombBeamEffect effect = beamObj.AddComponent<BombBeamEffect>();
        effect.beamColor = beamColor;
        
        // Настраиваем цвет частиц в зависимости от типа луча
        if (isHorizontal)
        {
            effect.particleColor = new Color(1f, 0.7f, 0.2f, 0.7f); // Оранжевый
        }
        else
        {
            effect.particleColor = new Color(0.2f, 0.7f, 1f, 0.7f); // Голубой
        }
        
        effect.Initialize(position, isHorizontal, boardWidth, boardHeight);
        return effect;
    }
}
