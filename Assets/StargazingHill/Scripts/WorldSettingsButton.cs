using UdonSharp;

namespace StargazingHill
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class WorldSettingsButton : UdonSharpBehaviour
    {
        public const int Mirror = 0;
        public const int MirrorsOff = 1;
        public const int AlarmHour = 2;
        public const int AlarmMinute = 3;
        public const int AlarmToggle = 4;
        public const int AlarmStop = 5;
        public const int RadioToggle = 6;
        public const int SaveToggle = 7;
        public const int BoardToggle = 8;
        public const int LanguageToggle = 9;
        public const int MirrorQuality = 10;
        public const int NotifySoundToggle = 11;
        public const int NotifyDisplayToggle = 12;

        public WorldSettingsController controller;
        public int action;
        public int value;

        public override void Interact()
        {
            if (controller == null) return;
            if (action == Mirror) controller.ToggleMirror(value);
            else if (action == MirrorsOff) controller.AllMirrorsOff();
            else if (action == MirrorQuality) controller.ToggleMirrorQuality();
            else if (action == NotifySoundToggle) controller.ToggleNotifySound();
            else if (action == NotifyDisplayToggle) controller.ToggleNotifyDisplay();
            else if (action == AlarmHour) controller.AdjustAlarmHour(value);
            else if (action == AlarmMinute) controller.AdjustAlarmMinute(value);
            else if (action == AlarmToggle) controller.ToggleAlarm();
            else if (action == AlarmStop) controller.StopAlarmSound();
            else if (action == RadioToggle) controller.ToggleRadio();
            else if (action == SaveToggle) controller.ToggleSave();
            else if (action == BoardToggle) controller.ToggleBoard();
            else if (action == LanguageToggle) controller.ToggleLanguage();
        }
    }
}
