using UnityEngine;
using UnityEngine.UI;
using System.Threading.Tasks;
using Unity.Services.Authentication;
using TMPro;

public class FreeWorldLeaderboardUI : MonoBehaviour
{
    [Header("References")]
    public FreeWorldLeaderboardReader reader;
    public FreeWorldRunManager runManager;

    [Header("UI")]
    public ScrollRect scrollRect;
    public Transform contentParent;
    public LeaderboardRowUi rowPrefab;
    public LeaderboardRowUi pinnedPlayerRow;
    public TextMeshProUGUI emptyText;
    private const int PAGE_SIZE = 20;
    private const int MAX_ENTRIES = 100;

    private int currentOffset = 0;
    private bool isLoading = false;
    private bool reachedEnd = false;

    void Awake()
    {
        scrollRect.onValueChanged.AddListener(OnScroll);
    }

    public async void OpenLeaderboard()
    {
        ClearList();
        await LoadPinnedPlayer();
        await LoadNextPage();
    }

    private async Task LoadPinnedPlayer()
    {
        var me = await reader.GetMyRankSafe(runManager.ChallengeType);

        if (me == null)
        {
            pinnedPlayerRow.gameObject.SetActive(false);
            return;
        }

        pinnedPlayerRow.gameObject.SetActive(true);
        pinnedPlayerRow.Setup(me, true);
    }

    private async Task LoadNextPage()
    {
        if (isLoading || reachedEnd) return;
        if (currentOffset >= MAX_ENTRIES) return;

        isLoading = true;

        var page = await reader.GetLeaderboardPage(
            runManager.ChallengeType,
            currentOffset,
            PAGE_SIZE
        );
        if(page.Count == 0 && currentOffset == 0)
        {
            emptyText.gameObject.SetActive(true);
        }
        else
        {
            emptyText.gameObject.SetActive(false);
        }
        foreach (var entry in page)
        {
            CreateRow(entry);
        }

        currentOffset += page.Count;

        if (page.Count < PAGE_SIZE || currentOffset >= MAX_ENTRIES)
            reachedEnd = true;

        isLoading = false;
    }

    private void OnScroll(Vector2 pos)
    {
        if (isLoading || reachedEnd) return;

        // Near bottom
        if (scrollRect.verticalNormalizedPosition <= 0.1f)
        {
            _ = LoadNextPage();
        }
    }

    private void CreateRow(LeaderboardEntryData data)
    {
        var row = Instantiate(rowPrefab, contentParent);
        row.gameObject.SetActive(true);

        bool isMe =
            data.PlayerName == AuthenticationService.Instance.PlayerId;

        row.Setup(data, isMe);
    }

    private void ClearList()
    {
        foreach (Transform child in contentParent)
            Destroy(child.gameObject);

        currentOffset = 0;
        reachedEnd = false;
    }
}
