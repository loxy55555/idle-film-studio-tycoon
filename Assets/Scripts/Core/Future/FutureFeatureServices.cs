using UnityEngine;

public class FutureFeatureServices : MonoBehaviour
{
    public static FutureFeatureServices Instance { get; private set; }

    public ICityService             City             { get; private set; }
    public IShopService             Shop             { get; private set; } = new ShopServiceStub();
    public IAdvancedDiamondsService AdvancedDiamonds { get; private set; } = new AdvancedDiamondsServiceStub();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void EnsureInstance()
    {
        if (Instance != null) return;
        var hub = Object.FindAnyObjectByType<GameHub>();
        if (hub == null) return;
        if (hub.GetComponent<FutureFeatureServices>() != null) return;
        hub.gameObject.AddComponent<FutureFeatureServices>();
    }

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(this); return; }
        Instance = this;
        City = new CityServiceBridge();
    }
}

public interface ICityService
{
    bool IsAvailable { get; }
    int Level { get; }
    float GlobalMultiplier { get; }
    bool CanUpgrade { get; }
}

public interface IShopService
{
    bool IsAvailable { get; }
}

public interface IAdvancedDiamondsService
{
    bool IsAvailable { get; }
}

public sealed class CityServiceBridge : ICityService
{
    CitySystem City => GameHub.Instance?.city;

    public bool IsAvailable => GameFeatureFlags.IsEnabled(GameFeature.City) && City != null;
    public int Level => City != null ? City.Level : 1;
    public float GlobalMultiplier => City != null ? City.GlobalMultiplier : 1f;
    public bool CanUpgrade => City != null && City.CanUpgrade();
}

public sealed class ShopServiceStub : IShopService
{
    public bool IsAvailable => GameFeatureFlags.IsEnabled(GameFeature.Shop);
}

public sealed class AdvancedDiamondsServiceStub : IAdvancedDiamondsService
{
    public bool IsAvailable => GameFeatureFlags.IsEnabled(GameFeature.AdvancedDiamonds);
}
