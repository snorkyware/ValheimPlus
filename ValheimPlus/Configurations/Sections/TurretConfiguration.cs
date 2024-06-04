namespace ValheimPlus.Configurations.Sections
{
    public class TurretConfiguration : ServerSyncConfig<TurretConfiguration>
    {
        public bool ignorePlayers { get; internal set; } = false;
        public bool unlimitedAmmo { get; internal set; } = false;
        public float turnRate { get; internal set; } = 0;
        public float attackCooldown { get; internal set; } = 0;
        public float viewDistance { get; internal set; } = 0;
        public float projectileVelocity { get; internal set; } = 0f;
        public float projectileAccuracy { get; internal set; } = 0f;
    }
}