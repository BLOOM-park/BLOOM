using UnityEngine;
using System;

using TMPro;

public class QuestManager : MonoBehaviour
{
    public static QuestManager instance;

    public int currentDay = 1;

    [Header("Quest Goals")]
    public int day1TreeGoal = 3;
    private int treesPlantedToday = 0;

    public int day2TrashGoal = 5;
    private int trashDisposedToday = 0;

    public int day3RiverTrashGoal = 5;
    private int riverTrashDisposedToday = 0;

    [Header("UI / Feedback")]
    public TextMeshProUGUI missionText; // UI Text component
    private bool isDayQuestComplete = false;
    private bool allQuestsComplete = false;

    private void Awake()
    {
        if (instance != null) Destroy(gameObject);
        else instance = this;
        
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        SetupDay(currentDay);
    }

    private void OnEnable()
    {
        TreePlanter.OnTreePlanted += HandleTreePlanted;
    }

    private void OnDisable()
    {
        TreePlanter.OnTreePlanted -= HandleTreePlanted;
    }

    public void SetupDay(int day)
    {
        isDayQuestComplete = false;
        
        Debug.Log($"--- STARTING DAY {day} ---");
        
        switch (day)
        {
            case 1:
                treesPlantedToday = 0;
                Debug.Log($"Quest: Plant {day1TreeGoal} trees.");
                break;
            case 2:
                trashDisposedToday = 0;
                Debug.Log($"Quest: Dispose of {day2TrashGoal} pieces of trash in the Dumpster.");
                break;
            case 3:
                riverTrashDisposedToday = 0;
                Debug.Log($"Quest: fish out {day3RiverTrashGoal} items from the river and dispose them.");
                break;
            default:
                Debug.Log("No specific quest for this day yet. Free play.");
                isDayQuestComplete = true; // Free pass
                break;
        }
        UpdateMissionText();
    }

    private void UpdateMissionText()
    {
        if (missionText == null) return;

        if (allQuestsComplete)
        {
            missionText.text = "Congratulations!\nYou completed all three days of quests!";
            return;
        }

        if (isDayQuestComplete)
        {
            missionText.text = "Mission Complete!\nGo to sleep to start the next day.";
            return;
        }

        switch (currentDay)
        {
            case 1:
                missionText.text = $"Mission:\nPlant Trees ({treesPlantedToday}/{day1TreeGoal})";
                break;
            case 2:
                missionText.text = $"Mission:\nDispose Trash ({trashDisposedToday}/{day2TrashGoal})";
                break;
            case 3:
                missionText.text = $"Mission:\nClean River ({riverTrashDisposedToday}/{day3RiverTrashGoal})";
                break;
            default:
                missionText.text = "No active missions.";
                break;
        }
    }

    #region Day 1: Tree Planting
    private void HandleTreePlanted()
    {
        if (currentDay == 1)
        {
            treesPlantedToday++;
            Debug.Log($"Trees Planted: {treesPlantedToday}/{day1TreeGoal}");
            CheckQuestCompletion();
            UpdateMissionText();
        }
    }
    #endregion

    #region Day 2 & 3: Trash Disposal
    // Called by Dumpster script
    public void HandleTrashDisposed(bool isRiverTrash)
    {
        if (currentDay == 2 && !isRiverTrash) // Normal trash
        {
            trashDisposedToday++;
            Debug.Log($"Trash Disposed: {trashDisposedToday}/{day2TrashGoal}");
            CheckQuestCompletion();
            UpdateMissionText();
        }
        else if (currentDay == 3 && isRiverTrash) // River trash
        {
            riverTrashDisposedToday++;
            Debug.Log($"River Trash Disposed: {riverTrashDisposedToday}/{day3RiverTrashGoal}");
            CheckQuestCompletion();
            UpdateMissionText();
        }
    }
    #endregion

    private void CheckQuestCompletion()
    {
        bool complete = false;
        switch (currentDay)
        {
            case 1:
                if (treesPlantedToday >= day1TreeGoal) complete = true;
                break;
            case 2:
                if (trashDisposedToday >= day2TrashGoal) complete = true;
                break;
            case 3:
                if (riverTrashDisposedToday >= day3RiverTrashGoal) complete = true;
                break;
        }

        if (complete && !isDayQuestComplete)
        {
            isDayQuestComplete = true;
            Debug.Log("Daily Quest Complete! You can now go to sleep.");
            UpdateMissionText();
        }
    }

    public bool TrySleep()
    {
        if (isDayQuestComplete)
        {
            Debug.Log("Sleeping... processing to next day.");
            AdvanceDay();
            return true;
        }
        else
        {
            Debug.Log("You cannot sleep yet. Quest not complete.");
            return false;
        }
    }

    private void AdvanceDay()
    {
        // Check if we just finished day 3
        if (currentDay == 3)
        {
            allQuestsComplete = true;
            UpdateMissionText();
            return;
        }
        
        currentDay++;
        SetupDay(currentDay);
        // Reset Time handled by DayNightController listener if needed, or call it directly
        // For simplicity, we might just set the time to morning here if we have a reference
        if (DayNightController.instance != null)
        {
            DayNightController.instance.SetTime(6f); // 6 AM
        }
    }

}
