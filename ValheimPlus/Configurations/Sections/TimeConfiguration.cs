namespace ValheimPlus.Configurations.Sections
{
    public class TimeConfiguration : ServerSyncConfig<TimeConfiguration>
    {
        public bool forcePartOfDay { get; internal set; } = false;
        public float forcePartOfDayTime { get; internal set; } = 0.5f;
        public float totalDayTimeInSeconds { get; internal set; } = 1800f;
        public float nightPercent { get; internal set; } = 30f;
    }
}
