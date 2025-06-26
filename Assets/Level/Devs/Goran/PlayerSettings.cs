using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections.Generic;

public class PlayerSettings : MonoBehaviour
{
    public static readonly Dictionary<string, string> RankNames = new Dictionary<string, string>
    {
        { "rank_1", "Rookie" },
        { "rank_2", "Scrapper" },
        { "rank_3", "Brawler" },
        { "rank_4", "Gunner" },
        { "rank_5", "Enforcer" },
        { "rank_6", "Specialist" },
        { "rank_7", "Sharpshot" },
        { "rank_8", "Reaper" },
        { "rank_9", "Legend" }
    };

    [SerializeField] private TMP_InputField usernameInputField;
    [SerializeField] private TMP_Text rankXpText;
    [SerializeField] private TMP_Text rankText;
    [SerializeField] private Slider progressSlider;
    [SerializeField] private Image rankIcon;
    [SerializeField] private Sprite[] rankSprites;
    [SerializeField] private TMP_Dropdown resolutionDropdown;
    [SerializeField] private Slider volumeSlider;

    public string Username { get; private set; } = "";
    public string Rank { get; private set; } = "rank_1";
    public string RankXp { get; private set; } = "0";

    private Resolution[] resolutions;

    void Start()
    {
        resolutions = Screen.resolutions;

        resolutionDropdown.ClearOptions();

        List<string> options = new List<string>();
        int currentResolutionIndex = 0;

        for (int i = 0; i < resolutions.Length; i++)
        {
            string option = resolutions[i].width + "x" + resolutions[i].height;
            options.Add(option);

            if (resolutions[i].width == Screen.currentResolution.width && resolutions[i].height == Screen.currentResolution.height)
            {
                currentResolutionIndex = i;
            }
        }

        resolutionDropdown.AddOptions(options);
        resolutionDropdown.value = currentResolutionIndex;
        resolutionDropdown.RefreshShownValue();

        QualitySettings.SetQualityLevel(0);

        QualitySettings.vSyncCount = 1;
    }

    public void OnTypedUsername(string _username)
    {
        Username = _username;
    }

    public void SetRank(string _rank)
    {
        Rank = _rank;
    }

    public void SetRankXp(string _rankXp)
    {
        RankXp = _rankXp;
    }

    public void SetResolution(int resolutionIndex)
    {
        Resolution resolution = resolutions[resolutionIndex];
        Screen.SetResolution(resolution.width, resolution.height, Screen.fullScreen);
    }

    public void SetQuality(int qualityIndex)
    {
        QualitySettings.SetQualityLevel(qualityIndex);
    }

    public void SetFullscreen(bool isFullscreen)
    {
        Screen.fullScreen = isFullscreen;
    }

    public void SetVolume()
    {
        AudioListener.volume = volumeSlider.value;
    }

    public void SetSettings(string _username, string _rank, string _rankXp)
    {
        Username = _username;
        Rank = _rank;
        RankXp = _rankXp;

        usernameInputField.text = Username;
        rankXpText.text = Rank == "rank_9" ? $"{RankXp} XP" : $"{RankXp} / 100 XP";
        rankText.text = RankNameString(Rank);
        progressSlider.maxValue = 1.0f;
        progressSlider.value = ConvertXpStringToFloat(RankXp);
        rankIcon.sprite = System.Array.Find(rankSprites, s => s.name == Rank);
    }

    private string RankNameString(string rankKey)
    {
        return RankNames.TryGetValue(rankKey, out var rankName) ? rankName : "Unknown";
    }

    public float ConvertXpStringToFloat(string xp)
    {
        if (float.TryParse(xp, out float result))
        {
            return result / 100f;
        }

        return 0.0f;
    }
}