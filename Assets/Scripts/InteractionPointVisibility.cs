using UnityEngine;
using UnityEngine.Rendering.Universal;
using System.Collections.Generic;

public class InteractionPointVisibility : MonoBehaviour
{
    private SpriteRenderer spriteRenderer;
    private Light2D[] spotLights;

    // État piloté par le script SceneChanger
    private bool isPlayerNearby = false;

    void Start()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        CacheSpotLights();

        // On cache l'objet au lancement
        if (spriteRenderer != null)
            spriteRenderer.enabled = false;
    }

    // Trouve toutes les lampes torches dans la scène
    void CacheSpotLights()
    {
        var all = Object.FindObjectsByType<Light2D>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        var list = new List<Light2D>();
        foreach (var l in all)
        {
            // On cherche les lumières de type Point (utilisées pour les lampes torches)
            if (l.lightType == Light2D.LightType.Point)
                list.Add(l);
        }
        spotLights = list.ToArray();
    }

    // Appelée par SceneChanger quand un joueur entre/sort
    public void SetPlayerNearby(bool nearby)
    {
        isPlayerNearby = nearby;
    }

    void Update()
    {
        if (spriteRenderer == null) return;

        // Condition 1 : Proximité physique
        bool visible = isPlayerNearby;

        // Condition 2 : Lampe torche (si le joueur n'est pas encore à côté)
        if (!visible && spotLights != null)
        {
            foreach (var light in spotLights)
            {
                if (light == null || !light.gameObject.activeInHierarchy) continue;

                if (IsInsideCone(light))
                {
                    visible = true;
                    break;
                }
            }
        }

        spriteRenderer.enabled = visible;
    }

    // Calcul mathématique pour savoir si l'objet est dans le cône de lumière
    bool IsInsideCone(Light2D light)
    {
        Vector2 toPoint = (Vector2)transform.position - (Vector2)light.transform.position;
        float distance = toPoint.magnitude;

        // Vérifie la distance max de la lampe
        if (distance > light.pointLightOuterRadius) return false;

        // Vérifie l'angle du cône (transform.up est la direction de la lampe)
        float angle = Vector2.Angle((Vector2)light.transform.up, toPoint);
        return angle <= light.pointLightOuterAngle * 0.5f;
    }
}