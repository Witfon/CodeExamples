using UnityEngine;

public class ParticleSpawner : MonoBehaviour
{
    [System.Serializable]
    public class ParticleSpawn
    {
        public GameObject particle;
        public Vector3 offset;
        public Gradient color;
        public bool useNativeColor;
        public bool useVelocity;
        public float velocityMultiplier = 1f;
    }
    [System.Serializable]
    public class FilteredParticleSpawn : ParticleSpawn
    {
        //array of tags of objects that shouldn't cause this object to spawn particles on collision
        public string[] whitelistTags;
    }
    public ParticleSpawn[] startParticles;
    public ParticleSpawn[] deathParticles;
    public FilteredParticleSpawn[] collisionParticles;
    public FilteredParticleSpawn[] triggerParticles;
    void Start(){SpawnParticles(startParticles, transform.position);}
    private void OnDestroy()
    {
        //try{SpawnParticles(deathParticles, transform.position);}
        //catch{}
    }

    void OnDisable()
    {
        if (Time.time > 1.1f)
        {
            try { SpawnParticles(deathParticles, transform.position); }
            catch { }
        }
        
    }
    void SpawnParticles(ParticleSpawn[] particles, Vector3 pos)
    {
        Rigidbody masterRb = new Rigidbody();
        if (gameObject.TryGetComponent(out Rigidbody rb)){masterRb = rb;}
        for (int i = 0; i < particles.Length; i++)
        {
            if (particles[i] != null)
            {
                Vector3 rbVelocity = new Vector3();
                if (particles[i].useVelocity)
                    rbVelocity = masterRb.velocity;
                if (!particles[i].useNativeColor)
                {
                    ParticleManager.SpawnParticle(particles[i].particle, pos + particles[i].offset, particles[i].color, rbVelocity, particles[i].velocityMultiplier);
                }
                else
                {
                    ParticleManager.SpawnParticle(particles[i].particle, pos + particles[i].offset, null, rbVelocity, particles[i].velocityMultiplier);
                }
            }
        }
    }
    void SpawnFilteredParticles(FilteredParticleSpawn[] particles, string filterTag, Vector3 collisionPos)
    {
        if (particles.Length > 0)
        { 
            for (int i = 0; i < particles.Length; i++)
            {
                bool blacklistTag = true;
                for (int j = 0; j < particles[i].whitelistTags.Length; j++)
                {
                    if (filterTag == gameObject.tag)
                    {
                        blacklistTag = false;
                        break;
                    }
                }
                if (blacklistTag)
                    return;
                SpawnParticles(particles, collisionPos);
            }
        }
    }
    private void OnTriggerEnter(Collider other){SpawnFilteredParticles(triggerParticles, other.tag, transform.position);}
    private void OnCollisionEnter(Collision collision){SpawnFilteredParticles(collisionParticles, collision.gameObject.tag, collision.GetContact(0).point);}
}
