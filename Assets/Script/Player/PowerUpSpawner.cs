using UnityEngine;

public class PowerUpSpawner : MonoBehaviour
{
    public GameObject[] powerUpPrefabs; 
    public float respawnDelay = 5f; // 重新生成的等待时间

    private GameObject currentSpawnedPowerUp; // 记录当前生成的道具实例
    private float timer;
    private bool isWaitingToRespawn = false;

    void Start()
    {
        SpawnRandomPowerUp();
    }

    void Update()
    {
        // 1. 如果当前位置有道具，则什么都不做
        if (currentSpawnedPowerUp != null) return;

        // 2. 如果道具被吃了（变为null），且还没有启动倒计时，则启动它
        if (!isWaitingToRespawn)
        {
            isWaitingToRespawn = true;
            timer = respawnDelay; // 重置 5 秒倒计时
        }

        // 3. 开始倒计时
        if (isWaitingToRespawn)
        {
            timer -= Time.deltaTime;

            // 4. 时间到，生成新道具，并关闭倒计时状态
            if (timer <= 0f)
            {
                SpawnRandomPowerUp();
                isWaitingToRespawn = false;
            }
        }
    }

    void SpawnRandomPowerUp()
    {
        if (powerUpPrefabs != null && powerUpPrefabs.Length > 0)
        {
            int randomIndex = Random.Range(0, powerUpPrefabs.Length);
            // 将生成的道具赋值给 currentSpawnedPowerUp 记录下来
            currentSpawnedPowerUp = Instantiate(powerUpPrefabs[randomIndex], transform.position, Quaternion.identity);
        }
    }
}