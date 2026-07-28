using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Services.CloudSave;
using UnityEngine;

public class CloudSaveManager : MonoBehaviour
{
    public static CloudSaveManager Instance;

    private void Awake()
    {
        Instance = this;
    }

    public async Task SaveMoney(float amount)
    {
        var data = new Dictionary<string, object>
        {
            { "Money", amount }
        };

        await CloudSaveService.Instance.Data.Player.SaveAsync(data);
    }

    public async Task<float> LoadMoney()
    {
        var result = await CloudSaveService.Instance.Data.Player.LoadAsync(
            new HashSet<string> { "Money" });

        if (result.TryGetValue("Money", out var item))
        {
            return item.Value.GetAs<float>();
        }

        return 0;
    }

    public async Task MigratePlayerPrefs()
    {
        bool migrated = PlayerPrefs.GetInt("CloudMigration", 0) == 1;

        if (migrated)
            return;

        float money = PlayerPrefs.GetFloat("Money", 0);

        await SaveMoney(money);

        PlayerPrefs.SetInt("CloudMigration", 1);
        PlayerPrefs.Save();
    }
}