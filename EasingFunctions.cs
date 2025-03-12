using UnityEngine;

public static class EasingFunctions
{
    // Плавный старт
    public static float EaseInQuad(float t) => t * t;
    
    // Плавное окончание
    public static float EaseOutQuad(float t) => 1 - (1 - t) * (1 - t);
    
    // Плавный старт и окончание
    public static float EaseInOutQuad(float t)
    {
        return t < 0.5 ? 2 * t * t : 1 - Mathf.Pow(-2 * t + 2, 2) / 2;
    }
    
    // Пружинистый эффект в конце
    public static float EaseOutBack(float t)
    {
        float c1 = 1.70158f;
        float c3 = c1 + 1;
        return 1 + c3 * Mathf.Pow(t - 1, 3) + c1 * Mathf.Pow(t - 1, 2);
    }
    
    // Отскок
    public static float EaseOutBounce(float t)
    {
        float n1 = 7.5625f;
        float d1 = 2.75f;
        
        if (t < 1 / d1) {
            return n1 * t * t;
        } else if (t < 2 / d1) {
            return n1 * (t -= 1.5f / d1) * t + 0.75f;
        } else if (t < 2.5 / d1) {
            return n1 * (t -= 2.25f / d1) * t + 0.9375f;
        } else {
            return n1 * (t -= 2.625f / d1) * t + 0.984375f;
        }
    }
}
