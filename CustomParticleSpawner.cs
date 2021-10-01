using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CustomParticleSpawner : MonoBehaviour
{
    CustomParticleBehavior[] particleScript;

    public bool activate = true;
    public bool activeOnStart = true;
    public GameObject customParicle;
    public Transform objectSpace;

    [Space]
    public float maxIntensity = 1;
    public float minIntensity = 0;
    public Color color = new Color(1,1,1,1);
    public float fadeInSpeed = 1;
    public float fadeOutSpeed = 1;
    public bool fadeIn;
    public bool lerpFade;
    public float speedMult;
    public bool animateLight;

    [Space]
    [Tooltip("when this amount of time is left, the particle will start to fade")]
    public float fadeAt = 1f;

    [Space]
    [Tooltip("Spawn rate frequency in seconds.")]
    public Vector2 frequency;
    public Vector2 lifetime;
    public Vector2 direction;
    public Vector2 particleRotationDir;
    public Vector2 particleScale = new Vector2(1,1);
    [Tooltip("Multiplied by the particle scale")]
    public Vector2 particleRandomScale = new Vector2(1, 1);
    public Quaternion particleRotation;
    public float particleSpeed = 1f;

    public int prewarmSize = 1;

    [Tooltip("Will linearly loop through all particle prewarms. Uncheck to use dynamic looping which will pick the first available particle.")]
    public bool linearIndexLooping;
    //used to dynamically loop through prewarm sprites
    int spawnIndex = 0;

    List<GameObject> allParticles = new List<GameObject>();
    float[] particleLifetimes;

    public Vector2[] offsetCorners = new Vector2[2];
    public Vector2 offsetPos;
    public bool drawBorders = false;

    [Tooltip("Used for trail particles. Leave empty if not using trail particles.")]
    public SpriteRenderer customSpriteSource;

    // Start is called before the first frame update
    void Start()
    {
        particleLifetimes = new float[prewarmSize];
        particleScript = new CustomParticleBehavior[prewarmSize];

        if (objectSpace == null)
            objectSpace = transform;

        for (int i = 0; i < prewarmSize; i++)
        {
            GameObject spawnedParticle = Instantiate(customParicle, transform.position, Quaternion.identity, objectSpace);
            allParticles.Add(spawnedParticle);
            particleScript[i] = spawnedParticle.GetComponent<CustomParticleBehavior>();

            if (customSpriteSource != null)
                particleScript[i].ChangeSprite(customSpriteSource.sprite);

            //transfering all values to the spawned particle
            /*public float maxIntensity = 1;
    public float minIntensity = 0;
    public Color Color;
    public float fadeInSpeed = 1;
    public float fadeOutSpeed = 1;
    public bool fadeIn;
    public bool lerpFade;
    public float speedMult;
    public bool animateLight;*/

            TransferParticleValues(particleScript[i]);

            spawnedParticle.SetActive(false);
        }

        if(activeOnStart)
            StartCoroutine(ParticleSpawner());
    }

    void TransferParticleValues(CustomParticleBehavior particle)
    {
        particle.maxIntensity = maxIntensity;
        particle.minIntensity = minIntensity;
        particle.color = color;
        particle.fadeInSpeed = fadeInSpeed;
        particle.fadeOutSpeed = fadeOutSpeed;
        particle.fadeIn = fadeIn;
        particle.lerpFade = lerpFade;
        particle.speed = speedMult;
        particle.animateLight = animateLight;
    }

    // Update is called once per frame
    void Update()
    {
        //checking for "dieded" particles and returning them into the prewarm pool
        for (int i = 0; i < allParticles.Count; i++)
        {
            if (allParticles[i].activeSelf)
            {
                particleLifetimes[i] -= Time.fixedDeltaTime;

                if (particleLifetimes[i] <= 0f)
                {
                    allParticles[i].SetActive(false);
                }

                if (particleLifetimes[i] < fadeAt)
                    particleScript[i].fadeIn = false;
            }
        }
    }

    //Chooses the particle to spawn
    void ChooseParticle(int index)
    {
        GameObject spawnedParticle = allParticles[index];

        //spawnedParticle.SetActive(true);
        spawnedParticle.SetActive(true);
        Vector2 offset = RandomSquarePos(offsetCorners) + offsetPos;
        spawnedParticle.transform.position = new Vector3(transform.position.x + offset.x, transform.position.y + offset.y, spawnedParticle.transform.position.z);

        //Transfering all particle values after spawn

        particleLifetimes[index] = Random.Range(lifetime.x, lifetime.y);
        particleScript[index].fadeIn = true;
        particleScript[index].speed = particleSpeed;
        //particle direction
        if (direction != Vector2.zero)
        {
            particleScript[index].direction = direction;
        }
        else
        {
            particleScript[index].direction = Random.insideUnitCircle.normalized;
        }

        if (particleRotationDir != Vector2.zero)
        {
            particleScript[index].gameObject.transform.rotation = Quaternion.LookRotation(new Vector3(0, 0, 1), particleRotationDir);
        }
        else
        {
            particleScript[index].gameObject.transform.localRotation = particleRotation;
        }

        if (customSpriteSource != null)
            particleScript[index].ChangeSprite(customSpriteSource.sprite);

        float newRandomScale = Random.Range(particleRandomScale.x, particleRandomScale.y);

        particleScript[index].gameObject.transform.localScale = Vector2.Scale(particleScale, new Vector2(newRandomScale, newRandomScale));
        
    }

    IEnumerator ParticleSpawner()
    {
        while (true)
        {
            //wait for x amount of seconds
            yield return new WaitForSeconds(Random.Range(frequency.x, frequency.y));

            //This happens when the function is called
            if (activate)
            {
                Emit();
            }
        }
    }

    public void Emit()
    {
        if (linearIndexLooping)
        {
            ChooseParticle(spawnIndex);
            spawnIndex++;

            if (spawnIndex >= prewarmSize)
                spawnIndex = 0;
        }
        else
        {
            for (int i = 0; i < allParticles.Count; i++)
            {
                if (!allParticles[i].activeSelf)
                {
                    ChooseParticle(i);

                    break;
                }
            }
        }
        //print("finishedSpawning");
    }

    public void SpawnParticles(float duration)
    {
        activate = true;
        CancelInvoke("DeactivateParticles");
        Invoke("DeactivateParticles", duration);
    }

    void DeactivateParticles()
    {
        activate = false;
    }

    Vector2 RandomSquarePos(Vector2[] corners)
    {
        if (corners.Length > 1)
        {
            return new Vector2(Random.Range(corners[0].x, corners[0].y), Random.Range(corners[1].x, corners[1].y));
        }
        else
        {
            return new Vector2(0, 0);
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (drawBorders)
        {
            Gizmos.DrawWireCube(transform.position, new Vector3(offsetCorners[0].y - offsetCorners[0].x, offsetCorners[1].y - offsetCorners[1].x, 0));
        }
        
    }
}
